//-------------------------------------------------------------------------------------------------
// <copyright file="MmcCompiler.cs" company="Microsoft">
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
// The compiler for the Windows Installer XML Toolset MMC Extension.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Extensions
{
	using System;
	using System.Diagnostics;
	using System.Globalization;
	using System.IO;
	using System.Reflection;
	using System.Security.Cryptography;
	using System.Text;
	using System.Xml;
	using System.Xml.Schema;
	using Microsoft.Tools.WindowsInstallerXml;

	/// <summary>
	/// The compiler for the Windows Installer XML Toolset MMC Extension.
	/// </summary>
	public sealed class MmcCompiler : CompilerExtension
	{
		/// <summary>
		/// Instantiate a new MmcCompiler.
		/// </summary>
		public MmcCompiler()
		{
			Assembly assembly = Assembly.GetExecutingAssembly();
			XmlReader schemaReader = null;

			// Load the schema extensions.
			try
			{
				schemaReader = GetXmlFromEmbeddedStream(assembly, "Microsoft.Tools.WindowsInstallerXml.Extensions.Xsd.mmc.xsd");
				this.xmlSchema = XmlSchema.Read(schemaReader, null);
			}
			finally
			{
				if (null != schemaReader)
				{
					schemaReader.Close();
				}
			}

			this.tableDefinitionCollection = new TableDefinitionCollection();
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
		/// <param name="contextValues">Extra information about the context in which this element is being parsed.</param>
		public override void ParseElement(SourceLineNumberCollection sourceLineNumbers, XmlElement parentElement, XmlElement element)
		{
			switch (parentElement.LocalName)
			{
				case "File":
					switch (element.LocalName)
					{
						case "SnapIn":
							string fileName = parentElement.GetAttribute("LongName");
							if (null == fileName || String.Empty == fileName)
							{
								fileName = parentElement.GetAttribute("Name");
							}

							XmlElement grandparentElement = (XmlElement)parentElement.ParentNode;
							string componentId = grandparentElement.GetAttribute("Id");

							this.ParseSnapInElement(element, fileName, componentId);
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
		/// Generate an identifier by hashing data from the row.
		/// </summary>
		/// <param name="tableName">Name of the table for which the identifier is being generated.</param>
		/// <param name="args">Information to hash.</param>
		/// <returns>The generated identifier.</returns>
		private static string GenerateIdentifier(string tableName, params string[] args)
		{
			string stringData = String.Join("|", args);
			byte[] data = Encoding.Unicode.GetBytes(stringData);

			// hash the data
			byte[] hash;
			using (MD5 md5 = new MD5CryptoServiceProvider())
			{
				hash = md5.ComputeHash(data);
			}

			// select a prefix based on the element localname
			string prefix = null;
			switch (tableName)
			{
				case "Registry":
				case "RemoveRegistry":
					prefix = "reg";
					break;
				default:
					throw new InvalidOperationException("Invalid table name passed into GenerateIdentifier.");
			}
			Debug.Assert(3 >= prefix.Length, "Prefix for generated identifiers must be 3 characters long or less.");

			// build up the identifier
			StringBuilder identifier = new StringBuilder(35, 35);
			identifier.Append(prefix);
			for (int i = 0; i < hash.Length; i++)
			{
				identifier.Append(hash[i].ToString("X2", CultureInfo.InvariantCulture.NumberFormat));
			}

			return identifier.ToString();
		}

		/// <summary>
		/// Parses a SnapIn element.
		/// </summary>
		/// <param name="node">Element to process.</param>
		/// <param name="fileName">The name of the file which contains the snap-in.</param>
		/// <param name="componentId">Id of the MSI component to generate registry rows into.</param>
		private void ParseSnapInElement(XmlNode node, string fileName, string componentId)
		{
			SourceLineNumberCollection sourceLineNumbers = this.Core.GetSourceLineNumbers(node);
			string id = null;
			string aboutGuid = "{00000000-0000-0000-0000-000000000000}";
			string assemblyName = null;
			string classType = null;
			string defaultCulture = "neutral";
			string defaultPubKeyToken = "null"; // Yes, the string "null", not the value.
			string defaultVersion = "1.0.0.0";
			string description = null;
			string extensionType = null;
			string mmcVersion = "3.0.0.0";
			string name = null;
			string provider = null;
			string runtimeVersion = "v2.0.50727";

			foreach (XmlAttribute attrib in node.Attributes)
			{
				if (0 == attrib.NamespaceURI.Length || attrib.NamespaceURI == this.xmlSchema.TargetNamespace)
				{
					switch (attrib.LocalName)
					{
						case "Id":
							id = String.Concat("{", this.Core.GetAttributeGuidValue(sourceLineNumbers, attrib, false), "}");
							break;
						case "About":
							aboutGuid = String.Concat("{", this.Core.GetAttributeGuidValue(sourceLineNumbers, attrib, false), "}");
							break;
						case "AssemblyName":
							assemblyName = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
							break;
						case "ClassType":
							classType = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
							break;
						case "DefaultCulture":
							defaultCulture = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
							break;
						case "DefaultPublicKeyToken":
							defaultPubKeyToken = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
							break;
						case "DefaultVersion":
							defaultVersion = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
							break;
						case "Description":
							description = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
							break;
						case "ExtensionType":
							extensionType = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
							switch (extensionType)
							{
								case CompilerCore.IllegalEmptyAttributeValue:
									break;
								case "ContextMenu":
								case "NameSpace":
								case "PropertySheet":
								case "Task":
								case "ToolBar":
								case "View":
									break;
								default:
									this.Core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name, attrib.Name, extensionType, "ContextMenu", "NameSpace", "PropertySheet", "Task", "ToolBar", "View"));
									break;
							}
							break;
						case "MmcVersion":
							mmcVersion = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
							break;
						case "Name":
							name = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
							break;
						case "Provider":
							provider = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
							break;
						case "RuntimeVersion":
							runtimeVersion = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
							break;
						default:
							this.Core.UnexpectedAttribute(sourceLineNumbers, attrib);
							break;
					}
				}
				else
				{
					this.Core.UnexpectedAttribute(sourceLineNumbers, attrib);
				}
			}

			if (null == id)
			{
				this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}

			if (null == assemblyName)
			{
				if (fileName.EndsWith(".dll"))
				{
					assemblyName = fileName.Substring(0, fileName.Length - 4);
				}
				else
				{
					this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "AssemblyName"));
				}
			}

			if (null == classType)
			{
				this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "ClassType"));
			}

			if (null == name)
			{
				this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Name"));
			}

			int registryRoot = 2; // HKLM
			string snapInKey = String.Concat(@"Software\Microsoft\MMC\SnapIns\FX:", id);

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					if (child.NamespaceURI == this.xmlSchema.TargetNamespace)
					{
						switch (child.LocalName)
						{
							case "ExtendedNodeType":
								this.ParseExtendedNodeTypeElement(child, id, componentId, extensionType);
								break;
							case "PublishedNodeType":
								this.ParsePublishedNodeTypeElement(child, id, componentId);
								break;
							case "Resources":
								this.ParseResourcesElement(child, snapInKey, componentId);
								break;
							default:
								this.Core.UnexpectedElement(node, child);
								break;
						}
					}
					else
					{
						this.Core.UnexpectedElement(node, child);
					}
				}
			}

			// Write row for About value
			this.CreateRegistryRow(sourceLineNumbers, registryRoot, snapInKey, "About", aboutGuid, componentId);

			// Write row for ApplicationBase value
			this.CreateRegistryRow(sourceLineNumbers, registryRoot, snapInKey, "ApplicationBase", String.Concat("[$", componentId, "]"), componentId);

			// Write row for AssemblyName value
			this.CreateRegistryRow(sourceLineNumbers, registryRoot, snapInKey, "AssemblyName", assemblyName, componentId);

			if (null != description)
			{
				// Write row for Description value
				this.CreateRegistryRow(sourceLineNumbers, registryRoot, snapInKey, "Description", description, componentId);
			}

			// Write row for FxVersion value
			this.CreateRegistryRow(sourceLineNumbers, registryRoot, snapInKey, "FxVersion", mmcVersion, componentId);

			// Write row for ModuleName value
			this.CreateRegistryRow(sourceLineNumbers, registryRoot, snapInKey, "ModuleName", fileName, componentId);

			// Write row for NameString value
			// TODO: should we also support NameStringIndirect (which appears to be used for the localizable name)
			this.CreateRegistryRow(sourceLineNumbers, registryRoot, snapInKey, "NameString", name, componentId);

			if (null != provider)
			{
				// Write row for Provider value
				this.CreateRegistryRow(sourceLineNumbers, registryRoot, snapInKey, "Provider", provider, componentId);
			}

			// Write row for RuntimeVersion value
			this.CreateRegistryRow(sourceLineNumbers, registryRoot, snapInKey, "RuntimeVersion", runtimeVersion, componentId);

			// Write row for Type value
			string typeValue = String.Format(
				CultureInfo.InvariantCulture,
				"{0}, {1}, Version={2}, Culture={3}, PublicKeyToken={4}",
				classType,
				assemblyName,
				defaultVersion,
				defaultCulture,
				defaultPubKeyToken);
			this.CreateRegistryRow(sourceLineNumbers, registryRoot, snapInKey, "Type", typeValue, componentId);

			if (null == extensionType)
			{
				// Write row for Standalone key
				this.CreateRegistryRow(sourceLineNumbers, registryRoot, String.Concat(snapInKey, @"\Standalone"), null, null, componentId);
			}
			else
			{
				this.CreateRegistryRow(sourceLineNumbers, registryRoot, String.Concat(snapInKey, @"\Extension"), null, extensionType, componentId);
			}
		}

		/// <summary>
		/// Parses a Resources element.
		/// </summary>
		/// <param name="node">Element to process.</param>
		/// <param name="snapInKey">The key under which to create values.</param>
		/// <param name="componentId">Id of the MSI component to generate Registry rows into.</param>
		private void ParseResourcesElement(XmlNode node, string snapInKey, string componentId)
		{
			SourceLineNumberCollection sourceLineNumbers = this.Core.GetSourceLineNumbers(node);
			int descriptionId = CompilerCore.IntegerNotSet;
			int displayNameId = CompilerCore.IntegerNotSet;
			string dllName = null;
			string dllPath = null;
			int folderColorMask = CompilerCore.IntegerNotSet;
			int iconId = CompilerCore.IntegerNotSet;
			int largeFolderBitmapId = CompilerCore.IntegerNotSet;
			int smallFolderBitmapId = CompilerCore.IntegerNotSet;
			int smallFolderSelectedBitmapId = CompilerCore.IntegerNotSet;
			int vendorId = CompilerCore.IntegerNotSet;
			int versionId = CompilerCore.IntegerNotSet;


			foreach (XmlAttribute attrib in node.Attributes)
			{
				if (0 == attrib.NamespaceURI.Length || attrib.NamespaceURI == this.xmlSchema.TargetNamespace)
				{
					switch (attrib.LocalName)
					{
						case "DescriptionId":
							descriptionId = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
							break;
						case "DisplayNameId":
							displayNameId = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
							break;
						case "DllName":
							dllName = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
							break;
						case "FolderBitmapsColorMask":
							folderColorMask = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
							break;
						case "IconId":
							iconId = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
							break;
						case "LargeFolderBitmapId":
							largeFolderBitmapId = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
							break;
						case "SmallFolderBitmapId":
							smallFolderBitmapId = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
							break;
						case "SmallFolderSelectedBitmapId":
							smallFolderSelectedBitmapId = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
							break;
						case "VendorId":
							vendorId = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
							break;
						case "VersionId":
							versionId = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
							break;
						default:
							this.Core.UnexpectedAttribute(sourceLineNumbers, attrib);
							break;
					}
				}
				else
				{
					this.Core.UnexpectedAttribute(sourceLineNumbers, attrib);
				}
			}

			if (null == dllName)
			{
				this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "DllName"));
			}
			else
			{
				dllPath = String.Concat("@[$", componentId, "]", dllName);
			}

			// find unexpected child elements
			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.Core.UnexpectedElement(node, child);
				}
			}

			if (null == dllPath)
			{
				return;
			}

			int registryRoot = 2; // HKLM

			if (CompilerCore.IntegerNotSet != descriptionId)
			{
				string descriptionValue = String.Concat(dllPath, ",-", descriptionId);
				this.CreateRegistryRow(sourceLineNumbers, registryRoot, snapInKey, "DescriptionStringIndirect", descriptionValue, componentId);
			}

			if (CompilerCore.IntegerNotSet != displayNameId)
			{
				string displayNameValue = String.Concat(dllPath, ",-", displayNameId);
				this.CreateRegistryRow(sourceLineNumbers, registryRoot, snapInKey, "NameStringIndirect", displayNameValue, componentId);
			}

			if (CompilerCore.IntegerNotSet != vendorId)
			{
				string vendorValue = String.Concat(dllPath, ",-", vendorId);
				this.CreateRegistryRow(sourceLineNumbers, registryRoot, snapInKey, "ProviderStringIndirect", vendorValue, componentId);
			}

			if (CompilerCore.IntegerNotSet != versionId)
			{
				string versionValue = String.Concat(dllPath, ",-", versionId);
				this.CreateRegistryRow(sourceLineNumbers, registryRoot, snapInKey, "VersionStringIndirect", versionValue, componentId);
			}

			if (CompilerCore.IntegerNotSet != folderColorMask)
			{
				string value = String.Concat("#", folderColorMask);
				Row row = this.Core.CreateRow(sourceLineNumbers, "Registry");
				row[0] = GenerateIdentifier("Registry", componentId, registryRoot.ToString(CultureInfo.InvariantCulture.NumberFormat), snapInKey.ToLower(CultureInfo.InvariantCulture), "folderbitmapscolormask");
				row[1] = registryRoot;
				row[2] = snapInKey;
				row[3] = "FolderBitmapsColorMask";
				row[4] = value;
				row[5] = componentId;
			}

			if (CompilerCore.IntegerNotSet != iconId)
			{
				string iconValue = String.Concat(dllPath, ",-", iconId);
				this.CreateRegistryRow(sourceLineNumbers, registryRoot, snapInKey, "IconIndirect", iconValue, componentId);
			}

			if (CompilerCore.IntegerNotSet != largeFolderBitmapId)
			{
				string largeBitmapValue = String.Concat(dllPath, ",-", largeFolderBitmapId);
				this.CreateRegistryRow(sourceLineNumbers, registryRoot, snapInKey, "LargeFolderBitmapIndirect", largeBitmapValue, componentId);
			}

			if (CompilerCore.IntegerNotSet != smallFolderBitmapId)
			{
				string smallBitmapValue = String.Concat(dllPath, ",-", smallFolderBitmapId);
				this.CreateRegistryRow(sourceLineNumbers, registryRoot, snapInKey, "SmallFolderBitmapIndirect", smallBitmapValue, componentId);
			}

			if (CompilerCore.IntegerNotSet != smallFolderSelectedBitmapId)
			{
				string smallSelectedBitmapValue = String.Concat(dllPath, ",-", smallFolderSelectedBitmapId);
				this.CreateRegistryRow(sourceLineNumbers, registryRoot, snapInKey, "SmallSelectedFolderBitmapIndirect", smallSelectedBitmapValue, componentId);
			}
		}

		/// <summary>
		/// Parses a PublishedNodeType element.
		/// </summary>
		/// <param name="node">Element to process.</param>
		/// <param name="snapInId">Id of the snap-in for which this node type is an extension point.</param>
		/// <param name="componentId">Id of the MSI component to generate registry rows into.</param>
		private void ParsePublishedNodeTypeElement(XmlNode node, string snapInId, string componentId)
		{
			SourceLineNumberCollection sourceLineNumbers = this.Core.GetSourceLineNumbers(node);
			string id = null;
			string description = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				if (0 == attrib.NamespaceURI.Length || attrib.NamespaceURI == this.xmlSchema.TargetNamespace)
				{
					switch (attrib.LocalName)
					{
						case "Id":
							id = String.Concat("{", this.Core.GetAttributeGuidValue(sourceLineNumbers, attrib, false), "}");
							break;
						case "Description":
							description = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
							break;
						default:
							this.Core.UnexpectedAttribute(sourceLineNumbers, attrib);
							break;
					}
				}
				else
				{
					this.Core.UnexpectedAttribute(sourceLineNumbers, attrib);
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

			int registryRoot = 2; // HKLM
			string snapInKey = String.Concat(@"Software\Microsoft\MMC\SnapIns\FX:", snapInId, @"\NodeTypes\", id);
			string nodeTypeKey = String.Concat(@"Software\Microsoft\MMC\NodeTypes\", id);

			// Write row for NodeType value
			this.CreateRegistryRow(sourceLineNumbers, registryRoot, snapInKey, null, description, componentId);

			// Write row for NodeType extensible value
			this.CreateRegistryRow(sourceLineNumbers, registryRoot, nodeTypeKey, null, description, componentId);
		}

		/// <summary>
		/// Parses an ExtendedNodeType element.
		/// </summary>
		/// <param name="node">Element to process.</param>
		/// <param name="snapInId">Id of the snap-in for which this node type is an extension point.</param>
		/// <param name="componentId">Id of the MSI component to generate registry rows into.</param>
		/// <param name="extensionType">Type of the extension.</param>
		private void ParseExtendedNodeTypeElement(XmlNode node, string snapInId, string componentId, string extensionType)
		{
			SourceLineNumberCollection sourceLineNumbers = this.Core.GetSourceLineNumbers(node);
			string id = null;
			string description = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				if (0 == attrib.NamespaceURI.Length || attrib.NamespaceURI == this.xmlSchema.TargetNamespace)
				{
					switch (attrib.LocalName)
					{
						case "Id":
							id = String.Concat("{", this.Core.GetAttributeGuidValue(sourceLineNumbers, attrib, false), "}");
							break;
						case "Description":
							description = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
							break;
						default:
							this.Core.UnexpectedAttribute(sourceLineNumbers, attrib);
							break;
					}
				}
				else
				{
					this.Core.UnexpectedAttribute(sourceLineNumbers, attrib);
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

			int registryRoot = 2; // HKLM
			string nodeTypeKey = String.Concat(@"Software\Microsoft\MMC\NodeTypes\", id, @"\Extensions\", extensionType);

			// Write row for NodeType value
			this.CreateRegistryRow(sourceLineNumbers, registryRoot, nodeTypeKey, String.Concat("FX:", snapInId), description, componentId);
		}

		/// <summary>
		/// Creates a Registry row in the active section.
		/// </summary>
		/// <param name="sourceLineNumbers">Source and line number of the current row.</param>
		/// <param name="root">The registry entry root.</param>
		/// <param name="key">The registry entry key.</param>
		/// <param name="name">The registry entry name.</param>
		/// <param name="value">The registry entry value.</param>
		/// <param name="componentId">The component which will control installation/uninstallation of the registry entry.</param>
		private void CreateRegistryRow(SourceLineNumberCollection sourceLineNumbers, int root, string key, string name, string value, string componentId)
		{
			if (!this.Core.EncounteredError)
			{
				if (-1 > root || 3 < root || null == key || null == componentId)
				{
					throw new ArgumentException("Illegal arguments passed.");
				}

				// escape the leading '#' character for string registry values
				if (null != value && value.StartsWith("#"))
				{
					value = String.Concat("#", value);
				}

				Row row = this.Core.CreateRow(sourceLineNumbers, "Registry");
				row[0] = GenerateIdentifier("Registry", componentId, root.ToString(CultureInfo.InvariantCulture.NumberFormat), key.ToLower(CultureInfo.InvariantCulture), (null != name ? name.ToLower(CultureInfo.InvariantCulture) : name));
				row[1] = root;
				row[2] = key;
				row[3] = name;
				row[4] = value;
				row[5] = componentId;
			}
		}
	}
}
