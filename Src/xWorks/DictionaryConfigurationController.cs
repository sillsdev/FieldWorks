// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
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
		private IDictionaryConfigurationView _view;

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
		void PopulateTreeView(DictionaryConfigurationModel model)
		{
			var rootNode = model.PartTree;

			AddNode(null, rootNode);

			_view.GetTreeView().AfterCheck += (sender, args) =>
			{
				ConfigurableDictionaryNode node = (ConfigurableDictionaryNode) args.Node.Tag;
				node.IsEnabled = args.Node.Checked;

				_view.Redraw();
			};

			_view.GetTreeView().AfterSelect += (sender, args) =>
			{
				ConfigurableDictionaryNode node = (ConfigurableDictionaryNode) args.Node.Tag;
				BuildAndShowOptions(node);
			};
		}

		private void AddNode(ConfigurableDictionaryNode parent, ConfigurableDictionaryNode node)
		{
			AddNodeToWidgetTree(parent, node);
			foreach (var child in node.Children)
			{
				AddNode(node, child);
			}
		}

		/// <summary>
		/// This method will add an individual node to the configuration tree attaching the <code>ConfigurableDictionaryNode</code>
		/// </summary>
		/// <param name="parentNode"></param>
		/// <param name="newNode"></param>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule", Justification = "TreeNode is owned by the view")]
		void AddNodeToWidgetTree(ConfigurableDictionaryNode parentNode, ConfigurableDictionaryNode newNode)
		{
			var newTreeNode = new TreeNode(newNode.Label) { Tag = newNode };

			var treeView = _view.GetTreeView();

			if (parentNode == null)
			{
				treeView.TopNode = newTreeNode;
				return;
			}

			// TODO: Fetch by object-as-key rather than by label-as-key. Could search the tree for a node that matches a tag.
			treeView.Nodes[parentNode.Label].Nodes.Add(newTreeNode);
		}

		public DictionaryConfigurationController()
		{
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