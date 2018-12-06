// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.WordWorks.Parser
{
	/// <summary />
	[TestFixture]
	public class ParseFilerProcessingTests : MemoryOnlyBackendProviderTestBase
	{
		#region Data Members

		private ParseFiler m_filer;
		private IdleQueue m_idleQueue;
		private CoreWritingSystemDefinition m_vernacularWS;
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

		protected ICmAgent ParserAgent => Cache.LanguageProject.DefaultParserAgent;

		protected ICmAgent HumanAgent => Cache.LanguageProject.DefaultUserAgent;

		protected IWfiWordform CheckAnnotationSize(string form, int expectedSize, bool isStarting)
		{
			var servLoc = Cache.ServiceLocator;
			var wf = FindOrCreateWordform(form);
			var actualSize = servLoc.GetInstance<ICmBaseAnnotationRepository>().AllInstances().Count(ann => ann.BeginObjectRA == wf);
			// wf.RefsFrom_CmBaseAnnotation_BeginObject.Count;
			var msg = string.Format("Wrong number of {0} annotations for: {1}", isStarting ? "starting" : "ending", form);
			Assert.AreEqual(expectedSize, actualSize, msg);
			return wf;
		}

		private IWfiWordform FindOrCreateWordform(string form)
		{
			var servLoc = Cache.ServiceLocator;
			var wf = servLoc.GetInstance<IWfiWordformRepository>().GetMatchingWordform(m_vernacularWS.Handle, form);
			if (wf == null)
			{
				UndoableUnitOfWorkHelper.Do("Undo create", "Redo create", m_actionHandler,
					() => wf = servLoc.GetInstance<IWfiWordformFactory>().Create(TsStringUtils.MakeString(form, m_vernacularWS.Handle)));
			}
			return wf;
		}

		protected IWfiWordform CheckAnalysisSize(string form, int expectedSize, bool isStarting)
		{
			var wf = FindOrCreateWordform(form);
			Assert.AreEqual(expectedSize, wf.AnalysesOC.Count, $"Wrong number of {(isStarting ? "starting" : "ending")} analyses for: {form}");
			return wf;
		}

		protected void CheckEvaluationSize(IWfiAnalysis analysis, int expectedSize, bool isStarting, string additionalMessage)
		{
			Assert.AreEqual(expectedSize, analysis.EvaluationsRC.Count, $"Wrong number of {(isStarting ? "starting" : "ending")} evaluations for analysis: {analysis.Hvo} ({additionalMessage})");
		}

		protected void ExecuteIdleQueue()
		{
			foreach (var task in m_idleQueue)
			{
				task.Delegate(task.Parameter);
			}
			m_idleQueue.Clear();
		}

		#endregion // Non-tests

		#region Setup and TearDown

		public override void FixtureSetup()
		{
			base.FixtureSetup();
			m_vernacularWS = Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem;
			m_idleQueue = new IdleQueue { IsPaused = true };
			m_filer = new ParseFiler(Cache, task => { }, m_idleQueue, Cache.LanguageProject.DefaultParserAgent);
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
			try
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
			}
			catch (Exception err)
			{
				throw new Exception($"Error in running {GetType().Name} FixtureTeardown method.", err);
			}
			finally
			{
				base.FixtureTeardown();
			}
		}

		public override void TestTearDown()
		{
			try
			{
				UndoAll();
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

		/// <summary>
		/// End the undoable UOW and Undo everything.
		/// </summary>
		protected void UndoAll()
		{
			// Undo the UOW (or more than one of them, if the test made new ones).
			while (m_actionHandler.CanUndo())
			{
				m_actionHandler.Undo();
			}
			// Need to 'Commit' to clear out redo stack,
			// since nothing is really saved.
			m_actionHandler.Commit();
		}

		#endregion Setup and TearDown

		#region Tests

		[Test]
		public void TooManyAnalyses()
		{
			var bearsTest = CheckAnnotationSize("bearsTEST", 0, true);
			var result = new ParseResult("Maximum permitted analyses (448) reached.");
			m_filer.ProcessParse(bearsTest, ParserPriority.Low, result);
			ExecuteIdleQueue();
			CheckAnnotationSize("bearsTEST", 1, false);
		}

		[Test]
		public void BufferOverrun()
		{
			var dogsTest = CheckAnnotationSize("dogsTEST", 0, true);
			var result = new ParseResult("Maximum internal buffer size (117) reached.");
			m_filer.ProcessParse(dogsTest, ParserPriority.Low, result);
			ExecuteIdleQueue();
			CheckAnnotationSize("dogsTEST", 1, false);
		}

		[Test]
		public void TwoAnalyses()
		{
			var catsTest = CheckAnalysisSize("catsTEST", 0, true);
			var ldb = Cache.LanguageProject.LexDbOA;
			ParseResult result = null;
			UndoableUnitOfWorkHelper.Do("Undo stuff", "Redo stuff", m_actionHandler, () =>
			{
				// Noun
				var catN = m_entryFactory.Create();
				var catNForm = m_stemAlloFactory.Create();
				catN.AlternateFormsOS.Add(catNForm);
				catNForm.Form.VernacularDefaultWritingSystem = TsStringUtils.MakeString("catNTEST", m_vernacularWS.Handle);
				var catNMsa = m_stemMsaFactory.Create();
				catN.MorphoSyntaxAnalysesOC.Add(catNMsa);

				var sPl = m_entryFactory.Create();
				var sPlForm = m_afxAlloFactory.Create();
				sPl.AlternateFormsOS.Add(sPlForm);
				sPlForm.Form.VernacularDefaultWritingSystem = TsStringUtils.MakeString("sPLTEST", m_vernacularWS.Handle);
				var sPlMsa = m_inflAffMsaFactory.Create();
				sPl.MorphoSyntaxAnalysesOC.Add(sPlMsa);

				// Verb
				var catV = m_entryFactory.Create();
				var catVForm = m_stemAlloFactory.Create();
				catV.AlternateFormsOS.Add(catVForm);
				catVForm.Form.VernacularDefaultWritingSystem = TsStringUtils.MakeString("catVTEST", m_vernacularWS.Handle);
				var catVMsa = m_stemMsaFactory.Create();
				catV.MorphoSyntaxAnalysesOC.Add(catVMsa);

				var sAgr = m_entryFactory.Create();
				var sAgrForm = m_afxAlloFactory.Create();
				sAgr.AlternateFormsOS.Add(sAgrForm);
				sAgrForm.Form.VernacularDefaultWritingSystem = TsStringUtils.MakeString("sAGRTEST", m_vernacularWS.Handle);
				var sAgrMsa = m_inflAffMsaFactory.Create();
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

		/// <summary>
		/// Ensure analyses with 'duplicate' analyses are both approved.
		/// "Duplicate" means the MSA and MoForm IDs are the same in two different analyses.
		/// </summary>
		[Test]
		public void DuplicateAnalysesApproval()
		{
			var pigs = CheckAnalysisSize("pigsTEST", 0, true);
			ParseResult result = null;
			IWfiAnalysis anal1 = null, anal2 = null, anal3 = null;
			UndoableUnitOfWorkHelper.Do("Undo stuff", "Redo stuff", m_actionHandler, () =>
			{
				// Bear entry
				var pigN = m_entryFactory.Create();
				var pigNForm = m_stemAlloFactory.Create();
				pigN.AlternateFormsOS.Add(pigNForm);
				pigNForm.Form.VernacularDefaultWritingSystem = TsStringUtils.MakeString("pigNTEST", m_vernacularWS.Handle);
				var pigNMsa = m_stemMsaFactory.Create();
				pigN.MorphoSyntaxAnalysesOC.Add(pigNMsa);
				var pigNSense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				pigN.SensesOS.Add(pigNSense);

				var analFactory = Cache.ServiceLocator.GetInstance<IWfiAnalysisFactory>();
				var mbFactory = Cache.ServiceLocator.GetInstance<IWfiMorphBundleFactory>();
				// First of two duplicate analyses
				var anal = analFactory.Create();
				pigs.AnalysesOC.Add(anal);
				anal1 = anal;
				var mb = mbFactory.Create();
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
			var theThreeLittlePigs = CheckAnalysisSize("theThreeLittlePigsTEST", 0, true);
			ParseResult result = null;
			IWfiAnalysis anal = null;
			UndoableUnitOfWorkHelper.Do("Undo stuff", "Redo stuff", m_actionHandler, () =>
			{
				// Pig entry
				var pigN = m_entryFactory.Create();
				var pigNForm = m_stemAlloFactory.Create();
				pigN.AlternateFormsOS.Add(pigNForm);
				pigNForm.Form.VernacularDefaultWritingSystem = TsStringUtils.MakeString("pigNTEST", m_vernacularWS.Handle);
				var pigNMsa = m_stemMsaFactory.Create();
				pigN.MorphoSyntaxAnalysesOC.Add(pigNMsa);
				var pigNSense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				pigN.SensesOS.Add(pigNSense);

				// Human approved anal. Start with parser approved, but then it failed.
				var analFactory = Cache.ServiceLocator.GetInstance<IWfiAnalysisFactory>();
				var mbFactory = Cache.ServiceLocator.GetInstance<IWfiMorphBundleFactory>();
				// Only analysis: human approved, previously parser approved but no longer produced.
				anal = analFactory.Create();
				theThreeLittlePigs.AnalysesOC.Add(anal);
				var mb = mbFactory.Create();
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
			var threeLittlePigs = CheckAnalysisSize("threeLittlePigsTEST", 0, true);
			ParseResult result = null;
			IWfiAnalysis anal = null;
			UndoableUnitOfWorkHelper.Do("Undo stuff", "Redo stuff", m_actionHandler, () =>
			{
				// Pig entry
				var pigN = m_entryFactory.Create();
				var pigNForm = m_stemAlloFactory.Create();
				pigN.AlternateFormsOS.Add(pigNForm);
				pigNForm.Form.VernacularDefaultWritingSystem = TsStringUtils.MakeString("pigNTEST", m_vernacularWS.Handle);
				var pigNMsa = m_stemMsaFactory.Create();
				pigN.MorphoSyntaxAnalysesOC.Add(pigNMsa);
				var pigNSense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				pigN.SensesOS.Add(pigNSense);

				// Human no-opinion anal. Parser had approved, but then it failed to produce it.
				var analFactory = Cache.ServiceLocator.GetInstance<IWfiAnalysisFactory>();
				var mbFactory = Cache.ServiceLocator.GetInstance<IWfiMorphBundleFactory>();
				// Human no-opinion anal. Parser had approved, but then it failed to produce it.
				anal = analFactory.Create();
				threeLittlePigs.AnalysesOC.Add(anal);
				var mb = mbFactory.Create();
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
			var creb = CheckAnalysisSize("crebTEST", 0, true);
			var ldb = Cache.LanguageProject.LexDbOA;
			ParseResult result = null;
			UndoableUnitOfWorkHelper.Do("Undo stuff", "Redo stuff", m_actionHandler, () =>
			{
				// Verb creb which is a past tense, plural irregularly inflected form of 'believe' and also 'seek'
				// with automatically generated null Tense slot and an automatically generated null Number slot filler
				// (This is not supposed to be English, in case you're wondering....)
				var pastTenseLexEntryInflType = m_lexEntryInflTypeFactory.Create();
				var pluralTenseLexEntryInflType = m_lexEntryInflTypeFactory.Create();
				Cache.LangProject.LexDbOA.VariantEntryTypesOA.PossibilitiesOS.Add(pastTenseLexEntryInflType);
				Cache.LangProject.LexDbOA.VariantEntryTypesOA.PossibilitiesOS.Add(pluralTenseLexEntryInflType);

				var believeV = m_entryFactory.Create();
				var believeVForm = m_stemAlloFactory.Create();
				believeV.AlternateFormsOS.Add(believeVForm);
				believeVForm.Form.VernacularDefaultWritingSystem = TsStringUtils.MakeString("believeVTEST", m_vernacularWS.Handle);
				var believeVMsa = m_stemMsaFactory.Create();
				believeV.MorphoSyntaxAnalysesOC.Add(believeVMsa);
				var believeVSense = m_senseFactory.Create();
				believeV.SensesOS.Add(believeVSense);
				believeVSense.MorphoSyntaxAnalysisRA = believeVMsa;

				var seekV = m_entryFactory.Create();
				var seekVForm = m_stemAlloFactory.Create();
				believeV.AlternateFormsOS.Add(seekVForm);
				seekVForm.Form.VernacularDefaultWritingSystem = TsStringUtils.MakeString("seekVTEST", m_vernacularWS.Handle);
				var seekVMsa = m_stemMsaFactory.Create();
				seekV.MorphoSyntaxAnalysesOC.Add(seekVMsa);
				var seekVSense = m_senseFactory.Create();
				seekV.SensesOS.Add(seekVSense);
				seekVSense.MorphoSyntaxAnalysisRA = seekVMsa;

				var crebV = m_entryFactory.Create();
				var crebVForm = m_stemAlloFactory.Create();
				crebV.AlternateFormsOS.Add(crebVForm);
				crebVForm.Form.VernacularDefaultWritingSystem = TsStringUtils.MakeString("crebVTEST", m_vernacularWS.Handle);
				var lexEntryref = m_lexEntryRefFactory.Create();
				crebV.EntryRefsOS.Add(lexEntryref);
				lexEntryref.ComponentLexemesRS.Add(believeV);
				lexEntryref.VariantEntryTypesRS.Add(pastTenseLexEntryInflType);
				lexEntryref.VariantEntryTypesRS.Add(pluralTenseLexEntryInflType);
				lexEntryref = m_lexEntryRefFactory.Create();
				crebV.EntryRefsOS.Add(lexEntryref);
				lexEntryref.ComponentLexemesRS.Add(seekV);
				lexEntryref.VariantEntryTypesRS.Add(pastTenseLexEntryInflType);
				lexEntryref.VariantEntryTypesRS.Add(pluralTenseLexEntryInflType);

				var nullPast = m_entryFactory.Create();
				var nullPastForm = m_afxAlloFactory.Create();
				nullPast.AlternateFormsOS.Add(nullPastForm);
				nullPastForm.Form.VernacularDefaultWritingSystem = TsStringUtils.MakeString("nullPASTTEST", m_vernacularWS.Handle);
				var nullPastMsa = m_inflAffMsaFactory.Create();
				nullPast.MorphoSyntaxAnalysesOC.Add(nullPastMsa);

				var nullPlural = m_entryFactory.Create();
				var nullPluralForm = m_afxAlloFactory.Create();
				nullPlural.AlternateFormsOS.Add(nullPluralForm);
				nullPluralForm.Form.VernacularDefaultWritingSystem = TsStringUtils.MakeString("nullPLURALTEST", m_vernacularWS.Handle);
				var nullPluralMsa = m_inflAffMsaFactory.Create();
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
			var brubs = CheckAnalysisSize("brubsTEST", 0, true);
			var ldb = Cache.LanguageProject.LexDbOA;
			ParseResult result = null;
			UndoableUnitOfWorkHelper.Do("Undo stuff", "Redo stuff", m_actionHandler, () =>
			{
				// Verb brub which is a present tense irregularly inflected form of 'believe'
				// with automatically generated null Tense slot and an -s Plural Number slot filler
				// (This is not supposed to be English, in case you're wondering....)
				var presentTenseLexEntryInflType = m_lexEntryInflTypeFactory.Create();
				Cache.LangProject.LexDbOA.VariantEntryTypesOA.PossibilitiesOS.Add(presentTenseLexEntryInflType);

				var believeV = m_entryFactory.Create();
				var believeVForm = m_stemAlloFactory.Create();
				believeV.AlternateFormsOS.Add(believeVForm);
				believeVForm.Form.VernacularDefaultWritingSystem = TsStringUtils.MakeString("believeVTEST", m_vernacularWS.Handle);
				var believeVMsa = m_stemMsaFactory.Create();
				believeV.MorphoSyntaxAnalysesOC.Add(believeVMsa);
				var believeVSense = m_senseFactory.Create();
				believeV.SensesOS.Add(believeVSense);
				believeVSense.MorphoSyntaxAnalysisRA = believeVMsa;

				var brubV = m_entryFactory.Create();
				var brubVForm = m_stemAlloFactory.Create();
				brubV.AlternateFormsOS.Add(brubVForm);
				brubVForm.Form.VernacularDefaultWritingSystem = TsStringUtils.MakeString("brubVTEST", m_vernacularWS.Handle);
				var lexEntryref = m_lexEntryRefFactory.Create();
				brubV.EntryRefsOS.Add(lexEntryref);
				lexEntryref.ComponentLexemesRS.Add(believeV);
				lexEntryref.VariantEntryTypesRS.Add(presentTenseLexEntryInflType);

				var nullPresent = m_entryFactory.Create();
				var nullPresentForm = m_afxAlloFactory.Create();
				nullPresent.AlternateFormsOS.Add(nullPresentForm);
				nullPresentForm.Form.VernacularDefaultWritingSystem = TsStringUtils.MakeString("nullPRESENTTEST", m_vernacularWS.Handle);
				var nullPresentMsa = m_inflAffMsaFactory.Create();
				nullPresent.MorphoSyntaxAnalysesOC.Add(nullPresentMsa);

				var sPlural = m_entryFactory.Create();
				var sPluralForm = m_afxAlloFactory.Create();
				sPlural.AlternateFormsOS.Add(sPluralForm);
				sPluralForm.Form.VernacularDefaultWritingSystem = TsStringUtils.MakeString("sPLURALTEST", m_vernacularWS.Handle);
				var sPluralMsa = m_inflAffMsaFactory.Create();
				sPlural.MorphoSyntaxAnalysesOC.Add(sPluralMsa);

				result = new ParseResult(new[]
				{
					new ParseAnalysis(new[]
					{
						new ParseMorph(brubVForm, MorphServices.GetMainOrFirstSenseOfVariant(brubV.EntryRefsOS[0]).MorphoSyntaxAnalysisRA, (ILexEntryInflType) brubV.EntryRefsOS[0].VariantEntryTypesRS[0]),
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