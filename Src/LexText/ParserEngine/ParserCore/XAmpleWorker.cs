using System;
using System.Diagnostics;
using System.Data.SqlClient;
using System.Xml;
using System.Text;
using System.IO;

using SIL.WordWorks.GAFAWS;
using SIL.FieldWorks.Common.Utils;

using XAmpleCOMWrapper;

namespace SIL.FieldWorks.WordWorks.Parser
{
	internal class XAmpleParserWorker : ParserWorker
	{
		private bool m_XAmpleHasBeenLoaded = false;
		private CXAmpleWrapperClass m_xample = null;
		private int m_idThread = 0;

		public XAmpleParserWorker(SqlConnection connection, string database, string LangProject, TaskUpdateEventHandler handler)
			: base(connection, database, LangProject, handler, "M3Parser", "Normal")
		{
		}

		internal override int ThreadId
		{
			get
			{
				CheckDisposed();
				return m_idThread;
			}
		}

		static internal string PathToXAmple()
		{
			return SIL.FieldWorks.Common.Utils.DirectoryFinder.FWCodeDirectory;
		}

		/// <summary>
		/// Create and initialize the XAmple wrapper, which also loads the XAmple DLL.
		/// DON'T CALL THIS UNTIL WE'RE RUNNING ON THE PROPER THREAD!  (See LT-3546 for
		/// what happens if you don't wait.)  The XAmple code will run on the thread
		/// from which this is called.
		/// </summary>
		internal override void InitParser()
		{
			CheckDisposed();

			m_xample = new CXAmpleWrapperClass();
			m_xample.Init(PathToXAmple());
		}

		protected override void CleanupParser()
		{
			if (m_xample != null)
			{
				System.Runtime.InteropServices.Marshal.ReleaseComObject(m_xample);
				m_xample = null;
				m_XAmpleHasBeenLoaded = false;
			}
		}

		protected override string ParseWord(string form, int hvoWordform)
		{
			Debug.Assert(m_XAmpleHasBeenLoaded, "It looks like the calling code forgot to load a XAmple");
			m_idThread = m_xample.get_AmpleThreadId();
			return CompleteAmpleResults(m_xample.ParseWord(form), hvoWordform);
		}

		protected override string TraceWord(string form, string selectTraceMorphs)
		{
			Debug.Assert(m_XAmpleHasBeenLoaded, "It looks like the calling code forgot to load a XAmple");
			m_idThread = m_xample.get_AmpleThreadId();
			return m_xample.TraceWord(form, selectTraceMorphs);
		}

		/// <summary>
		/// XAmple does not know the hvo of the Wordform.
		/// Thus it leaves a pattern which we need to replace with the actual hvo.
		/// </summary>
		/// <remarks>It would be nice if this was done down in the XAmple wrapper.
		/// However, I despaired of doing this simple replacement using bstrs, so I am doing it here.
		/// </remarks>
		/// <param name="rawAmpleResults"></param>
		/// <param name="hvoWordform"></param>
		/// <returns></returns>
		private string CompleteAmpleResults(string rawAmpleResults, int hvoWordform)
		{
			// REVIEW Jonh(RandyR): This should probably be a simple assert,
			// since it is a programming error in the XAmple COM dll.
			if (rawAmpleResults == null)
				throw new ApplicationException("XAmpleCOM Dll failed to return any results. "
					+ "[NOTE: This is a programming error. See WPS-24 in JIRA.]");

			//find any instance of "<...>" which must be replaced with "[..]" - this indicates full reduplication
			const string ksFullRedupMarker = "<...>";
			string sTemp = rawAmpleResults.Replace(ksFullRedupMarker, "[...]");
			//find the "DB_REF_HERE" which must be replaced with the actual hvo
			const string kmatch = "DB_REF_HERE";
			Debug.Assert(sTemp.IndexOf(kmatch) > 0,
				"There was a problem interpretting the response from XAMPLE. " + kmatch + " was not found.");
			return sTemp.Replace(kmatch, "'" + hvoWordform.ToString() + "'");
		}

		protected override void LoadParser(ref XmlDocument model, XmlDocument template, TaskReport task, ParserScheduler.NeedsUpdate eNeedsUpdate)
		{
			try
			{
				M3ToXAmpleTransformer transformer = new M3ToXAmpleTransformer(m_database);
				if (eNeedsUpdate == ParserScheduler.NeedsUpdate.GrammarAndLexicon ||
					eNeedsUpdate == ParserScheduler.NeedsUpdate.LexiconOnly ||
					eNeedsUpdate == ParserScheduler.NeedsUpdate.HaveChangedData)
				{ // even though POS is part of Grammar, this is only used by the lexicon
					DateTime startTime = DateTime.Now;
					// PrepareTemplatesForXAmpleFiles adds orderclass elements to MoInflAffixSlot elements
					transformer.PrepareTemplatesForXAmpleFiles(ref model, template, task);
					long ttlTicks = DateTime.Now.Ticks - startTime.Ticks;
					Trace.WriteLineIf(tracingSwitch.TraceInfo, "GAFAWS prep took : " + ttlTicks.ToString());
				}
				transformer.MakeAmpleFiles(model, task, eNeedsUpdate);
			}
			catch (Exception error)
			{
				if (error.GetType() == Type.GetType("System.Threading.ThreadInterruptedException") ||
					error.GetType() == Type.GetType("System.Threading.ThreadAbortException"))
				{
					throw error;
				}

				task.EncounteredError(null);	// Don't want to show message box in addition to yellow crash box!
				throw new ApplicationException("Error while generating files for the Parser.", error);
			}

			int maxAnalCount = 20;
			XmlNode maxAnalCountNode = model.SelectSingleNode("/M3Dump/ParserParameters/XAmple/MaxAnalysesToReturn");
			if (maxAnalCountNode != null)
			{
				maxAnalCount = Convert.ToInt16(maxAnalCountNode.FirstChild.Value);
				if (maxAnalCount < 1)
					maxAnalCount = -1;
			}

			try
			{
				m_xample.SetParameter("MaxAnalysesToReturn", maxAnalCount.ToString());
			}
			catch (Exception error)
			{
				if (error.GetType() == Type.GetType("System.Threading.ThreadInterruptedException") ||
					error.GetType() == Type.GetType("System.Threading.ThreadAbortException"))
				{
					throw error;
				}
				ApplicationException e = new ApplicationException("Error while setting Parser parameters.", error);
				task.EncounteredError(null);	// Don't want to show message box in addition to yellow crash box!
				throw e;
			}

			LoadXAmpleFiles(task);
		}

		private void LoadXAmpleFiles(TaskReport task)
		{
			try
			{
				EnsureXampleSupportFilesExist();
				string tempPath = System.IO.Path.GetTempPath();
				string xPath = XAmpleFixedFilesPath;
				m_xample.LoadFiles(xPath, tempPath, m_database);
				m_XAmpleHasBeenLoaded = true;
			}
			catch (Exception error)
			{
				if (error.GetType() == Type.GetType("System.Threading.ThreadInterruptedException") ||
					error.GetType() == Type.GetType("System.Threading.ThreadAbortException"))
				{
					throw error;
				}
				ApplicationException e = new ApplicationException("Error while loading the Parser.", error);
				task.EncounteredError(null);	// Don't want to show message box in addition to yellow crash box!
				throw e;
			}
		}

		private void EnsureXampleSupportFilesExist()
		{
			string path = XAmpleFixedFilesPath + @"\cd.tab";
			if (!System.IO.File.Exists(path))
				throw new ApplicationException("There seems to be a problem with the installation. Expected to find this file, but it does not exist: " + path);
		}

		private string XAmpleFixedFilesPath
		{
			get
			{
				return DirectoryFinder.FWCodeDirectory + @"\Language Explorer\Configuration\Grammar";
			}
		}

	}
}
