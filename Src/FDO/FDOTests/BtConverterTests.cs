using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.Utils;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// <summary>
	/// Test the Back translation conversion class.
	/// </summary>
	[TestFixture]
	public class BtConverterTests : ScrInMemoryFdoTestBase
	{
		private IText m_text;
		private IStTxtPara m_para;
		private ICmTranslation m_trans;
		ITsStrFactory m_tsf;
		int m_wsVern;
		int m_wsTrans;

		/// <summary>
		///
		/// </summary>
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, FixtureSetupInternal);
		}

		private void FixtureSetupInternal()
		{
			//IWritingSystem wsEn = Cache.WritingSystemFactory.get_Engine("en");
			// Setup default analysis ws
			//m_wsEn = Cache.ServiceLocator.GetInstance<ILgWritingSystemRepository>().GetObject(wsEn.WritingSystem);
			m_wsVern = Cache.DefaultVernWs;
			m_wsTrans = Cache.DefaultAnalWs;

			m_text = Cache.ServiceLocator.GetInstance<ITextFactory>().Create();
			//Cache.LangProject.TextsOC.Add(m_text);
			var stText = Cache.ServiceLocator.GetInstance<IStTextFactory>().Create();
			m_text.ContentsOA = stText;
			m_para = Cache.ServiceLocator.GetInstance<IScrTxtParaFactory>().CreateWithStyle(stText, ScrStyleNames.NormalParagraph);

			m_trans = m_para.GetOrCreateBT();
			Cache.LangProject.TranslatedScriptureOA.UseScriptDigits = false;
			m_tsf = Cache.TsStrFactory;
		}

		/// <summary>
		/// The simplest possible case, paragraph has single segment, copy whole BT across.
		/// </summary>
		[Test]
		public void SingleSegment()
		{
			string paraContents = "Das buch ist rot";
			string trans = "The book is red";
			m_para.Contents = m_tsf.MakeString(paraContents, m_wsVern);
			m_trans.Translation.set_String(m_wsTrans, trans);
			int cseg = m_para.SegmentsOS.Count;
			Assert.AreEqual(1, cseg, "Para with no EOS or verse should have one segment");
			VerifyFt(0, trans, "whole translation should be transferred to single ft.");
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void EmptyPara()
		{
			string paraContents = "";
			string trans = "";
			m_para.Contents = m_tsf.MakeString(paraContents, m_wsVern);
			m_trans.Translation.set_String(m_wsTrans, trans);
			int cseg = m_para.SegmentsOS.Count;
			Assert.AreEqual(0, cseg, "Empty para should have no segments");
		}

		/// <summary>
		///
		/// </summary>
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
			m_para.Contents = bldr.GetString();
			string trans1 = "The book is red.";
			string trans2 = "The girl is beautiful";
			bldr = m_tsf.MakeString(trans1 + verse + trans2, m_wsTrans).GetBldr();
			bldr.SetStrPropValue(trans1.Length, trans1.Length + verse.Length, (int)FwTextPropType.ktptNamedStyle,
								 ScrStyleNames.VerseNumber);
			m_trans.Translation.set_String(m_wsTrans, bldr.GetString());
			int cseg = m_para.SegmentsOS.Count;
			Assert.AreEqual(3, cseg, "parsing para should produce three segs.");
			VerifyFt(0, trans1, "first seg");
			VerifyFt(1, null, "no second FT");
			VerifyFt(2, trans2, "last seg");
		}

		void VerifyLabel(int iseg, string label)
		{
			Assert.IsTrue(m_para.SegmentsOS[iseg].IsLabel, label);
		}

		/// <summary>
		/// Verify that the segment indicated by hvoSeg has a free translation with Comment alternative m_wsTrans
		/// set to contents. If contents is null, it is acceptable to have no FT at all.
		/// </summary>
		void VerifyFt(int iseg, string contents, string label)
		{
			var seg = m_para.SegmentsOS[iseg];
			if (contents == null && seg.FreeTranslation.get_String(m_wsTrans).Length == 0)
				return;
			Assert.AreEqual(contents, seg.FreeTranslation.get_String(m_wsTrans).Text, label + " - comment");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test case where the vernacular has two sentences per verse but the BT has only one.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExtraSegsInPara_BTsForAllVerses()
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
			m_para.Contents = bldr.GetString();
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
			m_trans.Translation.set_String(m_wsTrans, bldr.GetString());
			int cseg = m_para.SegmentsOS.Count;
			Assert.AreEqual(8, cseg, "parsing para should produce eight segs.");
			VerifyFt(0, trans1, "first seg");
			VerifyFt(1, null, "verse number sync should keep second BT empty");
			VerifyFt(2, null, "no trans of verse number");
			VerifyFt(3, trans3, "3rd seg should get second BT");
			VerifyFt(4, null, "verse number sync should keep fourth BT empty");
			VerifyFt(5, null, "no trans of verse number");
			VerifyFt(6, trans5, "5th seg should get third BT");
			VerifyFt(7, null, "last seg left with empty BT");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test case where the BT does not have BTs for some verses. (FWR-2414)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExtraSegsInPara_BtMissingForSomeVerses()
		{
			// Make a paragraph where para contents has three verses each with two segs, BT has only one seg per verse.
			string verse1 = "7";
			string pc1 = "Das Madchen ist shon.";
			string verse2 = "8";
			string pc2 = "Der Herr ist gross.";
			string verse3 = "9";
			string pc3 = "Ich spreche nicht viel Deutsch.";

			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, verse1, StyleUtils.CharStyleTextProps(ScrStyleNames.VerseNumber, m_wsVern));
			bldr.Replace(bldr.Length, bldr.Length, pc1, StyleUtils.CharStyleTextProps(null, m_wsVern));
			bldr.Replace(bldr.Length, bldr.Length, verse2, StyleUtils.CharStyleTextProps(ScrStyleNames.VerseNumber, m_wsVern));
			bldr.Replace(bldr.Length, bldr.Length, pc2, StyleUtils.CharStyleTextProps(null, m_wsVern));
			bldr.Replace(bldr.Length, bldr.Length, verse3, StyleUtils.CharStyleTextProps(ScrStyleNames.VerseNumber, m_wsVern));
			bldr.Replace(bldr.Length, bldr.Length, pc3, StyleUtils.CharStyleTextProps(null, m_wsVern));
			m_para.Contents = bldr.GetString();
			string trans1 = "The book is red.";
			string trans3 = "The man is big.";
			bldr.Clear();
			bldr.Replace(0, 0, verse1, StyleUtils.CharStyleTextProps(ScrStyleNames.VerseNumber, m_wsTrans));
			bldr.Replace(bldr.Length, bldr.Length, trans1, StyleUtils.CharStyleTextProps(null, m_wsTrans));
			bldr.Replace(bldr.Length, bldr.Length, verse2, StyleUtils.CharStyleTextProps(ScrStyleNames.VerseNumber, m_wsTrans));
			bldr.Replace(bldr.Length, bldr.Length, " ", StyleUtils.CharStyleTextProps(null, m_wsTrans));
			bldr.Replace(bldr.Length, bldr.Length, verse3, StyleUtils.CharStyleTextProps(ScrStyleNames.VerseNumber, m_wsTrans));
			bldr.Replace(bldr.Length, bldr.Length, trans3, StyleUtils.CharStyleTextProps(null, m_wsTrans));
			m_trans.Translation.set_String(m_wsTrans, bldr.GetString());
			int cseg = m_para.SegmentsOS.Count;
			Assert.AreEqual(6, cseg, "parsing para should produce six segs.");
			VerifyFt(0, null, "verse number sync should keep BT empty for label segment");
			VerifyFt(1, trans1, "first verse should keep its BT");
			VerifyFt(2, null, "verse number sync should keep BT empty for label segment");
			VerifyFt(3, null, "second verse should have no BT");
			VerifyFt(4, null, "verse number sync should keep BT empty for label segment");
			VerifyFt(5, trans3, "third verse should keep its BT");
		}

		/// <summary>
		///
		/// </summary>
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
			m_para.Contents = bldr.GetString();
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
			m_trans.Translation.set_String(m_wsTrans, bldr.GetString());
			int cseg = m_para.SegmentsOS.Count;
			Assert.AreEqual(8, cseg, "parsing para should produce eight segs.");
			VerifyFt(0, trans1, "first seg");
			VerifyFt(1, null, "verse number sync should keep second BT empty");
			VerifyFt(2, null, "no trans of verse number");
			VerifyFt(3, trans3, "3rd seg should get second BT");
			VerifyFt(4, trans5, "2nd verse does not match, keep assigning sequentially");
			VerifyFt(5, null, "no trans of verse number");
			VerifyFt(6, null, "5th seg has no BTs left to assign");
			VerifyFt(7, null, "last seg left with empty BT");
		}

		/// <summary>
		///
		/// </summary>
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
			m_para.Contents = bldr.GetString();
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
			m_trans.Translation.set_String(m_wsTrans, bldr.GetString());
			int cseg = m_para.SegmentsOS.Count;
			Assert.AreEqual(5, cseg, "parsing para should produce five segs.");
			VerifyFt(0, trans1 + " " + trans2, "first seg should have first TWO translations");
			VerifyFt(1, null, "no trans of verse number");
			VerifyFt(2, trans3, "3rd seg should get next BTs");
			VerifyFt(3, null, "no trans of verse number");
			VerifyFt(4, trans4 + " " + trans5 + " " + trans6, "5th seg should get last three BTs");
		}

		/// <summary>
		///
		/// </summary>
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
			m_para.Contents = bldr.GetString();
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
			m_trans.Translation.set_String(m_wsTrans, bldr.GetString());
			int cseg = m_para.SegmentsOS.Count;
			Assert.AreEqual(5, cseg, "parsing para should produce five segs.");
			VerifyFt(0, trans1 + " " + trans2, "first seg should have first TWO translations");
			VerifyFt(1, null, "no trans of verse number");
			VerifyFt(2, trans3 + " " + trans4, "3rd seg should get next TWO BTs");
			VerifyFt(3, null, "no trans of verse number");
			VerifyFt(4, trans5 + " " + trans6, "5th seg should get fifth and sixth BTs");
		}

		/// <summary>
		///
		/// </summary>
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
				m_para.Contents = bldr.GetString();
				string trans1 = "The book is red.";
				string trans2 = "The girl is beautiful";
				string trans3 = "The main is big.";
				string trans4 = "I don't speak much German.";
				string trans5 = "What is that?";
				string trans6 = "How's it going?";
				bldr = m_tsf.MakeString(trans1 + trans2 + verse1 + trans3 + trans4 + verse2 + trans5 + trans6, m_wsTrans).GetBldr();
				bldr.SetStrPropValue(trans1.Length + trans2.Length, trans1.Length + trans2.Length + verse1.Length,
					(int) FwTextPropType.ktptNamedStyle, ScrStyleNames.VerseNumber);
				int ichEndTransV1 = trans1.Length + trans2.Length + verse1.Length + trans3.Length + trans4.Length;
				bldr.SetStrPropValue(ichEndTransV1, ichEndTransV1 + verse2.Length, (int) FwTextPropType.ktptNamedStyle,
					ScrStyleNames.VerseNumber);
				m_trans.Translation.set_String(m_wsTrans, bldr.GetString());
				int cseg = m_para.SegmentsOS.Count;
				Assert.AreEqual(5, cseg, "parsing para should produce five segs.");
				VerifyFt(0, trans1 + " " + trans2, "first seg should have first TWO translations");
				VerifyFt(1, null, "no trans of verse number");
				VerifyFt(2, trans3 + " " + trans4, "3rd seg should get next TWO BTs");
				VerifyFt(3, null, "no trans of verse number");
				VerifyFt(4, trans5 + " " + trans6, "5th seg should get fifth and sixth BTs");
			}
			finally
			{
				Cache.LangProject.TranslatedScriptureOA.UseScriptDigits = false;
				Cache.LangProject.TranslatedScriptureOA.ScriptDigitZero = oldDigitZero;
			}
		}

		/// <summary>
		///
		/// </summary>
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
			m_para.Contents = bldr.GetString();
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
			m_trans.Translation.set_String(m_wsTrans, bldr.GetString());
			int cseg = m_para.SegmentsOS.Count;
			Assert.AreEqual(8, cseg, "parsing para should produce eight segs.");
			VerifyFt(0, trans1, "first seg");
			VerifyFt(1, trans2, "2nd seg assigned sequentially");
			VerifyFt(2, null, "no trans of verse number");
			VerifyFt(3, trans3, "3rd seg should get re-ordered trans from correct verse");
			VerifyFt(4, trans4, "4th seg also reordered");
			VerifyFt(5, null, "no trans of verse number");
			VerifyFt(6, trans5, "5th seg should get third BT");
			VerifyFt(7, trans6, "last seg gets appropriate BT");
		}

		/// <summary>
		///
		/// </summary>
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
			m_para.Contents = bldr.GetString();
			string trans1 = "The book is red.";
			string trans2 = "The girl is beautiful";
			string trans5 = "What is that?";
			string trans6 = "How's it going?";
			bldr = m_tsf.MakeString(trans1 + trans2 + verse2 + trans5 + trans6, m_wsTrans).GetBldr();
			int ichEndTransV1 = trans1.Length + trans2.Length;
			bldr.SetStrPropValue(ichEndTransV1, ichEndTransV1 + verse2.Length, (int)FwTextPropType.ktptNamedStyle,
								 ScrStyleNames.VerseNumber);
			m_trans.Translation.set_String(m_wsTrans, bldr.GetString());
			int cseg = m_para.SegmentsOS.Count;
			Assert.AreEqual(8, cseg, "parsing para should produce eight segs.");
			VerifyFt(0, trans1, "first seg");
			VerifyFt(1, trans2, "2nd seg assigned sequentially");
			VerifyFt(2, null, "no trans of verse number");
			VerifyFt(3, null, "3rd seg should get no BT, all BTs belong to some other verse");
			VerifyFt(4, null, "4th seg should get no BT, all BTs belong to some other verse");
			VerifyFt(5, null, "no trans of verse number");
			VerifyFt(6, trans5, "5th seg should get third BT");
			VerifyFt(7, trans6, "last seg gets appropriate BT");
		}

		/// <summary>
		/// Test that footnote ORCs do not produce segment breaks
		/// </summary>
		[Test]
		public void MultipleORCsInOneVerse()
		{
			string pc1 = "Das buch ist rot.";
			string pc2 = " Das Madchen ist shon. ";
			string pc3 = "Der Herr ist gross. ";
			string pc4 = "Ich spreche nicht viel Deutsch. ";
			string orc = StringUtils.kChObject.ToString();
			ITsStrBldr bldr = m_tsf.MakeString(pc1 + orc + pc2 + orc + pc3 + pc4, m_wsVern).GetBldr();
			bldr.SetStrPropValue(pc1.Length, pc1.Length + orc.Length, (int)FwTextPropType.ktptObjData, string.Empty);
			bldr.SetStrPropValue(pc1.Length + orc.Length + pc2.Length, pc1.Length + orc.Length + pc2.Length + orc.Length,
				(int)FwTextPropType.ktptObjData, string.Empty);
			m_para.Contents = bldr.GetString();

			string trans1 = "The book is red.";
			string trans2 = " The girl is beautiful. ";
			string trans3 = "The man is big. ";
			string trans4 = "I don't speak much German. ";
			bldr = m_tsf.MakeString(trans1 + orc + trans2 + orc + trans3 + trans4, m_wsTrans).GetBldr();
			bldr.SetStrPropValue(trans1.Length, trans1.Length + orc.Length, (int)FwTextPropType.ktptObjData, string.Empty);
			bldr.SetStrPropValue(trans1.Length + orc.Length + trans2.Length, trans1.Length + orc.Length + trans2.Length + orc.Length,
				(int)FwTextPropType.ktptObjData, string.Empty);
			m_trans.Translation.set_String(m_wsTrans, bldr.GetString());

			int cseg = m_para.SegmentsOS.Count;
			Assert.AreEqual(4, cseg, "parsing para should produce six segs.");
			VerifyFt(0, trans1 + orc + " ", "1st seg for translation 1");
			VerifyFt(1, trans2.TrimStart(), "2nd seg for translation 2");
			VerifyFt(2, orc + trans3, "3rd seg for translation 3");
			VerifyFt(3, trans4, "4th seg for translation 4");
		}

		/// <summary>
		///
		/// </summary>
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
			m_para.Contents = bldr.GetString();
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
			m_trans.Translation.set_String(m_wsTrans, bldr.GetString());
			int cseg = m_para.SegmentsOS.Count;
			Assert.AreEqual(8, cseg, "parsing para should produce eight segs.");
			VerifyFt(0, null, "first leading seg should have nothing");
			VerifyFt(1, null, "2nd leading seg should have nothing");
			VerifyFt(2, null, "no trans of verse number");
			VerifyFt(3, trans3, "3rd seg should get normal stuff");
			VerifyFt(4, trans4, "4th seg also reordered");
			VerifyFt(5, null, "no trans of verse number");
			VerifyFt(6, trans5, "5th seg should get third BT");
			VerifyFt(7, trans6, "last seg gets appropriate BT");
		}

		/// <summary>
		///
		/// </summary>
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
			m_para.Contents = bldr.GetString();
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
			m_trans.Translation.set_String(m_wsTrans, bldr.GetString());
			int cseg = m_para.SegmentsOS.Count;
			Assert.AreEqual(6, cseg, "parsing para should produce six segs.");
			VerifyFt(0, null, "no trans of verse number");
			VerifyFt(1, trans1 + " " + trans2 + " " + trans3, "3rd seg should get extra from start of BT");
			VerifyFt(2, trans4, "4th seg should have corresponding BT");
			VerifyFt(3, null, "no trans of verse number");
			VerifyFt(4, trans5, "5th seg should get third BT");
			VerifyFt(5, trans6, "last seg gets appropriate BT");
		}

		/// <summary>
		///
		/// </summary>
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
			m_para.Contents = bldr.GetString();
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
			m_trans.Translation.set_String(m_wsTrans, bldr.GetString());
			int cseg = m_para.SegmentsOS.Count;
			Assert.AreEqual(6, cseg, "parsing para should produce six segs.");
			VerifyFt(0, null, "no trans of verse number");
			VerifyFt(1, trans3, "3rd seg should have corresponding BT");
			VerifyFt(2, trans4, "4th seg should have corresponding BT");
			VerifyFt(3, null, "no trans of verse number");
			VerifyFt(4, trans5, "5th seg should get third BT");
			VerifyFt(5, trans6, "last seg gets appropriate BT");
		}

		/// <summary>
		///
		/// </summary>
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
			m_para.Contents = bldr.GetString();
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
			m_trans.Translation.set_String(m_wsTrans, bldr.GetString());
			int cseg = m_para.SegmentsOS.Count;
			Assert.AreEqual(6, cseg, "parsing para should produce six segs.");
			VerifyLabel(0, "chap/verse number identified as label");
			VerifyFt(1, trans3, "2nd seg should have corresponding BT");
			VerifyFt(2, null, "3rd seg should have no BT");
			VerifyLabel(3, "verse number identified as label");
			VerifyFt(4, trans5, "5th seg should get third BT");
			VerifyFt(5, trans6, "last seg gets appropriate BT");
		}

		/// <summary>
		///
		/// </summary>
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
			m_para.Contents = bldr.GetString();
			string trans1 = "The book is red.";
			//string trans2 = "The girl is beautiful";
			string trans3 = "The man is big.";
			string trans4 = "I don't speak much German.";
			string trans5 = "What is that?";
			string trans6 = "How's it going?";
			bldr = m_tsf.MakeString(trans1 + verse1 + trans3 + trans4 + trans5 + trans6, m_wsTrans).GetBldr();
			bldr.SetStrPropValue(trans1.Length, trans1.Length + verse1.Length,
								 (int)FwTextPropType.ktptNamedStyle, ScrStyleNames.VerseNumber);
			m_trans.Translation.set_String(m_wsTrans, bldr.GetString());

			int cseg = m_para.SegmentsOS.Count;
			Assert.AreEqual(7, cseg, "parsing para should produce seven segs.");
			VerifyFt(0, trans1, "first leading seg should have proper trans");
			VerifyFt(1, null, "2nd leading seg should have nothing");
			VerifyFt(2, null, "no trans of verse number");
			VerifyFt(3, trans3, "4th seg should get normal stuff");
			VerifyFt(4, trans4, "5th seg gets trans in seqyence");
			VerifyFt(5, trans5, "6th seg gets trans in seq");
			VerifyFt(6, trans6, "last seg gets trans in seq");

			// Now, the real test!

			int ichDot = trans1.Length + verse1.Length + trans3.Length + trans4.Length - 1;
			bldr = m_trans.Translation.get_String(m_wsTrans).GetBldr();
			bldr.ReplaceTsString(ichDot, ichDot + 1, null);
			m_trans.Translation.set_String(m_wsTrans, bldr.GetString());

			cseg = m_para.SegmentsOS.Count;
			Assert.AreEqual(7, cseg, "reparsing para should produce seven segs.");
			VerifyFt(0, trans1, "first leading seg should (still) have proper trans");
			VerifyFt(1, null, "2nd leading seg should (still) have nothing");
			VerifyFt(2, null, "no trans of verse number");
			VerifyFt(3, trans3, "4th seg should get normal stuff");
			VerifyFt(4, trans4.Substring(0, trans4.Length - 1) + trans5, "4th seg gets blended translations");
			VerifyFt(5, null, "skip 6th seg to keep sixth unchanged");
			VerifyFt(6, trans6, "6th seg is not changed.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the StringsEndWithSameWord method for simple strings that all have the same
		/// properties and writing systems
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestStringsEndWithSameWord_Simple()
		{
			TestStringsEndWithSameWord("", "", false, "empty strings");
			TestStringsEndWithSameWord("a", "a", true, "single char");
			TestStringsEndWithSameWord(" a", "a", true, "one leading space");
			TestStringsEndWithSameWord("b  ", "b", true, "two trailing spaces");
			TestStringsEndWithSameWord("  ab  c  ", "abc", false, "multiple spaces several places different words");
			TestStringsEndWithSameWord("  ab  c  ", "ab c", true, "multiple spaces several places");
			TestStringsEndWithSameWord(" a b c", "a  b  c  ", true, "spaces both sides");
			TestStringsEndWithSameWord("a", "b", false, "single char different");
			TestStringsEndWithSameWord(" a b c", "abd", false, "complex different");
			TestStringsEndWithSameWord("", "abd", false, "empty/non-empty");
			TestStringsEndWithSameWord("", " ", false, "empty/space");
			TestStringsEndWithSameWord("", " a b d ", false, "empty/non-empty with spaces");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the StringsEndWithSameWord for strings the differ only by writing system
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestStringsEndWithSameWord_DifferByWritingSystem()
		{
			ITsString firstTss = m_tsf.MakeString("", m_wsEn);
			ITsString secondTss = m_tsf.MakeString("", m_wsDe);
			Assert.IsFalse(BtConverter.StringsEndWithSameWord(firstTss, secondTss));

			firstTss = m_tsf.MakeString(" a  b", m_wsEn);
			secondTss = m_tsf.MakeString("a b", m_wsDe);
			Assert.IsTrue(BtConverter.StringsEndWithSameWord(firstTss, secondTss));

			firstTss = m_tsf.MakeString("ab", m_wsEn).Replace(2, 0, m_tsf.MakeString("cd", m_wsDe));
			secondTss = m_tsf.MakeString("ab  cd ", m_wsDe);
			Assert.IsTrue(BtConverter.StringsEndWithSameWord(firstTss, secondTss));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the StringsEndWithSameWord for strings that have applied style properties
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestStringsEndWithSameWord_AppliedStyle()
		{
			ITsString firstTss = TsStringUtils.MakeTss("", m_wsEn, "Chapter");
			ITsString secondTss = TsStringUtils.MakeTss("", m_wsEn, "Verse");
			Assert.IsFalse(BtConverter.StringsEndWithSameWord(firstTss, secondTss));

			firstTss = TsStringUtils.MakeTss("a sd 1", m_wsEn, "Chapter");
			secondTss = TsStringUtils.MakeTss(" a  sd   1  ", m_wsEn, "Chapter");
			Assert.IsTrue(BtConverter.StringsEndWithSameWord(firstTss, secondTss));

			firstTss = TsStringUtils.MakeTss("a sd 1", m_wsEn, "Chapter");
			secondTss = TsStringUtils.MakeTss(" a  sd   1  ", m_wsEn, "Verse");
			Assert.IsFalse(BtConverter.StringsEndWithSameWord(firstTss, secondTss));

			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Append("3", StyleUtils.CharStyleTextProps("Chapter", m_wsEn));
			bldr.Append("4", StyleUtils.CharStyleTextProps("Verse", m_wsEn));
			firstTss = bldr.GetString();
			bldr.Clear();
			bldr.Append("  3  ", StyleUtils.CharStyleTextProps("Chapter", m_wsEn));
			bldr.Append("  4  ", StyleUtils.CharStyleTextProps("Verse", m_wsEn));
			secondTss = bldr.GetString();
			Assert.IsTrue(BtConverter.StringsEndWithSameWord(firstTss, secondTss));

			bldr.Clear();
			bldr.Append("1", StyleUtils.CharStyleTextProps("Chapter", m_wsEn));
			bldr.Append("1", StyleUtils.CharStyleTextProps("Verse", m_wsEn));
			firstTss = bldr.GetString();
			secondTss = TsStringUtils.MakeTss("11", m_wsEn, "Chapter");
			Assert.IsFalse(BtConverter.StringsEndWithSameWord(firstTss, secondTss));

			secondTss = TsStringUtils.MakeTss("1", m_wsEn, "Verse");
			Assert.IsTrue(BtConverter.StringsEndWithSameWord(firstTss, secondTss));

			bldr.Clear();
			bldr.Append("1", StyleUtils.CharStyleTextProps("Chapter", m_wsEn));
			bldr.Append("1", StyleUtils.CharStyleTextProps("Verse", m_wsEn));
			bldr.Append(" ", StyleUtils.CharStyleTextProps(null, m_wsEn)); // This trailing space should be ignored
			secondTss = bldr.GetString();
			Assert.IsTrue(BtConverter.StringsEndWithSameWord(firstTss, secondTss));
		}

		#region Helper methods

		private void TestStringsEndWithSameWord(string first, string second, bool equal, string label)
		{
			ITsString firstTss = m_tsf.MakeString(first, m_wsEn);
			ITsString secondTss = m_tsf.MakeString(second, m_wsEn);
			Assert.AreEqual(equal, BtConverter.StringsEndWithSameWord(firstTss, secondTss), label + " - forward");
		}
		#endregion
	}
}