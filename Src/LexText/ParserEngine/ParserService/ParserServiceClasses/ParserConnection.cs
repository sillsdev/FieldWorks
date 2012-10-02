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
// File: ParserConnection.cs
// Responsibility: John Hatton
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;	// for Monitor and locking code

using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.WordWorks.Parser
{
	/// <summary>
	/// Handles acquiring a parser and safely subscribing to and receiving events from it.
	/// </summary>
	public sealed class ParserConnection : MarshalByRefObject, IFWDisposable
	{
		private ParserScheduler m_scheduler;
		private ParserUpdateEventHandler m_localTaskReportHandler;
		private ParserUpdateEventHandler m_clientTaskReportHandler;
		private Exception m_currentError;
		private string m_activity;
		private string m_notificationMessage;
		// Protect m_traceResult as it can be accessed from different threads.
		// Variable to lock on as m_traceResult can be null.
		private string m_traceResult;
		private static string m_traceResultKEY = ParserServiceStrings.ksInitializing;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ParserConnection"/> class.
		/// This will attempt to connect to an existing parser or start a new one if necessary.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		// per KenZ, convention is for server to already include the \\SILFW,e.g. HATTON1\\SILFW
		public ParserConnection(string sServer, String sDatabase, String sLangProj, bool fParseAllWordforms)
		{
			m_activity = "";
			m_scheduler = ParserFactory.GetDefaultParser(sServer, sDatabase, sLangProj);
			m_scheduler.ParseAllWordforms = fParseAllWordforms;
			m_scheduler.ParserUpdateVerbose += ParserUpdateHandlerForPolling;
		}
		/// <summary>
		/// Set lifetime of the remoting object to infinite so we don't lose the connection (fixes LT-8597 and LT-8619)
		/// </summary>
		/// <returns></returns>
		public override object InitializeLifetimeService()
		{
			return null;
		}

		// Use this property so that the underlying variable can be protected from concurrent usage.
		public string TraceResult
		{
			get
			{
				CheckDisposed();

				lock (m_traceResultKEY)
				{
					return m_traceResult;
				}
// This code will log a msg to the debug output if there is a collision
//				try
//				{
//					if(!Monitor.TryEnter(m_traceResultKEY))
//					{
//						Debug.WriteLine(">>>>>>>*****  This would have been a collision : ParserConnection::TraceResult::Get ********<<<<<<<<<<<");
//						Monitor.Enter(m_traceResultKEY);
//					}
//					return m_traceResult;
//				}
//				finally
//				{
//					Monitor.Exit(m_traceResultKEY);
//				}
			}
			set
			{
				CheckDisposed();

				lock (m_traceResultKEY)
				{
					m_traceResult = value;
				}
				// This code will log a msg to the debug output if there is a collision
				//				try
				//				{
				//					if(!Monitor.TryEnter(m_traceResultKEY))
				//					{
				//						Debug.WriteLine(">>>>>>>*****  This would have been a collision : ParserConnection::TraceResult::Set ********<<<<<<<<<<<");
				//						Monitor.Enter(m_traceResultKEY);
				//					}
				//					m_traceResult = value;
				//				}
				//				finally
				//				{
				//					Monitor.Exit(m_traceResultKEY);
				//				}
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
				return m_scheduler.ParseAllWordforms;
			}
			set
			{
				CheckDisposed();
				m_scheduler.ParseAllWordforms = value;
			}
		}
		/// <summary>
		/// Get or Set state for the Try A Word dialog running
		/// </summary>
		public bool TryAWordDialogIsRunning
		{
			get
			{
				CheckDisposed();
				return m_scheduler.TryAWordDialogIsRunning;
			}
			set
			{
				CheckDisposed();
				m_scheduler.TryAWordDialogIsRunning = value;
			}
		}
		/// <summary>
		/// place an asynchronous request for tracing the word
		/// </summary>
		/// <param name="sForm">The word form to be parsed</param>
		/// <param name="fDoTrace">whether a trace is to be run or not</param>
		public void TryAWordAsynchronously(string sForm, bool fDoTrace)
		{
			TryAWordAsynchronously(sForm, fDoTrace, null);
		}

		/// <summary>
		/// place an asynchronous request for tracing the word
		/// </summary>
		/// <param name="sForm">The word form to be parsed</param>
		/// <param name="fDoTrace">whether a trace is to be run or not</param>
		/// <param name="sSelectTraceMorphs">list of msa hvos to limit trace to </param>
		public void TryAWordAsynchronously(string sForm, bool fDoTrace, string sSelectTraceMorphs)
		{
			CheckDisposed();

			//the result will be caught by our event handler and be accessible from the
			//TraceResult property
			TraceResult = null;
			this.m_scheduler.ScheduleOneWordformForTryAWord(sForm, fDoTrace, sSelectTraceMorphs);
		}

		/// <summary>
		/// This will cause a handler to be invoked, in the client's thread,
		/// for all events that the parser produces.
		/// </summary>
		/// <param name="handler"></param>
		public void SubscribeToParserEvents(bool verbose, ParserUpdateEventHandler handler)
		{
			CheckDisposed();

			//we don't directly wire up the parser to the client's handler.
			//instead, we wire up a local handler which general another job of
			//safely calling the clients Handler, which will be in a different thread at that point.

			Debug.Assert(handler.Target is System.Windows.Forms.Control,"Currently, you can only subscribe with a Windows forms control.");
			Debug.Assert(m_localTaskReportHandler == null, "You need to unsubscribe before subscribing again.");
			m_localTaskReportHandler = ParserUpdateHandler;
			m_clientTaskReportHandler = handler;

			if (verbose)
				m_scheduler.ParserUpdateVerbose += m_localTaskReportHandler;
			else
				m_scheduler.ParserUpdateNormal += m_localTaskReportHandler;
		}

		public void UnsubscribeToParserEvents()
		{
			CheckDisposed();

			m_clientTaskReportHandler = null;
			if (m_scheduler != null)
			{
				//first, we remove our own event handlers
				m_scheduler.AttemptToPause();
				if (m_scheduler.IsPaused)
				{
#if IsThisNeeded
					// This is not what we want: it will also disable the handlers that report the progress in the main app status bar
					m_scheduler.ParserUpdateVerbose -= ParserUpdateHandlerForPolling;
					m_scheduler.ParserUpdateNormal -= ParserUpdateHandlerForPolling;
#endif

					if (m_localTaskReportHandler != null)
					{
						m_scheduler.ParserUpdateVerbose -= m_localTaskReportHandler;
						m_scheduler.ParserUpdateNormal -= m_localTaskReportHandler;
						m_localTaskReportHandler = null;
					}
				}
				m_scheduler.Resume();
			}
		}

		/// <summary>
		/// this may become hidden someday and replaced
		///  with all of the public methods that a client would need to access on the parser.
		/// </summary>
		public ParserScheduler Parser
		{
			get
			{
				CheckDisposed();
				return m_scheduler;
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
		~ParserConnection()
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

			m_clientTaskReportHandler = null;

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_scheduler != null)
				{
					/* Don't know why it tries this, since the Dispose method in the scheduler calls Resume().
					m_scheduler.AttemptToPause();
					while (!m_scheduler.IsPaused)
					{
						// How long should we wait?
						// Or, should we put up a pacifier to let the user knwo we are trying to shut down the parser/
					}
					*/

					// Remove event handlers.
					m_scheduler.ParserUpdateVerbose -= ParserUpdateHandlerForPolling;
					m_scheduler.ParserUpdateNormal -= ParserUpdateHandlerForPolling;
					if (m_localTaskReportHandler != null)
					{
						m_scheduler.ParserUpdateVerbose -= m_localTaskReportHandler;
						m_scheduler.ParserUpdateNormal -= m_localTaskReportHandler;
					}
					ParserFactory.ReleaseScheduler(m_scheduler); // ReleaseScheduler calls Dispose on m_scheduler.
				}
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_localTaskReportHandler = null;
			m_scheduler = null;
			m_currentError = null;
			m_activity = null;
			TraceResult = null;
			m_notificationMessage = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		/// <summary>
		/// </summary>
		/// <param name="parser"></param>
		/// <param name="task"></param>
		public void ParserUpdateHandlerForPolling(ParserScheduler parser, TaskReport task)
		{
			CheckDisposed();

			//store this for clients which just want to poll us, instead of wiring up to the event
			m_activity = task.Description;
			m_currentError = task.CurrentError;
			if(task.NotificationMessage!=null
				&& task.Phase != TaskReport.TaskPhase.finished )//keeps us from getting the notification at the end of the task.
				m_notificationMessage = task.NotificationMessage;

			//will have to do something more smart something when details is used for something else
			if(task.Details != null)
				TraceResult = task.Details;

			if (m_currentError!= null)
				System.Windows.Forms.MessageBox.Show(m_currentError.Message,
					ParserServiceStrings.ksParserProblem,
					MessageBoxButtons.OK,MessageBoxIcon.Exclamation);
		}

		/// <summary>
		/// this will be called from the parser's thread; we need to marshal it to the client's thread
		/// so that it comes in as a normal event in the event handling loop of the client.
		/// </summary>
		/// <param name="parser"></param>
		/// <param name="task"></param>
		public void ParserUpdateHandler(ParserScheduler parser, TaskReport task)
		{
			CheckDisposed();

			if(null == m_clientTaskReportHandler)
				return;

			lock(this)
			{
				//using Invoke () here would cause this thread to wait until the event was handled.
				((Control)m_clientTaskReportHandler.Target).BeginInvoke(m_clientTaskReportHandler, new object[] {parser, task});
			}
			//testing

			//((Control)m_clientTaskReportHandler.Target).Invoke(m_clientTaskReportHandler, new object[] {parser, task});
		}

		/// <summary>
		/// returns a string describing what the Parser is up to.
		/// Note that, alternatively, you can  subscribe to events so that you get every one.
		/// </summary>
		public string Activity
		{
			get
			{
				CheckDisposed();

				if(this.IsPaused)
					return ParserServiceStrings.ksPaused;
				else
					return m_activity;
			}
		}

		/// <summary>
		/// client should pull this after calling TryAWordAsynchronously
		///
		/// (DLH) - This method has been enhanced to handle the current
		/// implementation where the client and parser are running on seperate
		/// threads and trying to share the m_traceResult variable.  The addition
		/// of the wrapper and the Monitor code here will keep the threads from
		/// clobbering each other's data.  This should also be considered for
		/// the other member variables that are shared among threads.
		/// </summary>
		public string GetAndClearTraceResult()
		{
			CheckDisposed();

			lock(m_traceResultKEY)
			{
				string result = TraceResult;
				TraceResult = null;
				return result;
			}
		}

		/// <summary>
		/// gives a notification string, if there is any.
		/// </summary>
		/// <returns></returns>
		public string GetAndClearNotification()
		{
			CheckDisposed();

			string result = m_notificationMessage;
			m_notificationMessage= null;
			return result;
		}

		/// <summary>
		/// Controls the paused-running state of the parser
		/// </summary>
		public bool IsPaused
		{
			get
			{
				CheckDisposed();

				return m_scheduler.IsPaused;
			}
		}

		/// <summary>
		/// try to pause for a limited amount of time
		/// </summary>
		/// <returns>true if the pause was successful before the timeout occurred</returns>
		public bool AttemptToPause()
		{
			CheckDisposed();

			return m_scheduler.AttemptToPause();
		}

		/// <summary>
		/// Un-Pause the Parser
		/// </summary>
		public void Resume()
		{
			CheckDisposed();

			m_scheduler.Resume();
		}
	}
}
