// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.Utils;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// A selection of dictionary elements and options, for configuring a dictionary publication.
	/// </summary>
	[XmlRoot(ElementName = "DictionaryConfiguration")]
	public class DictionaryConfigurationModel
	{
		/// <summary>
		/// File extension for dictionary configuration files.
		/// </summary>
		public const string FileExtension = ".fwdictconfig";

		public const string AllReversalIndexes = "All Reversal Indexes";

		/// <summary>
		/// Filename (without extension) of the reversal index configuration file
		/// for "all reversal indexes".
		/// </summary>
		public const string AllReversalIndexesFilenameBase = "AllReversalIndexes";

		/// <summary>
		/// Trees of dictionary elements
		/// </summary>
		[XmlElement(ElementName = "ConfigurationItem")]
		public List<ConfigurableDictionaryNode> Parts { get; set; }

		/// <summary>
		/// Trees of shared dictionary elements
		/// </summary>
		[XmlArray(ElementName = "SharedItems")]
		[XmlArrayItem(ElementName = "ConfigurationItem")]
		public List<ConfigurableDictionaryNode> SharedItems { get; set; }

		/// <summary>
		/// Name of this dictionary configuration. eg "Stem-based"
		/// </summary>
		[XmlAttribute(AttributeName = "name")]
		public string Label { get; set; }

		/// <summary>
		/// The version of the DictionaryConfigurationModel for use in data migration etc.
		/// </summary>
		[XmlAttribute(AttributeName = "version")]
		public int Version { get; set; }

		[XmlAttribute(AttributeName = "lastModified", DataType = "date")]
		public DateTime LastModified { get; set; }

		/// <summary>
		/// Publications for which this view applies. <seealso cref="AllPublications"/>
		/// </summary>
		[XmlArray(ElementName = "Publications")]
		[XmlArrayItem(ElementName = "Publication")]
		public List<string> Publications { get; set; }

		/// <summary>
		/// Whether all current and future publications should be used by this configuration.
		/// </summary>
		[XmlAttribute(AttributeName = "allPublications")]
		public bool AllPublications { get; set; }

		/// <summary>
		/// The writing system of the configuration.
		/// </summary>
		[XmlAttribute(AttributeName = "writingSystem")]
		public string WritingSystem { get; set; }

		/// <summary>
		/// File where data is stored
		/// </summary>
		[XmlIgnore]
		public string FilePath { get; set; }

		/// <summary></summary>
		public void Save()
		{
			LastModified = DateTime.Now;
			var serializer = new XmlSerializer(typeof(DictionaryConfigurationModel));
			var settings = new XmlWriterSettings { Indent = true };
			using(var writer = XmlWriter.Create(FilePath, settings))
			{
				serializer.Serialize(writer, this);
			}
		}

		/// <summary>
		/// Loads the model. If Cache is not null, also connects parents and references, and updates lists from the rest of the FieldWorks model.
		/// </summary>
		public void Load(FdoCache cache)
		{
			var serializer = new XmlSerializer(typeof(DictionaryConfigurationModel));
			using(var reader = XmlReader.Create(FilePath))
			{
				var model = (DictionaryConfigurationModel)serializer.Deserialize(reader);
				model.FilePath = FilePath; // this doesn't get [de]serialized
				foreach (var property in typeof(DictionaryConfigurationModel).GetProperties().Where(prop => prop.CanWrite))
					property.SetValue(this, property.GetValue(model, null), null);
			}
			if (cache == null)
				return;

			SpecifyParentsAndReferences(Parts);
			SpecifyParentsAndReferences(SharedItems); // REVIEW (Hasso) 2016.04: do we want to have to call Specify twice?
			Publications = AllPublications ? GetAllPublications(cache) : FilterRealPublications(cache);
			// Handle any changes to the custom field definitions.  (See https://jira.sil.org/browse/LT-16430.)
			// The "Merge" method handles both additions and deletions.
			DictionaryConfigurationController.MergeCustomFieldsIntoDictionaryModel(cache, this);
			// Handle changes to the lists of complex form types and variant types.
			MergeTypesIntoDictionaryModel(cache, this);
			// Handle any deleted styles.  (See https://jira.sil.org/browse/LT-16501.)
			EnsureValidStylesInModel(cache);
		}

		public static void MergeTypesIntoDictionaryModel(FdoCache cache, DictionaryConfigurationModel model)
		{
			var complexTypes = new Set<Guid>();
			foreach (var pos in cache.LangProject.LexDbOA.ComplexEntryTypesOA.ReallyReallyAllPossibilities)
				complexTypes.Add(pos.Guid);
			complexTypes.Add(Common.Controls.XmlViewsUtils.GetGuidForUnspecifiedComplexFormType());
			var variantTypes = new Set<Guid>();
			foreach (var pos in cache.LangProject.LexDbOA.VariantEntryTypesOA.ReallyReallyAllPossibilities)
				variantTypes.Add(pos.Guid);
			variantTypes.Add(Common.Controls.XmlViewsUtils.GetGuidForUnspecifiedVariantType());
			var referenceTypes = new Set<Guid>();
			if (cache.LangProject.LexDbOA.ReferencesOA != null)
			{
				foreach (var pos in cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS)
					referenceTypes.Add(pos.Guid);
			}
			foreach (var part in model.Parts)
			{
				FixTypeListOnNode(part, complexTypes, variantTypes, referenceTypes);
			}
		}

		private static void FixTypeListOnNode(ConfigurableDictionaryNode node, Set<Guid> complexTypes, Set<Guid> variantTypes, Set<Guid> referenceTypes)
		{
			if (node.DictionaryNodeOptions is DictionaryNodeListOptions)
			{
				var listOptions = (DictionaryNodeListOptions)node.DictionaryNodeOptions;
				switch (listOptions.ListId)
				{
					case DictionaryNodeListOptions.ListIds.Complex:
					{
						FixOptionsAccordingToCurrentTypes(listOptions.Options, complexTypes);
						break;
					}
					case DictionaryNodeListOptions.ListIds.Variant:
					{
						FixOptionsAccordingToCurrentTypes(listOptions.Options, variantTypes);
						break;
					}
					case DictionaryNodeListOptions.ListIds.Entry:
					{
						FixOptionsAccordingToCurrentTypes(listOptions.Options, referenceTypes);
						break;
					}
					case DictionaryNodeListOptions.ListIds.Sense:
					{
						FixOptionsAccordingToCurrentTypes(listOptions.Options, referenceTypes);
						break;
					}
					case DictionaryNodeListOptions.ListIds.Minor:
					{
						var complexAndVariant = complexTypes.Union(variantTypes);
						FixOptionsAccordingToCurrentTypes(listOptions.Options, complexAndVariant);
						break;
					}
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
			foreach (var type in possibilities)
			{
				if (!currentGuids.Contains(type))
					options.Add(new DictionaryNodeListOptions.DictionaryNodeOption { Id = type.ToString(), IsEnabled = true });
			}
			// remove options that no longer exist
			for (var i = options.Count - 1; i >= 0; --i)
			{
				Guid guid;
				if (Guid.TryParse(options[i].Id, out guid) && !possibilities.Contains(guid))
					options.RemoveAt(i);
			}
		}

		public void EnsureValidStylesInModel(FdoCache cache)
		{
			var styles = cache.LangProject.StylesOC.ToDictionary(style => style.Name);
			foreach (var part in Parts)
			{
				if (IsMainEntry(part) && string.IsNullOrEmpty(part.Style))
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

		private List<string> FilterRealPublications(FdoCache cache)
		{
			if (Publications == null || !Publications.Any())
				return new List<string>();
			var allPossibilities = cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS.ToList();
			var allPossiblePublicationsInAllWs = new HashSet<string>();
			foreach (var possibility in allPossibilities)
				foreach (var ws in cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems.Handles())
					allPossiblePublicationsInAllWs.Add(possibility.Name.get_String(ws).Text);
			return Publications.Where(allPossiblePublicationsInAllWs.Contains).ToList();
		}

		/// <summary>
		/// Default constructor for easier testing.
		/// </summary>
		internal DictionaryConfigurationModel() { SharedItems = new List<ConfigurableDictionaryNode>(); }

		/// <summary>Loads a DictionaryConfigurationModel from the given path</summary>
		public DictionaryConfigurationModel(string path, FdoCache cache)
		{
			FilePath = path;
			Load(cache);
		}

		/// <summary>Returns a deep clone of this DCM. Caller is responsible to choose a unique FilePath</summary>
		public DictionaryConfigurationModel DeepClone()
		{
			var clone = new DictionaryConfigurationModel();

			// Copy everything over at first, importantly handling strings and primitives.
			var properties = typeof(DictionaryConfigurationModel).GetProperties();
			foreach (var property in properties.Where(prop => prop.CanWrite)) // Skip any read-only properties
			{
				var originalValue = property.GetValue(this, null);
				property.SetValue(clone, originalValue, null);
			}

			// Deep-clone Parts
			if (Parts != null)
			{
				clone.Parts = Parts.Select(node => node.DeepCloneUnderSameParent()).ToList();
			}

			// Deep-clone SharedItems
			if (SharedItems != null)
			{
				clone.SharedItems = SharedItems.Select(node => node.DeepCloneUnderSameParent()).ToList();
			}

			// Clone Publications
			if (Publications != null)
			{
				clone.Publications = new List<string>(Publications);
			}

			return clone;
		}

		/// <summary>
		/// Assign Parent properties to descendants of nodes
		/// </summary>
		internal void SpecifyParentsAndReferences(List<ConfigurableDictionaryNode> nodes)
		{
			if (nodes == null)
				throw new ArgumentNullException();

			var rollingNodes = new List<ConfigurableDictionaryNode>(nodes);

			while (rollingNodes.Any())
			{
				var node = rollingNodes[0];
				rollingNodes.RemoveAt(0);
				if (!string.IsNullOrEmpty(node.ReferenceItem))
					LinkReferencedNode(node, node.ReferenceItem);
				if (node.Children == null)
					continue;
				foreach (var child in node.Children)
					child.Parent = node;
				rollingNodes.AddRange(node.Children);
			}
		}

		public bool LinkReferencedNode(ConfigurableDictionaryNode node, string referenceItem)
		{
			node.ReferencedNode = SharedItems.FirstOrDefault(si =>
				si.Label == referenceItem && si.FieldDescription == node.FieldDescription && si.SubField == node.SubField);
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
		public void ShareNodeAsReference(ConfigurableDictionaryNode node, string cssClass = null)
		{
			SharedItems = SharedItems ?? new List<ConfigurableDictionaryNode>();
			if (node.ReferencedNode != null)
				throw new InvalidOperationException(string.Format("Node {0} is already shared as {1}",
					DictionaryConfigurationMigrator.BuildPathStringFromNode(node), node.ReferenceItem ?? node.ReferencedNode.Label));
			if (node.Children == null || !node.Children.Any())
				return; // no point sharing Children there aren't any
			var dupItem = SharedItems.FirstOrDefault(item => item.FieldDescription == node.FieldDescription && item.SubField == node.SubField);
			if (dupItem != null)
			{
				var fullField = string.IsNullOrEmpty(node.SubField)
					? node.FieldDescription
					: string.Format("{0}.{1}", node.FieldDescription, node.SubField);
				// TODO pH 2016.04: replace this with MessageBox.Show when this method moves out of the model
				Console.WriteLine(string.Format("Inadvisable to share {0} because a shared node with the same field ({1}) already exists ({2})",
					node.DisplayLabel, fullField, DictionaryConfigurationMigrator.BuildPathStringFromNode(dupItem.Parent)));
				return;
			}

			// ENHANCE (Hasso) 2016.03: enforce that the specified node is part of *this* model (incl shared items)
			var key = string.IsNullOrEmpty(node.ReferenceItem) ? string.Format("Shared{0}", node.Label) : node.ReferenceItem;
			cssClass = string.IsNullOrEmpty(cssClass) ? string.Format("shared{0}", CssGenerator.GetClassAttributeForConfig(node)) : cssClass.ToLowerInvariant();
			// Ensure the shared node's Label and CSSClassNameOverride are both unique within this Configuration
			if (SharedItems.Any(item => item.Label == key || item.CSSClassNameOverride == cssClass))
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
			SharedItems.Add(sharedItem);
			node.ReferenceItem = key;
			node.ReferencedNode = sharedItem;
			node.Children = null;
		}

		public override string ToString()
		{
			return Label;
		}

		/// <summary>If node is a HeadWord node.</summary>
		internal static bool IsHeadWord(ConfigurableDictionaryNode node)
		{
			if (node == null)
				throw new ArgumentNullException("node");
			return node.CSSClassNameOverride == "headword" || node.CSSClassNameOverride == "mainheadword";
		}

		/// <summary>If node is a Main Entry node that should not be duplicated or edited.</summary>
		internal static bool IsReadonlyMainEntry(ConfigurableDictionaryNode node)
		{
			return IsMainEntry(node) && node.DictionaryNodeOptions == null;
		}

		/// <summary>If node is a Main Entry node.</summary>
		internal static bool IsMainEntry(ConfigurableDictionaryNode node)
		{
			if (node == null)
				throw new ArgumentNullException("node");
			switch (node.CSSClassNameOverride)
			{
				case "entry":
				case "mainentrycomplex":
				case "reversalindexentry":
					return true;
				default:
					return false;
			}
		}
	}
}
