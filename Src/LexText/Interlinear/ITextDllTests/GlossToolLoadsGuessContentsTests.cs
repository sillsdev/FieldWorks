// Copyright (c) 2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using NUnit.Framework;
using Rhino.Mocks;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.CoreImpl;

namespace SIL.FieldWorks.IText
{

	/// <summary/>
	[TestFixture]
	public class GlossToolLoadsGuessContentsTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private FDO.IText text;
		private AddWordsToLexiconTests.SandboxForTests m_sandbox;

		/// <summary/>
		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor,
				DoSetupFixture);
		}

		private void DoSetupFixture()
		{
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
																				 InterlinLineChoices.InterlinMode.Gloss);
			m_sandbox = new AddWordsToLexiconTests.SandboxForTests(Cache, lineChoices);
		}
		/// <summary>
		/// This unit test simulates selecting a wordform in interlinear view configured for glossing where there is an Analysis guess.
		/// The first meaning from the Analysis should be used to fill in the gloss in the sandbox.
		/// </summary>
		[Test]
		public void SandBoxWithGlossConfig_LoadsGuessForGlossFromAnalysis()
		{
			var mockRb = MockRepository.GenerateMock<IVwRootBox>();
			mockRb.Expect(rb => rb.DataAccess).Return(Cache.MainCacheAccessor);
			var textFactory = Cache.ServiceLocator.GetInstance<ITextFactory>();
			var stTextFactory = Cache.ServiceLocator.GetInstance<IStTextFactory>();
			text = textFactory.Create();
			var stText1 = stTextFactory.Create();
			text.ContentsOA = stText1;
			var para1 = stText1.AddNewTextPara(null);
			(text.ContentsOA[0]).Contents = TsStringUtils.MakeTss("xxxa xxxa xxxa.", Cache.DefaultVernWs);
			InterlinMaster.LoadParagraphAnnotationsAndGenerateEntryGuessesIfNeeded(stText1, true);
			using (var mockInterlinDocForAnalyis = new MockInterlinDocForAnalyis(stText1) { MockedRootBox = mockRb })
			{
				m_sandbox.SetInterlinDocForTest(mockInterlinDocForAnalyis);

				var cba0_0 = AddWordsToLexiconTests.GetNewAnalysisOccurence(text, 0, 0, 0);
				var wf = Cache.ServiceLocator.GetInstance<IWfiWordformFactory>().Create(TsStringUtils.MakeTss("xxxa", Cache.DefaultVernWs));
				cba0_0.Analysis = Cache.ServiceLocator.GetInstance<IWfiAnalysisFactory>().Create(wf, Cache.ServiceLocator.GetInstance<IWfiGlossFactory>());
				var gloss = cba0_0.Analysis.Analysis.MeaningsOC.First();
				var glossTss = TsStringUtils.MakeTss("I did it", Cache.DefaultAnalWs);
				gloss.Form.set_String(Cache.DefaultAnalWs, glossTss);
				m_sandbox.SwitchWord(cba0_0);
				// Verify that the wordgloss was loaded into the m_sandbox
				Assert.AreNotEqual(0, m_sandbox.WordGlossHvo, "The gloss was not set to Default gloss from the analysis.");
				Assert.AreEqual(m_sandbox.WordGlossHvo, gloss.Hvo, "The gloss was not set to Default gloss from the analysis.");
			}
		}
	}
}
