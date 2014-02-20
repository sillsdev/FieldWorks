// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace SIL.FieldWorks.XWorks
{
	class DictionaryConfigurationController
	{
		/// <summary>
		/// The current model being worked with
		/// </summary>
		internal DictionaryConfigurationModel _model;

		/// <summary>
		/// The view to display the model in
		/// </summary>
		internal IDictionaryConfigurationView View { get; set; }

		/// <summary>
		/// Available dictionary configurations (eg stem- and root-based)
		/// </summary>
		private Dictionary<string, DictionaryConfigurationModel> _alternateDictionaries;

		void SetAlternateDictionaryChoices()
		{
			// Figure out what alternate dictionaries are available (eg root-, stem-, ...)
			// Populate _alternateDictionaries with available models.
			// Populate view's list of alternate dictionaries with available choices.

			throw new NotImplementedException();
		}

		/// <summary>
		/// Populate dictionary elements tree, from model.
		/// </summary>
		internal void PopulateTreeView(DictionaryConfigurationModel model)
		{
			CreateTreeOfTreeNodes(null, model.Parts);

			View.GetTreeView().AfterCheck += (sender, args) =>
			{
				var node = (ConfigurableDictionaryNode) args.Node.Tag;
				node.IsEnabled = args.Node.Checked;

				View.Redraw();
			};

			View.GetTreeView().AfterSelect += (sender, args) =>
			{
				var node = (ConfigurableDictionaryNode) args.Node.Tag;
				BuildAndShowOptions(node);
			};
		}

		/// <summary>
		/// Create a tree of TreeNodes from a list of nodes and their Children, adding
		/// them into the TreeView parented by the TreeNode corresponding
		/// to parent.
		/// If parent is null, the nodes are added as direct children of the TreeView
		/// </summary>
		internal void CreateTreeOfTreeNodes(ConfigurableDictionaryNode parent, List<ConfigurableDictionaryNode> nodes)
		{
			if(nodes == null)
				throw new ArgumentNullException();

			foreach(var node in nodes)
			{
				CreateAndAddTreeNodeForNode(parent, node);
				if(node.Children != null)
				{
					CreateTreeOfTreeNodes(node, node.Children);
				}
			}
		}

		/// <summary>
		/// Create a TreeNode corresponding to node, and add it to the
		/// TreeView parented by the TreeNode corresponding to parentNode.
		/// If parentNode is null, node is considered to be at the root.
		/// </summary>
		/// <param name="parentNode"></param>
		/// <param name="node"></param>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule", Justification = "TreeNode is owned by the view")]
		internal void CreateAndAddTreeNodeForNode(ConfigurableDictionaryNode parentNode, ConfigurableDictionaryNode node)
		{
			if (node == null)
				throw new ArgumentNullException();

			var newTreeNode = new TreeNode(node.Label) { Tag = node };

			var treeView = View.GetTreeView();

			if (parentNode == null)
			{
				treeView.Nodes.Add(newTreeNode);
				treeView.TopNode = newTreeNode;
				return;
			}

			var parentTreeNode = FindTreeNode(parentNode, treeView.Nodes);
			if (parentTreeNode != null)
			{
				parentTreeNode.Nodes.Add(newTreeNode);
			}
		}

		/// <summary>
		/// FindTreeNode returns the treenode which has the tag that matches nodeToMatch, or null
		/// </summary>
		/// <param name="nodeToMatch"></param>
		/// <param name="treeNodeCollection"></param>
		/// <returns></returns>
		internal static TreeNode FindTreeNode(ConfigurableDictionaryNode nodeToMatch, TreeNodeCollection treeNodeCollection)
		{
			if(nodeToMatch == null || treeNodeCollection == null)
			{
				throw new ArgumentNullException();
			}
			foreach(TreeNode treeNode in treeNodeCollection)
			{
				if(treeNode.Tag == nodeToMatch)
				{
					return treeNode;
				}
				var branchResult = FindTreeNode(nodeToMatch, treeNode.Nodes);
				if(branchResult != null)
				{
					return branchResult;
				}
			}
			return null;
		}

		/// <summary>
		/// Default constructor to make testing easier.
		/// </summary>
		internal DictionaryConfigurationController()
		{
		}

		/// <summary>
		/// Constructs a DictionaryConfigurationController with a view and a model pulled from user settings
		/// </summary>
		/// <param name="view"></param>
		public DictionaryConfigurationController(IDictionaryConfigurationView view)
		{
			View = view;
			SetAlternateDictionaryChoices();
			var lastUsedAlternateDictionary = ""; // TODO: fetch from cache or settings
			PopulateTreeView(_alternateDictionaries[lastUsedAlternateDictionary]);
		}

		/// <summary>
		/// Populate options pane, from model.
		/// </summary>
		private void BuildAndShowOptions(ConfigurableDictionaryNode node)
		{
			var options = node.DictionaryNodeOptions;

			throw new NotImplementedException();
		}

		/// <summary>
		/// Whether node can be moved among its siblings.
		/// </summary>
		public static bool CanReorder(ConfigurableDictionaryNode node, Direction direction)
		{
			if (node == null)
				throw new ArgumentNullException();

			var parent = node.Parent;
			// Root node can't be moved
			if (parent == null)
				return false;

			var nodeIndex = parent.Children.IndexOf(node);
			if (direction == Direction.Up && nodeIndex == 0)
				return false;
			var lastSiblingIndex = parent.Children.Count - 1;
			if (direction == Direction.Down && nodeIndex == lastSiblingIndex)
				return false;

			return true;
		}

		/// <summary>
		/// Represents the direction of moving a configuration node among its siblings. (Not up or down a hierarchy).
		/// </summary>
		internal enum Direction
		{
			Up,
			Down
		};

		/// <summary>
		/// Move a node among its siblings in the model, and cause the view to update accordingly.
		/// </summary>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule", Justification = "GetTreeView returns a reference")]
		public void Reorder(ConfigurableDictionaryNode node, Direction direction)
		{
			if (node == null)
				throw new ArgumentNullException();
			if (!CanReorder(node, direction))
				throw new ArgumentOutOfRangeException();
			var parent = node.Parent;
			var nodeIndex = parent.Children.IndexOf(node);

			// For Direction.Up
			var newNodeIndex = nodeIndex - 1;
			// or Down
			if (direction == Direction.Down)
				newNodeIndex = nodeIndex + 1;

			parent.Children.RemoveAt(nodeIndex);
			parent.Children.Insert(newNodeIndex, node);

			// Update view
			View.GetTreeView().Nodes.Clear();
			var rootNodes = _model.Parts;
			CreateTreeOfTreeNodes(null, rootNodes);
		}
	}
}