using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.ServiceProcess;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Windows.Forms;
using Microsoft.Win32;                  // for RegistryKey

namespace MoveData
{
	static class Program
	{
		/// <summary>
		/// Main program that moves FW data from the "current" folder (as specified in
		/// HKEY_LOCAL_MACHINE\Software\SIL\FieldWorks::RootDataDir) to the folder
		/// specified as the first command line argument. If you want to override
		/// the "current" folder, then pass it on the command line as a second argument.
		/// </summary>
		/// <param name="args">array of strings that are the arguments on the command line.
		/// </param>
		static int Main(string[] args)
		{
			try
			{
				// Deal with command line arguments:
				if (args.Length == 0)
				{
					Usage();
					return 1;
				}
				if (args.Length > 0)
					Globals.strTargetDir = args[0];

				RegistryKey keyDataRoot = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\SIL\FieldWorks", true);

				if (args.Length > 1)
					Globals.strSourceDir = args[1];
				else
					Globals.strSourceDir = (string)keyDataRoot.GetValue("RootDataDir");

				if (!Directory.Exists(Globals.strSourceDir))
				{
					// Source directory does not exist. We can't even log this!
					return 0;
				}

				Log("Starting to move FieldWorks data from " + Globals.strSourceDir + " to " + Globals.strTargetDir + ".");

				Log("Making sure SQL Server is running...");
				StartDB();
				Log("...Done.");

				// Get all the SQL databases. Note that this will include system DBs like "master",
				// but because they aren't in the FW data folder, they won't get disturbed.
				Log("Fetching all database names...");
				ArrayList DBs = GetDatabases();
				Log("...Done.");

				InsureFolderExists(Globals.DbTargetFolder);

				Log("Copying databases...");
				foreach (string DB in DBs)
					CopyDB(DB, DB);
				Log("...Done.");

				Log("Copying Data folder...");
				CopyFwFolderContents("Data");
				Log("...Done.");

				Log("Copying Media folder...");
				CopyFwFolderContents("Media");
				Log("...Done.");

				Log("Copying Pictures folder...");
				CopyFwFolderContents("Pictures");
				Log("...Done.");

				Log("Copying Languages folder...");
				CopyFwFolderContents("Languages");
				Log("...Done.");

				Log("Setting RootDataDir to " + Globals.strTargetDir + "...");
				keyDataRoot.SetValue("RootDataDir", Globals.strTargetDir);
				Log("...Done.");

				Log("Setting DbDir to " + Globals.DbTargetFolder + "...");
				keyDataRoot.SetValue("DbDir", Globals.DbTargetFolder);
				Log("...Done.");
			}
			catch (Exception e)
			{
				Log("Error: " + e.Message);
				MessageBox.Show("Moving FieldWorks data has failed. See the log file for more details: " + Globals.strSourceDir + "\\MoveData.log",
					"MoveData");
				return -1;
			}
			if (Globals.ctWarnings > 0)
				MessageBox.Show("The utility to move FieldWorks data has finished, but with warnings. See the log file for more details: " + Globals.strSourceDir + "\\MoveData.log");
			return 0;
		}

		static public void Usage()
		{
			MessageBox.Show("MoveData <new-location> [<old-location>]\nRelocates FieldWorks data.\n<new-location> : full folder path where data should go\n<old-location> : (optional, overrides HKEY_LOCAL_MACHINE\\Software\\SIL\\FieldWorks::RootDataDir) location where data currently resides.",
				"MoveData");
		}

		static public void StartDB()
		{
			ServiceController oSC = new ServiceController("MSSQL$SILFW");
			if (oSC.Status != ServiceControllerStatus.StartPending && oSC.Status != ServiceControllerStatus.Running)
				oSC.Start();
		}

		/// <summary>
		/// Returns a string array listing all the SQL databases currently attached.
		/// </summary>
		static public ArrayList GetDatabases()
		{
			ArrayList retVal = new ArrayList();
			string ssql = "select name from sysdatabases order by name";
			using (SqlConnection oConn = Globals.Conn)
			using (SqlCommand oCommand = new SqlCommand(ssql, oConn))
			using (SqlDataReader oReader = oCommand.ExecuteReader())
			{
				while (oReader.Read())
				{
					string DB = oReader.GetString(0);
					Log(DB);
					retVal.Add(DB);
				}
			}
			return retVal;
		}

		static public void CopyDB(string sSourceDatabaseName, string sTargetDatabaseName)
		{
			string sDBLogFileExt = "";
			if (File.Exists(Globals.DbSourceFolder + sSourceDatabaseName + ".ldf"))
				sDBLogFileExt = ".ldf";
			if (File.Exists(Globals.DbSourceFolder + sSourceDatabaseName + "_log.ldf"))
				sDBLogFileExt = "_log.ldf";

			if (!File.Exists(Globals.DbSourceFolder + sSourceDatabaseName + ".mdf"))
			{
				Log(sSourceDatabaseName + " does not exist in " + Globals.DbSourceFolder);
				return;
			}

			if (File.Exists(Globals.DbTargetFolder + sTargetDatabaseName + ".mdf") ||
				File.Exists(Globals.DbTargetFolder + sTargetDatabaseName + sDBLogFileExt))
			{
				Log("WARNING: "+ sTargetDatabaseName + " already exists in " + Globals.DbTargetFolder + " - copy did not take place.");
				Globals.ctWarnings++;
				return;
			}

			DetachDB(sSourceDatabaseName);

			Log("Copying " + sSourceDatabaseName + " from " + Globals.DbSourceFolder + " to " + Globals.DbTargetFolder + ".");
			string sSourcePath = Globals.DbSourceFolder + sSourceDatabaseName + ".mdf";
			File.Copy(sSourcePath, Globals.DbTargetFolder + sTargetDatabaseName + ".mdf");

			Log("Renaming original to " + sSourcePath + ".old");
			File.Move(sSourcePath, sSourcePath + ".old");
			if (sDBLogFileExt.Length > 0)
			{
				Log("Copying " + sSourceDatabaseName + sDBLogFileExt + " from " + Globals.DbSourceFolder + " to " + Globals.DbTargetFolder + ".");
				sSourcePath = Globals.DbSourceFolder + sSourceDatabaseName + sDBLogFileExt;
				File.Copy(sSourcePath, Globals.DbTargetFolder + sTargetDatabaseName + sDBLogFileExt);

				Log("Renaming original to " + sSourcePath + ".old");
				File.Move(sSourcePath, sSourcePath + ".old");
			}

			AttachDB(sTargetDatabaseName);
		}

		static public void DetachDB(string sDatabaseName)
		{
			Log("Detaching " + sDatabaseName);

			string ssql = "exec sp_detach_db " + Globals.CSql(sDatabaseName);
			using (SqlConnection oConn = Globals.Conn)
			{
				SqlCommand oCommand = new SqlCommand(ssql, oConn);
				oCommand.ExecuteNonQuery();
			}
		}

		static public void AttachDB(string sDatabaseName)
		{
			Log("Attaching " + sDatabaseName + "...");

			using (SqlConnection oConn = Globals.Conn)
			{
				// Remove the extension from the end if it is passed.
				if (sDatabaseName.EndsWith(".mdf"))
					sDatabaseName = sDatabaseName.Substring(0, sDatabaseName.Length - 4);

				string sLogFile = "";
				if (File.Exists(Globals.DbTargetFolder + sDatabaseName + ".ldf"))
					sLogFile = Globals.DbTargetFolder + sDatabaseName + ".ldf";
				if (File.Exists(Globals.DbTargetFolder + sDatabaseName + "_log.ldf"))
					sLogFile = Globals.DbTargetFolder + sDatabaseName + "_log.ldf";

				string ssql =
					"exec sp_attach_db " +
					"   @dbname = " + Globals.CSql(sDatabaseName) + ", " +
					"   @filename1 = " + Globals.CSql(Globals.DbTargetFolder + sDatabaseName + ".mdf");
				if (sLogFile.Length > 0)
					ssql += ", @filename2 = " + Globals.CSql(sLogFile);

				SqlCommand oCommand = new SqlCommand(ssql, oConn);
				oCommand.ExecuteNonQuery();
			}
			Log("...Done.");
		}

		/// <summary>
		/// This method can be used to move all of the files in the given sub-folder (and all sub-folders)
		/// from the old FW location to the new one.
		/// </summary>
		/// <param name="strSubFolder">e.g. "Pictures"</param>
		static void CopyFwFolderContents(string strSubFolder)
		{
			string strSourcePath = Globals.strSourceDir + "\\" + strSubFolder;
			string strTargetPath = Globals.strTargetDir + "\\" + strSubFolder;
			if (Directory.Exists(strSourcePath))
			{
				Log("Copying files from " + strSourcePath + " to " + strTargetPath + "...");

				string[] astrFilesInSource = Directory.GetFiles(strSourcePath, "*.*", SearchOption.AllDirectories);

				foreach (string strFileInSource in astrFilesInSource)
				{
					// Make sure file is not already marked with the .old extension,
					// and isn't a .bak file (some users have too many and they are not normally needed):
					if (strFileInSource.EndsWith(".old") || strFileInSource.EndsWith(".bak"))
						Log("Not copying " + strFileInSource);
					else
					{
						string strFileInTarget = strTargetPath + strFileInSource.Substring(strSourcePath.Length);
						FileCopy(strFileInSource, strFileInTarget);
					}
				}
				Log("...Done (" + strSourcePath + ").");
			}
		}

		/// <summary>
		/// This method can be used to move a file from the source location to the target location (if it doesn't
		/// already exist in the target location)
		/// </summary>
		/// <param name="strFileInSource">file spec to move (e.g. "C:\Program Files\Common Files\MapsTables\silipa93.tec")</param>
		/// <param name="strFileInTarget">file spec of the moved file (e.g. "C:\Documents and Settings\All Users\Application Data\MapsTables\silipa93.tec")</param>
		static void FileCopy(string strFileInSource, string strFileInTarget)
		{
			InsureFolderExists(Path.GetDirectoryName(strFileInTarget));
			if (!File.Exists(strFileInTarget))
			{
				Log("Copying " + strFileInSource + " to " + strFileInTarget);
				File.Copy(strFileInSource, strFileInTarget);

				Log("Renaming " + strFileInSource + " to " + strFileInSource + ".old");
				File.Move(strFileInSource, strFileInSource + ".old");
			}
			else
			{
				Log("WARNING: " + strFileInTarget + " already exists - did not copy " + strFileInSource + ".");
				Globals.ctWarnings++;
			}
		}

		static void InsureFolderExists(string strFolderPath)
		{
			Log("Making sure folder " + strFolderPath + " exists...");
			bool fAssignedACL = false;
			_InsureFolderExists(strFolderPath, ref fAssignedACL);
			Log("...Done." + (fAssignedACL ? " Assigned" : " Did not assign") + " full permissions for authenticated users.");
		}

		/// <summary>
		/// This method can be used to insure that the parent folder corresponding to the given filename exists
		/// </summary>
		/// <param name="strFileSpec">the file spec whose parent folder's existence is to be insured (e.g. "C:\Documents and Settings\All Users\Application Data\MapsTables\silipa93.tec" to insure that the folder "C:\Documents and Settings\All Users\Application Data\MapsTables" is created)</param>
		static void _InsureFolderExists(string strFolderPath, ref bool fAssignedACL)
		{
			if (!Directory.Exists(strFolderPath))
			{
				_InsureFolderExists(Directory.GetParent(strFolderPath).FullName, ref fAssignedACL);

				Log("Creating folder " + strFolderPath);
				Directory.CreateDirectory(strFolderPath);

				if (!fAssignedACL)
				{
					Log("Assigning full permissions for authenticated users on folder " + strFolderPath + "...");
					// Set permissions on the new folder(s) so that all authenticated users get full control:
					DirectorySecurity dSecurity = Directory.GetAccessControl(strFolderPath);

					SecurityIdentifier sid = new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null);
					FileSystemAccessRule rule = new FileSystemAccessRule(sid,
						FileSystemRights.FullControl, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
						PropagationFlags.None, AccessControlType.Allow);
					dSecurity.AddAccessRule(rule);
					Directory.SetAccessControl(strFolderPath, dSecurity);
					fAssignedACL = true;
					Log("...Done.");
				}
			}
			else
				Log("Folder " + strFolderPath + " exists.");
		}

		/// <summary>
		/// Adds a message to the Log file
		/// </summary>
		/// <param name="Msg"></param>
		static void Log(string Msg)
		{
			StreamWriter fsLog = File.AppendText(Globals.strSourceDir + "\\MoveData.log");
			fsLog.WriteLine(DateTime.Now + " :  " + Msg);
			fsLog.Close();
		}
	}
}

public class Globals
{
	static public string strTargetDir = null;
	static public string strSourceDir = null;
	static private string m_sDbSourceFolder = null;
	static private string m_sDbTargetFolder = null;
	static private string m_sServer = Environment.MachineName + "\\SILFW";
	static public int ctWarnings = 0;

	static public string Server
	{
		get
		{
			return m_sServer;
		}
	}

	static public SqlConnection Conn
	{
		get
		{
			string sConnectionString =
				"Server=" + m_sServer + "; Database=master; User ID = sa;" +
				"Password=inscrutable; Connect Timeout = 2; Pooling=false;";
			SqlConnection oConn = new SqlConnection(sConnectionString);
			oConn.Open();
			return oConn;
		}
	}

	static public string DbSourceFolder
	{
		get
		{
			if (m_sDbSourceFolder == null)
			{
				m_sDbSourceFolder = strSourceDir;
				if (!m_sDbSourceFolder.EndsWith("\\"))
					m_sDbSourceFolder += "\\";
				m_sDbSourceFolder += "Data\\";
			}
			return m_sDbSourceFolder;
		}
	}

	static public string DbTargetFolder
	{
		get
		{
			if (m_sDbTargetFolder == null)
			{
				m_sDbTargetFolder = strTargetDir;
				if (!m_sDbTargetFolder.EndsWith("\\"))
					m_sDbTargetFolder += "\\";
				m_sDbTargetFolder += "Data\\";
			}
			return m_sDbTargetFolder;
		}
	}

	static public string CSql(string sValue)
	{
		return "'" + sValue.Replace("'", "''") + "'";
	}
}
