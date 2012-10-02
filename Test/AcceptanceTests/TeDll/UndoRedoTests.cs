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
// File: UndoRedoTests.cs
// Responsibility: TE Team
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Windows.Forms;

using NUnit.Framework;

using SIL.FieldWorks.FDO;
//using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.RootSites;
//using SIL.FieldWorks.ScrImportComponents;
using SIL.FieldWorks.Test.ProjectUnpacker;
using SIL.FieldWorks.TE;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.Common.ScriptureUtils;
//using SILUBS.SharedScrUtils;

namespace SIL.FieldWorks.AcceptanceTests.TE
{

#if WANTPORT // (TE) These tests create the whole app and need a real DB to load. We need to figure out what to do about them

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Acceptance tests for Undo/Redo
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class UndoRedoTests: BaseTest
	{
		private readonly string m_sSvrName = Environment.MachineName + "\\SILFW";
		private const string m_sDbName = "testlangproj";
		private const string m_ProjName = "DEB-Debug";
		private RegistryData m_regData;
		private TestTeApp m_testTeApp;
		private TestTeDraftView m_firstDraftView = null;
		private TestTeMainWnd m_firstMainWnd = null;
		private bool m_fMainWindowOpened = false;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="UndoRedoTests"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public UndoRedoTests()
		{
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[SetUp]
		public void Init()
		{
			CheckDisposed();

			Unpacker.UnPackParatextTestProjects();
			m_regData = Unpacker.PrepareRegistryForPTData();

			// TeApp derives from FwApp
			m_testTeApp = new TestTeApp(new string[] {
				"-c", m_sSvrName,			// ComputerName (aka the SQL server)
				"-proj", m_ProjName,		// ProjectName
				"-db", m_sDbName});			// DatabaseName

			m_fMainWindowOpened = m_testTeApp.OpenMainWindow();

			if (m_fMainWindowOpened)
			{
				m_firstMainWnd = (TestTeMainWnd)m_testTeApp.MainWindows[0];
				// reload the styles from the database (test may have inserted new styles!)
				m_firstMainWnd.Synchronize(new SyncInfo(SyncMsg.ksyncStyle, 0, 0));
				// Set the view to the DraftView
				m_firstMainWnd.SelectScriptureDraftView();
				Application.DoEvents();

				// insert book tests create filters - turn them off to prevent interaction
				// between tests
				m_firstMainWnd.TurnOffAllFilters();

				m_firstDraftView = (TestTeDraftView)m_firstMainWnd.TheDraftView;
				m_firstDraftView.ActivateView();

				SelectionHelper helper = m_firstDraftView.SetInsertionPoint(0, 0, 0, 0, true);
				// helper.IhvoEndPara = -1;
				helper.SetSelection(m_firstDraftView, true, true);

				// we don't want to open a transaction!
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[TearDown]
		public void CleanUp()
		{
			CheckDisposed();

			bool wipedOutStuff = false;
			if (m_fMainWindowOpened)
			{
				try
				{
					// Undo everything that we can undo - checking to make sure we are not
					// in an infinite loop
					int undoCount = 0;
					while (m_firstMainWnd.Cache.CanUndo)
					{
						if (++undoCount <= 10)
						{
							m_firstMainWnd.SimulateEditUndoClick();
							Application.DoEvents();
						}
						else
						{
							// Do a complete clean up and re-init so next test can run
							// without impact from this test.
							wipedOutStuff = true;
							if (m_testTeApp != null)
							{
								m_testTeApp.ExitAppplication();
								m_testTeApp.Dispose();
								m_testTeApp = null;
							}

							if (m_regData != null)
								m_regData.RestoreRegistryData();
							Unpacker.RemoveParatextTestProjects();

							return;
						}
					}
					m_firstMainWnd.Cache.ActionHandlerAccessor.Commit();
				}
				catch(Exception e)
				{
					System.Diagnostics.Debug.WriteLine("Got exception in UndoRedoTests.CleanUp: "
						+ e.Message);
				}
			}
			if (!wipedOutStuff)
			{
				if (m_testTeApp != null)
				{
					m_testTeApp.ExitAppplication();
					m_testTeApp.Dispose();
					m_testTeApp = null;
				}

				if (m_regData != null)
					m_regData.RestoreRegistryData();
				Unpacker.RemoveParatextTestProjects();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UndoRedoSettingStyleFromStylesCombo()
		{
			CheckDisposed();
			string origStyleName;
			string styleName;
			int origType;

			m_firstDraftView.SelectRangeOfChars(1, 7, 1, 163, 181);
			origType = m_firstDraftView.EditingHelper.GetStyleNameFromSelection(out origStyleName);

			// Make sure selection has only a paragraph style, no character style.
			Assert.AreEqual(StyleType.kstParagraph, (StyleType)origType);

			// Choose a style from the combo.
			m_firstMainWnd.ChangeCharStyleSelection("Supplied");
			Application.DoEvents();

			// Verify the selection has the character style.
			int type = m_firstDraftView.EditingHelper.GetStyleNameFromSelection(out styleName);
			Assert.AreEqual(StyleType.kstCharacter, (StyleType)type);
			Assert.AreEqual("Supplied", styleName);

			m_firstMainWnd.SimulateEditUndoClick();
			Application.DoEvents();

			// Reselect the range of characters since view sycronization doesn't fully work
			// yet after undo. TODO TeTeam: this line should be removed when syncronization
			// restores (or keeps) the cursor in the proper place after an undo action.
			m_firstDraftView.SelectRangeOfChars(1, 7, 1, 163, 181);

			// Verify the selection has the original style and style type.
			type = m_firstDraftView.EditingHelper.GetStyleNameFromSelection(out styleName);
			Assert.AreEqual((StyleType)origType, (StyleType)type);
			Assert.AreEqual(origStyleName, styleName);

			m_firstMainWnd.SimulateEditRedoClick();
			Application.DoEvents();

			// Reselect the range of characters since view sycronization doesn't fully work
			// yet after undo. TODO TeTeam: this line should be removed when syncronization
			// restores (or keeps) the cursor in the proper place after an undo action.
			m_firstDraftView.SelectRangeOfChars(1, 7, 1, 163, 181);

			// Verify the selection is back to Book Title Secondary style.
			type = m_firstDraftView.EditingHelper.GetStyleNameFromSelection(out styleName);
			Assert.AreEqual(StyleType.kstCharacter, (StyleType)type);
			Assert.AreEqual("Supplied", styleName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that Import can be undone (TE-213)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("Import is no longer an undoable operation")]
		public void UndoImport()
		{
			CheckDisposed();
			// Need to have an IP
			m_firstDraftView.RootBox.MakeSimpleSel(true, true, false, true);

			// When we start up we shouldn't be able to Undo
			Assert.IsFalse(m_firstMainWnd.Cache.CanUndo, "Undo possible after startup");

			// set up a ScrImportSet for our test
			ScrImportSet settings = new ScrImportSet();
			m_firstMainWnd.Cache.LangProject.TranslatedScriptureOA.DefaultImportSettings = settings;
			ImportTests.MakeParatextImportTestSettings(settings);

			// do the import
			settings.StartRef = settings.EndRef = new BCVRef(63, 1, 1);
			m_firstMainWnd.ImportWithUndoTask(settings);

			Assert.IsTrue(m_firstMainWnd.Cache.CanUndo, "Undo not possible after import");

			m_firstMainWnd.SimulateEditUndoClick();
			Assert.IsFalse(m_firstMainWnd.Cache.CanUndo, "Import not undone");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that Import can be undone and not redone (TE-538)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("Import is no longer an undoable operation")]
		public void UndoImportNoRedo()
		{
			CheckDisposed();
			// Need to have an IP
			m_firstDraftView.RootBox.MakeSimpleSel(true, true, false, true);

			// When we start up we shouldn't be able to Undo
			Assert.IsFalse(m_firstMainWnd.Cache.CanUndo, "Undo possible after startup");

			// set up a ScrImportSet for our test
			ScrImportSet settings = new ScrImportSet();
			m_firstMainWnd.Cache.LangProject.TranslatedScriptureOA.DefaultImportSettings = settings;
			ImportTests.MakeParatextImportTestSettings(settings);

			// do the import
			settings.StartRef = settings.EndRef = new BCVRef(63, 1, 1);
			m_firstMainWnd.ImportWithUndoTask(settings);

			Assert.IsTrue(m_firstMainWnd.Cache.CanUndo, "Undo not possible after import");

			m_firstMainWnd.SimulateEditUndoClick();
			Assert.IsFalse(m_firstMainWnd.Cache.CanUndo, "Import not undone");

			// make sure that REDO is not enabled.
			Assert.IsFalse(m_firstMainWnd.Cache.CanRedo, "Import redo should not be enabled");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests undoing and redoing a inserted footnote
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UndoRedoInsertFootnote()
		{
			CheckDisposed();
			// Need to have an IP
			m_firstDraftView.RootBox.MakeSimpleSel(true, true, false, true);
			ScrBook book = (ScrBook)m_firstMainWnd.Cache.LangProject.TranslatedScriptureOA.ScriptureBooksOS[0];
			int nFootnotes = book.FootnotesOS.Count;

			// When we start up we shouldn't be able to Undo
			Assert.IsFalse(m_firstMainWnd.Cache.CanUndo, "Undo possible after startup");

			m_firstMainWnd.CallInsertFootnote();
			Assert.IsTrue(m_firstMainWnd.Cache.CanUndo, "Undo not possible after inserting footnote");
			Assert.AreEqual(nFootnotes+1, book.FootnotesOS.Count);

			m_firstMainWnd.SimulateEditUndoClick();
			Assert.IsFalse(m_firstMainWnd.Cache.CanUndo, "Inserting footnote not undone");
			Assert.AreEqual(nFootnotes, book.FootnotesOS.Count);

			m_firstMainWnd.SimulateEditRedoClick();
			Assert.IsTrue(m_firstMainWnd.Cache.CanUndo, "Undo not possible after redoing inserting footnote");
			Assert.AreEqual(nFootnotes+1, book.FootnotesOS.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting a footnote after undo
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UndoReinsertInsertFootnote()
		{
			CheckDisposed();
			// Need to have an IP
			m_firstDraftView.RootBox.MakeSimpleSel(true, true, false, true);
			ScrBook book = (ScrBook)m_firstMainWnd.Cache.LangProject.TranslatedScriptureOA.ScriptureBooksOS[0];
			int nFootnotes = book.FootnotesOS.Count;

			// When we start up we shouldn't be able to Undo
			Assert.IsFalse(m_firstMainWnd.Cache.CanUndo, "Undo possible after startup");

			m_firstMainWnd.CallInsertFootnote();
			Assert.IsTrue(m_firstMainWnd.Cache.CanUndo, "Undo not possible after inserting footnote");
			Assert.AreEqual(nFootnotes+1, book.FootnotesOS.Count);

			m_firstMainWnd.SimulateEditUndoClick();
			Assert.IsFalse(m_firstMainWnd.Cache.CanUndo, "Inserting footnote not undone");
			Assert.AreEqual(nFootnotes, book.FootnotesOS.Count);
			Application.DoEvents();

			m_firstDraftView.RootBox.MakeSimpleSel(true, true, false, true);
			m_firstDraftView.Focus();
			m_firstMainWnd.CallInsertFootnote();
			Assert.IsTrue(m_firstMainWnd.Cache.CanUndo, "Undo not possible after second inserting footnote");
			Assert.AreEqual(nFootnotes+1, book.FootnotesOS.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that undo once undoes only one footnote
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("Asserts in views code when inserting the second footnote. See TE-972 for more info")]
		public void UndoAfterInsertingTwoFootnotes()
		{
			CheckDisposed();
			ScrBook book = (ScrBook)m_firstMainWnd.Cache.LangProject.TranslatedScriptureOA.ScriptureBooksOS[0];
			int nFootnotes = book.FootnotesOS.Count;

			// When we start up we shouldn't be able to Undo
			Assert.IsFalse(m_firstMainWnd.Cache.CanUndo, "Undo possible after startup");

			m_firstDraftView.RootBox.MakeSimpleSel(true, true, false, true);
			m_firstMainWnd.CallInsertFootnote();
			m_firstMainWnd.CallInsertFootnote();
			Assert.IsTrue(m_firstMainWnd.Cache.CanUndo, "Undo not possible after inserting footnote");
			Assert.AreEqual(nFootnotes+2, book.FootnotesOS.Count);

			m_firstMainWnd.SimulateEditUndoClick();
			Assert.IsTrue(m_firstMainWnd.Cache.CanUndo, "Undo no longer possible after undoing one");
			Assert.AreEqual(nFootnotes + 1, book.FootnotesOS.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests undoing and redoing inserting a book
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UndoRedoInsertBook()
		{
			CheckDisposed();
			FdoCache cache = m_firstMainWnd.Cache;
			IScripture scripture = cache.LangProject.TranslatedScriptureOA;
			// Need to have an IP
			m_firstDraftView.RootBox.MakeSimpleSel(true, true, false, true);

			// When we start up we shouldn't be able to Undo
			Assert.IsFalse(cache.CanUndo, "Undo possible after startup");
			Assert.IsFalse(cache.CanRedo, "Redo possible after startup");

			int nBooks = scripture.ScriptureBooksOS.Count;
			m_firstDraftView.InsertBook(33);
			Assert.AreEqual(nBooks + 1, scripture.ScriptureBooksOS.Count);
			Assert.IsTrue(cache.CanUndo, "Undo not possible after inserting book");
			Assert.IsFalse(cache.CanRedo, "Redo possible after inserting book");

			m_firstMainWnd.SimulateEditUndoClick();
			Assert.AreEqual(nBooks, scripture.ScriptureBooksOS.Count);
			Assert.IsFalse(cache.CanUndo, "Undo possible after undo");
			Assert.IsTrue(cache.CanRedo, "Redo not possible after undo");

			m_firstMainWnd.SimulateEditRedoClick();
			Assert.AreEqual(nBooks + 1, scripture.ScriptureBooksOS.Count);
			Assert.IsTrue(cache.CanUndo, "Undo not possible after redo");
			Assert.IsFalse(cache.CanRedo, "Redo possible after redo");

			m_firstMainWnd.SimulateEditUndoClick();
			Assert.AreEqual(nBooks, scripture.ScriptureBooksOS.Count);
			Assert.IsFalse(cache.CanUndo, "Undo possible after undo");
			Assert.IsTrue(cache.CanRedo, "Redo not possible after undo");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests undoing and redoing inserting two books
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UndoRedoInsertingTwoBooks()
		{
			CheckDisposed();
			FdoCache cache = m_firstMainWnd.Cache;
			IScripture scripture = cache.LangProject.TranslatedScriptureOA;
			// Need to have an IP
			m_firstDraftView.RootBox.MakeSimpleSel(true, true, false, true);

			// When we start up we shouldn't be able to Undo
			Assert.IsFalse(cache.CanUndo, "Undo possible after startup");
			Assert.IsFalse(cache.CanRedo, "Redo possible after startup");

			int nBooks = scripture.ScriptureBooksOS.Count;
			m_firstDraftView.InsertBook(33);
			Assert.AreEqual(nBooks + 1, scripture.ScriptureBooksOS.Count);
			Assert.IsTrue(cache.CanUndo, "Undo not possible after inserting 1. book");
			Assert.IsFalse(cache.CanRedo, "Redo possible after inserting 1. book");

			m_firstDraftView.InsertBook(66);
			Assert.AreEqual(nBooks + 2, scripture.ScriptureBooksOS.Count);
			Assert.IsTrue(cache.CanUndo, "Undo not possible after inserting 2. book");
			Assert.IsFalse(cache.CanRedo, "Redo possible after inserting 2. book");

			m_firstMainWnd.SimulateEditUndoClick();
			m_firstMainWnd.SimulateEditUndoClick();
			Assert.AreEqual(nBooks, scripture.ScriptureBooksOS.Count);
			Assert.IsFalse(cache.CanUndo, "Undo possible after undo");
			Assert.IsTrue(cache.CanRedo, "Redo not possible after undo");

			m_firstMainWnd.SimulateEditRedoClick();
			m_firstMainWnd.SimulateEditRedoClick();
			Assert.AreEqual(nBooks + 2, scripture.ScriptureBooksOS.Count);
			Assert.IsTrue(cache.CanUndo, "Undo not possible after redo");
			Assert.IsFalse(cache.CanRedo, "Redo possible after redo");

			m_firstMainWnd.SimulateEditUndoClick();
			m_firstMainWnd.SimulateEditUndoClick();
			Assert.AreEqual(nBooks, scripture.ScriptureBooksOS.Count);
			Assert.IsFalse(cache.CanUndo, "Undo possible after undo");
			Assert.IsTrue(cache.CanRedo, "Redo not possible after undo");
		}
	}
#endif
}
