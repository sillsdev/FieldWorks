using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.IText;

namespace ITextDllTests
{
	[TestFixture]
	public class FreeTransEditMonitorTests : InDatabaseFdoTestBase
	{
		private IScrBook m_book;
		private IScrSection m_section;
		private StText m_text;
		private IStTxtPara m_para;
		int m_wsVern;
		int m_wsTrans;
		ITsStrFactory m_tsf = TsStrFactoryClass.Create();
		private int m_hvoFtDefn; // annotation defn for free translation
		private int m_hvoSegDefn; // annotation defn for paragraph segment
		private int kflidFT;
		private int kflidSegments;

		[SetUp]
		public override void Initialize()
		{
			CheckDisposed();
			base.Initialize();
			InstallVirtuals(@"Language Explorer\Configuration\Words\AreaConfiguration.xml",
				new string[] { "SIL.FieldWorks.IText.ParagraphSegmentsVirtualHandler", "SIL.FieldWorks.IText.OccurrencesInTextsVirtualHandler" });
			m_wsVern = Cache.DefaultVernWs;
			m_wsTrans = Cache.DefaultAnalWs;
			m_book = new ScrBook();
			Cache.LangProject.TranslatedScriptureOA.ScriptureBooksOS.Append(m_book);
			m_section = new ScrSection();
			m_book.SectionsOS.Append(m_section);
			m_para = new StTxtPara();
			m_text = new StText();
			m_section.ContentOA = m_text;
			m_text.ParagraphsOS.Append(m_para);
			m_hvoSegDefn = CmAnnotationDefn.TextSegment(Cache).Hvo;
			m_hvoFtDefn = Cache.GetIdFromGuid(new Guid(LangProject.kguidAnnFreeTranslation));
			kflidFT = StTxtPara.SegmentFreeTranslationFlid(Cache);
			kflidSegments = StTxtPara.SegmentsFlid(Cache);
		}
		[Test]
		public void CreateAndUpdateSingleFt()
		{
			string paraContents = "Das buch ist rot";
			string trans = "The book is red";
			m_para.Contents.UnderlyingTsString = m_tsf.MakeString(paraContents, m_wsVern);
			ICmIndirectAnnotation ft = MakeFt(m_para, trans, 0, paraContents.Length);
			FreeTransEditMonitor monitor = new FreeTransEditMonitor(Cache, m_wsTrans); // BEFORE propChanged!
			Cache.PropChanged(ft.Hvo, (int)CmAnnotation.CmAnnotationTags.kflidComment, 0, 0, 0);
			monitor.LoseFocus();
			Assert.AreEqual(1, m_para.TranslationsOC.Count, "monitor should have made a CmTranslation");
			Assert.AreEqual(trans, m_para.TranslationsOC.ToList()[0].Translation.GetAlternative(m_wsTrans).Text);
			string trans2 = "The book is green";
			ft.Comment.SetAlternative(trans2, m_wsTrans);
			monitor.Dispose(); // should trigger update.
			Assert.AreEqual(1, m_para.TranslationsOC.Count, "monitor should not have made another translation");
			Assert.AreEqual(trans2, m_para.TranslationsOC.ToList()[0].Translation.GetAlternative(m_wsTrans).Text);
		}

		[Test]
		public void CreateAndUpdateOneOfTwoFts()
		{
			string pc1 = "Das buch ist rot. ";
			string pc2 = "Das Madchen ist shon.";
			string verse1 = "9";
			m_para.Contents.UnderlyingTsString = m_tsf.MakeString(pc1 + verse1 + pc2, m_wsVern);
			string trans1 = "The book is red.";
			string trans2 = "The girl is beautiful";
			ICmIndirectAnnotation ft = MakeFt(m_para, trans1, 0, pc1.Length);
			ICmBaseAnnotation verseSeg = MakeVerseSegment(m_para, pc1.Length, verse1.Length);
			ICmIndirectAnnotation ft2 = MakeFt(m_para, trans2, pc1.Length + verse1.Length, pc2.Length);
			FreeTransEditMonitor monitor = new FreeTransEditMonitor(Cache, m_wsTrans); // BEFORE propChanged!
			Cache.PropChanged(ft.Hvo, (int)CmAnnotation.CmAnnotationTags.kflidComment, 0, 0, 0);
			Cache.PropChanged(ft.Hvo, (int)CmAnnotation.CmAnnotationTags.kflidComment, 0, 0, 0);
			Assert.AreEqual(0, m_para.TranslationsOC.Count, "monitor should not have updated for change to same ft.");

			Cache.PropChanged(ft2.Hvo, (int)CmAnnotation.CmAnnotationTags.kflidComment, 0, 0, 0);

			Assert.AreEqual(1, m_para.TranslationsOC.Count, "monitor should have updated on changing another property");
			Assert.AreEqual(trans1 + " " + verse1 + trans2,
				m_para.TranslationsOC.ToList()[0].Translation.GetAlternative(m_wsTrans).Text,
				"translation should be correct after changing prop2");

			string trans2b = "The girl is pretty.";
			ft2.Comment.SetAlternative(trans2b, m_wsTrans); // should generate propChanged for same prop.
			Assert.AreEqual(trans1 + " " + verse1 + trans2,
				m_para.TranslationsOC.ToList()[0].Translation.GetAlternative(m_wsTrans).Text,
				"Another change to same prop should not produce yet another update");
			monitor.Dispose();
			Assert.AreEqual(trans1 + " " + verse1 + trans2b,
				m_para.TranslationsOC.ToList()[0].Translation.GetAlternative(m_wsTrans).Text,
				"Should get final update on Dispose");
		}

		[Test]
		public void NonScriptureText()
		{
			IText text = new Text();
			Cache.LangProject.TextsOC.Add(text);
			StText sttext = new StText();
			text.ContentsOA = sttext;
			m_para = new StTxtPara();
			sttext.ParagraphsOS.Append(m_para);
			string paraContents = "Das buch ist rot";
			string trans = "The book is red";
			m_para.Contents.UnderlyingTsString = m_tsf.MakeString(paraContents, m_wsVern);
			ICmIndirectAnnotation ft = MakeFt(m_para, trans, 0, paraContents.Length);
			FreeTransEditMonitor monitor = new FreeTransEditMonitor(Cache, m_wsTrans); // BEFORE propChanged!
			Cache.PropChanged(ft.Hvo, (int)CmAnnotation.CmAnnotationTags.kflidComment, 0, 0, 0);
			monitor.LoseFocus();
			Assert.AreEqual(0, m_para.TranslationsOC.Count, "monitor should not make CmTranslation for non-Scripture");
		}

		// Make a segment and its free translation. Append to or set up the segments property
		// of the para and set up the FT property of the segment.
		ICmIndirectAnnotation MakeFt(IStTxtPara para, string text, int beginOffset, int length)
		{
			ICmBaseAnnotation seg = MakeSegment(para, beginOffset, length);

			ICmIndirectAnnotation ft = CmIndirectAnnotation.CreateUnownedIndirectAnnotation(Cache);
			ft.AppliesToRS.Append(seg);
			ft.Comment.SetAlternative(text, m_wsTrans);
			ft.AnnotationTypeRAHvo = m_hvoFtDefn;

			// Backref links
			para.Cache.VwCacheDaAccessor.CacheObjProp(seg.Hvo, kflidFT, ft.Hvo);
			return ft;
		}

		ICmBaseAnnotation MakeVerseSegment(IStTxtPara para, int beginOffset, int length)
		{
			ICmBaseAnnotation seg = MakeSegment(para, beginOffset, length);
			ITsStrBldr bldr = para.Contents.UnderlyingTsString.GetBldr();
			bldr.SetStrPropValue(beginOffset, beginOffset + length, (int)FwTextPropType.ktptNamedStyle,
								 ScrStyleNames.VerseNumber);
			para.Contents.UnderlyingTsString = bldr.GetString();
			return seg;
		}

		private ICmBaseAnnotation MakeSegment(IStTxtPara para, int beginOffset, int length)
		{
			ICmBaseAnnotation seg = CmBaseAnnotation.CreateUnownedCba(Cache);
			seg.BeginObjectRA = para;
			seg.BeginOffset = beginOffset;
			seg.EndOffset = beginOffset + length;
			seg.AnnotationTypeRAHvo = m_hvoSegDefn;
			ISilDataAccess sda = para.Cache.MainCacheAccessor;
			int[] segments;
			if (sda.get_IsPropInCache(para.Hvo, kflidSegments, (int)CellarModuleDefns.kcptReferenceSequence, 0))
			{
				int[] segmentsT = Cache.GetVectorProperty(para.Hvo, kflidSegments, true);
				segments = new int[segmentsT.Length];
				Array.Copy(segmentsT, segments, segmentsT.Length);
				segments[segments.Length - 1] = seg.Hvo;
			}
			else
			{
				segments = new int[] { seg.Hvo };
			}
			para.Cache.VwCacheDaAccessor.CacheVecProp(seg.Hvo, kflidSegments, segments, segments.Length);
			return seg;
		}
	}
}
