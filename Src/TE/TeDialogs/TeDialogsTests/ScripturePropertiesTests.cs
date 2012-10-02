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
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Test.TestUtils;
using System.Diagnostics;

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
			m_stylesheet.Init(Cache, m_scr.Hvo, (int)Scripture.ScriptureTags.kflidStyles);

			m_exodus = m_scrInMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_scrInMemoryCache.AddTitleToMockedBook(m_exodus.Hvo, "Exodus");

			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(m_exodus.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(section.Hvo, "Section Heading", ScrStyleNames.SectionHead);
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "Verse one. ", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "2", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "Verse two.", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "3", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "Verse three.", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "4", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "Verse four. ", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "5", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "Verse five.", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "6", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "Verse six. ", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "7", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "Verse seven.", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "8", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "Verse seven.", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "9", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "Verse seven.", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "10", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "Verse seven.", null);
			section.AdjustReferences();
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public override void Exit()
		{
			CheckDisposed();

			m_exodus = null;

			base.Exit();
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

			ITsString tss = ((IStTxtPara)((IScrSection)m_exodus.SectionsOS[0]).ContentOA.ParagraphsOS[0]).Contents.UnderlyingTsString;
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
			CheckDisposed();

			ScriptureProperties dlg = new ScriptureProperties(Cache, m_stylesheet, null, true);

			char bengaliZero = '\u09e6';
			char bengaliNine = (char)((int)bengaliZero + 9);

			// test arabic->bengali
			ScriptDigitConversionTest(bengaliZero, bengaliNine, dlg);
			// test bengali->arabic
			ScriptDigitConversionTest('0', '9', dlg);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the conversion of an empty ChapterNumber run (TE-5628).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ConvertChapterVerseNumbersTest_EmptyChapterNumber()
		{
			CheckDisposed();

			// Add paragraph with empty chapter number run.
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(m_exodus.SectionsOS[0].Hvo,
				ScrStyleNames.ChapterNumber);
			ITsPropsFactory propFact = TsPropsFactoryClass.Create();
			ITsTextProps ttpChapterNumber = propFact.MakeProps(ScrStyleNames.ChapterNumber,
				(int)InMemoryFdoCache.s_wsHvos.Fr, 0);
			ITsStrBldr bldr = para.Contents.UnderlyingTsString.GetBldr();
			bldr.SetProperties(0, 0, ttpChapterNumber);
			para.Contents.UnderlyingTsString = bldr.GetString();

			ScriptureProperties dlg = new ScriptureProperties(Cache, m_stylesheet, null, true);

			char bengaliZero = '\u09e6';
			char bengaliNine = (char)((int)bengaliZero + 9);

			// test arabic->bengali when there is a paragraph with an empty chapter number run.
			// It should complete without crashing.
			ScriptDigitConversionTest(bengaliZero, bengaliNine, dlg);
			// test bengali->arabic
			ScriptDigitConversionTest('0', '9', dlg);
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
			StTxtPara para = AddPara(m_exodus.SectionsOS[0]);
			AddVerse(para, 0, 5, "Verse five.");
			// Right-to-Left Bridge: 6-24
			// NOTE: The verse number format is how TE represents verse numbers with bridges.
			// This method should not change anything but the verse bridge character.
			AddVerse(para, 0, "6" + '\u200f' + '\u200f' + "@" + '\u200f' + '\u200f' + '\u200f' + "24",
				"Verse with valid R-to-L bridge.");

			ScriptureProperties dlg = new ScriptureProperties(Cache, m_stylesheet, null, true);
			ReflectionHelper.CallMethod(dlg, "UpdateVerseBridgesInParagraph", para,
				'\u200f' + "@" + '\u200f', '\u200f' + "&" + '\u200f');

			AssertEx.RunIsCorrect(para.Contents.UnderlyingTsString, 0, "5", ScrStyleNames.VerseNumber,
				Cache.DefaultVernWs);
			AssertEx.RunIsCorrect(para.Contents.UnderlyingTsString, 1, "Verse five.", ScrStyleNames.NormalParagraph,
				Cache.DefaultVernWs);
			AssertEx.RunIsCorrect(para.Contents.UnderlyingTsString, 2, "6" + '\u200f' + '\u200f' + "&" +
				'\u200f' + '\u200f' + '\u200f' + "24", ScrStyleNames.VerseNumber, Cache.DefaultVernWs);
			AssertEx.RunIsCorrect(para.Contents.UnderlyingTsString, 3, "Verse with valid R-to-L bridge.",
				ScrStyleNames.NormalParagraph, Cache.DefaultVernWs);
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
			StTxtPara para = AddPara(m_exodus.SectionsOS[0]);
			AddVerse(para, 0, 1, "Verse one.");
			// NOTE: The verse number format is how TE represents verse numbers with bridges.
			// This method should not change anything but the verse bridge character.
			AddVerse(para, 0, "2" + '\u200F' + '\u200F' + "&" + '\u200F' + '\u200F' + '\u200F',
				"Verse with invalid bridge.");

			ScriptureProperties dlg = new ScriptureProperties(Cache, m_stylesheet, null, true);
			ReflectionHelper.CallMethod(dlg, "UpdateVerseBridgesInParagraph", para,
				'\u200f' + "&" + '\u200f', '\u200f' + "@" + '\u200f');

			AssertEx.RunIsCorrect(para.Contents.UnderlyingTsString, 0, "1",
				ScrStyleNames.VerseNumber, Cache.DefaultVernWs);
			AssertEx.RunIsCorrect(para.Contents.UnderlyingTsString, 1, "Verse one.",
				ScrStyleNames.NormalParagraph, Cache.DefaultVernWs);
			AssertEx.RunIsCorrect(para.Contents.UnderlyingTsString, 2, "2" + '\u200F' + '\u200F' + "@" + '\u200F' + '\u200F' + '\u200F',
				ScrStyleNames.VerseNumber, Cache.DefaultVernWs);
			AssertEx.RunIsCorrect(para.Contents.UnderlyingTsString, 3, "Verse with invalid bridge.",
				ScrStyleNames.NormalParagraph, Cache.DefaultVernWs);
		}
		#endregion
	}
}
