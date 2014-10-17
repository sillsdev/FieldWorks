// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
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
		private SimpleLogger m_logger = new SimpleLogger();

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
			m_logger.WriteLine(String.Format("Migrating old fwlayout and parts config: '{0}' - {1}.", label, layout));
			m_logger.IncreaseIndent();
			var configNodeList = new List<ConfigurableDictionaryNode>();
			foreach(var node in oldNodes)
			{
				configNodeList.Add(ConvertLayoutTreeNodeToConfigNode(node));
			}
			var convertedModel = new DictionaryConfigurationModel();
			convertedModel.Parts = configNodeList;
			convertedModel.Label = label;
			convertedModel.Version = -1;
			CopyNewDefaultsIntoConvertedModel(layout, convertedModel);
			convertedModel.Save();
			m_logger.DecreaseIndent();
		}

		/// <summary>
		/// Migrates old dictionary and reversal configurations if there are not already new dictionary and reversal configurations.
		/// </summary>
		public void MigrateOldConfigurationsIfNeeded()
		{
			var versionProvider = new VersionInfoProvider(Assembly.GetExecutingAssembly(), true);
			if (!ProjectHasNewDictionaryConfigs())
			{
				m_logger.WriteLine(String.Format("{0}: Dictionary configurations were found in need of migration. - {1}",
					versionProvider.ApplicationVersion, DateTime.Now.ToString("yyyy MMM d h:mm:ss")));
				UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(
					"Undo Migrate old Dictionary Configurations", "Redo Migrate old Dictionary Configurations", Cache.ActionHandlerAccessor,
					() =>
					{
						var configureLayouts = GetConfigureLayoutsNodeForTool("lexiconDictionary");
						LegacyConfigurationUtils.BuildTreeFromLayoutAndParts(configureLayouts, this);
					});
			}
			if (!ProjectHasNewReversalConfigs())
			{
				m_logger.WriteLine(String.Format("{0}: Reversal configurations were found in need of migration. - {1}",
					versionProvider.ApplicationVersion, DateTime.Now.ToString("yyyy MMM d h:mm:ss")));
				UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(
					"Undo Migrate old Reversal Configurations", "Redo Migrate old Reversal Configurations", Cache.ActionHandlerAccessor,
					() =>
					{
						var configureLayouts = GetConfigureLayoutsNodeForTool("reversalToolEditComplete");
						LegacyConfigurationUtils.BuildTreeFromLayoutAndParts(configureLayouts, this);
					});
			}
			File.AppendAllText(Path.Combine(DictionaryConfigurationListener.GetProjectConfigurationDirectory(m_mediator), "ConfigMigrationLog.txt"),
				m_logger.Content);
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
			var control = (Control)DynamicLoader.CreateObject(contentAssemblyPath, contentClass); // REVIEW (Hasso) 2014.10: this var is never used
			var parameters = controlNode.SelectSingleNode("parameters");
			var configureLayouts = XmlUtils.FindNode(parameters, "configureLayouts");
			return configureLayouts;
		}

		internal bool ProjectHasNewDictionaryConfigs()
		{
			var newDictionaryConfigLoc = Path.Combine(FdoFileHelper.GetConfigSettingsDir(Path.GetDirectoryName(Cache.ProjectId.Path)), "Dictionary");
			return Directory.Exists(newDictionaryConfigLoc)
				&& Directory.EnumerateFiles(newDictionaryConfigLoc, "*" + DictionaryConfigurationModel.FileExtension).Any();
		}

		internal bool ProjectHasNewReversalConfigs()
		{
			//todo: Implement reversal configuration migration.
			return true;
			//var newReversalConfigLoc = Path.Combine(FdoFileHelper.GetConfigSettingsDir(Path.GetDirectoryName(Cache.ProjectId.Path)), "Reversals");
			//return Directory.Exists(newReversalConfigLoc);
		}

		/// <summary>
		/// This method will take a skeleton model which has been already converted from LayoutTreeNodes
		/// and fill in data that we did not convert for some reason. It will use the current shipping
		/// default model for the layout which the old model used. (eg. publishStem)
		/// </summary>
		private void CopyNewDefaultsIntoConvertedModel(string layout, DictionaryConfigurationModel convertedModel)
		{
			DictionaryConfigurationModel.SpecifyParents(convertedModel.Parts);
			DictionaryConfigurationModel currentDefaultModel;
			if(convertedModel.Version == -1)
			{
				const string extension = DictionaryConfigurationModel.FileExtension;
				var defaultPath = DictionaryConfigurationListener.GetDefaultConfigurationDirectory(m_mediator);
				const string defaultStemName = "Stem" + extension;
				const string defaultRootName = "Root" + extension;
				var projectConfigBasePath = FdoFileHelper.GetConfigSettingsDir(Cache.ProjectId.ProjectFolder);
				switch(layout)
				{
					case "publishStem":
					{
						convertedModel.FilePath = Path.Combine(projectConfigBasePath, "Dictionary", defaultStemName);
						currentDefaultModel = new DictionaryConfigurationModel(Path.Combine(defaultPath, defaultStemName), Cache);
						break;
					}
					case "publishRoot":
					{
						convertedModel.FilePath = Path.Combine(projectConfigBasePath, "Dictionary", defaultRootName);
						currentDefaultModel = new DictionaryConfigurationModel(Path.Combine(defaultPath, defaultRootName), Cache);
						break;
					}
					case "publishReversal" :
					{
						throw new NotImplementedException("Reversal index migration has not yet been implemented.");
					}
					default :
					{
						// If a user copied an old configuration FLEx appended '#' followed by a unique integer to the layout name.
						// We will write out the new configuration to a file which uses what the user named it but preserving the integer
						// as a potential customer support aid.
						var customSuffixIndex = layout.IndexOf('#');
						if(customSuffixIndex > 0 && layout.StartsWith("publishStem"))
						{
							var customFileName = String.Format("{0}-Stem-{1}{2}", convertedModel.Label, layout.Substring(customSuffixIndex), extension);
							convertedModel.FilePath = Path.Combine(projectConfigBasePath, "Dictionary", customFileName);
							currentDefaultModel = new DictionaryConfigurationModel(Path.Combine(defaultPath, defaultStemName), Cache);
						}
						else if(customSuffixIndex > 0 && layout.StartsWith("publishRoot"))
						{
							var customFileName = String.Format("{0}-Root-{1}{2}", convertedModel.Label, layout.Substring(customSuffixIndex), extension);
							convertedModel.FilePath = Path.Combine(projectConfigBasePath, "Dictionary", customFileName);
							currentDefaultModel = new DictionaryConfigurationModel(Path.Combine(defaultPath, defaultRootName), Cache);
						}
						else // probably a reversal index for a specific language
						{
							throw new NotImplementedException("Custom reversal index migration has not yet been implemented.");
						}
						break;
					}
				}
				Directory.CreateDirectory(Path.GetDirectoryName(convertedModel.FilePath));
				CopyNewDefaultsIntoConvertedModel(convertedModel, currentDefaultModel);
			}
		}

		internal void CopyNewDefaultsIntoConvertedModel(DictionaryConfigurationModel convertedModel, DictionaryConfigurationModel currentDefaultModel)
		{
			CopyDefaultsIntoConfigNode(convertedModel.Parts[0], currentDefaultModel.Parts[0], convertedModel.Version);
			for(var i = 1; i < convertedModel.Parts.Count; ++i)
			{
				// Any copies of the minor entry node in the model we are converting should use the defaults from the minor entry node
				CopyDefaultsIntoConfigNode(convertedModel.Parts[i], currentDefaultModel.Parts[1], convertedModel.Version);
			}
		}

		/// <summary>
		/// This method will copy values that were not converted (eg. FieldDescription and SubField) from the current default node
		/// into the converted node and add any children that are new in the current defaults to the converted node. The order of children
		/// in the converted node is maintained.
		/// </summary>
		private void CopyDefaultsIntoConfigNode(ConfigurableDictionaryNode convertedNode, ConfigurableDictionaryNode currentDefaultNode, int version)
		{
			if(convertedNode.Label != currentDefaultNode.Label)
			{
				throw new ArgumentException("Can not merge two nodes that do not match.");
			}
			convertedNode.FieldDescription = currentDefaultNode.FieldDescription;
			convertedNode.SubField = currentDefaultNode.SubField;
			// if the base has children and we don't they need to be added
			if(convertedNode.Children == null && currentDefaultNode.Children != null &&
				currentDefaultNode.Children.Count > 0)
			{
				convertedNode.Children = new List<ConfigurableDictionaryNode>(currentDefaultNode.Children);
				return;
			}
			// if there are child lists to merge then merge them
			if(convertedNode.Children != null && currentDefaultNode.Children != null)
			{
				var currentDefaultChildren = new List<ConfigurableDictionaryNode>(currentDefaultNode.Children);
				var matchedChildren = new List<ConfigurableDictionaryNode>();
				foreach(var child in convertedNode.Children)
				{
					var pathStringTonode = BuildPathStringFromNode(child);
					if(version == -1) // Some configuration nodes had name changes in the new verison
					{
						if(child.Label == "Components" && child.Parent.Label == "Component References")
						{
							child.Label = "Referenced Entries";
						}
					}
					var matchFromBase = FindMatchingChild(child, currentDefaultChildren, matchedChildren);
					if(matchFromBase != null)
					{
						CopyDefaultsIntoConfigNode(child, matchFromBase, version);
					}
					else
					{
						m_logger.WriteLine(String.Format("Could not match '{0}' treating as custom.", pathStringTonode));
						// This node does not match anything in the shipping defaults.
						// It is probably a custom field in which case the label should match the FieldDescription.
						child.FieldDescription = child.Label;
						child.IsCustomField = true;
					}
				}
				//remove all the matches from default list
				currentDefaultChildren.RemoveAll(matchedChildren.Contains);
				foreach(var newChild in currentDefaultChildren)
				{
					m_logger.WriteLine(String.Format("'{0}' was not in the old version adding from default config.", BuildPathStringFromNode(newChild)));
					convertedNode.Children.Add(newChild);
				}
			}
			else if(convertedNode.Children != null) // if we have children and the base doesn't
			{
				throw new Exception("These nodes are not likely to match the convertedModel.");
			}
		}

		private string BuildPathStringFromNode(ConfigurableDictionaryNode child)
		{
			var path = child.DisplayLabel;
			var node = child;
			while(node.Parent != null)
			{
				path = node.Parent.DisplayLabel+"->"+path;
				node = node.Parent;
			}
			return path;
		}

		/// <summary>
		/// Matches a child node based off of the labels removes it from the given list and returns it. Otherwise returns null.
		/// </summary>
		private ConfigurableDictionaryNode FindMatchingChild(ConfigurableDictionaryNode child,
			IEnumerable<ConfigurableDictionaryNode> currentDefaultChildren, List<ConfigurableDictionaryNode> matchedChildren)
		{
			var matchingChild = currentDefaultChildren.FirstOrDefault(baseChild => child.Label == baseChild.Label);
			if(matchingChild != null)
			{
				matchedChildren.Add(matchingChild);
				return matchingChild;
			}
			return null;
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
			if(node.ShowSenseConfig)
			{
				string before = null, style = null, after = null;
				if(!String.IsNullOrEmpty(node.Number))
				{
					node.SplitNumberFormat(out before, out style, out after);
				}
				options = new DictionaryNodeSenseOptions
				{
					DisplayEachSenseInAParagraph = node.ShowSenseAsPara,
					ShowSharedGrammarInfoFirst = node.ShowSingleGramInfoFirst,
					NumberEvenASingleSense = node.NumberSingleSense,
					BeforeNumber = before,
					AfterNumber = after,
					NumberingStyle = style,
					NumberStyle = GenerateNumberStyleFromLayoutTreeNode(node)
				};
			}
			if(!String.IsNullOrEmpty(node.LexRelType))
			{
				options = new DictionaryNodeListOptions();
				SetListOptionsProperties(node.LexRelType, node.LexRelTypeSequence, (DictionaryNodeListOptions)options);
			}
			if(!String.IsNullOrEmpty(node.EntryType))
			{
				if(node.EntryType == "complex")
				{
					options = new DictionaryNodeComplexFormOptions { DisplayEachComplexFormInAParagraph = node.ShowComplexFormPara };
				}
				else
				{
					options = new DictionaryNodeListOptions();
				}
				SetListOptionsProperties(node.EntryType, node.EntryTypeSequence, (DictionaryNodeListOptions)options);
			}
			return options;
		}

		private void SetListOptionsProperties(string type, string sequence, DictionaryNodeListOptions options)
		{
			options.Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>();
			options.ListId = (DictionaryNodeListOptions.ListIds)Enum.Parse(typeof(DictionaryNodeListOptions.ListIds), type, true);
			// Create a list of dictionary node options from a string of the format "+guid,-guid,+guid"
			options.Options.AddRange(sequence.Split(',').Select(id => new DictionaryNodeListOptions.DictionaryNodeOption
																							{
																								IsEnabled = id.StartsWith("+"),
																								Id = id.Trim(new[] { '+', '-', ' ' })
																							}));
		}

		private string GenerateNumberStyleFromLayoutTreeNode(XmlDocConfigureDlg.LayoutTreeNode node)
		{
			var styleSheet = FontHeightAdjuster.StyleSheetFromMediator(m_mediator);
			const string senseNumberStyleBase = "Dictionary-SenseNumber";
			var senseNumberStyleName = senseNumberStyleBase;
			var matchedOrCreated = false;
			var styleNumberSuffix = 1;
			do
			{
				if(styleSheet.FindStyle(senseNumberStyleName) == null)
				{
					var senseNumberStyle = Cache.ServiceLocator.GetInstance<IStStyleFactory>().Create();
					Cache.LangProject.StylesOC.Add(senseNumberStyle);
					senseNumberStyle.Name = senseNumberStyleName;
					senseNumberStyle.Type = StyleType.kstCharacter;
					senseNumberStyle.UserLevel = 1;
					senseNumberStyle.IsBuiltIn = false;
					var propsBldr = TsPropsBldrClass.Create();
					propsBldr.SetStrPropValue((int)FwTextPropType.ktptFontFamily, node.NumFont);
					if(!String.IsNullOrEmpty(node.NumStyle))
					{
						if(node.NumStyle.Contains("-bold"))
						{
							propsBldr.SetIntPropValues((int)FwTextPropType.ktptBold, (int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvOff);
						}
						else if(node.NumStyle.Contains("bold"))
						{
							propsBldr.SetIntPropValues((int)FwTextPropType.ktptBold, (int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvForceOn);
						}
						if(node.NumStyle.Contains("-italic"))
						{
							propsBldr.SetIntPropValues((int)FwTextPropType.ktptItalic, (int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvOff);
						}
						else if(node.NumStyle.Contains("italic"))
						{
							propsBldr.SetIntPropValues((int)FwTextPropType.ktptItalic, (int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvForceOn);
						}
						senseNumberStyle.Rules = propsBldr.GetTextProps();
					}
					styleSheet.PutStyle(senseNumberStyleName, "Used for configuring some sense numbers in the dictionary",
						senseNumberStyle.Hvo, 0, 0, (int)StyleType.kstCharacter, false, false, propsBldr.GetTextProps());
					matchedOrCreated = true;
				}
				else if(LayoutOptionsMatchStyle(styleSheet.Styles[senseNumberStyleName], node))
				{
					matchedOrCreated = true;
				}
				else
				{
					senseNumberStyleName = String.Format("{0}-{1}", senseNumberStyleBase, ++styleNumberSuffix);
				}
			} while(!matchedOrCreated);
			return senseNumberStyleName;
		}

		private bool LayoutOptionsMatchStyle(BaseStyleInfo style, XmlDocConfigureDlg.LayoutTreeNode node)
		{
			// if the style isn't even a character style
			if(!style.IsCharacterStyle)
			{
				return false;
			}
			var fontInfo = style.DefaultCharacterStyleInfo;
			// if nothing about bold or italic are in the node but there is information in the style
			if(String.IsNullOrEmpty(node.NumStyle) && (fontInfo.Bold.ValueIsSet || fontInfo.Italic.ValueIsSet))
			{
				return false;
			}
			// if we have bold or italic info in the node but it doesn't match the style
			if(!String.IsNullOrEmpty(node.NumStyle) &&
				((node.NumStyle.Contains("-bold") && fontInfo.Bold.ValueIsSet && fontInfo.Bold.Value) ||
				 (!node.NumStyle.Contains("-bold") && node.NumStyle.Contains("bold") && fontInfo.Bold.ValueIsSet && !fontInfo.Bold.Value) ||
				 (node.NumStyle.Contains("bold") && !fontInfo.Bold.ValueIsSet) ||
				 (!node.NumStyle.Contains("bold") && fontInfo.Bold.ValueIsSet) ||
				 (node.NumStyle.Contains("-italic") && fontInfo.Italic.ValueIsSet && fontInfo.Italic.Value) ||
				 (!node.NumStyle.Contains("-italic") && node.NumStyle.Contains("italic") && fontInfo.Italic.ValueIsSet && !fontInfo.Italic.Value) ||
				 (node.NumStyle.Contains("italic") && !fontInfo.Italic.ValueIsSet) ||
				 (!node.NumStyle.Contains("italic") && fontInfo.Italic.ValueIsSet)))
			{
				return false;
			}
			// if the font doesn't match
			if(String.IsNullOrEmpty(node.NumFont) && fontInfo.FontName.ValueIsSet || // node value is empty but fontInfo isn't
				!String.IsNullOrEmpty(node.NumFont) && !fontInfo.FontName.ValueIsSet || // fontinfo is empty but node value isn't
				(fontInfo.FontName.ValueIsSet && String.Compare(node.NumFont, fontInfo.FontName.Value, StringComparison.Ordinal) != 0))
			{
				// node value was empty but fontInfo isn't or
				// fontInfo was empty but node value wasn't or
				// both strings had content but it didn't match
				return false;
			}
			return true;
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
