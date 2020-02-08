// Copyright (c) 2010-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using LanguageExplorer.Areas.TextsAndWords.Interlinear;
using LanguageExplorer.Filters;
using NUnit.Framework;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorerTests.DictionaryConfiguration
{
	/// <summary>
	/// Test the filter which eliminates words which occur only in texts not included in the
	/// current filter.
	/// </summary>
	[TestFixture]
	public class WordsUsedOnlyElsewhereFilterTests : MemoryOnlyBackendProviderTestBase
	{
		[Test]
		public void TryItOut()
		{
			var filter = new WordsUsedOnlyElsewhereFilter(Cache);
			filter.Init(Cache, null);
			var sda = new FakeDecorator((ISilDataAccessManaged)Cache.DomainDataByFlid);
			filter.DataAccess = sda;
			UndoableUnitOfWorkHelper.Do("undo", "redo", m_actionHandler, () =>
			{
				var wfTry = MakeWordform("try");
				var seg1 = MakeText("try it out");
				seg1.AnalysesRS.Add(wfTry);
				Assert.That(wfTry.FullConcordanceCount, Is.EqualTo(1));
				var itemTry = new ManyOnePathSortItem(wfTry);
				// Here the global count is non-zero but the corpus count is zero.
				Assert.That(filter.Accept(itemTry), Is.False, "should not accept an item which occurs elsewhere but not in corpus");
				sda.HvoToOccurrenceCount[wfTry.Hvo] = 1;
				Assert.That(filter.Accept(itemTry), Is.True, "should accept an item in an included text");
				sda.HvoToOccurrenceCount[wfTry.Hvo] = 5;
				Assert.That(filter.Accept(itemTry), Is.True, "should accept an item in an included text, even if there are other occurrences");
				var wfNowhere = MakeWordform("nowhere");
				var itemNowhere = new ManyOnePathSortItem(wfNowhere);
				Assert.That(filter.Accept(itemNowhere), Is.True,"should accept an item that occurs nowhere at all.");
			});
		}

		private void SetVernAlternative(IMultiUnicode mu, string content)
		{
			mu.VernacularDefaultWritingSystem = MakeVernTss(content);
		}

		private ITsString MakeVernTss(string content)
		{
			return TsStringUtils.MakeString(content, Cache.DefaultVernWs);
		}

		private IWfiWordform MakeWordform(string form)
		{
			var wf = Cache.ServiceLocator.GetInstance<IWfiWordformFactory>().Create();
			SetVernAlternative(wf.Form, form);
			return wf;
		}

		private ISegment MakeText(string content)
		{
			var text = Cache.ServiceLocator.GetInstance<ITextFactory>().Create();
			var stText = Cache.ServiceLocator.GetInstance<IStTextFactory>().Create();
			text.ContentsOA = stText;
			var para = Cache.ServiceLocator.GetInstance<IStTxtParaFactory>().Create();
			stText.ParagraphsOS.Add(para);
			para.Contents = MakeVernTss(content);
			var seg = Cache.ServiceLocator.GetInstance<ISegmentFactory>().Create();
			para.SegmentsOS.Add(seg);
			return seg;
		}

		/// <summary>
		/// a Decorator which implements OccurrenceCount to return whatever we want (0 by default).
		/// </summary>
		private sealed class FakeDecorator : DomainDataByFlidDecoratorBase
		{
			internal FakeDecorator(ISilDataAccessManaged domainDataByFlid) : base(domainDataByFlid)
			{
				m_mdc = new FakeMdc((IFwMetaDataCacheManaged)domainDataByFlid.MetaDataCache);
			}

			internal readonly Dictionary<int, int> HvoToOccurrenceCount = new Dictionary<int, int>();

			private IFwMetaDataCacheManaged m_mdc;

			public override IFwMetaDataCache MetaDataCache
			{
				get
				{
					return m_mdc;
				}
				set
				{
					base.MetaDataCache = value;
				}
			}

			public override int get_IntProp(int hvo, int tag)
			{
				if (tag == FakeMdc.kMadeUpFieldIdentifier)
				{
					int result;
					HvoToOccurrenceCount.TryGetValue(hvo, out result);
					return result;
				}
				return base.get_IntProp(hvo, tag);
			}

			private sealed class FakeMdc : LcmMetaDataCacheDecoratorBase
			{
				internal FakeMdc(IFwMetaDataCacheManaged metaDataCache) : base(metaDataCache)
				{
				}

				public override void AddVirtualProp(string bstrClass, string bstrField, int luFlid, int type)
				{
					throw new NotSupportedException();
				}

				internal const int kMadeUpFieldIdentifier = 5887;

				public override int GetFieldId2(int luClid, string bstrFieldName, bool fIncludeBaseClasses)
				{
					return bstrFieldName == "OccurrenceCount" ? kMadeUpFieldIdentifier : base.GetFieldId2(luClid, bstrFieldName, fIncludeBaseClasses);
				}
			}
		}
	}
}