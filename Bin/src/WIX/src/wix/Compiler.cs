//-------------------------------------------------------------------------------------------------
// <copyright file="Compiler.cs" company="Microsoft">
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
// Compiler core of the Windows Installer Xml toolset.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;
	using System.IO;
	using System.Text;
	using System.Collections;
	using System.Collections.Specialized;
	using System.Diagnostics;
	using System.Globalization;
	using System.Reflection;
	using System.Security.Cryptography;
	using System.Xml;
	using System.Xml.Schema;
	using Microsoft.Tools.WindowsInstallerXml.Msi.Interop;

	/// <summary>
	/// Compiler core of the Windows Installer Xml toolset.
	/// </summary>
	public sealed class Compiler
	{
		private TableDefinitionCollection tableDefinitions;
		private Hashtable extensions;
		private CompilerCore core;
		private ExtensionMessages extensionMessages;
		private PedanticLevel pedanticLevel;
		private XmlSchemaCollection schemas;
		private bool suppressValidation;

		// if these are true you know you are building a module or product
		// but if they are false you cannot not be sure they will not end
		// up a product or module.  Use these flags carefully.
		private bool compilingModule;
		private bool compilingProduct;

		private bool useShortFileNames;
		private string activeName;
		private string activeLanguage;

		// sql database attributes definitions (from sca.h)
		internal const int ScaDbCreateOnInstall = 0x00000001;
		internal const int ScaDbDropOnUninstall = 0x00000002;
		internal const int ScaDbContinueOnError = 0x00000004;
		internal const int ScaDbDropOnInstall = 0x00000008;
		internal const int ScaDbCreateOnUninstall = 0x00000010;
		internal const int ScaDbConfirmOverwrite = 0x00000020;
		internal const int ScaDbCreateOnReinstall = 0x00000040;
		internal const int ScaDbDropOnReinstall = 0x00000080;
		internal const int ScaSqlExecuteOnInstall = 0x00000001;
		internal const int ScaSqlExecuteOnUninstall = 0x00000002;
		internal const int ScaSqlContinueOnError = 0x00000004;
		internal const int ScaSqlRollback = 0x00000008;
		internal const int ScaSqlExecuteOnReinstall = 0x00000010;

		/// <summary>
		/// Creates a new compiler object with a default set of table definitions.
		/// </summary>
		/// <param name="smallTables">Use small table definitions for MSI/MSM.</param>
		public Compiler(bool smallTables)
		{
			this.tableDefinitions = Common.GetTableDefinitions(smallTables);
			this.extensions = new Hashtable();
		}

		/// <summary>
		/// Event for messages.
		/// </summary>
		public event MessageEventHandler Message;

		/// <summary>
		/// Types of permission setting methods.
		/// </summary>
		private enum PermissionType
		{
			/// <summary>LockPermissions (normal) type permission setting.</summary>
			LockPermissions,
			/// <summary>FileSharePermissions type permission setting.</summary>
			FileSharePermissions,
			/// <summary>SecureObjects type permission setting.</summary>
			SecureObjects,
		}

		/// <summary>
		/// Types of objects that custom HTTP Headers can be applied to.
		/// </summary>
		/// <remarks>Note that this must be kept in sync with the eHttpHeaderParentType in scahttpheader.h.</remarks>
		private enum HttpHeaderParentType
		{
			/// <summary>Custom HTTP Header is to be applied to a Web Virtual Directory.</summary>
			WebVirtualDir = 1,
			/// <summary>Custom HTTP Header is to be applied to a Web Site.</summary>
			WebSite = 2,
		}

		/// <summary>
		/// Types of objects that MimeMaps can be applied to.
		/// </summary>
		/// <remarks>Note that this must be kept in sync with the eMimeMapParentType in scamimemap.h.</remarks>
		private enum MimeMapParentType
		{
			/// <summary>MimeMap is to be applied to a Web Virtual Directory.</summary>
			WebVirtualDir = 1,
		}

		/// <summary>
		/// Types of objects that custom WebErrors can be applied to.
		/// </summary>
		/// <remarks>Note that this must be kept in sync with the eWebErrorParentType in scaweberr.h.</remarks>
		private enum WebErrorParentType
		{
			/// <summary>Custom WebError is to be applied to a Web Virtual Directory.</summary>
			WebVirtualDir = 1,
			/// <summary>Custom WebError is to be applied to a Web Site.</summary>
			WebSite = 2,
		}

		/// <summary>
		/// Target of shortcut when not advertised.
		/// </summary>
		private enum ShortcutType
		{
			/// <summary>Shortcut points at a directory.</summary>
			Directory,
			/// <summary>Shortcut points at a file.</summary>
			File
		}

		/// <summary>
		/// Binary type.
		/// </summary>
		private enum BinaryType
		{
			/// <summary>Binary</summary>
			Binary,
			/// <summary>Icon</summary>
			Icon,
		}

		/// <summary>
		/// Type of RadioButton element in a group.
		/// </summary>
		private enum RadioButtonType
		{
			/// <summary>Not set, yet.</summary>
			NotSet,
			/// <summary>Text</summary>
			Text,
			/// <summary>Bitmap</summary>
			Bitmap,
			/// <summary>Icon</summary>
			Icon,
		}

		/// <summary>
		/// Gets or sets the pedantic level.
		/// </summary>
		/// <value>The pedantic level.</value>
		public PedanticLevel PedanticLevel
		{
			get { return this.pedanticLevel; }
			set { this.pedanticLevel = value; }
		}

		/// <summary>
		/// Gets and sets if the compiler uses short names when processing MSI file names.
		/// </summary>
		/// <value>true if using short names for files in MSI format.</value>
		public bool ShortNames
		{
			get { return this.useShortFileNames; }
			set { this.useShortFileNames = value; }
		}

		/// <summary>
		/// Gets and sets if the source document should not be validated.
		/// </summary>
		/// <value>true if validation should be suppressed.</value>
		public bool SuppressValidate
		{
			get { return this.suppressValidation; }
			set { this.suppressValidation = value; }
		}

		/// <summary>
		/// Adds an extension to the compiler.
		/// </summary>
		/// <param name="extension">Compiler extension to add to compiler.</param>
		/// <remarks>Extension messages cannot be displayed from this method.</remarks>
		public void AddExtension(CompilerExtension extension)
		{
			// reset the cached schemas so that this extension gets added to the collection next time around
			this.schemas = null;

			// clone the table definition collection passed into the compiler to ensure that the external collection isn't corrupted
			if (0 == this.extensions.Count)
			{
				this.tableDefinitions = this.tableDefinitions.Clone();
			}

			// check if this extension is addding a schema namespace that already exists
			if (this.extensions.Contains(extension.Schema.TargetNamespace))
			{
				throw new WixExtensionNamespaceConflictException(extension, (CompilerExtension)this.extensions[extension.Schema.TargetNamespace]);
			}

			// check if the extension is adding a table that already exists
			foreach (TableDefinition tableDefinition in extension.TableDefinitions)
			{
				if (this.tableDefinitions.Contains(tableDefinition.Name))
				{
					throw new WixExtensionTableDefinitionConflictException(extension, tableDefinition);
				}
			}

			// add the extension and its table definitions to the compiler
			this.extensions.Add(extension.Schema.TargetNamespace, extension);
			foreach (TableDefinition tableDefinition in extension.TableDefinitions)
			{
				this.tableDefinitions.Add(tableDefinition);
			}
		}

		/// <summary>
		/// Compiles the provided Xml document into an intermediate object
		/// </summary>
		/// <param name="source">Source xml document to compile.</param>
		/// <param name="sourcePath">Optional original path to xml document on disk.</param>
		/// <returns>Intermediate object representing compiled source document.</returns>
		/// <remarks>This method is not thread-safe.</remarks>
		public Intermediate Compile(XmlDocument source, string sourcePath)
		{
			if (null == source)
			{
				throw new ArgumentNullException("source");
			}

			bool encounteredError = true; // assume we'll hit an error

			// create the intermediate
			Intermediate target = new Intermediate();
			target.SourcePath = sourcePath;

			// try to compile it
			try
			{
				this.core = new CompilerCore(target, this.tableDefinitions, this.Message);
				this.core.PedanticLevel = this.pedanticLevel;
				this.extensionMessages = new ExtensionMessages(this.core);

				foreach (CompilerExtension extension in this.extensions.Values)
				{
					extension.Core = this.core;
					extension.Messages = this.extensionMessages;
					extension.InitializeCompile();
				}

				// parse the document
				if ("Wix" == source.DocumentElement.LocalName)
				{
					this.ParseWixElement(source.DocumentElement);
				}
				else
				{
					this.core.OnMessage(WixErrors.InvalidDocumentElement(null, source.DocumentElement.Name, "source", "Wix"));
				}

				// perform schema validation if there were no errors and validation isn't suppressed
				if (!this.core.EncounteredError && !this.suppressValidation)
				{
					this.ValidateDocument(source);
				}
			}
			finally
			{
				encounteredError = this.core.EncounteredError;

				foreach (CompilerExtension extension in this.extensions.Values)
				{
					extension.FinalizeCompile();
					extension.Core = null;
				}
				this.core = null;
			}

			// return the compiled intermediate only if it completed successfully
			return (encounteredError ? null : target);
		}

		/// <summary>
		/// Generate an identifier by hashing data from the row.
		/// </summary>
		/// <param name="elementLocalName">Local name of the element.</param>
		/// <param name="args">Information to hash.</param>
		/// <returns>The generated identifier.</returns>
		private static string GenerateIdentifier(string elementLocalName, params string[] args)
		{
			// hash the data
			MD5 md5 = new MD5CryptoServiceProvider();
			string stringData = String.Concat(args);
			byte[] data = Encoding.Unicode.GetBytes(stringData);
			byte[] hash = md5.ComputeHash(data);

			// select a prefix based on the element localname
			string prefix = null;
			switch (elementLocalName)
			{
				case "Registry":
					prefix = "reg";
					break;
				default:
					Debug.Assert(true, "Invalid element localname passed into GenerateIdentifier.");
					break;
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
		/// Uppercases the first character of a string.
		/// </summary>
		/// <param name="s">String to uppercase first character of.</param>
		/// <returns>String with first character uppercased.</returns>
		private static string UppercaseFirstChar(string s)
		{
			if (0 == s.Length)
			{
				return s;
			}

			return String.Concat(s.Substring(0, 1).ToUpper(), s.Substring(1));
		}

		/// <summary>
		/// Converts an array to a magical bit.
		/// </summary>
		/// <param name="attributeNames">Array of attributes that map to bits.</param>
		/// <param name="attributeName">Name of attribute to check.</param>
		/// <param name="attributeValue">Value of attribute to check.</param>
		/// <returns>Bit set or -1 if not set.</returns>
		private static long NameToBit(string[] attributeNames, string attributeName, YesNoType attributeValue)
		{
			bool found = false;
			long bit = 1;

			if (null != attributeNames)   // if there are no attributes to check there is no point in doing all the work in here
			{
				for (int i = 0; i < attributeNames.Length; i++)
				{
					if (attributeName == attributeNames[i])   // found the match, bail
					{
						if (YesNoType.Yes != attributeValue) // if the value was "no", reset the bit back to zero
						{
							bit = 0;
						}

						found = true;
						break;
					}

					// keep walking up the bit flags
					if (0x40000000 == bit)
					{
						bit = 0x80000000; // TODO: VBScript didn't handle the transition to the high bit, does C#?
					}
					else
					{
						bit += bit; // square the bit
					}
				}
			}

			return found ? bit : -1;
		}

		/// <summary>
		/// Given a possible short and long file name, creat an msi filename value.
		/// </summary>
		/// <param name="name">The short file name.</param>
		/// <param name="longName">Possibly the long file name.</param>
		/// <returns>The value in the msi filename data type.</returns>
		private static string GetMsiFilenameValue(string name, string longName)
		{
			if (null != longName)
			{
				return String.Format(CultureInfo.InvariantCulture, "{0}|{1}", name, longName);
			}
			else
			{
				return name;
			}
		}

		/// <summary>
		/// Adds a search property to the active section.
		/// </summary>
		/// <param name="sourceLineNumbers">Current source/line number of processing.</param>
		/// <param name="property">Property to add to search.</param>
		/// <param name="signature">Signature for search.</param>
		private void AddAppSearch(SourceLineNumberCollection sourceLineNumbers, string property, string signature)
		{
			if (property.ToUpper() != property)
			{
				this.core.OnMessage(WixErrors.SearchPropertyNotUppercase(sourceLineNumbers, "Property", "Id", property));
			}

			Row row = this.core.CreateRow(sourceLineNumbers, "AppSearch");
			row[0] = property;
			row[1] = signature;
		}

		/// <summary>
		/// Adds a property to the active section.
		/// </summary>
		/// <param name="sourceLineNumbers">Current source/line number of processing.</param>
		/// <param name="property">Name of property to add.</param>
		/// <param name="value">Value of property.</param>
		/// <param name="admin">Flag if property is an admin property.</param>
		/// <param name="secure">Flag if property is a secure property.</param>
		/// <param name="hidden">Flag if property is to be hidden.</param>
		private void AddProperty(SourceLineNumberCollection sourceLineNumbers, string property, string value, bool admin, bool secure, bool hidden)
		{
			if (secure && property.ToUpper() != property)
			{
				this.core.OnMessage(WixErrors.SecurePropertyNotUppercase(sourceLineNumbers, "Property", "Id", property));
			}

			Row row = this.core.CreateRow(sourceLineNumbers, "Property");
			row[0] = property;
			row[1] = value;
			row[2] = admin ? '1' : '0';
			row[3] = secure ? '1' : '0';
			row[4] = hidden ? '1' : '0';
		}

		/// <summary>
		/// Adds a registry key to the active section.
		/// </summary>
		/// <param name="sourceLineNumbers">Current source/line number of processing.</param>
		/// <param name="id">Identifier for registry key.</param>
		/// <param name="root">Root for registry key.</param>
		/// <param name="key">Path for registry key.</param>
		/// <param name="name">Name for registry key.</param>
		/// <param name="value">Value for registry key.</param>
		/// <param name="componentId">Identifier of parent component.</param>
		private void AddRegistryKey(SourceLineNumberCollection sourceLineNumbers, string id, int root, string key, string name, string value, string componentId)
		{
			// if no id was provided, create one
			if (null == id)
			{
				id = GenerateIdentifier("Registry", componentId, root.ToString(CultureInfo.InvariantCulture.NumberFormat), key, name);
			}

			Row row = this.core.CreateRow(sourceLineNumbers, "Registry");
			row[0] = id;
			row[1] = root;
			row[2] = key;
			row[3] = name;
			row[4] = value;
			row[5] = componentId;
		}

		/// <summary>
		/// Adds a "implemented category" registry key to active section.
		/// </summary>
		/// <param name="sourceLineNumbers">Current source/line number of processing.</param>
		/// <param name="categoryId">GUID for category.</param>
		/// <param name="classId">ClassId for to mark "implemented".</param>
		/// <param name="componentId">Identifier of parent component.</param>
		private void RegisterImplementedCategories(SourceLineNumberCollection sourceLineNumbers, string categoryId, string classId, string componentId)
		{
			this.AddRegistryKey(sourceLineNumbers, null, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("CLSID\\", classId, "\\Implemented Categories\\", categoryId), "+", null, componentId);
		}

		/// <summary>
		/// Parses an application identifer element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="componentId">Identifier of parent component.</param>
		/// <param name="advertise">The required advertise state (set depending upon the parent).</param>
		/// <param name="fileServer">Optional file identifier for CLSID when not advertised.</param>
		/// <param name="typeLibId">Optional TypeLib GUID for CLSID.</param>
		/// <param name="typeLibVersion">Optional TypeLib Version for CLSID Interfaces (if any).</param>
		private void ParseAppIdElement(XmlNode node, string componentId, YesNoType advertise, string fileServer, string typeLibId, string typeLibVersion)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string appId = null;
			string remoteServerName = null;
			string localService = null;
			string serviceParameters = null;
			string dllSurrogate = null;
			YesNoType activateAtStorage = YesNoType.NotSet;
			YesNoType runAsInteractiveUser = YesNoType.NotSet;
			string description = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						appId = String.Concat("{", this.core.GetAttributeGuidValue(sourceLineNumbers, attrib, false), "}");
						break;
					case "ActivateAtStorage":
						activateAtStorage = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
						break;
					case "Advertise":
						YesNoType appIdAdvertise = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
						if ((YesNoType.No == advertise && YesNoType.Yes == appIdAdvertise) || (YesNoType.Yes == advertise && YesNoType.No == appIdAdvertise))
						{
							this.core.OnMessage(WixErrors.AppIdIncompatibleAdvertiseState(sourceLineNumbers, node.Name, attrib.Name, appIdAdvertise.ToString(CultureInfo.InvariantCulture.NumberFormat), advertise.ToString(CultureInfo.InvariantCulture.NumberFormat)));
						}
						advertise = appIdAdvertise;
						break;
					case "Description":
						description = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "DllSurrogate":
						dllSurrogate = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "LocalService":
						localService = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "RemoteServerName":
						remoteServerName = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "RunAsInteractiveUser":
						runAsInteractiveUser = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
						break;
					case "ServiceParameters":
						serviceParameters = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}
			if (null == appId)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}

			// if the advertise state has not been set, default to non-advertised
			if (YesNoType.NotSet == advertise)
			{
				advertise = YesNoType.No;
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					switch (child.LocalName)
					{
						case "Class":
							this.ParseClassElement(child, componentId, advertise, fileServer, typeLibId, typeLibVersion, appId);
							break;
						default:
							this.core.UnexpectedElement(node, child);
							break;
					}
				}
			}

			Debug.Assert((YesNoType.Yes == advertise) || (YesNoType.No == advertise) || (YesNoType.IllegalValue == advertise), "Unexpected YesNoType value encountered.");
			if (YesNoType.Yes == advertise)
			{
				if (null != description)
				{
					this.core.OnMessage(WixErrors.IllegalAttributeWhenAdvertised(sourceLineNumbers, node.Name, "Description"));
				}

				Row row = this.core.CreateRow(sourceLineNumbers, "AppId");
				row[0] = appId;
				row[1] = remoteServerName;
				row[2] = localService;
				row[3] = serviceParameters;
				row[4] = dllSurrogate;
				if (YesNoType.Yes == activateAtStorage)
				{
					row[5] = 1;
				}
				if (YesNoType.Yes == runAsInteractiveUser)
				{
					row[6] = 1;
				}
			}
			else if (YesNoType.No == advertise)
			{
				if (null != description)
				{
					this.AddRegistryKey(sourceLineNumbers, null, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("AppID\\", appId), null, description, componentId);
				}
				else
				{
					this.AddRegistryKey(sourceLineNumbers, null, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("AppID\\", appId), "+", null, componentId);
				}
				if (null != remoteServerName)
				{
					this.AddRegistryKey(sourceLineNumbers, null, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("AppID\\", appId), "RemoteServerName", remoteServerName, componentId);
				}
				if (null != localService)
				{
					this.AddRegistryKey(sourceLineNumbers, null, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("AppID\\", appId), "LocalService", localService, componentId);
				}
				if (null != serviceParameters)
				{
					this.AddRegistryKey(sourceLineNumbers, null, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("AppID\\", appId), "ServiceParameters", serviceParameters, componentId);
				}
				if (null != dllSurrogate)
				{
					this.AddRegistryKey(sourceLineNumbers, null, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("AppID\\", appId), "DllSurrogate", dllSurrogate, componentId);
				}
				if (YesNoType.Yes == activateAtStorage)
				{
					this.AddRegistryKey(sourceLineNumbers, null, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("AppID\\", appId), "ActivateAtStorage", "Y", componentId);
				}
				if (YesNoType.Yes == runAsInteractiveUser)
				{
					this.AddRegistryKey(sourceLineNumbers, null, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("AppID\\", appId), "RunAs", "Interactive User", componentId);
				}
			}
		}

		/// <summary>
		/// Parses an AssemblyName element.
		/// </summary>
		/// <param name="node">File element to parse.</param>
		/// <param name="componentId">Parent's component id.</param>
		private void ParseAssemblyName(XmlNode node, string componentId)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;
			string value = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Value":
						value = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}

			// find unexpected child elements
			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			Row row = this.core.CreateRow(sourceLineNumbers, "MsiAssemblyName");
			row[0] = componentId;
			row[1] = id;
			row[2] = value;
		}

		/// <summary>
		/// Parses a binary or icon element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="binaryType">"Binary" or "Icon" table type enum.</param>
		/// <returns>Identifier for the new row.</returns>
		private string ParseBinaryOrIconElement(XmlNode node, BinaryType binaryType)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;
			string sourceFile = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						break;
					case "SourceFile":
					case "src":
						if (null != sourceFile)
						{
							this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name, "SourceFile", "src"));
						}
						sourceFile = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}
			if (null == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
				id = CompilerCore.IllegalIdentifier;
			}
			else if (CompilerCore.IllegalIdentifier != id) // only check legal values
			{
				if (55 < id.Length)
				{
					this.core.OnMessage(WixErrors.BinaryOrIconIdentifierTooLong(sourceLineNumbers, node.Name, "Id", id));
				}
				else if (!this.compilingProduct && 18 < id.Length) // if we're not doing a product then we can't be sure that a binary identifier over 18 characters will fit
				{
					this.core.OnMessage(WixWarnings.BinaryOrIconIdentifierCannotBeModularized(sourceLineNumbers, node.Name, "Id", id));
				}
			}

			if (null == sourceFile)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "SourceFile"));
			}

			// find unexpected child elements
			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			Debug.Assert("Binary" == binaryType.ToString() || "Icon" == binaryType.ToString());
			Row row = this.core.CreateRow(sourceLineNumbers, binaryType.ToString());
			row[0] = id;
			row[1] = sourceFile;

			return id;
		}

		/// <summary>
		/// Parses a category element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="componentId">Identifier of parent component.</param>
		private void ParseCategoryElement(XmlNode node, string componentId)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;
			string appData = null;
			string feature = null;
			string qualifier = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = String.Concat("{", this.core.GetAttributeGuidValue(sourceLineNumbers, attrib, false), "}");
						break;
					case "AppData":
						appData = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Feature":
						feature = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						break;
					case "Qualifier":
						qualifier = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}
			if (null == qualifier)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Qualifier"));
			}

			// find unexpected child elements
			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			Row row = this.core.CreateRow(sourceLineNumbers, "PublishComponent");
			row[0] = id;
			row[1] = qualifier;
			row[2] = componentId;
			row[3] = appData;
			if (null == feature)
			{
				row[4] = Guid.Empty.ToString("B");
				this.core.AddFeatureBacklink(new FeatureBacklink(componentId, FeatureBacklinkType.PublishComponent, row.Symbol));
			}
			else
			{
				row[4] = feature;
				this.core.AddValidReference("Feature", feature);
			}
		}

		/// <summary>
		/// Parses a class element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="componentId">Identifier of parent component.</param>
		/// <param name="advertise">Optional Advertise State for the parent AppId element (if any).</param>
		/// <param name="fileServer">Optional file identifier for CLSID when not advertised.</param>
		/// <param name="typeLibId">Optional TypeLib GUID for CLSID.</param>
		/// <param name="typeLibVersion">Optional TypeLib Version for CLSID Interfaces (if any).</param>
		/// <param name="parentAppId">Optional parent AppId.</param>
		private void ParseClassElement(XmlNode node, string componentId, YesNoType advertise, string fileServer, string typeLibId, string typeLibVersion, string parentAppId)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);

			string appId = null;
			string argument = null;
			bool class16bit = false;
			bool class32bit = false;
			string classId = null;
			StringCollection context = new StringCollection();
			bool control = false;
			string defaultInprocHandler = null;
			string defaultProgId = null;
			string description = null;
			string fileTypeMask = null;
			string icon = null;
			string iconIndex = null;
			string insertable = null;
			bool programmable = false;
			YesNoType relativePath = YesNoType.NotSet;
			bool safeForInit = false;
			bool safeForScripting = false;
			string threadingModel = null;
			string version = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						classId = String.Concat("{", this.core.GetAttributeGuidValue(sourceLineNumbers, attrib, false), "}");
						break;
					case "Advertise":
						YesNoType classAdvertise = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
						if ((YesNoType.No == advertise && YesNoType.Yes == classAdvertise) || (YesNoType.Yes == advertise && YesNoType.No == classAdvertise))
						{
							this.core.OnMessage(WixErrors.AdvertiseStateMustMatch(sourceLineNumbers, classAdvertise.ToString(CultureInfo.InvariantCulture.NumberFormat), advertise.ToString(CultureInfo.InvariantCulture.NumberFormat)));
						}
						advertise = classAdvertise;
						break;
					case "AppId":
						if (null != parentAppId)
						{
							this.core.OnMessage(WixErrors.IllegalAttributeWhenNested(sourceLineNumbers, node.Name, attrib.Name, node.ParentNode.Name));
						}
						appId = String.Concat("{", this.core.GetAttributeGuidValue(sourceLineNumbers, attrib, false), "}");
						break;
					case "Argument":
						argument = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Context":
						string[] value = this.core.GetAttributeValue(sourceLineNumbers, attrib).Split("\r\n\t ".ToCharArray());
						for (int i = 0; i < value.Length; ++i)
						{
							if (0 == value[i].Length)
							{
								continue;
							}

							// check for duplicates in the list
							for (int j = 0; j < context.Count; ++j)
							{
								if (context[j] == value[i])
								{
									this.core.OnMessage(WixErrors.DuplicateContextValue(sourceLineNumbers, value[i]));
								}
							}

							// check if this context is 32 bit or not
							if (value[i].EndsWith("32"))
							{
								class32bit = true;
							}
							else
							{
								class16bit = true;
							}

							context.Add(value[i]);
						}
						break;
					case "Control":
						control = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
						break;
					case "Description":
						description = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Handler":
						defaultInprocHandler = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Icon":
						icon = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "IconIndex":
						iconIndex = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "RelativePath":
						relativePath = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
						break;

					// The following attributes result in rows always added to the Registry table rather than the Class table
					case "Insertable":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							insertable = "Insertable";
						}
						else
						{
							insertable = "NotInsertable";
						}
						break;
					case "Programmable":
						programmable = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
						break;
					case "SafeForInitializing":
						safeForInit = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
						break;
					case "SafeForScripting":
						safeForScripting = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
						break;
					case "Server":
						fileServer = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "ThreadingModel":
						threadingModel = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Version":
						version = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;

					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			// If the advertise state has not been set, default to non-advertised.
			if (YesNoType.NotSet == advertise)
			{
				advertise = YesNoType.No;
			}

			if (null == classId)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}
			if (0 == context.Count)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Context", classId));
			}

			// Local variables used strictly for child node processing.
			int fileTypeMaskIndex = 0;
			YesNoType firstProgIdForClass = YesNoType.Yes;

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					SourceLineNumberCollection childSourceLineNumbers = this.core.GetSourceLineNumbers(child);
					switch (child.LocalName)
					{
						case "FileTypeMask":
							if (YesNoType.Yes == advertise)
							{
								fileTypeMask = String.Concat(fileTypeMask, null == fileTypeMask ? String.Empty : ";", this.ParseFileTypeMaskElement(child));
							}
							else if (YesNoType.No == advertise)
							{
								this.AddRegistryKey(childSourceLineNumbers, null, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("FileType\\", classId, "\\", fileTypeMaskIndex.ToString(CultureInfo.InvariantCulture.NumberFormat)), String.Empty, this.ParseFileTypeMaskElement(child), componentId);
								fileTypeMaskIndex++;
							}
							break;
						case "Interface":
							this.ParseInterfaceElement(child, componentId, class16bit ? classId : null, class32bit ? classId : null, typeLibId, typeLibVersion);
							break;
						case "ProgId":
							bool foundExtension = false;
							string progId = this.ParseProgIdElement(child, componentId, advertise, classId, description, null, ref foundExtension, firstProgIdForClass);
							if (null == defaultProgId)
							{
								defaultProgId = progId;
							}
							firstProgIdForClass = YesNoType.No;
							break;
						default:
							this.core.UnexpectedElement(node, child);
							break;
					}
				}
			}

			// If this Class is being advertised.
			if (YesNoType.Yes == advertise)
			{
				if (null != fileServer)
				{
					this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name, "Server", "Advertise", "yes"));
				}

				if (null == appId && null != parentAppId)
				{
					appId = parentAppId;
				}

				// add a Class row for each context
				for (int i = 0; i < context.Count; ++i)
				{
					Row row = this.core.CreateRow(sourceLineNumbers, "Class");
					row[0] = classId;
					row[1] = context[i];
					row[2] = componentId;
					row[3] = defaultProgId;
					row[4] = description;
					if (null != appId)
					{
						row[5] = appId;
						this.core.AddValidReference("AppId", appId);
					}
					row[6] = fileTypeMask;
					if (null != icon)
					{
						row[7] = icon;
						this.core.AddValidReference("Icon", icon);
					}
					row[8] = iconIndex;
					row[9] = defaultInprocHandler;
					row[10] = argument;
					row[11] = Guid.Empty.ToString("B");
					if (YesNoType.Yes == relativePath)
					{
						row[12] = MsiInterop.MsidbClassAttributesRelativePath;
					}

					this.core.AddFeatureBacklink(new FeatureBacklink(componentId, FeatureBacklinkType.Class, row.Symbol));
				}
			}
			else if (YesNoType.No == advertise)
			{
				if (null == fileServer)
				{
					this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Server"));
				}
				if (null != appId) // need to use nesting (not a reference) for the unadvertised Class elements
				{
					this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name, "AppId", "Advertise", "no"));
				}

				// add the core registry keys for each context in the class
				for (int i = 0; i < context.Count; ++i)
				{
					if (null == argument)
					{
						this.AddRegistryKey(sourceLineNumbers, null, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("CLSID\\", classId, "\\", context[i]), String.Empty, String.Concat("[!", fileServer, "]"), componentId); // ClassId context
					}
					else
					{
						this.AddRegistryKey(sourceLineNumbers, null, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("CLSID\\", classId, "\\", context[i]), String.Empty, String.Concat("[!", fileServer, "] ", argument), componentId); // ClassId context
					}

					if (null != icon) // ClassId default icon
					{
						if (null != iconIndex)
						{
							icon = String.Concat(icon, ",", iconIndex);
						}
						this.AddRegistryKey(sourceLineNumbers, null, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("CLSID\\", classId, "\\", context[i], "\\DefaultIcon"), String.Empty, icon, componentId);
					}
				}

				if (null != parentAppId) // ClassId AppId (must be specified via nesting, not with the AppId attribute)
				{
					this.AddRegistryKey(sourceLineNumbers, null, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("CLSID\\", classId), "AppID", parentAppId, componentId);
				}

				if (null != description) // ClassId description
				{
					this.AddRegistryKey(sourceLineNumbers, null, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("CLSID\\", classId), String.Empty, description, componentId);
				}

				if (null != defaultInprocHandler)
				{
					switch (defaultInprocHandler) // ClassId Default Inproc Handler
					{
						case "1":
							this.AddRegistryKey(sourceLineNumbers, null, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("CLSID\\", classId, "\\InprocHandler"), String.Empty, "ole.dll", componentId);
							break;
						case "2":
							this.AddRegistryKey(sourceLineNumbers, null, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("CLSID\\", classId, "\\InprocHandler32"), String.Empty, "ole32.dll", componentId);
							break;
						case "3":
							this.AddRegistryKey(sourceLineNumbers, null, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("CLSID\\", classId, "\\InprocHandler"), String.Empty, "ole.dll", componentId);
							this.AddRegistryKey(sourceLineNumbers, null, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("CLSID\\", classId, "\\InprocHandler32"), String.Empty, "ole32.dll", componentId);
							break;
						default:
							this.AddRegistryKey(sourceLineNumbers, null, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("CLSID\\", classId, "\\InprocHandler32"), String.Empty, defaultInprocHandler, componentId);
							break;
					}
				}

				if (YesNoType.NotSet != relativePath) // ClassId's RelativePath
				{
					this.core.OnMessage(WixErrors.RelativePathForRegistryElement(sourceLineNumbers));
				}
			}

			if (null != threadingModel)
			{
				threadingModel = Compiler.UppercaseFirstChar(threadingModel);

				// add a threading model for each context in the class
				for (int i = 0; i < context.Count; ++i)
				{
					this.AddRegistryKey(sourceLineNumbers, null, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("CLSID\\", classId, "\\", context[i]), "ThreadingModel", threadingModel, componentId);
				}
			}

			if (null != typeLibId)
			{
				this.AddRegistryKey(sourceLineNumbers, null, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("CLSID\\", classId, "\\TypeLib"), null, typeLibId, componentId);
			}

			if (null != version)
			{
				this.AddRegistryKey(sourceLineNumbers, null, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("CLSID\\", classId, "\\Version"), null, version, componentId);
			}

			if (null != insertable)
			{
				// Add "*" for name so that any subkeys (shouldn't be any) are removed on uninstall.
				this.AddRegistryKey(sourceLineNumbers, null, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("CLSID\\", classId, "\\", insertable), "*", null, componentId);
			}

			if (control)
			{
				// Add "*" for name so that any subkeys (shouldn't be any) are removed on uninstall.
				this.AddRegistryKey(sourceLineNumbers, null, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("CLSID\\", classId, "\\Control"), "*", null, componentId);
			}

			if (programmable)
			{
				// Add "*" for name so that any subkeys (shouldn't be any) are removed on uninstall.
				this.AddRegistryKey(sourceLineNumbers, null, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("CLSID\\", classId, "\\Programmable"), "*", null, componentId);
			}

			if (safeForInit)
			{
				this.RegisterImplementedCategories(sourceLineNumbers, "{7DD95802-9882-11CF-9FA9-00AA006C42C4}", classId, componentId);
			}

			if (safeForScripting)
			{
				this.RegisterImplementedCategories(sourceLineNumbers, "{7DD95801-9882-11CF-9FA9-00AA006C42C4}", classId, componentId);
			}
		}

		/// <summary>
		/// Parses an Interface element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="componentId">Identifier of parent component.</param>
		/// <param name="proxyId">16-bit proxy for interface.</param>
		/// <param name="proxyId32">32-bit proxy for interface.</param>
		/// <param name="typeLibId">Optional TypeLib GUID for CLSID.</param>
		/// <param name="typelibVersion">Version of the TypeLib to which this interface belongs.  Required if typeLibId is specified</param>
		private void ParseInterfaceElement(XmlNode node, string componentId, string proxyId, string proxyId32, string typeLibId, string typelibVersion)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string baseInterface = null;
			string interfaceId = null;
			string name = null;
			int numMethods = CompilerCore.IntegerNotSet;
			bool versioned = true;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						interfaceId = String.Concat("{", this.core.GetAttributeGuidValue(sourceLineNumbers, attrib, false), "}");
						break;
					case "BaseInterface":
						baseInterface = String.Concat("{", this.core.GetAttributeGuidValue(sourceLineNumbers, attrib, false), "}");
						break;
					case "Name":
						name = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "NumMethods":
						numMethods = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						break;
					case "ProxyStubClassId":
						proxyId = String.Concat("{", this.core.GetAttributeGuidValue(sourceLineNumbers, attrib, false), "}");
						break;
					case "ProxyStubClassId32":
						proxyId32 = String.Concat("{", this.core.GetAttributeGuidValue(sourceLineNumbers, attrib, false), "}");
						break;
					case "Versioned":
						versioned = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == interfaceId)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}
			if (null == name)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Name"));
			}

			// find unexpected child elements
			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			this.AddRegistryKey(sourceLineNumbers, null, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("Interface\\", interfaceId), null, name, componentId);
			if (null != typeLibId)
			{
				this.AddRegistryKey(sourceLineNumbers, null, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("Interface\\", interfaceId, "\\TypeLib"), null, typeLibId, componentId);
				if (versioned)
				{
					this.AddRegistryKey(sourceLineNumbers, null, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("Interface\\", interfaceId, "\\TypeLib"), "Version", typelibVersion, componentId);
				}
			}
			if (null != baseInterface)
			{
				this.AddRegistryKey(sourceLineNumbers, null, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("Interface\\", interfaceId, "\\BaseInterface"), null, baseInterface, componentId);
			}
			if (CompilerCore.IntegerNotSet != numMethods)
			{
				this.AddRegistryKey(sourceLineNumbers, null, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("Interface\\", interfaceId, "\\NumMethods"), null, numMethods.ToString(CultureInfo.InvariantCulture.NumberFormat), componentId);
			}
			if (null != proxyId)
			{
				this.AddRegistryKey(sourceLineNumbers, null, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("Interface\\", interfaceId, "\\ProxyStubClsid"), null, proxyId, componentId);
			}
			if (null != proxyId32)
			{
				this.AddRegistryKey(sourceLineNumbers, null, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("Interface\\", interfaceId, "\\ProxyStubClsid32"), null, proxyId32, componentId);
			}
		}

		/// <summary>
		/// Parses a CLSID's file type mask element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <returns>String representing the file type mask elements.</returns>
		private string ParseFileTypeMaskElement(XmlNode node)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			int cb = 0;
			int offset = 0;
			string mask = null;
			string value = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Mask":
						mask = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Offset":
						offset = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						break;
					case "Value":
						value = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			// find unexpected child elements
			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			if (mask.Length != value.Length)
			{
				this.core.OnMessage(WixErrors.ValueAndMaskMustBeSameLength(sourceLineNumbers));
			}
			cb = mask.Length / 2;

			return String.Concat(offset.ToString(CultureInfo.InvariantCulture.NumberFormat), ",", cb.ToString(CultureInfo.InvariantCulture.NumberFormat), ",", mask, ",", value);
		}

		/// <summary>
		/// Parses a registry search element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <returns>Signature for search element.</returns>
		private string ParseRegistrySearchElement(XmlNode node)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;
			string key = null;
			string name = null;
			string signature = null;
			int root = CompilerCore.IntegerNotSet;
			int type = CompilerCore.IntegerNotSet;
			bool search64bit = false;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						break;
					case "Key":
						key = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Name":
						name = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Root":
						string rootValue = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						switch (rootValue)
						{
							case CompilerCore.IllegalEmptyAttributeValue: // this error is already handled
								break;
							case "HKCR":
								root = MsiInterop.MsidbRegistryRootClassesRoot;
								break;
							case "HKCU":
								root = MsiInterop.MsidbRegistryRootCurrentUser;
								break;
							case "HKLM":
								root = MsiInterop.MsidbRegistryRootLocalMachine;
								break;
							case "HKU":
								root = MsiInterop.MsidbRegistryRootUsers;
								break;
							default:
								this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name, "Root", rootValue, "HKCR", "HKCU", "HKLM", "HKU"));
								break;
						}
						break;
					case "Type":
						string typeValue = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						switch (typeValue)
						{
							case CompilerCore.IllegalEmptyAttributeValue: // this error is already handled
								break;
							case "directory":
								type = 0;
								break;
							case "file":
								type = 1;
								break;
							case "raw":
								type = 2;
								break;
							default:
								this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name, "Type", typeValue, "directory", "file", "raw"));
								break;
						}
						break;
					case "Win64":
						search64bit = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}
			if (null == key)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Key"));
			}
			if (CompilerCore.IntegerNotSet == root)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Root"));
			}
			if (CompilerCore.IntegerNotSet == type)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Type"));
			}

			signature = id;
			bool oneChild = false;
			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					switch (child.LocalName)
					{
						case "DirectorySearch":
							if (oneChild)
							{
								this.core.OnMessage(WixErrors.TooManySearchElements(sourceLineNumbers, node.Name));
							}
							oneChild = true;
							// directorysearch parentage should work like directory element, not the rest of the signature type because of the DrLocator.Parent column
							signature = this.ParseDirectorySearchElement(child, id);
							break;
						case "DirectorySearchRef":
							if (oneChild)
							{
								this.core.OnMessage(WixErrors.TooManySearchElements(sourceLineNumbers, node.Name));
							}
							oneChild = true;
							signature = this.ParseDirectorySearchRefElement(child, id);
							break;
						case "FileSearch":
							if (oneChild)
							{
								this.core.OnMessage(WixErrors.TooManySearchElements(sourceLineNumbers, node.Name));
							}
							oneChild = true;
							signature = this.ParseFileSearchElement(child, id);
							id = signature; // FileSearch signatures override parent signatures
							break;
						case "FileSearchRef":
							if (oneChild)
							{
								this.core.OnMessage(WixErrors.TooManySearchElements(sourceLineNumbers, node.Name));
							}
							oneChild = true;
							id = this.ParseFileSearchRefElement(child); // FileSearch signatures override parent signatures
							signature = null;
							break;
						default:
							this.core.UnexpectedElement(node, child);
							break;
					}
				}
			}

			Row row = this.core.CreateRow(sourceLineNumbers, "RegLocator");
			row[0] = id;
			row[1] = root;
			row[2] = key;
			row[3] = name;
			row[4] = search64bit ? (type | 16) : type;

			return signature;
		}

		/// <summary>
		/// Parses a registry search reference element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <returns>Signature of referenced search element.</returns>
		private string ParseRegistrySearchRefElement(XmlNode node)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}

			// find unexpected child elements
			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			this.core.AddValidReference("RegLocator", id);

			return id; // the id of the RegistrySearchRef element is its signature
		}

		/// <summary>
		/// Parses child elements for search signatures.
		/// </summary>
		/// <param name="node">Node whose children we are parsing.</param>
		/// <returns>Returns ArrayList of string signatures.</returns>
		private ArrayList ParseSearchSignatures(XmlNode node)
		{
			ArrayList signatures = new ArrayList();
			foreach (XmlNode child in node.ChildNodes)
			{
				string signature = null;
				if (XmlNodeType.Element == child.NodeType)
				{
					switch (child.LocalName)
					{
						case "ComplianceDrive":
							signature = this.ParseComplianceDriveElement(child);
							break;
						case "ComponentSearch":
							signature = this.ParseComponentSearchElement(child);
							break;
						case "DirectorySearch":
							signature = this.ParseDirectorySearchElement(child, String.Empty);
							break;
						case "DirectorySearchRef":
							signature = this.ParseDirectorySearchRefElement(child, String.Empty);
							break;
						case "FileSearch":
							signature = this.ParseFileSearchElement(child, String.Empty);
							break;
						case "IniFileSearch":
							signature = this.ParseIniFileSearchElement(child);
							break;
						case "RegistrySearch":
							signature = this.ParseRegistrySearchElement(child);
							break;
						case "RegistrySearchRef":
							signature = this.ParseRegistrySearchRefElement(child);
							break;
						default:
							this.core.UnexpectedElement(node, child);
							break;
					}
				}

				if (null != signature)
				{
					signatures.Add(signature);
				}
			}

			return signatures;
		}

		/// <summary>
		/// Parses a compliance drive element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <returns>Signature of nested search elements.</returns>
		private string ParseComplianceDriveElement(XmlNode node)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string signature = null;

			bool oneChild = false;
			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					SourceLineNumberCollection childSourceLineNumbers = this.core.GetSourceLineNumbers(node);
					switch (child.LocalName)
					{
						case "DirectorySearch":
							if (oneChild)
							{
								this.core.OnMessage(WixErrors.TooManySearchElements(childSourceLineNumbers, node.Name));
							}
							oneChild = true;
							signature = this.ParseDirectorySearchElement(child, "CCP_DRIVE");
							break;
						case "DirectorySearchRef":
							if (oneChild)
							{
								this.core.OnMessage(WixErrors.TooManySearchElements(childSourceLineNumbers, node.Name));
							}
							oneChild = true;
							signature = this.ParseDirectorySearchRefElement(child, "CCP_DRIVE");
							break;
						default:
							this.core.UnexpectedElement(node, child);
							break;
					}
				}
			}

			if (null == signature)
			{
				this.core.OnMessage(WixErrors.SearchElementRequired(sourceLineNumbers, node.Name));
			}

			return signature;
		}

		/// <summary>
		/// Parses a compilance check element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		private void ParseComplianceCheckElement(XmlNode node)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string signature = null;

			// see if this property is used for appSearch
			ArrayList signatures = this.ParseSearchSignatures(node);
			foreach (string sig in signatures)
			{
				// if we haven't picked a signature for this ComplianceCheck pick
				// this one
				if (null == signature)
				{
					signature = sig;
				}
				else if (signature != sig)
				{
					// all signatures under a ComplianceCheck must be the same
					this.core.OnMessage(WixErrors.MultipleIdentifiersFound(sourceLineNumbers, node.Name, sig, signature));
				}
			}

			if (null == signature)
			{
				this.core.OnMessage(WixErrors.SearchElementRequired(sourceLineNumbers, node.Name));
			}

			Row row = this.core.CreateRow(sourceLineNumbers, "CCPSearch");
			row[0] = signature;
		}

		/// <summary>
		/// Parses a component element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="directoryId">Identifier for component's directory.</param>
		/// <param name="srcPath">Source path for files up to this point.</param>
		private void ParseComponentElement(XmlNode node, string directoryId, string srcPath)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);

			string id = null;
			int bits = 0;
			int comPlusBits = CompilerCore.IntegerNotSet;
			int driverFlags = 0;
			bool driverInstall = false;
			int driverSequence = CompilerCore.IntegerNotSet;
			string guid = null;
			string condition = null;
			int diskId = CompilerCore.IntegerNotSet;
			int keyBits = 0;
			bool keyFound = false;
			string keyPath = null;
			bool win64 = false;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						break;
					case "ComPlusFlags":
						comPlusBits = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						break;
					case "DisableRegistryReflection":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							bits |= MsiInterop.MsidbComponentAttributesDisableRegistryReflection;
						}
						break;
					case "DiskId":
						diskId = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						break;
					case "DriverForceInstall":
						driverInstall = true;
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							driverFlags |= 1;
						}
						break;
					case "DriverPlugAndPlayPrompt":
						driverInstall = true;
						if (YesNoType.No == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							driverFlags |= 2;
						}
						break;
					case "DriverAddRemovePrograms":
						driverInstall = true;
						if (YesNoType.No == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							driverFlags |= 4;
						}
						break;
					case "DriverSequence":
						driverInstall = true;
						driverSequence = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						break;
					case "DriverLegacy":
						driverInstall = true;
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							driverFlags |= 8;
						}
						break;
					case "DriverDeleteFiles":
						driverInstall = true;
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							driverFlags |= 16;
						}
						break;
					case "Guid":
						guid = this.core.GetAttributeGuidValue(sourceLineNumbers, attrib, false, true);
						if (String.Empty != guid)
						{
							guid = String.Concat("{", guid, "}");
						}
						break;
					case "KeyPath":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							keyFound = true;
							keyPath = null;
							keyBits = 0;
						}
						break;
					case "Location":
						string location = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						switch (location)
						{
							case CompilerCore.IllegalEmptyAttributeValue: // this error is already handled
								break;
							case "either":
								bits |= MsiInterop.MsidbComponentAttributesOptional;
								break;
							case "local": // this is the default
								break;
							case "source":
								bits |= MsiInterop.MsidbComponentAttributesSourceOnly;
								break;
							default:
								this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name, attrib.Name, "either", "local", "source"));
								break;
						}
						break;
					case "NeverOverwrite":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							bits |= MsiInterop.MsidbComponentAttributesNeverOverwrite;
						}
						break;
					case "Permanent":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							bits |= MsiInterop.MsidbComponentAttributesPermanent;
						}
						break;
					case "SharedDllRefCount":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							bits |= MsiInterop.MsidbComponentAttributesSharedDllRefCount;
						}
						break;
					case "Transitive":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							bits |= MsiInterop.MsidbComponentAttributesTransitive;
						}
						break;
					case "Win64":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							bits |= MsiInterop.MsidbComponentAttributes64bit;
							win64 = true;
						}
						break;
					default:
						if (!this.TryExtensionParseForAttribute(sourceLineNumbers, (XmlElement)node, attrib))
						{
							this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						}
						break;
				}
			}

			if (null == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}


			if (CompilerCore.IntegerNotSet != diskId && CompilerCore.IllegalInteger != diskId)
			{
				if (1 > diskId)
				{
					this.core.OnMessage(WixErrors.DiskIdOutOfRange(sourceLineNumbers, node.Name, "DiskId", diskId));
				}
			}

			if (null == guid)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Guid"));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					bool keyPathSet = false;
					string keyPossible = null;
					int keyBit = 0;
					switch (child.LocalName)
					{
						case "AppId":
							this.ParseAppIdElement(child, id, YesNoType.NotSet, null, null, null);
							break;
						case "Category":
							this.ParseCategoryElement(child, id);
							break;
						case "Class":
							this.ParseClassElement(child, id, YesNoType.NotSet, null, null, null, null);
							break;
						case "Condition":
							if (null != condition)
							{
								SourceLineNumberCollection childSourceLineNumbers = this.core.GetSourceLineNumbers(node);
								this.core.OnMessage(WixErrors.TooManyChildren(childSourceLineNumbers, node.Name, child.Name));
							}
							condition = this.ParseConditionElement(child, node.LocalName, null, null);
							break;
						case "CopyFile":
							this.ParseCopyFileElement(child, id, null);
							break;
						case "CreateFolder":
							this.ParseCreateFolderElement(child, id, directoryId);
							break;
						case "Environment":
							this.ParseEnvironmentElement(child, id);
							break;
						case "Extension":
							this.ParseExtensionElement(child, id, YesNoType.NotSet, null);
							break;
						case "File":
							keyPathSet = this.ParseFileElement(child, id, directoryId, diskId, srcPath, out keyPossible, win64);
							keyBit = 0;
							break;
						case "IniFile":
							this.ParseIniFileElement(child, id);
							break;
						case "Interface":
							this.ParseInterfaceElement(child, id, null, null, null, null);
							break;
						case "IsolateComponent":
							this.ParseIsolateComponentElement(child, id);
							break;
						case "ODBCDataSource":
							keyPathSet = this.ParseODBCDataSource(child, id, null, out keyPossible);
							keyBit = MsiInterop.MsidbComponentAttributesODBCDataSource;
							break;
						case "ODBCDriver":
							this.ParseODBCDriverOrTranslator(child, id, null, this.tableDefinitions["ODBCDriver"]);
							break;
						case "ODBCTranslator":
							this.ParseODBCDriverOrTranslator(child, id, null, this.tableDefinitions["ODBCTranslator"]);
							break;
						case "ProgId":
							bool foundExtension = false;
							this.ParseProgIdElement(child, id, YesNoType.NotSet, null, null, null, ref foundExtension, YesNoType.NotSet);
							break;
						case "Registry":
							keyPathSet = this.ParseRegistryElement(child, id, CompilerCore.IntegerNotSet, null, out keyPossible);
							keyBit = MsiInterop.MsidbComponentAttributesRegistryKeyPath;
							break;
						case "RemoveFile":
							this.ParseRemoveFileElement(child, id, directoryId);
							break;
						case "RemoveFolder":
							this.ParseRemoveFolderElement(child, id, directoryId);
							break;
						case "ReserveCost":
							this.ParseReserveCostElement(child, id, directoryId);
							break;
						case "ServiceConfig":
							this.ParseServiceConfigElement(child, id, "Component", null);
							break;
						case "ServiceInstall":
							this.ParseServiceInstallElement(child, id);
							break;
						case "Shortcut":
							this.ParseShortcutElement(child, id, ShortcutType.Directory, directoryId);
							break;
						case "TypeLib":
							this.ParseTypeLibElement(child, id, null, win64);
							break;
						case "XmlFile":
							this.ParseXmlFileElement(child, id);
							break;
						// server elements
						case "Certificate":
							this.ParseCertificateElement(child, id);
							break;
						case "FileShare":
							this.ParseFileShareElement(child, id, directoryId);
							break;
						case "ServiceControl":
							this.ParseServiceControlElement(child, id);
							break;
						case "SqlDatabase":
							this.ParseSqlDatabaseElement(child, id);
							break;
						case "SqlScript":
							this.ParseSqlScriptElement(child, id, null);
							break;
						case "SqlString":
							this.ParseSqlStringElement(child, id, null);
							break;
						case "User":
							this.ParseUserElement(child, id);
							break;
						case "WebAppPool":
							this.ParseWebAppPoolElement(child, id);
							break;
						case "WebDir":
							this.ParseWebDirElement(child, id, null);
							break;
						case "WebFilter":
							this.ParseWebFilterElement(child, id, null);
							break;
						case "WebProperty":
							this.ParseWebPropertyElement(child, id);
							break;
						case "WebServiceExtension":
							this.ParseWebServiceExtensionElement(child, id);
							break;
						case "WebSite":
							this.ParseWebSiteElement(child, id);
							break;
						case "WebVirtualDir":
							this.ParseWebVirtualDirElement(child, id, null, null);
							break;
						default:
							if (!this.TryExtensionParseForElement(sourceLineNumbers, (XmlElement)node, (XmlElement)child))
							{
								this.core.UnexpectedElement(node, child);
							}
							break;
					}
					Debug.Assert(!keyPathSet || (keyPathSet && null != keyPossible));

					if (keyFound && keyPathSet)
					{
						this.core.OnMessage(WixErrors.ComponentMultipleKeyPaths(sourceLineNumbers, node.Name, "KeyPath", "yes", "File", "Registry", "ODBCDataSource"));
					}

					// if a possible KeyPath has been found and that value was explicitly set as
					// the KeyPath of the component, set it now.  Alternatively, if a possible
					// KeyPath has been found and no KeyPath has been previously set, use this
					// value as the default KeyPath of the component
					if (null != keyPossible && (keyPathSet || (null == keyPath && !keyFound)))
					{
						keyFound = keyPathSet;
						keyPath = keyPossible;
						keyBits = keyBit;
					}
				}
			}

			// check for implicit KeyPath which can easily be accidentally changed
			if (PedanticLevel.Legendary == this.pedanticLevel && !keyFound)
			{
				this.core.OnMessage(WixErrors.ImplicitComponentKeyPath(sourceLineNumbers, id));
			}

			// finally add the Component table row
			Row row = this.core.CreateRow(sourceLineNumbers, "Component");
			row[0] = id;
			row[1] = guid;
			row[2] = directoryId;
			row[3] = bits | keyBits;
			row[4] = condition;
			row[5] = keyPath;

			// if this is a module, automatically add this component to the references to ensure it gets in the ModuleComponents table
			if (this.compilingModule)
			{
				this.core.AddComplexReference(new ComplexReference(ComplexReferenceParentType.Module, this.activeName, this.activeLanguage, ComplexReferenceChildType.Component, id, false));
			}

			// Complus
			if (CompilerCore.IntegerNotSet != comPlusBits)
			{
				row = this.core.CreateRow(sourceLineNumbers, "Complus");
				row[0] = id;
				row[1] = comPlusBits;
			}

			// MsiDriverPackages
			if (driverInstall)
			{
				row = this.core.CreateRow(sourceLineNumbers, "MsiDriverPackages");
				row[0] = id;
				row[1] = driverFlags;
				if (CompilerCore.IntegerNotSet != driverSequence)
				{
					row[2] = driverSequence;
				}

				this.core.AddValidReference("CustomAction", "MsiProcessDrivers");
			}
		}

		/// <summary>
		/// Parses a component group element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		private void ParseComponentGroupElement(XmlNode node)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						if (!this.TryExtensionParseForAttribute(sourceLineNumbers, (XmlElement)node, attrib))
						{
							this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						}
						break;
				}
			}

			if (null == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					switch (child.LocalName)
					{
						case "ComponentRef":
							this.ParseComponentRefElement(child, node.LocalName, id, null);
							break;
						default:
							if (!this.TryExtensionParseForElement(sourceLineNumbers, (XmlElement)node, (XmlElement)child))
							{
								this.core.UnexpectedElement(node, child);
							}
							break;
					}
				}
			}

			Row row = this.core.CreateRow(sourceLineNumbers, "ComponentGroup");
			row[0] = id;
		}

		/// <summary>
		/// Parses a component group reference element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="parentLocalName">LocalName of parent element.</param>
		/// <param name="parentId">Identifier of parent element (usually a Feature or Module).</param>
		/// <param name="parentLanguage">Optional language of parent (only useful for Modules).</param>
		private void ParseComponentGroupRefElement(XmlNode node, string parentLocalName, string parentId, string parentLanguage)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;
			bool primary = false;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Primary":
						primary = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			if (null != id && CompilerCore.IllegalEmptyAttributeValue != id)
			{
				this.core.AddValidReference("ComponentGroup", id);

				ComplexReferenceParentType complexReferenceParentType;
				switch (parentLocalName)
				{
					case "ComponentGroup":
						complexReferenceParentType = ComplexReferenceParentType.ComponentGroup;
						break;
					case "Feature":
					case "FeatureRef":
						complexReferenceParentType = ComplexReferenceParentType.Feature;
						break;
					case "Module":
						complexReferenceParentType = ComplexReferenceParentType.Module;
						break;
					default:
						Debug.Fail("Unknown complex reference type.");
						complexReferenceParentType = ComplexReferenceParentType.Unknown;
						break;
				}
				this.core.AddComplexReference(new ComplexReference(complexReferenceParentType, parentId, parentLanguage, ComplexReferenceChildType.ComponentGroup, id, primary));
			}
		}

		/// <summary>
		/// Parses a custom action reference element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		private void ParseCustomActionRefElement(XmlNode node)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						break;
					default:
						if (!this.TryExtensionParseForAttribute(sourceLineNumbers, (XmlElement)node, attrib))
						{
							this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						}
						break;
				}
			}

			if (null == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			this.core.AddValidReference("CustomAction", id);
		}

		/// <summary>
		/// Parses a component reference element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="parentLocalName">LocalName of parent element.</param>
		/// <param name="parentId">Identifier of parent element (usually a Feature or Module).</param>
		/// <param name="parentLanguage">Optional language of parent (only useful for Modules).</param>
		private void ParseComponentRefElement(XmlNode node, string parentLocalName, string parentId, string parentLanguage)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;
			YesNoType primary = YesNoType.NotSet;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						break;
					case "Primary":
						primary = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}
			if ("ComponentGroup" == parentLocalName && YesNoType.Yes == primary)
			{
				this.core.OnMessage(WixErrors.ComponentRefCannotBePrimaryUnderComponentGroup(sourceLineNumbers, node.Name, "Primary", "yes", parentLocalName));
			}

			// find unexpected child elements
			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			if (null != id && CompilerCore.IllegalEmptyAttributeValue != id)
			{
				this.core.AddValidReference("Component", id);

				ComplexReferenceParentType complexReferenceParentType;
				switch (parentLocalName)
				{
					case "ComponentGroup":
						complexReferenceParentType = ComplexReferenceParentType.ComponentGroup;
						break;
					case "Feature":
					case "FeatureRef":
						complexReferenceParentType = ComplexReferenceParentType.Feature;
						break;
					case "Module":
						complexReferenceParentType = ComplexReferenceParentType.Module;
						break;
					default:
						Debug.Fail("Unknown complex reference type.");
						complexReferenceParentType = ComplexReferenceParentType.Unknown;
						break;
				}
				this.core.AddComplexReference(new ComplexReference(complexReferenceParentType, parentId, parentLanguage, ComplexReferenceChildType.Component, id, (YesNoType.Yes == primary)));
			}
		}

		/// <summary>
		/// Parses a component search element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <returns>Signature for search element.</returns>
		private string ParseComponentSearchElement(XmlNode node)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string signature = null;
			string id = null;
			string componentId = null;
			int type = 1;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						break;
					case "Guid":
						componentId = String.Concat("{", this.core.GetAttributeGuidValue(sourceLineNumbers, attrib, false), "}");
						break;
					case "Type":
						string typeValue = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						switch (typeValue)
						{
							case CompilerCore.IllegalEmptyAttributeValue: // this error is already handled
								break;
							case "directory":
								type = 0;
								break;
							case "file":
								type = 1;
								break;
							default:
								this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name, "Type", typeValue, "directory", "file"));
								break;
						}
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}

			signature = id;
			bool oneChild = false;
			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					switch (child.LocalName)
					{
						case "DirectorySearch":
							if (oneChild)
							{
								this.core.OnMessage(WixErrors.TooManySearchElements(sourceLineNumbers, node.Name));
							}
							oneChild = true;
							// directorysearch parentage should work like directory element, not the rest of the signature type because of the DrLocator.Parent column
							signature = this.ParseDirectorySearchElement(child, id);
							break;
						case "DirectorySearchRef":
							if (oneChild)
							{
								this.core.OnMessage(WixErrors.TooManySearchElements(sourceLineNumbers, node.Name));
							}
							oneChild = true;
							signature = this.ParseDirectorySearchRefElement(child, id);
							break;
						case "FileSearch":
							if (oneChild)
							{
								this.core.OnMessage(WixErrors.TooManySearchElements(sourceLineNumbers, node.Name));
							}
							oneChild = true;
							signature = this.ParseFileSearchElement(child, id);
							id = signature; // FileSearch signatures override parent signatures
							break;
						case "FileSearchRef":
							if (oneChild)
							{
								this.core.OnMessage(WixErrors.TooManySearchElements(sourceLineNumbers, node.Name));
							}
							oneChild = true;
							id = this.ParseFileSearchRefElement(child); // FileSearch signatures override parent signatures
							signature = null;
							break;
						default:
							this.core.UnexpectedElement(node, child);
							break;
					}
				}
			}

			Row row = this.core.CreateRow(sourceLineNumbers, "CompLocator");
			row[0] = id;
			row[1] = componentId;
			row[2] = type;

			return signature;
		}

		/// <summary>
		/// Parses a create folder element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="componentId">Identifier for parent component.</param>
		/// <param name="directoryId">Default identifier for directory to create.</param>
		private void ParseCreateFolderElement(XmlNode node, string componentId, string directoryId)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Directory":
						directoryId = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			// This code is roughly correct, but we don't want to force people
			//  to use SecureObjects in the way that this path does. Need to bring
			//  it back in when we have our secure object story worked out.
			// Count the Permission elements
			//            int permissionCount = 0;
			//            bool multiplePermissions = false;
			//            foreach (XmlNode child in node.ChildNodes)
			//            {
			//                if (child.LocalName == "Permission")
			//                {
			//                    permissionCount++;
			//                }
			//
			//                if (permissionCount > 1)
			//                {
			//                    multiplePermissions = true;
			//                    break;
			//                }
			//            }

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					switch (child.LocalName)
					{
						case "Shortcut":
							this.ParseShortcutElement(child, componentId, ShortcutType.Directory, directoryId);
							break;
						case "Permission":
							this.ParsePermissionElement(child, directoryId, "CreateFolder");
							break;
						default:
							this.core.UnexpectedElement(node, child);
							break;
					}
				}
			}

			Row row = this.core.CreateRow(sourceLineNumbers, "CreateFolder");
			row[0] = directoryId;
			row[1] = componentId;

			// add a reference to the directory
			this.core.AddValidReference("Directory", directoryId);
		}

		/// <summary>
		/// Parses a copy file element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="componentId">Identifier of parent component.</param>
		/// <param name="fileId">Identifier of file to copy (null if moving the file).</param>
		private void ParseCopyFileElement(XmlNode node, string componentId, string fileId)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;
			bool delete = false;
			string destinationDirectory = null;
			string destinationFolder = null;
			string destinationLongName = null;
			string destinationName = null;
			string destinationProperty = null;
			string sourceDirectory = null;
			string sourceFolder = null;
			string sourceName = null;
			string sourceProperty = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Delete":
						delete = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
						break;
					case "DestinationDirectory":
						destinationDirectory = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						this.core.AddValidReference("Directory", destinationDirectory);
						break;
					case "DestinationLongName":
						destinationLongName = this.core.GetAttributeLongFilename(sourceLineNumbers, attrib);
						break;
					case "DestinationName":
						destinationName = this.core.GetAttributeShortFilename(sourceLineNumbers, attrib);
						break;
					case "DestinationProperty":
						destinationProperty = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "FileId":
						if (null != fileId)
						{
							this.core.OnMessage(WixErrors.IllegalAttributeWhenNested(sourceLineNumbers, node.Name, attrib.Name, node.ParentNode.Name));
						}
						fileId = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "SourceDirectory":
						sourceDirectory = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						this.core.AddValidReference("Directory", sourceDirectory);
						break;
					case "SourceName":
						sourceName = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "SourceProperty":
						sourceProperty = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}
			if (null == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}
			if (null != sourceFolder && null != sourceDirectory) // SourceFolder and SourceDirectory cannot coexist
			{
				this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name, "SourceFolder", "SourceDirectory"));
			}
			if (null != sourceFolder && null != sourceProperty) // SourceFolder and SourceProperty cannot coexist
			{
				this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name, "SourceFolder", "SourceProperty"));
			}
			if (null != destinationFolder && null != destinationDirectory) // DestinationFolder and DestinationDirectory cannot coexist
			{
				this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name, "DestinationFolder", "DestinationDirectory"));
			}
			if (null != destinationFolder && null != destinationProperty) // DestinationFolder and DestinationDirectory cannot coexist
			{
				this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name, "DestinationFolder", "DestinationProperty"));
			}
			if (null != sourceDirectory && null != sourceProperty) // SourceDirectory and SourceProperty cannot coexist
			{
				this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name, "SourceProperty", "SourceDirectory"));
			}
			if (null != destinationDirectory && null != destinationProperty) // DestinationDirectory and DestinationProperty cannot coexist
			{
				this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name, "DestinationProperty", "DestinationDirectory"));
			}

			if (null != destinationLongName)
			{
				if (null == destinationName)
				{
					this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "DestinationName", "DestinationLongName"));
				}

				destinationName = String.Concat(destinationName, "|", destinationLongName);
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			if (null == fileId)
			{
				// DestinationDirectory or DestinationProperty must be specified
				if (null == destinationFolder && null == destinationDirectory && null == destinationProperty)
				{
					this.core.OnMessage(WixErrors.ExpectedAttributes(sourceLineNumbers, node.Name, "DestinationDirectory", "DestinationProperty", "FileId", "null"));
				}

				Row row = this.core.CreateRow(sourceLineNumbers, "MoveFile");
				row[0] = id;
				row[1] = componentId;
				row[2] = sourceName;
				row[3] = destinationName;
				if (null != sourceDirectory)
				{
					row[4] = sourceDirectory;
				}
				else if (null != sourceProperty)
				{
					row[4] = sourceProperty;
				}
				else
				{
					row[4] = sourceFolder;
				}
				if (null != destinationDirectory)
				{
					row[5] = destinationDirectory;
				}
				else if (null != destinationProperty)
				{
					row[5] = destinationProperty;
				}
				else
				{
					row[5] = destinationFolder;
				}
				row[6] = delete ? 1 : 0;
			}
			else // copy the file
			{
				if (null != sourceDirectory)
				{
					this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name, "SourceDirectory", "FileId"));
				}
				if (null != sourceFolder)
				{
					this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name, "SourceFolder", "FileId"));
				}
				if (null != sourceName)
				{
					this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name, "SourceName", "FileId"));
				}
				if (null != sourceProperty)
				{
					this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name, "SourceProperty", "FileId"));
				}
				if (delete)
				{
					this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name, "Delete", "FileId"));
				}
				if (null == destinationName && null == destinationFolder && null == destinationDirectory && null == destinationProperty)
				{
					this.core.OnMessage(WixWarnings.CopyFileFileIdUseless(sourceLineNumbers, WarningLevel.Moderate));
				}

				Row row = this.core.CreateRow(sourceLineNumbers, "DuplicateFile");
				row[0] = id;
				row[1] = componentId;
				row[2] = fileId;
				row[3] = destinationName;
				if (null != destinationDirectory)
				{
					row[4] = destinationDirectory;
				}
				else if (null != destinationProperty)
				{
					row[4] = destinationProperty;
				}
				else
				{
					row[4] = destinationFolder;
				}
			}
		}

		/// <summary>
		/// Parses a CustomAction element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		private void ParseCustomActionElement(XmlNode node)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;
			int bits = 0;
			bool inlineScript = false;
			string innerText = null;
			string source = null;
			int sourceBits = 0;
			string target = null;
			int targetBits = 0;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						break;
					case "BinaryKey":
						if (null != source)
						{
							this.core.OnMessage(WixErrors.CustomActionMultipleSources(sourceLineNumbers, node.Name, attrib.Name, "BinaryKey", "Directory", "FileKey", "Property", "Script"));
						}
						source = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						sourceBits = MsiInterop.MsidbCustomActionTypeBinaryData;
						this.core.AddValidReference("Binary", source); // add a reference to the appropriate Binary
						break;
					case "Directory":
						if (null != source)
						{
							this.core.OnMessage(WixErrors.CustomActionMultipleSources(sourceLineNumbers, node.Name, attrib.Name, "BinaryKey", "Directory", "FileKey", "Property", "Script"));
						}
						source = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						sourceBits = MsiInterop.MsidbCustomActionTypeDirectory;
						this.core.AddValidReference("Directory", source); // add a reference to the appropriate Directory
						break;
					case "DllEntry":
						if (null != target)
						{
							this.core.OnMessage(WixErrors.CustomActionMultipleTargets(sourceLineNumbers, node.Name, attrib.Name, "DllEntry", "Error", "ExeCommand", "JScriptCall", "Script", "Value", "VBScriptCall"));
						}
						target = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						targetBits = MsiInterop.MsidbCustomActionTypeDll;
						break;
					case "Error":
						if (null != target)
						{
							this.core.OnMessage(WixErrors.CustomActionMultipleTargets(sourceLineNumbers, node.Name, attrib.Name, "DllEntry", "Error", "ExeCommand", "JScriptCall", "Script", "Value", "VBScriptCall"));
						}
						target = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						targetBits = MsiInterop.MsidbCustomActionTypeTextData | MsiInterop.MsidbCustomActionTypeSourceFile;

						bool errorReference = true;

						try
						{
							// The target can be either a formatted error string or a literal
							// error number. Try to convert to error number to determine whether
							// to add a reference. No need to look at the value.
							int integer = Convert.ToInt32(target, CultureInfo.InvariantCulture.NumberFormat);
						}
						catch (FormatException)
						{
							errorReference = false;
						}
						catch (OverflowException)
						{
							errorReference = false;
						}

						if (errorReference)
						{
							this.core.AddValidReference("Error", target);
						}
						break;
					case "ExeCommand":
						if (null != target)
						{
							this.core.OnMessage(WixErrors.CustomActionMultipleTargets(sourceLineNumbers, node.Name, attrib.Name, "DllEntry", "Error", "ExeCommand", "JScriptCall", "Script", "Value", "VBScriptCall"));
						}
						target = this.core.GetAttributeValue(sourceLineNumbers, attrib, true); // one of the few cases where an empty string value is valid
						targetBits = MsiInterop.MsidbCustomActionTypeExe;
						break;
					case "Execute":
						string execute = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						switch (execute)
						{
							case CompilerCore.IllegalEmptyAttributeValue: // this error is already handled
								break;
							case "commit":
								bits |= MsiInterop.MsidbCustomActionTypeInScript | MsiInterop.MsidbCustomActionTypeCommit;
								break;
							case "deferred":
								bits |= MsiInterop.MsidbCustomActionTypeInScript;
								break;
							case "firstSequence":
								bits |= MsiInterop.MsidbCustomActionTypeFirstSequence;
								break;
							case "immediate":
								break;
							case "oncePerProcess":
								bits |= MsiInterop.MsidbCustomActionTypeOncePerProcess;
								break;
							case "rollback":
								bits |= MsiInterop.MsidbCustomActionTypeInScript | MsiInterop.MsidbCustomActionTypeRollback;
								break;
							case "secondSequence":
								bits |= MsiInterop.MsidbCustomActionTypeClientRepeat;
								break;
							default:
								this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name, attrib.Name, execute, "commit", "deferred", "firstSequence", "immediate", "oncePerProcess", "rollback", "secondSequence"));
								break;
						}
						break;
					case "FileKey":
						if (null != source)
						{
							this.core.OnMessage(WixErrors.CustomActionMultipleSources(sourceLineNumbers, node.Name, attrib.Name, "BinaryKey", "Directory", "FileKey", "Property", "Script"));
						}
						source = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						sourceBits = MsiInterop.MsidbCustomActionTypeSourceFile;
						this.core.AddValidReference("File", source); // add a reference to the appropriate File
						break;
					case "HideTarget":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							bits |= MsiInterop.MsidbCustomActionTypeHideTarget;
						}
						break;
					case "Impersonate":
						if (YesNoType.No == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							bits |= MsiInterop.MsidbCustomActionTypeNoImpersonate;
						}
						break;
					case "JScriptCall":
						if (null != target)
						{
							this.core.OnMessage(WixErrors.CustomActionMultipleTargets(sourceLineNumbers, node.Name, attrib.Name, "DllEntry", "Error", "ExeCommand", "JScriptCall", "Script", "Value", "VBScriptCall"));
						}
						target = this.core.GetAttributeValue(sourceLineNumbers, attrib, true); // one of the few cases where an empty string value is valid
						targetBits = MsiInterop.MsidbCustomActionTypeJScript;
						break;
					case "Property":
						if (null != source)
						{
							this.core.OnMessage(WixErrors.CustomActionMultipleSources(sourceLineNumbers, node.Name, attrib.Name, "BinaryKey", "Directory", "FileKey", "Property", "Script"));
						}
						source = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						sourceBits = MsiInterop.MsidbCustomActionTypeProperty;
						break;
					case "Return":
						string returnValue = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						switch (returnValue)
						{
							case CompilerCore.IllegalEmptyAttributeValue: // this error is already handled
								break;
							case "asyncNoWait":
								bits |= MsiInterop.MsidbCustomActionTypeAsync | MsiInterop.MsidbCustomActionTypeContinue;
								break;
							case "asyncWait":
								bits |= MsiInterop.MsidbCustomActionTypeAsync;
								break;
							case "check":
								break;
							case "ignore":
								bits |= MsiInterop.MsidbCustomActionTypeContinue;
								break;
							default:
								this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name, attrib.Name, returnValue, "asyncNoWait", "asyncWait", "check", "ignore"));
								break;
						}
						break;
					case "Script":
						if (null != source)
						{
							this.core.OnMessage(WixErrors.CustomActionMultipleSources(sourceLineNumbers, node.Name, attrib.Name, "BinaryKey", "Directory", "FileKey", "Property", "Script"));
						}
						if (null != target)
						{
							this.core.OnMessage(WixErrors.CustomActionMultipleTargets(sourceLineNumbers, node.Name, attrib.Name, "DllEntry", "Error", "ExeCommand", "JScriptCall", "Script", "Value", "VBScriptCall"));
						}

						// set the source and target to empty string for error messages when the user sets multiple sources or targets
						source = string.Empty;
						target = string.Empty;

						inlineScript = true;

						string script = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						switch (script)
						{
							case CompilerCore.IllegalEmptyAttributeValue: // this error is already handled
								break;
							case "jscript":
								sourceBits = MsiInterop.MsidbCustomActionTypeDirectory;
								targetBits = MsiInterop.MsidbCustomActionTypeJScript;
								break;
							case "vbscript":
								sourceBits = MsiInterop.MsidbCustomActionTypeDirectory;
								targetBits = MsiInterop.MsidbCustomActionTypeVBScript;
								break;
							default:
								this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name, attrib.Name, script, "jscript", "vbscript"));
								break;
						}
						break;
					case "TerminalServerAware":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							bits |= MsiInterop.MsidbCustomActionTypeTSAware;
						}
						break;
					case "Value":
						if (null != target)
						{
							this.core.OnMessage(WixErrors.CustomActionMultipleTargets(sourceLineNumbers, node.Name, attrib.Name, "DllEntry", "Error", "ExeCommand", "JScriptCall", "Script", "Value", "VBScriptCall"));
						}
						target = this.core.GetAttributeValue(sourceLineNumbers, attrib, true); // one of the few cases where an empty string value is valid
						targetBits = MsiInterop.MsidbCustomActionTypeTextData;
						break;
					case "VBScriptCall":
						if (null != target)
						{
							this.core.OnMessage(WixErrors.CustomActionMultipleTargets(sourceLineNumbers, node.Name, attrib.Name, "DllEntry", "Error", "ExeCommand", "JScriptCall", "Script", "Value", "VBScriptCall"));
						}
						target = this.core.GetAttributeValue(sourceLineNumbers, attrib, true); // one of the few cases where an empty string value is valid
						targetBits = MsiInterop.MsidbCustomActionTypeVBScript;
						break;
					case "Win64":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							bits |= MsiInterop.MsidbCustomActionType64BitScript;
						}
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			// get the inner text if any exists
			innerText = this.core.GetTrimmedInnerText(node);

			// if we have an in-lined Script CustomAction ensure no source or target attributes were provided
			if (inlineScript)
			{
				target = innerText;
			}
			else if (MsiInterop.MsidbCustomActionTypeVBScript == targetBits) // non-inline vbscript
			{
				if (null == source)
				{
					this.core.OnMessage(WixErrors.IllegalAttributeWithoutOtherAttributes(sourceLineNumbers, node.Name, "VBScriptCall", "BinaryKey", "FileKey", "Property"));
				}
				else if (MsiInterop.MsidbCustomActionTypeDirectory == sourceBits)
				{
					this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name, "VBScriptCall", "Directory"));
				}
			}
			else if (MsiInterop.MsidbCustomActionTypeJScript == targetBits) // non-inline jscript
			{
				if (null == source)
				{
					this.core.OnMessage(WixErrors.IllegalAttributeWithoutOtherAttributes(sourceLineNumbers, node.Name, "JScriptCall", "BinaryKey", "FileKey", "Property"));
				}
				else if (MsiInterop.MsidbCustomActionTypeDirectory == sourceBits)
				{
					this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name, "JScriptCall", "Directory"));
				}
			}
			else if (MsiInterop.MsidbCustomActionTypeTextData == (bits | sourceBits | targetBits))
			{
				this.core.OnMessage(WixErrors.IllegalAttributeWithoutOtherAttributes(sourceLineNumbers, node.Name, "Value", "Directory", "Property"));
			}
			else if (0 != innerText.Length) // inner text cannot be specified with non-script CAs
			{
				this.core.OnMessage(WixErrors.CustomActionIllegalInnerText(sourceLineNumbers, node.Name, innerText, "Script"));
			}

			if (MsiInterop.MsidbCustomActionType64BitScript == (bits & MsiInterop.MsidbCustomActionType64BitScript) && MsiInterop.MsidbCustomActionTypeVBScript != targetBits && MsiInterop.MsidbCustomActionTypeJScript != targetBits)
			{
				this.core.OnMessage(WixErrors.IllegalAttributeWithoutOtherAttributes(sourceLineNumbers, node.Name, "Win64", "Script", "VBScriptCall", "JScriptCall"));
			}

			if (0 == targetBits)
			{
				this.core.OnMessage(WixErrors.ExpectedAttributes(sourceLineNumbers, node.Name, "DllEntry", "Error", "ExeCommand", "JScriptCall", "Script", "Value", "VBScriptCall"));
			}

			// find unexpected child elements
			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			Row row = this.core.CreateRow(sourceLineNumbers, "CustomAction");
			row[0] = id;
			row[1] = bits | sourceBits | targetBits;
			row[2] = source;
			row[3] = target;
		}

		/// <summary>
		/// Parses an ensure table element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		private void ParseEnsureTableElement(XmlNode node)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}
			if (31 < id.Length)
			{
				this.core.OnMessage(WixErrors.TableNameTooLong(sourceLineNumbers, node.Name, "Id", id));
			}

			// find unexpected child elements
			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			this.EnsureTable(sourceLineNumbers, id);
		}

		/// <summary>
		/// Add the appropriate rows to make sure that the given table shows up
		/// in the resulting output.
		/// </summary>
		/// <param name="sourceLineNumbers">Source line numbers.</param>
		/// <param name="tableName">Name of the table to ensure existance of.</param>
		private void EnsureTable(SourceLineNumberCollection sourceLineNumbers, string tableName)
		{
			Row row = this.core.CreateRow(sourceLineNumbers, "EnsureTables");
			row[0] = tableName;

			// We don't add custom table definitions to the tableDefinitions collection,
			// so if it's not in there, it better be a custom table. If the Id is just wrong,
			// instead of a custom table, we get a WixUnresolvedReferenceException at
			// link time.
			try
			{
				TableDefinition tableDefinition = this.tableDefinitions[tableName];
			}
			catch
			{
				this.core.AddValidReference("CustomTables", tableName);
			}
		}

		/// <summary>
		/// Parses a custom table element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <remarks>not cleaned</remarks>
		private void ParseCustomTableElement(XmlNode node)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string tableId = null;

			string categories = null;
			int columnCount = 0;
			string columnNames = null;
			string columnTypes = null;
			string descriptions = null;
			string keyColumns = null;
			string keyTables = null;
			string maxValues = null;
			string minValues = null;
			string modularizations = null;
			string primaryKeys = null;
			string sets = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						tableId = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == tableId)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}
			if (31 < tableId.Length)
			{
				this.core.OnMessage(WixErrors.CustomTableNameTooLong(sourceLineNumbers, node.Name, "Id", tableId));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					SourceLineNumberCollection childSourceLineNumbers = this.core.GetSourceLineNumbers(child);
					switch (child.LocalName)
					{
						case "Column":
							++columnCount;

							string category = String.Empty;
							string columnName = null;
							string columnType = null;
							string description = String.Empty;
							int keyColumn = CompilerCore.IntegerNotSet;
							string keyTable = String.Empty;
							bool localizable = false;
							int maxValue = CompilerCore.IntegerNotSet;
							int minValue = CompilerCore.IntegerNotSet;
							string modularization = "None";
							bool nullable = false;
							bool primaryKey = false;
							string setValues = String.Empty;
							string typeName = null;
							int width = 0;

							foreach (XmlAttribute childAttrib in child.Attributes)
							{
								switch (childAttrib.LocalName)
								{
									case "Id":
										columnName = this.core.GetAttributeIdentifierValue(childSourceLineNumbers, childAttrib);
										break;
									case "Category":
										category = this.core.GetAttributeValue(childSourceLineNumbers, childAttrib);
										break;
									case "Description":
										description = this.core.GetAttributeValue(childSourceLineNumbers, childAttrib);
										break;
									case "KeyColumn":
										keyColumn = this.core.GetAttributeIntegerValue(childSourceLineNumbers, childAttrib);
										break;
									case "KeyTable":
										keyTable = this.core.GetAttributeValue(childSourceLineNumbers, childAttrib);
										break;
									case "Localizable":
										localizable = YesNoType.Yes == this.core.GetAttributeYesNoValue(childSourceLineNumbers, childAttrib);
										break;
									case "MaxValue":
										maxValue = this.core.GetAttributeIntegerValue(childSourceLineNumbers, childAttrib);
										break;
									case "MinValue":
										minValue = this.core.GetAttributeIntegerValue(childSourceLineNumbers, childAttrib);
										break;
									case "Modularize":
										modularization = this.core.GetAttributeValue(childSourceLineNumbers, childAttrib);
										break;
									case "Nullable":
										nullable = YesNoType.Yes == this.core.GetAttributeYesNoValue(childSourceLineNumbers, childAttrib);
										break;
									case "PrimaryKey":
										primaryKey = YesNoType.Yes == this.core.GetAttributeYesNoValue(childSourceLineNumbers, childAttrib);
										break;
									case "Set":
										setValues = this.core.GetAttributeValue(childSourceLineNumbers, childAttrib);
										break;
									case "Type":
										string typeValue = this.core.GetAttributeValue(childSourceLineNumbers, childAttrib);
										switch (typeValue)
										{
											case CompilerCore.IllegalEmptyAttributeValue:
												break;
											case "binary":
												typeName = "OBJECT";
												break;
											case "int":
												typeName = "SHORT";
												break;
											case "string":
												typeName = "CHAR";
												break;
											default:
												this.core.OnMessage(WixErrors.IllegalAttributeValue(childSourceLineNumbers, child.Name, "Type", typeValue, "binary", "int", "string"));
												break;
										}
										break;
									case "Width":
										width = this.core.GetAttributeIntegerValue(childSourceLineNumbers, childAttrib);
										break;
									default:
										this.core.UnexpectedAttribute(childSourceLineNumbers, childAttrib);
										break;
								}
							}

							if (null == columnName)
							{
								this.core.OnMessage(WixErrors.ExpectedAttribute(childSourceLineNumbers, child.Name, "Id"));
							}

							if (null == typeName)
							{
								this.core.OnMessage(WixErrors.ExpectedAttribute(childSourceLineNumbers, child.Name, "Type"));
							}
							else if ("SHORT" == typeName)
							{
								if (2 != width && 4 != width)
								{
									this.core.OnMessage(WixErrors.CustomTableIllegalColumnWidth(sourceLineNumbers, node.Name, "Width", width));
								}
								columnType = String.Concat(nullable ? "I" : "i", width);
							}
							else if ("CHAR" == typeName)
							{
								string typeChar = localizable ? "l" : "s";
								columnType = String.Concat(nullable ? typeChar.ToUpper(CultureInfo.InvariantCulture) : typeChar.ToLower(CultureInfo.InvariantCulture), width);
							}
							else
							{
								this.core.OnMessage(WixErrors.CustomTableColumnTypeNotSupported(sourceLineNumbers, node.Name, "Type", typeName));
							}

							foreach (XmlNode grandChild in child.ChildNodes)
							{
								if (XmlNodeType.Element == grandChild.NodeType)
								{
									this.core.UnexpectedElement(child, grandChild);
								}
							}

							columnNames = String.Concat(columnNames, null == columnNames ? String.Empty : "\t", columnName);
							columnTypes = String.Concat(columnTypes, null == columnTypes ? String.Empty : "\t", columnType);
							if (primaryKey)
							{
								primaryKeys = String.Concat(primaryKeys, null == primaryKeys ? String.Empty : "\t", columnName);
							}

							minValues = String.Concat(minValues, null == minValues ? String.Empty : "\t", CompilerCore.IntegerNotSet != minValue ? minValue.ToString(CultureInfo.InvariantCulture) : String.Empty);
							maxValues = String.Concat(maxValues, null == maxValues ? String.Empty : "\t", CompilerCore.IntegerNotSet != maxValue ? maxValue.ToString(CultureInfo.InvariantCulture) : String.Empty);
							keyTables = String.Concat(keyTables, null == keyTables ? String.Empty : "\t", keyTable);
							keyColumns = String.Concat(keyColumns, null == keyColumns ? String.Empty : "\t", CompilerCore.IntegerNotSet != keyColumn ? keyColumn.ToString(CultureInfo.InvariantCulture) : String.Empty);
							categories = String.Concat(categories, null == categories ? String.Empty : "\t", category);
							sets = String.Concat(sets, null == sets ? String.Empty : "\t", setValues);
							descriptions = String.Concat(descriptions, null == descriptions ? String.Empty : "\t", description);
							modularizations = String.Concat(modularizations, null == modularizations ? String.Empty : "\t", modularization);

							break;
						case "Row":
							string dataValue = null;
							int fieldCount = 0;

							foreach (XmlAttribute childAttrib in child.Attributes)
							{
								this.core.UnexpectedAttribute(childSourceLineNumbers, childAttrib);
							}

							foreach (XmlNode data in child.ChildNodes)
							{
								SourceLineNumberCollection dataSourceLineNumbers = this.core.GetSourceLineNumbers(data);
								switch (data.LocalName)
								{
									case "Data":
										columnName = null;
										foreach (XmlAttribute dataAttrib in data.Attributes)
										{
											switch (dataAttrib.LocalName)
											{
												case "Column":
													columnName = this.core.GetAttributeValue(dataSourceLineNumbers, dataAttrib);
													break;
												default:
													this.core.UnexpectedAttribute(dataSourceLineNumbers, dataAttrib);
													break;
											}
										}

										if (null == columnName)
										{
											this.core.OnMessage(WixErrors.ExpectedAttribute(dataSourceLineNumbers, data.Name, "Column"));
										}

										++fieldCount;
										dataValue = String.Concat(dataValue, null == dataValue ? String.Empty : "\t", columnName, ":", data.InnerText);
										break;
								}
							}

							this.core.AddValidReference("CustomTables", tableId);

							Row rowRow = this.core.CreateRow(childSourceLineNumbers, "RowData");
							rowRow[0] = tableId;
							rowRow[1] = fieldCount;
							rowRow[2] = dataValue;
							break;
						default:
							this.core.UnexpectedElement(node, child);
							break;
					}
				}
			}

			if (0 < columnCount)
			{
				if (null == primaryKeys || 0 == primaryKeys.Length)
				{
					this.core.OnMessage(WixErrors.CustomTableMissingPrimaryKey(sourceLineNumbers));
				}

				Row row = this.core.CreateRow(sourceLineNumbers, "CustomTables");
				row[0] = tableId;
				row[1] = columnCount;
				row[2] = columnNames;
				row[3] = columnTypes;
				row[4] = primaryKeys;
				row[5] = minValues;
				row[6] = maxValues;
				row[7] = keyTables;
				row[8] = keyColumns;
				row[9] = categories;
				row[10] = sets;
				row[11] = descriptions;
				row[12] = modularizations;
			}
		}

		/// <summary>
		/// Parses a directory element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="parentId">Optional identifier of parent directory.</param>
		/// <param name="fileSource">Path to source file as of yet.</param>
		private void ParseDirectoryElement(XmlNode node, string parentId, string fileSource)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;
			string name = null;
			string longName = null;
			string sourceName = null;
			string longSource = null;
			StringBuilder targetDir = null;
			string sourceDir = null;
			bool fileSourceAttribSet = false;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						break;
					case "FileSource":
					case "src":
						if (fileSourceAttribSet)
						{
							this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name, "FileSource", "src"));
						}
						fileSource = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						fileSourceAttribSet = true;
						break;
					case "LongName":
						longName = this.core.GetAttributeLongFilename(sourceLineNumbers, attrib);
						break;
					case "LongSource":
						longSource = this.core.GetAttributeLongFilename(sourceLineNumbers, attrib);
						break;
					case "Name":
						name = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						if ("." == name) // treat "." as a null value and add it back as "." later
						{
							name = null;
						}
						else if ("SourceDir" != name && CompilerCore.IllegalEmptyAttributeValue != name) // "SourceDir" has special handling in the linker
						{
							name = this.core.GetAttributeShortFilename(sourceLineNumbers, attrib);
						}
						break;
					case "SourceName":
						sourceName = this.core.GetAttributeShortFilename(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}
			if (CompilerCore.IllegalEmptyAttributeValue != name && CompilerCore.IllegalEmptyAttributeValue != longName)
			{
				if (null == name && null != longName)
				{
					this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Name", "LongName"));
				}
				else if (null != name && name == longName) // Name and LongName are identical
				{
					this.core.OnMessage(WixWarnings.DirectoryRedundantNames(sourceLineNumbers, node.Name, "Name", "LongName", name));
				}
				if (null == name && null == parentId)
				{
					this.core.OnMessage(WixErrors.DirectoryRootWithoutName(sourceLineNumbers, node.Name, "Name"));
				}
			}
			if (CompilerCore.IllegalEmptyAttributeValue != sourceName && CompilerCore.IllegalEmptyAttributeValue != longSource)
			{
				if (null == sourceName && null != longSource)
				{
					this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "SourceName", "LongSource"));
				}
				else if (null != sourceName && sourceName == longSource) // SourceName and LongSource are identical
				{
					this.core.OnMessage(WixWarnings.DirectoryRedundantNames(sourceLineNumbers, node.Name, "SourceName", "LongSource", sourceName));
				}
			}
			if (CompilerCore.IllegalEmptyAttributeValue != name && CompilerCore.IllegalEmptyAttributeValue != longName && CompilerCore.IllegalEmptyAttributeValue != sourceName && CompilerCore.IllegalEmptyAttributeValue != longSource &&
				name == sourceName && longName == longSource && (null != name || null != longName)) // source and target are identical
			{
				this.core.OnMessage(WixWarnings.DirectoryRedundantNames(sourceLineNumbers, node.Name, "SourceName", "LongSource"));
			}

			if (fileSourceAttribSet && !fileSource.EndsWith(Path.DirectorySeparatorChar.ToString()))
			{
				fileSource = String.Concat(fileSource, Path.DirectorySeparatorChar);
			}

			// track the src path
			if (!fileSourceAttribSet)
			{
				if (this.useShortFileNames)
				{
					if (null != sourceName)
					{
						fileSource = String.Format(CultureInfo.InvariantCulture, "{0}{1}{2}", fileSource, sourceName, Path.DirectorySeparatorChar);
					}
					else if (null != name)
					{
						fileSource = String.Format(CultureInfo.InvariantCulture, "{0}{1}{2}", fileSource, name, Path.DirectorySeparatorChar);
					}
				}
				else if (null != longSource)
				{
					fileSource = String.Format(CultureInfo.InvariantCulture, "{0}{1}{2}", fileSource, longSource, Path.DirectorySeparatorChar);
				}
				else
				{
					if (null != sourceName)
					{
						fileSource = String.Format(CultureInfo.InvariantCulture, "{0}{1}{2}", fileSource, sourceName, Path.DirectorySeparatorChar);
					}
					else if (null != longName)
					{
						fileSource = String.Format(CultureInfo.InvariantCulture, "{0}{1}{2}", fileSource, longName, Path.DirectorySeparatorChar);
					}
					else if (null != name)
					{
						fileSource = String.Format(CultureInfo.InvariantCulture, "{0}{1}{2}", fileSource, name, Path.DirectorySeparatorChar);
					}
				}
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					switch (child.LocalName)
					{
						case "Component":
							this.ParseComponentElement(child, id, fileSource);
							break;
						case "Directory":
							this.ParseDirectoryElement(child, id, fileSource);
							break;
						case "Merge":
							this.ParseMergeElement(child, id);
							break;
						default:
							this.core.UnexpectedElement(node, child);
							break;
					}
				}
			}

			// build the DefaultDir column
			targetDir = new StringBuilder((null == name ? "." : name));
			if (null != longName)
			{
				targetDir.AppendFormat(CultureInfo.InvariantCulture, "|{0}", longName);
			}

			if (null != sourceName)
			{
				sourceDir = GetMsiFilenameValue(sourceName, longSource);
			}
			if (null != sourceDir)
			{
				targetDir.Append(':');
				targetDir.Append(sourceDir);
			}

			Row row = this.core.CreateRow(sourceLineNumbers, "Directory");
			row[0] = id;
			row[1] = parentId;
			row[2] = targetDir.ToString();
		}

		/// <summary>
		/// Parses a directory reference element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		private void ParseDirectoryRefElement(XmlNode node)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;
			string fileSource = String.Empty;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						break;
					case "FileSource":
					case "src":
						if (0 != fileSource.Length)
						{
							this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name, "FileSource", "src"));
						}
						fileSource = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}

			if (!fileSource.EndsWith(Path.DirectorySeparatorChar.ToString()))
			{
				fileSource = String.Concat(fileSource, Path.DirectorySeparatorChar);
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					switch (child.LocalName)
					{
						case "Component":
							this.ParseComponentElement(child, id, fileSource);
							break;
						case "Directory":
							this.ParseDirectoryElement(child, id, fileSource);
							break;
						case "Merge":
							this.ParseMergeElement(child, id);
							break;
						default:
							this.core.UnexpectedElement(node, child);
							break;
					}
				}
			}

			this.core.AddValidReference("Directory", id);
		}

		/// <summary>
		/// Parses a directory search element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="parentSignature">Signature of parent search element.</param>
		/// <returns>Signature of search element.</returns>
		private string ParseDirectorySearchElement(XmlNode node, string parentSignature)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;
			int depth = CompilerCore.IntegerNotSet;
			string path = null;
			string signature = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						break;
					case "Depth":
						depth = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						break;
					case "Path":
						path = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			signature = id;

			bool oneChild = false;
			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					SourceLineNumberCollection childSourceLineNumbers = this.core.GetSourceLineNumbers(child);
					switch (child.LocalName)
					{
						case "DirectorySearch":
							if (oneChild)
							{
								this.core.OnMessage(WixErrors.TooManySearchElements(childSourceLineNumbers, node.Name));
							}
							oneChild = true;
							signature = this.ParseDirectorySearchElement(child, id);
							break;
						case "DirectorySearchRef":
							if (oneChild)
							{
								this.core.OnMessage(WixErrors.TooManySearchElements(childSourceLineNumbers, node.Name));
							}
							oneChild = true;
							signature = this.ParseDirectorySearchRefElement(child, id);
							break;
						case "FileSearch":
							if (oneChild)
							{
								this.core.OnMessage(WixErrors.TooManySearchElements(sourceLineNumbers, node.Name));
							}
							oneChild = true;
							signature = this.ParseFileSearchElement(child, id);
							id = signature; // FileSearch signatures override parent signatures
							break;
						case "FileSearchRef":
							if (oneChild)
							{
								this.core.OnMessage(WixErrors.TooManySearchElements(sourceLineNumbers, node.Name));
							}
							oneChild = true;
							id = this.ParseFileSearchRefElement(child); // FileSearch signatures override parent signatures
							signature = null;
							break;
						default:
							this.core.UnexpectedElement(node, child);
							break;
					}
				}
			}

			Row row = this.core.CreateRow(sourceLineNumbers, "DrLocator");
			row[0] = id;
			row[1] = parentSignature;
			row[2] = path;
			if (CompilerCore.IntegerNotSet != depth)
			{
				row[3] = depth;
			}

			return signature;
		}

		/// <summary>
		/// Parses a directory search reference element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="parentSignature">Signature of parent search element.</param>
		/// <returns>Signature of search element.</returns>
		private string ParseDirectorySearchRefElement(XmlNode node, string parentSignature)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;
			string parent = null;
			string path = null;
			string signature = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						break;
					case "Parent":
						parent = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						break;
					case "Path":
						path = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			signature = id;

			if (null != parent && 0 < parent.Length)
			{
				if (null != parentSignature && 0 < parentSignature.Length)
				{
					this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, id, parent, parentSignature));
				}
				else
				{
					parentSignature = parent;
				}
			}

			bool oneChild = false;
			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					SourceLineNumberCollection childSourceLineNumbers = this.core.GetSourceLineNumbers(child);
					switch (child.LocalName)
					{
						case "DirectorySearch":
							if (oneChild)
							{
								this.core.OnMessage(WixErrors.TooManySearchElements(childSourceLineNumbers, node.Name));
							}
							oneChild = true;
							signature = this.ParseDirectorySearchElement(child, id);
							break;
						case "DirectorySearchRef":
							if (oneChild)
							{
								this.core.OnMessage(WixErrors.TooManySearchElements(childSourceLineNumbers, node.Name));
							}
							oneChild = true;
							signature = this.ParseDirectorySearchRefElement(child, id);
							break;
						case "FileSearch":
							if (oneChild)
							{
								this.core.OnMessage(WixErrors.TooManySearchElements(childSourceLineNumbers, node.Name));
							}
							oneChild = true;
							signature = this.ParseFileSearchElement(child, id);
							id = signature; // FileSearch signatures override parent signatures
							break;
						case "FileSearchRef":
							if (oneChild)
							{
								this.core.OnMessage(WixErrors.TooManySearchElements(sourceLineNumbers, node.Name));
							}
							oneChild = true;
							id = this.ParseFileSearchRefElement(child); // FileSearch signatures override parent signatures
							signature = null;
							break;
						default:
							this.core.UnexpectedElement(node, child);
							break;
					}
				}
			}

			this.core.AddValidReference("DrLocator", String.Concat(id, "/", parentSignature, "/", path));
			return signature;
		}

		/// <summary>
		/// Parses a feature element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="parentId">Optional identifer for parent feature.</param>
		/// <param name="lastDisplay">Display value for last feature used to get the features to display in the same order as specified
		/// in the source code.</param>
		private void ParseFeatureElement(XmlNode node, string parentId, ref int lastDisplay)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;
			string allowAdvertise = null;
			int bits = 0;
			string configurableDirectory = null;
			string description = null;
			string display = "collapse";
			YesNoType followParent = YesNoType.NotSet;
			string installDefault = null;
			int level = CompilerCore.IntegerNotSet;
			string title = null;
			string typicalDefault = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						break;
					case "Absent":
						string absent = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						switch (absent)
						{
							case CompilerCore.IllegalEmptyAttributeValue: // this error is already handled
								break;
							case "allow": // this is the default
								break;
							case "disallow":
								bits = bits | MsiInterop.MsidbFeatureAttributesUIDisallowAbsent;
								break;
							default:
								this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name, attrib.Name, absent, "allow", "disallow"));
								break;
						}
						break;
					case "AllowAdvertise":
						allowAdvertise = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						switch (allowAdvertise)
						{
							case CompilerCore.IllegalEmptyAttributeValue: // this error is already handled
								break;
							case "no":
								bits |= MsiInterop.MsidbFeatureAttributesDisallowAdvertise;
								break;
							case "system":
								bits |= MsiInterop.MsidbFeatureAttributesNoUnsupportedAdvertise;
								break;
							case "yes": // this is the default
								break;
							default:
								this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name, attrib.Name, allowAdvertise, "no", "system", "yes"));
								break;
						}
						break;
					case "ConfigurableDirectory":
						configurableDirectory = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						this.core.AddValidReference("Directory", configurableDirectory);
						break;
					case "Description":
						description = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Display":
						display = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "InstallDefault":
						installDefault = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						switch (installDefault)
						{
							case CompilerCore.IllegalEmptyAttributeValue: // this error is already handled
								break;
							case "followParent":
								bits = bits | MsiInterop.MsidbFeatureAttributesFollowParent;
								break;
							case "local": // this is the default
								break;
							case "source":
								bits = bits | MsiInterop.MsidbFeatureAttributesFavorSource;
								break;
							default:
								this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name, attrib.Name, installDefault, "followParent", "local", "source"));
								break;
						}
						break;
					case "Level":
						level = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						break;
					case "Title":
						title = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "TypicalDefault":
						typicalDefault = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						switch (typicalDefault)
						{
							case CompilerCore.IllegalEmptyAttributeValue: // this error is already handled
								break;
							case "advertise":
								bits = bits | MsiInterop.MsidbFeatureAttributesFavorAdvertise;
								break;
							case "install": // this is the default
								break;
							default:
								this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name, attrib.Name, typicalDefault, "advertise", "install"));
								break;
						}
						break;
					default:
						if (!this.TryExtensionParseForAttribute(sourceLineNumbers, (XmlElement)node, attrib))
						{
							this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						}
						break;
				}
			}

			if (null == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}
			if (null != configurableDirectory && configurableDirectory.ToUpper() != configurableDirectory)
			{
				this.core.OnMessage(WixErrors.FeatureConfigurableDirectoryNotUppercase(sourceLineNumbers, node.Name, "ConfigurableDirectory", configurableDirectory));
			}
			if (CompilerCore.IntegerNotSet == level)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Level"));
			}
			if ("advertise" == typicalDefault && "no" == allowAdvertise)
			{
				this.core.OnMessage(WixErrors.FeatureCannotFavorAndDisallowAdvertise(sourceLineNumbers, node.Name, "TypicalDefault", typicalDefault, "AllowAdvertise", allowAdvertise));
			}
			if (YesNoType.Yes == followParent && ("local" == installDefault || "source" == installDefault))
			{
				this.core.OnMessage(WixErrors.FeatureCannotFollowParentAndFavorLocalOrSource(sourceLineNumbers, node.Name, "InstallDefault", "FollowParent", "yes"));
			}

			int childDisplay = 0;
			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					switch (child.LocalName)
					{
						case "ComponentGroupRef":
							this.ParseComponentGroupRefElement(child, node.LocalName, id, null);
							break;
						case "ComponentRef":
							this.ParseComponentRefElement(child, node.LocalName, id, null);
							break;
						case "Condition":
							this.ParseConditionElement(child, node.LocalName, id, null);
							break;
						case "Feature":
							this.ParseFeatureElement(child, id, ref childDisplay);
							break;
						case "FeatureRef":
							this.ParseFeatureRefElement(child, id);
							break;
						case "MergeRef":
							this.ParseMergeRefElement(child, id);
							break;
						default:
							if (!this.TryExtensionParseForElement(sourceLineNumbers, (XmlElement)node, (XmlElement)child))
							{
								this.core.UnexpectedElement(node, child);
							}
							break;
					}
				}
			}

			Row row = this.core.CreateRow(sourceLineNumbers, "Feature");
			row[0] = id;
			row[1] = parentId;
			row[2] = title;
			row[3] = description;
			switch (display)
			{
				case CompilerCore.IllegalEmptyAttributeValue: // this error is already handled
					break;
				case "collapse":
					lastDisplay = (lastDisplay | 1) + 1;
					row[4] = lastDisplay;
					break;
				case "expand":
					lastDisplay = (lastDisplay + 1) | 1;
					row[4] = lastDisplay;
					break;
				case "hidden":
					row[4] = 0;
					break;
				default:
					try
					{
						row[4] = Convert.ToInt32(display, CultureInfo.InvariantCulture);

						// save the display value of this row (if its not hidden) for subsequent rows
						if (0 != (int)row[4])
						{
							lastDisplay = (int)row[4];
						}
					}
					catch (Exception)
					{
						this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name, "Display", display, "collapse", "expand", "hidden"));
					}
					break;
			}
			row[5] = level;
			row[6] = configurableDirectory;
			row[7] = bits;
		}

		/// <summary>
		/// Parses a feature reference element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="parentId">Optional identifier for parent feature.</param>
		private void ParseFeatureRefElement(XmlNode node, string parentId)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}
			if (null == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}

			int lastDisplay = 0;
			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					switch (child.LocalName)
					{
						case "ComponentGroupRef":
							this.ParseComponentGroupRefElement(child, node.LocalName, id, null);
							break;
						case "ComponentRef":
							this.ParseComponentRefElement(child, node.LocalName, id, null);
							break;
						case "Feature":
							this.ParseFeatureElement(child, id, ref lastDisplay);
							break;
						case "FeatureRef":
							this.ParseFeatureRefElement(child, id);
							break;
						case "MergeRef":
							this.ParseMergeRefElement(child, id);
							break;
						default:
							if (!this.TryExtensionParseForElement(sourceLineNumbers, (XmlElement)node, (XmlElement)child))
							{
								this.core.UnexpectedElement(node, child);
							}
							break;
					}
				}
			}

			if (!this.core.EncounteredError)
			{
				this.core.AddValidReference("Feature", id);

				if (null != parentId && CompilerCore.IllegalEmptyAttributeValue != parentId)
				{
					this.core.AddComplexReference(new ComplexReference(ComplexReferenceParentType.Feature, parentId, null, ComplexReferenceChildType.Feature, id, false));
				}
			}
		}

		/// <summary>
		/// Parses an environment element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="componentId">Identifier of parent component.</param>
		private void ParseEnvironmentElement(XmlNode node, string componentId)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;
			string action = null;
			string name = null;
			string part = null;
			bool permanent = false;
			string separator = ";"; // default to ';'
			bool system = false;
			string text = null;
			string uninstall = "-"; // default to remove at uninstall

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						break;
					case "Action":
						string value = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						switch (value)
						{
							case CompilerCore.IllegalEmptyAttributeValue:
								break;
							case "create":
								action = "+";
								break;
							case "set":
								action = "=";
								break;
							case "remove":
								action = "!";
								break;
							default:
								this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name, attrib.Name, value, "create", "set", "remove"));
								break;
						}
						break;
					case "Name":
						name = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Part":
						part = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Permanent":
						permanent = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
						break;
					case "Separator":
						separator = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "System":
						system = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
						break;
					case "Value":
						text = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}
			if (null == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}
			if (null == name)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Name"));
			}

			switch (part)
			{
				case null: // not specified is the same as "all"
				case "all":
					break;
				case "first":
					text = String.Concat(text, separator, "[~]");
					break;
				case "last":
					text = String.Concat("[~]", separator, text);
					break;
				default:
					this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name, "Part", part, "all", "first", "last"));
					break;
			}

			if (permanent)
			{
				uninstall = null;
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			Row row = this.core.CreateRow(sourceLineNumbers, "Environment");
			row[0] = id;
			row[1] = String.Concat(action, uninstall, system ? "*" : String.Empty, name);
			row[2] = text;
			row[3] = componentId;
		}

		/// <summary>
		/// Parses an error element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		private void ParseErrorElement(XmlNode node)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			int id = CompilerCore.IntegerNotSet;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (CompilerCore.IntegerNotSet == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
				id = CompilerCore.IllegalInteger;
			}

			Row row = this.core.CreateRow(sourceLineNumbers, "Error");
			row[0] = id;
			row[1] = node.InnerText;
		}

		/// <summary>
		/// Parses an extension element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="componentId">Identifier of parent component.</param>
		/// <param name="advertise">Flag if this extension is advertised.</param>
		/// <param name="progId">ProgId for extension.</param>
		private void ParseExtensionElement(XmlNode node, string componentId, YesNoType advertise, string progId)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string extension = null;
			string mime = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						extension = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Advertise":
						YesNoType extensionAdvertise = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
						if ((YesNoType.No == advertise && YesNoType.Yes == extensionAdvertise) || (YesNoType.Yes == advertise && YesNoType.No == extensionAdvertise))
						{
							this.core.OnMessage(WixErrors.AdvertiseStateMustMatch(sourceLineNumbers, extensionAdvertise.ToString(CultureInfo.InvariantCulture.NumberFormat), advertise.ToString(CultureInfo.InvariantCulture.NumberFormat)));
						}
						advertise = extensionAdvertise;
						break;
					case "ContentType":
						mime = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}
			if (YesNoType.NotSet == advertise)
			{
				advertise = YesNoType.No;
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					switch (child.LocalName)
					{
						case "Verb":
							this.ParseVerbElement(child, extension, progId, componentId, advertise);
							break;
						case "MIME":
							string newMime = this.ParseMIMEElement(child, extension, componentId, advertise);
							if (null != newMime && null == mime)
							{
								mime = newMime;
							}
							break;
						default:
							this.core.UnexpectedElement(node, child);
							break;
					}
				}
			}

			if (YesNoType.Yes == advertise)
			{
				Row row = this.core.CreateRow(sourceLineNumbers, "Extension");
				row[0] = extension;
				row[1] = componentId;
				row[2] = progId;
				row[3] = mime;
				row[4] = Guid.Empty.ToString("B");

				this.core.AddFeatureBacklink(new FeatureBacklink(componentId, FeatureBacklinkType.Extension, row.Symbol));
			}
			else if (YesNoType.No == advertise)
			{
				this.AddRegistryKey(sourceLineNumbers, null, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat(".", extension), String.Empty, progId, componentId); // Extension
				if (null != mime)
				{
					this.AddRegistryKey(sourceLineNumbers, null, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat(".", extension), "Content Type", mime, componentId); // Extension's MIME ContentType
				}
			}
		}

		/// <summary>
		/// Parses a file element.
		/// </summary>
		/// <param name="node">File element to parse.</param>
		/// <param name="componentId">Parent's component id.</param>
		/// <param name="directoryId">Ancestor's directory id.</param>
		/// <param name="diskId">Disk id inherited from parent component.</param>
		/// <param name="sourcePath">Default source path of parent directory.</param>
		/// <param name="possibleKeyPath">This will be set with the possible keyPath for the parent component.</param>
		/// <param name="win64Component">true if the component is 64-bit.</param>
		/// <returns>Returns true if this file is the key path.</returns>
		private bool ParseFileElement(XmlNode node, string componentId, string directoryId, int diskId, string sourcePath, out string possibleKeyPath, bool win64Component)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;
			string assembly = null;
			string assemblyApplication = null;
			string assemblyManifest = null;
			string bindPath = null;
			int bits = 0;
			string companionFile = null;
			string defaultLanguage = null;
			int defaultSize = 0;
			string defaultVersion = null;
			string fontTitle = null;
			bool keyPath = false;
			string longName = null;
			string name = null;
			int patchGroup = CompilerCore.IntegerNotSet;
			string procArch = null;
			int selfRegCost = CompilerCore.IntegerNotSet;
			string source = sourcePath;   // assume we'll use the parents as the source for this file
			bool sourceSet = false;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						break;
					case "Assembly":
						string assemblyValue = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						switch (assemblyValue)
						{
							case CompilerCore.IllegalEmptyAttributeValue:
								break;
							case ".net":
								assembly = "0";
								break;
							case "no":
								assembly = null;
								break;
							case "win32":
								assembly = "1";
								break;
							default:
								this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, "File", "Assembly", assemblyValue, "no", "win32", ".net"));
								break;
						}
						break;
					case "AssemblyApplication":
						assemblyApplication = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						break;
					case "AssemblyManifest":
						assemblyManifest = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						break;
					case "BindPath":
						bindPath = this.core.GetAttributeValue(sourceLineNumbers, attrib, true);
						break;
					case "Checksum":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							bits |= MsiInterop.MsidbFileAttributesChecksum;
						}
						break;
					case "CompanionFile":
						companionFile = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						break;
					case "Compressed":
						YesNoDefaultType compressed = this.core.GetAttributeYesNoDefaultValue(sourceLineNumbers, attrib);
						if (YesNoDefaultType.Yes == compressed)
						{
							bits |= MsiInterop.MsidbFileAttributesCompressed;
						}
						else if (YesNoDefaultType.No == compressed)
						{
							bits |= MsiInterop.MsidbFileAttributesNoncompressed;
						}
						break;
					case "DefaultLanguage":
						defaultLanguage = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "DefaultSize":
						defaultSize = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						break;
					case "DefaultVersion":
						defaultVersion = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "DiskId":
						diskId = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						break;
					case "FontTitle":
						fontTitle = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Hidden":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							bits |= MsiInterop.MsidbFileAttributesHidden;
						}
						break;
					case "KeyPath":
						keyPath = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
						break;
					case "LongName":
						longName = this.core.GetAttributeLongFilename(sourceLineNumbers, attrib);
						break;
					case "Name":
						name = this.core.GetAttributeShortFilename(sourceLineNumbers, attrib);
						break;
					case "PatchGroup":
						patchGroup = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						break;
					case "ProcessorArchitecture":
						string procArchValue = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						switch (procArchValue)
						{
							case CompilerCore.IllegalEmptyAttributeValue:
								break;
							case "msil":
								procArch = "MSIL";
								break;
							case "x86":
								procArch = "x86";
								break;
							case "x64":
								procArch = "amd64";
								break;
							case "ia64":
								procArch = "ia64";
								break;
							default:
								this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, "File", "ProcessorArchitecture", procArchValue, "msil", "x86", "x64", "ia64"));
								break;
						}
						break;
					case "ReadOnly":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							bits |= MsiInterop.MsidbFileAttributesReadOnly;
						}
						break;
					case "SelfRegCost":
						selfRegCost = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						break;
					case "Source":
					case "src":
						if (sourceSet)
						{
							this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name, "Source", "src"));
						}
						source = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						sourceSet = true;
						break;
					case "System":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							bits |= MsiInterop.MsidbFileAttributesSystem;
						}
						break;
					case "TrueType":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							fontTitle = String.Empty;
						}
						break;
					case "Vital":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							bits |= MsiInterop.MsidbFileAttributesVital;
						}
						break;
					default:
						if (!this.TryExtensionParseForAttribute(sourceLineNumbers, (XmlElement)node, attrib))
						{
							this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						}
						break;
				}
			}

			if (null == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}
			if (null != companionFile)
			{
				// the companion file cannot be the key path of a component
				if (keyPath)
				{
					this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name, "CompanionFile", "KeyPath", "yes"));
				}
			}
			if (null == name)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Name"));
			}
			if (this.compilingModule && CompilerCore.IntegerNotSet != diskId)
			{
				this.core.OnMessage(WixErrors.IllegalAttributeInMergeModule(sourceLineNumbers, node.Name, "DiskId"));
			}
			else if (!this.compilingModule && CompilerCore.IntegerNotSet == diskId)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "DiskId"));
			}
			else if (null != defaultVersion && null != companionFile)
			{
				this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name, "DefaultVersion", "CompanionFile", companionFile));
			}
			if (CompilerCore.IntegerNotSet != diskId && 1 > diskId)
			{
				this.core.OnMessage(WixErrors.DiskIdOutOfRange(sourceLineNumbers, node.Name, "DiskId", diskId));
			}
			if (CompilerCore.IntegerNotSet != patchGroup && 1 > patchGroup)
			{
				this.core.OnMessage(WixErrors.PatchGroupOutOfRange(sourceLineNumbers, node.Name, "PatchGroup", patchGroup));
			}

			if (null == assembly)
			{
				if (null != assemblyManifest)
				{
					this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Assembly", "AssemblyManifest"));
				}

				if (null != assemblyApplication)
				{
					this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Assembly", "AssemblyApplication"));
				}
			}
			else
			{
				if (!keyPath)
				{
					this.core.OnMessage(WixErrors.IllegalAttributeValueWithoutOtherAttribute(sourceLineNumbers, node.Name, "Assembly", assembly, "KeyPath", "yes"));
				}

				// add a reference to the other file
				this.core.AddValidReference("File", assemblyManifest);

				// add a reference to the other file
				this.core.AddValidReference("File", assemblyApplication);

				// this.core.AddFeatureBacklink(componentId, FeatureBacklinkType.Assembly, row.SymbolicName);
			}

			// if source relies on parent directories, append the file name
			if (source.EndsWith(Path.DirectorySeparatorChar.ToString()))
			{
				if (!this.useShortFileNames && null != longName)
				{
					source = Path.Combine(source, longName);
				}
				else
				{
					source = Path.Combine(source, name);
				}
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					switch (child.LocalName)
					{
						case "AppId":
							this.ParseAppIdElement(child, componentId, YesNoType.NotSet, id, null, null);
							break;
						case "AssemblyName":
							this.ParseAssemblyName(child, componentId);
							break;
						case "Class":
							this.ParseClassElement(child, componentId, YesNoType.NotSet, id, null, null, null);
							break;
						case "CopyFile":
							this.ParseCopyFileElement(child, componentId, id);
							break;
						case "ODBCDriver":
							this.ParseODBCDriverOrTranslator(child, componentId, id, this.tableDefinitions["ODBCDriver"]);
							break;
						case "ODBCTranslator":
							this.ParseODBCDriverOrTranslator(child, componentId, id, this.tableDefinitions["ODBCTranslator"]);
							break;
						case "PerfCounter":
							this.ParsePerfCounterElement(child, componentId, id);
							break;
						case "Permission":
							this.ParsePermissionElement(child, id, "File");
							break;
						case "Shortcut":
							this.ParseShortcutElement(child, componentId, ShortcutType.File, id);
							break;
						case "TypeLib":
							this.ParseTypeLibElement(child, componentId, id, win64Component);
							break;
						default:
							if (!this.TryExtensionParseForElement(sourceLineNumbers, (XmlElement)node, (XmlElement)child))
							{
								this.core.UnexpectedElement(node, child);
							}
							break;
					}
				}
			}

			Row row = this.core.CreateRow(sourceLineNumbers, "File");
			row[0] = id;
			row[1] = componentId;
			row[2] = GetMsiFilenameValue(name, longName);
			row[3] = defaultSize;
			if (null != companionFile)
			{
				row[4] = companionFile;
			}
			else if (null != defaultVersion)
			{
				row[4] = defaultVersion;
			}
			row[5] = defaultLanguage;
			row[6] = bits;
			row[8] = assembly;
			row[9] = assemblyManifest;
			row[10] = assemblyApplication;
			row[11] = directoryId;
			if (CompilerCore.IntegerNotSet != diskId)
			{
				row[12] = diskId;
			}
			row[13] = source;
			row[14] = procArch;
			row[15] = (CompilerCore.IntegerNotSet != patchGroup ? patchGroup : -1);

			if (CompilerCore.IntegerNotSet != diskId)
			{
				this.core.AddValidReference("Media", diskId.ToString(CultureInfo.InvariantCulture.NumberFormat));
			}

			if (null != bindPath)
			{
				row = this.core.CreateRow(sourceLineNumbers, "BindImage");
				row[0] = id;
				row[1] = bindPath;

				// TODO: technically speaking each of the properties in the "bindPath" should be added as references, but how much do we really care about BindImage?
			}

			if (CompilerCore.IntegerNotSet != selfRegCost)
			{
				row = this.core.CreateRow(sourceLineNumbers, "SelfReg");
				row[0] = id;
				row[1] = selfRegCost;
			}

			if (null != fontTitle)
			{
				row = this.core.CreateRow(sourceLineNumbers, "Font");
				row[0] = id;
				row[1] = fontTitle;
			}

			possibleKeyPath = id;
			return keyPath;
		}

		/// <summary>
		/// Parses a file search element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="parentSignature">Signature of parent search element.</param>
		/// <returns>Signature of search element.</returns>
		private string ParseFileSearchElement(XmlNode node, string parentSignature)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;
			string languages = null;
			string longName = null;
			int minDate = CompilerCore.IntegerNotSet;
			int maxDate = CompilerCore.IntegerNotSet;
			string maxSize = null;
			string minSize = null;
			string maxVersion = null;
			string minVersion = null;
			string name = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						break;
					case "LongName":
						longName = this.core.GetAttributeLongFilename(sourceLineNumbers, attrib);
						break;
					case "Name":
						name = this.core.GetAttributeShortFilename(sourceLineNumbers, attrib);
						break;
					case "MinVersion":
						minVersion = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "MaxVersion":
						maxVersion = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "MinSize":
						minSize = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "MaxSize":
						maxSize = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "MinDate":
						DateTime minDateValue = this.core.GetAttributeDateTimeValue(sourceLineNumbers, attrib);
						minDate = this.core.ConvertDateTimeToInteger(minDateValue);
						break;
					case "MaxDate":
						DateTime maxDateValue = this.core.GetAttributeDateTimeValue(sourceLineNumbers, attrib);
						maxDate = this.core.ConvertDateTimeToInteger(maxDateValue);
						break;
					case "Languages":
						languages = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == id)
			{
				if (null == parentSignature)
				{
					this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
				}

				id = parentSignature;
			}

			// check name and longName: at least one is required and using both will not always work due to a Windows Installer bug
			if (null == name && null == longName)
			{
				this.core.OnMessage(WixErrors.ExpectedAttributes(sourceLineNumbers, node.Name, "Name", "LongName"));
			}
			else if (null != name && null != longName)
			{
				this.core.OnMessage(WixWarnings.FileSearchFileNameIssue(sourceLineNumbers, node.Name, "Name", "LongName"));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			Row row = this.core.CreateRow(sourceLineNumbers, "Signature");
			row[0] = id;
			if (null != name && null != longName)
			{
				row[1] = String.Concat(name, "|", longName);
			}
			else
			{
				row[1] = (null != name ? name : longName);
			}
			row[2] = minVersion;
			row[3] = maxVersion;
			row[4] = minSize;
			row[5] = maxSize;
			if (CompilerCore.IntegerNotSet != minDate)
			{
				row[6] = minDate;
			}
			if (CompilerCore.IntegerNotSet != maxDate)
			{
				row[7] = maxDate;
			}
			row[8] = languages;

			return id; // the id of the FileSearch element is its signature
		}

		/// <summary>
		/// Parses a file search reference element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <returns>Signature of referenced search element.</returns>
		private string ParseFileSearchRefElement(XmlNode node)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			this.core.AddValidReference("Signature", id);

			return id; // the id of the FileSearchRef element is its signature
		}

		/// <summary>
		/// Parses a file share element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="componentId">Identifier of parent component.</param>
		/// <param name="directoryId">Identifier of referred to directory.</param>
		private void ParseFileShareElement(XmlNode node, string componentId, string directoryId)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string description = null;
			string name = null;
			string id = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Name":
						name = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Description":
						description = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}
			if (null == name)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Name"));
			}
			if (1 > node.ChildNodes.Count)
			{
				this.core.OnMessage(WixErrors.ExpectedElement(sourceLineNumbers, node.Name, "Permission"));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					switch (child.LocalName)
					{
						case "Permission":
							this.ParsePermissionElement(child, id, "FileShare");
							break;
						default:
							this.core.UnexpectedElement(node, child);
							break;
					}
				}
			}

			// Reference ConfigureSmb since nothing will happen without it
			this.core.AddValidReference("CustomAction", "ConfigureSmb");

			Row row = this.core.CreateRow(sourceLineNumbers, "FileShare");
			row[0] = id;
			row[1] = name;
			row[2] = componentId;
			row[3] = description;
			row[4] = directoryId;
		}

		/// <summary>
		/// Parses a fragment element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		private void ParseFragmentElement(XmlNode node)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;

			this.activeName = null;
			this.activeLanguage = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}
			// NOTE: Id is not required for Fragments, this is a departure from the normal run of the mill processing.

			this.core.CreateActiveSection(id, SectionType.Fragment, 0);

			int featureDisplay = 0;
			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					switch (child.LocalName)
					{
						case "_locDefinition":
							break;
						case "AdminExecuteSequence":
						case "AdminUISequence":
						case "AdvertiseExecuteSequence":
						case "InstallExecuteSequence":
						case "InstallUISequence":
							this.ParseSequenceElement(child, child.LocalName);
							break;
						case "AppId":
							this.ParseAppIdElement(child, null, YesNoType.Yes, null, null, null);
							break;
						case "Binary":
							this.ParseBinaryOrIconElement(child, BinaryType.Binary);
							break;
						case "ComplianceCheck":
							this.ParseComplianceCheckElement(child);
							break;
						case "ComponentGroup":
							this.ParseComponentGroupElement(child);
							break;
						case "Condition":
							this.ParseConditionElement(child, node.LocalName, null, null);
							break;
						case "CustomAction":
							this.ParseCustomActionElement(child);
							break;
						case "CustomActionRef":
							this.ParseCustomActionRefElement(child);
							break;
						case "CustomTable":
							this.ParseCustomTableElement(child);
							break;
						case "Directory":
							this.ParseDirectoryElement(child, null, String.Empty);
							break;
						case "DirectoryRef":
							this.ParseDirectoryRefElement(child);
							break;
						case "EnsureTable":
							this.ParseEnsureTableElement(child);
							break;
						case "Feature":
							this.ParseFeatureElement(child, null, ref featureDisplay);
							break;
						case "FeatureRef":
							this.ParseFeatureRefElement(child, null);
							break;
						case "FragmentRef":
							this.ParseFragmentRefElement(child);
							break;
						case "Group":
							this.ParseGroupElement(child, null);
							break;
						case "Icon":
							this.ParseBinaryOrIconElement(child, BinaryType.Icon);
							break;
						case "IgnoreModularization":
							this.ParseIgnoreModularizationElement(child);
							break;
						case "Media":
							this.ParseMediaElement(child);
							break;
						case "MediaRef":
							this.ParseMediaRefElement(child);
							break;
						case "PatchCertificates":
							this.ParsePatchCertificatesElement(child);
							break;
						case "Property":
							this.ParsePropertyElement(child);
							break;
						case "PropertyRef":
							this.ParsePropertyRefElement(child);
							break;
						case "SFPCatalog":
							string parentName = null;
							this.ParseSFPCatalogElement(child, ref parentName);
							break;
						case "SqlDatabase":
							this.ParseSqlDatabaseElement(child, null);
							break;
						case "UI":
							this.ParseUIElement(child);
							break;
						case "UIRef":
							this.ParseUIRefElement(child);
							break;
						case "Upgrade":
							this.ParseUpgradeElement(child);
							break;
						case "User":
							this.ParseUserElement(child, null);
							break;
						case "WebApplication":
							this.ParseWebApplicationElement(child);
							break;
						case "WebAppPool":
							this.ParseWebAppPoolElement(child, null);
							break;
						case "WebDirProperties":
							this.ParseWebDirPropertiesElement(child);
							break;
						case "WebLog":
							this.ParseWebLogElement(child);
							break;
						case "WebSite":
							this.ParseWebSiteElement(child, null);
							break;
						default:
							if (!this.TryExtensionParseForElement(sourceLineNumbers, (XmlElement)node, (XmlElement)child))
							{
								this.core.UnexpectedElement(node, child);
							}
							break;
					}
				}
			}
		}

		/// <summary>
		/// Parses a fragment reference element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		private void ParseFragmentRefElement(XmlNode node)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}
			if (null == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			this.core.AddValidReference("Fragment", id);
		}

		/// <summary>
		/// Parses a condition element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="parentElementLocalName">LocalName of the parent element.</param>
		/// <param name="id">Id of the parent element.</param>
		/// <param name="dialog">Dialog of the parent element if its a Control.</param>
		/// <returns>The condition if one was found.</returns>
		private string ParseConditionElement(XmlNode node, string parentElementLocalName, string id, string dialog)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string action = null;
			string condition = null;
			int level = CompilerCore.IntegerNotSet;
			string message = null;
			Row row;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Action":
						if ("Control" == parentElementLocalName)
						{
							action = this.core.GetAttributeValue(sourceLineNumbers, attrib);
							switch (action)
							{
								case CompilerCore.IllegalEmptyAttributeValue: // this error is already handled
									break;
								case "default":
								case "disable":
								case "enable":
								case "hide":
								case "show":
									action = Compiler.UppercaseFirstChar(action);
									break;
								default:
									this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name, attrib.Name, action, "default", "disable", "enable", "hide", "show"));
									break;
							}
						}
						else
						{
							this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						}
						break;
					case "Level":
						if ("Feature" == parentElementLocalName)
						{
							level = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						}
						else
						{
							this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						}
						break;
					case "Message":
						if ("Fragment" == parentElementLocalName || "Product" == parentElementLocalName)
						{
							message = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						}
						else
						{
							this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						}
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			// get the condition from the inner text of the element
			condition = this.core.GetConditionInnerText(node);

			// find unexpected child elements
			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			// the condition should not be empty
			if (null == condition || 0 == condition.Length)
			{
				condition = null;
				this.core.OnMessage(WixErrors.ConditionExpected(sourceLineNumbers, node.Name));
			}

			switch (parentElementLocalName)
			{
				case "Control":
					if (null == action)
					{
						this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Action"));
					}
					row = this.core.CreateRow(sourceLineNumbers, "ControlCondition");
					row[0] = dialog;
					row[1] = id;
					row[2] = action;
					row[3] = condition;
					break;
				case "Feature":
					if (CompilerCore.IntegerNotSet == level)
					{
						this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Level"));
						level = CompilerCore.IllegalInteger;
					}

					row = this.core.CreateRow(sourceLineNumbers, "Condition");
					row[0] = id;
					row[1] = level;
					row[2] = condition;
					break;
				case "Fragment":
				case "Product":
					if (null == message)
					{
						this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Message"));
					}

					row = this.core.CreateRow(sourceLineNumbers, "LaunchCondition");
					row[0] = condition;
					row[1] = message;
					break;
			}

			return condition;
		}

		/// <summary>
		/// Parses a XmlFile element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="componentName">Name of parent component.</param>
		private void ParseXmlFileElement(XmlNode node, string componentName)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;
			string file = null;
			string elementPath = null;
			string name = null;
			string value = null;
			int sequence = -1;
			int flags = 0;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Action":
						string actionValue = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						switch (actionValue)
						{
							case "createElement":
								flags |= 0x00000001; // XMLFILE_CREATE_ELEMENT
								break;
							case "deleteValue":
								flags |= 0x00000002; // XMLFILE_DELETE_VALUE
								break;
							case "setValue":
								// no flag for set value since it's the default
								break;
							default:
								this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name, "Action", actionValue, "createElement", "deleteValue", "setValue"));
								break;
						}
						break;
					case "Id":
						id = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						break;
					case "File":
						file = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "ElementPath":
						elementPath = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Name":
						name = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Permanent":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							flags |= 0x00010000; // XMLFILE_DONT_UNINSTALL
						}
						break;
					case "Sequence":
						sequence = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						break;
					case "Value":
						value = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}
			if (null == file)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "File"));
			}
			if (null == elementPath)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "ElementPath"));
			}
			if ((0x00000001 /*XMLFILE_CREATE_ELEMENT*/ & flags) != 0 && null == name)
			{
				this.core.OnMessage(WixErrors.IllegalAttributeWithoutOtherAttributes(sourceLineNumbers, node.Name, "Action", "Name"));
			}


			Row row;
			row = this.core.CreateRow(sourceLineNumbers, "XmlFile");

			row[0] = id;
			row[1] = file;
			row[2] = elementPath;
			row[3] = name;
			row[4] = value;
			row[5] = flags;
			row[6] = componentName;
			if (-1 != sequence)
			{
				row[7] = sequence;
			}

			// Reference SchedXmlFile since nothing will happen without it
			this.core.AddValidReference("CustomAction", "SchedXmlFile");
		}

		/// <summary>
		/// Parses a IniFile element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="componentName">Name of parent component.</param>
		private void ParseIniFileElement(XmlNode node, string componentName)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;
			int action = CompilerCore.IntegerNotSet;
			string directory = null;
			string key = null;
			string longName = null;
			string name = null;
			string section = null;
			string tableName = null;
			string value = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						break;
					case "Action":
						string actionValue = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						switch (actionValue)
						{
							case CompilerCore.IllegalEmptyAttributeValue: // this error is already handled
								break;
							case "addLine":
								action = MsiInterop.MsidbIniFileActionAddLine;
								break;
							case "addTag":
								action = MsiInterop.MsidbIniFileActionAddTag;
								break;
							case "createLine":
								action = MsiInterop.MsidbIniFileActionCreateLine;
								break;
							case "removeLine":
								action = MsiInterop.MsidbIniFileActionRemoveLine;
								break;
							case "removeTag":
								action = MsiInterop.MsidbIniFileActionRemoveTag;
								break;
							default:
								this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name, "Action", actionValue, "addLine", "addTag", "createLine", "removeLine", "removeTag"));
								break;
						}
						break;
					case "Directory":
						directory = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						break;
					case "Key":
						key = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "LongName":
						longName = this.core.GetAttributeLongFilename(sourceLineNumbers, attrib);
						break;
					case "Name":
						name = this.core.GetAttributeShortFilename(sourceLineNumbers, attrib);
						break;
					case "Section":
						section = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Value":
						value = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}
			if (CompilerCore.IntegerNotSet == action)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Action"));
				action = CompilerCore.IllegalInteger;
			}

			if (null == key)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Key"));
			}

			if (null == name)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Name"));
			}

			if (null == section)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Section"));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			if (MsiInterop.MsidbIniFileActionRemoveLine == action || MsiInterop.MsidbIniFileActionRemoveTag == action)
			{
				tableName = "RemoveIniFile";
			}
			else
			{
				if (null == value)
				{
					this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Value"));
				}

				tableName = "IniFile";
			}

			Row row = this.core.CreateRow(sourceLineNumbers, tableName);
			row[0] = id;
			row[1] = GetMsiFilenameValue(name, longName);
			row[2] = directory;
			row[3] = section;
			row[4] = key;
			row[5] = value;
			row[6] = action;
			row[7] = componentName;
		}

		/// <summary>
		/// Parses an IniFile search element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <returns>Signature for search element.</returns>
		private string ParseIniFileSearchElement(XmlNode node)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;
			int field = CompilerCore.IntegerNotSet;
			string fileName = null;
			string key = null;
			string longName = null;
			string name = null;
			string section = null;
			string signature = null;
			int type = 1; // default is file

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						break;
					case "Field":
						field = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						break;
					case "Key":
						key = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "LongName":
						longName = this.core.GetAttributeLongFilename(sourceLineNumbers, attrib);
						break;
					case "Name":
						name = this.core.GetAttributeShortFilename(sourceLineNumbers, attrib);
						break;
					case "Section":
						section = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Type":
						string typeValue = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						switch (typeValue)
						{
							case CompilerCore.IllegalEmptyAttributeValue: // this error is already handled
								break;
							case "directory":
								type = 0;
								break;
							case "file":
								type = 1;
								break;
							case "raw":
								type = 2;
								break;
							default:
								this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name, "Type", typeValue, "directory", "file", "registry"));
								break;
						}
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}
			if (null == key)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Key"));
			}
			if (null == fileName && null == name)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Name"));
			}
			if (null == section)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Section"));
			}

			signature = id;

			bool oneChild = false;
			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					SourceLineNumberCollection childSourceLineNumbers = this.core.GetSourceLineNumbers(child);
					switch (child.LocalName)
					{
						case "DirectorySearch":
							if (oneChild)
							{
								this.core.OnMessage(WixErrors.TooManySearchElements(childSourceLineNumbers, node.Name));
							}
							oneChild = true;
							// directorysearch parentage should work like directory element, not the rest of the signature type because of the DrLocator.Parent column
							signature = this.ParseDirectorySearchElement(child, id);
							break;
						case "DirectorySearchRef":
							if (oneChild)
							{
								this.core.OnMessage(WixErrors.TooManySearchElements(childSourceLineNumbers, node.Name));
							}
							oneChild = true;
							signature = this.ParseDirectorySearchRefElement(child, id);
							break;
						case "FileSearch":
							if (oneChild)
							{
								this.core.OnMessage(WixErrors.TooManySearchElements(sourceLineNumbers, node.Name));
							}
							oneChild = true;
							signature = this.ParseFileSearchElement(child, id);
							id = signature; // FileSearch signatures override parent signatures
							break;
						case "FileSearchRef":
							if (oneChild)
							{
								this.core.OnMessage(WixErrors.TooManySearchElements(sourceLineNumbers, node.Name));
							}
							oneChild = true;
							id = this.ParseFileSearchRefElement(child); // FileSearch signatures override parent signatures
							signature = null;
							break;
						default:
							this.core.UnexpectedElement(node, child);
							break;
					}
				}
			}

			Row row = this.core.CreateRow(sourceLineNumbers, "IniLocator");
			row[0] = id;
			row[1] = (null != fileName ? fileName : GetMsiFilenameValue(name, longName));
			row[2] = section;
			row[3] = key;
			if (CompilerCore.IntegerNotSet != field)
			{
				row[4] = field;
			}
			row[5] = type;

			return signature;
		}

		/// <summary>
		/// Parses an isolated component element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="componentId">Identifier of parent component.</param>
		private void ParseIsolateComponentElement(XmlNode node, string componentId)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string shared = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Shared":
						shared = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == shared)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Shared"));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			Row row = this.core.CreateRow(sourceLineNumbers, "IsolatedComponent");
			row[0] = shared;
			row[1] = componentId;
		}

		/// <summary>
		/// Parses a PatchCertificates element.
		/// </summary>
		/// <param name="node">The element to parse.</param>
		private void ParsePatchCertificatesElement(XmlNode node)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);

			// no attributes are supported for this element
			foreach (XmlAttribute attrib in node.Attributes)
			{
				this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					switch (child.LocalName)
					{
						case "DigitalCertificate":
							string name = null;
							this.ParseDigitalCertificateElement(child, ref name);

							if (!this.core.EncounteredError)
							{
								Row row = this.core.CreateRow(sourceLineNumbers, "MsiPatchCertificate");
								row[0] = name;
								row[1] = name;
							}
							break;
						default:
							this.core.UnexpectedElement(node, child);
							break;
					}
				}
			}
		}

		/// <summary>
		/// Parses an digital certificate element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="certificateId">The certificate identifier from this element.</param>
		private void ParseDigitalCertificateElement(XmlNode node, ref string certificateId)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string certData = null;
			certificateId = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						certificateId = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "SourceFile":
						certData = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == certData)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "src"));
			}
			if (null == certificateId)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Name"));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			Row row = this.core.CreateRow(sourceLineNumbers, "MsiDigitalCertificate");
			row[0] = certificateId;
			row[1] = certData;
		}

		/// <summary>
		/// Parses an digital signature element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="diskId">Disk id inherited from parent component.</param>
		private void ParseDigitalSignatureElement(XmlNode node, string diskId)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string hash = null;
			string name = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "SourceFile":
						hash = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						this.core.OnMessage(WixErrors.BinaryColumnTypesNotYetImplementedInWiXToolSet(sourceLineNumbers, node.Name, "src", "MsiDigitalSignature", "Hash"));
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == hash)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Shared"));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					switch (child.LocalName)
					{
						case "DigitalSignature":
							this.ParseDigitalCertificateElement(child, ref name);
							break;
						default:
							this.core.UnexpectedElement(node, child);
							break;
					}
				}
			}

			Row row = this.core.CreateRow(sourceLineNumbers, "MsiDigitalSignature");
			row[0] = "media";
			row[1] = diskId;
			row[2] = name;
			row[3] = hash;
		}

		/// <summary>
		/// Parses a media element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		private void ParseMediaElement(XmlNode node)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			int id = CompilerCore.IntegerNotSet;
			string cabinet = null;
			string compressionLevel = null;
			string diskPrompt = null;
			YesNoType embedCab = YesNoType.NotSet;
			string layout = null;
			string volumeLabel = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						break;
					case "Cabinet":
						cabinet = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "CompressionLevel":
						compressionLevel = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						switch (compressionLevel)
						{
							case CompilerCore.IllegalEmptyAttributeValue: // this error is already handled
								break;
							case "high":
							case "low":
							case "medium":
							case "mszip":
							case "none":
								break;
							default:
								this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name, attrib.Name, compressionLevel, "high", "low", "medium", "mszip", "none"));
								break;
						}
						break;
					case "DiskPrompt":
						diskPrompt = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						this.core.AddValidReference("Property", "DiskPrompt"); // ensure the output has a DiskPrompt Property defined
						break;
					case "EmbedCab":
						embedCab = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
						break;
					case "Layout":
					case "src":
						if (null != layout)
						{
							this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name, "Layout", "src"));
						}
						layout = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "VolumeLabel":
						volumeLabel = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (CompilerCore.IntegerNotSet == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
				id = CompilerCore.IllegalInteger;
			}
			else if (1 > id)
			{
				this.core.OnMessage(WixErrors.DiskIdOutOfRange(sourceLineNumbers, node.Name, "Id", id));
			}
			if (YesNoType.IllegalValue != embedCab)
			{
				if (YesNoType.Yes == embedCab)
				{
					if (null == cabinet)
					{
						this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Cabinet", "EmbedCab", "yes"));
					}
					else
					{
						if (!cabinet.StartsWith("#"))
						{
							cabinet = String.Concat("#", cabinet);
						}
						if (63 < cabinet.Length)
						{
							this.core.OnMessage(WixErrors.MediaEmbeddedCabinetNameTooLong(sourceLineNumbers, node.Name, "Cabinet", cabinet));
						}
					}
				}
				else // external cabinet file
				{
					// external cabinet files must use 8.3 filenames
					if (null != cabinet && !CompilerCore.IsValidShortFilename(cabinet))
					{
						this.core.OnMessage(WixWarnings.MediaExternalCabinetFilenameIllegal(sourceLineNumbers, node.Name, "Cabinet", cabinet));
					}
				}
			}
			if (null != compressionLevel && null == cabinet)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Cabinet", "CompressionLevel"));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == node.NodeType)
				{
					switch (child.LocalName)
					{
						case "DigitalSignature": // supported in the schema but not yet implemented
							this.ParseDigitalSignatureElement(child, id.ToString(CultureInfo.InvariantCulture.NumberFormat));
							break;
						default:
							this.core.UnexpectedElement(node, child);
							break;
					}
				}
			}

			// add the row to the section
			Row row = this.core.CreateRow(sourceLineNumbers, "Media");
			row[0] = id;
			row[1] = 0; // LastSequence is set in the linker
			row[2] = diskPrompt;
			row[3] = cabinet;
			row[4] = volumeLabel;
			// column 6 is the Source column; its only used for patching
			row[6] = compressionLevel;
			row[7] = layout;
		}

		/// <summary>
		/// Parses a media reference element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		private void ParseMediaRefElement(XmlNode node)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			this.core.AddValidReference("Media", id);
		}

		/// <summary>
		/// Parses a merge element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="directoryId">Identifier for parent directory.</param>
		private void ParseMergeElement(XmlNode node, string directoryId)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;
			string configData = String.Empty;
			int diskId = CompilerCore.IntegerNotSet;
			YesNoType fileCompression = YesNoType.NotSet;
			int language = CompilerCore.IntegerNotSet;
			string sourceFile = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "DiskId":
						diskId = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						break;
					case "FileCompression":
						fileCompression = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
						break;
					case "Language":
						language = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						break;
					case "SourceFile":
					case "src":
						if (null != sourceFile)
						{
							this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name, "SourceFile", "src"));
						}
						sourceFile = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}
			if (null == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}
			if (CompilerCore.IntegerNotSet == language)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Language"));
				language = CompilerCore.IllegalInteger;
			}

			if (null == sourceFile)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "SourceFile"));
			}
			if (CompilerCore.IntegerNotSet == diskId)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "DiskId"));
				diskId = CompilerCore.IllegalInteger;
			}
			else
			{
				this.core.AddValidReference("Media", diskId.ToString(CultureInfo.InvariantCulture.NumberFormat));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					switch (child.LocalName)
					{
						case "ConfigurationData":
							if (0 == configData.Length)
							{
								configData = this.ParseConfigurationDataElement(child);
							}
							else
							{
								configData = String.Concat(configData, ",", this.ParseConfigurationDataElement(child));
							}
							break;
						default:
							this.core.UnexpectedElement(node, child);
							break;
					}
				}
			}

			Row row = this.core.CreateRow(sourceLineNumbers, "Merge");
			row[0] = id;
			row[1] = language;
			row[2] = directoryId;
			row[3] = sourceFile;
			row[4] = diskId;
			if (YesNoType.Yes == fileCompression)
			{
				row[5] = true;
			}
			else if (YesNoType.No == fileCompression)
			{
				row[5] = false;
			}
			// else YesNoType.NotSet == fileCompression and we leave the column null

			// column 6 is the HasFiles column; it is not set
			row[7] = configData;
		}

		/// <summary>
		/// Parses a configuration data element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <returns>String in format "name=value" with '%', ',' and '=' hex encoded.</returns>
		private string ParseConfigurationDataElement(XmlNode node)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string name = null;
			string value = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Name":
						name = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Value":
						value = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == name)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Name"));
			}
			if (null == value)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Value"));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			// Need to hex encode these characters.
			name = name.Replace("%", "%25");
			name = name.Replace("=", "%3D");
			name = name.Replace(",", "%2C");
			value = value.Replace("%", "%25");
			value = value.Replace("=", "%3D");
			value = value.Replace(",", "%2C");

			return String.Concat(name, "=", value);
		}

		/// <summary>
		/// Parses a merge reference element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="featureId">Identifier for parent feature.</param>
		private void ParseMergeRefElement(XmlNode node, string featureId)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;
			bool primary = false;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Primary":
						primary = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			this.core.AddValidReference("Merge", id);
			this.core.AddComplexReference(new ComplexReference(ComplexReferenceParentType.Feature, featureId, null, ComplexReferenceChildType.Module, id, primary));
		}

		/// <summary>
		/// Parses a mime element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="extension">Identifier for parent extension.</param>
		/// <param name="componentId">Identifier for parent component.</param>
		/// <param name="advertise">Flag if mime should be advertised.</param>
		/// <returns>Content type if this is the default for the MIME type.</returns>
		private string ParseMIMEElement(XmlNode node, string extension, string componentId, YesNoType advertise)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string classId = null;
			string contentType = null;
			YesNoType returnContentType = YesNoType.NotSet;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Class":
						classId = String.Concat("{", this.core.GetAttributeGuidValue(sourceLineNumbers, attrib, false), "}");
						break;
					case "ContentType":
						contentType = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Default":
						returnContentType = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == contentType)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "ContentType"));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			if (YesNoType.Yes == advertise)
			{
				Row row = this.core.CreateRow(sourceLineNumbers, "MIME");
				row[0] = contentType;
				row[1] = extension;
				row[2] = classId;
			}
			else if (YesNoType.No == advertise)
			{
				this.AddRegistryKey(sourceLineNumbers, null, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("MIME\\Database\\Content Type\\", contentType), "Extension", String.Concat(".", extension), componentId);
				if (null != classId)
				{
					this.AddRegistryKey(sourceLineNumbers, null, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("MIME\\Database\\Content Type\\", contentType), "CLSID", classId, componentId);
				}
			}

			return YesNoType.Yes == returnContentType ? contentType : null;
		}

		/// <summary>
		/// Parses a module element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		private void ParseModuleElement(XmlNode node)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			int codepage = 0;
			string moduleId = null;
			string version = null;

			this.activeName = null;
			this.activeLanguage = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						this.activeName = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						break;
					case "Codepage":
						codepage = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						break;
					case "Guid":
						moduleId = this.core.GetAttributeGuidValue(sourceLineNumbers, attrib, false);
						moduleId = moduleId.Replace("-", "_");
						break;
					case "Language":
						this.activeLanguage = this.core.GetAttributeLocalizableIntegerValue(sourceLineNumbers, attrib);
						break;
					case "Version":
						version = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == moduleId)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Guid"));
			}
			if (null == this.activeName)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}
			if (null == this.activeLanguage)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Language"));
			}
			//            else if (null != this.activeLanguage && 0 > this.activeLanguage)
			//            {
			//                this.core.OnMessage(WixErrors.IllegalLcidValue(sourceLineNumbers, node.Name, "Language", this.activeLanguage));
			//            }
			if (null == version)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Version"));
			}

			try
			{
				this.compilingModule = true; // notice that we are actually building a Merge Module here
				this.core.CreateActiveSection(this.activeName, SectionType.Module, codepage);

				Row row = this.core.CreateRow(sourceLineNumbers, "ModuleSignature");
				row[0] = this.activeName;
				row[1] = this.activeLanguage;
				row[2] = version;
				row[3] = moduleId;

				foreach (XmlNode child in node.ChildNodes)
				{
					if (XmlNodeType.Element == child.NodeType)
					{
						switch (child.LocalName)
						{
							case "AdminExecuteSequence":
							case "AdminUISequence":
							case "AdvertiseExecuteSequence":
							case "InstallExecuteSequence":
							case "InstallUISequence":
								this.ParseSequenceElement(child, child.LocalName);
								break;
							case "AppId":
								this.ParseAppIdElement(child, null, YesNoType.Yes, null, null, null);
								break;
							case "Binary":
								this.ParseBinaryOrIconElement(child, BinaryType.Binary);
								break;
							case "ComponentGroupRef":
								this.ParseComponentGroupRefElement(child, node.LocalName, this.activeName, this.activeLanguage);
								break;
							case "ComponentRef":
								this.ParseComponentRefElement(child, node.LocalName, this.activeName, this.activeLanguage);
								break;
							case "Configuration":
								this.ParseConfigurationElement(child);
								break;
							case "CustomAction":
								this.ParseCustomActionElement(child);
								break;
							case "CustomActionRef":
								this.ParseCustomActionRefElement(child);
								break;
							case "CustomTable":
								this.ParseCustomTableElement(child);
								break;
							case "Dependency":
								this.ParseDependencyElement(child, this.activeName, this.activeLanguage);
								break;
							case "Directory":
								this.ParseDirectoryElement(child, null, String.Empty);
								break;
							case "DirectoryRef":
								this.ParseDirectoryRefElement(child);
								break;
							case "EnsureTable":
								this.ParseEnsureTableElement(child);
								break;
							case "Exclusion":
								this.ParseExclusionElement(child, this.activeName, this.activeLanguage);
								break;
							case "FragmentRef":
								this.ParseFragmentRefElement(child);
								break;
							case "Group":
								this.ParseGroupElement(child, null);
								break;
							case "Icon":
								this.ParseBinaryOrIconElement(child, BinaryType.Icon);
								break;
							case "IgnoreModularization":
								this.ParseIgnoreModularizationElement(child);
								break;
							case "Package":
								this.ParsePackageElement(child, null);
								break;
							case "Property":
								this.ParsePropertyElement(child);
								break;
							case "PropertyRef":
								this.ParsePropertyRefElement(child);
								break;
							case "SFPCatalog":
								string parentName = null;
								this.ParseSFPCatalogElement(child, ref parentName);
								break;
							case "SqlDatabase":
								this.ParseSqlDatabaseElement(child, null);
								break;
							case "Substitution":
								this.ParseSubstitutionElement(child);
								break;
							case "UI":
								this.ParseUIElement(child);
								break;
							case "UIRef":
								this.ParseUIRefElement(child);
								break;
							case "User":
								this.ParseUserElement(child, null);
								break;
							case "WebApplication":
								this.ParseWebApplicationElement(child);
								break;
							case "WebAppPool":
								this.ParseWebAppPoolElement(child, null);
								break;
							case "WebDirProperties":
								this.ParseWebDirPropertiesElement(child);
								break;
							case "WebLog":
								this.ParseWebLogElement(child);
								break;
							case "WebSite":
								this.ParseWebSiteElement(child, null);
								break;
							default:
								if (!this.TryExtensionParseForElement(sourceLineNumbers, (XmlElement)node, (XmlElement)child))
								{
									this.core.UnexpectedElement(node, child);
								}
								break;
						}
					}
				}
			}
			finally
			{
				this.compilingModule = false; // notice that we are no longer building a Merge Module here
			}
		}

		/// <summary>
		/// Parses a patch creation element.
		/// </summary>
		/// <param name="node">The element to parse.</param>
		private void ParsePatchCreationElement(XmlNode node)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			bool clean = true; // Default is to clean
			int codepage = 0;
			string outputPath = null;
			bool productMismatches = false;
			string replaceGuids = String.Empty;
			string sourceList = null;
			string symbolFlags = null;
			string targetProducts = String.Empty;
			bool versionMismatches = false;
			bool wholeFiles = false;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						this.activeName = this.core.GetAttributeGuidValue(sourceLineNumbers, attrib, false);
						break;
					case "AllowMajorVersionMismatches":
						versionMismatches = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
						break;
					case "AllowProductCodeMismatches":
						productMismatches = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
						break;
					case "CleanWorkingFolder":
						clean = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
						break;
					case "Codepage":
						codepage = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						break;
					case "OutputPath":
						outputPath = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "SourceList":
						sourceList = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "SymbolFlags":
						symbolFlags = String.Format(CultureInfo.InvariantCulture, "0x{0:x8}", this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib));
						break;
					case "WholeFilesOnly":
						wholeFiles = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == this.activeName)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}

			this.core.CreateActiveSection(this.activeName, SectionType.PatchCreation, codepage);

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					switch (child.LocalName)
					{
						case "Family":
							this.ParseFamilyElement(child);
							break;
						case "PatchInformation":
							this.ParsePatchInformationElement(child);
							break;
						case "PatchMetadata":
							this.ParsePatchMetadataElement(child);
							break;
						case "PatchProperty":
							this.ParsePatchPropertyElement(child);
							break;
						case "PatchSequence":
							this.ParsePatchSequenceElement(child);
							break;
						case "ReplacePatch":
							replaceGuids = String.Concat(replaceGuids, "{", this.ParseReplacePatchElement(child), "}");
							break;
						case "TargetProductCode":
							string targetProduct = this.ParseTargetProductCodeElement(child);
							if (0 < targetProducts.Length)
							{
								targetProducts = String.Concat(targetProducts, ";");
							}

							if ("*" == targetProduct)
							{
								targetProducts = String.Concat(targetProducts, "*");
							}
							else
							{
								targetProducts = String.Concat(targetProducts, "{", targetProduct, "}");
							}
							break;
						default:
							this.core.UnexpectedElement(node, child);
							break;
					}
				}
			}

			this.ProcessProperties(sourceLineNumbers, "PatchGUID", String.Concat("{", this.activeName, "}"));
			this.ProcessProperties(sourceLineNumbers, "AllowProductCodeMismatches", productMismatches ? 1 : 0);
			this.ProcessProperties(sourceLineNumbers, "AllowProductVersionMajorMismatches", versionMismatches ? 1 : 0);
			this.ProcessProperties(sourceLineNumbers, "DontRemoveTempFolderWhenFinished", clean ? 0 : 1);
			this.ProcessProperties(sourceLineNumbers, "IncludeWholeFilesOnly", wholeFiles ? 1 : 0);

			if (null != symbolFlags)
			{
				this.ProcessProperties(sourceLineNumbers, "ApiPatchingSymbolFlags", symbolFlags);
			}
			if (0 < replaceGuids.Length)
			{
				this.ProcessProperties(sourceLineNumbers, "ListOfPatchGUIDsToReplace", replaceGuids);
			}
			if (0 < targetProducts.Length)
			{
				this.ProcessProperties(sourceLineNumbers, "ListOfTargetProductCodes", targetProducts);
			}
			if (null != outputPath)
			{
				this.ProcessProperties(sourceLineNumbers, "PatchOutputPath", outputPath);
			}
			if (null != sourceList)
			{
				this.ProcessProperties(sourceLineNumbers, "PatchSourceList", sourceList);
			}
		}

		/// <summary>
		/// Parses a family element.
		/// </summary>
		/// <param name="node">The element to parse.</param>
		private void ParseFamilyElement(XmlNode node)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			int diskId = CompilerCore.IntegerNotSet;
			string diskPrompt = null;
			string mediaSrcProp = null;
			string name = null;
			int sequenceStart = CompilerCore.IntegerNotSet;
			string volumeLabel = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "DiskId":
						diskId = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						break;
					case "DiskPrompt":
						diskPrompt = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "MediaSrcProp":
						mediaSrcProp = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Name":
						name = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "SequenceStart":
						sequenceStart = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						break;
					case "VolumeLabel":
						volumeLabel = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == name)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Name"));
			}
			else if (CompilerCore.IllegalEmptyAttributeValue != name)
			{
				if (8 < name.Length) // check the length
				{
					this.core.OnMessage(WixErrors.FamilyNameTooLong(sourceLineNumbers, node.Name, "Name", name, name.Length));
				}
				else // check for illegal characters
				{
					foreach (char character in name)
					{
						if (!Char.IsLetterOrDigit(character) && '_' != character)
						{
							this.core.OnMessage(WixErrors.IllegalFamilyName(sourceLineNumbers, node.Name, "Name", name));
						}
					}
				}
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					switch (child.LocalName)
					{
						case "UpgradeImage":
							this.ParseUpgradeImageElement(child, name);
							break;
						case "ExternalFile":
							this.ParseExternalFileElement(child, name);
							break;
						case "ProtectFile":
							this.ParseProtectFileElement(child, name);
							break;
						default:
							this.core.UnexpectedElement(node, child);
							break;
					}
				}
			}

			Row row = this.core.CreateRow(sourceLineNumbers, "ImageFamilies");
			row[0] = name;
			row[1] = mediaSrcProp;
			if (CompilerCore.IntegerNotSet != diskId)
			{
				row[2] = diskId;
			}
			if (CompilerCore.IntegerNotSet != sequenceStart)
			{
				row[3] = sequenceStart;
			}
			row[4] = diskPrompt;
			row[5] = volumeLabel;
		}

		/// <summary>
		/// Parses an upgrade image element.
		/// </summary>
		/// <param name="node">The element to parse.</param>
		/// <param name="family">The family for this element.</param>
		private void ParseUpgradeImageElement(XmlNode node, string family)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string sourceFile = null;
			string sourcePatch = null;
			string symbols = null;
			string upgrade = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						upgrade = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "SourceFile":
					case "src":
						if (null != sourceFile)
						{
							this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name, "src", "SourceFile"));
						}
						sourceFile = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "SourcePatch":
					case "srcPatch":
						if (null != sourcePatch)
						{
							this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name, "srcPatch", "SourcePatch"));
						}
						sourcePatch = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == upgrade)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}

			if (null == sourceFile)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "SourceFile"));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					switch (child.LocalName)
					{
						case "SymbolPath":
							if (null != symbols)
							{
								symbols = String.Concat(symbols, ";", this.ParseSymbolPathElement(child));
							}
							else
							{
								symbols = this.ParseSymbolPathElement(child);
							}
							break;
						case "TargetImage":
							this.ParseTargetImageElement(child, upgrade, family);
							break;
						case "UpgradeFile":
							this.ParseUpgradeFileElement(child, upgrade);
							break;
						default:
							this.core.UnexpectedElement(node, child);
							break;
					}
				}
			}

			Row row = this.core.CreateRow(sourceLineNumbers, "UpgradedImages");
			row[0] = upgrade;
			row[1] = sourceFile;
			row[2] = sourcePatch;
			row[3] = symbols;
			row[4] = family;
		}

		/// <summary>
		/// Parses an upgrade file element.
		/// </summary>
		/// <param name="node">The element to parse.</param>
		/// <param name="upgrade">The upgrade key for this element.</param>
		private void ParseUpgradeFileElement(XmlNode node, string upgrade)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			bool allowIgnoreOnError = false;
			string file = null;
			bool ignore = false;
			string symbols = null;
			bool wholeFile = false;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "AllowIgnoreOnError":
						allowIgnoreOnError = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
						break;
					case "File":
						file = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Ignore":
						ignore = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
						break;
					case "WholeFile":
						wholeFile = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == file)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "File"));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					switch (child.LocalName)
					{
						case "SymbolPath":
							if (null != symbols)
							{
								symbols = String.Concat(symbols, ";", this.ParseSymbolPathElement(child));
							}
							else
							{
								symbols = this.ParseSymbolPathElement(child);
							}
							break;
						default:
							this.core.UnexpectedElement(node, child);
							break;
					}
				}
			}

			if (ignore)
			{
				Row row = this.core.CreateRow(sourceLineNumbers, "UpgradedFilesToIgnore");
				row[0] = upgrade;
				row[1] = file;
			}
			else
			{
				Row row = this.core.CreateRow(sourceLineNumbers, "UpgradedFiles_OptionalData");
				row[0] = upgrade;
				row[1] = file;
				row[2] = symbols;
				row[3] = allowIgnoreOnError ? 1 : 0;
				row[4] = wholeFile ? 1 : 0;
			}
		}

		/// <summary>
		/// Parses a target image element.
		/// </summary>
		/// <param name="node">The element to parse.</param>
		/// <param name="upgrade">The upgrade key for this element.</param>
		/// <param name="family">The family key for this element.</param>
		private void ParseTargetImageElement(XmlNode node, string upgrade, string family)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			bool ignore = false;
			int order = CompilerCore.IntegerNotSet;
			string sourceFile = null;
			string symbols = null;
			string target = null;
			string validation = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						target = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "IgnoreMissingFiles":
						ignore = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
						break;
					case "Order":
						order = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						break;
					case "SourceFile":
					case "src":
						if (null != sourceFile)
						{
							this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name, "src", "SourceFile"));
						}
						sourceFile = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Validation":
						validation = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == target)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}

			if (null == sourceFile)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "SourceFile"));
			}
			if (CompilerCore.IntegerNotSet == order)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Order"));
				order = CompilerCore.IllegalInteger;
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					switch (child.LocalName)
					{
						case "SymbolPath":
							if (null != symbols)
							{
								symbols = String.Concat(symbols, ";", this.ParseSymbolPathElement(child));
							}
							else
							{
								symbols = this.ParseSymbolPathElement(child);
							}
							break;
						case "TargetFile":
							this.ParseTargetFileElement(child, target, family);
							break;
						default:
							this.core.UnexpectedElement(node, child);
							break;
					}
				}
			}

			Row row = this.core.CreateRow(sourceLineNumbers, "TargetImages");
			row[0] = target;
			row[1] = sourceFile;
			row[2] = symbols;
			row[3] = upgrade;
			row[4] = order;
			row[5] = validation;
			row[6] = ignore ? 1 : 0;
		}

		/// <summary>
		/// Parses an upgrade file element.
		/// </summary>
		/// <param name="node">The element to parse.</param>
		/// <param name="target">The upgrade key for this element.</param>
		/// <param name="family">The family key for this element.</param>
		private void ParseTargetFileElement(XmlNode node, string target, string family)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string file = null;
			string ignoreLengths = null;
			string ignoreOffsets = null;
			string protectLengths = null;
			string protectOffsets = null;
			string symbols = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						file = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == file)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					switch (child.LocalName)
					{
						case "IgnoreRange":
							this.ParseRangeElement(child, ref ignoreOffsets, ref ignoreLengths);
							break;
						case "ProtectRange":
							this.ParseRangeElement(child, ref protectOffsets, ref protectLengths);
							break;
						case "SymbolPath":
							symbols = this.ParseSymbolPathElement(child);
							break;
						default:
							this.core.UnexpectedElement(node, child);
							break;
					}
				}
			}

			Row row = this.core.CreateRow(sourceLineNumbers, "TargetFiles_OptionalData");
			row[0] = target;
			row[1] = file;
			row[2] = symbols;
			row[3] = ignoreOffsets;
			row[4] = ignoreLengths;

			if (null != protectOffsets)
			{
				row[5] = protectOffsets;

				Row row2 = this.core.CreateRow(sourceLineNumbers, "FamilyFileRanges");
				row2[0] = family;
				row2[1] = file;
				row2[2] = protectOffsets;
				row2[3] = protectLengths;
			}
		}

		/// <summary>
		/// Parses an external file element.
		/// </summary>
		/// <param name="node">The element to parse.</param>
		/// <param name="family">The family for this element.</param>
		private void ParseExternalFileElement(XmlNode node, string family)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string file = null;
			string ignoreLengths = null;
			string ignoreOffsets = null;
			int order = CompilerCore.IntegerNotSet;
			string protectLengths = null;
			string protectOffsets = null;
			string source = null;
			string symbols = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "File":
						file = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Order":
						order = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						break;
					case "Source":
					case "src":
						if (null != source)
						{
							this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name, "src", "Source"));
						}
						source = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == file)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "File"));
			}

			if (null == source)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Source"));
			}
			if (CompilerCore.IntegerNotSet == order)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Order"));
				order = CompilerCore.IllegalInteger;
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					switch (child.LocalName)
					{
						case "IgnoreRange":
							this.ParseRangeElement(child, ref ignoreOffsets, ref ignoreLengths);
							break;
						case "ProtectRange":
							this.ParseRangeElement(child, ref protectOffsets, ref protectLengths);
							break;
						case "SymbolPath":
							symbols = this.ParseSymbolPathElement(child);
							break;
						default:
							this.core.UnexpectedElement(node, child);
							break;
					}
				}
			}

			Row row = this.core.CreateRow(sourceLineNumbers, "ExternalFiles");
			row[0] = family;
			row[1] = file;
			row[2] = source;
			row[3] = symbols;
			row[4] = ignoreOffsets;
			row[5] = ignoreLengths;
			if (null != protectOffsets)
			{
				row[6] = protectOffsets;
			}
			row[7] = order;

			if (null != protectOffsets)
			{
				Row row2 = this.core.CreateRow(sourceLineNumbers, "FamilyFileRanges");
				row2[0] = family;
				row2[1] = file;
				row2[2] = protectOffsets;
				row2[3] = protectLengths;
			}
		}

		/// <summary>
		/// Parses a protect file element.
		/// </summary>
		/// <param name="node">The element to parse.</param>
		/// <param name="family">The family for this element.</param>
		private void ParseProtectFileElement(XmlNode node, string family)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string file = null;
			string protectLengths = null;
			string protectOffsets = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "File":
						file = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == file)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "File"));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					switch (child.LocalName)
					{
						case "ProtectRange":
							this.ParseRangeElement(child, ref protectOffsets, ref protectLengths);
							break;
						default:
							this.core.UnexpectedElement(node, child);
							break;
					}
				}
			}

			Row row = this.core.CreateRow(sourceLineNumbers, "FamilyFileRanges");
			row[0] = family;
			row[1] = file;
			row[2] = protectOffsets;
			row[3] = protectLengths;
		}

		/// <summary>
		/// Parses a range element (ProtectRange, IgnoreRange, etc).
		/// </summary>
		/// <param name="node">The element to parse.</param>
		/// <param name="offsets">Reference to the offsets string.</param>
		/// <param name="lengths">Reference to the lengths string.</param>
		private void ParseRangeElement(XmlNode node, ref string offsets, ref string lengths)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string length = null;
			string offset = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Length":
						length = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Offset":
						offset = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == length)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Length"));
			}
			if (null == offset)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Offset"));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			if (null != lengths)
			{
				lengths = String.Concat(lengths, ",", length);
			}
			else
			{
				lengths = length;
			}

			if (null != offsets)
			{
				offsets = String.Concat(offsets, ",", offset);
			}
			else
			{
				offsets = offset;
			}
		}

		/// <summary>
		/// Parses a patch property element.
		/// </summary>
		/// <param name="node">The element to parse.</param>
		private void ParsePatchPropertyElement(XmlNode node)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string name = null;
			string value = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Name":
						name = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Value":
						value = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == name)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Name"));
			}
			if (null == value)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Value"));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			this.ProcessProperties(sourceLineNumbers, name, value);
		}

		/// <summary>
		/// Parses a patch sequence element.
		/// </summary>
		/// <param name="node">The element to parse.</param>
		private void ParsePatchSequenceElement(XmlNode node)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string family = null;
			string target = null;
			string sequence = null;
			int supersede = CompilerCore.IntegerNotSet;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "PatchFamily":
						family = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Target":
						target = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Sequence":
						sequence = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Supersede":
						supersede = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == family)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "PatchFamily"));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			Row row = this.core.CreateRow(sourceLineNumbers, "PatchSequence");
			row[0] = family;
			row[1] = target;
			row[2] = sequence;
			if (CompilerCore.IntegerNotSet != supersede)
			{
				row[3] = supersede;
			}
		}

		/// <summary>
		/// Parses a TargetProductCode element.
		/// </summary>
		/// <param name="node">The element to parse.</param>
		/// <returns>The id from the node.</returns>
		private string ParseTargetProductCodeElement(XmlNode node)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						if (CompilerCore.IllegalEmptyAttributeValue != id && "*" != id)
						{
							id = this.core.GetAttributeGuidValue(sourceLineNumbers, attrib, false);
						}
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			return id;
		}

		/// <summary>
		/// Parses a ReplacePatch element.
		/// </summary>
		/// <param name="node">The element to parse.</param>
		/// <returns>The id from the node.</returns>
		private string ParseReplacePatchElement(XmlNode node)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeGuidValue(sourceLineNumbers, attrib, false);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			return id;
		}

		/// <summary>
		/// Parses a symbol path element.
		/// </summary>
		/// <param name="node">The element to parse.</param>
		/// <returns>The path from the node.</returns>
		private string ParseSymbolPathElement(XmlNode node)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string path = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Path":
						path = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == path)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Path"));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			return path;
		}

		/// <summary>
		/// Adds a row to the properties table.
		/// </summary>
		/// <param name="sourceLineNumbers">Source line numbers.</param>
		/// <param name="name">Name of the property.</param>
		/// <param name="value">Value of the property.</param>
		private void ProcessProperties(SourceLineNumberCollection sourceLineNumbers, string name, object value)
		{
			Row row = this.core.CreateRow(sourceLineNumbers, "Properties");
			row[0] = name;
			row[1] = value;
		}

		/// <summary>
		/// Parses a dependency element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="activeModule">Name of the active module.</param>
		/// <param name="activeLanguage">Language of the active module.</param>
		private void ParseDependencyElement(XmlNode node, string activeModule, string activeLanguage)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string requiredId = null;
			int requiredLanguage = CompilerCore.IntegerNotSet;
			string requiredVersion = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "RequiredId":
						requiredId = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						break;
					case "RequiredLanguage":
						requiredLanguage = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						break;
					case "RequiredVersion":
						requiredVersion = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == requiredId)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "RequiredId"));
				requiredId = CompilerCore.IllegalIdentifier;
			}

			if (CompilerCore.IntegerNotSet == requiredLanguage)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "RequiredLanguage"));
				requiredLanguage = CompilerCore.IllegalInteger;
			}

			// find unexpected child elements
			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			Row row = this.core.CreateRow(sourceLineNumbers, "ModuleDependency");
			row[0] = activeModule;
			row[1] = activeLanguage;
			row[2] = requiredId;
			row[3] = requiredLanguage;
			row[4] = requiredVersion;
		}

		/// <summary>
		/// Parses an exclusion element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="activeModule">Name of the active module.</param>
		/// <param name="activeLanguage">Language of the active module.</param>
		private void ParseExclusionElement(XmlNode node, string activeModule, string activeLanguage)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string excludedId = null;
			int excludeExceptLanguage = CompilerCore.IntegerNotSet;
			int excludeLanguage = CompilerCore.IntegerNotSet;
			int excludedLanguageField = 0;
			string excludedMaxVersion = null;
			string excludedMinVersion = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "ExcludedId":
						excludedId = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						break;
					case "ExcludeExceptLanguage":
						excludeExceptLanguage = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						break;
					case "ExcludeLanguage":
						excludeLanguage = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						break;
					case "ExcludedMaxVersion":
						excludedMaxVersion = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "ExcludedMinVersion":
						excludedMinVersion = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == excludedId)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "ExcludedId"));
				excludedId = CompilerCore.IllegalIdentifier;
			}

			if (CompilerCore.IntegerNotSet != excludeExceptLanguage && CompilerCore.IntegerNotSet != excludeLanguage)
			{
				this.core.OnMessage(WixErrors.IllegalModuleExclusionLanguageAttributes(sourceLineNumbers));
				excludedLanguageField = CompilerCore.IllegalInteger;
			}
			else if (CompilerCore.IntegerNotSet != excludeExceptLanguage)
			{
				excludedLanguageField = -excludeExceptLanguage;
			}
			else if (CompilerCore.IntegerNotSet != excludeLanguage)
			{
				excludedLanguageField = excludeLanguage;
			}

			// find unexpected child elements
			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			Row row = this.core.CreateRow(sourceLineNumbers, "ModuleExclusion");
			row[0] = activeModule;
			row[1] = activeLanguage;
			row[2] = excludedId;
			row[3] = excludedLanguageField;
			row[4] = excludedMinVersion;
			row[5] = excludedMaxVersion;
		}

		/// <summary>
		/// Parses a configuration element for a configurable merge module.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		private void ParseConfigurationElement(XmlNode node)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			int attributes = 0;
			string contextData = null;
			string defaultValue = null;
			string description = null;
			string displayName = null;
			int format = CompilerCore.IntegerNotSet;
			string helpKeyword = null;
			string helpLocation = null;
			string name = null;
			string type = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Name":
						name = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						break;
					case "ContextData":
						contextData = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Description":
						description = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "DefaultValue":
						defaultValue = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "DisplayName":
						displayName = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Format":
						string formatStr = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						switch (formatStr)
						{
							case CompilerCore.IllegalEmptyAttributeValue: // this error is already handled
								break;
							case "Text":
								format = 0;
								break;
							case "Key":
								format = 1;
								break;
							case "Integer":
								format = 2;
								break;
							case "Bitfield":
								format = 3;
								break;
							default:
								this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name, "Format", formatStr, "Text", "Key", "Integer", "Bitfield"));
								break;
						}
						break;
					case "HelpKeyword":
						helpKeyword = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "HelpLocation":
						helpLocation = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "KeyNoOrphan":
						attributes |= 0x00000001;
						break;
					case "NonNullable":
						attributes |= 0x00000002;
						break;
					case "Type":
						type = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == name)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Name"));
				name = CompilerCore.IllegalIdentifier;
			}

			if (CompilerCore.IntegerNotSet == format)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Format"));
				format = CompilerCore.IllegalInteger;
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			Row row = this.core.CreateRow(sourceLineNumbers, "ModuleConfiguration");
			row[0] = name;
			row[1] = format;
			row[2] = type;
			row[3] = contextData;
			row[4] = defaultValue;
			row[5] = attributes;
			row[6] = displayName;
			row[7] = description;
			row[8] = helpLocation;
			row[9] = helpKeyword;
		}

		/// <summary>
		/// Parses a substitution element for a configurable merge module.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		private void ParseSubstitutionElement(XmlNode node)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string column = null;
			string rowKeys = null;
			string table = null;
			string value = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Column":
						column = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						break;
					case "Row":
						rowKeys = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Table":
						table = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						break;
					case "Value":
						value = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == column)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Column"));
				column = CompilerCore.IllegalIdentifier;
			}
			if (null == table)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Table"));
				table = CompilerCore.IllegalIdentifier;
			}
			if (null == rowKeys)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Row"));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			Row row = this.core.CreateRow(sourceLineNumbers, "ModuleSubstitution");
			row[0] = table;
			row[1] = rowKeys;
			row[2] = column;
			row[3] = value;
		}

		/// <summary>
		/// Parses an odbc driver or translator element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="componentId">Identifier of parent component.</param>
		/// <param name="fileId">Default identifer for driver/translator file.</param>
		/// <param name="table">Table we're processing for.</param>
		private void ParseODBCDriverOrTranslator(XmlNode node, string componentId, string fileId, TableDefinition table)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;
			string driver = fileId;
			string name = null;
			string setup = fileId;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						break;
					case "File":
						driver = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						break;
					case "Name":
						name = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "SetupFile":
						setup = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}
			if (null == name)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Name"));
			}

			// drivers have a few possible children
			if ("ODBCDriver" == table.Name)
			{
				// process any data sources for the driver
				foreach (XmlNode child in node.ChildNodes)
				{
					if (XmlNodeType.Element == child.NodeType)
					{
						switch (child.LocalName)
						{
							case "ODBCDataSource":
								string ignoredKeyPath = null;
								this.ParseODBCDataSource(child, componentId, name, out ignoredKeyPath);
								break;
							case "Property":
								this.ParseODBCProperty(child, id, "ODBCAttribute");
								break;
							default:
								this.core.UnexpectedElement(node, child);
								break;
						}
					}
				}
			}
			else
			{
				foreach (XmlNode child in node.ChildNodes)
				{
					if (XmlNodeType.Element == child.NodeType)
					{
						this.core.UnexpectedElement(node, child);
					}
				}
			}

			Row row = this.core.CreateRow(sourceLineNumbers, table.Name);
			row[0] = id;
			row[1] = componentId;
			row[2] = name;
			row[3] = driver;
			row[4] = setup;
		}

		/// <summary>
		/// Parses a Property element underneath an ODBC driver or translator.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="parentId">Identifier of parent driver or translator.</param>
		/// <param name="tableName">Name of the table to create property in.</param>
		private void ParseODBCProperty(XmlNode node, string parentId, string tableName)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;
			string propertyValue = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Value":
						propertyValue = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			Row row = this.core.CreateRow(sourceLineNumbers, tableName);
			row[0] = parentId;
			row[1] = id;
			row[2] = propertyValue;
		}

		/// <summary>
		/// Parse an odbc data source element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="componentId">Identifier of parent component.</param>
		/// <param name="driverName">Default name of driver.</param>
		/// <param name="possibleKeyPath">Identifier of this element in case it is a keypath.</param>
		/// <returns>True if this element was marked as the parent component's key path.</returns>
		private bool ParseODBCDataSource(XmlNode node, string componentId, string driverName, out string possibleKeyPath)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;
			bool keyPath = false;
			string name = null;
			int registration = CompilerCore.IntegerNotSet;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						break;
					case "DriverName":
						driverName = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "KeyPath":
						keyPath = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
						break;
					case "Name":
						name = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Registration":
						string registrationValue = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						switch (registrationValue)
						{
							case CompilerCore.IllegalEmptyAttributeValue: // this error is already handled
								break;
							case "machine":
								registration = 0;
								break;
							case "user":
								registration = 1;
								break;
							default:
								this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name, "Registration", registrationValue, "machine", "user"));
								break;
						}
						break;
				}
			}

			if (null == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}
			if (CompilerCore.IntegerNotSet == registration)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Registration"));
				registration = CompilerCore.IllegalInteger;
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					switch (child.LocalName)
					{
						case "Property":
							this.ParseODBCProperty(child, id, "ODBCSourceAttribute");
							break;
						default:
							this.core.UnexpectedElement(node, child);
							break;
					}
				}
			}

			Row row = this.core.CreateRow(sourceLineNumbers, "ODBCDataSource");
			row[0] = id;
			row[1] = componentId;
			row[2] = name;
			row[3] = driverName;
			row[4] = registration;

			possibleKeyPath = id;
			return keyPath;
		}

		/// <summary>
		/// Parses a package element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="productAuthor">Default package author.</param>
		private void ParsePackageElement(XmlNode node, string productAuthor)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string codepage = "1252";
			string comments = String.Format(CultureInfo.InvariantCulture, "This installer database contains the logic and data required to install {0}.", this.activeName);
			string keywords = "Installer";
			int msiVersion = 100; // lowest released version, really should be specified
			string packageAuthor = productAuthor;
			string packageCode = null;
			string packageLanguages = this.activeLanguage;
			string packageName = this.activeName;
			string platforms = null;
			YesNoDefaultType security = YesNoDefaultType.Default;
			int sourceBits = 0;
			Row row;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						packageCode = this.core.GetAttributeGuidValue(sourceLineNumbers, attrib, true);
						break;
					case "AdminImage":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							sourceBits = sourceBits | 4;
						}
						else
						{
							sourceBits = sourceBits & ~4;
						}
						break;
					case "Comments":
						comments = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Compressed":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							sourceBits = sourceBits | 2;
						}
						else
						{
							sourceBits = sourceBits & ~2;
						}
						break;
					case "Description":
						packageName = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "InstallPrivileges":
						string installPrivileges = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						switch (installPrivileges)
						{
							case CompilerCore.IllegalEmptyAttributeValue: // this error is already handled
								break;
							case "elevated":
								sourceBits = sourceBits & ~8;
								break;
							case "limited":
								sourceBits = sourceBits | 8;
								break;
							default:
								this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name, attrib.Name, installPrivileges, "elevated", "limited"));
								break;
						}
						break;
					case "InstallerVersion":
						msiVersion = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						break;
					case "Keywords":
						keywords = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Languages":
						packageLanguages = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Manufacturer":
						packageAuthor = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Platforms":
						platforms = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "ReadOnly":
						security = this.core.GetAttributeYesNoDefaultValue(sourceLineNumbers, attrib);
						break;
					case "ShortNames":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							sourceBits = sourceBits | 1;
							this.useShortFileNames = true;
						}
						else
						{
							sourceBits = sourceBits & ~1;
						}
						break;
					case "SummaryCodepage":
						codepage = this.core.GetAttributeLocalizableIntegerValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == packageCode) // generate a package code if one wasn't provided
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}
			if (null == packageAuthor)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Manufacturer"));
			}

			// check if codepage is an integer, and if so, make sure it is a valid codepage (greater than zero)
			try
			{
				int integerCodepage = Convert.ToInt32(codepage, CultureInfo.InvariantCulture.NumberFormat);
				if (0 >= integerCodepage)
				{
					this.core.OnMessage(WixErrors.InvalidSummaryCodepage(sourceLineNumbers, node.Name, integerCodepage));
				}
			}
			catch (FormatException)
			{
				// ignore the exception if the codepage was a $(loc.variable)
			}
			catch (OverflowException)
			{
				// ignore the exception if the codepage was a $(loc.variable)
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			row = this.core.CreateRow(sourceLineNumbers, "_SummaryInformation");
			row[0] = 1;
			row[1] = codepage;

			row = this.core.CreateRow(sourceLineNumbers, "_SummaryInformation");
			row[0] = 2;
			row[1] = "Installation Database";

			row = this.core.CreateRow(sourceLineNumbers, "_SummaryInformation");
			row[0] = 3;
			row[1] = packageName;

			row = this.core.CreateRow(sourceLineNumbers, "_SummaryInformation");
			row[0] = 4;
			row[1] = packageAuthor;

			row = this.core.CreateRow(sourceLineNumbers, "_SummaryInformation");
			row[0] = 5;
			row[1] = keywords;

			row = this.core.CreateRow(sourceLineNumbers, "_SummaryInformation");
			row[0] = 6;
			row[1] = comments;

			row = this.core.CreateRow(sourceLineNumbers, "_SummaryInformation");
			row[0] = 7;
			row[1] = String.Format(CultureInfo.InvariantCulture, "{0};{1}", platforms, packageLanguages);

			row = this.core.CreateRow(sourceLineNumbers, "_SummaryInformation");
			row[0] = 9;
			row[1] = String.Concat("{", packageCode, "}");

			row = this.core.CreateRow(sourceLineNumbers, "_SummaryInformation");
			row[0] = 14;
			row[1] = msiVersion;

			row = this.core.CreateRow(sourceLineNumbers, "_SummaryInformation");
			row[0] = 15;
			row[1] = sourceBits;

			row = this.core.CreateRow(sourceLineNumbers, "_SummaryInformation");
			row[0] = 19;
			switch (security)
			{
				case YesNoDefaultType.No: // no restriction
					row[1] = 0;
					break;
				case YesNoDefaultType.Default: // read-only recommended
					row[1] = 2;
					break;
				case YesNoDefaultType.Yes: // read-only enforced
					row[1] = 4;
					break;
			}
		}

		/// <summary>
		/// Parses a patch metadata element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		private void ParsePatchMetadataElement(XmlNode node)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			YesNoType allowRemoval = YesNoType.NotSet;
			string classification = null;
			string creationTimeUtc = null;
			string description = null;
			string displayName = null;
			string manufacturerName = null;
			string minorUpdateTargetRTM = null;
			string moreInfoUrl = null;
			YesNoType optimizedInstallMode = YesNoType.NotSet;
			string targetProductName = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "AllowRemoval":
						allowRemoval = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
						break;
					case "Classification":
						classification = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						switch (classification)
						{
							case CompilerCore.IllegalEmptyAttributeValue: // this error is already handled
								break;
							case "Critical Update":
							case "Hotfix":
							case "Security Rollup":
							case "Service Pack":
							case "Update":
							case "Update Rollup":
								break;
							default:
								this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name, attrib.Name, classification, "Critical Update", "Hotfix", "Security Rollup", "Service Pack", "Update", "Update Rollup"));
								break;
						}
						break;
					case "CreationTimeUTC":
						creationTimeUtc = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Description":
						description = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "DisplayName":
						displayName = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "ManufacturerName":
						manufacturerName = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "MinorUpdateTargetRTM":
						minorUpdateTargetRTM = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "MoreInfoURL":
						moreInfoUrl = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "OptimizedInstallMode":
						optimizedInstallMode = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
						break;
					case "TargetProductName":
						targetProductName = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (YesNoType.NotSet == allowRemoval)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "AllowRemoval"));
			}
			if (null == classification)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Classification"));
			}
			if (null == description)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Description"));
			}
			if (null == displayName)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "DisplayName"));
			}
			if (null == manufacturerName)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "ManufacturerName"));
			}
			if (null == targetProductName)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "TargetProductName"));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					switch (child.LocalName)
					{
						case "CustomProperty":
							this.ParseCustomPropertyElement(child);
							break;
						default:
							this.core.UnexpectedElement(node, child);
							break;
					}
				}
			}

			if (YesNoType.NotSet != allowRemoval)
			{
				Row row = this.core.CreateRow(sourceLineNumbers, "PatchMetadata");
				row[0] = null;
				row[1] = "AllowRemoval";
				row[2] = YesNoType.Yes == allowRemoval ? 1 : 0;
			}

			if (null != classification)
			{
				Row row = this.core.CreateRow(sourceLineNumbers, "PatchMetadata");
				row[0] = null;
				row[1] = "Classification";
				row[2] = classification;
			}

			if (null != creationTimeUtc)
			{
				Row row = this.core.CreateRow(sourceLineNumbers, "PatchMetadata");
				row[0] = null;
				row[1] = "CreationTimeUTC";
				row[2] = creationTimeUtc;
			}

			if (null != description)
			{
				Row row = this.core.CreateRow(sourceLineNumbers, "PatchMetadata");
				row[0] = null;
				row[1] = "Description";
				row[2] = description;
			}

			if (null != displayName)
			{
				Row row = this.core.CreateRow(sourceLineNumbers, "PatchMetadata");
				row[0] = null;
				row[1] = "DisplayName";
				row[2] = displayName;
			}

			if (null != manufacturerName)
			{
				Row row = this.core.CreateRow(sourceLineNumbers, "PatchMetadata");
				row[0] = null;
				row[1] = "ManufacturerName";
				row[2] = manufacturerName;
			}

			if (null != minorUpdateTargetRTM)
			{
				Row row = this.core.CreateRow(sourceLineNumbers, "PatchMetadata");
				row[0] = null;
				row[1] = "MinorUpdateTargetRTM";
				row[2] = minorUpdateTargetRTM;
			}

			if (null != moreInfoUrl)
			{
				Row row = this.core.CreateRow(sourceLineNumbers, "PatchMetadata");
				row[0] = null;
				row[1] = "MoreInfoURL";
				row[2] = moreInfoUrl;
			}

			if (YesNoType.NotSet != optimizedInstallMode)
			{
				Row row = this.core.CreateRow(sourceLineNumbers, "PatchMetadata");
				row[0] = null;
				row[1] = "OptimizedInstallMode";
				row[2] = YesNoType.Yes == optimizedInstallMode ? 1 : 0;
			}

			if (null != targetProductName)
			{
				Row row = this.core.CreateRow(sourceLineNumbers, "PatchMetadata");
				row[0] = null;
				row[1] = "TargetProductName";
				row[2] = targetProductName;
			}
		}

		/// <summary>
		/// Parses a custom property element for the PatchMetadata table.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		private void ParseCustomPropertyElement(XmlNode node)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string company = null;
			string property = null;
			string value = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Company":
						company = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Property":
						property = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Value":
						value = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == company)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Company"));
			}
			if (null == property)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Property"));
			}
			if (null == value)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Value"));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			Row row = this.core.CreateRow(sourceLineNumbers, "PatchMetadata");
			row[0] = company;
			row[1] = property;
			row[2] = value;
		}

		/// <summary>
		/// Parses a patch information element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		private void ParsePatchInformationElement(XmlNode node)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string codepage = "1252";
			string comments = null;
			string keywords = "Installer,Patching,PCP,Database";
			int msiVersion = 1; // Should always be 1 for patches
			string packageAuthor = null;
			string packageCode = null;
			string packageLanguages = this.activeLanguage;
			string packageName = this.activeName;
			string platform = null;
			YesNoDefaultType security = YesNoDefaultType.Default;
			int sourceBits = 0;
			Row row;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "AdminImage":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							sourceBits = sourceBits | 4;
						}
						else
						{
							sourceBits = sourceBits & ~4;
						}
						break;
					case "Comments":
						comments = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Compressed":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							sourceBits = sourceBits | 2;
						}
						else
						{
							sourceBits = sourceBits & ~2;
						}
						break;
					case "Description":
						packageName = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Keywords":
						keywords = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Languages":
						packageLanguages = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Manufacturer":
						packageAuthor = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Platforms":
						platform = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "ReadOnly":
						security = this.core.GetAttributeYesNoDefaultValue(sourceLineNumbers, attrib);
						break;
					case "ShortNames":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							sourceBits = sourceBits | 1;
							this.useShortFileNames = true;
						}
						else
						{
							sourceBits = sourceBits & ~1;
						}
						break;
					case "SummaryCodepage":
						codepage = this.core.GetAttributeLocalizableIntegerValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			// check if codepage is an integer, and if so, make sure it is a valid codepage (greater than zero)
			try
			{
				int integerCodepage = Convert.ToInt32(codepage, CultureInfo.InvariantCulture.NumberFormat);
				if (0 >= integerCodepage)
				{
					this.core.OnMessage(WixErrors.InvalidSummaryCodepage(sourceLineNumbers, node.Name, integerCodepage));
				}
			}
			catch (FormatException)
			{
				// ignore the exception if the codepage was a $(loc.variable)
			}
			catch (OverflowException)
			{
				// ignore the exception if the codepage was a $(loc.variable)
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			row = this.core.CreateRow(sourceLineNumbers, "_SummaryInformation");
			row[0] = 1;
			row[1] = codepage;

			row = this.core.CreateRow(sourceLineNumbers, "_SummaryInformation");
			row[0] = 2;
			row[1] = "Installation Database";

			row = this.core.CreateRow(sourceLineNumbers, "_SummaryInformation");
			row[0] = 3;
			row[1] = packageName;

			if (null != packageAuthor)
			{
				row = this.core.CreateRow(sourceLineNumbers, "_SummaryInformation");
				row[0] = 4;
				row[1] = packageAuthor;
			}

			row = this.core.CreateRow(sourceLineNumbers, "_SummaryInformation");
			row[0] = 5;
			row[1] = keywords;

			if (null != comments)
			{
				row = this.core.CreateRow(sourceLineNumbers, "_SummaryInformation");
				row[0] = 6;
				row[1] = comments;
			}

			row = this.core.CreateRow(sourceLineNumbers, "_SummaryInformation");
			row[0] = 7;
			row[1] = String.Format(CultureInfo.InvariantCulture, "{0};{1}", platform, packageLanguages);

			if (null != packageCode)
			{
				row = this.core.CreateRow(sourceLineNumbers, "_SummaryInformation");
				row[0] = 9;
				row[1] = packageCode;
			}

			row = this.core.CreateRow(sourceLineNumbers, "_SummaryInformation");
			row[0] = 14;
			row[1] = msiVersion;

			row = this.core.CreateRow(sourceLineNumbers, "_SummaryInformation");
			row[0] = 15;
			row[1] = sourceBits;

			row = this.core.CreateRow(sourceLineNumbers, "_SummaryInformation");
			row[0] = 19;
			switch (security)
			{
				case YesNoDefaultType.No: // no restriction
					row[1] = 0;
					break;
				case YesNoDefaultType.Default: // read-only recommended
					row[1] = 2;
					break;
				case YesNoDefaultType.Yes: // read-only enforced
					row[1] = 4;
					break;
			}
		}

		/// <summary>
		/// Parses a perf counter element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="componentId">Identifier of parent component.</param>
		/// <param name="fileId">Identifier of referenced file.</param>
		private void ParsePerfCounterElement(XmlNode node, string componentId, string fileId)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string name = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Name":
						name = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == name)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Name"));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			Row row = this.core.CreateRow(sourceLineNumbers, "Perfmon");
			row[0] = componentId;
			row[1] = String.Concat("[!", fileId, "]");
			row[2] = name;

			// Reference ConfigurePerfmonInstall and ConfigurePerfmonUninstall since nothing will happen without them
			this.core.AddValidReference("CustomAction", "ConfigurePerfmonInstall");
			this.core.AddValidReference("CustomAction", "ConfigurePerfmonUninstall");
		}

		/// <summary>
		/// Parses an ignore modularization element.
		/// </summary>
		/// <param name="node">XmlNode on an IgnoreModulatization element.</param>
		private void ParseIgnoreModularizationElement(XmlNode node)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string name = null;
			string type = "none";

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Name":
						name = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Type":
						type = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == name)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Name"));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			if (null != name && CompilerCore.IllegalEmptyAttributeValue != name && null != type && CompilerCore.IllegalEmptyAttributeValue != type)
			{
				this.core.AddIgnoreModularization(new IgnoreModularization(name, type));
			}
		}

		/// <summary>
		/// Parses a permission element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="objectId">Identifier of object to be secured.</param>
		/// <param name="tableName">Name of table that contains objectId.</param>
		private void ParsePermissionElement(XmlNode node, string objectId, string tableName)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			long bits = 0;
			string domain = null;
			string[] specialPermissions = null;
			string user = null;

			PermissionType permissionType = PermissionType.LockPermissions;

			switch (tableName)
			{
				case "CreateFolder":
					specialPermissions = MsiInterop.FolderPermissions;
					break;
				case "File":
					specialPermissions = MsiInterop.FilePermissions;
					break;
				case "FileShare":
					specialPermissions = MsiInterop.FolderPermissions;
					permissionType = PermissionType.FileSharePermissions;
					break;
				case "Registry":
					specialPermissions = MsiInterop.RegistryPermissions;
					break;
				case "ServiceInstall":
					specialPermissions = MsiInterop.ServicePermissions;
					permissionType = PermissionType.SecureObjects;
					break;
				default:
					this.core.UnexpectedElement(node.ParentNode, node);
					break;
			}

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Domain":
						if (PermissionType.FileSharePermissions == permissionType)
						{
							this.core.OnMessage(WixErrors.IllegalAttributeWhenNested(sourceLineNumbers, node.Name, attrib.Name, node.ParentNode.Name));
						}
						domain = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Extended":
						if (PermissionType.LockPermissions == permissionType && Common.IsYes(this.core.GetAttributeValue(sourceLineNumbers, attrib), sourceLineNumbers, node.Name, attrib.Name, objectId))
						{
							permissionType = PermissionType.SecureObjects;
						}
						break;
					case "User":
						user = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						YesNoType attribValue = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
						long bit = Compiler.NameToBit(MsiInterop.StandardPermissions, attrib.Name, attribValue);
						if (-1 == bit)
						{
							bit = Compiler.NameToBit(MsiInterop.GenericPermissions, attrib.Name, attribValue);
							if (-1 == bit)
							{
								bit = Compiler.NameToBit(specialPermissions, attrib.Name, attribValue);
								if (-1 == bit)
								{
									this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
									break;
								}
							}
							else
							{
								if (8 == bit)
								{
									bit = 0x80000000;
								}
								else
								{
									bit = bit * 0x10000000;
								}
							}
						}
						else
						{
							bit = bit * 65536;
						}

						bits |= bit;
						break;
				}
			}

			if (null == user)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "User"));
			}
			if (0x80000000 == bits) // just GENERIC_READ, which is MSI_NULL
			{
				this.core.OnMessage(WixErrors.GenericReadNotAllowed(sourceLineNumbers));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			if (PermissionType.FileSharePermissions == permissionType)
			{
				this.core.AddValidReference("FileShare", objectId);
				this.core.AddValidReference("User", user);

				Row row = this.core.CreateRow(sourceLineNumbers, "FileSharePermissions");
				row[0] = objectId;
				row[1] = user;
				row[2] = bits;
			}
			else if (PermissionType.SecureObjects == permissionType)
			{
				Row row = this.core.CreateRow(sourceLineNumbers, "SecureObjects");
				row[0] = objectId;
				row[1] = tableName;
				row[2] = domain;
				row[3] = user;
				row[4] = bits;

				// Reference SchedSecureObjects since nothing will happen without it
				this.core.AddValidReference("CustomAction", "SchedSecureObjects");
			}
			else
			{
				Row row = this.core.CreateRow(sourceLineNumbers, "LockPermissions");
				row[0] = objectId;
				row[1] = tableName;
				row[2] = domain;
				row[3] = user;
				row[4] = bits;
			}
		}

		/// <summary>
		/// Parses a product element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		private void ParseProductElement(XmlNode node)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			int codepage = 0;
			string productCode = null;
			string upgradeCode = null;
			string manufacturer = null;
			string version = null;

			this.activeName = null;
			this.activeLanguage = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						productCode = this.core.GetAttributeGuidValue(sourceLineNumbers, attrib, true);
						break;
					case "Codepage":
						codepage = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						break;
					case "Language":
						this.activeLanguage = this.core.GetAttributeLocalizableIntegerValue(sourceLineNumbers, attrib);
						break;
					case "Manufacturer":
						manufacturer = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Name":
						this.activeName = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "UpgradeCode":
						upgradeCode = this.core.GetAttributeGuidValue(sourceLineNumbers, attrib, false);
						break;
					case "Version":
						version = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						if (!this.TryExtensionParseForAttribute(sourceLineNumbers, (XmlElement)node, attrib))
						{
							this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						}
						break;
				}
			}
			if (null == productCode)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}
			if (null == this.activeLanguage)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Language"));
			}
			if (null == manufacturer)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Manufacturer"));
			}
			if (null == this.activeName)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Name"));
			}
			if (null == version)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Version"));
			}

			try
			{
				this.compilingProduct = true;
				this.core.CreateActiveSection(productCode, SectionType.Product, codepage);

				this.AddProperty(sourceLineNumbers, "Manufacturer", manufacturer, false, false, false);
				this.AddProperty(sourceLineNumbers, "ProductCode", String.Concat("{", productCode, "}"), false, false, false);
				this.AddProperty(sourceLineNumbers, "ProductLanguage", this.activeLanguage, false, false, false);
				this.AddProperty(sourceLineNumbers, "ProductName", this.activeName, false, false, false);
				this.AddProperty(sourceLineNumbers, "ProductVersion", version, false, false, false);
				if (null != upgradeCode)
				{
					this.AddProperty(sourceLineNumbers, "UpgradeCode", String.Concat("{", upgradeCode, "}"), false, false, false);
				}

				int featureDisplay = 0;
				foreach (XmlNode child in node.ChildNodes)
				{
					if (XmlNodeType.Element == child.NodeType)
					{
						switch (child.LocalName)
						{
							case "_locDefinition":
								break;
							case "AdminExecuteSequence":
							case "AdminUISequence":
							case "AdvertiseExecuteSequence":
							case "InstallExecuteSequence":
							case "InstallUISequence":
								this.ParseSequenceElement(child, child.LocalName);
								break;
							case "AppId":
								this.ParseAppIdElement(child, null, YesNoType.Yes, null, null, null);
								break;
							case "Binary":
								this.ParseBinaryOrIconElement(child, BinaryType.Binary);
								break;
							case "ComplianceCheck":
								this.ParseComplianceCheckElement(child);
								break;
							case "Condition":
								this.ParseConditionElement(child, node.LocalName, null, null);
								break;
							case "CustomAction":
								this.ParseCustomActionElement(child);
								break;
							case "CustomActionRef":
								this.ParseCustomActionRefElement(child);
								break;
							case "CustomTable":
								this.ParseCustomTableElement(child);
								break;
							case "Directory":
								this.ParseDirectoryElement(child, null, String.Empty);
								break;
							case "DirectoryRef":
								this.ParseDirectoryRefElement(child);
								break;
							case "EnsureTable":
								this.ParseEnsureTableElement(child);
								break;
							case "Feature":
								this.ParseFeatureElement(child, null, ref featureDisplay);
								break;
							case "FeatureRef":
								this.ParseFeatureRefElement(child, null);
								break;
							case "FragmentRef":
								this.ParseFragmentRefElement(child);
								break;
							case "Group":
								this.ParseGroupElement(child, null);
								break;
							case "Icon":
								this.ParseBinaryOrIconElement(child, BinaryType.Icon);
								break;
							case "Media":
								this.ParseMediaElement(child);
								break;
							case "MediaRef":
								this.ParseMediaRefElement(child);
								break;
							case "Package":
								this.ParsePackageElement(child, manufacturer);
								break;
							case "PatchCertificates":
								this.ParsePatchCertificatesElement(child);
								break;
							case "Property":
								this.ParsePropertyElement(child);
								break;
							case "PropertyRef":
								this.ParsePropertyRefElement(child);
								break;
							case "SFPCatalog":
								string parentName = null;
								this.ParseSFPCatalogElement(child, ref parentName);
								break;
							case "SqlDatabase":
								this.ParseSqlDatabaseElement(child, null);
								break;
							case "UI":
								this.ParseUIElement(child);
								break;
							case "UIRef":
								this.ParseUIRefElement(child);
								break;
							case "Upgrade":
								this.ParseUpgradeElement(child);
								break;
							case "User":
								this.ParseUserElement(child, null);
								break;
							case "WebApplication":
								this.ParseWebApplicationElement(child);
								break;
							case "WebAppPool":
								this.ParseWebAppPoolElement(child, null);
								break;
							case "WebDirProperties":
								this.ParseWebDirPropertiesElement(child);
								break;
							case "WebLog":
								this.ParseWebLogElement(child);
								break;
							case "WebSite":
								this.ParseWebSiteElement(child, null);
								break;
							default:
								if (!this.TryExtensionParseForElement(sourceLineNumbers, (XmlElement)node, (XmlElement)child))
								{
									this.core.UnexpectedElement(node, child);
								}
								break;
						}
					}
				}
			}
			finally
			{
				this.compilingProduct = false;
			}
		}

		/// <summary>
		/// Parses a progid element
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="componentId">Identifier of parent component.</param>
		/// <param name="advertise">Flag if progid is advertised.</param>
		/// <param name="classId">CLSID related to ProgId.</param>
		/// <param name="description">Default description of ProgId</param>
		/// <param name="parent">Optional parent ProgId</param>
		/// <param name="foundExtension">Set to true if an extension is found; used for error-checking.</param>
		/// <param name="firstProgIdForClass">Whether or not this ProgId is the first one found in the parent class.</param>
		/// <returns>This element's Id.</returns>
		private string ParseProgIdElement(XmlNode node, string componentId, YesNoType advertise, string classId, string description, string parent, ref bool foundExtension, YesNoType firstProgIdForClass)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string icon = null;
			int iconIndex = CompilerCore.IntegerNotSet;
			string noOpen = null;
			string progId = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						progId = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Advertise":
						YesNoType progIdAdvertise = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
						if ((YesNoType.No == advertise && YesNoType.Yes == progIdAdvertise) || (YesNoType.Yes == advertise && YesNoType.No == progIdAdvertise))
						{
							this.core.OnMessage(WixErrors.AdvertiseStateMustMatch(sourceLineNumbers, advertise.ToString(CultureInfo.InvariantCulture.NumberFormat), progIdAdvertise.ToString(CultureInfo.InvariantCulture.NumberFormat)));
						}
						advertise = progIdAdvertise;
						break;
					case "Description":
						description = this.core.GetAttributeValue(sourceLineNumbers, attrib, true);
						break;
					case "Icon":
						icon = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "IconIndex":
						iconIndex = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						break;
					case "NoOpen":
						noOpen = this.core.GetAttributeValue(sourceLineNumbers, attrib, true);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}
			if (YesNoType.NotSet == advertise)
			{
				advertise = YesNoType.No;
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					switch (child.LocalName)
					{
						case "Extension":
							this.ParseExtensionElement(child, componentId, advertise, progId);
							foundExtension = true;
							break;
						case "ProgId":
							// Only allow one nested ProgId.  If we have a child, we should not have a parent.
							if (null == parent)
							{
								if (YesNoType.Yes == advertise)
								{
									this.ParseProgIdElement(child, componentId, advertise, null, description, progId, ref foundExtension, firstProgIdForClass);
								}
								else if (YesNoType.No == advertise)
								{
									this.ParseProgIdElement(child, componentId, advertise, classId, description, progId, ref foundExtension, firstProgIdForClass);
								}
							}
							else
							{
								SourceLineNumberCollection childSourceLineNumbers = this.core.GetSourceLineNumbers(child);
								this.core.OnMessage(WixErrors.ProgIdNestedTooDeep(childSourceLineNumbers));
							}
							break;
						default:
							this.core.UnexpectedElement(node, child);
							break;
					}
				}
			}

			if (YesNoType.Yes == advertise)
			{
				Row row = this.core.CreateRow(sourceLineNumbers, "ProgId");
				row[0] = progId;
				row[1] = parent;
				row[2] = classId;
				row[3] = description;
				row[4] = icon;
				if (CompilerCore.IntegerNotSet != iconIndex)
				{
					row[5] = iconIndex;
				}
			}
			else if (YesNoType.No == advertise)
			{
				this.AddRegistryKey(sourceLineNumbers, null, MsiInterop.MsidbRegistryRootClassesRoot, progId, String.Empty, description, componentId);
				if (null != classId)
				{
					this.AddRegistryKey(sourceLineNumbers, null, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat(progId, "\\CLSID"), String.Empty, classId, componentId);
					if (null != parent)   // if this is a version independent ProgId
					{
						if (YesNoType.Yes == firstProgIdForClass)
						{
							this.AddRegistryKey(sourceLineNumbers, null, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("CLSID\\", classId, "\\VersionIndependentProgID"), String.Empty, progId, componentId);
						}

						this.AddRegistryKey(sourceLineNumbers, null, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat(progId, "\\CurVer"), String.Empty, parent, componentId);
					}
					else
					{
						if (YesNoType.Yes == firstProgIdForClass)
						{
							this.AddRegistryKey(sourceLineNumbers, null, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat("CLSID\\", classId, "\\ProgID"), String.Empty, progId, componentId);
						}
					}
				}

				if (null != icon)   // ProgId's Default Icon
				{
					if (CompilerCore.IntegerNotSet != iconIndex)
					{
						icon = String.Concat(icon, ",", iconIndex);
					}

					this.AddRegistryKey(sourceLineNumbers, null, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat(progId, "\\DefaultIcon"), String.Empty, icon, componentId);
				}
			}

			if (null != noOpen)
			{
				this.AddRegistryKey(sourceLineNumbers, null, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat(".", progId), "NoOpen", noOpen, componentId); // ProgId NoOpen name
			}

			// raise an error for an orphaned ProgId
			if (YesNoType.Yes == advertise && !foundExtension && null == parent && null == classId)
			{
				this.core.OnMessage(WixWarnings.OrphanedProgId(sourceLineNumbers, WarningLevel.Moderate, progId));
			}

			return progId;
		}

		/// <summary>
		/// Parses a property element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		private void ParsePropertyElement(XmlNode node)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;
			bool admin = false;
			bool complianceCheck = false;
			bool hidden = false;
			bool secure = false;
			string value = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						break;
					case "Admin":
						admin = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
						break;
					case "ComplianceCheck":
						complianceCheck = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
						break;
					case "Hidden":
						hidden = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
						break;
					case "Secure":
						secure = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
						break;
					case "Value":
						value = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}
			if ("SecureCustomProperties" == id || "AdminProperties" == id || "MsiHiddenProperties" == id)
			{
				this.core.OnMessage(WixErrors.CannotAuthorSpecialProperties(sourceLineNumbers, id));
			}

			string innerText = this.core.GetTrimmedInnerText(node);
			if (null != value)
			{
				// cannot specify both the value attribute and inner text
				if (0 != innerText.Length)
				{
					this.core.OnMessage(WixErrors.IllegalAttributeWithInnerText(sourceLineNumbers, node.Name, "Value"));
				}
			}
			else // value attribute not specified
			{
				if (0 < innerText.Length)
				{
					value = innerText;
				}
			}

			if ("ErrorDialog" == id)
			{
				this.core.AddValidReference("Dialog", value);
			}

			// see if this property is used for appSearch
			ArrayList signatures = this.ParseSearchSignatures(node);

			// if we're doing CCP
			if (complianceCheck)
			{
				// there must be a signature
				if (0 == signatures.Count)
				{
					this.core.OnMessage(WixErrors.SearchElementRequiredWithAttribute(sourceLineNumbers, node.Name, "ComplianceCheck", "yes"));
				}
			}

			foreach (string sig in signatures)
			{
				if (complianceCheck)
				{
					Row row = this.core.CreateRow(sourceLineNumbers, "CCPSearch");
					row[0] = sig;
				}

				this.AddAppSearch(sourceLineNumbers, id, sig);
			}

			// if we're doing AppSearch get that setup
			if (0 < signatures.Count)
			{
				this.AddProperty(sourceLineNumbers, id, value, admin, secure, hidden);
			}
			else // just a normal old property
			{
				// if the property value is empty and none of the flags are set, print out a warning that we're ignoring
				// the element
				if ((null == value || 0 == value.Length) && !admin && !secure && !hidden)
				{
					this.core.OnMessage(WixWarnings.PropertyUseless(sourceLineNumbers, WarningLevel.Moderate, id));
				}
				else // there is a value and/or a flag set, do that
				{
					this.AddProperty(sourceLineNumbers, id, value, admin, secure, hidden);
				}
			}
		}

		/// <summary>
		/// Parses a property reference element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		private void ParsePropertyRefElement(XmlNode node)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			this.core.AddValidReference("Property", id);
		}

		/// <summary>
		/// Parses a registry element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="componentId">Identifier for parent component.</param>
		/// <param name="root">Root specified when element is nested under another Registry element, otherwise CompilerCore.IntegerNotSet.</param>
		/// <param name="parentKey">Parent key for this Registry element when nested.</param>
		/// <param name="possibleKeyPath">Identifier of this registry key since it could be the component's keypath.</param>
		/// <returns>True if registry element is marked as component's keypath</returns>
		private bool ParseRegistryElement(XmlNode node, string componentId, int root, string parentKey, out string possibleKeyPath)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;
			string key = parentKey; // default to parent key path
			string name = null;
			string value = null;
			string type = null;
			string action = null;
			bool keyPath = false;
			bool couldBeKeyPath = true; // assume that this is a regular registry key that could become the key path

			possibleKeyPath = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Action":
						action = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						switch (action)
						{
							case CompilerCore.IllegalEmptyAttributeValue: // this error is already handled
								break;
							case "append":
							case "createKey":
							case "createKeyAndRemoveKeyOnUninstall":
							case "prepend":
							case "remove":
							case "removeKeyOnInstall":
							case "removeKeyOnUninstall":
							case "write":
								break;
							default:
								this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name, attrib.Name, action, "append", "createKey", "createKeyAndRemoveKeyOnUninstall", "prepend", "remove", "removeKeyOnInstall", "removeKeyOnUninstall", "write"));
								break;
						}
						break;
					case "Key":
						key = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						if (null != parentKey)
						{
							if (parentKey.EndsWith("\\"))
							{
								key = String.Concat(parentKey, key);
							}
							else
							{
								key = String.Concat(parentKey, "\\", key);
							}
						}
						break;
					case "KeyPath":
						keyPath = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
						break;
					case "Name":
						name = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Root":
						if (CompilerCore.IntegerNotSet != root)
						{
							this.core.OnMessage(WixErrors.RegistryRootInvalid(sourceLineNumbers));
						}

						string rootString = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						switch (rootString)
						{
							case CompilerCore.IllegalEmptyAttributeValue: // this error is already handled
								break;
							case "HKMU":
								root = -1;
								break;
							case "HKCR":
								root = MsiInterop.MsidbRegistryRootClassesRoot;
								break;
							case "HKCU":
								root = MsiInterop.MsidbRegistryRootCurrentUser;
								break;
							case "HKLM":
								root = MsiInterop.MsidbRegistryRootLocalMachine;
								break;
							case "HKU":
								root = MsiInterop.MsidbRegistryRootUsers;
								break;
							default:
								this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name, attrib.Name, rootString, "HKMU", "HKCR", "HKCU", "HKLM", "HKU"));
								break;
						}
						break;
					case "Type":
						type = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						switch (type)
						{
							case CompilerCore.IllegalEmptyAttributeValue: // this error is already handled
								break;
							case "binary":
							case "expandable":
							case "integer":
							case "multiString":
							case "string":
								break;
							default:
								this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name, attrib.Name, type, "binary", "expandable", "integer", "multiString", "string"));
								break;
						}
						break;
					case "Value":
						value = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (CompilerCore.IntegerNotSet == root)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Root"));
				root = CompilerCore.IllegalInteger;
			}
			if (null == key)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Key"));
				key = String.Empty; // set the key to something to prevent null reference exceptions
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					switch (child.LocalName)
					{
						case "Permission":
							// We need to handle this below the generation of the id, because the id is an
							// input into the Permission element.
							break;
						case "Registry":
							if ("remove" == action || "removeKeyOnInstall" == action)
							{
								this.core.OnMessage(WixErrors.RegistrySubElementCannotBeRemoved(sourceLineNumbers, node.Name, child.Name, "Action", "remove", "removeKeyOnInstall"));
							}

							string possibleChildKeyPath = null;
							bool childIsKeyPath = this.ParseRegistryElement(child, componentId, root, key, out possibleChildKeyPath);
							if (childIsKeyPath)
							{
								if (keyPath)
								{
									this.core.OnMessage(WixErrors.ComponentMultipleKeyPaths(sourceLineNumbers, child.Name, "KeyPath", "yes", "File", "Registry", "ODBCDataSource"));
								}

								possibleKeyPath = possibleChildKeyPath; // the child is the key path
								keyPath = true;
							}

							break;
						case "RegistryValue":
							if ("remove" == action || "removeKeyOnInstall" == action)
							{
								this.core.OnMessage(WixErrors.RegistrySubElementCannotBeRemoved(sourceLineNumbers, node.Name, child.Name, "Action", "remove", "removeKeyOnInstall"));
							}
							if ("multiString" != type && null != value)
							{
								this.core.OnMessage(WixErrors.RegistryMultipleValuesWithoutMultiString(sourceLineNumbers, node.Name, "Value", child.Name, "Type", "multiString"));
							}
							else if (null == value)
							{
								value = child.InnerText;
							}
							else
							{
								value = String.Concat(value, "[~]", child.InnerText);
							}
							break;
						default:
							this.core.UnexpectedElement(node, child);
							break;
					}
				}
			}

			if ("remove" == action || "removeKeyOnInstall" == action) // RemoveRegistry table
			{
				if (keyPath)
				{
					this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name, "KeyPath", "Action", action));
				}
				if (null != value)
				{
					this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name, "Value", "Action", action));
				}
				if (null != type)
				{
					this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name, "Type", "Action", action));
				}
				if ("removeKeyOnInstall" == action)
				{
					if (null != name)
					{
						this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name, "Name", "Action", action));
					}

					name = "-";
				}

				// this cannot be a KeyPath
				couldBeKeyPath = false;

				// generate the identifier if it wasn't provided
				if (null == id)
				{
					id = GenerateIdentifier(node.LocalName, componentId, root.ToString(CultureInfo.InvariantCulture.NumberFormat), key.ToLower(CultureInfo.InvariantCulture), (null != name ? name.ToLower(CultureInfo.InvariantCulture) : name));
				}

				Row row = this.core.CreateRow(sourceLineNumbers, "RemoveRegistry");
				row[0] = id;
				row[1] = root;
				row[2] = key;
				row[3] = name;
				row[4] = componentId;

				this.EnsureTable(sourceLineNumbers, "Registry"); // RemoveRegistry table requires the Registry table
			}
			else // Registry table
			{
				if (("append" == action || "prepend" == action) && "multiString" != type)
				{
					this.core.OnMessage(WixErrors.IllegalAttributeValueWithoutOtherAttribute(sourceLineNumbers, node.Name, "Action", action, "Type", "multiString"));
				}

				if ("createKey" == action || "createKeyAndRemoveKeyOnUninstall" == action || "removeKeyOnUninstall" == action)
				{
					if (keyPath)
					{
						this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name, "KeyPath", "Action", action));
					}
					if (null != name)
					{
						this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name, "Name", "Action", action));
					}
					if (null != value)
					{
						this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name, "Value", "Action", action));
					}

					// this cannot be a KeyPath
					couldBeKeyPath = false;
				}

				if (null != value)
				{
					if ("removeKeyOnUninstall" == action || "writeKeyAndRemoveKeyOnUninstall" == action)
					{
						this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name, "Value", "Action", action));
					}
					if (null == type)
					{
						this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Type", "Value"));
					}

					switch (type)
					{
						case "binary":
							value = String.Concat("#x", value);
							break;
						case "expandable":
							value = String.Concat("#%", value);
							break;
						case "integer":
							value = String.Concat("#", value);
							break;
						case "multiString":
							switch (action)
							{
								case "append":
									value = String.Concat("[~]", value);
									break;
								case "prepend":
									value = String.Concat(value, "[~]");
									break;
								case "write":
								default:
									if (-1 == value.IndexOf("[~]"))
									{
										value = String.Format("[~]{0}[~]", value);
									}
									break;
							}
							break;
						case "string":
							// escape the leading '#' character for string registry keys
							if (value.StartsWith("#"))
							{
								value = String.Concat("#", value);
							}
							break;
					}
				}
				else // no value
				{
					if (null == name) // no name or value
					{
						if (null != type)
						{
							this.core.OnMessage(WixErrors.IllegalAttributeWithoutOtherAttributes(sourceLineNumbers, node.Name, "Type", "Name", "Value"));
						}

						switch (action)
						{
							case "createKey":
								name = "+";
								break;
							case "createKeyAndRemoveKeyOnUninstall":
								name = "*";
								break;
							case "removeKeyOnUninstall":
								name = "-";
								break;
						}
					}
					else // name specified, no value
					{
						if ("+" == name || "-" == name || "*" == name)
						{
							this.core.OnMessage(WixErrors.RegistryNameValueIncorrect(sourceLineNumbers, node.Name, "Name", name));

							if (keyPath)
							{
								this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name, "KeyPath", "Name", name));
							}
						}
					}

					if ("multiString" == type)
					{
						value = "[~][~]";
					}
				}

				// generate the identifier if it wasn't provided
				if (null == id)
				{
					id = GenerateIdentifier(node.LocalName, componentId, root.ToString(CultureInfo.InvariantCulture.NumberFormat), key.ToLower(CultureInfo.InvariantCulture), (null != name ? name.ToLower(CultureInfo.InvariantCulture) : name));
				}

				Row row = this.core.CreateRow(sourceLineNumbers, "Registry");
				row[0] = id;
				row[1] = root;
				row[2] = key;
				row[3] = name;
				row[4] = value;
				row[5] = componentId;
			}

			// This looks different from all the others, because the id must be generated before
			// we get here.
			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					switch (child.LocalName)
					{
						case "Permission":
							if ("remove" == action || "removeKeyOnInstall" == action)
							{
								this.core.OnMessage(WixErrors.RegistrySubElementCannotBeRemoved(sourceLineNumbers, node.Name, child.Name, "Action", "remove", "removeKeyOnInstall"));
							}
							this.ParsePermissionElement(child, id, node.LocalName);
							break;
					}
				}
			}

			// If this was just a regular registry key (that could be the key path)
			// and no child registry key set the possible key path, let's make this
			// Registry/@Id a possible key path.
			if (couldBeKeyPath && null == possibleKeyPath)
			{
				possibleKeyPath = id;
			}

			return keyPath;
		}

		/// <summary>
		/// Parses a remove file element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="componentId">Identifier of parent component.</param>
		/// <param name="parentDirectory">Identifier of the parent component's directory.</param>
		private void ParseRemoveFileElement(XmlNode node, string componentId, string parentDirectory)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;
			string directory = null;
			string longName = null;
			string name = null;
			int on = CompilerCore.IntegerNotSet;
			string property = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						break;
					case "Directory":
						directory = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						this.core.AddValidReference("Directory", directory);
						break;
					case "LongName":
						longName = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Name":
						name = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "On":
						string onValue = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						switch (onValue)
						{
							case CompilerCore.IllegalEmptyAttributeValue:
								on = CompilerCore.IllegalInteger;
								break;
							case "install":
								on = 1;
								break;
							case "uninstall":
								on = 2;
								break;
							case "both":
								on = 3;
								break;
							default:
								this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name, "On", onValue, "install", "uninstall", "both"));
								on = CompilerCore.IllegalInteger;
								break;
						}
						break;
					case "Property":
						property = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}
			if (null != longName && null == name)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Name", "LongName"));
			}
			if (CompilerCore.IntegerNotSet == on)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "On"));
				on = CompilerCore.IllegalInteger;
			}
			if (null != directory && null != property)
			{
				this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name, "Property", "Directory", directory));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			Row row = this.core.CreateRow(sourceLineNumbers, "RemoveFile");
			row[0] = id;
			row[1] = componentId;
			if (null != longName)
			{
				row[2] = String.Concat(name, "|", longName);
			}
			else // just use the name, even if the name is still null (that means we'll remove the directory)
			{
				row[2] = name;
			}
			if (null != directory)
			{
				row[3] = directory;
			}
			else if (null != property)
			{
				row[3] = property;
			}
			else
			{
				row[3] = parentDirectory;
			}
			row[4] = on;
		}

		/// <summary>
		/// Parses a RemoveFolder element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="componentId">Identifier of parent component.</param>
		/// <param name="parentDirectory">Identifier of parent component's directory.</param>
		private void ParseRemoveFolderElement(XmlNode node, string componentId, string parentDirectory)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;
			string directory = null;
			int on = CompilerCore.IntegerNotSet;
			string property = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						break;
					case "Directory":
						directory = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						this.core.AddValidReference("Directory", directory);
						break;
					case "On":
						string onValue = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						switch (onValue)
						{
							case CompilerCore.IllegalEmptyAttributeValue:
								on = CompilerCore.IllegalInteger;
								break;
							case "install":
								on = 1;
								break;
							case "uninstall":
								on = 2;
								break;
							case "both":
								on = 3;
								break;
							default:
								this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name, "On", onValue, "install", "uninstall", "both"));
								on = CompilerCore.IllegalInteger;
								break;
						}
						break;
					case "Property":
						property = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}
			if (CompilerCore.IntegerNotSet == on)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "On"));
				on = CompilerCore.IllegalInteger;
			}
			if (null != directory && null != property)
			{
				this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name, "Property", "Directory", directory));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			Row row = this.core.CreateRow(sourceLineNumbers, "RemoveFile");
			row[0] = id;
			row[1] = componentId;
			row[2] = null;
			if (null != directory)
			{
				row[3] = directory;
			}
			else if (null != property)
			{
				row[3] = property;
			}
			else
			{
				row[3] = parentDirectory;
			}
			row[4] = on;
		}

		/// <summary>
		/// Parses a reserve cost element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="componentId">Identifier of parent component.</param>
		/// <param name="directoryId">Optional and default identifier of referenced directory.</param>
		private void ParseReserveCostElement(XmlNode node, string componentId, string directoryId)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;
			int runLocal = CompilerCore.IntegerNotSet;
			int runFromSource = CompilerCore.IntegerNotSet;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Directory":
						directoryId = this.core.GetAttributeValue(sourceLineNumbers, attrib, true);
						break;
					case "RunLocal":
						runLocal = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						break;
					case "RunFromSource":
						runFromSource = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			Row row = this.core.CreateRow(sourceLineNumbers, "ReserveCost");
			row[0] = id;
			row[1] = componentId;
			row[2] = directoryId;
			if (CompilerCore.IntegerNotSet != runLocal)
			{
				row[3] = runLocal;
			}
			if (CompilerCore.IntegerNotSet != runFromSource)
			{
				row[4] = runFromSource;
			}
		}

		/// <summary>
		/// Parses a sequence element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="sequenceTable">Name of sequence table.</param>
		private void ParseSequenceElement(XmlNode node, string sequenceTable)
		{
			// Parse each action in the sequence.
			foreach (XmlNode child in node.ChildNodes)
			{
				if (!(child is XmlElement))   // only process elements here
				{
					continue;
				}

				SourceLineNumberCollection childSourceLineNumbers = this.core.GetSourceLineNumbers(child);
				string actionName = child.LocalName;
				string afterAction = null;
				string beforeAction = null;
				string condition = null;
				bool customAction = "Custom" == actionName;
				int sequence = CompilerCore.IntegerNotSet;
				bool showDialog = "Show" == actionName;
				bool specialAction = "InstallExecute" == actionName || "InstallExecuteAgain" == actionName || "RemoveExistingProducts" == actionName || "DisableRollback" == actionName || "ScheduleReboot" == actionName || "ForceReboot" == actionName || "ResolveSource" == actionName;
				bool specialStandardAction = "AppSearch" == actionName || "CCPSearch" == actionName || "RMCCPSearch" == actionName || "LaunchConditions" == actionName;
				bool suppress = false;

				foreach (XmlAttribute attrib in child.Attributes)
				{
					switch (attrib.LocalName)
					{
						case "Action":
							if (!customAction)
							{
								this.core.UnexpectedAttribute(childSourceLineNumbers, attrib);
							}
							actionName = this.core.GetAttributeIdentifierValue(childSourceLineNumbers, attrib);
							break;
						case "After":
							if (!customAction && !showDialog && !specialAction && !specialStandardAction)
							{
								this.core.UnexpectedAttribute(childSourceLineNumbers, attrib); // only valid for Custom actions and Show dialogs
							}
							afterAction = this.core.GetAttributeIdentifierValue(childSourceLineNumbers, attrib);
							break;
						case "Before":
							if (!customAction && !showDialog && !specialAction && !specialStandardAction)
							{
								this.core.UnexpectedAttribute(childSourceLineNumbers, attrib); // only valid for Custom actions and Show dialogs
							}
							beforeAction = this.core.GetAttributeIdentifierValue(childSourceLineNumbers, attrib);
							break;
						case "Dialog":
							if (!showDialog)
							{
								this.core.UnexpectedAttribute(childSourceLineNumbers, attrib);
							}
							actionName = this.core.GetAttributeIdentifierValue(childSourceLineNumbers, attrib);
							break;
						case "OnExit":
							if (!customAction && !showDialog && !specialAction)
							{
								this.core.UnexpectedAttribute(childSourceLineNumbers, attrib);
							}
							else if (CompilerCore.IntegerNotSet != sequence)
							{
								this.core.OnMessage(WixErrors.CannotSpecifySequenceAndOnExit(childSourceLineNumbers, child.Name));
							}

							string onExitValue = this.core.GetAttributeValue(childSourceLineNumbers, attrib);
							switch (onExitValue)
							{
								case CompilerCore.IllegalEmptyAttributeValue:
									break;
								case "success":
									sequence = -1;
									break;
								case "cancel":
									sequence = -2;
									break;
								case "error":
									sequence = -3;
									break;
								case "suspend":
									sequence = -4;
									break;
								default:
									this.core.OnMessage(WixErrors.IllegalAttributeValue(childSourceLineNumbers, child.Name, "OnExit", onExitValue, "success", "cancel", "error", "suspend"));
									break;
							}
							break;
						case "Sequence":
							if (CompilerCore.IntegerNotSet != sequence)
							{
								this.core.OnMessage(WixErrors.CannotSpecifySequenceAndOnExit(childSourceLineNumbers, child.Name));
							}

							sequence = this.core.GetAttributeIntegerValue(childSourceLineNumbers, attrib);
							if (-4 <= sequence && 0 >= sequence)
							{
								this.core.OnMessage(WixErrors.IllegalAttributeValue(childSourceLineNumbers, child.Name, attrib.Name, sequence.ToString(CultureInfo.InvariantCulture.NumberFormat)));
							}
							break;
						case "Suppress":
							suppress = YesNoType.Yes == this.core.GetAttributeYesNoValue(childSourceLineNumbers, attrib);
							break;
						default:
							this.core.UnexpectedAttribute(childSourceLineNumbers, attrib);
							break;
					}
				}

				// Get the condition from the inner text of the element.
				condition = this.core.GetConditionInnerText(child);

				if (customAction && "Custom" == actionName)
				{
					this.core.OnMessage(WixErrors.ExpectedAttribute(childSourceLineNumbers, child.Name, "Action"));
				}
				else if (showDialog && "Show" == actionName)
				{
					this.core.OnMessage(WixErrors.ExpectedAttribute(childSourceLineNumbers, child.Name, "Dialog"));
				}

				if (CompilerCore.IntegerNotSet != sequence && (null != beforeAction || null != afterAction))
				{
					this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(childSourceLineNumbers, child.Name, "Sequence", "Before", "After"));
				}

				if (null != beforeAction && null != afterAction)
				{
					this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(childSourceLineNumbers, child.Name, "After", "Before"));
				}
				else if ((customAction || showDialog || specialAction) && !suppress && CompilerCore.IntegerNotSet == sequence && null == beforeAction && null == afterAction)
				{
					this.core.OnMessage(WixErrors.NeedSequenceBeforeOrAfter(childSourceLineNumbers, child.Name));
				}

				// action that is scheduled to occur before/after itself
				if (beforeAction == actionName)
				{
					this.core.OnMessage(WixErrors.ActionScheduledRelativeToItself(childSourceLineNumbers, child.Name, "Before", beforeAction));
				}
				else if (afterAction == actionName)
				{
					this.core.OnMessage(WixErrors.ActionScheduledRelativeToItself(childSourceLineNumbers, child.Name, "After", afterAction));
				}

				// suppress cannot be specified at the same time as Before, After, or Sequence
				if (suppress && (null != afterAction || null != beforeAction || CompilerCore.IntegerNotSet != sequence))
				{
					this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttributes(childSourceLineNumbers, child.Name, "Suppress", "Before", "After", "Sequence"));
				}

				foreach (XmlNode grandChild in child.ChildNodes)
				{
					if (XmlNodeType.Element == grandChild.NodeType)
					{
						this.core.UnexpectedElement(child, grandChild);
					}
				}

				// add the row and any references needed
				Row row = this.core.CreateRow(childSourceLineNumbers, "Actions");
				row[0] = sequenceTable;
				row[1] = actionName;
				row[2] = condition;
				if (CompilerCore.IntegerNotSet != sequence)
				{
					row[3] = sequence;
				}
				row[4] = beforeAction;
				row[5] = afterAction;
				row[6] = suppress ? 1 : 0;

				if (customAction)
				{
					this.core.AddValidReference("CustomAction", actionName);
				}
				else if (showDialog)
				{
					this.core.AddValidReference("Dialog", actionName);
				}
				if (null != beforeAction)
				{
					this.core.AddValidReference("Actions", String.Concat(sequenceTable, "/", beforeAction));
				}
				else if (null != afterAction)
				{
					this.core.AddValidReference("Actions", String.Concat(sequenceTable, "/", afterAction));
				}
			}
		}

		/// <summary>
		/// Parses a service control element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="componentId">Identifier of parent component.</param>
		private void ParseServiceControlElement(XmlNode node, string componentId)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string arguments = null;
			int events = 0; // default is to do nothing
			string id = null;
			string name = null;
			YesNoType wait = YesNoType.NotSet;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Name":
						name = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Remove":
						string removeValue = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						switch (this.core.GetAttributeValue(sourceLineNumbers, attrib))
						{
							case CompilerCore.IllegalEmptyAttributeValue:
								break;
							case "install":
								events |= MsiInterop.MsidbServiceControlEventDelete;
								break;
							case "uninstall":
								events |= MsiInterop.MsidbServiceControlEventUninstallDelete;
								break;
							case "both":
								events |= MsiInterop.MsidbServiceControlEventDelete | MsiInterop.MsidbServiceControlEventUninstallDelete;
								break;
							default:
								this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name, "Remove", removeValue, "install", "uninstall", "both"));
								break;
						}
						break;
					case "Start":
						string startValue = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						switch (startValue)
						{
							case CompilerCore.IllegalEmptyAttributeValue:
								break;
							case "install":
								events |= MsiInterop.MsidbServiceControlEventStart;
								break;
							case "uninstall":
								events |= MsiInterop.MsidbServiceControlEventUninstallStart;
								break;
							case "both":
								events |= MsiInterop.MsidbServiceControlEventStart | MsiInterop.MsidbServiceControlEventUninstallStart;
								break;
							default:
								this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name, "Start", startValue, "install", "uninstall", "both"));
								break;
						}
						break;
					case "Stop":
						string stopValue = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						switch (stopValue)
						{
							case CompilerCore.IllegalEmptyAttributeValue:
								break;
							case "install":
								events |= MsiInterop.MsidbServiceControlEventStop;
								break;
							case "uninstall":
								events |= MsiInterop.MsidbServiceControlEventUninstallStop;
								break;
							case "both":
								events |= MsiInterop.MsidbServiceControlEventStop | MsiInterop.MsidbServiceControlEventUninstallStop;
								break;
							default:
								this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name, "Stop", stopValue, "install", "uninstall", "both"));
								break;
						}
						break;
					case "Wait":
						wait = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}

			if (null == name)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Name"));
			}

			// get the ServiceControl arguments
			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					switch (child.LocalName)
					{
						case "ServiceArgument":
							if (null != arguments)
							{
								arguments = String.Concat(arguments, "[~]");
							}
							arguments = String.Concat(arguments, this.core.GetTrimmedInnerText(child));
							break;
						default:
							this.core.UnexpectedElement(node, child);
							break;
					}
				}
			}

			Row row = this.core.CreateRow(sourceLineNumbers, "ServiceControl");
			row[0] = id;
			row[1] = name;
			row[2] = events;
			row[3] = arguments;
			if (YesNoType.NotSet != wait)
			{
				row[4] = YesNoType.Yes == wait ? 1 : 0;
			}
			row[5] = componentId;
		}

		/// <summary>
		/// Parses a service dependency element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <returns>Parsed sevice dependency name.</returns>
		private string ParseServiceDependencyElement(XmlNode node)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string dependency = null;
			bool group = false;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						dependency = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Group":
						group = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == dependency)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			return group ? String.Concat("+", dependency) : dependency;
		}

		/// <summary>
		/// Parses a service configuration element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="componentId">Identifier of parent component.</param>
		/// <param name="parentTableName">Name of parent element.</param>
		/// <param name="parentTableServiceName">Optional name of service </param>
		private void ParseServiceConfigElement(XmlNode node, string componentId, string parentTableName, string parentTableServiceName)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string firstFailureActionType = null;
			string newService = null;
			string programCommandLine = null;
			string rebootMessage = null;
			string resetPeriod = null;
			string restartServiceDelay = null;
			string secondFailureActionType = null;
			string serviceName = null;
			string thirdFailureActionType = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "FirstFailureActionType":
						firstFailureActionType = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "ProgramCommandLine":
						programCommandLine = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "RebootMessage":
						rebootMessage = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "ResetPeriodInDays":
						resetPeriod = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "RestartServiceDelayInSeconds":
						restartServiceDelay = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "SecondFailureActionType":
						secondFailureActionType = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "ServiceName":
						serviceName = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "ThirdFailureActionType":
						thirdFailureActionType = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			// if this element is a child of ServiceInstall then ignore the service name provided.
			if ("ServiceInstall" == parentTableName)
			{
				serviceName = parentTableServiceName;
				newService = "1";
			}
			else
			{
				// not a child of ServiceInstall, so ServiceName must have been provided
				if (null == serviceName)
				{
					this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "ServiceName"));
				}

				newService = "0";
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			// Reference SchedServiceConfig since nothing will happen without it
			this.core.AddValidReference("CustomAction", "SchedServiceConfig");

			Row row = this.core.CreateRow(sourceLineNumbers, "ServiceConfig");
			row[0] = serviceName;
			row[1] = componentId;
			row[2] = newService;
			row[3] = firstFailureActionType;
			row[4] = secondFailureActionType;
			row[5] = thirdFailureActionType;
			row[6] = resetPeriod;
			row[7] = restartServiceDelay;
			row[8] = programCommandLine;
			row[9] = rebootMessage;
		}

		/// <summary>
		/// Parses a service install element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="componentId">Identifier of parent component.</param>
		private void ParseServiceInstallElement(XmlNode node, string componentId)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;
			string account = null;
			string arguments = null;
			string dependencies = null;
			string description = null;
			string displayName = null;
			bool eraseDescription = false;
			int errorbits = 0;
			string loadOrderGroup = null;
			string localGroup = null;
			string name = null;
			string password = null;
			int startType = 0;
			int typebits = 0;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Account":
						account = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Arguments":
						arguments = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Description":
						description = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "DisplayName":
						displayName = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "EraseDescription":
						eraseDescription = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
						break;
					case "ErrorControl":
						string errorControlValue = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						switch (errorControlValue)
						{
							case CompilerCore.IllegalEmptyAttributeValue:
								break;
							case "ignore":
								errorbits |= MsiInterop.MsidbServiceInstallErrorIgnore;
								break;
							case "normal":
								errorbits |= MsiInterop.MsidbServiceInstallErrorNormal;
								break;
							case "critical":
								errorbits |= MsiInterop.MsidbServiceInstallErrorCritical;
								break;
							default:
								this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name, attrib.Name, errorControlValue, "ignore", "normal", "critical"));
								break;
						}
						break;
					case "Interactive":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							typebits |= MsiInterop.MsidbServiceInstallInteractive;
						}
						break;
					case "LoadOrderGroup":
						loadOrderGroup = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Name":
						name = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Password":
						password = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Start":
						string startValue = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						switch (startValue)
						{
							case CompilerCore.IllegalEmptyAttributeValue:
								break;
							case "auto":
								startType = MsiInterop.MsidbServiceInstallAutoStart;
								break;
							case "demand":
								startType = MsiInterop.MsidbServiceInstallDemandStart;
								break;
							case "disabled":
								startType = MsiInterop.MsidbServiceInstallDisabled;
								break;
							case "boot":
							case "system":
								this.core.OnMessage(WixErrors.ValueNotSupported(sourceLineNumbers, node.Name, attrib.Name, startValue));
								break;
							default:
								this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name, attrib.Name, startValue, "auto", "demand", "disabled"));
								break;
						}
						break;
					case "Type":
						string typeValue = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						switch (typeValue)
						{
							case CompilerCore.IllegalEmptyAttributeValue:
								break;
							case "ownProcess":
								typebits |= MsiInterop.MsidbServiceInstallOwnProcess;
								break;
							case "shareProcess":
								typebits |= MsiInterop.MsidbServiceInstallShareProcess;
								break;
							case "kernelDriver":
							case "systemDriver":
								this.core.OnMessage(WixErrors.ValueNotSupported(sourceLineNumbers, node.Name, attrib.Name, typeValue));
								break;
							default:
								this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name, node.Name, typeValue, "ownProcess", "shareProcess"));
								break;
						}
						break;
					case "Vital":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							errorbits |= MsiInterop.MsidbServiceInstallErrorControlVital;
						}
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}

			if (eraseDescription)
			{
				description = "[~]";
			}

			// get the ServiceInstall dependencies and config
			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					switch (child.LocalName)
					{
						case "Permission":
							this.ParsePermissionElement(child, id, "ServiceInstall");
							break;
						case "ServiceConfig":
							this.ParseServiceConfigElement(child, componentId, "ServiceInstall", name);
							break;
						case "ServiceDependency":
							dependencies = String.Concat(dependencies, this.ParseServiceDependencyElement(child), "[~]");
							break;
						default:
							this.core.UnexpectedElement(node, child);
							break;
					}
				}
			}

			if (null != dependencies)
			{
				dependencies = String.Concat(dependencies, "[~]");
			}

			Row row = this.core.CreateRow(sourceLineNumbers, "ServiceInstall");
			row[0] = id;
			row[1] = name;
			row[2] = displayName;
			row[3] = typebits;
			row[4] = startType;
			row[5] = errorbits;
			if (null == loadOrderGroup)
			{
				row[6] = localGroup;
			}
			else
			{
				row[6] = loadOrderGroup;
			}
			row[7] = dependencies;
			row[8] = account;
			row[9] = password;
			row[10] = arguments;
			row[11] = componentId;
			row[12] = description;
		}

		/// <summary>
		/// Parses a SFP catalog element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="parentSFPCatalog">Parent SFPCatalog.</param>
		private void ParseSFPFileElement(XmlNode node, string parentSFPCatalog)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			Row row = this.core.CreateRow(sourceLineNumbers, "FileSFPCatalog");
			row[0] = id;
			row[1] = parentSFPCatalog;
		}

		/// <summary>
		/// Parses a SFP catalog element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="parentSFPCatalog">Parent SFPCatalog.</param>
		private void ParseSFPCatalogElement(XmlNode node, ref string parentSFPCatalog)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string parentName = null;
			string dependency = null;
			string name = null;
			string sourceFile = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Dependency":
						dependency = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Name":
						name = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						parentSFPCatalog = name;
						break;
					case "SourceFile":
						sourceFile = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						this.core.OnMessage(WixErrors.BinaryColumnTypesNotYetImplementedInWiXToolSet(sourceLineNumbers, node.Name, "src", "SFPCatalog", "Catalog"));
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == name)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Name"));
			}

			if (null == sourceFile)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "SourceFile"));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					switch (child.LocalName)
					{
						case "SFPCatalog":
							this.ParseSFPCatalogElement(child, ref parentName);
							if (null != dependency && parentName == dependency)
							{
								this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Dependency"));
							}
							dependency = parentName;
							break;
						case "SFPFile":
							this.ParseSFPFileElement(child, name);
							break;
						default:
							this.core.UnexpectedElement(node, child);
							break;
					}
				}
			}

			if (null == dependency)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Dependency"));
			}

			Row row = this.core.CreateRow(sourceLineNumbers, "SFPCatalog");
			row[0] = name;
			row[1] = sourceFile;
			row[2] = dependency;
		}

		// stuff that still needs to be converted from WiX v1
		//    Sub ProcessSFPCatalogElement(node, sDependency)
		//        Dim op, row, attribute, child, sName, sSrc
		//
		//        Set row = CreateRecord(UBound(SFPCatalogTable))
		//        For Each attribute In node.Attributes
		//            Select Case(attribute.name)
		//                Case "op"    : op    = attribute.value
		//                Case "Name" : sName = attribute.value
		//                Case "Dependency" : If IsEmpty(sDependency) Then sDependency = attribute.value Else Fail 0, 3022, "child SFPCatalog cannot have dependency outside of parent"
		//                Case "src" : sSrc = BaseDir(attribute.value)
		//                Case Else : Unexpected attribute, node
		//            End Select
		//        Next
		//        If IsEmpty(sName) Then Fail 0, 3023, "Must specify a SFPCatalog.Name"
		//        If ElementHasText(node) Then Fail 0, 0, "robmen - SFP Catalogs stored as hex in XML not currently supported, use src attribute instead"
		//        If Not fso.FileExists(sSrc) Then Fail 0, 3024, "Missing sfp catalog file: " & sSrc
		//
		//        If fVerbose Then WScript.Echo "SFPCatalog reading catalog from src: " & sSrc
		//        row.StringData(SFPCatalog_SFPCatalog) = sName
		//        row.SetStream SFPCatalog_Catalog, sSrc  : CheckError 0
		//        row.StringData(SFPCatalog_Dependency) = sDependency
		//        DoAction SFPCatalogTable, op, row
		//
		//        For Each child In node.childNodes
		//            Select Case (GetElementName(child))
		//                Case Empty
		//                Case "SFPCatalog" : ProcessSFPCatalogElement child, sName
		//                Case "File" : ProcessFileSFPCatalogElement child, sName
		//                Case Else : Unexpected child, node
		//            End Select
		//        Next
		//    End Sub
		//
		//
		//    Sub ProcessFileSFPCatalogElement(node, sSFPCatalog)
		//        Dim op, row, attribute
		//
		//        Set row = CreateRecord(UBound(FileSFPCatalogTable))
		//        For Each attribute In node.Attributes
		//            Select Case(attribute.name)
		//                Case "op" : op = attribute.value
		//                Case Else : Unexpected attribute, node
		//            End Select
		//        Next
		//
		//        row.StringData(FileSFPCatalog_File_) = ElementText(node)
		//        row.StringData(FileSFPCatalog_SFPCatalog_) = sSFPCatalog
		//        DoAction FileSFPCatalogTable, op, row
		//    End Sub

		/// <summary>
		/// Parses a shortcut element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="componentId">Identifer for parent component.</param>
		/// <param name="targetType">Target type of shortcut (directory or file)</param>
		/// <param name="targetId">Default identifier of target.</param>
		private void ParseShortcutElement(XmlNode node, string componentId, ShortcutType targetType, string targetId)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;
			bool advertise = false;
			string arguments = null;
			string description = null;
			string descriptionResourceDll = null;
			int descriptionResourceId = CompilerCore.IntegerNotSet;
			string directory = null;
			string displayResourceDll = null;
			int displayResourceId = CompilerCore.IntegerNotSet;
			int hotkey = CompilerCore.IntegerNotSet;
			string icon = null;
			int iconIndex = CompilerCore.IntegerNotSet;
			string longName = null;
			string name = null;
			int show = CompilerCore.IntegerNotSet;
			string target = null;
			string workingDirectory = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						break;
					case "Advertise":
						advertise = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
						break;
					case "Arguments":
						arguments = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Description":
						description = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "DescriptionResourceDll":
						descriptionResourceDll = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "DescriptionResourceId":
						descriptionResourceId = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						break;
					case "Directory":
						directory = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						this.core.AddValidReference("Directory", directory);
						break;
					case "DisplayResourceDll":
						displayResourceDll = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "DisplayResourceId":
						displayResourceId = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						break;
					case "Hotkey":
						hotkey = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						break;
					case "Icon":
						icon = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						this.core.AddValidReference("Icon", icon);
						break;
					case "IconIndex":
						iconIndex = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						break;
					case "LongName":
						longName = this.core.GetAttributeLongFilename(sourceLineNumbers, attrib);
						break;
					case "Name":
						name = this.core.GetAttributeShortFilename(sourceLineNumbers, attrib);
						break;
					case "Show":
						string showValue = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						switch (showValue)
						{
							case CompilerCore.IllegalEmptyAttributeValue:
								show = CompilerCore.IllegalInteger;
								break;
							case "normal":
								show = 1;
								break;
							case "maximized":
								show = 3;
								break;
							case "minimized":
								show = 7;
								break;
							default:
								this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name, "Show", showValue, "normal", "maximized", "minimized"));
								show = CompilerCore.IllegalInteger;
								break;
						}
						break;
					case "Target":
						target = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "WorkingDirectory":
						workingDirectory = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						break;
				}
			}

			if (advertise && null != target)
			{
				this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name, "Target", "Advertise", "yes"));
			}

			if (null != descriptionResourceDll)
			{
				if (CompilerCore.IntegerNotSet == descriptionResourceId)
				{
					this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "DescriptionResourceDll", "DescriptionResourceId"));
				}
			}
			else
			{
				if (CompilerCore.IntegerNotSet != descriptionResourceId)
				{
					this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "DescriptionResourceId", "DescriptionResourceDll"));
				}
			}

			if (null != displayResourceDll)
			{
				if (CompilerCore.IntegerNotSet == displayResourceId)
				{
					this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "DisplayResourceDll", "DisplayResourceId"));
				}
			}
			else
			{
				if (CompilerCore.IntegerNotSet != displayResourceId)
				{
					this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "DisplayResourceId", "DisplayResourceDll"));
				}
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					switch (child.LocalName)
					{
						case "Icon":
							this.ParseBinaryOrIconElement(child, BinaryType.Icon);
							break;
					}
				}
			}

			Row row = this.core.CreateRow(sourceLineNumbers, "Shortcut");
			row[0] = id;
			row[1] = directory;
			row[2] = GetMsiFilenameValue(name, longName);
			row[3] = componentId;
			if (advertise)
			{
				row[4] = Guid.Empty.ToString("B");
				this.core.AddFeatureBacklink(new FeatureBacklink(componentId, FeatureBacklinkType.Shortcut, row.Symbol));
			}
			else if (null != target)
			{
				row[4] = target;
			}
			else if (ShortcutType.Directory == targetType)
			{
				row[4] = String.Format(CultureInfo.InvariantCulture, "[{0}]", targetId);
			}
			else if (ShortcutType.File == targetType)
			{
				row[4] = String.Format(CultureInfo.InvariantCulture, "[#{0}]", targetId);
			}
			row[5] = arguments;
			row[6] = description;
			if (CompilerCore.IntegerNotSet != hotkey)
			{
				row[7] = hotkey;
			}
			row[8] = icon;
			if (CompilerCore.IntegerNotSet != iconIndex)
			{
				row[9] = iconIndex;
			}
			if (CompilerCore.IntegerNotSet != show)
			{
				row[10] = show;
			}
			row[11] = workingDirectory;
			row[12] = displayResourceDll;
			if (CompilerCore.IntegerNotSet != displayResourceId)
			{
				row[13] = displayResourceId;
			}
			row[14] = descriptionResourceDll;
			if (CompilerCore.IntegerNotSet != descriptionResourceId)
			{
				row[15] = descriptionResourceId;
			}
		}

		/// <summary>
		/// Parses a typelib element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="componentId">Identifier of parent component.</param>
		/// <param name="fileServer">Identifier of file that acts as typelib server.</param>
		/// <param name="win64Component">true if the component is 64-bit.</param>
		private void ParseTypeLibElement(XmlNode node, string componentId, string fileServer, bool win64Component)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;
			YesNoType advertise = YesNoType.NotSet;
			int cost = CompilerCore.IntegerNotSet;
			string description = null;
			int flags = 0;
			string helpDirectory = null;
			int language = CompilerCore.IntegerNotSet;
			int majorVersion = CompilerCore.IntegerNotSet;
			int minorVersion = CompilerCore.IntegerNotSet;
			int resourceId = CompilerCore.IntegerNotSet;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = String.Concat("{", this.core.GetAttributeGuidValue(sourceLineNumbers, attrib, false), "}");
						break;
					case "Advertise":
						advertise = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
						break;
					case "Control":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							flags |= 2;
						}
						break;
					case "Cost":
						cost = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						break;
					case "Description":
						description = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "HasDiskImage":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							flags |= 8;
						}
						break;
					case "HelpDirectory":
						helpDirectory = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						break;
					case "Hidden":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							flags |= 4;
						}
						break;
					case "Language":
						language = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						break;
					case "MajorVersion":
						majorVersion = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						break;
					case "MinorVersion":
						minorVersion = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						break;
					case "ResourceId":
						resourceId = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						break;
					case "Restricted":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							flags |= 1;
						}
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}
			if (null == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}
			if (CompilerCore.IntegerNotSet != cost && CompilerCore.IllegalInteger != cost && 0 > cost)
			{
				this.core.OnMessage(WixErrors.CostOutOfRange(sourceLineNumbers, node.Name, "Cost", cost));
			}
			if (CompilerCore.IntegerNotSet == language)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Language"));
				language = CompilerCore.IllegalInteger;
			}
			if (CompilerCore.IntegerNotSet != majorVersion && CompilerCore.IllegalInteger != majorVersion && (0 > majorVersion || 65535 < majorVersion))
			{
				this.core.OnMessage(WixErrors.IllegalTypeLibMajorVersion(sourceLineNumbers, node.Name, "MajorVersion", majorVersion));
			}
			if (CompilerCore.IntegerNotSet != minorVersion && CompilerCore.IllegalInteger != minorVersion && (0 > minorVersion || 255 < minorVersion))
			{
				this.core.OnMessage(WixErrors.IllegalTypeLibMinorVersion(sourceLineNumbers, node.Name, "MinorVersion", minorVersion));
			}

			// build up the typelib version string for the registry if the major or minor version was specified
			string registryVersion = null;
			if (CompilerCore.IntegerNotSet != majorVersion || CompilerCore.IntegerNotSet != minorVersion)
			{
				if (CompilerCore.IntegerNotSet != majorVersion)
				{
					registryVersion = majorVersion.ToString("x", CultureInfo.InvariantCulture.NumberFormat);
				}
				else
				{
					registryVersion = "0";
				}
				if (CompilerCore.IntegerNotSet != minorVersion)
				{
					registryVersion = String.Concat(registryVersion, ".", minorVersion.ToString("x", CultureInfo.InvariantCulture.NumberFormat));
				}
				else
				{
					registryVersion = String.Concat(registryVersion, ".0");
				}
			}

			// if the advertise state has not been set, default to advertised
			if (YesNoType.NotSet == advertise)
			{
				advertise = YesNoType.Yes;
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					switch (child.LocalName)
					{
						case "AppId":
							this.ParseAppIdElement(child, componentId, YesNoType.NotSet, fileServer, id, registryVersion);
							break;
						case "Class":
							this.ParseClassElement(child, componentId, YesNoType.NotSet, fileServer, id, registryVersion, null);
							break;
						case "Interface":
							this.ParseInterfaceElement(child, componentId, null, null, id, registryVersion);
							break;
						default:
							this.core.UnexpectedElement(node, child);
							break;
					}
				}
			}

			if (YesNoType.Yes == advertise)
			{
				if (CompilerCore.IntegerNotSet != resourceId)
				{
					this.core.OnMessage(WixErrors.IllegalAttributeWhenAdvertised(sourceLineNumbers, node.Name, "ResourceId"));
				}

				if (0 != flags)
				{
					if (0 < (1 & flags))
					{
						this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name, "Restricted", "Advertise", "yes"));
					}
					if (0 < (2 & flags))
					{
						this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name, "Control", "Advertise", "yes"));
					}
					if (0 < (4 & flags))
					{
						this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name, "Hidden", "Advertise", "yes"));
					}
					if (0 < (8 & flags))
					{
						this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name, "HasDiskImage", "Advertise", "yes"));
					}
				}

				Row row = this.core.CreateRow(sourceLineNumbers, "TypeLib");
				row[0] = id;
				row[1] = language;
				row[2] = componentId;
				if (CompilerCore.IntegerNotSet != majorVersion || CompilerCore.IntegerNotSet != minorVersion)
				{
					row[3] = (CompilerCore.IntegerNotSet != majorVersion ? majorVersion * 256 : 0) + (CompilerCore.IntegerNotSet != minorVersion ? minorVersion : 0);
				}
				row[4] = description;
				row[5] = helpDirectory;
				row[6] = Guid.Empty.ToString("B");
				if (CompilerCore.IntegerNotSet != cost)
				{
					row[7] = cost;
				}

				// protect against an erroneous component identifier
				if (null != componentId && CompilerCore.IllegalEmptyAttributeValue != componentId)
				{
					this.core.AddFeatureBacklink(new FeatureBacklink(componentId, FeatureBacklinkType.TypeLib, row.Symbol));
				}
			}
			else if (YesNoType.No == advertise)
			{
				if (CompilerCore.IntegerNotSet != cost && CompilerCore.IllegalInteger != cost)
				{
					this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name, "Cost", "Advertise", "no"));
				}
				if (null == fileServer)
				{
					this.core.OnMessage(WixErrors.MissingTypeLibFile(sourceLineNumbers, node.Name, "File"));
				}
				if (null == registryVersion)
				{
					this.core.OnMessage(WixErrors.ExpectedAttributes(sourceLineNumbers, node.Name, "MajorVersion", "MinorVersion", "Advertise", "no"));
				}

				// HKCR\TypeLib\[ID]\[MajorVersion].[MinorVersion], (Default) = [Description]
				this.AddRegistryKey(sourceLineNumbers, null, MsiInterop.MsidbRegistryRootClassesRoot, String.Format(CultureInfo.InvariantCulture, @"TypeLib\{0}\{1}", id, registryVersion), null, description, componentId);

				// HKCR\TypeLib\[ID]\[MajorVersion].[MinorVersion]\[Language]\[win16|win32|win64], (Default) = [TypeLibPath]\[ResourceId]
				string path = String.Concat("[!", fileServer, "]");
				if (resourceId != CompilerCore.IntegerNotSet)
				{
					path = String.Concat(path, Path.DirectorySeparatorChar, resourceId.ToString(CultureInfo.InvariantCulture.NumberFormat));
				}
				this.AddRegistryKey(sourceLineNumbers, null, MsiInterop.MsidbRegistryRootClassesRoot, String.Format(CultureInfo.InvariantCulture, @"TypeLib\{0}\{1}\{2}\{3}", id, registryVersion, language, (win64Component ? "win64" : "win32")), null, path, componentId);

				// HKCR\TypeLib\[ID]\[MajorVersion].[MinorVersion]\FLAGS, (Default) = [TypeLibFlags]
				this.AddRegistryKey(sourceLineNumbers, null, MsiInterop.MsidbRegistryRootClassesRoot, String.Format(CultureInfo.InvariantCulture, @"TypeLib\{0}\{1}\FLAGS", id, registryVersion), null, flags.ToString(CultureInfo.InvariantCulture.NumberFormat), componentId);

				if (null != helpDirectory)
				{
					// HKCR\TypeLib\[ID]\[MajorVersion].[MinorVersion]\HELPDIR, (Default) = [HelpDirectory]
					this.AddRegistryKey(sourceLineNumbers, null, MsiInterop.MsidbRegistryRootClassesRoot, String.Format(CultureInfo.InvariantCulture, @"TypeLib\{0}\{1}\HELPDIR", id, registryVersion), null, String.Concat("[", helpDirectory, "]"), componentId);
				}
			}

			// add a reference to the help directory if it was specified
			this.core.AddValidReference("Directory", helpDirectory);
		}

		/// <summary>
		/// Parse the UIRef element.
		/// </summary>
		/// <param name="node">The element to parse.</param>
		private void ParseUIRefElement(XmlNode node)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			this.core.AddValidReference("UI", id);
		}

		/// <summary>
		/// Parses UI elements.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		private void ParseUIElement(XmlNode node)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					switch (child.LocalName)
					{
						case "BillboardAction":
							this.ParseBillboardActionElement(child);
							break;
						case "ComboBox":
							this.ParseControlGroupElement(child, this.tableDefinitions["ComboBox"], "ListItem");
							break;
						case "Dialog":
							this.ParseDialogElement(child);
							break;
						case "DialogRef":
							this.ParseDialogRefElement(child);
							break;
						case "Error":
							this.ParseErrorElement(child);
							break;
						case "ListBox":
							this.ParseControlGroupElement(child, this.tableDefinitions["ListBox"], "ListItem");
							break;
						case "ListView":
							this.ParseControlGroupElement(child, this.tableDefinitions["ListView"], "ListItem");
							break;
						case "ProgressText":
							this.ParseActionTextElement(child);
							break;
						case "RadioButtonGroup":
							RadioButtonType radioButtonType = this.ParseRadioButtonGroupElement(child, null, RadioButtonType.NotSet);
							if (RadioButtonType.Bitmap == radioButtonType || RadioButtonType.Icon == radioButtonType)
							{
								SourceLineNumberCollection childSourceLineNumbers = this.core.GetSourceLineNumbers(child);
								this.core.OnMessage(WixErrors.RadioButtonBitmapAndIconDisallowed(childSourceLineNumbers));
							}
							break;
						case "TextStyle":
							this.ParseTextStyleElement(child);
							break;
						case "UIText":
							this.ParseUITextElement(child);
							break;

						// the following are available indentically under the UI and Programs elements for document organization use only
						case "AdminUISequence":
						case "InstallUISequence":
							this.ParseSequenceElement(child, child.LocalName);
							break;
						case "Binary":
							this.ParseBinaryOrIconElement(child, BinaryType.Binary);
							break;
						case "Property":
							this.ParsePropertyElement(child);
							break;

						default:
							this.core.UnexpectedElement(node, child);
							break;
					}
				}
			}

			if (null != id)
			{
				Row row = this.core.CreateRow(sourceLineNumbers, "UI");
				row[0] = id;
			}
		}

		/// <summary>
		/// Parses a list item element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="table">Table to add row to.</param>
		/// <param name="property">Identifier of property referred to by list item.</param>
		/// <param name="order">Relative order of list items.</param>
		private void ParseListItemElement(XmlNode node, TableDefinition table, string property, ref int order)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string icon = null;
			string text = null;
			string value = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Icon":
						if ("ListView" == table.Name)
						{
							icon = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
							this.core.AddValidReference("Binary", icon);
						}
						else
						{
							this.core.OnMessage(WixErrors.IllegalAttributeExceptOnElement(sourceLineNumbers, node.Name, attrib.Name, "ListView"));
						}
						break;
					case "Text":
						text = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Value":
						value = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == value)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Value"));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			Row row = this.core.CreateRow(sourceLineNumbers, table.Name);
			row[0] = property;
			row[1] = ++order;
			row[2] = value;
			row[3] = text;
			if (null != icon)
			{
				row[4] = icon;
			}
		}

		/// <summary>
		/// Parses a radio button element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="property">Identifier of property referred to by radio button.</param>
		/// <param name="order">Relative order of radio buttons.</param>
		/// <returns>Type of this radio button.</returns>
		private RadioButtonType ParseRadioButtonElement(XmlNode node, string property, ref int order)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			RadioButtonType type = RadioButtonType.NotSet;
			string value = null;
			string x = null;
			string y = null;
			string width = null;
			string height = null;
			string text = null;
			string tooltip = null;
			string help = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Bitmap":
						if (RadioButtonType.NotSet != type)
						{
							this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttributes(sourceLineNumbers, node.Name, attrib.Name, "Icon", "Text"));
						}
						text = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						this.core.AddValidReference("Binary", text);
						type = RadioButtonType.Bitmap;
						break;
					case "Height":
						height = this.core.GetAttributeLocalizableIntegerValue(sourceLineNumbers, attrib);
						break;
					case "Help":
						help = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Icon":
						if (RadioButtonType.NotSet != type)
						{
							this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttributes(sourceLineNumbers, node.Name, attrib.Name, "Bitmap", "Text"));
						}
						text = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						this.core.AddValidReference("Binary", text);
						type = RadioButtonType.Icon;
						break;
					case "Text":
						if (RadioButtonType.NotSet != type)
						{
							this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name, attrib.Name, "Bitmap", "Icon"));
						}
						text = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						type = RadioButtonType.Text;
						break;
					case "ToolTip":
						tooltip = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Value":
						value = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Width":
						width = this.core.GetAttributeLocalizableIntegerValue(sourceLineNumbers, attrib);
						break;
					case "X":
						x = this.core.GetAttributeLocalizableIntegerValue(sourceLineNumbers, attrib);
						break;
					case "Y":
						y = this.core.GetAttributeLocalizableIntegerValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == value)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Value"));
			}
			if (null == x)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "X"));
			}
			if (null == y)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Y"));
			}
			if (null == width)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Width"));
			}
			if (null == height)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Height"));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			Row row = this.core.CreateRow(sourceLineNumbers, "RadioButton");
			row[0] = property;
			row[1] = ++order;
			row[2] = value;
			row[3] = x;
			row[4] = y;
			row[5] = width;
			row[6] = height;
			row[7] = text;
			if (null != tooltip || null != help)
			{
				row[8] = String.Concat(tooltip, "|", help);
			}

			return type;
		}

		/// <summary>
		/// Parses a billboard element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		private void ParseBillboardActionElement(XmlNode node)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string action = null;
			int order = 0;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						action = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == action)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}

			this.core.AddValidReference("Actions", String.Concat("InstallExecuteSequence/", action)); // add a reference to the action

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					switch (child.LocalName)
					{
						case "Billboard":
							order = order + 1;
							this.ParseBillboardElement(child, action, order);
							break;
						default:
							this.core.UnexpectedElement(node, child);
							break;
					}
				}
			}
		}

		/// <summary>
		/// Parses a billboard element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="action">Action for the billboard.</param>
		/// <param name="order">Order of the billboard.</param>
		private void ParseBillboardElement(XmlNode node, string action, int order)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;
			string feature = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						break;
					case "Feature":
						feature = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					switch (child.LocalName)
					{
						case "Control":
							// These are all thrown away.
							Row lastTabRow = null;
							string firstControl = null;
							string defaultControl = null;
							string cancelControl = null;

							this.ParseControlElement(child, id, this.tableDefinitions["BBControl"], ref lastTabRow, ref firstControl, ref defaultControl, ref cancelControl, false);
							break;
						default:
							this.core.UnexpectedElement(node, child);
							break;
					}
				}
			}

			// add a reference to the feature
			this.core.AddValidReference("Feature", feature);

			Row row = this.core.CreateRow(sourceLineNumbers, "Billboard");
			row[0] = id;
			row[1] = feature;
			row[2] = action;
			row[3] = order;
		}

		/// <summary>
		/// Parses a control group element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="table">Table referred to by control group.</param>
		/// <param name="childTag">Expected child elements.</param>
		private void ParseControlGroupElement(XmlNode node, TableDefinition table, string childTag)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			int order = 0;
			string property = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Property":
						property = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == property)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Property"));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					if (childTag != child.LocalName)
					{
						this.core.UnexpectedElement(node, child);
					}

					switch (child.LocalName)
					{
						case "ListItem":
							this.ParseListItemElement(child, table, property, ref order);
							break;
						case "Property":
							this.ParsePropertyElement(child);
							break;
						default:
							this.core.UnexpectedElement(node, child);
							break;
					}
				}
			}
		}

		/// <summary>
		/// Parses a radio button control group element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="property">Property associated with this radio button group.</param>
		/// <param name="groupType">Specifies the current type of radio buttons in the group.</param>
		/// <returns>The current type of radio buttons in the group.</returns>
		private RadioButtonType ParseRadioButtonGroupElement(XmlNode node, string property, RadioButtonType groupType)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			int order = 0;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Property":
						property = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == property)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Property"));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					switch (child.LocalName)
					{
						case "RadioButton":
							RadioButtonType type = this.ParseRadioButtonElement(child, property, ref order);
							if (RadioButtonType.NotSet == groupType)
							{
								groupType = type;
							}
							else if (groupType != type)
							{
								SourceLineNumberCollection childSourceLineNumbers = this.core.GetSourceLineNumbers(child);
								this.core.OnMessage(WixErrors.RadioButtonTypeInconsistent(childSourceLineNumbers));
							}
							break;
						default:
							this.core.UnexpectedElement(node, child);
							break;
					}
				}
			}

			return groupType;
		}

		/// <summary>
		/// Parses an action text element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		private void ParseActionTextElement(XmlNode node)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string action = null;
			string template = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Action":
						action = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Template":
						template = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == action)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Action"));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			Row row = this.core.CreateRow(sourceLineNumbers, "ActionText");
			row[0] = action;
			row[1] = node.InnerText;
			row[2] = template;
		}

		/// <summary>
		/// Parses an ui text element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		private void ParseUITextElement(XmlNode node)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			Row row = this.core.CreateRow(sourceLineNumbers, "UIText");
			row[0] = id;
			row[1] = node.InnerText;
		}

		/// <summary>
		/// Parses a text style element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		private void ParseTextStyleElement(XmlNode node)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;
			int bits = 0;
			int color = CompilerCore.IntegerNotSet;
			string faceName = null;
			int size = 0;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						break;

					// RGB Values
					case "Red":
						if (CompilerCore.IntegerNotSet == color)
						{
							color = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						}
						else
						{
							color += this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						}
						break;
					case "Green":
						if (CompilerCore.IntegerNotSet == color)
						{
							color = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib) * 256;
						}
						else
						{
							color += this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib) * 256;
						}
						break;
					case "Blue":
						if (CompilerCore.IntegerNotSet == color)
						{
							color = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib) * 65536;
						}
						else
						{
							color += this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib) * 65536;
						}
						break;

					// Style values
					case "Bold":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							bits |= 1;
						}
						break;
					case "Italic":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							bits |= 2;
						}
						break;
					case "Strike":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							bits |= 8;
						}
						break;
					case "Underline":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							bits |= 4;
						}
						break;

					// Font values
					case "FaceName":
						faceName = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Size":
						size = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						break;

					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}
			if (null == faceName)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "FaceName"));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			Row row = this.core.CreateRow(sourceLineNumbers, "TextStyle");
			row[0] = id;
			row[1] = faceName;
			row[2] = size;
			if (-1 < color)
			{
				row[3] = color;
			}
			if (0 < bits)
			{
				row[4] = bits;
			}
		}

		/// <summary>
		/// Parses a dialog element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		private void ParseDialogElement(XmlNode node)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;
			int bits = MsiInterop.MsidbDialogAttributesVisible | MsiInterop.MsidbDialogAttributesModal | MsiInterop.MsidbDialogAttributesMinimize;
			int height = 0;
			string title = null;
			bool trackDiskSpace = false;
			int width = 0;
			int x = 50;
			int y = 50;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Height":
						height = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						break;
					case "Title":
						title = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Width":
						width = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						break;
					case "X":
						x = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						break;
					case "Y":
						y = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						break;

					case "CustomPalette":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							bits ^= MsiInterop.MsidbDialogAttributesUseCustomPalette;
						}
						break;
					case "ErrorDialog":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							bits ^= MsiInterop.MsidbDialogAttributesError;
						}
						break;
					case "Hidden":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							bits ^= MsiInterop.MsidbDialogAttributesVisible;
						}
						break;
					case "KeepModeless":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							bits ^= MsiInterop.MsidbDialogAttributesKeepModeless;
						}
						break;
					case "LeftScroll":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							bits ^= MsiInterop.MsidbDialogAttributesLeftScroll;
						}
						break;
					case "Modeless":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							bits ^= MsiInterop.MsidbDialogAttributesModal;
						}
						break;
					case "NoMinimize":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							bits ^= MsiInterop.MsidbDialogAttributesMinimize;
						}
						break;
					case "RightAligned":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							bits ^= MsiInterop.MsidbDialogAttributesRightAligned;
						}
						break;
					case "RightToLeft":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							bits ^= MsiInterop.MsidbDialogAttributesRTLRO;
						}
						break;
					case "SystemModal":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							bits ^= MsiInterop.MsidbDialogAttributesSysModal;
						}
						break;
					case "TrackDiskSpace":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							bits ^= MsiInterop.MsidbDialogAttributesTrackDiskSpace;
							trackDiskSpace = true;
						}
						break;

					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			Row lastTabRow = null;
			string cancelControl = null;
			string defaultControl = null;
			string firstControl = null;

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					switch (child.LocalName)
					{
						case "Control":
							this.ParseControlElement(child, id, this.tableDefinitions["Control"], ref lastTabRow, ref firstControl, ref defaultControl, ref cancelControl, trackDiskSpace);
							break;
						default:
							this.core.UnexpectedElement(node, child);
							break;
					}
				}
			}

			if (null != lastTabRow && null != lastTabRow[1])
			{
				if (firstControl != lastTabRow[1].ToString())
				{
					lastTabRow[10] = firstControl;
				}
			}

			if (null == firstControl)
			{
				this.core.OnMessage(WixErrors.NoFirstControlSpecified(sourceLineNumbers, id));
			}

			Row row = this.core.CreateRow(sourceLineNumbers, "Dialog");
			row[0] = id;
			row[1] = x;
			row[2] = y;
			row[3] = width;
			row[4] = height;
			row[5] = bits;
			row[6] = title;
			row[7] = firstControl;
			row[8] = defaultControl;
			row[9] = cancelControl;
		}

		/// <summary>
		/// Parses a dialog reference element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		private void ParseDialogRefElement(XmlNode node)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}
			if (null == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			this.core.AddValidReference("Dialog", id);
		}

		/// <summary>
		/// Parses a control element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="dialog">Identifier for parent dialog.</param>
		/// <param name="table">Table control belongs in.</param>
		/// <param name="lastTabRow">Last row in the tab order.</param>
		/// <param name="firstControl">Name of the first control in the tab order.</param>
		/// <param name="defaultControl">Name of the default control.</param>
		/// <param name="cancelControl">Name of the candle control.</param>
		/// <param name="trackDiskSpace">True if the containing dialog tracks disk space.</param>
		private void ParseControlElement(XmlNode node, string dialog, TableDefinition table, ref Row lastTabRow, ref string firstControl, ref string defaultControl, ref string cancelControl, bool trackDiskSpace)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;
			long bits = 0;
			string checkboxValue = null;
			string controlType = null;
			bool disabled = false;
			string height = null;
			string help = null;
			bool isCancel = false;
			bool isDefault = false;
			bool notTabbable = false;
			string property = null;
			int publishOrder = 0;
			string[] specialAttributes = null;
			string sourceFile = null;
			string text = null;
			string tooltip = null;
			RadioButtonType radioButtonsType = RadioButtonType.NotSet;
			string width = null;
			string x = null;
			string y = null;

			// The rest of the method relies on the control's Type, so we have to get that first.
			XmlAttribute typeAttribute = node.Attributes["Type"];
			if (null == typeAttribute)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Type"));
			}
			else
			{
				controlType = this.core.GetAttributeValue(sourceLineNumbers, typeAttribute);
			}

			switch (controlType)
			{
				case "Billboard":
					specialAttributes = null;
					notTabbable = true;
					disabled = true;

					this.EnsureTable(sourceLineNumbers, "Billboard");
					break;
				case "Bitmap":
					specialAttributes = MsiInterop.BitmapControlAttributes;
					notTabbable = true;
					disabled = true;
					break;
				case "CheckBox":
					specialAttributes = MsiInterop.CheckboxControlAttributes;
					break;
				case "ComboBox":
					specialAttributes = MsiInterop.ComboboxControlAttributes;
					break;
				case "DirectoryCombo":
					specialAttributes = MsiInterop.VolumeControlAttributes;
					break;
				case "DirectoryList":
					specialAttributes = null;
					break;
				case "Edit":
					specialAttributes = MsiInterop.EditControlAttributes;
					break;
				case "GroupBox":
					specialAttributes = null;
					notTabbable = true;
					break;
				case "Icon":
					specialAttributes = MsiInterop.IconControlAttributes;
					notTabbable = true;
					disabled = true;
					break;
				case "Line":
					specialAttributes = null;
					notTabbable = true;
					disabled = true;
					break;
				case "ListBox":
					specialAttributes = MsiInterop.ListboxControlAttributes;
					break;
				case "ListView":
					specialAttributes = MsiInterop.ListviewControlAttributes;
					break;
				case "MaskedEdit":
					specialAttributes = MsiInterop.EditControlAttributes;
					break;
				case "PathEdit":
					specialAttributes = MsiInterop.EditControlAttributes;
					break;
				case "ProgressBar":
					specialAttributes = MsiInterop.ProgressControlAttributes;
					notTabbable = true;
					disabled = true;
					break;
				case "PushButton":
					specialAttributes = MsiInterop.ButtonControlAttributes;
					break;
				case "RadioButtonGroup":
					specialAttributes = MsiInterop.RadioControlAttributes;
					break;
				case "ScrollableText":
					specialAttributes = null;
					break;
				case "SelectionTree":
					specialAttributes = null;
					break;
				case "Text":
					specialAttributes = MsiInterop.TextControlAttributes;
					notTabbable = true;
					break;
				case "VolumeCostList":
					specialAttributes = MsiInterop.VolumeControlAttributes;
					notTabbable = true;
					break;
				case "VolumeSelectCombo":
					specialAttributes = MsiInterop.VolumeControlAttributes;
					break;
				default:
					specialAttributes = null;
					notTabbable = true;
					break;
			}

			if (disabled)
			{
				bits = MsiInterop.MsidbControlAttributesEnabled; // bit will be inverted when stored
			}

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
						break;
					case "Type": // already processed
						break;
					case "Cancel":
						isCancel = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
						break;
					case "CheckBoxValue":
						checkboxValue = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Default":
						isDefault = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
						break;
					case "Height":
						height = this.core.GetAttributeLocalizableIntegerValue(sourceLineNumbers, attrib);
						break;
					case "Help":
						help = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "IconSize":
						long iconSizeBit = -1;
						string iconSizeValue = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						switch (iconSizeValue)
						{
							case CompilerCore.IllegalEmptyAttributeValue:
								break;
							case "16":
								iconSizeBit = Compiler.NameToBit(specialAttributes, "Icon16", YesNoType.Yes);
								break;
							case "32":
								iconSizeBit = Compiler.NameToBit(specialAttributes, "Icon32", YesNoType.Yes);
								break;
							case "48":
								iconSizeBit = (Compiler.NameToBit(specialAttributes, "Icon16", YesNoType.Yes) | Compiler.NameToBit(specialAttributes, "Icon32", YesNoType.Yes));
								break;
							default:
								this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name, attrib.Name, iconSizeValue, "16", "32", "48"));
								break;
						}

						bits ^= (iconSizeBit * 65536);
						break;
					case "Property":
						property = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "TabSkip":
						notTabbable = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
						break;
					case "Text":
						text = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "ToolTip":
						tooltip = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Width":
						width = this.core.GetAttributeLocalizableIntegerValue(sourceLineNumbers, attrib);
						break;
					case "X":
						x = this.core.GetAttributeLocalizableIntegerValue(sourceLineNumbers, attrib);
						break;
					case "Y":
						y = this.core.GetAttributeLocalizableIntegerValue(sourceLineNumbers, attrib);
						break;
					default:
						YesNoType attribValue = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
						long bit = Compiler.NameToBit(MsiInterop.CommonControlAttributes, attrib.Name, attribValue);
						if (-1 == bit)
						{
							bit = Compiler.NameToBit(specialAttributes, attrib.Name, attribValue);
							if (-1 == bit)
							{
								this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
							}
							else
							{
								bit *= 65536;
							}
						}
						bits ^= bit;
						break;
				}
			}

			if (null == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}
			if (null == height)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Height"));
			}
			if (null == width)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Width"));
			}
			if (null == x)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "X"));
			}
			if (null == y)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Y"));
			}

			if (isCancel)
			{
				cancelControl = id;
			}
			if (isDefault)
			{
				defaultControl = id;
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					SourceLineNumberCollection childSourceLineNumbers = this.core.GetSourceLineNumbers(child);
					switch (child.LocalName)
					{
						case "Binary":
							this.ParseBinaryOrIconElement(child, BinaryType.Binary);
							break;
						case "ComboBox":
							this.ParseControlGroupElement(child, this.tableDefinitions["ComboBox"], "ListItem");
							break;
						case "Condition":
							this.ParseConditionElement(child, node.LocalName, id, dialog);
							break;
						case "ListBox":
							this.ParseControlGroupElement(child, this.tableDefinitions["ListBox"], "ListItem");
							break;
						case "ListView":
							this.ParseControlGroupElement(child, this.tableDefinitions["ListView"], "ListItem");
							break;
						case "Property":
							this.ParsePropertyElement(child);
							break;
						case "Publish":
							this.ParsePublishElement(child, dialog, id, ref publishOrder);
							break;
						case "RadioButtonGroup":
							radioButtonsType = this.ParseRadioButtonGroupElement(child, property, radioButtonsType);
							break;
						case "Subscribe":
							this.ParseSubscribeElement(child, dialog, id);
							break;
						case "Text":
							foreach (XmlAttribute attrib in child.Attributes)
							{
								switch (attrib.LocalName)
								{
									case "SourceFile":
									case "src":
										if (null != sourceFile)
										{
											this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(childSourceLineNumbers, node.Name, "src", "SourceFile"));
										}
										sourceFile = this.core.GetAttributeValue(childSourceLineNumbers, attrib);
										break;
									default:
										this.core.UnexpectedAttribute(childSourceLineNumbers, attrib);
										break;
								}
							}

							if (0 < child.InnerText.Length)
							{
								if (null != sourceFile)
								{
									this.core.OnMessage(WixErrors.IllegalAttributeWithInnerText(childSourceLineNumbers, child.Name, "SourceFile"));
								}
								else
								{
									text = child.InnerText;
								}
							}
							break;
						default:
							this.core.UnexpectedElement(node, child);
							break;
					}
				}
			}

			// If the radio buttons have icons, then we need to add the icon attribute.
			switch (radioButtonsType)
			{
				case RadioButtonType.Bitmap:
					bits |= MsiInterop.MsidbControlAttributesBitmap;
					break;
				case RadioButtonType.Icon:
					bits |= MsiInterop.MsidbControlAttributesIcon;
					break;
				case RadioButtonType.Text:
					// Text is the default so nothing needs to be added bits
					break;
			}

			// If we're tracking disk space, and this is a non-FormatSize Text control, and the text attribute starts with
			// '[' and ends with ']', add a space. It is not necessary for the whole string to be a property, just
			// those two characters matter.
			if (trackDiskSpace && "Text" == controlType &&
				MsiInterop.MsidbControlAttributesFormatSize != (bits & MsiInterop.MsidbControlAttributesFormatSize) &&
				null != text && text.StartsWith("[") && text.EndsWith("]"))
			{
				text = String.Concat(text, " ");
			}

			Row row = this.core.CreateRow(sourceLineNumbers, table.Name);
			row[0] = dialog;
			row[1] = id;
			row[2] = controlType;
			row[3] = x;
			row[4] = y;
			row[5] = width;
			row[6] = height;
			row[7] = bits ^ (MsiInterop.MsidbControlAttributesVisible | MsiInterop.MsidbControlAttributesEnabled);
			if ("BBControl" == table.Name)
			{
				row[8] = text; // BBControl.Text
				row[9] = sourceFile;
			}
			else
			{
				row[8] = property;
				row[9] = text;
				if (null != tooltip || null != help)
				{
					row[11] = String.Concat(tooltip, "|", help); // Separator is required, even if only one is non-null.
				}
				row[12] = sourceFile;
			}

			if (!notTabbable)
			{
				if ("BBControl" == table.Name)
				{
					this.core.OnMessage(WixErrors.TabbableControlNotAllowedInBillboard(sourceLineNumbers, node.Name, controlType));
				}
				if (null == firstControl)
				{
					firstControl = id;
				}
				if (null != lastTabRow)
				{
					lastTabRow[10] = id;
				}
				lastTabRow = row;
			}

			if ("CheckBox" == controlType && null != property)
			{
				row = this.core.CreateRow(sourceLineNumbers, "CheckBox");
				row[0] = property;
				row[1] = checkboxValue;
			}

			// binary controls contain a foreign key into the binary table in the text column;
			// add a reference if the identifier of the binary entry is known during compilation
			if ("Bitmap" == controlType && CompilerCore.IsIdentifier(text))
			{
				this.core.AddValidReference("Binary", text);
			}
		}

		/// <summary>
		/// Parses a publish control event element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="dialog">Identifier of parent dialog.</param>
		/// <param name="control">Identifier of parent control.</param>
		/// <param name="order">Relative order of controls.</param>
		private void ParsePublishElement(XmlNode node, string dialog, string control, ref int order)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string argument = null;
			string condition = null;
			string controlEvent = null;
			string property = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Event":
						controlEvent = Compiler.UppercaseFirstChar(this.core.GetAttributeValue(sourceLineNumbers, attrib));
						break;
					case "Property":
						property = String.Concat("[", this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib), "]");
						break;
					case "Value":
						argument = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}
			condition = this.core.GetConditionInnerText(node);

			if (null == controlEvent && null == property) // need to specify at least one
			{
				this.core.OnMessage(WixErrors.ExpectedAttributes(sourceLineNumbers, node.Name, "Event", "Property"));
			}
			else if (null != controlEvent && null != property) // cannot specify both
			{
				this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name, "Event", "Property"));
			}

			if (null == argument)
			{
				if (null != controlEvent)
				{
					this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Value", "Event"));
				}
				else if (null != property)
				{
					// if this is setting a property to null, put a special value in the argument column
					argument = "{}";
				}
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			Row row = this.core.CreateRow(sourceLineNumbers, "ControlEvent");
			row[0] = dialog;
			row[1] = control;
			row[2] = (null != controlEvent ? controlEvent : property);
			row[3] = argument;
			row[4] = condition;
			row[5] = ++order;

			if ("DoAction" == controlEvent && null != argument)
			{
				// if we're not looking at a standard action then create a reference
				// to the custom action.
				if (!Common.IsStandardAction(argument))
				{
					this.core.AddValidReference("CustomAction", argument);
				}
			}

			// if we're referring to a dialog but not through a property, add it to the references
			if (("NewDialog" == controlEvent || "SpawnDialog" == controlEvent || "SpawnWaitDialog" == controlEvent || "SelectionBrowse" == controlEvent) && CompilerCore.IsIdentifier(argument))
			{
				this.core.AddValidReference("Dialog", argument);
			}
		}

		/// <summary>
		/// Parses a recycle time element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <returns>Recycle time value.</returns>
		private string ParseRecycleTimeElement(XmlNode node)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string value = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Value":
						value = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == value)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Value"));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			return value;
		}

		/// <summary>
		/// Parses a control subscription element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="dialog">Identifier of dialog.</param>
		/// <param name="control">Identifier of control.</param>
		private void ParseSubscribeElement(XmlNode node, string dialog, string control)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string controlAttribute = null;
			string eventMapping = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Attribute":
						controlAttribute = Compiler.UppercaseFirstChar(this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib));
						break;
					case "Event":
						eventMapping = Compiler.UppercaseFirstChar(this.core.GetAttributeIdentifierValue(sourceLineNumbers, attrib));
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			Row row = this.core.CreateRow(sourceLineNumbers, "EventMapping");
			row[0] = dialog;
			row[1] = control;
			row[2] = eventMapping;
			row[3] = controlAttribute;
		}

		/// <summary>
		/// Parses an upgrade element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		private void ParseUpgradeElement(XmlNode node)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeGuidValue(sourceLineNumbers, attrib, false);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}

			// process the UpgradeVersion children here
			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					switch (child.LocalName)
					{
						case "Property":
							this.ParsePropertyElement(child);
							break;
						case "UpgradeVersion":
							this.ParseUpgradeVersionElement(child, id);
							break;
						default:
							this.core.UnexpectedElement(node, child);
							break;
					}
				}
			}

			// No rows created here. All row creation is done in ParseUpgradeVersionElement.
		}

		/// <summary>
		/// Parse upgrade version element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="upgradeId">Upgrade code.</param>
		private void ParseUpgradeVersionElement(XmlNode node, string upgradeId)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);

			string actionProperty = null;
			string language = null;
			string maximum = null;
			string minimum = null;
			int options = 256;
			string removeFeatures = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "ExcludeLanguages":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							options |= 1024;
						}
						break;
					case "IgnoreRemoveFailure":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							options |= 4;
						}
						break;
					case "IncludeMaximum":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							options |= 512;
						}
						break;
					case "IncludeMinimum": // this is "yes" by default
						if (YesNoType.No == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							options &= ~256;
						}
						break;
					case "Language":
						language = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Minimum":
						minimum = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Maximum":
						maximum = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "MigrateFeatures":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							options |= 1;
						}
						break;
					case "OnlyDetect":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							options |= 2;
						}
						break;
					case "Property":
						actionProperty = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "RemoveFeatures":
						removeFeatures = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						if (!this.TryExtensionParseForAttribute(sourceLineNumbers, (XmlElement)node, attrib))
						{
							this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						}
						break;
				}
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			if (null == actionProperty)
			{
				actionProperty = String.Concat("UC", upgradeId.Replace("-", String.Empty));
				this.AddProperty(sourceLineNumbers, actionProperty, null, false, true, false);
			}

			Row row = this.core.CreateRow(sourceLineNumbers, "Upgrade");
			row[0] = String.Concat("{", upgradeId, "}");
			row[1] = minimum;
			row[2] = maximum;
			row[3] = language;
			row[4] = options;
			row[5] = removeFeatures;
			row[6] = actionProperty;

			if (1 == row.Table.Rows.Count)
			{
				// Ensure that RemoveExistingProducts is authored in InstallExecuteSequence
				// if at least one row in Upgrade table
				this.core.AddValidReference("Actions", "InstallExecuteSequence/RemoveExistingProducts");
			}
		}

		/// <summary>
		/// Parses an user element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="componentId">Optional identifier of parent component.</param>
		private void ParseUserElement(XmlNode node, string componentId)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;
			int attributes = 0;
			string domain = null;
			string name = null;
			string password = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "CanNotChangePassword":
						if (null == componentId)
						{
							this.core.OnMessage(WixErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
						}

						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							attributes |= 0x00000002; //#define SCAU_PASSWD_CANT_CHANGE 0x00000002
						}
						break;
					case "CreateUser":
						if (null == componentId)
						{
							this.core.OnMessage(WixErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
						}

						if (YesNoType.No == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							attributes |= 0x00000200; //#define SCAU_DONT_CREATE_USER 0x00000200
						}
						break;
					case "Disabled":
						if (null == componentId)
						{
							this.core.OnMessage(WixErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
						}

						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							attributes |= 0x00000008; //#define SCAU_DISABLE_ACCOUNT 0x00000008
						}
						break;
					case "Domain":
						domain = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "FailIfExists":
						if (null == componentId)
						{
							this.core.OnMessage(WixErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
						}

						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							attributes |= 0x00000010; //#define SCAU_FAIL_IF_EXISTS 0x00000010
						}
						break;
					case "Name":
						name = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Password":
						password = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "PasswordExpired":
						if (null == componentId)
						{
							this.core.OnMessage(WixErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
						}

						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							attributes |= 0x00000004; //#define SCAU_PASSWD_CHANGE_REQD_ON_LOGIN 0x00000004
						}
						break;
					case "PasswordNeverExpires":
						if (null == componentId)
						{
							this.core.OnMessage(WixErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
						}

						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							attributes |= 0x00000001; //#define SCAU_DONT_EXPIRE_PASSWRD 0x00000001
						}
						break;
					case "RemoveOnUninstall":
						if (null == componentId)
						{
							this.core.OnMessage(WixErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
						}

						if (YesNoType.No == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							attributes |= 0x00000100; //#define SCAU_DONT_REMOVE_ON_UNINSTALL 0x00000100
						}
						break;
					case "UpdateIfExists":
						if (null == componentId)
						{
							this.core.OnMessage(WixErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
						}

						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							attributes |= 0x00000020; //#define SCAU_UPDATE_IF_EXISTS 0x00000020
						}
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}
			if (null == name)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Name"));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					SourceLineNumberCollection childSourceLineNumbers = this.core.GetSourceLineNumbers(child);
					switch (child.LocalName)
					{
						case "GroupRef":
							if (null == componentId)
							{
								this.core.OnMessage(WixErrors.IllegalElementWithoutComponent(childSourceLineNumbers, child.Name));
							}

							this.ParseGroupRefElement(child, id);
							break;
						default:
							this.core.UnexpectedElement(node, child);
							break;
					}
				}
			}

			if (null != componentId)
			{
				// Reference ConfigureIIs since nothing will happen without it
				this.core.AddValidReference("CustomAction", "ConfigureUsers");
			}

			Row row = this.core.CreateRow(sourceLineNumbers, "User");
			row[0] = id;
			row[1] = componentId;
			row[2] = name;
			row[3] = domain;
			row[4] = password;
			row[5] = attributes;
		}

		/// <summary>
		/// Parses a GroupRef element
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="userId">Required user id to be joined to the group.</param>
		private void ParseGroupRefElement(XmlNode node, String userId)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string groupId = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						groupId = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			Row row = this.core.CreateRow(sourceLineNumbers, "UserGroup");
			row[0] = userId;
			row[1] = groupId;

			this.core.AddValidReference("Group", groupId);
		}

		/// <summary>
		/// Parses a group element.
		/// </summary>
		/// <param name="node">Node to be parsed.</param>
		/// <param name="componentId">Component Id of the parent component of this element.</param>
		private void ParseGroupElement(XmlNode node, string componentId)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;
			string domain = null;
			string name = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Name":
						name = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Domain":
						domain = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			Row row = this.core.CreateRow(sourceLineNumbers, "Group");
			row[0] = id;
			row[1] = componentId;
			row[2] = name;
			row[3] = domain;
		}

		/// <summary>
		/// Parses a web directory properties element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		private void ParseWebDirPropertiesElement(XmlNode node)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;
			int access = 0;
			bool accessSet = false;
			int accessSSLFlags = 0;
			bool accessSSLFlagsSet = false;
			string anonymousUser = null;
			YesNoType aspDetailedError = YesNoType.NotSet;
			string authenticationProviders = null;
			int authorization = 0;
			bool authorizationSet = false;
			string cacheControlCustom = null;
			long cacheControlMaxAge = CompilerCore.LongNotSet;
			string defaultDocuments = null;
			string httpExpires = null;
			bool iisControlledPassword = false;
			YesNoType index = YesNoType.NotSet;
			YesNoType logVisits = YesNoType.NotSet;
			YesNoType notCustomError = YesNoType.NotSet;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "AnonymousUser":
						anonymousUser = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						this.core.AddValidReference("User", anonymousUser);
						break;
					case "AspDetailedError":
						aspDetailedError = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
						break;
					case "AuthenticationProviders":
						authenticationProviders = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "CacheControlCustom":
						cacheControlCustom = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "CacheControlMaxAge":
						cacheControlMaxAge = this.core.GetAttributeLongValue(sourceLineNumbers, attrib); // Range is 0 to 4294967295 (0xFFFFFFFF).  4294967295 represents unlimited.
						break;
					case "ClearCustomError":
						notCustomError = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
						break;
					case "DefaultDocuments":
						defaultDocuments = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "HttpExpires":
						httpExpires = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "IIsControlledPassword":
						iisControlledPassword = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
						break;
					case "Index":
						index = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
						break;
					case "LogVisits":
						logVisits = this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
						break;

					// Access attributes
					case "Execute":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							access |= 4;
						}
						else
						{
							access &= ~4;
						}
						accessSet = true;
						break;
					case "Read":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							access |= 1;
						}
						else
						{
							access &= ~1;
						}
						accessSet = true;
						break;
					case "Script":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							access |= 512;
						}
						else
						{
							access &= ~512;
						}
						accessSet = true;
						break;
					case "Write":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							access |= 2;
						}
						else
						{
							access &= ~2;
						}
						accessSet = true;
						break;

					// AccessSSL Attributes
					case "AccessSSL":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							accessSSLFlags |= 8;
						}
						else
						{
							accessSSLFlags &= ~8;
						}
						accessSSLFlagsSet = true;
						break;
					case "AccessSSL128":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							accessSSLFlags |= 256;
						}
						else
						{
							accessSSLFlags &= ~256;
						}
						accessSSLFlagsSet = true;
						break;
					case "AccessSSLMapCert":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							accessSSLFlags |= 128;
						}
						else
						{
							accessSSLFlags &= ~128;
						}
						accessSSLFlagsSet = true;
						break;
					case "AccessSSLNegotiateCert":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							accessSSLFlags |= 32;
						}
						else
						{
							accessSSLFlags &= ~32;
						}
						accessSSLFlagsSet = true;
						break;
					case "AccessSSLRequireCert":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							accessSSLFlags |= 64;
						}
						else
						{
							accessSSLFlags &= ~64;
						}
						accessSSLFlagsSet = true;
						break;

					// Authorization attributes
					case "AnonymousAccess":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							authorization |= 1;
						}
						else
						{
							authorization &= ~1;
						}
						authorizationSet = true;
						break;
					case "BasicAuthentication":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							authorization |= 2;
						}
						else
						{
							authorization &= ~2;
						}
						authorizationSet = true;
						break;
					case "DigestAuthentication":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							authorization |= 16;
						}
						else
						{
							authorization &= ~16;
						}
						authorizationSet = true;
						break;
					case "PassportAuthentication":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							authorization |= 64;
						}
						else
						{
							authorization &= ~64;
						}
						authorizationSet = true;
						break;
					case "WindowsAuthentication":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							authorization |= 4;
						}
						else
						{
							authorization &= ~4;
						}
						authorizationSet = true;
						break;

					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			Row row = this.core.CreateRow(sourceLineNumbers, "IIsWebDirProperties");
			row[0] = id;
			if (accessSet)
			{
				row[1] = access;
			}
			if (authorizationSet)
			{
				row[2] = authorization;
			}
			row[3] = anonymousUser;
			row[4] = iisControlledPassword ? 1 : 0;
			if (YesNoType.NotSet != logVisits)
			{
				row[5] = YesNoType.Yes == logVisits ? 1 : 0;
			}
			if (YesNoType.NotSet != index)
			{
				row[6] = YesNoType.Yes == index ? 1 : 0;
			}
			row[7] = defaultDocuments;
			if (YesNoType.NotSet != aspDetailedError)
			{
				row[8] = YesNoType.Yes == aspDetailedError ? 1 : 0;
			}
			row[9] = httpExpires;
			if (CompilerCore.LongNotSet != cacheControlMaxAge)
			{
				row[10] = cacheControlMaxAge;
			}
			row[11] = cacheControlCustom;
			if (YesNoType.NotSet != notCustomError)
			{
				row[12] = YesNoType.Yes == notCustomError ? 1 : 0;
			}
			if (accessSSLFlagsSet)
			{
				row[13] = accessSSLFlags;
			}
			if (null != authenticationProviders)
			{
				row[14] = authenticationProviders;
			}
		}

		/// <summary>
		/// Parses a web application extension element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="application">Identifier for parent web application.</param>
		private void ParseWebApplicationExtensionElement(XmlNode node, string application)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			int attributes = 0;
			string executable = null;
			string extension = null;
			string verbs = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "CheckPath":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							attributes |= 4;
						}
						else
						{
							attributes &= ~4;
						}
						break;
					case "Executable":
						executable = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Extension":
						extension = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Script":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							attributes |= 1;
						}
						else
						{
							attributes &= ~1;
						}
						break;
					case "Verbs":
						verbs = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			Row row = this.core.CreateRow(sourceLineNumbers, "IIsWebApplicationExtension");
			row[0] = application;
			row[1] = extension;
			row[2] = verbs;
			row[3] = executable;
			if (0 < attributes)
			{
				row[4] = attributes;
			}
		}

		/// <summary>
		/// Parses a certificate element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="componentId">Identifier for parent component.</param>
		private void ParseCertificateElement(XmlNode node, string componentId)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;
			int attributes = 0;
			string binaryKey = null;
			string certificatePath = null;
			string name = null;
			string pfxPassword = null;
			int storeLocation = 0;
			string storeName = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "BinaryKey":
						attributes |= 2; // SCA_CERT_ATTRIBUTE_BINARYDATA
						binaryKey = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						this.core.AddValidReference("Binary", binaryKey);
						break;
					case "CertificatePath":
						certificatePath = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Name":
						name = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Overwrite":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							attributes |= 4; // SCA_CERT_ATTRIBUTE_OVERWRITE
						}
						else
						{
							attributes &= ~4; // SCA_CERT_ATTRIBUTE_OVERWRITE
						}
						break;
					case "PFXPassword":
						pfxPassword = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Request":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							attributes |= 1; // SCA_CERT_ATTRIBUTE_REQUEST
						}
						else
						{
							attributes &= ~1; // SCA_CERT_ATTRIBUTE_REQUEST
						}
						break;
					case "StoreLocation":
						string storeLocationValue = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						switch (storeLocationValue)
						{
							case CompilerCore.IllegalEmptyAttributeValue:
								break;
							case "currentUser":
								storeLocation = 1; // SCA_CERTSYSTEMSTORE_CURRENTUSER
								break;
							case "localMachine":
								storeLocation = 2; // SCA_CERTSYSTEMSTORE_LOCALMACHINE
								break;
							default:
								this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name, "StoreLocation", storeLocationValue, "currentUser", "localMachine"));
								break;
						}
						break;
					case "StoreName":
						string storeNameValue = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						switch (storeNameValue)
						{
							case CompilerCore.IllegalEmptyAttributeValue:
								break;
							case "ca":
								storeName = "CA";
								break;
							case "my":
							case "personal":
								storeName = "MY";
								break;
							case "request":
								storeName = "REQUEST";
								break;
							case "root":
								storeName = "Root";
								break;
							case "otherPeople":
								storeName = "AddressBook";
								break;
							default:
								this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name, "StoreName", storeNameValue, "ca", "my", "request", "root"));
								break;
						}
						break;
				}
			}

			if (null == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}

			if (null == name)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Name"));
			}

			if (0 == storeLocation)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "StoreLocation"));
			}

			if (null == storeName)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "StoreName"));
			}

			if (null != binaryKey && null != certificatePath)
			{
				this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name, "BinaryKey", "CertificatePath", certificatePath));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			// Reference InstallCertificate and UninstallCertificate since nothing will happen without it
			this.core.AddValidReference("CustomAction", "InstallCertificates");
			this.core.AddValidReference("CustomAction", "UninstallCertificates");
			this.EnsureTable(sourceLineNumbers, "CertificateHash"); // Certificate CustomActions require the CertficateHash table

			Row row = this.core.CreateRow(sourceLineNumbers, "Certificate");
			row[0] = id;
			row[1] = componentId;
			row[2] = name;
			row[3] = storeLocation;
			row[4] = storeName;
			row[5] = attributes;
			if (null != binaryKey)
			{
				row[6] = binaryKey;
			}
			else
			{
				row[6] = certificatePath;
			}
			row[7] = pfxPassword;
		}

		/// <summary>
		/// Parses a CertificateRef extension element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="webId">Identifier for parent web site.</param>
		private void ParseCertificateRefElement(XmlNode node, string webId)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			this.core.AddValidReference("Certificate", id);

			Row row = this.core.CreateRow(sourceLineNumbers, "IIsWebSiteCertificates");
			row[0] = webId;
			row[1] = id;
		}

		/// <summary>
		/// Parses a web application element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <returns>Identifier for web application.</returns>
		private string ParseWebApplicationElement(XmlNode node)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;
			YesNoDefaultType allowSessions = YesNoDefaultType.Default;
			string appPool = null;
			YesNoDefaultType buffer = YesNoDefaultType.Default;
			YesNoDefaultType clientDebugging = YesNoDefaultType.Default;
			string defaultScript = null;
			int isolation = 0;
			string name = null;
			YesNoDefaultType parentPaths = YesNoDefaultType.Default;
			int scriptTimeout = CompilerCore.IntegerNotSet;
			int sessionTimeout = CompilerCore.IntegerNotSet;
			YesNoDefaultType serverDebugging = YesNoDefaultType.Default;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "AllowSessions":
						allowSessions = this.core.GetAttributeYesNoDefaultValue(sourceLineNumbers, attrib);
						break;
					case "Buffer":
						buffer = this.core.GetAttributeYesNoDefaultValue(sourceLineNumbers, attrib);
						break;
					case "ClientDebugging":
						clientDebugging = this.core.GetAttributeYesNoDefaultValue(sourceLineNumbers, attrib);
						break;
					case "DefaultScript":
						defaultScript = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Isolation":
						string isolationValue = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						switch (isolationValue)
						{
							case CompilerCore.IllegalEmptyAttributeValue:
								break;
							case "low":
								isolation = 0;
								break;
							case "medium":
								isolation = 2;
								break;
							case "high":
								isolation = 1;
								break;
							default:
								this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name, attrib.Name, isolationValue, "low", "medium", "high"));
								break;
						}
						break;
					case "Name":
						name = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "ParentPaths":
						parentPaths = this.core.GetAttributeYesNoDefaultValue(sourceLineNumbers, attrib);
						break;
					case "ScriptTimeout":
						scriptTimeout = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						break;
					case "ServerDebugging":
						serverDebugging = this.core.GetAttributeYesNoDefaultValue(sourceLineNumbers, attrib);
						break;
					case "SessionTimeout":
						sessionTimeout = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						break;
					case "WebAppPool":
						appPool = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}
			if (null == name)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Name"));
			}
			if (-1 != name.IndexOf("\\"))
			{
				this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name, "Name", name, '\\'));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					switch (child.LocalName)
					{
						case "WebApplicationExtension":
							this.ParseWebApplicationExtensionElement(child, id);
							break;
						default:
							this.core.UnexpectedElement(node, child);
							break;
					}
				}
			}

			this.core.AddValidReference("IIsAppPool", appPool);

			Row row = this.core.CreateRow(sourceLineNumbers, "IIsWebApplication");
			row[0] = id;
			row[1] = name;
			row[2] = isolation;
			if (YesNoDefaultType.Default != allowSessions)
			{
				row[3] = YesNoDefaultType.Yes == allowSessions ? 1 : 0;
			}
			if (CompilerCore.IntegerNotSet != sessionTimeout)
			{
				row[4] = sessionTimeout;
			}
			if (YesNoDefaultType.Default != buffer)
			{
				row[5] = YesNoDefaultType.Yes == buffer ? 1 : 0;
			}
			if (YesNoDefaultType.Default != parentPaths)
			{
				row[6] = YesNoDefaultType.Yes == parentPaths ? 1 : 0;
			}
			row[7] = defaultScript;
			if (CompilerCore.IntegerNotSet != scriptTimeout)
			{
				row[8] = scriptTimeout;
			}
			if (YesNoDefaultType.Default != serverDebugging)
			{
				row[9] = YesNoDefaultType.Yes == serverDebugging ? 1 : 0;
			}
			if (YesNoDefaultType.Default != clientDebugging)
			{
				row[10] = YesNoDefaultType.Yes == clientDebugging ? 1 : 0;
			}
			row[11] = appPool;

			return id;
		}

		/// <summary>
		/// Parses web application pool element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="componentId">Optional identifier of parent component.</param>
		private void ParseWebAppPoolElement(XmlNode node, string componentId)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;
			int attributes = 0;
			int cpuAction = CompilerCore.IntegerNotSet;
			string cpuMon = null;
			int idleTimeout = CompilerCore.IntegerNotSet;
			int maxCpuUsage = 0;
			int maxWorkerProcs = CompilerCore.IntegerNotSet;
			string name = null;
			int privateMemory = CompilerCore.IntegerNotSet;
			int queueLimit = CompilerCore.IntegerNotSet;
			int recycleMinutes = CompilerCore.IntegerNotSet;
			int recycleRequests = CompilerCore.IntegerNotSet;
			string recycleTimes = null;
			int refreshCpu = CompilerCore.IntegerNotSet;
			string user = null;
			int virtualMemory = CompilerCore.IntegerNotSet;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "CpuAction":
						if (null == componentId)
						{
							this.core.OnMessage(WixErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
						}

						string cpuActionValue = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						switch (cpuActionValue)
						{
							case CompilerCore.IllegalEmptyAttributeValue:
								break;
							case "shutdown":
								cpuAction = 1;
								break;
							case "none":
								cpuAction = 0;
								break;
							default:
								this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name, attrib.Name, cpuActionValue, "shutdown", "none"));
								break;
						}
						break;
					case "Identity":
						if (null == componentId)
						{
							this.core.OnMessage(WixErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
						}

						string identityValue = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						switch (identityValue)
						{
							case CompilerCore.IllegalEmptyAttributeValue:
								break;
							case "networkService":
								attributes = 1;
								break;
							case "localService":
								attributes = 2;
								break;
							case "localSystem":
								attributes = 4;
								break;
							case "other":
								attributes = 8;
								break;
							default:
								this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name, attrib.Name, identityValue, "networkService", "localService", "localSystem", "other"));
								break;
						}
						break;
					case "IdleTimeout":
						if (null == componentId)
						{
							this.core.OnMessage(WixErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
						}

						idleTimeout = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						break;
					case "MaxCpuUsage":
						if (null == componentId)
						{
							this.core.OnMessage(WixErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
						}

						maxCpuUsage = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						break;
					case "MaxWorkerProcesses":
						if (null == componentId)
						{
							this.core.OnMessage(WixErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
						}

						maxWorkerProcs = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						break;
					case "Name":
						name = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "PrivateMemory":
						if (null == componentId)
						{
							this.core.OnMessage(WixErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
						}

						privateMemory = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						break;
					case "QueueLimit":
						if (null == componentId)
						{
							this.core.OnMessage(WixErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
						}

						queueLimit = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						break;
					case "RecycleMinutes":
						if (null == componentId)
						{
							this.core.OnMessage(WixErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
						}

						recycleMinutes = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						break;
					case "RecycleRequests":
						if (null == componentId)
						{
							this.core.OnMessage(WixErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
						}

						recycleRequests = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						break;
					case "RefreshCpu":
						if (null == componentId)
						{
							this.core.OnMessage(WixErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
						}

						refreshCpu = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						break;
					case "User":
						if (null == componentId)
						{
							this.core.OnMessage(WixErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
						}

						user = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "VirtualMemory":
						if (null == componentId)
						{
							this.core.OnMessage(WixErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
						}

						virtualMemory = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}
			if (null == name)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Name"));
			}
			if (null == user && 8 == attributes)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "User", "Identity", "other"));
			}
			if (null != user && 8 != attributes)
			{
				this.core.OnMessage(WixErrors.IllegalAttributeValueWithoutOtherAttribute(sourceLineNumbers, node.Name, "User", user, "Identity", "other"));
			}

			cpuMon = maxCpuUsage.ToString(CultureInfo.InvariantCulture.NumberFormat);
			if (CompilerCore.IntegerNotSet != refreshCpu)
			{
				cpuMon = String.Concat(cpuMon, ",", refreshCpu.ToString(CultureInfo.InvariantCulture.NumberFormat));
				if (CompilerCore.IntegerNotSet != cpuAction)
				{
					cpuMon = String.Concat(cpuMon, ",", cpuAction.ToString(CultureInfo.InvariantCulture.NumberFormat));
				}
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					switch (child.LocalName)
					{
						case "RecycleTime":
							if (null == componentId)
							{
								SourceLineNumberCollection childSourceLineNumbers = this.core.GetSourceLineNumbers(child);
								this.core.OnMessage(WixErrors.IllegalElementWithoutComponent(childSourceLineNumbers, node.Name));
							}

							if (null == recycleTimes)
							{
								recycleTimes = this.ParseRecycleTimeElement(child);
							}
							else
							{
								recycleTimes = String.Concat(recycleTimes, ",", this.ParseRecycleTimeElement(child));
							}
							break;
						default:
							this.core.UnexpectedElement(node, child);
							break;
					}
				}
			}

			this.core.AddValidReference("User", user);

			if (null != componentId)
			{
				// Reference ConfigureIIs since nothing will happen without it
				this.core.AddValidReference("CustomAction", "ConfigureIIs");
			}

			Row row = this.core.CreateRow(sourceLineNumbers, "IIsAppPool");
			row[0] = id;
			row[1] = name;
			row[2] = componentId;
			row[3] = attributes;
			row[4] = user;
			if (CompilerCore.IntegerNotSet != recycleMinutes)
			{
				row[5] = recycleMinutes;
			}
			if (CompilerCore.IntegerNotSet != recycleRequests)
			{
				row[6] = recycleRequests;
			}
			row[7] = recycleTimes;
			if (CompilerCore.IntegerNotSet != idleTimeout)
			{
				row[8] = idleTimeout;
			}
			if (CompilerCore.IntegerNotSet != queueLimit)
			{
				row[9] = queueLimit;
			}
			row[10] = cpuMon;
			if (CompilerCore.IntegerNotSet != maxWorkerProcs)
			{
				row[11] = maxWorkerProcs;
			}

			if (CompilerCore.IntegerNotSet != virtualMemory)
			{
				row[12] = virtualMemory;
			}

			if (CompilerCore.IntegerNotSet != privateMemory)
			{
				row[13] = privateMemory;
			}
		}

		/// <summary>
		/// Parses a web filter element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="componentId">Identifier of parent component.</param>
		/// <param name="parentWeb">Optional identifier of parent web site.</param>
		private void ParseWebFilterElement(XmlNode node, string componentId, string parentWeb)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;
			string description = null;
			int flags = 0;
			int loadOrder = CompilerCore.IntegerNotSet;
			string name = null;
			string path = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Description":
						description = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Flags":
						flags = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						break;
					case "LoadOrder":
						string loadOrderValue = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						switch (loadOrderValue)
						{
							case CompilerCore.IllegalEmptyAttributeValue:
								break;
							case "first":
								loadOrder = 0;
								break;
							case "last":
								loadOrder = -1;
								break;
							default:
								loadOrder = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
								break;
						}
						break;
					case "Name":
						name = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Path":
						path = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "WebSite":
						if (null != parentWeb)
						{
							this.core.OnMessage(WixErrors.WebSiteAttributeUnderWebSite(sourceLineNumbers, node.Name));
						}

						parentWeb = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						this.core.AddValidReference("IIsWebSite", parentWeb);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}
			if (null == name)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Name"));
			}
			if (null == path)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Path"));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			// Reference ConfigureIIs since nothing will happen without it
			this.core.AddValidReference("CustomAction", "ConfigureIIs");

			Row row = this.core.CreateRow(sourceLineNumbers, "IIsFilter");
			row[0] = id;
			row[1] = name;
			row[2] = componentId;
			row[3] = path;
			row[4] = parentWeb;
			row[5] = description;
			row[6] = flags;
			if (CompilerCore.IntegerNotSet != loadOrder)
			{
				row[7] = loadOrder;
			}
		}

		/// <summary>
		/// Parses a web address element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="parentWeb">Identifier of parent web site.</param>
		/// <returns>Identifier for web address.</returns>
		private string ParseWebAddressElement(XmlNode node, string parentWeb)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;
			string header = null;
			string ip = null;
			string port = null;
			bool secure = false;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Header":
						header = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "IP":
						ip = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Port":
						port = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Secure":
						secure = YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}
			if (null == port)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Port"));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			Row row = this.core.CreateRow(sourceLineNumbers, "IIsWebAddress");
			row[0] = id;
			row[1] = parentWeb;
			row[2] = ip;
			row[3] = port;
			row[4] = header;
			row[5] = secure ? 1 : 0;

			return id;
		}

		/// <summary>
		/// Parses a web error element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="parentType">Type of the parent.</param>
		/// <param name="parent">Id of the parent.</param>
		private void ParseWebErrorElement(XmlNode node, WebErrorParentType parentType, string parent)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			int errorCode = CompilerCore.IntegerNotSet;
			string file = null;
			string url = null;
			int subCode = CompilerCore.IntegerNotSet;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "ErrorCode":
						errorCode = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						break;
					case "File":
						file = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "SubCode":
						subCode = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						break;
					case "URL":
						url = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (CompilerCore.IntegerNotSet == errorCode)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "ErrorCode"));
				errorCode = CompilerCore.IllegalInteger;
			}
			if (CompilerCore.IntegerNotSet == subCode)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "SubCode"));
				subCode = CompilerCore.IllegalInteger;
			}
			if (400 > errorCode || 599 < errorCode)
			{
				this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name, "ErrorCode", errorCode.ToString()));
				errorCode = CompilerCore.IllegalInteger;
			}
			if (null != file && null != url)
			{
				this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name, "File", "URL"));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			// Reference ConfigureIIs since nothing will happen without it
			this.core.AddValidReference("CustomAction", "ConfigureIIs");

			Row row = this.core.CreateRow(sourceLineNumbers, "IIsWebError");
			row[0] = errorCode;
			row[1] = subCode;
			row[2] = (int)parentType;
			row[3] = parent;
			row[4] = file;
			row[5] = url;
		}

		/// <summary>
		/// Parses a HTTP Header element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="parentType">Type of the parent.</param>
		/// <param name="parent">Id of the parent.</param>
		private void ParseHttpHeaderElement(XmlNode node, HttpHeaderParentType parentType, string parent)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string headerName = null;
			string headerValue = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Name":
						headerName = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Value":
						headerValue = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == headerName)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Name"));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			// Reference ConfigureIIs since nothing will happen without it
			this.core.AddValidReference("CustomAction", "ConfigureIIs");

			Row row = this.core.CreateRow(sourceLineNumbers, "IIsHttpHeader");
			row[0] = headerName;
			row[1] = (int)parentType;
			row[2] = parent;
			row[3] = headerName;
			row[4] = headerValue;
			row[5] = 0;
			row[6] = null;
		}

		/// <summary>
		/// Parses a virtual directory element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="componentId">Identifier of parent component.</param>
		/// <param name="parentWeb">Identifier of parent web site.</param>
		/// <param name="parentAlias">Alias of the parent web site.</param>
		private void ParseWebVirtualDirElement(XmlNode node, string componentId, string parentWeb, string parentAlias)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;
			string alias = null;
			string application = null;
			string directory = null;
			string dirProperties = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Alias":
						alias = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Directory":
						directory = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "DirProperties":
						dirProperties = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "WebApplication":
						application = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "WebSite":
						if (null != parentWeb)
						{
							this.core.OnMessage(WixErrors.WebSiteAttributeUnderWebSite(sourceLineNumbers, node.Name));
						}

						parentWeb = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						this.core.AddValidReference("IIsWebSite", parentWeb);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}
			if (null == alias)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Alias"));
			}
			if (null == directory)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Directory"));
			}
			if (null == parentWeb)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "WebSite"));
			}
			if (-1 != alias.IndexOf("\\"))
			{
				this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name, "Alias", alias, '\\'));
			}

			if (null == componentId)
			{
				this.core.OnMessage(WixErrors.IllegalElementWithoutComponent(sourceLineNumbers, node.Name));
			}

			if (null != parentAlias)
			{
				alias = String.Concat(parentAlias, "/", alias);
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					SourceLineNumberCollection childSourceLineNumbers = this.core.GetSourceLineNumbers(child);
					switch (child.LocalName)
					{
						case "WebApplication":
							if (null != application)
							{
								this.core.OnMessage(WixErrors.WebApplicationAlreadySpecified(childSourceLineNumbers, node.Name));
							}

							application = this.ParseWebApplicationElement(child);
							break;
						case "WebError":
							this.ParseWebErrorElement(child, WebErrorParentType.WebVirtualDir, id);
							break;
						case "WebVirtualDir":
							this.ParseWebVirtualDirElement(child, componentId, parentWeb, alias);
							break;
						case "HttpHeader":
							this.ParseHttpHeaderElement(child, HttpHeaderParentType.WebVirtualDir, id);
							break;
						case "MimeMap":
							this.ParseMimeMapElement(child, id, MimeMapParentType.WebVirtualDir);
							break;
						default:
							this.core.UnexpectedElement(node, child);
							break;
					}
				}
			}

			this.core.AddValidReference("Directory", directory);
			this.core.AddValidReference("IIsWebDirProperties", dirProperties);
			this.core.AddValidReference("IIsWebApplication", application);

			// Reference ConfigureIIs since nothing will happen without it
			this.core.AddValidReference("CustomAction", "ConfigureIIs");

			Row row = this.core.CreateRow(sourceLineNumbers, "IIsWebVirtualDir");
			row[0] = id;
			row[1] = componentId;
			row[2] = parentWeb;
			row[3] = alias;
			row[4] = directory;
			row[5] = dirProperties;
			row[6] = application;
		}

		/// <summary>
		/// Parses a mime map element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="parentId">Identifier for parent symbol.</param>
		/// <param name="parentType">Type that parentId refers to.</param>
		private void ParseMimeMapElement(XmlNode node, string parentId, MimeMapParentType parentType)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;
			string extension = null;
			string type = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Extension":
						extension = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Type":
						type = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}
			if (null == extension)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Extension"));
			}
			else if (CompilerCore.IllegalEmptyAttributeValue != extension)
			{
				if (!extension.StartsWith("."))
				{
					this.core.OnMessage(WixErrors.MimeMapExtensionMissingPeriod(sourceLineNumbers, node.Name, "Extension", extension));
				}
			}
			if (null == type)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Type"));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			Row row = this.core.CreateRow(sourceLineNumbers, "IIsMimeMap");
			row[0] = id;
			row[1] = (int)parentType;
			row[2] = parentId;
			row[3] = type;
			row[4] = extension;
		}

		/// <summary>
		/// Parses a web directory element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="componentId">Identifier for parent component.</param>
		/// <param name="parentWeb">Optional identifier for parent web site.</param>
		private void ParseWebDirElement(XmlNode node, string componentId, string parentWeb)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;
			string application = null;
			string dirProperties = null;
			string path = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "DirProperties":
						dirProperties = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Path":
						path = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "WebSite":
						if (null != parentWeb)
						{
							this.core.OnMessage(WixErrors.WebSiteAttributeUnderWebSite(sourceLineNumbers, node.Name));
						}

						parentWeb = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						this.core.AddValidReference("IIsWebSite", parentWeb);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}
			if (null == path)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Path"));
			}
			if (null == parentWeb)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "WebSite"));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			this.core.AddValidReference("IIsWebDirProperties", dirProperties);
			this.core.AddValidReference("IIsWebApplication", application);

			// Reference ConfigureIIs since nothing will happen without it
			this.core.AddValidReference("CustomAction", "ConfigureIIs");

			Row row = this.core.CreateRow(sourceLineNumbers, "IIsWebDir");
			row[0] = id;
			row[1] = componentId;
			row[2] = parentWeb;
			row[3] = path;
			row[4] = dirProperties;
			row[5] = application;
		}

		/// <summary>
		/// Parses a web property element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="componentId">Identifier for parent component.</param>
		private void ParseWebPropertyElement(XmlNode node, string componentId)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;
			string value = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Value":
						value = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			switch (id)
			{
				case "ETagChangeNumber":
				case "MaxGlobalBandwidth":
					// Must specify a value for these
					if (null == value)
					{
						this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Value", "Id", id));
					}
					break;
				case "IIs5IsolationMode":
				case "LogInUTF8":
					// Can't specify a value for these
					if (null != value)
					{
						this.core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name, "Value", "Id", id));
					}
					break;
				default:
					this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name, "Id", id, "ETagChangeNumber", "IIs5IsolationMode", "LogInUTF8", "MaxGlobalBandwidth"));
					break;
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			// Reference ConfigureIIs since nothing will happen without it
			this.core.AddValidReference("CustomAction", "ConfigureIIs");

			Row row = this.core.CreateRow(sourceLineNumbers, "IIsProperty");
			row[0] = id;
			row[1] = componentId;
			row[2] = 0;
			row[3] = value;
		}

		/// <summary>
		/// Parses a web site element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="componentId">Optional identifier of parent component.</param>
		private void ParseWebSiteElement(XmlNode node, string componentId)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;
			string application = null;
			int attributes = 0;
			int connectionTimeout = CompilerCore.IntegerNotSet;
			string description = null;
			string directory = null;
			string dirProperties = null;
			string keyAddress = null;
			string log = null;
			int sequence = -1;
			int state = CompilerCore.IntegerNotSet;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "AutoStart":
						if (null == componentId)
						{
							this.core.OnMessage(WixErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
						}

						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							state = 2;
						}
						else if (state != 1)
						{
							state = 0;
						}
						break;
					case "ConfigureIfExists":
						if (null == componentId)
						{
							this.core.OnMessage(WixErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
						}

						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							attributes &= ~2;
						}
						else
						{
							attributes |= 2;
						}
						break;
					case "ConnectionTimeout":
						connectionTimeout = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						break;
					case "Description":
						description = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Directory":
						directory = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "DirProperties":
						if (null == componentId)
						{
							this.core.OnMessage(WixErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
						}

						dirProperties = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Sequence":
						sequence = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						break;
					case "StartOnInstall":
						if (null == componentId)
						{
							this.core.OnMessage(WixErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
						}

						// when state is set to 2 it implies 1, so don't set it to 1
						if (2 != state && YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							state = 1;
						}
						else if (2 != state)
						{
							state = 0;
						}
						break;
					case "WebApplication":
						if (null == componentId)
						{
							this.core.OnMessage(WixErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
						}

						application = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "WebLog":
						if (null == componentId)
						{
							this.core.OnMessage(WixErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
						}

						log = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}
			if (CompilerCore.IntegerNotSet != connectionTimeout && CompilerCore.IllegalInteger != connectionTimeout)
			{
				if (0 > connectionTimeout)
				{
					this.core.OnMessage(WixErrors.ConnectionTimeoutOutOfRange(sourceLineNumbers, node.Name, "ConnectionTimeout", connectionTimeout));
				}
			}
			if (null == description)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Description"));
			}
			if (null == directory && null != componentId)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Directory"));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					SourceLineNumberCollection childSourceLineNumbers = this.core.GetSourceLineNumbers(child);
					switch (child.LocalName)
					{
						case "CertificateRef":
							if (null == componentId)
							{
								this.core.OnMessage(WixErrors.IllegalElementWithoutComponent(childSourceLineNumbers, child.Name));
							}

							this.ParseCertificateRefElement(child, id);
							break;
						case "HttpHeader":
							if (null == componentId)
							{
								this.core.OnMessage(WixErrors.IllegalElementWithoutComponent(childSourceLineNumbers, child.Name));
							}

							this.ParseHttpHeaderElement(child, HttpHeaderParentType.WebSite, id);
							break;
						case "WebAddress":
							string address = this.ParseWebAddressElement(child, id);
							if (null == keyAddress)
							{
								keyAddress = address;
							}
							break;
						case "WebApplication":
							if (null == componentId)
							{
								this.core.OnMessage(WixErrors.IllegalElementWithoutComponent(childSourceLineNumbers, child.Name));
							}

							if (null != application)
							{
								this.core.OnMessage(WixErrors.WebApplicationAlreadySpecified(childSourceLineNumbers, node.Name));
							}

							application = this.ParseWebApplicationElement(child);
							break;
						case "WebDir":
							if (null == componentId)
							{
								this.core.OnMessage(WixErrors.IllegalElementWithoutComponent(childSourceLineNumbers, child.Name));
							}

							this.ParseWebDirElement(child, componentId, id);
							break;
						case "WebError":
							if (null == componentId)
							{
								this.core.OnMessage(WixErrors.IllegalElementWithoutComponent(childSourceLineNumbers, child.Name));
							}

							this.ParseWebErrorElement(child, WebErrorParentType.WebSite, id);
							break;
						case "WebFilter":
							if (null == componentId)
							{
								this.core.OnMessage(WixErrors.IllegalElementWithoutComponent(childSourceLineNumbers, child.Name));
							}

							this.ParseWebFilterElement(child, componentId, id);
							break;
						case "WebVirtualDir":
							if (null == componentId)
							{
								this.core.OnMessage(WixErrors.IllegalElementWithoutComponent(childSourceLineNumbers, child.Name));
							}

							this.ParseWebVirtualDirElement(child, componentId, id, null);
							break;
						default:
							this.core.UnexpectedElement(node, child);
							break;
					}
				}
			}

			this.core.AddValidReference("Directory", directory);
			this.core.AddValidReference("IIsWebApplication", application);
			this.core.AddValidReference("IIsWebDirProperties", dirProperties);
			this.core.AddValidReference("IIsWebLog", log);

			if (null != componentId)
			{
				// Reference ConfigureIIs since nothing will happen without it
				this.core.AddValidReference("CustomAction", "ConfigureIIs");
			}

			Row row = this.core.CreateRow(sourceLineNumbers, "IIsWebSite");
			row[0] = id;
			row[1] = componentId;
			row[2] = description;
			if (CompilerCore.IntegerNotSet != connectionTimeout)
			{
				row[3] = connectionTimeout;
			}
			row[4] = directory;
			if (CompilerCore.IntegerNotSet < state)
			{
				row[5] = state;
			}
			if (0 != attributes)
			{
				row[6] = attributes;
			}
			row[7] = keyAddress;
			row[8] = dirProperties;
			row[9] = application;
			row[10] = sequence;
			row[11] = log;
		}

		/// <summary>
		/// Parses web log element.
		/// </summary>
		/// <param name="node">Node to be parsed.</param>
		private void ParseWebLogElement(XmlNode node)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;
			string type = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Type":
						string typeValue = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						switch (typeValue)
						{
							case CompilerCore.IllegalEmptyAttributeValue:
								break;
							case "IIS":
								type = "Microsoft IIS Log File Format";
								break;
							case "NCSA":
								type = "NCSA Common Log File Format";
								break;
							case "none":
								type = "none";
								break;
							case "ODBC":
								type = "ODBC Logging";
								break;
							case "W3C":
								type = "W3C Extended Log File Format";
								break;
							default:
								this.core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name, "Type", typeValue, "IIS", "NCSA", "ODBC", "W3C"));
								break;
						}
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}
			if (null == type)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Type"));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			Row row = this.core.CreateRow(sourceLineNumbers, "IIsWebLog");
			row[0] = id;
			row[1] = type;
		}

		/// <summary>
		/// Parses a web service extension element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="componentId">Identifier for parent component.</param>
		private void ParseWebServiceExtensionElement(XmlNode node, string componentId)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;
			int attributes = 0;
			string description = null;
			string file = null;
			string group = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Allow":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							attributes |= 1;
						}
						else
						{
							attributes &= ~1;
						}
						break;
					case "Description":
						description = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "File":
						file = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Group":
						group = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "UIDeletable":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							attributes |= 2;
						}
						else
						{
							attributes &= ~2;
						}
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}
			if (null == file)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "File"));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			// Reference ConfigureIIs since nothing will happen without it
			this.core.AddValidReference("CustomAction", "ConfigureIIs");

			Row row = this.core.CreateRow(sourceLineNumbers, "IIsWebServiceExtension");
			row[0] = id;
			row[1] = componentId;
			row[2] = file;
			row[3] = description;
			row[4] = group;
			row[5] = attributes;
		}

		/// <summary>
		/// Parses a sql string element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="componentId">Identifier for parent component.</param>
		/// <param name="sqlDb">Optional database to execute string against.</param>
		private void ParseSqlStringElement(XmlNode node, string componentId, string sqlDb)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;
			int attributes = 0;
			int sequence = CompilerCore.IntegerNotSet;
			string sql = null;
			string user = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "ContinueOnError":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							attributes |= ScaSqlContinueOnError;
						}
						else
						{
							attributes &= ~ScaSqlContinueOnError;
						}
						break;
					case "ExecuteOnInstall":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							attributes |= ScaSqlExecuteOnInstall;
						}
						else
						{
							attributes &= ~ScaSqlExecuteOnInstall;
						}
						break;
					case "ExecuteOnReInstall":
					case "ExecuteOnReinstall":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							attributes |= ScaSqlExecuteOnReinstall;
						}
						else
						{
							attributes &= ~ScaSqlExecuteOnReinstall;
						}
						break;
					case "ExecuteOnUninstall":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							attributes |= ScaSqlExecuteOnUninstall;
						}
						else
						{
							attributes &= ~ScaSqlExecuteOnUninstall;
						}
						break;
					case "RollbackOnInstall":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							attributes |= (ScaSqlExecuteOnInstall | ScaSqlRollback);
						}
						else
						{
							attributes &= ~(ScaSqlExecuteOnInstall | ScaSqlRollback);
						}
						break;
					case "RollbackOnReinstall":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							attributes |= (ScaSqlExecuteOnReinstall | ScaSqlRollback);
						}
						else
						{
							attributes &= ~(ScaSqlExecuteOnReinstall | ScaSqlRollback);
						}
						break;
					case "RollbackOnUninstall":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							attributes |= (ScaSqlExecuteOnUninstall | ScaSqlRollback);
						}
						else
						{
							attributes &= ~(ScaSqlExecuteOnUninstall | ScaSqlRollback);
						}
						break;
					case "Sequence":
						sequence = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						break;
					case "SQL":
						sql = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "SqlDb":
						if (null != sqlDb)
						{
							this.core.OnMessage(WixErrors.IllegalAttributeWhenNested(sourceLineNumbers, node.Name, "SqlDb", "SqlDatabase"));
						}

						sqlDb = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						this.core.AddValidReference("SqlDatabase", sqlDb);
						break;
					case "User":
						user = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						this.core.AddValidReference("User", user);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}
			if (null == sql)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "SQL"));
			}
			if (null == sqlDb)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "SqlDb"));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			// Reference ConfigureSql since nothing will happen without it
			this.core.AddValidReference("CustomAction", "ConfigureSql");

			Row row = this.core.CreateRow(sourceLineNumbers, "SqlString");
			row[0] = id;
			row[1] = sqlDb;
			row[2] = componentId;
			row[3] = sql;
			row[4] = user;
			row[5] = attributes;
			if (CompilerCore.IntegerNotSet != sequence)
			{
				row[6] = sequence;
			}
		}

		/// <summary>
		/// Parses a sql script element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="componentId">Identifier for parent component.</param>
		/// <param name="sqlDb">Optional database to execute script against.</param>
		private void ParseSqlScriptElement(XmlNode node, string componentId, string sqlDb)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;
			int attributes = 0;
			string binary = null;
			int sequence = CompilerCore.IntegerNotSet;
			string user = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "BinaryKey":
						binary = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Sequence":
						sequence = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						break;
					case "SqlDb":
						if (null != sqlDb)
						{
							this.core.OnMessage(WixErrors.IllegalAttributeWhenNested(sourceLineNumbers, node.Name, attrib.Name, node.ParentNode.Name));
						}
						sqlDb = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "User":
						user = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;

					// Flag-setting attributes
					case "ContinueOnError":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							attributes |= ScaSqlContinueOnError;
						}
						else
						{
							attributes &= ~ScaSqlContinueOnError;
						}
						break;
					case "ExecuteOnInstall":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							attributes |= ScaSqlExecuteOnInstall;
						}
						else
						{
							attributes &= ~ScaSqlExecuteOnInstall;
						}
						break;
					case "ExecuteOnReInstall":
					case "ExecuteOnReinstall":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							attributes |= ScaSqlExecuteOnReinstall;
						}
						else
						{
							attributes &= ~ScaSqlExecuteOnReinstall;
						}
						break;
					case "ExecuteOnUninstall":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							attributes |= ScaSqlExecuteOnUninstall;
						}
						else
						{
							attributes &= ~ScaSqlExecuteOnUninstall;
						}
						break;
					case "RollbackOnInstall":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							attributes |= (ScaSqlExecuteOnInstall | ScaSqlRollback);
						}
						else
						{
							attributes &= ~(ScaSqlExecuteOnInstall | ScaSqlRollback);
						}
						break;
					case "RollbackOnReinstall":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							attributes |= (ScaSqlExecuteOnReinstall | ScaSqlRollback);
						}
						else
						{
							attributes &= ~(ScaSqlExecuteOnReinstall | ScaSqlRollback);
						}
						break;
					case "RollbackOnUninstall":
						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							attributes |= (ScaSqlExecuteOnUninstall | ScaSqlRollback);
						}
						else
						{
							attributes &= ~(ScaSqlExecuteOnUninstall | ScaSqlRollback);
						}
						break;

					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}

			if (null == sqlDb)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "SqlDb"));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					switch (child.LocalName)
					{
						case "Binary":
							if (null != binary)
							{
								this.core.OnMessage(WixErrors.SingleBinaryRequired(sourceLineNumbers, node.Name));
							}

							binary = this.ParseBinaryOrIconElement(child, BinaryType.Binary);
							break;
						default:
							this.core.UnexpectedElement(node, child);
							break;
					}
				}
			}

			if (null == binary)
			{
				this.core.OnMessage(WixErrors.SingleBinaryRequired(sourceLineNumbers, node.Name));
			}
			else
			{
				this.core.AddValidReference("Binary", binary);
			}
			this.core.AddValidReference("User", user);

			// Reference ConfigureSql since nothing will happen without it
			this.core.AddValidReference("CustomAction", "ConfigureSql");

			Row row = this.core.CreateRow(sourceLineNumbers, "SqlScript");
			row[0] = id;
			row[1] = sqlDb;
			row[2] = componentId;
			row[3] = binary;
			row[4] = user;
			row[5] = attributes;
			if (CompilerCore.IntegerNotSet != sequence)
			{
				row[6] = sequence;
			}
		}

		/// <summary>
		/// Parses a sql file specification element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <returns>Identifier of sql file specification.</returns>
		private string ParseSqlFileSpecElement(XmlNode node)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;
			string fileName = null;
			string growthSize = null;
			string maxSize = null;
			string name = null;
			string size = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Name":
						name = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Filename":
						fileName = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Size":
						size = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "MaxSize":
						maxSize = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "GrowthSize":
						growthSize = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}
			if (null == fileName)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Filename"));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			Row row = this.core.CreateRow(sourceLineNumbers, "SqlFileSpec");
			row[0] = id;
			row[1] = name;
			row[2] = fileName;
			if (null != size)
			{
				row[3] = size;
			}
			if (null != maxSize)
			{
				row[4] = maxSize;
			}
			if (null != growthSize)
			{
				row[5] = growthSize;
			}

			return id;
		}

		/// <summary>
		/// Parses a sql database element
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="componentId">Identifier for parent component.</param>
		private void ParseSqlDatabaseElement(XmlNode node, string componentId)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;
			int attributes = 0;
			string database = null;
			string fileSpec = null;
			string instance = null;
			string logFileSpec = null;
			string server = null;
			string user = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "ConfirmOverwrite":
						if (null == componentId)
						{
							this.core.OnMessage(WixErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
						}

						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							attributes |= 32;
						}
						else
						{
							attributes &= ~32;
						}
						break;
					case "ContinueOnError":
						if (null == componentId)
						{
							this.core.OnMessage(WixErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
						}

						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							attributes |= ScaDbContinueOnError;
						}
						else
						{
							attributes &= ~ScaDbContinueOnError;
						}
						break;
					case "CreateOnInstall":
						if (null == componentId)
						{
							this.core.OnMessage(WixErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
						}

						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							attributes |= ScaDbCreateOnInstall;
						}
						else
						{
							attributes &= ~ScaDbCreateOnInstall;
						}
						break;
					case "CreateOnReinstall":
						if (null == componentId)
						{
							this.core.OnMessage(WixErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
						}

						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							attributes |= ScaDbCreateOnReinstall;
						}
						else
						{
							attributes &= ~ScaDbCreateOnReinstall;
						}
						break;
					case "CreateOnUninstall":
						if (null == componentId)
						{
							this.core.OnMessage(WixErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
						}

						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							attributes |= ScaDbCreateOnUninstall;
						}
						else
						{
							attributes &= ~ScaDbCreateOnUninstall;
						}
						break;
					case "Database":
						database = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "DropOnInstall":
						if (null == componentId)
						{
							this.core.OnMessage(WixErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
						}

						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							attributes |= ScaDbDropOnInstall;
						}
						else
						{
							attributes &= ~ScaDbDropOnInstall;
						}
						break;
					case "DropOnReinstall":
						if (null == componentId)
						{
							this.core.OnMessage(WixErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
						}

						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							attributes |= ScaDbDropOnReinstall;
						}
						else
						{
							attributes &= ~ScaDbDropOnReinstall;
						}
						break;

					case "DropOnUninstall":
						if (null == componentId)
						{
							this.core.OnMessage(WixErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
						}

						if (YesNoType.Yes == this.core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
						{
							attributes |= ScaDbDropOnUninstall;
						}
						else
						{
							attributes &= ~ScaDbDropOnUninstall;
						}
						break;
					case "Instance":
						instance = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Server":
						server = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "User":
						user = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}
			if (null == database)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Database"));
			}
			if (null == server)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Server"));
			}

			if (0 == attributes && null != componentId)
			{
				this.core.OnMessage(WixErrors.OneOfAttributesRequiredUnderComponent(sourceLineNumbers, node.Name, "CreateOnInstall", "CreateOnUninstall", "DropOnInstall", "DropOnUninstall"));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					SourceLineNumberCollection childSourceLineNumbers = this.core.GetSourceLineNumbers(child);
					switch (child.LocalName)
					{
						case "SqlScript":
							if (null == componentId)
							{
								this.core.OnMessage(WixErrors.IllegalElementWithoutComponent(childSourceLineNumbers, child.Name));
							}

							this.ParseSqlScriptElement(child, componentId, id);
							break;
						case "SqlString":
							if (null == componentId)
							{
								this.core.OnMessage(WixErrors.IllegalElementWithoutComponent(childSourceLineNumbers, child.Name));
							}

							this.ParseSqlStringElement(child, componentId, id);
							break;
						case "SqlFileSpec":
							if (null == componentId)
							{
								this.core.OnMessage(WixErrors.IllegalElementWithoutComponent(childSourceLineNumbers, child.Name));
							}

							fileSpec = this.ParseSqlFileSpecElement(child);
							break;
						case "SqlLogFileSpec":
							if (null == componentId)
							{
								this.core.OnMessage(WixErrors.IllegalElementWithoutComponent(childSourceLineNumbers, child.Name));
							}

							logFileSpec = this.ParseSqlFileSpecElement(child);
							break;
						default:
							this.core.UnexpectedElement(node, child);
							break;
					}
				}
			}

			this.core.AddValidReference("User", user);

			if (null != componentId)
			{
				// Reference ConfigureSql since nothing will happen without it
				this.core.AddValidReference("CustomAction", "ConfigureSql");
			}

			Row row = this.core.CreateRow(sourceLineNumbers, "SqlDatabase");
			row[0] = id;
			row[1] = server;
			row[2] = instance;
			row[3] = database;
			row[4] = componentId;
			row[5] = user;
			row[6] = fileSpec;
			row[7] = logFileSpec;
			if (0 != attributes)
			{
				row[8] = attributes;
			}
		}

		/// <summary>
		/// Parses a verb element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		/// <param name="extension">Extension verb is releated to.</param>
		/// <param name="progId">Optional progId for extension.</param>
		/// <param name="componentId">Identifier for parent component.</param>
		/// <param name="advertise">Flag if extension is advertised.</param>
		private void ParseVerbElement(XmlNode node, string extension, string progId, string componentId, YesNoType advertise)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			string id = null;
			string target = null;
			string command = null;
			string argument = null;
			int sequence = CompilerCore.IntegerNotSet;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "Id":
						id = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Argument":
						argument = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Command":
						command = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					case "Sequence":
						sequence = this.core.GetAttributeIntegerValue(sourceLineNumbers, attrib);
						break;
					case "Target":
						target = this.core.GetAttributeValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}

			if (null == id)
			{
				this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
			}

			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					this.core.UnexpectedElement(node, child);
				}
			}

			if (YesNoType.Yes == advertise)
			{
				if (null != target)
				{
					this.core.OnMessage(WixErrors.IllegalAttributeWhenAdvertised(sourceLineNumbers, node.Name, "Target"));
				}

				Row row = this.core.CreateRow(sourceLineNumbers, "Verb");
				row[0] = extension;
				row[1] = id;
				if (CompilerCore.IntegerNotSet != sequence)
				{
					row[2] = sequence;
				}
				row[3] = command;
				row[4] = argument;
			}
			else if (YesNoType.No == advertise)
			{
				if (null == target)
				{
					this.core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Target", "Advertise", "no"));
				}

				if (null != argument)
				{
					target = String.Concat(target, " ", argument);
				}
				if (null != progId)
				{
					if (null != command)
					{
						this.AddRegistryKey(sourceLineNumbers, null, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat(progId, "\\shell\\", id), String.Empty, command, componentId);
					}
					this.AddRegistryKey(sourceLineNumbers, null, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat(progId, "\\shell\\", id, "\\command"), String.Empty, target, componentId);
				}
				else
				{
					if (null != command)
					{
						this.AddRegistryKey(sourceLineNumbers, null, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat(".", extension, "\\shell\\", id), String.Empty, command, componentId);
					}
					this.AddRegistryKey(sourceLineNumbers, null, MsiInterop.MsidbRegistryRootClassesRoot, String.Concat(".", extension, "\\shell\\", id, "\\command"), String.Empty, target, componentId);
				}
			}
		}

		/// <summary>
		/// Parses a Wix element.
		/// </summary>
		/// <param name="node">Element to parse.</param>
		private void ParseWixElement(XmlNode node)
		{
			SourceLineNumberCollection sourceLineNumbers = this.core.GetSourceLineNumbers(node);
			Version requiredVersion = null;

			foreach (XmlAttribute attrib in node.Attributes)
			{
				switch (attrib.LocalName)
				{
					case "RequiredVersion":
						requiredVersion = this.core.GetAttributeVersionValue(sourceLineNumbers, attrib);
						break;
					default:
						this.core.UnexpectedAttribute(sourceLineNumbers, attrib);
						break;
				}
			}
			if (null != requiredVersion && CompilerCore.IllegalVersion != requiredVersion)
			{
				Version currentVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
				if (0 < requiredVersion.CompareTo(currentVersion))
				{
					this.core.OnMessage(WixErrors.InsufficientVersion(sourceLineNumbers, currentVersion, requiredVersion));
				}
			}

			// process all the sections
			foreach (XmlNode child in node.ChildNodes)
			{
				if (XmlNodeType.Element == child.NodeType)
				{
					switch (child.LocalName)
					{
						case "Fragment":
							this.ParseFragmentElement(child);
							break;
						case "Module":
							this.ParseModuleElement(child);
							break;
						case "PatchCreation":
							this.ParsePatchCreationElement(child);
							break;
						case "Product":
							this.ParseProductElement(child);
							break;
						default:
							this.core.UnexpectedElement(node, child);
							break;
					}
				}
			}
		}

		/// <summary>
		/// Attempts to use an extension to parse the attribute.
		/// </summary>
		/// <param name="sourceLineNumbers">Current line number for element.</param>
		/// <param name="element">Element containing attribute to be parsed.</param>
		/// <param name="attribute">Attribute to be parsed.</param>
		/// <returns>True if the attribute is handled by an extension.</returns>
		private bool TryExtensionParseForAttribute(SourceLineNumberCollection sourceLineNumbers, XmlElement element, XmlAttribute attribute)
		{
			CompilerExtension extension = this.FindExtension(attribute.NamespaceURI);
			if (null != extension)
			{
				extension.ParseAttribute(sourceLineNumbers, element, attribute);
			}

			return null != extension;
		}

		/// <summary>
		/// Attempts to use an extension to parse the element.
		/// </summary>
		/// <param name="sourceLineNumbers">Current line number for element.</param>
		/// <param name="parentElement">Element containing element to be parsed.</param>
		/// <param name="element">Element to be parsed.</param>
		/// <returns>True if the element is handled by an extension.</returns>
		private bool TryExtensionParseForElement(SourceLineNumberCollection sourceLineNumbers, XmlElement parentElement, XmlElement element)
		{
			CompilerExtension extension = this.FindExtension(element.NamespaceURI);
			if (null != extension)
			{
				extension.ParseElement(sourceLineNumbers, parentElement, element);
			}

			return null != extension;
		}

		/// <summary>
		/// Finds a compiler extension by namespace URI.
		/// </summary>
		/// <param name="namespaceUri">URI for namespace the extension supports.</param>
		/// <returns>Found compiler extension or null if nothing matches namespace URI.</returns>
		private CompilerExtension FindExtension(string namespaceUri)
		{
			return (CompilerExtension)this.extensions[namespaceUri];
		}

		/// <summary>
		/// Validate the document against the standard WiX schema and any extensions.
		/// </summary>
		/// <param name="document">The xml document to validate.</param>
		private void ValidateDocument(XmlDocument document)
		{
			// if we haven't loaded the schemas yet, do that now
			if (null == this.schemas)
			{
				this.schemas = new XmlSchemaCollection();

				// always load the WiX schema first
				Assembly assembly = Assembly.GetExecutingAssembly();
				XmlReader schemaReader = null;
				try
				{
					schemaReader = Common.GetXmlFromEmbeddedStream(assembly, "Microsoft.Tools.WindowsInstallerXml.Xsd.wix.xsd");
					this.schemas.Add("http://schemas.microsoft.com/wix/2003/01/wi", schemaReader);
				}
				finally
				{
					if (null != schemaReader)
					{
						schemaReader.Close();
					}
				}

				// add all the extension schemas
				foreach (SchemaExtension extension in this.extensions.Values)
				{
					this.schemas.Add(extension.Schema);
				}
			}

			// write the document to a string for validation
			StringWriter xml = new StringWriter();
			XmlTextWriter writer = null;
			try
			{
				writer = new XmlTextWriter(xml);
				document.WriteTo(writer);
			}
			catch (ArgumentException)
			{
				this.core.OnMessage(WixErrors.SP1ProbablyNotInstalled());
			}
			finally
			{
				if (null != writer)
				{
					writer.Close();
				}
			}

			// validate the xml string (and thus the document)
			SourceLineNumberCollection sourceLineNumbers = null;
			XmlParserContext context = new XmlParserContext(null, null, null, XmlSpace.None);
			XmlValidatingReader validatingReader = null;
			try
			{
				validatingReader = new XmlValidatingReader(xml.ToString(), XmlNodeType.Document, context);
				validatingReader.Schemas.Add(this.schemas);

				while (validatingReader.Read())
				{
					if (XmlNodeType.ProcessingInstruction == validatingReader.NodeType && Preprocessor.LineNumberElementName == validatingReader.Name)
					{
						sourceLineNumbers = new SourceLineNumberCollection(validatingReader.Value);
					}
				}
			}
			catch (XmlSchemaException e)
			{
				string message = e.Message.Replace("http://schemas.microsoft.com/wix/2003/01/wi:", String.Empty);

				// find the index of the erroneous line information and chop it off.
				int length = message.IndexOf(" An error occurred at");
				if (-1 == length)
				{
					length = message.Length; // couldn't find the erroneous info, so just show the whole message.
				}

				this.core.OnMessage(WixErrors.SchemaValidationFailed(sourceLineNumbers, message.Substring(0, length)));
			}
			finally
			{
				if (null != validatingReader)
				{
					validatingReader.Close();
				}
			}
		}
	}
}
