// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using NUnit.Framework;
using SIL.LCModel.Core.Text;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Infrastructure;
using SIL.LCModel.Utils;
using XCore;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary></summary>
	[TestFixture]
	public class DataTreeTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private Inventory m_parts;
		private Inventory m_layouts;

		private ILexEntry m_entry; // test object.
		private Mediator m_mediator;
		private PropertyTable m_propertyTable;
		private DataTree m_dtree;
		private Form m_parent;

		private CustomFieldForTest m_customField;
		#region Fixture Setup and Teardown
		internal static Inventory GenerateParts()
		{
			string partDirectory = Path.Combine(FwDirectoryFinder.SourceDirectory,
				@"Common/Controls/DetailControls/DetailControlsTests");

			var keyAttrs = new Dictionary<string, string[]>();
			keyAttrs["part"] = new[] {"id"};

			var parts = new Inventory(new string[] {partDirectory},
				"*Parts.xml", "/PartInventory/bin/*", keyAttrs, "DetailTreeTests", Path.GetTempPath());

			return parts;
		}

		internal static Inventory GenerateLayouts()
		{
			string partDirectory = Path.Combine(FwDirectoryFinder.SourceDirectory,
				@"Common/Controls/DetailControls/DetailControlsTests");

			Dictionary<string, string[]> keyAttrs = new Dictionary<string, string[]>();
			keyAttrs["layout"] = new[] {"class", "type", "name" };
			keyAttrs["group"] = new[] {"label"};
			keyAttrs["part"] = new[] {"ref"};

			var layouts = new Inventory(new[] {partDirectory},
				"*.fwlayout", "/LayoutInventory/*", keyAttrs, "DetailTreeTests", Path.GetTempPath());

			return layouts;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Setups this instance.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			m_layouts = GenerateLayouts();
			m_parts = GenerateParts();
		}
		#endregion

		#region Test setup and teardown
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create DataTree and parent form
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void TestSetup()
		{
			base.TestSetup();

			m_customField = new CustomFieldForTest(Cache, "testField", "testField", LexEntryTags.kClassId,
				CellarPropertyType.String, Guid.Empty);
			// base.TestSetup() may already start a unit-of-work; avoid nesting NonUndoable tasks.
			m_entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			m_entry.CitationForm.VernacularDefaultWritingSystem = TsStringUtils.MakeString("rubbish", Cache.DefaultVernWs);
			// We set both alternatives because currently the default part for Bibliography uses vernacular,
			// but I think this will probably get fixed. Anyway, this way the test is robust.
			m_entry.Bibliography.SetAnalysisDefaultWritingSystem("My rubbishy bibliography");
			m_entry.Bibliography.SetVernacularDefaultWritingSystem("My rubbishy bibliography");

			m_dtree = new DataTree();
			m_mediator = new Mediator();
			m_propertyTable = new PropertyTable(m_mediator);
			m_dtree.Init(m_mediator, m_propertyTable, null);
			m_parent = new Form();
			m_parent.Controls.Add(m_dtree);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Close and dispose DataTree and parent form
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void TestTearDown()
		{
			if (m_customField != null && Cache != null && Cache.MainCacheAccessor.MetaDataCache != null)
			{
				m_customField.Dispose();
				m_customField = null;
			}

			// m_dtree gets disposed from m_parent because it's part of its Controls
			if (m_parent != null)
			{
				m_parent.Close();
				m_parent.Dispose();
			}
			if (m_propertyTable != null)
			{
				m_propertyTable.Dispose();
				m_propertyTable = null;
			}
			if (m_mediator != null)
			{
				m_mediator.Dispose();
				m_mediator = null;
			}

			base.TestTearDown();
		}
		#endregion

		/// <summary></summary>
		[Test]
		public void OneStringAttr()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "CfOnly", null, m_entry, false);
			Assert.That(m_dtree.Controls.Count, Is.EqualTo(1));
			Assert.That((m_dtree.Controls[0] as Slice).Label, Is.EqualTo("CitationForm"));
			// Enhance JohnT: there are more things we could test about this slice,
			// such as the presence and contents and initial selection of the view,
			// but this round of tests is mainly aimed at the process of interpreting
			// layouts and parts to get slices.
		}

		/// <summary></summary>
		[Test]
		public void TwoStringAttr()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "CfAndBib", null, m_entry, false);
			Assert.That(m_dtree.Controls.Count, Is.EqualTo(2));
			Assert.That((m_dtree.Controls[0] as Slice).Label, Is.EqualTo("CitationForm"));
			Assert.That((m_dtree.Controls[1] as Slice).Label, Is.EqualTo("Bibliography"));
		}

		/// <summary></summary>
		[Test]
		public void LabelAbbreviations()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "Abbrs", null, m_entry, false);

			Assert.That(m_dtree.Controls.Count, Is.EqualTo(3));
			// 1) Test that labels that are not in "LabelAbbreviations" stringTable
			//		are abbreviated by being truncated to 4 characters.
			Assert.That((m_dtree.Controls[0] as Slice).Label, Is.EqualTo("CitationForm"));
			string abbr1 = StringTable.Table.GetString((m_dtree.Controls[0] as Slice).Label, "LabelAbbreviations");
			Assert.That(abbr1, Is.EqualTo("*" + (m_dtree.Controls[0] as Slice).Label + "*"));	// verify it's not in the table.
			Assert.That((m_dtree.Controls[0] as Slice).Abbreviation, Is.EqualTo("Cita"));		// verify truncation took place.
			// 2) Test that a label in "LabelAbbreviations" defaults to its string table entry.
			Assert.That((m_dtree.Controls[1] as Slice).Label, Is.EqualTo("Citation Form"));
			string abbr2 = StringTable.Table.GetString((m_dtree.Controls[1] as Slice).Label, "LabelAbbreviations");
			Assert.That(abbr2 == "*" + (m_dtree.Controls[1] as Slice).Label + "*", Is.False); // verify it IS in the table
			Assert.That((m_dtree.Controls[1] as Slice).Abbreviation, Is.EqualTo(abbr2));		// should be identical
			// 3) Test that a label with an "abbr" attribute overrides default abbreviation.
			Assert.That((m_dtree.Controls[2] as Slice).Label, Is.EqualTo("Citation Form"));
			Assert.That((m_dtree.Controls[2] as Slice).Abbreviation, Is.EqualTo("!?"));
			Assert.That(abbr2 == (m_dtree.Controls[2] as Slice).Abbreviation, Is.False);
		}

		/// <summary></summary>
		[Test]
		public void IfDataEmpty()
		{
			string anaWsText = m_entry.Bibliography.AnalysisDefaultWritingSystem.Text;
			string vernWsText = m_entry.Bibliography.VernacularDefaultWritingSystem.Text;
			try
			{
				m_entry.Bibliography.SetAnalysisDefaultWritingSystem("");
				m_entry.Bibliography.SetVernacularDefaultWritingSystem("");
				m_dtree.Initialize(Cache, false, m_layouts, m_parts);
				m_dtree.ShowObject(m_entry, "CfAndBib", null, m_entry, false);
				Assert.That(m_dtree.Controls.Count, Is.EqualTo(1));
				Assert.That((m_dtree.Controls[0] as Slice).Label, Is.EqualTo("CitationForm"));
			}
			finally
			{
				m_entry.Bibliography.SetAnalysisDefaultWritingSystem(anaWsText);
				m_entry.Bibliography.SetVernacularDefaultWritingSystem(vernWsText);
			}
		}

		[Test]
		public void ShowObject_ShowHiddenEnabledForCurrentTool_ShowsIfDataSlicesForLexEntry()
		{
			string anaWsText = m_entry.Bibliography.AnalysisDefaultWritingSystem.Text;
			string vernWsText = m_entry.Bibliography.VernacularDefaultWritingSystem.Text;
			try
			{
				m_entry.Bibliography.SetAnalysisDefaultWritingSystem("");
				m_entry.Bibliography.SetVernacularDefaultWritingSystem("");

				m_propertyTable.SetProperty("currentContentControl", "lexiconEdit-variant", PropertyTable.SettingsGroup.LocalSettings, false);
				m_propertyTable.SetProperty("ShowHiddenFields-lexiconEdit-variant", true, PropertyTable.SettingsGroup.LocalSettings, false);

				m_dtree.Initialize(Cache, false, m_layouts, m_parts);
				m_dtree.ShowObject(m_entry, "CfAndBib", null, m_entry, false);

				Assert.That(m_dtree.Controls.Count, Is.EqualTo(2));
				Assert.That((m_dtree.Controls[0] as Slice).Label, Is.EqualTo("CitationForm"));
				Assert.That((m_dtree.Controls[1] as Slice).Label, Is.EqualTo("Bibliography"));
			}
			finally
			{
				m_entry.Bibliography.SetAnalysisDefaultWritingSystem(anaWsText);
				m_entry.Bibliography.SetVernacularDefaultWritingSystem(vernWsText);
			}
		}

		/// <summary>
		/// When currentContentControl is not set, the show-hidden key for ILexEntry should
		/// fall back to "lexiconEdit" for backward compatibility.
		/// </summary>
		[Test]
		public void ShowObject_NoCurrentContentControl_FallsBackToLexiconEditForLexEntry()
		{
			string anaWsText = m_entry.Bibliography.AnalysisDefaultWritingSystem.Text;
			string vernWsText = m_entry.Bibliography.VernacularDefaultWritingSystem.Text;
			try
			{
				m_entry.Bibliography.SetAnalysisDefaultWritingSystem("");
				m_entry.Bibliography.SetVernacularDefaultWritingSystem("");

				// Do NOT set currentContentControl — test the fallback path.
				m_propertyTable.SetProperty("ShowHiddenFields-lexiconEdit", true, PropertyTable.SettingsGroup.LocalSettings, false);

				m_dtree.Initialize(Cache, false, m_layouts, m_parts);
				m_dtree.ShowObject(m_entry, "CfAndBib", null, m_entry, false);

				// The fallback should pick up "lexiconEdit" and show both slices.
				Assert.That(m_dtree.Controls.Count, Is.EqualTo(2),
					"Should show ifdata slices via lexiconEdit fallback");
				Assert.That((m_dtree.Controls[0] as Slice).Label, Is.EqualTo("CitationForm"));
				Assert.That((m_dtree.Controls[1] as Slice).Label, Is.EqualTo("Bibliography"));
			}
			finally
			{
				m_entry.Bibliography.SetAnalysisDefaultWritingSystem(anaWsText);
				m_entry.Bibliography.SetVernacularDefaultWritingSystem(vernWsText);
			}
		}

		/// <summary>
		/// Enabling ShowHiddenFields for a different tool must not reveal ifdata slices
		/// for the current tool.
		/// </summary>
		[Test]
		public void ShowObject_ShowHiddenForDifferentTool_DoesNotRevealIfDataSlices()
		{
			string anaWsText = m_entry.Bibliography.AnalysisDefaultWritingSystem.Text;
			string vernWsText = m_entry.Bibliography.VernacularDefaultWritingSystem.Text;
			try
			{
				m_entry.Bibliography.SetAnalysisDefaultWritingSystem("");
				m_entry.Bibliography.SetVernacularDefaultWritingSystem("");

				// Current tool is "lexiconEdit", but show-hidden is only set for a *different* tool.
				m_propertyTable.SetProperty("currentContentControl", "lexiconEdit", PropertyTable.SettingsGroup.LocalSettings, false);
				m_propertyTable.SetProperty("ShowHiddenFields-notebookEdit", true, PropertyTable.SettingsGroup.LocalSettings, false);

				m_dtree.Initialize(Cache, false, m_layouts, m_parts);
				m_dtree.ShowObject(m_entry, "CfAndBib", null, m_entry, false);

				// Only CitationForm should show; Bibliography has no data and the correct tool key is OFF.
				Assert.That(m_dtree.Controls.Count, Is.EqualTo(1),
					"ShowHiddenFields for a different tool should not affect the current tool");
				Assert.That((m_dtree.Controls[0] as Slice).Label, Is.EqualTo("CitationForm"));
			}
			finally
			{
				m_entry.Bibliography.SetAnalysisDefaultWritingSystem(anaWsText);
				m_entry.Bibliography.SetVernacularDefaultWritingSystem(vernWsText);
			}
		}

		/// <summary>
		/// When ShowHiddenFields is enabled, parts with visibility="never" should also appear.
		/// </summary>
		[Test]
		public void ShowObject_ShowHiddenEnabled_RevealsNeverVisibilityParts()
		{
			m_propertyTable.SetProperty("currentContentControl", "lexiconEdit", PropertyTable.SettingsGroup.LocalSettings, false);
			m_propertyTable.SetProperty("ShowHiddenFields-lexiconEdit", true, PropertyTable.SettingsGroup.LocalSettings, false);

			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			// CfOnly layout: CitationForm (always) + Bibliography (never)
			m_dtree.ShowObject(m_entry, "CfOnly", null, m_entry, false);

			// With show-hidden ON, even "never" parts should appear.
			Assert.That(m_dtree.Controls.Count, Is.EqualTo(2),
				"visibility='never' parts should be visible when ShowHiddenFields is ON");
			Assert.That((m_dtree.Controls[0] as Slice).Label, Is.EqualTo("CitationForm"));
			Assert.That((m_dtree.Controls[1] as Slice).Label, Is.EqualTo("Bibliography"));
		}

		/// <summary>
		/// OnPropertyChanged("ShowHiddenFields") should toggle the show-hidden state keyed
		/// to the current tool, not a hard-coded name.
		/// </summary>
		[Test]
		public void OnPropertyChanged_ShowHiddenFields_TogglesForCurrentTool()
		{
			string anaWsText = m_entry.Bibliography.AnalysisDefaultWritingSystem.Text;
			string vernWsText = m_entry.Bibliography.VernacularDefaultWritingSystem.Text;
			try
			{
				m_entry.Bibliography.SetAnalysisDefaultWritingSystem("");
				m_entry.Bibliography.SetVernacularDefaultWritingSystem("");

				m_propertyTable.SetProperty("currentContentControl", "mySpecialTool", PropertyTable.SettingsGroup.LocalSettings, false);

				m_dtree.Initialize(Cache, false, m_layouts, m_parts);
				m_dtree.ShowObject(m_entry, "CfAndBib", null, m_entry, false);

				// Initially show-hidden is off: only CitationForm should show.
				Assert.That(m_dtree.Controls.Count, Is.EqualTo(1), "Before toggle: only non-ifdata slice");

				// Toggle via OnPropertyChanged (simulates View menu).
				m_dtree.OnPropertyChanged("ShowHiddenFields");

				// The tool-specific property should now be true.
				bool showHidden = m_propertyTable.GetBoolProperty(
					"ShowHiddenFields-mySpecialTool", false, PropertyTable.SettingsGroup.LocalSettings);
				Assert.That(showHidden, Is.True,
					"OnPropertyChanged should toggle the property keyed to currentContentControl");

				// ifdata slices should now be visible.
				Assert.That(m_dtree.Controls.Count, Is.EqualTo(2), "After toggle: ifdata slice should appear");
				Assert.That((m_dtree.Controls[1] as Slice).Label, Is.EqualTo("Bibliography"));
			}
			finally
			{
				m_entry.Bibliography.SetAnalysisDefaultWritingSystem(anaWsText);
				m_entry.Bibliography.SetVernacularDefaultWritingSystem(vernWsText);
			}
		}

		/// <summary>
		/// OnDisplayShowHiddenFields should report Checked=true when the current tool's
		/// show-hidden property is true.
		/// </summary>
		[Test]
		public void OnDisplayShowHiddenFields_CheckedState_MatchesCurrentTool()
		{
			m_propertyTable.SetProperty("currentContentControl", "lexiconBrowse", PropertyTable.SettingsGroup.LocalSettings, false);
			m_propertyTable.SetProperty("ShowHiddenFields-lexiconBrowse", true, PropertyTable.SettingsGroup.LocalSettings, false);

			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "CfOnly", null, m_entry, false);

			var display = new UIItemDisplayProperties(null, "Show Hidden Fields", true, null, true);
			bool handled = m_dtree.OnDisplayShowHiddenFields(null, ref display);
			Assert.That(handled, Is.True);
			Assert.That(display.Checked, Is.True,
				"Checked should reflect ShowHiddenFields for the current tool (lexiconBrowse)");

			// Now set it to false and re-check.
			m_propertyTable.SetProperty("ShowHiddenFields-lexiconBrowse", false, PropertyTable.SettingsGroup.LocalSettings, false);
			display = new UIItemDisplayProperties(null, "Show Hidden Fields", true, null, true);
			m_dtree.OnDisplayShowHiddenFields(null, ref display);
			Assert.That(display.Checked, Is.False,
				"Checked should be false when ShowHiddenFields is off for the current tool");
		}

		/// <summary></summary>
		[Test]
		public void NestedExpandedPart()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "Nested-Expanded", null, m_entry, false);
			Assert.That(m_dtree.Controls.Count, Is.EqualTo(3));
			Assert.That((m_dtree.Controls[0] as Slice).Label, Is.EqualTo("Header"));
			Assert.That((m_dtree.Controls[1] as Slice).Label, Is.EqualTo("Citation form"));
			Assert.That((m_dtree.Controls[2] as Slice).Label, Is.EqualTo("Bibliography"));
			Assert.That((m_dtree.Controls[1] as Slice).Indent, Is.EqualTo(0)); // was 1, but indent currently suppressed.
		}

		/// <summary>Remove duplicate custom field placeholder parts</summary>
		[Test]
		public void RemoveDuplicateCustomFields()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "Normal", null, m_entry, false);
			var template = m_dtree.GetTemplateForObjLayout(m_entry, "Normal", null);
			var expected = "<layout class=\"LexEntry\" type=\"detail\" name=\"Normal\"><part ref=\"_CustomFieldPlaceholder\" customFields=\"here\" /><part ref=\"Custom\" param=\"testField\" /></layout>";
			Assert.That(expected, Is.EqualTo(template.OuterXml), "Exactly one part with a _CustomFieldPlaceholder ref attribute should exist.");
		}

		[Test]
		public void BadCustomFieldPlaceHoldersAreCorrected()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "NoRef", null, m_entry, false);
			var template = m_dtree.GetTemplateForObjLayout(m_entry, "NoRef", null);
			var expected = "<layout class=\"LexEntry\" type=\"detail\" name=\"NoRef\"><part customFields=\"here\" ref=\"_CustomFieldPlaceholder\" /><part ref=\"Custom\" param=\"testField\" /></layout>";
			Assert.That(expected, Is.EqualTo(template.OuterXml), "The previously empty ref on the customFields=\"here\" part should be _CustomFieldPlaceholder.");
		}

		/// <summary></summary>
		[Test]
		[Ignore("Collapsed nodes are currently not implemented")]
		public void NestedCollapsedPart()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "Nested-Collapsed", null, m_entry, false);
			Assert.That(m_dtree.Controls.Count, Is.EqualTo(1));
			Assert.That((m_dtree.Controls[0] as Slice).Label, Is.EqualTo("Header"));
		}

		[Test]
		public void GetGuidForJumpToTool_UsesRootObject_WhenNoCurrentSlice()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "OptSensesEty", null, m_entry, false);
			Assert.That(m_dtree.CurrentSlice == null, "may be OK to automatically select a slice...but this test will need rewriting");
			var doc = new XmlDocument();
			doc.LoadXml(
			@"<command id='CmdRootEntryJumpToConcordance' label='Show Entry in Concordance' message='JumpToTool'>
				<parameters tool='concordance' className='LexEntry'/>
			</command>");
			using (var cmd = new Command(null, doc.DocumentElement))
			{
				string tool;
				var guid = m_dtree.GetGuidForJumpToTool(cmd, true, out tool);
				Assert.That(guid, Is.EqualTo(m_dtree.Root.Guid));
			}
		}

		/// <summary></summary>
		[Test]
		public void OwnedObjects()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "OptSensesEty", null, m_entry, false);
			// With no etymology or senses, this view contains nothing at all.
			Assert.That(m_dtree.Controls.Count, Is.EqualTo(0));
			m_parent.Close();
			m_parent.Dispose();
			m_parent = null;

			ILexSense sense1 = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			m_entry.SensesOS.Add(sense1);
			ILexSense sense2 = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			m_entry.SensesOS.Add(sense2);
			Cache.MainCacheAccessor.SetString(sense2.Hvo,
				LexSenseTags.kflidScientificName,
				TsStringUtils.MakeString("blah blah", Cache.DefaultAnalWs));

			m_mediator.Dispose();
			m_mediator = new Mediator();
			m_propertyTable.Dispose();
			m_propertyTable = new PropertyTable(m_mediator);
			m_parent = new Form();
			m_dtree = new DataTree();
			m_dtree.Init(m_mediator, m_propertyTable, null);
			m_parent.Controls.Add(m_dtree);
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "OptSensesEty", null, m_entry, false);
			// With two senses, we get a header slice, a gloss slice for
			// sense 1 (not optional), and both gloss and Scientific name
			// slices for sense 2.
			Assert.That(m_dtree.Controls.Count, Is.EqualTo(3));
			//Assert.That((m_dtree.Controls[0] as Slice).Label, Is.EqualTo("Senses"));
			Assert.That((m_dtree.Controls[0] as Slice).Label, Is.EqualTo("Gloss"));
			Assert.That((m_dtree.Controls[1] as Slice).Label, Is.EqualTo("Gloss"));
			Assert.That((m_dtree.Controls[2] as Slice).Label, Is.EqualTo("ScientificName"));
			m_parent.Close();
			m_parent.Dispose();
			m_parent = null;

			var etymology = Cache.ServiceLocator.GetInstance<ILexEtymologyFactory>().Create();
			m_entry.EtymologyOS.Add(etymology);

			m_mediator.Dispose();
			m_mediator = new Mediator();
			m_propertyTable.Dispose();
			m_propertyTable = new PropertyTable(m_mediator);
			m_parent = new Form();
			m_dtree = new DataTree();
			m_dtree.Init(m_mediator, m_propertyTable, null);
			m_parent.Controls.Add(m_dtree);
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "OptSensesEty", null, m_entry, false);
			// Adding an etymology gets us just no more slices so far,
			// because it doesn't have a form or source
			Assert.That(m_dtree.Controls.Count, Is.EqualTo(3));
			m_parent.Close();
			m_parent.Dispose();
			m_parent = null;

			etymology.LanguageNotes.AnalysisDefaultWritingSystem = TsStringUtils.MakeString("source language", Cache.DefaultAnalWs);
			etymology.Form.VernacularDefaultWritingSystem = TsStringUtils.MakeString("rubbish", Cache.DefaultVernWs);

			m_mediator.Dispose();
			m_mediator = new Mediator();
			m_propertyTable.Dispose();
			m_propertyTable = new PropertyTable(m_mediator);
			m_parent = new Form();
			m_dtree = new DataTree();
			m_dtree.Init(m_mediator, m_propertyTable, null);
			m_parent.Controls.Add(m_dtree);
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "OptSensesEty", null, m_entry, false);
			// When the etymology has something we get two more.
			Assert.That(m_dtree.Controls.Count, Is.EqualTo(5));
			Assert.That((m_dtree.Controls[3] as Slice).Label, Is.EqualTo("Form"));
			Assert.That((m_dtree.Controls[4] as Slice).Label, Is.EqualTo("Source Language Notes"));
		}
	}
}
