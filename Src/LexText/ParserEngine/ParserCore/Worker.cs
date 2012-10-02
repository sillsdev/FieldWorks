// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ParserWorker.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
//  The name here, "worker" would lead one to think that this is the
//	class which is the top of the heap of the worker thread.
//	However, it is actually the "Scheduler" class which controls the thread and calls this.
// </remarks>
// --------------------------------------------------------------------------------------------
/*

throws exception:  * One way I recall is that they would create an inflectional template, but not put anything in it yet (i.e. no slots at all).
 * This causes XAmple to die because it produces a PC-PATR load error.
 * This could be fixed, of course, in the XSLT that generates the grammar file.
 * This one's on my TODO list (I've got the sticky note from Dallas)...


no exception: Try an adhoc prohibition with only one item in it

no exception: Create a compound with neither member specified or only one specified.

no exception:  Create an allomorph with an environment that is ill-formed.  (Presumably this will result in the same problem as breaking an environment for an existing allomorph.)

no exception: Create an infl affix slot with no affixes in it and then use this slot in a template (though this just might not cause the parser to fail - it would just be useless!).

*/
using System;
using System.Diagnostics;
using System.Data.SqlClient;
using System.Text;
using System.Xml;

using SIL.FieldWorks.Common.Utils;
using CodeProject.ReiMiyasaka;

using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.WordWorks.Parser
{
	/// <summary>
	/// Summary description for ParserWorker.
	/// </summary>
	internal abstract class ParserWorker : IFWDisposable
	{
		internal struct AgentInfo
		{
			public string m_name;
			public bool m_isHuman;
			public string m_version;
		}
		private SqlConnection m_connection;
		private string m_LangProject;
		private TaskUpdateEventHandler m_taskUpdateHandler;
		private int m_vernacularWS;
		private ParseFiler m_parserFiler;
		private AgentInfo m_agentInfo;
		private long m_ticksParser;
		private long m_ticksFiler;
		private int m_iNumberOfWordForms;
		private M3ParserModelRetriever m_retriever = null;

		protected string m_database;
		protected TraceSwitch tracingSwitch = new TraceSwitch("ParserCore.TracingSwitch", "Just regular tracking", "Off");

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="DummyParser"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public ParserWorker(SqlConnection connection, string database, string LangProject, TaskUpdateEventHandler handler,
			string parserName, string parserVersion)
		{
			Debug.Assert(connection != null && connection.State == System.Data.ConnectionState.Open);

			m_LangProject = LangProject;
			m_database = database;
			m_taskUpdateHandler = handler;
			Trace.WriteLineIf(tracingSwitch.TraceInfo, "ParserWorker(): CurrentThreadId = " + Win32.GetCurrentThreadId().ToString());
			// Don't create the XAmpleWrapper yet because we're still on the UI thread.  If we
			// create it now, the apartment thread it uses will be the UI thread, which is not
			// what we want!
			//CreateXAmpleWrapper();
			m_agentInfo.m_isHuman = false;
			m_agentInfo.m_name = parserName;
			m_agentInfo.m_version = parserVersion;
			m_connection = connection;
			m_parserFiler = new ParseFiler(m_connection, AnalyzingAgentId);
			try
			{
				SqlCommand command = m_connection.CreateCommand();
				command.CommandText = "select top 1 Dst\n"
					+ "from LangProject_CurVernWss\n"
					+ "order by Ord\n";
				m_vernacularWS = (int)command.ExecuteScalar();
			}
			catch (Exception error)
			{
				throw new ApplicationException("Error while getting the default vernacular writing system.", error);
			}
			m_iNumberOfWordForms = GetWfiSize();
			m_ticksParser = 0L;
			m_ticksFiler = 0L;
		}

		internal abstract void InitParser();

		private int GetWfiSize()
		{
			try
			{
				SqlCommand command = m_connection.CreateCommand();
				command.CommandText = "select count(*) from WfiWordform";
				return (int)command.ExecuteScalar();
			}
			catch (Exception error)
			{
				throw new ApplicationException("Error while getting word form innventory count.", error);
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
		~ParserWorker()
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
		private void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				Trace.WriteLineIf(tracingSwitch.TraceInfo, "Total number of wordforms = " + m_iNumberOfWordForms.ToString());
				Trace.WriteLineIf(tracingSwitch.TraceInfo, "Total time for XAmple parser = " + m_ticksParser.ToString());

				if (m_iNumberOfWordForms != 0)
				{
					long lAvg = m_ticksParser/m_iNumberOfWordForms;
					Trace.WriteLineIf(tracingSwitch.TraceInfo, "Average time for XAmple parser = " + lAvg.ToString());
				}

				Trace.WriteLineIf(tracingSwitch.TraceInfo, "Total time for parser filer = " + m_ticksFiler);

				if (m_iNumberOfWordForms != 0)
				{
					long lAvg = m_ticksFiler/m_iNumberOfWordForms;
					Trace.WriteLineIf(tracingSwitch.TraceInfo, "Average time for parser filer = " + lAvg.ToString());
				}

				// Dispose managed resources here.
				if (m_parserFiler != null)
					m_parserFiler.Dispose();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_taskUpdateHandler = null;
			m_parserFiler = null;
			m_connection = null; // Client needs to close and Dispose the connection, since it gave it to us.
			CleanupParser();
			m_LangProject = null;
			m_database = null;
			m_isDisposed = true;
		}

		protected abstract void CleanupParser();

		#endregion IDisposable & Co. implementation

		private string GetConnectionString(string server, string database)
		{
			return  "Server=" + server
				+ "; Database=" + database
				+ "; User ID=FWDeveloper;"
				+ "Password=careful";
		}

		private int AnalyzingAgentId
		{
			get
			{
				int agentId = 0;
				try
				{
					SqlCommand command = m_connection.CreateCommand();
					command.CommandType = System.Data.CommandType.Text;
					command.CommandText = String.Format("exec FindOrCreateCmAgent '{0}', {1}, '{2}'",
						m_agentInfo.m_name, (m_agentInfo.m_isHuman ? 1 : 0), m_agentInfo.m_version);
					agentId = (int)command.ExecuteScalar();
				}
				catch
				{
					agentId = 0;
				}
				return agentId;
			}
		}

		/// <summary>
		/// Return the Windows Id of the thread recorded just before parsing a word.  This could
		/// be useful if the user decides to stop the parser while we're parsing a word.
		/// </summary>
		internal abstract int ThreadId
		{
			get;
		}

		protected abstract string ParseWord(string form, int hvoWordform);
		protected abstract string TraceWord(string form, string selectTraceMorphs);

		private string GetOneWordformResult(int hvoWordform, string form)
		{
			Debug.Assert(hvoWordform > 0, "Wordform ID must be greater than zero.");
			Debug.Assert(form != null, "Wordform form must not be null.");

			try
			{
				Trace.WriteLineIf(tracingSwitch.TraceInfo, "GetOneWordformResult(): CurrentThreadId = " + Win32.GetCurrentThreadId().ToString());
				DateTime startTime = DateTime.Now;
				//Debug.WriteLine("Begin parsing wordform " + form);
				string results = ParseWord(Icu.Normalize(form, Icu.UNormalizationMode.UNORM_NFD), hvoWordform);
				//Debug.WriteLine("After parsing wordform " + form);
				long ttlTicks = DateTime.Now.Ticks - startTime.Ticks;
				m_ticksParser += ttlTicks;
				DebugMsg("ParseWord("+form+") took : " + ttlTicks.ToString());
				return Icu.Normalize(results, Icu.UNormalizationMode.UNORM_NFD);
			}
			catch (Exception error)
			{
				Trace.WriteLineIf(tracingSwitch.TraceError, "The word '"
					+ form
					+ "', id='"
					+ hvoWordform.ToString()
					+ "' failed to parse. error was: "
					+ error.Message);
				//might as well keep going.
				//TODO: create an problem object since we could not parse this word.
				throw new ApplicationException("Error while parsing '" + form + "'.", error);
			}
		}

		/// <summary>
		/// Try parsing a wordform, optionally getting a trace of the parse
		/// </summary>
		/// <param name="sForm">the word form to parse</param>
		/// <param name="fDoTrace">whether or not to trace the parse</param>
		/// <param name="sSelectTraceMorphs">list of msa hvos to limit trace to </param>
		internal void TryAWord(string sForm, bool fDoTrace, string sSelectTraceMorphs)
		{
			CheckDisposed();

			if (sForm == null)
				throw new ArgumentNullException("sForm", "TryAWord cannot trace a Null string.");
			if (sForm == String.Empty)
				throw new ArgumentException("Can't try a word with no content.", "sForm");

			using (TaskReport task = new TaskReport(
				String.Format(ParserCoreStrings.ksTraceWordformX, sForm),
				m_taskUpdateHandler))
			{
				try
				{
					string normForm = Icu.Normalize(sForm, Icu.UNormalizationMode.UNORM_NFD);
					string result = null;
					if (fDoTrace)
					{
						//Debug.WriteLine("Begin tracing wordform " + sForm);
						result = TraceWord(normForm, sSelectTraceMorphs);
						//Debug.WriteLine("After tacing wordform " + sForm);
						//Debug.WriteLine("Result of trace: " + task.Details);
					}
					else
						result = ParseWord(normForm, 0);
					task.Details = Icu.Normalize(result, Icu.UNormalizationMode.UNORM_NFD);
					return;
				}
				catch (Exception error)
				{
					Trace.WriteLineIf(tracingSwitch.TraceError, "The word '"
						+ sForm
						+ "' failed to parse. error was: "
						+ error.Message);
					task.EncounteredError(null);	// Don't want to show message box in addition to yellow crash box!
					//might as well keep going.
					//TODO: create an problem object since we could not parse this word.
					throw new ApplicationException("Error while parsing '" + sForm + "'.",error);
				}
			}
		}

		internal void UpdateWordform(int hvoWordform)
		{
			CheckDisposed();

			//the WFI DLL, created with FieldWorks COM code, can only be accessed by an STA
			// REVIEW JohnH(RandyR): Is this still relevant, now that ParseFiler isn't in its own DLL?
			//Debug.Assert( Thread.CurrentThread.ApartmentState == ApartmentState.STA, "Calling thread must set the apartment state to STA");
			uint uiCRCWordform;
			string form = GetWordformStringAndCRC(hvoWordform, out uiCRCWordform);
			// 'form' will now be null, if it could not find the wordform for whatever reason.
			// uiCRCWordform will also now be 0, if 'form' is null.
			// Cf. LT-7203 for how it could be null.
			if (form == null)
				return;

			TaskReport task = new TaskReport(String.Format(ParserCoreStrings.ksUpdateX, form),
				m_taskUpdateHandler);
			using (task)
			{
				Debug.Assert(m_parserFiler != null);
				string result="";
				try
				{
					string sResult = GetOneWordformResult(hvoWordform, form); // GetOneWordformResult can throw an exception
					//Debug.WriteLine("ParserWorker: Ample result = " + sAmpResult);
					uint uiCRC = CrcStream.GetCrc(sResult);
					if (uiCRCWordform != uiCRC)
					{
						StringBuilder sb = new StringBuilder(); // use StringBuilder for efficiency
						sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" ?>\n");
						//  REMOVED THIS FROM THE SAMPLE DOC: xmlns:xsi=\"http://www.w3.org/2000/10/XMLSchema-instance\" xsi:noNamespaceSchemaLocation=\"C:\ww\code\SampleData\XMLFieldWorksParse.xsd">
						sb.Append("  <AnalyzingAgentResults>\n<AnalyzingAgent name=\""
							+ m_agentInfo.m_name
							+ "\" human=\""
							+ m_agentInfo.m_isHuman.ToString()
							+ "\" version=\""
							+ m_agentInfo.m_version
							+ "\">\n");
						sb.Append("     <StateInformation />\n</AnalyzingAgent>\n<WordSet>\n");
						sb.Append(sResult);
						sb.Append("</WordSet>\n</AnalyzingAgentResults>\n");
						result = sb.ToString();
						DateTime startTime = DateTime.Now;
						m_parserFiler.ProcessParse(result); // ProcessParse can throw exceptions.
						long ttlTicks = DateTime.Now.Ticks - startTime.Ticks;
						m_ticksFiler += ttlTicks;
						Trace.WriteLineIf(tracingSwitch.TraceInfo, "parser filer(" + form + ") took : " + ttlTicks.ToString());
						SetChecksum(hvoWordform, uiCRC);
					}
				}
				catch (Exception error)
				{
					DebugMsg(error.Message);
					task.NotificationMessage = error.Message;
					throw;
				}
			}
		}

		private void SetChecksum(int hvo, uint uiCRC)
		{
			SqlCommand command = m_connection.CreateCommand();
			command.CommandType = System.Data.CommandType.Text;

			int iCRC = (int)uiCRC;
			command.CommandText = string.Format("update wfiwordform set checksum={0} where Id={1}",
				iCRC, hvo);
			command.ExecuteScalar();
		}

		internal TimeStamp LoadGrammarAndLexicon(ParserScheduler.NeedsUpdate eNeedsUpdate)
		{
			CheckDisposed();
			Trace.WriteLineIf(tracingSwitch.TraceInfo, "Worker.LoadGrammarAndLexicon: eNeedsUpdate = " + eNeedsUpdate);
			string sDescription = SetDescription(eNeedsUpdate);
			TaskReport task = new TaskReport(sDescription, m_taskUpdateHandler);
			// no longer need this pop-up; was only for debugging
			// task.NotificationMessage = "Loading Parser";

			if (m_retriever == null)
				m_retriever = new M3ParserModelRetriever(m_database);
			TimeStamp stamp;
			using (task)
			{
				XmlDocument fxtResult;
				XmlDocument gafawsFxtResult;
				try
				{
					DateTime startTime = DateTime.Now;
					stamp = m_retriever.RetrieveModel(m_connection, m_LangProject, task, eNeedsUpdate);
					long ttlTicks = DateTime.Now.Ticks - startTime.Ticks;
					Trace.WriteLineIf(tracingSwitch.TraceInfo, "FXT took : " + ttlTicks.ToString());
					fxtResult = m_retriever.ModelDom;
					gafawsFxtResult = m_retriever.TemplateDom;
				}
				catch (Exception error)
				{
					if (error.GetType() == Type.GetType("System.Threading.ThreadInterruptedException") ||
						error.GetType() == Type.GetType("System.Threading.ThreadAbortException"))
					{
						throw error;
					}
					task.EncounteredError(null); // Don't want to show message box in addition to yellow crash box!
					throw new ApplicationException("Error while retrieving model for the Parser.", error);
				}

				LoadParser(ref fxtResult, gafawsFxtResult, task, eNeedsUpdate);

			}
			return stamp;
		}

		protected abstract void LoadParser(ref XmlDocument model, XmlDocument template, TaskReport task, ParserScheduler.NeedsUpdate eNeedsUpdate);

		protected static string SetDescription(ParserScheduler.NeedsUpdate eNeedsUpdate)
		{
			string sDescription;
			switch (eNeedsUpdate)
			{
				case ParserScheduler.NeedsUpdate.GrammarAndLexicon:
					sDescription = ParserCoreStrings.ksLoadGrammarAndLexicon;
					break;
				case ParserScheduler.NeedsUpdate.GrammarOnly:
					sDescription = ParserCoreStrings.ksLoadGrammar;
					break;
				case ParserScheduler.NeedsUpdate.LexiconOnly:
					sDescription = ParserCoreStrings.ksLoadLexicon;
					break;
				case ParserScheduler.NeedsUpdate.HaveChangedData:
					sDescription = ParserCoreStrings.ksUpdatingGrammarAndLexicon;
					break;
				default:
					sDescription = ParserCoreStrings.ksNotDoingAnything;
					break;
			}
			return sDescription;
		}

#if FirstPassAttempt
		/// <summary>
		/// Get handler
		/// </summary>
		internal ChangedParserDataHandler ChangedParserDataHandler
		{
			get
			{
				CheckDisposed();
				return m_changedDataHandler;
			}
		}
#endif
		/// <summary>
		/// Get whether there is any stored data changes or not
		/// </summary>
		internal bool HaveChangedParserData
		{
			get
			{
				CheckDisposed();
				if (m_retriever != null)
					return m_retriever.HaveChangedData;
				else
					return false;
			}
		}
		public void StoreChangedDataItems(SqlDataReader sqlreader)
		{
			if (m_retriever != null)
				m_retriever.StoreChangedDataItems(sqlreader);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="hvo"></param>
		/// <returns>returns null if the Wordform is not found or does not have that writing system.</returns>
		private string GetWordformStringAndCRC(int hvo, out uint uiCRC)
		{
			Debug.Assert(m_connection != null);

			string form = null;
			uiCRC = 0;

			SqlCommand command = m_connection.CreateCommand();
			command.CommandType = System.Data.CommandType.Text;

			// Check to make sure the hvo is a wordform.
			command.CommandText = string.Format("SELECT Checksum FROM wfiwordform WHERE Id={0}",
				hvo);
			object obj = command.ExecuteScalar();
			//if (obj == null)
			//{
			//    // LT-7203 has a context where the wordform was recently deleted by the user.
			//    // Throwing the exception is a bit too rude for that context.
			//    //throw new ArgumentException("The Wordform '" + hvo.ToString() + "' was not found.");
			//}

			if (obj != null)
			{
				// The Checksum has to be stored as an integer,
				// so we need to convert to uint.
				// Cf. LT-7268
				int i = (int)obj;
				uiCRC = (uint)i;
				command.CommandText = string.Format("select txt from wfiwordform_form"
					+ " where Obj={0} and Ws={1}",
					hvo, m_vernacularWS);
				obj = command.ExecuteScalar();
				if (obj == null)
				{
					// No such form, so try some other ws.
					command.CommandText = string.Format("select top 1 txt from wfiwordform_form"
						+ " where Obj={0}",
						hvo);
					obj = command.ExecuteScalar();
				}
				if (obj != null)
				{
					form = obj as string;
					if (form !=null)
					{
						form = form.Replace(' ', '.'); // LT-7334 to allow for phrases
					}
				}
			}
			return form;
		}

		private void DebugMsg(string msg)
		{
#if DEBUG
			// create the initial info:
			// datetime threadid threadpriority: msg
			System.Text.StringBuilder msgOut = new System.Text.StringBuilder();
			msgOut.Append(DateTime.Now.Ticks);
			msgOut.Append("-");
			msgOut.Append(System.Threading.Thread.CurrentThread.GetHashCode());
			msgOut.Append("-");
			msgOut.Append(System.Threading.Thread.CurrentThread.Priority);
			msgOut.Append(": ");
			msgOut.Append(msg);
			Trace.WriteLineIf(tracingSwitch.TraceInfo, msgOut.ToString());
#endif
		}
	}
}
