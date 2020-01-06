// Copyright (c) 2004-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using NUnit.Framework;
using SIL.LCModel.Utils;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary>
	/// Tests for TriStateTreeView control.
	/// </summary>
	[TestFixture]
	public class TriStateTreeViewTests
	{
		private TriStateTreeView m_treeView;
		private TreeNode m_aNode;
		private TreeNode m_bNode;
		private TreeNode m_c1Node;
		private TreeNode m_c2Node;
		private TreeNode m_dNode;
		private bool m_fBeforeCheck;
		private bool m_fCancelInBeforeCheck;
		private bool m_fAfterCheck;

		/// <summary>
		/// Initialize a test
		///
		/// m_aNode
		/// |
		/// +- m_bNode
		///    |
		///    +- m_c1Node
		///    |  |
		///    |  +- m_dNode
		///    |
		///    +- m_c2Node
		///
		/// </summary>
		[SetUp]
		public void Init()
		{
			m_fBeforeCheck = false;
			m_fAfterCheck = false;
			m_fCancelInBeforeCheck = false;
			m_treeView = new TriStateTreeView();

			m_dNode = new TreeNode("d");
			m_c1Node = new TreeNode("c1", new[] { m_dNode });
			m_c2Node = new TreeNode("c2");
			m_bNode = new TreeNode("b", new[] { m_c1Node, m_c2Node });
			m_aNode = new TreeNode("a", new[] { m_bNode });
			m_treeView.Nodes.Add(m_aNode);
		}

		/// <summary />
		[TearDown]
		public void TearDown()
		{
			m_treeView?.Dispose();
			m_treeView = null;
		}

		/// <summary>
		/// Tests that all nodes in the tree view are initially unchecked
		/// </summary>
		[Test]
		public void InitiallyUnchecked()
		{
			Assert.AreEqual(TriStateTreeViewCheckState.Unchecked, m_treeView.GetChecked(m_aNode));
			Assert.AreEqual(TriStateTreeViewCheckState.Unchecked, m_treeView.GetChecked(m_bNode));
			Assert.AreEqual(TriStateTreeViewCheckState.Unchecked, m_treeView.GetChecked(m_c1Node));
			Assert.AreEqual(TriStateTreeViewCheckState.Unchecked, m_treeView.GetChecked(m_c2Node));
			Assert.AreEqual(TriStateTreeViewCheckState.Unchecked, m_treeView.GetChecked(m_dNode));
		}

		/// <summary>
		/// Tests that changing a node changes all children
		/// </summary>
		[Test]
		public void ChangeNodeChangesAllChildren_Check()
		{
			// Check a node -> should check all children
			m_treeView.SetChecked(m_bNode, TriStateTreeViewCheckState.Checked);

			Assert.AreEqual(TriStateTreeViewCheckState.Checked, m_treeView.GetChecked(m_bNode));
			Assert.AreEqual(TriStateTreeViewCheckState.Checked, m_treeView.GetChecked(m_c1Node));
			Assert.AreEqual(TriStateTreeViewCheckState.Checked, m_treeView.GetChecked(m_c2Node));
			Assert.AreEqual(TriStateTreeViewCheckState.Checked, m_treeView.GetChecked(m_dNode));
		}

		/// <summary>
		/// Tests that changing a node changes all children
		/// </summary>
		[Test]
		public void ChangeNodeChangesAllChildren_Uncheck()
		{
			// uncheck a node -> should uncheck all children
			m_treeView.SetChecked(m_bNode, TriStateTreeViewCheckState.Unchecked);

			Assert.AreEqual(TriStateTreeViewCheckState.Unchecked, m_treeView.GetChecked(m_bNode));
			Assert.AreEqual(TriStateTreeViewCheckState.Unchecked, m_treeView.GetChecked(m_c1Node));
			Assert.AreEqual(TriStateTreeViewCheckState.Unchecked, m_treeView.GetChecked(m_c2Node));
			Assert.AreEqual(TriStateTreeViewCheckState.Unchecked, m_treeView.GetChecked(m_dNode));
		}

		/// <summary>
		/// Tests that parent get grayed out if children are not all in same state
		/// </summary>
		[Test]
		public void ChangeParent_CheckOneChild()
		{
			// check child -> grey check all parents
			m_treeView.SetChecked(m_c2Node, TriStateTreeViewCheckState.Checked);

			Assert.AreEqual(TriStateTreeViewCheckState.GrayChecked, m_treeView.GetChecked(m_aNode));
			Assert.AreEqual(TriStateTreeViewCheckState.GrayChecked, m_treeView.GetChecked(m_bNode));
			Assert.AreEqual(TriStateTreeViewCheckState.Unchecked, m_treeView.GetChecked(m_c1Node));
			Assert.AreEqual(TriStateTreeViewCheckState.Checked, m_treeView.GetChecked(m_c2Node));
		}

		/// <summary>
		/// Tests that parent get grayed out if children are not all in same state
		/// </summary>
		[Test]
		public void ChangeParent_CheckAllChildren()
		{
			// check second child -> check all parents
			m_treeView.SetChecked(m_c2Node, TriStateTreeViewCheckState.Checked);
			m_treeView.SetChecked(m_c1Node, TriStateTreeViewCheckState.Checked);

			Assert.AreEqual(TriStateTreeViewCheckState.Checked, m_treeView.GetChecked(m_aNode));
			Assert.AreEqual(TriStateTreeViewCheckState.Checked, m_treeView.GetChecked(m_bNode));
			Assert.AreEqual(TriStateTreeViewCheckState.Checked, m_treeView.GetChecked(m_c1Node));
			Assert.AreEqual(TriStateTreeViewCheckState.Checked, m_treeView.GetChecked(m_c2Node));
		}

		/// <summary>
		/// Tests that the BeforeCheck event is raised
		/// </summary>
		[Test]
		public void BeforeCheckCalled()
		{
			m_treeView.BeforeCheck += OnBeforeCheck;
			m_treeView.SetChecked(m_c1Node, TriStateTreeViewCheckState.Checked);

			Assert.IsTrue(m_fBeforeCheck);
			Assert.AreEqual(TriStateTreeViewCheckState.Checked, m_treeView.GetChecked(m_c1Node));
		}

		/// <summary>
		/// Tests that the BeforeCheck event is raised if first node is changed
		/// </summary>
		[Test]
		public void BeforeCheckCalled_FirstNode()
		{
			m_treeView.BeforeCheck += OnBeforeCheck;
			ReflectionHelper.CallMethod(m_treeView, "ChangeNodeState", m_aNode);
			Assert.IsTrue(m_fBeforeCheck);
			Assert.AreEqual(TriStateTreeViewCheckState.Checked, m_treeView.GetChecked(m_aNode));
		}

		/// <summary>
		/// Tests that the AfterCheck event is raised
		/// </summary>
		[Test]
		public void AfterCheckCalled()
		{
			m_treeView.AfterCheck += OnAfterCheck;
			m_treeView.SetChecked(m_c1Node, TriStateTreeViewCheckState.Checked);
			Assert.IsTrue(m_fAfterCheck);
			Assert.AreEqual(TriStateTreeViewCheckState.Checked, m_treeView.GetChecked(m_c1Node));
		}

		/// <summary>
		/// When the cancel flag in BeforeCheck returns true we don't want to change the
		/// state of the node.
		/// </summary>
		[Test]
		public void StateNotChangedIfBeforeCheckCancels()
		{
			m_treeView.BeforeCheck += OnBeforeCheck;
			m_treeView.AfterCheck += OnAfterCheck;
			m_fCancelInBeforeCheck = true;

			m_treeView.SetChecked(m_c1Node, TriStateTreeViewCheckState.Checked);

			Assert.IsTrue(m_fBeforeCheck);
			Assert.IsFalse(m_fAfterCheck);
			Assert.AreEqual(TriStateTreeViewCheckState.Unchecked, m_treeView.GetChecked(m_c1Node));
		}

		/// <summary>
		/// Checks that the GetNodesWithState method returns the correct list of
		/// Checked nodes.
		/// </summary>
		[Test]
		public void GetNodesWithState_Checked()
		{
			var list = m_treeView.GetNodesWithState(TriStateTreeViewCheckState.Checked);
			Assert.IsEmpty(list);

			m_treeView.SetChecked(m_c1Node, TriStateTreeViewCheckState.Checked);
			list = m_treeView.GetNodesWithState(TriStateTreeViewCheckState.Checked);

			Assert.AreEqual(2, list.Length);
			Assert.AreEqual(m_c1Node, list[0]);
			Assert.AreEqual(m_dNode, list[1]);

			m_treeView.SetChecked(m_bNode, TriStateTreeViewCheckState.Checked);
			list = m_treeView.GetNodesWithState(TriStateTreeViewCheckState.Checked);

			Assert.AreEqual(5, list.Length);
			Assert.AreEqual(m_aNode, list[0]);
			Assert.AreEqual(m_bNode, list[1]);
			Assert.AreEqual(m_c1Node, list[2]);
			Assert.AreEqual(m_dNode, list[3]);
			Assert.AreEqual(m_c2Node, list[4]);
		}

		/// <summary>
		/// Checks that the GetNodesWithState method returns the correct list of
		/// UnChecked nodes.
		/// </summary>
		[Test]
		public void GetNodesWithState_Unchecked()
		{
			var list = m_treeView.GetNodesWithState(TriStateTreeViewCheckState.Checked);
			Assert.IsEmpty(list);

			// Check all nodes.
			m_treeView.SetChecked(m_aNode, TriStateTreeViewCheckState.Checked);
			list = m_treeView.GetNodesWithState(TriStateTreeViewCheckState.Unchecked);
			Assert.IsEmpty(list);

			m_treeView.SetChecked(m_c1Node, TriStateTreeViewCheckState.Unchecked);
			list = m_treeView.GetNodesWithState(TriStateTreeViewCheckState.Unchecked);

			Assert.AreEqual(2, list.Length);
			Assert.AreEqual(m_c1Node, list[0]);
			Assert.AreEqual(m_dNode, list[1]);
		}

		/// <summary>
		/// Checks that the GetNodesWithState method returns the correct list of nodes when
		/// requested to return "GrayChecked", which really means all nodes, regardless of
		/// check state.
		/// </summary>
		[Test]
		public void GetNodesWithState_GrayChecked()
		{
			var list = m_treeView.GetNodesWithState(TriStateTreeViewCheckState.Checked);
			Assert.IsEmpty(list);

			m_treeView.SetChecked(m_c1Node, TriStateTreeViewCheckState.Checked);
			// TomB: I have redefined GrayChecked to be synonymous with Unchecked | Checked, so
			// it is no longer possible to ask how many nodes are strictly GreyChecked. There is
			// no place in the production code where we currently care to get a list of
			// GrayChecked nodes, and it seems unlikely we'll ever care.
			list = m_treeView.GetNodesWithState(TriStateTreeViewCheckState.GrayChecked);

			Assert.AreEqual(5, list.Length);
		}

		/// <summary>
		/// Checks that the GetNodesOfTypeWithState method returns the correct list of
		/// Checked nodes of a certain type.
		/// </summary>
		/// m_aNode
		///   +- m_bNode
		///        +- c1Node
		///        |    +- dNode
		///        +- c2Node
		[Test]
		public void GetNodesOfTypeWithState()
		{
			m_treeView.Nodes.Clear();

			var dNode = new DummyTreeNode1("d");
			var c2Node = new DummyTreeNode2("c2");
			var c1Node = new DummyTreeNode2("c1", new TreeNode[] { dNode });
			m_bNode = new TreeNode("b", new TreeNode[] { c1Node, c2Node });
			m_aNode = new TreeNode("a", new[] { m_bNode });
			m_treeView.Nodes.Add(m_aNode);

			m_treeView.SetChecked(c1Node, TriStateTreeViewCheckState.Checked);

			// Get Checked nodes of type DummyTreeNode1.
			var list = m_treeView.GetNodesOfTypeWithState(typeof(DummyTreeNode1), TriStateTreeViewCheckState.Checked);

			Assert.AreEqual(1, list.Length);
			Assert.AreEqual(list[0], dNode);
			Assert.IsNotNull(list[0] as DummyTreeNode1);

			// Get Unchecked nodes of type DummyTreeNode2.
			list = m_treeView.GetNodesOfTypeWithState(typeof(DummyTreeNode2), TriStateTreeViewCheckState.Unchecked);

			Assert.AreEqual(1, list.Length);
			Assert.AreEqual(list[0], c2Node);
			Assert.IsNotNull(list[0] as DummyTreeNode2);

			// Get nodes of type DummyTreeNode2 regardless of check state (Unchecked, Checked or Grayed).
			list = m_treeView.GetNodesOfTypeWithState(typeof(DummyTreeNode2), TriStateTreeViewCheckState.Unchecked | TriStateTreeViewCheckState.Checked);

			Assert.AreEqual(2, list.Length);
			Assert.AreEqual(list[0], c1Node);
			Assert.AreEqual(list[1], c2Node);
			Assert.IsNotNull(list[0] as DummyTreeNode2);
			Assert.IsNotNull(list[1] as DummyTreeNode2);

			// Get nodes of type TreeNode regardless of check state (Unchecked, Checked or Grayed).
			list = m_treeView.GetNodesOfTypeWithState(typeof(TreeNode), TriStateTreeViewCheckState.GrayChecked);

			Assert.AreEqual(2, list.Length);
			Assert.AreEqual(list[0], m_aNode);
			Assert.AreEqual(list[1], m_bNode);
			Assert.IsNotNull(list[0]);
			Assert.IsNotNull(list[1]);
		}

		/// <summary>
		/// Checks that CheckedNodes property returns the correct list of GrayChecked nodes.
		/// </summary>
		[Test]
		public void GetCheckedTagData()
		{
			using (var dummyButton = new Button())
			{
				using (var dummyLabel = new Label())
				{
					m_bNode.Tag = dummyButton;
					m_c2Node.Tag = dummyLabel;
					m_treeView.SetChecked(m_bNode, TriStateTreeViewCheckState.Checked);
					var list = m_treeView.GetCheckedTagData();

					Assert.AreEqual(2, list.Count);
					Assert.AreEqual(dummyButton, list[0]);
					Assert.AreEqual(dummyLabel, list[1]);
				}
			}
		}

		#region Helper methods

		/// <summary />
		private void OnBeforeCheck(object sender, TreeViewCancelEventArgs e)
		{
			e.Cancel = m_fCancelInBeforeCheck;
			m_fBeforeCheck = true;
		}

		/// <summary />
		private void OnAfterCheck(object sender, TreeViewEventArgs e)
		{
			m_fAfterCheck = true;
		}
		#endregion

		/// <summary />
		private sealed class DummyTreeNode1 : TreeNode
		{
			internal DummyTreeNode1(string text) : base(text) { }
		}

		/// <summary />
		private sealed class DummyTreeNode2 : TreeNode
		{
			internal DummyTreeNode2(string text) : base(text) { }
			internal DummyTreeNode2(string text, TreeNode[] nodes) : base(text, nodes) { }
		}
	}
}