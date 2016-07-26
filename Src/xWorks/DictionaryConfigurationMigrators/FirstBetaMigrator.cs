// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

		public FirstBetaMigrator() : this(null, null)
		{
		}

		public FirstBetaMigrator(FdoCache cache, SimpleLogger logger)
		{
			Cache = cache;
			m_logger = logger;
		}

		public void MigrateIfNeeded(SimpleLogger logger, Mediator mediator, string appVersion)
		{
			m_logger = logger;
			Cache = (FdoCache)mediator.PropertyTable.GetValue("cache");
			var foundOne = string.Format("{0}: Configuration was found in need of migration. - {1}",
				appVersion, DateTime.Now.ToString("yyyy MMM d h:mm:ss"));
			foreach (var config in DictionaryConfigurationMigrator.GetConfigsNeedingMigration(Cache, DCM.VersionCurrent))
			{
				m_logger.WriteLine(foundOne);
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
				configPath = Path.Combine(dictionaryFolder, "Root" + DictionaryConfigurationModel.FileExtension);
			}
			else if(ConfigHasSubentriesNode(config)) // Hybrid configs have subentries
			{
				configPath = Path.Combine(dictionaryFolder, "Hybrid" + DictionaryConfigurationModel.FileExtension);
			}
			else // Must be stem
			{
				configPath = Path.Combine(dictionaryFolder, "Stem" + DictionaryConfigurationModel.FileExtension);
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
			var oldConfigList = new List<ConfigurableDictionaryNode>(oldConfig.PartsAndSharedItems);
			var currentDefaultList = new List<ConfigurableDictionaryNode>(currentDefaultModel.PartsAndSharedItems);
			for (var part = 0; part < oldConfigList.Count; ++part)
			{
				MoveNodesIntoNewGroups(oldConfigList[part], currentDefaultList[part]);
			}
			oldConfig.Version = DCM.VersionCurrent;
		}

		/// <summary>
		/// This recursive method will create group nodes in the migrated config and move all children in the node
		/// which belong in the group into it
		/// </summary>
		private void MoveNodesIntoNewGroups(ConfigurableDictionaryNode oldConfigNode, ConfigurableDictionaryNode defaultNode)
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
				var groupChildList = new List<ConfigurableDictionaryNode>();
				var groupNode = group.DeepCloneUnderParent(oldConfigNode);
				groupNode.Children = groupChildList;
				for (var i = oldConfigNode.Children.Count - 1; i >= 0; --i)
				{
					var oldChild = oldConfigNode.Children[i];
					if (group.Children.Any(groupChild => groupChild.Label == oldChild.Label))
					{
						groupChildList.Insert(0, oldChild);
						oldChild.Parent = groupNode;
						oldConfigNode.Children.RemoveAt(i);
					}
				}
				InsertGroupNodeIntoOldConfig(oldConfigNode, groupNode, defaultNode, defaultNode.ReferencedOrDirectChildren.IndexOf(group));
			}
		}

		/// <summary>
		/// Return a node that matches the label from the given collection and the children of any groups in that collection
		/// </summary>
		/// <returns>first match or null</returns>
		/// <remarks>it should be the only match, but that is not enforced by this code</remarks>
		private ConfigurableDictionaryNode FindMatchingChildNode(string label, List<ConfigurableDictionaryNode> defaultChildren)
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

		private void InsertGroupNodeIntoOldConfig(ConfigurableDictionaryNode oldConfigNode, ConfigurableDictionaryNode groupNode, ConfigurableDictionaryNode defaultNode, int indexOf)
		{
			if (indexOf == 0)
				oldConfigNode.Children.Insert(0, groupNode);
			else
			{
				var olderSiblingLabel = defaultNode.Children[indexOf - 1].Label;
				var indexOfOlderSibling = oldConfigNode.Children.FindIndex(n => n.Label == olderSiblingLabel);
				if (indexOfOlderSibling >= 0)
					oldConfigNode.Children.Insert(indexOfOlderSibling + 1, groupNode);
				else
					oldConfigNode.Children.Add(groupNode);
			}
		}

		public FdoCache Cache { get; set; }
	}
}
