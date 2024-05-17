using NUnit.Framework;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XCore;

namespace SIL.FieldWorks.WordWorks.Parser
{
	[TestFixture]
	public class ParserReportTests : MemoryOnlyBackendProviderTestBase
	{
		#region Data Members
		private CoreWritingSystemDefinition m_vernacularWS;
		private IWfiAnalysisFactory m_analysisFactory;
		private IWfiMorphBundleFactory m_mbFactory;
		#endregion Data Members

		#region Non-test methods
		private IWfiWordform FindOrCreateWordform(string form)
		{
			ILcmServiceLocator servLoc = Cache.ServiceLocator;
			IWfiWordform wf = servLoc.GetInstance<IWfiWordformRepository>().GetMatchingWordform(m_vernacularWS.Handle, form);
			if (wf == null)
			{
				UndoableUnitOfWorkHelper.Do("Undo create", "Redo create", m_actionHandler,
					() => wf = servLoc.GetInstance<IWfiWordformFactory>().Create(TsStringUtils.MakeString(form, m_vernacularWS.Handle)));
			}
			return wf;
		}

		private IWfiAnalysis CreateIWfiAnalysis(IWfiWordform wordform, List<ParseMorph> morphs)
		{
			// Create a new analysis, since there are no matches.
			var newAnal = m_analysisFactory.Create();
			wordform.AnalysesOC.Add(newAnal);
			// Make WfiMorphBundle(s).
			foreach (ParseMorph morph in morphs)
			{
				IWfiMorphBundle mb = m_mbFactory.Create();
				newAnal.MorphBundlesOS.Add(mb);
				mb.MorphRA = morph.Form;
				mb.MsaRA = morph.Msa;
				if (morph.InflType != null)
					mb.InflTypeRA = morph.InflType;
			}
			return newAnal;
		}

		protected IWfiWordform CheckAnalysisSize(string form, int expectedSize, bool isStarting)
		{
			IWfiWordform wf = FindOrCreateWordform(form);
			int actualSize = wf.AnalysesOC.Count;
			string msg = String.Format("Wrong number of {0} analyses for: {1}", isStarting ? "starting" : "ending", form);
			Assert.AreEqual(expectedSize, actualSize, msg);
			return wf;
		}
		#endregion // Non-tests

		#region Setup and TearDown
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			m_vernacularWS = Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem;
			m_analysisFactory = Cache.ServiceLocator.GetInstance<IWfiAnalysisFactory>();
			m_mbFactory = Cache.ServiceLocator.GetInstance<IWfiMorphBundleFactory>();
		}

		public override void FixtureTeardown()
		{
			m_vernacularWS = null;
			m_analysisFactory = null;
			m_mbFactory = null;

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

		[Test]
		public void TestAddParseResult()
		{
			ILexDb ldb = Cache.LanguageProject.LexDbOA;

			var wordform = FindOrCreateWordform("cat");
			ParseResult result = null;
			UndoableUnitOfWorkHelper.Do("Undo stuff", "Redo stuff", m_actionHandler, () =>
			{
				// Noun
				ILexEntry catN = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				IMoStemAllomorph catNForm = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
				catN.AlternateFormsOS.Add(catNForm);
				catNForm.Form.VernacularDefaultWritingSystem = TsStringUtils.MakeString("catn", m_vernacularWS.Handle);
				IMoStemMsa catNMsa = Cache.ServiceLocator.GetInstance<IMoStemMsaFactory>().Create();
				catN.MorphoSyntaxAnalysesOC.Add(catNMsa);
				var parseMorph = new ParseMorph(catNForm, catNMsa);
				result = new ParseResult(new[]
				{
					new ParseAnalysis(new[]
					{
						parseMorph,
					})
				});
				var analysis = CreateIWfiAnalysis(wordform, new List<ParseMorph> {parseMorph});
				analysis.SetAgentOpinion(Cache.LanguageProject.DefaultUserAgent, Opinions.disapproves);
				var analysis2 = CreateIWfiAnalysis(wordform, new List<ParseMorph> { parseMorph, parseMorph });
				analysis2.SetAgentOpinion(Cache.LanguageProject.DefaultUserAgent, Opinions.approves);
				result.ParseTime = 10;
			});

			var parseReport = new ParseReport(wordform, result);
			Assert.IsNull(parseReport.ErrorMessage);
			Assert.AreEqual(1, parseReport.NumAnalyses);
			Assert.AreEqual(10, parseReport.ParseTime);
			Assert.AreEqual(1, parseReport.NumHumanApprovedAnalysesMissing);
			Assert.AreEqual(1, parseReport.NumHumanDisapprovedAnalyses);

			var errorResult = new ParseResult("error"){ ParseTime = 10 };
			var errorReport = new ParseReport(wordform, errorResult);
			Assert.AreEqual("error", errorReport.ErrorMessage);
			Assert.AreEqual(0, errorReport.NumAnalyses);
			Assert.AreEqual(10, errorReport.ParseTime);

			var zeroResult = new ParseResult(Enumerable.Empty<ParseAnalysis>()){ ParseTime = 10 };
			var zeroReport = new ParseReport(wordform, zeroResult);
			Assert.IsNull(zeroReport.ErrorMessage);
			Assert.AreEqual(0, zeroReport.NumAnalyses);
			Assert.AreEqual(10, zeroReport.ParseTime);

			var parserReport = new ParserReport();
			parserReport.AddParseReport("cat", parseReport);
			parserReport.AddParseReport("error", errorReport);
			parserReport.AddParseReport("zero", zeroReport);
			Assert.IsTrue(parserReport.ParseReports.ContainsKey("cat"));
			Assert.AreEqual(1, parserReport.NumAnalyses);
			Assert.AreEqual(1, parserReport.NumHumanApprovedAnalysesMissing);
			Assert.AreEqual(1, parserReport.NumHumanDisapprovedAnalyses);
			Assert.AreEqual(1, parserReport.NumParseErrors);
			Assert.AreEqual(3, parserReport.NumWords);
			Assert.AreEqual(1, parserReport.NumZeroParses);
			Assert.AreEqual(30, parserReport.TotalParseTime);
		}

	}
}
