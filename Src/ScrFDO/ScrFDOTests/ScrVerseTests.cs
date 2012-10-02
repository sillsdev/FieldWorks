// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ScrVerseTests.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;

using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SILUBS.SharedScrUtils;

namespace SIL.FieldWorks.FDO.Scripture
{
	/// <summary>
	/// Summary description for ScrVerseTests.
	/// </summary>
	[TestFixture]
	public class ScrVerseTests: ScrInMemoryFdoTestBase
	{
		#region Member variables
		private IScrBook m_genesis;
		#endregion

		#region Test setup
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			m_genesis = m_scrInMemoryCache.AddBookToMockedScripture(1, "Genesis");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <remarks>This method is called after each test</remarks>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public override void Exit()
		{
			CheckDisposed();

			m_genesis = null;

			base.Exit();
		}
		#endregion

		/// -----------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void MoveNext_SpacesInVerses()
		{
			CheckDisposed();

			ScrSection sectionCur = new ScrSection();
			m_genesis.SectionsOS.Append(sectionCur);
			// Create a section head for this section
			sectionCur.HeadingOA = new StText();
			StTxtParaBldr paraBldr = new StTxtParaBldr(Cache);
			paraBldr.ParaProps = StyleUtils.ParaStyleTextProps(ScrStyleNames.SectionHead);
			paraBldr.AppendRun("My aching head!",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.CreateParagraph(sectionCur.HeadingOAHvo);
			sectionCur.ContentOA = new StText();

			paraBldr = new StTxtParaBldr(Cache);
			paraBldr.ParaProps = StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalParagraph);
			paraBldr.AppendRun("1", StyleUtils.CharStyleTextProps(ScrStyleNames.ChapterNumber,
				Cache.DefaultVernWs));
			paraBldr.AppendRun("Verse One. ",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.AppendRun("2", StyleUtils.CharStyleTextProps(ScrStyleNames.VerseNumber,
				Cache.DefaultVernWs));
			paraBldr.AppendRun(" Verse Two. ",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.AppendRun("3", StyleUtils.CharStyleTextProps(ScrStyleNames.VerseNumber,
				Cache.DefaultVernWs));
			paraBldr.AppendRun("Verse Three.",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.AppendRun("4", StyleUtils.CharStyleTextProps(ScrStyleNames.VerseNumber,
				Cache.DefaultVernWs));
			paraBldr.AppendRun("     ",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			StTxtPara para = paraBldr.CreateParagraph(sectionCur.ContentOA.Hvo);
			sectionCur.AdjustReferences();

			ScrTxtPara stPara = new ScrTxtPara(Cache, para.Hvo);
			ScrVerseSet verseSet = new ScrVerseSet(stPara);

			// Iterate through the verses in the paragraph
			ScrVerse verse;

			Assert.IsTrue(verseSet.MoveNext());
			verse = (ScrVerse)verseSet.Current;
			Assert.AreEqual("1", verse.Text.Text);
			Assert.AreEqual(01001001, verse.StartRef);
			Assert.AreEqual(01001001, verse.EndRef);

			Assert.IsTrue(verseSet.MoveNext());
			verse = (ScrVerse)verseSet.Current;
			Assert.AreEqual("Verse One. ", verse.Text.Text);
			Assert.AreEqual(01001001, verse.StartRef);
			Assert.AreEqual(01001001, verse.EndRef);

			Assert.IsTrue(verseSet.MoveNext());
			verse = (ScrVerse)verseSet.Current;
			Assert.AreEqual("2 Verse Two. ", verse.Text.Text);
			Assert.AreEqual(01001002, verse.StartRef);
			Assert.AreEqual(01001002, verse.EndRef);

			Assert.IsTrue(verseSet.MoveNext());
			verse = (ScrVerse)verseSet.Current;
			Assert.AreEqual("3Verse Three.", verse.Text.Text);
			Assert.AreEqual(01001003, verse.StartRef);
			Assert.AreEqual(01001003, verse.EndRef);

			Assert.IsTrue(verseSet.MoveNext());
			verse = (ScrVerse)verseSet.Current;
			Assert.AreEqual("4     ", verse.Text.Text);
			Assert.AreEqual(01001004, verse.StartRef);
			Assert.AreEqual(01001004, verse.EndRef);

			Assert.IsFalse(verseSet.MoveNext());
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test that getting the first verse in a
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void MoveNext_ImplicitChapter1AndVerse1()
		{
			CheckDisposed();

			IScrSection sectionCur = ScrSection.CreateSectionWithEmptyParas(m_genesis, 0, false);
			TsStringAccessor heading = ((StTxtPara)sectionCur.HeadingOA.ParagraphsOS[0]).Contents;
			ITsStrBldr strBldr = heading.UnderlyingTsString.GetBldr();
			strBldr.Replace(0, strBldr.Length, "My aching head!", null);
			heading.UnderlyingTsString = strBldr.GetString();

			TsStringAccessor content = ((StTxtPara)sectionCur.ContentOA.ParagraphsOS[0]).Contents;
			strBldr = content.UnderlyingTsString.GetBldr();
			strBldr.Replace(0, strBldr.Length, "Verse One. ", null);
			content.UnderlyingTsString = strBldr.GetString();

			sectionCur.AdjustReferences();

			ScrTxtPara stPara = new ScrTxtPara(Cache, sectionCur.ContentOA.ParagraphsOS.HvoArray[0]);
			ScrVerseSet verseSet = new ScrVerseSet(stPara);

			// Iterate through the verses in the paragraph
			ScrVerse verse;

			Assert.IsTrue(verseSet.MoveNext());
			verse = (ScrVerse)verseSet.Current;
			Assert.AreEqual("Verse One. ", verse.Text.Text);
			Assert.AreEqual(01001001, verse.StartRef);
			Assert.AreEqual(01001001, verse.EndRef);

			Assert.IsFalse(verseSet.MoveNext());
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test to make sure non-numeric text marked with chapter style doesn't cause an
		/// infinite loop. Jira # is TE-5449.
		/// </summary>
		/// <remarks>This data condition is hopefully prevented (or made much less likely by
		/// the fix to TE-5448), but it could be found in pre-existing data.</remarks>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void MoveNext_NonNumericChapter()
		{
			CheckDisposed();

			IScrSection sectionCur = ScrSection.CreateSectionWithEmptyParas(m_genesis, 0, false);
			TsStringAccessor heading = ((StTxtPara)sectionCur.HeadingOA.ParagraphsOS[0]).Contents;
			ITsStrBldr strBldr = heading.UnderlyingTsString.GetBldr();
			strBldr.Replace(0, strBldr.Length, "My aching head!", null);
			heading.UnderlyingTsString = strBldr.GetString();

			TsStringAccessor content = ((StTxtPara)sectionCur.ContentOA.ParagraphsOS[0]).Contents;
			strBldr = content.UnderlyingTsString.GetBldr();
			strBldr.Replace(0, strBldr.Length, "A",
				StyleUtils.CharStyleTextProps(ScrStyleNames.ChapterNumber, Cache.DefaultVernWs));
			strBldr.Replace(strBldr.Length, strBldr.Length, "Verse One. ",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			strBldr.Replace(strBldr.Length, strBldr.Length, "2",
				StyleUtils.CharStyleTextProps(ScrStyleNames.VerseNumber, Cache.DefaultVernWs));
			strBldr.Replace(strBldr.Length, strBldr.Length, " Verse Two. ",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			content.UnderlyingTsString = strBldr.GetString();

			sectionCur.AdjustReferences();

			ScrTxtPara stPara = new ScrTxtPara(Cache, sectionCur.ContentOA.ParagraphsOS.HvoArray[0]);

			ScrVerseSet verseSet = new ScrVerseSet(stPara);

			// Iterate through the verses in the paragraph
			ScrVerse verse;

			Assert.IsTrue(verseSet.MoveNext());
			verse = (ScrVerse)verseSet.Current;
			Assert.AreEqual("AVerse One. ", verse.Text.Text);
			Assert.AreEqual(01001001, verse.StartRef);
			Assert.AreEqual(01001001, verse.EndRef);

			Assert.IsTrue(verseSet.MoveNext());
			verse = (ScrVerse)verseSet.Current;
			Assert.AreEqual("2 Verse Two. ", verse.Text.Text);
			Assert.AreEqual(01001002, verse.StartRef);
			Assert.AreEqual(01001002, verse.EndRef);

			Assert.IsFalse(verseSet.MoveNext());
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Iterate through ScrVerses when the paragraph is an empty stanza break (TE-6184).
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void MoveNext_StanzaBreak()
		{
			CheckDisposed();
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(m_genesis.Hvo);
			StTxtPara emptyPara = AddEmptyPara(section, ScrStyleNames.StanzaBreak);
			section.AdjustReferences();

			// Create and iterate through the verses in the StText.
			ScrTxtPara emptyScrPara = new ScrTxtPara(m_inMemoryCache.Cache, emptyPara.Hvo);
			ScrVerseSet verseSet = new ScrVerseSet(emptyScrPara);

			Assert.IsTrue(verseSet.MoveNext());
			VerifyScrVerse((ScrVerse)verseSet.Current, m_inMemoryCache.Cache, null,
				ScrStyleNames.StanzaBreak, 01001001, 01001001);
			Assert.IsFalse(verseSet.MoveNext());
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Iterate through ScrVerses when the paragraph is empty (and not a stanza break)
		/// (TE-6184).
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void MoveNext_EmptyPara()
		{
			CheckDisposed();
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(m_genesis.Hvo);
			StTxtPara emptyPara = AddEmptyPara(section, ScrStyleNames.SpeechLine1);
			section.AdjustReferences();

			// Create and iterate through the verses in the StText.
			ScrTxtPara emptyScrPara = new ScrTxtPara(m_inMemoryCache.Cache, emptyPara.Hvo);
			ScrVerseSet verseSet = new ScrVerseSet(emptyScrPara);

			Assert.IsFalse(verseSet.MoveNext(),
				"The iterator provided a ScrVerse for an empty para that wasn't a Stanza Break.");
		}

		/// -----------------------------------------------------------------------------------
		///<summary>
		/// Verify the specified ScrVerse
		///</summary>
		///<param name="verse">specified ScrVerse</param>
		///<param name="cache">database</param>
		///<param name="verseText">expected text within the ScrVerse</param>
		///<param name="styleName">expected stylename for the ScrVerse paragraph</param>
		///<param name="startRef">expected starting reference</param>
		///<param name="endRef">expected ending reference</param>
		/// -----------------------------------------------------------------------------------
		public static void VerifyScrVerse(ScrVerse verse, FdoCache cache, string verseText,
						string styleName, BCVRef startRef, BCVRef endRef)
		{
			ScrTxtPara versePara = new ScrTxtPara(cache, verse.HvoPara);
			if (string.IsNullOrEmpty(verseText))
				Assert.IsTrue(verse.Text == null || string.IsNullOrEmpty(verse.Text.Text));
			else
				Assert.AreEqual(verseText, verse.Text.Text);
			Assert.AreEqual(styleName, ScrStyleNames.GetStyleName(versePara.Hvo, cache));
			Assert.AreEqual(startRef, verse.StartRef);
			Assert.AreEqual(endRef, verse.EndRef);
		}
	}
}
