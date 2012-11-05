using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO.DomainImpl;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.CoreImpl;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// <summary>
	/// Tests that paragraph analysis is adjusted with minimal loss of information when a paragraph is edited.
	/// </summary>
	[TestFixture]
	public class AnalysisAdjusterTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		/// <summary>
		/// Test that segment level analysis are left alone when the entire sentence is replaced with identical text in a different writing system.
		/// We want to make sure word level analyses are lost but verifying this has proved untestable in this framework .
		/// </summary>
		[Test]
		public virtual void FullIdenticalButNewWsReplacementLeavesTranslation()
		{
			//Test added to deal with change request from LT-12403
			int spanishWs = Cache.WritingSystemFactory.GetWsFromStr("es");
			var adjuster = new AdjustmentVerifier(Cache)
							{
								MatchingInitialSegments = 0,
								MatchOneMoreInitialTN = true
							};
			adjuster.OldContents = "pus yalola nihimbilira.";
			adjuster.SetNewContents(Cache.TsStrFactory.MakeString("pus yalola nihimbilira.", spanishWs));
			Cache.LangProject.AddToCurrentVernacularWritingSystems((IWritingSystem)Cache.WritingSystemFactory.get_Engine("es"));
			adjuster.RunTest();
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public virtual void InsertAndDeleteSpaceAtEndOfPara()
		{
			var adjuster = new BothWaysAdjustmentVerifier(Cache)
								{
								MatchingInitialSegments = 3,
								};
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.NewContents = "pus yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira. ";
			adjuster.RunTest();
		}


		/// <summary>
		/// This example caused a problem that was caught in LT-13633. It involves a combination of
		/// text in another writing sytem adjacent to punctuation, and caused a problem because the code that
		/// skips unchanged stuff was not properly treating foreign text as punctuation.
		/// </summary>
		[Test]
		public virtual void InsertCharacterAtEndOfParaWithPunctuationAndForeignWords()
		{
			var adjuster = new BothWaysAdjustmentVerifier(Cache)
			{
				MatchingInitialAnalyses = 19,
				MatchOneMoreInitialTN = true,
			};
			//											        1         2         3         4         5         6         7         8         9
			//								          0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			var bldr = Cache.TsStrFactory.MakeString("(TB: kajem) diki mi diki lapolo neki tari di ten te (TB: tarin) dep xy",
				Cache.DefaultVernWs).GetBldr();
			// The two 'TB' labels are supposed to be English.
			bldr.SetIntPropValues(1,3, (int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, Cache.DefaultAnalWs);
			bldr.SetIntPropValues(53, 55, (int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, Cache.DefaultAnalWs);
			adjuster.SetOldContents(bldr.GetString());
			bldr.ReplaceRgch(bldr.Length, bldr.Length, "z", 1, null); // the edit is typing an extra letter at the end
			adjuster.SetNewContents(bldr.GetString());
			adjuster.RunTest();
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public virtual void InsertAndDeleteSpaceBeforePeriodAtEndOfPara()
		{
			var adjuster = new BothWaysAdjustmentVerifier(Cache)
			{
				MatchingInitialSegments = 3,
							   };
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.NewContents = "pus yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira .";
			adjuster.RunTest();
		}

		/// <summary>
		/// We really wish it would match the middle analysis successfully and preserve it,
		/// but for now it just detects that the change extends from before yalola to after it,
		/// and concludes that it no longer knows for sure how that should be analyzed.
		/// </summary>
		[Test]
		public virtual void InsertAndDeleteSpacesThroughoutParaSingleSeg()
		{
			var adjuster = new BothWaysAdjustmentVerifier(Cache)
			{
				MatchingInitialAnalyses = 1,
				MatchingFinalAnalyses = 1,
				MatchOneMoreInitialTN = true, // one more than implied zero initial segments that match exactly
			};
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus yalola nihimbilira.";
			adjuster.NewContents = "pus  yalola  nihimbilira.";
			adjuster.RunTest();
		}

		/// <summary>
		/// What it says.
		/// </summary>
		[Test]
		public void FocusedObjectPostponesWordformDeletion()
		{
			var text = Cache.ServiceLocator.GetInstance<ITextFactory>().Create();
			//Cache.LangProject.TextsOC.Add(text);
			var sttext = Cache.ServiceLocator.GetInstance<IStTextFactory>().Create();
			text.ContentsOA = sttext;
			var para = Cache.ServiceLocator.GetInstance<IStTxtParaFactory>().Create();
			sttext.ParagraphsOS.Add(para);
			para.Contents = Cache.TsStrFactory.MakeString("xyzhello", Cache.DefaultVernWs);
			using (var pp = new ParagraphParser(Cache))
					pp.Parse(para);
			var wf = Cache.ServiceLocator.GetInstance<IWfiWordformRepository>().GetMatchingWordform(Cache.DefaultVernWs,
				"xyzhello");
			Cache.ServiceLocator.ObjectRepository.AddFocusedObject(wf);
			para.Contents = Cache.TsStrFactory.MakeString("xyhello", Cache.DefaultVernWs);
			m_actionHandler.EndUndoTask();

			Assert.That(wf.IsValidObject, "the focused wordform should not have been deleted");
			Cache.ServiceLocator.ObjectRepository.AddFocusedObject(wf); // count 2
			Cache.ServiceLocator.ObjectRepository.RemoveFocusedObject(wf);
			Assert.That(wf.IsValidObject, "the focused wordform should not have been deleted while some window still has it focused");
			Cache.ServiceLocator.ObjectRepository.RemoveFocusedObject(wf);
			Assert.That(wf.IsValidObject, Is.False, "the focused wordform should be deleted when focus moves");
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public virtual void InsertAndDeleteSpacesInFirstSegOfMultiSegPara()
		{
			var adjuster = new BothWaysAdjustmentVerifier(Cache)
			{
				MatchingFinalSegments = 2,
				MatchingInitialSegments = 0,
				MatchOneMoreInitialTN = true,
				MatchingInitialAnalyses = 1,
			};
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola. \"hesyla nihimbilira.\"";
			adjuster.NewContents = "pus  yalola nihimbilira.  nihimbilira pus yalola. \"hesyla nihimbilira.\"";
			adjuster.RunTest();
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public virtual void InsertAndDeleteSpacesThroughoutParaMultiSeg()
		{
			var adjuster = new BothWaysAdjustmentVerifier(Cache)
			{
				MatchingInitialSegments = 0,
				MatchingFinalSegments = 1,
				MatchOneMoreInitialTN = true,
				MatchingInitialAnalyses = 1
			};
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola. \"hesyla nihimbilira.\"";
			adjuster.NewContents = "pus  yalola  nihimbilira.  nihimbilira  pus  yalola.  \"hesyla nihimbilira.\"";
			adjuster.RunTest();
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public virtual void InsertAndDeleteSpacesAmidstEndingPunctuation()
		{
			var adjuster = new BothWaysAdjustmentVerifier(Cache)
			{
				MatchingInitialSegments = 0,
				MatchingInitialAnalyses = 1,
				MatchOneMoreInitialTN = true,
				MatchOneMoreFinalTN = true
			};
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "19-20Pakufa kwa Herodi, ntumwi wa Mulungu aonekera kuna Zuze mundoto ku Idjitu, mbampanga tenepa: \"Lamuka. Kwata mwana na mai wace ubwerera nawo ku dziko ya Izirayeli, tangwi afa ule akhafuna kupha mwana.\" 21Zuze alamuka, mbakwata mwana na mai wace, mbabwerera kuenda nawo ku dziko ya Izirayeli. 22Mbwenye, Zuze na kubva kuti pa kufa kwa mambo Herodi, adapita pampando pace ndi mwanace anacemerwa Arikilau, agopa kubwerera ku dziko eneyi. Na thangwi eneyi, Mulungu amulotesa ponto, iye mbalamuka mbaenda kakhala ku dziko ya Galileya. 23Aenda kakhalamu ndzinda wa Nazarete. Na mwenemu pyacitika pirepi khadalongwa na maporofeta kuti: \"Iye anadza kacemerwa munthu wa ku Nazareya.\"";
			adjuster.NewContents = "19-20\u2008Pakufa k\u2008wa H\u2008er\u2008o\u2008di\u2008, n\u2008tumwi w\u2008a Mu\u2008lungu aoneke\u2008ra ku?na Z\u2008uze mu\u2008ndoto ku Idj\u2008i\u2008tu, m\u2008b\u2008ampanga t\u2008ene\u2008pa\u2008: “L\u2008a\u2008mu\u2008ka\u2008. K\u2008wa\u2008t\u2008a m\u2008wana n\u2008a ma\u2008i wa\u2008ce u\u2008bwerera n\u2008awo ku dz\u2008iko y\u2008a I\u2008zi\u2008ra\u2008ye\u2008li\u2008, t\u2008a\u2008n\u2008gw\u2008i a\u2008f\u2008a ul\u2008e a\u2008kha\u2008funa ku\u2008ph\u2008a m\u2008wana\u2008.\" 21\u2008Zuze a\u2008lamuka\u2008, m\u2008b\u2008akwata m\u2008wana n\u2008a ma\u2008i wa\u2008ce\u2008, m\u2008b\u2008abwerera ku\u2008e\u2008nda n\u2008awo ku dz\u2008iko y\u2008a I\u2008zi\u2008ra\u2008ye\u2008li\u2008. 22\u2008Mbwenye\u2008, Z\u2008uze n\u2008a ku\u2008bv\u2008a ku\u2008ti p\u2008a ku\u2008f\u2008a k\u2008wa m\u2008ambo H\u2008er\u2008o\u2008di\u2008, a\u2008d\u2008apita pa\u2008mpando pa\u2008ce n\u2008di mw\u2008anace a\u2008n\u2008acemer\u2008wa A\u2008ri\u2008k\u2008i\u2008l\u2008a\u2008u, a\u2008gopa ku\u2008bwerera ku dz\u2008iko ene\u2008yi\u2008. N\u2008a t\u2008ha\u2008n\u2008gw\u2008i ene\u2008yi\u2008, Mu\u2008lungu a\u2008mu\u2008lot\u2008e\u2008sa po\u2008n\u2008to\u2008, i\u2008ye m\u2008b\u2008alamuka m\u2008b\u2008aenda ka\u2008khala ku dz\u2008iko y\u2008a G\u2008ali\u2008l\u2008e\u2008ya\u2008. 23\u2008Aenda ka\u2008khalamu n\u2008dzinda w\u2008a Na\u2008za\u2008r\u2008e\u2008t\u2008e\u2008. N\u2008a mwene\u2008mu pya\u2008citika pire\u2008pi kha\u2008dalo\u2008ngwa n\u2008a m\u2008aporofeta ku\u2008ti\u2008: “I\u2008ye a\u2008n\u2008adza ka\u2008cemer\u2008wa mu\u2008nthu w\u2008a ku Na\u2008za\u2008r\u2008e\u2008ya\u2008.\"";
			adjuster.RunTest();
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public virtual void InsertAndDeleteSpaceAtBeginningOfParagraph()
		{
			var adjuster = new BothWaysAdjustmentVerifier(Cache)
							   {
								   MatchingInitialSegments = 3,
							   };
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.NewContents = " pus yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.RunTest();
		}
		/// <summary>
		///
		/// </summary>
		[Test]
		public virtual void InsertAndDeleteSpaceInPhrase()
		{
			var adjuster = new AdjustmentVerifier(Cache)
			{
				Phrases = new [] {new Tuple<int, int, int>(0,0,1)},
				MatchOneMoreInitialTN = true,
			};
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "kick bucket";
			adjuster.NewContents = "kick  bucket";
			adjuster.RunTest();
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public virtual void PreservePhraseBeforeChange()
		{
			var adjuster = new AdjustmentVerifier(Cache)
			{
				Phrases = new[] { new Tuple<int, int, int>(0, 2, 4) },
				MatchOneMoreInitialTN = true,
			};
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "He will kick the bucket son";
			adjuster.NewContents = "He will kick the bucket soon";
			adjuster.RunTest();
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public virtual void PreservePhraseAfterChange()
		{
			var adjuster = new AdjustmentVerifier(Cache)
			{
				Phrases = new[] { new Tuple<int, int, int>(0, 2, 4) },
				MatchOneMoreInitialTN = true,
			};
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "He wil kick the bucket soon";
			adjuster.NewContents = "He will kick the bucket soon";
			adjuster.RunTest();
		}
		/// <summary>
		///
		/// </summary>
		[Test]
		public virtual void InsertAndDeleteSpaceAtEndOfFirstSentence()
		{
			var adjuster = new BothWaysAdjustmentVerifier(Cache)
							   {
								   MatchingInitialSegments = 3
							   };
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.NewContents = "pus yalola nihimbilira . nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.RunTest();
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public virtual void InsertAndDeleteSpaceInside1stWord()
		{
			var adjuster = new BothWaysAdjustmentVerifier(Cache)
							   {
								   MatchingFinalSegments = 2,
								   MatchingFinalAnalyses = 3,
								   MatchOneMoreInitialTN = true,
							   };
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.NewContents = "pu s yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.RunTest();
		}

		/// <summary>
		/// FWR-341: going through intermediate states should not leave garbage wordforms around.
		/// </summary>
		[Test]
		public void TypingCleansUpSpuriousWordforms()
		{
			// We won't run the adjuster, because it makes two texts, with the old and new contents,
			// and the extra text suppresses the cleanup. But it handles a lot of the initialization we want.
			var adjuster = new AdjustmentVerifier(Cache);
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus whatst nihimbilira.";
			//adjuster.NewContents = "pus whatsthis nihimbilira.";
			adjuster.ParseOldText(); // Now it has segments and wordforms.
			var wfRepo = Cache.ServiceLocator.GetInstance<IWfiWordformRepository>();
			var badWordform = wfRepo.GetMatchingWordform(Cache.DefaultVernWs, "whatst");
			// Simulate typing 's' on end of incomplete word
			adjuster.OldContents = "pus whatsth nihimbilira.";
			// Assume test data has no other refs to "whatst".
			Assert.IsFalse(badWordform.IsValidObject, "Should have deleted spurious wordform");

			var saveWordform = wfRepo.GetMatchingWordform(Cache.DefaultVernWs, "whatsth");
			saveWordform.SpellingStatus = (int)SpellingStatusStates.incorrect;
			adjuster.OldFirstPara.ParseIsCurrent = true; // AnalysisAdjuster clears this, but need it true to run full adjuster
			adjuster.OldContents = "pus whatsthi nihimbilira.";
			Assert.IsTrue(saveWordform.IsValidObject, "Should not have deleted wordform marked incorrect");

			saveWordform = wfRepo.GetMatchingWordform(Cache.DefaultVernWs, "whatsthi");
			saveWordform.SpellingStatus = (int)SpellingStatusStates.correct;
			adjuster.OldFirstPara.ParseIsCurrent = true; // AnalysisAdjuster clears this, but need it true to run full adjuster
			adjuster.OldContents = "pus whatsthis nihimbilira.";
			Assert.IsTrue(saveWordform.IsValidObject, "Should not have deleted wordform marked correct");

			saveWordform = wfRepo.GetMatchingWordform(Cache.DefaultVernWs, "whatsthis");
			var analysis = Cache.ServiceLocator.GetInstance<IWfiAnalysisFactory>().Create();
			saveWordform.AnalysesOC.Add(analysis);
			adjuster.OldFirstPara.ParseIsCurrent = true; // AnalysisAdjuster clears this, but need it true to run full adjuster
			adjuster.OldContents = "pus whatsthisa nihimbilira.";
			Assert.IsTrue(saveWordform.IsValidObject, "Should not have deleted wordform with analysis");

			saveWordform = wfRepo.GetMatchingWordform(Cache.DefaultVernWs, "whatsthisa");
			adjuster.AddOldParagraph("More text containing whatsthisa.");
			adjuster.ParseOldText();
			adjuster.OldContents = "pus whatsthisall nihimbilira.";
			Assert.IsTrue(saveWordform.IsValidObject, "Should not have deleted wordform used elsewhere");
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public virtual void InsertAndDeleteSegmentBreakAfter1stWord()
		{
			var adjuster = new BothWaysAdjustmentVerifier(Cache)
			{
				MatchingFinalSegments = 2,
				MatchingFinalAnalyses = 3,
				MatchingInitialAnalyses = 1,
				MatchOneMoreInitialTN = true, //TNs Para0Seg0 ends up on Para0Seg0 vs. Para0Seg1
				ReverseShouldMergeSegmentTN = true,
			};
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.NewContents = "pus. yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.RunTest();
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public virtual void RemoveMiddleSegmentsAndMergeOuterTwo()
		{
			var adjuster = new AdjustmentVerifier(Cache)
			{
				MatchingFinalSegments = 2,
				MatchingFinalAnalyses = 2,
				MatchingInitialAnalyses = 1,
				ShouldMergeSegmentTN = true,
			};
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus. yalola. nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.NewContents = "pus nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.RunTest();
			// Do this test again with multiple spaces after segment beak being deleted but the last two spaces
			// of the middle segment remains.
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus. yalola.	nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.NewContents = "pus  nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.RunTest();

			adjuster = new AdjustmentVerifier(Cache)
			{
				//MatchingFinalSegments = 2,
				MatchingFinalAnalyses = 2,
				MatchingInitialAnalyses = 1,
				ShouldMergeSegmentTN = true,
			};
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus. yalola. nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.NewContents = "pus hesyla nihimbilira.";
			adjuster.RunTest();
			// Do this test again with multiple spaces after segment beak being deleted but the last two spaces
			// of the middle segment remains.
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus. yalola.    nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.NewContents = "pus  hesyla nihimbilira.";
			adjuster.RunTest();
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public virtual void RemoveMiddleSegmentsAndMergeTwoLeavingParethesis()
		{
			var adjuster = new AdjustmentVerifier(Cache)
			{
				MatchingFinalSegments = 2,
				MatchingFinalAnalyses = 2,
				MatchingInitialAnalyses = 1,
				ShouldMergeSegmentTN = true,
			};
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus. yalola.) nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.NewContents = "pus) nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.RunTest();
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public virtual void RemoveMiddleThreeSegmentsAndMergeTwoLeavingParethesis()
		{
			var adjuster = new AdjustmentVerifier(Cache)
			{
				//MatchingFinalSegments = 2,
				MatchingFinalAnalyses = 3,
				MatchingInitialAnalyses = 1,
				ShouldMergeSegmentTN = true,
			};
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus. yalola. nihimbilira. nihimbilira pus yalola.) hesyla nihimbilira.";
			adjuster.NewContents = "pus) hesyla nihimbilira.";
			adjuster.RunTest();
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public virtual void RemoveMiddleSegmentAndMergeTwoLeavingParethesisAndChangeAnalysis()
		{
			var adjuster = new AdjustmentVerifier(Cache)
			{
				MatchingFinalSegments = 2,
				MatchingFinalAnalyses = 1,
				MatchingInitialAnalyses = 1,
				ShouldMergeSegmentTN = true,
			};
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus. yalola.) nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.NewContents = "pus) xxximbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.RunTest();
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public virtual void RemoveMostOfSegmentAndMergeTwoLeavingPeriodandParethesis()
		{
			var adjuster = new AdjustmentVerifier(Cache)
			{
				MatchingFinalSegments = 3,
				MatchingFinalAnalyses = 1,
				MatchingInitialAnalyses = 1,
				ShouldMergeSegmentTN = true,
			};
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus! yalola.) nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.NewContents = "pus.) nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.RunTest();
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public virtual void DeleteSegmentBreakToMerge2Sentences()
		{
			var adjuster = new BothWaysAdjustmentVerifier(Cache)
							   {
								   MatchingFinalSegments = 1,
								   MatchingFinalAnalyses = 4,
								   MatchingInitialAnalyses = 3,
								   ShouldMergeSegmentTN = true,
							   };
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.NewContents = "pus yalola nihimbilira nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.RunTest();
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public virtual void ReplaceSegBreakWithAnotherSegBreak()
		{
			var adjuster = new BothWaysAdjustmentVerifier(Cache)
			{
				MatchingFinalSegments = 2,
				MatchingInitialAnalyses = 3,
				MatchOneMoreInitialTN = true,

			};
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.NewContents = "pus yalola nihimbilira? nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.RunTest();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that a punctuation character added before a label segment adds it to that
		/// label segment. (see FWR-1359 to see what motivated this test)
		/// This is very rare, especially in finished Scripture. Although this test uses the
		/// example of a paragraph with an opening quote mark before the verse number, in actual
		/// practice we would expect that punctuation to appear after the verse number.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public virtual void InsertPunctuationBeforeLabelSegment()
		{
			Cache.LangProject.TranslatedScriptureOA = Cache.ServiceLocator.GetInstance<IScriptureFactory>().Create();
			var adjuster = new BothWaysAdjustmentVerifier(Cache)
			{
				MatchingFinalSegments = 1,
				MatchingInitialAnalyses = 0,
				MatchingFinalAnalyses = 0,
				MatchOneMoreInitialTN = true
			};
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "1Good", StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			bldr.SetStrPropValue(0, 1, (int)FwTextPropType.ktptNamedStyle, ScrStyleNames.VerseNumber);
			adjuster.SetOldContents(bldr.GetString());
			bldr.Replace(0, 0, "\"", StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			adjuster.SetNewContents(bldr.GetString());
			adjuster.RunTest();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that a word-forming character added before a mid-paragraph label segment adds
		/// it to the preceding (non-label) segment. (FWR-1350)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public virtual void InsertLetterBeforeLabelSegment()
		{
			Cache.LangProject.TranslatedScriptureOA = Cache.ServiceLocator.GetInstance<IScriptureFactory>().Create();
			var adjuster = new BothWaysAdjustmentVerifier(Cache)
			{
				MatchingFinalSegments = 2, // Verse 2 + Bad.
				MatchingInitialSegments = 1, // Chapter 1
				MatchingInitialAnalyses = 1, // Goody
				MatchingFinalAnalyses = 0,
				MatchOneMoreInitialTN = true,
			};
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "1Goody 2Bad.", StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			bldr.SetStrPropValue(0, 1, (int)FwTextPropType.ktptNamedStyle, ScrStyleNames.VerseNumber);
			bldr.SetStrPropValue(7, 8, (int)FwTextPropType.ktptNamedStyle, ScrStyleNames.VerseNumber);
			adjuster.SetOldContents(bldr.GetString());
			bldr.Replace(7, 7, "M", StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			adjuster.SetNewContents(bldr.GetString());
			adjuster.RunTest();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that applying a style change across multiple segmemnts (but leaving chapter
		/// and verses unscathed) does not lose any analyses. (TE-9271)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public virtual void MassiveStyleChangeInScripture()
		{
			Cache.LangProject.TranslatedScriptureOA = Cache.ServiceLocator.GetInstance<IScriptureFactory>().Create();
			var adjuster = new BothWaysAdjustmentVerifier(Cache)
			{
				MatchingInitialSegments = 8,
			};
			ITsStrBldr bldr = TsStrBldrClass.Create();
			//	                          1         2         3         4         5         6         7         8         9
			//                  0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			bldr.Replace(0, 0, "1Goody 2Bad. But not too bad. 3Wonderful. And nice.", StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			bldr.SetStrPropValue(0, 1, (int)FwTextPropType.ktptNamedStyle, ScrStyleNames.ChapterNumber);
			bldr.SetStrPropValue(7, 8, (int)FwTextPropType.ktptNamedStyle, ScrStyleNames.VerseNumber);
			bldr.SetStrPropValue(30, 31, (int)FwTextPropType.ktptNamedStyle, ScrStyleNames.VerseNumber);
			adjuster.SetOldContents(bldr.GetString());
			bldr.SetStrPropValue(8, 29, (int)FwTextPropType.ktptNamedStyle, "Miscellaneous style");
			bldr.SetStrPropValue(31, 45, (int)FwTextPropType.ktptNamedStyle, "Miscellaneous style");
			adjuster.SetNewContents(bldr.GetString());
			adjuster.RunTest();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that applying a writing system change across multiple segmemnts does not lose
		/// any analyses. (FWR-3464)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("Waiting on FWR-3464")]
		public virtual void WSChangeInScripture()
		{
			Cache.LangProject.TranslatedScriptureOA = Cache.ServiceLocator.GetInstance<IScriptureFactory>().Create();
			var adjuster = new BothWaysAdjustmentVerifier(Cache)
			{
				MatchingInitialSegments = 2,
				MatchingFinalSegments = 1,
			};
			ITsStrBldr bldr = TsStrBldrClass.Create();
			//	                          1         2         3         4         5         6         7         8         9
			//                  0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			bldr.Replace(0, 0, "1Goody 2Bad. But not too bad. 3Wonderful. And nice.", StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			bldr.SetStrPropValue(0, 1, (int)FwTextPropType.ktptNamedStyle, ScrStyleNames.ChapterNumber);
			bldr.SetStrPropValue(7, 8, (int)FwTextPropType.ktptNamedStyle, ScrStyleNames.VerseNumber);
			bldr.SetStrPropValue(30, 31, (int)FwTextPropType.ktptNamedStyle, ScrStyleNames.VerseNumber);
			adjuster.SetOldContents(bldr.GetString());
			bldr.SetIntPropValues(7, 41, (int)FwTextPropType.ktptWs, 0, Cache.DefaultAnalWs);
			adjuster.SetNewContents(bldr.GetString());
			adjuster.RunTest();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that label text added before an end-of-sentence mark adds a new segment for
		/// the label correctly. (FWR-1903)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public virtual void InsertLabelBeforeEOS()
		{
			Cache.LangProject.TranslatedScriptureOA = Cache.ServiceLocator.GetInstance<IScriptureFactory>().Create();
			var adjuster = new BothWaysAdjustmentVerifier(Cache)
			{
				MatchingInitialSegments = 1, // Verse 1
				MatchingInitialAnalyses = 1, // Goody
				MatchingFinalAnalyses = 1, // Bad
				MatchOneMoreInitialTN = true,
				MatchOneMoreFinalTN = true,
			};
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "1Goody .Bad.", StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			bldr.SetStrPropValue(0, 1, (int)FwTextPropType.ktptNamedStyle, ScrStyleNames.VerseNumber);
			adjuster.SetOldContents(bldr.GetString());
			bldr.Replace(7, 7, "2", StyleUtils.CharStyleTextProps(ScrStyleNames.VerseNumber, Cache.DefaultVernWs));
			adjuster.SetNewContents(bldr.GetString());
			adjuster.RunTest();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that label text added before an end-of-sentence mark adds a new segment for
		/// the label correctly. (FWR-1903)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public virtual void InsertMultiCharLabelBeforeEOS()
		{
			Cache.LangProject.TranslatedScriptureOA = Cache.ServiceLocator.GetInstance<IScriptureFactory>().Create();
			var adjuster = new BothWaysAdjustmentVerifier(Cache)
			{
				MatchingInitialSegments = 1, // Verse 1
				MatchingInitialAnalyses = 1, // Goody
				MatchingFinalAnalyses = 1, // Bad
				MatchOneMoreInitialTN = true,
				MatchOneMoreFinalTN = true,
			};
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "1Goody .Bad.", StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			bldr.SetStrPropValue(0, 1, (int)FwTextPropType.ktptNamedStyle, ScrStyleNames.VerseNumber);
			adjuster.SetOldContents(bldr.GetString());
			bldr.Replace(7, 7, "12", StyleUtils.CharStyleTextProps(ScrStyleNames.VerseNumber, Cache.DefaultVernWs));
			adjuster.SetNewContents(bldr.GetString());
			adjuster.RunTest();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that label text added before an end-of-sentence mark adds a new segment for
		/// the label correctly. (FWR-1903)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public virtual void ReplaceSpaceWithMultiCharLabelBeforeEOS()
		{
			Cache.LangProject.TranslatedScriptureOA = Cache.ServiceLocator.GetInstance<IScriptureFactory>().Create();
			var adjuster = new BothWaysAdjustmentVerifier(Cache)
			{
				MatchingInitialSegments = 1, // Verse 1
				MatchingInitialAnalyses = 1, // Goody
				MatchingFinalAnalyses = 1, // Bad
				MatchOneMoreInitialTN = true,
				MatchOneMoreFinalTN = true,
			};
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "1Goody .Bad.", StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			bldr.SetStrPropValue(0, 1, (int)FwTextPropType.ktptNamedStyle, ScrStyleNames.VerseNumber);
			adjuster.SetOldContents(bldr.GetString());
			bldr.Replace(6, 7, "12", StyleUtils.CharStyleTextProps(ScrStyleNames.VerseNumber, Cache.DefaultVernWs));
			adjuster.SetNewContents(bldr.GetString());
			adjuster.RunTest();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that applying a verse number style to text does not use the new verse number
		/// as a text segment (should be a label). (FWR-1416)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public virtual void ApplyVerseNumberStyleToText_BeginSegment()
		{
			Cache.LangProject.TranslatedScriptureOA = Cache.ServiceLocator.GetInstance<IScriptureFactory>().Create();
			var adjuster = new AdjustmentVerifier(Cache)
			{
				MatchingFinalSegments = 0,
				MatchingInitialSegments = 2, // 1 + Goody.
				MatchingInitialAnalyses = 0, // 2Bad, now 2
				MatchingFinalAnalyses = 0,
				MatchOneMoreInitialTN = false,
				MatchOneMoreFinalTN = true
			};
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "1Goody. 2Bad.", StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			bldr.SetStrPropValue(0, 1, (int)FwTextPropType.ktptNamedStyle, ScrStyleNames.VerseNumber);
			adjuster.SetOldContents(bldr.GetString());
			bldr.SetStrPropValue(8, 9, (int)FwTextPropType.ktptNamedStyle, ScrStyleNames.VerseNumber);
			adjuster.SetNewContents(bldr.GetString());
			adjuster.RunTest();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that applying a verse number style to text does not use the new verse number
		/// as a text segment (should be a label). (FWR-1416)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public virtual void ApplyVerseNumberStyleToText_MidSegment()
		{
			Cache.LangProject.TranslatedScriptureOA = Cache.ServiceLocator.GetInstance<IScriptureFactory>().Create();
			var adjuster = new AdjustmentVerifier(Cache)
			{
				MatchingFinalSegments = 0,
				MatchingInitialSegments = 1, // 1
				MatchingInitialAnalyses = 1, // Goodyr
				MatchingFinalAnalyses = 0,
				MatchOneMoreInitialTN = true,
				MatchOneMoreFinalTN = false
			};
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "1Goodyr 2Bad.", StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			bldr.SetStrPropValue(0, 1, (int)FwTextPropType.ktptNamedStyle, ScrStyleNames.VerseNumber);
			adjuster.SetOldContents(bldr.GetString());
			bldr.SetStrPropValue(8, 9, (int)FwTextPropType.ktptNamedStyle, ScrStyleNames.VerseNumber);
			adjuster.SetNewContents(bldr.GetString());
			adjuster.RunTest();
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public virtual void ReplaceFootnoteOrcWithAnotherFootnoteOrc()
		{
			var adjuster = new BothWaysAdjustmentVerifier(Cache)
			{
				MatchingInitialSegments = 0,
				MatchingFinalSegments = 0,
				MatchingInitialAnalyses = 1,
				MatchingFinalAnalyses = 1,
				MatchOneMoreInitialTN = true,
			};
			// Create a string with an ORC in the middle for the old contents
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "Start", StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			TsStringUtils.InsertOrcIntoPara(Guid.NewGuid(), FwObjDataTypes.kodtOwnNameGuidHot, bldr, bldr.Length, bldr.Length, Cache.DefaultVernWs);
			bldr.Replace(bldr.Length, bldr.Length, " end", StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			adjuster.SetOldContents(bldr.GetString());
			// Replace the old ORC with a new one for the new contents
			TsStringUtils.InsertOrcIntoPara(Guid.NewGuid(), FwObjDataTypes.kodtOwnNameGuidHot, bldr, 5, 6, Cache.DefaultVernWs);
			adjuster.SetNewContents(bldr.GetString());

			adjuster.RunTest();
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public virtual void ReplaceSegBreakNonSegBreak()
		{
			var adjuster = new BothWaysAdjustmentVerifier(Cache)
			{
				MatchingFinalSegments = 1,
				MatchingFinalAnalyses = 4,
				MatchingInitialAnalyses = 3,
				ShouldMergeSegmentTN = true,
			};
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.NewContents = "pus yalola nihimbilira, nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.RunTest();
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public virtual void InsertAndDeleteWordFormCharInside1stWord()
		{
			var adjuster = new BothWaysAdjustmentVerifier(Cache)
			{
				MatchingFinalSegments = 2,
				MatchingFinalAnalyses = 3,
				MatchOneMoreInitialTN = true,
			};
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.NewContents = "Apus yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.RunTest();
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public virtual void InsertNewWordIn1stSentence()
		{
			var adjuster = new BothWaysAdjustmentVerifier(Cache)
			{
				MatchingFinalSegments = 2,
				MatchingFinalAnalyses = 4,
				MatchOneMoreInitialTN = true,
			};
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.NewContents = "NewWord pus yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.RunTest();
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public virtual void ReplaceWordWithSameSizeWord()
		{
			var adjuster = new BothWaysAdjustmentVerifier(Cache)
			{
				MatchingFinalSegments = 2,
				MatchingFinalAnalyses = 3,
				MatchOneMoreInitialTN = true,
			};
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.NewContents = "sup yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.RunTest();
		}
		/// <summary>
		///
		/// </summary>
		[Test]
		public virtual void NoChange()
		{
			var adjuster = new BothWaysAdjustmentVerifier(Cache)
			{
				MatchingInitialSegments = 3,
			};
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.NewContents = "pus yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.RunTest();
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public virtual void ReplaceWordWithWords()
		{
			var adjuster = new BothWaysAdjustmentVerifier(Cache)
			{
				MatchingFinalSegments = 2,
				MatchingFinalAnalyses = 3,
				MatchOneMoreInitialTN = true,
			};
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.NewContents = "sup dup lup yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.RunTest();

			adjuster = new BothWaysAdjustmentVerifier(Cache)
			{
				MatchingInitialSegments = 1,
				MatchingFinalSegments = 1,
				MatchingFinalAnalyses = 2,
				MatchingInitialAnalyses = 1,
				MatchOneMoreInitialTN = true,
				MatchOneMoreFinalTN = true,
			};
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.NewContents = "pus yalola nihimbilira. nihimbilira sup dup lup yalola. hesyla nihimbilira.";
			adjuster.RunTest();
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public virtual void MergeWords()
		{
			var adjuster = new BothWaysAdjustmentVerifier(Cache)
			{
				MatchingFinalSegments = 2,
				MatchingFinalAnalyses = 1,
				MatchOneMoreInitialTN = true,
			};
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.NewContents = "pusyalolanihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.RunTest();
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public virtual void InsertDuplicateWordformIn1stSentence()
		{
			var adjuster = new BothWaysAdjustmentVerifier(Cache)
			{
				MatchingFinalSegments = 2,
				MatchingFinalAnalyses = 3,
				MatchingInitialAnalyses = 1,
				MatchOneMoreInitialTN = true,
			};
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.NewContents = "pus pus yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.RunTest();
		}

		#region Deletion tests
		/// <summary>
		///
		/// </summary>
		[Test]
		public virtual void DeleteLastWordIn1stSentence()
		{
			var adjuster = new BothWaysAdjustmentVerifier(Cache)
			{
				MatchingFinalSegments = 2,
				MatchingFinalAnalyses = 1,
				MatchingInitialAnalyses = 2,
				MatchOneMoreInitialTN = true,
			};
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.NewContents = "pus yalola. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.RunTest();
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public virtual void RemoveMiddleOccuranceOfWord()
		{
			var adjuster = new BothWaysAdjustmentVerifier(Cache)
							{
								MatchingFinalSegments = 1,
								MatchingInitialSegments = 1,
								MatchingFinalAnalyses = 3,
								MatchOneMoreInitialTN = true,
							};
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.NewContents = "pus yalola nihimbilira. pus yalola. hesyla nihimbilira.";
			adjuster.RunTest();
		}


		/// <summary>
		///
		/// </summary>
		[Test]
		public virtual void DeleteRangeSpanningSegmentBoundaryAndWholeWords()
		{
			var adjuster = new BothWaysAdjustmentVerifier(Cache)
			{
				MatchingFinalSegments = 1,
				MatchingFinalAnalyses = 2,
				MatchingInitialAnalyses = 2,
				ShouldMergeSegmentTN = true,
			};
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.NewContents = "pus yalola yalola. hesyla nihimbilira.";
			adjuster.RunTest();
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public virtual void DeleteFirstTwoSentences()
		{
			var adjuster = new BothWaysAdjustmentVerifier(Cache)
			{
				MatchingFinalSegments = 1,
			};
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.NewContents = "hesyla nihimbilira.";
			adjuster.RunTest();
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public virtual void DeleteLastTwoSentences()
		{
			var adjuster = new BothWaysAdjustmentVerifier(Cache)
			{
				MatchingInitialSegments = 1,
			};
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.NewContents = "pus yalola nihimbilira.";
			adjuster.RunTest();
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public virtual void DeleteAllSentencesResultingInEmptyPara()
		{
			var adjuster = new BothWaysAdjustmentVerifier(Cache);
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.NewContents = "";
			adjuster.RunTest();
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public virtual void DeleteSentenceWithoutSegmentBreakChar()
		{
			var adjuster = new BothWaysAdjustmentVerifier(Cache);
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = " pus yalola nihimbilira ";
			adjuster.NewContents = "";
			adjuster.RunTest();
		}
		#endregion

		#region Paragraph-level editing tests
		/// <summary>
		/// This will require having two paragraphs and deleting one.
		/// </summary>
		[Test]
		public virtual void DeleteParagraph()
		{
			var adjuster = new AdjustmentVerifier(Cache)
			{
				MatchingFinalParagraphs = 1,
			};
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola.";
			adjuster.AddOldParagraph("hesyla nihimbilira.");
			adjuster.NewContents = "hesyla nihimbilira.";
			adjuster.ChangeMaker = text => text.ParagraphsOS.RemoveAt(0);
			adjuster.RunTest();
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public virtual void DeleteParagraphBreak()
		{
			var adjuster = new AdjustmentVerifier(Cache)
			{
				MatchingFinalSegments = 1,
				MatchingInitialSegments = 2,
			};
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola.";
			adjuster.AddOldParagraph("sup layalo ranihimbili.");
			adjuster.NewContents = "pus yalola nihimbilira. nihimbilira pus yalola.sup layalo ranihimbili.";
			adjuster.ChangeMaker = text =>
				{
					var para0 = text[0];
					var para1 = text[1];
					text.Cache.DomainDataByFlid.MoveString(para1.Hvo, StTxtParaTags.kflidContents, 0,
						0, para1.Contents.Length, para0.Hvo, StTxtParaTags.kflidContents, 0,
						para0.Contents.Length, false);
					text.ParagraphsOS.RemoveAt(1);
				};
			adjuster.RunTest();
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public virtual void DeleteParagraphBreakAndMergeWords()
		{
			var adjuster = new AdjustmentVerifier(Cache)
			{
				MatchingFinalAnalyses = 3,
				MatchingInitialSegments = 1,
				MatchingInitialAnalyses = 2,
				ShouldMergeSegmentTN = true,
			};
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola";
			adjuster.AddOldParagraph("sup layalo ranihimbili.");
			adjuster.NewContents = "pus yalola nihimbilira. nihimbilira pus yalolasup layalo ranihimbili.";
			adjuster.ChangeMaker = text =>
			{
				var para0 = text[0];
				var para1 = text[1];
				text.Cache.DomainDataByFlid.MoveString(para1.Hvo, StTxtParaTags.kflidContents, 0,
					0, para1.Contents.Length, para0.Hvo, StTxtParaTags.kflidContents, 0, para0.Contents.Length, false);
				text.ParagraphsOS.RemoveAt(1);
			};
			adjuster.RunTest();
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public virtual void DeleteParagraphBreakAndMergeSegsButNotWords()
		{
			var adjuster = new AdjustmentVerifier(Cache)
			{
				MatchingFinalAnalyses = 4,
				MatchingInitialSegments = 1,
				MatchingInitialAnalyses = 3,
				ShouldMergeSegmentTN = true,
			};
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola ";
			adjuster.AddOldParagraph("sup layalo ranihimbili.");
			adjuster.NewContents = "pus yalola nihimbilira. nihimbilira pus yalola sup layalo ranihimbili.";
			adjuster.ChangeMaker = text =>
			{
				var para0 = text[0];
				var para1 = text[1];
				text.Cache.DomainDataByFlid.MoveString(para1.Hvo, StTxtParaTags.kflidContents, 0,
													   0, para1.Contents.Length,
													   para0.Hvo, StTxtParaTags.kflidContents, 0, para0.Contents.Length, false);
				text.ParagraphsOS.RemoveAt(1);
			};
			adjuster.RunTest();
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public virtual void DeleteParagraphBreakMovingMultipleSegs()
		{
			var adjuster = new AdjustmentVerifier(Cache)
			{
				MatchingInitialSegments = 2,
				MatchingFinalSegments = 2,
			};
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola.";
			adjuster.AddOldParagraph("sup layalo ranihimbili. Another sentence.");
			adjuster.NewContents = "pus yalola nihimbilira. nihimbilira pus yalola.sup layalo ranihimbili. Another sentence.";
			adjuster.ChangeMaker = text =>
			{
				var para0 = text[0];
				var para1 = text[1];
				text.Cache.DomainDataByFlid.MoveString(para1.Hvo, StTxtParaTags.kflidContents, 0,
													   0, para1.Contents.Length,
													   para0.Hvo, StTxtParaTags.kflidContents, 0, para0.Contents.Length, false);
				text.ParagraphsOS.RemoveAt(1);
			};
			adjuster.RunTest();
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public virtual void DeleteParagraphBreakMovingMultipleSegsAndMerging()
		{
			var adjuster = new AdjustmentVerifier(Cache)
			{
				MatchingInitialSegments = 1,
				MatchingFinalSegments = 1,
				ShouldMergeSegmentTN = true,
				MatchingInitialAnalyses = 3,
				MatchingFinalAnalyses = 4,
			};
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola ";
			adjuster.AddOldParagraph("sup layalo ranihimbili. Another sentence.");
			adjuster.NewContents = "pus yalola nihimbilira. nihimbilira pus yalola sup layalo ranihimbili. Another sentence.";
			adjuster.ChangeMaker = text =>
			{
				var para0 = text[0];
				var para1 = text[1];
				text.Cache.DomainDataByFlid.MoveString(para1.Hvo, StTxtParaTags.kflidContents, 0,
													   0, para1.Contents.Length,
													   para0.Hvo, StTxtParaTags.kflidContents, 0, para0.Contents.Length, false);
				text.ParagraphsOS.RemoveAt(1);
			};
			adjuster.RunTest();
		}

		/// <summary>
		///
		/// </summary>
		[Test, Ignore("We don't currently support this case")]
		public virtual void DeleteParagraphBreakAndSomeSegsMovingMultipleSegsAndMerging()
		{
			var adjuster = new AdjustmentVerifier(Cache)
			{
				MatchingInitialSegments = 1,
				MatchingFinalSegments = 1,
				ShouldMergeSegmentTN = true,
				MatchingInitialAnalyses = 3,
				MatchingFinalAnalyses = 4,
			};
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola. ";
			adjuster.AddOldParagraph("sup. layalo. ranihimbili. Another sentence.");
			adjuster.NewContents = "pus yalola nihimbilira. nihimbilira pus yalola ranihimbili. Another sentence.";
			adjuster.ChangeMaker = text =>
			{
				var para0 = text[0];
				var para1 = text[1];
				text.Cache.DomainDataByFlid.MoveString(para1.Hvo, StTxtParaTags.kflidContents, 0,
													   13, para1.Contents.Length,
													   para0.Hvo, StTxtParaTags.kflidContents, 0, para0.Contents.Length, false);
				text.ParagraphsOS.RemoveAt(1);
			};
			adjuster.RunTest();
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public virtual void PasteParaAtStartOfPara()
		{
			var adjuster = new AdjustmentVerifier(Cache)
			{
				MatchingFinalParagraphs = 1,
			};
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola.";
			adjuster.NewContents = "New";
			adjuster.AddNewParagraph("pus yalola nihimbilira. nihimbilira pus yalola.");
			adjuster.ChangeMaker = text =>
			{
				var paraNew = text.InsertNewTextPara(0, null);
				paraNew.Contents = paraNew.Cache.TsStrFactory.MakeString("New", paraNew.Cache.DefaultVernWs);
			};
			adjuster.RunTest();
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public virtual void PasteParaPlusTextAtStartOfPara()
		{
			var adjuster = new AdjustmentVerifier(Cache)
			{
				MatchingFinalSegments = 1,
				MatchingFinalAnalyses = 4,
				MatchOneMoreFinalTN = true,
			};
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola.";
			adjuster.NewContents = "New";
			adjuster.AddNewParagraph("More new pus yalola nihimbilira. nihimbilira pus yalola.");
			adjuster.ChangeMaker = text =>
			{

				var para = text[0];
				var paraNew = text.InsertNewTextPara(0, null);
				paraNew.Contents = paraNew.Cache.TsStrFactory.MakeString("New", paraNew.Cache.DefaultVernWs);
				para.Contents = para.Contents.Insert(0,
													 para.Cache.TsStrFactory.MakeString("More new ",
																						para.Cache.DefaultVernWs));

			};
			adjuster.RunTest();
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public virtual void SplitTheSecondParagraph()
		{
			var adjuster = new AdjustmentVerifier(Cache)
			{
				MatchingInitialParagraphs = 1,
				MatchingFinalAnalyses = 3,
				MatchingInitialAnalyses = 5,
				MatchOneMoreInitialTN = true,
			};
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola.";
			adjuster.AddOldParagraph("This paragraph will be split in two.");
			adjuster.NewContents = "pus yalola nihimbilira. nihimbilira pus yalola.";
			string firstHalfOfSplit = "This paragraph will be split";
			adjuster.AddNewParagraph(firstHalfOfSplit);
			string secondHalfOfSplit = " in two.";
			adjuster.AddNewParagraph(secondHalfOfSplit);
			adjuster.ChangeMaker = text =>
			{

				var para = text[1];
				var paraNew = text.InsertNewTextPara(2, null);
				text.Cache.DomainDataByFlid.MoveString(para.Hvo, StTxtParaTags.kflidContents, 0,
					firstHalfOfSplit.Length, para.Contents.Length,
					paraNew.Hvo, StTxtParaTags.kflidContents, 0, 0, true);
			};
			adjuster.RunTest();
		}
		#endregion

		#region Insert new line tests
		/// <summary>
		///
		/// </summary>
		[Test]
		public virtual void InsertNewlineInWord()
		{
			var adjuster = new AdjustmentVerifier(Cache)
			{
				MatchingInitialSegments = 1,
				MatchingFinalAnalyses = 1,
				MatchingInitialAnalyses = 2,
				MatchOneMoreInitialTN = true,
			};
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola.";
			string firstHalfOfSplit = "pus yalola nihimbilira. nihimbilira pus yal";
			adjuster.NewContents = firstHalfOfSplit;
			string secondHalfOfSplit = "ola.";
			adjuster.AddNewParagraph(secondHalfOfSplit);
			adjuster.ChangeMaker = text =>
			{

				var para = text[0];
				var paraNew = text.InsertNewTextPara(1, null);
				text.Cache.DomainDataByFlid.MoveString(para.Hvo, StTxtParaTags.kflidContents, 0,
					firstHalfOfSplit.Length, para.Contents.Length,
					paraNew.Hvo, StTxtParaTags.kflidContents, 0, 0, true);
			};
			adjuster.RunTest();
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public virtual void InsertNewlineMovingWordsAndSegs()
		{
			var adjuster = new AdjustmentVerifier(Cache)
			{
				MatchingInitialAnalyses = 1,
				MatchingFinalSegments = 1,
				MatchingFinalAnalyses = 3,
				MatchOneMoreInitialTN = true,
			};
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola.";
			string firstHalfOfSplit = "pus ";
			adjuster.NewContents = firstHalfOfSplit;
			string secondHalfOfSplit = "yalola nihimbilira. nihimbilira pus yalola.";
			adjuster.AddNewParagraph(secondHalfOfSplit);
			adjuster.ChangeMaker = text =>
			{

				var para = text[0];
				var paraNew = text.InsertNewTextPara(1, null);
				text.Cache.DomainDataByFlid.MoveString(para.Hvo, StTxtParaTags.kflidContents, 0,
					firstHalfOfSplit.Length, para.Contents.Length,
					paraNew.Hvo, StTxtParaTags.kflidContents, 0, 0, true);
			};
			adjuster.RunTest();
		}

		/// <summary>
		/// Test MoveString in the case where the source paragraph has not been analyzed into segments at all.
		/// This is unusual in the current system because the analysis adjuster runs whenever the contents is set.
		/// But it can happen if the source has been imported from an older version (e.g., in Notebook).
		/// </summary>
		[Test]
		public void MoveSentencesFromCompletelyUnanalyzedPara()
		{
			var text = Cache.ServiceLocator.GetInstance<ITextFactory>().Create();
			//Cache.LangProject.TextsOC.Add(text);
			var stText = Cache.ServiceLocator.GetInstance<IStTextFactory>().Create();
			text.ContentsOA = stText;
			var para = Cache.ServiceLocator.GetInstance<IStTxtParaFactory>().Create();
			stText.ParagraphsOS.Add(para);
			para.Contents = Cache.TsStrFactory.MakeString("pus yalola nihimbilira. nihimbilira pus yalola.", Cache.DefaultVernWs);
			var paraNew = stText.InsertNewTextPara(1, null);
			para.SegmentsOS.Clear(); // get rid of analysis generated by side effects.
			text.Cache.DomainDataByFlid.MoveString(para.Hvo, StTxtParaTags.kflidContents, 0,
				"pus ".Length, para.Contents.Length,
				paraNew.Hvo, StTxtParaTags.kflidContents, 0, 0, true);
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public virtual void InsertNewlineMovingOnlyPunctuation()
		{
			var adjuster = new AdjustmentVerifier(Cache)
			{
				MatchingInitialSegments = 1,
				MatchingInitialAnalyses = 3,
				MatchingFinalAnalyses = 1,
				MatchOneMoreInitialTN = true,
			};
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola.";
			string firstHalfOfSplit = "pus yalola nihimbilira. nihimbilira pus yalola";
			adjuster.NewContents = firstHalfOfSplit;
			string secondHalfOfSplit = ".";
			adjuster.AddNewParagraph(secondHalfOfSplit);
			adjuster.ChangeMaker = text =>
			{

				var para = text[0];
				var paraNew = text.InsertNewTextPara(1, null);
				text.Cache.DomainDataByFlid.MoveString(para.Hvo, StTxtParaTags.kflidContents, 0,
					firstHalfOfSplit.Length, para.Contents.Length,
					paraNew.Hvo, StTxtParaTags.kflidContents, 0, 0, true);
			};
			adjuster.RunTest();
		}
		#endregion

		///// <summary>
		/////
		///// WANTTESTPORT (FLEx) A higher-level test, more an integration one...do we still need this??
		///// </summary>
		//[Test, Ignore]
		//public virtual void LT7841_UndoDeleteParagraphBreakResultsInRefresh()
		//{
		//}

		#region Sentence replace tests
		/// <summary>
		///
		/// </summary>
		[Test]
		public virtual void ReplaceSentenceWithSameSizeSentence()
		{
			var adjuster = new BothWaysAdjustmentVerifier(Cache)
			{
				MatchingFinalSegments = 2,
				MatchOneMoreInitialTN = true,
			};
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.NewContents = "tus ralola qihimbilirp. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.RunTest();
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public virtual void ReplaceSentenceWithSentences()
		{
			var adjuster = new AdjustmentVerifier(Cache)
			{
				MatchingFinalSegments = 2,
				MatchOneMoreInitialTN = true, //TNs Para0Seg0 ends up on Para0Seg0 vs. Para0Seg2
			};
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.NewContents = "tus. ralola. qihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.RunTest();
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public virtual void ReplaceSentenceWithSentences_SameLength()
		{
			var adjuster = new AdjustmentVerifier(Cache)
			{
				MatchingFinalSegments = 2,
				MatchOneMoreInitialTN = true, //TNs Para0Seg0 ends up on Para0Seg0 vs. Para0Seg2
			};
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "puus yalolla nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.NewContents = "teg. moerna. squestering. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.RunTest();
		}

		/// <summary>
		/// This is the reverse of the previous test, but we can't do it with the BothWays thing, because there is
		/// no way to predict that the whole of the first segment is deleted.
		/// </summary>
		[Test]
		public virtual void ReplaceSentencesWithSentence()
		{
			var adjuster = new AdjustmentVerifier(Cache)
			{
				MatchingFinalSegments = 2,
				MatchOneMoreFinalTN = true, //TN Para0Seg2 survives on Para0Seg0
			};
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "tus. ralola. qihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.NewContents = "pus yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.RunTest();
		}

		/// <summary>
		/// This is the reverse of the previous test, but we can't do it with the BothWays thing, because there is
		/// no way to predict that the whole of the first segment is deleted.
		/// </summary>
		[Test]
		public virtual void ReplaceSentencesWithSentenceKeepingFirstWord()
		{
			var adjuster = new AdjustmentVerifier(Cache)
			{
				MatchingFinalSegments = 2,
				//TN results:
				// Para0Seg0 ends up on newPara0Seg0
				// Para0Seg1 is deleted
				// Para0Seg2 and Para0Seg3 end up concatenated and on  newPara0Seg1 bug
				//	   should only have Para0Seg3 end up on  newPara0Seg1
				// Para0Seg4 ends up on newPara0Seg2 correct!!
				MatchingInitialAnalyses = 1,
				MatchingFinalAnalyses = 1,
				ShouldMergeSegmentTN = true,
			};
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus. ralola. qihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.NewContents = "pus yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.RunTest();
		}

		/// <summary>
		/// This is the reverse of the previous test, but we can't do it with the BothWays thing, because there is
		/// no way to predict that the whole of the first segment is deleted.
		/// </summary>
		[Test]
		public virtual void ReplaceSentencesWithSentencesChangingResultingStringLength()
		{
			var adjuster = new AdjustmentVerifier(Cache)
			{
				MatchingFinalSegments = 2,
				MatchingInitialAnalyses = 1,
				MatchingFinalAnalyses = 1,
				MatchOneMoreInitialTN = true,
				MatchOneMoreFinalTN = true,
			};
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus. ralola. qihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.NewContents = "pus to. alata. nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.RunTest();
		}

		/// <summary>
		/// This addresses one of the comments in LT-5369.
		/// </summary>
		[Test]
		public virtual void InsertSentencesAfterFirstSentence()
		{
			var adjuster = new BothWaysAdjustmentVerifier(Cache)
			{
				MatchingFinalSegments = 2,
				MatchingInitialSegments = 1,
			};
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.NewContents = "pus yalola nihimbilira. First new. Second new. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.RunTest();
		}
		#endregion

		#region Tests involving TextTags

		/// <summary>
		/// Tests inserting a word in a segment that has TextTags
		/// </summary>
		[Test]
		public virtual void WithTags_InsertWordInternal()
		{
			var adjuster = new AdjustmentVerifierPlusTags(Cache);
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.NewContents = "pus yalola pus nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			const int i1stTag = 0;
			const int i2ndTag = 1;
			adjuster.m_oldTagSkels.Add(new TagSkeleton(0, 0, 0, i1stTag)); // 1st tag = "pus"
			adjuster.m_oldTagSkels.Add(new TagSkeleton(0, 1, 2, i2ndTag)); // 2nd tag = "yalola nihimbilira"
			adjuster.m_newTagSkels.Add(new TagSkeleton(0, 0, 0, i1stTag)); // 1st tag should be unaffected by later edit
			adjuster.m_newTagSkels.Add(new TagSkeleton(0, 1, 3, i2ndTag)); // 2nd tag gains a word; update EndAnalysisIndex
			adjuster.RunTest();
		}

		/// <summary>
		/// Tests deleting a word in a segment that has TextTags
		/// </summary>
		[Test]
		public virtual void WithTags_DeleteWordInternal()
		{
			var adjuster = new AdjustmentVerifierPlusTags(Cache);
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus yalola pus nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.NewContents = "pus yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			const int i1stTag = 1;
			adjuster.m_oldTagSkels.Add(new TagSkeleton(0, 1, 3, i1stTag)); // tag = "yalola pus nihimbilira"
			adjuster.m_newTagSkels.Add(new TagSkeleton(0, 1, 2, i1stTag)); // tag loses a word; update EndAnalysisIndex
			adjuster.RunTest();
		}

		/// <summary>
		/// Tests changing the 1st word in a Tag
		/// </summary>
		[Test]
		public virtual void WithTags_ChangeWordBeginning()
		{
			var adjuster = new AdjustmentVerifierPlusTags(Cache);
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.NewContents = "pus dalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			const int i1stTag = 0;
			adjuster.m_oldTagSkels.Add(new TagSkeleton(0, 1, 2, i1stTag));
			adjuster.m_newTagSkels.Add(new TagSkeleton(0, 1, 2, i1stTag)); // We just changed spelling on a word, tag unaffected
			adjuster.RunTest();
		}

		/// <summary>
		/// Tests changing the last word in a Tag
		/// </summary>
		[Test]
		public virtual void WithTags_ChangeWordEnding()
		{
			var adjuster = new AdjustmentVerifierPlusTags(Cache);
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.NewContents = "pus yalola nihimbilid. nihimbilira pus yalola. hesyla nihimbilira.";
			const int i1stTag = 0;
			adjuster.m_oldTagSkels.Add(new TagSkeleton(0, 1, 2, i1stTag));
			adjuster.m_newTagSkels.Add(new TagSkeleton(0, 1, 2, i1stTag)); // We just changed spelling on a word, tag unaffected
			adjuster.RunTest();
		}

		/// <summary>
		/// Tests inserting a word before a Tag.
		/// Tag should be shifted to follow its wordforms.
		/// </summary>
		[Test]
		public virtual void WithTags_InsertWordBefore()
		{
			var adjuster = new AdjustmentVerifierPlusTags(Cache);
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.NewContents = "pus hesyla yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			const int i1stTag = 0;
			adjuster.m_oldTagSkels.Add(new TagSkeleton(0, 1, 2, i1stTag)); // tag = "yalola nihimbilira" in 1st Seg
			adjuster.m_newTagSkels.Add(new TagSkeleton(0, 2, 3, i1stTag)); // Needs to update AnalysisIndex (beg/end) in tag
			adjuster.RunTest();
		}

		/// <summary>
		/// Tests deleting a word before a Tag.
		/// Tag should be shifted to follow its wordforms.
		/// </summary>
		[Test]
		public virtual void WithTags_DeleteWordBefore()
		{
			var adjuster = new AdjustmentVerifierPlusTags(Cache);
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus hesyla yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.NewContents = "pus yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			const int i1stTag = 0;
			adjuster.m_oldTagSkels.Add(new TagSkeleton(0, 2, 3, i1stTag)); // tag = "yalola nihimbilira" in 1st Seg
			adjuster.m_newTagSkels.Add(new TagSkeleton(0, 1, 2, i1stTag)); // Needs to update AnalysisIndex (beg/end) in tag
			adjuster.RunTest();
		}

		/// <summary>
		/// Tests deleting a word after a Tag.
		/// Tag should be unaffected.
		/// </summary>
		[Test]
		public virtual void WithTags_DeleteWordAfter()
		{
			var adjuster = new AdjustmentVerifierPlusTags(Cache);
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus yalola nihimbilira hesyla. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.NewContents = "pus yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			const int i1stTag = 0;
			adjuster.m_oldTagSkels.Add(new TagSkeleton(0, 1, 2, i1stTag)); // tag = "yalola nihimbilira" in 1st Seg
			adjuster.m_newTagSkels.Add(new TagSkeleton(0, 1, 2, i1stTag)); // tag should be unaffected by later edit
			adjuster.RunTest();
		}

		/// <summary>
		/// Tests inserting a word after a Tag.
		/// Tag should be unaffected.
		/// </summary>
		[Test]
		public virtual void WithTags_InsertWordAfter()
		{
			var adjuster = new AdjustmentVerifierPlusTags(Cache);
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.NewContents = "pus yalola nihimbilira hesyla. nihimbilira pus yalola. hesyla nihimbilira.";
			const int i1stTag = 0;
			adjuster.m_oldTagSkels.Add(new TagSkeleton(0, 1, 2, i1stTag)); // tag in 1st Seg ends before edit
			adjuster.m_newTagSkels.Add(new TagSkeleton(0, 1, 2, i1stTag)); // tag should be unaffected
			adjuster.RunTest();
		}

		/// <summary>
		/// Tests inserting a newline in a word that is one of several grouped in a tag.
		/// Also tests tagging multiple segments. Since only 1/2 of the word goes to the
		/// new paragraph, we'll have the tag stay with the part that is left behind.
		/// </summary>
		[Test]
		public virtual void WithTags_InsertNewlineInWord()
		{
			var adjuster = new AdjustmentVerifierPlusTags(Cache);
			//								  1		 2		 3		 4		 5		 6
			//					  0123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola.";
			var firstHalfOfSplit = "pus yalola nihimbilira. nihimbilira pus yal";
			adjuster.NewContents = firstHalfOfSplit;
			var secondHalfOfSplit = "ola.";
			adjuster.AddNewParagraph(secondHalfOfSplit);
			adjuster.ChangeMaker = text =>
									{
										var para = text[0];
										var paraNew = text.InsertNewTextPara(1, null);
										text.Cache.DomainDataByFlid.MoveString(para.Hvo, StTxtParaTags.kflidContents, 0,
																			   firstHalfOfSplit.Length, para.Contents.Length,
																			   paraNew.Hvo, StTxtParaTags.kflidContents, 0, 0, true);
									};
			const int i1stTag = 2;
			adjuster.m_oldTagSkels.Add(new TagSkeleton(0, 1, 1, 2, i1stTag)); // tag covers all but first word (2 sentences)
			adjuster.m_newTagSkels.Add(new TagSkeleton(0, 1, 1, 2, i1stTag)); // tag NOT split over 2 paras!
			adjuster.RunTest();
		}

		/// <summary>
		/// Tests deleting a newline/paragraph break (and punctuation) where a tag exists over the break.
		/// Also tests tagging multiple segments.
		/// </summary>
		[Test]
		public virtual void WithTags_DeleteParagraphBreakMergingSegments()
		{
			var adjuster = new AdjustmentVerifierPlusTags(Cache);
			//								  1		 2		 3		 4		 5		 6		 7
			//					  01234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola.";
			adjuster.AddOldParagraph("sup layalo ranihimbili.");
			adjuster.NewContents = "pus yalola nihimbilira. nihimbilira pus yalola. sup layalo ranihimbili.";
			adjuster.ChangeMaker = text =>
				{
					var para0 = text[0];
					var para1 = text[1];
					text.Cache.DomainDataByFlid.MoveString(para1.Hvo, StTxtParaTags.kflidContents, 0,
						0, para1.Contents.Length, para0.Hvo, StTxtParaTags.kflidContents, 0, para0.Contents.Length, false);
					text.ParagraphsOS.RemoveAt(1);
				};
			const int i1stTag = 2;
			// old tag covers last two words of 1stpara and part of 2nd "pus yalola. sup layalo"
			adjuster.m_oldTagSkels.Add(new TagSkeleton(0, 1, 1, 0, 1, 1, i1stTag));
			adjuster.m_newTagSkels.Add(new TagSkeleton(1, 2, 1, 1, i1stTag)); // same tag, but NOT split over 2 paras!
			adjuster.RunTest();
		}

		/// <summary>
		/// Tests inserting a newline to break a paragraph where a tag exists over most
		/// of the paragraph. Also tests tagging multiple segments and multiple paragraphs.
		/// </summary>
		[Test]
		public virtual void WithTags_InsertNewlineBeforeWord()
		{
			var adjuster = new AdjustmentVerifierPlusTags(Cache);
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola.";
			var firstHalfOfSplit = "pus yalola nihimbilira. nihimbilira pus";
			adjuster.NewContents = firstHalfOfSplit;
			var secondHalfOfSplit = "yalola.";
			adjuster.AddNewParagraph(secondHalfOfSplit);
			adjuster.ChangeMaker = text =>
				{
					var para = text[0];
					var paraNew = text.InsertNewTextPara(1, null);
					text.Cache.DomainDataByFlid.MoveString(para.Hvo, StTxtParaTags.kflidContents, 0,
														   firstHalfOfSplit.Length, para.Contents.Length,
														   paraNew.Hvo, StTxtParaTags.kflidContents, 0, 0, true);
				};
			const int i1stTag = 2;
			adjuster.m_oldTagSkels.Add(new TagSkeleton(0, 1, 1, 2, i1stTag)); // tag covers all but first word (2 sentences)
			adjuster.m_newTagSkels.Add(new TagSkeleton(0, 1, 0, 0, 1, 0, i1stTag)); // tag split over 2 paras!
			adjuster.RunTest();
		}

		/// <summary>
		/// Tests replacing a segment break (period) with a non-segment break (comma).
		/// In this case, a tag covers that break and will need adjusting.
		/// Also tests tagging multiple segments.
		/// </summary>
		[Test]
		public virtual void WithTags_ReplaceSegBreakNonSegBreak()
		{
			var adjuster = new AdjustmentVerifierPlusTags(Cache);
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.NewContents = "pus yalola nihimbilira, nihimbilira pus yalola. hesyla nihimbilira.";
			const int i1stTag = 2;
			const int i2ndTag = 0;
			adjuster.m_oldTagSkels.Add(new TagSkeleton(0, 1, 2, 0, i1stTag)); // 1st tag spans 2 segs (nihimbilira X 2)
			adjuster.m_oldTagSkels.Add(new TagSkeleton(2, 0, 1, i2ndTag)); // 2nd tag labels 3rd seg's words
			adjuster.m_newTagSkels.Add(new TagSkeleton(0, 2, 4, i1stTag)); // 1st tag is now within 1st seg (3 is PunctForm!)
			adjuster.m_newTagSkels.Add(new TagSkeleton(1, 0, 1, i2ndTag)); // 2nd tag is unaffected (Seg's index changes)
			adjuster.RunTest();
		}

		/// <summary>
		/// Tests replacing a segment break (period) with a non-segment break (comma).
		/// In this case, a tag spans the segment break after and will need adjusting.
		/// Also tests tagging multiple segments.
		/// </summary>
		[Test]
		public virtual void WithTags_ReplaceSegBreakNonSegBreakSpanEnd()
		{
			var adjuster = new AdjustmentVerifierPlusTags(Cache);
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.NewContents = "pus yalola nihimbilira, nihimbilira pus yalola. hesyla nihimbilira.";
			const int i1stTag = 2;
			adjuster.m_oldTagSkels.Add(new TagSkeleton(1, 2, 2, 0, i1stTag)); // tag spans 2 segs (yalola. hesyla)
			adjuster.m_newTagSkels.Add(new TagSkeleton(0, 1, 6, 0, i1stTag)); // 1st tag is now within 1st seg (3 is PunctForm!)
			adjuster.RunTest();
		}

		/// <summary>
		/// Tests deleting a segment altogether.
		/// Several tag cases are covered, including a tag covers that break and will need adjusting.
		/// Also tests tagging multiple segments.
		/// </summary>
		[Test]
		public virtual void WithTags_DeleteSegment()
		{
			var adjuster = new AdjustmentVerifierPlusTags(Cache);
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.NewContents = "pus yalola nihimbilira. hesyla nihimbilira.";
			const int i1stTag = 0;
			const int i2ndTag = 1;
			const int i3rdTag = 2;
			adjuster.m_oldTagSkels.Add(new TagSkeleton(0, 0, 1, i1stTag)); // 1st tag labels NPhrase in 1st seg
			adjuster.m_oldTagSkels.Add(new TagSkeleton(0, 1, 2, 0, i2ndTag)); // 2nd tag spans 2 segs (nihimbilira X 2)
			adjuster.m_oldTagSkels.Add(new TagSkeleton(2, 0, 0, i3rdTag)); // 3rd tag on one word in 3rd seg (hesyla)
			adjuster.m_newTagSkels.Add(new TagSkeleton(0, 0, 1, i1stTag)); // 1st tag is unaffected
			adjuster.m_newTagSkels.Add(new TagSkeleton(0, 2, 2, i2ndTag)); // 2nd tag moves its endPoint back to first word
			adjuster.m_newTagSkels.Add(new TagSkeleton(1, 0, 0, i3rdTag)); // 3rd tag is unaffected (hesyla) (Seg's index changes)
			adjuster.RunTest();
		}

		/// <summary>
		/// Tests deleting a segment altogether.
		/// Tests a case where a tag completely covers that segment (multi-segment tag) and needs
		/// its end segment index adjusted.
		/// </summary>
		[Test]
		public virtual void WithTags_DeleteSegment_CoveringTag()
		{
			var adjuster = new AdjustmentVerifierPlusTags(Cache);
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.NewContents = "pus yalola nihimbilira. hesyla nihimbilira.";
			const int i1stTag = 1;
			adjuster.m_oldTagSkels.Add(new TagSkeleton(0, 2, 1, 0, i1stTag)); // tag covers more than deleted seg
			adjuster.m_newTagSkels.Add(new TagSkeleton(0, 1, 1, 0, i1stTag)); // tag is unaffected (INDEX of EndSegment changes)
			adjuster.RunTest();
		}

		/// <summary>
		/// Tests deleting a segment altogether.
		/// Tests a case where a tag is within the deleted segment and needs to be deleted itself.
		/// </summary>
		[Test]
		public virtual void WithTags_DeleteSegment_InternalTag()
		{
			var adjuster = new AdjustmentVerifierPlusTags(Cache);
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.NewContents = "pus yalola nihimbilira. hesyla nihimbilira.";
			const int i1stTag = 1;
			adjuster.m_oldTagSkels.Add(new TagSkeleton(1, 1, 2, i1stTag)); // tag covers NP within deleted seg
			//adjuster.NewTagSkels.Add(new TagSkeleton(0, 1, 1, 0, i1stTag)); // tag should be deleted!
			adjuster.RunTest();
		}

		/// <summary>
		/// Tests deleting a segment altogether.
		/// This test covers the case where the first part of the tag is in the deleted segment and needs adjusting.
		/// </summary>
		[Test]
		public virtual void WithTags_DeleteSegment_1stPartOfTag()
		{
			var adjuster = new AdjustmentVerifierPlusTags(Cache);
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.NewContents = "pus yalola nihimbilira. hesyla nihimbilira.";
			const int i2ndTag = 1;
			adjuster.m_oldTagSkels.Add(new TagSkeleton(1, 2, 0, 0, i2ndTag)); // tag spans 2nd seg plus a word
			adjuster.m_newTagSkels.Add(new TagSkeleton(1, 0, 0, i2ndTag)); // tag is now on one word in new 2nd seg (hesyla)
			adjuster.RunTest();
		}

		/// <summary>
		/// Tests modifying a segment when there are tags in other surrounding segments.
		/// Tags should be unaffected (although the TagSkeletons used in the text may have
		/// different indices).
		/// </summary>
		[Test]
		public virtual void WithTags_ModInDifferentSegment()
		{
			var adjuster = new AdjustmentVerifierPlusTags(Cache);
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.NewContents = "pus yalola nihimbilira. hesyla nihimbilira.";
			const int i1stTag = 0;
			const int i2ndTag = 1;
			adjuster.m_oldTagSkels.Add(new TagSkeleton(0, 0, 1, i1stTag)); // 1st tag labels NPhrase in 1st seg
			adjuster.m_oldTagSkels.Add(new TagSkeleton(2, 0, 0, i2ndTag)); // 2nd tag on one word in 3rd seg (hesyla)
			adjuster.m_newTagSkels.Add(new TagSkeleton(0, 0, 1, i1stTag)); // 1st tag is unaffected
			adjuster.m_newTagSkels.Add(new TagSkeleton(1, 0, 0, i2ndTag)); // 2nd tag is unaffected (hesyla) (Seg's index changes)
			adjuster.RunTest();
		}

		#endregion

		#region Tests involving Chart cells

		/// <summary>
		/// Tests inserting a word in a segment that has Chart cells
		/// </summary>
		[Test]
		public virtual void WithChart_InsertWordInternal()
		{
			var adjuster = new AdjustmentVerifierPlusChartCells(Cache);
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.NewContents = "pus yalola pus nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			const int i1stRow = 0;
			const int i1stCol = 3;
			const int i2ndRow = 1;
			const int i2ndCol = 1;
			// update EndAnalysisIndex
			adjuster.m_oldCellSkels.Add(new CellSkeleton(0, 0, 0, i1stRow, i1stCol)); // 1st cell = "pus"
			adjuster.m_oldCellSkels.Add(new CellSkeleton(0, 1, 2, i2ndRow, i2ndCol)); // 2nd cell = "yalola nihimbilira"
			adjuster.m_newCellSkels.Add(new CellSkeleton(0, 0, 0, i1stRow, i1stCol)); // 1st cell should be unaffected by later edit
			adjuster.m_newCellSkels.Add(new CellSkeleton(0, 1, 3, i2ndRow, i2ndCol)); // 2nd cell gains a word
			adjuster.RunTest();
		}

		/// <summary>
		/// Tests deleting a word in a segment that has Chart cells
		/// </summary>
		[Test]
		public virtual void WithChart_DeleteWordInternal()
		{
			var adjuster = new AdjustmentVerifierPlusChartCells(Cache);
			adjuster.OldContents = "pus yalola pus nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.NewContents = "pus yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			const int i1stRow = 1;
			const int i1stCol = 1;
			// update EndAnalysisIndex
			adjuster.m_oldCellSkels.Add(new CellSkeleton(0, 1, 3, i1stRow, i1stCol)); // cell = "yalola pus nihimbilira"
			adjuster.m_newCellSkels.Add(new CellSkeleton(0, 1, 2, i1stRow, i1stCol)); // cell loses a word
			adjuster.RunTest();
		}

		/// <summary>
		/// Tests changing the 1st word in a Chart cell
		/// </summary>
		[Test]
		public virtual void WithChart_ChangeWordBeginning()
		{
			var adjuster = new AdjustmentVerifierPlusChartCells(Cache);
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.NewContents = "pus dalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			const int iRow = 0;
			const int iCol = 1;
			// We just changed spelling on a word, chart should be unaffected
			adjuster.m_oldCellSkels.Add(new CellSkeleton(0, 1, 2, iRow, iCol));
			adjuster.m_newCellSkels.Add(new CellSkeleton(0, 1, 2, iRow, iCol));
			adjuster.RunTest();
		}

		/// <summary>
		/// Tests changing the last word in a Chart cell
		/// </summary>
		[Test]
		public virtual void WithChart_ChangeWordEnding()
		{
			var adjuster = new AdjustmentVerifierPlusChartCells(Cache);
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.NewContents = "pus yalola nihimbilid. nihimbilira pus yalola. hesyla nihimbilira.";
			const int i1stRow = 0;
			const int i1stCol = 2;
			// We just changed spelling on a word, chart unaffected
			adjuster.m_oldCellSkels.Add(new CellSkeleton(0, 1, 2, i1stRow, i1stCol));
			adjuster.m_newCellSkels.Add(new CellSkeleton(0, 1, 2, i1stRow, i1stCol));
			adjuster.RunTest();
		}

		/// <summary>
		/// Tests inserting a word before a Chart cell.
		/// Chart cell should be shifted to follow its wordforms.
		/// </summary>
		[Test]
		public virtual void WithChart_InsertWordBefore()
		{
			var adjuster = new AdjustmentVerifierPlusChartCells(Cache);
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.NewContents = "pus hesyla yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			const int i1stRow = 0;
			const int i1stCol = 1;
			// cell = "yalola nihimbilira" in 1st Seg
			adjuster.m_oldCellSkels.Add(new CellSkeleton(0, 1, 2, i1stRow, i1stCol));
			// Needs to update AnalysisIndex (beg/end) in cell
			adjuster.m_newCellSkels.Add(new CellSkeleton(0, 2, 3, i1stRow, i1stCol));
			adjuster.RunTest();
		}

		/// <summary>
		/// Tests deleting a word before a Chart cell.
		/// Chart cell should be shifted to follow its wordforms.
		/// </summary>
		[Test]
		public virtual void WithChart_DeleteWordBefore()
		{
			var adjuster = new AdjustmentVerifierPlusChartCells(Cache);
			adjuster.OldContents = "pus hesyla yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.NewContents = "pus yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			const int i1stRow = 0;
			const int i1stCol = 1;
			// cell = "yalola nihimbilira" in 1st Seg
			adjuster.m_oldCellSkels.Add(new CellSkeleton(0, 2, 3, i1stRow, i1stCol));
			// Needs to update AnalysisIndex (beg/end) in cell
			adjuster.m_newCellSkels.Add(new CellSkeleton(0, 1, 2, i1stRow, i1stCol));
			adjuster.RunTest();
		}

		/// <summary>
		/// Tests deleting a word after a Chart cell.
		/// Chart cell should be unaffected.
		/// </summary>
		[Test]
		public virtual void WithChart_DeleteWordAfter()
		{
			var adjuster = new AdjustmentVerifierPlusChartCells(Cache);
			adjuster.OldContents = "pus yalola nihimbilira hesyla. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.NewContents = "pus yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			const int i1stRow = 0;
			const int i1stCol = 4;
			// cell = "yalola nihimbilira" in 1st Seg
			adjuster.m_oldCellSkels.Add(new CellSkeleton(0, 1, 2, i1stRow, i1stCol));
			// chart should be unaffected by later edit
			adjuster.m_newCellSkels.Add(new CellSkeleton(0, 1, 2, i1stRow, i1stCol));
			adjuster.RunTest();
		}

		/// <summary>
		/// Tests inserting a word after a Chart cell.
		/// Chart cell should be unaffected.
		/// </summary>
		[Test]
		public virtual void WithChart_InsertWordAfter()
		{
			var adjuster = new AdjustmentVerifierPlusChartCells(Cache);
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.NewContents = "pus yalola nihimbilira hesyla. nihimbilira pus yalola. hesyla nihimbilira.";
			const int i1stRow = 2;
			const int i1stCol = 1;
			// cell in 1st Seg ends before edit
			adjuster.m_oldCellSkels.Add(new CellSkeleton(0, 1, 2, i1stRow, i1stCol));
			// chart should be unaffected
			adjuster.m_newCellSkels.Add(new CellSkeleton(0, 1, 2, i1stRow, i1stCol));
			adjuster.RunTest();
		}

		/// <summary>
		/// Tests inserting a bunch of spaces into a 3 segment paragraph. (this crashed before the fix to LT-11049)
		/// The chart cell should be unaffected.
		/// </summary>
		[Test]
		public virtual void WithChart_InsertSpacesAsIfGuessWordBreaksCalled()
		{
			var adjuster = new AdjustmentVerifierPlusChartCells(Cache);
			//                                1         2         3         4         5         6         7         8         9
			//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
			adjuster.OldContents = "19-20Pakufa kwa Herodi, ntumwi wa Mulungu aonekera kuna Zuze mundoto ku Idjitu, mbampanga tenepa: " +
								   "\"Lamuka. Kwata mwana na mai wace ubwerera nawo ku dziko ya Izirayeli, tangwi afa ule akhafuna kupha mwana.\"";
			adjuster.NewContents = "19-20\u2008Pakufa k\u2008wa H\u2008er\u2008o\u2008di\u2008, n\u2008tumwi w\u2008a Mu\u2008lungu aoneke\u2008ra" +
								   " ku?na Z\u2008uze mu\u2008ndoto ku Idj\u2008i\u2008tu, m\u2008b\u2008ampanga t\u2008ene\u2008pa\u2008: " +
								   "\"L\u2008a\u2008mu\u2008ka\u2008. K\u2008wa\u2008t\u2008a m\u2008wana n\u2008a ma\u2008i wa\u2008ce u\u2008bwerera" +
								   " n\u2008awo ku dz\u2008iko y\u2008a I\u2008zi\u2008ra\u2008ye\u2008li\u2008, t\u2008a\u2008n\u2008gw\u2008i " +
								   "a\u2008f\u2008a ul\u2008e a\u2008kha\u2008funa ku\u2008ph\u2008a m\u2008wana\u2008.\"";
			// 1st cell spans 2 segs
			adjuster.m_oldCellSkels.Add(new CellSkeleton(0, 1, 6, 6, 0, 0));
			// 1st cell spans 2 segs
			adjuster.m_oldCellSkels.Add(new CellSkeleton(0, 1, 6, 6, 0, 0));
			adjuster.RunTest();
		}

		/// <summary>
		/// Tests inserting a newline in a word that is one of several grouped in a cell.
		/// Also tests putting multiple segments in a chart cell. Since only 1/2 of the word goes to the
		/// new paragraph, we'll have the cell stay with the part that is left behind.
		/// </summary>
		[Test]
		public virtual void WithChart_InsertNewlineInWord()
		{
			var adjuster = new AdjustmentVerifierPlusChartCells(Cache);
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola.";
			var firstHalfOfSplit = "pus yalola nihimbilira. nihimbilira pus yal";
			adjuster.NewContents = firstHalfOfSplit;
			var secondHalfOfSplit = "ola.";
			adjuster.AddNewParagraph(secondHalfOfSplit);
			adjuster.ChangeMaker = text =>
			{
				var para = text[0];
				var paraNew = text.InsertNewTextPara(1, null);
				text.Cache.DomainDataByFlid.MoveString(para.Hvo, StTxtParaTags.kflidContents, 0,
													   firstHalfOfSplit.Length, para.Contents.Length,
													   paraNew.Hvo, StTxtParaTags.kflidContents, 0, 0, true);
			};
			const int i1stRow = 2;
			const int i1stCol = 3;
			// cell covers all but first word (2 sentences)
			adjuster.m_oldCellSkels.Add(new CellSkeleton(0, 1, 1, 2, i1stRow, i1stCol));
			// cell NOT split over 2 paras!
			adjuster.m_newCellSkels.Add(new CellSkeleton(0, 1, 1, 2, i1stRow, i1stCol));
			adjuster.RunTest();
		}

		/// <summary>
		/// Tests deleting a newline/paragraph break (and punctuation) where a cell exists over the break.
		/// Also tests putting multiple segments in a cell.
		/// </summary>
		[Test]
		public virtual void WithChart_DeleteParagraphBreakMergingSegments()
		{
			var adjuster = new AdjustmentVerifierPlusChartCells(Cache);
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola.";
			adjuster.AddOldParagraph("sup layalo ranihimbili.");
			adjuster.NewContents = "pus yalola nihimbilira. nihimbilira pus yalola. sup layalo ranihimbili.";
			adjuster.ChangeMaker = text =>
			{
				var para0 = text[0];
				var para1 = text[1];
				text.Cache.DomainDataByFlid.MoveString(para1.Hvo, StTxtParaTags.kflidContents, 0,
					0, para1.Contents.Length, para0.Hvo, StTxtParaTags.kflidContents, 0, para0.Contents.Length, false);
				text.ParagraphsOS.RemoveAt(1);
			};
			const int i1stRow = 2;
			const int i1stCol = 0;
			// old cell covers last two words of 1stpara and part of 2nd "pus yalola. sup layalo"
			adjuster.m_oldCellSkels.Add(new CellSkeleton(0, 1, 1, 0, 1, 1, i1stRow, i1stCol));
			// same cell, but NOT split over 2 paras!
			adjuster.m_newCellSkels.Add(new CellSkeleton(1, 2, 1, 1, i1stRow, i1stCol));
			adjuster.RunTest();
		}

		/// <summary>
		/// Tests inserting a newline to break a paragraph where a cell exists over most
		/// of the paragraph.
		/// Also tests putting multiple segments and multiple paragraphs into a cell.
		/// </summary>
		[Test]
		public virtual void WithChart_InsertNewlineBeforeWord()
		{
			var adjuster = new AdjustmentVerifierPlusChartCells(Cache);
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola.";
			var firstHalfOfSplit = "pus yalola nihimbilira. nihimbilira pus";
			adjuster.NewContents = firstHalfOfSplit;
			var secondHalfOfSplit = "yalola.";
			adjuster.AddNewParagraph(secondHalfOfSplit);
			adjuster.ChangeMaker = text =>
			{
				var para = text[0];
				var paraNew = text.InsertNewTextPara(1, null);
				text.Cache.DomainDataByFlid.MoveString(para.Hvo, StTxtParaTags.kflidContents, 0,
													   firstHalfOfSplit.Length, para.Contents.Length,
													   paraNew.Hvo, StTxtParaTags.kflidContents, 0, 0, true);
			};
			const int i1stRow = 1;
			const int i1stCol = 3;
			// cell covers all but first word (2 sentences)
			adjuster.m_oldCellSkels.Add(new CellSkeleton(0, 1, 1, 2, i1stRow, i1stCol));
			// cell split over 2 paras!
			adjuster.m_newCellSkels.Add(new CellSkeleton(0, 1, 0, 0, 1, 0, i1stRow, i1stCol));
			adjuster.RunTest();
		}

		/// <summary>
		/// Tests replacing a segment break (period) with a non-segment break (comma).
		/// In this case, a cell covers that break and will need adjusting.
		/// Also tests putting multiple segments in a cell.
		/// </summary>
		[Test]
		public virtual void WithChart_ReplaceSegBreakNonSegBreak()
		{
			var adjuster = new AdjustmentVerifierPlusChartCells(Cache);
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.NewContents = "pus yalola nihimbilira, nihimbilira pus yalola. hesyla nihimbilira.";
			const int i1stRow = 0;
			const int i1stCol = 4;
			const int i2ndRow = 2;
			const int i2ndCol = 0;
			// 1st cell spans 2 segs (nihimbilira X 2)
			adjuster.m_oldCellSkels.Add(new CellSkeleton(0, 1, 2, 0, i1stRow, i1stCol));
			// 2nd cell labels 3rd seg's words
			adjuster.m_oldCellSkels.Add(new CellSkeleton(2, 0, 1, i2ndRow, i2ndCol));
			// 1st cell is now within 1st seg (3 is PunctForm!)
			adjuster.m_newCellSkels.Add(new CellSkeleton(0, 2, 4, i1stRow, i1stCol));
			// 2nd cell is unaffected (Seg's index changes)
			adjuster.m_newCellSkels.Add(new CellSkeleton(1, 0, 1, i2ndRow, i2ndCol));
			adjuster.RunTest();
		}

		/// <summary>
		/// Tests replacing a segment break (period) with a non-segment break (comma).
		/// In this case, a cell spans the segment break after and will need adjusting.
		/// Also tests putting multiple segments in a cell.
		/// </summary>
		[Test]
		public virtual void WithChart_ReplaceSegBreakNonSegBreakSpanEnd()
		{
			var adjuster = new AdjustmentVerifierPlusChartCells(Cache);
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.NewContents = "pus yalola nihimbilira, nihimbilira pus yalola. hesyla nihimbilira.";
			const int i1stRow = 0;
			const int i1stCol = 4;
			// cell spans 2 segs (yalola. hesyla)
			adjuster.m_oldCellSkels.Add(new CellSkeleton(1, 2, 2, 0, i1stRow, i1stCol));
			// 1st cell is now within 1st seg (3 is PunctForm!)
			adjuster.m_newCellSkels.Add(new CellSkeleton(0, 1, 6, 0, i1stRow, i1stCol));
			adjuster.RunTest();
		}

		/// <summary>
		/// Tests deleting a segment altogether.
		/// Several cell cases are covered, including a cell covers that break and will need adjusting.
		/// Also tests putting multiple segments in a cell.
		/// </summary>
		[Test]
		public virtual void WithChart_DeleteSegment()
		{
			var adjuster = new AdjustmentVerifierPlusChartCells(Cache);
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.NewContents = "pus yalola nihimbilira. hesyla nihimbilira.";
			const int i1stRow = 0;
			const int i1stCol = 4;
			const int i2ndRow = 0;
			const int i2ndCol = 4;
			const int i3rdRow = 1;
			const int i3rdCol = 4;
			// 1st cell labels NPhrase in 1st seg
			adjuster.m_oldCellSkels.Add(new CellSkeleton(0, 0, 1, i1stRow, i1stCol));
			// 2nd cell spans 2 segs (nihimbilira X 2)
			adjuster.m_oldCellSkels.Add(new CellSkeleton(0, 1, 2, 0, i2ndRow, i2ndCol));
			// 3rd cell on one word in 3rd seg (hesyla)
			adjuster.m_oldCellSkels.Add(new CellSkeleton(2, 0, 0, i3rdRow, i3rdCol));
			// 1st cell is unaffected
			adjuster.m_newCellSkels.Add(new CellSkeleton(0, 0, 1, i1stRow, i1stCol));
			// 2nd cell moves its endPoint back to first word
			adjuster.m_newCellSkels.Add(new CellSkeleton(0, 2, 2, i2ndRow, i2ndCol));
			// 3rd cell is unaffected (hesyla) (Seg's index changes)
			adjuster.m_newCellSkels.Add(new CellSkeleton(1, 0, 0, i3rdRow, i3rdCol));
			adjuster.RunTest();
		}

		/// <summary>
		/// Tests deleting a segment altogether.
		/// Tests a case where a cell completely covers that segment (multi-segment cell) and needs
		/// its end segment index adjusted.
		/// </summary>
		[Test]
		public virtual void WithChart_DeleteSegment_CoveringTag()
		{
			var adjuster = new AdjustmentVerifierPlusChartCells(Cache);
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.NewContents = "pus yalola nihimbilira. hesyla nihimbilira.";
			const int i1stRow = 0;
			const int i1stCol = 3;
			// cell covers more than deleted seg
			adjuster.m_oldCellSkels.Add(new CellSkeleton(0, 2, 1, 0, i1stRow, i1stCol));
			// cell is unaffected (INDEX of EndSegment changes)
			adjuster.m_newCellSkels.Add(new CellSkeleton(0, 1, 1, 0, i1stRow, i1stCol));
			adjuster.RunTest();
		}

		/// <summary>
		/// Tests deleting a segment altogether.
		/// Tests a case where a cell is within the deleted segment and needs to be deleted itself.
		/// </summary>
		[Test]
		public virtual void WithChart_DeleteSegment_InternalTag()
		{
			var adjuster = new AdjustmentVerifierPlusChartCells(Cache);
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.NewContents = "pus yalola nihimbilira. hesyla nihimbilira.";
			const int i1stRow = 0;
			const int i1stCol = 4;
			// cell covers NP within deleted seg
			adjuster.m_oldCellSkels.Add(new CellSkeleton(1, 1, 2, i1stRow, i1stCol));
			//adjuster.NewCellSkels.Add(...Nope! cell should be deleted! As well as the row we created.
			// But it'll only delete the one row, not multiple. So make i1stRow = 0.
			adjuster.RunTest();
		}

		/// <summary>
		/// Tests deleting a segment altogether.
		/// This test covers the case where the first part of the cell is in the deleted segment and needs adjusting.
		/// </summary>
		[Test]
		public virtual void WithChart_DeleteSegment_1stPartOfTag()
		{
			var adjuster = new AdjustmentVerifierPlusChartCells(Cache);
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.NewContents = "pus yalola nihimbilira. hesyla nihimbilira.";
			const int i2ndRow = 2;
			const int i1stCol = 4;
			// cell spans 2nd seg plus a word
			adjuster.m_oldCellSkels.Add(new CellSkeleton(1, 2, 0, 0, i2ndRow, i1stCol));
			// cell is now on one word in new 2nd seg (hesyla)
			adjuster.m_newCellSkels.Add(new CellSkeleton(1, 0, 0, i2ndRow, i1stCol));
			adjuster.RunTest();
		}

		/// <summary>
		/// Tests modifying a segment when there are cells in other surrounding segments.
		/// Cells should be unaffected (although the CellSkeletons used in the chart may have
		/// different indices).
		/// </summary>
		[Test]
		public virtual void WithChart_ModInDifferentSegment()
		{
			var adjuster = new AdjustmentVerifierPlusChartCells(Cache);
			adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
			adjuster.NewContents = "pus yalola nihimbilira. hesyla nihimbilira.";
			const int i1stRow = 0;
			const int i1stCol = 3;
			const int i2ndRow = 1;
			const int i2ndCol = 4;
			// 1st cell labels NPhrase in 1st seg
			adjuster.m_oldCellSkels.Add(new CellSkeleton(0, 0, 1, i1stRow, i1stCol));
			// 2nd cell on one word in 3rd seg (hesyla)
			adjuster.m_oldCellSkels.Add(new CellSkeleton(2, 0, 0, i2ndRow, i2ndCol));
			// 1st cell is unaffected
			adjuster.m_newCellSkels.Add(new CellSkeleton(0, 0, 1, i1stRow, i1stCol));
			// 2nd cell is unaffected (hesyla) (Seg's index changes)
			adjuster.m_newCellSkels.Add(new CellSkeleton(1, 0, 0, i2ndRow, i2ndCol));
			adjuster.RunTest();
		}

		/// <summary>
		/// Tests inserting a segment break (period) when there are cells in the enclosing segment.
		/// Cells should be unaffected (although the CellSkeletons used in the chart may have
		/// different indices).
		/// </summary>
		[Test]
		[Ignore("This needs to be fixed soon after Stable release 7.0.1")]
		public virtual void WithChart_InsertWordsAndSegmentPunctuation()
		{
			var adjuster = new AdjustmentVerifierPlusChartCells(Cache);
			adjuster.OldContents = "hesyla pus yalola, nihimbilira. hesyla nihimbilira.";
			adjuster.NewContents = "hesyla new segment. Oddness pus yalola, nihimbilira. hesyla nihimbilira.";
			const int i1stRow = 0;
			const int i1stCol = 0;
			const int i2ndRow = 1;
			const int i2ndCol = 1;

			// Old chart cells

			// 1st cell on first word in 1st seg (hesyla)
			adjuster.m_oldCellSkels.Add(new CellSkeleton(0, 0, 0, i1stRow, i1stCol));
			// 2nd cell on two words in 1st seg (pus yalola)
			adjuster.m_oldCellSkels.Add(new CellSkeleton(0, 1, 2, i1stRow, i2ndCol));
			// 1st cell in second row on last word in 1st seg (nihimbilira; skip comma)
			adjuster.m_oldCellSkels.Add(new CellSkeleton(0, 4, 4, i2ndRow, i1stCol));

			// New chart cells

			// 1st cell is expanded to include new text.
			adjuster.m_newCellSkels.Add(new CellSkeleton(0, 1, 0, 0, i1stRow, i1stCol));
			// GJM: The above is the best option, but it is feasible to leave the new text uncharted too.
			// Then it gets dealt with by the ChOrph routines and presented uncharted to the user
			// with green highlights to show possible insertion cells
			// In that case, the test should use this next line instead:
			//adjuster.m_newCellSkels.Add(new CellSkeleton(0, 0, 0, i1stRow, i1stCol));

			// 2nd cell is unaffected (pus yalola) (Seg's index changes, as well as some analysis indices)
			adjuster.m_newCellSkels.Add(new CellSkeleton(1,	1, 2, i1stRow, i2ndCol));
			// 1st cell in second row is largely unaffected (nihimbilira)
			// (Seg's index changes, not analysis indices, interestingly enough)
			adjuster.m_newCellSkels.Add(new CellSkeleton(1, 4, 4, i2ndRow, i1stCol));
			adjuster.RunTest();
		}

		#endregion
	}

	#region AdjustmentVerifier class
	/// <summary>
	/// Adjustment verifier is used to make a specified change to a text and verify the results.
	///
	/// The broad idea is to specify how to create an StText in a specified state, to specify a change
	/// that should be made to it and the expected resulting contents of the text. Also
	/// specified is a range of segments and analyses within the text that are permitted to be affected.
	///
	/// A text is originally stored as a sequence of StTxtParas, and thus both the original state and
	/// the changed state are specified by giving a list of TsStrings that should be contents of the text
	/// before and after the change we are adjusting for.
	///
	/// A text is anlyzed at two levels, first into 'segments' which typically are sentences, and then (possibly) into
	/// words (and punctuation chunks), each of which may have an analysis. The analysis of each item may be
	/// a wordform, WfiAnalysis, WfiGloss, or PunctuationForm. Thus, each segment has a sequence of Analyses, corresponding
	/// to words and punctuation, which may also be adjusted if present (when the segment is not completely replaced).
	/// In general, the code we are testing tries to preserve the translations and notes of any segments that are not
	/// completely replaced by the new text, and the analyses of any unchanged segments and any words at the start
	/// or end of modified segments.
	///
	/// The AdjustmentVerifier does the following:
	/// 1. Sets up an StText with the specified content and no analysis. Makes the change. Verifies
	/// that the result is as expected and also has no analysis.
	///
	/// 2. Again sets up the original text, this time with segments but no word-level analysis.
	/// Generates arbitrary free and literal translations and notes for each segment. Makes the change.
	/// Verifies that segments that should not have been changed were not.
	/// Makes a separate, new text with the expected output and generates segments for it from scratch;
	/// verifies that the resulting segment breaks are as expected. May run a supplied delegate to
	/// verify expectations regarding merged notes and translations.
	///
	/// 3. Again sets up the original text, this time analyzing it fully. Ensures there is at least one
	/// WfiAnalysis for each wordform used in the text, and changes the generated segments to refer to Analyses
	/// instead of Wordforms. Makes the change. Verifies that Analyses that should not be changed are not.
	/// Makes a new copy of the output text and parses it from scratch. Verifies that the resulting analysis
	/// refers to the same wordforms (possibly indirectly) and punctuation forms as the adjusted one.
	///
	/// The most typical usage is
	/// Set OldContents (string, default vern WS implied) or oldContentsTss; newContents or newContentsTss;
	/// modifiedSegment (or firstModifiedSegment/lastModifiedSegment), an index; firstModifiedAnalysis and
	/// lastModifiedAnalysis. The initial text contains a single paragraph with oldContents; the change is
	/// to replace it with newContents. Call RunTest(). (the identification of what should change is
	/// relative to the input text; what is actually verified is that the stuff outside that range is
	/// present unmodified in the output.
	///
	///		adjuster = new BothWaysAdjustmentVerifier(Cache)
	///		{
	///			MatchingInitialSegments = 1,
	///			MatchingFinalSegments = 1,
	///			MatchingFinalAnalyses = 2,
	///			MatchingInitialAnalyses = 1,
	///			MatchOneMoreInitialTN = true,
	///			MatchOneMoreFinalTN = true,
	///		};
	///		//                                1         2         3         4         5         6         7         8         9
	///		//                      0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
	///		adjuster.OldContents = "pus yalola nihimbilira. nihimbilira pus yalola. hesyla nihimbilira.";
	///		adjuster.NewContents = "pus yalola nihimbilira. nihimbilira sup dup lup yalola. hesyla nihimbilira.";
	///		adjuster.RunTest();
	///
	/// In the above, we are testing the effect of changing some text in the middle of a text that has one paragraph
	/// and three segments (because there are three sentences).
	/// One segment (the initial pus yalola nihimbilira) is identical in both old and new contents, so we
	/// set MatchingInitialSegments to 1 to indicate that in the first segment everything should match
	/// (the adjusted segment should have the same free translation, literal translation, notes, and analyses as
	/// before the change).
	/// Likewise, the last setence(hesyla nihimbilira.) is unchanged so the final segment should match exactly
	/// (MatchingFinalSegments = 1).
	/// Now for the middle sentence. There is one word at the start that is the same in old and new (the second
	/// nihimbilira), so in the segment that does not match entirely (the second), the first analysis should be
	/// the same before and after the adjustment (MatchingInitialAnalyses = 1). Likewise, the final "yalola" and period
	/// in that segment are unchanged, so the last two analysies should also be unchanged (MatchingFinalAnalyses = 2).
	///
	/// We expect that the adjustment will preserve the translation and notes on the middle segment, since the change
	/// leaves at least some of its content unchanged. Thus, in addition to the one initial and one final segment
	/// that are entirely unchanged, one more (counting from either end) should have translation and notes unchanged
	/// by the adjuster. This is indicated by MatchOneMoreInitialTN and MatchOneMoreFinalTN.
	///
	/// Variations:
	/// May set oldContentsParas, an array of TsStrings, if multiple paragraphs should be in the initial state;
	/// similarly newContentsParas, if the output is multiple.
	///
	/// May set firstModifiedPara/lastModifiedPara if either is non-zero.
	///
	/// May set changeDelegate, a function taking the input StText as argument, to control the change process in
	/// the most general way.
	///
	/// May call InsertNewline(ipara, ich) to simulate typing Return at that point.
	///
	/// May call MergeLines(ipara) to simulate deleting the line break between the indicated paragraph and the next.
	///
	/// May call Paste(iparaFirst, ichMin, iparaLast, ichLim, tss) to simulate pasting the specified TSS over
	/// the specified range.
	/// </summary>
	class AdjustmentVerifier
	{
		// These variables all describe what is expected in the process of comparing the text and analysis that results from
		// analyzing the original and then making the change (letting the analysis adjuster do its thing that we
		// want to test) with the text and analysis that results from applying the change and then analyzing the
		// resulting text from scratch. (We assume that analyzer is right.)
		/// <summary>Count of paragraphs at start that should match entirely.</summary>
		internal int MatchingInitialParagraphs;
		/// <summary>Count of paragraphs at end that should match entirely.</summary>
		internal int MatchingFinalParagraphs;
		/// <summary>Count of segments at start of para after matching initial paragraphs that should match exactly.</summary>
		internal int MatchingInitialSegments;
		/// <summary>Count of segments at end of para before matching final paragraphs that should match exactly</summary>
		internal int MatchingFinalSegments;
		/// <summary>One additional segment that doesn't have identical analyses should have identical Translation/Notes.
		/// One example of when this occurs is when a segment is split into two segments.</summary>
		internal bool MatchOneMoreInitialTN;
		/// <summary>One additional segment at the end that doesn't have identical analyses should have identical Translation/Notes.</summary>
		internal bool MatchOneMoreFinalTN;

		/// <summary>Count of analyses at start of segment matching initial segments in matching initial paragraph that should match</summary>
		internal int MatchingInitialAnalyses;
		/// <summary>Count of analyses at end of segment before matching final segments that should match</summary>
		internal int MatchingFinalAnalyses;

		/// <summary>true if output segment following matching initial segment (which might be in following para) should have merged
		/// Translation and/or Notes from para after matching initial segment in input and para before matching final segment in input.</summary>
		internal bool ShouldMergeSegmentTN;

		/// <summary>
		/// The specified words, from first to second in each tuple, which should not overlap, are grouped into phrases
		/// in the original. The first index specifies a segment, the other two a pair of wordforms in the segment.
		/// </summary>
		internal Tuple<int, int, int>[] Phrases { get; set; }

		/// <summary>By default the verifier assumes there is exactly one input and output paragraph and that the change
		/// we want to verify is replacing its old contents with its new contents.
		/// To handle some other kind of change set ChangeMaker to an action (usually a lambda expression), which
		/// makes the appropriate change to the text which is ChangeMaker's one argument.</summary>
		internal Action<IStText> ChangeMaker { get; set; }

		private FdoCache Cache { get; set; }

		protected IStText m_oldText;
		protected IStText m_newText;
		private IStTextFactory m_stTextFactory;
		private ITextFactory m_textFactory;
		private IStTxtParaFactory m_paraFactory;
		private ITsStrFactory m_tssFactory;
		private int m_wsEn, m_wsFr;

		//The list of analyses of the old text before we modified it.
		//The outer list has an item for each paragraph
		//which is a list that has an item for each segment
		//which is a list that has an item for each analysis of that segment.
		List<List<List<IAnalysis>>> m_oldAnalyses = new List<List<List<IAnalysis>>>();

		internal AdjustmentVerifier(FdoCache cache)
		{
			Cache = cache;
			m_stTextFactory = Cache.ServiceLocator.GetInstance<IStTextFactory>();
			m_textFactory = Cache.ServiceLocator.GetInstance<ITextFactory>();
			m_paraFactory = Cache.ServiceLocator.GetInstance<IStTxtParaFactory>();
			m_tssFactory = TsStrFactoryClass.Create();
			m_oldText = MakeStText();
			m_newText = MakeStText();
			m_wsFr = Cache.WritingSystemFactory.GetWsFromStr("fr");
			m_wsEn = Cache.WritingSystemFactory.GetWsFromStr("en");
		}

		internal string OldContents
		{
			set
			{
				SetOldContents(m_tssFactory.MakeString(value, Cache.DefaultVernWs));
			}
		}

		internal void SetOldContents(ITsString value)
		{
			OldFirstPara.Contents = value;
		}

		/// <summary>
		/// Should be the same paragraph that SetOldContents modifies.
		/// </summary>
		internal IStTxtPara OldFirstPara { get { return m_oldText[0]; } }

		internal string NewContents
		{
			set
			{
				SetNewContents(m_tssFactory.MakeString(value, Cache.DefaultVernWs));
			}
		}
		internal void SetNewContents(ITsString value)
		{
			m_newText[0].Contents = value;
		}

		internal void AddOldParagraph(String text)
		{
			var para = m_oldText.AddNewTextPara(null);
			para.Contents = m_tssFactory.MakeString(text, Cache.DefaultVernWs);
		}

		internal void AddNewParagraph(String text)
		{
			var para = m_newText.AddNewTextPara(null);
			para.Contents = m_tssFactory.MakeString(text, Cache.DefaultVernWs);
		}

		internal virtual void RunTest()
		{
			TestWithNoAnalysis();
			TestWithSegmentsOnly();
			TestWithFullAnalysis();
		}

		private void TestWithNoAnalysis()
		{
			var oldText = CopyBaseText(m_oldText);
			MakeTheTextChange(oldText, m_newText);
			VerifyBaseText(m_newText, oldText);
		}

		private void VerifyBaseText(IStText expectedText, IStText actualText)
		{
			Assert.AreEqual(expectedText.ParagraphsOS.Count, actualText.ParagraphsOS.Count, "The two texts should have the same number of paragraphs.");
			for (int i = 0; i < expectedText.ParagraphsOS.Count; i++)
			{
				Assert.IsTrue(expectedText[i].Contents.Equals(actualText[i].Contents),
					"Contents of paragraph " + i + " are not matching.");
			}
		}

		private void TestWithSegmentsOnly()
		{
			var oldText = CopyBaseText(m_oldText);
			var oldTextBeforeChanges = CopyBaseText(m_oldText);
			var newText = CopyBaseText(m_newText);
			//Break the text into segments.
			CreateSegments(oldText);
			CreateTranslationsAndNotes(oldText);
			CreateSegments(oldTextBeforeChanges);
			CreateTranslationsAndNotes(oldTextBeforeChanges);
			CreateSegments(newText);

			MakeTheTextChange(oldText, m_newText);

			VerifyBaseText(newText, oldText);
			VerifySegments(newText, oldText);
			VerifyTranslationsAndNotes(oldTextBeforeChanges, oldText);
		}

		private void VerifySegments(IStText expectedText, IStText actualText)
		{
			for (int i = 0; i < expectedText.ParagraphsOS.Count; i++)
			{
				var expectedPara = expectedText[i];
				var actualPara = actualText[i];
				Assert.AreEqual(expectedPara.SegmentsOS.Count,
								actualPara.SegmentsOS.Count, "Paragraph " + i + " has the wrong number of segments.");
				for (int j = 0; j < expectedPara.SegmentsOS.Count; j++)
				{
					ISegment expectedSeg = expectedPara.SegmentsOS[j];
					ISegment actualSeg = actualPara.SegmentsOS[j];
					Assert.AreEqual(expectedSeg.BeginOffset, actualSeg.BeginOffset,
						"Segment " + j + " of para " + i + " beginOffsets do not match.");
					Assert.AreEqual(expectedSeg.IsLabel, actualSeg.IsLabel);
				}
			}
		}

		/// <summary>
		/// Given the output of the change, and a text which contains a copy of the segments and their translations and notes
		/// before the change, see if we have the expected translations and notes on the output.
		/// - Unmodified segments should have unmodified translations and notes.
		/// - Typically, the first modified segment of the output should have the translations and notes of the first
		/// segment of the original that got modified.
		/// - Typically, the last modified segment of the output should have the translations and notes of the last
		/// segment of the original that got modified.
		/// - Special cases:
		///	 - If one of the flags FirstModifiedSegmentDeleted or LastModifiedSegmentDeleted is set, do NOT
		///	 expect to find its free translations.
		///	 - If there are no output modified segments, don't look for any TN.
		///	 - If there is only one output modified segment, look for a concatenation.
		/// </summary>
		private void VerifyTranslationsAndNotes(IStText oldTextBeforeChanges, IStText text)
		{
			int matchingInitialSegsTN = MatchingInitialSegments + (MatchOneMoreInitialTN ? 1 : 0);
			int matchingFinalSegsTN = MatchingFinalSegments + (MatchOneMoreFinalTN ? 1 : 0);
			IStTxtPara para, oldPara;
			// Verify that TN matches in initial identical paragraphs.
			for (int iPara = 0; iPara < MatchingInitialParagraphs; iPara++)
			{
				para = text[iPara];
				oldPara = oldTextBeforeChanges[iPara];
				for (int iSeg = 0; iSeg < para.SegmentsOS.Count; iSeg++)
				{
					var actualSeg = para.SegmentsOS[iSeg];
					var oldSeg = oldPara.SegmentsOS[iSeg];
					VerifyTranslationsAndNotesOfOneSegment(iPara, iSeg, oldSeg, actualSeg);
				}
			}
			// Verify analyses match in initial identical segments of para after MatchingInitialParagraphs.
			para = text[MatchingInitialParagraphs];
			oldPara = oldTextBeforeChanges[MatchingInitialParagraphs];
			for (int iSeg = 0; iSeg < matchingInitialSegsTN; iSeg++)
			{
				var actualSeg = para.SegmentsOS[iSeg];
				var oldSeg = oldPara.SegmentsOS[iSeg];
				VerifyTranslationsAndNotesOfOneSegment(MatchingInitialParagraphs, iSeg, oldSeg, actualSeg);
			}

			// Now do the final stuff.
			int paraDelta = text.ParagraphsOS.Count - oldTextBeforeChanges.ParagraphsOS.Count;
			// Verify analyses match in final identical paragraphs.
			for (int iPara = text.ParagraphsOS.Count - MatchingFinalParagraphs; iPara < text.ParagraphsOS.Count; iPara++)
			{
				para = text[iPara];
				oldPara = oldTextBeforeChanges[iPara - paraDelta];
				for (int iSeg = 0; iSeg < para.SegmentsOS.Count; iSeg++)
				{
					var actualSeg = para.SegmentsOS[iSeg];
					var oldSeg = oldPara.SegmentsOS[iSeg];
					VerifyTranslationsAndNotesOfOneSegment(iPara, iSeg, oldSeg, actualSeg);
				}
			}
			// Verify analyses match in final identical segments of paragraph before final identical paragraphs
			int iparaFinalSegmentsOutput = text.ParagraphsOS.Count - MatchingFinalParagraphs - 1;
			if (iparaFinalSegmentsOutput >= 0 && matchingFinalSegsTN > 0)
			{
				para = text[iparaFinalSegmentsOutput];
				oldPara = oldTextBeforeChanges[iparaFinalSegmentsOutput - paraDelta];
				int segDelta = para.SegmentsOS.Count - oldPara.SegmentsOS.Count;
				for (int iSeg = para.SegmentsOS.Count - matchingFinalSegsTN; iSeg < para.SegmentsOS.Count; iSeg++)
				{
					var actualSeg = para.SegmentsOS[iSeg];
					var oldSeg = oldPara.SegmentsOS[iSeg - segDelta];
					VerifyTranslationsAndNotesOfOneSegment(iparaFinalSegmentsOutput, iSeg, oldSeg, actualSeg);
				}
			}
			if (ShouldMergeSegmentTN)
			{
				oldPara = oldTextBeforeChanges[iparaFinalSegmentsOutput - paraDelta];
				int inputIndexOfFinalSegToMergeOrCheck = oldPara.SegmentsOS.Count - matchingFinalSegsTN - 1;
				// Expect concatenated translations and notes.
				var oldTextFirstSegToMergeTN = oldTextBeforeChanges[MatchingInitialParagraphs].SegmentsOS[matchingInitialSegsTN];
				var oldTextSecondSegToMergeTN = oldTextBeforeChanges[iparaFinalSegmentsOutput - paraDelta].SegmentsOS[inputIndexOfFinalSegToMergeOrCheck];
				var newTextSegWithMergedTN = text[MatchingInitialParagraphs].SegmentsOS[MatchingInitialSegments];

				VerifyConcatenatedTranslationsAndNotes(MatchingInitialParagraphs, MatchingInitialSegments,
					oldTextFirstSegToMergeTN, oldTextSecondSegToMergeTN, newTextSegWithMergedTN);
			}
			else //if ShouldMergeSegmentTN==true then are are no new segments or paragraphs inserted.
			{
				// Check there is no TN on wholly inserted paragraphs and segments
				int limParaIndexInput = text.ParagraphsOS.Count - MatchingFinalParagraphs;
				int iseg = matchingInitialSegsTN;
				int ipara = MatchingInitialParagraphs;
				for (; ipara < limParaIndexInput; )
				{
					var actualPara2 = text[ipara];
					if (ipara >= limParaIndexInput - 1 && iseg >= actualPara2.SegmentsOS.Count - matchingFinalSegsTN)
						break;
					if (iseg >= actualPara2.SegmentsOS.Count)
					{
						ipara++;
						iseg = 0;
						continue;
					}

					VerifyNoTranslationsAndNotes(ipara, iseg, actualPara2.SegmentsOS[iseg]);
					iseg++;
				}
			}
		}

		// Any segment with an index less than this should be duplicated completely unchanged in the output.
		// If no segments are modified it should be a large number.
		// Note that this does not take account of the possibility that something was inserted; a segment
		// in the output that has a larger index than anything in the input should not be checked, unless
		// it is after the change and is compared with a smaller index in the input.

		private void VerifyNoTranslationsAndNotes(int ipara, int iseg, ISegment actualSeg)
		{
			Assert.AreEqual(0, actualSeg.FreeTranslation.get_String(m_wsEn).Length,
						  "English Free Translation is not empty for para " + ipara + " segment " + iseg);
			Assert.AreEqual(0, actualSeg.FreeTranslation.get_String(m_wsFr).Length,
						  "French Free Translation is not empty for para " + ipara + " segment " + iseg);

			//Verify the LiteralTranslations match
			Assert.AreEqual(0, actualSeg.LiteralTranslation.get_String(m_wsEn).Length,
						  "English Literal Translation is not empty for para " + ipara + " segment " + iseg);
			Assert.AreEqual(0, actualSeg.LiteralTranslation.get_String(m_wsFr).Length,
						  "French Literal Translation is not empty for para " + ipara + " segment " + iseg);

			Assert.AreEqual(0, actualSeg.NotesOS.Count, "Wrong number of notes for para " + ipara + " segment " + iseg);
		}

		private void VerifyTranslationsAndNotesOfOneSegment(int ipara, int iseg, ISegment oldSeg, ISegment actualSeg)
		{
			Assert.IsTrue(oldSeg.FreeTranslation.get_String(m_wsEn).Equals(actualSeg.FreeTranslation.get_String(m_wsEn)),
						  "English Free Translation does not match for para " + ipara + " segment " + iseg + ". It is "
						  + actualSeg.FreeTranslation.get_String(m_wsEn).Text);
			Assert.IsTrue(oldSeg.FreeTranslation.get_String(m_wsFr).Equals(actualSeg.FreeTranslation.get_String(m_wsFr)),
						  "French Free Translation does not match for para " + ipara + " segment " + iseg);

			//Verify the LiteralTranslations match
			Assert.IsTrue(oldSeg.LiteralTranslation.get_String(m_wsEn).Equals(actualSeg.LiteralTranslation.get_String(m_wsEn)),
						  "English LiteralTranslation does not match for para " + ipara + " segment " + iseg);
			Assert.IsTrue(oldSeg.LiteralTranslation.get_String(m_wsFr).Equals(actualSeg.LiteralTranslation.get_String(m_wsFr)),
						  "French LiteralTranslation does not match for para " + ipara + " segment " + iseg);

			//Verify the Notes match
			Assert.AreEqual(oldSeg.NotesOS.Count, actualSeg.NotesOS.Count, "Wrong number of notes for para " + ipara + " segment " + iseg);
			for (int k = 0; k < oldSeg.NotesOS.Count; k++)
			{
				Assert.IsTrue(oldSeg.NotesOS[k].Content.get_String(m_wsEn).Equals(actualSeg.NotesOS[k].Content.get_String(m_wsEn)),
							  "English Note " + k + " does not match for para " + ipara + " segment " + iseg);
				Assert.IsTrue(oldSeg.NotesOS[k].Content.get_String(m_wsFr).Equals(actualSeg.NotesOS[k].Content.get_String(m_wsFr)),
							  "French Note " + k + " does not match for para " + ipara + " segment " + iseg);
			}
		}

		private void VerifyConcatenatedTranslationsAndNotes(int ipara, int iseg, ISegment oldSeg1, ISegment oldSeg2, ISegment actualSeg)
		{
			VerifyConcatMlStrings(oldSeg1.FreeTranslation, oldSeg2.FreeTranslation, actualSeg.FreeTranslation, m_wsEn,
				"English Free Translation does not match for para " + ipara + " segment " + iseg);
			VerifyConcatMlStrings(oldSeg1.FreeTranslation, oldSeg2.FreeTranslation, actualSeg.FreeTranslation, m_wsFr,
						  "French Free Translation does not match for para " + ipara + " segment " + iseg);

			//Verify the LiteralTranslations match
			VerifyConcatMlStrings(oldSeg1.LiteralTranslation, oldSeg2.LiteralTranslation, actualSeg.LiteralTranslation, m_wsEn,
						  "English LiteralTranslation does not match for para " + ipara + " segment " + iseg);
			VerifyConcatMlStrings(oldSeg1.LiteralTranslation, oldSeg2.LiteralTranslation, actualSeg.LiteralTranslation, m_wsFr,
						  "French LiteralTranslation does not match for para " + ipara + " segment " + iseg);

			//Verify the Notes match
			Assert.AreEqual(oldSeg1.NotesOS.Count + oldSeg2.NotesOS.Count, actualSeg.NotesOS.Count, "Wrong number of notes for para " + ipara + " segment " + iseg);
			for (int k = 0; k < oldSeg1.NotesOS.Count; k++)
			{
				Assert.IsTrue(oldSeg1.NotesOS[k].Content.get_String(m_wsEn).Equals(actualSeg.NotesOS[k].Content.get_String(m_wsEn)),
							  "English Note " + k + " does not match for para " + ipara + " segment " + iseg);
				Assert.IsTrue(oldSeg1.NotesOS[k].Content.get_String(m_wsFr).Equals(actualSeg.NotesOS[k].Content.get_String(m_wsFr)),
							  "French Note " + k + " does not match for para " + ipara + " segment " + iseg);
			}
			int offset = oldSeg1.NotesOS.Count;
			for (int k = 0; k < oldSeg2.NotesOS.Count; k++)
			{
				Assert.IsTrue(oldSeg2.NotesOS[k].Content.get_String(m_wsEn).Equals(actualSeg.NotesOS[k + offset].Content.get_String(m_wsEn)),
							  "English Note " + (k + offset) + " does not match for para " + ipara + " segment " + iseg);
				Assert.IsTrue(oldSeg2.NotesOS[k].Content.get_String(m_wsFr).Equals(actualSeg.NotesOS[k + offset].Content.get_String(m_wsFr)),
							  "French Note " + (k + offset) + " does not match for para " + ipara + " segment " + iseg);
			}
		}

		void VerifyConcatMlStrings(IMultiString first, IMultiString second, IMultiString actual, int ws, string label)
		{
			Assert.IsTrue(first.get_String(ws).
							  ConcatenateWithSpaceIfNeeded(second.get_String(ws)).
							  Equals(actual.get_String(ws)), label);
		}


		protected void CreateSegments(IStText text)
		{
			foreach (IStTxtPara para in text.ParagraphsOS)
			{
				SegmentServices.EnsureMainParaSegments(para);
			}
		}

		private void CreateTranslationsAndNotes(IStText text)
		{
			int ipara = 0;
			var noteFactory = Cache.ServiceLocator.GetInstance<INoteFactory>();
			foreach (IStTxtPara para in text.ParagraphsOS)
			{
				int iseg = 0;
				foreach (ISegment seg in para.SegmentsOS)
				{
					if (seg.IsLabel)
						continue;

					seg.FreeTranslation.set_String(m_wsEn,
						m_tssFactory.MakeString("English Translation of Para " + ipara + " Segment " + iseg + ". ", m_wsEn));
					seg.FreeTranslation.set_String(m_wsFr,
						m_tssFactory.MakeString("French Translation of Para " + ipara + " Segment " + iseg + ".", m_wsFr));

					seg.LiteralTranslation.set_String(m_wsEn,
						m_tssFactory.MakeString("English Literal Translation of Para " + ipara + " Segment " + iseg + ".", m_wsEn));
					seg.LiteralTranslation.set_String(m_wsFr,
						m_tssFactory.MakeString(" French Literal Translation of Para " + ipara + " Segment " + iseg + ".", m_wsFr));

					INote note = noteFactory.Create();
					seg.NotesOS.Add(note);
					note.Content.set_String(m_wsEn,
						m_tssFactory.MakeString("First English Note of Para " + ipara + " Segment " + iseg + ".", m_wsEn));
					note.Content.set_String(m_wsFr,
						m_tssFactory.MakeString("First French Note of Para " + ipara + " Segment " + iseg + ".", m_wsFr));

					note = noteFactory.Create();
					seg.NotesOS.Add(note);
					note.Content.set_String(m_wsFr,
						m_tssFactory.MakeString("Second French Note of Para " + ipara + " Segment " + iseg + ".", m_wsFr));

					iseg++;
				}
				ipara++;
			}
		}

		private void TestWithFullAnalysis()
		{
			var oldText = CopyBaseText(m_oldText);
			var newText = CopyBaseText(m_newText);
			var oldTextBeforeChanges = CopyBaseText(m_oldText);
			CreateSegments(oldTextBeforeChanges);
			CreateTranslationsAndNotes(oldTextBeforeChanges);
			//Break the text into segments and assign default (wordform) analyses.
			Parse(oldText);
			SetupPhrases(oldText, Phrases);

			ChangeWordformsToAnalyses(oldText);
			CreateTranslationsAndNotes(oldText);
			Parse(newText);
			// No longer needed because the required phrases are guessed once created in the SetupPhrases of the old text.
			//SetupPhrases(newText, ResultingPhrases);
			MakeTheTextChange(oldText, m_newText);

			VerifyBaseText(newText, oldText);
			VerifySegments(newText, oldText);
			VerifyAnalysis(newText, oldText);
			VerifyAnalysesOfUnchangedText(oldText);
			VerifyTranslationsAndNotes(oldTextBeforeChanges, oldText);
		}

		private void SetupPhrases(IStText oldText, Tuple<int, int, int>[] phrases)
		{
			// Process them in reverse order within each segment so merging later groups does not mess up earlier ones.
			if (phrases == null)
				return;
			var para = (IStTxtPara) oldText[0];
			foreach (var tuple in phrases.OrderBy(tuple=> -tuple.Item2))
			{
				var seg = para.SegmentsOS[tuple.Item1];
				int ichMin = seg.GetAnalysisBeginOffset(tuple.Item2);
				int ichLim = seg.GetAnalysisBeginOffset(tuple.Item3) + seg.GetBaselineText(tuple.Item3).Length;
				var phrase = para.Contents.Substring(ichMin, ichLim - ichMin);
				var wf = WfiWordformServices.FindOrCreateWordform(oldText.Cache, phrase);
				seg.AnalysesRS.Replace(tuple.Item2, tuple.Item3 - tuple.Item2 + 1, new [] {wf});
			}
		}

		/// <summary>
		/// Verify that the analyses before and after the change are still pointing at the original analyses.
		/// </summary>
		private void VerifyAnalysesOfUnchangedText(IStText text)
		{
			IStTxtPara para;
			// Verify that analyses match in initial identical paragraphs.
			for (int iPara = 0; iPara < MatchingInitialParagraphs; iPara++ )
			{
				para = text[iPara];
				for (int iSeg = 0; iSeg < para.SegmentsOS.Count; iSeg++)
				{
					var seg = para.SegmentsOS[iSeg];
					for (int iAnalysis = 0; iAnalysis < seg.AnalysesRS.Count; iAnalysis++)
					{
						Assert.AreEqual(m_oldAnalyses[iPara][iSeg][iAnalysis], seg.AnalysesRS[iAnalysis],
										"found wrong analysis at para " + iPara + " segment " + iSeg + " analysis " +
										iAnalysis);
					}
				}
			}
			if (MatchingInitialParagraphs >= text.ParagraphsOS.Count)
				return; // All paragraphs matched, nothing more to do (and getting para value in next line would fail).
			// Verify analyses match in initial identical segments of para after MatchingInitialParagraphs.
			para = text[MatchingInitialParagraphs];
			for (int iSeg = 0; iSeg < MatchingInitialSegments; iSeg++)
			{
				var seg = para.SegmentsOS[iSeg];
				for (int iAnalysis = 0; iAnalysis < seg.AnalysesRS.Count; iAnalysis++)
				{
					Assert.AreEqual(m_oldAnalyses[MatchingInitialParagraphs][iSeg][iAnalysis], seg.AnalysesRS[iAnalysis],
									"found wrong analysis at para " + MatchingInitialParagraphs + " segment " + iSeg + " analysis " +
									iAnalysis);
				}
			}
			// This condition is almost always true, but it can be that the first modified paragraph is just empty.
			if (MatchingInitialSegments < para.SegmentsOS.Count)
			{
				//Verify analyses match at start of segment after MatchingInitialSegments
				var segMI = para.SegmentsOS[MatchingInitialSegments];
				for (int iAnalysis = 0; iAnalysis < MatchingInitialAnalyses; iAnalysis++)
				{
					Assert.AreEqual(m_oldAnalyses[MatchingInitialParagraphs][MatchingInitialSegments][iAnalysis],
									segMI.AnalysesRS[iAnalysis],
									"found wrong analysis at para " + MatchingInitialParagraphs + " segment " +
									MatchingInitialSegments + " analysis " +
									iAnalysis);
				}
			}

			// Now do the final stuff.
			int paraDelta = text.ParagraphsOS.Count - m_oldAnalyses.Count;
			// Verify analyses match in final identical paragraphs.
			for (int iPara = text.ParagraphsOS.Count - MatchingFinalParagraphs; iPara < text.ParagraphsOS.Count; iPara++)
			{
				para = text[iPara];
				for (int iSeg = 0; iSeg < para.SegmentsOS.Count; iSeg++)
				{
					var seg = para.SegmentsOS[iSeg];
					for (int iAnalysis = 0; iAnalysis < seg.AnalysesRS.Count; iAnalysis++)
					{
						Assert.AreEqual(m_oldAnalyses[iPara - paraDelta][iSeg][iAnalysis], seg.AnalysesRS[iAnalysis],
										"found wrong analysis at para " + iPara + " segment " + iSeg + " analysis " +
										iAnalysis);
					}
				}
			}
			// Verify analyses match in final identical segments of paragraph before final paragraphs
			int iparaFinalSegmentsOutput = text.ParagraphsOS.Count - MatchingFinalParagraphs - 1;
			// The remaining checks can only be applied if there is a paragraph before the matching final ones
			// in both the output and the input.
			if (iparaFinalSegmentsOutput >= 0 && iparaFinalSegmentsOutput - paraDelta > 0)
			{
				para = text[iparaFinalSegmentsOutput];
				int segDelta = para.SegmentsOS.Count - m_oldAnalyses[iparaFinalSegmentsOutput - paraDelta].Count;
				for (int iSeg = para.SegmentsOS.Count - MatchingFinalSegments; iSeg < para.SegmentsOS.Count; iSeg++)
				{
					var seg = para.SegmentsOS[iSeg];
					for (int iAnalysis = 0; iAnalysis < seg.AnalysesRS.Count; iAnalysis++)
					{
						Assert.AreEqual(m_oldAnalyses[iparaFinalSegmentsOutput - paraDelta][iSeg - segDelta][iAnalysis],
										seg.AnalysesRS[iAnalysis],
										"found wrong analysis at para " + iparaFinalSegmentsOutput + " segment " + iSeg +
										" analysis " +
										iAnalysis);
					}
				}

				//Verify analyses match at end of final modified segment.
				int iSegMF = para.SegmentsOS.Count - MatchingFinalSegments - 1;
				if (iSegMF >= 0 && MatchingFinalAnalyses > 0) // false when the last modified para ends up empty.
				{
					var segMF = para.SegmentsOS[iSegMF];
					int analysisDelta = segMF.AnalysesRS.Count -
										m_oldAnalyses[iparaFinalSegmentsOutput - paraDelta][iSegMF - segDelta].Count;
					for (int iAnalysis = segMF.AnalysesRS.Count - MatchingFinalAnalyses;
						 iAnalysis < segMF.AnalysesRS.Count;
						 iAnalysis++)
					{
						Assert.AreEqual(
							m_oldAnalyses[iparaFinalSegmentsOutput - paraDelta][iSegMF - segDelta][
								iAnalysis - analysisDelta],
							segMF.AnalysesRS[iAnalysis],
							"found wrong analysis at para " + iparaFinalSegmentsOutput + " segment " + iSegMF +
							" analysis " +
							iAnalysis);
					}
				}
			}
		}


		/// <summary>
		/// Verify that the two texts have parallel sets of analyses.
		/// It should already have been verified that they have parallel collections of paragraphs and segments.
		/// Each segment should have the same length list of analysis, and corresponding ones should either
		/// be identical or have the same Wordform.
		/// </summary>
		private void VerifyAnalysis(IStText expectedText, IStText actualText)
		{
			for (int i = 0; i < expectedText.ParagraphsOS.Count; i++)
			{
				var expectedPara = expectedText[i];
				var actualPara = actualText[i];
				// In cases where we inserted a whole paragraph and didn't move anything into it,
				// it may not have a current parse, so it makes no sense to check its analyses.
				if (!actualPara.ParseIsCurrent)
					continue;
				for (int j = 0; j < expectedPara.SegmentsOS.Count; j++)
				{
					bool shouldBeExactlyMatching = j < MatchingInitialSegments ||
											j >= expectedPara.SegmentsOS.Count - MatchingFinalSegments;

					var expectedSeg = expectedPara.SegmentsOS[j];
					var actualSeg = actualPara.SegmentsOS[j];
					Assert.AreEqual(expectedSeg.AnalysesRS.Count, actualSeg.AnalysesRS.Count,
						"segment " + j + " of paragraph " + i + " has wrong number of analyses");
					if (actualSeg.IsLabel)
						continue; // REVIEW: Don't really care about guts of analysis since they are never shown and can sometimes be wrong.
					for (int k = 0; k < expectedSeg.AnalysesRS.Count; k++)
					{
						bool shouldMatchAnalysis = shouldBeExactlyMatching;
						if (shouldBeExactlyMatching)
						{
							shouldMatchAnalysis = k < MatchingInitialAnalyses ||
												  k >= expectedSeg.AnalysesRS.Count - MatchingFinalAnalyses;
						}
						var expectedAnalysis = expectedSeg.AnalysesRS[k];
						var actualAnalysis = actualSeg.AnalysesRS[k];
						if (expectedAnalysis is IWfiWordform)
						{
							if (shouldMatchAnalysis)
							{
								Assert.AreEqual(expectedAnalysis.Wordform, actualAnalysis.Wordform,
												"analysis " + k + " of segment " + j + " of paragraph " + i +
												" has an analysis of the wrong wordform: expected "
												+ expectedAnalysis.Wordform.Form.VernacularDefaultWritingSystem.Text + " but was "
												+ actualAnalysis.Wordform.Form.VernacularDefaultWritingSystem.Text);
							}
							else //The test did not expect an exact match here, so test the surface form only.
							{
								Assert.AreEqual(expectedAnalysis.Wordform.Form.BestVernacularAlternative.Text, actualAnalysis.Wordform.Form.BestVernacularAlternative.Text,
												   "analysis " + k + " of segment " + j + " of paragraph " + i +
												   " has an analysis of the wrong wordform: expected "
												   + expectedAnalysis.Wordform.Form.BestVernacularAlternative.Text + " but was "
												   + actualAnalysis.Wordform.Form.BestVernacularAlternative.Text);
							}
						}
						else
							Assert.AreEqual(expectedAnalysis, actualAnalysis,
							   "analysis " + k + " of segment " + j + " of paragraph " + i + " has the wrong analysis");
					}
				}
			}
		}

		/// <summary>
		/// Replace every analysis that is a WfiWordform with a distinct WfiAnalysis of that Wordform.
		/// Also as a side effect initialize m_oldAnalyses.
		/// </summary>
		/// <param name="stText"></param>
		protected void ChangeWordformsToAnalyses(IStText stText)
		{
			m_oldAnalyses.Clear();
			var analysisFactory = stText.Services.GetInstance<IWfiAnalysisFactory>();
			foreach (IStTxtPara para in stText.ParagraphsOS)
			{
				var segmentList = new List<List<IAnalysis>>();
				m_oldAnalyses.Add(segmentList);
				foreach (ISegment seg in para.SegmentsOS)
				{
					var analysesList = new List<IAnalysis>();
					segmentList.Add(analysesList);
					for (int i = 0; i < seg.AnalysesRS.Count; i++)
					{
						var wf = seg.AnalysesRS[i] as IWfiWordform;
						if (wf == null)
						{
							analysesList.Add(seg.AnalysesRS[i]);
							continue;
						}
						IWfiAnalysis analysis;
						analysis = analysisFactory.Create();
						wf.AnalysesOC.Add(analysis);
						seg.AnalysesRS[i] = analysis;
						analysesList.Add(analysis);
					}
				}
			}
		}

		internal void ParseOldText()
		{
			Parse(m_oldText);
		}

		protected virtual void Parse(IStText stText)
		{
			using (ParagraphParser pp = new ParagraphParser(stText.Cache))
			{
				foreach (IStTxtPara para in stText.ParagraphsOS)
					pp.Parse(para);
			}

		}

		protected IStText CopyBaseText(IStText oldText)
		{
			var stText = MakeStText();
			stText[0].Contents = oldText[0].Contents;

			// copy the remaining paragraphs if any.
			for (int i = 1; i < oldText.ParagraphsOS.Count; i++)
			{
				var newPara = oldText.Services.GetInstance<IStTxtParaFactory>().Create();
				stText.ParagraphsOS.Add(newPara);
				newPara.Contents = oldText[i].Contents;
			}

			return stText;
		}

		protected void MakeTheTextChange(IStText oldTextToChange, IStText newText)
		{
			if (ChangeMaker == null)
				oldTextToChange[0].Contents = newText[0].Contents;
			else
				ChangeMaker(oldTextToChange);
		}

		IStText MakeStText()
		{
			var text = m_textFactory.Create();
			//Cache.LangProject.TextsOC.Add(text);
			var stText = m_stTextFactory.Create();
			text.ContentsOA = stText;
			var stTextPara = m_paraFactory.Create();
			stText.ParagraphsOS.Add(stTextPara);

			return stText;
		}

	}
	#endregion

	#region BothWaysAdjustmentVerifier class
	class BothWaysAdjustmentVerifier : AdjustmentVerifier
	{
		internal BothWaysAdjustmentVerifier(FdoCache cache) : base(cache)
		{
		}

		internal bool ReverseShouldMergeSegmentTN { get; set; }

		internal override void RunTest()
		{
			//Run the test with normally with a change in the forward direction.
			base.RunTest();

			//Swap the input and output expected texts and run the test with the change in
			//the opposite direction.
			var swap = m_oldText;
			m_oldText = m_newText;
			m_newText = swap;
			// If we were merging segments going forward, going backward we can't split the 'merged' TN,
			// but we should copy it onto the first of the segments resulting from the split.
			if (ShouldMergeSegmentTN)
				MatchOneMoreInitialTN = true;
			ShouldMergeSegmentTN = ReverseShouldMergeSegmentTN;
			if (ShouldMergeSegmentTN)
				MatchOneMoreFinalTN = MatchOneMoreInitialTN = false;

			base.RunTest();
		}
	}
	#endregion

	#region AdjustmentVerifierPlusTags class
	class AdjustmentVerifierPlusTags : AdjustmentVerifier
	{
		protected ITextTagRepository m_tagRepo;
		protected ITextTagFactory m_tagFact;
		protected ICmPossibility[] m_tagPoss;
		internal List<TagSkeleton> m_oldTagSkels;
		internal List<TagSkeleton> m_newTagSkels;

		internal AdjustmentVerifierPlusTags(FdoCache cache) : base(cache)
		{
			Cache = cache;
			m_tagFact = Cache.ServiceLocator.GetInstance<ITextTagFactory>();
			m_tagRepo = Cache.ServiceLocator.GetInstance<ITextTagRepository>();

			// Get some tags for tests
			m_tagPoss = SetupTagPossibilities(Cache);
			m_oldTagSkels = new List<TagSkeleton>();
			m_newTagSkels = new List<TagSkeleton>();
		}

		internal FdoCache Cache { get; private set; }

		/// <summary>
		/// Loads an array of Possibilities from the first tagging list. [RRG Semantics]
		/// </summary>
		protected ICmPossibility[] SetupTagPossibilities(FdoCache cache)
		{
			// This could change, but at least it gives a reasonably stable list to test from.
			var textMarkupTags = cache.LangProject.GetDefaultTextTagList();
			var cposs = textMarkupTags.PossibilitiesOS[0].SubPossibilitiesOS.Count;
			var result = new ICmPossibility[cposs];
			for (var i = 0; i < cposs; i++)
				result[i] = textMarkupTags.PossibilitiesOS[0].SubPossibilitiesOS[i];
			return result;
		}

		/// <summary>
		/// Create a TextTag object for adding to a text (old or new)
		/// Handles tags spanning multiple Segments or multiple Paragraphs.
		/// </summary>
		/// <param name="skeleton">A TagSkeleton</param>
		/// <param name="txt">The text to add the tag to.</param>
		/// <returns></returns>
		private void MakeATextTag(TagSkeleton skeleton, IStText txt)
		{
			Debug.Assert(skeleton != null && txt != null,
				"No tag skeleton to build or no text to build it on!");
			var para = txt[skeleton.BegParaIndex];
			Debug.Assert(para != null && para.SegmentsOS.Count > 0, "Null or empty paragraph!");
			var para0Segs = para.SegmentsOS.ToArray();;
			var para1Segs = para0Segs;
			if (skeleton.BegParaIndex != skeleton.EndParaIndex)
			{
				var para1 = txt[skeleton.EndParaIndex];
				Debug.Assert(para1 != null && para1.SegmentsOS.Count > 0, "Null or empty end paragraph!");
				para1Segs = para1.SegmentsOS.ToArray();
			}
			var begPoint = new AnalysisOccurrence(para0Segs[skeleton.BeginSegmentIndex], skeleton.BeginAnalysisIndex);
			var endPoint = new AnalysisOccurrence(para1Segs[skeleton.EndSegmentIndex], skeleton.EndAnalysisIndex);
			m_tagFact.CreateOnText(begPoint, endPoint, m_tagPoss[skeleton.TagPossIndex]);
		}

		internal override void RunTest()
		{
			TestTagging();
		}

		private void TestTagging()
		{
			var oldText = CopyBaseText(m_oldText);
			var newText = CopyBaseText(m_newText);

			//Break the text into segments and assign default (wordform) analyses.
			Parse(oldText);
			ChangeWordformsToAnalyses(oldText);
			BuildTagging(oldText, m_oldTagSkels);

			Parse(newText);
			BuildTagging(newText, m_newTagSkels);

			MakeTheTextChange(oldText, m_newText);
			VerifyTextTags(newText, oldText);
		}

		/// <summary>
		/// Uses a List of TagSkeleton objects to build TextTags on an StText.
		/// </summary>
		/// <param name="txt"></param>
		/// <param name="skeletonList"></param>
		private void BuildTagging(IStText txt, IEnumerable<TagSkeleton> skeletonList)
		{
			foreach (var skel in skeletonList)
				MakeATextTag(skel, txt);
		}

		private static void VerifyTextTags(IStText expectedText, IStText actualText)
		{
			Assert.IsNotNull(expectedText, "Expected text is null!");
			Assert.IsNotNull(actualText, "Actual text is null!");
			var expTags = expectedText.TagsOC.ToList();
			var actTags = actualText.TagsOC.ToList();
			var cexpTags = expTags.Count;
			var cactTags = actTags.Count;
			if (cexpTags == 0)
			{
				Assert.AreEqual(cexpTags, cactTags, String.Format("Expected no tags, but found {0}.", cactTags));
			}
			else
			{
				Assert.AreEqual(cexpTags, cactTags, "Found unequal number of tags.");
				for (var i = 0; i < cexpTags; i++)
					Assert.IsTrue(expTags[i].IsAnalogousTo(actTags[i]), String.Format("Tags differ at index= {0}.", i));
			}
		}
	}
	#endregion

	#region AdjustmentVerifierPlusChartCells class
	class AdjustmentVerifierPlusChartCells : AdjustmentVerifier
	{
		protected IConstChartWordGroupRepository m_cellRepo;
		protected IConstChartWordGroupFactory m_cellFact;
		protected IConstChartRowFactory m_rowFact;
		internal ITsStrFactory m_tssFact;
		internal List<CellSkeleton> m_oldCellSkels;
		internal List<CellSkeleton> m_newCellSkels;
		internal ICmPossibility m_template;
		internal List<ICmPossibility> m_allCols;
		internal IDsConstChart m_chartOld;
		internal IDsConstChart m_chartNew;

		internal AdjustmentVerifierPlusChartCells(FdoCache cache)
			: base(cache)
		{
			Cache = cache;
			m_cellFact = Cache.ServiceLocator.GetInstance<IConstChartWordGroupFactory>();
			m_cellRepo = Cache.ServiceLocator.GetInstance<IConstChartWordGroupRepository>();
			m_rowFact = Cache.ServiceLocator.GetInstance<IConstChartRowFactory>();
			m_tssFact = Cache.TsStrFactory; // for creating row labels

			m_oldCellSkels = new List<CellSkeleton>();
			m_newCellSkels = new List<CellSkeleton>();

			// Setup a template
			m_template = MakeTemplate(out m_allCols);
		}

		internal FdoCache Cache { get; private set; }

		/// <summary>
		/// Create a ChartCell object for adding to a chart (on old or new text).
		/// Handles cells spanning multiple Segments or multiple Paragraphs.
		/// </summary>
		/// <param name="skeleton">A CellSkeleton</param>
		/// <param name="chart">The chart to add the cell to.</param>
		/// <returns></returns>
		private void MakeAChartCell(CellSkeleton skeleton, IDsConstChart chart)
		{
			Debug.Assert(skeleton != null && chart != null,
				"No cell skeleton to build or no chart to build it on!");
			var text = chart.BasedOnRA;
			var ccurrRows = chart.RowsOS.Count;
			var para = text[skeleton.BegParaIndex];
			Debug.Assert(para != null && para.SegmentsOS.Count > 0, "Null or empty paragraph!");
			var para0Segs = para.SegmentsOS.ToArray(); ;
			var para1Segs = para0Segs;
			if (skeleton.BegParaIndex != skeleton.EndParaIndex)
			{
				var para1 = text[skeleton.EndParaIndex];
				Debug.Assert(para1 != null && para1.SegmentsOS.Count > 0, "Null or empty end paragraph!");
				para1Segs = para1.SegmentsOS.ToArray();
			}

			// Make sure this chart has enough rows
			while (skeleton.RowIndex >= ccurrRows)
			{
				AddChartRow(chart);
				ccurrRows++;
			}

			// Create the cell
			var row = chart.RowsOS[skeleton.RowIndex];
			var begPoint = new AnalysisOccurrence(para0Segs[skeleton.BeginSegmentIndex],
								skeleton.BeginAnalysisIndex);
			var endPoint = new AnalysisOccurrence(para1Segs[skeleton.EndSegmentIndex],
								skeleton.EndAnalysisIndex);
			m_cellFact.Create(row, row.CellsOS.Count, m_allCols[skeleton.ColumnIndex],
								begPoint, endPoint);
		}

		internal override void RunTest()
		{
			TestCharting();
		}

		private void TestCharting()
		{
			var oldText = CopyBaseText(m_oldText);
			var newText = CopyBaseText(m_newText);

			//Break the text into segments and assign default (wordform) analyses.
			Parse(oldText);
			m_chartOld = SetupAChart(oldText);
			ChangeWordformsToAnalyses(oldText);
			BuildChartCells(m_chartOld, m_oldCellSkels);

			Parse(newText);
			m_chartNew = SetupAChart(newText);
			BuildChartCells(m_chartNew, m_newCellSkels);

			MakeTheTextChange(oldText, m_newText);
			VerifyCharts(m_chartNew, m_chartOld);
		}

		/// <summary>
		/// Uses a List of CellSkeleton objects to build Chart Cells on a ConstChart
		/// based on one of our texts.
		/// </summary>
		/// <param name="chart"></param>
		/// <param name="skeletonList"></param>
		private void BuildChartCells(IDsConstChart chart, IEnumerable<CellSkeleton> skeletonList)
		{
			foreach (var skel in skeletonList)
				MakeAChartCell(skel, chart);
		}

		private static void VerifyCharts(IDsConstChart expectedChart, IDsConstChart actualChart)
		{
			Assert.IsNotNull(expectedChart, "Expected chart is null!");
			Assert.IsNotNull(actualChart, "Actual chart is null!");
			var expRows = expectedChart.RowsOS.ToList();
			var actRows = actualChart.RowsOS.ToList();
			var cexpRows = expRows.Count;
			var cactRows = actRows.Count;
			if (cexpRows == 0)
			{
				Assert.AreEqual(cexpRows, cactRows, String.Format("Expected no rows, but found {0}.", cactRows));
			}
			else
			{
				Assert.AreEqual(cexpRows, cactRows, "Found unequal number of rows in chart.");
				// Check row contents
				for (var i = 0; i < cexpRows; i++)
					VerifyRowContents(i+1, expRows[i], actRows[i]);
			}
		}

		private static void VerifyRowContents(int rowNum, IConstChartRow expRow, IConstChartRow actRow)
		{
			var expCells = expRow.CellsOS.ToList();
			var actCells = actRow.CellsOS.ToList();
			var cexpCells = expCells.Count;
			var cactCells = actCells.Count;
			if (cexpCells == 0)
			{
				Assert.AreEqual(cexpCells, cactCells,
					String.Format("Expected no cells in row {0}, but found {1}.", rowNum, cactCells));
			}
			else
			{
				Assert.AreEqual(cexpCells, cactCells, String.Format("Found unequal number of cells in row {0}.", rowNum));
				for (var i = 0; i < cexpCells; i++)
				{
					VerifyCells(i, expCells[i], actCells[i]);
				}
			}

		}

		private static void VerifyCells(int icell, IConstituentChartCellPart expCell, IConstituentChartCellPart actCell)
		{
			if (expCell is IConstChartWordGroup)
			{
				var expGrp = (IConstChartWordGroup)expCell;
				var actGrp = (IConstChartWordGroup)actCell;
				Assert.IsTrue(expGrp.IsAnalogousTo(actGrp), String.Format("Cells differ at index = {0}.", icell));
			}
			else
			{
				if (actCell is IConstChartWordGroup)
					Assert.Fail(String.Format("Expected cell other than ConstChartWordGroup at index = {0}.", icell));
				Assert.IsTrue(expCell.ColumnRA == actCell.ColumnRA,
					String.Format("Cell is in the wrong column at index = {0}.", icell));
			}
		}

		#region Utility Methods

		/// <summary>
		/// Creates a chart on 'text', adds it to the LangProject, and sets a template
		/// </summary>
		internal IDsConstChart SetupAChart(IStText text)
		{
			Assert.IsNotNull(m_template, "Create the chart template first!");
			Assert.IsNotNull(Cache.LangProject, "No LangProject in the cache!");
			var data = Cache.LangProject.DiscourseDataOA;
			Assert.IsNotNull(data, "No DiscourseData object!");
			var result = Cache.ServiceLocator.GetInstance<IDsConstChartFactory>().Create(
				data, text, m_template);
			Cache.LangProject.GetDefaultChartMarkers();
			return result;
		}

		private void AddChartRow(IDsConstChart chart)
		{
			var crows = chart.RowsOS.Count + 1; // count after adding new row!
			var ws = Cache.WritingSystemFactory.GetWsFromStr("en");
			Debug.Assert(ws > 0, "No English writing system?");
			var rowLabel = m_tssFact.MakeString(crows.ToString(), ws);
			var row = m_rowFact.Create(chart, crows - 1, rowLabel);
			row.EndSentence = true; // don't want to deal with 1a, 1b, etc.
		}

		public ICmPossibility MakeTemplate(out List<ICmPossibility> allCols)
		{
			// The exact organization of columns is not of great
			// importance for the current tests (still less the names), but we do want there
			// to be a hierarchy, since that is a common problem, and naming them conventionally
			// may make debugging easier. Currently this is the same set of columns as
			// m_logic.CreateDefaultColumns, but we make it explicit here so most of the test
			// code is unaffected by changes to the default.
			var doc = new XmlDocument();
			doc.LoadXml(
				"<template name=\"default\">"
				+ "<column name=\"prenuclear\">"
				+ "<column name=\"prenuc1\"/>"
				+ "<column name=\"prenuc2\"/>"
				+ "</column>"
				+ "<column name=\"nucleus\">"
				+ "<column name=\"Subject\"/>"
				+ "<column name=\"verb\"/>"
				+ "<column name=\"object\"/>"
				+ "</column>"
				+ "<column name=\"postnuc\"/>"
				+ "</template>");
			m_template = Cache.LangProject.CreateChartTemplate(doc.DocumentElement);
			allCols = AllColumns(m_template);
			return m_template;
		}

		/// <summary>
		/// Gets all the 'leaf' nodes in a chart template, and also the ends of column groupings.
		/// </summary>
		/// <param name="template"></param>
		/// <returns>List of int (hvos?)</returns>
		public List<ICmPossibility> AllColumns(ICmPossibility template)
		{
			var result = new List<ICmPossibility>();
			if (template == null || template.SubPossibilitiesOS.Count == 0)
				return result; // template itself can't be a column even if no children.
			CollectColumns(result, template);
			return result;
		}

		/// <summary>
		/// Collect (in depth-first traversal) all the leaf columns in the template.
		/// </summary>
		/// <param name="result"></param>
		/// <param name="template"></param>
		private static void CollectColumns(ICollection<ICmPossibility> result, ICmPossibility template)
		{
			if (template.SubPossibilitiesOS.Count == 0)
			{
				// Note: do NOT do add to the list if it has children...we ONLY want leaves in the result.
				result.Add(template);
				return;
			}
			foreach (var child in template.SubPossibilitiesOS)
				CollectColumns(result, child);
		}

		#endregion
	}
	#endregion

	#region IarSkeleton class
	/// <summary>
	/// This class allows IAnalysisReferences to be defined by a test before the test text
	/// is analyzed. The test runner then creates them after the text has been analyzed.
	/// </summary>
	internal class IarSkeleton
	{
		/// <summary>
		/// Use this constructor to specify an IAnalysisReference that spans more than one StTxtPara.
		/// (This is the most complete constructor.)
		/// </summary>
		/// <param name="ibegPara"></param>
		/// <param name="iendPara"></param>
		/// <param name="ibegSeg"></param>
		/// <param name="iendSeg"></param>
		/// <param name="ibegIndex"></param>
		/// <param name="iendIndex"></param>
		public IarSkeleton(int ibegPara, int iendPara, int ibegSeg, int iendSeg, int ibegIndex, int iendIndex)
		{
			BegParaIndex = ibegPara;
			EndParaIndex = iendPara;
			EndAnalysisIndex = iendIndex;
			BeginAnalysisIndex = ibegIndex;
			EndSegmentIndex = iendSeg;
			BeginSegmentIndex = ibegSeg;
		}

		/// <summary>
		/// Index of the paragraph within the StText (usually 0)
		/// This is the paragraph index for the begin point of the tag,
		/// if it happens to span more than a single paragraph.
		/// </summary>
		public int BegParaIndex { get; private set; }

		/// <summary>
		/// This is the paragraph index for the end point of the tag,
		/// if it happens to span more than a single paragraph.
		/// </summary>
		public int EndParaIndex { get; private set; }

		/// <summary>
		/// Index of the EndSegment within the paragraph for the tag
		/// </summary>
		public int EndSegmentIndex { get; private set; }

		/// <summary>
		/// Index of the BeginSegment within the paragraph for the tag
		/// </summary>
		public int BeginSegmentIndex { get; private set; }

		/// <summary>
		/// BeginAnalysisIndex for the tag
		/// </summary>
		public int BeginAnalysisIndex { get; private set; }

		/// <summary>
		/// EndAnalysisIndex for the tag
		/// </summary>
		public int EndAnalysisIndex { get; private set; }
	}
	#endregion

	#region TagSkeleton class
	/// <summary>
	/// This class allows tags to be defined by a test before the test text is analyzed.
	/// The test runner then creates them after the text has been analyzed.
	/// </summary>
	internal class TagSkeleton : IarSkeleton
	{
		/// <summary>
		/// Use this constructor to specify a StTxtPara other than the first.
		/// </summary>
		/// <param name="ipara"></param>
		/// <param name="ibegSeg"></param>
		/// <param name="iendSeg"></param>
		/// <param name="ibegIndex"></param>
		/// <param name="iendIndex"></param>
		/// <param name="iPoss"></param>
		public TagSkeleton(int ipara, int ibegSeg, int iendSeg, int ibegIndex, int iendIndex, int iPoss)
			: base(ipara, ipara, ibegSeg, iendSeg, ibegIndex, iendIndex)
		{
			TagPossIndex = iPoss;
		}

		/// <summary>
		/// Default constructor for tags in the first paragraph of a text.
		/// </summary>
		/// <param name="ibegSeg"></param>
		/// <param name="iendSeg"></param>
		/// <param name="ibegIndex"></param>
		/// <param name="iendIndex"></param>
		/// <param name="iPoss"></param>
		public TagSkeleton(int ibegSeg, int iendSeg, int ibegIndex, int iendIndex, int iPoss)
			: base(0, 0, ibegSeg, iendSeg, ibegIndex, iendIndex)
		{
			TagPossIndex = iPoss;
		}

		/// <summary>
		/// Use this constructor if the begin and end Segment for the tag is the same
		/// (and in the first paragraph of a text).
		/// </summary>
		/// <param name="iSeg"></param>
		/// <param name="ibegIndex"></param>
		/// <param name="iendIndex"></param>
		/// <param name="iPoss"></param>
		public TagSkeleton(int iSeg, int ibegIndex, int iendIndex, int iPoss)
			: base(0, 0, iSeg, iSeg, ibegIndex, iendIndex)
		{
			TagPossIndex = iPoss;
		}

		/// <summary>
		/// Use this constructor to specify a tag that spans more than one StTxtPara.
		/// (This is the most complete constructor.)
		/// </summary>
		/// <param name="ibegPara"></param>
		/// <param name="iendPara"></param>
		/// <param name="ibegSeg"></param>
		/// <param name="iendSeg"></param>
		/// <param name="ibegIndex"></param>
		/// <param name="iendIndex"></param>
		/// <param name="iPoss"></param>
		public TagSkeleton(int ibegPara, int iendPara, int ibegSeg, int iendSeg, int ibegIndex, int iendIndex, int iPoss)
			: base(ibegPara, iendPara, ibegSeg, iendSeg, ibegIndex, iendIndex)
		{
			TagPossIndex = iPoss;
		}

		/// <summary>
		/// Index of the CmPossibility for the tag (in our test array)
		/// </summary>
		public int TagPossIndex { get; private set; }
	}
	#endregion

	#region CellSkeleton class
	/// <summary>
	/// This class allows cells to be defined by a test before the test text is analyzed.
	/// The test runner then creates them after the text has been analyzed.
	/// Unlike TextTags, Chart Cells are in a sequence, so create them in order!
	/// </summary>
	internal class CellSkeleton : IarSkeleton
	{
		/// <summary>
		/// Use this constructor to specify a StTxtPara other than the first.
		/// </summary>
		/// <param name="ipara"></param>
		/// <param name="ibegSeg"></param>
		/// <param name="iendSeg"></param>
		/// <param name="ibegIndex"></param>
		/// <param name="iendIndex"></param>
		/// <param name="iRow"></param>
		/// <param name="iColumn"></param>
		public CellSkeleton(int ipara, int ibegSeg, int iendSeg, int ibegIndex, int iendIndex,
			int iRow, int iColumn)
			: base(ipara, ipara, ibegSeg, iendSeg, ibegIndex, iendIndex)
		{
			RowIndex = iRow;
			ColumnIndex = iColumn;
		}

		/// <summary>
		/// Default constructor for cells in the first paragraph of a text.
		/// </summary>
		/// <param name="ibegSeg"></param>
		/// <param name="iendSeg"></param>
		/// <param name="ibegIndex"></param>
		/// <param name="iendIndex"></param>
		/// <param name="iRow"></param>
		/// <param name="iColumn"></param>
		public CellSkeleton(int ibegSeg, int iendSeg, int ibegIndex, int iendIndex, int iRow, int iColumn)
			: base(0, 0, ibegSeg, iendSeg, ibegIndex, iendIndex)
		{
			RowIndex = iRow;
			ColumnIndex = iColumn;
		}

		/// <summary>
		/// Use this constructor if the begin and end Segment for the cell is the same
		/// (and in the first paragraph of a text).
		/// </summary>
		/// <param name="iSeg"></param>
		/// <param name="ibegIndex"></param>
		/// <param name="iendIndex"></param>
		/// <param name="iRow"></param>
		/// <param name="iColumn"></param>
		public CellSkeleton(int iSeg, int ibegIndex, int iendIndex, int iRow, int iColumn)
			: base(0, 0, iSeg, iSeg, ibegIndex, iendIndex)
		{
			RowIndex = iRow;
			ColumnIndex = iColumn;
		}

		/// <summary>
		/// Use this constructor to specify a cell that spans more than one StTxtPara.
		/// (This is the most complete constructor.)
		/// </summary>
		/// <param name="ibegPara"></param>
		/// <param name="iendPara"></param>
		/// <param name="ibegSeg"></param>
		/// <param name="iendSeg"></param>
		/// <param name="ibegIndex"></param>
		/// <param name="iendIndex"></param>
		/// <param name="iRow"></param>
		/// <param name="iColumn"></param>
		public CellSkeleton(int ibegPara, int iendPara, int ibegSeg, int iendSeg, int ibegIndex,
			int iendIndex, int iRow, int iColumn)
			: base(ibegPara, iendPara, ibegSeg, iendSeg, ibegIndex, iendIndex)
		{
			RowIndex = iRow;
			ColumnIndex = iColumn;
		}

		/// <summary>
		/// Index of the chart row for the cell (in our test array)
		/// </summary>
		public int RowIndex { get; private set; }

		/// <summary>
		/// Index of the template column possibility for the cell (in our test array)
		/// </summary>
		public int ColumnIndex { get; private set; }
	}
	#endregion
}
