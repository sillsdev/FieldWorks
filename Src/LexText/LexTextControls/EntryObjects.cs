// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2003' to='2004' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: EntryObjects.cs
// Responsibility: Randy Regnier
// Last reviewed:
//
// <remarks>
// Implementation of:
//		ExtantEntryInfo - Class that gets the extant entries from the DB
//			which match the given information.
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel; // for BackgroundWorker
using System.Diagnostics;	// For debugging.
using System.Data.SqlClient;
using System.Xml;

using SIL.FieldWorks.FDO;
using XCore;
using SIL.Utils;
using SIL.FieldWorks.Common.COMInterfaces;
using System.Windows.Forms;

namespace SIL.FieldWorks.LexText.Controls
{
	#region ExtantEntryInfo class
	/// <summary>
	/// Information about an entry which is used in the matching entries control.
	/// </summary>
	public class ExtantEntryInfo
	{
		public event EventHandler ExtantEntriesCompleted;

		#region Data members

		private static string s_cf;
		private static string s_lf;
		private static string s_af;
		private static string s_gl;

		private int m_id = 0;
		private string m_cf;
		private int m_cfWs;
		private string m_lf;
		private int m_lfWs;
		private string m_af;
		private int m_afWs;
		private string m_gl;
		private int m_glWs;

		private SqlConnection m_sqlCon;
		private SqlDataReader m_sqlreader;
		private BackgroundQueryRunner m_queryRunner;
		private int m_currentID;
		private FdoCache m_cache;

		#endregion Data members

		#region Properties

		/// <summary>
		/// Get the searched for Entry citation form.
		/// </summary>
		public static string SrcCitationForm
		{
			get { return s_cf; }
		}

		/// <summary>
		/// Get the searched for Entry underlying form.
		/// </summary>
		public static string SrcLexemeForm
		{
			get { return s_lf; }
		}

		/// <summary>
		/// Get the searched for allomorph form.
		/// </summary>
		public static string SrcAlternateForms
		{
			get { return s_af; }
		}

		/// <summary>
		/// Get the searched for gloss from senses.
		/// </summary>
		public static string SrcGloss
		{
			get { return s_gl; }
		}
		// ********************

		/// <summary>
		/// Get and set the Entry ID.
		/// </summary>
		public int ID
		{
			set { m_id = value; }
			get { return m_id; }
		}

		/// <summary>
		/// Get and set the Entry citation form.
		/// </summary>
		public string CitationForm
		{
			set { m_cf = value; }
			get { return m_cf; }
		}

		/// <summary>
		/// Get and set the Entry citation form writing system id.
		/// </summary>
		public int CitationFormWs
		{
			set { m_cfWs = value; }
			get { return m_cfWs; }
		}

		/// <summary>
		/// Get and set the Entry underlying form.
		/// </summary>
		public string LexemeForm
		{
			set { m_lf = value; }
			get { return m_lf; }
		}

		/// <summary>
		/// Get and set the Entry underlying form writing system id.
		/// </summary>
		public int LexemeFormWs
		{
			set { m_lfWs = value; }
			get { return m_lfWs; }
		}

		/// <summary>
		/// Get and set combination of allomorph forms.
		/// </summary>
		public string AlternateForms
		{
			set { m_af = value; }
			get { return m_af; }
		}

		/// <summary>
		/// Get and set combination of allomorphs form writing system id.
		/// It will use the first one, since there could be different ones.
		/// </summary>
		public int AlternateFormsWs
		{
			set { m_afWs = value; }
			get { return m_afWs; }
		}

		/// <summary>
		/// Get and set the combination of glosses from senses.
		/// </summary>
		public string Glosses
		{
			set { m_gl = value; }
			get { return m_gl; }
		}

		/// <summary>
		/// Get and set the combination of allomorphs form writing system id.
		/// It will use the first one, since there could be different ones.
		/// </summary>
		public int GlossesWs
		{
			set { m_glWs = value; }
			get { return m_glWs; }
		}

		#endregion Properties

		#region Methods

		/// <summary>
		/// Start a thread to get the matching entries.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="currentID"></param>
		/// <param name="wantExactMatch"></param>
		/// <param name="vernWs"></param>
		/// <param name="cf"></param>
		/// <param name="uf"></param>
		/// <param name="af"></param>
		/// <param name="analWs"></param>
		/// <param name="gl"></param>
		/// <returns></returns>
		public void StartGettingExtantEntries(FdoCache cache, int currentID,
			bool wantExactMatch,
			int vernWs, string cf, string uf, string af,
			int analWs, string gl)
		{
			Debug.WriteLine("Starting to get entries for " + cf + " and " + gl);
			m_currentID = currentID;
			m_cache = cache;
			if (cf == null || cf == String.Empty)
				cf = "!";
			if (uf == null || uf == String.Empty)
				uf = "!";
			if (af == null || af == String.Empty)
				af = "!";
			if (gl == null || gl == String.Empty)
				gl = "!";

			if (cf == "!"
				&& uf == "!"
				&& af == "!"
				&& gl == "!")
			{
				RaiseCompleted(); // invoke anything waiting for us to be done.
				return; // Nothing to search on, so quit.
			}

			s_cf = cf;
			s_lf = uf;
			s_af = af;
			s_gl = gl;

			m_sqlCon = new SqlConnection(
				string.Format("Server={0}; Database={1}; User ID=FWDeveloper;"
				+ "Password=careful; Pooling=false;", cache.ServerName, cache.DatabaseName));
			m_sqlCon.Open();
			try
			{
				SqlCommand sqlComm = m_sqlCon.CreateCommand();
				string sSql;
				if (uf != "!")
				{
					sSql =
						@"SELECT EntryId, " +
						@"ISNULL(LexicalForm, N'***') AS LexicalForm, " +
						@"LexicalFormWS, " +
						@"ISNULL(CitationForm, N'***') AS CitationForm, " +
						@"CitationFormWS, " +
						@"ISNULL(AlternateForm, N'***') AS AlternateForm, " +
						@"AlternateFormWS, " +
						@"ISNULL(Gloss, N'***') AS Gloss, " +
						@"GlossWS " +
						@"FROM fnMatchEntries(@exactMatch, @uf, @cf, @af, @gl, @wsv, @wsa, @maxSize) " +
						@"ORDER BY LexicalForm, CitationForm, AlternateForm, Gloss ";
				}
				else
				{
					sSql =
						@"SELECT EntryId, " +
						@"ISNULL(LexicalForm, N'***') AS LexicalForm, " +
						@"LexicalFormWS, " +
						@"ISNULL(CitationForm, N'***') AS CitationForm, " +
						@"CitationFormWS, " +
						@"ISNULL(AlternateForm, N'***') AS AlternateForm, " +
						@"AlternateFormWS, " +
						@"ISNULL(Gloss, N'***') AS Gloss, " +
						@"GlossWS " +
						@"FROM fnMatchEntries(@exactMatch, @uf, @cf, @af, @gl, @wsv, @wsa, @maxSize) " +
						@"ORDER BY Gloss, LexicalForm, CitationForm, AlternateForm";
				}
				sqlComm.CommandText = sSql;
				sqlComm.Parameters.AddWithValue("@exactMatch", wantExactMatch ? 1 : 0);
				sqlComm.Parameters.AddWithValue("@uf", uf);
				sqlComm.Parameters.AddWithValue("@cf", cf);
				sqlComm.Parameters.AddWithValue("@af", af);
				sqlComm.Parameters.AddWithValue("@gl", gl);
				sqlComm.Parameters.AddWithValue("@wsv", vernWs);
				sqlComm.Parameters.AddWithValue("@wsa", analWs);
				sqlComm.Parameters.AddWithValue("@maxSize", 256);	// 256 seem good to SteveMc :-)
				sqlComm.CommandTimeout = 60; // seconds timeout; this query runs slower on MSDE than SqlServer developer.
				m_queryRunner = new BackgroundQueryRunner(sqlComm);
				m_queryRunner.CommandCompleted += new EventHandler(m_queryRunner_CommandCompleted);
				Debug.WriteLine("Running the reader");
				m_queryRunner.Run();
				//sqlreader =	sqlComm.ExecuteReader(System.Data.CommandBehavior.SingleResult);
			}
			catch(Exception)
			{
				Cleanup();
				throw;
			}
		}

		public void Cleanup()
		{
			try
			{
				if (m_sqlreader != null && !m_sqlreader.IsClosed)
				{
					m_sqlreader.Close();
					m_sqlreader = null;
				}
			}
			finally
			{
				try
				{
					if (m_sqlCon != null)
					{
						m_sqlCon.Close();
						m_sqlCon = null;
					}
				}
				catch
				{
				}
			}
		}

		void m_queryRunner_CommandCompleted(object sender, EventArgs e)
		{
			Exception ex = m_queryRunner.Exception;
			if (ex == null)
			{
				m_sqlreader = m_queryRunner.Result;
			}
			else
			{
				// The SqlException with number -2 is the undocumented exception that actually
				// happens if it times out!
				if (ex is System.TimeoutException || ex is System.Runtime.Remoting.RemotingTimeoutException
					|| ex is System.ServiceProcess.TimeoutException
					|| (ex is SqlException && (ex as SqlException).Number == -2))
				{
					MessageBox.Show(SIL.FieldWorks.LexText.Controls.LexTextControls.ksRetrievalTimedOut);
				}
				else
				{
					throw new Exception("Retrieve matching entries query failed", ex);
				}
			}
			RaiseCompleted();
		}

		void RaiseCompleted()
		{
			Debug.WriteLine("Query completed");
			if (ExtantEntriesCompleted != null)
				ExtantEntriesCompleted(this, new EventArgs());
		}

		public List<ExtantEntryInfo> Results()
		{
			List<ExtantEntryInfo> al = new List<ExtantEntryInfo>();
			Debug.WriteLine("Getting results");
			if (m_sqlreader == null || m_sqlreader.IsClosed)
			{
				// Cancelled, or didn't get any results, whatever
				return al;
			}
			/* The results have these columns:
				EntryID int,						-- 1/0
				UFTxt nvarchar(4000) default '***',	-- 2/1
				UFWs int,							-- 3/2
				CFTxt nvarchar(4000) default '***',	-- 4/3
				CFWs int,							-- 5/4
				AFTxt nvarchar(4000) default '***',	-- 6/5
				AFWs int,							-- 7/6
				GLTxt nvarchar(4000) default '***',	-- 8/7
				GLWs int							-- 9/8
				*/
			int uiWs = m_cache.DefaultUserWs;
			while (true)
			{
				try
				{
					if (!m_sqlreader.Read())
						break;
				}
				catch (SqlException ex)
				{
					Debug.WriteLine(ex.Message);
					continue;
				}
				int id = m_sqlreader.GetInt32(0);
				if (id != m_currentID)
				{
					ExtantEntryInfo eei = new ExtantEntryInfo();
					string ksStars = SIL.FieldWorks.LexText.Controls.LexTextControls.ksStars;
					// NB: The dummy ID in col 0 is not used here.
					eei.ID = id;
					eei.LexemeForm = m_sqlreader.GetString(1);
					if (eei.LexemeForm == ksStars)
					{
						eei.LexemeForm = String.Empty;
						eei.LexemeFormWs = uiWs;
					}
					else
						eei.LexemeFormWs = m_sqlreader.GetInt32(2);
					eei.CitationForm = m_sqlreader.GetString(3);
					if (eei.CitationForm == ksStars)
					{
						eei.CitationForm = String.Empty;
						eei.CitationFormWs = uiWs;
					}
					else
						eei.CitationFormWs = m_sqlreader.GetInt32(4);
					eei.AlternateForms = m_sqlreader.GetString(5);
					if (eei.AlternateForms == ksStars)
					{
						eei.AlternateForms = String.Empty;
						eei.AlternateFormsWs = uiWs;
					}
					else
						eei.AlternateFormsWs = m_sqlreader.GetInt32(6);
					eei.Glosses = m_sqlreader.GetString(7);
					if (eei.Glosses == ksStars)
					{
						eei.Glosses = String.Empty;
						eei.GlossesWs = uiWs;
					}
					else
						eei.GlossesWs = m_sqlreader.GetInt32(8);
					al.Add(eei);
				}
			}
			Debug.WriteLine("Got results");
			return al;
		}

		#endregion Methods

		// was internal
		public void Cancel()
		{
			if (m_queryRunner != null)
				m_queryRunner.Cancel();
		}
	}
	#endregion ExtantEntryInfo class

	/// <summary>
	/// This class executes an SqlComm in the background, raising CommandCompleted when it is done.
	/// </summary>
	public class BackgroundQueryRunner
	{
		BackgroundWorker m_worker;
		SqlCommand m_sqlComm;
		SqlDataReader m_reader; // the result
		bool m_fCancelled;
		Exception m_exception;

		public event EventHandler CommandCompleted;

		public BackgroundQueryRunner(SqlCommand sqlComm)
		{
			m_sqlComm = sqlComm;
		}

		// call this after hooking the CommandCompleted event
		public void Run()
		{
			m_worker = new BackgroundWorker();
			m_worker.WorkerSupportsCancellation = true;
			m_worker.DoWork += new DoWorkEventHandler(m_worker_DoWork);
			m_worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(m_worker_RunWorkerCompleted);
			m_worker.RunWorkerAsync(m_sqlComm);
		}

		// Attempt to cancel the operation.
		public void Cancel()
		{
			m_worker.CancelAsync();
			m_fCancelled = true;
		}

		void m_worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			//Debug.WriteLine("Worker completed");
			m_reader = e.Result as SqlDataReader;
			if (CommandCompleted != null)
				CommandCompleted(this, new EventArgs());
		}

		void m_worker_DoWork(object sender, DoWorkEventArgs e)
		{
			//Debug.WriteLine("starting to work in worker thread");
			SqlCommand sqlComm = e.Argument as SqlCommand;
			try
			{
				e.Result = sqlComm.ExecuteReader(System.Data.CommandBehavior.SingleResult);
			}
			catch (Exception ex)
			{
				if (m_fCancelled)	// for now
					return; // ignore anything that went wrong in a cancelled query
				m_exception = ex;
			}
			//Debug.WriteLine("finished work in worker thread");
		}

		// Get the result (typically in the CommandCompleted handler)
		public SqlDataReader Result
		{
			get { return m_reader; }
		}

		/// <summary>
		/// Get any exception that occurred in the other thread.
		/// </summary>
		public Exception Exception
		{
			get { return m_exception; }
		}
	}


	#region ExtantReversalIndexEntryInfo class
	/// <summary>
	/// Information about an entry which is used in the matching entries control.
	/// </summary>
	public class ExtantReversalIndexEntryInfo
	{
		#region Data members

		private static string s_form;

		private int m_id = 0;
		private string m_form;
		private int m_ws;

		#endregion Data members

		#region Properties

		/// <summary>
		/// Get the searched for Entry form.
		/// </summary>
		public static string SrcForm
		{
			get { return s_form; }
		}

		/// <summary>
		/// Get and set the Entry ID.
		/// </summary>
		public int ID
		{
			set { m_id = value; }
			get { return m_id; }
		}

		/// <summary>
		/// Get and set the Entry form.
		/// </summary>
		public string Form
		{
			set { m_form = value; }
			get { return m_form; }
		}

		/// <summary>
		/// Get and set the Entry form writing system id.
		/// </summary>
		public int Ws
		{
			set { m_ws = value; }
			get { return m_ws; }
		}

		#endregion Properties

		#region Methods

		/// <summary>
		/// Get the matching entries.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="currentID"></param>
		/// <param name="form"></param>
		/// <param name="ws"></param>
		/// <returns></returns>
		public static List<ExtantReversalIndexEntryInfo> ExtantEntries(FdoCache cache, string form, int ws)
		{
			List<ExtantReversalIndexEntryInfo> al = new List<ExtantReversalIndexEntryInfo>();
			if (form == null || form == String.Empty)
				return al;

			s_form = form;

			SqlConnection sqlCon = new SqlConnection(
				string.Format("Server={0}; Database={1}; User ID=FWDeveloper;"
				+ "Password=careful; Pooling=false;", cache.ServerName, cache.DatabaseName));
			sqlCon.Open();
			SqlDataReader sqlreader = null;
			try
			{
				SqlCommand sqlComm = sqlCon.CreateCommand();
				//sqlComm.CommandText = "SELECT Id, Form"
				//    + " FROM ReversalIndexEntry_"
				//    + " WHERE LOWER(RTRIM(LTRIM(Form))) LIKE LOWER(RTRIM(LTRIM(@form))) + '%'"
				//    + "		AND WritingSystem = @ws"
				//    + "	ORDER BY Id, Form";
				sqlComm.CommandText = "SELECT Obj, Txt" +
					" FROM ReversalIndexEntry_ReversalForm" +
					" WHERE LOWER(RTRIM(LTRIM(Txt))) LIKE LOWER(RTRIM(LTRIM(@form))) + '%' AND Ws=@ws" +
					" ORDER BY Obj, Txt";
				sqlComm.Parameters.AddWithValue("@form", form);
				sqlComm.Parameters.AddWithValue("@ws", ws.ToString());
				sqlreader =	sqlComm.ExecuteReader(System.Data.CommandBehavior.Default);
				while (sqlreader.Read())
				{
					ExtantReversalIndexEntryInfo eriei = new ExtantReversalIndexEntryInfo();
					eriei.ID = sqlreader.GetInt32(0);
					eriei.Form = sqlreader.GetString(1);
					al.Add(eriei);
				}
			}
			catch (Exception err)
			{
				Debug.WriteLine(err.Message);
			}
			finally
			{
				try
				{
					if (sqlreader != null && !sqlreader.IsClosed)
						sqlreader.Close();
				}
				finally
				{
					try
					{
						sqlCon.Close();
					}
					catch
					{
					}
				}
			}

			return al;
		}

		#endregion Methods
	}
	#endregion ExtantReversalIndexEntryInfo class


	#region ExtantWfiWordformInfo class
	/// <summary>
	/// Information about an wordform which is used in the matching wordform control.
	/// </summary>
	public class ExtantWfiWordformInfo
	{
		#region Data members

		private static string s_form;

		private int m_id = 0;
		private string m_form;
		private int m_ws;

		#endregion Data members

		#region Properties

		/// <summary>
		/// Get the searched for wordform form.
		/// </summary>
		public static string SrcForm
		{
			get { return s_form; }
		}

		/// <summary>
		/// Get and set the Entry ID.
		/// </summary>
		public int ID
		{
			set { m_id = value; }
			get { return m_id; }
		}

		/// <summary>
		/// Get and set the wordform form.
		/// </summary>
		public string Form
		{
			set { m_form = value; }
			get { return m_form; }
		}

		/// <summary>
		/// Get and set the wordform form writing system id.
		/// </summary>
		public int Ws
		{
			set { m_ws = value; }
			get { return m_ws; }
		}

		#endregion Properties

		#region Methods

		/// <summary>
		/// Get the matching entries.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="currentID"></param>
		/// <param name="form"></param>
		/// <param name="ws"></param>
		/// <returns></returns>
		public static List<ExtantWfiWordformInfo> ExtantWordformInfo(FdoCache cache, string form, int ws)
		{
			List<ExtantWfiWordformInfo> al = new List<ExtantWfiWordformInfo>();
			if (form == null || form == String.Empty)
				return al;

			s_form = form;

			SqlConnection sqlCon = new SqlConnection(
				string.Format("Server={0}; Database={1}; User ID=FWDeveloper;"
				+ "Password=careful; Pooling=false;", cache.ServerName, cache.DatabaseName));
			sqlCon.Open();
			SqlDataReader sqlreader = null;
			try
			{
				SqlCommand sqlComm = sqlCon.CreateCommand();
				sqlComm.CommandText = "SELECT Obj, Txt"
					+ " FROM WfiWordform_Form"
					+ " WHERE LOWER(RTRIM(LTRIM(Txt))) LIKE LOWER(RTRIM(LTRIM(@form))) + '%'"
					+ "		AND Ws = @ws"
					+ "	ORDER BY Obj, Txt";
				sqlComm.Parameters.AddWithValue("@form", form);
				sqlComm.Parameters.AddWithValue("@ws", ws.ToString());
				sqlreader =	sqlComm.ExecuteReader(System.Data.CommandBehavior.Default);
				while (sqlreader.Read())
				{
					ExtantWfiWordformInfo ewi = new ExtantWfiWordformInfo();
					ewi.ID = sqlreader.GetInt32(0);
					ewi.Form = sqlreader.GetString(1);
					al.Add(ewi);
				}
			}
			catch (Exception err)
			{
				Debug.WriteLine(err.Message);
			}
			finally
			{
				try
				{
					if (sqlreader != null && !sqlreader.IsClosed)
						sqlreader.Close();
				}
				finally
				{
					try
					{
						sqlCon.Close();
					}
					catch
					{
					}
				}
			}

			return al;
		}

		#endregion Methods
	}
	#endregion ExtantWfiWordformInfo class

	#region WindowParams class
	/// <summary>
	/// A class that allows for parameters to be passed to the Go dialog form the client.
	/// Currently, this only works for XCore messages, not the IText entry point.
	/// </summary>
	public class WindowParams
	{
		#region Data members

		/// <summary>
		/// Window title.
		/// </summary>
		public string m_title;
		/// <summary>
		/// Text in label to the left of the form edit box.
		/// </summary>
		public string m_label;
		/// <summary>
		/// Text on OK button.
		/// </summary>
		public string m_btnText;

		#endregion Data members
	}
	#endregion WindowParams class

	#region LObject class
	/// <summary>
	/// Abstract base class for LEnty and LSense,
	/// which are 'cheap' versions of the corresponding FDO classes.
	/// </summary>
	internal abstract class LObject
	{
		protected int m_hvo;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="hvo">Database ID of the object.</param>
		public LObject(int hvo)
		{
			m_hvo = hvo;
		}

		public int HVO
		{
			get { return m_hvo; }
		}
	}
	#endregion LObject class

	#region LEntry class
	/// <summary>
	/// Cheapo version of the FDO LexEntry object.
	/// </summary>
	internal class LEntry : LObject
	{
		#region Data members

		private string m_displayName;
		private int m_refProperty;
		private List<LAllomorph> m_alAlternateForms;
		private List<LSense> m_alSenses;
		private int m_type;

		#endregion Data members

		#region Properties

		public int Type
		{
			get { return m_type; }
			set { m_type = value; }
		}

		public int ReferenceProperty
		{
			get { return m_refProperty; }
			set { m_refProperty = value; }
		}

		public string DisplayName
		{
			get
			{
				return m_displayName;
			}
		}

		public List<LSense> Senses
		{
			get { return m_alSenses; }
		}

		public List<LAllomorph> AlternateForms
		{
			get { return m_alAlternateForms; }
		}

		#endregion Properties

		#region Construction & initialization

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="hvo">Database ID of the entry.</param>
		/// <param name="displayName">Display string of the entry.</param>
		public LEntry(int hvo, string displayName) : base(hvo)
		{
			m_displayName = displayName;
			m_alAlternateForms = new List<LAllomorph>();
			m_alSenses = new List<LSense>();
		}
		#endregion Construction & initialization

		#region Other methods

		public void AddAllomorph(LAllomorph allomorph)
		{
			m_alAlternateForms.Add(allomorph);
		}

		public void AddSense(LSense sense)
		{
			m_alSenses.Add(sense);
		}

		public override string ToString()
		{
			return m_displayName;
		}

		#endregion  Other methods
	}
	#endregion LEntry class

	#region LSense class
	/// <summary>
	/// Cheapo version of the FDO LexSense object.
	/// </summary>
	internal class LSense : LObject
	{
		#region Data members

		private string m_senseNum;
		private int m_status;
		private int m_senseType;
		private List<int> m_anthroCodes;
		private List<int> m_domainTypes;
		private List<int> m_usageTypes;
		private List<int> m_thesaurusItems;
		private List<int> m_semanticDomains;

		#endregion Data members

		#region Properties

		public string SenseNumber
		{
			get { return m_senseNum; }
		}

		public int SenseType
		{
			get { return m_senseType; }
			set { m_senseType = value; }
		}

		public int Status
		{
			get { return m_status; }
			set { m_status = value; }
		}

		public List<int> AnthroCodes
		{
			get { return m_anthroCodes; }
		}

		public List<int> DomainTypes
		{
			get { return m_domainTypes; }
		}

		public List<int> UsageTypes
		{
			get { return m_usageTypes; }
		}

		public List<int> ThesaurusItems
		{
			get { return m_thesaurusItems; }
		}

		public List<int> SemanticDomains
		{
			get { return m_semanticDomains; }
		}

		#endregion Properties

		#region Construction & initialization

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="hvo">Database ID of the object.</param>
		/// <param name="senseNum">Sense number.</param>
		/// <param name="gloss">Gloss or definition(?) of sense.</param>
		public LSense(int hvo, string senseNum) : base(hvo)
		{
			m_senseNum = senseNum;
			m_anthroCodes = new List<int>();
			m_domainTypes = new List<int>();
			m_usageTypes = new List<int>();
			m_thesaurusItems = new List<int>();
			m_semanticDomains = new List<int>();
		}

		#endregion Construction & initialization
	}
	#endregion LSense class

	#region LAllomorph class
	/// <summary>
	/// Cheapo version of the FDO MoForm object.
	/// </summary>
	internal class LAllomorph : LObject, ITssValue
	{
		#region Data members

		private int m_type;
		private ITsString m_form;

		#endregion Data members

		#region Properties

		public int Type
		{
			get { return m_type; }
		}

		#endregion Properties

		#region Construction & initialization

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="hvo">Database ID of the object.</param>
		/// <param name="senseNum">MoMorphType for the class, or 0, if not known.</param>
		public LAllomorph(int hvo, int type) : base(hvo)
		{
			m_type = type;
			m_form = null;
		}

		public LAllomorph(IMoForm allo) : base(allo.Hvo)
		{
			m_type = allo.ClassID;
			m_form = allo.Form.BestVernacularAlternative;
		}

		#endregion Construction & initialization

		public override string ToString()
		{
			return (m_form == null || m_form.Text == null) ? m_hvo.ToString() : m_form.Text;
		}

		#region ITssValue Members

		/// <summary>
		/// Implementing this allows the fw combo box to do a better job of displaying items.
		/// </summary>
		public ITsString AsTss
		{
			get { return m_form; }
		}

		#endregion
	}
	#endregion LAllomorph class

	#region LMsa class
	/// <summary>
	/// Cheapo version of the FDO MoForm object.
	/// </summary>
	internal class LMsa : LObject
	{
		#region Data members

		private int m_type;
		private string m_name;

		#endregion Data members

		#region Properties

		#endregion Properties

		#region Construction & initialization

		public LMsa(IMoMorphSynAnalysis msa) : base(msa.Hvo)
		{
			m_type = msa.ClassID;
			m_name = msa.InterlinearName;
		}

		public override string ToString()
		{
			return (m_name == null) ? m_hvo.ToString() : m_name;
		}

		#endregion Construction & initialization
	}
	#endregion LMsa class

#if false
	#region EntryWrapper class
	/// <summary>
	/// Summary description for EntryWrapper.
	/// </summary>
	internal abstract class EntryWrapper
	{
		protected LEntry m_entry;
		protected string m_displayName;

		public LEntry Entry
		{
			get { return m_entry; }
		}

		public EntryWrapper(LEntry entry, string name)
		{
			m_entry = entry;
			m_displayName = name;
		}

		/// <summary>
		/// Returns a String that represents the current entry.
		/// </summary>
		/// <returns>A String that represents the current Object.</returns>
		public override string ToString()
		{
			return m_displayName;
		}
	}
	#endregion EntryWrapper class

	#region EntryAsLexicalForm class
	internal class EntryAsLexicalForm : EntryWrapper
	{
		public EntryAsLexicalForm(LEntry entry, string form) : base(entry, form)
		{
			if (entry.HomographNumber.Length > 0)
				m_displayName += "_" + entry.HomographNumber;
		}
	}
	#endregion EntryAsLexicalForm class

	#region EntryAsCitationForm class
	internal class EntryAsCitationForm : EntryWrapper
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="entry">Entry which has the citation form.</param>
		public EntryAsCitationForm(LEntry entry) : base(entry, entry.CitationForm)
		{ }
	}
	#endregion EntryAsCitationForm class

	#region EntryAsLexemeForm class
	internal class EntryAsLexemeForm : EntryWrapper
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="entry">Entry which has the underlying form.</param>
		public EntryAsLexemeForm(LEntry entry) : base(entry, entry.UnderlyingForm)
		{ }
	}
	#endregion EntryAsLexemeForm class

	#region EntryAsGloss class
	internal class EntryAsGloss : EntryWrapper
	{
		private LSense m_sense;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="entry">Entry that ultimately owns the given sense.</param>
		/// <param name="sense">Sense which owns the actual gloss.</param>
		public EntryAsGloss(LEntry entry, LSense sense) : base(entry, sense.Gloss)
		{
			m_sense = sense;
		}
	}
	#endregion EntryAsGloss class
#endif
}
