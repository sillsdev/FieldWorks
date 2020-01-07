// Copyright (c) 2002-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Xml.Linq;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.WordWorks.Parser;
using SIL.LCModel;
using SIL.ObjectModel;

namespace LanguageExplorer.Areas.TextsAndWords
{
	/// <summary>
	/// Handles acquiring a parser and safely subscribing to and receiving events from it.
	/// </summary>
	internal sealed class ParserConnection : DisposableBase, IAsyncResult
	{
		private readonly ParserScheduler m_scheduler;
		private string m_activity;
		private string m_notificationMessage;
		private XDocument m_traceResult;
		private readonly ManualResetEvent m_event = new ManualResetEvent(false);

		/// <summary />
		public ParserConnection(LcmCache cache, IdleQueue idleQueue)
		{
			m_activity = string.Empty;
			m_scheduler = new ParserScheduler(cache, idleQueue, Path.Combine(FwDirectoryFinder.CodeDirectory, FwDirectoryFinder.ksFlexFolderName));
			m_scheduler.ParserUpdateVerbose += ParserUpdateHandlerForPolling;
		}

		private object SyncRoot { get; } = new object();

		/// <summary>
		/// Get or Set state for the Try A Word dialog running
		/// </summary>
		public bool TryAWordDialogIsRunning
		{
			get
			{
				return m_scheduler.TryAWordDialogIsRunning;
			}
			set
			{
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
		public IAsyncResult BeginTryAWord(string sForm, bool fDoTrace, int[] sSelectTraceMorphs)
		{
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
			m_scheduler.ScheduleWordformsForUpdate(wordforms, priority);
		}

		public void UpdateWordform(IWfiWordform wordform, ParserPriority priority)
		{
			m_scheduler.ScheduleOneWordformForUpdate(wordform, priority);
		}

		public Exception UnhandledException => m_scheduler.UnhandledException;

		public int GetQueueSize(ParserPriority priority)
		{
			return m_scheduler.GetQueueSize(priority);
		}

		public void ReloadGrammarAndLexicon()
		{
			m_scheduler.ReloadGrammarAndLexicon();
		}

		protected override void DisposeManagedResources()
		{
			// Remove event handlers.
			m_scheduler.ParserUpdateVerbose -= ParserUpdateHandlerForPolling;
			m_scheduler.Dispose();

			m_event.Close();
			m_event.Dispose();
		}

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + " ******");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			base.Dispose(disposing);
		}

		/// <summary />
		public void ParserUpdateHandlerForPolling(object sender, ParserUpdateEventArgs args)
		{
			lock (SyncRoot)
			{
				//store this for clients which just want to poll us, instead of wiring up to the event
				m_activity = args.Task.Description;
				//keeps us from getting the notification at the end of the task.
				if (args.Task.NotificationMessage != null && args.Task.Phase != TaskPhase.Finished)
				{
					m_notificationMessage = args.Task.NotificationMessage;
				}
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
		public string Activity
		{
			get
			{
				lock (SyncRoot)
				{
					return m_activity;
				}
			}
		}

		/// <summary>
		/// gives a notification string, if there is any.
		/// </summary>
		public string GetAndClearNotification()
		{
			lock (SyncRoot)
			{
				var result = m_notificationMessage;
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
				{
					return m_traceResult != null;
				}
			}
		}

		WaitHandle IAsyncResult.AsyncWaitHandle => m_event;

		object IAsyncResult.AsyncState
		{
			get
			{
				lock (SyncRoot)
				{
					var res = m_traceResult;
					m_traceResult = null;
					return res;
				}
			}
		}

		bool IAsyncResult.CompletedSynchronously => false;

		#endregion
	}
}