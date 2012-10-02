//-------------------------------------------------------------------------------------------------
// <copyright file="Decompiler.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
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
// The dark decompiler application.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;
	using System.Collections;
	using System.Collections.Specialized;
	using System.Globalization;
	using System.IO;
	using System.Xml;
	using System.Text;
	using System.Text.RegularExpressions;
	using Microsoft.Tools.WindowsInstallerXml.Cab;
	using Microsoft.Tools.WindowsInstallerXml.Msi;
	using Microsoft.Tools.WindowsInstallerXml.Msi.Interop;

	/// <summary>
	/// Decompiler object.
	/// </summary>
	public class Decompiler : IExtensionMessageHandler
	{
		/// <summary>
		/// messages exchanged with the extensions
		/// </summary>
		private ExtensionMessages extensionMessages;

		/// <summary>
		/// List of extensions for the decompiler.
		/// </summary>
		private ArrayList extensionList;

		/// <summary>
		/// Decompiler core. Utility methods and so on.
		/// </summary>
		private DecompilerCore core;

		/// <summary>
		/// list of standard actions
		/// </summary>
		private ActionTable standardActions;

		/// <summary>
		/// list of table definitions
		/// </summary>
		private TableDefinitionCollection tableDefinitions;

		/// <summary>
		/// database to process
		/// </summary>
		private Database inputDatabase;

		/// <summary>
		/// skip the ui elements
		/// </summary>
		private bool skipUI;

		/// <summary>
		/// skip the vsi specific elements
		/// </summary>
		private bool skipVSI;

		/// <summary>
		/// skip the InstallShield specific elements
		/// </summary>
		private bool skipInstallShield;

		/// <summary>
		/// only produce UI elements
		/// </summary>
		private bool processUIOnly;

		/// <summary>
		/// skip sequence tables
		/// </summary>
		private bool skipSequenceTables;

		/// <summary>
		/// explicit sequence tables
		/// </summary>
		private bool explicitSequenceTables;

		/// <summary>
		/// skip summary tables
		/// </summary>
		private bool skipSummaryInfo;

		/// <summary>
		/// export binaries
		/// </summary>
		private bool exportBinaries;

		/// <summary>
		/// binaries exported
		/// </summary>
		private bool binariesExported;

		/// <summary>
		/// icons exported
		/// </summary>
		private bool iconsExported;

		/// <summary>
		/// processing a merge module
		/// </summary>
		private bool processingModule;

		/// <summary>
		/// processing a fragment
		/// </summary>
		private bool processingFragment;

		/// <summary>
		/// interpolate registry entries into advertising table entries
		/// </summary>
		private bool interpolateRegistry;

		/// <summary>
		/// build library of fragments rather than single output file
		/// </summary>
		private bool generateFragments;

		/// <summary>
		/// keep the empty tables
		/// </summary>
		private bool keepEmptyTables;

		/// <summary>
		/// add comments
		/// </summary>
		private bool addComments;

		/// <summary>
		/// directories we've already processed
		/// </summary>
		private Hashtable processedDirectories = new Hashtable();

		/// <summary>
		/// components we've already processed
		/// </summary>
		private Hashtable processedComponents = new Hashtable();

		/// <summary>
		/// classes in registry we've already processed
		/// </summary>
		private Hashtable processedClasses = new Hashtable();

		/// <summary>
		/// appIds in registry we've already processed
		/// </summary>
		private Hashtable processedAppIds = new Hashtable();

		/// <summary>
		/// classes in registry we've already processed
		/// </summary>
		private Hashtable processedAdvertisedClasses = new Hashtable();

		/// <summary>
		/// appIds in registry we've already processed
		/// </summary>
		private Hashtable processedAdvertisedAppIds = new Hashtable();

		/// <summary>
		/// list of admin properties
		/// </summary>
		private Hashtable adminPropNames = new Hashtable();

		/// <summary>
		/// list of hidden properties
		/// </summary>
		private Hashtable hiddenPropNames = new Hashtable();

		/// <summary>
		/// list of secure properties
		/// </summary>
		private Hashtable securePropNames = new Hashtable();

		/// <summary>
		/// base path to export files
		/// </summary>
		private string exportBasePath;

		/// <summary>
		/// base path to export files
		/// </summary>
		private string imageBasePath;

		/// <summary>
		/// non-UI properties
		/// </summary>
		private Hashtable nonuiproperties = new Hashtable();

		/// <summary>
		/// UI Control properties we've processed
		/// </summary>
		private Hashtable uicontrolProperties = new Hashtable();

		/// <summary>
		/// binary rows we've processed
		/// </summary>
		private Hashtable binariesProcessed = new Hashtable();

		/// <summary>
		/// binary rows we've processed
		/// </summary>
		private Hashtable customActionsProcessed = new Hashtable();

		/// <summary>
		/// binary rows we've processed
		/// </summary>
		private Hashtable binariesIdProcessed = new Hashtable();

		/// <summary>
		/// appSearch properties we've processed
		/// </summary>
		private Hashtable appSearchProperties = new Hashtable();

		/// <summary>
		/// fileSearch signatures we've processed
		/// </summary>
		private Hashtable fileSearchSignatures = new Hashtable();

		/// <summary>
		/// directory search properties we've processed
		/// </summary>
		private Hashtable directorySearchSignatures = new Hashtable();

		/// <summary>
		/// registrySearch signatures we've processed
		/// </summary>
		private Hashtable registrySearchSignatures = new Hashtable();

		/// <summary>
		/// search signatures properties we've processed
		/// </summary>
		private Hashtable searchSignatures = new Hashtable();

		/// <summary>
		/// directories we've already processed
		/// </summary>
		private Hashtable openFragments = new Hashtable();

		/// <summary>
		/// AdminExecuteSequence we've already processed
		/// </summary>
		private Hashtable unprocessedAdminExecuteSequence = new Hashtable();

		/// <summary>
		/// AdminUISequence we've already processed
		/// </summary>
		private Hashtable unprocessedAdminUISequence = new Hashtable();

		/// <summary>
		/// AdvtExecuteSequence we've already processed
		/// </summary>
		private Hashtable unprocessedAdvtExecuteSequence = new Hashtable();

		/// <summary>
		/// InstallExecuteSequence we've already processed
		/// </summary>
		private Hashtable unprocessedInstallExecuteSequence = new Hashtable();

		/// <summary>
		/// InstallUISequence we've already processed
		/// </summary>
		private Hashtable unprocessedInstallUISequence = new Hashtable();

		/// <summary>
		/// SFPCatalog entries we've processed
		/// </summary>
		private Hashtable sfpCatalogProcessed = new Hashtable();

		/// <summary>
		/// duplicateFile entries we've processed
		/// </summary>
		private Hashtable duplicateFileProcessed = new Hashtable();

		/// <summary>
		/// duplicateFile entries we've processed
		/// </summary>
		private Hashtable odbcAttributeProcessed = new Hashtable();

		/// <summary>
		/// duplicateFile entries we've processed
		/// </summary>
		private Hashtable odbcDataSourceProcessed = new Hashtable();

		/// <summary>
		/// duplicateFile entries we've processed
		/// </summary>
		private Hashtable odbcDriverProcessed = new Hashtable();

		/// <summary>
		/// duplicateFile entries we've processed
		/// </summary>
		private Hashtable odbcSourceAttributeProcessed = new Hashtable();

		/// <summary>
		/// duplicateFile entries we've processed
		/// </summary>
		private Hashtable odbcTranslatorProcessed = new Hashtable();

		/// <summary>
		/// track if there's been an error while processing
		/// </summary>
		private bool foundError;

		/// <summary>
		/// output location for generated source
		/// </summary>
		private string outputFolder;

		/// <summary>
		/// root location where source files are kept
		/// </summary>
		private string imageFolder;

		/// <summary>
		/// extract location for CABs
		/// </summary>
		private string extractFolder;

		/// <summary>
		/// for merge modules, tracks the merge mudule GUID with dashes converted to underscores
		/// </summary>
		private string moduleIdUnderscored = "";

		/// <summary>
		/// Should code clean up after itself?
		/// </summary>
		private bool tidy;

		/// <summary>
		/// cache of diskIds and sequences
		/// </summary>
		private SortedList lastSequence;

		/// <summary>
		/// does the summary information stream say this is compressed
		/// </summary>
		private bool summaryInfoCompressed;

		/// <summary>
		/// Construct Decompiler object
		/// </summary>
		public Decompiler()
		{
			this.foundError = false;
			this.extensionMessages = new ExtensionMessages(this);
			this.extensionList = new ArrayList();
			this.tidy = true;
			this.lastSequence = new SortedList();
		}

		/// <summary>
		/// Construct Decompiler object with a set of table definitions
		/// </summary>
		/// <param name="tableDefinitions">table definitions.</param>
		public Decompiler(TableDefinitionCollection tableDefinitions)
		{
			this.tableDefinitions = tableDefinitions;
		}

		/// <summary>
		/// Event for messages.
		/// </summary>
		public event MessageEventHandler Message;

		/// <summary>
		/// Package Property Type
		/// </summary>
		private enum PropertyType
		{
			/// <summary>Ignore</summary>
			Ignore,

			/// <summary>Package</summary>
			Package,

			/// <summary>Product</summary>
			Product,

			/// <summary>UI</summary>
			UI,
		}

		/// <summary>
		/// Binary Type
		/// </summary>
		private enum BinaryType
		{
			/// <summary>Binary</summary>
			Binary,

			/// <summary>Icon</summary>
			Icon,
		}

		/// <summary>
		/// SkipUI accessor
		/// </summary>
		/// <value>true if skip ui.</value>
		public bool SkipUI
		{
			get { return this.skipUI; }
			set { this.skipUI = value; }
		}

		/// <summary>
		/// skipVSI accessor
		/// </summary>
		/// <value>true if skip VSI.</value>
		public bool SkipVSI
		{
			get { return this.skipVSI; }
			set { this.skipVSI = value; }
		}

		/// <summary>
		/// skipInstallShield accessor
		/// </summary>
		/// <value>true if skip InstallShield.</value>
		public bool SkipInstallShield
		{
			get { return this.skipInstallShield; }
			set { this.skipInstallShield = value; }
		}

		/// <summary>
		/// ProcessUIOnly accessor
		/// </summary>
		/// <value>true if process ui only.</value>
		public bool ProcessUIOnly
		{
			get { return this.processUIOnly; }
			set { this.processUIOnly = value; }
		}

		/// <summary>
		/// SkipSequenceTables accessor
		/// </summary>
		/// <value>true if skip sequence tables.</value>
		public bool SkipSequenceTables
		{
			get { return this.skipSequenceTables; }
			set { this.skipSequenceTables = value; }
		}

		/// <summary>
		/// ExplicitSequenceTables accessor
		/// </summary>
		/// <value>true if explicit sequence tables.</value>
		public bool ExplicitSequenceTables
		{
			get { return this.explicitSequenceTables; }
			set { this.explicitSequenceTables = value; }
		}

		/// <summary>
		/// SkipSummaryInfo accessor
		/// </summary>
		/// <value>true if skip summary information.</value>
		public bool SkipSummaryInfo
		{
			get { return this.skipSummaryInfo; }
			set { this.skipSummaryInfo = value; }
		}

		/// <summary>
		/// ExportBinaries accessor
		/// </summary>
		/// <value>true if export binaries.</value>
		public bool ExportBinaries
		{
			get { return this.exportBinaries; }
			set { this.exportBinaries = value; }
		}

		/// <summary>
		/// ExportBasePath accessor
		/// </summary>
		/// <value>base path to which the binaries will be exported.</value>
		public string ExportBasePath
		{
			get { return this.exportBasePath; }
			set { this.exportBasePath = value; }
		}

		/// <summary>
		/// imageBasePath accessor
		/// </summary>
		/// <value>base image path to from which the binaries will be extracted.</value>
		public string ImageBasePath
		{
			get { return this.imageBasePath; }
			set { this.imageBasePath = value; }
		}

		/// <summary>
		/// IsMergeModule accessor
		/// </summary>
		/// <value>true if is merge module.</value>
		public bool IsMergeModule
		{
			get { return this.processingModule; }
			set { this.processingModule = value; }
		}

		/// <summary>
		/// IsFragmentContainer accessor
		/// </summary>
		/// <value>true if is fragment container.</value>
		public bool IsFragmentContainer
		{
			get { return this.processingFragment; }
			set { this.processingFragment = value; }
		}

		/// <summary>
		/// InterpolateRegistry accessor
		/// </summary>
		/// <value>true if is merge module.</value>
		public bool InterpolateRegistry
		{
			get { return this.interpolateRegistry; }
			set { this.interpolateRegistry = value; }
		}

		/// <summary>
		/// InterpolateRegistry accessor
		/// </summary>
		/// <value>true if is merge module.</value>
		public bool GenerateFragments
		{
			get { return this.generateFragments; }
			set { this.generateFragments = value; }
		}

		/// <summary>
		/// KeepEmptyTables accessor
		/// </summary>
		/// <value>true if is merge module.</value>
		public bool KeepEmptyTables
		{
			get { return this.keepEmptyTables; }
			set { this.keepEmptyTables = value; }
		}

		/// <summary>
		/// AddComments accessor
		/// </summary>
		/// <value>true you want comments.</value>
		public bool AddComments
		{
			get { return this.addComments; }
			set { this.addComments = value; }
		}

		/// <summary>
		/// TableDefinitionCollection accessor
		/// </summary>
		/// <value>sets tabledefinitions to a user specified tabledefinitionscollection.</value>
		public TableDefinitionCollection TableDefinitions
		{
			get { return this.tableDefinitions; }
			set { this.tableDefinitions = value; }
		}

		/// <summary>
		/// Tidy accessor
		/// </summary>
		/// <value>sets the tidy boolean.</value>
		public bool Tidy
		{
			get { return this.tidy; }
			set { this.tidy = value; }
		}

		/// <summary>
		/// main entry point
		/// </summary>
		/// <param name="inputPath">input path.</param>
		/// <param name="outputPath">output path.</param>
		public void Decompile(string inputPath, string outputPath)
		{
			this.standardActions = Common.GetStandardActions();

			if (null == inputPath)
			{
				throw new ArgumentNullException("inputPath", "Input file must be specified.");
			}

			if (null == outputPath)
			{
				throw new ArgumentNullException("outputPath", "Output path must be specified.");
			}

			if (!File.Exists(inputPath))
			{
				throw new FileNotFoundException("Cannot find file to decompile.", inputPath);
			}

			if (!Path.IsPathRooted(inputPath))
			{
				inputPath = Path.GetFullPath(inputPath);
			}

			if (!Path.IsPathRooted(outputPath))
			{
				outputPath = Path.GetFullPath(outputPath);
			}

			XmlTextWriter writer = null;

			try
			{
				// remember the output folder and open the database
				this.outputFolder = Path.GetDirectoryName(outputPath);
				if (0 == this.outputFolder.Length)
				{
					this.outputFolder = System.Environment.CurrentDirectory;
				}

				if (null == this.exportBasePath || 0 == this.exportBasePath.Length)
				{
					this.exportBasePath = this.outputFolder;
				}
				else if (!Path.IsPathRooted(this.exportBasePath))
				{
					this.exportBasePath = Path.GetFullPath(Path.Combine(this.outputFolder, this.exportBasePath));
				}

				// create a temporary directory
				this.extractFolder = Path.Combine(this.exportBasePath, "extract");
				Directory.CreateDirectory(this.extractFolder);

				this.imageFolder = Path.GetDirectoryName(inputPath);
				if (null == this.imageBasePath || 0 == this.imageBasePath.Length)
				{
					this.imageBasePath = this.imageFolder;
				}
				else if (!Path.IsPathRooted(this.imageBasePath))
				{
					this.imageBasePath = Path.GetFullPath(Path.Combine(this.imageFolder, this.imageBasePath));
				}

				if (!Directory.Exists(this.exportBasePath))
				{
					Directory.CreateDirectory(this.exportBasePath);
				}

				using (this.inputDatabase = new Database(inputPath, OpenDatabase.ReadOnly))
				{
					using (MemoryStream memoryStream = new MemoryStream())
					{
						using (StreamWriter streamWriter = new StreamWriter(memoryStream, Encoding.UTF8))
						{
							writer = new XmlTextWriter(streamWriter);

							// use indenting for readability
							writer.Formatting = Formatting.Indented;

							this.core = new DecompilerCore(writer, this.Message);
							this.core.AddComments = this.addComments;

							// process the reader into the writer
							try
							{
								foreach (DecompilerExtension extension in this.extensionList)
								{
									extension.Core = this.core;

									extension.Messages = this.extensionMessages;
									extension.InitializeDecompile();
								}

								// do the actual processing to an internal buffer
								this.ProcessProductElement(true);
							}

							// throw an exception
							catch (XmlException e)
							{
								// todo: throw a real exception
								throw e;
							}

							// if no error was found, save the result to the specified file
							if (!this.foundError)
							{
								writer.Flush();

								using (FileStream fileStream = File.Create(outputPath))
								{
									memoryStream.WriteTo(fileStream);
								}
							}
						}
					}
				}
			}
			finally
			{
				if (null != writer)
				{
					writer.Close();
					writer = null;
				}

				foreach (XmlWriter fragmentWriter in this.openFragments.Values)
				{
					fragmentWriter.Close();
				}

				this.inputDatabase = null;
				this.outputFolder = null;

				if (!this.tidy)
				{
					Console.WriteLine("Tidy set to false; files can be found at '{0}'.", this.extractFolder);
				}
				else
				{
					this.DeleteTempFiles(this.extractFolder);
				}
			}
		}

		/// <summary>
		/// Adds an extension to the preprocessor.
		/// </summary>
		/// <param name="extension">preprocessor extension to add to preprocessor.</param>
		public void AddExtension(DecompilerExtension extension)
		{
			extension.Messages = this.extensionMessages;
			this.extensionList.Add(extension);
		}

		/// <summary>
		/// Sends an error to the message delegate if there is one.
		/// </summary>
		/// <param name="sourceLineNumbers">Source line numbers.</param>
		/// <param name="errorLevel">Level of the error message.</param>
		/// <param name="errorMessage">Error message string.</param>
		public void OnExtensionError(SourceLineNumberCollection sourceLineNumbers, ErrorLevel errorLevel, string errorMessage)
		{
			this.OnMessage(WixErrors.PreprocessorExtensionError(sourceLineNumbers, errorLevel, errorMessage));
		}

		/// <summary>
		/// Sends a warning to the message delegate if there is one.
		/// </summary>
		/// <param name="sourceLineNumbers">Source line numbers.</param>
		/// <param name="warningLevel">Level of the warning message.</param>
		/// <param name="warningMessage">Warning message string.</param>
		public void OnExtensionWarning(SourceLineNumberCollection sourceLineNumbers, WarningLevel warningLevel, string warningMessage)
		{
			this.OnMessage(WixWarnings.PreprocessorExtensionWarning(sourceLineNumbers, warningLevel, warningMessage));
		}

		/// <summary>
		/// Sends an error to the message delegate if there is one.
		/// </summary>
		/// <param name="sourceLineNumbers">Source line numbers.</param>
		/// <param name="verboseLevel">Level of the verbose message.</param>
		/// <param name="verboseMessage">Verbose message string.</param>
		public void OnExtensionVerbose(SourceLineNumberCollection sourceLineNumbers, VerboseLevel verboseLevel, string verboseMessage)
		{
			this.OnMessage(WixVerboses.PreprocessorExtensionVerbose(sourceLineNumbers, verboseLevel, verboseMessage));
		}

		/// <summary>
		/// Persists an MSI in an XML format.
		/// </summary>
		/// <param name="writeDocumentElements">If true, will write the document elements.</param>
		private void ProcessProductElement(bool writeDocumentElements)
		{
			this.core.SetCoveredTable("_Columns", "BaseDecompiler");
			this.core.SetCoveredTable("_Tables", "BaseDecompiler");
			this.core.SetCoveredTable("_Validation", "BaseDecompiler");

			this.core.SetCoveredTable("ActionText", "BaseDecompiler");
			this.core.SetCoveredTable("AdminExecuteSequence", "BaseDecompiler");
			this.core.SetCoveredTable("AdminUISequence", "BaseDecompiler");
			this.core.SetCoveredTable("AdvtExecuteSequence", "BaseDecompiler");
			this.core.SetCoveredTable("AppId", "BaseDecompiler");
			this.core.SetCoveredTable("AppSearch", "BaseDecompiler");

			this.core.SetCoveredTable("BBControl", "BaseDecompiler");
			this.core.SetCoveredTable("Billboard", "BaseDecompiler");
			this.core.SetCoveredTable("Binary", "BaseDecompiler");
			this.core.SetCoveredTable("BindImage", "BaseDecompiler");

			this.core.SetCoveredTable("CCPSearch", "BaseDecompiler");
			this.core.SetCoveredTable("CheckBox", "BaseDecompiler");
			this.core.SetCoveredTable("Class", "BaseDecompiler");
			this.core.SetCoveredTable("ComboBox", "BaseDecompiler");
			this.core.SetCoveredTable("CompLocator", "BaseDecompiler");
			this.core.SetCoveredTable("Complus", "BaseDecompiler");
			this.core.SetCoveredTable("Component", "BaseDecompiler");
			this.core.SetCoveredTable("Condition", "BaseDecompiler");
			this.core.SetCoveredTable("Control", "BaseDecompiler");
			this.core.SetCoveredTable("ControlCondition", "BaseDecompiler");
			this.core.SetCoveredTable("ControlEvent", "BaseDecompiler");
			this.core.SetCoveredTable("CreateFolder", "BaseDecompiler");
			this.core.SetCoveredTable("CustomAction", "BaseDecompiler");

			this.core.SetCoveredTable("Dialog", "BaseDecompiler");
			this.core.SetCoveredTable("Directory", "BaseDecompiler");
			this.core.SetCoveredTable("DrLocator", "BaseDecompiler");
			this.core.SetCoveredTable("DuplicateFile", "BaseDecompiler");

			this.core.SetCoveredTable("Environment", "BaseDecompiler");
			this.core.SetCoveredTable("Error", "BaseDecompiler");
			this.core.SetCoveredTable("EventMapping", "BaseDecompiler");
			this.core.SetCoveredTable("Extension", "BaseDecompiler");

			this.core.SetCoveredTable("Feature", "BaseDecompiler");
			this.core.SetCoveredTable("FeatureComponents", "BaseDecompiler");
			this.core.SetCoveredTable("File", "BaseDecompiler");
			this.core.SetCoveredTable("FileSFPCatalog", "BaseDecompiler");
			this.core.SetCoveredTable("Font", "BaseDecompiler");

			this.core.SetCoveredTable("Icon", "BaseDecompiler");
			this.core.SetCoveredTable("IniFile", "BaseDecompiler");
			this.core.SetCoveredTable("IniLocator", "BaseDecompiler");
			this.core.SetCoveredTable("InstallExecuteSequence", "BaseDecompiler");
			this.core.SetCoveredTable("InstallUISequence", "BaseDecompiler");
			this.core.SetCoveredTable("IsolatedComponent", "BaseDecompiler");

			this.core.SetCoveredTable("LaunchCondition", "BaseDecompiler");
			this.core.SetCoveredTable("ListBox", "BaseDecompiler");
			this.core.SetCoveredTable("ListView", "BaseDecompiler");
			this.core.SetCoveredTable("LockPermissions", "BaseDecompiler");

			this.core.SetCoveredTable("Media", "BaseDecompiler");
			this.core.SetCoveredTable("MIME", "BaseDecompiler");
			this.core.SetCoveredTable("MoveFile", "BaseDecompiler");
			this.core.SetCoveredTable("MsiAssembly", "BaseDecompiler");

			// TODO: if user passes an explicitDecompile switch, extract MsiAssemblyName table
			this.core.SetCoveredTable("MsiAssemblyName", "BaseDecompiler");
			this.core.SetCoveredTable("MsiDigitalCertificate", "BaseDecompiler");
			this.core.SetCoveredTable("MsiDigitalSignature", "BaseDecompiler");

			// TODO: if user passes an explicitDecompile switch, extract MsiFileHash table
			this.core.SetCoveredTable("MsiFileHash", "BaseDecompiler");
			this.core.SetCoveredTable("MsiPatchCertificate", "BaseDecompiler");

			// From SDK: This table is usually added to the install package by a transform from a patch package. It is usually not authored directly into an installation package.
			this.core.SetCoveredTable("MsiPatchHeaders", "BaseDecompiler");

			// TODO: if user passes an explicitDecompile switch, extract MsiFileHash table
			this.core.SetCoveredTable("ModuleAdminExecuteSequence", "BaseDecompiler");
			this.core.SetCoveredTable("ModuleAdminUISequence", "BaseDecompiler");
			this.core.SetCoveredTable("ModuleAdvtExecuteSequence", "BaseDecompiler");
			this.core.SetCoveredTable("ModuleComponents", "BaseDecompiler");
			this.core.SetCoveredTable("ModuleConfiguration", "BaseDecompiler");
			this.core.SetCoveredTable("ModuleDependency", "BaseDecompiler");
			this.core.SetCoveredTable("ModuleExclusion", "BaseDecompiler");

			// TODO: add support for ModuleIgnoreTable table.  Not currently supported in either the Schema or the Compiler.
			this.core.SetCoveredTable("ModuleIgnoreTable", "ProcessOtherTables");
			this.core.SetCoveredTable("ModuleInstallExecuteSequence", "BaseDecompiler");
			this.core.SetCoveredTable("ModuleInstallUISequence", "BaseDecompiler");
			this.core.SetCoveredTable("ModuleSignature", "BaseDecompiler");
			this.core.SetCoveredTable("ModuleSubstitution", "BaseDecompiler");

			this.core.SetCoveredTable("ODBCAttribute", "BaseDecompiler");
			this.core.SetCoveredTable("ODBCDataSource", "BaseDecompiler");
			this.core.SetCoveredTable("ODBCDriver", "BaseDecompiler");
			this.core.SetCoveredTable("ODBCSourceAttribute", "BaseDecompiler");
			this.core.SetCoveredTable("ODBCTranslator", "BaseDecompiler");

			// From SDK: This table is usually added to the install package by a transform from a patch package. It is usually not authored directly into an installation package.
			this.core.SetCoveredTable("Patch", "BaseDecompiler");

			// From SDK: This table is usually added to the install package by a transform from a patch package. It is usually not authored directly into an installation package.
			this.core.SetCoveredTable("PatchPackage", "BaseDecompiler");
			this.core.SetCoveredTable("ProgId", "BaseDecompiler");
			this.core.SetCoveredTable("Property", "BaseDecompiler");
			this.core.SetCoveredTable("PublishComponent", "BaseDecompiler");

			this.core.SetCoveredTable("RadioButton", "BaseDecompiler");
			this.core.SetCoveredTable("Registry", "BaseDecompiler");
			this.core.SetCoveredTable("RegLocator", "BaseDecompiler");
			this.core.SetCoveredTable("RemoveFile", "BaseDecompiler");
			this.core.SetCoveredTable("RemoveIniFile", "BaseDecompiler");
			this.core.SetCoveredTable("RemoveRegistry", "BaseDecompiler");
			this.core.SetCoveredTable("ReserveCost", "BaseDecompiler");

			this.core.SetCoveredTable("SelfReg", "BaseDecompiler");
			this.core.SetCoveredTable("ServiceControl", "BaseDecompiler");
			this.core.SetCoveredTable("ServiceInstall", "BaseDecompiler");
			this.core.SetCoveredTable("SFPCatalog", "BaseDecompiler");
			this.core.SetCoveredTable("Shortcut", "BaseDecompiler");
			this.core.SetCoveredTable("Signature", "BaseDecompiler");

			this.core.SetCoveredTable("TextStyle", "BaseDecompiler");
			this.core.SetCoveredTable("TypeLib", "BaseDecompiler");

			this.core.SetCoveredTable("UIText", "BaseDecompiler");
			this.core.SetCoveredTable("Upgrade", "BaseDecompiler");

			this.core.SetCoveredTable("Verb", "BaseDecompiler");

			this.core.SetCoveredTable("candle_DiskInfo", "BaseDecompiler");
			this.core.SetCoveredTable("candle_Files", "BaseDecompiler");
			this.core.SetCoveredTable("candle_Info", "BaseDecompiler");

			if (writeDocumentElements)
			{
				this.StartDocument(this.core.Writer);
			}

			if (this.processingModule)
			{
				string moduleName = "";
				string moduleId = "";
				string moduleLanguage = "";
				string moduleVersion = "";

				this.ProcessModuleSignatureTable(ref moduleName, ref moduleId, ref moduleLanguage, ref moduleVersion);
				this.moduleIdUnderscored = moduleId.Replace("-", "_");
				this.core.OnMessage(WixVerboses.ProcessingModule(null, VerboseLevel.Verbose, moduleName, this.moduleIdUnderscored));

				this.core.Writer.WriteStartElement("Module");
				this.core.WriteAttributeString("Id", moduleName);
				this.core.WriteAttributeString("Guid", moduleId);
				this.core.WriteAttributeString("Language", moduleLanguage);
				this.core.WriteAttributeString("Version", moduleVersion);

				// NOTE: Do not add any elements here, per Source Forge Bug  1235011, 1235012
				//       http://sourceforge.net/tracker/index.php?func=detail&aid=1235012&group_id=105970&atid=642714

				if (this.exportBinaries)
				{
					const string tableName = "File";
					if (this.inputDatabase.TableExists(tableName))
					{
						using (View view = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `", tableName, "`")))
						{
							Record record;
							if (view.Fetch(out record))
							{
								// extract the module cabinet, then explode all of the files to a temp directory
								string cabFileSpec = Path.Combine(this.exportBasePath, "MergeModule.CABinet");
								this.ExtractCabFromPackage(cabFileSpec, "MergeModule.CABinet");
								this.ExtractFilesFromCab(cabFileSpec, 0);
							}
						}
					}
				}
			}
			else
			{
				if (this.processingFragment)
				{
					this.core.Writer.WriteStartElement("Fragment");
				}
				else
				{
					this.core.Writer.WriteStartElement("Product");
					this.core.OnMessage(WixVerboses.ProcessingProduct(null, VerboseLevel.Verbose));
				}

				const string tableName = "Media";
				if (this.inputDatabase.TableExists(tableName))
				{
					using (View view = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `", tableName, "` ORDER BY `DiskId`")))
					{
						string str;
						int diskId = 0;
						Record record;
						while (view.Fetch(out record))
						{
							diskId = Convert.ToInt32(record[(int)MsiInterop.Media.DiskId]);
							int sequence = Convert.ToInt32(record[(int)MsiInterop.Media.LastSequence]);
							str = record[(int)MsiInterop.Media.Cabinet];

							if (0 < str.Length)
							{
								// keep this data around so decompiler does not need to rewalk the media table
								this.lastSequence.Add(diskId, sequence);
								if (str.StartsWith("#"))
								{
									str = str.Substring(1);

									// extract cabinet, then explode all of the files to a temp directory
									string cabFileSpec = Path.Combine(this.exportBasePath, str);
									if (this.exportBinaries)
									{
										this.ExtractCabFromPackage(cabFileSpec, str);
										this.ExtractFilesFromCab(cabFileSpec, diskId);
									}
								}
								else
								{
									// extract cabinet, then explode all of the files to a temp directory
									string cabFileSpec = Path.Combine(this.imageBasePath, str);
									if (this.exportBinaries)
									{
										this.ExtractFilesFromCab(cabFileSpec, diskId);
									}
								}
							}
							else
							{
								// keep this data around so decompiler does not need to rewalk the media table
								// use negative to indicate this diskId was not CABbed
								this.lastSequence.Add(diskId, -sequence);
							}
						}
					}
				}
			}
			this.ProcessSequenceTables();
			if (!this.processUIOnly)
			{
				this.ProcessPropertyTable(PropertyType.Package, this.nonuiproperties); // writes to this.core.Writer

				// NOTE: This is the place to add post package elements, per Source Forge Bugs: 1235011, 1235012
				//       http://sourceforge.net/tracker/index.php?func=detail&aid=1235012&group_id=105970&atid=642714
				if (this.processingModule)
				{
					this.ProcessModuleConfigurationTable(this.core.Writer);
					this.ProcessModuleDependencyTable(this.core.Writer);
					this.ProcessModuleExclusionTable(this.core.Writer);
					this.ProcessModuleSubstitutionTable(this.core.Writer);
				}

				// support decompiler extension here
				foreach (DecompilerExtension extension in this.extensionList)
				{
					extension.ExtendAttributesOfElement("Product", null);
				}

				if (!this.skipSummaryInfo)
				{
					this.ProcessSummaryInformation();
				}
				this.ProcessPropertyTable(PropertyType.Product, this.nonuiproperties);

				// write AppSearch
				this.ProcessAppSearch(this.nonuiproperties);

				// write LaunchConditions
				this.ProcessLaunchConditionTable();

				// write Directories, Components, and their childern
				this.ProcessDirectoryTable(null, null, "", "");

				// write Features
				this.core.WriteComment("Package Features and Root Features");
				this.ProcessFeatureTable(this.core.Writer, null);

				// write Upgrade
				this.core.WriteComment("Package Upgrade Table");
				this.ProcessUpgradeTable();

				// write CCPSearch
				this.core.WriteComment("Package ComplianceCheck Table");
				this.ProcessCCPSearchTable();

				// write Media
				this.core.WriteComment("Package Media Table");
				this.ProcessMediaTable();

				// write CustomActions
				this.core.WriteComment("Package CustomAction (or Not-Yet-Bound CustomAction)");
				this.ProcessCustomActionTable(null, null);
			}

			// if we're not skipping the UI tables, process them now
			if (!this.skipUI)
			{
				this.core.OnMessage(WixVerboses.ProcessingUI(null, VerboseLevel.Verbose));

				XmlWriter uiwriter = this.InitializeXmlTextWriter(Path.GetFullPath(Path.Combine(this.outputFolder, "SkuLibrary\\UI.wxs")));
				if (this.generateFragments)
				{
					// initalize the fragment
					uiwriter.WriteStartElement("Fragment");
					this.core.WriteAttributeString(uiwriter, "Id", "UI");

					// create reference to fragment
					this.core.WriteComment("Package User Interface");
					this.core.Writer.WriteStartElement("FragmentRef");
					this.core.WriteAttributeString("Id", "UI");
					this.core.Writer.WriteEndElement();
				}
				else
				{
					// initialize the UI element
					uiwriter.WriteStartElement("UI");
				}

				// write UI properties
				this.core.WriteComment(uiwriter, "UI Properties");
				this.Processuiproperties(uiwriter);

				// write TextStyles
				this.core.WriteComment(uiwriter, "TextStyle Table");
				this.ProcessTextStyleTable(uiwriter);

				// write UIText
				this.core.WriteComment(uiwriter, "UIText Table");
				this.ProcessUITextTable(uiwriter);

				// write ActionText
				this.core.WriteComment(uiwriter, "Package ActionText (or Not-Yet-Bound ActionText)");
				this.ProcessActionText(uiwriter);

				// write dialogs and controls
				this.core.WriteComment(uiwriter, "Dialogs");
				this.ProcessDialogTable(uiwriter);

				// write Errors
				this.core.WriteComment(uiwriter, "Package Errors (or Not-Yet-Bound Errors)");
				this.ProcessErrorTable(uiwriter);

				// write Billboards
				this.core.WriteComment(uiwriter, "Billboards");
				this.ProcessBillboardTable(uiwriter);

				// write AdminUISequence
				this.core.WriteComment(uiwriter, "UI Sequences (or Not-Yet-Bound UI Sequences)");
				this.EmitSequence("AdminUISequence", uiwriter, null);

				// write InstallUISequence
				this.EmitSequence("InstallUISequence", uiwriter, null);
				if (this.generateFragments)
				{
					// terminate fragment element
					uiwriter.WriteEndElement();
				}
				else
				{
					// terminate the UI element
					uiwriter.WriteEndElement();
				}
			}
			this.core.WriteComment("Package Properties (or Not-Yet-Bound Properties)");
			this.EmitProperties(this.core.Writer, this.nonuiproperties, true);

			if (!this.processUIOnly)
			{
				this.core.WriteComment("Package Execute Sequences (or Not-Yet-Bound Execute Sequences)");
				this.EmitSequence("AdvertiseExecuteSequence", null, null);
				this.EmitSequence("AdminExecuteSequence", null, null);
				this.EmitSequence("InstallExecuteSequence", null, null);
			}

			this.core.WriteComment("Product MsiPatchCertificate Table");
			this.ProcessMsiPatchCertificateTable(this.core.Writer);
			this.core.WriteComment("Package System File Protection Catalogs");
			this.ProcessSFPCatalogTables(this.core.Writer, null);
			this.core.WriteComment("Package Binaries (or Not-Yet-Bound Binaries)");
			this.ProcessBinaryTable(null, BinaryType.Binary, "");

			if (!this.processUIOnly)
			{
				this.ProcessBinaryTable(null, BinaryType.Icon, "");
			}
			this.core.WriteComment("Not Yet Implemented Tables.");
			this.ProcessOtherTables();

			this.core.Writer.WriteEndElement();

			if (writeDocumentElements)
			{
				this.EndDocument(this.core.Writer);
			}
		}

		/// <summary>
		/// Sends a message to the message delegate if there is one.
		/// </summary>
		/// <param name="mea">Message event arguments.</param>
		private void OnMessage(MessageEventArgs mea)
		{
			if (mea is WixError)
			{
				this.foundError = true;
			}

			if (null != this.Message)
			{
				this.Message(this, mea);
			}
		}

		/// <summary>
		/// Processes the Property table, looking only for UI properties.
		/// </summary>
		/// <param name="parentWriter">parent writer.</param>
		private void Processuiproperties(XmlWriter parentWriter)
		{
			Hashtable uiproperties = new Hashtable();
			this.ProcessPropertyTable(PropertyType.UI, uiproperties);
			if (0 < uiproperties.Count)
			{
				XmlWriter writer = this.InitializeXmlTextWriter(Path.GetFullPath(Path.Combine(this.outputFolder, "SkuLibrary\\UI\\uiproperties.wxs")));
				if (this.generateFragments)
				{
					// initalize the fragment
					writer.WriteStartElement("Fragment");
					this.core.WriteAttributeString(writer, "Id", "uiproperties");

					// create reference to fragment
					parentWriter.WriteStartElement("FragmentRef");
					this.core.WriteAttributeString(parentWriter, "Id", "uiproperties");
					parentWriter.WriteEndElement();
				}
				this.EmitProperties(writer, uiproperties, false);
				if (this.generateFragments)
				{
					// terminate the fragment
					writer.WriteEndElement();
				}
			}
		}

		/// <summary>
		/// Reads the SummaryInformation and creates the Package element.
		/// </summary>
		private void ProcessSummaryInformation()
		{
			using (SummaryInformation summary = new SummaryInformation(this.inputDatabase))
			{
				this.core.Writer.WriteStartElement("Package");
				this.core.OnMessage(WixVerboses.ProcessingSummaryInformation(null, VerboseLevel.Verbose));

				this.core.WriteAttributeString("Id", this.StripBraces(summary.GetProperty(9).ToString())); // PackageCode
				this.core.WriteAttributeString("Keywords", summary.GetProperty(5).ToString()); // package keywords
				this.core.WriteAttributeString("Description", summary.GetProperty(3).ToString()); // description
				this.core.WriteAttributeString("Comments", summary.GetProperty(6).ToString().Trim()); // package comments
				this.core.WriteAttributeString("Manufacturer", summary.GetProperty(4).ToString()); // manufacturer
				this.core.WriteAttributeString("InstallerVersion", summary.GetProperty(14).ToString()); // minimum required installer version
				string[] platformAndLanguage = summary.GetProperty(7).ToString().Split(';');
				if (0 < platformAndLanguage.Length)
				{
					this.core.WriteAttributeString("Platforms", platformAndLanguage[0]);
				}

				if (1 < platformAndLanguage.Length)
				{
					this.core.WriteAttributeString("Languages", platformAndLanguage[1]);
				}

				uint sourceFlags = Convert.ToUInt16(summary.GetProperty(15));
				if (0 < (sourceFlags & 1))
				{
					this.core.WriteAttributeString("ShortNames", "yes");
				}

				if (0 < (sourceFlags & 2))
				{
					this.core.WriteAttributeString("Compressed", "yes");
					this.summaryInfoCompressed = true;
				}

				if (0 < (sourceFlags & 4))
				{
					this.core.WriteAttributeString("AdminImage", "yes");
				}

				if (0 < (sourceFlags & 8))
				{
					this.core.WriteAttributeString("InstallPrivileges", "limited");
				}
				this.core.WriteAttributeString("SummaryCodepage", summary.GetProperty(1).ToString());

				int security = Convert.ToInt32(summary.GetProperty(19), CultureInfo.InvariantCulture);
				switch (security)
				{
					case 0:
						this.core.WriteAttributeString("ReadOnly", "no");
						break;
					case 2: // nothing to write; this is the default value
						break;
					case 4:
						this.core.WriteAttributeString("ReadOnly", "yes");
						break;
					default: // let the compiler use the default value
						break;
				}

				this.core.Writer.WriteEndElement();
				summary.Close(false);
			}
		}

		/// <summary>
		/// Emits xml for properties in the passed in list, setting attributes for Admin, Hidden, and Secure.
		/// </summary>
		/// <param name="writer">XmlWriter where the Intermediate should persist itself as XML.</param>
		/// <param name="properties">Hashtable of property names and values to be emitted.</param>
		/// <param name="key">key of the property that one should look for attributes</param>
		private void EmitPropertyAttributes(XmlWriter writer, Hashtable properties, string key)
		{
			this.EmitPropertyAttributes(writer, properties, key, "Id");
		}

		/// <summary>
		/// Emits xml for properties in the passed in list, setting attributes for Admin, Hidden, and Secure.
		/// </summary>
		/// <param name="writer">XmlWriter where the Intermediate should persist itself as XML.</param>
		/// <param name="properties">Hashtable of property names and values to be emitted.</param>
		/// <param name="key">key of the property that one should look for attributes</param>
		/// <param name="attributeName">key of the property that one should look for attributes</param>
		private void EmitPropertyAttributes(XmlWriter writer, Hashtable properties, string key, string attributeName)
		{
			this.core.WriteAttributeString(writer, attributeName, this.StripModuleId(key));
			if (null != this.adminPropNames[key] && true == (bool)this.adminPropNames[key])
			{
				this.adminPropNames[key] = false;
				this.core.WriteAttributeString(writer, "Admin", "yes");
			}

			if (null != this.hiddenPropNames[key] && true == (bool)this.hiddenPropNames[key])
			{
				this.hiddenPropNames[key] = false;
				this.core.WriteAttributeString(writer, "Hidden", "yes");
			}

			if (null != this.securePropNames[key] && true == (bool)this.securePropNames[key])
			{
				this.securePropNames[key] = false;
				this.core.WriteAttributeString(writer, "Secure", "yes");
			}

			if (properties.ContainsKey(key))
			{
				writer.WriteCData((string)properties[key]);
			}
		}

		/// <summary>
		/// Emits xml for properties in the passed in list, setting attributes for Admin, Hidden, and Secure.
		/// </summary>
		/// <param name="writer">XmlWriter where the Intermediate should persist itself as XML.</param>
		/// <param name="properties">Hashtable of property names and values to be emitted.</param>
		/// <param name="key">string that's the key to the property table.</param>
		private void EmitProperty(XmlWriter writer, Hashtable properties, string key)
		{
			if (properties.ContainsKey(key) ||
				(null != this.adminPropNames[key] && true == (bool)this.adminPropNames[key]) ||
				(null != this.hiddenPropNames[key] && true == (bool)this.hiddenPropNames[key]) ||
				(null != this.securePropNames[key] && true == (bool)this.securePropNames[key]))
			{
				writer.WriteStartElement("Property");
				this.EmitPropertyAttributes(writer, properties, key);
				writer.WriteEndElement();
				properties.Remove(key);
			}
		}

		/// <summary>
		/// Emits xml for properties in the passed in list, setting attributes for Admin, Hidden, and Secure.
		/// </summary>
		/// <param name="writer">XmlWriter where the Intermediate should persist itself as XML.</param>
		/// <param name="properties">Hashtable of property names and values to be emitted.</param>
		/// <param name="emitExtras">If true, any property names left in the admin, hidden and secure hashtables will have a blank property generated for them.</param>
		private void EmitProperties(XmlWriter writer, Hashtable properties, bool emitExtras)
		{
			foreach (string key in properties.Keys)
			{
				writer.WriteStartElement("Property");
				this.EmitPropertyAttributes(writer, properties, key);
				writer.WriteEndElement();
			}
			properties.Clear();

			if (emitExtras)
			{
				foreach (string key in this.adminPropNames.Keys)
				{
					if (true == (bool)this.adminPropNames[key])
					{
						writer.WriteStartElement("Property");
						this.core.WriteAttributeString(writer, "Id", this.StripModuleId(key));
						this.core.WriteAttributeString(writer, "Admin", "yes");
						if (null != this.hiddenPropNames[key] && true == (bool)this.hiddenPropNames[key])
						{
							this.hiddenPropNames.Remove(key);
							this.core.WriteAttributeString(writer, "Hidden", "yes");
						}

						if (null != this.securePropNames[key] && true == (bool)this.securePropNames[key])
						{
							this.securePropNames.Remove(key);
							this.core.WriteAttributeString(writer, "Secure", "yes");
						}
						writer.WriteEndElement();
					}
				}
				this.adminPropNames.Clear();

				foreach (string key in this.hiddenPropNames.Keys)
				{
					if (true == (bool)this.hiddenPropNames[key])
					{
						writer.WriteStartElement("Property");
						this.core.WriteAttributeString(writer, "Id", this.StripModuleId(key));
						this.core.WriteAttributeString(writer, "Hidden", "yes");
						if (null != this.securePropNames[key] && true == (bool)this.securePropNames[key])
						{
							this.securePropNames.Remove(key);
							this.core.WriteAttributeString(writer, "Secure", "yes");
						}
						writer.WriteEndElement();
					}
				}
				this.hiddenPropNames.Clear();

				foreach (string key in this.securePropNames.Keys)
				{
					if (true == (bool)this.securePropNames[key])
					{
						writer.WriteStartElement("Property");
						this.core.WriteAttributeString(writer, "Id", this.StripModuleId(key));
						this.core.WriteAttributeString(writer, "Secure", "yes");
						writer.WriteEndElement();
					}
				}
				this.securePropNames.Clear();
			}
		}

		/// <summary>
		/// Processes the Property table writing only the specified types
		/// of properties out to the xml stream.
		/// </summary>
		/// <param name="processPropertyType">Type of properties to process out of the table.</param>
		/// <param name="propertyHash">Hashtable to which to add the properties being processed.</param>
		/// <remarks>This is a terribly inefficient way to process the Property table.</remarks>
		private void ProcessPropertyTable(PropertyType processPropertyType, Hashtable propertyHash)
		{
			const string tableName = "Property";
			const string tableNameDependent = "AppSearch";
			bool hasAppSearchTable = false;
			if (!this.inputDatabase.TableExists(tableName))
			{
				return;
			}

			if (this.inputDatabase.TableExists(tableNameDependent))
			{
				hasAppSearchTable = true;
			}
			this.core.OnMessage(WixVerboses.ProcessingPropertyType(null, VerboseLevel.Verbose, processPropertyType.ToString()));

			string property;
			string value;
			PropertyType propertyType = PropertyType.Ignore;

			using (View view = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `", tableName, "`")))
			{
				Record record;
				while (view.Fetch(out record))
				{
					property = record[(int)MsiInterop.Property.Property];
					value = record[(int)MsiInterop.Property.Value];
					switch (property)
					{
						case "UpgradeCode":
							propertyType = PropertyType.Package;
							value = this.StripBraces(value);
							break;
						case "ProductCode":
							propertyType = PropertyType.Package;
							property = "Id";
							value = this.StripBraces(value);
							break;
						case "Manufacturer":
							propertyType = PropertyType.Package;
							property = "Manufacturer";
							break;
						case "ProductName":
							propertyType = PropertyType.Package;
							property = "Name";
							break;
						case "ProductLanguage":
							propertyType = PropertyType.Package;
							property = "Language";
							break;
						case "ProductVersion":
							propertyType = PropertyType.Package;
							property = "Version";
							break;
						case "XMLSchema":
							propertyType = PropertyType.Ignore;

							//should be we use this over again?
							break;
						case "ErrorDialog":
							propertyType = PropertyType.UI;
							break;
						case "DefaultUIFont":
							propertyType = PropertyType.UI;
							break;
						case "AdminProperties":
							this.InsertProperties(this.adminPropNames, value);
							continue;
						case "MsiHiddenProperties":
							this.InsertProperties(this.hiddenPropNames, value);
							continue;
						case "SecureCustomProperties":
							this.InsertProperties(this.securePropNames, value);
							continue;
						default:
							propertyType = PropertyType.Product;
							break;
					}

					bool hasAppSearchEntry = false;
					if (hasAppSearchTable)
					{
						using (View appSearchView = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `", tableNameDependent, "` WHERE `", tableNameDependent, "`.`", tableName, "`='", property, "'")))
						{
							Record appSearchRecord;
							while (appSearchView.Fetch(out appSearchRecord))
							{
								if (null != appSearchRecord)
								{
									hasAppSearchEntry = true;
								}
							}
						}
					}

					if (!hasAppSearchEntry && processPropertyType == propertyType)
					{
						this.core.OnMessage(WixVerboses.ProcessingProperty(null, VerboseLevel.Verbose, property));

						// package property types are stored as attributes on the parent attribute
						if (PropertyType.Package == propertyType)
						{
							if (!this.processingFragment)
							{
								this.core.WriteAttributeString(property, value);
							}
						}
						else // write out the property
						{
							propertyHash[property] = value;
						}
					}
				}
			}
		}

		/// <summary>
		/// Splits given nameList and places into hashtable as keys.
		/// </summary>
		/// <param name="propNames">Hashtable into which property names will be placed, with value true.</param>
		/// <param name="nameList">Semi-colon delimited string of property names.</param>
		private void InsertProperties(Hashtable propNames, string nameList)
		{
			string[] names = nameList.Split(";".ToCharArray());
			foreach (string name in names)
			{
				if (!propNames.ContainsKey(name))
				{
					propNames[name] = true;
				}
			}
		}

		/// <summary>
		/// Print File Search
		/// </summary>
		/// <param name="writer">XmlWriter where the Intermediate should persist itself as XML.</param>
		/// <param name="fileRecord">file record.</param>
		private void PrintFileSearch(XmlWriter writer, Record fileRecord)
		{
			if (fileRecord != null)
			{
				string signature = fileRecord[(int)MsiInterop.Signature.Signature];
				this.core.OnMessage(WixVerboses.ProcessingFileSearchSignature(null, VerboseLevel.Verbose, signature));

				if (this.fileSearchSignatures.ContainsKey(this.StripModuleId(signature)))
				{
					writer.WriteStartElement("FileSearchRef");
					this.core.WriteAttributeString(writer, "Id", this.StripModuleId(signature));
				}
				else
				{
					this.fileSearchSignatures[this.StripModuleId(signature)] = true;
					string fileName = fileRecord[(int)MsiInterop.Signature.FileName];
					string minversion = fileRecord[(int)MsiInterop.Signature.MinVersion];
					string maxversion = fileRecord[(int)MsiInterop.Signature.MaxVersion];
					string minsize = fileRecord[(int)MsiInterop.Signature.MinSize];
					string maxsize = fileRecord[(int)MsiInterop.Signature.MaxSize];
					string mindate = this.core.ConvertMSIDateToXmlDate(fileRecord[(int)MsiInterop.Signature.MinDate]);
					string maxdate = this.core.ConvertMSIDateToXmlDate(fileRecord[(int)MsiInterop.Signature.MaxDate]);
					string languages = fileRecord[(int)MsiInterop.Signature.Languages];

					// due to a Windows Installer bug, it's legal to have either the Name or LongName
					// attribute or both attributes
					string name = null;
					string longName = null;
					string[] fileNames = fileName.Split("|".ToCharArray());
					if (2 == fileNames.Length) // both short and long names
					{
						name = fileNames[0];
						longName = fileNames[1];
					}
					else if (CompilerCore.IsValidShortFilename(fileName)) // only short name is present
					{
						name = fileName;
					}
					else // only long name is present
					{
						longName = fileName;
					}

					writer.WriteStartElement("FileSearch");
					this.core.WriteAttributeString(writer, "Id", this.StripModuleId(signature));
					this.core.WriteAttributeString(writer, "LongName", longName);
					this.core.WriteAttributeString(writer, "Name", name);
					this.core.WriteAttributeString(writer, "MinVersion", minversion);
					this.core.WriteAttributeString(writer, "MaxVersion", maxversion);
					this.core.WriteAttributeString(writer, "MinSize", minsize);
					this.core.WriteAttributeString(writer, "MaxSize", maxsize);
					this.core.WriteAttributeString(writer, "MinDate", mindate);
					this.core.WriteAttributeString(writer, "MaxDate", maxdate);
					this.core.WriteAttributeString(writer, "Languages", languages);
				}
				writer.WriteEndElement();
			}
		}

		/// <summary>
		/// Process the SignatureKeys
		/// </summary>
		/// <param name="writer">writer.</param>
		/// <param name="rememberedSignatures">Hashtable of remembered signatures.</param>
		/// <param name="findRMCCP">true if one is processing RMCCP.</param>
		private void ProcessSignatureKeys(XmlWriter writer, Hashtable rememberedSignatures, bool findRMCCP)
		{
			if (!findRMCCP)
			{
				// call the search processors on the signature
				foreach (string associatedSignature in rememberedSignatures.Keys)
				{
					this.ProcessCompLocator(writer, associatedSignature);
				}

				foreach (string associatedSignature in rememberedSignatures.Keys)
				{
					this.ProcessRegLocator(writer, associatedSignature);
				}

				foreach (string associatedSignature in rememberedSignatures.Keys)
				{
					this.ProcessIniLocator(writer, associatedSignature);
				}
			}

			foreach (string associatedSignature in rememberedSignatures.Keys)
			{
				if (!this.ProcessSignature(writer, associatedSignature, findRMCCP))
				{
					this.ProcessDrLocator(writer, associatedSignature, null, true, findRMCCP);
				}
			}

			foreach (string associatedSignature in rememberedSignatures.Keys)
			{
				if (!this.searchSignatures.ContainsKey(this.StripModuleId(associatedSignature)) && this.inputDatabase.TableExists("Signature"))
				{
					using (View fileView = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `Signature` WHERE `Signature`='", associatedSignature, "'")))
					{
						Record fileRecord;
						fileView.Fetch(out fileRecord);

						if (null != fileRecord)
						{
							this.PrintFileSearch(writer, fileRecord);
						}
					}
				}
			}
		}

		/// <summary>
		/// Process the AppSearch table
		/// </summary>
		/// <param name="properties">Hashtable of property names and values to be emitted.</param>
		private void ProcessAppSearch(Hashtable properties)
		{
			const string tableName = "AppSearch";
			const string tableNameDependent = "Property";
			string property = "";
			string signature = "";
			XmlWriter writer = null;
			bool hasPropertyTable = false;
			if (!this.inputDatabase.TableExists(tableName))
			{
				return;
			}

			if (this.inputDatabase.TableExists(tableNameDependent))
			{
				hasPropertyTable = true;
			}

			using (View view = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `", tableName, "`")))
			{
				Record record;
				while (view.Fetch(out record))
				{
					if (null == writer)
					{
						writer = this.InitializeXmlTextWriter(Path.GetFullPath(Path.Combine(this.outputFolder, "BaseLibrary\\AppSearch.wxs")));
						this.core.WriteComment(writer, "Package AppSearch (or Not-Yet-Bound LaunchCondition)");
					}
					property = record[(int)MsiInterop.AppSearch.Property];
					signature = record[(int)MsiInterop.AppSearch.Signature];

					// set the property element header
					if (!this.appSearchProperties.ContainsKey(this.StripModuleId(property)))
					{
						if (this.generateFragments)
						{
							// initalize the fragment
							writer.WriteStartElement("Fragment");
							this.core.WriteAttributeString(writer, "Id", this.StripModuleId(property));
						}
						writer.WriteStartElement("Property");
						this.EmitPropertyAttributes(writer, properties, this.StripModuleId(property));
						properties.Remove(this.StripModuleId(property));
						string value = "";
						if (hasPropertyTable)
						{
							using (View propertyView = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `", tableNameDependent, "` WHERE `", tableNameDependent, "`='", property, "'")))
							{
								Record propertyRecord;
								propertyView.Fetch(out propertyRecord);

								if (null != propertyRecord)
								{
									value = propertyRecord[(int)MsiInterop.Property.Value];
								}
							}
						}

						if (0 < value.Length)
						{
							writer.WriteCData(value);
						}
						Hashtable rememberedSignatures = new Hashtable();

						// AppSearch has a multi-column key so look for all the signatures that are associated with this property
						using (View signatureView = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `", tableName, "` WHERE `Property`='", property, "'")))
						{
							Record signatureRecord;
							while (signatureView.Fetch(out signatureRecord))
							{
								rememberedSignatures[signatureRecord[(int)MsiInterop.AppSearch.Signature]] = true;
							}
						}

						// call the search processors on the signature
						this.ProcessSignatureKeys(writer, rememberedSignatures, false);

						this.appSearchProperties[this.StripModuleId(property)] = "";
						foreach (string associatedSignature in rememberedSignatures.Keys)
						{
							this.appSearchProperties[this.StripModuleId(property)] += associatedSignature;
						}

						if (this.generateFragments)
						{
							// terminate the fragment
							writer.WriteEndElement();
						}

						// finish the property element
						writer.WriteEndElement();
					}
				}
			}
		}

		/// <summary>
		/// Process the RegLocator table
		/// </summary>
		/// <param name="writer">XmlWriter where the Intermediate should persist itself as XML.</param>
		/// <param name="signature">string with signature of interest.</param>
		/// <returns>representing whether signature was found in this table</returns>
		private bool ProcessRegLocator(XmlWriter writer, string signature)
		{
			const string tableName = "RegLocator";
			const string tableNameDependent = "Signature";
			string root = "";
			string key = "";
			string name = "";
			string valtype = "";
			bool foundSignature = false;

			if (!this.inputDatabase.TableExists(tableName))
			{
				return foundSignature;
			}

			using (View view = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `", tableName, "` WHERE `", tableName, "`.`Signature_`='", signature, "'")))
			{
				Record record;
				while (view.Fetch(out record))
				{
					foundSignature = true;
					this.searchSignatures[this.StripModuleId(signature)] = true;
					switch (Convert.ToInt32(record[(int)MsiInterop.RegLocator.Root])) // non-nullable field
					{
						case MsiInterop.MsidbRegistryRootClassesRoot:
							root = "HKCR";
							break;
						case MsiInterop.MsidbRegistryRootCurrentUser:
							root = "HKCU";
							break;
						case MsiInterop.MsidbRegistryRootLocalMachine:
							root = "HKLM";
							break;
						case MsiInterop.MsidbRegistryRootUsers:
							root = "HKU";
							break;
						default:
							// TOOD: throw an exception - Fail "Unknown Registry root in RegLocator: " & row.IntegerData(4)
							break;
					}
					key = record[(int)MsiInterop.RegLocator.Key];
					name = record[(int)MsiInterop.RegLocator.Name];
					int type = MsiInterop.MsidbLocatorTypeFileName;
					if (0 < record[(int)MsiInterop.RegLocator.Type].Length)
					{
						type = Convert.ToInt32(record[(int)MsiInterop.RegLocator.Type]); // nullable field
					}

					switch (type)
					{
						case MsiInterop.MsidbLocatorTypeDirectory:
							valtype = "directory";
							break;
						case MsiInterop.MsidbLocatorTypeFileName:
							valtype = "file";
							break;
						case MsiInterop.MsidbLocatorTypeRawValue:
							valtype = "raw";
							break;
						default:
							// TODO: throw an exception - Fail "Unknown Registry root in RegLocator: " & row.IntegerData(4)
							break;
					}

					if (this.registrySearchSignatures.ContainsKey(this.StripModuleId(signature)))
					{
						writer.WriteStartElement("RegistrySearchRef");
						this.core.WriteAttributeString(writer, "Id", this.StripModuleId(signature));
					}
					else
					{
						this.registrySearchSignatures[this.StripModuleId(signature)] = true;
						writer.WriteStartElement("RegistrySearch");

						this.core.WriteAttributeString(writer, "Id", this.StripModuleId(signature));
						this.core.WriteAttributeString(writer, "Root", root);
						this.core.WriteAttributeString(writer, "Key", key);
						this.core.WriteAttributeString(writer, "Name", name);
						this.core.WriteAttributeString(writer, "Type", valtype);

						if (this.inputDatabase.TableExists(tableNameDependent))
						{
							using (View fileView = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `", tableNameDependent, "` WHERE `", tableNameDependent, "`='", signature, "'")))
							{
								Record fileRecord;
								fileView.Fetch(out fileRecord);

								if (null != fileRecord)
								{
									this.PrintFileSearch(writer, fileRecord);
								}
							}
						}

						if (this.inputDatabase.TableExists("DrLocator"))
						{
							using (View dirView = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `DrLocator` WHERE `Parent`='", signature, "'")))
							{
								Record dirRecord;
								while (dirView.Fetch(out dirRecord))
								{
									this.ProcessDrLocator(writer, this.StripModuleId(dirRecord[(int)MsiInterop.DrLocator.Signature]), null, true, false);
								}
							}
						}
					}
					writer.WriteEndElement();
				}
			}
			return foundSignature;
		}

		/// <summary>
		/// Process CompLocator table
		/// </summary>
		/// <param name="writer">XmlWriter where the Intermediate should persist itself as XML.</param>
		/// <param name="signature">string with signature of interest.</param>
		/// <returns>representing whether signature was found in this table</returns>
		private bool ProcessCompLocator(XmlWriter writer, string signature)
		{
			const string tableName = "CompLocator";
			const string tableNameDependent = "Signature";
			bool foundSignature = false;

			if (!this.inputDatabase.TableExists(tableName))
			{
				return foundSignature;
			}

			using (View view = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `", tableName, "` WHERE `", tableName, "`.`Signature_`='", signature, "'")))
			{
				Record record;
				while (view.Fetch(out record))
				{
					foundSignature = true;
					this.searchSignatures[this.StripModuleId(signature)] = true;

					int type;
					if (0 < record[(int)MsiInterop.CompLocator.Type].Length) // nullable field
					{
						type = Convert.ToInt32(record[(int)MsiInterop.CompLocator.Type]);
					}
					else
					{
						type = MsiInterop.MsidbLocatorTypeFileName;
					}

					string keypathtype = "";
					switch (type)
					{
						case MsiInterop.MsidbLocatorTypeDirectory:
							keypathtype = "directory";
							break;
						case MsiInterop.MsidbLocatorTypeFileName:
							keypathtype = "file";
							break;
						default:
							// TODO: throw an exception - Fail "Unknown Registry root in RegLocator: " & row.IntegerData(4)
							break;
					}

					writer.WriteStartElement("ComponentSearch");

					this.core.WriteAttributeString(writer, "Id", this.StripModuleId(signature));
					this.core.WriteAttributeString(writer, "Guid", this.StripBraces(record[(int)MsiInterop.CompLocator.ComponentId]));
					this.core.WriteAttributeString(writer, "Type", keypathtype);

					if (this.inputDatabase.TableExists(tableNameDependent))
					{
						using (View fileView = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `", tableNameDependent, "` WHERE `", tableNameDependent, "`='", signature, "'")))
						{
							Record fileRecord;
							fileView.Fetch(out fileRecord);

							if (fileRecord != null)
							{
								this.PrintFileSearch(writer, fileRecord);
							}
						}
					}

					if (this.inputDatabase.TableExists("DrLocator"))
					{
						using (View dirView = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `DrLocator` WHERE `Parent`='", signature, "'")))
						{
							Record dirRecord;
							while (dirView.Fetch(out dirRecord))
							{
									this.ProcessDrLocator(writer, this.StripModuleId(dirRecord[(int)MsiInterop.DrLocator.Signature]), null, true, false);
							}
						}
					}
					writer.WriteEndElement();
				}
			}
			return foundSignature;
		}

		/// <summary>
		/// Process IniLocator table
		/// </summary>
		/// <param name="writer">XmlWriter where the Intermediate should persist itself as XML.</param>
		/// <param name="signature">string with signature of interest.</param>
		/// <returns>representing whether signature was found in this table</returns>
		private bool ProcessIniLocator(XmlWriter writer, string signature)
		{
			const string tableName = "IniLocator";
			const string tableNameDependent = "Signature";
			bool foundSignature = false;

			if (!this.inputDatabase.TableExists(tableName))
			{
				return foundSignature;
			}

			using (View view = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `", tableName, "` WHERE `", tableName, "`.`Signature_`='", signature, "'")))
			{
				Record record;
				while (view.Fetch(out record))
				{
					foundSignature = true;
					this.searchSignatures[this.StripModuleId(signature)] = true;

					int type;
					if (0 < record[(int)MsiInterop.IniLocator.Type].Length) // nullable field
					{
						type = Convert.ToInt32(record[(int)MsiInterop.IniLocator.Type]);
					}
					else
					{
						type = MsiInterop.MsidbLocatorTypeFileName;
					}

					string keypathtype = "";
					switch (type)
					{
						case MsiInterop.MsidbLocatorTypeDirectory:
							keypathtype = "directory";
							break;
						case MsiInterop.MsidbLocatorTypeFileName:
							keypathtype = "file";
							break;
						case MsiInterop.MsidbLocatorTypeRawValue:
							keypathtype = "raw";
							break;
						default:
							// TODO: throw an exception - Fail "Unknown Registry root in RegLocator: " & row.IntegerData(4)
							break;
					}

					writer.WriteStartElement("IniFileSearch");

					string name = record[(int)MsiInterop.IniLocator.FileName];
					string longName = null;
					int indexOfSeparator = name.IndexOf('|');
					if (indexOfSeparator >= 0)
					{
						name = name.Substring(0, indexOfSeparator);
						longName = name.Substring(indexOfSeparator + 1);
					}

					this.core.WriteAttributeString(writer, "Id", this.StripModuleId(signature)); // non-nullable
					this.core.WriteAttributeString(writer, "Name", name); // non-nullable
					if (null != longName)
					{
						this.core.WriteAttributeString(writer, "LongName", longName);
					}
					this.core.WriteAttributeString(writer, "Section", record[(int)MsiInterop.IniLocator.Section]); // non-nullable
					this.core.WriteAttributeString(writer, "Key", record[(int)MsiInterop.IniLocator.Key]); // non-nullable
					this.core.WriteAttributeString(writer, "Field", record[(int)MsiInterop.IniLocator.Field]); // nullable
					this.core.WriteAttributeString(writer, "Type", keypathtype);

					if (this.inputDatabase.TableExists(tableNameDependent))
					{
						using (View fileView = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `", tableNameDependent, "` WHERE `", tableNameDependent, "`='", signature, "'")))
						{
							Record fileRecord;
							fileView.Fetch(out fileRecord);

							if (fileRecord != null)
							{
								this.PrintFileSearch(writer, fileRecord);
							}
						}
					}

					if (this.inputDatabase.TableExists("DrLocator"))
					{
						using (View dirView = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `DrLocator` WHERE `Parent`='", signature, "'")))
						{
							Record dirRecord;
							while (dirView.Fetch(out dirRecord))
							{
								this.ProcessDrLocator(writer, this.StripModuleId(dirRecord[(int)MsiInterop.DrLocator.Signature]), null, true, false);
							}
						}
					}
					writer.WriteEndElement();
				}
			}
			return foundSignature;
		}

		/// <summary>
		/// Process DrLocator table
		/// </summary>
		/// <param name="writer">XmlWriter where the Intermediate should persist itself as XML.</param>
		/// <param name="signature">Enclosing signature context.</param>
		/// <param name="fileRecord">Enclosing file context.</param>
		/// <param name="rootLocator">true if this is the root locator.</param>
		/// <param name="findRMCCP">true if one is processing RMCCP.</param>
		/// <returns>count of nested directories</returns>
		private int ProcessDrLocator(XmlWriter writer, string signature, Record fileRecord, bool rootLocator, bool findRMCCP)
		{
			const string tableName = "DrLocator";
			string parent = "";
			string path = "";
			string depth = "";
			int ndirs = 0;
			int ret = 0;
			int i = 0;

			if (!this.inputDatabase.TableExists(tableName))
			{
				return ret;
			}

			string query = String.Concat("SELECT * FROM `", tableName, "` WHERE `Signature_`='", signature, "'");
			if (findRMCCP)
			{
				query = String.Concat(query, " AND `Parent`='CCP_DRIVE'");
			}
			else
			{
				query = String.Concat(query, " AND `Parent`<>'CCP_DRIVE'");
			}

			using (View view = this.inputDatabase.OpenExecuteView(query))
			{
				Record record;
				while (view.Fetch(out record))
				{
					parent = record[(int)MsiInterop.DrLocator.Parent];
					if (0 < parent.Length)
					{
						if (!findRMCCP)
						{
							ndirs = this.ProcessDrLocator(writer, parent, null, false, findRMCCP);
						}
					}

					path = record[(int)MsiInterop.DrLocator.Path];
					depth = record[(int)MsiInterop.DrLocator.Depth];
					string key = String.Concat(this.StripModuleId(signature), ".", parent);

					if (this.directorySearchSignatures.ContainsKey(key))
					{
						writer.WriteStartElement("DirectorySearchRef");
						this.core.WriteAttributeString(writer, "Id", this.StripModuleId(signature));
						if (0 < parent.Length && 0 == ndirs)
						{
							this.core.WriteAttributeString(writer, "Parent", parent);
						}
						this.core.WriteAttributeString(writer, "Path", path);
					}
					else
					{
						this.searchSignatures[this.StripModuleId(signature)] = true;
						this.directorySearchSignatures[key] = true;
						writer.WriteStartElement("DirectorySearch");
						this.core.WriteAttributeString(writer, "Id", this.StripModuleId(signature));
						this.core.WriteAttributeString(writer, "Path", path);
						this.core.WriteAttributeString(writer, "Depth", depth);
					}

					if (rootLocator)
					{
						this.PrintFileSearch(writer, fileRecord);
						for (i = 0; i <= ndirs; i++)
						{
							writer.WriteEndElement();
						}
					}
					else
					{
						ret = ndirs + 1;
					}
				}
			}
			return ret;
		}

		/// <summary>
		/// Processes the Signature table;
		/// </summary>
		/// <param name="writer">XmlWriter where the Intermediate should persist itself as XML.</param>
		/// <param name="signature">string with signature of interest.</param>
		/// <param name="findRMCCP">true if one is processing RMCCP.</param>
		/// <returns>representing whether signature was found in this table</returns>
		private bool ProcessSignature(XmlWriter writer, string signature, bool findRMCCP)
		{
			const string tableName = "Signature";
			bool foundSignature = false;

			if (!this.inputDatabase.TableExists(tableName))
			{
				return foundSignature;
			}

			using (View view = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `", tableName, "` WHERE `", tableName, "`.`Signature`='", signature, "'")))
			{
				Record record;
				while (view.Fetch(out record))
				{
					foundSignature = true;
					this.ProcessDrLocator(writer, signature, record, true, findRMCCP);
				}
			}
			return foundSignature;
		}

		/// <summary>
		/// Process tables without specific processing method
		/// </summary>
		private void ProcessOtherTables()
		{
			XmlWriter writer = null;
			using (View databaseView = this.inputDatabase.OpenExecuteView("SELECT * FROM `_Tables` ORDER BY Name"))
			{
				Record databaseRecord;
				while (databaseView.Fetch(out databaseRecord))
				{
					string tableName = databaseRecord[1]; //1 - based - name of table
					if (this.skipVSI && "_vsdlaunchcondition" == tableName.ToLower())
					{
						this.core.OnMessage(WixWarnings.FilteringVSIStuff(null, WarningLevel.Major, "_VsdLaunchCondition", "entire table"));
						continue;
					}
					string processor = this.core.CoveredTableProcessor(tableName);
					if (this.inputDatabase.TableExists(tableName))
					{
						if (null == processor || "" == processor || "ProcessOtherTables" == processor)
						{
							using (View tableView = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `", tableName, "`")))
							{
								Record tableRecord = null;
								Record columnNamesRecord = null;
								Record columnTypesRecord = null;
								bool hasRecords = tableView.Fetch(out tableRecord);
								if (hasRecords || this.keepEmptyTables)
								{
									if (null == writer)
									{
										writer = this.InitializeXmlTextWriter(Path.GetFullPath(Path.Combine(this.outputFolder, "SkuLibrary\\Orphans.wxs")));
										if (processor == "ProcessOtherTables")
										{
											this.core.WriteComment("MSI or WiX Native Table");
										}
										else
										{
											this.core.WriteComment("Non-Native Table or Not-Yet-Implemented in Decompiler");
										}

										if (this.generateFragments)
										{
											// initalize the fragment
											writer.WriteStartElement("Fragment");
											this.core.WriteAttributeString(writer, "Id", "Orphans");

											// create reference to fragment
											this.core.Writer.WriteStartElement("FragmentRef");
											this.core.WriteAttributeString("Id", "Orphans");
											this.core.Writer.WriteEndElement();
										}
									}
									writer.WriteStartElement("CustomTable");

									this.core.WriteAttributeString(writer, "Id", tableName);

									Record keysRecord = this.inputDatabase.PrimaryKeys(tableName);

									// Get all the column text names - 1 - based
									// REVIEW: the following is never used in the VBS code... not sure why it's here so porting it anyway
									//int numCols = 0;
									using (View columnViewStray = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `_Columns` WHERE `Table` = '", tableName, "'")))
									{
										Record columnRecordStray;
										while (columnViewStray.Fetch(out columnRecordStray))
										{
										}
									}

									// Get all the column info
									tableView.GetColumnInfo(MsiInterop.MSICOLINFONAMES, out columnNamesRecord);
									tableView.GetColumnInfo(MsiInterop.MSICOLINFOTYPES, out columnTypesRecord);

									for (int index = 1; index <= columnNamesRecord.GetFieldCount(); index++)
									{
										writer.WriteStartElement("Column");
										string columnName = columnNamesRecord.GetString(index);
										this.core.WriteAttributeString(writer, "Id", columnName);
										string columnType = columnTypesRecord.GetString(index);
										string columnDataType = columnType.Substring(0, 1);
										string columnDataSize = columnType.Substring(1);

										// Nullable attribute
										if (columnDataType.ToUpper() == columnDataType)
										{
											this.core.WriteAttributeString(writer, "Nullable", "yes");
										}

										// Localizable attribute
										if (columnDataType.ToLower() == "l")
										{
											this.core.WriteAttributeString(writer, "Localizable", "yes");
										}

										// Type Attribute
										switch (columnDataType.ToLower())
										{
											case "s":
											case "l":
											case "g":
												this.core.WriteAttributeString(writer, "Type", "string");
												break;
											case "i":
											case "j":
												this.core.WriteAttributeString(writer, "Type", "int");
												break;
											case "v":
												this.core.WriteAttributeString(writer, "Type", "binary");
												break;
											default:
												// TODO: throw an exception
												break;
										}

										// Width Attribute
										if (columnDataType.ToLower() != "v")
										{
											this.core.WriteAttributeString(writer, "Width", columnDataSize);
										}

										bool primaryKey = false;
										for (int keyCount = 1; !primaryKey && keyCount <= keysRecord.GetFieldCount(); keyCount++)
										{
											if (columnName == keysRecord.GetString(keyCount))
											{
												primaryKey = true;
											}
										}

										if (primaryKey)
										{
											this.core.WriteAttributeString(writer, "PrimaryKey", "yes");
										}
										writer.WriteEndElement(); // column
									}
								}

								if (hasRecords)
								{
									do
									{
										writer.WriteStartElement("Row");
										for (int index = 1; index <= columnNamesRecord.GetFieldCount(); index++)
										{
											writer.WriteStartElement("Data");
											this.core.WriteAttributeString(writer, "Column", columnNamesRecord.GetString(index));
											string columnType = columnTypesRecord.GetString(index);
											string columnDataType = columnType.Substring(0, 1);
											if (columnDataType.ToLower() != "v")
											{
												string recordValue = tableRecord.GetString(index);
												if (null != recordValue && 0 < recordValue.Length)
												{
													writer.WriteCData(recordValue);
												}
											}
											else
											{
												// NOTE: this is not yet supported in the linker so this
												//       mainly just gets the decompiler exception out of the way
												if (this.exportBinaries)
												{
													// TODO: this does not export the binary
													string binaryName = String.Concat(tableName, ".idt");
													this.inputDatabase.Export(tableName, this.exportBasePath, binaryName);
													File.Delete(Path.Combine(this.exportBasePath, binaryName));
												}
												string id = "";
												for (int index2 = 1; index2 < index; index2++)
												{
													string columnType2 = columnTypesRecord.GetString(index2);
													string columnDataType2 = columnType2.Substring(0, 1);
													if (columnDataType2.ToLower() != "v")
													{
														id = String.Concat(id, columnNamesRecord.GetString(index2));
													}
												}
												this.core.WriteAttributeString(writer, "SourceFile", Path.Combine(tableName, String.Concat(id, ".ibd")));
											}
											writer.WriteEndElement(); // Data
										}
										writer.WriteEndElement(); // Row
									}
									while (tableView.Fetch(out tableRecord));
								}

								if (hasRecords || this.keepEmptyTables)
								{
									writer.WriteEndElement(); // CustomTable
								}
							}
						}

						if (this.keepEmptyTables && tableName.ToLower() != "_validation")
						{
							this.core.Writer.WriteStartElement("EnsureTable");
							this.core.WriteAttributeString("Id", tableName);
							this.core.Writer.WriteEndElement();
						}
					}
				}
			}

			foreach (DecompilerExtension extension in this.extensionList)
			{
				extension.ProcessOtherTables();
			}

			if (this.generateFragments && null != writer)
			{
				// terminate the fragment
				writer.WriteEndElement();
			}
		}

		/// <summary>
		/// Process LaunchCondition table
		/// </summary>
		private void ProcessLaunchConditionTable()
		{
			const string tableName = "LaunchCondition";

			if (!this.inputDatabase.TableExists(tableName))
			{
				return;
			}
			else if (this.processingModule)
			{
				this.core.OnMessage(WixWarnings.IllegalMergeModuleTable(null, WarningLevel.Major, "LaunchCondition"));
				return;
			}
			this.core.OnMessage(WixVerboses.ProcessingLaunchConditions(null, VerboseLevel.Verbose));

			using (View view = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `", tableName, "`")))
			{
				Record record;
				bool firstRecord = false;
				while (view.Fetch(out record))
				{
					if (!firstRecord)
					{
						firstRecord = true;
						this.core.WriteComment("Package LaunchCondition (or Not-Yet-Bound LaunchCondition)");
					}
					this.core.Writer.WriteStartElement("Condition");
					this.core.WriteAttributeString("Message", record[(int)MsiInterop.LaunchCondition.Description]);
					this.core.Writer.WriteCData(record[(int)MsiInterop.LaunchCondition.Condition]);
					this.core.Writer.WriteEndElement();
				}
			}
		}

		/// <summary>
		/// Process Condition table
		/// </summary>
		/// <param name="writer">XmlWriter where the Intermediate should persist itself as XML.</param>
		/// <param name="feature">Enclosing feature context.</param>
		private void ProcessConditionTable(XmlWriter writer, string feature)
		{
			const string tableName = "Condition";

			if (!this.inputDatabase.TableExists(tableName))
			{
				return;
			}
			this.core.OnMessage(WixVerboses.ProcessingFeatureConditions(null, VerboseLevel.Verbose));

			using (View view = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `", tableName, "` WHERE `Feature_` = '", feature, "'")))
			{
				Record record;
				while (view.Fetch(out record))
				{
					string condition = record[(int)MsiInterop.Condition.Condition];
					if (0 < condition.Length)
					{
						writer.WriteStartElement(tableName);
						this.core.WriteAttributeString(writer, "Level", record[(int)MsiInterop.Condition.Level]);
						writer.WriteCData(condition);
						writer.WriteEndElement();
					}
				}
			}
		}

		/// <summary>
		/// Processes the FeatureComponent entries for a particular feature.
		/// </summary>
		/// <param name="writer">XmlWriter where the Intermediate should persist itself as XML.</param>
		/// <param name="feature">Name of feature to extract components from FeatureComponent table.</param>
		private void ProcessFeatureComponentTable(XmlWriter writer, string feature)
		{
			const string tableName = "FeatureComponents";
			if (!this.inputDatabase.TableExists(tableName))
			{
				return;
			}
			this.core.OnMessage(WixVerboses.ProcessingFeatureComponents(null, VerboseLevel.Verbose, feature));

			using (View view = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `", tableName, "` WHERE `Feature_`='", feature, "'")))
			{
				Record record;
				while (view.Fetch(out record))
				{
					string component = this.core.GetValidIdentifier(record[(int)MsiInterop.FeatureComponents.Component], "FeatureComponent; Column: Component");
					this.core.OnMessage(WixVerboses.ProcessingFeatureComponent(null, VerboseLevel.Verbose, component));

					writer.WriteStartElement("ComponentRef");
					this.core.WriteAttributeString(writer, "Id", component);
					writer.WriteEndElement();
				}
			}
		}

		/// <summary>
		/// Process Class table
		/// </summary>
		/// <param name="writer">XmlWriter where the Intermediate should persist itself as XML.</param>
		/// <param name="component">Enclosing component context.</param>
		private void ProcessClassTable(XmlWriter writer, string component)
		{
			const string tableName = "Class";
			if (!this.inputDatabase.TableExists(tableName))
			{
				return;
			}
			Hashtable appIds = new Hashtable();
			appIds.Add("", true);
			if (this.inputDatabase.TableExists("AppId"))
			{
				using (View appIdsView = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `AppId`")))
				{
					Record appIdsRecord;
					while (appIdsView.Fetch(out appIdsRecord))
					{
						appIds.Add(appIdsRecord[(int)MsiInterop.AppId.AppId], true);
					}
				}
			}
			this.core.OnMessage(WixVerboses.ProcessingClass(null, VerboseLevel.Verbose, component));
			string andAppId = "";
			foreach (string key in appIds.Keys)
			{
				if (0 < key.Length)
				{
					andAppId = String.Concat(" AND `AppId_` = '", key, "'");
				}
				else
				{
					andAppId = String.Concat(" AND `AppId_` IS NULL");
				}
				string appId = "";
				using (View view = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `", tableName, "` WHERE `Component_`='", component, "'", andAppId)))
				{
					Record record;
					while (view.Fetch(out record))
					{
						string classId = record[(int)MsiInterop.Class.CLSID];
						if (!this.processedAdvertisedClasses.ContainsKey(classId))
						{
							this.processedAdvertisedClasses.Add(classId, true);
							if (0 < this.StripBraces(record[(int)MsiInterop.Class.AppId]).Length)
							{
								appId = record[(int)MsiInterop.Class.AppId];
								if (!this.processedAdvertisedAppIds.ContainsKey(String.Concat(appId, classId)))
								{
									this.processedAdvertisedAppIds.Add(String.Concat(appId, classId), true);
									if (!this.processedAdvertisedAppIds.ContainsKey(appId) && this.inputDatabase.TableExists("AppId"))
									{
										this.processedAdvertisedAppIds.Add(appId, true);
										using (View appIdView = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `AppId` WHERE `AppId` = '", appId, "'")))
										{
											Record appIdRecord;
											while (appIdView.Fetch(out appIdRecord))
											{
												writer.WriteStartElement("AppId");
												this.core.WriteAttributeString(writer, "Id", this.StripBraces(appId));
												string activateAtStorage = appIdRecord[(int)MsiInterop.AppId.ActivateAtStorage];
												if (0 < activateAtStorage.Length && "0" != activateAtStorage)
												{
													this.core.WriteAttributeString(writer, "ActivateAtStorage", "yes");
												}
												string dllSurrogate = appIdRecord[(int)MsiInterop.AppId.DllSurrogate];
												if (0 < dllSurrogate.Length)
												{
													this.core.WriteAttributeString(writer, "DllSurrogate", dllSurrogate);
												}
												string localService = appIdRecord[(int)MsiInterop.AppId.LocalService];
												if (0 < localService.Length)
												{
													this.core.WriteAttributeString(writer, "LocalService", localService);
												}
												string remoteServerName = appIdRecord[(int)MsiInterop.AppId.RemoteServerName];
												if (0 < remoteServerName.Length)
												{
													this.core.WriteAttributeString(writer, "RemoteServerName", remoteServerName);
												}
												string serviceParameters = appIdRecord[(int)MsiInterop.AppId.ServiceParameters];
												if (0 < serviceParameters.Length)
												{
													this.core.WriteAttributeString(writer, "ServiceParameters", serviceParameters);
												}
												string runAsInteractiveUser = appIdRecord[(int)MsiInterop.AppId.RunAsInteractiveUser];
												if (0 < runAsInteractiveUser.Length && "0" != runAsInteractiveUser)
												{
													this.core.WriteAttributeString(writer, "RunAsInteractiveUser", "yes");
												}
												this.core.WriteAttributeString(writer, "Advertise", "yes");
											}
										}
									}
								}
							}
							writer.WriteStartElement(tableName);
							this.core.OnMessage(WixVerboses.ProcessingClass(null, VerboseLevel.Verbose, classId));
							this.core.WriteAttributeString(writer, "Id", this.StripBraces(classId));
							string classContextList = "";
							using (View contextView = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `", tableName, "` WHERE `Component_` = '", component, "' AND `CLSID` = '", classId, "'", andAppId)))
							{
								Record contextRecord;
								while (contextView.Fetch(out contextRecord))
								{
									string contextKey = contextRecord[(int)MsiInterop.Class.Context];
									if (0 < classContextList.Length)
									{
										classContextList += "\t";
									}

									// ensure the case of these attributes is correct
									switch (contextKey.ToLower())
									{
										case "localserver":
											classContextList += "LocalServer";
											break;
										case "localserver32":
											classContextList += "LocalServer32";
											break;
										case "inprocserver":
											classContextList += "InprocServer";
											break;
										case "inprocserver32":
											classContextList += "InprocServer32";
											break;
										default: // let the user deal with other values in the compiler
											classContextList += contextKey;
											break;
									}
								}
							}
							this.core.WriteAttributeString(writer, "Context", classContextList);
							if (0 < record[(int)MsiInterop.Class.Description].Length)
							{
								this.core.WriteAttributeString(writer, "Description", record[(int)MsiInterop.Class.Description]);
							}

							if (0 < record[(int)MsiInterop.Class.IconIndex].Length)
							{
								this.core.WriteAttributeString(writer, "IconIndex", record[(int)MsiInterop.Class.IconIndex]);
							}

							if (0 < record[(int)MsiInterop.Class.DefInprocHandler].Length)
							{
								this.core.WriteAttributeString(writer, "Handler", record[(int)MsiInterop.Class.DefInprocHandler]);
							}

							if (0 < record[(int)MsiInterop.Class.Argument].Length)
							{
								this.core.WriteAttributeString(writer, "Argument", record[(int)MsiInterop.Class.Argument]);
							}
							string defProgId = record[(int)MsiInterop.Class.ProgId];

							if (0 < record[(int)MsiInterop.Class.Attributes].Length)
							{
								uint flags = Convert.ToUInt16(record[(int)MsiInterop.Class.Attributes]);
								if (0 < (flags & MsiInterop.MsidbClassAttributesRelativePath))
								{
									this.core.WriteAttributeString(writer, "RelativePath", "yes");
								}
							}

							this.core.WriteAttributeString(writer, "Advertise", "yes");
							this.ProcessProgIdTable(writer, component, classId, defProgId);
							string fileTypeMask = record[(int)MsiInterop.Class.FileTypeMask];
							if (0 < fileTypeMask.Length)
							{
								string[] fileTypeMasks = fileTypeMask.Split(';');
								foreach (string mask in fileTypeMasks)
								{
									string[] values = mask.Split(',');
									writer.WriteStartElement("FileTypeMask");
									if (3 < values.Length)
									{
										this.core.WriteAttributeString(writer, "Offset", values[0]);
										this.core.WriteAttributeString(writer, "Mask", values[2]);
										this.core.WriteAttributeString(writer, "Value", values[3]);
									}
									writer.WriteEndElement();
								}
							}

							// Need to support the interface table, this.ProcessInterfaceTable(writer, ...
							writer.WriteEndElement();
						}
					}
				}

				if (0 < appId.Length)
				{
					writer.WriteEndElement();
				}
			}
		}

		/// <summary>
		/// Process ProgId table
		/// </summary>
		/// <param name="writer">XmlWriter where the Intermediate should persist itself as XML.</param>
		/// <param name="component">Enclosing component context.</param>
		/// <param name="classId">Enclosing ClassId context.</param>
		/// <param name="defProgId">default ProdId.</param>
		private void ProcessProgIdTable(XmlWriter writer, string component, string classId, string defProgId) // component used for child Extension elements
		{
			const string tableName = "ProgId";

			if (!this.inputDatabase.TableExists(tableName))
			{
				if (0 < defProgId.Length)
				{
					this.core.OnMessage(WixWarnings.MissingProgId(null, WarningLevel.Major, defProgId, classId));
				}
				return;
			}
			this.core.OnMessage(WixVerboses.ProcessingProgId(null, VerboseLevel.Verbose));

			string queryPrimary = String.Concat("SELECT * FROM `", tableName, "` WHERE `Class_`='", classId, "'");
			string querySecondary = null;
			if (null == classId) // ' looking for version - independent ProgId, no classId allowed
			{
				queryPrimary = String.Concat("SELECT * FROM `", tableName, "` WHERE `Class_`= NULL AND `ProgId_Parent`='", defProgId, "'");
			}
			else if (0 < defProgId.Length) // looking for defProgId first, then others belonging to class
			{
				querySecondary = String.Concat(queryPrimary, " AND `ProgId`<>'", defProgId, "'");
				queryPrimary = String.Concat(queryPrimary, " AND `ProgId`='", defProgId, "'");
			}

			// the Using directive makes the view read only
			View view = this.inputDatabase.OpenExecuteView(queryPrimary);
			{
				Record record;
				while (view.Fetch(out record))
				{
					writer.WriteStartElement(tableName);
					string progId = record[(int)MsiInterop.ProgId.ProgId];
					this.core.OnMessage(WixVerboses.ProcessingProgId(null, VerboseLevel.Verbose, progId));

					this.core.WriteAttributeString(writer, "Id", this.StripBraces(progId));
					this.core.WriteAttributeString(writer, "Description", record[(int)MsiInterop.ProgId.Description]);
					if (null != classId)
					{
						this.core.WriteAttributeString(writer, "Icon", record[(int)MsiInterop.ProgId.Icon]);
						this.core.WriteAttributeString(writer, "IconIndex", record[(int)MsiInterop.ProgId.IconIndex]);
						this.ProcessExtensionTable(writer, component, progId);
						this.ProcessProgIdTable(writer, component, null, progId);
					}
					writer.WriteEndElement();

					if (null != querySecondary)
					{
						view = this.inputDatabase.OpenExecuteView(querySecondary);
						querySecondary = null;
					}
				}
			}
		}

		/// <summary>
		/// Process Extension table
		/// </summary>
		/// <param name="writer">XmlWriter where the Intermediate should persist itself as XML.</param>
		/// <param name="component">Enclosing component context.</param>
		/// <param name="progId">Enclosing progId context.</param>
		private void ProcessExtensionTable(XmlWriter writer, string component, string progId)
		{
			const string tableName = "Extension";

			if (!this.inputDatabase.TableExists(tableName))
			{
				return;
			}
			this.core.OnMessage(WixVerboses.ProcessingExtension(null, VerboseLevel.Verbose));

			string query = String.Concat("SELECT * FROM `", tableName, "` WHERE `Component_`='", component, "' AND `ProgId_`=");
			if (null != progId && 0 < progId.Length)
			{
				query = String.Concat(query, "'", progId, "'");
			}
			else
			{
				query = String.Concat(query, "NULL");
			}

			using (View view = this.inputDatabase.OpenExecuteView(query))
			{
				Record record;
				while (view.Fetch(out record))
				{
					writer.WriteStartElement(tableName);
					string extension = record[(int)MsiInterop.Extension.Extension];
					this.core.OnMessage(WixVerboses.ProcessingExtension(null, VerboseLevel.Verbose, extension));
					string mimeValue = record[(int)MsiInterop.Extension.MIME];
					this.core.WriteAttributeString(writer, "ContentType", mimeValue);
					this.core.WriteAttributeString(writer, "Id", extension);
					this.core.WriteAttributeString(writer, "Advertise", "yes");
					if (this.inputDatabase.TableExists("MIME"))
					{
						using (View mimeView = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `MIME` WHERE `Extension_`='", extension, "'")))
						{
							Record mimeRecord;
							while (mimeView.Fetch(out mimeRecord))
							{
								writer.WriteStartElement("MIME");
								this.core.WriteAttributeString(writer, "Class", mimeRecord[(int)MsiInterop.MIME.CLSID]);
								this.core.WriteAttributeString(writer, "ContentType", mimeRecord[(int)MsiInterop.MIME.ContentType]);
								if (null == mimeValue || 0 == mimeValue.Length)
								{
									this.core.WriteAttributeString(writer, "Default", "yes");
								}
								writer.WriteEndElement();
							}
						}
					}

					if (this.inputDatabase.TableExists("Verb"))
					{
						using (View verbView = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `Verb` WHERE `Extension_`='", extension, "'")))
						{
							Record verbRecord;
							while (verbView.Fetch(out verbRecord))
							{
								writer.WriteStartElement("Verb");
								this.core.WriteAttributeString(writer, "Id", verbRecord[(int)MsiInterop.Verb.Verb]);
								this.core.WriteAttributeString(writer, "Argument", verbRecord[(int)MsiInterop.Verb.Argument]);
								this.core.WriteAttributeString(writer, "Command", verbRecord[(int)MsiInterop.Verb.Command]);
								this.core.WriteAttributeString(writer, "Sequence", verbRecord[(int)MsiInterop.Verb.Sequence]);
								writer.WriteEndElement();
							}
						}
					}
					writer.WriteEndElement();
				}
			}
		}

		/// <summary>
		/// Processes the feature record under the specified parent.
		/// </summary>
		/// <param name="writer">XmlWriter where the Intermediate should persist itself as XML.</param>
		/// <param name="parentWriter">Parent XmlWriter to point to add the reference.</param>
		/// <param name="parent">Parent feature for this feature.</param>
		/// <param name="record">Feature record.</param>
		private void ProcessFeatureRecord(XmlWriter writer, XmlWriter parentWriter, string parent, Record record)
		{
			const string tableName = "Feature";
			const string tableNameRef = "FeatureRef";

			string featureName = record[(int)MsiInterop.Feature.Feature];
			this.core.OnMessage(WixVerboses.ProcessingFeature(null, VerboseLevel.Verbose, featureName));

			int displayLevel = 0;
			string display = null;
			int bits = Convert.ToInt32(record[(int)MsiInterop.Feature.Attributes]); // non-nullable field
			if (0 < record[(int)MsiInterop.Feature.Display].Length)
			{
				displayLevel = Convert.ToInt32(record[(int)MsiInterop.Feature.Display]);
			}

			if (0 == displayLevel)
			{
				display = "hidden";
			}
			else if (0 < (displayLevel & 1)) // is the first bit set
			{
				display = "expand";
			}

			if (this.generateFragments)
			{
				writer.WriteStartElement("Fragment");
			}

			writer.WriteStartElement(tableName);
			this.core.WriteAttributeString(writer, "Id", featureName);
			this.core.WriteAttributeString(writer, "Title", record[(int)MsiInterop.Feature.Title]);
			this.core.WriteAttributeString(writer, "Description", record[(int)MsiInterop.Feature.Description]);
			this.core.WriteAttributeString(writer, "Display", display);

			// if the condition is null then the level in the condition is actual level for the feature
			string level = record[(int)MsiInterop.Feature.Level];
			if (this.inputDatabase.TableExists("Condition"))
			{
				using (View conditionView = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `Condition` WHERE `Feature_` = '", featureName, "'")))
				{
					Record conditionRecord;
					while (conditionView.Fetch(out conditionRecord))
					{
						string condition = conditionRecord[(int)MsiInterop.Condition.Condition];
						if (0 == condition.Length)
						{
							this.core.OnMessage(WixWarnings.NullConditionInConditionTable(null, WarningLevel.Major, featureName));
							level = conditionRecord[(int)MsiInterop.Condition.Level];
						}
					}
				}
			}

			this.core.WriteAttributeString(writer, "Level", level);
			this.core.WriteAttributeString(writer, "ConfigurableDirectory", record[(int)MsiInterop.Feature.Directory]);
			if (0 < (bits & MsiInterop.MsidbFeatureAttributesFavorSource))
			{
				this.core.WriteAttributeString(writer, "InstallDefault", "source");
			}

			if (0 < (bits & MsiInterop.MsidbFeatureAttributesFavorAdvertise))
			{
				this.core.WriteAttributeString(writer, "TypicalDefault", "advertise");
			}

			if (0 < (bits & MsiInterop.MsidbFeatureAttributesFollowParent))
			{
				this.core.WriteAttributeString(writer, "InstallDefault", "followParent");
			}

			if (0 < (bits & MsiInterop.MsidbFeatureAttributesUIDisallowAbsent))
			{
				this.core.WriteAttributeString(writer, "Absent", "disallow");
			}

			if (0 < (bits & MsiInterop.MsidbFeatureAttributesDisallowAdvertise))
			{
				this.core.WriteAttributeString(writer, "AllowAdvertise", "no");
			}
			else if (0 < (bits & MsiInterop.MsidbFeatureAttributesNoUnsupportedAdvertise))
			{
				this.core.WriteAttributeString(writer, "AllowAdvertise", "system");
			}
			this.ProcessConditionTable(writer, featureName);
			this.ProcessFeatureComponentTable(writer, featureName);

			if (this.generateFragments)
			{
				parentWriter.WriteStartElement(tableNameRef);
				this.core.WriteAttributeString(parentWriter, "Id", featureName);
				parentWriter.WriteEndElement();
			}
			this.ProcessFeatureTable(writer, featureName);

			// support decompiler extension here
			foreach (DecompilerExtension extension in this.extensionList)
			{
				extension.ExtendChildrenOfElement("Feature", featureName);
			}

			if (this.generateFragments)
			{
				writer.WriteEndElement(); // close parent (sometimes self)
			}
			writer.WriteEndElement(); // close fragment or self
		}

		/// <summary>
		/// Processes the features under the specified parent.
		/// </summary>
		/// <param name="parentWriter">parent writer.</param>
		/// <param name="parent">Parent feature for this feature.</param>
		private void ProcessFeatureTable(XmlWriter parentWriter, string parent)
		{
			const string tableName = "Feature";
			if (!this.inputDatabase.TableExists(tableName))
			{
				return;
			}
			else if (this.processingModule)
			{
				this.core.OnMessage(WixWarnings.IllegalMergeModuleTable(null, WarningLevel.Major, "Feature"));
				return;
			}
			this.core.OnMessage(WixVerboses.ProcessingFeature(null, VerboseLevel.Verbose));

			using (View view = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `", tableName, "` WHERE `Feature_Parent` = '", parent, "' ORDER BY Display")))
			{
				Record record;
				while (view.Fetch(out record))
				{
					string featureName = record[(int)MsiInterop.Feature.Feature];
					XmlWriter writer = this.InitializeXmlTextWriter(Path.GetFullPath(Path.Combine(Path.Combine(this.outputFolder, "Features"), String.Concat(featureName, ".wxs"))));
					this.ProcessFeatureRecord(writer, parentWriter, parent, record);
				}
			}
		}

		/// <summary>
		/// Process Directory table
		/// </summary>
		/// <param name="writer">XmlWriter where the Intermediate should persist itself as XML.</param>
		/// <param name="parent">Enclosing node of directory context.</param>
		/// <param name="parentPathShort">Enclosing path of directory context.</param>
		/// <param name="parentPathLong">Enclosing path of directory context.</param>
		private void ProcessDirectoryTable(XmlWriter writer, string parent, string parentPathShort, string parentPathLong)
		{
			const string tableName = "Directory";
			const string tableNameRef = "DirectoryRef";
			if (!this.inputDatabase.TableExists(tableName))
			{
				return;
			}
			this.core.OnMessage(WixVerboses.ProcessingDirectory(null, VerboseLevel.Verbose));

			string query = String.Concat("SELECT * FROM `", tableName, "`");
			if (null != parent)
			{
				query = String.Concat(query, " WHERE `Directory_Parent`='", parent, "'");
			}

			using (View view = this.inputDatabase.OpenExecuteView(query))
			{
				Record record;
				while (view.Fetch(out record))
				{
					if (null == writer)
					{
						writer = this.InitializeXmlTextWriter(Path.GetFullPath(Path.Combine(this.outputFolder, "BaseLibrary\\directories.wxs")));
					}
					string directoryName = record[(int)MsiInterop.Directory.Directory];

					if (!this.processedDirectories.ContainsKey(directoryName))
					{
						this.core.OnMessage(WixVerboses.ProcessingDirectory(null, VerboseLevel.Verbose, directoryName));

						string newParent = record[(int)MsiInterop.Directory.DirectoryParent];
						if ((parent != null && 0 < parent.Length) ||
							parent == newParent ||
							newParent.Length == 0)
						{
							string id = this.StripModuleId(this.core.GetValidIdentifier(directoryName, "Directory"));
							if (this.generateFragments)
							{
								writer.WriteStartElement("Fragment");
								this.ProcessCustomActionTable(writer, id);
								if (null != parent)
								{
									writer.WriteStartElement(tableNameRef);
									string parentId = this.StripModuleId(parent);
									this.core.WriteAttributeString(writer, "Id", parentId);
								}
							}
							writer.WriteStartElement(tableName);
							this.core.WriteAttributeString(writer, "Id", id);

							string defaultDir = record[(int)MsiInterop.Directory.DefaultDir];
							int targetSeparator = defaultDir.IndexOf(":");

							// split source and target
							string sourceName = null;
							string targetName = defaultDir;
							if (0 <= targetSeparator)
							{
								sourceName = defaultDir.Substring(targetSeparator + 1);
								targetName = defaultDir.Substring(0, targetSeparator);
							}

							// split the source short and long names
							string sourceLongName = null;
							if (null != sourceName)
							{
								int sourceLongNameSeparator = sourceName.IndexOf("|");
								if (0 <= sourceLongNameSeparator)
								{
									sourceLongName = sourceName.Substring(sourceLongNameSeparator + 1);
									sourceName = sourceName.Substring(0, sourceLongNameSeparator);
								}
							}

							// split the target short and long names
							int targetLongNameSeparator = targetName.IndexOf("|");
							string targetLongName = null;
							if (0 <= targetLongNameSeparator)
							{
								targetLongName = targetName.Substring(targetLongNameSeparator + 1);
								targetName = targetName.Substring(0, targetLongNameSeparator);
							}

							// remove the long source name when its identical to the long source name
							if (null != sourceName && sourceName == sourceLongName)
							{
								sourceLongName = null;
							}

							// remove the long target name when its identical to the long target name
							if (null != targetName && targetName == targetLongName)
							{
								targetLongName = null;
							}

							// remove the source names when they are identical to the target names
							if (sourceName == targetName && sourceLongName == targetLongName)
							{
								sourceName = null;
								sourceLongName = null;
							}

							// name must be 8.3, preliminary test 12, may need further refinement
							if ("SourceDir" != targetName && "SOURCEDIR" != targetName && !DecompilerCore.IsValidShortFilename(targetName))
							{
								targetName = this.core.GetValidShortName(targetName, "Directory", id, "DefaultDir");
							}

							// write the target name(s)
							if ("." != targetName)
							{
								this.core.WriteAttributeString(writer, "Name", targetName);
							}

							if (null != targetLongName && "." != targetLongName)
							{
								this.core.WriteAttributeString(writer, "LongName", targetLongName);
							}

							// write the source name(s)
							if (null != sourceName && "." != sourceName)
							{
								this.core.WriteAttributeString(writer, "SourceName", sourceName);
							}

							if (null != sourceLongName && "." != sourceLongName)
							{
								this.core.WriteAttributeString(writer, "LongSource", sourceLongName);
							}

							// calculate the short source name for this directory
							string shortDirectory;
							string longDirectory;
							if (null != sourceName) // explicit source name
							{
								shortDirectory = sourceName;

								if (null != sourceLongName)
								{
									longDirectory = sourceLongName;
								}
								else
								{
									longDirectory = sourceName;
								}
							}
							else // use the "target" names
							{
								shortDirectory = targetName;

								if (null != targetLongName)
								{
									longDirectory = targetLongName;
								}
								else
								{
									longDirectory = targetName;
								}
							}

							// ignore "." since it actually means no change in directory
							if ("." == shortDirectory)
							{
								shortDirectory = String.Empty;
							}

							if ("." == longDirectory)
							{
								longDirectory = String.Empty;
							}

							string rootPathShort = "";
							string rootPathLong = "";
							if (0 < parentPathShort.Length)
							{
								rootPathShort = Path.Combine(parentPathShort, shortDirectory);
								rootPathLong = Path.Combine(parentPathLong, longDirectory);
							}
							else if (targetName != "SourceDir")
							{
								rootPathShort = shortDirectory;
								rootPathLong = longDirectory;
							}

							if (this.generateFragments)
							{
								if (null != parent)
								{
									writer.WriteEndElement(); // close self
								}
								writer.WriteEndElement(); // close parent (sometimes self)
								writer.WriteEndElement(); // close fragment
							}
							this.ProcessComponentTable(directoryName, rootPathShort, rootPathLong);
							this.ProcessDirectoryTable(writer, directoryName, rootPathShort, rootPathLong);
							if (this.processingModule && directoryName == "TARGETDIR")
							{
								this.ProcessOrphanedComponents(writer);
							}

							if (!this.generateFragments)
							{
								writer.WriteEndElement();
							}

							this.processedDirectories.Add(directoryName, rootPathShort);
						}
					}
				}
			}
		}

		/// <summary>
		/// Process Environment table
		/// </summary>
		/// <param name="writer">XmlWriter where the Intermediate should persist itself as XML.</param>
		/// <param name="component">Enclosing component context.</param>
		private void ProcessEnvironmentTable(XmlWriter writer, string component)
		{
			const string tableName = "Environment";
			if (!this.inputDatabase.TableExists(tableName))
			{
				return;
			}
			this.core.OnMessage(WixVerboses.ProcessingEnvironment(null, VerboseLevel.Verbose));

			using (View view = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `", tableName, "` WHERE `Component_` = '", component, "'")))
			{
				Record record;
				while (view.Fetch(out record))
				{
					writer.WriteStartElement(tableName);
					string env = record[(int)MsiInterop.Environment.Environment];
					this.core.OnMessage(WixVerboses.ProcessingEnvironment(null, VerboseLevel.Verbose, env));

					this.core.WriteAttributeString(writer, "Id", env);
					string name = record[(int)MsiInterop.Environment.Name];
					this.core.WriteAttributeString(writer, "Name", name);
					if (-1 < name.IndexOf("!"))
					{
						this.core.WriteAttributeString(writer, "Action", "remove");
					}
					else if (-1 < name.IndexOf("="))
					{
						this.core.WriteAttributeString(writer, "Action", "create");
					}
					else
					{
						this.core.WriteAttributeString(writer, "Action", "set");
					}

					if (-1 < name.IndexOf("*"))
					{
						this.core.WriteAttributeString(writer, "System", "yes");
					}

					if (-1 < name.IndexOf("-"))
					{
						this.core.WriteAttributeString(writer, "Permanent", "yes");
					}
					string envValue = record[(int)MsiInterop.Environment.Value];
					int position = envValue.IndexOf("[~]");
					if (-1 < position)
					{
						if (0 == position)
						{
							this.core.WriteAttributeString(writer, "Part", "last");
							envValue = envValue.Replace("[~];", "");
						}
						else
						{
							this.core.WriteAttributeString(writer, "Part", "first");
							envValue = envValue.Replace(";[~]", "");
						}
					}
					this.core.WriteAttributeString(writer, "Value", envValue);
					writer.WriteEndElement();
				}
			}
		}

		/// <summary>
		/// Process MsiAssembly table
		/// </summary>
		/// <param name="writer">XmlWriter where the Intermediate should persist itself as XML.</param>
		/// <param name="component">Enclosing component context.</param>
		private void ProcessMsiAssemblyTable(XmlWriter writer, string component)
		{
			const string tableName = "MsiAssembly";

			if (!this.inputDatabase.TableExists(tableName))
			{
				return;
			}

			using (View view = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `", tableName, "` WHERE `Component_` = '", component, "'")))
			{
				Record record;
				while (view.Fetch(out record))
				{
					string assemblyManifest = record[(int)MsiInterop.MsiAssembly.FileManifest];
					if (0 < assemblyManifest.Length)
					{
						this.core.WriteAttributeString(writer, "AssemblyManifest", this.core.GetValidIdentifier(assemblyManifest, "MsiAssemblyTable; Column: FileManifest"));
					}
					string assemblyApplicaiton = record[(int)MsiInterop.MsiAssembly.FileApplication];
					if (0 < assemblyApplicaiton.Length)
					{
						this.core.WriteAttributeString(writer, "AssemblyApplication", assemblyApplicaiton);
					}
					int attributes = Convert.ToInt32(record[(int)MsiInterop.MsiAssembly.Attributes]); // non-nullable field
					if (attributes == 1)
					{
						this.core.WriteAttributeString(writer, "Assembly", "win32");
					}
					else
					{
						this.core.WriteAttributeString(writer, "Assembly", ".net");
					}
				}
			}
		}

		/// <summary>
		/// Process RemoveFile table
		/// </summary>
		/// <param name="writer">XmlWriter where the Intermediate should persist itself as XML.</param>
		/// <param name="component">Enclosing component context.</param>
		private void ProcessRemoveFileTable(XmlWriter writer, string component)
		{
			const string tableName = "RemoveFile";
			if (!this.inputDatabase.TableExists(tableName))
			{
				return;
			}
			this.core.OnMessage(WixVerboses.ProcessingRemoveFile(null, VerboseLevel.Verbose, component));

			using (View view = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `", tableName, "` WHERE `Component_` = '", component, "'")))
			{
				Record record;
				while (view.Fetch(out record))
				{
					string fileName = record[(int)MsiInterop.RemoveFile.FileName];
					if (0 < fileName.Length)
					{
						writer.WriteStartElement(tableName);
					}
					else
					{
						writer.WriteStartElement("RemoveFolder");
					}
					this.core.WriteAttributeString(writer, "Id", this.StripModuleId(record[(int)MsiInterop.RemoveFile.FileKey]));
					string dirProperty = record[(int)MsiInterop.RemoveFile.DirProperty];
					if (this.inputDatabase.TableExists("Directory"))
					{
						using (View dirView = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `Directory` WHERE `Directory` = '", dirProperty, "'")))
						{
							Record dirRecord;
							if (dirView.Fetch(out dirRecord))
							{
								this.core.WriteAttributeString(writer, "Directory", this.StripModuleId(this.core.GetValidIdentifier(dirProperty, "RemoveFile; Column: Directory")));
							}
							else
							{
								this.core.WriteAttributeString(writer, "Property", this.StripModuleId(dirProperty));
							}
						}
					}
					string[] names = fileName.Split('|');
					this.core.WriteAttributeString(writer, "Name", names[0]);
					if (1 < names.Length && names[0] != names[1])
					{
						this.core.WriteAttributeString(writer, "LongName", names[1]);
					}
					int installMode = Convert.ToInt32(record[(int)MsiInterop.RemoveFile.InstallMode]); // non-nullable field
					string installModeValue = "";
					switch (installMode)
					{
						case MsiInterop.MsidbRemoveFileInstallModeOnInstall:
							installModeValue = "install";
							break;
						case MsiInterop.MsidbRemoveFileInstallModeOnRemove:
							installModeValue = "uninstall";
							break;
						case MsiInterop.MsidbRemoveFileInstallModeOnBoth:
							installModeValue = "both";
							break;
						default:
							// TODO: throw an exception
							break;
					}
					this.core.WriteAttributeString(writer, "On", installModeValue);
					writer.WriteEndElement();
				}
			}
		}

		/// <summary>
		/// Process IniFile table
		/// </summary>
		/// <param name="writer">XmlWriter where the Intermediate should persist itself as XML.</param>
		/// <param name="component">Enclosing component context.</param>
		/// <param name="tableName">table name.</param>
		private void ProcessIniFileTable(XmlWriter writer, string component, string tableName)
		{
			if (!this.inputDatabase.TableExists(tableName))
			{
				return;
			}

			using (View view = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `", tableName, "` WHERE `Component_` = '", component, "'")))
			{
				Record record;
				while (view.Fetch(out record))
				{
					writer.WriteStartElement("IniFile");
					this.core.WriteAttributeString(writer, "Id", this.StripModuleId(record[(int)MsiInterop.IniFile.IniFile])); //non-nullable
					this.core.WriteAttributeString(writer, "Name", record[(int)MsiInterop.IniFile.FileName]); //non-nullable
					this.core.WriteAttributeString(writer, "Directory", this.StripModuleId(this.core.GetValidIdentifier(record[(int)MsiInterop.IniFile.DirProperty], "IniFile"))); // nullable
					this.core.WriteAttributeString(writer, "Section", record[(int)MsiInterop.IniFile.Section]); //non-nullable
					this.core.WriteAttributeString(writer, "Key", record[(int)MsiInterop.IniFile.Key]); //non-nullable
					this.core.WriteAttributeString(writer, "Value", record[(int)MsiInterop.IniFile.Value]); //non-nullable
					int actionMode = Convert.ToInt32(record[(int)MsiInterop.IniFile.Action]); // non-nullable field
					string action = null;
					switch (actionMode)
					{
						case MsiInterop.MsidbIniFileActionCreateLine:
							action = "createLine";
							break;
						case MsiInterop.MsidbIniFileActionAddLine:
							action = "addLine";
							break;
						case MsiInterop.MsidbIniFileActionAddTag:
							action = "addTag";
							break;
						case MsiInterop.MsidbIniFileActionRemoveLine:
							action = "removeLine";
							break;
						case MsiInterop.MsidbIniFileActionRemoveTag:
							action = "removeTag";
							break;
						default:
							// TODO: throw an exception
							break;
					}
					this.core.WriteAttributeString(writer, "Action", action);
					writer.WriteEndElement();
				}
			}
		}

		/// <summary>
		/// Process TypeLib table
		/// </summary>
		/// <param name="writer">XmlWriter where the Intermediate should persist itself as XML.</param>
		/// <param name="component">Enclosing component context.</param>
		private void ProcessTypeLibTable(XmlWriter writer, string component)
		{
			string tableName = "TypeLib";
			if (!this.inputDatabase.TableExists(tableName))
			{
				return;
			}
			string query = String.Concat("SELECT * FROM `", tableName, "` WHERE `Component_`='", component, "'");

			using (View typeLibView = this.inputDatabase.OpenExecuteView(query))
			{
				Record typeLibRecord;
				while (typeLibView.Fetch(out typeLibRecord))
				{
					writer.WriteStartElement(tableName);
					string id = typeLibRecord[(int)MsiInterop.TypeLib.LibID];
					this.core.WriteAttributeString(writer, "Id", this.StripBraces(id)); // non-nullable
					this.core.WriteAttributeString(writer, "Language", typeLibRecord[(int)MsiInterop.TypeLib.Language]); // non-nullable
					string version = typeLibRecord[(int)MsiInterop.TypeLib.Version]; // nullable
					if (null != version && 0 < version.Length)
					{
						int ver = Convert.ToInt32(version);
						if (65536 == ver)
						{
							this.core.OnMessage(WixWarnings.PossiblyIncorrectTypelibVersion(id));
						}
						int minor = ver & 0xFF;
						int major = (ver & 0xFFFF00) >> 8;
						this.core.WriteAttributeString(writer, "MajorVersion", major.ToString());
						this.core.WriteAttributeString(writer, "MinorVersion", minor.ToString());
					}
					this.core.WriteAttributeString(writer, "Description", typeLibRecord[(int)MsiInterop.TypeLib.Description]); // nullable
					this.core.WriteAttributeString(writer, "HelpDirectory", typeLibRecord[(int)MsiInterop.TypeLib.Directory]); // nullable
					this.core.WriteAttributeString(writer, "Cost", typeLibRecord[(int)MsiInterop.TypeLib.Cost]); // nullable
					writer.WriteEndElement();
				}
			}
		}

		/// <summary>
		/// Process PublishComponent table
		/// </summary>
		/// <param name="writer">XmlWriter where the Intermediate should persist itself as XML.</param>
		/// <param name="component">Enclosing component context.</param>
		private void ProcessPublishComponentTable(XmlWriter writer, string component)
		{
			string tableName = "PublishComponent";
			if (!this.inputDatabase.TableExists(tableName))
			{
				return;
			}
			string query = String.Concat("SELECT * FROM `", tableName, "` WHERE `Component_`='", component, "'");

			using (View publishComponentView = this.inputDatabase.OpenExecuteView(query))
			{
				Record publishComponentRecord;
				while (publishComponentView.Fetch(out publishComponentRecord))
				{
					writer.WriteStartElement("Category");
					this.core.WriteAttributeString(writer, "Id", this.StripBraces(publishComponentRecord[(int)MsiInterop.PublishComponent.ComponentId])); // non-nullable
					this.core.WriteAttributeString(writer, "Qualifier", publishComponentRecord[(int)MsiInterop.PublishComponent.Qualifier]); // non-nullable
					this.core.WriteAttributeString(writer, "AppData", publishComponentRecord[(int)MsiInterop.PublishComponent.AppData]); // non-nullable
					writer.WriteEndElement();
				}
			}
		}

		/// <summary>
		/// Process ODBCDriver or ODBCTranslator table
		/// </summary>
		/// <param name="writer">XmlWriter where the Intermediate should persist itself as XML.</param>
		/// <param name="component">Enclosing component context.</param>
		/// <param name="parentFileId">parent file id.</param>
		/// <param name="tableName">table name.</param>
		private void ProcessODBCRootTable(XmlWriter writer, string component, string parentFileId, string tableName)
		{
			Hashtable processed;
			processed = this.odbcDriverProcessed;
			switch (tableName)
			{
				case "ODBCTranslator":
					processed = this.odbcTranslatorProcessed;
					break;
				case "ODBCDriver":
					processed = this.odbcDriverProcessed;
					break;
				default:
					// TODO: throw an exception
					break;
			}

			if (!this.inputDatabase.TableExists(tableName))
			{
				return;
			}
			string query = String.Concat("SELECT * FROM `", tableName, "` WHERE `Component_`='", component, "'");
			if (null != parentFileId && 0 < parentFileId.Length)
			{
				query = String.Concat(query, " AND `File_`='", parentFileId, "'");
			}

			using (View odbcDriverView = this.inputDatabase.OpenExecuteView(query))
			{
				Record odbcDriverRecord;
				while (odbcDriverView.Fetch(out odbcDriverRecord))
				{
					string key = odbcDriverRecord[(int)MsiInterop.ODBCDriver.Driver];
					if (!processed.Contains(key))
					{
						processed[key] = true;
						writer.WriteStartElement(tableName);
						this.core.WriteAttributeString(writer, "Id", key);
						this.core.WriteAttributeString(writer, "Name", odbcDriverRecord[(int)MsiInterop.ODBCDriver.Description]); // non-nullable
						string file = odbcDriverRecord[(int)MsiInterop.ODBCDriver.File]; //non-nullable
						if (null == parentFileId || parentFileId != file)
						{
							this.core.WriteAttributeString(writer, "File", file);
						}
						string fileSetup = odbcDriverRecord[(int)MsiInterop.ODBCDriver.FileSetup]; //nullable
						if (null != fileSetup && 0 < fileSetup.Length && fileSetup != file)
						{
							this.core.WriteAttributeString(writer, "SetupFile", fileSetup);
						}
						this.ProcessODBCAttributeTable(writer, key, "ODBCAttribute");
						this.ProcessODBCDataSourceTable(writer, component, key, null);
						writer.WriteEndElement();
					}
				}
			}
		}

		/// <summary>
		/// Process ODBCSourceAttribute table
		/// </summary>
		/// <param name="writer">XmlWriter where the Intermediate should persist itself as XML.</param>
		/// <param name="key">key.</param>
		/// <param name="tableName">name of table.</param>
		private void ProcessODBCAttributeTable(XmlWriter writer, string key, string tableName)
		{
			Hashtable processed;
			processed = this.odbcAttributeProcessed;
			string query = String.Concat("SELECT * FROM `", tableName, "`");
			switch (tableName)
			{
				case "ODBCSourceAttribute":
					query = String.Concat(query, " WHERE `DataSource_`='", key, "'");
					processed = this.odbcSourceAttributeProcessed;
					break;
				case "ODBCAttribute":
					query = String.Concat(query, " WHERE `Driver_`='", key, "'");
					processed = this.odbcAttributeProcessed;
					break;
				default:
					// TODO: throw an exception
					break;
			}

			if (!this.inputDatabase.TableExists(tableName) || processed.Contains(key))
			{
				return;
			}

			using (View odbcAttributeView = this.inputDatabase.OpenExecuteView(query))
			{
				Record odbcAttributeRecord;
				while (odbcAttributeView.Fetch(out odbcAttributeRecord))
				{
					processed[key] = true;
					writer.WriteStartElement("Property");
					this.core.WriteAttributeString(writer, "Id", odbcAttributeRecord[(int)MsiInterop.ODBCAttribute.Attribute]); // non-nullable
					this.core.WriteAttributeString(writer, "Value", odbcAttributeRecord[(int)MsiInterop.ODBCAttribute.Value]); // nullable
					writer.WriteEndElement();
				}
			}
		}

		/// <summary>
		/// Process ODBCDataSource table
		/// </summary>
		/// <param name="writer">XmlWriter where the Intermediate should persist itself as XML.</param>
		/// <param name="component">Enclosing component context.</param>
		/// <param name="parentDriverName">parent driver name.</param>
		/// <param name="keyPath">key path.</param>
		private void ProcessODBCDataSourceTable(XmlWriter writer, string component, string parentDriverName, string keyPath)
		{
			string tableName = "ODBCDataSource";
			if (!this.inputDatabase.TableExists(tableName))
			{
				return;
			}
			string query = String.Concat("SELECT * FROM `", tableName, "` WHERE `Component_`='", component, "'");
			if (null != parentDriverName && 0 < parentDriverName.Length)
			{
				query = String.Concat(query, " AND `DriverDescription`='", parentDriverName, "'");
			}

			using (View odbcDataSourceView = this.inputDatabase.OpenExecuteView(query))
			{
				Record odbcDataSourceRecord;
				while (odbcDataSourceView.Fetch(out odbcDataSourceRecord))
				{
					string dataSource = odbcDataSourceRecord[(int)MsiInterop.ODBCDataSource.DataSource];
					if (!this.odbcDataSourceProcessed.Contains(dataSource))
					{
						this.odbcDataSourceProcessed[dataSource] = true;
						writer.WriteStartElement(tableName);
						this.core.WriteAttributeString(writer, "Id", dataSource);
						this.core.WriteAttributeString(writer, "Name", odbcDataSourceRecord[(int)MsiInterop.ODBCDataSource.Description]); // non-nullable
						string driverName = odbcDataSourceRecord[(int)MsiInterop.ODBCDataSource.DriverDescription]; //non-nullable
						if (null == parentDriverName || parentDriverName != driverName)
						{
							this.core.WriteAttributeString(writer, "DriverName", driverName);
						}
						string registration = odbcDataSourceRecord[(int)MsiInterop.ODBCDataSource.Registration]; //nullable
						string registrationType = null;
						int registrationIndex = Convert.ToInt32(registration);
						switch (registrationIndex)
						{
							case MsiInterop.MsidbODBCDataSourceRegistrationPerMachine:
								registrationType = "machine";
								break;
							case MsiInterop.MsidbODBCDataSourceRegistrationPerUser:
								registrationType = "user";
								break;
							default:
								// TODO: throw an exception
								break;
						}
						this.core.WriteAttributeString(writer, "Registration", registrationType);
						if (null != keyPath && 0 < keyPath.Length && keyPath == dataSource)
						{
							this.core.WriteAttributeString(writer, "KeyPath", "yes");
						}
						this.ProcessODBCAttributeTable(writer, dataSource, "ODBCSourceAttribute");
						writer.WriteEndElement();
					}
				}
			}
		}

		/// <summary>
		/// Process DuplicateFile table
		/// </summary>
		/// <param name="writer">XmlWriter where the Intermediate should persist itself as XML.</param>
		/// <param name="component">Enclosing component context.</param>
		/// <param name="file">file.</param>
		private void ProcessDuplicateFileTable(XmlWriter writer, string component, string file)
		{
			string tableName = "DuplicateFile";
			if (!this.inputDatabase.TableExists(tableName))
			{
				return;
			}
			string query = String.Concat("SELECT * FROM `", tableName, "` WHERE `Component_`='", component, "'");
			if (null != file && 0 < file.Length)
			{
				query = String.Concat(query, " AND `File_`='", file, "'");
			}

			using (View duplicateFileView = this.inputDatabase.OpenExecuteView(query))
			{
				Record duplicateFileRecord;
				while (duplicateFileView.Fetch(out duplicateFileRecord))
				{
					string fileKey = duplicateFileRecord[(int)MsiInterop.DuplicateFile.FileKey];
					if (!this.duplicateFileProcessed.Contains(fileKey))
					{
						this.duplicateFileProcessed[fileKey] = true;
						writer.WriteStartElement("CopyFile");
						if (null == file || 0 == file.Length)
						{
							this.core.WriteAttributeString(writer, "FileId", duplicateFileRecord[(int)MsiInterop.DuplicateFile.File]);
						}
						this.core.WriteAttributeString(writer, "Id", fileKey);
						string fileName = duplicateFileRecord[(int)MsiInterop.DuplicateFile.DestName];
						string[] names = fileName.Split('|');

						string shortName = names[0];
						string longName = shortName;
						if (1 < names.Length)
						{
							longName = names[1];
						}

						if (DecompilerCore.IsValidShortFilename(shortName))
						{
							this.core.WriteAttributeString(writer, "DestinationName", shortName);
							if (longName != shortName)
							{
								this.core.WriteAttributeString(writer, "DestinationLongName", longName);
							}
						}
						else
						{
							shortName = this.core.GetValidShortName(shortName, "DuplicateFile", fileKey, "DestName");
							this.core.WriteAttributeString(writer, "DestinationName", shortName);
							this.core.WriteAttributeString(writer, "DestinationLongName", longName);
						}

						string dirProperty = duplicateFileRecord[(int)MsiInterop.DuplicateFile.DestFolder];
						if (this.inputDatabase.TableExists("Directory"))
						{
							using (View dirView = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `Directory` WHERE `Directory` = '", dirProperty, "'")))
							{
								Record dirRecord;
								if (dirView.Fetch(out dirRecord))
								{
									this.core.WriteAttributeString(writer, "DestinationDirectory", this.StripModuleId(this.core.GetValidIdentifier(dirProperty, "DuplicateFile; Column: DestFolder")));
								}
								else
								{
									this.core.WriteAttributeString(writer, "DestinationProperty", this.StripModuleId(dirProperty));
								}
							}
						}
						writer.WriteEndElement();
					}
				}
			}
		}

		/// <summary>
		/// Process MoveFile table
		/// </summary>
		/// <param name="writer">XmlWriter where the Intermediate should persist itself as XML.</param>
		/// <param name="component">Enclosing component context.</param>
		private void ProcessMoveFileTable(XmlWriter writer, string component)
		{
			string tableName = "MoveFile";
			if (!this.inputDatabase.TableExists(tableName))
			{
				return;
			}

			using (View view = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `", tableName, "` WHERE `Component_` = '", component, "'")))
			{
				Record moveFileRecord;
				while (view.Fetch(out moveFileRecord))
				{
					writer.WriteStartElement("CopyFile");
					this.core.WriteAttributeString(writer, "Id", moveFileRecord[(int)MsiInterop.MoveFile.FileKey]);
					this.core.WriteAttributeString(writer, "SourceName", moveFileRecord[(int)MsiInterop.MoveFile.SourceName]);
					string fileName = moveFileRecord[(int)MsiInterop.MoveFile.DestName];
					string[] names = fileName.Split('|');

					string shortName = names[0];
					string longName = shortName;
					if (1 < names.Length)
					{
						longName = names[1];
					}

					if (DecompilerCore.IsValidShortFilename(shortName))
					{
						this.core.WriteAttributeString(writer, "DestinationName", shortName);
						if (longName != shortName)
						{
							this.core.WriteAttributeString(writer, "DestinationLongName", longName);
						}
					}
					else
					{
						shortName = this.core.GetValidShortName(shortName, "MoveFile", moveFileRecord[(int)MsiInterop.MoveFile.FileKey], "DestName");
						this.core.WriteAttributeString(writer, "DestinationName", shortName);
						this.core.WriteAttributeString(writer, "DestinationLongName", longName);
					}

					string dirProperty = moveFileRecord[(int)MsiInterop.MoveFile.SourceFolder];
					if (this.inputDatabase.TableExists("Directory"))
					{
						using (View dirView = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `Directory` WHERE `Directory` = '", dirProperty, "'")))
						{
							Record dirRecord;
							if (dirView.Fetch(out dirRecord))
							{
								this.core.WriteAttributeString(writer, "SourceDirectory", this.StripModuleId(this.core.GetValidIdentifier(dirProperty, "MoveFile; Column: SourceFolder")));
							}
							else
							{
								this.core.WriteAttributeString(writer, "SourceProperty", this.StripModuleId(dirProperty));
							}
						}
					}
					dirProperty = moveFileRecord[(int)MsiInterop.MoveFile.DestFolder];
					if (this.inputDatabase.TableExists("Directory"))
					{
						using (View dirView = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `Directory` WHERE `Directory` = '", dirProperty, "'")))
						{
							Record dirRecord;
							if (dirView.Fetch(out dirRecord))
							{
								this.core.WriteAttributeString(writer, "DestinationDirectory", this.StripModuleId(this.core.GetValidIdentifier(dirProperty, "MoveFile; Column: DestFolder")));
							}
							else
							{
								this.core.WriteAttributeString(writer, "DestinationProperty", this.StripModuleId(dirProperty));
							}
						}
					}
					string options = moveFileRecord[(int)MsiInterop.MoveFile.Options];
					string delete = "no";
					if (null != options && 0 < options.Length && "1" == options)
					{
						delete = "yes";
					}
					this.core.WriteAttributeString(writer, "Delete", delete);
					writer.WriteEndElement();
				}
			}
		}

		/// <summary>
		/// Process ReserveCost table
		/// </summary>
		/// <param name="writer">XmlWriter where the Intermediate should persist itself as XML.</param>
		/// <param name="component">Enclosing component context.</param>
		private void ProcessReserveCostTable(XmlWriter writer, string component)
		{
			const string tableName = "ReserveCost";
			if (!this.inputDatabase.TableExists(tableName))
			{
				return;
			}
			this.core.OnMessage(WixVerboses.ProcessingReserveCost(null, VerboseLevel.Verbose, component));

			using (View view = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `", tableName, "` WHERE `Component_` = '", component, "'")))
			{
				Record record;
				while (view.Fetch(out record))
				{
					writer.WriteStartElement(tableName);
					this.core.WriteAttributeString(writer, "Directory", this.core.GetValidIdentifier(record[(int)MsiInterop.ReserveCost.ReserveFolder], "ReserveCost; Column: ReserveFolder"));
					this.core.WriteAttributeString(writer, "RunLocal", record[(int)MsiInterop.ReserveCost.ReserveLocal]);
					this.core.WriteAttributeString(writer, "RunFromSource", record[(int)MsiInterop.ReserveCost.ReserveSource]);
					this.core.WriteAttributeString(writer, "Id", record[(int)MsiInterop.ReserveCost.ReserveKey]);
					writer.WriteEndElement();
				}
			}
		}

		/// <summary>
		/// Process IsolatedComponent table
		/// </summary>
		/// <param name="writer">XmlWriter where the Intermediate should persist itself as XML.</param>
		/// <param name="component">Enclosing component context.</param>
		private void ProcessIsolatedComponentTable(XmlWriter writer, string component)
		{
			const string tableName = "IsolatedComponent";
			if (!this.inputDatabase.TableExists(tableName))
			{
				return;
			}

			using (View view = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `", tableName, "` WHERE `Component_Shared` = '", component, "'")))
			{
				Record record;
				while (view.Fetch(out record))
				{
					writer.WriteStartElement("IsolateComponent");
					this.core.WriteAttributeString(writer, "Shared", record[(int)MsiInterop.IsolatedComponent.ComponentApplication]);
					writer.WriteEndElement();
				}
			}
		}

		/// <summary>
		/// Process ShortCut table
		/// </summary>
		/// <param name="writer">XmlWriter where the Intermediate should persist itself as XML.</param>
		/// <param name="component">Enclosing component context.</param>
		private void ProcessShortcutTable(XmlWriter writer, string component)
		{
			const string tableName = "Shortcut";
			if (!this.inputDatabase.TableExists(tableName))
			{
				return;
			}
			this.core.OnMessage(WixVerboses.ProcessingShortcutForComponent(null, VerboseLevel.Verbose, component));

			using (View view = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `", tableName, "` WHERE `Component_` = '", component, "'")))
			{
				Record record;
				while (view.Fetch(out record))
				{
					writer.WriteStartElement(tableName);
					string shortcut = record[(int)MsiInterop.Shortcut.Shortcut];
					this.core.OnMessage(WixVerboses.ProcessingShortcut(null, VerboseLevel.Verbose, shortcut));
					this.core.WriteAttributeString(writer, "Id", shortcut);

					this.core.WriteAttributeString(writer, "Directory", this.StripModuleId(this.core.GetValidIdentifier(record[(int)MsiInterop.Shortcut.Directory], "Shortcut; Column: Directory")));
					string[] names = record[(int)MsiInterop.Shortcut.Name].Split('|');

					string shortName = names[0];
					string longName = shortName;
					if (1 < names.Length)
					{
						longName = names[1];
					}

					if (DecompilerCore.IsValidShortFilename(shortName))
					{
						this.core.WriteAttributeString(writer, "Name", shortName);
						if (longName != shortName)
						{
							this.core.WriteAttributeString(writer, "LongName", longName);
						}
					}
					else
					{
						shortName = this.core.GetValidShortName(shortName, "Shortcut", shortcut, "Name");
						this.core.WriteAttributeString(writer, "Name", shortName);
						this.core.WriteAttributeString(writer, "LongName", longName);
					}

					this.core.WriteAttributeString(writer, "Target", record[(int)MsiInterop.Shortcut.Target]);
					this.core.WriteAttributeString(writer, "Description", record[(int)MsiInterop.Shortcut.Description]);
					this.core.WriteAttributeString(writer, "Arguments", this.StripModuleId(record[(int)MsiInterop.Shortcut.Arguments]));
					this.core.WriteAttributeString(writer, "Hotkey", record[(int)MsiInterop.Shortcut.Hotkey]);
					this.core.WriteAttributeString(writer, "Icon", record[(int)MsiInterop.Shortcut.Icon]);
					this.core.WriteAttributeString(writer, "IconIndex", record[(int)MsiInterop.Shortcut.IconIndex]);
					string show = record[(int)MsiInterop.Shortcut.ShowCmd]; // nullable field
					if (0 < show.Length)
					{
						int showCmd = Convert.ToInt32(show);
						string showCmdValue = "";
						switch (showCmd)
						{
							case MsiInterop.SWSHOWMAXIMIZED:
							case MsiInterop.SWSHOWMINNOACTIVE:
								showCmdValue = "maximized";
								break;
							case MsiInterop.SWSHOWNORMAL:
							default:
								showCmdValue = "normal";
								break;
						}
						this.core.WriteAttributeString(writer, "Show", showCmdValue);
					}
					this.core.WriteAttributeString(writer, "WorkingDirectory", record[(int)MsiInterop.Shortcut.WkDir]);
/* broken and not needed right now
					if (0 < record[(int)MsiInterop.Shortcut.Icon].Length)
					{
						this.ProcessBinaryTable(writer, BinaryType.Icon, record[(int)MsiInterop.Shortcut.Icon]);
					}
*/
					this.core.WriteAttributeString(writer, "DisplayResourceDll", record[(int)MsiInterop.Shortcut.DisplayResourceDLL]);
					this.core.WriteAttributeString(writer, "DisplayResourceId", record[(int)MsiInterop.Shortcut.DisplayResourceId]);
					this.core.WriteAttributeString(writer, "DescriptionResourceDll", record[(int)MsiInterop.Shortcut.DescriptionResourceDLL]);
					this.core.WriteAttributeString(writer, "DescriptionResourceId", record[(int)MsiInterop.Shortcut.DescriptionResourceId]);
					writer.WriteEndElement();
				}
			}
		}

		/// <summary>
		/// Processes Component table for a given directory.
		/// </summary>
		/// <param name="writer">XmlWriter which should will persist the XML.</param>
		/// <param name="directory">Directory node to process Components for.</param>
		/// <param name="rootPathShort">Directory path to process components for.</param>
		/// <param name="rootPathLong">Directory path to process components for.</param>
		/// <param name="record">Record from the component table to process.</param>
		private void ProcessComponentRecord(XmlWriter writer, string directory, string rootPathShort, string rootPathLong, Record record)
		{
			const string tableName = "Component";
			string id = record[(int)MsiInterop.Component.ComponentId];
			string rawComponent = record[(int)MsiInterop.Component.Component];
			string component = this.core.GetValidIdentifier(rawComponent, "Component");

			if ((0 < id.Length && !this.processedComponents.ContainsKey(id)) || !this.processedComponents.ContainsKey(rawComponent))
			{
				string condition = record[(int)MsiInterop.Component.Condition].Trim();

				int bits = Convert.ToInt32(record[(int)MsiInterop.Component.Attributes]); // non-nullable field
				string keyPath = record[(int)MsiInterop.Component.KeyPath];
				string regKeyPath = null;
				string fileKeyPath = null;
				string odbcKeyPath = null;

				if (this.generateFragments)
				{
					writer.WriteStartElement("Fragment");
					if (null != directory)
					{
						writer.WriteStartElement("DirectoryRef");
						string directoryId = this.StripModuleId(directory);
						this.core.WriteAttributeString(writer, "Id", directoryId);
					}
				}
				writer.WriteStartElement(tableName);
				this.core.WriteAttributeString(writer, "Id", this.StripModuleId(component));
				this.core.WriteAttributeString(writer, null, "Guid", this.StripBraces(id), true);

				if (0 < keyPath.Length)
				{
					if (0 < (bits & MsiInterop.MsidbComponentAttributesRegistryKeyPath))
					{
						regKeyPath  = keyPath;
					}
					else if (0 < (bits & MsiInterop.MsidbComponentAttributesODBCDataSource))
					{
						odbcKeyPath = keyPath;
					}
					else
					{
						fileKeyPath = keyPath;
					}
				}
				else // the Component is using the directory as the keypath
				{
					this.core.WriteAttributeString(writer, "KeyPath", "yes");
				}

				if (0 < (bits & MsiInterop.MsidbComponentAttributesSharedDllRefCount))
				{
					this.core.WriteAttributeString(writer, "SharedDllRefCount", "yes");
				}

				if (0 < (bits & MsiInterop.MsidbComponentAttributesPermanent))
				{
					this.core.WriteAttributeString(writer, "Permanent", "yes");
				}

				if (0 < (bits & MsiInterop.MsidbComponentAttributesTransitive))
				{
					this.core.WriteAttributeString(writer, "Transitive", "yes");
				}

				if (0 < (bits & MsiInterop.MsidbComponentAttributesNeverOverwrite))
				{
					this.core.WriteAttributeString(writer, "NeverOverwrite", "yes");
				}

				if (0 < (bits & MsiInterop.MsidbComponentAttributesOptional))
				{
					this.core.WriteAttributeString(writer, "Location", "either");
				}
				else if (0 < (bits & MsiInterop.MsidbComponentAttributesSourceOnly))
				{
					this.core.WriteAttributeString(writer, "Location", "source");
				}

				if (0 < (bits & MsiInterop.MsidbComponentAttributesDisableRegistryReflection))
				{
					this.core.WriteAttributeString(writer, "DisableRegistryReflection", "yes");
				}

				if (this.inputDatabase.TableExists("Complus"))
				{
					using (View complusView = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `Complus` WHERE `Component_`='", rawComponent, "'")))
					{
						Record complusRecord;
						while (complusView.Fetch(out complusRecord))
						{
							this.core.WriteAttributeString(writer, "ComPlusFlags", complusRecord[(int)MsiInterop.Complus.ExpType]);
						}
					}
				}

				// support decompiler extension here
				foreach (DecompilerExtension extension in this.extensionList)
				{
					extension.ExtendAttributesOfElement("Component", rawComponent);
				}

				if (0 < condition.Length)
				{
					writer.WriteStartElement("Condition");
					writer.WriteCData(condition);
					writer.WriteEndElement();
				}

				this.ProcessFileTable(writer, rawComponent, fileKeyPath, rootPathShort, rootPathLong);
				this.ProcessCreateFolderTable(writer, rawComponent, directory);
				this.ProcessRegistryTable(writer, rawComponent, regKeyPath, "Registry");
				this.ProcessRegistryTable(writer, rawComponent, regKeyPath, "RemoveRegistry");
				this.ProcessServiceControlTable(writer, rawComponent);
				this.ProcessServiceInstallTable(writer, rawComponent);
				this.ProcessEnvironmentTable(writer, rawComponent);
				this.ProcessRemoveFileTable(writer, rawComponent);
				this.ProcessReserveCostTable(writer, rawComponent);
				this.ProcessShortcutTable(writer, rawComponent);
				this.ProcessClassTable(writer, rawComponent);
				this.ProcessExtensionTable(writer, rawComponent, null);
				this.ProcessIniFileTable(writer, rawComponent, "IniFile");
				this.ProcessIniFileTable(writer, rawComponent, "RemoveIniFile");
				this.ProcessMoveFileTable(writer, rawComponent);
				this.ProcessDuplicateFileTable(writer, rawComponent, null);
				this.ProcessIsolatedComponentTable(writer, rawComponent);
				this.ProcessTypeLibTable(writer, rawComponent);
				this.ProcessPublishComponentTable(writer, rawComponent);
				this.ProcessODBCRootTable(writer, rawComponent, null, "ODBCDriver");
				this.ProcessODBCRootTable(writer, rawComponent, null, "ODBCTranslator");
				this.ProcessODBCDataSourceTable(writer, rawComponent, null, keyPath);

				// support decompiler extension here
				foreach (DecompilerExtension extension in this.extensionList)
				{
					extension.ExtendChildrenOfElement("Component", rawComponent);
				}

				if (this.generateFragments)
				{
					if (null != directory)
					{
						writer.WriteEndElement(); // close self
					}
					writer.WriteEndElement(); // close parent (sometimes self)
					writer.WriteEndElement(); // close fragment
				}
				else
				{
					writer.WriteEndElement();
				}

				if (0 < id.Length)
				{
					this.processedComponents.Add(id, 1);
				}
				else
				{
					this.processedComponents.Add(rawComponent, 1);
				}
			}
		}

		/// <summary>
		/// Processes Component table for a given directory.
		/// </summary>
		/// <param name="directory">Directory node to process Components for.</param>
		/// <param name="rootPathShort">Directory path to process components for.</param>
		/// <param name="rootPathLong">Directory path to process components for.</param>
		private void ProcessComponentTable(string directory, string rootPathShort, string rootPathLong)
		{
			const string tableName = "Component";
			if (!this.inputDatabase.TableExists(tableName))
			{
				return;
			}

			using (View view = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `", tableName, "` WHERE `Directory_`='", directory, "'")))
			{
				Record record;
				while (view.Fetch(out record))
				{
					string componentName = this.core.GetValidIdentifier(record[(int)MsiInterop.Component.Component], "Component");
					this.ProcessComponentRecord(this.InitializeXmlTextWriter(Path.GetFullPath(Path.Combine(Path.Combine(this.outputFolder, "Components"), String.Concat(componentName, ".wxs")))), directory, rootPathShort, rootPathLong, record);
				}
			}
		}

		/// <summary>
		/// Processes File table for a given component.
		/// </summary>
		/// <param name="writer">XmlWriter where the Intermediate should persist itself as XML.</param>
		/// <param name="component">Component to get files.</param>
		/// <param name="keyPath">File identifier that is marked as the key path for the component.</param>
		/// <param name="componentPathShort">Directory path of components to get files.</param>
		/// <param name="componentPathLong">Directory path of components to get files.</param>
		private void ProcessFileTable(XmlWriter writer, string component, string keyPath, string componentPathShort, string componentPathLong)
		{
			const string tableName = "File";
			if (!this.inputDatabase.TableExists(tableName))
			{
				return;
			}

			using (View view = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `", tableName, "` WHERE `Component_`='", component, "' ORDER BY `Sequence`")))
			{
				Record record;
				while (view.Fetch(out record))
				{
					writer.WriteStartElement(tableName);

					string originalFileId = record[(int)MsiInterop.File.File];
					string fileId = this.core.GetValidIdentifier(originalFileId, "File");
					string[] names = record[(int)MsiInterop.File.FileName].Split('|');
					int sequence = Convert.ToInt32(record[(int)MsiInterop.File.Sequence]); // non-nullable field
					int diskId = 0;
					bool useImagePath = !this.summaryInfoCompressed;

					// pull out attributes early because file decompression needs this
					string attrString = record[(int)MsiInterop.File.Attributes]; // nullable field
					int bits = (null == attrString || 0 == attrString.Length) ? 0 : Convert.ToInt32(attrString);

					// add appropriate DiskId based on Media
					for (int index = 0; index < (int)this.lastSequence.Count; index++)
					{
						int lastSequence = Math.Abs((int)this.lastSequence.GetByIndex(index));

						// index is the diskid of the LastSequence is less than the lastSequence for this diskid
						if (sequence <= lastSequence) // non-nullable field
						{
							diskId = index + 1;

							// negative lastSequence means the file is not in a CAB and diskId zero means look in image
							if (0 > (int)this.lastSequence.GetByIndex(index) || 0 < (bits & MsiInterop.MsidbFileAttributesNoncompressed))
							{
								useImagePath = true;
							}
							if (0 < (bits & MsiInterop.MsidbFileAttributesCompressed))
							{
								useImagePath = false;
							}
							break;
						}

						// short circuit for reaching the last media entry
						if (0 == (int)this.lastSequence.GetByIndex(index))
						{
							break;
						}
					}

					string id = this.StripModuleId(fileId);

					this.core.WriteAttributeString(writer, "Id", id);

					string shortName = names[0];
					string longName = shortName;
					if (1 < names.Length)
					{
						longName = names[1];
					}

					if (DecompilerCore.IsValidShortFilename(shortName))
					{
						this.core.WriteAttributeString(writer, "Name", shortName);
						if (longName != shortName)
						{
							this.core.WriteAttributeString(writer, "LongName", longName);
						}
					}
					else
					{
						shortName = this.core.GetValidShortName(shortName, "File", originalFileId, "FileName");
						this.core.WriteAttributeString(writer, "Name", shortName);
						this.core.WriteAttributeString(writer, "LongName", longName);
					}

					if (this.exportBinaries)
					{
						string extractDir = this.extractFolder;

						if (!this.processingModule)
						{
							extractDir = Path.Combine(extractDir, diskId.ToString());
						}

						if (!Directory.Exists(extractDir))
						{
							Directory.CreateDirectory(extractDir);
						}

						try
						{
							extractDir = FileSystem.FileSystemBase.GetLongPathName(extractDir);
						}
						catch
						{
							// don't care
						}
						string cabFileSpec = Path.Combine(extractDir, originalFileId);
						string wixFileSpec = Path.Combine(extractDir, shortName);
						string wixFileSpecLong = wixFileSpec;
						if (longName != shortName)
						{
							wixFileSpecLong = Path.Combine(extractDir, longName);
						}
						string sourceDir = Path.Combine(this.exportBasePath, componentPathShort);
						string srcFileSpec = Path.Combine(sourceDir, shortName);

						if (useImagePath)
						{
							if (this.summaryInfoCompressed)
							{
								cabFileSpec = Path.Combine(this.imageBasePath, shortName);
								if (!File.Exists(cabFileSpec))
								{
									cabFileSpec = Path.Combine(this.imageBasePath, longName);
								}
							}
							else
							{
								cabFileSpec = Path.Combine(this.imageBasePath, Path.Combine(componentPathShort, shortName));
								if (!File.Exists(cabFileSpec))
								{
									cabFileSpec = Path.Combine(this.imageBasePath, Path.Combine(componentPathLong, longName));
								}
							}
						}

						if (File.Exists(cabFileSpec))
						{
							if (File.Exists(wixFileSpec) && wixFileSpecLong != cabFileSpec)
							{
								this.core.OnMessage(WixWarnings.OverwritingFile(null, WarningLevel.Major, wixFileSpec));
								File.Delete(wixFileSpec);
							}

							if (wixFileSpecLong != cabFileSpec)
							{
								if (useImagePath)
								{
									File.Copy(cabFileSpec, wixFileSpec);
								}
								else
								{
									File.Move(cabFileSpec, wixFileSpec);
								}
							}

							if (!Directory.Exists(sourceDir))
							{
								Directory.CreateDirectory(sourceDir);
							}

							if (File.Exists(srcFileSpec) && wixFileSpec != srcFileSpec)
							{
								this.core.OnMessage(WixWarnings.OverwritingFile(null, WarningLevel.Major, srcFileSpec));
								File.Delete(srcFileSpec);
							}

							if (wixFileSpec != srcFileSpec)
							{
								File.Copy(wixFileSpec, srcFileSpec);
							}
							this.core.WriteAttributeString(writer, "Source", Path.Combine("SourceDir", Path.Combine(componentPathShort, shortName)));
						}
						else
						{
							this.core.OnMessage(WixWarnings.UnableToFindFileFromCabOrImage(null, WarningLevel.Major, cabFileSpec, Path.Combine(componentPathShort, shortName)));
						}
					}

					string version = record[(int)MsiInterop.File.Version]; // nullable field
					if (!DecompilerCore.LegalVersion.IsMatch(version))
					{
						this.core.WriteAttributeString(writer, "CompanionFile", this.StripModuleId(version));
					}

					if (0 < (bits & MsiInterop.MsidbFileAttributesReadOnly))
					{
						this.core.WriteAttributeString(writer, "ReadOnly", "yes");
					}

					if (0 < (bits & MsiInterop.MsidbFileAttributesHidden))
					{
						this.core.WriteAttributeString(writer, "Hidden", "yes");
					}

					if (0 < (bits & MsiInterop.MsidbFileAttributesSystem))
					{
						this.core.WriteAttributeString(writer, "System", "yes");
					}

					if (0 < (bits & MsiInterop.MsidbFileAttributesVital))
					{
						this.core.WriteAttributeString(writer, "Vital", "yes");
					}

					if (0 < (bits & MsiInterop.MsidbFileAttributesChecksum))
					{
						this.core.WriteAttributeString(writer, "Checksum", "yes");
					}

					if (0 < (bits & MsiInterop.MsidbFileAttributesPatchAdded))
					{
						this.core.WriteAttributeString(writer, "PatchGroup", "1");
					}

					if (0 < (bits & MsiInterop.MsidbFileAttributesNoncompressed))
					{
						this.core.WriteAttributeString(writer, "Compressed", "no");
					}

					if (0 < (bits & MsiInterop.MsidbFileAttributesCompressed))
					{
						this.core.WriteAttributeString(writer, "Compressed", "yes");
					}

					if (keyPath == fileId)
					{
						this.core.WriteAttributeString(writer, "KeyPath", "yes");
						this.ProcessMsiAssemblyTable(writer, component);
					}

					if (this.inputDatabase.TableExists("BindImage"))
					{
						using (View bindView = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `BindImage` WHERE `File_`='", fileId, "'")))
						{
							Record bindRecord;
							while (bindView.Fetch(out bindRecord))
							{
								// note: this value may be an empty string
								this.core.WriteAttributeString(writer, null, "BindPath", bindRecord[(int)MsiInterop.BindImage.Path], true);
							}
						}
					}

					if (this.inputDatabase.TableExists("SelfReg"))
					{
						using (View selfRegView = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `SelfReg` WHERE `File_`='", fileId, "'")))
						{
							Record selfRegRecord;
							while (selfRegView.Fetch(out selfRegRecord))
							{
								this.core.WriteAttributeString(writer, "SelfRegCost", selfRegRecord[(int)MsiInterop.SelfReg.Cost]);
							}
						}
					}

					if (this.inputDatabase.TableExists("Font"))
					{
						using (View fontView = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `Font` WHERE `File_`='", fileId, "'")))
						{
							Record fontRecord;
							while (fontView.Fetch(out fontRecord))
							{
								if (null == fontRecord[(int)MsiInterop.Font.FontTitle])
								{
									this.core.WriteAttributeString(writer, "TrueType", "yes");
								}
								else
								{
									this.core.WriteAttributeString(writer, "FontTitle", fontRecord[(int)MsiInterop.Font.FontTitle]);
								}
							}
						}
					}

					if (diskId > 0)
					{
						this.core.WriteAttributeString(writer, "DiskId", diskId.ToString());
					}

					// support decompiler extension here
					foreach (DecompilerExtension extension in this.extensionList)
					{
						extension.ExtendAttributesOfElement("File", fileId);
					}

					this.ProcessLockPermissionsTable(writer, fileId, "File");
					if (this.interpolateRegistry)
					{
						this.ProcessRegistryTable(writer, component, keyPath, "File");
					}
					this.ProcessDuplicateFileTable(writer, component, fileId);
					this.ProcessODBCRootTable(writer, component, fileId, "ODBCDriver");
					this.ProcessODBCRootTable(writer, component, fileId, "ODBCTranslator");

					// REM   <element type='Patch'          minOccurs='0' maxOccurs='*'/><!-- to Patch table -->
					writer.WriteEndElement();
				}
			}
		}

		/// <summary>
		/// Process Class from Registry table
		/// </summary>
		/// <param name="writer">XmlWriter where the Intermediate should persist itself as XML.</param>
		/// <param name="component">Enclosing component context.</param>
		/// <param name="keyPath">keyPath.</param>
		/// <param name="classId">classId.</param>
		private void ProcessClassFromRegistryTable(XmlWriter writer, string component, string keyPath, string classId)
		{
			string tableName = "Registry";
			if (!this.inputDatabase.TableExists(tableName))
			{
				return;
			}
			this.core.OnMessage(WixVerboses.ProcessingClass(null, VerboseLevel.Verbose, classId));
			string classContextList = "";
			string argument = "";
			using (View contextView = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `", tableName, "` WHERE `Component_` = '", component, "'")))
			{
				Record contextRecord;
				while (contextView.Fetch(out contextRecord))
				{
					string contextKey = contextRecord[(int)MsiInterop.Registry.Key];
					string[] hives = contextKey.Split('\\');
					if ("CLSID" == hives[0] && String.Concat("{", classId, "}") == hives[1])
					{
						if (2 < hives.Length)
						{
							if ("LocalServer" == hives[2])
							{
								if (0 < classContextList.Length)
								{
									classContextList += "\t";
								}
								classContextList += "LocalServer";
							}

							if ("LocalServer32" == hives[2])
							{
								if (0 < classContextList.Length)
								{
									classContextList += "\t";
								}
								classContextList += "LocalServer32";
							}

							if ("InprocServer" == hives[2])
							{
								if (0 < classContextList.Length)
								{
									classContextList += "\t";
								}
								classContextList += "InprocServer";
							}

							if ("InprocServer32" == hives[2])
							{
								if (0 < classContextList.Length)
								{
									classContextList += "\t";
								}
								classContextList += "InprocServer32";
							}
							string contextValue = contextRecord[(int)MsiInterop.Registry.Value].Trim('\"');
							string[] values = contextValue.Split(']');
							if (null != keyPath && values[0].EndsWith(keyPath))
							{
								if (1 < values.Length && 0 < values[1].Length)
								{
									argument += contextValue.Substring(values[0].Length + 1).Trim();
								}
							}
						}
					}
				}
			}
			writer.WriteStartElement("Class");
			this.core.WriteAttributeString(writer, "Id", this.StripBraces(classId));
			if (0 < classContextList.Length)
			{
				this.core.WriteAttributeString(writer, "Context", classContextList);
			}

			if (0 < argument.Length)
			{
				this.core.WriteAttributeString(writer, "Argument", argument);
			}

			using (View fileTypeView = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `", tableName, "` WHERE `Component_` = '", component, "'")))
			{
				Record fileTypeRecord;
				while (fileTypeView.Fetch(out fileTypeRecord))
				{
					string fileTypeKey = fileTypeRecord[(int)MsiInterop.Registry.Key];
					string[] hives = fileTypeKey.Split('\\');
					if ("FileType" == hives[0] && String.Concat("{", classId, "}") == hives[1])
					{
						string mask = fileTypeRecord[(int)MsiInterop.Registry.Value];
						string[] values = mask.Split(',');
						writer.WriteStartElement("FileTypeMask");
						this.core.WriteAttributeString(writer, "Offset", values[0]);
						this.core.WriteAttributeString(writer, "Mask", values[2]);
						this.core.WriteAttributeString(writer, "Value", values[3]);
						writer.WriteEndElement();
					}
				}
			}
			writer.WriteEndElement();
		}

		/// <summary>
		/// Process Registry table
		/// </summary>
		/// <param name="writer">XmlWriter where the Intermediate should persist itself as XML.</param>
		/// <param name="component">Enclosing component context.</param>
		/// <param name="keyPath">keyPath.</param>
		/// <param name="tableName">tableName.</param>
		private void ProcessRegistryTable(XmlWriter writer, string component, string keyPath, string tableName)
		{
			if (!this.inputDatabase.TableExists(tableName))
			{
				return;
			}
			string tableNameContext = tableName;
			if ("File" == tableName)
			{
				if (!this.inputDatabase.TableExists("Registry"))
				{
					return;
				}
				bool foundFileReference = false;
				using (View fileReferencedView = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `Registry` WHERE `Component_` = '", component, "'")))
				{
					Record fileReferencedRecord;
					while (fileReferencedView.Fetch(out fileReferencedRecord))
					{
						string fileReferencedValue = fileReferencedRecord[(int)MsiInterop.Registry.Value].Trim('\"');
						string[] hives = fileReferencedValue.Split(']');
						if (null != keyPath && hives[0].EndsWith(keyPath))
						{
							foundFileReference = true;
							tableName = "Registry";
						}
					}
				}

				if (!foundFileReference)
				{
					return;
				}
			}

			using (View view = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `", tableName, "` WHERE `Component_` = '", component, "'")))
			{
				Record record;
				while (view.Fetch(out record))
				{
					int rootNumber = Convert.ToInt32(record[(int)MsiInterop.Registry.Root]); // non-nullable field
					string root = "";

					switch (rootNumber)
					{
						case -1:
							root = "HKMU";
							break;
						case 0:
							root = "HKCR";
							break;
						case 1:
							root = "HKCU";
							break;
						case 2:
							root = "HKLM";
							break;
						case 3:
							root = "HKU";
							break;
						default:
							// TODO: throw an exception
							break;
					}

					if (this.interpolateRegistry && (tableName == "Registry" || tableName == "File") && 0 == rootNumber)
					{
						string appId = "";
						if (this.inputDatabase.TableExists("AppId"))
						{
							using (View appIdsView = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `", tableName, "` WHERE `Component_` = '", component, "' AND `Name` = 'AppID'")))
							{
								Record appIdsRecord;
								while (appIdsView.Fetch(out appIdsRecord))
								{
									appId = appIdsRecord[(int)MsiInterop.Registry.Value];
									if (!this.processedAppIds.ContainsKey(appId))
									{
										this.processedAppIds.Add(appId, true);
										writer.WriteStartElement("AppId");
										this.core.WriteAttributeString(writer, "Id", this.StripBraces(appId));
										using (View classIdsFromAppIdView = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `", tableName, "` WHERE `Component_` = '", component, "' AND `Name` = 'AppID' AND `Value` = '",  appId, "'")))
										{
											Record classIdsFromAppIdRecord;
											while (classIdsFromAppIdView.Fetch(out classIdsFromAppIdRecord))
											{
												string classIdsFromAppIdKey = classIdsFromAppIdRecord[(int)MsiInterop.Registry.Key];
												string classIdFromAppId = null;
												Regex classIdFromAppIdGuidRegex = new Regex(@"([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})", RegexOptions.IgnoreCase);

												// Match the regular expression pattern against a text string.
												Match classIdFromAppIdGuidMatch = classIdFromAppIdGuidRegex.Match(classIdsFromAppIdKey);
												classIdFromAppId = classIdFromAppIdGuidMatch.Value;
												if (!this.processedClasses.ContainsKey(classIdFromAppId))
												{
													this.processedClasses.Add(classIdFromAppId, true);
													this.ProcessClassFromRegistryTable(writer, component, keyPath, classIdFromAppId);
												}
											}
										}

										if (0 < appId.Length)
										{
											writer.WriteEndElement();
										}
									}
								}
							}
						}

						// REM !! need to skip rows used as Attributes for Class element, such as ThreadingModel, TypeLib and Version...
						// REM If key starts with HKCR\CLSID\ then pull off class ID and check to see if it's in Class table
						string key = record[(int)MsiInterop.Registry.Key];
						string classId = null;
						Regex guidRegex = new Regex(@"([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})", RegexOptions.IgnoreCase);

						// Match the regular expression pattern against a text string.
						Match guidMatch = guidRegex.Match(key);
						string[] matchHives = key.Split('\\');
						if ("CLSID" == matchHives[0] && guidMatch.Success)
						{
							classId = guidMatch.Value;
							if (!this.processedClasses.ContainsKey(classId))
							{
								this.processedClasses.Add(classId, true);
								this.ProcessClassFromRegistryTable(writer, component, keyPath, classId);
							}
						}
					}
					else
					{
						if ("RemoveRegistry" == tableName)
						{
							writer.WriteStartElement("Registry");
						}
						else
						{
							writer.WriteStartElement(tableName);
						}
						string regId = record[(int)MsiInterop.Registry.Registry];

						this.core.WriteAttributeString(writer, "Id", this.StripModuleId(regId.ToString()));
						this.core.WriteAttributeString(writer, "Root",  root);
						this.core.WriteAttributeString(writer, "Key", record[(int)MsiInterop.Registry.Key]);
						string name = this.StripModuleId(record[(int)MsiInterop.Registry.Name]);
						if ("RemoveRegistry" == tableName)
						{
							if ("-" == name)
							{
								this.core.WriteAttributeString(writer, "Action", "removeKeyOnInstall");
							}
							else
							{
								this.core.WriteAttributeString(writer, "Action", "remove");
								this.core.WriteAttributeString(writer, "Name", name);
							}
						}
						else
						{
							string filteredValue = this.StripModuleId(record[(int)MsiInterop.Registry.Value]);
							if (0 < filteredValue.Length)
							{
								this.core.WriteAttributeString(writer, "Name", name);
							}
							else
							{
								if ("Registry" == tableName && "*" == name)
								{
									this.core.WriteAttributeString(writer, "Action", "createKeyAndRemoveKeyOnUninstall");
								}
								else if ("Registry" == tableName && "-" == name)
								{
									this.core.WriteAttributeString(writer, "Action", "removeKeyOnUninstall");
								}
								else if ("Registry" == tableName && "+" == name)
								{
									this.core.WriteAttributeString(writer, "Action", "createKey");
								}
								else
								{
									this.core.WriteAttributeString(writer, "Name", name);
								}
							}

							/*
							<xs:attribute name="Type">
								<xs:simpleType>
									<xs:restriction base="xs:NMTOKEN">
										<xs:enumeration value="string"/>
										<xs:enumeration value="integer"/>
										<xs:enumeration value="binary"/>
										<xs:enumeration value="expandable"/>
										<xs:enumeration value="multiString"/>
									</xs:restriction>
								</xs:simpleType>
							</xs:attribute>

							#x The value is interpreted and stored as a hexadecimal value (REG_BINARY).
							#% The value is interpreted and stored as an expandable string (REG_EXPAND_SZ).
							# The value is interpreted and stored as an integer (REG_DWORD).
							If the value begins with two or more consecutive number signs (#), the first
							# is ignored and value is interpreted and stored as a string.

							If the value contains the sequence tilde [~], then the value is interpreted as
							a Null-delimited list of strings (REG_MULTI_SZ). For example, to specify a list
							containing the three strings a, b and c, use "a[~]b[~]c."
							*/
							string type = "string";

							// #x The value is interpreted and stored as a hexadecimal value (REG_BINARY).
							if (filteredValue.StartsWith("#x"))
							{
								filteredValue = filteredValue.Remove(0, 2);
								type = "binary";
							}

								// #% The value is interpreted and stored as an expandable string (REG_EXPAND_SZ).
							else if (filteredValue.StartsWith("#%"))
							{
								filteredValue = filteredValue.Remove(0, 2);
								type = "expandable";
							}

								// # The value is interpreted and stored as an integer (REG_DWORD).
							else if (filteredValue.StartsWith("#"))
							{
								filteredValue = filteredValue.Remove(0, 1);

								// If the value begins with two or more consecutive number signs (#), the first
								//   # is ignored and value is interpreted and stored as a string.
								if (!filteredValue.StartsWith("#"))
								{
									type = "integer";
								}
							}
							else if (0 < filteredValue.Length && -1 < filteredValue.IndexOf("[~]"))
							{
								type = "multiString";
								if (filteredValue.StartsWith("[~]") && !filteredValue.EndsWith("[~]"))
								{
									this.core.WriteAttributeString(writer, "Action", "append");
								}
								else if (filteredValue.EndsWith("[~]") && !filteredValue.StartsWith("[~]"))
								{
									this.core.WriteAttributeString(writer, "Action", "prepend");
								}
							}

							if (0 < filteredValue.Length)
							{
								this.core.WriteAttributeString(writer, "Type", type);
							}

							if (keyPath != null)
							{
								if (regId.ToString() == keyPath)
								{
									this.core.WriteAttributeString(writer, "KeyPath", "yes");
								}

								if (keyPath.Length == 0)
								{
									this.core.WriteAttributeString(writer, "KeyPath", "no");
								}
							}

							if (type != "multiString")
							{
								this.core.WriteAttributeString(writer, "Value", filteredValue);
							}
							else
							{
								Regex regexStrings = new Regex("(\\[~])");
								MatchCollection stringsMatches = regexStrings.Matches(filteredValue);
								int startOfString = 0;
								int endOfString = filteredValue.Length;
								foreach (Match stringMatch in stringsMatches)
								{
									// handle the starting null
									if (0 == stringMatch.Index)
									{
										startOfString = 3;
										continue;
									}
									endOfString = stringMatch.Index;
									writer.WriteStartElement("RegistryValue");
									this.core.WriteString(writer, filteredValue.Substring(startOfString, endOfString - startOfString));
									writer.WriteEndElement();

									// handle the ending null
									if (filteredValue.Length == stringMatch.Index + stringMatch.Length)
									{
										startOfString = filteredValue.Length + 1;
									}
									else
									{
										startOfString = stringMatch.Index + 3;
									}
								}

								if (startOfString < filteredValue.Length)
								{
									writer.WriteStartElement("RegistryValue");
									this.core.WriteString(writer, filteredValue.Substring(startOfString, filteredValue.Length - startOfString));
									writer.WriteEndElement();
								}
							}
						}
						this.ProcessLockPermissionsTable(writer, regId.ToString(), "Registry");
						writer.WriteEndElement();
					}
				}
			}
		}

		/// <summary>
		/// Process CreateFolder table
		/// </summary>
		/// <param name="writer">XmlWriter where the Intermediate should persist itself as XML.</param>
		/// <param name="component">Enclosing component context.</param>
		/// <param name="directory">Enclosing directory context.</param>
		private void ProcessCreateFolderTable(XmlWriter writer, string component, string directory)
		{
			const string tableName = "CreateFolder";

			if (!this.inputDatabase.TableExists(tableName))
			{
				return;
			}

			using (View view = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `", tableName, "` WHERE `Component_` = '", component, "'")))
			{
				Record record;
				while (view.Fetch(out record))
				{
					writer.WriteStartElement(tableName);
					if (record[(int)MsiInterop.CreateFolder.Directory] != directory)
					{
						this.core.WriteAttributeString(writer, "Directory", this.core.GetValidIdentifier(record[(int)MsiInterop.CreateFolder.Directory], "CreateFolder; Column: Directory"));
					}
					this.ProcessLockPermissionsTable(writer, record[(int)MsiInterop.CreateFolder.Directory], "CreateFolder");
					writer.WriteEndElement();
				}
			}
		}

		/// <summary>
		/// Process CCPSearch table
		/// </summary>
		private void ProcessCCPSearchTable()
		{
			const string tableName = "CCPSearch";
			XmlWriter writer = null;

			if (!this.inputDatabase.TableExists(tableName))
			{
				return;
			}

			if (this.inputDatabase.TableExists("DrLocator"))
			{
				using (View dirLocatorView = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `DrLocator` WHERE `Parent`='CCP_DRIVE'")))
				{
					Record dirLocatorRecord;
					if (dirLocatorView.Fetch(out dirLocatorRecord))
					{
						using (View rmccpView = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `", tableName, "`")))
						{
							Record rmccpRecord;
							while (rmccpView.Fetch(out rmccpRecord))
							{
								if (null == writer)
								{
									writer = this.InitializeXmlTextWriter(Path.GetFullPath(Path.Combine(this.outputFolder, "BaseLibrary\\ComplianceCheck.wxs")));
									writer.WriteStartElement("ComplianceCheck");
									writer.WriteStartElement("ComplianceDrive");
								}
								string signature = rmccpRecord[(int)MsiInterop.CCPSearch.Signature];
								Hashtable rememberedSignatures = new Hashtable();
								rememberedSignatures[signature] = true;

								// call the search processors on the signature
								this.ProcessSignatureKeys(writer, rememberedSignatures, true);
							}
						}

						if (null != writer)
						{
							this.core.Writer.WriteEndElement();
						}
					}
				}
			}

			using (View ccpView = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `", tableName, "`")))
			{
				Record ccpRecord;
				while (ccpView.Fetch(out ccpRecord))
				{
					if (null == writer)
					{
						writer = this.InitializeXmlTextWriter(Path.GetFullPath(Path.Combine(this.outputFolder, "BaseLibrary\\ComplianceCheck.wxs")));
						writer.WriteStartElement("ComplianceCheck");
					}
					string signature = ccpRecord[(int)MsiInterop.CCPSearch.Signature];
					Hashtable rememberedSignatures = new Hashtable();
					rememberedSignatures[signature] = true;

					// call the search processors on the signature
					this.ProcessSignatureKeys(writer, rememberedSignatures, false);
				}
			}

			if (null != writer)
			{
				this.core.Writer.WriteEndElement();
			}
		}

		/// <summary>
		/// Process Upgrade table
		/// </summary>
		private void ProcessUpgradeTable()
		{
			const string tableName = "Upgrade";

			if (!this.inputDatabase.TableExists(tableName))
			{
				return;
			}

			using (View view = this.inputDatabase.OpenExecuteView(String.Concat("SELECT DISTINCT UpgradeCode FROM `", tableName, "`")))
			{
				Record record;
				bool firstRecord = false;
				while (view.Fetch(out record))
				{
					if (!firstRecord)
					{
						firstRecord = true;
						this.core.WriteComment("Package Upgrade Table (or Not-Yet-Bound Upgrade Table)");
					}
					string upgradeCode = record[(int)MsiInterop.Upgrade.UpgradeCode];  // non-nullable
					this.core.Writer.WriteStartElement("Upgrade");
					this.core.WriteAttributeString("Id", this.StripBraces(upgradeCode));
					using (View perCodeView = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `", tableName, "` WHERE UpgradeCode='", upgradeCode, "'")))
					{
						Record perCodeRecord;
						while (perCodeView.Fetch(out perCodeRecord))
						{
							this.core.Writer.WriteStartElement("UpgradeVersion");

							// VersionMin is nullable
							this.core.WriteAttributeString("Minimum", perCodeRecord[(int)MsiInterop.Upgrade.VersionMin]);

							// VersionMax is nullable
							this.core.WriteAttributeString("Maximum", perCodeRecord[(int)MsiInterop.Upgrade.VersionMax]);

							// Language is nullable
							this.core.WriteAttributeString("Language", perCodeRecord[(int)MsiInterop.Upgrade.Language]);

							// Remove is nullable
							this.core.WriteAttributeString("RemoveFeatures", perCodeRecord[(int)MsiInterop.Upgrade.Remove]);

							// ActionProperty is non-nullable
							string property = perCodeRecord[(int)MsiInterop.Upgrade.ActionProperty];
							this.core.WriteAttributeString("Property", property);

							// Attributes is non-nullable
							string attributes = perCodeRecord[(int)MsiInterop.Upgrade.Attributes];
							int bits = 0;
							if (0 < attributes.Length)
							{
								bits = Convert.ToInt32(attributes);
							}

							if (0 < (bits & MsiInterop.MsidbUpgradeAttributesMigrateFeatures))
							{
								this.core.WriteAttributeString("MigrateFeatures", "yes");
							}

							if (0 < (bits & MsiInterop.MsidbUpgradeAttributesOnlyDetect))
							{
								this.core.WriteAttributeString("OnlyDetect", "yes");
							}

							if (0 < (bits & MsiInterop.MsidbUpgradeAttributesIgnoreRemoveFailure))
							{
								this.core.WriteAttributeString("IgnoreRemoveFailure", "yes");
							}

							if (0 < (bits & MsiInterop.MsidbUpgradeAttributesVersionMinInclusive))
							{
								this.core.WriteAttributeString("IncludeMinimum", "yes");
							}

							if (0 < (bits & MsiInterop.MsidbUpgradeAttributesVersionMaxInclusive))
							{
								this.core.WriteAttributeString("IncludeMaximum", "yes");
							}

							if (0 < (bits & MsiInterop.MsidbUpgradeAttributesLanguagesExclusive))
							{
								this.core.WriteAttributeString("ExcludeLanguages", "yes");
							}

							// support decompiler extension here
							foreach (DecompilerExtension extension in this.extensionList)
							{
								extension.ExtendAttributesOfElement("Upgrade", upgradeCode);
							}

							this.core.Writer.WriteEndElement();
							this.EmitProperty(this.core.Writer, this.nonuiproperties, property);
						}
					}
					this.core.Writer.WriteEndElement();
				}
			}
		}

		/// <summary>
		/// Process Servicecontrol table
		/// </summary>
		/// <param name="writer">XmlWriter where the Intermediate should persist itself as XML.</param>
		/// <param name="component">Enclosing component context.</param>
		private void ProcessServiceControlTable(XmlWriter writer, string component)
		{
			const string tableName = "ServiceControl";

			if (!this.inputDatabase.TableExists(tableName))
			{
				return;
			}

			using (View view = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `", tableName, "` WHERE `Component_` = '", component, "'")))
			{
				Record record;
				while (view.Fetch(out record))
				{
					writer.WriteStartElement(tableName);
					this.core.WriteAttributeString(writer, "Id", this.StripModuleId(record[(int)MsiInterop.ServiceControl.ServiceControl]));
					this.core.WriteAttributeString(writer, "Name", record[(int)MsiInterop.ServiceControl.Name]);
					int bits = Convert.ToInt32(record[(int)MsiInterop.ServiceControl.Event]); // non-nullable field
					if (((bits & MsiInterop.MsidbServiceControlEventStart) == MsiInterop.MsidbServiceControlEventStart) &&
						((bits & MsiInterop.MsidbServiceControlEventUninstallStart) == MsiInterop.MsidbServiceControlEventUninstallStart))
					{
						this.core.WriteAttributeString(writer, "Start", "both");
					}
					else if (0 < (bits & MsiInterop.MsidbServiceControlEventUninstallStart))
					{
						this.core.WriteAttributeString(writer, "Start", "uninstall");
					}
					else if (0 < (bits & MsiInterop.MsidbServiceControlEventStart))
					{
						this.core.WriteAttributeString(writer, "Start", "install");
					}

					if (((bits & MsiInterop.MsidbServiceControlEventStop) == MsiInterop.MsidbServiceControlEventStop) &&
						((bits & MsiInterop.MsidbServiceControlEventUninstallStop) == MsiInterop.MsidbServiceControlEventUninstallStop))
					{
						this.core.WriteAttributeString(writer, "Stop", "both");
					}
					else if (0 < (bits & MsiInterop.MsidbServiceControlEventUninstallStop))
					{
						this.core.WriteAttributeString(writer, "Stop", "uninstall");
					}
					else if (0 < (bits & MsiInterop.MsidbServiceControlEventStop))
					{
						this.core.WriteAttributeString(writer, "Stop", "install");
					}

					if (((bits & MsiInterop.MsidbServiceControlEventDelete) == MsiInterop.MsidbServiceControlEventDelete) &&
						((bits & MsiInterop.MsidbServiceControlEventUninstallDelete) == MsiInterop.MsidbServiceControlEventUninstallDelete))
					{
						this.core.WriteAttributeString(writer, "Remove", "both");
					}
					else if (0 < (bits & MsiInterop.MsidbServiceControlEventUninstallDelete))
					{
						this.core.WriteAttributeString(writer, "Remove", "uninstall");
					}
					else if (0 < (bits & MsiInterop.MsidbServiceControlEventDelete))
					{
						this.core.WriteAttributeString(writer, "Remove", "install");
					}

					if (0 < record[(int)MsiInterop.ServiceControl.Wait].Length)
					{
						if ("1" == record[(int)MsiInterop.ServiceControl.Wait])
						{
							this.core.WriteAttributeString(writer, "Wait", "yes");
						}

						if ("0" == record[(int)MsiInterop.ServiceControl.Wait])
						{
							this.core.WriteAttributeString(writer, "Wait", "no");
						}
					}
					this.ProcessServiceControlArguments(writer, record[(int)MsiInterop.ServiceControl.Arguments]);
					writer.WriteEndElement();
				}
			}
		}

		/// <summary>
		/// Process ServiceControl Arguments
		/// </summary>
		/// <param name="writer">XmlWriter where the Intermediate should persist itself as XML.</param>
		/// <param name="arguments">arguments.</param>
		private void ProcessServiceControlArguments(XmlWriter writer, string arguments)
		{
			if (0 < arguments.Length)
			{
				string argument = arguments;
				int seperatorPosition = 0;
				while (-1 < (seperatorPosition = arguments.IndexOf("[~]")))
				{
					argument = arguments.Substring(0, seperatorPosition);
					arguments = arguments.Substring(seperatorPosition + 3);
					writer.WriteStartElement("ServiceArgument");
					this.core.WriteString(writer, argument);
					writer.WriteEndElement();
				}
				writer.WriteStartElement("ServiceArgument");
				this.core.WriteString(writer, argument);
				writer.WriteEndElement();
			}
		}

		/// <summary>
		/// Process ServiceInstall table
		/// </summary>
		/// <param name="writer">XmlWriter where the Intermediate should persist itself as XML.</param>
		/// <param name="component">Enclosing component context.</param>
		private void ProcessServiceInstallTable(XmlWriter writer, string component)
		{
			const string tableName = "ServiceInstall";

			if (!this.inputDatabase.TableExists(tableName))
			{
				return;
			}

			using (View view = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `", tableName, "` WHERE `Component_` = '", component, "'")))
			{
				Record record;
				while (view.Fetch(out record))
				{
					writer.WriteStartElement(tableName);
					this.core.WriteAttributeString(writer, "Id", this.StripModuleId(record[(int)MsiInterop.ServiceInstall.ServiceInstall]));
					this.core.WriteAttributeString(writer, "Name", record[(int)MsiInterop.ServiceInstall.Name]);
					this.core.WriteAttributeString(writer, "DisplayName", record[(int)MsiInterop.ServiceInstall.DisplayName]);
					int bits = Convert.ToInt32(record[(int)MsiInterop.ServiceInstall.ServiceType]); // non-nullable field
					if ((bits & MsiInterop.MsidbServiceInstallShareProcess) == MsiInterop.MsidbServiceInstallShareProcess)
					{
						this.core.WriteAttributeString(writer, "Type", "shareProcess");
					}
					else if ((bits & MsiInterop.MsidbServiceInstallOwnProcess) == MsiInterop.MsidbServiceInstallOwnProcess)
					{
						this.core.WriteAttributeString(writer, "Type", "ownProcess");
					}

					if ((bits & MsiInterop.MsidbServiceInstallInteractive) == MsiInterop.MsidbServiceInstallInteractive)
					{
						this.core.WriteAttributeString(writer, "Interactive", "yes");
					}
					bits = Convert.ToInt32(record[(int)MsiInterop.ServiceInstall.StartType]); // non-nullable field
					if ((bits & MsiInterop.MsidbServiceInstallDisabled) == MsiInterop.MsidbServiceInstallDisabled)
					{
						this.core.WriteAttributeString(writer, "Start", "disabled");
					}
					else if ((bits & MsiInterop.MsidbServiceInstallDemandStart) == MsiInterop.MsidbServiceInstallDemandStart)
					{
						this.core.WriteAttributeString(writer, "Start", "demand");
					}
					else if ((bits & MsiInterop.MsidbServiceInstallAutoStart) == MsiInterop.MsidbServiceInstallAutoStart)
					{
						this.core.WriteAttributeString(writer, "Start", "auto");
					}
					bits = Convert.ToInt32(record[(int)MsiInterop.ServiceInstall.ErrorControl]); // non-nullable field
					if ((bits & MsiInterop.MsidbServiceInstallErrorCritical) == MsiInterop.MsidbServiceInstallErrorCritical)
					{
						this.core.WriteAttributeString(writer, "ErrorControl", "critical");
					}
					else if ((bits & MsiInterop.MsidbServiceInstallErrorNormal) == MsiInterop.MsidbServiceInstallErrorNormal)
					{
						this.core.WriteAttributeString(writer, "ErrorControl", "normal");
					}
					else if ((bits & MsiInterop.MsidbServiceInstallErrorIgnore) == MsiInterop.MsidbServiceInstallErrorIgnore)
					{
						this.core.WriteAttributeString(writer, "ErrorControl", "ignore");
					}

					if ((bits & MsiInterop.MsidbServiceInstallErrorControlVital) == MsiInterop.MsidbServiceInstallErrorControlVital)
					{
						this.core.WriteAttributeString(writer, "Vital", "yes");
					}

					this.core.WriteAttributeString(writer, "LocalGroup", record[(int)MsiInterop.ServiceInstall.LoadOrderGroup]);
					this.core.WriteAttributeString(writer, "Account", record[(int)MsiInterop.ServiceInstall.StartName]);
					this.core.WriteAttributeString(writer, "Password", record[(int)MsiInterop.ServiceInstall.Password]);
					this.core.WriteAttributeString(writer, "Arguments", record[(int)MsiInterop.ServiceInstall.Arguments]);
					if (0 < record[(int)MsiInterop.ServiceInstall.Description].Length)
					{
						if (record[(int)MsiInterop.ServiceInstall.Description] == "[~]")
						{
							this.core.WriteAttributeString(writer, "EraseDescription", "yes");
						}
						else
						{
							this.core.WriteAttributeString(writer, "Description", record[(int)MsiInterop.ServiceInstall.Description]);
						}
					}
					this.ProcessServiceInstallDependencies(writer, record[(int)MsiInterop.ServiceInstall.Dependencies]);
					writer.WriteEndElement();
				}
			}
		}

		/// <summary>
		/// Process ServiceInstall dependencies
		/// </summary>
		/// <param name="writer">XmlWriter where the Intermediate should persist itself as XML.</param>
		/// <param name="dependencies">Dependencies.</param>
		private void ProcessServiceInstallDependencies(XmlWriter writer, string dependencies)
		{
			if (0 < dependencies.Length)
			{
				string[] deps = dependencies.Split("[~]".ToCharArray());
				foreach (string dep in deps)
				{
					if (0 < dep.Length)
					{
						string text = dep;
						writer.WriteStartElement("ServiceDependency");
						if (text.Substring(0, 1) == "+")
						{
							this.core.WriteAttributeString(writer, "Group", "yes");
							text = text.Substring(1);
						}
						this.core.WriteAttributeString(writer, "Id", this.StripModuleId(text));
						writer.WriteEndElement();
					}
				}
			}
		}

		/// <summary>
		/// Process LockPermissions table
		/// </summary>
		/// <param name="writer">XmlWriter where the Intermediate should persist itself as XML.</param>
		/// <param name="argTableKey">Table Key.</param>
		/// <param name="argTableName">Table Name.</param>
		private void ProcessLockPermissionsTable(XmlWriter writer, string argTableKey, string argTableName)
		{
			const string tableName = "LockPermissions";
			if (!this.inputDatabase.TableExists(tableName))
			{
				return;
			}
			string[] specialPermissions = MsiInterop.FilePermissions;
			switch (argTableName)
			{
				case "File":
					specialPermissions = MsiInterop.FilePermissions;
					break;
				case "CreateFolder":
					specialPermissions = MsiInterop.FolderPermissions;
					break;
				case "Registry":
					specialPermissions = MsiInterop.RegistryPermissions;
					break;
				default:
					// TODO: throw an exception - Fail "Invalid parent table name for LockPermissions entry: " & tableName
					break;
			}

			using (View view = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `", tableName, "` WHERE `LockObject` = '", argTableKey, "' AND `Table`= '", argTableName, "'")))
			{
				Record record;
				while (view.Fetch(out record))
				{
					writer.WriteStartElement("Permission");
					this.core.WriteAttributeString(writer, "User", record[(int)MsiInterop.LockPermissions.User]);
					this.core.WriteAttributeString(writer, "Domain", record[(int)MsiInterop.LockPermissions.Domain]);
					if (0 >= record[(int)MsiInterop.LockPermissions.Permission].Length)
					{
						this.core.OnMessage(WixWarnings.NullLockPermissionPermission(null, WarningLevel.Major));
					}
					int bits = Convert.ToInt32(record[(int)MsiInterop.LockPermissions.Permission]); // nullable field, though reserved for future use
					for (int index = 0; index < 32; index++)
					{
						if (((bits >> index) & 1) != 0)
						{
							string name = null;
							if (index < 16 && index < specialPermissions.Length)
							{
								name = specialPermissions[index];
							}
							else if ((index < 28) && ((index - 16) < MsiInterop.StandardPermissions.Length))
							{
								name = MsiInterop.StandardPermissions[index - 16];
							}
							else if ((-1 < (index - 28)) && ((index - 28) < MsiInterop.GenericPermissions.Length))
							{
								name = MsiInterop.GenericPermissions[index - 28];
							}

							if (name == null)
							{
								this.core.OnMessage(WixWarnings.UnknownPermission(null, WarningLevel.Minor, index, argTableKey));
								continue;
							}
							this.core.WriteAttributeString(writer, name, "yes");
						}
					}
					writer.WriteEndElement();
				}
			}
		}

		/// <summary>
		/// Processes the Media table.
		/// </summary>
		private void ProcessMediaTable()
		{
			const string tableName = "Media";
			if (!this.inputDatabase.TableExists(tableName))
			{
				return;
			}
			else if (this.processingModule)
			{
				this.core.OnMessage(WixWarnings.IllegalMergeModuleTable(null, WarningLevel.Major, "Media"));
				return;
			}

			using (View view = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `", tableName, "` ORDER BY `DiskId`")))
			{
				string str;
				Record record;
				while (view.Fetch(out record))
				{
					this.core.Writer.WriteStartElement(tableName);
					this.core.WriteAttributeString("Id", record[(int)MsiInterop.Media.DiskId]);
					this.core.WriteAttributeString("DiskPrompt", record[(int)MsiInterop.Media.DiskPrompt]);
					str = record[(int)MsiInterop.Media.Cabinet];
					if (0 < str.Length)
					{
						if (str.StartsWith("#"))
						{
							str = str.Substring(1);
							this.core.WriteAttributeString("EmbedCab", "yes");
						}
						this.core.WriteAttributeString("Cabinet", str);
					}
					this.core.WriteAttributeString("VolumeLabel", record[(int)MsiInterop.Media.VolumeLabel]);
					this.core.WriteAttributeString("Source", record[(int)MsiInterop.Media.Source]);
					this.core.Writer.WriteEndElement();
				}
			}
		}

		/// <summary>
		/// Process MsiDigitalSignature table
		/// </summary>
		/// <param name="writer">XmlWriter where the Intermediate should persist itself as XML.</param>
		/// <param name="diskId">diskId key reference from the callers context</param>
		private void ProcessMsiDigitalSignatureTable(XmlWriter writer, string diskId)
		{
			const string tableName = "MsiDigitalSignature";
			if (!this.inputDatabase.TableExists(tableName))
			{
				return;
			}

			using (View view = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `", tableName, "` WHERE `Table` = 'Media' AND `SignObject` = '", diskId, "'")))
			{
				Record record;
				while (view.Fetch(out record))
				{
					writer.WriteStartElement("DigitalSignature");

					// TODO: extract binary from column record[(int)MsiInterop.MsiDigitalSignature.Hash] into file system and mark up src attribute
					writer.WriteEndElement();
				}
			}
		}

		/// <summary>
		/// Process MsiDigitalCertificate table
		/// </summary>
		/// <param name="writer">XmlWriter where the Intermediate should persist itself as XML.</param>
		/// <param name="digitalCertificate">digitalCertificate key from the caller's context</param>
		private void ProcessMsiDigitalCertificateTable(XmlWriter writer, string digitalCertificate)
		{
			const string tableName = "MsiDigitalCertificate";
			if (!this.inputDatabase.TableExists(tableName))
			{
				return;
			}

			using (View view = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `", tableName, "` WHERE `DigitalCertificate` = '", digitalCertificate, "'")))
			{
				Record record;
				while (view.Fetch(out record))
				{
					writer.WriteStartElement("DigitalSignature");

					// TODO: extract binary from column record[(int)MsiInterop.MsiDigitalCertificate.Hash] into file system and mark up src attribute
					writer.WriteEndElement();
				}
			}
		}

		/// <summary>
		/// Process MsiPatchCertificate table
		/// </summary>
		/// <param name="writer">XmlWriter where the Intermediate should persist itself as XML.</param>
		private void ProcessMsiPatchCertificateTable(XmlWriter writer)
		{
			const string tableName = "MsiPatchCertificate";
			if (!this.inputDatabase.TableExists(tableName))
			{
				return;
			}

			using (View view = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `", tableName, "`")))
			{
				Record record;
				while (view.Fetch(out record))
				{
					writer.WriteStartElement("PatchCertificates");

					// CONSIDER: What if the record[(int)MsiInterop.MsiPatchCertificate.DigitalCertificate] != record[(int)MsiInterop.MsiPatchCertificate.PatchCertificate]?
					string digitalCertificate = record[(int)MsiInterop.MsiPatchCertificate.DigitalCertificate];

					// CONSIDER: DigitalCertificate_ column is non-nullable but the MSI may not have complied
					this.ProcessMsiDigitalCertificateTable(writer, digitalCertificate);
					writer.WriteEndElement();
				}
			}
		}

		/// <summary>
		/// Processes the feature record under the specified parent.
		/// </summary>
		/// <param name="parentWriter">XmlWriter where the Intermediate should persist itself as XML.</param>
		/// <param name="record">record from the custom action table that needs to be estruded as XML.</param>
		private void ProcessCustomActionRecord(XmlWriter parentWriter, Record record)
		{
			const string tableName = "CustomAction";

			XmlWriter writer = parentWriter;
			string id = this.StripModuleId(this.core.GetValidIdentifier(record[(int)MsiInterop.CustomAction.Action], "CustomAction; Column: Action"));
			if (null == writer)
			{
				writer = this.InitializeXmlTextWriter(Path.GetFullPath(Path.Combine(this.outputFolder, String.Concat("BaseLibrary\\CustomActions\\", id, ".wxs"))));
			}

			if (this.generateFragments && null == parentWriter)
			{
				writer.WriteStartElement("Fragment");
				this.core.WriteAttributeString(writer, "Id", id);
			}

			string source = record[(int)MsiInterop.CustomAction.Source];
			string target = record[(int)MsiInterop.CustomAction.Target];

			// type is a non-nullable field
			int bits = Convert.ToInt32(record[(int)MsiInterop.CustomAction.Type]);

			writer.WriteStartElement(tableName);
			this.core.WriteAttributeString(writer, "Id", id);

			string returnType = null;
			switch (bits & MsiInterop.MsidbCustomActionTypeReturnBits)
			{
				case 0:
					returnType = "check";
					break;
				case MsiInterop.MsidbCustomActionTypeContinue:
					returnType = "ignore";
					break;
				case MsiInterop.MsidbCustomActionTypeAsync:
					returnType = "asyncWait";
					break;
				case MsiInterop.MsidbCustomActionTypeAsync + MsiInterop.MsidbCustomActionTypeContinue:
					returnType = "asyncNoWait";
					break;
				default:
					// TODO: throw an exception
					break;
			}
			this.core.WriteAttributeString(writer, "Return", returnType);

			string execute = null;
			switch (bits & MsiInterop.MsidbCustomActionTypeExecuteBits)
			{
				case 0:
					execute = null;
					break;
				case MsiInterop.MsidbCustomActionTypeFirstSequence:
					execute = "firstSequence";
					break;
				case MsiInterop.MsidbCustomActionTypeOncePerProcess:
					execute = "oncePerProcess";
					break;
				case MsiInterop.MsidbCustomActionTypeClientRepeat:
					execute = "secondSequence";
					break;
				case MsiInterop.MsidbCustomActionTypeInScript:
					execute = "deferred";
					break;
				case MsiInterop.MsidbCustomActionTypeInScript + MsiInterop.MsidbCustomActionTypeRollback:
					execute = "rollback";
					break;
				case MsiInterop.MsidbCustomActionTypeInScript + MsiInterop.MsidbCustomActionTypeCommit:
					execute = "commit";
					break;
				case 7:
				default:
					// TODO: throw an exception
					break;
			}
			this.core.WriteAttributeString(writer, "Execute", execute);

			if (0 < (bits & MsiInterop.MsidbCustomActionTypeNoImpersonate))
			{
				this.core.WriteAttributeString(writer, "Impersonate", "no");
			}

			string name = null;
			string customActionType = null;
			switch (bits & MsiInterop.MsidbCustomActionTypeSourceBits)
			{
				case MsiInterop.MsidbCustomActionTypeBinaryData:
					name = "BinaryKey";
					break;
				case MsiInterop.MsidbCustomActionTypeSourceFile:
					name = "FileKey";
					break;
				case MsiInterop.MsidbCustomActionTypeDirectory:
					name = "Directory";
					break;
				case MsiInterop.MsidbCustomActionTypeProperty:
					name = "Property";
					break;
				default:
					// TODO: throw an exception
					break;
			}

			bool canBeEmptyCustomActionTypeValue = false;
			switch (bits & MsiInterop.MsidbCustomActionTypeTypeBits)
			{
				case MsiInterop.MsidbCustomActionTypeDll:
					customActionType = "DllEntry";
					break;
				case MsiInterop.MsidbCustomActionTypeExe:
					canBeEmptyCustomActionTypeValue = true;
					customActionType = "ExeCommand";
					break;
				case MsiInterop.MsidbCustomActionTypeTextData:
					canBeEmptyCustomActionTypeValue = true;
					customActionType = "Value";
					break;
				case MsiInterop.MsidbCustomActionTypeJScript:
					canBeEmptyCustomActionTypeValue = true;
					customActionType = "JScriptCall";
					break;
				case MsiInterop.MsidbCustomActionTypeVBScript:
					canBeEmptyCustomActionTypeValue = true;
					customActionType = "VBScriptCall";
					break;
				case MsiInterop.MsidbCustomActionTypeInstall:
					customActionType = "InstallProperties";
					if ("Directory" == name)
					{
						name = "PackageProductCode";
					}

					if ("FileKey" == name)
					{
						name = "PackagePath";
					}

					if ("BinaryKey" == name)
					{
						name = "PackageSubstorage";
					}
					break;
				case 0:
				case 4:
				default:
					// TODO: throw an exception:  Fail "Unsupported custom action type: "
					break;
			}

			if ("FileKey" == name && "Value" == customActionType)
			{
				this.core.WriteAttributeString(writer, "Error", target);
			}
			else
			{
				if (0 < source.Length)
				{
					source = this.StripModuleId(source);
					if ("Directory" == name)
					{
						source = this.core.GetValidIdentifier(source, "CustomAction; Column: Source"); // nullable
					}
					this.core.WriteAttributeString(writer, name, source);

					// target is a formatted field so it is possible the value is a
					// foriegn key with a module id in it.
					target = this.StripModuleId(target);

					this.core.WriteAttributeString(writer, null, customActionType, target, canBeEmptyCustomActionTypeValue);
				}
				else
				{
					if ("JScriptCall" == customActionType)
					{
						this.core.WriteAttributeString(writer, "Script", "jscript");
					}
					else if ("VBScriptCall" == customActionType)
					{
						this.core.WriteAttributeString(writer, "Script", "vbscript");
					}
					else
					{
						// TODO: throw an exception
					}
					writer.WriteCData(target);
				}
			}
			writer.WriteEndElement();
			if (this.generateFragments)
			{
				this.EmitSequence("AdminUISequence", writer, id);

				// write InstallUISequence
				this.EmitSequence("InstallUISequence", writer, id);
				this.EmitSequence("AdvertiseExecuteSequence", writer, id);
				this.EmitSequence("AdminExecuteSequence", writer, id);
				this.EmitSequence("InstallExecuteSequence", writer, id);
				if (null == parentWriter)
				{
					writer.WriteEndElement();
				}
			}
		}

		/// <summary>
		/// Process the CustomAction table.
		/// </summary>
		/// <param name="parentWriter">writer context from the caller</param>
		/// <param name="key">specific key that's the caller wants</param>
		private void ProcessCustomActionTable(XmlWriter parentWriter, string key)
		{
			const string tableName = "CustomAction";
			if (!this.inputDatabase.TableExists(tableName))
			{
				return;
			}

			string query = String.Concat("SELECT * FROM `", tableName, "`");
			if (null != key && 0 < key.Length)
			{
				if (this.customActionsProcessed.ContainsKey(key))
				{
					return;  // not an error because multiple controls can reference a binary/icon
				}
				query = String.Concat(query, " WHERE `Action`='", key, "'");
			}

			using (View view = this.inputDatabase.OpenExecuteView(query))
			{
				Record record;
				while (view.Fetch(out record))
				{
					if (this.skipVSI && ("dirca_checkfx" == this.StripModuleId(record[(int)MsiInterop.CustomAction.Action]).ToLower() || "vsdca_vsdlaunchconditions" == this.StripModuleId(record[(int)MsiInterop.CustomAction.Action]).ToLower()))
					{
						this.core.OnMessage(WixWarnings.FilteringVSIStuff(null, WarningLevel.Major, "CustomAction", this.StripModuleId(record[(int)MsiInterop.CustomAction.Action]).ToLower()));
						continue;
					}

					if (0 == record[(int)MsiInterop.CustomAction.Target].Length &&
						(Convert.ToInt32(record[(int)MsiInterop.CustomAction.Type]) & MsiInterop.MsidbCustomActionTypeExecuteBits) == MsiInterop.MsidbCustomActionTypeDll)
					{
						this.core.OnMessage(WixWarnings.InvalidNullTargetCustomAction(null, WarningLevel.Major, this.StripModuleId(record[(int)MsiInterop.CustomAction.Action]), "DllEntry"));
						continue;
					}

					string actionName = this.StripModuleId(record[(int)MsiInterop.CustomAction.Action]);
					if (this.customActionsProcessed.ContainsKey(actionName))
					{
						continue;  // not an error because multiple controls can reference a binary/icon
					}
					else
					{
						this.customActionsProcessed[actionName] = true;
					}

					this.ProcessCustomActionRecord(parentWriter, record);
				}
			}
		}

		/// <summary>
		/// Generate sequence tuple.
		/// </summary>
		/// <param name="element">Element name from the WiX schema that we're writing</param>
		/// <param name="action">Action we're working with</param>
		/// <param name="actionLabel">Attribute name for the action as its different depending on the enclosing element.</param>
		/// <param name="condition">Condition on the sequence</param>
		/// <param name="sequenceString">sequence integer represented as a string</param>
		/// <param name="afterString">if this sequence item is after, then this string will have length greater than zero</param>
		/// <param name="beforeString">if this sequence item is before, then this string will have length greater than zero</param>
		/// <param name="exit">if this sequence supposed to set the OnExit attrbute, this will have length greater than zero</param>
		/// <param name="msmTable">if the name of the MSI table does not match the name of the element, then this will be true</param>
		/// <returns>array with aggregation of arguments as well as nulling out the arguments</returns>
		private string[] GenerateSequenceTuple(ref string element, ref string action, ref string actionLabel, ref string condition, ref string sequenceString, ref string afterString, ref string beforeString, ref string exit, bool msmTable)
		{
			if (this.explicitSequenceTables)
			{
				if (!msmTable)
				{
					afterString = "";
					beforeString = "";
				}
			}
			else
			{
				sequenceString = "";
			}

			string[] tuple = { element, action, actionLabel, condition, sequenceString, afterString, beforeString, exit };
			element = "";
			action = "";
			actionLabel = "";
			condition = "";
			sequenceString = "";
			afterString = "";
			beforeString = "";
			exit = "";
			return tuple;
		}

		/// <summary>
		/// Emit sequence tuple.
		/// </summary>
		/// <param name="tableName">Name of table to process.</param>
		/// <param name="parentWriter">parent writer context</param>
		/// <param name="keyRequest">Specific key of interest (optional)</param>
		private void EmitSequence(string tableName, XmlWriter parentWriter, string keyRequest)
		{
			Hashtable unprocessed;
			XmlWriter writer = parentWriter;
			bool tableStarted = false;
			switch (tableName)
			{
				case "AdminUISequence":
					unprocessed = this.unprocessedAdminUISequence;
					break;
				case "AdminExecuteSequence":
					unprocessed = this.unprocessedAdminExecuteSequence;
					break;
				case "AdvertiseExecuteSequence":
					unprocessed = this.unprocessedAdvtExecuteSequence;
					break;
				case "InstallExecuteSequence":
					unprocessed = this.unprocessedInstallExecuteSequence;
					break;
				case "InstallUISequence":
					unprocessed = this.unprocessedInstallUISequence;
					break;
				default:
					throw new WixInvalidSequenceTypeException(null, tableName);
			}
			ArrayList keys = new ArrayList();
			if (null != keyRequest && 0 < keyRequest.Length)
			{
				keys.Add(keyRequest);
			}
			else
			{
				keys.AddRange(unprocessed.Keys);
			}

			foreach (string key in keys)
			{
				if (unprocessed.Contains(key))
				{
					string[] tuple = (string[])unprocessed[key];
					if (null != tuple[0] && 0 < tuple[0].Length)
					{
						if (null == writer)
						{
							writer = this.core.Writer;
						}

						if (!tableStarted)
						{
							writer.WriteStartElement(tableName);
							tableStarted = true;
						}

						// start element.  could be one of three types
						writer.WriteStartElement(tuple[0]);

						// write "action".  Could be absent or one of two types
						if (null != tuple[2] && 0 < tuple[2].Length)
						{
							this.core.WriteAttributeString(writer, tuple[2], this.StripModuleId(tuple[1]));
						}

						// write sequence if present
						if (null != tuple[4] && 0 < tuple[4].Length)
						{
							this.core.WriteAttributeString(writer, "Sequence", tuple[4]);
						}

						// write After if present
						if (null != tuple[5] && 0 < tuple[5].Length)
						{
							this.core.WriteAttributeString(writer, "After", tuple[5]);
						}

						// write Before if present
						if (null != tuple[6] && 0 < tuple[6].Length)
						{
							this.core.WriteAttributeString(writer, "Before", tuple[6]);
						}

						// write OnExit if present
						if (null != tuple[7] && 0 < tuple[7].Length)
						{
							this.core.WriteAttributeString(writer, "OnExit", tuple[7]);
						}

						// write condition data if present
						if (null != tuple[3] && 0 < tuple[3].Length)
						{
							writer.WriteCData(tuple[3]);
						}
						writer.WriteEndElement();
						unprocessed.Remove(key);
					}
				}
			}

			if (tableStarted)
			{
				writer.WriteEndElement();
			}
		}

		/// <summary>
		/// Processes a sequence table.
		/// </summary>
		private void ProcessSequenceTables()
		{
			string[] sequences = new string[] { "AdminUISequence", "AdminExecuteSequence", "AdvertiseExecuteSequence", "InstallExecuteSequence", "InstallUISequence" };
			foreach (string sequenceName in sequences)
			{
				ArrayList processTables = new ArrayList();
				SequenceType sequenceType;
				Hashtable unprocessed;
				sequenceType = SequenceType.installUI;
				unprocessed = this.unprocessedInstallUISequence;
				string tableName = sequenceName;
				string elementName = tableName;
				switch (tableName)
				{
					case "AdminUISequence":
						sequenceType = SequenceType.adminUI;
						unprocessed = this.unprocessedAdminUISequence;
						break;
					case "AdminExecuteSequence":
						sequenceType = SequenceType.adminExecute;
						unprocessed = this.unprocessedAdminExecuteSequence;
						break;
					case "AdvertiseExecuteSequence":
						tableName = "AdvtExecuteSequence";
						sequenceType = SequenceType.advertiseExecute;
						unprocessed = this.unprocessedAdvtExecuteSequence;
						break;
					case "InstallExecuteSequence":
						sequenceType = SequenceType.installExecute;
						unprocessed = this.unprocessedInstallExecuteSequence;
						break;
					case "InstallUISequence":
						sequenceType = SequenceType.installUI;
						unprocessed = this.unprocessedInstallUISequence;
						break;
					default:
						throw new WixInvalidSequenceTypeException(null, tableName);
				}

				if (this.processingModule)
				{
					string moduleTableName = String.Concat("Module", tableName);
					if (this.inputDatabase.TableExists(moduleTableName))
					{
						processTables.Add(moduleTableName);
					}
				}

				if (this.inputDatabase.TableExists(tableName))
				{
					processTables.Add(tableName);
				}

				if (0 == processTables.Count)
				{
					continue;
				}

				foreach (string sequenceTableName in processTables)
				{
					using (View view = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `", sequenceTableName, "` ORDER BY `Sequence`")))
					{
						string lastAction = null;
						string needActionBefore = null;
						string lastCondition = null;
						string actionId = null;
						string actionLabel = null;
						string element = "";
						string condition = "";
						int sequence = 0;
						string sequenceString = "";
						string baseAction = "";
						int after = -1;
						string afterString = "";
						string beforeString = "";
						string terminationFlag = "";
						bool foundLaunchConditions = false;

						Record record;
						while (view.Fetch(out record))
						{
							string action;
							if (tableName == sequenceTableName)
							{
								action = this.StripModuleId(record[(int)MsiInterop.InstallUISequence.Action]);
							}
							else
							{
								action = this.StripModuleId(record[(int)MsiInterop.ModuleInstallUISequence.Action]);
							}

							if (this.skipVSI && ("dirca_checkfx" == action.ToLower() || "vsdca_vsdlaunchconditions" == action.ToLower()))
							{
								this.core.OnMessage(WixWarnings.FilteringVSIStuff(null, WarningLevel.Major, "CustomAction", action.ToLower()));
								continue;
							}
							this.core.OnMessage(WixVerboses.ProcessingSequence(null, VerboseLevel.Verbose, sequenceTableName));

							bool customAction = false;
							bool dialogAction = false;
							bool standardAction = false;
							bool specialAction = false;

							this.core.OnMessage(WixVerboses.ProcessingAction(null, VerboseLevel.Verbose, action));

							if (null != needActionBefore)
							{
								beforeString = action;
								string key = actionId;
								if (null == key || 0 == key.Length)
								{
									key = element;
								}
								unprocessed[key] = this.GenerateSequenceTuple(ref element, ref actionId, ref actionLabel, ref lastCondition, ref sequenceString, ref afterString, ref beforeString, ref terminationFlag, tableName != sequenceTableName);

								needActionBefore = null; // reset the before action chain
							}

							if (tableName == sequenceTableName)
							{
								condition = record[(int)MsiInterop.InstallUISequence.Condition];
								sequenceString = record[(int)MsiInterop.InstallUISequence.Sequence]; // non-nullable field
								sequence = 0;
								if (0 < sequenceString.Length)
								{
									sequence = Convert.ToInt32(sequenceString);
								}
							}
							else
							{
								condition = record[(int)MsiInterop.ModuleInstallUISequence.Condition]; // nullable field
								sequenceString = record[(int)MsiInterop.ModuleInstallUISequence.Sequence]; // non-nullable field
								baseAction = this.StripModuleId(record[(int)MsiInterop.ModuleInstallUISequence.BaseAction]); // nullable field
								afterString = record[(int)MsiInterop.ModuleInstallUISequence.After]; // nullable field
								sequence = 0;
								if (0 < sequenceString.Length)
								{
									sequence = Convert.ToInt32(sequenceString);
								}
								after = -1;
								if (0 < afterString.Length)
								{
									after = Convert.ToInt32(afterString);
								}
							}

							// existing condition maybe just whitespace
							if (0 < condition.Length)
							{
								condition.Trim();
							}

							if (null != this.standardActions[sequenceType, action]) // custom action
							{
								if (this.explicitSequenceTables)
								{
									actionId = "";
									element = this.StripModuleId(action);
									string key = element;
									unprocessed[key] = this.GenerateSequenceTuple(ref element, ref actionId, ref actionLabel, ref condition, ref sequenceString, ref afterString, ref beforeString, ref terminationFlag, tableName != sequenceTableName);
								}
								else
								{
									if (action == "LaunchConditions")
									{
										foundLaunchConditions = true;
									}

									if (action == "AppSearch" && foundLaunchConditions)
									{
										actionId = "";
										element = this.StripModuleId(action);
										string key = element;
										if (0 == afterString.Length)
										{
											afterString = "LaunchConditions";
										}
										unprocessed[key] = this.GenerateSequenceTuple(ref element, ref actionId, ref actionLabel, ref condition, ref sequenceString, ref afterString, ref beforeString, ref terminationFlag, tableName != sequenceTableName);
									}
								}
								standardAction = true;
							}

							if (this.inputDatabase.TableExists("CustomAction"))
							{
								using (View customActionView = this.inputDatabase.OpenView("SELECT `Type` FROM `CustomAction` WHERE `Action`=?"))
								{
									customActionView.Execute(record); // need to put a try catch here
									Record customActionRecord;
									if (customActionView.Fetch(out customActionRecord))
									{
										// advertise execute sequence cannot have custom actions so skip the processing of this row, and fetch the next
										if ("AdvtExecuteSequence" == tableName)
										{
											int actionType = customActionRecord.GetInteger(1);

											// The only custom actions that are allowed in the AdvtExecuteSequence are type 19 (0x013) type 35 (0x023) and type 51 (0x033)
											if (0x013 != (actionType & 0x013) && 0x023 != (actionType & 0x023) && 0x033 != (actionType & 0x033))
											{
												this.core.OnMessage(WixWarnings.IllegalAdvtExecuteSequenceCustomAction(null, WarningLevel.Major, action));
												continue;
											}
										}

										element = "Custom";
										actionId = this.StripModuleId(this.core.GetValidIdentifier(action, String.Concat(sequenceTableName, "; Column: Action")));
										actionLabel = "Action";

										customAction = true;
									}
								}
							}

							if (this.inputDatabase.TableExists("Dialog"))
							{
								using (View dialogView = this.inputDatabase.OpenView("SELECT * FROM `Dialog` WHERE `Dialog`=?"))
								{
									if ((null != action) && (null != dialogView))
									{
										dialogView.Execute(record); // need to put a try catch here
										Record dialogRecord;
										if (dialogView.Fetch(out dialogRecord))
										{
											element = "Show";
											actionId = this.StripModuleId(action);
											actionLabel = "Dialog";

											dialogAction = true;
										}
									}
								}
							}

							if (!customAction && !dialogAction && !standardAction)
							{
								// for special actions we just write out the sequence
								if ("InstallExecute" == action || "InstallExecuteAgain" == action || "RemoveExistingProducts" == action || "DisableRollback" == action || "ScheduleReboot" == action || "ForceReboot" == action || "ResolveSource" == action)
								{
									element = action;

									specialAction = true;
								}
								else
								{
									this.core.OnMessage(WixWarnings.UnknownAction(null, WarningLevel.Major, action, sequenceTableName));
									continue;
								}
							}

							if (customAction || dialogAction || specialAction)
							{
								if (0 > sequence)
								{
									string exit = null;
									switch (sequence)
									{
										case -1:
											exit = "success";
											break;
										case -2:
											exit = "cancel";
											break;
										case -3:
											exit = "error";
											break;
										case -4:
											exit = "suspend";
											break;
										default:
											// TODO: throw an exception - Fail "Invalid OnExit sequence number: " & sequence
											break;
									}
									terminationFlag = exit;

									action = null; // don't allow "OnExit" dialogs to be the last action
								}
								else
								{
									if (tableName == sequenceTableName)
									{
										if (null == lastAction)
										{
											needActionBefore = action;
											lastCondition = condition;
										}
										else
										{
											afterString = lastAction;
										}
									}
									else
									{
										if (1 == after)
										{
											afterString = baseAction;
										}
										else if (0 == after)
										{
											beforeString = baseAction;
										}
									}
								}

								if (tableName == sequenceTableName)
								{
									if (null == needActionBefore)
									{
										string key = actionId;
										if (null == key || 0 == key.Length)
										{
											key = element;
										}
										unprocessed[key] = this.GenerateSequenceTuple(ref element, ref actionId, ref actionLabel, ref condition, ref sequenceString, ref afterString, ref beforeString, ref terminationFlag, tableName != sequenceTableName);
									}
								}
								else
								{
									condition = this.StripModuleId(condition);
									string key = actionId;
									if (null == key || 0 == key.Length)
									{
										key = element;
									}
									unprocessed[key] = this.GenerateSequenceTuple(ref element, ref actionId, ref actionLabel, ref condition, ref sequenceString, ref afterString, ref beforeString, ref terminationFlag, tableName != sequenceTableName);
								}
							}

							if (tableName == sequenceTableName && null == needActionBefore)
							{
								lastAction = action;
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// Processes the Binary and Icon tables.
		/// </summary>
		/// <param name="writer">XmlWriter where the Intermediate should persist itself as XML.</param>
		/// <param name="binaryType">type of binary to process.</param>
		/// <param name="binaryName">Name of binary to process.</param>
		private void ProcessBinaryTable(XmlWriter writer, BinaryType binaryType, string binaryName)
		{
			if (!this.inputDatabase.TableExists(binaryType.ToString()))
			{
				return;
			}
			string query = String.Concat("SELECT * FROM `", binaryType.ToString(), "`");
			if (0 < binaryName.Length)
			{
				if (this.binariesProcessed.ContainsKey(binaryName))
				{
					return;  // not an error because multiple controls can reference a binary/icon
				}
				query = String.Concat(query, " WHERE `Name`='", binaryName, "'");
			}

			using (View view = this.inputDatabase.OpenExecuteView(query))
			{
				if (this.exportBinaries)
				{
					if ((BinaryType.Binary == binaryType && !this.binariesExported) ||
						(BinaryType.Icon == binaryType && !this.iconsExported))
					{
						string binaryFileName = String.Concat(binaryType.ToString(), ".idt");
						this.inputDatabase.Export(binaryType.ToString(), this.exportBasePath, binaryFileName);
						File.Delete(Path.Combine(this.exportBasePath, binaryFileName));
						if (BinaryType.Binary == binaryType)
						{
							this.binariesExported = true;
						}
						else if (BinaryType.Icon == binaryType)
						{
							this.iconsExported = true;
						}
					}
				}
				Record record;
				while (view.Fetch(out record))
				{
					string name = record[(int)MsiInterop.Binary.Name];
					if (this.binariesProcessed.ContainsKey(name))
					{
						continue;  // not an error because multiple controls can reference a property
					}
					else
					{
						this.binariesProcessed[name] = true;
					}
					string id = this.StripModuleId(name);
					if (this.skipVSI && ("dirca_checkfx" == id.ToLower() || "vsdca_vsdlaunchconditions" == id.ToLower()))
					{
						this.core.OnMessage(WixWarnings.FilteringVSIStuff(null, WarningLevel.Major, "Binary", id.ToLower()));
						continue;
					}

					if (null == writer)
					{
						writer = this.InitializeXmlTextWriter(Path.GetFullPath(Path.Combine(Path.Combine(this.outputFolder, "BaseLibrary"), String.Concat(binaryType.ToString(), ".wxs"))));
						if (this.generateFragments)
						{
							// initalize the fragment
							writer.WriteStartElement("Fragment");
						}
					}
					writer.WriteStartElement(binaryType.ToString());
					this.core.WriteAttributeString(writer, "Id", id);
					this.core.WriteAttributeString(writer, "SourceFile", Path.Combine(binaryType.ToString(), String.Concat(id, ".ibd")));
					writer.WriteEndElement();
				}

				if (this.generateFragments && null != writer)
				{
					writer.WriteEndElement();
				}
			}
		}

		/// <summary>
		/// Processes the BBControl table.
		/// </summary>
		/// <param name="writer">XmlWriter for this file</param>
		/// <param name="billboardId">billboard that needs processing</param>
		private void ProcessBBControlTable(XmlWriter writer, string billboardId)
		{
			const string tableName = "BBControl";
			if (!this.inputDatabase.TableExists(tableName))
			{
				return;
			}

			using (View view = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `", tableName, "` WHERE `Billboard_`='", billboardId, "'")))
			{
				Record record;
				while (view.Fetch(out record))
				{
					// NOTE: this code is mostly duplicated (copy and permute) from this.ProcessControl.  No obvious easy way to call that method from here.
					writer.WriteStartElement("Control");
					string controlType = record[(int)MsiInterop.BBControl.BBControl]; // non-nullable

					// From SDK "Only static controls, such as a Text, Bitmap, Icon, or custom control can be placed on a billboard. For a complete list of controls, see the Controls section."
					/*   TODO: figure out the non-static controls
						 TODO: write a warning for this case
					 */
					this.core.WriteAttributeString(writer, "Id", record[(int)MsiInterop.BBControl.BBControl]); // non-nullable
					this.core.WriteAttributeString(writer, "Type", controlType);
					this.core.WriteAttributeString(writer, "X", record[(int)MsiInterop.BBControl.X]);
					this.core.WriteAttributeString(writer, "Y", record[(int)MsiInterop.BBControl.Y]);
					this.core.WriteAttributeString(writer, "Width", record[(int)MsiInterop.BBControl.Width]);
					this.core.WriteAttributeString(writer, "Height", record[(int)MsiInterop.BBControl.Height]);

					// TODO: test Attributes for blowing up when null, error violation of SDK constraint "This must be a non-negative number"
					this.ProcessControlAttributes(writer, controlType, Convert.ToInt32(record[(int)MsiInterop.BBControl.Attributes]), true);
					if (0 < record[(int)MsiInterop.BBControl.Text].Length)
					{
						string text = record[(int)MsiInterop.BBControl.Text];
						if (20 >= text.Length && !this.NeedsEscape(text))
						{
							this.core.WriteAttributeString(writer, "Text", text);
						}
						else
						{
							writer.WriteStartElement("Text");
							writer.WriteCData(text);
							writer.WriteEndElement();
						}
					}
					writer.WriteEndElement();
				}
			}
		}

		/// <summary>
		/// Processes the Billboard table.
		/// </summary>
		/// <param name="parentWriter">XmlWriter for parent</param>
		private void ProcessBillboardTable(XmlWriter parentWriter)
		{
			const string tableName = "Billboard";
			XmlWriter writer = null;
			string lastAction = null;
			if (!this.inputDatabase.TableExists(tableName))
			{
				return;
			}

			using (View view = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `", tableName, "` ORDER BY `Action`, `Ordering`")))
			{
				Record record;
				while (view.Fetch(out record))
				{
					if (null == writer)
					{
						writer = this.InitializeXmlTextWriter(Path.GetFullPath(Path.Combine(this.outputFolder, "SkuLibrary\\UI\\BillboardAction.wxs")));
						if (this.generateFragments)
						{
							// initalize the fragment
							writer.WriteStartElement("Fragment");
							this.core.WriteAttributeString(writer, "Id", "BillboardAction");

							// create reference to fragment
							parentWriter.WriteStartElement("FragmentRef");
							this.core.WriteAttributeString(parentWriter, "Id", "BillboardAction");
							parentWriter.WriteEndElement();
						}
					}

					string billboardId = record[(int)MsiInterop.Billboard.Billboard];
					string actionId = record[(int)MsiInterop.Billboard.Action];
					string feature = record[(int)MsiInterop.Billboard.Feature];

					if (lastAction != actionId)
					{
						if (null != lastAction)
						{
							writer.WriteEndElement();
						}
						lastAction = actionId;
						writer.WriteStartElement("BillboardAction");
						this.core.WriteAttributeString(writer, "Id", actionId); // non-null-able
					}
					this.ProcessBillboard(writer, billboardId, feature);
					if (this.generateFragments && null != writer)
					{
						// terminate the fragment
						writer.WriteEndElement();
					}
				}

				if (null != lastAction)
				{
					writer.WriteEndElement();
				}
			}
		}

		/// <summary>
		/// Processes the Billboard
		/// </summary>
		/// <param name="parentWriter">XmlWriter where the Intermediate should persist itself as XML.</param>
		/// <param name="billboardId">billboard identifier</param>
		/// <param name="feature">feature context</param>
		private void ProcessBillboard(XmlWriter parentWriter, string billboardId, string feature)
		{
			XmlWriter writer = this.InitializeXmlTextWriter(Path.GetFullPath(Path.Combine(this.outputFolder, string.Concat("SkuLibrary\\UI\\Billboard\\", billboardId, ".wxs"))));
			if (this.generateFragments)
			{
				// initalize the fragment
				writer.WriteStartElement("Fragment");
				this.core.WriteAttributeString(writer, "Id", billboardId);

				// create reference to fragment
				parentWriter.WriteStartElement("FragmentRef");
				this.core.WriteAttributeString(parentWriter, "Id", billboardId);
				parentWriter.WriteEndElement();
			}

			writer.WriteStartElement("Billboard");
			this.core.WriteAttributeString(writer, "Id", billboardId); // non-null-able
			this.core.WriteAttributeString(writer, "Feature", feature); // null-able
			this.ProcessBBControlTable(writer, billboardId);
			writer.WriteEndElement();
			if (this.generateFragments)
			{
				// terminate the fragment
				writer.WriteEndElement();
			}
		}

		/// <summary>
		/// Processes the UIText table.
		/// </summary>
		/// <param name="parentWriter">XmlWriter where the Intermediate should persist itself as XML.</param>
		private void ProcessUITextTable(XmlWriter parentWriter)
		{
			const string tableName = "UIText";
			XmlWriter writer = null;
			if (!this.inputDatabase.TableExists(tableName))
			{
				return;
			}

			using (View view = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `", tableName, "`")))
			{
				Record record;
				while (view.Fetch(out record))
				{
					if (null == writer)
					{
						writer = this.InitializeXmlTextWriter(Path.GetFullPath(Path.Combine(this.outputFolder, "SkuLibrary\\UI\\UIText.wxs")));
						if (this.generateFragments)
						{
							// initalize the fragment
							writer.WriteStartElement("Fragment");
							this.core.WriteAttributeString(writer, "Id", "UIText");

							// initalize the UI element
							writer.WriteStartElement("UI");

							// create reference to fragment
							parentWriter.WriteStartElement("FragmentRef");
							this.core.WriteAttributeString(parentWriter, "Id", "UIText");
							parentWriter.WriteEndElement();
						}
					}
					writer.WriteStartElement(tableName);
					this.core.WriteAttributeString(writer, "Id", record[(int)MsiInterop.UIText.Key]);
					writer.WriteCData(record[(int)MsiInterop.UIText.Text]);
					writer.WriteEndElement();
				}

				if (this.generateFragments && null != writer)
				{
					// terminate the UI element
					writer.WriteEndElement();

					// terminate the fragment
					writer.WriteEndElement();
				}
			}
		}

		/// <summary>
		/// Processes the TextStyle table.
		/// </summary>
		/// <param name="parentWriter">XmlWriter where the Intermediate should persist itself as XML.</param>
		private void ProcessTextStyleTable(XmlWriter parentWriter)
		{
			const string tableName = "TextStyle";
			XmlWriter writer = null;
			if (!this.inputDatabase.TableExists(tableName))
			{
				return;
			}

			using (View view = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `", tableName, "`")))
			{
				Record record;
				while (view.Fetch(out record))
				{
					if (null == writer)
					{
						writer = this.InitializeXmlTextWriter(Path.GetFullPath(Path.Combine(this.outputFolder, "SkuLibrary\\UI\\TextStyles.wxs")));
						if (this.generateFragments)
						{
							// initalize the fragment
							writer.WriteStartElement("Fragment");
							this.core.WriteAttributeString(writer, "Id", "TextStyles");

							// initalize the UI element
							writer.WriteStartElement("UI");

							// create reference to fragment
							parentWriter.WriteStartElement("FragmentRef");
							this.core.WriteAttributeString(parentWriter, "Id", "TextStyles");
							parentWriter.WriteEndElement();
						}
					}
					writer.WriteStartElement(tableName);
					this.core.WriteAttributeString(writer, "Id", record[(int)MsiInterop.TextStyle.TextStyle]);
					this.core.WriteAttributeString(writer, "FaceName", record[(int)MsiInterop.TextStyle.FaceName]);
					this.core.WriteAttributeString(writer, "Size", record[(int)MsiInterop.TextStyle.Size]);
					if (0 < record[(int)MsiInterop.TextStyle.Color].Length) // nullable field
					{
						int color = Convert.ToInt32(record[(int)MsiInterop.TextStyle.Color]);
						this.core.WriteAttributeString(writer, "Red", (color & 255).ToString());
						this.core.WriteAttributeString(writer, "Green", (color / 256 & 255).ToString());
						this.core.WriteAttributeString(writer, "Blue", (color / 65536 & 255).ToString());
					}

					string styleString = record[(int)MsiInterop.TextStyle.StyleBits]; // nullable field
					if (0 < styleString.Length)
					{
						int style = Convert.ToInt32(styleString);
						if (0 < (style & 1))
						{
							this.core.WriteAttributeString(writer, "Bold", "yes");
						}

						if (0 < (style & 2))
						{
							this.core.WriteAttributeString(writer, "Italic", "yes");
						}

						if (0 < (style & 4))
						{
							this.core.WriteAttributeString(writer, "Underline", "yes");
						}

						if (0 < (style & 8))
						{
							this.core.WriteAttributeString(writer, "Strike", "yes");
						}
					}
					writer.WriteEndElement();
				}

				if (this.generateFragments && null != writer)
				{
					// terminate the UI element
					writer.WriteEndElement();

					// terminate the fragment
					writer.WriteEndElement();
				}
			}
		}

		/// <summary>
		/// Processes the Dialog table.
		/// </summary>
		/// <param name="parentWriter">XmlWriter where the Intermediate should persist itself as XML.</param>
		private void ProcessDialogTable(XmlWriter parentWriter)
		{
			const string tableName = "Dialog";
			if (!this.inputDatabase.TableExists(tableName))
			{
				return;
			}

			using (View view = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `", tableName, "`")))
			{
				Record record;
				while (view.Fetch(out record))
				{
					XmlTextWriter writer = this.InitializeXmlTextWriter(Path.GetFullPath(Path.Combine(this.outputFolder, String.Concat("SkuLibrary\\UI\\Dialogs\\", record[(int)MsiInterop.Dialog.Dialog], ".wxs"))));
					if (this.generateFragments)
					{
						// initalize the fragment
						writer.WriteStartElement("Fragment");
						this.core.WriteAttributeString(writer, "Id", record[(int)MsiInterop.Dialog.Dialog]);

						// initalize the UI element
						writer.WriteStartElement("UI");

						// create reference to fragment
						parentWriter.WriteStartElement("FragmentRef");
						this.core.WriteAttributeString(parentWriter, "Id", record[(int)MsiInterop.Dialog.Dialog]);
						parentWriter.WriteEndElement();
					}

					// TODO: test this for blowing up when null, error violation of SDK constraint "This must be a non-negative number"
					int bits = Convert.ToInt32(record[(int)MsiInterop.Dialog.Attributes]) ^ MsiInterop.DialogAttributesInvert; // nullable field
					string firstControl = record[(int)MsiInterop.Dialog.ControlFirst];
					string defaultControl = record[(int)MsiInterop.Dialog.ControlDefault];
					string cancelControl  = record[(int)MsiInterop.Dialog.ControlCancel];
					string str;

					writer.WriteStartElement(tableName);
					this.core.WriteAttributeString(writer, "Id", record[(int)MsiInterop.Dialog.Dialog]);

					if (50 != Convert.ToInt32(record[(int)MsiInterop.Dialog.HCentering])) // non-nullable field
					{
						this.core.WriteAttributeString(writer, "X", record[(int)MsiInterop.Dialog.HCentering]);
					}

					if (50 != Convert.ToInt32(record[(int)MsiInterop.Dialog.VCentering])) // non-nullable field
					{
						this.core.WriteAttributeString(writer, "Y", record[(int)MsiInterop.Dialog.VCentering]);
					}

					this.core.WriteAttributeString(writer, "Width", record[(int)MsiInterop.Dialog.Width]);
					this.core.WriteAttributeString(writer, "Height", record[(int)MsiInterop.Dialog.Height]);
					str = record[(int)MsiInterop.Dialog.Title];
					this.core.WriteAttributeString(writer, "Title", str);

					if (0 < (bits & MsiInterop.MsidbDialogAttributesModal))
					{
						bits = bits & ~MsiInterop.MsidbDialogAttributesMinimize;
					}

					if (0 < (bits & MsiInterop.MsidbDialogAttributesError))
					{
						this.core.WriteAttributeString(writer, "ErrorDialog", "yes"); // can we discover this ourselves?
					}

					for (int index = 0; index <= MsiInterop.DialogAttributes.Length; index++)
					{
						if (0 < (bits & 1))
						{
							string name = MsiInterop.DialogAttributes[index];
							if (null == name || 0 == name.Length)
							{
								// TODO: throw an exception - Fail "Unknown attribute at bit position " & index
							}
							this.core.WriteAttributeString(writer, name, "yes");
						}
						bits = bits / 2;
					}

					Record sqlParams = new Record(2);
					if (this.inputDatabase.TableExists("Control"))
					{
						using (View dialogView = this.inputDatabase.OpenView("SELECT * FROM `Control` WHERE `Dialog_` = ?"))
						{
							ArrayList processedControls = new ArrayList();
							using (View dialogControlView = this.inputDatabase.OpenView("SELECT * FROM `Control` WHERE `Dialog_` = ? AND `Control` = ?"))
							{
								string nextControl = "_WiX_First_Control_Never_Matches_Anything_";
								sqlParams.SetString(1, record[(int)MsiInterop.Dialog.Dialog]);

								while (0 < nextControl.Length && nextControl != firstControl)
								{
									if ("_WiX_First_Control_Never_Matches_Anything_" == nextControl)
									{
										nextControl = firstControl;
									}

									sqlParams.SetString(2, nextControl);
									dialogControlView.Execute(sqlParams);
									Record dialogControlRecord;
									dialogControlView.Fetch(out dialogControlRecord);
									if (null == dialogControlRecord)
									{
										// TODO: throw an exception - Fail "Control " & nextControl & " not found"
										break;
									}

									this.ProcessControl(writer, parentWriter, dialogControlRecord, defaultControl, cancelControl, false);
									processedControls.Add(nextControl);
									nextControl = dialogControlRecord[(int)MsiInterop.Control.ControlNext];
								}
							}

							dialogView.Execute(sqlParams);
							Record dialogRecord;
							while (dialogView.Fetch(out dialogRecord))
							{
								string nextControl = dialogRecord[(int)MsiInterop.Control.Control];
								if (null != nextControl && nextControl != firstControl && !processedControls.Contains(nextControl))
								{
									this.ProcessControl(writer, parentWriter, dialogRecord, defaultControl, cancelControl, true);
								}
							}
						}
					}

					if (this.generateFragments)
					{
						// terminate the UI element
						writer.WriteEndElement();

						// terminate the fragment
						writer.WriteEndElement();
					}
					writer.WriteEndElement();
				}
			}
		}

		/// <summary>
		/// Process control attributes
		/// </summary>
		/// <param name="writer">XmlWriter where the Intermediate should persist itself as XML.</param>
		/// <param name="controlType">type of control</param>
		/// <param name="rawBits">bits representing attributes</param>
		/// <param name="tabDisabled">true if tab disabled</param>
		private void ProcessControlAttributes(XmlWriter writer, string controlType, int rawBits, bool tabDisabled)
		{
			const string tableName = "Control";
			if (!this.inputDatabase.TableExists(tableName))
			{
				return;
			}

			bool tabSkip = false;
			bool disabled = false;
			string[] specialAttributes = null;
			int ignoreBits = 0;
			int commonCount = MsiInterop.CommonControlAttributes.Length;
			switch (controlType)
			{
				case "Text":
					specialAttributes = MsiInterop.TextControlAttributes;
					tabSkip = true;
					break;
				case "Edit":
					specialAttributes = MsiInterop.EditControlAttributes;
					ignoreBits = MsiInterop.MsidbControlAttributesNoPrefix;
					break;
				case "MaskedEdit":
					specialAttributes = MsiInterop.EditControlAttributes;
					break;
				case "PathEdit":
					specialAttributes = MsiInterop.EditControlAttributes;
					break;
				case "Icon":
					specialAttributes = MsiInterop.IconControlAttributes;
					tabSkip = true;
					disabled = true;
					ignoreBits = MsiInterop.MsidbControlAttributesIcon;
					break;
				case "Bitmap":
					specialAttributes = MsiInterop.BitmapControlAttributes;
					tabSkip = true;
					disabled = true;
					ignoreBits = MsiInterop.MsidbControlAttributesBitmap;
					break;
				case "ProgressBar":
					specialAttributes = MsiInterop.ProgressControlAttributes;
					tabSkip = true;
					disabled = true;
					break;
				case "DirectoryCombo":
					specialAttributes = MsiInterop.VolumeControlAttributes;
					break;
				case "VolumeSelectCombo":
					specialAttributes = MsiInterop.VolumeControlAttributes;
					break;
				case "VolumeCostList":
					specialAttributes = MsiInterop.VolumeControlAttributes;
					tabSkip = true;
					break;
				case "ListBox":
					specialAttributes = MsiInterop.ListboxControlAttributes;
					break;
				case "ListView":
					specialAttributes = MsiInterop.ListviewControlAttributes;
					break;
				case "ComboBox":
					specialAttributes = MsiInterop.ComboboxControlAttributes;
					break;
				case "PushButton":
					specialAttributes = MsiInterop.ButtonControlAttributes;
					break;
				case "CheckBox":
					specialAttributes = MsiInterop.CheckboxControlAttributes;
					break;
				case "RadioButtonGroup":
					specialAttributes = MsiInterop.RadioControlAttributes;
					break;
				case "ScrollableText":
				case "SelectionTree":
				case "DirectoryList":
				case "GroupBox":
					tabSkip = true;
					disabled = true;
					break;
				case "Line":
					tabSkip = true;
					disabled = true;
					break;
				case "Billboard":
					tabSkip = true;
					disabled = true;
					break;
				default:
					// TODO: throw an exception - Fail "Unknown control type: " & controlType & "  Attributes = &h" & Hex(row.IntegerData(MsiInterop.Control.Attributes))
					break;
			}

			// nullable field,
			int bits = rawBits & ~ignoreBits;
			if (disabled)
			{
				bits = bits | MsiInterop.MsidbControlAttributesEnabled;
			}

			bits = bits ^ MsiInterop.CommonControlAttributesInvert;
			for (int index = 0; index < 16; index++)
			{
				if (index < commonCount && 0 < (bits & 1))
				{
					string name = MsiInterop.CommonControlAttributes[index];
					if (null == name || 0 == name.Length)
					{
						// TODO: throw an exception - Fail "Unknown attribute at bit position " & index
					}
					this.core.WriteAttributeString(writer, name, "yes");
				}
				bits = bits / 2;
			}

			int limit = -1;
			int iconSize = 0;
			if (null != specialAttributes && 0 < specialAttributes.Length)
			{
				limit = specialAttributes.Length;

				for (int index = 0; index <= 15 && bits != 0; index++)
				{
					string name = null;
					if (index < limit)
					{
						name = specialAttributes[index];
					}

					if (0 < (bits & 1))
					{
						if (null != name && 0 < name.Length)
						{
							if (name.StartsWith("Icon") && 6 == name.Length)
							{
								iconSize += Convert.ToInt32(name.Substring(4, 2));
							}
							else
							{
								this.core.WriteAttributeString(writer, name, "yes");
							}
						}
					}
					bits = bits / 2;
				}
			}

			if (0 < iconSize)
			{
				this.core.WriteAttributeString(writer, "IconSize", iconSize.ToString());
			}

			if (tabDisabled && !tabSkip)
			{
				this.core.WriteAttributeString(writer, "TabSkip", "yes");
			}

			if (!tabDisabled && tabSkip)
			{
				this.core.WriteAttributeString(writer, "TabSkip", "no");
			}
		}

		/// <summary>
		/// Process control.
		/// </summary>
		/// <param name="writer">XmlWriter where the Intermediate should persist itself as XML.</param>
		/// <param name="parentWriter">XmlWriter where the Intermediate should persist itself as XML.</param>
		/// <param name="record">Record from Control table.</param>
		/// <param name="defaultControl">Name of default control.</param>
		/// <param name="cancelControl">Name of cancel control.</param>
		/// <param name="tabDisabled">Specifies if control should not be tabbed to.</param>
		private void ProcessControl(XmlWriter writer, XmlWriter parentWriter, Record record, string defaultControl, string cancelControl, bool tabDisabled)
		{
			const string tableName = "Control";
			const string tableNameDependent = "CheckBox";
			bool hasCheckBoxTable = false;
			if (!this.inputDatabase.TableExists(tableName))
			{
				return;
			}

			if (this.inputDatabase.TableExists(tableNameDependent))
			{
				hasCheckBoxTable = true;
			}

			string id = record[(int)MsiInterop.Control.Control];
			string controlType = record[(int)MsiInterop.Control.Type];

			string text = record[(int)MsiInterop.Control.Text];
			if (!(this.skipInstallShield && 0 < text.IndexOf("InstallShield")))
			{
				writer.WriteStartElement(tableName);
				this.core.WriteAttributeString(writer, "Id", id);
				this.core.WriteAttributeString(writer, "Type", controlType);
				this.core.WriteAttributeString(writer, "X", record[(int)MsiInterop.Control.X]);
				this.core.WriteAttributeString(writer, "Y", record[(int)MsiInterop.Control.Y]);
				this.core.WriteAttributeString(writer, "Width", record[(int)MsiInterop.Control.Width]);
				this.core.WriteAttributeString(writer, "Height", record[(int)MsiInterop.Control.Height]);
				this.core.WriteAttributeString(writer, "Property", record[(int)MsiInterop.Control.Property]);
				string[] helpArray = record[(int)MsiInterop.Control.Help].Split('|');
				if (0 < helpArray.Length)
				{
					this.core.WriteAttributeString(writer, "ToolTip", helpArray[0]);
					if (1 < helpArray.Length && 0 < helpArray[1].Length)
					{
						this.core.WriteAttributeString(writer, "Help", helpArray[1]);
					}
				}

				if (hasCheckBoxTable && "CheckBox" == controlType)
				{
					using (View checkBoxView = this.inputDatabase.OpenExecuteView(String.Concat("SELECT `Value` FROM `", tableNameDependent, "` WHERE `Property`='", record[(int)MsiInterop.Control.Property], "'")))
					{
						Record checkBoxRecord;
						while (checkBoxView.Fetch(out checkBoxRecord)) // will return a single value or will fail
						{
							this.core.WriteAttributeString(writer, "CheckBoxValue", checkBoxRecord[1]); // hard coded value because select (above) is for a single column
						}
					}
				}

				if (id == defaultControl)
				{
					this.core.WriteAttributeString(writer, "Default", "yes");
				}

				if (id == cancelControl)
				{
					this.core.WriteAttributeString(writer, "Cancel", "yes");
				}

				// TODO: test Attributes for blowing up when null, error violation of SDK constraint "This must be a non-negative number"
				this.ProcessControlAttributes(writer, controlType, Convert.ToInt32(record[(int)MsiInterop.Control.Attributes]), tabDisabled);

				if (0 < record[(int)MsiInterop.Control.Text].Length)
				{
					if (20 >= text.Length && !this.NeedsEscape(text))
					{
						this.core.WriteAttributeString(writer, "Text", text);
					}
					else
					{
						writer.WriteStartElement("Text");
						writer.WriteCData(text);
						writer.WriteEndElement();
					}
				}

				string property = record[(int)MsiInterop.Control.Property];
				if (0 < property.Length)
				{
					if ("ListBox" == controlType)
					{
						this.ProcessControlGroupTable(writer, "ListBox", record[(int)MsiInterop.Control.Property]);
					}
					else if ("RadioButtonGroup" == controlType)
					{
						this.ProcessControlGroupTable(writer, "RadioButton", record[(int)MsiInterop.Control.Property]);
					}
					else if ("ListView" == controlType)
					{
						this.ProcessControlGroupTable(writer, "ListView", record[(int)MsiInterop.Control.Property]);
					}
					else if ("ComboBox" == controlType)
					{
						this.ProcessControlGroupTable(writer, "ComboBox", record[(int)MsiInterop.Control.Property]);
					}
					else
					{
						this.EmitProperty(parentWriter, this.nonuiproperties, property);
					}
				}
				/* broken and not needed right now
							if ("Icon" == controlType && 0 < record[(int)MsiInterop.Control.Text].Length)
							{
								this.ProcessBinaryTable(writer, BinaryType.Binary, record[(int)MsiInterop.Control.Text]);
							}
				*/
				Record sqlParams = new Record(2);
				sqlParams.SetString(1, record[(int)MsiInterop.Control.Dialog]);
				sqlParams.SetString(2, record[(int)MsiInterop.Control.Control]);
				if (this.inputDatabase.TableExists("ControlEvent"))
				{
					using (View publishView = this.inputDatabase.OpenView("SELECT * FROM `ControlEvent` WHERE `Dialog_` = ? AND `Control_` = ? ORDER BY `Ordering`"))
					{
						publishView.Execute(sqlParams);
						Record publishRecord;
						while (publishView.Fetch(out publishRecord))
						{
							writer.WriteStartElement("Publish");
							string controlEvent = publishRecord[(int)MsiInterop.ControlEvent.Event];
							if (controlEvent.StartsWith("[") && controlEvent.EndsWith("]"))
							{
								this.core.WriteAttributeString(writer, "Property", controlEvent.Substring(1, controlEvent.Length - 2));
							}
							else
							{
								this.core.WriteAttributeString(writer, "Event", controlEvent);
							}
							this.core.WriteAttributeString(writer, "Value", publishRecord[(int)MsiInterop.ControlEvent.Argument]);
							writer.WriteCData(publishRecord[(int)MsiInterop.ControlEvent.Condition]);
							writer.WriteEndElement();
						}
					}
				}

				if (this.inputDatabase.TableExists("EventMapping"))
				{
					using (View subscribeView = this.inputDatabase.OpenView("SELECT * FROM `EventMapping` WHERE `Dialog_` = ? AND `Control_` = ?"))
					{
						subscribeView.Execute(sqlParams);
						Record subscribeRecord;
						while (subscribeView.Fetch(out subscribeRecord))
						{
							writer.WriteStartElement("Subscribe");
							this.core.WriteAttributeString(writer, "Event", subscribeRecord[(int)MsiInterop.EventMapping.Event]);
							this.core.WriteAttributeString(writer, "Attribute", subscribeRecord[(int)MsiInterop.EventMapping.Attribute]);
							writer.WriteEndElement();
						}
					}
				}

				if (this.inputDatabase.TableExists("ControlCondition"))
				{
					using (View conditionView = this.inputDatabase.OpenView("SELECT * FROM `ControlCondition` WHERE `Dialog_` = ? AND `Control_` = ?"))
					{
						conditionView.Execute(sqlParams);
						Record conditionRecord;
						while (conditionView.Fetch(out conditionRecord))
						{
							writer.WriteStartElement("Condition");
							this.core.WriteAttributeString(writer, "Action", conditionRecord[(int)MsiInterop.ControlCondition.Action].ToLower());
							writer.WriteCData(conditionRecord[(int)MsiInterop.ControlCondition.Condition]);
							writer.WriteEndElement();
						}
					}
				}
				writer.WriteEndElement();
			}
			else
			{
				this.core.OnMessage(WixWarnings.FilteringInstallShieldStuff(null, WarningLevel.Major, "Control", String.Concat(record[(int)MsiInterop.Control.Dialog], "::", record[(int)MsiInterop.Control.Control])));
			}
		}

		/// <summary>
		/// Process a control group table.
		/// </summary>
		/// <param name="writer">XmlWriter where the Intermediate should persist itself as XML.</param>
		/// <param name="tableName">Name of table to process.</param>
		/// <param name="propertyName">Name of property to process.</param>
		private void ProcessControlGroupTable(XmlWriter writer, string tableName, string propertyName)
		{
			if (!this.inputDatabase.TableExists(tableName))
			{
				return;
			}

			string query = String.Concat("SELECT * FROM `", tableName, "`");
			if (0 < propertyName.Length)
			{
				if (this.uicontrolProperties.ContainsKey(propertyName))
				{
					return;  // not an error because multiple controls can reference a property
				}
				query = String.Concat(query, " WHERE `Property`='", propertyName, "'");
			}

			using (View view = this.inputDatabase.OpenExecuteView(String.Concat(query)))
			{
				string lastProperty = null;
				string attrName = null;

				Record record;
				while (view.Fetch(out record))
				{
					string property = record[(int)MsiInterop.ListBox.Property];
					if (property != lastProperty)
					{
						if (null != lastProperty)
						{
							writer.WriteEndElement();
						}

						if (this.uicontrolProperties.ContainsKey(property))
						{
							continue;  // not an error because multiple controls can reference a property
						}

						if ("RadioButton" == tableName)
						{
							writer.WriteStartElement("RadioButtonGroup");
							attrName = "Text";

							// query control table to see if this is really "Icon"
							if (this.inputDatabase.TableExists("Control"))
							{
								using (View attributesView = this.inputDatabase.OpenExecuteView(String.Concat("SELECT `Attributes` FROM `Control` WHERE `Type`='RadioButtonGroup' AND `Property`='", property, "'")))
								{
									Record attributesRecord;
									while (attributesView.Fetch(out attributesRecord))
									{
										if (0 < (Convert.ToInt32(attributesRecord[1]) & (MsiInterop.MsidbControlAttributesIcon + MsiInterop.MsidbControlAttributesBitmap)))
										{
											attrName = "Icon";
										}
									}
								}
							}
						}
						else
						{
							writer.WriteStartElement(tableName);
						}

						lastProperty = property;
						this.uicontrolProperties[property] = true;
						this.core.WriteAttributeString(writer, "Property", property);
					}

					if (tableName == "RadioButton")
					{
						writer.WriteStartElement(tableName);
						this.core.WriteAttributeString(writer, "Value", record[(int)MsiInterop.RadioButton.Value]);
						this.core.WriteAttributeString(writer, "X", record[(int)MsiInterop.RadioButton.X]);
						this.core.WriteAttributeString(writer, "Y", record[(int)MsiInterop.RadioButton.Y]);
						this.core.WriteAttributeString(writer, "Width", record[(int)MsiInterop.RadioButton.Width]);
						this.core.WriteAttributeString(writer, "Height", record[(int)MsiInterop.RadioButton.Height]);
						this.core.WriteAttributeString(writer, attrName, record[(int)MsiInterop.RadioButton.Text]);

						string[] helpArray = record[(int)MsiInterop.RadioButton.Help].Split('|');
						if (0 < helpArray.Length)
						{
							this.core.WriteAttributeString(writer, "ToolTip", helpArray[0]);
							if (1 < helpArray.Length && 0 < helpArray[1].Length)
							{
								this.core.WriteAttributeString(writer, "Help", helpArray[1]);
							}
						}
					}
					else // '"ListBox" or "ListView" ' tables identical except for Icon field only in ListView
					{
						writer.WriteStartElement("ListItem");
						this.core.WriteAttributeString(writer, "Value", record[(int)MsiInterop.ListView.Value]);
						this.core.WriteAttributeString(writer, "Text", record[(int)MsiInterop.ListView.Text]);
						this.core.WriteAttributeString(writer, "Icon", record[(int)MsiInterop.ListView.Binary]);
					}

					writer.WriteEndElement();
				}

				if (null != lastProperty)
				{
					writer.WriteEndElement();
				}
			}
		}

		/// <summary>
		/// Processes the ActionText table.
		/// </summary>
		/// <param name="parentWriter">parent writer.</param>
		private void ProcessActionText(XmlWriter parentWriter)
		{
			const string tableName = "ActionText";
			XmlWriter writer = null;
			if (!this.inputDatabase.TableExists(tableName))
			{
				return;
			}

			using (View view = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `", tableName, "`")))
			{
				Record record;
				while (view.Fetch(out record))
				{
					if (null == writer)
					{
						writer = this.InitializeXmlTextWriter(Path.GetFullPath(Path.Combine(this.outputFolder, "SkuLibrary\\UI\\ActionText.wxs")));
						if (this.generateFragments)
						{
							// initalize the fragment
							writer.WriteStartElement("Fragment");
							this.core.WriteAttributeString(writer, "Id", "ActionText");

							// initalize the UI element
							writer.WriteStartElement("UI");

							// create reference to fragment
							parentWriter.WriteStartElement("FragmentRef");
							this.core.WriteAttributeString(parentWriter, "Id", "ActionText");
							parentWriter.WriteEndElement();
						}
					}
					writer.WriteStartElement("ProgressText");
					this.core.WriteAttributeString(writer, "Action", record[(int)MsiInterop.ActionText.Action]);
					this.core.WriteAttributeString(writer, "Template", record[(int)MsiInterop.ActionText.Template]);
					writer.WriteCData(record[(int)MsiInterop.ActionText.Description]);
					writer.WriteEndElement();
				}

				if (this.generateFragments && null != writer)
				{
					// terminate the UI element
					writer.WriteEndElement();

					// terminate the fragment
					writer.WriteEndElement();
				}
			}
		}

		/// <summary>
		/// Processes the Error table.
		/// </summary>
		/// <param name="parentWriter">parent writer.</param>
		private void ProcessErrorTable(XmlWriter parentWriter)
		{
			const string tableName = "Error";
			XmlWriter writer = null;
			if (!this.inputDatabase.TableExists(tableName))
			{
				return;
			}
			else if (this.processingModule)
			{
				this.core.OnMessage(WixWarnings.IllegalMergeModuleTable(null, WarningLevel.Major, "Error"));
				return;
			}

			using (View view = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `", tableName, "`")))
			{
				Record record;
				while (view.Fetch(out record))
				{
					if (null == writer)
					{
						writer = this.InitializeXmlTextWriter(Path.GetFullPath(Path.Combine(this.outputFolder, "SkuLibrary\\UI\\Error.wxs")));
						if (this.generateFragments)
						{
							// initalize the fragment
							writer.WriteStartElement("Fragment");
							this.core.WriteAttributeString(writer, "Id", tableName);

							// initalize the UI element
							writer.WriteStartElement("UI");

							// create reference to fragment
							parentWriter.WriteStartElement("FragmentRef");
							this.core.WriteAttributeString(parentWriter, "Id", tableName);
							parentWriter.WriteEndElement();
						}
					}
					writer.WriteStartElement("Error");
					this.core.WriteAttributeString(writer, "Id", record[(int)MsiInterop.Error.Error]);
					writer.WriteCData(record[(int)MsiInterop.Error.Message]);
					writer.WriteEndElement();
				}

				if (this.generateFragments && null != writer)
				{
					// terminate the UI element
					writer.WriteEndElement();

					// terminate the fragment
					writer.WriteEndElement();
				}
			}
		}

		/// <summary>
		/// Processes the ProcessSFPCatalogTables table.
		/// </summary>
		/// <returns>true if SFP catalog.</returns>
		/// <param name="parentWriter">parent writer.</param>
		/// <param name="parentCatalog">parent catalog.</param>
		private bool ProcessSFPCatalogTables(XmlWriter parentWriter, string parentCatalog)
		{
			const string tableName = "SFPCatalog";
			XmlWriter writer = null;
			bool foundSFPCatalog = false;
			if (!this.inputDatabase.TableExists(tableName))
			{
				return foundSFPCatalog;
			}
			string query = String.Concat("SELECT * FROM `", tableName, "`");
			if (null != parentCatalog && 0 < parentCatalog.Length)
			{
				query = String.Concat(" AND `SFPCatalog`='", parentCatalog, "'");
				writer = parentWriter;
			}

			if (this.inputDatabase.TableExists(tableName))
			{
				using (View sfpView = this.inputDatabase.OpenExecuteView(query))
				{
					Record sfpRecord;
					while (sfpView.Fetch(out sfpRecord))
					{
						foundSFPCatalog = true;
						if (null == writer)
						{
							writer = this.InitializeXmlTextWriter(Path.GetFullPath(Path.Combine(this.outputFolder, "BaseLibrary\\SFPCatalog.wxs")));
							if (this.generateFragments)
							{
								// initalize the fragment
								writer.WriteStartElement("Fragment");
								this.core.WriteAttributeString(writer, "Id", tableName);

								// create reference to fragment
								parentWriter.WriteStartElement("FragmentRef");
								this.core.WriteAttributeString(parentWriter, "Id", tableName);
								parentWriter.WriteEndElement();
							}
						}

						// TODO: does this get modularized?
						string key = sfpRecord[(int)MsiInterop.SFPCatalog.SFPCatalog];
						if (this.sfpCatalogProcessed.ContainsKey(key))
						{
							continue;

							// TODO: determine if SFPCatalogRef is also needed
							// writer.WriteStartElement("SFPCatalogRef");
							// this.core.WriteAttributeString(writer, "Id", key);
						}
						else
						{
							this.sfpCatalogProcessed[key] = true;
							writer.WriteStartElement("SFPCatalog");
							this.core.WriteAttributeString(writer, "Name", key);

							// TODO: extract binary from column sfpRecord[(int)MsiInterop.SFPCatalog.Catalog] into file system and mark up src attribute
							string dependency = sfpRecord[(int)MsiInterop.SFPCatalog.Dependency];
							if (null != dependency && 0 < dependency.Length)
							{
								if (!this.ProcessSFPCatalogTables(writer, dependency))
								{
									this.core.WriteAttributeString(writer, "Dependency", dependency);
								}
							}

							if (this.inputDatabase.TableExists("FileSFPCatalog"))
							{
								using (View fsfpView = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `FileSFPCatalog` AND `SFPCatalog_`='", key, "'")))
								{
									Record fsfpRecord;
									while (fsfpView.Fetch(out fsfpRecord))
									{
										writer.WriteStartElement("SFPFile");
										this.core.WriteAttributeString(writer, "Id", fsfpRecord[(int)MsiInterop.FileSFPCatalog.File_]);
										writer.WriteEndElement();
									}
								}
							}
						}
					}

					if (this.generateFragments && null != writer && writer != parentWriter)
					{
						// terminate the fragment
						writer.WriteEndElement();
					}
				}
			}
			return foundSFPCatalog;
		}

		/// <summary>
		/// Process Module Configuration Table.
		/// </summary>
		/// <param name="parentWriter">XmlWriter where the Intermediate should persist itself as XML.</param>
		private void ProcessModuleConfigurationTable(XmlWriter parentWriter)
		{
			const string tableName = "ModuleConfiguration";
			if (!this.inputDatabase.TableExists(tableName))
			{
				return;
			}

			using (View view = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `", tableName, "`")))
			{
				Record record;
				if (view.Fetch(out record))
				{
					parentWriter.WriteStartElement("Configuration");
					this.core.WriteAttributeString(parentWriter, "Name", record[(int)MsiInterop.ModuleConfiguration.Name]); //non-nullable
					string format = null;
					switch (Convert.ToInt32(record[(int)MsiInterop.ModuleConfiguration.Format])) //non-nullable
					{
						case MsiInterop.MsidbModuleConfigurationFormatText:
							format = "Text";
							break;
						case MsiInterop.MsidbModuleConfigurationFormatKey:
							format = "Key";
							break;
						case MsiInterop.MsidbModuleConfigurationFormatInteger:
							format = "Integer";
							break;
						case MsiInterop.MsidbModuleConfigurationFormatBitfield:
							format = "Bitfield";
							break;
						default:
							// TOOD: throw an exception
							break;
					}
					this.core.WriteAttributeString(parentWriter, "Format", format);

					// nullable
					this.core.WriteAttributeString(parentWriter, "Type", record[(int)MsiInterop.ModuleConfiguration.Type]);

					// nullable
					this.core.WriteAttributeString(parentWriter, "ContextData", record[(int)MsiInterop.ModuleConfiguration.ContextData]);

					// nullable
					this.core.WriteAttributeString(parentWriter, "DefaultValue", record[(int)MsiInterop.ModuleConfiguration.DefaultValue]);

					// nullable
					string attributes = record[(int)MsiInterop.ModuleConfiguration.Attributes];
					int bits = 0;
					if (null != attributes && 0 < attributes.Length)
					{
						bits = Convert.ToInt32(attributes);
					}

					if (0 < (bits & MsiInterop.MsidbMsmConfigurableOptionKeyNoOrphan)) // NoOrphan bit set
					{
						this.core.WriteAttributeString(parentWriter, "KeyNoOrphan", "yes");
					}

					if (0 < (bits & MsiInterop.MsidbMsmConfigurableOptionNonNullable)) // NonNullable bit set
					{
						this.core.WriteAttributeString(parentWriter, "NonNullable", "yes");
					}
					this.core.WriteAttributeString(parentWriter, "DisplayName", record[(int)MsiInterop.ModuleConfiguration.DisplayName]); //nullable
					this.core.WriteAttributeString(parentWriter, "Description", record[(int)MsiInterop.ModuleConfiguration.Description]); //nullable
					this.core.WriteAttributeString(parentWriter, "HelpLocation", record[(int)MsiInterop.ModuleConfiguration.HelpLocation]); //nullable
					this.core.WriteAttributeString(parentWriter, "HelpKeyword", record[(int)MsiInterop.ModuleConfiguration.HelpKeyword]); //nullable
					parentWriter.WriteEndElement();
				}
			}
		}

		/// <summary>
		/// Process Module Dependency Table.
		/// </summary>
		/// <param name="parentWriter">XmlWriter where the Intermediate should persist itself as XML.</param>
		private void ProcessModuleDependencyTable(XmlWriter parentWriter)
		{
			const string tableName = "ModuleDependency";
			if (!this.inputDatabase.TableExists(tableName))
			{
				return;
			}

			using (View view = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `", tableName, "`")))
			{
				Record record;
				while (view.Fetch(out record))
				{
					parentWriter.WriteStartElement("Dependency");
					this.core.WriteAttributeString(parentWriter, "RequiredId", record[(int)MsiInterop.ModuleDependency.RequiredID]); //non-nullable
					this.core.WriteAttributeString(parentWriter, "RequiredLanguage", record[(int)MsiInterop.ModuleDependency.RequiredLanguage]); //non-nullable
					this.core.WriteAttributeString(parentWriter, "RequiredVersion", record[(int)MsiInterop.ModuleDependency.RequiredVersion]); //nullable
					parentWriter.WriteEndElement();
				}
			}
		}

		/// <summary>
		/// Process Module Exclusion Table.
		/// </summary>
		/// <param name="parentWriter">XmlWriter where the Intermediate should persist itself as XML.</param>
		private void ProcessModuleExclusionTable(XmlWriter parentWriter)
		{
			const string tableName = "ModuleExclusion";
			if (!this.inputDatabase.TableExists(tableName))
			{
				return;
			}

			using (View view = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `", tableName, "`")))
			{
				Record record;
				if (view.Fetch(out record))
				{
					parentWriter.WriteStartElement("Exclusion");
					this.core.WriteAttributeString(parentWriter, "ExcludedId", record[(int)MsiInterop.ModuleExclusion.ExcludedID]); //non-nullable
					this.core.WriteAttributeString(parentWriter, "ExcludedLanguage", record[(int)MsiInterop.ModuleExclusion.ExcludedLanguage]); //non-nullable
					this.core.WriteAttributeString(parentWriter, "ExcludedMinVersion", record[(int)MsiInterop.ModuleExclusion.ExcludedMinVersion]); //nullable
					this.core.WriteAttributeString(parentWriter, "ExcludedMaxVersion", record[(int)MsiInterop.ModuleExclusion.ExcludedMaxVersion]); //nullable
					parentWriter.WriteEndElement();
				}
			}
		}

		/// <summary>
		/// Process Module Substitution Table.
		/// </summary>
		/// <param name="parentWriter">XmlWriter where the Intermediate should persist itself as XML.</param>
		private void ProcessModuleSubstitutionTable(XmlWriter parentWriter)
		{
			const string tableName = "ModuleSubstitution";
			if (!this.inputDatabase.TableExists(tableName))
			{
				return;
			}

			using (View view = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `", tableName, "`")))
			{
				Record record;
				if (view.Fetch(out record))
				{
					parentWriter.WriteStartElement("Substitution");
					this.core.WriteAttributeString(parentWriter, "Table", record[(int)MsiInterop.ModuleSubstitution.Table]); //non-nullable
					this.core.WriteAttributeString(parentWriter, "Row", record[(int)MsiInterop.ModuleSubstitution.Row]); //non-nullable
					this.core.WriteAttributeString(parentWriter, "Column", record[(int)MsiInterop.ModuleSubstitution.Column]); //non-nullable
					this.core.WriteAttributeString(parentWriter, "Value", record[(int)MsiInterop.ModuleSubstitution.Value]); //nullable
					parentWriter.WriteEndElement();
				}
			}
		}

		/// <summary>
		/// Process Module Signature Table.
		/// </summary>
		/// <param name="name">Name of module.</param>
		/// <param name="id">Module guid.</param>
		/// <param name="language">Module language.</param>
		/// <param name="version">Version of module.</param>
		private void ProcessModuleSignatureTable(ref string name, ref string id, ref string language, ref string version)
		{
			const string tableName = "ModuleSignature";
			if (!this.inputDatabase.TableExists(tableName))
			{
				return;
			}

			string moduleId = String.Empty;
			using (View view = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `", tableName, "`")))
			{
				Record record;
				if (view.Fetch(out record))
				{
					moduleId = record[(int)MsiInterop.ModuleSignature.ModuleID];
					language = record[(int)MsiInterop.ModuleSignature.Language];
					version = record[(int)MsiInterop.ModuleSignature.Version];

					Regex guidRegex = new Regex(@"(\.[0-9a-fA-F]{8}_[0-9a-fA-F]{4}_[0-9a-fA-F]{4}_[0-9a-fA-F]{4}_[0-9a-fA-F]{12})$", RegexOptions.IgnoreCase);

					// Match the regular expression pattern against a text string.
					name = moduleId;
					Match guidMatch = guidRegex.Match(name);
					if (guidMatch.Success)
					{
						id = guidMatch.Value;
						name = name.Replace(id, ""); // remove any occurrances of the ModuleGuid
						id = id.Replace("_", "-");
						id = id.Replace(".", "");
					}
					else
					{
						this.core.OnMessage(WixWarnings.UnknownModularization(null, WarningLevel.Major, name));
						id = name;
					}
				}
			}
		}

		/// <summary>
		/// Processes any floating Components.
		/// </summary>
		/// <param name="writer">XmlWriter where the Intermediate should persist itself as XML.</param>
		private void ProcessOrphanedComponents(XmlWriter writer)
		{
			const string tableName = "Component";
			if (!this.inputDatabase.TableExists(tableName))
			{
				return;
			}

			using (View view = this.inputDatabase.OpenExecuteView(String.Concat("SELECT DISTINCT Directory_ FROM `", tableName, "`")))
			{
				Record record;
				while (view.Fetch(out record))
				{
					string directory = record[(int)MsiInterop.Directory.Directory];
					if (directory == "TARGETDIR")
					{
						continue;
					}
					string parentDirectory = String.Empty;
					using (View directoryView = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM Directory WHERE Directory='", directory, "'")))
					{
						Record directoryRecord;
						while (directoryView.Fetch(out directoryRecord))
						{
							parentDirectory = directoryRecord[(int)MsiInterop.Directory.DirectoryParent];
						}
					}

					if (0 == parentDirectory.Length || this.processedDirectories.ContainsKey(parentDirectory))
					{
						if (!this.processedDirectories.ContainsKey(directory))
						{
							string rootPathShort = "";
							string rootPathLong = "";
							writer.WriteStartElement("Directory");
							string directoryName = this.StripModuleId(this.core.GetValidIdentifier(directory, "Directory"));
							this.core.WriteAttributeString(writer, "Id", directoryName);
							string shortName = directoryName.Substring(0, 8);
							if (directoryName.Length <= 8)
							{
								this.core.WriteAttributeString(writer, "Name", directory);
								shortName = directory;
							}
							else
							{
								this.core.WriteAttributeString(writer, "Name", shortName);
								this.core.WriteAttributeString(writer, "LongName", directoryName);
							}

							if (0 < parentDirectory.Length)
							{
								rootPathShort = Path.Combine(this.processedDirectories[parentDirectory].ToString(), shortName);
								rootPathLong = Path.Combine(this.processedDirectories[parentDirectory].ToString(), directoryName);
							}
							else
							{
								rootPathShort = shortName;
								rootPathLong = directoryName;
							}
							this.ProcessComponentTable(directory, rootPathShort, rootPathLong);
							writer.WriteEndElement();
							this.processedDirectories.Add(directory, rootPathShort);
						}
					}
					else
					{
						this.ProcessOrphanedDirectories(writer, parentDirectory);
					}
				}
			}
		}

		/// <summary>
		/// Processes any floating directories.
		/// </summary>
		/// <param name="writer">XmlWriter where the Intermediate should persist itself as XML.</param>
		/// <param name="directory">Directory identifier to start processing from.</param>
		private void ProcessOrphanedDirectories(XmlWriter writer, string directory)
		{
			if (String.Empty == directory)
			{
				return;
			}

			using (View view = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `Directory` WHERE `Directory` = '", directory, "'")))
			{
				Record record;
				if (view.Fetch(out record))
				{
					string parentDir = record[(int)MsiInterop.Directory.DirectoryParent];
					if (!this.processedDirectories.ContainsKey(parentDir))
					{
						this.ProcessOrphanedDirectories(writer, parentDir);
					}
				}
				else
				{
					this.ProcessDirectoryTable(writer, directory, "", "");
				}
			}
		}

		/// <summary>
		/// Helper function to remove the module guid from a string.
		/// </summary>
		/// <param name="str">String to remove the module guid from.</param>
		/// <returns>str with the module guid stripped</returns>
		private string StripModuleId(string str)
		{
			if (this.processingModule)
			{
				// this code assumes modules were built using the msi.chm topic "Naming Primary Keys in Merge Module Databases [Windows Installer]"
				string before = str;
				string dottedModuleGuid = String.Concat(".", this.moduleIdUnderscored);
				str = str.Replace(dottedModuleGuid, ""); // remove any occurrances of the ModuleGuid
				if (str == before)
				{
					// when modules were NOT built using the msi.chm topic "Naming Primary Keys in Merge Module Databases [Windows Installer]",
					// we will trim the GUID and then warn that the identifier has changed.  NOTE: this pattern can also occur in a formatted field.
					// Compile the regular expression.
					Regex guidRegex = new Regex(@"(\.[0-9a-fA-F]{8}_[0-9a-fA-F]{4}_[0-9a-fA-F]{4}_[0-9a-fA-F]{4}_[0-9a-fA-F]{12})", RegexOptions.IgnoreCase);

					// Match the regular expression pattern against a text string.
					Match guidMatch = guidRegex.Match(str);
					if (guidMatch.Success)
					{
						string wrongGuidDotted = guidMatch.Value;
						this.core.OnMessage(WixWarnings.UnknownModularization(null, WarningLevel.Major, str, wrongGuidDotted, dottedModuleGuid));
						str = str.Replace(wrongGuidDotted, ""); // remove any occurrances of the ModuleGuid
					}
				}
			}
			return str;
		}

		/// <summary>
		/// Helper function to remove the braces from either end of a guid string.
		/// </summary>
		/// <param name="guid">Guid in string format.</param>
		/// <returns>Guid without braces.</returns>
		private string StripBraces(string guid)
		{
			guid = guid.TrimStart("{ \r\n\t".ToCharArray());
			guid = guid.TrimEnd("} \r\n\t".ToCharArray());
			return guid;
		}

		/// <summary>
		/// Helper function to check if text needs to be escaped in XML.
		/// </summary>
		/// <param name="text">String to test if it could use escaping</param>
		/// <returns>true if text has values that would have to be escaped in XML.</returns>
		private bool NeedsEscape(string text)
		{
			return 0 < text.IndexOfAny("<>&'\"".ToCharArray());
		}

		/// <summary>
		/// Helper method to initialize and return appropriate XmlTextWriter
		/// </summary>
		/// <param name="path">path to file</param>
		/// <returns>XmlTextWriter appropriate to context.</returns>
		private XmlTextWriter InitializeXmlTextWriter(string path)
		{
			XmlTextWriter writer = this.core.Writer;
			if (this.generateFragments)
			{
				if (!Directory.Exists(Path.GetDirectoryName(path)))
				{
					Directory.CreateDirectory(Path.GetDirectoryName(path));
				}

				if (this.openFragments.ContainsKey(path))
				{
					// todo throw an exception
					Console.WriteLine("Hey! that's a duplicate fragment.  '{0}'.", path);
				}
				else
				{
					writer = new XmlTextWriter(path, Encoding.UTF8);
					writer.Formatting = Formatting.Indented; // use indenting for readability
					this.StartDocument(writer);
					this.openFragments[path] = writer;
				}
			}
			return writer;
		}

		/// <summary>
		/// Helper method to finalize and close appropriate XmlTextWriter
		/// </summary>
		/// <param name="writer">XmlWriter where the Intermediate should persist itself as XML.</param>
		private void FinalizeXmlTextWriter(XmlTextWriter writer)
		{
			if (this.generateFragments)
			{
				try
				{
					this.EndDocument(writer);

					// if no error was found, save the result to the specified file
					if (!this.foundError)
					{
						writer.Flush();
					}
				}
				finally
				{
					if (null != writer)
					{
						writer.Close();
						writer = null;
					}
				}
			}
		}

		/// <summary>
		/// Helper method to write the start of a document
		/// </summary>
		/// <param name="writer">writer in which to start document.</param>
		private void StartDocument(XmlWriter writer)
		{
			writer.WriteStartDocument();
			writer.WriteStartElement("Wix");
			this.core.WriteAttributeString(writer, "xmlns", "http://schemas.microsoft.com/wix/2003/01/wi");
		}

		/// <summary>
		/// Helper method to write the end of a document
		/// </summary>
		/// <param name="writer">writer in which to end document.</param>
		private void EndDocument(XmlWriter writer)
		{
			writer.WriteEndElement();
			writer.WriteEndDocument();
		}

		/// <summary>
		/// Write the Cab to disk.
		/// </summary>
		/// <param name="filePath">Specifies the path to the file to contain the stream.</param>
		/// <param name="cabName">Specifies the name of the file in the stream.</param>
		private void ExtractCabFromPackage(string filePath, string cabName)
		{
			using (View view = this.inputDatabase.OpenExecuteView(String.Concat("SELECT * FROM `_Streams` WHERE `Name` = '", cabName, "'")))
			{
				Record record;
				if (view.Fetch(out record))
				{
					FileStream mergeModuleCab = null;
					BinaryWriter writer = null;
					try
					{
						mergeModuleCab = new FileStream(filePath, FileMode.Create);

						// Create the writer for data.
						writer = new BinaryWriter(mergeModuleCab);
						int count = 512;
						byte[] buf = new byte[count];
						while (count == buf.Length)
						{
							count = record.GetStream((int)MsiInterop.Storages.Data, buf, count);
							if (buf.Length > 0)
							{
								// Write data to Test.data.
								writer.Write(buf);
							}
						}
					}
					finally
					{
						if (writer != null)
						{
							writer.Close();
						}

						if (mergeModuleCab != null)
						{
							mergeModuleCab.Close();
						}
					}
				}
			}
		}

		/// <summary>
		/// Write files from Cab to disk.
		/// </summary>
		/// <param name="cabFileSpec">Specifies the path to the CAB containing the files.</param>
		/// <param name="diskId">DiskId of the cab.</param>
		private void ExtractFilesFromCab(string cabFileSpec, int diskId)
		{
			WixExtractCab extractCab = null;
			string extractDir = this.extractFolder;

			if (!this.processingModule)
			{
				extractDir = Path.Combine(extractDir, diskId.ToString());
			}

			if (Directory.Exists(extractDir))
			{
				Directory.Delete(extractDir, true);
			}

			try
			{
				if (File.Exists(cabFileSpec))
				{
					extractCab = new WixExtractCab();
					Directory.CreateDirectory(extractDir);
					extractCab.Extract(cabFileSpec, extractDir);
				}
			}
			catch (WixCabExtractionException)
			{
				this.core.OnMessage(WixErrors.CabExtractionFailed(cabFileSpec, extractDir));
			}
			finally
			{
				if (null != extractCab)
				{
					try
					{
						extractCab.Close();
					}
					catch (WixCabExtractionException)
					{
						this.core.OnMessage(WixErrors.CabClosureFailed(cabFileSpec));
					}
				}
			}
		}

		/// <summary>
		/// Delete the temporary files.  Code is taken from linker.cs.
		/// </summary>
		/// <param name="tempFolder">temporary folder location.</param>
		/// <returns>Returns true if files were deleted.</returns>
		private bool DeleteTempFiles(string tempFolder)
		{
			try
			{
				Directory.Delete(tempFolder, true); // toast the whole temp directory
			}
			catch (UnauthorizedAccessException)
			{
				Console.WriteLine(String.Concat("Access denied to delete ", tempFolder));

				// TODO: unmark the file "read-only" and try again before giving up like this
				return false;
			}
			catch (DirectoryNotFoundException)
			{
				// if the path doesn't exist, then there is nothing for us to worry about
			}
			catch (IOException) // file is in use
			{
				Console.WriteLine(String.Concat("File ", tempFolder, " in use and cannot be deleted"));

				// TODO: retry before giving up like this
				return false;
			}

			return true;
		}
	}
}
