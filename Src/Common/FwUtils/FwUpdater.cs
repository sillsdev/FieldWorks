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

		private static FwUpdate Current
		{
			get
			{
				var vip = new VersionInfoProvider(Assembly.GetEntryAssembly(), true);
				return new FwUpdate(vip.NumericAppVersion, Environment.Is64BitProcess, vip.BaseBuildNumber, FwUpdate.Typ.Offline);
			}
		}

		///// <remarks>so we can check daily even if the user never closes FW. REVIEW (Hasso) 2021.07: would a timer be better?
		///// If we check daily, and we download two updates before the user restarts (unlikely!), should we notify again?</remarks>
		//private static DateTime s_lastCheck;

		// TODO (Hasso) 2021.07: bool or RESTClient to track whether we're presently checking or downloading; clear in `finally`

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

				var available = GetLatestUpdateFrom(Current, XDocument.Load(infoURL), baseURL);
				// ENHANCE (Hasso) 2021.07: catch WebEx and try again in a minute
				if (available == null)
				{
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

				NotifyUserOnIdle(ui,
					string.Format(FwUtilsStrings.UpdateDownloadedVersionXCurrentXPromptX, available.Version, Current.Version, FwUtilsStrings.RestartToUpdatePrompt),
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
			return GetLatestUpdateFrom(current,
				bucketContents.Root.Elements().Where(elt => elt.Name.LocalName.Equals("Contents")).Select(elt => Parse(elt, bucketURL)));
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
		/// <returns>true iff potential is a(n online) base installer with a greater version than current</returns>
		internal static bool IsNewerBase(FwUpdate current, FwUpdate potential)
		{
			return potential != null
				&& potential.InstallerType == FwUpdate.Typ.Online
				&& potential.Is64Bit == current.Is64Bit
				&& potential.Version > current.Version;
		}

		/// <param name="elt">a &lt;Contents/&gt; element from an S3 bucket list</param>
		/// <param name="baseURL">the https:// URL of the bucket, with the trailing /</param>
		internal static FwUpdate Parse(XElement elt, string baseURL)
		{
			try
			{
				var update = Parse(elt.Element("Key")?.Value, baseURL);
				if (update != null && ulong.TryParse(elt.Element("Size")?.Value, out var byteSize))
				{
					// round up to the next MB
					var size = (int)((byteSize + BytesPerMiB - 1) / BytesPerMiB);
					update = new FwUpdate(update.Version, update.Is64Bit, update.BaseBuild, update.InstallerType, size, update.URL);
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

		/// <param name="key"></param>
		/// <param name="baseURI">the https:// URL of the bucket, with the trailing /</param>
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

		#region install updates
		/// <summary>
		/// If an update has been downloaded and the user wants to, install it
		/// </summary>
		public static void InstallDownloadedUpdate()
		{
			var updateSettings = new FwApplicationSettings().Update;
			if (updateSettings == null || updateSettings.Behavior == UpdateSettings.Behaviors.DoNotCheck)
			{
				// TODO (Hasso) 2021.08: whenever we implement check on demand, we will need to offer the update once per check (LT-20774, LT-19171)
				return;
			}
			var latestPatch = GetLatestDownloadedUpdate(Current, FwDirectoryFinder.DownloadedUpdates);
			if (latestPatch == null || DialogResult.Yes != MessageBox.Show(
				string.Format(FwUtilsStrings.UpdateDownloadedVersionXCurrentXPromptX, latestPatch.Version, Current.Version, FwUtilsStrings.UpdateNowPrompt),
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
		/// Returns the latest fully-downloaded patch that can be installed to upgrade this version of FW
		/// </summary>
		internal static FwUpdate GetLatestDownloadedUpdate(FwUpdate current, string downloadsDir)
		{
			var dirInfo = new DirectoryInfo(downloadsDir);
			if (!dirInfo.Exists)
				return null;
			return GetLatestUpdateFrom(current, dirInfo.EnumerateFiles()
				.Select(fi => Parse(fi.Name, $"{fi.DirectoryName}{Path.DirectorySeparatorChar}")));
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
		public string URL { get; }

		/// <summary/>
		public FwUpdate(string version, bool is64Bit, int baseBuild, Typ installerType, int size = -1, string url = null)
			: this(new Version(version), is64Bit, baseBuild, installerType, size, url)
		{
		}

		/// <summary/>
		public FwUpdate(Version version, bool is64Bit, int baseBuild, Typ installerType, int size = 0, string url = null)
		{
			Version = version;
			Is64Bit = is64Bit;
			BaseBuild = baseBuild;
			InstallerType = installerType;
			Size = size;
			URL = url;
		}
	}
}