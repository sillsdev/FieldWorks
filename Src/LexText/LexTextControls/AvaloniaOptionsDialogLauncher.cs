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
using AvControl = Avalonia.Controls.Control;
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
	/// This is the FIRST concrete subclass of the generic <see cref="AvaloniaDialogLauncher{TState,TViewModel,TPayload}"/>
	/// dialog-kit scaffold: it implements the LCModel-aware <c>BuildState</c>/<c>CreateViewModel</c>/
	/// <c>CreateView</c>/<c>Apply</c> steps; the scaffold owns the build-VM → ShowModal → dispose → return
	/// loop. The public static <see cref="Show"/> entry point (kept for the existing callers in LexTextApp and
	/// WelcomeToFieldWorksDlg) constructs an instance and runs it. A second dialog reuses the scaffold rather
	/// than copying this launch plumbing.
	///
	/// PARITY POLICY: this dialog must not diverge from <see cref="LexOptionsDlg"/> without explicit product
	/// approval (see the "dialog-update" skill). Lexical Edit UI mode applies LIVE on OK in both (broadcasting
	/// the PropertyTable "UIMode" property re-resolves the open lexical surfaces — no restart). The UI-language
	/// thread-culture switch is now mirrored too: like WinForms, <c>Apply</c> calls
	/// <c>FormLanguageSwitchSingleton.Instance.ChangeCurrentThreadUICulture</c> so subsequent resource lookups
	/// in the process use the new culture immediately (WinForms's companion <c>ChangeLanguage(this)</c> call
	/// has no Avalonia equivalent — it reflects over a WinForms Form's designer-generated per-control resx keys,
	/// and re-languages only that one closing dialog, which is cosmetic and not reproducible here). Both paths
	/// still prompt for restart afterward — this was never a full live language switch in WinForms either.
	///
	/// APPROVED CARVE-OUT: the "Manage Individual Features..." button is Avalonia-only — it is NOT duplicated
	/// on <see cref="LexOptionsDlg"/>. Per-tool disabling only has meaning in New mode, and the feature has no
	/// WinForms precedent (it was introduced alongside this Avalonia dialog, not migrated from it), so the
	/// parity rule doesn't apply: there is nothing on the WinForms side to stay in sync with. Cost accepted:
	/// a user switching Legacy → New from the WinForms dialog cannot also configure per-tool disables in that
	/// same session; they reopen Options afterward (now Avalonia, since UIMode is New) to do so.
	/// </summary>
	public sealed class AvaloniaOptionsDialogLauncher
		: AvaloniaDialogLauncher<OptionsState, OptionsDialogViewModel, AvaloniaOptionsDialogLauncher.OptionsPayload>
	{
		private const string UIModePropertyName = "UIMode";
		private const string LegacyUIMode = "Legacy";
		private const string NewUIMode = "New";

		// Live-settings context captured at construction; the scaffold's Run loop calls BuildState then Apply
		// against this same context, so the plugin docs harvested while building the state are reused on apply.
		private readonly LcmCache _cache;
		private readonly Mediator _mediator;
		private readonly PropertyTable _propertyTable;
		private readonly FwApplicationSettingsBase _settings;
		private readonly FwApp _app;
		private readonly IWin32Window _owner;
		private readonly string _userWs;
		private readonly Dictionary<string, XmlDocument> _pluginDocs = new Dictionary<string, XmlDocument>();

		private AvaloniaOptionsDialogLauncher(LcmCache cache, Mediator mediator, PropertyTable propertyTable,
			FwApplicationSettingsBase settings, FwApp app, IWin32Window owner)
		{
			_cache = cache;
			_mediator = mediator;
			_propertyTable = propertyTable;
			_settings = settings;
			_app = app;
			_owner = owner;
			_userWs = cache?.ServiceLocator.WritingSystemManager.UserWritingSystem.Id ?? "en";
		}

		/// <summary>The dialog-specific follow-up signals (the same payload LexTextApp reads after the dialog).</summary>
		public struct OptionsPayload
		{
			public bool WritingSystemChanged;
			public bool PluginsUpdated;
		}

		/// <summary>
		/// Outcome the caller acts on (the same signals LexTextApp reads after LexOptionsDlg). Kept as the
		/// launcher's public return shape for the existing callers; internally it is projected from the
		/// scaffold's <see cref="DialogOutcome{TPayload}"/>.
		/// </summary>
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

			var launcher = new AvaloniaOptionsDialogLauncher(cache, mediator, propertyTable, settings, app, owner);
			var outcome = launcher.Run(owner);
			if (!outcome.Accepted)
				return new Result { Accepted = false };
			return new Result
			{
				Accepted = true,
				WritingSystemChanged = outcome.Payload.WritingSystemChanged,
				PluginsUpdated = outcome.Payload.PluginsUpdated
			};
		}

		// ----- scaffold steps -----

		protected override string DialogTitle => FwAvaloniaDialogsStrings.OptionsTitle;
		protected override int DialogWidth => 430;
		protected override int DialogHeight => 360;

		protected override OptionsState BuildState()
		{
			var state = BuildState(_cache, _mediator, _settings, _app, _userWs, _pluginDocs);
			// Per-feature disable set (New mode), seeded from settings and edited by the "Manage Individual
			// Features" dialog — parity with LexOptionsDlg.m_pendingUiModeDisabledTools. The product edge owns
			// the owner window + feature catalog, so it supplies the callback that shows the nested dialog.
			state.UIModeDisabledTools = _settings.UIModeDisabledTools ?? string.Empty;
			state.ManageFeatures = ShowManageFeaturesDialog;
			return state;
		}

		/// <summary>
		/// Opens the "Manage Individual Features" dialog seeded with <paramref name="disabledCsv"/> and returns
		/// the edited CSV (or the same value on cancel). The Avalonia layer calls this through
		/// <see cref="OptionsState.ManageFeatures"/> so it never references the feature catalog or an owner
		/// window itself. Mirrors <c>LexOptionsDlg.m_manageFeaturesButton_Click</c>.
		/// </summary>
		private string ShowManageFeaturesDialog(string disabledCsv)
		{
			var disabledNow = LexicalEditSurfaceResolver.ParseDisabledTools(disabledCsv);
			var edited = LexicalEditFeatureManagerDialog.Show(_owner, LexicalEditFeatureCatalog.Features, disabledNow);
			return edited == null ? disabledCsv : LexicalEditSurfaceResolver.SerializeDisabledTools(edited);
		}

		protected override OptionsDialogViewModel CreateViewModel(OptionsState state) =>
			new OptionsDialogViewModel(state);

		protected override AvControl CreateView(OptionsDialogViewModel viewModel) =>
			new OptionsDialogView { DataContext = viewModel };

		protected override OptionsPayload Apply(OptionsState state) =>
			Apply(_cache, _mediator, _propertyTable, _settings, _app, _userWs, state, _pluginDocs, _owner);

		// ----- build the state from the live settings -----

		internal static OptionsState BuildState(LcmCache cache, Mediator mediator, FwApplicationSettingsBase settings,
			FwApp app, string userWs, IDictionary<string, XmlDocument> pluginDocs)
		{
			var normUserWs = NormalizeWs(userWs);
			var state = new OptionsState
			{
				AvailableUiLanguages = BuildLanguages(normUserWs, GetSatelliteResourceDirectory()),
				UiLanguage = normUserWs,
				UiMode = NormalizeUiMode(settings.UIMode),
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

		private static OptionsPayload Apply(LcmCache cache, Mediator mediator, PropertyTable propertyTable,
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
			// property re-resolves the open lexical surfaces (matches LexOptionsDlg.m_btnOK_Click).
			var newUiMode = NormalizeUiMode(state.UiMode);
			if (NormalizeUiMode(settings.UIMode) != newUiMode)
				ApplyUiModeLive(propertyTable, settings, newUiMode);

			// Per-feature disable set (New mode): persist + broadcast so open surfaces re-resolve, mirroring
			// LexOptionsDlg's m_pendingUiModeDisabledTools apply. Live like the mode itself — no restart.
			var newDisabledTools = state.UIModeDisabledTools ?? string.Empty;
			if ((settings.UIModeDisabledTools ?? string.Empty) != newDisabledTools)
			{
				settings.UIModeDisabledTools = newDisabledTools;
				if (propertyTable != null)
				{
					propertyTable.SetProperty(LexicalEditSurfaceResolver.UIModeDisabledToolsPropertyName, newDisabledTools, true);
					propertyTable.SetPropertyPersistence(LexicalEditSurfaceResolver.UIModeDisabledToolsPropertyName, false);
				}
			}

			// User-interface language: switch the current thread's UI culture live (matches
			// LexOptionsDlg.m_btnOK_Click), then persist registry value + project WS + reload the string
			// table. FieldWorks.SetUICulture() reads the same registry value at startup for future sessions.
			var newWs = NormalizeWs(state.UiLanguage);
			if (ShouldApplyWritingSystemChange(userWs, newWs))
			{
				var ci = MiscUtils.GetCultureForWs(newWs);
				if (ci != null)
					FormLanguageSwitchSingleton.Instance.ChangeCurrentThreadUICulture(ci);

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
				// Avalonia message box (kit) instead of raw WinForms MessageBox — same OK-only prompt text,
				// hosted in a WinForms-owned modal window via AvaloniaDialogHost so the confirmation matches
				// the rest of the New-mode surface (dialog-ownership.md).
				FwMessageBox.Show(owner, LexTextControls.RestartToForSettingsToTakeEffect_Content,
					LexTextControls.RestartToForSettingsToTakeEffect_Title, FwMessageBoxButtons.Ok);

			return new OptionsPayload { WritingSystemChanged = wsChanged, PluginsUpdated = pluginsUpdated };
		}

		/// <summary>
		/// Diffs each plugin's edited <see cref="PluginOption.Installed"/> against its
		/// <see cref="PluginOption.WasInstalled"/> and mutates the filesystem accordingly: toggled-on
		/// (Installed &amp;&amp; !WasInstalled) installs, toggled-off (!Installed &amp;&amp; WasInstalled)
		/// uninstalls, and unchanged does nothing. Returns true when at least one plugin was installed or
		/// uninstalled. Internal so the diff/dispatch can be tested against temp source/target dirs without a
		/// live mediator-driven dialog (InternalsVisibleTo("LexTextControlsTests")).
		/// </summary>
		internal static bool ApplyPlugins(Mediator mediator, OptionsState state, IDictionary<string, XmlDocument> pluginDocs)
		{
			if (mediator == null || state.Plugins == null || state.Plugins.Count == 0)
				return false;

			var basePluginPath = Path.Combine(
				FwDirectoryFinder.GetCodeSubDirectory(Path.Combine("Language Explorer", "Configuration")),
				"Available Plugins");
			var baseExtensionPath = Path.Combine(FwDirectoryFinder.DataDirectory,
				Path.Combine("Language Explorer", "Configuration"));
			return ApplyPlugins(mediator, state, pluginDocs, basePluginPath, baseExtensionPath);
		}

		/// <summary>
		/// The pure diff/dispatch core of <see cref="ApplyPlugins(Mediator,OptionsState,IDictionary{string,XmlDocument})"/>,
		/// taking the source/target roots explicitly so a test can drive install/uninstall against temp
		/// directories instead of the real install layout. Returns true when at least one plugin changed.
		/// </summary>
		internal static bool ApplyPlugins(Mediator mediator, OptionsState state,
			IDictionary<string, XmlDocument> pluginDocs, string basePluginPath, string baseExtensionPath)
		{
			if (mediator == null || state.Plugins == null || state.Plugins.Count == 0)
				return false;

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
