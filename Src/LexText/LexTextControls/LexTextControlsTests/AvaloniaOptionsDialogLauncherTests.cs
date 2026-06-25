// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using FwAvaloniaDialogs;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Reporting;
using SIL.Settings;
using XCore;

namespace LexTextControlsTests
{
	/// <summary>
	/// Unit tests for the pure decision logic of <see cref="SIL.FieldWorks.LexText.Controls.AvaloniaOptionsDialogLauncher"/>
	/// (visible via InternalsVisibleTo). The launcher mutates registry/settings/writing-systems/filesystem,
	/// so these cover the parts that are testable without a live LcmCache: the writing-system + UI-mode
	/// normalization/gate logic, the satellite-folder language list against a temp directory, the
	/// state-from-settings mapping, and the hardened plugin-manager XML loader.
	/// </summary>
	[TestFixture]
	public class AvaloniaOptionsDialogLauncherTests
	{
		// ----- ShouldUseAvaloniaOptionsDialog (the shared New-mode gate) -----

		[TestCase("New", true)]
		[TestCase("new", true)]
		[TestCase("NEW", true)]
		[TestCase("Legacy", false)]
		[TestCase("legacy", false)]
		[TestCase("", false)]
		[TestCase(null, false)]
		[TestCase("something-else", false)]
		public void ShouldUseAvaloniaOptionsDialog_OnlyTrueForNew(string mode, bool expected)
		{
			Assert.That(SIL.FieldWorks.LexText.Controls.AvaloniaOptionsDialogLauncher.ShouldUseAvaloniaOptionsDialog(mode),
				Is.EqualTo(expected));
		}

		// ----- NormalizeWs -----

		[Test]
		public void NormalizeWs_EnUs_NormalizesToEn()
		{
			Assert.That(SIL.FieldWorks.LexText.Controls.AvaloniaOptionsDialogLauncher.NormalizeWs("en-US"), Is.EqualTo("en"));
		}

		[TestCase("en")]
		[TestCase("fr")]
		[TestCase("es-ES")]
		[TestCase(null)]
		public void NormalizeWs_NonEnUs_Unchanged(string ws)
		{
			Assert.That(SIL.FieldWorks.LexText.Controls.AvaloniaOptionsDialogLauncher.NormalizeWs(ws), Is.EqualTo(ws));
		}

		// ----- ShouldApplyWritingSystemChange (the gate from Apply) -----

		[Test]
		public void ShouldApplyWritingSystemChange_SameWs_False()
		{
			Assert.That(SIL.FieldWorks.LexText.Controls.AvaloniaOptionsDialogLauncher.ShouldApplyWritingSystemChange("fr", "fr"), Is.False);
		}

		[Test]
		public void ShouldApplyWritingSystemChange_DifferentWs_True()
		{
			Assert.That(SIL.FieldWorks.LexText.Controls.AvaloniaOptionsDialogLauncher.ShouldApplyWritingSystemChange("en", "fr"), Is.True);
		}

		[Test]
		public void ShouldApplyWritingSystemChange_NewIsEnUs_False()
		{
			// newWs == "en-US" must never apply (it normalizes to the default "en"); guards the
			// newWs != "en-US" branch of the original inline gate.
			Assert.That(SIL.FieldWorks.LexText.Controls.AvaloniaOptionsDialogLauncher.ShouldApplyWritingSystemChange("fr", "en-US"), Is.False);
		}

		[Test]
		public void ShouldApplyWritingSystemChange_EnToFr_True()
		{
			Assert.That(SIL.FieldWorks.LexText.Controls.AvaloniaOptionsDialogLauncher.ShouldApplyWritingSystemChange("en", "fr"), Is.True);
		}

		// ----- NormalizeUiMode -----

		[TestCase("New", "New")]
		[TestCase("new", "New")]
		[TestCase("NEW", "New")]
		[TestCase("Legacy", "Legacy")]
		[TestCase("legacy", "Legacy")]
		[TestCase("", "Legacy")]
		[TestCase(null, "Legacy")]
		[TestCase("garbage", "Legacy")]
		public void NormalizeUiMode_MapsToCanonicalCasing_OrLegacyDefault(string input, string expected)
		{
			Assert.That(SIL.FieldWorks.LexText.Controls.AvaloniaOptionsDialogLauncher.NormalizeUiMode(input), Is.EqualTo(expected));
		}

		// ----- BuildLanguages (against a temp resource directory) -----

		[Test]
		public void BuildLanguages_NoResourceDir_StillOffersEnAndCurrentWs()
		{
			var langs = SIL.FieldWorks.LexText.Controls.AvaloniaOptionsDialogLauncher.BuildLanguages("fr", null);
			var codes = langs.Select(l => l.Code).ToList();
			Assert.That(codes, Does.Contain("en"));
			Assert.That(codes, Does.Contain("fr"));
		}

		[Test]
		public void BuildLanguages_CurrentWsIsEn_DoesNotDuplicateEn()
		{
			var langs = SIL.FieldWorks.LexText.Controls.AvaloniaOptionsDialogLauncher.BuildLanguages("en", null);
			Assert.That(langs.Count(l => l.Code == "en"), Is.EqualTo(1));
		}

		[Test]
		public void BuildLanguages_PicksUpSatelliteResourceFolders()
		{
			var tempRoot = Path.Combine(Path.GetTempPath(), "FwOptLangTest_" + System.Guid.NewGuid().ToString("N"));
			try
			{
				// A "fr" folder with a satellite resources DLL should be offered; an "empty" folder without
				// one should not.
				var frDir = Directory.CreateDirectory(Path.Combine(tempRoot, "fr"));
				File.WriteAllText(Path.Combine(frDir.FullName, "Something.resources.dll"), "stub");
				Directory.CreateDirectory(Path.Combine(tempRoot, "empty"));

				var langs = SIL.FieldWorks.LexText.Controls.AvaloniaOptionsDialogLauncher.BuildLanguages("en", tempRoot);
				var codes = langs.Select(l => l.Code).ToList();

				Assert.That(codes, Does.Contain("fr"), "a folder with a *.resources.dll is a selectable UI language");
				Assert.That(codes, Does.Not.Contain("empty"), "a folder without a satellite DLL is not a UI language");
				Assert.That(codes, Does.Contain("en"), "English is always offered");
			}
			finally
			{
				if (Directory.Exists(tempRoot))
					Directory.Delete(tempRoot, true);
			}
		}

		// ----- BuildState mapping (no cache / mediator / app required) -----

		[Test]
		public void BuildState_MapsSettingsAndNormalizesWsAndMode()
		{
			var settings = new TestFwApplicationSettings
			{
				UIMode = "new",
				Reporting = new ReportingSettings { OkToPingBasicUsageData = true },
				Update = new UpdateSettings
				{
					Behavior = UpdateSettings.Behaviors.Download,
					Channel = UpdateSettings.Channels.Beta
				}
			};

			var pluginDocs = new System.Collections.Generic.Dictionary<string, XmlDocument>();
			var state = SIL.FieldWorks.LexText.Controls.AvaloniaOptionsDialogLauncher.BuildState(
				null, null, settings, null, "en-US", pluginDocs);

			Assert.That(state.UiLanguage, Is.EqualTo("en"), "en-US normalizes to en");
			Assert.That(state.OriginalUiLanguage, Is.EqualTo("en"));
			Assert.That(state.UiMode, Is.EqualTo("New"), "UIMode is normalized to canonical casing");
			Assert.That(state.OriginalUiMode, Is.EqualTo("New"));
			Assert.That(state.OkToPingBasicUsageData, Is.True);
			Assert.That(state.PluginsAvailable, Is.False, "no mediator => plugins unavailable");
			Assert.That(state.AutoOpenLastProject, Is.False, "null app => default false");
			Assert.That(state.AvailableUiLanguages.Select(l => l.Code), Does.Contain("en"));
		}

		[Test]
		public void BuildState_DefaultsWhenReportingAndAppAbsent()
		{
			var settings = new TestFwApplicationSettings { UIMode = "Legacy" };

			var state = SIL.FieldWorks.LexText.Controls.AvaloniaOptionsDialogLauncher.BuildState(
				null, null, settings, null, "fr",
				new System.Collections.Generic.Dictionary<string, XmlDocument>());

			Assert.That(state.UiMode, Is.EqualTo("Legacy"));
			Assert.That(state.UiLanguage, Is.EqualTo("fr"));
			Assert.That(state.OkToPingBasicUsageData, Is.False, "no Reporting settings => false");
		}

		// ----- LoadPluginManagerDoc (hardened XML I/O) -----

		[Test]
		public void LoadPluginManagerDoc_MissingFile_ReturnsNull()
		{
			Assert.That(SIL.FieldWorks.LexText.Controls.AvaloniaOptionsDialogLauncher.LoadPluginManagerDoc(
				Path.Combine(Path.GetTempPath(), "does-not-exist-" + System.Guid.NewGuid().ToString("N") + ".xml")),
				Is.Null);
		}

		[Test]
		public void LoadPluginManagerDoc_NullOrEmpty_ReturnsNull()
		{
			Assert.That(SIL.FieldWorks.LexText.Controls.AvaloniaOptionsDialogLauncher.LoadPluginManagerDoc(null), Is.Null);
			Assert.That(SIL.FieldWorks.LexText.Controls.AvaloniaOptionsDialogLauncher.LoadPluginManagerDoc(string.Empty), Is.Null);
		}

		[Test]
		public void LoadPluginManagerDoc_MalformedXml_ReturnsNull_DoesNotThrow()
		{
			var path = WriteTemp("<manager name='X'"); // truncated / not well-formed
			try
			{
				Assert.That(SIL.FieldWorks.LexText.Controls.AvaloniaOptionsDialogLauncher.LoadPluginManagerDoc(path), Is.Null);
			}
			finally { File.Delete(path); }
		}

		[Test]
		public void LoadPluginManagerDoc_NoManagerRoot_ReturnsNull()
		{
			var path = WriteTemp("<other/>");
			try
			{
				Assert.That(SIL.FieldWorks.LexText.Controls.AvaloniaOptionsDialogLauncher.LoadPluginManagerDoc(path), Is.Null);
			}
			finally { File.Delete(path); }
		}

		[Test]
		public void LoadPluginManagerDoc_WellFormed_ReturnsDoc()
		{
			var path = WriteTemp("<manager name='Concorder' description='A tool'><configfiles targetdir='Concorder'/></manager>");
			try
			{
				var doc = SIL.FieldWorks.LexText.Controls.AvaloniaOptionsDialogLauncher.LoadPluginManagerDoc(path);
				Assert.That(doc, Is.Not.Null);
				Assert.That(doc.SelectSingleNode("/manager").Attributes["name"].Value, Is.EqualTo("Concorder"));
			}
			finally { File.Delete(path); }
		}

		[Test]
		public void LoadPluginManagerDoc_WithDtd_IsRejected_NotResolved()
		{
			// DtdProcessing.Prohibit / XmlResolver=null: a DTD must not be processed (XXE guard). Either it
			// throws XmlException internally (returned as null) — never resolves an external entity.
			var path = WriteTemp(
				"<?xml version='1.0'?><!DOCTYPE manager [<!ENTITY x 'y'>]><manager name='X' description='d'><configfiles targetdir='t'/></manager>");
			try
			{
				Assert.That(SIL.FieldWorks.LexText.Controls.AvaloniaOptionsDialogLauncher.LoadPluginManagerDoc(path), Is.Null,
					"DTD processing is prohibited, so a DOCTYPE makes the load fail safely rather than resolve entities");
			}
			finally { File.Delete(path); }
		}

		// ----- ApplyPlugins install/uninstall diff (against temp source/target dirs) -----

		[Test]
		public void ApplyPlugins_ToggledOn_InstallsConfigFiles()
		{
			using (var dirs = new TempPluginDirs())
			using (var mediator = new Mediator())
			{
				// Source plugin layout: Available Plugins/Demo/Demo.fwlayout (the configfile to install).
				var srcDemo = Directory.CreateDirectory(Path.Combine(dirs.PluginRoot, "Demo"));
				File.WriteAllText(Path.Combine(srcDemo.FullName, "Demo.fwlayout"), "layout");
				var doc = ManagerDoc("Demo", "DemoTarget", "Demo.fwlayout");

				// User toggled it ON (Installed=true) when it was NOT previously installed.
				var plugin = new PluginOption("Demo", "A demo", installed: false);
				plugin.Installed = true;
				var state = StateWith(plugin);
				var docs = new Dictionary<string, XmlDocument> { ["Demo"] = doc };

				var changed = SIL.FieldWorks.LexText.Controls.AvaloniaOptionsDialogLauncher.ApplyPlugins(
					mediator, state, docs, dirs.PluginRoot, dirs.ExtensionRoot);

				Assert.That(changed, Is.True, "toggling a plugin on reports an update");
				var installed = Path.Combine(dirs.ExtensionRoot, "DemoTarget", "Demo.fwlayout");
				Assert.That(File.Exists(installed), Is.True, "the configfile is copied into the target extension dir");
			}
		}

		[Test]
		public void ApplyPlugins_ToggledOff_UninstallsTargetDir()
		{
			using (var dirs = new TempPluginDirs())
			using (var mediator = new Mediator())
			{
				// The target extension dir already exists (the plugin was installed).
				var target = Directory.CreateDirectory(Path.Combine(dirs.ExtensionRoot, "DemoTarget"));
				File.WriteAllText(Path.Combine(target.FullName, "Demo.fwlayout"), "layout");
				var doc = ManagerDoc("Demo", "DemoTarget", "Demo.fwlayout");

				// User toggled it OFF: WasInstalled=true (constructed installed) then Installed=false.
				var plugin = new PluginOption("Demo", "A demo", installed: true);
				plugin.Installed = false;
				var state = StateWith(plugin);
				var docs = new Dictionary<string, XmlDocument> { ["Demo"] = doc };

				var changed = SIL.FieldWorks.LexText.Controls.AvaloniaOptionsDialogLauncher.ApplyPlugins(
					mediator, state, docs, dirs.PluginRoot, dirs.ExtensionRoot);

				Assert.That(changed, Is.True, "toggling a plugin off reports an update");
				Assert.That(Directory.Exists(target.FullName), Is.False, "the target extension dir is deleted on uninstall");
			}
		}

		[Test]
		public void ApplyPlugins_Unchanged_DoesNothing()
		{
			using (var dirs = new TempPluginDirs())
			using (var mediator = new Mediator())
			{
				var doc = ManagerDoc("Demo", "DemoTarget", "Demo.fwlayout");

				// Installed == WasInstalled (both true): no install, no uninstall.
				var stillInstalled = new PluginOption("Demo", "A demo", installed: true);
				// And a never-installed, still-unchecked one (both false).
				var stillAbsent = new PluginOption("Other", "Another", installed: false);
				var state = StateWith(stillInstalled, stillAbsent);
				var docs = new Dictionary<string, XmlDocument> { ["Demo"] = doc, ["Other"] = ManagerDoc("Other", "OtherTarget", "Other.fwlayout") };

				var changed = SIL.FieldWorks.LexText.Controls.AvaloniaOptionsDialogLauncher.ApplyPlugins(
					mediator, state, docs, dirs.PluginRoot, dirs.ExtensionRoot);

				Assert.That(changed, Is.False, "no toggled plugin means no install/uninstall and no reported update");
				Assert.That(Directory.GetFileSystemEntries(dirs.ExtensionRoot), Is.Empty,
					"an unchanged plugin set must not touch the filesystem");
			}
		}

		[Test]
		public void ApplyPlugins_NullMediator_DoesNothing()
		{
			using (var dirs = new TempPluginDirs())
			{
				var plugin = new PluginOption("Demo", "A demo", installed: false);
				plugin.Installed = true;
				var state = StateWith(plugin);
				var docs = new Dictionary<string, XmlDocument> { ["Demo"] = ManagerDoc("Demo", "DemoTarget", "Demo.fwlayout") };

				var changed = SIL.FieldWorks.LexText.Controls.AvaloniaOptionsDialogLauncher.ApplyPlugins(
					null, state, docs, dirs.PluginRoot, dirs.ExtensionRoot);

				Assert.That(changed, Is.False, "no mediator => plugins are unavailable, so nothing applies");
				Assert.That(Directory.GetFileSystemEntries(dirs.ExtensionRoot), Is.Empty);
			}
		}

		private static OptionsState StateWith(params PluginOption[] plugins) =>
			new OptionsState { PluginsAvailable = true, Plugins = plugins.ToList() };

		/// <summary>Builds a minimal, well-formed ExtensionManager-style manager doc (no dlls node, so install
		/// copies only the named configfile into a temp target dir).</summary>
		private static XmlDocument ManagerDoc(string name, string targetDir, string configFileName)
		{
			var doc = new XmlDocument();
			doc.LoadXml(
				$"<manager name='{name}' description='d'><configfiles targetdir='{targetDir}'><file name='{configFileName}'/></configfiles></manager>");
			return doc;
		}

		/// <summary>A pair of temp dirs (the Available-Plugins source root and the extension target root) cleaned
		/// up on dispose, so the install/uninstall tests never touch the real install layout.</summary>
		private sealed class TempPluginDirs : System.IDisposable
		{
			private readonly string _root;
			public string PluginRoot { get; }
			public string ExtensionRoot { get; }

			public TempPluginDirs()
			{
				_root = Path.Combine(Path.GetTempPath(), "FwOptApplyPlugins_" + System.Guid.NewGuid().ToString("N"));
				PluginRoot = Directory.CreateDirectory(Path.Combine(_root, "Available Plugins")).FullName;
				ExtensionRoot = Directory.CreateDirectory(Path.Combine(_root, "Configuration")).FullName;
			}

			public void Dispose()
			{
				if (Directory.Exists(_root))
					Directory.Delete(_root, true);
			}
		}

		private static string WriteTemp(string content)
		{
			var path = Path.Combine(Path.GetTempPath(), "FwOptPluginTest_" + System.Guid.NewGuid().ToString("N") + ".xml");
			File.WriteAllText(path, content);
			return path;
		}
	}
}
