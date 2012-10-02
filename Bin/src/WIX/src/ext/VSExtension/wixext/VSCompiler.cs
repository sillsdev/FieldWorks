//-------------------------------------------------------------------------------------------------
// <copyright file="VSCompiler.cs" company="Microsoft">
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
// The compiler for the Windows Installer XML Toolset Visual Studio Extension.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Extensions
{
	using System;
	using System.Diagnostics;
	using System.IO;
	using System.Reflection;
	using System.Xml;
	using System.Xml.Schema;
	using Microsoft.Tools.WindowsInstallerXml;

	/// <summary>
	/// The compiler for the Windows Installer XML Toolset Visual Studio Extension.
	/// </summary>
	public sealed class VSCompiler : CompilerExtension
	{
		/// <summary>
		/// Instantiate a new VSCompiler.
		/// </summary>
		public VSCompiler()
		{
			Assembly assembly = Assembly.GetExecutingAssembly();
			XmlReader schemaReader = null;

			// load the schema extensions
			try
			{
				schemaReader = GetXmlFromEmbeddedStream(assembly, "Microsoft.Tools.WindowsInstallerXml.Extensions.Xsd.vs.xsd");
				this.xmlSchema = XmlSchema.Read(schemaReader, null);
			}
			finally
			{
				if (null != schemaReader)
				{
					schemaReader.Close();
				}
			}

			// load the table definition extensions
			this.tableDefinitionCollection = GetTableDefinitions();
		}

		/// <summary>
		/// Gets the table definitions stored in this assembly.
		/// </summary>
		/// <returns>Table definition collection for tables stored in this assembly.</returns>
		public static TableDefinitionCollection GetTableDefinitions()
		{
			Assembly assembly = Assembly.GetExecutingAssembly();
			XmlReader tableDefinitionsReader = null;

			try
			{
				tableDefinitionsReader = GetXmlFromEmbeddedStream(assembly, "Microsoft.Tools.WindowsInstallerXml.Extensions.Data.tables.xml");
				return TableDefinitionCollection.Load(tableDefinitionsReader);
			}
			finally
			{
				if (null != tableDefinitionsReader)
				{
					tableDefinitionsReader.Close();
				}
			}
		}

		/// <summary>
		/// Processes an attribute for the Compiler.
		/// </summary>
		/// <param name="sourceLineNumbers">Source line number for the parent element.</param>
		/// <param name="parentElement">Parent element of attribute.</param>
		/// <param name="attribute">Attribute to process.</param>
		public override void ParseAttribute(SourceLineNumberCollection sourceLineNumbers, XmlElement parentElement, XmlAttribute attribute)
		{
			this.Core.UnexpectedAttribute(sourceLineNumbers, attribute);
		}

		/// <summary>
		/// Processes an element for the Compiler.
		/// </summary>
		/// <param name="sourceLineNumbers">Source line number for the parent element.</param>
		/// <param name="parentElement">Parent element of element to process.</param>
		/// <param name="element">Element to process.</param>
		public override void ParseElement(SourceLineNumberCollection sourceLineNumbers, XmlElement parentElement, XmlElement element)
		{
			switch (parentElement.LocalName)
			{
				case "File":
					string fileId = parentElement.GetAttribute("Id");

					switch (element.LocalName)
					{
						case "HelpCollection":
							this.ParseHelpCollectionElement(element, fileId);
							break;
						case "HelpFile":
							this.ParseHelpFileElement(element, fileId);
							break;
						default:
							this.Core.UnexpectedElement(parentElement, element);
							break;
					}
					break;
				case "Fragment":
				case "Product":
					switch (element.LocalName)
					{
						case "HelpCollectionRef":
							this.ParseHelpCollectionRefElement(element);
							break;
						case "HelpFilter":
							this.ParseHelpFilterElement(element);
							break;
						default:
							this.Core.UnexpectedElement(parentElement, element);
							break;
					}
					break;
				default:
					this.Core.UnexpectedElement(parentElement, element);
					break;
			}
		}

		/// <summary>
		/// Gets an Xml reader from the stream in the specified assembly.
		/// </summary>
		/// <param name="assembly">Assembly to get embedded stream from.</param>
		/// <param name="resourceStreamName">Name of stream.</param>
		/// <returns>Xml reader for stream in assembly.</returns>
		/// <remarks>The returned reader should be closed when done processing the Xml.</remarks>
		private static XmlReader GetXmlFromEmbeddedStream(Assembly assembly, string resourceStreamName)
		{
			Stream stream = assembly.GetManifestResourceStream(resourceStreamName);
			return new XmlTextReader(stream);
		}

		/// <summary>
		/// Parses a HelpCollectionRef element.
		/// </summary>
		/// <param name="node">Element to process.</param>
		private void ParseHelpCollectionRefElement(XmlNode node)
		{
			SourceLineNumberCollection sourceLineNumbers = this.Core.GetSourceLineNumbers(node);
			string id = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						break;
					default:
						this.Core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == id)
			{
				this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					switch (child.LocalName)
					{
						case "HelpFileRef":
							this.ParseHelpFileRefElement(child, id);
							break;
						default:
							this.Core.UnexpectedElement(node, child);
							break;
					}
				}
			}

			this.Core.AddValidReference("HelpNamespace", id);
		}

		/// <summary>
		/// Parses a HelpCollection element.
		/// </summary>
		/// <param name="node">Element to process.</param>
		/// <param name="fileId">Identifier of the parent File element.</param>
		private void ParseHelpCollectionElement(XmlNode node, string fileId)
		{
			SourceLineNumberCollection sourceLineNumbers = this.Core.GetSourceLineNumbers(node);
			string id = null;
			string description = null;
			string name = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						break;
					case "Description":
						description = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Name":
						name = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						this.Core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == id)
			{
				this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}

			if (null == description)
			{
				this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Description"));
			}

			if (null == name)
			{
				this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Name"));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					switch (child.LocalName)
					{
						case "HelpFileRef":
							this.ParseHelpFileRefElement(child, id);
							break;
						case "HelpFilterRef":
							this.ParseHelpFilterRefElement(child, id);
							break;
						case "PlugCollectionInto":
							this.ParsePlugCollectionIntoElement(child, id);
							break;
						default:
							this.Core.UnexpectedElement(node, child);
							break;
					}
				}
			}

			if (!this.Core.EncounteredError)
			{
				Row row = this.Core.CreateRow(sourceLineNumbers, "HelpNamespace");
				row[0] = id;
				row[1] = name;
				row[2] = fileId;
				row[3] = description;
			}
		}

		/// <summary>
		/// Parses a HelpFile element.
		/// </summary>
		/// <param name="node">Element to process.</param>
		/// <param name="fileId">Identifier of the parent file element.</param>
		private void ParseHelpFileElement(XmlNode node, string fileId)
		{
			SourceLineNumberCollection sourceLineNumbers = this.Core.GetSourceLineNumbers(node);
			string id = null;
			string name = null;
			int language = CompilerCore.IntegerNotSet;
			string hxi = null;
			string hxq = null;
			string hxr = null;
			string samples = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						break;
					case "AttributeIndex":
						hxr = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						this.Core.AddValidReference("File", hxr);
						break;
					case "Index":
						hxi = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						this.Core.AddValidReference("File", hxi);
						break;
					case "Language":
						language = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						break;
					case "Name":
						name = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "SampleLocation":
						samples = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						this.Core.AddValidReference("File", samples);
						break;
					case "Search":
						hxq = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						this.Core.AddValidReference("File", hxq);
						break;
					default:
						this.Core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == id)
			{
				this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}

			if (null == name)
			{
				this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Name"));
			}

			// find unexpected child elements
			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.Core.UnexpectedElement(node, child);
				}
			}

			if (!this.Core.EncounteredError)
			{
				Row row = this.Core.CreateRow(sourceLineNumbers, "HelpFile");
				row[0] = id;
				row[1] = name;
				if (CompilerCore.IntegerNotSet != language)
				{
					row[2] = language;
				}
				row[3] = fileId;
				row[4] = hxi;
				row[5] = hxq;
				row[6] = hxr;
				row[7] = samples;
			}
		}

		/// <summary>
		/// Parses a HelpFileRef element.
		/// </summary>
		/// <param name="node">Element to process.</param>
		/// <param name="collectionId">Identifier of the parent help collection.</param>
		private void ParseHelpFileRefElement(XmlNode node, string collectionId)
		{
			SourceLineNumberCollection sourceLineNumbers = this.Core.GetSourceLineNumbers(node);
			string id = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						break;
					default:
						this.Core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == id)
			{
				this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}

			// find unexpected child elements
			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.Core.UnexpectedElement(node, child);
				}
			}

			if (!this.Core.EncounteredError)
			{
				this.Core.AddValidReference("HelpFile", id);

				Row row = this.Core.CreateRow(sourceLineNumbers, "HelpFileToNamespace");
				row[0] = id;
				row[1] = collectionId;
			}
		}

		/// <summary>
		/// Parses a HelpFilter element.
		/// </summary>
		/// <param name="node">Element to process.</param>
		private void ParseHelpFilterElement(XmlNode node)
		{
			SourceLineNumberCollection sourceLineNumbers = this.Core.GetSourceLineNumbers(node);
			string id = null;
			string filterDefinition = null;
			string name = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						break;
					case "FilterDefinition":
						filterDefinition = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Name":
						name = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						this.Core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == id)
			{
				this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}

			if (null == name)
			{
				this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Name"));
			}

			// find unexpected child elements
			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.Core.UnexpectedElement(node, child);
				}
			}

			if (!this.Core.EncounteredError)
			{
				Row row = this.Core.CreateRow(sourceLineNumbers, "HelpFilter");
				row[0] = id;
				row[1] = name;
				row[2] = filterDefinition;
			}
		}

		/// <summary>
		/// Parses a HelpFilterRef element.
		/// </summary>
		/// <param name="node">Element to process.</param>
		/// <param name="collectionId">Identifier of the parent help collection.</param>
		private void ParseHelpFilterRefElement(XmlNode node, string collectionId)
		{
			SourceLineNumberCollection sourceLineNumbers = this.Core.GetSourceLineNumbers(node);
			string id = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						break;
					default:
						this.Core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == id)
			{
				this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}

			// find unexpected child elements
			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.Core.UnexpectedElement(node, child);
				}
			}

			if (!this.Core.EncounteredError)
			{
				this.Core.AddValidReference("HelpFilter", id);

				Row row = this.Core.CreateRow(sourceLineNumbers, "HelpFilterToNamespace");
				row[0] = id;
				row[1] = collectionId;
			}
		}

		/// <summary>
		/// Parses a PlugCollectionInto element.
		/// </summary>
		/// <param name="node">Element to process.</param>
		/// <param name="parentId">Identifier of the parent help collection.</param>
		private void ParsePlugCollectionIntoElement(XmlNode node, string parentId)
		{
			SourceLineNumberCollection sourceLineNumbers = this.Core.GetSourceLineNumbers(node);
			string hxa = null;
			string hxt = null;
			string hxtParent = null;
			string namespaceParent = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Attributes":
						hxa = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						break;
					case "TableOfContents":
						hxt = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						break;
					case "TargetCollection":
						namespaceParent = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						break;
					case "TargetTableOfContents":
						hxtParent = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						break;
					default:
						this.Core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == namespaceParent)
			{
				this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "TargetCollection"));
			}

			// find unexpected child elements
			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.Core.UnexpectedElement(node, child);
				}
			}

			if (!this.Core.EncounteredError)
			{
				Row row = this.Core.CreateRow(sourceLineNumbers, "HelpPlugin");
				row[0] = parentId;
				row[1] = namespaceParent;
				row[2] = hxt;
				row[3] = hxa;
				row[4] = hxtParent;
			}
		}
	}
}
