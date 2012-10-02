// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2002, SIL International. All Rights Reserved.
// <copyright from='2002' to='2002' company='SIL International'>
//		Copyright (c) 2002, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FindReplaceTests.cs
// Responsibility: TETeam
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Windows.Forms;

using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.TE;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.FwCoreDlgs;

namespace SIL.FieldWorks.AcceptanceTests.TE
{
	#region Dummy find dialog
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Expose protected members of the <see cref="FwFindReplaceDlg"/> class.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyFwFindReplaceDlg: FwFindReplaceDlg
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Expose the OnReplace method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void DoReplace()
		{
			CheckDisposed();

			OnReplace(null, null);
		}
	}
	#endregion

	#region Find/Replace tests
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Test Find and Replace operations.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class FindReplaceTests : BaseTest
	{
		private string m_sSvrName = MiscUtils.LocalServerName;
		private string m_sDbName = "testlangproj";
		private string m_ProjName = "DEB-Debug";
		private TestTeApp m_testTeApp;
		private TestTeDraftView m_firstDraftView = null;
		private TestTeMainWnd m_firstMainWnd = null;
		private bool m_fMainWindowOpened = false;
		private FwFindReplaceDlg.MatchType m_noMatchFoundType =
			FwFindReplaceDlg.MatchType.NotSet;

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make a TSS string from a text string.
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private ITsString MakeTSS(string str)
		{
			ITsStrFactory tsf = TsStrFactoryClass.Create();
			return tsf.MakeString(str, m_firstDraftView.Cache.DefaultVernWs);
		}
		#endregion

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public FindReplaceTests()
		{
		}

		#region Setup and Teardown
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Instantiate a TeApp object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			CheckDisposed();
			base.FixtureSetup();

			// TeApp derives from FwApp
			m_testTeApp = new TestTeApp(new string[] {
														 "-c", m_sSvrName,			// ComputerName (aka the SQL server)
														 "-proj", m_ProjName,		// ProjectName
														 "-db", m_sDbName});			// DatabaseName

			m_fMainWindowOpened = m_testTeApp.OpenMainWindow();
		}

		/// <summary>
		/// Correct way to deal with FixtureTearDown for class that derive from BaseTest.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose(bool disposing)
		{
			if (IsDisposed)
				return;

			if (disposing)
			{
				if (m_testTeApp != null)
				{
					m_testTeApp.ExitAppplication();
					m_testTeApp.Dispose();
				}
			}
			m_testTeApp = null;

			base.Dispose(disposing);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[SetUp]
		public void Init()
		{
			CheckDisposed();

			if (m_fMainWindowOpened)
			{
				m_firstMainWnd = (TestTeMainWnd)m_testTeApp.MainWindows[0];
				// Set the view to the DraftView
				m_firstMainWnd.SelectScriptureDraftView();
				Application.DoEvents();

				m_firstDraftView = (TestTeDraftView)m_firstMainWnd.TheDraftView;
				m_firstDraftView.ActivateView();

				SelectionHelper helper = m_firstDraftView.SetInsertionPoint(0, 0, 0, 0, true);
				// helper.IhvoEndPara = -1;
				helper.SetSelection(m_firstDraftView, true, true);

				if (!m_firstMainWnd.Cache.DatabaseAccessor.IsTransactionOpen())
					m_firstMainWnd.Cache.DatabaseAccessor.BeginTrans();
				m_firstMainWnd.Cache.BeginUndoTask("Undo TeAppTest", "Redo TeAppTest");
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

			if (m_fMainWindowOpened)
			{
				m_firstMainWnd.Cache.ActionHandlerAccessor.EndOuterUndoTask();
				while (m_firstMainWnd.Cache.Undo());

				// This will make sure the undo/redo stack is empty.
				m_firstMainWnd.Cache.ActionHandlerAccessor.Commit();

				//if (m_firstMainWnd.Cache.DatabaseAccessor.IsTransactionOpen())
				//	m_firstMainWnd.Cache.DatabaseAccessor.RollbackTrans();
			}
		}
		#endregion

		#region FindNext Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This tests that searching begins at the IP rather than at the top of the view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindNext_WhenChangingIPManually()
		{
			CheckDisposed();

			m_testTeApp.ShowFindReplaceDialog(false, m_firstDraftView);
			FwFindReplaceDlg dlg = m_testTeApp.FindReplaceDialog;

			dlg.FindText = MakeTSS("the");

			// make sure the initial find works
			dlg.FindNext();
			Assert.AreEqual(0, m_firstDraftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(2, m_firstDraftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(1, m_firstDraftView.ParagraphIndex);
			Assert.AreEqual(138, m_firstDraftView.SelectionAnchorIndex);
			Assert.AreEqual(142, m_firstDraftView.SelectionEndIndex);

			// make sure find next works
			dlg.FindNext();
			Assert.AreEqual(0, m_firstDraftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(3, m_firstDraftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(0, m_firstDraftView.ParagraphIndex);
			Assert.AreEqual(42, m_firstDraftView.SelectionAnchorIndex);
			Assert.AreEqual(46, m_firstDraftView.SelectionEndIndex);

			// make sure find next works finding in a book title
			dlg.FindNext();
			Assert.AreEqual(1, m_firstDraftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(-1, m_firstDraftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(0, m_firstDraftView.ParagraphIndex);
			Assert.AreEqual(0, m_firstDraftView.SelectionAnchorIndex);
			Assert.AreEqual(3, m_firstDraftView.SelectionEndIndex);

			// make sure find next works after setting the IP manually
			m_firstDraftView.SetInsertionPoint(2, 4, 1, 163, true);
			dlg.FindNext();
			Assert.AreEqual(2, m_firstDraftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(4, m_firstDraftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(1, m_firstDraftView.ParagraphIndex);
			Assert.AreEqual(174, m_firstDraftView.SelectionAnchorIndex);
			Assert.AreEqual(177, m_firstDraftView.SelectionEndIndex);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This tests that searching wraps around and continues searching from the top of the
		/// view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindNext_Wrap()
		{
			CheckDisposed();

			// Set IP to a point between the second and third occurances of the word 'Jude'.
			m_firstDraftView.SetInsertionPoint(2, 1, 0, 1, true);

			m_testTeApp.ShowFindReplaceDialog(false, m_firstDraftView);
			FwFindReplaceDlg dlg = m_testTeApp.FindReplaceDialog;

			dlg.FindText = MakeTSS("jude");

			// make sure the initial find works
			dlg.FindNext();
			Assert.AreEqual(2, m_firstDraftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(2, m_firstDraftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(0, m_firstDraftView.ParagraphIndex);
			Assert.AreEqual(9, m_firstDraftView.SelectionAnchorIndex);
			Assert.AreEqual(13, m_firstDraftView.SelectionEndIndex);

			// make sure the search wraps and finds the first occurance (in the view) of Jude.
			dlg.FindNext();
			Assert.AreEqual(2, m_firstDraftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(-1, m_firstDraftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(0, m_firstDraftView.ParagraphIndex);
			Assert.AreEqual(16, m_firstDraftView.SelectionAnchorIndex);
			Assert.AreEqual(20, m_firstDraftView.SelectionEndIndex);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This tests that we get a dialog saying that there are no more matches after we
		/// find all matches.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindNext_NoMoreMatchesFound()
		{
			CheckDisposed();

			m_testTeApp.ShowFindReplaceDialog(false, m_firstDraftView);
			FwFindReplaceDlg dlg = m_testTeApp.FindReplaceDialog;

			dlg.MatchNotFound += new FwFindReplaceDlg.MatchNotFoundHandler(FindDlgMatchNotFound);
			dlg.FindText = MakeTSS("jude");

			// make sure the initial find works
			dlg.FindNext();
			dlg.FindNext();
			dlg.FindNext();
			m_noMatchFoundType = FwFindReplaceDlg.MatchType.NotSet;
			dlg.FindNext();
			Assert.AreEqual(FwFindReplaceDlg.MatchType.NoMoreMatchesFound,
				m_noMatchFoundType);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This tests that we get a dialog saying that there are no matches found if we
		/// search for something that doesn't exist.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindNext_NoMatchesFound()
		{
			CheckDisposed();

			m_testTeApp.ShowFindReplaceDialog(false, m_firstDraftView);
			FwFindReplaceDlg dlg = m_testTeApp.FindReplaceDialog;

			dlg.MatchNotFound += new FwFindReplaceDlg.MatchNotFoundHandler(FindDlgMatchNotFound);
			dlg.FindText = MakeTSS("The will of the people");

			// make sure the initial find works
			m_noMatchFoundType = FwFindReplaceDlg.MatchType.NotSet;
			dlg.FindNext();
			Assert.AreEqual(FwFindReplaceDlg.MatchType.NoMatchFound,
				m_noMatchFoundType);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This tests that searching after a failed find, will work after changing the text
		/// to something that will be found.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindNext_AfterFailedFind()
		{
			CheckDisposed();

			m_testTeApp.ShowFindReplaceDialog(false, m_firstDraftView);
			FwFindReplaceDlg dlg = m_testTeApp.FindReplaceDialog;

			dlg.MatchNotFound += new FwFindReplaceDlg.MatchNotFoundHandler(FindDlgMatchNotFound);
			dlg.FindText = MakeTSS("The will of the people");

			// make sure the initial find works
			m_noMatchFoundType = FwFindReplaceDlg.MatchType.NotSet;
			dlg.FindNext();
			Assert.AreEqual(FwFindReplaceDlg.MatchType.NoMatchFound,
				m_noMatchFoundType);

			dlg.FindText = MakeTSS("jude");

			// make sure the search finds the first occurance (in the view) of Jude.
			dlg.FindNext();
			Assert.AreEqual(2, m_firstDraftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(-1, m_firstDraftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(0, m_firstDraftView.ParagraphIndex);
			Assert.AreEqual(16, m_firstDraftView.SelectionAnchorIndex);
			Assert.AreEqual(20, m_firstDraftView.SelectionEndIndex);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This tests that successive searches of different strings finds both and in the
		/// right place.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindNext_DifferentSearches()
		{
			CheckDisposed();

			m_testTeApp.ShowFindReplaceDialog(false, m_firstDraftView);
			FwFindReplaceDlg dlg = m_testTeApp.FindReplaceDialog;

			dlg.MatchNotFound += new FwFindReplaceDlg.MatchNotFoundHandler(FindDlgMatchNotFound);
			dlg.FindText = MakeTSS("the");

			// make sure the initial find works
			dlg.FindNext();
			Assert.AreEqual(0, m_firstDraftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(2, m_firstDraftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(1, m_firstDraftView.ParagraphIndex);
			Assert.AreEqual(138, m_firstDraftView.SelectionAnchorIndex);
			Assert.AreEqual(142, m_firstDraftView.SelectionEndIndex);

			dlg.FindText = MakeTSS("jude");

			// make sure the search finds the first occurance (in the view) of Jude.
			dlg.FindNext();
			Assert.AreEqual(2, m_firstDraftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(-1, m_firstDraftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(0, m_firstDraftView.ParagraphIndex);
			Assert.AreEqual(16, m_firstDraftView.SelectionAnchorIndex);
			Assert.AreEqual(20, m_firstDraftView.SelectionEndIndex);
		}
		#endregion

		#region Replace tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test an initial find when finding a match using the replace button. The first time
		/// the user presses the "Replace" button, we just find the next match, but we don't
		/// actually replace.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReplaceButtonInitiallyJustDoesAFind()
		{
			CheckDisposed();

			DummyFwFindReplaceDlg dlg = new DummyFwFindReplaceDlg();
			m_testTeApp.FindReplaceDialog = dlg;
			m_testTeApp.ShowFindReplaceDialog(true, m_firstDraftView);

			dlg.MatchNotFound += new FwFindReplaceDlg.MatchNotFoundHandler(FindDlgMatchNotFound);
			dlg.FindText = MakeTSS("the");
			dlg.ReplaceText = MakeTSS("da");

			// make sure the initial replace finds the first occurrence of "the"
			dlg.DoReplace();
			Assert.AreEqual(0, m_firstDraftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(2, m_firstDraftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(1, m_firstDraftView.ParagraphIndex);
			Assert.AreEqual(138, m_firstDraftView.SelectionAnchorIndex);
			// Selection is 4 chars long because it's an accented "é"
			Assert.AreEqual(142, m_firstDraftView.SelectionEndIndex);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test a replace when finding a match using the replace button. This simulates the
		/// "second" time the user presses Replace, where we actually do the replace and then
		/// go on to find the next match.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReplaceButtonTest()
		{
			CheckDisposed();

			DummyFwFindReplaceDlg dlg = new DummyFwFindReplaceDlg();
			m_testTeApp.FindReplaceDialog = dlg;
			m_testTeApp.ShowFindReplaceDialog(true, m_firstDraftView);

			dlg.MatchNotFound += new FwFindReplaceDlg.MatchNotFoundHandler(FindDlgMatchNotFound);
			dlg.FindText = MakeTSS("the");
			dlg.ReplaceText = MakeTSS("da");

			// Initial replace finds the first occurrence of "the"
			dlg.DoReplace();
			// Second replace actually replaces it and finds the following occurrence

			dlg.DoReplace();
			Assert.AreEqual(0, m_firstDraftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(3, m_firstDraftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(0, m_firstDraftView.ParagraphIndex);
			Assert.AreEqual(42, m_firstDraftView.SelectionAnchorIndex);
			// Selection is 4 chars long because it's an accented "é"
			Assert.AreEqual(46, m_firstDraftView.SelectionEndIndex);

			// Now make sure first replacement happened.
			// Set IP Back to the top of the view
			m_firstDraftView.SetInsertionPoint(0, 0, 0, 0, true);
			dlg.FindText = MakeTSS("dao"); // The original word was Théo
			dlg.FindNext();
			Assert.AreEqual(0, m_firstDraftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(2, m_firstDraftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(1, m_firstDraftView.ParagraphIndex);
			Assert.AreEqual(138, m_firstDraftView.SelectionAnchorIndex);
			Assert.AreEqual(141, m_firstDraftView.SelectionEndIndex);
		}
		#endregion

		#region Match Not Found handler
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is used to prevent displaying a Message box when no match is found
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="defaultMsg"></param>
		/// <param name="noMatchFoundType"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private bool FindDlgMatchNotFound(object sender, string defaultMsg,
			FwFindReplaceDlg.MatchType noMatchFoundType)
		{
			m_noMatchFoundType = noMatchFoundType;
			return false;
		}
		#endregion
	}
	#endregion
}
