// Copyright (c) 2021-2021 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Linq;
using SIL.LCModel;
using SIL.LCModel.Utils;
using SIL.Reporting;
using SIL.Settings;
using SIL.Xml;
using Timer = System.Timers.Timer;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Finds and downloads updates to FieldWorks.
	/// </summary>
	public static class FwUpdater
	{
		private const uint BytesPerMiB = 1048576;

		private static FwUpdate Current { get; }

		/// <summary>
		/// The local copy of the selected channel's UpdateInfo.xml, downloaded when checking and kept until we determine FLEx is up to date or until
		/// the user decides whether to install the update. Used to decide whether to offer to install downloaded updates at startup and to determine
		/// if downloaded updates contain model changes.
		/// </summary>
		internal static string LocalUpdateInfoFilePath { get; }

		///// <remarks>so we can check daily even if the user never closes FW. REVIEW (Hasso) 2021.07: would a timer be better?
		///// If we check daily, and we download two updates before the user restarts (unlikely!), should we notify again?</remarks>
		//private static DateTime s_lastCheck;

		// TODO (Hasso) 2021.07: bool or RESTClient to track whether we're presently checking or downloading; clear in `finally`

		static FwUpdater()
		{
			// Entry Assembly is null during unit tests, which also supply their own "current" version info.
			var assembly = Assembly.GetEntryAssembly();
			if (assembly != null)
			{
				var vip = new VersionInfoProvider(assembly, true);
				// Base builds can take precedence over each other by being online or offline. Selecting Patch here will prevent
				// finding an "update" that is really the same version.
				Current = new FwUpdate(vip.NumericAppVersion, Environment.Is64BitProcess, vip.BaseBuildNumber, FwUpdate.Typ.Patch,
					0, LcmCache.ModelVersion.ToString(), FLExBridgeHelper.LiftVersion, FLExBridgeHelper.FlexBridgeDataVersion);
			}
			LocalUpdateInfoFilePath = Path.Combine(FwDirectoryFinder.DownloadedUpdates, "LastCheckUpdateInfo.xml");
		}

		#region check for updates
		/// <summary>
		/// Checks for updates to FieldWorks, if the settings say to. If an update is found,
		/// downloads the update in the background and notifies the user when the download is complete.
		/// </summary>
		/// <param name="ui">to notify the user when an update is ready to install</param>
		public static void CheckForUpdates(ILcmUI ui)
		{
			var updateSettings = new FwApplicationSettings().Update;
			if (updateSettings == null || updateSettings.Behavior == UpdateSettings.Behaviors.DoNotCheck)
			{
				return;
			}
			Logger.WriteEvent("Checking for updates...");
			// Check on a background thread; hitting the Internet on the main thread can be a performance hit
			new Thread(() => Logger.WriteEvent(CheckForUpdatesInternal(ui, updateSettings)))
			{
				IsBackground = true
			}.Start();
		}

		/// <returns>a message for the log</returns>
		private static string CheckForUpdatesInternal(ILcmUI ui, UpdateSettings updateSettings)
		{
			try
			{
				if (!MiscUtils.IsWindows)
				{
					ErrorReport.ReportNonFatalExceptionWithMessage(new ApplicationException(), "Only Windows updates are available here");
					return "ERROR: Only Windows updates are available here";
				}

				var baseURL = "https://downloads.languagetechnology.org/fieldworks/";
				var infoURL = "https://downloads.languagetechnology.org/fieldworks/UpdateInfo{0}.xml";

				switch (updateSettings.Channel)
				{
					case UpdateSettings.Channels.Stable:
						infoURL = string.Format(infoURL, string.Empty);
						break;
					case UpdateSettings.Channels.Beta:
					case UpdateSettings.Channels.Alpha:
						infoURL = string.Format(infoURL, updateSettings.Channel);
						break;
					case UpdateSettings.Channels.Nightly:
						baseURL = "https://flex-updates.s3.amazonaws.com/";
						infoURL = "https://flex-updates.s3.amazonaws.com/?prefix=jobs/FieldWorks-Win-all";
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}

				if (!new DownloadClient().DownloadFile(infoURL, LocalUpdateInfoFilePath))
				{
					return $"Failed to download update info from {infoURL}";
				}
				var available = GetLatestUpdateFrom(Current, XDocument.Load(LocalUpdateInfoFilePath), baseURL);
				if (available == null)
				{
					File.Delete(LocalUpdateInfoFilePath);
					return "Check complete; already up to date";
				}
				Logger.WriteMinorEvent($"Update found at {available.URL}");

				var localFile = Path.Combine(FwDirectoryFinder.DownloadedUpdates, Path.GetFileName(available.URL));
				if(File.Exists(localFile))
				{
					return $"Update already downloaded to {localFile}";
				}

				var tempFile = $"{localFile}.tmp";
				if (new DownloadClient().DownloadFile(available.URL, tempFile))
				{
					File.Move(tempFile, localFile);
				}
				else
				{
					return "Update found, but failed to download";
				}

				NotifyUserOnIdle(ui, GetUpdateMessage(Current, available, FwUtilsStrings.RestartToUpdatePrompt),
					FwUtilsStrings.RestartToUpdateCaption);

				return $"Update downloaded to {localFile}";
			}
			catch (Exception e)
			{
				return $"Got {e.GetType()}: {e.Message}";
			}
		}

		private static void NotifyUserOnIdle(ILcmUI ui, string message, string caption = null)
		{
			var timer = new Timer { SynchronizingObject = ui.SynchronizeInvoke, Interval = 1000 };
			timer.Elapsed += (o, e) =>
			{
				if (DateTime.Now - ui.LastActivityTime < TimeSpan.FromSeconds(12))
					return; // Don't interrupt a user who is busy typing. Wait for a pause to prompt to install updates.

				timer.Stop(); // one notification is enough
				MessageBox.Show(Form.ActiveForm, message, caption);
			};
			timer.Start();
		}

		public static FwUpdate GetLatestUpdateFrom(FwUpdate current, XDocument bucketContents, string bucketURL)
		{
			if (bucketContents.Root == null)
			{
				return null;
			}
			bucketContents.Root.RemoveNamespaces();
			return GetLatestUpdateFrom(current, bucketContents.Root.Elements("Contents").Select(elt => Parse(elt, bucketURL)));
		}
		#endregion check for updates

		#region select updates
		public static FwUpdate GetLatestUpdateFrom(FwUpdate current, IEnumerable<FwUpdate> available)
		{
			FwUpdate latestPatch = null, latestBase = null;
			foreach (var potential in available)
			{
				if (IsPatchOn(latestPatch ?? current, potential))
				{
					latestPatch = potential;
				}

				if (IsNewerBase(latestBase ?? current, potential))
				{
					latestBase = potential;
				}
			}

			return latestBase == null
				? latestPatch
				: latestPatch == null || latestBase.Version > latestPatch.Version
					? latestBase
					: latestPatch;
		}

		/// <param name="current">the currently-installed version</param>
		/// <param name="potential">a potential installer</param>
		/// <returns>true iff potential is a patch that can be installed on top of current</returns>
		internal static bool IsPatchOn(FwUpdate current, FwUpdate potential)
		{
			return potential != null
				&& potential.InstallerType == FwUpdate.Typ.Patch
				&& potential.Is64Bit == current.Is64Bit
				&& potential.BaseBuild == current.BaseBuild
				&& potential.Version > current.Version;
		}

		/// <param name="current">the currently-installed version</param>
		/// <param name="potential">a potential installer</param>
		/// <returns>>true iff potential is a base installer with a greater version than current,
		/// or that takes precedence by being online installer of the same version as current.</returns>
		internal static bool IsNewerBase(FwUpdate current, FwUpdate potential)
		{
			return potential != null
				&& (potential.InstallerType == FwUpdate.Typ.Online || potential.InstallerType == FwUpdate.Typ.Offline)
				&& potential.Is64Bit == current.Is64Bit
				&& (potential.Version > current.Version
					|| potential.Version == current.Version
						&& current.InstallerType == FwUpdate.Typ.Offline
						&& potential.InstallerType == FwUpdate.Typ.Online);
		}

		/// <param name="elt">a &lt;Contents/&gt; element from an S3 bucket list</param>
		/// <param name="baseURL">the https:// URL of the bucket, with the trailing /</param>
		internal static FwUpdate Parse(XElement elt, string baseURL)
		{
			try
			{
				var update = Parse(elt.Element("Key")?.Value, baseURL);
				if (update != null)
				{
					update = AddSizeAndModelVersions(update, elt);
				}

				return update;
			}
			catch (Exception e)
			{
				// ReSharper disable once LocalizableElement (log content)
				Console.WriteLine($"Got {e.GetType()}: {e.Message}");
				return null;
			}
		}

		/// <returns>a new FwUpdate, cloned from <c>update</c>, with size and model version information from <c>elt</c></returns>
		internal static FwUpdate AddSizeAndModelVersions(FwUpdate update, XElement elt)
		{
			// round up to the next MB
			var size = ulong.TryParse(elt.Element("Size")?.Value, out var byteSize)
				? (int)((byteSize + BytesPerMiB - 1) / BytesPerMiB)
				: update.Size;
			var lcmVersion = elt.Element("LCModelVersion")?.Value;
			var liftVersion = elt.Element("LIFTModelVersion")?.Value;
			var fbDataVersion = elt.Element("FlexBridgeDataVersion")?.Value;
			update = new FwUpdate(update, size, lcmVersion, liftVersion, fbDataVersion);
			return update;
		}

		/// <param name="key">the filename and possibly part or all of the URI</param>
		/// <param name="baseURI">the https:// URL of the bucket, with the trailing '/'. Full URI = baseURI + key</param>
		internal static FwUpdate Parse(string key, string baseURI)
		{
			try
			{
				// Key will be something like
				// jobs/FieldWorks-Win-all-Base/312/FieldWorks_9.0.11.1_Online_x64.exe
				// jobs/FieldWorks-Win-all-Patch/10/FieldWorks_9.0.14.10_b312_x64.msp
				// 9.0.15/FieldWorks_9.0.16.128_b312_x64.msp
				// 9.0.15/312/FieldWorks_9.0.15.1_Online_x64.exe
				var keyParts = Path.GetFileName(key)?.Split('_');
				if (keyParts?.Length != 4)
				{
					return null;
				}

				var extension = Path.GetExtension(keyParts[3]);
				if (!extension.Equals(".msp") && !extension.Equals(".exe"))
				{
					return null;
				}

				var isBaseBuild = !extension.Equals(".msp");
				var baseBuild = isBaseBuild
					? ParseBuildNumber(key)
					: int.Parse(keyParts[2].Substring(1));
				var installerType = isBaseBuild
					? keyParts[2].Equals("Offline") ? FwUpdate.Typ.Offline : FwUpdate.Typ.Online
					: FwUpdate.Typ.Patch;

				return new FwUpdate(keyParts[1], keyParts[3].StartsWith("x64."), baseBuild, installerType, url: $"{baseURI}{key}");
			}
			catch (Exception e)
			{
				// ReSharper disable once LocalizableElement (log content)
				Console.WriteLine($"Got {e.GetType()} parsing {key}: {e.Message}");
				return null;
			}
		}

		/// <returns>the build number from a base build key, or 0 if the build number could not be determined</returns>
		private static int ParseBuildNumber(string baseKey)
		{
			var parts = baseKey.Split('/');
			return parts.Length > 1 && int.TryParse(parts[parts.Length - 2], out var baseBuild)
				? baseBuild
				: 0;
		}
		#endregion select updates

		internal static string GetUpdateMessage(FwUpdate current, FwUpdate update, string actionPrompt)
		{
			var messageParts = new List<string>(4) { FwUtilsStrings.UpdateDownloadedVersionYCurrentX };
			if (HasVersionChanged(current.LCModelVersion, update.LCModelVersion))
			{
				messageParts.Add(FwUtilsStrings.ModelChangeLCM);
			}
			// If FLEx Bridge is not installed, the current FB data version will be null, and users won't care about S/R compatibility
			else if (current.FlexBridgeDataVersion != null && HasVersionChanged(current.FlexBridgeDataVersion, update.FlexBridgeDataVersion))
			{
				messageParts.Add(FwUtilsStrings.ModelChangeFBButNotFW);
			}

			if (HasVersionChanged(current.LIFTModelVersion, update.LIFTModelVersion))
			{
				messageParts.Add(FwUtilsStrings.ModelChangeLIFT);
			}
			messageParts.Add(actionPrompt);
			return string.Format(string.Join($"{Environment.NewLine}{Environment.NewLine}", messageParts), current.Version, update.Version);
		}

		private static bool HasVersionChanged(string curVer, string newVer)
		{
			// If the new version is null, we didn't read it from the XML (Nightly downloads, or didn't find a matching XML entry
			// for a file that is already downloaded). Don't warn users or testers about something that's probably not true.
			return !newVer?.Equals(curVer) ?? false;
		}

		#region install updates
		/// <summary>
		/// If an update has been downloaded and the user wants to, install it
		/// </summary>
		public static void InstallDownloadedUpdate()
		{
			var latestPatch = GetLatestDownloadedUpdate(Current);
			if (latestPatch == null || DialogResult.Yes != MessageBox.Show(
				GetUpdateMessage(Current, latestPatch, FwUtilsStrings.UpdateNowPrompt),
				FwUtilsStrings.UpdateNowCaption, MessageBoxButtons.YesNo))
			{
				return;
			}

			var latestPatchFile = latestPatch.URL;
			var installerRunner = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles), "SIL", "ProcRunner_5.0.exe");
			if (!File.Exists(installerRunner))
			{
				MessageBox.Show(string.Format(FwUtilsStrings.CannotRestartAutomaticallyMessage, latestPatchFile),
					FwUtilsStrings.CouldNotUpdateAutomaticallyCaption, MessageBoxButtons.OK, MessageBoxIcon.Error);
				Process.Start(latestPatchFile, "/passive");
				Environment.Exit(1);
			}
			installerRunner = installerRunner.Replace(@"\", @"\\");
			Logger.WriteEvent($"Installing {latestPatchFile} using {installerRunner}");

			try
			{
				var exeToRestart = Assembly.GetEntryAssembly();
				var info = new ProcessStartInfo
				{
					FileName = installerRunner,
					UseShellExecute = false,
					CreateNoWindow = true,
					RedirectStandardError = true,
					RedirectStandardInput = true,
					RedirectStandardOutput = true,
					// ReSharper disable once PossibleNullReferenceException
					Arguments = $"\"{latestPatchFile}\" \"{exeToRestart.Location}\""
				};
				Process.Start(info);
				Environment.Exit(0);
			}
			catch (Exception e)
			{
				Logger.WriteError(e);
				MessageBox.Show(string.Format(FwUtilsStrings.CouldNotUpdateAutomaticallyFileXMessage, latestPatchFile),
					FwUtilsStrings.CouldNotUpdateAutomaticallyCaption, MessageBoxButtons.OK, MessageBoxIcon.Error);
				throw;
			}
		}

		/// <summary>
		/// Returns the latest fully-downloaded update that can be installed directly on this version of FW.
		/// Returns null if none is found, or if there is no UpdateInfo.xml present.
		/// </summary>
		internal static FwUpdate GetLatestDownloadedUpdate(FwUpdate current)
		{
			if (!FileUtils.FileExists(LocalUpdateInfoFilePath))
				return null;
			var result = GetLatestUpdateFrom(current,
				FileUtils.GetFilesInDirectory(FwDirectoryFinder.DownloadedUpdates).Select(file => Parse(file, string.Empty)));
			// Deleting the info file ensures that we don't offer to install updates until after the next check (LT-20774)
			FileUtils.Delete(LocalUpdateInfoFilePath);
			return result;
		}
		#endregion install updates
	}

	/// <summary>Represents an update that can be downloaded</summary>
	public class FwUpdate
	{
		/// <summary/>
		public enum Typ { Patch, Offline, Online }

		/// <summary/>
		public Version Version { get; }

		/// <summary/>
		public bool Is64Bit { get; }

		/// <summary/>
		public int BaseBuild { get; }

		/// <summary/>
		public Typ InstallerType { get; }

		/// <summary>Size in MB of the file to be downloaded</summary>
		public int Size { get; }

		/// <summary/>
		// ReSharper disable once InconsistentNaming
		public string LCModelVersion { get; }

		/// <summary/>
		public string LIFTModelVersion { get; }

		/// <summary/>
		public string FlexBridgeDataVersion { get; }

		/// <summary/>
		public string URL { get; }

		/// <summary/>
		internal FwUpdate(FwUpdate toCopy, int size, string lcmVersion, string liftModelVersion, string flexBridgeDataVersion)
			: this(toCopy.Version, toCopy.Is64Bit, toCopy.BaseBuild, toCopy.InstallerType, size,
				lcmVersion, liftModelVersion, flexBridgeDataVersion, toCopy.URL)
		{
		}

		/// <summary/>
		public FwUpdate(string version, bool is64Bit, int baseBuild, Typ installerType, int size = 0,
			string lcmVersion = null, string liftModelVersion = null, string flexBridgeDataVersion = null, string url = null)
			: this(new Version(version), is64Bit, baseBuild, installerType, size, lcmVersion, liftModelVersion, flexBridgeDataVersion, url)
		{
		}

		/// <summary/>
		public FwUpdate(Version version, bool is64Bit, int baseBuild, Typ installerType, int size = 0,
			string lcmVersion = null, string liftModelVersion = null, string flexBridgeDataVersion = null, string url = null)
		{
			Version = version;
			Is64Bit = is64Bit;
			BaseBuild = baseBuild;
			InstallerType = installerType;
			Size = size;
			LCModelVersion = lcmVersion;
			LIFTModelVersion = liftModelVersion;
			FlexBridgeDataVersion = flexBridgeDataVersion;
			URL = url;
		}
	}
}