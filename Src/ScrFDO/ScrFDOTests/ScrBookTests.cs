// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ScrBookTests.cs
// Responsibility: Eberhard Beilharz
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;

using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Test.TestUtils;
using SILUBS.SharedScrUtils;
using System.Collections.Generic;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.FDO.Scripture.FDOTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// DummyScrBook allows tests to use the in-memory cache for tests that rely on BackTransWs,
	/// which normally requires a real cache.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyScrBook : ScrBook
	{
		/// <summary>hvos of the writing systems for the back translations, which can be set
		/// from tests.</summary>
		public List<int> m_btWs = null;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="DummyScrBook"/> class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="hvo">The hvo.</param>
		/// ------------------------------------------------------------------------------------
		public DummyScrBook(FdoCache cache, int hvo) : base(cache, hvo)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets hvos of the writing systems for the back translations used in this book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override List<int> BackTransWs
		{
			get { return m_btWs; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Calls FindMissingBts.
		/// </summary>
		/// <param name="savedVersion">The saved version.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public List<int> CallFindMissingBts(DummyScrBook savedVersion)
		{
			return this.FindMissingBts(savedVersion);
		}
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests the ScrBook class
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class ScrBookTests : ScrInMemoryFdoTestBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test funcionality of CreateNewScrBook.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreateNewScrBook()
		{
			CheckDisposed();

			int cBooksOrig = m_scr.ScriptureBooksOS.Count;
			int hvoTitle;

			IScrBook genesis = m_scr.FindBook("GEN");
			Assert.IsNull(genesis, "Genesis should not exist in TestLangProj.");
			// insert Genesis (before existing books)
			genesis = ScrBook.CreateNewScrBook(1, m_scr, out hvoTitle);
			Assert.IsNotNull(genesis, "Genesis was not created.");
			Assert.AreEqual(cBooksOrig + 1, m_scr.ScriptureBooksOS.Count);
			Assert.AreEqual("GEN", ((ScrBook)m_scr.ScriptureBooksOS[0]).BookId,
				"Genesis not first book in ScriptureBooksOS");
			Assert.IsTrue(genesis.TitleOA.IsValidObject());
			Assert.AreEqual(hvoTitle, genesis.TitleOAHvo);

			IScrBook revelation = m_scr.FindBook("REV");
			Assert.IsNull(revelation, "Revelation should not exist in TestLangProj.");
			// insert Revelation (after current books)
			revelation = ScrBook.CreateNewScrBook(Scripture.kiNtMax, m_scr, out hvoTitle);
			Assert.IsNotNull(revelation, "Revelation was not created.");
			Assert.AreEqual(cBooksOrig + 2, m_scr.ScriptureBooksOS.Count);
			Assert.AreEqual("REV", ((ScrBook)m_scr.ScriptureBooksOS[cBooksOrig + 2 - 1]).BookId,
				"Revelation not last book in ScriptureBooksOS");
			Assert.IsTrue(revelation.TitleOA.IsValidObject());
			Assert.AreEqual(hvoTitle, revelation.TitleOAHvo);

			IScrBook leviticus = m_scr.FindBook("LEV");
			Assert.IsNull(leviticus, "Leviticus should not exist in TestLangProj.");
			// insert Leviticus (middle of current books)
			leviticus = ScrBook.CreateNewScrBook(3, m_scr, out hvoTitle);
			Assert.IsNotNull(leviticus, "Leviticus was not created.");
			Assert.AreEqual(cBooksOrig + 3, m_scr.ScriptureBooksOS.Count);
			Assert.AreEqual("LEV", ((ScrBook)m_scr.ScriptureBooksOS[1]).BookId,
				"Leviticus not second book in ScriptureBooksOS");
			Assert.IsTrue(leviticus.TitleOA.IsValidObject());
			Assert.AreEqual(hvoTitle, leviticus.TitleOAHvo);
		}

		///  ------------------------------------------------------------------------------------
		/// <summary>
		/// Verify operation of BestUIName property.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BestUIName()
		{
			CheckDisposed();

			ScrBook book = (ScrBook)m_scrInMemoryCache.AddBookToMockedScripture(3, "Leviticus");

			// test if the book name is returned properly from the ScrBookRef
			Assert.AreEqual("Leviticus", book.BestUIName);

			// REVIEW: Removed this part of test because BestUIName was changed not to
			// use book.Name.UserDefaultWritingSystem.
			// set UI Name of ScrBook; it should be returned as the first priority
			// book.Name.UserDefaultWritingSystem = "UI Leviticus";
			// Assert.AreEqual("UI Leviticus", book.BestUIName);
		}

//		///  ------------------------------------------------------------------------------------
//		/// <summary>
//		/// Verify operation of BestAvailName property.
//		/// </summary>
//		/// ------------------------------------------------------------------------------------
//		[Test]
//		public void BestAvailName()
//		{
//			CheckDisposed();
//
//			ScrBook book = new ScrBook();
//			m_scr.ScriptureBooksOS.Append(book);
//			book.BookIdRA = (ScrBookRef)m_fdoCache.ScriptureReferenceSystem.BooksOS[2]; //Leviticus
//
//			// Set the UI name. It should never be returned.
//			book.Name.SetAlternative("UI Leviticus", m_fdoCache.DefaultUserWs);
//
//			// test if only SIL id available
//			Assert.AreEqual("LEV", book.BestAvailName);
//
//			// test if analysis Name also available
//			book.Name.AnalysisDefaultWritingSystem = "Leviticus "; //test trim
//			Assert.AreEqual("Leviticus", book.BestAvailName);
//
//			// test if vernacular Name also available
//			book.Name.VernacularDefaultWritingSystem = "Levítico";
//			// apparently IndexOf performs Unicode normalization
//			Assert.IsTrue(book.BestAvailName.IndexOf("Levítico") >= 0, "Should be Levítico");
//
//			// test first book in TestLangProj
//			book = (ScrBook)m_scr.ScriptureBooksOS[0];
//			// apparently IndexOf performs Unicode normalization
//			Assert.IsTrue(book.BestAvailName.IndexOf("PHILÉMON") >= 0,
//				"Should contain PHILÉMON"); //sufficient test of BestAvailName
//			Assert.IsTrue(book.BestAvailName.IndexOf("ÉPÎTRE À PHILÉMON") >= 0); //if name is whole title
//			// It's difficult to test for equality because of Unicode normalization in the cache.
//			// Assert.AreEqual("ÉPÎTRE À PHILÉMON", book.BestAvailName); fails
//		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests finding a book from a paragraph
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindBook()
		{
			CheckDisposed();

			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(1, "Genesis");
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);
			StTxtPara paraContent = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			StText textTitle = m_scrInMemoryCache.AddTitleToMockedBook(book.Hvo, "Title");
			StTxtPara paraTitle = (StTxtPara)textTitle.ParagraphsOS[0];

			// Test with Content paragraph
			IScrBook foundBook = ScrBook.Find(Cache, paraContent.Hvo);
			Assert.AreEqual(book.Hvo, foundBook.Hvo);

			// Test with Title paragraph
			foundBook = ScrBook.Find(Cache, paraTitle.Hvo);
			Assert.AreEqual(book.Hvo, foundBook.Hvo);

			// Test with something else
			foundBook = ScrBook.Find(Cache, m_scr.Hvo);
			Assert.IsNull(foundBook);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the FindBookID
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindBookByID()
		{
			CheckDisposed();

			m_scrInMemoryCache.AddBookToMockedScripture(57, "Philemon");
			m_scrInMemoryCache.AddBookToMockedScripture(59, "James");
			m_scrInMemoryCache.AddBookToMockedScripture(65, "Jude");

			IScrBook book;
			book = ScrBook.FindBookByID(m_scr, 1);		// look for Genesis?
			Assert.IsNull(book, "Genesis was found!");

			book = ScrBook.FindBookByID(m_scr, 57);		// look for Philemon
			Assert.IsNotNull(book, "Philemon was not found");
			Assert.AreEqual("PHM", book.BookId);

			book = ScrBook.FindBookByID(m_scr, 59);		// look for James
			Assert.IsNotNull(book, "James was not found");
			Assert.AreEqual("JAS", book.BookId);

			book = ScrBook.FindBookByID(m_scr, 65);		// look for Jude
			Assert.IsNotNull(book, "Jude was not found");
			Assert.AreEqual("JUD", book.BookId);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test funcionality of InitTitlePara.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InitTitlePara()
		{
			CheckDisposed();

			ILgWritingSystemFactory wsf = Cache.LanguageWritingSystemFactoryAccessor;
			int wsUser = wsf.UserWs;
			// insert Genesis
			IScrBook genesis = m_scrInMemoryCache.AddBookToMockedScripture(1, "Genesis");
			StText title = new StText(Cache, m_inMemoryCache.NewHvo(StText.kClassId));
			Cache.VwCacheDaAccessor.CacheObjProp(genesis.Hvo, (int)ScrBook.ScrBookTags.kflidTitle,
				title.Hvo);

			genesis.InitTitlePara();

			//verify the title para in Genesis
			Assert.IsTrue(genesis.TitleOA.IsValidObject());
			Assert.AreEqual(1, genesis.TitleOA.ParagraphsOS.Count);
			StTxtPara para = (StTxtPara)genesis.TitleOA.ParagraphsOS[0];
			// verify the para
			Assert.IsNull(para.Contents.Text);
			string sWhy;
			if (!TsTextPropsHelper.PropsAreEqual(
				StyleUtils.ParaStyleTextProps(ScrStyleNames.MainBookTitle),
				para.StyleRules,
				out sWhy))
			{
				Assert.Fail(sWhy);
			}

			ITsString tss = para.Contents.UnderlyingTsString;
			Assert.AreEqual(1, tss.RunCount);
		}

		#region DetermineOverwritability tests

		#region DetermineOverwritability tests for full overwrite with no data loss
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overwrite should be allowed when the saved version is a super set of the
		/// current version.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DetermineOverwritability_FullNoDataLoss_SavedIsSupersetOfCurrent_SingleSection_MatchingStart()
		{
			DummyScrBook genesis = new DummyScrBook(Cache,
				m_scrInMemoryCache.AddBookToMockedScripture(1, "Genesis").Hvo);
			ScrSection section1cur = new ScrSection();
			genesis.SectionsOS.Append(section1cur);
			section1cur.VerseRefStart = section1cur.VerseRefMin = new BCVRef(1, 1, 1).BBCCCVVV;
			section1cur.VerseRefEnd = section1cur.VerseRefMax = new BCVRef(1, 1, 20).BBCCCVVV;

			DummyScrBook savedGenesis = new DummyScrBook(Cache,
				m_scrInMemoryCache.AddArchiveBookToMockedScripture(1, "Genesis").Hvo);
			ScrSection section1saved = new ScrSection();
			savedGenesis.SectionsOS.Append(section1saved);
			section1saved.VerseRefStart = section1saved.VerseRefMin = new BCVRef(1, 1, 1).BBCCCVVV;
			section1saved.VerseRefEnd = section1saved.VerseRefMax = new BCVRef(1, 1, 27).BBCCCVVV;
			string sDetails;
			List<IScrSection> sectionsToRemove;
			List<int> missingBtWs;
			Assert.AreEqual(OverwriteType.FullNoDataLoss,
				genesis.DetermineOverwritability(savedGenesis, out sDetails, out sectionsToRemove,
					out missingBtWs),
				"Should allow overwrite when saved book is a superset of current");
			Assert.IsNull(sectionsToRemove);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overwrite should be allowed when the saved version is a super set of the
		/// current version.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DetermineOverwritability_FullNoDataLoss_SavedIsSupersetOfCurrent_SingleSection_MatchingEnd()
		{
			DummyScrBook genesis = new DummyScrBook(Cache,
				m_scrInMemoryCache.AddBookToMockedScripture(1, "Genesis").Hvo);
			ScrSection section1cur = new ScrSection();
			genesis.SectionsOS.Append(section1cur);
			section1cur.VerseRefStart = section1cur.VerseRefMin = new BCVRef(1, 2, 1).BBCCCVVV;
			section1cur.VerseRefEnd = section1cur.VerseRefMax = new BCVRef(1, 2, 10).BBCCCVVV;

			DummyScrBook savedGenesis = new DummyScrBook(Cache,
				m_scrInMemoryCache.AddArchiveBookToMockedScripture(1, "Genesis").Hvo);
			ScrSection section1saved = new ScrSection();
			savedGenesis.SectionsOS.Append(section1saved);
			section1saved.VerseRefStart = section1saved.VerseRefMin = new BCVRef(1, 1, 1).BBCCCVVV;
			section1saved.VerseRefEnd = section1saved.VerseRefMax = new BCVRef(1, 2, 10).BBCCCVVV;
			string sDetails;
			List<IScrSection> sectionsToRemove;
			List<int> missingBtWs;
			Assert.AreEqual(OverwriteType.FullNoDataLoss,
				genesis.DetermineOverwritability(savedGenesis, out sDetails, out sectionsToRemove,
					out missingBtWs), "Should allow overwrite when saved book is a superset of current");
			Assert.IsNull(sectionsToRemove);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overwrite should be allowed when the saved version is a super set of the
		/// current version.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DetermineOverwritability_FullNoDataLoss_SavedIsSupersetOfCurrent_SingleSection_StartAndEndDifferent()
		{
			DummyScrBook genesis = new DummyScrBook(Cache,
				m_scrInMemoryCache.AddBookToMockedScripture(1, "Genesis").Hvo);
			ScrSection section1cur = new ScrSection();
			genesis.SectionsOS.Append(section1cur);
			section1cur.VerseRefStart = section1cur.VerseRefMin = new BCVRef(1, 2, 1).BBCCCVVV;
			section1cur.VerseRefEnd = section1cur.VerseRefMax = new BCVRef(1, 2, 10).BBCCCVVV;

			DummyScrBook savedGenesis = new DummyScrBook(Cache,
				m_scrInMemoryCache.AddArchiveBookToMockedScripture(1, "Genesis").Hvo);
			ScrSection section1saved = new ScrSection();
			savedGenesis.SectionsOS.Append(section1saved);
			section1saved.VerseRefStart = section1saved.VerseRefMin = new BCVRef(1, 1, 1).BBCCCVVV;
			section1saved.VerseRefEnd = section1saved.VerseRefMax = new BCVRef(1, 2, 18).BBCCCVVV;
			string sDetails;
			List<IScrSection> sectionsToRemove;
			List<int> missingBtWs;
			Assert.AreEqual(OverwriteType.FullNoDataLoss,
				genesis.DetermineOverwritability(savedGenesis, out sDetails, out sectionsToRemove,
					out missingBtWs), "Should allow overwrite when saved book is a superset of current");
			Assert.IsNull(sectionsToRemove);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overwrite should be allowed when all the sections in both the saved version and the
		/// current version are intro sections. This can never happen in real life because we
		/// always require at least one Scripture section, even if it's empty.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DetermineOverwritability_FullNoDataLoss_SavedAndCurrentHaveOnlyIntroSections()
		{
			DummyScrBook genesis = new DummyScrBook(Cache,
				m_scrInMemoryCache.AddBookToMockedScripture(1, "Genesis").Hvo);
			ScrSection introCur1 = new ScrSection();
			genesis.SectionsOS.Append(introCur1);
			introCur1.VerseRefStart = introCur1.VerseRefMin = new BCVRef(1, 1, 0).BBCCCVVV;
			introCur1.VerseRefEnd = introCur1.VerseRefMax = new BCVRef(1, 1, 0).BBCCCVVV;
			ScrSection introCur2 = new ScrSection();
			genesis.SectionsOS.Append(introCur2);
			introCur2.VerseRefStart = introCur2.VerseRefMin = new BCVRef(1, 1, 0).BBCCCVVV;
			introCur2.VerseRefEnd = introCur2.VerseRefMax = new BCVRef(1, 1, 0).BBCCCVVV;

			DummyScrBook savedGenesis = new DummyScrBook(Cache,
				m_scrInMemoryCache.AddArchiveBookToMockedScripture(1, "Genesis").Hvo);
			ScrSection section1saved = new ScrSection();
			savedGenesis.SectionsOS.Append(section1saved);
			section1saved.VerseRefStart = section1saved.VerseRefMin = new BCVRef(1, 1, 0).BBCCCVVV;
			section1saved.VerseRefEnd = section1saved.VerseRefMax = new BCVRef(1, 1, 0).BBCCCVVV;
			string sDetails;
			List<IScrSection> sectionsToRemove;
			List<int> missingBtWs;
			Assert.AreEqual(OverwriteType.FullNoDataLoss,
				genesis.DetermineOverwritability(savedGenesis, out sDetails, out sectionsToRemove,
					out missingBtWs), "Should allow overwrite when all the sections in both the saved " +
					"version and the current version are intro sections.");
			Assert.IsNull(sectionsToRemove);
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overwrite should be allowed when both the saved version and the current version have
		/// identical Scripture sections, but the saved version also has an intro section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DetermineOverwritability_FullNoDataLoss_MatchingScrSectionsButOnlySavedHasIntroSection()
		{
			DummyScrBook genesis = new DummyScrBook(Cache,
				m_scrInMemoryCache.AddBookToMockedScripture(1, "Genesis").Hvo);
			ScrSection section1Cur = new ScrSection();
			genesis.SectionsOS.Append(section1Cur);
			section1Cur.VerseRefStart = section1Cur.VerseRefMin = new BCVRef(1, 1, 1).BBCCCVVV;
			section1Cur.VerseRefEnd = section1Cur.VerseRefMax = new BCVRef(1, 1, 31).BBCCCVVV;
			ScrSection section2Cur = new ScrSection();
			genesis.SectionsOS.Append(section2Cur);
			section2Cur.VerseRefStart = section2Cur.VerseRefMin = new BCVRef(1, 2, 1).BBCCCVVV;
			section2Cur.VerseRefEnd = section2Cur.VerseRefMax = new BCVRef(1, 2, 20).BBCCCVVV;

			DummyScrBook savedGenesis = new DummyScrBook(Cache,
				m_scrInMemoryCache.AddArchiveBookToMockedScripture(1, "Genesis").Hvo);
			ScrSection introSaved1 = new ScrSection();
			savedGenesis.SectionsOS.Append(introSaved1);
			introSaved1.VerseRefStart = introSaved1.VerseRefMin = new BCVRef(1, 1, 0).BBCCCVVV;
			introSaved1.VerseRefEnd = introSaved1.VerseRefMax = new BCVRef(1, 1, 0).BBCCCVVV;
			ScrSection section1Saved = new ScrSection();
			savedGenesis.SectionsOS.Append(section1Saved);
			section1Saved.VerseRefStart = section1Saved.VerseRefMin = new BCVRef(1, 1, 1).BBCCCVVV;
			section1Saved.VerseRefEnd = section1Saved.VerseRefMax = new BCVRef(1, 1, 31).BBCCCVVV;
			ScrSection section2Saved = new ScrSection();
			savedGenesis.SectionsOS.Append(section2Saved);
			section2Saved.VerseRefStart = section2Saved.VerseRefMin = new BCVRef(1, 2, 1).BBCCCVVV;
			section2Saved.VerseRefEnd = section2Saved.VerseRefMax = new BCVRef(1, 2, 20).BBCCCVVV;

			string sDetails;
			List<IScrSection> sectionsToRemove;
			List<int> missingBtWs;
			Assert.AreEqual(OverwriteType.FullNoDataLoss,
				genesis.DetermineOverwritability(savedGenesis, out sDetails, out sectionsToRemove,
					out missingBtWs), "Should allow overwrite when both the saved version and the current "
					+ "version have identical Scripture sections, but the saved version also has an intro section.");
			Assert.IsNull(sectionsToRemove);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overwrite should be allowed when the saved version and current version have the same
		/// missing portion.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DetermineOverwritability_FullNoDataLoss_MatchingHoles()
		{
			DummyScrBook genesis = new DummyScrBook(Cache,
				m_scrInMemoryCache.AddBookToMockedScripture(1, "Genesis").Hvo);
			ScrSection sectionCur = new ScrSection();
			genesis.SectionsOS.Append(sectionCur);
			sectionCur.VerseRefStart = sectionCur.VerseRefMin = new BCVRef(1, 1, 1).BBCCCVVV;
			sectionCur.VerseRefEnd = sectionCur.VerseRefMax = new BCVRef(1, 1, 15).BBCCCVVV;
			sectionCur = new ScrSection();
			genesis.SectionsOS.Append(sectionCur);
			sectionCur.VerseRefStart = sectionCur.VerseRefMin = new BCVRef(1, 1, 19).BBCCCVVV;
			sectionCur.VerseRefEnd = sectionCur.VerseRefMax = new BCVRef(1, 1, 27).BBCCCVVV;

			DummyScrBook savedGenesis = new DummyScrBook(Cache,
				m_scrInMemoryCache.AddArchiveBookToMockedScripture(1, "Genesis").Hvo);
			ScrSection sectionSaved = new ScrSection();
			savedGenesis.SectionsOS.Append(sectionSaved);
			sectionSaved.VerseRefStart = sectionSaved.VerseRefMin = new BCVRef(1, 1, 1).BBCCCVVV;
			sectionSaved.VerseRefEnd = sectionSaved.VerseRefMax = new BCVRef(1, 1, 10).BBCCCVVV;
			sectionSaved = new ScrSection();
			savedGenesis.SectionsOS.Append(sectionSaved);
			sectionSaved.VerseRefStart = sectionSaved.VerseRefMin = new BCVRef(1, 1, 11).BBCCCVVV;
			sectionSaved.VerseRefEnd = sectionSaved.VerseRefMax = new BCVRef(1, 1, 15).BBCCCVVV;
			sectionSaved = new ScrSection();
			savedGenesis.SectionsOS.Append(sectionSaved);
			sectionSaved.VerseRefStart = sectionSaved.VerseRefMin = new BCVRef(1, 1, 19).BBCCCVVV;
			sectionSaved.VerseRefEnd = sectionSaved.VerseRefMax = new BCVRef(1, 1, 27).BBCCCVVV;
			string sDetails;
			List<IScrSection> sectionsToRemove;
			List<int> missingBtWs;
			Assert.AreEqual(OverwriteType.FullNoDataLoss,
				genesis.DetermineOverwritability(savedGenesis, out sDetails, out sectionsToRemove,
					out missingBtWs), "Should be ok to overwrite with saved book that contains a matching hole");
			Assert.IsNull(sectionsToRemove);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overwrite should be allowed when the saved version and current version have the same
		/// missing portion. Jira issue for this is TE-7913.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DetermineOverwritability_FullNoDataLoss_IdenticalHoleAtEndOfChapter()
		{
			DummyScrBook genesis = new DummyScrBook(Cache,
				m_scrInMemoryCache.AddBookToMockedScripture(1, "Genesis").Hvo);
			ScrSection sectionCur = new ScrSection();
			genesis.SectionsOS.Append(sectionCur);
			sectionCur.VerseRefStart = sectionCur.VerseRefMin = new BCVRef(1, 1, 1).BBCCCVVV;
			sectionCur.VerseRefEnd = sectionCur.VerseRefMax = new BCVRef(1, 1, 30).BBCCCVVV;
			sectionCur = new ScrSection();
			genesis.SectionsOS.Append(sectionCur);
			sectionCur.VerseRefStart = sectionCur.VerseRefMin = new BCVRef(1, 2, 1).BBCCCVVV;
			sectionCur.VerseRefEnd = sectionCur.VerseRefMax = new BCVRef(1, 2, 25).BBCCCVVV;
			// Since we want identical holes and section ranges, we can pass the exact same
			// ScrBook object as both the current and the revision.
			string sDetails;
			List<IScrSection> sectionsToRemove;
			List<int> missingBtWs;
			Assert.AreEqual(OverwriteType.FullNoDataLoss,
				genesis.DetermineOverwritability(genesis, out sDetails, out sectionsToRemove,
					out missingBtWs), "Should be ok to overwrite with saved book that contains identical hole");
			Assert.IsNull(sectionsToRemove);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overwrite would not result in data loss when the current version doesn't have
		/// Scripture (just intro), but the revision does. A partial overwrite is needed to
		/// preserve the intro in the Current.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DetermineOverwritability_FullNoDataLoss_CurrentHasNoScripture()
		{
			DummyScrBook genesis = new DummyScrBook(Cache,
				m_scrInMemoryCache.AddBookToMockedScripture(1, "Genesis").Hvo);
			ScrSection introCur = new ScrSection();
			genesis.SectionsOS.Append(introCur);
			introCur.VerseRefStart = introCur.VerseRefMin = new BCVRef(1, 1, 0).BBCCCVVV;
			introCur.VerseRefEnd = introCur.VerseRefMax = new BCVRef(1, 1, 0).BBCCCVVV;

			DummyScrBook savedGenesis = new DummyScrBook(Cache,
				m_scrInMemoryCache.AddArchiveBookToMockedScripture(1, "Genesis").Hvo);
			ScrSection section1saved = new ScrSection();
			savedGenesis.SectionsOS.Append(section1saved);
			section1saved.VerseRefStart = section1saved.VerseRefMin = new BCVRef(1, 1, 0).BBCCCVVV;
			section1saved.VerseRefEnd = section1saved.VerseRefMax = new BCVRef(1, 1, 0).BBCCCVVV;
			ScrSection section2saved = new ScrSection();
			savedGenesis.SectionsOS.Append(section2saved);
			section2saved.VerseRefStart = section2saved.VerseRefMin = new BCVRef(1, 1, 1).BBCCCVVV;
			section2saved.VerseRefEnd = section2saved.VerseRefMax = new BCVRef(1, 1, 15).BBCCCVVV;
			string sDetails;
			List<IScrSection> sectionsToRemove;
			List<int> missingBtWs;
			Assert.AreEqual(OverwriteType.FullNoDataLoss,
				genesis.DetermineOverwritability(savedGenesis, out sDetails, out sectionsToRemove,
					out missingBtWs),
				"Can overwrite current version of Scripture with saved book that only has an intro");
			Assert.IsNull(sectionsToRemove);
			Assert.IsNull(sDetails);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overwrite should be allowed when the current version has a section that contains
		/// out-of-order verses. This particular test tests section one = (1:1-1:5) and section
		/// two = (1:1-1:10). (TE-8584)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DetermineOverwritability_FullNoDataLoss_SectionContainsOutOfOrderVerses()
		{
			DummyScrBook genesis = new DummyScrBook(Cache,
				m_scrInMemoryCache.AddBookToMockedScripture(1, "Genesis").Hvo);
			ScrSection section1Cur = new ScrSection();
			genesis.SectionsOS.Append(section1Cur);
			section1Cur.VerseRefStart = section1Cur.VerseRefMin = new BCVRef(1, 1, 1).BBCCCVVV;
			section1Cur.VerseRefEnd = section1Cur.VerseRefMax = new BCVRef(1, 1, 5).BBCCCVVV;
			ScrSection section2Cur = new ScrSection();
			genesis.SectionsOS.Append(section2Cur);
			section2Cur.VerseRefStart = section2Cur.VerseRefMin = new BCVRef(1, 1, 1).BBCCCVVV;
			section2Cur.VerseRefEnd = section2Cur.VerseRefMax = new BCVRef(1, 1, 20).BBCCCVVV;

			DummyScrBook savedGenesis = new DummyScrBook(Cache,
				m_scrInMemoryCache.AddArchiveBookToMockedScripture(1, "Genesis").Hvo);
			ScrSection section1Saved = new ScrSection();
			savedGenesis.SectionsOS.Append(section1Saved);
			section1Saved.VerseRefStart = section1Saved.VerseRefMin = new BCVRef(1, 1, 1).BBCCCVVV;
			section1Saved.VerseRefEnd = section1Saved.VerseRefMax = new BCVRef(1, 1, 20).BBCCCVVV;

			string sDetails;
			List<IScrSection> sectionsToRemove;
			List<int> missingBtWs;
			Assert.AreEqual(OverwriteType.FullNoDataLoss,
				genesis.DetermineOverwritability(savedGenesis, out sDetails, out sectionsToRemove,
					out missingBtWs));
			Assert.IsNull(sectionsToRemove);
		}
		#endregion

		#region DetermineOverwritability tests for full overwrite with data loss
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overwrite would result in data loss when the first verse of the saved version comes
		/// after the first verse of the current version.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DetermineOverwritability_DataLoss_SavedStartsAfterCurrent()
		{
			DummyScrBook genesis = new DummyScrBook(Cache,
				m_scrInMemoryCache.AddBookToMockedScripture(1, "Genesis").Hvo);
			ScrSection section1cur = new ScrSection();
			genesis.SectionsOS.Append(section1cur);
			section1cur.VerseRefStart = section1cur.VerseRefMin = new BCVRef(1, 1, 1).BBCCCVVV;
			section1cur.VerseRefEnd = section1cur.VerseRefMax = new BCVRef(1, 1, 20).BBCCCVVV;

			DummyScrBook savedGenesis = new DummyScrBook(Cache,
				m_scrInMemoryCache.AddArchiveBookToMockedScripture(1, "Genesis").Hvo);
			ScrSection section1saved = new ScrSection();
			savedGenesis.SectionsOS.Append(section1saved);
			section1saved.VerseRefStart = section1saved.VerseRefMin = new BCVRef(1, 1, 6).BBCCCVVV;
			section1saved.VerseRefEnd = section1saved.VerseRefMax = new BCVRef(1, 1, 20).BBCCCVVV;
			string sDetails;
			List<IScrSection> sectionsToRemove;
			List<int> missingBtWs;
			Assert.AreEqual(OverwriteType.DataLoss,
				genesis.DetermineOverwritability(savedGenesis, out sDetails, out sectionsToRemove,
					out missingBtWs), "Cannot overwrite with saved book that starts after the current");
			Assert.IsNull(sectionsToRemove);
			Assert.AreEqual("   GEN 1:1-5", sDetails);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overwrite would result in data loss when the last verse of the saved version ends
		/// before the last verse of the current version.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DetermineOverwritability_DataLoss_SavedEndsBeforeCurrent()
		{
			DummyScrBook genesis = new DummyScrBook(Cache,
				m_scrInMemoryCache.AddBookToMockedScripture(1, "Genesis").Hvo);
			ScrSection section1cur = new ScrSection();
			genesis.SectionsOS.Append(section1cur);
			section1cur.VerseRefStart = section1cur.VerseRefMin = new BCVRef(1, 1, 1).BBCCCVVV;
			section1cur.VerseRefEnd = section1cur.VerseRefMax = new BCVRef(1, 1, 20).BBCCCVVV;

			DummyScrBook savedGenesis = new DummyScrBook(Cache,
				m_scrInMemoryCache.AddArchiveBookToMockedScripture(1, "Genesis").Hvo);
			ScrSection section1saved = new ScrSection();
			savedGenesis.SectionsOS.Append(section1saved);
			section1saved.VerseRefStart = section1saved.VerseRefMin = new BCVRef(1, 1, 1).BBCCCVVV;
			section1saved.VerseRefEnd = section1saved.VerseRefMax = new BCVRef(1, 1, 15).BBCCCVVV;
			string sDetails;
			List<IScrSection> sectionsToRemove;
			List<int> missingBtWs;
			Assert.AreEqual(OverwriteType.DataLoss,
				genesis.DetermineOverwritability(savedGenesis, out sDetails, out sectionsToRemove,
					out missingBtWs), "Cannot overwrite with saved book that ends before the current");
			Assert.IsNull(sectionsToRemove);
			Assert.AreEqual("   GEN 1:16-20", sDetails);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overwrite would result in data loss when the first verse of the saved version comes
		/// after the first verse of the current version and the last verse of the saved version
		/// ends before the last verse of the current version.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DetermineOverwritability_DataLoss_SavedSubsetOfCurrent()
		{
			DummyScrBook genesis = new DummyScrBook(Cache,
				m_scrInMemoryCache.AddBookToMockedScripture(1, "Genesis").Hvo);
			ScrSection section1cur = new ScrSection();
			genesis.SectionsOS.Append(section1cur);
			section1cur.VerseRefStart = section1cur.VerseRefMin = new BCVRef(1, 1, 1).BBCCCVVV;
			section1cur.VerseRefEnd = section1cur.VerseRefMax = new BCVRef(1, 1, 20).BBCCCVVV;

			DummyScrBook savedGenesis = new DummyScrBook(Cache,
				m_scrInMemoryCache.AddArchiveBookToMockedScripture(1, "Genesis").Hvo);
			ScrSection section1saved = new ScrSection();
			savedGenesis.SectionsOS.Append(section1saved);
			section1saved.VerseRefStart = section1saved.VerseRefMin = new BCVRef(1, 1, 5).BBCCCVVV;
			section1saved.VerseRefEnd = section1saved.VerseRefMax = new BCVRef(1, 1, 15).BBCCCVVV;
			string sDetails;
			List<IScrSection> sectionsToRemove;
			List<int> missingBtWs;
			Assert.AreEqual(OverwriteType.DataLoss,
				genesis.DetermineOverwritability(savedGenesis, out sDetails, out sectionsToRemove,
					out missingBtWs),
				"Cannot overwrite with saved book that starts after and ends before the current");
			Assert.IsNull(sectionsToRemove);
			Assert.AreEqual("   GEN 1:1-4" + Environment.NewLine + "   GEN 1:16-20", sDetails);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overwrite would result in data loss when one verse is added to the end of the
		/// current section. (TE-8215)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DetermineOverwritability_DataLoss_OneVerseAddedToEnd()
		{
			DummyScrBook genesis = new DummyScrBook(Cache,
				m_scrInMemoryCache.AddBookToMockedScripture(1, "Genesis").Hvo);
			ScrSection section1cur = new ScrSection();
			genesis.SectionsOS.Append(section1cur);
			section1cur.VerseRefStart = section1cur.VerseRefMin = new BCVRef(1, 1, 1).BBCCCVVV;
			section1cur.VerseRefEnd = section1cur.VerseRefMax = new BCVRef(1, 1, 13).BBCCCVVV;

			DummyScrBook savedGenesis = new DummyScrBook(Cache,
				m_scrInMemoryCache.AddArchiveBookToMockedScripture(1, "Genesis").Hvo);
			ScrSection section1saved = new ScrSection();
			savedGenesis.SectionsOS.Append(section1saved);
			section1saved.VerseRefStart = section1saved.VerseRefMin = new BCVRef(1, 1, 1).BBCCCVVV;
			section1saved.VerseRefEnd = section1saved.VerseRefMax = new BCVRef(1, 1, 12).BBCCCVVV;
			string sDetails;
			List<IScrSection> sectionsToRemove;
			List<int> missingBtWs;
			Assert.AreEqual(OverwriteType.DataLoss,
				genesis.DetermineOverwritability(savedGenesis, out sDetails, out sectionsToRemove,
					out missingBtWs),
				"Cannot overwrite without data loss when a section in a saved book is missing the last verse");
			Assert.IsNull(sectionsToRemove);
			Assert.AreEqual("   GEN 1:13", sDetails);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overwrite would result in data loss when one verse is added to the start of the
		/// current section. (TE-8215)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DetermineOverwritability_DataLoss_OneVerseAddedToStart()
		{
			DummyScrBook genesis = new DummyScrBook(Cache,
				m_scrInMemoryCache.AddBookToMockedScripture(1, "Genesis").Hvo);
			ScrSection section1cur = new ScrSection();
			genesis.SectionsOS.Append(section1cur);
			section1cur.VerseRefStart = section1cur.VerseRefMin = new BCVRef(1, 1, 1).BBCCCVVV;
			section1cur.VerseRefEnd = section1cur.VerseRefMax = new BCVRef(1, 1, 12).BBCCCVVV;

			DummyScrBook savedGenesis = new DummyScrBook(Cache,
				m_scrInMemoryCache.AddArchiveBookToMockedScripture(1, "Genesis").Hvo);
			ScrSection section1saved = new ScrSection();
			savedGenesis.SectionsOS.Append(section1saved);
			section1saved.VerseRefStart = section1saved.VerseRefMin = new BCVRef(1, 1, 2).BBCCCVVV;
			section1saved.VerseRefEnd = section1saved.VerseRefMax = new BCVRef(1, 1, 12).BBCCCVVV;
			string sDetails;
			List<IScrSection> sectionsToRemove;
			List<int> missingBtWs;
			Assert.AreEqual(OverwriteType.DataLoss,
				genesis.DetermineOverwritability(savedGenesis, out sDetails, out sectionsToRemove,
					out missingBtWs),
				"Cannot overwrite without data loss when a section in a saved book is missing the first verse");
			Assert.IsNull(sectionsToRemove);
			Assert.AreEqual("   GEN 1:1", sDetails);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overwrite would result in data loss when one verse is added to the start of the
		/// current section. In this scenario, we have missing verses in the revision before
		/// and after the imported data. (TE-8215)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DetermineOverwritability_DataLoss_MissingSectionBefore()
		{
			DummyScrBook genesis = new DummyScrBook(Cache,
				m_scrInMemoryCache.AddBookToMockedScripture(1, "Genesis").Hvo);
			ScrSection section1cur = new ScrSection();
			genesis.SectionsOS.Append(section1cur);
			section1cur.VerseRefStart = section1cur.VerseRefMin = new BCVRef(1, 1, 1).BBCCCVVV;
			section1cur.VerseRefEnd = section1cur.VerseRefMax = new BCVRef(1, 1, 12).BBCCCVVV;
			ScrSection section2cur = new ScrSection();
			genesis.SectionsOS.Append(section2cur);
			section2cur.VerseRefStart = section2cur.VerseRefMin = new BCVRef(1, 2, 1).BBCCCVVV;
			section2cur.VerseRefEnd = section2cur.VerseRefMax = new BCVRef(1, 2, 13).BBCCCVVV;

			DummyScrBook savedGenesis = new DummyScrBook(Cache,
				m_scrInMemoryCache.AddArchiveBookToMockedScripture(1, "Genesis").Hvo);
			ScrSection section1saved = new ScrSection();
			savedGenesis.SectionsOS.Append(section1saved);
			section1saved.VerseRefStart = section1saved.VerseRefMin = new BCVRef(1, 2, 1).BBCCCVVV;
			section1saved.VerseRefEnd = section1saved.VerseRefMax = new BCVRef(1, 2, 12).BBCCCVVV;
			string sDetails;
			List<IScrSection> sectionsToRemove;
			List<int> missingBtWs;
			Assert.AreEqual(OverwriteType.DataLoss,
				genesis.DetermineOverwritability(savedGenesis, out sDetails, out sectionsToRemove,
					out missingBtWs),
				"Cannot overwrite without data loss when a section in a saved book is missing the first verse");
			Assert.IsNull(sectionsToRemove);
			Assert.AreEqual("   GEN 1:1-31" + Environment.NewLine + "   GEN 2:13", sDetails);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overwrite would result in data loss when one verse is added to the start of the
		/// current section. In this scenario, we have missing verses in the revision before
		/// and after the imported data. (TE-8215)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DetermineOverwritability_DataLoss_MultipleHolesBefore()
		{
			DummyScrBook genesis = new DummyScrBook(Cache,
				m_scrInMemoryCache.AddBookToMockedScripture(1, "Genesis").Hvo);
			ScrSection section1cur = new ScrSection();
			genesis.SectionsOS.Append(section1cur);
			section1cur.VerseRefStart = section1cur.VerseRefMin = new BCVRef(1, 1, 1).BBCCCVVV;
			section1cur.VerseRefEnd = section1cur.VerseRefMax = new BCVRef(1, 1, 15).BBCCCVVV;
			ScrSection section2cur = new ScrSection();
			genesis.SectionsOS.Append(section2cur);
			section2cur.VerseRefStart = section2cur.VerseRefMin = new BCVRef(1, 1, 16).BBCCCVVV;
			section2cur.VerseRefEnd = section2cur.VerseRefMax = new BCVRef(1, 1, 19).BBCCCVVV;
			ScrSection section3cur = new ScrSection();
			genesis.SectionsOS.Append(section3cur);
			section3cur.VerseRefStart = section3cur.VerseRefMin = new BCVRef(1, 1, 20).BBCCCVVV;
			section3cur.VerseRefEnd = section3cur.VerseRefMax = new BCVRef(1, 1, 31).BBCCCVVV;
			ScrSection section4cur = new ScrSection();
			genesis.SectionsOS.Append(section4cur);
			section4cur.VerseRefStart = section4cur.VerseRefMin = new BCVRef(1, 2, 1).BBCCCVVV;
			section4cur.VerseRefEnd = section4cur.VerseRefMax = new BCVRef(1, 2, 13).BBCCCVVV;

			DummyScrBook savedGenesis = new DummyScrBook(Cache,
				m_scrInMemoryCache.AddArchiveBookToMockedScripture(1, "Genesis").Hvo);
			ScrSection section1saved = new ScrSection();
			savedGenesis.SectionsOS.Append(section1saved);
			section1saved.VerseRefStart = section1saved.VerseRefMin = new BCVRef(1, 1, 16).BBCCCVVV;
			section1saved.VerseRefEnd = section1saved.VerseRefMax = new BCVRef(1, 1, 19).BBCCCVVV;
			ScrSection section2saved = new ScrSection();
			savedGenesis.SectionsOS.Append(section2saved);
			section2saved.VerseRefStart = section2saved.VerseRefMin = new BCVRef(1, 2, 1).BBCCCVVV;
			section2saved.VerseRefEnd = section2saved.VerseRefMax = new BCVRef(1, 2, 12).BBCCCVVV;
			string sDetails;
			List<IScrSection> sectionsToRemove;
			List<int> missingBtWs;
			Assert.AreEqual(OverwriteType.DataLoss,
				genesis.DetermineOverwritability(savedGenesis, out sDetails, out sectionsToRemove,
					out missingBtWs),
				"Cannot overwrite without data loss when a section in a saved book is missing the first verse");
			Assert.IsNull(sectionsToRemove);
			Assert.AreEqual("   GEN 1:1-15" + Environment.NewLine + "   GEN 1:20-31" +
				Environment.NewLine + "   GEN 2:13", sDetails);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overwrite would result in data loss when the saved version has a hole that is not in the
		/// current version.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DetermineOverwritability_DataLoss_SavedHasHoles()
		{
			DummyScrBook genesis = new DummyScrBook(Cache,
				m_scrInMemoryCache.AddBookToMockedScripture(1, "Genesis").Hvo);
			ScrSection sectionCur = new ScrSection();
			genesis.SectionsOS.Append(sectionCur);
			sectionCur.VerseRefStart = sectionCur.VerseRefMin = new BCVRef(1, 1, 1).BBCCCVVV;
			sectionCur.VerseRefEnd = sectionCur.VerseRefMax = new BCVRef(1, 1, 15).BBCCCVVV;
			sectionCur = new ScrSection();
			genesis.SectionsOS.Append(sectionCur);
			sectionCur.VerseRefStart = sectionCur.VerseRefMin = new BCVRef(1, 1, 16).BBCCCVVV;
			sectionCur.VerseRefEnd = sectionCur.VerseRefMax = new BCVRef(1, 1, 31).BBCCCVVV;
			sectionCur = new ScrSection();
			genesis.SectionsOS.Append(sectionCur);
			sectionCur.VerseRefStart = sectionCur.VerseRefMin = new BCVRef(1, 2, 1).BBCCCVVV;
			sectionCur.VerseRefEnd = sectionCur.VerseRefMax = new BCVRef(1, 2, 25).BBCCCVVV;

			DummyScrBook savedGenesis = new DummyScrBook(Cache,
				m_scrInMemoryCache.AddArchiveBookToMockedScripture(1, "Genesis").Hvo);
			ScrSection sectionSaved = new ScrSection();
			savedGenesis.SectionsOS.Append(sectionSaved);
			sectionSaved.VerseRefStart = sectionSaved.VerseRefMin = new BCVRef(1, 1, 1).BBCCCVVV;
			sectionSaved.VerseRefEnd = sectionSaved.VerseRefMax = new BCVRef(1, 1, 10).BBCCCVVV;
			sectionSaved = new ScrSection();
			savedGenesis.SectionsOS.Append(sectionSaved);
			sectionSaved.VerseRefStart = sectionSaved.VerseRefMin = new BCVRef(1, 1, 11).BBCCCVVV;
			sectionSaved.VerseRefEnd = sectionSaved.VerseRefMax = new BCVRef(1, 1, 18).BBCCCVVV;
			sectionSaved = new ScrSection();
			savedGenesis.SectionsOS.Append(sectionSaved);
			sectionSaved.VerseRefStart = sectionSaved.VerseRefMin = new BCVRef(1, 1, 22).BBCCCVVV;
			sectionSaved.VerseRefEnd = sectionSaved.VerseRefMax = new BCVRef(1, 1, 31).BBCCCVVV;
			sectionSaved = new ScrSection();
			savedGenesis.SectionsOS.Append(sectionSaved);
			sectionSaved.VerseRefStart = sectionSaved.VerseRefMin = new BCVRef(1, 2, 22).BBCCCVVV;
			sectionSaved.VerseRefEnd = sectionSaved.VerseRefMax = new BCVRef(1, 2, 25).BBCCCVVV;
			string sDetails;
			List<IScrSection> sectionsToRemove;
			List<int> missingBtWs;
			Assert.AreEqual(OverwriteType.DataLoss,
				genesis.DetermineOverwritability(savedGenesis, out sDetails, out sectionsToRemove,
					out missingBtWs),
				"Cannot overwrite with saved book that contains a hole");
			Assert.IsNull(sectionsToRemove);
			Assert.AreEqual("   GEN 1:19-21" + Environment.NewLine + "   GEN 2:1-21", sDetails);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overwrite would result in data loss when the saved version has holes that are only
		/// partly in the current version.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DetermineOverwritability_DataLoss_NastyMismatchedHoles()
		{
			DummyScrBook genesis = new DummyScrBook(Cache,
				m_scrInMemoryCache.AddBookToMockedScripture(1, "Genesis").Hvo);
			ScrSection sectionCur = new ScrSection();
			genesis.SectionsOS.Append(sectionCur);
			sectionCur.VerseRefStart = sectionCur.VerseRefMin = new BCVRef(1, 1, 1).BBCCCVVV;
			sectionCur.VerseRefEnd = sectionCur.VerseRefMax = new BCVRef(1, 1, 15).BBCCCVVV;
			sectionCur = new ScrSection();
			genesis.SectionsOS.Append(sectionCur);
			sectionCur.VerseRefStart = sectionCur.VerseRefMin = new BCVRef(1, 1, 18).BBCCCVVV;
			sectionCur.VerseRefEnd = sectionCur.VerseRefMax = new BCVRef(1, 1, 29).BBCCCVVV;
			sectionCur = new ScrSection();
			genesis.SectionsOS.Append(sectionCur);
			sectionCur.VerseRefStart = sectionCur.VerseRefMin = new BCVRef(1, 2, 1).BBCCCVVV;
			sectionCur.VerseRefEnd = sectionCur.VerseRefMax = new BCVRef(1, 2, 25).BBCCCVVV;
			sectionCur = new ScrSection();
			genesis.SectionsOS.Append(sectionCur);
			sectionCur.VerseRefStart = sectionCur.VerseRefMin = new BCVRef(1, 3, 4).BBCCCVVV;
			sectionCur.VerseRefEnd = sectionCur.VerseRefMax = new BCVRef(1, 3, 15).BBCCCVVV;
			sectionCur = new ScrSection();
			genesis.SectionsOS.Append(sectionCur);
			sectionCur.VerseRefStart = sectionCur.VerseRefMin = new BCVRef(1, 3, 18).BBCCCVVV;
			sectionCur.VerseRefEnd = sectionCur.VerseRefMax = new BCVRef(1, 3, 24).BBCCCVVV;
			sectionCur = new ScrSection();
			genesis.SectionsOS.Append(sectionCur);
			sectionCur.VerseRefStart = sectionCur.VerseRefMin = new BCVRef(1, 4, 1).BBCCCVVV;
			sectionCur.VerseRefEnd = sectionCur.VerseRefMax = new BCVRef(1, 4, 4).BBCCCVVV;
			sectionCur = new ScrSection();
			genesis.SectionsOS.Append(sectionCur);
			sectionCur.VerseRefStart = sectionCur.VerseRefMin = new BCVRef(1, 4, 10).BBCCCVVV;
			sectionCur.VerseRefEnd = sectionCur.VerseRefMax = new BCVRef(1, 4, 18).BBCCCVVV;

			DummyScrBook savedGenesis = new DummyScrBook(Cache,
				m_scrInMemoryCache.AddArchiveBookToMockedScripture(1, "Genesis").Hvo);
			ScrSection sectionSaved = new ScrSection();
			savedGenesis.SectionsOS.Append(sectionSaved);
			sectionSaved.VerseRefStart = sectionSaved.VerseRefMin = new BCVRef(1, 1, 1).BBCCCVVV;
			sectionSaved.VerseRefEnd = sectionSaved.VerseRefMax = new BCVRef(1, 1, 10).BBCCCVVV;
			sectionSaved = new ScrSection();
			savedGenesis.SectionsOS.Append(sectionSaved);
			sectionSaved.VerseRefStart = sectionSaved.VerseRefMin = new BCVRef(1, 2, 22).BBCCCVVV;
			sectionSaved.VerseRefEnd = sectionSaved.VerseRefMax = new BCVRef(1, 2, 25).BBCCCVVV;
			sectionSaved = new ScrSection();
			savedGenesis.SectionsOS.Append(sectionSaved);
			sectionSaved.VerseRefStart = sectionSaved.VerseRefMin = new BCVRef(1, 3, 4).BBCCCVVV;
			sectionSaved.VerseRefEnd = sectionSaved.VerseRefMax = new BCVRef(1, 3, 15).BBCCCVVV;
			sectionSaved = new ScrSection();
			savedGenesis.SectionsOS.Append(sectionSaved);
			sectionSaved.VerseRefStart = sectionSaved.VerseRefMin = new BCVRef(1, 3, 18).BBCCCVVV;
			sectionSaved.VerseRefEnd = sectionSaved.VerseRefMax = new BCVRef(1, 3, 21).BBCCCVVV;
			sectionSaved = new ScrSection();
			savedGenesis.SectionsOS.Append(sectionSaved);
			sectionSaved.VerseRefStart = sectionSaved.VerseRefMin = new BCVRef(1, 4, 1).BBCCCVVV;
			sectionSaved.VerseRefEnd = sectionSaved.VerseRefMax = new BCVRef(1, 4, 6).BBCCCVVV;
			sectionSaved = new ScrSection();
			savedGenesis.SectionsOS.Append(sectionSaved);
			sectionSaved.VerseRefStart = sectionSaved.VerseRefMin = new BCVRef(1, 4, 8).BBCCCVVV;
			sectionSaved.VerseRefEnd = sectionSaved.VerseRefMax = new BCVRef(1, 4, 18).BBCCCVVV;
			string sDetails;
			List<IScrSection> sectionsToRemove;
			List<int> missingBtWs;
			Assert.AreEqual(OverwriteType.DataLoss,
				genesis.DetermineOverwritability(savedGenesis, out sDetails, out sectionsToRemove,
					out missingBtWs),
				"Cannot overwrite with saved book that contains a hole");
			Assert.IsNull(sectionsToRemove);
			Assert.AreEqual("   GEN 1:11-15" + Environment.NewLine + "   GEN 1:18-29" +
				Environment.NewLine + "   GEN 2:1-21" + Environment.NewLine + "   GEN 3:22-24", sDetails);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overwrite should be allowed when the current version has a section that contains
		/// out-of-order verses. This particular test tests section one = (50:1-50:26) and section
		/// two = (50:12-50:26). (TE-8584)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DetermineOverwritability_DataLoss_SectionContainsOutOfOrderVerses()
		{
			DummyScrBook genesis = new DummyScrBook(Cache,
				m_scrInMemoryCache.AddBookToMockedScripture(1, "Genesis").Hvo);
			ScrSection section1Cur = new ScrSection();
			genesis.SectionsOS.Append(section1Cur);
			section1Cur.VerseRefStart = section1Cur.VerseRefMin = new BCVRef(1, 50, 1).BBCCCVVV;
			section1Cur.VerseRefEnd = section1Cur.VerseRefMax = new BCVRef(1, 50, 26).BBCCCVVV;
			ScrSection section2Cur = new ScrSection();
			genesis.SectionsOS.Append(section2Cur);
			section2Cur.VerseRefStart = section2Cur.VerseRefMin = new BCVRef(1, 50, 12).BBCCCVVV;
			section2Cur.VerseRefEnd = section2Cur.VerseRefMax = new BCVRef(1, 50, 26).BBCCCVVV;

			DummyScrBook savedGenesis = new DummyScrBook(Cache,
				m_scrInMemoryCache.AddArchiveBookToMockedScripture(1, "Genesis").Hvo);
			ScrSection section1Saved = new ScrSection();
			savedGenesis.SectionsOS.Append(section1Saved);
			section1Saved.VerseRefStart = section1Saved.VerseRefMin = new BCVRef(1, 50, 1).BBCCCVVV;
			section1Saved.VerseRefEnd = section1Saved.VerseRefMax = new BCVRef(1, 50, 26).BBCCCVVV;

			string sDetails;
			List<IScrSection> sectionsToRemove;
			List<int> missingBtWs;
			Assert.AreEqual(OverwriteType.FullNoDataLoss,
				genesis.DetermineOverwritability(savedGenesis, out sDetails, out sectionsToRemove,
					out missingBtWs));
			Assert.IsNull(sectionsToRemove);
		}
		#endregion

		#region DetermineOverwritability tests for partial overwrite
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overwrite would result in data loss when the saved version doesn't have an intro,
		/// but the current version current version does.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DetermineOverwritability_Partial_MatchingScrSections_SavedHasNoIntro()
		{
			DummyScrBook genesis = new DummyScrBook(Cache,
				m_scrInMemoryCache.AddBookToMockedScripture(1, "Genesis").Hvo);
			ScrSection introCur = new ScrSection();
			genesis.SectionsOS.Append(introCur);
			introCur.VerseRefStart = introCur.VerseRefMin = new BCVRef(1, 1, 0).BBCCCVVV;
			introCur.VerseRefEnd = introCur.VerseRefMax = new BCVRef(1, 1, 0).BBCCCVVV;
			ScrSection section1cur = new ScrSection();
			genesis.SectionsOS.Append(section1cur);
			section1cur.VerseRefStart = section1cur.VerseRefMin = new BCVRef(1, 1, 1).BBCCCVVV;
			section1cur.VerseRefEnd = section1cur.VerseRefMax = new BCVRef(1, 1, 20).BBCCCVVV;

			DummyScrBook savedGenesis = new DummyScrBook(Cache,
				m_scrInMemoryCache.AddArchiveBookToMockedScripture(1, "Genesis").Hvo);
			ScrSection section1saved = new ScrSection();
			savedGenesis.SectionsOS.Append(section1saved);
			section1saved.VerseRefStart = section1saved.VerseRefMin = new BCVRef(1, 1, 1).BBCCCVVV;
			section1saved.VerseRefEnd = section1saved.VerseRefMax = new BCVRef(1, 1, 20).BBCCCVVV;
			string sDetails;
			List<IScrSection> sectionsToRemove;
			List<int> missingBtWs;
			Assert.AreEqual(OverwriteType.Partial,
				genesis.DetermineOverwritability(savedGenesis, out sDetails, out sectionsToRemove,
					out missingBtWs),
				"Can do partial overwrite with saved book that does not have intro");
			Assert.AreEqual(1, sectionsToRemove.Count);
			Assert.AreEqual(genesis.SectionsOS.HvoArray[1], sectionsToRemove[0].Hvo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Partial overwrite is possible when the saved version has a hole that is covered
		/// exactly (with no conflicts) in the current version.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DetermineOverwritability_Partial_SavedHasHoles_ExactlyCoveredByCurrent()
		{
			DummyScrBook genesis = new DummyScrBook(Cache,
				m_scrInMemoryCache.AddBookToMockedScripture(1, "Genesis").Hvo);
			ScrSection sectionCur = new ScrSection();
			genesis.SectionsOS.Append(sectionCur);
			sectionCur.VerseRefStart = sectionCur.VerseRefMin = new BCVRef(1, 1, 1).BBCCCVVV;
			sectionCur.VerseRefEnd = sectionCur.VerseRefMax = new BCVRef(1, 1, 15).BBCCCVVV;
			sectionCur = new ScrSection();
			genesis.SectionsOS.Append(sectionCur);
			sectionCur.VerseRefStart = sectionCur.VerseRefMin = new BCVRef(1, 1, 16).BBCCCVVV;
			sectionCur.VerseRefEnd = sectionCur.VerseRefMax = new BCVRef(1, 1, 31).BBCCCVVV;
			sectionCur = new ScrSection();
			genesis.SectionsOS.Append(sectionCur);
			sectionCur.VerseRefStart = sectionCur.VerseRefMin = new BCVRef(1, 2, 1).BBCCCVVV;
			sectionCur.VerseRefEnd = sectionCur.VerseRefMax = new BCVRef(1, 2, 25).BBCCCVVV;

			DummyScrBook savedGenesis = new DummyScrBook(Cache,
				m_scrInMemoryCache.AddArchiveBookToMockedScripture(1, "Genesis").Hvo);
			ScrSection sectionSaved = new ScrSection();
			savedGenesis.SectionsOS.Append(sectionSaved);
			sectionSaved.VerseRefStart = sectionSaved.VerseRefMin = new BCVRef(1, 1, 1).BBCCCVVV;
			sectionSaved.VerseRefEnd = sectionSaved.VerseRefMax = new BCVRef(1, 1, 15).BBCCCVVV;
			sectionSaved = new ScrSection();
			savedGenesis.SectionsOS.Append(sectionSaved);
			sectionSaved.VerseRefStart = sectionSaved.VerseRefMin = new BCVRef(1, 2, 1).BBCCCVVV;
			sectionSaved.VerseRefEnd = sectionSaved.VerseRefMax = new BCVRef(1, 2, 25).BBCCCVVV;
			string sDetails;
			List<IScrSection> sectionsToRemove;
			List<int> missingBtWs;
			Assert.AreEqual(OverwriteType.Partial,
				genesis.DetermineOverwritability(savedGenesis, out sDetails, out sectionsToRemove,
					out missingBtWs),
				"Can perform a partial overwrite with saved book that contains a hole exactly matched by a current section");
			Assert.AreEqual(2, sectionsToRemove.Count);
			Assert.AreEqual(genesis.SectionsOS.HvoArray[0], sectionsToRemove[0].Hvo);
			Assert.AreEqual(genesis.SectionsOS.HvoArray[2], sectionsToRemove[1].Hvo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Partial overwrite is possible when the saved version has a hole that is partially
		/// covered by a section (with no conflicts) in the current version.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DetermineOverwritability_Partial_SavedHasHoles_PartiallyCoveredByCurrent()
		{
			DummyScrBook genesis = new DummyScrBook(Cache,
				m_scrInMemoryCache.AddBookToMockedScripture(1, "Genesis").Hvo);
			ScrSection sectionCur = new ScrSection();
			genesis.SectionsOS.Append(sectionCur);
			sectionCur.VerseRefStart = sectionCur.VerseRefMin = new BCVRef(1, 1, 1).BBCCCVVV;
			sectionCur.VerseRefEnd = sectionCur.VerseRefMax = new BCVRef(1, 1, 15).BBCCCVVV;
			sectionCur = new ScrSection();
			genesis.SectionsOS.Append(sectionCur);
			sectionCur.VerseRefStart = sectionCur.VerseRefMin = new BCVRef(1, 1, 18).BBCCCVVV;
			sectionCur.VerseRefEnd = sectionCur.VerseRefMax = new BCVRef(1, 1, 25).BBCCCVVV;
			sectionCur = new ScrSection();
			genesis.SectionsOS.Append(sectionCur);
			sectionCur.VerseRefStart = sectionCur.VerseRefMin = new BCVRef(1, 2, 1).BBCCCVVV;
			sectionCur.VerseRefEnd = sectionCur.VerseRefMax = new BCVRef(1, 2, 25).BBCCCVVV;

			DummyScrBook savedGenesis = new DummyScrBook(Cache,
				m_scrInMemoryCache.AddArchiveBookToMockedScripture(1, "Genesis").Hvo);
			ScrSection sectionSaved = new ScrSection();
			savedGenesis.SectionsOS.Append(sectionSaved);
			sectionSaved.VerseRefStart = sectionSaved.VerseRefMin = new BCVRef(1, 1, 1).BBCCCVVV;
			sectionSaved.VerseRefEnd = sectionSaved.VerseRefMax = new BCVRef(1, 1, 15).BBCCCVVV;
			sectionSaved = new ScrSection();
			savedGenesis.SectionsOS.Append(sectionSaved);
			sectionSaved.VerseRefStart = sectionSaved.VerseRefMin = new BCVRef(1, 2, 1).BBCCCVVV;
			sectionSaved.VerseRefEnd = sectionSaved.VerseRefMax = new BCVRef(1, 2, 25).BBCCCVVV;
			string sDetails;
			List<IScrSection> sectionsToRemove;
			List<int> missingBtWs;
			Assert.AreEqual(OverwriteType.Partial,
				genesis.DetermineOverwritability(savedGenesis, out sDetails, out sectionsToRemove,
					out missingBtWs),
				"Can perform a partial overwrite with saved book that contains a hole matched by a current section");
			Assert.AreEqual(2, sectionsToRemove.Count);
			Assert.AreEqual(genesis.SectionsOS.HvoArray[0], sectionsToRemove[0].Hvo);
			Assert.AreEqual(genesis.SectionsOS.HvoArray[2], sectionsToRemove[1].Hvo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Partial overwrite is possible when the saved version has a hole that is exactly
		/// covered by a section in the current version but also has another section beyond the
		/// extent of the Scripture sections in the current version. This is a special case of
		/// partial overwrite where no sections need to be removed from the current before
		/// applying the overwrite. (This would never happen during import because the
		/// auto-merge code would detect this, but it can happen when using the Saved and
		/// Imported Versions dialog box because there we don't attempt to auto-merge.)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DetermineOverwritability_Partial_SavedHasHolesAndExtendsBeyondCurrent()
		{
			DummyScrBook genesis = new DummyScrBook(Cache,
				m_scrInMemoryCache.AddBookToMockedScripture(1, "Genesis").Hvo);
			ScrSection sectionCur = new ScrSection();
			genesis.SectionsOS.Append(sectionCur);
			sectionCur.VerseRefStart = sectionCur.VerseRefMin = new BCVRef(1, 1, 2).BBCCCVVV;
			sectionCur.VerseRefEnd = sectionCur.VerseRefMax = new BCVRef(1, 1, 8).BBCCCVVV;

			DummyScrBook savedGenesis = new DummyScrBook(Cache,
				m_scrInMemoryCache.AddArchiveBookToMockedScripture(1, "Genesis").Hvo);
			ScrSection sectionSaved = new ScrSection();
			savedGenesis.SectionsOS.Append(sectionSaved);
			sectionSaved.VerseRefStart = sectionSaved.VerseRefMin = new BCVRef(1, 1, 1).BBCCCVVV;
			sectionSaved.VerseRefEnd = sectionSaved.VerseRefMax = new BCVRef(1, 1, 1).BBCCCVVV;
			sectionSaved = new ScrSection();
			savedGenesis.SectionsOS.Append(sectionSaved);
			sectionSaved.VerseRefStart = sectionSaved.VerseRefMin = new BCVRef(1, 1, 9).BBCCCVVV;
			sectionSaved.VerseRefEnd = sectionSaved.VerseRefMax = new BCVRef(1, 1, 11).BBCCCVVV;
			string sDetails;
			List<IScrSection> sectionsToRemove;
			List<int> missingBtWs;
			Assert.AreEqual(OverwriteType.Partial,
				genesis.DetermineOverwritability(savedGenesis, out sDetails, out sectionsToRemove,
					out missingBtWs),
				"Can perform a partial overwrite with saved book that contains a hole matched by a current section");
			Assert.AreEqual(0, sectionsToRemove.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overwrite would be partial when the saved version has no Scripture (just
		/// intro), but the current version has an intro and Scripture. (The current intro
		/// would be overwritten).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DetermineOverwritability_Partial_SavedHasNoScripture()
		{
			DummyScrBook genesis = new DummyScrBook(Cache,
				m_scrInMemoryCache.AddBookToMockedScripture(1, "Genesis").Hvo);
			ScrSection introCur = new ScrSection();
			genesis.SectionsOS.Append(introCur);
			introCur.VerseRefStart = introCur.VerseRefMin = new BCVRef(1, 1, 0).BBCCCVVV;
			introCur.VerseRefEnd = introCur.VerseRefMax = new BCVRef(1, 1, 0).BBCCCVVV;
			ScrSection section1cur = new ScrSection();
			genesis.SectionsOS.Append(section1cur);
			section1cur.VerseRefStart = section1cur.VerseRefMin = new BCVRef(1, 1, 1).BBCCCVVV;
			section1cur.VerseRefEnd = section1cur.VerseRefMax = new BCVRef(1, 3, 20).BBCCCVVV;

			DummyScrBook savedGenesis = new DummyScrBook(Cache,
				m_scrInMemoryCache.AddArchiveBookToMockedScripture(1, "Genesis").Hvo);
			ScrSection section1saved = new ScrSection();
			savedGenesis.SectionsOS.Append(section1saved);
			section1saved.VerseRefStart = section1saved.VerseRefMin = new BCVRef(1, 1, 0).BBCCCVVV;
			section1saved.VerseRefEnd = section1saved.VerseRefMax = new BCVRef(1, 1, 0).BBCCCVVV;
			string sDetails;
			List<IScrSection> sectionsToRemove;
			List<int> missingBtWs;
			Assert.AreEqual(OverwriteType.Partial,
				genesis.DetermineOverwritability(savedGenesis, out sDetails, out sectionsToRemove,
					out missingBtWs),
				"Can overwrite current version of Scripture with saved book that only has an intro");
			Assert.AreEqual(1, sectionsToRemove.Count);
			Assert.AreEqual(sectionsToRemove[0].Hvo, introCur.Hvo);
			Assert.IsNull(sDetails);
		}
		#endregion

		#region FindMissingBt (used in DetermineOverwritability) tests for back translation
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests FindMissingBt when neither the current book nor the saved version have back
		/// translations.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindMissingBt_NoWs()
		{
			// Set up Genesis and a revision of Genesis with no back translations.
			DummyScrBook book = new DummyScrBook(Cache,
				m_scrInMemoryCache.AddBookToMockedScripture(1, "Genesis").Hvo);
			DummyScrBook savedVersion = new DummyScrBook(Cache,
				m_scrInMemoryCache.AddArchiveBookToMockedScripture(1, "Genesis").Hvo);

			// Determine writing systems in use in Genesis, but not in its revision.
			List<int> missingBts = book.CallFindMissingBts(savedVersion);

			// We expect that there would be no missing writing systems in the revision.
			Assert.IsNull(missingBts);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests FindMissingBt when the current book and the saved version use the same back
		/// translation writing systems.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindMissingBt_WsSame()
		{
			// Set up Genesis and a revision of Genesis each using the same back translation
			// "writing system".
			DummyScrBook book = new DummyScrBook(Cache,
				m_scrInMemoryCache.AddBookToMockedScripture(1, "Genesis").Hvo);
			book.m_btWs = new List<int>(new int[] {1});
			DummyScrBook savedVersion = new DummyScrBook(Cache,
				m_scrInMemoryCache.AddArchiveBookToMockedScripture(1, "Genesis").Hvo);
			savedVersion.m_btWs = new List<int>(new int[] { 1 });

			// Determine writing systems in use in Genesis, but not in its revision.
			List<int> missingBts = book.CallFindMissingBts(savedVersion);

			// We expect that there would be no missing writing systems in the revision.
			Assert.IsNull(missingBts);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests FindMissingBt when the saved version uses BT writing systems that are a subset
		/// of the BT writing systems used in this book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindMissingBt_VersionSubset()
		{
			// Set up Genesis and a revision of Genesis where the back translation "writing systems"
			// in use in the revision is a subset of those in use in Genesis.
			DummyScrBook book = new DummyScrBook(Cache,
				m_scrInMemoryCache.AddBookToMockedScripture(1, "Genesis").Hvo);
			book.m_btWs = new List<int>(new int[] { 1, 2, 3 });
			DummyScrBook savedVersion = new DummyScrBook(Cache,
				m_scrInMemoryCache.AddArchiveBookToMockedScripture(1, "Genesis").Hvo);
			savedVersion.m_btWs = new List<int>(new int[] { 1 });

			// Determine writing systems in use in Genesis, but not in its revision.
			List<int> missingBts = book.CallFindMissingBts(savedVersion);

			// We expect that there would be two missing writing systems in the revision.
			Assert.IsNotNull(missingBts);
			Assert.AreEqual(2, missingBts.Count);
			Assert.IsTrue(missingBts.Contains(2));
			Assert.IsTrue(missingBts.Contains(3));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests FindMissingBt when the current book uses BT writing systems that are a subset
		/// of the BT writing systems used in the saved version.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindMissingBt_CurrentSubset()
		{
			// Set up Genesis and a revision of Genesis where the back translation "writing systems"
			// in use in Genesis is a subset of those in use in the revision.
			DummyScrBook book = new DummyScrBook(Cache,
				m_scrInMemoryCache.AddBookToMockedScripture(1, "Genesis").Hvo);
			book.m_btWs = new List<int>(new int[] { 1 });
			DummyScrBook savedVersion = new DummyScrBook(Cache,
				m_scrInMemoryCache.AddArchiveBookToMockedScripture(1, "Genesis").Hvo);
			savedVersion.m_btWs = new List<int>(new int[] { 1, 2, 3 });

			// Determine writing systems in use in Genesis, but not in its revision.
			List<int> missingBts = book.CallFindMissingBts(savedVersion);

			// We expect that there would be no missing writing systems in the revision.
			Assert.IsNull(missingBts);
		}
		#endregion

		#endregion

		#region RemoveSections tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests removing all sections in a book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RemoveSections_All()
		{
			ScrBook genesis = (ScrBook)m_scrInMemoryCache.AddBookToMockedScripture(1, "Genesis");
			IScrSection sectionCur1 = CreateSection(genesis, "Section 1");
			AddEmptyPara(sectionCur1);
			IScrSection sectionCur2 = CreateSection(genesis, "Section 2");
			AddEmptyPara(sectionCur2);
			IScrSection sectionCur3 = CreateSection(genesis, "Section 3");
			AddEmptyPara(sectionCur3);

			List<IScrSection> sectionsToRemove = new List<IScrSection>();
			sectionsToRemove.Add(sectionCur1);
			sectionsToRemove.Add(sectionCur2);
			sectionsToRemove.Add(sectionCur3);

			Assert.AreEqual(3, genesis.SectionsOS.Count);
			genesis.RemoveSections(sectionsToRemove);
			Assert.AreEqual(0, genesis.SectionsOS.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests removing only the first section of a book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RemoveSections_AtStart()
		{
			ScrBook genesis = (ScrBook)m_scrInMemoryCache.AddBookToMockedScripture(1, "Genesis");
			IScrSection sectionCur1 = CreateSection(genesis, "Section 1");
			AddEmptyPara(sectionCur1);
			IScrSection sectionCur2 = CreateSection(genesis, "Section 2");
			AddEmptyPara(sectionCur2);
			IScrSection sectionCur3 = CreateSection(genesis, "Section 3");
			AddEmptyPara(sectionCur3);

			List<IScrSection> sectionsToRemove = new List<IScrSection>();
			sectionsToRemove.Add(sectionCur1);

			Assert.AreEqual(3, genesis.SectionsOS.Count);
			genesis.RemoveSections(sectionsToRemove);
			Assert.AreEqual(2, genesis.SectionsOS.Count);
			Assert.AreEqual(genesis.SectionsOS[0].Hvo, sectionCur2.Hvo);
			Assert.AreEqual(genesis.SectionsOS[1].Hvo, sectionCur3.Hvo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests removing only the last section of a book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RemoveSections_AtEnd()
		{
			ScrBook genesis = (ScrBook)m_scrInMemoryCache.AddBookToMockedScripture(1, "Genesis");
			IScrSection sectionCur1 = CreateSection(genesis, "Section 1");
			AddEmptyPara(sectionCur1);
			IScrSection sectionCur2 = CreateSection(genesis, "Section 2");
			AddEmptyPara(sectionCur2);
			IScrSection sectionCur3 = CreateSection(genesis, "Section 3");
			AddEmptyPara(sectionCur3);

			List<IScrSection> sectionsToRemove = new List<IScrSection>();
			sectionsToRemove.Add(sectionCur3);

			Assert.AreEqual(3, genesis.SectionsOS.Count);
			genesis.RemoveSections(sectionsToRemove);
			Assert.AreEqual(2, genesis.SectionsOS.Count);
			Assert.AreEqual(genesis.SectionsOS[0].Hvo, sectionCur1.Hvo);
			Assert.AreEqual(genesis.SectionsOS[1].Hvo, sectionCur2.Hvo);
		}
		#endregion
	}
}
