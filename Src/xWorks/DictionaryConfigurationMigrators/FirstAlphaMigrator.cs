using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SIL.FieldWorks.FDO;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.XWorks.DictionaryConfigurationMigrators
{
	/// <summary>
	/// This file will migrate all the configurations produced during the first 8.3 alpha
	/// </summary>
	public class FirstAlphaMigrator : IDictionaryConfigurationMigrator
	{
		public FirstAlphaMigrator() : this(null)
		{
		}

		public FirstAlphaMigrator(FdoCache cache)
		{
			Cache = cache;
		}

		private FdoCache Cache { get; set; }

		public void MigrateIfNeeded(SimpleLogger logger, Mediator mediator, string applicationVersion)
		{
			Cache = (FdoCache)mediator.PropertyTable.GetValue("cache");
			foreach (var config in GetConfigsNeedingMigratingFrom83())
			{
				MigrateFrom83Alpha(config);
				config.Save();
			}
		}

		internal List<DictionaryConfigurationModel> GetConfigsNeedingMigratingFrom83()
		{
			var configSettingsDir = FdoFileHelper.GetConfigSettingsDir(Path.GetDirectoryName(Cache.ProjectId.Path));
			var dictionaryConfigLoc = Path.Combine(configSettingsDir, DictionaryConfigurationListener.DictionaryConfigurationDirectoryName);
			var reversalIndexConfigLoc = Path.Combine(configSettingsDir, DictionaryConfigurationListener.ReversalIndexConfigurationDirectoryName);
			var projectConfigPaths = new List<string>(DictionaryConfigurationMigrator.ConfigFilesInDir(dictionaryConfigLoc));
			projectConfigPaths.AddRange(DictionaryConfigurationMigrator.ConfigFilesInDir(reversalIndexConfigLoc));
			return projectConfigPaths.Select(path => new DictionaryConfigurationModel(path, null))
				.Where(model => model.Version < DictionaryConfigurationMigrator.VersionCurrent).ToList();
		}

		internal void MigrateFrom83Alpha(DictionaryConfigurationModel alphaModel)
		{
			var allParts = new List<ConfigurableDictionaryNode>(alphaModel.Parts);
			allParts.AddRange(alphaModel.SharedItems);
			switch (alphaModel.Version)
			{
				case -1: // previous migrations neglected to update the version number; this is the same as 1
				case 1:
					RemoveReferencedItems(alphaModel.Parts);
					ExtractWritingSystemOptionsFromReferringSenseOptions(alphaModel.Parts);
					goto case 2;
				case 2:
					HandleFieldChanges(allParts, 2, alphaModel.IsReversal);
					goto case 3;
				case 3:
					HandleLabelChanges(allParts, 3);
					HandleFieldChanges(allParts, 3, false);
					DictionaryConfigurationMigrator.SetWritingSystemForReversalModel(alphaModel, Cache);
					AddSharedNodesToAlphaConfigurations(alphaModel, alphaModel.WritingSystem != null);
					goto case 4;
				case 4:
					HandleOptionsChanges(allParts, 4);
					HandleCssClassChanges(allParts, 4);
					break;
			}
			alphaModel.Version = DictionaryConfigurationMigrator.VersionCurrent;
		}

		private void AddSharedNodesToAlphaConfigurations(DictionaryConfigurationModel model, bool isReversal)
		{
			if (model.SharedItems.Any())
			{
				// TODO: Log something about this unexpected situation
				return;
			}
			foreach (var configNode in model.Parts)
			{
				SetReferenceNodeInConfigNodes(configNode);
			}
			if (!isReversal)
			{
				var mainEntrySubSenseNode = FindMainEntryGrandChildNode(model, "Senses", "Subsenses");
				DictionaryConfigurationController.ShareNodeAsReference(model.SharedItems, mainEntrySubSenseNode, "mainentrysubsenses");
				var mainEntrySubEntries = GetMainEntryChildNode(model, "Subentries");
				AddSubsubEntriesOptionsIfNeeded(mainEntrySubEntries);
				DictionaryConfigurationController.ShareNodeAsReference(model.SharedItems, mainEntrySubEntries, "mainentrysubentries");
			}
			else
			{
				var reversalSubEntries = GetMainEntryChildNode(model, "Reversal Subentries");
				AddReversalSubsubEntriesIfNeeded(reversalSubEntries);
				DictionaryConfigurationController.ShareNodeAsReference(model.SharedItems, reversalSubEntries, "allreversalsubentries");
			}
			foreach (var configNode in model.Parts)
			{
				RemoveDirectChildrenFromSharedNodes(configNode);
			}
		}

		private void AddSubsubEntriesOptionsIfNeeded(ConfigurableDictionaryNode mainEntrySubEntries)
		{
			if (mainEntrySubEntries.DictionaryNodeOptions == null)
			{
				mainEntrySubEntries.DictionaryNodeOptions = new DictionaryNodeComplexFormOptions
				{
					DisplayEachComplexFormInAParagraph = false,
					ListId = DictionaryNodeListOptions.ListIds.Complex
				};
			}
		}

		private static void AddReversalSubsubEntriesIfNeeded(ConfigurableDictionaryNode reversalSubentriesNode)
		{
			if (reversalSubentriesNode != null && reversalSubentriesNode.Children.Any(n => n.Label == "Reversal Subsubentries"))
				return;
			// Add in the new reversal subsubentries node
			var revsubsubentriesNode = new ConfigurableDictionaryNode
			{
				Label = "Reversal Subsubentries",
				IsEnabled = true,
				Style = "Reversal-Subentry",
				FieldDescription = "SubentriesOS",
				CSSClassNameOverride = "subentries",
				ReferenceItem = "AllReversalSubentries",
				DictionaryNodeOptions = new DictionaryNodeComplexFormOptions {DisplayEachComplexFormInAParagraph = false}
			};
			reversalSubentriesNode.Children.Add(revsubsubentriesNode);
		}

		private static ConfigurableDictionaryNode GetMainEntryChildNode(DictionaryConfigurationModel model, string mainEntryChildLabel)
		{
			ConfigurableDictionaryNode mainEntryChildNode = null;
			var mainEntry = model.Parts.FirstOrDefault();
			if (mainEntry != null)
			{
				mainEntryChildNode = mainEntry.Children.Find(n => n.Label == mainEntryChildLabel && string.IsNullOrEmpty(n.LabelSuffix));
			}
			// If we couldn't find a subsense node this is probably a test that didn't have a full model
			//TODO: log error about not being able to find subentries
			return mainEntryChildNode ?? new ConfigurableDictionaryNode { Children = new List<ConfigurableDictionaryNode>() };
		}

		private static ConfigurableDictionaryNode FindMainEntryGrandChildNode(DictionaryConfigurationModel model, string childLabel, string grandChildLabel)
		{
			ConfigurableDictionaryNode subsenseNode = null;
			var mainEntry = model.Parts.FirstOrDefault();
			if (mainEntry != null)
			{
				var senses = mainEntry.Children.Find(n => n.Label == childLabel && string.IsNullOrEmpty(n.LabelSuffix));
				if (senses != null)
				{
					subsenseNode = senses.Children.Find(n => n.Label == grandChildLabel && string.IsNullOrEmpty(n.LabelSuffix));
				}
			}
			// If we couldn't find a subsense node this is probably a test that didn't have a full model
			//TODO: log error about not being able to find subsenses
			return subsenseNode ?? new ConfigurableDictionaryNode { Children = new List<ConfigurableDictionaryNode>() };
		}

		private static void SetReferenceNodeInConfigNodes(ConfigurableDictionaryNode configNode)
		{
			if (configNode.Label == "Subentries")
			{
				configNode.ReferenceItem = "MainEntrySubentries";
				if (configNode.DictionaryNodeOptions == null)
				{
					configNode.DictionaryNodeOptions = new DictionaryNodeComplexFormOptions
					{
						ListId = DictionaryNodeListOptions.ListIds.Complex
					};
				}
			}
			else if (configNode.Label == "Subsenses" || configNode.Label == "Subsubsenses")
			{
				configNode.ReferenceItem = "MainEntrySubsenses";
			}
			else if (configNode.Label == "Reversal Subentries")
			{
				configNode.ReferenceItem = "AllReversalSubentries"; if (configNode.DictionaryNodeOptions == null)
				{
					configNode.DictionaryNodeOptions = new DictionaryNodeComplexFormOptions
					{
						DisplayEachComplexFormInAParagraph = false
					};
				}
			}
			if (configNode.Children == null)
				return;
			foreach (var child in configNode.Children)
			{
				SetReferenceNodeInConfigNodes(child);
			}
		}

		private void RemoveDirectChildrenFromSharedNodes(ConfigurableDictionaryNode configNode)
		{
			if (configNode.Children == null)
			{
				return;
			}
			foreach (var child in configNode.Children)
			{
				RemoveDirectChildrenFromSharedNodes(child);
			}
			if (!string.IsNullOrEmpty(configNode.ReferenceItem))
				configNode.Children = null;
		}

		private static void HandleCssClassChanges(List<ConfigurableDictionaryNode> parts, int version)
		{
			foreach (var node in parts)
			{
				switch (version)
				{
					case 4:
						ReplaceTranslationsCssClass(node, n => n.FieldDescription == "TranslationsOC", "translationcontents");
						break;
				}
			}
		}

		private static void ReplaceTranslationsCssClass(ConfigurableDictionaryNode node, Func<ConfigurableDictionaryNode, bool> match, string cssClass)
		{
			if (match(node))
			{
				node.CSSClassNameOverride = cssClass;
			}
			if (node.Children == null)
				return;
			foreach (var child in node.Children)
			{
				ReplaceTranslationsCssClass(child, match, cssClass);
			}
		}

		private static void HandleOptionsChanges(List<ConfigurableDictionaryNode> parts, int version)
		{
			foreach (var node in parts)
			{
				switch (version)
				{
					case 4:
						SetOptionsInExamplesNodes(node, n => n.FieldDescription == "ExamplesOS");
						break;
				}
			}
		}

		private static void SetOptionsInExamplesNodes(ConfigurableDictionaryNode node, Func<ConfigurableDictionaryNode, bool> match)
		{
			if (match(node))
			{
				node.StyleType = ConfigurableDictionaryNode.StyleTypes.Paragraph;
				node.Style = "Bulleted List";
				DictionaryNodeOptions options = new DictionaryNodeComplexFormOptions { DisplayEachComplexFormInAParagraph = true };
				node.DictionaryNodeOptions = options;
			}
			if (node.Children == null)
				return;
			foreach (var child in node.Children)
			{
				SetOptionsInExamplesNodes(child, match);
			}
		}

		private static void HandleLabelChanges(List<ConfigurableDictionaryNode> parts, int version)
		{
			// If you add to this method (i.e. we have later version label changes), don't forget to
			// also modify HandleChildNodeRenaming() in PreHistoricMigrator.
			foreach (var node in parts)
			{
				switch (version)
				{
					case 3:
						ReplaceLabelInChildren(node, n => n.FieldDescription == "ExamplesOS", n => n.FieldDescription == "Example", "Example Sentence");
						ReplaceLabelInChildren(node, n => n.FieldDescription == "ReferringSenses", n => n.FieldDescription == "Owner" && n.SubField == "Bibliography", "Bibliography (Entry)");
						ReplaceLabelInChildren(node, n => n.FieldDescription == "ReferringSenses", n => n.FieldDescription == "Bibliography", "Bibliography (Sense)");
						break;
				}
			}
		}

		private static void ReplaceLabelInChildren(ConfigurableDictionaryNode node, Func<ConfigurableDictionaryNode, bool> parentMatch, Func<ConfigurableDictionaryNode, bool> childMatch, string newLabelValue)
		{
			if (node.Children == null)
				return;
			if (parentMatch(node))
			{
				foreach (var replaceChild in node.Children.Where(childMatch))
				{
					replaceChild.Label = newLabelValue;
				}
			}
			foreach (var child in node.Children)
			{
				ReplaceLabelInChildren(child, parentMatch, childMatch, newLabelValue);
			}
		}

		private static void HandleFieldChanges(List<ConfigurableDictionaryNode> parts, int version, bool isReversal)
		{
			var wsOptions = new DictionaryNodeWritingSystemOptions
			{
				WsType = DictionaryNodeWritingSystemOptions.WritingSystemType.Vernacular,
				Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>
				{
					new DictionaryNodeListOptions.DictionaryNodeOption {Id = "vernacular", IsEnabled = true }
				}
			};
			foreach (var node in parts)
			{
				switch (version)
				{
					case 2:
						var newHeadword = isReversal ? "ReversalName" : "HeadWordRef";
						ReplaceFieldInNodesAndOptionallyEnsureOptions(node, n => n.Label == "Referenced Headword", newHeadword, isReversal ? wsOptions : null);
						ReplaceSubFieldInNodes(node, n => n.FieldDescription == "OwningEntry" && n.SubField == "MLHeadWord", newHeadword);
						break;
					case 3:
						ReplaceFieldInNodesAndOptionallyEnsureOptions(node, n => n.Label == "Gloss (or Summary Definition)", "GlossOrSummary");
						break;
				}
			}
		}

		private static void ReplaceSubFieldInNodes(ConfigurableDictionaryNode node, Func<ConfigurableDictionaryNode, bool> match, string newSubFieldValue)
		{
			if (match(node))
			{
				node.SubField = newSubFieldValue;
			}
			if (node.Children == null)
				return;
			foreach (var child in node.Children)
			{
				ReplaceSubFieldInNodes(child, match, newSubFieldValue);
			}
		}

		private static void ReplaceFieldInNodesAndOptionallyEnsureOptions(ConfigurableDictionaryNode node, Func<ConfigurableDictionaryNode, bool> match, string newFieldValue, DictionaryNodeOptions childOptions = null)
		{
			if (match(node))
			{
				node.FieldDescription = newFieldValue;
				if (childOptions != null && node.DictionaryNodeOptions == null)
					node.DictionaryNodeOptions = childOptions.DeepClone();
			}
			if (node.Children == null)
				return;
			foreach (var child in node.Children)
			{
				ReplaceFieldInNodesAndOptionallyEnsureOptions(child, match, newFieldValue, childOptions);
			}
		}

		private static void RemoveReferencedItems(List<ConfigurableDictionaryNode> nodes)
		{
			foreach (var node in nodes)
			{
				node.ReferenceItem = null; // For now, assume they're all bad (they were in version 1)
				if (node.Children != null)
					RemoveReferencedItems(node.Children);
			}
		}

		private static void ExtractWritingSystemOptionsFromReferringSenseOptions(List<ConfigurableDictionaryNode> nodes)
		{
			foreach (var node in nodes)
			{
				var rsOpts = node.DictionaryNodeOptions as DictionaryNodeReferringSenseOptions;
				if (rsOpts != null)
					node.DictionaryNodeOptions = rsOpts.WritingSystemOptions;
				if (node.Children != null)
					ExtractWritingSystemOptionsFromReferringSenseOptions(node.Children);
			}
		}
	}
}