//-------------------------------------------------------------------------------------------------
// <copyright file="NetFxCompiler.cs" company="Microsoft">
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
// The compiler for the Windows Installer XML Toolset .NET Framework Extension.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Extensions
{
	using System;
	using System.Diagnostics;
	using System.Globalization;
	using System.IO;
	using System.Reflection;
	using System.Xml;
	using System.Xml.Schema;
	using Microsoft.Tools.WindowsInstallerXml;

	/// <summary>
	/// The compiler for the Windows Installer XML Toolset .NET Framework Extension.
	/// </summary>
	public sealed class NetFxCompiler : CompilerExtension
	{
		/// <summary>
		/// Instantiate a new NetFxCompiler.
		/// </summary>
		public NetFxCompiler()
		{
			Assembly assembly = Assembly.GetExecutingAssembly();
			XmlReader schemaReader = null;

			// load the schema extensions
			try
			{
				schemaReader = GetXmlFromEmbeddedStream(assembly, "Microsoft.Tools.WindowsInstallerXml.Extensions.Xsd.netfx.xsd");
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
						case "NativeImage":
							this.ParseNativeImageElement(element, fileId);
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
		/// Parses a NativeImage element.
		/// </summary>
		/// <param name="node">The element to parse.</param>
		/// <param name="fileId">The file identifier of the parent element.</param>
		private void ParseNativeImageElement(XmlNode node, string fileId)
		{
			SourceLineNumberCollection sourceLineNumbers = this.Core.GetSourceLineNumbers(node);
			string id = null;
			string appBaseDirectory = null;
			string assemblyApplication = null;
			int attributes = 0x8; // 32bit is on by default
			int priority = 3;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						break;
					case "AppBaseDirectory":
						appBaseDirectory = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
						this.Core.AddValidReference("Directory", appBaseDirectory);
						break;
					case "AssemblyApplication":
						assemblyApplication = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
						this.Core.AddValidReference("File", assemblyApplication);
						break;
					case "Debug":
						if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							attributes |= 0x1;
						}
						break;
					case "Dependencies":
						if (YesNoType.No == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							attributes |= 0x2;
						}
						break;
					case "Platform":
						switch (this.Core.GetAttributeValue(sourceLineNumbers, attrib))
						{
							case CompilerCore.IllegalEmptyAttributeValue: // this error is already handled
								break;
							case "32bit":
								// 0x8 is already on by default
								break;
							case "64bit":
								attributes &= ~0x8;
								attributes |= 0x10;
								break;
							case "all":
								attributes |= 0x10;
								break;
						}
						break;
					case "Priority":
						priority = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						break;
					case "Profile":
						if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							attributes |= 0x4;
						}
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

			if (0 > priority || 3 < priority)
			{
				this.Core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name, "Priority", priority.ToString(CultureInfo.InvariantCulture)));
			}

			// find unexpected child elements
			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.Core.UnexpectedElement(node, child);
				}
			}

			this.Core.AddValidReference("CustomAction", "NetFxScheduleNativeImage");

			if (!this.Core.EncounteredError)
			{
				Row row = this.Core.CreateRow(sourceLineNumbers, "NetFxNativeImage");
				row[0] = id;
				row[1] = fileId;
				row[2] = priority;
				row[3] = attributes;
				row[4] = assemblyApplication;
				row[5] = appBaseDirectory;
			}
		}
	}
}
