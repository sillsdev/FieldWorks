// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// A selection of dictionary elements and options, for configuring a dictionary publication.
	/// </summary>
	[XmlRoot(ElementName = "DictionaryConfiguration")]
	public class DictionaryConfigurationModel
	{
		/// <summary>
		/// Trees of dictionary elements
		/// </summary>
		[XmlElement(ElementName = "ConfigurationItem")]
		public List<ConfigurableDictionaryNode> Parts;

		/// <summary>
		/// File where data is stored
		/// </summary>
		[XmlIgnore]
		public string FilePath { get; set; }

		/// <summary>
		/// Name of this dictionary configuration. eg "Stem-based"
		/// </summary>
		[XmlAttribute(AttributeName = "name")]
		public string Label;

		[XmlAttribute(AttributeName = "lastModified", DataType = "date")]
		public DateTime LastModified { get; set; }

		/// <summary>
		/// The version of the DictionaryConfigurationModel for use in data migration ect.
		/// </summary>
		[XmlAttribute(AttributeName = "version")]
		public int Version;

		/// <summary></summary>
		public void Save()
		{
			LastModified = DateTime.Now;
			var serializer = new XmlSerializer(typeof(DictionaryConfigurationModel));
			using(var writer = XmlWriter.Create(FilePath))
			{
				serializer.Serialize(writer, this);
			}
		}

		/// <summary></summary>
		public void Load()
		{
			var serializer = new XmlSerializer(typeof(DictionaryConfigurationModel));
			using(var reader = XmlReader.Create(FilePath))
			{
				var model = (DictionaryConfigurationModel)serializer.Deserialize(reader);
				Label = model.Label;
				LastModified = model.LastModified;
				Version = model.Version;
				Parts = model.Parts;
			}
			SpecifyParents(Parts);
		}

		/// <summary>
		/// Default constructor for easier testing.
		/// </summary>
		internal DictionaryConfigurationModel() {}

		/// <summary>Loads a DictionaryConfigurationModel from the given path</summary>
		public DictionaryConfigurationModel(string path)
		{
			FilePath = path;
			Load();
		}

		/// <summary>
		/// Assign Parent properties to descendants of nodes.
		/// </summary>
		internal static void SpecifyParents(List<ConfigurableDictionaryNode> nodes)
		{
			if (nodes == null)
				throw new ArgumentNullException();

			foreach (var node in nodes)
			{
				if (node.Children == null)
					continue;
				foreach (var child in node.Children)
					child.Parent = node;
				SpecifyParents(node.Children);
			}
		}
	}
}
