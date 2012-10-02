//-------------------------------------------------------------------------------------------------
// <copyright file="Linker.cs" company="Microsoft">
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
// Linker core of the Windows Installer Xml toolset.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;
	using System.Collections;
	using System.Collections.Specialized;
	using System.Diagnostics;
	using System.Globalization;
	using System.IO;
	using System.Text;

	/// <summary>
	/// Linker core of the Windows Installer Xml toolset.
	/// </summary>
	public class Linker : IExtensionMessageHandler, IMessageHandler
	{
		private const int VisitedActionSentinel = -10;
		private static readonly char[] tabCharacter = "\t".ToCharArray();
		private static readonly char[] colonCharacter = ":".ToCharArray();

		private ActionTable standardActions;
		private TableDefinitionCollection tableDefinitions;
		private Hashtable extensions;
		private Output activeOutput;
		private ExtensionMessages extensionMessages;

		private bool foundError;
		private ArrayList intermediates;
		private string imagebaseOutputPath;
		private bool allowIdenticalRows;
		private bool allowUnresolvedReferences;
		private PedanticLevel pedanticLevel;
		private bool sectionIdOnTuples;
		private bool suppressAdminSequence;
		private bool suppressAdvertiseSequence;
		private bool suppressUISequence;
		private Localizer localizer;

		/// <summary>
		/// Creates a linker.
		/// </summary>
		/// <param name="smallTables">Use small table definitions for MSI/MSM.</param>
		public Linker(bool smallTables)
		{
			this.standardActions = Common.GetStandardActions();
			this.tableDefinitions = Common.GetTableDefinitions(smallTables);
			this.extensions = new Hashtable();
			this.extensionMessages = new ExtensionMessages(this);

			this.intermediates = new ArrayList();
		}

		/// <summary>
		/// Event for messages.
		/// </summary>
		public event MessageEventHandler Message;

		/// <summary>
		/// Gets the list of intermediates to link.
		/// </summary>
		/// <value>List of intermediates to link.</value>
		public ArrayList Intermediates
		{
			get { return this.intermediates; }
		}

		/// <summary>
		/// Gets or sets the base path for output image.
		/// </summary>
		/// <value>Base path for output image.</value>
		public string ImageBaseOutputPath
		{
			get { return this.imagebaseOutputPath; }
			set { this.imagebaseOutputPath = value; }
		}

		/// <summary>
		/// Gets or sets the flag specifying if identical rows are allowed during linking.
		/// </summary>
		/// <value>True if identical rows are allowed.</value>
		public bool AllowIdenticalRows
		{
			get { return this.allowIdenticalRows; }
			set { this.allowIdenticalRows = value; }
		}

		/// <summary>
		/// Gets or sets the flag specifying if unresolved references are allowed during linking.
		/// </summary>
		/// <value>True if unresolved references are allowed.</value>
		public bool AllowUnresolvedReferences
		{
			get { return this.allowUnresolvedReferences; }
			set { this.allowUnresolvedReferences = value; }
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
		/// Turns on or off tagging the rows with the sectionId attribute in the output xml.
		/// </summary>
		/// <value>True if rows should be tagged.</value>
		public bool SectionIdOnTuples
		{
			get { return this.sectionIdOnTuples; }
			set { this.sectionIdOnTuples = value; }
		}

		/// <summary>
		/// Sets the option to suppress admin sequence actions.
		/// </summary>
		/// <value>The option to suppress admin sequence actions.</value>
		public bool SuppressAdminSequence
		{
			get { return this.suppressAdminSequence; }
			set { this.suppressAdminSequence = value; }
		}

		/// <summary>
		/// Sets the option to suppress advertise sequence actions.
		/// </summary>
		/// <value>The option to suppress advertise sequence actions.</value>
		public bool SuppressAdvertiseSequence
		{
			get { return this.suppressAdvertiseSequence; }
			set { this.suppressAdvertiseSequence = value; }
		}

		/// <summary>
		/// Sets the option to suppress UI sequence actions.
		/// </summary>
		/// <value>The option to suppress UI sequence actions.</value>
		public bool SuppressUISequence
		{
			get { return this.suppressUISequence; }
			set { this.suppressUISequence = value; }
		}

		/// <summary>
		/// Gets the table definitions used by the linkner.
		/// </summary>
		/// <value>Table definitions used by the linker.</value>
		public TableDefinitionCollection TableDefinitions
		{
			get { return this.tableDefinitions; }
		}

		/// <summary>
		/// Gets or sets the localizer.
		/// </summary>
		/// <value>The localizer.</value>
		public Localizer Localizer
		{
			get { return this.localizer; }
			set { this.localizer = value; }
		}

		/// <summary>
		/// Adds an extension to the linker.
		/// </summary>
		/// <param name="extension">Schema extension to add to linker.</param>
		public void AddExtension(SchemaExtension extension)
		{
			extension.Messages = this.extensionMessages;

			// check if this extension is addding a schema namespace that already exists
			if (this.extensions.Contains(extension.Schema.TargetNamespace))
			{
				throw new WixExtensionNamespaceConflictException(extension, (SchemaExtension)this.extensions[extension.Schema.TargetNamespace]);
			}

			// check if the extension is adding a table that already exists
			foreach (TableDefinition tableDefinition in extension.TableDefinitions)
			{
				if (this.tableDefinitions.Contains(tableDefinition.Name))
				{
					throw new WixExtensionTableDefinitionConflictException(extension, tableDefinition);
				}
			}

			// add the extension and its table definitions to the linker
			this.extensions.Add(extension.Schema.TargetNamespace, extension);
			foreach (TableDefinition tableDefinition in extension.TableDefinitions)
			{
				this.tableDefinitions.Add(tableDefinition);
			}
		}

		/// <summary>
		/// Links an array of intermediates into an output.
		/// </summary>
		/// <param name="intermediates">Array of intermediates to link together.</param>
		/// <returns>Output object from the linking.</returns>
		public Output Link(Intermediate[] intermediates)
		{
			Output output = null;

			try
			{
				SymbolCollection allSymbols;
				Section entrySection;
				bool containsModuleSubstitution = false;
				bool containsModuleConfiguration = false;

				StringCollection referencedSymbols = new StringCollection();
				ArrayList unresolvedReferences = new ArrayList();

				ConnectToFeatureCollection componentGroupsToFeatures = new ConnectToFeatureCollection();
				ConnectToModuleCollection componentGroupsToModules = new ConnectToModuleCollection();
				ComplexReferenceCollection componentsToComponentGroupsComplexReferences = new ComplexReferenceCollection();
				ConnectToFeatureCollection componentsToFeatures = new ConnectToFeatureCollection();
				ConnectToFeatureCollection featuresToFeatures = new ConnectToFeatureCollection();

				this.activeOutput = null;
				this.foundError = false;

				SortedList adminProperties = new SortedList();
				SortedList secureProperties = new SortedList();
				SortedList hiddenProperties = new SortedList();

				ActionTable requiredActions = new ActionTable();
				RowCollection suppressActionRows = new RowCollection();
				TableDefinitionCollection customTableDefinitions = new TableDefinitionCollection();
				RowCollection customRows = new RowCollection();

				foreach (SchemaExtension extension in this.extensions.Values)
				{
					extension.Messages = this.extensionMessages;
				}

				// first find the entry section and create the symbols hash for all
				// the sections in all the intermediates
				Common.FindEntrySectionAndLoadSymbols(intermediates, this.allowIdenticalRows, this, out entrySection, out allSymbols);

				// should have found an entry section by now
				if (null == entrySection)
				{
					this.OnMessage(WixErrors.MissingEntrySection());
					return null;
				}

				// add the standard action symbols to the entry section's symbol table
				this.LoadStandardActionSymbols(allSymbols, entrySection, this.standardActions);

				// now that we know where we're starting from, create the output object
				output = new Output(entrySection); // Note: this entry section will get added to the output section collection later
				if (null != this.localizer && -1 != this.localizer.Codepage)
				{
					output.Codepage = this.localizer.Codepage;
				}
				this.activeOutput = output;

				// resolve the symbol references to find the set of sections we
				// care about then resolve complex references in those sections
				Common.ResolveReferences(output.Type, output.Sections, output.EntrySection, allSymbols, referencedSymbols, unresolvedReferences, this);
				this.ProcessComplexReferences(output, output.Sections, referencedSymbols, componentsToComponentGroupsComplexReferences, componentGroupsToFeatures, componentGroupsToModules, componentsToFeatures, featuresToFeatures);
				for (int i = 0; i < unresolvedReferences.Count; ++i)
				{
					Common.ReferenceSection referenceSection = (Common.ReferenceSection)unresolvedReferences[i];
					if (this.allowUnresolvedReferences)
					{
						this.OnMessage(WixWarnings.UnresolvedReferenceWarning(SourceLineNumberCollection.FromFileName(referenceSection.section.Intermediate.SourcePath), referenceSection.section.Type.ToString(), referenceSection.section.Id, referenceSection.reference.SymbolicName));
					}
					else
					{
						this.OnMessage(WixErrors.UnresolvedReference(SourceLineNumberCollection.FromFileName(referenceSection.section.Intermediate.SourcePath), referenceSection.section.Type.ToString(), referenceSection.section.Id, referenceSection.reference.SymbolicName));
					}
				}

				if (this.foundError)
				{
					return null;
				}

				this.ResolveComponentGroups(output, referencedSymbols, componentsToComponentGroupsComplexReferences, componentGroupsToFeatures, componentGroupsToModules, componentsToFeatures);
				this.FindOrphanedSymbols(referencedSymbols);

				// resolve the feature backlink for each section then update the feature to feature connects
				this.ResolveFeatureBacklinks(output, componentsToFeatures, allSymbols, referencedSymbols, unresolvedReferences);
				this.ResolveFeatureToFeatureConnects(featuresToFeatures, allSymbols, referencedSymbols, unresolvedReferences);

				// create a Hashtable of all the suppressed sequence types
				Hashtable suppressedSequenceTypes = new Hashtable();
				// create a Hashtable of all the suppressed standard actions
				Hashtable suppressedStandardActions = new Hashtable();

				if (this.suppressAdminSequence)
				{
					suppressedSequenceTypes[SequenceType.adminExecute] = null;
					suppressedSequenceTypes[SequenceType.adminUI] = null;
				}
				if (this.suppressAdvertiseSequence)
				{
					suppressedSequenceTypes[SequenceType.advertiseExecute] = null;
				}
				if (this.suppressUISequence)
				{
					suppressedSequenceTypes[SequenceType.adminUI] = null;
					suppressedSequenceTypes[SequenceType.installUI] = null;
				}

				// start generating OutputTables and OutputRows for all the sections in the output
				RowCollection ensureTableRows = new RowCollection();
				foreach (Section section in this.activeOutput.Sections)
				{
					// add this sections list of identifiers to ignore modularization
					this.activeOutput.IgnoreModularizations.Add(section.IgnoreModularizations);

					foreach (Table table in section.Tables)
					{
						bool copyRows = !table.Definition.IsUnreal; // by default, copy rows if the table is not unreal

						// handle special tables
						switch (table.Name)
						{
							case "Actions":
								foreach (Row row in table.Rows)
								{
									SequenceType sequenceType;
									string seqType = (string)row[0];
									string id = (string)row[1];
									int sequence = null == row[3] ? 0 : Convert.ToInt32(row[3]);
									bool suppress = 1 == Convert.ToInt32(row[6]);

									switch (seqType)
									{
										case "AdminUISequence":
											sequenceType = SequenceType.adminUI;
											break;
										case "AdminExecuteSequence":
											sequenceType = SequenceType.adminExecute;
											break;
										case "AdvertiseExecuteSequence":
											sequenceType = SequenceType.advertiseExecute;
											break;
										case "InstallExecuteSequence":
											sequenceType = SequenceType.installExecute;
											break;
										case "InstallUISequence":
											sequenceType = SequenceType.installUI;
											break;
										default:
											throw new WixInvalidSequenceTypeException(null, seqType);
									}

									if (suppressedSequenceTypes.Contains(sequenceType))
									{
										this.OnMessage(WixWarnings.SuppressAction(id, Action.SequenceTypeToString(sequenceType)));
										continue;
									}

									// create a SuppressAction row to allow suppressing the action from a merge module
									if (suppress)
									{
										Row suppressActionRow = new Row(row.SourceLineNumbers, this.tableDefinitions["SuppressAction"]);
										if ("AdvertiseExecuteSequence" == (string)row[0])
										{
											suppressActionRow[0] = "AdvtExecuteSequence";
										}
										else
										{
											suppressActionRow[0] = row[0];
										}
										suppressActionRow[1] = row[1];

										suppressActionRows.Add(suppressActionRow);
									}

									Action action = this.standardActions[sequenceType, id];
									string beforeAction = (string)row[4];
									string afterAction = (string)row[5];

									// if this is not a standard action or there is a before or after action specified
									if (null == action || null != beforeAction || null != afterAction)
									{
										action = new Action(sequenceType, id, (string)row[2], sequence, beforeAction, afterAction);
										requiredActions.Add(action, true); // add the action and overwrite even if it already exists since this is a customization

										// if the parent action is a standard action add it to the required list
										string parentActionName = null != beforeAction ? beforeAction : afterAction;
										Action parentAction = this.standardActions[sequenceType, parentActionName];
										if (null != parentAction)
										{
											requiredActions.Add(parentAction);
										}
									}
									else if (!suppress) // must have a standard action that is being overriden (when not suppressed)
									{
										action.Condition = (string)row[2];
										if (0 != sequence) // if the user specified a sequence number, override the default
										{
											action.SequenceNumber = sequence;
										}

										requiredActions.Add(action, true); // ensure this action is in the required list
									}

									// action was suppressed by user
									if (suppress && null != action)
									{
										suppressedStandardActions[String.Concat(action.SequenceType.ToString(), id)] = action;
									}
								}
								break;

							case "AppSearch":
								Common.EnsureOutputTable(this.activeOutput, this.tableDefinitions["Signature"]);
								requiredActions.Add(this.standardActions[SequenceType.installExecute, "AppSearch"]);
								requiredActions.Add(this.standardActions[SequenceType.installUI, "AppSearch"]);
								break;

							case "Binary":
							case "Icon":
							case "MsiDigitalCertificate":
								foreach (Row row in table.Rows)
								{
									ImportStreamType importStreamType = ImportStreamType.Unknown;
									switch (table.Name)
									{
										case "Binary":
											importStreamType = ImportStreamType.Binary;
											break;
										case "Icon":
											importStreamType = ImportStreamType.Icon;
											break;
										case "MsiDigitalCertificate":
											importStreamType = ImportStreamType.DigitalCertificate;
											break;
									}

									ImportStream importStream = new ImportStream(importStreamType, row[0].ToString(), row[1].ToString());
									if (this.activeOutput.ImportStreams.Contains(importStream.Name))
									{
										this.OnMessage(WixErrors.DuplicateSymbol(row.SourceLineNumbers, String.Format("{0} element with Id='{1}' is defined multiple times.", table.Name, row.Symbol.Name)));
									}

									this.activeOutput.ImportStreams.Add(importStream);
								}
								Common.EnsureOutputTable(this.activeOutput, this.tableDefinitions[table.Name]);
								copyRows = false;
								break;

							case "BindImage":
								requiredActions.Add(this.standardActions[SequenceType.installExecute, "BindImage"]);
								break;

							case "CCPSearch":
								requiredActions.Add(this.standardActions[SequenceType.installExecute, "AppSearch"]);
								requiredActions.Add(this.standardActions[SequenceType.installExecute, "CCPSearch"]);
								requiredActions.Add(this.standardActions[SequenceType.installExecute, "RMCCPSearch"]);
								requiredActions.Add(this.standardActions[SequenceType.installUI, "AppSearch"]);
								requiredActions.Add(this.standardActions[SequenceType.installUI, "CCPSearch"]);
								requiredActions.Add(this.standardActions[SequenceType.installUI, "RMCCPSearch"]);
								break;

							case "Class":
								requiredActions.Add(this.standardActions[SequenceType.advertiseExecute, "RegisterClassInfo"]);
								requiredActions.Add(this.standardActions[SequenceType.installExecute, "RegisterClassInfo"]);
								requiredActions.Add(this.standardActions[SequenceType.installExecute, "UnregisterClassInfo"]);
								break;

							case "Complus":
								requiredActions.Add(this.standardActions[SequenceType.installExecute, "RegisterComPlus"]);
								requiredActions.Add(this.standardActions[SequenceType.installExecute, "UnregisterComPlus"]);
								break;

							case "CreateFolder":
								requiredActions.Add(this.standardActions[SequenceType.installExecute, "CreateFolders"]);
								requiredActions.Add(this.standardActions[SequenceType.installExecute, "RemoveFolders"]);
								break;

							case "CustomAction":
								if (OutputType.Module == this.activeOutput.Type)
								{
									Common.EnsureOutputTable(this.activeOutput, this.tableDefinitions["AdminExecuteSequence"]);
									Common.EnsureOutputTable(this.activeOutput, this.tableDefinitions["AdminUISequence"]);
									Common.EnsureOutputTable(this.activeOutput, this.tableDefinitions["AdvtExecuteSequence"]);
									Common.EnsureOutputTable(this.activeOutput, this.tableDefinitions["InstallExecuteSequence"]);
									Common.EnsureOutputTable(this.activeOutput, this.tableDefinitions["InstallUISequence"]);
								}
								break;

							case "CustomTables":
								foreach (Row row in table.Rows)
								{
									TableDefinition customTable = new TableDefinition((string)row[0], false);

									if (null == row[4])
									{
										this.OnMessage(WixErrors.ExpectedAttribute(row.SourceLineNumbers, "CustomTable/Column", "PrimaryKey"));
									}

									string[] columnNames = row[2].ToString().Split(tabCharacter);
									string[] columnTypes = row[3].ToString().Split(tabCharacter);
									string[] primaryKeys = row[4].ToString().Split(tabCharacter);
									string[] minValues = row[5] == null ? null : row[5].ToString().Split(tabCharacter);
									string[] maxValues = row[6] == null ? null : row[6].ToString().Split(tabCharacter);
									string[] keyTables = row[7] == null ? null : row[7].ToString().Split(tabCharacter);
									string[] keyColumns = row[8] == null ? null : row[8].ToString().Split(tabCharacter);
									string[] categories = row[9] == null ? null : row[9].ToString().Split(tabCharacter);
									string[] sets = row[10] == null ? null : row[10].ToString().Split(tabCharacter);
									string[] descriptions = row[11] == null ? null : row[11].ToString().Split(tabCharacter);
									string[] modularizations = row[12] == null ? null : row[12].ToString().Split(tabCharacter);

									int currentPrimaryKey = 0;

									for (int i = 0; i < columnNames.Length; ++i)
									{
										string name = columnNames[i];
										ColumnType type = ColumnType.Unknown;
										switch (columnTypes[i].Substring(0, 1).ToLower(CultureInfo.InvariantCulture))
										{
											case "s":
												type = ColumnType.String;
												break;
											case "l":
												type = ColumnType.Localized;
												break;
											case "i":
												type = ColumnType.Number;
												break;
											case "g":
												type = ColumnType.Object;
												break;
											default:
												throw new ApplicationException(String.Format("Unknown custom table column type: {0}", columnTypes[i]));
										}
										bool nullable = columnTypes[i].Substring(0, 1) == columnTypes[i].Substring(0, 1).ToUpper(CultureInfo.InvariantCulture);
										int length = Convert.ToInt32(columnTypes[i].Substring(1));

										bool primaryKey = false;
										if (currentPrimaryKey < primaryKeys.Length && primaryKeys[currentPrimaryKey] == columnNames[i])
										{
											primaryKey = true;
											currentPrimaryKey++;
										}

										bool minValSet = null != minValues && null != minValues[i] && 0 < minValues[i].Length;
										int minValue = 0;
										if (minValSet)
										{
											minValue = Convert.ToInt32(minValues[i]);
										}

										bool maxValSet = null != maxValues && null != maxValues[i] && 0 < maxValues[i].Length;
										int maxValue = 0;
										if (maxValSet)
										{
											maxValue = Convert.ToInt32(maxValues[i]);
										}

										bool keyColumnSet = null != keyColumns && null != keyColumns[i] && 0 < keyColumns[i].Length;
										int keyColumn = 0;
										if (keyColumnSet)
										{
											keyColumn = Convert.ToInt32(keyColumns[i]);
										}

										ColumnCategory category = ColumnCategory.Unknown;
										if (null != categories && null != categories[i] && 0 < categories[i].Length)
										{
											switch (categories[i])
											{
												case "Text":
													category = ColumnCategory.Text;
													break;
												case "UpperCase":
													category = ColumnCategory.UpperCase;
													break;
												case "LowerCase":
													category = ColumnCategory.LowerCase;
													break;
												case "Integer":
													category = ColumnCategory.Integer;
													break;
												case "DoubleInteger":
													category = ColumnCategory.DoubleInteger;
													break;
												case "TimeDate":
													category = ColumnCategory.TimeDate;
													break;
												case "Identifier":
													category = ColumnCategory.Identifier;
													break;
												case "Property":
													category = ColumnCategory.Property;
													break;
												case "Filename":
													category = ColumnCategory.Filename;
													break;
												case "WildCardFilename":
													category = ColumnCategory.WildCardFilename;
													break;
												case "Path":
													category = ColumnCategory.Path;
													break;
												case "Paths":
													category = ColumnCategory.Paths;
													break;
												case "AnyPath":
													category = ColumnCategory.AnyPath;
													break;
												case "DefaultDir":
													category = ColumnCategory.DefaultDir;
													break;
												case "RegPath":
													category = ColumnCategory.RegPath;
													break;
												case "Formatted":
													category = ColumnCategory.Formatted;
													break;
												case "Template":
													category = ColumnCategory.Template;
													break;
												case "Condition":
													category = ColumnCategory.Condition;
													break;
												case "Guid":
													category = ColumnCategory.Guid;
													break;
												case "Version":
													category = ColumnCategory.Version;
													break;
												case "Language":
													category = ColumnCategory.Language;
													break;
												case "Binary":
													category = ColumnCategory.Binary;
													break;
												case "CustomSource":
													category = ColumnCategory.CustomSource;
													break;
												case "Cabinet":
													category = ColumnCategory.Cabinet;
													break;
												case "Shortcut":
													category = ColumnCategory.Shortcut;
													break;
												default:
													break;
											}
										}

										string keyTable = keyTables != null ? keyTables[i] : null;
										string setValue = sets != null ? sets[i] : null;
										string description = descriptions != null ? descriptions[i] : null;
										string modString = modularizations != null ? modularizations[i] : null;
										ColumnModularizeType modularization = ColumnModularizeType.None;
										if (modString != null)
										{
											switch (modString)
											{
												case "None":
													modularization = ColumnModularizeType.None;
													break;
												case "Column":
													modularization = ColumnModularizeType.Column;
													break;
												case "Property":
													modularization = ColumnModularizeType.Property;
													break;
												case "Condition":
													modularization = ColumnModularizeType.Condition;
													break;
												case "CompanionFile":
													modularization = ColumnModularizeType.CompanionFile;
													break;
												case "SemicolonDelimited":
													modularization = ColumnModularizeType.SemicolonDelimited;
													break;
											}
										}

										ColumnDefinition columnDefinition = new ColumnDefinition(name, type, length, primaryKey, false, nullable, modularization, false, ColumnType.Localized == type, minValSet, minValue, maxValSet, maxValue, keyTable, keyColumnSet, keyColumn, category, setValue, description, true, true);
										customTable.Columns.Add(columnDefinition);
									}

									customTableDefinitions.Add(customTable);
								}

								copyRows = false; // we've created table definitions from these rows, no need to process them any longer
								break;

							case "RowData":
								foreach (Row row in table.Rows)
								{
									customRows.Add(row);
								}

								copyRows = false;
								break;

							case "Dialog":
								Common.EnsureOutputTable(this.activeOutput, this.tableDefinitions["ListBox"]);
								break;

							case "Directory":
								foreach (Row row in table.Rows)
								{
									if (OutputType.Module == this.activeOutput.Type && Common.IsStandardDirectory(row[0].ToString()))
									{
										// if the directory table contains references to standard windows folders
										// mergemod.dll will add customactions to set the MSM directory to
										// the same directory as the standard windows folder and will add references to
										// custom action to all the standard sequence tables.  A problem will occur
										// if the MSI does not have these tables as mergemod.dll does not add these
										// tables to the MSI if absent.  This code adds the tables in case mergemod.dll
										// needs them.
										Common.EnsureOutputTable(this.activeOutput, this.tableDefinitions["CustomAction"]);
										Common.EnsureOutputTable(this.activeOutput, this.tableDefinitions["AdminExecuteSequence"]);
										Common.EnsureOutputTable(this.activeOutput, this.tableDefinitions["AdminUISequence"]);
										Common.EnsureOutputTable(this.activeOutput, this.tableDefinitions["AdvtExecuteSequence"]);
										Common.EnsureOutputTable(this.activeOutput, this.tableDefinitions["InstallExecuteSequence"]);
										Common.EnsureOutputTable(this.activeOutput, this.tableDefinitions["InstallUISequence"]);
										break; // no need to look here any more, we already found all that we needed to
									}
								}
								break;

							case "DuplicateFile":
								requiredActions.Add(this.standardActions[SequenceType.installExecute, "DuplicateFiles"]);
								requiredActions.Add(this.standardActions[SequenceType.installExecute, "RemoveDuplicateFiles"]);
								break;

							case "EnsureTables":
								foreach (Row row in table.Rows)
								{
									ensureTableRows.Add(row);
								}
								break;

							case "Environment":
								requiredActions.Add(this.standardActions[SequenceType.installExecute, "WriteEnvironmentStrings"]);
								requiredActions.Add(this.standardActions[SequenceType.installExecute, "RemoveEnvironmentStrings"]);
								break;

							case "Extension":
								requiredActions.Add(this.standardActions[SequenceType.advertiseExecute, "RegisterExtensionInfo"]);
								requiredActions.Add(this.standardActions[SequenceType.installExecute, "RegisterExtensionInfo"]);
								requiredActions.Add(this.standardActions[SequenceType.installExecute, "UnregisterExtensionInfo"]);
								break;

							case "File":
								foreach (FileRow row in table.Rows)
								{
									// DiskId is not valid when creating a module, so set it to
									// 0 for all files to ensure proper sorting in the binder
									if (OutputType.Module == this.activeOutput.Type)
									{
										row.DiskId = 0;
									}

									// if we have an assembly, insert the MsiAssembly row and assembly actions
									if (FileAssemblyType.NotAnAssembly != row.AssemblyType)
									{
										string feature;
										if (OutputType.Module == output.Type)
										{
											feature = Guid.Empty.ToString("B");
										}
										else
										{
											ConnectToFeature connect = componentsToFeatures[row.Component];
											if (null == connect)
											{
												throw new WixMissingFeatureException(row.SourceLineNumbers, new FeatureBacklink(row.Component, FeatureBacklinkType.Assembly, row.File));
											}
											feature = connect.PrimaryFeature;
										}

										OutputTable assemblyOutputTable = Common.EnsureOutputTable(output, this.tableDefinitions["MsiAssembly"]);
										Row assemblyRow = new Row(assemblyOutputTable.TableDefinition);
										assemblyRow[0] = row.Component;
										assemblyRow[1] = feature;
										assemblyRow[2] = row.AssemblyManifest;
										assemblyRow[3] = row.AssemblyApplication;
										assemblyRow[4] = Convert.ToInt32(row.AssemblyType);
										assemblyOutputTable.OutputRows.Add(new OutputRow(assemblyRow, this.sectionIdOnTuples ? section.Id : null));

										requiredActions.Add(this.standardActions[SequenceType.advertiseExecute, "MsiPublishAssemblies"]);
										requiredActions.Add(this.standardActions[SequenceType.installExecute, "MsiPublishAssemblies"]);
										requiredActions.Add(this.standardActions[SequenceType.installExecute, "MsiUnpublishAssemblies"]);
									}

									if (null == row.Source) // source to the file must be provided
									{
										this.OnMessage(WixErrors.FileSourceRequired(row.SourceLineNumbers, row.File));
									}
									this.activeOutput.FileMediaInformationCollection.Add(new FileMediaInformation(row));
								}
								requiredActions.Add(this.standardActions[SequenceType.installExecute, "InstallFiles"]);
								requiredActions.Add(this.standardActions[SequenceType.installExecute, "RemoveFiles"]);
								break;

							case "Font":
								requiredActions.Add(this.standardActions[SequenceType.installExecute, "RegisterFonts"]);
								requiredActions.Add(this.standardActions[SequenceType.installExecute, "UnregisterFonts"]);
								break;

							case "IniFile":
							case "RemoveIniFile":
								requiredActions.Add(this.standardActions[SequenceType.installExecute, "WriteIniValues"]);
								requiredActions.Add(this.standardActions[SequenceType.installExecute, "RemoveIniValues"]);
								break;

							case "IsolatedComponent":
								requiredActions.Add(this.standardActions[SequenceType.installExecute, "IsolateComponents"]);
								break;

							case "LaunchCondition":
								requiredActions.Add(this.standardActions[SequenceType.installExecute, "LaunchConditions"]);
								requiredActions.Add(this.standardActions[SequenceType.installUI, "LaunchConditions"]);
								break;

							case "Media":
								foreach (MediaRow row in table.Rows)
								{
									this.activeOutput.MediaRows.Add(row);
								}
								copyRows = false;
								break;

							case "Merge":
								// just copy the rows to the output
								copyRows = true;
								break;

							case "MIME":
								requiredActions.Add(this.standardActions[SequenceType.advertiseExecute, "RegisterMIMEInfo"]);
								requiredActions.Add(this.standardActions[SequenceType.installExecute, "RegisterMIMEInfo"]);
								requiredActions.Add(this.standardActions[SequenceType.installExecute, "UnregisterMIMEInfo"]);
								break;

							case "ModuleSignature":
								if (OutputType.Module == this.activeOutput.Type)
								{
									foreach (Row row in table.Rows)
									{
										if (null != this.activeOutput.ModularizationGuid)
										{
											throw new ArgumentOutOfRangeException("Unexpected number of rows found in table", "ModuleSignature");
										}

										this.activeOutput.ModularizationGuid = row[3].ToString();
									}
								}
								break;

							case "ModuleSubstitution":
								containsModuleSubstitution = true;
								break;

							case "ModuleConfiguration":
								containsModuleConfiguration = true;
								break;

							case "MoveFile":
								requiredActions.Add(this.standardActions[SequenceType.installExecute, "MoveFiles"]);
								break;

							case "MsiAssembly":
								requiredActions.Add(this.standardActions[SequenceType.advertiseExecute, "MsiPublishAssemblies"]);
								requiredActions.Add(this.standardActions[SequenceType.installExecute, "MsiPublishAssemblies"]);
								requiredActions.Add(this.standardActions[SequenceType.installExecute, "MsiUnpublishAssemblies"]);
								break;

							case "ODBCDataSource":
							case "ODBCTranslator":
							case "ODBCDriver":
								requiredActions.Add(this.standardActions[SequenceType.installExecute, "SetODBCFolders"]);
								requiredActions.Add(this.standardActions[SequenceType.installExecute, "InstallODBC"]);
								requiredActions.Add(this.standardActions[SequenceType.installExecute, "RemoveODBC"]);
								break;

							case "ProgId":
								requiredActions.Add(this.standardActions[SequenceType.advertiseExecute, "RegisterProgIdInfo"]);
								requiredActions.Add(this.standardActions[SequenceType.installExecute, "RegisterProgIdInfo"]);
								requiredActions.Add(this.standardActions[SequenceType.installExecute, "UnregisterProgIdInfo"]);
								Common.EnsureOutputTable(this.activeOutput, this.tableDefinitions["Extension"]); // Extension table is required with a ProgId table
								break;

							case "Property":
								foreach (PropertyRow row in table.Rows)
								{
									// if there is no value in the property, then it must be virtual
									if (null == row.Value || 0 == row.Value.Length)
									{
										row.IsUnreal = true;
									}

									if (row.Admin)
									{
										adminProperties[row.Id] = null;
									}

									if (row.Secure)
									{
										secureProperties[row.Id] = null;
									}

									if (row.Hidden)
									{
										hiddenProperties[row.Id] = null;
									}
								}
								break;

							case "PublishComponent":
								requiredActions.Add(this.standardActions[SequenceType.advertiseExecute, "PublishComponents"]);
								requiredActions.Add(this.standardActions[SequenceType.installExecute, "PublishComponents"]);
								requiredActions.Add(this.standardActions[SequenceType.installExecute, "UnpublishComponents"]);
								break;

							case "Registry":
								requiredActions.Add(this.standardActions[SequenceType.installExecute, "WriteRegistryValues"]);
								requiredActions.Add(this.standardActions[SequenceType.installExecute, "RemoveRegistryValues"]);
								break;

							case "RemoveFile":
								requiredActions.Add(this.standardActions[SequenceType.installExecute, "RemoveFiles"]);
								break;

							case "SelfReg":
								requiredActions.Add(this.standardActions[SequenceType.installExecute, "SelfRegModules"]);
								requiredActions.Add(this.standardActions[SequenceType.installExecute, "SelfUnregModules"]);
								break;

							case "ServiceControl":
								requiredActions.Add(this.standardActions[SequenceType.installExecute, "StartServices"]);
								requiredActions.Add(this.standardActions[SequenceType.installExecute, "StopServices"]);
								break;

							case "ServiceInstall":
								requiredActions.Add(this.standardActions[SequenceType.installExecute, "InstallServices"]);
								requiredActions.Add(this.standardActions[SequenceType.installExecute, "DeleteServices"]);
								break;

							case "Shortcut":
								requiredActions.Add(this.standardActions[SequenceType.advertiseExecute, "CreateShortcuts"]);
								requiredActions.Add(this.standardActions[SequenceType.installExecute, "CreateShortcuts"]);
								requiredActions.Add(this.standardActions[SequenceType.installExecute, "RemoveShortcuts"]);
								break;

							case "TypeLib":
								requiredActions.Add(this.standardActions[SequenceType.installExecute, "RegisterTypeLibraries"]);
								requiredActions.Add(this.standardActions[SequenceType.installExecute, "UnregisterTypeLibraries"]);
								break;

							case "Upgrade":
							{
								requiredActions.Add(this.standardActions[SequenceType.installExecute, "FindRelatedProducts"]);
								requiredActions.Add(this.standardActions[SequenceType.installExecute, "MigrateFeatureStates"]);
								requiredActions.Add(this.standardActions[SequenceType.installUI, "FindRelatedProducts"]);
								requiredActions.Add(this.standardActions[SequenceType.installUI, "MigrateFeatureStates"]);

								foreach (UpgradeRow row in table.Rows)
								{
									// this should never happen because candle will make sure that all UpgradeVersion(s) have an ActionProperty...but still don't let it slide
									if (null == row.ActionProperty)
									{
										this.OnMessage(WixErrors.ExpectedAttribute(row.SourceLineNumbers, "UpgradeVersion", "ActionProperty"));
									}

									secureProperties[row.ActionProperty] = null;
								}

								break;
							}

							case "_SummaryInformation":
								// if we are processing a product, reach into the summary
								// information and pull out the bits that say if the layout
								// image is supposed to have long file names and is compressed
								if (OutputType.Product == output.Type)
								{
									foreach (Row row in table.Rows)
									{
										// we're looking for the "Word Count" property which happens to
										// be number 15 (and I thought the answer to the universe was 42, heh).
										if ("15" == row[0].ToString())
										{
											output.LongFileNames = (0 == (Convert.ToInt32(row[1]) & 1));
											output.Compressed = (2 == (Convert.ToInt32(row[1]) & 2));

											break; // we're done looking for what we came looking for
										}
									}
								}
								break;
						}

						if (copyRows)
						{
							OutputTable outputTable = Common.EnsureOutputTable(this.activeOutput, this.tableDefinitions[table.Name]);
							this.CopyTableRowsToOutputTable(table, outputTable, section.Id);
						}
					}
				}

				if (0 < ensureTableRows.Count)
				{
					foreach (Row row in ensureTableRows)
					{
						string tableId = (string)row[0];
						TableDefinition tableDef = null;

						try
						{
							tableDef = this.tableDefinitions[tableId];
						}
						catch (WixMissingTableDefinitionException)
						{
							tableDef = customTableDefinitions[tableId];
						}

						Common.EnsureOutputTable(this.activeOutput, tableDef);
					}
				}

				// copy all the suppress action rows to the output to suppress actions from merge modules
				if (0 < suppressActionRows.Count)
				{
					OutputTable suppressActionOutputTable = new OutputTable(this.tableDefinitions["SuppressAction"]);
					this.activeOutput.OutputTables.Add(suppressActionOutputTable);
					foreach (Row suppressActionRow in suppressActionRows)
					{
						suppressActionOutputTable.OutputRows.Add(new OutputRow(suppressActionRow));
					}
				}

				foreach (Action suppressedAction in suppressedStandardActions.Values)
				{
					if (requiredActions.Contains(suppressedAction))
					{
						// We thought they really ought to have a standard action
						// that they wanted to suppress, so warn them and remove it
						this.OnMessage(WixWarnings.SuppressAction(suppressedAction.Id, Action.SequenceTypeToString(suppressedAction.SequenceType)));
						requiredActions.Remove(suppressedAction);
					}
				}

				// check for missing table and add them or display an error as appropriate
				switch (this.activeOutput.Type)
				{
					case OutputType.Module:
						Common.EnsureOutputTable(this.activeOutput, this.tableDefinitions["Component"]);
						Common.EnsureOutputTable(this.activeOutput, this.tableDefinitions["Directory"]);
						Common.EnsureOutputTable(this.activeOutput, this.tableDefinitions["FeatureComponents"]);
						Common.EnsureOutputTable(this.activeOutput, this.tableDefinitions["File"]);
						Common.EnsureOutputTable(this.activeOutput, this.tableDefinitions["ModuleComponents"]);
						Common.EnsureOutputTable(this.activeOutput, this.tableDefinitions["ModuleSignature"]);
						break;
					case OutputType.PatchCreation:
						OutputTable imageFamiliesTable = this.activeOutput.OutputTables["ImageFamilies"];
						OutputTable targetImagesTable = this.activeOutput.OutputTables["TargetImages"];
						OutputTable upgradedImagesTable = this.activeOutput.OutputTables["UpgradedImages"];

						if (null == imageFamiliesTable || 1 > imageFamiliesTable.OutputRows.Count)
						{
							this.OnMessage(WixErrors.ExpectedRowInPatchCreationPackage("ImageFamilies"));
						}

						if (null == targetImagesTable || 1 > targetImagesTable.OutputRows.Count)
						{
							this.OnMessage(WixErrors.ExpectedRowInPatchCreationPackage("TargetImages"));
						}

						if (null == upgradedImagesTable || 1 > upgradedImagesTable.OutputRows.Count)
						{
							this.OnMessage(WixErrors.ExpectedRowInPatchCreationPackage("UpgradedImages"));
						}

						Common.EnsureOutputTable(this.activeOutput, this.tableDefinitions["Properties"]);
						break;
					case OutputType.Product:
						// AdminExecuteSequence Table
						this.AddIfNotSuppressed(requiredActions, suppressedStandardActions, this.standardActions[SequenceType.adminExecute, "CostInitialize"]);
						this.AddIfNotSuppressed(requiredActions, suppressedStandardActions, this.standardActions[SequenceType.adminExecute, "CostInitialize"]);
						this.AddIfNotSuppressed(requiredActions, suppressedStandardActions, this.standardActions[SequenceType.adminExecute, "FileCost"]);
						this.AddIfNotSuppressed(requiredActions, suppressedStandardActions, this.standardActions[SequenceType.adminExecute, "CostFinalize"]);
						this.AddIfNotSuppressed(requiredActions, suppressedStandardActions, this.standardActions[SequenceType.adminExecute, "InstallValidate"]);
						this.AddIfNotSuppressed(requiredActions, suppressedStandardActions, this.standardActions[SequenceType.adminExecute, "InstallInitialize"]);
						this.AddIfNotSuppressed(requiredActions, suppressedStandardActions, this.standardActions[SequenceType.adminExecute, "InstallFiles"]);
						this.AddIfNotSuppressed(requiredActions, suppressedStandardActions, this.standardActions[SequenceType.adminExecute, "InstallAdminPackage"]);
						this.AddIfNotSuppressed(requiredActions, suppressedStandardActions, this.standardActions[SequenceType.adminExecute, "InstallFinalize"]);

						// AdminUISequence Table
						this.AddIfNotSuppressed(requiredActions, suppressedStandardActions, this.standardActions[SequenceType.adminUI, "CostInitialize"]);
						this.AddIfNotSuppressed(requiredActions, suppressedStandardActions, this.standardActions[SequenceType.adminUI, "FileCost"]);
						this.AddIfNotSuppressed(requiredActions, suppressedStandardActions, this.standardActions[SequenceType.adminUI, "CostFinalize"]);
						this.AddIfNotSuppressed(requiredActions, suppressedStandardActions, this.standardActions[SequenceType.adminUI, "ExecuteAction"]);

						// AdvtExecuteSequence Table
						this.AddIfNotSuppressed(requiredActions, suppressedStandardActions, this.standardActions[SequenceType.advertiseExecute, "CostInitialize"]);
						this.AddIfNotSuppressed(requiredActions, suppressedStandardActions, this.standardActions[SequenceType.advertiseExecute, "CostFinalize"]);
						this.AddIfNotSuppressed(requiredActions, suppressedStandardActions, this.standardActions[SequenceType.advertiseExecute, "InstallValidate"]);
						this.AddIfNotSuppressed(requiredActions, suppressedStandardActions, this.standardActions[SequenceType.advertiseExecute, "InstallInitialize"]);
						this.AddIfNotSuppressed(requiredActions, suppressedStandardActions, this.standardActions[SequenceType.advertiseExecute, "PublishFeatures"]);
						this.AddIfNotSuppressed(requiredActions, suppressedStandardActions, this.standardActions[SequenceType.advertiseExecute, "PublishProduct"]);
						this.AddIfNotSuppressed(requiredActions, suppressedStandardActions, this.standardActions[SequenceType.advertiseExecute, "InstallFinalize"]);

						// InstallExecuteSequence Table
						this.AddIfNotSuppressed(requiredActions, suppressedStandardActions, this.standardActions[SequenceType.installExecute, "ValidateProductID"]);
						this.AddIfNotSuppressed(requiredActions, suppressedStandardActions, this.standardActions[SequenceType.installExecute, "CostInitialize"]);
						this.AddIfNotSuppressed(requiredActions, suppressedStandardActions, this.standardActions[SequenceType.installExecute, "FileCost"]);
						this.AddIfNotSuppressed(requiredActions, suppressedStandardActions, this.standardActions[SequenceType.installExecute, "CostFinalize"]);
						this.AddIfNotSuppressed(requiredActions, suppressedStandardActions, this.standardActions[SequenceType.installExecute, "InstallValidate"]);
						this.AddIfNotSuppressed(requiredActions, suppressedStandardActions, this.standardActions[SequenceType.installExecute, "InstallInitialize"]);
						this.AddIfNotSuppressed(requiredActions, suppressedStandardActions, this.standardActions[SequenceType.installExecute, "ProcessComponents"]);
						this.AddIfNotSuppressed(requiredActions, suppressedStandardActions, this.standardActions[SequenceType.installExecute, "UnpublishFeatures"]);
						this.AddIfNotSuppressed(requiredActions, suppressedStandardActions, this.standardActions[SequenceType.installExecute, "CostInitialize"]);
						this.AddIfNotSuppressed(requiredActions, suppressedStandardActions, this.standardActions[SequenceType.installExecute, "RegisterUser"]);
						this.AddIfNotSuppressed(requiredActions, suppressedStandardActions, this.standardActions[SequenceType.installExecute, "RegisterProduct"]);
						this.AddIfNotSuppressed(requiredActions, suppressedStandardActions, this.standardActions[SequenceType.installExecute, "PublishFeatures"]);
						this.AddIfNotSuppressed(requiredActions, suppressedStandardActions, this.standardActions[SequenceType.installExecute, "PublishProduct"]);
						this.AddIfNotSuppressed(requiredActions, suppressedStandardActions, this.standardActions[SequenceType.installExecute, "InstallFinalize"]);

						// InstallUISequence Table
						this.AddIfNotSuppressed(requiredActions, suppressedStandardActions, this.standardActions[SequenceType.installUI, "ValidateProductID"]);
						this.AddIfNotSuppressed(requiredActions, suppressedStandardActions, this.standardActions[SequenceType.installUI, "CostInitialize"]);
						this.AddIfNotSuppressed(requiredActions, suppressedStandardActions, this.standardActions[SequenceType.installUI, "FileCost"]);
						this.AddIfNotSuppressed(requiredActions, suppressedStandardActions, this.standardActions[SequenceType.installUI, "CostFinalize"]);
						this.AddIfNotSuppressed(requiredActions, suppressedStandardActions, this.standardActions[SequenceType.installUI, "ExecuteAction"]);

						Common.EnsureOutputTable(this.activeOutput, this.tableDefinitions["File"]);
						Common.EnsureOutputTable(this.activeOutput, this.tableDefinitions["Media"]);
						break;
				}

				// check for illegal tables
				foreach (OutputTable table in this.activeOutput.OutputTables)
				{
					switch (this.activeOutput.Type)
					{
						case OutputType.Module:
							if ("BBControl" == table.Name ||
								"Billboard" == table.Name ||
								"CCPSearch" == table.Name ||
								"Feature" == table.Name ||
								"LaunchCondition" == table.Name ||
								"Media" == table.Name ||
								"Merge" == table.Name ||
								"Patch" == table.Name ||
								"Upgrade" == table.Name)
							{
								foreach (OutputRow outputRow in table.OutputRows)
								{
									this.OnMessage(WixErrors.UnexpectedTableInMergeModule(outputRow.Row.SourceLineNumbers, table.Name));
								}
							}
							else if ("Error" == table.Name)
							{
								foreach (OutputRow outputRow in table.OutputRows)
								{
									this.OnMessage(WixWarnings.DangerousTableInMergeModule(outputRow.Row.SourceLineNumbers, table.Name));
								}
							}
							break;
						case OutputType.PatchCreation:
							if ("_SummaryInformation" != table.Name &&
								"ExternalFiles" != table.Name &&
								"FamilyFileRanges" != table.Name &&
								"ImageFamilies" != table.Name &&
								"PatchMetadata" != table.Name &&
								"PatchSequence" != table.Name &&
								"Properties" != table.Name &&
								"TargetFiles_OptionalData" != table.Name &&
								"TargetImages" != table.Name &&
								"UpgradedFiles_OptionalData" != table.Name &&
								"UpgradedFilesToIgnore" != table.Name &&
								"UpgradedImages" != table.Name)
							{
								foreach (OutputRow outputRow in table.OutputRows)
								{
									this.OnMessage(WixErrors.UnexpectedTableInPatchCreationPackage(outputRow.Row.SourceLineNumbers, table.Name));
								}
							}
							break;
						case OutputType.Product:
							if ("ModuleAdminExecuteSequence" == table.Name ||
								"ModuleAdminUISequence" == table.Name ||
								"ModuleAdvtExecuteSequence" == table.Name ||
								"ModuleAdvtUISequence" == table.Name ||
								"ModuleComponents" == table.Name ||
								"ModuleConfiguration" == table.Name ||
								"ModuleDependency" == table.Name ||
								"ModuleExclusion" == table.Name ||
								"ModuleIgnoreTable" == table.Name ||
								"ModuleInstallExecuteSequence" == table.Name ||
								"ModuleInstallUISequence" == table.Name ||
								"ModuleSignature" == table.Name ||
								"ModuleSubstitution" == table.Name)
							{
								foreach (OutputRow outputRow in table.OutputRows)
								{
									this.OnMessage(WixWarnings.UnexpectedTableInProduct(outputRow.Row.SourceLineNumbers, table.Name));
								}
							}
							break;
					}
				}

				// add the custom row data
				foreach (Row row in customRows)
				{
					TableDefinition customTable = (TableDefinition)customTableDefinitions[row[0].ToString()];

					string[] data = row[2].ToString().Split(tabCharacter);

					Row customRow = new Row(customTable);
					for (int i = 0; i < data.Length; ++i)
					{
						string[] item = data[i].Split(colonCharacter, 2);
						customRow.SetData(item[0], item[1]);
					}

					bool dataErrors = false;
					for (int i = 0; i < customTable.Columns.Count; ++i)
					{
						if (!customTable.Columns[i].IsNullable && customRow.IsColumnEmpty(i))
						{
							this.OnMessage(WixErrors.NoDataForColumn(row.SourceLineNumbers, customTable.Columns[i].Name, customTable.Name));
							dataErrors = true;
						}
					}

					if (!dataErrors)
					{
						OutputTable outputTable = Common.EnsureOutputTable(this.activeOutput, customTable);
						outputTable.OutputRows.Add(new OutputRow(customRow));
					}
				}

				// update the special properties
				if (0 < adminProperties.Count)
				{
					Row newRow = new Row(this.tableDefinitions["Property"]);
					newRow[0] = "AdminProperties";
					newRow[1] = GetPropertyListString(adminProperties);

					OutputTable outputTable = Common.EnsureOutputTable(this.activeOutput, this.tableDefinitions["Property"]);
					outputTable.OutputRows.Add(new OutputRow(newRow));
				}
				if (0 < secureProperties.Count)
				{
					Row newRow = new Row(this.tableDefinitions["Property"]);
					newRow[0] = "SecureCustomProperties";
					newRow[1] = GetPropertyListString(secureProperties);

					OutputTable outputTable = Common.EnsureOutputTable(this.activeOutput, this.tableDefinitions["Property"]);
					outputTable.OutputRows.Add(new OutputRow(newRow));
				}
				if (0 < hiddenProperties.Count)
				{
					Row newRow = new Row(this.tableDefinitions["Property"]);
					newRow[0] = "MsiHiddenProperties";
					newRow[1] = GetPropertyListString(hiddenProperties);

					OutputTable outputTable = Common.EnsureOutputTable(this.activeOutput, this.tableDefinitions["Property"]);
					outputTable.OutputRows.Add(new OutputRow(newRow));
				}

				if (containsModuleSubstitution)
				{
					Row newRow = new Row(this.tableDefinitions["ModuleIgnoreTable"]);
					newRow[0] = "ModuleSubstitution";

					OutputTable outputTable = Common.EnsureOutputTable(this.activeOutput, this.tableDefinitions["ModuleIgnoreTable"]);
					outputTable.OutputRows.Add(new OutputRow(newRow));
				}

				if (containsModuleConfiguration)
				{
					Row newRow = new Row(this.tableDefinitions["ModuleIgnoreTable"]);
					newRow[0] = "ModuleConfiguration";

					OutputTable outputTable = Common.EnsureOutputTable(this.activeOutput, this.tableDefinitions["ModuleIgnoreTable"]);
					outputTable.OutputRows.Add(new OutputRow(newRow));
				}

				// process the actions
				foreach (Action action in requiredActions)
				{
					// skip actions in suppressed sequences
					if (suppressedSequenceTypes.Contains(action.SequenceType))
					{
						continue;
					}

					if (OutputType.Product == this.activeOutput.Type)
					{
						this.ResolveActionSequence(action, requiredActions);
					}

					TableDefinition sequenceTableDef = null;
					bool module = OutputType.Module == this.activeOutput.Type;
					switch (action.SequenceType)
					{
						case SequenceType.adminExecute:
							if (module)
							{
								Common.EnsureOutputTable(this.activeOutput, this.tableDefinitions["AdminExecuteSequence"]);
								sequenceTableDef = this.tableDefinitions["ModuleAdminExecuteSequence"];
							}
							else
							{
								sequenceTableDef = this.tableDefinitions["AdminExecuteSequence"];
							}
							break;
						case SequenceType.adminUI:
							if (module)
							{
								Common.EnsureOutputTable(this.activeOutput, this.tableDefinitions["AdminUISequence"]);
								sequenceTableDef = this.tableDefinitions["ModuleAdminUISequence"];
							}
							else
							{
								sequenceTableDef = this.tableDefinitions["AdminUISequence"];
							}
							break;
						case SequenceType.advertiseExecute:
							if (module)
							{
								Common.EnsureOutputTable(this.activeOutput, this.tableDefinitions["AdvtExecuteSequence"]);
								sequenceTableDef = this.tableDefinitions["ModuleAdvtExecuteSequence"];
							}
							else
							{
								sequenceTableDef = this.tableDefinitions["AdvtExecuteSequence"];
							}
							break;
						case SequenceType.installExecute:
							if (module)
							{
								Common.EnsureOutputTable(this.activeOutput, this.tableDefinitions["InstallExecuteSequence"]);
								sequenceTableDef = this.tableDefinitions["ModuleInstallExecuteSequence"];
							}
							else
							{
								sequenceTableDef = this.tableDefinitions["InstallExecuteSequence"];
							}
							break;
						case SequenceType.installUI:
							if (module)
							{
								Common.EnsureOutputTable(this.activeOutput, this.tableDefinitions["InstallUISequence"]);
								sequenceTableDef = this.tableDefinitions["ModuleInstallUISequence"];
							}
							else
							{
								sequenceTableDef = this.tableDefinitions["InstallUISequence"];
							}
							break;
					}

					Row row = new Row(sequenceTableDef);
					if (module)
					{
						row[0] = action.Id;
						if (0 != action.SequenceNumber)
						{
							row[1] = action.SequenceNumber;
						}
						else
						{
							bool after = null == action.Before;
							row[2] = after ? action.After : action.Before;
							row[3] = after ? 1 : 0;
						}
						row[4] = action.Condition;
					}
					else // add the row to the sequence table
					{
						row[0] = action.Id;
						row[1] = action.Condition;
						row[2] = action.SequenceNumber;
					}

					OutputTable outputTable = Common.EnsureOutputTable(this.activeOutput, sequenceTableDef);
					outputTable.OutputRows.Add(new OutputRow(row));
				}

				// set the suppressed action sequences
				if (this.suppressAdminSequence)
				{
					this.activeOutput.SuppressAdminSequence = true;
				}
				if (this.suppressAdvertiseSequence)
				{
					this.activeOutput.SuppressAdvertiseSequence = true;
				}
				if (this.suppressUISequence)
				{
					this.activeOutput.SuppressUISequence = true;
				}
			}
			finally
			{
				this.activeOutput = null;
			}

			return (this.foundError ? null : output);
		}

		/// <summary>
		/// Sends an error to the message delegate if there is one.
		/// </summary>
		/// <param name="sourceLineNumbers">Source line numbers.</param>
		/// <param name="errorLevel">Level of the error message.</param>
		/// <param name="errorMessage">Error message string.</param>
		public void OnExtensionError(SourceLineNumberCollection sourceLineNumbers, ErrorLevel errorLevel, string errorMessage)
		{
			this.OnMessage(WixErrors.LinkerExtensionError(sourceLineNumbers, errorLevel, errorMessage));
		}

		/// <summary>
		/// Sends a warning to the message delegate if there is one.
		/// </summary>
		/// <param name="sourceLineNumbers">Source line numbers.</param>
		/// <param name="warningLevel">Level of the warning message.</param>
		/// <param name="warningMessage">Warning message string.</param>
		public void OnExtensionWarning(SourceLineNumberCollection sourceLineNumbers, WarningLevel warningLevel, string warningMessage)
		{
			this.OnMessage(WixWarnings.LinkerExtensionWarning(sourceLineNumbers, warningLevel, warningMessage));
		}

		/// <summary>
		/// Sends an error to the message delegate if there is one.
		/// </summary>
		/// <param name="sourceLineNumbers">Source line numbers.</param>
		/// <param name="verboseLevel">Level of the verbose message.</param>
		/// <param name="verboseMessage">Verbose message string.</param>
		public void OnExtensionVerbose(SourceLineNumberCollection sourceLineNumbers, VerboseLevel verboseLevel, string verboseMessage)
		{
			this.OnMessage(WixVerboses.LinkerExtensionVerbose(sourceLineNumbers, verboseLevel, verboseMessage));
		}

		/// <summary>
		/// Sends a message to the message delegate if there is one.
		/// </summary>
		/// <param name="mea">Message event arguments.</param>
		public void OnMessage(MessageEventArgs mea)
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
		/// Get a sorted property list as a semicolon-delimited string.
		/// </summary>
		/// <param name="properties">SortedList of the properties.</param>
		/// <returns>Semicolon-delimited string representing the property list.</returns>
		private static string GetPropertyListString(SortedList properties)
		{
			bool first = true;
			StringBuilder propertiesString = new StringBuilder();

			foreach (string propertyName in properties.Keys)
			{
				if (first)
				{
					first = false;
				}
				else
				{
					propertiesString.Append(';');
				}
				propertiesString.Append(propertyName);
			}

			return propertiesString.ToString();
		}

		/// <summary>
		/// Loads the standard actions' symbols into the entry section.
		/// </summary>
		/// <param name="allSymbols">Collection of symbols.</param>
		/// <param name="entrySection">Entry section.</param>
		/// <param name="actionTable">Table that contains the standard actions.</param>
		private void LoadStandardActionSymbols(SymbolCollection allSymbols, Section entrySection, ActionTable actionTable)
		{
			foreach (Action action in actionTable)
			{
				// if the action's symbol has not already been defined (i.e. overriden by the user), add it now
				Symbol symbol = action.GetSymbol(entrySection);
				if (!allSymbols.Contains(symbol.Name))
				{
					allSymbols.Add(symbol);
				}
			}
		}

		/// <summary>
		/// Process the complex references.
		/// </summary>
		/// <param name="output">Active output to add sections to.</param>
		/// <param name="sections">Sections that are referenced during the link process.</param>
		/// <param name="referencedSymbols">Collection of all symbols referenced during linking.</param>
		/// <param name="componentsToComponentGroupsComplexReferences">Component to ComponentGroup complex references.</param>
		/// <param name="componentGroupsToFeatures">ComponentGroups to features complex references.</param>
		/// <param name="componentGroupsToModules">ComponentGroups to modules complex references.</param>
		/// <param name="componentsToFeatures">Component to feature complex references.</param>
		/// <param name="featuresToFeatures">Feature to feature complex references.</param>
		private void ProcessComplexReferences(
			Output output,
			SectionCollection sections,
			StringCollection referencedSymbols,
			ComplexReferenceCollection componentsToComponentGroupsComplexReferences,
			ConnectToFeatureCollection componentGroupsToFeatures,
			ConnectToModuleCollection componentGroupsToModules,
			ConnectToFeatureCollection componentsToFeatures,
			ConnectToFeatureCollection featuresToFeatures)
		{
			Hashtable componentsToModules = new Hashtable();

			foreach (Section section in sections)
			{
				foreach (ComplexReference cref in section.ComplexReferences)
				{
					ConnectToFeature connection;
					switch (cref.ParentType)
					{
						case ComplexReferenceParentType.ComponentGroup:
						switch (cref.ChildType)
						{
							case ComplexReferenceChildType.Component:
								componentsToComponentGroupsComplexReferences.Add(cref);
								break;
							default:
								throw new ApplicationException("Unexpected complex reference child type."); // TODO: come up with a real exception to throw (Unexpected complex reference child type)
						}
							break;
						case ComplexReferenceParentType.Feature:
						switch (cref.ChildType)
						{
							case ComplexReferenceChildType.Component:
								connection = componentsToFeatures[cref.ChildId];
								if (null == connection)
								{
									componentsToFeatures.Add(new ConnectToFeature(section, cref.ChildId, cref.ParentId, cref.IsPrimary));
								}
								else if (cref.IsPrimary && connection.IsExplicitPrimaryFeature)
								{
									this.OnMessage(WixErrors.MultiplePrimaryReferences(SourceLineNumberCollection.FromFileName(section.Intermediate.Path), cref.ChildType.ToString(), cref.ChildId, cref.ParentId, connection.PrimaryFeature));
									continue;
								}
								else if (cref.IsPrimary)
								{
									connection.ConnectFeatures.Add(connection.PrimaryFeature); // move the guessed primary feature to the list of connects
									connection.PrimaryFeature = cref.ParentId; // set the new primary feature
									connection.IsExplicitPrimaryFeature = true; // and make sure we remember that we set it so we can fail if we try to set it again
								}
								else
								{
									connection.ConnectFeatures.Add(cref.ParentId);
								}

								// add a row to the FeatureComponents table
								Row row = Common.CreateRowInSection(null, section, this.tableDefinitions["FeatureComponents"]);
								if (this.sectionIdOnTuples)
								{
									row.SectionId = section.Id;
								}
								row[0] = cref.ParentId;
								row[1] = cref.ChildId;

								// index the component for finding orphaned records
								string symbolName = String.Concat("Component:", cref.ChildId);
								if (!referencedSymbols.Contains(symbolName))
								{
									referencedSymbols.Add(symbolName);
								}

								break;
							case ComplexReferenceChildType.ComponentGroup:
								connection = componentGroupsToFeatures[cref.ChildId];
								if (null == connection)
								{
									componentGroupsToFeatures.Add(new ConnectToFeature(section, cref.ChildId, cref.ParentId, cref.IsPrimary));
								}
								else if (cref.IsPrimary && connection.IsExplicitPrimaryFeature)
								{
									this.OnMessage(WixErrors.MultiplePrimaryReferences(SourceLineNumberCollection.FromFileName(section.Intermediate.Path), cref.ChildType.ToString(), cref.ChildId, cref.ParentId, connection.PrimaryFeature));
									continue;
								}
								else if (cref.IsPrimary)
								{
									connection.ConnectFeatures.Add(connection.PrimaryFeature);
									connection.PrimaryFeature = cref.ParentId;
									connection.IsExplicitPrimaryFeature = true;
								}
								else
								{
									connection.ConnectFeatures.Add(cref.ParentId);
								}
								break;

							case ComplexReferenceChildType.Feature:
								connection = featuresToFeatures[cref.ChildId];
								if (null != connection)
								{
									this.OnMessage(WixErrors.MultiplePrimaryReferences(SourceLineNumberCollection.FromFileName(section.Intermediate.Path), cref.ChildType.ToString(), cref.ChildId, cref.ParentId, connection.PrimaryFeature));
									continue;
								}

								featuresToFeatures.Add(new ConnectToFeature(section, cref.ChildId, cref.ParentId, cref.IsPrimary));
								break;

							case ComplexReferenceChildType.Module:
								connection = output.ModulesToFeatures[cref.ChildId];
								if (null == connection)
								{
									output.ModulesToFeatures.Add(new ConnectToFeature(section, cref.ChildId, cref.ParentId, cref.IsPrimary));
								}
								else if (cref.IsPrimary && connection.IsExplicitPrimaryFeature)
								{
									this.OnMessage(WixErrors.MultiplePrimaryReferences(SourceLineNumberCollection.FromFileName(section.Intermediate.Path), cref.ChildType.ToString(), cref.ChildId, cref.ParentId, connection.PrimaryFeature));
									continue;
								}
								else if (cref.IsPrimary)
								{
									connection.ConnectFeatures.Add(connection.PrimaryFeature); // move the guessed primary feature to the list of connects
									connection.PrimaryFeature = cref.ParentId; // set the new primary feature
									connection.IsExplicitPrimaryFeature = true; // and make sure we remember that we set it so we can fail if we try to set it again
								}
								else
								{
									connection.ConnectFeatures.Add(cref.ParentId);
								}
								break;

							default:
								throw new ApplicationException("Unexpected complex reference child type"); // TODO: come up with a real exception to throw (Unexpected complex reference child type)
						}
							break;

						case ComplexReferenceParentType.Module:
						switch (cref.ChildType)
						{
							case ComplexReferenceChildType.Component:
								if (componentsToModules.ContainsKey(cref.ChildId))
								{
									this.OnMessage(WixErrors.ComponentReferencedTwice(SourceLineNumberCollection.FromFileName(section.Intermediate.Path), cref.ChildId));
									continue;
								}
								else
								{
									componentsToModules.Add(cref.ChildId, cref); // should always be new

									// add a row to the ModuleComponents table
									Row row = Common.CreateRowInSection(null, section, this.tableDefinitions["ModuleComponents"]);
									if (this.sectionIdOnTuples)
									{
										row.SectionId = section.Id;
									}
									row[0] = cref.ChildId;
									row[1] = cref.ParentId;
									row[2] = cref.ParentLanguage;
								}

								// index the component for finding orphaned records
								string componentSymbolName = String.Concat("Component:", cref.ChildId);
								if (!referencedSymbols.Contains(componentSymbolName))
								{
									referencedSymbols.Add(componentSymbolName);
								}

								break;

							case ComplexReferenceChildType.ComponentGroup:
								ConnectToModule moduleConnection = componentGroupsToModules[cref.ChildId];
								if (null == moduleConnection)
								{
									componentGroupsToModules.Add(new ConnectToModule(cref.ChildId, cref.ParentId, cref.ParentLanguage));
								}
								break;

							default:
								throw new ApplicationException("Unexpected complex reference child type"); // TODO: come up with a real exception to throw (Unexpected complex reference child type)
						}
							break;
					}
				}
			}
		}

		/// <summary>
		/// Find orphaned records (like Component entries without a parent Feature).
		/// </summary>
		/// <param name="referencedSymbols">Collection of all symbols referenced during linking.</param>
		private void FindOrphanedSymbols(StringCollection referencedSymbols)
		{
			// look in all sections
			foreach (Section section in this.activeOutput.Sections)
			{
				// look at all symbols in each section
				foreach (Symbol symbol in section.GetSymbols(this))
				{
					// look at symbols that were not pulled in with a reference
					if (!referencedSymbols.Contains(symbol.Name))
					{
						// display a warning message for Components with no parent feature
						if (null != symbol.Row && "Component" == symbol.Row.Table.Name)
						{
							this.OnMessage(WixWarnings.OrphanedComponent(symbol.Row.SourceLineNumbers, WarningLevel.Major, symbol.RowId));
						}
					}
				}
			}
		}

		/// <summary>
		/// Resolves an action's sequence.
		/// </summary>
		/// <param name="action">Action to resolve sequence for.</param>
		/// <param name="actions">Collection of all actions.</param>
		/// <remarks>Note: recursive function.</remarks>
		private void ResolveActionSequence(Action action, ActionTable actions)
		{
			// if we've already visited this action then there must be a loop in the action
			// sequence which would cause us to go into an infinite loop (until we blew the
			// stack) so just throw an exception instead.
			if (Linker.VisitedActionSentinel == action.SequenceNumber)
			{
				throw new WixRecursiveActionException(null, action.Id, action.SequenceType.ToString()); // TODO: pass the source file for this action instead of "null"
			}

			// if the action hasn't been resolved already
			if (!action.Resolved)
			{
				// if we have no before/after and there is a sequence number then mark the action as resolved
				if (null == action.Before && null == action.After)
				{
					action.Resolved = 0 != action.SequenceNumber;
				}
			}

			// if we already have a sequence number for this action, go back
			if (action.Resolved)
			{
				Debug.Assert(Linker.VisitedActionSentinel != action.SequenceNumber, "visited action sentinel found unexpectedly.");
				return;
			}

			action.SequenceNumber = Linker.VisitedActionSentinel; // mark this action as visited to prevent infinite recursive loops

			bool afterParent = null == action.Before;
			string parentActionName = afterParent ? action.After : action.Before;
			Action parentAction = actions[action.SequenceType, parentActionName];

			// can this actually happen if we've already done all the symbol/reference lookups?
			if (null == parentAction)
			{
				throw new WixMissingActionException(null, parentActionName, action.Id); // TODO: pass the source file for this action instead of "null"
			}

			// ensure the parent action has its sequence number resolved
			this.ResolveActionSequence(parentAction, actions);

			if (0 > parentAction.SequenceNumber)
			{
				this.OnMessage(WixErrors.ActionScheduledRelativeToTerminationAction(action.SequenceType.ToString(), action.Id, parentActionName));
			}

			action.SequenceNumber = afterParent ? parentAction.SequenceNumber + 1 : parentAction.SequenceNumber - 1;
			action.Resolved = true;
		}

		/// <summary>
		/// Resolve component groups.
		/// </summary>
		/// <param name="output">Active output to add sections to.</param>
		/// <param name="referencedSymbols">Collection of all symbols referenced during linking.</param>
		/// <param name="componentsToComponentGroupsComplexReferences">Component to ComponentGroup complex references.</param>
		/// <param name="componentGroupsToFeatures">ComponentGroups to features complex references.</param>
		/// <param name="componentGroupsToModules">ComponentGroups to modules complex references.</param>
		/// <param name="componentsToFeatures">Component to feature complex references.</param>
		private void ResolveComponentGroups(
			Output output,
			StringCollection referencedSymbols,
			ComplexReferenceCollection componentsToComponentGroupsComplexReferences,
			ConnectToFeatureCollection componentGroupsToFeatures,
			ConnectToModuleCollection componentGroupsToModules,
			ConnectToFeatureCollection componentsToFeatures)
		{
			foreach (ComplexReference cref in componentsToComponentGroupsComplexReferences)
			{
				// only connect a Component to a Feature if the ComponentGroup is connected to a Feature
				ConnectToFeature connectComponentGroupToFeature = componentGroupsToFeatures[cref.ParentId];
				if (null != connectComponentGroupToFeature)
				{
					// create a list of all features (use an ArrayList because StringCollection lacks an AddRange method that takes an ICollection)
					ArrayList features = new ArrayList(connectComponentGroupToFeature.ConnectFeatures);
					features.Add(connectComponentGroupToFeature.PrimaryFeature);

					foreach (string feature in features)
					{
						ConnectToFeature connectComponentToFeature = componentsToFeatures[cref.ChildId];
						bool isExplicitPrimaryFeature = (connectComponentGroupToFeature.IsExplicitPrimaryFeature && (connectComponentGroupToFeature.PrimaryFeature == feature));

						if (null == connectComponentToFeature)
						{
							componentsToFeatures.Add(new ConnectToFeature(connectComponentGroupToFeature.Section, cref.ChildId, feature, isExplicitPrimaryFeature));
						}
						else if (isExplicitPrimaryFeature && connectComponentToFeature.IsExplicitPrimaryFeature && feature != connectComponentToFeature.PrimaryFeature)
						{
							this.OnMessage(WixErrors.MultiplePrimaryReferences(SourceLineNumberCollection.FromFileName(connectComponentToFeature.Section.Intermediate.Path), cref.ChildType.ToString(), cref.ChildId, cref.ParentId, feature));
							continue;
						}
						else if (isExplicitPrimaryFeature)
						{
							connectComponentToFeature.ConnectFeatures.Add(connectComponentToFeature.PrimaryFeature); // move the guessed primary feature to the list of connects
							connectComponentToFeature.PrimaryFeature = feature; // set the new primary feature
							connectComponentToFeature.IsExplicitPrimaryFeature = true; // and make sure we remember that we set it so we can fail if we try to set it again
						}
						else
						{
							connectComponentToFeature.ConnectFeatures.Add(feature);
						}

						// add a row to the FeatureComponents table
						Row row = Common.CreateRowInSection(null, output.EntrySection, this.tableDefinitions["FeatureComponents"]);
						row[0] = feature;
						row[1] = cref.ChildId;

						// index the component for finding orphaned records
						Reference reference = new Reference("Component", cref.ChildId);
						if (!referencedSymbols.Contains(reference.SymbolicName))
						{
							referencedSymbols.Add(reference.SymbolicName);
						}
					}
				}

				// only connect a Component to a Module if the ComponentGroup is connected to a Module
				ConnectToModule connectComponentGroupToModule = componentGroupsToModules[cref.ParentId];
				if (null != connectComponentGroupToModule)
				{
					// add a row to the ModuleComponents table
					Row row = Common.CreateRowInSection(null, output.EntrySection, this.tableDefinitions["ModuleComponents"]);
					row[0] = cref.ChildId;
					row[1] = connectComponentGroupToModule.Module;
					row[2] = connectComponentGroupToModule.ModuleLanguage;

					// index the component for finding orphaned records
					Reference reference = new Reference("Component", cref.ChildId);
					if (!referencedSymbols.Contains(reference.SymbolicName))
					{
						referencedSymbols.Add(reference.SymbolicName);
					}
				}
			}
		}

		/// <summary>
		/// Resolve the feature backlinks to the final feature that a component will live in.
		/// </summary>
		/// <param name="output">Active output to add sections to.</param>
		/// <param name="componentsToFeatures">Component to feature complex references.</param>
		/// <param name="allSymbols">All symbols loaded from the intermediates.</param>
		/// <param name="referencedSymbols">Collection of all symbols referenced during linking.</param>
		/// <param name="unresolvedReferences">Unresolved references.</param>
		private void ResolveFeatureBacklinks(
			Output output,
			ConnectToFeatureCollection componentsToFeatures,
			SymbolCollection allSymbols,
			StringCollection referencedSymbols,
			ArrayList unresolvedReferences)
		{
			Hashtable uniqueComponentIds = new Hashtable();

			foreach (Section section in output.Sections)
			{
				foreach (FeatureBacklink blink in section.FeatureBacklinks)
				{
					Reference reference = blink.Reference;
					Symbol symbol = Common.GetSymbolForReference(section, reference, allSymbols, referencedSymbols, unresolvedReferences, this);
					if (null == symbol)
					{
						continue;
					}

					Row row = symbol.Row;
					string parentFeature;
					if (OutputType.Module == output.Type)
					{
						parentFeature = Guid.Empty.ToString("B");
					}
					else
					{
						ConnectToFeature connection = componentsToFeatures[blink.Component];
						if (null == connection)
						{
							throw new WixMissingFeatureException(SourceLineNumberCollection.FromFileName(section.Intermediate.Path), blink);
						}

						parentFeature = connection.PrimaryFeature;

						// check for unique, implicit, primary feature parents with multiple possible parent features
						if (PedanticLevel.Legendary == this.pedanticLevel &&
							!connection.IsExplicitPrimaryFeature &&
							0 < connection.ConnectFeatures.Count &&
							!uniqueComponentIds.Contains(blink.Component))
						{
							this.OnMessage(WixWarnings.ImplicitPrimaryFeature(blink.Component));

							// remember this component so only one warning is generated for it
							uniqueComponentIds[blink.Component] = null;
						}
					}

					switch (blink.Type)
					{
						case FeatureBacklinkType.Class:
							row[11] = parentFeature;
							break;
						case FeatureBacklinkType.Extension:
							row[4] = parentFeature;
							break;
						case FeatureBacklinkType.PublishComponent:
							row[4] = parentFeature;
							break;
						case FeatureBacklinkType.Shortcut:
							row[4] = parentFeature;
							break;
						case FeatureBacklinkType.TypeLib:
							row[6] = parentFeature;
							break;
						default:
							throw new ApplicationException("Internal Error: Unknown FeatureBackLinkType.");
					}
				}
			}
		}

		/// <summary>
		/// Resolves the features connected to other features in the active output.
		/// </summary>
		/// <param name="featuresToFeatures">Feature to feature complex references.</param>
		/// <param name="allSymbols">All symbols loaded from the intermediates.</param>
		/// <param name="referencedSymbols">Collection of all symbols referenced during linking.</param>
		/// <param name="unresolvedReferences">Unresolved references.</param>
		private void ResolveFeatureToFeatureConnects(ConnectToFeatureCollection featuresToFeatures, SymbolCollection allSymbols, StringCollection referencedSymbols, ArrayList unresolvedReferences)
		{
			foreach (ConnectToFeature connection in featuresToFeatures)
			{
				Reference reference = new Reference("Feature", connection.ChildId);
				Symbol symbol = Common.GetSymbolForReference(connection.Section, reference, allSymbols, referencedSymbols, unresolvedReferences, this);
				if (null == symbol)
				{
					continue;
				}

				Row row = symbol.Row;
				row[1] = connection.PrimaryFeature;
			}
		}

		/// <summary>
		/// Checks to see if an Action is in the suppressed standard action
		/// list. If not, adds the action to the ActionTable.
		/// </summary>
		/// <param name="requiredActions">Table of required actions.</param>
		/// <param name="suppressedActions">Hashtable of suppressed actions.</param>
		/// <param name="action">Action to lookup and conditionally add.</param>
		private void AddIfNotSuppressed(ActionTable requiredActions, Hashtable suppressedActions, Action action)
		{
			string key = String.Concat(action.SequenceType.ToString(), action.Id);
			if (!suppressedActions.Contains(key))
			{
				requiredActions.Add(action);
			}
		}

		/// <summary>
		/// Copies a table's rows to an output table.
		/// </summary>
		/// <param name="table">Source table to copy rows from.</param>
		/// <param name="outputTable">Destination output table to copy rows into.</param>
		/// <param name="sectionId">Id of the section that the table lives in.</param>
		private void CopyTableRowsToOutputTable(Table table, OutputTable outputTable, string sectionId)
		{
			int[] localizedColumns = new int[table.Columns.Count];
			int localizedColumnCount = 0;

			// if there are localization strings, figure out which columns can be localized in this table
			if (null != this.localizer)
			{
				for (int i = 0; i < table.Columns.Count; i++)
				{
					if (table.Columns[i].IsLocalizable)
					{
						localizedColumns[localizedColumnCount++] = i;
					}
				}
			}

			// process each row in the table doing the string resource substitutions
			// then add the row to the output
			foreach (Row row in table.Rows)
			{
				if (row.IsUnreal)
				{
					continue;
				}

				// localize all the values
				for (int i = 0; i < localizedColumnCount; i++)
				{
					object val = row[localizedColumns[i]];

					if (null != val)
					{
						row[localizedColumns[i]] = this.localizer.GetLocalizedValue(val.ToString());
					}
				}

				outputTable.OutputRows.Add(new OutputRow(row, this.sectionIdOnTuples ? sectionId : null));
			}
		}
	}
}
