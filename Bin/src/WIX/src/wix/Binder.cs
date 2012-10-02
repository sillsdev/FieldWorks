//-------------------------------------------------------------------------------------------------
// <copyright file="Binder.cs" company="Microsoft">
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
	using System.CodeDom.Compiler;
	using System.Collections;
	using System.Collections.Specialized;
	using System.Diagnostics;
	using System.Globalization;
	using System.IO;
	using System.Reflection;
	using System.Runtime.InteropServices;
	using System.Text;
	using System.Xml;
	using System.Xml.XPath;
	using Microsoft.Tools.WindowsInstallerXml.Cab;
	using Microsoft.Tools.WindowsInstallerXml.CLR.Interop;
	using Microsoft.Tools.WindowsInstallerXml.Msi;
	using Microsoft.Tools.WindowsInstallerXml.Msi.Interop;

	/// <summary>
	/// Delegate which handles file copying.
	/// </summary>
	/// <param name="source">The file to copy.</param>
	/// <param name="destination">The destination file.</param>
	/// <param name="overwrite">true if the destination file can be overwritten; otherwise, false.</param>
	public delegate void FileCopyHandler(string source, string destination, bool overwrite);

	/// <summary>
	/// Delegate which handles file moving.
	/// </summary>
	/// <param name="source">The file to move.</param>
	/// <param name="destination">The destination file.</param>
	public delegate void FileMoveHandler(string source, string destination);

	/// <summary>
	/// Used to specify the type of file being searched for when FileResolutionHandler
	/// is called.
	/// </summary>
	public enum FileResolutionType
	{
		/// <summary>
		/// Unknown file type.
		/// </summary>
		Unknown,
		/// <summary>
		/// Normal file.
		/// </summary>
		File,
		/// <summary>
		/// Normal directory.
		/// </summary>
		Directory,
		/// <summary>
		/// File to be imported to the Binary table.
		/// </summary>
		Binary,
		/// <summary>
		/// File to be imported to the Icon table.
		/// </summary>
		Icon,
		/// <summary>
		/// Merge Module to be imported.
		/// </summary>
		Module,
		/// <summary>
		/// File to be imprted to the MsiDigitalCertificate table.
		/// </summary>
		DigitalCertificate
	}

	/// <summary>
	/// Linker core of the Windows Installer Xml toolset.
	/// </summary>
	public class Binder : IExtensionMessageHandler, IMessageHandler
	{
		private const int VisitedActionSentinel = -10;
		private static readonly char[] tabCharacter = "\t".ToCharArray();
		private static readonly char[] colonCharacter = ":".ToCharArray();

		private TableDefinitionCollection tableDefinitions;

		private BinderExtension extension;
		private ExtensionMessages extensionMessages;
		private FileCopyHandler fileCopyHandler;
		private FileMoveHandler fileMoveHandler;
		private bool foundError;
		private string imagebaseOutputPath;
		private Localizer localizer;
		private bool setMsiAssemblyNameFileVersion;
		private bool suppressAclReset;
		private bool suppressAssemblies;
		private bool suppressFileHashAndInfo;
		private bool suppressLayout;
		private TempFileCollection tempFiles;

		/// <summary>
		/// Creates a linker.
		/// </summary>
		/// <param name="smallTables">Use small table definitions for MSI/MSM.</param>
		public Binder(bool smallTables)
		{
			this.tableDefinitions = Common.GetTableDefinitions(smallTables);

			this.extension = new BinderExtension();
			this.extensionMessages = new ExtensionMessages(this);
			this.fileCopyHandler = new FileCopyHandler(File.Copy);
			this.fileMoveHandler = new FileMoveHandler(File.Move);
		}

		/// <summary>
		/// Event for messages.
		/// </summary>
		public event MessageEventHandler Message;

		/// <summary>
		/// Gets or sets the binder extension class.
		/// </summary>
		/// <value>The binder extension class.</value>
		public BinderExtension Extension
		{
			get { return this.extension; }
			set { this.extension = value; }
		}

		/// <summary>
		/// Gets or sets the FileCopyHandler delegate used for copying files.
		/// </summary>
		/// <value>The FileCopyHandler delegate used for copying files.</value>
		public FileCopyHandler FileCopyHandler
		{
			get { return this.fileCopyHandler; }
			set { this.fileCopyHandler = value; }
		}

		/// <summary>
		/// Gets or sets the FileMoveHandler delegate used for moving files.
		/// </summary>
		/// <value>The FileMoveHandler delegate used for moving files.</value>
		public FileMoveHandler FileMoveHandler
		{
			get { return this.fileMoveHandler; }
			set { this.fileMoveHandler = value; }
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
		/// Gets or sets the localizer.
		/// </summary>
		/// <value>The localizer.</value>
		public Localizer Localizer
		{
			get { return this.localizer; }
			set { this.localizer = value; }
		}

		/// <summary>
		/// Gets and sets the option to set the file version in the MsiAssemblyName table.
		/// </summary>
		/// <value>The option to set the file version in the MsiAssemblyName table.</value>
		public bool SetMsiAssemblyNameFileVersion
		{
			get { return this.setMsiAssemblyNameFileVersion; }
			set { this.setMsiAssemblyNameFileVersion = value; }
		}

		/// <summary>
		/// Gets and sets the option to suppress resetting ACLs by the binder.
		/// </summary>
		/// <value>The option to suppress resetting ACLs by the binder.</value>
		public bool SuppressAclReset
		{
			get { return this.suppressAclReset; }
			set { this.suppressAclReset = value; }
		}

		/// <summary>
		/// Gets and sets the option to suppress grabbing assembly name information from assemblies.
		/// </summary>
		/// <value>The option to suppress grabbing assembly name information from assemblies.</value>
		public bool SuppressAssemblies
		{
			get { return this.suppressAssemblies; }
			set { this.suppressAssemblies = value; }
		}

		/// <summary>
		/// Gets and sets the option to suppress grabbing the file hash, version and language at link time.
		/// </summary>
		/// <value>The option to suppress grabbing the file hash, version and language.</value>
		public bool SuppressFileHashAndInfo
		{
			get { return this.suppressFileHashAndInfo; }
			set { this.suppressFileHashAndInfo = value; }
		}

		/// <summary>
		/// Gets and sets the option to suppress creating an image for MSI/MSM.
		/// </summary>
		/// <value>The option to suppress creating an image for MSI/MSM.</value>
		public bool SuppressLayout
		{
			get { return this.suppressLayout; }
			set { this.suppressLayout = value; }
		}

		/// <summary>
		/// Gets the table definitions used by the Binder.
		/// </summary>
		/// <value>Table definitions used by the linker.</value>
		public TableDefinitionCollection TableDefinitions
		{
			get { return this.tableDefinitions; }
		}

		/// <summary>
		/// Gets or sets the temporary path for the Binder.  If left null, the binder
		/// will use %TEMP% environment variable.
		/// </summary>
		/// <value>Path to temp files.</value>
		public string TempFilesLocation
		{
			get { return null == this.tempFiles ? String.Empty : this.tempFiles.BasePath; }
			set
			{
				if (null == value)
				{
					this.tempFiles = new TempFileCollection();
				}
				else
				{
					this.tempFiles = new TempFileCollection(value);
				}
			}
		}

		/// <summary>
		/// Cleans up the temp files used by the Binder.
		/// </summary>
		/// <returns>True if all files were deleted, false otherwise.</returns>
		public bool DeleteTempFiles()
		{
			if (null == this.tempFiles)
			{
				return true; // no work to do
			}

			// try three times and give up with a warning if the temp files aren't gone by then
			const int retryLimit = 3;
			for (int i = 0; i < retryLimit; i++)
			{
				try
				{
					Directory.Delete(this.tempFiles.BasePath, true);   // toast the whole temp directory
					break; // no exception means we got success the first time
				}
				catch (UnauthorizedAccessException)
				{
					if (0 == i) // should only need to unmark readonly once - there's no point in doing it again and again
					{
						RecursiveFileAttributes(this.tempFiles.BasePath, FileAttributes.ReadOnly, false); // toasting will fail if any files are read-only. Try changing them to not be.
					}
					else
					{
						this.OnMessage(WixWarnings.AccessDeniedForDeletion(null, WarningLevel.Minor, this.tempFiles.BasePath));
						return false;
					}
				}
				catch (DirectoryNotFoundException)
				{
					// if the path doesn't exist, then there is nothing for us to worry about
					break;
				}
				catch (IOException) // directory in use
				{
					if (i == (retryLimit - 1)) // last try failed still, give up
					{
						this.OnMessage(WixWarnings.DirectoryInUse(null, WarningLevel.Minor, this.tempFiles.BasePath));
						return false;
					}
					System.Threading.Thread.Sleep(300);  // sleep a bit before trying again
				}
			}

			this.tempFiles = null; // temp files have been deleted, no need to remember this now
			return true;
		}

		/// <summary>
		/// Binds an output.
		/// </summary>
		/// <param name="output">Path output should be written to eventually.</param>
		/// <remarks>The Binder.DeleteTempFiles method should be called after calling this method</remarks>
		/// <returns>true if binding completed successfully; false otherwise</returns>
		public bool Bind(Output output)
		{
			string databasePath;
			ArrayList fileTransfers = new ArrayList();

			this.foundError = false;
			this.extension.Messages = this.extensionMessages;

			// if we don't have the temporary files object yet, get one
			if (null == this.tempFiles)
			{
				this.tempFiles = new TempFileCollection();
			}
			Directory.CreateDirectory(this.tempFiles.BasePath); // ensure the base path is there

			// process tables as necessary
			foreach (OutputTable outputTable in output.OutputTables)
			{
				switch (outputTable.Name)
				{
					case "Property": // update the ProductCode property as necessary
						foreach (OutputRow propertyOutputRow in outputTable.OutputRows)
						{
							if ("ProductCode" == propertyOutputRow.Row[0].ToString() && "{????????-????-????-????-????????????}" == propertyOutputRow.Row[1].ToString())
							{
								propertyOutputRow.Row[1] = GenerateGuid();
							}
						}
						break;
				}
			}

			this.OnMessage(WixVerboses.UpdatingFileInformation());
			this.UpdateFileInformation(output);
			this.UpdateControlText(output);

			// add back suppressed tables which must be present prior to merging in modules
			if (OutputType.Product == output.Type)
			{
				OutputTable mergeTable = output.OutputTables["Merge"];

				if (null != mergeTable && 0 < mergeTable.OutputRows.Count)
				{
					if (output.SuppressAdminSequence)
					{
						Common.EnsureOutputTable(output, this.tableDefinitions["AdminUISequence"]);
						Common.EnsureOutputTable(output, this.tableDefinitions["AdminExecuteSequence"]);
					}

					if (output.SuppressAdvertiseSequence)
					{
						Common.EnsureOutputTable(output, this.tableDefinitions["AdvtExecuteSequence"]);
					}

					if (output.SuppressUISequence)
					{
						Common.EnsureOutputTable(output, this.tableDefinitions["AdminUISequence"]);
						Common.EnsureOutputTable(output, this.tableDefinitions["InstallUISequence"]);
					}
				}
			}

			this.OnMessage(WixVerboses.GeneratingDatabase());
			databasePath = this.GenerateDatabase(output);
			fileTransfers.Add(new FileTransfer(databasePath, output.Path, true)); // note where this database needs to move in the future

			if (OutputType.Product == output.Type)
			{
				this.OnMessage(WixVerboses.MergingModules());
				this.MergeModules(databasePath, output);
			}
			if (!this.suppressLayout || OutputType.Module == output.Type)
			{
				this.OnMessage(WixVerboses.ProcessingMediaInformation());
				this.ProcessMediaInformation(databasePath, output, fileTransfers);
			}

			this.OnMessage(WixVerboses.ImportingStreams());
			this.ImportStreams(databasePath, output);
			this.OnMessage(WixVerboses.LayingOutMedia());
			this.LayoutMedia(fileTransfers);

			return !this.foundError;
		}

		/// <summary>
		/// Sends an error to the message delegate if there is one.
		/// </summary>
		/// <param name="sourceLineNumbers">Source line numbers.</param>
		/// <param name="errorLevel">Level of the error message.</param>
		/// <param name="errorMessage">Error message string.</param>
		public void OnExtensionError(SourceLineNumberCollection sourceLineNumbers, ErrorLevel errorLevel, string errorMessage)
		{
			this.OnMessage(WixErrors.BinderExtensionError(sourceLineNumbers, errorLevel, errorMessage));
		}

		/// <summary>
		/// Sends a warning to the message delegate if there is one.
		/// </summary>
		/// <param name="sourceLineNumbers">Source line numbers.</param>
		/// <param name="warningLevel">Level of the warning message.</param>
		/// <param name="warningMessage">Warning message string.</param>
		public void OnExtensionWarning(SourceLineNumberCollection sourceLineNumbers, WarningLevel warningLevel, string warningMessage)
		{
			this.OnMessage(WixWarnings.BinderExtensionWarning(sourceLineNumbers, warningLevel, warningMessage));
		}

		/// <summary>
		/// Sends an error to the message delegate if there is one.
		/// </summary>
		/// <param name="sourceLineNumbers">Source line numbers.</param>
		/// <param name="verboseLevel">Level of the verbose message.</param>
		/// <param name="verboseMessage">Verbose message string.</param>
		public void OnExtensionVerbose(SourceLineNumberCollection sourceLineNumbers, VerboseLevel verboseLevel, string verboseMessage)
		{
			this.OnMessage(WixVerboses.BinderExtensionVerbose(sourceLineNumbers, verboseLevel, verboseMessage));
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
		/// Generate a new Windows Installer-friendly guid.
		/// </summary>
		/// <returns>A new guid.</returns>
		private static string GenerateGuid()
		{
			return String.Concat("{", Guid.NewGuid().ToString().ToUpper(CultureInfo.InvariantCulture), "}");
		}

		/// <summary>
		/// Get the source name for a file or directory.
		/// </summary>
		/// <param name="names">Source names for the file or directory.</param>
		/// <param name="longNamesInImage">Flag if to resolve to long names.</param>
		/// <returns>Source name for a file or directory.</returns>
		private static string GetSourceName(string names, bool longNamesInImage)
		{
			string source;

			// get source
			int sourceTargetSeparator = names.IndexOf(":");
			if (0 <= sourceTargetSeparator)
			{
				source = names.Substring(sourceTargetSeparator + 1);
			}
			else
			{
				source = names;
			}

			// get the short and long source names
			int sourceSeparator = source.IndexOf("|");
			if (0 <= sourceSeparator)
			{
				if (longNamesInImage)
				{
					return source.Substring(sourceSeparator + 1);
				}
				else
				{
					return source.Substring(0, sourceSeparator);
				}
			}
			else
			{
				return source;
			}
		}

		/// <summary>
		/// Get the source path of a directory.
		/// </summary>
		/// <param name="directories">All cached directories.</param>
		/// <param name="directory">Directory identifier.</param>
		/// <param name="longNamesInImage">Flag if to resolve to long names.</param>
		/// <returns>Source path of a directory.</returns>
		private static string GetDirectoryPath(Hashtable directories, string directory, bool longNamesInImage)
		{
			if (!directories.Contains(directory))
			{
				throw new WixMissingDirectoryException(null, directory);
			}
			ResolvedDirectory resolvedDirectory = (ResolvedDirectory)directories[directory];

			if (null == resolvedDirectory.sourcePath)
			{
				if (0 == resolvedDirectory.directoryParent.Length)
				{
					// base case: this is a directory with no parent, so return the defaultdir
					resolvedDirectory.sourcePath = resolvedDirectory.sourceName;
				}
				else
				{
					resolvedDirectory.sourcePath = Path.Combine(GetDirectoryPath(directories, resolvedDirectory.directoryParent, longNamesInImage), resolvedDirectory.sourceName);
				}
			}

			return resolvedDirectory.sourcePath;
		}

		/// <summary>
		/// Recursively loops through a directory, changing an attribute on all of the underlying files.
		/// An example is to add/remove the ReadOnly flag from each file.
		/// </summary>
		/// <param name="path">The directory path to start deleting from.</param>
		/// <param name="fileAttribute">The FileAttribute to change on each file.</param>
		/// <param name="markAttribute">If true, add the attribute to each file. If false, remove it.</param>
		private static void RecursiveFileAttributes(string path, FileAttributes fileAttribute, bool markAttribute)
		{
			foreach (string subDirectory in Directory.GetDirectories(path))
			{
				RecursiveFileAttributes(subDirectory, fileAttribute, markAttribute);
			}

			foreach (string filePath in Directory.GetFiles(path))
			{
				FileAttributes attributes = File.GetAttributes(filePath);
				if (markAttribute)
				{
					attributes = attributes | fileAttribute; // add to list of attributes
				}
				else if (fileAttribute == (attributes & fileAttribute)) // if attribute set
				{
					attributes = attributes ^ fileAttribute; // remove from list of attributes
				}
				File.SetAttributes(filePath, attributes);
			}
		}

		/// <summary>
		/// Set an MsiAssemblyName row.  If it was directly authored, override the value, otherwise
		/// create a new row.
		/// </summary>
		/// <param name="assemblyNameOutputTable">MsiAssemblyName output table.</param>
		/// <param name="fileRow">FileRow containing the assembly read for the MsiAssemblyName row.</param>
		/// <param name="name">MsiAssemblyName name.</param>
		/// <param name="value">MsiAssemblyName value.</param>
		private void SetMsiAssemblyName(OutputTable assemblyNameOutputTable, FileRow fileRow, string name, string value)
		{
			// check for null value (this can occur when grabbing the file version from an assembly without one)
			if (null == value || 0 == value.Length)
			{
				this.OnMessage(WixErrors.NullMsiAssemblyNameValue(fileRow.SourceLineNumbers, fileRow.Component, name));
			}

			// override directly authored value
			foreach (OutputRow outputRow in assemblyNameOutputTable.OutputRows)
			{
				if ((string)outputRow.Row[0] == fileRow.Component && (string)outputRow.Row[1] == name)
				{
					outputRow.Row[2] = value;
					return;
				}
			}

			Row assemblyNameRow = new Row(assemblyNameOutputTable.TableDefinition);
			assemblyNameRow[0] = fileRow.Component;
			assemblyNameRow[1] = name;
			assemblyNameRow[2] = value;
			assemblyNameOutputTable.OutputRows.Add(new OutputRow(assemblyNameRow));
		}

		/// <summary>
		/// Update several msi tables with data contained in files references in the File table.
		/// </summary>
		/// <remarks>
		/// For versioned files, update the file version and language in the File table.  For
		/// unversioned files, add a row to the MsiFileHash table for the file.  For assembly
		/// files, add a row to the MsiAssembly table and add AssemblyName information by adding
		/// MsiAssemblyName rows.
		/// </remarks>
		/// <param name="output">Internal representation of the msi database to operate upon.</param>
		private void UpdateFileInformation(Output output)
		{
			OutputTable mergeTable = output.OutputTables["Merge"];
			if (null != mergeTable)
			{
				foreach (OutputRow outputRow in mergeTable.OutputRows)
				{
					MergeRow mergeRow = (MergeRow)outputRow.Row;
					string moduleFile = null;
					try
					{
						moduleFile = this.extension.FileResolutionHandler(mergeRow.SourceFile, FileResolutionType.Module);
					}
					catch (WixFileNotFoundException wfnfe)
					{
						this.OnMessage(WixErrors.BinderExtensionMissingFile(null, ErrorLevel.Normal, wfnfe.Message));
						continue;
					}

					output.Modules.Add(mergeRow);
					try
					{
						// read the module's File table to get its FileMediaInformation entries
						using (Database db = new Database(moduleFile, OpenDatabase.ReadOnly))
						{
							mergeRow.HasFiles = false;

							if (db.TableExists("File") && db.TableExists("Component"))
							{
								Hashtable uniqueModuleFileIdentifiers = System.Collections.Specialized.CollectionsUtil.CreateCaseInsensitiveHashtable();

								using (View view = db.OpenExecuteView("SELECT `File`, `Directory_` FROM `File`, `Component` WHERE `Component_`=`Component`"))
								{
									Record record;
									while (view.Fetch(out record))
									{
										FileMediaInformation fileMediaInformation = new FileMediaInformation(record[1], record[2], mergeRow.DiskId, String.Concat(this.tempFiles.BasePath, Path.DirectorySeparatorChar, "MergeId.", mergeRow.Id.GetHashCode().ToString("X4", CultureInfo.InvariantCulture.NumberFormat), Path.DirectorySeparatorChar, record[1]), mergeRow.Number, mergeRow.FileCompression, moduleFile, -1);
										FileMediaInformation otherFileMediaInformation = output.FileMediaInformationCollection[fileMediaInformation.FileId];
										string collidingModuleFileIdentifier = (string)uniqueModuleFileIdentifiers[fileMediaInformation.FileId];

										if (null == otherFileMediaInformation && null == collidingModuleFileIdentifier)
										{
											output.FileMediaInformationCollection.Add(fileMediaInformation);

											// keep track of file identifiers in this merge module
											uniqueModuleFileIdentifiers.Add(fileMediaInformation.FileId, fileMediaInformation.FileId);
										}
										else // collision(s) detected
										{
											// case-sensitive collision with another merge module or a user-authored file identifier
											if (null != otherFileMediaInformation)
											{
												this.OnMessage(WixErrors.DuplicateModuleFileIdentifier(mergeRow.SourceLineNumbers, mergeRow.Id, fileMediaInformation.FileId));
											}

											// case-insensitive collision with another file identifier in the same merge module
											if (null != collidingModuleFileIdentifier)
											{
												this.OnMessage(WixErrors.DuplicateModuleCaseInsensitiveFileIdentifier(mergeRow.SourceLineNumbers, mergeRow.Id, fileMediaInformation.FileId, collidingModuleFileIdentifier));
											}
										}

										mergeRow.HasFiles = true;
									}
								}
							}
						}
					}
					catch (FileNotFoundException fnfe)
					{
						throw new WixFileNotFoundException(null, moduleFile, fnfe);
					}
					catch (IOException ioe)
					{
						throw new WixMergeModuleOpenException(mergeRow.SourceLineNumbers, mergeRow.Id, moduleFile, ioe);
					}
				}
			}

			// calculate sequence numbers and media disk id layout for all file media information objects
			if (OutputType.Module == output.Type)
			{
				int lastSequence = 0;
				foreach (FileMediaInformation fmi in output.FileMediaInformationCollection)
				{
					fmi.Modularize(output.ModularizationGuid);
					fmi.Sequence = ++lastSequence;
				}
			}
			else
			{
				int lastSequence = 0;
				MediaRow mediaRow = null;
				output.FileMediaInformationCollection.Sort();
				SortedList patchGroups = new SortedList();

				// sequence the non-patch-added files
				foreach (FileMediaInformation fmi in output.FileMediaInformationCollection)
				{
					if (null == mediaRow)
					{
						mediaRow = output.MediaRows[fmi.Media];
					}
					else if (mediaRow.DiskId != fmi.Media)
					{
						mediaRow.LastSequence = lastSequence;
						mediaRow = output.MediaRows[fmi.Media];
					}

					if (0 < fmi.PatchGroup)
					{
						ArrayList patchGroup = (ArrayList)patchGroups[fmi.PatchGroup];

						if (null == patchGroup)
						{
							patchGroup = new ArrayList();
							patchGroups.Add(fmi.PatchGroup, patchGroup);
						}

						patchGroup.Add(fmi);
					}
					else
					{
						fmi.Sequence = ++lastSequence;
					}
				}
				if (null != mediaRow)
				{
					mediaRow.LastSequence = lastSequence;
					mediaRow = null;
				}

				// sequence the patch-added files
				foreach (ArrayList patchGroup in patchGroups.Values)
				{
					foreach (FileMediaInformation fmi in patchGroup)
					{
						if (null == mediaRow)
						{
							mediaRow = output.MediaRows[fmi.Media];
						}
						else if (mediaRow.DiskId != fmi.Media)
						{
							mediaRow.LastSequence = lastSequence;
							mediaRow = output.MediaRows[fmi.Media];
						}

						fmi.Sequence = ++lastSequence;
					}
				}
				if (null != mediaRow)
				{
					mediaRow.LastSequence = lastSequence;
				}
			}

			// copy the special media rows back to the real media table
			if (0 < output.MediaRows.Count)
			{
				OutputTable mediaTable = Common.EnsureOutputTable(output, this.tableDefinitions["Media"]);

				foreach (MediaRow mediaRow in output.MediaRows)
				{
					mediaTable.OutputRows.Add(new OutputRow(mediaRow));
				}
			}

			OutputTable fileTable = output.OutputTables["File"];
			if (null == fileTable)   // no work to do
			{
				return;
			}

			foreach (OutputRow outputRow in fileTable.OutputRows)
			{
				FileRow fileRow = outputRow.Row as FileRow;
				if (null == fileRow)
				{
					throw new ApplicationException("Expected FileRow");
				}

				// copy the sequence number from the file media information to the file table
				fileRow.Sequence = output.FileMediaInformationCollection[fileRow.File].Sequence;

				string src = fileRow.Source;
				FileInfo fileInfo = null;

				if (!this.suppressFileHashAndInfo || (!this.suppressAssemblies && FileAssemblyType.NotAnAssembly != fileRow.AssemblyType))
				{
					try
					{
						src = this.extension.FileResolutionHandler(fileRow.Source, FileResolutionType.File);
					}
					catch (WixFileNotFoundException wfnfe)
					{
						this.OnMessage(WixErrors.BinderExtensionMissingFile(fileRow.SourceLineNumbers, ErrorLevel.Normal, wfnfe.Message));
						continue;
					}

					try
					{
						fileInfo = new FileInfo(src);
					}
					catch (ArgumentException)
					{
						this.OnMessage(WixErrors.InvalidFileName(fileRow.SourceLineNumbers, ErrorLevel.Normal, src));
						continue;
					}
					catch (PathTooLongException)
					{
						this.OnMessage(WixErrors.InvalidFileName(fileRow.SourceLineNumbers, ErrorLevel.Normal, src));
						continue;
					}
					catch (NotSupportedException)
					{
						this.OnMessage(WixErrors.InvalidFileName(fileRow.SourceLineNumbers, ErrorLevel.Normal, src));
						continue;
					}
				}

				if (!this.suppressFileHashAndInfo)
				{
					if (fileInfo.Exists)
					{
						string version;
						string language;

						fileRow.FileSize = fileInfo.Length;
						try
						{
							MsiBase.FileVersion(fileInfo.FullName, out version, out language);
						}
						catch (FileNotFoundException e)
						{
							throw new WixFileNotFoundException(null, fileInfo.FullName, e);   // TODO: find a way to get the sourceFile (instead of null)
						}

						if (0 == version.Length && 0 == language.Length)   // unversioned files have their hashes added to the MsiFileHash table
						{
							int[] hash;
							try
							{
								MsiBase.GetFileHash(fileInfo.FullName, 0, out hash);
							}
							catch (FileNotFoundException e)
							{
								throw new WixFileNotFoundException(null, fileInfo.FullName, e);   // TODO: find a way to get the sourceFile (instead of null)
							}

							OutputTable outputHashTable = Common.EnsureOutputTable(output, this.tableDefinitions["MsiFileHash"]);
							Row hashRow = new Row(outputHashTable.TableDefinition);
							hashRow[0] = fileRow.File;
							hashRow[1] = 0;
							hashRow[2] = hash[0];
							hashRow[3] = hash[1];
							hashRow[4] = hash[2];
							hashRow[5] = hash[3];
							outputHashTable.OutputRows.Add(new OutputRow(hashRow));
						}
						else // update the file row with the version and language information
						{
							fileRow.Version = version;
							fileRow.Language = language;
						}
					}
					else
					{
						this.OnMessage(WixErrors.CannotFindFile(fileRow.SourceLineNumbers, ErrorLevel.Normal, fileRow.File, fileRow.FileName, src));
					}
				}

				// if we're not suppressing automagically grabbing assembly information and this is a
				// CLR assembly, load the assembly and get the assembly name information
				if (!this.suppressAssemblies)
				{
					if (FileAssemblyType.DotNetAssembly == fileRow.AssemblyType)
					{
						StringDictionary assemblyNameValues = new StringDictionary();

						// under CLR 2.0, use a more robust method of gathering AssemblyName information
						if (2 <= Environment.Version.Major)
						{
							CLRInterop.IReferenceIdentity referenceIdentity = null;
							Guid referenceIdentityGuid = CLRInterop.ReferenceIdentityGuid;

							if (0 == CLRInterop.GetAssemblyIdentityFromFile(fileInfo.FullName, ref referenceIdentityGuid, out referenceIdentity))
							{
								if (null != referenceIdentity)
								{
									string culture = referenceIdentity.GetAttribute(null, "Culture");
									if (null != culture)
									{
										assemblyNameValues.Add("Culture", culture);
									}

									string name = referenceIdentity.GetAttribute(null, "Name");
									if (null != name)
									{
										assemblyNameValues.Add("Name", name);
									}

									string processorArchitecture = referenceIdentity.GetAttribute(null, "ProcessorArchitecture");
									if (null != processorArchitecture)
									{
										assemblyNameValues.Add("ProcessorArchitecture", processorArchitecture);
									}

									string publicKeyToken = referenceIdentity.GetAttribute(null, "PublicKeyToken");
									if (null != publicKeyToken)
									{
										assemblyNameValues.Add("PublicKeyToken", publicKeyToken.ToUpper(CultureInfo.InvariantCulture));
									}

									string version = referenceIdentity.GetAttribute(null, "Version");
									if (null != version)
									{
										assemblyNameValues.Add("Version", version);
									}
								}
							}
						}
						else
						{
							AssemblyName assemblyName = null;
							try
							{
								assemblyName = AssemblyName.GetAssemblyName(fileInfo.FullName);

								if (null != assemblyName.CultureInfo)
								{
									assemblyNameValues.Add("Culture", assemblyName.CultureInfo.ToString());
								}

								if (null != assemblyName.Name)
								{
									assemblyNameValues.Add("Name", assemblyName.Name);
								}

								byte[] publicKey = assemblyName.GetPublicKeyToken();
								if (null != publicKey && 0 < publicKey.Length)
								{
									StringBuilder sb = new StringBuilder();
									for (int i = 0; i < publicKey.GetLength(0); ++i)
									{
										sb.AppendFormat("{0:X2}", publicKey[i]);
									}
									assemblyNameValues.Add("PublicKeyToken", sb.ToString());
								}

								if (null != assemblyName.Version)
								{
									assemblyNameValues.Add("Version", assemblyName.Version.ToString());
								}
							}
							catch (FileNotFoundException fnfe)
							{
								throw new WixFileNotFoundException(fileRow.SourceLineNumbers, fileInfo.FullName, fnfe);
							}
							catch (Exception e)
							{
								if (e is NullReferenceException || e is SEHException)
								{
									throw;
								}
								else
								{
									throw new WixInvalidAssemblyException(fileRow.SourceLineNumbers, fileInfo, e);
								}
							}
						}

						OutputTable assemblyNameOutputTable = Common.EnsureOutputTable(output, this.tableDefinitions["MsiAssemblyName"]);
						if (assemblyNameValues.ContainsKey("name"))
						{
							this.SetMsiAssemblyName(assemblyNameOutputTable, fileRow, "name", assemblyNameValues["name"]);
						}

						string fileVersion = null;
						if (this.setMsiAssemblyNameFileVersion)
						{
							string language;

							MsiBase.FileVersion(fileInfo.FullName, out fileVersion, out language);
							this.SetMsiAssemblyName(assemblyNameOutputTable, fileRow, "fileVersion", fileVersion);
						}

						if (assemblyNameValues.ContainsKey("version"))
						{
							string assemblyVersion = assemblyNameValues["version"];

							// there is a bug in fusion that requires the assembly's "version" attribute
							// to be equal to or longer than the "fileVersion" in length when its present;
							// the workaround is to prepend zeroes to the last version number in the assembly version
							if (this.setMsiAssemblyNameFileVersion && null != fileVersion && fileVersion.Length > assemblyVersion.Length)
							{
								string padding = new string('0', fileVersion.Length - assemblyVersion.Length);
								string[] assemblyVersionNumbers = assemblyVersion.Split('.');

								if (assemblyVersionNumbers.Length > 0)
								{
									assemblyVersionNumbers[assemblyVersionNumbers.Length - 1] = String.Concat(padding, assemblyVersionNumbers[assemblyVersionNumbers.Length - 1]);
									assemblyVersion = String.Join(".", assemblyVersionNumbers);
								}
							}

							this.SetMsiAssemblyName(assemblyNameOutputTable, fileRow, "version", assemblyVersion);
						}

						if (assemblyNameValues.ContainsKey("culture"))
						{
							string culture = assemblyNameValues["culture"];
							this.SetMsiAssemblyName(assemblyNameOutputTable, fileRow, "culture", (String.Empty == culture ? "neutral" : culture));
						}

						if (assemblyNameValues.ContainsKey("publicKeyToken"))
						{
							this.SetMsiAssemblyName(assemblyNameOutputTable, fileRow, "publicKeyToken", assemblyNameValues["publicKeyToken"]);
						}

						if (null != fileRow.ProcessorArchitecture && 0 < fileRow.ProcessorArchitecture.Length)
						{
							this.SetMsiAssemblyName(assemblyNameOutputTable, fileRow, "processorArchitecture", fileRow.ProcessorArchitecture);
						}

						if (assemblyNameValues.ContainsKey("processorArchitecture"))
						{
							this.SetMsiAssemblyName(assemblyNameOutputTable, fileRow, "processorArchitecture", assemblyNameValues["processorArchitecture"]);
						}
					}
					else if (FileAssemblyType.Win32Assembly == fileRow.AssemblyType)
					{
						FileRow fileManifestRow = fileRow;

						// would rather look this up through a data structure rather than
						// do an order n search through the list of files for every
						// instance of a win32 assembly.  From what I can find, there
						// are no indexed data structures available at this point
						// in the code we're left with this expensive search.
						foreach (OutputRow manifestOutputRow in fileTable.OutputRows)
						{
							fileManifestRow = manifestOutputRow.Row as FileRow;
							if (fileManifestRow.File == fileRow.AssemblyManifest)
							{
								break;
							}
						}

						string type = null;
						string name = null;
						string version = null;
						string processorArchitecture = null;
						string publicKeyToken = null;

						// loading the dom is expensive we want more performant APIs than the DOM
						// Navigator is cheaper than dom.  Perhaps there is a cheaper API still.
						string manifestSourcePath = null;
						try
						{
							manifestSourcePath = this.extension.FileResolutionHandler(fileManifestRow.Source, FileResolutionType.File);
						}
						catch (WixFileNotFoundException wfnfe)
						{
							this.OnMessage(WixErrors.BinderExtensionMissingFile(fileRow.SourceLineNumbers, ErrorLevel.Normal, wfnfe.Message));
							continue;
						}

						try
						{
							XPathDocument doc = new XPathDocument(manifestSourcePath);
							XPathNavigator nav = doc.CreateNavigator();
							nav.MoveToRoot();

							// this assumes a particular schema for a win32 manifest and does not
							// provide error checking if the file does not conform to schema.
							// The fallback case here is that nothing is added to the MsiAssemblyName
							// table for a out of tollerence Win32 manifest.  Perhaps warnings needed.
							if (nav.MoveToFirstChild())
							{
								while (nav.NodeType != XPathNodeType.Element || nav.Name != "assembly")
								{
									nav.MoveToNext();
								}
								if (nav.MoveToFirstChild())
								{
									while (nav.NodeType != XPathNodeType.Element || nav.Name != "assemblyIdentity")
									{
										nav.MoveToNext();
									}
									if (nav.MoveToAttribute("type", String.Empty))
									{
										type = nav.Value;
										nav.MoveToParent();
									}
									if (nav.MoveToAttribute("name", String.Empty))
									{
										name = nav.Value;
										nav.MoveToParent();
									}
									if (nav.MoveToAttribute("version", String.Empty))
									{
										version = nav.Value;
										nav.MoveToParent();
									}
									if (nav.MoveToAttribute("processorArchitecture", String.Empty))
									{
										processorArchitecture = nav.Value;
										nav.MoveToParent();
									}
									if (nav.MoveToAttribute("publicKeyToken", String.Empty))
									{
										publicKeyToken = nav.Value;
										nav.MoveToParent();
									}
								}
							}
						}
						catch (XmlException xe)
						{
							this.OnMessage(WixErrors.InvalidXml(SourceLineNumberCollection.FromFileName(manifestSourcePath), "manifest", xe.Message));
						}

						OutputTable assemblyNameOutputTable = Common.EnsureOutputTable(output, this.tableDefinitions["MsiAssemblyName"]);
						if (null != name && 0 < name.Length)
						{
							this.SetMsiAssemblyName(assemblyNameOutputTable, fileRow, "name", name);
						}
						if (null != version && 0 < version.Length)
						{
							this.SetMsiAssemblyName(assemblyNameOutputTable, fileRow, "version", version);
						}
						if (null != type && 0 < type.Length)
						{
							this.SetMsiAssemblyName(assemblyNameOutputTable, fileRow, "type", type);
						}
						if (null != processorArchitecture && 0 < processorArchitecture.Length)
						{
							this.SetMsiAssemblyName(assemblyNameOutputTable, fileRow, "processorArchitecture", processorArchitecture);
						}
						if (null != publicKeyToken && 0 < publicKeyToken.Length)
						{
							this.SetMsiAssemblyName(assemblyNameOutputTable, fileRow, "publicKeyToken", publicKeyToken);
						}
					}
				}
			}
		}

		/// <summary>
		/// Update Control and BBControl text by reading from files when necessary.
		/// </summary>
		/// <param name="output">Internal representation of the msi database to operate upon.</param>
		private void UpdateControlText(Output output)
		{
			// Control table
			OutputTable controlTable = output.OutputTables["Control"];
			if (null != controlTable)
			{
				foreach (OutputRow outputRow in controlTable.OutputRows)
				{
					ControlRow controlRow = outputRow.Row as ControlRow;
					if (null == controlRow)
					{
						throw new ApplicationException("Expected ControlRow");
					}
					if (null != controlRow.SourceFile)
					{
						controlRow.Text = this.ReadTextFile(controlRow.SourceLineNumbers, controlRow.SourceFile);
					}
				}
			}

			// BBControl table
			OutputTable bbcontrolTable = output.OutputTables["BBControl"];
			if (null != bbcontrolTable)
			{
				foreach (OutputRow outputRow in bbcontrolTable.OutputRows)
				{
					BBControlRow bbcontrolRow = outputRow.Row as BBControlRow;
					if (null == bbcontrolRow)
					{
						throw new ApplicationException("Expected BBControlRow");
					}
					if (null != bbcontrolRow.SourceFile)
					{
						bbcontrolRow.Text = this.ReadTextFile(bbcontrolRow.SourceLineNumbers, bbcontrolRow.SourceFile);
					}
				}
			}
		}

		/// <summary>
		/// Reads a text file and returns the contents.
		/// </summary>
		/// <param name="sourceLineNumbers">Source line numbers for row from source.</param>
		/// <param name="src">Source path to file to read.</param>
		/// <returns>Text string read from file.</returns>
		private string ReadTextFile(SourceLineNumberCollection sourceLineNumbers, string src)
		{
			string filePath = null;
			string text = null;
			try
			{
				filePath = this.extension.FileResolutionHandler(src, FileResolutionType.File);
			}
			catch (WixFileNotFoundException wfnfe)
			{
				this.OnMessage(WixErrors.BinderExtensionMissingFile(sourceLineNumbers, ErrorLevel.Normal, wfnfe.Message));
				return null;
			}

			if (File.Exists(filePath))
			{
				try
				{
					using (StreamReader reader = new StreamReader(filePath))
					{
						text = reader.ReadToEnd();
					}
				}
				catch (FileNotFoundException e)
				{
					this.OnMessage(WixErrors.BinderExtensionMissingFile(sourceLineNumbers, ErrorLevel.Normal, e.Message));
				}
				catch (DirectoryNotFoundException e)
				{
					this.OnMessage(WixErrors.BinderExtensionMissingFile(sourceLineNumbers, ErrorLevel.Normal, e.Message));
				}
				catch (IOException e)
				{
					this.OnMessage(WixErrors.BinderExtensionMissingFile(sourceLineNumbers, ErrorLevel.Normal, e.Message));
				}
			}
			else
			{
				this.OnMessage(WixErrors.FileNotFound(filePath));
			}

			return text;
		}

		/// <summary>
		/// Creates the MSI/MSM database for this output in a temp location.
		/// </summary>
		/// <param name="output">Output to create database for.</param>
		/// <returns>Path to generated MSI/MSM in temp directory.</returns>
		private string GenerateDatabase(Output output)
		{
			string databasePath;
			try
			{
				OutputTable validationTable = new OutputTable(this.tableDefinitions["_Validation"]);
				databasePath = Path.Combine(this.tempFiles.BasePath, Path.GetFileName(output.Path));

				// try to create the database
				using (Database db = new Database(databasePath, OpenDatabase.CreateDirect))
				{
					// localize the codepage if a value was specified by the localizer
					if (null != this.localizer && -1 != this.localizer.Codepage)
					{
						output.Codepage = this.localizer.Codepage;
					}

					// if we're not using the default codepage, import a new one into our
					// database before we add any tables (or the tables would be added
					// with the wrong codepage)
					if (0 != output.Codepage)
					{
						this.SetDatabaseCodepage(db, output);
					}

					foreach (OutputTable outputTable in output.OutputTables)
					{
						if (outputTable.TableDefinition.IsUnreal)
						{
							continue;
						}

						int[] localizedColumns = new int[outputTable.TableDefinition.Columns.Count];
						int localizedColumnCount = 0;

						// if there are localization strings, figure out which columns can be localized in this table
						if (null != this.localizer)
						{
							for (int i = 0; i < outputTable.TableDefinition.Columns.Count; i++)
							{
								if (outputTable.TableDefinition.Columns[i].IsLocalizable)
								{
									localizedColumns[localizedColumnCount++] = i;
								}
							}
						}

						// process each row in the table doing the string resource substitutions
						foreach (OutputRow outputRow in outputTable.OutputRows)
						{
							for (int i = 0; i < localizedColumnCount; i++)
							{
								object val = outputRow.Row[localizedColumns[i]];

								if (null != val)
								{
									outputRow.Row[localizedColumns[i]] = this.localizer.GetLocalizedValue(val.ToString());
								}
							}
						}

						// remember the validation rows for this table
						validationTable.OutputRows.Add(outputTable.TableDefinition.GetValidationRows(validationTable.TableDefinition));

						try
						{
							this.ImportTable(db, output, outputTable);
						}
						catch (WixInvalidIdtException wiie)
						{
							// If ValidateRows finds anything it doesn't like, it throws
							outputTable.ValidateRows();
							// Otherwise we rethrow the InvalidIdt
							throw wiie;
						}
					}

					validationTable.OutputRows.Add(validationTable.TableDefinition.GetValidationRows(validationTable.TableDefinition));
					this.ImportTable(db, output, validationTable); // import the validation table

					// we're good, commit the changes to the new MSI
					db.Commit();

					// update the summary information
					this.UpdateSummaryInfo(db);
				}
			}
			catch (IOException e)
			{
				throw new WixFileNotFoundException(SourceLineNumberCollection.FromFileName(output.Path), null, e);
			}

			return databasePath;
		}

		/// <summary>
		/// Merges in any modules to the output database.
		/// </summary>
		/// <param name="databasePath">Path to database.</param>
		/// <param name="output">Output that specifies database and modules to merge.</param>
		/// <remarks>Expects that output's database has already been generated.</remarks>
		private void MergeModules(string databasePath, Output output)
		{
			Debug.Assert(OutputType.Product == output.Type);

			if (0 == output.Modules.Count)   // no work to do
			{
				return;
			}

			IMsmMerge2 merge = null;
			bool commit = false;
			bool logOpen = false;
			bool databaseOpen = false;
			bool moduleOpen = false;
			try
			{
				bool foundError = false;
				MsmMerge msm = new MsmMerge();
				merge = (IMsmMerge2)msm;

				merge.OpenLog(String.Concat(this.tempFiles.BasePath, Path.DirectorySeparatorChar, "merge.log"));
				logOpen = true;

				merge.OpenDatabase(databasePath);
				databaseOpen = true;

				// process all the merge rows
				foreach (MergeRow mergeRow in output.Modules)
				{
					string mergeModulePath = null;
					try
					{
						mergeModulePath = this.extension.FileResolutionHandler(mergeRow.SourceFile, FileResolutionType.Module);
					}
					catch (WixFileNotFoundException wfnfe)
					{
						this.OnMessage(WixErrors.BinderExtensionMissingFile(mergeRow.SourceLineNumbers, ErrorLevel.Normal, wfnfe.Message));
						foundError = true;
						continue;
					}

					try
					{
						merge.OpenModule(mergeModulePath, mergeRow.Language);
					}
					catch (COMException ce)
					{
						if (-2147023273 == ce.ErrorCode) // 0x80070657 - ERROR_INSTALL_LANGUAGE_UNSUPPORTED
						{
							throw new WixUnknownMergeLanguageException(mergeRow.SourceLineNumbers, mergeRow.Id, mergeModulePath, mergeRow.Language, ce);
						}
						else
						{
							throw;
						}
					}
					moduleOpen = true;

					ConnectToFeature connection = output.ModulesToFeatures[mergeRow.Id];
					if (null == connection)
					{
						throw new WixMergeModuleMissingFeatureException(mergeRow.SourceLineNumbers, mergeRow.Id);
					}

					string configData = mergeRow.ConfigurationData;
					if (null != configData)
					{
						ConfigurationCallback callback = new ConfigurationCallback(configData);
						merge.MergeEx(connection.PrimaryFeature, mergeRow.Directory, callback);
					}
					else
					{
						merge.Merge(connection.PrimaryFeature, mergeRow.Directory);
					}

					/*
					IMsmErrors errorCollection = null;
					merge.get_Errors(out errorCollection);
					long count = errorCollection.get_Count();

					if (0 < count)
					{

						throw new WixMergeFailureException(null, this.tempFiles.BasePath, count, null);
					}
					*/

					foreach (string connectTo in connection.ConnectFeatures)
					{
						merge.Connect(connectTo);
					}

					// if the module has files and creating layout
					if (mergeRow.HasFiles && !this.suppressLayout)
					{
						string hashedMergeId = mergeRow.Id.GetHashCode().ToString("X4", CultureInfo.InvariantCulture.NumberFormat);

						// extract the module cabinet, then explode all of the files to a temp directory
						string moduleCabPath = String.Concat(this.tempFiles.BasePath, Path.DirectorySeparatorChar, hashedMergeId, ".module.cab");
						merge.ExtractCAB(moduleCabPath);

						string mergeIdPath = String.Concat(this.tempFiles.BasePath, Path.DirectorySeparatorChar, "MergeId.", hashedMergeId);
						Directory.CreateDirectory(mergeIdPath);

						WixExtractCab extCab = null;
						try
						{
							extCab = new WixExtractCab();
							extCab.Extract(moduleCabPath, mergeIdPath);
						}
						catch (WixCabExtractionException wce)
						{
							COMException comException = wce.InnerException as COMException;
							foundError = true;
							if (null != comException && 0x80070002 == unchecked((uint)comException.ErrorCode))
							{
								extCab = null; // Cab doesn't exist, so drop the object.
								this.OnMessage(WixErrors.CabFileDoesNotExist(moduleCabPath, mergeModulePath, mergeIdPath));
							}
							else
							{
								this.OnMessage(WixErrors.CabExtractionFailed(moduleCabPath, mergeModulePath, mergeIdPath));
							}
						}
						finally
						{
							if (null != extCab)
							{
								try
								{
									extCab.Close();
								}
								catch (WixCabExtractionException)
								{
									this.OnMessage(WixErrors.CabClosureFailed(moduleCabPath));
								}
							}
						}
					}

					moduleOpen = false;
					merge.CloseModule();
				}

				commit = !foundError; // if all seems to have progressed cleanly, feel free to commit the changes to the database
			}
			finally
			{
				if (moduleOpen)
				{
					merge.CloseModule();
				}
				if (databaseOpen)
				{
					merge.CloseDatabase(commit);
				}
				if (logOpen)
				{
					merge.CloseLog();
				}
			}

			// create a Hashtable of all the suppressed sequence types
			Hashtable suppressedTableNames = new Hashtable();
			if (output.SuppressAdminSequence)
			{
				suppressedTableNames[Action.SequenceTypeToString(SequenceType.adminExecute)] = null;
				suppressedTableNames[Action.SequenceTypeToString(SequenceType.adminUI)] = null;
			}
			if (output.SuppressAdvertiseSequence)
			{
				suppressedTableNames[Action.SequenceTypeToString(SequenceType.advertiseExecute)] = null;
			}
			if (output.SuppressUISequence)
			{
				suppressedTableNames[Action.SequenceTypeToString(SequenceType.adminUI)] = null;
				suppressedTableNames[Action.SequenceTypeToString(SequenceType.installUI)] = null;
			}

			using (Database db = new Database(databasePath, OpenDatabase.Direct))
			{
				OutputTable suppressActionOutputTable = output.OutputTables["SuppressAction"];

				// suppress individual actions
				if (null != suppressActionOutputTable)
				{
					foreach (OutputRow outputRow in suppressActionOutputTable.OutputRows)
					{
						if (db.TableExists((string)outputRow.Row[0]))
						{
							Row row = outputRow.Row;
							string query = String.Format("SELECT * FROM {0} WHERE `Action` = '{1}'", row[0].ToString(), (string)row[1]);

							using (View view = db.OpenExecuteView(query))
							{
								Record record;

								if (view.Fetch(out record))
								{
									this.OnMessage(WixWarnings.SuppressMergedAction((string)row[1], row[0].ToString()));
									view.Modify(ModifyView.Delete, record);
									record.Close();
								}
							}
						}
					}
				}

				// query for merge module actions in suppressed sequences and drop them
				foreach (string tableName in suppressedTableNames.Keys)
				{
					if (!db.TableExists(tableName))
					{
						continue;
					}

					using (View view = db.OpenExecuteView(String.Concat("SELECT `Action` FROM ", tableName)))
					{
						Record resultRecord;
						while (view.Fetch(out resultRecord))
						{
							this.OnMessage(WixWarnings.SuppressMergedAction(resultRecord.GetString(1), tableName));
							resultRecord.Close();
						}
					}

					// drop suppressed sequences
					using (View view = db.OpenExecuteView(String.Concat("DROP TABLE ", tableName)))
					{
					}

					// delete the validation rows
					using (View view = db.OpenView(String.Concat("DELETE FROM _Validation WHERE `Table` = ?")))
					{
						Record record = new Record(1);
						record.SetString(1, tableName);
						view.Execute(record);
					}
				}

				// now update the Attributes column for the files from the Merge Modules
				using (View view = db.OpenView("SELECT `Sequence`, `Attributes` FROM `File` WHERE `File`=?"))
				{
					foreach (FileMediaInformation fmi in output.FileMediaInformationCollection)
					{
						if (!fmi.IsInModule)
						{
							continue;
						}

						Record record = new Record(1);
						record.SetString(1, fmi.File);
						view.Execute(record);

						Record recordUpdate;
						view.Fetch(out recordUpdate);

						if (null == recordUpdate)
						{
							throw new WixMergeFailureException(null, this.tempFiles.BasePath, 1, null);
						}

						recordUpdate.SetInteger(1, fmi.Sequence);

						// update the file attributes to match the compression specified
						// on the Merge element or on the Package element
						int attributes = 0;

						// get the current value if its not null
						if (!recordUpdate.IsNull(2))
						{
							attributes = recordUpdate.GetInteger(2);
						}

						if (FileCompressionValue.Yes == fmi.FileCompression)
						{
							attributes |= MsiInterop.MsidbFileAttributesCompressed;
						}
						else if (FileCompressionValue.No == fmi.FileCompression)
						{
							attributes |= MsiInterop.MsidbFileAttributesNoncompressed;
						}
						else // not specified
						{
							Debug.Assert(FileCompressionValue.NotSpecified == fmi.FileCompression);

							// clear any compression bits
							attributes &= ~MsiInterop.MsidbFileAttributesCompressed;
							attributes &= ~MsiInterop.MsidbFileAttributesNoncompressed;
						}
						recordUpdate.SetInteger(2, attributes);

						view.Modify(ModifyView.Update, recordUpdate);
					}
				}

				db.Commit();
			}
		}

		/// <summary>
		/// Adds all the streams to the final output.
		/// </summary>
		/// <param name="databasePath">Path to database.</param>
		/// <param name="output">Output object that points at final output.</param>
		private void ImportStreams(string databasePath, Output output)
		{
			using (Database db = new Database(databasePath, OpenDatabase.Direct))
			{
				View streamsView = null;
				View binaryView = null;
				View iconView = null;
				View certificateView = null;

				try
				{
					streamsView = db.OpenExecuteView("SELECT `Name`, `Data` FROM `_Streams`");
					if (db.TableExists("Binary"))
					{
						binaryView = db.OpenExecuteView("SELECT `Name`, `Data` FROM `Binary`");
					}
					if (db.TableExists("Icon"))
					{
						iconView = db.OpenExecuteView("SELECT `Name`, `Data` FROM `Icon`");
					}
					if (db.TableExists("MsiDigitalCertificate"))
					{
						certificateView = db.OpenExecuteView("SELECT `DigitalCertificate`, `CertData` FROM `MsiDigitalCertificate`");
					}

					foreach (ImportStream importStream in output.ImportStreams)
					{
						string src;
						using (Record record = new Record(2))
						{
							try
							{
								switch (importStream.Type)
								{
									case ImportStreamType.Cabinet:
										this.OnMessage(WixVerboses.ImportCabinetStream(importStream.StreamName, importStream.Path));

										record[1] = importStream.StreamName;
										record.SetStream(2, importStream.Path);
										streamsView.Modify(ModifyView.Assign, record);
										break;

									case ImportStreamType.DigitalCertificate:
										src = this.extension.FileResolutionHandler(importStream.Path, FileResolutionType.DigitalCertificate);
										this.OnMessage(WixVerboses.ImportDigitalCertificateStream(null, VerboseLevel.Trace, src));

										record[1] = importStream.StreamName;
										record.SetStream(2, importStream.Path);
										certificateView.Modify(ModifyView.Assign, record);
										break;

									case ImportStreamType.Binary:
										src = this.extension.FileResolutionHandler(importStream.Path, FileResolutionType.Binary);
										this.OnMessage(WixVerboses.ImportBinaryStream(null, VerboseLevel.Trace, src));

										if (OutputType.Module == output.Type)
										{
											record[1] = String.Concat(importStream.StreamName, ".", output.ModularizationGuid);
										}
										else
										{
											record[1] = importStream.StreamName;
										}
										if (55 < record[1].Length)
										{
											throw new WixInvalidAttributeException(null, "Binary", "Id", String.Format("Identifier cannot be longer than 55 characters.  Binary identifier: {0}", record[1]));
										}
										record.SetStream(2, src);
										binaryView.Modify(ModifyView.Assign, record);
										break;

									case ImportStreamType.Icon:
										src = this.extension.FileResolutionHandler(importStream.Path, FileResolutionType.Icon);
										this.OnMessage(WixVerboses.ImportIconStream(null, VerboseLevel.Verbose, src));

										if (OutputType.Module == output.Type)
										{
											int start = importStream.StreamName.LastIndexOf(".");
											if (-1 == start)
											{
												record[1] = String.Concat(importStream.StreamName, ".", output.ModularizationGuid);
											}
											else
											{
												record[1] = String.Concat(importStream.StreamName.Substring(0, start), ".", output.ModularizationGuid, importStream.StreamName.Substring(start));
											}
										}
										else
										{
											record[1] = importStream.StreamName;
										}
										if (55 < record[1].Length)
										{
											throw new WixInvalidAttributeException(null, "Icon", "Id", String.Format("Identifier cannot be longer than 55 characters.  Icon identifier: {0}", record[1]));
										}
										record.SetStream(2, src);
										iconView.Modify(ModifyView.Assign, record);
										break;

									default:
										throw new ArgumentException(String.Format("unknown import stream type: {0}, name: {1}", importStream.Type, importStream.StreamName), "importStream");
								}
							}
							catch (WixFileNotFoundException wfnfe)
							{
								this.OnMessage(WixErrors.BinderExtensionMissingFile(null, ErrorLevel.Normal, wfnfe.Message));
							}
						}
					}

					db.Commit();
				}
				catch (FileNotFoundException fnfe)
				{
					throw new WixFileNotFoundException(null, fnfe.FileName, fnfe);
				}
				finally
				{
					if (null != certificateView)
					{
						certificateView.Close();
					}
					if (null != iconView)
					{
						iconView.Close();
					}
					if (null != binaryView)
					{
						binaryView.Close();
					}
					if (null != streamsView)
					{
						streamsView.Close();
					}
				}
			}
		}

		/// <summary>
		/// Creates a source image for the output.
		/// </summary>
		/// <param name="databasePath">Path to database.</param>
		/// <param name="output">Output to generate image for.</param>
		/// <param name="fileTransfers">Array of files to be transfered.</param>
		/// <remarks>Expects that database has already been generated.</remarks>
		private void ProcessMediaInformation(string databasePath, Output output, ArrayList fileTransfers)
		{
			// finally, layout the media
			bool packageCompressed = output.Compressed;
			string lastCabinet = null;
			CompressionLevel lastCompressionLevel = CompressionLevel.Mszip;
			ArrayList compressedFiles = new ArrayList();
			ArrayList uncompressedFiles = new ArrayList();
			foreach (FileMediaInformation fmi in output.FileMediaInformationCollection)
			{
				string cabinet = null;
				CompressionLevel compressionLevel = CompressionLevel.Mszip;

				if (OutputType.Module == output.Type)
				{
					cabinet = "#MergeModule.CABinet";
				}
				else
				{
					MediaRow mediaRow = output.MediaRows[fmi.Media];
					cabinet = mediaRow.Cabinet;
					compressionLevel = mediaRow.CompressionLevel;
				}

				if (lastCabinet != cabinet)
				{
					if (null != lastCabinet && 0 < compressedFiles.Count)
					{
						this.CreateCabinet(Path.GetDirectoryName(output.Path), lastCabinet, lastCompressionLevel, compressedFiles, output.ImportStreams, fileTransfers);
						compressedFiles.Clear();
					}

					lastCabinet = cabinet;
					lastCompressionLevel = compressionLevel;
				}

				if (OutputType.Product == output.Type &&
					(FileCompressionValue.No == fmi.FileCompression ||
					(FileCompressionValue.NotSpecified == fmi.FileCompression && !packageCompressed)))
				{
					uncompressedFiles.Add(fmi);
				}
				else // file in a Module or marked compressed
				{
					compressedFiles.Add(fmi);
				}
			}

			if (0 < compressedFiles.Count)
			{
				if (null == lastCabinet)
				{
					throw new WixInvalidElementException(SourceLineNumberCollection.FromFileName(output.Path), "Media", "Some files were marked compressed, but no 'Cabinet' attribute was provided.");
				}

				this.CreateCabinet(Path.GetDirectoryName(output.Path), lastCabinet, lastCompressionLevel, compressedFiles, output.ImportStreams, fileTransfers);
			}

			// handle the files that aren't in a cabinet...
			this.CreateUncompressedImage(databasePath, output, uncompressedFiles, packageCompressed, fileTransfers);
		}

		/// <summary>
		/// Final step in binding that transfers (moves/copies) all files generated into the appropriate
		/// location in the source image
		/// </summary>
		/// <param name="fileTransfers">Array of files to transfer.</param>
		private void LayoutMedia(ArrayList fileTransfers)
		{
			ArrayList destinationFiles = new ArrayList();

			for (int i = 0; i < fileTransfers.Count; ++i)
			{
				FileTransfer fileTransfer = (FileTransfer)fileTransfers[i];

				Debug.Assert(0 != String.Compare(fileTransfer.source, fileTransfer.destination, true, CultureInfo.CurrentCulture));
				bool retry = false;
				do
				{
					try
					{
						if (fileTransfer.move)
						{
							this.OnMessage(WixVerboses.MoveFile(null, VerboseLevel.Trace, fileTransfer.source, fileTransfer.destination));
							this.fileMoveHandler(fileTransfer.source, fileTransfer.destination);
							retry = false;
						}
						else
						{
							this.OnMessage(WixVerboses.CopyFile(null, VerboseLevel.Trace, fileTransfer.source, fileTransfer.destination));
							this.fileCopyHandler(fileTransfer.source, fileTransfer.destination, true);
							retry = false;
						}

						destinationFiles.Add(fileTransfer.destination);
					}
					catch (FileNotFoundException e)
					{
						throw new WixFileNotFoundException(null, e.FileName, e);
					}
					catch (DirectoryNotFoundException)
					{
						// if we already retried, give up
						if (retry)
						{
							throw;
						}

						string directory = Path.GetDirectoryName(fileTransfer.destination);
						this.OnMessage(WixVerboses.CreateDirectory(null, VerboseLevel.Trace, directory));
						Directory.CreateDirectory(directory);
						retry = true;
					}
					catch (UnauthorizedAccessException)
					{
						// if we already retried, give up
						if (retry)
						{
							throw;
						}

						if (File.Exists(fileTransfer.destination))
						{
							this.OnMessage(WixVerboses.RemoveDestinationFile(null, VerboseLevel.Trace, fileTransfer.destination));

							// try to ensure the file is not read-only
							FileAttributes attributes = File.GetAttributes(fileTransfer.destination);
							try
							{
								File.SetAttributes(fileTransfer.destination, attributes & ~FileAttributes.ReadOnly);
							}
							catch (ArgumentException) // thrown for unauthorized access errors
							{
								this.OnMessage(WixErrors.UnauthorizedAccess(fileTransfer.destination));
							}

							// try to delete the file
							try
							{
								File.Delete(fileTransfer.destination);
							}
							catch (IOException e)
							{
								throw new WixFileInUseException(fileTransfer.destination, e);
							}
							retry = true;
						}
						else // no idea what just happened, bail
						{
							throw;
						}
					}
					catch (IOException)
					{
						// if we already retried, give up
						if (retry)
						{
							throw;
						}

						if (File.Exists(fileTransfer.destination))
						{
							this.OnMessage(WixVerboses.RemoveDestinationFile(null, VerboseLevel.Trace, fileTransfer.destination));

							// ensure the file is not read-only, then delete it
							FileAttributes attributes = File.GetAttributes(fileTransfer.destination);
							File.SetAttributes(fileTransfer.destination, attributes & ~FileAttributes.ReadOnly);
							try
							{
								File.Delete(fileTransfer.destination);
							}
							catch (IOException e)
							{
								throw new WixFileInUseException(fileTransfer.destination, e);
							}
							retry = true;
						}
						else // no idea what just happened, bail
						{
							throw;
						}
					}
				}
				while (retry);
			}

			// finally, if there were any files remove the ACL that may have been added to
			// during the file transfer process
			if (0 < destinationFiles.Count && !this.suppressAclReset)
			{
				int result = Microsoft.Tools.WindowsInstallerXml.Cab.Interop.CabInterop.ResetAcls((string[])destinationFiles.ToArray(typeof(string)), (uint)destinationFiles.Count);
				if (0 != result)
				{
					this.OnMessage(WixWarnings.UnableToResetAcls());
				}
			}
		}

		/// <summary>
		/// Sets the codepage of a database.
		/// </summary>
		/// <param name="db">Database to set codepage into.</param>
		/// <param name="output">Output with the codepage for the database.</param>
		private void SetDatabaseCodepage(Database db, Output output)
		{
			// write out the _ForceCodepage IDT file
			string idtPath = String.Concat(this.tempFiles.BasePath, Path.DirectorySeparatorChar, "codepage.idt");
			using (StreamWriter idtFile = new StreamWriter(idtPath, true, Encoding.ASCII))
			{
				idtFile.WriteLine(); // dummy column name record
				idtFile.WriteLine(); // dummy column definition record
				idtFile.Write(output.Codepage);
				idtFile.WriteLine("\t_ForceCodepage");
			}

			// try to import the table into the MSI
			try
			{
				db.Import(Path.GetDirectoryName(idtPath), Path.GetFileName(idtPath));
			}
			catch (System.Configuration.ConfigurationException ce)
			{
				throw new WixInvalidCodepageException(SourceLineNumberCollection.FromFileName(output.Path), output.Codepage, ce);
			}
		}

		/// <summary>
		/// Imports a table into the database.
		/// </summary>
		/// <param name="db">Database to import table to.</param>
		/// <param name="output">Output for current database.</param>
		/// <param name="outputTable">Output table to import into database.</param>
		private void ImportTable(Database db, Output output, OutputTable outputTable)
		{
			// write out the table to an IDT file
			string idtPath = Path.Combine(this.tempFiles.BasePath, String.Concat(outputTable.Name, ".idt"));
			StreamWriter idtWriter = null;

			try
			{
				Encoding encoding = (0 == output.Codepage ? Encoding.ASCII : Encoding.GetEncoding(output.Codepage));

				// this is a workaround to prevent the UTF-8 byte order marking (BOM)
				// from being added to the beginning of the idt file - according to
				// MSDN, the default encoding for StreamWriter is a special UTF-8
				// encoding that returns an empty byte[] from GetPreamble
				if (Encoding.UTF8 == encoding)
				{
					idtWriter = new StreamWriter(idtPath, false);
				}
				else
				{
					idtWriter = new StreamWriter(idtPath, false, encoding);
				}

				idtWriter.Write(outputTable.ToIdtDefinition(output.ModularizationGuid, 0 == output.IgnoreModularizations.Count ? null : output.IgnoreModularizations));
			}
			finally
			{
				if (null != idtWriter)
				{
					idtWriter.Close();
				}
			}

			// try to import the table into the MSI
			try
			{
				db.Import(Path.GetDirectoryName(idtPath), Path.GetFileName(idtPath));
			}
			catch (System.Configuration.ConfigurationException ce)
			{
				throw new WixInvalidIdtException(SourceLineNumberCollection.FromFileName(output.Path), outputTable.Name, idtPath, ce);
			}
		}

		/// <summary>
		/// Updates the summary information of a database with the current time.
		/// </summary>
		/// <param name="db">Database to update.</param>
		private void UpdateSummaryInfo(Database db)
		{
			string now = DateTime.Now.ToString(CultureInfo.InvariantCulture);

			using (SummaryInformation summary = new SummaryInformation(db))
			{
				if ("{????????-????-????-????-????????????}" == summary.GetProperty(9).ToString())
				{
					summary.SetProperty(9, GenerateGuid());
				}
				summary.SetProperty(12, now);
				summary.SetProperty(13, now);
				Version currentVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
				summary.SetProperty(18, String.Format("Windows Installer XML v{0} (candle/light)", currentVersion.ToString()));

				summary.Close(true);
			}
		}

		/// <summary>
		/// Creates a cabinet using the wixcab.dll interop layer.
		/// </summary>
		/// <param name="cabinetDir">Directory to create cabinet in.</param>
		/// <param name="cabinetName">Name of cabinet to create.</param>
		/// <param name="level">Level of compression to compress with.</param>
		/// <param name="files">Array of files to put into cabinet.</param>
		/// <param name="importStreams">Collection of import streams to add cabinet to if embedding.</param>
		/// <param name="fileTransfers">Array of files to be transfered.</param>
		private void CreateCabinet(string cabinetDir, string cabinetName, CompressionLevel level, ArrayList files, ImportStreamCollection importStreams, ArrayList fileTransfers)
		{
			if (0 == files.Count || this.foundError)
			{
				return;
			}

			bool embedCabinet;
			string cabinetPath;
			string[] fileIds = new string[files.Count];
			string[] filePaths = new string[files.Count];

			// if the cabinet is embedded
			if (cabinetName.StartsWith("#"))
			{
				cabinetName = cabinetName.Substring(1);
				cabinetPath = Path.Combine(this.tempFiles.BasePath, cabinetName);

				embedCabinet = true;
			}
			else // cabinet is not embedded
			{
				cabinetPath = Path.Combine(this.tempFiles.BasePath, cabinetName);
				embedCabinet = false;
			}

			for (int i = 0; i < files.Count; i++)
			{
				FileMediaInformation fmi = (FileMediaInformation)files[i];

				fileIds[i] = fmi.File;
				try
				{
					filePaths[i] = this.extension.FileResolutionHandler(fmi.Source, FileResolutionType.File);
				}
				catch (WixFileNotFoundException wfnfe)
				{
					this.OnMessage(WixErrors.BinderExtensionMissingFile(null, ErrorLevel.Normal, wfnfe.Message));
					continue;
				}

				this.OnMessage(WixVerboses.CabFile(null, VerboseLevel.Verbose, fileIds[i], filePaths[i]));
			}

			if (this.foundError)
			{
				// Don't create the cabinet if errors have been found.
				return;
			}

			CabinetBuildOption cabinetBuildOption = this.extension.CabinetResolutionHandler(fileIds, filePaths, ref cabinetPath);

			// build the cabinet if its not being skipped
			if (CabinetBuildOption.BuildAndCopy == cabinetBuildOption || CabinetBuildOption.BuildAndMove == cabinetBuildOption)
			{
				WixCreateCab cab = null;
				try
				{
					cab = new WixCreateCab(Path.GetFileName(cabinetPath), Path.GetDirectoryName(cabinetPath), 0, 0, level);
					cab.AddFiles(filePaths, fileIds, false);
				}
				catch (WixCabCreationException wcce)
				{
					if (null != wcce.FileName)
					{
						this.OnMessage(WixErrors.CabCreationFailed(cabinetPath, wcce.FileName, wcce.ErrorCode));
					}
					else
					{
						this.OnMessage(WixErrors.CabCreationFailed(cabinetPath, wcce.ErrorCode));
					}
				}
				catch (FileNotFoundException fnfe)
				{
					throw new WixFileNotFoundException(null, fnfe.FileName, fnfe);
				}
				finally
				{
					if (null != cab)
					{
						try
						{
							cab.Close();
						}
						catch (WixCabCreationException)
						{
							this.OnMessage(WixErrors.CabClosureFailed(cabinetPath));
						}
					}
				}
			}

			if (embedCabinet)
			{
				importStreams.Add(new ImportStream(ImportStreamType.Cabinet, cabinetName, cabinetPath));
			}
			else
			{
				string destinationPath = Path.Combine(cabinetDir, cabinetName);
				fileTransfers.Add(new FileTransfer(cabinetPath, destinationPath, CabinetBuildOption.BuildAndMove == cabinetBuildOption));
			}
		}

		/// <summary>
		/// Lays out the binaries for the uncompressed portion of a source image.
		/// </summary>
		/// <param name="databasePath">Path to database.</param>
		/// <param name="output">Output being created.</param>
		/// <param name="files">Array of files to copy into image.</param>
		/// <param name="packageCompressed">Flag if package is compressed.</param>
		/// <param name="fileTransfers">Array of files to be transfered.</param>
		private void CreateUncompressedImage(string databasePath, Output output, ArrayList files, bool packageCompressed, ArrayList fileTransfers)
		{
			if (0 == files.Count || this.foundError)
			{
				return;
			}

			bool longNamesInImage = output.LongFileNames;
			Hashtable directories = new Hashtable();
			using (Database db = new Database(databasePath, OpenDatabase.ReadOnly))
			{
				using (View directoryView = db.OpenExecuteView("SELECT `Directory`, `Directory_Parent`, `DefaultDir` FROM `Directory`"))
				{
					Record directoryRecord;

					while (directoryView.Fetch(out directoryRecord))
					{
						string sourceName = GetSourceName(directoryRecord.GetString(3), longNamesInImage);

						directories.Add(directoryRecord.GetString(1), new ResolvedDirectory(directoryRecord.GetString(2), sourceName));
					}
				}

				using (View fileView = db.OpenView("SELECT `Directory_`, `FileName` FROM `Component`, `File` WHERE `Component`.`Component`=`File`.`Component_` AND `File`.`File`=?"))
				{
					// if an output path was specified for our image, use that as our default base,
					// otherwise use the directory where the output is being placed
					string defaultBaseOuputPath = null != this.imagebaseOutputPath ? this.imagebaseOutputPath : Path.GetDirectoryName(output.Path);

					using (Record fileQueryRecord = new Record(1))
					{
						// for each file in the array of uncompressed files
						foreach (FileMediaInformation fmi in files)
						{
							string currentSourcePath = null;
							string relativeSourcePath = null;

							// determine what the base of the file should be.  If there was
							// no src specified in the Media element (the default) then just
							// use the default output path (usually the same directory as the
							// output file).  If there was a build directory specified then
							// check if it is a absolute path, and if not add the default
							// output path to the root
							MediaRow mediaRow = output.MediaRows[fmi.Media];
							string baseRelativeSourcePath = mediaRow.Layout;
							if (null == baseRelativeSourcePath)
							{
								baseRelativeSourcePath = defaultBaseOuputPath;
							}
							else if (!Path.IsPathRooted(baseRelativeSourcePath))
							{
								baseRelativeSourcePath = Path.Combine(defaultBaseOuputPath, baseRelativeSourcePath);
							}

							// setup up the query record and find the appropriate file in the
							// previously executed file view
							fileQueryRecord[1] = fmi.File;
							fileView.Execute(fileQueryRecord);

							Record fileRecord;
							if (!fileView.Fetch(out fileRecord))
							{
								throw new WixFileMediaInformationKeyNotFoundException(fmi.File);
							}

							string fileName = GetSourceName(fileRecord[2], longNamesInImage);

							if (packageCompressed)
							{
								// use just the file name of the file since all uncompressed files must appear
								// in the root of the image in a compressed package
								relativeSourcePath = fileName;
							}
							else
							{
								// get the relative path of where we want the source to be as specified
								// in the Directory table
								string directoryPath = GetDirectoryPath(directories, fileRecord[1], longNamesInImage);
								relativeSourcePath = Path.Combine(directoryPath, fileName);
							}

							// if the relative source path was not resolved above then we have to bail
							if (null == relativeSourcePath)
							{
								throw new WixFileMediaInformationKeyNotFoundException(fmi.File);
							}

							// strip off "SourceDir" if it's still on there
							if (relativeSourcePath.StartsWith("SourceDir\\"))
							{
								relativeSourcePath = relativeSourcePath.Substring(10);
							}

							// resolve the src path for the file and ensure it exists
							try
							{
								currentSourcePath = this.extension.FileResolutionHandler(fmi.Source, FileResolutionType.File);
							}
							catch (WixFileNotFoundException wfnfe)
							{
								this.OnMessage(WixErrors.BinderExtensionMissingFile(null, ErrorLevel.Normal, wfnfe.Message));
								continue;
							}

							if (!(File.Exists(currentSourcePath)))
							{
								this.OnMessage(WixErrors.CannotFindFile(null, ErrorLevel.Normal, fmi.FileId, fmi.File, fmi.Source));
								continue;
							}

							// finally put together the base image output path and the resolved source path
							string resolvedSourcePath = Path.Combine(baseRelativeSourcePath, relativeSourcePath);

							// if the current source path (where we know that the file already exists) and the resolved
							// path as dictated by the Directory table are not the same, then propagate the file.  The
							// image that we create may have already been done by some other process other than the linker, so
							// there is no reason to copy the files to the resolved source if they are already there.
							if (0 != String.Compare(Path.GetFullPath(currentSourcePath), Path.GetFullPath(resolvedSourcePath), true))
							{
								// just put the file in the transfers array, how anti-climatic
								fileTransfers.Add(new FileTransfer(currentSourcePath, resolvedSourcePath, false));
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// Structure used for all file transfer information.
		/// </summary>
		private struct FileTransfer
		{
			/// <summary>Source path to file.</summary>
			public string source;
			/// <summary>Destination path for file.</summary>
			public string destination;
			/// <summary>Flag if file should be moved (optimal).</summary>
			public bool move;

			/// <summary>
			/// Basic constructor for struct
			/// </summary>
			/// <param name="source">Source path to file.</param>
			/// <param name="destination">Destination path for file.</param>
			/// <param name="move">File if file should be moved (optimal).</param>
			public FileTransfer(string source, string destination, bool move)
			{
				this.source = source;
				this.destination = destination;
				this.move = move;
			}
		}

		/// <summary>
		/// Structure used for resolved directory information.
		/// </summary>
		private struct ResolvedDirectory
		{
			/// <summary>The directory parent.</summary>
			public string directoryParent;
			/// <summary>The source name of this directory.</summary>
			public string sourceName;
			/// <summary>The source path of this directory.</summary>
			public string sourcePath;

			/// <summary>
			/// Constructor for ResolvedDirectory.
			/// </summary>
			/// <param name="directoryParent">Parent directory.</param>
			/// <param name="sourceName">Default directory names.</param>
			public ResolvedDirectory(string directoryParent, string sourceName)
			{
				this.directoryParent = directoryParent;
				this.sourceName = sourceName;
				this.sourcePath = null;
			}
		}

		/// <summary>
		/// Callback object for configurable merge modules.
		/// </summary>
		public class ConfigurationCallback : IMsmConfigureModule
		{
			private const int SOk = 0x0;
			private const int SFalse = 0x1;
			private Hashtable configData;

			/// <summary>
			/// Creates a ConfigurationCallback object.
			/// </summary>
			/// <param name="configurationData">String to break up into name/value pairs.</param>
			public ConfigurationCallback(string configurationData)
			{
				if (null == configurationData)
				{
					throw new ArgumentNullException("configurationData");
				}

				this.configData = new Hashtable();
				string[] pairs = configurationData.Split(",".ToCharArray());
				for (int i = 0; i < pairs.Length; ++i)
				{
					string[] nameVal = pairs[i].Split("=".ToCharArray());
					string name = nameVal[0];
					string value = nameVal[1];

					name = name.Replace("%2C", ",");
					name = name.Replace("%3D", "=");
					name = name.Replace("%25", "%");

					value = value.Replace("%2C", ",");
					value = value.Replace("%3D", "=");
					value = value.Replace("%25", "%");

					this.configData[name] = value;
				}
			}

			/// <summary>
			/// Returns text data based on name.
			/// </summary>
			/// <param name="name">Name of value to return.</param>
			/// <param name="configData">Out param to put configuration data into.</param>
			/// <returns>S_OK if value provided, S_FALSE if not.</returns>
			public int ProvideTextData(string name, out string configData)
			{
				if (this.configData.Contains(name))
				{
					configData = (string)this.configData[name];
					return SOk;
				}
				else
				{
					configData = null;
					return SFalse;
				}
			}

			/// <summary>
			/// Returns integer data based on name.
			/// </summary>
			/// <param name="name">Name of value to return.</param>
			/// <param name="configData">Out param to put configuration data into.</param>
			/// <returns>S_OK if value provided, S_FALSE if not.</returns>
			public int ProvideIntegerData(string name, out int configData)
			{
				if (this.configData.Contains(name))
				{
					string val = (string)this.configData[name];
					configData = Convert.ToInt32(val);
					return SOk;
				}
				else
				{
					configData = 0;
					return SFalse;
				}
			}
		}
	}
}
