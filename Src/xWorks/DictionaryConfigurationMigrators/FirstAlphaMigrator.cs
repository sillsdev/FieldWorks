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
					HandleFieldChanges(allParts, 2, !string.IsNullOrEmpty(alphaModel.WritingSystem));
					HandleFieldChanges(allParts, 3, false);
					goto case 3;
				case 3:
					HandleLabelChanges(allParts, 3);
					HandleFieldChanges(allParts, 3, false);
					DictionaryConfigurationMigrator.SetWritingSystemForReversalModel(alphaModel, Cache);
					goto case 4;
				case 4:
					HandleOptionsChanges(allParts, 4);
					break;
			}
			alphaModel.Version = DictionaryConfigurationMigrator.VersionCurrent;
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
			foreach (var node in parts)
			{
				switch (version)
				{
					case 2:
						var newHeadword = isReversal ? "ReversalName" : "HeadWordRef";
						ReplaceFieldInNodes(node, n => n.Label == "Referenced Headword", newHeadword);
						ReplaceSubFieldInNodes(node, n => n.FieldDescription == "OwningEntry" && n.SubField == "MLHeadWord", newHeadword);
						break;
					case 3:
						ReplaceFieldInNodes(node, n => n.Label == "Gloss (or Summary Definition)", "GlossOrSummary");
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

		private static void ReplaceFieldInNodes(ConfigurableDictionaryNode node, Func<ConfigurableDictionaryNode, bool> match, string newFieldValue)
		{
			if (match(node))
			{
				node.FieldDescription = newFieldValue;
			}
			if (node.Children == null)
				return;
			foreach (var child in node.Children)
			{
				ReplaceFieldInNodes(child, match, newFieldValue);
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