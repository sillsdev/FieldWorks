// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using FwAvaloniaDialogs;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Utils;
using SIL.PlatformUtilities;
using SIL.Reporting;
using SIL.Settings;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// Launches the Avalonia Tools → Options dialog (the migrated replacement for <see cref="LexOptionsDlg"/>)
	/// and applies the result to the real settings bus. The Avalonia layer (FwAvaloniaDialogs) stays
	/// LCModel-free by editing an <see cref="OptionsState"/> DTO; this product-side launcher populates that
	/// state from the live settings and, on OK, applies it in roughly <c>LexOptionsDlg</c>'s apply order
	/// (Privacy → Updates → UI mode → UI language → Plugins → Save → auto-open → restart prompt). It is shown
	/// only when <c>UIMode == New</c>; Legacy keeps the WinForms dialog.
	///
	/// SANCTIONED DIVERGENCES FROM LexOptionsDlg (intentional design choices, not bugs — the next migrator
	/// should not "restore parity" here):
	///  1. Lexical Edit UI mode is applied LIVE (no restart). Legacy required a restart to switch the mode;
	///     here broadcasting the PropertyTable "UIMode" property re-resolves the open lexical surfaces in
	///     place, so the General tab offers an "Apply" button instead of a restart prompt.
	///  2. The live UI-language thread-culture switch is intentionally OMITTED. Legacy called
	///     <c>ChangeCurrentThreadUICulture</c> to swap the running UI's culture immediately; here the change
	///     is persisted (registry user-locale + project writing system + reloaded string table) and takes
	///     effect on the (still-prompted) restart, matching how <c>FieldWorks.SetUICulture()</c> reads the
	///     same registry value at startup. Everything else mirrors the legacy apply.
	/// </summary>
	public static class AvaloniaOptionsDialogLauncher
	{
		private const string UIModePropertyName = "UIMode";
		private const string LegacyUIMode = "Legacy";
		private const string NewUIMode = "New";

		/// <summary>
		/// The single New-mode gate both launch sites share (LexTextApp's <c>OnLaunchConnectedDialog</c> and
		/// the Welcome dialog's Options button): the Avalonia Options dialog is shown only when the UI is in
		/// "New" mode; "Legacy" (or anything else / null) keeps the WinForms <see cref="LexOptionsDlg"/>, so a
		/// New-mode issue can never affect the default surface. Both sites read the current mode from their own
		/// source (PropertyTable "UIMode" vs FwApplicationSettings.UIMode) and pass it here, so the decision
		/// can't drift between them.
		/// </summary>
		public static bool ShouldUseAvaloniaOptionsDialog(string currentUiMode) =>
			string.Equals(currentUiMode, NewUIMode, StringComparison.OrdinalIgnoreCase);

		/// <summary>Outcome the caller acts on (the same signals LexTextApp reads after LexOptionsDlg).</summary>
		public struct Result
		{
			public bool Accepted;
			public bool WritingSystemChanged;
			public bool PluginsUpdated;
		}

		/// <summary>
		/// Shows the dialog modally over <paramref name="owner"/> and applies on OK. <paramref name="mediator"/>
		/// may be null (the Welcome dialog's bare-bones path): Plugins are then unavailable.
		/// </summary>
		public static Result Show(LcmCache cache, Mediator mediator, PropertyTable propertyTable,
			FwApplicationSettingsBase settings, FwApp app, IWin32Window owner)
		{
			if (settings == null) throw new ArgumentNullException(nameof(settings));

			var userWs = cache?.ServiceLocator.WritingSystemManager.UserWritingSystem.Id ?? "en";
			var pluginDocs = new Dictionary<string, XmlDocument>();
			var state = BuildState(cache, mediator, settings, app, userWs, pluginDocs);
			// The "Apply" button switches the Lexical Edit UI mode LIVE (no restart): broadcasting the
			// PropertyTable "UIMode" property re-resolves the open lexical surfaces.
			state.ApplyUiModeLive = mode => ApplyUiModeLive(propertyTable, settings, mode);

			var vm = new OptionsDialogViewModel(state);
			var view = new OptionsDialogView { DataContext = vm };
			var ok = AvaloniaDialogHost.ShowModal(owner, view, vm, FwAvaloniaDialogsStrings.OptionsTitle, 430, 360);
			if (ok != true)
				return new Result { Accepted = false };

			return Apply(cache, mediator, propertyTable, settings, app, userWs, state, pluginDocs, owner);
		}

		// ----- build the state from the live settings -----

		internal static OptionsState BuildState(LcmCache cache, Mediator mediator, FwApplicationSettingsBase settings,
			FwApp app, string userWs, IDictionary<string, XmlDocument> pluginDocs)
		{
			var normUserWs = NormalizeWs(userWs);
			var state = new OptionsState
			{
				AvailableUiLanguages = BuildLanguages(normUserWs, GetSatelliteResourceDirectory()),
				UiLanguage = normUserWs,
				OriginalUiLanguage = normUserWs,
				UiMode = NormalizeUiMode(settings.UIMode),
				OriginalUiMode = NormalizeUiMode(settings.UIMode),
				AutoOpenLastProject = app?.RegistrySettings?.AutoOpenLastEditedProject ?? false,
				OkToPingBasicUsageData = settings.Reporting?.OkToPingBasicUsageData ?? false,
				UpdatesTabVisible = Platform.IsWindows,
				PluginsAvailable = mediator != null
			};

			if (Platform.IsWindows)
			{
				var update = settings.Update ?? new UpdateSettings { Behavior = UpdateSettings.Behaviors.DoNotCheck };
				state.AutoUpdate = update.Behavior != UpdateSettings.Behaviors.DoNotCheck;
				state.UpdateChannel = update.Channel.ToString();
				var channels = new List<NamedOption>
				{
					new NamedOption(UpdateSettings.Channels.Stable.ToString(), LexTextControls.UpdatesStable, LexTextControls.UpdatesStableDescription),
					new NamedOption(UpdateSettings.Channels.Beta.ToString(), LexTextControls.UpdatesBeta, LexTextControls.UpdatesBetaDescription),
					new NamedOption(UpdateSettings.Channels.Alpha.ToString(), LexTextControls.UpdatesAlpha, LexTextControls.UpdatesAlphaDescription)
				};
				// Keep a QA channel visible only if it is the one currently selected (matches LexOptionsDlg).
				if (update.Channel == UpdateSettings.Channels.Nightly || update.Channel == UpdateSettings.Channels.Testing)
					channels.Add(new NamedOption(update.Channel.ToString(), update.Channel.ToString(), string.Empty));
				state.AvailableChannels = channels;
			}

			if (mediator != null)
				state.Plugins = BuildPlugins(pluginDocs);
			return state;
		}

		/// <summary>The directory scanned for satellite resource DLLs (the running executable's directory).</summary>
		private static string GetSatelliteResourceDirectory() => Path.GetDirectoryName(Application.ExecutablePath);

		/// <summary>
		/// Builds the selectable UI-language list from the satellite resource folders under
		/// <paramref name="resourceDir"/> (mirrors UserInterfaceChooser), always including English and the
		/// current UI language. Pure given the directory, so it is unit-testable against a temp folder.
		/// </summary>
		internal static IReadOnlyList<NamedOption> BuildLanguages(string normUserWs, string resourceDir)
		{
			var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			var langs = new List<NamedOption>();
			void Add(string code)
			{
				if (string.IsNullOrEmpty(code) || !seen.Add(code))
					return;
				langs.Add(new NamedOption(code, DisplayName(code)));
			}

			// Languages with satellite resource DLLs next to the executable (mirrors UserInterfaceChooser).
			if (!string.IsNullOrEmpty(resourceDir) && Directory.Exists(resourceDir))
			{
				foreach (var dir in Directory.GetDirectories(resourceDir))
					if (Directory.GetFiles(dir, "*.resources.dll").Length > 0)
						Add(Path.GetFileName(dir));
			}
			if (!langs.Any(l => l.Code.StartsWith("en", StringComparison.OrdinalIgnoreCase)))
				Add("en");
			Add(normUserWs); // ensure the current UI language is selectable even without a satellite folder
			return langs;
		}

		private static string DisplayName(string code)
		{
			try { return new CultureInfo(code).NativeName; }
			catch { return code; }
		}

		private static List<PluginOption> BuildPlugins(IDictionary<string, XmlDocument> pluginDocs)
		{
			var plugins = new List<PluginOption>();
			var basePluginPath = Path.Combine(
				FwDirectoryFinder.GetCodeSubDirectory(Path.Combine("Language Explorer", "Configuration")),
				"Available Plugins");
			var baseExtensionPath = Path.Combine(FwDirectoryFinder.DataDirectory,
				Path.Combine("Language Explorer", "Configuration"));
			if (!Directory.Exists(basePluginPath))
				return plugins;

			foreach (var dir in Directory.GetDirectories(basePluginPath))
			{
				if (Platform.IsUnix && dir == Path.Combine(basePluginPath, "Concorder"))
					continue; // FWNX-755: Concorder not offered on Unix
				var managerPath = Path.Combine(dir, "ExtensionManager.xml");
				if (!File.Exists(managerPath))
					continue;
				var doc = LoadPluginManagerDoc(managerPath);
				if (doc == null)
					continue; // malformed/unreadable ExtensionManager.xml — skip the plugin rather than crash.
				var manager = doc.SelectSingleNode("/manager");
				var name = GetRequiredAttr(manager, "name");
				var targetDir = GetRequiredAttr(manager?.SelectSingleNode("configfiles"), "targetdir");
				if (name == null || targetDir == null)
					continue; // required metadata missing — skip this plugin (matches "do nothing" intent).
				var description = GetRequiredAttr(manager, "description") ?? string.Empty;
				var extensionPath = Path.Combine(baseExtensionPath, targetDir);
				plugins.Add(new PluginOption(name, description, Directory.Exists(extensionPath)));
				pluginDocs[name] = doc;
			}
			return plugins;
		}

		/// <summary>
		/// Loads an <c>ExtensionManager.xml</c> with a non-resolving reader (XmlResolver=null, no DTD
		/// processing) to avoid XXE, returning null if the file is missing/malformed rather than throwing
		/// mid-apply. Shared by build and install/uninstall so all plugin XML I/O is guarded the same way.
		/// </summary>
		internal static XmlDocument LoadPluginManagerDoc(string managerPath)
		{
			if (string.IsNullOrEmpty(managerPath) || !File.Exists(managerPath))
				return null;
			try
			{
				var doc = new XmlDocument { XmlResolver = null };
				var readerSettings = new XmlReaderSettings
				{
					XmlResolver = null,
					DtdProcessing = DtdProcessing.Prohibit
				};
				using (var reader = XmlReader.Create(managerPath, readerSettings))
					doc.Load(reader);
				return doc.SelectSingleNode("/manager") == null ? null : doc;
			}
			catch (XmlException e)
			{
				// Skip rather than crash mid-apply — but leave a breadcrumb so a corrupt manifest (which
				// now silently vanishes from the Options plugin list) is still diagnosable.
				Logger.WriteEvent($"Skipping plugin manifest '{managerPath}': malformed XML — {e.Message}");
				return null;
			}
			catch (IOException e)
			{
				Logger.WriteEvent($"Skipping plugin manifest '{managerPath}': cannot read — {e.Message}");
				return null;
			}
		}

		/// <summary>Returns a non-empty attribute value, or null when the node/attribute is missing or blank.</summary>
		private static string GetRequiredAttr(XmlNode node, string attrName)
		{
			var value = node?.Attributes?[attrName]?.Value;
			return string.IsNullOrEmpty(value) ? null : value;
		}

		// ----- apply the edited state back to the live settings (mirrors LexOptionsDlg.m_btnOK_Click) -----

		private static Result Apply(LcmCache cache, Mediator mediator, PropertyTable propertyTable,
			FwApplicationSettingsBase settings, FwApp app, string userWs, OptionsState state,
			IDictionary<string, XmlDocument> pluginDocs, IWin32Window owner)
		{
			var restartRequired = false;
			var wsChanged = false;

			// Privacy.
			if (settings.Reporting != null && settings.Reporting.OkToPingBasicUsageData != state.OkToPingBasicUsageData)
			{
				settings.Reporting.OkToPingBasicUsageData = state.OkToPingBasicUsageData;
				restartRequired = true;
			}

			// Updates (Windows only).
			if (Platform.IsWindows)
			{
				var update = settings.Update ?? new UpdateSettings { Behavior = UpdateSettings.Behaviors.DoNotCheck };
				var oldBehavior = update.Behavior;
				var oldChannel = update.Channel;
				update.Behavior = state.AutoUpdate ? UpdateSettings.Behaviors.Download : UpdateSettings.Behaviors.DoNotCheck;
				if (Enum.TryParse(state.UpdateChannel, out UpdateSettings.Channels channel))
					update.Channel = channel;
				settings.Update = update;
				if (mediator != null && (oldBehavior != update.Behavior || oldChannel != update.Channel))
					restartRequired = true;
			}

			// Lexical Edit UI mode: applied LIVE (no restart) — broadcasting the PropertyTable "UIMode"
			// property re-resolves the open lexical surfaces. (The "Apply" button does the same.)
			var newUiMode = NormalizeUiMode(state.UiMode);
			if (NormalizeUiMode(settings.UIMode) != newUiMode)
				ApplyUiModeLive(propertyTable, settings, newUiMode);

			// User-interface language. The live thread-culture switch is omitted (the change applies on the
			// required restart); the persisted registry value + project WS + reloaded string table match
			// LexOptionsDlg, and FieldWorks.SetUICulture() reads the same registry value at startup.
			var newWs = NormalizeWs(state.UiLanguage);
			if (ShouldApplyWritingSystemChange(userWs, newWs))
			{
				FwRegistryHelper.FieldWorksRegistryKey.SetValue(FwRegistryHelper.UserLocaleValueName, newWs);
				if (cache != null)
				{
					cache.ServiceLocator.WritingSystemManager.GetOrSet(newWs, out var ws);
					cache.ServiceLocator.WritingSystemManager.UserWritingSystem = ws;
				}
				StringTable.Table.Reload(newWs);
				restartRequired = true;
				wsChanged = true;
			}

			// Plugins install/uninstall (diff Installed vs WasInstalled).
			var pluginsUpdated = ApplyPlugins(mediator, state, pluginDocs);

			settings.Save();

			if (app?.RegistrySettings != null)
				app.RegistrySettings.AutoOpenLastEditedProject = state.AutoOpenLastProject;

			if (restartRequired)
				MessageBox.Show(owner, LexTextControls.RestartToForSettingsToTakeEffect_Content,
					LexTextControls.RestartToForSettingsToTakeEffect_Title);

			return new Result { Accepted = true, WritingSystemChanged = wsChanged, PluginsUpdated = pluginsUpdated };
		}

		private static bool ApplyPlugins(Mediator mediator, OptionsState state, IDictionary<string, XmlDocument> pluginDocs)
		{
			if (mediator == null || state.Plugins == null || state.Plugins.Count == 0)
				return false;

			var basePluginPath = Path.Combine(
				FwDirectoryFinder.GetCodeSubDirectory(Path.Combine("Language Explorer", "Configuration")),
				"Available Plugins");
			var baseExtensionPath = Path.Combine(FwDirectoryFinder.DataDirectory,
				Path.Combine("Language Explorer", "Configuration"));
			var updated = false;

			foreach (var plugin in state.Plugins)
			{
				if (!pluginDocs.TryGetValue(plugin.Name, out var doc))
					continue;
				if (plugin.Installed && !plugin.WasInstalled)
				{
					InstallPlugin(doc, basePluginPath, baseExtensionPath);
					updated = true;
				}
				else if (!plugin.Installed && plugin.WasInstalled)
				{
					UninstallPlugin(doc, mediator, baseExtensionPath);
					updated = true;
				}
			}
			return updated;
		}

		private static void InstallPlugin(XmlDocument managerDoc, string basePluginPath, string baseExtensionPath)
		{
			var manager = managerDoc?.SelectSingleNode("/manager");
			var name = GetRequiredAttr(manager, "name");
			var configfiles = manager?.SelectSingleNode("configfiles");
			var targetDir = GetRequiredAttr(configfiles, "targetdir");
			if (name == null || targetDir == null)
				return; // guarded: malformed manager mid-apply must not throw.
			var srcDir = Path.Combine(basePluginPath, name);
			var extensionPath = Path.Combine(baseExtensionPath, targetDir);
			Directory.CreateDirectory(extensionPath);
			foreach (XmlNode fileNode in configfiles.SelectNodes("file"))
			{
				var fileName = GetRequiredAttr(fileNode, "name");
				if (fileName != null)
					CopyPluginFile(srcDir, extensionPath, fileName);
			}
			var fwInstallDir = FwDirectoryFinder.CodeDirectory;
			foreach (XmlNode dllNode in manager.SelectNodes("dlls/file"))
			{
				var dllName = GetRequiredAttr(dllNode, "name");
				if (dllName != null)
					CopyPluginFile(srcDir, fwInstallDir, dllName);
			}
		}

		private static void CopyPluginFile(string srcDir, string destDir, string filename)
		{
			var dest = Path.Combine(destDir, filename);
			try
			{
				File.Copy(Path.Combine(srcDir, filename), dest, true);
				File.SetAttributes(dest, FileAttributes.Normal);
			}
			catch
			{
				// Eat copy exception (matches LexOptionsDlg).
			}
		}

		private static void UninstallPlugin(XmlDocument managerDoc, Mediator mediator, string baseExtensionPath)
		{
			var manager = managerDoc?.SelectSingleNode("/manager");
			var targetDir = GetRequiredAttr(manager?.SelectSingleNode("configfiles"), "targetdir");
			if (targetDir == null)
				return; // guarded: no target dir means nothing to uninstall (and avoids Delete on a bad path).
			var shutdownMsg = XmlUtils.GetOptionalAttributeValue(manager, "shutdown");
			if (!string.IsNullOrEmpty(shutdownMsg))
#pragma warning disable 618
				mediator.SendMessage(shutdownMsg, null);
#pragma warning restore 618
			var extensionPath = Path.Combine(baseExtensionPath, targetDir);
			if (Directory.Exists(extensionPath))
				Directory.Delete(extensionPath, true);
			// Leave any dlls in place since they may be shared, or in use for the moment.
		}

		// Applies a UI-mode change live: persist into settings + mirror+broadcast through the PropertyTable
		// so the open lexical surfaces (RecordBrowseView/RecordEditView) re-resolve without a restart.
		private static void ApplyUiModeLive(PropertyTable propertyTable, FwApplicationSettingsBase settings, string mode)
		{
			var norm = NormalizeUiMode(mode);
			settings.UIMode = norm;
			if (propertyTable != null)
			{
				propertyTable.SetProperty(UIModePropertyName, norm, true);
				propertyTable.SetPropertyPersistence(UIModePropertyName, false);
			}
		}

		internal static string NormalizeUiMode(string mode) =>
			string.Equals(mode, NewUIMode, StringComparison.OrdinalIgnoreCase) ? NewUIMode : LegacyUIMode;

		internal static string NormalizeWs(string ws) => ws == "en-US" ? "en" : ws;

		/// <summary>
		/// The UI-language apply gate (mirrors LexOptionsDlg): apply only when the chosen WS actually
		/// differs from the current one AND it is not "en-US" (which normalizes to the default "en", so a
		/// switch to it is a no-op). <paramref name="newWs"/> is expected to be already normalized via
		/// <see cref="NormalizeWs"/>. Pure, so the gate logic is unit-testable on its own.
		/// </summary>
		internal static bool ShouldApplyWritingSystemChange(string userWs, string newWs) =>
			!string.Equals(userWs, newWs, StringComparison.Ordinal) && newWs != "en-US";
	}
}
