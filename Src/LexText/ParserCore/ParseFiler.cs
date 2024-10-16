// Copyright (c) 2003-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Infrastructure;
using XCore;

namespace SIL.FieldWorks.WordWorks.Parser
{
	/// <summary>
	/// The event args for the WordformUpdated event.
	/// </summary>
	public class WordformUpdatedEventArgs : EventArgs
	{
		public WordformUpdatedEventArgs(IWfiWordform wordform, ParserPriority priority, ParseResult parseResult, bool checkParser)
		{
			Wordform = wordform;
			Priority = priority;
			ParseResult = parseResult;
			CheckParser = checkParser;
		}

		public IWfiWordform Wordform
		{
			get; private set;
		}

		public ParserPriority Priority
		{
			get; private set;
		}

		public ParseResult ParseResult
		{
			get; private set;
		}

		public bool CheckParser
		{
			get; private set;
		}
	}

	/// <summary>
	/// Summary description for ParseFiler.
	/// </summary>
	public class ParseFiler
	{
		/// <summary>
		/// Occurs when a wordform is updated.
		/// </summary>
		public event EventHandler<WordformUpdatedEventArgs> WordformUpdated;

		#region Data members

		private readonly LcmCache m_cache;
		private readonly Action<TaskReport> m_taskUpdateHandler;
		private readonly IdleQueue m_idleQueue;
		private readonly ICmAgent m_parserAgent;
		private readonly Queue<WordformUpdateWork> m_workQueue;
		private readonly object m_syncRoot;

		private readonly IWfiAnalysisFactory m_analysisFactory;
		private readonly IWfiMorphBundleFactory m_mbFactory;
		private readonly ICmBaseAnnotationRepository m_baseAnnotationRepository;
		private readonly ICmBaseAnnotationFactory m_baseAnnotationFactory;
		private readonly ICmAgent m_userAgent;

		#endregion Data members

		#region Properties

		#endregion Properties

		#region Construction and Disposal

		/// <summary>
		/// Initializes a new instance of the <see cref="ParseFiler"/> class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="taskUpdateHandler">The task update handler.</param>
		/// <param name="idleQueue">The idle queue.</param>
		/// <param name="parserAgent">The parser agent.</param>
		public ParseFiler(LcmCache cache, Action<TaskReport> taskUpdateHandler, IdleQueue idleQueue, ICmAgent parserAgent)
		{
			Debug.Assert(cache != null);
			Debug.Assert(taskUpdateHandler != null);
			Debug.Assert(idleQueue != null);
			Debug.Assert(parserAgent != null);

			m_cache = cache;
			m_taskUpdateHandler = taskUpdateHandler;
			m_idleQueue = idleQueue;
			m_parserAgent = parserAgent;
			m_workQueue = new Queue<WordformUpdateWork>();
			m_syncRoot = new object();

			ILcmServiceLocator servLoc = cache.ServiceLocator;
			m_analysisFactory = servLoc.GetInstance<IWfiAnalysisFactory>();
			m_mbFactory = servLoc.GetInstance<IWfiMorphBundleFactory>();
			m_baseAnnotationRepository = servLoc.GetInstance<ICmBaseAnnotationRepository>();
			m_baseAnnotationFactory = servLoc.GetInstance<ICmBaseAnnotationFactory>();
			m_userAgent = m_cache.LanguageProject.DefaultUserAgent;
		}

		#endregion Construction and Disposal

		#region Public methods
		///  <summary>
		///  Process the parse result.
		///  </summary>
		/// <param name="wordform">The wordform.</param>
		/// <param name="priority">The priority.</param>
		///  <param name="parseResult">The parse result.</param>
		public bool ProcessParse(IWfiWordform wordform, ParserPriority priority, ParseResult parseResult, bool checkParser = false)
		{
			lock (m_syncRoot)
				m_workQueue.Enqueue(new WordformUpdateWork(wordform, priority, parseResult, checkParser));
			m_idleQueue.Add(IdleQueuePriority.Low, UpdateWordforms);
			return true;
		}

		#endregion Public methods

		#region Private methods

		/// <summary>
		/// Updates the wordform. This will be run in the UI thread when the application is idle. If it can't be done right now,
		/// it returns false, and the caller should try again later.
		/// </summary>
		/// <param name="parameter">The parameter.</param>
		/// <returns></returns>
		private bool UpdateWordforms(object parameter)
		{
			// If a UOW is in progress, the application isn't really idle, so try again later. One case where this used
			// to be true was the dialog in IText for choosing the writing system of a new text, which was run while
			// the UOW was active.
			if (!((IActionHandlerExtensions) m_cache.ActionHandlerAccessor).CanStartUow)
				return false;

			// update all of the wordforms in a batch, this might slow down the UI thread a little, if it causes too much unresponsiveness
			// we can bail out early if there is a message in the Win32 message queue
			IEnumerable<WordformUpdateWork> results;
			lock (m_syncRoot)
			{
				results = m_workQueue.ToArray();
				m_workQueue.Clear();
			}

			NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
			{
				foreach (WordformUpdateWork work in results)
				{
					if (work.CheckParser)
					{
						// This was just a test.  Don't update data.
						string testform = work.Wordform.Form.BestVernacularAlternative.Text;
						using (new TaskReport(String.Format(ParserCoreStrings.ksTestX, testform), m_taskUpdateHandler))
						{ }
						FireWordformUpdated(work.Wordform, work.Priority, work.ParseResult, work.CheckParser);
						continue;
					}
					if (!work.IsValid)
					{
						// the wordform or the candidate analyses are no longer valid, so just skip this parse
						FireWordformUpdated(work.Wordform, work.Priority, work.ParseResult, work.CheckParser);
						continue;
					}
					if (work.Wordform.Checksum == work.ParseResult.GetHashCode())
					{
						// Nothing changed, but clients might like to know anyway.
						FireWordformUpdated(work.Wordform, work.Priority, work.ParseResult, work.CheckParser);
						continue;
					}
					string form = work.Wordform.Form.BestVernacularAlternative.Text;
					using (new TaskReport(String.Format(ParserCoreStrings.ksUpdateX, form), m_taskUpdateHandler))
					{
						// delete all old problem annotations
						// (We no longer create new problem annotations.)
						IEnumerable<ICmBaseAnnotation> problemAnnotations =
							from ann in m_baseAnnotationRepository.AllInstances()
							where ann.SourceRA == m_parserAgent
							select ann;
						foreach (ICmBaseAnnotation problem in problemAnnotations)
							m_cache.DomainDataByFlid.DeleteObj(problem.Hvo);

						foreach (IWfiAnalysis analysis in work.Wordform.AnalysesOC)
							m_parserAgent.SetEvaluation(analysis, Opinions.noopinion);

						if (work.ParseResult.ErrorMessage != null)
						{
							SetUnsuccessfulParseEvals(work.Wordform, Opinions.noopinion);
						}
						else
						{
							// update the wordform
							foreach (ParseAnalysis analysis in work.ParseResult.Analyses)
								ProcessAnalysis(work.Wordform, analysis);
							SetUnsuccessfulParseEvals(work.Wordform, Opinions.disapproves);
						}
						work.Wordform.Checksum = work.ParseResult.GetHashCode();
					}
					// notify all listeners that the wordform has been updated
					FireWordformUpdated(work.Wordform, work.Priority, work.ParseResult, work.CheckParser);
				}
			});
			return true;
		}

		private void FireWordformUpdated(IWfiWordform wordform, ParserPriority priority, ParseResult parseResult, bool checkParser)
		{
			if (WordformUpdated != null)
				WordformUpdated(this, new WordformUpdatedEventArgs(wordform, priority, parseResult, checkParser));
		}

		/// <summary>
		/// Process an analysis.
		/// </summary>
		/// <remarks>
		/// This method contains the port of the UpdWfiAnalysisAndEval$ SP.
		/// The SP was about 220 lines of code (not counting a commetned out section).
		/// The C# version is about 60 lines long.
		/// </remarks>
		private void ProcessAnalysis(IWfiWordform wordform, ParseAnalysis analysis)
		{
			// Find matching analysis/analyses, if any exist.
			var matches = new HashSet<IWfiAnalysis>();
			foreach (IWfiAnalysis anal in wordform.AnalysesOC)
			{
				if (analysis.MatchesIWfiAnalysis(anal))
					matches.Add(anal);
			}
			if (matches.Count == 0)
			{
				// Create a new analysis, since there are no matches.
				var newAnal = m_analysisFactory.Create();
				wordform.AnalysesOC.Add(newAnal);
				// Make WfiMorphBundle(s).
				foreach (ParseMorph morph in analysis.Morphs)
				{
					IWfiMorphBundle mb = m_mbFactory.Create();
					newAnal.MorphBundlesOS.Add(mb);
					mb.MorphRA = morph.Form;
					mb.MsaRA = morph.Msa;
					if (morph.InflType != null)
						mb.InflTypeRA = morph.InflType;
					if (morph.GuessedString != null)
					{
						// Override default Form with GuessedString.
						int vernWS = m_cache.DefaultVernWs;
						mb.Form.set_String(vernWS, TsStringUtils.MakeString(morph.GuessedString, vernWS));
					}
				}
				matches.Add(newAnal);
			}
			// (Re)set evaluations.
			foreach (IWfiAnalysis matchingAnal in matches)
				m_parserAgent.SetEvaluation(matchingAnal, Opinions.approves);
		}

		#region Wordform Preparation methods

		private void SetUnsuccessfulParseEvals(IWfiWordform wordform, Opinions opinion)
		{
			var segmentAnalyses = new HashSet<IAnalysis>();
			foreach (ISegment seg in wordform.OccurrencesBag)
				segmentAnalyses.UnionWith(seg.AnalysesRS);
			foreach (IWfiAnalysis analysis in wordform.AnalysesOC)
			{
				// ensure that used analyses have a user evaluation
				if (segmentAnalyses.Contains(analysis) || analysis.MeaningsOC.Any(gloss => segmentAnalyses.Contains(gloss)))
					m_userAgent.SetEvaluation(analysis, Opinions.approves);
				if (analysis.GetAgentOpinion(m_parserAgent) == Opinions.noopinion)
				{
					if (analysis.GetAgentOpinion(m_userAgent) == Opinions.noopinion)
						analysis.Delete();
					else if (opinion != Opinions.noopinion)
						m_parserAgent.SetEvaluation(analysis, opinion);
				}
			}
		}

		#endregion Wordform Preparation methods

		#endregion Private methods

		private class WordformUpdateWork
		{
			private readonly IWfiWordform m_wordform;
			private readonly ParserPriority m_priority;
			private readonly ParseResult m_parseResult;
			private readonly bool m_checkParser;

			public WordformUpdateWork(IWfiWordform wordform, ParserPriority priority, ParseResult parseResult, bool checkParser)
			{
				m_wordform = wordform;
				m_priority = priority;
				m_parseResult = parseResult;
				m_checkParser = checkParser;
			}

			public IWfiWordform Wordform
			{
				get { return m_wordform; }
			}

			public ParserPriority Priority
			{
				get { return m_priority; }
			}

			public ParseResult ParseResult
			{
				get { return m_parseResult; }
			}

			public bool CheckParser
			{
				get { return m_checkParser; }
			}

			public bool IsValid
			{
				get { return m_wordform.IsValidObject && m_parseResult.IsValid; }
			}
		}
	}
}
