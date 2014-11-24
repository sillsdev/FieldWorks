// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.FDO.DomainImpl;
using SIL.FieldWorks.FDO.DomainServices;
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
		/// The entry being used for preview purposes
		/// </summary>
		internal ICmObject _previewEntry;

		/// <summary>
		/// Mediator to use
		/// </summary>
		internal Mediator _mediator;

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
		internal List<DictionaryConfigurationModel> _dictionaryConfigurations;

		/// <summary>
		/// Directory where configurations of the current type (Dictionary, Reversal, ...) are stored
		/// for the project.
		/// </summary>
		private string _projectConfigDir;

		/// <summary>
		/// Publication decorator necessary to view sense numbers in the preview
		/// </summary>
		private DictionaryPublicationDecorator _allEntriesPublicationDecorator;

		/// <summary>
		/// Directory where shipped default configurations of the current type (Dictionary, Reversal, ...)
		/// are stored.
		/// </summary>
		internal string _defaultConfigDir;

		/// <summary>
		/// Figure out what alternate dictionaries are available (eg root-, stem-, ...)
		/// Populate _dictionaryConfigurations with available models.
		/// Populate view's list of alternate dictionaries with available choices.
		/// </summary>
		private void LoadDictionaryConfigurations()
		{
			var cache = (FdoCache) _mediator.PropertyTable.GetValue("cache");
			_dictionaryConfigurations = GetDictionaryConfigurationModels(cache, _defaultConfigDir, _projectConfigDir);
			View.SetChoices(_dictionaryConfigurations);
		}

		/// <summary>
		/// Loads a List of configuration choices from default and projcet folders.
		/// Project-specific configurations override default configurations of the same (file)name.
		/// </summary>
		/// <param name="defaultPath"></param>
		/// <param name="projectPath"></param>
		/// <seealso cref="XhtmlDocView.GatherBuiltInAndUserConfigurations()"/>
		/// <returns>List of paths to configurations</returns>
		internal static List<string> ListDictionaryConfigurationChoices(string defaultPath, string projectPath)
		{
			var choices = new Dictionary<string, string>();
			foreach(var file in Directory.EnumerateFiles(defaultPath, "*" + DictionaryConfigurationModel.FileExtension))
			{
				choices[Path.GetFileNameWithoutExtension(file)] = file;
			}
			if(!Directory.Exists(projectPath))
			{
				Directory.CreateDirectory(projectPath);
			}
			else
			{
				foreach(var choice in Directory.EnumerateFiles(projectPath, "*" + DictionaryConfigurationModel.FileExtension))
				{
					choices[Path.GetFileNameWithoutExtension(choice)] = choice;
				}
			}
			return choices.Values.ToList();
		}

		/// <summary>
		/// Return dictionary configurations from default and project-specific paths, skipping default/shipped configurations that are
		/// superceded by project-specific configurations. Keys are labels, values are the models.
		/// </summary>
		public static Dictionary<string, DictionaryConfigurationModel> GetDictionaryConfigurationLabels(FdoCache cache, string defaultPath, string projectPath)
		{
			var configurationModels = GetDictionaryConfigurationModels(cache, defaultPath, projectPath);
			var labelToFileDictionary = new Dictionary<string, DictionaryConfigurationModel>();
			foreach(var model in configurationModels)
			{
				labelToFileDictionary[model.Label] = model;
			}
			return labelToFileDictionary;
		}

		private static List<DictionaryConfigurationModel> GetDictionaryConfigurationModels(FdoCache cache, string defaultPath, string projectPath)
		{
			var configurationPaths = ListDictionaryConfigurationChoices(defaultPath, projectPath);
			var configurationModels = new List<DictionaryConfigurationModel>();
			foreach(var path in configurationPaths)
			{
				var model = new DictionaryConfigurationModel(path, cache);
				MergeCustomFieldsIntoDictionaryModel(cache, model);
				configurationModels.Add(model);
			}
			configurationModels.Sort((lhs, rhs) => string.Compare(lhs.Label, rhs.Label));
			return configurationModels;
		}

		/// <summary>
		/// Populate dictionary elements tree, from model.
		/// </summary>
		internal void PopulateTreeView()
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
			{
				//If an expanded node is removed it is added to the expanedNodes list before
				//the tree is rebuilt. Therefore when tree is rebuilt FindTreeNode returns null since
				//it cannot find that node anymore.
				var node = FindTreeNode(expandedNode, tree.Nodes);
				if (node != null)
					node.Expand();
			}

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
			RefreshPreview();
			DisplayPublicationTypes();
		}

		/// <summary>Refresh the Preview without reloading the entire configuration tree</summary>
		private void RefreshPreview()
		{
			//_mediator should be null only for unit tests which don't need styles
			if(_mediator != null && _previewEntry != null)
			{
				View.PreviewData = ConfiguredXHTMLGenerator.GenerateEntryHtmlWithStyles(_previewEntry, _model, _allEntriesPublicationDecorator, _mediator);
			}
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
		/// <param name="previewEntry"></param>
		public DictionaryConfigurationController(IDictionaryConfigurationView view, Mediator mediator, ICmObject previewEntry)
		{
			_mediator = mediator;
			var cache = (FdoCache)mediator.PropertyTable.GetValue("cache");
			_allEntriesPublicationDecorator = new DictionaryPublicationDecorator(cache,
																										(ISilDataAccessManaged)cache.MainCacheAccessor,
																										cache.ServiceLocator.GetInstance<Virtuals>().LexDbEntries);

			_previewEntry = previewEntry ?? GetDefaultEntryForType(DictionaryConfigurationListener.GetDictionaryConfigurationType(mediator), cache);
			View = view;
			_projectConfigDir = DictionaryConfigurationListener.GetProjectConfigurationDirectory(mediator);
			_defaultConfigDir = DictionaryConfigurationListener.GetDefaultConfigurationDirectory(mediator);
			LoadDictionaryConfigurations();
			LoadLastDictionaryConfiguration();
			PopulateTreeView();
			View.ManageConfigurations += (sender, args) =>
			{
				// show the Configuration Manager dialog
				using (var dialog = new DictionaryConfigurationManagerDlg())
				{
					var configurationManagerController = new DictionaryConfigurationManagerController(dialog, _mediator,
						_dictionaryConfigurations, _model.GetAllPublications(cache), _projectConfigDir, _defaultConfigDir, _model);
					configurationManagerController.Finished += SelectModelFromManager;
					dialog.ShowDialog(View as Form);
				}

				// Update our Views
				View.SetChoices(_dictionaryConfigurations);
				MergeCustomFieldsIntoDictionaryModel(cache, _model);
				RefreshView();
				SelectCurrentConfiguration();
			};
			View.SaveModel += SaveModelHandler;
			View.SwitchConfiguration += (sender, args) =>
			{
				_model = args.ConfigurationPicked;
				RefreshView();
			};

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
						MessageBox.Show(xWorksStrings.FailedToRename);
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

				// Details may need to be enabled or disabled
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
			View.TreeControl.CheckAll += treeNode =>
			{
				EnableNodeAndDescendants(treeNode.Tag as ConfigurableDictionaryNode);
				RefreshView();
			};
			View.TreeControl.UnCheckAll += treeNode =>
			{
				DisableNodeAndDescendants(treeNode.Tag as ConfigurableDictionaryNode);
				RefreshView();
			};
			SelectCurrentConfiguration();
		}

		public void SelectModelFromManager(DictionaryConfigurationModel model)
		{
			_model = model;
			SelectCurrentConfiguration();
		}

		/// <summary>
		/// Returns a default entry for the given configuration type or null if the cache has no items for that type.
		/// </summary>
		internal static ICmObject GetDefaultEntryForType(string configurationType, FdoCache cache)
		{
			var serviceLocator = cache.ServiceLocator;
			switch(configurationType)
			{
				case "Dictionary":
				{
					var entryRepo = serviceLocator.GetInstance<ILexEntryRepository>().AllInstances();
					// try to find the first entry with a headword not equal to "???"
					var entryWithHeadword = entryRepo.FirstOrDefault(entry => StringServices.DefaultHomographString() != entry.HeadWord.Text);
					if(entryWithHeadword == null)
					{
						entryWithHeadword = entryRepo.FirstOrDefault();
					}
					return entryWithHeadword;
				}
				default :
				{
					throw new NotImplementedException(String.Format("Default entry for {0} type not implemented.", configurationType));
				}
			}
		}

		private void LoadLastDictionaryConfiguration()
		{
			var lastUsedConfiguration = DictionaryConfigurationListener.GetCurrentConfiguration(_mediator);
			_model = _dictionaryConfigurations.FirstOrDefault(config => config.FilePath == lastUsedConfiguration)
				?? _dictionaryConfigurations.First();
		}

		private void SaveModelHandler(object sender, EventArgs e)
		{
			SaveModel();
			RefreshView(); // REVIEW (Hasso) 2014.11: refreshing here is beneficial only if some configuration change fails to refresh the preview
		}

		internal void SaveModel()
		{
			foreach (var config in _dictionaryConfigurations)
			{
				config.FilePath = GetProjectConfigLocationForPath(config.FilePath, _mediator);
				config.Save();
			}
			// This property must be set *after* saving, because the initial save changes the FilePath
			_mediator.PropertyTable.SetProperty("DictionaryPublicationLayout", _model.FilePath, true);
		}

		internal string GetProjectConfigLocationForPath(string filePath, Mediator mediator)
		{
			var cache = (FdoCache)mediator.PropertyTable.GetValue("cache");
			var projectConfigDir = FdoFileHelper.GetConfigSettingsDir(cache.ProjectId.ProjectFolder);
			if(filePath.StartsWith(projectConfigDir))
			{
				return filePath;
			}
			var detailedConfig = filePath.Substring(FwDirectoryFinder.DefaultConfigurations.Length + 1);
			return Path.Combine(projectConfigDir, detailedConfig);
		}

		/// <summary>
		/// Populate options pane, from model.
		/// </summary>
		private void BuildAndShowOptions(ConfigurableDictionaryNode node, Mediator mediator)
		{
			if (DetailsController == null)
			{
				DetailsController = new DictionaryDetailsController(node, mediator);
				DetailsController.DetailsModelChanged += (sender, e) => RefreshPreview();
			}
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
		/// Display the list of publications configured by the current dictionary configuration.
		/// </summary>
		private void DisplayPublicationTypes()
		{
			View.ShowPublicationsForConfiguration(AffectedPublications);
		}

		/// <summary>
		/// Friendly display string listing the publications affected by making changes to the current dictionary configuration.
		/// </summary>
		public string AffectedPublications
		{
			get
			{
				if (_model.AllPublications)
					return xWorksStrings.Allpublications;

				var strbldr = new StringBuilder();

				if (_model.Publications == null || !_model.Publications.Any())
					return xWorksStrings.ksNone1;
				foreach (var pubType in _model.Publications)
				{
					strbldr.AppendFormat("{0}, ", pubType);
				}
				var str = strbldr.ToString();
				return str.Substring(0, str.Length - 2);
			}
		}

		private void SelectCurrentConfiguration()
		{
			View.SelectConfiguration(_model);
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
				MergeCustomFieldsIntoDictionaryModel(cache, part.Children);
			}
		}

		/// <summary>
		/// This helper method is used to recurse into all of the configuration nodes in a DictionaryModel and merge the custom fields
		/// in each ConfigurableDictionaryNode with those defined in the FieldWorks model according to the metadata cache.
		/// </summary>
		private static void MergeCustomFieldsIntoDictionaryModel(FdoCache cache, IEnumerable<ConfigurableDictionaryNode> configurationList)
		{
			if(configurationList == null)
				return;
			// Gather up the custom fields and map them by type name
			var classToCustomFields = BuildCustomFieldMap(cache);
			foreach(var configNode in configurationList)
			{
				Type unneeded;
				// The class that contains the type information for the field we are inspecting
				var lookupClass = ConfiguredXHTMLGenerator.GetTypeForConfigurationNode(configNode, out unneeded);
				// If the node describes a collection we may want to add the custom field node if the collection is of
				// the type that the field is added to. (e.g. Senses, ExampleSentences)
				if(ConfiguredXHTMLGenerator.GetPropertyTypeForConfigurationNode(configNode) ==
					ConfiguredXHTMLGenerator.PropertyType.CollectionType)
				{
					if(lookupClass.IsGenericType)
					{
						lookupClass = lookupClass.GetGenericArguments()[0];
					}
				}
				if(lookupClass != null)
				{
					var fieldsForType = GetCustomFieldsForType(cache, lookupClass.Name, classToCustomFields);
					if(configNode.Children == null)
					{
						configNode.Children = new List<ConfigurableDictionaryNode>();
					}
					MergeCustomFieldLists(configNode, fieldsForType);
				}
				// recurse into the rest of the dictionary model
				MergeCustomFieldsIntoDictionaryModel(cache, configNode.Children);
			}
		}

		/// <summary>
		/// This method will generate a mapping between the class name (and interface name)
		/// and each custom field in the model associated with that class.
		/// </summary>
		private static Dictionary<string, List<int>> BuildCustomFieldMap(FdoCache cache)
		{
			var metaDataCache = (IFwMetaDataCacheManaged)cache.MetaDataCacheAccessor;
			var classToCustomFields = new Dictionary<string, List<int>>();
			var fieldIds = metaDataCache.GetFieldIds();
			var customFieldIds = fieldIds.Where(metaDataCache.IsCustom);
			foreach(var customFieldId in customFieldIds)
			{
				var cfOwnerClassName = metaDataCache.GetOwnClsName(customFieldId);
				// Also generate a mapping for the corresponding FDO interface (metadata does not contain this)
				var cfOwnerInterfaceName = cfOwnerClassName.Insert(0, "I");
				// Map the class name and then the interface name to the custom field id list
				if(classToCustomFields.ContainsKey(cfOwnerClassName))
					classToCustomFields[cfOwnerClassName].Add(customFieldId);
				else
					classToCustomFields[cfOwnerClassName] = new List<int> { customFieldId };

				if(classToCustomFields.ContainsKey(cfOwnerInterfaceName))
					classToCustomFields[cfOwnerInterfaceName].Add(customFieldId);
				else
					classToCustomFields[cfOwnerInterfaceName] = new List<int> { customFieldId };
			}
			return classToCustomFields;
		}

		private static void MergeCustomFieldLists(ConfigurableDictionaryNode parent, List<ConfigurableDictionaryNode> customFieldNodes)
		{
			// Set the parent on the customFieldNodes (needed for Contains and to make any new fields valid when added)
			foreach(var customField in customFieldNodes)
			{
				customField.Parent = parent;
			}
			var children = parent.Children;
			// Traverse through the children from end to beginning removing any custom fields that no longer exist.
			for(var i = children.Count - 1; i >= 0; --i)
			{
				var configNode = children[i];
				if(!configNode.IsCustomField)
					continue;
				if(!customFieldNodes.Contains(configNode))
				{
					children.Remove(configNode); // field no longer exists
				}
				else
				{
					customFieldNodes.Remove(configNode); // field found
				}
			}
			// Then add any custom fields that don't yet exist in the children configurationList to the end.
			children.AddRange(customFieldNodes);
		}

		/// <summary>
		/// Generate a list of ConfigurableDictionaryNode objects to represent each custom field of the given type.
		/// </summary>
		/// <param name="customFieldMap">existing custom field map for performance, method will build one if none given</param>
		/// <returns></returns>
		public static List<ConfigurableDictionaryNode> GetCustomFieldsForType(FdoCache cache, string className,
			Dictionary<string, List<int>> customFieldMap = null)
		{
			customFieldMap = customFieldMap ?? BuildCustomFieldMap(cache);
			var customFieldIds = customFieldMap.ContainsKey(className)
												 ? customFieldMap[className]
												 : new List<int>();

			var customFieldList = new List<ConfigurableDictionaryNode>();
			var metaDataCache = (IFwMetaDataCacheManaged)cache.MetaDataCacheAccessor;
			foreach(var field in customFieldIds)
			{
					var configNode = new ConfigurableDictionaryNode();
					configNode.Label = metaDataCache.GetFieldLabel(field) ?? metaDataCache.GetFieldName(field);
					configNode.IsCustomField = true;
					configNode.IsEnabled = false;
					configNode.FieldDescription = metaDataCache.GetFieldName(field);
					configNode.DictionaryNodeOptions = BuildOptionsForType(metaDataCache.GetFieldType(field));
					var listId = metaDataCache.GetFieldListRoot(field);
					if(listId != Guid.Empty)
					{
						AddFieldsForPossibilityList(configNode);
					}
					customFieldList.Add(configNode);
			}
			return customFieldList;
		}

		/// <summary>
		/// Add configuration nodes for all properties that we want to enable a user to display for a custom
		/// PossibilityList field. (Currently Name and Abbreviation)
		/// </summary>
		private static void AddFieldsForPossibilityList(ConfigurableDictionaryNode configNode)
		{
			configNode.Children = new List<ConfigurableDictionaryNode>();
			configNode.Children.Add(new ConfigurableDictionaryNode
				{
					Label = "Name",
					FieldDescription = "Name",
					DictionaryNodeOptions = new DictionaryNodeWritingSystemOptions { WsType = DictionaryNodeWritingSystemOptions.WritingSystemType.Analysis },
					Parent = configNode,
					IsCustomField = true
				});
			configNode.Children.Add(new ConfigurableDictionaryNode
				{
					Label = "Abbreviation",
					FieldDescription = "Abbreviation",
					DictionaryNodeOptions = new DictionaryNodeWritingSystemOptions { WsType = DictionaryNodeWritingSystemOptions.WritingSystemType.Analysis },
					Parent = configNode,
					IsCustomField = true
				});
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
		public static string GetClassNameForTargetType(int fieldType,
																		string lookupClass,
																		string fieldName,
																		IFwMetaDataCache metaDataCache)
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

		public static void EnableNodeAndDescendants(ConfigurableDictionaryNode node)
		{
			SetIsEnabledForSubTree(node, true);
		}

		public static void DisableNodeAndDescendants(ConfigurableDictionaryNode node)
		{
			SetIsEnabledForSubTree(node, false);
		}

		private static void SetIsEnabledForSubTree(ConfigurableDictionaryNode node, bool isEnabled)
		{
			if(node == null)
				return;
			node.IsEnabled = isEnabled;
			if(node.Children != null)
			{
				foreach(var child in node.Children)
				{
					SetIsEnabledForSubTree(child, isEnabled);
				}
			}
		}
	}
}
