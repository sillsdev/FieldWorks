// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2004' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
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
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.Common.Controls;
using SILUBS.SharedScrUtils;
using SIL.FieldWorks.FDO.LangProj;

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
		public override void Initialize()
		{
			CheckDisposed();
			base.Initialize();
			m_inMemoryCache.InitializeWritingSystemEncodings();

			// init the DummyBookMerger
			Debug.Assert(m_bookMerger == null, "m_bookMerger is not null.");
			m_bookMerger = new DummyBookMerger(Cache, null, m_genesisRevision);
			m_bookMerger.AttemptAutoMerge = true;
			m_bookVersionAgent = new DummyBookVersionAgent();
			m_bookMerger.BookVersionAgent = m_bookVersionAgent;
			EnsureAnnDefn(LangProject.kguidAnnTextSegment);
			EnsureAnnDefn(LangProject.kguidAnnFreeTranslation);
			EnsureAnnDefn(LangProject.kguidAnnWordformInContext);
			EnsureAnnDefn(LangProject.kguidAnnPunctuationInContext);
			if (Cache.LangProject.WordformInventoryOA == null)
			{
				WordformInventory wfi = new WordformInventory();
				Cache.LangProject.WordformInventoryOA = wfi;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shuts down the cache.
		/// </summary>
		/// <remarks>This method is called after each test</remarks>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public override void Exit()
		{
			CheckDisposed();

			m_bookMerger.Dispose();
			m_bookMerger = null;
			m_genesis = null;
			m_genesisRevision = null;

			base.Exit();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Allows subclasses to do other stuff to initialize the cache before it gets used
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void InitializeCache()
		{
			Cache.MapType(typeof(StTxtPara), typeof(ScrTxtPara));
			Cache.MapType(typeof(StFootnote), typeof(ScrFootnote));
			base.InitializeCache();

			// For these tests we want to use a real action handler
			// Will be disposed from FdoCache
			((NewFdoCache)m_inMemoryCache.Cache).ActionHandler = ActionHandlerClass.Create();
		}

		#region IDisposable override

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_bookMerger != null)
					m_bookMerger.Dispose();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_bookMerger = null;
			m_genesis = null;
			m_genesisRevision = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overridden to only create a book with no content, heading, title, etc.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			m_genesis = m_scrInMemoryCache.AddBookToMockedScripture(1, "Genesis");
			m_scrInMemoryCache.AddTitleToMockedBook(m_genesis.Hvo, "Genesis");
			m_genesisRevision = m_scrInMemoryCache.AddArchiveBookToMockedScripture(1, "Genesis");
			m_scrInMemoryCache.AddTitleToMockedBook(m_genesisRevision.Hvo, "Genesis");
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
			CheckDisposed();

			// Build the "current" section
			IScrSection origSection1Curr = CreateSection(m_genesis, "My Second Section");
			StTxtPara para1Curr = AddPara(origSection1Curr);
			AddVerse(para1Curr, 2, 1, "There was a vast array (how geeky). ");
			AddVerse(para1Curr, 0, 25, "They were naked, but no biggie.");
			origSection1Curr.AdjustReferences();

			// Build the "revision" sections
			IScrSection section1Rev = CreateSection(m_genesisRevision, "My First Section");
			StTxtPara para1Rev = AddPara(section1Rev);
			AddVerse(para1Rev, 1, 1, "In the beginning God made everything. ");
			AddVerse(para1Rev, 0, 31, "It was all good.");
			section1Rev.AdjustReferences();

			// Detect differences
			m_bookMerger.DetectDifferences(null);
			Assert.IsTrue(m_bookMerger.AutoMerged);
			Assert.AreEqual(1, m_bookVersionAgent.m_NumberOfCallsToMakeBackupIfNeeded);

			// In real life, the import code would set the filter on before calling
			// DetectDifferences, but for the test we do it here to prove that the
			// auto-merge functionality is not dependent on the filter.
			m_bookMerger.UseFilteredDiffList();
			Assert.AreEqual(0, m_bookMerger.Differences.Count);

			// The current should now contain the contents of the revision.
			Assert.AreEqual(2, m_genesis.SectionsOS.Count);
			IScrSection newSection1Curr = (ScrSection)m_genesis.SectionsOS[0];
			Assert.AreEqual("11In the beginning God made everything. 31It was all good.",
				((StTxtPara)newSection1Curr.ContentOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual(origSection1Curr.Hvo, m_genesis.SectionsOS.HvoArray[1]);
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
			CheckDisposed();

			// Build the "current" section
			IScrSection origSection1Curr = CreateSection(m_genesis, "My Second Section");
			StTxtPara para1Curr = AddPara(origSection1Curr);
			AddVerse(para1Curr, 2, 1, "There was a vast array (how geeky). ");
			AddVerse(para1Curr, 0, 25, "They were naked, but no biggie.");
			origSection1Curr.AdjustReferences();

			// Build the "revision" sections
			IScrSection section1Rev = CreateIntroSection(m_genesisRevision, "All About Genesis");
			StTxtPara para1Rev = AddPara(section1Rev, ScrStyleNames.IntroParagraph);
			section1Rev.AdjustReferences();

			// Detect differences
			m_bookMerger.DetectDifferences(null);
			Assert.IsTrue(m_bookMerger.AutoMerged);
			Assert.AreEqual(1, m_bookVersionAgent.m_NumberOfCallsToMakeBackupIfNeeded);

			// In real life, the import code would set the filter on before calling
			// DetectDifferences, but for the test we do it here to prove that the
			// auto-merge functionality is not dependent on the filter.
			m_bookMerger.UseFilteredDiffList();
			Assert.AreEqual(0, m_bookMerger.Differences.Count);

			// The current should now contain the contents of the revision.
			Assert.AreEqual(2, m_genesis.SectionsOS.Count);
			IScrSection newSection1Curr = (ScrSection)m_genesis.SectionsOS[0];
			Assert.AreEqual("All About Genesis",
				((StTxtPara)newSection1Curr.HeadingOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual(origSection1Curr.Hvo, m_genesis.SectionsOS.HvoArray[1]);
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
			CheckDisposed();

			// Build the "current" section
			IScrSection origSection1Curr = CreateIntroSection(m_genesis, "All About Genesis");
			StTxtPara paraCurr = AddPara(origSection1Curr, ScrStyleNames.IntroParagraph);
			origSection1Curr.AdjustReferences();

			// Build the "revision" sections
			IScrSection section1Rev = CreateSection(m_genesisRevision, "Chapter Two");
			StTxtPara paraRev = AddPara(section1Rev);
			AddVerse(paraRev, 2, 1, "There was a vast array (how geeky). ");
			AddVerse(paraRev, 0, 25, "They were naked, but no biggie.");
			section1Rev.AdjustReferences();

			// Detect differences
			m_bookMerger.UseFilteredDiffList();
			m_bookMerger.DetectDifferences(null);
			Assert.IsTrue(m_bookMerger.AutoMerged);
			Assert.AreEqual(1, m_bookVersionAgent.m_NumberOfCallsToMakeBackupIfNeeded);
			Assert.AreEqual(0, m_bookMerger.Differences.Count);

			// The current should now contain the contents of the revision.
			Assert.AreEqual(2, m_genesis.SectionsOS.Count);
			Assert.AreEqual(origSection1Curr.Hvo, m_genesis.SectionsOS.HvoArray[0]);
			IScrSection newSection2Curr = (ScrSection)m_genesis.SectionsOS[1];
			Assert.AreEqual("Chapter Two",
				((StTxtPara)newSection2Curr.HeadingOA.ParagraphsOS[0]).Contents.Text);
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
			CheckDisposed();

			// Build the "current" section
			IScrSection origSection1Curr = CreateIntroSection(m_genesis, "All About Genesis");
			StTxtPara paraCurr = AddPara(origSection1Curr, ScrStyleNames.IntroParagraph);
			origSection1Curr.AdjustReferences();

			// Build the "revision" sections
			IScrSection section1Rev = CreateSection(m_genesisRevision, "Chapter Two");
			StTxtPara paraRev = AddPara(section1Rev);
			AddVerse(paraRev, 2, 1, "There was a vast array (how geeky). ");
			AddVerse(paraRev, 0, 25, "They were naked, but no biggie.");
			section1Rev.AdjustReferences();

			// Detect differences
			m_bookMerger.UseFilteredDiffList();
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
			CheckDisposed();

			// Build the "current" section
			IScrSection section1Curr = CreateSection(m_genesis, "My First Section");
			StTxtPara para1Curr = AddPara(section1Curr);
			AddVerse(para1Curr, 1, 1, "In the beginning God made everything. ");
			AddVerse(para1Curr, 0, 31, "It was all good.");
			section1Curr.AdjustReferences();

			// Build the "revision" sections
			IScrSection section1Rev = CreateSection(m_genesisRevision, "My Second Section");
			StTxtPara para1Rev = AddPara(section1Rev);
			AddVerse(para1Rev, 2, 1, "There was a vast array (how geeky). ");
			AddVerse(para1Rev, 0, 25, "They were naked, but no biggie.");
			section1Rev.AdjustReferences();

			// Detect differences
			m_bookMerger.DetectDifferences(null);
			Assert.IsTrue(m_bookMerger.AutoMerged);
			Assert.AreEqual(1, m_bookVersionAgent.m_NumberOfCallsToMakeBackupIfNeeded);
			// In real life, the import code would set the filter on before calling
			// DetectDifferences, but for the test we do it here to prove that the
			// auto-merge functionality is not dependent on the filter.
			m_bookMerger.UseFilteredDiffList();
			Assert.AreEqual(0, m_bookMerger.Differences.Count);

			// The current should now contain the contents of the revision.
			Assert.AreEqual(2, m_genesis.SectionsOS.Count);
			Assert.AreEqual(section1Curr.Hvo, m_genesis.SectionsOS.HvoArray[0]);
			IScrSection newSection2Curr = (ScrSection)m_genesis.SectionsOS[1];
			Assert.AreEqual("21There was a vast array (how geeky). 25They were naked, but no biggie.",
				((StTxtPara)newSection2Curr.ContentOA.ParagraphsOS[0]).Contents.Text);
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
			CheckDisposed();

			// Build the "current" section
			IScrSection section1Curr = CreateSection(m_genesis, "My First Section");
			StTxtPara para1Curr = AddPara(section1Curr);
			AddVerse(para1Curr, 1, 1, "In the beginning God made everything. ");
			AddVerse(para1Curr, 0, 31, "It was all good.");
			section1Curr.AdjustReferences();

			// Build the "revision" sections
			((StTxtPara)m_genesisRevision.TitleOA.ParagraphsOS[0]).Contents.Text = string.Empty;
			IScrSection section1Rev = CreateSection(m_genesisRevision, "My Second Section");
			StTxtPara para1Rev = AddPara(section1Rev);
			AddVerse(para1Rev, 2, 1, "There was a vast array (how geeky). ");
			AddVerse(para1Rev, 0, 25, "They were naked, but no biggie.");
			section1Rev.AdjustReferences();

			// Detect differences
			m_bookMerger.DetectDifferences(null);
			Assert.IsTrue(m_bookMerger.AutoMerged);
			Assert.AreEqual(1, m_bookVersionAgent.m_NumberOfCallsToMakeBackupIfNeeded);

			// In real life, the import code would set the filter on before calling
			// DetectDifferences, but for the test we do it here to prove that the
			// auto-merge functionality is not dependent on the filter.
			m_bookMerger.UseFilteredDiffList();
			Assert.AreEqual(1, m_bookMerger.Differences.Count, "Should still be a difference (which we can ignore) for the book title.");

			// The current should now contain the contents of the revision.
			Assert.AreEqual(2, m_genesis.SectionsOS.Count);
			Assert.AreEqual(section1Curr.Hvo, m_genesis.SectionsOS.HvoArray[0]);
			IScrSection newSection2Curr = (ScrSection)m_genesis.SectionsOS[1];
			Assert.AreEqual("21There was a vast array (how geeky). 25They were naked, but no biggie.",
				((StTxtPara)newSection2Curr.ContentOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual("Genesis", ((StTxtPara)m_genesis.TitleOA.ParagraphsOS[0]).Contents.Text);
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
			CheckDisposed();

			// Build the "current" section
			((StTxtPara)m_genesis.TitleOA.ParagraphsOS[0]).Contents.Text = string.Empty;
			IScrSection origSection1Curr = CreateSection(m_genesis, "My Second Section");
			StTxtPara para1Curr = AddPara(origSection1Curr);
			AddVerse(para1Curr, 2, 1, "There was a vast array (how geeky). ");
			AddVerse(para1Curr, 0, 25, "They were naked, but no biggie.");
			origSection1Curr.AdjustReferences();

			// Build the "revision" sections
			IScrSection section1Rev = CreateSection(m_genesisRevision, "My First Section");
			StTxtPara para1Rev = AddPara(section1Rev);
			AddVerse(para1Rev, 1, 1, "In the beginning God made everything. ");
			AddVerse(para1Rev, 0, 31, "It was all good.");
			section1Rev.AdjustReferences();

			// Detect differences
			m_bookMerger.DetectDifferences(null);
			Assert.IsTrue(m_bookMerger.AutoMerged);
			Assert.AreEqual(1, m_bookVersionAgent.m_NumberOfCallsToMakeBackupIfNeeded);

			// In real life, the import code would set the filter on before calling
			// DetectDifferences, but for the test we do it here to prove that the
			// auto-merge functionality is not dependent on the filter.
			m_bookMerger.UseFilteredDiffList();
			Assert.AreEqual(0, m_bookMerger.Differences.Count);

			// The current should now contain the contents of the revision.
			Assert.AreEqual(2, m_genesis.SectionsOS.Count);
			IScrSection newSection1Curr = (ScrSection)m_genesis.SectionsOS[0];
			Assert.AreEqual("11In the beginning God made everything. 31It was all good.",
				((StTxtPara)newSection1Curr.ContentOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual(origSection1Curr.Hvo, m_genesis.SectionsOS.HvoArray[1]);
			Assert.AreEqual("Genesis", ((StTxtPara)m_genesis.TitleOA.ParagraphsOS[0]).Contents.Text);
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
			CheckDisposed();

			// Build the "current" section
			IScrSection origSection1Curr = CreateSection(m_genesis, "Creation");
			StTxtPara paraCurr = AddPara(origSection1Curr);
			AddVerse(paraCurr, 1, 1, "In the beginning God made everything. ");
			AddVerse(paraCurr, 0, 31, "It was all good.");
			origSection1Curr.AdjustReferences();

			IScrSection origSection2Curr = CreateSection(m_genesis, "The Fall of Man");
			paraCurr = AddPara(origSection2Curr);
			AddVerse(paraCurr, 3, 1, "The smart, mean, nasty snake came a pestering poor little Eve. ");
			AddVerse(paraCurr, 0, 13, "God asked Eve what she did and she pretty much blamed the snake.");
			origSection2Curr.AdjustReferences();

			IScrSection origSection3Curr = CreateSection(m_genesis, "The World's First Skyscraper");
			paraCurr = AddPara(origSection3Curr);
			AddVerse(paraCurr, 11, 1, "There was one world-wide language. ");
			AddVerse(paraCurr, 0, 9, "So they called it Babel and that's why Wycliffe had to come into existence.");
			origSection3Curr.AdjustReferences();

			// Build the "revision" sections
			IScrSection section1Rev = CreateSection(m_genesisRevision, "God and Adam Hang Out");
			StTxtPara paraRev = AddPara(section1Rev);
			AddVerse(paraRev, 2, 1, "There was a vast array (how geeky). ");
			AddVerse(paraRev, 0, 25, "They were naked, but no biggie.");
			section1Rev.AdjustReferences();

			IScrSection section2Rev = CreateSection(m_genesisRevision, "Can You Say Thunderstorm?!");
			paraRev = AddPara(section2Rev);
			AddVerse(paraRev, 6, 1, "Men++ led to Daughters++");
			paraRev = AddPara(section2Rev);
			AddVerse(paraRev, 7, 1, "Noah, you're a good guy, so you can get into the boat.");
			paraRev = AddPara(section2Rev);
			AddVerse(paraRev, 8, 1, "God didn't forget Noah or the cute little puppy dogs.");
			paraRev = AddPara(section2Rev, ScrStyleNames.Line1);
			AddVerse(paraRev, 0, 22, "Now you get to have summer and winter and stuff.");
			section2Rev.AdjustReferences();

			// Detect differences
			m_bookMerger.DetectDifferences(null);
			Assert.IsTrue(m_bookMerger.AutoMerged);
			Assert.AreEqual(1, m_bookVersionAgent.m_NumberOfCallsToMakeBackupIfNeeded);

			// In real life, the import code would set the filter on before calling
			// DetectDifferences, but for the test we do it here to prove that the
			// auto-merge functionality is not dependent on the filter.
			m_bookMerger.UseFilteredDiffList();
			Assert.AreEqual(0, m_bookMerger.Differences.Count);

			// The current should now contain the contents of the revision.
			Assert.AreEqual(5, m_genesis.SectionsOS.Count);
			Assert.AreEqual(origSection1Curr.Hvo, m_genesis.SectionsOS.HvoArray[0]);
			IScrSection newSection2Curr = (ScrSection)m_genesis.SectionsOS[1];
			Assert.AreEqual("21There was a vast array (how geeky). 25They were naked, but no biggie.",
				((StTxtPara)newSection2Curr.ContentOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual(origSection2Curr.Hvo, m_genesis.SectionsOS.HvoArray[2]);
			IScrSection newSection4Curr = (ScrSection)m_genesis.SectionsOS[3];
			Assert.AreEqual("61Men++ led to Daughters++",
				((StTxtPara)newSection4Curr.ContentOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual("71Noah, you're a good guy, so you can get into the boat.",
				((StTxtPara)newSection4Curr.ContentOA.ParagraphsOS[1]).Contents.Text);
			Assert.AreEqual("81God didn't forget Noah or the cute little puppy dogs.",
				((StTxtPara)newSection4Curr.ContentOA.ParagraphsOS[2]).Contents.Text);
			Assert.AreEqual("22Now you get to have summer and winter and stuff.",
				((StTxtPara)newSection4Curr.ContentOA.ParagraphsOS[3]).Contents.Text);
			Assert.AreEqual(origSection3Curr.Hvo, m_genesis.SectionsOS.HvoArray[4]);
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
			CheckDisposed();

			// Build the "current" section
			IScrSection origSection1Curr = CreateSection(m_genesis, "God and Adam Hang Out");
			StTxtPara paraCurr = AddPara(origSection1Curr);
			AddVerse(paraCurr, 2, 1, "There was a vast array (how geeky). ");
			AddVerse(paraCurr, 0, 17, "Eat the forbidden fruit and you can kiss this popsicle stand adios!");
			origSection1Curr.AdjustReferences();

			IScrSection origSection2Curr = CreateSection(m_genesis, "Curse of the Snake");
			paraCurr = AddPara(origSection2Curr);
			AddVerse(paraCurr, 3, 14, "God told the snake:");
			paraCurr = AddPara(origSection2Curr, ScrStyleNames.Line1);
			paraCurr.Contents.Text = "Dude, you are toast!";
			paraCurr = AddPara(origSection2Curr, ScrStyleNames.Line1);
			AddVerse(paraCurr, 0, 15, "Jesus is gonna crush your head!");
			origSection2Curr.AdjustReferences();

			// Build the "revision" sections
			IScrSection section1Rev = CreateSection(m_genesisRevision, "Creation");
			StTxtPara paraRev = AddPara(section1Rev);
			AddVerse(paraRev, 1, 1, "In the beginning God made everything. ");
			AddVerse(paraRev, 0, 31, "It was all good.");
			section1Rev.AdjustReferences();

			IScrSection section2Rev = CreateSection(m_genesisRevision, "Eve Created and Tempted");
			paraRev = AddPara(section2Rev);
			AddVerse(paraRev, 2, 18, "Poor Adam! All alone with no wife. ");
			AddVerse(paraRev, 3, 6, "Wow! Nummy fruit! Adam, you want some?");
			section2Rev.AdjustReferences();

			IScrSection section3Rev = CreateSection(m_genesisRevision, "The World's First Skyscraper");
			paraRev = AddPara(section3Rev);
			AddVerse(paraRev, 11, 1, "There was one world-wide language and only one verse in this chapter to boot.");
			section3Rev.AdjustReferences();

			// Detect differences
			m_bookMerger.DetectDifferences(null);
			Assert.IsTrue(m_bookMerger.AutoMerged);
			Assert.AreEqual(1, m_bookVersionAgent.m_NumberOfCallsToMakeBackupIfNeeded);

			// In real life, the import code would set the filter on before calling
			// DetectDifferences, but for the test we do it here to prove that the
			// auto-merge functionality is not dependent on the filter.
			m_bookMerger.UseFilteredDiffList();
			Assert.AreEqual(0, m_bookMerger.Differences.Count);

			// The current should now contain the contents of the revision.
			Assert.AreEqual(5, m_genesis.SectionsOS.Count);
			IScrSection newSection1Curr = (ScrSection)m_genesis.SectionsOS[0];
			Assert.AreEqual("11In the beginning God made everything. 31It was all good.",
				((StTxtPara)newSection1Curr.ContentOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual(origSection1Curr.Hvo, m_genesis.SectionsOS.HvoArray[1]);
			IScrSection newSection3Curr = (ScrSection)m_genesis.SectionsOS[2];
			Assert.AreEqual("218Poor Adam! All alone with no wife. 36Wow! Nummy fruit! Adam, you want some?",
				((StTxtPara)newSection3Curr.ContentOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual(origSection2Curr.Hvo, m_genesis.SectionsOS.HvoArray[3]);
			IScrSection newSection5Curr = (ScrSection)m_genesis.SectionsOS[4];
			Assert.AreEqual("111There was one world-wide language and only one verse in this chapter to boot.",
				((StTxtPara)newSection5Curr.ContentOA.ParagraphsOS[0]).Contents.Text);
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
			CheckDisposed();

			// Build the "current" section
			IScrSection origSection1Curr = CreateSection(m_genesis, "My Second Section");
			StTxtPara paraCurr = AddPara(origSection1Curr);
			AddVerse(paraCurr, 2, 1, "There was a vast array (how geeky). ");
			paraCurr = AddPara(origSection1Curr);
			AddVerse(paraCurr, 0, 25, "They were naked, but no biggie.");
			origSection1Curr.AdjustReferences();

			// Build the "revision" sections
			IScrSection section1Rev = CreateSection(m_genesisRevision, "My First Section");
			StTxtPara para1Rev = AddPara(section1Rev);
			AddVerse(para1Rev, 1, 1, "In the beginning God made everything. ");
			AddVerse(para1Rev, 0, 31, "It was all good.");
			StTxtPara para2Rev = AddPara(section1Rev);
			AddVerse(para1Rev, 2, 1, "There was a vast array (how geeky). ");
			section1Rev.AdjustReferences();

			// Detect differences
			m_bookMerger.DetectDifferences(null);
			Assert.IsFalse(m_bookMerger.AutoMerged);
			Assert.IsTrue(m_bookMerger.Differences.Count > 0);
			Assert.AreEqual(0, m_bookMerger.ReviewedDiffs.Count);

			// The current version should not have changed.
			Assert.AreEqual(1, m_genesis.SectionsOS.Count);
			Assert.AreEqual(origSection1Curr.Hvo, m_genesis.SectionsOS.HvoArray[0]);
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
			CheckDisposed();

			// Build the "current" section
			IScrSection origSection1Curr = CreateSection(m_genesis, "My Second Section");
			StTxtPara para1Curr = AddPara(origSection1Curr);
			AddVerse(para1Curr, 2, 1, "There was a vast array (how geeky). ");
			AddVerse(para1Curr, 0, 25, "They were naked, but no biggie.");
			origSection1Curr.AdjustReferences();

			// Build the "revision" sections
			IScrSection section1Rev = CreateSection(m_genesisRevision, "My First Section");
			StTxtPara para1Rev = AddPara(section1Rev);
			AddVerse(para1Rev, 1, 1, "In the beginning God made everything. ");
			AddVerse(para1Rev, 0, 31, "It was all good.");
			StTxtPara para2Rev = AddPara(section1Rev);
			AddVerse(para1Rev, 2, 1, "Thus the heavens and earth were completed in all their vast array. ");
			section1Rev.AdjustReferences();

			// Detect differences
			m_bookMerger.DetectDifferences(null);
			Assert.IsFalse(m_bookMerger.AutoMerged);
			Assert.IsTrue(m_bookMerger.Differences.Count > 0);
			Assert.AreEqual(0, m_bookMerger.ReviewedDiffs.Count);

			// The current version should not have changed.
			Assert.AreEqual(1, m_genesis.SectionsOS.Count);
			Assert.AreEqual(origSection1Curr.Hvo, m_genesis.SectionsOS.HvoArray[0]);
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
			CheckDisposed();

			// Build the "current" section
			IScrSection origSection1Curr = CreateSection(m_genesis, "My Second Section");
			StTxtPara paraCurr = AddPara(origSection1Curr);
			AddVerse(paraCurr, 2, 1, "There was a vast array (how geeky). ");
			AddVerse(paraCurr, 0, 25, "They were naked, but no biggie.");
			origSection1Curr.AdjustReferences();

			// Build the "revision" sections
			IScrSection section1Rev = CreateSection(m_genesisRevision, "My First Section");
			StTxtPara paraRev = AddPara(section1Rev);
			AddVerse(paraRev, 1, 1, "In the beginning God made everything. ");
			AddVerse(paraRev, 0, 31, "It was all good.");
			section1Rev.AdjustReferences();

			IScrSection section2Rev = CreateSection(m_genesisRevision, "My Second Section");
			paraRev = AddPara(section2Rev);
			AddVerse(paraRev, 2, 1, "There was a vast array (how geeky). ");
			AddVerse(paraRev, 0, 25, "They were naked, but no biggie.");
			section2Rev.AdjustReferences();

			// Detect differences
			m_bookMerger.DetectDifferences(null);
			Assert.IsFalse(m_bookMerger.AutoMerged);
			Assert.AreEqual(1, m_bookMerger.Differences.Count);
			Assert.AreEqual(0, m_bookMerger.ReviewedDiffs.Count);

			// The current version should not have changed.
			Assert.AreEqual(1, m_genesis.SectionsOS.Count);
			Assert.AreEqual(origSection1Curr.Hvo, m_genesis.SectionsOS.HvoArray[0]);
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
			CheckDisposed();

			// Build the "current" section
			IScrSection origSection1Curr = CreateIntroSection(m_genesis, "All About Genesis");
			StTxtPara paraCurr = AddPara(origSection1Curr, ScrStyleNames.IntroParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(paraCurr, "Forty-eight llamas (and one duck).", null);
			origSection1Curr.AdjustReferences();

			// Build the "revision" sections
			IScrSection section1Rev = CreateIntroSection(m_genesisRevision, "General Outline");
			StTxtPara paraRev = AddPara(section1Rev, ScrStyleNames.IntroListItem1);
			m_scrInMemoryCache.AddRunToMockedPara(paraRev, "Twenty-one monkeys", null);
			paraRev = AddPara(section1Rev, ScrStyleNames.IntroListItem1);
			m_scrInMemoryCache.AddRunToMockedPara(paraRev, "got on the ark by mistake.", null);
			section1Rev.AdjustReferences();

			IScrSection section2Rev = CreateIntroSection(m_genesisRevision, "Nice Frogs");
			paraRev = AddPara(section2Rev, ScrStyleNames.IntroListItem2);
			m_scrInMemoryCache.AddRunToMockedPara(paraRev, "Hey, the frogs don't come in until Exodus!", null);
			section2Rev.AdjustReferences();

			// Detect differences
			m_bookMerger.UseFilteredDiffList();
			m_bookMerger.DetectDifferences(null);
			Assert.IsFalse(m_bookMerger.AutoMerged);
			Assert.IsTrue(m_bookMerger.Differences.Count > 1);
			Assert.AreEqual(0, m_bookMerger.ReviewedDiffs.Count);

			// The current version should not have changed.
			Assert.AreEqual(1, m_genesis.SectionsOS.Count);
			Assert.AreEqual(origSection1Curr.Hvo, m_genesis.SectionsOS.HvoArray[0]);
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
			CheckDisposed();

			// Build the "current" section
			((StTxtPara)m_genesis.TitleOA.ParagraphsOS[0]).Contents.Text = "First Book of the Bible";
			IScrSection section1Curr = CreateSection(m_genesis, "My First Section");
			StTxtPara para1Curr = AddPara(section1Curr);
			AddVerse(para1Curr, 1, 1, "In the beginning God made everything. ");
			AddVerse(para1Curr, 0, 31, "It was all good.");
			section1Curr.AdjustReferences();

			// Build the "revision" sections
			IScrSection section1Rev = CreateSection(m_genesisRevision, "My Second Section");
			StTxtPara para1Rev = AddPara(section1Rev);
			AddVerse(para1Rev, 2, 1, "There was a vast array (how geeky). ");
			AddVerse(para1Rev, 0, 25, "They were naked, but no biggie.");
			section1Rev.AdjustReferences();

			// Detect differences
			m_bookMerger.DetectDifferences(null);
			Assert.IsFalse(m_bookMerger.AutoMerged);
			Assert.IsTrue(m_bookMerger.Differences.Count > 1);
			Assert.AreEqual(0, m_bookMerger.ReviewedDiffs.Count);

			// The current version should not have changed.
			Assert.AreEqual(1, m_genesis.SectionsOS.Count);
			Assert.AreEqual(section1Curr.Hvo, m_genesis.SectionsOS.HvoArray[0]);
			Assert.AreEqual("First Book of the Bible", ((StTxtPara)m_genesis.TitleOA.ParagraphsOS[0]).Contents.Text);
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
			CheckDisposed();

			((StTxtPara)m_genesisRevision.TitleOA.ParagraphsOS[0]).Contents.Text = String.Empty;
			List<IScrSection> sectionsToRemove = new List<IScrSection>();

			// Build the "current" section
			IScrSection origIntroSection = CreateIntroSection(m_genesis, "All About Genesis");
			StTxtPara paraCurr = AddPara(origIntroSection, ScrStyleNames.IntroParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(paraCurr, "Forty-eight llamas (and one duck).", null);
			origIntroSection.AdjustReferences();
			sectionsToRemove.Add(origIntroSection);

			IScrSection origScrSection1Curr = CreateSection(m_genesis, "My First Section");
			paraCurr = AddPara(origScrSection1Curr);
			AddVerse(paraCurr, 1, 1, "In the beginning God made everything. ");
			AddVerse(paraCurr, 0, 31, "It was all good.");
			origScrSection1Curr.AdjustReferences();

			IScrSection origScrSection2Curr = CreateSection(m_genesis, "My Second Section");
			paraCurr = AddPara(origScrSection2Curr);
			AddVerse(paraCurr, 2, 1, "There was a vast array (how geeky). ");
			AddVerse(paraCurr, 0, 25, "They were naked, but no biggie.");
			origScrSection2Curr.AdjustReferences();
			sectionsToRemove.Add(origScrSection2Curr);

			// Build the "revision" sections
			IScrSection revIntroSection1 = CreateIntroSection(m_genesisRevision, "General Outline");
			StTxtPara paraRev = AddPara(revIntroSection1, ScrStyleNames.IntroListItem1);
			m_scrInMemoryCache.AddRunToMockedPara(paraRev, "Twenty-one monkeys", null);
			paraRev = AddPara(revIntroSection1, ScrStyleNames.IntroListItem1);
			m_scrInMemoryCache.AddRunToMockedPara(paraRev, "got on the ark by mistake.", null);
			revIntroSection1.AdjustReferences();

			IScrSection revIntroSection2 = CreateIntroSection(m_genesisRevision, "Nice Frogs");
			paraRev = AddPara(revIntroSection2, ScrStyleNames.IntroListItem2);
			m_scrInMemoryCache.AddRunToMockedPara(paraRev, "Hey, the frogs don't come in until Exodus!", null);
			revIntroSection2.AdjustReferences();

			IScrSection revScrSection1 = CreateSection(m_genesisRevision, "My Second Section");
			paraRev = AddPara(revScrSection1);
			AddVerse(paraRev, 2, 1, "There was a vast array of stuff. ");
			AddVerse(paraRev, 0, 25, "Adam and the wife were naked as jay birds, but no biggie.");
			revScrSection1.AdjustReferences();

			// Detect differences
			m_bookMerger.DetectDifferences(null);
			Assert.IsFalse(m_bookMerger.AutoMerged);
			Assert.AreEqual(0, m_bookVersionAgent.m_NumberOfCallsToMakeBackupIfNeeded);

			m_bookMerger.DoPartialOverwrite(sectionsToRemove);
			Assert.AreEqual(1, m_bookVersionAgent.m_NumberOfCallsToMakeBackupIfNeeded);

			// The title should not have changed
			Assert.AreEqual("Genesis", ((StTxtPara)m_genesis.TitleOA.ParagraphsOS[0]).Contents.Text);
			// The current should now contain the contents of the revision.
			Assert.AreEqual(4, m_genesis.SectionsOS.Count);
			IScrSection newSection1Curr = (ScrSection)m_genesis.SectionsOS[0];
			Assert.AreEqual("Twenty-one monkeys",
				((StTxtPara)newSection1Curr.ContentOA.ParagraphsOS[0]).Contents.Text);
			IScrSection newSection2Curr = (ScrSection)m_genesis.SectionsOS[1];
			Assert.AreEqual("Hey, the frogs don't come in until Exodus!",
				((StTxtPara)newSection2Curr.ContentOA.ParagraphsOS[0]).Contents.Text);
			IScrSection newSection3Curr = (ScrSection)m_genesis.SectionsOS[2];
			Assert.AreEqual("11In the beginning God made everything. 31It was all good.",
				((StTxtPara)newSection3Curr.ContentOA.ParagraphsOS[0]).Contents.Text);
			IScrSection newSection4Curr = (ScrSection)m_genesis.SectionsOS[3];
			Assert.AreEqual("21There was a vast array of stuff. 25Adam and the wife were naked as jay birds, but no biggie.",
				((StTxtPara)newSection4Curr.ContentOA.ParagraphsOS[0]).Contents.Text);
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
			CheckDisposed();

			((StTxtPara)m_genesisRevision.TitleOA.ParagraphsOS[0]).Contents.Text = "The Start of Everything";
			List<IScrSection> sectionsToRemove = new List<IScrSection>();

			// Build the "current" section
			IScrSection origIntroSection = CreateIntroSection(m_genesis, "All About Genesis");
			StTxtPara paraCurr = AddPara(origIntroSection, ScrStyleNames.IntroParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(paraCurr, "Forty-eight llamas (and one duck).", null);
			origIntroSection.AdjustReferences();
			sectionsToRemove.Add(origIntroSection);

			IScrSection origScrSection1Curr = CreateSection(m_genesis, "My First Section");
			paraCurr = AddPara(origScrSection1Curr);
			AddVerse(paraCurr, 1, 1, "In the beginning God made everything. ");
			AddVerse(paraCurr, 0, 31, "It was all good.");
			origScrSection1Curr.AdjustReferences();

			IScrSection origScrSection2Curr = CreateSection(m_genesis, "My Second Section");
			paraCurr = AddPara(origScrSection2Curr);
			AddVerse(paraCurr, 2, 1, "There was a vast array (how geeky). ");
			AddVerse(paraCurr, 0, 25, "They were naked, but no biggie.");
			origScrSection2Curr.AdjustReferences();
			sectionsToRemove.Add(origScrSection2Curr);

			// Build the "revision" sections
			IScrSection revIntroSection1 = CreateIntroSection(m_genesisRevision, "General Outline");
			StTxtPara paraRev = AddPara(revIntroSection1, ScrStyleNames.IntroListItem1);
			m_scrInMemoryCache.AddRunToMockedPara(paraRev, "Twenty-one monkeys", null);
			paraRev = AddPara(revIntroSection1, ScrStyleNames.IntroListItem1);
			m_scrInMemoryCache.AddRunToMockedPara(paraRev, "got on the ark by mistake.", null);
			revIntroSection1.AdjustReferences();

			IScrSection revIntroSection2 = CreateIntroSection(m_genesisRevision, "Nice Frogs");
			paraRev = AddPara(revIntroSection2, ScrStyleNames.IntroListItem2);
			m_scrInMemoryCache.AddRunToMockedPara(paraRev, "Hey, the frogs don't come in until Exodus!", null);
			revIntroSection2.AdjustReferences();

			IScrSection revScrSection1 = CreateSection(m_genesisRevision, "My Second Section");
			paraRev = AddPara(revScrSection1);
			AddVerse(paraRev, 2, 1, "There was a vast array of stuff. ");
			AddVerse(paraRev, 0, 25, "Adam and the wife were naked as jay birds, but no biggie.");
			revScrSection1.AdjustReferences();

			// Detect differences
			m_bookMerger.DetectDifferences(null);
			Assert.IsFalse(m_bookMerger.AutoMerged);
			Assert.AreEqual(0, m_bookVersionAgent.m_NumberOfCallsToMakeBackupIfNeeded);

			m_bookVersionAgent.MakeBackupCalled += new DummyBookVersionAgent.MakeBackupHandler(m_bookVersionAgent_MakeBackupCalled_DoPartialOverwrite_TitleInRevision);
			m_bookMerger.DoPartialOverwrite(sectionsToRemove);
			Assert.AreEqual(1, m_bookVersionAgent.m_NumberOfCallsToMakeBackupIfNeeded);

			// The title should not have changed
			Assert.AreEqual("The Start of Everything", ((StTxtPara)m_genesis.TitleOA.ParagraphsOS[0]).Contents.Text);
			// The current should now contain the contents of the revision.
			Assert.AreEqual(4, m_genesis.SectionsOS.Count);
			IScrSection newSection1Curr = (ScrSection)m_genesis.SectionsOS[0];
			Assert.AreEqual("Twenty-one monkeys",
				((StTxtPara)newSection1Curr.ContentOA.ParagraphsOS[0]).Contents.Text);
			IScrSection newSection2Curr = (ScrSection)m_genesis.SectionsOS[1];
			Assert.AreEqual("Hey, the frogs don't come in until Exodus!",
				((StTxtPara)newSection2Curr.ContentOA.ParagraphsOS[0]).Contents.Text);
			IScrSection newSection3Curr = (ScrSection)m_genesis.SectionsOS[2];
			Assert.AreEqual("11In the beginning God made everything. 31It was all good.",
				((StTxtPara)newSection3Curr.ContentOA.ParagraphsOS[0]).Contents.Text);
			IScrSection newSection4Curr = (ScrSection)m_genesis.SectionsOS[3];
			Assert.AreEqual("21There was a vast array of stuff. 25Adam and the wife were naked as jay birds, but no biggie.",
				((StTxtPara)newSection4Curr.ContentOA.ParagraphsOS[0]).Contents.Text);
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
			CheckDisposed();

			((StTxtPara)m_genesisRevision.TitleOA.ParagraphsOS[0]).Contents.Text = "The Start of Everything";
			List<IScrSection> sectionsToRemove = new List<IScrSection>();
			ScriptureChangeWatcher.Create(Cache); // change watcher for footnotes

			// Build the "current" section
			IScrSection origScrSection1aCurr = CreateSection(m_genesis, "My First Section");
			StTxtPara paraCurr = AddPara(origScrSection1aCurr);
			AddVerse(paraCurr, 1, 1, "In the beginning God made everything. ");
			AddVerse(paraCurr, 0, 19, "He made the light.");
			origScrSection1aCurr.AdjustReferences();
			sectionsToRemove.Add(origScrSection1aCurr);

			IScrSection origScrSection1bCurr = CreateSection(m_genesis, "My First Section, Part 2");
			paraCurr = AddPara(origScrSection1bCurr);
			AddVerse(paraCurr, 0, 20, "Then came the fish. ");
			AddVerse(paraCurr, 0, 31, "It was all good.");
			origScrSection1bCurr.AdjustReferences();
			sectionsToRemove.Add(origScrSection1bCurr);

			IScrSection origScrSection2Curr = CreateSection(m_genesis, "My Second Section");
			paraCurr = AddPara(origScrSection2Curr);
			AddVerse(paraCurr, 2, 1, "There was a vast array (how geeky). ");
			AddVerse(paraCurr, 0, 25, "They were naked, but no biggie.");
			origScrSection2Curr.AdjustReferences();
			sectionsToRemove.Add(origScrSection2Curr);

			// Build the "revision" sections
			IScrSection revIntroSection = CreateIntroSection(m_genesisRevision, "Genesis Background");
			StTxtPara paraRev = AddPara(revIntroSection, ScrStyleNames.IntroParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(paraRev, "Forty-seven llamas (and two ducks).", null);
			StFootnote footnote = m_scrInMemoryCache.AddFootnote(m_genesisRevision, paraRev, 11, "some say forty");
			revIntroSection.AdjustReferences();

			IScrSection revScrSection1 = CreateSection(m_genesisRevision, "My First Chapter");
			paraRev = AddPara(revScrSection1);
			AddVerse(paraRev, 1, 1, "In the beginning God made everything. ");
			AddVerse(paraRev, 0, 31, "It couldn't have been better.");
			revScrSection1.AdjustReferences();

			IScrSection revScrSection2 = CreateSection(m_genesisRevision, "My Second Chapter");
			paraRev = AddPara(revScrSection2);
			AddVerse(paraRev, 2, 1, "There was a vast array of stuff. ");
			AddVerse(paraRev, 0, 25, "Adam and the wife were naked as jay birds, but no biggie.");
			revScrSection2.AdjustReferences();

			IScrSection revScrSection3 = CreateSection(m_genesisRevision, "My Third Chapter");
			paraRev = AddPara(revScrSection3);
			AddVerse(paraRev, 3, 1, "The snake, now, he was a bad guy. ");
			AddVerse(paraRev, 0, 24, "The angel stood watch over Eden.");
			revScrSection3.AdjustReferences();

			// Detect differences
			m_bookMerger.DetectDifferences(null);
			Assert.IsFalse(m_bookMerger.AutoMerged);
			Assert.AreEqual(0, m_bookVersionAgent.m_NumberOfCallsToMakeBackupIfNeeded);

			m_bookVersionAgent.MakeBackupCalled += new DummyBookVersionAgent.MakeBackupHandler(m_bookVersionAgent_MakeBackupCalled_DoPartialOverwrite_TitleInRevision);
			m_bookMerger.DoPartialOverwrite(sectionsToRemove);
			Assert.AreEqual(1, m_bookVersionAgent.m_NumberOfCallsToMakeBackupIfNeeded);

			// The title should not have changed
			Assert.AreEqual("The Start of Everything", ((StTxtPara)m_genesis.TitleOA.ParagraphsOS[0]).Contents.Text);
			// The current should now contain the contents of the revision.
			Assert.AreEqual(4, m_genesis.SectionsOS.Count);
			IScrSection newIntroSectionCurr = (ScrSection)m_genesis.SectionsOS[0];
			Assert.AreEqual("Genesis Background",
				((StTxtPara)newIntroSectionCurr.HeadingOA.ParagraphsOS[0]).Contents.Text);
			StTxtPara paraIntroCurr = (StTxtPara)newIntroSectionCurr.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("Forty-seven" + StringUtils.kchObject + " llamas (and two ducks).",
				paraIntroCurr.Contents.Text);
			VerifyFootnote(m_genesis.FootnotesOS[0], paraIntroCurr, 11);
			IScrSection newSection1Curr = (ScrSection)m_genesis.SectionsOS[1];
			Assert.AreEqual("My First Chapter",
				((StTxtPara)newSection1Curr.HeadingOA.ParagraphsOS[0]).Contents.Text);
			StTxtPara para1Curr = (StTxtPara)newSection1Curr.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11In the beginning God made everything. " +
				"31It couldn't have been better.", para1Curr.Contents.Text);
			IScrSection newSection2Curr = (ScrSection)m_genesis.SectionsOS[2];
			Assert.AreEqual("My Second Chapter",
				((StTxtPara)newSection2Curr.HeadingOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual("21There was a vast array of stuff. 25Adam and the wife were naked as jay " +
				"birds, but no biggie.", ((StTxtPara)newSection2Curr.ContentOA.ParagraphsOS[0]).Contents.Text);
			IScrSection newSection3Curr = (ScrSection)m_genesis.SectionsOS[3];
			Assert.AreEqual("My Third Chapter",
				((StTxtPara)newSection3Curr.HeadingOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual("31The snake, now, he was a bad guy. " +
				"24The angel stood watch over Eden.",
				((StTxtPara)newSection3Curr.ContentOA.ParagraphsOS[0]).Contents.Text);
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
			StTxtPara title = (StTxtPara)bookMerger.BookCurr.TitleOA.ParagraphsOS[0];
			Assert.AreEqual("Genesis", title.Contents.Text);
			Assert.AreEqual(3, bookMerger.BookCurr.SectionsOS.Count);
		}
		#endregion
	}
}
