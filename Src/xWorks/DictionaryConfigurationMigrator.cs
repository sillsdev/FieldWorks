// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using SIL.FieldWorks.FDO;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// This class is used to migrate dictionary configurations from the old layout and parts to the new <code>DictionaryConfigurationModel</code> xml.
	/// </summary>
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification="Cache is a reference")]
	public class DictionaryConfigurationMigrator : ILayoutConverter
	{
		private readonly Inventory m_layoutInventory;
		private readonly Inventory m_partInventory;
		private Mediator m_mediator;

		public DictionaryConfigurationMigrator(Mediator mediator)
		{
			m_mediator = mediator;
			Cache = (FdoCache)mediator.PropertyTable.GetValue("cache");
			StringTable = mediator.StringTbl;
			LayoutLevels = new LayoutLevels();
			m_layoutInventory = Inventory.GetInventory("layouts", Cache.ProjectId.Name);
			m_partInventory = Inventory.GetInventory("parts", Cache.ProjectId.Name);
		}

		public void AddDictionaryTypeItem(XmlNode layoutNode, List<XmlDocConfigureDlg.LayoutTreeNode> oldNodes)
		{
			// layoutNode is expected to look similar to:
			//<layoutType label="Stem-based (complex forms as main entries)" layout="publishStem">
			//<configure class="LexEntry" label="Main Entry" layout="publishStemEntry" />
			//<configure class="LexEntry" label="Minor Entry" layout="publishStemMinorEntry" hideConfig="true" />
			//</layoutType>
			var label = XmlUtils.GetManditoryAttributeValue(layoutNode, "label");
			var layout = XmlUtils.GetManditoryAttributeValue(layoutNode, "layout");
			var configNodeList = new List<ConfigurableDictionaryNode>();
			foreach(var node in oldNodes)
			{
				configNodeList.Add(ConvertLayoutTreeNodeToConfigNode(node));
			}
			var model = new DictionaryConfigurationModel();
			model.Parts = configNodeList;
			model.Label = label;
			model.Version = -1;
			MergeConfigWithNewDefaults(layout, model);
			model.Save();
		}

		public void MigrateOldConfigurationsIfNeeeded()
		{
			if(!ProjectHasNewDictionaryConfigs())
			{
				var configureLayouts = GetConfigureLayoutsNodeForTool("lexiconDictionary");

				LegacyConfigurationUtils.BuildTreeFromLayoutAndParts(configureLayouts, this);
			}
			if(!ProjectHasNewReversalConfigs())
			{
				var configureLayouts = GetConfigureLayoutsNodeForTool("reversalToolEditComplete");

				LegacyConfigurationUtils.BuildTreeFromLayoutAndParts(configureLayouts, this);
			}
		}

		private void MergeConfigWithNewDefaults(string layout, DictionaryConfigurationModel model)
		{
			DictionaryConfigurationModel baseModel;
			if(model.Version == -1)
			{
				const string extension = DictionaryConfigurationModel.FileExtension;
				var defaultPath = DictionaryConfigurationListener.GetDefaultConfigurationDirectory(m_mediator);
				const string defaultStemName = "Stem" + extension;
				const string defaultRootName = "Root" + extension;
//				var projectConfigBasePath = FdoFileHelper.GetConfigSettingsDir(Cache.ProjectId.ProjectFolder);
// todo: Use the real path when the migration is more functional.
				var projectConfigBasePath = Path.Combine(Path.GetTempPath(), "ConfigMigrationTesting");
				switch(layout)
				{
					case "publishStem":
					{
						model.FilePath = Path.Combine(projectConfigBasePath, "Dictionary", defaultStemName);
						baseModel = new DictionaryConfigurationModel(Path.Combine(defaultPath, defaultStemName), Cache);
						break;
					}
					case "publishRoot":
					{
						model.FilePath = Path.Combine(projectConfigBasePath, "Dictionary", defaultRootName);
						baseModel = new DictionaryConfigurationModel(Path.Combine(defaultPath, defaultRootName), Cache);
						break;
					}
					case "publishReversal" :
					{
						throw new NotImplementedException("Reversal index migration has not yet been implemented.");
					}
					default :
					{
						var customSuffixIndex = layout.IndexOf('#');
						if(customSuffixIndex > 0 && layout.StartsWith("publishStem"))
						{
							var customFileName = String.Format("{0}-Stem-{1}{2}", model.Label, layout.Substring(customSuffixIndex), extension);
							model.FilePath = Path.Combine(projectConfigBasePath, "Dictionary", customFileName);
							baseModel = new DictionaryConfigurationModel(Path.Combine(defaultPath, defaultStemName), Cache);
						}
						else if(customSuffixIndex > 0 && layout.StartsWith("publishRoot"))
						{
							var customFileName = String.Format("{0}-Root-{1}{2}", model.Label, layout.Substring(customSuffixIndex), extension);
							model.FilePath = Path.Combine(projectConfigBasePath, "Dictionary", customFileName);
							baseModel = new DictionaryConfigurationModel(Path.Combine(defaultPath, defaultRootName), Cache);
						}
						else // probably a reversal index for a specific language
						{
							throw new NotImplementedException("Custom reversal index migration has not yet been implemented.");
						}
						break;
					}
				}
				Directory.CreateDirectory(Path.GetDirectoryName(model.FilePath));
			}
		}

		/// <summary>
		/// Loads the xml configuration for the given tool and returns its configureLayouts child.
		/// </summary>
		private XmlNode GetConfigureLayoutsNodeForTool(string tool)
		{
			var collector = new XmlNode[1];
			var parameter = new Tuple<string, string, XmlNode[]>("lexicon", tool,
																				  collector);
			m_mediator.SendMessage("GetContentControlParameters", parameter);
			var controlNode = collector[0];
			var dynLoaderNode = controlNode.SelectSingleNode("dynamicloaderinfo");
			var contentAssemblyPath = XmlUtils.GetAttributeValue(dynLoaderNode, "assemblyPath");
			var contentClass = XmlUtils.GetAttributeValue(dynLoaderNode, "class");
			var mainControl = (Control)DynamicLoader.CreateObject(contentAssemblyPath, contentClass);
			var parameters = controlNode.SelectSingleNode("parameters");
			var configureLayouts = XmlUtils.FindNode(parameters, "configureLayouts");
			return configureLayouts;
		}

		private bool ProjectHasNewDictionaryConfigs()
		{
			var newDictionaryConfigLoc = Path.Combine(FdoFileHelper.GetConfigSettingsDir(Path.GetDirectoryName(Cache.ProjectId.Path)), "Dictionary");
			return Directory.Exists(newDictionaryConfigLoc);
		}

		private bool ProjectHasNewReversalConfigs()
		{
			//todo: Implement reversal configuration migration.
			return false;
			//var newReversalConfigLoc = Path.Combine(FdoFileHelper.GetConfigSettingsDir(Path.GetDirectoryName(Cache.ProjectId.Path)), "Reversals");
			//return !Directory.Exists(newReversalConfigLoc);
		}

		internal ConfigurableDictionaryNode ConvertLayoutTreeNodeToConfigNode(XmlDocConfigureDlg.LayoutTreeNode node)
		{
			var convertedNode =  new ConfigurableDictionaryNode
			{
				IsDuplicate = node.IsDuplicate,
				LabelSuffix = node.DupString,
				Before = node.Before,
				After = node.After,
				Between = node.Between,
				Style = node.StyleName,
				Label = node.Label,
				IsEnabled = node.Checked,
				DictionaryNodeOptions = CreateOptionsFromLayoutTreeNode(node)
			};
			if(node.Nodes.Count > 0)
			{
				convertedNode.Children = new List<ConfigurableDictionaryNode>();
				foreach(XmlDocConfigureDlg.LayoutTreeNode childNode in node.Nodes)
				{
					convertedNode.Children.Add(ConvertLayoutTreeNodeToConfigNode(childNode));
				}
			}
			return convertedNode;
		}

		private DictionaryNodeOptions CreateOptionsFromLayoutTreeNode(XmlDocConfigureDlg.LayoutTreeNode node)
		{
			DictionaryNodeOptions options = null;
			if(!String.IsNullOrEmpty(node.WsType))
			{
				options = new DictionaryNodeWritingSystemOptions
				{
					DisplayWritingSystemAbbreviations = node.ShowWsLabels,
					WsType = MigrateWsType(node.WsType),
					Options = MigrateWsOptions(node.WsLabel)
				};
			}
			//todo: Handle list options, sense options, any other options
			return options;
		}

		private List<DictionaryNodeListOptions.DictionaryNodeOption> MigrateWsOptions(string wsLabel)
		{
			return wsLabel.Split(',').Select(item => new DictionaryNodeListOptions.DictionaryNodeOption { Id = item.Trim(), IsEnabled = true }).ToList();
		}

		private DictionaryNodeWritingSystemOptions.WritingSystemType MigrateWsType(string wsType)
		{
			switch(wsType)
			{
				case "analysis": return DictionaryNodeWritingSystemOptions.WritingSystemType.Analysis;
				case "vernacular": return DictionaryNodeWritingSystemOptions.WritingSystemType.Vernacular;
				case "vernacular analysis":
				case "analysis vernacular":
				case "vernoranal" : return DictionaryNodeWritingSystemOptions.WritingSystemType.Both;
				case "pronunciation" : return DictionaryNodeWritingSystemOptions.WritingSystemType.Pronunciation;
				default : throw new ArgumentException(String.Format("Unknown writing system type {0}", wsType), wsType);
			}
		}

		#region trivial portions of the ILayoutConverter implementation
		public IEnumerable<XmlNode> GetLayoutTypes()
		{
			return m_layoutInventory.GetLayoutTypes();
		}

		public FdoCache Cache { get; private set; }
		public StringTable StringTable { get; private set; }
		public LayoutLevels LayoutLevels { get; private set; }

		public void ExpandWsTaggedNodes(string sWsTag)
		{
			m_layoutInventory.ExpandWsTaggedNodes(sWsTag);
		}

		public void SetOriginalIndexForNode(XmlDocConfigureDlg.LayoutTreeNode mainLayoutNode)
		{
			//Not important for migration
		}

		public XmlNode GetLayoutElement(string className, string layoutName)
		{
			return LegacyConfigurationUtils.GetLayoutElement(m_layoutInventory, className, layoutName);
		}

		public XmlNode GetPartElement(string className, string sRef)
		{
			return LegacyConfigurationUtils.GetPartElement(m_partInventory, className, sRef);
		}

		public void BuildRelationTypeList(XmlDocConfigureDlg.LayoutTreeNode ltn)
		{
			//Not important for migration - Handled separately by the new configuration dialog
		}

		public void BuildEntryTypeList(XmlDocConfigureDlg.LayoutTreeNode ltn, string layoutName)
		{
			//Not important for migration - Handled separately by the new configuration dialog
		}
		#endregion
	}
}
