// Copyright (c) 2016-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using DCM = LanguageExplorer.Works.DictionaryConfigurationMigrator;

namespace LanguageExplorer.Works.DictionaryConfigurationMigrators
{
	/// <summary>
	/// This file will migrate all the configurations produced during the first 8.3 alpha
	/// </summary>
	public class FirstAlphaMigrator : IDictionaryConfigurationMigrator
	{
		private SimpleLogger m_logger;
		internal const int VersionAlpha2 = 5;
		internal const int VersionAlpha3 = 8;

		public FirstAlphaMigrator() : this(null, null)
		{
		}

		public FirstAlphaMigrator(LcmCache cache, SimpleLogger logger)
		{
			Cache = cache;
			m_logger = logger;
		}

		private LcmCache Cache { get; set; }

		public void MigrateIfNeeded(SimpleLogger logger, IPropertyTable propertyTable, string appVersion)
		{
			m_logger = logger;
			Cache = propertyTable.GetValue<LcmCache>("cache");
			var foundOne = $"{appVersion}: Configuration was found in need of migration. - {DateTime.Now:yyyy MMM d h:mm:ss}";
			foreach (var config in DCM.GetConfigsNeedingMigration(Cache, VersionAlpha3))
			{
				m_logger.WriteLine(foundOne);
				m_logger.WriteLine($"Migrating {config.Type} configuration '{config.Label}' from version {config.Version} to {VersionAlpha3}.");
				m_logger.IncreaseIndent();
				MigrateFrom83Alpha(config);
				config.Save();
				m_logger.DecreaseIndent();
			}
		}

		internal void MigrateFrom83Alpha(DictionaryConfigurationModel alphaModel)
		{
			// original migration neglected to update the version number; -1 (Pre83) is the same as 1 (Alpha1)
			if (alphaModel.Version == PreHistoricMigrator.VersionPre83 || alphaModel.Version == PreHistoricMigrator.VersionAlpha1)
				RemoveNonLoadableData(alphaModel.PartsAndSharedItems);
			// now that it's safe to specify them, it would be helpful to have parents in certain steps:
			DictionaryConfigurationModel.SpecifyParentsAndReferences(alphaModel.Parts, alphaModel.SharedItems);
			switch (alphaModel.Version)
			{
				case -1:
				case 1:
				case 2:
					HandleNodewiseChanges(alphaModel.PartsAndSharedItems, m_logger, 2, alphaModel.IsReversal);
					goto case 3;
				case 3:
					HandleNodewiseChanges(alphaModel.PartsAndSharedItems, m_logger, 3, alphaModel.IsReversal);
					DCM.SetWritingSystemForReversalModel(alphaModel, Cache);
					AddSharedNodesToAlphaConfigurations(alphaModel);
					goto case 4;
				case 4:
					HandleNodewiseChanges(alphaModel.PartsAndSharedItems, m_logger, 4, alphaModel.IsReversal);
					goto case VersionAlpha2;
				case VersionAlpha2:
					HandleNodewiseChanges(alphaModel.PartsAndSharedItems, m_logger, VersionAlpha2, alphaModel.IsReversal);
					goto case 6;
				case 6:
					HandleNodewiseChanges(alphaModel.PartsAndSharedItems, m_logger, 6, alphaModel.IsReversal);
					goto case 7;
				case 7:
					var fileName = Path.GetFileNameWithoutExtension(alphaModel.FilePath);
					if (!alphaModel.IsRootBased)
						alphaModel.IsRootBased = fileName == DCM.RootFileName;
					break;
				default:
					m_logger.WriteLine(string.Format(
						"Unable to migrate {0}: no migration instructions for version {1}", alphaModel.Label, alphaModel.Version));
					break;
			}
			alphaModel.Version = VersionAlpha3;
		}

		private static void RemoveNonLoadableData(IEnumerable<ConfigurableDictionaryNode> nodes)
		{
			DCM.PerformActionOnNodes(nodes, node =>
			{
				node.ReferenceItem = null;
				var rsOptions = node.DictionaryNodeOptions as DictionaryNodeReferringSenseOptions;
				if (rsOptions != null)
					node.DictionaryNodeOptions = rsOptions.WritingSystemOptions;
			});
		}

		private void AddSharedNodesToAlphaConfigurations(DictionaryConfigurationModel model)
		{
			if (model.SharedItems.Any())
			{
				m_logger.WriteLine("Not adding shared nodes because some already exist:");
				m_logger.IncreaseIndent();
				model.SharedItems.ForEach(si => m_logger.WriteLine(DCM.BuildPathStringFromNode(si)));
				m_logger.DecreaseIndent();
				return;
			}
			DCM.PerformActionOnNodes(model.Parts, SetReferenceItem);
			if (model.IsReversal)
			{
				var reversalSubEntries = FindMainEntryDescendant(model, "SubentriesOS");
				AddSubsubNodeIfNeeded(reversalSubEntries);
				DictionaryConfigurationController.ShareNodeAsReference(model.SharedItems, reversalSubEntries, "allreversalsubentries");
			}
			else // is Configured Dictionary
			{
				var mainEntrySubSenseNode = FindMainEntryDescendant(model, "SensesOS", "SensesOS");
				AddSubsubNodeIfNeeded(mainEntrySubSenseNode);
				DictionaryConfigurationController.ShareNodeAsReference(model.SharedItems, mainEntrySubSenseNode, "mainentrysubsenses");
				var mainEntrySubEntries = FindMainEntryDescendant(model, "Subentries");
				AddSubsubEntriesOptionsIfNeeded(mainEntrySubEntries);
				DictionaryConfigurationController.ShareNodeAsReference(model.SharedItems, mainEntrySubEntries, "mainentrysubentries");
			}
			// Remove direct children from nodes with referenced children
			DCM.PerformActionOnNodes(model.PartsAndSharedItems, n => { if (!string.IsNullOrEmpty(n.ReferenceItem)) n.Children = null; });
		}

		private static void AddSubsubEntriesOptionsIfNeeded(ConfigurableDictionaryNode mainEntrySubEntries)
		{
			if (mainEntrySubEntries.DictionaryNodeOptions == null)
			{
				mainEntrySubEntries.DictionaryNodeOptions = new DictionaryNodeListAndParaOptions
				{
					ListId = DictionaryNodeListOptions.ListIds.Complex
				};
			}
		}

		private static void AddSubsubNodeIfNeeded(ConfigurableDictionaryNode subNode)
		{
			if (subNode.Children.Any(n => n.FieldDescription == subNode.FieldDescription))
				return;
			switch (subNode.FieldDescription)
			{
				case "SensesOS":
					// On the odd chance Subsense has no SenseOptions, construct some.
					subNode.DictionaryNodeOptions = subNode.DictionaryNodeOptions ?? new DictionaryNodeSenseOptions();
					// Add in the new subsubsenses node
					subNode.Children.Add(new ConfigurableDictionaryNode
					{
						Label = "Subsubsenses",
						IsEnabled = true,
						Style = "Dictionary-Sense",
						FieldDescription = "SensesOS",
						CSSClassNameOverride = "subsenses",
						ReferenceItem = "MainEntrySubsenses",
						DictionaryNodeOptions = subNode.DictionaryNodeOptions.DeepClone()
					});
					break;
				case "SubentriesOS": // SubentriesOS uniquely identifies Reversal Index subentries
					// Add in the new reversal subsubentries node
					subNode.Children.Add(new ConfigurableDictionaryNode
					{
						Label = "Reversal Subsubentries",
						IsEnabled = true,
						Style = "Reversal-Subentry",
						FieldDescription = "SubentriesOS",
						CSSClassNameOverride = "subentries",
						ReferenceItem = "AllReversalSubentries"
					});
					break;
			}
		}

		/// <param name="model"></param>
		/// <param name="ancestors">Fields in the desired node's ancestry, included the desired node's field, but not including Main Entry</param>
		/// <remarks>Currently ignores nodes' subfields</remarks>
		private ConfigurableDictionaryNode FindMainEntryDescendant(DictionaryConfigurationModel model, params string[] ancestors)
		{
			var failureMessage = "Unable to find 'Main Entry";
			var nextNode = model.Parts.FirstOrDefault();
			foreach (var ancestor in ancestors)
			{
				if (nextNode == null)
					break;
				failureMessage += DCM.NodePathSeparator + ancestor;
				nextNode = nextNode.Children.Find(n => n.FieldDescription == ancestor && string.IsNullOrEmpty(n.LabelSuffix));
			}
			if (nextNode != null)
				return nextNode;
			// If we couldn't find the node, this is probably a test that didn't have a full model
			m_logger.WriteLine(string.Format("Unable to find '{0}'", string.Join(DCM.NodePathSeparator, new[] { "Main Entry" }.Concat(ancestors))));
			m_logger.WriteLine(failureMessage + "'");
			return new ConfigurableDictionaryNode { Children = new List<ConfigurableDictionaryNode>() };
		}

		private static void SetReferenceItem(ConfigurableDictionaryNode configNode)
		{
			switch (configNode.FieldDescription)
			{
				case "Subentries":
					configNode.ReferenceItem = "MainEntrySubentries";
					if (configNode.DictionaryNodeOptions == null)
					{
						configNode.DictionaryNodeOptions = new DictionaryNodeListAndParaOptions
						{
							ListId = DictionaryNodeListOptions.ListIds.Complex
						};
					}
					break;
				case "SensesOS":
					if (configNode.Parent.FieldDescription == "SensesOS") // update only subsenses
						configNode.ReferenceItem = "MainEntrySubsenses";
					break;
				case "SubentriesOS": // uniquely identifies Reversal Index Subentries
					configNode.ReferenceItem = "AllReversalSubentries";
					break;
			}
		}

		/// <remarks>
		/// Handles various kinds of configuration node changes that happened post-8.3Alpha1.
		/// It should no longer be necessary to add changes in two places
		/// [except: see caveat on PreHistoricMigrator.HandleChildNodeRenaming()]
		/// </remarks>
		private static void HandleNodewiseChanges(IEnumerable<ConfigurableDictionaryNode> nodes, ISimpleLogger logger, int version, bool isReversal)
		{
			var newHeadword = isReversal ? "ReversalName" : "HeadWordRef";
			switch (version)
			{
				case 2:
					DCM.PerformActionOnNodes(nodes, n =>
					{
						if (n.FieldDescription == "OwningEntry" && n.SubField == "MLHeadWord")
							n.SubField = newHeadword;
					});
					break;
				case 3:
					DCM.PerformActionOnNodes(nodes, n =>
					{
						if (n.Label == "Gloss (or Summary Definition)")
							n.FieldDescription = "GlossOrSummary";
						if (n.Parent == null)
							return;
						if (n.Parent.FieldDescription == "ExamplesOS" && n.FieldDescription == "Example")
							n.Label = "Example Sentence";
						else if (n.Parent.FieldDescription == "ReferringSenses")
						{
							if (n.FieldDescription == "Owner" && n.SubField == "Bibliography")
								n.Label = "Bibliography (Entry)";
							else if (n.FieldDescription == "Bibliography")
								n.Label = "Bibliography (Sense)";
						}
					});
					break;
				case 4:
					DCM.PerformActionOnNodes(nodes, n =>
					{
						switch (n.FieldDescription)
						{
							case "TranslationsOC":
								n.CSSClassNameOverride = "translationcontents";
								break;
							case "ExamplesOS":
								n.CSSClassNameOverride = "examplescontents";
								n.StyleType = ConfigurableDictionaryNode.StyleTypes.Character;
								n.DictionaryNodeOptions = new DictionaryNodeListAndParaOptions(); // allow to be shown in paragraph
								break;
						}
					});
					break;
				case VersionAlpha2:
					DCM.PerformActionOnNodes(nodes, n =>
					{
						if (n.FieldDescription == null)
						{
							logger.WriteLine(string.Format("Warning: '{0}' reached the Alpha2 migration with a null FieldDescription.", DCM.BuildPathStringFromNode(n)));
							return;
						}
						if (n.FieldDescription == "VisibleVariantEntryRefs" && n.Label == "Variant Of")
							n.Label = "Variant of";
						else if (n.FieldDescription.Contains("EntryType"))
						{
							var parentFd = n.Parent.FieldDescription;
							if (n.FieldDescription == ConfiguredXHTMLGenerator.LookupComplexEntryType ||
								isReversal && (n.FieldDescription == "VariantEntryTypesRS" || n.FieldDescription == "ComplexEntryTypesRS"))
							{
								if (parentFd == "ComplexFormEntryRefs")
								{
									// set type children to RevAbbr/RevName
									SetEntryTypeChildrenBackward(n);
								}
								else
								{
									// set type children to Abbr/Name
									SetEntryTypeChildrenForward(n);
								}
							}
							else if (parentFd.EndsWith("BackRefs") || parentFd == "ComplexFormsNotSubentries")
							{
								// set type children to Abbr/Name
								SetEntryTypeChildrenForward(n);
							}
							else
							{
								// set type children to RevAbbr/RevName
								SetEntryTypeChildrenBackward(n);
							}
						}
						else if ((n.Label == "Headword" && n.Parent.FieldDescription == "ReferringSenses") ||
							(n.Label == "Form" && n.Parent.Label.StartsWith("Subentry Under")))
						{
							n.Label = "Referenced Headword";
						}
						else if (n.Label == "Subsenses" && n.Parent.FieldDescription == "SensesOS")
						{
							var senseNode = (DictionaryNodeSenseOptions)n.DictionaryNodeOptions;
							if (senseNode != null)
							{
								senseNode.ParentSenseNumberingStyle = "%.";
								senseNode.NumberingStyle = "%d";
								n.DictionaryNodeOptions = senseNode;
							}
						}
						else if (n.Label == "Allomorphs" && n.FieldDescription == "Owner")
						{
							n.FieldDescription = "Entry";
						}
						if (n.Label == "Referenced Headword")
						{
							n.FieldDescription = newHeadword;
							if (isReversal && n.DictionaryNodeOptions == null)
								n.DictionaryNodeOptions = new DictionaryNodeWritingSystemOptions
								{
									WsType = DictionaryNodeWritingSystemOptions.WritingSystemType.Vernacular,
									Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>
									{
										new DictionaryNodeListOptions.DictionaryNodeOption { Id = "vernacular", IsEnabled = true }
									}
								};
						}
					});
					break;
				case 6:
					DCM.PerformActionOnNodes(nodes, n =>
					{
						if (isReversal && n.Label == "Pronunciation" && n.Parent.Label == "Pronunciations")
						{
							var parent = n.Parent;
							parent.Before = "[";
							parent.Between = " ";
							parent.After = "] ";
							n.Before = "";
							n.Between = "";
							n.After = " ";
						}
						UpdatePicturesChildren(n);
					});
					break;
			}
		}

		/// <summary>
		/// If node is a Pictures node, update its child nodes by removing Sense Number and adding Headword and Gloss.
		/// Part of LT-12572.
		/// </summary>
		private static void UpdatePicturesChildren(ConfigurableDictionaryNode node)
		{
			if (node == null)
				return;
			if (node.Label != "Pictures")
				return;

			node.Children.RemoveAll(child => child.Label == "Sense Number");

			var analysisWsOptions = new DictionaryNodeWritingSystemOptions
				{
					WsType = DictionaryNodeWritingSystemOptions.WritingSystemType.Analysis,
					DisplayWritingSystemAbbreviations = false,
					Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>
						{
							new DictionaryNodeListOptions.DictionaryNodeOption {Id = "analysis", IsEnabled = true}
						}
				};

			var vernacularWsOptions = new DictionaryNodeWritingSystemOptions
				{
					WsType = DictionaryNodeWritingSystemOptions.WritingSystemType.Vernacular,
					DisplayWritingSystemAbbreviations = false,
					Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>
						{
							new DictionaryNodeListOptions.DictionaryNodeOption {Id = "vernacular", IsEnabled = true}
						}
				};

			var headwordNode = new ConfigurableDictionaryNode
				{
					After = "  ", Between = " ", Label = "Headword", FieldDescription = "Owner",
					SubField="OwnerOutlineName", CSSClassNameOverride="headword",
					Style="Dictionary-Headword",
					IsEnabled = true, DictionaryNodeOptions = vernacularWsOptions
				};

			var glossNode = new ConfigurableDictionaryNode
				{
					After = " ", Between = " ", Label = "Gloss", FieldDescription = "Owner",
					SubField="Gloss",
					IsEnabled = true, DictionaryNodeOptions = analysisWsOptions
				};

			node.Children.Add(headwordNode);
			node.Children.Add(glossNode);
		}

		/// <summary>
		/// Swap out node Label and FieldDescription, checks for null node and empty strings
		/// in case only one of the parameters needs changing.
		/// </summary>
		private static void SwapOutNodeLabelAndField(ConfigurableDictionaryNode node, string label, string fieldDescription)
		{
			if (node == null)
				return;
			if (!string.IsNullOrEmpty(label))
				node.Label = label;
			if (!string.IsNullOrEmpty(fieldDescription))
				node.FieldDescription = fieldDescription;
		}

		private const string Abbr = "Abbreviation"; // good for label and field
		private const string Name = "Name"; // good for label and field
		private const string ReversePrefix = "Reverse "; // for reverse labels
		private const string RevAbbr = "ReverseAbbr";
		private const string RevName = "ReverseName";

		/// <summary>
		/// Makes sure EntryType node contains Abbreviation and Name nodes
		/// </summary>
		private static void SetEntryTypeChildrenForward(ConfigurableDictionaryNode entryTypeNode)
		{
			var abbrNode = entryTypeNode.Children.Find(node => node.FieldDescription == RevAbbr);
			SwapOutNodeLabelAndField(abbrNode, Abbr, Abbr);
			var nameNode = entryTypeNode.Children.Find(node => node.FieldDescription == RevName);
			SwapOutNodeLabelAndField(nameNode, Name, Name);
		}

		/// <summary>
		/// Makes sure EntryType node contains ReverseAbbr and ReverseName nodes
		/// </summary>
		private static void SetEntryTypeChildrenBackward(ConfigurableDictionaryNode entryTypeNode)
		{
			var abbrNode = entryTypeNode.Children.Find(node => node.FieldDescription == Abbr);
			SwapOutNodeLabelAndField(abbrNode, ReversePrefix + Abbr, RevAbbr);
			var nameNode = entryTypeNode.Children.Find(node => node.FieldDescription == Name);
			SwapOutNodeLabelAndField(nameNode, ReversePrefix + Name, RevName);
		}
	}
}