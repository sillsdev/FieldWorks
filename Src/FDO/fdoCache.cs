// --------------------------------------------------------------------------------------------
// Copyright (C) 2002 SIL International. All rights reserved.
//
// Distributable under the terms of either the Common Public License or the
// GNU Lesser General Public License, as specified in the LICENSING.txt file.
//
// File: fdoCache.cs
// Responsibility: John Hatton and Randy Regnier
// Last reviewed: never
//
//
// <remarks>
// Implementation of:
//		FdoCache (formerly AfDbInfo and CustViewDa)
// </remarks>
// --------------------------------------------------------------------------------------------

//Enable this flag when you want to list the queries that the cache is making on the console.
//Why not just use the profiler? Because sometimes, you want to know the queries that are associated with
//various other things your program is doing.  Those other things can also write to the console,
//so that everything will be shown in context. If it is possible to send little notes to the sqldb
//that would show up in the profiler, then we could get rid of this.
//#define _verbose
/*
AlistairI suggests we instead implement a simple macro to send little notes to sqldb that would
appear on the profiler. Here is a possible implementation outline:
Define a macro, which expands to nothing in the release build;
The macro takes one argument: the text we want to appear on the profiler;
This text is then incorporated into some redundant query, perhaps something like:
	"select * from Text where SoundFilePath = 'This is the text I want in the profiler.'"
The idea is that the query would do little, reading from a table with not much data, so as not
to waste much time, yet the query would appear in the profiler output, and therefore so would
the text.
We could even add a dummy table to the database, with one text value, to ensure the query does
as little as possible.
--
SteveMiller: Might try xp_trace_generate_event. From http://doc.ddart.net/mssql/sql70/xp_trace_9.htm:
"This example creates a user-defined event with an event class of 82. This event class
indicates the running of a script, and the completion of one stage of the process."
Event class 82 is a user configurable event in Profiler. I think @event_text_data is what
you're looking for. We could turn the following example from the site into a stored procedure.

DECLARE @myvariable int
SET @myvariable = 4 -- Initialize to completion level.
EXEC xp_trace_generate_event @event_class = 82,
	@event_text_data = 'Stage 1 of Stage 3',
	@application_name = 'Myscript',
	-- Put the return value in a variable to check completion status.
	@integer_data = @myvariable,
	@nt_user_name = 'janetj'
*/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.ComponentModel;	// Needed for InvalidEnumArgumentException use.
using System.Data.SqlClient;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices; // needed for Marshal
using System.Windows.Forms;		// for MessageBox
using System.Xml;	// to pass an XmlNode paramater.
using Microsoft.Win32;
using System.Text;

using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Utils;

namespace SIL.FieldWorks.FDO
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for FdoCache.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class FdoCache : IFWDisposable
	{
		#region Data members for FdoCache
		/// <summary>
		/// When the FdoCache is created by COM clients, they provide these:
		/// m_ode = ode;
		/// m_mdc = mdc;
		/// m_odde = sda;
		/// m_lef = sda.WritingSystemFactory;
		/// So, our disposer shouldn't nuke them.
		/// </summary>
		private bool m_clientProvidedCache = false;
		/// <summary> </summary>
		protected ISilDataAccess m_odde;
		/// <summary> </summary>
		protected IOleDbEncap m_ode;
		/// <summary> </summary>
		protected IFwMetaDataCache m_mdc;
		/// <summary> </summary>
		protected ILgWritingSystemFactory m_lef;
		// Cached data.
		/// <summary> </summary>
		protected ILangProject m_lp;
		/// <summary> </summary>
		protected IScrRefSystem m_srs;
		/// <summary> </summary>
		protected UserViewCollection m_cuv;
		/// <summary> </summary>
		protected LgWritingSystemCollection m_cle;
		/// <summary> </summary>
		protected CmPossibilityListCollection m_cpl;
		/// <summary>
		/// Collection of active Change Watchers registered with DB accessor
		/// </summary>
		protected List<ChangeWatcher> m_rgChangeWatchers;

		/// <summary>Hashtables defined for the database cache.</summary>
		protected Dictionary<Guid, IDictionary> m_hashtables = new Dictionary<Guid, IDictionary>();

		/// <summary>Provides the footnotes index in the list of auto-lettered footnotes</summary>
		private IFootnoteMarkerIndexCache m_FootnoteIndexes;

		/// <summary>when set FdoCache will not request anything to be loaded into the cache.
		///  See note on corresponding property for more information.</summary>
		protected bool m_assumeCacheFullyLoaded = false;

		/// <summary>True to pre-load data when creating an FDO object, false otherwise</summary>
		protected bool m_fPreloadData = true;

		/// <summary>Flag indicating the cache is busy in some process (e.g. undoing
		/// or redoing.</summary>
		protected bool m_isBusy = false;

		/// <summary>Determines how to process PropChanged notifications</summary>
		protected PropChangedHandling m_PropChangedHandling = PropChangedHandling.SuppressNone;

		/// <summary>
		/// The next available dummy flid.
		/// </summary>
		protected static int m_sDummyFlid = -1000;

		/// <summary>
		/// If this is set, it is the watcher that causes modifications to this cache to
		/// result in new Sync records in the database.
		/// </summary>
		protected SyncWatcher m_watcher;

		/// <summary>
		/// The action handler from m_odde. We store it in a variable because we have
		/// to call ReleaseComObject on it. Use the ActionHandlerAccessor property
		/// instead of this variable.
		/// </summary>
		protected IActionHandler m_actionHandler;
		/// <summary>
		/// Use this flag to prevent suppressing action handler.
		/// Some unit tests require adding all undo actions, to restore database to initial state for each test.
		/// </summary>
		private bool m_fAddAllActionsForTestCache = false;

		/// <summary>Used to display progress when loading data</summary>
		private IAdvInd3 m_progressBar;

		/// <summary>
		/// If we are recording create and modify times store the manager here so that
		/// Saves invoke it.
		/// </summary>
		private CreateModifyTimeManager m_createModifyManager;

		/// <summary>Maps the types to the assembly that implements this type for a specific
		/// cellar module</summary>
		private Dictionary<string, string> m_TypeAssemblyMap = new Dictionary<string,string>();

		private bool m_fUndoEnabled = true;		// allow Undo/Redo processing by default.

		internal bool m_fTestMode = false;
		#endregion	// Data members for FdoCache

		#region Construction
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor for testing
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FdoCache()
		{
			m_clientProvidedCache = true;
			InitializeModules();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Public Constructor for FdoCache, to be used when a connection and cache have already
		/// been established and a new FdoCache is needed to encapsulate them.
		/// </summary>
		/// <remarks>
		/// The primary purpose of this constructor is to enable common dialogs implemented in
		/// .Net to be called from unmanaged clients that have already established a connection
		/// to an FW database.
		/// </remarks>
		/// <param name="ode"></param>
		/// <param name="mdc"></param>
		/// <param name="oleDbAccess"></param>
		/// ------------------------------------------------------------------------------------
		public FdoCache(IOleDbEncap ode, IFwMetaDataCache mdc, IVwOleDbDa oleDbAccess)
			: this(ode, mdc, oleDbAccess, oleDbAccess as ISilDataAccess)
		{
			m_clientProvidedCache = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Public Constructor for FdoCache that allows setting cache... objects.
		/// </summary>
		/// <remarks>
		/// This constructor is used in tests
		/// </remarks>
		/// <param name="ode"></param>
		/// <param name="mdc"></param>
		/// <param name="oleDbAccess"></param>
		/// <param name="sda"></param>
		/// ------------------------------------------------------------------------------------
		public FdoCache(IOleDbEncap ode, IFwMetaDataCache mdc, IVwOleDbDa oleDbAccess,
			ISilDataAccess sda)
		{
			if (ode == null)
				throw new ArgumentNullException("ode", "Null object IOleDbEncap passed to FdoCache constructor");
			if (mdc == null)
				throw new ArgumentNullException("mdc", "Null object IFwMetaDataCache passed to FdoCache constructor");
			if (oleDbAccess == null)
				throw new ArgumentNullException("oleDbAccess", "Null object IVwOleDbDa passed to FdoCache constructor");
			if (sda == null)
				throw new ArgumentNullException("sda", "Null object ISilDataAccess passed to FdoCache constructor");

			InitializeModules();
			m_ode = ode;
			m_mdc = mdc;
			m_odde = sda;
			m_lef = sda.WritingSystemFactory;
			ClearSyncTable();
			m_clientProvidedCache = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Private Constructor for FdoCache.
		/// FdoCache objects can only be created via one of the three static methods.
		/// </summary>
		/// <param name="sMSDEPath">The named instance of a SQL server.</param>
		/// <param name="sDatabaseName">Name of the database to use.</param>
		/// ------------------------------------------------------------------------------------
		private FdoCache(string sMSDEPath, string sDatabaseName)
		{
			InitializeModules();
			FullyInitializeWithDatabase(sMSDEPath, sDatabaseName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize FdoCache with database
		/// </summary>
		/// <param name="sMSDEPath">The named instance of a SQL server.</param>
		/// <param name="sDatabaseName">Name of the database to use.</param>
		/// ------------------------------------------------------------------------------------
		private void FullyInitializeWithDatabase(string sMSDEPath, string sDatabaseName)
		{
			m_ode = OleDbEncapClass.Create();
			m_ode.Init(sMSDEPath, sDatabaseName, Logger.Stream,
				// You can set SQL Server's SET LOCK_TIMEOUT to something other
				// than the dofault of not timing out, but then you'd better
				// be prepared to have some code in your app that can handle
				// error 1222 when you do get a block.
				OdeLockTimeoutMode.koltMsgBox, (int)OdeLockTimeoutValue.koltvForever);
			m_mdc = FwMetaDataCacheClass.Create();
			m_mdc.Init(m_ode);
			m_odde = VwOleDbDaClass.Create();
			IActionHandler ah = ActionHandlerClass.Create();
			ah.UndoGrouper = (IUndoGrouper)m_ode;
			ILgWritingSystemFactoryBuilder lefBuilder = LgWritingSystemFactoryBuilderClass.Create();
			m_lef = lefBuilder.GetWritingSystemFactory(m_ode, Logger.Stream);
			// The following line doesn't work if we try to call ((ISetupVwOleDbDa)m_odde).Init,
			// so we have to do this strange cast.
			//			((ISetupVwOleDbDa)m_odde).Init(ode, m_mdc, m_lef, ah);
			((VwOleDbDa)m_odde).Init(m_ode, m_mdc, m_lef, ah);
			Marshal.ReleaseComObject(ah);
			ah = null;
			Marshal.ReleaseComObject(lefBuilder);
			lefBuilder = null;

			ClearSyncTable();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clear out the syncronization table if we're the only connection to the DB.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ClearSyncTable()
		{
			if (String.IsNullOrEmpty(DatabaseName))
				return;		// must be running a test with a dummy cache...
			IOleDbCommand odc = null;
			try
			{
				m_ode.CreateCommand(out odc);
				// It's safest to pass the database name as a parameter, because it may contain
				// apostrophes.  See LT-8910.
				odc.SetStringParameter(1, (uint)DBPARAMFLAGSENUM.DBPARAMFLAGS_ISINPUT,
					null, DatabaseName, (uint)DatabaseName.Length);
				odc.ExecCommand("exec ClearSyncTable$ ?", (int)SqlStmtType.knSqlStmtStoredProcedure);
			}
			finally
			{
				DbOps.ShutdownODC(ref odc);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get an FdoCache for the first FW database on the local server.
		/// </summary>
		/// <returns>An FdoCache, or null, if not able to create one.</returns>
		/// ------------------------------------------------------------------------------------
		public static FdoCache Create()
		{
			Dictionary<string, string> cacheOptions = new Dictionary<string, string>();
			cacheOptions.Add("c", MiscUtils.LocalServerName);
			return Create(cacheOptions);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get an FdoCache for the given database on the local server.
		/// </summary>
		/// <param name="sDatabase">
		/// The database to open.
		/// </param>
		/// <returns>An FdoCache, or null, if not able to create one.</returns>
		/// ------------------------------------------------------------------------------------
		public static FdoCache Create(string sDatabase)
		{
			Debug.Assert(sDatabase != null && sDatabase.Length > 0);

			Dictionary<string, string> cacheOptions = new Dictionary<string, string>();
			cacheOptions.Add("db", sDatabase);
			return Create(cacheOptions);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get an FdoCache for the first FW database that matches the parameters,
		/// if there are any.
		/// </summary>
		/// <param name="sServer">SQL server</param>
		/// <param name="sDatabase">Database name</param>
		/// <param name="sRootObjectname">Name of a CmMajorObject</param>
		/// <returns>An FdoCache, or null, if not able to create one.</returns>
		/// ------------------------------------------------------------------------------------
		public static FdoCache Create(string sServer, string sDatabase,
			string sRootObjectname)
		{
			Dictionary<string, string> cacheOptions = new Dictionary<string, string>();
			if (sServer != null && sServer.Length > 0)
				cacheOptions.Add("c", sServer);
			if (sDatabase != null && sDatabase.Length > 0)
				cacheOptions.Add("db", sDatabase);
			if (sRootObjectname != null && sRootObjectname.Length > 0)
				cacheOptions.Add("filename", sRootObjectname);
			return Create(cacheOptions);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure MSDE is initialized and running.
		/// </summary>
		/// <param name="sServer">Server to initialize. If the server fails to initialize,
		/// then the local server will be attempted.</param>
		/// <returns>the name of the server actually initialized.</returns>
		/// ------------------------------------------------------------------------------------
		public static string InitMSDE(string sServer)
		{
			// if no server was given then start on the local machine
			if (sServer == null || sServer == string.Empty)
				sServer = MiscUtils.LocalServerName;

			IOleDbEncap ode = null;
			try
			{
				ode = OleDbEncapClass.Create();
				ode.InitMSDE(Logger.Stream, false); // It calls StartMSDE().

				// Try to initialize the server. If it fails, ignore the error.
				try
				{
					ode.Init(sServer, "master", null,
						// You can set SQL Server's SET LOCK_TIMEOUT to something other
						// than the dofault of not timing out, but then you'd better
						// be prepared to have some code in your app that can handle
						// error 1222 when you do get a block.
						OdeLockTimeoutMode.koltMsgBox, (int)OdeLockTimeoutValue.koltvForever);
				}
				catch (Exception err)
				{
					Debug.WriteLine(err.Message);
				}
			}
			catch (Exception err)
			{
				// If MSDE does not get initialized properly the first time, or if SQL Server doesn't
				// get started any time, there isn't much sense in going on. We're dead!
				throw new ApplicationException(Strings.CannotStartDatabaseServer, err);
			}
			finally
			{
				if (ode != null && Marshal.IsComObject(ode))
					Marshal.ReleaseComObject(ode);
			}

			// This is not the right solution. This code all needs to go under the C++ InitMSDE method. Also,
			// it needs research to determine if MSDE memory should be limited, and if so, how much. For the
			// moment, we'll get the value from a registry entry and if not found, use 1/4 of physical memory
			// or 128Mb, whichever is greater.
			ulong mem = 0;
			try
			{
				RegistryKey regKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\SIL\FieldWorks");
				if (regKey != null)
				{
					int nmem = (int)regKey.GetValue("MSDEMem", 0);
					mem = (ulong)nmem;
					regKey.Close();
				}
				if (mem == 0)
				{
					// SQL Server 2005 will not work reliably on 64Mb of memory, so set the minimum to 128.
					mem = Math.Max(MiscUtils.GetPhysicalMemoryBytes() / 1024 / 1024 / 4, 128);
				}
			}
			catch
			{
				Logger.WriteEvent("Unable to get memory size.");
				return sServer;
			}
			string sSql = string.Format("Server={0}; Database=master; User ID=FWDeveloper;" +
				" Password=careful; Pooling=false;", MiscUtils.LocalServerName);
			using (SqlConnection connection = new SqlConnection(sSql))
			{
				try
				{
					connection.Open();
				}
				catch (SqlException e)
				{
					// If MSDE does not get initialized properly the first time, or if SQL Server doesn't
					// get started any time, there isn't much sense in going on. We're dead!
					throw new ApplicationException(Strings.CannotStartDatabaseServer, e);
				}
				using (SqlCommand commandProjectList = connection.CreateCommand())
				{
					commandProjectList.CommandText = string.Format("sp_configure 'max server memory', {0}; reconfigure",
						mem);

					const int kMaxAttempts = 3;
					for (int numberOfAttempts = 0; numberOfAttempts < kMaxAttempts; numberOfAttempts++)
					{
						try
						{
							commandProjectList.ExecuteNonQuery();
							break;
						}
						catch (Exception ex)
						{
							// Ignore any errors on the first two attempts and retry.
							// After the third attempt, just report it as an error.
							if (numberOfAttempts == kMaxAttempts - 1)
							{
								StringBuilder bldr = new StringBuilder(Strings.ksErrorMaxingSQLSvr);
								if (ex is SqlException)
								{
									SqlException e = (SqlException)ex;
									foreach (SqlError err in e.Errors)
									{
										// error classes above 18 are really bad!
										if (err.Class > 18)
										{
											try
											{
												connection.Close();
											}
											catch
											{
												// Ignore this error. Throw original error instead.
											}
											throw e;
										}
										bldr.AppendLine();
										bldr.Append(err.Message);
									}
								}
								MessageBox.Show(bldr.ToString(), "SQL Server error");
							}
						}
					}
				}
				connection.Close();
			}
			return sServer;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get an FdoCache for the first FW database that matches the parameters,
		/// if there are any.
		/// </summary>
		/// <param name="args">A Dictioanry with string keys of:
		/// "c" SQL Server,
		/// "db" Database name,
		/// "filename" a CmMajorObject name.
		/// These are all optional elements in the Dictionary.
		/// The values in the hashtable are expected to be strings.
		/// Any other keys are ignored.
		/// </param>
		/// <returns>An FdoCache, or null, if not able to create one.</returns>
		/// ------------------------------------------------------------------------------------
		public static FdoCache Create(Dictionary<string, string> args)
		{
			Debug.Assert(args != null);

			bool fDBFound = false;
			// Dig out information from htArgs.
			string sServer = MiscUtils.LocalServerName;
			if (args.ContainsKey("c"))
				sServer = args["c"];
			string sDbName = null;
			if (args.ContainsKey("db"))
				sDbName = args["db"];
			string sRtObjName = null;
			if (args.ContainsKey("filename"))
				sRtObjName = args["filename"];

			// Make sure ICU files are properly updated after installation.
			// Note. We don't want to open the key for writing here or no one will
			// be able to use the program with Windows logins under Power User.
			RegistryKey key = Registry.LocalMachine.OpenSubKey("Software\\SIL");
			if (key != null)
			{
				int value = (int)key.GetValue("InitIcu", 0);
				if (value != 0)
				{
					Process prc = new Process();
					try
					{
						// Call installLanguage -o to initialize ICU file to match
						// language definition files in the languages directory.
						System.Reflection.Assembly asm = System.Reflection.Assembly.GetExecutingAssembly();
						// make sure no open ICU files -dlh
						IIcuCleanupManager icm = IcuCleanupManagerClass.Create();
						icm.Cleanup();
						Marshal.ReleaseComObject(icm);
						icm = null;
						string baseDir = asm.Location;
						baseDir = baseDir.Substring(0, baseDir.LastIndexOf("\\"));
						prc.StartInfo.FileName = System.IO.Path.Combine(baseDir, "InstallLanguage.exe ");
						// Arguments need directories surrounded in double quotes to cover spaces in path.
						prc.StartInfo.Arguments = "-o";
						prc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
						prc.StartInfo.WorkingDirectory = baseDir;
						prc.Start();
						prc.WaitForExit();

						if (true)	// was having problems with icu if bad xml lang files existed
						{
							key.Close();
							// Now get a registry key we can modify. This won't work for User/Guest
							// accounts, but this only happens the first time after installation.
							key = Registry.LocalMachine.OpenSubKey("Software\\SIL", true);
							if (key != null)
								key.SetValue("InitIcu", 0);
						}
					}
					catch (Exception e)
					{
						Debug.WriteLine("An error occurred restoring ICU files: '{0}'", e.ToString());
					}
				}
				key.Close();
			}

			sServer = FdoCache.InitMSDE(sServer);

			StringCollection alDbNames = new StringCollection();
			// First, try to get a connection to the server, in order to get a list of
			// FieldWorks DB names.
			string sSql = string.Format("Server={0}; Database=master; User ID = sa;"
				+ "Password=inscrutable; Pooling=false;", sServer);
			using (SqlConnection sqlConMaster = new SqlConnection(sSql))
			{
				try
				{
					sqlConMaster.Open();
				}
				catch
				{
					// "Invalid PInvoke metadata format."
					return null;
				}
				using (SqlCommand sqlComm = sqlConMaster.CreateCommand())
				{
					sqlComm.CommandText = "exec sp_GetFWDBs";
					using (SqlDataReader sqlreader =
						sqlComm.ExecuteReader(System.Data.CommandBehavior.SingleResult))
					{
						// Spin through DBs, and store all names.
						while (sqlreader.Read())
						{
							string sName = sqlreader.GetString(0);
							alDbNames.Add(sName);
							if (sDbName != null && sDbName.ToLowerInvariant() == sName.ToLowerInvariant())
							{
								sDbName = sName; // in case the case is different
								// Ok,I lied a bit. We now only want to store the matching DB name.
								alDbNames.Clear();
								alDbNames.Add(sName);
								fDBFound = true;
								break;
							}
						}
						sqlreader.Close();
					}
				}
				sqlConMaster.Close();
			}

			// alDbNames.Count == 0 means no FieldWorks databases in server.
			// !fDBFound means we have the name, but couldn't find it
			if ((alDbNames.Count == 0) ||
				(sDbName != null && sDbName.Length > 0 && !fDBFound))
				return null;

			if (sRtObjName != null)
			{
				// Client is particular, so wade through all DB names.
				for (int i = 0; i < alDbNames.Count; i++)
				{
					bool fFoundRootObject = false;
					string sCurrentDbName = (string)alDbNames[i];
					sSql = "Server=" + sServer + "; Database=" + sCurrentDbName
						+ "; User ID=FWDeveloper; Password=careful; Connect Timeout = 30; Pooling=false;";
					SqlConnection sqlConFWDb = new SqlConnection(sSql);
					sqlConFWDb.Open();

					try
					{
						if (sRtObjName != null)
						{
							SqlCommand sqlCmdRtName = sqlConFWDb.CreateCommand();
							// TODO Randy: sRtObjName must be known to be normalized to NFD.
							sqlCmdRtName.CommandText =
								string.Format("select TOP 1 Txt from CmMajorObject_Name"
								+ " where Txt='{0}'"
								+ " order by Obj, Ws", sRtObjName);
							SqlDataReader sqlreaderRt =
								sqlCmdRtName.ExecuteReader(System.Data.CommandBehavior.SingleResult);
							if (sqlreaderRt.Read())
								fFoundRootObject = true;
							sqlreaderRt.Close();
							if (fFoundRootObject)
							{
								// Client is not interested in project, just the root object.
								sDbName = sCurrentDbName;
								break;
							}
						}
					}
					finally
					{
						sqlConFWDb.Close();
						sqlConFWDb.Dispose();
						sqlConFWDb = null;
					}

					if (sRtObjName != null && fFoundRootObject)
					{
						// Client is interested in both, and they were found.
						sDbName = sCurrentDbName;
						break;
					}
				}
			}
			else
			{
				// Client doesn't care about -proj or -filename,
				// so use first name in array.
				// [Note: If the client cares about the DB name,
				// then there will only be one item in the array at this point,
				// so the first is still the right one.]
				String sFirstName = (string)alDbNames[0];
				Debug.Assert((sDbName == null) || (sDbName == sFirstName));
				sDbName = sFirstName;
			}
			return new FdoCache(sServer, sDbName);
		}
		#endregion 	// Construction

		#region IDisposable & Co. implementation
		// Region last reviewed: never

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		/// True, if the object has been disposed.
		/// </summary>
		private bool m_isDisposed = false;

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed
		{
			get { return m_isDisposed; }
		}

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~FdoCache()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <summary>
		///
		/// </summary>
		/// <remarks>Must not be virtual.</remarks>
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected virtual void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				if (m_createModifyManager != null)
				{
					m_createModifyManager.Dispose(); // will remove self from this.
				}
				// remove undo action com objects we were tracking.
				ReleaseAllTrackedComObjects(true);
				// Dispose managed resources here.
				// Clean up any change watchers
				if (m_rgChangeWatchers != null)
				{
					// Make a copy, since the ChangeWatcher Dispose method call will effectively
					// remove itself from the original collection.
					List<ChangeWatcher> changeWatchers = new List<ChangeWatcher>(m_rgChangeWatchers);
					foreach (ChangeWatcher changeWatcher in changeWatchers)
						changeWatcher.Dispose(); // MainCacheAccessor.RemoveNotification(changeWatcher);
					changeWatchers.Clear();
					changeWatchers = null;
				}
				if (m_watcher != null)
					m_watcher.Dispose();
				if (m_rgChangeWatchers != null)
					m_rgChangeWatchers.Clear();
				if (m_cuv != null)
					m_cuv.Clear();
				if (m_cle != null)
					m_cle.Clear();
				if (m_cpl != null)
					m_cpl.Clear();

				if (m_fTestMode)
					CmObject.s_classIdToType.Clear();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_watcher = null;
			m_rgChangeWatchers = null;
			m_lp = null;
			m_srs = null;
			m_cuv = null;
			m_cle = null;
			m_cpl = null;

			if (m_actionHandler != null && Marshal.IsComObject(m_actionHandler))
			{
				Marshal.ReleaseComObject(m_actionHandler);
			}
			m_actionHandler = null;

			if (m_clientProvidedCache)
			{
				// The Lord gaveth, and the Lord has to take them away.
				m_lef = null;
				m_mdc = null;
				m_odde = null;
				m_ode = null;
			}
			else
			{
				try
				{
					// We made 'em, so we zap 'em.
					// These all go away when m_odde goes away.
					if (m_lef != null)
					{
						m_lef.Shutdown();
						if (Marshal.IsComObject(m_lef))
						{
							// NOTE: don't call Marshal.FinalReleaseComObject here! It is quite
							// possible that we have another FdoCache around that still has a
							// pointer to this WritingSystemFactory! If we call
							// FinalReleaseComObject here we'll get a "COM object has been
							// separated from its underlaying RCW" exception (TE-5883). (e.g.
							// opening "Add Encoding Converter" dialog on writing system
							// properties dialog causes a second FdoCache to get created and
							// disposed)
							Marshal.ReleaseComObject(m_lef);
						}
						m_lef = null;
					}

					if (m_mdc != null)
					{
						if (Marshal.IsComObject(m_mdc))
							Marshal.ReleaseComObject(m_mdc);
						m_mdc = null;
					}
					if (m_odde != null)
					{
						if (m_odde is IVwOleDbDa)
						{
							if (m_odde is IVwCacheDa)
								(m_odde as IVwCacheDa).ClearAllData();
							// Ideally, all we would have to do here is Close m_odde. However,
							// it seems that in some circumstances, despite the garbage collect
							// above, C# retains a proxy for the action handler, though we can
							// find no reason why it would not be garbage. The reference count
							// from the C# proxy prevents the action handler from being actually
							// deleted when the cache gives up its reference count. The action
							// handler has a reference to the OleDbEncap, so that also doesn't
							// get deleted, which means we keep a database connection...and this can
							// prevent operations like backup/restore on the database (see LT-2690).
							// The workaround is to get a current proxy to the action handler and force
							// a Release().
							// REVIEW: This probably does not do anything because the ref count
							// was large. The get/release just increments the count and
							// decrements it - it does not release all references. There is now
							// a property to access the action handler which guarantees that the
							// reference count will not grow. See also TE-4281.
							IActionHandler ah = m_odde.GetActionHandler();
							IFwMetaDataCache mdc = m_odde.MetaDataCache;
							(m_odde as IVwOleDbDa).Close();
							if (ah != null && Marshal.IsComObject(ah))
								Marshal.ReleaseComObject(ah);
							// call FinalReleaseComObject seems to be the only
							// way to get rid of all the references. JohnT says
							// we shouldn't be sharing this mdc across databases,
							// and there shouldn't be multiple FdoCaches per database
							// so this should be safe.
							if (mdc != null && Marshal.IsComObject(mdc))
								Marshal.FinalReleaseComObject(mdc);
						}
						if (Marshal.IsComObject(m_odde))
							Marshal.ReleaseComObject(m_odde);
						m_odde = null;
					}
					if (m_ode != null)
					{
						if (Marshal.IsComObject(m_ode))
							Marshal.ReleaseComObject(m_ode);
						m_ode = null;
					}
				}
				catch
				{
					// If we are called from the Finalizer just ignore any errors we might get -
					// we're going away anyways. Otherwise pass the exception so that we get a report
					// for it.
					if (disposing)
						throw;
				}
			}

			m_isDisposed = true;
		}

		/// <summary>
		/// Dispose the cache, and shut down the WS factory.
		/// </summary>
		public void DisposeWithWSFactoryShutdown()
		{
			if (m_isDisposed)
				return;

			LanguageWritingSystemFactoryAccessor.Shutdown();
			Dispose();
		}

		#endregion IDisposable & Co. implementation

		#region Loading types from modules in external assemblies

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the modules.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void InitializeModules()
		{
			Assembly executingAssembly = Assembly.GetExecutingAssembly();
			foreach (Attribute attr in executingAssembly.GetCustomAttributes(true))
			{
				CellarModuleAttribute cellarModule = attr as CellarModuleAttribute;
				if (cellarModule != null && cellarModule.Location != "FDO")
				{
					ProcessCellarModuleAttribute(cellarModule);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes the cellar module attribute.
		/// </summary>
		/// <param name="attr">The attr.</param>
		/// <returns><c>true</c> if ok, <c>false</c> if assembly can't be found.</returns>
		/// ------------------------------------------------------------------------------------
		protected virtual bool ProcessCellarModuleAttribute(CellarModuleAttribute attr)
		{
			// Path of the current executing assembly without the "file://"
			string currentAssemblyPath = Assembly.GetExecutingAssembly().CodeBase.Substring(8);
			string assemblyPath = Path.Combine(Path.GetDirectoryName(currentAssemblyPath),
				attr.Location + ".dll");
			Assembly assembly;
			try
			{
				assembly = Assembly.LoadFile(assemblyPath);
			}
			catch (FileNotFoundException)
			{
				System.Diagnostics.Debug.WriteLine("Can't load module assembly " + attr.Location);
				return false;
			}

			foreach (Type type in assembly.GetTypes())
			{
				m_TypeAssemblyMap.Add(type.FullName, type.AssemblyQualifiedName);
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the assembly qualified name for the given type.
		/// </summary>
		/// <param name="typeName">Fully qualified name of the type.</param>
		/// <returns>The type or <c>null</c> if not found in the TypeAssembly map.</returns>
		/// ------------------------------------------------------------------------------------
		protected internal Type GetTypeInAssembly(string typeName)
		{
			if (m_TypeAssemblyMap.ContainsKey(typeName))
				return Type.GetType(m_TypeAssemblyMap[typeName], false);

			return Assembly.GetExecutingAssembly().GetType(typeName, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Remap typeName to newType, i.e. if we're asked to create an object of type typeName
		/// we'll create an object of newType instead.
		/// </summary>
		/// <param name="typeName">Fully qualified name of the type.</param>
		/// <param name="newType">The new type.</param>
		/// <exception cref="InvalidCastException">Throws an exception if newType isn't a
		/// subclass of typeName. (It's also OK for the two types to be the same.)
		/// </exception>
		/// ------------------------------------------------------------------------------------
		public void MapType(string typeName, Type newType)
		{
			Type substituteType = GetTypeInAssembly(typeName);
			if (!(newType == substituteType || newType.IsSubclassOf(substituteType)))
			{
				throw new InvalidCastException(string.Format("New type {0} doesn't derive from type {1}",
					newType.Name, typeName));
			}
			if (m_TypeAssemblyMap.ContainsKey(typeName))
				m_TypeAssemblyMap[typeName] = newType.AssemblyQualifiedName;
			else
				m_TypeAssemblyMap.Add(typeName, newType.AssemblyQualifiedName);

			// We also have to reset the ClassId to Type map that CmObject has so that it'll
			// use the new type.
			CmObject.s_classIdToType.Clear();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Remap typeName to newType, i.e. if we're asked to create an object of type typeName
		/// we'll create an object of newType instead.
		/// </summary>
		/// <param name="oldType">The old type.</param>
		/// <param name="newType">The new type.</param>
		/// <exception cref="InvalidCastException">Throws an exception if newType isn't a
		/// subclass of oldType.
		/// </exception>
		/// ------------------------------------------------------------------------------------
		public void MapType(Type oldType, Type newType)
		{
			MapType(oldType.FullName, newType);
		}

		#endregion

		#region Database syncronization

		/// <summary>
		/// Start the process of automatically making sync records in the database as
		/// properties are modified.
		/// </summary>
		/// <param name="appGuid"></param>
		public void MakeDbSyncRecords(Guid appGuid)
		{
			CheckDisposed();
			if (m_watcher != null)
				return;
			m_watcher = new SyncWatcher(this, appGuid);
		}

		/// <summary>
		/// Tell the cache, if it has a SyncWatcher, that changes to the specified
		/// property should be ignored (typically while we process a sync record for
		/// that property). Set to (0,0) to disable.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="flid"></param>
		public void SetIgnoreSync(int hvo, int flid)
		{
			CheckDisposed();
			if (m_watcher != null)
				m_watcher.SetIgnore(hvo, flid);
		}

		#endregion Database syncronization

		#region Public Methods

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the marker index cache.
		/// </summary>
		/// <value>The marker index cache.</value>
		/// ------------------------------------------------------------------------------------
		public IFootnoteMarkerIndexCache MarkerIndexCache
		{
			set
			{
				CheckDisposed();
				m_FootnoteIndexes = value;}
			get
			{
				CheckDisposed();
				return m_FootnoteIndexes;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the index of the footnote marker.
		/// </summary>
		/// <param name="footnoteHvo">The footnote hvo.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public int GetFootnoteMarkerIndex(int footnoteHvo)
		{
			CheckDisposed();
			return m_FootnoteIndexes.GetIndexForFootnote(footnoteHvo);
		}

		#endregion

		#region Properties
		#region Ownerless objects Properties
		// The formal definition of an 'ownerless object' is those elements
		// that are found in the FW DTD for the element FwDatabase. As of
		// 8/7/2002 these are:
		// <!ELEMENT FwDatabase
		//	(AdditionalFields | CmPossibilityList | LangProject | LgWritingSystem
		//		| ReversalIndex | ScrRefSystem | UserView)*>
		// To date, we support access to all but 'AdditionalFields'.
		// Those appear to be non-CmObject objects, which will require
		// special handling in FDO.

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Interface to the LangProject in the database.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual ILangProject LangProject
		{
			get
			{
				CheckDisposed();
				if (m_lp == null)
					m_lp = (ILangProject)CmObject.CreateFromDBObject(this, GetOwnerlessIds("LangProject").ToArray()[0], true);
				return m_lp;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The ScrRefSystem in the database.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual IScrRefSystem ScriptureReferenceSystem
		{
			get
			{
				CheckDisposed();
				if (m_srs == null)
				{
					Set<int> alIDs = GetOwnerlessIds("ScrRefSystem");
					if (alIDs.Count > 1)
						throw new Exception("Too many ScrRefSystem objects in database.");
					if (alIDs.Count == 0)
						return null;	// REVIEW: Should this also be an exception?
					m_srs = (IScrRefSystem)CmObject.CreateFromDBObject(this, alIDs.ToArray()[0]);
				}
				return m_srs;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The UserView objects in the database.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual UserViewCollection UserViewSpecs
		{
			get
			{
				CheckDisposed();
				if (m_cuv == null)
					m_cuv = UserView.Load(this, GetOwnerlessIds("UserView"));
				return m_cuv;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The LgWritingSystem objects in the database.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual LgWritingSystemCollection LanguageEncodings
		{
			get
			{
				CheckDisposed();
				if (m_cle == null)
					m_cle = SIL.FieldWorks.FDO.Cellar.LgWritingSystem.Load(this,
						GetOwnerlessIds("LgWritingSystem"));
				return m_cle;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether project includes a right-to-left writing system.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool ProjectIncludesRightToLeftWs
		{
			get
			{
				CheckDisposed();
				foreach (NamedWritingSystem nws in LangProject.GetActiveNamedWritingSystems())
				{
					ILgWritingSystem lgws = nws.GetLgWritingSystem(this);
					if (lgws.RightToLeft)
						return true;
				}
				return false;
			}
		}

		// NOTE: this property returns all writing systems in the current database. Not all of
		// those writing systems might be actually included in the current project (see
		// LangProject.GetActiveNamedWritingSystems()), and there might be additional writing
		// systems in the languages directory that are not yet imported into the database (see
		// LangProject.GetAllNamedWritingSystems()).
		///// <summary>
		///// Return an array of the IDs of all known writing systems.
		///// </summary>
		//public int[] LanguageEncodingIds
		//{
		//    get
		//    {
		//        CheckDisposed();
		//        return DbOps.ListToIntArray(GetOwnerlessIds("LgWritingSystem"));
		//    }
		//}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clear the cache of LgWritingSystem objects in the database.
		/// This is necessary when we've added a new one and want it to show up on
		/// subsequent calls to LanguageEncodings.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void ResetLanguageEncodings()
		{
			CheckDisposed();
			if (m_cle != null && m_cle.Count > 0)
				m_cle.Clear();
			m_cle = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The CmPossibilityList objects in the database that have no owner.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual CmPossibilityListCollection UserDefinedLists
		{
			get
			{
				CheckDisposed();
				if (m_cpl == null)
					m_cpl = CmPossibilityList.Load(this,
						GetOwnerlessIds("CmPossibilityList"));
				return m_cpl;
			}
		}

		#endregion	// Ownerless objects

		/// <summary>
		/// Get/set the object that manages create and modify times.
		/// </summary>
		public CreateModifyTimeManager CreateModifyManager
		{
			get
			{
				CheckDisposed();
				return m_createModifyManager;
			}
			set
			{
				CheckDisposed();
				m_createModifyManager = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets whether or not to preload data when accessing a property. Set this to
		/// <c>false</c> to improve performance after making sure all needed data is loaded.
		/// Any property that results in a miss in the cache will still be loaded automatically.
		/// If this is <c>false</c> there is the risk that the cache could return stale data.
		/// REVIEW: TE is now using this to enhance performance. Should FLEx use it, too? We
		/// need to study this issue further to see how to deal with multi-user access.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool PreloadData
		{
			get
			{
				CheckDisposed();
				return m_fPreloadData;
			}
			set
			{
				CheckDisposed();
				m_fPreloadData = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/Sets how to handle prop changed notifications
		/// </summary>
		/// <remarks>
		/// Note: We don't call VwCacheDaAccessor.SuppressPropChanges() because this is usually
		/// used in cases where we are doing to massive a change to bother processing all of
		/// the propchanges generated (e.g. during Import).
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public PropChangedHandling PropChangedHandling
		{
			get
			{
				CheckDisposed();
				return m_PropChangedHandling;
			}
			set
			{
				CheckDisposed();
				m_PropChangedHandling = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Allows special behavior in test code.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool TestMode
		{
			set { m_fTestMode = value; }
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// The name of the Database represented by this connection
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public string DatabaseName
		{
			get
			{
				CheckDisposed();
				return m_ode != null ? m_ode.Database : "";
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// The name of the DB Server represented by this connection
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public virtual string ServerName
		{
			get
			{
				CheckDisposed();
				return m_ode != null ? m_ode.Server : "";
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The default analysis writing system.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int DefaultAnalWs
		{
			get
			{
				CheckDisposed();
				return LangProject.DefaultAnalysisWritingSystem;
			}
		}

		/// <summary>
		/// The default pronunciation writing system.
		/// </summary>
		public int DefaultPronunciationWs
		{
			get
			{
				CheckDisposed();
				return LangProject.DefaultPronunciationWritingSystem;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The default vernacular writing system.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int DefaultVernWs
		{
			get
			{
				CheckDisposed();
				return LangProject.DefaultVernacularWritingSystem;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the default (and only) UI writing system.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int DefaultUserWs
		{
			get
			{
				CheckDisposed();
				return LanguageWritingSystemFactoryAccessor.UserWs;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the fallback user locale as a string (e.g. "en").
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string FallbackUserLocale
		{
			get
			{
				CheckDisposed();
				return "en";
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the fallback user writing system
		/// (i.e. LanguageWritingSystemFactoryAccessor.GetWsFromStr(FallbackUserLocale)).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int FallbackUserWs
		{
			get
			{
				CheckDisposed();
				return LanguageWritingSystemFactoryAccessor.GetWsFromStr(FallbackUserLocale);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether or not the cache is busy processing something
		/// (e.g. in the middle of an undo or redo action.)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsBusy
		{
			get
			{
				CheckDisposed();
				return m_isBusy;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the current 'Undo ...' text, if there is one, and it is undoable.
		/// </summary>
		///<returns>
		///The 'Undo ...' text, or a null string, if not undoable, or available.
		///</returns>
		/// ------------------------------------------------------------------------------------
		public string UndoText
		{
			get
			{
				CheckDisposed();
				Debug.Assert(ActionHandlerAccessor != null);
				return ActionHandlerAccessor.GetUndoText();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the current 'Redo ...' text, if there is one, and it is redoable.
		/// </summary>
		///<returns>
		///The 'Redo ...' text, or a null string, if not redoable, or available.
		///</returns>
		/// ------------------------------------------------------------------------------------
		public string RedoText
		{
			get
			{
				CheckDisposed();
				Debug.Assert(ActionHandlerAccessor != null);
				return ActionHandlerAccessor.GetRedoText();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the current 'Redo' state.
		/// </summary>
		///<returns>true, if an action is redoable, otherwise false.</returns>
		/// ------------------------------------------------------------------------------------
		public bool CanRedo
		{
			get
			{
				CheckDisposed();
				if (ActionHandlerAccessor == null)
					return false;
				return ActionHandlerAccessor.CanRedo();
			}
		}

		//#if DEBUG
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When set, FdoCache will not request anything to be loaded into the cache
		/// </summary>
		/// <remarks> This is useful to determine the "lower bound" when trying to get
		/// something based on FDO to run fast.  First, run the test with this turned off,
		/// and note the elapsed time.  Then, run it again (in the same test) with this
		/// turned on. The difference in times will give you an idea of how much time is
		/// being spent pulling things into the cache.
		/// Note that there are places, particularly in the underlying C++ cache, which know
		/// nothing about this and will try to load do things into the cache, particularly
		/// if those things are actually empty. So you also need to either run the profile
		/// or more turn on the _VERBOSE flag to make sure that you really are not hitting
		/// the database went in this mode.
		/// This property is available for debugging only because, if the cache was shared,
		/// it would be disastrous for one processed to turn this flag on.</remarks>
		/// ------------------------------------------------------------------------------------
		public bool TestingOnly_AssumeCacheFullyLoaded
		{
			set
			{
				CheckDisposed();
				m_assumeCacheFullyLoaded = value;
			}
			get
			{
				CheckDisposed();
				return m_assumeCacheFullyLoaded;
			}
		}
		//#endif

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the current 'Undo' state.
		/// </summary>
		///<returns>true, if an action is undoable, otherwise false.</returns>
		/// ------------------------------------------------------------------------------------
		public bool CanUndo
		{
			get
			{
				CheckDisposed();
				if (ActionHandlerAccessor == null)
					return false;
				return m_fUndoEnabled && ActionHandlerAccessor.CanUndo();
			}
		}

		/// <summary>
		/// There are places where we may wish to disable Undo regardless of the ActionHandler.
		/// See LT-6583 for motivation.
		/// </summary>
		public bool EnableUndo
		{
			get { return m_fUndoEnabled; }
			set { m_fUndoEnabled = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IOleDbEncap DatabaseAccessor
		{
			get
			{
				CheckDisposed();
				return (IOleDbEncap)m_ode;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IVwOleDbDa VwOleDbDaAccessor
		{
			get
			{
				CheckDisposed();
				return m_odde as IVwOleDbDa;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IFwMetaDataCache MetaDataCacheAccessor
		{
			get
			{
				CheckDisposed();
				return m_mdc as IFwMetaDataCache;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ISilDataAccess MainCacheAccessor
		{
			get
			{
				CheckDisposed();
				return m_odde;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IVwCacheDa VwCacheDaAccessor
		{
			get
			{
				CheckDisposed();
				return (IVwCacheDa)m_odde;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This property manages the ActionHandler. Since it is a COM object we want to
		/// limit the number of references to the object. Using this property will guarantee that
		/// we only get one reference to the COM object.
		/// </summary>
		/// <remarks>Use the SuppressSubTasks class instead of changing the action handler
		/// directly.</remarks>
		/// ------------------------------------------------------------------------------------
		public virtual IActionHandler ActionHandlerAccessor
		{
			get
			{
				CheckDisposed();
				if (m_actionHandler == null)
					m_actionHandler = m_odde.GetActionHandler();

				return m_actionHandler;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the Action Handler. This method should only be used by SuppressSubTasks object.
		/// </summary>
		/// <param name="ah">The new action handler</param>
		/// ------------------------------------------------------------------------------------
		internal protected void SetActionHandler(IActionHandler ah)
		{
			CheckDisposed();
			if (m_actionHandler != ah)
			{
				if (ah != null || !m_fAddAllActionsForTestCache)
				{
					m_actionHandler = ah;
					m_odde.SetActionHandler(ah);
				}
			}
		}

		/// <summary>
		/// Use this property to prevent suppressing action handler with SuppressSubTasks.
		/// Some unit tests require adding all undo actions, to restore database to initial state for each test.
		/// </summary>
		public bool AddAllActionsForTests
		{
			get
			{
				CheckDisposed();
				return m_fAddAllActionsForTestCache;
			}
			set
			{
				CheckDisposed();
				m_fAddAllActionsForTestCache = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ILgWritingSystemFactory LanguageWritingSystemFactoryAccessor
		{
			get
			{
				CheckDisposed();
				return m_lef;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a default character property engine (can be used, for example, to query the
		/// Unicode properties of any character, regardless of writing system).
		/// Callers are responsible for cleaning up with code like the following:
		/// if (Marshal.IsComObject(unicodeCharProps))
		///		Marshal.ReleaseComObject(unicodeCharProps);
		///	unicodeCharProps = null;
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		public ILgCharacterPropertyEngine UnicodeCharProps
		{
			get
			{
				CheckDisposed();
				// We can't get the Unicode char props and hang onto them, because that locks
				// some of the ICU data files into memory where they can't be changed (for
				// instance by adding a new writing system and trying to install it).
				ILgCharacterPropertyEngine unicodeCharProps = null;
				if (m_lef != null)
					unicodeCharProps = m_lef.UnicodeCharProps;
				Debug.Assert(unicodeCharProps != null,
					"This should only be null in tests. Do not use this property in tests using a mocked cache.");
				return unicodeCharProps;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Collection of all registered change watchers
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public List<ChangeWatcher> ChangeWatchers
		{
			get
			{
				CheckDisposed();
				if (m_rgChangeWatchers == null)
					m_rgChangeWatchers = new List<ChangeWatcher>(1);
				return m_rgChangeWatchers;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the next available dummy flid.
		/// </summary>
		/// <remarks>
		/// Initially, this start at -1000, and get decremented for each call,
		/// so it won't clash with real flids, which are all positive integers.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public static int DummyFlid
		{
			get { return m_sDummyFlid--; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the progress bar.
		/// </summary>
		/// <value>The progress bar.</value>
		/// ------------------------------------------------------------------------------------
		public IAdvInd3 ProgressBar
		{
			get { return m_progressBar; }
			set { m_progressBar = value; }
		}
		#endregion	// Properties

		#region Load data from database into cache
		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Load the object(s) specified in sQuery from the database into the cache.
		/// [Note: If the query returns multiple rows, they will all be added to the cache.]
		/// </summary>
		/// <param name="sQuery">SQL query to perform.</param>
		/// <param name="qdcsColumnSpec">Specification of values returned by sQuery.</param>
		/// <param name="hvoObjectID">ID of object being loaded.
		/// [Note: If ID is included in the sQuery (and thus, is in qdcsColumnSpec),
		/// then this parameter can be 0.]
		/// </param>
		/// -------------------------------------------------------------------------------------
		public virtual void  LoadData(string sQuery, IDbColSpec qdcsColumnSpec, int hvoObjectID)
		{
			CheckDisposed();
			//			VwOleDbDaAccessor.Load(sQuery, qdcsColumnSpec, hvoObjectID, 0, null, false);alse);
			RequestData(sQuery, qdcsColumnSpec, hvoObjectID);
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Load all of the columns in the CmObject table in the database into the cache.
		/// </summary>
		/// <param name="hvoObj">The ID of the object to load.
		/// [Note: If hvoObj is less than 1,
		/// then all objects in the CmObject table will be loaded.]
		/// </param>
		/// -------------------------------------------------------------------------------------
		public virtual void LoadBasicObjectInfo(int hvoObj)
		{
			CheckDisposed();
			if (!(m_odde is IVwOleDbDa))
				return; // some sort of non-database cache, maybe for testing, assume preloaded.
			string sQry = "select * from CmObject";

			if (hvoObj > 0)
				sQry += " where id=" + hvoObj.ToString();
			else
				hvoObj = 0;

			IDbColSpec dcs = DbColSpecClass.Create();
			dcs.Push((int)DbColType.koctBaseId, 0, 0, 0);	// ID
			dcs.Push((int)DbColType.koctGuid, 1,
				(int)CmObjectFields.kflidCmObject_Guid, 0);
			dcs.Push((int)DbColType.koctInt, 1,
				(int)CmObjectFields.kflidCmObject_Class, 0);
			dcs.Push((int)DbColType.koctObj, 1,
				(int)CmObjectFields.kflidCmObject_Owner, 0);
			dcs.Push((int)DbColType.koctInt, 1,
				(int)CmObjectFields.kflidCmObject_OwnFlid, 0);
			dcs.Push((int)DbColType.koctInt, 1,
				(int)CmObjectFields.kflidCmObject_OwnOrd, 0);	//OwnOrd$
			dcs.Push((int)DbColType.koctTimeStamp, 1, 0, 0); //UpdStmp$
			dcs.Push((int)DbColType.koctTime, 1, 0, 0);	// UpdDttm$

			//			LoadData(sQry, dcs, hvoObj);
			RequestData(sQry, dcs, hvoObj);

			Marshal.ReleaseComObject(dcs);
		}


		// note: just an attrName isn't enough 'cause we need to know which class (could be a super class) has the attr.
		// e.g. sViewName = CmPossibility_Name
		/// -------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="hvoOwningObj"></param>
		/// <param name="flidTag"></param>
		/// <param name="ws"></param>
		/// <param name="sViewName"></param>
		/// -------------------------------------------------------------------------------------
		public virtual void LoadMultiUnicodeAlt(int hvoOwningObj, int flidTag, int ws, string sViewName)
		{
			CheckDisposed();

			// have to reorder and omit some fields in order to meet expectations of the odde.Load() call

			/*			string sQry = "select obj, txt, fmt from " + sViewName +
							" where obj = " + hvoOwningObj.ToString() + " and ws= " + ws;

						IDbColSpec dcs = DbColSpecClass.Create();
						dcs.Push((int)DbColType.koctBaseId, 0, 0, 0);
						dcs.Push((int)DbColType.koctMlsAlt, 1, flidTag, ws);
						dcs.Push((int)DbColType.koctFmt, 1, flidTag, 0);
			*/
			string sQry = "select obj, txt from " + sViewName +
				" where obj = " + hvoOwningObj.ToString() + " and ws= " + ws;


			IDbColSpec dcs = DbColSpecClass.Create();
			dcs.Push((int)DbColType.koctBaseId, 0, 0, 0);
			dcs.Push((int)DbColType.koctMltAlt, 1, flidTag, ws);

			// VwOleDbDaAccessor.Load(sQry, dcs, hvoOwningObj, 0, null, false);
			RequestData(sQry, dcs, hvoOwningObj);
			Marshal.ReleaseComObject(dcs);
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// potentially load the database into the cache, depending on the current cache mode.
		/// </summary>
		/// <remarks> no code should call m_odde.Load() directly; everything should go through
		/// this.</remarks>
		/// <param name="queryString"></param>
		/// <param name="databaseColumnSpec"></param>
		/// <param name="hvoOwningObj"></param>
		/// -------------------------------------------------------------------------------------
		private void RequestData(string queryString, IDbColSpec databaseColumnSpec,
			int hvoOwningObj)
		{
			//#if DEBUG
			if (!m_assumeCacheFullyLoaded)
				//#endif
			{
				if (ProgressBar != null)
					ProgressBar.Position = 0;

#if _verbose
					DateTime dtstart = DateTime.Now;
					Trace.WriteLine(queryString);
#endif
				((IVwOleDbDa)m_odde).Load(queryString, databaseColumnSpec, hvoOwningObj, 0,
					ProgressBar, false);
#if _verbose
					TimeSpan tsTimeSpan = new TimeSpan(DateTime.Now.Ticks - dtstart.Ticks);
					Trace.WriteLine("    took: " + tsTimeSpan.TotalSeconds.ToString() + " Seconds: ");
#endif

				if (ProgressBar != null)
					ProgressBar.Position = 0;
			}
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="hvoOwningObj"></param>
		/// <param name="flidTag"></param>
		/// <param name="ws"></param>
		/// <param name="sViewName"></param>
		/// -------------------------------------------------------------------------------------
		public virtual void LoadMultiStringAlt(int hvoOwningObj, int flidTag, int ws, string sViewName)
		{
			CheckDisposed();
			int cptFieldType = MetaDataCacheAccessor.GetFieldType((uint)flidTag);

			// We have to reorder and omit some fields in order
			// to meet expectations of the odde.Load() call

			string sQry;
			IDbColSpec dcs = DbColSpecClass.Create();
			dcs.Push((int)DbColType.koctBaseId, 0, 0, 0);
			dcs.Push((int)DbColType.koctMlsAlt, 1, flidTag, ws);

			if (cptFieldType == (int)CellarModuleDefns.kcptMultiUnicode)
			{
				sQry = "select obj, txt from " + sViewName +
					" where obj = " + hvoOwningObj.ToString() +
					" and ws= " + ws;
			}
			else
			{
				sQry = "select obj, txt, fmt from " + sViewName +
					" where obj = " + hvoOwningObj.ToString() +
					" and ws= " + ws;
				dcs.Push((int)DbColType.koctFmt, 1, flidTag, ws);
			}

			RequestData(sQry, dcs, hvoOwningObj);
			//((IVwOleDbDa)m_odde).Load(sQry, dcs, hvoOwningObj, 0, null, false);
			Marshal.ReleaseComObject(dcs);
		}


		#endregion	// Load data from database into cache

		#region Create data methods
		/// -----------------------------------------------------------------------------------
		///<summary>
		///Create a new object.
		///</summary>
		///<param name="classID">Class of new object.</param>
		///<param name="hvoOwner">Owner of new object.</param>
		///<param name="flidOwning">Owning flid of new object.</param>
		///<param name="ihvo">Index for where to insert new object in the sequence.
		///[Note: This parameter is ignored when <paramref name="flidOwning"/> is not
		///kcptOwningSequence.]
		///</param>
		///<returns>The ID of the new object, or 0 if not created.</returns>
		///<exception cref="InvalidEnumArgumentException">
		///Thrown when <paramref name="flidOwning"/> is not a valid part of the FieldType enum.
		///</exception>
		/// -----------------------------------------------------------------------------------
		public virtual int CreateObject(int classID, int hvoOwner, int flidOwning, int ihvo)
		{
			CheckDisposed();
			int iType = m_mdc.GetFieldType((uint)flidOwning);
			switch (iType)
			{
				default:
					throw(new System.ArgumentException("Not an owning property", "flidOwning"));
				case (int)FieldType.kcptOwningCollection:
					iType = -1;
					break;
				case (int)FieldType.kcptOwningAtom:
					iType = -2;
					break;
				case (int)FieldType.kcptOwningSequence:
					if ((object)ihvo == null)
						ihvo = 0;
					if (ihvo < 0)
						throw new ArgumentException("Cannot be less than zero", "ihvo");
					iType = ihvo;
					break;
			}
			int hvoRet = m_odde.MakeNewObject(classID, hvoOwner, flidOwning, iType);
			// TomB: other versions of Append issue PropChanged, but this one intentionally
			// leaves that up to the app because often newly created objects require some
			// initialization before they are readyb to be displayed.
			ICmObject obj = CmObject.CreateFromDBObject(this, hvoRet);
			(obj as CmObject).InitNewInternal();
			// Populate basic fields with newly created information.
			//LoadBasicObjectInfo(hvoRet);
			return hvoRet;
		}

		/// -----------------------------------------------------------------------------------
		///<summary>
		///Create a new, ownerless, object.
		///</summary>
		///<param name="classID">Class of new object.</param>
		///<returns>The ID of the new object, or 0 if not created.</returns>
		/// -----------------------------------------------------------------------------------
		public virtual int CreateObject(int classID)
		{
			CheckDisposed();
			// TODO(Undo): Figure out how to put this into cache processing with Undo/Redo
			// capability (and whether its worth doing this).

			// REVIEW (EberhardB): Do we need this method?
			// YES, for ownerless objects (eg. pictures)
			Debug.Assert(classID > 0);

			bool fIsNull;
			uint uintSize = (uint)Marshal.SizeOf(typeof(uint));
			IOleDbCommand odc = null;
			m_ode.CreateCommand(out odc);
			odc.SetParameter(1, (uint)DBPARAMFLAGSENUM.DBPARAMFLAGS_ISOUTPUT,
				null, (ushort)DBTYPEENUM.DBTYPE_I4,
				new uint[1] {0}, uintSize);
			string sSql = string.Format("exec CreateObject$ {0}, ? output, null", classID);
			using (ArrayPtr rgHvo = MarshalEx.ArrayToNative(1, typeof(uint)))
			{
				try
				{
					odc.ExecCommand(sSql, (int)SqlStmtType.knSqlStmtStoredProcedure);
					odc.GetParameter(1, rgHvo, uintSize, out fIsNull);
				}
				finally
				{
					DbOps.ShutdownODC(ref odc);
				}
				if (fIsNull)
					throw new Exception("Unable to create new object");
				int hvoRet = (int)(((uint[])MarshalEx.NativeToArray(rgHvo, 1, typeof(uint)))[0]);
				ICmObject obj = CmObject.CreateFromDBObject(this, hvoRet);
				(obj as CmObject).InitNewInternal();
				// Populate basic fields with newly created information.
				//LoadBasicObjectInfo(hvoRet);
				if (ActionHandlerAccessor != null)
					ActionHandlerAccessor.AddAction(new UndoCreateObject(classID, hvoRet, obj.Guid.ToString(), this));
				return hvoRet;
			}
		}


		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Create a Tss from a string, using the default vernacular encoding.
		/// </summary>
		/// <param name="src"></param>
		/// <returns></returns>
		/// -----------------------------------------------------------------------------------
		public ITsString MakeVernTss(string src)
		{
			CheckDisposed();
			return StringUtils.MakeTss(src, DefaultVernWs);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Create a Tss from a string, using the default analysis encoding.
		/// </summary>
		/// <param name="src"></param>
		/// <returns></returns>
		/// -----------------------------------------------------------------------------------
		public ITsString MakeAnalysisTss(string src)
		{
			CheckDisposed();
			return StringUtils.MakeTss(src, DefaultAnalWs);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Create a Tss from a string, using the default user interface encoding.
		/// </summary>
		/// <param name="src"></param>
		/// <returns></returns>
		/// -----------------------------------------------------------------------------------
		public ITsString MakeUserTss(string src)
		{
			CheckDisposed();
			ITsStrFactory tsf = TsStrFactoryClass.Create();
			if (src == null)
				src = "";
			return tsf.MakeString(src, DefaultUserWs);
		}

		#endregion	// Create data methods

		#region Move data
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Change the owner of an object in an atomic or collection owning property.
		/// </summary>
		/// <param name="hvo">ID of the object that gets moved to new owner.</param>
		/// <param name="hvoNewOwner">ID of the new owning object.</param>
		/// <param name="flidNewOwner">Field ID where hvo will be owned.</param>
		/// ------------------------------------------------------------------------------------
		public void ChangeOwner(int hvo, int hvoNewOwner, int flidNewOwner)
		{
			CheckDisposed();
			int hvoOldOwner = ((IVwOleDbDa)m_odde).get_ObjOwner(hvo);
			int flidOldOwner = ((IVwOleDbDa)m_odde).get_ObjOwnFlid(hvo);
			int ihvoOld = ((ISilDataAccess)m_odde).GetObjIndex(hvoOldOwner, flidOldOwner, hvo);
			FieldType ihvoNewType = GetFieldType(flidNewOwner);

			if (ihvoNewType == FieldType.kcptOwningSequence)
				throw new ArgumentException("Invalid flid for new home.", "flidNewOwner");

			m_odde.MoveOwn(hvoOldOwner, flidOldOwner, hvo, hvoNewOwner, flidNewOwner, 0);

			// Notify old guy of his loss.
			PropChanged(null, PropChangeType.kpctNotifyAll, hvoOldOwner, flidOldOwner,
				ihvoOld, 0, 1);
			// Notify new guy of gain.
			PropChanged(null, PropChangeType.kpctNotifyAll, hvoNewOwner, flidNewOwner,
				0, 1, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Change the owner of an object in a sequence owning property.
		/// </summary>
		/// <param name="hvo">ID of the object that gets moved to new owner.</param>
		/// <param name="hvoNewOwner">ID of the new owning object.</param>
		/// <param name="flidNewOwner">Field ID where hvo will be owned.</param>
		/// <param name="iAt">Location to put hvo in flidNewOwner.</param>
		/// ------------------------------------------------------------------------------------
		public void ChangeOwner(int hvo, int hvoNewOwner, int flidNewOwner, int iAt)
		{
			CheckDisposed();
			int hvoOldOwner = ((IVwOleDbDa)m_odde).get_ObjOwner(hvo);
			int flidOldOwner = ((IVwOleDbDa)m_odde).get_ObjOwnFlid(hvo);
			int ihvoOld = ((ISilDataAccess)m_odde).GetObjIndex(hvoOldOwner, flidOldOwner, hvo);
			FieldType ihvoNewType = GetFieldType(flidNewOwner);
			FieldType ihvoOldType = GetFieldType(flidOldOwner);

			if (iAt < 0)
				throw new System.ArgumentException("Invalid location.", "iAt");
			if (ihvoNewType != FieldType.kcptOwningSequence)
				throw new System.ArgumentException("Invalid flid for new home.", "flidNewOwner");

			m_odde.MoveOwn(hvoOldOwner, flidOldOwner, hvo, hvoNewOwner, flidNewOwner, iAt);

			// Notify old guy of his loss.
			PropChanged(null, PropChangeType.kpctNotifyAll, hvoOldOwner, flidOldOwner,
				ihvoOld, 0, 1);
			// Notify new guy of gain.
			PropChanged(null, PropChangeType.kpctNotifyAll, hvoNewOwner, flidNewOwner,
				iAt, 1, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Move object(s) to new owner.
		/// </summary>
		/// <param name="hvoSrcOwner">Old owner's ID.</param>
		/// <param name="flidSrc">Old owning flid.</param>
		/// <param name="ihvoStart">Index in flidSrc for first object to be moved.</param>
		/// <param name="ihvoEnd">Index in flidSrc for last object to move.</param>
		/// <param name="hvoDstOwner">New owner's ID.</param>
		/// <param name="flidDst">New owning flid.</param>
		/// <param name="ihvoDstStart">
		/// Index point at which to move the objects in flidDst.
		/// </param>
		/// <remarks>if this MoveOwningSequence is undone/redone, no PropChanged is issued.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public void MoveOwningSequence(int hvoSrcOwner, int flidSrc, int ihvoStart, int ihvoEnd,
			int hvoDstOwner, int flidDst, int ihvoDstStart)
		{
			CheckDisposed();
			m_odde.MoveOwnSeq(hvoSrcOwner, flidSrc, ihvoStart, ihvoEnd, hvoDstOwner,
				flidDst, ihvoDstStart);
			int cobjMoved = ihvoEnd - ihvoStart + 1;
			if (hvoDstOwner == hvoSrcOwner && flidDst == flidSrc)
			{
				// Move in same property: best to do just one PropChanged (doing two can
				// somehow leave the Views code in a bad state, probably somehow because
				// the first propchanged implies a state of the property, with the objects removed,
				// that isn't accurate).
				int ihvoMin = Math.Min(ihvoStart, ihvoDstStart); // first item in prop affected.
				int ihvoLim = Math.Max(ihvoDstStart, ihvoEnd + 1); // item after range affected
				cobjMoved = ihvoLim - ihvoMin;
				PropChanged(null, PropChangeType.kpctNotifyAll, hvoSrcOwner, flidSrc,
					ihvoMin, cobjMoved, cobjMoved);
			}
			else
			{
				// Notify old guy of his loss.
				PropChanged(null, PropChangeType.kpctNotifyAll, hvoSrcOwner, flidSrc,
					ihvoStart, 0, cobjMoved);
				// Notify new guy of gain.
				PropChanged(null, PropChangeType.kpctNotifyAll, hvoDstOwner, flidDst,
					ihvoDstStart, cobjMoved, 0);
			}
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copies the object <paramref name="hvoSrc"/> to the owning sequence field
		/// <paramref name="flidDestOwner"/> owned by <paramref name="hvoDestOwner"/>
		/// </summary>
		/// <param name="hvoSrc">The object to copy</param>
		/// <param name="hvoDestOwner">The new owner</param>
		/// <param name="flidDestOwner">The field in which to copy</param>
		/// <returns>HVO of the new copied object</returns>
		/// ------------------------------------------------------------------------------------
		public int CopyObject(int hvoSrc, int hvoDestOwner, int flidDestOwner)
		{
			CheckDisposed();
			return CopyObject(hvoSrc, hvoDestOwner, flidDestOwner, -1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copies the object <paramref name="hvoSrc"/> to the field
		/// <paramref name="flidDestOwner"/> owned by <paramref name="hvoDestOwner"/>
		/// </summary>
		/// <param name="hvoSrc">The object to copy</param>
		/// <param name="hvoDestOwner">The new owner</param>
		/// <param name="flidDestOwner">The field in which to copy</param>
		/// <param name="hvoDstStart">The ID of the object before which the copied object will
		/// be inserted, for owning sequences. This must be -1 for fields that are not owning
		/// sequences. If -1 for owning sequences, the object will be appended to the list.
		/// </param>
		/// <returns>HVO of the new copied object</returns>
		/// ------------------------------------------------------------------------------------
		public virtual int CopyObject(int hvoSrc, int hvoDestOwner, int flidDestOwner,
			int hvoDstStart)
		{
			CheckDisposed();
			IOleDbCommand odc = null;
			m_ode.CreateCommand(out odc);
			// Need to wrap this puppy in a transaction because if anything fails in the s'proc
			// the whole thing is hosed.
			bool fOpenedTransaction = false;
			if (!m_ode.IsTransactionOpen())
			{
				Debug.WriteLine("Normally we should already have a transaction open by now, as part of an undoable sequence.");
				fOpenedTransaction = true;
				m_ode.BeginTrans();
			}
			bool fIsNull;
			uint uintSize = (uint)Marshal.SizeOf(typeof(uint));
			odc.SetParameter(1, (uint)DBPARAMFLAGSENUM.DBPARAMFLAGS_ISOUTPUT,
				null, (ushort)DBTYPEENUM.DBTYPE_I4, new uint[] { 0 }, uintSize);
			string destString = (hvoDestOwner > 0 ? hvoDestOwner.ToString() : "NULL");
			string flidString = (flidDestOwner > 0 ? flidDestOwner.ToString() : "NULL");
			string sSql = string.Format("exec CopyObj$ {0}, {1}, {2}, {3}, ? output", hvoSrc,
				destString, flidString, ((hvoDstStart < 0) ? "NULL" : hvoDstStart.ToString()));
			int hvoRet = 0;
			try
			{
				odc.ExecCommand(sSql, (int)SqlStmtType.knSqlStmtStoredProcedure);
				using (ArrayPtr rgHvo = MarshalEx.ArrayToNative(1, typeof(uint)))
				{
					odc.GetParameter(1, rgHvo, uintSize, out fIsNull);

					if (fIsNull)
						throw new Exception("Unable to copy object");
					hvoRet = (int)(((uint[])MarshalEx.NativeToArray(rgHvo, 1, typeof(uint)))[0]);
				}

				// Hook this in to the Undo/Redo processing if we can.
				IActionHandler ah = ActionHandlerAccessor;
				if (ah != null)
				{
					DbOps.ShutdownODC(ref odc);
					m_ode.CreateCommand(out odc);

					// Create the SqlUndoAction
					ISqlUndoAction sqlua = SqlUndoActionClass.Create();
					// TODO: This could lead to a corrupted database. We need a better
					// way of redoing copyobj! (fixed temporarily until we actually have
					// a plan to allow the redo of CopyObj)
					// This is not redoable, but the following SQL would sort of redo it if
					// the stored procedure paid attention to the incoming value of the last
					// parameter.  However, any subobjects might change ids randomly in the
					// redo, and that would not be good.  :-(

					// Not setting a redo string now makes it so the
					// SqlUndoAction is not redoable. This causes the redo stack to get
					// deleted after it is undone, but is better than trying to verify
					// using the method below which will always cause the redo to fail,
					// leaving the database in an unknown state.
					//string sRedo = string.Format("exec CopyObj$ {0}, {1}, {2}, {3}",
					//    hvoSrc, hvoDestOwner, flidDestOwner, hvoRet);
					//string sVerifyRedo = "select 0";
					//sqlua.AddRedoCommand(m_ode, odc, sRedo);
					//sqlua.VerifyRedoable(m_ode, odc, sVerifyRedo);

					// Undoing is simple, but this verification is unbelievably weak.
					// TODO: Feel free to beef up the verification!
					string sUndo = string.Format("EXEC DeleteObjects '{0}'", hvoRet);
					string sVerifyUndo = string.Format(
						"select count(id) from CmObject where id = {0}", hvoRet);
					sqlua.AddUndoCommand(m_ode, odc, sUndo);
					sqlua.VerifyUndoable(m_ode, odc, sVerifyUndo);

					if (flidDestOwner != 0)
					{
						// Add AddUndoReloadInfo/AddRedoReloadInfo for vector so that the cache
						// gets updated properly!
						string reloadSql =
							string.Format("select [id], [owner$], [ownflid$] from CmObject " +
									"where [owner$]={0} and [OwnFlid$]={1} order by [OwnOrd$]",
									hvoDestOwner, flidDestOwner);
						DbColSpec dcs = DbColSpecClass.Create();
						dcs.Push((int)DbColType.koctObjVec, 0, flidDestOwner, 0);
						dcs.Push((int)DbColType.koctObj, 1, (int)CmObjectFields.kflidCmObject_Owner, 0);
						dcs.Push((int)DbColType.koctInt, 1, (int)CmObjectFields.kflidCmObject_OwnFlid, 0);
						// Note: The undo and redo reload statements are the same.
						sqlua.AddUndoReloadInfo(VwOleDbDaAccessor, reloadSql, dcs, hvoDestOwner, 0, null);
						sqlua.AddRedoReloadInfo(VwOleDbDaAccessor, reloadSql, dcs, hvoDestOwner, 0, null);
					}

					ah.AddAction((IUndoAction)sqlua);

					// FWC-16: Some of our tests (i.e. at least
					// TeImportTestsWithDb.InsertBookAfterCancelledImport) hang because sqlnclir.dll
					// isn't unloaded. It seems that the garbage collector hasn't yet collected
					// the RCW of sqlua which causes the SQLNCLI to still be in use. If you
					// run in debug mode you can see a message in the output window "Object was open".
					// By explicitly calling FinalReleaseComObject we release the RCW at this point,
					// so later when we dispose the FdoCache the OleDbCommand and OleDbEncap objects
					// can be released which in turn releases SQLNCLI.
					Marshal.FinalReleaseComObject(sqlua);
				}

				if (fOpenedTransaction)
					m_ode.CommitTrans();
			}
			finally
			{
				DbOps.ShutdownODC(ref odc);
			}
			// Now we should make sure the cache is up to date. We may already have loaded
			// the property we just modified!
			if (hvoDestOwner > 0 && flidDestOwner > 0)
			{
				int cpt = MetaDataCacheAccessor.GetFieldType((uint)flidDestOwner);
				switch(cpt)
				{
					case (int)FieldType.kcptOwningAtom:
						VwCacheDaAccessor.CacheObjProp(hvoDestOwner, flidDestOwner, hvoRet);
						break;
					default:
						// not super efficient, but at least safe...wish we could just clear info
						// about that one property...
						VwCacheDaAccessor.ClearInfoAbout(hvoDestOwner, VwClearInfoAction.kciaRemoveObjectInfoOnly);
						break;
				}
			}
			return hvoRet;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copy all objects in a sequence to the same property in another object. This is
		/// primarily intended for copying objects into an initially empty sequence (of a new
		/// object), but if the existing object already has items in the sequence, the copied
		/// items will be appended to the list.
		/// </summary>
		/// <param name="srcOwnSequence">Source owning sequence.</param>
		/// <param name="hvoDstOwner">New owner's ID.</param>
		/// ------------------------------------------------------------------------------------
		public void CopyOwningSequence<T>(FdoOwningSequence<T> srcOwnSequence, int hvoDstOwner)
			where T:ICmObject
		{
			CopyOwningSequence(srcOwnSequence, hvoDstOwner, srcOwnSequence.Flid, -1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copy all objects in a sequence to another (initially empty) sequence.
		/// </summary>
		/// <param name="srcOwnSequence">Source owning sequence.</param>
		/// <param name="hvoDstOwner">New owner's ID.</param>
		/// <param name="flidDst">New owning flid.</param>
		/// <param name="hvoDstStart">The ID of the object before which the copied object will
		/// be inserted. If -1, the object will be appended to the list.</param>
		/// ------------------------------------------------------------------------------------
		public void CopyOwningSequence<T>(FdoOwningSequence<T> srcOwnSequence, int hvoDstOwner,
			int flidDst, int hvoDstStart) where T : ICmObject
		{
			CheckDisposed();

			Debug.Assert(hvoDstStart < 0 ||
				(new FdoOwningSequence<T>(this, hvoDstOwner, flidDst)).Contains(hvoDstStart));

			foreach (int hvoSrcObj in srcOwnSequence.HvoArray)
				CopyObject(hvoSrcObj, hvoDstOwner, flidDst, hvoDstStart);
		}
		#endregion	// Move data

		#region Delete object & Remove reference methods
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Deletes hvo object from database and cache.
		/// </summary>
		/// <param name="hvo">Id of object to be deleted.</param>
		/// <exception cref="System.Exception">
		/// Thrown when the Oner's ID is in the cache, but not the owning flid.
		/// </exception>
		/// -----------------------------------------------------------------------------------
		public void DeleteObject(int hvo)
		{
			CheckDisposed();
			bool fNewTransaction = false;
			string sSavePointName = string.Empty;
			if (DatabaseAccessor != null && !DatabaseAccessor.IsTransactionOpen())
			{
				DatabaseAccessor.SetSavePointOrBeginTrans(out sSavePointName);
				fNewTransaction = true;
			}

			int hvoOwner;
			int flidOwner;
			int ihvoIndex = 0;
			try
			{
				string errorMessage = String.Format("The object being deleted ({0}) may still be referred to by other objects.", hvo);
				hvoOwner = m_odde.get_ObjectProp(hvo, (int)CmObjectFields.kflidCmObject_Owner);
				if (hvoOwner == 0)
				{
					// No owner, at least in cache, so zap it on the cheap,
					// which will skip most messing with the cache.
					try
					{
						// Clear the information about the object from the cache before deleting
						// the object so that information relating to its owned objects will be
						// cleared as well.  See LT-5158.
						((IVwCacheDa)m_odde).ClearInfoAbout(hvo,
							VwClearInfoAction.kciaRemoveAllObjectInfo);
						m_odde.DeleteObj(hvo);
					}
					catch (Exception err)
					{
						throw new ArgumentException(errorMessage, "hvo", err);
					}
					// No need to call PropChanged, since owner is not in cache.
					// But we better close the transaction if we started it!
					if (DatabaseAccessor != null && fNewTransaction)
						DatabaseAccessor.CommitTrans();
					return;
				}

				// Get owning flid, which we require to be in the cache, since the owner's ID was in it.
				flidOwner = m_odde.get_IntProp(hvo, (int)CmObjectFields.kflidCmObject_OwnFlid);
				if (flidOwner < 1)
					throw new System.Exception("The owning flid can't be loaded.");

				// Get field type.
				int ihvo = -1;	// Good for collections or sequences.
				int iType = m_mdc.GetFieldType((uint)flidOwner);

				// Force the owning property to be in the cache.
				// Earlier versions of this method used DeleteObj if the owning property was not
				// cached. This is not always safe. For example, it is possible for a virtual
				// property to be in the cache that is derived from this owning property, and
				// that we are counting on being told that the owning one changed in order to
				// update it; yet possibly the owning property was never loaded, or has been
				// removed.
				if (iType == (int)FieldType.kcptOwningAtom)
				{
					m_odde.get_ObjectProp(hvoOwner, flidOwner);
					ihvo = -2;
				}
				else
				{
					// This was removed because it didn't look like it was needed :)
					// If it turns out that it is needed we need to put it back ;)
					//m_odde.get_VecSize(hvoOwner, flidOwner);
					ihvoIndex = ((ISilDataAccess)m_odde).GetObjIndex(hvoOwner, flidOwner, hvo);
				}
				try
				{
					// Clear the information about the object from the cache before deleting the
					// object so that information relating to its owned objects will be cleared
					// as well.  See LT-5158.
					((IVwCacheDa)m_odde).ClearInfoAbout(hvo,
						VwClearInfoAction.kciaRemoveAllObjectInfo);
					m_odde.DeleteObjOwner(hvoOwner, hvo, flidOwner, ihvo);
				}
				catch (Exception err)
				{
					throw new ArgumentException(errorMessage, "hvo", err);
				}
			}
			catch
			{
				Debug.Assert(DatabaseAccessor != null);
				if(DatabaseAccessor != null && fNewTransaction)
					DatabaseAccessor.RollbackSavePoint(sSavePointName);
				return;
			}
			if (DatabaseAccessor != null && fNewTransaction)
				DatabaseAccessor.CommitTrans();
			PropChanged(null, PropChangeType.kpctNotifyAll, hvoOwner, flidOwner,
				ihvoIndex, 0, 1);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Remove hvoItem from a reference property.
		/// </summary>
		/// <param name="hvoObj">ID of object that refers to hvoItem.</param>
		/// <param name="flid">Field ID that holds hvoItem.</param>
		/// <param name="hvoItem">ID of obejct to remove from flid.</param>
		/// <exception cref="System.ArgumentException">
		/// Thrown when flid is not a reference property.
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// Thrown when hvoItem is not in reference property vector.
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// Thrown when hvoItem is not in atomic reference property.
		/// </exception>
		/// -----------------------------------------------------------------------------------
		public void RemoveReference(int hvoObj, int flid, int hvoItem)
		{
			CheckDisposed();
			if (!IsReferenceProperty(flid))
				throw new System.ArgumentException("Field is not a reference property.", "flid");
			if (IsVectorProperty(flid))
			{
				// Collection or sequence property.
				int ihvo = ((ISilDataAccess)m_odde).GetObjIndex(hvoObj, flid, hvoItem);
				if (ihvo == -1)
					throw new System.ArgumentException("Item is not in the vector.", "hvoItem");
				m_odde.Replace(hvoObj, flid, ihvo, ihvo + 1, new int[1] {0}, 0);
				PropChanged(null, PropChangeType.kpctNotifyAll, hvoObj, flid,
					ihvo, 0, 1);
			}
			else
			{
				// Atomic reference property.
				int hvoCurr = m_odde.get_ObjectProp(hvoObj, flid);
				if (hvoCurr != hvoItem)
					throw new System.ArgumentException("Item is not in the property.", "hvoItem");
				// Set atomic reference property to NULL;
				if (hvoItem > 0)
				{
					m_odde.SetObjProp(hvoObj, flid, 0);
					PropChanged(null, PropChangeType.kpctNotifyAll, hvoObj, flid,
						0, 0, 1);
				}
			}
		}
		#endregion	// Delete object & Remove reference methods

		#region Utility / Conversion

		/// <summary>
		/// Returns the ClassId for the given flid.
		/// </summary>
		/// <param name="flid"></param>
		public int GetOwnClsId(int flid)
		{
			CheckDisposed();
			if (flid <= 0)
				return 0;	// must be a fake flid.
			uint clsid = m_mdc.GetOwnClsId((uint)flid);
			return (int)clsid;
		}

		/// <summary>
		/// Answer whether clidTest is, or is a subclass of, clidSig.
		/// That is, either clidTest is the same as clidSig, or one of the base classes of clidTest is clidSig.
		/// As a special case, if clidSig is 0, all classes are considered to match
		/// </summary>
		/// <param name="clidTest"></param>
		/// <param name="clidSig"></param>
		/// <returns></returns>
		public bool ClassIsOrInheritsFrom(uint clidTest, uint clidSig)
		{
			CheckDisposed();
			if (clidSig == 0)
				return true;
			IFwMetaDataCache mdc = this.MetaDataCacheAccessor;
			for (uint clidBase = clidTest; clidBase != 0; clidBase = mdc.GetBaseClsId(clidTest))
			{
				if (clidBase == clidSig)
					return true;
				clidTest = clidBase;
			}
			return false;
		}

		#endregion Utility / Conversion

		#region Put data into cache & retrieve data from cache methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the class of an object. (Load object from DB, if not in cache.)
		/// </summary>
		/// <param name="hvo">The object to get the class for.</param>
		/// <returns>The class ID.</returns>
		/// ------------------------------------------------------------------------------------
		public virtual int GetClassOfObject(int hvo)
		{
			CheckDisposed();
			try
			{
				// Check to see if it already in the cache.
				int clsid = m_odde.get_IntProp(hvo, (int)CmObjectFields.kflidCmObject_Class);
				if (clsid > 0 || hvo <= 0)
					return clsid;

				LoadBasicObjectInfo(hvo);
				return m_odde.get_IntProp(hvo, (int)CmObjectFields.kflidCmObject_Class);
			}
			catch (Exception e)
			{
				throw new Exception("Failed to get class of object " + hvo, e);
			}
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the owner of an object. (Load object from DB, if not in cache.)
		/// </summary>
		/// <param name="hvo">The object to get the class for.</param>
		/// <returns>The owner's ID.</returns>
		/// ------------------------------------------------------------------------------------
		public int GetOwnerOfObject(int hvo)
		{
			CheckDisposed();
			int hvoOwner = m_odde.get_ObjectProp(hvo, (int)CmObjectFields.kflidCmObject_Owner);
			if (hvoOwner > 0 || hvo <= 0 || ClassIsOwnerless(hvo))
				return hvoOwner;

			LoadBasicObjectInfo(hvo);
			return m_odde.get_ObjectProp(hvo, (int)CmObjectFields.kflidCmObject_Owner);
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the owner of an objec of the specified class. (Load object from DB, if not in cache.)
		/// </summary>
		/// <param name="hvo">The object to get the class for.</param>
		/// <param name="clsid">The class the owner should be in.</param>
		/// <returns>The owner's ID, or 0, if no such owner.</returns>
		/// ------------------------------------------------------------------------------------
		public int GetOwnerOfObjectOfClass(int hvo, int clsid)
		{
			CheckDisposed();
			int hvoOwner = 0;
			int hvoCurrentObj = hvo;
			while (hvoOwner == 0)
			{
				hvoOwner = GetOwnerOfObject(hvoCurrentObj);
				if (hvoOwner == 0
					|| m_odde.get_IntProp(hvoOwner, (int)CmObjectFields.kflidCmObject_Class) == clsid)
					break;
				hvoCurrentObj = hvoOwner;
				hvoOwner = 0;
			}
			return hvoOwner;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns true if the object belongs to a ownerless class (e.g. LgWritingSystem
		/// or LangProject)
		/// </summary>
		/// <param name="hvo">HVO of the object</param>
		/// <returns><c>true</c> if ownerless, otherwise false</returns>
		/// ------------------------------------------------------------------------------------
		public bool ClassIsOwnerless(int hvo)
		{
			CheckDisposed();
			Debug.Assert(IsValidObject(hvo), "This method won't work correctly if HVO " + hvo + " is invalid");
			Type type = CmObject.GetTypeFromFWClassID(this, GetClassOfObject(hvo));

			CmObject x = (CmObject)type.GetConstructor(Type.EmptyTypes).Invoke(null);
			Debug.Assert(x.GetType() != typeof(CmObject), "We shouldn't create a generic CmObject!");
			bool fRet = x.IsOwnerless;

			return fRet;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the owning flid of an object. (Load object from DB, if not in cache.)
		/// </summary>
		/// <param name="hvo">The object to get the owning flid for.</param>
		/// <returns>The owning flid.</returns>
		/// ------------------------------------------------------------------------------------
		public int GetOwningFlidOfObject(int hvo)
		{
			CheckDisposed();
			int hvoOwnFlid = m_odde.get_IntProp(hvo, (int)CmObjectFields.kflidCmObject_OwnFlid);
			if (hvoOwnFlid > 0 || hvo <= 0)
				return hvoOwnFlid;

			LoadBasicObjectInfo(hvo);
			return m_odde.get_IntProp(hvo, (int)CmObjectFields.kflidCmObject_OwnFlid);
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the object ID for the given property.
		/// </summary>
		/// <param name="hvoObj">Object that has the property.</param>
		/// <param name="flidProperty">flid for property.</param>
		/// <returns>Object ID.</returns>
		/// ------------------------------------------------------------------------------------
		public int GetObjProperty(int hvoObj, int flidProperty)
		{
			CheckDisposed();
			return m_odde.get_ObjectProp(hvoObj, flidProperty);
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the given atomic reference property to the given object ID.
		/// </summary>
		/// <param name="hvoObj">Referring object.</param>
		/// <param name="flidProperty">Property flid.</param>
		/// <param name="hvoValue">New atomic reference value.</param>
		/// ------------------------------------------------------------------------------------
		public void SetObjProperty(int hvoObj, int flidProperty, int hvoValue)
		{
			CheckDisposed();
			bool fNewValueIsNull = hvoValue == 0;
			//there are probably lots of things that can go wrong in the following call,
			// and you are very unlikely to get a helpful exception  back.
			// One I just ran into is caused when you are giving and hvo of an object of the class
			// which does not fit the signature of this reference property.check the debugger output window:
			//	in my case, it listed "Description: UPDATE statement conflicted with COLUMN FOREIGN KEY constraint '_FK_CmAnnotation_Source'.
			//	The conflict occurred in database 'TestLangProj', table 'CmAgent', column 'id'."
			m_odde.SetObjProp(hvoObj, flidProperty, hvoValue);
			PropChanged(null, PropChangeType.kpctNotifyAll, hvoObj, flidProperty,
				0, fNewValueIsNull?0:1, fNewValueIsNull?1:0);
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the integer value for the given property.
		/// </summary>
		/// <param name="hvoObjectID">Object ID that contains the integer.</param>
		/// <param name="flidProperty">Property flid of hvoObjectID</param>
		/// <returns>The integer stored in the property.</returns>
		/// ------------------------------------------------------------------------------------
		public int GetIntProperty(int hvoObjectID, int flidProperty)
		{
			CheckDisposed();
			return m_odde.get_IntProp(hvoObjectID, flidProperty);
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the integer value for the given property.
		/// </summary>
		/// <param name="hvoOwner">Object ID that owns the integer value.</param>
		/// <param name="flidProperty">Property flid.</param>
		/// <param name="iValue">New integer value.</param>
		/// ------------------------------------------------------------------------------------
		public void SetIntProperty(int hvoOwner, int flidProperty, int iValue)
		{
			CheckDisposed();
			m_odde.SetInt(hvoOwner, flidProperty, iValue);
			PropChanged(null, PropChangeType.kpctNotifyAll, hvoOwner, flidProperty,
				0, 1, 1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="hvoObjectID"></param>
		/// <param name="flidProperty"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public byte[] GetBinaryProperty(int hvoObjectID, int flidProperty)
		{
			CheckDisposed();
			int cb;
			using (ArrayPtr rgb = MarshalEx.ArrayToNative(8000, typeof(byte)))
			{
				Debug.Assert(m_odde != null);
				m_odde.BinaryPropRgb(hvoObjectID, flidProperty, rgb, 8000, out cb);
				Debug.Assert(rgb != null);
				byte[] vbRet = (byte[])MarshalEx.NativeToArray(rgb, cb, typeof(byte));
				return vbRet;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="hvoOwner"></param>
		/// <param name="flidProperty"></param>
		/// <param name="vbInValue"></param>
		/// ------------------------------------------------------------------------------------
		public void SetBinaryProperty(int hvoOwner, int flidProperty, byte[] vbInValue)
		{
			CheckDisposed();
			m_odde.SetBinary(hvoOwner, flidProperty, vbInValue,
				vbInValue.Length);
			PropChanged(null, PropChangeType.kpctNotifyAll, hvoOwner, flidProperty,
				0, 1, 1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="hvoOwner"></param>
		/// <param name="flidProperty"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool GetBoolProperty(int hvoOwner, int flidProperty)
		{
			CheckDisposed();
			return m_odde.get_BooleanProp(hvoOwner, flidProperty);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="hvoOwner"></param>
		/// <param name="flidProperty"></param>
		/// <param name="fValue"></param>
		/// ------------------------------------------------------------------------------------
		public void SetBoolProperty(int hvoOwner, int flidProperty, bool fValue)
		{
			CheckDisposed();
			m_odde.SetBoolean(hvoOwner, flidProperty, fValue);
			PropChanged(null, PropChangeType.kpctNotifyAll, hvoOwner, flidProperty,
				0, 1, 1);
		}

		/**************
				public TsStringAccessor GetTsStringProperty(int hvoOwner, int flidProperty)
				{
					return new TsStringAccessor(m_odde.get_StringProp(hvoOwner, flidProperty));
				}

				public void SetTsStringProperty(int hvoOwner, int flidProperty, TsStringAccessor tssValue)
				{
					m_odde.SetString(hvoOwner, flidProperty, tssValue.UnderlyingTsString);
					// TODO: Call PropChanged in m_odde.
							}
		**************/

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="hvoOwner"></param>
		/// <param name="flidProperty"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public ITsString GetTsStringProperty(int hvoOwner, int flidProperty)
		{
			CheckDisposed();
			return m_odde.get_StringProp(hvoOwner, flidProperty);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="hvoOwner"></param>
		/// <param name="flidProperty"></param>
		/// <param name="tssValue"></param>
		/// ------------------------------------------------------------------------------------
		public void SetTsStringProperty(int hvoOwner, int flidProperty, ITsString tssValue)
		{
			CheckDisposed();
			tssValue = CheckAndFixStringLength(flidProperty, tssValue);
			ITsString tssOld = GetTsStringProperty(hvoOwner, flidProperty);
			// A valid PropChanged for a string property is supposed to indicate the range of
			// changed characters by means of ivMin, cvIns, and cvDel, which in this case apply
			// to characters.
			int ivMin, cvIns, cvDel;
			StringUtils.GetDiffsInTsStrings(tssOld, tssValue, out ivMin, out cvIns, out cvDel);
			m_odde.SetString(hvoOwner, flidProperty, tssValue);
			PropChanged(null, PropChangeType.kpctNotifyAll, hvoOwner, flidProperty,
				ivMin, cvIns, cvDel);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="hvoOwner"></param>
		/// <param name="flidProperty"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public object GetUnknown(int hvoOwner, int flidProperty)
		{
			CheckDisposed();
			return m_odde.get_UnknownProp(hvoOwner, flidProperty);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="hvoOwner"></param>
		/// <param name="flidProperty"></param>
		/// <param name="unknown"></param>
		/// ------------------------------------------------------------------------------------
		public void SetUnknown(int hvoOwner, int flidProperty, object unknown)
		{
			CheckDisposed();
			m_odde.SetUnknown(hvoOwner, flidProperty, unknown);
			PropChanged(null, PropChangeType.kpctNotifyAll, hvoOwner, flidProperty,
				0, 1, 1);
		}

		/**************
				// Are these just not something anyone needed,
				// so that the odde doesn't provide it?
				public MultiUnicode GetMultiUnicodeProperty(int hvoOwner, int flidProperty)
				{
					throw new Exception("GetMultiUnicodeProperty not implemented");
				}

				public void SetMultiUnicodeProperty(int hvoOwner, int flidProperty, MultiUnicode msValue)
				{
					// TODO: Call PropChanged in m_odde.
					throw new Exception("SetMultiUnicodeProperty not implemented");
				}
		**************/
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="hvoOwner"></param>
		/// <param name="flidProperty"></param>
		/// <param name="ws"></param>
		///<param name="sViewName">the SQL View that will give us this string</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public string GetMultiUnicodeAlt(int hvoOwner, int flidProperty, int ws, string sViewName)
		{
			CheckDisposed();
			//get_MultiStringAlt is now smart and will load this if needed.
			ITsString tss = m_odde.get_MultiStringAlt(hvoOwner, flidProperty, ws);
			//			string retVal = tss.Text;
			//			if ((retVal != null) && retVal.Length > 0)
			//				return retVal;
			//
			//			// Wasn't in cache, so try loading it.
			//			LoadMultiUnicodeAlt(hvoOwner, flidProperty, ws, sViewName);

			//			tss = m_odde.get_MultiStringAlt(hvoOwner, flidProperty, ws);
			return tss.Text;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="hvoOwner"></param>
		/// <param name="flidProperty"></param>
		/// <param name="ws"></param>
		/// <param name="sValue"></param>
		/// ------------------------------------------------------------------------------------
		public void SetMultiUnicodeAlt(int hvoOwner, int flidProperty, int ws, string sValue)
		{
			CheckDisposed();
			sValue = CheckAndFixUnicodeLength(flidProperty, sValue);
			//REVIEW: ugly (and slow)
			// (EberhardB): changed from ITsIncStrBldr to ITsStrFactory, but probably still
			// ugly (and slow).
			ITsStrFactory tsf = TsStrFactoryClass.Create();
			SetMultiStringAlt(hvoOwner, flidProperty, ws, tsf.MakeString(sValue, ws));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the value of a multilingual string.
		/// Note: this previously took an sViewName argument, used in preloading it.
		/// However, preloading is now automatic and built into the cache.
		/// If it is necessary to reload because of stale data, call LoadMultiStringAlt
		/// directly.
		/// </summary>
		/// <param name="hvoOwner"></param>
		/// <param name="flidProperty"></param>
		/// <param name="ws"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public virtual ITsString GetMultiStringAlt(int hvoOwner, int flidProperty, int ws)
		{
			CheckDisposed();
			return m_odde.get_MultiStringAlt(hvoOwner, flidProperty, ws);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the multi string alt.
		/// </summary>
		/// <param name="hvoOwner">The hvo owner.</param>
		/// <param name="flidProperty">The flid property.</param>
		/// <param name="ws">The ws.</param>
		/// <param name="tssValue">The string value.</param>
		/// ------------------------------------------------------------------------------------
		public void SetMultiStringAlt(int hvoOwner, int flidProperty, int ws, ITsString tssValue)
		{
			CheckDisposed();
			tssValue = CheckAndFixStringLength(flidProperty, tssValue);
			m_odde.SetMultiStringAlt(hvoOwner, flidProperty, ws, tssValue);
			PropChanged(null, PropChangeType.kpctNotifyAll, hvoOwner, flidProperty,
				ws, 1, 1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks the length of the string and shortens it if it is over the maximum length.
		/// </summary>
		/// <param name="flidProperty">The flid property.</param>
		/// <param name="tssValue">The string value.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private ITsString CheckAndFixStringLength(int flidProperty, ITsString tssValue)
		{
			int cchMax = MaxFieldLength(flidProperty);
			if (cchMax < Int32.MaxValue && tssValue != null && tssValue.Length > cchMax)
			{
				string sMsg = String.Format(Strings.ksTruncatedToXXXChars, cchMax);
				System.Windows.Forms.MessageBox.Show(sMsg, Strings.ksWarning,
					System.Windows.Forms.MessageBoxButtons.OK,
					System.Windows.Forms.MessageBoxIcon.Warning);
				ITsStrBldr bldr = tssValue.GetBldr();
				bldr.ReplaceTsString(cchMax, tssValue.Length, null);
				tssValue = bldr.GetString();
			}
			return tssValue;
		}


		/**************
				public TsMultiString GetMultiStringProperty(int hvoOwner, int flidProperty)
						{
							// xREVIEW JohnH(RandyR): In line 170 of VwBaseDataAccess.cpp this chokes on an Assert(false).
							// It looks like it hasn't been implemented yet in FW.

							//: bring into cache if necessary
							ITsMultiString tms= m_odde.get_MultiStringProp(hvoOwner, flidProperty);
							return new TsMultiString(this, tms);

							throw new Exception("GetMultiStringProperty not implemented because  VwBaseDataAccess.cpp does not implement it.");
						}

						// is this just not something anyone needed so that the odde doesn't provide it?
						public void SetMultiStringProperty(int hvoOwner, int flidProperty, TsMultiString tmsValue)
						{
							// TODO: Call PropChanged in m_odde.
							throw new Exception("SetMultiStringProperty not implemented");
						}
		**************/


		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="hvoOwner"></param>
		/// <param name="flidProperty"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public string GetUnicodeProperty(int hvoOwner, int flidProperty)
		{
			CheckDisposed();
			return m_odde.get_UnicodeProp(hvoOwner, flidProperty);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="hvoOwner"></param>
		/// <param name="flidProperty"></param>
		/// <param name="sValue"></param>
		/// ------------------------------------------------------------------------------------
		public void SetUnicodeProperty(int hvoOwner, int flidProperty, string sValue)
		{
			CheckDisposed();
			sValue = CheckAndFixUnicodeLength(flidProperty, sValue);
			m_odde.set_UnicodeProp(hvoOwner, flidProperty, sValue);
			PropChanged(null, PropChangeType.kpctNotifyAll, hvoOwner, flidProperty,
				0, 1, 1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks the length of the unicode string and shortens it if it is too long.
		/// </summary>
		/// <param name="flidProperty">The flid property.</param>
		/// <param name="sValue">The string value.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private string CheckAndFixUnicodeLength(int flidProperty, string sValue)
		{
			int cchMax = MaxFieldLength(flidProperty);
			if (cchMax < Int32.MaxValue && sValue != null && sValue.Length > cchMax)
			{
				string sMsg = String.Format(Strings.ksTruncatedToXXXChars, cchMax);
				System.Windows.Forms.MessageBox.Show(sMsg, Strings.ksWarning,
					System.Windows.Forms.MessageBoxButtons.OK,
					System.Windows.Forms.MessageBoxIcon.Warning);
				sValue = sValue.Remove(cchMax);

			}
			return sValue;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a list of all back translation writing systems used for Scripture.
		/// </summary>
		/// <returns>array list of back translation writing system HVOs</returns>
		/// ------------------------------------------------------------------------------------
		public virtual List<int> GetUsedScriptureBackTransWs()
		{
			CheckDisposed();
			// TODO: Appears that this gets all translations instead of just back translations.
			//  flid 29001 is for all translations.
			// Need to change the name to GetUsedScriptureTransWs() & summary, or change the code.
			return DbOps.ReadIntsFromCommand(this,
				"select mb.ws from MultiBigStr$ mb join CmTranslation_ trans on mb.obj = trans.id join StTxtPara_ para on " +
				"trans.Owner$ = para.id join StText_ txt on para.Owner$ = txt.id where mb.flid=29001 and " +
				"txt.OwnFlid$ in (3005001,3005002,3002004,3002010) group by mb.ws",
				null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a list of all translation writing systems used for the specified paragraph
		/// in Scripture.
		/// </summary>
		/// <param name="hvoPara">The hvo of the specified paragraph</param>
		/// <returns>array list of translation writing system HVOs</returns>
		/// ------------------------------------------------------------------------------------
		public virtual List<int> GetUsedScriptureTransWsForPara(int hvoPara)
		{
			//TODO TE-5047: This may not be the best way to get the desired info because the InMemoryCache
			// has to override this method and fudge the results.
			// Perhaps implement in FDO the ITsMultiString methods get_StringCount and GetStringFromIndex.
			// Or perhaps ISilDataAccess could implement a GetMultiStringWs()
			//  which would return an array of the writing systems used in this MultiString.
			// Then eliminate or change this method and its overloads.
			return DbOps.ReadIntsFromCommand(this,
				"select mb.ws from MultiBigStr$ mb join CmTranslation_ trans on mb.obj = trans.id join StTxtPara_ para on " +
				"trans.Owner$ = " + hvoPara.ToString() + " join StText_ txt on para.Owner$ = txt.id where mb.flid=29001 and " +
				"txt.OwnFlid$ in (3005001,3005002,3002004,3002010) group by mb.ws",
				null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the date and time from the database.
		/// </summary>
		/// <param name="hvoOwner">Id of the owning object.</param>
		/// <param name="flidProperty">Flid that has the date and time.</param>
		/// <returns>The date and time from the database.</returns>
		/// ------------------------------------------------------------------------------------
		public virtual DateTime GetTimeProperty(int hvoOwner, int flidProperty)
		{
			CheckDisposed();
			long tim = MainCacheAccessor.get_TimeProp(hvoOwner, flidProperty);
			if (tim != 0)
				return DateTime.FromFileTime(tim * 10000);
			else
				return new DateTime(1, 1, 1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the date and time on a database object.
		/// </summary>
		/// <param name="hvoOwner">Id of the owning object.</param>
		/// <param name="flidProperty">Flid that gets updated.</param>
		/// <param name="dtValue">New date and time value.</param>
		/// ------------------------------------------------------------------------------------
		public virtual void SetTimeProperty(int hvoOwner, int flidProperty, DateTime dtValue)
		{
			CheckDisposed();
			// We don't want to create a new database connection here as this will cause
			// a database lock on typing. (It's also not undoable.)
			MainCacheAccessor.SetTime(hvoOwner, flidProperty, dtValue.ToFileTime() / 10000);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="hvoOwner"></param>
		/// <param name="flidProperty"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public Guid GetGuidProperty(int hvoOwner, int flidProperty)
		{
			CheckDisposed();
			return m_odde.get_GuidProp(hvoOwner, flidProperty);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="hvoOwner"></param>
		/// <param name="flidProperty"></param>
		/// <param name="guidValue"></param>
		/// ------------------------------------------------------------------------------------
		public void SetGuidProperty(int hvoOwner, int flidProperty, Guid guidValue)
		{
			CheckDisposed();
			m_odde.SetGuid(hvoOwner, flidProperty, guidValue);
			PropChanged(null, PropChangeType.kpctNotifyAll, hvoOwner,
				flidProperty, 0, 1, 1);
		}

		//TODO What's up with generalize dates?  Is this just an integer?
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="hvoOwner"></param>
		/// <param name="flidProperty"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public int GetGenDateProperty(int hvoOwner, int flidProperty)
		{
			CheckDisposed();
			throw new Exception("GetGenDateProperty not implemented");
		}

		//TODO What's up with generalize dates?  Is this just an integer?
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="hvoOwner"></param>
		/// <param name="flidProperty"></param>
		/// <param name="gdValue"></param>
		/// ------------------------------------------------------------------------------------
		public void SetGenDateProperty(int hvoOwner, int flidProperty, int gdValue)
		{
			CheckDisposed();
			throw new Exception("SetGenDateProperty not implemented");
			/* TODO: Enable PropChanged call, when it gets implemented.
			PropChanged(null, PropChangeType.kpctNotifyAll, hvoOwner,
				flidProperty, 0, 1, 1);
			*/
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// get an array of hvos
		/// </summary>
		/// <remarks> The assumeCached is needed because the cache currently cannot tell
		/// the difference between an empty vector and a vector which was never loaded.
		/// for vector set are very often empty (there are a lot of these in linguistics)
		/// discipline means that each empty vector means a separate cache hit, just to retrieve
		/// emptiness again. Perhaps good Zen, but bad performance.</remarks>
		/// <param name="hvoOwner"></param>
		/// <param name="flidProperty"></param>
		/// <param name="assumeCached">should be true if you know that the query was done on
		/// the vector</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public int[] GetVectorProperty(int hvoOwner, int flidProperty, bool assumeCached)
		{
			return GetVectorProperty(hvoOwner, flidProperty, assumeCached, null);
		}

		/// <summary>
		/// Get an array of hvos, stepping an (optional) progress bar for each item in the
		/// vector.  In a perfect world, progress reporting would not be needed, but...
		/// (See LT-8665.)
		/// </summary>
		/// <param name="hvoOwner"></param>
		/// <param name="flidProperty"></param>
		/// <param name="assumeCached"></param>
		/// <param name="prog"></param>
		/// <returns></returns>
		public int[] GetVectorProperty(int hvoOwner, int flidProperty, bool assumeCached,
			IAdvInd4 prog)
		{
			CheckDisposed();
			Debug.Assert(hvoOwner != 0);
			int sz;
			if (assumeCached)
				sz = m_odde.get_VecSizeAssumeCached(hvoOwner, flidProperty);
			else
				sz = m_odde.get_VecSize(hvoOwner, flidProperty);
			int[] vec = new int[sz];
			if (prog != null && sz > 0)
			{
				prog.SetRange(0, sz);
				prog.Position = 0;
			}
			for (int i = 0; i < sz; i++)
			{
				vec[i] = m_odde.get_VecItem(hvoOwner, flidProperty, i);
				if (prog != null)
					prog.Step(1);
			}
			return vec;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="hvoOwner"></param>
		/// <param name="flidProperty"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public int GetVectorSize(int hvoOwner, int flidProperty)
		{
			CheckDisposed();
			return m_odde.get_VecSize(hvoOwner, flidProperty);
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the ID of the object at the given index in a vector.
		/// </summary>
		/// <param name="hvoOwner">ID of the onwing object.</param>
		/// <param name="flidProperty">Owning flid.</param>
		/// <param name="index">Index of object to get.</param>
		/// <returns>The ID of the requested object.</returns>
		/// ------------------------------------------------------------------------------------
		public int GetVectorItem(int hvoOwner, int flidProperty, int index)
		{
			CheckDisposed();
			return m_odde.get_VecItem(hvoOwner, flidProperty, index);
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Replace all or part of the reference property's values with the new values.
		/// [Note: This can be used to simply reorder values in a sequence property.
		/// It can also be used for atomic and collection properties, in which case
		/// some parameters are ignored.]
		/// </summary>
		/// <param name="hvoObj">ID of the referring object.</param>
		/// <param name="flid">Reference flid of the referring property.</param>
		/// <param name="ihvoMin">The index to start the replacement.</param>
		/// <param name="ihvoLim">The index to end the replacement.</param>
		/// <param name="ahvo">The replacement values.</param>
		/// ------------------------------------------------------------------------------------
		public void ReplaceReferenceProperty(int hvoObj, int flid, int ihvoMin, int ihvoLim,
			ref int[] ahvo)
		{
			CheckDisposed();
			m_odde.Replace(hvoObj, flid, ihvoMin, ihvoLim, ahvo, ahvo.Length);
			PropChanged(null, PropChangeType.kpctNotifyAll, hvoObj, flid,
				ihvoMin, ahvo.Length, ihvoLim - ihvoMin);
		}

		#endregion	// Put data into cache & retrieve data from cache methods

		#region Undo-Redo processing methods
		// [Note: Some of this type of code is in the 'Properties' region.]

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Have FW cache start an undoable action.
		/// The two input strings are of the form:
		/// "Undo Insert Sense" and "Redo Insert Sense"
		/// where the action (e.g., Insert/Delete/Edit, etc.) and the object are the same.
		/// </summary>
		///<param name="sUndo">The 'Undo' string.</param>
		///<param name="sRedo">The 'Redo' string.</param>
		/// ------------------------------------------------------------------------------------
		public virtual void BeginUndoTask(string sUndo, string sRedo)
		{
			CheckDisposed();
			m_odde.BeginUndoTask(sUndo, sRedo);
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Have FW cache start an undoable action.
		/// The two input strings are of the form:
		/// "Undo Insert Sense" and "Redo Insert Sense"
		/// where the action (e.g., Insert/Delete/Edit, etc.) and the object are the same.
		/// </summary>
		/// <param name="iUndo">String resource ID for the 'Undo' string.</param>
		/// <param name="iRedo">String resource ID for the 'Redo' string.</param>
		/// ------------------------------------------------------------------------------------
		public void BeginUndoTask(int iUndo, int iRedo)
		{
			CheckDisposed();
			// ENHANCE: Add one that use the resource for strings.
			throw new Exception("BeginUndoTask (using resource string IDs) not implemented.");
		}

		/// <summary>
		/// Have the OleDbEncap continue an undo task (that is, add an after-thought like
		/// a change of modify time to an undo task we thought was done).
		/// </summary>
		public void ContinueUndoTask()
		{
			CheckDisposed();
			m_odde.ContinueUndoTask();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Have Fw cache end an undoable action.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void EndUndoTask()
		{
			CheckDisposed();
			m_odde.EndUndoTask();
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Version of Redo where we don't care about return result (mainly for tests).
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool Undo()
		{
			CheckDisposed();
			UndoResult ures;
			return Undo(out ures);
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Undo the most recent action, if there is one.
		/// </summary>
		///<returns>true, if an action was undone, otherwise false.</returns>
		/// ------------------------------------------------------------------------------------
		public bool Undo(out UndoResult ures)
		{
			CheckDisposed();
			if (!CanUndo)
			{
				ures = UndoResult.kuresSuccess; // no refresh needed, anyway.
				return false;
			}

			Debug.Assert(ActionHandlerAccessor != null);

			try
			{
				m_isBusy = true;
				using (new IgnorePropChanged(this, PropChangedHandling.SuppressChangeWatcher))
				{
					ures = ActionHandlerAccessor.Undo();
				}
				if (ures == UndoResult.kuresError || ures == UndoResult.kuresFailed)
					ClearAllData(); // We don't know what state we should be in now...
			}
			finally
			{
				m_isBusy = false;
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Version of Redo where we don't care about return result (mainly for tests).
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool Redo()
		{
			CheckDisposed();
			UndoResult ures;
			return Redo(out ures);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Redo the most recent action, if there is one.
		/// </summary>
		///<returns>true, if an action was redone, otherwise false.</returns>
		/// ------------------------------------------------------------------------------------
		public bool Redo(out UndoResult ures)
		{
			CheckDisposed();
			if (!CanRedo)
			{
				ures = UndoResult.kuresSuccess; // no refresh needed, anyway.
				return false;
			}

			Debug.Assert(ActionHandlerAccessor != null);
			try
			{
				m_isBusy = true;
				using (new IgnorePropChanged(this, PropChangedHandling.SuppressChangeWatcher))
				{
					ures = ActionHandlerAccessor.Redo();
				}
				if (ures == UndoResult.kuresError || ures == UndoResult.kuresFailed)
					ClearAllData(); // We don't know what state we should be in now...
			}
			finally
			{
				m_isBusy = false;
			}
			return true;
		}

		/// <summary>
		/// mark a Com object as needing to be released when
		/// we dispose of the cache.
		/// this is useful for tests that depend upon Undo
		/// for restoring data. It may otherwise be difficult
		/// for knowing when it's okay to release the object.
		/// </summary>
		Set<object> m_comObjects = new Set<object>();
		internal void TrackComObject(object obj)
		{
			if (obj != null && Marshal.IsComObject(obj))
				m_comObjects.Add(obj);
		}

		/// <summary>
		/// Marshal release the given Com object and remove
		/// it from the ones we're tracking.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="fFinalRelease"></param>
		internal void ReleaseComObject(object obj, bool fFinalRelease)
		{
			if (obj != null && Marshal.IsComObject(obj))
			{
				if (fFinalRelease)
					Marshal.FinalReleaseComObject(obj);
				else
					Marshal.ReleaseComObject(obj);
			}
			m_comObjects.Remove(obj);
		}

		/// <summary>
		/// release all com objects marked as for needing to be released.
		/// this can avoid hanging in tests (cf. LT-7119).
		/// </summary>
		/// <param name="fFinalRelease"></param>
		private void ReleaseAllTrackedComObjects(bool fFinalRelease)
		{
			foreach (object obj in m_comObjects.ToArray())
			{
				ReleaseComObject(obj, fFinalRelease);
			}
		}

		// ENHANCE: Add access to: "BreakUndoTask", "ContinueUndoTask",
		// and "EndOuterUndoTask", if there is ever a reason to do so.
		#endregion	// Undo-Redo processing methods

		#region Change Notification methods

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Notify those interested of changes to some property.
		/// </summary>
		/// <param name="nchng">
		/// Interface to an object that no longer wants
		/// to be notified of a change to some property.
		/// </param>
		/// <param name="pct">Type of property change notification.</param>
		/// <param name="hvo">ID of the object that has changed.</param>
		/// <param name="tag">The property (flid) that has changed.</param>
		/// <param name="ivMin">For vectors, the starting index where the change occurred.
		/// For MultiStrings, the writing system where the change occurred.</param>
		/// <param name="cvIns">For vectors, the number of items inserted.
		/// For atomic objects, 1 if an item was added.
		/// Otherwise (including basic properties), 0.</param>
		/// <param name="cvDel">For vectors, the number of items deleted.
		/// For atomic objects, 1 if an item was deleted.
		/// Otherwise (including basic properties), 0.</param>
		/// ------------------------------------------------------------------------------------
		public void PropChanged(IVwNotifyChange nchng, PropChangeType pct, int hvo,
			int tag, int ivMin, int cvIns, int cvDel)
		{
			CheckDisposed();
			if ((m_PropChangedHandling & PropChangedHandling.SuppressView) ==
				PropChangedHandling.SuppressNone)
			{
				m_odde.PropChanged(nchng, (int)pct, hvo, tag, ivMin, cvIns, cvDel);
			}
		}

		#endregion	// Change Notification methods
		/// <summary>
		///
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="ivMin"></param>
		/// <param name="cvIns"></param>
		/// <param name="cvDel"></param>
		public delegate void PropChangedDelegate(int hvo, int tag, int ivMin, int cvIns, int cvDel);
		/// <summary>
		///
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="ivMin"></param>
		/// <param name="cvIns"></param>
		/// <param name="cvDel"></param>
		public void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			PropChanged(null, PropChangeType.kpctNotifyAll, hvo, tag, ivMin, cvIns, cvDel);
		}

		#region Meta data methods

		/// ------------------------------------------------------------------------------------
		/// <summary>Checks a field id to see if it is a collection or sequence property.</summary>
		/// <param name="flid">Field ID to be checked.</param>
		/// <returns>
		/// true, if flid is a collection or sequence property, otherwise false.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public bool IsVectorProperty(int flid)
		{
			CheckDisposed();
			FieldType iType = GetFieldType(flid);
			return ((iType == FieldType.kcptOwningCollection)
				|| (iType == FieldType.kcptOwningSequence)
				|| (iType == FieldType.kcptReferenceCollection)
				|| (iType == FieldType.kcptReferenceSequence));
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>Checks a field ID to see if it is an owning property.</summary>
		/// <param name="flid">Field ID to be checked.</param>
		/// <returns>
		/// true, if flid is an owning property, otherwise false.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public bool IsOwningProperty(int flid)
		{
			CheckDisposed();
			FieldType iType = GetFieldType(flid);
			return ((iType == FieldType.kcptOwningCollection)
				|| (iType == FieldType.kcptOwningSequence)
				|| (iType == FieldType.kcptOwningAtom));
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>Checks a field ID to see if it is a reference property.</summary>
		/// <param name="flid">Field ID to be checked.</param>
		/// <returns>
		/// true, if flid is a reference property, otherwise false.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public bool IsReferenceProperty(int flid)
		{
			CheckDisposed();
			FieldType iType = GetFieldType(flid);
			return ((iType == FieldType.kcptReferenceAtom)
				|| (iType == FieldType.kcptReferenceCollection)
				|| (iType == FieldType.kcptReferenceSequence));
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the field type.
		/// </summary>
		/// <param name="flid">Field ID to get type for.</param>
		/// <returns>The field type</returns>
		/// ------------------------------------------------------------------------------------
		public FieldType GetFieldType(int flid)
		{
			CheckDisposed();
			return (FieldType)m_mdc.GetFieldType((uint)flid);
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the name of a class.
		/// </summary>
		/// <param name="clsid">Class ID.</param>
		/// <returns>A string that contains the name of the class.</returns>
		/// ------------------------------------------------------------------------------------
		public virtual string GetClassName(uint clsid)
		{
			CheckDisposed();
			if (clsid == 0)
				return "CmObject";
			return m_mdc.GetClassName(clsid);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the Flid of the hvo object given the field name.
		/// </summary>
		/// <param name="hvo">The hvo.</param>
		/// <param name="stClassName">class name. if null or empty, we compute the flid based
		/// upon the classId of hvo.</param>
		/// <param name="stFieldName">field name</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public int GetFlid(int hvo, string stClassName, string stFieldName)
		{
			CheckDisposed();
			int flid = 0;
			if (stFieldName == "OwningFlid")
			{
				flid = this.GetOwningFlidOfObject(hvo);
			}
			else if (stClassName == null || stClassName.Length == 0)
			{
				uint classId = (uint) this.MainCacheAccessor.get_IntProp(hvo,
					(int)CmObjectFields.kflidCmObject_Class);
				flid = (int)m_mdc.GetFieldId2(classId, stFieldName, true);
			}
			else
			{
				flid = (int)m_mdc.GetFieldId(stClassName, stFieldName, true);
			}
			if (flid == 0)
			{
				// try a general purpose field that doesn't get treated as a 'base' class.
				flid = (int)m_mdc.GetFieldId("CmObject", stFieldName, false);
			}
			return flid;
		}

		/// <summary>
		/// Try to get dependencies for the hvo/flid combination (for virtual fields).
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="flid"></param>
		/// <param name="objIds"></param>
		/// <param name="flids"></param>
		/// <returns></returns>
		public bool TryGetDependencies(int hvo, int flid, out List<int> objIds, out List<int> flids)
		{
			objIds = null;
			flids = null;
			IVwVirtualHandler vh;
			if (TryGetVirtualHandler(flid, out vh) && vh is BaseVirtualHandler)
			{
				BaseVirtualHandler bvh = vh as BaseVirtualHandler;
				objIds = new List<int>(bvh.DependencyPaths.Count);
				flids = new List<int>(bvh.DependencyPaths.Count);
				foreach (List<int> flidpath in bvh.DependencyPaths)
				{
					int[] rgObjId = GetHvosForFlidPath(hvo, flidpath);
					for (int i = 0; i < rgObjId.Length; ++i)
					{
						if (this.IsValidObject(rgObjId[i]))
						{
							objIds.Add(rgObjId[i]);
							// the last flid in the path is the dependency.
							flids.Add(flidpath[flidpath.Count - 1]);
						}
					}
				}
			}

			return objIds != null && objIds.Count > 0;
		}

		/// <summary>
		/// Determine the flids that the given 'flid' depends upon.
		/// </summary>
		/// <param name="flid">the main virtual flid for determining dependencies</param>
		/// <param name="flids">the flids that 'flid' depends upon.</param>
		/// <returns></returns>
		public bool TryGetDependencies(int flid, out List<int> flids)
		{
			flids = null;
			Set<int> uniqueflids = null;
			IVwVirtualHandler vh;
			if (TryGetVirtualHandler(flid, out vh) && vh is BaseVirtualHandler)
			{
				BaseVirtualHandler bvh = vh as BaseVirtualHandler;
				uniqueflids = new Set<int>();
				foreach (List<int> flidpath in bvh.DependencyPaths)
				{
					// the last flid in the path is the dependency.
					uniqueflids.Add(flidpath[flidpath.Count - 1]);
				}
				flids = new List<int>(uniqueflids);
			}

			return flids != null && flids.Count > 0;
		}

		/// <summary>
		/// Return the hvo for the flid path of a given root object
		/// </summary>
		/// <param name="hvoRoot"></param>
		/// <param name="flidPath"></param>
		/// <returns></returns>
		private int[] GetHvosForFlidPath(int hvoRoot, List<int> flidPath)
		{
			//int srcHvo = hvoRoot;
			//int dstHvo = 0;
			int cLevel = 0;
			List<int> rgSrcHvo = new List<int>();
			rgSrcHvo.Add(hvoRoot);
			foreach (int flid in flidPath)
			{
				cLevel++;
				if (cLevel < flidPath.Count)
				{
					if (this.IsVectorProperty(flid))
					{
						//throw new ArgumentException("GetHvoForFlidPath: Vector flids not yet supported.");
						List<int> rgNewSrc = new List<int>();
						for (int i = 0; i < rgSrcHvo.Count; ++i)
						{
							int hvo = rgSrcHvo[i];
							if (hvo != 0)
							{
								int[] rgHvos = this.GetVectorProperty(rgSrcHvo[i], flid, false);
								if (rgHvos != null && rgHvos.Length > 0)
									rgNewSrc.AddRange(rgHvos);
							}
						}
						if (rgNewSrc.Count > 0)
							rgSrcHvo = rgNewSrc;
						else
							return new int[1] { 0 };	// no objects found!
					}
					else
					{
						// make the dstHvo the source of the next flid
						for (int i = 0; i < rgSrcHvo.Count; ++i)
						{
							int hvo = rgSrcHvo[i];
							if (hvo != 0)
								hvo = this.GetObjProperty(hvo, flid);
							rgSrcHvo[i] = hvo;
						}
					}
				}
			}
			return rgSrcHvo.ToArray();
		}

		/// <summary>
		/// try to get a virtual handler from the given flid.
		/// </summary>
		/// <param name="flid"></param>
		/// <param name="vh"></param>
		/// <returns></returns>
		public bool TryGetVirtualHandler(int flid, out IVwVirtualHandler vh)
		{
			vh = null;
			if (flid != 0 && this.MetaDataCacheAccessor.get_IsVirtual((uint)flid))
			{
				vh = this.VwCacheDaAccessor.GetVirtualHandlerId(flid);
			}
			return vh != null;
		}


		/// <summary>
		/// determine whether flidTest is a virtual handler that matches the target class (stores the same type of items).
		/// </summary>
		/// <param name="targetClassId">the destination class that we want to try to match against flidTest's destination class.</param>
		/// <param name="flidTest">the tag of a virtual handler we want to test for compatibility.</param>
		/// <param name="bvh">compatible virtual handler</param>
		/// <returns>true if we could get a compatible handler, false otherwise.</returns>
		public bool TryMatchCompatibleHandler(int targetClassId, int flidTest, out BaseFDOPropertyVirtualHandler bvh)
		{
			bvh = null;
			IVwVirtualHandler vh = null;
			if (targetClassId != 0 &&
				this.TryGetVirtualHandler(flidTest, out vh) && vh is IDummyRequestConversion && vh is BaseFDOPropertyVirtualHandler)
			{
				// determine if the target object classes are compatible.
				int testDstCls = (vh as BaseFDOPropertyVirtualHandler).DestinationClassId;
				if (testDstCls == targetClassId)
				{
					bvh = (vh as BaseFDOPropertyVirtualHandler);
				}
			}
			return bvh != null;
		}

		/// <summary>
		/// Try looking up an item in the list and replacing it if we find it.
		/// </summary>
		/// <param name="hvoOwner"></param>
		/// <param name="owningFlid"></param>
		/// <param name="hvoToReplace"></param>
		/// <param name="hvoNew">hvo of the new item, or 0 to delete the hvoToReplace</param>
		/// <param name="fMakeActionUndoable">if true, add the replacement to the undo stack.</param>
		/// <param name="ihvoToReplace">the index in the vector where the item was replaced.</param>
		/// <param name="fDoNotify">if true, issues PropChanged</param>
		public bool TryCacheReplaceOneItemInVector(int hvoOwner, int owningFlid, int hvoToReplace, int hvoNew, bool fDoNotify, bool fMakeActionUndoable, out int ihvoToReplace)
		{
			List<int> ids = new List<int>(this.GetVectorProperty(hvoOwner, owningFlid, true));
			ihvoToReplace = ids.IndexOf(hvoToReplace);
			if (ihvoToReplace >= 0)
			{
				int[] newItems = hvoNew != 0 ? new int[] { hvoNew } : new int[0];
				CacheReplaceOneUndoAction replaceAction = new CacheReplaceOneUndoAction(this,
					hvoOwner, owningFlid, ihvoToReplace, ihvoToReplace + 1, newItems);
				replaceAction.DoIt(fDoNotify);
				if (fMakeActionUndoable && this.ActionHandlerAccessor != null)
				{
					this.ActionHandlerAccessor.AddAction(replaceAction);
				}
			}
			return ihvoToReplace >= 0;
		}

		/// <summary>
		/// Returns the string value for the given flid. If the flid does not refer to a supported
		/// string type, fTypeFound is set to false.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="flid"></param>
		/// <param name="frag">XmlNode containing the ws id for Multi string types.</param>
		/// <param name="fTypeFound">true if the flid refers to a supported string type, false otherwise.</param>
		/// <returns>if fTypeFound is true we return the string value for the flid. Otherwise we return </returns>
		public string GetText(int hvo, int flid, XmlNode frag, out bool fTypeFound)
		{
			CheckDisposed();
			int itype = m_mdc.GetFieldType((uint)flid);
			fTypeFound = true;

			switch(itype & 0x1f) // strip virtual bit
			{
				case (int)CellarModuleDefns.kcptUnicode:
				case (int)CellarModuleDefns.kcptBigUnicode:
					return this.MainCacheAccessor.get_UnicodeProp(hvo, flid);
				case (int) CellarModuleDefns.kcptString:
				case (int) CellarModuleDefns.kcptBigString:
					return this.MainCacheAccessor.get_StringProp(hvo, flid).Text;
				case (int)CellarModuleDefns.kcptMultiString:
				case (int)CellarModuleDefns.kcptMultiBigString:
				case (int)CellarModuleDefns.kcptMultiUnicode:
				case (int)CellarModuleDefns.kcptMultiBigUnicode:
				{
					int wsid = SIL.FieldWorks.FDO.LangProj.LangProject.GetWritingSystem(frag, this, null, hvo, flid, 0);
					if (wsid == 0)
						return string.Empty;
					return this.MainCacheAccessor.get_MultiStringAlt(hvo, flid, wsid).Text;
				}
				default:
					// This string type is not supported.
					fTypeFound = false;
					return itype.ToString();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return a list of ClassAndPropInfo objects giving information about the classes that can be
		/// stored in the owning properties of the specified class.
		/// </summary>
		/// <param name="clsid">Class ID to get information on.</param>
		/// <param name="flidType">Type of fields to get information on.</param>
		/// <param name="excludeAbstractClasses">True to exclude abstract classes in the returned array.</param>
		/// <returns>A List that contains ClassAndPropInfo objects.</returns>
		/// ------------------------------------------------------------------------------------
		public List<ClassAndPropInfo> GetPropsAndClasses(int clsid, FieldType flidType, bool excludeAbstractClasses)
		{
			CheckDisposed();
			List<ClassAndPropInfo> result = new List<ClassAndPropInfo>();

			foreach(uint flid in DbOps.GetFieldsInClassOfType(m_mdc, clsid, flidType))
				AddClassesForField(flid, excludeAbstractClasses, result);

			return result;
		}

		/// <summary>
		/// Add to the list a ClassAndPropInfo for each concrete class of object that may be added to
		/// property flid.
		/// (Note that at present this does not depend on the object. But we expect eventually
		/// that this will become a method of FDO.CmObject.
		/// </summary>
		/// <param name="flid"></param>
		/// <param name="excludeAbstractClasses"></param>
		/// <param name="list"></param>
		public void AddClassesForField(uint flid, bool excludeAbstractClasses, List<ClassAndPropInfo> list)
		{
			CheckDisposed();
			uint clsidDst = GetDestinationClass(flid);
			if (clsidDst == 0)
				return;	// not enough information from this flid.
			string fieldName = m_mdc.GetFieldName(flid);
			int cClasses;
			m_mdc.GetAllSubclasses(clsidDst, 0, out cClasses, ArrayPtr.Null);
			int fldType = m_mdc.GetFieldType(flid);
			using (ArrayPtr rgclid = new ArrayPtr(cClasses * Marshal.SizeOf(typeof(uint))))
			{
				m_mdc.GetAllSubclasses(clsidDst, cClasses, out cClasses, rgclid);
				foreach (uint clsidPossDst in MarshalEx.NativeToArray(rgclid, cClasses, typeof(uint)))
				{
					ClassAndPropInfo cpi = new ClassAndPropInfo();
					cpi.fieldName = fieldName;
					cpi.flid = flid;
					cpi.signatureClsid = clsidPossDst;
					cpi.fieldType = fldType;
					cpi.signatureClassName = m_mdc.GetClassName(clsidPossDst);
					cpi.isAbstract = m_mdc.GetAbstract(clsidPossDst);
					cpi.isBasic = fldType < (int)FieldType.kcptMinObj;
					cpi.isCustom = GetIsCustomField(flid);
					cpi.isReference = this.IsReferenceProperty((int)flid);
					cpi.isVector = this.IsVectorProperty((int)flid);
					cpi.isVirtual = m_mdc.get_IsVirtual(flid);
					list.Add(cpi);
					if (excludeAbstractClasses && cpi.isAbstract)
						list.Remove(cpi);
				}
			}
		}

		/// <summary>
		/// Get's the destination class of the given flid, (try even from a virtual handler).
		/// </summary>
		/// <param name="flid"></param>
		/// <returns></returns>
		public uint GetDestinationClass(uint flid)
		{
			uint clsidDst = 0;
			// The MetaDataCache.GetDstClsId does not work for virtual handlers. Try another way.
			IVwVirtualHandler vh = (MainCacheAccessor as IVwCacheDa).GetVirtualHandlerId((int)flid);
			if (vh != null)
			{
				if (vh is BaseFDOPropertyVirtualHandler)
				{
					clsidDst = (uint)(vh as BaseFDOPropertyVirtualHandler).DestinationClassId;
				}
			}
			else
			{
				// get real information from metadata cache.
				clsidDst = m_mdc.GetDstClsId(flid);
			}
			return clsidDst;
		}

		/// <summary>
		/// Gets abstract information on given class id.
		/// </summary>
		/// <param name="clsid"></param>
		/// <returns></returns>
		public bool GetAbstract(int clsid)
		{
			CheckDisposed();
			return m_mdc.GetAbstract((uint)clsid);
		}

		/// <summary>
		/// find out if the 1 class is the same as or a subclass of another CELLAR class
		/// </summary>
		/// <param name="classId"></param>
		/// <param name="baseClassId"></param>
		/// <returns></returns>
		public bool IsSameOrSubclassOf(int classId, int baseClassId)
		{
			CheckDisposed();
			if (classId == baseClassId)
				return true;

			bool retval = false;
			int countOfClasses;
			m_mdc.GetAllSubclasses((uint)baseClassId, 0, out countOfClasses, ArrayPtr.Null);
			using (ArrayPtr subClassIds = new ArrayPtr(countOfClasses * Marshal.SizeOf(typeof(uint))))
			{
				m_mdc.GetAllSubclasses((uint)baseClassId, countOfClasses, out countOfClasses, subClassIds);
				foreach (uint c in MarshalEx.NativeToArray(subClassIds, countOfClasses, typeof(uint)))
				{
					if(classId == c)
					{
						retval = true;
						break;
					}
				}
			}
			return retval;
		}

		/// <summary>
		/// Gives a list of ClassAndPropInfo objects representing each field in the class (including super classes).
		/// </summary>
		/// <param name="classId">the id of the class</param>
		/// <returns></returns>
		public List<ClassAndPropInfo> GetFieldsOfClass(uint classId)
		{
			CheckDisposed();
			List<ClassAndPropInfo> result = new List<ClassAndPropInfo>();
			foreach (uint flid in DbOps.GetFieldsInClassOfType(m_mdc, classId, FieldType.kgrfcptAll))
			{
				// Figure all the classes that can be stored in property flid.
				ClassAndPropInfo cpi = GetClassAndPropInfo(flid);
				result.Add(cpi);
			}
			return result; // Will be empty, if the for loop did not find any
		}

		/// <summary>
		/// Get the real field ids owning the given 'classId'. Does not return fields owning subclasses.
		/// </summary>
		/// <param name="classId"></param>
		/// <returns></returns>
		public List<ClassAndPropInfo> GetFieldsOwningClass(uint classId)
		{
			List<ClassAndPropInfo> result = new List<ClassAndPropInfo>();
			if (DatabaseAccessor == null)
			{
				// Running with memory-only cache, get the info by brute force from the MDC.
				IFwMetaDataCache mdc = MetaDataCacheAccessor;
				int cfields = mdc.FieldCount;
				uint[] fields;
				using (ArrayPtr flids = MarshalEx.ArrayToNative(cfields, typeof(uint)))
				{
					mdc.GetFieldIds(cfields, flids);
					fields = (uint[])MarshalEx.NativeToArray(flids, cfields, typeof(uint));
				}
				foreach (uint flid in fields)
				{
					if (mdc.GetDstClsId(flid) == classId)
					{
						int typ = m_mdc.GetFieldType(flid);
						if (typ == (int)CellarModuleDefns.kcptOwningSequence || typ == (int)CellarModuleDefns.kcptOwningCollection
							|| typ == (int)CellarModuleDefns.kcptOwningAtom)
						{
							result.Add(GetClassAndPropInfo(flid));
						}
					}
				}
			}
			else
			{
				// normal case
				string sql = "select f.Id from Field$ f join Class$ c on c.Id = ?  and f.DstCls=c.Id";

				foreach (int flid in DbOps.ReadIntsFromCommand(this, sql, (int) classId))
				{
					ClassAndPropInfo cpi = GetClassAndPropInfo((uint) flid);
					result.Add(cpi);
				}
			}
			return result;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the class and prop info.
		/// </summary>
		/// <param name="flid">The flid.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public ClassAndPropInfo GetClassAndPropInfo(uint flid)
		{
			const uint kNegativeOne = uint.MaxValue; // what c++ uses as a -1 for an unsigned int

			ClassAndPropInfo cpi = new ClassAndPropInfo();
			cpi.flid = flid;
			cpi.fieldName = m_mdc.GetFieldName(flid);
			cpi.sourceClsid = m_mdc.GetOwnClsId(flid);
			cpi.signatureClsid = this.GetDestinationClass(flid);
			cpi.isReference = this.IsReferenceProperty((int)flid);	// !this.IsOwningProperty((int)flid);
			cpi.isVector = this.IsVectorProperty((int)flid);
			cpi.isCustom = GetIsCustomField(flid);
			int typ = m_mdc.GetFieldType(flid);
			cpi.fieldType = typ;
			cpi.isBasic = typ < (int)FieldType.kcptMinObj;
			// The signature class id is negative one if it was null in the DB! (TE-5627)
			if (cpi.signatureClsid != kNegativeOne)
				cpi.isAbstract = m_mdc.GetAbstract(cpi.signatureClsid);
			cpi.isVirtual = m_mdc.get_IsVirtual(flid);
			return cpi;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the hashtable of the specified hash table type.
		/// </summary>
		/// <param name="hashtableType">type of hash table on the cache.</param>
		/// <param name="dict">hash table of the specified type</param>
		/// <returns>A dictionary of the specified flavor</returns>
		/// ------------------------------------------------------------------------------------
		public bool TryGetHashtable<KeyType, ValueType>(Guid hashtableType,
			out Dictionary<KeyType, ValueType> dict)
		{
			IDictionary temp;
			bool fHashTableExists = m_hashtables.TryGetValue(hashtableType, out temp);
			dict = temp as Dictionary<KeyType, ValueType>;
			return fHashTableExists;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Remove hash table entry from hash table of hash tables on the cache.
		/// </summary>
		/// <param name="hashTableType"></param>
		/// ------------------------------------------------------------------------------------
		public void RemoveHashTable(Guid hashTableType)
		{
			if (m_hashtables.ContainsKey(hashTableType))
				m_hashtables.Remove(hashTableType);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the hashtable of the specified hash table type. If instantiating a new one,
		/// create a change watcher of the specified type to watch property changes in the
		/// given tag.
		/// </summary>
		/// <param name="hashtableType">type of hash table on the cache.</param>
		/// <param name="tag">The property tag that the change watcher wants to be notified about
		/// </param>
		/// <returns>A dictionary of the specified flavor</returns>
		/// ------------------------------------------------------------------------------------
		public Dictionary<KeyType, ValueType> GetHashtable<KeyType, ValueType,
			ChangeWatcherType>(Guid hashtableType, int tag)
			where ChangeWatcherType:ChangeWatcher, new()
		{
			// Determine if the dictionary exists in the hash table.
			Dictionary<KeyType, ValueType> dict;
			if (TryGetHashtable<KeyType, ValueType>(hashtableType, out dict))
				return dict; // return currently existing dictionary

			// Didn't find dictionary, so create a new one and add it to the hash table of dictionaries.
			dict = new Dictionary<KeyType,ValueType>();
			m_hashtables.Add(hashtableType, dict as IDictionary);

			// Determine if change watcher is already registered with cache
			bool registerWatcher = true;
			if (ChangeWatchers != null)
			{
				foreach (ChangeWatcher cw in ChangeWatchers)
				{
					if (cw is ChangeWatcherType)
					{
						registerWatcher = false;
						break;
					}
				}
			}
			// if required, register change watcher so we will update cache based on the tag.
			if (registerWatcher)
			{
				ChangeWatcherType cw = new ChangeWatcherType();
				cw.Init(this, tag);
			}

			return dict;
		}

		/// <summary>
		/// determine if the flid is in the custom range
		/// </summary>
		/// <param name="flid"></param>
		/// <returns></returns>
		public bool GetIsCustomField (uint flid)
		{
			CheckDisposed();
			long remainder;
			Math.DivRem( flid, 1000, out remainder);
			return remainder >= 500;
		}

		#endregion	// Meta data methods

		#region Misc methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a Set of database IDs for the given table.
		/// </summary>
		/// <param name="sTableName">Database table name.</param>
		/// <returns>A Set which contains zero, or more, IDs.</returns>
		/// ------------------------------------------------------------------------------------
		private Set<int> GetOwnerlessIds(string sTableName)
		{
			return new Set<int>(DbOps.ReadIntsFromCommand(this,
				string.Format("select ID from {0}_ where Owner$ is null", sTableName),
				null));
		}

		/// <summary>
		/// Wraps the GetLinkedObjs$ stored procedure.
		/// </summary>
		/// <param name="ids">Array of ids to process.</param>
		/// <param name="type">Mask that indicates what types of related objects should be retrieved.</param>
		/// <param name="includeBaseClasses">
		/// A flag that determines if the base classes of owned objects are included
		/// in the object list (e.g., rows for each object + all superclasses except CmObject.
		/// So if a CmPerson is included, it will also have a row for CmPossibility)
		/// </param>
		/// <param name="includeSubClasses">
		/// A flag that determines if the sub classes of owned objects are included in the object list.
		/// </param>
		/// <param name="recurse">
		/// A flag that determines if the owning tree is traversed.
		/// </param>
		/// <param name="referenceDirection">
		/// Determines which reference directions will be included in the results:
		/// (0=both, 1=referenced by this/these object(s), -1 reference this/these objects).
		/// </param>
		/// <param name="filterByClass">
		/// only return objects of this class (including subclasses of this class).
		/// Zero (0) returns all classes.
		/// </param>
		/// <param name="calculateOrderKey">
		/// A flag that determines if the order key is calculated.
		/// </param>
		/// <returns>A generic list that contains zero, or more, LinkedObjectInfo objects.</returns>
		/// <remarks>
		/// The <b>ids</b> parameter handles the first two parameters in the actual stored procedure (@ObjId and @hXMLDocObjList).
		/// </remarks>
		public virtual List<LinkedObjectInfo> GetLinkedObjects(List<int> ids, LinkedObjectType type,
			bool includeBaseClasses, bool includeSubClasses, bool recurse,
			ReferenceDirection referenceDirection, int filterByClass, bool calculateOrderKey)
		{
			CheckDisposed();
			// The SQL commands must NOT modify the database contents!
			List<LinkedObjectInfo> list = new List<LinkedObjectInfo>();
			if (ids.Count == 0)
				return list; // saves query and guards against index [0] out of range below.
			IOleDbCommand odc = null;
			try
			{
				m_ode.CreateCommand(out odc);

				// See if we have one id, or several, to work on.
				string objId = ids[0].ToString();
				string sql;
				bool fGetLinkedObjsProc = true;

				// Get References to objects. Currently the function handles only one at a time.
				if ((ids.Count == 1) && includeBaseClasses && includeSubClasses && includeSubClasses
					&& recurse && ((int)referenceDirection == -1) && !calculateOrderKey)
				{
					sql = String.Format("SELECT "
						+ "fn.ObjId, fn.ObjClass, "
						+ "fn.ObjLevel, fn.RefObjId, fn.RefObjClass, "
						+ "fn.RefObjField, fn.RefObjFieldOrder, fn.RefObjFieldType, "
						+ "NULL AS RefObjFieldOrdKey "
						+ "FROM dbo.fnGetRefsToObj({0}, NULL) fn", objId);
					fGetLinkedObjsProc = false;
					// TODO: fn.ClassLevel is hard-coded as 0 in the stored procedure. Is this right?
					// Is this field ever used? Looks like it's used for InheritDepth below.
				}

					// Get any other kind of linked objects
				else
				{
					// Create temporary table.
					sql = "create table [#ObjInfoTbl$]("
						  + "[ObjId] int not null,"
						  + "[ObjClass] int null,"
						  + "[OwnerDepth] int null default(0),"
						  + "[RelObjId] int null,"
						  + "[RelObjClass] int null,"
						  + "[RelObjField] int null,"
						  + "[RelOrder] int null,"
						  + "[RelType] int null,"
						  + "[OrdKey] varbinary(250) null default(0));";
					odc.ExecCommand(sql, (int) SqlStmtType.knSqlStmtNoResults);

					// REVIEW (SteveMiller): Indexes on temp tables may well be too much
					// overhead. FDB-225.

					// Set up index for temporary table.
					odc.ExecCommand("create nonclustered index #ObjInfoTblObjId on #ObjInfoTbl$ ([ObjId])",
									(int) SqlStmtType.knSqlStmtNoResults);

					// Run stored procedure.
					StringBuilder sqlBldr = new StringBuilder();
					sqlBldr.Append("exec GetLinkedObjs$ '");
					foreach (int id in ids)
						sqlBldr.AppendFormat("{0},", id.ToString()); // @objids nvarchar(max)
					sqlBldr.Append(
						String.Format("', {0}, {1}, {2}, {3}, {4}, {5}, {6}\n",
									  (int) type, // @grfcpt int=kgrfcptAll
									  includeBaseClasses ? "1" : "0", // @fBaseClasses bit=0
									  includeSubClasses ? "1" : "0", // @fSubClasses bit=0
									  recurse ? "1" : "0", // @fRecurse bit=1
									  (int) referenceDirection, // @nRefDirection smallint=0
									  (filterByClass == 0) ? "null" : filterByClass.ToString(), // @riid int=null
									  calculateOrderKey ? "1" : "0")); // @fCalcOrdKey bit=1
					odc.ExecCommand(sqlBldr.ToString(), (int) SqlStmtType.knSqlStmtStoredProcedure);

					// Get results from running GetLinkedObjs$ stored procedure.
					sql = "SELECT ObjId, ObjClass, OwnerDepth, RelObjId, RelObjClass, " +
						  "RelObjField, RelOrder, RelType, OrdKey FROM #ObjInfoTbl$";
				}

				odc.ExecCommand(sql, (int)SqlStmtType.knSqlStmtSelectWithOneRowset);
				odc.GetRowset(0);
				bool fMoreRows;
				odc.NextRow(out fMoreRows);
				while (fMoreRows)
				{
					LinkedObjectInfo loi = new LinkedObjectInfo();
					loi.ObjId = DbOps.ReadInt(odc, 0);
					loi.ObjClass = DbOps.ReadInt(odc, 1);
					loi.OwnerDepth = DbOps.ReadInt(odc, 2);
					loi.RelObjId = DbOps.ReadInt(odc, 3);
					loi.RelObjClass = DbOps.ReadInt(odc, 4);
					loi.RelObjField = DbOps.ReadInt(odc, 5);
					loi.RelOrder = DbOps.ReadInt(odc, 6);
					loi.RelType = DbOps.ReadInt(odc, 7);
					loi.OrdKey = DbOps.ReadBytes(odc, 8);
					list.Add(loi);
					odc.NextRow(out fMoreRows);
				}

				// Delete temporary table left over
				if (fGetLinkedObjsProc)
				{
					odc.ExecCommand("drop table [#ObjInfoTbl$]", (int)SqlStmtType.knSqlStmtNoResults);
				}
			}
			finally
			{
				DbOps.ShutdownODC(ref odc);
			}

			return list;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Client should call this to clear the Undo/Redo buffers
		/// and commit the database transaction.
		/// [Note: The changes have already been made to the FW cache and the Database.
		/// But the DB transaction was not committed.]
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void Save()
		{
			CheckDisposed();
			if (m_odde != null && !this.AddAllActionsForTests)
				((IVwOleDbDa)m_odde).Save();
		}

		/// <summary>
		/// Clear everything that came from the database (used in Refresh)
		/// </summary>
		public void ClearAllData()
		{
			CheckDisposed();
			VwCacheDaAccessor.ClearAllData();
			FieldDescription.ClearDataAbout(this);
		}

		/// ------------------------------------------------------------------------------------
		///<summary>Gets the index of an object in a vector.</summary>
		///<param name="hvoOwner">Id of main object (owning object).</param>
		///<param name="flid">Field ID of main object that holds hvoItem.</param>
		///<param name="hvoItem">ID of object in flid.</param>
		///<returns>Returns the index of hvoItem in flid.
		///If flid is an atomic property, then zero (0) is returned.
		///If hvoItem is not in flid, then -1 is returned.</returns>
		/// ------------------------------------------------------------------------------------
		public int GetObjIndex(int hvoOwner, int flid, int hvoItem)
		{
			CheckDisposed();
			int ihvoItem = ((ISilDataAccess)m_odde).GetObjIndex(hvoOwner, flid, hvoItem);
			return ihvoItem;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure the object really exists in the database and is of the correct type.
		/// This always involves one database query unless we are in a bulk load mode,
		/// and often then.
		/// </summary>
		/// <param name="hvo">ID of object.</param>
		/// <param name="clsid">The purported class Id of the object</param>
		/// <returns>True if object is in database and is of the type (or subtype of) the
		/// specified class ID</returns>
		/// ------------------------------------------------------------------------------------
		public virtual bool IsRealObject(int hvo, int clsid)
		{
			return !IsDummyObject(hvo) && IsValidObject(hvo) &&
				ClassIsOrInheritsFrom((uint)GetClassOfObject(hvo), (uint)clsid);

			// The implementation below doesn't work if we have the situation that the class
			// information about hvo is still cached, but the object itself was deleted from
			// the database. The code only checks if we have the class information for hvo
			// and if it is of the correct type, but it doesn't check if the object still exists
			// in the database. For an example when this can happen see TE-5922 (undoing an
			// insert book/delete book).
			//CheckDisposed();
			//// This seemed to do the same thing and was cached so was faster...
			//if (hvo < 1)
			//    return false;
			//int objClass = GetClassOfObject(hvo);
			//if (objClass == clsid)
			//    return true;
			//if (objClass <= 0)
			//    return false;

			//// The class ID may be a derived class so we need to check to see
			//// if its base type matches.
			//Type t1 = CmObject.GetTypeFromFWClassID(this, clsid);
			//Type t2 = CmObject.GetTypeFromFWClassID(this, objClass);
			//while (t2 != null)
			//{
			//    t2 = t2.BaseType;
			//    if (t1 == t2)
			//        return true;
			//}
			//return false;
		}

		/// <summary>
		/// A dummy object is an object in the cache, but not a real object in the database.
		/// By default we don't check to see if the object is usable (it may have been
		/// cleared by Refresh).
		/// </summary>
		/// <param name="hvo"></param>
		/// <returns></returns>
		public bool IsDummyObject(int hvo)
		{
			CheckDisposed();
			return MainCacheAccessor.get_IsDummyId(hvo) ;
		}

		/// <summary>
		/// Verify that this id refers to a Dummy Object.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="fCheckValidity">if true, checks to see if this DummyObject
		/// it is in a readable state. </param>
		/// <returns></returns>
		public bool IsDummyObject(int hvo, bool fCheckValidity)
		{
			CheckDisposed();
			if (!IsDummyObject(hvo))
				return false;
			if (fCheckValidity)
				return  MainCacheAccessor.get_IsValidObject(hvo);
			else
				return true;
		}

		private bool m_fCacheValidObjectIds = false;
		private Set<int> m_setValidHvos;
		/// <summary>
		/// Flag whether or not to cache valid object ids.  The default is NO.  However, in
		/// some operations such as laying out a view, the speed benefit of caching during that
		/// operation may outway the danger of stale data.
		/// </summary>
		public bool CacheValidObjectIds
		{
			get
			{
				CheckDisposed();
				return m_fCacheValidObjectIds;
			}
			set
			{
				CheckDisposed();
				if (m_fCacheValidObjectIds == value)
					return;
				m_fCacheValidObjectIds = value;
				if (m_fCacheValidObjectIds)
					m_setValidHvos = new Set<int>();
				else
					m_setValidHvos = null;
			}
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns true if the object could be considered real or a dummy object
		/// in a readable state, and furthermore, is of the specified class.
		/// Note that this will always do a database query, except
		/// when we are caching validity, which is done only for the duration of one batch
		/// of EnableBulkLoading.
		/// </summary>
		/// <param name="hvo">The hvo.</param>
		/// <param name="clsid"></param>
		/// <returns><c>true</c> if the object is valid; otherwise, <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		public bool IsValidObject(int hvo, int clsid)
		{
			return IsValidObject(hvo) && ClassIsOrInheritsFrom((uint)GetClassOfObject(hvo), (uint)clsid);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns true if the object could be considered real or a dummy object
		/// in a readable state. Note that this will always do a database query, except
		/// when we are caching validity, which is done only for the duration of one batch
		/// of EnableBulkLoading.
		/// </summary>
		/// <param name="hvo">The hvo.</param>
		/// <returns><c>true</c> if the object is valid; otherwise, <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		public bool IsValidObject(int hvo)
		{
			CheckDisposed();
			bool fIsValid = false;

			if (hvo == 0)
				return false;
			if (m_setValidHvos != null && m_setValidHvos.Contains(hvo))
				return true;
			try
			{

				// No point in doing this as well, it's essentially what get_IsValidObject does,
				// doing it first typically doubles the work.
				//int clsid = GetClassOfObject(hvo);
				//fIsValid = (clsid > 0) && MainCacheAccessor.get_IsValidObject(hvo);
				fIsValid = MainCacheAccessor.get_IsValidObject(hvo);
				if (fIsValid && m_setValidHvos != null)
				{
					m_setValidHvos.Add(hvo);
					if (!MainCacheAccessor.get_IsDummyId(hvo))
					{
						// We're bulk loading...get ALL the valid objects (of that class) at once!
						int clid = GetClassOfObject(hvo); // already cached by testing get_IsValidObject()
						string sql = "select id from CmObject where Class$ = " + clid;
						int[] validObjects = DbOps.ReadIntArrayFromCommand(this, sql, null);
						foreach (int hvoValid in validObjects)
						{
							m_setValidHvos.Add(hvoValid);
							VwCacheDaAccessor.CacheIntProp(hvoValid, (int)CmObjectFields.kflidCmObject_Class, clid);
						}
					}
				}
			}
			catch (Exception)
			{
			}
			return fIsValid;
		}

		/// <summary>
		/// Check whether the given object is valid.  If it's not null, but invalid, report this
		/// to the user by a message box.
		/// </summary>
		/// <returns></returns>
		public bool VerifyValidObject(ICmObject obj)
		{
			if (obj == null)
				return VerifyValidObject(0);
			else
				return VerifyValidObject(obj.Hvo);
		}

		/// <summary>
		/// Check whether the given object id is valid.  If it's not zero, but invalid, report
		/// this to the user by a message box.
		/// </summary>
		public bool VerifyValidObject(int hvo)
		{
			if (hvo == 0)
			{
				MessageBox.Show(Strings.ksEntryNotSetMaybeDeleted, Strings.ksEntryNotSet,
					MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return false;
			}
			else if (IsValidObject(hvo))
			{
				return true;
			}
			else
			{
				MessageBox.Show(Strings.ksEntryHasBeenDeleted, Strings.ksEntryDeleted,
					MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return false;
			}
		}

		/// <summary>
		/// Returns a unique id for making dummy objects that can be stored in the cache.
		/// </summary>
		/// <param name="hvo"></param>
		public void CreateDummyID(out int hvo)
		{
			CheckDisposed();
			this.VwOleDbDaAccessor.CreateDummyID(out hvo); // Enhance: make a call to a new interface method of ISilDataAccess.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks whether the given server and database match the one used for this cache's
		/// connection.
		/// </summary>
		/// <param name="sServer">Name of the server</param>
		/// <param name="sDbName">Name of the database</param>
		/// <returns>true if server and db match the one used for this cache's
		/// connection (case-insensitive); false otherwise</returns>
		/// ------------------------------------------------------------------------------------
		public bool IsSameConnection(string sServer, string sDbName)
		{
			CheckDisposed();
			return (string.Compare(ServerName, sServer, true) == 0 &&
				string.Compare(DatabaseName, sDbName, true) == 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the number of remote clients that are currently connected to the Database
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public int GetNumberOfRemoteClients()
		{
			string query = "select count(*) from sys.sysprocesses [sproc] " +
				"join sys.sysdatabases [sdb] on [sdb].[dbid] = [sproc].[dbid] and [name] = DB_NAME() " +
				"where rtrim([sproc].[hostname]) != HOST_NAME()";
			int numConn;
			return DbOps.ReadOneIntFromCommand(this, query, null, out numConn) ? numConn : 0;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Find the guid of the CmObject in the database that matches this ID.
		/// </summary>
		///
		/// <param name="hvo">Object id to look for in the database.</param>
		///
		/// <returns>Corresponding GUID, or null if not found</returns>
		/// -----------------------------------------------------------------------------------
		public virtual Guid GetGuidFromId(int hvo)
		{
			CheckDisposed();
			return GetGuidProperty(hvo, (int)CmObjectFields.kflidCmObject_Guid);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Find the ID of the CmObject in the database that matches this guid.
		/// </summary>
		///
		/// <param name="guid">A GUID that represents an entry in the database CmObject table
		/// (the Guid$ field).</param>
		///
		/// <returns>The database ID corresponding to the GUID (the Id field), or zero if the
		/// GUID is invalid.</returns>
		/// -----------------------------------------------------------------------------------
		public virtual int GetIdFromGuid(Guid guid)
		{
			CheckDisposed();
			return m_odde.get_ObjFromGuid(guid);
		}

		/// <summary>
		/// Find the ID of the CmObject in the database that matches this guid.
		/// </summary>
		/// <param name="guid">string that can be made into a system Guid.</param>
		/// <returns></returns>
		public virtual int GetIdFromGuid(string guid)
		{
			CheckDisposed();
			return GetIdFromGuid(new Guid(guid));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a string representation of the object suitable to put on the clipboard.
		/// </summary>
		/// <param name="guid">The guid of the object in the DB</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public string TextRepOfObj(Guid guid)
		{
			CheckDisposed();
			int hvoObj = GetIdFromGuid(guid);
			if (hvoObj == 0)
				return null; // guid does not reference object in this database (TE-5012)

			switch(GetClassOfObject(hvoObj))
			{
				case CmPicture.kclsidCmPicture:
				{
					CmPicture pict = new CmPicture(this, hvoObj);
					return pict.TextRepOfPicture;
				}
				case StFootnote.kclsidStFootnote:
				{
					StFootnote footnote = new StFootnote(this, hvoObj);
					return footnote.GetTextRepresentation();
				}
			}
			return null;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Adds a ChangeWatcher to a collection of them, but only one time.
		/// No duplicates are allowed.
		/// </summary>
		/// <param name="cw">Change watcher potentially being added.</param>
		/// <returns>True, if it was added, otherwise false</returns>
		/// -----------------------------------------------------------------------------------
		public bool AddChangeWatcher(ChangeWatcher cw)
		{
			CheckDisposed();

			if (!ChangeWatchers.Contains(cw)) // Only add it once.
			{
				m_rgChangeWatchers.Add(cw);
				return true; // Was added.
			}
			return false; // Not added.
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Remove a ChangeWatcher, if it is present.
		/// </summary>
		/// <param name="cw">Change watcher being removed, if present.</param>
		/// -----------------------------------------------------------------------------------
		internal void RemoveChangeWatcher(ChangeWatcher cw)
		{
			CheckDisposed();

			// It is only put in it once, if the calling code was well-behaved.
			ChangeWatchers.Remove(cw);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Store the given sync info in the database if there is more than one connection.
		/// </summary>
		/// <param name="appGuid"></param>
		/// <param name="sync">The Sync information to store describing a given change.</param>
		/// ------------------------------------------------------------------------------------
		public virtual void StoreSync(Guid appGuid, SyncInfo sync)
		{
			CheckDisposed();
			string sSql = string.Format("exec StoreSyncRec$ ?, '{0}', {1}, {2}, {3}",
				appGuid, (int)sync.msg, sync.hvo, sync.flid);

			IOleDbCommand odc = null;
			try
			{
				m_ode.CreateCommand(out odc);
				// It's safest to pass the database name as a parameter, because it may contain
				// apostrophes.  See LT-8910.
				odc.SetStringParameter(1, (uint)DBPARAMFLAGSENUM.DBPARAMFLAGS_ISINPUT,
					null, DatabaseName, (uint)DatabaseName.Length);
				odc.ExecCommand(sSql, (int)SqlStmtType.knSqlStmtStoredProcedure);
			}
			finally
			{
				DbOps.ShutdownODC(ref odc);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a virtual property from the cache
		/// </summary>
		/// <param name="className">class name of the property</param>
		/// <param name="fieldName">field name of the property</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public IVwVirtualHandler GetVirtualProperty(string className, string fieldName)
		{
			CheckDisposed();
			IVwCacheDa cda = MainCacheAccessor as IVwCacheDa;
			if (cda != null)
				return cda.GetVirtualHandlerName(className, fieldName);
			else
				return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Install a virtual property handler to the cache
		/// </summary>
		/// <param name="propertyHandler"></param>
		/// ------------------------------------------------------------------------------------
		public void InstallVirtualProperty(IVwVirtualHandler propertyHandler)
		{
			CheckDisposed();
			IVwCacheDa cda = MainCacheAccessor as IVwCacheDa;
			if (cda != null)
				cda.InstallVirtual(propertyHandler);
		}

		private AutoloadPolicies m_alpSaved = AutoloadPolicies.kalpNoAutoload;
		private int m_cEnableBulkLoadingCalled = 0;
		/// <summary>
		/// This method enables/disables bulk loading of lots of data.
		/// </summary>
		public void EnableBulkLoadingIfPossible(bool fEnable)
		{
			if (fEnable)
				++m_cEnableBulkLoadingCalled;
			else
				--m_cEnableBulkLoadingCalled;
			if (m_cEnableBulkLoadingCalled > (fEnable ? 1 : 0))
				return;			// Don't need to change the state of affairs.
			Debug.Assert(m_cEnableBulkLoadingCalled == (fEnable ? 1 : 0));
			BaseVirtualHandler.ForceBulkLoadIfPossible = fEnable;
			this.CacheValidObjectIds = fEnable;
			if (fEnable)
			{
				m_alpSaved = this.VwOleDbDaAccessor.AutoloadPolicy;
				if (m_alpSaved != AutoloadPolicies.kalpLoadForAllOfObjectClass)
					this.VwOleDbDaAccessor.AutoloadPolicy = AutoloadPolicies.kalpLoadForAllOfObjectClass;
			}
			else
			{
				if (m_alpSaved != AutoloadPolicies.kalpLoadForAllOfObjectClass)
					this.VwOleDbDaAccessor.AutoloadPolicy = m_alpSaved;
			}
		}

		#endregion	// Misc methods

		#region Special Preload Methods
		/// <summary>
		///
		/// </summary>
		/// <param name="hvoOwningObj"></param>
		/// <param name="flidTag"></param>
		/// <param name="ws"></param>
		/// <param name="sViewName"></param>
		public void LoadAllOfMultiUnicodeAlt(int hvoOwningObj, int flidTag, int ws, string sViewName)
		{
			CheckDisposed();
			string sQry = "select obj, txt from " + sViewName +
				" where ws= " + ws;


			IDbColSpec dcs = DbColSpecClass.Create();
			dcs.Push((int)DbColType.koctBaseId, 0, 0, 0);
			dcs.Push((int)DbColType.koctMltAlt, 1, flidTag, ws);

			LoadData(sQry, dcs, hvoOwningObj);
			Marshal.ReleaseComObject(dcs);
		}

		/// <summary>
		/// Load all writing systems of a multiuser code for all objects of the giving class.  See remarks for performance caution.
		/// </summary>
		/// <remarks>
		/// Note, when you later ask the cache for a string of a certain encoding, if that string is empty,
		/// there will still be a cache hit.  This can be a major performance problem.  In that case, consider
		/// using LoadAllOfOneWsOfAMultiUnicode() instead, for which you must specify the particular encoding your interested in..
		/// </remarks>
		/// <xparam name="hvoOwningObj">use 0 if you don't care who the owner is</xparam>
		/// <param name="flidTag"></param>
		/// <xparam name="ws"></xparam>
		/// <param name="sClassName"></param>
		public void LoadAllOfMultiUnicode(/*int hvoOwningObj,*/ int flidTag, string sClassName)
		{
			CheckDisposed();
			string sField = MetaDataCacheAccessor.GetFieldName((uint)flidTag);
			string sClass = MetaDataCacheAccessor.GetOwnClsName((uint)flidTag);

			string sQry = "select x.[id], mlt.txt, " + flidTag + ", mlt.ws, null, cmo.UpdStmp "
				+ " from  " + sClassName + " x "
				+ " join " + sClass + "_" + sField + " mlt "
				+ " on x.[id] = mlt.obj "
				+ " join CmObject cmo on cmo.id = x.id ";

			IDbColSpec dcs = DbColSpecClass.Create();
			dcs.Push((int)DbColType.koctBaseId, 0, 0, 0);
			dcs.Push((int)DbColType.koctMlaAlt, 1, 0, 0);
			dcs.Push((int)DbColType.koctFlid, 1, 0, 0);
			dcs.Push((int)DbColType.koctEnc, 1, 0, 0);
			//This is needed even though the format is meaningless for Unicode strings.
			dcs.Push((int)DbColType.koctFmt, 1, 0, 0);
			//This is here to support the synchronizing in system.
			dcs.Push((int)DbColType.koctTimeStamp, 1, 0, 0);

			LoadData(sQry, dcs, 0);
			Marshal.ReleaseComObject(dcs);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method is used to optimize work on a large list of objects, typically a
		/// significant fraction of all objects of a given class. It tests whether the
		/// particular property of the particular object (and, if relevant, the particular ws)
		/// is already cached. If not, it loads that property for ALL objects of the class of
		/// the given object.
		/// </summary>
		/// <param name="hvo">The hvo of the parent object</param>
		/// <param name="flid">The field that should be preloaded</param>
		/// <param name="ws">The writing system if relevant for specified flid, otherwise
		/// ignored.</param>
		/// ------------------------------------------------------------------------------------
		public bool PreloadIfMissing(int hvo, int flid, int ws)
		{
			CheckDisposed();
			return PreloadIfMissing(new int[] { hvo }, flid, ws, true);
		}

		/// <summary>
		/// This method is used to optimize work on a large list of objects, typically a
		/// significant fraction of all objects of a given class. It tests whether the
		/// particular property of the given objects (and, if relevant, the particular ws)
		/// is already cached. If not, it loads that property for the specified objects of the class of
		/// the given object.
		/// </summary>
		/// <param name="hvos">owning objects for which to load data</param>
		/// <param name="flid">the property of the data to load.</param>
		/// <param name="ws"></param>
		/// <param name="fLoadForEntireClassIfMissing">if true and not all of the given objects have their data loaded,
		/// we will load all the flid data for the Class of the objects given. if false, we will only load
		/// data for the given objects that are missing the data.</param>
		/// <returns></returns>
		public bool PreloadIfMissing(int[] hvos, int flid, int ws, bool fLoadForEntireClassIfMissing)
		{
			int iType = m_mdc.GetFieldType((uint)flid);
			switch (iType)
			{
				case (int)CellarModuleDefns.kcptString:
				case (int)CellarModuleDefns.kcptBigString:
					break;
				default:
					if (!fLoadForEntireClassIfMissing)
					{
						throw new Exception(String.Format("PreloadIfMissing does not yet support loading flid {0} for specified hvos on type {1}. It can only load for entire class.",
							flid, iType));
					}
					break;
			}
			return PreloadIfMissing(hvos, flid, iType, ws, fLoadForEntireClassIfMissing);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method is used to optimize work on a large list of objects, typically a
		/// significant fraction of all objects of a given class. It tests whether the
		/// particular property of the particular object (and, if relevant, the particular ws)
		/// is already cached. If not, it loads that property for ALL objects of the class of
		/// the given object.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="flid"></param>
		/// <param name="itype">returned by meta data cache.GetFieldType for flid.</param>
		/// <param name="ws"></param>
		/// ------------------------------------------------------------------------------------
		public bool PreloadIfMissing(int hvo, int flid, int itype, int ws)
		{
			return PreloadIfMissing(new int[] { hvo }, flid, itype, ws, true);
		}

		private bool PreloadIfMissing(int[] hvos, int flid, int itype, int ws, bool fLoadForEntireClassIfMissing)
		{
			CheckDisposed();
			List<int> hvosNeedingLoad = new List<int>(hvos.Length);
			// figure out which objects still need to load the flid data.
			foreach (int hvo in hvos)
			{
				if (!MainCacheAccessor.get_IsPropInCache(hvo, flid, itype, ws))
					hvosNeedingLoad.Add(hvo);
			}

			if (hvosNeedingLoad.Count == 0)
			{
				return false;
			}

			// Load all of it, assume they are all the same class.
			int clid = GetClassOfObject(hvosNeedingLoad[0]);
			string className = m_mdc.GetClassName((uint) clid);
			switch(itype)
			{
				case (int)CellarModuleDefns.kcptInteger:
					LoadAllOfAnIntProp(flid);
					break;
				case (int)CellarModuleDefns.kcptUnicode:
				case (int)CellarModuleDefns.kcptBigUnicode:
					LoadAllOfOneWsOfAMultiUnicode(flid, className, ws);
					break;
				case (int) CellarModuleDefns.kcptString:
				case (int) CellarModuleDefns.kcptBigString:
					if (fLoadForEntireClassIfMissing)
						LoadAllOfAStringProp(flid);
					else
						LoadAllOfAStringProp(flid, hvosNeedingLoad.ToArray());
					break;
				case (int)CellarModuleDefns.kcptMultiString:
				case (int)CellarModuleDefns.kcptMultiBigString:
					LoadAllOfOneWsOfAMultiString(flid, className, ws);
					break;
				case (int)CellarModuleDefns.kcptMultiUnicode:
				case (int)CellarModuleDefns.kcptMultiBigUnicode:
					LoadAllOfOneWsOfAMultiUnicode(flid, className, ws);
					break;
				case (int)CellarModuleDefns.kcptOwningAtom:
					LoadAllOfAnOwningAtomicProp(flid, className);
					break;
				case (int)CellarModuleDefns.kcptOwningCollection:
				case (int)CellarModuleDefns.kcptOwningSequence:
					LoadAllOfAnOwningVectorProp(flid, className);
					break;
				default:
					// Not implemented, harmless not to preload.
					Debug.WriteLine("Trying to preload all of undefined prop type " + itype + " for flid " + flid);
					return false;
			}
			return true; // loaded all matching this class and flid.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load all of a single writing system of a MultiString (or MultiBigString) field,
		/// plus make an empty slot in the cache for any objects that don't have this WS so that
		/// if the string is missing then laziness code will not  hit the database just to find that,
		/// lo, the string is missing.
		/// </summary>
		/// <param name="flidTag">The flid tag.</param>
		/// <param name="sClassName">Name of the s class.</param>
		/// <param name="ws">The ws.</param>
		/// <remarks>Note that usually, sClassName is the same as the sClass deduced from the flid.
		/// We may want an override where this argument is optional. However, it is possible to
		/// load the property only for a subclass of objects, for example, the CmPossibility_Name
		/// of all CmPersons.</remarks>
		/// ------------------------------------------------------------------------------------
		public void LoadAllOfOneWsOfAMultiString(int flidTag, string sClassName, int ws)
		{
			CheckDisposed();
			string sField = MetaDataCacheAccessor.GetFieldName((uint)flidTag);
			string sClass = MetaDataCacheAccessor.GetOwnClsName((uint)flidTag);

			//Notice that the "left out or join" is what gives us and empty row even if the string is missing.
			//We need this, to circumvent the laziness code later.

			string sQry = "select cmo.id, cmo.UpdStmp, x.txt, x.fmt from " + sClassName + "_ cmo "
				+ " left outer join " + sClass + "_" + sField + " x on cmo.id = x.obj and ws = " + ws.ToString();

			IDbColSpec dcs = DbColSpecClass.Create();
			dcs.Push((int)DbColType.koctBaseId, 0, 0, 0);
			dcs.Push((int)DbColType.koctTimeStamp, 1, 0, 0);
			dcs.Push((int)DbColType.koctMlsAlt, 1, flidTag, ws);
			dcs.Push((int)DbColType.koctFmt, 1, flidTag, ws);

			LoadData(sQry, dcs, 0);
			Marshal.ReleaseComObject(dcs);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load a value for the given string (or BigString) property into the cache for every
		/// object of the specified class.
		/// </summary>
		/// <param name="flidTag">The flid tag.</param>
		/// ------------------------------------------------------------------------------------
		public void LoadAllOfAStringProp(int flidTag)
		{
			LoadAllOfAStringProp(flidTag, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loads all of A string prop.
		/// </summary>
		/// <param name="flidTag">The flid tag.</param>
		/// <param name="hvos">particular objects to load for.
		/// if empty or null, we'll load for the entire class.</param>
		/// m&gt;
		/// ------------------------------------------------------------------------------------
		public void LoadAllOfAStringProp(int flidTag,  int [] hvos)
		{
			CheckDisposed();
			string sField = MetaDataCacheAccessor.GetFieldName((uint)flidTag);
			string sClass = MetaDataCacheAccessor.GetOwnClsName((uint)flidTag);

			// Notice that the "left out or join" is what gives us an empty row even if the
			// string is missing.
			// We need this, to circumvent the laziness code later.
			// Sample query: select cmo.id, cmo.UpdStmp, cmo.contents,
			//		cmo.Contents_fmt from StTxtPara_ cmo

			string sQryBase = "select cmo.id, cmo.UpdStmp, cmo." + sField + ", cmo." + sField + "_fmt "
				+ " from " + sClass + "_ cmo";
			IDbColSpec dcs = DbColSpecClass.Create();
			dcs.Push((int)DbColType.koctBaseId, 0, 0, 0);
			dcs.Push((int)DbColType.koctTimeStamp, 1, 0, 0);
			dcs.Push((int)DbColType.koctString, 1, flidTag, 0);
			dcs.Push((int)DbColType.koctFmt, 1, flidTag, 0);

			string sQry = null;
			if (hvos != null && hvos.Length > 0)
			{
				// Some of our lookups can have tens of thousands of ids for large projects.
				// This can choke SQL Server (see LT-6245), so break the list into manageable chunks.
				StringBuilder sb = new StringBuilder(20000);
				for (int i = 0; i < hvos.Length; ++i)
				{
					if (sb.Length > 0)
						sb.Append(",");
					sb.Append(hvos[i].ToString());
					if (((i + 1) % 8192) == 0)
					{
						sQry = String.Format("{0} where cmo.id in ({1})", sQryBase, sb.ToString());
						LoadData(sQry, dcs, 0);
						sb.Remove(0, sb.Length);
					}
				}
				if (sb.Length > 0)
					sQry = String.Format("{0} where cmo.id in ({1})", sQryBase, sb.ToString());
				else
					sQry = null;
			}
			else
			{
				sQry = sQryBase;
			}
			if (sQry != null)
				LoadData(sQry, dcs, 0);
			Marshal.ReleaseComObject(dcs);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load a value for the given integer property into the cache for every object of the
		/// specified class.
		/// </summary>
		/// <param name="flidTag">The flid tag.</param>
		/// ------------------------------------------------------------------------------------
		public void LoadAllOfAnIntProp(int flidTag)
		{
			CheckDisposed();
			string sField = MetaDataCacheAccessor.GetFieldName((uint)flidTag);
			string sClass = MetaDataCacheAccessor.GetOwnClsName((uint)flidTag);

			// Sample query: select cmo.id, cmo.UpdStmp, cmo.Ref from ChkRef_ cmo

			string sQry = "select cmo.id, cmo.UpdStmp, cmo." + sField + " from " + sClass + "_ cmo";

			IDbColSpec dcs = DbColSpecClass.Create();
			dcs.Push((int)DbColType.koctBaseId, 0, 0, 0);
			dcs.Push((int)DbColType.koctTimeStamp, 1, 0, 0);
			dcs.Push((int)DbColType.koctInt, 1, flidTag, 0);

			LoadData(sQry, dcs, 0);
			Marshal.ReleaseComObject(dcs);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load a value for the given owning atomic property into the cache for every
		/// object of the specified class.
		/// </summary>
		/// <param name="flidTag">The flid tag.</param>
		/// <param name="sClassName">Name of the s class.</param>
		/// <remarks>Note that usually, sClassName is the same as the sClass deduced from the flid.
		/// We may want an override where this argument is optional. However, it is possible to
		/// load the property only for a subclass of objects, for example, the CmPossibility_Name
		/// of all CmPersons.</remarks>
		/// ------------------------------------------------------------------------------------
		public void LoadAllOfAnOwningAtomicProp(int flidTag, string sClassName)
		{
			CheckDisposed();
			string sField = MetaDataCacheAccessor.GetFieldName((uint)flidTag);
			string sClass = MetaDataCacheAccessor.GetOwnClsName((uint)flidTag);

			// Notice that the "left out or join" is what gives us and empty row even if the
			// string is missing.
			// We need this, to circumvent the laziness code later.
			// Sample query: select cmo.id, cmo.UpdStmp, cmo.contents,
			//		cmo.Contents_fmt from StTxtPara_ cmo

			string sQry = "select cmo.id, cmo.UpdStmp, x.id from " + sClassName + "_ cmo "
				+ "left outer join CmObject x on x.owner$ = cmo.id and x.ownflid$ = " + flidTag;

			IDbColSpec dcs = DbColSpecClass.Create();
			dcs.Push((int)DbColType.koctBaseId, 0, 0, 0);
			dcs.Push((int)DbColType.koctTimeStamp, 1, 0, 0);
			dcs.Push((int)DbColType.koctObjOwn, 1, flidTag, 0);

			LoadData(sQry, dcs, 0);
			Marshal.ReleaseComObject(dcs);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load a value for the given owning sequence or collection property into the cache for every
		/// object of the specified class. (Collections will be in an arbitrary order.)
		/// </summary>
		/// <param name="flidTag"></param>
		/// <param name="sClassName"></param>
		/// <remarks>Note that usually, sClassName is the same as the sClass deduced from the flid.
		/// We may want an override where this argument is optional. However, it is possible to
		/// load the property only for a subclass of objects, for example, the CmPossibility_Name
		/// of all CmPersons.</remarks>
		/// ------------------------------------------------------------------------------------
		public void LoadAllOfAnOwningVectorProp(int flidTag, string sClassName)
		{
			CheckDisposed();
			LoadAllOfAnOwningVectorProp(flidTag, sClassName, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load a value for the given owning sequence or collection property into the cache for every
		/// object of the specified class. (Collections will be in an arbitrary order.)
		/// </summary>
		/// <param name="flidTag"></param>
		/// <param name="sClassName"></param>
		/// <param name="fLoadClass">Flag indicating whether or not to load the class of the
		/// CmObject. When true, then CmObject.InitExisting() will not attempt to load the
		/// other properties of the class when instantiating the object.</param>
		/// <remarks>Note that usually, sClassName is the same as the sClass deduced from the flid.
		/// We may want an override where this argument is optional. However, it is possible to
		/// load the property only for a subclass of objects, for example, the CmPossibility_Name
		/// of all CmPersons.</remarks>
		/// ------------------------------------------------------------------------------------
		public void LoadAllOfAnOwningVectorProp(int flidTag, string sClassName, bool fLoadClass)
		{
			CheckDisposed();
			LoadAllOfAnOwningVectorProp(flidTag, sClassName, null, fLoadClass);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load a value for the given owning sequence or collection property into the cache for
		/// every object of the specified class. (Collections will be in an arbitrary order.)
		/// </summary>
		/// <param name="flidTag"></param>
		/// <param name="sClassName"></param>
		/// <param name="flids">Flids of the owner to filter on</param>
		/// ------------------------------------------------------------------------------------
		public void LoadAllOfAnOwningVectorProp(int flidTag, string sClassName, int[] flids)
		{
			CheckDisposed();
			LoadAllOfAnOwningVectorProp(flidTag, sClassName, flids, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load a value for the given owning sequence or collection property into the cache for
		/// every object of the specified class. (Collections will be in an arbitrary order.)
		/// </summary>
		/// <param name="flidTag"></param>
		/// <param name="sClassName"></param>
		/// <param name="flids">Flids of the owner to filter on</param>
		/// <param name="fLoadClass">Flag indicating whether or not to load the class of the
		/// CmObject. When true, then CmObject.InitExisting() will not attempt to load the
		/// other properties of the class when instantiating the object.</param>
		/// ------------------------------------------------------------------------------------
		public void LoadAllOfAnOwningVectorProp(int flidTag, string sClassName, int[] flids,
			bool fLoadClass)
		{
			CheckDisposed();
			string sField = MetaDataCacheAccessor.GetFieldName((uint)flidTag);

			//Notice that the "left outer join" is what gives us and empty row even if there are no objects in the vector.
			//We need this, to circumvent the laziness code later.
			// Sample query:
			// select cmo.id, cmo.UpdStmp, x.id from LexEntry_ cmo
			//     left outer join CmObject x on x.owner$ = cmo.id and x.ownflid$ = 5002011 order by cmo.id, x.ownord$

			StringBuilder sQry = new StringBuilder("select cmo.id, cmo.UpdStmp, x.id");

			if (fLoadClass)
				sQry.Append(", x.class$");

			sQry.AppendFormat(" from {0}_ cmo", sClassName);
			sQry.Append(" left outer join CmObject x on x.owner$ = cmo.id and x.ownflid$ = ");
			sQry.Append(flidTag);

			if (flids != null)
			{
				Debug.Assert(flids.Length > 0);
				sQry.Append(" where cmo.ownflid$ in (");
				foreach (int flid in flids)
					sQry.AppendFormat("{0},", flid);

				// replace last , with )
				sQry[sQry.Length - 1] = ')';
			}
			sQry.Append(" order by cmo.id, x.ownord$");

			IDbColSpec dcs = DbColSpecClass.Create();
			dcs.Push((int)DbColType.koctBaseId, 0, 0, 0);
			dcs.Push((int)DbColType.koctTimeStamp, 1, 0, 0);
			dcs.Push((int)DbColType.koctObjVecOwn, 1, flidTag, 0);

			if (fLoadClass)
				dcs.Push((int)DbColType.koctInt, 3, (int)CmObjectFields.kflidCmObject_Class, 0);

			LoadData(sQry.ToString(), dcs, 0);
			Marshal.ReleaseComObject(dcs);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load all of a single writing system of able to Unicode field, plus an empty slot in
		/// the cache so that if the string is missing then laziness code will not to hit the
		/// database just to find that, lo, the string is missing.
		/// </summary>
		/// <param name="flidTag">The flid tag.</param>
		/// <param name="sClassName">Name of the s class.</param>
		/// <param name="ws">The ws.</param>
		/// ------------------------------------------------------------------------------------
		public void LoadAllOfOneWsOfAMultiUnicode(int flidTag, string sClassName, int ws)
		{
			CheckDisposed();
			string sField = MetaDataCacheAccessor.GetFieldName((uint)flidTag);
			string sClass = MetaDataCacheAccessor.GetOwnClsName((uint)flidTag);

			//Notice that the "left out or join" is what gives us and empty row even if the string is missing.
			//We need this, to circumvent the laziness code later.

			string sQry = "select x.[id], mlt.txt"
				+" from " + sClassName + " x "
				+" left outer join " + sClass + "_" + sField + " mlt "
				+" on x.[id] = mlt.obj and mlt.ws= " + ws.ToString();

			// This original does NOT work correctly. It leaves out any row which has no
			// value for alternative ws, but DOES have a value for some other ws.
			//			string sQry = "select x.[id], mlt.txt"
			//				+" from " + sClassName + " x "
			//				+" left outer join " + sClass + "_" + sField + " mlt "
			//				+" on x.[id] = mlt.obj "
			//				+" where mlt.ws= " + ws.ToString() + " or mlt.ws is null" ;

			//todo: I have not been able to get including TimeStamps to work,
			//queries that I have tried that include this information
			//also omit rows for objects where this string is missing (which defeats the purpose of this method)

			//			string sQry = "select x.[id], mlt.txt  , cmo.UpdStmp "
			//				+" from "+sClassName+" x "
			//				+" left outer join multitxt$ mlt "
			//				+" on x.[id] = mlt.obj and mlt.flid = " + flidTag.ToString()
			//				+  " join CmObject cmo on cmo.id = x.id "
			//				+" where mlt.ws= " + ws.ToString()   ;
			//
			IDbColSpec dcs = DbColSpecClass.Create();
			dcs.Push((int)DbColType.koctBaseId, 0, 0, 0);
			dcs.Push((int)DbColType.koctMltAlt, 1, flidTag, ws);
			//			dcs.Push((int)DbColType.koctTimeStamp, 1, 0, 0);


			LoadData(sQry, dcs, 0);
			Marshal.ReleaseComObject(dcs);
		}

		#endregion

		#region Outline Numbering
		/// ------------------------------------------------------------------------------------
		/// <summary>Return a string giving an outline number such as 1.2.3 based on position in
		/// the owning hierarcy.
		/// </summary>
		/// <param name='hvo'>The object for which we want an outline number.</param>
		/// <param name='flid'>The property on hvo's owner that holds hvo.</param>
		/// <param name='fFinPer'>True if you want a final period appended to the string.</param>
		/// <param name='fIncTopOwner'>True if you want to include the index number of the owner
		/// that does not come from the same field, but from a similar one (same type and
		/// destination class).</param>
		/// <returns>A System.String</returns>
		/// ------------------------------------------------------------------------------------
		public string GetOutlineNumber(int hvo, int flid, bool fFinPer, bool fIncTopOwner)
		{
			CheckDisposed();
			Debug.Assert(hvo != 0);
			Debug.Assert(flid != 0);

			if (!fIncTopOwner)
				return ((ISilDataAccess)m_odde).GetOutlineNumber(hvo, flid, fFinPer);

			string sNum = "";
			bool fFirstIteration = true;
			while (true)
			{
				int hvoOwn = ((ISilDataAccess)m_odde).get_ObjectProp(hvo,
					(int)CmObjectFields.kflidCmObject_Owner);
				if (hvoOwn == 0)
					break;
				int ihvo = -1;
				if (fFirstIteration)
				{
					// The first iteration we don't insist that it be the correct owning property.
					ihvo = ((ISilDataAccess)m_odde).GetObjIndex(hvoOwn, flid, hvo);
					fFirstIteration = false;
				}
				else
				{
					// If this object has a different owner property, treat it as not found. This saves us
					// looking in a property which may not exist for the class of the object, which in turn
					// can lead (if not to a crash) at least to a lot of unnecessary loading, particularly
					// when LoadAllOfClsss is turned on.
					int flidOwn = m_odde.get_IntProp(hvo, (int)CmObjectFields.kflidCmObject_OwnFlid);
					if (flidOwn == flid)
						ihvo = ((ISilDataAccess)m_odde).GetObjIndex(hvoOwn, flid, hvo);
				}
				if (ihvo < 0)
				{
					/*
					Id    Class          Owner$  OwnOrd$  OwnFlid$  Field                    Type  Class  DstCls
					----  -------------  ------  -------  --------  -----------------------  ----  -----  ------
					8031  LexEntry		 4043    NULL     5005001   LexDb_Entries			 25    5005   5002
					8032  LexSense       8031    1        5002011   LexEntry_Senses          27    5002   5016
					8038  LexSense       8032    1        5016003   LexSense_Senses          27    5016   5016
					*/
					int nType = m_mdc.GetFieldType((uint)flid);
					int grfcpt = 1 << nType;
					if (grfcpt == (int)FieldType.kfcptOwningSequence ||
						grfcpt == (int)FieldType.kfcptOwningCollection)
					{
						uint clidDst = m_mdc.GetDstClsId((uint)flid);
						uint clidTop = (uint)((ISilDataAccess)m_odde).get_IntProp(hvoOwn,
							(int)CmObjectFields.kflidCmObject_Class);
						FieldType fieldType = grfcpt == (int)FieldType.kfcptOwningSequence ? FieldType.kfcptOwningSequence : FieldType.kfcptOwningCollection;
						foreach (uint flid2 in DbOps.GetFieldsInClassOfType(m_mdc, clidTop, fieldType))
						{
							uint clidDst2 = m_mdc.GetDstClsId(flid2);
							if (clidDst2 != clidDst)
								continue;
							ihvo = GetObjIndex(hvoOwn, (int)flid2, hvo);
							if (ihvo >= 0)
							{
								if (sNum.Length == 0)
									sNum = string.Format("{0}", ihvo + 1);
								else
									sNum = string.Format("{0}.{1}", ihvo + 1, sNum);
								break;
							}
						}
					}
					break;
				}
				if (sNum.Length == 0)
					sNum = string.Format("{0}", ihvo + 1);
				else
					sNum = string.Format("{0}.{1}", ihvo + 1, sNum);
				// Next iteration: treat the owner from this cycle as the target object for the next.
				hvo = hvoOwn;
			}
			if (fFinPer)
				sNum += ".";
			return sNum;
		}

		#endregion

		#region Special Test Methods that really belong in a test specific subclass...
		/// <summary>
		/// This highly specialized method is called by test framework teardown methods to
		/// restore the standard test database to its original pristine condition.  This depends
		/// on the build process creating the backup file copies in a known location (eg. nant safecopyTLP).
		///
		/// NOTE: All connections to TestLangProj must be disconnected before calling this function;
		/// So, we assume that all FdoCache objects based on TestLangProj have run their Dispose()
		/// before this is called.
		/// </summary>
		public static void RestoreTestLangProj()
		{
			// Verify that we're using the standard test database.
			string sDatabase = "TestLangProj";
			string sServer = MiscUtils.LocalServerName;

			// Get the filenames of the backup files.
			RegistryKey regKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\SIL\FieldWorks");
			Debug.Assert(regKey != null);
			string path = (string)regKey.GetValue("RootCodeDir");
			regKey.Close();
			if (path[path.Length - 1] == '\\')
				path = path.Substring(0, path.Length - 1);
			path = path.Substring(0, path.LastIndexOf("\\"));
			path += "\\Output\\SampleData\\" + sDatabase;
			string sBackupMdf = path + ".mdf";
			string sBackupLog = path + "_log.ldf";

			// Connect to the master database.
			string sMdfFile = null;
			string sLogFile = null;
			SqlConnection sqlcon = null;
			SqlCommand sqlcmd = null;
			try
			{
				string cnx = string.Format("Server={0}; Database=master; User ID=FWDeveloper; " +
					"Password=careful; Pooling=false;", sServer);
				sqlcon = (SqlConnection)new SqlConnection(cnx);
				sqlcon.Open();
				sqlcmd = sqlcon.CreateCommand();

				// Get the filenames of the official TestLangProj database files.
				sqlcmd.CommandText =
					"SELECT Filename FROM sysdatabases WHERE name='TestLangProj'";
				SqlDataReader sqlrd = null;
				try
				{
					sqlrd = sqlcmd.ExecuteReader();
					if (sqlrd.Read())
						sMdfFile = sqlrd.GetString(0);
				}
				finally
				{
					if (sqlrd != null)
						sqlrd.Close();
					sqlrd = null;
				}
				Debug.Assert(sMdfFile != null && sMdfFile != "");
				sLogFile = sMdfFile.Substring(0, sMdfFile.LastIndexOf(".mdf")) + "_log.ldf";

				// Detach the TestLangProj database.
				sqlcmd.CommandText = string.Format("exec sp_detach_db {0}", sDatabase);
				sqlcmd.ExecuteNonQuery();

				// Copy the backup files over the official database files.
				System.IO.File.Copy(sBackupMdf, sMdfFile, true);
				System.IO.File.Copy(sBackupLog, sLogFile, true);

				// Attach the newly restored pristine TestLangProj database.
				sqlcmd.CommandText = string.Format("exec sp_attach_db @dbname=N'{0}', @filename1=N'{1}', @filename2=N'{2}'",
					sDatabase, sMdfFile, sLogFile);
				sqlcmd.ExecuteNonQuery();
			}
			catch (Exception e)
			{
				Console.WriteLine("Got exception in FdoCache.RestoreTestLangProj: " + e.Message);
				throw;
			}
			finally
			{
				if (sqlcmd != null)
				{
					sqlcmd.Dispose();
					sqlcmd = null;
				}
				if (sqlcon != null)
				{
					sqlcon.Close();
					sqlcon = null;
				}
			}
		}
		#endregion

		/// <summary>
		/// This is the definitive method that sets limits on how long strings can be, based on the FLID.
		/// AVOID using fixed constants like 450!
		/// </summary>
		/// <param name="flidProperty"></param>
		/// <returns></returns>
		public int MaxFieldLength(int flidProperty)
		{
			int nType = m_mdc.GetFieldType((uint)flidProperty);
			switch (nType)
			{
					// These ones should be limited
				case (int)CellarModuleDefns.kcptString:
				case (int)CellarModuleDefns.kcptMultiString:
				case (int)CellarModuleDefns.kcptUnicode:
				case (int)CellarModuleDefns.kcptMultiUnicode:
					break;
				default: // no limit
					return Int32.MaxValue;
			}
			// Look for special cases.
			// These two are special-cased shorter because they are indexed...
			if (flidProperty == (int)MoForm.MoFormTags.kflidForm ||
				flidProperty == (int)WfiWordform.WfiWordformTags.kflidForm)
			{
				return 300;
			}
			string sName = m_mdc.GetFieldName((uint)flidProperty);
			// descriptions can be long (probably should be Big variant, but...)
			// definitions can also be long (see LT-8335)
			// there's also no reason to limit custom fields, since we don't know what they'll
			// be used for.
			if (sName == "Description" || sName == "Definition" || sName.StartsWith("custom"))
				return 4000;		// our current max size in the database tables.
			return 450; // current general default.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the project name formatted suitably for displaying in a main window's title bar.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string ProjectName()
		{
			return MiscUtils.IsServerLocal(ServerName) ? DatabaseName :
				string.Format(Strings.ksProjectNameFmt, DatabaseName, ServerMachineName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the server machine (trailing \\SILFW stripped off).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string ServerMachineName
		{
			get
			{
				string server = ServerName;
				// Note Use ToLowerInvariant because lowercase of SILFW in Turkish is not silfw.
				if (server.ToLowerInvariant().EndsWith("\\silfw"))
					server = server.Remove(server.LastIndexOf('\\'), 6);
				return server;
			}
		}
	}

	#region Sync Messages
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Enum listing types of change being made for synchronization purposes. These messages are
	/// used in SyncInfo.msg to indicate the type of change that was made. The usage of
	/// SyncInfo.hvo and SyncInfo.flid varies depending on the message.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public enum SyncMsg
	{
		/// <summary>
		/// Used to indicate nothing needs synchronization.
		/// </summary>
		ksyncNothing = 0,
		/// <summary>
		/// Writing system change. Practically everything needs to be reloaded to reflect new
		/// writing system. hvo and flid are unused.
		/// </summary>
		ksyncWs,
		/// <summary>
		/// A possibility list was changed (name/abbr, ordering) and should be
		/// reloaded. hvo is list id, and flid is unused.
		/// </summary>
		ksyncPossList,
		/// <summary>
		/// A new possibility item was added. hvo is list id, flid really the hvo
		/// of the added item.
		/// </summary>
		ksyncAddPss,
		/// <summary>
		/// A possibility item was deleted. hvo is list id, flid is really the
		/// hvo of the deleted item.
		/// </summary>
		ksyncDelPss,
		/// <summary>
		/// Two possibility items were merged. hvo is list id, flid is unused.
		/// </summary>
		ksyncMergePss,
		/// <summary>
		/// Edited string. hvo is string owner, flid is string property.
		/// </summary>
		ksyncSimpleEdit,
		/// <summary>
		/// Add new major/subentry. hvo is root object of window, flid is really
		/// the hvo of the new object.
		/// </summary>
		ksyncAddEntry,
		/// <summary>
		/// Delete major/subentry. hvo is root object of window, flid is really
		/// the hvo of the new object.
		/// </summary>
		ksyncDelEntry,
		/// <summary>
		/// A major/subentry was moved. hvo is root object of window, flid is really
		/// the hvo of the moved object.
		/// </summary>
		ksyncMoveEntry,
		/// <summary>
		/// A subentry was promoted. hvo is the major object of the tool (e.g.,
		/// Notebook and flid is unused.
		/// </summary>
		ksyncPromoteEntry,
		/// <summary>
		/// Add/Modify/Delete a style. hvo and flid are unused.
		/// </summary>
		ksyncStyle,
		/// <summary>
		/// Add/Modify/Delete a custom field. hvo and flid are unused.
		/// </summary>
		ksyncCustomField,
		/// <summary>
		/// Add/Modify/Delete a user view. hvo and flid are unused.
		/// </summary>
		ksyncUserViews,
		/// <summary>
		/// Modified Page Setup information. Each window needs to have page headings
		/// reloaded. hvo = root object for window. flid is unused.
		/// </summary>
		ksyncPageSetup,
		/// <summary>
		/// Modified overlays. hvo is ??? and flid is ???
		/// </summary>
		ksyncOverlays,
		/// <summary>
		/// A Language project or major object name changed. All headings need
		/// need to be reloaded. hvo and flid are unused.
		/// </summary>
		ksyncHeadingChg,
		/// <summary>
		/// Refresh everything. hvo and flid are unused.
		/// </summary>
		ksyncFullRefresh,
		/// <summary>
		/// We have issued an undo/redo. hvo and flid are unused. At some point this
		/// should be made more powerful so it only does what is necessary.
		/// </summary>
		ksyncUndoRedo,
		/// <summary>
		/// Modified scripture due to an import. hvo and flid are unused.
		/// </summary>
		ksyncScriptureImport,
		/// <summary>
		/// Created a new scripture book. hvo and flid are used.
		/// </summary>
		ksyncScriptureNewBook,
		/// <summary>
		/// The scripture books have changed so reload the scripture control.
		/// </summary>
		ksyncReloadScriptureControl,
		/// <summary>
		/// When a book is deleted. hvo and flid are used.
		/// </summary>
		ksyncScriptureDeleteBook,
		/// <summary></summary>
		ksyncLim,
	};

	#endregion

	#region Sync Info Structure
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Structure used when accessing data from Synch$ table in database as well as used for
	/// updating various windows and caches in the current application.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public struct SyncInfo
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a SyncInfo structure.
		/// </summary>
		/// <param name="msgIn">Message indicating type of change.</param>
		/// <param name="hvoIn">Item being changed.</param>
		/// <param name="flidIn">Field being changed.</param>
		/// ------------------------------------------------------------------------------------
		public SyncInfo(SyncMsg msgIn, int hvoIn, int flidIn)
		{
			msg = msgIn;
			hvo = hvoIn;
			flid = flidIn;
		}

		/// <summary>Message indicating type of change.</summary>
		public SyncMsg msg;
		/// <summary>Item being changed.</summary>
		public int hvo;
		/// <summary>Field being changed.</summary>
		public int flid;
	};
	#endregion

	#region Object creation support class

	/// <summary>
	/// Holds information about one property of an object and one type of object we could create there.
	/// </summary>
	/// <remarks>
	/// The ClassFieldInfo struct and the ClassAndPropInfo class need to be combined.
	/// </remarks>
	public class ClassAndPropInfo
	{
		/// <summary></summary>
		public const int kposNotSet = -3; // must be negative. Good to avoid -1 and -2 which have other special values for some functions.
		/// <summary></summary>
		public uint flid; // an owning property in which we could create an object.
		/// <summary></summary>
		public string fieldName; // the text name of that property.
		/// <summary>the class that has the property</summary>
		public uint sourceClsid;
		/// <summary></summary>
		public uint signatureClsid; // a class of object we could add to that property
		/// <summary></summary>
		public string signatureClassName; // the text name of that class.
		/// <summary></summary>
		public int fieldType; // CmTypes constant kcptOwningAtom, kcptOwningCollection, kcptOwningSequence.
		// These two may be left out if the client knows where to create.
		/// <summary></summary>
		public int hvoOwner = 0; // Thing that will own the newly created object.
		/// <summary></summary>
		public int ihvoPosition = kposNotSet; // Place it will occupy in the property; or not set.
		// For a collection, this is the currently cached position.
		/// <summary></summary>
		public bool isAbstract;
		/// <summary></summary>
		public bool isVector;
		/// <summary></summary>
		public bool isReference;
		/// <summary></summary>
		public bool isBasic;
		/// <summary>is this a custom user field?</summary>
		public bool isCustom;
		/// <summary>is this a virtual field?</summary>
		public bool isVirtual;
	}

	#endregion // Object creation support classe

	#region Linked object info support class

	/// <summary>
	/// This class holds information that corresponds to one row
	/// in the #ObjInfoTbl$ temporary table, which is created (and deleted) by the
	/// GetLinkedObjects on FdoCache. That method returns a List of these objects.
	/// </summary>
	public class LinkedObjectInfo
	{
		/*
				+ "[ObjId] int not null,"
				+ "[ObjClass] int null,"
				+ "[OwnerDepth] int null default(0),"
				+ "[RelObjId] int null,"
				+ "[RelObjClass] int null,"
				+ "[RelObjField] int null,"
				+ "[RelOrder] int null,"
				+ "[RelType] int null,"
				+ "[OrdKey] varbinary(250) null default(0))";
		*/
		/// <summary>
		///
		/// </summary>
		public int ObjId;
		/// <summary>
		///
		/// </summary>
		public int ObjClass;
		/// <summary>
		///
		/// </summary>
		public int OwnerDepth;
		/// <summary>
		///
		/// </summary>
		public int RelObjId;
		/// <summary>
		///
		/// </summary>
		public int RelObjClass;
		/// <summary>
		///
		/// </summary>
		public int RelObjField;
		/// <summary>
		///
		/// </summary>
		public int RelOrder;
		/// <summary>
		///
		/// </summary>
		public int RelType;
		/// <summary>
		///
		/// </summary>
		public byte[] OrdKey; // Size limit of 250 bytes.
	}

	#endregion Linked object info support class

	#region Cache Pair, one attached to database and one purely in memory
	/// <summary>
	/// CachePair maintains a relationship between two caches, a regular FdoCache storing real data,
	/// and an ISilDataAccess, typically a VwCacheDaClass, that stores temporary data for a
	/// secondary view. As well as storing both cache objects, it stores two maps which maintain a
	/// bidirectional link between HVOs in one and those in the other.
	/// </summary>
	public class CachePair : IFWDisposable
	{
		private FdoCache m_fdoCache;
		private ISilDataAccess m_sda;
		private Dictionary<int, int> m_FdoToSda;
		private Dictionary<int, int> m_SdaToFdo;

		/// <summary>
		///
		/// </summary>
		public CachePair()
		{}

		#region IDisposable & Co. implementation
		// Region last reviewed: never

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		/// True, if the object has been disposed.
		/// </summary>
		private bool m_isDisposed = false;

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed
		{
			get { return m_isDisposed; }
		}

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~CachePair()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <summary>
		///
		/// </summary>
		/// <remarks>Must not be virtual.</remarks>
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected virtual void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_FdoToSda != null)
					m_FdoToSda.Clear();
				if (m_SdaToFdo != null)
					m_SdaToFdo.Clear();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			Marshal.ReleaseComObject(m_sda);
			m_sda = null;
			m_fdoCache = null;
			m_FdoToSda = null;
			m_SdaToFdo = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		/// <summary>
		/// Forget any previously-established relationships between objects in the main
		/// and secondary caches.
		/// </summary>
		public void ClearMaps()
		{
			CheckDisposed();
			m_SdaToFdo.Clear();
			m_FdoToSda.Clear();
		}
		/// <summary>
		///
		/// </summary>
		public FdoCache MainCache
		{
			get
			{
				CheckDisposed();
				return m_fdoCache;
			}
			set
			{
				CheckDisposed();
				if (m_fdoCache == value)
					return;

				m_fdoCache = value;
				// Forget any existing relationships.
				m_FdoToSda = new Dictionary<int, int>();
				m_SdaToFdo = new Dictionary<int, int>();
			}
		}

		/// <summary>
		///
		/// </summary>
		public ISilDataAccess DataAccess
		{
			get
			{
				CheckDisposed();
				return m_sda;
			}
			set
			{
				CheckDisposed();
				if (m_sda == value)
					return;
				if (m_sda != null)
					Marshal.ReleaseComObject(m_sda);
				m_sda = value;
				// Forget any existing relationships.
				m_FdoToSda = new Dictionary<int, int>();
				m_SdaToFdo = new Dictionary<int, int>();
			}
		}

		/// <summary>
		/// Create a new secondary cache.
		/// </summary>
		public void CreateSecCache()
		{
			CheckDisposed();
			DataAccess = VwCacheDaClass.Create();
			DataAccess.WritingSystemFactory = m_fdoCache.LanguageWritingSystemFactoryAccessor;
		}

		/// <summary>
		/// Map from secondary hvo (in the SilDataAccess) to real hvo (in the FdoCache).
		/// </summary>
		/// <param name="secHvo"></param>
		/// <returns></returns>
		public int RealHvo(int secHvo)
		{
			CheckDisposed();
			if (m_SdaToFdo.ContainsKey(secHvo))
				return m_SdaToFdo[secHvo];

			return 0;
		}

		/// <summary>
		/// Create a two-way mapping.
		/// </summary>
		/// <param name="secHvo">SilDataAccess HVO</param>
		/// <param name="realHvo">In the FDO Cache</param>
		public void Map(int secHvo, int realHvo)
		{
			CheckDisposed();
			m_SdaToFdo[secHvo] = realHvo;
			m_FdoToSda[realHvo] = secHvo;
		}

		/// <summary>
		/// Removes a two-way mapping.
		/// </summary>
		/// <param name="secHvo">SilDataAccess HVO</param>
		/// <returns><c>true</c> if the mapping was successfully removed, otherwise <c>false</c>.</returns>
		public bool RemoveSec(int secHvo)
		{
			CheckDisposed();
			int realHvo;
			if (m_SdaToFdo.TryGetValue(secHvo, out realHvo))
				m_FdoToSda.Remove(realHvo);
			return m_SdaToFdo.Remove(secHvo);
		}

		/// <summary>
		/// Removes a two-way mapping.
		/// </summary>
		/// <param name="realHvo">In the FDO Cache</param>
		/// <returns><c>true</c> if the mapping was successfully removed, otherwise <c>false</c>.</returns>
		public bool RemoveReal(int realHvo)
		{
			CheckDisposed();
			int secHvo;
			if (m_FdoToSda.TryGetValue(realHvo, out secHvo))
				m_SdaToFdo.Remove(secHvo);
			return m_FdoToSda.Remove(realHvo);
		}

		/// <summary>
		/// Map from real hvo (in the FdoCache) to secondary (in the SilDataAccess).
		/// </summary>
		/// <param name="realHvo"></param>
		/// <returns></returns>
		public int SecHvo(int realHvo)
		{
			CheckDisposed();
			if (m_FdoToSda.ContainsKey(realHvo))
				return m_FdoToSda[realHvo];

			return 0;
		}

		/// <summary>
		/// Look for a secondary-cache object that corresponds to hvoReal. If one does not already exist,
		/// create it by appending to property flidOwn of object hvoOwner.
		/// </summary>
		/// <param name="hvoReal"></param>
		/// <param name="clid"></param>
		/// <param name="hvoOwner"></param>
		/// <param name="flidOwn"></param>
		/// <returns></returns>
		public int FindOrCreateSec(int hvoReal, int clid, int hvoOwner, int flidOwn)
		{
			CheckDisposed();
			int hvoSec = 0;
			if (hvoReal != 0)
				hvoSec = SecHvo(hvoReal);
			if (hvoSec == 0)
			{
				hvoSec = m_sda.MakeNewObject(clid, hvoOwner, flidOwn, m_sda.get_VecSize(hvoOwner, flidOwn));
				if (hvoReal != 0)
					Map(hvoSec, hvoReal);
			}
			return hvoSec;
		}
		/// <summary>
		/// Look for a secondary-cache object that corresponds to hvoReal. If one does not already exist,
		/// create it by appending to property flidOwn of object hvoOwner.
		/// Set its flidName property to a string name in writing system ws.
		/// If hvoReal is zero, just create an object, but don't look for or create an association.
		/// </summary>
		/// <param name="hvoReal"></param>
		/// <param name="clid"></param>
		/// <param name="hvoOwner"></param>
		/// <param name="flidOwn"></param>
		/// <param name="flidName"></param>
		/// <param name="tss"></param>
		/// <returns></returns>
		public int FindOrCreateSec(int hvoReal, int clid, int hvoOwner, int flidOwn, int flidName, ITsString tss)
		{
			CheckDisposed();
			int hvoSec = FindOrCreateSec(hvoReal, clid, hvoOwner, flidOwn);
			m_sda.SetString(hvoSec, flidName, tss);
			return hvoSec;
		}

		/// <summary>
		/// Look for a secondary-cache object that corresponds to hvoReal. If one does not already exist,
		/// create it by appending to property flidOwn of object hvoOwner.
		/// Set its flidName property to a string name in writing system ws.
		/// </summary>
		/// <param name="hvoReal"></param>
		/// <param name="clid"></param>
		/// <param name="hvoOwner"></param>
		/// <param name="flidOwn"></param>
		/// <param name="name"></param>
		/// <param name="flidName"></param>
		/// <param name="ws"></param>
		/// <returns></returns>
		public int FindOrCreateSec(int hvoReal, int clid, int hvoOwner, int flidOwn, string name, int flidName, int ws)
		{
			CheckDisposed();
			ITsStrFactory tsf = TsStrFactoryClass.Create();
			return FindOrCreateSec(hvoReal, clid, hvoOwner, flidOwn, flidName, tsf.MakeString(name, ws));
		}
		/// <summary>
		/// Like FindOrCreateSec, except the ws is taken automaticaly as the default analysis ws of the main cache.
		/// </summary>
		/// <param name="hvoReal"></param>
		/// <param name="clid"></param>
		/// <param name="hvoOwner"></param>
		/// <param name="flidOwn"></param>
		/// <param name="name"></param>
		/// <param name="flidName"></param>
		/// <returns></returns>
		public int FindOrCreateSecAnalysis(int hvoReal, int clid, int hvoOwner, int flidOwn, string name, int flidName)
		{
			CheckDisposed();
			return FindOrCreateSec(hvoReal, clid, hvoOwner, flidOwn, name, flidName, m_fdoCache.DefaultAnalWs);
		}

		/// <summary>
		/// Like FindOrCreateSec, except the ws is taken automaticaly as the default vernacular ws of the main cache.
		/// </summary>
		/// <param name="hvoReal"></param>
		/// <param name="clid"></param>
		/// <param name="hvoOwner"></param>
		/// <param name="flidOwn"></param>
		/// <param name="name"></param>
		/// <param name="flidName"></param>
		/// <returns></returns>
		public int FindOrCreateSecVern(int hvoReal, int clid, int hvoOwner, int flidOwn, string name, int flidName)
		{
			CheckDisposed();
			return FindOrCreateSec(hvoReal, clid, hvoOwner, flidOwn, name, flidName, m_fdoCache.DefaultVernWs);
		}
	}


	#endregion Cache Pair, one attached to database and one purely in memory

	#region Interface IStoresFdoCache
	/// <summary>
	/// This interface is common to objects which can be initialized with an FdoCache.
	/// </summary>
	public interface IStoresFdoCache
	{
		/// <summary>
		/// Set the cache. This may be used on initializers which only optionally pass
		/// information on to a child object, so there is no getter.
		/// </summary>
		FdoCache Cache { set; }
	}
	#endregion

	#region UndoRedoTaskHelper class

	/// <summary>
	/// This is the main interface of the UndoRedoTaskHelper, abstracted so we can make test spys.
	/// </summary>
	public interface IUndoRedoTaskHelper: IDisposable
	{
		/// <summary>
		/// Add an action to the ActionHandler.
		/// </summary>
		/// <param name="action"></param>
		void AddAction(IUndoAction action);
	}
	/// <summary>
	/// Class to handle adding undo tasks to the undo stack with an appropriate description.
	/// Alternatively can be used to suppress a given task.
	/// </summary>
	public class UndoRedoTaskHelper : SuppressSubTasks, IUndoRedoTaskHelper
	{
		bool m_fBeginUndoTask = false;
		int m_initialUndoTaskCount = 0;
		int m_initialDepth = 0;

		/// <summary>
		/// Begin undo task and end the task during dispose.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="sUndo">Text to use with Undo</param>
		/// <param name="sRedo">Text to use with Redo</param>
		public UndoRedoTaskHelper(FdoCache cache, string sUndo, string sRedo)
			: this(cache, sUndo, sRedo, true, false)
		{
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="sUndo"></param>
		/// <param name="sRedo"></param>
		/// <param name="fBeginUndoTask">if true, begin an undo task.
		/// if false, suppress this as an undo task.</param>
		/// <param name="fInvalidateUndoActions">if the enclosed sub task can invalidate undo actions, then we want
		/// to clear our actions before proceeding.</param>
		public UndoRedoTaskHelper(FdoCache cache, string sUndo, string sRedo, bool fBeginUndoTask, bool fInvalidateUndoActions)
			: base(cache, !fBeginUndoTask, fInvalidateUndoActions)
		{
			m_fBeginUndoTask = fBeginUndoTask;
			m_initialUndoTaskCount = cache.ActionHandlerAccessor.UndoableSequenceCount;
			m_initialDepth = cache.ActionHandlerAccessor.CurrentDepth;
			if (fBeginUndoTask)
			{
				m_cache.BeginUndoTask(sUndo, sRedo);
			}
		}

		/// <summary>
		/// This is provided partly so we can override for a test spy.
		/// </summary>
		/// <param name="action"></param>
		public void AddAction(IUndoAction action)
		{
			m_cache.ActionHandlerAccessor.AddAction(action);
		}

		#region IDisposable
		/// <summary>
		/// Will EndUndoTask if it started one.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose(bool disposing)
		{
			FdoCache cache = m_cache;
			try
			{
				if (m_isDisposed)
					return;

				if (disposing)
				{
					if (m_cache != null && m_fBeginUndoTask)
					{
						// End the undo task.
						m_cache.EndUndoTask();
						m_fBeginUndoTask = false;
						Debug.Assert(m_initialDepth == cache.ActionHandlerAccessor.CurrentDepth,
							String.Format("Expected undo action count of {0} but got {1}", m_initialDepth, cache.ActionHandlerAccessor.CurrentDepth));
					}
				}
			}
			finally
			{
				base.Dispose(disposing);
			}
		}
		#endregion IDisposable
	}

	#endregion
	#region Suppress Sub Tasks helper class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Helper class to suppress sub tasks by setting the action handler to null
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class SuppressSubTasks : IFWDisposable
	{
		private IActionHandler m_oldActionHandler;
		/// <summary>
		///
		/// </summary>
		protected FdoCache m_cache;
		/// <summary>
		/// this is used in tests where we actually want to add all tasks as undoable
		/// and not suppress them.
		/// </summary>
		UndoRedoTaskHelper m_urth;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Suppress any sub tasks while this object is in scope
		/// </summary>
		/// <param name="cache">The FDO cache</param>
		/// ------------------------------------------------------------------------------------
		public SuppressSubTasks(FdoCache cache) : this(cache, true, false)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Conditionally suppress adding undo/redo actions, depending upon
		/// whether or not we're in the context of an UndoRedo task.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="fSuppressOnlyIfNotAlreadyInUndoRedoTask">if true, we'll suppress sub
		/// tasks only if we aren't already in the context of a beginUndoTask, if false, we'll
		/// start suppressing unconditionally</param>
		/// ------------------------------------------------------------------------------------
		public SuppressSubTasks(FdoCache cache, bool fSuppressOnlyIfNotAlreadyInUndoRedoTask)
			: this(cache,
				(fSuppressOnlyIfNotAlreadyInUndoRedoTask && cache.ActionHandlerAccessor != null) ?
					cache.ActionHandlerAccessor.CurrentDepth == 0 : true,
				false)
		{
		}

		/// <summary>
		/// Suppress any sub tasks while this object is in scope
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="fStartSuppressed">if true, we'll suppress subtasks as soon as this object is created.
		/// if false, we'll disable/delay suppressing subtasks</param>
		/// <param name="fInvalidateUndoActions">if the enclosed sub task can invalidate undo actions, then we want
		/// to clear our actions before proceeding.</param>
		public SuppressSubTasks(FdoCache cache, bool fStartSuppressed, bool fInvalidateUndoActions)
		{
			m_cache = cache;
			if (m_cache != null)
			{
				if (m_cache.AddAllActionsForTests && !(this is UndoRedoTaskHelper))
				{
					// SuppressSubTasks class does not make sense in context of AddAllActionsForTests.
					// in many tests, we actually want to add all the actions as undoable
					// so create an undo task to envelope the otherwise suppressed actions.
					// this should prevent cluttering top level actions in the action handler.
					m_urth = new UndoRedoTaskHelper(cache,
						"SuppressSubTasks - UndoAction", "SuppressSubTasks - RedoAction", true, false);
				}
				else
				{
					m_oldActionHandler = m_cache.ActionHandlerAccessor;
					if (fInvalidateUndoActions && m_cache.ActionHandlerAccessor != null)
						m_cache.ActionHandlerAccessor.Commit();
					if (fStartSuppressed)
						m_cache.SetActionHandler(null);
				}
			}
		}

		#region IDisposable & Co. implementation
		// Region last reviewed: never

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		/// True, if the object has been disposed.
		/// </summary>
		protected bool m_isDisposed = false;

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed
		{
			get { return m_isDisposed; }
		}

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~SuppressSubTasks()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <summary>
		///
		/// </summary>
		/// <remarks>Must not be virtual.</remarks>
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected virtual void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				if (m_cache != null && !m_cache.IsDisposed)
				{
					if (m_urth != null && !m_urth.IsDisposed)
					{
						// during cache.AddAllActionsForTests we actually created an undo/redo task, so end it.
						m_urth.Dispose();
					}
					else if (m_cache.ActionHandlerAccessor != m_oldActionHandler)
					{
						// restore our action handler.
						m_cache.SetActionHandler(m_oldActionHandler);
					}
				}
			}
			m_oldActionHandler = null;
			m_cache = null;
			m_urth = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation
	}
	#endregion

	/// <summary>
	/// UndoAction to support undoing the creation of unowned objects. This is fairly rudimentary...no checking
	/// that the database is in a state where it makes sense...mainly used for annotations.
	/// </summary>
	class UndoCreateObject : IUndoAction
	{
		private int m_hvo;
		private string m_guid;
		private int m_clsid;
		private FdoCache m_cache;

		/// <summary>
		/// make one.
		/// </summary>
		public UndoCreateObject(int clsid, int hvo, string guid, FdoCache cache)
		{
			m_clsid = clsid;
			m_hvo = hvo;
			m_guid = guid;
			m_cache = cache;
		}
		#region IUndoAction Members

		public void Commit()
		{
		}

		public bool IsDataChange()
		{
			return true;
		}

		public bool IsRedoable()
		{
			return true;
		}

		public bool Redo(bool fRefreshPending)
		{
			IOleDbCommand odc;
			m_cache.DatabaseAccessor.CreateCommand(out odc);
			string sSql = string.Format(
				"set IDENTITY_INSERT CmObject ON;exec CreateObject$ {0}, {1}, '{2}';set IDENTITY_INSERT CmObject OFF",
				m_clsid, m_hvo, m_guid);
			try
			{
				odc.ExecCommand(sSql, (int) SqlStmtType.knSqlStmtStoredProcedure);
			}
			finally
			{
				DbOps.ShutdownODC(ref odc);
			}
			ICmObject obj = CmObject.CreateFromDBObject(m_cache, m_hvo);
			(obj as CmObject).InitNewInternal();
			return true;
		}

		public bool RequiresRefresh()
		{
			return false;
		}

		public bool SuppressNotification
		{
			set {  }
		}

		public bool Undo(bool fRefreshPending)
		{
			IOleDbCommand odc = null;
			m_cache.DatabaseAccessor.CreateCommand(out odc);
			string sSql = string.Format("exec DeleteObjects '{0}'", m_hvo);
			try
			{
				odc.ExecCommand(sSql, (int)SqlStmtType.knSqlStmtStoredProcedure);
			}
			finally
			{
				DbOps.ShutdownODC(ref odc);
			}
			return true;
		}

		#endregion
	}
}
