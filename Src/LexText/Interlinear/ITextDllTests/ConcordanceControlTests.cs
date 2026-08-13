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
			var data = CreateCustomFieldData("the big bad wolf", "the nice big dog",
				"the small furry cat");
			var vwPattern = VwPatternClass.Create();
			vwPattern.Pattern = TsStringUtils.MakeString("big", Cache.DefaultAnalWs);
			var matcher = new RegExpMatcher(vwPattern);
			var result = ConcordanceControl.GetOccurrencesInCustomField(data.FieldId,
				data.Paragraphs, Cache.MainCacheAccessor, matcher);
			Assert.That(result, Has.Count.EqualTo(2));
			Assert.That(result.Any(pf => pf.Segment == data.Segments[0]));
			Assert.That(result.Any(pf => pf.Segment == data.Segments[1]));
		}

		[Test]
		public void ConcordanceRegex_IsCaseInsensitive()
		{
			var data = CreateCustomFieldData("the big bad wolf", "the nice BIG dog",
				"the small furry cat");
			var vwPattern = VwPatternClass.Create();
			vwPattern.Pattern = TsStringUtils.MakeString("BIG", Cache.DefaultAnalWs);
			vwPattern.MatchCase = false;
			var matcher = new RegExpMatcher(vwPattern);
			var result = ConcordanceControl.GetOccurrencesInCustomField(data.FieldId, data.Paragraphs,
				Cache.MainCacheAccessor, matcher);
			Assert.That(result, Has.Count.EqualTo(2));
			Assert.That(result.Select(pf => pf.Segment), Is.EquivalentTo(new[]
				{ data.Segments[0], data.Segments[1] }));
		}

		[Test]
		public void ConcordanceRegex_NfcPatternMatchesNfdFieldValueNotNfc()
		{
			var data = CreateCustomFieldData("caf\u00e9", "cafe\u0301", "cafe");
			var vwPattern = VwPatternClass.Create();
			vwPattern.Pattern = TsStringUtils.MakeString("\u00e9", Cache.DefaultAnalWs);
			var matcher = new RegExpMatcher(vwPattern);
			var result = ConcordanceControl.GetOccurrencesInCustomField(data.FieldId,
				data.Paragraphs, Cache.MainCacheAccessor, matcher);
			Assert.That(result.Select(pf => pf.Segment), Is.EquivalentTo(new[]
				{ data.Segments[1] }));
		}

		[Test]
		public void ConcordanceRegex_CaptureMatchesExpectedSegments()
		{
			var data = CreateCustomFieldData("the big bad wolf", "the nice big dog",
				"the small furry cat");
			var vwPattern = VwPatternClass.Create();
			vwPattern.Pattern = TsStringUtils.MakeString("(big)", Cache.DefaultAnalWs);
			var matcher = new RegExpMatcher(vwPattern);
			var result = ConcordanceControl.GetOccurrencesInCustomField(data.FieldId,
				data.Paragraphs, Cache.MainCacheAccessor, matcher);
			Assert.That(result.Select(pf => pf.Segment), Is.EquivalentTo(new[]
				{ data.Segments[0], data.Segments[1] }));
		}

		private sealed class CustomFieldData
		{
			public int FieldId { get; set; }
			public HashSet<IStTxtPara> Paragraphs { get; set; }
			public IList<ISegment> Segments { get; set; }
		}

		private CustomFieldData CreateCustomFieldData(params string[] values)
		{
			CustomFieldData data = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var mdc = Cache.ServiceLocator.GetInstance<IFwMetaDataCacheManaged>();
				mdc.AddCustomField("Segment", "test1", CellarPropertyType.String, 0,
					"just testing", Cache.DefaultAnalWs, Guid.Empty);
				var text1 = MakeText("AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA",
					"Sentence one. Sentence 2.");
				var para1 = text1.ContentsOA.ParagraphsOS[0] as IStTxtPara;
				var text2 = MakeText("AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAB",
					"Another Sentence one. Another Sentence 2.");
				var para2 = text2.ContentsOA.ParagraphsOS[0] as IStTxtPara;
				var segments = new List<ISegment>
				{
					para1.SegmentsOS[0], para2.SegmentsOS[1], para2.SegmentsOS[0]
				};
				var testFlid = mdc.GetFieldId("Segment", "test1", false);
				for (var i = 0; i < values.Length; i++)
					Cache.MainCacheAccessor.SetString(segments[i].Hvo, testFlid,
						TsStringUtils.MakeString(values[i], Cache.DefaultAnalWs));
				data = new CustomFieldData
				{
					FieldId = testFlid,
					Paragraphs = new HashSet<IStTxtPara> { para1, para2 },
					Segments = segments
				};
			});
			return data;
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
