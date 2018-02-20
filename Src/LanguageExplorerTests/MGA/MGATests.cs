// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.IO;
using System.Xml;
using LanguageExplorer.MGA;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.WritingSystems;
using SIL.Xml;

namespace LanguageExplorerTests.MGA
{
	/// <summary>
	/// Test sets for the GlossListBox class.
	/// </summary>
	[TestFixture]
	public class GlossListBoxTest : MemoryOnlyBackendProviderTestBase
	{
		private GlossListBox m_LabelGlosses;
		private XmlDocument m_doc;

		public override void FixtureSetup()
		{
			if (!Sldr.IsInitialized)
			{
				// initialize the SLDR
				Sldr.Initialize();
			}

			base.FixtureSetup();
		}

		public override void FixtureTeardown()
		{
			base.FixtureTeardown();

			if (Sldr.IsInitialized)
			{
				Sldr.Cleanup();
			}
		}

		/// <summary>
		/// This method is called before each test
		/// </summary>
		public override void TestSetup()
		{
			base.TestSetup();

			var sXmlFile = Path.Combine(FwDirectoryFinder.CodeDirectory, "Language Explorer", "MGA", "GlossLists", "EticGlossList.xml");
			m_doc = new XmlDocument();
			m_doc.Load(sXmlFile);
			m_LabelGlosses = new GlossListBox
			{
				Sorted = false
			};
			var node = m_doc.SelectSingleNode("//item[@id='vPositive']");
			var glbi = new GlossListBoxItem(Cache, node, ".", string.Empty, false);
			m_LabelGlosses.Items.Add(glbi);
		}

		/// <summary>
		/// This method is called after each test
		/// </summary>
		[TearDown]
		public override void TestTearDown()
		{
			m_LabelGlosses.Dispose();

			base.TestTearDown();
		}

		[Test]
		public void GlossListBoxCountTest()
		{
			Assert.AreEqual(1, this.m_LabelGlosses.Items.Count);
		}
		[Test]
		public void GlossListBoxContentTest()
		{
			Assert.AreEqual("positive: pos", this.m_LabelGlosses.Items[0].ToString());
		}
		[Test]
		public void GlossListItemConflicts()
		{
			// check another terminal node, but with different parent, so no conflict
			var node = m_doc.SelectSingleNode("//item[@id='cAdjAgr']/item[@target='tCommonAgr']/item[@target='fGender']/item[@target='vMasc']");
			var glbiNew = new GlossListBoxItem(Cache, node, ".", "", false);
			GlossListBoxItem glbiConflict;
			var fResult = m_LabelGlosses.NewItemConflictsWithExtantItem(glbiNew, out glbiConflict);
			var sMsg = glbiConflict != null ? $"Masculine gender should not conflict, but did with {glbiConflict.Abbrev}." : "Masculine gender should not conflict";
			Assert.IsFalse(fResult, sMsg);
			// check a non-terminal node, so no conflict
			node = m_doc.SelectSingleNode("//item[@id='fDeg']");
			glbiNew = new GlossListBoxItem(Cache, node, ".", "", false);
			fResult = m_LabelGlosses.NewItemConflictsWithExtantItem(glbiNew, out glbiConflict);
			sMsg = glbiConflict != null ? $"Feature degree should not conflict, but did with {glbiConflict.Abbrev}" : "Feature degree should not conflict";
			Assert.IsFalse(fResult, sMsg);
			// check another terminal node with same parent, so there is conflict
			node = m_doc.SelectSingleNode("//item[@id='vComp']");
			glbiNew = new GlossListBoxItem(Cache, node, ".", "", false);
			fResult = m_LabelGlosses.NewItemConflictsWithExtantItem(glbiNew, out glbiConflict);
			Assert.IsTrue(fResult, "Comparative should conflict with positive, but did not");
		}
	}
	/// <summary>
	/// Test sets for the GlossListTreeView class.
	/// </summary>
	[TestFixture]
	public class GlossListTreeViewTest
	{
		private GlossListTreeView treeViewGlossList;
		private readonly string sXmlFile = Path.Combine(FwDirectoryFinder.CodeDirectory, @"Language Explorer", "MGA", "GlossLists", "EticGlossList.xml");
		private XmlDocument dom = new XmlDocument();
		private string m_sTopOfList = "eticGlossList";

		/// <summary>
		///
		/// </summary>
		/// <remarks>This method is called before each test </remarks>
		[SetUp]
		public void Init()
		{
			treeViewGlossList = new GlossListTreeView();
			treeViewGlossList.LoadGlossListTreeFromXml(sXmlFile, "en");

			dom.Load(sXmlFile);
		}
		/// <summary>
		///
		/// </summary>
		/// <remarks>This method is called after each test </remarks>
		[TearDown]
		public virtual void TearDown()
		{
			treeViewGlossList.Dispose();
		}

		[Test]
		public void SomeNodeCountsTest()
		{
			Assert.AreEqual(5, treeViewGlossList.Nodes.Count);
			Assert.AreEqual(2, treeViewGlossList.Nodes[0].Nodes.Count);
			Assert.AreEqual(2, treeViewGlossList.Nodes[1].Nodes.Count);
			Assert.AreEqual(2, treeViewGlossList.Nodes[2].Nodes.Count);
			Assert.AreEqual(682, treeViewGlossList.GetNodeCount(true));
		}
		[Test]
		public void SomeNodeContentsTest()
		{
			Assert.AreEqual("adjective-related", treeViewGlossList.Nodes[0].Text);
			Assert.AreEqual("degree: deg", treeViewGlossList.Nodes[0].Nodes[0].Text);
			Assert.AreEqual("article-related", treeViewGlossList.Nodes[1].Text);
			Assert.AreEqual("gender: gen", treeViewGlossList.Nodes[0].Nodes[1].Nodes[0].Text);
		}
		[Test]
		public void GetFirstItemAbbrevTest()
		{

			var xn = dom.SelectSingleNode(m_sTopOfList + "/item/abbrev");
			var strCheckBoxes = xn.InnerText;
			Assert.AreEqual("adj.r", strCheckBoxes);
		}
		[Test]
		public void GetTreeNonExistantAttrTest()
		{

			var treeTop = dom.SelectSingleNode(m_sTopOfList);
			Assert.IsNull(XmlUtils.GetOptionalAttributeValue(treeTop, "nonExistant"), "Expected null object");
		}
		[Test]
		public void TreeNodeBitmapTest()
		{
			Assert.AreEqual(MGAImageKind.userChoice, (MGAImageKind)treeViewGlossList.Nodes[0].Nodes[0].ImageIndex);
			Assert.AreEqual(MGAImageKind.userChoice, (MGAImageKind)treeViewGlossList.Nodes[1].Nodes[1].ImageIndex);
		}
		[Test]
		public void WritingSystemDefaultsToEnglishTest()
		{
			using (var myTVGL = new GlossListTreeView())
			{
				// sXmlFile doesn't have any "fr" items in it; so it should default to English
				myTVGL.LoadGlossListTreeFromXml(sXmlFile, "fr");
				Assert.IsTrue(myTVGL.WritingSystemAbbrev == "en", "Expected writing system to default to English, but it did not.");
			}
		}
	}
}
