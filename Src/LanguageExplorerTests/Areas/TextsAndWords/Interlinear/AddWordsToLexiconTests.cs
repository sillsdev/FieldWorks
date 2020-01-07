// Copyright (c) 2009-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Linq;
using LanguageExplorer.Areas.TextsAndWords.Interlinear;
using LanguageExplorer.TestUtilities;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorerTests.Areas.TextsAndWords.Interlinear
{
	/// <summary />
	[TestFixture]
	public class AddWordsToLexiconTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private IText m_text1;
		private SandboxForTests m_sandbox;
		private FlexComponentParameters _flexComponentParameters;
		private IPropertyTable _propertyTable;

		/// <summary />
		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, DoSetupFixture);
		}

		/// <summary>
		/// non-undoable task
		/// </summary>
		private void DoSetupFixture()
		{
			var textFactory = Cache.ServiceLocator.GetInstance<ITextFactory>();
			var stTextFactory = Cache.ServiceLocator.GetInstance<IStTextFactory>();
			m_text1 = textFactory.Create();
			var stText1 = stTextFactory.Create();
			m_text1.ContentsOA = stText1;
			var para1 = stText1.AddNewTextPara(null);
			(m_text1.ContentsOA[0]).Contents = TsStringUtils.MakeString("xxxa xxxb xxxc xxxd xxxe, xxxa xxxb.", Cache.DefaultVernWs);
			stText1.LoadParagraphAnnotationsAndGenerateEntryGuessesIfNeeded(false);
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
			try
			{
				// Dispose managed resources here.
				m_sandbox?.Dispose();
				TestSetupServices.DisposeTrash(_flexComponentParameters);
				m_sandbox = null;
				_propertyTable = null;
				_flexComponentParameters = null;
			}
			catch (Exception err)
			{
				throw new Exception($"Error in running {GetType().Name} TestTearDown method.", err);
			}
			finally
			{
				base.TestTearDown();
			}
		}

		[SetUp]
		public override void TestSetup()
		{
			base.TestSetup();
			var lineChoices = InterlinLineChoices.DefaultChoices(Cache.LangProject, Cache.DefaultVernWs, Cache.DefaultAnalWs, InterlinMode.GlossAddWordsToLexicon);
			_flexComponentParameters = TestSetupServices.SetupEverything(Cache, false);
			_propertyTable = _flexComponentParameters.PropertyTable;
			m_sandbox = new SandboxForTests(Cache, lineChoices);
			m_sandbox.InitializeFlexComponent(_flexComponentParameters);
		}

		internal static void CompareTss(ITsString tssExpected, ITsString tssActual)
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

		internal static AnalysisOccurrence GetNewAnalysisOccurence(IText text, int iPara, int iSeg, int iSegForm)
		{
			var para = text.ContentsOA.ParagraphsOS[iPara] as IStTxtPara;
			var seg = para.SegmentsOS[iSeg];
			return new AnalysisOccurrence(seg, iSegForm);
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
			var cba0_0 = GetNewAnalysisOccurence(m_text1, 0, 0, 0);
			m_sandbox.SwitchWord(cba0_0);

			// verify that the word gloss is empty
			var tssEmpty = TsStringUtils.MakeString("", Cache.DefaultAnalWs);
			var tssWordGloss = m_sandbox.GetTssInSandbox(InterlinLineChoices.kflidWordGloss, Cache.DefaultAnalWs);
			CompareTss(tssEmpty, tssWordGloss);
			// add a new word gloss and confirm the analysis.
			var tssWordGlossInSandbox = m_sandbox.SetTssInSandbox(InterlinLineChoices.kflidWordGloss, Cache.DefaultAnalWs, "0.0.xxxa");
			// mark the count of LexEntries
			var cEntriesOrig = Cache.LangProject.LexDbOA.Entries.Count();
			// verify no analyses exist for this wordform;
			var wf = cba0_0.Analysis.Wordform;
			Assert.AreEqual(0, wf.AnalysesOC.Count);

			// set word pos, to first possibility (e.g. 'adjunct')
			var hvoSbWordPos = m_sandbox.SelectIndexInCombo(_propertyTable, InterlinLineChoices.kflidWordPos, 0, 0);
			Assert.IsFalse(hvoSbWordPos == 0); // select nonzero pos

			// confirm the analysis (making a real analysis and a LexSense)
			var wag = m_sandbox.ConfirmAnalysis();
			var glossFactory = Cache.ServiceLocator.GetInstance<IWfiGlossFactory>();

			var wfiGloss = wag.Gloss;
			CompareTss(tssWordGlossInSandbox, wfiGloss.Form.get_String(Cache.DefaultAnalWs));
			// confirm we have only one analysis and that it is monomorphemic
			var wfiAnalysis = wag.WfiAnalysis;
			Assert.AreEqual(wf, wag.Wordform, "Expected confirmed analysis to be owned by the original wordform.");
			Assert.AreEqual(1, wf.AnalysesOC.Count);
			Assert.AreEqual(1, wfiAnalysis.MorphBundlesOS.Count);
			Assert.AreEqual(1, wfiAnalysis.MeaningsOC.Count);

			// make sure the strings of the wfi gloss matches the strings of the lex gloss.
			ValidateSenseWithAnalysis(m_sandbox.GetLexSenseForWord(), wfiGloss, hvoSbWordPos);

			// make sure a new entry is in the Lexicon.
			var cEntriesAfter = Cache.LangProject.LexDbOA.Entries.Count();
			Assert.AreEqual(cEntriesOrig + 1, cEntriesAfter);
		}


		private void ValidateSenseWithAnalysis(ILexSense sense, IWfiGloss wfiGloss, int hvoSbWordPos)
		{
			ValidateSenseWithAnalysis(sense, wfiGloss, hvoSbWordPos, false, sense.Entry.LexemeFormOA as IMoStemAllomorph);
		}

		private void ValidateSenseWithAnalysis(ILexSense sense, IWfiGloss wfiGloss, int hvoSbWordPos, bool fMatchMainPossibility, IMoStemAllomorph allomorph)
		{
			var wfiAnalysis = wfiGloss.Owner as IWfiAnalysis;
			CompareTss(sense.Gloss.get_String(Cache.DefaultAnalWs), wfiGloss.Form.get_String(Cache.DefaultAnalWs));
			// make sure the morph is linked to the lexicon sense, msa, and part of speech.
			var morphBundle = wfiAnalysis.MorphBundlesOS[0];
			Assert.AreEqual(sense, morphBundle.SenseRA);
			Assert.AreEqual(sense.MorphoSyntaxAnalysisRA, morphBundle.MsaRA);
			if (!fMatchMainPossibility)
			{
				// expect exact possibility
				Assert.AreEqual(hvoSbWordPos, (sense.MorphoSyntaxAnalysisRA as IMoStemMsa).PartOfSpeechRA.Hvo);
			}
			else
			{
				var posTarget = Cache.ServiceLocator.GetInstance<IPartOfSpeechRepository>().GetObject(hvoSbWordPos);
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
			var cba0_0 = GetNewAnalysisOccurence(m_text1, 0, 0, 0);
			m_sandbox.SwitchWord(cba0_0);
			ILexEntry lexEntry1_Entry;
			ILexSense lexEntry1_Sense1;
			SetupLexEntryAndSense("xxxa", "xxxa.existingsense1", out lexEntry1_Entry, out lexEntry1_Sense1);

			// mark the count of LexEntries
			var cEntriesOrig = Cache.LangProject.LexDbOA.Entries.Count();

			// add a new word gloss
			m_sandbox.SetTssInSandbox(InterlinLineChoices.kflidWordGloss, Cache.DefaultAnalWs, "0.0.xxxa");
			var wf = cba0_0.Analysis.Wordform;
			// set word pos, to first possibility (e.g. 'adjunct')
			var hvoSbWordPos = m_sandbox.SelectIndexInCombo(_propertyTable, InterlinLineChoices.kflidWordPos, 0, 0);

			// confirm the analysis (making a real analysis and a LexSense)
			var wag = m_sandbox.ConfirmAnalysis();
			var wfiGloss = wag.Gloss;

			// make sure we didn't add entries to the Lexicon.
			var cEntriesAfter = Cache.LangProject.LexDbOA.Entries.Count();
			Assert.AreEqual(cEntriesOrig, cEntriesAfter);

			// confirm we have only one analysis and that it is monomorphemic
			var wfiAnalysis = wag.WfiAnalysis;
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
			var cba0_0 = GetNewAnalysisOccurence(m_text1, 0, 0, 0);
			m_sandbox.SwitchWord(cba0_0);
			const string formLexEntry = "xxxab";
			var tssLexEntryForm = TsStringUtils.MakeString(formLexEntry, Cache.DefaultVernWs);
			const string formAllomorph = "xxxa";
			var tssAllomorphForm = TsStringUtils.MakeString(formAllomorph, Cache.DefaultVernWs);

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
			var allomorph = stemFactory.Create();
			lexEntry1_Entry.AlternateFormsOS.Add(allomorph);
			allomorph.MorphTypeRA = lexEntry1_Entry.LexemeFormOA.MorphTypeRA;
			allomorph.Form.set_String(TsStringUtils.GetWsAtOffset(tssAllomorphForm, 0), tssAllomorphForm);
			// mark the count of LexEntries
			var cEntriesOrig = Cache.LangProject.LexDbOA.Entries.Count();

			// add a new word gloss
			var tssWordGlossInSandbox = m_sandbox.SetTssInSandbox(InterlinLineChoices.kflidWordGloss, Cache.DefaultAnalWs, "0.0.xxxa");
			var wf = cba0_0.Analysis.Wordform;
			// set word pos to verb
			var hvoSbWordPos = m_sandbox.GetComboItemHvo(_propertyTable, InterlinLineChoices.kflidWordPos, 0, "transitive verb");
			m_sandbox.SelectItemInCombo(_propertyTable, InterlinLineChoices.kflidWordPos, 0, hvoSbWordPos);

			// confirm the analysis (making a real analysis and a LexSense)
			var wag = m_sandbox.ConfirmAnalysis();
			var wfiGloss = wag.Gloss;

			// make sure we didn't add entries to the Lexicon.
			var cEntriesAfter = Cache.LangProject.LexDbOA.Entries.Count();
			Assert.AreEqual(cEntriesOrig, cEntriesAfter);

			// confirm we have only one analysis and that it is monomorphemic
			var wfiAnalysis = wag.WfiAnalysis;
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
			var cba0_0 = GetNewAnalysisOccurence(m_text1, 0, 0, 0);
			m_sandbox.SwitchWord(cba0_0);
			ILexEntry lexEntry1_Entry;
			ILexSense lexEntry1_Sense1;
			SetupLexEntryAndSense("xxxa", "0.0.xxxa", out lexEntry1_Entry, out lexEntry1_Sense1);

			// mark the count of LexEntries
			var cEntriesOrig = Cache.LangProject.LexDbOA.Entries.Count();

			// add a new word gloss
			m_sandbox.SetTssInSandbox(InterlinLineChoices.kflidWordGloss, Cache.DefaultAnalWs, "0.0.xxxa");
			var wf = cba0_0.Analysis.Wordform;
			// set word pos, to first possibility (e.g. 'adjunct')
			var hvoSbWordPos = m_sandbox.SelectIndexInCombo(_propertyTable, InterlinLineChoices.kflidWordPos, 0, 0);

			// confirm the analysis (making a real analysis and a LexSense)
			var wag = m_sandbox.ConfirmAnalysis();
			var wfiGloss = wag.Gloss;

			// make sure we didn't add entries or senses to the Lexicon.
			var cEntriesAfter = Cache.LangProject.LexDbOA.Entries.Count();
			Assert.AreEqual(cEntriesOrig, cEntriesAfter);
			Assert.AreEqual(1, lexEntry1_Entry.SensesOS.Count);

			// make sure the sense matches the existing one.
			var sense = m_sandbox.GetLexSenseForWord();
			Assert.AreEqual(lexEntry1_Sense1.Hvo, sense.Hvo);
			// make sure the morph is linked to our lexicon sense, msa, and part of speech.
			ValidateSenseWithAnalysis(sense, wfiGloss, hvoSbWordPos);

			// confirm we have created a new analysis and that it is monomorphemic
			var wfiAnalysis = wag.WfiAnalysis;
			Assert.AreEqual(wf, wag.Wordform, "Expected confirmed analysis to be owned by the original wordform.");
			Assert.AreEqual(1, wf.AnalysesOC.Count);
			Assert.AreEqual(1, wfiAnalysis.MorphBundlesOS.Count);
			Assert.AreEqual(1, wfiAnalysis.MeaningsOC.Count);
		}

		private void SetupLexEntryAndSense(string formLexEntry, string senseGloss, out ILexEntry lexEntry, out ILexSense lexSense)
		{
			SetupLexEntryAndSense(formLexEntry, senseGloss, Cache, m_sandbox, out lexEntry, out lexSense);
		}

		/// <summary />
		internal static void SetupLexEntryAndSense(string formLexEntry, string senseGloss, LcmCache cache, SandboxForTests testSandBox, out ILexEntry lexEntry, out ILexSense lexSense)
		{
			SetupLexEntryAndSense(formLexEntry, senseGloss, "adjunct", cache, testSandBox, out lexEntry, out lexSense);
		}

		private void SetupLexEntryAndSense(string formLexEntry, string senseGloss, string partOfSpeech, out ILexEntry lexEntry, out ILexSense lexSense)
		{
			SetupLexEntryAndSense(formLexEntry, senseGloss, partOfSpeech, Cache, m_sandbox, out lexEntry, out lexSense);
		}

		/// <summary />
		internal static void SetupLexEntryAndSense(string formLexEntry, string senseGloss, string partOfSpeech, LcmCache cache, SandboxForTests testSandBox, out ILexEntry lexEntry, out ILexSense lexSense)
		{
			var tssLexEntryForm = TsStringUtils.MakeString(formLexEntry, cache.DefaultVernWs);
			// create a sense with a matching gloss
			var entryComponents = MorphServices.BuildEntryComponents(cache, tssLexEntryForm);
			var hvoSenseMsaPos = testSandBox.GetComboItemHvo(null, InterlinLineChoices.kflidWordPos, 0, partOfSpeech);
			if (hvoSenseMsaPos != 0)
			{
				entryComponents.MSA.MainPOS = cache.ServiceLocator.GetInstance<IPartOfSpeechRepository>().GetObject(hvoSenseMsaPos);
			}
			entryComponents.GlossAlternatives.Add(TsStringUtils.MakeString(senseGloss, cache.DefaultAnalWs));
			var newEntry = cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(entryComponents);
			lexEntry = newEntry;
			lexSense = newEntry.SensesOS[0];
		}

		private IWfiMorphBundle SetupMorphBundleForEntry(AnalysisOccurrence cba0_0, string gloss, ILexEntry lexEntry1_Entry, ILexSense lexEntry1_Sense1, out IWfiWordform wf)
		{
			return AppendMorphBundleToAnalysis(lexEntry1_Entry, lexEntry1_Sense1, SetupAnalysisForEntry(cba0_0, gloss, lexEntry1_Sense1, out wf));
		}

		private static IWfiMorphBundle AppendMorphBundleToAnalysis(ILexEntry lexEntry1_Entry, ILexSense lexEntry1_Sense1, IWfiAnalysis analysis)
		{
			var mbFactory = analysis.Cache.ServiceLocator.GetInstance<IWfiMorphBundleFactory>();
			var morphBundle = mbFactory.Create();
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
			var cba0_0 = GetNewAnalysisOccurence(m_text1, 0, 0, 0);
			m_sandbox.SwitchWord(cba0_0);
			ILexEntry lexEntry1_Entry;
			ILexSense lexEntry1_Sense1;
			ILexEntry lexEntry2_Entry;
			ILexSense lexEntry2_Sense1;
			IWfiWordform wf;
			SetupLexEntryAndSense("xxxa", "0.0.xxxa", out lexEntry1_Entry, out lexEntry1_Sense1);
			SetupLexEntryAndSense("xxxa", "xxxa.AlternativeGloss", out lexEntry2_Entry, out lexEntry2_Sense1);
			// setup an existing analysis and gloss to match existing entry
			var morphBundle1 = SetupMorphBundleForEntry(cba0_0, "0.0.xxxa", lexEntry1_Entry, lexEntry1_Sense1, out wf);
			var morphBundle2 = SetupMorphBundleForEntry(cba0_0, "xxxa.AlternativeGloss", lexEntry2_Entry, lexEntry2_Sense1, out wf);
			// load sandbox with a guess.
			m_sandbox.SwitchWord(cba0_0);

			// mark the count of LexEntries
			var cEntriesOrig = Cache.LangProject.LexDbOA.Entries.Count();

			// first select 'unknown' to clear the guess for the word gloss/pos
			m_sandbox.SelectItemInCombo(_propertyTable, InterlinLineChoices.kflidWordGloss, 0, "Unknown");
			// confirm Sandbox is in the expected state.
			var tssWordGlossInSandbox = m_sandbox.GetTssInSandbox(InterlinLineChoices.kflidWordGloss, Cache.DefaultAnalWs);
			Assert.AreEqual(null, tssWordGlossInSandbox.Text);
			var hvoPos = m_sandbox.GetRealHvoInSandbox(InterlinLineChoices.kflidWordPos, 0);
			Assert.AreEqual(0, hvoPos);

			// simulate selecting a lex gloss '0.0.xxxa'
			m_sandbox.SelectItemInCombo(_propertyTable, InterlinLineChoices.kflidWordGloss, 0, lexEntry1_Sense1.Hvo);
			// confirm Sandbox is in the expected state.
			tssWordGlossInSandbox = m_sandbox.GetTssInSandbox(InterlinLineChoices.kflidWordGloss, Cache.DefaultAnalWs);
			Assert.AreEqual("0.0.xxxa", tssWordGlossInSandbox.Text);
			var hvoPos2 = m_sandbox.GetRealHvoInSandbox(InterlinLineChoices.kflidWordPos, 0);
			Assert.AreNotEqual(0, hvoPos2);

			// simulate selecting the other lex gloss 'xxxa.AlternativeGloss'
			m_sandbox.SelectItemInCombo(_propertyTable, InterlinLineChoices.kflidWordGloss, 0, lexEntry2_Sense1.Hvo);
			// confirm Sandbox is in the expected state.
			tssWordGlossInSandbox = m_sandbox.GetTssInSandbox(InterlinLineChoices.kflidWordGloss, Cache.DefaultAnalWs);
			Assert.AreEqual("xxxa.AlternativeGloss", tssWordGlossInSandbox.Text);
			var hvoPos3 = m_sandbox.GetRealHvoInSandbox(InterlinLineChoices.kflidWordPos, 0);
			Assert.AreNotEqual(0, hvoPos3);

			// Next simulate picking an existing word gloss/pos by typing/selecting
			tssWordGlossInSandbox = m_sandbox.SetTssInSandbox(InterlinLineChoices.kflidWordGloss, Cache.DefaultAnalWs, "0.0.xxxa");
			// set word pos, to first possibility (e.g. 'adjunct')
			var hvoSbWordPos = m_sandbox.SelectIndexInCombo(_propertyTable, InterlinLineChoices.kflidWordPos, 0, 0);

			// confirm the analysis (using existing analysis and a LexSense)
			var wag = m_sandbox.ConfirmAnalysis();
			var wfiGloss = wag.Gloss;

			// make sure we didn't add entries or senses to the Lexicon.
			var cEntriesAfter = Cache.LangProject.LexDbOA.Entries.Count();
			Assert.AreEqual(cEntriesOrig, cEntriesAfter);
			Assert.AreEqual(1, lexEntry1_Entry.SensesOS.Count);

			// make sure the sense matches the existing one.
			var sense = m_sandbox.GetLexSenseForWord();
			Assert.AreEqual(lexEntry1_Sense1.Hvo, sense.Hvo);
			// make sure the strings of the wfi gloss matches the strings of the lex gloss.
			ValidateSenseWithAnalysis(sense, wfiGloss, hvoSbWordPos);

			// confirm we have not created a new analysis and that it is monomorphemic
			var wfiAnalysis = wag.WfiAnalysis;
			Assert.AreEqual(wf, wag.Wordform, "Expected confirmed analysis to be owned by the original wordform.");
			Assert.AreEqual(hvoSbWordPos, wfiAnalysis.CategoryRA.Hvo);
			Assert.AreEqual(2, wf.AnalysesOC.Count);
			Assert.AreEqual(1, wfiAnalysis.MorphBundlesOS.Count);
			Assert.AreEqual(1, wfiAnalysis.MeaningsOC.Count);
			var wfiAnalysis2 = morphBundle2.Owner as IWfiAnalysis;
			Assert.AreEqual(1, wfiAnalysis2.MorphBundlesOS.Count);
			Assert.AreEqual(1, wfiAnalysis2.MeaningsOC.Count);

			// make sure the morph is linked to our lexicon sense, msa, and part of speech.
			var wfiMorphBundle = wfiAnalysis.MorphBundlesOS[0];
			Assert.AreEqual(morphBundle1.Hvo, wfiMorphBundle.Hvo);
		}
	}
}