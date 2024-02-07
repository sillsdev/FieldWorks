// Copyright (c) 2021-2021 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Linq;
using SIL.LCModel;
using SIL.LCModel.Utils;
using SIL.PlatformUtilities;
using SIL.Reporting;
using SIL.Settings;
using SIL.Windows.Forms;
using SIL.Windows.Forms.Reporting;
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
		private static FwUpdate CurrentFlexBridge { get; }

		/// <summary>
		/// The local copy of the selected channel's UpdateInfo.xml, downloaded when checking and kept until we determine FLEx is up to date or until
		/// the user decides whether to install the update. Used to decide whether to offer to install downloaded updates at startup and to determine
		/// if downloaded updates contain model changes.
		/// </summary>
		internal static string LocalUpdateInfoFilePath(bool isFlexBridge) => isFlexBridge ? LocalFBUpdateInfoFilePath : LocalFWUpdateInfoFilePath;
		private static readonly string LocalFWUpdateInfoFilePath;
		private static readonly string LocalFBUpdateInfoFilePath;

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
					0, vip.ApparentBuildDate, LcmCache.ModelVersion.ToString(), FLExBridgeHelper.LiftVersion, FLExBridgeHelper.FlexBridgeDataVersion);
				CurrentFlexBridge = new FwUpdate(FLExBridgeHelper.FlexBridgeVersion, false, 0, FwUpdate.Typ.Patch,
					flexBridgeDataVersion: FLExBridgeHelper.FlexBridgeDataVersion);
			}
			LocalFWUpdateInfoFilePath = Path.Combine(FwDirectoryFinder.DownloadedUpdates, "LastCheckUpdateInfo.xml");
			LocalFBUpdateInfoFilePath = Path.Combine(FwDirectoryFinder.DownloadedUpdates, "LastCheckFLExBridgeUpdateInfo.xml");
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
			Logger.WriteEvent("Checking for FieldWorks updates...");
			// Only testers will access nightly and testing updates, so we can call more attention to errors
			if (updateSettings.Channel == UpdateSettings.Channels.Nightly || updateSettings.Channel == UpdateSettings.Channels.Testing)
			{
				ExceptionHandler.Init(new WinFormsExceptionHandler());
			}
			// Check on a background thread; hitting the Internet on the main thread can be a performance hit
			new Thread(() => Logger.WriteEvent(CheckForUpdatesInternal(ui, updateSettings)))
			{
				IsBackground = true
			}.Start();
		}

		/// <returns>a message for the log</returns>
		private static void CheckForFlexBridgeUpdates(ILcmUI ui, UpdateSettings updateSettings)
		{
			if (CurrentFlexBridge.Version == null)
			{
				Logger.WriteMinorEvent("FLEx Bridge not installed; not checking for FLEx Bridge updates.");
				return;
			}
			Logger.WriteEvent("Checking for FLEx Bridge updates...");
			new Thread(() => Logger.WriteEvent(CheckForUpdatesInternal(ui, updateSettings, true)))
			{
				IsBackground = true
			}.Start();
		}

		/// <returns>a message for the log</returns>
		private static string CheckForUpdatesInternal(ILcmUI ui, UpdateSettings updateSettings, bool isFlexBridge = false)
		{
			var alreadyUpToDate = false;
			try
			{
				if (!Platform.IsWindows)
				{
					return "ERROR: Only Windows updates are available here";
				}

				var baseURL = "https://downloads.languagetechnology.org/fieldworks/";
				var infoURL = "https://downloads.languagetechnology.org/fieldworks/UpdateInfo{0}.xml";
				var isNightly = false;

				switch (updateSettings.Channel)
				{
					case UpdateSettings.Channels.Stable:
						infoURL = string.Format(infoURL, string.Empty);
						break;
					case UpdateSettings.Channels.Beta:
					case UpdateSettings.Channels.Alpha:
					case UpdateSettings.Channels.Testing:
						infoURL = string.Format(infoURL, updateSettings.Channel);
						break;
					case UpdateSettings.Channels.Nightly:
						baseURL = "https://flex-updates.s3.amazonaws.com/";
						infoURL = "https://flex-updates.s3.amazonaws.com/?prefix=jobs/FieldWorks-Win-all";
						isNightly = true;
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}

				if (isFlexBridge)
				{
					baseURL = "https://software.sil.org/downloads/r/fieldworks/";
					infoURL = "https://downloads.languagetechnology.org/flexbridge/UpdateInfoFLExBridge.xml";
					// Since FLEx Bridge is not installed earlier in the startup process, its UpdateInfo.xml may still be present.
					// Attempting to download to a fully-downloaded file results in an out of range exception from the server.
					// Delete the file so it can be downloaded again (in case there are newer updates).
					FileUtils.Delete(LocalFBUpdateInfoFilePath);
				}

				if (isNightly)
				{
					// LT-20875: WebClient requires the directory to exist before downloading to it. (DownloadClient creates directories itself)
					FileUtils.EnsureDirectoryExists(FwDirectoryFinder.DownloadedUpdates);
					// Use WebClient for nightly builds because DownloadClient can't download the dynamically built update info (LT-20819).
					// DownloadClient is still best for all other channels because it is better for unstable internet.
					new WebClient().DownloadFile(infoURL, LocalUpdateInfoFilePath(isFlexBridge));
				}
				else if (!new DownloadClient().DownloadFile(infoURL, LocalUpdateInfoFilePath(isFlexBridge)))
				{
					return $"Failed to download update info from {infoURL}";
				}

				var infoDoc = XDocument.Load(LocalUpdateInfoFilePath(isFlexBridge));
				GetBaseUrlFromUpdateInfo(infoDoc, ref baseURL);

				var available = GetLatestUpdateFrom(isFlexBridge ? CurrentFlexBridge : Current, infoDoc, baseURL,
					isNightly || updateSettings.Channel == UpdateSettings.Channels.Testing);
				if (available == null)
				{
					File.Delete(LocalUpdateInfoFilePath(isFlexBridge));
					alreadyUpToDate = true;
					return $"Check complete; {(isFlexBridge ? "FLEx Bridge" : "FieldWorks")} is already up to date";
				}
				Logger.WriteMinorEvent($"Update found at {available.URL}");

				var localFile = Path.Combine(FwDirectoryFinder.DownloadedUpdates, Path.GetFileName(available.URL));
				if (!File.Exists(localFile))
				{
					var tempFile = $"{localFile}.tmp";
					if (new DownloadClient().DownloadFile(available.URL, tempFile))
					{
						File.Move(tempFile, localFile);
					}
					else
					{
						return "Update found, but failed to download";
					}
				}
				else if(isFlexBridge)
				{
					// If a FLEx Bridge update is already downloaded, continue to install it (these are not installed on FLEx startup)
					Logger.WriteEvent($"Update already downloaded to {localFile}");
				}
				else
				{
					// If a FLEx update is already downloaded, the user probably declined to install it on startup
					return $"Update already downloaded to {localFile}";
				}

				if (isFlexBridge)
				{
					NotifyUserOnIdle(ui, () =>
					{
						if (DialogResult.Yes == FlexibleMessageBox.Show(Form.ActiveForm,
							GetUpdateMessage(FwUtilsStrings.UpdateFBDownloadedVersionYCurrentX, CurrentFlexBridge, available,
								FwUtilsStrings.UpdateNowPrompt, FwUtilsStrings.UpdateFBNowInstructions),
							FwUtilsStrings.UpdateFBNowCaption, MessageBoxButtons.YesNo, options: FlexibleMessageBoxOptions.AlwaysOnTop))
						{
							var timer = new Timer { Interval = 1000 };
							timer.Elapsed += (o, e) =>
							{
								// Wait for FLEx Bridge to exit.
								// If multiple Elapsed events have piled up, install only once.
								if (Process.GetProcessesByName("FLExBridge").Any() || !timer.Enabled)
									return;

								timer.Stop(); // one installer can run at once
								InstallFlexBridge(localFile);
							};
							timer.Start();
						}
					});
				}
				else
				{
					NotifyUserOnIdle(ui, () => MessageBox.Show(Form.ActiveForm,
						GetUpdateMessage(FwUtilsStrings.UpdateDownloadedVersionYCurrentX, Current, available, FwUtilsStrings.RestartToUpdatePrompt),
						FwUtilsStrings.RestartToUpdateCaption));
				}

				return $"Update downloaded to {localFile}";
			}
			catch (Exception e)
			{
				if (updateSettings.Channel == UpdateSettings.Channels.Nightly)
				{
					ErrorReport.AddStandardProperties();
					ErrorReport.ReportNonFatalExceptionWithMessage(e, "Failed to download updates");
				}
				return $"Got {e.GetType()}: {e.Message}";
			}
			finally
			{
				// Check for FLEx Bridge updates only if FieldWorks is up to date or the user is a tester
				if (!isFlexBridge && (alreadyUpToDate ||
									  updateSettings.Channel == UpdateSettings.Channels.Nightly ||
									  updateSettings.Channel == UpdateSettings.Channels.Testing))
				{
					CheckForFlexBridgeUpdates(ui, updateSettings);
				}
			}
		}

		private static void NotifyUserOnIdle(ILcmUI ui, Action notifyAction)
		{
			var timer = new Timer { SynchronizingObject = ui.SynchronizeInvoke, Interval = 1000 };
			timer.Elapsed += (o, e) =>
			{
				// Don't interrupt a user who is busy typing; wait for a pause to prompt to install updates.
				// If multiple Elapsed events have piled up, don't keep notifying the user.
				if (DateTime.Now - ui.LastActivityTime < TimeSpan.FromSeconds(12) || !timer.Enabled)
					return;

				timer.Stop(); // one notification is enough
				notifyAction();
			};
			timer.Start();
		}

		internal static void GetBaseUrlFromUpdateInfo(XDocument info, ref string baseUrl)
		{
			var urlFromDoc = info.Root?.Element("BaseUrl")?.Value;
			if (!string.IsNullOrWhiteSpace(urlFromDoc))
			{
				if (!urlFromDoc.EndsWith("/"))
				{
					urlFromDoc += "/";
				}
				baseUrl = urlFromDoc;
			}
		}

		internal static FwUpdate GetLatestUpdateFrom(FwUpdate current, XDocument bucketContents, string bucketURL, bool userChoice = false)
		{
			if (bucketContents.Root == null)
			{
				return null;
			}

			var availableUpdates = GetUpdatesFromBucketContents(bucketContents, bucketURL);
			return userChoice ? ChooseUpdateFrom(current, availableUpdates) : GetLatestUpdateFrom(current, availableUpdates);
		}

		public static IEnumerable<FwUpdate> GetUpdatesFromBucketContents(XDocument bucketContents, string bucketURL)
		{
			return bucketContents.Root.RemoveNamespaces().Elements("Contents").Select(elt => Parse(elt, bucketURL));
		}
		#endregion check for updates

		#region select updates
		internal static FwUpdate GetLatestUpdateFrom(FwUpdate current, IEnumerable<FwUpdate> available)
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

		/// <summary>
		/// Presents the user with a dialog to choose an update from those that are available (filtered to direct updates from the current version)
		/// </summary>
		private static FwUpdate ChooseUpdateFrom(FwUpdate current, IEnumerable<FwUpdate> available)
		{
			var directUpdates = GetAvailableUpdatesFrom(current, available).ToList();
			if (!directUpdates.Any())
			{
				return null;
			}
			directUpdates.Sort();
			using (var chooser = new FwUpdateChooserDlg(current, directUpdates))
			{
				return chooser.ShowDialog() == DialogResult.OK ? chooser.Choice : null;
			}
		}

		/// <returns>
		/// A list of all available updates. Intended for nightly updates, whose list is not curated.
		/// </returns>
		internal static IEnumerable<FwUpdate> GetAvailableUpdatesFrom(FwUpdate current, IEnumerable<FwUpdate> available)
		{
			return available.Where(u => IsPatchOn(current, u) || IsNewerBaseThanBase(current, u));
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

		/// <param name="current">the currently-installed version</param>
		/// <param name="potential">a potential installer</param>
		/// <returns>
		/// true iff potential is a base installer with a greater build number than current's base,
		/// or if there is no base build number and potential has a higher version (needed b/c FLEx Bridge installer URLs have no base build number)
		/// </returns>
		internal static bool IsNewerBaseThanBase(FwUpdate current, FwUpdate potential)
		{
			return potential != null
				&& (potential.InstallerType == FwUpdate.Typ.Online || potential.InstallerType == FwUpdate.Typ.Offline)
				&& potential.Is64Bit == current.Is64Bit
				&& (potential.BaseBuild > current.BaseBuild
					|| (current.BaseBuild == 0 && potential.Version > current.Version));
		}

		/// <summary>
		/// Given a patch installer try to find the most recent associated base installer.
		/// Note that the base offline installer takes precedence over the base online installer.
		/// </summary>
		/// <param name="patch">the patch installer</param>
		/// <param name="available">the available installers</param>
		/// <returns>>If found then returns the most recent associated base installer, else return null.</returns>
		internal static FwUpdate GetBaseForPatch(FwUpdate patch, IEnumerable<FwUpdate> available)
		{
			FwUpdate baseUpdate = null;
			foreach (var potential in available)
			{
				if (potential != null
					&& (potential.InstallerType == FwUpdate.Typ.Online || potential.InstallerType == FwUpdate.Typ.Offline)
					&& potential.Is64Bit == patch.Is64Bit
					&& potential.Version.Major == patch.Version.Major
					&& potential.Version.Minor == patch.Version.Minor)
				{
					if (baseUpdate == null)
						baseUpdate = potential;
					// If the Build number is more recent then possibly return it.
					else if (potential.Version.Build > baseUpdate.Version.Build)
						baseUpdate = potential;
					// If the build number is the same but this is a offline installer then possibly return it.
					else if (potential.Version.Build == baseUpdate.Version.Build && potential.InstallerType == FwUpdate.Typ.Offline)
						baseUpdate = potential;
				}
			}

			return baseUpdate;
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
					update = AddMetadata(update, elt);
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

		/// <returns>a new FwUpdate, cloned from <c>update</c>, with size, date, and model version information from <c>elt</c></returns>
		internal static FwUpdate AddMetadata(FwUpdate update, XElement elt)
		{
			// round up to the next MB
			var size = ulong.TryParse(elt.Element("Size")?.Value, out var byteSize)
				? (int)((byteSize + BytesPerMiB - 1) / BytesPerMiB)
				: update.Size;
			DateTime date = DateTime.TryParse(elt.Element("LastModified")?.Value, out date) ? date.ToUniversalTime() : update.Date;
			var lcmVersion = elt.Element("LCModelVersion")?.Value;
			var liftVersion = elt.Element("LIFTModelVersion")?.Value;
			var fbDataVersion = elt.Element("FlexBridgeDataVersion")?.Value;
			update = new FwUpdate(update, size, date, lcmVersion, liftVersion, fbDataVersion);
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
				// FLExBridge_Offline_4.1.0.exe
				var keyParts = Path.GetFileName(key)?.Split('_');
				if (keyParts?.Length == 3 && keyParts[0].Equals("FLExBridge"))
				{
					// extract the version from before ".exe"
					var verString = keyParts[2].Remove(keyParts[2].LastIndexOf('.'));
					// As of 2023.07, FieldWorks and FLEx Bridge have version and o*line in opposite positions
					keyParts = new[] { keyParts[0], verString, keyParts[1], keyParts[2] };
				}
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

		internal static string GetUpdateMessage(string updateAvailableMsgFormat, FwUpdate current, FwUpdate update, string actionPrompt,
			string flexBridgeInstructions = null)
		{
			// 4 parts max. FW has two fixed messages and up to two version warnings. FB has three fixed messages and one possible version warning.
			var messageParts = new List<string>(4) { updateAvailableMsgFormat };
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
			if (flexBridgeInstructions != null)
			{
				messageParts.Add(flexBridgeInstructions);
			}
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
		/// If an update has been downloaded and the user wants to, install it.
		/// ENHANCE (Hasso) 2023.08: (FB) offer to install FLEx Bridge updates on startup (probably safe, since it shouldn't be running)
		/// </summary>
		public static void InstallDownloadedUpdate()
		{
			var latestPatch = GetLatestDownloadedUpdate(Current);
			if (latestPatch == null || DialogResult.Yes != FlexibleMessageBox.Show(
				GetUpdateMessage(FwUtilsStrings.UpdateDownloadedVersionYCurrentX, Current, latestPatch, FwUtilsStrings.UpdateNowPrompt),
				FwUtilsStrings.UpdateNowCaption, MessageBoxButtons.YesNo, options: FlexibleMessageBoxOptions.AlwaysOnTop))
			{
				return;
			}

			DeleteOldUpdateFiles(latestPatch);

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
				MessageBox.Show(string.Join($"{Environment.NewLine}{Environment.NewLine}",
						string.Format(FwUtilsStrings.CouldNotUpdateAutomaticallyFileXMessage, latestPatchFile), FwUtilsStrings.PleaseReport),
					FwUtilsStrings.CouldNotUpdateAutomaticallyCaption, MessageBoxButtons.OK, MessageBoxIcon.Error);
				throw;
			}
		}

		private static void InstallFlexBridge(string installer)
		{
			Logger.WriteEvent($"Installing {installer}");
			try
			{
				Process.Start(new ProcessStartInfo
				{
					FileName = installer,
					UseShellExecute = false,
					CreateNoWindow = true,
					RedirectStandardError = true,
					RedirectStandardInput = true,
					RedirectStandardOutput = true,
					Arguments = "/passive"
				});
			}
			catch (Exception e)
			{
				Logger.WriteError(e);
				MessageBox.Show(string.Join($"{Environment.NewLine}{Environment.NewLine}",
						string.Format(FwUtilsStrings.CouldNotUpdateFBAutomaticallyFileXMessage, installer), FwUtilsStrings.PleaseReport),
					FwUtilsStrings.CouldNotUpdateAutomaticallyCaption, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		/// <summary>
		/// Returns the latest fully-downloaded update that can be installed directly on this version of FW.
		/// Returns null if none is found, or if there is no UpdateInfo.xml present.
		/// </summary>
		internal static FwUpdate GetLatestDownloadedUpdate(FwUpdate current)
		{
			if (!FileUtils.FileExists(LocalFWUpdateInfoFilePath))
				return null;
			var result = GetLatestUpdateFrom(current,
				FileUtils.GetFilesInDirectory(FwDirectoryFinder.DownloadedUpdates).Select(file => Parse(file, string.Empty)));
			result = AddMetaDataFromUpdateInfo(result, LocalFWUpdateInfoFilePath);
			// Deleting the info file ensures that we don't offer to install updates until after the next check (LT-20774)
			FileUtils.Delete(LocalFWUpdateInfoFilePath);
			return result;
		}

		/// <summary>
		/// Deletes the old update files that have been downloaded.
		/// If we are installing a base then delete all but the base we are installing.
		/// If we are installing a patch then delete all but the patch we are installing and the
		/// base that it patches (if it is downloaded).
		/// </summary>
		internal static void DeleteOldUpdateFiles(FwUpdate newUpdate)
		{
			string[] files = FileUtils.GetFilesInDirectory(FwDirectoryFinder.DownloadedUpdates);

			// Check for a base file associated with this patch.
			FwUpdate newBase = null;
			if (newUpdate.InstallerType == FwUpdate.Typ.Patch)
				newBase = GetBaseForPatch(newUpdate, files.Select(file => Parse(file, string.Empty)));

			var newUpdateFileName = Path.GetFileName(newUpdate.URL);
			var newBaseFileName = newBase == null ? "" : Path.GetFileName(newBase.URL);
			foreach (string file in files)
			{
				if (!newUpdateFileName.Equals(Path.GetFileName(file), StringComparison.OrdinalIgnoreCase) &&
					!newBaseFileName.Equals(Path.GetFileName(file), StringComparison.OrdinalIgnoreCase))
					FileUtils.Delete(file);
			}
		}

		private static FwUpdate AddMetaDataFromUpdateInfo(FwUpdate updateFromFile, string localUpdateInfoFilePath)
		{
			// To facilitate unit testing we will read the file contents into a string and then use XDocument to parse the string
			using (var fileContentStream =
				FileUtils.OpenFileForRead(localUpdateInfoFilePath, Encoding.UTF8))
			{
				var bucketListString = fileContentStream.ReadToEnd();
				try
				{
					var localInfoDoc = XDocument.Parse(bucketListString);
					var localUpdates =
						GetUpdatesFromBucketContents(localInfoDoc, updateFromFile.URL);
					var infoFromBucketFile = localUpdates.First(update =>
						update.Version == updateFromFile.Version &&
						update.Is64Bit == updateFromFile.Is64Bit);

					return new FwUpdate(updateFromFile, infoFromBucketFile.Size, infoFromBucketFile.Date, infoFromBucketFile.LCModelVersion,
						infoFromBucketFile.LIFTModelVersion, infoFromBucketFile.FlexBridgeDataVersion);
				}
				catch
				{
					// If the file is corrupted, continue happily for users, but alert developers
#if DEBUG
					MessageBoxUtils.Show($"Invalid xml in {localUpdateInfoFilePath} updates may be broken.");
#endif

					return updateFromFile;
				}
			}
		}
		#endregion install updates
	}

	/// <summary>Represents an update that can be downloaded</summary>
	public class FwUpdate : IComparable<FwUpdate>
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
		public DateTime Date { get; }

		/// <summary/>
		// ReSharper disable once InconsistentNaming
		public string LCModelVersion { get; }

		/// <summary/>
		public string LIFTModelVersion { get; }

		/// <summary/>
		public string FlexBridgeDataVersion { get; }

		/// <summary/>
		public string URL { get; }

		/// <summary>Partial copy constructor; used for adding metadata</summary>
		internal FwUpdate(FwUpdate toCopy, int size, DateTime date, string lcmVersion, string liftModelVersion, string flexBridgeDataVersion)
			: this(toCopy.Version, toCopy.Is64Bit, toCopy.BaseBuild, toCopy.InstallerType, size, date,
				lcmVersion, liftModelVersion, flexBridgeDataVersion, toCopy.URL)
		{
		}

		/// <summary/>
		public FwUpdate(string version, bool is64Bit, int baseBuild, Typ installerType, int size = 0, DateTime date = new DateTime(),
			string lcmVersion = null, string liftModelVersion = null, string flexBridgeDataVersion = null, string url = null)
			: this(new Version(version), is64Bit, baseBuild, installerType, size, date, lcmVersion, liftModelVersion, flexBridgeDataVersion, url)
		{
		}

		/// <summary/>
		public FwUpdate(Version version, bool is64Bit, int baseBuild, Typ installerType, int size = 0, DateTime date = new DateTime(),
			string lcmVersion = null, string liftModelVersion = null, string flexBridgeDataVersion = null, string url = null)
		{
			Version = version;
			Is64Bit = is64Bit;
			BaseBuild = baseBuild;
			InstallerType = installerType;
			Size = size;
			Date = date;
			LCModelVersion = lcmVersion;
			LIFTModelVersion = liftModelVersion;
			FlexBridgeDataVersion = flexBridgeDataVersion;
			URL = url;
		}

		public override string ToString()
		{
			var bldr = new StringBuilder(32).Append(Version);
			if (InstallerType != Typ.Patch)
			{
				bldr.Append('_').Append(BaseBuild);
			}

			return bldr.Append(" built ").Append(Date.ToString("yyyy-MM-dd")).Append(' ').Append(InstallerType).ToString();
		}

		public int CompareTo(FwUpdate other)
		{
			if (ReferenceEquals(this, other))
				return 0;
			if (null == other)
				return 1;
			var versionComparison = Comparer<Version>.Default.Compare(Version, other.Version);
			if (versionComparison != 0)
				return versionComparison;
			var baseBuildComparison = BaseBuild.CompareTo(other.BaseBuild);
			if (baseBuildComparison != 0)
				return baseBuildComparison;
			var installerTypeComparison = InstallerType.CompareTo(other.InstallerType);
			return installerTypeComparison;
		}
	}
}