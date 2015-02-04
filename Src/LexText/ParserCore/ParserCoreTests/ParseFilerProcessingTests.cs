// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ParseFilerProcessingTests.cs
// Responsibility: Randy Regnier
// Last reviewed:
//
// <remarks>
// Implements the ParseFilerProcessingTests unit tests.
// </remarks>
// buildtest ParseFiler-nodep

using System;
using System.Linq;
using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.Utils;

namespace SIL.FieldWorks.WordWorks.Parser
{
	/// <summary>
	/// Summary description for ParseFilerProcessingTests.
	/// </summary>
	[TestFixture]
	public class ParseFilerProcessingTests : MemoryOnlyBackendProviderTestBase
	{
		#region Data Members

		private ParseFiler m_filer;
		private IdleQueue m_idleQueue;
		private WritingSystem m_vernacularWS;
		private ILexEntryFactory m_entryFactory;
		private ILexSenseFactory m_senseFactory;
		private IMoStemAllomorphFactory m_stemAlloFactory;
		private IMoAffixAllomorphFactory m_afxAlloFactory;
		private IMoStemMsaFactory m_stemMsaFactory;
		private IMoInflAffMsaFactory m_inflAffMsaFactory;
		private ILexEntryRefFactory m_lexEntryRefFactory;
		private ILexEntryInflTypeFactory m_lexEntryInflTypeFactory;

		#endregion Data Members

		#region Non-test methods

		protected ICmAgent ParserAgent
		{
			get
			{
				return Cache.LanguageProject.DefaultParserAgent;
			}
		}

		protected ICmAgent HumanAgent
		{
			get
			{
				return Cache.LanguageProject.DefaultUserAgent;
			}
		}

		protected IWfiWordform CheckAnnotationSize(string form, int expectedSize, bool isStarting)
		{
			IFdoServiceLocator servLoc = Cache.ServiceLocator;
			IWfiWordform wf = FindOrCreateWordform(form);
			int actualSize =
				(from ann in servLoc.GetInstance<ICmBaseAnnotationRepository>().AllInstances()
				 where ann.BeginObjectRA == wf
				 select ann).Count();
				// wf.RefsFrom_CmBaseAnnotation_BeginObject.Count;
			string msg = String.Format("Wrong number of {0} annotations for: {1}", isStarting ? "starting" : "ending", form);
			Assert.AreEqual(expectedSize, actualSize, msg);
			return wf;
		}

		private IWfiWordform FindOrCreateWordform(string form)
		{
			IFdoServiceLocator servLoc = Cache.ServiceLocator;
			IWfiWordform wf = servLoc.GetInstance<IWfiWordformRepository>().GetMatchingWordform(m_vernacularWS.Handle, form);
			if (wf == null)
			{
				UndoableUnitOfWorkHelper.Do("Undo create", "Redo create", m_actionHandler,
					() => wf = servLoc.GetInstance<IWfiWordformFactory>().Create(Cache.TsStrFactory.MakeString(form, m_vernacularWS.Handle)));
			}
			return wf;
		}

		protected IWfiWordform CheckAnalysisSize(string form, int expectedSize, bool isStarting)
		{
			IWfiWordform wf = FindOrCreateWordform(form);
			int actualSize = wf.AnalysesOC.Count;
			string msg = String.Format("Wrong number of {0} analyses for: {1}", isStarting ? "starting" : "ending", form);
			Assert.AreEqual(expectedSize, actualSize, msg);
			return wf;
		}

		protected void CheckEvaluationSize(IWfiAnalysis analysis, int expectedSize, bool isStarting, string additionalMessage)
		{
			int actualSize = analysis.EvaluationsRC.Count;
			string msg = String.Format("Wrong number of {0} evaluations for analysis: {1} ({2})", isStarting ? "starting" : "ending", analysis.Hvo, additionalMessage);
			Assert.AreEqual(expectedSize, actualSize, msg);
		}

		protected void ExecuteIdleQueue()
		{
			foreach (var task in m_idleQueue)
				task.Delegate(task.Parameter);
			m_idleQueue.Clear();
		}

		#endregion // Non-tests

		#region Setup and TearDown

		public override void FixtureSetup()
		{
			base.FixtureSetup();
			m_vernacularWS = Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem;
			m_idleQueue = new IdleQueue {IsPaused = true};
			m_filer = new ParseFiler(Cache, task => {}, m_idleQueue, Cache.LanguageProject.DefaultParserAgent);
			m_entryFactory = Cache.ServiceLocator.GetInstance<ILexEntryFactory>();
			m_senseFactory = Cache.ServiceLocator.GetInstance<ILexSenseFactory>();
			m_stemAlloFactory = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>();
			m_afxAlloFactory = Cache.ServiceLocator.GetInstance<IMoAffixAllomorphFactory>();
			m_stemMsaFactory = Cache.ServiceLocator.GetInstance<IMoStemMsaFactory>();
			m_inflAffMsaFactory = Cache.ServiceLocator.GetInstance<IMoInflAffMsaFactory>();
			m_lexEntryRefFactory = Cache.ServiceLocator.GetInstance<ILexEntryRefFactory>();
			m_lexEntryInflTypeFactory = Cache.ServiceLocator.GetInstance<ILexEntryInflTypeFactory>();
		}

		public override void FixtureTeardown()
		{
			m_vernacularWS = null;
			m_filer = null;
			m_idleQueue.Dispose();
			m_idleQueue = null;
			m_entryFactory = null;
			m_senseFactory = null;
			m_stemAlloFactory = null;
			m_afxAlloFactory = null;
			m_stemMsaFactory = null;
			m_inflAffMsaFactory = null;
			m_lexEntryRefFactory = null;
			m_lexEntryInflTypeFactory = null;

			base.FixtureTeardown();
		}

		public override void TestTearDown()
		{
			UndoAll();
			base.TestTearDown();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// End the undoable UOW and Undo everything.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void UndoAll()
		{
			// Undo the UOW (or more than one of them, if the test made new ones).
			while (m_actionHandler.CanUndo())
				m_actionHandler.Undo();

			// Need to 'Commit' to clear out redo stack,
			// since nothing is really saved.
			m_actionHandler.Commit();
		}

		#endregion Setup and TearDown

		#region Tests

		[Test]
		public void TooManyAnalyses()
		{
			IWfiWordform bearsTest = CheckAnnotationSize("bearsTEST", 0, true);
			var result = new ParseResult("Maximum permitted analyses (448) reached.");
			m_filer.ProcessParse(bearsTest, ParserPriority.Low, result);
			ExecuteIdleQueue();
			CheckAnnotationSize("bearsTEST", 1, false);
		}

		[Test]
		public void BufferOverrun()
		{
			IWfiWordform dogsTest = CheckAnnotationSize("dogsTEST", 0, true);
			var result = new ParseResult("Maximum internal buffer size (117) reached.");
			m_filer.ProcessParse(dogsTest, ParserPriority.Low, result);
			ExecuteIdleQueue();
			CheckAnnotationSize("dogsTEST", 1, false);
		}

		[Test]
		public void TwoAnalyses()
		{
			IWfiWordform catsTest = CheckAnalysisSize("catsTEST", 0, true);
			ILexDb ldb = Cache.LanguageProject.LexDbOA;

			ParseResult result = null;
			UndoableUnitOfWorkHelper.Do("Undo stuff", "Redo stuff", m_actionHandler, () =>
			{
				// Noun
				ILexEntry catN = m_entryFactory.Create();
				IMoStemAllomorph catNForm = m_stemAlloFactory.Create();
				catN.AlternateFormsOS.Add(catNForm);
				catNForm.Form.VernacularDefaultWritingSystem = Cache.TsStrFactory.MakeString("catNTEST", m_vernacularWS.Handle);
				IMoStemMsa catNMsa = m_stemMsaFactory.Create();
				catN.MorphoSyntaxAnalysesOC.Add(catNMsa);

				ILexEntry sPl = m_entryFactory.Create();
				IMoAffixAllomorph sPlForm = m_afxAlloFactory.Create();
				sPl.AlternateFormsOS.Add(sPlForm);
				sPlForm.Form.VernacularDefaultWritingSystem = Cache.TsStrFactory.MakeString("sPLTEST", m_vernacularWS.Handle);
				IMoInflAffMsa sPlMsa = m_inflAffMsaFactory.Create();
				sPl.MorphoSyntaxAnalysesOC.Add(sPlMsa);

				// Verb
				ILexEntry catV = m_entryFactory.Create();
				IMoStemAllomorph catVForm = m_stemAlloFactory.Create();
				catV.AlternateFormsOS.Add(catVForm);
				catVForm.Form.VernacularDefaultWritingSystem = Cache.TsStrFactory.MakeString("catVTEST", m_vernacularWS.Handle);
				IMoStemMsa catVMsa = m_stemMsaFactory.Create();
				catV.MorphoSyntaxAnalysesOC.Add(catVMsa);

				ILexEntry sAgr = m_entryFactory.Create();
				IMoAffixAllomorph sAgrForm = m_afxAlloFactory.Create();
				sAgr.AlternateFormsOS.Add(sAgrForm);
				sAgrForm.Form.VernacularDefaultWritingSystem = Cache.TsStrFactory.MakeString("sAGRTEST", m_vernacularWS.Handle);
				IMoInflAffMsa sAgrMsa = m_inflAffMsaFactory.Create();
				sAgr.MorphoSyntaxAnalysesOC.Add(sAgrMsa);

			result = new ParseResult(new[]
				{
					new ParseAnalysis(new[]
						{
							new ParseMorph(catNForm, catNMsa),
							new ParseMorph(sPlForm, sPlMsa)
						}),
					new ParseAnalysis(new[]
						{
							new ParseMorph(catVForm, catVMsa),
							new ParseMorph(sAgrForm, sAgrMsa)
						})
				});
			});
			m_filer.ProcessParse(catsTest, ParserPriority.Low, result);
			ExecuteIdleQueue();
			CheckAnalysisSize("catsTEST", 2, false);
		}

		[Test]
		[Ignore("Is it ever possible for a parser to return more than one wordform parse?")]
		public void TwoWordforms()
		{
			IWfiWordform snake = CheckAnalysisSize("snakeTEST", 0, true);
			IWfiWordform bull = CheckAnalysisSize("bullTEST", 0, true);
			ILexDb ldb = Cache.LanguageProject.LexDbOA;

			ParseResult result = null;
			UndoableUnitOfWorkHelper.Do("Undo stuff", "Redo stuff", m_actionHandler, () =>
			{
				// Snake
				ILexEntry snakeN = m_entryFactory.Create();
				IMoStemAllomorph snakeNForm = m_stemAlloFactory.Create();
				snakeN.AlternateFormsOS.Add(snakeNForm);
				snakeNForm.Form.VernacularDefaultWritingSystem = Cache.TsStrFactory.MakeString("snakeNTEST", m_vernacularWS.Handle);
				IMoStemMsa snakeNMsa = m_stemMsaFactory.Create();
				snakeN.MorphoSyntaxAnalysesOC.Add(snakeNMsa);

				// Bull
				ILexEntry bullN = m_entryFactory.Create();
				IMoStemAllomorph bullNForm = m_stemAlloFactory.Create();
				bullN.AlternateFormsOS.Add(bullNForm);
				bullNForm.Form.VernacularDefaultWritingSystem = Cache.TsStrFactory.MakeString("bullNTEST", m_vernacularWS.Handle);
				IMoStemMsa bullNMsa = m_stemMsaFactory.Create();
				bullN.MorphoSyntaxAnalysesOC.Add(bullNMsa);

				result = new ParseResult(new[]
					{
						new ParseAnalysis(new[]
							{
								new ParseMorph(snakeNForm, snakeNMsa)
							}),
						new ParseAnalysis(new[]
							{
								new ParseMorph(bullNForm, bullNMsa)
							})
					});
			});

			m_filer.ProcessParse(snake, ParserPriority.Low, result);
			ExecuteIdleQueue();
			CheckAnalysisSize("snakeTEST", 1, false);
			CheckAnalysisSize("bullTEST", 1, false);
		}

		/// <summary>
		/// Ensure analyses with 'duplicate' analyses are both approved.
		/// "Duplicate" means the MSA and MoForm IDs are the same in two different analyses.
		/// </summary>
		[Test]
		public void DuplicateAnalysesApproval()
		{
			IWfiWordform pigs = CheckAnalysisSize("pigsTEST", 0, true);

			ParseResult result = null;
			IWfiAnalysis anal1 = null, anal2 = null, anal3 = null;
			UndoableUnitOfWorkHelper.Do("Undo stuff", "Redo stuff", m_actionHandler, () =>
			{
				// Bear entry
				ILexEntry pigN = m_entryFactory.Create();
				IMoStemAllomorph pigNForm = m_stemAlloFactory.Create();
				pigN.AlternateFormsOS.Add(pigNForm);
				pigNForm.Form.VernacularDefaultWritingSystem = Cache.TsStrFactory.MakeString("pigNTEST", m_vernacularWS.Handle);
				IMoStemMsa pigNMsa = m_stemMsaFactory.Create();
				pigN.MorphoSyntaxAnalysesOC.Add(pigNMsa);
				ILexSense pigNSense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				pigN.SensesOS.Add(pigNSense);

				var analFactory = Cache.ServiceLocator.GetInstance<IWfiAnalysisFactory>();
				var mbFactory = Cache.ServiceLocator.GetInstance<IWfiMorphBundleFactory>();
				// First of two duplicate analyses
				IWfiAnalysis anal = analFactory.Create();
				pigs.AnalysesOC.Add(anal);
				anal1 = anal;
				IWfiMorphBundle mb = mbFactory.Create();
				anal.MorphBundlesOS.Add(mb);
				mb.MorphRA = pigNForm;
				mb.MsaRA = pigNMsa;
				CheckEvaluationSize(anal1, 0, true, "anal1");

				// Non-duplicate, to make sure it does not get approved.
				anal = analFactory.Create();
				pigs.AnalysesOC.Add(anal);
				anal2 = anal;
				mb = mbFactory.Create();
				anal.MorphBundlesOS.Add(mb);
				mb.SenseRA = pigNSense;
				CheckEvaluationSize(anal2, 0, true, "anal2");

				// Second of two duplicate analyses
				anal = analFactory.Create();
				pigs.AnalysesOC.Add(anal);
				anal3 = anal;
				mb = mbFactory.Create();
				anal.MorphBundlesOS.Add(mb);
				mb.MorphRA = pigNForm;
				mb.MsaRA = pigNMsa;
				CheckEvaluationSize(anal3, 0, true, "anal3");
				CheckAnalysisSize("pigsTEST", 3, false);

				result = new ParseResult(new[]
					{
						new ParseAnalysis(new[]
							{
								new ParseMorph(pigNForm, pigNMsa)
							})
					});
			});

			m_filer.ProcessParse(pigs, ParserPriority.Low, result);
			ExecuteIdleQueue();
			CheckEvaluationSize(anal1, 1, false, "anal1Hvo");
			Assert.IsFalse(anal2.IsValidObject, "analysis 2 should end up with no evaluations and so be deleted");
			CheckEvaluationSize(anal3, 1, false, "anal3Hvo");
		}

		[Test]
		public void HumanApprovedParserPreviouslyApprovedButNowRejectedAnalysisSurvives()
		{
			IWfiWordform theThreeLittlePigs = CheckAnalysisSize("theThreeLittlePigsTEST", 0, true);

			ParseResult result = null;
			IWfiAnalysis anal = null;
			UndoableUnitOfWorkHelper.Do("Undo stuff", "Redo stuff", m_actionHandler, () =>
			{
				// Pig entry
				ILexEntry pigN = m_entryFactory.Create();
				IMoStemAllomorph pigNForm = m_stemAlloFactory.Create();
				pigN.AlternateFormsOS.Add(pigNForm);
				pigNForm.Form.VernacularDefaultWritingSystem = Cache.TsStrFactory.MakeString("pigNTEST", m_vernacularWS.Handle);
				IMoStemMsa pigNMsa = m_stemMsaFactory.Create();
				pigN.MorphoSyntaxAnalysesOC.Add(pigNMsa);
				ILexSense pigNSense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				pigN.SensesOS.Add(pigNSense);

				// Human approved anal. Start with parser approved, but then it failed.
				var analFactory = Cache.ServiceLocator.GetInstance<IWfiAnalysisFactory>();
				var mbFactory = Cache.ServiceLocator.GetInstance<IWfiMorphBundleFactory>();
				// Only analysis: human approved, previously parser approved but no longer produced.
				anal = analFactory.Create();
				theThreeLittlePigs.AnalysesOC.Add(anal);
				IWfiMorphBundle mb = mbFactory.Create();
				anal.MorphBundlesOS.Add(mb);
				mb.MorphRA = pigNForm;
				mb.MsaRA = pigNMsa;
				HumanAgent.SetEvaluation(anal, Opinions.approves);
				ParserAgent.SetEvaluation(anal, Opinions.approves);
				CheckEvaluationSize(anal, 2, true, "anal");
				CheckAnalysisSize("theThreeLittlePigsTEST", 1, true);

				result = new ParseResult(Enumerable.Empty<ParseAnalysis>());
			});

			m_filer.ProcessParse(theThreeLittlePigs, ParserPriority.Low, result);
			ExecuteIdleQueue();
			CheckEvaluationSize(anal, 2, false, "analHvo");
			Assert.IsTrue(anal.IsValidObject, "analysis should end up with one evaluation and not be deleted");
		}

		[Test]
		public void HumanHasNoopinionParserHadApprovedButNoLongerApprovesRemovesAnalysis()
		{
			IWfiWordform threeLittlePigs = CheckAnalysisSize("threeLittlePigsTEST", 0, true);

			ParseResult result = null;
			IWfiAnalysis anal = null;
			UndoableUnitOfWorkHelper.Do("Undo stuff", "Redo stuff", m_actionHandler, () =>
			{
				// Pig entry
				ILexEntry pigN = m_entryFactory.Create();
				IMoStemAllomorph pigNForm = m_stemAlloFactory.Create();
				pigN.AlternateFormsOS.Add(pigNForm);
				pigNForm.Form.VernacularDefaultWritingSystem = Cache.TsStrFactory.MakeString("pigNTEST", m_vernacularWS.Handle);
				IMoStemMsa pigNMsa = m_stemMsaFactory.Create();
				pigN.MorphoSyntaxAnalysesOC.Add(pigNMsa);
				ILexSense pigNSense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				pigN.SensesOS.Add(pigNSense);

				// Human no-opinion anal. Parser had approved, but then it failed to produce it.
				var analFactory = Cache.ServiceLocator.GetInstance<IWfiAnalysisFactory>();
				var mbFactory = Cache.ServiceLocator.GetInstance<IWfiMorphBundleFactory>();
				// Human no-opinion anal. Parser had approved, but then it failed to produce it.
				anal = analFactory.Create();
				threeLittlePigs.AnalysesOC.Add(anal);
				IWfiMorphBundle mb = mbFactory.Create();
				anal.MorphBundlesOS.Add(mb);
				mb.MorphRA = pigNForm;
				mb.MsaRA = pigNMsa;
				HumanAgent.SetEvaluation(anal, Opinions.noopinion);
				ParserAgent.SetEvaluation(anal, Opinions.approves);
				CheckEvaluationSize(anal, 1, true, "anal");
				CheckAnalysisSize("threeLittlePigsTEST", 1, true);

				result = new ParseResult(Enumerable.Empty<ParseAnalysis>());
			});

			m_filer.ProcessParse(threeLittlePigs, ParserPriority.Low, result);
			ExecuteIdleQueue();
			Assert.IsFalse(anal.IsValidObject, "analysis should end up with no evaluations and be deleted.");
		}

		[Test]
		public void LexEntryInflTypeTwoAnalyses()
		{
			IWfiWordform creb = CheckAnalysisSize("crebTEST", 0, true);
			ILexDb ldb = Cache.LanguageProject.LexDbOA;

			ParseResult result = null;
			UndoableUnitOfWorkHelper.Do("Undo stuff", "Redo stuff", m_actionHandler, () =>
			{
				// Verb creb which is a past tense, plural irregularly inflected form of 'believe' and also 'seek'
				// with automatically generated null Tense slot and an automatically generated null Number slot filler
				// (This is not supposed to be English, in case you're wondering....)

				ILexEntryInflType pastTenseLexEntryInflType = m_lexEntryInflTypeFactory.Create();
				ILexEntryInflType pluralTenseLexEntryInflType = m_lexEntryInflTypeFactory.Create();
				Cache.LangProject.LexDbOA.VariantEntryTypesOA.PossibilitiesOS.Add(pastTenseLexEntryInflType);
				Cache.LangProject.LexDbOA.VariantEntryTypesOA.PossibilitiesOS.Add(pluralTenseLexEntryInflType);

				ILexEntry believeV = m_entryFactory.Create();
				IMoStemAllomorph believeVForm = m_stemAlloFactory.Create();
				believeV.AlternateFormsOS.Add(believeVForm);
				believeVForm.Form.VernacularDefaultWritingSystem = Cache.TsStrFactory.MakeString("believeVTEST", m_vernacularWS.Handle);
				IMoStemMsa believeVMsa = m_stemMsaFactory.Create();
				believeV.MorphoSyntaxAnalysesOC.Add(believeVMsa);
				ILexSense believeVSense = m_senseFactory.Create();
				believeV.SensesOS.Add(believeVSense);
				believeVSense.MorphoSyntaxAnalysisRA = believeVMsa;

				ILexEntry seekV = m_entryFactory.Create();
				IMoStemAllomorph seekVForm = m_stemAlloFactory.Create();
				believeV.AlternateFormsOS.Add(seekVForm);
				seekVForm.Form.VernacularDefaultWritingSystem = Cache.TsStrFactory.MakeString("seekVTEST", m_vernacularWS.Handle);
				IMoStemMsa seekVMsa = m_stemMsaFactory.Create();
				seekV.MorphoSyntaxAnalysesOC.Add(seekVMsa);
				ILexSense seekVSense = m_senseFactory.Create();
				seekV.SensesOS.Add(seekVSense);
				seekVSense.MorphoSyntaxAnalysisRA = seekVMsa;

				ILexEntry crebV = m_entryFactory.Create();
				IMoStemAllomorph crebVForm = m_stemAlloFactory.Create();
				crebV.AlternateFormsOS.Add(crebVForm);
				crebVForm.Form.VernacularDefaultWritingSystem = Cache.TsStrFactory.MakeString("crebVTEST", m_vernacularWS.Handle);
				ILexEntryRef lexEntryref = m_lexEntryRefFactory.Create();
				crebV.EntryRefsOS.Add(lexEntryref);
				lexEntryref.ComponentLexemesRS.Add(believeV);
				lexEntryref.VariantEntryTypesRS.Add(pastTenseLexEntryInflType);
				lexEntryref.VariantEntryTypesRS.Add(pluralTenseLexEntryInflType);
				lexEntryref = m_lexEntryRefFactory.Create();
				crebV.EntryRefsOS.Add(lexEntryref);
				lexEntryref.ComponentLexemesRS.Add(seekV);
				lexEntryref.VariantEntryTypesRS.Add(pastTenseLexEntryInflType);
				lexEntryref.VariantEntryTypesRS.Add(pluralTenseLexEntryInflType);

				ILexEntry nullPast = m_entryFactory.Create();
				IMoAffixAllomorph nullPastForm = m_afxAlloFactory.Create();
				nullPast.AlternateFormsOS.Add(nullPastForm);
				nullPastForm.Form.VernacularDefaultWritingSystem = Cache.TsStrFactory.MakeString("nullPASTTEST", m_vernacularWS.Handle);
				IMoInflAffMsa nullPastMsa = m_inflAffMsaFactory.Create();
				nullPast.MorphoSyntaxAnalysesOC.Add(nullPastMsa);

				ILexEntry nullPlural = m_entryFactory.Create();
				IMoAffixAllomorph nullPluralForm = m_afxAlloFactory.Create();
				nullPlural.AlternateFormsOS.Add(nullPluralForm);
				nullPluralForm.Form.VernacularDefaultWritingSystem = Cache.TsStrFactory.MakeString("nullPLURALTEST", m_vernacularWS.Handle);
				IMoInflAffMsa nullPluralMsa = m_inflAffMsaFactory.Create();
				nullPlural.MorphoSyntaxAnalysesOC.Add(nullPluralMsa);

				result = new ParseResult(new[]
					{
						new ParseAnalysis(new[]
							{
								new ParseMorph(crebVForm, MorphServices.GetMainOrFirstSenseOfVariant(crebV.EntryRefsOS[1]).MorphoSyntaxAnalysisRA,
									(ILexEntryInflType) crebV.EntryRefsOS[1].VariantEntryTypesRS[0])
							}),
						new ParseAnalysis(new[]
							{
								new ParseMorph(crebVForm, MorphServices.GetMainOrFirstSenseOfVariant(crebV.EntryRefsOS[0]).MorphoSyntaxAnalysisRA,
									(ILexEntryInflType) crebV.EntryRefsOS[0].VariantEntryTypesRS[0])
							})
					});
			});

			m_filer.ProcessParse(creb, ParserPriority.Low, result);
			ExecuteIdleQueue();
			CheckAnalysisSize("crebTEST", 2, false);
			foreach (var analysis in creb.AnalysesOC)
			{
				Assert.AreEqual(1, analysis.MorphBundlesOS.Count, "Expected only 1 morph in the analysis");
				var morphBundle = analysis.MorphBundlesOS.ElementAt(0);
				Assert.IsNotNull(morphBundle.Form, "First bundle: form is not null");
				Assert.IsNotNull(morphBundle.MsaRA, "First bundle: msa is not null");
				Assert.IsNotNull(morphBundle.InflTypeRA, "First bundle: infl type is not null");
			}
		}

		[Test]
		public void LexEntryInflTypeAnalysisWithNullForSlotFiller()
		{
			IWfiWordform brubs = CheckAnalysisSize("brubsTEST", 0, true);
			ILexDb ldb = Cache.LanguageProject.LexDbOA;

			ParseResult result = null;
			UndoableUnitOfWorkHelper.Do("Undo stuff", "Redo stuff", m_actionHandler, () =>
			{
				// Verb brub which is a present tense irregularly inflected form of 'believe'
				// with automatically generated null Tense slot and an -s Plural Number slot filler
				// (This is not supposed to be English, in case you're wondering....)

				ILexEntryInflType presentTenseLexEntryInflType = m_lexEntryInflTypeFactory.Create();
				Cache.LangProject.LexDbOA.VariantEntryTypesOA.PossibilitiesOS.Add(presentTenseLexEntryInflType);

				ILexEntry believeV = m_entryFactory.Create();
				IMoStemAllomorph believeVForm = m_stemAlloFactory.Create();
				believeV.AlternateFormsOS.Add(believeVForm);
				believeVForm.Form.VernacularDefaultWritingSystem = Cache.TsStrFactory.MakeString("believeVTEST", m_vernacularWS.Handle);
				IMoStemMsa believeVMsa = m_stemMsaFactory.Create();
				believeV.MorphoSyntaxAnalysesOC.Add(believeVMsa);
				ILexSense believeVSense = m_senseFactory.Create();
				believeV.SensesOS.Add(believeVSense);
				believeVSense.MorphoSyntaxAnalysisRA = believeVMsa;

				ILexEntry brubV = m_entryFactory.Create();
				IMoStemAllomorph brubVForm = m_stemAlloFactory.Create();
				brubV.AlternateFormsOS.Add(brubVForm);
				brubVForm.Form.VernacularDefaultWritingSystem = Cache.TsStrFactory.MakeString("brubVTEST", m_vernacularWS.Handle);
				ILexEntryRef lexEntryref = m_lexEntryRefFactory.Create();
				brubV.EntryRefsOS.Add(lexEntryref);
				lexEntryref.ComponentLexemesRS.Add(believeV);
				lexEntryref.VariantEntryTypesRS.Add(presentTenseLexEntryInflType);

				ILexEntry nullPresent = m_entryFactory.Create();
				IMoAffixAllomorph nullPresentForm = m_afxAlloFactory.Create();
				nullPresent.AlternateFormsOS.Add(nullPresentForm);
				nullPresentForm.Form.VernacularDefaultWritingSystem = Cache.TsStrFactory.MakeString("nullPRESENTTEST", m_vernacularWS.Handle);
				IMoInflAffMsa nullPresentMsa = m_inflAffMsaFactory.Create();
				nullPresent.MorphoSyntaxAnalysesOC.Add(nullPresentMsa);

				ILexEntry sPlural = m_entryFactory.Create();
				IMoAffixAllomorph sPluralForm = m_afxAlloFactory.Create();
				sPlural.AlternateFormsOS.Add(sPluralForm);
				sPluralForm.Form.VernacularDefaultWritingSystem = Cache.TsStrFactory.MakeString("sPLURALTEST", m_vernacularWS.Handle);
				IMoInflAffMsa sPluralMsa = m_inflAffMsaFactory.Create();
				sPlural.MorphoSyntaxAnalysesOC.Add(sPluralMsa);

				result = new ParseResult(new[]
					{
						new ParseAnalysis(new[]
							{
								new ParseMorph(brubVForm, MorphServices.GetMainOrFirstSenseOfVariant(brubV.EntryRefsOS[0]).MorphoSyntaxAnalysisRA,
									(ILexEntryInflType) brubV.EntryRefsOS[0].VariantEntryTypesRS[0]),
								new ParseMorph(sPluralForm, sPluralMsa)
							})
					});
			});

			m_filer.ProcessParse(brubs, ParserPriority.Low, result);
			ExecuteIdleQueue();
			CheckAnalysisSize("brubsTEST", 1, false);
			var analysis = brubs.AnalysesOC.ElementAt(0);
			Assert.AreEqual(2, analysis.MorphBundlesOS.Count, "Expected only 2 morphs in the analysis");
			var morphBundle = analysis.MorphBundlesOS.ElementAt(0);
			Assert.IsNotNull(morphBundle.Form, "First bundle: form is not null");
			Assert.IsNotNull(morphBundle.MsaRA, "First bundle: msa is not null");
			Assert.IsNotNull(morphBundle.InflTypeRA, "First bundle: infl type is not null");
		}

		#endregion // Tests
	}
}
