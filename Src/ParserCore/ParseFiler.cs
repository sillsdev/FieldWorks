// Copyright (c) 2003-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.WordWorks.Parser
{
	/// <summary />
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

		#region Construction and Disposal

		/// <summary />
		public ParseFiler(LcmCache cache, Action<TaskReport> taskUpdateHandler, IdleQueue idleQueue, ICmAgent parserAgent)
		{
			Guard.AgainstNull(cache, nameof(cache));
			Guard.AgainstNull(taskUpdateHandler, nameof(taskUpdateHandler));
			Guard.AgainstNull(idleQueue, nameof(idleQueue));
			Guard.AgainstNull(parserAgent, nameof(parserAgent));

			m_cache = cache;
			m_taskUpdateHandler = taskUpdateHandler;
			m_idleQueue = idleQueue;
			m_parserAgent = parserAgent;
			m_workQueue = new Queue<WordformUpdateWork>();
			m_syncRoot = new object();

			var servLoc = cache.ServiceLocator;
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
		public bool ProcessParse(IWfiWordform wordform, ParserPriority priority, ParseResult parseResult)
		{
			lock (m_syncRoot)
			{
				m_workQueue.Enqueue(new WordformUpdateWork(wordform, priority, parseResult));
			}
			m_idleQueue.Add(IdleQueuePriority.Low, UpdateWordforms);
			return true;
		}

		#endregion Public methods

		#region Private methods

		/// <summary>
		/// Updates the wordform. This will be run in the UI thread when the application is idle. If it can't be done right now,
		/// it returns false, and the caller should try again later.
		/// </summary>
		private bool UpdateWordforms(object parameter)
		{
			// If a UOW is in progress, the application isn't really idle, so try again later. One case where this used
			// to be true was the dialog in IText for choosing the writing system of a new text, which was run while
			// the UOW was active.
			if (!((IActionHandlerExtensions)m_cache.ActionHandlerAccessor).CanStartUow)
			{
				return false;
			}
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
				foreach (var work in results)
				{
					if (!work.IsValid)
					{
						// the wordform or the candidate analyses are no longer valid, so just skip this parse
						FireWordformUpdated(work.Wordform, work.Priority);
						continue;
					}
					var form = work.Wordform.Form.BestVernacularAlternative.Text;
					using (new TaskReport(string.Format(ParserCoreStrings.ksUpdateX, form), m_taskUpdateHandler))
					{
						// delete old problem annotations
						var problemAnnotations = m_baseAnnotationRepository.AllInstances().Where(ann => ann.BeginObjectRA == work.Wordform && ann.SourceRA == m_parserAgent);
						foreach (var problem in problemAnnotations)
						{
							m_cache.DomainDataByFlid.DeleteObj(problem.Hvo);
						}
						foreach (var analysis in work.Wordform.AnalysesOC)
						{
							m_parserAgent.SetEvaluation(analysis, Opinions.noopinion);
						}
						if (work.ParseResult.ErrorMessage != null)
						{
							// there was an error, so create a problem annotation
							var problemReport = m_baseAnnotationFactory.Create();
							m_cache.LangProject.AnnotationsOC.Add(problemReport);
							problemReport.CompDetails = work.ParseResult.ErrorMessage;
							problemReport.SourceRA = m_parserAgent;
							problemReport.AnnotationTypeRA = null;
							problemReport.BeginObjectRA = work.Wordform;
							SetUnsuccessfulParseEvals(work.Wordform, Opinions.noopinion);
						}
						else
						{
							// update the wordform
							foreach (var analysis in work.ParseResult.Analyses)
							{
								ProcessAnalysis(work.Wordform, analysis);
							}
							SetUnsuccessfulParseEvals(work.Wordform, Opinions.disapproves);
						}
						work.Wordform.Checksum = work.ParseResult.GetHashCode();
					}
					// notify all listeners that the wordform has been updated
					FireWordformUpdated(work.Wordform, work.Priority);
				}
			});
			return true;
		}

		private void FireWordformUpdated(IWfiWordform wordform, ParserPriority priority)
		{
			WordformUpdated?.Invoke(this, new WordformUpdatedEventArgs(wordform, priority));
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
			/*
				Try to find matching analysis(analyses) that already exist.
				A "match" is one in which:
				(1) the number of morph bundles equal the number of the MoForm and
					MorphoSyntaxAnanlysis (MSA) IDs passed in to the stored procedure, and
				(2) The objects of each MSA+Form pair match those of the corresponding WfiMorphBundle.
			*/
			// Find matching analysis/analyses, if any exist.
			var matches = new HashSet<IWfiAnalysis>();
			foreach (var anal in wordform.AnalysesOC)
			{
				if (anal.MorphBundlesOS.Count == analysis.Morphs.Count)
				{
					// Meets match condition (1), above.
					var mbMatch = false; //Start pessimistically.
					var i = 0;
					foreach (var mb in anal.MorphBundlesOS)
					{
						var current = analysis.Morphs[i++];
						if (mb.MorphRA == current.Form && mb.MsaRA == current.Msa && mb.InflTypeRA == current.InflType)
						{
							// Possibly matches condition (2), above.
							mbMatch = true;
						}
						else
						{
							// Fails condition (2), above.
							mbMatch = false;
							break; // No sense in continuing.
						}
					}
					if (mbMatch)
					{
						// Meets matching condition (2), above.
						matches.Add(anal);
					}
				}
			}
			if (matches.Count == 0)
			{
				// Create a new analysis, since there are no matches.
				var newAnal = m_analysisFactory.Create();
				wordform.AnalysesOC.Add(newAnal);
				// Make WfiMorphBundle(s).
				foreach (var morph in analysis.Morphs)
				{
					var mb = m_mbFactory.Create();
					newAnal.MorphBundlesOS.Add(mb);
					mb.MorphRA = morph.Form;
					mb.MsaRA = morph.Msa;
					if (morph.InflType != null)
					{
						mb.InflTypeRA = morph.InflType;
					}
				}
				matches.Add(newAnal);
			}
			// (Re)set evaluations.
			foreach (var matchingAnal in matches)
			{
				m_parserAgent.SetEvaluation(matchingAnal, Opinions.approves);
			}
		}

		#region Wordform Preparation methods

		private void SetUnsuccessfulParseEvals(IWfiWordform wordform, Opinions opinion)
		{
			var segmentAnalyses = new HashSet<IAnalysis>();
			foreach (var seg in wordform.OccurrencesBag)
			{
				segmentAnalyses.UnionWith(seg.AnalysesRS);
			}
			foreach (var analysis in wordform.AnalysesOC)
			{
				// ensure that used analyses have a user evaluation
				if (segmentAnalyses.Contains(analysis) || analysis.MeaningsOC.Any(gloss => segmentAnalyses.Contains(gloss)))
				{
					m_userAgent.SetEvaluation(analysis, Opinions.approves);
				}
				if (analysis.GetAgentOpinion(m_parserAgent) == Opinions.noopinion)
				{
					if (analysis.GetAgentOpinion(m_userAgent) == Opinions.noopinion)
					{
						analysis.Delete();
					}
					else if (opinion != Opinions.noopinion)
					{
						m_parserAgent.SetEvaluation(analysis, opinion);
					}
				}
			}
		}

		#endregion Wordform Preparation methods

		#endregion Private methods

		private sealed class WordformUpdateWork
		{
			internal WordformUpdateWork(IWfiWordform wordform, ParserPriority priority, ParseResult parseResult)
			{
				Wordform = wordform;
				Priority = priority;
				ParseResult = parseResult;
			}

			internal IWfiWordform Wordform { get; }

			internal ParserPriority Priority { get; }

			internal ParseResult ParseResult { get; }

			internal bool IsValid => Wordform.IsValidObject && ParseResult.IsValid;
		}
	}
}