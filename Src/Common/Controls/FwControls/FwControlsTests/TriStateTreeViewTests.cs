// Copyright (c) 2004-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using NUnit.Framework;
using SIL.LCModel.Utils;

namespace SIL.FieldWorks.Common.Controls
{
	#region Dummy tree node classes
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DummyTreeNode1 : TreeNode
	{
		internal DummyTreeNode1(string text) : base(text) { }
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DummyTreeNode2 : TreeNode
	{
		internal DummyTreeNode2(string text) : base(text) { }
		internal DummyTreeNode2(string text, TreeNode[] nodes) : base(text, nodes) { }
	}

	#endregion

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for TriStateTreeView control.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
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

		/// ------------------------------------------------------------------------------------
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
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public void Init()
		{
			m_fBeforeCheck = false;
			m_fAfterCheck = false;
			m_fCancelInBeforeCheck = false;
			m_treeView = new TriStateTreeView();

			m_dNode = new TreeNode("d");
			m_c1Node = new TreeNode("c1", new TreeNode[] { m_dNode });
			m_c2Node = new TreeNode("c2");
			m_bNode = new TreeNode("b", new TreeNode[] { m_c1Node, m_c2Node });
			m_aNode = new TreeNode("a", new TreeNode[] { m_bNode });
			m_treeView.Nodes.Add(m_aNode);
		}

		/// <summary/>
		[TearDown]
		public void TearDown()
		{
			if (m_treeView != null)
				m_treeView.Dispose();
			m_treeView = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that all nodes in the tree view are initially unchecked
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InitiallyUnchecked()
		{
			Assert.That(m_treeView.GetChecked(m_aNode), Is.EqualTo(TriStateTreeView.CheckState.Unchecked));
			Assert.That(m_treeView.GetChecked(m_bNode), Is.EqualTo(TriStateTreeView.CheckState.Unchecked));
			Assert.That(m_treeView.GetChecked(m_c1Node), Is.EqualTo(TriStateTreeView.CheckState.Unchecked));
			Assert.That(m_treeView.GetChecked(m_c2Node), Is.EqualTo(TriStateTreeView.CheckState.Unchecked));
			Assert.That(m_treeView.GetChecked(m_dNode), Is.EqualTo(TriStateTreeView.CheckState.Unchecked));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that changing a node changes all children
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ChangeNodeChangesAllChildren_Check()
		{
			// Check a node -> should check all children
			m_treeView.SetChecked(m_bNode, TriStateTreeView.CheckState.Checked);

			Assert.That(m_treeView.GetChecked(m_bNode), Is.EqualTo(TriStateTreeView.CheckState.Checked));
			Assert.That(m_treeView.GetChecked(m_c1Node), Is.EqualTo(TriStateTreeView.CheckState.Checked));
			Assert.That(m_treeView.GetChecked(m_c2Node), Is.EqualTo(TriStateTreeView.CheckState.Checked));
			Assert.That(m_treeView.GetChecked(m_dNode), Is.EqualTo(TriStateTreeView.CheckState.Checked));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that changing a node changes all children
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ChangeNodeChangesAllChildren_Uncheck()
		{
			// uncheck a node -> should uncheck all children
			m_treeView.SetChecked(m_bNode, TriStateTreeView.CheckState.Unchecked);

			Assert.That(m_treeView.GetChecked(m_bNode), Is.EqualTo(TriStateTreeView.CheckState.Unchecked));
			Assert.That(m_treeView.GetChecked(m_c1Node), Is.EqualTo(TriStateTreeView.CheckState.Unchecked));
			Assert.That(m_treeView.GetChecked(m_c2Node), Is.EqualTo(TriStateTreeView.CheckState.Unchecked));
			Assert.That(m_treeView.GetChecked(m_dNode), Is.EqualTo(TriStateTreeView.CheckState.Unchecked));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that parent get greyed out if children are not all in same state
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ChangeParent_CheckOneChild()
		{
			// check child -> grey check all parents
			m_treeView.SetChecked(m_c2Node, TriStateTreeView.CheckState.Checked);

			Assert.That(m_treeView.GetChecked(m_aNode), Is.EqualTo(TriStateTreeView.CheckState.GreyChecked));
			Assert.That(m_treeView.GetChecked(m_bNode), Is.EqualTo(TriStateTreeView.CheckState.GreyChecked));
			Assert.That(m_treeView.GetChecked(m_c1Node), Is.EqualTo(TriStateTreeView.CheckState.Unchecked));
			Assert.That(m_treeView.GetChecked(m_c2Node), Is.EqualTo(TriStateTreeView.CheckState.Checked));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that parent get greyed out if children are not all in same state
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ChangeParent_CheckAllChildren()
		{
			// check second child -> check all parents
			m_treeView.SetChecked(m_c2Node, TriStateTreeView.CheckState.Checked);
			m_treeView.SetChecked(m_c1Node, TriStateTreeView.CheckState.Checked);

			Assert.That(m_treeView.GetChecked(m_aNode), Is.EqualTo(TriStateTreeView.CheckState.Checked));
			Assert.That(m_treeView.GetChecked(m_bNode), Is.EqualTo(TriStateTreeView.CheckState.Checked));
			Assert.That(m_treeView.GetChecked(m_c1Node), Is.EqualTo(TriStateTreeView.CheckState.Checked));
			Assert.That(m_treeView.GetChecked(m_c2Node), Is.EqualTo(TriStateTreeView.CheckState.Checked));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the BeforeCheck event is raised
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BeforeCheckCalled()
		{
			m_treeView.BeforeCheck += OnBeforeCheck;
			m_treeView.SetChecked(m_c1Node, TriStateTreeView.CheckState.Checked);

			Assert.That(m_fBeforeCheck, Is.True);
			Assert.That(m_treeView.GetChecked(m_c1Node), Is.EqualTo(TriStateTreeView.CheckState.Checked));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the BeforeCheck event is raised if first node is changed
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BeforeCheckCalled_FirstNode()
		{
			m_treeView.BeforeCheck += OnBeforeCheck;
			ReflectionHelper.CallMethod(m_treeView, "ChangeNodeState", m_aNode);
			Assert.That(m_fBeforeCheck, Is.True);
			Assert.That(m_treeView.GetChecked(m_aNode), Is.EqualTo(TriStateTreeView.CheckState.Checked));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the AfterCheck event is raised
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AfterCheckCalled()
		{
			m_treeView.AfterCheck += OnAfterCheck;
			m_treeView.SetChecked(m_c1Node, TriStateTreeView.CheckState.Checked);
			Assert.That(m_fAfterCheck, Is.True);
			Assert.That(m_treeView.GetChecked(m_c1Node), Is.EqualTo(TriStateTreeView.CheckState.Checked));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When the cancel flag in BeforeCheck returns true we don't want to change the
		/// state of the node.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void StateNotChangedIfBeforeCheckCancels()
		{
			m_treeView.BeforeCheck += OnBeforeCheck;
			m_treeView.AfterCheck += OnAfterCheck;
			m_fCancelInBeforeCheck = true;

			m_treeView.SetChecked(m_c1Node, TriStateTreeView.CheckState.Checked);

			Assert.That(m_fBeforeCheck, Is.True);
			Assert.That(m_fAfterCheck, Is.False);
			Assert.That(m_treeView.GetChecked(m_c1Node), Is.EqualTo(TriStateTreeView.CheckState.Unchecked));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks that the GetNodesWithState method returns the correct list of
		/// Checked nodes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetNodesWithState_Checked()
		{
			TreeNode[] list = m_treeView.GetNodesWithState(TriStateTreeView.CheckState.Checked);
			Assert.That(list, Is.Empty);

			m_treeView.SetChecked(m_c1Node, TriStateTreeView.CheckState.Checked);
			list = m_treeView.GetNodesWithState(TriStateTreeView.CheckState.Checked);

			Assert.That(list.Length, Is.EqualTo(2));
			Assert.That(list[0], Is.EqualTo(m_c1Node));
			Assert.That(list[1], Is.EqualTo(m_dNode));

			m_treeView.SetChecked(m_bNode, TriStateTreeView.CheckState.Checked);
			list = m_treeView.GetNodesWithState(TriStateTreeView.CheckState.Checked);

			Assert.That(list.Length, Is.EqualTo(5));
			Assert.That(list[0], Is.EqualTo(m_aNode));
			Assert.That(list[1], Is.EqualTo(m_bNode));
			Assert.That(list[2], Is.EqualTo(m_c1Node));
			Assert.That(list[3], Is.EqualTo(m_dNode));
			Assert.That(list[4], Is.EqualTo(m_c2Node));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks that the GetNodesWithState method returns the correct list of
		/// UnChecked nodes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetNodesWithState_Unchecked()
		{
			TreeNode[] list = m_treeView.GetNodesWithState(TriStateTreeView.CheckState.Checked);
			Assert.That(list, Is.Empty);

			// Check all nodes.
			m_treeView.SetChecked(m_aNode, TriStateTreeView.CheckState.Checked);
			list = m_treeView.GetNodesWithState(TriStateTreeView.CheckState.Unchecked);
			Assert.That(list, Is.Empty);

			m_treeView.SetChecked(m_c1Node, TriStateTreeView.CheckState.Unchecked);
			list = m_treeView.GetNodesWithState(TriStateTreeView.CheckState.Unchecked);

			Assert.That(list.Length, Is.EqualTo(2));
			Assert.That(list[0], Is.EqualTo(m_c1Node));
			Assert.That(list[1], Is.EqualTo(m_dNode));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks that the GetNodesWithState method returns the correct list of nodes when
		/// requested to return "GreyChecked", which really means all nodes, regardless of
		/// check state.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetNodesWithState_GreyChecked()
		{
			TreeNode[] list = m_treeView.GetNodesWithState(TriStateTreeView.CheckState.Checked);
			Assert.That(list, Is.Empty);

			m_treeView.SetChecked(m_c1Node, TriStateTreeView.CheckState.Checked);
			// TomB: I have redefined GreyChecked to be synonymous with Unchecked | Checked, so
			// it is no longer possible to ask how many nodes are strictly GreyChecked. There is
			// no place in the production code where we currently care to get a list of
			// GreyCecked nodes, and it seems unlikely we'll ever care.
			list = m_treeView.GetNodesWithState(TriStateTreeView.CheckState.GreyChecked);

			Assert.That(list.Length, Is.EqualTo(5));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks that the GetNodesOfTypeWithState method returns the correct list of
		/// Checked nodes of a certain type.
		/// </summary>
		/// m_aNode
		///   +- m_bNode
		///        +- c1Node
		///        |    +- dNode
		///        +- c2Node
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetNodesOfTypeWithState()
		{
			m_treeView.Nodes.Clear();

			DummyTreeNode1 dNode = new DummyTreeNode1("d");
			DummyTreeNode2 c2Node = new DummyTreeNode2("c2");
			DummyTreeNode2 c1Node = new DummyTreeNode2("c1", new TreeNode[] { dNode });
			m_bNode = new TreeNode("b", new TreeNode[] { c1Node, c2Node });
			m_aNode = new TreeNode("a", new TreeNode[] { m_bNode });
			m_treeView.Nodes.Add(m_aNode);

			m_treeView.SetChecked(c1Node, TriStateTreeView.CheckState.Checked);

			// Get Checked nodes of type DummyTreeNode1.
			TreeNode[] list = m_treeView.GetNodesOfTypeWithState(typeof(DummyTreeNode1),
				TriStateTreeView.CheckState.Checked);

			Assert.That(list.Length, Is.EqualTo(1));
			Assert.That(dNode, Is.EqualTo(list[0]));
			Assert.That(list[0], Is.TypeOf<DummyTreeNode1>());

			// Get Unchecked nodes of type DummyTreeNode2.
			list = m_treeView.GetNodesOfTypeWithState(typeof(DummyTreeNode2),
				TriStateTreeView.CheckState.Unchecked);

			Assert.That(list.Length, Is.EqualTo(1));
			Assert.That(c2Node, Is.EqualTo(list[0]));
			Assert.That(list[0], Is.TypeOf<DummyTreeNode2>());

			// Get nodes of type DummyTreeNode2 regardless of check state (Unchecked, Checked or Greyed).
			list = m_treeView.GetNodesOfTypeWithState(typeof(DummyTreeNode2),
				TriStateTreeView.CheckState.Unchecked |
				TriStateTreeView.CheckState.Checked);

			Assert.That(list.Length, Is.EqualTo(2));
			Assert.That(c1Node, Is.EqualTo(list[0]));
			Assert.That(c2Node, Is.EqualTo(list[1]));
			Assert.That(list[0], Is.TypeOf<DummyTreeNode2>());
			Assert.That(list[1], Is.TypeOf<DummyTreeNode2>());

			// Get nodes of type TreeNode regardless of check state (Unchecked, Checked or Greyed).
			list = m_treeView.GetNodesOfTypeWithState(typeof(TreeNode),
				TriStateTreeView.CheckState.GreyChecked);

			Assert.That(list.Length, Is.EqualTo(2));
			Assert.That(m_aNode, Is.EqualTo(list[0]));
			Assert.That(m_bNode, Is.EqualTo(list[1]));
			Assert.That(list[0], Is.TypeOf<TreeNode>());
			Assert.That(list[1], Is.TypeOf<TreeNode>());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks that CheckedNodes property returns the correct list of GreyChecked nodes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetCheckedTagData()
		{
			using (Button dummyButton = new Button())
			{
				using (Label dummyLabel = new Label())
				{
					m_bNode.Tag = dummyButton;
					m_c2Node.Tag = dummyLabel;
					m_treeView.SetChecked(m_bNode, TriStateTreeView.CheckState.Checked);
					System.Collections.ArrayList list = m_treeView.GetCheckedTagData();

					Assert.That(list.Count, Is.EqualTo(2));
					Assert.That(list[0], Is.EqualTo(dummyButton));
					Assert.That(list[1], Is.EqualTo(dummyLabel));
				}
			}
		}

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void OnBeforeCheck(object sender, TreeViewCancelEventArgs e)
		{
			e.Cancel = m_fCancelInBeforeCheck;
			m_fBeforeCheck = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void OnAfterCheck(object sender, TreeViewEventArgs e)
		{
			m_fAfterCheck = true;
		}
		#endregion
	}
}
