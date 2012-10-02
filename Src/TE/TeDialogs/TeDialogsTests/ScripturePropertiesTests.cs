// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ScripturePropertiesTests.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;

using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Common.RootSites;
using SIL.Utils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.Test.TestUtils;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Contains tests for ScriptureProperties dialog
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class ScripturePropertiesTests : ScrInMemoryFdoTestBase
	{
		#region Member variables
		/// <summary></summary>
		private IScrBook m_exodus;
		private FwStyleSheet m_stylesheet;
		#endregion

		#region Test setup
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			m_stylesheet = new FwStyleSheet();
			m_stylesheet.Init(Cache, m_scr.Hvo, ScriptureTags.kflidStyles);

			m_exodus = AddBookToMockedScripture(2, "Exodus");
			AddTitleToMockedBook(m_exodus, "Exodus");

			IScrSection section = AddSectionToMockedBook(m_exodus);
			AddSectionHeadParaToSection(section, "Section Heading", ScrStyleNames.SectionHead);
			IScrTxtPara para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "Verse one. ", null);
			AddRunToMockedPara(para, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "Verse two.", null);
			AddRunToMockedPara(para, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "Verse three.", null);
			AddRunToMockedPara(para, "4", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "Verse four. ", null);
			AddRunToMockedPara(para, "5", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "Verse five.", null);
			AddRunToMockedPara(para, "6", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "Verse six. ", null);
			AddRunToMockedPara(para, "7", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "Verse seven.", null);
			AddRunToMockedPara(para, "8", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "Verse seven.", null);
			AddRunToMockedPara(para, "9", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "Verse seven.", null);
			AddRunToMockedPara(para, "10", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "Verse seven.", null);
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public override void TestTearDown()
		{
			m_exodus = null;

			base.TestTearDown();
		}
		#endregion

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test converting the digits to a specific digit set
		/// </summary>
		/// <param name="zeroChar">zero character in the desired language</param>
		/// <param name="nineChar">nine character in the desired language</param>
		/// <param name="dlg">scripture properties dialog</param>
		/// ------------------------------------------------------------------------------------
		private void ScriptDigitConversionTest(char zeroChar, char nineChar, ScriptureProperties dlg)
		{
			m_scr.ScriptDigitZero = zeroChar;
			m_scr.UseScriptDigits = (zeroChar != '0');

			ReflectionHelper.CallMethod(dlg, "ConvertChapterVerseNumbers", null);

			int[] expectedNumbers = new int[] { 1, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
			int expectedIndex = 0;

			ITsString tss = ((IStTxtPara)((IScrSection)m_exodus.SectionsOS[0]).ContentOA.ParagraphsOS[0]).Contents;
			for (int i = 0; i < tss.RunCount; i++)
			{
				TsRunInfo tri;
				ITsTextProps ttp = tss.FetchRunInfo(i, out tri);
				IStStyle style = m_scr.FindStyle(ttp);
				if (style != null &&
					(style.Function == FunctionValues.Verse ||
					style.Function == FunctionValues.Chapter))
				{
					int expectedNumber = expectedNumbers[expectedIndex++];
					string runChars = tss.GetChars(tri.ichMin, tri.ichLim);
					// make sure the expected digits were found
					Assert.AreEqual(expectedNumber, ScrReference.ChapterToInt(runChars));

					// make sure that all of the digits are in the desired language
					foreach (char c in runChars)
					{
						if (Char.IsDigit(c))
							Assert.IsTrue(c >= zeroChar && c <= nineChar, "Found incorrect digit");
					}
				}
			}

			// Make sure we saw all the expected numbers in the data
			Assert.AreEqual(expectedNumbers.Length, expectedIndex);
		}
		#endregion

		#region Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the conversion of chapter and verse numbers between Arabic and Bengali
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ConvertChapterVerseNumbersTest_Bengali()
		{
			using (ScriptureProperties dlg = new ScriptureProperties(Cache, m_stylesheet, null, true, null))
			{
				char bengaliZero = '\u09e6';
				char bengaliNine = (char)((int)bengaliZero + 9);

				// test arabic->bengali
				ScriptDigitConversionTest(bengaliZero, bengaliNine, dlg);
				// test bengali->arabic
				ScriptDigitConversionTest('0', '9', dlg);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the conversion of an empty ChapterNumber run (TE-5628).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ConvertChapterVerseNumbersTest_EmptyChapterNumber()
		{
			// Add paragraph with empty chapter number run.
			IStTxtPara para = AddParaToMockedSectionContent(m_exodus.SectionsOS[0], ScrStyleNames.ChapterNumber);
			ITsPropsFactory propFact = TsPropsFactoryClass.Create();
			ITsTextProps ttpChapterNumber = propFact.MakeProps(ScrStyleNames.ChapterNumber,
				Cache.ServiceLocator.WritingSystemManager.GetWsFromStr("fr"), 0);
			ITsStrBldr bldr = para.Contents.GetBldr();
			bldr.SetProperties(0, 0, ttpChapterNumber);
			para.Contents = bldr.GetString();

			using (ScriptureProperties dlg = new ScriptureProperties(Cache, m_stylesheet, null, true, null))
			{
				char bengaliZero = '\u09e6';
				char bengaliNine = (char)((int)bengaliZero + 9);

				// test arabic->bengali when there is a paragraph with an empty chapter number run.
				// It should complete without crashing.
				ScriptDigitConversionTest(bengaliZero, bengaliNine, dlg);
				// test bengali->arabic
				ScriptDigitConversionTest('0', '9', dlg);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests UpdateVerseBridgesInParagraph when there is an invalid verse bridge with a
		/// right-to-left writing system.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UpdateVerseBridgesInParagraph_ValidRtoLBridge()
		{
			// Set up a verse with invalid verse bridge
			IScrTxtPara para = AddParaToMockedSectionContent(m_exodus.SectionsOS[0], ScrStyleNames.NormalParagraph);
			AddVerse(para, 0, 5, "Verse five.");
			// Right-to-Left Bridge: 6-24
			// NOTE: The verse number format is how TE represents verse numbers with bridges.
			// This method should not change anything but the verse bridge character.
			AddVerse(para, 0, "6" + '\u200f' + '\u200f' + "@" + '\u200f' + '\u200f' + '\u200f' + "24",
				"Verse with valid R-to-L bridge.");

			using (ScriptureProperties dlg = new ScriptureProperties(Cache, m_stylesheet, null, true, null))
			{
				ReflectionHelper.CallMethod(dlg, "UpdateVerseBridgesInParagraph", para,
					'\u200f' + "@" + '\u200f', '\u200f' + "&" + '\u200f');

				AssertEx.RunIsCorrect(para.Contents, 0, "5", ScrStyleNames.VerseNumber,
					Cache.DefaultVernWs);
				AssertEx.RunIsCorrect(para.Contents, 1, "Verse five.", null, Cache.DefaultVernWs);
				AssertEx.RunIsCorrect(para.Contents, 2, "6" + '\u200f' + '\u200f' + "&" +
					'\u200f' + '\u200f' + '\u200f' + "24", ScrStyleNames.VerseNumber, Cache.DefaultVernWs);
				AssertEx.RunIsCorrect(para.Contents, 3, "Verse with valid R-to-L bridge.",
					null, Cache.DefaultVernWs);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests UpdateVerseBridgesInParagraph when there is an invalid verse bridge with a
		/// right-to-left writing system.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UpdateVerseBridgesInParagraph_InvalidRtoLBridge()
		{
			// Set up a verse with invalid verse bridge
			IScrTxtPara para = AddParaToMockedSectionContent(m_exodus.SectionsOS[0], ScrStyleNames.NormalParagraph);
			AddVerse(para, 0, 1, "Verse one.");
			// NOTE: The verse number format is how TE represents verse numbers with bridges.
			// This method should not change anything but the verse bridge character.
			AddVerse(para, 0, "2" + '\u200F' + '\u200F' + "&" + '\u200F' + '\u200F' + '\u200F',
				"Verse with invalid bridge.");

			using (ScriptureProperties dlg = new ScriptureProperties(Cache, m_stylesheet, null, true, null))
			{
				ReflectionHelper.CallMethod(dlg, "UpdateVerseBridgesInParagraph", para,
				'\u200f' + "&" + '\u200f', '\u200f' + "@" + '\u200f');

				AssertEx.RunIsCorrect(para.Contents, 0, "1",
					ScrStyleNames.VerseNumber, Cache.DefaultVernWs);
				AssertEx.RunIsCorrect(para.Contents, 1, "Verse one.", null, Cache.DefaultVernWs);
				AssertEx.RunIsCorrect(para.Contents, 2, "2" + '\u200F' + '\u200F' + "@" + '\u200F' + '\u200F' + '\u200F',
					ScrStyleNames.VerseNumber, Cache.DefaultVernWs);
				AssertEx.RunIsCorrect(para.Contents, 3, "Verse with invalid bridge.", null, Cache.DefaultVernWs);
			}
		}
		#endregion
	}
}
