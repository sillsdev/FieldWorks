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
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Threading;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.WordWorks.Parser;
using SIL.Utils;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// Handles acquiring a parser and safely subscribing to and receiving events from it.
	/// </summary>
	public sealed class ParserConnection : FwDisposableBase, IAsyncResult
	{
		private readonly ParserScheduler m_scheduler;

		private string m_activity;
		private string m_notificationMessage;
		private string m_traceResult;
		private readonly ManualResetEvent m_event = new ManualResetEvent(false);

		private readonly object m_syncRoot = new object();

		/// <summary>
		/// Initializes a new instance of the <see cref="ParserConnection"/> class.
		/// This will attempt to connect to an existing parser or start a new one if necessary.
		/// </summary>
		public ParserConnection(FdoCache cache, IdleQueue idleQueue)
		{
			m_activity = "";
			m_scheduler = new ParserScheduler(cache, idleQueue, FwDirectoryFinder.CodeDirectory);
			m_scheduler.ParserUpdateVerbose += ParserUpdateHandlerForPolling;
		}

		private object SyncRoot
		{
			get
			{
				return m_syncRoot;
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
		public IAsyncResult BeginTryAWord(string sForm, bool fDoTrace)
		{
			return BeginTryAWord(sForm, fDoTrace, null);
		}

		/// <summary>
		/// place an asynchronous request for tracing the word
		/// </summary>
		/// <param name="sForm">The word form to be parsed</param>
		/// <param name="fDoTrace">whether a trace is to be run or not</param>
		/// <param name="sSelectTraceMorphs">list of msa hvos to limit trace to </param>
		public IAsyncResult BeginTryAWord(string sForm, bool fDoTrace, string sSelectTraceMorphs)
		{
			CheckDisposed();

			//the result will be caught by our event handler and be accessible from the
			//TraceResult property
			lock (SyncRoot)
			{
				m_traceResult = null;
				m_event.Reset();
			}
			m_scheduler.ScheduleOneWordformForTryAWord(sForm, fDoTrace, sSelectTraceMorphs);
			return this;
		}

		public void UpdateWordforms(IEnumerable<IWfiWordform> wordforms, ParserPriority priority)
		{
			CheckDisposed();
			m_scheduler.ScheduleWordformsForUpdate(wordforms, priority);
		}

		public void UpdateWordform(IWfiWordform wordform, ParserPriority priority)
		{
			CheckDisposed();
			m_scheduler.ScheduleOneWordformForUpdate(wordform, priority);
		}

		public Exception UnhandledException
		{
			get
			{
				CheckDisposed();
				return m_scheduler.UnhandledException;
			}
		}

		public int GetQueueSize(ParserPriority priority)
		{
			CheckDisposed();
			return m_scheduler.GetQueueSize(priority);
		}

		public void ReloadGrammarAndLexicon()
		{
			CheckDisposed();
			m_scheduler.ReloadGrammarAndLexicon();
		}

		protected override void DisposeManagedResources()
		{
				// Remove event handlers.
				m_scheduler.ParserUpdateVerbose -= ParserUpdateHandlerForPolling;
				m_scheduler.Dispose();

				m_event.Close();
				((IDisposable)m_event).Dispose();
			}

		/// <summary>
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="args">The <see cref="ParserUpdateEventArgs"/> instance containing the event data.</param>
		public void ParserUpdateHandlerForPolling(object sender, ParserUpdateEventArgs args)
		{
			CheckDisposed();

			lock (SyncRoot)
			{
				//store this for clients which just want to poll us, instead of wiring up to the event
				m_activity = args.Task.Description;
				//keeps us from getting the notification at the end of the task.
				if (args.Task.NotificationMessage != null && args.Task.Phase != TaskReport.TaskPhase.Finished)
					m_notificationMessage = args.Task.NotificationMessage;

				//will have to do something more smart something when details is used for something else
				if (args.Task.Details != null)
				{
					m_traceResult = args.Task.Details;
					m_event.Set();
				}
			}
		}

		/// <summary>
		/// returns a string describing what the Parser is up to.
		/// Note that, alternatively, you can  subscribe to events so that you get every one.
		/// </summary>
		/// <value>The activity.</value>
		public string Activity
		{
			get
			{
				CheckDisposed();

				lock (SyncRoot)
					return m_activity;
			}
		}

		/// <summary>
		/// gives a notification string, if there is any.
		/// </summary>
		/// <returns></returns>
		public string GetAndClearNotification()
		{
			CheckDisposed();

			lock (SyncRoot)
			{
				string result = m_notificationMessage;
				m_notificationMessage = null;
				return result;
			}
		}

		#region Implementation of IAsyncResult

		bool IAsyncResult.IsCompleted
		{
			get
			{
				lock (SyncRoot)
					return m_traceResult != null;
			}
		}

		WaitHandle IAsyncResult.AsyncWaitHandle
		{
			get { return m_event; }
		}

		object IAsyncResult.AsyncState
		{
			get
			{
				lock (SyncRoot)
				{
					string res = m_traceResult;
					m_traceResult = null;
					return res;
				}
			}
		}

		bool IAsyncResult.CompletedSynchronously
		{
			get { return false; }
		}

		#endregion
	}
}
