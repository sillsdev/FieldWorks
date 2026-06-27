// Copyright (c) 2002-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// <remarks>
//	this class implements a worker thread.
// </remarks>

using System;
using System.Collections.Generic;
using System.Linq;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel.Utils;
using SIL.LCModel;
using SIL.ObjectModel;
using XCore;
using System.Threading;

namespace SIL.FieldWorks.WordWorks.Parser
{
	/// <summary>
	/// The parser queue priority
	/// </summary>
	public enum ParserPriority
	{
		ReloadGrammarAndLexicon = 0,
		TryAWord = 1,
		High = 2,
		Medium = 3,
		Low = 4
	};

	/// <summary>
	/// The event args for the ParserUpdate events.
	/// </summary>
	public class ParserUpdateEventArgs : EventArgs
	{
		public ParserUpdateEventArgs(TaskReport task)
		{
			Task = task;
		}

		public TaskReport Task
		{
			get; private set;
		}
	}

	/// <summary>
	///
	/// </summary>
	public sealed class ParserScheduler : DisposableBase
	{
		abstract class ParserWork
		{
			protected readonly ParserScheduler m_scheduler;
			protected readonly ParserPriority m_priority;
			// The number of wordforms this work item represents. The queue counts track
			// wordforms (not work items) so the "Queue: low/med/high" display and the idle
			// detection stay meaningful when many wordforms are batched into one work item.
			private readonly int m_queueCount;

			protected ParserWork(ParserScheduler scheduler, ParserPriority priority, int queueCount = 1)
			{
				m_scheduler = scheduler;
				m_priority = priority;
				m_queueCount = queueCount;
				m_scheduler.IncrementQueueCount(m_priority, m_queueCount);
			}

			public virtual void DoWork()
			{
				// This undoes the IncrementQueueCount above.
				// Subclasses should always call base.DoWork().
				// Nobody else should call IncrementQueueCount or DecrementQueueCount.
				m_scheduler.DecrementQueueCount(m_priority, m_queueCount);
			}
		}

		class TryAWordWork : ParserWork
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
					m_scheduler.m_parserWorker.TryAWord(m_tryAWord, m_doTrace, m_selectTraceMorphs);
				base.DoWork();
			}
		}

		class UpdateWordformWork : ParserWork
		{
			private readonly IWfiWordform m_wordform;
			private readonly bool m_checkParser;

			public UpdateWordformWork(ParserScheduler scheduler, ParserPriority priority, IWfiWordform wordform, bool checkParser)
				: base(scheduler, priority)
			{
				m_wordform = wordform;
				m_checkParser = checkParser;
			}

			public override void DoWork()
			{
				m_scheduler.m_parserWorker.ParseAndUpdateWordform(m_wordform, m_priority, m_checkParser);
				base.DoWork();
			}
		}

		/// <summary>
		/// A batch of wordforms parsed together. The parses may run concurrently (for
		/// thread-safe parsers); filing of results stays on the existing serial idle-queue path.
		/// </summary>
		class UpdateWordformsWork : ParserWork
		{
			private readonly IList<IWfiWordform> m_wordforms;
			private readonly bool m_checkParser;
			private readonly int m_maxDegreeOfParallelism;

			public UpdateWordformsWork(ParserScheduler scheduler, ParserPriority priority, IList<IWfiWordform> wordforms, bool checkParser, int maxDegreeOfParallelism)
				: base(scheduler, priority, wordforms.Count)
			{
				m_wordforms = wordforms;
				m_checkParser = checkParser;
				m_maxDegreeOfParallelism = maxDegreeOfParallelism;
			}

			public override void DoWork()
			{
				m_scheduler.m_parserWorker.ParseAndUpdateWordforms(m_wordforms, m_priority, m_checkParser, m_maxDegreeOfParallelism);
				base.DoWork();
			}
		}

		class ReloadGrammarAndLexiconWork : ParserWork
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
		public event EventHandler<WordformUpdatedEventArgs> WordformUpdated;

		private readonly ConsumerThread<ParserPriority, ParserWork> m_thread;
		private ParserWorker m_parserWorker;
		private readonly object m_syncRoot = new object();
		private readonly int[] m_queueCounts = new int[5];
		private volatile bool m_tryAWordDialogRunning;
		private TaskReport m_TaskReport;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ParserScheduler"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public ParserScheduler(LcmCache cache, PropertyTable propertyTable, IdleQueue idleQueue, string dataDir)
		{
			m_parserWorker = new ParserWorker(cache, propertyTable, HandleTaskUpdate, idleQueue, dataDir);
			m_parserWorker.ParseFiler.WordformUpdated += ParseFiler_WordformUpdated;

			m_thread = new ConsumerThread<ParserPriority, ParserWork>(Work);
			ReloadGrammarAndLexicon();
			m_thread.Start();
		}

		/// <summary>
		/// Gets the unhandled exception.
		/// </summary>
		/// <value>The unhandled exception.</value>
		public Exception UnhandledException
		{
			get
			{
				return m_thread.UnhandledException;
			}
		}

		/// <summary>
		/// Gets the synchronization root. This is the object that should be
		/// used for all locking in this scheduler.
		/// </summary>
		/// <value>The synchronization root.</value>
		private object SyncRoot
		{
			get
			{
				return m_syncRoot;
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
				return m_tryAWordDialogRunning;
			}
			set
			{
				CheckDisposed();
				m_tryAWordDialogRunning = value;
				if (!value)
					// wake up the thread so that it can process any queued wordforms
					m_thread.WakeUp();
			}
		}

		protected override void DisposeManagedResources()
		{
			// Dispose the managed resources in a separate thread
			// so that the user gets control back right away.
			System.Threading.Tasks.Task.Run(() =>
			{
				FinishDisposeManagedResources();
			});
		}

		private void FinishDisposeManagedResources()
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

		/// <summary>
		/// Reload Grammar And Lexicon
		/// </summary>
		public void ReloadGrammarAndLexicon()
		{
			CheckDisposed();

			m_thread.EnqueueWork(ParserPriority.ReloadGrammarAndLexicon, new ReloadGrammarAndLexiconWork(this));
		}

		private void Work(IQueueAccessor<ParserPriority, ParserWork> queueAccessor)
		{
			ParserWork work;
			if (queueAccessor.GetNextWorkItem(out work))
				work.DoWork();
		}

		/// <summary>
		/// returns the number of the Wordforms in the low priority queue.
		/// </summary>
		public int GetQueueSize(ParserPriority priority)
		{
			CheckDisposed();

			lock (SyncRoot)
				return m_queueCounts[(int) priority];
		}

		private void IncrementQueueCount(ParserPriority priority, int count = 1)
		{
			lock (SyncRoot)
				m_queueCounts[(int) priority] += count;
		}

		private void DecrementQueueCount(ParserPriority priority, int count = 1)
		{
			bool isIdle;
			lock (SyncRoot)
			{
				m_queueCounts[(int) priority] -= count;
				isIdle = m_queueCounts[(int)ParserPriority.TryAWord] == 0
						 && m_queueCounts[(int)ParserPriority.Low] == 0
						 && m_queueCounts[(int)ParserPriority.Medium] == 0
						 && m_queueCounts[(int)ParserPriority.High] == 0;
			}
			if (isIdle && (m_TaskReport == null || m_TaskReport.Description == ParserCoreStrings.ksIdle_))
			{
				if (m_TaskReport != null)
					m_TaskReport.Dispose();
				m_TaskReport = new TaskReport(ParserCoreStrings.ksIdle_, HandleTaskUpdate);
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
		public void ScheduleOneWordformForTryAWord(string form, bool fDoTrace, int[] sSelectTraceMorphs)
		{
			CheckDisposed();

			m_thread.EnqueueWork(ParserPriority.TryAWord, new TryAWordWork(this, form, fDoTrace, sSelectTraceMorphs));
		}

		public void ScheduleOneWordformForUpdate(IWfiWordform wordform, ParserPriority priority, bool checkParser)
		{
			CheckDisposed();

			m_thread.EnqueueWork(priority, new UpdateWordformWork(this, priority, wordform, checkParser));
		}

		/// <summary>
		/// Number of bounded chunks-worth of parallel "waves" packed into a single batch work
		/// item. Larger amortizes the cost of starting a parallel loop; smaller lets an
		/// interactive Try-A-Word (higher priority) or grammar reload preempt sooner.
		/// </summary>
		private const int ParseChunkMultiplier = 8;

		public void ScheduleWordformsForUpdate(IEnumerable<IWfiWordform> wordforms, ParserPriority priority, bool checkParser)
		{
			CheckDisposed();

			// Materialize once: callers commonly pass lazy queries (AllInstances(), Union(), ...).
			IList<IWfiWordform> wordformList = wordforms as IList<IWfiWordform> ?? wordforms.ToList();
			if (wordformList.Count == 0)
				return;

			int maxDegreeOfParallelism = m_parserWorker.MaxDegreeOfParallelism;
			// Split into bounded chunks and enqueue one batch work item per chunk. The work
			// items go through the same priority queue, so a Try-A-Word (priority TryAWord) or
			// a reload (priority ReloadGrammarAndLexicon) still preempts between chunks, and
			// Stop()/Dispose only has to wait for the current chunk. A non-parallel parser keeps
			// one wordform per work item, exactly as before.
			int chunkSize = maxDegreeOfParallelism <= 1 ? 1 : maxDegreeOfParallelism * ParseChunkMultiplier;

			for (int start = 0; start < wordformList.Count; start += chunkSize)
			{
				int count = Math.Min(chunkSize, wordformList.Count - start);
				var chunk = new List<IWfiWordform>(count);
				for (int i = 0; i < count; i++)
					chunk.Add(wordformList[start + i]);
				m_thread.EnqueueWork(priority, new UpdateWordformsWork(this, priority, chunk, checkParser, maxDegreeOfParallelism));
			}
		}

		private void HandleTaskUpdate(TaskReport task)
		{
			if (IsDisposed)
				return;

			if (ParserUpdateNormal != null && task.Depth == 0)
			{
				//notify any delegates
				ParserUpdateNormal(this, new ParserUpdateEventArgs(task));
			}

			if (ParserUpdateVerbose != null)
			{
				//notify any delegates
				ParserUpdateVerbose(this, new ParserUpdateEventArgs(task.MostRecentTask)/*not sure this is right*/);
			}
		}

		private void ParseFiler_WordformUpdated(object sender, WordformUpdatedEventArgs e)
		{
			if (WordformUpdated != null)
			{
				WordformUpdated(this, e);
			}
		}

		/// <summary>
		/// Determine if the wordform is a single word or a phrase
		/// </summary>
		/// <param name="sWord">wordform</param>
		/// <returns>true if a single word; false otherwise</returns>
		public static bool IsOneWord(string sWord)
		{
			var acSpaceTab = new[]{ ' ', '	' };
			var i = sWord.IndexOfAny(acSpaceTab);
			return (i <= -1) || (i >= sWord.Length);
		}
	}
}
