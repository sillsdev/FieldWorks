//---------------------------------------------------------------------------------------------
#region /// Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2010' to='2011' company='SIL International'>
//	 Copyright (c) 2011, SIL International. All Rights Reserved.
// </copyright>
#endregion
//
// File: ImportFrom6_0.cs
// Responsibility: FW Team
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Text;
using System.Windows.Forms;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Win32;

using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;
using SIL.Utils;
using ProgressBarStyle = SIL.FieldWorks.Common.FwUtils.ProgressBarStyle;

namespace SIL.FieldWorks.FDO.DomainServices.DataMigration
{
	/// <summary>
	/// Handles import of FW 6.0X data from a zip file containing an XML backup.
	/// It also handles import of a plain FW 6.0X XML file.
	/// </summary>
	public class ImportFrom6_0
	{
		#region Data members
		/// <summary>Db Version number for FieldWorks 6.0</summary>
		public const int FieldWorks6DbVersion = 200260;
		/// <summary>the name of the temporary project/database created for migrating from 6.0 to 7.0</summary>
		public const string TempDatabaseName = "TempForMigration";
		/// <summary>path to the version 6.0 (or earlier) dumpxml.exe program</summary>
		private string m_dumpxmlPath;
		/// <summary>path to the enhanced version of the db.exe program</summary>
		private string m_dbPath;
		/// <summary>accumulated error messages from running a process</summary>
		private string m_errorMessages = "";
		/// <summary>accumulated messages written to stdout by a process</summary>
		private StringBuilder m_stdoutBldr;

		bool m_fCheckedForSqlServer;
		bool m_fCheckedForOldFieldWorks;
		bool m_fHaveSqlServer;
		bool m_fHaveOldFieldWorks;

		readonly bool m_fVerboseDebug;
		private readonly IThreadedProgress m_progressDlg;
		#endregion

		/// <summary>
		/// provides parent form for progress dialog
		/// </summary>
		public Form ParentForm { get; set; }

		#region Constructors
		/// <summary>
		/// Constructor for run-time debugging.
		/// </summary>
		public ImportFrom6_0(IThreadedProgress progressDlg, bool fDebug)
		{
			m_progressDlg = progressDlg;
			m_fVerboseDebug = fDebug;
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public ImportFrom6_0(IThreadedProgress progressDlg) : this(progressDlg, false)
		{
		}
		#endregion

		/// <summary>
		/// Do the import of the specified zip or XML file. Return true if successful and the caller should open the database.
		/// </summary>
		public bool Import(string pathname, string projectName, out string projectFile)
		{
			var destFolder = DirectoryFinder.ProjectsDirectory;
			var folderName = Path.Combine(destFolder, projectName);
			projectFile = Path.Combine(folderName, projectName + FwFileExtensions.ksFwDataXmlFileExtension);
			string extension = Path.GetExtension(pathname);
			if (extension != null)
				extension = extension.ToLowerInvariant();
			if (extension == ".xml")
			{
				if (!IsValid6_0Xml(pathname))
				{
					MessageBoxUtils.Show(m_progressDlg.Form, Strings.ksBackupXMLFileTooOld,
						Strings.ksCannotConvert);
					return false;
				}
				var result1 = ImportFrom6_0Xml(pathname, folderName, projectFile);
				return result1;
			}
			using (var zipFile = new ZipFile(pathname))
			{
				bool fHasXml = false;
				bool fHasBak = false;
				foreach (ZipEntry entry in zipFile)
				{
					// Check only the first XML file found in the zip file.
					if (!fHasXml && entry.Name.ToLowerInvariant().EndsWith(".xml") && entry.IsFile)
					{
						fHasXml = true;
						string tempPath;
						bool fCreateFolder = !Directory.Exists(folderName);
						if (fCreateFolder)
							Directory.CreateDirectory(folderName);
						string message = String.Format(Strings.ksExtractingFromZip, Path.GetFileName(entry.Name));
						if (!UnzipFile(zipFile, entry, message, out tempPath))
						{
							return false;
						}
						// Now we should have the XML file extracted!
						if (!IsValid6_0Xml(tempPath))
						{
							// Can't use the XML file, so don't try.  Continue on to verify whether this is
							// actually a FieldWorks backup zip file or not.
							File.Delete(tempPath);
							if (fCreateFolder)
								Directory.Delete(folderName);
							if (fHasBak)
								break;
							else
								continue;
						}
						// Next step is to run the converter. It should be in the same directory as FDO.dll
						var result = ImportFrom6_0Xml(tempPath, folderName, projectFile);
						File.Delete(tempPath);
						return result;
					}
					if (entry.Name.ToLowerInvariant().EndsWith(".bak") && entry.IsFile)
					{
						fHasBak = true;
						if (fHasXml)
							break;
					}
				}
				if (!fHasBak)
				{
					MessageBoxUtils.Show(m_progressDlg.Form, Strings.ksZipNotFieldWorksBackup, Strings.ksCannotConvert);
					return false;
				}
				if (HaveFwSqlServer && HaveOldFieldWorks)
				{
					foreach (ZipEntry entry in zipFile)
					{
						if (entry.Name.ToLowerInvariant().EndsWith(".bak") && entry.IsFile)
						{
							string tempPath;
							string message = String.Format(Strings.ksExtractingFromZip, Path.GetFileName(entry.Name));
							if (!UnzipFile(zipFile, entry, message, out tempPath))
							{
								return false;
							}
							DeleteTempDatabase();
							// Now we should have the .bak file extracted.  Create a temp database from
							// the given .bak file, migrate it, and dump it out as XML.
							string project = Path.GetFileNameWithoutExtension(pathname);
							string proj = Path.GetFileNameWithoutExtension(tempPath);
							string msg = String.Format(SIL.FieldWorks.FDO.Strings.ksRestoringToTempProject, project);
							string errMsgFmt = String.Format(SIL.FieldWorks.FDO.Strings.ksRestoringToTempProjectFailed,
							project, "{0}", "{1}");
							if (!CreateTempDatabase(tempPath, msg, errMsgFmt))
								return false;
							string msg2 = String.Format(SIL.FieldWorks.FDO.Strings.ksMigratingTempCopyToFw60, proj);
							string errMsgFmt2 = String.Format(SIL.FieldWorks.FDO.Strings.ksMigratingTempCopyToFw60Failed,
							proj, "{0}", "{1}");
							if (!MigrateTempDatabase(msg2, errMsgFmt2))
								return false;
							string tempXmlPath = Path.ChangeExtension(tempPath, "xml");
							string msg3 = String.Format(SIL.FieldWorks.FDO.Strings.ksWritingTempCopyAsFw60XML, proj);
							string errMsgFmt3 = String.Format(SIL.FieldWorks.FDO.Strings.ksWritingTempCopyAsFw60XMLFailed,
							proj, "{0}", "{1}");
							if (!DumpDatabaseAsXml(TempDatabaseName, tempXmlPath, msg3, errMsgFmt3))
								return false;
							// Next step is to run the converter. It should be in the same directory as FDO.dll
							var result = ImportFrom6_0Xml(tempXmlPath, folderName, projectFile);
							File.Delete(tempXmlPath);
							DeleteTempDatabase();
							return result;
						}
					}
					// Should never get here, but ...
					MessageBoxUtils.Show(m_progressDlg.Form, Strings.ksZipNotFieldWorksBackup, Strings.ksCannotConvert);
				}
				return false;
			}
		}

		private bool UnzipFile(ZipFile zipFile, ZipEntry entry, string message, out string tempPath)
		{
			string folderName = DirectoryFinder.ProjectsDirectory;
			if (!Directory.Exists(folderName))
				Directory.CreateDirectory(folderName);
			// We will extract the file to here.
			tempPath = Path.Combine(folderName, entry.Name);
			using (var stream = zipFile.GetInputStream(entry))
			{
				m_progressDlg.Title = Strings.ksConverting;
				m_progressDlg.ProgressBarStyle = ProgressBarStyle.Continuous;
				m_progressDlg.Maximum = (int)entry.Size;
				if (File.Exists(tempPath))
					File.Delete(tempPath);	// if we tried and failed earlier, try again.
				using (var tempStream = new FileStream(tempPath, FileMode.CreateNew))
				{
					bool fCanceled = !(bool)m_progressDlg.RunTask(true, ExtractFile, stream, tempStream, message);
					tempStream.Close();
					stream.Close();
					if (fCanceled)
					{
						File.Delete(tempPath);
						return false;
					}
					return true;
				}
			}
		}

		/// <summary>
		/// Check whether the FieldWorks installation of MS SQL Server exists and is running.
		/// </summary>
		/// <returns>true if we're all set to go, false otherwise</returns>
		public bool IsFwSqlServerInstalled()
		{
			// Check for MSSQL$SILFW (the FieldWorks installation of MS SQL Server),
			// and if it's available, ensure that it's running since we'll need it later.
			try
			{
				using (var key = Registry.LocalMachine.OpenSubKey(
					@"SOFTWARE\Microsoft\Microsoft SQL Server\SILFW\MSSQLServer\CurrentVersion", false))
				{
					if (key == null)
						return false;
					string version = key.GetValue("CurrentVersion", null) as string;
					if (version == null)
						return false;
					using (var sc = new ServiceController("MSSQL$SILFW"))
					{
						if (sc.Status.Equals(ServiceControllerStatus.Stopped) ||
							sc.Status.Equals(ServiceControllerStatus.StopPending))
						{
							// Start the service, and wait until its status is "Running".
							sc.Start();
							sc.WaitForStatus(ServiceControllerStatus.Running);
						}
						return true;
					}
				}
			}
			catch (Exception)
			{
				if (m_fVerboseDebug)
					Debug.WriteLine("The FieldWorks installation of SQL Server (MSSQL$SILFW) does not exist.",
						"DEBUG!");
				return false;	// The FieldWorks installation of SQL Server isn't available
			}
		}

		/// <summary>
		/// Check whether various FieldWorks C++ COM classes are registered, and whether the
		/// corresponding DLLs exist.  If so, find the dumpxml.exe program, which will either be
		/// in the same directory (for user machines), or close by (for developer machines), and
		/// save its path for later use.
		/// Also checks that the installation is between version 5.4 and version 6.0, that the
		/// final SQL Migration script exists, and
		/// </summary>
		/// <returns>true if FwCellar.dll, MigrateData.dll, and dumpxml.exe all exist</returns>
		public bool IsValidOldFwInstalled(out string version)
		{
			version = String.Empty;
			try
			{
				using (var clsidKey = Registry.ClassesRoot.OpenSubKey("CLSID"))
				{
					if (clsidKey == null)
					{
						if (m_fVerboseDebug)
							Debug.WriteLine("Unable to open the CLSID registry subkey????");
						return false;
					}
					// check for registered class id for FwXmlData.
					string cellarPath = FindComDllIfRegistered(clsidKey, "{2F0FCCC2-C160-11D3-8DA2-005004DEFEC4}", ref version);
					if (cellarPath == null)
					{
						if (m_fVerboseDebug)
							Debug.WriteLine("FwCellar.dll is not registered.", "DEBUG!");
						return false;
					}
					// check for registered class id for MigrateData.
					string migratePath = FindComDllIfRegistered(clsidKey, "{461989B4-CA92-4EAB-8CAD-ADB28C3B4D10}", ref version);
					if (migratePath == null)
					{
						if (m_fVerboseDebug)
							Debug.WriteLine("MigrateData.dll is not registered.", "DEBUG!");
						return false;
					}
					// check for registered class id for LgWritingSystemFactory.
					string languagePath = FindComDllIfRegistered(clsidKey, "{D96B7867-EDE6-4C0D-80C6-B929300985A6}", ref version);
					if (languagePath == null)
					{
						if (m_fVerboseDebug)
							Debug.WriteLine("Language.dll is not registered.", "DEBUG!");
						return false;
					}
					// check for registered class id for TsStrFactory.
					string kernelPath = FindComDllIfRegistered(clsidKey, "{F1EF76E9-BE04-11D3-8D9A-005004DEFEC4}", ref version);
					if (kernelPath == null)
					{
						if (m_fVerboseDebug)
							Debug.WriteLine("FwKernel.dll is not registered.", "DEBUG!");
						return false;
					}
					// check for registered class id for OleDbEncap.
					string dbaccessPath = FindComDllIfRegistered(clsidKey, "{AAB4A4A3-3C83-11D4-A1BB-00C04F0C9593}", ref version);
					if (dbaccessPath == null)
					{
						if (m_fVerboseDebug)
							Debug.WriteLine("DbAccess.dll is not registered.", "DEBUG!");
						return false;
					}
					// Get (and save) the path to dumpxml.exe.
					string basepath = Path.GetDirectoryName(cellarPath);
					m_dumpxmlPath = Path.Combine(basepath, "dumpxml.exe");
					if (!File.Exists(m_dumpxmlPath))
					{
						// Not found where it should be on a user machine.  Try for where it exists
						// on a developer machine.
						int idxOutput = basepath.IndexOf("\\Output\\");
						if (idxOutput > 0)
						{
							basepath = basepath.Substring(0, idxOutput);
							m_dumpxmlPath = Path.Combine(basepath, "Bin\\dumpxml.exe");
							if (!File.Exists(m_dumpxmlPath))
							{
								if (m_fVerboseDebug)
									Debug.WriteLine("Cannot find dumpxml.exe in the old FieldWorks installation.", "DEBUG!");
								return false;
							}
						}
					}
					// Check for 200259To200260.sql migration script.
					string scriptPath = Path.Combine(basepath, "DataMigration\\200259To200260.sql");
					if (!File.Exists(scriptPath))
					{
						// Not found where it should be on a user machine.  Try for where it exists
						// on a developer machine.
						scriptPath = Path.Combine(basepath, "DistFiles\\DataMigration\\200259To200260.sql");
						if (!File.Exists(scriptPath))
						{
							if (m_fVerboseDebug)
								Debug.WriteLine("Cannot find DataMigration\\200259To200260.sql in the old FieldWorks installation.", "DEBUG!");
							return false;
						}
					}
					m_dbPath = Path.Combine(DirectoryFinder.GetFWCodeSubDirectory("MSSQLMigration"), "db.exe");
					if (!File.Exists(m_dbPath))
					{
						if (m_fVerboseDebug)
							Debug.WriteLine("Cannot find MSSQLMigration\\db.exe in the FieldWorks 7.0 or later installation.", "DEBUG!");
						return false;
					}
					return true;
				}
			}
			catch (Exception e)
			{
				if (m_fVerboseDebug)
				{
					string msg = String.Format(
						"An exception was thrown while checking for an old version of FieldWorks:{1}{0}",
						e.Message, Environment.NewLine);
					Debug.WriteLine(msg, "DEBUG!");
				}
			}
			return false;
		}

		private string FindComDllIfRegistered(RegistryKey clsidKey, string sClsid, ref string version)
		{
			using (RegistryKey cellarKey = clsidKey.OpenSubKey(sClsid))
			{
				if (cellarKey == null)
					return null;
				using (RegistryKey cellarKey2 = cellarKey.OpenSubKey("InprocServer32"))
				{
					if (cellarKey2 == null)
						return null;
					string dllPath = cellarKey2.GetValue("") as string;
					if (dllPath == null || !File.Exists(dllPath))
					{
						if (m_fVerboseDebug)
						{
							string msg = String.Format("Nonexistent file for a registered COM DLL: {0}", dllPath);
							Debug.WriteLine(msg, "DEBUG!");
						}
						return null;
					}
					// The file version of any C++ DLL (such as FwCellar.dll) gives the FieldWorks version.
					// Verify that we have an appropriate version registered.
					FileVersionInfo info = FileVersionInfo.GetVersionInfo(dllPath);
					string fileVersion = TruncateVersion(info.FileVersion);
					if (String.IsNullOrEmpty(version))
						version = fileVersion;
					if (version != fileVersion)
					{
						if (m_fVerboseDebug)
						{
							string msg = String.Format("Multiple versions found in the registered COM DLLs: {0} and {1} [{2}]",
								version, fileVersion, dllPath);
							Debug.WriteLine(msg, "DEBUG!");
						}
						return null; // don't want a mix of versions!!
					}
					if (String.IsNullOrEmpty(version) || version.CompareTo("5.4") < 0 || version.CompareTo("6.1") >= 0)
					{
						if (!String.IsNullOrEmpty(version) && version.CompareTo("5.4") < 0)
						{
							string launchesFlex = "0";
							string launchesTE = "0";
							if (RegistryHelper.KeyExists(FwRegistryHelper.FieldWorksRegistryKey, "Language Explorer"))
							{
								using (RegistryKey keyFlex = FwRegistryHelper.FieldWorksRegistryKey.CreateSubKey("Language Explorer"))
									launchesFlex = keyFlex.GetValue("launches", "0") as string;
							}
							if (RegistryHelper.KeyExists(FwRegistryHelper.FieldWorksRegistryKey, FwSubKey.TE))
							{
								using (RegistryKey keyTE = FwRegistryHelper.FieldWorksRegistryKey.CreateSubKey(FwSubKey.TE))
									launchesTE = keyTE.GetValue("launches", "0") as string;
							}
							if (launchesFlex == "0" && launchesTE == "0")
							{
								FwRegistryHelper.FieldWorksRegistryKey.SetValue("MigrationTo7Needed", "true");
							}
						}
						else if (m_fVerboseDebug)
						{
							string msg = String.Format("Invalid version found in a registered COM DLL: {0} [{1}]",
								version, dllPath);
							Debug.WriteLine(msg, "DEBUG!");
						}
						return null;
					}
					return dllPath;
				}
			}
		}

		/// <summary>
		/// File versions look something like "1.2.3.45678".  We want only the "1.2.3".
		/// </summary>
		/// <param name="fileVersion"></param>
		/// <returns></returns>
		private string TruncateVersion(string fileVersion)
		{
			int idx = fileVersion.IndexOf('.');
			if (idx < 0)
				return fileVersion;
			idx = fileVersion.IndexOf('.', idx + 1);
			if (idx < 0)
				return fileVersion;
			idx = fileVersion.IndexOf('.', idx + 1);
			if (idx < 0)
				return fileVersion;
			else
				return fileVersion.Substring(0, idx);
		}

		/// <summary>
		/// Create a temporary database from a .bak file
		/// It also deletes the .bak file when finished with it.
		/// </summary>
		/// <returns>true if successful, false if cancelled or an error occurs</returns>
		private bool CreateTempDatabase(string pathname, string progressMsg, string errorMsgFmt)
		{
			return CallDbProgram(String.Format("restore {0} \"{1}\"", TempDatabaseName, pathname),
				progressMsg, errorMsgFmt);
		}

		/// <summary>
		/// Make a temporary copy of the project.
		/// </summary>
		/// <returns>true if successful, false if cancelled or an error occurs</returns>
		public bool CopyToTempDatabase(string project, string progressMsg, string errorMsgFmt)
		{
			return CallDbProgram(String.Format("copy \"{0}\" {1}", project, TempDatabaseName),
				progressMsg, errorMsgFmt);
		}

		/// <summary>
		/// Migrate the temporary database to version 200260
		/// </summary>
		/// <returns>true if successful, false if cancelled or an error occurs</returns>
		public bool MigrateTempDatabase(string progressMsg, string errorMsgFmt)
		{
			return CallDbProgram(String.Format("migrate {0}", TempDatabaseName),
				progressMsg, errorMsgFmt);
		}

		/// <summary>
		/// Delete the temporary database (TempForMigration).
		/// </summary>
		/// <returns>true if successfull, false if cancelled or an error occurs</returns>
		public bool DeleteTempDatabase()
		{
			return CallDbProgram(String.Format("delete {0}", TempDatabaseName),
				String.Format(SIL.FieldWorks.FDO.Strings.ksDeletingOldTempDatabase, TempDatabaseName),
				String.Format(SIL.FieldWorks.FDO.Strings.ksDeletingOldTempDatabaseFailed,
					TempDatabaseName, "{0}", "{1}"));
		}

		/// <summary>
		/// Call the db.exe program with the given args, and display the message in the progress
		/// dialog.
		/// </summary>
		/// <returns>true if successful, false if an error occurs</returns>
		private bool CallDbProgram(string args, string progressMsg, string errorMsgFmt)
		{
			using (var process = CreateAndInitProcess(m_dbPath, args))
			{
				m_progressDlg.ProgressBarStyle = ProgressBarStyle.Marquee; // Can't get actual progress from external program
				m_progressDlg.Title = Strings.ksConverting;
				if (!(bool)m_progressDlg.RunTask(true, ProcessFile, process, progressMsg))
				{
					// user canceled
					return false;
				}
				if (process.ExitCode != 0 || !string.IsNullOrEmpty(m_errorMessages))
				{
					var msg = string.Format(errorMsgFmt, process.ExitCode, m_errorMessages);
					MessageBoxUtils.Show(m_progressDlg.Form, msg, Strings.ksCannotConvert);
					return false;
				}
				return true;
			}
		}

		/// <summary>
		/// Dump the XML file from the given database.
		/// </summary>
		/// <returns>true if successful, false if cancelled or an error occurs</returns>
		public bool DumpDatabaseAsXml(string dbName, string tempXmlPath, string progressMsg,
			string errorMsgFmt)
		{
			using (var process = CreateAndInitProcess(m_dumpxmlPath, "-d \"" + dbName + "\" -o \"" + tempXmlPath + '"'))
			{
				m_progressDlg.ProgressBarStyle = ProgressBarStyle.Marquee; // Can't get actual progress from external program
				m_progressDlg.Title = Strings.ksConverting;
				if (!(bool)m_progressDlg.RunTask(true, ProcessFile, process, progressMsg))
				{
					// user canceled
					return false;
				}
				if (process.ExitCode != 0 || !string.IsNullOrEmpty(m_errorMessages))
				{
					var msg = string.Format(errorMsgFmt, process.ExitCode, m_errorMessages);
					MessageBoxUtils.Show(m_progressDlg.Form, msg, Strings.ksCannotConvert);
					return false;
				}
				return true;
			}
		}

		/// <summary>
		/// Test whether the given file is a FieldWorks 6.0 XML file.
		/// </summary>
		private bool IsValid6_0Xml(string pathname)
		{
			int version = FieldWorksXmlVersion(pathname);
			return version >= 200260 && version < 200300;	// upper bound is overgenerous
		}

		/// <summary>
		/// Get the version number from a FieldWorks XML file (version 6.0 or earlier).
		/// </summary>
		/// <returns>version number recorded in file, or -1 if an error occurs</returns>
		public static int FieldWorksXmlVersion(string pathname)
		{
			try
			{
				using (StreamReader reader = new StreamReader(pathname))
				{
					string line = reader.ReadLine();
					if (line.ToLowerInvariant() != "<?xml version=\"1.0\" encoding=\"utf-8\"?>")
						return -1;
					line = reader.ReadLine();
					if (line != "<!DOCTYPE FwDatabase SYSTEM \"FwDatabase.dtd\">")
						return -1;
					line = reader.ReadLine();
					if (!line.StartsWith("<FwDatabase version=\""))
						return -1;
					const int idxMin = 21;
					int idxLim = line.IndexOf('"', idxMin);
					if (idxLim <= idxMin)
						return -1;
					string sVersion = line.Substring(idxMin, idxLim - idxMin);
					int version = Int32.Parse(sVersion);
					return version;
				}
			}
			catch (Exception)
			{
				return -1;
			}
		}

		/// <summary>
		/// Check whether a suitable old version of FieldWorks is installed.
		/// </summary>
		public bool HaveOldFieldWorks
		{
			get
			{
				if (!m_fCheckedForOldFieldWorks)
				{
					string version;
					m_fHaveOldFieldWorks = IsValidOldFwInstalled(out version);
					m_fCheckedForOldFieldWorks = true;
				}
				return m_fHaveOldFieldWorks;
			}
		}

		/// <summary>
		/// Check whether the FieldWorks instance of SQL Server is installed.
		/// </summary>
		public bool HaveFwSqlServer
		{
			get
			{
				if (!m_fCheckedForSqlServer)
				{
					m_fHaveSqlServer = IsFwSqlServerInstalled();
					m_fCheckedForSqlServer = true;
				}
				return m_fHaveSqlServer;
			}
		}

		/// <summary>
		/// Import from a 6.0 XML file.
		/// </summary>
		public bool ImportFrom6_0Xml(string pathname, string folderName, string projectFile)
		{
			bool retval = true;
			bool fCreateFolder = !Directory.Exists(folderName);
			string replacedProj = null;
			if (fCreateFolder)
			{
				Directory.CreateDirectory(folderName);
			}
			else if (File.Exists(projectFile))
			{
				replacedProj = projectFile + "-replaced";
				if (File.Exists(replacedProj))
					File.Delete(replacedProj);
				File.Move(projectFile, replacedProj);
			}
			using (var process = CreateAndInitProcess(
				Path.Combine(DirectoryFinder.FWCodeDirectory, "ConverterConsole.exe"),
				'"' + pathname + "\" \"" + projectFile + '"'))
			{
				m_progressDlg.ProgressBarStyle = ProgressBarStyle.Marquee; // Can't get actual progress from external program
				m_progressDlg.Title = Strings.ksConverting;
				string message = String.Format(Strings.ksConvertingFile, Path.GetFileNameWithoutExtension(projectFile));
				if (!(bool)m_progressDlg.RunTask(true, ProcessFile, process, message))
				{
					// user canceled
					retval = false;
				}
				else if (process.ExitCode != 0 || !String.IsNullOrEmpty(m_errorMessages))
				{
					message = String.Format(Strings.ksConversionProcessFailed, process.ExitCode, m_errorMessages);
					// ENHANCE (TimS): We should not be showing a message box at this level. If we
					// really need to show it here, we should pass in the owning form instead of relying on
					// Form.ActiveForm since it can return null if no .Net forms have focus.
					MessageBoxUtils.Show(Form.ActiveForm, message, Strings.ksCannotConvert);
					retval = false;
				}
				if (retval == false)
				{
					File.Delete(projectFile);
					if (fCreateFolder)
						Directory.Delete(folderName);
					else if (replacedProj != null)
						File.Move(replacedProj, projectFile);
				}
				else
				{
					try
					{
						string srcDir = Path.GetDirectoryName(pathname);
						string destDir = Path.GetDirectoryName(projectFile);
						if (srcDir == destDir)
						{
							File.Delete(pathname);
							string logfile = pathname.Replace(".xml", "-Export.log");
							File.Delete(logfile);
						}
						FdoCache.CreateProjectSubfolders(folderName);
					}
					catch (Exception)
					{
						// ignore any exceptions thrown when trying to create the subdirectories.
					}
				}
				return retval;
			}
		}

		/// <summary>
		/// Create a process and initialize it according to our standard way of doing things.
		/// </summary>
		/// <param name="programPath">path to the program to execute</param>
		/// <param name="programArgs">command line arguments for the program</param>
		/// <returns>the Process object created</returns>
		public Process CreateAndInitProcess(string programPath, string programArgs)
		{
			var process = new Process();
			var processInfo = process.StartInfo;
			processInfo.Arguments = programArgs;
			processInfo.FileName = programPath; // program to run
			processInfo.CreateNoWindow = true;
			processInfo.WindowStyle = ProcessWindowStyle.Hidden;
			processInfo.UseShellExecute = false;
			processInfo.RedirectStandardError = true;
			processInfo.RedirectStandardOutput = true;
			process.OutputDataReceived += new DataReceivedEventHandler(process_OutputDataReceived);
			process.ErrorDataReceived += new DataReceivedEventHandler(process_ErrorDataReceived);
			return process;
		}

		/// <summary>
		/// Method to be the task executed by ProgressDialogWithTask.RunTask; must have this exact signature.
		/// Takes two parameters, as indicated by the first two lines of code.
		/// </summary>
		/// <returns>true if completed, false if canceled</returns>
		private object ExtractFile(IThreadedProgress progressDlg, object[] parameters)
		{
			var stream = (Stream)parameters[0];
			FileStream tempStream = (FileStream)parameters[1];
			progressDlg.Message = (string)parameters[2];
			const int interval = 100000; // update progress every 100K.
			int count = 0;
			for (int oneByte = stream.ReadByte(); oneByte != -1; oneByte = stream.ReadByte())
			{
				tempStream.WriteByte((byte) oneByte);
				if (count++ % interval == 0)
				{
					progressDlg.Position = count;
					if (progressDlg.Canceled)
						return false;
				}
			}
			return true;
		}
		/// <summary>
		/// Method to be the task executed by ProgressDialogWithTask.RunTask; must have this exact signature.
		/// Takes one parameter, as indicated by the first line of code.
		/// </summary>
		/// <returns>true if completed, false if canceled</returns>
		public object ProcessFile(IThreadedProgress progressDlg, object[] parameters)
		{
			var process = (Process) parameters[0];
			progressDlg.Message = (string)parameters[1];
			m_errorMessages = "";
			m_stdoutBldr = new StringBuilder();
			process.Start();
			process.BeginErrorReadLine();
			process.BeginOutputReadLine();
			while (!process.WaitForExit(300))
			{
				if (progressDlg.Canceled)
				{
					process.Kill();
					process.WaitForExit();
					return false;
				}
			}
			return true;
		}

		void process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
		{
			if (e.Data != null)
			{
				if (string.IsNullOrEmpty(m_errorMessages))
					m_errorMessages = e.Data;
				else
				{
					m_errorMessages += ' ' + e.Data;
				}
			}
		}

		void process_OutputDataReceived(object sender, DataReceivedEventArgs e)
		{
			if (e.Data != null)
				m_stdoutBldr.AppendLine(e.Data);
		}
	}
}
