// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows.Forms;

namespace SIL.FieldWorks.XWorks
{
	class DictionaryConfigurationController
	{
		/// <summary>
		/// The current model being worked with
		/// </summary>
		private DictionaryConfigurationModel _model;

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
			var rootNode = model.PartTree;

			AddNode(null, rootNode);

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

		private void AddNode(ConfigurableDictionaryNode parent, ConfigurableDictionaryNode node)
		{
			AddNodeToWidgetTree(parent, node);
			if(node.Children != null)
			{
				foreach(var child in node.Children)
				{
					AddNode(node, child);
				}
			}
		}

		/// <summary>
		/// This method will add an individual node to the configuration tree attaching the <code>ConfigurableDictionaryNode</code>
		/// </summary>
		/// <param name="parentNode"></param>
		/// <param name="newNode"></param>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule", Justification = "TreeNode is owned by the view")]
		internal void AddNodeToWidgetTree(ConfigurableDictionaryNode parentNode, ConfigurableDictionaryNode newNode)
		{
			if (newNode == null)
				throw new ArgumentNullException();

			var newTreeNode = new TreeNode(newNode.Label) { Tag = newNode };

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
	}
}