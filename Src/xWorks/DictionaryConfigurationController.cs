// Copyright (c) 2014-2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.FDO.DomainImpl;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.XWorks.DictionaryDetailsView;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	internal class DictionaryConfigurationController
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

		private FdoCache Cache { get { return (FdoCache)_mediator.PropertyTable.GetValue("cache"); } }

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

		private bool m_isDirty;

		/// <summary>
		/// Flag whether we're highlighting the affected node in the preview area.
		/// </summary>
		private bool _isHighlighted;

		/// <summary>
		/// Whether any changes have been saved, including changes to the Configs, which Config is the current Config, changes to Styles, etc.,
		/// that require the preview to be updated.
		/// </summary>
		public bool MasterRefreshRequired { get; private set; }

		/// <summary>
		/// Figure out what alternate dictionaries are available (eg root-, stem-, ...)
		/// Populate _dictionaryConfigurations with available models.
		/// Populate view's list of alternate dictionaries with available choices.
		/// </summary>
		private void LoadDictionaryConfigurations()
		{
			_dictionaryConfigurations = GetDictionaryConfigurationModels(Cache, _defaultConfigDir, _projectConfigDir);
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
				MergeCustomFieldsIntoDictionaryModel(model, cache);
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
		private void RefreshPreview(bool isChangeInDictionaryModel = true)
		{
			if (isChangeInDictionaryModel)
				m_isDirty = true;
			else
				MasterRefreshRequired = true;
			//_mediator should be null only for unit tests which don't need styles
			if (_mediator == null || _previewEntry == null || !_previewEntry.IsValidObject)
				return;
			View.PreviewData = ConfiguredXHTMLGenerator.GenerateEntryHtmlWithStyles(_previewEntry, _model, _allEntriesPublicationDecorator, _mediator);
			if(_isHighlighted)
				View.HighlightContent(View.TreeControl.Tree.SelectedNode.Tag as ConfigurableDictionaryNode, Cache);
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
				// Configure shared nodes exactly once: under their master parent
				if (!node.IsSubordinateParent && node.ReferencedOrDirectChildren != null)
					CreateTreeOfTreeNodes(node, node.ReferencedOrDirectChildren);
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

			if (_mediator != null && _mediator.StringTbl != null)
				node.StringTable = _mediator.StringTbl;	// for localization
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
			var cache = Cache;
			_allEntriesPublicationDecorator = new DictionaryPublicationDecorator(cache,
				(ISilDataAccessManaged)cache.MainCacheAccessor, cache.ServiceLocator.GetInstance<Virtuals>().LexDbEntries);

			_previewEntry = previewEntry ?? GetDefaultEntryForType(DictionaryConfigurationListener.GetDictionaryConfigurationBaseType(mediator), cache);
			View = view;
			_projectConfigDir = DictionaryConfigurationListener.GetProjectConfigurationDirectory(mediator);
			_defaultConfigDir = DictionaryConfigurationListener.GetDefaultConfigurationDirectory(mediator);
			LoadDictionaryConfigurations();
			LoadLastDictionaryConfiguration();
			PopulateTreeView();
			View.ManageConfigurations += (sender, args) =>
			{
				var currentModel = _model;
				bool managerMadeChanges;
				// show the Configuration Manager dialog
				using (var dialog = new DictionaryConfigurationManagerDlg(_mediator.HelpTopicProvider))
				{
					var configurationManagerController = new DictionaryConfigurationManagerController(dialog, _mediator,
						_dictionaryConfigurations, GetAllPublications(cache), _projectConfigDir, _defaultConfigDir, _model);
					configurationManagerController.Finished += SelectModelFromManager;
					SetManagerTypeInfo(dialog);
					dialog.ShowDialog(View as Form);
					managerMadeChanges = configurationManagerController.IsDirty ||  _model != currentModel;
				}
				// if the manager has not updated anything then we don't need to make any adustments
				if (!managerMadeChanges)
					return;
				// Update our Views
				View.SetChoices(_dictionaryConfigurations);
				MergeCustomFieldsIntoDictionaryModel(_model, cache);
				SaveModel();
				SelectCurrentConfigurationAndRefresh();
			};
			View.SaveModel += SaveModelHandler;
			View.SwitchConfiguration += (sender, args) =>
			{
				if (_model == args.ConfigurationPicked)
					return;
				_model = args.ConfigurationPicked;
				RefreshView(); // isChangeInDictionaryModel: true, because we update the current config in the PropertyTable when we save the model.
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

			View.TreeControl.Highlight += (node, button, tooltip) =>
			{
				_isHighlighted = !_isHighlighted;
				if (_isHighlighted)
				{
					View.HighlightContent(node.Tag as ConfigurableDictionaryNode, cache);
					button.BackColor = Color.White;
					tooltip.SetToolTip(button, xWorksStrings.RemoveHighlighting);
				}
				else
				{
					View.HighlightContent(null, cache);	// turns off current highlighting.
					button.BackColor = Color.Yellow;
					tooltip.SetToolTip(button, xWorksStrings.HighlightAffectedContent);
				}
			};

			View.TreeControl.Tree.AfterCheck += (sender, args) =>
			{
				var node = (ConfigurableDictionaryNode) args.Node.Tag;
				node.IsEnabled = args.Node.Checked;

				// Details may need to be enabled or disabled
				RefreshPreview();
				View.TreeControl.Tree.SelectedNode = FindTreeNode(node, View.TreeControl.Tree.Nodes);
				BuildAndShowOptions(node);
			};

			View.TreeControl.Tree.AfterSelect += (sender, args) =>
			{
				var node = (ConfigurableDictionaryNode) args.Node.Tag;

				View.TreeControl.MoveUpEnabled = CanReorder(node, Direction.Up);
				View.TreeControl.MoveDownEnabled = CanReorder(node, Direction.Down);
				View.TreeControl.DuplicateEnabled = !node.IsReadonlyMainEntry;
				View.TreeControl.RemoveEnabled = node.IsDuplicate;
				View.TreeControl.RenameEnabled = node.IsDuplicate;

				BuildAndShowOptions(node);

				if (_isHighlighted)
				{
					// Highlighting is turned on, change what is highlighted.
					View.HighlightContent(node, cache);
				}
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
			SelectCurrentConfigurationAndRefresh();
			MasterRefreshRequired = m_isDirty = false;
		}

		private void SetManagerTypeInfo(DictionaryConfigurationManagerDlg dialog)
		{
			dialog.HelpTopic = DictionaryConfigurationListener.GetDictionaryConfigurationBaseType(_mediator) == xWorksStrings.Dictionary
						? "khtpDictConfigManager"
						: "khtpRevIndexConfigManager";

			if (DictionaryConfigurationListener.GetDictionaryConfigurationBaseType(_mediator) == xWorksStrings.ReversalIndex)
			{
				dialog.Text = xWorksStrings.ReversalIndexConfigurationDlgTitle;
				dialog.ConfigurationGroupText = xWorksStrings.DictionaryConfigurationMangager_ReversalConfigurations_GroupLabel;
			}
		}

		public void SelectModelFromManager(DictionaryConfigurationModel model)
		{
			_model = model;
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
					var entryRepo = serviceLocator.GetInstance<ILexEntryRepository>().AllInstances().ToList();
					// try to find the first entry with a headword not equal to "???"; otherwise, any entry will have to do.
					return entryRepo.FirstOrDefault(entry => StringServices.DefaultHomographString() != entry.HeadWord.Text)
						?? entryRepo.FirstOrDefault();
					}
				case "Reversal Index":
				{
					var entryRepo = serviceLocator.GetInstance<IReversalIndexEntryRepository>().AllInstances().ToList(); // TODO pH 2015.07: filter by WS
					// try to find the first entry with a headword not equal to "???"; otherwise, any entry will have to do.
					return entryRepo.FirstOrDefault(
									entry => StringServices.DefaultHomographString() != entry.ReversalForm.BestAnalysisAlternative.Text)
						?? entryRepo.FirstOrDefault() ?? serviceLocator.GetInstance<IReversalIndexEntryFactory>().Create();
				}
				default:
				{
					throw new NotImplementedException(string.Format("Default entry for {0} type not implemented.", configurationType));
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
			if (m_isDirty)
				SaveModel();
		}

		internal void SaveModel()
		{
			foreach (var config in _dictionaryConfigurations)
			{
				config.FilePath = GetProjectConfigLocationForPath(config.FilePath);
				config.Save();
			}
			// This property must be set *after* saving, because the initial save changes the FilePath
			DictionaryConfigurationListener.SetCurrentConfiguration(_mediator, _model.FilePath, false);
			MasterRefreshRequired = true;
			m_isDirty = false;
		}

		internal string GetProjectConfigLocationForPath(string filePath)
		{
			var projectConfigDir = FdoFileHelper.GetConfigSettingsDir(Cache.ProjectId.ProjectFolder);
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
		private void BuildAndShowOptions(ConfigurableDictionaryNode node)
		{
			if (DetailsController == null)
			{
				DetailsController = new DictionaryDetailsController(new DetailsView(), _mediator);
				DetailsController.DetailsModelChanged += (sender, e) => RefreshPreview();
				DetailsController.StylesDialogMadeChanges += (sender, e) =>
				{
					EnsureValidStylesInModel(_model, Cache); // in case the change was a rename or deletion
					RefreshPreview(false);
				};
				DetailsController.SelectedNodeChanged += (sender, e) =>
				{
					var nodeToSelect = sender as ConfigurableDictionaryNode;
					if (nodeToSelect != null)
						View.TreeControl.Tree.SelectedNode = FindTreeNode(nodeToSelect, View.TreeControl.Tree.Nodes);
				};
			}
			DetailsController.LoadNode(_model, node);
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
			// Root nodes can't be moved
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

		private void SelectCurrentConfigurationAndRefresh()
		{
			View.SelectConfiguration(_model);
			RefreshView(); // REVIEW pH 2016.02: this is called only in ctor and after ManageViews. do we even want to refresh and set isDirty?
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

		/// <summary>
		/// Link this node to a SharedItem to use its children. Returns true if this node is the first (Master) parent; false otherwise
		/// </summary>
		public static bool LinkReferencedNode(List<ConfigurableDictionaryNode> sharedItems, ConfigurableDictionaryNode node, string referenceItem)
		{
			node.ReferencedNode = sharedItems.FirstOrDefault(
				si => si.Label == referenceItem && si.FieldDescription == node.FieldDescription && si.SubField == node.SubField);
			if (node.ReferencedNode == null)
				throw new KeyNotFoundException(string.Format("Could not find Referenced Node named {0} for field {1}.{2}",
					referenceItem, node.FieldDescription, node.SubField));
			node.ReferenceItem = referenceItem;
			if (node.ReferencedNode.Parent != null)
				return false;
			node.ReferencedNode.Parent = node;
			return true;
		}

		/// <summary>
		/// Allow other nodes to reference this node's children
		/// </summary>
		public static void ShareNodeAsReference(List<ConfigurableDictionaryNode> sharedItems, ConfigurableDictionaryNode node, string cssClass = null)
		{
			if (node.ReferencedNode != null)
				throw new InvalidOperationException(string.Format("Node {0} is already shared as {1}",
					DictionaryConfigurationMigrator.BuildPathStringFromNode(node), node.ReferenceItem ?? node.ReferencedNode.Label));
			if (node.Children == null || !node.Children.Any())
				return; // no point sharing Children there aren't any
			var dupItem = sharedItems.FirstOrDefault(item => item.FieldDescription == node.FieldDescription && item.SubField == node.SubField);
			if (dupItem != null)
			{
				var fullField = string.IsNullOrEmpty(node.SubField)
					? node.FieldDescription
					: string.Format("{0}.{1}", node.FieldDescription, node.SubField);
				MessageBoxUtils.Show(string.Format(xWorksStrings.InadvisableToShare,
					node.DisplayLabel, fullField, DictionaryConfigurationMigrator.BuildPathStringFromNode(dupItem.Parent)));
				return;
			}

			// ENHANCE (Hasso) 2016.03: enforce that the specified node is part of *this* model (incl shared items)
			var key = string.IsNullOrEmpty(node.ReferenceItem) ? string.Format("Shared{0}", node.Label) : node.ReferenceItem;
			cssClass = string.IsNullOrEmpty(cssClass) ? string.Format("shared{0}", CssGenerator.GetClassAttributeForConfig(node)) : cssClass.ToLowerInvariant();
			// Ensure the shared node's Label and CSSClassNameOverride are both unique within this Configuration
			if (sharedItems.Any(item => item.Label == key || item.CSSClassNameOverride == cssClass))
			{
				throw new ArgumentException(string.Format("A SharedItem already exists with the Label '{0}' or the class '{1}'", key, cssClass));
			}
			var sharedItem = new ConfigurableDictionaryNode
			{
				Label = key,
				CSSClassNameOverride = cssClass,
				FieldDescription = node.FieldDescription,
				SubField = node.SubField,
				Parent = node,
				Children = node.Children, // ENHANCE (Hasso) 2016.03: deep-clone so that unshared changes are not lost? Or only on share-with?
				IsEnabled = true // shared items are always enabled (for configurability)
			};
			foreach (var child in sharedItem.Children)
				child.Parent = sharedItem;
			sharedItems.Add(sharedItem);
			node.ReferenceItem = key;
			node.ReferencedNode = sharedItem;
			node.Children = null; // For now, we expect that nodes have ReferencedChildren NAND direct Children.
			// ENHANCE pH 2016.04: if we ever allow nodes to have both Referenced and direct Children, all DC-model-sync code will need to change.
		}

		#region ModelSynchronization
		public static void MergeTypesIntoDictionaryModel(DictionaryConfigurationModel model, FdoCache cache)
		{
			var complexTypes = new Set<Guid>();
			foreach (var pos in cache.LangProject.LexDbOA.ComplexEntryTypesOA.ReallyReallyAllPossibilities)
				complexTypes.Add(pos.Guid);
			complexTypes.Add(XmlViewsUtils.GetGuidForUnspecifiedComplexFormType());
			var variantTypes = new Set<Guid>();
			foreach (var pos in cache.LangProject.LexDbOA.VariantEntryTypesOA.ReallyReallyAllPossibilities)
				variantTypes.Add(pos.Guid);
			variantTypes.Add(XmlViewsUtils.GetGuidForUnspecifiedVariantType());
			var referenceTypes = new Set<Guid>();
			if (cache.LangProject.LexDbOA.ReferencesOA != null)
			{
				foreach (var pos in cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS)
					referenceTypes.Add(pos.Guid);
			}
			foreach (var part in model.PartsAndSharedItems)
			{
				FixTypeListOnNode(part, complexTypes, variantTypes, referenceTypes);
			}
		}

		private static void FixTypeListOnNode(ConfigurableDictionaryNode node, Set<Guid> complexTypes, Set<Guid> variantTypes, Set<Guid> referenceTypes)
		{
			var listOptions = node.DictionaryNodeOptions as DictionaryNodeListOptions;
			if (listOptions != null)
			{
				switch (listOptions.ListId)
				{
					case DictionaryNodeListOptions.ListIds.Complex:
						FixOptionsAccordingToCurrentTypes(listOptions.Options, complexTypes);
						break;
					case DictionaryNodeListOptions.ListIds.Variant:
						FixOptionsAccordingToCurrentTypes(listOptions.Options, variantTypes);
						break;
					case DictionaryNodeListOptions.ListIds.Entry:
						FixOptionsAccordingToCurrentTypes(listOptions.Options, referenceTypes);
						break;
					case DictionaryNodeListOptions.ListIds.Sense:
						FixOptionsAccordingToCurrentTypes(listOptions.Options, referenceTypes);
						break;
					case DictionaryNodeListOptions.ListIds.Minor:
						var complexAndVariant = complexTypes.Union(variantTypes);
						FixOptionsAccordingToCurrentTypes(listOptions.Options, complexAndVariant);
						break;
				}
			}
			//Recurse into child nodes and fix the type lists on them
			if (node.Children != null)
			{
				foreach (var child in node.Children)
					FixTypeListOnNode(child, complexTypes, variantTypes, referenceTypes);
			}
		}

		private static void FixOptionsAccordingToCurrentTypes(List<DictionaryNodeListOptions.DictionaryNodeOption> options, Set<Guid> possibilities)
		{
			var currentGuids = new Set<Guid>();
			foreach (var opt in options)
			{
				Guid guid;
				if (Guid.TryParse(opt.Id, out guid))	// can be empty string
					currentGuids.Add(guid);
			}
			// add types that do not exist already
			options.AddRange(possibilities.Where(type => !currentGuids.Contains(type))
				.Select(type => new DictionaryNodeListOptions.DictionaryNodeOption { Id = type.ToString(), IsEnabled = true }));
			// remove options that no longer exist
			for (var i = options.Count - 1; i >= 0; --i)
			{
				Guid guid;
				if (Guid.TryParse(options[i].Id, out guid) && !possibilities.Contains(guid))
					options.RemoveAt(i);
			}
		}

		public static void EnsureValidStylesInModel(DictionaryConfigurationModel model, FdoCache cache)
		{
			var styles = cache.LangProject.StylesOC.ToDictionary(style => style.Name);
			foreach (var part in model.PartsAndSharedItems)
			{
				if (part.IsMainEntry && string.IsNullOrEmpty(part.Style))
					part.Style = "Dictionary-Normal";
				EnsureValidStylesInConfigNodes(part, styles);
			}
		}

		private static void EnsureValidStylesInConfigNodes(ConfigurableDictionaryNode node, Dictionary<string, IStStyle> styles)
		{
			if (!string.IsNullOrEmpty(node.Style) && !styles.ContainsKey(node.Style))
				node.Style = null;
			if (node.DictionaryNodeOptions != null)
				EnsureValidStylesInNodeOptions(node.DictionaryNodeOptions, styles);
			if (node.Children != null)
			{
				foreach (var child in node.Children)
					EnsureValidStylesInConfigNodes(child, styles);
			}
		}

		private static void EnsureValidStylesInNodeOptions(DictionaryNodeOptions options, Dictionary<string, IStStyle> styles)
		{
			var senseOptions = options as DictionaryNodeSenseOptions;
			if (senseOptions == null)
				return;
			if (!string.IsNullOrEmpty(senseOptions.NumberStyle) && !styles.ContainsKey(senseOptions.NumberStyle))
				senseOptions.NumberStyle = null;
		}

		public static List<string> GetAllPublications(FdoCache cache)
		{
			return cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS.Select(p => p.Name.BestAnalysisAlternative.Text).ToList();
		}

		public static void FilterInvalidPublicationsFromModel(DictionaryConfigurationModel model, FdoCache cache)
		{
			if (model.Publications == null || !model.Publications.Any())
				return;
			var allPossibilities = cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS.ToList();
			var allPossiblePublicationsInAllWs = new HashSet<string>();
			foreach (var possibility in allPossibilities)
				foreach (var ws in cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems.Handles())
					allPossiblePublicationsInAllWs.Add(possibility.Name.get_String(ws).Text);
			model.Publications = model.Publications.Where(allPossiblePublicationsInAllWs.Contains).ToList();
		}

		public static void MergeCustomFieldsIntoDictionaryModel(DictionaryConfigurationModel model, FdoCache cache)
		{
			foreach(var part in model.Parts)
			{
				// Detect a bad configuration file and report it in an intelligable way. We generated bad configs before the migration code was cleaned up
				// This is only expected to happen to our testers, we don't need to recover, just inform the testers.
				if (part.FieldDescription == null)
				{
					throw new ApplicationException(string.Format("{0} is corrupt. {1} has no FieldDescription. Deleting this configuration file may fix the problem.",
						model.FilePath, part.Label));
				}
				var customFields = GetCustomFieldsForType(cache, part.FieldDescription);
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
			// Custom fields don't need to have their children merged; skip those
			foreach(var configNode in configurationList.Where(node => !node.IsCustomField))
			{
				var lookupClass = GetLookupClassForCustomFieldParent(configNode, cache);
				if(lookupClass != null)
				{
					var fieldsForType = GetCustomFieldsForType(cache, lookupClass, classToCustomFields);
					MergeCustomFieldLists(configNode, fieldsForType);
				}
				// recurse into the rest of the dictionary model
				MergeCustomFieldsIntoDictionaryModel(cache, configNode.Children);
			}
		}

		public static string GetLookupClassForCustomFieldParent(ConfigurableDictionaryNode parent, FdoCache cache)
		{
			Type unneeded;
			// The class that contains the type information for the field we are inspecting
			var lookupClass = ConfiguredXHTMLGenerator.GetTypeForConfigurationNode(parent, cache, out unneeded);
			// If the node describes a collection we may want to add the custom field node if the collection is of
			// the type that the field is added to. (e.g. Senses, ExampleSentences)
			if(ConfiguredXHTMLGenerator.GetPropertyTypeForConfigurationNode(parent, cache) ==
				ConfiguredXHTMLGenerator.PropertyType.CollectionType)
			{
				if(lookupClass.IsGenericType)
				{
					lookupClass = lookupClass.GetGenericArguments()[0];
				}
			}
			return lookupClass == null ? null : lookupClass.Name;
		}

		/// <summary>
		/// This method will generate a mapping between the class name (and interface name)
		/// and each custom field in the model associated with that class.
		/// </summary>
		public static Dictionary<string, List<int>> BuildCustomFieldMap(FdoCache cache)
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
			if (parent.IsSubordinateParent)
				return; // If parent has Referenced Children but is not the Master Parent, return; fields will be merged under the Master Parent
			parent = parent.ReferencedNode ?? parent;
			// Set the parent on the customFieldNodes (needed for Contains and to make any new fields valid when added)
			foreach(var customField in customFieldNodes)
			{
				customField.Parent = parent;
			}
			if (parent.Children == null)
				parent.Children = new List<ConfigurableDictionaryNode>();
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
		/// <param name="cache"></param>
		/// <param name="className"></param>
		/// <param name="customFieldMap">existing custom field map for performance, method will build one if none given</param>
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
				var configNode = new ConfigurableDictionaryNode
				{
					Label = metaDataCache.GetFieldLabel(field),
					IsCustomField = true,
					IsEnabled = false,
					FieldDescription = metaDataCache.GetFieldName(field),
					DictionaryNodeOptions = BuildOptionsForType(metaDataCache.GetFieldType(field))
				};
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
		/// <remarks>
		/// We need this for migrating configurations of custom fields as well as for creating a configuration
		/// from scratch for a new custom field.
		/// </remarks>
		internal static void AddFieldsForPossibilityList(ConfigurableDictionaryNode configNode)
		{
			configNode.Children = new List<ConfigurableDictionaryNode>
			{
				new ConfigurableDictionaryNode
				{
					Label = "Name",
					FieldDescription = "Name",
					DictionaryNodeOptions = new DictionaryNodeWritingSystemOptions { WsType = DictionaryNodeWritingSystemOptions.WritingSystemType.Analysis },
					Parent = configNode,
					IsCustomField = false // the parent node may be for a custom field, but this node is for a standard CmPossibility field
				},
				new ConfigurableDictionaryNode
				{
					Label = "Abbreviation",
					FieldDescription = "Abbreviation",
					DictionaryNodeOptions = new DictionaryNodeWritingSystemOptions { WsType = DictionaryNodeWritingSystemOptions.WritingSystemType.Analysis },
					Parent = configNode,
					IsCustomField = false // the parent node may be for a custom field, but this node is for a standard CmPossibility field
				}
			};
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
		#endregion ModelSynchronization

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

		/// <summary>
		/// Search the TreeNode tree to find a starting node based on matching the "class"
		/// attributes of the generated XHTML tracing back from the XHTML element clicked.
		/// If no match is found, SelectedNode is not set.  Otherwise, the best match found
		/// is used to set SelectedNode.
		/// </summary>
		internal void SetStartingNode(List<string> classList)
		{
			if (classList == null || classList.Count == 0)
				return;
			if (View != null &&
				View.TreeControl != null &&
				View.TreeControl.Tree != null)
			{
				ConfigurableDictionaryNode topNode = null;
				// Search through the configuration trees associated with each toplevel TreeNode to find
				// the best match.  If no match is found, give up.
				foreach (TreeNode node in View.TreeControl.Tree.Nodes)
				{
					var configNode = node.Tag as ConfigurableDictionaryNode;
					if (configNode == null)
						continue;
					var cssClass = CssGenerator.GetClassAttributeForConfig(configNode);
					if (cssClass == classList[0])
					{
						topNode = configNode;
						break;
					}
				}
				if (topNode == null)
					return;
				// We have a match, so search through the TreeNode tree to find the TreeNode tagged
				// with the given configuration node.  If found, set that as the SelectedNode.
				classList.RemoveAt(0);
				var startingConfigNode = FindStartingConfigNode(topNode, classList);
				foreach (TreeNode node in View.TreeControl.Tree.Nodes)
				{
					var startingTreeNode = FindMatchingTreeNode(node, startingConfigNode);
					if (startingTreeNode != null)
					{
						View.TreeControl.Tree.SelectedNode = startingTreeNode;
						break;
					}
				}
			}
		}

		/// <summary>
		/// Recursively descend the configuration tree, progressively matching nodes against the list of classes.  Stop
		/// when we run out of both tree and classes.  Classes can be skipped if not matched.  Running out of tree nodes
		/// before running out of classes causes one level of backtracking up the configuration tree to look for a better
		/// match.
		/// </summary>
		/// <remarks>LT-17213 Now 'internal static' so DictionaryConfigurationDlg can use it.</remarks>
		internal static ConfigurableDictionaryNode FindStartingConfigNode(ConfigurableDictionaryNode topNode, List<string> classList)
		{
			if (classList.Count == 0)
			{
				return topNode.IsSharedItem ? topNode.Parent : topNode; // what we have already is the best we can find.
			}
			// If we have a referenced node, we prefer to use it over any Children we might have
			if (topNode.ReferencedNode != null)
				topNode = topNode.ReferencedNode;

			// If we can't go further down the configuration tree, but still have classes to match, back up one level
			// and try matching with the remaining classes.  The configuration tree doesn't always map exactly with
			// the XHTML tree structure.  For instance, in the XHTML, Examples contains instances of Example, each
			// of which contains an instance of Translations, which contains instances of Translation.  In the configuration
			// tree, Examples contains Example and Translations at the same level.
			if (topNode.Children == null || topNode.Children.Count == 0)
			{
				var match = FindStartingConfigNode(topNode.Parent, classList);
				if (match != topNode.Parent)
					return match;	// we found something better!
				return topNode;		// this is the best we can find.
			}
			ConfigurableDictionaryNode matchingNode = null;
			foreach (ConfigurableDictionaryNode node in topNode.Children)
			{
				var cssClass = CssGenerator.GetClassAttributeForConfig(node);
				// LT-17359 a reference node might have "senses mainentrysubsenses"
				if (cssClass == classList[0].Split(' ')[0])
				{
					matchingNode = node;
					break;
				}
			}
			// If we didn't match, skip this class in the list and try the next class, looking at the same configuration
			// node.  There are classes in the XHTML that aren't represented in the configuration nodes.  ("sensecontent"
			// and "sense" among others)
			if (matchingNode == null)
				matchingNode = topNode;
			classList.RemoveAt(0);
			return FindStartingConfigNode(matchingNode, classList);
		}

		/// <summary>
		/// Find the TreeNode that has the given configuration node as its Tag value.  (If there were a
		/// bidirectional link between the two, this method would be unnecessary...)
		/// </summary>
		private TreeNode FindMatchingTreeNode(TreeNode node, ConfigurableDictionaryNode configNode)
		{
			if (node.Tag as ConfigurableDictionaryNode == configNode)
				return node;
			foreach (TreeNode child in node.Nodes)
			{
				var start = FindMatchingTreeNode(child, configNode);
				if (start != null)
					return start;
			}
			return null;
		}
	}
}
