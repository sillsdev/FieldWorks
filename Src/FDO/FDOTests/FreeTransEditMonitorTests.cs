// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.DomainImpl;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.Common.ScriptureUtils;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// <summary>
	/// Tests for FreeTranslation editing logic.
	/// </summary>
	[TestFixture]
	public class FreeTransEditTests : ScrInMemoryFdoTestBase
	{
		private IScrBook m_book;
		private IScrSection m_section;
		private IStText m_text;
		private IStTxtPara m_para;
		int m_wsVern;
		int m_wsTrans;

		/// <summary>
		///
		/// </summary>
		public override void TestSetup()
		{
			base.TestSetup();

			m_wsVern = Cache.DefaultVernWs;
			m_wsTrans = Cache.DefaultAnalWs;
			m_book = Cache.ServiceLocator.GetInstance<IScrBookFactory>().Create(1);
			m_section = Cache.ServiceLocator.GetInstance<IScrSectionFactory>().Create();
			m_book.SectionsOS.Add(m_section);
			m_section.ContentOA = m_text = Cache.ServiceLocator.GetInstance<IStTextFactory>().Create(); ;
			m_para = m_text.AddNewTextPara(ScrStyleNames.NormalParagraph);
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void CreateAndUpdateSingleFt()
		{
			string paraContents = "Das buch ist rot";
			string trans = "The book is red";
			m_para.Contents = Cache.TsStrFactory.MakeString(paraContents, m_wsVern);
			var seg = SetFt(m_para, trans, 0);
			Assert.AreEqual(1, m_para.TranslationsOC.Count, "should have made a CmTranslation");
			Assert.AreEqual(trans, m_para.TranslationsOC.ToArray()[0].Translation.get_String(m_wsTrans).Text);
			string trans2 = "The book is green";
			seg.FreeTranslation.set_String(m_wsTrans, trans2);
			Assert.AreEqual(1, m_para.TranslationsOC.Count, "should not have made another translation");
			Assert.AreEqual(trans2, m_para.TranslationsOC.ToArray()[0].Translation.get_String(m_wsTrans).Text);
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void CreateAndUpdateOneOfTwoFts()
		{
			m_para.SegmentsOS.Clear();
			string pc1 = "Das buch ist rot. ";
			string pc2 = "Das Madchen ist shon.";
			string verse1 = "9";
			m_para.Contents = Cache.TsStrFactory.MakeString(pc1 + verse1 + pc2, m_wsVern);
			string trans1 = "The book is red.";
			string trans2 = "The girl is beautiful";
			BackTranslationAndFreeTranslationUpdateHelper.Do(m_para, () => SetFt(m_para, trans1, 0));
			Assert.AreEqual(1, m_para.TranslationsOC.Count, "should not have updated for change to same ft.");
			BackTranslationAndFreeTranslationUpdateHelper.Do(
				m_para, () => MakeVerseSegment(m_para, pc1.Length, verse1.Length));
			var seg2 = SetFt(m_para, trans2, 2);
			Assert.AreEqual(1, m_para.TranslationsOC.Count, "should have updated on changing another property");
			Assert.AreEqual(trans1 + " " + verse1 + trans2,
							m_para.TranslationsOC.ToArray()[0].Translation.get_String(m_wsTrans).Text,
							"translation should be correct after changing prop2");
			string trans2b = "The girl is pretty.";
			seg2.FreeTranslation.set_String(m_wsTrans, trans2b); // should generate propChanged for same prop.
			Assert.AreEqual(trans1 + " " + verse1 + trans2b,
							m_para.TranslationsOC.ToArray()[0].Translation.get_String(m_wsTrans).Text,
							"translation should be correct after changing prop2");
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void UpdateFtAddsSpacesBetweenLabels()
		{
			m_para.SegmentsOS.Clear();
			string pc1 = "Das buch ist rot. ";
			string pc2 = "Das Madchen ist shon.";
			string verse1 = "9";
			string verse2 = "10";
			string verse3 = "11";
			string trans1 = "The book is red.";
			AddVerse((IScrTxtPara)m_para, 0, 9, pc1);
			AddVerse((IScrTxtPara)m_para, 0, 10, pc2);
			AddVerse((IScrTxtPara)m_para, 0, 11, string.Empty);
			ISegment seg1 = SetFt(m_para, trans1, 1);
			Assert.AreEqual(verse1 + trans1 + " " + verse2 + " " + verse3,
				m_para.TranslationsOC.ToArray()[0].Translation.get_String(m_wsTrans).Text,
				"translation should be correct after changing prop2");
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void NonScriptureText()
		{
			IText text = Cache.ServiceLocator.GetInstance<ITextFactory>().Create();
			//Cache.LangProject.TextsOC.Add(text);
			IStText sttext = Cache.ServiceLocator.GetInstance<IStTextFactory>().Create();
			text.ContentsOA = sttext;
			m_para = sttext.AddNewTextPara(null);
			string paraContents = "Das buch ist rot";
			string trans = "The book is red";
			m_para.Contents = Cache.TsStrFactory.MakeString(paraContents, m_wsVern);
			SetFt(m_para, trans, 0);
			Assert.AreEqual(0, m_para.TranslationsOC.Count, "should not make CmTranslation for non-Scripture");
		}

		// Set the free translationof the indicated segment.
		ISegment SetFt(IStTxtPara para, string text, int segIndex)
		{
			ISegment seg = para.SegmentsOS[segIndex];
			seg.FreeTranslation.set_String(m_wsTrans, text);
			return seg;
		}

		ISegment MakeVerseSegment(IStTxtPara para, int beginOffset, int length)
		{
			var seg = ((SegmentFactory)m_para.Services.GetInstance<ISegmentFactory>()).Create(para, beginOffset);
			ITsStrBldr bldr = para.Contents.GetBldr();
			bldr.SetStrPropValue(beginOffset, beginOffset + length, (int)FwTextPropType.ktptNamedStyle,
								 ScrStyleNames.VerseNumber);
			para.Contents = bldr.GetString();
			return seg;
		}
	}
}