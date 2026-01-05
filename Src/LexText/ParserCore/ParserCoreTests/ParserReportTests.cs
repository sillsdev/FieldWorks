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

		private void CheckParseReport(ParseReport report, int numAnalyses = 0, int numApprovedMissing = 0,
			int numDisapproved = 0, int numNoOpinion = 0, int parseTime = 0, string errorMessage = null)
		{
			Assert.That(report.NumAnalyses, Is.EqualTo(numAnalyses));
			Assert.That(report.NumUserDisapprovedAnalyses, Is.EqualTo(numDisapproved));
			Assert.That(report.NumUserApprovedAnalysesMissing, Is.EqualTo(numApprovedMissing));
			Assert.That(report.NumUserNoOpinionAnalyses, Is.EqualTo(numNoOpinion));
			Assert.That(report.ParseTime, Is.EqualTo(parseTime));
			Assert.That(report.ErrorMessage, Is.EqualTo(errorMessage));
		}

		private void CheckParserReport(ParserReport report, int numParseErrors = 0, int numWords = 0,
			int numZeroParses = 0, int totalAnalyses = 0, int totalApprovedMissing = 0,
			int totalDisapproved = 0, int totalNoOpinion = 0,int totalParseTime = 0)
		{
			Assert.That(report.TotalAnalyses, Is.EqualTo(totalAnalyses));
			Assert.That(report.TotalUserDisapprovedAnalyses, Is.EqualTo(totalDisapproved));
			Assert.That(report.TotalUserApprovedAnalysesMissing, Is.EqualTo(totalApprovedMissing));
			Assert.That(report.TotalUserNoOpinionAnalyses, Is.EqualTo(totalNoOpinion));
			Assert.That(report.NumParseErrors, Is.EqualTo(numParseErrors));
			Assert.That(report.NumWords, Is.EqualTo(numWords));
			Assert.That(report.NumZeroParses, Is.EqualTo(numZeroParses));
			Assert.That(report.TotalParseTime, Is.EqualTo(totalParseTime));
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

			var catWordform = FindOrCreateWordform("cat");
			var errorWordform = FindOrCreateWordform("error");
			var zeroWordform = FindOrCreateWordform("zero");
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
				// Verb
				ILexEntry catV = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				IMoStemAllomorph catVForm = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
				catV.AlternateFormsOS.Add(catVForm);
				catVForm.Form.VernacularDefaultWritingSystem = TsStringUtils.MakeString("catv", m_vernacularWS.Handle);
				IMoStemMsa catVMsa = Cache.ServiceLocator.GetInstance<IMoStemMsaFactory>().Create();
				catV.MorphoSyntaxAnalysesOC.Add(catVMsa);
				var parseMorph2 = new ParseMorph(catVForm, catVMsa);
				// Other
				ILexEntry catX = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				IMoStemAllomorph catXForm = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
				catX.AlternateFormsOS.Add(catXForm);
				catXForm.Form.VernacularDefaultWritingSystem = TsStringUtils.MakeString("catx", m_vernacularWS.Handle);
				IMoStemMsa catXMsa = Cache.ServiceLocator.GetInstance<IMoStemMsaFactory>().Create();
				catX.MorphoSyntaxAnalysesOC.Add(catXMsa);
				var parseMorph3 = new ParseMorph(catXForm, catXMsa);

				result = new ParseResult(new[]
				{
					new ParseAnalysis(new[] { parseMorph }),
					new ParseAnalysis(new[] { parseMorph2 }),
					new ParseAnalysis(new[] { parseMorph2, parseMorph2 }),
					new ParseAnalysis(new[] { parseMorph3 })
				});
				var analysis = CreateIWfiAnalysis(catWordform, new List<ParseMorph> {parseMorph});
				analysis.SetAgentOpinion(Cache.LanguageProject.DefaultUserAgent, Opinions.approves);
				var analysisX = CreateIWfiAnalysis(catWordform, new List<ParseMorph> { parseMorph3 });
				analysisX.SetAgentOpinion(Cache.LanguageProject.DefaultUserAgent, Opinions.disapproves);
				// Missing approved analyses.
				var analysis2 = CreateIWfiAnalysis(catWordform, new List<ParseMorph> { parseMorph, parseMorph });
				analysis2.SetAgentOpinion(Cache.LanguageProject.DefaultUserAgent, Opinions.approves);
				var analysis3 = CreateIWfiAnalysis(catWordform, new List<ParseMorph> { parseMorph, parseMorph, parseMorph });
				analysis3.SetAgentOpinion(Cache.LanguageProject.DefaultUserAgent, Opinions.approves);
				var analysis4 = CreateIWfiAnalysis(catWordform, new List<ParseMorph> { parseMorph, parseMorph, parseMorph, parseMorph });
				analysis4.SetAgentOpinion(Cache.LanguageProject.DefaultUserAgent, Opinions.approves);
				result.ParseTime = 10;
			});

			var parseReport = new ParseReport(catWordform, result);
			CheckParseReport(parseReport, numAnalyses: 4, numApprovedMissing: 3, numDisapproved: 1, numNoOpinion: 2, parseTime: 10);

			var errorResult = new ParseResult("error"){ ParseTime = 1 };
			var errorReport = new ParseReport(catWordform, errorResult);
			CheckParseReport(errorReport, numApprovedMissing: 4, parseTime: 1, errorMessage: "error");
			errorReport = new ParseReport(errorWordform, errorResult);
			CheckParseReport(errorReport, parseTime: 1, errorMessage: "error");

			var zeroResult = new ParseResult(Enumerable.Empty<ParseAnalysis>()){ ParseTime = 2 };
			var zeroReport = new ParseReport(catWordform, zeroResult);
			CheckParseReport(zeroReport, numApprovedMissing: 4, parseTime: 2);
			zeroReport = new ParseReport(zeroWordform, zeroResult);
			CheckParseReport(zeroReport, parseTime: 2);

			var parserReport = new ParserReport(Cache);
			parserReport.SourceText = "Testbed";
			parserReport.AddParseReport("cat", parseReport);
			parserReport.AddParseReport("error", errorReport);
			parserReport.AddParseReport("zero", zeroReport);
			Assert.That(parserReport.ParseReports.ContainsKey("cat"), Is.True);
			CheckParserReport(parserReport, numParseErrors: 1, numWords: 3,
				numZeroParses: 2, totalAnalyses: 4, totalApprovedMissing: 3,
				totalDisapproved: 1, totalNoOpinion: 2, totalParseTime: 13);

			// Check SubtractParseReport.
			var eeReport = errorReport.DiffParseReport(errorReport);
			CheckParseReport(eeReport);

			var epReport = parseReport.DiffParseReport(errorReport);
			CheckParseReport(epReport, numAnalyses: 4, numApprovedMissing: 3,
				numDisapproved: 1, numNoOpinion: 2, parseTime: 9, errorMessage: "error => ");

			var ezReport = errorReport.DiffParseReport(zeroReport);
			CheckParseReport(ezReport, parseTime: -1, errorMessage: " => error");

			var peReport = errorReport.DiffParseReport(parseReport);
			CheckParseReport(peReport, numAnalyses: -4, numApprovedMissing: -3,
				numDisapproved: -1, numNoOpinion: -2, parseTime: -9, errorMessage: " => error");

			var ppReport = parseReport.DiffParseReport(parseReport);
			CheckParseReport(ppReport);

			var pzReport = zeroReport.DiffParseReport(parseReport);
			CheckParseReport(pzReport, numAnalyses: -4, numApprovedMissing: -3,
				numDisapproved: -1, numNoOpinion: -2, parseTime: -8);

			var zeReport = errorReport.DiffParseReport(zeroReport);
			CheckParseReport(zeReport, parseTime: -1, errorMessage: " => error");

			var zpReport = parseReport.DiffParseReport(zeroReport);
			CheckParseReport(zpReport, numAnalyses: 4, numApprovedMissing: 3,
				numDisapproved: 1, numNoOpinion: 2, parseTime: 8);

			var zzReport = zeroReport.DiffParseReport(zeroReport);
			CheckParseReport(zzReport);

			var parserReport2 = new ParserReport();
			parserReport2.AddParseReport("cat", parseReport);
			parserReport2.AddParseReport("extra", zeroReport);
			var diff = parserReport2.DiffParserReports(parserReport);
			Assert.That(diff.ParseReports.ContainsKey("extra"), Is.True);
			CheckParseReport(diff.ParseReports["extra"], parseTime: 2);
			Assert.That(diff.ParseReports["extra"].Word, Is.EqualTo(" => zero"));
			Assert.That(diff.ParseReports.ContainsKey("zero"), Is.True);
			CheckParseReport(diff.ParseReports["zero"], parseTime: -2);
			Assert.That(diff.ParseReports["zero"].Word, Is.EqualTo("zero => "));
			Assert.That(diff.ParseReports.ContainsKey("cat"), Is.True);
			CheckParseReport(diff.ParseReports["cat"]);
		}

	}
}
