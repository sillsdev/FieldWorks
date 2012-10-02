using System;
using System.Data.SqlClient;
using System.Collections;
using System.IO;
using System.ServiceProcess;
using System.Diagnostics;
using System.Collections.Specialized; // Needed for StringCollection.

namespace DBProgram
{
	public class Globals
	{
		static private string m_sDbFolder = null;
		static private string m_sTemplateFolder = null;
		static private string m_sServer = Environment.MachineName + "\\SILFW";

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
				// Note: Integrated Security setting allows a database to be attached when the database was
				// detached with different credentials (e.g., Windows Authentication in Management Studio as
				// opposed to using the SA login). Unfortunately, on Vista if UAC is enabled, this fails.
				// See TE-6601 for details. Until this can be resolved, we've reverted all changes.
				// Changeset 23030 contains the changes that were made in addition to the one in InitMSDE
				// that David Olson made.
				string sConnectionString =
					"Server=" + m_sServer + "; Database=master; User ID = sa;" +
					//"Password=inscrutable; Integrated Security=SSPI; Pooling=false;";
					"Password=inscrutable; Pooling=false;";
				SqlConnection oConn = new SqlConnection(sConnectionString);
				oConn.Open();
				return oConn;
			}
		}

		static public string DbFolder
		{
			get
			{
				if (m_sDbFolder == null)
				{
					m_sDbFolder = "%ALLUSERSPROFILE%\\Application Data\\SIL\\FieldWorks\\Data\\";
					m_sDbFolder = Environment.ExpandEnvironmentVariables(m_sDbFolder);
					Microsoft.Win32.RegistryKey oKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("Software\\SIL\\FieldWorks");
					if (oKey != null)
					{
						object oFolder = oKey.GetValue("DbDir");
						if (oFolder != null)
							m_sDbFolder = oFolder.ToString();
					}
					if (!m_sDbFolder.EndsWith("\\"))
						m_sDbFolder += "\\";
				}
				return m_sDbFolder;
			}
		}

		static public string TemplateFolder
		{
			get
			{
				if (m_sTemplateFolder == null)
				{
					m_sTemplateFolder = "c:\\Program Files\\FieldWorks\\";
					Microsoft.Win32.RegistryKey oKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("Software\\SIL\\FieldWorks");
					if (oKey != null)
					{
						object oFolder = oKey.GetValue("RootCodeDir");
						if (oFolder != null)
							m_sTemplateFolder = oFolder.ToString();
					}
					if (!m_sTemplateFolder.EndsWith("\\"))
						m_sTemplateFolder += "\\";
					m_sTemplateFolder += "templates\\";
				}
				return m_sTemplateFolder;
			}
		}

		static public string CSqlN(string sValue)
		{
			// Need N to interpret Unicode names properly.
			return "N'" + sValue.Replace("'", "''") + "'";
		}

		static public string CSql(string sValue)
		{
			// Unicode for connection strings doesn't use N.
			return "'" + sValue.Replace("'", "''") + "'";
		}

		static public bool DatabaseExists(string sDatabaseName)
		{
			string ssql = "select name from sysdatabases where name = " + Globals.CSqlN(sDatabaseName) + "";
			using (SqlConnection oConn = Globals.Conn)
			using (SqlCommand oCommand = new SqlCommand(ssql, oConn))
			using (SqlDataReader oReader = oCommand.ExecuteReader())
			{
				if (oReader.Read() && oReader.GetString(0).ToLower() == sDatabaseName.ToLower())
					return true;
			}
			Console.WriteLine("Database \"" + sDatabaseName + "\" does not exist.");
			return false;
		}
	}

	/// <summary>
	/// This is the class that will interface with the Database
	/// </summary>
	public class DBInterface
	{
		/// <summary>
		/// List all the database names.
		/// </summary>
		static public void ListAllDatabases()
		{
			string ssql = "select name from sysdatabases order by name";
			using (SqlConnection oConn = Globals.Conn)
			using (SqlCommand oCommand = new SqlCommand(ssql, oConn))
			using (SqlDataReader oReader = oCommand.ExecuteReader())
			{
				while (oReader.Read())
					Console.WriteLine(oReader.GetString(0));
			}
		}

		/// <summary>
		/// List only the FieldWorks database names.
		/// </summary>
		static public void ListFwDatabases()
		{
			string ssql = "exec sp_GetFWDBs";
			using (SqlConnection oConn = Globals.Conn)
			using (SqlCommand oCommand = new SqlCommand(ssql, oConn))
			using (SqlDataReader oReader = oCommand.ExecuteReader())
			{
				while (oReader.Read())
					Console.WriteLine(oReader.GetString(0));
			}
		}

		/// <summary>
		/// List logins available in master
		/// </summary>
		static public void ListLogins()
		{
			string ssql = "select name from syslogins";
			using (SqlConnection oConn = Globals.Conn)
			using (SqlCommand oCommand = new SqlCommand(ssql, oConn))
			using (SqlDataReader oReader = oCommand.ExecuteReader())
			{
				while (oReader.Read())
					Console.WriteLine(oReader.GetString(0));
			}
		}

		/// <summary>
		/// //list master stored procedures used by FW
		/// </summary>
		static public void ListProcs()
		{
			string ssql = "select name from sysobjects where id = object_id('sp_GetFWDBs') or id = object_id('sp_DbStartup')";
			using (SqlConnection oConn = Globals.Conn)
			using (SqlCommand oCommand = new SqlCommand(ssql, oConn))
			using (SqlDataReader oReader = oCommand.ExecuteReader())
			{
				while (oReader.Read())
					Console.WriteLine(oReader.GetString(0));
			}
		}

		/// <summary>
		/// show current DB version, from dbname pass as arg
		/// </summary>
		static public void GetDBVersion(string sDatabaseName)
		{
			if (!Globals.DatabaseExists(sDatabaseName))
				return;
			string ssql = "select DbVer from [" + sDatabaseName +  "].dbo.Version$";
			using (SqlConnection oConn = Globals.Conn)
			using (SqlCommand oCommand = new SqlCommand(ssql, oConn))
			using (SqlDataReader oReader = oCommand.ExecuteReader())
			{
				while (oReader.Read())
					Console.WriteLine(oReader[0].ToString());
			}
		}

		/// <summary>
		/// shrink the DB whose name is attached
		/// </summary>
		static public void ShrinkDB(string sDatabaseName)
		{
			if (!Globals.DatabaseExists(sDatabaseName))
				return;
			string ssql = "alter database [" + sDatabaseName + "] set recovery simple";
			using (SqlConnection oConn = Globals.Conn)
			using (SqlCommand oCommand = new SqlCommand(ssql, oConn))
			{
				oCommand.ExecuteNonQuery();

				oCommand.CommandText = "alter database [" + sDatabaseName + "] set auto_shrink on";
				oCommand.ExecuteNonQuery();

				oCommand.CommandText = "DBCC shrinkdatabase ([" + sDatabaseName +  "])";
				oCommand.ExecuteNonQuery();
			}
		}

		static public void AttachDB(string sDatabaseName)
		{
			using (SqlConnection oConn = Globals.Conn)
			{
				// Remove the extension from the end if it is passed.
				if (sDatabaseName.EndsWith(".mdf"))
					sDatabaseName = sDatabaseName.Substring(0, sDatabaseName.Length - 4);
				string sDbFile = Globals.DbFolder + sDatabaseName + ".mdf";
				if (!File.Exists(sDbFile))
				{
					Console.WriteLine("File \"" + sDbFile + "\" does not exist.");
					return;
				}
				// Make sure db files are not readonly. SQL Server does nasty things in this case.
				if ((File.GetAttributes(sDbFile) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
					File.SetAttributes(sDbFile, File.GetAttributes(sDbFile) & ~FileAttributes.ReadOnly);

				string sLogFile = "";
				if (File.Exists(Globals.DbFolder + sDatabaseName + ".ldf"))
					sLogFile = Globals.DbFolder + sDatabaseName + ".ldf";
				if (File.Exists(Globals.DbFolder + sDatabaseName + "_log.ldf"))
					sLogFile = Globals.DbFolder + sDatabaseName + "_log.ldf";
				if (sLogFile.Length > 0 && (File.GetAttributes(sLogFile) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
					File.SetAttributes(sLogFile, File.GetAttributes(sLogFile) & ~FileAttributes.ReadOnly);

				string ssql =
					"exec sp_attach_db " +
					"   @dbname = " + Globals.CSqlN(sDatabaseName) + ", " +
					"   @filename1 = " + Globals.CSqlN(sDbFile);
				if (sLogFile.Length > 0)
					ssql += ", @filename2 = " + Globals.CSqlN(sLogFile);

				SqlCommand oCommand = new SqlCommand(ssql, oConn);
				oCommand.ExecuteNonQuery();
			}
		}

		static public void DetachDB(string sDatabaseName)
		{
			if (!Globals.DatabaseExists(sDatabaseName))
				return;
			string ssql = "exec sp_detach_db " + Globals.CSqlN(sDatabaseName);
			using (SqlConnection oConn = Globals.Conn)
			{
				SqlCommand oCommand = new SqlCommand(ssql, oConn);
				oCommand.ExecuteNonQuery();
			}
		}

		static public void CopyDB(string sSourceDatabaseName, string sTargetDatabaseName)
		{
			if (!Globals.DatabaseExists(sSourceDatabaseName))
				return;
			bool fAttachSourceDb = false;
			try
			{
				string sDBLogFileExt = "";
				if (File.Exists(Globals.DbFolder + sSourceDatabaseName +  ".ldf"))
					sDBLogFileExt = ".ldf";
				if (File.Exists(Globals.DbFolder + sSourceDatabaseName +  "_log.ldf"))
					sDBLogFileExt = "_log.ldf";

				if (!File.Exists(Globals.DbFolder + sSourceDatabaseName + ".mdf"))
				{
					Console.WriteLine("Cannot copy " + sSourceDatabaseName + " to " + sTargetDatabaseName + " because the source database does not exist.");
					return;
				}
				if (File.Exists(Globals.DbFolder + sTargetDatabaseName + ".mdf") ||
					File.Exists(Globals.DbFolder + sTargetDatabaseName + sDBLogFileExt))
				{
					Console.WriteLine("Cannot copy " + sSourceDatabaseName + " to " + sTargetDatabaseName + " because the target database already exists.");
					return;
				}

				DetachDB(sSourceDatabaseName);
				fAttachSourceDb = true;

				File.Copy(Globals.DbFolder + sSourceDatabaseName + ".mdf", Globals.DbFolder + sTargetDatabaseName + ".mdf");
				if (sDBLogFileExt.Length > 0)
					File.Copy(Globals.DbFolder + sSourceDatabaseName + sDBLogFileExt, Globals.DbFolder + sTargetDatabaseName + sDBLogFileExt);

				AttachDB(sTargetDatabaseName);
			}
			finally
			{
				if (fAttachSourceDb)
					AttachDB(sSourceDatabaseName);
			}
		}

		static public void RenameDB(string sSourceDatabaseName, string sTargetDatabaseName)
		{
			if (!Globals.DatabaseExists(sSourceDatabaseName))
				return;
			string sDBLogFileExt = "";
			if (File.Exists(Globals.DbFolder + sSourceDatabaseName +  ".ldf"))
				sDBLogFileExt = ".ldf";
			if (File.Exists(Globals.DbFolder + sSourceDatabaseName +  "_log.ldf"))
				sDBLogFileExt = "_log.ldf";

			if (!File.Exists(Globals.DbFolder + sSourceDatabaseName + ".mdf"))
			{
				Console.WriteLine("Cannot rename " + sSourceDatabaseName + " to " + sTargetDatabaseName + " because the source database does not exist.");
				return;
			}
			if (File.Exists(Globals.DbFolder + sTargetDatabaseName + ".mdf") ||
				File.Exists(Globals.DbFolder + sTargetDatabaseName + sDBLogFileExt))
			{
				Console.WriteLine("Cannot rename " + sSourceDatabaseName + " to " + sTargetDatabaseName + " because the target database already exists.");
				return;
			}

			bool fAttachDB = false;
			try
			{
				DetachDB(sSourceDatabaseName);
				fAttachDB = true;

				File.Move(Globals.DbFolder + sSourceDatabaseName + ".mdf", Globals.DbFolder + sTargetDatabaseName + ".mdf");
				File.Move(Globals.DbFolder + sSourceDatabaseName + sDBLogFileExt, Globals.DbFolder + sTargetDatabaseName + sDBLogFileExt);
				sSourceDatabaseName = sTargetDatabaseName;
			}
			finally
			{
				if (fAttachDB)
					AttachDB(sSourceDatabaseName);
			}
		}

		static public void BackupDB(string sDatabaseName, string sBackupFilename)
		{
			if (!Globals.DatabaseExists(sDatabaseName))
				return;
			// If the optional filename is not included, default to the database name.
			if (sBackupFilename == null)
				sBackupFilename = sDatabaseName + ".bak";
			// If the filename does not include the path, default to the DbDir path.
			if (sBackupFilename.IndexOf('\\') == -1)
				sBackupFilename = Globals.DbFolder + sBackupFilename;

			string ssql = "backup database [" + sDatabaseName +  "] to disk = " + Globals.CSqlN(sBackupFilename) + " with init";
			using (SqlConnection oConn = Globals.Conn)
			using (SqlCommand oCommand = new SqlCommand(ssql, oConn))
			{
				oCommand.ExecuteNonQuery();
			}
		}

		static public void RestoreDB(string sDatabaseName, string sBackupFilename)
		{
			// If the optional filename is not included, default to the database name.
			if (sBackupFilename == null)
				sBackupFilename = sDatabaseName + ".bak";
			// If the filename does not include the path, default to the DbDir path.
			if (sBackupFilename.IndexOf('\\') == -1)
				sBackupFilename = Globals.DbFolder + sBackupFilename;

			if (!File.Exists(sBackupFilename))
			{
				Console.WriteLine("Cannot restore " + sBackupFilename + " because the backup file does not exist.");
				return;
			}

			using (SqlConnection oConn = Globals.Conn)
			{
				// Under some rare situations, the restore fails if the database already exists, even with move
				// and replace. It may have something to do with duplicate logical names, although that's not
				// consistent. So to be safe, we'll delete the database first if it is present.
				string ssql = "If exists (select name from sysdatabases where name = '" + sDatabaseName + "') " +
					"begin " +
					  "drop database [" + sDatabaseName + "] " +
					"end";
				using (SqlCommand oCommand = new SqlCommand(ssql, oConn))
				{
					oCommand.ExecuteNonQuery();
				}

				// Get the list of the logical files in the backup file and reset the path for each one.
				ssql = "restore filelistonly from disk = " + Globals.CSqlN(sBackupFilename);
				using (SqlCommand oCommand = new SqlCommand(ssql, oConn))
				using (SqlDataReader oReader = oCommand.ExecuteReader())
				{
					ssql = "restore database [" + sDatabaseName + "] from disk = " + Globals.CSqlN(sBackupFilename) + " with replace";
					int iLogFile = 0;
					while (oReader.Read())
					{
						string sFilename = Globals.DbFolder + sDatabaseName;
						if (oReader["Type"].ToString().ToUpper() == "D")
						{
							sFilename += ".mdf";
						}
						else
						{
							string sExt = (oReader["PhysicalName"].ToString().IndexOf("_log") > -1) ? "_log.ldf" : ".ldf";
							if (iLogFile++ == 0)
								sFilename += "_log.ldf";
							else
								sFilename += iLogFile + "_log.ldf";
						}
						ssql += ", move " + Globals.CSqlN(oReader["LogicalName"].ToString()) + " to " + Globals.CSqlN(sFilename);
					}
				}

				using (SqlCommand oCommand = new SqlCommand(ssql, oConn))
				{
					// Default is 30 which is too short when restoring a big database from SQL Server 2000
					// on slower machines. 0 is unlimited time.
					oCommand.CommandTimeout = 0;
					oCommand.ExecuteNonQuery();
				}
			}
		}

		static public void DeleteDB(string sDatabaseName)
		{
			if (!Globals.DatabaseExists(sDatabaseName))
				return;
			string sDBLogFileExt = "";
			if (File.Exists(Globals.DbFolder + sDatabaseName +  ".ldf"))
				sDBLogFileExt = ".ldf";
			if (File.Exists(Globals.DbFolder + sDatabaseName +  "_log.ldf"))
				sDBLogFileExt = "_log.ldf";

			string ssql = "drop database [" + sDatabaseName + "]";
			using (SqlConnection oConn = Globals.Conn)
			using (SqlCommand oCommand = new SqlCommand(ssql, oConn))
			{
				oCommand.ExecuteNonQuery();
			}

			if (File.Exists(Globals.DbFolder + sDatabaseName))
				File.Delete(Globals.DbFolder + sDatabaseName);
			if (sDBLogFileExt.Length > 0)
				File.Delete(Globals.DbFolder + sDatabaseName + sDBLogFileExt);
		}

		static public void StartDB()
		{
			ServiceController oSC = new ServiceController("MSSQL$SILFW");
			if (oSC.Status != ServiceControllerStatus.StartPending && oSC.Status != ServiceControllerStatus.Running)
				oSC.Start();
		}

		static public void StopDB()
		{
			ServiceController oSC = new ServiceController("MSSQL$SILFW");
			if (oSC.Status != ServiceControllerStatus.StopPending && oSC.Status != ServiceControllerStatus.Stopped)
				oSC.Stop();
		}

		static public void InitializeFW()
		{
			// Try to find the path to DbAccess.dll.
			string sFilename = "DbAccess.dll";
			Microsoft.Win32.RegistryKey oKey = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey("SIL.DbAccess.OleDbEncap\\CLSID");
			if (oKey != null)
			{
				object oCLSID = oKey.GetValue(null);
				if (oCLSID != null)
				{
					Microsoft.Win32.RegistryKey oClassKey =
						Microsoft.Win32.Registry.ClassesRoot.OpenSubKey("CLSID\\" + oCLSID.ToString() + "\\InprocServer32");
					if (oClassKey != null)
					{
						object oFilename = oClassKey.GetValue(null);
						if (oFilename != null)
							sFilename = oFilename.ToString();
						oClassKey.Close();
					}
				}
				oKey.Close();
			}
			if (!File.Exists(sFilename))
			{
				Console.WriteLine("Cannot call ExtInitMSDE in " + sFilename + " because the file does not exist.");
				return;
			}
			System.Diagnostics.Process.Start("rundll32.exe", "\"" + sFilename + "\",ExtInitMSDE force");
		}

		static public void ExecDB(string sFilename, string sDatabaseName)
		{
			if (!Globals.DatabaseExists(sDatabaseName))
				return;
			// If the filename does not include the path, default to the DbDir path.
			if (sFilename.IndexOf('\\') == -1)
				sFilename = Globals.DbFolder + sFilename;
			if (!File.Exists(sFilename) && sFilename.IndexOf('.') == -1)
				sFilename += ".sql";

			if (!File.Exists(sFilename))
			{
				Console.WriteLine("Cannot open " + sFilename + " because it does not exist.");
				return;
			}
			// Use osql instead of StreamReader so it can execute multiple batches separated by GO
			Process proc = new Process();
			proc.StartInfo.FileName = "osql.exe";
			proc.StartInfo.Arguments = "-S" + Globals.Server + " -d\"" + sDatabaseName + "\" -Usa " +
				"-Pinscrutable -i\"" + sFilename + "\" -n'";
			proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
			// The following StartInfo properties allow us to extract the standard output
			proc.StartInfo.UseShellExecute = false;
			proc.StartInfo.RedirectStandardOutput = true;
			proc.Start();
			// Do not wait for the child process to exit before
			// reading to the end of its redirected stream, or the process will hang.
			// proc.WaitForExit();
			// Read the output stream first and then wait.
			string output = proc.StandardOutput.ReadToEnd();
			proc.WaitForExit();
			if( proc.ExitCode != 0 )
			{
				Console.WriteLine("osql failed with error code: " + proc.ExitCode);
			}
			Console.WriteLine(output);
		}

		static public void TraceDB(string sTrace, string sDatabaseName, int nTraceType)
		{
			if (!Globals.DatabaseExists(sDatabaseName))
				return;
			string ssql = "exec [" + sDatabaseName + "].dbo.";
			// The trace can be put somewhere other than root by passing in the full path name to
			// StartDebugTrace or StartPerformanceTrace. StopTrace defaults to stopping the first
			// trace that has "Fw" in the name.
			if (nTraceType == 0)
				ssql = ssql + ((sTrace.ToLower() == "on") ? "StartDebugTrace" : "StopTrace 'FwDebug'");
			else if (nTraceType == 1)
				ssql = ssql + ((sTrace.ToLower() == "on") ? "StartPerformanceTrace" : "StopTrace 'FwPerformance'");
			using (SqlConnection oConn = Globals.Conn)
			using (SqlCommand oCommand = new SqlCommand(ssql, oConn))
			{
				oCommand.ExecuteNonQuery();
			}
		}

		static public void LoadDB(string sDatabaseName)
		{
			// Make sure the XML file exists.
			string sXMLFilename = Globals.DbFolder + sDatabaseName + ".xml";
			if (!File.Exists(sXMLFilename))
			{
				Console.WriteLine("Cannot load " + sDatabaseName + " because the XML file " + sXMLFilename + " does not exist.");
				return;
			}
			string sTemplateMDF = Globals.TemplateFolder + "BlankLangProj.mdf";
			if (!File.Exists(sTemplateMDF))
			{
				Console.WriteLine("Cannot load " + sDatabaseName + " because the template file " + sTemplateMDF + " does not exist.");
				return;
			}
			string sTemplateLDF = Globals.TemplateFolder + "BlankLangProj_log.ldf";
			if (!File.Exists(sTemplateLDF))
			{
				Console.WriteLine("Cannot load " + sDatabaseName + " because the template file " + sTemplateLDF + " file does not exist.");
				return;
			}

			// See if the database exists.
			string ssql = "select dbid from master.dbo.sysdatabases where name = " + Globals.CSqlN(sDatabaseName);
			using (SqlConnection oConn = Globals.Conn)
			{
				using (SqlCommand oCommand = new SqlCommand(ssql, oConn))
				{
					object o = oCommand.ExecuteScalar();
					if (o != null)
						DeleteDB(sDatabaseName);
				}
			}

			File.Copy(sTemplateMDF, Globals.DbFolder + sDatabaseName + ".mdf", true);
			File.Copy(sTemplateLDF, Globals.DbFolder + sDatabaseName + "_log.ldf", true);

			AttachDB(sDatabaseName);

			System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo("loadxml", "-i \"" + sXMLFilename + "\"");
			psi.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
			System.Diagnostics.Process oProcess = System.Diagnostics.Process.Start(psi);
			oProcess.WaitForExit();
		}

		static public void DumpDB(string sDatabaseName)
		{
			if (!Globals.DatabaseExists(sDatabaseName))
				return;
			string sXMLFilename = Globals.DbFolder + sDatabaseName + ".xml";
			if (File.Exists(sXMLFilename))
				File.Delete(sXMLFilename);

			System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo("dumpxml", "-d \"" + sDatabaseName + "\" -o \"" + sXMLFilename + "\"");
			psi.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
			System.Diagnostics.Process oProcess = System.Diagnostics.Process.Start(psi);
			oProcess.WaitForExit();
		}

		static public void ValidateXml(string sXmlName)
		{
			// Make sure the XML file exists.
			string sXMLFilename = Globals.DbFolder + sXmlName + ".xml";
			if (!File.Exists(sXMLFilename))
			{
				Console.WriteLine("Cannot validate " + sXmlName + " because the XML file " + sXMLFilename + " does not exist.");
				return;
			}
			string sDtdFilename = Globals.DbFolder + "FwDatabase.dtd";
			if (!File.Exists(sDtdFilename))
			{
				Console.WriteLine("Cannot validate " + sXmlName + " because the DTD file " + sDtdFilename + " does not exist.");
				return;
			}
//			// This approach would work if rxp understood Windows path names, but it gives an error unless
//			// it is specified as /c:/fw/bin/test.xml
//			string sProgram = System.Reflection.Assembly.GetExecutingAssembly().Location;
//			sProgram = sProgram.Substring(0, sProgram.LastIndexOf("\\") + 1) + "rxp.exe";
//			string sTempFilename = Globals.DbFolder + "temp.err";
//			System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo(sProgram, "-Vs -f \"" + sTempFilename + "\" \"" + sXMLFilename + "\"");
//			psi.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
//			psi.WorkingDirectory = Globals.DbFolder;
//			psi.UseShellExecute = true;
//			System.Diagnostics.Process oProcess = System.Diagnostics.Process.Start(psi);
//			oProcess.WaitForExit();

			// So this approach keeps path names out of the file names fed to rxp.
			string sProgram = System.Reflection.Assembly.GetExecutingAssembly().Location;
			sProgram = sProgram.Substring(0, sProgram.LastIndexOf("\\") + 1) + "rxp.exe";
			string sTempFilename = Globals.DbFolder + "temp.err";
			System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo(sProgram, "-Vs -f \"" + "temp.err" + "\" \"" + sXmlName + ".xml\"");
			psi.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
			psi.WorkingDirectory = Globals.DbFolder;
			psi.UseShellExecute = true;
			System.Diagnostics.Process oProcess = System.Diagnostics.Process.Start(psi);
			oProcess.WaitForExit();
		}

		static public void DumpSources(string sDatabaseName, string sSourceFilename)
		{
			if (!Globals.DatabaseExists(sDatabaseName))
				return;
			// If the optional filename is not included, default to the database name.
			if (sSourceFilename == null)
				sSourceFilename = sDatabaseName + " Sources.sql";
			// If the filename does not include the path, default to the DbDir path.
			if (sSourceFilename.IndexOf('\\') == -1)
				sSourceFilename = Globals.DbFolder + sSourceFilename;

			// Open connection to database
			string sConnectionString = "Server=" + Globals.Server + "; Database=" +
				Globals.CSql(sDatabaseName) + "; User ID = sa;" +
				"Password=inscrutable; Connect Timeout = 2; Pooling=false;";
			using (SqlConnection connection = new SqlConnection(sConnectionString))
			{
				try
				{
					connection.Open();
				}
				catch (Exception)
				{
					throw new Exception("Unable to open database " + sDatabaseName);
				}
				StreamWriter file = File.CreateText(sSourceFilename);
				DumpSource("P", "Procedures", connection, file);
				DumpSource("TF", "Functions", connection, file);
				DumpSource("TR", "Triggers", connection, file);
				DumpSource("V", "Views", connection, file);
				DumpSource("C", "Constraints", connection, file);
				connection.Close();
				file.Flush();
				file.Close();
			}
		}

		static public void DumpStructure(string sDatabaseName, string sStructureFilename)
		{
			if (!Globals.DatabaseExists(sDatabaseName))
				return;
			// If the optional filename is not included, default to the database name.
			if (sStructureFilename == null)
				sStructureFilename = sDatabaseName + " Structure.txt";
			// If the filename does not include the path, default to the DbDir path.
			if (sStructureFilename.IndexOf('\\') == -1)
				sStructureFilename = Globals.DbFolder + sStructureFilename;

			// Open connection to database
			string sConnectionString = "Server=" + Globals.Server + "; Database=" +
				Globals.CSql(sDatabaseName) + "; User ID = sa;" +
				"Password=inscrutable; Connect Timeout = 2; Pooling=false;";
			using (SqlConnection connection = new SqlConnection(sConnectionString))
			{
				try
				{
					connection.Open();
				}
				catch (Exception)
				{
					throw new Exception("Unable to open database " + sDatabaseName);
				}
				StreamWriter file = File.CreateText(sStructureFilename);
				WriteQueryResults("Class$", "select * from Class$ order by id", connection, file);
				WriteQueryResults("Field$", "select * from Field$ order by id", connection, file);
				WriteQueryResults("ClassPar$", "select * from ClassPar$ order by src, depth", connection, file);
				SqlCommand command;
				StringCollection names = new StringCollection();
				command = new SqlCommand("select name from sysobjects where type = 'u' order by name", connection);
				SqlDataReader reader = command.ExecuteReader();
				try
				{
					while (reader.Read())
					{
						string s = reader.GetString(0);
						names.Add(s);
					}
				}
				finally
				{
					// Always call Close when done reading.
					reader.Close();
				}
				for (int i = 0; i < names.Count; ++i)
					WriteTable(names[i], connection, file);
				connection.Close();
				file.Flush();
				file.Close();
			}
		}

		static public void WriteQueryResults(string sTable, string sQuery, SqlConnection connection, StreamWriter file)
		{
			SqlCommand command;
			command = new SqlCommand(sQuery, connection);
			SqlDataReader reader = command.ExecuteReader();
			try
			{
				int field;
				int columns = reader.FieldCount;
				file.WriteLine("**>> " + sTable + " <<**");
				// Write out the column headers.
				for (field = 0; field < columns; field++)
				{
					string s = reader.GetName(field);
					file.Write(s);
					if (field != columns - 1)
						file.Write('\t');
				}
				file.WriteLine();
				while (reader.Read())
				{
					//System.Type type;
					string type;
					string s = "";
					for (field = 0; field < columns; field++)
					{
						if (!reader.IsDBNull(field))
						{
							type = reader.GetFieldType(field).ToString();
							switch (type)
							{
								default:
									s = "unknown";
									break;
								case "System.Byte":
									s = reader.GetByte(field).ToString();
									break;
								case "System.Int32":
									s = reader.GetInt32(field).ToString();
									break;
								case "System.Int16":
									s = reader.GetInt16(field).ToString();
									break;
								case "System.Guid":
									s = reader.GetGuid(field).ToString();
									break;
								case "System.String":
									s = reader.GetString(field);
									break;
							}
						}
						else
							s = "NULL";
						file.Write(s);
						if (field != columns - 1)
							file.Write('\t');
					}
					file.WriteLine();
				}
			}
			finally
			{
				// Always call Close when done reading.
				reader.Close();
			}
		}

		static public void DumpSource(string sType, string sName, SqlConnection connection, StreamWriter file)
		{
			string ssql;
			SqlCommand command;
			StringCollection names = new StringCollection();
			ssql = "select name from sysobjects where type = '" + sType + "' order by name";
			command = new SqlCommand(ssql, connection);
			SqlDataReader reader = command.ExecuteReader();
			try
			{
				while (reader.Read())
				{
					string s = reader.GetString(0);
					names.Add(s);
				}
			}
			finally
			{
				// Always call Close when done reading.
				reader.Close();
			}
			file.WriteLine("**>> " + names.Count.ToString() + " " + sName + " <<**");
			for (int i = 0; i < names.Count; ++i)
			{
				string s = names[i];
				ssql = "select text from syscomments where id=object_id('" + s + "')";
				command = new SqlCommand(ssql, connection);
				reader = command.ExecuteReader();
				try
				{
					string source = "";
					while (reader.Read())
					{
						source += reader.GetString(0);
					}
					file.WriteLine(sType + "-->>" + s);
					file.WriteLine(source);
				}
				finally
				{
					// Always call Close when done reading.
					reader.Close();
				}
			}
	   }

		static public void WriteTable(string sName, SqlConnection connection, StreamWriter file)
		{
			string ssql;
			SqlCommand command;
			file.WriteLine(">>Table: " + sName);
			ssql = "exec sp_MShelpcolumns '" + sName + "', @orderby = 'id'";
			command = new SqlCommand(ssql, connection);
			SqlDataReader reader = command.ExecuteReader();
			try
			{
				file.WriteLine("id" + '\t' + "name" + '\t' + "type" + '\t' + "len" + '\t' + "default" + '\t' + "collation");
				while (reader.Read())
				{
					// col_id  (1)
					file.Write(reader.GetInt32(1).ToString() + '\t');
					// col_name (0)
					file.Write(reader.GetString(0) + '\t');
					// col_typename  (2)
					file.Write(reader.GetString(2) + '\t');
					// col_len  (3)
					file.Write(reader.GetInt32(3).ToString() + '\t');
					// text  (15)
					if (!reader.IsDBNull(15))
						file.Write(reader.GetString(15) + '\t');
					else
						file.Write("NULL" + '\t');
					// collation  (25)
					if (!reader.IsDBNull(25))
						file.WriteLine(reader.GetString(25));
					else
						file.WriteLine("NULL");
				}
			}
			finally
			{
				// Always call Close when done reading.
				reader.Close();
			}
			ssql = "exec sp_MStablekeys '" + sName + "'";
			command = new SqlCommand(ssql, connection);
			reader = command.ExecuteReader();
			try
			{
				file.WriteLine("name" + '\t' + "type" + '\t' + "flags" + '\t' + "key");
				while (reader.Read())
				{
					// name  (1)
					file.Write(reader.GetString(1) + '\t');
					// type (0)
					file.Write(reader.GetByte(0).ToString() + '\t');
					// flags  (2)
					file.Write(reader.GetInt32(2).ToString() + '\t');
					// key  (7)
					file.WriteLine(reader.GetString(7));
				}
			}
			finally
			{
				// Always call Close when done reading.
				reader.Close();
			}
			ssql = "exec sp_MStablechecks '" + sName + "'";
			command = new SqlCommand(ssql, connection);
			reader = command.ExecuteReader();
			try
			{
				if (reader.HasRows)
					file.WriteLine("name" + '\t' + "check");
				while (reader.Read())
				{
					// name  (0)
					file.Write(reader.GetString(0) + '\t');
					// check (1)
					file.WriteLine(reader.GetString(1));
				}
			}
			finally
			{
				// Always call Close when done reading.
				reader.Close();
			}
		}
	}
}