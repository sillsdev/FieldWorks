// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ParseFiler.cs
// Responsibility: Randy Regnier
// Last reviewed:
//
// <remarks>
// Implements the ParseFiler.
// </remarks>
// buildtest ParseFiler-nodep
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.Utils;

namespace SIL.FieldWorks.WordWorks.Parser
{
	/// <summary>
	/// The event args for the WordformUpdated event.
	/// </summary>
	public class WordformUpdatedEventArgs : EventArgs
	{
		public WordformUpdatedEventArgs(IWfiWordform wordform, ParserPriority priority)
		{
			Wordform = wordform;
			Priority = priority;
		}

		public IWfiWordform Wordform
		{
			get; private set;
		}

		public ParserPriority Priority
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

		private readonly FdoCache m_cache;
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

		/// <summary>
		/// Set of analyses which had a recorded evaluation by this parser agent when the engine was loaded.
		/// These evaluations are considered stale until we set a new evaluation, which removes that item from the set.
		/// </summary>
		private readonly HashSet<IWfiAnalysis> m_analysesWithOldEvaluation;

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
		public ParseFiler(FdoCache cache, Action<TaskReport> taskUpdateHandler, IdleQueue idleQueue, ICmAgent parserAgent)
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

			IFdoServiceLocator servLoc = cache.ServiceLocator;
			m_analysisFactory = servLoc.GetInstance<IWfiAnalysisFactory>();
			m_mbFactory = servLoc.GetInstance<IWfiMorphBundleFactory>();
			m_baseAnnotationRepository = servLoc.GetInstance<ICmBaseAnnotationRepository>();
			m_baseAnnotationFactory = servLoc.GetInstance<ICmBaseAnnotationFactory>();
			m_userAgent = m_cache.LanguageProject.DefaultUserAgent;

			m_analysesWithOldEvaluation = new HashSet<IWfiAnalysis>(
				m_cache.ServiceLocator.GetInstance<IWfiAnalysisRepository>().AllInstances().Where(
				analysis => analysis.GetAgentOpinion(m_parserAgent) != Opinions.noopinion));
		}

		#endregion Construction and Disposal

		#region Public methods
		///  <summary>
		///  Process the parse result.
		///  </summary>
		/// <param name="wordform">The wordform.</param>
		/// <param name="priority">The priority.</param>
		///  <param name="parseResult">The parse result.</param>
		public bool ProcessParse(IWfiWordform wordform, ParserPriority priority, ParseResult parseResult)
		{
			lock (m_syncRoot)
				m_workQueue.Enqueue(new WordformUpdateWork(wordform, priority, parseResult));
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
					if (!work.IsValid)
					{
						// the wordform or the candidate analyses are no longer valid, so just skip this parse
						FireWordformUpdated(work.Wordform, work.Priority);
						continue;
					}
					string form = work.Wordform.Form.BestVernacularAlternative.Text;
					using (new TaskReport(String.Format(ParserCoreStrings.ksUpdateX, form), m_taskUpdateHandler))
					{
						// delete old problem annotations
						IEnumerable<ICmBaseAnnotation> problemAnnotations =
							from ann in m_baseAnnotationRepository.AllInstances()
							where ann.BeginObjectRA == work.Wordform && ann.SourceRA == m_parserAgent
							select ann;
						foreach (ICmBaseAnnotation problem in problemAnnotations)
							m_cache.DomainDataByFlid.DeleteObj(problem.Hvo);

						if (work.ParseResult.ErrorMessage != null)
						{
							// there was an error, so create a problem annotation
							ICmBaseAnnotation problemReport = m_baseAnnotationFactory.Create();
							m_cache.LangProject.AnnotationsOC.Add(problemReport);
							problemReport.CompDetails = work.ParseResult.ErrorMessage;
							problemReport.SourceRA = m_parserAgent;
							problemReport.AnnotationTypeRA = null;
							problemReport.BeginObjectRA = work.Wordform;
							FinishWordForm(work.Wordform);
						}
						else
						{
							// update the wordform
							foreach (ParseAnalysis analysis in work.ParseResult.Analyses)
								ProcessAnalysis(work.Wordform, analysis);
							FinishWordForm(work.Wordform);
							foreach (IWfiAnalysis analysis in work.Wordform.AnalysesOC.Where(anal => anal.GetAgentOpinion(m_parserAgent) != Opinions.approves))
							{
								m_analysesWithOldEvaluation.Remove(analysis);
								if (analysis.GetAgentOpinion(m_userAgent) == Opinions.noopinion)
									analysis.Delete();
								else
									m_parserAgent.SetEvaluation(analysis, Opinions.disapproves);
							}
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
			if (WordformUpdated != null)
				WordformUpdated(this, new WordformUpdatedEventArgs(wordform, priority));
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
			foreach (IWfiAnalysis anal in wordform.AnalysesOC)
			{
				if (anal.MorphBundlesOS.Count == analysis.Morphs.Count)
				{
					// Meets match condition (1), above.
					bool mbMatch = false; //Start pessimistically.
					int i = 0;
					foreach (IWfiMorphBundle mb in anal.MorphBundlesOS)
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
				foreach (ParseMorph morph in analysis.Morphs)
				{
					IWfiMorphBundle mb = m_mbFactory.Create();
					newAnal.MorphBundlesOS.Add(mb);
					mb.MorphRA = morph.Form;
					mb.MsaRA = morph.Msa;
					if (morph.InflType != null)
						mb.InflTypeRA = morph.InflType;
				}
				matches.Add(newAnal);
			}
			// (Re)set evaluations.
			foreach (IWfiAnalysis matchingAnal in matches)
			{
				m_parserAgent.SetEvaluation(matchingAnal, Opinions.approves);
				m_analysesWithOldEvaluation.Remove(matchingAnal);
			}
		}

		#region Wordform Preparation methods

		private void FinishWordForm(IWfiWordform wordform)
		{
			// the following is a port of the SP RemoveUnusedAnalyses

			// Delete stale evaluations on analyses. The only non-stale analyses are new, positive ones, so this
			// makes all analyses that are not known to be correct no-opinion. Later any of them that survive at all
			// will be changed to failed (if there was no error in parsing the wordform).
			foreach (IWfiAnalysis analysis in wordform.AnalysesOC.Where(a => m_analysesWithOldEvaluation.Contains(a)))
				analysis.SetAgentOpinion(m_parserAgent, Opinions.noopinion);

			// Make sure all analyses have human evaluations, if they,
			// or glosses they own, are referred to by an ISegment.
			//var annLookup = m_baseAnnotationRepository.AllInstances()
			//	.Where(ann => ann.AnnotationTypeRA != null && ann.AnnotationTypeRA.Guid == CmAnnotationDefnTags.kguidAnnWordformInContext)
			//	.ToLookup(ann => ann.InstanceOfRA);
			var segmentAnalyses = new HashSet<IAnalysis>();
			foreach (ISegment seg in wordform.OccurrencesBag)
				segmentAnalyses.UnionWith(seg.AnalysesRS.ToArray());
			IEnumerable<IWfiAnalysis> analyses =
				from anal in wordform.AnalysesOC
				where segmentAnalyses.Contains(anal) || anal.MeaningsOC.Any(segmentAnalyses.Contains)
				select anal;
			foreach (IWfiAnalysis analysis in analyses)
				m_userAgent.SetEvaluation(analysis, Opinions.approves);

			// Delete orphan analyses, which have no evaluations (Review JohnT: should we also check for no owned WfiGlosses?)
			IEnumerable<IWfiAnalysis> orphanedAnalyses =
				from anal in wordform.AnalysesOC
				where anal.EvaluationsRC.Count == 0
				select anal;
			foreach (IWfiAnalysis analysis in orphanedAnalyses)
				m_cache.DomainDataByFlid.DeleteObj(analysis.Hvo);
		}

		#endregion Wordform Preparation methods

		#endregion Private methods

		private class WordformUpdateWork
		{
			private readonly IWfiWordform m_wordform;
			private readonly ParserPriority m_priority;
			private readonly ParseResult m_parseResult;

			public WordformUpdateWork(IWfiWordform wordform, ParserPriority priority, ParseResult parseResult)
			{
				m_wordform = wordform;
				m_priority = priority;
				m_parseResult = parseResult;
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

			public bool IsValid
			{
				get { return m_wordform.IsValidObject && m_parseResult.IsValid; }
			}
		}
	}
}
