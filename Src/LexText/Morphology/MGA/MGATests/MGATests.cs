// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: MGATests.cs
// Responsibility: Andy Black
// Last reviewed:
//
// <remarks>
// Unit tests for GlossListBox and GlossListTreeView
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.IO;
using System.Xml;
using NUnit.Framework;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.Utils;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.LexText.Controls.MGA
{
	/// <summary>
	/// Test sets for the GlossListBox class.
	/// </summary>
	[TestFixture]
	public class GlossListBoxTest : MemoryOnlyBackendProviderTestBase
	{
		private GlossListBox m_LabelGlosses;
		private XmlDocument m_doc;

		/// <summary>
		///
		/// </summary>
		/// <remarks>This method is called before each test </remarks>
		[SetUp]
		public void Init()
		{
			string sXmlFile = Path.Combine(DirectoryFinder.FWCodeDirectory,
				@"Language Explorer/MGA/GlossLists/EticGlossList.xml");
			m_doc = new XmlDocument();
			m_doc.Load(sXmlFile);
			m_LabelGlosses = new GlossListBox();
			m_LabelGlosses.Sorted = false;
			XmlNode node = m_doc.SelectSingleNode("//item[@id='vPositive']");
			GlossListBoxItem glbi = new GlossListBoxItem(Cache, node, ".", "", false);
			m_LabelGlosses.Items.Add(glbi);
		}
		/// <summary>
		///
		/// </summary>
		/// <remarks>This method is called after each test </remarks>
		[TearDown]
		public virtual void TearDown()
		{
			m_LabelGlosses.Dispose();
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
			XmlNode node = m_doc.SelectSingleNode("//item[@id='cAdjAgr']/item[@target='tCommonAgr']/item[@target='fGender']/item[@target='vMasc']");
			GlossListBoxItem glbiNew = new GlossListBoxItem(Cache, node, ".", "", false);
			GlossListBoxItem glbiConflict;
			bool fResult = m_LabelGlosses.NewItemConflictsWithExtantItem(glbiNew, out glbiConflict);
			string sMsg;
			if (glbiConflict != null)
				sMsg = String.Format("Masculine gender should not conflict, but did with {0}.", glbiConflict.Abbrev);
			else
				sMsg = "Masculine gender should not conflict";
			Assert.IsFalse(fResult, sMsg);
			// check a non-terminal node, so no conflict
			node = m_doc.SelectSingleNode("//item[@id='fDeg']");
			glbiNew = new GlossListBoxItem(Cache, node, ".", "", false);
			fResult = m_LabelGlosses.NewItemConflictsWithExtantItem(glbiNew, out glbiConflict);
			if (glbiConflict != null)
			sMsg = String.Format("Feature degree should not conflict, but did with {0}", glbiConflict.Abbrev);
			else
				sMsg = "Feature degree should not conflict";
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
	public class GlossListTreeViewTest: SIL.FieldWorks.Test.TestUtils.BaseTest
	{
		private GlossListTreeView treeViewGlossList;
		private string sXmlFile = Path.Combine(DirectoryFinder.FWCodeDirectory,
			@"Language Explorer/MGA/GlossLists/EticGlossList.xml");
		private XmlDocument dom = new XmlDocument();
		private string m_sTopOfList = "eticGlossList";

		public GlossListTreeViewTest()
		{
		}
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

			XmlNode xn = dom.SelectSingleNode(m_sTopOfList + "/item/abbrev");
			string strCheckBoxes = xn.InnerText;
			Assert.AreEqual("adj.r", strCheckBoxes);
		}
		[Test]
		public void GetTreeNonExistantAttrTest()
		{

			XmlNode treeTop = dom.SelectSingleNode(m_sTopOfList);
			Assert.IsNull(XmlUtils.GetAttributeValue(treeTop, "nonExistant"), "Expected null object");
		}
		[Test]
		public void TreeNodeBitmapTest()
		{
			Assert.AreEqual(GlossListTreeView.ImageKind.userChoice,
				(GlossListTreeView.ImageKind)treeViewGlossList.Nodes[0].Nodes[0].ImageIndex);
			Assert.AreEqual(GlossListTreeView.ImageKind.userChoice,
				(GlossListTreeView.ImageKind)treeViewGlossList.Nodes[1].Nodes[1].ImageIndex);
		}
		[Test]
		public void WritingSystemDefaultsToEnglishTest()
		{
			using (GlossListTreeView myTVGL = new GlossListTreeView())
			{
				// sXmlFile doesn't have any "fr" items in it; so it should default to English
				myTVGL.LoadGlossListTreeFromXml(sXmlFile, "fr");
				Assert.IsTrue(myTVGL.WritingSystemAbbrev == "en", "Expected writing system to default to English, but it did not.");
			}
		}
	}
}
