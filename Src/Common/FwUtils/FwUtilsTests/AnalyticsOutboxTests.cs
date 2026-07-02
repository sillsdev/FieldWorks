// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Tests for <see cref="AnalyticsOutbox"/>. These exercise the outbox's own logic entirely
	/// through its test seams (<see cref="AnalyticsOutbox.AllowTrackingOverride"/>,
	/// <see cref="AnalyticsOutbox.NetworkAvailableOverride"/>,
	/// <see cref="AnalyticsOutbox.TrackDeliveryOverride"/>) rather than the real
	/// <c>DesktopAnalytics.Analytics</c> class, because that class is a hard, process-wide
	/// singleton (its constructor throws if called more than once per process, and
	/// <c>AllowTracking</c> can never change once set) - so a single test run could never
	/// otherwise exercise both a consenting and a non-consenting scenario.
	/// See openspec/changes/telemetry-migration-baseline/ for the design this verifies.
	/// </summary>
	// AnalyticsOutbox's test seams (AllowTrackingOverride, NetworkAvailableOverride,
	// TrackDeliveryOverride, MaxQueuedFiles, MaxQueuedAge, OrphanClaimStaleness) are static,
	// process-wide state. VSTest's NUnit adapter runs fixtures on multiple parallel workers
	// by default (Test.runsettings: NumberOfTestWorkers=0/auto), so without this attribute
	// these tests race each other's static state and fail intermittently.
	[TestFixture, NonParallelizable]
	public class AnalyticsOutboxTests
	{
		private string m_outboxDir;
		private List<Tuple<string, Dictionary<string, string>>> m_delivered;

		[SetUp]
		public void SetUp()
		{
			m_outboxDir = Path.Combine(Path.GetTempPath(), "AnalyticsOutboxTests_" + Guid.NewGuid().ToString("N"));
			m_delivered = new List<Tuple<string, Dictionary<string, string>>>();

			AnalyticsOutbox.SetOutboxDirectoryForTests(m_outboxDir);
			AnalyticsOutbox.AllowTrackingOverride = () => true;
			// Default to "offline" so Enqueue's own fire-and-forget immediate-delivery attempt
			// never races with a test's assertions; tests that want delivery flip this to true
			// and then call FlushSync themselves, which is deterministic.
			AnalyticsOutbox.NetworkAvailableOverride = () => false;
			AnalyticsOutbox.TrackDeliveryOverride = (name, props) =>
			{
				lock (m_delivered)
					m_delivered.Add(Tuple.Create(name, props));
			};
		}

		[TearDown]
		public void TearDown()
		{
			AnalyticsOutbox.ResetOutboxDirectoryForTests();
			if (Directory.Exists(m_outboxDir))
				Directory.Delete(m_outboxDir, true);
		}

		private class RawEntry
		{
			public string Kind { get; set; }
			public string EventName { get; set; }
			public Dictionary<string, string> Properties { get; set; }
			public DateTime QueuedAtUtc { get; set; }
		}

		private void WriteRawOutboxFile(DateTime queuedAtUtc, string eventName, string suffix = "")
		{
			Directory.CreateDirectory(m_outboxDir);
			var fileName = $"{queuedAtUtc.Ticks:D19}_{Guid.NewGuid():N}.json{suffix}";
			var json = JsonConvert.SerializeObject(new RawEntry
			{
				Kind = "Track",
				EventName = eventName,
				Properties = new Dictionary<string, string>(),
				QueuedAtUtc = queuedAtUtc
			});
			File.WriteAllText(Path.Combine(m_outboxDir, fileName), json);
		}

		[Test]
		public void Track_ConsentOff_DoesNotPersistAnything()
		{
			AnalyticsOutbox.AllowTrackingOverride = () => false;

			AnalyticsOutbox.Track("SomeEvent", new Dictionary<string, string> { { "k", "v" } });

			Assert.That(Directory.Exists(m_outboxDir), Is.False,
				"consent-off must not even create the outbox directory");
		}

		[Test]
		public void Track_ConsentOn_NetworkUnavailable_PersistsFileUndelivered()
		{
			AnalyticsOutbox.Track("SomeEvent", new Dictionary<string, string> { { "k", "v" } });
			Thread.Sleep(50); // let the fire-and-forget immediate-attempt (a no-op offline) settle

			Assert.That(Directory.GetFiles(m_outboxDir, "*.json"), Has.Length.EqualTo(1));
			Assert.That(m_delivered, Is.Empty);
		}

		[Test]
		public void QueuedEvent_DeliveredOnLaterFlush_WhenNetworkBecomesAvailable()
		{
			// Models: event queued while offline, then delivered on a later launch's startup sweep.
			AnalyticsOutbox.Track("SomeEvent", new Dictionary<string, string> { { "k", "v" } });
			Thread.Sleep(50);
			Assert.That(m_delivered, Is.Empty, "must not deliver while offline");

			AnalyticsOutbox.NetworkAvailableOverride = () => true;
			AnalyticsOutbox.FlushSync(TimeSpan.FromSeconds(5));

			Assert.That(m_delivered, Has.Count.EqualTo(1));
			Assert.That(m_delivered[0].Item1, Is.EqualTo("SomeEvent"));
			Assert.That(m_delivered[0].Item2["k"], Is.EqualTo("v"));
			Assert.That(Directory.GetFiles(m_outboxDir, "*.json"), Is.Empty,
				"a delivered file must be removed from the outbox");
		}

		[Test]
		public void Flush_DeliversMultipleQueuedEvents_InFifoOrder()
		{
			for (var i = 0; i < 5; i++)
			{
				AnalyticsOutbox.Track("Event" + i, new Dictionary<string, string>());
				Thread.Sleep(5); // ensure distinct tick-based filenames sort in enqueue order
			}

			AnalyticsOutbox.NetworkAvailableOverride = () => true;
			AnalyticsOutbox.FlushSync(TimeSpan.FromSeconds(5));

			Assert.That(m_delivered.Select(e => e.Item1),
				Is.EqualTo(new[] { "Event0", "Event1", "Event2", "Event3", "Event4" }));
		}

		[Test]
		public void Flush_EvictsOldestFilesFirst_WhenOverCountCap()
		{
			AnalyticsOutbox.MaxQueuedFiles = 2;
			for (var i = 0; i < 4; i++)
			{
				AnalyticsOutbox.Track("Event" + i, new Dictionary<string, string>());
				Thread.Sleep(5);
			}

			// Network stays "unavailable" (default) - this isolates eviction from delivery.
			AnalyticsOutbox.FlushSync(TimeSpan.FromSeconds(5));

			var remaining = Directory.GetFiles(m_outboxDir, "*.json")
				.OrderBy(f => Path.GetFileName(f), StringComparer.Ordinal)
				.Select(f => JsonConvert.DeserializeObject<RawEntry>(File.ReadAllText(f)).EventName)
				.ToList();
			Assert.That(remaining, Is.EqualTo(new[] { "Event2", "Event3" }),
				"only the 2 newest survive a cap of 2; the oldest must be evicted first");
			Assert.That(m_delivered, Is.Empty, "eviction must not attempt delivery of the survivors");
		}

		[Test]
		public void Flush_EvictsFilesOlderThanMaxAge_WithoutDelivering()
		{
			AnalyticsOutbox.MaxQueuedAge = TimeSpan.FromDays(1);
			WriteRawOutboxFile(DateTime.UtcNow.AddDays(-5), "StaleEvent");
			AnalyticsOutbox.Track("FreshEvent", new Dictionary<string, string>());
			Thread.Sleep(50);

			AnalyticsOutbox.NetworkAvailableOverride = () => true;
			AnalyticsOutbox.FlushSync(TimeSpan.FromSeconds(5));

			Assert.That(m_delivered.Select(e => e.Item1), Is.EqualTo(new[] { "FreshEvent" }),
				"the stale event must be evicted rather than delivered");
		}

		[Test]
		public void Flush_RestoresOriginalFileName_WhenDeliveryThrows()
		{
			AnalyticsOutbox.Track("WillFail", new Dictionary<string, string>());
			Thread.Sleep(50);
			AnalyticsOutbox.NetworkAvailableOverride = () => true;
			AnalyticsOutbox.TrackDeliveryOverride = (name, props) =>
				throw new InvalidOperationException("simulated delivery failure");

			AnalyticsOutbox.FlushSync(TimeSpan.FromSeconds(5));

			var files = Directory.GetFiles(m_outboxDir);
			Assert.That(files, Has.Length.EqualTo(1));
			Assert.That(files[0], Does.EndWith(".json"),
				"a failed delivery must restore the original .json name, not leave a stray .inflight file");
		}

		[Test]
		public void Flush_ConcurrentFlushCalls_DeliverEachEventExactlyOnce()
		{
			for (var i = 0; i < 10; i++)
				AnalyticsOutbox.Track("Event" + i, new Dictionary<string, string>());
			Thread.Sleep(50);

			AnalyticsOutbox.NetworkAvailableOverride = () => true;
			// A small delay widens the race window between the two concurrent flushes below, so
			// the claim race is reliably exercised rather than won by luck.
			AnalyticsOutbox.TrackDeliveryOverride = (name, props) =>
			{
				Thread.Sleep(10);
				lock (m_delivered)
					m_delivered.Add(Tuple.Create(name, props));
			};

			var flush1 = Task.Run(() => AnalyticsOutbox.FlushSync(TimeSpan.FromSeconds(5)));
			var flush2 = Task.Run(() => AnalyticsOutbox.FlushSync(TimeSpan.FromSeconds(5)));
			Task.WaitAll(flush1, flush2);

			Assert.That(m_delivered.Select(e => e.Item1).Distinct().Count(), Is.EqualTo(10),
				"every event must eventually be delivered");
			Assert.That(m_delivered, Has.Count.EqualTo(10),
				"the atomic-rename claim must prevent the same event being delivered twice by concurrent flushes");
		}

		[Test]
		public void Flush_DropsQueuedEvent_WhenConsentRevokedBeforeFlush()
		{
			AnalyticsOutbox.Track("ShouldBeDropped", new Dictionary<string, string>());
			Thread.Sleep(50);
			AnalyticsOutbox.AllowTrackingOverride = () => false;
			AnalyticsOutbox.NetworkAvailableOverride = () => true;

			AnalyticsOutbox.FlushSync(TimeSpan.FromSeconds(5));

			Assert.That(m_delivered, Is.Empty);
			Assert.That(Directory.GetFiles(m_outboxDir, "*.json"), Is.Empty,
				"a consent-revoked event must be deleted, not left queued indefinitely");
		}

		[Test]
		public void ReportException_ReplaysAsQueuedExceptionReport_NotTheOriginalExceptionType()
		{
			AnalyticsOutbox.ReportException(new InvalidOperationException("boom"),
				new Dictionary<string, string> { { "extra", "1" } });
			Thread.Sleep(50);

			AnalyticsOutbox.NetworkAvailableOverride = () => true;
			AnalyticsOutbox.FlushSync(TimeSpan.FromSeconds(5));

			Assert.That(m_delivered, Has.Count.EqualTo(1));
			Assert.That(m_delivered[0].Item1, Is.EqualTo("QueuedExceptionReport"),
				"replayed exceptions must go through Track, not the real ReportException path (design.md D8)");
			Assert.That(m_delivered[0].Item2["Message"], Is.EqualTo("boom"));
			Assert.That(m_delivered[0].Item2["extra"], Is.EqualTo("1"));
		}

		[Test]
		public void FlushSync_StopsAtTimeout_LeavingRemainingEventsQueued()
		{
			for (var i = 0; i < 5; i++)
			{
				AnalyticsOutbox.Track("Event" + i, new Dictionary<string, string>());
				Thread.Sleep(5);
			}

			AnalyticsOutbox.NetworkAvailableOverride = () => true;
			AnalyticsOutbox.TrackDeliveryOverride = (name, props) =>
			{
				Thread.Sleep(100);
				lock (m_delivered)
					m_delivered.Add(Tuple.Create(name, props));
			};

			AnalyticsOutbox.FlushSync(TimeSpan.FromMilliseconds(150));

			Assert.That(m_delivered.Count, Is.LessThan(5),
				"a short timeout must stop the sweep before every file is processed");
			Assert.That(Directory.GetFiles(m_outboxDir, "*.json"), Is.Not.Empty,
				"events not reached before the timeout must remain queued for the next sweep");
		}

		[Test]
		public void Flush_DeliversOrphanedInflightFile_LeftBehindByACrashedPriorClaim()
		{
			// Simulates a process that claimed a file (renamed it to .inflight) and then crashed
			// or was killed before finishing delivery. Zeroing the staleness threshold means this
			// (freshly-written-by-the-test) .inflight file is immediately eligible for recovery,
			// isolating the orphan-recovery path from the separate staleness-gating behavior
			// covered by InflightFile_NotYetStale_IsLeftAloneRatherThanRedelivered below.
			AnalyticsOutbox.OrphanClaimStaleness = TimeSpan.Zero;
			WriteRawOutboxFile(DateTime.UtcNow, "Orphaned", suffix: ".inflight");

			AnalyticsOutbox.NetworkAvailableOverride = () => true;
			AnalyticsOutbox.FlushSync(TimeSpan.FromSeconds(5));

			Assert.That(m_delivered.Select(e => e.Item1), Is.EqualTo(new[] { "Orphaned" }));
			Assert.That(Directory.GetFiles(m_outboxDir), Is.Empty);
		}

		[Test]
		public void InflightFile_NotYetStale_IsLeftAloneRatherThanRedelivered()
		{
			// This is the exact scenario that caused a real, test-caught double-delivery bug
			// during development: a fresh .inflight file (a legitimate, still-in-progress claim
			// by another thread/process, not a crash orphan) must not be redelivered by a sweep
			// that merely happens to enumerate it.
			WriteRawOutboxFile(DateTime.UtcNow, "StillBeingDelivered", suffix: ".inflight");

			AnalyticsOutbox.NetworkAvailableOverride = () => true;
			AnalyticsOutbox.FlushSync(TimeSpan.FromSeconds(5));

			Assert.That(m_delivered, Is.Empty,
				"a fresh .inflight claim must be left alone, not treated as an orphan");
			Assert.That(Directory.GetFiles(m_outboxDir, "*.inflight"), Has.Length.EqualTo(1),
				"the file must remain exactly as claimed for whoever is legitimately delivering it");
		}

		[Test]
		public void Track_NullProperties_DoesNotThrow_AndDeliversEmptyPropertiesDictionary()
		{
			Assert.DoesNotThrow(() => AnalyticsOutbox.Track("NullPropsEvent", null));
			Thread.Sleep(50);

			AnalyticsOutbox.NetworkAvailableOverride = () => true;
			AnalyticsOutbox.FlushSync(TimeSpan.FromSeconds(5));

			Assert.That(m_delivered, Has.Count.EqualTo(1));
			Assert.That(m_delivered[0].Item2, Is.Not.Null.And.Empty);
		}

		[Test]
		public void ReportException_NullMoreProperties_DoesNotThrow()
		{
			Assert.DoesNotThrow(() => AnalyticsOutbox.ReportException(new Exception("x"), null));
		}
	}
}
