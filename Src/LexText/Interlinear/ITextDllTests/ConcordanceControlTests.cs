using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.Filters;
using SIL.LCModel;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.IText
{
	public class
		ConcordanceControlTests : MemoryOnlyBackendProviderReallyRestoredForEachTestTestBase
	{

		[Test]
		public void UpdateConcordanceForCustomField_FindsMatches()
		{
			// build pre-existing data
			var sl = Cache.ServiceLocator;
			var wsf = Cache.WritingSystemFactory;
			var mdc = Cache.ServiceLocator.GetInstance<IFwMetaDataCacheManaged>();

			IWfiWordform word = null;
			ITsString para1_1Contents = null;
			Guid segGuid = new Guid();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				mdc.AddCustomField("Segment", "test1", CellarPropertyType.String, 0,
					"just testing", Cache.DefaultAnalWs, Guid.Empty);
				var text1 = MakeText("AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA",
					"Sentence one. Sentence 2.");
				var sttext1 = text1.ContentsOA;
				var para1_1 = sttext1.ParagraphsOS[0] as IStTxtPara;
				var segment1_1_1 = para1_1.SegmentsOS[0];
				var testFlid = mdc.GetFieldId("Segment", "test1", false);
				Cache.MainCacheAccessor.SetString(segment1_1_1.Hvo, testFlid,
					TsStringUtils.MakeString("the big bad wolf", Cache.DefaultAnalWs));

				var text2 = MakeText("AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAB",
					"Another Sentence one. Another Sentence 2.");
				var sttext2 = text2.ContentsOA;
				var para2_1 = sttext2.ParagraphsOS[0] as IStTxtPara;
				var segment2_1_2 = para2_1.SegmentsOS[1];
				Cache.MainCacheAccessor.SetString(segment2_1_2.Hvo, testFlid,
					TsStringUtils.MakeString("the nice big dog", Cache.DefaultAnalWs));
				var segment2_1_1 = para2_1.SegmentsOS[0];
				Cache.MainCacheAccessor.SetString(segment2_1_1.Hvo, testFlid,
					TsStringUtils.MakeString("the small furry cat", Cache.DefaultAnalWs));

				var paragraphs = new HashSet<IStTxtPara>();
				paragraphs.Add(para1_1);
				paragraphs.Add(para2_1);
				var vwPattern = VwPatternClass.Create();
				vwPattern.Pattern = TsStringUtils.MakeString("big", Cache.DefaultAnalWs);
				var matcher = new RegExpMatcher(vwPattern);
				var result = ConcordanceControl.GetOccurrencesInCustomField(testFlid, paragraphs,
					Cache.MainCacheAccessor, matcher);
				Assert.That(result, Has.Count.EqualTo(2));
				Assert.That(result.Any(pf => pf.Segment == segment1_1_1));
				Assert.That(result.Any(pf => pf.Segment == segment2_1_2));
			});
		}

		private LCModel.IText MakeText(string guid, string para1Content)
		{
			var sl = Cache.ServiceLocator;
			var wsf = Cache.WritingSystemFactory;
			var text = sl.GetInstance<ITextFactory>().Create(Cache,
				new Guid(guid));
			var sttext1 = sl.GetInstance<IStTextFactory>().Create();
			text.ContentsOA = sttext1;
			var para1_1 = sl.GetInstance<IStTxtParaFactory>().Create();
			sttext1.ParagraphsOS.Add(para1_1);
			var para1_1Contents = TsStringUtils.MakeString(para1Content,
				wsf.get_Engine("en").Handle);
			para1_1.Contents = para1_1Contents;
			ParagraphParser.ParseText(sttext1);
			return text;
		}
	}
}
