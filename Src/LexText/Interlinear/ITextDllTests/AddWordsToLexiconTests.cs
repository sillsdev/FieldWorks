using System.Collections.Generic;
using System.Linq;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.FDOTests;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.Utils;
using SIL.CoreImpl;

namespace SIL.FieldWorks.IText
{

	/// <summary>
	/// </summary>
	[TestFixture]
	public class AddWordsToLexiconTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private FDO.IText m_text1;
		private SandboxForTests m_sandbox;

		/// <summary>
		///
		/// </summary>
		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor,
				DoSetupFixture);
		}

		/// <summary>
		/// non-undoable task
		/// </summary>
		private void DoSetupFixture()
		{
			var textFactory = Cache.ServiceLocator.GetInstance<ITextFactory>();
			var stTextFactory = Cache.ServiceLocator.GetInstance<IStTextFactory>();
			m_text1 = textFactory.Create();
			//Cache.LangProject.TextsOC.Add(m_text1);
			var stText1 = stTextFactory.Create();
			m_text1.ContentsOA = stText1;
			var para1 = stText1.AddNewTextPara(null);
			(m_text1.ContentsOA[0]).Contents =
				TsStringUtils.MakeTss("xxxa xxxb xxxc xxxd xxxe, xxxa xxxb.", Cache.DefaultVernWs);
			InterlinMaster.LoadParagraphAnnotationsAndGenerateEntryGuessesIfNeeded(stText1, false);

			// setup language project parts of speech
			var partOfSpeechFactory = Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>();
			var adjunct = partOfSpeechFactory.Create();
			var noun = partOfSpeechFactory.Create();
			var verb = partOfSpeechFactory.Create();
			var transitiveVerb = partOfSpeechFactory.Create();
			Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.Add(adjunct);
			Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.Add(noun);
			Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.Add(verb);
			verb.SubPossibilitiesOS.Add(transitiveVerb);
			adjunct.Name.set_String(Cache.DefaultAnalWs, "adjunct");
			noun.Name.set_String(Cache.DefaultAnalWs, "noun");
			verb.Name.set_String(Cache.DefaultAnalWs, "verb");
			transitiveVerb.Name.set_String(Cache.DefaultAnalWs, "transitive verb");
		}

		public override void TestTearDown()
		{
			// Dispose managed resources here.
			if (m_sandbox != null)
				m_sandbox.Dispose();
			base.TestTearDown();
		}

		[SetUp]
		public override void TestSetup()
		{
			base.TestSetup();
			InterlinLineChoices lineChoices = InterlinLineChoices.DefaultChoices(Cache.LangProject,
																				 Cache.DefaultVernWs,
																				 Cache.DefaultAnalWs,
																				 InterlinLineChoices.InterlinMode.
																					 GlossAddWordsToLexicon);
			m_sandbox = new SandboxForTests(Cache, lineChoices);
		}


		internal class SandboxForTests : Sandbox
		{
			internal SandboxForTests(FdoCache cache, InterlinLineChoices lineChoices)
				: base(cache, null, null, lineChoices)
			{
			}

			ISilDataAccess SandboxCacheDa
			{
				get { return m_caches.DataAccess; }
			}

#pragma warning disable 169
			ISilDataAccess MainCacheDa
			{
				get { return m_caches.MainCache.MainCacheAccessor; }
			}
#pragma warning restore 169

			internal ITsString GetTssInSandbox(int flid, int ws)
			{
				ITsString tss = null;
				switch (flid)
				{
					default:
						tss = null;
						break;
					case InterlinLineChoices.kflidWordGloss:
						tss = SandboxCacheDa.get_MultiStringAlt(kSbWord, ktagSbWordGloss, ws);
						break;
				}
				return tss;
			}

			internal int GetRealHvoInSandbox(int flid, int ws)
			{
				int hvo = 0;
				switch (flid)
				{
					default:
						break;
					case InterlinLineChoices.kflidWordPos:
						hvo = m_caches.RealHvo(SandboxCacheDa.get_ObjectProp(kSbWord, ktagSbWordPos));
						break;
				}
				return hvo;
			}


			internal ITsString SetTssInSandbox(int flid, int ws, string str)
			{
				ITsString tss = TsStringUtils.MakeTss(str, ws);
				switch (flid)
				{
					default:
						tss = null;
						break;
					case InterlinLineChoices.kflidWordGloss:
						m_caches.DataAccess.SetMultiStringAlt(kSbWord, ktagSbWordGloss, ws, tss);
						break;
				}
				return tss;
			}

			/// <summary>
			///
			/// </summary>
			/// <param name="flid"></param>
			/// <param name="morphIndex"></param>
			/// <param name="index"></param>
			/// <returns>hvo of item in Items</returns>
			internal int SelectIndexInCombo(int flid, int morphIndex, int index)
			{
				using (InterlinComboHandler handler = GetComboHandler(flid, morphIndex))
				{
					handler.HandleSelect(index);
					return handler.Items[handler.IndexOfCurrentItem];
				}
			}

			/// <summary>
			///
			/// </summary>
			/// <param name="flid"></param>
			/// <param name="morphIndex"></param>
			/// <param name="comboItem"></param>
			/// <returns>index of item</returns>
			internal int SelectItemInCombo(int flid, int morphIndex, string comboItem)
			{
				using (InterlinComboHandler handler = GetComboHandler(flid, morphIndex))
				{
					handler.SelectComboItem(comboItem);
					return handler.IndexOfCurrentItem;
				}
			}

			/// <summary>
			///
			/// </summary>
			/// <param name="flid"></param>
			/// <param name="morphIndex"></param>
			/// <param name="hvoTarget"></param>
			/// <returns>index of item in combo</returns>
			internal int SelectItemInCombo(int flid, int morphIndex, int hvoTarget)
			{
				using (InterlinComboHandler handler = GetComboHandler(flid, morphIndex))
				{
					handler.SelectComboItem(hvoTarget);
					return handler.IndexOfCurrentItem;
				}

			}


			protected override bool ShouldAddWordGlossToLexicon
			{
				get
				{
					return true;
				}
			}

			internal AnalysisTree ConfirmAnalysis()
			{
				IWfiAnalysis obsoleteAna;
				return GetRealAnalysis(true, out obsoleteAna);
			}

			internal ILexSense GetLexSenseForWord()
			{
				List<int> hvoSenses = LexSensesForCurrentMorphs();
				// the sense only represents the whole word if there is only one morph.
				if (hvoSenses.Count != 1)
					return null;
				return Cache.ServiceLocator.GetInstance<ILexSenseRepository>().GetObject(hvoSenses[0]);
			}

			private InterlinComboHandler GetComboHandler(int flid, int morphIndex)
			{
				// first select the proper pull down icon.
				int tagIcon = 0;
				switch (flid)
				{
					default:
						break;
					case InterlinLineChoices.kflidWordGloss:
						tagIcon = ktagWordGlossIcon;
						break;
					case InterlinLineChoices.kflidWordPos:
						tagIcon = ktagWordPosIcon;
						break;
				}
				return InterlinComboHandler.MakeCombo(
					m_mediator != null ? m_mediator.HelpTopicProvider : null, tagIcon,
					this, morphIndex) as InterlinComboHandler;
			}

			internal List<int> GetComboItems(int flid, int morphIndex)
			{
				List<int> items = new List<int>();
				using (InterlinComboHandler handler = GetComboHandler(flid, morphIndex))
				{
					items.AddRange(handler.Items);
				}
				return items;
			}
			/// <summary>
			///
			/// </summary>
			/// <param name="flid"></param>
			/// <param name="morphIndex"></param>
			/// <param name="target"></param>
			/// <returns></returns>
			internal int GetComboItemHvo(int flid, int morphIndex, string target)
			{
				using (InterlinComboHandler handler = GetComboHandler(flid, morphIndex))
				{
					int index;
					object item = handler.GetComboItem(target, out index);
					if (item != null)
						return handler.Items[index];
				}
				return 0;
			}
		}

		void CompareTss(ITsString tssExpected, ITsString tssActual)
		{
			if (tssExpected != null && tssActual != null)
			{
				Assert.AreEqual(tssExpected.Text, tssActual.Text);
				Assert.IsTrue(tssExpected.Equals(tssActual));
			}
			else
			{
				Assert.AreEqual(tssExpected, tssActual);
			}
		}

		private AnalysisOccurrence GetCba(int iPara, int iSeg, int iSegForm)
		{
			IStTxtPara para = m_text1.ContentsOA.ParagraphsOS[iPara] as IStTxtPara;
			var seg = para.SegmentsOS[iSeg];
			return new AnalysisOccurrence(seg, iSegForm);
		}

		/// <summary>
		/// keeps track of how many UndoTasks we create during a test.
		/// </summary>
		internal class UndoableUOWHelperForTests : FwDisposableBase
		{
			private IActionHandler m_actionHandler;
			private Queue<UOW> m_taskQueue = new Queue<UOW>();
			private Stack<UOW> m_doneStack = new Stack<UOW>();

			internal UndoableUOWHelperForTests(IActionHandler actionHandler)
			{
				m_actionHandler = actionHandler;
				OriginalUndoCount = m_actionHandler.UndoableSequenceCount;
			}

			int OriginalUndoCount { get; set; }

			internal void DoUOW(string undo, string redo, System.Action task)
			{
				QueueUOW(undo, redo, task);
				DoNext();
			}
			private void QueueUOW(string undo, string redo, System.Action task)
			{
				m_taskQueue.Enqueue(new UOW(undo, redo, task));
			}

			private void DoNext()
			{
				var todo = m_taskQueue.Dequeue();
				if (todo != null)
				{
					UndoableUnitOfWorkHelper.Do(todo.Undo, todo.Redo, m_actionHandler, todo.Task);
					todo.Task();
					m_doneStack.Push(todo);
				}
			}

			internal void UndoAll()
			{
				while (m_doneStack.Count > 0)
				{
					Assert.AreEqual(m_doneStack.Peek().Undo, m_actionHandler.GetUndoText());
					m_actionHandler.Undo();
					// put it back on the taskQueue as something that can be Redone.
					m_taskQueue.Enqueue(m_doneStack.Pop());
				}
				Assert.AreEqual(OriginalUndoCount, m_actionHandler.UndoableSequenceCount);
			}

			class UOW
			{
				internal UOW(string undo, string redo, System.Action task)
				{
					Undo = undo;
					Redo = redo;
					Task = task;
				}
				internal string Undo { get; set; }
				internal string Redo { get; set; }
				internal System.Action Task { get; set; }
			}

			protected override void DisposeManagedResources()
			{
				UndoAll();
			}
		}

		/// <summary>
		/// A new word gloss for a new word (i.e. no analyses) will
		/// add a new analysis to the word and
		/// also add the word to the Lexicon (LexEntry and LexSense).
		/// </summary>
		[Test]
		public void NewGlossNewLexEntryNewLexSense()
		{
			// load sandbox for first 'xxxa'
			var cba0_0 = GetCba(0, 0, 0);
			m_sandbox.SwitchWord(cba0_0);

			// verify that the word gloss is empty
			ITsString tssEmpty = TsStringUtils.MakeTss("", Cache.DefaultAnalWs);
			ITsString tssWordGloss = m_sandbox.GetTssInSandbox(InterlinLineChoices.kflidWordGloss,
															   Cache.DefaultAnalWs);
			CompareTss(tssEmpty, tssWordGloss);
			// add a new word gloss and confirm the analysis.
			ITsString tssWordGlossInSandbox = m_sandbox.SetTssInSandbox(InterlinLineChoices.kflidWordGloss,
																		Cache.DefaultAnalWs, "0.0.xxxa");
			// mark the count of LexEntries
			int cEntriesOrig = Cache.LangProject.LexDbOA.Entries.Count();
			// verify no analyses exist for this wordform;
			IWfiWordform wf = cba0_0.Analysis.Wordform;
			Assert.AreEqual(0, wf.AnalysesOC.Count);

			// set word pos, to first possibility (e.g. 'adjunct')
			int hvoSbWordPos = m_sandbox.SelectIndexInCombo(InterlinLineChoices.kflidWordPos, 0, 0);
			Assert.IsFalse(hvoSbWordPos == 0); // select nonzero pos

			// confirm the analysis (making a real analysis and a LexSense)
			var wag = m_sandbox.ConfirmAnalysis();
			var glossFactory = Cache.ServiceLocator.GetInstance<IWfiGlossFactory>();

			IWfiGloss wfiGloss = wag.Gloss;
			CompareTss(tssWordGlossInSandbox, wfiGloss.Form.get_String(Cache.DefaultAnalWs));
			// confirm we have only one analysis and that it is monomorphemic
			IWfiAnalysis wfiAnalysis = wag.WfiAnalysis;
			Assert.AreEqual(wf, wag.Wordform, "Expected confirmed analysis to be owned by the original wordform.");
			Assert.AreEqual(1, wf.AnalysesOC.Count);
			Assert.AreEqual(1, wfiAnalysis.MorphBundlesOS.Count);
			Assert.AreEqual(1, wfiAnalysis.MeaningsOC.Count);

			// make sure the strings of the wfi gloss matches the strings of the lex gloss.
			ValidateSenseWithAnalysis(m_sandbox.GetLexSenseForWord(), wfiGloss, hvoSbWordPos);

			// make sure a new entry is in the Lexicon.
			int cEntriesAfter = Cache.LangProject.LexDbOA.Entries.Count();
			Assert.AreEqual(cEntriesOrig + 1, cEntriesAfter);
		}


		private void ValidateSenseWithAnalysis(ILexSense sense, IWfiGloss wfiGloss, int hvoSbWordPos)
		{
			ValidateSenseWithAnalysis(sense, wfiGloss, hvoSbWordPos, false, sense.Entry.LexemeFormOA as IMoStemAllomorph);
		}

		private void ValidateSenseWithAnalysis(ILexSense sense, IWfiGloss wfiGloss, int hvoSbWordPos, bool fMatchMainPossibility, IMoStemAllomorph allomorph)
		{
			IWfiAnalysis wfiAnalysis = wfiGloss.Owner as IWfiAnalysis;
			CompareTss(sense.Gloss.get_String(Cache.DefaultAnalWs),
				wfiGloss.Form.get_String(Cache.DefaultAnalWs));

			// make sure the morph is linked to the lexicon sense, msa, and part of speech.
			IWfiMorphBundle morphBundle = wfiAnalysis.MorphBundlesOS[0];
			Assert.AreEqual(sense, morphBundle.SenseRA);
			Assert.AreEqual(sense.MorphoSyntaxAnalysisRA, morphBundle.MsaRA);
			if (!fMatchMainPossibility)
			{
				// expect exact possibility
				Assert.AreEqual(hvoSbWordPos, (sense.MorphoSyntaxAnalysisRA as IMoStemMsa).PartOfSpeechRA.Hvo);
			}
			else
			{
				IPartOfSpeech posTarget = Cache.ServiceLocator.GetInstance<IPartOfSpeechRepository>().GetObject(hvoSbWordPos);
				Assert.AreEqual(posTarget.MainPossibility, (sense.MorphoSyntaxAnalysisRA as IMoStemMsa).PartOfSpeechRA.MainPossibility);
			}
			Assert.AreEqual(allomorph, morphBundle.MorphRA);
			Assert.AreEqual(hvoSbWordPos, wfiAnalysis.CategoryRA.Hvo);
		}

		/// <summary>
		/// A new word gloss for an existing lex entry will
		/// add another sense to the existing lexicon word.
		/// </summary>
		[Test]
		public void NewGlossExistingLexEntryNewLexSense()
		{
			var cba0_0 = GetCba(0, 0, 0);
			m_sandbox.SwitchWord(cba0_0);
			ILexEntry lexEntry1_Entry;
			ILexSense lexEntry1_Sense1;
			SetupLexEntryAndSense("xxxa", "xxxa.existingsense1", out lexEntry1_Entry, out lexEntry1_Sense1);

			// mark the count of LexEntries
			int cEntriesOrig = Cache.LangProject.LexDbOA.Entries.Count();

			// add a new word gloss
			m_sandbox.SetTssInSandbox(InterlinLineChoices.kflidWordGloss,
																		Cache.DefaultAnalWs, "0.0.xxxa");
			IWfiWordform wf = cba0_0.Analysis.Wordform;
			// set word pos, to first possibility (e.g. 'adjunct')
			int hvoSbWordPos = m_sandbox.SelectIndexInCombo(InterlinLineChoices.kflidWordPos, 0, 0);

			// confirm the analysis (making a real analysis and a LexSense)
			var wag = m_sandbox.ConfirmAnalysis();
			IWfiGloss wfiGloss = wag.Gloss;

			// make sure we didn't add entries to the Lexicon.
			int cEntriesAfter = Cache.LangProject.LexDbOA.Entries.Count();
			Assert.AreEqual(cEntriesOrig, cEntriesAfter);

			// confirm we have only one analysis and that it is monomorphemic
			IWfiAnalysis wfiAnalysis = wag.WfiAnalysis;
			Assert.AreEqual(wf, wag.Wordform, "Expected confirmed analysis to be owned by the original wordform.");
			Assert.AreEqual(1, wf.AnalysesOC.Count);
			Assert.AreEqual(1, wfiAnalysis.MorphBundlesOS.Count);
			Assert.AreEqual(1, wfiAnalysis.MeaningsOC.Count);

			// make sure the strings of the wfi gloss matches the strings of the lex gloss.
			ValidateSenseWithAnalysis(m_sandbox.GetLexSenseForWord(), wfiGloss, hvoSbWordPos);
		}

		/// <summary>
		/// A new word gloss for an existing lex entry that has a matching allomorph form.
		/// </summary>
		[Test]
		public void NewGlossExistingLexEntryAllomorphNewLexSense()
		{
			var cba0_0 = GetCba(0, 0, 0);
			m_sandbox.SwitchWord(cba0_0);
			string formLexEntry = "xxxab";
			ITsString tssLexEntryForm = TsStringUtils.MakeTss(formLexEntry, Cache.DefaultVernWs);
			string formAllomorph = "xxxa";
			ITsString tssAllomorphForm = TsStringUtils.MakeTss(formAllomorph, Cache.DefaultVernWs);

			// first create an entry with a matching allomorph that doesn't match 'verb' POS we will be selecting in the sandbox
			ILexEntry lexEntry_NounPos;
			ILexSense lexSense_NounPos;
			SetupLexEntryAndSense("xxxab", "0.0.xxxab_NounPos", "noun", out lexEntry_NounPos, out lexSense_NounPos);
			var stemFactory = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>();
			var allomorph0 = stemFactory.Create();
			lexEntry_NounPos.AlternateFormsOS.Add(allomorph0);
			allomorph0.Form.set_String(TsStringUtils.GetWsAtOffset(tssAllomorphForm, 0), tssAllomorphForm);

			// now create the entry we want to match, that has a 'verb' POS.
			ILexEntry lexEntry1_Entry;
			ILexSense lexEntry1_Sense1;
			SetupLexEntryAndSense("xxxab", "0.0.xxxab_VerbPos", "verb", out lexEntry1_Entry, out lexEntry1_Sense1);
			IMoStemAllomorph allomorph = stemFactory.Create();
			lexEntry1_Entry.AlternateFormsOS.Add(allomorph);
			allomorph.MorphTypeRA = lexEntry1_Entry.LexemeFormOA.MorphTypeRA;
			allomorph.Form.set_String(TsStringUtils.GetWsAtOffset(tssAllomorphForm, 0), tssAllomorphForm);
			// mark the count of LexEntries
			int cEntriesOrig = Cache.LangProject.LexDbOA.Entries.Count();

			// add a new word gloss
			ITsString tssWordGlossInSandbox = m_sandbox.SetTssInSandbox(InterlinLineChoices.kflidWordGloss,
																		Cache.DefaultAnalWs, "0.0.xxxa");
			IWfiWordform wf = cba0_0.Analysis.Wordform;
			// set word pos to verb
			int hvoSbWordPos = m_sandbox.GetComboItemHvo(InterlinLineChoices.kflidWordPos, 0, "transitive verb");
			m_sandbox.SelectItemInCombo(InterlinLineChoices.kflidWordPos, 0, hvoSbWordPos);

			// confirm the analysis (making a real analysis and a LexSense)
			var wag = m_sandbox.ConfirmAnalysis();
			IWfiGloss wfiGloss = wag.Gloss;

			// make sure we didn't add entries to the Lexicon.
			int cEntriesAfter = Cache.LangProject.LexDbOA.Entries.Count();
			Assert.AreEqual(cEntriesOrig, cEntriesAfter);

			// confirm we have only one analysis and that it is monomorphemic
			IWfiAnalysis wfiAnalysis = wag.WfiAnalysis;
			Assert.AreEqual(wf, wag.Wordform, "Expected confirmed analysis to be owned by the original wordform.");
			Assert.AreEqual(1, wf.AnalysesOC.Count);
			Assert.AreEqual(1, wfiAnalysis.MorphBundlesOS.Count);
			Assert.AreEqual(1, wfiAnalysis.MeaningsOC.Count);

			// make sure the strings of the wfi gloss matches the strings of the lex gloss.
			ValidateSenseWithAnalysis(m_sandbox.GetLexSenseForWord(), wfiGloss, hvoSbWordPos, true, allomorph);
		}

		[Test]
		public void PickLexGlossCreatingNewAnalysis()
		{
			var cba0_0 = GetCba(0, 0, 0);
			m_sandbox.SwitchWord(cba0_0);
			ILexEntry lexEntry1_Entry;
			ILexSense lexEntry1_Sense1;
			SetupLexEntryAndSense("xxxa", "0.0.xxxa", out lexEntry1_Entry, out lexEntry1_Sense1);

			// mark the count of LexEntries
			int cEntriesOrig = Cache.LangProject.LexDbOA.Entries.Count();

			// add a new word gloss
			m_sandbox.SetTssInSandbox(InterlinLineChoices.kflidWordGloss,
				Cache.DefaultAnalWs, "0.0.xxxa");
			IWfiWordform wf = cba0_0.Analysis.Wordform;
			// set word pos, to first possibility (e.g. 'adjunct')
			int hvoSbWordPos = m_sandbox.SelectIndexInCombo(InterlinLineChoices.kflidWordPos, 0, 0);

			// confirm the analysis (making a real analysis and a LexSense)
			var wag = m_sandbox.ConfirmAnalysis();
			IWfiGloss wfiGloss = wag.Gloss;

			// make sure we didn't add entries or senses to the Lexicon.
			int cEntriesAfter = Cache.LangProject.LexDbOA.Entries.Count();
			Assert.AreEqual(cEntriesOrig, cEntriesAfter);
			Assert.AreEqual(1, lexEntry1_Entry.SensesOS.Count);

			// make sure the sense matches the existing one.
			ILexSense sense = m_sandbox.GetLexSenseForWord();
			Assert.AreEqual(lexEntry1_Sense1.Hvo, sense.Hvo);
			// make sure the morph is linked to our lexicon sense, msa, and part of speech.
			ValidateSenseWithAnalysis(sense, wfiGloss, hvoSbWordPos);

			// confirm we have created a new analysis and that it is monomorphemic
			IWfiAnalysis wfiAnalysis = wag.WfiAnalysis;
			Assert.AreEqual(wf, wag.Wordform, "Expected confirmed analysis to be owned by the original wordform.");
			Assert.AreEqual(1, wf.AnalysesOC.Count);
			Assert.AreEqual(1, wfiAnalysis.MorphBundlesOS.Count);
			Assert.AreEqual(1, wfiAnalysis.MeaningsOC.Count);
		}


		/// <summary>
		/// </summary>
		/// <param name="formLexEntry"></param>
		/// <param name="senseGloss"></param>
		/// <param name="lexEntry1_Entry"></param>
		/// <param name="lexEntry1_Sense1"></param>
		private void SetupLexEntryAndSense(string formLexEntry, string senseGloss, out ILexEntry lexEntry1_Entry, out ILexSense lexEntry1_Sense1)
		{
			SetupLexEntryAndSense(formLexEntry, senseGloss, "adjunct", out lexEntry1_Entry, out lexEntry1_Sense1);
		}

		/// <summary>
		/// </summary>
		/// <param name="formLexEntry"></param>
		/// <param name="senseGloss"></param>
		/// <param name="partOfSpeech"></param>
		/// <param name="lexEntry1_Entry"></param>
		/// <param name="lexEntry1_Sense1"></param>
		private void SetupLexEntryAndSense(string formLexEntry, string senseGloss, string partOfSpeech, out ILexEntry lexEntry1_Entry, out ILexSense lexEntry1_Sense1)
		{
			ITsString tssLexEntryForm = TsStringUtils.MakeTss(formLexEntry, Cache.DefaultVernWs);
			// create a sense with a matching gloss
			var entryComponents = MorphServices.BuildEntryComponents(Cache, tssLexEntryForm);
			int hvoSenseMsaPos = m_sandbox.GetComboItemHvo(InterlinLineChoices.kflidWordPos, 0, partOfSpeech);
			if (hvoSenseMsaPos != 0)
				entryComponents.MSA.MainPOS = Cache.ServiceLocator.GetInstance<IPartOfSpeechRepository>().GetObject(hvoSenseMsaPos);
			entryComponents.GlossAlternatives.Add(TsStringUtils.MakeTss(senseGloss, Cache.DefaultAnalWs));
			ILexEntry newEntry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(entryComponents);
			lexEntry1_Entry = newEntry;
			lexEntry1_Sense1 = newEntry.SensesOS[0];
		}

		[Test]
		[Ignore("Not sure what we're supposed to do with glossing on a polymorphemic guess. Need analyst input")]
		public void NewGlossForFocusBoxWithPolymorphemicGuess()
		{
			var cba0_0 = GetCba(0, 0, 0);
			m_sandbox.SwitchWord(cba0_0);
			// build polymorphemic guess
			ILexEntry lexEntry1_Entry;
			ILexSense lexEntry1_Sense1;
			ILexEntry lexEntry2_Entry;
			ILexSense lexEntry2_Sense1;
			SetupLexEntryAndSense("xx", "xx.existingsense1", out lexEntry1_Entry, out lexEntry1_Sense1);
			SetupLexEntryAndSense("xa", "xa.ExistingSense1", out lexEntry2_Entry, out lexEntry2_Sense1);
			// setup another morph bundle
			IWfiWordform wf;
			IWfiAnalysis analysis = SetupAnalysisForEntry(cba0_0, "0.0.xxxa", lexEntry1_Sense1, out wf);
			AppendMorphBundleToAnalysis(lexEntry1_Entry, lexEntry1_Sense1, analysis);
			AppendMorphBundleToAnalysis(lexEntry2_Entry, lexEntry2_Sense1, analysis);
			// load sandbox with a polymonomorphemic guess.
			m_sandbox.SwitchWord(cba0_0);
			Assert.IsTrue(m_sandbox.UsingGuess);

			// begin testing.
		}

		private IWfiMorphBundle SetupMorphBundleForEntry(AnalysisOccurrence cba0_0, string gloss, ILexEntry lexEntry1_Entry, ILexSense lexEntry1_Sense1, out IWfiWordform wf)
		{
			IWfiAnalysis analysis = SetupAnalysisForEntry(cba0_0, gloss, lexEntry1_Sense1, out wf);
			return AppendMorphBundleToAnalysis(lexEntry1_Entry, lexEntry1_Sense1, analysis);
		}

		private static IWfiMorphBundle AppendMorphBundleToAnalysis(ILexEntry lexEntry1_Entry, ILexSense lexEntry1_Sense1, IWfiAnalysis analysis)
		{
			var mbFactory = analysis.Cache.ServiceLocator.GetInstance<IWfiMorphBundleFactory>();
			IWfiMorphBundle morphBundle = mbFactory.Create();
			analysis.MorphBundlesOS.Add(morphBundle);
			morphBundle.MorphRA = lexEntry1_Entry.LexemeFormOA;
			morphBundle.SenseRA = lexEntry1_Sense1;
			morphBundle.MsaRA = lexEntry1_Sense1.MorphoSyntaxAnalysisRA;
			return morphBundle;
		}

		private IWfiAnalysis SetupAnalysisForEntry(AnalysisOccurrence cba0_0, string gloss, ILexSense lexEntry1_Sense1, out IWfiWordform wf)
		{
			wf = cba0_0.Analysis.Wordform;
			var wag = WordAnalysisOrGlossServices.CreateNewAnalysisTreeGloss(wf);
			wag.WfiAnalysis.CategoryRA = (lexEntry1_Sense1.MorphoSyntaxAnalysisRA as IMoStemMsa).PartOfSpeechRA;
			wag.Gloss.Form.set_String(Cache.DefaultAnalWs, gloss);
			return wag.WfiAnalysis;
		}


		[Test]
		public void PickLexGlossUsingExistingAnalysis()
		{
			var cba0_0 = GetCba(0, 0, 0);
			m_sandbox.SwitchWord(cba0_0);
			ILexEntry lexEntry1_Entry;
			ILexSense lexEntry1_Sense1;
			ILexEntry lexEntry2_Entry;
			ILexSense lexEntry2_Sense1;
			IWfiWordform wf;
			SetupLexEntryAndSense ("xxxa", "0.0.xxxa", out lexEntry1_Entry, out lexEntry1_Sense1);
			SetupLexEntryAndSense ("xxxa", "xxxa.AlternativeGloss", out lexEntry2_Entry, out lexEntry2_Sense1);
			// setup an existing analysis and gloss to match existing entry
			var morphBundle1 = SetupMorphBundleForEntry (cba0_0, "0.0.xxxa", lexEntry1_Entry, lexEntry1_Sense1, out wf);
			var morphBundle2 = SetupMorphBundleForEntry (cba0_0, "xxxa.AlternativeGloss", lexEntry2_Entry, lexEntry2_Sense1, out wf);
			// load sandbox with a guess.
			m_sandbox.SwitchWord(cba0_0);
#if WANTTESTPORT
				Assert.IsTrue(m_sandbox.UsingGuess);
#endif

			// mark the count of LexEntries
			int cEntriesOrig = Cache.LangProject.LexDbOA.Entries.Count();

			// first select 'unknown' to clear the guess for the word gloss/pos
			m_sandbox.SelectItemInCombo(InterlinLineChoices.kflidWordGloss, 0, "Unknown");
			// confirm Sandbox is in the expected state.
			ITsString tssWordGlossInSandbox = m_sandbox.GetTssInSandbox(InterlinLineChoices.kflidWordGloss,
																		Cache.DefaultAnalWs);
			Assert.AreEqual(null, tssWordGlossInSandbox.Text);
			int hvoPos = m_sandbox.GetRealHvoInSandbox(InterlinLineChoices.kflidWordPos, 0);
			Assert.AreEqual(0, hvoPos);

			// simulate selecting a lex gloss '0.0.xxxa'
			m_sandbox.SelectItemInCombo(InterlinLineChoices.kflidWordGloss, 0, lexEntry1_Sense1.Hvo);
			// confirm Sandbox is in the expected state.
			tssWordGlossInSandbox = m_sandbox.GetTssInSandbox(InterlinLineChoices.kflidWordGloss,
															  Cache.DefaultAnalWs);
			Assert.AreEqual("0.0.xxxa", tssWordGlossInSandbox.Text);
			int hvoPos2 = m_sandbox.GetRealHvoInSandbox(InterlinLineChoices.kflidWordPos, 0);
			Assert.AreNotEqual(0, hvoPos2);

			// simulate selecting the other lex gloss 'xxxa.AlternativeGloss'
			m_sandbox.SelectItemInCombo(InterlinLineChoices.kflidWordGloss, 0, lexEntry2_Sense1.Hvo);
			// confirm Sandbox is in the expected state.
			tssWordGlossInSandbox = m_sandbox.GetTssInSandbox(InterlinLineChoices.kflidWordGloss,
															  Cache.DefaultAnalWs);
			Assert.AreEqual("xxxa.AlternativeGloss", tssWordGlossInSandbox.Text);
			int hvoPos3 = m_sandbox.GetRealHvoInSandbox(InterlinLineChoices.kflidWordPos, 0);
			Assert.AreNotEqual(0, hvoPos3);

			// Next simulate picking an existing word gloss/pos by typing/selecting
			tssWordGlossInSandbox = m_sandbox.SetTssInSandbox(InterlinLineChoices.kflidWordGloss,
															  Cache.DefaultAnalWs, "0.0.xxxa");
			// set word pos, to first possibility (e.g. 'adjunct')
			int hvoSbWordPos = m_sandbox.SelectIndexInCombo(InterlinLineChoices.kflidWordPos, 0, 0);

			// confirm the analysis (using existing analysis and a LexSense)
			var wag = m_sandbox.ConfirmAnalysis();
			IWfiGloss wfiGloss = wag.Gloss;

			// make sure we didn't add entries or senses to the Lexicon.
			int cEntriesAfter = Cache.LangProject.LexDbOA.Entries.Count();
			Assert.AreEqual(cEntriesOrig, cEntriesAfter);
			Assert.AreEqual(1, lexEntry1_Entry.SensesOS.Count);

			// make sure the sense matches the existing one.
			ILexSense sense = m_sandbox.GetLexSenseForWord();
			Assert.AreEqual(lexEntry1_Sense1.Hvo, sense.Hvo);
			// make sure the strings of the wfi gloss matches the strings of the lex gloss.
			ValidateSenseWithAnalysis(sense, wfiGloss, hvoSbWordPos);

			// confirm we have not created a new analysis and that it is monomorphemic
			IWfiAnalysis wfiAnalysis = wag.WfiAnalysis;
			Assert.AreEqual(wf, wag.Wordform, "Expected confirmed analysis to be owned by the original wordform.");
			Assert.AreEqual(hvoSbWordPos, wfiAnalysis.CategoryRA.Hvo);
			Assert.AreEqual(2, wf.AnalysesOC.Count);
			Assert.AreEqual(1, wfiAnalysis.MorphBundlesOS.Count);
			Assert.AreEqual(1, wfiAnalysis.MeaningsOC.Count);
			IWfiAnalysis wfiAnalysis2 = (morphBundle2 as IWfiMorphBundle).Owner as IWfiAnalysis;
			Assert.AreEqual(1, wfiAnalysis2.MorphBundlesOS.Count);
			Assert.AreEqual(1, wfiAnalysis2.MeaningsOC.Count);

			// make sure the morph is linked to our lexicon sense, msa, and part of speech.
			IWfiMorphBundle wfiMorphBundle = wfiAnalysis.MorphBundlesOS[0];
			Assert.AreEqual(morphBundle1.Hvo, wfiMorphBundle.Hvo);
		}
	}
}
