//-------------------------------------------------------------------------------------------------
// <copyright file="Common.cs" company="Microsoft">
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
// Common Wix utility methods and types.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;
	using System.Collections;
	using System.Collections.Specialized;
	using System.Diagnostics;
	using System.IO;
	using System.Reflection;
	using System.Text;
	using System.Xml;
	using System.Xml.Schema;

	/// <summary>
	/// Utility methods for wix.
	/// </summary>
	public class Common
	{
		private static readonly Version intermediateVersion = new Version("2.0.2207.0");
		private static readonly Version libraryVersion = new Version("2.0.2207.0");
		private static readonly Version outputVersion = new Version("2.0.2324.0");

		private static TableDefinitionCollection tableDefinitions;
		private static ActionTable standardActions;
		private static Hashtable standardActionsHash;
		private static Hashtable standardDirectories;
		private static Hashtable standardProperties;

		/// <summary>
		/// Gets the version of the intermediate file format.
		/// </summary>
		/// <value>Version of the intermediate file format.</value>
		public static Version IntermediateFormatVersion
		{
			get { return intermediateVersion; }
		}

		/// <summary>
		/// Gets the version of the library file format
		/// </summary>
		/// <value>Version of the library file format.</value>
		public static Version LibraryFormatVersion
		{
			get { return libraryVersion; }
		}

		/// <summary>
		/// Gets the version of the output file format
		/// </summary>
		/// <value>Version of the output file format.</value>
		public static Version OutputFormatVersion
		{
			get { return outputVersion; }
		}

		/// <summary>
		/// Get a set of files that possibly have a search pattern in the path (such as '*').
		/// </summary>
		/// <param name="searchPath">Search path to find files in.</param>
		/// <param name="fileType">Type of file; typically "Source".</param>
		/// <returns>An array of FileInfo objects matching the search path.</returns>
		/// <remarks>
		/// This method is written in this verbose way because it needs to support ".." in the path.
		/// It needs the directory path isolated from the file name in order to use Directory.GetFiles
		/// or DirectoryInfo.GetFiles.  The only way to get this directory path is manually since
		/// Path.GetDirectoryName does not support ".." in the path.
		/// </remarks>
		/// <exception cref="WixFileNotFoundException">Throws WixFileNotFoundException if no file matching the pattern can be found.</exception>
		public static FileInfo[] GetFiles(string searchPath, string fileType)
		{
			if (null == searchPath)
			{
				throw new ArgumentNullException("searchPath");
			}

			// convert alternate directory separators to the standard one
			string filePath = searchPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
			int lastSeparator = filePath.LastIndexOf(Path.DirectorySeparatorChar);
			FileInfo[] files = null;

			try
			{
				if (0 > lastSeparator)
				{
					DirectoryInfo directory = new DirectoryInfo(".");

					files = directory.GetFiles(filePath);
				}
				else // found directory separator
				{
					DirectoryInfo directory = new DirectoryInfo(filePath.Substring(0, lastSeparator + 1));
					string searchPattern = filePath.Substring(lastSeparator + 1);

					files = directory.GetFiles(searchPattern);
				}
			}
			catch (DirectoryNotFoundException)
			{
				// don't let this function throw the DirectoryNotFoundException. (this exception
				// occurs for non-existant directories and invalid characters in the searchPattern)
			}
			catch (IOException)
			{
				throw new WixFileNotFoundException(searchPath, fileType);
			}

			// file could not be found or path is invalid in some way
			if (null == files || 0 == files.Length)
			{
				throw new WixFileNotFoundException(searchPath, fileType);
			}

			return files;
		}

		/// <summary>
		/// Parse a response file.
		/// </summary>
		/// <param name="responseFile">The file to parse.</param>
		/// <returns>The array of arguments.</returns>
		public static string[] ParseResponseFile(string responseFile)
		{
			ArrayList newArgs = new ArrayList();

			using (StreamReader reader = new StreamReader(responseFile))
			{
				string line;

				while (null != (line = reader.ReadLine()))
				{
					StringBuilder newArg = new StringBuilder();
					bool betweenQuotes = false;
					for (int j = 0; j < line.Length; ++j)
					{
						// skip whitespace
						if (!betweenQuotes && (' ' == line[j] || '\t' == line[j]))
						{
							if (0 != newArg.Length)
							{
								newArgs.Add(newArg.ToString());
								newArg = new StringBuilder();
							}

							continue;
						}

						// if we're escaping a quote
						if ('\\' == line[j] && '"' == line[j])
						{
							++j;
						}
						else if ('"' == line[j])   // if we've hit a new quote
						{
							betweenQuotes = !betweenQuotes;
							continue;
						}

						newArg.Append(line[j]);
					}
					if (0 != newArg.Length)
					{
						newArgs.Add(newArg.ToString());
					}
				}
			}

			return (string[])newArgs.ToArray(typeof(string));
		}

		/// <summary>
		/// Gets an Xml reader from the stream in the specified assembly.
		/// </summary>
		/// <param name="assembly">Assembly to get embedded stream from.</param>
		/// <param name="resourceStreamName">Name of stream.</param>
		/// <returns>Xml reader for stream in assembly.</returns>
		/// <remarks>The returned reader should be closed when done processing the Xml.</remarks>
		internal static XmlReader GetXmlFromEmbeddedStream(Assembly assembly, string resourceStreamName)
		{
			Stream stream = assembly.GetManifestResourceStream(resourceStreamName);
			return new XmlTextReader(stream);
		}

		/// <summary>
		/// Gets an Xml validating reader from the stream in the specified assembly.
		/// </summary>
		/// <param name="assembly">Assembly to get embedded stream from.</param>
		/// <param name="resourceStreamName">Name of stream.</param>
		/// <param name="resourceSchemaStreamName">Name of stream with schema.</param>
		/// <param name="schemaNamespace">Namespace of schema.</param>
		/// <returns>Xml validating reader for stream in assembly.</returns>
		/// <remarks>The returned reader should be closed when done processing the Xml.</remarks>
		internal static XmlValidatingReader GetValidatedXmlFromEmbeddedStream(Assembly assembly, string resourceStreamName, string resourceSchemaStreamName, string schemaNamespace)
		{
			XmlValidatingReader validatingReader = null;

			using (Stream schemaStream = assembly.GetManifestResourceStream(resourceSchemaStreamName))
			{
				XmlReader reader = new XmlTextReader(schemaStream);
				XmlSchemaCollection schemas = new XmlSchemaCollection();
				schemas.Add(schemaNamespace, reader);

				Stream stream = assembly.GetManifestResourceStream(resourceStreamName);
				validatingReader = new XmlValidatingReader(new XmlTextReader(stream));
				validatingReader.Schemas.Add(schemas);
			}

			return validatingReader;
		}

		/// <summary>
		/// Gets the table definitions stored in this assembly.
		/// </summary>
		/// <param name="smallTables">Use small table definitions for MSI</param>
		/// <returns>Table definition collection for tables stored in this assembly.</returns>
		internal static TableDefinitionCollection GetTableDefinitions(bool smallTables)
		{
			if (null == tableDefinitions)
			{
				string tablesXml = smallTables ? "Microsoft.Tools.WindowsInstallerXml.Data.smalltables.xml" : "Microsoft.Tools.WindowsInstallerXml.Data.tables.xml";
				Assembly assembly = Assembly.GetExecutingAssembly();
				XmlReader tableDefinitionsReader = null;

				try
				{
#if DEBUG
					tableDefinitionsReader = Common.GetValidatedXmlFromEmbeddedStream(assembly, tablesXml, "Microsoft.Tools.WindowsInstallerXml.Xsd.tables.xsd", "http://schemas.microsoft.com/wix/2003/04/tables");
#else
					tableDefinitionsReader = Common.GetXmlFromEmbeddedStream(assembly, tablesXml);
#endif
					tableDefinitions = TableDefinitionCollection.Load(tableDefinitionsReader);
				}
				finally
				{
					if (null != tableDefinitionsReader)
					{
						tableDefinitionsReader.Close();
					}
				}
			}

			return tableDefinitions;
		}

		/// <summary>
		/// Gets the standard actions stored in this assembly.
		/// </summary>
		/// <returns>Table of standard actions in this assembly.</returns>
		internal static ActionTable GetStandardActions()
		{
			if (null == standardActions)
			{
				Assembly assembly = Assembly.GetExecutingAssembly();
				XmlReader actionDefinitionsReader = null;
				try
				{
#if DEBUG
					actionDefinitionsReader = Common.GetValidatedXmlFromEmbeddedStream(assembly, "Microsoft.Tools.WindowsInstallerXml.Data.actions.xml", "Microsoft.Tools.WindowsInstallerXml.Xsd.actions.xsd", "http://schemas.microsoft.com/wix/2003/04/actions");
#else
					actionDefinitionsReader = Common.GetXmlFromEmbeddedStream(assembly, "Microsoft.Tools.WindowsInstallerXml.Data.actions.xml");
#endif
					standardActions = new ActionTable(actionDefinitionsReader);
				}
				finally
				{
					if (null != actionDefinitionsReader)
					{
						actionDefinitionsReader.Close();
					}
				}
			}

			return standardActions;
		}

		/// <summary>
		/// Turn DateTime into the number of ticks since 12:00 am on Jan 1, 1601 CE.
		/// </summary>
		/// <param name="dateTime">The DateTime to convert.</param>
		/// <returns>The number of ticks.</returns>
		internal static long ToFileUtc(DateTime dateTime)
		{
			return dateTime.Subtract(new DateTime(1601, 1, 1, 0, 0, 0, 0)).Ticks;
		}

		/// <summary>
		/// Create a row in a section.
		/// </summary>
		/// <param name="sourceLineNumbers">Line number information about the row.</param>
		/// <param name="section">Section in which a row is to be added.</param>
		/// <param name="tableName">Table of the row.</param>
		/// <param name="tableDefinitions">Table definitions containing the table's defition.</param>
		/// <returns>The row added to the section.</returns>
		internal static Row CreateRowInSection(SourceLineNumberCollection sourceLineNumbers, Section section, string tableName, TableDefinitionCollection tableDefinitions)
		{
			Table table = section.Tables[tableName];
			if (null == table)
			{
				TableDefinition tableDef = tableDefinitions[tableName];
				if (null == tableDef)
				{
					throw new ApplicationException(String.Format("Unknown table name: {0}", tableName));
				}

				table = new Table(section, tableDef);
				section.Tables.Add(table);
			}

			return table.CreateRow(sourceLineNumbers);
		}

		/// <summary>
		/// Create a row in a section.
		/// </summary>
		/// <param name="sourceLineNumbers">Line number information about the row.</param>
		/// <param name="section">Section in which a row is to be added.</param>
		/// <param name="tableDefinition">Table definition for the row being added.</param>
		/// <returns>The row added to the section.</returns>
		internal static Row CreateRowInSection(SourceLineNumberCollection sourceLineNumbers, Section section, TableDefinition tableDefinition)
		{
			Table table = section.Tables[tableDefinition.Name];
			if (null == table)
			{
				table = new Table(section, tableDefinition);
				section.Tables.Add(table);
			}

			return table.CreateRow(sourceLineNumbers);
		}

		/// <summary>
		/// Find out if an identifier is supposed to be excluded from modularization.
		/// </summary>
		/// <param name="identifier">Identifier to check.</param>
		/// <returns>true if the modifer should not be included in modularization, false otherwise.</returns>
		internal static bool IsExcludedFromModularization(string identifier)
		{
			return Common.IsStandardAction(identifier) || Common.IsStandardProperty(identifier);
		}

		/// <summary>
		/// Find out if an action is a standard action.
		/// </summary>
		/// <param name="actionName">Name of the action.</param>
		/// <returns>true if the action is standard, false otherwise.</returns>
		internal static bool IsStandardAction(string actionName)
		{
			// if not initialized yet, do that
			if (null == standardActionsHash)
			{
				standardActionsHash = new Hashtable();
				standardActionsHash.Add("InstallInitialize", "Action");
				standardActionsHash.Add("InstallFinalize", "Action");
				standardActionsHash.Add("InstallFiles", "Action");
				standardActionsHash.Add("InstallAdminPackage", "Action");
				standardActionsHash.Add("FileCost", "Action");
				standardActionsHash.Add("CostInitialize", "Action");
				standardActionsHash.Add("CostFinalize", "Action");
				standardActionsHash.Add("InstallValidate", "Action");
				standardActionsHash.Add("ExecuteAction", "Action");
				standardActionsHash.Add("CreateShortcuts", "Action");
				standardActionsHash.Add("MsiPublishAssemblies", "Action");
				standardActionsHash.Add("PublishComponents", "Action");
				standardActionsHash.Add("PublishFeatures", "Action");
				standardActionsHash.Add("PublishProduct", "Action");
				standardActionsHash.Add("RegisterClassInfo", "Action");
				standardActionsHash.Add("RegisterExtensionInfo", "Action");
				standardActionsHash.Add("RegisterMIMEInfo", "Action");
				standardActionsHash.Add("RegisterProgIdInfo", "Action");
				standardActionsHash.Add("AllocateRegistrySpace", "Action");
				standardActionsHash.Add("AppSearch", "Action");
				standardActionsHash.Add("BindImage", "Action");
				standardActionsHash.Add("CCPSearch", "Action");
				standardActionsHash.Add("CreateFolders", "Action");
				standardActionsHash.Add("DeleteServices", "Action");
				standardActionsHash.Add("DuplicateFiles", "Action");
				standardActionsHash.Add("FindRelatedProducts", "Action");
				standardActionsHash.Add("InstallODBC", "Action");
				standardActionsHash.Add("InstallServices", "Action");
				standardActionsHash.Add("IsolateComponents", "Action");
				standardActionsHash.Add("LaunchConditions", "Action");
				standardActionsHash.Add("MigrateFeatureStates", "Action");
				standardActionsHash.Add("MoveFiles", "Action");
				standardActionsHash.Add("PatchFiles", "Action");
				standardActionsHash.Add("ProcessComponents", "Action");
				standardActionsHash.Add("RegisterComPlus", "Action");
				standardActionsHash.Add("RegisterFonts", "Action");
				standardActionsHash.Add("RegisterProduct", "Action");
				standardActionsHash.Add("RegisterTypeLibraries", "Action");
				standardActionsHash.Add("RegisterUser", "Action");
				standardActionsHash.Add("RemoveDuplicateFiles", "Action");
				standardActionsHash.Add("RemoveEnvironmentStrings", "Action");
				standardActionsHash.Add("RemoveFiles", "Action");
				standardActionsHash.Add("RemoveFolders", "Action");
				standardActionsHash.Add("RemoveIniValues", "Action");
				standardActionsHash.Add("RemoveODBC", "Action");
				standardActionsHash.Add("RemoveRegistryValues", "Action");
				standardActionsHash.Add("RemoveShortcuts", "Action");
				standardActionsHash.Add("RMCCPSearch", "Action");
				standardActionsHash.Add("SelfRegModules", "Action");
				standardActionsHash.Add("SelfUnregModules", "Action");
				standardActionsHash.Add("SetODBCFolders", "Action");
				standardActionsHash.Add("StartServices", "Action");
				standardActionsHash.Add("StopServices", "Action");
				standardActionsHash.Add("MsiUnpublishAssemblies", "Action");
				standardActionsHash.Add("UnpublishComponents", "Action");
				standardActionsHash.Add("UnpublishFeatures", "Action");
				standardActionsHash.Add("UnregisterClassInfo", "Action");
				standardActionsHash.Add("UnregisterComPlus", "Action");
				standardActionsHash.Add("UnregisterExtensionInfo", "Action");
				standardActionsHash.Add("UnregisterFonts", "Action");
				standardActionsHash.Add("UnregisterMIMEInfo", "Action");
				standardActionsHash.Add("UnregisterProgIdInfo", "Action");
				standardActionsHash.Add("UnregisterTypeLibraries", "Action");
				standardActionsHash.Add("ValidateProductID", "Action");
				standardActionsHash.Add("WriteEnvironmentStrings", "Action");
				standardActionsHash.Add("WriteIniValues", "Action");
				standardActionsHash.Add("WriteRegistryValues", "Action");
				standardActionsHash.Add("InstallExecute", "Action");
				standardActionsHash.Add("InstallExecuteAgain", "Action");
				standardActionsHash.Add("RemoveExistingProducts", "Action");
				standardActionsHash.Add("DisableRollback", "Action");
				standardActionsHash.Add("ScheduleReboot", "Action");
				standardActionsHash.Add("ForceReboot", "Action");
				standardActionsHash.Add("ResolveSource", "Action");
			}

			return standardActionsHash.ContainsKey(actionName);
		}

		/// <summary>
		/// Find out if a directory is a standard directory.
		/// </summary>
		/// <param name="directoryName">Name of the directory.</param>
		/// <returns>true if the directory is standard, false otherwise.</returns>
		internal static bool IsStandardDirectory(string directoryName)
		{
			// if not initialized yet, do that
			if (null == standardDirectories)
			{
				standardDirectories = new Hashtable();
				standardDirectories.Add("TARGETDIR", "Directory");
				standardDirectories.Add("AdminToolsFolder", "Directory");
				standardDirectories.Add("AppDataFolder", "Directory");
				standardDirectories.Add("CommonAppDataFolder", "Directory");
				standardDirectories.Add("CommonFilesFolder", "Directory");
				standardDirectories.Add("DesktopFolder", "Directory");
				standardDirectories.Add("FavoritesFolder", "Directory");
				standardDirectories.Add("FontsFolder", "Directory");
				standardDirectories.Add("LocalAppDataFolder", "Directory");
				standardDirectories.Add("MyPicturesFolder", "Directory");
				standardDirectories.Add("PersonalFolder", "Directory");
				standardDirectories.Add("ProgramFilesFolder", "Directory");
				standardDirectories.Add("ProgramMenuFolder", "Directory");
				standardDirectories.Add("SendToFolder", "Directory");
				standardDirectories.Add("StartMenuFolder", "Directory");
				standardDirectories.Add("StartupFolder", "Directory");
				standardDirectories.Add("System16Folder", "Directory");
				standardDirectories.Add("SystemFolder", "Directory");
				standardDirectories.Add("TempFolder", "Directory");
				standardDirectories.Add("TemplateFolder", "Directory");
				standardDirectories.Add("WindowsFolder", "Directory");
				standardDirectories.Add("CommonFiles64Folder", "Directory");
				standardDirectories.Add("ProgramFiles64Folder", "Directory");
				standardDirectories.Add("System64Folder", "Directory");
			}

			return standardDirectories.ContainsKey(directoryName);
		}

		/// <summary>
		/// Find out if a property is a standard property.
		///   References:
		///        Title:   Property Reference [Windows Installer]:
		///        URL:     http://msdn.microsoft.com/library/en-us/msi/setup/property_reference.asp
		///
		/// </summary>
		/// <param name="propertyName">Name of the property.</param>
		/// <returns>true if a property is standard, false otherwise.</returns>
		internal static bool IsStandardProperty(string propertyName)
		{
			// if not initialized yet, do that
			if (null == standardProperties)
			{
				standardProperties = new Hashtable();
				standardProperties.Add("~", "REG_MULTI_SZ/NULL marker");

				//    Name:  ACTION
				//    Desc:
				//    URL:
				//    Class: Configuration Properties
				//           Restricted Public Properties
				//    URL:   Configuration Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/configuration_properties.asp
				//           Restricted Public Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/restricted_public_properties.asp
				standardProperties.Add("ACTION", "Property");

				//    Name:  ADDDEFAULT
				//    Desc:
				//    URL:
				//    Class: Feature Installation Options Properties
				//    URL:   Feature Installation Options Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/feature_installation_options_properties.asp
				standardProperties.Add("ADDDEFAULT", "Property");

				//    Name:  ADDLOCAL
				//    Desc:
				//    URL:
				//    Class: Feature Installation Options Properties
				//    URL:   Feature Installation Options Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/feature_installation_options_properties.asp
				standardProperties.Add("ADDLOCAL", "Property");

				//    Name:  ADDSOURCE
				//    Desc:
				//    URL:
				//    Class: Feature Installation Options Properties
				//    URL:   Feature Installation Options Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/feature_installation_options_properties.asp
				standardProperties.Add("ADDDSOURCE", "Property");

				//    Name:  AdminProperties
				//    Desc:
				//    URL:
				//    Class: User Information Properties [Windows Installer]
				//    URL:   User Information Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/user_information_properties.asp
				standardProperties.Add("AdminProperties", "Property");

				//    Name:  AdminUser
				//    Desc:
				//    URL:
				//    Class: Operating System Properties
				//    URL:   Operating System Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/operating_system_properties.asp
				standardProperties.Add("AdminUser", "Property");

				//    Name:  ADVERTISE
				//    Desc:
				//    URL:
				//    Class: Feature Installation Options Properties
				//    URL:   Feature Installation Options Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/feature_installation_options_properties.asp
				standardProperties.Add("ADVERTISE", "Property");

				//    Name:  AFTERREBOOT
				//    Desc:
				//    URL:
				//    Class: Installation Status Properties
				//           Restricted Public Properties
				//    URL:   Installation Status Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/installation_status_properties.asp
				//           Restricted Public Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/restricted_public_properties.asp
				standardProperties.Add("AFTERREBOOT", "Property");

				//    Name:  AllowProductCodeMismatches
				//    Desc:
				//    URL:
				//    Class: Properties Table (PATCHWIZ.DLL)
				//    URL:   Properties Table (Patchwiz.dll) [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/properties_table_patchwiz_dll_.asp
				standardProperties.Add("AllowProductCodeMismatches", "Property");

				//    Name:  AllowProductVersionMajorMismatches
				//    Desc:
				//    URL:
				//    Class: Properties Table (PATCHWIZ.DLL)
				//    URL:   Properties Table (Patchwiz.dll) [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/properties_table_patchwiz_dll_.asp
				standardProperties.Add("AllowProductVersionMajorMismatches", "Property");

				//    Name:  ALLUSERS
				//    Desc:
				//    URL:
				//    Class: Configuration Properties
				//           Restricted Public Properties
				//    URL:   Configuration Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/configuration_properties.asp
				//           Restricted Public Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/restricted_public_properties.asp
				standardProperties.Add("ALLUSERS", "Property");

				//    Name:  Alpha
				//    Desc:
				//    URL:
				//    Class: Hardware Properties [Windows Installer]
				//    URL:   Hardware Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/hardware_properties.asp
				standardProperties.Add("Alpha", "Property");

				//    Name:  ApiPatchingSymbolFlags
				//    Desc:
				//    URL:
				//    Class: Properties Table (PATCHWIZ.DLL)
				//    URL:   Properties Table (Patchwiz.dll) [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/properties_table_patchwiz_dll_.asp
				standardProperties.Add("ApiPatchingSymbolFlags", "Property");

				//    Name:  ARPAUTHORIZEDCDFPREFIX
				//    Desc:
				//    URL:
				//    Class: Configuration Properties
				//    URL:   Configuration Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/configuration_properties.asp
				standardProperties.Add("ARPAUTHORIZEDCDFPREFIX", "Property");

				//    Name:  ARPCOMMENTS
				//    Desc:
				//    URL:
				//    Class: Configuration Properties
				//    URL:   Configuration Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/configuration_properties.asp
				standardProperties.Add("ARPCOMMENTS", "Property");

				//    Name:  ARPCONTACT
				//    Desc:
				//    URL:
				//    Class: Configuration Properties
				//    URL:   Configuration Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/configuration_properties.asp
				standardProperties.Add("ARPCONTACT", "Property");

				//    Name:  ARPHELPLINK
				//    Desc:
				//    URL:
				//    Class: Product Information Properties
				//    URL:   Product Information Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/product_information_properties.asp
				standardProperties.Add("ARPHELPLINK", "Property");

				//    Name:  ARPHELPTELEPHONE
				//    Desc:
				//    URL:
				//    Class: Product Information Properties
				//    URL:   Product Information Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/product_information_properties.asp
				standardProperties.Add("ARPHELPTELEPHONE", "Property");

				//    Name:  ARPINSTALLLOCATION
				//    Desc:
				//    URL:
				//    Class: Configuration Properties
				//    URL:   Configuration Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/configuration_properties.asp
				standardProperties.Add("ARPINSTALLLOCATION", "Property");

				//    Name:  ARPNOMODIFY
				//    Desc:
				//    URL:
				//    Class: Configuration Properties
				//    URL:   Configuration Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/configuration_properties.asp
				standardProperties.Add("ARPNOMODIFY", "Property");

				//    Name:  ARPNOREMOVE
				//    Desc:
				//    URL:
				//    Class: Configuration Properties
				//    URL:   Configuration Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/configuration_properties.asp
				standardProperties.Add("ARPNOREMOVE", "Property");

				//    Name:  ARPNOREPAIR
				//    Desc:
				//    URL:
				//    Class: Configuration Properties
				//    URL:   Configuration Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/configuration_properties.asp
				standardProperties.Add("ARPNOREPAIR", "Property");

				//    Name:  ARPPRODUCTIONICON
				//    Desc:
				//    URL:
				//    Class: Configuration Properties
				//    URL:   Configuration Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/configuration_properties.asp
				standardProperties.Add("ARPPRODUCTIONICON", "Property");

				//    Name:  ARPREADME
				//    Desc:
				//    URL:
				//    Class: Configuration Properties
				//    URL:   Configuration Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/configuration_properties.asp
				standardProperties.Add("ARPREADME", "Property");

				//    Name:  ARPSIZE
				//    Desc:
				//    URL:
				//    Class: Configuration Properties
				//    URL:   Configuration Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/configuration_properties.asp
				standardProperties.Add("ARPSIZE", "Property");

				//    Name:  ARPSYSTEMCOMPONENT
				//    Desc:
				//    URL:
				//    Class: Configuration Properties
				//    URL:   Configuration Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/configuration_properties.asp
				standardProperties.Add("ARPSYSTEMCOMPONENT", "Property");

				//    Name:  ARPULRINFOABOUT
				//    Desc:
				//    URL:
				//    Class: Configuration Properties
				//    URL:   Configuration Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/configuration_properties.asp
				standardProperties.Add("ARPULRINFOABOUT", "Property");

				//    Name:  ARPURLUPDATEINFO
				//    Desc:
				//    URL:
				//    Class: Configuration Properties
				//    URL:   Configuration Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/configuration_properties.asp
				standardProperties.Add("ARPURLUPDATEINFO", "Property");

				//    Name:  AVAILABLEFREEREG
				//    Desc:
				//    URL:
				//    Class: Configuration Properties
				//    URL:   Configuration Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/configuration_properties.asp
				standardProperties.Add("AVAILABLEFREEREG", "Property");

				//    Name:  BorderSize
				//    Desc:
				//    URL:
				//    Class: Hardware Properties [Windows Installer]
				//    URL:   Hardware Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/hardware_properties.asp
				standardProperties.Add("BorderSize", "Property");

				//    Name:  BorderTop
				//    Desc:
				//    URL:
				//    Class: Hardware Properties [Windows Installer]
				//    URL:   Hardware Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/hardware_properties.asp
				standardProperties.Add("BorderTop", "Property");

				//    Name:  CaptionHeight
				//    Desc:
				//    URL:
				//    Class: Hardware Properties [Windows Installer]
				//    URL:   Hardware Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/hardware_properties.asp
				standardProperties.Add("CaptionHeight", "Property");

				//    Name:  CCP_DRIVE
				//    Desc:
				//    URL:
				//    Class: Configuration Properties
				//    URL:   Configuration Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/configuration_properties.asp
				standardProperties.Add("CCP_DRIVE", "Property");

				//    Name:  ColorBits
				//    Desc:
				//    URL:
				//    Class: Hardware Properties [Windows Installer]
				//    URL:   Hardware Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/hardware_properties.asp
				standardProperties.Add("ColorBits", "Property");

				//    Name:  COMPADDLOCAL
				//    Desc:
				//    URL:
				//    Class: Feature Installation Options Properties
				//    URL:   Feature Installation Options Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/feature_installation_options_properties.asp
				standardProperties.Add("COMPADDLOCAL", "Property");

				//    Name:  COMPADDSOURCE
				//    Desc:
				//    URL:
				//    Class: Feature Installation Options Properties
				//    URL:   Feature Installation Options Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/feature_installation_options_properties.asp
				standardProperties.Add("COMPADDSOURCE", "Property");

				//    Name:  COMPANYNAME
				//    Desc:
				//    URL:
				//    Class: User Information Properties [Windows Installer]
				//    URL:   User Information Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/user_information_properties.asp
				standardProperties.Add("COMPANYNAME", "Property");

				//    Name:  ComputerName
				//    Desc:
				//    URL:
				//    Class: Operating System Properties
				//    URL:   Operating System Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/operating_system_properties.asp
				standardProperties.Add("ComputerName", "Property");

				//    Name:  CostingComplete
				//    Desc:
				//    URL:
				//    Class: Installation Status Properties
				//    URL:   Installation Status Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/installation_status_properties.asp
				standardProperties.Add("CostingComplete", "Property");

				//    Name:  Date
				//    Desc:
				//    URL:
				//    Class: Date, Time Properties
				//    URL:   Date, Time Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/date_time_properties.asp
				standardProperties.Add("Date", "Property");

				//    Name:  DefaultUIFont
				//    Desc:
				//    URL:
				//    Class: Configuration Properties
				//    URL:   Configuration Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/configuration_properties.asp
				standardProperties.Add("DefaultUIFont", "Property");

				//    Name:  DISABLEADVTSHORTCUTS
				//    Desc:
				//    URL:
				//    Class: Configuration Properties
				//    URL:   Configuration Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/configuration_properties.asp
				standardProperties.Add("DISABLEADVTSHORTCUTS", "Property");

				//    Name:  DISABLEMEDIA
				//    Desc:
				//    URL:
				//    Class: Configuration Properties
				//    URL:   Configuration Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/configuration_properties.asp
				standardProperties.Add("DISABLEMEDIA", "Property");

				//    Name:  DISABLEROLLBACK
				//    Desc:
				//    URL:
				//    Class: Configuration Properties
				//    URL:   Configuration Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/configuration_properties.asp
				standardProperties.Add("DISABLEROLLBACK", "Property");

				//    Name:  DiskPrompt
				//    Desc:
				//    URL:
				//    Class: Product Information Properties
				//    URL:   Product Information Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/product_information_properties.asp
				standardProperties.Add("DiskPrompt", "Property");

				//    Name:  DontRemoveTempFolderWhenFinished
				//    Desc:
				//    URL:
				//    Class: Properties Table (PATCHWIZ.DLL)
				//    URL:   Properties Table (Patchwiz.dll) [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/properties_table_patchwiz_dll_.asp
				standardProperties.Add("DontRemoveTempFolderWhenFinished", "Property");

				//    Name:  EnableUserControl
				//    Desc:
				//    URL:
				//    Class: System Folder Properties
				//    URL:   System Folder Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/system_folder_properties.asp
				standardProperties.Add("EnableUserControl", "Property");

				//    Name:  EXECUTEACTION
				//    Desc:
				//    URL:
				//    Class: Configuration Properties
				//           Restricted Public Properties
				//    URL:   Configuration Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/configuration_properties.asp
				//           Restricted Public Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/restricted_public_properties.asp
				standardProperties.Add("EXECUTEACTION", "Property");

				//    Name:  EXECUTEMODE
				//    Desc:
				//    URL:
				//    Class: Configuration Properties
				//           Restricted Public Properties
				//    URL:   Configuration Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/configuration_properties.asp
				//           Restricted Public Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/restricted_public_properties.asp
				standardProperties.Add("EXECUTEMODE", "Property");

				//    Name:  FASTOEM
				//    Desc:
				//    URL:
				//    Class: Configuration Properties
				//    URL:   Configuration Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/configuration_properties.asp
				standardProperties.Add("FASTOEM", "Property");

				//    Name:  FILEADDDEFAULT
				//    Desc:
				//    URL:
				//    Class: Feature Installation Options Properties
				//           Restricted Public Properties
				//    URL:   Feature Installation Options Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/feature_installation_options_properties.asp
				//           Restricted Public Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/restricted_public_properties.asp
				standardProperties.Add("FILEADDDEFAULT", "Property");

				//    Name:  FILEADDLOCAL
				//    Desc:
				//    URL:
				//    Class: Feature Installation Options Properties
				//           Restricted Public Properties
				//    URL:   Feature Installation Options Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/feature_installation_options_properties.asp
				//           Restricted Public Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/restricted_public_properties.asp
				standardProperties.Add("FILEADDLOCAL", "Property");

				//    Name:  FILEADDSOURCE
				//    Desc:
				//    URL:
				//    Class: Feature Installation Options Properties
				//           Restricted Public Properties
				//    URL:   Feature Installation Options Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/feature_installation_options_properties.asp
				//           Restricted Public Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/restricted_public_properties.asp
				standardProperties.Add("FILEADDSOURCE", "Property");

				//    Name:  IncludeWholeFilesOnly
				//    Desc:
				//    URL:
				//    Class: Properties Table (PATCHWIZ.DLL)
				//    URL:   Properties Table (Patchwiz.dll) [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/properties_table_patchwiz_dll_.asp
				standardProperties.Add("IncludeWholeFilesOnly", "Property");

				//    Name:  Installed
				//    Desc:
				//    URL:
				//    Class: Installation Status Properties
				//    URL:   Installation Status Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/installation_status_properties.asp
				standardProperties.Add("Installed", "Property");

				//    Name:  INSTALLLEVEL
				//    Desc:
				//    URL:
				//    Class: Configuration Properties
				//           Restricted Public Properties
				//    URL:   Configuration Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/configuration_properties.asp
				//           Restricted Public Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/restricted_public_properties.asp
				standardProperties.Add("INSTALLLEVEL", "Property");

				//    Name:  Intel
				//    Desc:
				//    URL:
				//    Class: Hardware Properties [Windows Installer]
				//    URL:   Hardware Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/hardware_properties.asp
				standardProperties.Add("Intel", "Property");

				//    Name:  Intel64
				//    Desc:
				//    URL:
				//    Class: Hardware Properties [Windows Installer]
				//    URL:   Hardware Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/hardware_properties.asp
				standardProperties.Add("Intel64", "Property");

				//    Name:  IsAdminPackage
				//    Desc:
				//    URL:
				//    Class: Product Information Properties
				//    URL:   Product Information Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/product_information_properties.asp
				standardProperties.Add("IsAdminPackage", "Property");

				//    Name:  LeftUnit
				//    Desc:
				//    URL:
				//    Class: Product Information Properties
				//    URL:   Product Information Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/product_information_properties.asp
				standardProperties.Add("LeftUnit", "Property");

				//    Name:  LIMITUI
				//    Desc:
				//    URL:
				//    Class: Configuration Properties
				//           Restricted Public Properties
				//    URL:   Configuration Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/configuration_properties.asp
				//           Restricted Public Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/restricted_public_properties.asp
				standardProperties.Add("LIMITUI", "Property");

				//    Name:  ListOfPatchGUIDsToReplace
				//    Desc:
				//    URL:
				//    Class: Properties Table (PATCHWIZ.DLL)
				//    URL:   Properties Table (Patchwiz.dll) [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/properties_table_patchwiz_dll_.asp
				standardProperties.Add("ListOfPatchGUIDsToReplace", "Property");

				//    Name:  ListOfTargetProductCode
				//    Desc:
				//    URL:
				//    Class: Properties Table (PATCHWIZ.DLL)
				//    URL:   Properties Table (Patchwiz.dll) [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/properties_table_patchwiz_dll_.asp
				standardProperties.Add("ListOfTargetProductCode", "Property");

				//    Name:  LOGACTION
				//    Desc:
				//    URL:
				//    Class: Configuration Properties
				//    URL:   Configuration Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/configuration_properties.asp
				standardProperties.Add("LOGACTION", "Property");

				//    Name:  LogonUser
				//    Desc:
				//    URL:
				//    Class: User Information Properties [Windows Installer]
				//    URL:   User Information Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/user_information_properties.asp
				standardProperties.Add("LogonUser", "Property");

				//    Name:  Manufacturer
				//    Desc:
				//    URL:
				//    Class: Product Information Properties
				//    URL:   Product Information Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/product_information_properties.asp
				standardProperties.Add("Manufacturer", "Property");

				//    Name:  MEDIAPACKAGEPATH
				//    Desc:
				//    URL:
				//    Class: Configuration Properties
				//    URL:   Configuration Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/configuration_properties.asp
				standardProperties.Add("MEDIAPACKAGEPATH", "Property");

				//    Name:  MediaSourceDir
				//    Desc:
				//    URL:
				//    Class: Product Information Properties
				//    URL:   Product Information Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/product_information_properties.asp
				standardProperties.Add("MediaSourceDir", "Property");

				//    Name:  MinimumRequiredMsiVersion
				//    Desc:
				//    URL:
				//    Class: Properties Table (PATCHWIZ.DLL)
				//    URL:   Properties Table (Patchwiz.dll) [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/properties_table_patchwiz_dll_.asp
				standardProperties.Add("MinimumRequiredMsiVersion", "Property");

				//    Name:  MsiAMD64
				//    Desc:
				//    URL:
				//    Class: Hardware Properties [Windows Installer]
				//    URL:   Hardware Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/hardware_properties.asp
				standardProperties.Add("MsiAMD64", "Property");

				//    Name:  MSIAPRSETTINGSIDENTIFIER
				//    Desc:
				//    URL:
				//    Class: Configuration Properties
				//    URL:   Configuration Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/configuration_properties.asp
				standardProperties.Add("MSIAPRSETTINGSIDENTIFIER", "Property");

				//    Name:  MSICHECKCRCS
				//    Desc:
				//    URL:
				//    Class: Installation Status Properties
				//    URL:   Installation Status Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/installation_status_properties.asp
				standardProperties.Add("MSICHECKCRCS", "Property");

				//    Name:  MSIDISABLERMRESTART
				//    Desc:
				//    URL:
				//    Class: Feature Installation Options Properties
				//    URL:   Feature Installation Options Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/feature_installation_options_properties.asp
				standardProperties.Add("MSIDISABLERMRESTART", "Property");

				//    Name:  MSIENFORCEUPGRADECOMPONENTRULES
				//    Desc:
				//    URL:
				//    Class: Configuration Properties
				//           Restricted Public Properties
				//    URL:   Configuration Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/configuration_properties.asp
				//           Restricted Public Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/restricted_public_properties.asp
				standardProperties.Add("MSIENFORCEUPGRADECOMPONENTRULES", "Property");

				//    Name:  MsiFileToUseToCreatePatchTables
				//    Desc:
				//    URL:
				//    Class: Properties Table (PATCHWIZ.DLL)
				//    URL:   Properties Table (Patchwiz.dll) [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/properties_table_patchwiz_dll_.asp
				standardProperties.Add("MsiFileToUseToCreatePatchTables", "Property");

				//    Name:  MsiHiddenProperties
				//    Desc:
				//    URL:
				//    Class: User Information Properties [Windows Installer]
				//    URL:   User Information Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/user_information_properties.asp
				standardProperties.Add("MsiHiddenProperties", "Property");

				//    Name:  MSIINSTANCEGUID
				//    Desc:
				//    URL:
				//    Class: Product Information Properties
				//           Restricted Public Properties
				//    URL:   Product Information Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/product_information_properties.asp
				//           Restricted Public Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/restricted_public_properties.asp
				standardProperties.Add("MSIINSTANCEGUID", "Property");

				//    Name:  MsiLogFileLocation
				//    Desc:
				//    URL:
				//    Class: Configuration Properties
				//    URL:   Configuration Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/configuration_properties.asp
				standardProperties.Add("MsiLogFileLocation", "Property");

				//    Name:  MsiLogging
				//    Desc:
				//    URL:
				//    Class: Configuration Properties
				//    URL:   Configuration Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/configuration_properties.asp
				standardProperties.Add("MsiLogging", "Property");

				//    Name:  MsiNetAssemblySupport
				//    Desc:
				//    URL:
				//    Class: Operating System Properties
				//    URL:   Operating System Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/operating_system_properties.asp
				standardProperties.Add("MsiNetAssemblySupport", "Property");

				//    Name:  MSINEWINSTANCE
				//    Desc:
				//    URL:
				//    Class: Product Information Properties
				//    URL:   Product Information Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/product_information_properties.asp
				standardProperties.Add("MSINEWINSTANCE", "Property");

				//    Name:  MSINODISABLEMEDIA
				//    Desc:
				//    URL:
				//    Class: Configuration Properties
				//           Restricted Public Properties
				//    URL:   Configuration Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/configuration_properties.asp
				//           Restricted Public Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/restricted_public_properties.asp
				standardProperties.Add("MSINODISABLEMEDIA", "Property");

				//    Name:  MsiNTProductType
				//    Desc:
				//    URL:
				//    Class: Operating System Properties
				//    URL:   Operating System Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/operating_system_properties.asp
				standardProperties.Add("MsiNTProductType", "Property");

				//    Name:  MsiNTSuiteBackOffice
				//    Desc:
				//    URL:
				//    Class: Operating System Properties
				//    URL:   Operating System Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/operating_system_properties.asp
				standardProperties.Add("MsiNTSuiteBackOffice", "Property");

				//    Name:  MsiNTSuiteDataCenter
				//    Desc:
				//    URL:
				//    Class: Operating System Properties
				//    URL:   Operating System Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/operating_system_properties.asp
				standardProperties.Add("MsiNTSuiteDataCenter", "Property");

				//    Name:  MsiNTSuiteEnterprise
				//    Desc:
				//    URL:
				//    Class: Operating System Properties
				//    URL:   Operating System Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/operating_system_properties.asp
				standardProperties.Add("MsiNTSuiteEnterprise", "Property");

				//    Name:  MsiNTSuiteSmallBusiness
				//    Desc:
				//    URL:
				//    Class: Operating System Properties
				//    URL:   Operating System Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/operating_system_properties.asp
				standardProperties.Add("MsiNTSuiteSmallBusiness", "Property");

				//    Name:  MsiNTSuiteSmallBusinessRestricted
				//    Desc:
				//    URL:
				//    Class: Operating System Properties
				//    URL:   Operating System Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/operating_system_properties.asp
				standardProperties.Add("MsiNTSuiteSmallBusinessRestricted", "Property");

				//    Name:  MsiNTSuiteWebServer
				//    Desc:
				//    URL:
				//    Class: Operating System Properties
				//    URL:   Operating System Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/operating_system_properties.asp
				standardProperties.Add("MsiNTSuiteWebServer", "Property");

				//    Name:  MsiNTSuitePersonal
				//    Desc:
				//    URL:
				//    Class: Operating System Properties
				//    URL:   Operating System Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/operating_system_properties.asp
				standardProperties.Add("MsiNTSuitePersonal", "Property");

				//    Name:  MsiPatchRemovalList
				//    Desc:
				//    URL:
				//    Class: Feature Installation Options Properties
				//    URL:   Feature Installation Options Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/feature_installation_options_properties.asp
				standardProperties.Add("MsiPatchRemovalList", "Property");

				//    Name:  MSIPATCHREMOVE
				//    Desc:
				//    URL:
				//    Class: Feature Installation Options Properties
				//           Restricted Public Properties
				//    URL:   Feature Installation Options Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/feature_installation_options_properties.asp
				//           Restricted Public Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/restricted_public_properties.asp
				standardProperties.Add("MSIPATCHREMOVE", "Property");

				//    Name:  MSIRESTARTMANAGERCONTROL
				//    Desc:
				//    URL:
				//    Class: Feature Installation Options Properties
				//    URL:   Feature Installation Options Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/feature_installation_options_properties.asp
				standardProperties.Add("MSIRESTARTMANAGERCONTROL", "Property");

				//    Name:  MsiRestartManagerSessionKey
				//    Desc:
				//    URL:
				//    Class: Installation Status Properties
				//    URL:   Installation Status Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/installation_status_properties.asp
				standardProperties.Add("MsiRestartManagerSessionKey", "Property");

				//    Name:  MSIRMSHUTDOWN
				//    Desc:
				//    URL:
				//    Class: Feature Installation Options Properties
				//    URL:   Feature Installation Options Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/feature_installation_options_properties.asp
				standardProperties.Add("MSIRMSHUTDOWN", "Property");

				//    Name:  MsiRunningElevated
				//    Desc:
				//    URL:
				//    Class: Installation Status Properties
				//    URL:   Installation Status Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/installation_status_properties.asp
				standardProperties.Add("MsiRunningElevated", "Property");

				//    Name:  MsiUIHideCancel
				//    Desc:
				//    URL:
				//    Class: Installation Status Properties
				//    URL:   Installation Status Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/installation_status_properties.asp
				standardProperties.Add("MsiUIHideCancel", "Property");

				//    Name:  MsiUIProgressOnly
				//    Desc:
				//    URL:
				//    Class: Installation Status Properties
				//    URL:   Installation Status Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/installation_status_properties.asp
				standardProperties.Add("MsiUIProgressOnly", "Property");

				//    Name:  MsiSystemRebootPending
				//    Desc:
				//    URL:
				//    Class: Installation Status Properties
				//    URL:   Installation Status Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/installation_status_properties.asp
				standardProperties.Add("MsiSystemRebootPending", "Property");

				//    Name:  MsiUISourceResOnly
				//    Desc:
				//    URL:
				//    Class: Installation Status Properties
				//    URL:   Installation Status Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/installation_status_properties.asp
				standardProperties.Add("MsiUISourceResOnly", "Property");

				//    Name:  MsiWin32AssemblySupport
				//    Desc:
				//    URL:
				//    Class: Operating System Properties
				//    URL:   Operating System Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/operating_system_properties.asp
				standardProperties.Add("MsiWin32AssemblySupport", "Property");

				//    Name:  NOCOMPANYNAME
				//    Desc:
				//    URL:
				//    Class: Installation Status Properties
				//           Restricted Public Properties
				//    URL:   Installation Status Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/installation_status_properties.asp
				//           Restricted Public Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/restricted_public_properties.asp
				standardProperties.Add("NOCOMPANYNAME", "Property");

				//    Name:  NOUSERNAME
				//    Desc:
				//    URL:
				//    Class: Installation Status Properties
				//           Restricted Public Properties
				//    URL:   Installation Status Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/installation_status_properties.asp
				//           Restricted Public Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/restricted_public_properties.asp
				standardProperties.Add("NOUSERNAME", "Property");

				//    Name:  OLEAdvtSupport
				//    Desc:
				//    URL:
				//    Class: Operating System Properties
				//    URL:   Operating System Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/operating_system_properties.asp
				standardProperties.Add("OLEAdvtSupport", "Property");

				//    Name:  OptimizePatchSizeForLargeFiles
				//    Desc:
				//    URL:
				//    Class: Properties Table (PATCHWIZ.DLL)
				//    URL:   Properties Table (Patchwiz.dll) [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/properties_table_patchwiz_dll_.asp
				standardProperties.Add("OptimizePatchSizeForLargeFiles", "Property");

				//    Name:  OriginalDatabase
				//    Desc:  The installer sets the OriginalDatabase property to the launched-from database, the database on the source, or the cached database.
				//    URL:   OriginalDatabase Property [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/originaldatabase_property.asp
				//    Class: Component Location Properties
				//    URL:   Component Location Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/component_location_properties.asp
				standardProperties.Add("OriginalDatabase", "Property");

				//    Name:  OutOfDiskSpace
				//    Desc:
				//    URL:
				//    Class: Installation Status Properties
				//    URL:   Installation Status Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/installation_status_properties.asp
				standardProperties.Add("OutOfDiskSpace", "Property");

				//    Name:  OutOfNoRbDiskSpace
				//    Desc:
				//    URL:
				//    Class: Installation Status Properties
				//    URL:   Installation Status Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/installation_status_properties.asp
				standardProperties.Add("OutOfNoRbDiskSpace", "Property");

				//    Name:  ParentOriginalDatabase
				//    Desc:  The installer sets this property for installations run by a nested installation action.
				//    URL:   ParentOriginalDatabase Property [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/originaldatabase_property.asp
				//    Class: Component Location Properties
				//    URL:   Component Location Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/component_location_properties.asp
				standardProperties.Add("ParentOriginalDatabase", "Property");

				//    Name:  ParentProductCode
				//    Desc:
				//    URL:
				//    Class: Product Information Properties
				//    URL:   Product Information Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/product_information_properties.asp
				standardProperties.Add("ParentProductCode", "Property");

				//    Name:  PATCH
				//    Desc:
				//    URL:
				//    Class: Feature Installation Options Properties
				//           Restricted Public Properties
				//    URL:   Feature Installation Options Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/feature_installation_options_properties.asp
				//           Restricted Public Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/restricted_public_properties.asp
				standardProperties.Add("PATCH", "Property");

				//    Name:  PATCH_CACHE_DIR
				//    Desc:
				//    URL:
				//    Class: Properties Table (PATCHWIZ.DLL)
				//    URL:   Properties Table (Patchwiz.dll) [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/properties_table_patchwiz_dll_.asp
				standardProperties.Add("PATCH_CACHE_DIR", "Property");

				//    Name:  PATCH_CACHE_ENABLED
				//    Desc:
				//    URL:
				//    Class: Properties Table (PATCHWIZ.DLL)
				//    URL:   Properties Table (Patchwiz.dll) [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/properties_table_patchwiz_dll_.asp
				standardProperties.Add("PATCH_CACHE_ENABLED", "Property");

				//    Name:  PatchGUID
				//    Desc:
				//    URL:
				//    Class: Properties Table (PATCHWIZ.DLL)
				//    URL:   Properties Table (Patchwiz.dll) [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/properties_table_patchwiz_dll_.asp
				standardProperties.Add("PatchGUID", "Property");

				//    Name:  PATCHNEWPACKAGECODE
				//    Desc:
				//    URL:
				//    Class: Summary Information Update Properties
				//    URL:   Summary Information Update Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/summary_information_update_properties.asp
				standardProperties.Add("PATCHNEWPACKAGECODE", "Property");

				//    Name:  PATCHNEWSUMMARYCOMMENTS
				//    Desc:
				//    URL:
				//    Class: Summary Information Update Properties
				//    URL:   Summary Information Update Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/summary_information_update_properties.asp
				standardProperties.Add("PATCHNEWSUMMARYCOMMENTS", "Property");

				//    Name:  PATCHNEWSUMMARYSUBJECT
				//    Desc:
				//    URL:
				//    Class: Summary Information Update Properties
				//    URL:   Summary Information Update Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/summary_information_update_properties.asp
				standardProperties.Add("PATCHNEWSUMMARYSUBJECT", "Property");

				//    Name:  PatchOutputPath
				//    Desc:
				//    URL:
				//    Class: Properties Table (PATCHWIZ.DLL)
				//    URL:   Properties Table (Patchwiz.dll) [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/properties_table_patchwiz_dll_.asp
				standardProperties.Add("PatchOutputPath", "Property");

				//    Name:  PatchSourceList
				//    Desc:
				//    URL:
				//    Class: Properties Table (PATCHWIZ.DLL)
				//    URL:   Properties Table (Patchwiz.dll) [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/properties_table_patchwiz_dll_.asp
				standardProperties.Add("PatchSourceList", "Property");

				//    Name:  PhysicalMemory
				//    Desc:
				//    URL:
				//    Class: Hardware Properties [Windows Installer]
				//    URL:   Hardware Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/hardware_properties.asp
				standardProperties.Add("PhysicalMemory", "Property");

				//    Name:  PIDKEY
				//    Desc:
				//    URL:
				//    Class: User Information Properties [Windows Installer]
				//    URL:   User Information Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/user_information_properties.asp
				standardProperties.Add("PIDKEY", "Property");

				//    Name:  PIDTemplate
				//    Desc:
				//    URL:
				//    Class: Product Information Properties
				//    URL:   Product Information Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/product_information_properties.asp
				standardProperties.Add("PIDTemplate", "Property");

				//    Name:  Preselected
				//    Desc:
				//    URL:
				//    Class: Installation Status Properties
				//    URL:   Installation Status Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/installation_status_properties.asp
				standardProperties.Add("Preselected", "Property");

				//    Name:  PRIMARYFOLDER
				//    Desc:
				//    URL:
				//    Class: Configuration Properties
				//           Restricted Public Properties
				//    URL:   Configuration Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/configuration_properties.asp
				//           Restricted Public Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/restricted_public_properties.asp
				standardProperties.Add("PRIMARYFOLDER", "Property");

				//    Name:  PrimaryVolumePath
				//    Desc:
				//    URL:
				//    Class: Installation Status Properties
				//    URL:   Installation Status Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/installation_status_properties.asp
				standardProperties.Add("PrimaryVolumePath", "Property");

				//    Name:  PrimaryVolumeSpaceAvailable
				//    Desc:
				//    URL:
				//    Class: Installation Status Properties
				//    URL:   Installation Status Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/installation_status_properties.asp
				standardProperties.Add("PrimaryVolumeSpaceAvailable", "Property");

				//    Name:  PrimaryVolumeSpaceRemaining
				//    Desc:
				//    URL:
				//    Class: Installation Status Properties
				//    URL:   Installation Status Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/installation_status_properties.asp
				standardProperties.Add("PrimaryVolumeSpaceRemaining", "Property");

				//    Name:  PrimaryVolumeSpaceRequired
				//    Desc:
				//    URL:
				//    Class: Installation Status Properties
				//    URL:   Installation Status Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/installation_status_properties.asp
				standardProperties.Add("PrimaryVolumeSpaceRequired", "Property");

				//    Name:  Privileged
				//    Desc:
				//    URL:
				//    Class: Configuration Properties
				//    URL:   Configuration Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/configuration_properties.asp
				standardProperties.Add("Privileged", "Property");

				//    Name:  ProductCode
				//    Desc:
				//    URL:
				//    Class: Product Information Properties
				//    URL:   Product Information Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/product_information_properties.asp
				standardProperties.Add("ProductCode", "Property");

				//    Name:  ProductID
				//    Desc:
				//    URL:
				//    Class: User Information Properties [Windows Installer]
				//    URL:   User Information Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/user_information_properties.asp
				standardProperties.Add("ProductID", "Property");

				//    Name:  ProductLanguage
				//    Desc:
				//    URL:
				//    Class: Installation Status Properties
				//    URL:   Installation Status Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/installation_status_properties.asp
				standardProperties.Add("ProductLanguage", "Property");

				//    Name:  ProductName
				//    Desc:
				//    URL:
				//    Class: Product Information Properties
				//    URL:   Product Information Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/product_information_properties.asp
				standardProperties.Add("ProductName", "Property");

				//    Name:  ProductState
				//    Desc:
				//    URL:
				//    Class: Product Information Properties
				//    URL:   Product Information Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/product_information_properties.asp
				standardProperties.Add("ProductState", "Property");

				//    Name:  ProductVersion
				//    Desc:
				//    URL:
				//    Class: Product Information Properties
				//    URL:   Product Information Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/product_information_properties.asp
				standardProperties.Add("ProductVersion", "Property");

				//    Name:  PROMPTROLLBACKCOST
				//    Desc:
				//    URL:   http://msdn.microsoft.com/library/en-us/msi/setup/promptrollbackcost_property.asp
				//    Class: Configuration Properties
				//           Restricted Public Properties
				//    URL:   Configuration Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/configuration_properties.asp
				//           Restricted Public Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/restricted_public_properties.asp
				standardProperties.Add("PROMPTROLLBACKCOST", "Property");

				//    Name:  REBOOT
				//    Desc:
				//    URL:
				//    Class: Configuration Properties
				//           Restricted Public Properties
				//    URL:   Configuration Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/configuration_properties.asp
				//           Restricted Public Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/restricted_public_properties.asp
				standardProperties.Add("REBOOT", "Property");

				//    Name:  REBOOTPROMPT
				//    Desc:
				//    URL:
				//    Class: Configuration Properties
				//    URL:   Configuration Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/configuration_properties.asp
				standardProperties.Add("REBOOTPROMPT", "Property");

				//    Name:  RedirectedDllSupport
				//    Desc:
				//    URL:
				//    Class: Operating System Properties
				//    URL:   Operating System Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/operating_system_properties.asp
				standardProperties.Add("RedirectedDllSupport", "Property");

				//    Name:  REINSTALL
				//    Desc:
				//    URL:
				//    Class: Configuration Properties
				//           Restricted Public Properties
				//    URL:   Configuration Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/configuration_properties.asp
				//           Restricted Public Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/restricted_public_properties.asp
				standardProperties.Add("REINSTALL", "Property");

				//    Name:  REINSTALLMODE
				//    Desc:
				//    URL:
				//    Class: Feature Installation Options Properties
				//           Restricted Public Properties
				//    URL:   Feature Installation Options Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/feature_installation_options_properties.asp
				//           Restricted Public Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/restricted_public_properties.asp
				standardProperties.Add("REINSTALLMODE", "Property");

				//    Name:  RemoteAdminTS
				//    Desc:
				//    URL:
				//    Class: Operating System Properties
				//    URL:   Operating System Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/operating_system_properties.asp
				standardProperties.Add("RemoveAdminTS", "Property");

				//    Name:  REMOVE
				//    Desc:
				//    URL:
				//    Class: Feature Installation Options Properties
				//    URL:   Feature Installation Options Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/feature_installation_options_properties.asp
				standardProperties.Add("REMOVE", "Property");

				//    Name:  ReplacedInUseFiles
				//    Desc:
				//    URL:
				//    Class: Installation Status Properties
				//    URL:   Installation Status Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/installation_status_properties.asp
				standardProperties.Add("ReplacedInUseFiles", "Property");

				//    Name:  RestrictedUserControl
				//    Desc:
				//    URL:
				//    Class: System Folder Properties
				//    URL:   System Folder Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/system_folder_properties.asp
				standardProperties.Add("RestrictedUserControl", "Property");

				//    Name:  RESUME
				//    Desc:
				//    URL:
				//    Class: Installation Status Properties
				//           Restricted Public Properties
				//    URL:   Installation Status Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/installation_status_properties.asp
				//           Restricted Public Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/restricted_public_properties.asp
				standardProperties.Add("RESUME", "Property");

				//    Name:  RollbackDisabled
				//    Desc:
				//    URL:
				//    Class: Installation Status Properties
				//    URL:   Installation Status Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/installation_status_properties.asp
				standardProperties.Add("RollbackDisabled", "Property");

				//    Name:  ROOTDRIVE
				//    Desc:
				//    URL:
				//    Class: Configuration Properties
				//    URL:   Configuration Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/configuration_properties.asp
				standardProperties.Add("ROOTDRIVE", "Property");

				//    Name:  ScreenX
				//    Desc:
				//    URL:
				//    Class: Hardware Properties [Windows Installer]
				//    URL:   Hardware Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/hardware_properties.asp
				standardProperties.Add("ScreenX", "Property");

				//    Name:  ScreenY
				//    Desc:
				//    URL:
				//    Class: Hardware Properties [Windows Installer]
				//    URL:   Hardware Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/hardware_properties.asp
				standardProperties.Add("ScreenY", "Property");

				//    Name:  SecureCustomProperties
				//    Desc:
				//    URL:
				//    Class: System Folder Properties
				//    URL:   System Folder Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/system_folder_properties.asp
				standardProperties.Add("SecureCustomProperties", "Property");

				//    Name:  ServicePackLevel
				//    Desc:
				//    URL:
				//    Class: Operating System Properties
				//    URL:   Operating System Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/operating_system_properties.asp
				standardProperties.Add("ServicePackLevel", "Property");

				//    Name:  ServicePackLevelMinor
				//    Desc:
				//    URL:
				//    Class: Operating System Properties
				//    URL:   Operating System Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/operating_system_properties.asp
				standardProperties.Add("ServicePackLevelMinor", "Property");

				//    Name:  SEQUENCE
				//    Desc:
				//    URL:
				//    Class: Configuration Properties
				//           Restricted Public Properties
				//    URL:   Configuration Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/configuration_properties.asp
				//           Restricted Public Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/restricted_public_properties.asp
				standardProperties.Add("SEQUENCE", "Property");

				//    Name:  SharedWindows
				//    Desc:
				//    URL:
				//    Class: Operating System Properties
				//    URL:   Operating System Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/operating_system_properties.asp
				standardProperties.Add("SharedWindows", "Property");

				//    Name:  ShellAdvtSupport
				//    Desc:
				//    URL:
				//    Class: Operating System Properties
				//    URL:   Operating System Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/operating_system_properties.asp
				standardProperties.Add("ShellAdvtSupport", "Property");

				//    Name:  SHORTFILENAMES
				//    Desc:
				//    URL:
				//    Class: Configuration Properties
				//           Restricted Public Properties
				//    URL:   Configuration Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/configuration_properties.asp
				//           Restricted Public Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/restricted_public_properties.asp
				standardProperties.Add("SHORTFILENAMES", "Property");

				//    Name:  SourceDir
				//    Desc:  The SourceDir property is the root directory that contains the source cabinet file or the source file tree of the installation package. This value is used for directory resolution.
				//    URL:   SourceDir Property [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/sourcedir_property.asp
				//    Class: Component Location Properties
				//    URL:   Component Location Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/component_location_properties.asp
				standardProperties.Add("SourceDir", "Property");

				//    Name:  SOURCELIST
				//    Desc:  The SOURCELIST property is a semicolon-delimited list of network or URL source paths to the application's installation package.
				//    URL:   SOURCELIST Property [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/sourcelist_property.asp
				//    Class:
				//    URL:
				standardProperties.Add("SOURCELIST", "Property");

				//    Name:  SystemLanguageID
				//    Desc:
				//    URL:
				//    Class: Operating System Properties
				//    URL:   Operating System Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/operating_system_properties.asp
				standardProperties.Add("SystemLanguageID", "Property");

				//    Name:  TARGETDIR
				//    Desc:  The TARGETDIR property specifies the root destination directory for the installation.
				//    URL:   TARGETDIR Property [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/targetdir_property.asp
				//    Class: Component Location Properties
				//    URL:   Component Location Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/component_location_properties.asp
				standardProperties.Add("TARGETDIR", "Property");

				//    Name:  TerminalServer
				//    Desc:
				//    URL:
				//    Class: Operating System Properties
				//    URL:   Operating System Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/operating_system_properties.asp
				standardProperties.Add("TerminalServer", "Property");

				//    Name:  TextHeight
				//    Desc:
				//    URL:
				//    Class: Hardware Properties [Windows Installer]
				//    URL:   Hardware Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/hardware_properties.asp
				standardProperties.Add("TextHeight", "Property");

				//    Name:  Time
				//    Desc:
				//    URL:
				//    Class: Date, Time Properties
				//    URL:   Date, Time Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/date_time_properties.asp
				standardProperties.Add("Time", "Property");

				//    Name:  TRANSFORMS
				//    Desc:
				//    URL:
				//    Class: Configuration Properties
				//           Restricted Public Properties
				//    URL:   Configuration Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/configuration_properties.asp
				//           Restricted Public Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/restricted_public_properties.asp
				standardProperties.Add("TRANSFORMS", "Property");

				//    Name:  TRANSFORMSATSOURCE
				//    Desc:
				//    URL:
				//    Class: Configuration Properties
				//           Restricted Public Properties
				//    URL:   Configuration Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/configuration_properties.asp
				//           Restricted Public Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/restricted_public_properties.asp
				standardProperties.Add("TRANSFORMSATSOURCE", "Property");

				//    Name:  TRANSFORMSSECURE
				//    Desc:
				//    URL:
				//    Class: Configuration Properties
				//    URL:   Configuration Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/configuration_properties.asp
				standardProperties.Add("TRANSFORMSSECURE", "Property");

				//    Name:  TTCSupport
				//    Desc:
				//    URL:
				//    Class: Operating System Properties
				//    URL:   Operating System Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/operating_system_properties.asp
				standardProperties.Add("TTCSupport", "Property");

				//    Name:  UILevel
				//    Desc:
				//    URL:
				//    Class: Installation Status Properties
				//    URL:   Installation Status Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/installation_status_properties.asp
				standardProperties.Add("UILevel", "Property");

				//    Name:  UpdateStarted
				//    Desc:
				//    URL:
				//    Class: Installation Status Properties
				//    URL:   Installation Status Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/installation_status_properties.asp
				standardProperties.Add("UpdateStarted", "Property");

				//    Name:  UpgradeCode
				//    Desc:
				//    URL:
				//    Class: Product Information Properties
				//    URL:   Product Information Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/product_information_properties.asp
				standardProperties.Add("UpgradeCode", "Property");

				//    Name:  UPGRADINGPRODUCTCODE
				//    Desc:
				//    URL:
				//    Class: Installation Status Properties
				//    URL:   Installation Status Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/installation_status_properties.asp
				standardProperties.Add("UPGRADINGPRODUCTCODE", "Property");

				//    Name:  UserLanguageID
				//    Desc:
				//    URL:
				//    Class: User Information Properties [Windows Installer]
				//    URL:   User Information Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/user_information_properties.asp
				standardProperties.Add("UserLanguageID", "Property");

				//    Name:  USERNAME
				//    Desc:
				//    URL:
				//    Class: User Information Properties [Windows Installer]
				//    URL:   User Information Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/user_information_properties.asp
				standardProperties.Add("USERNAME", "Property");

				//    Name:  UserSID
				//    Desc:
				//    URL:
				//    Class: User Information Properties [Windows Installer]
				//    URL:   User Information Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/user_information_properties.asp
				standardProperties.Add("UserSID", "Property");

				//    Name:  Version9X
				//    Desc:
				//    URL:
				//    Class: Operating System Properties
				//    URL:   Operating System Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/operating_system_properties.asp
				standardProperties.Add("Version9X", "Property");

				//    Name:  VersionDatabase
				//    Desc:
				//    URL:
				//    Class: Operating System Properties
				//    URL:   Operating System Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/operating_system_properties.asp
				standardProperties.Add("VersionDatabase", "Property");

				//    Name:  VersionMsi
				//    Desc:
				//    URL:
				//    Class: Installation Status Properties
				//    URL:   Installation Status Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/installation_status_properties.asp
				standardProperties.Add("VersionMsi", "Property");

				//    Name:  VersionNT
				//    Desc:
				//    URL:
				//    Class: Operating System Properties
				//    URL:   Operating System Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/operating_system_properties.asp
				standardProperties.Add("VersionNT", "Property");

				//    Name:  VersionNT64
				//    Desc:
				//    URL:
				//    Class: Operating System Properties
				//    URL:   Operating System Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/operating_system_properties.asp
				standardProperties.Add("VersionNT64", "Property");

				//    Name:  VirtualMemory
				//    Desc:
				//    URL:
				//    Class: Hardware Properties [Windows Installer]
				//    URL:   Hardware Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/hardware_properties.asp
				standardProperties.Add("VirtualMemory", "Property");

				//    Name:  WindowsBuild
				//    Desc:
				//    URL:
				//    Class: Operating System Properties
				//    URL:   Operating System Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/operating_system_properties.asp
				standardProperties.Add("WindowsBuild", "Property");

				//    Name:  WindowsVolume
				//    Desc:
				//    URL:
				//    Class: System Folder Properties
				//    URL:   System Folder Properties [Windows Installer]: http://msdn.microsoft.com/library/en-us/msi/setup/system_folder_properties.asp
				standardProperties.Add("WindowsVolume", "Property");
			}

			return standardProperties.ContainsKey(propertyName);
		}

		/// <summary>
		/// Get the value of an attribute with type YesNoType.
		/// </summary>
		/// <param name="value">Value to process.</param>
		/// <param name="sourceLineNumbers">Source information for the value.</param>
		/// <param name="elementName">Name of the element for this attribute, used for a possible exception.</param>
		/// <param name="attributeName">Name of the attribute.</param>
		/// <param name="elementId">Id of the element.</param>
		/// <returns>Returns true for a value of 'yes' and false for a value of 'no'.</returns>
		/// <exception cref="WixInvalidAttributeException">Thrown when the attribute's value is not 'yes' or 'no'.</exception>
		internal static bool IsYes(string value, SourceLineNumberCollection sourceLineNumbers, string elementName, string attributeName, string elementId)
		{
			if ("yes" == value)
			{
				return true;
			}
			else if ("no" == value)
			{
				return false;
			}
			else
			{
				throw new WixInvalidAttributeException(sourceLineNumbers, elementName, attributeName, "Must contain 'yes' or 'no' value.", elementId);
			}
		}

		/// <summary>
		/// Get the value of an attribute with type YesNoDefaultType.
		/// </summary>
		/// <param name="value">Value to process.</param>
		/// <param name="sourceLineNumbers">Source information for the value.</param>
		/// <param name="elementName">Name of the element for this attribute, used for a possible exception.</param>
		/// <param name="attributeName">Name of the attribute.</param>
		/// <param name="elementId">Id of the element.</param>
		/// <returns>Returns a YesNoDefaultType for the contents of the attribute value.</returns>
		/// <exception cref="WixInvalidAttributeException">Thrown when the attribute's value is not 'yes', 'no', or 'default'.</exception>
		internal static YesNoDefaultType GetYesNoDefault(string value, SourceLineNumberCollection sourceLineNumbers, string elementName, string attributeName, string elementId)
		{
			switch (value)
			{
				case "yes":
					return YesNoDefaultType.Yes;
				case "no":
					return YesNoDefaultType.No;
				case "default":
					return YesNoDefaultType.Default;
				default:
					throw new WixInvalidAttributeException(sourceLineNumbers, elementName, attributeName, "Must contain 'yes', 'no', or 'default' value.", elementId);
			}
		}

		/// <summary>
		/// Trims beginning and ending double-quote characters off a string, if they exist.
		/// </summary>
		/// <param name="value">string to be trimmed</param>
		/// <returns>string without beginning and ending double-quotes</returns>
		internal static string TrimQuotes(string value)
		{
			if ('"' == value[0] && '"' == value[value.Length - 1])
			{
				value = value.Substring(1, value.Length - 2);
			}
			return value;
		}

		/// <summary>
		/// Ensures the output contains a particular table definition and returns the output table.
		/// </summary>
		/// <param name="output">Output to add table definition.</param>
		/// <param name="tableDef">Table definition to add.</param>
		/// <returns>Output table in the output.</returns>
		internal static OutputTable EnsureOutputTable(Output output, TableDefinition tableDef)
		{
			OutputTable outputTable = output.OutputTables[tableDef.Name];
			if (null == outputTable)
			{
				outputTable = new OutputTable(tableDef);
				output.OutputTables.Add(outputTable);
			}

			return outputTable;
		}

		/// <summary>
		/// Finds the entry section and loads the symbols from an array of intermediates.
		/// </summary>
		/// <param name="intermediates">Array of intermediates to load symbols for and find entry section.</param>
		/// <param name="allowIdenticalRows">Flag specifying whether identical rows are allowed or not.</param>
		/// <param name="messageHandler">Message handler object to route all errors through.</param>
		/// <param name="entrySection">Located entry section.</param>
		/// <param name="allSymbols">Collection of symbols loaded.</param>
		internal static void FindEntrySectionAndLoadSymbols(
			Intermediate[] intermediates,
			bool allowIdenticalRows,
			IMessageHandler messageHandler,
			out Section entrySection,
			out SymbolCollection allSymbols)
		{
			entrySection = null;
			allSymbols = new SymbolCollection();

			for (int i = 0; i < intermediates.Length; ++i)
			{
				foreach (Section section in intermediates[i].Sections)
				{
					if (SectionType.Product == section.Type || SectionType.Module == section.Type || SectionType.PatchCreation == section.Type)
					{
						if (null == entrySection)
						{
							entrySection = section;
						}
						else
						{
							messageHandler.OnMessage(WixErrors.MultipleEntrySections(SourceLineNumberCollection.FromFileName(entrySection.Intermediate.SourcePath), entrySection.Id, section.Id));
							messageHandler.OnMessage(WixErrors.MultipleEntrySections2(SourceLineNumberCollection.FromFileName(section.Intermediate.SourcePath)));
						}
					}

					foreach (Symbol symbol in section.GetSymbols(messageHandler))
					{
						try
						{
							Symbol existingSymbol = allSymbols[symbol.Name];
							if (null == existingSymbol)
							{
								allSymbols.Add(symbol);
							}
							else if (allowIdenticalRows && existingSymbol.Row.IsIdentical(symbol.Row))
							{
								messageHandler.OnMessage(WixWarnings.IdenticalRowWarning(symbol.Row.SourceLineNumbers, existingSymbol.Name));
								messageHandler.OnMessage(WixWarnings.IdenticalRowWarning2(existingSymbol.Row.SourceLineNumbers));
							}
							else
							{
								allSymbols.AddDuplicate(symbol);
							}
						}
						catch (DuplicateSymbolsException)
						{
							// if there is already a duplicate symbol, just
							// another to the list, don't bother trying to
							// see if there are any identical symbols
							allSymbols.AddDuplicate(symbol);
						}
					}
				}
			}
		}

		/// <summary>
		/// Resolves all the simple references in a section.
		/// </summary>
		/// <param name="outputType">Active output type.</param>
		/// <param name="sections">Collection to add sections to.</param>
		/// <param name="section">Section with references to resolve.</param>
		/// <param name="allSymbols">Collection of all symbols from loaded intermediates.</param>
		/// <param name="referencedSymbols">Collection of all symbols referenced during linking.</param>
		/// <param name="unresolvedReferences">Unresolved references.</param>
		/// <param name="messageHandler">Message handler to report errors through.</param>
		/// <remarks>Note: recursive function.</remarks>
		internal static void ResolveReferences(
			OutputType outputType,
			SectionCollection sections,
			Section section,
			SymbolCollection allSymbols,
			StringCollection referencedSymbols,
			ArrayList unresolvedReferences,
			IMessageHandler messageHandler)
		{
			// if we already have this section bail
			if (sections.Contains(section))
			{
				return;
			}

			// add the section to the output and loop through the rest of the references
			// in a simple depth-first search
			sections.Add(section);
			foreach (Reference reference in section.References)
			{
				// if we're building into an output, modules ignore all references to the Media table
				if (OutputType.Module == outputType && "Media" == reference.TableName)
				{
					continue;
				}

				Symbol symbol = Common.GetSymbolForReference(section, reference, allSymbols, referencedSymbols, unresolvedReferences, messageHandler);
				if (null != symbol)
				{
					Common.ResolveReferences(outputType, sections, symbol.Section, allSymbols, referencedSymbols, unresolvedReferences, messageHandler);
				}
			}
		}

		/// <summary>
		/// Gets the symbol for a reference.
		/// </summary>
		/// <param name="section">Section that contains references to resolve.</param>
		/// <param name="reference">References to resolve.</param>
		/// <param name="allSymbols">Collection of all symbols from loaded intermediates.</param>
		/// <param name="referencedSymbols">Collection of all symbols referenced during linking.</param>
		/// <param name="unresolvedReferences">Unresolved references.</param>
		/// <param name="messageHandler">Message handler to report errors through.</param>
		/// <returns>Symbol it it was found or null if the symbol was not specified.</returns>
		internal static Symbol GetSymbolForReference(
			Section section,
			Reference reference,
			SymbolCollection allSymbols,
			StringCollection referencedSymbols,
			ArrayList unresolvedReferences,
			IMessageHandler messageHandler)
		{
			Symbol symbol = null;
			try
			{
				symbol = allSymbols[reference.SymbolicName];
				if (null == symbol)
				{
					unresolvedReferences.Add(new ReferenceSection(section, reference));
				}
				else
				{
					// components are indexed in ResolveComplexReferences
					if (null != symbol.TableName && "Component" != symbol.TableName && !referencedSymbols.Contains(symbol.Name))
					{
						referencedSymbols.Add(symbol.Name);
					}
				}
			}
			catch (DuplicateSymbolsException e)
			{
				Symbol[] symbols = e.GetDuplicateSymbols();
				Debug.Assert(1 < symbols.Length);

				messageHandler.OnMessage(WixErrors.DuplicateSymbol((null != symbols[0].Row ? symbols[0].Row.SourceLineNumbers : null), symbols[0].Name));
				for (int i = 1; i < symbols.Length; ++i)
				{
					if (null != symbols[i].Row && null != symbols[i].Row.SourceLineNumbers)
					{
						messageHandler.OnMessage(WixErrors.DuplicateSymbol2(symbols[i].Row.SourceLineNumbers));
					}
				}
			}

			return symbol;
		}

		/// <summary>
		/// Helper class to keep track of references in their section.
		/// </summary>
		internal struct ReferenceSection
		{
			public Section section;
			public Reference reference;

			/// <summary>
			/// Creates an object that ties references to their section
			/// </summary>
			/// <param name="section">Section that owns the reference.</param>
			/// <param name="reference">Reference in the section.</param>
			public ReferenceSection(Section section, Reference reference)
			{
				this.section = section;
				this.reference = reference;
			}
		}
	}
}
