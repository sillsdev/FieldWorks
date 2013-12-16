// Copyright (c) 2011-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: TeKeyTermsTests.cs
// Responsibility: TE Team

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using NUnit.Framework;
using Rhino.Mocks;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Dummy tree for testing
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DummyKeyTermsTree : KeyTermsTree
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="DummyKeyTermsTree"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal DummyKeyTermsTree(ICmPossibilityList dummyList) : base(dummyList)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the FindNextMatch methodfor testing.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal FindResult CallFindNextMatch(string s)
		{
			return FindNextMatch(s);
		}

		internal new IEnumerable<TreeNode> AllNodes
		{
			get { return base.AllNodes; }
		}
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Test handling of key terms.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification="Unit test - m_treeView gets disposed in TestTearDown()")]
	public class KeyTermsTreeTests : ScrInMemoryFdoTestBase
	{
		#region Member variables
		DummyKeyTermsTree m_treeView;
		#endregion

		#region Setup, Teardown
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialization called for each test.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public override void TestSetup()
		{
			base.TestSetup();
			ICmPossibilityList dummyList = MockRepository.GenerateMock<ICmPossibilityList>();
			dummyList.Stub(l => l.Cache).Return(Cache);
			m_treeView = new DummyKeyTermsTree(dummyList);
		}

		/// <summary/>
		[TearDown]
		public override void TestTearDown()
		{
			m_treeView.Dispose();
			base.TestTearDown();
		}
		#endregion

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindNextMatch method when starting from a parent node, which contains a
		/// single match.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void FindNextMatchStartingAtParentNode_SingleMatch()
		{
			TreeNode catNode = new TreeNode("Category1");
			m_treeView.Nodes.Add(catNode);
			catNode.Nodes.Add("animal");
			catNode.Nodes.Add("grasshopper");
			catNode.Nodes.Add("locust");
			catNode.Nodes.Add("yak");
			m_treeView.SelectedNode = catNode;
			Assert.AreEqual(KeyTermsTree.FindResult.MatchFound, m_treeView.CallFindNextMatch("locust"));
			Assert.AreEqual("locust", m_treeView.SelectedNode.Text);
			Assert.AreEqual(KeyTermsTree.FindResult.NoMoreMatches, m_treeView.CallFindNextMatch("locust"));
			Assert.AreEqual("locust", m_treeView.SelectedNode.Text);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindNextMatch method when starting from a parent node, which contains a
		/// single match.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void FindNextMatchStartingAtParentNode_MultipleMatches()
		{
			TreeNode catNode = new TreeNode("Insects");
			m_treeView.Nodes.Add(catNode);
			catNode.Nodes.Add("firefly");
			catNode.Nodes.Add("dead locust");
			catNode.Nodes.Add("gnat");
			catNode.Nodes.Add("live locust");
			catNode.Nodes.Add("regular locust");
			catNode.Nodes.Add("yak");
			m_treeView.SelectedNode = catNode;
			Assert.AreEqual(KeyTermsTree.FindResult.MatchFound, m_treeView.CallFindNextMatch("locust"));
			Assert.AreEqual("dead locust", m_treeView.SelectedNode.Text);
			Assert.AreEqual(KeyTermsTree.FindResult.MatchFound, m_treeView.CallFindNextMatch("locust"));
			Assert.AreEqual("live locust", m_treeView.SelectedNode.Text);
			Assert.AreEqual(KeyTermsTree.FindResult.MatchFound, m_treeView.CallFindNextMatch("locust"));
			Assert.AreEqual("regular locust", m_treeView.SelectedNode.Text);
			Assert.AreEqual(KeyTermsTree.FindResult.NoMoreMatches, m_treeView.CallFindNextMatch("locust"));
			Assert.AreEqual("regular locust", m_treeView.SelectedNode.Text);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindNextMatch method when starting from the only match.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void FindOnlyMatchStartingAtThatMatch()
		{
			TreeNode catNode = new TreeNode("Category1");
			m_treeView.Nodes.Add(catNode);
			catNode.Nodes.Add("animal");
			catNode.Nodes.Add("grasshopper");
			m_treeView.SelectedNode = catNode.Nodes.Add("locust");
			catNode.Nodes.Add("yak");
			Assert.AreEqual(KeyTermsTree.FindResult.MatchFound, m_treeView.CallFindNextMatch("locust"));
			Assert.AreEqual("locust", m_treeView.SelectedNode.Text);
			Assert.AreEqual(KeyTermsTree.FindResult.NoMoreMatches, m_treeView.CallFindNextMatch("locust"));
			Assert.AreEqual("locust", m_treeView.SelectedNode.Text);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindNextMatch method when starting from a matching node, when a single
		/// subsequent match exists.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void FindNextMatchStartingFromFirstMatch_TwoMatches()
		{
			TreeNode catNode = new TreeNode("Category1");
			m_treeView.Nodes.Add(catNode);
			catNode.Nodes.Add("animal");
			m_treeView.SelectedNode = catNode.Nodes.Add("locust brains");
			catNode.Nodes.Add("fried locust");
			catNode.Nodes.Add("yak");
			Assert.AreEqual(KeyTermsTree.FindResult.MatchFound, m_treeView.CallFindNextMatch("locust"));
			Assert.AreEqual("fried locust", m_treeView.SelectedNode.Text);
			Assert.AreEqual(KeyTermsTree.FindResult.MatchFound, m_treeView.CallFindNextMatch("locust"));
			Assert.AreEqual("locust brains", m_treeView.SelectedNode.Text);
			Assert.AreEqual(KeyTermsTree.FindResult.NoMoreMatches, m_treeView.CallFindNextMatch("locust"));
			Assert.AreEqual("locust brains", m_treeView.SelectedNode.Text);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindNextMatch method when starting from a matching node, when multiple
		/// subsequent matches exist.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void FindNextMatchStartingFromFirstMatch_MultipleMatches()
		{
			TreeNode catNode = new TreeNode("Category1");
			m_treeView.Nodes.Add(catNode);
			catNode.Nodes.Add("animal");
			m_treeView.SelectedNode = catNode.Nodes.Add("locust brains");
			catNode.Nodes.Add("fried locust");
			catNode.Nodes.Add("yak");
			catNode.Nodes.Add("locust stew");
			catNode.Nodes.Add("zebra");
			Assert.AreEqual(KeyTermsTree.FindResult.MatchFound, m_treeView.CallFindNextMatch("locust"));
			Assert.AreEqual("fried locust", m_treeView.SelectedNode.Text);
			Assert.AreEqual(KeyTermsTree.FindResult.MatchFound, m_treeView.CallFindNextMatch("locust"));
			Assert.AreEqual("locust stew", m_treeView.SelectedNode.Text);
			Assert.AreEqual(KeyTermsTree.FindResult.MatchFound, m_treeView.CallFindNextMatch("locust"));
			Assert.AreEqual("locust brains", m_treeView.SelectedNode.Text);
			Assert.AreEqual(KeyTermsTree.FindResult.NoMoreMatches, m_treeView.CallFindNextMatch("locust"));
			Assert.AreEqual("locust brains", m_treeView.SelectedNode.Text);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindNextMatch method when starting from a node that occurs between the
		/// first and second match.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void FindNextMatchStartingBetweenMatches()
		{
			TreeNode catNode = new TreeNode("Category1");
			m_treeView.Nodes.Add(catNode);
			catNode.Nodes.Add("animal");
			catNode.Nodes.Add("fried locust");
			m_treeView.SelectedNode = catNode.Nodes.Add("grasshopper");
			catNode.Nodes.Add("locust stew");
			catNode.Nodes.Add("yak");
			Assert.AreEqual(KeyTermsTree.FindResult.MatchFound, m_treeView.CallFindNextMatch("locust"));
			Assert.AreEqual("locust stew", m_treeView.SelectedNode.Text);
			Assert.AreEqual(KeyTermsTree.FindResult.MatchFound, m_treeView.CallFindNextMatch("locust"));
			Assert.AreEqual("fried locust", m_treeView.SelectedNode.Text);
			Assert.AreEqual(KeyTermsTree.FindResult.NoMoreMatches, m_treeView.CallFindNextMatch("locust"));
			Assert.AreEqual("fried locust", m_treeView.SelectedNode.Text);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindNextMatch method when starting from a node that occurs after a couple
		/// matches.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void FindNextMatchStartingAfterLastMatch()
		{
			TreeNode catNode = new TreeNode("Category1");
			m_treeView.Nodes.Add(catNode);
			catNode.Nodes.Add("animal");
			catNode.Nodes.Add("fried locust");
			catNode.Nodes.Add("grasshopper");
			catNode.Nodes.Add("locust stew");
			m_treeView.SelectedNode = catNode.Nodes.Add("yak");
			Assert.AreEqual(KeyTermsTree.FindResult.MatchFound, m_treeView.CallFindNextMatch("locust"));
			Assert.AreEqual("fried locust", m_treeView.SelectedNode.Text);
			Assert.AreEqual(KeyTermsTree.FindResult.MatchFound, m_treeView.CallFindNextMatch("locust"));
			Assert.AreEqual("locust stew", m_treeView.SelectedNode.Text);
			Assert.AreEqual(KeyTermsTree.FindResult.NoMoreMatches, m_treeView.CallFindNextMatch("locust"));
			Assert.AreEqual("locust stew", m_treeView.SelectedNode.Text);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindNextMatch method when starting from a node that occurs before a couple
		/// matches.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void FindNextMatchStartingBeforeFirstMatch()
		{
			TreeNode catNode = new TreeNode("Category1");
			m_treeView.Nodes.Add(catNode);
			m_treeView.SelectedNode = catNode.Nodes.Add("animal");
			catNode.Nodes.Add("fried locust");
			catNode.Nodes.Add("grasshopper");
			catNode.Nodes.Add("locust stew");
			catNode.Nodes.Add("yak");
			Assert.AreEqual(KeyTermsTree.FindResult.MatchFound, m_treeView.CallFindNextMatch("locust"));
			Assert.AreEqual("fried locust", m_treeView.SelectedNode.Text);
			Assert.AreEqual(KeyTermsTree.FindResult.MatchFound, m_treeView.CallFindNextMatch("locust"));
			Assert.AreEqual("locust stew", m_treeView.SelectedNode.Text);
			Assert.AreEqual(KeyTermsTree.FindResult.NoMoreMatches, m_treeView.CallFindNextMatch("locust"));
			Assert.AreEqual("locust stew", m_treeView.SelectedNode.Text);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindNextMatch method when starting from a matching node, when a
		/// subsequent match exists.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void FindNextMatchStartingFromSecondMatch()
		{
			TreeNode catNode = new TreeNode("Category1");
			m_treeView.Nodes.Add(catNode);
			catNode.Nodes.Add("animal");
			catNode.Nodes.Add("locust brains");
			m_treeView.SelectedNode = catNode.Nodes.Add("fried locust");
			catNode.Nodes.Add("yak");
			Assert.AreEqual(KeyTermsTree.FindResult.MatchFound, m_treeView.CallFindNextMatch("locust"));
			Assert.AreEqual("locust brains", m_treeView.SelectedNode.Text);
			Assert.AreEqual(KeyTermsTree.FindResult.MatchFound, m_treeView.CallFindNextMatch("locust"));
			Assert.AreEqual("fried locust", m_treeView.SelectedNode.Text);
			Assert.AreEqual(KeyTermsTree.FindResult.NoMoreMatches, m_treeView.CallFindNextMatch("locust"));
			Assert.AreEqual("fried locust", m_treeView.SelectedNode.Text);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindNextMatch method when starting from second matching node, when matches
		/// exist both before and after it.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void FindNextMatchStartingFromSecondMatch_MultipleMatches()
		{
			TreeNode catNode = new TreeNode("Category1");
			m_treeView.Nodes.Add(catNode);
			catNode.Nodes.Add("animal");
			catNode.Nodes.Add("locust brains");
			m_treeView.SelectedNode = catNode.Nodes.Add("fried locust");
			catNode.Nodes.Add("yak");
			catNode.Nodes.Add("locust stew");
			catNode.Nodes.Add("zebra");
			Assert.AreEqual(KeyTermsTree.FindResult.MatchFound, m_treeView.CallFindNextMatch("locust"));
			Assert.AreEqual("locust stew", m_treeView.SelectedNode.Text);
			Assert.AreEqual(KeyTermsTree.FindResult.MatchFound, m_treeView.CallFindNextMatch("locust"));
			Assert.AreEqual("locust brains", m_treeView.SelectedNode.Text);
			Assert.AreEqual(KeyTermsTree.FindResult.MatchFound, m_treeView.CallFindNextMatch("locust"));
			Assert.AreEqual("fried locust", m_treeView.SelectedNode.Text);
			Assert.AreEqual(KeyTermsTree.FindResult.NoMoreMatches, m_treeView.CallFindNextMatch("locust"));
			Assert.AreEqual("fried locust", m_treeView.SelectedNode.Text);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindNextMatch method when starting from a parent node, which contains a
		/// single match.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void FindNextMatchChangeSearchString()
		{
			TreeNode catNode = new TreeNode("Category1");
			m_treeView.Nodes.Add(catNode);
			catNode.Nodes.Add("animal");
			m_treeView.SelectedNode = catNode.Nodes.Add("grasshopper");
			catNode.Nodes.Add("locust");
			catNode.Nodes.Add("yak");
			Assert.AreEqual(KeyTermsTree.FindResult.MatchFound, m_treeView.CallFindNextMatch("animal"));
			Assert.AreEqual("animal", m_treeView.SelectedNode.Text);
			Assert.AreEqual(KeyTermsTree.FindResult.MatchFound, m_treeView.CallFindNextMatch("locust"));
			Assert.AreEqual("locust", m_treeView.SelectedNode.Text);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindNextMatch method when starting from a parent node, which contains a
		/// single match.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void FindNextMatch_RequireNormalization()
		{
			IWritingSystem ws;
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("grc", out ws);

			int wsGreek = ws.Handle;

			TreeNode catNode = new TreeNode("Proper Names");
			m_treeView.Nodes.Add(catNode);
			TreeNode chkTermNode = new TreeNode("Abraham");
			ICmPossibilityList list = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
			Cache.LanguageProject.CheckListsOC.Add(list);
			IChkTerm term = Cache.ServiceLocator.GetInstance<IChkTermFactory>().Create();
			list.PossibilitiesOS.Add(term);
			ITsString tssName = Cache.TsStrFactory.MakeString("Abraham".Normalize(NormalizationForm.FormD), Cache.DefaultUserWs);
			const string abrahamGrk = "\u1F08\u03B2\u03C1\u03B1\u1F71\u03BC";
			ITsString tssDesc = Cache.TsStrFactory.MakeString(abrahamGrk.Normalize(NormalizationForm.FormD), wsGreek);
			ITsString tssSeeAlso = Cache.TsStrFactory.MakeString("", Cache.DefaultUserWs);
			term.Name.set_String(Cache.DefaultUserWs, tssName);
			term.Description.set_String(wsGreek, tssDesc);
			term.SeeAlso.set_String(Cache.DefaultUserWs, tssName);
			chkTermNode.Tag = term;
			catNode.Nodes.Add(chkTermNode);
			m_treeView.SelectedNode = catNode;
			Assert.AreEqual(KeyTermsTree.FindResult.MatchFound, m_treeView.CallFindNextMatch(abrahamGrk.Normalize(NormalizationForm.FormC)));
			Assert.AreEqual("Abraham", m_treeView.SelectedNode.Text);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the AllNodes property.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void GetAllNodes()
		{
			TreeNode catNode = new TreeNode("Category1");
			m_treeView.Nodes.Add(catNode);
			catNode.Nodes.Add("animal");
			catNode.Nodes.Add("grasshopper");
			catNode = new TreeNode("Category2");
			m_treeView.Nodes.Add(catNode);
			catNode.Nodes.Add("animal");
			catNode.Nodes.Add("yak");
			catNode = new TreeNode("Category3");
			m_treeView.Nodes.Add(catNode);
			catNode.Nodes.Add("frog");
			catNode.Nodes.Add("crawdad");
			TreeNode subCatNode = new TreeNode("Category3B");
			catNode.Nodes.Add(subCatNode);
			subCatNode.Nodes.Add("mushroom");
			subCatNode.Nodes.Add("cloak room");

			TreeNode[] allNodes = m_treeView.AllNodes.ToArray();
			int i = 0;
			Assert.AreEqual("Category1", allNodes[i++].Text);
			Assert.AreEqual("animal", allNodes[i++].Text);
			Assert.AreEqual("grasshopper", allNodes[i++].Text);
			Assert.AreEqual("Category2", allNodes[i++].Text);
			Assert.AreEqual("animal", allNodes[i++].Text);
			Assert.AreEqual("yak", allNodes[i++].Text);
			Assert.AreEqual("Category3", allNodes[i++].Text);
			Assert.AreEqual("frog", allNodes[i++].Text);
			Assert.AreEqual("crawdad", allNodes[i++].Text);
			Assert.AreEqual("Category3B", allNodes[i++].Text);
			Assert.AreEqual("mushroom", allNodes[i++].Text);
			Assert.AreEqual("cloak room", allNodes[i].Text);
			Assert.AreEqual(12, allNodes.Length);
		}
	}
}
