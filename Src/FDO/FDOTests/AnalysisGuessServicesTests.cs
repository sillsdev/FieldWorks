// Copyright (c) 2009-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: AnalysisGuessServicesTests.cs
// Responsibility: pyle
//
// <remarks>
// </remarks>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.FDO.DomainImpl;
using SIL.Utils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.CoreImpl;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class AnalysisGuessServicesTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		// REVIEW (TomB): There is no reason to derive from FwDisposableBase because neither
		// Dispose method is being overriden. Either override one of those methods or get rid
		// of all the using statements where objects of this class are instantiated.
		internal class AnalysisGuessBaseSetup : FwDisposableBase
		{
			internal IText Text { get; set; }
			internal IStText StText { get; set; }
			internal StTxtPara Para0 { get; set; }
			internal IList<IWfiWordform> Words_para0 { get; set; }
			internal ICmAgent UserAgent { get; set; }
			internal ICmAgent ParserAgent { get; set; }
			internal AnalysisGuessServices GuessServices { get; set; }
			internal ILexEntryFactory EntryFactory { get; set; }


			// parts of speech
			internal IPartOfSpeech Pos_adjunct { get; set; }
			internal IPartOfSpeech Pos_noun { get; set; }
			internal IPartOfSpeech Pos_verb { get; set; }
			internal IPartOfSpeech Pos_transitiveVerb { get; set; }

			// variant entry types
			internal ILexEntryType Vet_DialectalVariant { get; set; }
			internal ILexEntryType Vet_FreeVariant { get; set; }
			internal ILexEntryType Vet_InflectionalVariant { get; set; }

			internal enum Flags
			{
				PartsOfSpeech,
				VariantEntryTypes
			}

			AnalysisGuessBaseSetup()
			{
				Words_para0 = new List<IWfiWordform>();
			}

			FdoCache Cache { get; set; }

			internal AnalysisGuessBaseSetup(FdoCache cache) : this()
			{
				Cache = cache;
				UserAgent = Cache.LanguageProject.DefaultUserAgent;
				ParserAgent = Cache.LangProject.DefaultParserAgent;
				GuessServices = new AnalysisGuessServices(Cache);
				EntryFactory = Cache.ServiceLocator.GetInstance<ILexEntryFactory>();
				DoDataSetup();
			}

			internal AnalysisGuessBaseSetup(FdoCache cache, params Flags[] options)
				: this(cache)
			{
				if (options.Contains(Flags.PartsOfSpeech))
					SetupPartsOfSpeech();
				if (options.Contains(Flags.VariantEntryTypes))
					SetupVariantEntryTypes();
			}

			internal void DoDataSetup()
			{
				var textFactory = Cache.ServiceLocator.GetInstance<ITextFactory>();
				var stTextFactory = Cache.ServiceLocator.GetInstance<IStTextFactory>();
				Text = textFactory.Create();
				//Cache.LangProject.TextsOC.Add(Text);
				StText = stTextFactory.Create();
				Text.ContentsOA = StText;
				Para0 = (StTxtPara)StText.AddNewTextPara(null);
				var wfFactory = Cache.ServiceLocator.GetInstance<IWfiWordformFactory>();
				var wsVern = Cache.DefaultVernWs;
				/* A a a a. */
				IWfiWordform A = wfFactory.Create(TsStringUtils.MakeTss("A", wsVern));
				IWfiWordform a = wfFactory.Create(TsStringUtils.MakeTss("a", wsVern));
				Words_para0.Add(A);
				Words_para0.Add(a);
				Words_para0.Add(a);
				Words_para0.Add(a);
				Para0.Contents = TsStringUtils.MakeTss(
					Words_para0[0].Form.BestVernacularAlternative.Text + " " +
					Words_para0[1].Form.BestVernacularAlternative.Text + " " +
					Words_para0[2].Form.BestVernacularAlternative.Text + " " +
					Words_para0[3].Form.BestVernacularAlternative.Text + ".", wsVern);
				/* b B. */
				IWfiWordform b = wfFactory.Create(TsStringUtils.MakeTss("b", wsVern));
				IWfiWordform B = wfFactory.Create(TsStringUtils.MakeTss("B", wsVern));
				Words_para0.Add(b);
				Words_para0.Add(B);
				var bldr = Para0.Contents.GetIncBldr();
				bldr.AppendTsString(TsStringUtils.MakeTss(
					" " + Words_para0[4].Form.BestVernacularAlternative.Text + " " +
					Words_para0[5].Form.BestVernacularAlternative.Text + ".", wsVern));
				Para0.Contents = bldr.GetString();
				using (ParagraphParser pp = new ParagraphParser(Cache))
				{
					foreach (IStTxtPara para in StText.ParagraphsOS)
						pp.Parse(para);
				}
			}

			internal void SetupPartsOfSpeech()
			{
				// setup language project parts of speech
				var partOfSpeechFactory = Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>();
				Pos_adjunct = partOfSpeechFactory.Create();
				Pos_noun = partOfSpeechFactory.Create();
				Pos_verb = partOfSpeechFactory.Create();
				Pos_transitiveVerb = partOfSpeechFactory.Create();
				Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.Add(Pos_adjunct);
				Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.Add(Pos_noun);
				Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.Add(Pos_verb);
				Pos_verb.SubPossibilitiesOS.Add(Pos_transitiveVerb);
				Pos_adjunct.Name.set_String(Cache.DefaultAnalWs, "adjunct");
				Pos_noun.Name.set_String(Cache.DefaultAnalWs, "noun");
				Pos_verb.Name.set_String(Cache.DefaultAnalWs, "verb");
				Pos_transitiveVerb.Name.set_String(Cache.DefaultAnalWs, "transitive verb");
			}

			internal void SetupVariantEntryTypes()
			{
				VariantEntryTypes = Cache.LangProject.LexDbOA.VariantEntryTypesOA;
				Vet_DialectalVariant = VariantEntryTypes.PossibilitiesOS[0] as ILexEntryType;
				Vet_FreeVariant = VariantEntryTypes.PossibilitiesOS[1] as ILexEntryType;
				Vet_InflectionalVariant = VariantEntryTypes.PossibilitiesOS[2] as ILexEntryType;
			}

			ICmPossibilityList VariantEntryTypes { get; set; }
		}

		/// <summary>
		/// Kludge: undo doesn't work for everything in these tests, so RestartCache to be more radical.
		/// </summary>
		public override void TestTearDown()
		{
			base.TestTearDown();
			base.FixtureTeardown();
			base.FixtureSetup();
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void NoExpectedGuessForWord_NoAnalyses()
		{
			// don't make any analyses. so we don't expect any guesses.
			using (var setup = new AnalysisGuessBaseSetup(Cache))
			{
				var guessActual = setup.GuessServices.GetBestGuess(setup.Words_para0[1]);
				Assert.AreEqual(new NullWAG(), guessActual);
			}
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void NoExpectedGuessForAnalysis_NoAnalyses()
		{
			// don't make any analyses. so we don't expect any guesses.
			using (var setup = new AnalysisGuessBaseSetup(Cache))
			{
				var guessActual = setup.GuessServices.GetBestGuess(setup.Words_para0[1]);
				Assert.AreEqual(new NullWAG(), guessActual);
			}
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void NoExpectedGuessForAnalysis_NoGlosses()
		{
			// make two analyses, but don't make any glosses. so we don't expect any guesses for one of the analyses.
			using (var setup = new AnalysisGuessBaseSetup(Cache))
			{
				var newAnalysisWag = WordAnalysisOrGlossServices.CreateNewAnalysisWAG(setup.Words_para0[1]);
				var newAnalysisWag2 = WordAnalysisOrGlossServices.CreateNewAnalysisWAG(setup.Words_para0[1]);
				var guessActual = setup.GuessServices.GetBestGuess(newAnalysisWag2.Analysis);
				Assert.AreEqual(new NullWAG(), guessActual);
			}
		}


		/// <summary>
		/// make a disapproved analysis that shouldn't be returned as a guess.
		/// </summary>
		[Test]
		public void NoExpectedGuessForWord_DisapprovesHumanAnalysis()
		{
			using (var setup = new AnalysisGuessBaseSetup(Cache))
			{
				var newWag = WordAnalysisOrGlossServices.CreateNewAnalysisWAG(setup.Words_para0[1]);
				var newAnalysis = newWag.Analysis;
				setup.UserAgent.SetEvaluation(newAnalysis, Opinions.disapproves);
				var guessActual = setup.GuessServices.GetBestGuess(setup.Words_para0[1]);
				Assert.AreEqual(new NullWAG(), guessActual);
			}
		}

		/// <summary>
		/// make a disapproved analysis that shouldn't be returned as a guess for another analysis.
		/// </summary>
		[Test]
		public void NoExpectedGuessForAnalysis_DisapprovesHumanAnalysis()
		{
			using (var setup = new AnalysisGuessBaseSetup(Cache))
			{
				var newAnalysisWag = WordAnalysisOrGlossServices.CreateNewAnalysisWAG(setup.Words_para0[1]);
				var newAnalysisWag2 = WordAnalysisOrGlossServices.CreateNewAnalysisWAG(setup.Words_para0[1]);
				setup.UserAgent.SetEvaluation(newAnalysisWag.Analysis, Opinions.disapproves);
				var guessActual = setup.GuessServices.GetBestGuess(newAnalysisWag2.Analysis);
				Assert.AreEqual(new NullWAG(), guessActual);
			}
		}

		/// <summary>
		/// make a gloss with a disapproved analysis that shouldn't be returned as a guess.
		/// </summary>
		[Test]
		public void NoExpectedGuessForWord_DisapprovesHumanAnalysisOfGloss()
		{
			using (var setup = new AnalysisGuessBaseSetup(Cache))
			{
				var newGlossWag = WordAnalysisOrGlossServices.CreateNewAnalysisTreeGloss(setup.Words_para0[1]);
				setup.UserAgent.SetEvaluation(newGlossWag.WfiAnalysis, Opinions.disapproves);
				var guessActual = setup.GuessServices.GetBestGuess(setup.Words_para0[1]);
				Assert.AreEqual(new NullWAG(), guessActual);
			}
		}

		/// <summary>
		/// make a gloss with a disapproved analysis that shouldn't be returned as a guess.
		/// </summary>
		[Test]
		public void NoExpectedGuessForAnalysis_DisapprovesHumanAnalysisOfGloss()
		{
			using (var setup = new AnalysisGuessBaseSetup(Cache))
			{
				var newGlossWag = WordAnalysisOrGlossServices.CreateNewAnalysisTreeGloss(setup.Words_para0[1]);
				setup.UserAgent.SetEvaluation(newGlossWag.WfiAnalysis, Opinions.disapproves);
				var guessActual = setup.GuessServices.GetBestGuess(newGlossWag.WfiAnalysis);
				Assert.AreEqual(new NullWAG(), guessActual);
			}
		}

		/// <summary>
		/// make a human disapproved analysis that shouldn't be returned as a guess.
		/// </summary>
		[Test]
		public void NoExpectedGuessForWord_DisapprovesParserApprovedAnalysis()
		{
			using (var setup = new AnalysisGuessBaseSetup(Cache))
			{
				var newWag = WordAnalysisOrGlossServices.CreateNewAnalysisWAG(setup.Words_para0[1]);
				var newAnalysis = newWag.Analysis;
				setup.ParserAgent.SetEvaluation(newAnalysis, Opinions.approves);
				setup.UserAgent.SetEvaluation(newAnalysis, Opinions.disapproves);
				var guessActual = setup.GuessServices.GetBestGuess(setup.Words_para0[1]);
				Assert.AreEqual(new NullWAG(), guessActual);
			}
		}

		/// <summary>
		/// make an entry (affix) that shouldn't be returned as a guess.
		/// </summary>
		[Test]
		public void NoExpectedGuessForWord_NoMatchingEntries()
		{
			using (var setup = new AnalysisGuessBaseSetup(Cache))
			{
				// create an affix entry
				var newEntry1= setup.EntryFactory.Create("a-", "aPrefix", SandboxGenericMSA.Create(MsaType.kInfl, null));
				var newEntry2 = setup.EntryFactory.Create("-a", "aSuffix", SandboxGenericMSA.Create(MsaType.kDeriv, null));
				var newEntry3 = setup.EntryFactory.Create("-a-", "aInfix", SandboxGenericMSA.Create(MsaType.kUnclassified, null));
				var newEntry4 = setup.EntryFactory.Create("ay", "Astem", SandboxGenericMSA.Create(MsaType.kStem, null));
				var newEntry5 = setup.EntryFactory.Create("ay", "Aroot", SandboxGenericMSA.Create(MsaType.kRoot, null));
				// try to generate analyses for matching entries (should have no results)
				setup.GuessServices.GenerateEntryGuesses(setup.StText);

				int cAnalyses = Cache.ServiceLocator.GetInstance<IWfiAnalysisRepository>().Count;
				Assert.AreEqual(0, cAnalyses, "Should not have generated guesses.");

				// make sure we don't actually make a guess.
				var guessActual = setup.GuessServices.GetBestGuess(setup.Words_para0[1]);
				Assert.AreEqual(new NullWAG(), guessActual);
			}
		}


		/// <summary>
		///
		/// </summary>
		[Test]
		public void NoExpectedGuessForWord_DontMatchBoundedStemOrRoot()
		{
			using (var setup = new AnalysisGuessBaseSetup(Cache))
			{
				var morphTypeRepository = Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>();
				var morphTypeBoundedRoot = morphTypeRepository.GetObject(MoMorphTypeTags.kguidMorphBoundRoot);
				var morphTypeBoundedStem = morphTypeRepository.GetObject(MoMorphTypeTags.kguidMorphBoundStem);
				// first make sure these types don't care about prefix/postfix markers
				morphTypeBoundedRoot.Prefix = morphTypeBoundedRoot.Postfix = null;
				morphTypeBoundedStem.Prefix = morphTypeBoundedStem.Postfix = null;

				// create an affix entry
				var newEntry1 = setup.EntryFactory.Create("a-", "aPrefix", SandboxGenericMSA.Create(MsaType.kInfl, null));
				var newEntry2 = setup.EntryFactory.Create("-a", "aSuffix", SandboxGenericMSA.Create(MsaType.kDeriv, null));
				var newEntry3 = setup.EntryFactory.Create("-a-", "aInfix", SandboxGenericMSA.Create(MsaType.kUnclassified, null));
				var boundedStem = setup.EntryFactory.Create(morphTypeBoundedStem, TsStringUtils.MakeTss("a", Cache.DefaultVernWs),
					"aboundedstem", SandboxGenericMSA.Create(MsaType.kStem, null));
				var boundedRoot = setup.EntryFactory.Create(morphTypeBoundedRoot, TsStringUtils.MakeTss("a", Cache.DefaultVernWs),
					"aboundedroot", SandboxGenericMSA.Create(MsaType.kRoot, null));
				// try to generate analyses for matching entries (should have no results)
				setup.GuessServices.GenerateEntryGuesses(setup.StText);

				int cAnalyses = Cache.ServiceLocator.GetInstance<IWfiAnalysisRepository>().Count;
				Assert.AreEqual(0, cAnalyses, "Should not have generated guesses.");

				// make sure we don't actually make a guess.
				var guessActual = setup.GuessServices.GetBestGuess(setup.Words_para0[1]);
				Assert.AreEqual(new NullWAG(), guessActual);
			}

		}

		/// <summary>
		/// make a human disapproved analysis that shouldn't be returned as a guess.
		/// </summary>
		[Test]
		public void NoExpectedGuessForAnalysis_DisapprovesParserApprovedAnalysis()
		{
			using (var setup = new AnalysisGuessBaseSetup(Cache))
			{
				var newWag = WordAnalysisOrGlossServices.CreateNewAnalysisWAG(setup.Words_para0[1]);
				var newAnalysis = newWag.Analysis;
				setup.ParserAgent.SetEvaluation(newAnalysis, Opinions.approves);
				setup.UserAgent.SetEvaluation(newAnalysis, Opinions.disapproves);
				var guessActual = setup.GuessServices.GetBestGuess(newWag.Analysis);
				Assert.AreEqual(new NullWAG(), guessActual);
			}
		}

		/// <summary>
		/// make an entry (stem) that should be returned as a guess.
		/// </summary>
		[Test]
		public void ExpectedGuessForWord_MatchingEntry()
		{
			using (var setup = new AnalysisGuessBaseSetup(Cache, AnalysisGuessBaseSetup.Flags.PartsOfSpeech))
			{
				// create an affix entry
				var newEntry1 = setup.EntryFactory.Create("a-", "aPrefix", SandboxGenericMSA.Create(MsaType.kInfl, null));
				var newEntry2 = setup.EntryFactory.Create("-a", "aSuffix", SandboxGenericMSA.Create(MsaType.kDeriv, null));
				var newEntry3 = setup.EntryFactory.Create("-a-", "aInfix", SandboxGenericMSA.Create(MsaType.kUnclassified, null));
				var newEntry4_expectedMatch = setup.EntryFactory.Create("a", "astem", SandboxGenericMSA.Create(MsaType.kStem, setup.Pos_noun));
				var newEntry5 = setup.EntryFactory.Create("a", "aroot", SandboxGenericMSA.Create(MsaType.kRoot, null));

				// expect a guess to be generated
				setup.GuessServices.GenerateEntryGuesses(setup.StText);
				var guessActual = setup.GuessServices.GetBestGuess(setup.Words_para0[1]);
				Assert.AreNotEqual(new NullWAG(), guessActual);
				Assert.AreEqual(newEntry4_expectedMatch.LexemeFormOA.Form.BestVernacularAlternative.Text, guessActual.Wordform.Form.BestVernacularAlternative.Text);
				Assert.AreEqual(1, guessActual.Analysis.MorphBundlesOS.Count);
				Assert.AreEqual(newEntry4_expectedMatch.LexemeFormOA, guessActual.Analysis.MorphBundlesOS[0].MorphRA);
				Assert.AreEqual(newEntry4_expectedMatch.SensesOS[0], guessActual.Analysis.MorphBundlesOS[0].SenseRA);
				Assert.AreEqual(newEntry4_expectedMatch.SensesOS[0].MorphoSyntaxAnalysisRA, guessActual.Analysis.MorphBundlesOS[0].MsaRA);
				Assert.AreEqual(newEntry4_expectedMatch.SensesOS[0].Gloss.BestAnalysisAlternative.Text,
								guessActual.Analysis.MeaningsOC.First().Form.BestAnalysisAlternative.Text);
				Assert.AreEqual(setup.Pos_noun, guessActual.Analysis.CategoryRA);
				int cAnalyses = Cache.ServiceLocator.GetInstance<IWfiAnalysisRepository>().Count;
				Assert.AreEqual(1, cAnalyses, "Should have only generated one computer guess analysis.");
			}
		}

		/// <summary>
		/// make a variant entry (stem) that should be returned as a guess.
		/// </summary>
		[Test]
		public void ExpectedGuessForWord_MatchingVariantOfEntry()
		{
			using (var setup = new AnalysisGuessBaseSetup(Cache,
				AnalysisGuessBaseSetup.Flags.PartsOfSpeech, AnalysisGuessBaseSetup.Flags.VariantEntryTypes))
			{
				// create an affix entry
				var mainEntry = setup.EntryFactory.Create("aMain", "astem", SandboxGenericMSA.Create(MsaType.kStem, setup.Pos_noun));
				ILexEntryRef ler1 = mainEntry.CreateVariantEntryAndBackRef(setup.Vet_DialectalVariant, TsStringUtils.MakeTss("a", Cache.DefaultVernWs));
				var variantOfEntry = ler1.OwnerOfClass<ILexEntry>();
				// try to generate analyses for matching entries (should have no results)
				setup.GuessServices.GenerateEntryGuesses(setup.StText);
				var guessVariantOfEntry = setup.GuessServices.GetBestGuess(setup.Words_para0[1]);
				Assert.AreNotEqual(new NullWAG(), guessVariantOfEntry);
				Assert.AreEqual(1, guessVariantOfEntry.Analysis.MorphBundlesOS.Count);
				Assert.AreEqual(variantOfEntry.LexemeFormOA, guessVariantOfEntry.Analysis.MorphBundlesOS[0].MorphRA);
				Assert.AreEqual(mainEntry.SensesOS[0], guessVariantOfEntry.Analysis.MorphBundlesOS[0].SenseRA);
				Assert.AreEqual(mainEntry.SensesOS[0].MorphoSyntaxAnalysisRA, guessVariantOfEntry.Analysis.MorphBundlesOS[0].MsaRA);
				Assert.AreEqual(mainEntry.SensesOS[0].Gloss.BestAnalysisAlternative.Text,
								guessVariantOfEntry.Analysis.MeaningsOC.First().Form.BestAnalysisAlternative.Text);
				Assert.AreEqual(setup.Pos_noun, guessVariantOfEntry.Analysis.CategoryRA);
				int cAnalyses = Cache.ServiceLocator.GetInstance<IWfiAnalysisRepository>().Count;
				Assert.AreEqual(1, cAnalyses, "Should have only generated one computer guess analysis.");
			}
		}

		/// <summary>
		/// make an variant entry with its own sense/gloss that should be returned as a guess.
		/// </summary>
		[Test]
		public void ExpectedGuessForWord_MatchingVariantofSense()
		{
			using (var setup = new AnalysisGuessBaseSetup(Cache,
				AnalysisGuessBaseSetup.Flags.PartsOfSpeech, AnalysisGuessBaseSetup.Flags.VariantEntryTypes))
			{
				// create an affix entry
				var mainEntry = setup.EntryFactory.Create("aMain", "astem", SandboxGenericMSA.Create(MsaType.kStem, setup.Pos_noun));
				ILexEntryRef ler = mainEntry.SensesOS[0].CreateVariantEntryAndBackRef(setup.Vet_FreeVariant, TsStringUtils.MakeTss("a", Cache.DefaultVernWs));
				var variantOfSense = ler.OwnerOfClass<ILexEntry>();
				// try to generate analyses for matching entries (should have no results)
				setup.GuessServices.GenerateEntryGuesses(setup.StText);
				var guessVariantOfSense = setup.GuessServices.GetBestGuess(setup.Words_para0[1]);
				Assert.AreNotEqual(new NullWAG(), guessVariantOfSense);
				Assert.AreEqual(1, guessVariantOfSense.Analysis.MorphBundlesOS.Count);
				// (LT-9681) Not sure how the MorphRA/SenseRA/MsaRA data should be represented for this case.
				// typically for variants, MorphRA points to the variant, and SenseRA points to the primary entry's sense.
				// in this case, perhaps the SenseRA should point to the variant's sense.
				Assert.AreEqual(variantOfSense.LexemeFormOA, guessVariantOfSense.Analysis.MorphBundlesOS[0].MorphRA);
				Assert.AreEqual(mainEntry.SensesOS[0], guessVariantOfSense.Analysis.MorphBundlesOS[0].SenseRA);
				Assert.AreEqual(mainEntry.SensesOS[0].MorphoSyntaxAnalysisRA, guessVariantOfSense.Analysis.MorphBundlesOS[0].MsaRA);
				Assert.AreEqual(mainEntry.SensesOS[0].Gloss.BestAnalysisAlternative.Text,
								guessVariantOfSense.Analysis.MeaningsOC.First().Form.BestAnalysisAlternative.Text);
				Assert.AreEqual(setup.Pos_noun, guessVariantOfSense.Analysis.CategoryRA);
				int cAnalyses = Cache.ServiceLocator.GetInstance<IWfiAnalysisRepository>().Count;
				Assert.AreEqual(1, cAnalyses, "Should have only generated one computer guess analysis.");
			}
		}

		/// <summary>
		/// make an variant entry with its own sense/gloss that should be returned as a guess.
		/// </summary>
		[Ignore("support for LT-9681. Not sure how the MorphRA/SenseRA/MsaRA data should be represented for this case.")]
		[Test]
		public void ExpectedGuessForWord_MatchingVariantHavingSense()
		{
			using (var setup = new AnalysisGuessBaseSetup(Cache,
				AnalysisGuessBaseSetup.Flags.PartsOfSpeech, AnalysisGuessBaseSetup.Flags.VariantEntryTypes))
			{
				// create an affix entry
				var mainEntry = setup.EntryFactory.Create("aMain", "astem", SandboxGenericMSA.Create(MsaType.kStem, setup.Pos_noun));
				ILexEntryRef ler = mainEntry.CreateVariantEntryAndBackRef(setup.Vet_FreeVariant, TsStringUtils.MakeTss("a", Cache.DefaultVernWs));
				var variantOfEntry = ler.OwnerOfClass<ILexEntry>();
				// make the variant have it's own gloss...should take precendence over main entry gloss info (cf. LT-9681)
				var senseFactory = Cache.ServiceLocator.GetInstance<ILexSenseFactory>();
				senseFactory.Create(variantOfEntry, SandboxGenericMSA.Create(MsaType.kStem, setup.Pos_verb), "variantOfSenseGloss");
				// try to generate analyses for matching entries (should have no results)
				setup.GuessServices.GenerateEntryGuesses(setup.StText);
				var guessVariantOfSense = setup.GuessServices.GetBestGuess(setup.Words_para0[1]);
				Assert.AreNotEqual(new NullWAG(), guessVariantOfSense);
				Assert.AreEqual(1, guessVariantOfSense.Analysis.MorphBundlesOS.Count);
				// (LT-9681) Not sure how the MorphRA/SenseRA/MsaRA data should be represented for this case.
				// typically for variants, MorphRA points to the variant, and SenseRA points to the primary entry's sense.
				// in this case, perhaps the SenseRA should point to the variant's sense.
				Assert.AreEqual(variantOfEntry.LexemeFormOA, guessVariantOfSense.Analysis.MorphBundlesOS[0].MorphRA);
				Assert.AreEqual(variantOfEntry.SensesOS[0], guessVariantOfSense.Analysis.MorphBundlesOS[0].SenseRA);
				Assert.AreEqual(variantOfEntry.SensesOS[0].MorphoSyntaxAnalysisRA, guessVariantOfSense.Analysis.MorphBundlesOS[0].MsaRA);
				Assert.AreEqual(variantOfEntry.SensesOS[0].Gloss.BestAnalysisAlternative.Text,
								guessVariantOfSense.Analysis.MeaningsOC.First().Form.BestAnalysisAlternative.Text);
				Assert.AreEqual(setup.Pos_noun, guessVariantOfSense.Analysis.CategoryRA);
				int cAnalyses = Cache.ServiceLocator.GetInstance<IWfiAnalysisRepository>().Count;
				Assert.AreEqual(1, cAnalyses, "Should have only generated one computer guess analysis.");
			}
		}

		/// <summary>
		/// make an approved analysis, expected to be a guess.
		/// </summary>
		[Test]
		public void ExpectedGuess_OneAnalysis_HumanApproves()
		{
			using (var setup = new AnalysisGuessBaseSetup(Cache))
			{
				var newWag = WordAnalysisOrGlossServices.CreateNewAnalysisWAG(setup.Words_para0[1]);
				setup.UserAgent.SetEvaluation(newWag.Analysis, Opinions.approves);
				var guessActual = setup.GuessServices.GetBestGuess(setup.Words_para0[1]);
				Assert.AreEqual(newWag.Analysis, guessActual);
			}
		}

		/// <summary>
		/// make an human approved analysis (but parser disapproves), expected to be a guess.
		/// </summary>
		[Test]
		public void ExpectedGuess_OneAnalysis_HumanApproves_ParserDisapproves()
		{
			using (var setup = new AnalysisGuessBaseSetup(Cache))
			{
				var newWag = WordAnalysisOrGlossServices.CreateNewAnalysisWAG(setup.Words_para0[1]);
				setup.ParserAgent.SetEvaluation(newWag.Analysis, Opinions.disapproves);
				setup.UserAgent.SetEvaluation(newWag.Analysis, Opinions.approves);
				var guessActual = setup.GuessServices.GetBestGuess(setup.Words_para0[1]);
				Assert.AreEqual(newWag.Analysis, guessActual);
			}
		}

		/// <summary>
		/// make a gloss with an approved analysis, expected to be a guess.
		/// </summary>
		[Test]
		public void ExpectedGuessForWord_OneGloss_HumanApprovesAnalysis()
		{
			using (var setup = new AnalysisGuessBaseSetup(Cache))
			{
				var newWagGloss = WordAnalysisOrGlossServices.CreateNewAnalysisTreeGloss(setup.Words_para0[1]);
				setup.UserAgent.SetEvaluation(newWagGloss.WfiAnalysis, Opinions.approves);
				var guessActual = setup.GuessServices.GetBestGuess(setup.Words_para0[1]);
				Assert.AreEqual(newWagGloss.Gloss, guessActual);
			}
		}

		/// <summary>
		/// make a gloss with an approved analysis, expected to be a guess.
		/// </summary>
		[Test]
		public void ExpectedGuessForAnalysis_OneGloss_HumanApprovesAnalysis()
		{
			using (var setup = new AnalysisGuessBaseSetup(Cache))
			{
				var newWagGloss = WordAnalysisOrGlossServices.CreateNewAnalysisTreeGloss(setup.Words_para0[1]);
				setup.UserAgent.SetEvaluation(newWagGloss.WfiAnalysis, Opinions.approves);
				var guessActual = setup.GuessServices.GetBestGuess(newWagGloss.WfiAnalysis);
				Assert.AreEqual(newWagGloss.Gloss, guessActual);
			}
		}

		/// <summary>
		/// make an approved analysis, expected to be a guess.
		/// </summary>
		[Test]
		public void ExpectedGuess_OneAnalysis_ParserApproves()
		{
			using (var setup = new AnalysisGuessBaseSetup(Cache))
			{
				var newWag = WordAnalysisOrGlossServices.CreateNewAnalysisWAG(setup.Words_para0[1]);
				var newAnalysis = newWag.Analysis;
				setup.ParserAgent.SetEvaluation(newAnalysis, Opinions.approves);
				var guessActual = setup.GuessServices.GetBestGuess(setup.Words_para0[1]);
				Assert.AreEqual(newWag.Analysis, guessActual);
			}
		}

		/// <summary>
		/// make an analysis with an "noopinion" evaluation, expected to be a guess.
		/// </summary>
		[Test]
		public void ExpectedGuess_OneAnalysis_NoOpinion()
		{
			using (var setup = new AnalysisGuessBaseSetup(Cache))
			{
				var newWag = WordAnalysisOrGlossServices.CreateNewAnalysisWAG(setup.Words_para0[1]);
				var newAnalysis = newWag.Analysis;
				setup.UserAgent.SetEvaluation(newAnalysis, Opinions.noopinion);
				var guessActual = setup.GuessServices.GetBestGuess(setup.Words_para0[1]);
				Assert.AreEqual(newWag.Analysis, guessActual);
			}
		}

		/// <summary>
		/// make a gloss with analysis with an "noopinion" evaluation, expected to be a guess.
		/// </summary>
		[Test]
		public void ExpectedGuessForWord_OneGloss_NoOpinion()
		{
			using (var setup = new AnalysisGuessBaseSetup(Cache))
			{
				var newWagGloss = WordAnalysisOrGlossServices.CreateNewAnalysisTreeGloss(setup.Words_para0[1]);
				setup.UserAgent.SetEvaluation(newWagGloss.WfiAnalysis, Opinions.noopinion); // should be equivalent to no evaluation.
				var guessActual = setup.GuessServices.GetBestGuess(setup.Words_para0[1]);
				Assert.AreEqual(newWagGloss.Gloss, guessActual);
			}
		}

		/// <summary>
		/// make a gloss with analysis with an "noopinion" evaluation, expected to be a guess.
		/// </summary>
		[Test]
		public void ExpectedGuessForAnalysis_OneGloss_NoOpinion()
		{
			using (var setup = new AnalysisGuessBaseSetup(Cache))
			{
				var newWagGloss = WordAnalysisOrGlossServices.CreateNewAnalysisTreeGloss(setup.Words_para0[1]);
				setup.UserAgent.SetEvaluation(newWagGloss.WfiAnalysis, Opinions.noopinion); // should be equivalent to no evaluation.
				var guessActual = setup.GuessServices.GetBestGuess(newWagGloss.WfiAnalysis);
				Assert.AreEqual(newWagGloss.Gloss, guessActual);
			}
		}

		/// <summary>
		/// Not sure which to choose is right if they are all equally approved.
		/// Just make sure we return something.
		/// </summary>
		[Test]
		public void ExpectedGuess_MultipleAnalyses_EquallyApproved_NoneInTexts()
		{
			using (var setup = new AnalysisGuessBaseSetup(Cache))
			{
				var newAnalysisWag1 = WordAnalysisOrGlossServices.CreateNewAnalysisWAG(setup.Words_para0[1]);
				var newAnalysisWag2 = WordAnalysisOrGlossServices.CreateNewAnalysisWAG(setup.Words_para0[1]);
				var newAnalysis1 = newAnalysisWag1.Analysis;
				var newAnalysis2 = newAnalysisWag2.Analysis;
				setup.UserAgent.SetEvaluation(newAnalysis1, Opinions.approves);
				setup.UserAgent.SetEvaluation(newAnalysis2, Opinions.approves);
				var guessActual = setup.GuessServices.GetBestGuess(setup.Words_para0[1]);
				Assert.AreNotEqual(new NullWAG(), guessActual);
			}
		}

		/// <summary>
		/// Prefer an analysis 'approved' to one created without an evaluation.
		/// </summary>
		[Test]
		public void ExpectedGuess_MultipleAnalyses_PreferOneApprovedToNoEvaluation()
		{
			using (var setup = new AnalysisGuessBaseSetup(Cache))
			{
				var newWagEvaluation = WordAnalysisOrGlossServices.CreateNewAnalysisWAG(setup.Words_para0[1]);
				var newWagNoEvaluation = WordAnalysisOrGlossServices.CreateNewAnalysisWAG(setup.Words_para0[1]);
				setup.UserAgent.SetEvaluation(newWagEvaluation.Analysis, Opinions.approves);
				var guessActual = setup.GuessServices.GetBestGuess(setup.Words_para0[1]);
				Assert.AreEqual(newWagEvaluation.Analysis, guessActual);
			}
		}

		/// <summary>
		/// Prefer a matching entry parser generated guess over a (computer) guess matching lexical entry.
		/// </summary>
		[Test]
		public void ExpectedGuess_MultipleAnalyses_PreferParserAgentGuessOverMatchingEntryGuess()
		{
			using (var setup = new AnalysisGuessBaseSetup(Cache, AnalysisGuessBaseSetup.Flags.PartsOfSpeech))
			{
				// create an affix entry
				var expectedMatch = setup.EntryFactory.Create("a", "astem", SandboxGenericMSA.Create(MsaType.kStem, setup.Pos_noun));
				// expect a guess to be generated
				setup.GuessServices.GenerateEntryGuesses(setup.StText);
				// create parser approved guess
				var newWagParserApproves = WordAnalysisOrGlossServices.CreateNewAnalysisWAG(setup.Words_para0[1]);
				setup.ParserAgent.SetEvaluation(newWagParserApproves.Analysis, Opinions.approves);
				var guessActual = setup.GuessServices.GetBestGuess(setup.Words_para0[1]);
				Assert.AreEqual(newWagParserApproves.Analysis, guessActual);
			}
		}

		/// <summary>
		/// Prefer an user agent analysis to one created as a matching entry
		/// </summary>
		[Test]
		public void ExpectedGuess_MultipleAnalyses_PreferUserAgentGuessOverMatchingEntryGuess()
		{
			using (var setup = new AnalysisGuessBaseSetup(Cache, AnalysisGuessBaseSetup.Flags.PartsOfSpeech))
			{
				// create an affix entry
				var expectedMatch = setup.EntryFactory.Create("a", "astem", SandboxGenericMSA.Create(MsaType.kStem, setup.Pos_noun));
				// expect a guess to be generated
				setup.GuessServices.GenerateEntryGuesses(setup.StText);
				// create user approved guess
				var newWagUserApproves = WordAnalysisOrGlossServices.CreateNewAnalysisWAG(setup.Words_para0[1]);
				setup.UserAgent.SetEvaluation(newWagUserApproves.Analysis, Opinions.approves);
				var guessActual = setup.GuessServices.GetBestGuess(setup.Words_para0[1]);
				Assert.AreEqual(newWagUserApproves.Analysis, guessActual);
			}
		}

		/// <summary>
		/// Prefer an analysis approved in a text to one approved outside of text.
		/// (All other things equal, it makes more sense when guessing for a wordform to use analyses
		/// approved in text than one approved in analyses.)
		/// </summary>
		[Test]
		public void ExpectedAnalysisGuess_MultipleAnalyses_PreferOneApprovedInText()
		{
			using (var setup = new AnalysisGuessBaseSetup(Cache))
			{
				var newWagOutsideTexts = WordAnalysisOrGlossServices.CreateNewAnalysisWAG(setup.Words_para0[1]);
				var newWagInText = WordAnalysisOrGlossServices.CreateNewAnalysisWAG(setup.Words_para0[1]);
				// set the analysis at the appropriate location to be the one we created.
				setup.Para0.SetAnalysis(0, 1, newWagInText.Analysis);
				var guessActual = setup.GuessServices.GetBestGuess(setup.Words_para0[1]);
				Assert.AreEqual(newWagInText.Analysis, guessActual);
			}
		}

		/// <summary>
		/// Prefer an analysis 'approved' in the text to one created as a matching entry
		/// </summary>
		[Test]
		public void ExpectedGuess_MultipleAnalyses_PreferOneApprovedInTextToMatchingEntry()
		{
			using (var setup = new AnalysisGuessBaseSetup(Cache, AnalysisGuessBaseSetup.Flags.PartsOfSpeech))
			{
				// create an affix entry
				var expectedMatch = setup.EntryFactory.Create("a", "astem", SandboxGenericMSA.Create(MsaType.kStem, setup.Pos_noun));
				// expect a guess to be generated
				setup.GuessServices.GenerateEntryGuesses(setup.StText);
				// create user approved guess
				var newWagInText = WordAnalysisOrGlossServices.CreateNewAnalysisWAG(setup.Words_para0[1]);
				// set the analysis at the appropriate location to be the one we created.
				setup.Para0.SetAnalysis(0, 1, newWagInText.Analysis);
				var guessActual = setup.GuessServices.GetBestGuess(setup.Words_para0[1]);
				Assert.AreEqual(newWagInText.Analysis, guessActual);
			}
		}

		/// <summary>
		/// Prefer an analysis approved in a text to a gloss outside of text.
		/// (All other things equal, it makes more sense when guessing for a wordform to use analyses
		/// approved in text than one approved in analyses.)
		/// </summary>
		[Test]
		public void ExpectedAnalysisGuess_MultipleAnalyses_PreferAnalysisInText()
		{
			using (var setup = new AnalysisGuessBaseSetup(Cache))
			{
				var newWagOutsideTexts = WordAnalysisOrGlossServices.CreateNewAnalysisTreeGloss(setup.Words_para0[1]);
				var newWagInText = WordAnalysisOrGlossServices.CreateNewAnalysisWAG(setup.Words_para0[1]);
				// set the analysis at the appropriate location to be the one we created.
				setup.Para0.SetAnalysis(0, 1, newWagInText.Analysis);
				var guessActual = setup.GuessServices.GetBestGuess(setup.Words_para0[1]);
				Assert.AreEqual(newWagInText.Analysis, guessActual);
			}
		}


		/// <summary>
		/// If a wordform is in a sentence initial position (and non-lowercase), prefer a guess for
		/// the lowercase form.
		/// </summary>
		[Test]
		public void ExpectedAnalysisGuess_ForSentenceInitialPositionLowerCaseAlternative()
		{
			using (var setup = new AnalysisGuessBaseSetup(Cache))
			{
				var newWagUppercase = WordAnalysisOrGlossServices.CreateNewAnalysisTreeGloss(setup.Words_para0[0]);
				var newWagLowercase = WordAnalysisOrGlossServices.CreateNewAnalysisWAG(setup.Words_para0[1]);
				var wagUppercase = new AnalysisOccurrence(setup.Para0.SegmentsOS[0], 0);
				var guessActual = setup.GuessServices.GetBestGuess(wagUppercase);
				Assert.AreEqual(newWagLowercase.Analysis, guessActual);
			}

		}

		/// <summary>
		/// if a wordform is in a sentence initial position (and non-lowercase),
		/// default to looking for the upper case form when lower isn't found
		/// </summary>
		[Test]
		public void ExpectedAnalysisGuess_ForSentenceInitialPositionUpperCaseAlternative()
		{
			using (var setup = new AnalysisGuessBaseSetup(Cache))
			{
				var newWagUppercase = WordAnalysisOrGlossServices.CreateNewAnalysisWAG(setup.Words_para0[0]);
				var wagUppercase = new AnalysisOccurrence(setup.Para0.SegmentsOS[0],0);
				var guessActual = setup.GuessServices.GetBestGuess(wagUppercase);
				Assert.AreEqual(newWagUppercase.Analysis, guessActual);
			}

		}

		/// <summary>
		/// if a wordform is in a sentence initial position and lowercase,
		/// don't default to looking for the upper case form when lower isn't found
		/// </summary>
		[Test]
		public void ExpectedAnalysisGuess_ForSentenceInitialOnlyLowercase()
		{
			using (var setup = new AnalysisGuessBaseSetup(Cache))
			{
				var wagUppercaseB = WordAnalysisOrGlossServices.CreateNewAnalysisWAG(setup.Words_para0[5]);
				var wagLowercaseB = new AnalysisOccurrence(setup.Para0.SegmentsOS[1], 0);
				var guessActual = setup.GuessServices.GetBestGuess(wagLowercaseB);
				Assert.AreEqual(new NullWAG(), guessActual);
			}
		}

		/// <summary>
		/// This class allows us to fake out the guesser by passing an analysis occurrence with the analyis we want,
		/// even though it isn't the analysis recorded in the paragraph.
		/// Since we haven't ensured consistency of any other properties (like baseline text), be careful how you use this.
		/// </summary>
		class TestModAnalysisOccurrence : AnalysisOccurrence
		{
			private IAnalysis m_trickAnalysis;
			public TestModAnalysisOccurrence(ISegment seg, int index, IAnalysis trickAnalysis) : base(seg, index)
			{
				m_trickAnalysis = trickAnalysis;
			}

			public override IAnalysis Analysis
			{
				get { return m_trickAnalysis; }
			}
		}

		/// <summary>
		/// if an uppercase wordform is in a sentence initial position and already has an analysis
		/// don't default to looking for the lowercase form
		/// </summary>
		[Test]
		public void ExpectedAnalysisGuess_ForSentenceInitialUppercaseWithAnalysis()
		{
			using (var setup = new AnalysisGuessBaseSetup(Cache))
			{
				var newWagUppercase = WordAnalysisOrGlossServices.CreateNewAnalysisTreeGloss(setup.Words_para0[0]);
				WordAnalysisOrGlossServices.CreateNewAnalysisTreeGloss(setup.Words_para0[1]);
				var wagUppercase = new TestModAnalysisOccurrence(setup.Para0.SegmentsOS[0], 0, newWagUppercase.WfiAnalysis);
				var guessActual = setup.GuessServices.GetBestGuess(wagUppercase);
				Assert.AreEqual(newWagUppercase.Gloss, guessActual);
			}
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void ExpectedAnalysisGuessForWord_GlossOfApprovedAnalysis()
		{
			using (var setup = new AnalysisGuessBaseSetup(Cache))
			{
				var newWagApproves = WordAnalysisOrGlossServices.CreateNewAnalysisTreeGloss(setup.Words_para0[1]);
				// set the analysis at the appropriate location to be the one we created.
				setup.Para0.SetAnalysis(0, 1, newWagApproves.Gloss);
				setup.UserAgent.SetEvaluation(newWagApproves.WfiAnalysis, Opinions.approves);
				var guessActual = setup.GuessServices.GetBestGuess(setup.Words_para0[1]);
				Assert.AreEqual(newWagApproves.Gloss, guessActual);
			}
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void ExpectedAnalysisGuessForAnalysis_GlossOfApprovedAnalysis()
		{
			using (var setup = new AnalysisGuessBaseSetup(Cache))
			{
				var newWagApproves = WordAnalysisOrGlossServices.CreateNewAnalysisTreeGloss(setup.Words_para0[1]);
				setup.Para0.SetAnalysis(0, 1, newWagApproves.Gloss);
				setup.UserAgent.SetEvaluation(newWagApproves.WfiAnalysis, Opinions.approves);
				var guessActual = setup.GuessServices.GetBestGuess(newWagApproves.WfiAnalysis);
				Assert.AreEqual(newWagApproves.Gloss, guessActual);
			}
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void ExpectedAnalysisGuessForWord_PreferOneGlossOverOneAnalysis()
		{
			using (var setup = new AnalysisGuessBaseSetup(Cache))
			{
				var newWagApproves = WordAnalysisOrGlossServices.CreateNewAnalysisTreeGloss(setup.Words_para0[1]);
				setup.Para0.SetAnalysis(0, 1, newWagApproves.WfiAnalysis);
				setup.Para0.SetAnalysis(0, 2, newWagApproves.Gloss);
				setup.UserAgent.SetEvaluation(newWagApproves.WfiAnalysis, Opinions.approves);
				var guessActual = setup.GuessServices.GetBestGuess(setup.Words_para0[1]);
				Assert.AreEqual(newWagApproves.Gloss, guessActual);
			}
		}

		/// <summary>
		/// corner case
		/// </summary>
		[Test]
		public void ExpectedAnalysisGuessForAnalysis_PreferOneGlossOverOneAnalysis()
		{
			using (var setup = new AnalysisGuessBaseSetup(Cache))
			{
				var newWagApproves = WordAnalysisOrGlossServices.CreateNewAnalysisTreeGloss(setup.Words_para0[1]);
				setup.Para0.SetAnalysis(0, 1, newWagApproves.WfiAnalysis);
				setup.Para0.SetAnalysis(0, 2, newWagApproves.Gloss);
				setup.UserAgent.SetEvaluation(newWagApproves.WfiAnalysis, Opinions.approves);
				var guessActual = setup.GuessServices.GetBestGuess(newWagApproves.WfiAnalysis);
				Assert.AreEqual(newWagApproves.Gloss, guessActual);
			}
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void ExpectedAnalysisGuessForWord_PreferFrequentAnalysisOverLessFrequentGloss()
		{
			using (var setup = new AnalysisGuessBaseSetup(Cache))
			{
				var newWagApproves = WordAnalysisOrGlossServices.CreateNewAnalysisTreeGloss(setup.Words_para0[1]);
				setup.Para0.SetAnalysis(0, 1, newWagApproves.Gloss);
				setup.Para0.SetAnalysis(0, 2, newWagApproves.WfiAnalysis);
				setup.Para0.SetAnalysis(0, 3, newWagApproves.WfiAnalysis);
				setup.UserAgent.SetEvaluation(newWagApproves.WfiAnalysis, Opinions.approves);
				var guessActual = setup.GuessServices.GetBestGuess(setup.Words_para0[1]);
				Assert.AreEqual(newWagApproves.WfiAnalysis, guessActual);
			}
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void ExpectedAnalysisGuess_PreferFrequentGlossOverLessFrequentAnalysis()
		{
			using (var setup = new AnalysisGuessBaseSetup(Cache))
			{
				var newWagApproves = WordAnalysisOrGlossServices.CreateNewAnalysisTreeGloss(setup.Words_para0[1]);
				setup.Para0.SetAnalysis(0, 1, newWagApproves.WfiAnalysis);
				setup.Para0.SetAnalysis(0, 2, newWagApproves.Gloss);
				setup.Para0.SetAnalysis(0, 3, newWagApproves.Gloss);
				setup.UserAgent.SetEvaluation(newWagApproves.WfiAnalysis, Opinions.approves);
				var guessActual = setup.GuessServices.GetBestGuess(setup.Words_para0[1]);
				Assert.AreEqual(newWagApproves.Gloss, guessActual);
			}
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void ExpectedAnalysisGuessForWord_PreferFrequentGlossOverLessFrequentGloss()
		{
			using (var setup = new AnalysisGuessBaseSetup(Cache))
			{
				var newWagFrequentGloss = WordAnalysisOrGlossServices.CreateNewAnalysisTreeGloss(setup.Words_para0[1]);
				var newWagLessFrequentGloss = WordAnalysisOrGlossServices.CreateNewAnalysisTreeGloss(setup.Words_para0[1]);
				setup.Para0.SetAnalysis(0, 1, newWagLessFrequentGloss.Gloss);
				setup.Para0.SetAnalysis(0, 2, newWagFrequentGloss.Gloss);
				setup.Para0.SetAnalysis(0, 3, newWagFrequentGloss.Gloss);
				setup.UserAgent.SetEvaluation(newWagLessFrequentGloss.WfiAnalysis, Opinions.approves);
				setup.UserAgent.SetEvaluation(newWagFrequentGloss.WfiAnalysis, Opinions.approves);
				var guessActual = setup.GuessServices.GetBestGuess(setup.Words_para0[1]);
				Assert.AreEqual(newWagFrequentGloss.Gloss, guessActual);
			}
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void ExpectedAnalysisGuessForAnalysis_PreferFrequentGlossOverLessFrequentGloss()
		{
			using (var setup = new AnalysisGuessBaseSetup(Cache))
			{
				var newWagFrequentGloss = WordAnalysisOrGlossServices.CreateNewAnalysisTreeGloss(setup.Words_para0[1]);
				var newWagLessFrequentGloss = WordAnalysisOrGlossServices.CreateNewAnalysisTreeGloss(setup.Words_para0[1]);
				setup.Para0.SetAnalysis(0, 1, newWagLessFrequentGloss.Gloss);
				setup.Para0.SetAnalysis(0, 2, newWagFrequentGloss.Gloss);
				setup.Para0.SetAnalysis(0, 3, newWagFrequentGloss.Gloss);
				setup.UserAgent.SetEvaluation(newWagLessFrequentGloss.WfiAnalysis, Opinions.approves);
				setup.UserAgent.SetEvaluation(newWagFrequentGloss.WfiAnalysis, Opinions.approves);
				var guessActual = setup.GuessServices.GetBestGuess(newWagFrequentGloss.WfiAnalysis);
				Assert.AreEqual(newWagFrequentGloss.Gloss, guessActual);
			}
		}

		/// <summary>
		/// Make sure we don't select the disapproved analysis
		/// </summary>
		[Test]
		public void ExpectedGuess_MultipleAnalyses_MostApprovedAfterDisapproved()
		{
			using (var setup = new AnalysisGuessBaseSetup(Cache))
			{
				var newWagDisapproved = WordAnalysisOrGlossServices.CreateNewAnalysisWAG(setup.Words_para0[1]);
				var newWagApproved = WordAnalysisOrGlossServices.CreateNewAnalysisWAG(setup.Words_para0[1]);
				setup.UserAgent.SetEvaluation(newWagDisapproved.Analysis, Opinions.disapproves);
				setup.UserAgent.SetEvaluation(newWagApproved.Analysis, Opinions.approves);
				var guessActual = setup.GuessServices.GetBestGuess(setup.Words_para0[1]);
				Assert.AreEqual(newWagApproved.Analysis, guessActual);
			}
		}

		/// <summary>
		/// </summary>
		[Test]
		public void ExpectedGuess_PreferUserApprovedAnalysisOverParserApprovedAnalysis()
		{
			using (var setup = new AnalysisGuessBaseSetup(Cache))
			{
				var newWagParserApproves = WordAnalysisOrGlossServices.CreateNewAnalysisWAG(setup.Words_para0[1]);
				var newWagHumanApproves = WordAnalysisOrGlossServices.CreateNewAnalysisWAG(setup.Words_para0[1]);
				setup.ParserAgent.SetEvaluation(newWagParserApproves.Analysis, Opinions.approves);
				setup.UserAgent.SetEvaluation(newWagHumanApproves.Analysis, Opinions.approves);
				var guessActual = setup.GuessServices.GetBestGuess(setup.Words_para0[1]);
				Assert.AreEqual(newWagHumanApproves.Analysis, guessActual);
			}
		}
	}

}
