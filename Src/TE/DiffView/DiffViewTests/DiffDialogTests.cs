// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2003-2006, SIL International. All Rights Reserved.
// <copyright from='2003' to='2006' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: DiffDialogTests.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System.Diagnostics;
using System.Windows.Forms;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Test.TestUtils;
using SIL.Utils;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.CoreImpl;

namespace SIL.FieldWorks.TE
{
	#region DummyDiffDialog class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyDiffDialog : DiffDialog
	{
		/// <summary>Determines whether </summary>
		public bool m_fPermitRevertToOld;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="bookMerger"></param>
		/// <param name="cache"></param>
		/// <param name="stylesheet"></param>
		/// <param name="draft"></param>
		/// ------------------------------------------------------------------------------------
		public DummyDiffDialog(BookMerger bookMerger, FdoCache cache, IVwStylesheet stylesheet,
			IScrDraft draft) :
			base(bookMerger, cache, stylesheet, 1.0f, 1.0f, null, null)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether the window will be activated when it is shown.
		/// </summary>
		/// <value></value>
		/// <returns>Always <c>true</c>.</returns>
		/// ------------------------------------------------------------------------------------
		protected override bool ShowWithoutActivation
		{
			get { return true; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal DiffView LeftView
		{
			get
			{
					return m_diffViewWrapper.RevisionDiffView;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal DiffView RightView
		{
			get
			{
					return m_diffViewWrapper.CurrentDiffView;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal DifferenceList DifferenceList
		{
			get
			{
					return m_differences;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool EditMode
		{
			get
			{
					return m_editMode;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public BookMerger BookMerger
		{
			get
			{
					return m_bookMerger;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SimulatePrevButtonClick()
		{
			btnPrev_Click(null, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SimulateNextButtonClick()
		{
			btnNext_Click(null, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SimulateRevertToOldButtonClick(bool createUndoTask)
		{
			if (createUndoTask)
				btnRevertToOld_Click(null, null);
			else
				RevertToOld();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SimulateKeepCurrentClick(bool createUndoTask)
		{
			if (createUndoTask)
				btnKeepCurrent_Click(null, null);
			else
				KeepCurrent();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void PerformUndo()
		{
			OnEditUndo(null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void PerformRedo()
		{
			OnEditRedo(null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Performs the paste.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void PerformPaste()
		{
			OnEditPaste(null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the active view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override IRootSite ActiveView
		{
			get
			{
					return RightView;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Simulates the user confirming or rejecting the action which will result in losing
		/// data by deleting what is in the current version.
		/// </summary>
		/// <returns>
		/// 	<c>true</c> if the user confirms desire to delete dat from the current
		/// version; <c>false</c> otherwise.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		protected override bool AllowUseThisVersionToLoseData()
		{
			return m_fPermitRevertToOld;
		}
	}
	#endregion

	#region MockedCacheDiffDialog class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This is a dummy diff dialog designed to work with a mocked cache. Doesn't support undo
	/// tasks
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class MockedCacheDiffDialog : DummyDiffDialog
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="bookMerger"></param>
		/// <param name="cache"></param>
		/// <param name="stylesheet"></param>
		/// <param name="draft"></param>
		/// ------------------------------------------------------------------------------------
		public MockedCacheDiffDialog(BookMerger bookMerger, FdoCache cache,
			IVwStylesheet stylesheet, IScrDraft draft) :
			base(bookMerger, cache, stylesheet, draft)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether the window will be activated when it is shown.
		/// </summary>
		/// <value></value>
		/// <returns>Always <c>true</c>.</returns>
		/// ------------------------------------------------------------------------------------
		protected override bool ShowWithoutActivation
		{
			get { return true; }
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Do nothing
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected override void CreateUndoMark()
		{
			// do nothing
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Do nothing
		/// </summary>
		/// <param name="args">Unused</param>
		/// <returns><c>true</c></returns>
		/// ------------------------------------------------------------------------------------
		protected override bool OnUpdateEditUndo(object args)
		{
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Do nothing
		/// </summary>
		/// <param name="args">Unused</param>
		/// <returns><c>true</c></returns>
		/// ------------------------------------------------------------------------------------
		protected override bool OnDropDownEditUndo(object args)
		{
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Do nothing
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
		{
			// Do nothing
		}
	}
	#endregion

	#region DummyDiffViewEditingHelper class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DummyDiffViewEditingHelper : DiffViewEditingHelper
	{
		internal DummyDiffViewEditingHelper(IEditingCallbacks callbacks, FdoCache cache, IScrBook book)
			: base(callbacks, cache, 0, TeViewType.Scripture | TeViewType.DiffView, book, null)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a flag indicating whether to defer setting a selection until the end of the
		/// Unit of Work.
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		protected override bool DeferSelectionUntilEndOfUOW
		{
			get { return false; }
		}
	}
	#endregion

	#region DummyDiffView class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DummyDiffView : DiffView
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the DummyDiffView class
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="book">Scripture book to be displayed as the root in this view</param>
		/// <param name="differences">List of differences</param>
		/// <param name="fRev"><c>true</c> if we display the revision, <c>false</c> if we
		/// display the current version.</param>
		/// ------------------------------------------------------------------------------------
		public DummyDiffView(FdoCache cache, IScrBook book, DifferenceList differences, bool fRev)
			: base(cache, book, differences, fRev, 0, null)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		public void ExposeOnKeyDown (KeyEventArgs e)
		{
			base.OnKeyDown(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		public void ExposeOnKeyPress (KeyPressEventArgs e)
		{
			base.OnKeyPress(e);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Recompute the layout
		/// </summary>
		/// <param name="levent"></param>
		/// -----------------------------------------------------------------------------------
		protected override void OnLayout(LayoutEventArgs levent)
		{
			using(new HoldGraphics(this))
			{
				if (DoLayout())
					Invalidate();
			}
			base.OnLayout(levent);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Provide a testing-specific implementation of the DiffViewEditingHelper
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override EditingHelper CreateEditingHelper()
		{
			return new DummyDiffViewEditingHelper(this, Cache, m_scrBook);
		}
	}
	#endregion

	#region DiffDialogTests class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for the <see cref="DiffDialog"/> class.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class DiffDialogTests : ScrInMemoryFdoTestBase
	{
		private BookMerger m_bookMerger;
		private IScrBook m_philemonRev;
		private IScrBook m_philemonCurr;
		private DummyDiffDialog m_dlg;
		private IScrDraft m_draft;
		private FwStyleSheet m_styleSheet;

		#region Setup/Teardown

		/// <summary>
		/// The DiffDialog tests are dependent on Times New Roman
		/// </summary>
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.DefaultFontName = "Times New Roman";
		}

		/// <summary/>
		public override void FixtureTeardown()
		{
			KeyboardHelper.Release();
			base.FixtureTeardown();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Override to start an undoable UOW.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void TestSetup()
		{
			base.TestSetup();
			// Because these tests do a lot with undo/redo, we want to end the undo task so that
			// tasks will get created for the work that is done during the test. This also means
			// that we need to call the versions of the methods that create a UOW since there
			// will no longer be a UOW in progress.
			m_actionHandler.EndUndoTask();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clean up after test.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public override void TestTearDown()
		{
			// m_actionHandler.BeginUndoTask("for teardown", "for teardown");

			if (m_dlg != null)
			{
				m_dlg.Close();
				m_dlg.Dispose();
				m_dlg = null;
			}

			// Have to do this before the BookMerger is zapped,
			// or the Difference objects in some special action handler blow up.
			base.TestTearDown();

			// clear out the difference list.
			if (m_bookMerger != null)
			{
				// No need to remove them this way,
				// as Dispsoe does a much better job of wiping out a BookMerger object now.
				//while (m_bookMerger.Differences.MoveFirst() != null)
				//	m_bookMerger.Differences.Remove(m_bookMerger.Differences.CurrentDifference);
				m_bookMerger.Dispose();
				m_bookMerger = null;
			}
			m_philemonRev = null;
			m_philemonCurr = null;
			m_draft = null;
			m_styleSheet = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overridden to create Philemon with three sections:
		/// IntroSection
		///		Paragraph A
		///		Paragraph B
		/// Section
		///		Paragraph Verse 1
		/// Section2
		///		Paragraph2 Verses 17,18,21
		///		Paragraph3 Verse 22,23
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			//Philemon
			IScrBook book = AddBookToMockedScripture(57, "Philemon");
			AddTitleToMockedBook(book, "Philemon");

			// intro section
			IScrSection introSection = AddSectionToMockedBook(book, true);
			AddSectionHeadParaToSection(introSection, "Intro section",
				ScrStyleNames.IntroSectionHead);
			IScrTxtPara para = AddParaToMockedSectionContent(introSection,
				ScrStyleNames.IntroParagraph);
			AddRunToMockedPara(para, "This is the intro para.", null);
			para = AddParaToMockedSectionContent(introSection,
				ScrStyleNames.IntroParagraph);
			AddRunToMockedPara(para, "Another intro para.", null);

			// section1 -with verse 1
			IScrSection section = AddSectionToMockedBook(book);
			AddSectionHeadParaToSection(section, "Paul tells people",
				ScrStyleNames.SectionHead);
			para = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para,
				"and the earth was without form and void and darkness covered the face of the deep",
				null);

			// section 2 - with verses 17-23
			IScrSection section2 = AddSectionToMockedBook(book);
			AddSectionHeadParaToSection(section2, "Paul tells people more",
				ScrStyleNames.SectionHead);
			IScrTxtPara para2 = AddParaToMockedSectionContent(section2,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para2, "17", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para2, "paul expounds on the nature of reality",
				null);
			AddRunToMockedPara(para2, "18", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para2, "This is verse 18.",
				null);
			AddRunToMockedPara(para2, "21", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para2, "And verse 21.",
				null);
			IScrTxtPara para3 = AddParaToMockedSectionContent(section2,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para3, "22", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para3, "the existentialists are all wrong. ",
				null);
			AddRunToMockedPara(para3, "23", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para3, "The last verse.",
				null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Completes the initialize. Every test needs to call this before initializing.
		/// We don't include this in the initialize run before every test because, if we want
		/// to create another archive, it will cause a crash.
		/// </summary>
		/// <param name="fMakePhilemonChanges">if <c>true</c> make changes in Philemon and
		/// detect differences</param>
		/// <param name="bookRev">the book to use for the revision in the bookmerger.</param>
		/// ------------------------------------------------------------------------------------
		private void CompleteInitialize(bool fMakePhilemonChanges, IScrBook bookRev)
		{
			Debug.Assert(m_bookMerger == null, "m_bookMerger is not null.");

			if (fMakePhilemonChanges)
			{
				using (UndoableUnitOfWorkHelper undoHelper = new UndoableUnitOfWorkHelper(
					m_actionHandler, "revision"))
				{
					m_philemonCurr = m_scr.FindBook(57);
					m_draft = Cache.ServiceLocator.GetInstance<IScrDraftFactory>().Create("PhilemonArchive", new IScrBook[] { m_philemonCurr });
					m_philemonRev = m_draft.FindBook(57);
					m_draft.Type = ScrDraftType.ImportedVersion;

					undoHelper.RollBack = false;
				}
			}
			m_bookMerger = new BookMerger(Cache, null, fMakePhilemonChanges ? m_philemonRev : bookRev);

			if (fMakePhilemonChanges)
			{
				using (UndoableUnitOfWorkHelper undoHelper = new UndoableUnitOfWorkHelper(
					m_actionHandler, "Philemon changes"))
				{
					MakeChangesInPhilemonCurrent();
					m_bookMerger.DetectDifferences(null);
					Assert.AreEqual(6, m_bookMerger.Differences.Count,
						"Problem in TestSetup (unexpected number of diffs)");

					undoHelper.RollBack = false;
				}
			}

			m_styleSheet = new FwStyleSheet();
			m_styleSheet.Init(Cache, m_scr.Hvo, ScriptureTags.kflidStyles);

			Debug.Assert(m_dlg == null, "m_dlg is not null.");
			//if (m_dlg != null)
			//	m_dlg.Dispose();
			m_dlg = new DummyDiffDialog(m_bookMerger, Cache, m_styleSheet, null);
			m_dlg.CreateControl();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make changes in the Current book of Philemon:
		/// in the intro section, add " Diff" to heading and truncate the content paragraph,
		/// in Phil 1:17 delete characters 5-9 and the last five characters,
		/// and delete verse number of Philemon 1:23.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void MakeChangesInPhilemonCurrent()
		{
			// intro section:
			IScrSection section = m_philemonCurr.SectionsOS[0];

			// modify intro section heading
			IScrTxtPara paraHeading = (IScrTxtPara)section.HeadingOA.ParagraphsOS[0];
			ITsStrBldr tssBldr = paraHeading.Contents.GetBldr();
			tssBldr.Replace(paraHeading.Contents.Length - 1, paraHeading.Contents.Length - 1,
				" Diff", paraHeading.Contents.get_Properties(0));
			paraHeading.Contents = tssBldr.GetString();

			// modify intro content paragraph
			IScrTxtPara para = (IScrTxtPara)section.ContentOA.ParagraphsOS[0];
			tssBldr = para.Contents.GetBldr();
			tssBldr.Replace(10, para.Contents.Length, string.Empty, null);
			para.Contents = tssBldr.GetString();

			// last scripture section:
			section = m_philemonCurr.SectionsOS[2];
			// first para: modify Phm 1:17-21 to have two changes in the same paragraph
			para = (IScrTxtPara)section.ContentOA.ParagraphsOS[0];
			tssBldr = para.Contents.GetBldr();
			tssBldr.Replace(5, 9, string.Empty, null);
			int curLen = tssBldr.Length;
			tssBldr.Replace(curLen - 5, curLen, string.Empty, null);
			para.Contents = tssBldr.GetString();

			// second para: remove verse number of Phm 1:23. This will cause 2 diffs.
			para = (IScrTxtPara)section.ContentOA.ParagraphsOS[1];
			tssBldr = para.Contents.GetBldr();
			tssBldr.Replace(37, 39, null, null);
			para.Contents = tssBldr.GetString();
		}
		#endregion

		#region DiffDialog Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// TE-4449: This will test that the IP is in the section head when a diff is in the
		/// section head.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CheckIP_SectionHeadDiff()
		{
			CompleteInitialize(true, m_philemonRev);

			using (DummyDiffView view = new DummyDiffView(Cache, m_philemonCurr,
				m_bookMerger.Differences, false))
			{
				view.StyleSheet = m_styleSheet;
				view.MakeRoot();
				view.PerformLayout();
				view.RootBox.Activate(VwSelectionState.vssEnabled);
				Difference diff1 = m_bookMerger.Differences.MoveFirst();
				view.ScrollToParaDiff(diff1.ParaCurr, diff1.IchMinCurr);

				// The IP should be set in a section heading and at the beginning of the difference.
				SelectionHelper helper = SelectionHelper.Create(view);
				Assert.AreEqual(12, helper.IchAnchor);
				Assert.AreEqual(ScrSectionTags.kflidHeading, helper.LevelInfo[1].tag);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the arrow keys are able to move the IP when the view is not editable.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void KeyboardHandling_arrowsNonEditable()
		{
			CompleteInitialize(true, m_philemonRev);

			using (DummyDiffView view = new DummyDiffView(Cache, m_philemonCurr,
				m_bookMerger.Differences, false))
			{
				view.StyleSheet = m_styleSheet;
				view.MakeRoot();
				view.PerformLayout();
				view.RootBox.Activate(VwSelectionState.vssEnabled);
				// if we try to set the insertion point to section 0 it doesn't work.
				// Couldn't figure out why.
				view.SetInsertionPoint(0, 1, 0, 0, true);
				// press the down arrow key
				view.ExposeOnKeyDown(new KeyEventArgs(Keys.Down));
				SelectionHelper helper = SelectionHelper.Create(view);
				Assert.IsTrue(helper.IchAnchor > 15, "IP should move down");
				// press the up arrow key
				view.ExposeOnKeyDown(new KeyEventArgs(Keys.Up));
				helper = SelectionHelper.Create(view);
				Assert.AreEqual(0, helper.IchAnchor, "IP should move back to the previous location");
				// press the right arrow key
				view.ExposeOnKeyDown(new KeyEventArgs(Keys.Right));
				helper = SelectionHelper.Create(view);
				Assert.AreEqual(1, helper.IchAnchor, "IP should move to the right");
				// press the left arrow key
				view.ExposeOnKeyDown(new KeyEventArgs(Keys.Left));
				helper = SelectionHelper.Create(view);
				Assert.AreEqual(0, helper.IchAnchor, "IP should move back to the previous location");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the user cannot type in the read-only view
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void KeyboardHandling_otherKeysNonEditable()
		{
			CompleteInitialize(true, m_philemonRev);

			using (DummyDiffView view = new DummyDiffView(Cache, m_philemonRev,
				m_bookMerger.Differences, true))
			{
				view.StyleSheet = m_styleSheet;
				view.MakeRoot();
				view.PerformLayout();
				view.RootBox.Activate(VwSelectionState.vssEnabled);
				view.RootBox.MakeSimpleSel(true, true, true, true);
				view.SetInsertionPoint(0, 1, 0, 0, true);
				IScrTxtPara para = (IScrTxtPara)m_philemonRev.SectionsOS[1].ContentOA.ParagraphsOS[0];
				int charCount = para.Contents.Length;
				// press a letter
				view.ExposeOnKeyPress(new KeyPressEventArgs('f'));
				SelectionHelper helper = SelectionHelper.Create(view);
				Assert.AreEqual(0, helper.IchAnchor, "IP should not move");
				Assert.AreEqual(charCount, para.Contents.Length);
				// press the enter key
				view.ExposeOnKeyDown(new KeyEventArgs(Keys.Enter));
				helper = SelectionHelper.Create(view);
				Assert.AreEqual(0, helper.IchAnchor, "IP should not move");
				Assert.AreEqual(charCount, para.Contents.Length);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// TE-4224: Tests that user can delete a paragraph break in edit mode.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void KeyboardHandling_DeleteParaBreak()
		{
			CompleteInitialize(true, m_philemonRev);

			using (DummyDiffView view = new DummyDiffView(Cache, m_philemonRev,
				m_bookMerger.Differences, true))
			{
				view.StyleSheet = m_styleSheet;
				view.MakeRoot();
				view.PerformLayout();
				view.Editable = true;
				view.RootBox.Activate(VwSelectionState.vssEnabled);
				view.RootBox.MakeSimpleSel(true, true, true, true);
				view.SetInsertionPoint(0, 0, 1, 0, true);
				IScrTxtPara para1 = (IScrTxtPara)m_philemonRev.SectionsOS[0].ContentOA.ParagraphsOS[0];
				int charCountPara1 = para1.Contents.Length;
				IScrTxtPara para2 = (IScrTxtPara)m_philemonRev.SectionsOS[0].ContentOA.ParagraphsOS[1];
				int charCountPara2 = para2.Contents.Length;

				// Press a backspace. In edit mode, this should concatenate the first two paras.
				view.ExposeOnKeyPress(new KeyPressEventArgs('\b'));
				Assert.AreEqual(charCountPara1 + charCountPara2, para1.Contents.Length);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the initial state of the buttons on the <see cref="DiffDialog"/>.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InitialButtonState()
		{
			CompleteInitialize(true, m_philemonRev);

			// Need to create the dialog completely for this test
			m_dlg.Show();
			m_dlg.Hide();

			// This is a imported version, so the labels should be on the "Revision" side of the
			// dialog.
			Label lblCurrentDiffType = (Label)ReflectionHelper.GetField(m_dlg, "lblCurrentDiffType");
			Label lblRevDiffType = (Label)ReflectionHelper.GetField(m_dlg, "lblRevDiffType");

			Assert.IsTrue(string.IsNullOrEmpty(lblCurrentDiffType.Text));
			Assert.AreEqual("Text Changed", lblRevDiffType.Text);

			CheckButtonStates(true, true, false);
		}
		//TODO: A test to navigate through several differences, and verify the button states.
		#endregion

		#region Undo tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that all undo tasks created while the dialog was opened, are collapsed to one.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CollapseUndoTasksToSingleTask()
		{
			CompleteInitialize(true, m_philemonRev);

			// Need to create the dialog completely for this test
			m_dlg.Show();
			m_dlg.Hide();

			// Before starting, the number of undo tasks should be 1 due to archiving a
			// book in the setup code.
			int cSeq = Cache.ActionHandlerAccessor.UndoableSequenceCount;
			Assert.AreEqual(3, cSeq);

			// Mark each difference as reviewed and check the task count along the way.
			int origDiffCount = m_bookMerger.Differences.Count;
			Assert.IsTrue(origDiffCount > 2, "Need more diffs for a useful test");
			for(int i = 1; i <= origDiffCount; i++)
			{
				m_dlg.SimulateKeepCurrentClick(true);
				cSeq = Cache.ActionHandlerAccessor.UndoableSequenceCount;
				Assert.AreEqual(i + 3, cSeq);
			}
			cSeq = Cache.ActionHandlerAccessor.UndoableSequenceCount;
			Assert.AreEqual(origDiffCount + 3, cSeq);

			// Now close the dialog and verify that there are two undo tasks: the initial one
			// from archiving a book and all the reviews collapsed to a single task.
			m_dlg.Close();
			cSeq = Cache.ActionHandlerAccessor.UndoableSequenceCount;
			Assert.AreEqual(4, cSeq);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test undoing a Reviewed action.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UndoRedoOfReviewed()
		{
			CompleteInitialize(true, m_philemonRev);

			// Need to create the dialog completely for this test
			m_dlg.Show();
			m_dlg.Hide();

			int origDiffCount = 6;

			// Advance to the second diff, and remember it
			m_dlg.SimulateNextButtonClick();
			Difference oldDiff = m_bookMerger.Differences.CurrentDifference;
			int oldDiffIndex = m_bookMerger.Differences.CurrentDifferenceIndex;
			Assert.AreEqual(1, oldDiffIndex, "Bad Diff Index at second diff");

			// Mark the difference as reviewed and check the difference count.
			m_dlg.SimulateKeepCurrentClick(true);
			Assert.AreEqual(origDiffCount - 1, m_bookMerger.Differences.Count,
				"Bad Diff. Count After Reviewed");

			// Make sure the current difference is the second one in the list
			Assert.AreEqual(1, m_bookMerger.Differences.CurrentDifferenceIndex);

			// To complicate the undo, first go to prev. diff.
			m_dlg.SimulatePrevButtonClick();
			Assert.AreEqual(0, m_bookMerger.Differences.CurrentDifferenceIndex);

			// Undo the reviewed action and check the difference count.
			m_dlg.PerformUndo();
			Assert.AreEqual(origDiffCount, m_bookMerger.Differences.Count,
				"Bad Diff. Count After Undo");
			// Make sure the current difference is the second one in the list.
			Assert.AreEqual(1, m_bookMerger.Differences.CurrentDifferenceIndex);

			// Make sure the current difference is the same one we had before undoing.
			Difference newDiff = m_bookMerger.Differences.CurrentDifference;
			Assert.AreEqual(oldDiff.RefStart, newDiff.RefStart, "Bad Start Refs.");
			Assert.AreEqual(oldDiff.DiffType, newDiff.DiffType, "Bad Diff. Types.");
			int newDiffIndex = m_bookMerger.Differences.CurrentDifferenceIndex;
			Assert.AreEqual(oldDiffIndex, newDiffIndex, "Bad Diff Index after Undo");

			// Go back to the first difference in the list.
			m_dlg.SimulatePrevButtonClick();

			// Redo the review we just undid.
			m_dlg.PerformRedo();
			Assert.AreEqual(origDiffCount - 1, m_bookMerger.Differences.Count,
				"Bad Diff. Count After Redo");

			// Make sure the current difference is the second one in the list.
			Assert.AreEqual(1, m_bookMerger.Differences.CurrentDifferenceIndex,
				"Bad Diff. Index After Redo");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test undoing/redoing a Restore action (i.e. Copy Revision to Current),
		/// and of multiple actions (TE-7842) with this data:
		/// 	Revision			Current
		/// 	Section 1			Section 1
		/// 	  verse 1			  verse 1
		/// 	Section 2			Section 2
		/// 	  verse 2			  verse 2
		/// 	Section 3			Section 3
		/// 	  (empty)			  (empty)
		/// 	Section 4			Section 5
		/// 	  verse 3			  (empty)
		/// 	Section 5
		/// 	  (empty)
		/// </summary>
		/// <remarks>The setup for this scenario requires that content of a paragraph be restored
		/// to an empty paragraph and then restoring the following section head.</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UndoRedoOfSectionHeadRestore()
		{
			IScrBook genesis;
			IScrBook genesisRev;
			using (UndoableUnitOfWorkHelper undoHelper = new UndoableUnitOfWorkHelper(m_actionHandler, "initialization"))
			{
				// Create a new book (Genesis)
				genesis = AddBookToMockedScripture(1, "Genesis");
				AddTitleToMockedBook(genesis, "Genesis");

				// Build the "revision" section: chapter 1 contains five sections with sections 3 and 5 empty
				IScrSection section1Curr = CreateSection(genesis, "My Section 1");
				IScrTxtPara para1Curr = AddParaToMockedSectionContent(section1Curr, ScrStyleNames.NormalParagraph);
				AddVerse(para1Curr, 1, 0, "Section 1 Text");
				IScrSection section2Curr = CreateSection(genesis, "My Section 2");
				IScrTxtPara para2Curr = AddParaToMockedSectionContent(section2Curr, ScrStyleNames.NormalParagraph);
				AddVerse(para2Curr, 0, 2, "Section 2 Text");
				IScrSection section3Curr = CreateSection(genesis, "My Section 3");
				StTxtParaBldr paraBldr = new StTxtParaBldr(Cache);
					paraBldr.ParaStyleName = ScrStyleNames.NormalParagraph;
					paraBldr.CreateParagraph(section3Curr.ContentOA);
					IScrSection section4Curr = CreateSection(genesis, "My Section 4");
					IScrTxtPara para4Curr = AddParaToMockedSectionContent(section4Curr, ScrStyleNames.NormalParagraph);
					AddVerse(para4Curr, 0, 3, "Section 4 Text");
					IScrSection section5Curr = CreateSection(genesis, "My Section 5");
					paraBldr.ParaStyleName = ScrStyleNames.NormalParagraph;
					paraBldr.CreateParagraph(section5Curr.ContentOA);

					IScrDraft draft = Cache.ServiceLocator.GetInstance<IScrDraftFactory>().Create("GenesisArchive", new IScrBook[] { genesis });
					draft.Type = ScrDraftType.ImportedVersion;
					genesisRev = draft.FindBook(1);

					// Remove section 4 in the current
					genesis.SectionsOS.RemoveAt(3);
					undoHelper.RollBack = false;
				}

			// We don't want to complete the initialization until we are finished with the IScrDraft.
			CompleteInitialize(false, genesisRev);

			// Detect differences
			m_bookMerger.DetectDifferences(null);
			CheckSectionHeadRestoreDiffs(genesis, genesisRev);

			// Need to create the dialog completely for this test
			m_dlg.Show();
			m_dlg.Hide();

			int origDiffCount = 3;

			// Restore text difference in section head
			m_dlg.SimulateRevertToOldButtonClick(true);
			Assert.AreEqual(origDiffCount - 1, m_bookMerger.Differences.Count,
				"Bad Diff. Count After Restore 1");
			Assert.AreEqual(0, m_bookMerger.Differences.CurrentDifferenceIndex);

			// Restore missing para in Current
			m_dlg.SimulateRevertToOldButtonClick(true);
			Assert.AreEqual(origDiffCount - 2, m_bookMerger.Differences.Count,
				"Bad Diff. Count After Restore 2");

			// Check the section head added diff to make sure that the destination IP is at the
			// correct location now (adjusted for the added para contents).
			Difference diff = m_bookMerger.Differences.CurrentDifference;
			IScrTxtPara para4Rev = (IScrTxtPara)genesisRev.SectionsOS[3].ContentOA.ParagraphsOS[0];
			Assert.AreEqual(para4Rev.Contents.Length, diff.IchMinCurr);
			Assert.AreEqual(para4Rev.Contents.Length, diff.IchLimCurr);

			// Restore missing para in Current
			m_dlg.SimulateRevertToOldButtonClick(true);
			Assert.AreEqual(origDiffCount - 3, m_bookMerger.Differences.Count,
				"Bad Diff. Count After Restore 3");

			// Complete checks here for each undo.
			m_dlg.PerformUndo();
			Assert.AreEqual(1, m_bookMerger.Differences.Count);
			// The last section should be removed.`
			Assert.AreEqual(4, genesis.SectionsOS.Count);

			m_dlg.PerformUndo();
			Assert.AreEqual(2, m_bookMerger.Differences.Count);
			// The content of the fourth section should be deleted.
			IScrTxtPara content4Curr = (IScrTxtPara)genesis.SectionsOS[3].ContentOA.ParagraphsOS[0];
			Assert.IsTrue(string.IsNullOrEmpty(content4Curr.Contents.Text));

			m_dlg.PerformUndo();
			Assert.AreEqual(3, m_bookMerger.Differences.Count);
			// The fourth section should be renamed back to "Section 5"
			IScrTxtPara heading4Curr = (IScrTxtPara)genesis.SectionsOS[3].HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("My Section 5", heading4Curr.Contents.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks the differences created in UndoRedoOfSectionHeadRestore.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CheckSectionHeadRestoreDiffs(IScrBook genesis, IScrBook genesisRev)
		{
			Assert.AreEqual(3, m_bookMerger.Differences.Count);

			Difference diff1 = m_bookMerger.Differences.MoveFirst();
			IScrTxtPara headingPara4Curr = (IScrTxtPara)genesis.SectionsOS[3].HeadingOA.ParagraphsOS[0];
			IScrTxtPara headingPara4Rev = (IScrTxtPara)genesisRev.SectionsOS[3].HeadingOA.ParagraphsOS[0];
			DiffTestHelper.VerifyParaDiff(diff1, 01001003, DifferenceType.TextDifference,
				headingPara4Curr, headingPara4Curr.Contents.Length - 1, headingPara4Curr.Contents.Length,
				headingPara4Rev, headingPara4Rev.Contents.Length - 1, headingPara4Rev.Contents.Length);

			Difference diff2 = m_bookMerger.Differences.MoveNext();
			IScrTxtPara contentPara4Curr = (IScrTxtPara)genesis.SectionsOS[3].ContentOA.ParagraphsOS[0];
			IScrTxtPara contentPara4Rev = (IScrTxtPara)genesisRev.SectionsOS[3].ContentOA.ParagraphsOS[0];
			DiffTestHelper.VerifyParaDiff(diff2, 01001003, DifferenceType.TextDifference,
				contentPara4Curr, 0, 0, contentPara4Rev, 0, contentPara4Rev.Contents.Length);

			Difference diff3 = m_bookMerger.Differences.MoveNext();
			IScrSection section5Rev = genesisRev.SectionsOS[4];
			DiffTestHelper.VerifySectionDiff(diff3, 01001003, 01001003, DifferenceType.SectionHeadMissingInCurrent,
				new IScrSection[] { section5Rev }, contentPara4Curr, contentPara4Curr.Contents.Length);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test undoing/redoing a Restore action (i.e. Copy Revision to Current),
		/// and of multiple actions.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UndoRedoOfRestore()
		{
			CompleteInitialize(true, m_philemonRev);

			// Need to create the dialog completely for this test
			m_dlg.Show();
			m_dlg.Hide();
			int origDiffCount = 6;

			// Restore original 1
			m_dlg.SimulateRevertToOldButtonClick(true);
			Assert.AreEqual(origDiffCount - 1, m_bookMerger.Differences.Count,
				"Bad Diff. Count After Restore 1");
			Assert.AreEqual(0, m_bookMerger.Differences.CurrentDifferenceIndex);

			m_bookMerger.Differences.MoveNext();
			// We're now at the third diff; remember it
			IScrTxtPara para = m_bookMerger.Differences.CurrentDifference.ParaCurr;
			string sThirdDiffCurr = para.Contents.Text;
			// Save next two differences - it will be changed by restore and needs to be
			// updated by the undo.
			m_bookMerger.Differences.MoveNext();
			Difference origFollowingDiff = m_bookMerger.Differences.CurrentDifference.Clone();
			Assert.AreEqual(21, origFollowingDiff.RefStart.Verse);  // verify next verse
			m_bookMerger.Differences.MovePrev();

			// Restore original 3
			m_dlg.SimulateRevertToOldButtonClick(true);

			// Verify that Restore made a change in the third difference.
			string sThirdDiffRestored = para.Contents.Text;
			Assert.IsTrue(sThirdDiffRestored != sThirdDiffCurr);

			// We're now at the fourth diff; remember it
			para = m_bookMerger.Differences.CurrentDifference.ParaCurr;
			string sFourthDiffCurr = para.Contents.Text;

			// Restore original 4
			m_dlg.SimulateRevertToOldButtonClick(true);
			Assert.AreEqual(origDiffCount - 3, m_bookMerger.Differences.Count,
				"Bad Diff. Count After Restore 4");
			Assert.AreEqual(1, m_bookMerger.Differences.CurrentDifferenceIndex);

			// Verify that Restore made a change in the third difference.
			string sFourthDiffRestored = para.Contents.Text;
			Assert.IsTrue(sFourthDiffRestored != sFourthDiffCurr);

			// Undo the 4th restore action and check paragraph contents.
			m_dlg.PerformUndo();
			para = m_bookMerger.Differences.CurrentDifference.ParaCurr;
			Assert.AreEqual(sFourthDiffCurr, para.Contents.Text);
			Assert.AreEqual(origDiffCount - 2, m_bookMerger.Differences.Count,
				"Bad Diff. Count After Undo 4");
			Assert.AreEqual(1, m_bookMerger.Differences.CurrentDifferenceIndex);

			// Undo the 2nd restore action and check paragraph contents.
			m_dlg.PerformUndo();
			para = m_bookMerger.Differences.CurrentDifference.ParaCurr;
			Assert.AreEqual(sThirdDiffCurr, para.Contents.Text);
			Assert.AreEqual(origDiffCount - 1, m_bookMerger.Differences.Count,
				"Bad Diff. Count After Undo 2");
			Assert.AreEqual(1, m_bookMerger.Differences.CurrentDifferenceIndex);
			m_bookMerger.Differences.MoveNext();
			Difference followingDiff = m_bookMerger.Differences.CurrentDifference.Clone();
			m_bookMerger.Differences.MovePrev();
			Assert.AreEqual(origFollowingDiff, followingDiff, "Undo did not restore difference");

			// Redo the 2nd restore action and verify that the redo restored the text
			m_dlg.PerformRedo();
			Assert.AreEqual(sThirdDiffRestored, para.Contents.Text);

			// Redo the 3rd restore action and verify that the redo restored the text
			para = m_bookMerger.Differences.CurrentDifference.ParaCurr;
			m_dlg.PerformRedo();
			Assert.AreEqual(sFourthDiffRestored, para.Contents.Text);
			Assert.AreEqual(origDiffCount - 3, m_bookMerger.Differences.Count,
				"Bad Diff. Count After Redo 3");
			Assert.AreEqual(1, m_bookMerger.Differences.CurrentDifferenceIndex);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test undoing/redoing a Restore action for the condition that caused TE-6336.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UndoRedoOfRestore_TE6336()
		{
			CompleteInitialize(true, m_philemonRev);

			// Need some special data for this test: we delete some characters of the first
			// paragraph of the first section and assign a different style
			IScrSection section = m_philemonCurr.SectionsOS[0];
			IScrTxtPara paraHeading = (IScrTxtPara)section.HeadingOA.ParagraphsOS[0];
			ITsStrBldr tssBldr = paraHeading.Contents.GetBldr();
			tssBldr.Replace(0, paraHeading.Contents.Length - 1, string.Empty, null);

			using (UndoableUnitOfWorkHelper undoHelper = new UndoableUnitOfWorkHelper(m_actionHandler, "initialization"))
			{
				paraHeading.StyleName = ScrStyleNames.SecondaryBookTitle;
				paraHeading.Contents = tssBldr.GetString();
				undoHelper.RollBack = false;
			}
			m_bookMerger.Differences.Clear();
			m_bookMerger.DetectDifferences(null);

			// Need to create the dialog completely for this test
			m_dlg.Show();
			m_dlg.Hide();

			int origDiffCount = 7;

			// Restore original 1
			IScrTxtPara para = m_bookMerger.Differences.CurrentDifference.ParaCurr;
			string sFirstDiffCurrentStyle = para.StyleName;
			m_dlg.SimulateRevertToOldButtonClick(true);
			Assert.AreEqual(origDiffCount - 1, m_bookMerger.Differences.Count,
				"Bad Diff. Count After Restore 1");
			Assert.AreEqual(0, m_bookMerger.Differences.CurrentDifferenceIndex);

			// Verify that Restore made a change in the first difference.
			string sFirstDiffRestoredStyle = para.StyleName;
			Assert.AreNotEqual(sFirstDiffRestoredStyle, sFirstDiffCurrentStyle);

			// Undo the 1st restore action and check paragraph contents.
			m_dlg.PerformUndo();
			para = m_bookMerger.Differences.CurrentDifference.ParaCurr;
			Assert.AreEqual(sFirstDiffCurrentStyle, para.StyleName);
			Assert.AreEqual(origDiffCount, m_bookMerger.Differences.Count,
				"Bad Diff. Count After Undo");
			Assert.AreEqual(0, m_bookMerger.Differences.CurrentDifferenceIndex);
			// Check the next difference.
			m_bookMerger.Differences.MoveNext();
			Assert.AreEqual(0, m_bookMerger.Differences.CurrentDifference.IchMinCurr);
			Assert.AreEqual(0, m_bookMerger.Differences.CurrentDifference.IchMinRev);
		}
		#endregion

		#region TE-5448 editing tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that when footnote markers are pasted in the Diff Dialog (in edit mode) a new
		/// footnote is created.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Edit_PastingFootnoteMarkers()
		{
			CompleteInitialize(true, m_philemonRev);

			var para = (IScrTxtPara)m_philemonCurr.SectionsOS[1].ContentOA.ParagraphsOS[0];
			IStFootnote footnote;
			using (var undoHelper = new UndoableUnitOfWorkHelper(m_actionHandler, "initialization"))
			{
				footnote = AddFootnote(m_philemonCurr, para, 0);
				undoHelper.RollBack = false;
			}
			var hvoOrigFootnote = footnote.Hvo;

			using (var view = new DummyDiffView(Cache, m_philemonCurr,
				m_bookMerger.Differences, false))
			{
				view.Editable = true;
				view.StyleSheet = m_styleSheet;
				view.MakeRoot();
				view.PerformLayout();
				view.RootBox.Activate(VwSelectionState.vssEnabled);
				// Copy the footnote at the start of the first Scripture section and paste it at the para start.
				view.SetInsertionPoint(0, 1, 0, 0, true);
				view.ExposeOnKeyDown(new KeyEventArgs(Keys.Shift | Keys.Right));
				view.EditingHelper.CopySelection();
				view.ExposeOnKeyDown(new KeyEventArgs(Keys.Left));
				using (var undoHelper = new UndoableUnitOfWorkHelper(m_actionHandler, "initialization"))
				{
					view.EditingHelper.PasteClipboard();
					undoHelper.RollBack = false;
				}

				var helper = SelectionHelper.Create(view);
				Assert.AreEqual(1, helper.IchAnchor, "IP should be after the footnote marker (and the little space)");
				Assert.AreEqual(2, m_philemonCurr.FootnotesOS.Count, "Should create a new footnote when pasting footnote marker");
				var guid = TsStringUtils.GetGuidFromRun(para.Contents, 0);
				footnote = m_philemonCurr.FootnotesOS[0];
				Assert.AreEqual(footnote.Guid, guid);
				Assert.AreNotEqual(hvoOrigFootnote, footnote.Hvo);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that when IP is next to a verse number, typed text will not be in "Verse
		/// Number" style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Edit_TypingNextToVerseNumber()
		{
			CompleteInitialize(true, m_philemonRev);

			IScrTxtPara para = (IScrTxtPara)m_philemonCurr.SectionsOS[1].ContentOA.ParagraphsOS[0];

			using (DummyDiffView view = new DummyDiffView(Cache, m_philemonCurr,
				m_bookMerger.Differences, false))
			{
				view.Editable = true;
				view.StyleSheet = m_styleSheet;
				view.MakeRoot();
				view.PerformLayout();
				view.RootBox.Activate(VwSelectionState.vssEnabled);
				view.SetInsertionPoint(0, 1, 0, 0, true);
				view.ExposeOnKeyPress(new KeyPressEventArgs('a'));
				view.ExposeOnKeyDown(new KeyEventArgs(Keys.Right));
				view.ExposeOnKeyPress(new KeyPressEventArgs('b'));

				ITsString tss = para.Contents;
				AssertEx.RunIsCorrect(tss, 0, "a", null, Cache.DefaultVernWs);
				AssertEx.RunIsCorrect(tss, 1, "1", ScrStyleNames.VerseNumber, Cache.DefaultVernWs);
				AssertEx.RunIsCorrect(tss, 2, "band the earth was without form and void and darkness covered the face of the deep", null, Cache.DefaultVernWs);
			}
		}

#if WANTTESTPORT // Need to handle deleting of footnotes when the ORCs get deleted, see FWR-217
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that when footnote markers are deleted in the Diff Dialog (in edit mode) the
		/// corresponding footnote is also deleted.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Edit_DeletingFootnoteMarkers()
		{
			CompleteInitialize(true, m_philemonRev);

			IScrTxtPara para = (IScrTxtPara)m_philemonCurr.SectionsOS[1].ContentOA.ParagraphsOS[0];
			IStFootnote footnote;
			using (UndoableUnitOfWorkHelper undoHelper = new UndoableUnitOfWorkHelper(m_actionHandler, "initialization"))
			{
				footnote = AddFootnote(m_philemonCurr, para, 0);
				undoHelper.RollBack = false;
			}
			int footnoteCount = m_philemonCurr.FootnotesOS.Count;

			using (DummyDiffView view = new DummyDiffView(Cache, m_philemonCurr,
				m_bookMerger.Differences, false))
			{
				view.Editable = true;
				view.StyleSheet = m_styleSheet;
				view.MakeRoot();
				view.PerformLayout();
				view.RootBox.Activate(VwSelectionState.vssEnabled);
				// Copy the footnote at the start of the first Scripture section and paste it at the para start.
				view.SetInsertionPoint(0, 1, 0, 0, true);
				view.ExposeOnKeyDown(new KeyEventArgs(Keys.Shift | Keys.Right));
				using (UndoableUnitOfWorkHelper undoHelper = new UndoableUnitOfWorkHelper(m_actionHandler, ":("))
				{
					view.ExposeOnKeyDown(new KeyEventArgs(Keys.Delete));
					undoHelper.RollBack = false;
				}
				SelectionHelper helper = SelectionHelper.Create(view);
				Assert.AreEqual(0, helper.IchAnchor, "IP should be at the start of the paragraph");
				Assert.IsFalse(helper.IsRange, "Should have a simple IP");
				Assert.AreEqual(footnoteCount - 1, m_philemonCurr.FootnotesOS.Count);
				Assert.IsFalse(footnote.IsValidObject);
				AssertEx.RunIsCorrect(para.Contents, 0, "1",
					ScrStyleNames.VerseNumber, Cache.DefaultVernWs);
			}
		}
#endif

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that pressing enter at end of section head doesn't insert body para into
		/// section head.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Edit_EnterAtEndOfSectionHeadDoesntInsertBodyParaIntoSectionHead()
		{
			CompleteInitialize(true, m_philemonRev);

			IStStyle sectionHeadStyle = m_scr.FindStyle(ScrStyleNames.SectionHead);
			IFdoOwningSequence<IStPara> headingParas = m_philemonCurr.SectionsOS[1].HeadingOA.ParagraphsOS;
			int sectionHeadParaCount = headingParas.Count;
			int sectionContentParaCount = m_philemonCurr.SectionsOS[1].ContentOA.ParagraphsOS.Count;

			using (UndoableUnitOfWorkHelper undoHelper = new UndoableUnitOfWorkHelper(m_actionHandler, "initialization"))
			{
				sectionHeadStyle.NextRA = m_scr.FindStyle(ScrStyleNames.NormalParagraph);
				undoHelper.RollBack = false;
			}

			using (DummyDiffView view = new DummyDiffView(Cache, m_philemonCurr,
				m_bookMerger.Differences, false))
			{
				view.Editable = true;
				view.StyleSheet = m_styleSheet;
				view.MakeRoot();
				view.PerformLayout();
				view.RootBox.Activate(VwSelectionState.vssEnabled);
				view.SetInsertionPoint(0, 1, 0, 0, true);
				view.ExposeOnKeyDown(new KeyEventArgs(Keys.Left));
				SelectionHelper helper = SelectionHelper.Create(view);
				Assert.AreEqual(((IScrTxtPara)headingParas[0]).Contents.Length,
					helper.IchAnchor, "IP should be at the end of the last section head paragraph");
				view.ExposeOnKeyDown(new KeyEventArgs(Keys.Enter));
				view.ExposeOnKeyPress(new KeyPressEventArgs('\r'));

				Assert.AreEqual(sectionHeadParaCount,
					m_philemonCurr.SectionsOS[1].HeadingOA.ParagraphsOS.Count,
					"the number of heading paragraphs should stay the same");
				Assert.AreEqual(sectionContentParaCount + 1,
					m_philemonCurr.SectionsOS[1].ContentOA.ParagraphsOS.Count,
					"the number of content paragraphs should increase by one");
				Assert.AreEqual(0,
					((IScrTxtPara)m_philemonCurr.SectionsOS[1].ContentOA.ParagraphsOS[0]).Contents.Length,
					"the inserted paragraph should be empty");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that pressing enter at end of section head paragraph which is not the last para
		/// in the section head causes the section to break in two.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Edit_EnterAtEndOfNonUltimateSectionHeadPara()
		{
			CompleteInitialize(true, m_philemonRev);

			IStStyle sectionHeadStyle = m_scr.FindStyle(ScrStyleNames.SectionHead);
			int origNumberOfSections = m_philemonCurr.SectionsOS.Count;
			IStText heading = m_philemonCurr.SectionsOS[1].HeadingOA;
			IFdoOwningSequence<IStPara> headingParas = heading.ParagraphsOS;
			int origNumberOfSectionHeadParas = headingParas.Count;
			int sectionContentParaCount = m_philemonCurr.SectionsOS[1].ContentOA.ParagraphsOS.Count;
			using (UndoableUnitOfWorkHelper undoHelper = new UndoableUnitOfWorkHelper(m_actionHandler, "initialization"))
			{
				sectionHeadStyle.NextRA = m_scr.FindStyle(ScrStyleNames.NormalParagraph);
				IScrTxtPara headPara2 = (IScrTxtPara)AddParaToMockedText(heading, ScrStyleNames.SectionHead);
				AddRunToMockedPara(headPara2, "Head After Break", Cache.DefaultVernWs);
				undoHelper.RollBack = false;
			}

			using (DummyDiffView view = new DummyDiffView(Cache, m_philemonCurr,
				m_bookMerger.Differences, false))
			{
				view.Editable = true;
				view.StyleSheet = m_styleSheet;
				view.MakeRoot();
				view.PerformLayout();
				view.RootBox.Activate(VwSelectionState.vssEnabled);
				view.SetInsertionPoint(0, 1, 0, 0, true);
				view.ExposeOnKeyDown(new KeyEventArgs(Keys.Up)); // Move to start of last section head para
				view.ExposeOnKeyDown(new KeyEventArgs(Keys.Left)); // Move to end of preceding section head para
				SelectionHelper helper = SelectionHelper.Create(view);
				Assert.AreEqual(((IScrTxtPara)headingParas[origNumberOfSectionHeadParas - 1]).Contents.Length,
					helper.IchAnchor, "IP should be at the end of the penultimate section head paragraph");
				view.ExposeOnKeyDown(new KeyEventArgs(Keys.Enter));
				view.ExposeOnKeyPress(new KeyPressEventArgs('\r'));

				Assert.AreEqual(origNumberOfSections + 1, m_philemonCurr.SectionsOS.Count,
					"New section should have been added.");
				IScrSection section2 = m_philemonCurr.SectionsOS[1];
				IScrSection section3 = m_philemonCurr.SectionsOS[2];
				Assert.AreEqual(origNumberOfSectionHeadParas,
					section2.HeadingOA.ParagraphsOS.Count,
					"The number of heading paragraphs in second section should go back to what it was before we added an extra one");
				Assert.AreEqual(1,
					section2.ContentOA.ParagraphsOS.Count,
					"There should only be 1 content paragraph in the second section.");
				Assert.AreEqual(0,
					((IScrTxtPara)section2.ContentOA.ParagraphsOS[0]).Contents.Length,
					"Content paragraph in the second section should be empty.");
				Assert.AreEqual(sectionContentParaCount,
					section3.ContentOA.ParagraphsOS.Count,
					"All content paragraphs from original section should now be in the new section.");
				Assert.AreEqual(1, section3.HeadingOA.ParagraphsOS.Count,
					"New section should have 1 heading paragraph.");
				Assert.AreEqual("Head After Break",
					((IScrTxtPara)section3.HeadingOA.ParagraphsOS[0]).Contents.Text,
					"New section heading paragraph should be the last paragraph of the original section heading.");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that when a range selection includes a verse number, character formatting is
		/// removed from surrounding text but verse number style is preserved.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Edit_ControlSpacePreservesVerseNumber()
		{
			CompleteInitialize(true, m_philemonRev);

			IFdoOwningSequence<IStPara> paras = m_philemonCurr.SectionsOS[2].ContentOA.ParagraphsOS;
			IScrTxtPara para = (IScrTxtPara)paras[paras.Count - 1];
			int ichAnchor = para.Contents.Length;
			int iRun = para.Contents.RunCount - 1;
			using (UndoableUnitOfWorkHelper undoHelper = new UndoableUnitOfWorkHelper(m_actionHandler, "initialization"))
			{
				AddRunToMockedPara(para, " More text in last verse.", "Emphasis");
				AddRunToMockedPara(para, "24", ScrStyleNames.VerseNumber);
				AddRunToMockedPara(para, "The really very last verse.", "Strong");
				undoHelper.RollBack = false;
			}
			int ichEnd = para.Contents.Length;

			using (DummyDiffView view = new DummyDiffView(Cache, m_philemonCurr,
				m_bookMerger.Differences, false))
			{
				// select all three added runs
				view.Editable = true;
				view.StyleSheet = m_styleSheet;
				view.MakeRoot();
				view.PerformLayout();
				view.RootBox.Activate(VwSelectionState.vssEnabled);
				view.SetInsertionPoint(0, 2, paras.Count - 1, ichAnchor, true);
				SelectionHelper helper = SelectionHelper.Create(view);
				helper.IchEnd = ichEnd;
				helper.MakeBest(true);

				// press Ctrl+Space to clear character styles (except verse number)
				view.ExposeOnKeyDown(new KeyEventArgs(Keys.Control | Keys.Space));

				// verify that the character styles have been cleared (but not the verse number style)
				ITsString tss = para.Contents;
				AssertEx.RunIsCorrect(tss, iRun, "the existentialists are all wrong. " +
					"The last verse. " + "More text in last verse.",
					null, Cache.DefaultVernWs);
				AssertEx.RunIsCorrect(tss, iRun + 1, "24", ScrStyleNames.VerseNumber,
					Cache.DefaultVernWs);
				AssertEx.RunIsCorrect(tss, iRun + 2, "The really very last verse.",
					null, Cache.DefaultVernWs);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that when IP is next to a chapter number, non-FW text pasted from the
		/// clipboard will not be in "Chapter Number" style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Edit_PasteBeforeChapterNumber()
		{
			CompleteInitialize(true, m_philemonRev);

			IScrTxtPara para = (IScrTxtPara)m_philemonCurr.SectionsOS[1].ContentOA.ParagraphsOS[0];
			using (UndoableUnitOfWorkHelper undoHelper = new UndoableUnitOfWorkHelper(m_actionHandler, "initialization"))
			{
				ITsStrBldr bldr = para.Contents.GetBldr();
				bldr.Replace(0, 0, "1",
					StyleUtils.CharStyleTextProps(ScrStyleNames.ChapterNumber, Cache.DefaultVernWs));
				para.Contents = bldr.GetString();
				undoHelper.RollBack = false;
			}
			ClipboardUtils.SetText("We should paste Audio and Video");

			using (DummyDiffView view = new DummyDiffView(Cache, m_philemonCurr,
				m_bookMerger.Differences, false))
			{
				view.Editable = true;
				view.StyleSheet = m_styleSheet;
				view.MakeRoot();
				view.PerformLayout();
				view.RootBox.Activate(VwSelectionState.vssEnabled);
				view.SetInsertionPoint(0, 1, 0, 0, true);
				using (UndoableUnitOfWorkHelper undoHelper = new UndoableUnitOfWorkHelper(m_actionHandler, "paste"))
				{
					view.EditingHelper.PasteClipboard();
					undoHelper.RollBack = false;
				}
				ITsString tss = para.Contents;
				AssertEx.RunIsCorrect(tss, 0, "We should paste Audio and Video", null, Cache.DefaultVernWs);
				AssertEx.RunIsCorrect(tss, 1, "1", ScrStyleNames.ChapterNumber, Cache.DefaultVernWs);
				AssertEx.RunIsCorrect(tss, 2, "1", ScrStyleNames.VerseNumber, Cache.DefaultVernWs);
			}
		}
		#endregion

		#region Helper Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure buttons are enabled/disabled appropriately.
		/// </summary>
		/// <param name="fHaveCurrentDifference"></param>
		/// <param name="fNextShouldBeEnabled"></param>
		/// <param name="fPrevShouldBeEnabled"></param>
		/// ------------------------------------------------------------------------------------
		private void CheckButtonStates(bool fHaveCurrentDifference, bool fNextShouldBeEnabled,
			bool fPrevShouldBeEnabled)
		{
			Assert.AreEqual(fNextShouldBeEnabled, m_dlg.NextButtonEnabled);
			Assert.AreEqual(fPrevShouldBeEnabled, m_dlg.PreviousButtonEnabled);
			Assert.AreEqual(fHaveCurrentDifference, m_dlg.RestoreOriginalButtonEnabled);
			Assert.AreEqual(fHaveCurrentDifference, m_dlg.ReviewedButtonEnabled);
		}

		#endregion
	}
	#endregion

	#region DiffDialogTests_MockedCache class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// More tests for the <see cref="DiffDialog"/> class, using an in-memory cache.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class DiffDialogTests_More : ScrInMemoryFdoTestBase
	{
		private BookMerger m_bookMerger;
		private IScrBook m_philemonRev;
		private IScrBook m_philemonCurr;
		private MockedCacheDiffDialog m_dlg;
		private FwStyleSheet m_styleSheet;

		#region Setup/Teardown
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a book and a revision, and create the diff dialog.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public override void TestSetup()
		{
			base.TestSetup();

			// init the DummyBookMerger
			Debug.Assert(m_bookMerger == null, "m_bookMerger is not null.");
			//if (m_bookMerger != null)
			//	m_bookMerger.Dispose();
			m_bookMerger = new DummyBookMerger(Cache, null, m_philemonRev);

			m_styleSheet = new FwStyleSheet();
			m_styleSheet.Init(Cache, m_scr.Hvo, ScriptureTags.kflidStyles);

			Debug.Assert(m_dlg == null, "m_dlg is not null.");
			//if (m_dlg != null)
			//	m_dlg.Dispose();
			m_dlg = new MockedCacheDiffDialog(m_bookMerger, Cache, m_styleSheet, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overridden to only create a book with no content, heading, title, etc.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			m_philemonCurr = AddBookToMockedScripture(57, "Philemon");
			m_philemonRev = AddArchiveBookToMockedScripture(57, "Philemon");
			AddTitleToMockedBook(m_philemonCurr, "Philemon");
			AddTitleToMockedBook(m_philemonRev, "Philemon");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clean up after test.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public override void TestTearDown()
		{
			// clear out the difference list.
			while (m_bookMerger.Differences.MoveFirst() != null)
				m_bookMerger.Differences.Remove(m_bookMerger.Differences.CurrentDifference);
			m_bookMerger.Dispose();
			m_bookMerger = null;

			m_dlg.Close();
			m_dlg.Dispose();
			m_dlg = null;
			base.TestTearDown();
		}
		#endregion

		#region Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to restore original, when current diff revision has an implicit verse
		/// and the current has the explicit verse number.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RevertToOriginal_ImplicitVerseInRevision()
		{
			// Create the Current section with chapter 1 verse 1
			IScrSection sectionCur = CreateSection(m_philemonCurr, "My aching head!");
			StTxtParaBldr paraBldr = new StTxtParaBldr(Cache);
				paraBldr.ParaStyleName = ScrStyleNames.NormalParagraph;
				paraBldr.AppendRun("1", StyleUtils.CharStyleTextProps(ScrStyleNames.ChapterNumber, Cache.DefaultVernWs));
				paraBldr.AppendRun("1", StyleUtils.CharStyleTextProps(ScrStyleNames.VerseNumber, Cache.DefaultVernWs));
				int ichLimV1Cur = paraBldr.Length;
				paraBldr.AppendRun("This is text.", StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
				IScrTxtPara HvoCurr = (IScrTxtPara)paraBldr.CreateParagraph(sectionCur.ContentOA);

				// Create the Revision section with chapter 1
				IScrSection sectionRev = CreateSection(m_philemonRev, "My aching head!");
				paraBldr.ParaStyleName = ScrStyleNames.NormalParagraph;
				paraBldr.AppendRun("1", StyleUtils.CharStyleTextProps(ScrStyleNames.ChapterNumber, Cache.DefaultVernWs));
				int ichLimRev = paraBldr.Length;
				paraBldr.AppendRun("This is text.", StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
				IScrTxtPara hvoRev = (IScrTxtPara)paraBldr.CreateParagraph(sectionRev.ContentOA);

				m_bookMerger.DetectDifferences(null);

				// Make sure there are two differences.
				Assert.AreEqual(1, m_dlg.DifferenceList.Count);
				m_dlg.Show();

				Difference diff = m_dlg.DifferenceList.CurrentDifference;
				// Verify that differences are correct
				Assert.AreEqual(57001001, diff.RefStart);
				Assert.AreEqual(HvoCurr, diff.ParaCurr);
				Assert.AreEqual(hvoRev, diff.ParaRev);
				Assert.AreEqual(1, diff.IchMinCurr);
				Assert.AreEqual(ichLimV1Cur, diff.IchLimCurr);
				Assert.AreEqual(1, diff.IchMinRev);
				Assert.AreEqual(ichLimRev, diff.IchLimRev);

				// Restore original
				m_dlg.SimulateRevertToOldButtonClick(false);

				// verify the results
				Assert.AreEqual(0, m_bookMerger.Differences.Count, "Bad diff. count after restore");
				Assert.AreEqual(-1, m_bookMerger.Differences.CurrentDifferenceIndex);
				m_dlg.Hide();
				IScrTxtPara para = (IScrTxtPara)sectionCur.ContentOA.ParagraphsOS[0];
				Assert.AreEqual("1This is text.", para.Contents.Text);
			}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to restore original, when first diff is a Paragraph style change
		/// and subsequent diff(s) are verse text differences in the same paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RevertToOriginal_ParaStyleChangeFollowedByVerseTextDiffs()
		{
			// To test this well, we want the revision paragraph to be much longer than the
			// current paragraph. This will demonstrate the bug when
			IScrSection sectionCur = CreateSection(m_philemonCurr, "My aching head!");
			IScrSection sectionRev = CreateSection(m_philemonRev, "My aching head!");

			StTxtParaBldr paraBldr = new StTxtParaBldr(Cache);
				paraBldr.ParaStyleName = ScrStyleNames.NormalParagraph;
				paraBldr.AppendRun("1", StyleUtils.CharStyleTextProps(ScrStyleNames.ChapterNumber,
					Cache.DefaultVernWs));
				paraBldr.AppendRun("ABC",
					StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
				int ichLimParaCur = paraBldr.Length;
				IScrTxtPara hvoCurr = (IScrTxtPara)paraBldr.CreateParagraph(sectionCur.ContentOA);

				paraBldr.ParaStyleName = "Random Style";
				paraBldr.AppendRun("1", StyleUtils.CharStyleTextProps(ScrStyleNames.ChapterNumber,
					Cache.DefaultVernWs));
				paraBldr.AppendRun("In the beginning, God created the heavens and the earth. ",
					StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
				int ichLimParaRev = paraBldr.Length;
				IScrTxtPara HvoRev = (IScrTxtPara)paraBldr.CreateParagraph(sectionRev.ContentOA);

				m_bookMerger.DetectDifferences(null);

				// Make sure there are two differences.
				Assert.AreEqual(2, m_dlg.DifferenceList.Count);
				m_dlg.Show();

				Difference diff = m_dlg.DifferenceList.CurrentDifference;
				// Verify that differences are correct
				Assert.AreEqual(57001001, diff.RefStart);
				Assert.AreEqual(DifferenceType.ParagraphStyleDifference, diff.DiffType);
				Assert.AreEqual(hvoCurr, diff.ParaCurr);
				Assert.AreEqual(HvoRev, diff.ParaRev);
				Assert.AreEqual(0, diff.IchMinCurr);
				Assert.AreEqual(ichLimParaCur, diff.IchLimCurr);
				Assert.AreEqual(0, diff.IchMinRev);
				Assert.AreEqual(ichLimParaRev, diff.IchLimRev);

				// Restore original 1
				m_dlg.SimulateRevertToOldButtonClick(false);
				Assert.AreEqual(1, m_bookMerger.Differences.Count, "Bad diff. count after restore");
				Assert.AreEqual(0, m_bookMerger.Differences.CurrentDifferenceIndex);

				// We're now at the second diff
				diff = m_dlg.DifferenceList.CurrentDifference;
				Assert.AreEqual(57001001, diff.RefStart);
				Assert.AreEqual(DifferenceType.TextDifference, diff.DiffType);
				Assert.AreEqual(hvoCurr, diff.ParaCurr);
				Assert.AreEqual(HvoRev, diff.ParaRev);
				Assert.AreEqual(1, diff.IchMinCurr);
				Assert.AreEqual(ichLimParaCur, diff.IchLimCurr);
				Assert.AreEqual(1, diff.IchMinRev);
				Assert.AreEqual(ichLimParaRev, diff.IchLimRev);

				m_dlg.Hide();
			}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to restore original, when first diff is a verse added difference
		/// and subsequent diff is a Paragraph style change in the same paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RevertToOriginal_VerseAddedDiffAndParaStyleChange()
		{
			ITsTextProps verseNumProps = StyleUtils.CharStyleTextProps(ScrStyleNames.VerseNumber,
				Cache.DefaultVernWs);
//			ITsTextProps chapterNumProps = StyleUtils.CharStyleTextProps(ScrStyleNames.VerseNumber,
//				Cache.DefaultVernWs);

			// Create a section for the current
			// Verses 1 and 2, with a paragraph style of 'Random Style'
			IScrSection sectionCur = CreateSection(m_philemonCurr, "My aching head!");
			StTxtParaBldr paraBldr = new StTxtParaBldr(Cache);
				paraBldr.ParaStyleName = "Random Style";
				paraBldr.AppendRun("1", verseNumProps);
				paraBldr.AppendRun("ABC", StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
				int ichLimCurVerse1 = paraBldr.Length;
				paraBldr.AppendRun("2", verseNumProps);
				paraBldr.AppendRun("In the beginning, God created the heavens and the earth. ",
					StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
				int ichLimCurVerse2 = paraBldr.Length;
				IScrTxtPara hvoCurr = (IScrTxtPara)paraBldr.CreateParagraph(sectionCur.ContentOA);
				Assert.AreEqual(57001001, sectionCur.VerseRefStart);

				// Create a section for the revision as well
				// Verse 2 only, with a paragraph style of 'NormalParagraph'
				IScrSection sectionRev = CreateSection(m_philemonRev, "My aching head!");
				paraBldr.ParaStyleName = ScrStyleNames.NormalParagraph;
				paraBldr.AppendRun("2", verseNumProps);
				paraBldr.AppendRun("In the beginning, God created the heavens and the earth. ",
					StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
				int ichLimParaRev = paraBldr.Length;
				IScrTxtPara hvoRev = (IScrTxtPara)paraBldr.CreateParagraph(sectionRev.ContentOA);
				Assert.AreEqual(57001002, sectionRev.VerseRefStart);

				m_bookMerger.DetectDifferences(null);

				// Make sure there are two differences
				Assert.AreEqual(2, m_dlg.DifferenceList.Count);

				m_dlg.Show();

				// Verify the current difference (first one)
				Difference diff = m_dlg.DifferenceList.CurrentDifference;
				// TODO: The start ref here probably ought to be one, but it isn't (It's non critical)
				Assert.AreEqual(57001002, diff.RefStart);
				Assert.AreEqual(57001002, diff.RefEnd);
				Assert.AreEqual(DifferenceType.ParagraphStyleDifference, (DifferenceType)diff.DiffType);
				Assert.AreEqual(hvoCurr, diff.ParaCurr);
				Assert.AreEqual(hvoRev, diff.ParaRev);
				Assert.AreEqual(0, diff.IchMinCurr);
				Assert.AreEqual(ichLimCurVerse2, diff.IchLimCurr);
				Assert.AreEqual(0, diff.IchMinRev);
				Assert.AreEqual(ichLimParaRev, diff.IchLimRev);

				// Restore original 1: should apply para style NormalParagraph
				m_dlg.SimulateRevertToOldButtonClick(false);

				// Verify the Current paragraph
				ITsTextProps ttp = StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalParagraph);
				IScrTxtPara para = (IScrTxtPara) sectionCur.ContentOA[0];
				Assert.AreEqual(ttp, para.StyleRules);

				// Verify the current difference (second one)
				diff = m_dlg.DifferenceList.CurrentDifference;
				Assert.AreEqual(57001001, diff.RefStart);
				Assert.AreEqual(57001001, diff.RefEnd);
				Assert.AreEqual(DifferenceType.VerseAddedToCurrent, (DifferenceType)diff.DiffType);
				Assert.AreEqual(hvoCurr, diff.ParaCurr);
				Assert.AreEqual(hvoRev, diff.ParaRev);
				Assert.AreEqual(0, diff.IchMinCurr);
				Assert.AreEqual(ichLimCurVerse1, diff.IchLimCurr);
				Assert.AreEqual(0, diff.IchMinRev);
				Assert.AreEqual(0, diff.IchLimRev);

				// Restore original 2: should delete verse 1 in the current
				m_dlg.m_fPermitRevertToOld = true; // reverting causes data loss
				m_dlg.SimulateRevertToOldButtonClick(false);
				Assert.AreEqual(-1, m_bookMerger.Differences.CurrentDifferenceIndex);

				//Verify the Current paragraph
				para = (IScrTxtPara)sectionCur.ContentOA[0];
				Assert.AreEqual("2In the beginning, God created the heavens and the earth. ",
					para.Contents.Text);

				m_dlg.Hide();
			}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the case where a "section with footnotes" is missing in the current and
		/// "Revert to original" is clicked to restore (insert) the section in the current.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("WANTTESTPORT: Handle changing paragraph content when footnotes are deleted (FWR-217)")]
		public void RevertToOriginal_SectionMissingInCurrent_Footnotes()
		{
			// Make a section in Current: verses 1,3
			IScrSection sectionCurr = CreateSection(m_philemonCurr, "First Section Head");
			IScrTxtPara paraCurr = AddParaToMockedSectionContent(sectionCurr, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(paraCurr, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(paraCurr, "Verse 1.", Cache.DefaultVernWs);
			AddRunToMockedPara(paraCurr, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(paraCurr, "This is verse three.", Cache.DefaultVernWs);
			IScrFootnote footnote = AddFootnote(m_philemonCurr, paraCurr, 14);
			IScrTxtPara footnotePara = (IScrTxtPara)AddParaToMockedText(footnote, ScrStyleNames.NormalFootnoteParagraph);
			AddRunToMockedPara(footnotePara, "Here is footnote A", null);
			footnote = AddFootnote(m_philemonCurr, paraCurr, 23);
			footnotePara = (IScrTxtPara)AddParaToMockedText(footnote, ScrStyleNames.NormalFootnoteParagraph);
			AddRunToMockedPara(footnotePara, "Here is footnote B", null);

			// Make a second section in Current: verse 5
			IScrSection section2Curr = CreateSection(m_philemonCurr, "Third Section Head");
			paraCurr = AddParaToMockedSectionContent(section2Curr, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(paraCurr, "5", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(paraCurr, "Verse 5.", Cache.DefaultVernWs);
			footnote = AddFootnote(m_philemonCurr, paraCurr, 9);
			footnotePara = (IScrTxtPara)AddParaToMockedText(footnote, ScrStyleNames.NormalFootnoteParagraph);
			AddRunToMockedPara(footnotePara, "Here is footnote 4", null);

			// Make a section in Revision: verses 1,2
			IScrSection section1Rev = CreateSection(m_philemonRev, "First Section Head");
			IScrTxtPara paraRev = AddParaToMockedSectionContent(section1Rev, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(paraRev, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(paraRev, "Verse 1.", Cache.DefaultVernWs);
			AddRunToMockedPara(paraRev, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(paraRev, "Here is the long lost missing verse.", Cache.DefaultVernWs);
			footnote = AddFootnote(m_philemonRev, paraRev, 17);
			footnotePara = (IScrTxtPara)AddParaToMockedText(footnote, ScrStyleNames.NormalFootnoteParagraph);
			AddRunToMockedPara(footnotePara, "Here is footnote 1", null);

			// Make a second section in Revision: verse 4
			IScrSection section2Rev = CreateSection(m_philemonRev, "Second Section Head");
			paraRev = AddParaToMockedSectionContent(section2Rev, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(paraRev, "4", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(paraRev, "This is verse four.", Cache.DefaultVernWs);
			footnote = AddFootnote(m_philemonRev, paraRev, 4);
			footnotePara = (IScrTxtPara)AddParaToMockedText(footnote, ScrStyleNames.NormalFootnoteParagraph);
			AddRunToMockedPara(footnotePara, "Here is footnote 2", null);
			footnote = AddFootnote(m_philemonRev, paraRev, 13);
			footnotePara = (IScrTxtPara)AddParaToMockedText(footnote, ScrStyleNames.NormalFootnoteParagraph);
			AddRunToMockedPara(footnotePara, "Here is footnote 3", null);

			// Make a third section in Revision: verse 5
			IScrSection section3Rev = CreateSection(m_philemonRev, "Third Section Head");
			paraRev = AddParaToMockedSectionContent(section3Rev, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(paraRev, "5", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(paraRev, "Verse 5.", Cache.DefaultVernWs);
			footnote = AddFootnote(m_philemonRev, paraRev, 9);
			footnotePara = (IScrTxtPara)AddParaToMockedText(footnote, ScrStyleNames.NormalFootnoteParagraph);
			AddRunToMockedPara(footnotePara, "Here is footnote 4", null);

			// Detect differences
			m_bookMerger.DetectDifferences(null);

			// Check the diffs
			Assert.AreEqual(3, m_bookMerger.Differences.Count);
			Difference diff1 = m_bookMerger.Differences.MoveFirst();
			Assert.AreEqual(DifferenceType.VerseMissingInCurrent, (DifferenceType)diff1.DiffType);
			Assert.AreEqual(57001002, diff1.RefStart);

			Difference diff2 = m_bookMerger.Differences.MoveNext();
			Assert.AreEqual(DifferenceType.VerseAddedToCurrent, (DifferenceType)diff2.DiffType);
			Assert.AreEqual(57001003, diff2.RefStart);

			Difference diff3 = m_bookMerger.Differences.MoveNext();
			Assert.AreEqual(DifferenceType.SectionMissingInCurrent, (DifferenceType)diff3.DiffType);
			Assert.AreEqual(57001004, diff3.RefStart);

			// Check the section counts before doing the restore
			Assert.AreEqual(2, m_philemonCurr.SectionsOS.Count);
			Assert.AreEqual(3, m_philemonRev.SectionsOS.Count);

			// Show the dialog; perform Restore of all diffs
			m_dlg.Show();
			while (m_bookMerger.Differences.Count > 0)
			{
				// if the reverting diff would involve data loss, prevent it from displaying a confirmation
				// dialog (and stalling the test).
				m_dlg.m_fPermitRevertToOld = (m_bookMerger.Differences.CurrentDifference.DiffType & DifferenceType.VerseAddedToCurrent) != 0;

				m_dlg.SimulateRevertToOldButtonClick(false);
			}

			// Verify the new section counts
			Assert.AreEqual(3, m_philemonCurr.SectionsOS.Count);
			Assert.AreEqual(3, m_philemonRev.SectionsOS.Count);

			// Verify the resulting sections
			IScrSection section1 = m_philemonCurr.SectionsOS[0];
			IScrSection section2 = m_philemonCurr.SectionsOS[1];
			IScrSection section3 = m_philemonCurr.SectionsOS[2];
			// check the section heads
			Assert.AreEqual("First Section Head",
				((IScrTxtPara)section1.HeadingOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual("Second Section Head",
				((IScrTxtPara)section2.HeadingOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual("Third Section Head",
				((IScrTxtPara)section3.HeadingOA.ParagraphsOS[0]).Contents.Text);
			// also check the refs of the sections
			Assert.AreEqual(57001001, section1.VerseRefStart);
			Assert.AreEqual(57001002, section1.VerseRefEnd);
			Assert.AreEqual(57001004, section2.VerseRefStart);
			Assert.AreEqual(57001004, section2.VerseRefEnd);
			Assert.AreEqual(57001005, section3.VerseRefStart);
			Assert.AreEqual(57001005, section3.VerseRefEnd);
			// verify footnotes too
			IScrFootnote footnote1 = m_philemonCurr.FootnotesOS[0];
			IScrTxtPara para = (IScrTxtPara)(section1.ContentOA.ParagraphsOS[0]);
			VerifyFootnote(footnote1, para, 17);
			Assert.AreEqual("Here is footnote 1", ((IScrTxtPara)footnote1.ParagraphsOS[0]).Contents.Text);
			IScrFootnote footnote2 = m_philemonCurr.FootnotesOS[1];
			para = (IScrTxtPara)(section2.ContentOA.ParagraphsOS[0]);
			VerifyFootnote(footnote2, para, 4);
			Assert.AreEqual("Here is footnote 2", ((IScrTxtPara)footnote2.ParagraphsOS[0]).Contents.Text);
			IScrFootnote footnote3 = m_philemonCurr.FootnotesOS[2];
			para = (IScrTxtPara)(section2.ContentOA.ParagraphsOS[0]);
			VerifyFootnote(footnote3, para, 13);
			Assert.AreEqual("Here is footnote 3", ((IScrTxtPara)footnote3.ParagraphsOS[0]).Contents.Text);
			IScrFootnote footnote4 = m_philemonCurr.FootnotesOS[3];
			para = (IScrTxtPara)section3.ContentOA.ParagraphsOS[0];
			VerifyFootnote(footnote4, para, 9);
			Assert.AreEqual("Here is footnote 4", ((IScrTxtPara)footnote4.ParagraphsOS[0]).Contents.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the case where a section is missing in the current and its last paragraph (in
		/// the revision) is empty. This is a regression test for TE-4190.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DisplayDiffs_SectionMissingInCurrentWithEmptyPara()
		{
			// Make a section in Current: verse 5
			IScrSection sectionCurr = AddSectionToMockedBook(m_philemonCurr);
			AddSectionHeadParaToSection(sectionCurr, "Section Head", ScrStyleNames.SectionHead);
			IScrTxtPara paraCurr = AddParaToMockedSectionContent(sectionCurr, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(paraCurr, "5", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(paraCurr, "This is verse five.", Cache.DefaultVernWs);

			// make a section in Revision that does not match and will be counted as "missing in Current"
			IScrSection section1Rev = AddSectionToMockedBook(m_philemonRev);
			AddSectionHeadParaToSection(section1Rev, "Section Head Added", ScrStyleNames.SectionHead);
			IScrTxtPara paraRev = AddParaToMockedSectionContent(section1Rev, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(paraRev, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(paraRev, "Here is the long lost missing verse.", Cache.DefaultVernWs);
			// add an empty paragraph too
			AddParaToMockedSectionContent(section1Rev, ScrStyleNames.NormalParagraph);

			// create a matching section in Revision: verse 5
			IScrSection section2Rev = AddSectionToMockedBook(m_philemonRev);
			AddSectionHeadParaToSection(section2Rev, "Section Head", ScrStyleNames.SectionHead);
			IScrTxtPara para2Rev = AddParaToMockedSectionContent(section2Rev, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para2Rev, "5", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para2Rev, "This is verse five.", Cache.DefaultVernWs);

			// Detect differences
			m_bookMerger.DetectDifferences(null);

			// Check the diff and section counts
			Assert.AreEqual(1, m_bookMerger.Differences.Count);
			Assert.AreEqual(1, m_philemonCurr.SectionsOS.Count);
			Assert.AreEqual(2, m_philemonRev.SectionsOS.Count);

			// Show the dialog - Should not crash
			m_dlg.Show();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the case where a section is missing in the current and "Revert to original"
		/// is clicked to restore (insert) the section in the current.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RevertToOriginal_SectionMissingInCurrent()
		{
			// Make a section in Current: verse 5
			IScrSection sectionCurr = AddSectionToMockedBook(m_philemonCurr);
			AddSectionHeadParaToSection(sectionCurr, "Section Head", ScrStyleNames.SectionHead);
			IScrTxtPara paraCurr = AddParaToMockedSectionContent(sectionCurr, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(paraCurr, "5", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(paraCurr, "This is verse five.", Cache.DefaultVernWs);

			// make a section in Revision that does not match and will be counted as "missing in Current"
			IScrSection section1Rev = AddSectionToMockedBook(m_philemonRev);
			AddSectionHeadParaToSection(section1Rev, "Section Head Added", ScrStyleNames.SectionHead);
			IScrTxtPara paraRev = AddParaToMockedSectionContent(section1Rev, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(paraRev, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(paraRev, "Here is the long lost missing verse.", Cache.DefaultVernWs);

			// create a matching section in Revision: verse 5
			IScrSection section2Rev = AddSectionToMockedBook(m_philemonRev);
			AddSectionHeadParaToSection(section2Rev, "Section Head", ScrStyleNames.SectionHead);
			IScrTxtPara para2Rev = AddParaToMockedSectionContent(section2Rev, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para2Rev, "5", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para2Rev, "This is verse five.", Cache.DefaultVernWs);

			// Detect differences
			m_bookMerger.DetectDifferences(null);

			// Check the diff and section counts before doing the restore
			Assert.AreEqual(1, m_bookMerger.Differences.Count);
			Assert.AreEqual(1, m_philemonCurr.SectionsOS.Count);
			Assert.AreEqual(2, m_philemonRev.SectionsOS.Count);

			//Difference diff = m_bookMerger.Differences.MoveFirst();
			//Assert.AreEqual(DifferenceType.SectionMissingInCurrent, (DifferenceType)diff.DiffType);

			// Show the dialog; perform the Restore
			m_dlg.Show();
			m_dlg.SimulateRevertToOldButtonClick(false);

			// Verify the new section counts
			Assert.AreEqual(2, m_philemonCurr.SectionsOS.Count);
			Assert.AreEqual(2, m_philemonRev.SectionsOS.Count);

			// Verify the text of the paragraphs in the new section in current
			IScrSection insertedSection = m_philemonCurr.SectionsOS[0];
			Assert.AreEqual("Section Head Added",
				((IScrTxtPara)insertedSection.HeadingOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual("2Here is the long lost missing verse.",
				((IScrTxtPara)insertedSection.ContentOA.ParagraphsOS[0]).Contents.Text);
			// also check the refs of the new section
			Assert.AreEqual(57001002, insertedSection.VerseRefStart);
			Assert.AreEqual(57001002, insertedSection.VerseRefEnd);

			// Verify that the references of the following section are still correct
			Assert.AreEqual(57001005, sectionCurr.VerseRefStart);
			Assert.AreEqual(57001005, sectionCurr.VerseRefEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the case where a section is added to the current and a "Revert to original"
		/// is clicked to remove the section in the current.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RevertToOriginal_SectionAddedToCurrent()
		{
			// Create a section in current that does not match and will be counted as "added in Current"
			IScrSection section1Curr = AddSectionToMockedBook(m_philemonCurr);
			AddSectionHeadParaToSection(section1Curr, "Section Head Added", ScrStyleNames.SectionHead);
			IScrTxtPara para1Curr = AddParaToMockedSectionContent(section1Curr, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para1Curr, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para1Curr, "Here is the long lost missing verse.", Cache.DefaultVernWs);

			// create another section in Current: verse 5
			IScrSection section2Curr = AddSectionToMockedBook(m_philemonCurr);
			AddSectionHeadParaToSection(section2Curr, "Section Head", ScrStyleNames.SectionHead);
			IScrTxtPara para2Curr = AddParaToMockedSectionContent(section2Curr, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para2Curr, "5", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para2Curr, "This is verse five.", Cache.DefaultVernWs);

			// create a matching section in Revision: verse 5
			IScrSection sectionRev = AddSectionToMockedBook(m_philemonRev);
			AddSectionHeadParaToSection(sectionRev, "Section Head", ScrStyleNames.SectionHead);
			IScrTxtPara paraRev = AddParaToMockedSectionContent(sectionRev, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(paraRev, "5", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(paraRev, "This is verse five.", Cache.DefaultVernWs);

			// Detect differences
			m_bookMerger.DetectDifferences(null);

			// Check the diff and section counts before doing the restore
			Assert.AreEqual(1, m_bookMerger.Differences.Count);
			Assert.AreEqual(2, m_philemonCurr.SectionsOS.Count);
			Assert.AreEqual(1, m_philemonRev.SectionsOS.Count);

			Difference diff = m_bookMerger.Differences.MoveFirst();
			Assert.AreEqual(DifferenceType.SectionAddedToCurrent, (DifferenceType)diff.DiffType);

			// Show the dialog; perform the Restore
			m_dlg.Show();
			m_dlg.m_fPermitRevertToOld = false;
			m_dlg.SimulateRevertToOldButtonClick(false);
			// Verify the section counts haven't changed
			Assert.AreEqual(2, m_philemonCurr.SectionsOS.Count);
			Assert.AreEqual(1, m_philemonRev.SectionsOS.Count);
			m_dlg.m_fPermitRevertToOld = true;
			m_dlg.SimulateRevertToOldButtonClick(false);

			// Verify the new section counts
			Assert.AreEqual(1, m_philemonCurr.SectionsOS.Count);
			Assert.AreEqual(1, m_philemonRev.SectionsOS.Count);

			// Make sure the correct section was removed, by verifying the remaining section's section head
			IScrSection remainingSection = m_philemonCurr.SectionsOS[0];
			Assert.AreEqual("Section Head",
				((IScrTxtPara)remainingSection.HeadingOA.ParagraphsOS[0]).Contents.Text);
			// also check the refs of the remaining section
			Assert.AreEqual(57001005, remainingSection.VerseRefStart);
			Assert.AreEqual(57001005, remainingSection.VerseRefEnd);
		}
		#endregion
	}
	#endregion
}
