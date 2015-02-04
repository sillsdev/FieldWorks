// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml;

using NUnit.Framework;

using SIL.CoreImpl;
using SIL.FieldWorks.CacheLight;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
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
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
	public class TestManyOneBrowse : SIL.FieldWorks.Test.TestUtils.BaseTest
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
		[TestFixtureSetUp]
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
			m_sda = m_cda as ISilDataAccess;
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
			Assert.AreEqual(3, m_sda.get_ObjectProp(2, 23011), "part of speech of an MoStemMsa");
			Assert.AreEqual(2, m_sda.get_VecItem(1, 2009, 0), "owned msa");
			Assert.AreEqual("noun", m_sda.get_MultiStringAlt(3, 7003, wsEn).Text, "got ms property");
			Assert.AreEqual(9, m_sda.get_VecItem(6, 2010, 2), "3rd sense");
			Assert.AreEqual(31, m_sda.get_VecItem(9, 21016, 1), "2nd semantic domain");

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

		[TestFixtureTearDown]
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
			XmlViewsUtils.CollectBrowseItems(1, column, list, m_mdc, m_sda, m_layouts);
			Assert.AreEqual(1, list.Count, "got one item for lexeme obj 1");
			IManyOnePathSortItem bv = list[0] as IManyOnePathSortItem;
			Assert.AreEqual(1, bv.KeyObject);
			Assert.AreEqual(0, bv.PathLength);
			list.Clear();
			XmlViewsUtils.CollectBrowseItems(4, column, list, m_mdc, m_sda, m_layouts);
			Assert.AreEqual(1, list.Count, "got one item for lexeme obj 4");
			list.Clear();
			XmlViewsUtils.CollectBrowseItems(6, column, list, m_mdc, m_sda, m_layouts);
			Assert.AreEqual(1, list.Count, "got one item for lexeme obj 6");
			bv = list[0] as IManyOnePathSortItem;
			Assert.AreEqual(6, bv.KeyObject);
			Assert.AreEqual(0, bv.PathLength);
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
			XmlViewsUtils.CollectBrowseItems(1, column, list, m_mdc, m_sda, m_layouts);
			Assert.AreEqual(1, list.Count, "got one item for etymology obj 1");
			IManyOnePathSortItem bv = list[0] as IManyOnePathSortItem;
			Assert.AreEqual(60, bv.KeyObject);
			Assert.AreEqual(1, bv.PathLength);
			Assert.AreEqual(1, bv.PathObject(0));
			Assert.AreEqual(2011, bv.PathFlid(0));
			list.Clear();
			XmlViewsUtils.CollectBrowseItems(4, column, list, m_mdc, m_sda, m_layouts);
			Assert.AreEqual(1, list.Count, "got one item for etymology obj 4");
			list.Clear();
			XmlViewsUtils.CollectBrowseItems(6, column, list, m_mdc, m_sda, m_layouts);
			Assert.AreEqual(1, list.Count, "got one item for etymology obj 6");
			bv = list[0] as IManyOnePathSortItem;
			Assert.AreEqual(61, bv.KeyObject);
			Assert.AreEqual(1, bv.PathLength);
			Assert.AreEqual(6, bv.PathObject(0));
			Assert.AreEqual(2011, bv.PathFlid(0));
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
			XmlViewsUtils.CollectBrowseItems(1, column, list, m_mdc, m_sda, m_layouts);
			Assert.AreEqual(1, list.Count, "got one items for glosses obj 1");
			list.Clear();
			XmlViewsUtils.CollectBrowseItems(4, column, list, m_mdc, m_sda, m_layouts);
			Assert.AreEqual(1, list.Count, "got one item for glosses obj 4");
			IManyOnePathSortItem bv = list[0] as IManyOnePathSortItem;
			Assert.AreEqual(5, bv.KeyObject);
			Assert.AreEqual(1, bv.PathLength);
			Assert.AreEqual(4, bv.PathObject(0));
			Assert.AreEqual(2010, bv.PathFlid(0));
			list.Clear();
			XmlViewsUtils.CollectBrowseItems(6, column, list, m_mdc, m_sda, m_layouts);
			Assert.AreEqual(3, list.Count, "got three items for glosses obj 6");
			int[] keys = new int[] {7, 8, 9};
			for (int i = 0; i < keys.Length; i++)
			{
				bv = list[i] as IManyOnePathSortItem;
				Assert.AreEqual(keys[i], bv.KeyObject);
				Assert.AreEqual(1, bv.PathLength);
				Assert.AreEqual(6, bv.PathObject(0));
				Assert.AreEqual(2010, bv.PathFlid(0));
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
			XmlViewsUtils.CollectBrowseItems(1, column, list, m_mdc, m_sda, m_layouts);
			Assert.AreEqual(1, list.Count, "got one item for SD obj 1"); // no senses!
			list.Clear();
			XmlViewsUtils.CollectBrowseItems(4, column, list, m_mdc, m_sda, m_layouts);
			Assert.AreEqual(1, list.Count, "got one item for SD obj 4"); // sense 5 has no SDs
			list.Clear();
			// Senses 7, 8, 9, having SDs 7->30, 8->31, and 9->30, 31, 32
			XmlViewsUtils.CollectBrowseItems(6, column, list, m_mdc, m_sda, m_layouts);
			Assert.AreEqual(5, list.Count, "got five items for SD obj 6");
			int[] keys = new int[] {30, 31, 30, 31, 32};
			int[] keys2 = new int[] {7, 8, 9, 9, 9};
			for (int i = 0; i < keys.Length; i++)
			{
				bv = list[i] as IManyOnePathSortItem;
				Assert.AreEqual(keys[i], bv.KeyObject);
				Assert.AreEqual(2, bv.PathLength);
				Assert.AreEqual(6, bv.PathObject(0));
				Assert.AreEqual(2010, bv.PathFlid(0)); // LexEntry.Senses
				Assert.AreEqual(keys2[i], bv.PathObject(1));
				Assert.AreEqual(21016, bv.PathFlid(1)); // LexSense.SemanticDomain
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
			XmlViewsUtils.CollectBrowseItems(1, column, list, m_mdc, m_sda, m_layouts);
			IManyOnePathSortItem bvi = list[0] as IManyOnePathSortItem;

			// Try on original column. We get original object since there's no path,
			// but we still dig inside the span
			int useHvo;
			List<XmlNode> collectStructNodes = new List<XmlNode>();
			XmlNode useNode = XmlViewsUtils.GetNodeToUseForColumn(bvi, m_columnList[0],
				m_mdc, m_sda, m_layouts, out useHvo, collectStructNodes);
			Assert.AreEqual(1, useHvo);
			CheckDebugId(useNode, "LexemeCf");
			Assert.AreEqual(1, collectStructNodes.Count);
			CheckDebugId(collectStructNodes[0], "LexemeSpan");

			// Try on another column. Again we get original object, and dig inside span
			collectStructNodes.Clear();
			useNode = XmlViewsUtils.GetNodeToUseForColumn(bvi, m_columnList[1],
				m_mdc, m_sda, m_layouts, out useHvo, collectStructNodes);
			Assert.AreEqual(1, useHvo);
			CheckDebugId(useNode, "EtymologyObj");
			Assert.AreEqual(1, collectStructNodes.Count);
			XmlNode structNode1 = collectStructNodes[0];
			CheckDebugId(structNode1, "EtymologySpan");

			// Try on a column involving a lookup. This affects the node output.
			collectStructNodes.Clear();
			useNode = XmlViewsUtils.GetNodeToUseForColumn(bvi, m_columnList[2],
				m_mdc, m_sda, m_layouts, out useHvo, collectStructNodes);
			Assert.AreEqual(1, useHvo);
			CheckDebugId(useNode, "EntryMsaSeq");
			Assert.AreEqual(1, collectStructNodes.Count);
			structNode1 = collectStructNodes[0];
			CheckDebugId(structNode1, "EntryMsasDiv");
		}

		void CheckDebugId(XmlNode node, string id)
		{
			Assert.AreEqual(id, XmlUtils.GetOptionalAttributeValue(node, "debugId"));
		}

		/// <summary>
		/// Test generating what to display for BVI with one atomic object path.
		/// </summary>
		[Test]
		public void DisplayAtomicPathObject()
		{
			ArrayList list = new ArrayList();
			XmlNode column = m_columnList[1];
			XmlViewsUtils.CollectBrowseItems(1, column, list, m_mdc, m_sda, m_layouts);
			IManyOnePathSortItem bvi = list[0] as IManyOnePathSortItem;

			// Try on first column. Nothing in the path matches, but we still dig inside
			// the span.
			int useHvo;
			List<XmlNode> collectStructNodes = new List<XmlNode>();
			XmlNode useNode = XmlViewsUtils.GetNodeToUseForColumn(bvi, m_columnList[0],
				m_mdc, m_sda, m_layouts, out useHvo, collectStructNodes);
			Assert.AreEqual(1, useHvo);
			CheckDebugId(useNode, "LexemeCf");
			Assert.AreEqual(1, collectStructNodes.Count);
			CheckDebugId(collectStructNodes[0], "LexemeSpan");

			// Try on matching column.
			collectStructNodes.Clear();
			useNode = XmlViewsUtils.GetNodeToUseForColumn(bvi, m_columnList[1],
				m_mdc, m_sda, m_layouts, out useHvo, collectStructNodes);
			Assert.AreEqual(bvi.KeyObject, useHvo);
			CheckDebugId(useNode, "EtymologyComment");
			Assert.AreEqual(1, collectStructNodes.Count);
			XmlNode structNode1 = collectStructNodes[0];
			CheckDebugId(structNode1, "EtymologySpan");

			// Try on a column involving a lookup. This affects the node output.
			collectStructNodes.Clear();
			useNode = XmlViewsUtils.GetNodeToUseForColumn(bvi, m_columnList[2],
				m_mdc, m_sda, m_layouts, out useHvo, collectStructNodes);
			Assert.AreEqual(1, useHvo);
			CheckDebugId(useNode, "EntryMsaSeq");
			Assert.AreEqual(1, collectStructNodes.Count);
			structNode1 = collectStructNodes[0];
			CheckDebugId(structNode1, "EntryMsasDiv");

			// On a different view of the Etymology, we should still get the Etymology object.
			collectStructNodes.Clear();
			useNode = XmlViewsUtils.GetNodeToUseForColumn(bvi, m_columnList[6],
				m_mdc, m_sda, m_layouts, out useHvo, collectStructNodes);
			Assert.AreEqual(bvi.KeyObject, useHvo);
			CheckDebugId(useNode,"EtymologyComment2");
			// But this column has no structural nodes.
			Assert.AreEqual(0, collectStructNodes.Count);
		}

		/// <summary>
		/// Test generating what to display for BVI with two-level seq path.
		/// </summary>
		[Test]
		public void DisplayDoubleSeqPathObject()
		{
			ArrayList list = new ArrayList();
			XmlNode column = m_columnList[5];
			XmlViewsUtils.CollectBrowseItems(6, column, list, m_mdc, m_sda, m_layouts);
			IManyOnePathSortItem bvi = list[0] as IManyOnePathSortItem;

			// Try on first column. Nothing in the path matches, but we still dig inside
			// the span.
			int useHvo;
			List<XmlNode> collectStructNodes = new List<XmlNode>();
			XmlNode useNode = XmlViewsUtils.GetNodeToUseForColumn(bvi, m_columnList[0],
				m_mdc, m_sda, m_layouts, out useHvo, collectStructNodes);
			Assert.AreEqual(6, useHvo);
			CheckDebugId(useNode, "LexemeCf");
			Assert.AreEqual(1, collectStructNodes.Count);
			CheckDebugId(collectStructNodes[0], "LexemeSpan");

			// Try on etymology column. Has an <obj>, but doens't match
			collectStructNodes.Clear();
			useNode = XmlViewsUtils.GetNodeToUseForColumn(bvi, m_columnList[1],
				m_mdc, m_sda, m_layouts, out useHvo, collectStructNodes);
			Assert.AreEqual(6, useHvo);
			CheckDebugId(useNode, "EtymologyObj");
			Assert.AreEqual(1, collectStructNodes.Count);
			XmlNode structNode1 = collectStructNodes[0];
			CheckDebugId(structNode1, "EtymologySpan");

			// Try on a column involving a lookup. This affects the node output.
			collectStructNodes.Clear();
			useNode = XmlViewsUtils.GetNodeToUseForColumn(bvi, m_columnList[2],
				m_mdc, m_sda, m_layouts, out useHvo, collectStructNodes);
			Assert.AreEqual(6, useHvo);
			CheckDebugId(useNode, "EntryMsaSeq");
			Assert.AreEqual(1, collectStructNodes.Count);
			structNode1 = collectStructNodes[0];
			CheckDebugId(structNode1, "EntryMsasDiv");

			// On the matching column, we should get the leaf object
			collectStructNodes.Clear();
			useNode = XmlViewsUtils.GetNodeToUseForColumn(bvi, m_columnList[5],
				m_mdc, m_sda, m_layouts, out useHvo, collectStructNodes);
			Assert.AreEqual(bvi.KeyObject, useHvo);
			CheckDebugId(useNode,"PACN_Para");
			Assert.AreEqual(1, collectStructNodes.Count);
			structNode1 = collectStructNodes[0];
			CheckDebugId(structNode1, "DosDiv");

			// On the gloss column, we get the sense.
			collectStructNodes.Clear();
			useNode = XmlViewsUtils.GetNodeToUseForColumn(bvi, m_columnList[3],
				m_mdc, m_sda, m_layouts, out useHvo, collectStructNodes);
			Assert.AreEqual(7, useHvo); // the first sense
			CheckDebugId(useNode,"SenseGloss");
			Assert.AreEqual(1, collectStructNodes.Count);
			structNode1 = collectStructNodes[0];
			CheckDebugId(structNode1, "SenseGlossPara");

			// Make sure that for later Bvis, we get later senses
			collectStructNodes.Clear();
			bvi = list[3] as IManyOnePathSortItem;
			useNode = XmlViewsUtils.GetNodeToUseForColumn(bvi, m_columnList[3],
				m_mdc, m_sda, m_layouts, out useHvo, collectStructNodes);
			Assert.AreEqual(9, useHvo); // the third sense, in which context we display the 4th SD
		}
	}
}
