// Copyright (c) 2003-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.IO;
using System.Xml;
using LanguageExplorer.MGA;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Xml;

namespace LanguageExplorerTests.MGA
{
	/// <summary>
	/// Test sets for the GlossListTreeView class.
	/// </summary>
	[TestFixture]
	public class GlossListTreeViewTest
	{
		private GlossListTreeView _treeViewGlossList;
		private readonly string _xmlPathname = Path.Combine(FwDirectoryFinder.CodeDirectory, @"Language Explorer", "MGA", "GlossLists", "EticGlossList.xml");
		private XmlDocument _dom = new XmlDocument();
		private string _topOfList = "eticGlossList";

		/// <summary />
		/// <remarks>This method is called before each test </remarks>
		[SetUp]
		public void Init()
		{
			_treeViewGlossList = new GlossListTreeView();
			_treeViewGlossList.LoadGlossListTreeFromXml(_xmlPathname, "en");

			_dom.Load(_xmlPathname);
		}

		/// <summary />
		/// <remarks>This method is called after each test </remarks>
		[TearDown]
		public virtual void TearDown()
		{
			_treeViewGlossList.Dispose();
		}

		[Test]
		public void SomeNodeCountsTest()
		{
			Assert.AreEqual(5, _treeViewGlossList.Nodes.Count);
			Assert.AreEqual(2, _treeViewGlossList.Nodes[0].Nodes.Count);
			Assert.AreEqual(2, _treeViewGlossList.Nodes[1].Nodes.Count);
			Assert.AreEqual(2, _treeViewGlossList.Nodes[2].Nodes.Count);
			Assert.AreEqual(682, _treeViewGlossList.GetNodeCount(true));
		}
		[Test]
		public void SomeNodeContentsTest()
		{
			Assert.AreEqual("adjective-related", _treeViewGlossList.Nodes[0].Text);
			Assert.AreEqual("degree: deg", _treeViewGlossList.Nodes[0].Nodes[0].Text);
			Assert.AreEqual("article-related", _treeViewGlossList.Nodes[1].Text);
			Assert.AreEqual("gender: gen", _treeViewGlossList.Nodes[0].Nodes[1].Nodes[0].Text);
		}
		[Test]
		public void GetFirstItemAbbrevTest()
		{

			var xn = _dom.SelectSingleNode(_topOfList + "/item/abbrev");
			var strCheckBoxes = xn.InnerText;
			Assert.AreEqual("adj.r", strCheckBoxes);
		}
		[Test]
		public void GetTreeNonExistentAttrTest()
		{
			var treeTop = _dom.SelectSingleNode(_topOfList);
			Assert.IsNull(XmlUtils.GetOptionalAttributeValue(treeTop, "nonExistant"), "Expected null object");
		}
		[Test]
		public void TreeNodeBitmapTest()
		{
			Assert.AreEqual(MGAImageKind.userChoice, (MGAImageKind)_treeViewGlossList.Nodes[0].Nodes[0].ImageIndex);
			Assert.AreEqual(MGAImageKind.userChoice, (MGAImageKind)_treeViewGlossList.Nodes[1].Nodes[1].ImageIndex);
		}
		[Test]
		public void WritingSystemDefaultsToEnglishTest()
		{
			using (var myTVGL = new GlossListTreeView())
			{
				// sXmlFile doesn't have any "fr" items in it; so it should default to English
				myTVGL.LoadGlossListTreeFromXml(_xmlPathname, "fr");
				Assert.IsTrue(myTVGL.WritingSystemAbbrev == "en", "Expected writing system to default to English, but it did not.");
			}
		}
	}
}