using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.IText;

namespace ITextDllTests
{
	[TestFixture]
	public class ScriptureSegmentLabelTests : InMemoryFdoTestBase
	{
		private IText m_text;
		private StText m_stText;
		private ScrTxtPara m_para;
		ITsStrFactory m_tsf = TsStrFactoryClass.Create();
		int m_wsVern;
		private int ktagParaSegments;
		[SetUp]
		public override void Initialize()
		{
			CheckDisposed();
			base.Initialize();
			m_inMemoryCache.InitializeAnnotationDefs();
			InstallVirtuals(@"Language Explorer\Configuration\Words\AreaConfiguration.xml",
				new string[] { "SIL.FieldWorks.IText.ParagraphSegmentsVirtualHandler", "SIL.FieldWorks.IText.OccurrencesInTextsVirtualHandler" });
			m_text = new Text();
			Cache.LangProject.TextsOC.Add(m_text);
			m_para = new ScrTxtPara();
			m_stText = new StText();
			m_text.ContentsOA = m_stText;
			m_stText.ParagraphsOS.Append(m_para);
			ktagParaSegments = InterlinVc.ParaSegmentTag(Cache);
			m_wsVern = Cache.DefaultVernWs;
		}

		[Test]
		public void OneSegPerVerse()
		{
			string pc1 = "Das buch ist rot. ";
			string verse1 = "9";
			string pc2 = "Der Herr ist gross.";
			string verse2 = "10";
			string pc3 = "Ich spreche nicht viel Deutsch.";

			ITsStrBldr bldr = m_tsf.MakeString(pc1 + verse1 + pc2 + verse2 + pc3, m_wsVern).GetBldr();
			bldr.SetStrPropValue(pc1.Length, pc1.Length + verse1.Length, (int)FwTextPropType.ktptNamedStyle,
								 ScrStyleNames.VerseNumber);
			int ichV2 = pc1.Length + verse1.Length + pc2.Length;
			bldr.SetStrPropValue(ichV2, ichV2 + verse2.Length, (int)FwTextPropType.ktptNamedStyle,
								 ScrStyleNames.VerseNumber);
			m_para.Contents.UnderlyingTsString = bldr.GetString();
			ParagraphParser pp = new ParagraphParser(m_para);
			List<int> eosIndexes;
			List<int> segments = pp.CollectSegmentAnnotations(m_para.Contents.UnderlyingTsString, out eosIndexes);
			Cache.VwCacheDaAccessor.CacheVecProp(m_para.Hvo, ktagParaSegments, segments.ToArray(), segments.Count);
			Assert.AreEqual(5, segments.Count);
			Assert.AreEqual("", AnnotationRefHandler.VerseSegLabel(m_para, 0, ktagParaSegments));
			Assert.AreEqual("", AnnotationRefHandler.VerseSegLabel(m_para, 2, ktagParaSegments));
			Assert.AreEqual("", AnnotationRefHandler.VerseSegLabel(m_para, 4, ktagParaSegments));
		}

		[Test]
		public void TwoSegsPerVerse()
		{
			string pc1 = "Das buch ist rot. ";
			string pc2 = "Das Madchen ist shon.";
			string verse1 = "9";
			string pc3 = "Der Herr ist gross.";
			string pc4 = "Ich spreche nicht viel Deutsch.";
			string verse2 = "10";
			string pc5 = "Was is das?";
			string pc6 = "Wie gehts?";

			ITsStrBldr bldr = m_tsf.MakeString(pc1 + pc2 + verse1 + pc3 + pc4 + verse2 + pc5 + pc6, m_wsVern).GetBldr();
			bldr.SetStrPropValue(pc1.Length + pc2.Length, pc1.Length + pc2.Length + verse1.Length, (int)FwTextPropType.ktptNamedStyle,
								 ScrStyleNames.VerseNumber);
			int ichEndV1 = pc1.Length + pc2.Length + verse1.Length + pc3.Length + pc4.Length;
			bldr.SetStrPropValue(ichEndV1, ichEndV1 + verse2.Length, (int)FwTextPropType.ktptNamedStyle,
								 ScrStyleNames.VerseNumber);
			m_para.Contents.UnderlyingTsString = bldr.GetString();
			ParagraphParser pp = new ParagraphParser(m_para);
			List<int> eosIndexes;
			List<int> segments = pp.CollectSegmentAnnotations(m_para.Contents.UnderlyingTsString, out eosIndexes);
			Cache.VwCacheDaAccessor.CacheVecProp(m_para.Hvo, ktagParaSegments, segments.ToArray(), segments.Count);
			Assert.AreEqual(8, segments.Count);
			Assert.AreEqual("a", AnnotationRefHandler.VerseSegLabel(m_para, 0, ktagParaSegments));
			Assert.AreEqual("b", AnnotationRefHandler.VerseSegLabel(m_para, 1, ktagParaSegments));
			Assert.AreEqual("a", AnnotationRefHandler.VerseSegLabel(m_para, 3, ktagParaSegments));
			Assert.AreEqual("b", AnnotationRefHandler.VerseSegLabel(m_para, 4, ktagParaSegments));
			Assert.AreEqual("a", AnnotationRefHandler.VerseSegLabel(m_para, 6, ktagParaSegments));
			Assert.AreEqual("b", AnnotationRefHandler.VerseSegLabel(m_para, 7, ktagParaSegments));
		}

		[Test]
		public void MultipleParas()
		{
			ScrTxtPara paraPrev = new ScrTxtPara();
			m_stText.ParagraphsOS.InsertAt(paraPrev, 0);
			ScrTxtPara paraFirst = new ScrTxtPara();
			m_stText.ParagraphsOS.InsertAt(paraFirst, 0);
			ScrTxtPara paraNext = new ScrTxtPara();
			m_stText.ParagraphsOS.Append(paraNext);
			ScrTxtPara paraLast = new ScrTxtPara();
			m_stText.ParagraphsOS.Append(paraLast);

			string pc1 = "Das buch ist rot. ";
			string pc2 = "Das Madchen ist shon.";
			string verse1 = "9";
			string pc3 = "Der Herr ist gross.";
			string pc4 = "Ich spreche nicht viel Deutsch.";
			string verse2 = "10";
			string pc5 = "Was is das?";
			string pc6 = "Wie gehts?";

			ITsStrBldr bldr = m_tsf.MakeString(pc1 + pc2 + verse1 + pc3 + pc4 + verse2 + pc5, m_wsVern).GetBldr();
			bldr.SetStrPropValue(pc1.Length + pc2.Length, pc1.Length + pc2.Length + verse1.Length, (int)FwTextPropType.ktptNamedStyle,
								 ScrStyleNames.VerseNumber);
			int ichEndV1 = pc1.Length + pc2.Length + verse1.Length + pc3.Length + pc4.Length;
			bldr.SetStrPropValue(ichEndV1, ichEndV1 + verse2.Length, (int)FwTextPropType.ktptNamedStyle,
								 ScrStyleNames.VerseNumber);
			List<int> segments = GetSegments(bldr, m_para);

			string verse8 = "8";
			bldr = m_tsf.MakeString(verse8 + pc3 + pc4, m_wsVern).GetBldr();
			bldr.SetStrPropValue(0, verse8.Length, (int)FwTextPropType.ktptNamedStyle,
								 ScrStyleNames.VerseNumber);
			GetSegments(bldr, paraFirst);

			bldr = m_tsf.MakeString(pc1 + pc2, m_wsVern).GetBldr();
			GetSegments(bldr, paraPrev);

			string verse11 = "11";
			bldr = m_tsf.MakeString(pc3 + verse11 + pc4, m_wsVern).GetBldr();
			bldr.SetStrPropValue(pc3.Length, pc3.Length + verse11.Length, (int)FwTextPropType.ktptNamedStyle,
								 ScrStyleNames.VerseNumber);
			GetSegments(bldr, paraNext);

			string verse12 = "12";
			bldr = m_tsf.MakeString(verse12 + pc5 + pc6, m_wsVern).GetBldr();
			bldr.SetStrPropValue(0, verse12.Length, (int)FwTextPropType.ktptNamedStyle,
								 ScrStyleNames.VerseNumber);
			GetSegments(bldr, paraLast);
			Assert.AreEqual(7, segments.Count);
			Assert.AreEqual("a", AnnotationRefHandler.VerseSegLabel(paraFirst, 1, ktagParaSegments));
			Assert.AreEqual("b", AnnotationRefHandler.VerseSegLabel(paraFirst, 2, ktagParaSegments));
			Assert.AreEqual("c", AnnotationRefHandler.VerseSegLabel(paraPrev, 0, ktagParaSegments));
			Assert.AreEqual("d", AnnotationRefHandler.VerseSegLabel(paraPrev, 1, ktagParaSegments));
			Assert.AreEqual("e", AnnotationRefHandler.VerseSegLabel(m_para, 0, ktagParaSegments));
			Assert.AreEqual("f", AnnotationRefHandler.VerseSegLabel(m_para, 1, ktagParaSegments));
			Assert.AreEqual("a", AnnotationRefHandler.VerseSegLabel(m_para, 3, ktagParaSegments));
			Assert.AreEqual("b", AnnotationRefHandler.VerseSegLabel(m_para, 4, ktagParaSegments));
			Assert.AreEqual("a", AnnotationRefHandler.VerseSegLabel(m_para, 6, ktagParaSegments), "should have label because seg in following para");
			Assert.AreEqual("b", AnnotationRefHandler.VerseSegLabel(paraNext, 0, ktagParaSegments), "should have label due to previous para");
			Assert.AreEqual("", AnnotationRefHandler.VerseSegLabel(paraNext, 2, ktagParaSegments),
				"should have no label because next para starts with verse");
		}

		private List<int> GetSegments(ITsStrBldr bldr, ScrTxtPara para)
		{
			para.Contents.UnderlyingTsString = bldr.GetString();
			ParagraphParser pp = new ParagraphParser(para);
			List<int> eosIndexes;
			List<int> segments = pp.CollectSegmentAnnotations(para.Contents.UnderlyingTsString, out eosIndexes);
			Cache.VwCacheDaAccessor.CacheVecProp(para.Hvo, ktagParaSegments, segments.ToArray(), segments.Count);
			return segments;
		}
	}
}
