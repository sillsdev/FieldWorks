// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ParseFiler.cs
// Responsibility: Randy Regnier
// Last reviewed:
//
// <remarks>
// Implements the ParseFiler.
// </remarks>
// buildtest ParseFiler-nodep

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
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
	public class ParseFiler : IFWDisposable
	{
		/// <summary>
		/// Occurs when a wordform is updated.
		/// </summary>
		public event EventHandler<WordformUpdatedEventArgs> WordformUpdated;

		#region internal class

		private class ParseResult
		{
			public ParseResult(IWfiWordform wordform, uint crc, ParserPriority priority, IList<ParseAnalysis> analyses,
				string errorMessage)
			{
				Wordform = wordform;
				Crc = crc;
				Priority = priority;
				Analyses = analyses;
				ErrorMessage = errorMessage;
			}

			public IWfiWordform Wordform
			{
				get; private set;
			}

			public uint Crc
			{
				get; private set;
			}

			public IList<ParseAnalysis> Analyses
			{
				get; private set;
			}

			public string ErrorMessage
			{
				get; private set;
			}

			public ParserPriority Priority
			{
				get; private set;
			}

			public bool IsValid
			{
				get
				{
					if (!Wordform.IsValidObject)
						return false;
					return Analyses.All(analysis => analysis.IsValid);
				}
			}
		}

		private class ParseAnalysis
		{
			public ParseAnalysis(IList<ParseMorph> morphs)
			{
				Morphs = morphs;
			}

			public IList<ParseMorph> Morphs
			{
				get; private set;
			}

			public bool IsValid
			{
				get
				{
					return Morphs.All(morph => morph.IsValid);
				}
			}
		}

		private class ParseMorph
		{
			public ParseMorph(IMoForm form, IMoMorphSynAnalysis msa)
			{
				Form = form;
				Msa = msa;
			}

			public ParseMorph(IMoForm form, IMoMorphSynAnalysis msa, ILexEntryInflType inflType)
			{
				Form = form;
				Msa = msa;
				InflType = inflType;
			}

			public IMoForm Form
			{
				get; private set;
			}

			public IMoMorphSynAnalysis Msa
			{
				get; private set;
			}

			public ILexEntryInflType InflType
			{
				get; private set;
			}

			public bool IsValid
			{
				get
				{
					return Form.IsValidObject && Msa.IsValidObject;
				}
			}
		}

		#endregion internal class

		#region Data members

		private readonly FdoCache m_cache;
		private readonly Action<TaskReport> m_taskUpdateHandler;
		private readonly IdleQueue m_idleQueue;
		private readonly ICmAgent m_parserAgent;
		private readonly Queue<ParseResult> m_resultQueue;
		private readonly object m_syncRoot;

		private readonly IWfiWordformRepository m_wordformRepository;
		private readonly IWfiAnalysisFactory m_analysisFactory;
		private readonly IWfiMorphBundleFactory m_mbFactory;
		private readonly ICmBaseAnnotationRepository m_baseAnnotationRepository;
		private readonly ICmBaseAnnotationFactory m_baseAnnotationFactory;
		private readonly IMoFormRepository m_moFormRepository;
		private readonly IMoMorphSynAnalysisRepository m_msaRepository;
		private readonly ICmAgent m_userAgent;

		/// <summary>
		/// Set of analyses which had a recorded evaluation by this parser agent when the engine was loaded.
		/// These evaluations are considered stale until we set a new evaluation, which removes that item from the set.
		/// </summary>
		private readonly HashSet<IWfiAnalysis> m_analysesWithOldEvaluation;
		private readonly TraceSwitch m_tracingSwitch = new TraceSwitch("ParserCore.TracingSwitch", "Just regular tracking", "Off");
		private long m_ticksFiler;
		private int m_numberOfWordForms;

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
			m_resultQueue = new Queue<ParseResult>();
			m_syncRoot = new object();

			var servLoc = cache.ServiceLocator;
			m_wordformRepository = servLoc.GetInstance<IWfiWordformRepository>();
			m_analysisFactory = servLoc.GetInstance<IWfiAnalysisFactory>();
			m_mbFactory = servLoc.GetInstance<IWfiMorphBundleFactory>();
			m_baseAnnotationRepository = servLoc.GetInstance<ICmBaseAnnotationRepository>();
			m_baseAnnotationFactory = servLoc.GetInstance<ICmBaseAnnotationFactory>();
			m_moFormRepository = servLoc.GetInstance<IMoFormRepository>();
			m_msaRepository = servLoc.GetInstance<IMoMorphSynAnalysisRepository>();
			m_userAgent = m_cache.LanguageProject.DefaultUserAgent;

			m_analysesWithOldEvaluation = new HashSet<IWfiAnalysis>(
				m_cache.ServiceLocator.GetInstance<IWfiAnalysisRepository>().AllInstances().Where(
				analysis => analysis.GetAgentOpinion(m_parserAgent) != Opinions.noopinion));
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
		private bool m_isDisposed;

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
		~ParseFiler()
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
		protected virtual void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + ". ******************");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				Trace.WriteLineIf(m_tracingSwitch.TraceInfo, "Total number of wordforms updated = " + m_numberOfWordForms);
				Trace.WriteLineIf(m_tracingSwitch.TraceInfo, "Total time for parser filer = " + TimeSpan.FromTicks(m_ticksFiler).TotalMilliseconds);

				if (m_numberOfWordForms != 0)
				{
					long lAvg = m_ticksFiler / m_numberOfWordForms;
					Trace.WriteLineIf(m_tracingSwitch.TraceInfo, "Average time for parser filer = " + TimeSpan.FromTicks(lAvg).TotalMilliseconds);
				}
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		#endregion Construction and Disposal

		#region Public methods
		/// <summary>
		/// Process the XML data.
		/// </summary>
		/// <param name="priority">The priority.</param>
		/// <param name="parse">The XML data to process.</param>
		/// <param name="crc">The CRC.</param>
		/// <remarks>
		/// The 'parser' XML string may, or may not, be well formed XML.
		/// If there is an Exception node in the XML, then it may not be well-formed XML beyond that node,
		/// since the XAmple parser may have choked.
		/// This is why we can't use a DOM to get all of the XML, but we have to read it as it goes by in a stream.
		///
		/// ENHANCE (DamienD): right now we are not supporting malformed XML
		/// </remarks>
		public bool ProcessParse(ParserPriority priority, string parse, uint crc)
		{
			var wordformElem = XElement.Parse(parse);
			string errorMessage = null;
			var exceptionElem = wordformElem.Element("Exception");
			if (exceptionElem != null)
			{
				var totalAnalysesValue = (string) exceptionElem.Attribute("totalAnalyses");
				switch ((string) exceptionElem.Attribute("code"))
				{
					case "ReachedMaxAnalyses":
						errorMessage = String.Format(ParserCoreStrings.ksReachedMaxAnalysesAllowed,
							totalAnalysesValue);
						break;
					case "ReachedMaxBufferSize":
						errorMessage = String.Format(ParserCoreStrings.ksReachedMaxInternalBufferSize,
							totalAnalysesValue);
						break;
				}
			}
			else
			{
				errorMessage = (string) wordformElem.Element("Error");
			}

			try
			{
				ParseResult result;
				using (new WorkerThreadReadHelper(m_cache.ServiceLocator.GetInstance<IWorkerThreadReadHandler>()))
				{
					var wordform = m_wordformRepository.GetObject((int) wordformElem.Attribute("DbRef"));
					IList<ParseAnalysis> analyses = null;
					analyses = (from analysisElem in wordformElem.Descendants("WfiAnalysis")
								let morphs = from morphElem in analysisElem.Descendants("Morph")
											 let morph = CreateParseMorph(morphElem)
											 where morph != null
											 select morph
								where morphs.Any()
								select new ParseAnalysis(morphs.ToList())).ToList();
					result = new ParseResult(wordform, crc, priority, analyses, errorMessage);
				}

				lock (m_syncRoot)
					m_resultQueue.Enqueue(result);
				m_idleQueue.Add(IdleQueuePriority.Low, UpdateWordforms);
				return true;
			}
			catch (KeyNotFoundException)
			{
				// a wordform, form, or MSA no longer exists, so skip this parse result
			}
			return false;
		}

		#endregion Public methods

		#region Private methods

		/// <summary>
		/// Creates a single ParseMorph object
		/// Handles special cases where the MoForm hvo and/or MSI hvos are
		/// not actual MoForm or MSA objects.
		/// </summary>
		/// <param name="morphElem">A Morph element returned by one of the automated parsers</param>
		/// <returns>a new ParseMorph object or null if the morpheme should be skipped</returns>
		private ParseMorph CreateParseMorph(XElement morphElem)
		{
			// Normally, the hvo for MoForm is a MoForm and the hvo for MSI is an MSA
			// There are four exceptions, though, when an irregularly inflected form is involved:
			// 1. <MoForm DbRef="x"... and x is an hvo for a LexEntryInflType.
			//       This is one of the null allomorphs we create when building the
			//       input for the parser in order to still get the Word Grammar to have something in any
			//       required slots in affix templates.  The parser filer can ignore these.
			// 2. <MSI DbRef="y"... and y is an hvo for a LexEntryInflType.
			//       This is one of the null allomorphs we create when building the
			//       input for the parser in order to still get the Word Grammar to have something in any
			//       required slots in affix templates.  The parser filer can ignore these.
			// 3. <MSI DbRef="y"... and y is an hvo for a LexEntry.
			//       The LexEntry is an irregularly inflected form for the first set of LexEntryRefs.
			// 4. <MSI DbRef="y"... and y is an hvo for a LexEntry followed by a period and an index digit.
			//       The LexEntry is an irregularly inflected form and the (non-zero) index indicates
			//       which set of LexEntryRefs it is for.

			var hvoForm = (int) morphElem.Element("MoForm").Attribute("DbRef");
			var objForm = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoForm);
			var form = objForm as IMoForm;

			var msaHvo = morphElem.Element("MSI").Attribute("DbRef");
			string sMsaHvo = msaHvo.Value;
			// Irregulary inflected forms can have a combination MSA hvo: the LexEntry hvo, a period, and an index to the LexEntryRef
			var indexOfPeriod = IndexOfPeriodInMsaHvo(ref sMsaHvo);
			ICmObject objMsa = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(Convert.ToInt32(sMsaHvo));
			var msa = objMsa as IMoMorphSynAnalysis;

			if (form != null && msa != null)
				return new ParseMorph(form, msa);

			var msaAsLexEntry = objMsa as ILexEntry;
			if (msaAsLexEntry != null && form != null)
			{
				// is an irregularly inflected form
				// get the MoStemMsa of its variant
				if (msaAsLexEntry.EntryRefsOS.Count > 0)
				{
					var index = IndexOfLexEntryRef(msaHvo.Value, indexOfPeriod); // the value of the int after the period
					var lexEntryRef = msaAsLexEntry.EntryRefsOS[index];
					var sense = FDO.DomainServices.MorphServices.GetMainOrFirstSenseOfVariant(lexEntryRef);
					var stemMsa = sense.MorphoSyntaxAnalysisRA as IMoStemMsa;
					var entryOfForm = form.Owner as ILexEntry;
					var inflType = lexEntryRef.VariantEntryTypesRS.ElementAt(0);
					return new ParseMorph(form, stemMsa, inflType as ILexEntryInflType);
				}
			}
			// if it is anything else, we ignore it
			return null;
		}

		/// <summary>
		/// Updates the wordform. This will be run in the UI thread when the application is idle. If it can't be done right now,
		/// it returns false, and the caller should try again later.
		/// </summary>
		/// <param name="parameter">The parameter.</param>
		/// <returns></returns>
		private bool UpdateWordforms(object parameter)
		{
			if (IsDisposed)
				return true;
			// If a UOW is in progress, the application isn't really idle, so try again later. One case where this used
			// to be true was the dialog in IText for choosing the writing system of a new text, which was run while
			// the UOW was active.
			if (!((IActionHandlerExtensions) m_cache.ActionHandlerAccessor).CanStartUow)
				return false;

			// update all of the wordforms in a batch, this might slow down the UI thread a little, if it causes too much unresponsiveness
			// we can bail out early if there is a message in the Win32 message queue
			IEnumerable<ParseResult> results;
			lock (m_syncRoot)
			{
				results = m_resultQueue.ToArray();
				m_resultQueue.Clear();
			}

			NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
			{
				foreach (ParseResult result in results)
				{
					if (!result.IsValid)
					{
						// the wordform or the candidate analyses are no longer valid, so just skip this parse
						FireWordformUpdated(result.Wordform, result.Priority);
						continue;
					}
					var startTime = DateTime.Now;
					string form = result.Wordform.Form.BestVernacularAlternative.Text;
					using (new TaskReport(String.Format(ParserCoreStrings.ksUpdateX, form), m_taskUpdateHandler))
					{
						// delete old problem annotations
						var problemAnnotations = from ann in m_baseAnnotationRepository.AllInstances()
												 where
													ann.BeginObjectRA == result.Wordform &&
													ann.SourceRA == m_parserAgent
												 select ann;
						foreach (var problem in problemAnnotations)
							m_cache.DomainDataByFlid.DeleteObj(problem.Hvo);

						if (result.ErrorMessage != null)
						{
							// there was an error, so create a problem annotation
							var problemReport = m_baseAnnotationFactory.Create();
							m_cache.LangProject.AnnotationsOC.Add(problemReport);
							problemReport.CompDetails = result.ErrorMessage;
							problemReport.SourceRA = m_parserAgent;
							problemReport.AnnotationTypeRA = null;
							problemReport.BeginObjectRA = result.Wordform;
							FinishWordForm(result.Wordform);
						}
						else
						{
							// update the wordform
							foreach (var analysis in result.Analyses)
								ProcessAnalysis(result.Wordform, analysis);
							FinishWordForm(result.Wordform);
							RemoveUnlovedParses(result.Wordform);
						}
						result.Wordform.Checksum = (int)result.Crc;
					}
					// notify all listeners that the wordform has been updated
					FireWordformUpdated(result.Wordform, result.Priority);
					long ttlTicks = DateTime.Now.Ticks - startTime.Ticks;
					m_ticksFiler += ttlTicks;
					m_numberOfWordForms++;
					Trace.WriteLineIf(m_tracingSwitch.TraceInfo, "parser filer(" + form + ") took : " + TimeSpan.FromTicks(ttlTicks).TotalMilliseconds);
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
						mb.InflTypeRA = morph.InflType;
				}
				matches.Add(newAnal);
			}
			// (Re)set evaluations.
			foreach (var matchingAnal in matches)
			{
				m_parserAgent.SetEvaluation(matchingAnal,
					Opinions.approves);
				m_analysesWithOldEvaluation.Remove(matchingAnal);
			}
		}

		/// <summary>
		///
		/// </summary>
		private void RemoveUnlovedParses(IWfiWordform wordform)
		{
			// Solves LT-1842.
			/*
				Get all the IDs for Analyses that belong to the wordform, but which don't have an
				evaluation belonging to the given agent.  These will all be set to FAILED.
			*/
			foreach (var nobodyCareAboutMeAnalysis in wordform.AnalysesOC.Where(anal =>
							anal.GetAgentOpinion(m_parserAgent) != Opinions.approves // Parser doesn't like it
							&& anal.GetAgentOpinion(m_userAgent) == Opinions.noopinion)) // And, Human doesn't care
			{
				// A parser never has 'Opinions.disapproves'
				//m_parserAgent.SetEvaluation(failure, Opinions.disapproves);
				m_analysesWithOldEvaluation.Remove(nobodyCareAboutMeAnalysis);
				nobodyCareAboutMeAnalysis.Delete();
			}
		}

		#region Wordform Preparation methods

		private void FinishWordForm(IWfiWordform wordform)
		{
			// the following is a port of the SP RemoveUnusedAnalyses

			// Delete stale evaluations on analyses. The only non-stale analyses are new, positive ones, so this
			// makes all analyses that are not known to be correct no-opinion. Later any of them that survive at all
			// will be changed to failed (if there was no error in parsing the wordform).
			var analysesNotUpdated = from analysis in wordform.AnalysesOC where m_analysesWithOldEvaluation.Contains(analysis) select analysis;
			foreach (var analysis in analysesNotUpdated)
				analysis.SetAgentOpinion(m_parserAgent, Opinions.noopinion);

			// Make sure all analyses have human evaluations, if they,
			// or glosses they own, are referred to by an ISegment.
			//var annLookup = m_baseAnnotationRepository.AllInstances()
			//	.Where(ann => ann.AnnotationTypeRA != null && ann.AnnotationTypeRA.Guid == CmAnnotationDefnTags.kguidAnnWordformInContext)
			//	.ToLookup(ann => ann.InstanceOfRA);
			var segmentAnalyses = new HashSet<IAnalysis>();
			foreach (var seg in wordform.OccurrencesBag)
				segmentAnalyses.UnionWith(seg.AnalysesRS.ToArray());
			var analyses = from anal in wordform.AnalysesOC
						   where segmentAnalyses.Contains(anal) || anal.MeaningsOC.Any(segmentAnalyses.Contains)
						   select anal;
			foreach (var analysis in analyses)
				m_userAgent.SetEvaluation(analysis, Opinions.approves);

			// Delete orphan analyses, which have no evaluations (Review JohnT: should we also check for no owned WfiGlosses?)
			var orphanedAnalyses = from anal in wordform.AnalysesOC
								   where anal.EvaluationsRC.Count == 0
								   select anal;
			foreach (var analysis in orphanedAnalyses)
				m_cache.DomainDataByFlid.DeleteObj(analysis.Hvo);
		}

		#endregion Wordform Preparation methods

		#endregion Private methods

		public static int IndexOfLexEntryRef(string sHvo, int indexOfPeriod)
		{
			int index = 0;
			if (indexOfPeriod >= 0)
			{
				string sIndex = sHvo.Substring(indexOfPeriod+1);
				index = Convert.ToInt32(sIndex);
			}
			return index;
		}

		public static int IndexOfPeriodInMsaHvo(ref string sObjHvo)
		{
			// Irregulary inflected forms can a combination MSA hvo: the LexEntry hvo, a period, and an index to the LexEntryRef
			int indexOfPeriod = sObjHvo.IndexOf('.');
			if (indexOfPeriod >= 0)
			{
				sObjHvo = sObjHvo.Substring(0, indexOfPeriod);
			}
			return indexOfPeriod;
		}
	}
}
