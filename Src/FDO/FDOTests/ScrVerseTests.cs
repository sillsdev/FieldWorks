// Copyright (c) 2004-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ScrVerseTests.cs
// Responsibility: TE Team

using NUnit.Framework;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.CoreImpl;

namespace SIL.FieldWorks.FDO.FDOTests
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
			m_genesis = AddBookToMockedScripture(1, "Genesis");
		}
		#endregion

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests enumerating the ScrVerses in a paragraph when the verses are separated by
		/// spaces and the set is built with chapter numbers as separate ScrVerses
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void MoveNext_SpacesInVerses_ChapterNumberSeparate()
		{
			IScrSection sectionCur = AddSectionToMockedBook(m_genesis);
			// Create a section head for this section
			AddSectionHeadParaToSection(sectionCur, "My aching head!", ScrStyleNames.SectionHead);

			StTxtParaBldr paraBldr = new StTxtParaBldr(Cache);
				paraBldr.ParaStyleName = ScrStyleNames.NormalParagraph;
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
				IScrTxtPara para = (IScrTxtPara)paraBldr.CreateParagraph(sectionCur.ContentOA);

				using (ScrVerseSet verseSet = new ScrVerseSet(para))
				{
					// Iterate through the verses in the paragraph
					ScrVerse verse;

					Assert.IsTrue(verseSet.MoveNext());
				verse = verseSet.Current;
					Assert.AreEqual("1", verse.Text.Text);
					Assert.AreEqual(01001001, verse.StartRef);
					Assert.AreEqual(01001001, verse.EndRef);

					Assert.IsTrue(verseSet.MoveNext());
				verse = verseSet.Current;
					Assert.AreEqual("Verse One. ", verse.Text.Text);
					Assert.AreEqual(01001001, verse.StartRef);
					Assert.AreEqual(01001001, verse.EndRef);

					Assert.IsTrue(verseSet.MoveNext());
				verse = verseSet.Current;
					Assert.AreEqual("2 Verse Two. ", verse.Text.Text);
					Assert.AreEqual(01001002, verse.StartRef);
					Assert.AreEqual(01001002, verse.EndRef);

					Assert.IsTrue(verseSet.MoveNext());
				verse = verseSet.Current;
					Assert.AreEqual("3Verse Three.", verse.Text.Text);
					Assert.AreEqual(01001003, verse.StartRef);
					Assert.AreEqual(01001003, verse.EndRef);

					Assert.IsTrue(verseSet.MoveNext());
				verse = verseSet.Current;
					Assert.AreEqual("4     ", verse.Text.Text);
					Assert.AreEqual(01001004, verse.StartRef);
					Assert.AreEqual(01001004, verse.EndRef);

					Assert.IsFalse(verseSet.MoveNext());
				}
			}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests enumerating the ScrVerses in a paragraph when the set is built with chapter
		/// numbers combined with the following verses (if any)
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void MoveNext_ChapterNumberCombinedWithVerses()
		{
			IScrSection sectionCur = AddSectionToMockedBook(m_genesis);
			// Create a section head for this section
			AddSectionHeadParaToSection(sectionCur, "My aching head!", ScrStyleNames.SectionHead);

			StTxtParaBldr paraBldr = new StTxtParaBldr(Cache);
			paraBldr.ParaStyleName = ScrStyleNames.NormalParagraph;
			paraBldr.AppendRun("1", StyleUtils.CharStyleTextProps(ScrStyleNames.ChapterNumber,
				Cache.DefaultVernWs));
			paraBldr.AppendRun("Verse One. ",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.AppendRun("2", StyleUtils.CharStyleTextProps(ScrStyleNames.VerseNumber,
				Cache.DefaultVernWs));
			paraBldr.AppendRun(" Verse Two. ",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.AppendRun("2", StyleUtils.CharStyleTextProps(ScrStyleNames.ChapterNumber,
				Cache.DefaultVernWs));
			paraBldr.AppendRun("1", StyleUtils.CharStyleTextProps(ScrStyleNames.VerseNumber,
				Cache.DefaultVernWs));
			paraBldr.AppendRun("Verse with chapter and verse number.",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.AppendRun("4", StyleUtils.CharStyleTextProps(ScrStyleNames.VerseNumber,
				Cache.DefaultVernWs));
			paraBldr.AppendRun("     ",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.AppendRun("4", StyleUtils.CharStyleTextProps(ScrStyleNames.ChapterNumber,
				Cache.DefaultVernWs));
			paraBldr.AppendRun("1-3", StyleUtils.CharStyleTextProps(ScrStyleNames.VerseNumber,
				Cache.DefaultVernWs));
			paraBldr.AppendRun("Verses one thru three.",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			IScrTxtPara para = (IScrTxtPara)paraBldr.CreateParagraph(sectionCur.ContentOA);

			using (ScrVerseSet verseSet = new ScrVerseSet(para, false))
			{
			// Iterate through the verses in the paragraph
			ScrVerse verse;

			Assert.IsTrue(verseSet.MoveNext());
			verse = verseSet.Current;
			Assert.AreEqual("1Verse One. ", verse.Text.Text);
			Assert.AreEqual(01001001, verse.StartRef);
			Assert.AreEqual(01001001, verse.EndRef);

			Assert.IsTrue(verseSet.MoveNext());
			verse = verseSet.Current;
			Assert.AreEqual("2 Verse Two. ", verse.Text.Text);
			Assert.AreEqual(01001002, verse.StartRef);
			Assert.AreEqual(01001002, verse.EndRef);

			Assert.IsTrue(verseSet.MoveNext());
			verse = verseSet.Current;
			Assert.AreEqual("21Verse with chapter and verse number.", verse.Text.Text);
			Assert.AreEqual(01002001, verse.StartRef);
			Assert.AreEqual(01002001, verse.EndRef);

			Assert.IsTrue(verseSet.MoveNext());
			verse = verseSet.Current;
			Assert.AreEqual("4     ", verse.Text.Text);
			Assert.AreEqual(01002004, verse.StartRef);
			Assert.AreEqual(01002004, verse.EndRef);

			Assert.IsTrue(verseSet.MoveNext());
			verse = verseSet.Current;
			Assert.AreEqual("41-3Verses one thru three.", verse.Text.Text);
			Assert.AreEqual(01004001, verse.StartRef);
			Assert.AreEqual(01004003, verse.EndRef);

			Assert.IsFalse(verseSet.MoveNext());
		}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test that getting the first verse in a
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void MoveNext_ImplicitChapter1AndVerse1()
		{
			var sectionCur = Cache.ServiceLocator.GetInstance<IScrSectionFactory>().CreateScrSection(
				m_genesis, 0, "Verse One. ", StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs), false);

			var stPara = (IScrTxtPara)sectionCur.ContentOA.ParagraphsOS[0];

			using (var verseSet = new ScrVerseSet(stPara))
			{
				// Iterate through the verses in the paragraph
				Assert.IsTrue(verseSet.MoveNext());
				var verse = verseSet.Current;
				Assert.AreEqual("Verse One. ", verse.Text.Text);
				Assert.AreEqual(01001001, verse.StartRef);
				Assert.AreEqual(01001001, verse.EndRef);

				Assert.IsFalse(verseSet.MoveNext());
			}
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
			IScrSection sectionCur = Cache.ServiceLocator.GetInstance<IScrSectionFactory>().CreateSection(
				m_genesis, 0, false, true, true);

			IScrTxtPara contentPara = (IScrTxtPara)sectionCur.ContentOA.ParagraphsOS[0];
			ITsStrBldr strBldr = contentPara.Contents.GetBldr();
			strBldr.Replace(0, strBldr.Length, "A",
				StyleUtils.CharStyleTextProps(ScrStyleNames.ChapterNumber, Cache.DefaultVernWs));
			strBldr.Replace(strBldr.Length, strBldr.Length, "Verse One. ",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			strBldr.Replace(strBldr.Length, strBldr.Length, "2",
				StyleUtils.CharStyleTextProps(ScrStyleNames.VerseNumber, Cache.DefaultVernWs));
			strBldr.Replace(strBldr.Length, strBldr.Length, " Verse Two. ",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			contentPara.Contents = strBldr.GetString();

			using (ScrVerseSet verseSet = new ScrVerseSet(contentPara))
			{
				// Iterate through the verses in the paragraph
				ScrVerse verse;

				Assert.IsTrue(verseSet.MoveNext());
			verse = verseSet.Current;
				Assert.AreEqual("AVerse One. ", verse.Text.Text);
				Assert.AreEqual(01001001, verse.StartRef);
				Assert.AreEqual(01001001, verse.EndRef);

				Assert.IsTrue(verseSet.MoveNext());
			verse = verseSet.Current;
				Assert.AreEqual("2 Verse Two. ", verse.Text.Text);
				Assert.AreEqual(01001002, verse.StartRef);
				Assert.AreEqual(01001002, verse.EndRef);

				Assert.IsFalse(verseSet.MoveNext());
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Iterate through ScrVerses when the paragraph is an empty stanza break (TE-6184).
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void MoveNext_StanzaBreak()
		{
			IScrSection section = AddSectionToMockedBook(m_genesis);
			IScrTxtPara emptyPara = AddEmptyPara(section, ScrStyleNames.StanzaBreak);

			// Create and iterate through the verses in the StText.
			using (ScrVerseSet verseSet = new ScrVerseSet(emptyPara))
			{
				Assert.IsTrue(verseSet.MoveNext());
			ScrVerse verse = verseSet.Current;
				Assert.IsTrue(verse.Text == null || string.IsNullOrEmpty(verse.Text.Text));
				Assert.AreEqual(ScrStyleNames.StanzaBreak, verse.Para.StyleName);
				Assert.AreEqual(01001001, verse.StartRef);
				Assert.AreEqual(01001001, verse.EndRef);
				Assert.IsFalse(verseSet.MoveNext());
			}
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
			IScrSection section = AddSectionToMockedBook(m_genesis);
			IScrTxtPara emptyPara = AddEmptyPara(section, ScrStyleNames.SpeechLine1);

			// Create and iterate through the verses in the StText.
			using (ScrVerseSet verseSet = new ScrVerseSet(emptyPara))
			{
				Assert.IsFalse(verseSet.MoveNext(),
					"The iterator provided a ScrVerse for an empty para that wasn't a Stanza Break.");
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test comparing to ScrVerse objects
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void Equals()
		{
			var sectionCur = Cache.ServiceLocator.GetInstance<IScrSectionFactory>().CreateScrSection(
				m_genesis, 0, "Verse One. ", StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs), false);

			var stPara = (IScrTxtPara)sectionCur.ContentOA.ParagraphsOS[0];
			using (var verseSet1 = new ScrVerseSet(stPara))
			{
				using (var verseSet2 = new ScrVerseSet(stPara))
				{
					// Iterate through the verses in the paragraph
					Assert.IsTrue(verseSet1.MoveNext());
					Assert.IsTrue(verseSet2.MoveNext());
					Assert.AreEqual(verseSet1.Current, verseSet2.Current);
				}
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test that the implementation of ScrVerse.Equals follows the rules (see
		/// <see href="http://msdn.microsoft.com/en-us/library/336aedhh.aspx"/>
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void Equals_FollowsGuidelines()
		{
			var sectionCur = Cache.ServiceLocator.GetInstance<IScrSectionFactory>().CreateScrSection(
				m_genesis, 0, "Verse One. ", StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs), false);

			var stPara = (IScrTxtPara)sectionCur.ContentOA.ParagraphsOS[0];
			using (var verseSet1 = new ScrVerseSet(stPara))
			{
				using (var verseSet2 = new ScrVerseSet(stPara))
				{
					using (var verseSet3 = new ScrVerseSet(stPara))
					{
						// Iterate through the verses in the paragraph
						Assert.IsTrue(verseSet1.MoveNext());
						Assert.IsTrue(verseSet2.MoveNext());
						Assert.IsTrue(verseSet3.MoveNext());

						var x = verseSet1.Current;
						var y = verseSet2.Current;
						var z = verseSet3.Current;
						Assert.IsTrue(x.Equals(x));
						Assert.AreEqual(x.Equals(y), y.Equals(x));
						Assert.AreEqual(x.Equals(z), x.Equals(y) && y.Equals(z));
						Assert.AreEqual(x.Equals(y), x.Equals(y));
						Assert.IsFalse(x.Equals(null));
					}
				}
			}
		}
	}
}
