// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using XCore;

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
		/// Controls the portion of the dialog where an element in a dictionary entry is configured in detail
		/// </summary>
		private DictionaryDetailsController DetailsController { get; set; }

		/// <summary>
		/// Available dictionary configurations (eg stem- and root-based)
		/// </summary>
		private Dictionary<string, DictionaryConfigurationModel> _alternateDictionaries;

		void SetAlternateDictionaryChoices(Mediator mediator)
		{
			var configurationType = DictionaryConfigurationListener.GetDictionaryConfigurationType(mediator);
			// Figure out what alternate dictionaries are available (eg root-, stem-, ...)
			// Populate _alternateDictionaries with available models.
			// Populate view's list of alternate dictionaries with available choices.
			var cache = (FdoCache)mediator.PropertyTable.GetValue("cache");
			var projectConfigDir = Path.Combine(DirectoryFinder.GetConfigSettingsDir(cache.ProjectId.ProjectFolder), configurationType);
			var defaultConfigDir = Path.Combine(DirectoryFinder.DefaultConfigurations, configurationType);

			var choices = ReadAlternateDictionaryChoices(defaultConfigDir, projectConfigDir);
			_alternateDictionaries = new Dictionary<string, DictionaryConfigurationModel>();
			foreach(var choice in choices)
			{
				_alternateDictionaries[choice.Key] = new DictionaryConfigurationModel(choice.Value);
			}
			View.SetChoices(choices.Keys);
		}

		/// <summary>
		/// Loads a dictionary with the configuration choices from two folders with the project specific
		/// configurations overriding the default configurations of the same name.
		/// </summary>
		/// <param name="defaultPath"></param>
		/// <param name="projectPath"></param>
		/// <returns></returns>
		internal Dictionary<string, string> ReadAlternateDictionaryChoices(string defaultPath, string projectPath)
		{
			var choices = new Dictionary<string, string>();
			var defaultFiles = new List<string>(Directory.EnumerateFiles(defaultPath, "*.xml"));
			if(!Directory.Exists(projectPath))
			{
				Directory.CreateDirectory(projectPath);
			}
			else
			{
				foreach(var choice in Directory.EnumerateFiles(projectPath, "*.xml"))
				{
					choices[Path.GetFileNameWithoutExtension(choice)] = choice;
				}
			}
			foreach(var file in defaultFiles)
			{
				var niceName = Path.GetFileNameWithoutExtension(file);
				if(!choices.ContainsKey(niceName))
				{
					choices[niceName] = file;
				}
			}
			return choices;
		}

		/// <summary>
		/// Populate dictionary elements tree, from model.
		/// </summary>
		internal void PopulateTreeView(Mediator mediator)
		{
			RefreshView();
		}

		/// <summary>
		/// Refresh view from model. Try to select nodeToSelect in the view afterward. If nodeToSelect is null, try to preserve the existing node selection.
		/// </summary>
		private void RefreshView(ConfigurableDictionaryNode nodeToSelect = null)
		{
			var tree = View.TreeControl.Tree;
			var expandedNodes = new List<ConfigurableDictionaryNode>();
			FindExpandedNodes(tree.Nodes, ref expandedNodes);

			ConfigurableDictionaryNode topVisibleNode = null;
			if (tree.TopNode != null)
				topVisibleNode = tree.TopNode.Tag as ConfigurableDictionaryNode;

			if (nodeToSelect == null && tree.SelectedNode != null)
				nodeToSelect = tree.SelectedNode.Tag as ConfigurableDictionaryNode;

			// Rebuild view from model
			tree.Nodes.Clear();
			var rootNodes = _model.Parts;
			CreateTreeOfTreeNodes(null, rootNodes);

			// Preserve treenode expansions
			foreach (var expandedNode in expandedNodes)
				FindTreeNode(expandedNode, tree.Nodes).Expand();

			if (nodeToSelect != null)
				tree.SelectedNode = FindTreeNode(nodeToSelect, tree.Nodes);
			// Fallback to selecting first root, trying to make sure there is always a selection for the buttons to be enabled or disabled with respect to.
			if (tree.SelectedNode == null && tree.Nodes.Count > 0)
				tree.SelectedNode = tree.Nodes[0];

			// Try to prevent scrolling away from what the user was seeing in the tree. But if necessary, scroll so the selected node is visible.
			if (topVisibleNode != null)
				tree.TopNode = FindTreeNode(topVisibleNode, tree.Nodes);
			if (tree.SelectedNode != null)
				tree.SelectedNode.EnsureVisible();
		}

		/// <summary>
		/// Populate a list of dictionary nodes that correspond to treenodes that are expanded in the 'treenodes' or its children.
		/// </summary>
		private static void FindExpandedNodes(TreeNodeCollection treenodes, ref List<ConfigurableDictionaryNode> expandedNodes)
		{
			foreach (TreeNode treenode in treenodes)
			{
				if (treenode.IsExpanded)
					expandedNodes.Add(treenode.Tag as ConfigurableDictionaryNode);
				FindExpandedNodes(treenode.Nodes, ref expandedNodes);
			}
		}

		/// <summary>
		/// Create a tree of TreeNodes from a list of nodes and their Children, adding
		/// them into the TreeView parented by the TreeNode corresponding
		/// to parent.
		/// If parent is null, the nodes are added as direct children of the TreeView
		/// </summary>
		internal void CreateTreeOfTreeNodes(ConfigurableDictionaryNode parent, List<ConfigurableDictionaryNode> nodes)
		{
			if (nodes == null)
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

			var newTreeNode = new TreeNode(node.Label) { Tag = node, Checked = node.IsEnabled };

			var treeView = View.TreeControl.Tree;

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
				if(nodeToMatch.Equals(treeNode.Tag))
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
		/// <param name="mediator"></param>
		public DictionaryConfigurationController(IDictionaryConfigurationView view, Mediator mediator)
		{
			View = view;
			SetAlternateDictionaryChoices(mediator);
			var lastUsedAlternateDictionary =
				mediator.PropertyTable.GetStringProperty("LastDictionaryConfiguration", "Root");
			_model = _alternateDictionaries.ContainsKey(lastUsedAlternateDictionary)
						? _alternateDictionaries[lastUsedAlternateDictionary]
						: _alternateDictionaries.Values.First();
			// Populate the tree view with the users last configuration, or the first one in the list of alternates.
			PopulateTreeView(mediator);
			View.ManageViews += (sender, args) => new DictionaryConfigMgrDlg(mediator, "", new List<XmlNode>(), null).ShowDialog(View as Form);
			View.SaveModel += (sender, args) => { _model.FilePath = GetProjectConfigLocationForPath(_model.FilePath, mediator); _model.Save(); };
			view.SwitchView += (sender, args) => { _model = _alternateDictionaries[args.ViewPicked]; RefreshView(); };

			View.TreeControl.MoveUp += node => Reorder(node.Tag as ConfigurableDictionaryNode, Direction.Up);
			View.TreeControl.MoveDown += node => Reorder(node.Tag as ConfigurableDictionaryNode, Direction.Down);
			View.TreeControl.Duplicate += node =>
			{
				var dictionaryNode = node.Tag as ConfigurableDictionaryNode;
				var siblings = dictionaryNode.Parent == null ? _model.Parts : dictionaryNode.Parent.Children;

				var duplicate = dictionaryNode.DuplicateAmongSiblings(siblings);
				RefreshView(duplicate);
			};
			View.TreeControl.Rename += node =>
			{
				var dictionaryNode = node.Tag as ConfigurableDictionaryNode;
				var siblings = dictionaryNode.Parent == null ? _model.Parts : dictionaryNode.Parent.Children;

				using (var renameDialog = new DictionaryConfigurationNodeRenameDlg())
				{
					renameDialog.NewName = dictionaryNode.Label;

					// Unchanged?
					if (renameDialog.ShowDialog() != DialogResult.OK || renameDialog.NewName == dictionaryNode.Label)
						return;

					if (!dictionaryNode.Relabel(renameDialog.NewName, siblings))
					{
						MessageBox.Show("Failed to rename. Use a name that is not already in use.");
						return;
					}
				}
				RefreshView();
			};
			View.TreeControl.Remove += node =>
			{
				var dictionaryNode = node.Tag as ConfigurableDictionaryNode;
				if (dictionaryNode.Parent == null)
					_model.Parts.Remove(dictionaryNode);
				else
					dictionaryNode.UnlinkFromParent();
				RefreshView();
			};

			View.TreeControl.Tree.AfterCheck += (sender, args) =>
			{
				var node = (ConfigurableDictionaryNode) args.Node.Tag;
				node.IsEnabled = args.Node.Checked;

				// Details may need enabled or disabled
				RefreshView();
			};

			View.TreeControl.Tree.AfterSelect += (sender, args) =>
			{
				var node = (ConfigurableDictionaryNode) args.Node.Tag;

				View.TreeControl.MoveUpEnabled = CanReorder(node, Direction.Up);
				View.TreeControl.MoveDownEnabled = CanReorder(node, Direction.Down);
				View.TreeControl.DuplicateEnabled = true;
				View.TreeControl.RemoveEnabled = node.IsDuplicate;
				View.TreeControl.RenameEnabled = node.IsDuplicate;

				BuildAndShowOptions(node, mediator);
			};
		}

		internal string GetProjectConfigLocationForPath(string filePath, Mediator mediator)
		{
			var cache = (FdoCache)mediator.PropertyTable.GetValue("cache");
			var projectConfigDir = DirectoryFinder.GetConfigSettingsDir(cache.ProjectId.ProjectFolder);
			if(filePath.StartsWith(projectConfigDir))
			{
				return filePath;
			}
			var detailedConfig =
				filePath.Substring(DirectoryFinder.DefaultConfigurations.Length + 1);
			return Path.Combine(projectConfigDir, detailedConfig);
		}

		/// <summary>
		/// Populate options pane, from model.
		/// </summary>
		private void BuildAndShowOptions(ConfigurableDictionaryNode node, Mediator mediator)
		{
			if (DetailsController == null)
				DetailsController = new DictionaryDetailsController(node, mediator);
			else
				DetailsController.LoadNode(node);
			View.DetailsView = DetailsController.View;
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

			RefreshView();
		}
	}
}
