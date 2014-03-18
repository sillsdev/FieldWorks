// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;
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

			var newTreeNode = new TreeNode(node.DisplayLabel) { Tag = node, Checked = node.IsEnabled };

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
			var cache = (FdoCache)mediator.PropertyTable.GetValue("cache");
			MergeCustomFieldsIntoDictionaryModel(cache, _model);
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
					renameDialog.DisplayLabel = dictionaryNode.DisplayLabel;
					renameDialog.NewSuffix = dictionaryNode.LabelSuffix;

					// Unchanged?
					if (renameDialog.ShowDialog() != DialogResult.OK || renameDialog.NewSuffix == dictionaryNode.LabelSuffix)
						return;

					if (!dictionaryNode.ChangeSuffix(renameDialog.NewSuffix, siblings))
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

		public static void MergeCustomFieldsIntoDictionaryModel(FdoCache cache, DictionaryConfigurationModel model)
		{
			foreach(var part in model.Parts)
			{
				var customFields = GetCustomFieldsForType(cache, part.FieldDescription);
				if(part.Children == null)
					part.Children = new List<ConfigurableDictionaryNode>();
				MergeCustomFieldLists(part, customFields);
				MergeCustomFieldsIntoDictionaryModel(cache, part.FieldDescription, part.Children);
			}
		}

		/// <summary>
		/// This helper method is used to recurse into all of the configuration nodes in a DictionaryModel and merge the custom fields
		/// in each ConfigurableDictionaryNode with those defined in the FieldWorks model according to the metadata cache.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="parentClass"></param>
		/// <param name="configurationList"></param>
		private static void MergeCustomFieldsIntoDictionaryModel(FdoCache cache, string parentClass, List<ConfigurableDictionaryNode> configurationList)
		{
			if(configurationList == null)
				return;
			var metaDataCache = (IFwMetaDataCacheManaged)cache.MetaDataCacheAccessor;
			foreach(var configNode in configurationList)
			{
				// The class that contains the type information for the field we are inspecting
				var lookupClass = parentClass;
				if(!metaDataCache.FieldExists(parentClass, configNode.FieldDescription, true))
				{
					Debug.WriteLine(@"Couldn't locate {0} in the MetaDataCache for the class {1}: ConfigNode {2} under {3}",
										 configNode.FieldDescription, parentClass, configNode.Label, configNode.Parent.Label);
					continue; // The field as specified in the configuration can not be looked up in the metadata, but may still exist
				}
				// The type of the field configured by configNode that we will check for user defined custom fields
				var fieldType = metaDataCache.GetFieldType(metaDataCache.GetFieldId(parentClass, configNode.FieldDescription, true));
				// If there is a sub field then the type we are interested in belongs to a property of the field in the configNode
				if(!String.IsNullOrEmpty(configNode.SubField))
				{
					lookupClass = GetClassNameForTargetType(fieldType, parentClass, configNode.FieldDescription,
																		 metaDataCache);
					if(!metaDataCache.FieldExists(lookupClass, configNode.SubField, true))
					{
						Debug.WriteLine(@"Couldn't locate {0} in the MetaDataCache for the class {1}: ConfigNode {2} under {3}",
											 configNode.SubField, lookupClass, configNode.Label, configNode.Parent.Label);
						continue; // The field as specified in the configuration can not be looked up in the metadata, but may still exist
					}
					fieldType = metaDataCache.GetFieldType(metaDataCache.GetFieldId(lookupClass, configNode.SubField, true));
				}
				string className = GetClassNameForTargetType(fieldType, lookupClass, configNode.SubField ?? configNode.FieldDescription, metaDataCache);
				var fieldsForType = GetCustomFieldsForType(cache, className);
				if(fieldsForType.Count > 0)
				{
					if(configNode.Children == null)
					{
						configNode.Children = new List<ConfigurableDictionaryNode>();
					}
					MergeCustomFieldLists(configNode, fieldsForType);
				}
				// recurse into the rest of the dictionary model
				MergeCustomFieldsIntoDictionaryModel(cache, className, configNode.Children);
			}
		}

		private static void MergeCustomFieldLists(ConfigurableDictionaryNode parent, List<ConfigurableDictionaryNode> customFieldNodes)
		{
			var children = parent.Children;
			// Traverse through the children from end to beginning removing any custom fields that no longer exist.
			for(var i = children.Count - 1; i >= 0; --i)
			{
				var configNode = children[i];
				if(configNode.IsCustomField && !customFieldNodes.Contains(configNode))
				{
					children.Remove(configNode);
				}
				if(configNode.IsCustomField && customFieldNodes.Contains(configNode))
				{
					customFieldNodes.Remove(configNode);
				}
			}
			// Then add any custom fields that don't yet exist in the children configurationList to the end.
			foreach(var customField in customFieldNodes)
			{
				customField.Parent = parent;
			}
			children.AddRange(customFieldNodes);
		}

		/// <summary>
		/// Generate a list of ConfigurableDictionaryNode objects to represent each custom field of the given type.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="className"></param>
		/// <returns></returns>
		public static List<ConfigurableDictionaryNode> GetCustomFieldsForType(FdoCache cache, string className)
		{
			var customFieldList = new List<ConfigurableDictionaryNode>();
			var metaDataCache = (IFwMetaDataCacheManaged)cache.MetaDataCacheAccessor;
			var lexEntryId = metaDataCache.GetClassId(className);
			var size = metaDataCache.GetFields(lexEntryId, true, (int)CellarPropertyTypeFilter.All, 0, null);
			using(var fields = MarshalEx.ArrayToNative<int>(size))
			{
				metaDataCache.GetFields(lexEntryId, true, (int)CellarPropertyTypeFilter.All, size, fields);
				var fieldArray = MarshalEx.NativeToArray<int>(fields, size);
				foreach(var field in fieldArray)
				{
					if(metaDataCache.IsCustom(field))
					{
						var configNode = new ConfigurableDictionaryNode();
						configNode.Label = metaDataCache.GetFieldLabel(field) ?? metaDataCache.GetFieldName(field);
						configNode.IsCustomField = true;
						configNode.IsEnabled = false;
						configNode.FieldDescription = metaDataCache.GetFieldName(field);
						configNode.DictionaryNodeOptions = BuildOptionsForType(metaDataCache.GetFieldType(field));
						customFieldList.Add(configNode);
					}
				}
			}
			return customFieldList;
		}

		/// <summary>
		/// This method will return the class name string in the FieldWorks model that represents the data type of the
		/// given in fieldType.
		/// In cases where this is a simple type the class name is returned directly but in cases where the type in the model
		/// is a reference the class name string is that of the destination class for the reference.
		/// </summary>
		/// <param name="fieldType"></param>
		/// <param name="lookupClass"></param>
		/// <param name="metaDataCache"></param>
		/// <param name="fieldName"></param>
		/// <returns></returns>
		private static string GetClassNameForTargetType(int fieldType,
																		string lookupClass,
																		string fieldName,
																		IFwMetaDataCacheManaged metaDataCache)
		{
			string className;
			// These types in the FieldWorks model only point to or contain the class we are interested in, so we grab their destination class
			if(fieldType == (int)CellarPropertyType.OwningSequence ||
				fieldType == (int)CellarPropertyType.OwningCollection ||
				fieldType == (int)CellarPropertyType.OwningAtomic ||
				fieldType == (int)CellarPropertyType.ReferenceCollection ||
				fieldType == (int)CellarPropertyType.ReferenceSequence ||
				fieldType == (int)CellarPropertyType.ReferenceAtomic)
			{
				var destinationClass =
					metaDataCache.GetDstClsId(metaDataCache.GetFieldId(lookupClass, fieldName, true));
				className = metaDataCache.GetClassName(destinationClass);
			}
			else
				className = metaDataCache.GetClassName(fieldType);
			return className;
		}

		private static DictionaryNodeOptions BuildOptionsForType(int fieldType)
		{
			switch(fieldType)
			{
				case (int)CellarPropertyType.MultiString:
				case (int)CellarPropertyType.MultiUnicode:
					return new DictionaryNodeWritingSystemOptions();
				case (int)CellarPropertyType.OwningCollection:
				case (int)CellarPropertyType.OwningSequence:
				case (int)CellarPropertyType.ReferenceCollection:
				case (int)CellarPropertyType.ReferenceSequence:
					return new DictionaryNodeListOptions();
			}
			return null;
		}
	}
}
