// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2005, SIL International. All Rights Reserved.
// <copyright from='2005' to='2005' company='SIL International'>
//		Copyright (c) 2005, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ScriptureChangeWatcherTests.cs
// Responsibility: MichaelL
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;

using NUnit.Framework;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FDO.Scripture;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for the ScriptureChangeWatcher
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class ScriptureChangeWatcherTests: ScrInMemoryFdoTestBase
	{
		#region Data members
		private IScrBook m_book;
		#endregion

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
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_book = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		#region Test setup
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create test data
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			m_inMemoryCache.InitializeWritingSystemEncodings();
			m_book = m_scrInMemoryCache.AddBookToMockedScripture(40, "Matthews");
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

			m_book = null;

			base.Exit();
		}
		#endregion

		#region Adjust section references
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure section reference is extended by one verse when a new verse is added at
		/// the end.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AdjustSectionReferences_InsertedVerseAtEnd()
		{
			CheckDisposed();

			// add section and paragraph
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "This is verse one. ", null);
			section.AdjustReferences();

			// set up the scripture paragraph ChangeWatcher
			ScriptureChangeWatcher.Create(Cache);

			// Test inserting an additional verse number in the paragraph
			m_scrInMemoryCache.AddRunToMockedPara(para, "2", ScrStyleNames.VerseNumber);

			// Now indirectly invoke the change watcher
			Cache.MainCacheAccessor.PropChanged(null,
				(int)PropChangeType.kpctNotifyAll, para.Hvo,
				(int)StTxtPara.StTxtParaTags.kflidContents, 21, 1, 0);

			// verify the results
			Assert.AreEqual(40001001, section.VerseRefMin);
			Assert.AreEqual(40001002, section.VerseRefMax);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure section reference of following sections are updated if a change to the end
		/// reference of one section spills over into the following one because of inserting
		/// a verse number.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AdjustSectionReferences_InsertCascadeChangeToNextsection()
		{
			CheckDisposed();

			// add section and paragraph
			IScrSection section1 = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			StTxtPara para1 = m_scrInMemoryCache.AddParaToMockedSectionContent(section1.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "This is verse one. ", null);
			section1.AdjustReferences();

			IScrSection section2 = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			StTxtPara para2 = m_scrInMemoryCache.AddParaToMockedSectionContent(section2.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "This is more text. ", null);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "3", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "This is verse three. ", null);
			section2.AdjustReferences();

			// set up the scripture paragraph ChangeWatcher
			ScriptureChangeWatcher.Create(Cache);

			// Test inserting an additional verse number in the first paragraph
			m_scrInMemoryCache.AddRunToMockedPara(para1, "2", ScrStyleNames.VerseNumber);

			// Now indirectly invoke the change watcher
			Cache.MainCacheAccessor.PropChanged(null,
				(int)PropChangeType.kpctNotifyAll, para1.Hvo,
				(int)StTxtPara.StTxtParaTags.kflidContents, 21, 1, 0);

			// verify the results
			Assert.AreEqual(40001001, section1.VerseRefMin);
			Assert.AreEqual(40001002, section1.VerseRefMax);
			Assert.AreEqual(40001002, section2.VerseRefMin);
			Assert.AreEqual(40001003, section2.VerseRefMax);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure section reference of following sections are not updated if following
		/// section starts with a new verse.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AdjustSectionReferences_DeleteCascadeNoChangeIfNextSectionStartsWithVerse()
		{
			CheckDisposed();

			// add section and paragraph
			IScrSection section1 = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			StTxtPara para1 = m_scrInMemoryCache.AddParaToMockedSectionContent(section1.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "This is verse one. ", null);
			section1.AdjustReferences();

			IScrSection section2 = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			StTxtPara para2 = m_scrInMemoryCache.AddParaToMockedSectionContent(section2.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "2", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "This is more text. ", null);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "3", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "This is verse three. ", null);
			section2.AdjustReferences();

			// set up the scripture paragraph ChangeWatcher
			ScriptureChangeWatcher.Create(Cache);

			// Test inserting an additional verse number in the first paragraph
			m_scrInMemoryCache.AddRunToMockedPara(para1, "3", ScrStyleNames.VerseNumber);

			// Now indirectly invoke the change watcher
			Cache.MainCacheAccessor.PropChanged(null,
				(int)PropChangeType.kpctNotifyAll, para1.Hvo,
				(int)StTxtPara.StTxtParaTags.kflidContents, 21, 1, 0);

			// verify the results
			Assert.AreEqual(40001001, section1.VerseRefMin);
			Assert.AreEqual(40001003, section1.VerseRefMax);
			Assert.AreEqual(40001002, section2.VerseRefMin);
			Assert.AreEqual(40001003, section2.VerseRefMax);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure section reference of following sections are updated if a change to the end
		/// reference of one section spills over into the following one because of deleting
		/// a verse number. This also tests deleting a verse from the end of a section updates
		/// the section reference.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AdjustSectionReferences_DeleteCascadeChangeToNextsection()
		{
			CheckDisposed();

			// add section and paragraph
			IScrSection section1 = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			StTxtPara para1 = m_scrInMemoryCache.AddParaToMockedSectionContent(section1.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "This is verse one. ", null);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "2", ScrStyleNames.VerseNumber);
			section1.AdjustReferences();

			IScrSection section2 = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			StTxtPara para2 = m_scrInMemoryCache.AddParaToMockedSectionContent(section2.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "This is more text. ", null);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "3", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "This is verse three. ", null);
			section2.AdjustReferences();

			// set up the scripture paragraph ChangeWatcher
			ScriptureChangeWatcher.Create(Cache);

			// Test deleting the verse number at the end of para1
			ITsStrBldr strBuilder = para1.Contents.UnderlyingTsString.GetBldr();
			strBuilder.ReplaceRgch(21, 22, "", 0, para1.Contents.UnderlyingTsString.get_PropertiesAt(3)); // delete verse number 2
			para1.Contents.UnderlyingTsString = strBuilder.GetString();

			// Now indirectly invoke the change watcher
			Cache.MainCacheAccessor.PropChanged(null,
				(int)PropChangeType.kpctNotifyAll, para1.Hvo,
				(int)StTxtPara.StTxtParaTags.kflidContents, 21, 0, 1);

			// verify the results
			Assert.AreEqual(40001001, section1.VerseRefMin);
			Assert.AreEqual(40001001, section1.VerseRefEnd);
			Assert.AreEqual(40001001, section1.VerseRefMax);
			Assert.AreEqual(40001001, section2.VerseRefMin);
			Assert.AreEqual(40001001, section2.VerseRefStart);
			Assert.AreEqual(40001003, section2.VerseRefMax);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure section reference is not changed while a person is typing a new verse
		/// at the end of a section. E.g. section is 1:1-25 and user intends to type verse 26.
		/// When just the "2" has been typed, don't change the section ref to 1:1-2 before
		/// the "6" is typed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("Not sure if we want this rqmt. Expensive to do, and not a critical bug right now.")]
		public void AdjustSectionReferences_InsertedPartialVerseNumAtEnd()
		{
			CheckDisposed();

			// add section and paragraph
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "This is verse one. ", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "25", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "This is verse twentyfive. ", null);
			section.AdjustReferences();

			// set up the scripture paragraph ChangeWatcher
			ScriptureChangeWatcher.Create(Cache);

			// Test inserting an additional verse number in the paragraph
			m_scrInMemoryCache.AddRunToMockedPara(para, "2", ScrStyleNames.VerseNumber);

			// Now indirectly invoke the change watcher
			Cache.MainCacheAccessor.PropChanged(null,
				(int)PropChangeType.kpctNotifyAll, para.Hvo,
				(int)StTxtPara.StTxtParaTags.kflidContents, 49, 1, 0);

			// verify the results
			Assert.AreEqual(40001001, section.VerseRefMin);
			Assert.AreEqual(40001025, section.VerseRefMax);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure section reference is not changed when a out of order verse number is added
		/// at the end.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AdjustSectionReferences_InsertedBadVerseNumAtEnd()
		{
			CheckDisposed();

			// add section and paragraph
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "This is verse one. ", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "25", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "This is verse twentyfive. ", null);
			section.AdjustReferences();

			// set up the scripture paragraph ChangeWatcher
			ScriptureChangeWatcher.Create(Cache);

			// Test inserting an additional verse number in the paragraph
			m_scrInMemoryCache.AddRunToMockedPara(para, "2", ScrStyleNames.VerseNumber);

			// Now indirectly invoke the change watcher
			Cache.MainCacheAccessor.PropChanged(null,
				(int)PropChangeType.kpctNotifyAll, para.Hvo,
				(int)StTxtPara.StTxtParaTags.kflidContents, 49, 1, 0);

			// verify the results
			Assert.AreEqual(40001001, section.VerseRefMin);
			Assert.AreEqual(40001025, section.VerseRefMax);
			Assert.AreEqual(40001001, section.VerseRefStart);
			Assert.AreEqual(40001002, section.VerseRefEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure section reference is not changed when a out of order verse number is added
		/// at the beginning.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AdjustSectionReferences_InsertedBadVerseNumAtBeginning()
		{
			CheckDisposed();

			// add section and paragraph
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "24", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "This is verse one. ", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "25", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "This is verse twentyfive. ", null);
			section.AdjustReferences();

			// set up the scripture paragraph ChangeWatcher
			ScriptureChangeWatcher.Create(Cache);

			// Test inserting an extra digit into the first verse number making it 224
			ITsStrBldr strBuilder = para.Contents.UnderlyingTsString.GetBldr();
			// replace the current verse number with 224 to simulate editing
			strBuilder.ReplaceRgch(0, 2, "224", 3, para.Contents.UnderlyingTsString.get_PropertiesAt(0));
			para.Contents.UnderlyingTsString = strBuilder.GetString();
			// Now indirectly invoke the change watcher
			Cache.MainCacheAccessor.PropChanged(null,
				(int)PropChangeType.kpctNotifyAll, para.Hvo,
				(int)StTxtPara.StTxtParaTags.kflidContents,0, 3, 2);

			// verify the results
			Assert.AreEqual(40001001, section.VerseRefMin);
			Assert.AreEqual(40001025, section.VerseRefMax);
			Assert.AreEqual(40001001, section.VerseRefStart);
			Assert.AreEqual(40001025, section.VerseRefEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure verse reference on section is not reset to 0 when the text is deleted from
		/// the only paragraph in the section. To simulate deleting using BKSP, we need to delete
		/// all but the first character of the para, issue a prop changed, then delete the first
		/// character. Jira # is TE-2364.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AdjustSectionReferences_ParaContentsDeleted()
		{
			CheckDisposed();

			// add section and paragraph
			IScrSection section1 = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			StTxtPara para1 = m_scrInMemoryCache.AddParaToMockedSectionContent(section1.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "This is verse one. ", null);
			section1.AdjustReferences();

			IScrSection section2 = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			StTxtPara para2 = m_scrInMemoryCache.AddParaToMockedSectionContent(section2.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "2", ScrStyleNames.VerseNumber);
			section2.AdjustReferences();

			// set up the scripture paragraph ChangeWatcher
			ScriptureChangeWatcher.Create(Cache);

			// Test deleting the verse number at the start of para2
			ITsStrBldr strBuilder = para2.Contents.UnderlyingTsString.GetBldr();
			strBuilder.ReplaceRgch(0, 1, "", 0, para1.Contents.UnderlyingTsString.get_PropertiesAt(3));
			para2.Contents.UnderlyingTsString = strBuilder.GetString();

			// Now indirectly invoke the change watcher
			Cache.MainCacheAccessor.PropChanged(null,
				(int)PropChangeType.kpctNotifyAll, para2.Hvo,
				(int)StTxtPara.StTxtParaTags.kflidContents, 0, 0, 1);

			// verify the results
			Assert.AreEqual(40001001, section1.VerseRefMin);
			Assert.AreEqual(40001001, section1.VerseRefMax);
			Assert.AreEqual(40001001, section2.VerseRefMin);
			Assert.AreEqual(40001001, section2.VerseRefMax);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure verse reference on section resets to the end ref of the following section
		/// when the all but the first and last characters of the paragraph are deleted, leaving
		/// only a verse #2 and a period in the para. Jira # is TE-2364.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("This problem with section reference range is corrected when user corrects verse numbers")]
		public void AdjustSectionReferences_TrimFirstVerseToSingleDigit()
		{
			CheckDisposed();

			// add section and paragraph
			IScrSection section1 = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			StTxtPara para1 = m_scrInMemoryCache.AddParaToMockedSectionContent(section1.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "23", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "This is verse one. ", null);
			section1.AdjustReferences();

			IScrSection section2 = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			StTxtPara para2 = m_scrInMemoryCache.AddParaToMockedSectionContent(section2.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "24", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "This is more text. ", null);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "25", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "This is verse three.", null);
			section2.AdjustReferences();

			// set up the scripture paragraph ChangeWatcher
			ScriptureChangeWatcher.Create(Cache);

			// Test deleting the verse number at the start of para2
			ITsStrBldr strBuilder = para2.Contents.UnderlyingTsString.GetBldr();
			int cch = strBuilder.Length;
			strBuilder.ReplaceRgch(1, cch - 1, "", 0,
				para2.Contents.UnderlyingTsString.get_Properties(1));
			para2.Contents.UnderlyingTsString = strBuilder.GetString();

			// Now indirectly invoke the change watcher
			Cache.MainCacheAccessor.PropChanged(null,
				(int)PropChangeType.kpctNotifyAll, para2.Hvo,
				(int)StTxtPara.StTxtParaTags.kflidContents, 1, 0, cch - 2);

			// verify the results
			Assert.AreEqual(40001023, section1.VerseRefMin);
			Assert.AreEqual(40001023, section1.VerseRefMax);
			Assert.AreEqual(40001023, section2.VerseRefMin);
			Assert.AreEqual(40001023, section2.VerseRefMax);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure verse reference on section is correct when paragraph begins with a verse
		/// bridge and has no other verse references in the para. Jira # is TE-2364.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AdjustSectionReferences_ExtendVerseRangeAtBeginning()
		{
			CheckDisposed();

			// add section and paragraph
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "24", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "This is a verse. ", null);
			section.AdjustReferences();

			// set up the scripture paragraph ChangeWatcher
			ScriptureChangeWatcher.Create(Cache);

			// set up for the test: extend verse 24 to be a verse bridge
			// verse 25 is the last verse for chapter 2, so the recorded range will be 24-25
			ITsStrBldr strBuilder = para.Contents.UnderlyingTsString.GetBldr();
			strBuilder.Replace(2, 2, "-26", para.Contents.UnderlyingTsString.get_Properties(0));
			para.Contents.UnderlyingTsString = strBuilder.GetString();

			// Now indirectly invoke the change watcher
			Cache.MainCacheAccessor.PropChanged(null,
				(int)PropChangeType.kpctNotifyAll, para.Hvo,
				(int)StTxtPara.StTxtParaTags.kflidContents, 2, 3, 0);

			// verify the results
			Assert.AreEqual(40001024, section.VerseRefMin);
			Assert.AreEqual(40001025, section.VerseRefMax);
			Assert.AreEqual(40001024, section.VerseRefStart);
			Assert.AreEqual(40001025, section.VerseRefEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure section reference is extended by one verse when a new verse is added at
		/// the beginning of the paragraph of the first section of the book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AdjustSectionReferences_InsertedVerseAtBeginning_BeginOfBook()
		{
			CheckDisposed();

			// add section and paragraph
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "24", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "This is verse one. ", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "25", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "This is verse twentyfive. ", null);
			section.AdjustReferences();

			// set up the scripture paragraph ChangeWatcher
			ScriptureChangeWatcher.Create(Cache);

			// Test inserting an additional verse in the paragraph
			ITsStrBldr strBuilder = para.Contents.UnderlyingTsString.GetBldr();
			strBuilder.ReplaceRgch(0, 0, "13", 2, para.Contents.UnderlyingTsString.get_Properties(0));
			string sVerseText = "This is a new verse. ";
			strBuilder.ReplaceRgch(2, 2, sVerseText, sVerseText.Length,
				para.Contents.UnderlyingTsString.get_Properties(1));
			para.Contents.UnderlyingTsString = strBuilder.GetString();

			// Now indirectly invoke the change watcher
			Cache.MainCacheAccessor.PropChanged(null,
				(int)PropChangeType.kpctNotifyAll, para.Hvo,
				(int)StTxtPara.StTxtParaTags.kflidContents, 0, sVerseText.Length + 1, 0);

			// verify the results
			Assert.AreEqual(40001013, section.VerseRefMin);
			Assert.AreEqual(40001025, section.VerseRefMax);
			Assert.AreEqual(40001013, section.VerseRefStart);
			Assert.AreEqual(40001025, section.VerseRefEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure section reference is extended by one verse when a new verse is added at
		/// the beginning of a paragraph in a non-first section of the book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AdjustSectionReferences_InsertedVerseAtBeginning_MiddleOfBook()
		{
			CheckDisposed();

			// add section and paragraph
			IScrSection section1 = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			StTxtPara para1 = m_scrInMemoryCache.AddParaToMockedSectionContent(section1.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "This is verse one. ", null);
			section1.AdjustReferences();

			IScrSection section2 = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			StTxtPara para2 = m_scrInMemoryCache.AddParaToMockedSectionContent(section2.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "24", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "This is verse one. ", null);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "25", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "This is verse twentyfive. ", null);
			section2.AdjustReferences();

			// set up the scripture paragraph ChangeWatcher
			ScriptureChangeWatcher.Create(Cache);

			// Test inserting an additional verse in the first paragraph of the second section
			ITsStrBldr strBuilder = para2.Contents.UnderlyingTsString.GetBldr();
			strBuilder.ReplaceRgch(0, 0, "14", 2, para2.Contents.UnderlyingTsString.get_Properties(0));
			string sVerseText = "This is a new verse. ";
			strBuilder.ReplaceRgch(2, 2, sVerseText, sVerseText.Length,
				para2.Contents.UnderlyingTsString.get_Properties(1));
			para2.Contents.UnderlyingTsString = strBuilder.GetString();

			// Now indirectly invoke the change watcher
			Cache.MainCacheAccessor.PropChanged(null,
				(int)PropChangeType.kpctNotifyAll, para2.Hvo,
				(int)StTxtPara.StTxtParaTags.kflidContents, 0, sVerseText.Length + 1, 0);

			// verify the results
			Assert.AreEqual(40001001, section1.VerseRefMin);
			Assert.AreEqual(40001001, section1.VerseRefMax);
			Assert.AreEqual(40001014, section2.VerseRefMin);
			Assert.AreEqual(40001025, section2.VerseRefMax);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure section reference is reduced by one verse when a verse is deleted at
		/// the beginning.
		/// </summary>
		/// <remarks>When TE-3521 is done, we can change this test so that it uses only one
		/// section.</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AdjustSectionReferences_DeletedVerseAtBeginning()
		{
			CheckDisposed();

			// add section and paragraph
			IScrSection section1 = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			StTxtPara para1 = m_scrInMemoryCache.AddParaToMockedSectionContent(section1.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "This is verse one. ", null);
			section1.AdjustReferences();

			IScrSection section2 = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			StTxtPara para2 = m_scrInMemoryCache.AddParaToMockedSectionContent(section2.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "24", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "This is verse one. ", null);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "25", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "This is verse twentyfive. ", null);
			section2.AdjustReferences();

			// set up the scripture paragraph ChangeWatcher
			ScriptureChangeWatcher.Create(Cache);

			// Test deleting the first verse of the second para
			ITsStrBldr strBuilder = para2.Contents.UnderlyingTsString.GetBldr();
			strBuilder.ReplaceRgch(0, 21, "", 0,
				para2.Contents.UnderlyingTsString.get_Properties(1));
			para2.Contents.UnderlyingTsString = strBuilder.GetString();

			// Now indirectly invoke the change watcher
			Cache.MainCacheAccessor.PropChanged(null,
				(int)PropChangeType.kpctNotifyAll, para2.Hvo,
				(int)StTxtPara.StTxtParaTags.kflidContents, 0, 0, 21);

			// verify the results
			Assert.AreEqual(40001001, section1.VerseRefMin);
			Assert.AreEqual(40001001, section1.VerseRefMax);
			Assert.AreEqual(40001025, section2.VerseRefMin);
			Assert.AreEqual(40001025, section2.VerseRefMax);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure section reference is expanded by one verse when a verse number is deleted
		/// at the beginning.
		/// </summary>
		/// <remarks>When TE-3521 is done, we can change this test so that it uses only one
		/// section.</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AdjustSectionReferences_DeletedVerseNumAtBeginning()
		{
			CheckDisposed();

			// add section and paragraph
			IScrSection section1 = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			StTxtPara para1 = m_scrInMemoryCache.AddParaToMockedSectionContent(section1.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "This is verse one. ", null);
			section1.AdjustReferences();

			IScrSection section2 = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			StTxtPara para2 = m_scrInMemoryCache.AddParaToMockedSectionContent(section2.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "24", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "This is verse one. ", null);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "25", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "This is verse twentyfive. ", null);
			section2.AdjustReferences();

			// set up the scripture paragraph ChangeWatcher
			ScriptureChangeWatcher.Create(Cache);

			// Test deleting the first verse number of the second para
			ITsStrBldr strBuilder = para2.Contents.UnderlyingTsString.GetBldr();
			strBuilder.ReplaceRgch(0, 2, "", 0,
				para2.Contents.UnderlyingTsString.get_Properties(1));
			para2.Contents.UnderlyingTsString = strBuilder.GetString();

			// Now indirectly invoke the change watcher
			Cache.MainCacheAccessor.PropChanged(null,
				(int)PropChangeType.kpctNotifyAll, para2.Hvo,
				(int)StTxtPara.StTxtParaTags.kflidContents, 0, 0, 2);

			// verify the results
			Assert.AreEqual(40001001, section1.VerseRefMin);
			Assert.AreEqual(40001001, section1.VerseRefMax);
			Assert.AreEqual(40001001, section2.VerseRefMin);
			Assert.AreEqual(40001025, section2.VerseRefMax);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure the chapter numbers of the section references are changed if a chapter
		/// number is deleted at the beginning of the section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AdjustSectionReferences_DeletedChapterNumAtBeginning()
		{
			CheckDisposed();

			// add section and paragraph
			IScrSection section1 = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			StTxtPara para1 = m_scrInMemoryCache.AddParaToMockedSectionContent(section1.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "This is verse one. ", null);
			section1.AdjustReferences();

			IScrSection section2 = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			StTxtPara para2 = m_scrInMemoryCache.AddParaToMockedSectionContent(section2.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "2", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "24", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "This is verse one. ", null);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "25", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "This is verse twentyfive. ", null);
			section2.AdjustReferences();

			// set up the scripture paragraph ChangeWatcher
			ScriptureChangeWatcher.Create(Cache);

			// Test deleting the chapter number from the second para
			ITsStrBldr strBuilder = para2.Contents.UnderlyingTsString.GetBldr();
			strBuilder.ReplaceRgch(0, 1, "", 0,
				para2.Contents.UnderlyingTsString.get_Properties(1));
			para2.Contents.UnderlyingTsString = strBuilder.GetString();

			// Now indirectly invoke the change watcher
			Cache.MainCacheAccessor.PropChanged(null,
				(int)PropChangeType.kpctNotifyAll, para2.Hvo,
				(int)StTxtPara.StTxtParaTags.kflidContents, 0, 0, 1);

			// verify the results
			Assert.AreEqual(40001001, section1.VerseRefMin);
			Assert.AreEqual(40001001, section1.VerseRefMax);
			Assert.AreEqual(40001024, section2.VerseRefMin);
			Assert.AreEqual(40001025, section2.VerseRefMax);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure the chapter numbers of the section references are updated when a
		/// chapter number is changed at the beginning of the section.
		/// </summary>
		/// <remarks>When TE-3521 is done, we can get rid of section0.</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AdjustSectionReferences_ChangedChapterNumAtBeginning()
		{
			CheckDisposed();

			// add section and paragraph
			IScrSection section0 = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			StTxtPara para0 = m_scrInMemoryCache.AddParaToMockedSectionContent(section0.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para0, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para0, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para0, "This is verse one. ", null);
			section0.AdjustReferences();

			IScrSection section1 = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			StTxtPara para1 = m_scrInMemoryCache.AddParaToMockedSectionContent(section1.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "2", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "This is verse one. ", null);
			section1.AdjustReferences();

			IScrSection section2 = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			StTxtPara para2 = m_scrInMemoryCache.AddParaToMockedSectionContent(section2.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "24", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "This is verse one. ", null);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "25", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "This is verse twentyfive. ", null);
			section2.AdjustReferences();

			// set up the scripture paragraph ChangeWatcher
			ScriptureChangeWatcher.Create(Cache);

			// Test deleting the first verse number of the second para
			ITsStrBldr strBuilder = para1.Contents.UnderlyingTsString.GetBldr();
			strBuilder.ReplaceRgch(0, 1, "5", 1,
				para1.Contents.UnderlyingTsString.get_Properties(0));
			para1.Contents.UnderlyingTsString = strBuilder.GetString();

			// Now indirectly invoke the change watcher
			Cache.MainCacheAccessor.PropChanged(null,
				(int)PropChangeType.kpctNotifyAll, para1.Hvo,
				(int)StTxtPara.StTxtParaTags.kflidContents, 0, 1, 1);

			// verify the results
			Assert.AreEqual(40005001, section1.VerseRefMin);
			Assert.AreEqual(40005001, section1.VerseRefMax);
			Assert.AreEqual(40005001, section1.VerseRefStart);
			Assert.AreEqual(40005001, section1.VerseRefEnd);

			Assert.AreEqual(40005024, section2.VerseRefMin);
			Assert.AreEqual(40005025, section2.VerseRefMax);
			Assert.AreEqual(40005024, section2.VerseRefStart);
			Assert.AreEqual(40005025, section2.VerseRefEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure section reference is not changed when a new Scripture section is inserted
		/// between last intro section and first Scripture section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AdjustSectionReferences_InsertedScriptureSectionAfterIntro()
		{
			CheckDisposed();

			// add intro section and paragraph
			IScrSection section0 = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			StTxtPara para0 = m_scrInMemoryCache.AddParaToMockedSectionContent(section0.Hvo,
				ScrStyleNames.IntroParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para0, "Intro para.", null);
			section0.AdjustReferences();

			// add scripture section and paragraph
			IScrSection section1 = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			StTxtPara para1 = m_scrInMemoryCache.AddParaToMockedSectionContent(section1.Hvo,
				ScrStyleNames.IntroParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "This is the verse.", null);
			section1.AdjustReferences();

			// set up the scripture paragraph ChangeWatcher
			ScriptureChangeWatcher.Create(Cache);

			// Create StTxtPara to be tested, and load it from database.
			IScrSection newSection = ScrSection.CreateSectionWithEmptyParas(m_book, 1, false);

			Assert.AreEqual(40001001, newSection.VerseRefMin);
			Assert.AreEqual(40001001, newSection.VerseRefMax);
		}

		#endregion

		#region BT tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure the back translation gets marked as unfinished when the vernacular
		/// paragraph is edited
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MarkBtAsUnfinishedOnVernacularEdit()
		{
			CheckDisposed();

			// add scripture section and paragraph
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.IntroParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "This is the verse.", null);
			section.AdjustReferences();

			// Create a back translation paragraph.
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation btpara = m_scrInMemoryCache.AddBtToMockedParagraph(para, wsBT);
			m_scrInMemoryCache.AddRunToMockedTrans(btpara, wsBT, "My Back Translation", null);

			// set the state of the back translation to "finished".
			btpara.Status.AnalysisDefaultWritingSystem = BackTranslationStatus.Finished.ToString();

			// set up the scripture paragraph ChangeWatcher
			ScriptureChangeWatcher.Create(Cache);

			// change the vernacular paragraph
			ITsString contents = para.Contents.UnderlyingTsString;
			ITsStrBldr strBuilder = para.Contents.UnderlyingTsString.GetBldr();
			strBuilder.ReplaceRgch(0, 1, "5", 1, para.Contents.UnderlyingTsString.get_Properties(0));
			para.Contents.UnderlyingTsString = strBuilder.GetString();

			Cache.MainCacheAccessor.PropChanged(null,
				(int)PropChangeType.kpctNotifyAll, para.Hvo,
				(int)StTxtPara.StTxtParaTags.kflidContents, 0, 1, 1);

			// make sure the back translation is changed to "unfinished".
			string checkState = BackTranslationStatus.Unfinished.ToString();
			Assert.AreEqual(checkState, btpara.Status.AnalysisDefaultWritingSystem);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure the back translation is left unchanged when the vernacular paragraph is
		/// "edited" in such a way that nothing actually changes. For example, the change
		/// watcher gets called when the user presses ENTER at the end of the para, even though
		/// nothing is inserted or deleted in the para. TE-4970
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void LeaveBtStatusUnchangedIfNothingInParaChanged()
		{
			CheckDisposed();

			// add scripture section and paragraph
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.IntroParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "This is the verse.", null);
			section.AdjustReferences();

			// Create a back translation paragraph.
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation btpara = m_scrInMemoryCache.AddBtToMockedParagraph(para, wsBT);
			m_scrInMemoryCache.AddRunToMockedTrans(btpara, wsBT, "My Back Translation", null);

			// set the state of the back translation to "finished".
			btpara.Status.AnalysisDefaultWritingSystem = BackTranslationStatus.Finished.ToString();

			// set up the scripture paragraph ChangeWatcher
			ScriptureChangeWatcher.Create(Cache);

			// Issue a changeless PropChanged for the paragraph
			Cache.MainCacheAccessor.PropChanged(null,
				(int)PropChangeType.kpctNotifyAll, para.Hvo,
				(int)StTxtPara.StTxtParaTags.kflidContents, 0, 0, 0);

			// make sure the back translation is unchanged.
			string checkState = BackTranslationStatus.Finished.ToString();
			Assert.AreEqual(checkState, btpara.Status.AnalysisDefaultWritingSystem);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure the back translation gets marked as unfinished when the BT paragraph is
		/// edited.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MarkBtAsUnfinishedOnBtEdit()
		{
			CheckDisposed();

			// add scripture section and paragraph
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.IntroParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "This is the verse.", null);
			section.AdjustReferences();

			// Create a back translation paragraph.
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation btpara = m_scrInMemoryCache.AddBtToMockedParagraph(para, wsBT);
			m_scrInMemoryCache.AddRunToMockedTrans(btpara, wsBT, "My Back Translation", null);

			// set the state of the back translation to "checked".
			btpara.Status.AnalysisDefaultWritingSystem = BackTranslationStatus.Checked.ToString();

			// set up the Scripture paragraph ChangeWatcher
			ScriptureChangeWatcher.Create(Cache);

			// change the back translation paragraph
			btpara.Translation.SetAlternative("Your Back Translation", Cache.DefaultAnalWs);

			Cache.MainCacheAccessor.PropChanged(null,
				(int)PropChangeType.kpctNotifyAll,
				btpara.Hvo,
				(int)CmTranslation.CmTranslationTags.kflidTranslation,
				Cache.DefaultAnalWs, 4, 2);

			// make sure the back translation is changed to "unfinished".
			Assert.AreEqual(BackTranslationStatus.Unfinished.ToString(),
				btpara.Status.AnalysisDefaultWritingSystem);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure the back translation gets created when a vernacular paragraph is
		/// added to the section content.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreateBtWhenContentParagraphIsAdded()
		{
			CheckDisposed();

			// add scripture section and paragraph
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.IntroParagraph);

			// set up the Scripture paragraph ChangeWatcher
			ScriptureChangeWatcher.Create(Cache);

			// Issue a PropChanged that should create a back translation paragraph.
			Cache.MainCacheAccessor.PropChanged(null,
				(int)PropChangeType.kpctNotifyAll,
				section.ContentOA.Hvo,
				(int)StText.StTextTags.kflidParagraphs,
				0, 1, 0);

			// make sure the back translation is created.
			ScrTxtPara scrPara = new ScrTxtPara(Cache, para.Hvo);
			ICmTranslation bt = scrPara.GetBT();
			Assert.IsNotNull(bt);
			Assert.IsNull(bt.Status.AnalysisDefaultWritingSystem);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure the back translation gets created when a mutliple vernacular paragraphs
		/// are pasted over an existing paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreateBtsWhenContentParagraphIsReplacedByMultipleOtherParagraphs()
		{
			CheckDisposed();

			// add scripture section and paragraph
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			StTxtPara para1 = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.IntroParagraph);
			StTxtPara para2 = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.IntroParagraph);
			StTxtPara para3 = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.IntroParagraph);

			// set up the Scripture paragraph ChangeWatcher
			ScriptureChangeWatcher.Create(Cache);

			// Issue a PropChanged that should create a back translation paragraph.
			Cache.MainCacheAccessor.PropChanged(null,
				(int)PropChangeType.kpctNotifyAll,
				section.ContentOA.Hvo,
				(int)StText.StTextTags.kflidParagraphs,
				0, 3, 1);

			// make sure the back translations got created.
			ScrTxtPara scrPara = new ScrTxtPara(Cache, para1.Hvo);
			ICmTranslation bt = scrPara.GetBT();
			Assert.IsNotNull(bt);
			Assert.IsNull(bt.Status.AnalysisDefaultWritingSystem);

			scrPara = new ScrTxtPara(Cache, para2.Hvo);
			bt = scrPara.GetBT();
			Assert.IsNotNull(bt);
			Assert.IsNull(bt.Status.AnalysisDefaultWritingSystem);

			scrPara = new ScrTxtPara(Cache, para3.Hvo);
			bt = scrPara.GetBT();
			Assert.IsNotNull(bt);
			Assert.IsNull(bt.Status.AnalysisDefaultWritingSystem);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure the back translation gets created when a vernacular paragraph is
		/// added to the section heading.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreateBtWhenHeadingParagraphIsAdded()
		{
			CheckDisposed();

			// add scripture section and paragraph
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			StTxtPara para = m_scrInMemoryCache.AddSectionHeadParaToSection(section.Hvo,
				"Intro to Genesis or whatever", ScrStyleNames.IntroSectionHead);

			// set up the Scripture paragraph ChangeWatcher
			ScriptureChangeWatcher.Create(Cache);

			// Issue a PropChanged that should create a back translation paragraph.
			Cache.MainCacheAccessor.PropChanged(null,
				(int)PropChangeType.kpctNotifyAll,
				section.HeadingOA.Hvo,
				(int)StText.StTextTags.kflidParagraphs,
				0, 1, 0);

			// make sure the back translation is created.
			ScrTxtPara scrPara = new ScrTxtPara(Cache, para.Hvo);
			ICmTranslation bt = scrPara.GetBT();
			Assert.IsNotNull(bt);
			Assert.IsNull(bt.Status.AnalysisDefaultWritingSystem);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure the back translation gets created when a vernacular paragraph is
		/// added to the book title.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreateBtWhenBookTitleParagraphIsAdded()
		{
			CheckDisposed();

			// add title paragraphs
			StText title = m_scrInMemoryCache.AddTitleToMockedBook(m_book.Hvo,
				"Genesis or whatever");
			StTxtPara para2 = m_scrInMemoryCache.AddParaToMockedText(title.Hvo, "An anthology");
			StTxtPara para3 = m_scrInMemoryCache.AddParaToMockedText(title.Hvo, "Written by God");
			m_scrInMemoryCache.AddParaToMockedText(title.Hvo, "For Israel and everyone else");

			// set up the Scripture paragraph ChangeWatcher
			ScriptureChangeWatcher.Create(Cache);

			// Issue a PropChanged that should create a back translation paragraph.
			Cache.MainCacheAccessor.PropChanged(null,
				(int)PropChangeType.kpctNotifyAll,
				title.Hvo,
				(int)StText.StTextTags.kflidParagraphs,
				1, 2, 0);

			// Make sure the back translations got created.
			ScrTxtPara scrPara = new ScrTxtPara(Cache, para2.Hvo);
			ICmTranslation bt = scrPara.GetBT();
			Assert.IsNotNull(bt);
			Assert.IsNull(bt.Status.AnalysisDefaultWritingSystem);

			scrPara = new ScrTxtPara(Cache, para3.Hvo);
			bt = scrPara.GetBT();
			Assert.IsNotNull(bt);
			Assert.IsNull(bt.Status.AnalysisDefaultWritingSystem);
		}
		#endregion
	}
}
