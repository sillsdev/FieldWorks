// Copyright (c) 2016-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.Linq;
using SIL.LCModel.Utils;
using XCore;
using DCM = SIL.FieldWorks.XWorks.DictionaryConfigurationMigrator;
using System.Xml.Linq;

namespace SIL.FieldWorks.XWorks.DictionaryConfigurationMigrators
{
	public class FirstBetaMigrator : IDictionaryConfigurationMigrator
	{
		private ISimpleLogger m_logger;
		internal const int VersionBeta5 = 14;
		internal const int VersionRC2 = 17; // 8.3.7

		public FirstBetaMigrator() : this(null, null)
		{
		}

		public FirstBetaMigrator(LcmCache cache, SimpleLogger logger)
		{
			Cache = cache;
			m_logger = logger;
		}

		public LcmCache Cache { get; set; }

		public void MigrateIfNeeded(SimpleLogger logger, PropertyTable propertyTable, string appVersion)
		{
			m_logger = logger;
			Cache = propertyTable.GetValue<LcmCache>("cache");
			var foundOne = string.Format("{0}: Configuration was found in need of migration. - {1}",
				appVersion, DateTime.Now.ToString("yyyy MMM d h:mm:ss"));
			var configSettingsDir = LcmFileHelper.GetConfigSettingsDir(Cache.ProjectId.ProjectFolder);
			var dictionaryConfigLoc = Path.Combine(configSettingsDir, DictionaryConfigurationListener.DictionaryConfigurationDirectoryName);
			var stemPath = Path.Combine(dictionaryConfigLoc, "Stem" + DictionaryConfigurationModel.FileExtension);
			var lexemePath = Path.Combine(dictionaryConfigLoc, "Lexeme" + DictionaryConfigurationModel.FileExtension);
			if (File.Exists(stemPath) && !File.Exists(lexemePath))
			{
				File.Move(stemPath, lexemePath);
			}
			RenameReversalConfigFiles(configSettingsDir);
			foreach (var config in DCM.GetConfigsNeedingMigration(Cache, DCM.VersionCurrent))
			{
				m_logger.WriteLine(foundOne);
				if (config.Label.StartsWith("Stem-"))
				{
					config.Label = config.Label.Replace("Stem-", "Lexeme-");
				}
				m_logger.WriteLine(string.Format("Migrating {0} configuration '{1}' from version {2} to {3}.",
					config.Type, config.Label, config.Version, DCM.VersionCurrent));
				m_logger.IncreaseIndent();
				MigrateFrom83Alpha(logger, config, LoadBetaDefaultForAlphaConfig(config));
				config.Save();
				m_logger.DecreaseIndent();
			}
		}

		internal DictionaryConfigurationModel LoadBetaDefaultForAlphaConfig(DictionaryConfigurationModel config)
		{
			var dictionaryFolder = Path.Combine(FwDirectoryFinder.DefaultConfigurations, DictionaryConfigurationListener.DictionaryConfigurationDirectoryName);
			var reversalFolder = Path.Combine(FwDirectoryFinder.DefaultConfigurations, DictionaryConfigurationListener.ReversalIndexConfigurationDirectoryName);

			string configPath;
			// There is only one default config for reversals
			if (config.IsReversal)
			{
				configPath = Path.Combine(reversalFolder, DictionaryConfigurationModel.AllReversalIndexesFilenameBase + DictionaryConfigurationModel.FileExtension);
			}
			else if (config.IsRootBased)
			{
				configPath = Path.Combine(dictionaryFolder, DCM.RootFileName + DictionaryConfigurationModel.FileExtension);
			}
			else if (config.IsHybrid) // Hybrid configs have subentries
			{
				configPath = Path.Combine(dictionaryFolder, DCM.HybridFileName + DictionaryConfigurationModel.FileExtension);
			}
			else // Must be Lexeme
			{
				configPath = Path.Combine(dictionaryFolder, DCM.LexemeFileName + DictionaryConfigurationModel.FileExtension);
			}
			return new DictionaryConfigurationModel(configPath, Cache);
		}

		internal void MigrateFrom83Alpha(ISimpleLogger logger, DictionaryConfigurationModel oldConfig, DictionaryConfigurationModel currentDefaultModel)
		{
			// it may be helpful to have parents and current custom fields in the oldConfig (currentDefaultModel already has them):
			DictionaryConfigurationModel.SpecifyParentsAndReferences(oldConfig.Parts, oldConfig.SharedItems);
			DictionaryConfigurationController.MergeCustomFieldsIntoDictionaryModel(oldConfig, Cache);
			ChooseAppropriateComplexForms(oldConfig); // 13->14, but needed before rearranging and adding new nodes
			ConflateMainEntriesIfNecessary(logger, oldConfig); // 12->13, but needed before rearranging and adding new nodes
			var currentDefaultList = new List<ConfigurableDictionaryNode>(currentDefaultModel.PartsAndSharedItems);
			foreach (var part in oldConfig.PartsAndSharedItems)
			{
				var defaultPart = FindMatchingChildNode(part.Label, currentDefaultList);
				if (defaultPart == null)
				{
					throw new NullReferenceException(string.Format(
						"{0} is corrupt. {1} has no corresponding part in the defaults (perhaps it missed a rename migration step).",
						oldConfig.FilePath, part.Label));
				}
				MigratePartFromOldVersionToCurrent(logger, oldConfig, part, defaultPart);
			}
			oldConfig.Version = DCM.VersionCurrent;
			logger.WriteLine("Migrated to version " + DCM.VersionCurrent);
		}

		/// <summary>
		/// Earlier versions of Hybrid mistakenly used VisibleComplexFormBackRefs instead of ComplexFormsNotSubentries. Correct this mistake.
		/// Referenced Complex Forms should be *Other* Referenced Complex Forms whenever they are siblings of Subentries
		/// </summary>
		private static void ChooseAppropriateComplexForms(DictionaryConfigurationModel migratingModel)
		{
			if (!migratingModel.IsHybrid)
				return;
			DCM.PerformActionOnNodes(migratingModel.Parts, parentNode =>
			{
				if (parentNode.ReferencedOrDirectChildren != null && parentNode.ReferencedOrDirectChildren.Any(node => node.FieldDescription == "Subentries"))
					parentNode.ReferencedOrDirectChildren.Where(sib => sib.FieldDescription == "VisibleComplexFormBackRefs").ForEach(sib =>
					{
						sib.Label = "Other Referenced Complex Forms";
						sib.FieldDescription = "ComplexFormsNotSubentries";
					});
			});
		}

		private static void ConflateMainEntriesIfNecessary(ISimpleLogger logger, DictionaryConfigurationModel config)
		{
			if (config.Version >= VersionBeta5 || config.IsRootBased || config.IsReversal)
				return;
			var mainEntriesComplexForms = config.Parts.FirstOrDefault(n => IsComplexFormsNode(n) && n.IsEnabled)
				?? config.Parts.FirstOrDefault(IsComplexFormsNode);
			if (mainEntriesComplexForms == null)
			{
				logger.WriteLine(string.Format("Unable to conflate main entries for config '{0}' (version {1})", config.Label, config.Version));
				return;
			}
			MigrateNewChildNodesAndOptionsInto(config.Parts[0], mainEntriesComplexForms);
			// Remove all complex form nodes *except* Main Entry
			for (var i = config.Parts.Count - 1; i >= 1; --i)
				if (IsComplexFormsNode(config.Parts[i]))
					config.Parts.RemoveAt(i);
		}

		private static bool IsComplexFormsNode(ConfigurableDictionaryNode node)
		{
			var options = node.DictionaryNodeOptions as DictionaryNodeListOptions;
			return options != null && options.ListId == DictionaryNodeListOptions.ListIds.Complex;
		}

		private static void MigratePartFromOldVersionToCurrent(ISimpleLogger logger, DictionaryConfigurationModel oldConfig,
			ConfigurableDictionaryNode oldConfigPart, ConfigurableDictionaryNode currentDefaultConfigPart)
		{
			var oldVersion = oldConfig.Version;
			if (oldVersion < FirstAlphaMigrator.VersionAlpha3)
				throw new ApplicationException("Beta migration starts at VersionAlpha3 (8)");
			switch (oldVersion)
			{
				case FirstAlphaMigrator.VersionAlpha3:
					MoveNodesIntoNewGroups(oldConfigPart, currentDefaultConfigPart);
					MigrateNewChildNodesAndOptionsInto(oldConfigPart, currentDefaultConfigPart);
					goto case 9;
				case 9:
					UpgradeEtymologyCluster(oldConfigPart, oldConfig);
					goto case 10;
				case 10:
				case 11:
				case 12:
				case 13:
					RemoveMostOfGramInfoUnderRefdComplexForms(oldConfigPart);
					goto case VersionBeta5;
				case VersionBeta5:
				case 15:
					MigrateNewChildNodesAndOptionsInto(oldConfigPart, currentDefaultConfigPart);
					goto case 16;
				case 16:
					RemoveHiddenChildren(oldConfigPart, logger);
					goto case VersionRC2;
				case VersionRC2:
					ChangeReferenceSenseHeadwordFieldName(oldConfigPart);
					goto case 18;
				case 18:
					RemoveReferencedHeadwordSubField(oldConfigPart);
					break;
				default:
					logger.WriteLine(string.Format(
						"Unable to migrate {0}: no migration instructions for version {1}", oldConfigPart.Label, oldVersion));
					break;
			}
		}

		/// <summary>
		/// Case FirstAlphaMigrator.VersionAlpha3 above will pull in all the new nodes in the Etymology cluster by Label.
		/// Gloss is the only pre-existing node that doesn't have a new name, so it won't be replaced.
		/// It needs to be marked Enabled. The main Etymology node needs several modifications.
		/// Three old nodes will need deleting: Etymological Form, Comment and Source
		/// </summary>
		/// <param name="oldConfigPart"></param>
		/// <param name="oldConfig"></param>
		private static void UpgradeEtymologyCluster(ConfigurableDictionaryNode oldConfigPart, DictionaryConfigurationModel oldConfig)
		{
			if (oldConfigPart.Children == null || oldConfigPart.Children.Count == 0)
				return; // safety net

			var etymNodes = new List<ConfigurableDictionaryNode>();
			DCM.PerformActionOnNodes(oldConfigPart.Children, node =>
			{
				if (node.Label == "Etymology")
					etymNodes.Add(node); // since we have to do some node deleting, just collect up the relevant nodes
			});

			foreach (var node in etymNodes)
			{
				if (node.IsCustomField) // Unfortunately there are some pathological users who have ancient custom fields named etymology
					continue; // Leave them be
				// modify main node
				var etymSequence = "EtymologyOS";
				if (oldConfig.IsReversal)
				{
					node.SubField = etymSequence;
					node.FieldDescription = "Entry";
					node.IsEnabled = true;
				}
				else
				{
					node.FieldDescription = etymSequence;
					node.IsEnabled = !oldConfig.IsHybrid;
				}
				node.CSSClassNameOverride = "etymologies";
				node.Before = "(";
				node.Between = " ";
				node.After = ") ";

				if (node.Children == null)
					continue;

				// enable Gloss node
				var glossNode = node.Children.Find(n => n.Label == "Gloss");
				if(glossNode != null)
					glossNode.IsEnabled = true;

				// enable Source Language Notes
				var notesList = node.Children.Find(n => n.FieldDescription == "LanguageNotes");
				if(notesList != null) // ran into some cases where this node didn't exist in reversal config!
					notesList.IsEnabled = true;

				// remove old children
				var nodesToRemove = new[] {"Etymological Form", "Comment", "Source"};
				node.Children.RemoveAll(n => nodesToRemove.Contains(n.Label));
			}
			// Etymology changed too much to be matched in the PreHistoricMigration and was marked as custom
			DCM.PerformActionOnNodes(etymNodes, n => {n.IsCustomField = false;});
		}

		private static void RemoveReferencedHeadwordSubField(ConfigurableDictionaryNode part)
		{
			DCM.PerformActionOnNodes(part.Children, node =>
			{
				// AllReversalSubentries under Referenced Headword field is ReversalName
				if (node.FieldDescription == "ReversalName" && node.SubField == "MLHeadWord")
				{
					node.SubField = null;
				}
			});
		}

		private static void RemoveMostOfGramInfoUnderRefdComplexForms(ConfigurableDictionaryNode part)
		{
			DCM.PerformActionOnNodes(part.Children, node =>
			{
				// GramInfo under (Other) Ref'd Complex Forms is MorphoSystaxAnalyses
				// GramInfo under Senses  is MorphoSyntaxAnalysisRA and should not lose any children
				if (node.FieldDescription == "MorphoSyntaxAnalyses")
					node.Children.RemoveAll(child => child.FieldDescription != "MLPartOfSpeech");
			});
		}

		/// <summary>
		/// Renames the .fwdictconfig files in the ReversalIndex Folder
		/// For ex. english.fwdictconfig to en.fwdictconfig
		/// </summary>
		/// <param name="configSettingsDir"></param>
		private static void RenameReversalConfigFiles(string configSettingsDir)
		{
			var reversalIndexConfigLoc = Path.Combine(configSettingsDir, DictionaryConfigurationListener.ReversalIndexConfigurationDirectoryName);
			var dictConfigFiles = new List<string>(DCM.ConfigFilesInDir(reversalIndexConfigLoc));
			string newFName = string.Empty;
			foreach (string fName in dictConfigFiles)
			{
				newFName = GetWritingSystemName(fName);
				if(!string.IsNullOrEmpty(newFName))
				{
					newFName = Path.Combine(Path.GetDirectoryName(fName), newFName + DictionaryConfigurationModel.FileExtension);
					File.Move(fName, newFName);
				}
			}
		}

		/// <summary>
		/// Reads the .fwdictconfig config file and gets the writing system name
		/// </summary>
		/// <param name="fileName"></param>
		/// <returns></returns>
		private static string GetWritingSystemName(string fileName)
		{
			string wsName = string.Empty;
			try
			{
				XDocument xDoc = XDocument.Load(fileName);
				XElement rootElement = xDoc.Root;
				if (rootElement != null)
				{
					XAttribute writingSystemAttribute = rootElement.Attribute("writingSystem");
					if (writingSystemAttribute != null)
					{
						wsName = writingSystemAttribute.Value.ToString();
					}
				}
			}
			catch (Exception e)
			{
				wsName = string.Empty;
			}
			return wsName;
		}

		/// <summary>
		/// This recursive method will migrate new nodes from sourceNode into destinationNode node
		/// </summary>
		private static void MigrateNewChildNodesAndOptionsInto(ConfigurableDictionaryNode destinationNode, ConfigurableDictionaryNode sourceNode)
		{
			// REVIEW (Hasso) 2017.03: If this is a NoteInParaStyles node: Rather than overwriting the user's Options, copy their Options into a new WS&ParaOptions
			if ((destinationNode.DictionaryNodeOptions == null || DictionaryConfigurationModel.NoteInParaStyles.Contains(sourceNode.FieldDescription)) &&
				sourceNode.DictionaryNodeOptions != null)
				destinationNode.DictionaryNodeOptions = sourceNode.DictionaryNodeOptions;
			EnsureCssOverrideAndStylesAreUpdated(destinationNode, sourceNode);
			// LT-18286: don't merge direct children into sharing parents.
			if (destinationNode.ReferencedNode != null || destinationNode.Children == null || sourceNode.Children == null)
				return;
			// First recurse into each matching child node
			foreach (var newChild in sourceNode.Children)
			{
				var matchFromDestination = FindMatchingChildNode(newChild.Label, destinationNode.Children);
				if (matchFromDestination != null)
				{
					MigrateNewChildNodesAndOptionsInto(matchFromDestination, newChild);
				}
				else
				{
					var indexOfNewChild = sourceNode.Children.FindIndex(n => n.Label == newChild.Label);
					InsertNewNodeIntoOldConfig(destinationNode, newChild.DeepCloneUnderParent(destinationNode, true), sourceNode, indexOfNewChild);
				}
			}
		}

		private static void EnsureCssOverrideAndStylesAreUpdated(ConfigurableDictionaryNode destinationNode, ConfigurableDictionaryNode sourceNode)
		{
			if (sourceNode.StyleType != ConfigurableDictionaryNode.StyleTypes.Default && destinationNode.StyleType == ConfigurableDictionaryNode.StyleTypes.Default)
			{
				var nodeStyleType = sourceNode.StyleType;
				var nodeParaOpts = destinationNode.DictionaryNodeOptions as IParaOption;
				if (nodeParaOpts != null)
				{
					nodeStyleType = nodeParaOpts.DisplayEachInAParagraph
						? ConfigurableDictionaryNode.StyleTypes.Paragraph
						: ConfigurableDictionaryNode.StyleTypes.Character;
				}
				destinationNode.StyleType = nodeStyleType;
			}
			if (sourceNode.StyleType == destinationNode.StyleType && // in case the user changed, for example, from Paragraph to Character style
				!string.IsNullOrEmpty(sourceNode.Style) && string.IsNullOrEmpty(destinationNode.Style))
			{
				destinationNode.Style = sourceNode.Style;
			}
			if (!string.IsNullOrEmpty(sourceNode.CSSClassNameOverride) && string.IsNullOrEmpty(destinationNode.CSSClassNameOverride))
			{
				destinationNode.CSSClassNameOverride = sourceNode.CSSClassNameOverride;
			}
		}

		/// <summary>
		/// This recursive method will create group nodes in the migrated config and move all children in the node
		/// which belong in the group into it
		/// </summary>
		private static void MoveNodesIntoNewGroups(ConfigurableDictionaryNode oldConfigNode, ConfigurableDictionaryNode defaultNode)
		{
			if (oldConfigNode.Children == null || defaultNode.Children == null)
				return;
			// First recurse into each matching child node
			foreach (var oldChild in oldConfigNode.Children)
			{
				var matchFromDefault = FindMatchingChildNode(oldChild.Label, defaultNode.Children);
				if (matchFromDefault != null)
				{
					MoveNodesIntoNewGroups(oldChild, matchFromDefault);
				}
			}
			// Next find any groups at this level and move the matching children defaultChildren into the group
			foreach (var group in defaultNode.Children.Where(n => n.DictionaryNodeOptions is DictionaryNodeGroupingOptions))
			{
				// DeepClone skips children for grouping nodes. We want this, because we are going to move children in from the old configNode.
				var groupNode = group.DeepCloneUnderParent(oldConfigNode);
				groupNode.Children = new List<ConfigurableDictionaryNode>();
				for (var i = oldConfigNode.Children.Count - 1; i >= 0; --i)
				{
					var oldChild = oldConfigNode.Children[i];
					if (group.Children.Any(groupChild => groupChild.Label == oldChild.Label))
					{
						groupNode.Children.Insert(0, oldChild);
						oldChild.Parent = groupNode;
						oldConfigNode.Children.RemoveAt(i);
					}
				}
				InsertNewNodeIntoOldConfig(oldConfigNode, groupNode, defaultNode, defaultNode.Children.IndexOf(group));
			}
		}

		/// <summary>
		/// Return a node that matches the label from the given collection and the children of any groups in that collection
		/// </summary>
		/// <returns>first match or null</returns>
		/// <remarks>it should be the only match, but that is not enforced by this code</remarks>
		private static ConfigurableDictionaryNode FindMatchingChildNode(string label, List<ConfigurableDictionaryNode> defaultChildren)
		{
			var possibleMatches = new List<ConfigurableDictionaryNode>(defaultChildren);
			// Add the children of default groups as possible matches
			foreach (var child in defaultChildren.Where(sibling => sibling.DictionaryNodeOptions is DictionaryNodeGroupingOptions))
			{
				if (child.Children != null)
					possibleMatches.AddRange(child.Children);
			}
			return possibleMatches.FirstOrDefault(n => n.Label == label);
		}

		private static void InsertNewNodeIntoOldConfig(ConfigurableDictionaryNode destinationParentNode, ConfigurableDictionaryNode newChildNode,
			ConfigurableDictionaryNode sourceParentNode, int indexInSourceParentNode)
		{
			if (indexInSourceParentNode == 0)
				destinationParentNode.Children.Insert(0, newChildNode);
			else
			{
				var olderSiblingLabel = sourceParentNode.Children[indexInSourceParentNode - 1].Label;
				var indexOfOlderSibling = destinationParentNode.Children.FindIndex(n => n.Label == olderSiblingLabel);
				if (indexOfOlderSibling >= 0)
					destinationParentNode.Children.Insert(indexOfOlderSibling + 1, newChildNode);
				else
					destinationParentNode.Children.Add(newChildNode);
			}
		}

		/// <summary>LT-18286: One Sharing Parent in Hybrid erroneously got direct children (from a migration step). Remove them.</summary>
		private static void RemoveHiddenChildren(ConfigurableDictionaryNode parent, ISimpleLogger logger)
		{
			DCM.PerformActionOnNodes(parent.Children, p =>
			{
				if (p.ReferencedNode != null && p.Children != null && p.Children.Any())
				{
					logger.WriteLine(DCM.BuildPathStringFromNode(p) + " contains both Referenced And Direct Children. Removing Direct Children.");
					p.Children = new List<ConfigurableDictionaryNode>();
				}
			});
		}

		/// <summary>LT-18288: Change Headword to HeadwordRef for All "Referenced Sense Headword" to allow users to select WS</summary>
		private static void ChangeReferenceSenseHeadwordFieldName(ConfigurableDictionaryNode oldConfigPart)
		{
			DCM.PerformActionOnNodes(oldConfigPart.Children, node =>
			{
				if (node.Label == "Referenced Sense Headword")
					node.FieldDescription = "HeadWordRef";
			});
		}
	}
}
