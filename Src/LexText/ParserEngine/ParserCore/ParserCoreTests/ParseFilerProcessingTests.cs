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
// File: ParseFilerProcessingTests.cs
// Responsibility: Randy Regnier
// Last reviewed:
//
// <remarks>
// Implements the ParseFilerProcessingTests unit tests.
// </remarks>
// buildtest ParseFiler-nodep
// --------------------------------------------------------------------------------------------
using System;
using System.Linq;
using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
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
		private IWritingSystem m_vernacularWS;
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

		protected ICmAgent AnalyzingAgent
		{
			get
			{
				return Cache.LanguageProject.DefaultParserAgent;
			}
		}

		protected IWfiWordform CheckAnnotationSize(string form, int expectedSize, bool isStarting)
		{
			var servLoc = Cache.ServiceLocator;
			var wf = FindOrCreateWordform(form);
			var actualSize =
				(from ann in servLoc.GetInstance<ICmBaseAnnotationRepository>().AllInstances()
				 where ann.BeginObjectRA == wf
				 select ann).Count();
				// wf.RefsFrom_CmBaseAnnotation_BeginObject.Count;
			var msg = String.Format("Wrong number of {0} annotations for: {1}", isStarting ? "starting" : "ending", form);
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
					() => wf = servLoc.GetInstance<IWfiWordformFactory>().Create(Cache.TsStrFactory.MakeString(form, m_vernacularWS.Handle)));
			}
			return wf;
		}

		protected IWfiWordform CheckAnalysisSize(string form, int expectedSize, bool isStarting)
		{
			var wf = FindOrCreateWordform(form);
			var actualSize = wf.AnalysesOC.Count;
			var msg = String.Format("Wrong number of {0} analyses for: {1}", isStarting ? "starting" : "ending", form);
			Assert.AreEqual(expectedSize, actualSize, msg);
			return wf;
		}

		protected void CheckEvaluationSize(IWfiAnalysis analysis, int expectedSize, bool isStarting, string additionalMessage)
		{
			var actualSize = analysis.EvaluationsRC.Count;
			var msg = String.Format("Wrong number of {0} evaluations for analysis: {1} ({2})", isStarting ? "starting" : "ending", analysis.Hvo, additionalMessage);
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
			m_filer.Dispose();
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
			var bearsTEST = CheckAnnotationSize("bearsTEST", 0, true);
			var xmlFragment = "<Wordform DbRef='" + bearsTEST.Hvo + "' Form='bearsTEST'>" + Environment.NewLine
				+ "<Exception code='ReachedMaxAnalyses' totalAnalyses='448'/>" + Environment.NewLine
				+ "</Wordform>";
			m_filer.ProcessParse(ParserPriority.Low, xmlFragment, 0);
			ExecuteIdleQueue();
			CheckAnnotationSize("bearsTEST", 1, false);
		}

		[Test]
		public void BufferOverrun()
		{
			var dogsTEST = CheckAnnotationSize("dogsTEST", 0, true);
			var xmlFragment = "<Wordform DbRef='" + dogsTEST.Hvo + "' Form='dogsTEST'>" + Environment.NewLine
				+ "<Exception code='ReachedMaxBufferSize' totalAnalyses='117'/>" + Environment.NewLine
				+ "</Wordform>";
			m_filer.ProcessParse(ParserPriority.Low, xmlFragment, 0);
			ExecuteIdleQueue();
			CheckAnnotationSize("dogsTEST", 1, false);
		}

		[Test]
		public void TwoAnalyses()
		{
			var catsTEST = CheckAnalysisSize("catsTEST", 0, true);
			var ldb = Cache.LanguageProject.LexDbOA;

			string xmlFragment = null;
			UndoableUnitOfWorkHelper.Do("Undo stuff", "Redo stuff", m_actionHandler, () =>
			{
				// Noun
				var catN = m_entryFactory.Create();
				var catNForm = m_stemAlloFactory.Create();
				catN.AlternateFormsOS.Add(catNForm);
				catNForm.Form.VernacularDefaultWritingSystem = Cache.TsStrFactory.MakeString("catNTEST", m_vernacularWS.Handle);
				var catNMSA = m_stemMsaFactory.Create();
				catN.MorphoSyntaxAnalysesOC.Add(catNMSA);

				var sPL = m_entryFactory.Create();
				var sPLForm = m_afxAlloFactory.Create();
				sPL.AlternateFormsOS.Add(sPLForm);
				sPLForm.Form.VernacularDefaultWritingSystem = Cache.TsStrFactory.MakeString("sPLTEST", m_vernacularWS.Handle);
				var sPLMSA = m_inflAffMsaFactory.Create();
				sPL.MorphoSyntaxAnalysesOC.Add(sPLMSA);

				// Verb
				var catV = m_entryFactory.Create();
				var catVForm = m_stemAlloFactory.Create();
				catV.AlternateFormsOS.Add(catVForm);
				catVForm.Form.VernacularDefaultWritingSystem = Cache.TsStrFactory.MakeString("catVTEST", m_vernacularWS.Handle);
				var catVMSA = m_stemMsaFactory.Create();
				catV.MorphoSyntaxAnalysesOC.Add(catVMSA);

				var sAGR = m_entryFactory.Create();
				var sAGRForm = m_afxAlloFactory.Create();
				sAGR.AlternateFormsOS.Add(sAGRForm);
				sAGRForm.Form.VernacularDefaultWritingSystem = Cache.TsStrFactory.MakeString("sAGRTEST", m_vernacularWS.Handle);
				var sAGRMSA = m_inflAffMsaFactory.Create();
				sAGR.MorphoSyntaxAnalysesOC.Add(sAGRMSA);

				xmlFragment = "<Wordform DbRef='" + catsTEST.Hvo + "' Form='catsTEST'>" + Environment.NewLine
									 + "<WfiAnalysis>" + Environment.NewLine
									 + "<Morphs>" + Environment.NewLine
									 + "<Morph>" + Environment.NewLine
									 + "<MoForm DbRef='" + catNForm.Hvo + "' Label='catNTEST'/>" + Environment.NewLine
									 + "<MSI DbRef='" + catNMSA.Hvo + "'/>" + Environment.NewLine
									 + "</Morph>" + Environment.NewLine
									 + "<Morph>" + Environment.NewLine
									 + "<MoForm DbRef='" + sPLForm.Hvo + "' Label='sPLTEST'/>" + Environment.NewLine
									 + "<MSI DbRef='" + sPLMSA.Hvo + "'/>" + Environment.NewLine
									 + "</Morph>" + Environment.NewLine
									 + "</Morphs>" + Environment.NewLine
									 + "</WfiAnalysis>" + Environment.NewLine
									 + "<WfiAnalysis>" + Environment.NewLine
									 + "<Morphs>" + Environment.NewLine
									 + "<Morph>" + Environment.NewLine
									 + "<MoForm DbRef='" + catVForm.Hvo + "' Label='catVTEST'/>" + Environment.NewLine
									 + "<MSI DbRef='" + catVMSA.Hvo + "'/>" + Environment.NewLine
									 + "</Morph>" + Environment.NewLine
									 + "<Morph>" + Environment.NewLine
									 + "<MoForm DbRef='" + sAGRForm.Hvo + "' Label='sAGRTEST'/>" + Environment.NewLine
									 + "<MSI DbRef='" + sAGRMSA.Hvo + "'/>" + Environment.NewLine
									 + "</Morph>" + Environment.NewLine
									 + "</Morphs>" + Environment.NewLine
									 + "</WfiAnalysis>" + Environment.NewLine
									 + "</Wordform>" + Environment.NewLine;
			});

			m_filer.ProcessParse(ParserPriority.Low, xmlFragment, 0);
			ExecuteIdleQueue();
			CheckAnalysisSize("catsTEST", 2, false);
		}

		[Test]
		[Ignore("Is it ever possible for a parser to return more than one wordform parse?")]
		public void TwoWordforms()
		{
			var snakeTEST = CheckAnalysisSize("snakeTEST", 0, true);
			var bullTEST = CheckAnalysisSize("bullTEST", 0, true);
			var ldb = Cache.LanguageProject.LexDbOA;

			string xmlFragment = null;
			UndoableUnitOfWorkHelper.Do("Undo stuff", "Redo stuff", m_actionHandler, () =>
			{
				// Snake
				var snakeN = m_entryFactory.Create();
				var snakeNForm = m_stemAlloFactory.Create();
				snakeN.AlternateFormsOS.Add(snakeNForm);
				snakeNForm.Form.VernacularDefaultWritingSystem = Cache.TsStrFactory.MakeString("snakeNTEST", m_vernacularWS.Handle);
				var snakeNMSA = m_stemMsaFactory.Create();
				snakeN.MorphoSyntaxAnalysesOC.Add(snakeNMSA);

				// Bull
				var bullN = m_entryFactory.Create();
				var bullNForm = m_stemAlloFactory.Create();
				bullN.AlternateFormsOS.Add(bullNForm);
				bullNForm.Form.VernacularDefaultWritingSystem = Cache.TsStrFactory.MakeString("bullNTEST", m_vernacularWS.Handle);
				var bullNMSA = m_stemMsaFactory.Create();
				bullN.MorphoSyntaxAnalysesOC.Add(bullNMSA);

				xmlFragment = "<Wordform DbRef='" + snakeTEST.Hvo + "' Form='snakeTEST'>" + Environment.NewLine
									 + "<WfiAnalysis>" + Environment.NewLine
									 + "<Morphs>" + Environment.NewLine
									 + "<Morph>" + Environment.NewLine
									 + "<MoForm DbRef='" + snakeNForm.Hvo + "' Label='snakeNTEST'/>" + Environment.NewLine
									 + "<MSI DbRef='" + snakeNMSA.Hvo + "'/>" + Environment.NewLine
									 + "</Morph>" + Environment.NewLine
									 + "</Morphs>" + Environment.NewLine
									 + "</WfiAnalysis>" + Environment.NewLine
									 + "</Wordform>" + Environment.NewLine
									 + "<Wordform DbRef='" + bullTEST.Hvo + "' Form='bullTEST'>" + Environment.NewLine
									 + "<WfiAnalysis>" + Environment.NewLine
									 + "<Morphs>" + Environment.NewLine
									 + "<Morph>" + Environment.NewLine
									 + "<MoForm DbRef='" + bullNForm.Hvo + "' Label='bullNTEST'/>" + Environment.NewLine
									 + "<MSI DbRef='" + bullNMSA.Hvo + "'/>" + Environment.NewLine
									 + "</Morph>" + Environment.NewLine
									 + "</Morphs>" + Environment.NewLine
									 + "</WfiAnalysis>" + Environment.NewLine
									 + "</Wordform>" + Environment.NewLine;
			});

			m_filer.ProcessParse(ParserPriority.Low, xmlFragment, 0);
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
			var pigsTEST = CheckAnalysisSize("pigsTEST", 0, true);
			var ldb = Cache.LanguageProject.LexDbOA;

			string xmlFragment = null;
			IWfiAnalysis anal1 = null, anal2 = null, anal3 = null;
			UndoableUnitOfWorkHelper.Do("Undo stuff", "Redo stuff", m_actionHandler, () =>
			{
				// Bear entry
				var pigN = m_entryFactory.Create();
				var pigNForm = m_stemAlloFactory.Create();
				pigN.AlternateFormsOS.Add(pigNForm);
				pigNForm.Form.VernacularDefaultWritingSystem = Cache.TsStrFactory.MakeString("pigNTEST", m_vernacularWS.Handle);
				var pigNMSA = m_stemMsaFactory.Create();
				pigN.MorphoSyntaxAnalysesOC.Add(pigNMSA);
				var pigNLS = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				pigN.SensesOS.Add(pigNLS);

				var analFactory = Cache.ServiceLocator.GetInstance<IWfiAnalysisFactory>();
				var mbFactory = Cache.ServiceLocator.GetInstance<IWfiMorphBundleFactory>();
				// First of two duplicate analyses
				var anal = analFactory.Create();
				pigsTEST.AnalysesOC.Add(anal);
				anal1 = anal;
				var mb = mbFactory.Create();
				anal.MorphBundlesOS.Add(mb);
				mb.MorphRA = pigNForm;
				mb.MsaRA = pigNMSA;
				CheckEvaluationSize(anal1, 0, true, "anal1");

				// Non-duplicate, to make sure it does not get approved.
				anal = analFactory.Create();
				pigsTEST.AnalysesOC.Add(anal);
				anal2 = anal;
				mb = mbFactory.Create();
				anal.MorphBundlesOS.Add(mb);
				mb.SenseRA = pigNLS;
				CheckEvaluationSize(anal2, 0, true, "anal2");

				// Second of two duplicate analyses
				anal = analFactory.Create();
				pigsTEST.AnalysesOC.Add(anal);
				anal3 = anal;
				mb = mbFactory.Create();
				anal.MorphBundlesOS.Add(mb);
				mb.MorphRA = pigNForm;
				mb.MsaRA = pigNMSA;
				CheckEvaluationSize(anal3, 0, true, "anal3");
				CheckAnalysisSize("pigsTEST", 3, false);

				xmlFragment = "<Wordform DbRef='" + pigsTEST.Hvo + "' Form='pigsTEST'>" + Environment.NewLine
									 + "<WfiAnalysis>" + Environment.NewLine
									 + "<Morphs>" + Environment.NewLine
									 + "<Morph>" + Environment.NewLine
									 + "<MoForm DbRef='" + pigNForm.Hvo + "' Label='pigNTEST'/>" + Environment.NewLine
									 + "<MSI DbRef='" + pigNMSA.Hvo + "'/>" + Environment.NewLine
									 + "</Morph>" + Environment.NewLine
									 + "</Morphs>" + Environment.NewLine
									 + "</WfiAnalysis>" + Environment.NewLine
									 + "</Wordform>" + Environment.NewLine;
			});

			m_filer.ProcessParse(ParserPriority.Low, xmlFragment, 0);
			ExecuteIdleQueue();
			CheckEvaluationSize(anal1, 1, false, "anal1Hvo");
			Assert.IsFalse(anal2.IsValidObject, "analysis 2 should end up with no evaluations and so be deleted");
			CheckEvaluationSize(anal3, 1, false, "anal3Hvo");
		}

		[Test]
		public void LexEntryInflTypeTwoAnalyses()
		{
			var crebTEST = CheckAnalysisSize("crebTEST", 0, true);
			var ldb = Cache.LanguageProject.LexDbOA;

			string xmlFragment = null;
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
				believeVForm.Form.VernacularDefaultWritingSystem = Cache.TsStrFactory.MakeString("believeVTEST", m_vernacularWS.Handle);
				var believeVMSA = m_stemMsaFactory.Create();
				believeV.MorphoSyntaxAnalysesOC.Add(believeVMSA);
				var believeVSense = m_senseFactory.Create();
				believeV.SensesOS.Add(believeVSense);
				believeVSense.MorphoSyntaxAnalysisRA = believeVMSA;

				var seekV = m_entryFactory.Create();
				var seekVForm = m_stemAlloFactory.Create();
				believeV.AlternateFormsOS.Add(seekVForm);
				seekVForm.Form.VernacularDefaultWritingSystem = Cache.TsStrFactory.MakeString("seekVTEST", m_vernacularWS.Handle);
				var seekVMSA = m_stemMsaFactory.Create();
				seekV.MorphoSyntaxAnalysesOC.Add(seekVMSA);
				var seekVSense = m_senseFactory.Create();
				seekV.SensesOS.Add(seekVSense);
				seekVSense.MorphoSyntaxAnalysisRA = seekVMSA;

				var crebV = m_entryFactory.Create();
				var crebVForm = m_stemAlloFactory.Create();
				crebV.AlternateFormsOS.Add(crebVForm);
				crebVForm.Form.VernacularDefaultWritingSystem = Cache.TsStrFactory.MakeString("crebVTEST", m_vernacularWS.Handle);
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

				var nullPAST = m_entryFactory.Create();
				var nullPASTForm = m_afxAlloFactory.Create();
				nullPAST.AlternateFormsOS.Add(nullPASTForm);
				nullPASTForm.Form.VernacularDefaultWritingSystem = Cache.TsStrFactory.MakeString("nullPASTTEST", m_vernacularWS.Handle);
				var nullPASTMSA = m_inflAffMsaFactory.Create();
				nullPAST.MorphoSyntaxAnalysesOC.Add(nullPASTMSA);

				var nullPLURAL = m_entryFactory.Create();
				var nullPLURALForm = m_afxAlloFactory.Create();
				nullPLURAL.AlternateFormsOS.Add(nullPLURALForm);
				nullPLURALForm.Form.VernacularDefaultWritingSystem = Cache.TsStrFactory.MakeString("nullPLURALTEST", m_vernacularWS.Handle);
				var nullPluralMSA = m_inflAffMsaFactory.Create();
				nullPLURAL.MorphoSyntaxAnalysesOC.Add(nullPluralMSA);

				xmlFragment = "<Wordform DbRef='" + crebTEST.Hvo + "' Form='crebTEST'>" + Environment.NewLine
									 + "<WfiAnalysis>" + Environment.NewLine
									 + "<Morphs>" + Environment.NewLine
									 + "<Morph>" + Environment.NewLine
									 + "<MoForm DbRef='" + crebVForm.Hvo + "' Label='crebVTEST'/>" + Environment.NewLine
									 + "<MSI DbRef='" + crebV.Hvo + ".1'/>" + Environment.NewLine
									 + "</Morph>" + Environment.NewLine
									 + "<Morph>" + Environment.NewLine
									 + "<MoForm DbRef='" + pastTenseLexEntryInflType.Hvo + "' Label='TEST'/>" + Environment.NewLine
									 + "<MSI DbRef='" + pastTenseLexEntryInflType.Hvo + "'/>" + Environment.NewLine
									 + "</Morph>" + Environment.NewLine
									 + "<Morph>" + Environment.NewLine
									 + "<MoForm DbRef='" + pluralTenseLexEntryInflType.Hvo + "' Label='TEST'/>" + Environment.NewLine
									 + "<MSI DbRef='" + pluralTenseLexEntryInflType.Hvo + "'/>" + Environment.NewLine
									 + "</Morph>" + Environment.NewLine
									 + "</Morphs>" + Environment.NewLine
									 + "</WfiAnalysis>" + Environment.NewLine
									 + "<WfiAnalysis>" + Environment.NewLine
									 + "<Morphs>" + Environment.NewLine
									 + "<Morph>" + Environment.NewLine
									 + "<MoForm DbRef='" + crebVForm.Hvo + "' Label='crebVTEST'/>" + Environment.NewLine
									 + "<MSI DbRef='" + crebV.Hvo + "'/>" + Environment.NewLine
									 + "</Morph>" + Environment.NewLine
									 + "<Morph>" + Environment.NewLine
									 + "<MoForm DbRef='" + pastTenseLexEntryInflType.Hvo + "' Label='TEST'/>" + Environment.NewLine
									 + "<MSI DbRef='" + pastTenseLexEntryInflType.Hvo + "'/>" + Environment.NewLine
									 + "</Morph>" + Environment.NewLine
									 + "<Morph>" + Environment.NewLine
									 + "<MoForm DbRef='" + pluralTenseLexEntryInflType.Hvo + "' Label='TEST'/>" + Environment.NewLine
									 + "<MSI DbRef='" + pluralTenseLexEntryInflType.Hvo + "'/>" + Environment.NewLine
									 + "</Morph>" + Environment.NewLine
									 + "</Morphs>" + Environment.NewLine
									 + "</WfiAnalysis>" + Environment.NewLine + "</Wordform>" + Environment.NewLine;
			});

			m_filer.ProcessParse(ParserPriority.Low, xmlFragment, 0);
			ExecuteIdleQueue();
			CheckAnalysisSize("crebTEST", 2, false);
			foreach (var analysis in crebTEST.AnalysesOC)
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
			var brubsTEST = CheckAnalysisSize("brubsTEST", 0, true);
			var ldb = Cache.LanguageProject.LexDbOA;

			string xmlFragment = null;
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
				believeVForm.Form.VernacularDefaultWritingSystem = Cache.TsStrFactory.MakeString("believeVTEST", m_vernacularWS.Handle);
				var believeVMSA = m_stemMsaFactory.Create();
				believeV.MorphoSyntaxAnalysesOC.Add(believeVMSA);
				var believeVSense = m_senseFactory.Create();
				believeV.SensesOS.Add(believeVSense);
				believeVSense.MorphoSyntaxAnalysisRA = believeVMSA;

				var brubV = m_entryFactory.Create();
				var brubVForm = m_stemAlloFactory.Create();
				brubV.AlternateFormsOS.Add(brubVForm);
				brubVForm.Form.VernacularDefaultWritingSystem = Cache.TsStrFactory.MakeString("brubVTEST", m_vernacularWS.Handle);
				var lexEntryref = m_lexEntryRefFactory.Create();
				brubV.EntryRefsOS.Add(lexEntryref);
				lexEntryref.ComponentLexemesRS.Add(believeV);
				lexEntryref.VariantEntryTypesRS.Add(presentTenseLexEntryInflType);

				var nullPRESENT = m_entryFactory.Create();
				var nullPRESENTForm = m_afxAlloFactory.Create();
				nullPRESENT.AlternateFormsOS.Add(nullPRESENTForm);
				nullPRESENTForm.Form.VernacularDefaultWritingSystem = Cache.TsStrFactory.MakeString("nullPRESENTTEST", m_vernacularWS.Handle);
				var nullPRESENTMSA = m_inflAffMsaFactory.Create();
				nullPRESENT.MorphoSyntaxAnalysesOC.Add(nullPRESENTMSA);

				var sPLURAL = m_entryFactory.Create();
				var sPLURALForm = m_afxAlloFactory.Create();
				sPLURAL.AlternateFormsOS.Add(sPLURALForm);
				sPLURALForm.Form.VernacularDefaultWritingSystem = Cache.TsStrFactory.MakeString("sPLURALTEST", m_vernacularWS.Handle);
				var sPluralMSA = m_inflAffMsaFactory.Create();
				sPLURAL.MorphoSyntaxAnalysesOC.Add(sPluralMSA);

				xmlFragment = "<Wordform DbRef='" + brubsTEST.Hvo + "' Form='brubsTEST'>" + Environment.NewLine
									 + "<WfiAnalysis>" + Environment.NewLine
									 + "<Morphs>" + Environment.NewLine
									 + "<Morph>" + Environment.NewLine
									 + "<MoForm DbRef='" + brubVForm.Hvo + "' Label='brubVTEST'/>" + Environment.NewLine
									 + "<MSI DbRef='" + brubV.Hvo + "'/>" + Environment.NewLine
									 + "</Morph>" + Environment.NewLine
									 + "<Morph>" + Environment.NewLine
									 + "<MoForm DbRef='" + presentTenseLexEntryInflType.Hvo + "' Label='TEST'/>" + Environment.NewLine
									 + "<MSI DbRef='" + presentTenseLexEntryInflType.Hvo + "'/>" + Environment.NewLine
									 + "</Morph>" + Environment.NewLine
									 + "<Morph>" + Environment.NewLine
									 + "<MoForm DbRef='" + sPLURALForm.Hvo + "' Label='sPLURALTEST'/>" + Environment.NewLine
									 + "<MSI DbRef='" + sPluralMSA.Hvo + "'/>" + Environment.NewLine
									 + "</Morph>" + Environment.NewLine
									 + "</Morphs>" + Environment.NewLine
									 + "</WfiAnalysis>" + Environment.NewLine
									 + "</Wordform>" + Environment.NewLine;
			});

			m_filer.ProcessParse(ParserPriority.Low, xmlFragment, 0);
			ExecuteIdleQueue();
			CheckAnalysisSize("brubsTEST", 1, false);
			var analysis = brubsTEST.AnalysesOC.ElementAt(0);
			Assert.AreEqual(2, analysis.MorphBundlesOS.Count, "Expected only 2 morphs in the analysis");
			var morphBundle = analysis.MorphBundlesOS.ElementAt(0);
			Assert.IsNotNull(morphBundle.Form, "First bundle: form is not null");
			Assert.IsNotNull(morphBundle.MsaRA, "First bundle: msa is not null");
			Assert.IsNotNull(morphBundle.InflTypeRA, "First bundle: infl type is not null");
		}

		#endregion // Tests
	}
}
