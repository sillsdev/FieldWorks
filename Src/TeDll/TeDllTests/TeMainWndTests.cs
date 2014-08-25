// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: TeMainWndTests.cs
// Responsibility: TeTeam
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System.Diagnostics;
using System.Windows.Forms;
using System.Linq;
using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.Resources;
using SIL.Utils;
using SILUBS.SharedScrUtils;

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
		public DummyTeMainWnd(FdoCache cache) : base(cache)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// We assume the only control in the main window's control collection is the dummy
		/// draft view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override DraftView TheDraftView
		{
			get { return m_dummyDraftView; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// For now, at least, tests will simulate the condition where there is no filter.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override bool BookFilterInEffect
		{
			get { return false;	}
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
				if (m_StyleSheet == null)
				{
					m_StyleSheet = new FwStyleSheet();
				}
				if (m_StyleSheet.Cache == null)
				{
					ILangProject lgproj = Cache.LangProject;
					IScripture scripture = lgproj.TranslatedScriptureOA;
					m_StyleSheet.Init(Cache, scripture.Hvo, ScriptureTags.kflidStyles, ResourceHelper.DefaultParaCharsStyleName);
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
			m_rgClientViews.Add(m_dummyDraftView.GetType().Name, m_dummyDraftView);
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
			if (m_dummyDraftView != null)
			{
				Controls.Remove(m_dummyDraftView);
				m_dummyDraftView.Dispose(); // This will close the root box along with any other critical stuff.
				//m_dummyDraftView.RootBox.Close();
				m_dummyDraftView = null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Closes the rootsites of all views and disposes of them (needed so that undoing
		/// changes made during the tests won't cause views to respond to changes).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal void CloseViews()
		{
			foreach (IRootSite site in m_rgClientViews.Values)
			{
				site.CloseRootBox();
				((IFWDisposable)site).Dispose();
			}
			m_rgClientViews.Clear();
		}
	}
	#endregion

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for <see cref="TeMainWnd"/>
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class TeMainWndTests : TeTestBase
	{
		#region Data members
		private DummyTeMainWnd m_mainWnd;
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
			base.FixtureSetup();

			// Save value of user prompt setting - restored in CleanUpFixture.
			m_saveShowPrompts = Options.ShowEmptyParagraphPromptsSetting;
			Options.ShowEmptyParagraphPromptsSetting = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set up an initial transaction and undo item.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public override void TestSetup()
		{
			base.TestSetup();
			Debug.Assert(m_mainWnd == null, "m_mainWnd is not null.");
			m_mainWnd = new DummyTeMainWnd(Cache);
			m_wsVern = Cache.DefaultVernWs;

			m_mainWnd.CreateDraftView();
			m_mainWnd.Show();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Undo all DB changes
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public override void TestTearDown()
		{
			if (m_mainWnd != null)
			{
				m_mainWnd.CloseViews();

				// Before we close the main window undo anything on the main window's undo stack
				// (separate from the undo stack created when the cache was created).
				UndoAll();

				m_mainWnd.Close();
			}
			m_mainWnd = null;

			base.TestTearDown();
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
			CreateExodusData();
			m_mainWnd.TheDraftView.RefreshDisplay();
			Assert.IsTrue(m_mainWnd.ActiveEditingHelper.GotoVerse(
				new ScrReference(2, 1, 3, ScrVers.English)));
			((DummyDraftView)m_mainWnd.TheDraftView).TurnOnHeightEstimator();
			Assert.IsNotNull(m_mainWnd.ActiveEditingHelper.CreateSection(false));
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
			// Insert Exodus
			IScrBookRef bookRef = Cache.ServiceLocator.GetInstance<IScrRefSystemRepository>().AllInstances().First().BooksOS[1];
			bookRef.BookName.SetVernacularDefaultWritingSystem("FunSquigles5");
			bookRef.BookName.SetUserWritingSystem("Exodus");
			bookRef.BookAbbrev.SetVernacularDefaultWritingSystem("STH");
			bookRef.BookAbbrev.SetUserWritingSystem("Exo");
			((TestTeEditingHelper)m_mainWnd.ActiveEditingHelper).m_DeferSelectionUntilEndOfUOW = true;
			IScrBook exodus = m_mainWnd.ActiveEditingHelper.InsertBook(2);

			// Verify a bunch of stuff after inserting a book.
			TeEditingHelperTestsWithMockedFdoCache.VerifyInsertedBook(exodus, "EXO");
			Assert.AreEqual(exodus, m_scr.ScriptureBooksOS[0]);
			VerifyInsertedBookInDraftView(((DummyDraftView)m_mainWnd.TheDraftView).RequestedSelectionAtEndOfUow);

			// Verify that there is a filter in place for exodus
			Assert.AreEqual(1, m_mainWnd.BookFilter.BookCount);
			Assert.AreEqual(exodus.Hvo, m_mainWnd.BookFilter.GetBook(0).Hvo);

			Assert.AreEqual("Exodus", exodus.Name.UserDefaultWritingSystem.Text);
			Assert.AreEqual("Exo", exodus.Abbrev.UserDefaultWritingSystem.Text);
			Assert.AreEqual("FunSquigles5", exodus.Name.VernacularDefaultWritingSystem.Text);
			Assert.AreEqual("STH", exodus.Abbrev.VernacularDefaultWritingSystem.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Performs a bunch of verifications in a draft view after a scripture book has been
		/// inserted.
		/// </summary>
		/// <param name="selHelper">The selection helper represnting the selection that is
		/// made when the book is inserted.</param>
		/// ------------------------------------------------------------------------------------
		private void VerifyInsertedBookInDraftView(SelectionHelper selHelper)
		{
			bool fAssocPrev;
			int ich, hvoObj, tag, enc;
			ITsString tss;

			// Make sure that IP has been given appropriate properties for typing regular
			// vernacular text (not continuing to enter text using "Chapter Number" style).
			Assert.AreEqual(4, selHelper.NumberOfLevels);
			// This better be the first book in the view.
			Assert.AreEqual(0, selHelper.LevelInfo[3].ihvo);
			// Since this book was just inserted, we should be in the first section.
			Assert.AreEqual(0, selHelper.LevelInfo[2].ihvo);
			// The InsertBook command should leave our insertion point in the Content of
			// this section (as opposed to the section Heading).
			Assert.AreEqual(ScrSectionTags.kflidContent,
				selHelper.LevelInfo[1].tag);
			// We should be in the first paragraph of that section's content.
			Assert.AreEqual(0, selHelper.LevelInfo[0].ihvo);
			// We should have a simple IP, not a range selection
			Assert.IsFalse(selHelper.IsRange);
			Assert.AreEqual(1, selHelper.IchAnchor, "IP should follow chapter number");
#if WANTTESTPORT // (TE) This is probably unnecessary. If needed at all, we can probably change the selection props whenever a selection is created next to a chapter number.
			// If the user starts typing, they should be entering regular vernacular text
			Assert.AreEqual(StyleUtils.CharStyleTextProps(null, m_wsVern), selHelper.SelProps);
#endif
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
			//			if (Cache.LangProject.TranslatedScriptureOA.FindBook("GEN") == null)
			//				Assert.IsTrue(m_mainWnd.m_mnuInsertBookOT.MenuItems[0].Enabled,
			//					"Genesis should be enabled"); // expected in a clean TestLangProj
			//			else
			//				Assert.IsFalse(m_mainWnd.m_mnuInsertBookOT.MenuItems[0].Enabled,
			//					"Genesis should be disabled");
			//
			//			if (Cache.LangProject.TranslatedScriptureOA.FindBook("PHM") == null)
			//				Assert.IsTrue(m_mainWnd.m_mnuInsertBookNT.MenuItems[57-40].Enabled,
			//					"Philemon should be enabled");
			//			else
			//				Assert.IsFalse(m_mainWnd.m_mnuInsertBookNT.MenuItems[57-40].Enabled,
			//					"Philemon should be disabled"); // expected in a clean TestLangProj
		}
		#endregion
	}
}
