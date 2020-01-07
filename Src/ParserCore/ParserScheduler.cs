// Copyright (c) 2002-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Utils;
using SIL.ObjectModel;

namespace SIL.FieldWorks.WordWorks.Parser
{
	/// <summary />
	public sealed class ParserScheduler : DisposableBase
	{
		private abstract class ParserWork
		{
			protected readonly ParserScheduler m_scheduler;
			protected readonly ParserPriority m_priority;

			protected ParserWork(ParserScheduler scheduler, ParserPriority priority)
			{
				m_scheduler = scheduler;
				m_priority = priority;
				m_scheduler.IncrementQueueCount(m_priority);
			}

			public virtual void DoWork()
			{
				m_scheduler.DecrementQueueCount(m_priority);
			}
		}

		private sealed class TryAWordWork : ParserWork
		{
			private readonly string m_tryAWord;
			private readonly bool m_doTrace;
			private readonly int[] m_selectTraceMorphs;

			public TryAWordWork(ParserScheduler scheduler, string tryAWord, bool doTrace, int[] selectTraceMorphs)
				: base(scheduler, ParserPriority.TryAWord)
			{
				m_tryAWord = tryAWord;
				m_doTrace = doTrace;
				m_selectTraceMorphs = selectTraceMorphs;
			}

			public override void DoWork()
			{
				if (m_scheduler.m_tryAWordDialogRunning)
				{
					m_scheduler.m_parserWorker.TryAWord(m_tryAWord, m_doTrace, m_selectTraceMorphs);
				}
				base.DoWork();
			}
		}

		private sealed class UpdateWordformWork : ParserWork
		{
			private readonly IWfiWordform m_wordform;

			public UpdateWordformWork(ParserScheduler scheduler, ParserPriority priority, IWfiWordform wordform)
				: base(scheduler, priority)
			{
				m_wordform = wordform;
			}

			public override void DoWork()
			{
				if (!m_scheduler.m_parserWorker.UpdateWordform(m_wordform, m_priority))
				{
					// this wordform was skipped
					base.DoWork();
				}
			}
		}

		private sealed class ReloadGrammarAndLexiconWork : ParserWork
		{
			public ReloadGrammarAndLexiconWork(ParserScheduler scheduler)
				: base(scheduler, ParserPriority.ReloadGrammarAndLexicon)
			{
			}

			public override void DoWork()
			{
				m_scheduler.m_parserWorker.ReloadGrammarAndLexicon();
				base.DoWork();
			}
		}

		public event EventHandler<ParserUpdateEventArgs> ParserUpdateVerbose;
		public event EventHandler<ParserUpdateEventArgs> ParserUpdateNormal;

		private readonly ConsumerThread<ParserPriority, ParserWork> m_thread;
		private ParserWorker m_parserWorker;
		private readonly int[] m_queueCounts = new int[5];
		private volatile bool m_tryAWordDialogRunning;
		private TaskReport m_TaskReport;

		/// <summary />
		public ParserScheduler(LcmCache cache, IdleQueue idleQueue, string dataDir)
		{
			m_parserWorker = new ParserWorker(cache, HandleTaskUpdate, idleQueue, dataDir);
			m_parserWorker.ParseFiler.WordformUpdated += ParseFiler_WordformUpdated;

			m_thread = new ConsumerThread<ParserPriority, ParserWork>(Work);
			ReloadGrammarAndLexicon();
			m_thread.Start();
		}

		/// <summary>
		/// Gets the unhandled exception.
		/// </summary>
		/// <value>The unhandled exception.</value>
		public Exception UnhandledException => m_thread.UnhandledException;

		/// <summary>
		/// Gets the synchronization root. This is the object that should be
		/// used for all locking in this scheduler.
		/// </summary>
		/// <value>The synchronization root.</value>
		private object SyncRoot { get; } = new object();

		/// <summary>
		/// Get or Set state for the try A Word dialog running
		/// </summary>
		public bool TryAWordDialogIsRunning
		{
			get
			{
				return m_tryAWordDialogRunning;
			}
			set
			{
				m_tryAWordDialogRunning = value;
				if (!value)
				{
					// wake up the thread so that it can process any queued wordforms
					m_thread.WakeUp();
				}
			}
		}

		protected override void DisposeManagedResources()
		{
			m_thread.Stop();
			m_thread.Dispose();

			if (m_parserWorker != null)
			{
				m_parserWorker.ParseFiler.WordformUpdated -= ParseFiler_WordformUpdated;
				m_parserWorker.Dispose();
				m_parserWorker = null;
			}

			if (m_TaskReport != null)
			{
				m_TaskReport.Dispose();
				m_TaskReport = null;
			}
		}

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + " ******");
			base.Dispose(disposing);
		}

		/// <summary>
		/// Reload Grammar And Lexicon
		/// </summary>
		public void ReloadGrammarAndLexicon()
		{
			m_thread.EnqueueWork(ParserPriority.ReloadGrammarAndLexicon, new ReloadGrammarAndLexiconWork(this));
		}

		private void Work(IQueueAccessor<ParserPriority, ParserWork> queueAccessor)
		{
			ParserWork work;
			if (queueAccessor.GetNextWorkItem(out work))
			{
				work.DoWork();
			}
		}

		/// <summary>
		/// returns the number of the Wordforms in the low priority queue.
		/// </summary>
		public int GetQueueSize(ParserPriority priority)
		{
			lock (SyncRoot)
			{
				return m_queueCounts[(int)priority];
			}
		}

		private void IncrementQueueCount(ParserPriority priority)
		{
			lock (SyncRoot)
			{
				m_queueCounts[(int)priority]++;
			}
		}

		private void DecrementQueueCount(ParserPriority priority)
		{
			bool isIdle;
			lock (SyncRoot)
			{
				m_queueCounts[(int)priority]--;
				isIdle = m_queueCounts[(int)ParserPriority.TryAWord] == 0
						 && m_queueCounts[(int)ParserPriority.Low] == 0
						 && m_queueCounts[(int)ParserPriority.Medium] == 0
						 && m_queueCounts[(int)ParserPriority.High] == 0;
			}
			if (isIdle && (m_TaskReport == null || m_TaskReport.Description == ParserCoreStrings.ksIdle_))
			{
				m_TaskReport?.Dispose();
				m_TaskReport = new TaskReport(ParserCoreStrings.ksIdle_, HandleTaskUpdate);
			}
		}

		/// <summary />
		/// <remarks> this is done with a string rather than an hvo so that we can use it
		/// when the user is just testing different things, which might not even be real words,
		/// and certainly might not be in the WordformInventory yet.</remarks>
		public void ScheduleOneWordformForTryAWord(string form, bool fDoTrace, int[] sSelectTraceMorphs)
		{
			m_thread.EnqueueWork(ParserPriority.TryAWord, new TryAWordWork(this, form, fDoTrace, sSelectTraceMorphs));
		}

		public void ScheduleOneWordformForUpdate(IWfiWordform wordform, ParserPriority priority)
		{
			m_thread.EnqueueWork(priority, new UpdateWordformWork(this, priority, wordform));
		}

		public void ScheduleWordformsForUpdate(IEnumerable<IWfiWordform> wordforms, ParserPriority priority)
		{
			foreach (var wordform in wordforms)
			{
				ScheduleOneWordformForUpdate(wordform, priority);
			}
		}

		private void HandleTaskUpdate(TaskReport task)
		{
			if (IsDisposed)
			{
				return;
			}
			if (ParserUpdateNormal != null && ((task.Depth == 0) || (task.NotificationMessage != null)))
			{
				//notify any delegates
				ParserUpdateNormal(this, new ParserUpdateEventArgs(task));
			}

			//notify any delegates
			ParserUpdateVerbose?.Invoke(this, new ParserUpdateEventArgs(task.MostRecentTask)/*not sure this is right*/);
		}

		private void ParseFiler_WordformUpdated(object sender, WordformUpdatedEventArgs e)
		{
			DecrementQueueCount(e.Priority);
		}

		/// <summary>
		/// Determine if the wordform is a single word or a phrase
		/// </summary>
		/// <param name="sWord">wordform</param>
		/// <returns>true if a single word; false otherwise</returns>
		public static bool IsOneWord(string sWord)
		{
			var acSpaceTab = new[] { ' ', '	' };
			var i = sWord.IndexOfAny(acSpaceTab);
			return (i <= -1) || (i >= sWord.Length);
		}
	}
}