using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

using NUnit.Framework;

using SIL.CoreImpl;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.Utils;
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
		private DataTree m_dtree;
		private Form m_parent;

		private SIL.Utils.StringTable m_stringTable;  // for "LabelAbbreviations"

		#region Fixture Setup and Teardown
		internal static StringTable GenerateStringTable()
		{
			string configurationDir = Path.Combine(DirectoryFinder.FWCodeDirectory,
				@"Language Explorer/Configuration");
			var stringTable = new SIL.Utils.StringTable(configurationDir);

			return stringTable;
		}

		internal static Inventory GenerateParts()
		{
			string partDirectory = Path.Combine(DirectoryFinder.FwSourceDirectory,
				@"Common/Controls/DetailControls/DetailControlsTests");

			var keyAttrs = new Dictionary<string, string[]>();
			keyAttrs["part"] = new string[] {"id"};

			var parts = new Inventory(new string[] {partDirectory},
				"*Parts.xml", "/PartInventory/bin/*", keyAttrs, "DetailTreeTests", "ProjectPath");

			return parts;
		}

		internal static Inventory GenerateLayouts()
		{
			string partDirectory = Path.Combine(DirectoryFinder.FwSourceDirectory,
				@"Common/Controls/DetailControls/DetailControlsTests");

			Dictionary<string, string[]> keyAttrs = new Dictionary<string, string[]>();
			keyAttrs["layout"] = new string[] {"class", "type", "name" };
			keyAttrs["group"] = new string[] {"label"};
			keyAttrs["part"] = new string[] {"ref"};

			var layouts = new Inventory(new string[] {partDirectory},
				"*Layouts.xml", "/LayoutInventory/*", keyAttrs, "DetailTreeTests", "ProjectPath");

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
			m_stringTable = GenerateStringTable();

			NonUndoableUnitOfWorkHelper.Do(m_actionHandler, () =>
			{
				m_entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				m_entry.CitationForm.VernacularDefaultWritingSystem = StringUtils.MakeTss("rubbish", Cache.DefaultVernWs);
				// We set both alternatives because currently the default part for Bibliography uses vernacular,
				// but I think this will probably get fixed. Anyway, this way the test is robust.
				m_entry.Bibliography.SetAnalysisDefaultWritingSystem("My rubbishy bibliography");
				m_entry.Bibliography.SetVernacularDefaultWritingSystem("My rubbishy bibliography");
			});
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
			m_dtree = new DataTree();
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
			// m_dtree gets disposed from m_parent because it's part of its Controls

			if (m_parent != null)
			{
				m_parent.Close();
				m_parent.Dispose();
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
			Assert.AreEqual(1, m_dtree.Controls.Count);
			Assert.AreEqual("CitationForm", (m_dtree.Controls[0] as Slice).Label);
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
			Assert.AreEqual(2, m_dtree.Controls.Count);
			Assert.AreEqual("CitationForm", (m_dtree.Controls[0] as Slice).Label);
			Assert.AreEqual("Bibliography", (m_dtree.Controls[1] as Slice).Label);
		}

		/// <summary></summary>
		[Test]
		public void LabelAbbreviations()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.StringTbl = m_stringTable;
			m_dtree.ShowObject(m_entry, "Abbrs", null, m_entry, false);

			Assert.AreEqual(3, m_dtree.Controls.Count);
			// 1) Test that labels that are not in "LabelAbbreviations" stringTable
			//		are abbreviated by being truncated to 4 characters.
			Assert.AreEqual("CitationForm", (m_dtree.Controls[0] as Slice).Label);
			string abbr1 = m_stringTable.GetString((m_dtree.Controls[0] as Slice).Label, "LabelAbbreviations");
			Assert.AreEqual(abbr1, "*" + (m_dtree.Controls[0] as Slice).Label + "*");	// verify it's not in the table.
			Assert.AreEqual("Cita", (m_dtree.Controls[0] as Slice).Abbreviation);		// verify truncation took place.
			// 2) Test that a label in "LabelAbbreviations" defaults to its string table entry.
			Assert.AreEqual("Citation Form", (m_dtree.Controls[1] as Slice).Label);
			string abbr2 = m_stringTable.GetString((m_dtree.Controls[1] as Slice).Label, "LabelAbbreviations");
			Assert.IsFalse(abbr2 == "*" + (m_dtree.Controls[1] as Slice).Label + "*"); // verify it IS in the table
			Assert.AreEqual(abbr2, (m_dtree.Controls[1] as Slice).Abbreviation);		// should be identical
			// 3) Test that a label with an "abbr" attribute overrides default abbreviation.
			Assert.AreEqual("Citation Form", (m_dtree.Controls[2] as Slice).Label);
			Assert.AreEqual((m_dtree.Controls[2] as Slice).Abbreviation, "!?");
			Assert.IsFalse(abbr2 == (m_dtree.Controls[2] as Slice).Abbreviation);
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
				Assert.AreEqual(1, m_dtree.Controls.Count);
				Assert.AreEqual("CitationForm", (m_dtree.Controls[0] as Slice).Label);
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
			Assert.AreEqual(3, m_dtree.Controls.Count);
			Assert.AreEqual("Header", (m_dtree.Controls[0] as Slice).Label);
			Assert.AreEqual("Citation form", (m_dtree.Controls[1] as Slice).Label);
			Assert.AreEqual("Bibliography", (m_dtree.Controls[2] as Slice).Label);
			Assert.AreEqual(0, (m_dtree.Controls[1] as Slice).Indent); // was 1, but indent currently suppressed.
		}
		/// <summary></summary>
		[Test]
		[Ignore("Collapsed nodes are currently not implemented")]
		public void NestedCollapsedPart()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "Nested-Collapsed", null, m_entry, false);
			Assert.AreEqual(1, m_dtree.Controls.Count);
			Assert.AreEqual("Header", (m_dtree.Controls[0] as Slice).Label);
		}

		/// <summary></summary>
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

			ILexSense sense1 = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			m_entry.SensesOS.Add(sense1);
			ILexSense sense2 = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			m_entry.SensesOS.Add(sense2);
			Cache.MainCacheAccessor.SetString(sense2.Hvo,
				LexSenseTags.kflidScientificName,
				StringUtils.MakeTss("blah blah", Cache.DefaultAnalWs));

			m_parent = new Form();
			m_dtree = new DataTree();
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

			ILexEtymology lm = Cache.ServiceLocator.GetInstance<ILexEtymologyFactory>().Create();
			m_entry.EtymologyOA = lm;

			m_parent = new Form();
			m_dtree = new DataTree();
			m_parent.Controls.Add(m_dtree);
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "OptSensesEty", null, m_entry, false);
			// Adding an etymology gets us just no more slices so far,
			// because it doesn't have a form or source
			Assert.AreEqual(3, m_dtree.Controls.Count);
			//Assert.AreEqual("Etymology", (m_dtree.Controls[3] as Slice).Label);
			m_parent.Close();
			m_parent.Dispose();
			m_parent = null;

			lm.Source = "source";
			// Again set both because I'm not sure which it really
			// should be.
			lm.Form.VernacularDefaultWritingSystem = StringUtils.MakeTss("rubbish", Cache.DefaultVernWs);
			lm.Form.AnalysisDefaultWritingSystem = StringUtils.MakeTss("rubbish", Cache.DefaultAnalWs);

			m_parent = new Form();
			m_dtree = new DataTree();
			m_parent.Controls.Add(m_dtree);
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "OptSensesEty", null, m_entry, false);
			// When the etymology has something we get two more.
			Assert.AreEqual(5, m_dtree.Controls.Count);
			Assert.AreEqual("Form", (m_dtree.Controls[3] as Slice).Label);
			Assert.AreEqual("Source", (m_dtree.Controls[4] as Slice).Label);
		}
	}
}
