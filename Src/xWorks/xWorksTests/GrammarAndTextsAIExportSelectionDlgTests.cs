// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Application.ApplicationServices;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.XWorks
{
	[TestFixture]
	public class GrammarAndTextsAIExportSelectionDlgTests
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

		private IStText MakeText(string title)
		{
			SIL.LCModel.IText text = null;
			UndoableUnitOfWorkHelper.Do("Undo", "Redo", m_cache.ActionHandlerAccessor, () =>
			{
				text = m_cache.ServiceLocator.GetInstance<ITextFactory>().Create();
				m_cache.LangProject.Texts.Add(text);
				text.ContentsOA = m_cache.ServiceLocator.GetInstance<IStTextFactory>().Create();
				text.Name.SetAnalysisDefaultWritingSystem(title);
			});
			return text.ContentsOA;
		}

		[Test]
		public void ApplyPreviousSelection_OnlyChecksTextsThatWereSelectedBefore()
		{
			var textA = MakeText("Text A");
			var textB = MakeText("Text B");
			using (var dlg = new GrammarAndTextsAIExportSelectionDlg(m_cache, new[] { textA, textB }))
			{
				dlg.ApplyPreviousSelection(new HashSet<string> { textA.Guid.ToString() });

				var selected = dlg.SelectedTexts.ToList();
				Assert.That(selected, Has.Count.EqualTo(1));
				Assert.That(selected[0], Is.SameAs(textA));
			}
		}

		[Test]
		public void ApplyPreviousSelection_WithNoPriorSelection_ChecksEveryText()
		{
			var textA = MakeText("Text A");
			var textB = MakeText("Text B");
			using (var dlg = new GrammarAndTextsAIExportSelectionDlg(m_cache, new[] { textA, textB }))
			{
				dlg.ApplyPreviousSelection(null);

				Assert.That(dlg.SelectedTexts.ToList(), Has.Count.EqualTo(2));
			}
		}
	}
}
