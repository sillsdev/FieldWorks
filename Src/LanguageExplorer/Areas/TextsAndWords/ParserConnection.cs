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
using SIL.FieldWorks.WordWorks.Parser.HermitCrab;
using SIL.FieldWorks.WordWorks.Parser.XAmple;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Infrastructure;
using SIL.LCModel.Utils;
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
		internal ParserConnection(LcmCache cache, IdleQueue idleQueue)
		{
			m_activity = string.Empty;
			m_scheduler = new ParserScheduler(cache, idleQueue, Path.Combine(FwDirectoryFinder.CodeDirectory, FwDirectoryFinder.ksFlexFolderName));
			m_scheduler.ParserUpdateVerbose += ParserUpdateHandlerForPolling;
		}

		private object SyncRoot { get; } = new object();

		/// <summary>
		/// Get or Set state for the Try A Word dialog running
		/// </summary>
		internal bool TryAWordDialogIsRunning
		{
			get => m_scheduler.TryAWordDialogIsRunning;
			set => m_scheduler.TryAWordDialogIsRunning = value;
		}

		/// <summary>
		/// place an asynchronous request for tracing the word
		/// </summary>
		/// <param name="sForm">The word form to be parsed</param>
		/// <param name="fDoTrace">whether a trace is to be run or not</param>
		/// <param name="sSelectTraceMorphs">list of msa hvos to limit trace to </param>
		internal IAsyncResult BeginTryAWord(string sForm, bool fDoTrace, int[] sSelectTraceMorphs)
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

		internal void UpdateWordforms(IEnumerable<IWfiWordform> wordforms, ParserPriority priority)
		{
			m_scheduler.ScheduleWordformsForUpdate(wordforms, priority);
		}

		internal void UpdateWordform(IWfiWordform wordform, ParserPriority priority)
		{
			m_scheduler.ScheduleOneWordformForUpdate(wordform, priority);
		}

		internal Exception UnhandledException => m_scheduler.UnhandledException;

		internal int GetQueueSize(ParserPriority priority)
		{
			return m_scheduler.GetQueueSize(priority);
		}

		internal void ReloadGrammarAndLexicon()
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
		private void ParserUpdateHandlerForPolling(object sender, ParserUpdateEventArgs args)
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
		internal string Activity
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
		internal string GetAndClearNotification()
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
		/// <summary />
		private sealed class ParserScheduler : DisposableBase
		{
			/// <summary />
			private sealed class ParserWorker : DisposableBase
			{
				private readonly LcmCache m_cache;
				private readonly Action<TaskReport> m_taskUpdateHandler;
				private int m_numberOfWordForms;
				private IParser m_parser;

				/// <summary />
				internal ParserWorker(LcmCache cache, Action<TaskReport> taskUpdateHandler, IdleQueue idleQueue, string dataDir)
				{
					m_cache = cache;
					m_taskUpdateHandler = taskUpdateHandler;
					ICmAgent agent;
					switch (m_cache.LanguageProject.MorphologicalDataOA.ActiveParser)
					{
						case "XAmple":
							m_parser = new XAmpleParser(cache, dataDir);
							agent = cache.ServiceLocator.GetInstance<ICmAgentRepository>().GetObject(CmAgentTags.kguidAgentXAmpleParser);
							break;
						case "HC":
							m_parser = new HCParser(cache);
							agent = cache.ServiceLocator.GetInstance<ICmAgentRepository>().GetObject(CmAgentTags.kguidAgentHermitCrabParser);
							break;
						default:
							throw new InvalidOperationException("The language project is set to use an unrecognized parser.");
					}
					ParseFiler = new ParseFiler(cache, taskUpdateHandler, idleQueue, agent);
				}

				protected override void DisposeManagedResources()
				{
					if (m_parser != null)
					{
						m_parser.Dispose();
						m_parser = null;
					}
				}

				/// <inheritdoc />
				protected override void Dispose(bool disposing)
				{
					Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + " ******");
					base.Dispose(disposing);
				}

				internal ParseFiler ParseFiler { get; }

				/// <summary>
				/// Try parsing a wordform, optionally getting a trace of the parse
				/// </summary>
				/// <param name="sForm">the word form to parse</param>
				/// <param name="fDoTrace">whether or not to trace the parse</param>
				/// <param name="sSelectTraceMorphs">list of msa hvos to limit trace to </param>
				internal void TryAWord(string sForm, bool fDoTrace, int[] sSelectTraceMorphs)
				{
					if (sForm == null)
					{
						throw new ArgumentNullException("sForm", "TryAWord cannot trace a Null string.");
					}
					if (sForm == string.Empty)
					{
						throw new ArgumentException("Can't try a word with no content.", "sForm");
					}
					CheckNeedsUpdate();
					using (var task = new TaskReport(string.Format(ParserCoreStrings.ksTraceWordformX, sForm), m_taskUpdateHandler))
					{
						var normForm = CustomIcu.GetIcuNormalizer(FwNormalizationMode.knmNFD).Normalize(sForm);
						task.Details = fDoTrace ? m_parser.TraceWordXml(normForm, sSelectTraceMorphs) : m_parser.ParseWordXml(normForm);
					}
				}

				internal bool UpdateWordform(IWfiWordform wordform, ParserPriority priority)
				{
					var wordformHash = 0;
					ITsString form = null;
					var hvo = 0;
					using (new WorkerThreadReadHelper(m_cache.ServiceLocator.GetInstance<IWorkerThreadReadHandler>()))
					{
						if (wordform.IsValidObject)
						{
							wordformHash = wordform.Checksum;
							form = wordform.Form.VernacularDefaultWritingSystem;
						}
					}
					// 'form' will now be null, if it could not find the wordform for whatever reason.
					// uiCRCWordform will also now be 0, if 'form' is null.
					if (form == null || string.IsNullOrEmpty(form.Text))
					{
						return false;
					}
					CheckNeedsUpdate();
					var result = m_parser.ParseWord(CustomIcu.GetIcuNormalizer(FwNormalizationMode.knmNFD).Normalize(form.Text.Replace(' ', '.')));
					return wordformHash != result.GetHashCode() && ParseFiler.ProcessParse(wordform, priority, result);
				}

				private void CheckNeedsUpdate()
				{
					using (new TaskReport(ParserCoreStrings.ksUpdatingGrammarAndLexicon, m_taskUpdateHandler))
					{
						if (!m_parser.IsUpToDate())
						{
							m_parser.Update();
						}
					}
				}

				internal void ReloadGrammarAndLexicon()
				{
					m_parser.Reset();
					CheckNeedsUpdate();
				}
			}

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

				internal virtual void DoWork()
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

				internal override void DoWork()
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

				internal override void DoWork()
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

				internal override void DoWork()
				{
					m_scheduler.m_parserWorker.ReloadGrammarAndLexicon();
					base.DoWork();
				}
			}

			internal event EventHandler<ParserUpdateEventArgs> ParserUpdateVerbose;
			internal event EventHandler<ParserUpdateEventArgs> ParserUpdateNormal;

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
				get => m_tryAWordDialogRunning;
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
				if (queueAccessor.GetNextWorkItem(out var work))
				{
					work.DoWork();
				}
			}

			/// <summary>
			/// returns the number of the Wordforms in the low priority queue.
			/// </summary>
			internal int GetQueueSize(ParserPriority priority)
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

			internal void ScheduleOneWordformForUpdate(IWfiWordform wordform, ParserPriority priority)
			{
				m_thread.EnqueueWork(priority, new UpdateWordformWork(this, priority, wordform));
			}

			internal void ScheduleWordformsForUpdate(IEnumerable<IWfiWordform> wordforms, ParserPriority priority)
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
				if (ParserUpdateNormal != null && (task.Depth == 0 || task.NotificationMessage != null))
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
				return i <= -1 || i >= sWord.Length;
			}
		}
	}
}