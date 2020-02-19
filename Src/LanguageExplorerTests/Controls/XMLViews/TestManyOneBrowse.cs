// Copyright (c) 2010-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml;
using System.Xml.Linq;
using LanguageExplorer;
using LanguageExplorer.Controls.XMLViews;
using LanguageExplorer.TestUtilities;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.Xml;

namespace LanguageExplorerTests.Controls.XMLViews
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
		private IList<XElement> m_columnList;
		private Inventory m_layoutInventory;
		private Inventory m_partInventory;
		private LayoutCache m_layouts;
		private WritingSystemManager m_wsManager;
		private IVwCacheDa m_cda;

		private static string PathToXmlViewsTests => Path.Combine(FwDirectoryFinder.SourceDirectory, "LanguageExplorerTests", "Controls", "XMLViews");

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
			var testPath = Path.Combine(PathToXmlViewsTests, "SampleCm.xml");
			m_mdc = MetaDataCache.CreateMetaDataCache(testPath);
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
			m_sda = (ISilDataAccess)m_cda;
			m_wsManager = new WritingSystemManager();
			m_sda.WritingSystemFactory = m_wsManager;
			var parser = new SimpleDataParser(m_mdc, m_cda);
			parser.Parse(Path.Combine(PathToXmlViewsTests, "SampleData.xml"));
			var wsEn = m_wsManager.GetWsFromStr("en");
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
			var docColumns = XDocument.Load(Path.Combine(PathToXmlViewsTests, "TestColumns.xml"));
			m_columnList = docColumns.Root.Elements().ToList();
			// Parts just has what those columns need.
			var partDirectory = PathToXmlViewsTests;
			var keyAttrs = new Dictionary<string, string[]>
			{
				["layout"] = new[] { "class", "type", "name" },
				["group"] = new[] { "label" },
				["part"] = new[] { "ref" }
			};
			// Currently there are no specialized layout files that match.
			m_layoutInventory = new Inventory(new[] { partDirectory }, "*.fwlayout", "/LayoutInventory/*", keyAttrs, "TestManyOneBrowse", "ProjectPath");
			keyAttrs = new Dictionary<string, string[]>
			{
				["part"] = new[] { "id" }
			};
			m_partInventory = new Inventory(new[] { partDirectory }, "TestParts.xml", "/PartInventory/bin/*", keyAttrs, "TestManyOneBrowse", "ProjectPath");
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
			var list = new List<IManyOnePathSortItem>();
			var column = m_columnList[0];
			XmlViewsUtils.CollectBrowseItems(1, column, list, m_mdc, m_sda, m_layouts);
			Assert.AreEqual(1, list.Count, "got one item for lexeme obj 1");
			var bv = list[0];
			Assert.AreEqual(1, bv.KeyObject);
			Assert.AreEqual(0, bv.PathLength);
			list.Clear();
			XmlViewsUtils.CollectBrowseItems(4, column, list, m_mdc, m_sda, m_layouts);
			Assert.AreEqual(1, list.Count, "got one item for lexeme obj 4");
			list.Clear();
			XmlViewsUtils.CollectBrowseItems(6, column, list, m_mdc, m_sda, m_layouts);
			Assert.AreEqual(1, list.Count, "got one item for lexeme obj 6");
			bv = list[0];
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
			var list = new List<IManyOnePathSortItem>();
			var column = m_columnList[1]; // Etymology
			XmlViewsUtils.CollectBrowseItems(1, column, list, m_mdc, m_sda, m_layouts);
			Assert.AreEqual(1, list.Count, "got one item for etymology obj 1");
			var bv = list[0];
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
			bv = list[0];
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
			var list = new List<IManyOnePathSortItem>();
			var column = m_columnList[3]; // Glosses
			XmlViewsUtils.CollectBrowseItems(1, column, list, m_mdc, m_sda, m_layouts);
			Assert.AreEqual(1, list.Count, "got one items for glosses obj 1");
			list.Clear();
			XmlViewsUtils.CollectBrowseItems(4, column, list, m_mdc, m_sda, m_layouts);
			Assert.AreEqual(1, list.Count, "got one item for glosses obj 4");
			var bv = list[0];
			Assert.AreEqual(5, bv.KeyObject);
			Assert.AreEqual(1, bv.PathLength);
			Assert.AreEqual(4, bv.PathObject(0));
			Assert.AreEqual(2010, bv.PathFlid(0));
			list.Clear();
			XmlViewsUtils.CollectBrowseItems(6, column, list, m_mdc, m_sda, m_layouts);
			Assert.AreEqual(3, list.Count, "got three items for glosses obj 6");
			int[] keys = { 7, 8, 9 };
			for (var i = 0; i < keys.Length; i++)
			{
				bv = list[i];
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
			var list = new List<IManyOnePathSortItem>();
			IManyOnePathSortItem bv;
			var column = m_columnList[5]; // Semantic domains
			XmlViewsUtils.CollectBrowseItems(1, column, list, m_mdc, m_sda, m_layouts);
			Assert.AreEqual(1, list.Count, "got one item for SD obj 1"); // no senses!
			list.Clear();
			XmlViewsUtils.CollectBrowseItems(4, column, list, m_mdc, m_sda, m_layouts);
			Assert.AreEqual(1, list.Count, "got one item for SD obj 4"); // sense 5 has no SDs
			list.Clear();
			// Senses 7, 8, 9, having SDs 7->30, 8->31, and 9->30, 31, 32
			XmlViewsUtils.CollectBrowseItems(6, column, list, m_mdc, m_sda, m_layouts);
			Assert.AreEqual(5, list.Count, "got five items for SD obj 6");
			int[] keys = { 30, 31, 30, 31, 32 };
			int[] keys2 = { 7, 8, 9, 9, 9 };
			for (var i = 0; i < keys.Length; i++)
			{
				bv = list[i];
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
			var list = new List<IManyOnePathSortItem>();
			var column = m_columnList[0];
			XmlViewsUtils.CollectBrowseItems(1, column, list, m_mdc, m_sda, m_layouts);
			var bvi = list[0];

			// Try on original column. We get original object since there's no path,
			// but we still dig inside the span
			int useHvo;
			var collectStructNodes = new List<XElement>();
			var useNode = XmlViewsUtils.GetNodeToUseForColumn(bvi, m_columnList[0], m_mdc, m_sda, m_layouts, out useHvo, collectStructNodes);
			Assert.AreEqual(1, useHvo);
			CheckDebugId(useNode, "LexemeCf");
			Assert.AreEqual(1, collectStructNodes.Count);
			CheckDebugId(collectStructNodes[0], "LexemeSpan");

			// Try on another column. Again we get original object, and dig inside span
			collectStructNodes.Clear();
			useNode = XmlViewsUtils.GetNodeToUseForColumn(bvi, m_columnList[1], m_mdc, m_sda, m_layouts, out useHvo, collectStructNodes);
			Assert.AreEqual(1, useHvo);
			CheckDebugId(useNode, "EtymologyObj");
			Assert.AreEqual(1, collectStructNodes.Count);
			var structNode1 = collectStructNodes[0];
			CheckDebugId(structNode1, "EtymologySpan");

			// Try on a column involving a lookup. This affects the node output.
			collectStructNodes.Clear();
			useNode = XmlViewsUtils.GetNodeToUseForColumn(bvi, m_columnList[2], m_mdc, m_sda, m_layouts, out useHvo, collectStructNodes);
			Assert.AreEqual(1, useHvo);
			CheckDebugId(useNode, "EntryMsaSeq");
			Assert.AreEqual(1, collectStructNodes.Count);
			structNode1 = collectStructNodes[0];
			CheckDebugId(structNode1, "EntryMsasDiv");
		}

		private static void CheckDebugId(XElement node, string id)
		{
			Assert.AreEqual(id, XmlUtils.GetOptionalAttributeValue(node, "debugId"));
		}

		/// <summary>
		/// Test generating what to display for BVI with one atomic object path.
		/// </summary>
		[Test]
		public void DisplayAtomicPathObject()
		{
			var list = new List<IManyOnePathSortItem>();
			var column = m_columnList[1];
			XmlViewsUtils.CollectBrowseItems(1, column, list, m_mdc, m_sda, m_layouts);
			var bvi = list[0];

			// Try on first column. Nothing in the path matches, but we still dig inside
			// the span.
			int useHvo;
			var collectStructNodes = new List<XElement>();
			var useNode = XmlViewsUtils.GetNodeToUseForColumn(bvi, m_columnList[0], m_mdc, m_sda, m_layouts, out useHvo, collectStructNodes);
			Assert.AreEqual(1, useHvo);
			CheckDebugId(useNode, "LexemeCf");
			Assert.AreEqual(1, collectStructNodes.Count);
			CheckDebugId(collectStructNodes[0], "LexemeSpan");

			// Try on matching column.
			collectStructNodes.Clear();
			useNode = XmlViewsUtils.GetNodeToUseForColumn(bvi, m_columnList[1], m_mdc, m_sda, m_layouts, out useHvo, collectStructNodes);
			Assert.AreEqual(bvi.KeyObject, useHvo);
			CheckDebugId(useNode, "EtymologyComment");
			Assert.AreEqual(1, collectStructNodes.Count);
			var structNode1 = collectStructNodes[0];
			CheckDebugId(structNode1, "EtymologySpan");

			// Try on a column involving a lookup. This affects the node output.
			collectStructNodes.Clear();
			useNode = XmlViewsUtils.GetNodeToUseForColumn(bvi, m_columnList[2], m_mdc, m_sda, m_layouts, out useHvo, collectStructNodes);
			Assert.AreEqual(1, useHvo);
			CheckDebugId(useNode, "EntryMsaSeq");
			Assert.AreEqual(1, collectStructNodes.Count);
			structNode1 = collectStructNodes[0];
			CheckDebugId(structNode1, "EntryMsasDiv");

			// On a different view of the Etymology, we should still get the Etymology object.
			collectStructNodes.Clear();
			useNode = XmlViewsUtils.GetNodeToUseForColumn(bvi, m_columnList[6], m_mdc, m_sda, m_layouts, out useHvo, collectStructNodes);
			Assert.AreEqual(bvi.KeyObject, useHvo);
			CheckDebugId(useNode, "EtymologyComment2");
			// But this column has no structural nodes.
			Assert.AreEqual(0, collectStructNodes.Count);
		}

		/// <summary>
		/// Test generating what to display for BVI with two-level seq path.
		/// </summary>
		[Test]
		public void DisplayDoubleSeqPathObject()
		{
			var list = new List<IManyOnePathSortItem>();
			var column = m_columnList[5];
			XmlViewsUtils.CollectBrowseItems(6, column, list, m_mdc, m_sda, m_layouts);
			var bvi = list[0];

			// Try on first column. Nothing in the path matches, but we still dig inside
			// the span.
			int useHvo;
			var collectStructNodes = new List<XElement>();
			var useNode = XmlViewsUtils.GetNodeToUseForColumn(bvi, m_columnList[0], m_mdc, m_sda, m_layouts, out useHvo, collectStructNodes);
			Assert.AreEqual(6, useHvo);
			CheckDebugId(useNode, "LexemeCf");
			Assert.AreEqual(1, collectStructNodes.Count);
			CheckDebugId(collectStructNodes[0], "LexemeSpan");

			// Try on etymology column. Has an <obj>, but doesn't match
			collectStructNodes.Clear();
			useNode = XmlViewsUtils.GetNodeToUseForColumn(bvi, m_columnList[1], m_mdc, m_sda, m_layouts, out useHvo, collectStructNodes);
			Assert.AreEqual(6, useHvo);
			CheckDebugId(useNode, "EtymologyObj");
			Assert.AreEqual(1, collectStructNodes.Count);
			var structNode1 = collectStructNodes[0];
			CheckDebugId(structNode1, "EtymologySpan");

			// Try on a column involving a lookup. This affects the node output.
			collectStructNodes.Clear();
			useNode = XmlViewsUtils.GetNodeToUseForColumn(bvi, m_columnList[2], m_mdc, m_sda, m_layouts, out useHvo, collectStructNodes);
			Assert.AreEqual(6, useHvo);
			CheckDebugId(useNode, "EntryMsaSeq");
			Assert.AreEqual(1, collectStructNodes.Count);
			structNode1 = collectStructNodes[0];
			CheckDebugId(structNode1, "EntryMsasDiv");

			// On the matching column, we should get the leaf object
			collectStructNodes.Clear();
			useNode = XmlViewsUtils.GetNodeToUseForColumn(bvi, m_columnList[5], m_mdc, m_sda, m_layouts, out useHvo, collectStructNodes);
			Assert.AreEqual(bvi.KeyObject, useHvo);
			CheckDebugId(useNode, "PACN_Para");
			Assert.AreEqual(1, collectStructNodes.Count);
			structNode1 = collectStructNodes[0];
			CheckDebugId(structNode1, "DosDiv");

			// On the gloss column, we get the sense.
			collectStructNodes.Clear();
			useNode = XmlViewsUtils.GetNodeToUseForColumn(bvi, m_columnList[3], m_mdc, m_sda, m_layouts, out useHvo, collectStructNodes);
			Assert.AreEqual(7, useHvo); // the first sense
			CheckDebugId(useNode, "SenseGloss");
			Assert.AreEqual(1, collectStructNodes.Count);
			structNode1 = collectStructNodes[0];
			CheckDebugId(structNode1, "SenseGlossPara");

			// Make sure that for later Bvis, we get later senses
			collectStructNodes.Clear();
			bvi = list[3];
			useNode = XmlViewsUtils.GetNodeToUseForColumn(bvi, m_columnList[3], m_mdc, m_sda, m_layouts, out useHvo, collectStructNodes);
			Assert.AreEqual(9, useHvo); // the third sense, in which context we display the 4th SD
		}

		/// <summary>
		/// SimpleDataParser parses a simple XML representation of some FieldWorks-type data
		/// into an IVwCacheDa, with minimal checking.
		/// </summary>
		private sealed class SimpleDataParser
		{
			private readonly IFwMetaDataCache m_mdc;
			private readonly IVwCacheDa m_cda;
			private readonly ISilDataAccess m_sda;
			private readonly ILgWritingSystemFactory m_wsf;

			public SimpleDataParser(IFwMetaDataCache mdc, IVwCacheDa cda)
			{
				m_mdc = mdc;
				m_cda = cda;
				m_sda = (ISilDataAccess)cda;
				m_wsf = m_sda.WritingSystemFactory;
			}

			public void Parse(string pathname)
			{
				var docSrc = new XmlDocument();
				docSrc.Load(pathname);
				Parse(docSrc.DocumentElement);
			}

			private List<int> Parse(XmlNode root)
			{
				var result = new List<int>(root.ChildNodes.Count);
				foreach (XmlNode elt in root.ChildNodes)
				{
					if (elt is XmlComment)
					{
						continue;
					}
					switch (elt.Name)
					{
						case "relatomic":
							SetAtomicRef(elt);
							break;
						case "relseq":
							SetSeqRef(elt);
							break;
						default:
							result.Add(MakeObject(elt));
							break;
					}
				}
				return result;
			}

			private void SetAtomicRef(XmlNode elt)
			{
				var src = GetSource(elt);
				var dst = GetDst(elt);
				var flid = GetProp(src, elt);
				m_cda.CacheObjProp(src, flid, dst);
			}

			private void SetSeqRef(XmlNode elt)
			{
				var src = GetSource(elt);
				var flid = GetProp(src, elt);
				var dst = new List<int>();
				foreach (XmlNode child in elt.ChildNodes)
				{
					if (child is XmlComment)
					{
						continue;
					}
					dst.Add(GetDst(child));
				}
				m_cda.CacheVecProp(src, flid, dst.ToArray(), dst.Count);
			}

			private static int GetSource(XmlNode elt)
			{
				return XmlUtils.GetMandatoryIntegerAttributeValue(elt, "src");
			}

			private static int GetDst(XmlNode elt)
			{
				return XmlUtils.GetMandatoryIntegerAttributeValue(elt, "dst");
			}

			private static int GetId(XmlNode elt)
			{
				return XmlUtils.GetMandatoryIntegerAttributeValue(elt, "id");
			}

			private int GetProp(int hvo, XmlNode elt)
			{
				var propName = XmlUtils.GetMandatoryAttributeValue(elt, "prop");
				var clsid = m_sda.get_IntProp(hvo, (int)CmObjectFields.kflidCmObject_Class);
				return m_mdc.GetFieldId2(clsid, propName, true);
			}

			private int MakeObject(XmlNode elt)
			{
				var className = elt.Name;
				var clid = m_mdc.GetClassId(className);
				if (clid == 0)
				{
					throw new Exception("class not found " + className);
				}
				var hvo = GetId(elt);
				m_cda.CacheIntProp(hvo, (int)CmObjectFields.kflidCmObject_Class, (int)clid);
				foreach (XmlNode child in elt.ChildNodes)
				{
					if (child is XmlComment)
					{
						continue;
					}
					switch (child.Name)
					{
						case "seq":
							AddOwningSeqProp(hvo, child);
							break;
						case "ms":
							AddMultiStringProp(hvo, child);
							break;
						case "obj":
							AddOwningAtomicProp(hvo, child);
							break;
						default:
							throw new Exception("unexpected element " + child.Name + " found in " + className);
					}
				}
				return hvo;
			}

			private void AddOwningSeqProp(int hvo, XmlNode seq)
			{
				var items = Parse(seq);
				m_cda.CacheVecProp(hvo, GetProp(hvo, seq), items.ToArray(), items.Count);
			}

			private void AddOwningAtomicProp(int hvo, XmlNode objElt)
			{
				var items = Parse(objElt);
				if (items.Count > 1)
				{
					throw new Exception("<obj> element may only contain one object");
				}
				var hvoVal = 0;
				if (items.Count > 0)
				{
					hvoVal = items[0];
				}
				m_cda.CacheObjProp(hvo, GetProp(hvo, objElt), hvoVal);
			}

			private static ITsString MakeString(int ws, XmlNode elt)
			{
				return TsStringUtils.MakeString(XmlUtils.GetMandatoryAttributeValue(elt, "val"), ws);
			}

			private int GetWritingSystem(XmlNode elt)
			{
				var wsId = XmlUtils.GetMandatoryAttributeValue(elt, "ws");
				var ws = m_wsf.get_Engine(wsId).Handle;
				if (ws == 0)
				{
					throw new Exception($"writing system {wsId} not recognized");
				}
				return ws;
			}

			private void AddMultiStringProp(int hvo, XmlNode elt)
			{
				var ws = GetWritingSystem(elt);
				m_cda.CacheStringAlt(hvo, GetProp(hvo, elt), ws, MakeString(ws, elt));
			}
		}
	}
}