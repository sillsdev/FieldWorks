// Copyright (c) 2016-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Palaso.Linq;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.Utils;
using XCore;
using DCM = SIL.FieldWorks.XWorks.DictionaryConfigurationMigrator;

namespace SIL.FieldWorks.XWorks.DictionaryConfigurationMigrators
{
	class FirstBetaMigrator : IDictionaryConfigurationMigrator
	{
		private ISimpleLogger m_logger;
		internal const int VersionBeta5 = 13;

		public FirstBetaMigrator() : this(null, null)
		{
		}

		public FirstBetaMigrator(FdoCache cache, SimpleLogger logger)
		{
			Cache = cache;
			m_logger = logger;
		}

		public FdoCache Cache { get; set; }

		public void MigrateIfNeeded(SimpleLogger logger, Mediator mediator, string appVersion)
		{
			m_logger = logger;
			Cache = (FdoCache)mediator.PropertyTable.GetValue("cache");
			var foundOne = string.Format("{0}: Configuration was found in need of migration. - {1}",
				appVersion, DateTime.Now.ToString("yyyy MMM d h:mm:ss"));
			var configSettingsDir = FdoFileHelper.GetConfigSettingsDir(Path.GetDirectoryName(Cache.ProjectId.Path));
			var dictionaryConfigLoc = Path.Combine(configSettingsDir, DictionaryConfigurationListener.DictionaryConfigurationDirectoryName);
			var stemPath = Path.Combine(dictionaryConfigLoc, "Stem" + DictionaryConfigurationModel.FileExtension);
			var lexemePath = Path.Combine(dictionaryConfigLoc, "Lexeme" + DictionaryConfigurationModel.FileExtension);
			if (File.Exists(stemPath) && !File.Exists(lexemePath))
			{
				File.Move(stemPath, lexemePath);
			}
			foreach (var config in DCM.GetConfigsNeedingMigration(Cache, DCM.VersionCurrent))
			{
				m_logger.WriteLine(foundOne);
				if (config.Label.StartsWith("Stem-"))
				{
					config.Label = config.Label.Replace("Stem-", "Lexeme-");
				}
				m_logger.WriteLine(string.Format("Migrating {0} configuration '{1}' from version {2} to {3}.",
					config.IsReversal ? "Reversal Index" : "Dictionary", config.Label, config.Version, DCM.VersionCurrent));
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
			else if(ConfigHasSubentriesNode(config)) // Hybrid configs have subentries
			{
				configPath = Path.Combine(dictionaryFolder, DCM.HybridFileName + DictionaryConfigurationModel.FileExtension);
			}
			else // Must be Lexeme
			{
				configPath = Path.Combine(dictionaryFolder, DCM.LexemeFileName + DictionaryConfigurationModel.FileExtension);
			}
			return new DictionaryConfigurationModel(configPath, Cache);
		}

		private static bool ConfigHasSubentriesNode(DictionaryConfigurationModel config)
		{
			// Perform a breadth first search for Subentries nodes
			var nodesToCompare = new List<ConfigurableDictionaryNode>(config.PartsAndSharedItems);
			while (nodesToCompare.Count > 0)
			{
				var currentNode = nodesToCompare[0];
				nodesToCompare.RemoveAt(0);
				if (currentNode.FieldDescription == "Subentries")
					return true;
				if (currentNode.Children != null)
					nodesToCompare.AddRange(currentNode.Children);
			}
			return false;
		}

		internal void MigrateFrom83Alpha(ISimpleLogger logger, DictionaryConfigurationModel oldConfig, DictionaryConfigurationModel currentDefaultModel)
		{
			// it may be helpful to have parents and current custom fields in the oldConfig (currentDefaultModel already has them):
			DictionaryConfigurationModel.SpecifyParentsAndReferences(oldConfig.Parts, oldConfig.SharedItems);
			DictionaryConfigurationController.MergeCustomFieldsIntoDictionaryModel(oldConfig, Cache);
			ConflateMainEntriesIfNecessary(logger, oldConfig);
			var currentDefaultList = new List<ConfigurableDictionaryNode>(currentDefaultModel.PartsAndSharedItems);
			foreach (var part in oldConfig.PartsAndSharedItems)
			{
				MigratePartFromOldVersionToCurrent(logger, oldConfig, part, FindMatchingChildNode(part.Label, currentDefaultList));
			}
			oldConfig.Version = DCM.VersionCurrent;
			logger.WriteLine("Migrated to version " + DCM.VersionCurrent);
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
			MigrateNewChildNodesInto(config.Parts[0], mainEntriesComplexForms);
			config.Parts.RemoveAll(IsComplexFormsNode);
			config.Parts[0].DictionaryNodeOptions = mainEntriesComplexForms.DictionaryNodeOptions;
			// Earlier versions of Hybrid mistakenly used VisibleComplexFormBackRefs instead of ComplexFormsNotSubentries under
			// Main Entry (Complex Forms) (but not under Main Entry). Remove the mistake.
			if (config.Parts[0].Children.Any(c => c.FieldDescription == "Subentries"))
				RemoveFromChildrenAndGroups(config.Parts[0].Children, "VisibleComplexFormBackRefs");
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
					MigrateNewChildNodesInto(oldConfigPart, currentDefaultConfigPart);
					goto case 9;
				case 9:
					UpgradeEtymologyCluster(oldConfigPart, oldConfig);
					goto case 10;
				case 10:
				case 11:
					MigrateNewChildNodesInto(oldConfigPart, currentDefaultConfigPart);
					break;
				case 12:
					break; // (Conflate Main Entry nodes) Migrated at config level; no nodewise migration.
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
					node.IsEnabled = !IsHybrid(oldConfig);
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

		private static bool IsHybrid(DictionaryConfigurationModel model)
		{
			return !model.IsRootBased && ConfigHasSubentriesNode(model);
		}

		/// <summary>
		/// This recursive method will migrate new nodes from default node to old config node
		/// </summary>
		private static void MigrateNewChildNodesInto(ConfigurableDictionaryNode destinationNode, ConfigurableDictionaryNode sourceNode)
		{
			if (destinationNode.Children == null || sourceNode.Children == null)
				return;
			if (((sourceNode.FieldDescription == "VariantFormEntryBackRefs" && destinationNode.DictionaryNodeOptions == null) ||
				DictionaryConfigurationModel.NoteInParaStyles.Contains(sourceNode.FieldDescription)) && sourceNode.DictionaryNodeOptions != null)
				destinationNode.DictionaryNodeOptions = sourceNode.DictionaryNodeOptions;
			// First recurse into each matching child node
			foreach (var newChild in sourceNode.Children)
			{
				var matchFromDefault = FindMatchingChildNode(newChild.Label, destinationNode.Children);
				if (matchFromDefault != null)
				{
					MigrateNewChildNodesInto(matchFromDefault, newChild);
				}
				else
				{
					var indexOfNewChild = sourceNode.Children.FindIndex(n => n.Label == newChild.Label);
					InsertNewNodeIntoOldConfig(destinationNode, newChild.DeepCloneUnderParent(destinationNode, true), sourceNode, indexOfNewChild);
				}
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
				InsertNewNodeIntoOldConfig(oldConfigNode, groupNode, defaultNode, defaultNode.ReferencedOrDirectChildren.IndexOf(group));
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

		private static void RemoveFromChildrenAndGroups(List<ConfigurableDictionaryNode> children, string fieldDescription)
		{
			children.RemoveAll(c => c.FieldDescription == fieldDescription);
			children.Where(c => c.DictionaryNodeOptions is DictionaryNodeGroupingOptions)
				.ForEach(c => RemoveFromChildrenAndGroups(c.Children, fieldDescription));
		}
	}
}
