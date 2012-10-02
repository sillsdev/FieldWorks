// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2002, SIL International. All Rights Reserved.
// <copyright from='2002' to='2002' company='SIL International'>
//		Copyright (c) 2002, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: Scheduler.cs
// Responsibility: John Hatton
// Last reviewed:
//
// <remarks>
//	this class implements a worker thread.
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Xml;
using SIL.FieldWorks.Common.Utils; // for Win32 message defns.

namespace SIL.FieldWorks.WordWorks.Parser
{
	/// <summary>
	/// Clients should normally not access this directly,
	/// but rather use the subscription services of the	ParserConnection.
	/// </summary>
	public delegate void ParserUpdateEventHandler(ParserScheduler sender, TaskReport topTask);

	/// <summary>
	///
	/// </summary>
	public sealed class ParserScheduler : MarshalByRefObject, IFWDisposable
	{
		public enum NeedsUpdate {GrammarAndLexicon, GrammarOnly, LexiconOnly, HaveChangedData, Nothing};
		public enum Priority {eventually, soon, ASAP};
		public event ParserUpdateEventHandler ParserUpdateVerbose = delegate {};  // in theory, setting to delegate{} makes the event thread safe
		public event ParserUpdateEventHandler ParserUpdateNormal = delegate {};

		public double kSecondsBetweenUpdateChecks = 31536000; // effectively disable it; this is a years worth of seconds; was 60;
		private bool m_fForceGrammarOrLexiconNeedsUpdatingCheck;
		private bool m_fSqlConnectionIsBusy;
		private const string m_parserGuid = "E8F3514C-D0B5-4978-AA07-56B27EE68779";
		private M3ParserWordformQueue m_lowQueue;
		private M3ParserWordformQueue m_mediumQueue;
		private M3ParserWordformQueue m_highQueue;
		private DateTime m_lastNeedUpdateCheck;	// this variable is protected via a cs
		private object m_lastNeedUpdateCheckObject = new object();
		private string m_tryAWord;
		private bool m_doTrace = false;
		private string m_sSelectTraceMorphs = null;
		private object m_tryAWord_lock = new object();
		private Thread m_consumerThread;
		private ManualResetEvent m_PauseEvent;		// Pause processing...
		private ManualResetEvent m_StopEvent;			// Stop
		private ManualResetEvent m_QueueEvent;		// hvo placed in a queue
		private ManualResetEvent m_TryAWordEvent;	// Trace Word to process
		// number of seconds to wait for event in main thread routine
		private TimeSpan m_waitTime;
		private bool m_paused;
		private bool m_fTryAWordDialogRunning = false;
		private bool m_fParseAllWordforms = true;
		private Object m_pausedObject = new object();
		private Exception m_caughtException;
		private ParserWorker m_parserWorker;
		private SqlConnection m_sqlConnection;
		private TimeStamp m_lastGrammarUpdateStamp;
		private object m_lastGrammarUpdateStampObject = new object();
		private string m_server;
		private string m_database;
		private string m_LangProject;
		private string m_parser;
		private TraceSwitch lockingSwitch = new TraceSwitch("ParserCore.LockingTrace", "Syncronization tracking", "Off");
		private TraceSwitch tracingSwitch = new TraceSwitch("ParserCore.TracingSwitch", "Just regular tracking", "Off");

		public const string m_ksChangedParserDataQuery =
			"SELECT DISTINCT sync.ObjId, sync.ObjFlid, co.Class$, class.Name, sync.Id FROM Sync$ sync \n" +
			"JOIN CmObject co ON sync.ObjId=co.Id \n" +
			"JOIN Class$ class ON co.Class$=class.Id \n" +
			"	WHERE co.UpdStmp > {0} \n" +
			"		AND (co.Class$ IN \n" +
			"			( 4, -- FsComplexFeature 4 \n" +
			"			  7, -- CmPossibilityList 7 \n" +  // productivity restrictions
			"			  8, -- CmPossibility 8 \n" +      // productivity restriction
			"			 49, -- FsFeatureSystem 49 \n" +
			"			 50, -- FsClosedFeature 50 \n" +
			"			 51, -- FsClosedValue 51 \n" +
			"			 53, -- FsComplexValue 53 \n" +
			"			 57, -- FsFeatureStructure 57 \n" +
			"			 59, -- FsFeatureStructureType 59 \n" +
			"			 65, -- FsSymFeatVal 65 \n" +
			"			 5001, -- MoStemMsa 5001 \n" +
			"			 5002, -- LexEntry 5002 \n" +
			"			 5005, -- LexDb 5005 \n" +
			"			 5016, -- LexSense 5016 \n" +
			"			 5026, -- MoAdhocProhib 5026 \n" +
			"			 5027, -- MoAffixAllomorph 5027 (Actually only want MoAffixForm, but it doesn't work) \n" +
			"			 5028, -- MoAffixForm 5028 \n" +
			"            5029, -- MoAffixProcess 5029 \n" +
			"			 5030, -- MoCompoundRule 5030 \n" +
			"			 5031, -- MoDerivAffMsa 5031 \n" +
			"			 5033, -- MoEndoCompound 5033 \n" +
			"			 5034, -- MoExoCompound 5034 \n" +
			"			 5035, -- MoForm 5035 \n" +
			"			 5036, -- MoInflAffixSlot 5036 \n" +
			"			 5037, -- MoInflAffixTemplate 5037 \n" +
			"			 5038, -- MoInflectionalAffixMsa 5038 \n" +
			"			 5039, -- MoInflClass 5039 \n" +
			"			 5040, -- MoMorphData 5040 \n" +
			"			 5041, -- MoMorphSynAnalysis 5041 \n" +
			"			 5042, -- MoMorphType 5042 \n" +
			"			 5045, -- MoStemAllomorph 5045 \n" +
			"			 5049, -- PartOfSpeech 5049 \n" +
			"            5070, -- MoModifyFromInput 5070 \n" +
			"            5082, -- PhIterationContext 5082 \n" +
			"            5083, -- PhSequenceContext 5083 \n" +
			"            5086, -- PhSimpleContextNC 5086 \n" +
			"            5089, -- PhPhonemeSet 5089 \n" +
			"			 5092, -- PhPhoneme 5092 \n" +
			"            5094, -- PhNCFeatures 5094 \n" +
			"			 5095, -- PhNCSegments 5095 \n" +
			"			 5097, -- PhEnvironment 5097 \n" +
			"			 5098, -- PhCode 5098 \n" +
			"			 5099, -- PhPhonData 5099 \n" +
			"			 5101, -- MoAlloAdhocProhib 5101 \n" +
			"			 5102, -- MoMorphAdhocProhib 5102 \n" +
			"            5103, -- MoCopyFromInput 5103 \n" +
			"			 5110, -- MoAdhocProhibGr 5110 \n" +
			"			 5117, -- MoUnclassifiedAffixMsa 5117 \n" +
			"            5128, -- PhSegmentRule 5128 \n" +
			"            5129, -- PhRegularRule 5129 \n" +
			"            5130, -- PhMetathesisRule 5130 \n" +
			"            5131 -- PhSegRuleRHS 5131 \n" +
			"			)) ORDER BY sync.Id ";

		// We need a reference count to properly handle sharing a scheduler between 2 (or more) windows.
		// See LT-6500.
		private int m_cref;

		public static string AppGuid
		{
			get { return m_parserGuid; }
		}

		public DateTime NeedUpdateCheck
		{
			get
			{
				CheckDisposed();

				DateTime rval;
				if (!Monitor.TryEnter(m_lastNeedUpdateCheckObject))
				{
					TraceMsg(lockingSwitch.TraceInfo, ">>>>>>>*****  This would have been a collision (GET)********<<<<<<<<<<<");
					Monitor.Enter(m_lastNeedUpdateCheckObject);
				}
				rval = m_lastNeedUpdateCheck;
				Monitor.Exit(m_lastNeedUpdateCheckObject);
				return rval;
				//	get {lock(this)	{ return m_lastNeedUpdateCheck;	} }
			}

			set
			{
				CheckDisposed();

				if (!Monitor.TryEnter(m_lastNeedUpdateCheckObject))
				{
					TraceMsg(lockingSwitch.TraceInfo, ">>>>>>>*****  This would have been a collision (SET)********<<<<<<<<<<<");
					Monitor.Enter(m_lastNeedUpdateCheckObject);
				}
				m_lastNeedUpdateCheck = value;
				Monitor.Exit(m_lastNeedUpdateCheckObject);
				//	set {lock(this)	{ m_lastNeedUpdateCheck = value; } }
			}
		}
		/// <summary>
		/// Get or Set state for parsing all wordforms
		/// </summary>
		public bool ParseAllWordforms
		{
			get
			{
				CheckDisposed();
				return m_fParseAllWordforms;
			}
			set
			{
				CheckDisposed();
				m_fParseAllWordforms = value;
			}
		}
		/// <summary>
		/// Get or Set state for the try A Word dialog running
		/// </summary>
		public bool TryAWordDialogIsRunning
		{
			get
			{
				CheckDisposed();
				return m_fTryAWordDialogRunning;
			}
			set
			{
				CheckDisposed();
				m_fTryAWordDialogRunning = value;
			}
		}
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ParserScheduler"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public ParserScheduler()
		{}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ParserScheduler"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public void Init(string server, string database, string LangProject)
		{
			DebugMsg("ParserScheduler constructor is being called");
			Trace.WriteLineIf(tracingSwitch.TraceInfo, "ParserScheduler(): CurrentThreadId = " + Win32.GetCurrentThreadId().ToString());

			m_server = server;
			m_database = database;
			m_LangProject = LangProject;
			m_paused = false;
			m_sqlConnection = new SqlConnection(GetConnectionString());
			m_sqlConnection.Open();

			SqlCommand command = m_sqlConnection.CreateCommand();
			command.CommandText = "select top 1 ParserParameters from MoMorphData";
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(((string)command.ExecuteScalar()).Trim());

			XmlNode parserNode = doc.SelectSingleNode("/ParserParameters/ActiveParser");
			m_parser = parserNode == null ? "XAmple" : parserNode.InnerText;

			// could use reflection to create the correct parser worker, then this switch statement
			// would not need to be updated for each parser, probably not a big deal for now
			switch (m_parser)
			{
				case "XAmple":
					m_parserWorker = new XAmpleParserWorker(m_sqlConnection, m_database, m_LangProject,
						new TaskUpdateEventHandler(this.HandleTaskUpdate));
					break;

				case "HC":
					m_parserWorker = new HCParserWorker(m_sqlConnection, m_database, m_LangProject,
						new TaskUpdateEventHandler(this.HandleTaskUpdate));
					break;
			}

			m_lastGrammarUpdateStamp = new TimeStamp();
			m_fForceGrammarOrLexiconNeedsUpdatingCheck = false;

			m_StopEvent = new ManualResetEvent(false);
			m_PauseEvent = new ManualResetEvent(false);
			m_QueueEvent = new ManualResetEvent(false);
			m_TryAWordEvent = new ManualResetEvent(false);
			m_waitTime = new TimeSpan(0, 0, 1);

			m_lowQueue = new M3ParserWordformQueue();
			m_mediumQueue = new M3ParserWordformQueue();
			m_highQueue = new M3ParserWordformQueue();
			//			m_traceQueue = new M3ParserWordformQueue();

			m_consumerThread = new Thread(new ThreadStart(this.ConsumerThreadStart));
			m_consumerThread.Priority = ThreadPriority.BelowNormal;
			m_consumerThread.IsBackground = true; // Can't prevent owning app from terminating.
			m_consumerThread.Name = "Parser scheduler-worker";
			m_consumerThread.SetApartmentState(ApartmentState.STA); // do before starting - DH
			m_consumerThread.CurrentUICulture = System.Threading.Thread.CurrentThread.CurrentUICulture;
			m_consumerThread.Start();
			m_cref = 1;
		}

		/// <summary>
		/// Create a parrserScheduler in a separate AppDomain so all its objects are isolated
		/// and its COM objects can live in its own thread.
		/// </summary>
		/// <param name="server"></param>
		/// <param name="database"></param>
		/// <param name="LangProject"></param>
		/// <returns></returns>
		public static ParserScheduler CreateInDomain(string server, string database, string LangProject)
		{
			// Construct and initialize settings for a second AppDomain.
			string exeAssembly = Assembly.GetExecutingAssembly().FullName;
			AppDomainSetup ads = new AppDomainSetup();
			ads.ApplicationBase = Path.GetDirectoryName(Assembly.GetAssembly(typeof(ParserScheduler)).Location);
			// ads.ApplicationBase = System.Environment.CurrentDirectory;
			ads.DisallowBindingRedirects = false;
			ads.DisallowCodeDownload = true;
			ads.ConfigurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;

			// Create the second AppDomain.
			AppDomain adParser = AppDomain.CreateDomain("AD Parser scheduler-worker", null, ads);

			// Create an instance of MarshalbyRefType in the second AppDomain.
			// A proxy to the object is returned.
			ParserScheduler parser =
				(ParserScheduler)adParser.CreateInstanceAndUnwrap(
					exeAssembly,
					typeof(ParserScheduler).FullName
				);
			parser.Init(server, database, LangProject);
			return parser;
		}

		/// <summary>
		/// Set lifetime of the remoting object to infinite so we don't lose the connection (fixes LT-8597 and LT-8619)
		/// </summary>
		/// <returns></returns>
		public override object InitializeLifetimeService()
		{
			return null;
		}

		/// <summary>
		/// Increment the reference count and return the new value.
		/// </summary>
		/// <returns></returns>
		public int AddRef()
		{
			++m_cref;
			return m_cref;
		}

		/// <summary>
		/// Decrement the reference count and return the new value.
		/// </summary>
		/// <returns></returns>
		public int SubtractRef()
		{
			--m_cref;
			return m_cref;
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
		///
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
		~ParserScheduler()
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
			//Debug.Assert(disposing, "Don't even think about not disposing this object properly!");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_consumerThread != null)
				{
					//REVIEW: not sure if this is right
					//				if(this.IsPaused)
					//					this.Resume();

					// Notify the parser to quit if it's running.  See LT-3546 for motivation.
					if (m_parserWorker != null)
					{
						int idThread = m_parserWorker.ThreadId;
						Trace.WriteLineIf(tracingSwitch.TraceInfo, "ParserScheduler.Dispose(true): m_parserWorker.ThreadId = " + idThread.ToString() +
							", m_consumerThread.ManagedThreadId = " + m_consumerThread.ManagedThreadId +
							", GetCurrentThreadId() = " + Win32.GetCurrentThreadId().ToString());
						if (idThread != 0)
						{
							bool fOk;
							fOk = Win32.PostThreadMessage(idThread, (int)Win32.WinMsgs.WM_USER,
								(uint)Win32.WinMsgs.WM_QUIT, (uint)idThread);
							if (!fOk)
								Trace.WriteLineIf(tracingSwitch.TraceError, "ParserScheduler.Dispose(true): Win32.PostThreadMessage() to stop thread failed!??");
						}
					}
					// REVIEW DanH (RandyR): Why is is paused only to be resumed?
					AttemptToPause();
					//un-pause if necessary
					Resume();
					//note that this is a polite interruption... it will only stop the thread if it is sleeping
					//m_consumerThread.Interrupt();

					if (m_consumerThread.IsAlive)
					{
						m_consumerThread.Priority = ThreadPriority.AboveNormal;	// make sure we are normal priority to get chance to run
						m_consumerThread.Interrupt();
					}
					m_StopEvent.Set();		// ready to stop now, signal STOP event
					// If the DB is still working, then it will pull the plug on the thread.
					// I (RandyR) reset the timeout for the DB to be 30 seconds, since some actions took longer than the
					// previous limit of 10 seconds.
					// Therefore, I bumped this wait from 10 to 45 seconds.
					// Even 45 seconds isn't long enough when it is dumping the grammar and lexicon, for larger data sets.
					if (m_consumerThread.IsAlive == false || m_consumerThread.Join(new TimeSpan(0,0,45)))	// wait up to 45 seconds, since the DQ connection mya be waiting for 30 seconds.
					{
						// consumer thread exited
						Trace.WriteLineIf(tracingSwitch.TraceInfo, "==== ParserScheduler thread Successfully shutdown.");
					}
					else
					{
						Trace.WriteLineIf(tracingSwitch.TraceError, "**** ERROR : ParserScheduler Thread didn't shut down, Aborting.");
						try
						{
							m_consumerThread.Abort();
						}
						catch (ThreadAbortException)
						{
							// Eat the ThreadAbortException exception, since it isn't fatal.
						}
						finally
						{
							if (m_consumerThread.Join(new TimeSpan(0,0,10))== false)	// wait up to 10 more seconds
							{
								Trace.WriteLineIf(tracingSwitch.TraceError, "**** ERROR : Abort didn't shutdown.");
							}
							// Both of these should have been Closed/Disposed & the variables set to null by now,
							// IF the thread had shut down properly, that is.
							if (m_sqlConnection != null)
							{
								Trace.WriteLineIf(tracingSwitch.TraceError, "Shut down connection by brute force.");
								m_sqlConnection.Close();
								m_sqlConnection.Dispose();
							}
							if (m_parserWorker != null)
							{
								Trace.WriteLineIf(tracingSwitch.TraceError, "Disposing Worker bee by brute force.");
								m_parserWorker.Dispose();
							}
						}
					}
				}
			}
			// Dispose unmanaged resources here, whether disposing is true or false.
			m_caughtException = null;
			m_sqlConnection = null;
			m_parserWorker = null;
			m_consumerThread = null;
			m_lastGrammarUpdateStamp = null;
			m_server = null;
			m_database = null;
			m_LangProject = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		private void ResetGrammarTime()
		{
			ResetGrammarTime(new TimeStamp());	//make the reset time *now*
		}

		private void ResetGrammarTime(TimeStamp ts)
		{
			if (!Monitor.TryEnter(m_lastGrammarUpdateStampObject))
			{
				TraceMsg(lockingSwitch.TraceInfo, ">>>>>>>*****  ResetGrammarTime colision ********<<<<<<<<<<<");
				Monitor.Enter(m_lastGrammarUpdateStampObject);
			}
			m_lastGrammarUpdateStamp = ts;
			// Also reset the checking interval time, so it won't bother checking again,
			// until its time is up.
			NeedUpdateCheck = DateTime.Now;
			Monitor.Exit(m_lastGrammarUpdateStampObject);
		}

		private void LoadGrammarAndLexicon(NeedsUpdate eNeedsUpdate)
		{
			Trace.WriteLineIf(tracingSwitch.TraceInfo, "Scheduler.LoadGrammarAndLexicon: eNeedsUpdate = " + eNeedsUpdate);
			try
			{
				if (eNeedsUpdate != NeedsUpdate.Nothing)
				{
					if (ParseAllWordforms)
						InvalidateAllWordforms();
					TimeStamp ts = m_parserWorker.LoadGrammarAndLexicon(eNeedsUpdate);
					ResetGrammarTime(ts);
				}
			}
			catch (System.Threading.ThreadAbortException)
			{
				// Error? What error?
				//m_caughtException = error;
			}
			catch (System.Threading.ThreadInterruptedException)
			{
				// Error? What error?
				//m_caughtException = error;
			}
		}

		/// <summary>
		/// Reload Grammar And Lexicon
		/// </summary>
		public void ReloadGrammarAndLexicon()
		{
			CheckDisposed();

			//needs synchronization since will be called and read on diff threads
			ResetGrammarTime();
		}

		/// <summary>
		/// Reload Grammar And Lexicon
		/// </summary>
		public void LoadGrammarAndLexiconIfNeeded()
		{
			CheckDisposed();

			m_fForceGrammarOrLexiconNeedsUpdatingCheck = true;
			//LoadGrammarAndLexicon(GrammarOrLexiconNeedsToBeUpdated());
		}
		/// <summary>
		/// Put all Wordforms in the entire WordformInventory into the low priority queue.
		/// </summary>
		public void InvalidateAllWordforms()
		{
			Debug.Assert(m_sqlConnection.State == System.Data.ConnectionState.Open);
			//enhance: could clear the queue first (it does not have a clear() method).

			try
			{
				Trace.WriteLineIf(tracingSwitch.TraceInfo, "InvalidatingAllWordforms: before busy check");
				while (m_fSqlConnectionIsBusy)
					Thread.Sleep(10);

				m_fSqlConnectionIsBusy = true;
				Trace.WriteLineIf(tracingSwitch.TraceInfo, "InvalidatingAllWordforms: busy");
				SqlCommand command = m_sqlConnection.CreateCommand();
				// Have it process the most recently added wordforms first.
				// That way IText will show the parser's guesses sooner
				// (otherwise the most recently added wordforms get parsed last)
				command.CommandText = "select id from WfiWordform order by id DESC";
				using (SqlDataReader reader = command.ExecuteReader())
				{
					while (reader.Read())
						ScheduleOneWordformForUpdate(reader.GetInt32(0), Priority.eventually);
				}
				m_fSqlConnectionIsBusy = false;
				Trace.WriteLineIf(tracingSwitch.TraceInfo, "InvalidatingAllWordforms: not busy");

			}
			catch (Exception error)
			{
				Debug.Assert(error == null, error.Message);
			}
		}

		internal void ConsumerThreadStart()
		{
			CheckDisposed();

			try
			{
				// REVIEW JohnH(RandyR): Is it legal to use MTA, now that ParseFiler isn't COM?
				//JH says: I don't know... the cache still is COM
				//	I notice that w/ STA, it crashes silently, but w/ MTA, it "HRESULTS" in the cache code.
//-				Thread.CurrentThread.SetApartmentState(ApartmentState.STA);
				Debug.Assert( Thread.CurrentThread.GetApartmentState() == ApartmentState.STA, "Could not set the apartment state to STA");

				// first time through, show thread ID in debug output
				Trace.WriteLineIf(tracingSwitch.TraceInfo, "=======>Scheduler Thread: " + Thread.CurrentThread.ManagedThreadId.ToString());
				Trace.WriteLineIf(tracingSwitch.TraceInfo, "ConsumerThreadStart(): CurrentThreadId = " + Win32.GetCurrentThreadId().ToString());

				// Create the list of wait events, order is important here.  Highest priority first.
				WaitHandle[] waitEvents = new WaitHandle[3];
				waitEvents[0] = m_StopEvent;		// Stop Event for this thread
				waitEvents[1] = m_TryAWordEvent;	// Trace word event
				waitEvents[2] = m_QueueEvent;		// hvo has been put in the Queue

				// Create the XAmpleWrapper on the consumer thread so that the XAmple code will
				// run on the consumer thread!  See LT-3546 for what happens if it runs on the
				// UI thread!  Also ensure that this thread can receive messages as per MSDN.
				m_parserWorker.InitParser();
				Win32.MSG msg = new Win32.MSG();
				Win32.PeekMessage(ref msg, (IntPtr)null, (uint)Win32.WinMsgs.WM_USER,
					(uint)Win32.WinMsgs.WM_USER, (uint)Win32.PeekFlags.PM_NOREMOVE);

				bool bIdle = false;
				// main thread loop
				MainLoop(waitEvents, bIdle);
			}
				// The process of loading the Lexicon and Grammar can be a long process,
				// if the user tries to stop the parser while doing this - the only quick
				// way to end the thread is to 'Interupt' it.
			catch (System.Threading.ThreadInterruptedException error)
			{
				m_caughtException = error;
			}
			catch (System.Threading.ThreadAbortException error)
			{
				m_caughtException = error;
			}
			catch (Exception error)
			{
				// The sky is *not* falling,
				// so don't scare up any more snakes than you can kill.
				// Try and be nice and give them a benign warning,
				// but all you get is a zillion JIRA issues.
				// Cf. LTB-4, LT-5590, LT-6627, LT-6675, LT-6766, LT-7236, LTB-45, LT-6185, and LT-7414.
				// Let's throw in the two omnibus JIRA issues, as well: LT-6759 and  LT-5659.
				// NO: SIL.Utils.ErrorReporter.ReportException(error,null, false);//non-fatal to the whole app...
				m_caughtException = error;	//but this thread is hosed
			}
		}

		private void MainLoop(WaitHandle[] waitEvents, bool bIdle)
		{
			try
			{
				while (true)
				{
					//				DebugMsg("parser worker thread loop");
					// Look for STOP event or real work
					int waitIndex = WaitHandle.WaitAny(waitEvents, m_waitTime, false);

					// handle the STOP event without doing other work
					if (waitIndex == 0)
					{
						try
						{
							if (!Monitor.TryEnter(this))
							{
								TraceMsg(lockingSwitch.TraceInfo, ">>>>>>>*****  collision locking parser thread A********<<<<<<<<<<<");
								Monitor.Enter(this);
							}
							if (m_parserWorker != null)
							{
								m_parserWorker.Dispose();
								m_parserWorker = null;
							}
							if (m_sqlConnection != null)
							{
								m_sqlConnection.Close();
								m_sqlConnection = null;
							}
							break;
						}
						finally
						{
							Monitor.Exit(this);
						}
					}

					// handle the Paused 'state'
					if (IsPaused)
					{
						Thread.Sleep(250);	// give it a quarter second to become unpaused
						continue;	// Paused, continue allows checking for Stop event
					}

					// not Stopped or Paused, see if the Grammar needs to be updated
					bool notYetLoaded;
					try
					{
						if (!Monitor.TryEnter(this))
						{
							TraceMsg(lockingSwitch.TraceInfo, ">>>>>>>*****  collision locking parser thread B********<<<<<<<<<<<");
							Monitor.Enter(this);
						}
						notYetLoaded = m_lastGrammarUpdateStamp.Empty;
					}
					finally
					{
						Monitor.Exit(this);
					}
					if (notYetLoaded)
					{
						LoadGrammarAndLexicon(NeedsUpdate.GrammarAndLexicon);
						bIdle = false;
					}
					else
					{
						NeedsUpdate updateNeeded = GetGrammarOrLexiconNeedsUpdate();
						LoadGrammarAndLexicon(updateNeeded);
						if (updateNeeded != NeedsUpdate.Nothing)
							bIdle = false; // now there is something to do
					}

					// See if none of our previous events have been fired
					if (waitIndex == WaitHandle.WaitTimeout)
					{
						// switch back to lower priority thread, if not already
						if (Thread.CurrentThread.Priority != ThreadPriority.BelowNormal)
						{
							Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
							Trace.WriteLineIf(tracingSwitch.TraceInfo, "=======>Scheduler Thread: " + Thread.CurrentThread.ManagedThreadId.ToString() + " setting priority to BelowNormal");
						}

						// still nothing pending...
						if (!bIdle)
						{
							//Trace.WriteLine("MainLoop: Begin Show Idle, WaitHandle.WaitTimeout");
							new TaskReport(ParserCoreStrings.ksIdle_,
								new TaskUpdateEventHandler(this.HandleTaskUpdate));
							//Trace.WriteLine("MainLoop: End   Show Idle, WaitHandle.WaitTimeout");
							bIdle = true;	// this will keep us from reporting 'idle' constantly
						}
						continue;
					}
					// See if Try a Word is running and there are still words in a queue, but we're not parsing a word for Try a Word
					if (m_fTryAWordDialogRunning && waitIndex == 2)
					{
						if (!bIdle)
						{
							//Trace.WriteLine("MainLoop: Begin Show Idle, Try A Word");
							new TaskReport(ParserCoreStrings.ksIdle_,
								new TaskUpdateEventHandler(this.HandleTaskUpdate));
							//Trace.WriteLine("MainLoop: End   Show Idle, Try A Word");
							bIdle = true;	// this will keep us from reporting 'idle' constantly
						}
						continue;
					}
					bIdle = false;	// turn off idle flag

					DoWordEvent(waitIndex);
				}
			}
			catch (System.Threading.ThreadInterruptedException)
			{
				//m_caughtException = error;
			}
			catch (System.Threading.ThreadAbortException)
			{
				//m_caughtException = error;
			}
		}

		private void DoWordEvent(int waitIndex)
		{
			DebugMsg("DoWordEvent-" + waitIndex.ToString());
			switch (waitIndex)
			{
				case 1:		// Trace Word event
					DebugMsg("Start - DoWordEvent 1 - before Lock");
					try
					{
						if(!Monitor.TryEnter(m_tryAWord_lock))
						{
							TraceMsg(lockingSwitch.TraceInfo, ">>>>>>>*****  collision locking m_tryAWord A********<<<<<<<<<<<");
							Monitor.Enter(m_tryAWord_lock);
						}

						DebugMsg("Start - DoWordEvent 1 - after Lock");
						if (m_tryAWord !=null )
						{
							// force loading of grammar and/or lexicon if needed
							LoadGrammarAndLexicon(GrammarOrLexiconNeedsToBeUpdated());
							//int tracehvo = m_traceQueue.DequeueWordform();
							//the results get back to the client asynchronously through the TaskReport
							DebugMsg("Start - DoWordEvent 1 - Before parserWorker.TryAWord");
							m_parserWorker.TryAWord(m_tryAWord, m_doTrace, m_sSelectTraceMorphs);
							DebugMsg("Start - DoWordEvent 1 - Before parserWorker.TryAWord");

							m_tryAWord= null;
							m_TryAWordEvent.Reset();	// unsignal the event
							//continue;
						}
						//						if (m_traceQueue.Count > 0)
						//						{
						//							int tracehvo = m_traceQueue.DequeueWordform();
						//							//the results get back to the client asynchronously through the TaskReport
						//							m_parserWorker.TraceWord(tracehvo);
						//							continue;
						//						}
						DebugMsg("Start - DoWordEvent 1 - exiting Lock");
					}
					finally
					{
						Monitor.Exit(m_tryAWord_lock);
					}
					DebugMsg("Start - DoWordEvent 1 - exited Lock");
					break;
				case 2:		// hvo queue event
					if (m_fTryAWordDialogRunning)
						break;
					int hvo = 0;
					try
					{
						if(!Monitor.TryEnter(this))
						{
							TraceMsg(lockingSwitch.TraceInfo, ">>>>>>>*****  collision locking parser thread L********<<<<<<<<<<<");
							Monitor.Enter(this);
						}
						if (m_highQueue.Count > 0)
						{
							hvo = m_highQueue.DequeueWordform();
							if (Thread.CurrentThread.Priority != ThreadPriority.Normal)
							{
								Thread.CurrentThread.Priority = ThreadPriority.Normal;
								Trace.WriteLineIf(tracingSwitch.TraceInfo, "=======>Scheduler Thread: " + Thread.CurrentThread.ManagedThreadId.ToString() + " setting priority to Normal");
							}
						}
						else
						{
							if (Thread.CurrentThread.Priority != ThreadPriority.BelowNormal)
							{
								Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
								Trace.WriteLineIf(tracingSwitch.TraceInfo, "=======>Scheduler Thread: " + Thread.CurrentThread.ManagedThreadId.ToString() + " setting priority to BelowNormal");
							}
							if (m_mediumQueue.Count > 0)
								hvo = m_mediumQueue.DequeueWordform();
							else if (m_lowQueue.Count > 0)
								hvo = m_lowQueue.DequeueWordform();
						}
						if (hvo <= 0)
							m_QueueEvent.Reset();		// turn off queue event flag
					}
					finally
					{
						Monitor.Exit(this);
					}

					if (hvo > 0)
					{
						Trace.WriteLine("UpdateWordform("+hvo.ToString()+") - Start");
						m_parserWorker.UpdateWordform(hvo);
						Trace.WriteLine("UpdateWordform("+hvo.ToString()+") - Finished");
					}
					break;

				default:
					break;
			}
		}

		// per KenZ, convention is for server to already include the \\SILFW,e.g. HATTON1\\SILFW
		private string GetConnectionString()
		{
			return  "Server=" + m_server + "; Database=" +m_database+ "; User ID=FWDeveloper;"
				+ "Password=careful; Pooling=false;";

		}

		/// <summary>
		/// Determine whether the parsing engine is up to date or not.
		/// </summary>
		/// <returns>True if the grammar is out of date, otherwise false.</returns>
		private NeedsUpdate GetGrammarOrLexiconNeedsUpdate()
		{
			// Don't use 'Seconds', as that is just the seconds part of the time span.
			double span = TimeSpan.FromTicks(DateTime.Now.Ticks - NeedUpdateCheck.Ticks).TotalSeconds;
			if ((span < kSecondsBetweenUpdateChecks) && !m_fForceGrammarOrLexiconNeedsUpdatingCheck)
			{
				return NeedsUpdate.Nothing;
			}
			m_fForceGrammarOrLexiconNeedsUpdatingCheck = false;
			return GrammarOrLexiconNeedsToBeUpdated();
		}

		private NeedsUpdate GrammarOrLexiconNeedsToBeUpdated()
		{
			object lexiconResult = null;
			object grammarResult = null;
			try
			{
				// result is ID of the first object which is newer than the given date,
				// or null, if everything is current.
				Trace.WriteLineIf(tracingSwitch.TraceInfo, "GrammarOrLexiconNeedsToBeUpdated: before busy check");
				while(m_fSqlConnectionIsBusy)
					Thread.Sleep(10);
				m_fSqlConnectionIsBusy = true;
				if (HaveChangedParserData())
				{
					m_fSqlConnectionIsBusy = false;
					return NeedsUpdate.HaveChangedData;
				}

				Trace.WriteLineIf(tracingSwitch.TraceInfo, "GrammarOrLexiconNeedsToBeUpdated is now busy");
				SqlCommand cmd = m_sqlConnection.CreateCommand();
				cmd.CommandType = System.Data.CommandType.StoredProcedure;
				SqlParameter parameter = cmd.Parameters.Add("@stampCompare", System.Data.SqlDbType.Timestamp);

				try
				{
					if (!Monitor.TryEnter(this))
					{
						TraceMsg(lockingSwitch.TraceInfo, ">>>>>>>*****  collision locking parser thread D********<<<<<<<<<<<");
						Monitor.Enter(this);
					}
					parameter.Value = (System.Array)m_lastGrammarUpdateStamp;
				}
				finally
				{
					Monitor.Exit(this);
				}
				cmd.CommandText = "WasParsingLexiconDataModified";
				DebugMsg("Start - SqlCommand - WasParsingLexiconDataModified");
				lexiconResult = cmd.ExecuteScalar();
				DebugMsg("End   - SqlCommand - WasParsingLexiconDataModified");
				cmd.CommandText = "WasParsingGrammarDataModified";
				DebugMsg("Start - SqlCommand - WasParsingGrammarDataModified");
				grammarResult = cmd.ExecuteScalar();
				DebugMsg("End   - SqlCommand - WasParsingGrammarDataModified");
				//cmd.CommandText = "WasParsingDataModified";
				//result = cmd.ExecuteScalar();
				m_fSqlConnectionIsBusy = false;
				Trace.WriteLineIf(tracingSwitch.TraceInfo, "GrammarOrLexiconNeedsToBeUpdated is not busy");
			}
			catch (Exception error)
			{
				Debug.Assert(error == null, error.Message);
				throw;
			}
			NeedUpdateCheck = DateTime.Now;
			bool fLexiconNeedsUpdate = (lexiconResult != null);
			bool fGrammarNeedsUpdate = (grammarResult != null);
			if (fLexiconNeedsUpdate && fGrammarNeedsUpdate)
				return NeedsUpdate.GrammarAndLexicon;
			else if (fLexiconNeedsUpdate)
				return NeedsUpdate.LexiconOnly;
			else if (fGrammarNeedsUpdate)
				return NeedsUpdate.GrammarOnly;
			else
				return NeedsUpdate.Nothing;
		}

		private bool HaveChangedParserData()
		{
			SqlCommand newcmd = m_sqlConnection.CreateCommand();
			newcmd.CommandType = System.Data.CommandType.Text;
			newcmd.CommandText = String.Format(m_ksChangedParserDataQuery, m_lastGrammarUpdateStamp.Hex);
			using (SqlDataReader sqlreader = newcmd.ExecuteReader())
			{
				m_parserWorker.StoreChangedDataItems(sqlreader);
			}
			return m_parserWorker.HaveChangedParserData;
		}

		/// <summary>
		/// returns the number of the Wordforms in the low priority queue.
		/// </summary>
		public int GetQueueSize(Priority priority)
		{
			CheckDisposed();

			switch(priority)
			{
				case Priority.ASAP:
					return m_highQueue.Count;
				case Priority.soon:
					return m_mediumQueue.Count;
				case Priority.eventually:
					return m_lowQueue.Count;
				default:
					return 0;
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <remarks> this is done with a string rather than an hvo so that we can use it
		/// when the user is just testing different things, which might not even be real words,
		/// and certainly might not be in the WordformInventory yet.</remarks>
		/// <param name="form"></param>
		/// <param name="fDoTrace">whether or not to trace the parse</param>
		/// <param name="sSelectTraceMorphs">list of msa hvos to limit trace to </param>
		public void ScheduleOneWordformForTryAWord(string form, bool fDoTrace, string sSelectTraceMorphs)
		{
			CheckDisposed();
			bool haveLock = false;
			try
			{
				if(!Monitor.TryEnter(m_tryAWord_lock))
				{
					TraceMsg(lockingSwitch.TraceInfo, ">>>>>>>*****  collision locking Trace Word B ********<<<<<<<<<<<");
#if true
					Monitor.Enter(m_tryAWord_lock);
#else
					// This is here to handle the case where the worker thread has already come
					// through and has the lock, but is taking a very long time (seen several minute parses),
					// when that happens the main thread sit here and waits for the lock to be released.
					// This code will only wait a given time before returning to the application so it doesn't
					// get mistaken for a crashed / hung application.  A further refinement would be to only
					// wait if this is the thread of the application and not any helpers.
					if (!Monitor.TryEnter(m_traceWord_lock, new TimeSpan(0, 0, 10)))	// wait up to 10 seconds
					{
						return;
					}
#endif
				}
				haveLock = true;
				//if there is already a trace in there, this will just knock it out
				m_tryAWord= form;
				m_doTrace = fDoTrace;
				m_sSelectTraceMorphs = sSelectTraceMorphs;
				m_TryAWordEvent.Set();
			}
			finally
			{
				if (haveLock)
					Monitor.Exit(m_tryAWord_lock);
			}
		}

		public void ScheduleOneWordformForUpdate(int hvoWordform, Priority priority)
		{
			CheckDisposed();

////			lock(this)
////			{
				bool bAddedToQueue = true;	// default to true
				switch(priority)
				{
						//				case Priority.trace:
						//					m_traceQueue.EnqueueWordform(hvoWordform);
						//					break;
					case Priority.ASAP:
						//remove from lower priority queues
						m_lowQueue.RemoveWordformIfPresent(hvoWordform);
						m_mediumQueue.RemoveWordformIfPresent(hvoWordform);

						m_highQueue.EnqueueWordform(hvoWordform);
						break;
					case Priority.soon:
						//remove from lower priority queues
						m_lowQueue.RemoveWordformIfPresent(hvoWordform);

						//only add it if it is not already in a higher priority queue
						if(!m_highQueue.Contains(hvoWordform))
							m_mediumQueue.EnqueueWordform(hvoWordform);
						break;
					case Priority.eventually:
						//only add it if it is not already in a higher priority queue
						if(!(m_highQueue.Contains(hvoWordform) || m_mediumQueue.Contains(hvoWordform)))
						{
							m_lowQueue.EnqueueWordform(hvoWordform);
						}
						break;
					default:
						bAddedToQueue = false;
						break;
				}

				if (bAddedToQueue)
					m_QueueEvent.Set();		// set event
////			}
		}

		public void ScheduleWordformsForUpdate(int[] aiWordformHvos, Priority priority)
		{
			CheckDisposed();

			////			lock(this)
			////			{
#if DoTiming
			DateTime start = new DateTime();
			DateTime finish = new DateTime();
			start = DateTime.Now;
#endif
#if DoTiming
			finish = DateTime.Now;
			Trace.WriteLine("Collecting Unique Wordforms took " + (finish - start));
#endif
			foreach (int hvoWordform in aiWordformHvos)
			{
				ScheduleOneWordformForUpdate(hvoWordform, priority);
			}
			////			}
		}


		private void AddToQueue(int hvoWordform, M3ParserWordformQueue queue)
		{
			queue.EnqueueWordform(hvoWordform);
		}

		internal void HandleTaskUpdate(TaskReport task)
		{
			CheckDisposed();

			Trace.WriteLineIf(tracingSwitch.TraceInfo, task.Description + " " + task.PhaseDescription);

			if (ParserUpdateNormal != null && ((task.Depth == 0) || (task.NotificationMessage != null)))
			{
				//notify any delegates
				ParserUpdateNormal(this, task);
			}

			if (ParserUpdateVerbose != null)
			{
				//notify any delegates
				ParserUpdateVerbose(this, task.MostRecentTask /*not sure this is right*/);
			}


			System.Diagnostics.Trace.WriteLineIf(tracingSwitch.TraceInfo, task.Description);
		}

		public string Server
		{
			get
			{
				CheckDisposed();
				return m_server;
			}
		}
		public string Database
		{
			get
			{
				CheckDisposed();
				return m_database;
			}
		}
		public string LangProject
		{
			get
			{
				CheckDisposed();
				return m_LangProject;
			}
		}
		public string Parser
		{
			get
			{
				CheckDisposed();
				return m_parser;
			}
		}

		/// <summary>
		/// Check the paused-running state of the parser
		/// </summary>
		///<remarks>I initially tried using Suspend and Resume, but ran into deadlocks.
		///so now pausing works by grabbing a mutex; windy worker thread has completed a task,
		///it releases this mutex and then tries to grab it again before starting a new task.</remarks>
		public bool IsPaused
		{
			get
			{
				CheckDisposed();

				bool paused = false;
				try
				{
					if(!Monitor.TryEnter(m_pausedObject))
					{
						TraceMsg(lockingSwitch.TraceInfo, "=========== %%%%%%%%%%%%%%% >>>>>>>*****  collision locking parser paused A ********<<<<<<<<<<<=========== %%%%%%%%%%%%%%% ");
						Monitor.Enter(m_pausedObject);
					}
					paused = m_paused;	// dlh TESTING instead of using pause event

////					if (m_PauseEvent.WaitOne(1, false))
////						paused = true;		// We're currently paused...
////					else
////						m_PauseEvent.Reset();
				}
				finally
				{
					Monitor.Exit(m_pausedObject);
				}
				return paused;
			}
		}

		/// <summary>
		/// if there has been an error, this will return the exception that stopped it.
		/// </summary>
		public Exception ErrorException
		{
			get
			{
				CheckDisposed();

				return m_caughtException;
			}
		}

		private void TraceMsg(string msg)
		{
			Trace.WriteLine(msg, "ParserScheduler");
		}

		private void TraceMsg(bool doit, string msg)
		{
			Trace.WriteLineIf(doit, msg, "ParserScheduler");
		}

		/// <summary>
		/// try to pause the worker thread for a limited amount of time
		/// </summary>
		///<remarks>I initially tried using Suspend and Resume, but ran into deadlocks.
		///so now pausing works by grabbing a mutex; windy worker thread has completed a task,
		///</remarks>
		/// <returns>true if the pause was successful before the timeout occurred</returns>
		public bool AttemptToPause()
		{
			CheckDisposed();

			bool paused = false;
			try
			{
				if(!Monitor.TryEnter(m_pausedObject))
				{
					TraceMsg(lockingSwitch.TraceInfo, ">>>>>>>*****  collision locking parser paused B ********<<<<<<<<<<<");
					Monitor.Enter(m_pausedObject);
				}
				if(!m_paused)
				{
					m_PauseEvent.Set();
					m_paused = true;
					paused = true;
				}
			}
			finally
			{
				Monitor.Exit(m_pausedObject);
			}
			return paused;
		}

		/// <summary>
		/// Un-Pause and worker thread
		/// </summary>
		public void Resume()
		{
			CheckDisposed();

			try
			{
				if(!Monitor.TryEnter(m_pausedObject))
				{
					TraceMsg(lockingSwitch.TraceInfo, ">>>>>>>*****  collision locking parser paused C ********<<<<<<<<<<<");
					Monitor.Enter(m_pausedObject);
				}
				if (m_paused)
					m_PauseEvent.Reset();
				m_paused=false;
			}
			finally
			{
				Monitor.Exit(m_pausedObject);
			}
		}

		private void DebugMsg(string msg)
		{
			// create the initial info:
			// datetime threadid threadpriority: msg
			System.Text.StringBuilder msgOut = new System.Text.StringBuilder();
//			msgOut.Append(DateTime.Now.Ticks);
			msgOut.Append(DateTime.Now.ToString("HH:mm:ss.fff"));
			msgOut.Append("-");
			msgOut.Append(Thread.CurrentThread.GetHashCode());
			msgOut.Append("-");
			msgOut.Append(Thread.CurrentThread.Priority);
			msgOut.Append(": ");
			msgOut.Append(msg);
			System.Diagnostics.Trace.WriteLineIf(tracingSwitch.TraceInfo, msgOut.ToString());
		}
		/// <summary>
		/// Determine if the wordform is a single word or a phrase
		/// </summary>
		/// <param name="sWord">wordform</param>
		/// <returns>true if a single word; false otherwise</returns>
		public static bool IsOneWord(string sWord)
		{
			char[] acSpaceTab;
			acSpaceTab = new char[2] { ' ', '	' };
			int i = sWord.IndexOfAny(acSpaceTab);
			if ((i > -1) && (i < sWord.Length))
			{
				return false;
			}
			return true;
		}
	}
}
