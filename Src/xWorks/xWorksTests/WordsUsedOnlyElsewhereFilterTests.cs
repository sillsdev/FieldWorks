using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.Filters;

namespace SIL.FieldWorks.XWorks
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
			var filter = new WordsUsedOnlyElsewhereFilter();
			filter.Init(Cache, null);
			var sda = new FakeDecorator((ISilDataAccessManaged)Cache.DomainDataByFlid);
			filter.DataAccess = sda;
			UndoableUnitOfWorkHelper.Do("undo", "redo", m_actionHandler,
				() =>
					{
						var wfTry = MakeWordform("try");
						ISegment seg1;
						var text1 = MakeText("try it out", out seg1);
						seg1.AnalysesRS.Add(wfTry);
						Assert.That(wfTry.FullConcordanceCount, Is.EqualTo(1));
						ManyOnePathSortItem itemTry = new ManyOnePathSortItem(wfTry);
						// Here the global count is non-zero but the corpus count is zero.
						Assert.That(filter.Accept(itemTry), Is.False, "should not accept an item which occurs elsewhere but not in corpus");
						sda.HvoToOccurrenceCount[wfTry.Hvo] = 1;
						Assert.That(filter.Accept(itemTry), Is.True, "should accept an item in an included text");
						sda.HvoToOccurrenceCount[wfTry.Hvo] = 5;
						Assert.That(filter.Accept(itemTry), Is.True, "should accept an item in an included text, even if there are other occurrences");
						var wfNowhere = MakeWordform("nowhere");
						ManyOnePathSortItem itemNowhere = new ManyOnePathSortItem(wfNowhere);
						Assert.That(filter.Accept(itemNowhere), Is.True,"should accept an item that occurs nowhere at all.");
					});
		}

		void SetVernAlternative(IMultiUnicode mu, string content)
		{
			mu.VernacularDefaultWritingSystem = MakeVernTss(content);
		}

		private ITsString MakeVernTss(string content)
		{
			return Cache.TsStrFactory.MakeString(content, Cache.DefaultVernWs);
		}

		IWfiWordform MakeWordform(string form)
		{
			var wf = Cache.ServiceLocator.GetInstance<IWfiWordformFactory>().Create();
			SetVernAlternative(wf.Form, form);
			return wf;
		}

		IText MakeText(string content, out ISegment seg)
		{
			var text = Cache.ServiceLocator.GetInstance<ITextFactory>().Create();
			//Cache.LangProject.TextsOC.Add(text);
			var stText = Cache.ServiceLocator.GetInstance<IStTextFactory>().Create();
			text.ContentsOA = stText;
			var para = Cache.ServiceLocator.GetInstance<IStTxtParaFactory>().Create();
			stText.ParagraphsOS.Add(para);
			para.Contents = MakeVernTss(content);
			seg = Cache.ServiceLocator.GetInstance<ISegmentFactory>().Create();
			para.SegmentsOS.Add(seg);
			return text;
		}
	}

	/// <summary>
	/// a Decorator which implements OccurrenceCount to return whatever we want (0 by default).
	/// </summary>
	class FakeDecorator : DomainDataByFlidDecoratorBase
	{
		public FakeDecorator(ISilDataAccessManaged domainDataByFlid) : base(domainDataByFlid)
		{
			m_mdc = new FakeMdc((IFwMetaDataCacheManaged)domainDataByFlid.MetaDataCache);
		}
		public Dictionary<int, int> HvoToOccurrenceCount = new Dictionary<int, int>();

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
			if (tag == FakeMdc.kfakeFlid)
			{
				int result;
				HvoToOccurrenceCount.TryGetValue(hvo, out result);
				return result;
			}
			return base.get_IntProp(hvo, tag);
		}
	}

	class FakeMdc : FdoMetaDataCacheDecoratorBase
	{
		public FakeMdc(IFwMetaDataCacheManaged metaDataCache) : base(metaDataCache)
		{
		}

		public override void AddVirtualProp(string bstrClass, string bstrField, int luFlid, int type)
		{
			throw new NotImplementedException();
		}

		internal const int kfakeFlid = 5887;
		public override int GetFieldId2(int luClid, string bstrFieldName, bool fIncludeBaseClasses)
		{
			if (bstrFieldName == "OccurrenceCount")
				return kfakeFlid;
			return base.GetFieldId2(luClid, bstrFieldName, fIncludeBaseClasses);
		}
	}
}
