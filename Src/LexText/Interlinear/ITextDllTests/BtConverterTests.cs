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
	/// <summary>
	/// Test the Back translation conversion class.
	/// Unfortunately it needs to use StTxtPara.LoadSegmentFreeTranslations, which requires a real database.
	/// </summary>
	[TestFixture]
	public class BtConverterTests : InDatabaseFdoTestBase
	{
		private IText m_text;
		private IStTxtPara m_para;
		private ICmTranslation m_trans;
		ITsStrFactory m_tsf = TsStrFactoryClass.Create();
		private int kflidFT;
		private int kflidSegments;
		private ICmPossibility m_btPoss;
		int m_wsVern;
		int m_wsTrans;
		private bool m_fWasUseScriptDigits;

		[SetUp]
		public override void Initialize()
		{
			CheckDisposed();
			base.Initialize();
			//m_inMemoryCache.InitializeAnnotationDefs();
			InstallVirtuals(@"Language Explorer\Configuration\Words\AreaConfiguration.xml",
				new string[] { "SIL.FieldWorks.IText.ParagraphSegmentsVirtualHandler", "SIL.FieldWorks.IText.OccurrencesInTextsVirtualHandler" });
			m_wsVern = Cache.DefaultVernWs;
			m_wsTrans = Cache.DefaultAnalWs;
			m_text = new Text();
			Cache.LangProject.TextsOC.Add(m_text);
			m_para = new StTxtPara();
			StText text = new StText();
			m_text.ContentsOA = text;
			text.ParagraphsOS.Append(m_para);
			m_trans = new CmTranslation();
			m_para.TranslationsOC.Add(m_trans);
			kflidFT = StTxtPara.SegmentFreeTranslationFlid(Cache);
			kflidSegments = StTxtPara.SegmentsFlid(Cache);
			m_btPoss = Cache.LangProject.TranslationTagsOA.LookupPossibilityByGuid(
				LangProject.kguidTranBackTranslation);
			m_trans.TypeRA = m_btPoss;
			m_fWasUseScriptDigits = Cache.LangProject.TranslatedScriptureOA.UseScriptDigits;
			Cache.LangProject.TranslatedScriptureOA.UseScriptDigits = false;
			// do we need to set status?
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Undo everything possible in the FDO cache
		/// </summary>
		/// <remarks>This method is called after each test</remarks>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public override void Exit()
		{
			CheckDisposed();
			Cache.LangProject.TranslatedScriptureOA.UseScriptDigits = m_fWasUseScriptDigits;
			base.Exit();
		}
		/// <summary>
		/// The simplest possible case, paragraph has single segment, copy whole BT across.
		/// </summary>
		[Test]
		public void SingleSegment()
		{
			string paraContents = "Das buch ist rot";
			string trans = "The book is red";
			m_para.Contents.UnderlyingTsString = m_tsf.MakeString(paraContents, m_wsVern);
			m_trans.Translation.SetAlternative(trans, m_wsTrans);
			BtConverter converter = new BtConverter(m_para);
			converter.ConvertCmTransToInterlin(m_wsTrans);
			int cseg = Cache.GetVectorSize(m_para.Hvo, kflidSegments);
			Assert.AreEqual(1, cseg, "Para with no EOS or verse should have one segment");
			VerifyFt(0, trans, "whole translation should be transferred to single ft.");
		}

		[Test]
		public void EmptyPara()
		{
			string paraContents = "";
			string trans = "";
			m_para.Contents.UnderlyingTsString = m_tsf.MakeString(paraContents, m_wsVern);
			m_trans.Translation.SetAlternative(trans, m_wsTrans);
			BtConverter converter = new BtConverter(m_para);
			converter.ConvertCmTransToInterlin(m_wsTrans);
			int cseg = Cache.GetVectorSize(m_para.Hvo, kflidSegments);
			Assert.AreEqual(0, cseg, "Empty para should have no segments");
		}

		[Test]
		public void MatchingVerses()
		{
			// Make a paragraph where both para contents and translation have two segments separated by verse number.
			string paraContents1 = "Das buch ist rot. ";
			string verse = "12";
			string paraContents2 = "Das Madchen ist shon.";
			ITsStrBldr bldr = m_tsf.MakeString(paraContents1 + verse + paraContents2, m_wsVern).GetBldr();
			bldr.SetStrPropValue(paraContents1.Length, paraContents1.Length + verse.Length, (int)FwTextPropType.ktptNamedStyle,
								 ScrStyleNames.VerseNumber);
			m_para.Contents.UnderlyingTsString = bldr.GetString();
			string trans1 = "The book is red.";
			string trans2 = "The girl is beautiful";
			bldr = m_tsf.MakeString(trans1 + verse + trans2, m_wsTrans).GetBldr();
			bldr.SetStrPropValue(trans1.Length, trans1.Length + verse.Length, (int)FwTextPropType.ktptNamedStyle,
								 ScrStyleNames.VerseNumber);
			m_trans.Translation.SetAlternative(bldr.GetString(), m_wsTrans);

			BtConverter converter = new BtConverter(m_para);
			converter.ConvertCmTransToInterlin(m_wsTrans);
			int cseg = Cache.GetVectorSize(m_para.Hvo, kflidSegments);
			Assert.AreEqual(3, cseg, "parsing para should produce three segs.");
			VerifyFt(0, trans1, "first seg");
			// Currently we don't care whether there is an FT for verse segments...maybe one day we will.
			//VerifyFt(1, null, "no second FT");
			VerifyFt(2, trans2, "last seg");
		}

		void VerifyLabel(int iseg, string label)
		{
			int hvoSeg = Cache.GetVectorItem(m_para.Hvo, kflidSegments, iseg);
			CmBaseAnnotation seg = CmObject.CreateFromDBObject(Cache, hvoSeg, false) as CmBaseAnnotation;
			Assert.IsTrue(SegmentBreaker.HasLabelText(m_para.Contents.UnderlyingTsString, seg.BeginOffset, seg.EndOffset), label);
		}

		/// <summary>
		/// Verify that the segment indicated by hvoSeg has a free translation with Comment alternative m_wsTrans
		/// set to contents. If contents is null, it is acceptable to have no FT at all.
		/// </summary>
		void VerifyFt(int iseg, string contents, string label)
		{
			int hvoSeg = Cache.GetVectorItem(m_para.Hvo, kflidSegments, iseg);
			int hvoFTrans = Cache.GetObjProperty(hvoSeg, kflidFT);
			if (contents == null && hvoFTrans == 0)
				return;
			CmIndirectAnnotation ft = CmObject.CreateFromDBObject(Cache, hvoFTrans) as CmIndirectAnnotation;
			Assert.AreEqual(contents, ft.Comment.GetAlternative(m_wsTrans).Text, label + " - comment");
			Assert.AreEqual(hvoSeg, ft.AppliesToRS[0].Hvo, label + " - AppliesTo set correctly");
		}

		[Test]
		public void ExtraSegsInPara()
		{
			// Make a paragraph where para contents has three verses each with two segs, BT has only one seg per verse.
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
			string trans1 = "The book is red.";
			//string trans2 = "The girl is beautiful";
			string trans3 = "The man is big.";
			// string trans4 = "I don't speak much German.";
			string trans5 = "What is that?";
			// string trans6 = "How's it going?";
			bldr = m_tsf.MakeString(trans1 + verse1 + trans3 + verse2 + trans5, m_wsTrans).GetBldr();
			bldr.SetStrPropValue(trans1.Length, trans1.Length + verse1.Length, (int)FwTextPropType.ktptNamedStyle,
								 ScrStyleNames.VerseNumber);
			int ichEndTransV1 = trans1.Length + verse1.Length + trans3.Length;
			bldr.SetStrPropValue(ichEndTransV1, ichEndTransV1 + verse2.Length, (int)FwTextPropType.ktptNamedStyle,
				ScrStyleNames.VerseNumber);
			m_trans.Translation.SetAlternative(bldr.GetString(), m_wsTrans);

			BtConverter converter = new BtConverter(m_para);
			converter.ConvertCmTransToInterlin(m_wsTrans);
			int cseg = Cache.GetVectorSize(m_para.Hvo, kflidSegments);
			Assert.AreEqual(8, cseg, "parsing para should produce eight segs.");
			VerifyFt(0, trans1, "first seg");
			VerifyFt(1, null, "verse number sync should keep second BT empty");
			//VerifyFt(2, null, "no trans of verse number");
			VerifyFt(3, trans3, "3rd seg should get second BT");
			VerifyFt(4, null, "verse number sync should keep fourth BT empty");
			//VerifyFt(5, null, "no trans of verse number");
			VerifyFt(6, trans5, "5th seg should get third BT");
			VerifyFt(7, null, "last seg left with empty BT");
		}
		[Test]
		public void ExtraSegsInBtVerseNotMatching()
		{
			// Like ExtraSegsInBt, but second verse number does not match in BT.
			string pc1 = "Das buch ist rot. ";
			string pc2 = "Das Madchen ist shon.";
			string verse1 = "9";
			string pc3 = "Der Herr ist gross.";
			string pc4 = "Ich spreche nicht viel Deutsch.";
			string verse2 = "10";
			string pc5 = "Was is das?";
			string pc6 = "Wie gehts?";
			string verse3 = "11";

			ITsStrBldr bldr = m_tsf.MakeString(pc1 + pc2 + verse1 + pc3 + pc4 + verse2 + pc5 + pc6, m_wsVern).GetBldr();
			bldr.SetStrPropValue(pc1.Length + pc2.Length, pc1.Length + pc2.Length + verse1.Length, (int)FwTextPropType.ktptNamedStyle,
								 ScrStyleNames.VerseNumber);
			int ichEndV1 = pc1.Length + pc2.Length + verse1.Length + pc3.Length + pc4.Length;
			bldr.SetStrPropValue(ichEndV1, ichEndV1 + verse2.Length, (int)FwTextPropType.ktptNamedStyle,
								 ScrStyleNames.VerseNumber);
			m_para.Contents.UnderlyingTsString = bldr.GetString();
			string trans1 = "The book is red.";
			//string trans2 = "The girl is beautiful";
			string trans3 = "The man is big.";
			// string trans4 = "I don't speak much German.";
			string trans5 = "What is that?";
			// string trans6 = "How's it going?";
			bldr = m_tsf.MakeString(trans1 + verse1 + trans3 + verse3 + trans5, m_wsTrans).GetBldr();
			bldr.SetStrPropValue(trans1.Length, trans1.Length + verse1.Length, (int)FwTextPropType.ktptNamedStyle,
								 ScrStyleNames.VerseNumber);
			int ichEndTransV1 = trans1.Length + verse1.Length + trans3.Length;
			bldr.SetStrPropValue(ichEndTransV1, ichEndTransV1 + verse3.Length, (int)FwTextPropType.ktptNamedStyle,
				ScrStyleNames.VerseNumber);
			m_trans.Translation.SetAlternative(bldr.GetString(), m_wsTrans);

			BtConverter converter = new BtConverter(m_para);
			converter.ConvertCmTransToInterlin(m_wsTrans);
			int cseg = Cache.GetVectorSize(m_para.Hvo, kflidSegments);
			Assert.AreEqual(8, cseg, "parsing para should produce eight segs.");
			VerifyFt(0, trans1, "first seg");
			VerifyFt(1, null, "verse number sync should keep second BT empty");
			//VerifyFt(2, null, "no trans of verse number");
			VerifyFt(3, trans3, "3rd seg should get second BT");
			VerifyFt(4, trans5, "2nd verse does not match, keep assigning sequentially");
			//VerifyFt(5, null, "no trans of verse number");
			VerifyFt(6, null, "5th seg has no BTs left to assign");
			VerifyFt(7, null, "last seg left with empty BT");
		}

		[Test]
		public void ExtraSegsInParaVerseNotMatching()
		{
			// Make a paragraph where para contents has three verses with one seg each, BT has two segs per verse.
			string pc1 = "Das buch ist rot. ";
			//string pc2 = "Das Madchen ist shon.";
			string verse1 = "9";
			string pc3 = "Der Herr ist gross.";
			//string pc4 = "Ich spreche nicht viel Deutsch.";
			string verse2 = "10";
			string pc5 = "Was is das?";
			//string pc6 = "Wie gehts?";
			string verse3 = "11";

			ITsStrBldr bldr = m_tsf.MakeString(pc1 + verse1 + pc3 + verse2 + pc5, m_wsVern).GetBldr();
			bldr.SetStrPropValue(pc1.Length, pc1.Length + verse1.Length, (int)FwTextPropType.ktptNamedStyle,
								 ScrStyleNames.VerseNumber);
			int ichEndV1 = pc1.Length + verse1.Length + pc3.Length;
			bldr.SetStrPropValue(ichEndV1, ichEndV1 + verse2.Length, (int)FwTextPropType.ktptNamedStyle,
								 ScrStyleNames.VerseNumber);
			m_para.Contents.UnderlyingTsString = bldr.GetString();
			string trans1 = "The book is red.";
			string trans2 = "The girl is beautiful";
			string trans3 = "The man is big.";
			string trans4 = "I don't speak much German.";
			string trans5 = "What is that?";
			string trans6 = "How's it going?";
			bldr = m_tsf.MakeString(trans1 + trans2 + verse1 + trans3 + trans4 + verse3 + trans5 + trans6, m_wsTrans).GetBldr();
			bldr.SetStrPropValue(trans1.Length + trans2.Length, trans1.Length + trans2.Length + verse1.Length,
				(int)FwTextPropType.ktptNamedStyle, ScrStyleNames.VerseNumber);
			int ichEndTransV1 = trans1.Length + trans2.Length + verse1.Length + trans3.Length + trans4.Length;
			bldr.SetStrPropValue(ichEndTransV1, ichEndTransV1 + verse3.Length, (int)FwTextPropType.ktptNamedStyle,
				ScrStyleNames.VerseNumber);
			m_trans.Translation.SetAlternative(bldr.GetString(), m_wsTrans);

			BtConverter converter = new BtConverter(m_para);
			converter.ConvertCmTransToInterlin(m_wsTrans);
			int cseg = Cache.GetVectorSize(m_para.Hvo, kflidSegments);
			Assert.AreEqual(5, cseg, "parsing para should produce five segs.");
			VerifyFt(0, trans1 + " " + trans2, "first seg should have first TWO translations");
			//VerifyFt(1, null, "no trans of verse number");
			VerifyFt(2, trans3, "3rd seg should get next BTs");
			//VerifyFt(3, null, "no trans of verse number");
			VerifyFt(4, trans4 + " " + trans5 + " " + trans6, "5th seg should get last three BTs");
		}
		[Test]
		public void ExtraSegsInBt()
		{
			// Make a paragraph where para contents has three verses with one seg each, BT has two segs per verse.
			string pc1 = "Das buch ist rot. ";
			//string pc2 = "Das Madchen ist shon.";
			string verse1 = "9";
			string pc3 = "Der Herr ist gross.";
			//string pc4 = "Ich spreche nicht viel Deutsch.";
			string verse2 = "10";
			string pc5 = "Was is das?";
			//string pc6 = "Wie gehts?";

			ITsStrBldr bldr = m_tsf.MakeString(pc1 + verse1 + pc3 + verse2 + pc5, m_wsVern).GetBldr();
			bldr.SetStrPropValue(pc1.Length, pc1.Length + verse1.Length, (int)FwTextPropType.ktptNamedStyle,
								 ScrStyleNames.VerseNumber);
			int ichEndV1 = pc1.Length + verse1.Length + pc3.Length;
			bldr.SetStrPropValue(ichEndV1, ichEndV1 + verse2.Length, (int)FwTextPropType.ktptNamedStyle,
								 ScrStyleNames.VerseNumber);
			m_para.Contents.UnderlyingTsString = bldr.GetString();
			string trans1 = "The book is red.";
			string trans2 = "The girl is beautiful";
			string trans3 = "The main is big.";
			string trans4 = "I don't speak much German.";
			string trans5 = "What is that?";
			string trans6 = "How's it going?";
			bldr = m_tsf.MakeString(trans1 + trans2 + verse1 + trans3 + trans4 + verse2 + trans5 + trans6, m_wsTrans).GetBldr();
			bldr.SetStrPropValue(trans1.Length + trans2.Length, trans1.Length + trans2.Length + verse1.Length,
				(int)FwTextPropType.ktptNamedStyle, ScrStyleNames.VerseNumber);
			int ichEndTransV1 = trans1.Length + trans2.Length + verse1.Length + trans3.Length + trans4.Length;
			bldr.SetStrPropValue(ichEndTransV1, ichEndTransV1 + verse2.Length, (int)FwTextPropType.ktptNamedStyle,
				ScrStyleNames.VerseNumber);
			m_trans.Translation.SetAlternative(bldr.GetString(), m_wsTrans);

			BtConverter converter = new BtConverter(m_para);
			converter.ConvertCmTransToInterlin(m_wsTrans);
			int cseg = Cache.GetVectorSize(m_para.Hvo, kflidSegments);
			Assert.AreEqual(5, cseg, "parsing para should produce five segs.");
			VerifyFt(0, trans1 + " " + trans2, "first seg should have first TWO translations");
			//VerifyFt(1, null, "no trans of verse number");
			VerifyFt(2, trans3 + " " + trans4, "3rd seg should get next TWO BTs");
			//VerifyFt(3, null, "no trans of verse number");
			VerifyFt(4, trans5 + " " + trans6, "5th seg should get fifth and sixth BTs");
		}

		[Test]
		public void ExtraSegsInBtWithDiffNumberSystem()
		{
			// Make a paragraph where para contents has three verses with one seg each, BT has two segs per verse.
			string pc1 = "Das buch ist rot. ";
			//string pc2 = "Das Madchen ist shon.";
			string verse1 = "9";
			string pc3 = "Der Herr ist gross.";
			//string pc4 = "Ich spreche nicht viel Deutsch.";
			string verse2 = "10";
			string pc5 = "Was is das?";
			//string pc6 = "Wie gehts?";
			Cache.LangProject.TranslatedScriptureOA.UseScriptDigits = true;
			int oldDigitZero = Cache.LangProject.TranslatedScriptureOA.ScriptDigitZero;
			try
			{
				int charOffset = 2534 - '0';
				Cache.LangProject.TranslatedScriptureOA.ScriptDigitZero = 2534; // Bengali
				StringBuilder bldrV = new StringBuilder();
				bldrV.Append((char)('9' + charOffset));
				string verse1V = bldrV.ToString();
				bldrV = new StringBuilder();
				bldrV.Append((char)('1' + charOffset));
				bldrV.Append((char)('0' + charOffset));
				string verse2V = bldrV.ToString();
				ITsStrBldr bldr = m_tsf.MakeString(pc1 + verse1V + pc3 + verse2V + pc5, m_wsVern).GetBldr();
				bldr.SetStrPropValue(pc1.Length, pc1.Length + verse1V.Length, (int) FwTextPropType.ktptNamedStyle,
									 ScrStyleNames.VerseNumber);
				int ichEndV1 = pc1.Length + verse1V.Length + pc3.Length;
				bldr.SetStrPropValue(ichEndV1, ichEndV1 + verse2V.Length, (int) FwTextPropType.ktptNamedStyle,
									 ScrStyleNames.VerseNumber);
				m_para.Contents.UnderlyingTsString = bldr.GetString();
				string trans1 = "The book is red.";
				string trans2 = "The girl is beautiful";
				string trans3 = "The main is big.";
				string trans4 = "I don't speak much German.";
				string trans5 = "What is that?";
				string trans6 = "How's it going?";
				bldr =
					m_tsf.MakeString(trans1 + trans2 + verse1 + trans3 + trans4 + verse2 + trans5 + trans6, m_wsTrans).
						GetBldr();
				bldr.SetStrPropValue(trans1.Length + trans2.Length, trans1.Length + trans2.Length + verse1.Length,
									 (int) FwTextPropType.ktptNamedStyle, ScrStyleNames.VerseNumber);
				int ichEndTransV1 = trans1.Length + trans2.Length + verse1.Length + trans3.Length + trans4.Length;
				bldr.SetStrPropValue(ichEndTransV1, ichEndTransV1 + verse2.Length, (int) FwTextPropType.ktptNamedStyle,
									 ScrStyleNames.VerseNumber);
				m_trans.Translation.SetAlternative(bldr.GetString(), m_wsTrans);

				BtConverter converter = new BtConverter(m_para);
				converter.ConvertCmTransToInterlin(m_wsTrans);
				int cseg = Cache.GetVectorSize(m_para.Hvo, kflidSegments);
				Assert.AreEqual(5, cseg, "parsing para should produce five segs.");
				VerifyFt(0, trans1 + " " + trans2, "first seg should have first TWO translations");
				//VerifyFt(1, null, "no trans of verse number");
				VerifyFt(2, trans3 + " " + trans4, "3rd seg should get next TWO BTs");
				//VerifyFt(3, null, "no trans of verse number");
				VerifyFt(4, trans5 + " " + trans6, "5th seg should get fifth and sixth BTs");

			}
			finally
			{
				Cache.LangProject.TranslatedScriptureOA.UseScriptDigits = false;
				Cache.LangProject.TranslatedScriptureOA.ScriptDigitZero = oldDigitZero;
			}
		}

		[Test]
		public void VersesOutOfOrder()
		{
			// Here for some reason the BT is out of order: has verse 10 before verse 9. The program still assigns
			// sequentially the segments that follow matching verse numbers.
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
			string trans1 = "The book is red.";
			string trans2 = "The girl is beautiful";
			string trans3 = "The man is big.";
			string trans4 = "I don't speak much German.";
			string trans5 = "What is that?";
			string trans6 = "How's it going?";
			bldr = m_tsf.MakeString(trans1 + trans2 + verse2 + trans5 + trans6 + verse1 + trans3 + trans4, m_wsTrans).GetBldr();
			bldr.SetStrPropValue(trans1.Length + trans2.Length, trans1.Length + trans2.Length + verse2.Length,
				(int)FwTextPropType.ktptNamedStyle, ScrStyleNames.VerseNumber);
			int ichEndTransV1 = trans1.Length + trans2.Length + verse2.Length + trans5.Length + trans6.Length;
			bldr.SetStrPropValue(ichEndTransV1, ichEndTransV1 + verse1.Length, (int)FwTextPropType.ktptNamedStyle,
				ScrStyleNames.VerseNumber);
			m_trans.Translation.SetAlternative(bldr.GetString(), m_wsTrans);

			BtConverter converter = new BtConverter(m_para);
			converter.ConvertCmTransToInterlin(m_wsTrans);
			int cseg = Cache.GetVectorSize(m_para.Hvo, kflidSegments);
			Assert.AreEqual(8, cseg, "parsing para should produce eight segs.");
			VerifyFt(0, trans1, "first seg");
			VerifyFt(1, trans2, "2nd seg assigned sequentially");
			//VerifyFt(2, null, "no trans of verse number");
			VerifyFt(3, trans3, "3rd seg should get re-ordered trans from correct verse");
			VerifyFt(4, trans4, "4th seg also reordered");
			//VerifyFt(5, null, "no trans of verse number");
			VerifyFt(6, trans5, "5th seg should get third BT");
			VerifyFt(7, trans6, "last seg gets appropriate BT");
		}
		[Test]
		public void MissingBtVerse()
		{
			// Here for some reason the BT is out of order: has verse 10 before verse 9. The program still assigns
			// sequentially the segments that follow matching verse numbers.
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
			string trans1 = "The book is red.";
			string trans2 = "The girl is beautiful";
			//string trans3 = "The man is big.";
			//string trans4 = "I don't speak much German.";
			string trans5 = "What is that?";
			string trans6 = "How's it going?";
			bldr = m_tsf.MakeString(trans1 + trans2 + verse2 + trans5 + trans6, m_wsTrans).GetBldr();
			int ichEndTransV1 = trans1.Length + trans2.Length;
			bldr.SetStrPropValue(ichEndTransV1, ichEndTransV1 + verse2.Length, (int)FwTextPropType.ktptNamedStyle,
				ScrStyleNames.VerseNumber);
			m_trans.Translation.SetAlternative(bldr.GetString(), m_wsTrans);

			BtConverter converter = new BtConverter(m_para);
			converter.ConvertCmTransToInterlin(m_wsTrans);
			int cseg = Cache.GetVectorSize(m_para.Hvo, kflidSegments);
			Assert.AreEqual(8, cseg, "parsing para should produce eight segs.");
			VerifyFt(0, trans1, "first seg");
			VerifyFt(1, trans2, "2nd seg assigned sequentially");
			//VerifyFt(2, null, "no trans of verse number");
			VerifyFt(3, null, "3rd seg should get no BT, all BTs belong to some other verse");
			VerifyFt(4, null, "4th seg should get no BT, all BTs belong to some other verse");
			//VerifyFt(5, null, "no trans of verse number");
			VerifyFt(6, trans5, "5th seg should get third BT");
			VerifyFt(7, trans6, "last seg gets appropriate BT");
		}
		[Test]
		public void NothingAtStartOfBt()
		{
			// Here there is extra material in the paragraph before the first verse number, but not in the BT.
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
			//string trans1 = "The book is red.";
			//string trans2 = "The girl is beautiful";
			string trans3 = "The man is big.";
			string trans4 = "I don't speak much German.";
			string trans5 = "What is that?";
			string trans6 = "How's it going?";
			bldr = m_tsf.MakeString(verse1 + trans3 + trans4 + verse2 + trans5 + trans6, m_wsTrans).GetBldr();
			bldr.SetStrPropValue(0, verse1.Length,
				(int)FwTextPropType.ktptNamedStyle, ScrStyleNames.VerseNumber);
			int ichEndTransV1 = verse1.Length + trans3.Length + trans4.Length;
			bldr.SetStrPropValue(ichEndTransV1, ichEndTransV1 + verse2.Length, (int)FwTextPropType.ktptNamedStyle,
				ScrStyleNames.VerseNumber);
			m_trans.Translation.SetAlternative(bldr.GetString(), m_wsTrans);

			BtConverter converter = new BtConverter(m_para);
			converter.ConvertCmTransToInterlin(m_wsTrans);
			int cseg = Cache.GetVectorSize(m_para.Hvo, kflidSegments);
			Assert.AreEqual(8, cseg, "parsing para should produce eight segs.");
			VerifyFt(0, null, "first leading seg should have nothing");
			VerifyFt(1, null, "2nd leading seg should have nothing");
			//VerifyFt(2, null, "no trans of verse number");
			VerifyFt(3, trans3, "3rd seg should get normal stuff");
			VerifyFt(4, trans4, "4th seg also reordered");
			//VerifyFt(5, null, "no trans of verse number");
			VerifyFt(6, trans5, "5th seg should get third BT");
			VerifyFt(7, trans6, "last seg gets appropriate BT");
		}
		[Test]
		public void ExtraAtStartOfBt()
		{
			// Here there is extra material in the BT before the first verse number, but not in the paragraph.
			//string pc1 = "Das buch ist rot. ";
			//string pc2 = "Das Madchen ist shon.";
			string verse1 = "9";
			string pc3 = "Der Herr ist gross.";
			string pc4 = "Ich spreche nicht viel Deutsch.";
			string verse2 = "10";
			string pc5 = "Was is das?";
			string pc6 = "Wie gehts?";

			ITsStrBldr bldr = m_tsf.MakeString(verse1 + pc3 + pc4 + verse2 + pc5 + pc6, m_wsVern).GetBldr();
			bldr.SetStrPropValue(0, verse1.Length, (int)FwTextPropType.ktptNamedStyle,
								 ScrStyleNames.VerseNumber);
			int ichEndV1 = verse1.Length + pc3.Length + pc4.Length;
			bldr.SetStrPropValue(ichEndV1, ichEndV1 + verse2.Length, (int)FwTextPropType.ktptNamedStyle,
								 ScrStyleNames.VerseNumber);
			m_para.Contents.UnderlyingTsString = bldr.GetString();
			string trans1 = "The book is red.";
			string trans2 = "The girl is beautiful";
			string trans3 = "The man is big.";
			string trans4 = "I don't speak much German.";
			string trans5 = "What is that?";
			string trans6 = "How's it going?";
			bldr = m_tsf.MakeString(trans1 + trans2 + verse1 + trans3 + trans4 + verse2 + trans5 + trans6, m_wsTrans).GetBldr();
			bldr.SetStrPropValue(trans1.Length + trans2.Length, trans1.Length + trans2.Length + verse1.Length,
				(int)FwTextPropType.ktptNamedStyle, ScrStyleNames.VerseNumber);
			int ichEndTransV1 = trans1.Length + trans2.Length + verse1.Length + trans3.Length + trans4.Length;
			bldr.SetStrPropValue(ichEndTransV1, ichEndTransV1 + verse2.Length, (int)FwTextPropType.ktptNamedStyle,
				ScrStyleNames.VerseNumber);
			m_trans.Translation.SetAlternative(bldr.GetString(), m_wsTrans);

			BtConverter converter = new BtConverter(m_para);
			converter.ConvertCmTransToInterlin(m_wsTrans);
			int cseg = Cache.GetVectorSize(m_para.Hvo, kflidSegments);
			Assert.AreEqual(6, cseg, "parsing para should produce six segs.");
			//VerifyFt(0, null, "no trans of verse number");
			VerifyFt(1, trans1 + " " + trans2 + " " + trans3, "3rd seg should get extra from start of BT");
			VerifyFt(2, trans4, "4th seg should have corresponding BT");
			//VerifyFt(3, null, "no trans of verse number");
			VerifyFt(4, trans5, "5th seg should get third BT");
			VerifyFt(5, trans6, "last seg gets appropriate BT");
		}
		[Test]
		public void BothStartWithVerse()
		{
			// Here there is extra material in the BT before the first verse number, but not in the paragraph.
			//string pc1 = "Das buch ist rot. ";
			//string pc2 = "Das Madchen ist shon.";
			string verse1 = "9";
			string pc3 = "Der Herr ist gross.";
			string pc4 = "Ich spreche nicht viel Deutsch.";
			string verse2 = "10";
			string pc5 = "Was is das?";
			string pc6 = "Wie gehts?";

			ITsStrBldr bldr = m_tsf.MakeString(verse1 + pc3 + pc4 + verse2 + pc5 + pc6, m_wsVern).GetBldr();
			bldr.SetStrPropValue(0, verse1.Length, (int)FwTextPropType.ktptNamedStyle,
								 ScrStyleNames.VerseNumber);
			int ichEndV1 = verse1.Length + pc3.Length + pc4.Length;
			bldr.SetStrPropValue(ichEndV1, ichEndV1 + verse2.Length, (int)FwTextPropType.ktptNamedStyle,
								 ScrStyleNames.VerseNumber);
			m_para.Contents.UnderlyingTsString = bldr.GetString();
			//string trans1 = "The book is red.";
			//string trans2 = "The girl is beautiful";
			string trans3 = "The man is big.";
			string trans4 = "I don't speak much German.";
			string trans5 = "What is that?";
			string trans6 = "How's it going?";
			bldr = m_tsf.MakeString(verse1 + trans3 + trans4 + verse2 + trans5 + trans6, m_wsTrans).GetBldr();
			bldr.SetStrPropValue(0, verse1.Length,
				(int)FwTextPropType.ktptNamedStyle, ScrStyleNames.VerseNumber);
			int ichEndTransV1 = verse1.Length + trans3.Length + trans4.Length;
			bldr.SetStrPropValue(ichEndTransV1, ichEndTransV1 + verse2.Length, (int)FwTextPropType.ktptNamedStyle,
				ScrStyleNames.VerseNumber);
			m_trans.Translation.SetAlternative(bldr.GetString(), m_wsTrans);

			BtConverter converter = new BtConverter(m_para);
			converter.ConvertCmTransToInterlin(m_wsTrans);
			int cseg = Cache.GetVectorSize(m_para.Hvo, kflidSegments);
			Assert.AreEqual(6, cseg, "parsing para should produce six segs.");
			//VerifyFt(0, null, "no trans of verse number");
			VerifyFt(1, trans3, "3rd seg should have corresponding BT");
			VerifyFt(2, trans4, "4th seg should have corresponding BT");
			//VerifyFt(3, null, "no trans of verse number");
			VerifyFt(4, trans5, "5th seg should get third BT");
			VerifyFt(5, trans6, "last seg gets appropriate BT");
		}
		[Test]
		public void ChapterAndWhiteSpace()
		{
			// Here we have chapter number as well as verse number, and white space that could throw things off.
			//string pc1 = "Das buch ist rot. ";
			//string pc2 = "Das Madchen ist shon.";
			string chap1 = "1";
			string verse1 = "9";
			string pc3 = "Der Herr ist gross.";
			string pc4 = "Ich spreche nicht viel Deutsch.";
			string verse2 = "10";
			string pc5 = "Was is das?";
			string pc6 = "Wie gehts?";

			ITsStrBldr bldr = m_tsf.MakeString(" " + chap1 + " " + verse1 + pc3 + pc4 + verse2 + pc5 + pc6, m_wsVern).GetBldr();
			bldr.SetStrPropValue(1, chap1.Length, (int)FwTextPropType.ktptNamedStyle,
								 ScrStyleNames.ChapterNumber);
			bldr.SetStrPropValue(1 + chap1.Length + 1, 1 + chap1.Length + 1 + verse1.Length, (int)FwTextPropType.ktptNamedStyle,
								 ScrStyleNames.VerseNumber);
			int ichEndV1 = 1 + chap1.Length + 1 + verse1.Length + pc3.Length + pc4.Length;
			bldr.SetStrPropValue(ichEndV1, ichEndV1 + verse2.Length, (int)FwTextPropType.ktptNamedStyle,
								 ScrStyleNames.VerseNumber);
			m_para.Contents.UnderlyingTsString = bldr.GetString();
			//string trans1 = "The book is red.";
			//string trans2 = "The girl is beautiful";
			string trans3 = "The man is big.";
			//string trans4 = "I don't speak much German.";
			string trans5 = "What is that?";
			string trans6 = "How's it going?";
			bldr = m_tsf.MakeString(chap1 + verse1 + " " + trans3 + verse2 + " " + trans5 + trans6, m_wsTrans).GetBldr();
			bldr.SetStrPropValue(0, chap1.Length, (int)FwTextPropType.ktptNamedStyle,
								 ScrStyleNames.ChapterNumber);
			bldr.SetStrPropValue(chap1.Length, chap1.Length + verse1.Length,
				(int)FwTextPropType.ktptNamedStyle, ScrStyleNames.VerseNumber);
			int ichEndTransV1 = chap1.Length + verse1.Length + 1 + trans3.Length;
			bldr.SetStrPropValue(ichEndTransV1, ichEndTransV1 + verse2.Length, (int)FwTextPropType.ktptNamedStyle,
				ScrStyleNames.VerseNumber);
			m_trans.Translation.SetAlternative(bldr.GetString(), m_wsTrans);

			BtConverter converter = new BtConverter(m_para);
			converter.ConvertCmTransToInterlin(m_wsTrans);
			int cseg = Cache.GetVectorSize(m_para.Hvo, kflidSegments);
			Assert.AreEqual(6, cseg, "parsing para should produce six segs.");
			VerifyLabel(0, "chap/verse number identified as label");
			VerifyFt(1, trans3, "2nd seg should have corresponding BT");
			VerifyFt(2, null, "3rd seg should have no BT");
			VerifyLabel(3, "verse number identified as label");
			VerifyFt(4, trans5, "5th seg should get third BT");
			VerifyFt(5, trans6, "last seg gets appropriate BT");
		}

		[Test]
		public void TestStringsEqualExceptSpace()
		{
			ILgCharacterPropertyEngine cpe = Cache.LanguageWritingSystemFactoryAccessor.UnicodeCharProps;
			VerifyEquality("", "", true, cpe, "empty strings");
			VerifyEquality("a", "a", true, cpe, "single char");
			VerifyEquality(" a", "a", true, cpe, "one leading space");
			VerifyEquality("b  ", "b", true, cpe, "two trailing spaces");
			VerifyEquality("  ab  c  ", "abc", true, cpe, "multiple spaces several places");
			VerifyEquality(" a b c", "a  b  c  ", true, cpe, "spaces both sides");
			VerifyEquality("a", "b", false, cpe, "single char different");
			VerifyEquality(" a b c", "abd", false, cpe, "complex different");
			VerifyEquality("", "abd", false, cpe, "empty/non-empty");
			VerifyEquality("", " ", true, cpe, "empty/space");
			VerifyEquality("", " a b d ", false, cpe, "empty/non-empty with spaces");
		}

		void VerifyEquality(string first, string second, bool equal, ILgCharacterPropertyEngine cpe, string label)
		{
			Assert.AreEqual(equal, BtConverter.StringsEqualExceptSpace(first, second, cpe), label + " - forward");
			Assert.AreEqual(equal, BtConverter.StringsEqualExceptSpace(second, first, cpe), label + " - backward");
			Assert.AreEqual(true, BtConverter.StringsEqualExceptSpace(first, first, cpe), label + " - first to self");
			Assert.AreEqual(true, BtConverter.StringsEqualExceptSpace(second, second, cpe), label + " - second to self");
		}

		[Test]
		public void SmartResync()
		{
			// Here we end up with four segments in the second group. After building it once, we make a change that merges
			// the middle two BT segments. We hope the fourth stays aligned.
			string pc1 = "Das buch ist rot. ";
			string pc2 = "Das Madchen ist shon.";
			string verse1 = "9";
			string pc3 = "Der Herr ist gross.";
			string pc4 = "Ich spreche nicht viel Deutsch.";
			string pc5 = "Was is das?";
			string pc6 = "Wie gehts?";

			ITsStrBldr bldr = m_tsf.MakeString(pc1 + pc2 + verse1 + pc3 + pc4 + pc5 + pc6, m_wsVern).GetBldr();
			bldr.SetStrPropValue(pc1.Length + pc2.Length, pc1.Length + pc2.Length + verse1.Length, (int)FwTextPropType.ktptNamedStyle,
								 ScrStyleNames.VerseNumber);
			m_para.Contents.UnderlyingTsString = bldr.GetString();
			string trans1 = "The book is red.";
			//string trans2 = "The girl is beautiful";
			string trans3 = "The man is big.";
			string trans4 = "I don't speak much German.";
			string trans5 = "What is that?";
			string trans6 = "How's it going?";
			bldr = m_tsf.MakeString(trans1 + verse1 + trans3 + trans4 + trans5 + trans6, m_wsTrans).GetBldr();
			bldr.SetStrPropValue(trans1.Length, trans1.Length + verse1.Length,
				(int)FwTextPropType.ktptNamedStyle, ScrStyleNames.VerseNumber);
			m_trans.Translation.SetAlternative(bldr.GetString(), m_wsTrans);

			BtConverter converter = new BtConverter(m_para);
			converter.ConvertCmTransToInterlin(m_wsTrans);
			int cseg = Cache.GetVectorSize(m_para.Hvo, kflidSegments);
			Assert.AreEqual(7, cseg, "parsing para should produce seven segs.");
			VerifyFt(0, trans1, "first leading seg should have proper trans");
			VerifyFt(1, null, "2nd leading seg should have nothing");
			//VerifyFt(2, null, "no trans of verse number");
			VerifyFt(3, trans3, "4th seg should get normal stuff");
			VerifyFt(4, trans4, "5th seg gets trans in seqyence");
			VerifyFt(5, trans5, "6th seg gets trans in seq");
			VerifyFt(6, trans6, "last seg gets trans in seq");

			// Now, the real test!

			int ichDot = trans1.Length + verse1.Length + trans3.Length + trans4.Length - 1;
			bldr = m_trans.Translation.GetAlternative(m_wsTrans).UnderlyingTsString.GetBldr();
			bldr.ReplaceTsString(ichDot, ichDot + 1, null);
			m_trans.Translation.SetAlternative(bldr.GetString(), m_wsTrans);
			converter = new BtConverter(m_para);
			converter.ConvertCmTransToInterlin(m_wsTrans);

			cseg = Cache.GetVectorSize(m_para.Hvo, kflidSegments);
			Assert.AreEqual(7, cseg, "reparsing para should produce seven segs.");
			VerifyFt(0, trans1, "first leading seg should (still) have proper trans");
			VerifyFt(1, null, "2nd leading seg should (still) have nothing");
			//VerifyFt(2, null, "no trans of verse number");
			VerifyFt(3, trans3, "4th seg should get normal stuff");
			VerifyFt(4, trans4.Substring(0, trans4.Length - 1) + trans5, "4th seg gets blended translations");
			VerifyFt(5, null, "skip 6th seg to keep sixth unchanged");
			VerifyFt(6, trans6, "6th seg is not changed.");
		}
	}
}
