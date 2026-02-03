// Copyright (c) 2010-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml;

using NUnit.Framework;

using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.FieldWorks.CacheLight;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Filters;
using SIL.Utils;
using XCore;


namespace XMLViewsTests
{
	/// <summary>
	/// Test components related to sorting Browse view by things that are in many:1 relation
	/// to the root object of each row.
	/// </summary>
	[TestFixture]
	public class TestManyOneBrowse
	{
		private IFwMetaDataCache m_mdc;
		private ISilDataAccess m_sda;
		private XmlNodeList m_columnList;
		private Inventory m_layoutInventory;
		private Inventory m_partInventory;
		private LayoutCache m_layouts;
		private WritingSystemManager m_wsManager;
		private IVwCacheDa m_cda;

		/// <summary>
		/// Create objects required for testing.
		/// </summary>
		[OneTimeSetUp]
		public void Setup()
		{
			// Create the following:
			// - part and layout inventories
			// - metadata cache
			// - DataAccess cache
			// - collection of columns to display.

			// We want a MetaDataCache that knows about
			// - LexEntry.Senses, Msas, CitationForm, Bibliography, Etymology
			// - LexSense.SemanticDomains, SenseType, Status, gloss
			// - CmPossibility Name, abbr
			// - MoMorphSynAnalysis
			// - MoStemMsa
			// - MoDerivationalMsa
			string m_sTestPath = Path.Combine(FwDirectoryFinder.SourceDirectory,
				Path.Combine("Common",
				Path.Combine("Controls",
				Path.Combine("XMLViews",
				Path.Combine("XMLViewsTests", "SampleCm.xml")))));
			m_mdc = MetaDataCache.CreateMetaDataCache(m_sTestPath);

			// We want ISilDataAccess with:
			// - LexEntry (1) with no senses and one MSA (2)
			// - LexEntry (4) with one sense (5) and no MSA
			// - LexEntry (6) with three senses (7, 8, 9) and two MSAs (10, 11)
			// - sense(5) with no semantic domains
			// - senses with one SD (7->30, 8->31)
			// - sense with three SDs, one the same as the first (9->30, 31, 32)
			// - MoStemMsa (2, 11)
			// - MoDerivationalMsa (10)
			m_cda = VwCacheDaClass.Create();
			m_cda.TsStrFactory = TsStringUtils.TsStrFactory;
			m_sda = (ISilDataAccess) m_cda;
			m_wsManager = new WritingSystemManager();
			m_sda.WritingSystemFactory = m_wsManager;
			var parser = new SimpleDataParser(m_mdc, m_cda);

			parser.Parse(Path.Combine(FwDirectoryFinder.SourceDirectory,
				Path.Combine("Common",
				Path.Combine("Controls",
				Path.Combine("XMLViews",
				Path.Combine("XMLViewsTests", "SampleData.xml"))))));
			int wsEn = m_wsManager.GetWsFromStr("en");
			// These are mainly to check out the parser.
			Assert.That(m_sda.get_ObjectProp(2, 23011), Is.EqualTo(3), "part of speech of an MoStemMsa");
			Assert.That(m_sda.get_VecItem(1, 2009, 0), Is.EqualTo(2), "owned msa");
			Assert.That(m_sda.get_MultiStringAlt(3, 7003, wsEn).Text, Is.EqualTo("noun"), "got ms property");
			Assert.That(m_sda.get_VecItem(6, 2010, 2), Is.EqualTo(9), "3rd sense");
			Assert.That(m_sda.get_VecItem(9, 21016, 1), Is.EqualTo(31), "2nd semantic domain");

			// Columns includes
			// - CitationForm (string inside span)
			// - Bibliography (string not in span)
			// - Sense glosses (string in para in seq, nested in column element)
			// - Semantic domains (pair of strings in para in seq in seq, using layout refs)
			// - MSAs (simplified, but polymorphic with one having <choice> and one <obj> to CmPossibility
			XmlDocument docColumns = new XmlDocument();
			docColumns.Load(Path.Combine(FwDirectoryFinder.SourceDirectory,
				Path.Combine("Common",
				Path.Combine("Controls",
				Path.Combine("XMLViews",
				Path.Combine("XMLViewsTests", "TestColumns.xml"))))));
			m_columnList = docColumns.DocumentElement.ChildNodes;

			// Parts just has what those columns need.
			string partDirectory = Path.Combine(FwDirectoryFinder.SourceDirectory,
				Path.Combine("Common",
				Path.Combine("Controls",
				Path.Combine("XMLViews", "XMLViewsTests"))));
			Dictionary<string, string[]> keyAttrs = new Dictionary<string, string[]>();
			keyAttrs["layout"] = new string[] { "class", "type", "name" };
			keyAttrs["group"] = new string[] { "label" };
			keyAttrs["part"] = new string[] { "ref" };


			// Currently there are no specialized layout files that match.
			m_layoutInventory = new Inventory(new string[] { partDirectory },
				"*.fwlayout", "/LayoutInventory/*", keyAttrs, "TestManyOneBrowse", "ProjectPath");

			keyAttrs = new Dictionary<string, string[]>();
			keyAttrs["part"] = new string[] { "id" };

			m_partInventory = new Inventory(new string[] { partDirectory },
				"TestParts.xml", "/PartInventory/bin/*", keyAttrs, "TestManyOneBrowse", "ProjectPath");
			m_layouts = new LayoutCache(m_mdc, m_layoutInventory, m_partInventory);
		}

		[OneTimeTearDown]
		public void Teardown()
		{
			if (Marshal.IsComObject(m_cda))
			{
				Marshal.ReleaseComObject(m_cda);
			}
			m_cda = null;

			if (Marshal.IsComObject(m_mdc))
			{
				Marshal.ReleaseComObject(m_mdc);
			}
			m_mdc = null;

			if (Marshal.IsComObject(m_sda))
			{
				Marshal.ReleaseComObject(m_sda);
			}
			m_sda = null;

			m_layoutInventory = null;
			m_columnList = null;
			m_layouts = null;
			m_partInventory = null;
			m_wsManager = null;
		}

		/// <summary>
		/// Test generating ManyOnePathSortItems for columns wanting simple string props of
		/// the root object.
		/// </summary>
		[Test]
		public void GeneratePathlessItems()
		{
			ArrayList list = new ArrayList();
			XmlNode column = m_columnList[0];
			XmlViewsUtils.CollectBrowseItems(1, column, list, null, m_mdc, m_sda, m_layouts);
			Assert.That(list.Count, Is.EqualTo(1), "got one item for lexeme obj 1");
			IManyOnePathSortItem bv = list[0] as IManyOnePathSortItem;
			Assert.That(bv.KeyObject, Is.EqualTo(1));
			Assert.That(bv.PathLength, Is.EqualTo(0));
			list.Clear();
			XmlViewsUtils.CollectBrowseItems(4, column, list, null, m_mdc, m_sda, m_layouts);
			Assert.That(list.Count, Is.EqualTo(1), "got one item for lexeme obj 4");
			list.Clear();
			XmlViewsUtils.CollectBrowseItems(6, column, list, null, m_mdc, m_sda, m_layouts);
			Assert.That(list.Count, Is.EqualTo(1), "got one item for lexeme obj 6");
			bv = list[0] as IManyOnePathSortItem;

			Assert.That(bv.KeyObject, Is.EqualTo(6));
			Assert.That(bv.PathLength, Is.EqualTo(0));
		}
		/// <summary>
		/// Test generating ManyOnePathSortItems for columns wanting an object in an atomic prop
		/// of the root.
		/// </summary>
		[Test]
		public void GenerateAtomicItems()
		{
			ArrayList list = new ArrayList();
			XmlNode column = m_columnList[1]; // Etymology
			XmlViewsUtils.CollectBrowseItems(1, column, list, null, m_mdc, m_sda, m_layouts);
			Assert.That(list.Count, Is.EqualTo(1), "got one item for etymology obj 1");
			IManyOnePathSortItem bv = list[0] as IManyOnePathSortItem;
			Assert.That(bv.KeyObject, Is.EqualTo(60));
			Assert.That(bv.PathLength, Is.EqualTo(1));
			Assert.That(bv.PathObject(0), Is.EqualTo(1));
			Assert.That(bv.PathFlid(0), Is.EqualTo(2011));
			list.Clear();
			XmlViewsUtils.CollectBrowseItems(4, column, list, null, m_mdc, m_sda, m_layouts);
			Assert.That(list.Count, Is.EqualTo(1), "got one item for etymology obj 4");
			list.Clear();
			XmlViewsUtils.CollectBrowseItems(6, column, list, null, m_mdc, m_sda, m_layouts);
			Assert.That(list.Count, Is.EqualTo(1), "got one item for etymology obj 6");
			bv = list[0] as IManyOnePathSortItem;
			Assert.That(bv.KeyObject, Is.EqualTo(61));
			Assert.That(bv.PathLength, Is.EqualTo(1));
			Assert.That(bv.PathObject(0), Is.EqualTo(6));
			Assert.That(bv.PathFlid(0), Is.EqualTo(2011));
		}
		/// <summary>
		/// Test generating ManyOnePathSortItems for columns wanting an object in an seq prop
		/// of the root.
		/// </summary>
		[Test]
		public void GenerateSeqItems()
		{
			ArrayList list = new ArrayList();
			XmlNode column = m_columnList[3]; // Glosses
			XmlViewsUtils.CollectBrowseItems(1, column, list, null, m_mdc, m_sda, m_layouts);
			Assert.That(list.Count, Is.EqualTo(1), "got one items for glosses obj 1");
			list.Clear();
			XmlViewsUtils.CollectBrowseItems(4, column, list, null, m_mdc, m_sda, m_layouts);
			Assert.That(list.Count, Is.EqualTo(1), "got one item for glosses obj 4");
			IManyOnePathSortItem bv = list[0] as IManyOnePathSortItem;
			Assert.That(bv.KeyObject, Is.EqualTo(5));
			Assert.That(bv.PathLength, Is.EqualTo(1));
			Assert.That(bv.PathObject(0), Is.EqualTo(4));
			Assert.That(bv.PathFlid(0), Is.EqualTo(2010));
			list.Clear();
			XmlViewsUtils.CollectBrowseItems(6, column, list, null, m_mdc, m_sda, m_layouts);
			Assert.That(list.Count, Is.EqualTo(3), "got three items for glosses obj 6");
			int[] keys = new int[] {7, 8, 9};
			for (int i = 0; i < keys.Length; i++)
			{
				bv = list[i] as IManyOnePathSortItem;
				Assert.That(bv.KeyObject, Is.EqualTo(keys[i]));
				Assert.That(bv.PathLength, Is.EqualTo(1));
				Assert.That(bv.PathObject(0), Is.EqualTo(6));
				Assert.That(bv.PathFlid(0), Is.EqualTo(2010));
			}
		}
		/// <summary>
		/// Test generating ManyOnePathSortItems for columns wanting an object in an seq prop
		/// of a seq property of the root.
		/// </summary>
		[Test]
		public void GenerateDoubleSeqItems()
		{
			ArrayList list = new ArrayList();
			IManyOnePathSortItem bv;
			XmlNode column = m_columnList[5]; // Semantic domains
			XmlViewsUtils.CollectBrowseItems(1, column, list, null, m_mdc, m_sda, m_layouts);
			Assert.That(list.Count, Is.EqualTo(1), "got one item for SD obj 1"); // no senses!
			list.Clear();
			XmlViewsUtils.CollectBrowseItems(4, column, list, null, m_mdc, m_sda, m_layouts);
			Assert.That(list.Count, Is.EqualTo(1), "got one item for SD obj 4"); // sense 5 has no SDs
			list.Clear();
			// Senses 7, 8, 9, having SDs 7->30, 8->31, and 9->30, 31, 32
			XmlViewsUtils.CollectBrowseItems(6, column, list, null, m_mdc, m_sda, m_layouts);
			Assert.That(list.Count, Is.EqualTo(5), "got five items for SD obj 6");
			int[] keys = new int[] {30, 31, 30, 31, 32};
			int[] keys2 = new int[] {7, 8, 9, 9, 9};
			for (int i = 0; i < keys.Length; i++)
			{
				bv = list[i] as IManyOnePathSortItem;
				Assert.That(bv.KeyObject, Is.EqualTo(keys[i]));
				Assert.That(bv.PathLength, Is.EqualTo(2));
				Assert.That(bv.PathObject(0), Is.EqualTo(6));
				Assert.That(bv.PathFlid(0), Is.EqualTo(2010)); // LexEntry.Senses
				Assert.That(bv.PathObject(1), Is.EqualTo(keys2[i]));
				Assert.That(bv.PathFlid(1), Is.EqualTo(21016)); // LexSense.SemanticDomain
			}
		}

		/// <summary>
		/// Test generating what to display for BVI with no path.
		/// </summary>
		[Test]
		public void DisplayPathlessObject()
		{
			ArrayList list = new ArrayList();
			XmlNode column = m_columnList[0];
			XmlViewsUtils.CollectBrowseItems(1, column, list, null, m_mdc, m_sda, m_layouts);
			IManyOnePathSortItem bvi = list[0] as IManyOnePathSortItem;

			// Try on original column. We get original object since there's no path,
			// but we still dig inside the span
			int useHvo;
			List<XmlNode> collectStructNodes = new List<XmlNode>();
			XmlNode useNode = XmlViewsUtils.GetNodeToUseForColumn(bvi, m_columnList[0], null,
				m_mdc, m_sda, m_layouts, out useHvo, collectStructNodes);
			Assert.That(useHvo, Is.EqualTo(1));
			CheckDebugId(useNode, "LexemeCf");
			Assert.That(collectStructNodes.Count, Is.EqualTo(1));
			CheckDebugId(collectStructNodes[0], "LexemeSpan");

			// Try on another column. Again we get original object, and dig inside span
			collectStructNodes.Clear();
			useNode = XmlViewsUtils.GetNodeToUseForColumn(bvi, m_columnList[1], null,
				m_mdc, m_sda, m_layouts, out useHvo, collectStructNodes);
			Assert.That(useHvo, Is.EqualTo(1));
			CheckDebugId(useNode, "EtymologyObj");
			Assert.That(collectStructNodes.Count, Is.EqualTo(1));
			XmlNode structNode1 = collectStructNodes[0];
			CheckDebugId(structNode1, "EtymologySpan");

			// Try on a column involving a lookup. This affects the node output.
			collectStructNodes.Clear();
			useNode = XmlViewsUtils.GetNodeToUseForColumn(bvi, m_columnList[2], null,
				m_mdc, m_sda, m_layouts, out useHvo, collectStructNodes);
			Assert.That(useHvo, Is.EqualTo(1));
			CheckDebugId(useNode, "EntryMsaSeq");
			Assert.That(collectStructNodes.Count, Is.EqualTo(1));
			structNode1 = collectStructNodes[0];
			CheckDebugId(structNode1, "EntryMsasDiv");
		}

		void CheckDebugId(XmlNode node, string id)
		{
			Assert.That(XmlUtils.GetOptionalAttributeValue(node, "debugId"), Is.EqualTo(id));
		}

		/// <summary>
		/// Test generating what to display for BVI with one atomic object path.
		/// </summary>
		[Test]
		public void DisplayAtomicPathObject()
		{
			ArrayList list = new ArrayList();
			XmlNode column = m_columnList[1];
			// This call was already patched manually in my previous "failed" attempt that likely partially succeeded or I need to check?
			// Wait, I only patched Generate* methods. So this call needs patching.
			XmlViewsUtils.CollectBrowseItems(1, column, list, null, m_mdc, m_sda, m_layouts);
			IManyOnePathSortItem bvi = list[0] as IManyOnePathSortItem;

			// Try on first column. Nothing in the path matches, but we still dig inside
			// the span.
			int useHvo;
			List<XmlNode> collectStructNodes = new List<XmlNode>();
			XmlNode useNode = XmlViewsUtils.GetNodeToUseForColumn(bvi, m_columnList[0], null,
				m_mdc, m_sda, m_layouts, out useHvo, collectStructNodes);
			Assert.That(useHvo, Is.EqualTo(1));
			CheckDebugId(useNode, "LexemeCf");
			Assert.That(collectStructNodes.Count, Is.EqualTo(1));
			CheckDebugId(collectStructNodes[0], "LexemeSpan");

			// Try on matching column.
			collectStructNodes.Clear();
			useNode = XmlViewsUtils.GetNodeToUseForColumn(bvi, m_columnList[1], null,
				m_mdc, m_sda, m_layouts, out useHvo, collectStructNodes);
			Assert.That(useHvo, Is.EqualTo(bvi.KeyObject));
			CheckDebugId(useNode, "EtymologyComment");
			Assert.That(collectStructNodes.Count, Is.EqualTo(1));
			XmlNode structNode1 = collectStructNodes[0];
			CheckDebugId(structNode1, "EtymologySpan");

			// Try on a column involving a lookup. This affects the node output.
			collectStructNodes.Clear();
			useNode = XmlViewsUtils.GetNodeToUseForColumn(bvi, m_columnList[2], null,
				m_mdc, m_sda, m_layouts, out useHvo, collectStructNodes);
			Assert.That(useHvo, Is.EqualTo(1));
			CheckDebugId(useNode, "EntryMsaSeq");
			Assert.That(collectStructNodes.Count, Is.EqualTo(1));
			structNode1 = collectStructNodes[0];
			CheckDebugId(structNode1, "EntryMsasDiv");

			// On a different view of the Etymology, we should still get the Etymology object.
			collectStructNodes.Clear();
			useNode = XmlViewsUtils.GetNodeToUseForColumn(bvi, m_columnList[6], null,
				m_mdc, m_sda, m_layouts, out useHvo, collectStructNodes);
			Assert.That(useHvo, Is.EqualTo(bvi.KeyObject));
			CheckDebugId(useNode,"EtymologyComment2");
			// But this column has no structural nodes.
			Assert.That(collectStructNodes.Count, Is.EqualTo(0));
		}

		/// <summary>
		/// Test generating what to display for BVI with two-level seq path.
		/// </summary>
		[Test]
		public void DisplayDoubleSeqPathObject()
		{
			ArrayList list = new ArrayList();
			XmlNode column = m_columnList[5];
			XmlViewsUtils.CollectBrowseItems(6, column, list, null, m_mdc, m_sda, m_layouts);
			IManyOnePathSortItem bvi = list[0] as IManyOnePathSortItem;

			// Try on first column. Nothing in the path matches, but we still dig inside
			// the span.
			int useHvo;
			List<XmlNode> collectStructNodes = new List<XmlNode>();
			XmlNode useNode = XmlViewsUtils.GetNodeToUseForColumn(bvi, m_columnList[0], null,
				m_mdc, m_sda, m_layouts, out useHvo, collectStructNodes);
			Assert.That(useHvo, Is.EqualTo(6));
			CheckDebugId(useNode, "LexemeCf");
			Assert.That(collectStructNodes.Count, Is.EqualTo(1));
			CheckDebugId(collectStructNodes[0], "LexemeSpan");

			// Try on etymology column. Has an <obj>, but doens't match
			collectStructNodes.Clear();
			useNode = XmlViewsUtils.GetNodeToUseForColumn(bvi, m_columnList[1], null,
				m_mdc, m_sda, m_layouts, out useHvo, collectStructNodes);
			Assert.That(useHvo, Is.EqualTo(6));
			CheckDebugId(useNode, "EtymologyObj");
			Assert.That(collectStructNodes.Count, Is.EqualTo(1));
			XmlNode structNode1 = collectStructNodes[0];
			CheckDebugId(structNode1, "EtymologySpan");

			// Try on a column involving a lookup. This affects the node output.
			collectStructNodes.Clear();
			useNode = XmlViewsUtils.GetNodeToUseForColumn(bvi, m_columnList[2], null,
				m_mdc, m_sda, m_layouts, out useHvo, collectStructNodes);
			Assert.That(useHvo, Is.EqualTo(6));
			CheckDebugId(useNode, "EntryMsaSeq");
			Assert.That(collectStructNodes.Count, Is.EqualTo(1));
			structNode1 = collectStructNodes[0];
			CheckDebugId(structNode1, "EntryMsasDiv");

			// On the matching column, we should get the leaf object
			collectStructNodes.Clear();
			useNode = XmlViewsUtils.GetNodeToUseForColumn(bvi, m_columnList[5], null,
				m_mdc, m_sda, m_layouts, out useHvo, collectStructNodes);
			Assert.That(useHvo, Is.EqualTo(bvi.KeyObject));
			CheckDebugId(useNode,"PACN_Para");
			Assert.That(collectStructNodes.Count, Is.EqualTo(1));
			structNode1 = collectStructNodes[0];
			CheckDebugId(structNode1, "DosDiv");

			// On the gloss column, we get the sense.
			collectStructNodes.Clear();
			useNode = XmlViewsUtils.GetNodeToUseForColumn(bvi, m_columnList[3], null,
				m_mdc, m_sda, m_layouts, out useHvo, collectStructNodes);
			Assert.That(useHvo, Is.EqualTo(7)); // the first sense
			CheckDebugId(useNode,"SenseGloss");
			Assert.That(collectStructNodes.Count, Is.EqualTo(1));
			structNode1 = collectStructNodes[0];
			CheckDebugId(structNode1, "SenseGlossPara");

			// Make sure that for later Bvis, we get later senses
			collectStructNodes.Clear();
			bvi = list[3] as IManyOnePathSortItem;
			useNode = XmlViewsUtils.GetNodeToUseForColumn(bvi, m_columnList[3], null,
				m_mdc, m_sda, m_layouts, out useHvo, collectStructNodes);
			Assert.That(useHvo, Is.EqualTo(9)); // the third sense, in which context we display the 4th SD
		}
	}
}
