// Copyright (c) 2016-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer;
using LanguageExplorer.Controls.DetailControls;
using LanguageExplorerTests.Impls;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Infrastructure;
using SIL.Xml;

namespace LanguageExplorerTests.Controls.DetailControls
{
	/// <summary />
	[TestFixture]
	public class DataTreeTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private Inventory m_parts;
		private Inventory m_layouts;
		private ILexEntry m_entry; // test object.
		private IPropertyTable m_propertyTable;
		private IPublisher m_publisher;
		private ISubscriber m_subscriber;
		private DataTree m_dtree;
		private Form m_parent;
		private DummyFwMainWnd _dummyWindow;
		private CustomFieldForTest m_customField;

		#region Fixture Setup and Teardown
		internal static Inventory GenerateParts()
		{
			string partDirectory = Path.Combine(FwDirectoryFinder.SourceDirectory, "LanguageExplorerTests", "Controls", "DetailControls");

			var keyAttrs = new Dictionary<string, string[]>();
			keyAttrs["part"] = new[] {"id"};

			var parts = new Inventory(new string[] {partDirectory},
				"*Parts.xml", "/PartInventory/bin/*", keyAttrs, "DetailTreeTests", Path.GetTempPath());

			return parts;
		}

		internal static Inventory GenerateLayouts()
		{
			string partDirectory = Path.Combine(FwDirectoryFinder.SourceDirectory, "LanguageExplorerTests", "Controls", "DetailControls");

			Dictionary<string, string[]> keyAttrs = new Dictionary<string, string[]>();
			keyAttrs["layout"] = new[] {"class", "type", "name" };
			keyAttrs["group"] = new[] {"label"};
			keyAttrs["part"] = new[] {"ref"};

			var layouts = new Inventory(new[] {partDirectory},
				"*.fwlayout", "/LayoutInventory/*", keyAttrs, "DetailTreeTests", Path.GetTempPath());

			return layouts;
		}

		/// <summary>
		/// Setups this instance.
		/// </summary>
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			m_layouts = GenerateLayouts();
			m_parts = GenerateParts();
			m_customField = new CustomFieldForTest(Cache, "testField", "testField", LexEntryTags.kClassId, CellarPropertyType.String, Guid.Empty);

			NonUndoableUnitOfWorkHelper.Do(m_actionHandler, () =>
			{
				m_entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				m_entry.CitationForm.VernacularDefaultWritingSystem = TsStringUtils.MakeString("rubbish", Cache.DefaultVernWs);
				// We set both alternatives because currently the default part for Bibliography uses vernacular,
				// but I think this will probably get fixed. Anyway, this way the test is robust.
				m_entry.Bibliography.SetAnalysisDefaultWritingSystem("My rubbishy bibliography");
				m_entry.Bibliography.SetVernacularDefaultWritingSystem("My rubbishy bibliography");
			});
		}

		public override void FixtureTeardown()
		{
			base.FixtureTeardown();
			if (Cache != null && Cache.MainCacheAccessor.MetaDataCache != null)
			{
				m_customField.Dispose();
			}
		}
		#endregion

		#region Test setup and teardown
		/// <summary>
		/// Create DataTree and parent form
		/// </summary>
		public override void TestSetup()
		{
			base.TestSetup();
			m_dtree = new DataTree();
			SetupPubSubAndPropertyTable();
			_dummyWindow = new DummyFwMainWnd();
			m_propertyTable.SetProperty("window", _dummyWindow);
			m_dtree.InitializeFlexComponent(new FlexComponentParameters(m_propertyTable, m_publisher, m_subscriber));
			m_parent = new Form();
			m_parent.Controls.Add(m_dtree);
		}

		private void SetupPubSubAndPropertyTable()
		{
			m_propertyTable = TestSetupServices.SetupTestTriumvirate(out m_publisher, out m_subscriber);
		}

		/// <summary>
		/// Close and dispose DataTree and parent form
		/// </summary>
		public override void TestTearDown()
		{
			if (m_parent != null)
			{
				m_parent.Close();
				m_parent.Dispose();
			}
			_dummyWindow.Dispose();
			m_propertyTable?.Dispose();

			_dummyWindow = null;
			m_propertyTable = null;
			m_publisher = null;
			m_subscriber = null;

			base.TestTearDown();
		}
		#endregion

		/// <summary />
		[Test]
		public void OneStringAttr()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "CfOnly", null, m_entry, false);
			Assert.AreEqual(1, m_dtree.Controls.Count);
			Assert.AreEqual("CitationForm", (m_dtree.Controls[0] as Slice).Label);
			// Enhance JohnT: there are more things we could test about this slice,
			// such as the presence and contents and initial selection of the view,
			// but this round of tests is mainly aimed at the process of interpreting
			// layouts and parts to get slices.
		}

		/// <summary />
		[Test]
		public void TwoStringAttr()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "CfAndBib", null, m_entry, false);
			Assert.AreEqual(2, m_dtree.Controls.Count);
			Assert.AreEqual("CitationForm", (m_dtree.Controls[0] as Slice).Label);
			Assert.AreEqual("Bibliography", (m_dtree.Controls[1] as Slice).Label);
		}

		/// <summary />
		[Test]
		public void LabelAbbreviations()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "Abbrs", null, m_entry, false);

			Assert.AreEqual(3, m_dtree.Controls.Count);
			// 1) Test that labels that are not in "LabelAbbreviations" stringTable
			//		are abbreviated by being truncated to 4 characters.
			Assert.AreEqual("CitationForm", (m_dtree.Controls[0] as Slice).Label);
			string abbr1 = StringTable.Table.GetString((m_dtree.Controls[0] as Slice).Label, "LabelAbbreviations");
			Assert.AreEqual(abbr1, "*" + (m_dtree.Controls[0] as Slice).Label + "*");	// verify it's not in the table.
			Assert.AreEqual("Cita", (m_dtree.Controls[0] as Slice).Abbreviation);		// verify truncation took place.
			// 2) Test that a label in "LabelAbbreviations" defaults to its string table entry.
			Assert.AreEqual("Citation Form", (m_dtree.Controls[1] as Slice).Label);
			string abbr2 = StringTable.Table.GetString((m_dtree.Controls[1] as Slice).Label, "LabelAbbreviations");
			Assert.IsFalse(abbr2 == "*" + (m_dtree.Controls[1] as Slice).Label + "*"); // verify it IS in the table
			Assert.AreEqual(abbr2, (m_dtree.Controls[1] as Slice).Abbreviation);		// should be identical
			// 3) Test that a label with an "abbr" attribute overrides default abbreviation.
			Assert.AreEqual("Citation Form", (m_dtree.Controls[2] as Slice).Label);
			Assert.AreEqual((m_dtree.Controls[2] as Slice).Abbreviation, "!?");
			Assert.IsFalse(abbr2 == (m_dtree.Controls[2] as Slice).Abbreviation);
		}

		/// <summary />
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
				Assert.AreEqual(1, m_dtree.Controls.Count);
				Assert.AreEqual("CitationForm", (m_dtree.Controls[0] as Slice).Label);
			}
			finally
			{
				m_entry.Bibliography.SetAnalysisDefaultWritingSystem(anaWsText);
				m_entry.Bibliography.SetVernacularDefaultWritingSystem(vernWsText);
			}
		}

		/// <summary />
		[Test]
		public void NestedExpandedPart()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "Nested-Expanded", null, m_entry, false);
			Assert.AreEqual(3, m_dtree.Controls.Count);
			Assert.AreEqual("Header", (m_dtree.Controls[0] as Slice).Label);
			Assert.AreEqual("Citation form", (m_dtree.Controls[1] as Slice).Label);
			Assert.AreEqual("Bibliography", (m_dtree.Controls[2] as Slice).Label);
			Assert.AreEqual(0, (m_dtree.Controls[1] as Slice).Indent); // was 1, but indent currently suppressed.
		}

		/// <summary>Remove duplicate custom field placeholder parts</summary>
		[Test]
		public void RemoveDuplicateCustomFields()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "Normal", null, m_entry, false);
			var template = m_dtree.GetTemplateForObjLayout(m_entry, "Normal", null);
			var expectedElement = XElement.Parse("<layout class=\"LexEntry\" type=\"detail\" name=\"Normal\"><part ref=\"_CustomFieldPlaceholder\" customFields=\"here\" /><part ref=\"Custom\" param=\"testField\" /></layout>");
			Assert.IsTrue(XmlUtils.NodesMatch(expectedElement, template), "Exactly one part with a _CustomFieldPlaceholder ref attribute should exist.");
		}

		[Test]
		public void BadCustomFieldPlaceHoldersAreCorrected()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "NoRef", null, m_entry, false);
			var template = m_dtree.GetTemplateForObjLayout(m_entry, "NoRef", null);
			var expectedElement = XElement.Parse("<layout class=\"LexEntry\" type=\"detail\" name=\"NoRef\"><part customFields=\"here\" ref=\"_CustomFieldPlaceholder\" /><part ref=\"Custom\" param=\"testField\" /></layout>");
			Assert.IsTrue(XmlUtils.NodesMatch(expectedElement, template), "The previously empty ref on the customFields=\"here\" part should be _CustomFieldPlaceholder.");
		}

		/// <summary />
		[Test]
		[Ignore("Collapsed nodes are currently not implemented")]
		public void NestedCollapsedPart()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "Nested-Collapsed", null, m_entry, false);
			Assert.AreEqual(1, m_dtree.Controls.Count);
			Assert.AreEqual("Header", (m_dtree.Controls[0] as Slice).Label);
		}

		/// <summary />
		[Test]
		public void OwnedObjects()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "OptSensesEty", null, m_entry, false);
			// With no etymology or senses, this view contains nothing at all.
			Assert.AreEqual(0, m_dtree.Controls.Count);
			m_parent.Close();
			m_parent.Dispose();
			m_parent = null;
			m_propertyTable.RemoveProperty("window");
			_dummyWindow.Dispose();
			_dummyWindow = null;

			ILexSense sense1 = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			m_entry.SensesOS.Add(sense1);
			ILexSense sense2 = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			m_entry.SensesOS.Add(sense2);
			Cache.MainCacheAccessor.SetString(sense2.Hvo,
				LexSenseTags.kflidScientificName,
				TsStringUtils.MakeString("blah blah", Cache.DefaultAnalWs));

			m_propertyTable.Dispose();
			SetupPubSubAndPropertyTable();
			_dummyWindow = new DummyFwMainWnd();
			m_propertyTable.SetProperty("window", _dummyWindow);
			m_parent = new Form();
			m_dtree = new DataTree();
			m_dtree.InitializeFlexComponent(new FlexComponentParameters(m_propertyTable, m_publisher, m_subscriber));
			m_parent.Controls.Add(m_dtree);
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "OptSensesEty", null, m_entry, false);
			// With two senses, we get a header slice, a gloss slice for
			// sense 1 (not optional), and both gloss and Scientific name
			// slices for sense 2.
			Assert.AreEqual(3, m_dtree.Controls.Count);
			//Assert.AreEqual("Senses", (m_dtree.Controls[0] as Slice).Label);
			Assert.AreEqual("Gloss", (m_dtree.Controls[0] as Slice).Label);
			Assert.AreEqual("Gloss", (m_dtree.Controls[1] as Slice).Label);
			Assert.AreEqual("ScientificName", (m_dtree.Controls[2] as Slice).Label);
			m_parent.Close();
			m_parent.Dispose();
			m_parent = null;
			m_propertyTable.RemoveProperty("window");
			_dummyWindow.Dispose();
			_dummyWindow = null;

			var etymology = Cache.ServiceLocator.GetInstance<ILexEtymologyFactory>().Create();
			m_entry.EtymologyOS.Add(etymology);

			m_propertyTable.Dispose();
			SetupPubSubAndPropertyTable();
			_dummyWindow = new DummyFwMainWnd();
			m_propertyTable.SetProperty("window", _dummyWindow);
			m_parent = new Form();
			m_dtree = new DataTree();
			m_dtree.InitializeFlexComponent(new FlexComponentParameters(m_propertyTable, m_publisher, m_subscriber));
			m_parent.Controls.Add(m_dtree);
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "OptSensesEty", null, m_entry, false);
			// Adding an etymology gets us just no more slices so far,
			// because it doesn't have a form or source
			Assert.AreEqual(3, m_dtree.Controls.Count);
			m_parent.Close();
			m_parent.Dispose();
			m_parent = null;
			m_propertyTable.RemoveProperty("window");
			_dummyWindow.Dispose();
			_dummyWindow = null;

			etymology.LanguageNotes.AnalysisDefaultWritingSystem = TsStringUtils.MakeString("source language", Cache.DefaultAnalWs);
			etymology.Form.VernacularDefaultWritingSystem = TsStringUtils.MakeString("rubbish", Cache.DefaultVernWs);

			m_propertyTable.Dispose();
			SetupPubSubAndPropertyTable();
			_dummyWindow = new DummyFwMainWnd();
			m_propertyTable.SetProperty("window", _dummyWindow);
			m_parent = new Form();
			m_dtree = new DataTree();
			m_dtree.InitializeFlexComponent(new FlexComponentParameters(m_propertyTable, m_publisher, m_subscriber));
			m_parent.Controls.Add(m_dtree);
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "OptSensesEty", null, m_entry, false);
			// When the etymology has something we get two more.
			Assert.AreEqual(5, m_dtree.Controls.Count);
			Assert.AreEqual("Form", (m_dtree.Controls[3] as Slice).Label);
			Assert.AreEqual("Source Language Notes", (m_dtree.Controls[4] as Slice).Label);
		}
	}
}