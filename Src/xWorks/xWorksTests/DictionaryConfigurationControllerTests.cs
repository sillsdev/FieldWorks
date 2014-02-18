// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;
using NUnit.Framework;
using SIL.FieldWorks.FDO.FDOTests;

namespace SIL.FieldWorks.XWorks
{
	[TestFixture]
	class DictionaryConfigurationControllerTests : MemoryOnlyBackendProviderTestBase
	{
		private DictionaryConfigurationModel m_model;

		[SetUp]
		public void Setup()
		{
			m_model = new DictionaryConfigurationModel();
		}

		[TearDown]
		public void TearDown()
		{

		}

		/// <summary>
		/// This test verifies that PopulateTreeView builds a TreeView that has the same structure as the model it is based on
		/// </summary>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule", Justification = "GetTreeView returns a reference")]
		[Test]
		public void PopulateTreeViewBuildsRightNumberOfNodes()
		{
			using(var testView = new TestConfigurableDictionaryView())
			{
				m_model.PartTree = BuildTestPartTree(2, 5);
				var dcc = new DictionaryConfigurationController { View = testView };

				//SUT
				dcc.PopulateTreeView(m_model);
				ValidateTreeForm(2, 5, dcc.View.GetTreeView());
			}
		}

		private void ValidateTreeForm(int levels, int nodeCount, TreeView treeView)
		{
			var validationCount = 0;
			var validationLevels = 0;
			CalculateTreeInfo(ref validationLevels, ref validationCount, treeView.Nodes);
			Assert.AreEqual(levels, validationLevels, "Tree heirarchy incorrect");
			Assert.AreEqual(nodeCount, validationCount, "Tree node count incorrect");
		}

		private void CalculateTreeInfo(ref int levels, ref int count, TreeNodeCollection nodes)
		{
			if(nodes == null || nodes.Count < 1)
			{
				return;
			}
			++levels;
			foreach(TreeNode node in nodes)
			{
				++count;
				if(node.Nodes.Count > 0)
				{
					CalculateTreeInfo(ref levels, ref count, node.Nodes);
				}
			}
		}

		/// <summary>
		/// Builds a test tree of ConfigurableDictionary nodes with the given numbers of levels and nodes
		/// the structure of the tree is TODO: say what we did once this test code stabalizes
		/// </summary>
		/// <param name="numberOfLevels"></param>
		/// <param name="numberOfNodes"></param>
		/// <returns></returns>
		private ConfigurableDictionaryNode BuildTestPartTree(int numberOfLevels, int numberOfNodes)
		{
			if(numberOfLevels < 1)
			{
				throw new ArgumentException("You wanted less than one level in the heirarchy, really?");
			}

			if(numberOfNodes < numberOfLevels)
			{
				throw new ArgumentException("You asked for more levels in the heirarchy then nodes in the tree, how did you expect me to do that?");
			}
			ConfigurableDictionaryNode rootNode = null;
			ConfigurableDictionaryNode workingNode = null;
			var children = new List<ConfigurableDictionaryNode>();
			for(var i = 0; i < numberOfLevels; ++i)
			{
				if(workingNode == null)
				{
					workingNode = rootNode = new ConfigurableDictionaryNode { Label = "root" };
					continue;
				}
				children = new List<ConfigurableDictionaryNode>();
				workingNode.Children = children;
				workingNode = new ConfigurableDictionaryNode { Label = "level" + i };
				children.Add(workingNode);
			}
			// Add remaining desired nodes at the bottom
			for(var i = 0; i < numberOfNodes - numberOfLevels; ++i)
			{
				children.Add(new ConfigurableDictionaryNode { Label = "extraChild" + i });
			}
			return rootNode;
		}

		/// <summary/>
		[Test]
		public void FindTreeNode_ThrowsOnNullArgument()
		{
			var node = new ConfigurableDictionaryNode();
			using (var treeView = new TreeView())
			{
				var collection = treeView.Nodes;
				// SUT
				Assert.Throws<ArgumentNullException>(() => DictionaryConfigurationController.FindTreeNode(null, collection));
				Assert.Throws<ArgumentNullException>(() => DictionaryConfigurationController.FindTreeNode(node, null));
				Assert.Throws<ArgumentNullException>(() => DictionaryConfigurationController.FindTreeNode(null, null));
			}
		}

		/// <summary/>
		[Test]
		public void FindTreeNode_CanFindRoot()
		{
			var node = new ConfigurableDictionaryNode();
			using (var treeView = new TreeView())
			{
				var treeNode = new TreeNode();
				treeNode.Tag = node;
				treeView.Nodes.Add(treeNode);
				treeView.TopNode = treeNode;
				// SUT
				var returnedTreeNode = DictionaryConfigurationController.FindTreeNode(node, treeView.Nodes);
				Assert.That(returnedTreeNode.Tag, Is.EqualTo(node));
			}
		}

		/// <summary/>
		[Test]
		public void FindTreeNode_CanFindChild()
		{
			var node = new ConfigurableDictionaryNode();
			using (var treeView = new TreeView())
			{
				var treeNode = new TreeNode();
				treeNode.Tag = node;
				treeView.Nodes.Add(new TreeNode());
				// Adding a decoy tree node first
				treeView.Nodes[0].Nodes.Add(new TreeNode());
				treeView.Nodes[0].Nodes.Add(treeNode);
				// SUT
				var returnedTreeNode = DictionaryConfigurationController.FindTreeNode(node, treeView.Nodes);
				Assert.That(returnedTreeNode.Tag, Is.EqualTo(node));
			}
		}

		/// <summary/>
		[Test]
		public void FindTreeNode_ReturnsNullIfNotFound()
		{
			var node = new ConfigurableDictionaryNode();
			using (var treeView = new TreeView())
			{
				// Decoys
				treeView.Nodes.Add(new TreeNode());
				treeView.Nodes[0].Nodes.Add(new TreeNode());
				// SUT
				var returnedTreeNode = DictionaryConfigurationController.FindTreeNode(node, treeView.Nodes);
				Assert.That(returnedTreeNode, Is.EqualTo(null));
			}
		}

		/// <summary/>
		[Test]
		public void CreateAndAddTreeNodeForNode_ThrowsOnNullNodeArgument()
		{
			var controller = new DictionaryConfigurationController();
			var parentNode = new ConfigurableDictionaryNode();
			// SUT
			Assert.Throws<ArgumentNullException>(() => controller.CreateAndAddTreeNodeForNode(parentNode, null));
			Assert.Throws<ArgumentNullException>(() => controller.CreateAndAddTreeNodeForNode(null, null));
		}

		/// <summary/>
		[Test]
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule", Justification = "GetTreeView returns a reference")]
		public void CreateAndAddTreeNodeForNode_CanAddRoot()
		{
			var controller = new DictionaryConfigurationController();
			var node = new ConfigurableDictionaryNode();
			using (var dummyView = new TestConfigurableDictionaryView())
			{
				controller.View = dummyView;
				// SUT
				controller.CreateAndAddTreeNodeForNode(null, node);
				Assert.That(controller.View.GetTreeView().Nodes.Count, Is.EqualTo(1), "No TreeNode was added");
				Assert.That(controller.View.GetTreeView().Nodes[0].Tag, Is.EqualTo(node), "New TreeNode's tag does not match");
			}
		}

		/// <summary/>
		[Test]
		public void CreateTreeOfTreeNodes_ThrowsOnNullNodeArgument()
		{
			var controller = new DictionaryConfigurationController();
			var parentNode = new ConfigurableDictionaryNode();
			// SUT
			Assert.Throws<ArgumentNullException>(() => controller.CreateTreeOfTreeNodes(parentNode, null));
			Assert.Throws<ArgumentNullException>(() => controller.CreateTreeOfTreeNodes(null, null));
		}

		/// <summary/>
		[Test]
		public void CreateTreeOfTreeNodes_CanCreateOneLevelTree()
		{
			var controller = new DictionaryConfigurationController() {View = new TestConfigurableDictionaryView()};
			var rootNode = new ConfigurableDictionaryNode() {Label = "0", Children = new List<ConfigurableDictionaryNode>()};
			// SUT
			controller.CreateTreeOfTreeNodes(null, rootNode);

			BasicTreeNodeVerification(controller, rootNode);
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule", Justification = "GetTreeView returns a reference")]
		private TreeNode BasicTreeNodeVerification(DictionaryConfigurationController controller, ConfigurableDictionaryNode rootNode)
		{
			Assert.That(controller.View.GetTreeView().Nodes[0].Tag, Is.EqualTo(rootNode), "root TreeNode does not corresponded to expected dictionary configuration node");
			Assert.That(controller.View.GetTreeView().Nodes.Count, Is.EqualTo(1), "Did not expect more than one root TreeNode");
			var rootTreeNode = controller.View.GetTreeView().Nodes[0];
			VerifyTreeNodeHierarchy(rootTreeNode);
			Assert.That(rootTreeNode.Nodes.Count, Is.EqualTo(rootNode.Children.Count), "root treenode does not have expected number of descendants");
			return rootTreeNode;
		}

		/// <summary/>
		[Test]
		public void CreateTreeOfTreeNodes_CanCreateTwoLevelTree()
		{
			var controller = new DictionaryConfigurationController() { View = new TestConfigurableDictionaryView() };
			var rootNode = new ConfigurableDictionaryNode() { Label = "0", Children = new List<ConfigurableDictionaryNode>() };
			AddChildrenToNode(rootNode, 3);
			// SUT
			controller.CreateTreeOfTreeNodes(null, rootNode);

			var rootTreeNode = BasicTreeNodeVerification(controller, rootNode);
			string errorMessage = "Should not have made any third-level children that did not exist in the dictionary configuration node hierarchy";
			for (int i = 0; i < 3; i++)
				Assert.That(rootTreeNode.Nodes[i].Nodes.Count, Is.EqualTo(rootNode.Children[i].Children.Count), errorMessage); // ie 0
		}

		/// <summary>
		/// Will create tree with one root node having two child nodes.
		/// The first of those second-level children will have 2 children,
		/// and the second of the second-level children will have 3 children.
		/// </summary>
		[Test]
		public void CreateTreeOfTreeNodes_CanCreateThreeLevelTree()
		{
			var controller = new DictionaryConfigurationController() { View = new TestConfigurableDictionaryView() };
			var rootNode = new ConfigurableDictionaryNode() {Label = "0", Children = new List<ConfigurableDictionaryNode>()};
			AddChildrenToNode(rootNode, 2);
			AddChildrenToNode(rootNode.Children[0], 2);
			AddChildrenToNode(rootNode.Children[1], 3);

			// SUT
			controller.CreateTreeOfTreeNodes(null, rootNode);

			var rootTreeNode = BasicTreeNodeVerification(controller, rootNode);
			string errorMessage = "Did not make correct number of third-level children";
			Assert.That(rootTreeNode.Nodes[0].Nodes.Count, Is.EqualTo(rootNode.Children[0].Children.Count), errorMessage); // ie 2
			Assert.That(rootTreeNode.Nodes[1].Nodes.Count, Is.EqualTo(rootNode.Children[1].Children.Count), errorMessage); // ie 3
			string errorMessage2 = "Should not have made any fourth-level children that did not exist in the dictionary configuration node hierarchy.";
			for (int i = 0; i < 2; i++)
				Assert.That(rootTreeNode.Nodes[0].Nodes[i].Nodes.Count, Is.EqualTo(rootNode.Children[0].Children[i].Children.Count), errorMessage2); // ie 0
			for (int i = 0; i < 3; i++)
				Assert.That(rootTreeNode.Nodes[1].Nodes[i].Nodes.Count, Is.EqualTo(rootNode.Children[1].Children[i].Children.Count), errorMessage2); // ie 0
		}

		/// <summary/>
		public void AddChildrenToNode(ConfigurableDictionaryNode node, int numberOfChildren)
		{
			for (int childIndex = 0; childIndex < numberOfChildren; childIndex++)
			{
				var child = new ConfigurableDictionaryNode() { Label = node.Label + "." + childIndex, Children = new List<ConfigurableDictionaryNode>()};
				node.Children.Add(child);
			}
		}

		/// <summary>
		/// Verify that all descendants of treeNode are associated with
		/// ConfigurableDictionaryNode objects with labels that match
		/// the hierarchy that they are found in the TreeNode.
		/// </summary>
		public void VerifyTreeNodeHierarchy(TreeNode treeNode)
		{
			var label = ((ConfigurableDictionaryNode)treeNode.Tag).Label;
			for (int childIndex = 0; childIndex < treeNode.Nodes.Count; childIndex++)
			{
				var child = treeNode.Nodes[childIndex];
				var childLabel = ((ConfigurableDictionaryNode) child.Tag).Label;
				var expectedChildLabel = label + "." + childIndex;
				Assert.That(childLabel, Is.EqualTo(expectedChildLabel), "TreeNode child has associated configuration dictionary node with wrong label");
				VerifyTreeNodeHierarchy(child);
			}
		}

		private sealed class TestConfigurableDictionaryView : IDictionaryConfigurationView, IDisposable
		{
			private TreeView view = new TreeView();

			public TreeView GetTreeView()
			{
				return view;
			}

			public void Redraw()
			{
				;
			}

			public void Dispose()
			{
				view.Dispose();
			}
		}
	}
}
