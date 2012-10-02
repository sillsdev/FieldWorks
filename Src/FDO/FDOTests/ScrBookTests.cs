// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2003' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ScrBookTests.cs
// Responsibility: TE Team
// --------------------------------------------------------------------------------------------
using System;

using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Test.TestUtils;
using SILUBS.SharedScrUtils;
using System.Collections.Generic;
using SIL.Utils;
using SIL.FieldWorks.FDO.DomainImpl;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.CoreImpl;

namespace SIL.FieldWorks.FDO.FDOTests
{
	#region DummyScrBook
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// DummyScrBook allows tests to use the in-memory cache for tests that rely on BackTransWs,
	/// which normally requires a real cache.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DummyScrBook : ScrBook
	{
		/// <summary>hvos of the writing systems for the back translations, which can be set
		/// from tests.</summary>
		public HashSet<int> m_btWs = null;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets hvos of the writing systems for the back translations used in this book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override HashSet<int> BackTransWs
		{
			get { return m_btWs; }
		}
	}
	#endregion

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests the ScrBook class
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class ScrBookTests : ScrInMemoryFdoTestBase
	{
		private IWritingSystem m_wsGerman;
		private IWritingSystem m_wsFrench;
		private IWritingSystem m_wsSpanish;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Does setup for all the tests
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("de", out m_wsGerman);
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("fr", out m_wsFrench);
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("es", out m_wsSpanish);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test funcionality of IScrBookFactory.Create.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Create()
		{
			int cBooksOrig = m_scr.ScriptureBooksOS.Count;
			IStText title;

			IScrBook genesis = m_scr.FindBook("GEN");
			Assert.IsNull(genesis, "Genesis should not exist in TestLangProj.");
			// insert Genesis (before existing books)
			genesis = Cache.ServiceLocator.GetInstance<IScrBookFactory>().Create(1,
				out title);
			Assert.IsNotNull(genesis, "Genesis was not created.");
			Assert.AreEqual(cBooksOrig + 1, m_scr.ScriptureBooksOS.Count);
			Assert.AreEqual("GEN", ((ScrBook)m_scr.ScriptureBooksOS[0]).BookId,
				"Genesis not first book in ScriptureBooksOS");
			Assert.IsTrue(genesis.TitleOA.IsValidObject);
			Assert.AreEqual(title, genesis.TitleOA);

			IScrBook revelation = m_scr.FindBook("REV");
			Assert.IsNull(revelation, "Revelation should not exist in TestLangProj.");
			// insert Revelation (after current books)
			revelation = Cache.ServiceLocator.GetInstance<IScrBookFactory>().Create(
				BCVRef.LastBook, out title);
			Assert.IsNotNull(revelation, "Revelation was not created.");
			Assert.AreEqual(cBooksOrig + 2, m_scr.ScriptureBooksOS.Count);
			Assert.AreEqual("REV", ((ScrBook)m_scr.ScriptureBooksOS[cBooksOrig + 2 - 1]).BookId,
				"Revelation not last book in ScriptureBooksOS");
			Assert.IsTrue(revelation.TitleOA.IsValidObject);
			Assert.AreEqual(title, revelation.TitleOA);

			IScrBook leviticus = m_scr.FindBook("LEV");
			Assert.IsNull(leviticus, "Leviticus should not exist in TestLangProj.");
			// insert Leviticus (middle of current books)
			leviticus = Cache.ServiceLocator.GetInstance<IScrBookFactory>().Create(3,
				out title);
			Assert.IsNotNull(leviticus, "Leviticus was not created.");
			Assert.AreEqual(cBooksOrig + 3, m_scr.ScriptureBooksOS.Count);
			Assert.AreEqual("LEV", ((ScrBook)m_scr.ScriptureBooksOS[1]).BookId,
				"Leviticus not second book in ScriptureBooksOS");
			Assert.IsTrue(leviticus.TitleOA.IsValidObject);
			Assert.AreEqual(title, leviticus.TitleOA);
		}

		///  ------------------------------------------------------------------------------------
		/// <summary>
		/// Verify operation of BestUIName property.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BestUIName()
		{
			IScrBook book = AddBookToMockedScripture(3, "Leviticus");

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
		/// Test funcionality of InitTitlePara.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InitTitlePara()
		{
			// insert Genesis
			IScrBook genesis = AddBookToMockedScripture(1, "Genesis");
			Assert.IsNull(genesis.TitleOA);
			genesis.TitleOA = Cache.ServiceLocator.GetInstance<IStTextFactory>().Create();

			genesis.InitTitlePara();

			//verify the title para in Genesis
			Assert.IsTrue(genesis.TitleOA.IsValidObject);
			Assert.AreEqual(1, genesis.TitleOA.ParagraphsOS.Count);
			IStTxtPara para = (IStTxtPara)genesis.TitleOA.ParagraphsOS[0];
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

			Assert.AreEqual(1, para.Contents.RunCount);
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
			IScrBook genesis = AddBookToMockedScripture(1, "Genesis");
			IScrSection section1cur = AddSectionToMockedBook(genesis);
			section1cur.VerseRefStart = section1cur.VerseRefMin = new BCVRef(1, 1, 1).BBCCCVVV;
			section1cur.VerseRefEnd = section1cur.VerseRefMax = new BCVRef(1, 1, 20).BBCCCVVV;

			IScrBook savedGenesis = AddArchiveBookToMockedScripture(1, "Genesis");
			IScrSection section1saved = AddSectionToMockedBook(savedGenesis);
			section1saved.VerseRefStart = section1saved.VerseRefMin = new BCVRef(1, 1, 1).BBCCCVVV;
			section1saved.VerseRefEnd = section1saved.VerseRefMax = new BCVRef(1, 1, 27).BBCCCVVV;
			string sDetails;
			List<IScrSection> sectionsToRemove;
			HashSet<int> missingBtWs;
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
			IScrBook genesis = AddBookToMockedScripture(1, "Genesis");
			IScrSection section1cur = AddSectionToMockedBook(genesis);
			section1cur.VerseRefStart = section1cur.VerseRefMin = new BCVRef(1, 2, 1).BBCCCVVV;
			section1cur.VerseRefEnd = section1cur.VerseRefMax = new BCVRef(1, 2, 10).BBCCCVVV;

			IScrBook savedGenesis = AddArchiveBookToMockedScripture(1, "Genesis");
			IScrSection section1saved = AddSectionToMockedBook(savedGenesis);
			section1saved.VerseRefStart = section1saved.VerseRefMin = new BCVRef(1, 1, 1).BBCCCVVV;
			section1saved.VerseRefEnd = section1saved.VerseRefMax = new BCVRef(1, 2, 10).BBCCCVVV;
			string sDetails;
			List<IScrSection> sectionsToRemove;
			HashSet<int> missingBtWs;
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
			IScrBook genesis = AddBookToMockedScripture(1, "Genesis");
			IScrSection section1cur = AddSectionToMockedBook(genesis);
			section1cur.VerseRefStart = section1cur.VerseRefMin = new BCVRef(1, 2, 1).BBCCCVVV;
			section1cur.VerseRefEnd = section1cur.VerseRefMax = new BCVRef(1, 2, 10).BBCCCVVV;

			IScrBook savedGenesis = AddArchiveBookToMockedScripture(1, "Genesis");
			IScrSection section1saved = AddSectionToMockedBook(savedGenesis);
			section1saved.VerseRefStart = section1saved.VerseRefMin = new BCVRef(1, 1, 1).BBCCCVVV;
			section1saved.VerseRefEnd = section1saved.VerseRefMax = new BCVRef(1, 2, 18).BBCCCVVV;
			string sDetails;
			List<IScrSection> sectionsToRemove;
			HashSet<int> missingBtWs;
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
			IScrBook genesis = AddBookToMockedScripture(1, "Genesis");
			IScrSection introCur1 = AddSectionToMockedBook(genesis);
			introCur1.VerseRefStart = introCur1.VerseRefMin = new BCVRef(1, 1, 0).BBCCCVVV;
			introCur1.VerseRefEnd = introCur1.VerseRefMax = new BCVRef(1, 1, 0).BBCCCVVV;
			IScrSection introCur2 = AddSectionToMockedBook(genesis);
			introCur2.VerseRefStart = introCur2.VerseRefMin = new BCVRef(1, 1, 0).BBCCCVVV;
			introCur2.VerseRefEnd = introCur2.VerseRefMax = new BCVRef(1, 1, 0).BBCCCVVV;

			IScrBook savedGenesis = AddArchiveBookToMockedScripture(1, "Genesis");
			IScrSection section1saved = AddSectionToMockedBook(savedGenesis);
			section1saved.VerseRefStart = section1saved.VerseRefMin = new BCVRef(1, 1, 0).BBCCCVVV;
			section1saved.VerseRefEnd = section1saved.VerseRefMax = new BCVRef(1, 1, 0).BBCCCVVV;
			string sDetails;
			List<IScrSection> sectionsToRemove;
			HashSet<int> missingBtWs;
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
			IScrBook genesis = AddBookToMockedScripture(1, "Genesis");
			IScrSection section1Cur = AddSectionToMockedBook(genesis);
			section1Cur.VerseRefStart = section1Cur.VerseRefMin = new BCVRef(1, 1, 1).BBCCCVVV;
			section1Cur.VerseRefEnd = section1Cur.VerseRefMax = new BCVRef(1, 1, 31).BBCCCVVV;
			IScrSection section2Cur = AddSectionToMockedBook(genesis);
			section2Cur.VerseRefStart = section2Cur.VerseRefMin = new BCVRef(1, 2, 1).BBCCCVVV;
			section2Cur.VerseRefEnd = section2Cur.VerseRefMax = new BCVRef(1, 2, 20).BBCCCVVV;

			IScrBook savedGenesis = AddArchiveBookToMockedScripture(1, "Genesis");
			IScrSection introSaved1 = AddSectionToMockedBook(savedGenesis);
			introSaved1.VerseRefStart = introSaved1.VerseRefMin = new BCVRef(1, 1, 0).BBCCCVVV;
			introSaved1.VerseRefEnd = introSaved1.VerseRefMax = new BCVRef(1, 1, 0).BBCCCVVV;
			IScrSection section1Saved = AddSectionToMockedBook(savedGenesis);
			section1Saved.VerseRefStart = section1Saved.VerseRefMin = new BCVRef(1, 1, 1).BBCCCVVV;
			section1Saved.VerseRefEnd = section1Saved.VerseRefMax = new BCVRef(1, 1, 31).BBCCCVVV;
			IScrSection section2Saved = AddSectionToMockedBook(savedGenesis);
			section2Saved.VerseRefStart = section2Saved.VerseRefMin = new BCVRef(1, 2, 1).BBCCCVVV;
			section2Saved.VerseRefEnd = section2Saved.VerseRefMax = new BCVRef(1, 2, 20).BBCCCVVV;

			string sDetails;
			List<IScrSection> sectionsToRemove;
			HashSet<int> missingBtWs;
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
			IScrBook genesis = AddBookToMockedScripture(1, "Genesis");
			IScrSection sectionCur = AddSectionToMockedBook(genesis);
			sectionCur.VerseRefStart = sectionCur.VerseRefMin = new BCVRef(1, 1, 1).BBCCCVVV;
			sectionCur.VerseRefEnd = sectionCur.VerseRefMax = new BCVRef(1, 1, 15).BBCCCVVV;
			sectionCur = AddSectionToMockedBook(genesis);
			sectionCur.VerseRefStart = sectionCur.VerseRefMin = new BCVRef(1, 1, 19).BBCCCVVV;
			sectionCur.VerseRefEnd = sectionCur.VerseRefMax = new BCVRef(1, 1, 27).BBCCCVVV;

			IScrBook savedGenesis = AddArchiveBookToMockedScripture(1, "Genesis");
			IScrSection sectionSaved = AddSectionToMockedBook(savedGenesis);
			sectionSaved.VerseRefStart = sectionSaved.VerseRefMin = new BCVRef(1, 1, 1).BBCCCVVV;
			sectionSaved.VerseRefEnd = sectionSaved.VerseRefMax = new BCVRef(1, 1, 10).BBCCCVVV;
			sectionSaved = AddSectionToMockedBook(savedGenesis);
			sectionSaved.VerseRefStart = sectionSaved.VerseRefMin = new BCVRef(1, 1, 11).BBCCCVVV;
			sectionSaved.VerseRefEnd = sectionSaved.VerseRefMax = new BCVRef(1, 1, 15).BBCCCVVV;
			sectionSaved = AddSectionToMockedBook(savedGenesis);
			sectionSaved.VerseRefStart = sectionSaved.VerseRefMin = new BCVRef(1, 1, 19).BBCCCVVV;
			sectionSaved.VerseRefEnd = sectionSaved.VerseRefMax = new BCVRef(1, 1, 27).BBCCCVVV;
			string sDetails;
			List<IScrSection> sectionsToRemove;
			HashSet<int> missingBtWs;
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
			IScrBook genesis = AddBookToMockedScripture(1, "Genesis");
			IScrSection sectionCur = AddSectionToMockedBook(genesis);
			sectionCur.VerseRefStart = sectionCur.VerseRefMin = new BCVRef(1, 1, 1).BBCCCVVV;
			sectionCur.VerseRefEnd = sectionCur.VerseRefMax = new BCVRef(1, 1, 30).BBCCCVVV;
			sectionCur = AddSectionToMockedBook(genesis);
			sectionCur.VerseRefStart = sectionCur.VerseRefMin = new BCVRef(1, 2, 1).BBCCCVVV;
			sectionCur.VerseRefEnd = sectionCur.VerseRefMax = new BCVRef(1, 2, 25).BBCCCVVV;
			// Since we want identical holes and section ranges, we can pass the exact same
			// ScrBook object as both the current and the revision.
			string sDetails;
			List<IScrSection> sectionsToRemove;
			HashSet<int> missingBtWs;
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
			IScrBook genesis = AddBookToMockedScripture(1, "Genesis");
			IScrSection introCur = AddSectionToMockedBook(genesis);
			introCur.VerseRefStart = introCur.VerseRefMin = new BCVRef(1, 1, 0).BBCCCVVV;
			introCur.VerseRefEnd = introCur.VerseRefMax = new BCVRef(1, 1, 0).BBCCCVVV;

			IScrBook savedGenesis = AddArchiveBookToMockedScripture(1, "Genesis");
			IScrSection section1saved = AddSectionToMockedBook(savedGenesis);
			section1saved.VerseRefStart = section1saved.VerseRefMin = new BCVRef(1, 1, 0).BBCCCVVV;
			section1saved.VerseRefEnd = section1saved.VerseRefMax = new BCVRef(1, 1, 0).BBCCCVVV;
			IScrSection section2saved = AddSectionToMockedBook(savedGenesis);
			section2saved.VerseRefStart = section2saved.VerseRefMin = new BCVRef(1, 1, 1).BBCCCVVV;
			section2saved.VerseRefEnd = section2saved.VerseRefMax = new BCVRef(1, 1, 15).BBCCCVVV;
			string sDetails;
			List<IScrSection> sectionsToRemove;
			HashSet<int> missingBtWs;
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
			IScrBook genesis = AddBookToMockedScripture(1, "Genesis");
			IScrSection section1Cur = AddSectionToMockedBook(genesis);
			section1Cur.VerseRefStart = section1Cur.VerseRefMin = new BCVRef(1, 1, 1).BBCCCVVV;
			section1Cur.VerseRefEnd = section1Cur.VerseRefMax = new BCVRef(1, 1, 5).BBCCCVVV;
			IScrSection section2Cur = AddSectionToMockedBook(genesis);
			section2Cur.VerseRefStart = section2Cur.VerseRefMin = new BCVRef(1, 1, 1).BBCCCVVV;
			section2Cur.VerseRefEnd = section2Cur.VerseRefMax = new BCVRef(1, 1, 20).BBCCCVVV;

			IScrBook savedGenesis = AddArchiveBookToMockedScripture(1, "Genesis");
			IScrSection section1Saved = AddSectionToMockedBook(savedGenesis);
			section1Saved.VerseRefStart = section1Saved.VerseRefMin = new BCVRef(1, 1, 1).BBCCCVVV;
			section1Saved.VerseRefEnd = section1Saved.VerseRefMax = new BCVRef(1, 1, 20).BBCCCVVV;

			string sDetails;
			List<IScrSection> sectionsToRemove;
			HashSet<int> missingBtWs;
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
			IScrBook genesis = AddBookToMockedScripture(1, "Genesis");
			IScrSection section1cur = AddSectionToMockedBook(genesis);
			section1cur.VerseRefStart = section1cur.VerseRefMin = new BCVRef(1, 1, 1).BBCCCVVV;
			section1cur.VerseRefEnd = section1cur.VerseRefMax = new BCVRef(1, 1, 20).BBCCCVVV;

			IScrBook savedGenesis = AddArchiveBookToMockedScripture(1, "Genesis");
			IScrSection section1saved = AddSectionToMockedBook(savedGenesis);
			section1saved.VerseRefStart = section1saved.VerseRefMin = new BCVRef(1, 1, 6).BBCCCVVV;
			section1saved.VerseRefEnd = section1saved.VerseRefMax = new BCVRef(1, 1, 20).BBCCCVVV;
			string sDetails;
			List<IScrSection> sectionsToRemove;
			HashSet<int> missingBtWs;
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
			IScrBook genesis = AddBookToMockedScripture(1, "Genesis");
			IScrSection section1cur = AddSectionToMockedBook(genesis);
			section1cur.VerseRefStart = section1cur.VerseRefMin = new BCVRef(1, 1, 1).BBCCCVVV;
			section1cur.VerseRefEnd = section1cur.VerseRefMax = new BCVRef(1, 1, 20).BBCCCVVV;

			IScrBook savedGenesis = AddArchiveBookToMockedScripture(1, "Genesis");
			IScrSection section1saved = AddSectionToMockedBook(savedGenesis);
			section1saved.VerseRefStart = section1saved.VerseRefMin = new BCVRef(1, 1, 1).BBCCCVVV;
			section1saved.VerseRefEnd = section1saved.VerseRefMax = new BCVRef(1, 1, 15).BBCCCVVV;
			string sDetails;
			List<IScrSection> sectionsToRemove;
			HashSet<int> missingBtWs;
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
			IScrBook genesis = AddBookToMockedScripture(1, "Genesis");
			IScrSection section1cur = AddSectionToMockedBook(genesis);
			section1cur.VerseRefStart = section1cur.VerseRefMin = new BCVRef(1, 1, 1).BBCCCVVV;
			section1cur.VerseRefEnd = section1cur.VerseRefMax = new BCVRef(1, 1, 20).BBCCCVVV;

			IScrBook savedGenesis = AddArchiveBookToMockedScripture(1, "Genesis");
			IScrSection section1saved = AddSectionToMockedBook(savedGenesis);
			section1saved.VerseRefStart = section1saved.VerseRefMin = new BCVRef(1, 1, 5).BBCCCVVV;
			section1saved.VerseRefEnd = section1saved.VerseRefMax = new BCVRef(1, 1, 15).BBCCCVVV;
			string sDetails;
			List<IScrSection> sectionsToRemove;
			HashSet<int> missingBtWs;
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
			IScrBook genesis = AddBookToMockedScripture(1, "Genesis");
			IScrSection section1cur = AddSectionToMockedBook(genesis);
			section1cur.VerseRefStart = section1cur.VerseRefMin = new BCVRef(1, 1, 1).BBCCCVVV;
			section1cur.VerseRefEnd = section1cur.VerseRefMax = new BCVRef(1, 1, 13).BBCCCVVV;

			IScrBook savedGenesis = AddArchiveBookToMockedScripture(1, "Genesis");
			IScrSection section1saved = AddSectionToMockedBook(savedGenesis);
			section1saved.VerseRefStart = section1saved.VerseRefMin = new BCVRef(1, 1, 1).BBCCCVVV;
			section1saved.VerseRefEnd = section1saved.VerseRefMax = new BCVRef(1, 1, 12).BBCCCVVV;
			string sDetails;
			List<IScrSection> sectionsToRemove;
			HashSet<int> missingBtWs;
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
			IScrBook genesis = AddBookToMockedScripture(1, "Genesis");
			IScrSection section1cur = AddSectionToMockedBook(genesis);
			section1cur.VerseRefStart = section1cur.VerseRefMin = new BCVRef(1, 1, 1).BBCCCVVV;
			section1cur.VerseRefEnd = section1cur.VerseRefMax = new BCVRef(1, 1, 12).BBCCCVVV;

			IScrBook savedGenesis = AddArchiveBookToMockedScripture(1, "Genesis");
			IScrSection section1saved = AddSectionToMockedBook(savedGenesis);
			section1saved.VerseRefStart = section1saved.VerseRefMin = new BCVRef(1, 1, 2).BBCCCVVV;
			section1saved.VerseRefEnd = section1saved.VerseRefMax = new BCVRef(1, 1, 12).BBCCCVVV;
			string sDetails;
			List<IScrSection> sectionsToRemove;
			HashSet<int> missingBtWs;
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
			IScrBook genesis = AddBookToMockedScripture(1, "Genesis");
			IScrSection section1cur = AddSectionToMockedBook(genesis);
			section1cur.VerseRefStart = section1cur.VerseRefMin = new BCVRef(1, 1, 1).BBCCCVVV;
			section1cur.VerseRefEnd = section1cur.VerseRefMax = new BCVRef(1, 1, 12).BBCCCVVV;
			IScrSection section2cur = AddSectionToMockedBook(genesis);
			section2cur.VerseRefStart = section2cur.VerseRefMin = new BCVRef(1, 2, 1).BBCCCVVV;
			section2cur.VerseRefEnd = section2cur.VerseRefMax = new BCVRef(1, 2, 13).BBCCCVVV;

			IScrBook savedGenesis = AddArchiveBookToMockedScripture(1, "Genesis");
			IScrSection section1saved = AddSectionToMockedBook(savedGenesis);
			section1saved.VerseRefStart = section1saved.VerseRefMin = new BCVRef(1, 2, 1).BBCCCVVV;
			section1saved.VerseRefEnd = section1saved.VerseRefMax = new BCVRef(1, 2, 12).BBCCCVVV;
			string sDetails;
			List<IScrSection> sectionsToRemove;
			HashSet<int> missingBtWs;
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
			IScrBook genesis = AddBookToMockedScripture(1, "Genesis");
			IScrSection section1cur = AddSectionToMockedBook(genesis);
			section1cur.VerseRefStart = section1cur.VerseRefMin = new BCVRef(1, 1, 1).BBCCCVVV;
			section1cur.VerseRefEnd = section1cur.VerseRefMax = new BCVRef(1, 1, 15).BBCCCVVV;
			IScrSection section2cur = AddSectionToMockedBook(genesis);
			section2cur.VerseRefStart = section2cur.VerseRefMin = new BCVRef(1, 1, 16).BBCCCVVV;
			section2cur.VerseRefEnd = section2cur.VerseRefMax = new BCVRef(1, 1, 19).BBCCCVVV;
			IScrSection section3cur = AddSectionToMockedBook(genesis);
			section3cur.VerseRefStart = section3cur.VerseRefMin = new BCVRef(1, 1, 20).BBCCCVVV;
			section3cur.VerseRefEnd = section3cur.VerseRefMax = new BCVRef(1, 1, 31).BBCCCVVV;
			IScrSection section4cur = AddSectionToMockedBook(genesis);
			section4cur.VerseRefStart = section4cur.VerseRefMin = new BCVRef(1, 2, 1).BBCCCVVV;
			section4cur.VerseRefEnd = section4cur.VerseRefMax = new BCVRef(1, 2, 13).BBCCCVVV;

			IScrBook savedGenesis = AddArchiveBookToMockedScripture(1, "Genesis");
			IScrSection section1saved = AddSectionToMockedBook(savedGenesis);
			section1saved.VerseRefStart = section1saved.VerseRefMin = new BCVRef(1, 1, 16).BBCCCVVV;
			section1saved.VerseRefEnd = section1saved.VerseRefMax = new BCVRef(1, 1, 19).BBCCCVVV;
			IScrSection section2saved = AddSectionToMockedBook(savedGenesis);
			section2saved.VerseRefStart = section2saved.VerseRefMin = new BCVRef(1, 2, 1).BBCCCVVV;
			section2saved.VerseRefEnd = section2saved.VerseRefMax = new BCVRef(1, 2, 12).BBCCCVVV;
			string sDetails;
			List<IScrSection> sectionsToRemove;
			HashSet<int> missingBtWs;
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
			IScrBook genesis = AddBookToMockedScripture(1, "Genesis");
			IScrSection sectionCur = AddSectionToMockedBook(genesis);
			sectionCur.VerseRefStart = sectionCur.VerseRefMin = new BCVRef(1, 1, 1).BBCCCVVV;
			sectionCur.VerseRefEnd = sectionCur.VerseRefMax = new BCVRef(1, 1, 15).BBCCCVVV;
			sectionCur = AddSectionToMockedBook(genesis);
			sectionCur.VerseRefStart = sectionCur.VerseRefMin = new BCVRef(1, 1, 16).BBCCCVVV;
			sectionCur.VerseRefEnd = sectionCur.VerseRefMax = new BCVRef(1, 1, 31).BBCCCVVV;
			sectionCur = AddSectionToMockedBook(genesis);
			sectionCur.VerseRefStart = sectionCur.VerseRefMin = new BCVRef(1, 2, 1).BBCCCVVV;
			sectionCur.VerseRefEnd = sectionCur.VerseRefMax = new BCVRef(1, 2, 25).BBCCCVVV;

			IScrBook savedGenesis = AddArchiveBookToMockedScripture(1, "Genesis");
			IScrSection sectionSaved = AddSectionToMockedBook(savedGenesis);
			sectionSaved.VerseRefStart = sectionSaved.VerseRefMin = new BCVRef(1, 1, 1).BBCCCVVV;
			sectionSaved.VerseRefEnd = sectionSaved.VerseRefMax = new BCVRef(1, 1, 10).BBCCCVVV;
			sectionSaved = AddSectionToMockedBook(savedGenesis);
			sectionSaved.VerseRefStart = sectionSaved.VerseRefMin = new BCVRef(1, 1, 11).BBCCCVVV;
			sectionSaved.VerseRefEnd = sectionSaved.VerseRefMax = new BCVRef(1, 1, 18).BBCCCVVV;
			sectionSaved = AddSectionToMockedBook(savedGenesis);
			sectionSaved.VerseRefStart = sectionSaved.VerseRefMin = new BCVRef(1, 1, 22).BBCCCVVV;
			sectionSaved.VerseRefEnd = sectionSaved.VerseRefMax = new BCVRef(1, 1, 31).BBCCCVVV;
			sectionSaved = AddSectionToMockedBook(savedGenesis);
			sectionSaved.VerseRefStart = sectionSaved.VerseRefMin = new BCVRef(1, 2, 22).BBCCCVVV;
			sectionSaved.VerseRefEnd = sectionSaved.VerseRefMax = new BCVRef(1, 2, 25).BBCCCVVV;
			string sDetails;
			List<IScrSection> sectionsToRemove;
			HashSet<int> missingBtWs;
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
			IScrBook genesis = AddBookToMockedScripture(1, "Genesis");
			IScrSection sectionCur = AddSectionToMockedBook(genesis);
			sectionCur.VerseRefStart = sectionCur.VerseRefMin = new BCVRef(1, 1, 1).BBCCCVVV;
			sectionCur.VerseRefEnd = sectionCur.VerseRefMax = new BCVRef(1, 1, 15).BBCCCVVV;
			sectionCur = AddSectionToMockedBook(genesis);
			sectionCur.VerseRefStart = sectionCur.VerseRefMin = new BCVRef(1, 1, 18).BBCCCVVV;
			sectionCur.VerseRefEnd = sectionCur.VerseRefMax = new BCVRef(1, 1, 29).BBCCCVVV;
			sectionCur = AddSectionToMockedBook(genesis);
			sectionCur.VerseRefStart = sectionCur.VerseRefMin = new BCVRef(1, 2, 1).BBCCCVVV;
			sectionCur.VerseRefEnd = sectionCur.VerseRefMax = new BCVRef(1, 2, 25).BBCCCVVV;
			sectionCur = AddSectionToMockedBook(genesis);
			sectionCur.VerseRefStart = sectionCur.VerseRefMin = new BCVRef(1, 3, 4).BBCCCVVV;
			sectionCur.VerseRefEnd = sectionCur.VerseRefMax = new BCVRef(1, 3, 15).BBCCCVVV;
			sectionCur = AddSectionToMockedBook(genesis);
			sectionCur.VerseRefStart = sectionCur.VerseRefMin = new BCVRef(1, 3, 18).BBCCCVVV;
			sectionCur.VerseRefEnd = sectionCur.VerseRefMax = new BCVRef(1, 3, 24).BBCCCVVV;
			sectionCur = AddSectionToMockedBook(genesis);
			sectionCur.VerseRefStart = sectionCur.VerseRefMin = new BCVRef(1, 4, 1).BBCCCVVV;
			sectionCur.VerseRefEnd = sectionCur.VerseRefMax = new BCVRef(1, 4, 4).BBCCCVVV;
			sectionCur = AddSectionToMockedBook(genesis);
			sectionCur.VerseRefStart = sectionCur.VerseRefMin = new BCVRef(1, 4, 10).BBCCCVVV;
			sectionCur.VerseRefEnd = sectionCur.VerseRefMax = new BCVRef(1, 4, 18).BBCCCVVV;

			IScrBook savedGenesis = AddArchiveBookToMockedScripture(1, "Genesis");
			IScrSection sectionSaved = AddSectionToMockedBook(savedGenesis);
			sectionSaved.VerseRefStart = sectionSaved.VerseRefMin = new BCVRef(1, 1, 1).BBCCCVVV;
			sectionSaved.VerseRefEnd = sectionSaved.VerseRefMax = new BCVRef(1, 1, 10).BBCCCVVV;
			sectionSaved = AddSectionToMockedBook(savedGenesis);
			sectionSaved.VerseRefStart = sectionSaved.VerseRefMin = new BCVRef(1, 2, 22).BBCCCVVV;
			sectionSaved.VerseRefEnd = sectionSaved.VerseRefMax = new BCVRef(1, 2, 25).BBCCCVVV;
			sectionSaved = AddSectionToMockedBook(savedGenesis);
			sectionSaved.VerseRefStart = sectionSaved.VerseRefMin = new BCVRef(1, 3, 4).BBCCCVVV;
			sectionSaved.VerseRefEnd = sectionSaved.VerseRefMax = new BCVRef(1, 3, 15).BBCCCVVV;
			sectionSaved = AddSectionToMockedBook(savedGenesis);
			sectionSaved.VerseRefStart = sectionSaved.VerseRefMin = new BCVRef(1, 3, 18).BBCCCVVV;
			sectionSaved.VerseRefEnd = sectionSaved.VerseRefMax = new BCVRef(1, 3, 21).BBCCCVVV;
			sectionSaved = AddSectionToMockedBook(savedGenesis);
			sectionSaved.VerseRefStart = sectionSaved.VerseRefMin = new BCVRef(1, 4, 1).BBCCCVVV;
			sectionSaved.VerseRefEnd = sectionSaved.VerseRefMax = new BCVRef(1, 4, 6).BBCCCVVV;
			sectionSaved = AddSectionToMockedBook(savedGenesis);
			sectionSaved.VerseRefStart = sectionSaved.VerseRefMin = new BCVRef(1, 4, 8).BBCCCVVV;
			sectionSaved.VerseRefEnd = sectionSaved.VerseRefMax = new BCVRef(1, 4, 18).BBCCCVVV;
			string sDetails;
			List<IScrSection> sectionsToRemove;
			HashSet<int> missingBtWs;
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
			IScrBook genesis = AddBookToMockedScripture(1, "Genesis");
			IScrSection section1Cur = AddSectionToMockedBook(genesis);
			section1Cur.VerseRefStart = section1Cur.VerseRefMin = new BCVRef(1, 50, 1).BBCCCVVV;
			section1Cur.VerseRefEnd = section1Cur.VerseRefMax = new BCVRef(1, 50, 26).BBCCCVVV;
			IScrSection section2Cur = AddSectionToMockedBook(genesis);
			section2Cur.VerseRefStart = section2Cur.VerseRefMin = new BCVRef(1, 50, 12).BBCCCVVV;
			section2Cur.VerseRefEnd = section2Cur.VerseRefMax = new BCVRef(1, 50, 26).BBCCCVVV;

			IScrBook savedGenesis = AddArchiveBookToMockedScripture(1, "Genesis");
			IScrSection section1Saved = AddSectionToMockedBook(savedGenesis);
			section1Saved.VerseRefStart = section1Saved.VerseRefMin = new BCVRef(1, 50, 1).BBCCCVVV;
			section1Saved.VerseRefEnd = section1Saved.VerseRefMax = new BCVRef(1, 50, 26).BBCCCVVV;

			string sDetails;
			List<IScrSection> sectionsToRemove;
			HashSet<int> missingBtWs;
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
			IScrBook genesis = AddBookToMockedScripture(1, "Genesis");
			IScrSection introCur = AddSectionToMockedBook(genesis);
			introCur.VerseRefStart = introCur.VerseRefMin = new BCVRef(1, 1, 0).BBCCCVVV;
			introCur.VerseRefEnd = introCur.VerseRefMax = new BCVRef(1, 1, 0).BBCCCVVV;
			IScrSection section1cur = AddSectionToMockedBook(genesis);
			section1cur.VerseRefStart = section1cur.VerseRefMin = new BCVRef(1, 1, 1).BBCCCVVV;
			section1cur.VerseRefEnd = section1cur.VerseRefMax = new BCVRef(1, 1, 20).BBCCCVVV;

			IScrBook savedGenesis = AddArchiveBookToMockedScripture(1, "Genesis");
			IScrSection section1saved = AddSectionToMockedBook(savedGenesis);
			section1saved.VerseRefStart = section1saved.VerseRefMin = new BCVRef(1, 1, 1).BBCCCVVV;
			section1saved.VerseRefEnd = section1saved.VerseRefMax = new BCVRef(1, 1, 20).BBCCCVVV;
			string sDetails;
			List<IScrSection> sectionsToRemove;
			HashSet<int> missingBtWs;
			Assert.AreEqual(OverwriteType.Partial,
				genesis.DetermineOverwritability(savedGenesis, out sDetails, out sectionsToRemove,
					out missingBtWs),
				"Can do partial overwrite with saved book that does not have intro");
			Assert.AreEqual(1, sectionsToRemove.Count);
			Assert.AreEqual(genesis.SectionsOS[1].Hvo, sectionsToRemove[0].Hvo);
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
			IScrBook genesis = AddBookToMockedScripture(1, "Genesis");
			IScrSection sectionCur = AddSectionToMockedBook(genesis);
			sectionCur.VerseRefStart = sectionCur.VerseRefMin = new BCVRef(1, 1, 1).BBCCCVVV;
			sectionCur.VerseRefEnd = sectionCur.VerseRefMax = new BCVRef(1, 1, 15).BBCCCVVV;
			sectionCur = AddSectionToMockedBook(genesis);
			sectionCur.VerseRefStart = sectionCur.VerseRefMin = new BCVRef(1, 1, 16).BBCCCVVV;
			sectionCur.VerseRefEnd = sectionCur.VerseRefMax = new BCVRef(1, 1, 31).BBCCCVVV;
			sectionCur = AddSectionToMockedBook(genesis);
			sectionCur.VerseRefStart = sectionCur.VerseRefMin = new BCVRef(1, 2, 1).BBCCCVVV;
			sectionCur.VerseRefEnd = sectionCur.VerseRefMax = new BCVRef(1, 2, 25).BBCCCVVV;

			IScrBook savedGenesis = AddArchiveBookToMockedScripture(1, "Genesis");
			IScrSection sectionSaved = AddSectionToMockedBook(savedGenesis);
			sectionSaved.VerseRefStart = sectionSaved.VerseRefMin = new BCVRef(1, 1, 1).BBCCCVVV;
			sectionSaved.VerseRefEnd = sectionSaved.VerseRefMax = new BCVRef(1, 1, 15).BBCCCVVV;
			sectionSaved = AddSectionToMockedBook(savedGenesis);
			sectionSaved.VerseRefStart = sectionSaved.VerseRefMin = new BCVRef(1, 2, 1).BBCCCVVV;
			sectionSaved.VerseRefEnd = sectionSaved.VerseRefMax = new BCVRef(1, 2, 25).BBCCCVVV;
			string sDetails;
			List<IScrSection> sectionsToRemove;
			HashSet<int> missingBtWs;
			Assert.AreEqual(OverwriteType.Partial,
				genesis.DetermineOverwritability(savedGenesis, out sDetails, out sectionsToRemove,
					out missingBtWs),
				"Can perform a partial overwrite with saved book that contains a hole exactly matched by a current section");
			Assert.AreEqual(2, sectionsToRemove.Count);
			Assert.AreEqual(genesis.SectionsOS[0].Hvo, sectionsToRemove[0].Hvo);
			Assert.AreEqual(genesis.SectionsOS[2].Hvo, sectionsToRemove[1].Hvo);
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
			IScrBook genesis = AddBookToMockedScripture(1, "Genesis");
			IScrSection sectionCur = AddSectionToMockedBook(genesis);
			sectionCur.VerseRefStart = sectionCur.VerseRefMin = new BCVRef(1, 1, 1).BBCCCVVV;
			sectionCur.VerseRefEnd = sectionCur.VerseRefMax = new BCVRef(1, 1, 15).BBCCCVVV;
			sectionCur = AddSectionToMockedBook(genesis);
			sectionCur.VerseRefStart = sectionCur.VerseRefMin = new BCVRef(1, 1, 18).BBCCCVVV;
			sectionCur.VerseRefEnd = sectionCur.VerseRefMax = new BCVRef(1, 1, 25).BBCCCVVV;
			sectionCur = AddSectionToMockedBook(genesis);
			sectionCur.VerseRefStart = sectionCur.VerseRefMin = new BCVRef(1, 2, 1).BBCCCVVV;
			sectionCur.VerseRefEnd = sectionCur.VerseRefMax = new BCVRef(1, 2, 25).BBCCCVVV;

			IScrBook savedGenesis = AddArchiveBookToMockedScripture(1, "Genesis");
			IScrSection sectionSaved = AddSectionToMockedBook(savedGenesis);
			sectionSaved.VerseRefStart = sectionSaved.VerseRefMin = new BCVRef(1, 1, 1).BBCCCVVV;
			sectionSaved.VerseRefEnd = sectionSaved.VerseRefMax = new BCVRef(1, 1, 15).BBCCCVVV;
			sectionSaved = AddSectionToMockedBook(savedGenesis);
			sectionSaved.VerseRefStart = sectionSaved.VerseRefMin = new BCVRef(1, 2, 1).BBCCCVVV;
			sectionSaved.VerseRefEnd = sectionSaved.VerseRefMax = new BCVRef(1, 2, 25).BBCCCVVV;
			string sDetails;
			List<IScrSection> sectionsToRemove;
			HashSet<int> missingBtWs;
			Assert.AreEqual(OverwriteType.Partial,
				genesis.DetermineOverwritability(savedGenesis, out sDetails, out sectionsToRemove,
					out missingBtWs),
				"Can perform a partial overwrite with saved book that contains a hole matched by a current section");
			Assert.AreEqual(2, sectionsToRemove.Count);
			Assert.AreEqual(genesis.SectionsOS[0].Hvo, sectionsToRemove[0].Hvo);
			Assert.AreEqual(genesis.SectionsOS[2].Hvo, sectionsToRemove[1].Hvo);
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
			IScrBook genesis = AddBookToMockedScripture(1, "Genesis");
			IScrSection sectionCur = AddSectionToMockedBook(genesis);
			sectionCur.VerseRefStart = sectionCur.VerseRefMin = new BCVRef(1, 1, 2).BBCCCVVV;
			sectionCur.VerseRefEnd = sectionCur.VerseRefMax = new BCVRef(1, 1, 8).BBCCCVVV;

			IScrBook savedGenesis = AddArchiveBookToMockedScripture(1, "Genesis");
			IScrSection sectionSaved = AddSectionToMockedBook(savedGenesis);
			sectionSaved.VerseRefStart = sectionSaved.VerseRefMin = new BCVRef(1, 1, 1).BBCCCVVV;
			sectionSaved.VerseRefEnd = sectionSaved.VerseRefMax = new BCVRef(1, 1, 1).BBCCCVVV;
			sectionSaved = AddSectionToMockedBook(savedGenesis);
			sectionSaved.VerseRefStart = sectionSaved.VerseRefMin = new BCVRef(1, 1, 9).BBCCCVVV;
			sectionSaved.VerseRefEnd = sectionSaved.VerseRefMax = new BCVRef(1, 1, 11).BBCCCVVV;
			string sDetails;
			List<IScrSection> sectionsToRemove;
			HashSet<int> missingBtWs;
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
			IScrBook genesis = AddBookToMockedScripture(1, "Genesis");
			IScrSection introCur = AddSectionToMockedBook(genesis);
			introCur.VerseRefStart = introCur.VerseRefMin = new BCVRef(1, 1, 0).BBCCCVVV;
			introCur.VerseRefEnd = introCur.VerseRefMax = new BCVRef(1, 1, 0).BBCCCVVV;
			IScrSection section1cur = AddSectionToMockedBook(genesis);
			section1cur.VerseRefStart = section1cur.VerseRefMin = new BCVRef(1, 1, 1).BBCCCVVV;
			section1cur.VerseRefEnd = section1cur.VerseRefMax = new BCVRef(1, 3, 20).BBCCCVVV;

			IScrBook savedGenesis = AddArchiveBookToMockedScripture(1, "Genesis");
			IScrSection section1saved = AddSectionToMockedBook(savedGenesis);
			section1saved.VerseRefStart = section1saved.VerseRefMin = new BCVRef(1, 1, 0).BBCCCVVV;
			section1saved.VerseRefEnd = section1saved.VerseRefMax = new BCVRef(1, 1, 0).BBCCCVVV;
			string sDetails;
			List<IScrSection> sectionsToRemove;
			HashSet<int> missingBtWs;
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
			// Set up a book and a revision of it with no back translations.
			IScrBook book = AddBookToMockedScripture(1, "Genesis");
			IScrBook savedVersion = AddArchiveBookToMockedScripture(1, "Genesis");

			// Determine writing systems in use in the book, but not in its revision.
			HashSet<int> missingBts = (HashSet<int>)ReflectionHelper.GetResult(book, "FindMissingBts",
				savedVersion);

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
			// Set up a book and a revision of it each using the same back translation
			// "writing system".
			DummyScrBook book = new DummyScrBook();
			book.m_btWs = new HashSet<int>(new int[] {1});
			DummyScrBook savedVersion = new DummyScrBook();
			savedVersion.m_btWs = new HashSet<int>(new int[] { 1 });

			// Determine writing systems in use in the book, but not in its revision.
			HashSet<int> missingBts = (HashSet<int>)ReflectionHelper.GetResult(book, "FindMissingBts",
				savedVersion);

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
			// Set up a book where the back translation "writing systems"
			// in use in the revision is a subset of those in use in the book.
			DummyScrBook book = new DummyScrBook();
			book.m_btWs = new HashSet<int>(new int[] { 1, 2, 3 });
			DummyScrBook savedVersion = new DummyScrBook();
			savedVersion.m_btWs = new HashSet<int>(new int[] { 1 });

			// Determine writing systems in use in the book, but not in its revision.
			HashSet<int> missingBts = (HashSet<int>)ReflectionHelper.GetResult(book, "FindMissingBts",
				savedVersion);

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
			// Set up a book and a revision of it where the back translation "writing systems"
			// in use in Genesis is a subset of those in use in the revision.
			DummyScrBook book = new DummyScrBook();
			book.m_btWs = new HashSet<int>(new int[] { 1 });
			DummyScrBook savedVersion = new DummyScrBook();
			savedVersion.m_btWs = new HashSet<int>(new int[] { 1, 2, 3 });

			// Determine writing systems in use in the book, but not in its revision.
			HashSet<int> missingBts = (HashSet<int>)ReflectionHelper.GetResult(book, "FindMissingBts",
				savedVersion);

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
			IScrBook genesis = AddBookToMockedScripture(1, "Genesis");
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
			IScrBook genesis = AddBookToMockedScripture(1, "Genesis");
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
			IScrBook genesis = AddBookToMockedScripture(1, "Genesis");
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

		#region BackTransWs tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests BackTransWs when there are no back translations.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BackTransWs_NoBTs()
		{
			IScrBook book = AddBookWithTwoSections(1, "Genesis");

			// We expect that the book will have no back translations.
			Assert.AreEqual(0, book.BackTransWs.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests BackTransWs when there is one back translation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BackTransWs_OneBT()
		{
			int wsSpanish = m_wsSpanish.Handle;

			// Add a Spanish back translation
			IScrBook book = AddBookWithTwoSections(1, "Genesis");
			IStTxtPara para = (IStTxtPara)book.SectionsOS[0].ContentOA.ParagraphsOS[0];
			ICmTranslation trans = para.GetOrCreateBT();
			ITsStrBldr bldr = trans.Translation.get_String(wsSpanish).GetBldr();
			bldr.Replace(0, 0, "uno dos tres", null);
			trans.Translation.set_String(wsSpanish, bldr.GetString());

			// We expect that the book will have one back translation for Spanish.
			HashSet<int> wsBTs = book.BackTransWs;
			Assert.IsNotNull(wsBTs);
			Assert.AreEqual(1, wsBTs.Count);
			Assert.IsTrue(wsBTs.Contains(wsSpanish));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests BackTransWs when there are three back translations.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BackTransWs_ThreeBTs()
		{
			int wsSpanish = m_wsSpanish.Handle;
			int wsGerman = m_wsGerman.Handle;
			int wsFrench = m_wsFrench.Handle;

			// Add Spanish, German and French back translations.
			IScrBook book = AddBookWithTwoSections(1, "Genesis");
			IStTxtPara para = (IStTxtPara)book.SectionsOS[0].ContentOA.ParagraphsOS[0];
			ICmTranslation trans = para.GetOrCreateBT();
			ITsStrBldr bldr = trans.Translation.get_String(wsSpanish).GetBldr();
			bldr.Replace(0, 0, "uno dos tres", null);
			trans.Translation.set_String(wsSpanish, bldr.GetString());
			bldr = trans.Translation.get_String(wsGerman).GetBldr();
			bldr.Replace(0, 0, "eins zwei drei", null);
			trans.Translation.set_String(wsGerman, bldr.GetString());
			bldr = trans.Translation.get_String(wsFrench).GetBldr();
			bldr.Replace(0, 0, "un deux trois", null);
			trans.Translation.set_String(wsFrench, bldr.GetString());

			// We expect that the book will have back translations for Spanish, German and French.
			HashSet<int> wsBTs = book.BackTransWs;
			Assert.IsNotNull(wsBTs);
			Assert.AreEqual(3, wsBTs.Count);
			Assert.IsTrue(wsBTs.Contains(wsSpanish));
			Assert.IsTrue(wsBTs.Contains(wsGerman));
			Assert.IsTrue(wsBTs.Contains(wsFrench));
		}
		#endregion
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests the ScrBook class with test data.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class ScrBookTestsWithData : ScrInMemoryFdoTestBase
	{
		#region Member data
		private IScrBook m_philemon;
		#endregion

		#region Test setup
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create test data
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void TestSetup()
		{
			base.TestSetup();

			m_philemon = AddBookToMockedScripture(57, "Philemon");
			AddTitleToMockedBook(m_philemon, "Philemon");

			// Initialize Philemon with intro and scripture sections

			IScrSection section = AddSectionToMockedBook(m_philemon, true);
			AddSectionHeadParaToSection(section, "Intro1", ScrStyleNames.IntroSectionHead);
			IStTxtPara para = AddParaToMockedSectionContent(section, ScrStyleNames.IntroParagraph);
			AddRunToMockedPara(para, "some intro text", null);

			section = AddSectionToMockedBook(m_philemon, true);
			AddSectionHeadParaToSection(section, "Intro2", ScrStyleNames.IntroSectionHead);
			para = AddParaToMockedSectionContent(section, ScrStyleNames.IntroListItem1);
			AddRunToMockedPara(para, "some more intro text", null);

			// intro ends

			// normal scripture
			section = AddSectionToMockedBook(m_philemon);
			para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			AddSectionHeadParaToSection(section, "Scripture1", ScrStyleNames.SectionHead);
			AddRunToMockedPara(para, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "some text", null);
			AddRunToMockedPara(para, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "some text", null);
			AddRunToMockedPara(para, "3-5", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "some text", null);

			section = AddSectionToMockedBook(m_philemon);
			AddSectionHeadParaToSection(section, "Scripture2", ScrStyleNames.SectionHead);
			para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para, "6", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "some text", null);
			AddRunToMockedPara(para, "7", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "some text", null);
		}
		#endregion

		#region MergeSectionIntoPreviousSectionContent Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test merging the section into the previous section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MergeSectionIntoPreviousSectionContent()
		{
			IScrSection firstSection = m_philemon.SectionsOS[0];
			Assert.AreEqual(1, firstSection.ContentOA.ParagraphsOS.Count);
			IScrSection secondSection = m_philemon.SectionsOS[1];
			int numSectionsAtStart = m_philemon.SectionsOS.Count;

			IStStyle newStyle = Cache.LangProject.TranslatedScriptureOA.FindStyle(ScrStyleNames.Line1);
			m_philemon.MergeSectionIntoPreviousSectionContent(1, newStyle);

			// We expect the heading and contents of the second section to be added to the end
			// of the first section contents.
			Assert.AreEqual(3, firstSection.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual("some intro text",
				((IScrTxtPara)firstSection.ContentOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual("Intro2",
				((IScrTxtPara)firstSection.ContentOA.ParagraphsOS[1]).Contents.Text);
			Assert.AreEqual("some more intro text",
				((IScrTxtPara)firstSection.ContentOA.ParagraphsOS[2]).Contents.Text);

			// We also expect that the original second section has been deleted and that the
			// current second section is now the first Scripture section.
			Assert.AreEqual(numSectionsAtStart - 1, m_philemon.SectionsOS.Count,
				"We expect one section to be removed from Philemon.");
			Assert.AreEqual("Scripture1",
				((IScrTxtPara)m_philemon.SectionsOS[1].HeadingOA.ParagraphsOS[0]).Contents.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test merging the section into the previous section (when there is no prior section).
		/// We expect an ArgumentOutOfRangeException.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void MergeSectionIntoPreviousSectionContent_NoPreviousSection()
		{
			IStStyle newStyle = Cache.LangProject.TranslatedScriptureOA.FindStyle(ScrStyleNames.NormalParagraph);
			m_philemon.MergeSectionContentIntoPreviousSectionContent(0);
		}
		#endregion

		#region MoveHeadingParasToPreviousSectionContent Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test moving the first of three heading paras section into the previous section
		/// content.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MoveHeadingParasToPreviousSectionContent_FirstPara()
		{
			IScrSection firstSection = m_philemon.SectionsOS[0];
			Assert.AreEqual(1, firstSection.ContentOA.ParagraphsOS.Count);
			IScrSection secondSection = m_philemon.SectionsOS[1];
			int numSectionsAtStart = m_philemon.SectionsOS.Count;

			// Add two section head paragraphs to second section head
			AddSectionHeadParaToSection(secondSection, "Intro Heading2b", ScrStyleNames.IntroSectionHead);
			AddSectionHeadParaToSection(secondSection, "Intro Heading2c", ScrStyleNames.IntroSectionHead);
			Assert.AreEqual(3, m_philemon.SectionsOS[1].HeadingOA.ParagraphsOS.Count);

			IStStyle paraStyle = Cache.LangProject.TranslatedScriptureOA.FindStyle(ScrStyleNames.NormalParagraph);
			m_philemon.MoveHeadingParasToPreviousSectionContent(1, 0, paraStyle);

			// We expect the first heading of the second section to be added to the end
			// of the first section contents.
			Assert.AreEqual(2, m_philemon.SectionsOS[1].HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual("some intro text",
				((IScrTxtPara)firstSection.ContentOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual("Intro2",
				((IScrTxtPara)firstSection.ContentOA.ParagraphsOS[1]).Contents.Text);

			// We also expect that the original second section will still be present, but have the
			// first paragraph removed.
			Assert.AreEqual(numSectionsAtStart, m_philemon.SectionsOS.Count,
				"We expect that no sections should be removed from Philemon.");
			Assert.AreEqual(2, m_philemon.SectionsOS[1].HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual("Intro Heading2b",
				((IScrTxtPara)m_philemon.SectionsOS[1].HeadingOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual("Intro Heading2c",
				((IScrTxtPara)m_philemon.SectionsOS[1].HeadingOA.ParagraphsOS[1]).Contents.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test moving two heading paras section into the previous section content.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MoveHeadingParasToPreviousSectionContent_TwoParas()
		{
			IScrSection firstSection = m_philemon.SectionsOS[0];
			Assert.AreEqual(1, firstSection.ContentOA.ParagraphsOS.Count);
			IScrSection secondSection = m_philemon.SectionsOS[1];
			int numSectionsAtStart = m_philemon.SectionsOS.Count;

			// Add two section head paragraphs to second section head
			AddSectionHeadParaToSection(secondSection, "Intro Heading2b", ScrStyleNames.IntroSectionHead);
			AddSectionHeadParaToSection(secondSection, "Intro Heading2c", ScrStyleNames.IntroSectionHead);
			Assert.AreEqual(3, m_philemon.SectionsOS[1].HeadingOA.ParagraphsOS.Count);

			IStStyle contentStyle = Cache.LangProject.TranslatedScriptureOA.FindStyle(ScrStyleNames.IntroParagraph);
			m_philemon.MoveHeadingParasToPreviousSectionContent(1, 1, contentStyle);

			// We expect the first and second heading of the second section to be added to the end
			// of the first section contents.

			Assert.AreEqual(3, m_philemon.SectionsOS[0].ContentOA.ParagraphsOS.Count);
			Assert.AreEqual("some intro text",
				((IScrTxtPara)m_philemon.SectionsOS[0].ContentOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual("Intro2",
				((IScrTxtPara)m_philemon.SectionsOS[0].ContentOA.ParagraphsOS[1]).Contents.Text);
			Assert.AreEqual("Intro Heading2b",
				((IScrTxtPara)m_philemon.SectionsOS[0].ContentOA.ParagraphsOS[2]).Contents.Text);

			// We also expect that the original second section will still be present, but have the
			// first paragraph removed.
			Assert.AreEqual(numSectionsAtStart, m_philemon.SectionsOS.Count,
				"We expect that no sections should be removed from Philemon.");
			Assert.AreEqual(1, m_philemon.SectionsOS[1].HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual("Intro Heading2c",
				((IScrTxtPara)m_philemon.SectionsOS[1].HeadingOA.ParagraphsOS[0]).Contents.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test moving all heading paras section into the previous section content.
		/// We expect an exception because all paras would be moved leaving the section heading
		/// in the following section in an invalid state.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(InvalidOperationException), ExpectedMessage=
			"Must not move all paragraphs in the heading.")]
		public void MoveHeadingParasToPreviousSectionContent_AllParas()
		{
			IScrSection firstSection = m_philemon.SectionsOS[0];
			Assert.AreEqual(1, firstSection.ContentOA.ParagraphsOS.Count);
			IScrSection secondSection = m_philemon.SectionsOS[1];
			int numSectionsAtStart = m_philemon.SectionsOS.Count;

			// Add two section head paragraphs to second section head
			AddSectionHeadParaToSection(secondSection, "Intro Heading2b", ScrStyleNames.IntroSectionHead);
			AddSectionHeadParaToSection(secondSection, "Intro Heading2c", ScrStyleNames.IntroSectionHead);
			Assert.AreEqual(3, m_philemon.SectionsOS[1].HeadingOA.ParagraphsOS.Count);

			IStStyle contentStyle = Cache.LangProject.TranslatedScriptureOA.FindStyle(ScrStyleNames.IntroParagraph);
			m_philemon.MoveHeadingParasToPreviousSectionContent(1, 2, contentStyle);
		}
		#endregion

		#region MergeSectionContentIntoPreviousSectionContent
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test merging the second section content into the first section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MergeSectionContentIntoPreviousSectionContent()
		{
			IScrSection firstSection = m_philemon.SectionsOS[0];
			Assert.AreEqual(1, firstSection.ContentOA.ParagraphsOS.Count);
			IScrSection secondSection = m_philemon.SectionsOS[1];
			int numSectionsAtStart = m_philemon.SectionsOS.Count;

			m_philemon.MergeSectionContentIntoPreviousSectionContent(1);

			// We expect the contents of the second section to be added to the end of the first
			// section.
			Assert.AreEqual(2, firstSection.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual("some intro text",
				((IScrTxtPara)firstSection.ContentOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual("some more intro text",
				((IScrTxtPara)firstSection.ContentOA.ParagraphsOS[1]).Contents.Text);
			Assert.AreEqual(numSectionsAtStart - 1, m_philemon.SectionsOS.Count,
				"We expect one section to be removed from Philemon.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test merging the section content into the previous section (when there is no prior
		/// section). We expect an ArgumentOutOfRangeException.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void MergeSectionContentIntoPreviousSectionContent_NoPreviousSection()
		{
			m_philemon.MergeSectionContentIntoPreviousSectionContent(0);
		}
		#endregion

		#region MergeSectionIntoNextSectionHeading
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test merging the previous section heading and content into this second section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MergeSectionIntoNextSectionHeading()
		{
			IScrSection secondSection = m_philemon.SectionsOS[1];
			Assert.AreEqual(1, secondSection.HeadingOA.ParagraphsOS.Count);
			int numSectionsAtStart = m_philemon.SectionsOS.Count;

			IStStyle introHeadingStyle = Cache.LangProject.TranslatedScriptureOA.FindStyle(
				ScrStyleNames.IntroSectionHead);
			m_philemon.MergeSectionIntoNextSectionHeading(0, introHeadingStyle);

			// We expect the contents of the second section to be added to the end of the first
			// section.
			Assert.AreEqual(3, secondSection.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual("Intro1",
				((IScrTxtPara)secondSection.HeadingOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual("some intro text",
				((IScrTxtPara)secondSection.HeadingOA.ParagraphsOS[1]).Contents.Text);
			Assert.AreEqual("Intro2",
				((IScrTxtPara)secondSection.HeadingOA.ParagraphsOS[2]).Contents.Text);

			Assert.AreEqual(numSectionsAtStart - 1, m_philemon.SectionsOS.Count,
				"We expect one section to be removed from Philemon.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test merging the section heading and content into the following section heading (when
		/// there is no following section). We expect an ArgumentOutOfRangeException.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void MergeSectionIntoNextSectionHeading_NoNextSection()
		{
			IStStyle paraStyle = Cache.LangProject.TranslatedScriptureOA.FindStyle(ScrStyleNames.NormalParagraph);
			m_philemon.MergeSectionIntoNextSectionHeading(3, paraStyle);
		}
		#endregion

		#region MoveContentParasToNextSectionHeading Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test moving one content paragraph into the second section head.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MoveContentParasToNextSectionHeading_LastPara()
		{
			IScrSection firstSection = m_philemon.SectionsOS[0];
			IScrSection secondSection = m_philemon.SectionsOS[1];
			Assert.AreEqual(1, secondSection.HeadingOA.ParagraphsOS.Count);
			int numSectionsAtStart = m_philemon.SectionsOS.Count;

			// Add two paragraphs to first section content
			IScrTxtPara para = AddParaToMockedSectionContent(firstSection, ScrStyleNames.IntroParagraph);
			AddRunToMockedPara(para, "para A", Cache.DefaultVernWs);
			para = AddParaToMockedSectionContent(firstSection, ScrStyleNames.IntroParagraph);
			AddRunToMockedPara(para, "para B", Cache.DefaultVernWs);
			Assert.AreEqual(3, m_philemon.SectionsOS[0].ContentOA.ParagraphsOS.Count);

			// We must apply a heading style to the paragrpah that will become a heading.
			para.StyleRules = StyleUtils.ParaStyleTextProps(ScrStyleNames.SectionHead);
			IStStyle paraStyle = Cache.LangProject.TranslatedScriptureOA.FindStyle(para.StyleRules);

			m_philemon.MoveContentParasToNextSectionHeading(0, 2, paraStyle);

			Assert.AreEqual(numSectionsAtStart, m_philemon.SectionsOS.Count,
				"We expect that no sections will be removed from Philemon.");

			// We expect the last para of the first section to be added to the start
			// of the following section head
			Assert.AreEqual(2, secondSection.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual("para B",
				((IScrTxtPara)secondSection.HeadingOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual("Intro2",
				((IScrTxtPara)secondSection.HeadingOA.ParagraphsOS[1]).Contents.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test moving one content paragraph into the next section head, when there is no
		/// next section. We expect an ArgumentOutOfRangeException.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void MoveContentParasToNextSectionHeading_LastSection()
		{
			// Add a paragraph to last section content
			IScrSection lastSection = m_philemon.SectionsOS[3];
			IScrTxtPara para = AddParaToMockedSectionContent(lastSection, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para, "second para", Cache.DefaultVernWs);

			IStStyle headingStyle = Cache.LangProject.TranslatedScriptureOA.FindStyle(ScrStyleNames.SectionHead);

			// We expect the section index to cause the exception to be thrown.
			m_philemon.MoveContentParasToNextSectionHeading(3, 1, headingStyle);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test moving one content paragraph into the next section head, when that would be all
		/// content paragraphs. We expect an InvalidOperationException.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(InvalidOperationException),
			ExpectedMessage = "iFirstPara cannot be the first paragraph.")]
		public void MoveContentParasToNextSectionHeading_AllParas()
		{
			IStStyle headingStyle = Cache.LangProject.TranslatedScriptureOA.FindStyle(ScrStyleNames.SectionHead);

			// We expect the para index to cause the exception to be thrown.
			m_philemon.MoveContentParasToNextSectionHeading(0, 0, headingStyle);
		}
		#endregion
	}
}
