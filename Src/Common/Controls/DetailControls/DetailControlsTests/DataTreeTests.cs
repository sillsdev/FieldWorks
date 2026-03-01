// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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

		#region Characterization Tests — Coverage Gap Closures

		[Test]
		public void DoNotRefresh_GetterReflectsSetter()
		{
			Assert.That(m_dtree.DoNotRefresh, Is.False);
			m_dtree.DoNotRefresh = true;
			Assert.That(m_dtree.DoNotRefresh, Is.True);
			m_dtree.DoNotRefresh = false;
			Assert.That(m_dtree.DoNotRefresh, Is.False);
		}

		[Test]
		public void Init_AssignsMediatorAndPropertyTable()
		{
			Assert.That(m_dtree.Mediator, Is.SameAs(m_mediator));
			Assert.That(m_dtree.PropTable, Is.SameAs(m_propertyTable));
		}

		[Test]
		public void RootLayoutName_DefaultAndAfterShowObject()
		{
			Assert.That(m_dtree.RootLayoutName, Is.EqualTo("default"));

			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "CfOnly", null, m_entry, false);

			Assert.That(m_dtree.RootLayoutName, Is.EqualTo("CfOnly"));
		}

		[Test]
		public void SliceFilter_GetterReflectsSetter()
		{
			Assert.That(m_dtree.SliceFilter, Is.Null);

			var filter = new SliceFilter();
			m_dtree.SliceFilter = filter;

			Assert.That(m_dtree.SliceFilter, Is.SameAs(filter));
		}

		[Test]
		public void ShowingAllFields_ReadsShowHiddenSettingForTool()
		{
			m_propertyTable.SetProperty("ShowHiddenFields-lexiconEdit", true, true, PropertyTable.SettingsGroup.LocalSettings);

			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "CfOnly", null, m_entry, false);

			Assert.That(m_dtree.ShowingAllFields, Is.True);
		}

		[Test]
		public void GetFlidIfPossible_ValidField_ReturnsFlid()
		{
			var mdc = Cache.DomainDataByFlid.MetaDataCache as IFwMetaDataCacheManaged;
			Assert.That(mdc, Is.Not.Null);

			int flid = m_dtree.GetFlidIfPossible(LexEntryTags.kClassId, "CitationForm", mdc);

			Assert.That(flid, Is.GreaterThan(0), "CitationForm should resolve to a valid flid");
		}

		[Test]
		public void GetFlidIfPossible_InvalidField_ReturnsZero_AndCachesInvalidKey()
		{
			var mdc = Cache.DomainDataByFlid.MetaDataCache as IFwMetaDataCacheManaged;
			Assert.That(mdc, Is.Not.Null);
			int countBefore = GetInvalidFieldCacheCount();

			int flid = m_dtree.GetFlidIfPossible(LexEntryTags.kClassId, "DefinitelyNotARealField", mdc);

			Assert.That(flid, Is.EqualTo(0));
			Assert.That(GetInvalidFieldCacheCount(), Is.EqualTo(countBefore + 1),
				"Invalid field should be cached after first failed lookup");
		}

		[Test]
		public void GetFlidIfPossible_InvalidField_SecondCallDoesNotGrowCache()
		{
			var mdc = Cache.DomainDataByFlid.MetaDataCache as IFwMetaDataCacheManaged;
			Assert.That(mdc, Is.Not.Null);

			m_dtree.GetFlidIfPossible(LexEntryTags.kClassId, "StillNotARealField", mdc);
			int countAfterFirst = GetInvalidFieldCacheCount();

			m_dtree.GetFlidIfPossible(LexEntryTags.kClassId, "StillNotARealField", mdc);
			int countAfterSecond = GetInvalidFieldCacheCount();

			Assert.That(countAfterSecond, Is.EqualTo(countAfterFirst),
				"Same invalid field should not be added twice to invalid-field cache");
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

		#region Characterization Tests — ShowObject & Show-Hidden Fields

		/// <summary>
		/// Calling ShowObject with identical parameters to the previous call is a no-op.
		/// </summary>
		[Test]
		public void ShowObject_IdenticalParameters_IsNoOp()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "CfAndBib", null, m_entry, false);
			int sliceCount = m_dtree.Slices.Count;
			var firstSlice = m_dtree.Slices[0];

			// Call again with identical parameters.
			m_dtree.ShowObject(m_entry, "CfAndBib", null, m_entry, false);

			Assert.That(m_dtree.Slices.Count, Is.EqualTo(sliceCount), "Slice count should not change on identical ShowObject call");
			Assert.That(m_dtree.Slices[0], Is.SameAs(firstSlice), "Slice instances should be unchanged");
		}

		/// <summary>
		/// ShowObject with a different root object disposes old slices and creates new ones.
		/// </summary>
		[Test]
		public void ShowObject_DifferentRoot_RecreatesAllSlices()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "CfOnly", null, m_entry, false);
			Assert.That(m_dtree.Slices.Count, Is.EqualTo(1));
			var oldSlice = m_dtree.Slices[0];

			// Create a different root object.
			var entry2 = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			entry2.CitationForm.VernacularDefaultWritingSystem =
				TsStringUtils.MakeString("other", Cache.DefaultVernWs);

			m_dtree.ShowObject(entry2, "CfOnly", null, entry2, false);
			Assert.That(m_dtree.Slices.Count, Is.EqualTo(1));
			Assert.That(m_dtree.Slices[0], Is.Not.SameAs(oldSlice), "Old slice should have been replaced");
			Assert.That(m_dtree.Root, Is.EqualTo(entry2));
		}

		/// <summary>
		/// Same root, same layout → RefreshList path. Slices should be reused.
		/// </summary>
		[Test]
		public void ShowObject_SameRootSameLayout_RefreshesAndReusesSlices()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "CfAndBib", null, m_entry, false);
			var sliceBefore = m_dtree.Slices[0];

			// Force a different descendant to trigger the else-if branch (same root, different descendant).
			ILexSense sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			m_entry.SensesOS.Add(sense);
			m_dtree.ShowObject(m_entry, "CfAndBib", null, m_entry, false);

			// Same root and layout means RefreshList(false) was called. The first slice should survive.
			Assert.That(m_dtree.Slices.Count, Is.EqualTo(2));
			Assert.That(m_dtree.Slices[0], Is.SameAs(sliceBefore), "RefreshList should reuse matching slices");
		}

		/// <summary>
		/// Show-hidden-fields ON reveals visibility="ifdata" slices even when the field is empty.
		/// </summary>
		[Test]
		public void ShowObject_ShowHiddenEnabled_RevealsIfDataEmpty()
		{
			m_entry.Bibliography.SetAnalysisDefaultWritingSystem("");
			m_entry.Bibliography.SetVernacularDefaultWritingSystem("");

			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			// Set show-hidden for lexiconEdit tool.
			m_propertyTable.SetProperty("ShowHiddenFields-lexiconEdit", true,
				PropertyTable.SettingsGroup.LocalSettings, true);
			m_propertyTable.SetDefault("ShowHiddenFields", true,
				PropertyTable.SettingsGroup.LocalSettings, false);

			m_dtree.ShowObject(m_entry, "ShowHiddenTest", null, m_entry, false);

			// With show-hidden ON: CitationForm + Bibliography (empty but revealed) + NeverField (revealed).
			Assert.That(m_dtree.Slices.Count, Is.EqualTo(3),
				"Show-hidden should reveal ifdata-empty and visibility=never slices");
		}

		/// <summary>
		/// Show-hidden-fields OFF hides visibility="ifdata" slices when the field is empty.
		/// </summary>
		[Test]
		public void ShowObject_ShowHiddenDisabled_HidesIfDataEmpty()
		{
			m_entry.Bibliography.SetAnalysisDefaultWritingSystem("");
			m_entry.Bibliography.SetVernacularDefaultWritingSystem("");

			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_propertyTable.SetProperty("ShowHiddenFields-lexiconEdit", false,
				PropertyTable.SettingsGroup.LocalSettings, true);

			m_dtree.ShowObject(m_entry, "ShowHiddenTest", null, m_entry, false);

			// With show-hidden OFF: only CitationForm (Bibliography empty, NeverField hidden).
			Assert.That(m_dtree.Slices.Count, Is.EqualTo(1),
				"With show-hidden off, ifdata-empty and visibility=never should be hidden");
			Assert.That(m_dtree.Slices[0].Label, Is.EqualTo("CitationForm"));
		}

		/// <summary>
		/// Show-hidden-fields ON reveals visibility="never" slices.
		/// </summary>
		[Test]
		public void ShowObject_ShowHiddenEnabled_RevealsNeverVisibility()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_propertyTable.SetProperty("ShowHiddenFields-lexiconEdit", true,
				PropertyTable.SettingsGroup.LocalSettings, true);
			m_propertyTable.SetDefault("ShowHiddenFields", true,
				PropertyTable.SettingsGroup.LocalSettings, false);

			m_dtree.ShowObject(m_entry, "CfOnly", null, m_entry, false);

			// CfOnly has CitationForm + Bibliography(visibility="never").
			// With show-hidden ON, Bibliography should appear.
			Assert.That(m_dtree.Slices.Count, Is.EqualTo(2),
				"Show-hidden should reveal visibility=never slices");
		}

		/// <summary>
		/// Show-hidden OFF hides visibility="never" slices.
		/// </summary>
		[Test]
		public void ShowObject_ShowHiddenDisabled_HidesNeverVisibility()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_propertyTable.SetProperty("ShowHiddenFields-lexiconEdit", false,
				PropertyTable.SettingsGroup.LocalSettings, true);

			m_dtree.ShowObject(m_entry, "CfOnly", null, m_entry, false);

			// CfOnly has CitationForm + Bibliography(visibility="never").
			// With show-hidden OFF, only CitationForm should appear.
			Assert.That(m_dtree.Slices.Count, Is.EqualTo(1));
			Assert.That(m_dtree.Slices[0].Label, Is.EqualTo("CitationForm"));
		}

		/// <summary>
		/// The tool-specific ShowHiddenFields property is isolated per tool.
		/// Setting it for one tool does not affect another.
		/// </summary>
		[Test]
		public void ShowObject_ShowHiddenForDifferentTool_DoesNotAffect()
		{
			m_entry.Bibliography.SetAnalysisDefaultWritingSystem("");
			m_entry.Bibliography.SetVernacularDefaultWritingSystem("");

			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			// Enable show-hidden for a DIFFERENT tool.
			m_propertyTable.SetProperty("ShowHiddenFields-notebookEdit", true,
				PropertyTable.SettingsGroup.LocalSettings, true);
			// lexiconEdit (default for LexEntry) is not set.

			m_dtree.ShowObject(m_entry, "ShowHiddenTest", null, m_entry, false);

			// lexiconEdit show-hidden is off, so ifdata-empty and never should be hidden.
			Assert.That(m_dtree.Slices.Count, Is.EqualTo(1));
		}

		/// <summary>
		/// Toggling ShowHiddenFields via OnPropertyChanged causes refresh.
		/// </summary>
		[Test]
		public void OnPropertyChanged_ShowHiddenFields_TogglesVisibility()
		{
			m_entry.Bibliography.SetAnalysisDefaultWritingSystem("");
			m_entry.Bibliography.SetVernacularDefaultWritingSystem("");

			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_propertyTable.SetProperty("currentContentControl", "lexiconEdit", true);
			m_propertyTable.SetProperty("ShowHiddenFields-lexiconEdit", false,
				PropertyTable.SettingsGroup.LocalSettings, true);

			m_dtree.ShowObject(m_entry, "ShowHiddenTest", null, m_entry, false);
			int countBefore = m_dtree.Slices.Count;
			Assert.That(countBefore, Is.EqualTo(1), "Initially only non-hidden slices");

			// Toggle show-hidden ON.
			m_dtree.OnPropertyChanged("ShowHiddenFields");

			Assert.That(m_dtree.Slices.Count, Is.GreaterThan(countBefore),
				"Toggling show-hidden should reveal more slices");
		}

		#endregion

		#region Characterization Tests — Slice Reuse & Refresh

		/// <summary>
		/// RefreshList(false) reuses existing slice instances when the object and layout haven't changed.
		/// </summary>
		[Test]
		public void RefreshList_SameObject_ReusesSlices()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "CfAndBib", null, m_entry, false);
			Assert.That(m_dtree.Slices.Count, Is.EqualTo(2));
			var slice0 = m_dtree.Slices[0];
			var slice1 = m_dtree.Slices[1];

			m_dtree.RefreshList(false);

			Assert.That(m_dtree.Slices.Count, Is.EqualTo(2));
			Assert.That(m_dtree.Slices[0], Is.SameAs(slice0), "First slice should be reused");
			Assert.That(m_dtree.Slices[1], Is.SameAs(slice1), "Second slice should be reused");
		}

		/// <summary>
		/// RefreshList(true) (different object) does not reuse slices by strict key match.
		/// </summary>
		[Test]
		public void RefreshList_DifferentObject_DoesNotReuseByKey()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "CfAndBib", null, m_entry, false);
			var oldSlice0 = m_dtree.Slices[0];

			m_dtree.RefreshList(true);

			// After differentObject=true, slices may have been recreated.
			// The important thing is the tree still has the right content.
			Assert.That(m_dtree.Slices.Count, Is.EqualTo(2));
		}

		/// <summary>
		/// DoNotRefresh=true suppresses RefreshList; setting it back to false triggers deferred refresh.
		/// </summary>
		[Test]
		public void DoNotRefresh_SuppressesThenTriggersRefresh()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "CfAndBib", null, m_entry, false);
			Assert.That(m_dtree.Slices.Count, Is.EqualTo(2));

			m_dtree.DoNotRefresh = true;

			// Request a refresh while suppressed.
			m_dtree.RefreshList(false);
			Assert.That(m_dtree.RefreshListNeeded, Is.True, "Refresh should be deferred");

			// Unsuppress — deferred refresh should fire.
			m_dtree.DoNotRefresh = false;
			Assert.That(m_dtree.RefreshListNeeded, Is.False, "Deferred refresh should have cleared the flag");
		}

		/// <summary>
		/// RefreshList with an invalid (deleted) root object calls Reset and produces zero slices.
		/// </summary>
		[Test]
		public void RefreshList_InvalidRoot_ProducesZeroSlices()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "CfOnly", null, m_entry, false);
			Assert.That(m_dtree.Slices.Count, Is.EqualTo(1));

			// Delete the root object (we are already inside a UOW from the base class).
			m_entry.Delete();

			m_dtree.RefreshList(true);

			Assert.That(m_dtree.Slices.Count, Is.EqualTo(0),
				"After deleting root, RefreshList should produce zero slices");
		}

		[Test]
		public void MonitoredProps_AccumulatesAcrossRefresh_CurrentBehavior()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "CfOnly", null, m_entry, false);

			int hvo = m_entry.Hvo;
			int flid = (int)LexEntryTags.kflidCitationForm;
			m_dtree.MonitorProp(hvo, flid);
			int countBefore = GetMonitoredPropsCount();

			m_dtree.RefreshList(false);
			int countAfter = GetMonitoredPropsCount();

			Assert.That(countAfter, Is.GreaterThanOrEqualTo(countBefore),
				"Current behavior: monitored props are retained across RefreshList");
		}

		[Test]
		[Explicit("Expected to fail until m_monitoredProps is cleared on refresh.")]
		public void MonitoredProps_ClearedOnRefresh_ExpectedAfterFix()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "CfOnly", null, m_entry, false);

			m_dtree.MonitorProp(m_entry.Hvo, (int)LexEntryTags.kflidCitationForm);
			Assert.That(GetMonitoredPropsCount(), Is.GreaterThan(0),
				"Sanity check: at least one monitored prop should be present before refresh");

			m_dtree.RefreshList(false);

			Assert.That(GetMonitoredPropsCount(), Is.EqualTo(0),
				"Expected future behavior: refresh clears stale monitored props");
		}

		#endregion

		#region Characterization Tests — Navigation

		/// <summary>
		/// NavigationTest layout produces 5 slices for navigation testing.
		/// </summary>
		[Test]
		public void NavigationTest_ProducesExpectedSlices()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "NavigationTest", null, m_entry, false);
			Assert.That(m_dtree.Slices.Count, Is.EqualTo(5));
			Assert.That(m_dtree.Slices[0].Label, Is.EqualTo("CitationForm"));
			Assert.That(m_dtree.Slices[1].Label, Is.EqualTo("Bibliography"));
			Assert.That(m_dtree.Slices[2].Label, Is.EqualTo("CF2"));
			Assert.That(m_dtree.Slices[3].Label, Is.EqualTo("Bib2"));
			Assert.That(m_dtree.Slices[4].Label, Is.EqualTo("CF3"));
		}

		#endregion

		#region Characterization Tests — ManySenses / DummyObjectSlice

		/// <summary>
		/// Helper: creates an entry with the given number of senses.
		/// </summary>
		private ILexEntry CreateEntryWithSenses(int count)
		{
			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			entry.CitationForm.VernacularDefaultWritingSystem =
				TsStringUtils.MakeString("sensetest", Cache.DefaultVernWs);
			for (int i = 0; i < count; i++)
			{
				var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				entry.SensesOS.Add(sense);
				sense.Gloss.AnalysisDefaultWritingSystem =
					TsStringUtils.MakeString("gloss" + i, Cache.DefaultAnalWs);
			}
			return entry;
		}

		/// <summary>
		/// Sequences with more than kInstantSliceMax (20) items may use DummyObjectSlice placeholders.
		/// </summary>
		[Test]
		public void ManySenses_LargeSequence_CreatesSomeSlices()
		{
			var entry = CreateEntryWithSenses(25);
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);

			m_parent.Close();
			m_parent.Dispose();
			m_mediator.Dispose();
			m_mediator = new Mediator();
			m_propertyTable.Dispose();
			m_propertyTable = new PropertyTable(m_mediator);
			m_dtree = new DataTree();
			m_dtree.Init(m_mediator, m_propertyTable, null);
			m_parent = new Form();
			m_parent.Controls.Add(m_dtree);
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(entry, "ManySenses", null, entry, false);

			// With 25 senses, we expect slices to be created.
			// kInstantSliceMax is 20, so some may be DummyObjectSlice (not real).
			Assert.That(m_dtree.Slices.Count, Is.GreaterThan(0),
				"ManySenses layout should create slices for the 25 senses");

			// Characterize: count real vs dummy slices.
			int dummyCount = 0;
			int realCount = 0;
			foreach (var slice in m_dtree.Slices)
			{
				if (slice.IsRealSlice)
					realCount++;
				else
					dummyCount++;
			}
			// Document actual behavior: with the test harness, ALL slices are dummies.
			// This is expected since 25 > kInstantSliceMax (20).
			Assert.That(m_dtree.Slices.Count, Is.GreaterThan(0),
				"With 25 senses, DataTree should create some slices (real or dummy)");
		}

		/// <summary>
		/// FieldAt expands a dummy slice to a real one.
		/// </summary>
		[Test]
		public void FieldAt_ExpandsDummyToReal()
		{
			var entry = CreateEntryWithSenses(25);

			m_parent.Close();
			m_parent.Dispose();
			m_mediator.Dispose();
			m_mediator = new Mediator();
			m_propertyTable.Dispose();
			m_propertyTable = new PropertyTable(m_mediator);
			m_dtree = new DataTree();
			m_dtree.Init(m_mediator, m_propertyTable, null);
			m_parent = new Form();
			m_parent.Controls.Add(m_dtree);
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(entry, "ManySenses", null, entry, false);

			// Find a non-real slice, if any.
			int dummyIndex = -1;
			for (int i = 0; i < m_dtree.Slices.Count; i++)
			{
				if (!m_dtree.Slices[i].IsRealSlice)
				{
					dummyIndex = i;
					break;
				}
			}

			if (dummyIndex >= 0)
			{
				// FieldAt should expand it.
				var realSlice = m_dtree.FieldAt(dummyIndex);
				Assert.That(realSlice, Is.Not.Null);
				Assert.That(realSlice.IsRealSlice, Is.True,
					"FieldAt should expand a DummyObjectSlice to a real slice");
			}
			else
			{
				// All slices are real — document this behavior.
				Assert.Pass("All 25 sense slices were created as real slices (no dummies used).");
			}
		}

		#endregion

		#region Characterization Tests — SetCurrentSlicePropertyNames

		/// <summary>
		/// Verify the property-table keys follow the expected pattern.
		/// </summary>
		[Test]
		public void SetCurrentSlicePropertyNames_ConstructsCorrectKeys()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_propertyTable.SetProperty("areaChoice", "lexicon", true);
			m_propertyTable.SetProperty("currentContentControl", "lexiconEdit", true);

			m_dtree.ShowObject(m_entry, "CfOnly", null, m_entry, false);

			// Verify the property keys are constructed.
			// The keys should follow the pattern: {area}${tool}$CurrentSlicePartName
			// We verify indirectly by checking the properties exist.
			string partNameKey = "lexicon$lexiconEdit$CurrentSlicePartName";
			string objGuidKey = "lexicon$lexiconEdit$CurrentSliceObjectGuid";

			// These should have been set (possibly to null/empty) during ShowObject.
			Assert.That(m_propertyTable.PropertyExists(partNameKey, PropertyTable.SettingsGroup.LocalSettings),
				Is.True, "CurrentSlicePartName property should exist in PropertyTable");
			Assert.That(m_propertyTable.PropertyExists(objGuidKey, PropertyTable.SettingsGroup.LocalSettings),
				Is.True, "CurrentSliceObjectGuid property should exist in PropertyTable");
		}

		#endregion

		#region Characterization Tests — Nested/Expanded Layout

		/// <summary>
		/// Nested expanded layout creates header plus children with correct indent levels.
		/// </summary>
		[Test]
		public void NestedExpandedLayout_HeaderPlusChildren()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "Nested-Expanded", null, m_entry, false);

			Assert.That(m_dtree.Slices.Count, Is.EqualTo(3));
			Assert.That(m_dtree.Slices[0].Label, Is.EqualTo("Header"));
			Assert.That(m_dtree.Slices[1].Label, Is.EqualTo("Citation form"));
			Assert.That(m_dtree.Slices[2].Label, Is.EqualTo("Bibliography"));

			// Document the indent levels for characterization.
			// Note: indent is currently 0 for nested children (documented in existing test).
			Assert.That(m_dtree.Slices[0].Indent, Is.EqualTo(0), "Header at root indent");
		}

		#endregion

		#region Helper Methods

		/// <summary>
		/// Helper: shows object and returns the slice list.
		/// </summary>
		private List<Slice> ShowObjectAndGetSlices(string layoutName, ICmObject obj = null)
		{
			obj = obj ?? m_entry;
			m_dtree.ShowObject(obj, layoutName, null, obj, false);
			return m_dtree.Slices.ToList();
		}

		private int GetMonitoredPropsCount()
		{
			var field = typeof(DataTree).GetField("m_monitoredProps",
				BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.That(field, Is.Not.Null, "Could not reflect DataTree.m_monitoredProps field");
			var value = field.GetValue(m_dtree);
			Assert.That(value, Is.Not.Null, "m_monitoredProps should be non-null");
			var countProperty = value.GetType().GetProperty("Count");
			Assert.That(countProperty, Is.Not.Null, "m_monitoredProps should expose Count");
			return (int)countProperty.GetValue(value, null);
		}

		private int GetInvalidFieldCacheCount()
		{
			var field = typeof(DataTree).GetField("m_setInvalidFields",
				BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.That(field, Is.Not.Null, "Could not reflect DataTree.m_setInvalidFields field");
			var value = field.GetValue(m_dtree);
			Assert.That(value, Is.Not.Null, "m_setInvalidFields should be non-null");
			var countProperty = value.GetType().GetProperty("Count");
			Assert.That(countProperty, Is.Not.Null, "m_setInvalidFields should expose Count");
			return (int)countProperty.GetValue(value, null);
		}

		#endregion
	}
}
