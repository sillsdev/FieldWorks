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
// File: TeMainWndTests.cs
// Responsibility: TeTeam
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.IO;

using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.ScrImportComponents;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Test.ProjectUnpacker;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.Common.UIAdapters;
using SIL.FieldWorks.Common.ScriptureUtils;

namespace SIL.FieldWorks.TE
{
	#region DummyTeMainWnd
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Dummy class to expose protected members of <see cref="TeMainWnd"/>
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DummyTeMainWnd: TeMainWnd
	{
		private DummyDraftView m_dummyDraftView = null;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is a constructor, dude!
		/// </summary>
		/// <remarks>Cache created here gets disposed in the teardown method</remarks>
		/// ------------------------------------------------------------------------------------
		public DummyTeMainWnd() : base(FdoCache.Create("TestLangProj"), null)
		{
			// Make sure we don't call InstallLanguage during tests.
			m_cache.LanguageWritingSystemFactoryAccessor.BypassInstall = true;
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
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_dummyDraftView = null; // Should be disposed by the window, since it is one of its controls.

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="importSettings"></param>
		/// ------------------------------------------------------------------------------------
		public void ImportFile(ScrImportSet importSettings)
		{
			CheckDisposed();

			TestImportManager.ImportWithUndoTask(this, importSettings);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Calls the base class implementation
		/// </summary>
		/// <returns>
		/// True, if the backup is performed successfully or the user elects not to back up;
		/// False, if the user chooses Cancel or if the backup fails.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public new bool EncourageBackup()
		{
			CheckDisposed();

			return base.EncourageBackup();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// We assume the only control in the main window's control collection is the dummy
		/// draft view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override DraftView TheDraftView
		{
			get
			{
				CheckDisposed();
				return m_dummyDraftView;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// For now, at least, tests will simulate the condition where there is no filter.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override bool BookFilterInEffect
		{
			get
			{
				CheckDisposed();
				return false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Allows mocking the style sheet
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public new FwStyleSheet StyleSheet
		{
			get
			{
				CheckDisposed();

				if (m_StyleSheet == null)
				{
					m_StyleSheet = new FwStyleSheet();
				}
				if (m_StyleSheet.Cache == null)
				{
					ILangProject lgproj = Cache.LangProject;
					IScripture scripture = lgproj.TranslatedScriptureOA;
					m_StyleSheet.Init(Cache, scripture.Hvo,
						(int)Scripture.ScriptureTags.kflidStyles);
				}
				return m_StyleSheet;
			}
			set { m_StyleSheet = value; }
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Creates a dummy draft view in the main window.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public void CreateDraftView()
		{
			CheckDisposed();

			// Check for verse bridge
			if (m_dummyDraftView != null)
				m_dummyDraftView.Dispose();
			m_dummyDraftView = new DummyDraftView(Cache, false, Handle.ToInt32());
			m_dummyDraftView.Name = "dummyDraftView";

			m_dummyDraftView.Visible = true;
			m_dummyDraftView.StyleSheet = StyleSheet;
			m_dummyDraftView.Anchor = AnchorStyles.Top | AnchorStyles.Left |
				AnchorStyles.Right | AnchorStyles.Bottom;
			m_dummyDraftView.Dock = DockStyle.Fill;
			Controls.Add(m_dummyDraftView);
			ClientWindows.Add(m_dummyDraftView.GetType().Name, m_dummyDraftView);
			m_dummyDraftView.MakeRoot(); // JT needed to add this to get tests to pass.
			m_dummyDraftView.ActivateView();
			// m_dummyDraftView.TeEditingHelper.InTestMode = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Destroy the draft view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void DestroyDraftView()
		{
			CheckDisposed();

			if (m_dummyDraftView != null)
			{
				Controls.Remove(m_dummyDraftView);
				m_dummyDraftView.Dispose(); // This will close the root box along with any other critical stuff.
				//m_dummyDraftView.RootBox.Close();
				m_dummyDraftView = null;
			}
		}
	}
	#endregion

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for <see cref="TeMainWnd"/>
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class TeMainWndTests : BaseTest
	{
		#region Data members
		private DummyTeMainWnd m_mainWnd;
		private FdoCache m_cache;
		private IScripture m_scr;
		private static int m_wsVern; // writing system info needed by tests
		private bool m_saveShowPrompts;
		#endregion

		#region Setup, Teardown
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialization called once.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void FixtureSetup()
		{
			CheckDisposed();
			base.FixtureSetup();

			// Save value of user prompt setting - restored in CleanUpFixture.
			m_saveShowPrompts = Options.ShowEmptyParagraphPromptsSetting;
			Options.ShowEmptyParagraphPromptsSetting = false;
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
				// Dispose window first, as it needs the cache to clear out its notification.
				if (m_mainWnd != null)
					m_mainWnd.Dispose();
				if (m_cache != null)
				{
					UndoResult ures = 0;
					while (m_cache.CanUndo)
					{
						m_cache.Undo(out ures);
						if (ures == UndoResult.kuresFailed  || ures == UndoResult.kuresError)
							Assert.Fail("ures should not be == " + ures.ToString());
					}
					m_cache.Dispose(); // Yes, since the window isn't supposed to do it.
				}
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_cache = null;
			m_mainWnd = null;
			m_scr = null;
			// Restore prompt setting
			Options.ShowEmptyParagraphPromptsSetting = m_saveShowPrompts; // Options is some kind of Registry gizmo.

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set up an initial transaction and undo item.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public void Initialize()
		{
			CheckDisposed();

			Debug.Assert(m_mainWnd == null, "m_mainWnd is not null.");
			//if (m_mainWnd != null)
			//	m_mainWnd.Dispose();
			m_mainWnd = new DummyTeMainWnd();
			m_cache = m_mainWnd.Cache;
			m_scr = m_cache.LangProject.TranslatedScriptureOA;
			m_wsVern = m_cache.DefaultVernWs;

			m_mainWnd.CreateDraftView();
			m_mainWnd.Show();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Undo all DB changes
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public void CleanUp()
		{
			CheckDisposed();

			UndoResult ures = 0;
			while (m_cache.CanUndo)
			{
				m_cache.Undo(out ures);
				if (ures == UndoResult.kuresFailed  || ures == UndoResult.kuresError)
					Assert.Fail("ures should not be == " + ures.ToString());
			}
			// Don't dispose the cache, until after the window closes, as it needs it
			// to clear its notification from the cache.
			// m_mainWnd.Hide();
			m_mainWnd.Close();
			// m_mainWnd.Dispose(); // Not needed, since Close calls Dispose.
			m_mainWnd = null;

			m_cache.Dispose();
			m_cache = null;
		}
		#endregion

		#region EncourageBackup
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the return values of the EncourageBackup method
		/// </summary>
		/// <remarks>This test does not work right now, because calling SendWait before opening
		/// the dialog sends the keystrokes to NUnit. We need to be able to wait for the
		/// creation of the dialog and then send the keystrokes (in a seperate thread).
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("Need to develop separate thread to send keystrokes to dialog")]
		public void EncourageBackup()
		{
			CheckDisposed();

			// Backup all databases except first (usually only one)
			SendKeys.SendWait("B%P %S");
			bool fRet = m_mainWnd.EncourageBackup();
			Assert.IsTrue(fRet, "EncourageBackup returned wrong value when doing backup");

			// need more tests for the other possibilities:
			// Open backup dialog but then close dialog

			// Open backup dialog and do a restore

			// Do import without backup

			// Cancel import
		}
		#endregion

		#region InsertSection Test
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the inserting a Scripture section. We need this one test to use a mainwnd and
		/// a view to make sure nothing bad happens in real life (TE-3987).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertSectionTest()
		{
			CheckDisposed();

			m_mainWnd.ActiveEditingHelper.GotoVerse(
				new ScrReference(57, 1, 3, Paratext.ScrVers.English));
			((DummyDraftView)m_mainWnd.TheDraftView).TurnOnHeightEstimator();
			Assert.IsTrue(m_mainWnd.ActiveEditingHelper.CreateSection(false));
		}
		#endregion

		#region InsertBook Test
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the inserting of a scripture book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertBookTest_BeforeEverything()
		{
			CheckDisposed();

			// Insert Exodus
			IScrBookRef bookRef = m_cache.ScriptureReferenceSystem.BooksOS[1];
			bookRef.BookName.VernacularDefaultWritingSystem = "FunSquigles5";
			bookRef.BookAbbrev.VernacularDefaultWritingSystem = "STH";
			IScrBook exodus = m_mainWnd.ActiveEditingHelper.InsertBook(2);

			// Verify a bunch of stuff after inserting a book.
			VerifyInsertedBook(exodus, "EXO");
			Assert.AreEqual(exodus.Hvo, m_scr.ScriptureBooksOS.HvoArray[0]);
			VerifyInsertedBookMainTitle(exodus.TitleOA, "Exodus");
			IScrSection section = exodus.SectionsOS[0];
			ScrSectionTests.VerifyInsertedBookSection(section, false, "1",
				"Chapter Number", m_cache.DefaultVernWs, 02001001);
			VerifyInsertedBookInDraftView("Exodus", exodus);

			// Verify that there is a filter in place for exodus
			Assert.AreEqual(1, m_mainWnd.BookFilter.BookCount);
			Assert.AreEqual(exodus.Hvo, m_mainWnd.BookFilter.GetBook(0).Hvo);

			Assert.AreEqual("Exodus", exodus.Name.UserDefaultWritingSystem);
			Assert.AreEqual("Exo", exodus.Abbrev.UserDefaultWritingSystem);
			Assert.AreEqual("FunSquigles5", exodus.Name.VernacularDefaultWritingSystem);
			Assert.AreEqual("STH", exodus.Abbrev.VernacularDefaultWritingSystem);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verify book is in the cache and that it only has one section.
		/// </summary>
		/// <param name="book"></param>
		/// <param name="sSilCode"></param>
		/// ------------------------------------------------------------------------------------
		private void VerifyInsertedBook(IScrBook book, string sSilCode)
		{
			Assert.IsNotNull(book, "new book wasn't inserted.");
			Assert.AreEqual(sSilCode, book.BookId);
			Assert.AreEqual(1, book.SectionsOS.Count, "Incorrect number of sections");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verify misc. stuff about the inserted book's main title.
		/// </summary>
		/// <param name="title">title</param>
		/// <param name="sText">Expected book title.</param>
		/// ------------------------------------------------------------------------------------
		private void VerifyInsertedBookMainTitle(IStText title, string sText)
		{
			FdoOwningSequence<IStPara> titleParas = title.ParagraphsOS;
			Assert.AreEqual(1, titleParas.Count, "The title should consist of 1 para");

			// Verify the main title's text and paragraph style is correct.
			Assert.IsNull(((StTxtPara)titleParas[0]).Contents.Text, "Incorrect book title");
			Assert.AreEqual(StyleUtils.ParaStyleTextProps(ScrStyleNames.MainBookTitle),
				((StTxtPara)titleParas[0]).StyleRules);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Performs a bunch of verifications in a draft view after a scripture book has been
		/// inserted.
		/// </summary>
		/// <param name="sTitle">Book main title expected in the draft view.</param>
		/// <param name="book">The ScrBook inserted in the draft view. The test assumes this
		/// is canonically the first book in the view.</param>
		/// ------------------------------------------------------------------------------------
		private void VerifyInsertedBookInDraftView(string sTitle, IScrBook book)
		{
			bool fAssocPrev;
			int ich, hvoObj, tag, enc;
			ITsString tss;
			SelectionHelper selHelper = SelectionHelper.Create(m_mainWnd.TheDraftView);

			// Make sure that IP has been given appropriate properties for typing regular
			// vernacular text (not continuing to enter text using "Chapter Number" style).
			Assert.AreEqual(4, selHelper.NumberOfLevels);
			// This better be the first book in the view.
			Assert.AreEqual(0, selHelper.LevelInfo[3].ihvo);
			// Since this book was just inserted, we should be in the first section.
			Assert.AreEqual(0, selHelper.LevelInfo[2].ihvo);
			// The InsertBook command should leave our insertion point in the Content of
			// this section (as opposed to the section Heading).
			Assert.AreEqual((int)ScrSection.ScrSectionTags.kflidContent,
				selHelper.LevelInfo[1].tag);
			// We should be in the first paragraph of that section's content.
			Assert.AreEqual(0, selHelper.LevelInfo[0].ihvo);
			// We should have a simple IP, not a range selection
			IVwSelection sel = selHelper.Selection;
			Assert.IsFalse(sel.IsRange);
			Assert.AreEqual(1, selHelper.IchAnchor, "IP should follow chapter number");
			// If the user starts typing, they should be entering regular vernacular text
			Assert.AreEqual(StyleUtils.CharStyleTextProps(null, m_wsVern), selHelper.SelProps);

			// Put cursor in the book title. If TestLangProj ever contains a book before sTitle
			// (i.e., canonically), then this will no longer be the first book in the view.
			m_mainWnd.TheDraftView.TeEditingHelper.GoToFirstBook();
			sel = m_mainWnd.TheDraftView.RootBox.Selection;
			sel.TextSelInfo(true, out tss, out ich, out fAssocPrev, out hvoObj, out tag, out enc);
			Assert.IsNull(tss.Text, "book title has a title");

			// Put the cursor in the empty section heading and verify stuff about the section
			// heading.
			m_mainWnd.TheDraftView.TeEditingHelper.GoToFirstSection();
			sel = m_mainWnd.TheDraftView.RootBox.Selection;
			sel.TextSelInfo(true, out tss, out ich, out fAssocPrev, out hvoObj, out tag, out enc);
			AssertEx.RunIsCorrect(tss, 0, null, null, m_wsVern);

			// Make sure the chapter number is set
			m_mainWnd.TheDraftView.SetInsertionPoint(0, 0, 0, 0, true);
			sel = m_mainWnd.TheDraftView.RootBox.Selection;
			sel.TextSelInfo(true, out tss, out ich, out fAssocPrev, out hvoObj, out tag, out enc);
			AssertEx.RunIsCorrect(tss, 0, "1", ScrStyleNames.ChapterNumber, m_wsVern);
		}
		#endregion

		#region Menu Update Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verify the contents of the InsertBook booklist menu items
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("Don't know how this will work now with the menu adapter.")]
		public void InsertBookMenu()
		{
			CheckDisposed();

			//			// note: this test does not require the DummyTEMainWnd to be displayed, only created
			//			// note: this test does not require the Paratext project files
			//
			//			//Simulate the popup event to disable existing books
			//			m_mainWnd.OnUpdateInsertBookMenu(m_mainWnd.m_mnuInsertBook);
			//
			//			// Verify that menu items have correct BookOrd and Text
			//			// we assume that the elements of MenuItems are in logical order, ie that Genesis is item zero
			//			// Genesis, OT index 0
			//			Assert.AreEqual("Genesis", m_mainWnd.m_mnuInsertBookOT.MenuItems[0].Text);
			//			// Malachi, OT index 38
			//			Assert.AreEqual("Malachi", m_mainWnd.m_mnuInsertBookOT.MenuItems[38].Text);
			//			// Matthew, NT index 0
			//			Assert.AreEqual("Matthew", m_mainWnd.m_mnuInsertBookNT.MenuItems[0].Text);
			//			// Revelation, NT index 26
			//			Assert.AreEqual("Revelation", m_mainWnd.m_mnuInsertBookNT.MenuItems[26].Text);
			//
			//			// Verify that books already in TestLangProj are shown as disabled
			//			if (m_cache.LangProject.TranslatedScriptureOA.FindBook("GEN") == null)
			//				Assert.IsTrue(m_mainWnd.m_mnuInsertBookOT.MenuItems[0].Enabled,
			//					"Genesis should be enabled"); // expected in a clean TestLangProj
			//			else
			//				Assert.IsFalse(m_mainWnd.m_mnuInsertBookOT.MenuItems[0].Enabled,
			//					"Genesis should be disabled");
			//
			//			if (m_cache.LangProject.TranslatedScriptureOA.FindBook("PHM") == null)
			//				Assert.IsTrue(m_mainWnd.m_mnuInsertBookNT.MenuItems[57-40].Enabled,
			//					"Philemon should be enabled");
			//			else
			//				Assert.IsFalse(m_mainWnd.m_mnuInsertBookNT.MenuItems[57-40].Enabled,
			//					"Philemon should be disabled"); // expected in a clean TestLangProj
		}
		#endregion
	}
}
