//-------------------------------------------------------------------------------------------------
// <copyright file="Intermediate.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//
//    The use and distribution terms for this software are covered by the
//    Common Public License 1.0 (http://opensource.org/licenses/cpl.php)
//    which can be found in the file CPL.TXT at the root of this distribution.
//    By using this software in any fashion, you are agreeing to be bound by
//    the terms of this license.
//
//    You must not remove this notice, or any other, from this software.
// </copyright>
//
// <summary>
// Container class for an intermediate object.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;
	using System.Xml;

	/// <summary>
	/// Container class for an intermediate object.
	/// </summary>
	public class Intermediate
	{
		private string path;
		private string sourcePath;
		private SectionCollection sections;

		/// <summary>
		/// Instantiate a new Intermediate.
		/// </summary>
		public Intermediate()
		{
			this.sections = new SectionCollection();
		}

		/// <summary>
		/// Get or set the path to this intermediate on disk.
		/// </summary>
		/// <value>Path to this intermediate on disk.</value>
		/// <remarks>The Path may be null if this intermediate was never persisted to disk.</remarks>
		public string Path
		{
			get { return this.path; }
			set { this.path = value; }
		}

		/// <summary>
		/// Get or set the path to the original source file that was used to generate this intermediate.
		/// </summary>
		/// <value>Path to the original source file that was used to generate this intermediate.</value>
		/// <remarks>The SourcePath may be null if the original source was not loaded from disk intermediate.</remarks>
		public string SourcePath
		{
			get { return this.sourcePath; }
			set { this.sourcePath = value; }
		}

		/// <summary>
		/// Get the sections contained in this intermediate.
		/// </summary>
		/// <value>Sections contained in this intermediate.</value>
		internal SectionCollection Sections
		{
			get { return this.sections; }
		}

		/// <summary>
		/// Loads an intermediate from a path on disk.
		/// </summary>
		/// <param name="path">Path to intermediate file saved on disk.</param>
		/// <param name="tableDefinitions">Collection containing TableDefinitions to use when reconstituting the intermediate.</param>
		/// <param name="suppressVersionCheck">Suppress checking for wix.dll version mismatches.</param>
		/// <returns>Returns the loaded intermediate.</returns>
		/// <remarks>This method will set the Path and SourcePath properties to the appropriate values on successful load.</remarks>
		public static Intermediate Load(string path, TableDefinitionCollection tableDefinitions, bool suppressVersionCheck)
		{
			Intermediate intermediate = new Intermediate();
			intermediate.Path = path;

			XmlTextReader reader = null;
			try
			{
				reader = new XmlTextReader(path);
				ParseIntermediate(intermediate, reader, tableDefinitions, suppressVersionCheck);
			}
			finally
			{
				if (null != reader)
				{
					reader.Close();
				}
			}

			return intermediate;
		}

		/// <summary>
		/// Loads an intermediate from a XmlReader in memory.
		/// </summary>
		/// <param name="reader">XmlReader with intermediate persisted as Xml.</param>
		/// <param name="path">Path to intermediate file saved on disk.</param>
		/// <param name="tableDefinitions">Collection containing TableDefinitions to use when reconstituting the intermediate.</param>
		/// <param name="suppressVersionCheck">Suppresses wix.dll version mismatch check.</param>
		/// <returns>Returns the loaded intermediate.</returns>
		/// <remarks>This method will set the SourcePath property to the appropriate values on successful load, but will not update the Path property.</remarks>
		public static Intermediate Load(XmlReader reader, string path, TableDefinitionCollection tableDefinitions, bool suppressVersionCheck)
		{
			Intermediate intermediate = new Intermediate();
			intermediate.path = path;

			ParseIntermediate(intermediate, reader, tableDefinitions, suppressVersionCheck);
			return intermediate;
		}

		/// <summary>
		/// Saves an intermediate to a path on disk.
		/// </summary>
		/// <param name="path">Path to save intermediate file to disk.</param>
		/// <remarks>This method will set the Path property to the passed in value before saving.</remarks>
		public void Save(string path)
		{
			this.Path = path;
			this.Save();
		}

		/// <summary>
		/// Saves an intermediate to a path on disk.
		/// </summary>
		/// <remarks>This method will save the intermediate to the file specified in the Path property.</remarks>
		public void Save()
		{
			XmlWriter writer = null;
			try
			{
				writer = new XmlTextWriter(this.Path, System.Text.Encoding.UTF8);
				this.Persist(writer);
			}
			finally
			{
				if (null != writer)
				{
					writer.Close();
				}
			}
		}

		/// <summary>
		/// Persists an intermediate in an XML format.
		/// </summary>
		/// <param name="writer">XmlWriter where the Intermediate should persist itself as XML.</param>
		public void Persist(XmlWriter writer)
		{
			this.Persist(writer, true);
		}

		/// <summary>
		/// Persists an intermediate in an XML format.
		/// </summary>
		/// <param name="writer">XmlWriter where the Intermediate should persist itself as XML.</param>
		/// <param name="writeDocumentElements">If true, will write the document elements.</param>
		public void Persist(XmlWriter writer, bool writeDocumentElements)
		{
			if (null == writer)
			{
				throw new ArgumentNullException("writer");
			}

			if (writeDocumentElements)
			{
				writer.WriteStartDocument();
			}
			writer.WriteStartElement("wixObject");
			writer.WriteAttributeString("xmlns", "http://schemas.microsoft.com/wix/2003/04/objects");
			writer.WriteAttributeString("src", this.sourcePath);

			Version currentVersion = Common.IntermediateFormatVersion;
			writer.WriteAttributeString("version", currentVersion.ToString());

			foreach (Section section in this.sections)
			{
				// save the section
				writer.WriteStartElement("section");
				if (null != section.Id)
				{
					writer.WriteAttributeString("id", section.Id);
				}
				switch (section.Type)
				{
					case SectionType.Fragment:
						writer.WriteAttributeString("type", "fragment");
						break;
					case SectionType.Module:
						writer.WriteAttributeString("type", "module");
						break;
					case SectionType.Product:
						writer.WriteAttributeString("type", "product");
						break;
					case SectionType.PatchCreation:
						writer.WriteAttributeString("type", "patchCreation");
						break;
				}
				if (0 != section.Codepage)
				{
					writer.WriteAttributeString("codepage", section.Codepage.ToString());
				}

				// don't need to persist the symbols since they are recreated during load

				// save the references
				foreach (Reference reference in section.References)
				{
					reference.Persist(writer);
				}

				foreach (ComplexReference complexReference in section.ComplexReferences)
				{
					complexReference.Persist(writer);
				}

				// save the feature backlinks
				foreach (FeatureBacklink blink in section.FeatureBacklinks)
				{
					blink.Persist(writer);
				}

				// save the ignoreModularizations
				foreach (IgnoreModularization ignoreModular in section.IgnoreModularizations)
				{
					ignoreModular.Persist(writer);
				}

				// save the rows in table order
				foreach (Table table in section.Tables)
				{
					table.Persist(writer);
				}

				writer.WriteEndElement();
			}

			writer.WriteEndElement();
			if (writeDocumentElements)
			{
				writer.WriteEndDocument();
			}
		}

		/// <summary>
		/// Parse an intermediate from an XML format.
		/// </summary>
		/// <param name="intermediate">Intermediate to populate with persisted data.</param>
		/// <param name="reader">XmlReader where the intermediate is persisted.</param>
		/// <param name="tableDefinitions">TableDefinitions to use in the intermediate.</param>
		/// <param name="suppressVersionCheck">Suppress checking for wix.dll version mismatch.</param>
		private static void ParseIntermediate(Intermediate intermediate, XmlReader reader, TableDefinitionCollection tableDefinitions, bool suppressVersionCheck)
		{
			// read the document root
			reader.MoveToContent();
			if ("wixObject" != reader.LocalName)
			{
				throw new WixNotIntermediateException(SourceLineNumberCollection.FromFileName(intermediate.Path), String.Format("Invalid root element: '{0}', expected 'wixObject'", reader.LocalName));
			}

			bool empty = reader.IsEmptyElement;

			Version objVersion = null;

			while (reader.MoveToNextAttribute())
			{
				switch (reader.LocalName)
				{
					case "src":
						intermediate.SourcePath = reader.Value;
						break;
					case "version":
						objVersion = new Version(reader.Value);
						break;
				}
			}

			if (null != objVersion && !suppressVersionCheck)
			{
				Version currentVersion = Common.IntermediateFormatVersion;
				if (0 != currentVersion.CompareTo(objVersion))
				{
					throw new WixVersionMismatchException(currentVersion, objVersion, "Intermediate", intermediate.Path);
				}
			}

			// loop through the rest of the xml building up the SectionCollection
			if (!empty)
			{
				bool done = false;

				while (!done && reader.Read())
				{
					switch (reader.NodeType)
					{
						case XmlNodeType.Element:
							switch (reader.LocalName)
							{
								case "section":
									ParseSection(intermediate, reader, tableDefinitions);
									break;
								default:
									throw new WixInvalidIntermediateException(SourceLineNumberCollection.FromFileName(intermediate.Path), String.Format("Unexpected element while processing 'wixObject': '{0}'", reader.LocalName));
							}
							break;
						case XmlNodeType.EndElement:
							done = true;
							break;
					}
				}

				if (!done)
				{
					throw new WixInvalidIntermediateException(SourceLineNumberCollection.FromFileName(intermediate.Path), String.Format("Missing end element while processing 'wixObject'."));
				}
			}
		}

		/// <summary>
		/// Parse a section from the xml.
		/// </summary>
		/// <param name="intermediate">Intermediate to populate with persisted data.</param>
		/// <param name="reader">XmlReader where the intermediate is persisted.</param>
		/// <param name="tableDefinitions">TableDefinitions to use in the intermediate.</param>
		private static void ParseSection(Intermediate intermediate, XmlReader reader, TableDefinitionCollection tableDefinitions)
		{
			Section section = null;
			string id = null;
			SectionType type = SectionType.Unknown;
			int codepage = 0;
			bool empty = reader.IsEmptyElement;

			while (reader.MoveToNextAttribute())
			{
				switch (reader.Name)
				{
					case "id":
						id = reader.Value;
						break;
					case "type":
						switch (reader.Value)
						{
							case "fragment":
								type = SectionType.Fragment;
								break;
							case "module":
								type = SectionType.Module;
								break;
							case "product":
								type = SectionType.Product;
								break;
							case "patchCreation":
								type = SectionType.PatchCreation;
								break;
						}
						break;
					case "codepage":
						codepage = Convert.ToInt32(reader.Value);
						break;
				}
			}
			if (SectionType.Unknown == type)
			{
				throw new WixInvalidIntermediateException(SourceLineNumberCollection.FromFileName(intermediate.Path), "Unknown section type");
			}
			if (null == id && SectionType.Fragment != type)
			{
				throw new WixInvalidIntermediateException(SourceLineNumberCollection.FromFileName(intermediate.Path), "Section missing required attribute: 'id'");
			}

			section = new Section(intermediate, id, type, codepage);

			if (!empty)
			{
				bool done = false;

				while (!done && reader.Read())
				{
					switch (reader.NodeType)
					{
						case XmlNodeType.Element:
						switch (reader.LocalName)
						{
							case "reference":
								ParseReference(intermediate, reader, section);
								break;
							case "complexReference":
								ParseComplexReference(intermediate, reader, section);
								break;
							case "featureBacklink":
								ParseFeatureBacklink(intermediate, reader, section);
								break;
							case "table":
								ParseTable(intermediate, reader, section, tableDefinitions);
								break;
							case "ignoreModularization":
								ParseIgnoreModularization(intermediate, reader, section);
								break;
							default:
								throw new WixInvalidIntermediateException(SourceLineNumberCollection.FromFileName(intermediate.Path), String.Format("Unexpected element while processing 'section': '{0}'", reader.LocalName));
						}
							break;
						case XmlNodeType.EndElement:
							done = true;
							break;
					}
				}

				if (!done)
				{
					throw new WixInvalidIntermediateException(SourceLineNumberCollection.FromFileName(intermediate.Path), String.Format("Missing end element while processing 'section'."));
				}
			}

			intermediate.Sections.Add(section);
		}

		/// <summary>
		/// Parse a reference from the xml.
		/// </summary>
		/// <param name="intermediate">Intermediate to populate with persisted data.</param>
		/// <param name="reader">XmlReader where the intermediate is persisted.</param>
		/// <param name="section">Section to populate with persisted data.</param>
		private static void ParseReference(Intermediate intermediate, XmlReader reader, Section section)
		{
			bool empty = reader.IsEmptyElement;
			string id = null;
			string table = null;

			while (reader.MoveToNextAttribute())
			{
				switch (reader.LocalName)
				{
					case "symbol":
						id = reader.Value;
						break;
					case "table":
						table = reader.Value;
						break;
				}
			}
			if (null == table)
			{
				throw new WixInvalidIntermediateException(SourceLineNumberCollection.FromFileName(intermediate.Path), "Reference missing required attribute: 'table'");
			}
			if (null == id)
			{
				throw new WixInvalidIntermediateException(SourceLineNumberCollection.FromFileName(intermediate.Path), "Reference missing required attribute: 'id'");
			}

			if (!empty && reader.Read() && XmlNodeType.EndElement != reader.MoveToContent())
			{
				throw new WixInvalidIntermediateException(SourceLineNumberCollection.FromFileName(intermediate.Path), String.Format("Unexpected content while processing 'reference': {0}", reader.NodeType.ToString()));
			}

			section.References.Add(new Reference(table, id));
		}

		/// <summary>
		/// Parse a feature backlink from the xml.
		/// </summary>
		/// <param name="intermediate">Intermediate to populate with persisted data.</param>
		/// <param name="reader">XmlReader where the intermediate is persisted.</param>
		/// <param name="section">Section to populate with persisted data.</param>
		private static void ParseFeatureBacklink(Intermediate intermediate, XmlReader reader, Section section)
		{
			bool empty = reader.IsEmptyElement;
			string targetSymbol = null;
			string component = null;
			FeatureBacklinkType type = FeatureBacklinkType.Unknown;

			while (reader.MoveToNextAttribute())
			{
				switch (reader.LocalName)
				{
					case "targetSymbol":
						targetSymbol = reader.Value;
						break;
					case "component":
						component = reader.Value;
						break;
					case "type":
						switch (reader.Value)
						{
							case "Class":
								type = FeatureBacklinkType.Class;
								break;
							case "Extension":
								type = FeatureBacklinkType.Extension;
								break;
							case "Shortcut":
								type = FeatureBacklinkType.Shortcut;
								break;
							case "PublishComponent":
								type = FeatureBacklinkType.PublishComponent;
								break;
							case "TypeLib":
								type = FeatureBacklinkType.TypeLib;
								break;
							default:
								throw new WixInvalidIntermediateException(SourceLineNumberCollection.FromFileName(intermediate.Path), "Invalid FeatureBacklinkType");
						}
						break;
				}
			}
			if (FeatureBacklinkType.Unknown == type)
			{
				throw new WixInvalidIntermediateException(SourceLineNumberCollection.FromFileName(intermediate.Path), "Unknown FeatureBackLink type");
			}
			if (null == component)
			{
				throw new WixInvalidIntermediateException(SourceLineNumberCollection.FromFileName(intermediate.Path), "FeatureBackLink missing required attribute: 'component'");
			}
			if (null == targetSymbol)
			{
				throw new WixInvalidIntermediateException(SourceLineNumberCollection.FromFileName(intermediate.Path), "FeatureBackLink missing required attribute: 'targetSymbol'");
			}

			if (!empty && reader.Read() && XmlNodeType.EndElement != reader.MoveToContent())
			{
				throw new WixInvalidIntermediateException(SourceLineNumberCollection.FromFileName(intermediate.Path), String.Format("Unexpected content while processing 'featureBacklink': {0}", reader.NodeType.ToString()));
			}

			section.FeatureBacklinks.Add(new FeatureBacklink(component, type, targetSymbol));
		}

		/// <summary>
		/// Parse a complex reference from the xml.
		/// </summary>
		/// <param name="intermediate">Intermediate to populate with persisted data.</param>
		/// <param name="reader">XmlReader where the intermediate is persisted.</param>
		/// <param name="section">Section to populate with persisted data.</param>
		private static void ParseComplexReference(Intermediate intermediate, XmlReader reader, Section section)
		{
			bool empty = reader.IsEmptyElement;
			ComplexReferenceParentType parentType = ComplexReferenceParentType.Unknown;
			string parentId = null;
			string parentLanguage = null;
			ComplexReferenceChildType childType = ComplexReferenceChildType.Unknown;
			string childId = null;
			bool primary = false;

			while (reader.MoveToNextAttribute())
			{
				switch (reader.LocalName)
				{
					case "parent":
						parentId = reader.Value;
						break;
					case "parentLanguage":
						parentLanguage = reader.Value;
						break;
					case "parentType":
						switch (reader.Value)
						{
							case "componentGroup":
								parentType = ComplexReferenceParentType.ComponentGroup;
								break;
							case "feature":
								parentType = ComplexReferenceParentType.Feature;
								break;
							case "module":
								parentType = ComplexReferenceParentType.Module;
								break;
						}
						break;
					case "child":
						childId = reader.Value;
						break;
					case "childType":
						switch (reader.Value)
						{
							case "component":
								childType = ComplexReferenceChildType.Component;
								break;
							case "componentGroup":
								childType = ComplexReferenceChildType.ComponentGroup;
								break;
							case "feature":
								childType = ComplexReferenceChildType.Feature;
								break;
							case "fragment":
								childType = ComplexReferenceChildType.Fragment;
								break;
							case "module":
								childType = ComplexReferenceChildType.Module;
								break;
						}
						break;
					case "primary":
						primary = Common.IsYes(reader.Value, SourceLineNumberCollection.FromFileName(intermediate.Path), "complexReference", "primary", parentId);
						break;
				}
			}
			if (ComplexReferenceParentType.Unknown == parentType)
			{
				throw new WixInvalidIntermediateException(SourceLineNumberCollection.FromFileName(intermediate.Path), "Unknown ComplexReferenceParentType type");
			}
			if (null == parentId)
			{
				throw new WixInvalidIntermediateException(SourceLineNumberCollection.FromFileName(intermediate.Path), "ComplexReference missing required attribute: 'parentId'");
			}
			if (ComplexReferenceChildType.Unknown == childType)
			{
				throw new WixInvalidIntermediateException(SourceLineNumberCollection.FromFileName(intermediate.Path), "Unknown ComplexReferenceChildType type");
			}
			if (null == childId)
			{
				throw new WixInvalidIntermediateException(SourceLineNumberCollection.FromFileName(intermediate.Path), "ComplexReference missing required attribute: 'childId'");
			}

			if (!empty && reader.Read() && XmlNodeType.EndElement != reader.MoveToContent())
			{
				throw new WixInvalidIntermediateException(SourceLineNumberCollection.FromFileName(intermediate.Path), String.Format("Unexpected content while processing 'complexReference': {0}", reader.NodeType.ToString()));
			}

			section.ComplexReferences.Add(new ComplexReference(parentType, parentId, parentLanguage, childType, childId, primary));
		}

		/// <summary>
		/// Parse a table from the xml.
		/// </summary>
		/// <param name="intermediate">Intermediate to populate with persisted data.</param>
		/// <param name="reader">XmlReader where the intermediate is persisted.</param>
		/// <param name="section">Section to populate with persisted data.</param>
		/// <param name="tableDefinitions">TableDefinitions to use in the intermediate.</param>
		private static void ParseTable(Intermediate intermediate, XmlReader reader, Section section, TableDefinitionCollection tableDefinitions)
		{
			bool empty = reader.IsEmptyElement;
			string name = null;

			while (reader.MoveToNextAttribute())
			{
				switch (reader.LocalName)
				{
					case "name":
						name = reader.Value;
						break;
				}
			}
			if (null == name)
			{
				throw new WixInvalidIntermediateException(SourceLineNumberCollection.FromFileName(intermediate.Path), "Table missing required attribute: 'name'");
			}

			if (!empty)
			{
				bool done = false;

				// loop through all the rows (tuples) in a table
				while (!done && reader.Read())
				{
					switch (reader.NodeType)
					{
						case XmlNodeType.Element:
							switch (reader.LocalName)
							{
								case "tuple":
									ParseTuple(intermediate, reader, section, tableDefinitions[name]);
									break;
								default:
									throw new WixInvalidIntermediateException(SourceLineNumberCollection.FromFileName(intermediate.Path), String.Format("Unexpected element while processing 'table': '{0}'", reader.LocalName));
							}
							break;
						case XmlNodeType.EndElement:
							done = true;
							break;
					}
				}

				if (!done)
				{
					throw new WixInvalidIntermediateException(SourceLineNumberCollection.FromFileName(intermediate.Path), String.Format("Missing end element while processing 'table'."));
				}
			}
		}

		/// <summary>
		/// Parse ignore modularization data from the xml.
		/// </summary>
		/// <param name="intermediate">Intermediate to populate with persisted data.</param>
		/// <param name="reader">XmlReader where the intermediate is persisted.</param>
		/// <param name="section">Section to populate with persisted data.</param>
		private static void ParseIgnoreModularization(Intermediate intermediate, XmlReader reader, Section section)
		{
			bool empty = reader.IsEmptyElement;
			string name = null;
			string type = null;

			while (reader.MoveToNextAttribute())
			{
				switch (reader.LocalName)
				{
					case "name":
						name = reader.Value;
						break;
					case "type":
						type = reader.Value;
						break;
				}
			}
			if (null == name)
			{
				throw new WixInvalidIntermediateException(SourceLineNumberCollection.FromFileName(intermediate.Path), "Reference missing required attribute: 'name'");
			}
			if (null == type)
			{
				throw new WixInvalidIntermediateException(SourceLineNumberCollection.FromFileName(intermediate.Path), "Reference missing required attribute: 'type'");
			}

			if (!empty && reader.Read() && XmlNodeType.EndElement != reader.MoveToContent())
			{
				throw new WixInvalidIntermediateException(SourceLineNumberCollection.FromFileName(intermediate.Path), String.Format("Unexpected content while processing 'ignoreModularization': {0}", reader.NodeType.ToString()));
			}

			section.IgnoreModularizations.Add(new IgnoreModularization(name, type));
		}

		/// <summary>
		/// Parse a tuple from the xml.
		/// </summary>
		/// <param name="intermediate">Intermediate to populate with persisted data.</param>
		/// <param name="reader">XmlReader where the intermediate is persisted.</param>
		/// <param name="section">Section to populate with persisted data.</param>
		/// <param name="tableDef">Table definition of the tuple to parse.</param>
		private static void ParseTuple(Intermediate intermediate, XmlReader reader, Section section, TableDefinition tableDef)
		{
			bool empty = reader.IsEmptyElement;
			SourceLineNumberCollection sourceLineNumbers = null;
			int field = 0;

			while (reader.MoveToNextAttribute())
			{
				switch (reader.LocalName)
				{
					case "sourceLineNumber":
						sourceLineNumbers = new SourceLineNumberCollection(reader.Value);
						break;
				}
			}

			Row row = Common.CreateRowInSection(sourceLineNumbers, section, tableDef);

			if (!empty)
			{
				bool done = false;

				// loop through all the fields in a row
				while (!done && reader.Read())
				{
					switch (reader.NodeType)
					{
						case XmlNodeType.Element:
							switch (reader.LocalName)
							{
								case "field":
									row[field] = Field.Parse(reader);
									++field;
									break;
								default:
									throw new WixInvalidIntermediateException(SourceLineNumberCollection.FromFileName(intermediate.Path), String.Format("Unexpected element while processing 'tuple': '{0}'", reader.LocalName));
							}
							break;
						case XmlNodeType.EndElement:
							done = true;
							break;
					}
				}

				if (!done)
				{
					throw new WixInvalidIntermediateException(SourceLineNumberCollection.FromFileName(intermediate.Path), String.Format("Missing end element while processing 'tuple'."));
				}
			}
		}
	}
}
