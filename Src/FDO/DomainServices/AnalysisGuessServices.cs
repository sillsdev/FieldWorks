// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: AnalysisGuessServices.cs
// Responsibility: pyle
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System.Collections.Generic;
using System.Linq;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO.Infrastructure;

namespace SIL.FieldWorks.FDO.DomainServices
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class AnalysisGuessServices
	{
		/// <summary>
		///
		/// </summary>
		/// <param name="cache"></param>
		public AnalysisGuessServices(FdoCache cache)
		{
			Cache = cache;
		}

		/// <summary>
		///
		/// </summary>
		public enum OpinionAgent
		{
			/// <summary>
			///
			/// </summary>
			Computer = -1,
			/// <summary>
			///
			/// </summary>
			Parser = 0,
			/// <summary>
			///
			/// </summary>
			Human = 1,
		}

		FdoCache Cache { get; set; }

		private IDictionary<IWfiAnalysis, ICmAgentEvaluation> m_analysisApprovalTable;
		/// <summary>
		/// Table that has user opinions about analyses.
		/// </summary>
		IDictionary<IWfiAnalysis, ICmAgentEvaluation> AnalysisApprovalTable
		{
			get
			{
				if (m_analysisApprovalTable == null)
					LoadAnalysisApprovalTable();
				return m_analysisApprovalTable;
			}
			set { m_analysisApprovalTable = value; }
		}

		private HashSet<IWfiAnalysis> m_computerApprovedTable;
		/// <summary>
		/// Table for which analyses have been approved by a computer (i.e. for matching words to Entries)
		/// </summary>
		HashSet<IWfiAnalysis> ComputerApprovedTable
		{
			get
			{
				if (m_computerApprovedTable == null)
					LoadComputerApprovedTable();
				return m_computerApprovedTable;
			}
			set { m_computerApprovedTable = value; }
		}

		private HashSet<IWfiAnalysis> m_parserApprovedTable;
		/// <summary>
		/// Table for which analyses have been approved by grammatical parser
		/// </summary>
		HashSet<IWfiAnalysis> ParserApprovedTable
		{
			get
			{
				if (m_parserApprovedTable == null)
					LoadParserApprovedTable();
				return m_parserApprovedTable;
			}
			set { m_parserApprovedTable = value; }
		}

		private IDictionary<IAnalysis, IAnalysis> m_guessTable;
		IDictionary<IAnalysis, IAnalysis> GuessTable
		{
			get
			{
				if (m_guessTable == null)
					LoadGuessTable();
				return m_guessTable;
			}
			set { m_guessTable = value; }
		}

		/// <summary>
		/// Informs the guess service that the indicated occurrence is being replaced with the specified new
		/// analysis. If necessary clear the GuessTable. If possible update it. The most common and
		/// performance-critical case is confirming a guess. Return true if the cache was changed.
		/// </summary>
		public bool UpdatingOccurrence(IAnalysis oldAnalysis, IAnalysis newAnalysis)
		{
			if (m_guessTable == null)
				return false; // already cleared, forget it.
			if (oldAnalysis == newAnalysis)
				return false; // nothing changed, no problem.
			if (!(oldAnalysis is IWfiWordform))
			{
				// In general, no predicting what effect it has on the guess for the
				// wordform or analysis that owns the old analysis. If the old analysis is
				// not the default for its owner or owner's owner, we are OK, but that's too rare
				// to worry about.
				ClearGuessData();
				return true;
			}
			if (newAnalysis is IWfiWordform || newAnalysis.Wordform == null)
				return false; // unlikely but doesn't mess up our guesses.
			var result = false;
			// if the new analysis is NOT the guess for one of its owners, one more occurrence might
			// make it the guess, so we need to regenerate.
			IAnalysis currentDefault;
			if (!m_guessTable.TryGetValue(newAnalysis.Wordform, out currentDefault))
			{
				// We have no guess for this wordform: the new analysis becomes it.
				m_guessTable[newAnalysis.Wordform] = newAnalysis;
				result = true; // we didn't clear the cache but did change it.
			}
			else if (currentDefault != newAnalysis)
			{
				// Some other analysis just became more common...maybe now the default?
				ClearGuessData();
				return true;
			}
			if (newAnalysis is IWfiAnalysis)
				return result;
			if (!m_guessTable.TryGetValue(newAnalysis.Analysis, out currentDefault))
			{
				// We have no guess for this analysis: the new analysis becomes it.
				m_guessTable[newAnalysis.Analysis] = newAnalysis;
				result = true; // we didn't clear the cache but did change it.
			}
			else if (currentDefault != newAnalysis)
			{
				// Some other analysis just became more common...maybe now the default?
				ClearGuessData();
				return true;
			}
			// We haven't messed up any guesses so the guess table can survive.
			return result; // but we may have filled in some guesses.
		}

		bool IsNotDisapproved(IWfiAnalysis wa)
		{
			ICmAgentEvaluation cae;
			if (AnalysisApprovalTable.TryGetValue(wa, out cae))
				return cae.Approves;
			return true; // no opinion
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="wa"></param>
		/// <returns>by default, returns Human agent (e.g. could be approved in text)</returns>
		public OpinionAgent GetOpinionAgent(IWfiAnalysis wa)
		{
			if (IsHumanApproved(wa))
				return OpinionAgent.Human;
			if (IsParserApproved(wa))
				return OpinionAgent.Parser;
			if (IsComputerApproved(wa))
				return OpinionAgent.Computer;
			return OpinionAgent.Human;

		}
		/// <summary>
		///
		/// </summary>
		/// <param name="wa"></param>
		/// <returns></returns>
		public bool IsHumanApproved(IWfiAnalysis wa)
		{
			ICmAgentEvaluation cae;
			if (AnalysisApprovalTable.TryGetValue(wa, out cae))
				return cae.Approves;
			return false; // no opinion
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="candidate"></param>
		/// <returns></returns>
		public bool IsComputerApproved(IWfiAnalysis candidate)
		{
			return ComputerApprovedTable.Contains(candidate);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="candidate"></param>
		/// <returns></returns>
		public bool IsParserApproved(IWfiAnalysis candidate)
		{
			return ParserApprovedTable.Contains(candidate);
		}

		void LoadAnalysisApprovalTable()
		{
			var dictionary = new Dictionary<IWfiAnalysis, ICmAgentEvaluation>();
			foreach(var analysis in Cache.ServiceLocator.GetInstance<IWfiAnalysisRepository>().AllInstances())
				foreach (var ae in analysis.EvaluationsRC)
					if (((ICmAgent) ae.Owner).Human)
						dictionary[analysis] = ae;
			AnalysisApprovalTable = dictionary;
		}

		void LoadComputerApprovedTable()
		{
			IEnumerable<IWfiAnalysis> list = GetAgentApprovedList(Cache.LangProject.DefaultComputerAgent);
			ComputerApprovedTable = new HashSet<IWfiAnalysis>(list);
		}

		/// <summary>
		/// Get all the analyses approved by the specified agent.
		/// </summary>
		/// <param name="agent"></param>
		/// <returns></returns>
		private IEnumerable<IWfiAnalysis> GetAgentApprovedList(ICmAgent agent)
		{
			return Cache.ServiceLocator.GetInstance<IWfiAnalysisRepository>().AllInstances().Where(
				analysis => analysis.GetAgentOpinion(agent) == Opinions.approves);
		}

		void LoadParserApprovedTable()
		{
			IEnumerable<IWfiAnalysis> list = GetAgentApprovedList(Cache.LangProject.DefaultParserAgent);
			ParserApprovedTable = new HashSet<IWfiAnalysis>(list);
		}

		/// <summary>
		/// NOTE: this only gets analyses and glosses that are referred to by Segment.AnalysesRS
		/// </summary>
		/// <returns></returns>
		IEnumerable<IAnalysis> GetAllAnalysesAndGlossesOrderedByFrequencyOfUseInText()
		{
			return from seg in Cache.ServiceLocator.GetInstance<ISegmentRepository>().AllInstances()
				   from analysis in seg.AnalysesRS
				   where (analysis is IWfiAnalysis || analysis is IWfiGloss)
				   group analysis by analysis
				   into countedAnalysis
					   orderby countedAnalysis.Count()
					   select countedAnalysis.Key;
		}

		/// <summary>
		/// This class stores the relevant database ids for information which can generate a
		/// default analysis for a WfiWordform that has no analyses, but whose form exactly
		/// matches a LexemeForm or an AlternateForm of a LexEntry.
		/// </summary>
		private struct EmptyWwfInfo
		{
			public readonly IMoForm Form;
			public readonly IMoMorphSynAnalysis Msa;
			public readonly IPartOfSpeech Pos;
			public readonly ILexSense Sense;
			public EmptyWwfInfo(IMoForm form, IMoMorphSynAnalysis msa, IPartOfSpeech pos, ILexSense sense)
			{
				Form = form;
				Msa = msa;
				Pos = pos;
				Sense = sense;
			}
		}

		// Allows comparing strings in dictionary.
		class TsStringEquator : IEqualityComparer<ITsString>
		{
			public bool Equals(ITsString x, ITsString y)
			{
				return x.Equals(y); // NOT ==
			}

			public int GetHashCode(ITsString obj)
			{
				return(obj.Text ?? "").GetHashCode() ^ obj.get_WritingSystem(0);
			}
		}

		/// <summary>
		/// For a given text, go through all the paragraph Segment.Analyses looking for words
		/// to be candidates for guesses provided by matching to entries in the Lexicon.
		/// For a word to be a candidate for a computer guess it must have
		/// 1) no analyses that have human approved/disapproved evaluations and
		/// 2) no analyses that have computer approved/disapproved evaluations
		/// </summary>
		IDictionary<IWfiWordform, EmptyWwfInfo> MapWordsForComputerGuessesToBestMatchingEntry(IStText stText)
		{
			var wordsToMatch = new Dictionary<IWfiWordform, ITsString>();
			foreach (IStTxtPara para in stText.ParagraphsOS)
			{
				foreach (
					var analysisOccurrence in
						SegmentServices.StTextAnnotationNavigator.GetWordformOccurrencesAdvancingInPara(para)
							.Where(ao => ao.Analysis is IWfiWordform))
				{
					var word = analysisOccurrence.Analysis.Wordform;
					if (!wordsToMatch.ContainsKey(word) && !HasAnalysis(word))
					{
						var tssWord = analysisOccurrence.BaselineText;
						wordsToMatch[word] = tssWord;
					}
				}
			}
			return MapWordsForComputerGuessesToBestMatchingEntry(wordsToMatch);
		}

		/// <summary>
		/// This overload finds defaults for a specific wordform
		/// </summary>
		private Dictionary<IWfiWordform, EmptyWwfInfo> MapWordsForComputerGuessesToBestMatchingEntry(IWfiWordform wf, int ws)
		{
			var wordsToMatch = new Dictionary<IWfiWordform, ITsString>();
			wordsToMatch[wf] = wf.Form.get_String(ws);
			return MapWordsForComputerGuessesToBestMatchingEntry(wordsToMatch);
		}


		/// <summary>
		/// This overload finds guesses for wordforms specified in a dictionary that maps a wordform to the
		/// string that we want to match (might be a different case form).
		/// </summary>
		/// <param name="wordsToMatch"></param>
		/// <returns></returns>
		private Dictionary<IWfiWordform, EmptyWwfInfo> MapWordsForComputerGuessesToBestMatchingEntry(Dictionary<IWfiWordform, ITsString> wordsToMatch)
		{
			var matchingMorphs = new Dictionary<ITsString, IMoStemAllomorph>(new TsStringEquator());
			foreach (var tssWord in wordsToMatch.Values)
				matchingMorphs[tssWord] = null;
			MorphServices.GetMatchingMonomorphemicMorphs(Cache, matchingMorphs);
			var mapEmptyWfInfo = new Dictionary<IWfiWordform, EmptyWwfInfo>();
			foreach (var kvp in wordsToMatch)
			{
				var word = kvp.Key;
				var tssWord = kvp.Value;
				var bestMatchingMorph = matchingMorphs[tssWord];
				if (bestMatchingMorph != null)
				{
					var entryOrVariant = bestMatchingMorph.OwnerOfClass<ILexEntry>();
					ILexEntry mainEntry;
					ILexSense sense;
					GetMainEntryAndSense(entryOrVariant, out mainEntry, out sense);
					if (sense == null && mainEntry.SensesOS.Count > 0)
					{
						sense = mainEntry.SensesOS.Where(s => s.MorphoSyntaxAnalysisRA is IMoStemMsa)
							.FirstOrDefault();
					}
					IMoStemMsa msa = null;
					IPartOfSpeech pos = null;
					if (sense != null)
					{
						msa = (IMoStemMsa) sense.MorphoSyntaxAnalysisRA;
						pos = msa.PartOfSpeechRA;
					}
					// map the word to its best entry.
					var entryInfo = new EmptyWwfInfo(bestMatchingMorph, msa, pos, sense);
					mapEmptyWfInfo.Add(word, entryInfo);
				}

			}
			return mapEmptyWfInfo;
		}

		private void GetMainEntryAndSense(ILexEntry entryOrVariant, out ILexEntry mainEntry, out ILexSense sense)
		{
			sense = null;
			// first see if this is a variant of another entry.
			var entryRef = DomainObjectServices.GetVariantRef(entryOrVariant, true);
			if (entryRef != null)
			{
				// get the main entry or sense.
				var component = entryRef.ComponentLexemesRS[0] as IVariantComponentLexeme;
				if (component is ILexSense)
				{
					sense = component as ILexSense;
					mainEntry = sense.Entry;
				}
				else
				{
					mainEntry = component as ILexEntry;
					// consider using the sense of the variant, if it has one. (LT-9681)
				}
			}
			else
			{
				mainEntry = entryOrVariant;
			}
		}

		private bool HasAnalysis(IWfiWordform word)
		{
			return word.AnalysesOC.Count > 0;
		}

		/// <summary>
		/// For the given text, find words for which we can generate analyses that match lexical entries.
		/// </summary>
		/// <param name="stText"></param>
		public void GenerateEntryGuesses(IStText stText)
		{
			GenerateEntryGuesses(MapWordsForComputerGuessesToBestMatchingEntry(stText));
		}

		/// <summary>
		/// For the given wordform, find words for which we can generate analyses that match lexical entries.
		/// </summary>
		internal void GenerateEntryGuesses(IWfiWordform wf, int ws)
		{
			GenerateEntryGuesses(MapWordsForComputerGuessesToBestMatchingEntry(wf, ws));
		}

		/// <summary>
		/// For the given sequence of correspondences, generate analyses.
		/// </summary>
		private void GenerateEntryGuesses(IDictionary<IWfiWordform, EmptyWwfInfo> map)
		{
			var waFactory = Cache.ServiceLocator.GetInstance<IWfiAnalysisFactory>();
			var wgFactory = Cache.ServiceLocator.GetInstance<IWfiGlossFactory>();
			var wmbFactory = Cache.ServiceLocator.GetInstance<IWfiMorphBundleFactory>();
			var computerAgent = Cache.LangProject.DefaultComputerAgent;
			foreach (var keyPair in map)
			{
				var ww = keyPair.Key;
				var info = keyPair.Value;
				if (!HasAnalysis(ww))
				{
					NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUowOrSkip(Cache.ActionHandlerAccessor,
						"Trying to generate guesses during PropChanged when we can't save them.",
						() =>
							{
								var newAnalysis = waFactory.Create(ww, wgFactory);
								newAnalysis.CategoryRA = info.Pos;
								// Not all entries have senses.
								if (info.Sense != null)
								{
									// copy all the gloss alternatives from the sense into the word gloss.
									IWfiGloss wg = newAnalysis.MeaningsOC.First();
									wg.Form.MergeAlternatives(info.Sense.Gloss);
								}
								var wmb = wmbFactory.Create();
								newAnalysis.MorphBundlesOS.Add(wmb);
								if (info.Form != null)
									wmb.MorphRA = info.Form;
								if (info.Msa != null)
									wmb.MsaRA = info.Msa;
								if (info.Sense != null)
									wmb.SenseRA = info.Sense;

								// Now, set up an approved "Computer" evaluation of this generated analysis
								computerAgent.SetEvaluation(newAnalysis, Opinions.approves);
							});
				}
			}
		}

		void LoadGuessTable()
		{
			GuessTable = new Dictionary<IAnalysis, IAnalysis>();

			HashSet<IWfiAnalysis> analysesRemaining;
			HashSet<IWfiGloss> glossesRemaining;
			AddOccurrenceApprovedAnalysesAndGlossesToGuessTable(out analysesRemaining, out glossesRemaining);

			// add any remaining Human approved analyses.
			AddRemainingNonDisapprovedGlossesAndAnalysesToGuessTable(glossesRemaining, analysesRemaining,
				IsHumanApproved);

			// next go through any (Parser) generated analyses and glosses.
			AddRemainingNonDisapprovedGlossesAndAnalysesToGuessTable(glossesRemaining, analysesRemaining,
				IsParserApprovedAndNotDisapproved);

			// lastly, add any remaining approved analyses/glosses (e.g. Computer guesses)
			AddRemainingNonDisapprovedGlossesAndAnalysesToGuessTable(glossesRemaining, analysesRemaining,
				IsNotDisapproved);

		}

		/// <summary>
		/// Whenever the data we depend upon changes, use this to make sure we load the latest Guess data.
		/// </summary>
		public void ClearGuessData()
		{
			GuessTable = null;
			ParserApprovedTable = null;
			ComputerApprovedTable = null;
			AnalysisApprovalTable = null;
		}

		/// <summary>
		/// adds analyses/glosses that have been approved in a text.
		/// </summary>
		/// <param name="analysesRemaining">analyses that were not processed by this routine</param>
		/// <param name="glossesRemaining">glosses that were not processed by this routine</param>
		private void AddOccurrenceApprovedAnalysesAndGlossesToGuessTable(
			out HashSet<IWfiAnalysis> analysesRemaining,
			out HashSet<IWfiGloss> glossesRemaining)
		{
			// keep track of the analyses we've made a decision about whether to load into the GuessTable.
			analysesRemaining = new HashSet<IWfiAnalysis>(Cache.ServiceLocator.GetInstance<IWfiAnalysisRepository>().AllInstances());
			glossesRemaining = new HashSet<IWfiGloss>(Cache.ServiceLocator.GetInstance<IWfiGlossRepository>().AllInstances());
			foreach (var wag in GetAllAnalysesAndGlossesOrderedByFrequencyOfUseInText())
			{
				if (wag is IWfiAnalysis)
				{
					// since an occurrence has an instanceOf this analysis
					GuessTable[wag.Wordform] = wag;
					analysesRemaining.Remove(wag.Analysis);
				}
				if (wag is IWfiGloss)
				{
					GuessTable[wag.Wordform] = wag;
					GuessTable[wag.Analysis] = wag;
					glossesRemaining.Remove(wag as IWfiGloss);
					analysesRemaining.Remove(wag.Analysis);
				}
			}
		}

		private delegate bool AddToGuessTableCondition(
			IDictionary<IAnalysis, IAnalysis> guessMap, IAnalysis candidate);


		bool IsHumanApproved(IDictionary<IAnalysis, IAnalysis> guessMap, IAnalysis candidate)
		{
			return !guessMap.Keys.Contains(candidate.Wordform) && IsHumanApproved(candidate.Analysis);
		}

		bool IsParserApprovedAndNotDisapproved(IDictionary<IAnalysis, IAnalysis> guessMap, IAnalysis candidate)
		{
			return !guessMap.Keys.Contains(candidate.Wordform) && IsNotDisapproved(candidate.Analysis) && IsParserApproved(candidate.Analysis);
		}

		bool IsNotDisapproved(IDictionary<IAnalysis, IAnalysis> guessMap, IAnalysis candidate)
		{
			return !guessMap.Keys.Contains(candidate.Wordform) && IsNotDisapproved(candidate.Analysis);
		}

		private void AddRemainingNonDisapprovedGlossesAndAnalysesToGuessTable(
			HashSet<IWfiGloss> glossesRemaining,
			HashSet<IWfiAnalysis> analysesRemaining,
			AddToGuessTableCondition accept)
		{
			IDictionary<IAnalysis, IAnalysis> tmpGuesses = new Dictionary<IAnalysis, IAnalysis>();
			foreach (var wg in glossesRemaining)
			{
				// approved analyses have precendence over "no opinion".
				if (accept(tmpGuesses, wg))
				{
					tmpGuesses[wg.Wordform] = wg;
					tmpGuesses[wg.Analysis] = wg;
				}
			}
			foreach (var wa in analysesRemaining)
			{
				// approved analyses have precendence over "no opinion".
				if (accept(tmpGuesses, wa))
				{
					tmpGuesses[wa.Wordform] = wa;
				}
			}
			foreach (var pair in tmpGuesses)
			{
				// don't overwrite any existing mapping from texts
				if (!GuessTable.Keys.Contains<IAnalysis>(pair.Key))
					GuessTable.Add(pair);
				if (pair.Value is IWfiGloss)
					glossesRemaining.Remove(pair.Value as IWfiGloss);
				analysesRemaining.Remove(pair.Value.Analysis);
			}
		}

		/// <summary>
		/// Given a wordform, provide the best analysis guess for it (using the default vernacular WS).
		/// </summary>
		/// <param name="wf"></param>
		/// <returns></returns>
		public IAnalysis GetBestGuess(IWfiWordform wf)
		{
			return GetBestGuess(wf, wf.Cache.DefaultVernWs);
		}

		/// <summary>
		/// Given a wf provide the best guess based on the user-approved analyses (in or outside of texts).
		/// If we don't already have a guess, this will try to create one from the lexicon, based on the
		/// form in the specified WS.
		/// </summary>
		public IAnalysis GetBestGuess(IWfiWordform wf, int ws)
		{
			IAnalysis wag;
			if (GuessTable.TryGetValue(wf, out wag))
				return wag;
			if (wf.AnalysesOC.Count == 0)
			{
				GenerateEntryGuesses(wf, ws);
				if (GuessTable.TryGetValue(wf, out wag))
					return wag;
			}
			return new NullWAG();
		}

		/// <summary>
		/// Given a wa provide the best guess based on glosses for that analysis (made in or outside of texts).
		/// </summary>
		/// <param name="wa"></param>
		/// <returns></returns>
		public IAnalysis GetBestGuess(IWfiAnalysis wa)
		{
			IAnalysis wag;
			if (GuessTable.TryGetValue(wa, out wag))
				return wag;
			return new NullWAG();
		}

		/// <summary>
		/// This guess factors in the placement of an occurrence in its segment for making other
		/// decisions like matching lowercase alternatives for sentence initial occurrences.
		/// </summary>
		public IAnalysis GetBestGuess(AnalysisOccurrence occurrence)
		{
			// first see if we can make a guess based on the lowercase form of a sentence initial (non-lowercase) wordform
			// TODO: make it look for the first word in the sentence...may not be at Index 0!
			if (occurrence.Analysis is IWfiWordform && occurrence.Index == 0)
			{
				ITsString tssWfBaseline = occurrence.BaselineText;
				var tracker = new CpeTracker(Cache.WritingSystemFactory, tssWfBaseline);
				ILgCharacterPropertyEngine cpe = tracker.CharPropEngine(0);
				string sLower = cpe.ToLower(tssWfBaseline.Text);
				// don't bother looking up the lowercased wordform if the instanceOf is already in lowercase form.
				if (sLower != tssWfBaseline.Text)
				{
					ITsString tssLower = TsStringUtils.MakeTss(sLower, TsStringUtils.GetWsAtOffset(tssWfBaseline, 0));
					IWfiWordform lowercaseWf;
					if (Cache.ServiceLocator.GetInstance<IWfiWordformRepository>().TryGetObject(tssLower, out lowercaseWf))
					{
						IAnalysis bestGuess;
						if (TryGetBestGuess(lowercaseWf, occurrence.BaselineWs, out bestGuess))
							return bestGuess;
					}
				}
			}
			return GetBestGuess(occurrence.Analysis, occurrence.BaselineWs);
		}

		private IAnalysis GetBestGuess(IAnalysis wag, int ws)
		{
			if (wag is IWfiWordform)
				return GetBestGuess(wag.Wordform, ws);
			if (wag is IWfiAnalysis)
				return GetBestGuess(wag.Analysis);
			return new NullWAG();
		}

		/// <summary>
		///
		/// </summary>
		public bool TryGetBestGuess(IAnalysis wag, int ws, out IAnalysis bestGuess)
		{
			bestGuess = GetBestGuess(wag, ws);
			return !(bestGuess is NullWAG);
		}

		/// <summary>
		///
		/// </summary>
		public bool TryGetBestGuess(AnalysisOccurrence occurrence, out IAnalysis bestGuess)
		{
			bestGuess = GetBestGuess(occurrence);
			return !(bestGuess is NullWAG);
		}
	}
}
