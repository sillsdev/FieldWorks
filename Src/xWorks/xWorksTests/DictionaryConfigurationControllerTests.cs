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

		sealed class TestConfigurableDictionaryView : IDictionaryConfigurationView, IDisposable
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
