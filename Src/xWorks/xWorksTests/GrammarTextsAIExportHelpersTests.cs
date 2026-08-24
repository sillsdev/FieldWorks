// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System.Collections.Generic;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Application.ApplicationServices;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.XWorks
{
	[TestFixture]
	public class GrammarTextsAIExportHelpersTests
	{
		private LcmCache m_cache;

		[SetUp]
		public void CreateMockCache()
		{
			m_cache = LcmCache.CreateCacheWithNewBlankLangProj(
				new TestProjectId(BackendProviderType.kMemoryOnly, null), "en", "fr", "en", new DummyLcmUI(),
				FwDirectoryFinder.LcmDirectories, new LcmSettings());
		}

		[TearDown]
		public void DestroyMockCache()
		{
			m_cache.Dispose();
			m_cache = null;
		}

		private SIL.LCModel.IText MakeTextWithOneParagraph(string vernacularWord, out IStTxtPara para)
		{
			SIL.LCModel.IText text = null;
			UndoableUnitOfWorkHelper.Do("Undo", "Redo", m_cache.ActionHandlerAccessor, () =>
			{
				text = m_cache.ServiceLocator.GetInstance<ITextFactory>().Create();
				m_cache.LangProject.Texts.Add(text);
				var stText = m_cache.ServiceLocator.GetInstance<IStTextFactory>().Create();
				text.ContentsOA = stText;
				var newPara = m_cache.ServiceLocator.GetInstance<IStTxtParaFactory>().Create();
				stText.ParagraphsOS.Add(newPara);
				newPara.Contents = TsStringUtils.MakeString(vernacularWord, m_cache.DefaultVernWs);
			});
			para = (IStTxtPara)text.ContentsOA[0];
			return text;
		}

		[Test]
		public void CountWordsAndAnalyses_UnanalyzedParagraph_CountsWordsButNoAnalyses()
		{
			IStTxtPara para;
			var text = MakeTextWithOneParagraph("bonjour tout le monde", out para);
			UndoableUnitOfWorkHelper.Do("Undo", "Redo", m_cache.ActionHandlerAccessor, () =>
			{
				using (var pp = new ParagraphParser(m_cache))
					pp.Parse(para);
			});

			var counts = GrammarTextsAIExportHelpers.CountWordsAndAnalyses(text.ContentsOA);

			Assert.That(counts.Words, Is.EqualTo(4));
			Assert.That(counts.Analyses, Is.EqualTo(0));
		}

		[Test]
		public void CountWordsAndAnalyses_OneWordGivenARealAnalysis_CountsThatWordAsAnalyzed()
		{
			IStTxtPara para;
			var text = MakeTextWithOneParagraph("bonjour", out para);
			UndoableUnitOfWorkHelper.Do("Undo", "Redo", m_cache.ActionHandlerAccessor, () =>
			{
				using (var pp = new ParagraphParser(m_cache))
					pp.Parse(para);
			});
			var segment = para.SegmentsOS[0];
			var wordform = (IWfiWordform)segment.AnalysesRS[0];
			UndoableUnitOfWorkHelper.Do("Undo", "Redo", m_cache.ActionHandlerAccessor, () =>
			{
				var analysis = m_cache.ServiceLocator.GetInstance<IWfiAnalysisFactory>().Create();
				wordform.AnalysesOC.Add(analysis);
				segment.AnalysesRS[0] = analysis;
			});

			var counts = GrammarTextsAIExportHelpers.CountWordsAndAnalyses(text.ContentsOA);

			Assert.That(counts.Words, Is.EqualTo(1));
			Assert.That(counts.Analyses, Is.EqualTo(1));
		}

		[Test]
		public void GetTextDisplayName_OwnedByAnIText_ReturnsTextName()
		{
			IStTxtPara para;
			var text = MakeTextWithOneParagraph("hello", out para);
			UndoableUnitOfWorkHelper.Do("Undo", "Redo", m_cache.ActionHandlerAccessor, () =>
			{
				text.Name.SetAnalysisDefaultWritingSystem("My Test Text");
			});

			var name = GrammarTextsAIExportHelpers.GetTextDisplayName(text.ContentsOA);

			Assert.That(name, Is.EqualTo("My Test Text"));
		}

		[Test]
		public void MakeSafeFileName_StripsInvalidCharactersAndDedupes()
		{
			var used = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase) { "HCGrammar" };

			var first = GrammarTextsAIExportHelpers.MakeSafeFileName("Story: Part 1?", used);
			var second = GrammarTextsAIExportHelpers.MakeSafeFileName("Story: Part 1?", used);

			Assert.That(first, Is.EqualTo("Story_ Part 1_"));
			Assert.That(second, Is.EqualTo("Story_ Part 1_ (2)"));
			Assert.That(used, Does.Contain(first));
			Assert.That(used, Does.Contain(second));
		}
	}
}
