// Copyright (c) 2014-2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using SIL.FieldWorks.FDO;

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

		/// <summary>
		/// Checks which folder this will be saved in to determine if it is a reversal
		/// </summary>
		internal bool IsReversal
		{
			get
			{
				if (!string.IsNullOrEmpty(WritingSystem))
					return true;

				// Fallback for migration
				if (string.IsNullOrEmpty(FilePath))
					return false; // easiest way to avoid a crash; assume something that may not be true!
				var directory = Path.GetFileName(Path.GetDirectoryName(FilePath));
				return DictionaryConfigurationListener.ReversalIndexConfigurationDirectoryName.Equals(directory);
			}
		}

		/// <summary>
		/// A concatenation of Parts and SharedItems; useful for migration and synchronization with the FDO model
		/// </summary>
		[XmlIgnore]
		public IEnumerable<ConfigurableDictionaryNode> PartsAndSharedItems { get { return Parts.Concat(SharedItems); } }

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
			SharedItems = SharedItems ?? new List<ConfigurableDictionaryNode>();
			if (cache == null)
				return;
			SpecifyParentsAndReferences(Parts, SharedItems);
			if (AllPublications)
				Publications = DictionaryConfigurationController.GetAllPublications(cache);
			else
				DictionaryConfigurationController.FilterInvalidPublicationsFromModel(this, cache);
			// Handle any changes to the custom field definitions.  (See https://jira.sil.org/browse/LT-16430.)
			// The "Merge" method handles both additions and deletions.
			DictionaryConfigurationController.MergeCustomFieldsIntoDictionaryModel(this, cache);
			// Handle changes to the lists of complex form types and variant types.
			DictionaryConfigurationController.MergeTypesIntoDictionaryModel(this, cache);
			// Handle any deleted styles.  (See https://jira.sil.org/browse/LT-16501.)
			DictionaryConfigurationController.EnsureValidStylesInModel(this, cache);
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
		/// Assign Parent and ReferencedNode properties to descendants of nodes.
		/// </summary>
		internal static void SpecifyParentsAndReferences(List<ConfigurableDictionaryNode> nodes, List<ConfigurableDictionaryNode> sharedItems = null)
		{
			if (nodes == null)
				throw new ArgumentNullException();

			var rollingNodes = new List<ConfigurableDictionaryNode>(nodes);

			while (rollingNodes.Any())
			{
				var node = rollingNodes[0];
				rollingNodes.RemoveAt(0);
				if (!string.IsNullOrEmpty(node.ReferenceItem))
					DictionaryConfigurationController.LinkReferencedNode(sharedItems, node, node.ReferenceItem);
				if (node.Children == null)
					continue;
				foreach (var child in node.Children)
					child.Parent = node;
				rollingNodes.AddRange(node.Children);
			}

			if (sharedItems != null && !ReferenceEquals(nodes, sharedItems))
				SpecifyParentsAndReferences(sharedItems, sharedItems);
		}

		public override string ToString()
		{
			return Label;
		}
	}
}
