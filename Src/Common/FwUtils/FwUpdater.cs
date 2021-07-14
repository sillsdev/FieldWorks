// Copyright (c) 2021-2021 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
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
	/// <remarks>REVIEW (Hasso) 2021.07: should this be static or singleton?</remarks>
	public class FwUpdater
	{
		private const uint BytesPerMiB = 1048576;

		///// <remarks>so we can check daily even if the user never closes FW. REVIEW (Hasso) 2021.07: would a timer be better?
		///// If we check daily, and we download two updates before the user restarts (unlikely!), should we notify again?</remarks>
		//private static DateTime s_lastCheck;

		// TODO (Hasso) 2021.07: bool or RESTClient to track whether we're presently checking or downloading; clear in `finally`

		/// <summary>
		/// Checks for updates to FieldWorks, if the settings say to. If an update is found,
		/// downloads the update in the background and (TODO!) notifies the user when the download is complete.
		/// </summary>
		/// <param name="ui">to notify the user when an update is ready to install</param>
		/// <param name="fwAssembly">The FieldWorks assembly (to determine the current version)</param>
		public static void CheckForUpdates(ILcmUI ui, Assembly fwAssembly = null)
		{
			var settings = new FwApplicationSettings();
			var updateSettings = settings.Update;
			if (updateSettings == null)
			{
				// TODO (Hasso) 2021.07: remove or refine this before sending to users
				settings.Update = updateSettings = Environment.GetEnvironmentVariable("FEEDBACK") != null &&
												   MessageBoxUtils.Show(null, "Would you like to check for and download the latest nightly patch now?",
													   "Check for updates?", MessageBoxButtons.YesNo) == DialogResult.Yes
						? new UpdateSettings { Behavior = UpdateSettings.Behaviors.Download, Channel = UpdateSettings.Channels.Nightly }
						: new UpdateSettings { Behavior = UpdateSettings.Behaviors.DoNotCheck, Channel = UpdateSettings.Channels.Stable };
			}
			if (updateSettings.Behavior == UpdateSettings.Behaviors.DoNotCheck)
			{
				return;
			}
			Logger.WriteEvent("Checking for updates...");
			// Check on a background thread; hitting the Internet on the main thread can be a performance hit
			new Thread(() => Logger.WriteEvent(CheckForUpdatesInternal(ui, updateSettings, fwAssembly ?? Assembly.GetEntryAssembly())))
			{
				IsBackground = true
			}.Start();
		}

		/// <returns>a message for the log</returns>
		private static string CheckForUpdatesInternal(ILcmUI ui, UpdateSettings updateSettings, Assembly fwAssembly)
		{
			try
			{
				if (!MiscUtils.IsWindows)
				{
					ErrorReport.ReportNonFatalExceptionWithMessage(new ApplicationException(), "Only Windows updates are available here");
					return "ERROR: Only Windows updates are available here";
				}
				if (updateSettings.Channel != UpdateSettings.Channels.Nightly)
				{
					return "Presently, only nightly releases are available automatically;" +
						   $" {updateSettings.Channel} releases must be downloaded from software.sil.org/fieldworks/downloads";
				}

				var vip = new VersionInfoProvider(fwAssembly, true);
				var current = new FwUpdate(vip.NumericAppVersion, Environment.Is64BitProcess, vip.BaseBuildNumber, FwUpdate.Typ.Offline);
				#if DEBUG
				current = new FwUpdate("9.1.4.847", false, 453, FwUpdate.Typ.Offline);
				#endif

				var available = GetLatestNightlyPatch(current,
					XDocument.Load("https://flex-updates.s3.amazonaws.com/?prefix=jobs/FieldWorks-Win-all"),
					"https://flex-updates.s3.amazonaws.com/");
				// ENHANCE (Hasso) 2021.07: catch WebEx and try again in a minute
				if (available == null)
				{
					return "Check complete; already up to date";
				}
				Logger.WriteMinorEvent($"Update found at {available.URL}");

				var localFile = Path.Combine(FwDirectoryFinder.DownloadedUpdates, Path.GetFileName(available.URL));
				if(!File.Exists(localFile))
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

				// TODO (Hasso) 2021.07: localize strings after they are finalized
				NotifyUserOnIdle(ui,
					$"An update has been downloaded to {localFile}; you can install it at your convenience", "sorry for the inconvenience");

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
				if (DateTime.Now - ui.LastActivityTime < TimeSpan.FromMilliseconds(8000))
					return; // Don't interrupt a user who is busy typing. Wait for a pause to prompt to install updates.

				timer.Stop(); // one notification is enough
				MessageBox.Show(message, caption);
			};
			timer.Start();
		}

		public static FwUpdate GetLatestNightlyPatch(FwUpdate current, XDocument bucketContents, string bucketURL)
		{
			if (bucketContents.Root == null)
			{
				return null;
			}

			bucketContents.Root.RemoveNamespaces();
			FwUpdate latest = null;
			foreach (var potential in bucketContents.Root.Elements().Where(elt => elt.Name.LocalName.Equals("Contents"))
				.Select(elt => Parse(elt, bucketURL)).Where(ver => IsPatchOn(latest ?? current, ver)))
			{
				latest = potential;
			}

			return latest;
		}

		/// <param name="current">the currently-installed version</param>
		/// <param name="potential">a potential installer</param>
		/// <returns>true iff potential is a patch that can be installed on top of current</returns>
		public static bool IsPatchOn(FwUpdate current, FwUpdate potential)
		{
			return potential.InstallerType == FwUpdate.Typ.Patch
				&& potential.Is64Bit == current.Is64Bit
				&& potential.BaseBuild == current.BaseBuild
				&& potential.Version > current.Version;
		}

		/// <param name="elt">a &lt;Contents/&gt; element from an S3 bucket list</param>
		/// <param name="baseURL">the https:// URL of the bucket, with the trailing /</param>
		internal static FwUpdate Parse(XElement elt, string baseURL)
		{
			try
			{
				// Key will be something like
				// jobs/FieldWorks-Win-all-Base/312/FieldWorks_9.0.11.1_Online_x64.exe
				// jobs/FieldWorks-Win-all-Patch/10/FieldWorks_9.0.14.10_b312_x64.msp
				var key = elt.Element("Key")?.Value;
				var keyParts = Path.GetFileName(key)?.Split('_');
				if (keyParts?.Length != 4)
				{
					return null;
				}

				var isBaseBuild = !Path.GetExtension(keyParts[3]).Equals(".msp");
				var baseBuild = isBaseBuild
					? int.Parse(key.Split('/')[2])
					: int.Parse(keyParts[2].Substring(1));
				var installerType = isBaseBuild
					? keyParts[2].Equals("Offline") ? FwUpdate.Typ.Offline : FwUpdate.Typ.Online
					: FwUpdate.Typ.Patch;
				var size = -1;
				if (ulong.TryParse(elt.Element("Size")?.Value, out var byteSize))
				{
					// round up to the next MB
					size = (int)((byteSize + BytesPerMiB - 1) / BytesPerMiB);
				}

				return new FwUpdate(keyParts[1], keyParts[3].StartsWith("x64."), baseBuild, installerType, size, $"{baseURL}{key}");
			}
			catch (Exception e)
			{
				// ReSharper disable once LocalizableElement (log content)
				Console.WriteLine($"Got {e.GetType()}: {e.Message}");
				// REVIEW (Hasso) 2021.05: would returning all zeros be better?
				return null;
			}
		}
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
		public FwUpdate(string version, bool is64Bit, int baseBuild, Typ installerType, int size = 0, string url = null)
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