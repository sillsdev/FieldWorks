// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
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
		/// Available dictionary configurations (eg stem- and root-based)
		/// </summary>
		private Dictionary<string, DictionaryConfigurationModel> _alternateDictionaries;

		void SetAlternateDictionaryChoices(Mediator mediator)
		{
			var configurationType = mediator.PropertyTable.GetStringProperty("toolName", "Dictionary");
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
		internal void PopulateTreeView(DictionaryConfigurationModel model)
		{
			CreateTreeOfTreeNodes(null, model.Parts);

			View.TreeControl.Tree.AfterCheck += (sender, args) =>
			{
				var node = (ConfigurableDictionaryNode) args.Node.Tag;
				node.IsEnabled = args.Node.Checked;

				View.Redraw();
			};

			View.TreeControl.Tree.AfterSelect += (sender, args) =>
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
			PopulateTreeView(_model);
			view.ManageViews += (sender, args) => new DictionaryConfigMgrDlg(mediator, "", new List<XmlNode>(), null).ShowDialog(view as Form);
			view.SaveModel += (sender, args) => { /*_model.Save(); (needs to save in project config location, not default where it was loaded from) */ };

			view.TreeControl.MoveUp += node => Reorder(node.Tag as ConfigurableDictionaryNode, Direction.Up);
			view.TreeControl.MoveDown += node => Reorder(node.Tag as ConfigurableDictionaryNode, Direction.Down);
			view.TreeControl.Duplicate += node => { throw new NotImplementedException(); };
			view.TreeControl.Rename += node => { throw new NotImplementedException(); };
			view.TreeControl.Remove += node => { throw new NotImplementedException(); };
		}

		/// <summary>
		/// Populate options pane, from model.
		/// </summary>
		private void BuildAndShowOptions(ConfigurableDictionaryNode node)
		{
			var options = node.DictionaryNodeOptions;
			// todo: Hasso will put awesome code here to make a control from the node and put it
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
			View.TreeControl.Tree.Nodes.Clear();
			var rootNodes = _model.Parts;
			CreateTreeOfTreeNodes(null, rootNodes);
		}
	}
}