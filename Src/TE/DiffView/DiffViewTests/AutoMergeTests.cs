// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2004' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: AutoMergeTests.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;

using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.Utils;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Dummy class to ensure that MakeBackupIfNeeded gets called when needed.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DummyBookVersionAgent : IBookVersionAgent
	{
		public delegate void MakeBackupHandler(BookMerger bookMerger);
		public event MakeBackupHandler MakeBackupCalled;
		internal int m_NumberOfCallsToMakeBackupIfNeeded = 0;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Just note that this method got called.
		/// </summary>
		/// <param name="bookMerger">The book merger.</param>
		/// ------------------------------------------------------------------------------------
		public void MakeBackupIfNeeded(BookMerger bookMerger)
		{
			Assert.IsNotNull(bookMerger);
			if (MakeBackupCalled != null)
				MakeBackupCalled(bookMerger);
			m_NumberOfCallsToMakeBackupIfNeeded++;
		}
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for the AutoMerge method of the <see cref="BookMerger"/> class. DetectDifferences
	/// calls AutoMerge if the AttemptAutoMerge property is set to <c>true</c>.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class AutoMergeTests : ScrInMemoryFdoTestBase
	{
		#region Member variables
		private IScrBook m_genesis;
		private IScrBook m_genesisRevision;
		private DummyBookMerger m_bookMerger;
		private DummyBookVersionAgent m_bookVersionAgent;
		#endregion

		#region Setup
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create Genesis and a revision of Genesis, and create the BookMerger.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public override void TestSetup()
		{
			base.TestSetup();

			// init the DummyBookMerger
			Debug.Assert(m_bookMerger == null, "m_bookMerger is not null.");
			m_bookMerger = new DummyBookMerger(Cache, null, m_genesisRevision);
			m_bookMerger.AttemptAutoMerge = true;
			m_bookVersionAgent = new DummyBookVersionAgent();
			m_bookMerger.BookVersionAgent = m_bookVersionAgent;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shuts down the cache.
		/// </summary>
		/// <remarks>This method is called after each test</remarks>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public override void TestTearDown()
		{
			m_bookMerger.Dispose();
			m_bookMerger = null;
			m_genesis = null;
			m_genesisRevision = null;

			base.TestTearDown();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overridden to only create a book with no content, heading, title, etc.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			m_genesis = AddBookToMockedScripture(1, "Genesis");
			AddTitleToMockedBook(m_genesis, "Genesis");
			m_genesisRevision = AddArchiveBookToMockedScripture(1, "Genesis");
			AddTitleToMockedBook(m_genesisRevision, "Genesis");
		}
		#endregion

		#region AutoMerge Succeeded Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that AutoMerge successfully merges in a newly imported Scripture section that
		/// is before any existing section.
		///
		/// revision        current
		/// 1:1-31
		///                 2:1-25
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void NewSectionAtStartOfBook()
		{
			// Build the "current" section
			IScrSection origSection1Curr = CreateSection(m_genesis, "My Second Section");
			IScrTxtPara para1Curr = AddParaToMockedSectionContent(origSection1Curr, ScrStyleNames.NormalParagraph);
			AddVerse(para1Curr, 2, 1, "There was a vast array (how geeky). ");
			AddVerse(para1Curr, 0, 25, "They were naked, but no biggie.");

			// Build the "revision" sections
			IScrSection section1Rev = CreateSection(m_genesisRevision, "My First Section");
			IScrTxtPara para1Rev = AddParaToMockedSectionContent(section1Rev, ScrStyleNames.NormalParagraph);
			AddVerse(para1Rev, 1, 1, "In the beginning God made everything. ");
			AddVerse(para1Rev, 0, 31, "It was all good.");

			// Detect differences
			m_bookMerger.DetectDifferences(null);
			Assert.IsTrue(m_bookMerger.AutoMerged);
			Assert.AreEqual(1, m_bookVersionAgent.m_NumberOfCallsToMakeBackupIfNeeded);

			// In real life, the import code would set the filter on before calling
			// DetectDifferences, but for the test we do it here to prove that the
			// auto-merge functionality is not dependent on the filter.
			m_bookMerger.UseFilteredDiffList = true;
			Assert.AreEqual(0, m_bookMerger.Differences.Count);

			// The current should now contain the contents of the revision.
			Assert.AreEqual(2, m_genesis.SectionsOS.Count);
			IScrSection newSection1Curr = m_genesis.SectionsOS[0];
			Assert.AreEqual("11In the beginning God made everything. 31It was all good.",
				((IScrTxtPara)newSection1Curr.ContentOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual(origSection1Curr, m_genesis.SectionsOS[1]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that AutoMerge successfully merges in a newly imported Intro section if the
		/// current version has no intro sections at all.
		///
		/// revision        current
		/// Intro
		///                 2:1-25
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void NewIntroSectionAtStartOfBook()
		{
			// Build the "current" section
			IScrSection origSection1Curr = CreateSection(m_genesis, "My Second Section");
			IScrTxtPara para1Curr = AddParaToMockedSectionContent(origSection1Curr, ScrStyleNames.NormalParagraph);
			AddVerse(para1Curr, 2, 1, "There was a vast array (how geeky). ");
			AddVerse(para1Curr, 0, 25, "They were naked, but no biggie.");

			// Build the "revision" sections
			IScrSection section1Rev = CreateIntroSection(m_genesisRevision, "All About Genesis");
			IScrTxtPara para1Rev = AddParaToMockedSectionContent(section1Rev, ScrStyleNames.IntroParagraph);

			// Detect differences
			m_bookMerger.DetectDifferences(null);
			Assert.IsTrue(m_bookMerger.AutoMerged);
			Assert.AreEqual(1, m_bookVersionAgent.m_NumberOfCallsToMakeBackupIfNeeded);

			// In real life, the import code would set the filter on before calling
			// DetectDifferences, but for the test we do it here to prove that the
			// auto-merge functionality is not dependent on the filter.
			m_bookMerger.UseFilteredDiffList = true;
			Assert.AreEqual(0, m_bookMerger.Differences.Count);

			// The current should now contain the contents of the revision.
			Assert.AreEqual(2, m_genesis.SectionsOS.Count);
			IScrSection newSection1Curr = m_genesis.SectionsOS[0];
			Assert.AreEqual("All About Genesis",
				((IScrTxtPara)newSection1Curr.HeadingOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual(origSection1Curr, m_genesis.SectionsOS[1]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that AutoMerge successfully merges in a newly imported Scripture section if the
		/// current version only an intro section.
		///
		/// revision        current
		///                 Intro
		/// 2:1-25
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void NewScrSectionFollowingIntro()
		{
			// Build the "current" section
			IScrSection origSection1Curr = CreateIntroSection(m_genesis, "All About Genesis");
			IScrTxtPara paraCurr = AddParaToMockedSectionContent(origSection1Curr, ScrStyleNames.IntroParagraph);

			// Build the "revision" sections
			IScrSection section1Rev = CreateSection(m_genesisRevision, "Chapter Two");
			IScrTxtPara paraRev = AddParaToMockedSectionContent(section1Rev, ScrStyleNames.NormalParagraph);
			AddVerse(paraRev, 2, 1, "There was a vast array (how geeky). ");
			AddVerse(paraRev, 0, 25, "They were naked, but no biggie.");

			// Detect differences
			m_bookMerger.UseFilteredDiffList = true;
			m_bookMerger.DetectDifferences(null);
			Assert.IsTrue(m_bookMerger.AutoMerged);
			Assert.AreEqual(1, m_bookVersionAgent.m_NumberOfCallsToMakeBackupIfNeeded);
			Assert.AreEqual(0, m_bookMerger.Differences.Count);

			// The current should now contain the contents of the revision.
			Assert.AreEqual(2, m_genesis.SectionsOS.Count);
			Assert.AreEqual(origSection1Curr, m_genesis.SectionsOS[0]);
			IScrSection newSection2Curr = m_genesis.SectionsOS[1];
			Assert.AreEqual("Chapter Two",
				((IScrTxtPara)newSection2Curr.HeadingOA.ParagraphsOS[0]).Contents.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that AutoMerge doesn't crash when the book merger agent is null. (This happens
		/// when the automerge is done as part of a partial overwrite from the Saved and
		/// Imported Versions dialog box.)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AutoMergeWithoutBookMergerAgent()
		{
			// Build the "current" section
			IScrSection origSection1Curr = CreateIntroSection(m_genesis, "All About Genesis");
			IScrTxtPara paraCurr = AddParaToMockedSectionContent(origSection1Curr, ScrStyleNames.IntroParagraph);

			// Build the "revision" sections
			IScrSection section1Rev = CreateSection(m_genesisRevision, "Chapter Two");
			IScrTxtPara paraRev = AddParaToMockedSectionContent(section1Rev, ScrStyleNames.NormalParagraph);
			AddVerse(paraRev, 2, 1, "There was a vast array (how geeky). ");
			AddVerse(paraRev, 0, 25, "They were naked, but no biggie.");

			// Detect differences
			m_bookMerger.UseFilteredDiffList = true;
			m_bookMerger.BookVersionAgent = null;
			m_bookMerger.DetectDifferences(null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that AutoMerge successfully merges in a newly imported Scripture section that
		/// is after any existing section.
		///
		/// revision        current
		///                 1:1-31
		/// 2:1-25
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void NewSectionAtEndOfBook()
		{
			// Build the "current" section
			IScrSection section1Curr = CreateSection(m_genesis, "My First Section");
			IScrTxtPara para1Curr = AddParaToMockedSectionContent(section1Curr, ScrStyleNames.NormalParagraph);
			AddVerse(para1Curr, 1, 1, "In the beginning God made everything. ");
			AddVerse(para1Curr, 0, 31, "It was all good.");

			// Build the "revision" sections
			IScrSection section1Rev = CreateSection(m_genesisRevision, "My Second Section");
			IScrTxtPara para1Rev = AddParaToMockedSectionContent(section1Rev, ScrStyleNames.NormalParagraph);
			AddVerse(para1Rev, 2, 1, "There was a vast array (how geeky). ");
			AddVerse(para1Rev, 0, 25, "They were naked, but no biggie.");

			// Detect differences
			m_bookMerger.DetectDifferences(null);
			Assert.IsTrue(m_bookMerger.AutoMerged);
			Assert.AreEqual(1, m_bookVersionAgent.m_NumberOfCallsToMakeBackupIfNeeded);
			// In real life, the import code would set the filter on before calling
			// DetectDifferences, but for the test we do it here to prove that the
			// auto-merge functionality is not dependent on the filter.
			m_bookMerger.UseFilteredDiffList = true;
			Assert.AreEqual(0, m_bookMerger.Differences.Count);

			// The current should now contain the contents of the revision.
			Assert.AreEqual(2, m_genesis.SectionsOS.Count);
			Assert.AreEqual(section1Curr, m_genesis.SectionsOS[0]);
			IScrSection newSection2Curr = m_genesis.SectionsOS[1];
			Assert.AreEqual("21There was a vast array (how geeky). 25They were naked, but no biggie.",
				((IScrTxtPara)newSection2Curr.ContentOA.ParagraphsOS[0]).Contents.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that AutoMerge successfully merges in a newly imported Scripture section that
		/// is after any existing section when the imported version does not have a title but
		/// the current version does.
		///
		/// revision        current
		///                 Title
		///                 1:1-31
		/// 2:1-25
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void NewSectionAtEndOfBook_NoTitleInImportedVersion()
		{
			// Build the "current" section
			IScrSection section1Curr = CreateSection(m_genesis, "My First Section");
			IScrTxtPara para1Curr = AddParaToMockedSectionContent(section1Curr, ScrStyleNames.NormalParagraph);
			AddVerse(para1Curr, 1, 1, "In the beginning God made everything. ");
			AddVerse(para1Curr, 0, 31, "It was all good.");

			// Build the "revision" sections
			m_genesisRevision.TitleOA[0].Contents = Cache.TsStrFactory.MakeString(string.Empty,
				Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle);
			IScrSection section1Rev = CreateSection(m_genesisRevision, "My Second Section");
			IScrTxtPara para1Rev = AddParaToMockedSectionContent(section1Rev, ScrStyleNames.NormalParagraph);
			AddVerse(para1Rev, 2, 1, "There was a vast array (how geeky). ");
			AddVerse(para1Rev, 0, 25, "They were naked, but no biggie.");

			// Detect differences
			m_bookMerger.DetectDifferences(null);
			Assert.IsTrue(m_bookMerger.AutoMerged);
			Assert.AreEqual(1, m_bookVersionAgent.m_NumberOfCallsToMakeBackupIfNeeded);

			// In real life, the import code would set the filter on before calling
			// DetectDifferences, but for the test we do it here to prove that the
			// auto-merge functionality is not dependent on the filter.
			m_bookMerger.UseFilteredDiffList = true;
			Assert.AreEqual(1, m_bookMerger.Differences.Count, "Should still be a difference (which we can ignore) for the book title.");

			// The current should now contain the contents of the revision.
			Assert.AreEqual(2, m_genesis.SectionsOS.Count);
			Assert.AreEqual(section1Curr, m_genesis.SectionsOS[0]);
			IScrSection newSection2Curr = m_genesis.SectionsOS[1];
			Assert.AreEqual("21There was a vast array (how geeky). 25They were naked, but no biggie.",
				((IScrTxtPara)newSection2Curr.ContentOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual("Genesis", ((IScrTxtPara)m_genesis.TitleOA.ParagraphsOS[0]).Contents.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that AutoMerge successfully merges in a newly imported title and a Scripture
		/// section that is before any existing section when the current version does not have a
		/// title.
		///
		/// revision        current
		/// Title
		/// 1:1-31
		///                 2:1-25
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void NewSectionAtStartOfBook_NoTitleInCurrentVersion()
		{
			// Build the "current" section
			m_genesis.TitleOA[0].Contents = Cache.TsStrFactory.MakeString(string.Empty,
				Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle);

			IScrSection origSection1Curr = CreateSection(m_genesis, "My Second Section");
			IScrTxtPara para1Curr = AddParaToMockedSectionContent(origSection1Curr, ScrStyleNames.NormalParagraph);
			AddVerse(para1Curr, 2, 1, "There was a vast array (how geeky). ");
			AddVerse(para1Curr, 0, 25, "They were naked, but no biggie.");

			// Build the "revision" sections
			IScrSection section1Rev = CreateSection(m_genesisRevision, "My First Section");
			IScrTxtPara para1Rev = AddParaToMockedSectionContent(section1Rev, ScrStyleNames.NormalParagraph);
			AddVerse(para1Rev, 1, 1, "In the beginning God made everything. ");
			AddVerse(para1Rev, 0, 31, "It was all good.");

			// Detect differences
			m_bookMerger.DetectDifferences(null);
			Assert.IsTrue(m_bookMerger.AutoMerged);
			Assert.AreEqual(1, m_bookVersionAgent.m_NumberOfCallsToMakeBackupIfNeeded);

			// In real life, the import code would set the filter on before calling
			// DetectDifferences, but for the test we do it here to prove that the
			// auto-merge functionality is not dependent on the filter.
			m_bookMerger.UseFilteredDiffList = true;
			Assert.AreEqual(0, m_bookMerger.Differences.Count);

			// The current should now contain the contents of the revision.
			Assert.AreEqual(2, m_genesis.SectionsOS.Count);
			IScrSection newSection1Curr = m_genesis.SectionsOS[0];
			Assert.AreEqual("11In the beginning God made everything. 31It was all good.",
				((IScrTxtPara)newSection1Curr.ContentOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual(origSection1Curr, m_genesis.SectionsOS[1]);
			Assert.AreEqual("Genesis", ((IScrTxtPara)m_genesis.TitleOA.ParagraphsOS[0]).Contents.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that AutoMerge successfully merges in newly imported Scripture sections that
		/// are interspersed with but not overlapping existing sections. Each section in both
		/// the current and the imported version starts at a chapter boundary so no chapter
		/// numbers need to be removed to avoid repeated chapter numbers.
		///
		/// revision        current
		///                 1:1-31
		/// 2:1-25
		///                 3:1-13
		/// 6:1-8:22
		///                 11:1-9
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void NewSectionsThroughoutBook_DistinctChapterNumbers()
		{
			// Build the "current" section
			IScrSection origSection1Curr = CreateSection(m_genesis, "Creation");
			IScrTxtPara paraCurr = AddParaToMockedSectionContent(origSection1Curr, ScrStyleNames.NormalParagraph);
			AddVerse(paraCurr, 1, 1, "In the beginning God made everything. ");
			AddVerse(paraCurr, 0, 31, "It was all good.");

			IScrSection origSection2Curr = CreateSection(m_genesis, "The Fall of Man");
			paraCurr = AddParaToMockedSectionContent(origSection2Curr, ScrStyleNames.NormalParagraph);
			AddVerse(paraCurr, 3, 1, "The smart, mean, nasty snake came a pestering poor little Eve. ");
			AddVerse(paraCurr, 0, 13, "God asked Eve what she did and she pretty much blamed the snake.");

			IScrSection origSection3Curr = CreateSection(m_genesis, "The World's First Skyscraper");
			paraCurr = AddParaToMockedSectionContent(origSection3Curr, ScrStyleNames.NormalParagraph);
			AddVerse(paraCurr, 11, 1, "There was one world-wide language. ");
			AddVerse(paraCurr, 0, 9, "So they called it Babel and that's why Wycliffe had to come into existence.");

			// Build the "revision" sections
			IScrSection section1Rev = CreateSection(m_genesisRevision, "God and Adam Hang Out");
			IScrTxtPara paraRev = AddParaToMockedSectionContent(section1Rev, ScrStyleNames.NormalParagraph);
			AddVerse(paraRev, 2, 1, "There was a vast array (how geeky). ");
			AddVerse(paraRev, 0, 25, "They were naked, but no biggie.");

			IScrSection section2Rev = CreateSection(m_genesisRevision, "Can You Say Thunderstorm?!");
			paraRev = AddParaToMockedSectionContent(section2Rev, ScrStyleNames.NormalParagraph);
			AddVerse(paraRev, 6, 1, "Men++ led to Daughters++");
			paraRev = AddParaToMockedSectionContent(section2Rev, ScrStyleNames.NormalParagraph);
			AddVerse(paraRev, 7, 1, "Noah, you're a good guy, so you can get into the boat.");
			paraRev = AddParaToMockedSectionContent(section2Rev, ScrStyleNames.NormalParagraph);
			AddVerse(paraRev, 8, 1, "God didn't forget Noah or the cute little puppy dogs.");
			paraRev = AddParaToMockedSectionContent(section2Rev, ScrStyleNames.Line1);
			AddVerse(paraRev, 0, 22, "Now you get to have summer and winter and stuff.");

			// Detect differences
			m_bookMerger.DetectDifferences(null);
			Assert.IsTrue(m_bookMerger.AutoMerged);
			Assert.AreEqual(1, m_bookVersionAgent.m_NumberOfCallsToMakeBackupIfNeeded);

			// In real life, the import code would set the filter on before calling
			// DetectDifferences, but for the test we do it here to prove that the
			// auto-merge functionality is not dependent on the filter.
			m_bookMerger.UseFilteredDiffList = true;
			Assert.AreEqual(0, m_bookMerger.Differences.Count);

			// The current should now contain the contents of the revision.
			Assert.AreEqual(5, m_genesis.SectionsOS.Count);
			Assert.AreEqual(origSection1Curr, m_genesis.SectionsOS[0]);
			IScrSection newSection2Curr = m_genesis.SectionsOS[1];
			Assert.AreEqual("21There was a vast array (how geeky). 25They were naked, but no biggie.",
				((IScrTxtPara)newSection2Curr.ContentOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual(origSection2Curr, m_genesis.SectionsOS[2]);
			IScrSection newSection4Curr = m_genesis.SectionsOS[3];
			Assert.AreEqual("61Men++ led to Daughters++",
				((IScrTxtPara)newSection4Curr.ContentOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual("71Noah, you're a good guy, so you can get into the boat.",
				((IScrTxtPara)newSection4Curr.ContentOA.ParagraphsOS[1]).Contents.Text);
			Assert.AreEqual("81God didn't forget Noah or the cute little puppy dogs.",
				((IScrTxtPara)newSection4Curr.ContentOA.ParagraphsOS[2]).Contents.Text);
			Assert.AreEqual("22Now you get to have summer and winter and stuff.",
				((IScrTxtPara)newSection4Curr.ContentOA.ParagraphsOS[3]).Contents.Text);
			Assert.AreEqual(origSection3Curr, m_genesis.SectionsOS[4]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that AutoMerge successfully merges in newly imported Scripture sections that
		/// are interspersed with but not overlapping existing sections.
		///
		/// revision        current
		/// 1:1-31
		///                 2:1-17
		/// 2:18-3:6
		///                 3:14-15
		/// 11:1
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void NewSectionsThroughoutBook_ChapterNumbersRepeated()
		{
			// Build the "current" section
			IScrSection origSection1Curr = CreateSection(m_genesis, "God and Adam Hang Out");
			IScrTxtPara paraCurr = AddParaToMockedSectionContent(origSection1Curr, ScrStyleNames.NormalParagraph);
			AddVerse(paraCurr, 2, 1, "There was a vast array (how geeky). ");
			AddVerse(paraCurr, 0, 17, "Eat the forbidden fruit and you can kiss this popsicle stand adios!");

			IScrSection origSection2Curr = CreateSection(m_genesis, "Curse of the Snake");
			paraCurr = AddParaToMockedSectionContent(origSection2Curr, ScrStyleNames.NormalParagraph);
			AddVerse(paraCurr, 3, 14, "God told the snake:");
			paraCurr = AddParaToMockedSectionContent(origSection2Curr, ScrStyleNames.Line1);
			paraCurr.Contents = Cache.TsStrFactory.MakeString("Dude, you are toast!",
				Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle);
			paraCurr = AddParaToMockedSectionContent(origSection2Curr, ScrStyleNames.Line1);
			AddVerse(paraCurr, 0, 15, "Jesus is gonna crush your head!");

			// Build the "revision" sections
			IScrSection section1Rev = CreateSection(m_genesisRevision, "Creation");
			IScrTxtPara paraRev = AddParaToMockedSectionContent(section1Rev, ScrStyleNames.NormalParagraph);
			AddVerse(paraRev, 1, 1, "In the beginning God made everything. ");
			AddVerse(paraRev, 0, 31, "It was all good.");

			IScrSection section2Rev = CreateSection(m_genesisRevision, "Eve Created and Tempted");
			paraRev = AddParaToMockedSectionContent(section2Rev, ScrStyleNames.NormalParagraph);
			AddVerse(paraRev, 2, 18, "Poor Adam! All alone with no wife. ");
			AddVerse(paraRev, 3, 6, "Wow! Nummy fruit! Adam, you want some?");

			IScrSection section3Rev = CreateSection(m_genesisRevision, "The World's First Skyscraper");
			paraRev = AddParaToMockedSectionContent(section3Rev, ScrStyleNames.NormalParagraph);
			AddVerse(paraRev, 11, 1, "There was one world-wide language and only one verse in this chapter to boot.");

			// Detect differences
			m_bookMerger.DetectDifferences(null);
			Assert.IsTrue(m_bookMerger.AutoMerged);
			Assert.AreEqual(1, m_bookVersionAgent.m_NumberOfCallsToMakeBackupIfNeeded);

			// In real life, the import code would set the filter on before calling
			// DetectDifferences, but for the test we do it here to prove that the
			// auto-merge functionality is not dependent on the filter.
			m_bookMerger.UseFilteredDiffList = true;
			Assert.AreEqual(0, m_bookMerger.Differences.Count);

			// The current should now contain the contents of the revision.
			Assert.AreEqual(5, m_genesis.SectionsOS.Count);
			IScrSection newSection1Curr = m_genesis.SectionsOS[0];
			Assert.AreEqual("11In the beginning God made everything. 31It was all good.",
				((IScrTxtPara)newSection1Curr.ContentOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual(origSection1Curr, m_genesis.SectionsOS[1]);
			IScrSection newSection3Curr = m_genesis.SectionsOS[2];
			Assert.AreEqual("218Poor Adam! All alone with no wife. 36Wow! Nummy fruit! Adam, you want some?",
				((IScrTxtPara)newSection3Curr.ContentOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual(origSection2Curr, m_genesis.SectionsOS[3]);
			IScrSection newSection5Curr = m_genesis.SectionsOS[4];
			Assert.AreEqual("111There was one world-wide language and only one verse in this chapter to boot.",
				((IScrTxtPara)newSection5Curr.ContentOA.ParagraphsOS[0]).Contents.Text);
		}
		#endregion

		#region AutoMerge Failed Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that AutoMerge fails to merge an imported Scripture section at the start of
		/// a book which overlaps an existing section, even if the overlapping content is
		/// identical.
		///
		/// revision        current
		/// 1:1-2:1
		///                 2:1-25
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void OverlappingSectionAtStartOfBook_NoTextDifference()
		{
			// Build the "current" section
			IScrSection origSection1Curr = CreateSection(m_genesis, "My Second Section");
			IScrTxtPara paraCurr = AddParaToMockedSectionContent(origSection1Curr, ScrStyleNames.NormalParagraph);
			AddVerse(paraCurr, 2, 1, "There was a vast array (how geeky). ");
			paraCurr = AddParaToMockedSectionContent(origSection1Curr, ScrStyleNames.NormalParagraph);
			AddVerse(paraCurr, 0, 25, "They were naked, but no biggie.");

			// Build the "revision" sections
			IScrSection section1Rev = CreateSection(m_genesisRevision, "My First Section");
			IScrTxtPara para1Rev = AddParaToMockedSectionContent(section1Rev, ScrStyleNames.NormalParagraph);
			AddVerse(para1Rev, 1, 1, "In the beginning God made everything. ");
			AddVerse(para1Rev, 0, 31, "It was all good.");
			IScrTxtPara para2Rev = AddParaToMockedSectionContent(section1Rev, ScrStyleNames.NormalParagraph);
			AddVerse(para1Rev, 2, 1, "There was a vast array (how geeky). ");

			// Detect differences
			m_bookMerger.DetectDifferences(null);
			Assert.IsFalse(m_bookMerger.AutoMerged);
			Assert.IsTrue(m_bookMerger.Differences.Count > 0);
			Assert.AreEqual(0, m_bookMerger.ReviewedDiffs.Count);

			// The current version should not have changed.
			Assert.AreEqual(1, m_genesis.SectionsOS.Count);
			Assert.AreEqual(origSection1Curr, m_genesis.SectionsOS[0]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that AutoMerge fails to merge an imported Scripture section at the start of
		/// a book which overlaps an existing section.
		///
		/// revision        current
		/// 1:1-2:1
		///                 2:1-25
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void OverlappingSectionAtStartOfBook_WithVerseTextDifference()
		{
			// Build the "current" section
			IScrSection origSection1Curr = CreateSection(m_genesis, "My Second Section");
			IScrTxtPara para1Curr = AddParaToMockedSectionContent(origSection1Curr, ScrStyleNames.NormalParagraph);
			AddVerse(para1Curr, 2, 1, "There was a vast array (how geeky). ");
			AddVerse(para1Curr, 0, 25, "They were naked, but no biggie.");

			// Build the "revision" sections
			IScrSection section1Rev = CreateSection(m_genesisRevision, "My First Section");
			IScrTxtPara para1Rev = AddParaToMockedSectionContent(section1Rev, ScrStyleNames.NormalParagraph);
			AddVerse(para1Rev, 1, 1, "In the beginning God made everything. ");
			AddVerse(para1Rev, 0, 31, "It was all good.");
			IScrTxtPara para2Rev = AddParaToMockedSectionContent(section1Rev, ScrStyleNames.NormalParagraph);
			AddVerse(para1Rev, 2, 1, "Thus the heavens and earth were completed in all their vast array. ");

			// Detect differences
			m_bookMerger.DetectDifferences(null);
			Assert.IsFalse(m_bookMerger.AutoMerged);
			Assert.IsTrue(m_bookMerger.Differences.Count > 0);
			Assert.AreEqual(0, m_bookMerger.ReviewedDiffs.Count);

			// The current version should not have changed.
			Assert.AreEqual(1, m_genesis.SectionsOS.Count);
			Assert.AreEqual(origSection1Curr, m_genesis.SectionsOS[0]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that AutoMerge fails to merge imported Scripture that has an identical section
		/// at the start of a book, followed by a new section which does not overlap that
		/// section. It might seem like we could safely automerge, but there are two problems:
		/// <list type="number">
		/// <item>The identical section might have a back-translation which differs from the
		/// existing section because the diff code doesn't currently detect that. If we blindly
		/// automerged the new section, leaving the existing section as it was, we would have
		/// inadvertently discarded the imported BT for the existing section without giving the
		/// user a chance to "overwrite" the existing with the imported version. Once TE-8113
		/// is done, this will cease to be a problem.</item>
		/// <item>There is also a very slight risk that some other difference in the text might
		/// not yet be caught by our imperfect diff algorithm (e.g., blank lines). This reason
		/// alone probably wouldn't be enough for us to prevent auto-merge.</item>
		/// </list>
		///
		/// revision        current
		/// 1:1-31
		/// 2:1-25 (+BT?)   2:1-25
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void NewSectionAtStartOfBook_FollowedByIdenticalSection()
		{
			// Build the "current" section
			IScrSection origSection1Curr = CreateSection(m_genesis, "My Second Section");
			IScrTxtPara paraCurr = AddParaToMockedSectionContent(origSection1Curr, ScrStyleNames.NormalParagraph);
			AddVerse(paraCurr, 2, 1, "There was a vast array (how geeky). ");
			AddVerse(paraCurr, 0, 25, "They were naked, but no biggie.");

			// Build the "revision" sections
			IScrSection section1Rev = CreateSection(m_genesisRevision, "My First Section");
			IScrTxtPara paraRev = AddParaToMockedSectionContent(section1Rev, ScrStyleNames.NormalParagraph);
			AddVerse(paraRev, 1, 1, "In the beginning God made everything. ");
			AddVerse(paraRev, 0, 31, "It was all good.");

			IScrSection section2Rev = CreateSection(m_genesisRevision, "My Second Section");
			paraRev = AddParaToMockedSectionContent(section2Rev, ScrStyleNames.NormalParagraph);
			AddVerse(paraRev, 2, 1, "There was a vast array (how geeky). ");
			AddVerse(paraRev, 0, 25, "They were naked, but no biggie.");

			// Detect differences
			m_bookMerger.DetectDifferences(null);
			Assert.IsFalse(m_bookMerger.AutoMerged);
			Assert.AreEqual(1, m_bookMerger.Differences.Count);
			Assert.AreEqual(0, m_bookMerger.ReviewedDiffs.Count);

			// The current version should not have changed.
			Assert.AreEqual(1, m_genesis.SectionsOS.Count);
			Assert.AreEqual(origSection1Curr, m_genesis.SectionsOS[0]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that AutoMerge fails to merge imported Scripture that has an intro section
		/// if the current version also has an intro section.
		///
		/// <remarks>We really expected to have to do something special to get this to pass,
		/// but apparently the current diff code bends over backwards to correlate intro
		/// sections, even if they are totally different. If we ever improve the merge code to
		/// treat grossly different intro sections as additions/removals (in some arbitrary
		/// order?), then BookMerger.IsAutoMergePossible is going to have to be made more
		/// intelligent.</remarks>
		///
		/// revision        current
		/// IntroB          IntroA
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IntroSectionsInBothVersions_Different()
		{
			// Build the "current" section
			IScrSection origSection1Curr = CreateIntroSection(m_genesis, "All About Genesis");
			IScrTxtPara paraCurr = AddParaToMockedSectionContent(origSection1Curr, ScrStyleNames.IntroParagraph);
			AddRunToMockedPara(paraCurr, "Forty-eight llamas (and one duck).", null);

			// Build the "revision" sections
			IScrSection section1Rev = CreateIntroSection(m_genesisRevision, "General Outline");
			IScrTxtPara paraRev = AddParaToMockedSectionContent(section1Rev, ScrStyleNames.IntroListItem1);
			AddRunToMockedPara(paraRev, "Twenty-one monkeys", null);
			paraRev = AddParaToMockedSectionContent(section1Rev, ScrStyleNames.IntroListItem1);
			AddRunToMockedPara(paraRev, "got on the ark by mistake.", null);

			IScrSection section2Rev = CreateIntroSection(m_genesisRevision, "Nice Frogs");
			paraRev = AddParaToMockedSectionContent(section2Rev, ScrStyleNames.IntroListItem2);
			AddRunToMockedPara(paraRev, "Hey, the frogs don't come in until Exodus!", null);

			// Detect differences
			m_bookMerger.UseFilteredDiffList = true;
			m_bookMerger.DetectDifferences(null);
			Assert.IsFalse(m_bookMerger.AutoMerged);
			Assert.IsTrue(m_bookMerger.Differences.Count > 1);
			Assert.AreEqual(0, m_bookMerger.ReviewedDiffs.Count);

			// The current version should not have changed.
			Assert.AreEqual(1, m_genesis.SectionsOS.Count);
			Assert.AreEqual(origSection1Curr, m_genesis.SectionsOS[0]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that AutoMerge fails to merge in a newly imported Scripture section that
		/// is after any existing section when the imported version's title is different from
		/// that of the current version.
		///
		/// revision        current
		/// Title'          Title
		///                 1:1-31
		/// 2:1-25
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void NewSectionAtEndOfBook_TitleChanged()
		{
			// Build the "current" section
			m_genesis.TitleOA[0].Contents = Cache.TsStrFactory.MakeString("First Book of the Bible",
				Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle);

			IScrSection section1Curr = CreateSection(m_genesis, "My First Section");
			IScrTxtPara para1Curr = AddParaToMockedSectionContent(section1Curr, ScrStyleNames.NormalParagraph);
			AddVerse(para1Curr, 1, 1, "In the beginning God made everything. ");
			AddVerse(para1Curr, 0, 31, "It was all good.");

			// Build the "revision" sections
			IScrSection section1Rev = CreateSection(m_genesisRevision, "My Second Section");
			IScrTxtPara para1Rev = AddParaToMockedSectionContent(section1Rev, ScrStyleNames.NormalParagraph);
			AddVerse(para1Rev, 2, 1, "There was a vast array (how geeky). ");
			AddVerse(para1Rev, 0, 25, "They were naked, but no biggie.");

			// Detect differences
			m_bookMerger.DetectDifferences(null);
			Assert.IsFalse(m_bookMerger.AutoMerged);
			Assert.IsTrue(m_bookMerger.Differences.Count > 1);
			Assert.AreEqual(0, m_bookMerger.ReviewedDiffs.Count);

			// The current version should not have changed.
			Assert.AreEqual(1, m_genesis.SectionsOS.Count);
			Assert.AreEqual(section1Curr, m_genesis.SectionsOS[0]);
			Assert.AreEqual("First Book of the Bible", ((IScrTxtPara)m_genesis.TitleOA.ParagraphsOS[0]).Contents.Text);
		}
		#endregion

		#region Partial Overwrite tests (yeah, this sort of doesn't belong in this file, but...)
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DoPartialOverwrite method when there is no title in the revision.
		///
		/// revision           current
		/// IntroB             IntroA
		/// IntroC
		///                    1:1-31
		/// 2:1-25 (modified)  2:1-25
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DoPartialOverwrite_NoTitleInRevision()
		{
			m_genesisRevision.TitleOA[0].Contents = Cache.TsStrFactory.MakeString(String.Empty,
				Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle);
			List<IScrSection> sectionsToRemove = new List<IScrSection>();

			// Build the "current" section
			IScrSection origIntroSection = CreateIntroSection(m_genesis, "All About Genesis");
			IScrTxtPara paraCurr = AddParaToMockedSectionContent(origIntroSection, ScrStyleNames.IntroParagraph);
			AddRunToMockedPara(paraCurr, "Forty-eight llamas (and one duck).", null);
			sectionsToRemove.Add(origIntroSection);

			IScrSection origScrSection1Curr = CreateSection(m_genesis, "My First Section");
			paraCurr = AddParaToMockedSectionContent(origScrSection1Curr, ScrStyleNames.NormalParagraph);
			AddVerse(paraCurr, 1, 1, "In the beginning God made everything. ");
			AddVerse(paraCurr, 0, 31, "It was all good.");

			IScrSection origScrSection2Curr = CreateSection(m_genesis, "My Second Section");
			paraCurr = AddParaToMockedSectionContent(origScrSection2Curr, ScrStyleNames.NormalParagraph);
			AddVerse(paraCurr, 2, 1, "There was a vast array (how geeky). ");
			AddVerse(paraCurr, 0, 25, "They were naked, but no biggie.");
			sectionsToRemove.Add(origScrSection2Curr);

			// Build the "revision" sections
			IScrSection revIntroSection1 = CreateIntroSection(m_genesisRevision, "General Outline");
			IScrTxtPara paraRev = AddParaToMockedSectionContent(revIntroSection1, ScrStyleNames.IntroListItem1);
			AddRunToMockedPara(paraRev, "Twenty-one monkeys", null);
			paraRev = AddParaToMockedSectionContent(revIntroSection1, ScrStyleNames.IntroListItem1);
			AddRunToMockedPara(paraRev, "got on the ark by mistake.", null);

			IScrSection revIntroSection2 = CreateIntroSection(m_genesisRevision, "Nice Frogs");
			paraRev = AddParaToMockedSectionContent(revIntroSection2, ScrStyleNames.IntroListItem2);
			AddRunToMockedPara(paraRev, "Hey, the frogs don't come in until Exodus!", null);

			IScrSection revScrSection1 = CreateSection(m_genesisRevision, "My Second Section");
			paraRev = AddParaToMockedSectionContent(revScrSection1, ScrStyleNames.NormalParagraph);
			AddVerse(paraRev, 2, 1, "There was a vast array of stuff. ");
			AddVerse(paraRev, 0, 25, "Adam and the wife were naked as jay birds, but no biggie.");

			// Detect differences
			m_bookMerger.DetectDifferences(null);
			Assert.IsFalse(m_bookMerger.AutoMerged);
			Assert.AreEqual(0, m_bookVersionAgent.m_NumberOfCallsToMakeBackupIfNeeded);

			m_bookMerger.DoPartialOverwrite(sectionsToRemove);
			Assert.AreEqual(1, m_bookVersionAgent.m_NumberOfCallsToMakeBackupIfNeeded);

			// The title should not have changed
			Assert.AreEqual("Genesis", ((IScrTxtPara)m_genesis.TitleOA.ParagraphsOS[0]).Contents.Text);
			// The current should now contain the contents of the revision.
			Assert.AreEqual(4, m_genesis.SectionsOS.Count);
			IScrSection newSection1Curr = m_genesis.SectionsOS[0];
			Assert.AreEqual("Twenty-one monkeys",
				((IScrTxtPara)newSection1Curr.ContentOA.ParagraphsOS[0]).Contents.Text);
			IScrSection newSection2Curr = m_genesis.SectionsOS[1];
			Assert.AreEqual("Hey, the frogs don't come in until Exodus!",
				((IScrTxtPara)newSection2Curr.ContentOA.ParagraphsOS[0]).Contents.Text);
			IScrSection newSection3Curr = m_genesis.SectionsOS[2];
			Assert.AreEqual("11In the beginning God made everything. 31It was all good.",
				((IScrTxtPara)newSection3Curr.ContentOA.ParagraphsOS[0]).Contents.Text);
			IScrSection newSection4Curr = m_genesis.SectionsOS[3];
			Assert.AreEqual("21There was a vast array of stuff. 25Adam and the wife were naked as jay birds, but no biggie.",
				((IScrTxtPara)newSection4Curr.ContentOA.ParagraphsOS[0]).Contents.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DoPartialOverwrite method when there is no title in the revision.
		///
		/// revision           current
		/// TitleB             TitleA
		/// IntroB             IntroA
		/// IntroC
		///                    1:1-31
		/// 2:1-25 (modified)  2:1-25
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DoPartialOverwrite_TitleInRevision()
		{
			m_genesisRevision.TitleOA[0].Contents = Cache.TsStrFactory.MakeString("The Start of Everything",
				Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle);
			List<IScrSection> sectionsToRemove = new List<IScrSection>();

			// Build the "current" section
			IScrSection origIntroSection = CreateIntroSection(m_genesis, "All About Genesis");
			IScrTxtPara paraCurr = AddParaToMockedSectionContent(origIntroSection, ScrStyleNames.IntroParagraph);
			AddRunToMockedPara(paraCurr, "Forty-eight llamas (and one duck).", null);
			sectionsToRemove.Add(origIntroSection);

			IScrSection origScrSection1Curr = CreateSection(m_genesis, "My First Section");
			paraCurr = AddParaToMockedSectionContent(origScrSection1Curr, ScrStyleNames.NormalParagraph);
			AddVerse(paraCurr, 1, 1, "In the beginning God made everything. ");
			AddVerse(paraCurr, 0, 31, "It was all good.");

			IScrSection origScrSection2Curr = CreateSection(m_genesis, "My Second Section");
			paraCurr = AddParaToMockedSectionContent(origScrSection2Curr, ScrStyleNames.NormalParagraph);
			AddVerse(paraCurr, 2, 1, "There was a vast array (how geeky). ");
			AddVerse(paraCurr, 0, 25, "They were naked, but no biggie.");
			sectionsToRemove.Add(origScrSection2Curr);

			// Build the "revision" sections
			IScrSection revIntroSection1 = CreateIntroSection(m_genesisRevision, "General Outline");
			IScrTxtPara paraRev = AddParaToMockedSectionContent(revIntroSection1, ScrStyleNames.IntroListItem1);
			AddRunToMockedPara(paraRev, "Twenty-one monkeys", null);
			paraRev = AddParaToMockedSectionContent(revIntroSection1, ScrStyleNames.IntroListItem1);
			AddRunToMockedPara(paraRev, "got on the ark by mistake.", null);

			IScrSection revIntroSection2 = CreateIntroSection(m_genesisRevision, "Nice Frogs");
			paraRev = AddParaToMockedSectionContent(revIntroSection2, ScrStyleNames.IntroListItem2);
			AddRunToMockedPara(paraRev, "Hey, the frogs don't come in until Exodus!", null);

			IScrSection revScrSection1 = CreateSection(m_genesisRevision, "My Second Section");
			paraRev = AddParaToMockedSectionContent(revScrSection1, ScrStyleNames.NormalParagraph);
			AddVerse(paraRev, 2, 1, "There was a vast array of stuff. ");
			AddVerse(paraRev, 0, 25, "Adam and the wife were naked as jay birds, but no biggie.");

			// Detect differences
			m_bookMerger.DetectDifferences(null);
			Assert.IsFalse(m_bookMerger.AutoMerged);
			Assert.AreEqual(0, m_bookVersionAgent.m_NumberOfCallsToMakeBackupIfNeeded);

			m_bookVersionAgent.MakeBackupCalled += new DummyBookVersionAgent.MakeBackupHandler(m_bookVersionAgent_MakeBackupCalled_DoPartialOverwrite_TitleInRevision);
			m_bookMerger.DoPartialOverwrite(sectionsToRemove);
			Assert.AreEqual(1, m_bookVersionAgent.m_NumberOfCallsToMakeBackupIfNeeded);

			// The title should not have changed
			Assert.AreEqual("The Start of Everything", ((IScrTxtPara)m_genesis.TitleOA.ParagraphsOS[0]).Contents.Text);
			// The current should now contain the contents of the revision.
			Assert.AreEqual(4, m_genesis.SectionsOS.Count);
			IScrSection newSection1Curr = m_genesis.SectionsOS[0];
			Assert.AreEqual("Twenty-one monkeys",
				((IScrTxtPara)newSection1Curr.ContentOA.ParagraphsOS[0]).Contents.Text);
			IScrSection newSection2Curr = m_genesis.SectionsOS[1];
			Assert.AreEqual("Hey, the frogs don't come in until Exodus!",
				((IScrTxtPara)newSection2Curr.ContentOA.ParagraphsOS[0]).Contents.Text);
			IScrSection newSection3Curr = m_genesis.SectionsOS[2];
			Assert.AreEqual("11In the beginning God made everything. 31It was all good.",
				((IScrTxtPara)newSection3Curr.ContentOA.ParagraphsOS[0]).Contents.Text);
			IScrSection newSection4Curr = m_genesis.SectionsOS[3];
			Assert.AreEqual("21There was a vast array of stuff. 25Adam and the wife were naked as jay birds, but no biggie.",
				((IScrTxtPara)newSection4Curr.ContentOA.ParagraphsOS[0]).Contents.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DoPartialOverwrite method when the revision is a superset of the current
		/// (has the same sections plus one more). (TE-8253)
		///
		/// revision           current
		/// TitleB             TitleA
		/// Intro              Intro
		/// 1:1-31             1:1-31
		/// 2:1-25             2:1-25
		/// 3:1-24
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DoPartialOverwrite_RevIsSuperSetOfCur()
		{
			((IScrTxtPara)m_genesisRevision.TitleOA.ParagraphsOS[0]).Contents
				= Cache.TsStrFactory.MakeString("The Start of Everything",
				Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle);
			List<IScrSection> sectionsToRemove = new List<IScrSection>();

			// Build the "current" section
			IScrSection origScrSection1aCurr = CreateSection(m_genesis, "My First Section");
			IScrTxtPara paraCurr = AddParaToMockedSectionContent(origScrSection1aCurr, ScrStyleNames.NormalParagraph);
			AddVerse(paraCurr, 1, 1, "In the beginning God made everything. ");
			AddVerse(paraCurr, 0, 19, "He made the light.");
			sectionsToRemove.Add(origScrSection1aCurr);

			IScrSection origScrSection1bCurr = CreateSection(m_genesis, "My First Section, Part 2");
			paraCurr = AddParaToMockedSectionContent(origScrSection1bCurr, ScrStyleNames.NormalParagraph);
			AddVerse(paraCurr, 0, 20, "Then came the fish. ");
			AddVerse(paraCurr, 0, 31, "It was all good.");
			sectionsToRemove.Add(origScrSection1bCurr);

			IScrSection origScrSection2Curr = CreateSection(m_genesis, "My Second Section");
			paraCurr = AddParaToMockedSectionContent(origScrSection2Curr, ScrStyleNames.NormalParagraph);
			AddVerse(paraCurr, 2, 1, "There was a vast array (how geeky). ");
			AddVerse(paraCurr, 0, 25, "They were naked, but no biggie.");
			sectionsToRemove.Add(origScrSection2Curr);

			// Build the "revision" sections
			IScrSection revIntroSection = CreateIntroSection(m_genesisRevision, "Genesis Background");
			IScrTxtPara paraRev = AddParaToMockedSectionContent(revIntroSection, ScrStyleNames.IntroParagraph);
			AddRunToMockedPara(paraRev, "Forty-seven llamas (and two ducks).", null);
			IScrFootnote footnote = AddFootnote(m_genesisRevision, paraRev, 11, "some say forty");

			IScrSection revScrSection1 = CreateSection(m_genesisRevision, "My First Chapter");
			paraRev = AddParaToMockedSectionContent(revScrSection1, ScrStyleNames.NormalParagraph);
			AddVerse(paraRev, 1, 1, "In the beginning God made everything. ");
			AddVerse(paraRev, 0, 31, "It couldn't have been better.");

			IScrSection revScrSection2 = CreateSection(m_genesisRevision, "My Second Chapter");
			paraRev = AddParaToMockedSectionContent(revScrSection2, ScrStyleNames.NormalParagraph);
			AddVerse(paraRev, 2, 1, "There was a vast array of stuff. ");
			AddVerse(paraRev, 0, 25, "Adam and the wife were naked as jay birds, but no biggie.");

			IScrSection revScrSection3 = CreateSection(m_genesisRevision, "My Third Chapter");
			paraRev = AddParaToMockedSectionContent(revScrSection3, ScrStyleNames.NormalParagraph);
			AddVerse(paraRev, 3, 1, "The snake, now, he was a bad guy. ");
			AddVerse(paraRev, 0, 24, "The angel stood watch over Eden.");

			// Detect differences
			m_bookMerger.DetectDifferences(null);
			Assert.IsFalse(m_bookMerger.AutoMerged);
			Assert.AreEqual(0, m_bookVersionAgent.m_NumberOfCallsToMakeBackupIfNeeded);

			m_bookVersionAgent.MakeBackupCalled += new DummyBookVersionAgent.MakeBackupHandler(m_bookVersionAgent_MakeBackupCalled_DoPartialOverwrite_TitleInRevision);
			m_bookMerger.DoPartialOverwrite(sectionsToRemove);
			Assert.AreEqual(1, m_bookVersionAgent.m_NumberOfCallsToMakeBackupIfNeeded);

			// The title should not have changed
			Assert.AreEqual("The Start of Everything", ((IScrTxtPara)m_genesis.TitleOA.ParagraphsOS[0]).Contents.Text);
			// The current should now contain the contents of the revision.
			Assert.AreEqual(4, m_genesis.SectionsOS.Count);
			IScrSection newIntroSectionCurr = m_genesis.SectionsOS[0];
			Assert.AreEqual("Genesis Background",
				((IScrTxtPara)newIntroSectionCurr.HeadingOA.ParagraphsOS[0]).Contents.Text);
			IScrTxtPara paraIntroCurr = (IScrTxtPara)newIntroSectionCurr.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("Forty-seven" + StringUtils.kChObject + " llamas (and two ducks).",
				paraIntroCurr.Contents.Text);
			VerifyFootnote(m_genesis.FootnotesOS[0], paraIntroCurr, 11);
			IScrSection newSection1Curr = m_genesis.SectionsOS[1];
			Assert.AreEqual("My First Chapter",
				((IScrTxtPara)newSection1Curr.HeadingOA.ParagraphsOS[0]).Contents.Text);
			IScrTxtPara para1Curr = (IScrTxtPara)newSection1Curr.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11In the beginning God made everything. " +
				"31It couldn't have been better.", para1Curr.Contents.Text);
			IScrSection newSection2Curr = m_genesis.SectionsOS[2];
			Assert.AreEqual("My Second Chapter",
				((IScrTxtPara)newSection2Curr.HeadingOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual("21There was a vast array of stuff. 25Adam and the wife were naked as jay " +
				"birds, but no biggie.", ((IScrTxtPara)newSection2Curr.ContentOA.ParagraphsOS[0]).Contents.Text);
			IScrSection newSection3Curr = m_genesis.SectionsOS[3];
			Assert.AreEqual("My Third Chapter",
				((IScrTxtPara)newSection3Curr.HeadingOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual("31The snake, now, he was a bad guy. " +
				"24The angel stood watch over Eden.",
				((IScrTxtPara)newSection3Curr.ContentOA.ParagraphsOS[0]).Contents.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DoPartialOverwrite method when the revision includes a whole chapter and
		/// a partial chapter of the current (only has the first section of the second chapter). (TE-9311)
		///
		/// revision           current
		///			           TitleA
		/// 1:1-31             1:1-19
		///                    1:20-31
		/// 2:1-9              2:1-9
		///                    2:10-25
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DoPartialOverwrite_RevIsPartialChapterOfCur()
		{
			((IScrTxtPara)m_genesisRevision.TitleOA.ParagraphsOS[0]).Contents
				= Cache.TsStrFactory.MakeString("The Start of Everything",
				Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle);
			List<IScrSection> sectionsToRemove = new List<IScrSection>();

			// Build the "current" section
			IScrSection origScrSection1aCurr = CreateSection(m_genesis, "My First Section");
			IScrTxtPara paraCurr = AddParaToMockedSectionContent(origScrSection1aCurr, ScrStyleNames.NormalParagraph);
			AddVerse(paraCurr, 1, 1, "In the beginning God made everything. ");
			AddVerse(paraCurr, 0, 19, "He made the light.");
			sectionsToRemove.Add(origScrSection1aCurr);

			IScrSection origScrSection1bCurr = CreateSection(m_genesis, "My First Section, Part 2");
			paraCurr = AddParaToMockedSectionContent(origScrSection1bCurr, ScrStyleNames.NormalParagraph);
			AddVerse(paraCurr, 0, 20, "Then came the fish. ");
			AddVerse(paraCurr, 0, 31, "It was all good.");
			sectionsToRemove.Add(origScrSection1bCurr);

			IScrSection origScrSection2aCurr = CreateSection(m_genesis, "My Second Section");
			paraCurr = AddParaToMockedSectionContent(origScrSection2aCurr, ScrStyleNames.NormalParagraph);
			AddVerse(paraCurr, 2, 1, "There was a vast array (how geeky). ");
			AddVerse(paraCurr, 0, 9, "There was a tree.");
			sectionsToRemove.Add(origScrSection2aCurr);

			IScrSection origScrSection2bCurr = CreateSection(m_genesis, "My Second Section - Part 2");
			paraCurr = AddParaToMockedSectionContent(origScrSection2bCurr, ScrStyleNames.NormalParagraph);
			AddVerse(paraCurr, 0, 10, "There was a river. ");
			AddVerse(paraCurr, 0, 25, "They were naked, but no biggie.");

			// Build the "revision" sections

			IScrSection revScrSection1 = CreateSection(m_genesisRevision, "My First Chapter");
			IScrTxtPara paraRev = AddParaToMockedSectionContent(revScrSection1, ScrStyleNames.NormalParagraph);
			AddVerse(paraRev, 1, 1, "In the beginning God created everything. ");
			AddVerse(paraRev, 0, 19, "He made light shine out. ");
			AddVerse(paraRev, 0, 20, "Then came the fish and other swimming things. ");
			AddVerse(paraRev, 0, 31, "It was all extremely good.");

			IScrSection revScrSection2 = CreateSection(m_genesisRevision, "My Second Chapter");
			paraRev = AddParaToMockedSectionContent(revScrSection2, ScrStyleNames.NormalParagraph);
			AddVerse(paraRev, 2, 1, "There was a vast array of stuff. ");
			AddVerse(paraRev, 0, 9, "There was a tree in the middle of the garden.");

			// Detect differences
			m_bookMerger.DetectDifferences(null);
			Assert.IsFalse(m_bookMerger.AutoMerged);
			Assert.AreEqual(0, m_bookVersionAgent.m_NumberOfCallsToMakeBackupIfNeeded);

			m_bookVersionAgent.MakeBackupCalled += new DummyBookVersionAgent.MakeBackupHandler(m_bookVersionAgent_MakeBackupCalled_DoPartialOverwrite_TitleInRevision);
			m_bookMerger.DoPartialOverwrite(sectionsToRemove);
			Assert.AreEqual(1, m_bookVersionAgent.m_NumberOfCallsToMakeBackupIfNeeded);

			// The title should not have changed
			Assert.AreEqual("The Start of Everything", ((IScrTxtPara)m_genesis.TitleOA.ParagraphsOS[0]).Contents.Text);
			// The current should now contain the contents of the revision.
			Assert.AreEqual(3, m_genesis.SectionsOS.Count);
			IScrSection revisedSection1 = m_genesis.SectionsOS[0];
			Assert.AreEqual("My First Chapter",
				((IScrTxtPara)revisedSection1.HeadingOA.ParagraphsOS[0]).Contents.Text);
			paraCurr = (IScrTxtPara)revisedSection1.ContentOA.ParagraphsOS[0];
			AddVerse(paraRev, 1, 1, "In the beginning God created everything. ");
			AddVerse(paraRev, 0, 19, "He made light shine out. ");
			AddVerse(paraRev, 0, 20, "Then came the fish and other swimming things. ");
			AddVerse(paraRev, 0, 31, "It was all extremely good.");
			Assert.AreEqual("11In the beginning God created everything. " +
				"19He made light shine out. " +
				"20Then came the fish and other swimming things. " +
				"31It was all extremely good.", paraCurr.Contents.Text);
			IScrSection revisedSection2a = m_genesis.SectionsOS[1];
			Assert.AreEqual("My Second Chapter",
				((IScrTxtPara)revisedSection2a.HeadingOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual("21There was a vast array of stuff. " +
				"9There was a tree in the middle of the garden.", ((IScrTxtPara)revisedSection2a.ContentOA.ParagraphsOS[0]).Contents.Text);
			IScrSection unchangedSection2b = m_genesis.SectionsOS[2];
			Assert.AreEqual("My Second Section - Part 2",
				((IScrTxtPara)unchangedSection2b.HeadingOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual("10There was a river. " +
				"25They were naked, but no biggie.",
				((IScrTxtPara)unchangedSection2b.ContentOA.ParagraphsOS[0]).Contents.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handler to make sure MakeBackupIfNeeded was called before the current was changed
		/// by the code that executes the partial overwrite.
		/// </summary>
		/// <param name="bookMerger">The book merger.</param>
		/// ------------------------------------------------------------------------------------
		private void m_bookVersionAgent_MakeBackupCalled_DoPartialOverwrite_TitleInRevision(BookMerger bookMerger)
		{
			Assert.AreEqual(1, bookMerger.BookCurr.TitleOA.ParagraphsOS.Count);
			IScrTxtPara title = (IScrTxtPara)bookMerger.BookCurr.TitleOA.ParagraphsOS[0];
			Assert.AreEqual("Genesis", title.Contents.Text);
		}
		#endregion
	}
}
