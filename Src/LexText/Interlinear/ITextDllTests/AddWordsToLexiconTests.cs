using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.XPath;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.FDOTests;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.XWorks;
using SIL.Utils;

namespace SIL.FieldWorks.IText
{

	/// <summary>
	/// </summary>
	[TestFixture]
	public class AddWordsToLexiconTests: InterlinearTestBase
	{
		FDO.IText m_text1 = null;
		SandboxForTests m_sandbox;

		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			CheckDisposed();
			base.FixtureSetup();

			InstallVirtuals(@"Language Explorer\Configuration\Main.xml",
				new string[] { "SIL.FieldWorks.FDO.", "SIL.FieldWorks.IText." });
		}

		/// ------------------------------------------------------------------------------------
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
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_sandbox != null)
					m_sandbox.Dispose();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_text1 = null;

			base.Dispose(disposing);
		}

		[SetUp]
		public override void Initialize()
		{
			CheckDisposed();
			base.Initialize();
			m_text1 = Cache.LangProject.TextsOC.Add(new Text());
			m_text1.ContentsOA = new StText();
			m_text1.ContentsOA.ParagraphsOS.Append(new StTxtPara());
			(m_text1.ContentsOA.ParagraphsOS[0] as StTxtPara).Contents.UnderlyingTsString =
				StringUtils.MakeTss("xxxa xxxb xxxc xxxd xxxe, xxxa xxxb.", Cache.DefaultVernWs);
			bool fDidParse;
			ParagraphParser.ParseText(m_text1.ContentsOA, new NullProgressState(), out fDidParse);
			InterlinLineChoices lineChoices = InterlinLineChoices.DefaultChoices(0, Cache.DefaultAnalWs, Cache.LangProject,
				InterlinLineChoices.InterlinMode.GlossAddWordsToLexicon);
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

			ISilDataAccess MainCacheDa
			{
				get { return m_caches.MainCache.MainCacheAccessor; }
			}

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
				ITsString tss = StringUtils.MakeTss(str, ws);
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

			internal int ConfirmAnalysis()
			{
				return GetRealAnalysis(true);
			}

			internal ILexSense GetLexSenseForWord()
			{
				List<int> hvoSenses = LexSensesForCurrentMorphs();
				// the sense only represents the whole word if there is only one morph.
				if (hvoSenses.Count != 1)
					return null;
				return new LexSense(Cache, hvoSenses[0]);
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
				List<int> currentMorphs = CurrentMorphs();
				return InterlinComboHandler.MakeCombo(tagIcon, this, currentMorphs[morphIndex]) as InterlinComboHandler;
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

		[TearDown]
		public override void Exit()
		{
			CheckDisposed();

			// UndoEverything before we clear our wordform table, so we can make sure
			// the real wordform list is what we want to start with the next time.
			base.Exit();

			// clear the wordform table.
			m_text1.Cache.LangProject.WordformInventoryOA.ResetAllWordformOccurrences();
			m_text1 = null;
			// Dispose the sandbox
			m_sandbox.Dispose();
			m_sandbox = null;
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

		private int GetCbaHvo(int iPara, int iSeg, int iSegForm)
		{
			StTxtPara para = m_text1.ContentsOA.ParagraphsOS[iPara] as StTxtPara;
			return para.SegmentForm(iSeg, iSegForm);
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
			int hvoCba0_0 = GetCbaHvo(0, 0, 0);
			m_sandbox.SwitchWord(hvoCba0_0, false);

			// verify that the word gloss is empty
			ITsString tssEmpty = StringUtils.MakeTss("", Cache.DefaultAnalWs);
			ITsString tssWordGloss = m_sandbox.GetTssInSandbox(InterlinLineChoices.kflidWordGloss, Cache.DefaultAnalWs);
			CompareTss(tssEmpty, tssWordGloss);
			// add a new word gloss and confirm the analysis.
			ITsString tssWordGlossInSandbox = m_sandbox.SetTssInSandbox(InterlinLineChoices.kflidWordGloss, Cache.DefaultAnalWs, "0.0.xxxa");
			// mark the count of LexEntries
			int cEntriesOrig = Cache.LangProject.LexDbOA.EntriesOC.Count;
			// verify no analyses exist for this wordform;
			int hvoWf = WfiWordform.GetWfiWordformFromInstanceOf(Cache, hvoCba0_0);
			WfiWordform wf = new WfiWordform(Cache, hvoWf);
			Assert.AreEqual(0, wf.AnalysesOC.Count);

			// set word pos, to first possibility (e.g. 'adjunct')
			int hvoSbWordPos = m_sandbox.SelectIndexInCombo(InterlinLineChoices.kflidWordPos, 0, 0);
			Assert.IsFalse(hvoSbWordPos == 0);	 // select nonzero pos

			// confirm the analysis (making a real analysis and a LexSense)
			int hvoGloss = m_sandbox.ConfirmAnalysis();

			WfiGloss wfiGloss = new WfiGloss(Cache, hvoGloss);
			CompareTss(tssWordGlossInSandbox, wfiGloss.Form.GetAlternativeTss(Cache.DefaultAnalWs));
			// confirm we have only one analysis and that it is monomorphemic
			WfiAnalysis wfiAnalysis = wfiGloss.Owner as WfiAnalysis;
			Assert.AreEqual(wf.Hvo, wfiAnalysis.OwnerHVO, "Expected confirmed analysis to be owned by the original wordform.");
			Assert.AreEqual(1, wf.AnalysesOC.Count);
			Assert.AreEqual(1, wfiAnalysis.MorphBundlesOS.Count);
			Assert.AreEqual(1, wfiAnalysis.MeaningsOC.Count);

			// make sure the strings of the wfi gloss matches the strings of the lex gloss.
			ValidateSenseWithAnalysis(m_sandbox.GetLexSenseForWord(), wfiGloss, hvoSbWordPos);

			// make sure a new entry is in the Lexicon.
			int cEntriesAfter = Cache.LangProject.LexDbOA.EntriesOC.Count;
			Assert.AreEqual(cEntriesOrig + 1, cEntriesAfter);
		}


		private void ValidateSenseWithAnalysis(ILexSense sense, WfiGloss wfiGloss, int hvoSbWordPos)
		{
			ValidateSenseWithAnalysis(sense, wfiGloss, hvoSbWordPos, false, sense.Entry.LexemeFormOA as IMoStemAllomorph);
		}

		private void ValidateSenseWithAnalysis(ILexSense sense, WfiGloss wfiGloss, int hvoSbWordPos, bool fMatchMainPossibility, IMoStemAllomorph allomorph)
		{
			WfiAnalysis wfiAnalysis = wfiGloss.Owner as WfiAnalysis;
			CompareTss(sense.Gloss.GetAlternativeTss(Cache.DefaultAnalWs),
				wfiGloss.Form.GetAlternativeTss(Cache.DefaultAnalWs));

			// make sure the morph is linked to the lexicon sense, msa, and part of speech.
			IWfiMorphBundle morphBundle = wfiAnalysis.MorphBundlesOS[0];
			Assert.AreEqual(sense.Hvo, morphBundle.SenseRAHvo);
			Assert.AreEqual(sense.MorphoSyntaxAnalysisRAHvo, morphBundle.MsaRAHvo);
			if (!fMatchMainPossibility)
			{
				// expect exact possibility
				Assert.AreEqual(hvoSbWordPos, (sense.MorphoSyntaxAnalysisRA as MoStemMsa).PartOfSpeechRAHvo);
			}
			else
			{
				IPartOfSpeech posTarget = PartOfSpeech.CreateFromDBObject(Cache, hvoSbWordPos);
				Assert.AreEqual(posTarget.MainPossibility.Hvo, (sense.MorphoSyntaxAnalysisRA as MoStemMsa).PartOfSpeechRA.MainPossibility.Hvo);
			}
			Assert.AreEqual(allomorph.Hvo, morphBundle.MorphRAHvo);
			Assert.AreEqual(hvoSbWordPos, wfiAnalysis.CategoryRAHvo);
		}

		/// <summary>
		/// A new word gloss for an existing lex entry will
		/// add another sense to the existing lexicon word.
		/// </summary>
		[Test]
		public void NewGlossExistingLexEntryNewLexSense()
		{
			int hvoCba0_0 = GetCbaHvo(0, 0, 0);
			m_sandbox.SwitchWord(hvoCba0_0, false);
			string formLexEntry = "xxxa";
			ITsString tssLexEntryForm = StringUtils.MakeTss(formLexEntry, Cache.DefaultVernWs);
			int clsidForm;
			ILexEntry lexEntry1_Entry = LexEntry.CreateEntry(Cache,
				MoMorphType.FindMorphType(Cache, new MoMorphTypeCollection(Cache), ref formLexEntry, out clsidForm), tssLexEntryForm,
				"xxxa.existingsense1", null);
			ILexSense lexEntry1_Sense1 = lexEntry1_Entry.SensesOS[0];

			// mark the count of LexEntries
			int cEntriesOrig = Cache.LangProject.LexDbOA.EntriesOC.Count;

			// add a new word gloss
			ITsString tssWordGlossInSandbox = m_sandbox.SetTssInSandbox(InterlinLineChoices.kflidWordGloss, Cache.DefaultAnalWs, "0.0.xxxa");
			int hvoWf = WfiWordform.GetWfiWordformFromInstanceOf(Cache, hvoCba0_0);
			WfiWordform wf = new WfiWordform(Cache, hvoWf);
			// set word pos, to first possibility (e.g. 'adjunct')
			int hvoSbWordPos = m_sandbox.SelectIndexInCombo(InterlinLineChoices.kflidWordPos, 0, 0);

			// confirm the analysis (making a real analysis and a LexSense)
			int hvoGloss = m_sandbox.ConfirmAnalysis();
			WfiGloss wfiGloss = new WfiGloss(Cache, hvoGloss);

			// make sure we didn't add entries to the Lexicon.
			int cEntriesAfter = Cache.LangProject.LexDbOA.EntriesOC.Count;
			Assert.AreEqual(cEntriesOrig, cEntriesAfter);

			// confirm we have only one analysis and that it is monomorphemic
			WfiAnalysis wfiAnalysis = wfiGloss.Owner as WfiAnalysis;
			Assert.AreEqual(wf.Hvo, wfiAnalysis.OwnerHVO, "Expected confirmed analysis to be owned by the original wordform.");
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
			int hvoCba0_0 = GetCbaHvo(0, 0, 0);
			m_sandbox.SwitchWord(hvoCba0_0, false);
			string formLexEntry = "xxxab";
			ITsString tssLexEntryForm = StringUtils.MakeTss(formLexEntry, Cache.DefaultVernWs);
			string formAllomorph = "xxxa";
			ITsString tssAllomorphForm = StringUtils.MakeTss(formAllomorph, Cache.DefaultVernWs);

			// first create an entry with a matching allomorph that doesn't match 'verb' POS we will be selecting in the sandbox
			ILexEntry lexEntry_NounPos;
			ILexSense lexSense_NounPos;
			SetupLexEntryAndSense("xxxab", "0.0.xxxab_NounPos", "noun", out lexEntry_NounPos, out lexSense_NounPos);
			IMoStemAllomorph allomorph0 = lexEntry_NounPos.AlternateFormsOS.Append(new MoStemAllomorph()) as IMoStemAllomorph;
			allomorph0.Form.SetAlternativeTss(tssAllomorphForm);

			// now create the entry we want to match, that has a 'verb' POS.
			ILexEntry lexEntry1_Entry;
			ILexSense lexEntry1_Sense1;
			SetupLexEntryAndSense("xxxab", "0.0.xxxab_VerbPos", "verb", out lexEntry1_Entry, out lexEntry1_Sense1);
			IMoStemAllomorph allomorph = lexEntry1_Entry.AlternateFormsOS.Append(new MoStemAllomorph()) as IMoStemAllomorph;
			allomorph.Form.SetAlternativeTss(tssAllomorphForm);

			// mark the count of LexEntries
			int cEntriesOrig = Cache.LangProject.LexDbOA.EntriesOC.Count;

			// add a new word gloss
			ITsString tssWordGlossInSandbox = m_sandbox.SetTssInSandbox(InterlinLineChoices.kflidWordGloss, Cache.DefaultAnalWs, "0.0.xxxa");
			int hvoWf = WfiWordform.GetWfiWordformFromInstanceOf(Cache, hvoCba0_0);
			WfiWordform wf = new WfiWordform(Cache, hvoWf);
			// set word pos to verb
			int hvoSbWordPos = m_sandbox.GetComboItemHvo(InterlinLineChoices.kflidWordPos, 0, "transitive verb");
			m_sandbox.SelectItemInCombo(InterlinLineChoices.kflidWordPos, 0, hvoSbWordPos);

			// confirm the analysis (making a real analysis and a LexSense)
			int hvoGloss = m_sandbox.ConfirmAnalysis();
			WfiGloss wfiGloss = new WfiGloss(Cache, hvoGloss);

			// make sure we didn't add entries to the Lexicon.
			int cEntriesAfter = Cache.LangProject.LexDbOA.EntriesOC.Count;
			Assert.AreEqual(cEntriesOrig, cEntriesAfter);

			// confirm we have only one analysis and that it is monomorphemic
			WfiAnalysis wfiAnalysis = wfiGloss.Owner as WfiAnalysis;
			Assert.AreEqual(wf.Hvo, wfiAnalysis.OwnerHVO, "Expected confirmed analysis to be owned by the original wordform.");
			Assert.AreEqual(1, wf.AnalysesOC.Count);
			Assert.AreEqual(1, wfiAnalysis.MorphBundlesOS.Count);
			Assert.AreEqual(1, wfiAnalysis.MeaningsOC.Count);

			// make sure the strings of the wfi gloss matches the strings of the lex gloss.
			ValidateSenseWithAnalysis(m_sandbox.GetLexSenseForWord(), wfiGloss, hvoSbWordPos, true, allomorph);
		}

		[Test]
		public void PickLexGlossCreatingNewAnalysis()
		{
			int hvoCba0_0 = GetCbaHvo(0, 0, 0);
			m_sandbox.SwitchWord(hvoCba0_0, false);
			ILexEntry lexEntry1_Entry;
			ILexSense lexEntry1_Sense1;
			SetupLexEntryAndSense("xxxa", "0.0.xxxa", out lexEntry1_Entry, out lexEntry1_Sense1);

			// mark the count of LexEntries
			int cEntriesOrig = Cache.LangProject.LexDbOA.EntriesOC.Count;

			// add a new word gloss
			ITsString tssWordGlossInSandbox = m_sandbox.SetTssInSandbox(InterlinLineChoices.kflidWordGloss, Cache.DefaultAnalWs, "0.0.xxxa");
			int hvoWf = WfiWordform.GetWfiWordformFromInstanceOf(Cache, hvoCba0_0);
			WfiWordform wf = new WfiWordform(Cache, hvoWf);
			// set word pos, to first possibility (e.g. 'adjunct')
			int hvoSbWordPos = m_sandbox.SelectIndexInCombo(InterlinLineChoices.kflidWordPos, 0, 0);

			// confirm the analysis (making a real analysis and a LexSense)
			int hvoGloss = m_sandbox.ConfirmAnalysis();
			WfiGloss wfiGloss = new WfiGloss(Cache, hvoGloss);

			// make sure we didn't add entries or senses to the Lexicon.
			int cEntriesAfter = Cache.LangProject.LexDbOA.EntriesOC.Count;
			Assert.AreEqual(cEntriesOrig, cEntriesAfter);
			Assert.AreEqual(1, lexEntry1_Entry.SensesOS.Count);

			// make sure the sense matches the existing one.
			ILexSense sense = m_sandbox.GetLexSenseForWord();
			Assert.AreEqual(lexEntry1_Sense1.Hvo, sense.Hvo);
			// make sure the morph is linked to our lexicon sense, msa, and part of speech.
			ValidateSenseWithAnalysis(sense, wfiGloss, hvoSbWordPos);

			// confirm we have created a new analysis and that it is monomorphemic
			WfiAnalysis wfiAnalysis = wfiGloss.Owner as WfiAnalysis;
			Assert.AreEqual(wf.Hvo, wfiAnalysis.OwnerHVO, "Expected confirmed analysis to be owned by the original wordform.");
			Assert.AreEqual(1, wf.AnalysesOC.Count);
			Assert.AreEqual(1, wfiAnalysis.MorphBundlesOS.Count);
			Assert.AreEqual(1, wfiAnalysis.MeaningsOC.Count);
		}


		private void SetupLexEntryAndSense(string formLexEntry, string senseGloss, out ILexEntry lexEntry1_Entry, out ILexSense lexEntry1_Sense1)
		{
			SetupLexEntryAndSense(formLexEntry, senseGloss, "adjunct", out lexEntry1_Entry, out lexEntry1_Sense1);
		}

		private void SetupLexEntryAndSense(string formLexEntry, string senseGloss, string partOfSpeech, out ILexEntry lexEntry1_Entry, out ILexSense lexEntry1_Sense1)
		{
			ITsString tssLexEntryForm = StringUtils.MakeTss(formLexEntry, Cache.DefaultVernWs);
			int clsidForm;
			// create a sense with a matching gloss
			DummyGenericMSA dummyMsa = new DummyGenericMSA();
			int hvoSenseMsaPos = m_sandbox.GetComboItemHvo(InterlinLineChoices.kflidWordPos, 0, partOfSpeech);
			dummyMsa.MainPOS = hvoSenseMsaPos;
			lexEntry1_Entry = LexEntry.CreateEntry(Cache,
				MoMorphType.FindMorphType(Cache, new MoMorphTypeCollection(Cache), ref formLexEntry, out clsidForm), tssLexEntryForm,
				senseGloss, dummyMsa);
			lexEntry1_Sense1 = lexEntry1_Entry.SensesOS[0];
		}

		[Test]
		[Ignore("Not sure what we're supposed to do with glossing on a polymorphemic guess. Need analyst input")]
		public void NewGlossForFocusBoxWithPolymorphemicGuess()
		{
			int hvoCba0_0 = GetCbaHvo(0, 0, 0);
			m_sandbox.SwitchWord(hvoCba0_0, false);
			// build polymorphemic guess
			ILexEntry lexEntry1_Entry;
			ILexSense lexEntry1_Sense1;
			SetupLexEntryAndSense("xx", "xx.existingsense1", out lexEntry1_Entry, out lexEntry1_Sense1);
			ILexEntry lexEntry2_Entry;
			ILexSense lexEntry2_Sense1;
			SetupLexEntryAndSense("xa", "xa.ExistingSense1", out lexEntry2_Entry, out lexEntry2_Sense1);
			// setup another morph bundle
			WfiWordform wf;
			IWfiAnalysis analysis = SetupAnalysisForEntry(hvoCba0_0, "0.0.xxxa", lexEntry1_Sense1, out wf);
			AppendMorphBundleToAnalysis(lexEntry1_Entry, lexEntry1_Sense1, analysis);
			AppendMorphBundleToAnalysis(lexEntry2_Entry, lexEntry2_Sense1, analysis);
			// load sandbox with a polymonomorphemic guess.
			m_sandbox.SwitchWord(hvoCba0_0, false);
			Assert.IsTrue(m_sandbox.UsingGuess);

			// begin testing.
		}

		private IWfiMorphBundle SetupMorphBundleForEntry(int hvoCba0_0, string gloss, ILexEntry lexEntry1_Entry, ILexSense lexEntry1_Sense1, out WfiWordform wf)
		{
			IWfiAnalysis analysis = SetupAnalysisForEntry(hvoCba0_0, gloss, lexEntry1_Sense1, out wf);
			return AppendMorphBundleToAnalysis(lexEntry1_Entry, lexEntry1_Sense1, analysis);
		}

		private static IWfiMorphBundle AppendMorphBundleToAnalysis(ILexEntry lexEntry1_Entry, ILexSense lexEntry1_Sense1, IWfiAnalysis analysis)
		{
			IWfiMorphBundle morphBundle = analysis.MorphBundlesOS.Append(new WfiMorphBundle());
			morphBundle.MorphRA = lexEntry1_Entry.LexemeFormOA;
			morphBundle.SenseRA = lexEntry1_Sense1;
			morphBundle.MsaRA = lexEntry1_Sense1.MorphoSyntaxAnalysisRA;
			return morphBundle;
		}

		private IWfiAnalysis SetupAnalysisForEntry(int hvoCba0_0, string gloss, ILexSense lexEntry1_Sense1, out WfiWordform wf)
		{
			int hvoWf = WfiWordform.GetWfiWordformFromInstanceOf(Cache, hvoCba0_0);
			wf = new WfiWordform(Cache, hvoWf);
			IWfiAnalysis analysis = wf.AnalysesOC.Add(new WfiAnalysis());
			analysis.CategoryRA = (lexEntry1_Sense1.MorphoSyntaxAnalysisRA as MoStemMsa).PartOfSpeechRA;
			IWfiGloss wfigloss = analysis.MeaningsOC.Add(new WfiGloss());
			wfigloss.Form.SetAlternative(gloss, Cache.DefaultAnalWs);
			return analysis;
		}


		[Test]
		public void PickLexGlossUsingExistingAnalysis()
		{
			int hvoCba0_0 = GetCbaHvo(0, 0, 0);
			m_sandbox.SwitchWord(hvoCba0_0, false);
			ILexEntry lexEntry1_Entry;
			ILexSense lexEntry1_Sense1;
			SetupLexEntryAndSense("xxxa", "0.0.xxxa", out lexEntry1_Entry, out lexEntry1_Sense1);
			ILexEntry lexEntry2_Entry;
			ILexSense lexEntry2_Sense1;
			SetupLexEntryAndSense("xxxa", "xxxa.AlternativeGloss", out lexEntry2_Entry, out lexEntry2_Sense1);
			// setup an existing analysis and gloss to match existing entry
			WfiWordform wf;
			IWfiMorphBundle morphBundle1 = SetupMorphBundleForEntry(hvoCba0_0, "0.0.xxxa", lexEntry1_Entry, lexEntry1_Sense1, out wf);
			IWfiMorphBundle morphBundle2 = SetupMorphBundleForEntry(hvoCba0_0, "xxxa.AlternativeGloss", lexEntry2_Entry, lexEntry2_Sense1, out wf);
			// load sandbox with a guess.
			m_sandbox.SwitchWord(hvoCba0_0, false);
			Assert.IsTrue(m_sandbox.UsingGuess);

			// mark the count of LexEntries
			int cEntriesOrig = Cache.LangProject.LexDbOA.EntriesOC.Count;

			// first select 'unknown' to clear the guess for the word gloss/pos
			m_sandbox.SelectItemInCombo(InterlinLineChoices.kflidWordGloss, 0, "Unknown");
			// confirm Sandbox is in the expected state.
			ITsString tssWordGlossInSandbox = m_sandbox.GetTssInSandbox(InterlinLineChoices.kflidWordGloss, Cache.DefaultAnalWs);
			Assert.AreEqual(null, tssWordGlossInSandbox.Text);
			int hvoPos = m_sandbox.GetRealHvoInSandbox(InterlinLineChoices.kflidWordPos, 0);
			Assert.AreEqual(0, hvoPos);

			// simulate selecting a lex gloss '0.0.xxxa'
			m_sandbox.SelectItemInCombo(InterlinLineChoices.kflidWordGloss, 0, lexEntry1_Sense1.Hvo);
			// confirm Sandbox is in the expected state.
			tssWordGlossInSandbox = m_sandbox.GetTssInSandbox(InterlinLineChoices.kflidWordGloss, Cache.DefaultAnalWs);
			Assert.AreEqual("0.0.xxxa", tssWordGlossInSandbox.Text);
			int hvoPos2 = m_sandbox.GetRealHvoInSandbox(InterlinLineChoices.kflidWordPos, 0);
			Assert.AreNotEqual(0, hvoPos2);

			// simulate selecting the other lex gloss 'xxxa.AlternativeGloss'
			m_sandbox.SelectItemInCombo(InterlinLineChoices.kflidWordGloss, 0, lexEntry2_Sense1.Hvo);
			// confirm Sandbox is in the expected state.
			tssWordGlossInSandbox = m_sandbox.GetTssInSandbox(InterlinLineChoices.kflidWordGloss, Cache.DefaultAnalWs);
			Assert.AreEqual("xxxa.AlternativeGloss", tssWordGlossInSandbox.Text);
			int hvoPos3 = m_sandbox.GetRealHvoInSandbox(InterlinLineChoices.kflidWordPos, 0);
			Assert.AreNotEqual(0, hvoPos3);

			// Next simulate picking an existing word gloss/pos by typing/selecting
			tssWordGlossInSandbox = m_sandbox.SetTssInSandbox(InterlinLineChoices.kflidWordGloss, Cache.DefaultAnalWs, "0.0.xxxa");
			// set word pos, to first possibility (e.g. 'adjunct')
			int hvoSbWordPos = m_sandbox.SelectIndexInCombo(InterlinLineChoices.kflidWordPos, 0, 0);

			// confirm the analysis (using existing analysis and a LexSense)
			int hvoGloss = m_sandbox.ConfirmAnalysis();
			WfiGloss wfiGloss = new WfiGloss(Cache, hvoGloss);

			// make sure we didn't add entries or senses to the Lexicon.
			int cEntriesAfter = Cache.LangProject.LexDbOA.EntriesOC.Count;
			Assert.AreEqual(cEntriesOrig, cEntriesAfter);
			Assert.AreEqual(1, lexEntry1_Entry.SensesOS.Count);

			// make sure the sense matches the existing one.
			ILexSense sense = m_sandbox.GetLexSenseForWord();
			Assert.AreEqual(lexEntry1_Sense1.Hvo, sense.Hvo);
			// make sure the strings of the wfi gloss matches the strings of the lex gloss.
			ValidateSenseWithAnalysis(sense, wfiGloss, hvoSbWordPos);

			// confirm we have not created a new analysis and that it is monomorphemic
			IWfiAnalysis wfiAnalysis = wfiGloss.Owner as WfiAnalysis;
			Assert.AreEqual(wf.Hvo, wfiAnalysis.OwnerHVO, "Expected confirmed analysis to be owned by the original wordform.");
			Assert.AreEqual(hvoSbWordPos, wfiAnalysis.CategoryRAHvo);
			Assert.AreEqual(2, wf.AnalysesOC.Count);
			Assert.AreEqual(1, wfiAnalysis.MorphBundlesOS.Count);
			Assert.AreEqual(1, wfiAnalysis.MeaningsOC.Count);
			IWfiAnalysis wfiAnalysis2 = (morphBundle2 as WfiMorphBundle).Owner as WfiAnalysis;
			Assert.AreEqual(1, wfiAnalysis2.MorphBundlesOS.Count);
			Assert.AreEqual(1, wfiAnalysis2.MeaningsOC.Count);

			// make sure the morph is linked to our lexicon sense, msa, and part of speech.
			IWfiMorphBundle wfiMorphBundle = wfiAnalysis.MorphBundlesOS[0];
			Assert.AreEqual(morphBundle1.Hvo, wfiMorphBundle.Hvo);
		}
	}
}
