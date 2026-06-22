// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.IO;
using System.Linq;
using System.Xml;
using FwAvaloniaDialogs;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Reporting;
using SIL.Settings;

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

		private static string WriteTemp(string content)
		{
			var path = Path.Combine(Path.GetTempPath(), "FwOptPluginTest_" + System.Guid.NewGuid().ToString("N") + ".xml");
			File.WriteAllText(path, content);
			return path;
		}
	}
}
