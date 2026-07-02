// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using DesktopAnalytics;
using Newtonsoft.Json;
using SIL.Code;
using SIL.IO;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Durable local delivery for DesktopAnalytics Track/ReportException calls. Every event is
	/// first persisted as its own small JSON file under a per-user outbox directory, then an
	/// immediate best-effort flush is attempted; a full directory sweep also runs at startup and
	/// (bounded) at shutdown, so events survive being generated while offline instead of
	/// DesktopAnalytics' own silent fire-and-forget loss.
	/// </summary>
	/// <remarks>
	/// One file per event (not one shared queue file) avoids needing any cross-process lock:
	/// FieldWorks can run multiple simultaneous processes (one per open project), and a single
	/// process can also run concurrent flushes (e.g. the enqueue-time immediate attempt racing a
	/// startup sweep), so a queued file is "claimed" for delivery by renaming it to a deterministic
	/// ".inflight" name and then opening that with <see cref="FileShare.None"/> - the exclusive
	/// open, not the rename, is the real single-owner guarantee (see D14: two callers racing an
	/// identical rename can both report success). A sweep that later finds an already-".inflight"
	/// file only treats it as a crash orphan once it's old enough (<see cref="OrphanClaimStaleness"/>)
	/// that no legitimate in-progress delivery could still be running; a fresh one is left alone.
	/// See openspec/changes/telemetry-migration-baseline/design.md for the full rationale,
	/// including a documented gap: DesktopAnalytics' Track call is internally fire-and-forget and
	/// essentially never throws synchronously, so a network-availability pre-check (not a real
	/// delivery confirmation) gates whether a claim is even attempted.
	/// </remarks>
	public static class AnalyticsOutbox
	{
		private const string ExceptionEventKind = "Exception";
		private const string TrackEventKind = "Track";
		private const string InflightSuffix = ".inflight";
		private const string RecoveringSuffix = ".recovering";
		private const string QueuedExceptionReportEventName = "QueuedExceptionReport";

		/// <summary>
		/// How long an ".inflight" claim must sit untouched before a sweep is willing to treat it
		/// as orphaned (left behind by a crashed claimant) rather than a legitimate, still-in-
		/// progress delivery by another thread or process. Kept comfortably above any realistic
		/// delivery duration - including a slow network - so a healthy in-progress claim is never
		/// mistaken for an orphan and delivered twice (a real double-delivery bug this class's own
		/// tests caught during development, when a naive "any .inflight file is safe to redeliver"
		/// rule raced two concurrent flushes against the exact same file). Not readonly so tests
		/// can shrink it instead of waiting real minutes.
		/// </summary>
		internal static TimeSpan OrphanClaimStaleness = TimeSpan.FromMinutes(5);

		/// <summary>
		/// Maximum number of queued files retained; oldest excess files are evicted unsent.
		/// Not a const so tests can shrink it rather than creating thousands of files.
		/// </summary>
		internal static int MaxQueuedFiles = 2000;

		/// <summary>
		/// Maximum age of a queued file before it is evicted unsent. Not readonly so tests can
		/// shrink it rather than waiting real days.
		/// </summary>
		internal static TimeSpan MaxQueuedAge = TimeSpan.FromDays(30);

		private static readonly ISet<Type> TransientIoExceptionTypes = new HashSet<Type>
		{
			typeof(IOException),
			typeof(UnauthorizedAccessException)
		};

		private static string s_outboxDirectoryOverride;

		private static string OutboxDirectory => s_outboxDirectoryOverride ??
			Path.Combine(FwDirectoryFinder.UserAppDataFolder("FieldWorks"), "Analytics", "Outbox");

		/// <summary>
		/// Test seam: <see cref="DesktopAnalytics.Analytics"/> is a hard, process-wide singleton
		/// (its constructor throws "You can only construct a single Analytics object" if called
		/// twice, and <see cref="DesktopAnalytics.Analytics.AllowTracking"/> can never change once
		/// set) - so tests cannot exercise both a consenting and a non-consenting scenario against
		/// the real class in one test run. When set, this overrides <see cref="Analytics.AllowTracking"/>.
		/// </summary>
		internal static Func<bool> AllowTrackingOverride;

		/// <summary>
		/// Test seam: overrides the real <see cref="NetworkInterface.GetIsNetworkAvailable"/>
		/// check so tests aren't at the mercy of the test machine's actual connectivity.
		/// </summary>
		internal static Func<bool> NetworkAvailableOverride;

		/// <summary>
		/// Test seam: overrides the real call to <see cref="DesktopAnalytics.Analytics.Track"/>
		/// that would otherwise happen at the end of a successful flush, so tests can observe what
		/// would have been sent (and simulate a delivery failure by throwing) without a live
		/// Analytics singleton or network access.
		/// </summary>
		internal static Action<string, Dictionary<string, string>> TrackDeliveryOverride;

		private static bool AllowTracking => AllowTrackingOverride?.Invoke() ?? Analytics.AllowTracking;
		private static bool NetworkAvailable => NetworkAvailableOverride?.Invoke() ?? NetworkInterface.GetIsNetworkAvailable();

		private static void DeliverViaAnalytics(string eventName, Dictionary<string, string> properties)
		{
			if (TrackDeliveryOverride != null)
				TrackDeliveryOverride(eventName, properties);
			else
				Analytics.Track(eventName, properties);
		}

		/// <summary>Test seam: points the outbox at a temp directory instead of the real per-user folder.</summary>
		internal static void SetOutboxDirectoryForTests(string directory)
		{
			s_outboxDirectoryOverride = directory;
		}

		/// <summary>Test seam: restores the default (real) outbox directory and clears all overrides.</summary>
		internal static void ResetOutboxDirectoryForTests()
		{
			s_outboxDirectoryOverride = null;
			AllowTrackingOverride = null;
			NetworkAvailableOverride = null;
			TrackDeliveryOverride = null;
			MaxQueuedFiles = 2000;
			MaxQueuedAge = TimeSpan.FromDays(30);
			OrphanClaimStaleness = TimeSpan.FromMinutes(5);
		}

		/// <summary>
		/// Durable replacement for <see cref="Analytics.Track(string, Dictionary{string, string})"/>.
		/// Does nothing if the user has not consented to analytics.
		/// </summary>
		public static void Track(string eventName, Dictionary<string, string> properties)
		{
			Enqueue(TrackEventKind, eventName, properties);
		}

		/// <summary>
		/// Durable replacement for <see cref="Analytics.ReportException(Exception, Dictionary{string, string})"/>.
		/// Does nothing if the user has not consented to analytics.
		/// </summary>
		public static void ReportException(Exception exception, Dictionary<string, string> moreProperties)
		{
			var properties = moreProperties == null
				? new Dictionary<string, string>()
				: new Dictionary<string, string>(moreProperties);
			properties["Message"] = exception.Message;
			properties["Stack Trace"] = exception.StackTrace ?? string.Empty;
			Enqueue(ExceptionEventKind, exception.GetType().FullName, properties);
		}

		/// <summary>
		/// Sweeps the outbox in the background (fire-and-forget). Intended for the startup flush,
		/// which is what actually delivers everything accumulated while offline.
		/// </summary>
		public static void FlushInBackground()
		{
			Task.Run(() => Flush(null));
		}

		/// <summary>
		/// Sweeps the outbox synchronously, but bounded by <paramref name="timeout"/> so it never
		/// materially delays application shutdown. Any events not reached before the timeout are
		/// left in place for the next startup sweep.
		/// </summary>
		public static void FlushSync(TimeSpan timeout)
		{
			Flush(DateTime.UtcNow + timeout);
		}

		private static void Enqueue(string kind, string eventName, Dictionary<string, string> properties)
		{
			if (!AllowTracking)
				return;

			var outboxDirectory = OutboxDirectory;
			// RobustIO has no "create if missing" helper - RequireThatDirectoryExists asserts
			// existence rather than creating it (confirmed by decompiling SIL.Core), so this
			// (idempotent, no-op if already present) is the correct call, not that one.
			Directory.CreateDirectory(outboxDirectory);

			var entry = new OutboxEntry
			{
				Kind = kind,
				EventName = eventName,
				Properties = properties ?? new Dictionary<string, string>(),
				QueuedAtUtc = DateTime.UtcNow
			};

			var fileName = $"{entry.QueuedAtUtc.Ticks:D19}_{Guid.NewGuid():N}.json";
			var path = Path.Combine(outboxDirectory, fileName);
			RetryUtility.Retry(
				() => RobustFile.WriteAllText(path, JsonConvert.SerializeObject(entry)),
				exceptionTypesToRetry: TransientIoExceptionTypes,
				memo: "AnalyticsOutbox.Enqueue");

			// Best-effort immediate delivery attempt so the common (online) case still delivers
			// with negligible added latency; the startup/shutdown sweeps are the durability
			// backstop, not the primary path.
			Task.Run(() => ProcessOutboxFile(path));
		}

		private static void Flush(DateTime? deadlineUtc)
		{
			var outboxDirectory = OutboxDirectory;
			if (!Directory.Exists(outboxDirectory))
				return;

			EvictBeyondCap(outboxDirectory);

			IEnumerable<string> pending;
			try
			{
				pending = RobustIO.EnumerateFilesInDirectory(outboxDirectory, "*.json")
					.Concat(RobustIO.EnumerateFilesInDirectory(outboxDirectory, "*.json" + InflightSuffix))
					.OrderBy(p => Path.GetFileName(p), StringComparer.Ordinal)
					.ToList();
			}
			catch (DirectoryNotFoundException)
			{
				return;
			}

			foreach (var path in pending)
			{
				if (deadlineUtc.HasValue && DateTime.UtcNow >= deadlineUtc.Value)
					break;
				ProcessOutboxFile(path);
			}
		}

		private static void ProcessOutboxFile(string path)
		{
			if (path.EndsWith(InflightSuffix, StringComparison.Ordinal))
			{
				ProcessInflightFile(path);
				return;
			}

			if (!RobustFile.Exists(path))
				return; // already claimed/handled by another process or a previous pass

			if (!AllowTracking)
			{
				// Consent has been revoked since this was queued - fail closed on privacy:
				// drop it rather than send data the user no longer consents to.
				TryDelete(path);
				return;
			}

			if (!NetworkAvailable)
				return; // known gap (design.md D12): this only proves *a* network exists, not that Mixpanel is reachable

			if (!TryClaimExclusive(path, InflightSuffix, out var claimedPath, out var claimedStream))
				return; // another thread/process already claimed (or removed) this exact file first

			DeliverClaimedFile(claimedStream, claimedPath, path);
		}

		/// <summary>
		/// Handles a file already sitting in the claimed (".inflight") state when a sweep finds
		/// it. This could be a crash orphan, or - just as likely within one process - another
		/// thread's flush that is still legitimately, actively delivering it right now. Those two
		/// cases are indistinguishable from the filename alone, so this gates on a staleness
		/// check: only a claim old enough that no realistic delivery would still be in progress is
		/// treated as abandoned. (A naive "any .inflight file is fair game" rule was tried first
		/// and caused a real, test-caught double delivery between two concurrent flushes.)
		/// </summary>
		private static void ProcessInflightFile(string path)
		{
			DateTime claimedAtUtc;
			try
			{
				claimedAtUtc = RobustFile.GetLastWriteTimeUtc(path);
			}
			catch (IOException)
			{
				return; // gone already - another thread/process finished with it
			}

			if (!RobustFile.Exists(path))
				return; // gone already - GetLastWriteTimeUtc doesn't throw for a missing file (see D14)

			if (DateTime.UtcNow - claimedAtUtc < OrphanClaimStaleness)
				return; // too fresh to safely assume orphaned; a legitimate claim may still be in progress

			// Stale enough to treat as abandoned. Still claim it via the same rename+exclusive-open
			// pattern before delivering, so two sweeps recovering the same stale orphan at the
			// same moment can't both deliver it. On a delivery failure this is restored all the
			// way back to the original, non-suffixed name (not left as ".recovering"), so the
			// next sweep re-enters the normal, single-claim path rather than growing the suffix
			// further. Known, accepted residual gap: if the process crashes again inside this
			// narrow recovery window itself, that one file could be left permanently as
			// ".recovering" until manually cleared - considered acceptable given how rare two
			// crashes in the same delivery attempt would be.
			var originalPath = path.Substring(0, path.Length - InflightSuffix.Length);
			if (!TryClaimExclusive(path, RecoveringSuffix, out var claimedPath, out var claimedStream))
				return;

			DeliverClaimedFile(claimedStream, claimedPath, originalPath);
		}

		/// <summary>
		/// Claims <paramref name="path"/> for exclusive delivery by this caller. First renames it
		/// to <paramref name="path"/> + <paramref name="suffix"/> (relocating it out of the name a
		/// future sweep would otherwise re-discover), then opens the result with
		/// <see cref="FileShare.None"/> as the actual single-owner guarantee.
		/// </summary>
		/// <remarks>
		/// The rename's own success/failure is NOT a reliable exclusivity signal on its own:
		/// verified empirically (see design.md D14) that two threads racing an identical
		/// (source, destination) pair through <see cref="RobustFile.Move(string,string)"/> /
		/// <see cref="System.IO.File.Move(string,string)"/> can BOTH return without throwing, even
		/// though the OS performs exactly one physical rename - this class's own concurrent-flush
		/// test caught real double delivery caused by trusting that signal alone. A
		/// <see cref="FileShare.None"/> open, by contrast, is a real OS-enforced lock: only one
		/// caller can ever hold it, which is what the returned <paramref name="claimedStream"/> is
		/// used for.
		/// </remarks>
		private static bool TryClaimExclusive(string path, string suffix, out string claimedPath, out FileStream claimedStream)
		{
			claimedPath = path + suffix;
			claimedStream = null;
			try
			{
				RobustFile.Move(path, claimedPath);
			}
			catch (IOException)
			{
				// Covers FileNotFoundException too: another process (or a previous pass)
				// already claimed or removed this file.
				return false;
			}
			catch (UnauthorizedAccessException)
			{
				return false;
			}

			try
			{
				claimedStream = new FileStream(claimedPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
				return true;
			}
			catch (IOException)
			{
				// Someone else's Move "succeeded" against the same physical file (see remarks) and
				// already holds the real, exclusive lock - or has already finished and deleted it.
				return false;
			}
			catch (UnauthorizedAccessException)
			{
				return false;
			}
		}

		private static void DeliverClaimedFile(FileStream claimedStream, string claimedPath, string originalPath)
		{
			if (!AllowTracking)
			{
				claimedStream.Dispose();
				TryDelete(claimedPath);
				return;
			}

			try
			{
				string content;
				// The StreamReader owns claimedStream and disposes it (releasing the exclusive
				// lock) as soon as the read completes, whether or not this throws.
				using (var reader = new StreamReader(claimedStream, Encoding.UTF8, true, 1024))
					content = reader.ReadToEnd();

				var entry = JsonConvert.DeserializeObject<OutboxEntry>(content);
				if (entry.Kind == ExceptionEventKind)
				{
					// Replayed via Track, not ReportException: replaying through ReportException
					// would count a backlog of queued exceptions against this run's own live
					// MAX_EXCEPTION_REPORTS_PER_RUN budget (design.md D8).
					DeliverViaAnalytics(QueuedExceptionReportEventName, entry.Properties);
				}
				else
				{
					DeliverViaAnalytics(entry.EventName, entry.Properties);
				}
				TryDelete(claimedPath);
			}
			catch
			{
				// Couldn't even attempt delivery (e.g. deserialization failure) - restore the
				// original name so a later sweep retries it, rather than losing it silently.
				TryMove(claimedPath, originalPath);
			}
		}

		private static void EvictBeyondCap(string outboxDirectory)
		{
			List<string> files;
			try
			{
				files = RobustIO.EnumerateFilesInDirectory(outboxDirectory, "*.json")
					.OrderBy(p => Path.GetFileName(p), StringComparer.Ordinal)
					.ToList();
			}
			catch (DirectoryNotFoundException)
			{
				return;
			}

			var cutoffUtc = DateTime.UtcNow - MaxQueuedAge;
			foreach (var path in files.ToList())
			{
				if (TryGetQueuedAtUtc(Path.GetFileName(path), out var queuedAtUtc) && queuedAtUtc < cutoffUtc)
				{
					TryDelete(path);
					files.Remove(path);
				}
			}

			var excess = files.Count - MaxQueuedFiles;
			for (var i = 0; i < excess; i++)
				TryDelete(files[i]);
		}

		private static bool TryGetQueuedAtUtc(string fileName, out DateTime queuedAtUtc)
		{
			queuedAtUtc = default(DateTime);
			var ticksPart = Path.GetFileNameWithoutExtension(fileName).Split('_')[0];
			if (!long.TryParse(ticksPart, out var ticks))
				return false;
			try
			{
				queuedAtUtc = new DateTime(ticks, DateTimeKind.Utc);
				return true;
			}
			catch (ArgumentOutOfRangeException)
			{
				return false;
			}
		}

		private static void TryDelete(string path)
		{
			try
			{
				RobustFile.Delete(path);
			}
			catch (IOException) { }
			catch (UnauthorizedAccessException) { }
		}

		private static void TryMove(string sourcePath, string destinationPath)
		{
			try
			{
				RobustFile.Move(sourcePath, destinationPath, true);
			}
			catch (IOException) { }
			catch (UnauthorizedAccessException) { }
		}

		private class OutboxEntry
		{
			public string Kind { get; set; }
			public string EventName { get; set; }
			public Dictionary<string, string> Properties { get; set; }
			public DateTime QueuedAtUtc { get; set; }
		}
	}
}
