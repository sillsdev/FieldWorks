// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: TestFwFindReplaceDlg.cs
// Responsibility: TE Team

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Test.TestUtils;
using SIL.Utils;
using SIL.Utils.Attributes;

namespace SIL.FieldWorks.FwCoreDlgs
{
	#region DummyFwFindReplaceDlg
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Dummy find dialog
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyFwFindReplaceDlg : FwFindReplaceDlg
	{
		/// <summary></summary>
		public MatchType m_matchNotFoundType = MatchType.NotSet;
		/// <summary></summary>
		public string m_matchMsg = string.Empty;
		/// <summary><c>true</c> if the "Invalid RegEx" message box was displayed</summary>
		public bool m_fInvalidRegExDisplayed;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DummyFwFindReplaceDlg()
		{
			MatchNotFound += OnMatchNotFound;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Displays a message box that the regular expression is invalid.
		/// </summary>
		/// <param name="errorMessage">The error message.</param>
		/// ------------------------------------------------------------------------------------
		protected override void DisplayInvalidRegExMessage(string errorMessage)
		{
			m_fInvalidRegExDisplayed = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="defaultMsg"></param>
		/// <param name="matchType"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private bool OnMatchNotFound(object sender, string defaultMsg, MatchType matchType)
		{
			m_matchMsg = defaultMsg;
			m_matchNotFoundType = matchType;
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the m_prevSearchText member variable.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ITsString PrevPatternText
		{
			get
			{
				CheckDisposed();
				return m_prevSearchText;
			}
			set
			{
				CheckDisposed();
				m_prevSearchText = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the Find Text box on the Find/Replace dialog
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FwTextBox FindTextControl
		{
			get
			{
				CheckDisposed();
				return fweditFindText;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the Find Format label on the Find/Replace dialog
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Label FindFormatLabel
		{
			get
			{
				CheckDisposed();
				return lblFindFormat;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the Find Format Text label on the Find/Replace dialog
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Label FindFormatTextLabel
		{
			get
			{
				CheckDisposed();
				return lblFindFormatText;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the Replace Text box on the Find/Replace dialog
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FwTextBox ReplaceTextControl
		{
			get
			{
				CheckDisposed();
				return fweditReplaceText;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the Replace Format label on the Find/Replace dialog
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Label FindReplaceLabel
		{
			get
			{
				CheckDisposed();
				return lblReplaceFormat;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the Find Replace Text label on the Find/Replace dialog
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Label FindReplaceTextLabel
		{
			get
			{
				CheckDisposed();
				return lblReplaceFormatText;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the find collector environment.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FindCollectorEnv FindEnvironment
		{
			get
			{
				CheckDisposed();
				return m_findEnvironment;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets whether or not the "Match Diacritics" checkbox on the Find/Replace dialog is
		/// checked.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool MatchDiacriticsCheckboxChecked
		{
			get
			{
				CheckDisposed();
				return chkMatchDiacritics.Checked;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets whether or not the "Match Writing System" checkbox on the Find/Replace dialog
		/// is checked.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool MatchWsCheckboxChecked
		{
			get
			{
				CheckDisposed();
				return chkMatchWS.Checked;
			}
			set
			{
				CheckDisposed();
				chkMatchWS.Checked = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets whether or not the "Match Case" checkbox on the Find/Replace dialog is
		/// checked.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool MatchCaseCheckboxChecked
		{
			get
			{
				CheckDisposed();
				return chkMatchCase.Checked;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets whether or not the "Match Whole Word" checkbox on the Find/Replace dialog is
		/// checked.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool MatchWholeWordCheckboxChecked
		{
			get
			{
				CheckDisposed();
				return chkMatchWholeWord.Checked;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets whether or not the "Use Regular Expressions" checkbox on the Find/Replace
		/// dialog is checked.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool UseRegExCheckboxChecked
		{
			get
			{
				CheckDisposed();
				return chkUseRegularExpressions.Checked;
			}
			set
			{
				CheckDisposed();
				chkUseRegularExpressions.Checked = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets whether or not the "More Controls" panel on the Find/Replace dialog is visible.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool MoreControlsPanelVisible
		{
			get
			{
				CheckDisposed();
				return panelSearchOptions.Visible;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SimulateFindPrevButtonClick()
		{
			CheckDisposed();

			FindPrevious();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SimulateFindButtonClick()
		{
			CheckDisposed();

			m_fInvalidRegExDisplayed = false;
			OnFindNext(null, new EventArgs());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SimulateReplaceButtonClick()
		{
			CheckDisposed();
			DoReplace(CurrentSelection);
			m_cache.ServiceLocator.GetInstance<IActionHandler>().BreakUndoTask("blah", "blah");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SimulateReplaceAllButtonClick()
		{
			CheckDisposed();

			DoReplaceAll();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Button CloseButton
		{
			get
			{
				CheckDisposed();
				return btnClose;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Expose the PopulateStyleMenu method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public new void PopulateStyleMenu()
		{
			CheckDisposed();

			base.PopulateStyleMenu();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the style menu
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public MenuItem StyleMenu
		{
			get
			{
				CheckDisposed();
				return mnuStyle;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the ApplyStyle method
		/// </summary>
		/// <param name="fwTextBox">The Tss edit control whose selection should have the
		/// specified style applied to it.</param>
		/// <param name="sStyle">The name of the style to apply</param>
		/// ------------------------------------------------------------------------------------
		public new void ApplyStyle(FwTextBox fwTextBox, string sStyle)
		{
			CheckDisposed();

			base.ApplyStyle(fwTextBox, sStyle);
			Focus();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Expose the PopulateWritingSystemMenu method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public new void PopulateWritingSystemMenu()
		{
			CheckDisposed();

			base.PopulateWritingSystemMenu();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the writing system menu
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public MenuItem WritingSystemMenu
		{
			get
			{
				CheckDisposed();
				return mnuWritingSystem;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that the correct selection was made as the result of the find/replace
		/// operation.
		/// </summary>
		/// <param name="iInstancePara">The index of the instance of the paragraph property
		/// in whose contants the selection was expected.</param>
		/// <param name="iPara">The index of the para where the selection was expected.</param>
		/// <param name="iInstanceString">The index of the instance of the string property where
		/// the selection was expected.</param>
		/// <param name="ichAnchor">The character offset of the anchor.</param>
		/// <param name="ichEnd">The character offset of the end.</param>
		/// ------------------------------------------------------------------------------------
		public void VerifySelection(int iInstancePara, int iPara, int iInstanceString,
			int ichAnchor, int ichEnd)
		{
			CheckDisposed();

			SelectionHelper helper = ((DummyBasicView)m_vwRootsite).EditingHelper.CurrentSelection;
			SelLevInfo[] selLevels = helper.GetLevelInfo(SelectionHelper.SelLimitType.Anchor);
			Assert.AreEqual(1, selLevels.Length);
			Assert.AreEqual(iPara, selLevels[0].ihvo);
			Assert.AreEqual(14001, selLevels[0].tag);
			Assert.AreEqual(iInstancePara, selLevels[0].cpropPrevious);

			selLevels = helper.GetLevelInfo(SelectionHelper.SelLimitType.End);
			Assert.AreEqual(1, selLevels.Length);
			Assert.AreEqual(iPara, selLevels[0].ihvo);
			Assert.AreEqual(14001, selLevels[0].tag);
			Assert.AreEqual(iInstancePara, selLevels[0].cpropPrevious);

			Assert.AreEqual(ichAnchor, helper.IchAnchor);
			Assert.AreEqual(ichEnd, helper.IchEnd);
			Assert.AreEqual(16002, helper.GetTextPropId(SelectionHelper.SelLimitType.Anchor));
			Assert.AreEqual(16002, helper.GetTextPropId(SelectionHelper.SelLimitType.End));
			Assert.AreEqual(iInstanceString,
				helper.GetNumberOfPreviousProps(SelectionHelper.SelLimitType.Anchor));
			Assert.AreEqual(iInstanceString,
				helper.GetNumberOfPreviousProps(SelectionHelper.SelLimitType.End));
		}
	}
	#endregion

	#region class FwFindReplaceDlgBaseTests
	/// -----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for FwFindReplaceDlgBaseTests.
	/// </summary>
	/// -----------------------------------------------------------------------------------------
	public class FwFindReplaceDlgBaseTests : ScrInMemoryFdoTestBase
	{
		#region Data members
		/// <summary></summary>
		protected const string m_kTitleText = "Blah, blah, blah!";
		/// <summary></summary>
		protected DummyFwFindReplaceDlg m_dlg;
		/// <summary></summary>
		protected DummyBasicView m_vwRootsite;
		/// <summary></summary>
		protected IVwPattern m_vwPattern;
		/// <summary></summary>
		protected IVwStylesheet m_Stylesheet;
		/// <summary></summary>
		protected IStText m_text;
		/// <summary></summary>
		protected IScrBook m_genesis;
		#endregion

		#region setup & teardown
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the ScrReference for testing.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			WritingSystem ws;
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("en-fonipa-x-etic", out ws);
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("es", out ws);
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("de", out ws);
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("fr", out ws);
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("ur", out ws);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create the dialog
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public override void TestSetup()
		{
			base.TestSetup();

			WritingSystemManager wsManager = Cache.ServiceLocator.WritingSystemManager;
			m_genesis = AddBookToMockedScripture(1, "Genesis");
			m_text = AddTitleToMockedBook(m_genesis, m_kTitleText, wsManager.GetWsFromStr("en-fonipa-x-etic"));

			m_vwPattern = VwPatternClass.Create();
			m_Stylesheet = new TestFwStylesheet();

			m_vwRootsite = new DummyBasicView();
			m_vwRootsite.StyleSheet = m_Stylesheet;
			m_vwRootsite.Cache = Cache;
			m_vwRootsite.DisplayType = DummyBasicViewVc.DisplayType.kMappedPara; // Needed for some footnote tests
			m_vwRootsite.MakeRoot(m_text.Hvo, ScrBookTags.kflidTitle, 3);

			m_dlg = new DummyFwFindReplaceDlg();

			IWritingSystemContainer wsContainer = Cache.ServiceLocator.WritingSystems;
			wsContainer.AnalysisWritingSystems.Add(wsManager.Get("en-fonipa-x-etic"));
			wsContainer.AnalysisWritingSystems.Add(wsManager.Get("fr"));
			wsContainer.AnalysisWritingSystems.Add(wsManager.Get("de"));
			wsContainer.AnalysisWritingSystems.Add(wsManager.Get("es"));
			wsContainer.AnalysisWritingSystems.Add(wsManager.Get("ur"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Dispose of the dialog
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public override void TestTearDown()
		{
			if (m_dlg != null)
			{
				if (m_dlg.IsHandleCreated)
					m_dlg.Close();
				m_dlg.Dispose();
				m_dlg = null;
			}

			if (m_vwRootsite != null)
			{
				m_vwRootsite.Dispose();
				m_vwRootsite = null;
			}
			if (m_vwPattern != null)
			{
				if (Marshal.IsComObject(m_vwPattern))
					Marshal.ReleaseComObject(m_vwPattern);
				m_vwPattern = null;
			}
			if (m_Stylesheet != null)
			{
				if (Marshal.IsComObject(m_Stylesheet))
					Marshal.ReleaseComObject(m_Stylesheet);
				m_Stylesheet = null;
			}
			m_text = null;
			m_genesis = null;

			base.TestTearDown();
		}
		#endregion
	}
	#endregion class FwFindReplaceDlgBaseTests

	#region class FwFindReplaceDlgTests
	/// -----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for TestFwFindReplaceDlg.
	/// </summary>
	/// -----------------------------------------------------------------------------------------
	[TestFixture]
	public class FwFindReplaceDlgTests : FwFindReplaceDlgBaseTests
	{
		private WritingSystem m_wsFr;
		private WritingSystem m_wsIpa;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the ScrReference for testing.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("fr", out m_wsFr);
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("en-fonipa-x-etic", out m_wsIpa);
		}

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Setup mocks to return styles
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void SetupStylesheet()
		{
			int hvoStyle = m_Stylesheet.MakeNewStyle();
			m_Stylesheet.PutStyle("CStyle3", "bla", hvoStyle, 0, 0, 1, false, false, null);
			hvoStyle = m_Stylesheet.MakeNewStyle();
			m_Stylesheet.PutStyle("CStyle2", "bla", hvoStyle, 0, 0, 1, false, false, null);
			hvoStyle = m_Stylesheet.MakeNewStyle();
			m_Stylesheet.PutStyle("PStyle1", "bla", hvoStyle, 0, 0, 0, false, false, null);
			hvoStyle = m_Stylesheet.MakeNewStyle();
			m_Stylesheet.PutStyle("CStyle1", "bla", hvoStyle, 0, 0, 1, false, false, null);
		}
		#endregion

		#region Dialog initilization tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test to see if the selected text gets copied into the find dialog and verify state
		/// of controls.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CheckInitialDlgState()
		{
			SelectionHelper helper = SelectionHelper.Create(m_vwRootsite);
			helper.IchEnd = 4; // Select the first "Blah" in the view
			IVwSelection selInitial = helper.SetSelection(false);
			m_dlg.SetDialogValues(Cache, m_vwPattern, m_vwRootsite, false, false,
				null, null, null);

			// None of the options are checked initially and the More pane should be hidden
			// Except now the MatchDiacritics defaults to checked (LT-8191)
			ITsString tss;
			selInitial.GetSelectionString(out tss, string.Empty);
			AssertEx.AreTsStringsEqual(tss, m_dlg.FindText);
			Assert.IsFalse(m_dlg.MatchWsCheckboxChecked);
			Assert.IsTrue(m_dlg.MatchDiacriticsCheckboxChecked);
			Assert.IsFalse(m_dlg.MatchWholeWordCheckboxChecked);
			Assert.IsFalse(m_dlg.MatchCaseCheckboxChecked);
			Assert.IsFalse(m_dlg.MoreControlsPanelVisible);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verify that the Styles menu contains the correct styles.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerifyAllStylesInMenu()
		{
			SetupStylesheet();
			m_dlg.SetDialogValues(Cache, m_vwPattern, m_vwRootsite, false, false,
				null, null, null);
			m_dlg.PopulateStyleMenu();

			Assert.AreEqual("<no style>", m_dlg.StyleMenu.MenuItems[0].Text);
			Assert.IsTrue(m_dlg.StyleMenu.MenuItems[0].Checked);
			Assert.AreEqual("Default Paragraph Characters", m_dlg.StyleMenu.MenuItems[1].Text);
			Assert.IsFalse(m_dlg.StyleMenu.MenuItems[1].Checked);
			Assert.AreEqual("CStyle1", m_dlg.StyleMenu.MenuItems[2].Text);
			Assert.IsFalse(m_dlg.StyleMenu.MenuItems[2].Checked);
			Assert.AreEqual("CStyle2", m_dlg.StyleMenu.MenuItems[3].Text);
			Assert.IsFalse(m_dlg.StyleMenu.MenuItems[3].Checked);
			Assert.AreEqual("CStyle3", m_dlg.StyleMenu.MenuItems[4].Text);
			Assert.IsFalse(m_dlg.StyleMenu.MenuItems[4].Checked);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verify that the Styles menu contains the correct checked style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerifyCheckedStyle()
		{
			SetupStylesheet();
			SelectionHelper helper = SelectionHelper.Create(m_vwRootsite);
			helper.IchEnd = 4; // Select the first "Blah" in the view
			helper.SetSelection(false);
			m_dlg.SetDialogValues(Cache, m_vwPattern, m_vwRootsite, false, false,
				null, null, null);

			m_dlg.ApplyStyle(m_dlg.FindTextControl, "CStyle3");
			m_dlg.PopulateStyleMenu();

			Assert.AreEqual("CStyle3", m_dlg.StyleMenu.MenuItems[4].Text);
			Assert.IsTrue(m_dlg.StyleMenu.MenuItems[4].Checked);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verify that the Writing System menu contains the correct writing systems.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerifyAllWritingSystemsInMenu()
		{
			m_dlg.SetDialogValues(Cache, m_vwPattern, m_vwRootsite, false, false,
				null, null, null);
			// For this test, we have a simple IP in French text, so that WS should be checked.
			m_dlg.PopulateWritingSystemMenu();

			Assert.AreEqual(6, m_dlg.WritingSystemMenu.MenuItems.Count);
			int i = 0;
			Assert.AreEqual("English", m_dlg.WritingSystemMenu.MenuItems[i].Text);
			Assert.IsFalse(m_dlg.WritingSystemMenu.MenuItems[i++].Checked);
			Assert.AreEqual("English (Phonetic)", m_dlg.WritingSystemMenu.MenuItems[i].Text);
			Assert.IsTrue(m_dlg.WritingSystemMenu.MenuItems[i++].Checked);
			// Depending on the ICU files present on a given machine, we may or may not have an English name for the French WS.
			Assert.IsTrue(m_dlg.WritingSystemMenu.MenuItems[i].Text == "fr" || m_dlg.WritingSystemMenu.MenuItems[i].Text == "French");
			Assert.IsFalse(m_dlg.WritingSystemMenu.MenuItems[i++].Checked);
			Assert.AreEqual("German", m_dlg.WritingSystemMenu.MenuItems[i].Text);
			Assert.IsFalse(m_dlg.WritingSystemMenu.MenuItems[i++].Checked);
			Assert.AreEqual("Spanish", m_dlg.WritingSystemMenu.MenuItems[i].Text);
			Assert.IsFalse(m_dlg.WritingSystemMenu.MenuItems[i++].Checked);
			Assert.AreEqual("Urdu", m_dlg.WritingSystemMenu.MenuItems[i].Text);
			Assert.IsFalse(m_dlg.WritingSystemMenu.MenuItems[i].Checked);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verify that nothing in the Writing System menu is checked when selection contains
		/// multiple writing systems.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerifyNoCheckedWritingSystem()
		{
			IStTxtPara para = m_text[0];
			ITsStrBldr bldr = para.Contents.GetBldr();
			bldr.SetIntPropValues(0, 2, (int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault,
				Cache.ServiceLocator.WritingSystemManager.GetWsFromStr("de"));
			para.Contents = bldr.GetString();

			m_vwRootsite.RootBox.Reconstruct();
			m_vwRootsite.RootBox.MakeSimpleSel(true, true, false, true);

			SelectionHelper helper = SelectionHelper.Create(m_vwRootsite);
			helper.IchEnd = 4; // Select the first "Blah" in the view (two different WS's)
			helper.SetSelection(false);
			m_dlg.SetDialogValues(Cache, m_vwPattern, m_vwRootsite, false, false,
				null, null, null);
			m_dlg.PopulateWritingSystemMenu();

			Assert.AreEqual(6, m_dlg.WritingSystemMenu.MenuItems.Count);
			foreach (MenuItem mi in m_dlg.WritingSystemMenu.MenuItems)
				Assert.IsFalse(mi.Checked);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that reopening the dialog when previously a WS but no find text was specified
		/// remembers the WS. (TE-5127)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReopeningRemembersWsWithoutText()
		{
			SelectionHelper helper = SelectionHelper.Create(m_vwRootsite);
			helper.SetSelection(true);
			m_dlg.SetDialogValues(Cache, m_vwPattern, m_vwRootsite, false, false,
				null, null, null);

			// Set a writing system
			m_dlg.ApplyWS(m_dlg.FindTextControl, Cache.WritingSystemFactory.GetWsFromStr("de"));

			// Simulate a find. This is necessary to save the changes.
			m_dlg.PrevPatternText = null;
			m_dlg.SimulateFindButtonClick();

			m_dlg.PopulateWritingSystemMenu();
			Assert.AreEqual(0, m_dlg.FindText.Length, "Shouldn't have any find text before closing dialog");
			Assert.IsTrue(m_dlg.MatchWsCheckboxChecked, "WS Checkbox should be checked before closing dialog");
			Assert.AreEqual("German", m_dlg.FindFormatTextLabel.Text);

			m_dlg.Hide(); // this is usually done in OnClosing, but for whatever reason that doesn't work in our test

			// Simulate reshowing the dialog
			helper.IchAnchor = 0;
			helper.IchEnd = 0;
			helper.SetSelection(true);
			m_dlg.SetDialogValues(Cache, m_vwPattern, m_vwRootsite, false, false,
				null, null, null);

			m_dlg.PopulateWritingSystemMenu();
			Assert.AreEqual(0, m_dlg.FindText.Length, "Shouldn't have any find text after reopening dialog");
			Assert.IsTrue(m_dlg.MatchWsCheckboxChecked, "WS Checkbox should be checked after reopening dialog");
			Assert.AreEqual("German", m_dlg.FindFormatTextLabel.Text);

			// Match diacritics now defaults to checked (LT-8191)
			Assert.IsTrue(m_dlg.MatchDiacriticsCheckboxChecked);
			Assert.IsFalse(m_dlg.MatchWholeWordCheckboxChecked);
			Assert.IsFalse(m_dlg.MatchCaseCheckboxChecked);
			Assert.IsFalse(m_dlg.MoreControlsPanelVisible);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the LastTextBoxInFocus property.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Category("DesktopRequired")]
		public void LastTextBoxInFocus()
		{
			m_dlg.SetDialogValues(Cache, m_vwPattern, m_vwRootsite, true, false,
				null, null, null);

			m_dlg.Show();
			Application.DoEvents();

			Assert.AreEqual(m_dlg.FindTextControl, m_dlg.LastTextBoxInFocus);

			// set the focus to the replace box
			m_dlg.ReplaceTextControl.Focus();

			Assert.AreEqual(m_dlg.FindTextControl, m_dlg.LastTextBoxInFocus);

			// set the focus to the find box
			m_dlg.FindTextControl.Focus();

			Assert.AreEqual(m_dlg.ReplaceTextControl, m_dlg.LastTextBoxInFocus);
		}
		#endregion

		#region Apply style tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ApplyStyle method with a range selection
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ApplyStyle_ToSelectedString()
		{
			SelectionHelper helper = SelectionHelper.Create(m_vwRootsite);
			helper.IchEnd = 4; // Select the first "Blah" in the view (two different WS's)
			helper.SetSelection(false);
			SetupStylesheet();
			m_dlg.SetDialogValues(Cache, m_vwPattern, m_vwRootsite, false, false,
				null, null, null);
			m_dlg.Show();
			Application.DoEvents();
			Assert.IsFalse(m_dlg.FindFormatLabel.Visible);
			Assert.IsFalse(m_dlg.FindFormatTextLabel.Visible);

			m_dlg.ApplyStyle(m_dlg.FindTextControl, "CStyle3");

			ITsPropsBldr propsBldr = TsPropsBldrClass.Create();
			propsBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "CStyle3");
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptWs,
				(int)FwTextPropVar.ktpvDefault, Cache.WritingSystemFactory.GetWsFromStr("en-fonipa-x-etic"));
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			strBldr.Replace(0, 0, "Blah", propsBldr.GetTextProps());
			ITsString tssExpected = strBldr.GetString();
			AssertEx.AreTsStringsEqual(tssExpected, m_dlg.FindTextControl.Tss);
			Assert.IsTrue(m_dlg.FindFormatLabel.Visible);
			Assert.IsTrue(m_dlg.FindFormatTextLabel.Visible);
			Assert.AreEqual("CStyle3", m_dlg.FindFormatTextLabel.Text);

			// If we check the match WS checkbox we need to show the writing system
			m_dlg.MatchWsCheckboxChecked = true;
			Assert.AreEqual("CStyle3, English (Phonetic)", m_dlg.FindFormatTextLabel.Text);
			m_dlg.MatchWsCheckboxChecked = false;

			strBldr.SetStrPropValue(0, 4, (int)FwTextPropType.ktptNamedStyle, null);
			tssExpected = strBldr.GetString();

			// Not sure why we have to do this, but the selection seems to get lost in the test,
			// so if we don't do this, it applies the style to an IP, and the test fails.
			m_dlg.FindTextControl.SelectAll();
			m_dlg.ApplyStyle(m_dlg.FindTextControl, "<No Style>");

			AssertEx.AreTsStringsEqual(tssExpected, m_dlg.FindTextControl.Tss);
			Assert.IsFalse(m_dlg.FindFormatLabel.Visible);
			Assert.IsFalse(m_dlg.FindFormatTextLabel.Visible);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ApplyStyle method applying multiple styles to a string
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ApplyStyle_MultipleStyles()
		{
			SelectionHelper helper = SelectionHelper.Create(m_vwRootsite);
			helper.IchEnd = 10; // Select the "Blah, blah" in the view
			helper.SetSelection(false);
			SetupStylesheet();
			m_dlg.SetDialogValues(Cache, m_vwPattern, m_vwRootsite, false, false,
				null, null, null);
			m_dlg.Show();
			Application.DoEvents();
			Assert.IsFalse(m_dlg.FindFormatLabel.Visible);
			Assert.IsFalse(m_dlg.FindFormatTextLabel.Visible);

			m_dlg.ApplyWS(m_dlg.FindTextControl, Cache.WritingSystemFactory.GetWsFromStr("de"));
			m_dlg.FindTextControl.Select(0, 4);
			m_dlg.ApplyStyle(m_dlg.FindTextControl, "CStyle3");
			m_dlg.FindTextControl.Select(4, 6);
			m_dlg.ApplyStyle(m_dlg.FindTextControl, "CStyle2");

			// make the string backwards...
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			strBldr.Replace(0, 0, ", blah", StyleUtils.CharStyleTextProps("CStyle2", Cache.WritingSystemFactory.GetWsFromStr("de")));
			strBldr.Replace(0, 0, "Blah", StyleUtils.CharStyleTextProps("CStyle3", Cache.WritingSystemFactory.GetWsFromStr("de")));
			ITsString tssExpected = strBldr.GetString();
			AssertEx.AreTsStringsEqual(tssExpected, m_dlg.FindTextControl.Tss);
			Assert.IsTrue(m_dlg.MatchWsCheckboxChecked);
			Assert.IsTrue(m_dlg.FindFormatLabel.Visible);
			Assert.IsTrue(m_dlg.FindFormatTextLabel.Visible);
			Assert.AreEqual("Multiple Styles, German", m_dlg.FindFormatTextLabel.Text);

			// unchecking the match WS should hide the WS name
			m_dlg.MatchWsCheckboxChecked = false;
			Assert.IsTrue(m_dlg.FindFormatLabel.Visible);
			Assert.IsTrue(m_dlg.FindFormatTextLabel.Visible);
			Assert.AreEqual("Multiple Styles", m_dlg.FindFormatTextLabel.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ApplyStyle method applying a style to part of a string
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ApplyStyle_StyleOnPartOfString()
		{
			SelectionHelper helper = SelectionHelper.Create(m_vwRootsite);
			helper.IchEnd = 10; // Select the "Blah, blah" in the view
			helper.SetSelection(false);
			SetupStylesheet();
			m_dlg.SetDialogValues(Cache, m_vwPattern, m_vwRootsite, false, false,
				null, null, null);
			m_dlg.Show();
			Application.DoEvents();
			Assert.IsFalse(m_dlg.FindFormatLabel.Visible);
			Assert.IsFalse(m_dlg.FindFormatTextLabel.Visible);

			m_dlg.FindTextControl.Select(4, 6);
			m_dlg.ApplyStyle(m_dlg.FindTextControl, "CStyle2");

			// make the string backwards...
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			strBldr.Replace(0, 0, ", blah", StyleUtils.CharStyleTextProps("CStyle2", Cache.WritingSystemFactory.GetWsFromStr("en-fonipa-x-etic")));
			strBldr.Replace(0, 0, "Blah", StyleUtils.CharStyleTextProps(null, Cache.WritingSystemFactory.GetWsFromStr("en-fonipa-x-etic")));
			ITsString tssExpected = strBldr.GetString();
			AssertEx.AreTsStringsEqual(tssExpected, m_dlg.FindTextControl.Tss);
			Assert.IsTrue(m_dlg.FindFormatLabel.Visible);
			Assert.IsTrue(m_dlg.FindFormatTextLabel.Visible);
			Assert.AreEqual("Multiple Styles", m_dlg.FindFormatTextLabel.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ApplyStyle method with an IP in an empty text box
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ApplyStyle_ToEmptyTextBox()
		{
			SetupStylesheet();
			m_dlg.SetDialogValues(Cache, m_vwPattern, m_vwRootsite, false, false,
				null, null, null);
			m_dlg.Show();
			Application.DoEvents();

			m_dlg.ApplyStyle(m_dlg.FindTextControl, "CStyle3");

			Assert.IsTrue(m_dlg.FindTextControl.Focused,
				"Focus should have returned to Find text box");
			ITsString tssFind = m_dlg.FindTextControl.Tss;
			Assert.AreEqual(1, tssFind.RunCount);
			Assert.AreEqual("CStyle3", tssFind.get_Properties(0).GetStrPropValue(
				(int)FwTextPropType.ktptNamedStyle));
		}
		#endregion

		#region Apply Writing System tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ApplyWritingSystem method with a range selection
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ApplyWS_ToSelectedString()
		{
			SelectionHelper helper = SelectionHelper.Create(m_vwRootsite);
			helper.IchEnd = 4; // Select the first "Blah" in the view (two different WS's)
			helper.SetSelection(false);
			m_dlg.SetDialogValues(Cache, m_vwPattern, m_vwRootsite, true, false,
				null, null, null);
			m_dlg.Show();
			Application.DoEvents();

			m_dlg.ApplyWS(m_dlg.FindTextControl, Cache.WritingSystemFactory.GetWsFromStr("de"));
			Assert.IsTrue(m_dlg.FindTextControl.Focused, "Focus should have returned to Find box");

			ITsPropsBldr propsBldr = TsPropsBldrClass.Create();
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptWs,
				(int)FwTextPropVar.ktpvDefault, Cache.WritingSystemFactory.GetWsFromStr("de"));

			ITsStrBldr strBldr = TsStrBldrClass.Create();
			strBldr.Replace(0, 0, "Blah", propsBldr.GetTextProps());
			ITsString tssExpected = strBldr.GetString();
			AssertEx.AreTsStringsEqual(tssExpected, m_dlg.FindTextControl.Tss);
			Assert.IsTrue(m_dlg.MatchWsCheckboxChecked);
			Assert.IsTrue(m_dlg.FindFormatLabel.Visible);
			Assert.IsTrue(m_dlg.FindFormatTextLabel.Visible);
			Assert.AreEqual("German", m_dlg.FindFormatTextLabel.Text);

			// We should only show the WS information if the match WS check box is checked
			m_dlg.MatchWsCheckboxChecked = false;
			Assert.IsFalse(m_dlg.FindFormatLabel.Visible);
			Assert.IsFalse(m_dlg.FindFormatTextLabel.Visible);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ApplyWritingSystem method applying multiple writing systems
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ApplyWS_MultipleWritingSystems()
		{
			// Blah, blah, blah!
			SelectionHelper helper = SelectionHelper.Create(m_vwRootsite);
			helper.IchEnd = 10; // Select the "Blah, blah" in the view
			helper.SetSelection(false);
			m_dlg.SetDialogValues(Cache, m_vwPattern, m_vwRootsite, true, false,
				null, null, null);
			m_dlg.Show();
			Application.DoEvents();

			m_dlg.FindTextControl.Select(0, 4);
			m_dlg.ApplyWS(m_dlg.FindTextControl, Cache.WritingSystemFactory.GetWsFromStr("de"));
			m_dlg.FindTextControl.Select(4, 6);
			m_dlg.ApplyWS(m_dlg.FindTextControl, Cache.WritingSystemFactory.GetWsFromStr("en"));
			Assert.IsTrue(m_dlg.FindTextControl.Focused, "Focus should have returned to Find box");

			// make the string backwards...
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			strBldr.Replace(0, 0, ", blah", StyleUtils.CharStyleTextProps(null, Cache.WritingSystemFactory.GetWsFromStr("en")));
			strBldr.Replace(0, 0, "Blah", StyleUtils.CharStyleTextProps(null, Cache.WritingSystemFactory.GetWsFromStr("de")));
			ITsString tssExpected = strBldr.GetString();
			AssertEx.AreTsStringsEqual(tssExpected, m_dlg.FindTextControl.Tss);
			Assert.IsTrue(m_dlg.MatchWsCheckboxChecked);
			Assert.IsTrue(m_dlg.FindFormatLabel.Visible);
			Assert.IsTrue(m_dlg.FindFormatTextLabel.Visible);
			Assert.AreEqual("Multiple Writing Systems", m_dlg.FindFormatTextLabel.Text);

			// We should only show the WS information if the match WS check box is checked
			m_dlg.MatchWsCheckboxChecked = false;
			Assert.IsFalse(m_dlg.FindFormatLabel.Visible);
			Assert.IsFalse(m_dlg.FindFormatTextLabel.Visible);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ApplyWritingSystem method with an IP in an empty text box
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ApplyWS_ToEmptyTextBox()
		{
			m_dlg.SetDialogValues(Cache, m_vwPattern, m_vwRootsite, false, false,
				null, null, null);
			m_dlg.Show();
			Application.DoEvents();

			m_dlg.ApplyWS(m_dlg.FindTextControl, Cache.WritingSystemFactory.GetWsFromStr("de"));

			Assert.IsTrue(m_dlg.FindTextControl.Focused, "Focus should have returned to Find box");
			ITsString tssFind = m_dlg.FindTextControl.Tss;
			Assert.AreEqual(1, tssFind.RunCount);
			int nvar;
			Assert.AreEqual(Cache.WritingSystemFactory.GetWsFromStr("de"), tssFind.get_Properties(0).GetIntPropValues(
				(int)FwTextPropType.ktptWs, out nvar));
			Assert.IsTrue(m_dlg.MatchWsCheckboxChecked);
			Assert.IsTrue(m_dlg.FindFormatLabel.Visible);
			Assert.IsTrue(m_dlg.FindFormatTextLabel.Visible);
			Assert.AreEqual("German", m_dlg.FindFormatTextLabel.Text);

			// We should only show the WS information if the match WS check box is checked
			m_dlg.MatchWsCheckboxChecked = false;
			Assert.IsFalse(m_dlg.FindFormatLabel.Visible);
			Assert.IsFalse(m_dlg.FindFormatTextLabel.Visible);
		}
		#endregion

		#region Both Style and WS tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ApplyWritingSystem method applying multiple writing systems and one style
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ApplyWS_OneStyleMultipleWritingSystems()
		{
			SetupStylesheet();
			SelectionHelper helper = SelectionHelper.Create(m_vwRootsite);
			helper.IchEnd = 10; // Select the "Blah, blah" in the view
			helper.SetSelection(false);
			m_dlg.SetDialogValues(Cache, m_vwPattern, m_vwRootsite, true, false,
				null, null, null);
			m_dlg.Show();
			Application.DoEvents();

			m_dlg.FindTextControl.Select(0, 10);
			m_dlg.ApplyStyle(m_dlg.FindTextControl, "CStyle3");
			m_dlg.FindTextControl.Select(0, 4);
			m_dlg.ApplyWS(m_dlg.FindTextControl, Cache.WritingSystemFactory.GetWsFromStr("de"));
			m_dlg.FindTextControl.Select(4, 6);
			m_dlg.ApplyWS(m_dlg.FindTextControl, Cache.WritingSystemFactory.GetWsFromStr("en"));
			Assert.IsTrue(m_dlg.FindTextControl.Focused, "Focus should have returned to Find box");

			// make the string backwards...
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			strBldr.Replace(0, 0, ", blah", StyleUtils.CharStyleTextProps("CStyle3", Cache.WritingSystemFactory.GetWsFromStr("en")));
			strBldr.Replace(0, 0, "Blah", StyleUtils.CharStyleTextProps("CStyle3", Cache.WritingSystemFactory.GetWsFromStr("de")));
			ITsString tssExpected = strBldr.GetString();
			AssertEx.AreTsStringsEqual(tssExpected, m_dlg.FindTextControl.Tss);
			Assert.IsTrue(m_dlg.MatchWsCheckboxChecked);
			Assert.IsTrue(m_dlg.FindFormatLabel.Visible);
			Assert.IsTrue(m_dlg.FindFormatTextLabel.Visible);
			Assert.AreEqual("CStyle3, Multiple Writing Systems", m_dlg.FindFormatTextLabel.Text);

			// When we uncheck the match WS checkbox we should hide the WS information
			m_dlg.MatchWsCheckboxChecked = false;
			Assert.AreEqual("CStyle3", m_dlg.FindFormatTextLabel.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ApplyWritingSystem method applying multiple writing systems and multiple
		/// styles
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ApplyWS_MultipleStylesMultipleWritingSystems()
		{
			// Blah, blah, blah!
			SetupStylesheet();
			SelectionHelper helper = SelectionHelper.Create(m_vwRootsite);
			helper.IchEnd = 10; // Select the "Blah, blah" in the view
			helper.SetSelection(false);
			m_dlg.SetDialogValues(Cache, m_vwPattern, m_vwRootsite, true, false,
				null, null, null);
			m_dlg.Show();
			Application.DoEvents();

			m_dlg.FindTextControl.Select(0, 4);
			m_dlg.ApplyWS(m_dlg.FindTextControl, Cache.WritingSystemFactory.GetWsFromStr("de"));
			m_dlg.ApplyStyle(m_dlg.FindTextControl, "CStyle3");
			m_dlg.FindTextControl.Select(4, 6);
			m_dlg.ApplyWS(m_dlg.FindTextControl, Cache.WritingSystemFactory.GetWsFromStr("en"));
			m_dlg.ApplyStyle(m_dlg.FindTextControl, "CStyle2");
			Assert.IsTrue(m_dlg.FindTextControl.Focused, "Focus should have returned to Find box");

			// make the string backwards...
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			strBldr.Replace(0, 0, ", blah", StyleUtils.CharStyleTextProps("CStyle2", Cache.WritingSystemFactory.GetWsFromStr("en")));
			strBldr.Replace(0, 0, "Blah", StyleUtils.CharStyleTextProps("CStyle3", Cache.WritingSystemFactory.GetWsFromStr("de")));
			ITsString tssExpected = strBldr.GetString();
			AssertEx.AreTsStringsEqual(tssExpected, m_dlg.FindTextControl.Tss);
			Assert.IsTrue(m_dlg.MatchWsCheckboxChecked);
			Assert.IsTrue(m_dlg.FindFormatLabel.Visible);
			Assert.IsTrue(m_dlg.FindFormatTextLabel.Visible);
			Assert.AreEqual("Multiple Styles, Multiple Writing Systems",
				m_dlg.FindFormatTextLabel.Text);

			// When we uncheck the match WS checkbox we should hide the WS information
			m_dlg.MatchWsCheckboxChecked = false;
			Assert.AreEqual("Multiple Styles", m_dlg.FindFormatTextLabel.Text);
		}
		#endregion

		#region Find tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test an initial search when finding a next match.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InitialFindWithMatch()
		{
			m_dlg.SetDialogValues(Cache, m_vwPattern, m_vwRootsite, false, false,
				null, null, null);

			m_dlg.FindText = TsStringHelper.MakeTSS("Blah", Cache.WritingSystemFactory.GetWsFromStr("fr"));

			m_dlg.PrevPatternText = null;
			m_dlg.SimulateFindButtonClick();
			m_dlg.VerifySelection(0, 0, 0, 0, 4);
			Assert.IsTrue(m_dlg.FindEnvironment.FoundMatch);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test an initial search when finding a previous match.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("Need to finish the find previous for this to work")]
		public void InitialFindPrevWithMatch()
		{
			m_vwRootsite.RootBox.MakeSimpleSel(false, true, false, true);
			m_dlg.SetDialogValues(Cache, m_vwPattern, m_vwRootsite, false, false,
				null, null, null);

			m_dlg.FindText = TsStringHelper.MakeTSS("Blah", Cache.WritingSystemFactory.GetWsFromStr("fr"));

			m_dlg.PrevPatternText = null;
			m_dlg.SimulateFindPrevButtonClick();
			Assert.IsTrue(m_dlg.FindEnvironment.FoundMatch);
			m_dlg.VerifySelection(0, 0, 2, 12, 16);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test an initial search when using an invalid regular expression (TE-4866).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InitialFindWithRegEx_Invalid()
		{
			m_dlg.SetDialogValues(Cache, m_vwPattern, m_vwRootsite, false, false,
				null, null, null);
			m_dlg.UseRegExCheckboxChecked = true;

			m_dlg.FindText = TsStringHelper.MakeTSS("?", Cache.WritingSystemFactory.GetWsFromStr("fr"));

			m_dlg.PrevPatternText = null;
			m_dlg.SimulateFindButtonClick();
			Assert.IsTrue(m_dlg.m_fInvalidRegExDisplayed);
			Assert.IsFalse(m_dlg.FindEnvironment.FoundMatch);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test an initial search when using a valid regular expression.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InitialFindWithRegEx_Valid()
		{
			m_dlg.SetDialogValues(Cache, m_vwPattern, m_vwRootsite, false, false,
				null, null, null);
			m_dlg.UseRegExCheckboxChecked = true;

			m_dlg.FindText = TsStringHelper.MakeTSS("(blah, )?", Cache.WritingSystemFactory.GetWsFromStr("fr"));

			m_dlg.PrevPatternText = null;
			m_dlg.SimulateFindButtonClick();
			Assert.IsFalse(m_dlg.m_fInvalidRegExDisplayed);
			Assert.IsTrue(m_dlg.FindEnvironment.FoundMatch);
			m_dlg.VerifySelection(0, 0, 0, 0, 6);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test an initial search when not finding a match even after wrapping around.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InitialFindWithNoMatch()
		{
			m_dlg.SetDialogValues(Cache, m_vwPattern, m_vwRootsite, false, false,
				null, null, null);

			m_dlg.FindText = TsStringHelper.MakeTSS("Text that ain't there",
				Cache.WritingSystemFactory.GetWsFromStr("fr"));

			m_dlg.PrevPatternText = null;
			m_dlg.SimulateFindButtonClick();

			// Make sure the dialog thinks there were no matches.
			Assert.IsFalse(m_dlg.FindEnvironment.FoundMatch);
			Assert.AreEqual(FwFindReplaceDlg.MatchType.NoMatchFound,
				m_dlg.m_matchNotFoundType);
			m_dlg.VerifySelection(0, 0, 0, 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test an initial backward search when not finding a match even after wrapping around.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InitialFindPrevWithNoMatch()
		{
			m_vwRootsite.RootBox.MakeSimpleSel(false, true, false, true);
			m_dlg.SetDialogValues(Cache, m_vwPattern, m_vwRootsite, false, false,
				null, null, null);

			m_dlg.FindText = TsStringHelper.MakeTSS("Text that ain't there",
				Cache.WritingSystemFactory.GetWsFromStr("fr"));

			m_dlg.PrevPatternText = null;
			m_dlg.SimulateFindPrevButtonClick();

			// Make sure the dialog thinks there were no matches.
			Assert.IsFalse(m_dlg.FindEnvironment.FoundMatch);
			Assert.AreEqual(FwFindReplaceDlg.MatchType.NoMatchFound,
				m_dlg.m_matchNotFoundType);
			m_dlg.VerifySelection(0, 0, 2, 17, 17);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test an initial search when finding a match after wrapping around.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InitialFindWithMatchAfterWrap()
		{
			IStTxtPara para = AddParaToMockedText(m_text, "Whatever");
			AddRunToMockedPara(para, "Waldo", Cache.WritingSystemFactory.GetWsFromStr("fr"));
			m_vwRootsite.RootBox.Reconstruct();
			SelLevInfo[] levInfo = new SelLevInfo[1];
			levInfo[0] = new SelLevInfo();
			levInfo[0].ihvo = 1;
			levInfo[0].tag = StTextTags.kflidParagraphs;
			Assert.IsNotNull(m_vwRootsite.RootBox.MakeTextSelection(0, 1, levInfo,
				StTxtParaTags.kflidContents, 1, 0, 0, Cache.WritingSystemFactory.GetWsFromStr("fr"),
				false, -1, null, true));

			m_dlg.SetDialogValues(Cache, m_vwPattern, m_vwRootsite, false, false,
				null, null, null);

			m_dlg.FindText = TsStringHelper.MakeTSS("Blah!", Cache.WritingSystemFactory.GetWsFromStr("fr"));

			m_dlg.PrevPatternText = null;
			m_dlg.SimulateFindButtonClick();
			Assert.IsTrue(m_dlg.FindEnvironment.FoundMatch);
			m_dlg.VerifySelection(0, 0, 0, 12, 17);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test an initial backward search when finding a match after wrapping around
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("Need to finish the find previous for this to work")]
		public void InitialFindPrevWithMatchAfterWrap()
		{
			IStTxtPara para = Cache.ServiceLocator.GetInstance<IStTxtParaFactory>().Create();
			m_text.ParagraphsOS.Insert(0, para);
			AddRunToMockedPara(para, "Waldo", Cache.WritingSystemFactory.GetWsFromStr("fr"));
			m_vwRootsite.RootBox.Reconstruct();
			SelLevInfo[] levInfo = new SelLevInfo[1];
			levInfo[0] = new SelLevInfo();
			levInfo[0].ihvo = 0;
			levInfo[0].tag = StTextTags.kflidParagraphs;
			Assert.IsNotNull(m_vwRootsite.RootBox.MakeTextSelection(0, 1, levInfo,
				StTxtParaTags.kflidContents, 1, 0, 0, Cache.WritingSystemFactory.GetWsFromStr("fr"),
				false, -1, null, true));

			m_dlg.SetDialogValues(Cache, m_vwPattern, m_vwRootsite, false, false,
				null, null, null);

			m_dlg.FindText = TsStringHelper.MakeTSS("Blah!", Cache.WritingSystemFactory.GetWsFromStr("fr"));

			m_dlg.PrevPatternText = null;
			m_dlg.SimulateFindPrevButtonClick();
			Assert.IsTrue(m_dlg.FindEnvironment.FoundMatch);
			m_dlg.VerifySelection(0, 1, 2, 12, 17);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test finding a match when doing a find that isn't the initial find.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindNextWithMatch()
		{
			m_dlg.SetDialogValues(Cache, m_vwPattern, m_vwRootsite, false, false,
				null, null, null);

			m_dlg.FindText = TsStringHelper.MakeTSS("blah!", Cache.WritingSystemFactory.GetWsFromStr("fr"));

			m_dlg.PrevPatternText = null;
			m_dlg.SimulateFindButtonClick();
			Assert.IsTrue(m_dlg.FindEnvironment.FoundMatch);
			m_dlg.VerifySelection(0, 0, 0, 12, 17);
			m_dlg.SimulateFindButtonClick();
			Assert.IsTrue(m_dlg.FindEnvironment.FoundMatch);
			m_dlg.VerifySelection(0, 0, 1, 12, 17);
			m_dlg.SimulateFindButtonClick();
			Assert.IsTrue(m_dlg.FindEnvironment.FoundMatch);
			m_dlg.VerifySelection(0, 0, 2, 12, 17);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the FindNext method when doing a find that isn't the initial find. After
		/// finding some matches, the search wraps and finds no more matches.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindNextWithNoMatchAfterWrap()
		{
			IStTxtPara para = AddParaToMockedText(m_text, "Whatever");
			AddRunToMockedPara(para, "Waldo", Cache.WritingSystemFactory.GetWsFromStr("fr"));
			m_vwRootsite.RootBox.Reconstruct();
			m_vwRootsite.RootBox.MakeSimpleSel(true, true, false, true);

			m_dlg.SetDialogValues(Cache, m_vwPattern, m_vwRootsite, false, false,
				null, null, null);

			m_dlg.FindText = TsStringHelper.MakeTSS("Waldo", Cache.WritingSystemFactory.GetWsFromStr("fr"));

			m_dlg.PrevPatternText = null;
			m_dlg.SimulateFindButtonClick();
			Assert.IsTrue(m_dlg.FindEnvironment.FoundMatch);
			m_dlg.VerifySelection(0, 1, 0, 0, 5);
			m_dlg.SimulateFindButtonClick();
			Assert.IsTrue(m_dlg.FindEnvironment.FoundMatch);
			m_dlg.VerifySelection(0, 1, 1, 0, 5);
			m_dlg.SimulateFindButtonClick();
			Assert.IsTrue(m_dlg.FindEnvironment.FoundMatch);
			m_dlg.VerifySelection(0, 1, 2, 0, 5);
			m_dlg.SimulateFindButtonClick();
			Assert.IsFalse(m_dlg.FindEnvironment.FoundMatch);

			// Make sure the dialog thinks there were no more matches.
			Assert.AreEqual(FwFindReplaceDlg.MatchType.NoMoreMatchesFound,
				m_dlg.m_matchNotFoundType);

			m_dlg.VerifySelection(0, 1, 2, 0, 5); // Selection shouldn't have moved
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the FindNext method when doing a find that begins in the word we're searching
		/// for. The search should wrap and find the match but not find it again.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindNextFromWithinMatchingWord()
		{
			SelectionHelper selHelper = SelectionHelper.Create(
				m_vwRootsite.RootBox.MakeSimpleSel(true, true, false, false), m_vwRootsite);
			selHelper.IchAnchor = 3;
			selHelper.IchEnd = 3;
			selHelper.MakeBest(false);
			m_vwPattern.MatchCase = true;
			m_dlg.SetDialogValues(Cache, m_vwPattern, m_vwRootsite, false, false,
				null, null, null);

			m_dlg.FindText = TsStringHelper.MakeTSS("Blah, ", Cache.WritingSystemFactory.GetWsFromStr("fr"));
			m_dlg.PrevPatternText = null;
			m_dlg.SimulateFindButtonClick();
			Assert.IsTrue(m_dlg.FindEnvironment.FoundMatch);
			m_dlg.VerifySelection(0, 0, 1, 0, 6);
			m_dlg.SimulateFindButtonClick();
			Assert.IsTrue(m_dlg.FindEnvironment.FoundMatch);
			m_dlg.VerifySelection(0, 0, 2, 0, 6);
			m_dlg.SimulateFindButtonClick();
			Assert.IsTrue(m_dlg.FindEnvironment.FoundMatch);
			m_dlg.VerifySelection(0, 0, 0, 0, 6);
			m_dlg.SimulateFindButtonClick();
			Assert.IsFalse(m_dlg.FindEnvironment.FoundMatch);

			// Make sure the dialog thinks there were no more matches.
			Assert.AreEqual(FwFindReplaceDlg.MatchType.NoMoreMatchesFound,
				m_dlg.m_matchNotFoundType);

			m_dlg.VerifySelection(0, 0, 0, 0, 6); // Selection shouldn't have moved
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the FindNext method when doing a find when there is no selection in the view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindWithNoInitialSelection()
		{
			// This destroys the selection
			m_vwRootsite.RootBox.Reconstruct();

			m_dlg.SetDialogValues(Cache, m_vwPattern, m_vwRootsite, false, false,
				null, null, null);

			m_dlg.FindText = TsStringHelper.MakeTSS("blah!", Cache.WritingSystemFactory.GetWsFromStr("fr"));

			m_dlg.PrevPatternText = null;
			m_dlg.SimulateFindButtonClick();
			Assert.IsTrue(m_dlg.FindEnvironment.FoundMatch);
			m_dlg.VerifySelection(0, 0, 0, 12, 17);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test Find when the match contains an ORC.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Find_ORCwithinMatch()
		{
			// Add ORC within title
			AddFootnote(m_genesis, m_text[0], 3);

			// This destroys the selection
			m_vwRootsite.RootBox.Reconstruct();

			m_dlg.SetDialogValues(Cache, m_vwPattern, m_vwRootsite, false, false,
				null, null, null);
			m_dlg.FindText = TsStringHelper.MakeTSS("blah", Cache.WritingSystemFactory.GetWsFromStr("fr"));
			m_dlg.PrevPatternText = null;

			m_dlg.SimulateFindButtonClick();
			Assert.IsTrue(m_dlg.FindEnvironment.FoundMatch);
			m_dlg.VerifySelection(0, 0, 0, 0, 5);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test Find when the pattern to search for contains an ORC.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Find_ORCwithinPattern()
		{
			// This destroys the selection
			m_vwRootsite.RootBox.Reconstruct();

			m_dlg.SetDialogValues(Cache, m_vwPattern, m_vwRootsite, false, false,
				null, null, null);
			ITsString strFind = TsStringHelper.MakeTSS("blah", Cache.WritingSystemFactory.GetWsFromStr("fr"));
			ITsStrBldr strBldr = strFind.GetBldr();
			TsStringUtils.InsertOrcIntoPara(Guid.NewGuid(), FwObjDataTypes.kodtOwnNameGuidHot,
				strBldr, 2, 2, Cache.WritingSystemFactory.GetWsFromStr("fr"));
			strFind = strBldr.GetString();

			m_dlg.FindText = strFind;
			m_dlg.PrevPatternText = null;

			m_dlg.SimulateFindButtonClick();
			Assert.AreEqual("blah".ToCharArray(), m_dlg.FindText.Text.ToCharArray());
			Assert.IsTrue(m_dlg.FindEnvironment.FoundMatch);
			m_dlg.VerifySelection(0, 0, 0, 0, 4);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test Find when the pattern to search for contains an ORC.
		/// </summary>
		/// <remarks>This test needs to be modified to treat the found text like the program
		/// does. Also, see TeAppTests.cs for a test of the same name that attempts to test
		/// this issue there.</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Replace_MatchContainsORC()
		{
			m_vwRootsite.CloseRootBox();
			m_vwRootsite.DisplayType = DummyBasicViewVc.DisplayType.kNormal |
				DummyBasicViewVc.DisplayType.kMappedPara;
			m_vwRootsite.MakeRoot(m_text.Hvo, ScrBookTags.kflidTitle, 3);

			// Add ORC within title (within last "blah!")
			IStTxtPara para = m_text[0];
			ITsStrBldr strbldr = para.Contents.GetBldr();
			m_genesis.InsertFootnoteAt(0, strbldr, para.Contents.Length - 3);
			para.Contents = strbldr.GetString();

			m_vwRootsite.RefreshDisplay();

			int origFootnoteCount = m_genesis.FootnotesOS.Count;
			Assert.AreEqual(1, origFootnoteCount);

			// This destroys the selection
			m_vwRootsite.RootBox.Reconstruct();

			m_dlg.SetDialogValues(Cache, m_vwPattern, m_vwRootsite, false, false,
				null, null, null);
			m_dlg.FindText = TsStringHelper.MakeTSS("blah!", Cache.WritingSystemFactory.GetWsFromStr("fr"));
			Debug.WriteLine(m_dlg.FindText.Text);
			m_dlg.ReplaceText = TsStringHelper.MakeTSS("text", Cache.WritingSystemFactory.GetWsFromStr("fr"));
			Debug.WriteLine(m_dlg.ReplaceText.Text);
			m_dlg.PrevPatternText = null;

			m_dlg.SimulateFindButtonClick();
			Assert.IsTrue(m_dlg.FindEnvironment.FoundMatch);
			m_dlg.SimulateReplaceButtonClick();
			string expected = "Blah, blah, text" + StringUtils.kChObject;
			Assert.AreEqual(expected.ToCharArray(), para.Contents.Text.ToCharArray());

			// Confirm that the footnote was not deleted.
			////Assert.AreEqual(origFootnoteCount, m_genesis.FootnotesOS.Count);
			////m_dlg.VerifySelection(0, 0, 0, 0, 4);
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
		public void InitialFindUsingReplaceTabWithMatch()
		{
			m_dlg.SetDialogValues(Cache, m_vwPattern, m_vwRootsite, true, false,
				null, null, null);

			m_dlg.FindText = TsStringHelper.MakeTSS("Blah", Cache.WritingSystemFactory.GetWsFromStr("fr"));
			m_dlg.ReplaceText = TsStringHelper.MakeTSS("Monkey feet", Cache.WritingSystemFactory.GetWsFromStr("fr"));

			m_dlg.PrevPatternText = null;
			m_dlg.SimulateReplaceButtonClick();
			m_dlg.VerifySelection(0, 0, 0, 0, 4);
			Assert.IsTrue(m_dlg.FindEnvironment.FoundMatch);
			Assert.AreEqual(m_kTitleText, m_text[0].Contents.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test a replace when finding a match using the replace button. This simulates the
		/// "second" time the user presses Replace, where we actually do the replace and then
		/// go on to find the next match.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InitialReplaceTabWithMatch()
		{
			m_dlg.SetDialogValues(Cache, m_vwPattern, m_vwRootsite, false, false,
				null, null, null);

			m_dlg.FindText = TsStringHelper.MakeTSS("Blah", Cache.WritingSystemFactory.GetWsFromStr("fr"));
			m_dlg.ReplaceText = TsStringHelper.MakeTSS("Monkey feet", Cache.WritingSystemFactory.GetWsFromStr("fr"));

			m_dlg.PrevPatternText = null;
			m_dlg.SimulateReplaceButtonClick();
			m_dlg.SimulateReplaceButtonClick();
			m_dlg.VerifySelection(0, 0, 0, 13, 17);
			Assert.IsTrue(m_dlg.FindEnvironment.FoundMatch);
			Assert.AreEqual("Monkey feet, blah, blah!", m_text[0].Contents.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test a replace when the replace text contains multiple runs with styles.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReplaceStyles()
		{
			int ipaWs = Cache.WritingSystemFactory.GetWsFromStr("en-fonipa-x-etic");
			m_dlg.SetDialogValues(Cache, m_vwPattern, m_vwRootsite, false, false,
				null, null, null);

			m_dlg.FindText = TsStringHelper.MakeTSS("Blah", ipaWs);
			// Change the replace string in the dialog to have 2 styled runs.
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "Run 2", StyleUtils.CharStyleTextProps("CStyle1", ipaWs));
			bldr.Replace(0, 0, "Run 1", StyleUtils.CharStyleTextProps("CStyle3", ipaWs));
			m_dlg.ReplaceText = bldr.GetString();

			m_dlg.PrevPatternText = null;
			m_dlg.SimulateReplaceButtonClick();
			m_dlg.SimulateReplaceButtonClick();
			m_dlg.VerifySelection(0, 0, 0, 12, 16);
			Assert.IsTrue(m_dlg.FindEnvironment.FoundMatch);

			bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, ", blah, blah!", StyleUtils.CharStyleTextProps(null, ipaWs));
			bldr.Replace(0, 0, "Run 2", StyleUtils.CharStyleTextProps("CStyle1", ipaWs));
			bldr.Replace(0, 0, "Run 1", StyleUtils.CharStyleTextProps("CStyle3", ipaWs));
			ITsString expectedTssReplace = bldr.GetString();

			AssertEx.AreTsStringsEqual(expectedTssReplace, m_text[0].Contents);

			// the cancel button should say "close"
			Assert.AreEqual("Close", m_dlg.CloseButton.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test a Replace All matching for a character style (no text) and replacing with
		/// another character style (no text).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReplaceAllStyles()
		{
			SetupStylesheet();

			int wsFr = Cache.ServiceLocator.WritingSystemManager.GetWsFromStr("fr");
			IScrTxtPara para = Cache.ServiceLocator.GetInstance<IScrTxtParaFactory>().CreateWithStyle(
				m_text, 0, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para, "Initial text ", wsFr);
			AddRunToMockedPara(para, "replace this", "CStyle3");
			AddRunToMockedPara(para, " more text more text more text more text" +
				" more text ", wsFr);
			AddRunToMockedPara(para, "replace this", "CStyle3");
			AddRunToMockedPara(para, " more text ", wsFr);
			AddRunToMockedPara(para, "replace this", "CStyle3");
			AddRunToMockedPara(para, " last text.", wsFr);

			m_dlg.SetDialogValues(Cache, m_vwPattern, m_vwRootsite, true, false,
				null, null, null);

			// Set up the find/replace text with two different character styles.
			m_dlg.PrevPatternText = null;
			m_dlg.ApplyStyle(m_dlg.FindTextControl, "CStyle3");
			m_dlg.ReplaceTextControl.Tss = TsStringHelper.MakeTSS(string.Empty, wsFr);
			m_dlg.ApplyStyle(m_dlg.ReplaceTextControl, "CStyle2");

			m_dlg.SimulateReplaceAllButtonClick();

			// Create the expected result.
			ITsStrBldr bldrExpected = TsStrBldrClass.Create();
			bldrExpected.Replace(0, 0, " last text.", StyleUtils.CharStyleTextProps(null, wsFr));
			bldrExpected.Replace(0, 0, "replace this", StyleUtils.CharStyleTextProps("CStyle2", wsFr));
			bldrExpected.Replace(0, 0, " more text ", StyleUtils.CharStyleTextProps(null, wsFr));
			bldrExpected.Replace(0, 0, "replace this", StyleUtils.CharStyleTextProps("CStyle2", wsFr));
			bldrExpected.Replace(0, 0, " more text more text more text more text more text ",
				StyleUtils.CharStyleTextProps(null, wsFr));
			bldrExpected.Replace(0, 0, "replace this", StyleUtils.CharStyleTextProps("CStyle2", wsFr));
			bldrExpected.Replace(0, 0, "Initial text ", StyleUtils.CharStyleTextProps(null, wsFr));
			ITsString expectedTssReplace = bldrExpected.GetString();

			AssertEx.AreTsStringsEqual(expectedTssReplace, m_text[0].Contents);

			// the cancel button should say "close"
			Assert.AreEqual("Close", m_dlg.CloseButton.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test a replace when the replace text contains multiple runs with different writing
		/// systems.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReplaceWSs()
		{
			int ipaWs = Cache.WritingSystemFactory.GetWsFromStr("en-fonipa-x-etic");
			int deWs = Cache.WritingSystemFactory.GetWsFromStr("de");
			int frWs = Cache.WritingSystemFactory.GetWsFromStr("fr");
			m_dlg.SetDialogValues(Cache, m_vwPattern, m_vwRootsite, false, false,
				null, null, null);

			m_dlg.FindText = TsStringHelper.MakeTSS("Blah", ipaWs);
			// Change the replace string in the dialog to have 2 runs with different writing systems.
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "Run 2", StyleUtils.CharStyleTextProps("CStyle1", deWs));
			bldr.Replace(0, 0, "Run 1", StyleUtils.CharStyleTextProps("CStyle1", frWs));
			m_dlg.ReplaceText = bldr.GetString();

			m_dlg.MatchWsCheckboxChecked = true;
			m_dlg.PrevPatternText = null;
			m_dlg.SimulateReplaceButtonClick();
			m_dlg.SimulateReplaceButtonClick();
			m_dlg.VerifySelection(0, 0, 0, 12, 16);
			Assert.IsTrue(m_dlg.FindEnvironment.FoundMatch);

			// Create string with expected results.
			bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, ", blah, blah!", StyleUtils.CharStyleTextProps(null, ipaWs));
			bldr.Replace(0, 0, "Run 2", StyleUtils.CharStyleTextProps("CStyle1", deWs));
			bldr.Replace(0, 0, "Run 1", StyleUtils.CharStyleTextProps("CStyle1", frWs));
			ITsString expectedTssReplace = bldr.GetString();

			AssertEx.AreTsStringsEqual(expectedTssReplace, m_text[0].Contents);

			// the cancel button should say "close"
			Assert.AreEqual("Close", m_dlg.CloseButton.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test replace with an empty find text but a writing system specified (TE-1658).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("TE-1658. Needs analyst decision")]
		public void ReplaceWithMatchWs_EmptyFindText()
		{
			IStTxtPara para = m_text[0];
			ITsStrBldr bldr = para.Contents.GetBldr();
			bldr.SetIntPropValues(3, 7, (int)FwTextPropType.ktptWs,
				(int)FwTextPropVar.ktpvDefault, Cache.WritingSystemFactory.GetWsFromStr("de"));
			para.Contents = bldr.GetString();

			m_vwPattern.MatchOldWritingSystem = true;
			m_dlg.SetDialogValues(Cache, m_vwPattern, m_vwRootsite, false, false,
				null, null, null);
			Assert.IsTrue(m_dlg.MatchWsCheckboxChecked);
			m_dlg.ApplyWS(m_dlg.FindTextControl, Cache.WritingSystemFactory.GetWsFromStr("de"));

			m_dlg.FindText = TsStringHelper.MakeTSS(string.Empty, Cache.WritingSystemFactory.GetWsFromStr("de"));
			// This behavior is what is specified in TE-1658. However, there are some usability
			// issues with this. See comment on TE-1658 for details.
			Assert.IsFalse(m_dlg.ReplaceTextControl.Enabled,
				"Replace Text box should be disabled when searching for a WS without text specified");

			// Simulate setting the writing system for the replace string
			m_dlg.ReplaceText = TsStringHelper.MakeTSS(string.Empty, Cache.WritingSystemFactory.GetWsFromStr("en-fonipa-x-etic"));

			m_dlg.MatchWsCheckboxChecked = true;
			m_dlg.PrevPatternText = null;
			m_dlg.SimulateReplaceButtonClick();
			m_dlg.SimulateReplaceButtonClick();
			m_dlg.VerifySelection(0, 0, 0, 3, 7);
			Assert.IsTrue(m_dlg.FindEnvironment.FoundMatch);

			// Create string with expected results
			bldr = para.Contents.GetBldr();
			bldr.SetIntPropValues(3, 7, (int)FwTextPropType.ktptWs,
				(int)FwTextPropVar.ktpvDefault, Cache.WritingSystemFactory.GetWsFromStr("en-fonipa-x-etic"));
			ITsString expectedTssReplace = bldr.GetString();

			AssertEx.AreTsStringsEqual(expectedTssReplace, m_text[0].Contents);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test a replace all when finding a match using the replace all button. The selection
		/// is already set to a match, and there's one other match in the document.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReplaceAllWithMatch()
		{
			m_dlg.SetDialogValues(Cache, m_vwPattern, m_vwRootsite, true, false,
				null, null, null);

			m_dlg.FindText = TsStringHelper.MakeTSS("Blah", Cache.WritingSystemFactory.GetWsFromStr("fr"));
			m_dlg.ReplaceText = TsStringHelper.MakeTSS("Monkey feet", Cache.WritingSystemFactory.GetWsFromStr("fr"));

			m_dlg.PrevPatternText = null;
			m_dlg.SimulateReplaceAllButtonClick();
			m_dlg.VerifySelection(0, 0, 0, 0, 0);
			ITsString expectedTss = TsStringHelper.MakeTSS("Monkey feet, Monkey feet, Monkey feet!",
				Cache.WritingSystemFactory.GetWsFromStr("en-fonipa-x-etic"));
			AssertEx.AreTsStringsEqual(expectedTss, m_text[0].Contents);

			// the cancel button should say "close"
			Assert.AreEqual("Close", m_dlg.CloseButton.Text);
			Assert.AreEqual(FwFindReplaceDlg.MatchType.ReplaceAllFinished,
				m_dlg.m_matchNotFoundType);
			Assert.AreEqual("Finished searching the document and made 3 replacements.", m_dlg.m_matchMsg);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test a replace all when finding no matches.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReplaceAllWithNoMatch()
		{
			m_dlg.SetDialogValues(Cache, m_vwPattern, m_vwRootsite, true, false,
				null, null, null);

			m_dlg.FindText = TsStringHelper.MakeTSS("Blepharophimosis", Cache.WritingSystemFactory.GetWsFromStr("fr"));
			m_dlg.ReplaceText = TsStringHelper.MakeTSS("Monkey feet", Cache.WritingSystemFactory.GetWsFromStr("fr"));

			m_dlg.PrevPatternText = null;
			m_dlg.SimulateReplaceAllButtonClick();
			m_dlg.VerifySelection(0, 0, 0, 0, 0);
			ITsString expectedTss = TsStringHelper.MakeTSS(m_kTitleText, Cache.WritingSystemFactory.GetWsFromStr("en-fonipa-x-etic"));
			AssertEx.AreTsStringsEqual(expectedTss, m_text[0].Contents);

			// Changed for TE-4839. Button always says Close now.
			// the cancel button still should say "cancel"
			// Assert.AreEqual("Cancel", m_dlg.CloseButton.Text);
			Assert.AreEqual(FwFindReplaceDlg.MatchType.NoMatchFound, m_dlg.m_matchNotFoundType);
			Assert.AreEqual("Finished searching the document. The search item was not found.",
				m_dlg.m_matchMsg);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ability to replace an occurence of a string after a footnote.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReplaceTextAfterFootnote()
		{
			SetupStylesheet();

			m_dlg.SetDialogValues(Cache, m_vwPattern, m_vwRootsite, true, false,
				null, null, null);

			IStTxtPara para = m_text[0];
			AddFootnote(m_genesis, para, 0);
			ITsStrBldr bldr = para.Contents.GetBldr();
			bldr.Replace(4, 14, "Q", null);
			ITsString expectedTss = bldr.GetString();

			m_dlg.FindText = TsStringHelper.MakeTSS("h, blah, b", Cache.WritingSystemFactory.GetWsFromStr("fr"));
			m_dlg.ReplaceText = TsStringHelper.MakeTSS("Q", Cache.WritingSystemFactory.GetWsFromStr("fr"));

			m_dlg.PrevPatternText = null;
			m_dlg.SimulateReplaceAllButtonClick();
			m_dlg.VerifySelection(0, 0, 0, 0, 0);
			AssertEx.AreTsStringsEqual(expectedTss, para.Contents);

			// the cancel button should say "close"
			Assert.AreEqual("Close", m_dlg.CloseButton.Text);
		}
		#endregion

		#region Match style tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ability to find occurences of a character style (with no Find What text
		/// specified). This test finds no match.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindCharStyleWithNoFindText_NoMatch()
		{
			SetupStylesheet();

			m_dlg.SetDialogValues(Cache, m_vwPattern, m_vwRootsite, false, false,
				null, null, null);

			m_dlg.PrevPatternText = null;
			m_dlg.ApplyStyle(m_dlg.FindTextControl, "CStyle3");
			m_dlg.SimulateFindButtonClick();

			m_dlg.VerifySelection(0, 0, 0, 0, 0);
			Assert.IsFalse(m_dlg.FindEnvironment.FoundMatch);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ability to find occurences of a character style (with no Find What text
		/// specified).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindCharStyleWithNoFindText_Match()
		{
			SetupStylesheet();

			m_dlg.SetDialogValues(Cache, m_vwPattern, m_vwRootsite, false, false,
				null, null, null);

			IStTxtPara para = m_text[0];
			ITsStrBldr bldr = para.Contents.GetBldr();
			bldr.SetStrPropValue(6, 14, (int)FwTextPropType.ktptNamedStyle, "CStyle3");
			para.Contents = bldr.GetString();

			m_dlg.PrevPatternText = null;
			m_dlg.ApplyStyle(m_dlg.FindTextControl, "CStyle3");
			m_dlg.SimulateFindButtonClick();

			Assert.IsTrue(m_dlg.FindEnvironment.FoundMatch);
			m_dlg.VerifySelection(0, 0, 0, 6, 14);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ability to replace an occurence of a character style (with no Find What or
		/// Replace With text specified).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReplaceCharStyleWithNoFindText()
		{
			SetupStylesheet();

			IStTxtPara para = m_text[0];
			ITsStrBldr bldr = para.Contents.GetBldr();
			bldr.SetStrPropValue(6, 14, (int)FwTextPropType.ktptNamedStyle, "CStyle3");
			para.Contents = bldr.GetString();
			bldr.SetStrPropValue(6, 14, (int)FwTextPropType.ktptNamedStyle, "CStyle2");
			ITsString expectedTss = bldr.GetString();

			m_vwRootsite.RootBox.Reconstruct();
			m_vwRootsite.RootBox.MakeSimpleSel(true, true, false, true);

			m_dlg.SetDialogValues(Cache, m_vwPattern, m_vwRootsite, true, false,
				null, null, null);

			m_dlg.PrevPatternText = null;
			m_dlg.ApplyStyle(m_dlg.FindTextControl, "CStyle3");
			m_dlg.ReplaceTextControl.Tss = TsStringHelper.MakeTSS(string.Empty, Cache.WritingSystemFactory.GetWsFromStr("fr"));
			m_dlg.ApplyStyle(m_dlg.ReplaceTextControl, "CStyle2");
			m_dlg.SimulateReplaceButtonClick();
			Assert.IsTrue(m_dlg.FindEnvironment.FoundMatch);
			m_dlg.VerifySelection(0, 0, 0, 6, 14);
			m_dlg.SimulateReplaceButtonClick();
			Assert.IsFalse(m_dlg.FindEnvironment.FoundMatch);
			m_dlg.VerifySelection(0, 0, 0, 14, 14);

			AssertEx.AreTsStringsEqual(expectedTss, para.Contents);

			// the cancel button should say "close"
			Assert.AreEqual("Close", m_dlg.CloseButton.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ability to replace an occurence of a character style after a footnote
		/// (with no Find What or Replace With text specified). (TE-5323)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReplaceCharStyleAfterFootnote()
		{
			SetupStylesheet();

			m_dlg.SetDialogValues(Cache, m_vwPattern, m_vwRootsite, true, false,
				null, null, null);

			IStTxtPara para = m_text[0];
			AddFootnote(m_genesis, para, 0);
			ITsStrBldr bldr = para.Contents.GetBldr();
			bldr.SetStrPropValue(6, 14, (int)FwTextPropType.ktptNamedStyle, "CStyle3");
			para.Contents = bldr.GetString();
			bldr.SetStrPropValue(6, 14, (int)FwTextPropType.ktptNamedStyle, "CStyle2");
			ITsString expectedTss = bldr.GetString();

			m_vwRootsite.RootBox.Reconstruct();
			m_vwRootsite.RootBox.MakeSimpleSel(true, true, false, true);

			m_dlg.PrevPatternText = null;
			m_dlg.ApplyStyle(m_dlg.FindTextControl, "CStyle3");
			m_dlg.ReplaceTextControl.Tss = TsStringHelper.MakeTSS(string.Empty, Cache.WritingSystemFactory.GetWsFromStr("fr"));
			m_dlg.ApplyStyle(m_dlg.ReplaceTextControl, "CStyle2");
			m_dlg.SimulateReplaceButtonClick();
			Assert.IsTrue(m_dlg.FindEnvironment.FoundMatch);
			m_dlg.VerifySelection(0, 0, 0, 6, 14);
			m_dlg.SimulateReplaceButtonClick();
			Assert.IsFalse(m_dlg.FindEnvironment.FoundMatch);
			m_dlg.VerifySelection(0, 0, 0, 14, 14);

			AssertEx.AreTsStringsEqual(expectedTss, para.Contents);

			// the cancel button should say "close"
			Assert.AreEqual("Close", m_dlg.CloseButton.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ability to replace an occurence of a character style after a footnote
		/// (with no Find What or Replace With text specified) when a footnote immediately
		/// follows the the found range. (TE-5323)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReplaceCharStyleBetweenFootnotes()
		{
			SetupStylesheet();

			m_dlg.SetDialogValues(Cache, m_vwPattern, m_vwRootsite, true, false,
				null, null, null);

			IStTxtPara para = m_text[0];
			AddFootnote(m_genesis, para, 0); // Footnote at beginning
			AddFootnote(m_genesis, para, 14); // Footnote after text selection.
			ITsStrBldr bldr = para.Contents.GetBldr();
			bldr.SetStrPropValue(6, 14, (int)FwTextPropType.ktptNamedStyle, "CStyle3");
			para.Contents = bldr.GetString();
			bldr.SetStrPropValue(6, 14, (int)FwTextPropType.ktptNamedStyle, "CStyle2");
			ITsString expectedTss = bldr.GetString();

			m_vwRootsite.RootBox.Reconstruct();
			m_vwRootsite.RootBox.MakeSimpleSel(true, true, false, true);

			m_dlg.PrevPatternText = null;
			m_dlg.ApplyStyle(m_dlg.FindTextControl, "CStyle3");
			m_dlg.ReplaceTextControl.Tss = TsStringHelper.MakeTSS(string.Empty, Cache.WritingSystemFactory.GetWsFromStr("fr"));
			m_dlg.ApplyStyle(m_dlg.ReplaceTextControl, "CStyle2");
			m_dlg.SimulateReplaceButtonClick();
			Assert.IsTrue(m_dlg.FindEnvironment.FoundMatch);
			m_dlg.VerifySelection(0, 0, 0, 6, 14);
			m_dlg.SimulateReplaceButtonClick();
			Assert.IsFalse(m_dlg.FindEnvironment.FoundMatch);
			m_dlg.VerifySelection(0, 0, 0, 14, 14);

			AssertEx.AreTsStringsEqual(expectedTss, para.Contents);

			// the cancel button should say "close"
			Assert.AreEqual("Close", m_dlg.CloseButton.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ability to replace text with a footnote in the middle of the find text.
		/// This is possible because we ignore footnote ORCs when doing the find, but during the
		/// replace, we push the footnotes to the end of the replacement text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReplaceWhenFoundWithFootnote()
		{
			SetupStylesheet();

			IStTxtPara para = (IStTxtPara)m_text.ParagraphsOS[0];
			AddFootnote(m_genesis, para, 1); // Footnote near beginning

			m_dlg.SetDialogValues(Cache, m_vwPattern, m_vwRootsite, true, false, null,
				null, null);

			m_vwRootsite.RootBox.Reconstruct();
			m_vwRootsite.RootBox.MakeSimpleSel(true, true, false, true);

			ITsStrBldr bldr = para.Contents.GetBldr();
			bldr.Replace(2, 6, null, null); // Delete the 'lah,' after the footnote
			bldr.Replace(0, 1, "blah", null); // Add 'blah' before the footnote
			ITsString expectedTss = bldr.GetString();

			m_dlg.PrevPatternText = null;
			m_dlg.FindText = TsStringHelper.MakeTSS("blah,", m_wsFr.Handle);
			m_dlg.ReplaceText = TsStringHelper.MakeTSS("blah", m_wsFr.Handle);
			m_dlg.SimulateReplaceButtonClick();
			Assert.IsTrue(m_dlg.FindEnvironment.FoundMatch);
			m_dlg.VerifySelection(0, 0, 0, 0, 6);
			m_dlg.SimulateReplaceButtonClick();

			AssertEx.AreTsStringsEqual(expectedTss, para.Contents);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ability to replace text with a footnote in the middle of the find text when
		/// also replacing styles. (TE-8761)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReplaceWhenFoundWithFootnote_WithStyles()
		{
			SetupStylesheet();

			IStTxtPara para = (IStTxtPara)m_text.ParagraphsOS[0];
			AddFootnote(m_genesis, para, 1); // Footnote at beginning

			m_vwRootsite.RootBox.Reconstruct();
			m_vwRootsite.RootBox.MakeSimpleSel(true, true, false, true);

			m_dlg.SetDialogValues(Cache, m_vwPattern, m_vwRootsite, true, false, null,
				null, null);

			ITsStrBldr bldr = para.Contents.GetBldr();
			bldr.Replace(2, 6, null, null); // Delete the 'lah,' after the footnote
			bldr.Replace(0, 1, "blah", StyleUtils.CharStyleTextProps("CStyle2", m_wsIpa.Handle)); // Add 'blah' before the footnote
			ITsString expectedTss = bldr.GetString();

			m_dlg.PrevPatternText = null;
			m_dlg.FindText = TsStringHelper.MakeTSS("blah,", m_wsFr.Handle);
			m_dlg.ReplaceTextControl.Tss = TsStringHelper.MakeTSS("blah", m_wsFr.Handle);
			m_dlg.ApplyStyle(m_dlg.ReplaceTextControl, "CStyle2");
			m_dlg.SimulateReplaceButtonClick();
			Assert.IsTrue(m_dlg.FindEnvironment.FoundMatch);
			m_dlg.VerifySelection(0, 0, 0, 0, 6);
			m_dlg.SimulateReplaceButtonClick();

			AssertEx.AreTsStringsEqual(expectedTss, para.Contents);
		}
		#endregion

		#region Advanced options tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test an initial search when finding a string whose Writing System matches.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindWithMatchWs_NonEmptyFindText()
		{
			IStTxtPara para = m_text[0];
			ITsStrBldr bldr = para.Contents.GetBldr();
			bldr.SetIntPropValues(3, 7, (int)FwTextPropType.ktptWs,
				(int)FwTextPropVar.ktpvDefault, Cache.WritingSystemFactory.GetWsFromStr("de"));
			para.Contents = bldr.GetString();

			m_vwPattern.MatchOldWritingSystem = true;
			m_dlg.SetDialogValues(Cache, m_vwPattern, m_vwRootsite, false, false,
				null, null, null);
			Assert.IsTrue(m_dlg.MatchWsCheckboxChecked);

			m_dlg.FindText = TsStringHelper.MakeTSS(",", Cache.WritingSystemFactory.GetWsFromStr("fr"));
			m_dlg.ApplyWS(m_dlg.FindTextControl, Cache.WritingSystemFactory.GetWsFromStr("de"));

			m_dlg.PrevPatternText = null;
			m_dlg.SimulateFindButtonClick();
			Assert.IsTrue(m_dlg.FindEnvironment.FoundMatch);
			m_dlg.VerifySelection(0, 0, 0, 4, 5);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test an initial search when finding anything in a given Writing System.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindWithMatchWs_EmptyFindText()
		{
			IStTxtPara para = m_text[0];
			ITsStrBldr bldr = para.Contents.GetBldr();
			bldr.SetIntPropValues(3, 7, (int)FwTextPropType.ktptWs,
				(int)FwTextPropVar.ktpvDefault, Cache.WritingSystemFactory.GetWsFromStr("de"));
			para.Contents = bldr.GetString();

			m_vwPattern.MatchOldWritingSystem = true;
			m_dlg.SetDialogValues(Cache, m_vwPattern, m_vwRootsite, false, false,
				null, null, null);
			Assert.IsTrue(m_dlg.MatchWsCheckboxChecked);

			m_dlg.ApplyWS(m_dlg.FindTextControl, Cache.WritingSystemFactory.GetWsFromStr("de"));

			m_dlg.PrevPatternText = null;
			m_dlg.SimulateFindButtonClick();
			Assert.IsTrue(m_dlg.FindEnvironment.FoundMatch);
			m_dlg.VerifySelection(0, 0, 0, 3, 7);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test an initial search when finding a string whose diacritics match.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindWithMatchDiacritics()
		{
			IStTxtPara para = m_text[0];
			ITsStrBldr bldr = para.Contents.GetBldr();
			bldr.Replace(6, 6, "a\u0301", null);
			para.Contents = bldr.GetString();
			m_vwRootsite.RootBox.Reconstruct();
			m_vwRootsite.RootBox.MakeSimpleSel(true, true, false, true);

			m_vwPattern.MatchDiacritics = true;
			m_dlg.SetDialogValues(Cache, m_vwPattern, m_vwRootsite, false, false,
				null, null, null);
			Assert.IsTrue(m_dlg.MatchDiacriticsCheckboxChecked);

			// First, search for a base character with no diacritic. Characters in the text
			// that do have diacritics should not be found.
			m_dlg.FindText = TsStringHelper.MakeTSS("a", Cache.WritingSystemFactory.GetWsFromStr("fr"));

			m_dlg.PrevPatternText = null;
			m_dlg.SimulateFindButtonClick();
			Assert.IsTrue(m_dlg.FindEnvironment.FoundMatch);
			m_dlg.VerifySelection(0, 0, 0, 2, 3);
			m_dlg.SimulateFindButtonClick();
			Assert.IsTrue(m_dlg.FindEnvironment.FoundMatch);
			m_dlg.VerifySelection(0, 0, 0, 10, 11);

			// Next, search for a character with a diacritic. Only characters in the text
			// that have the diacritic should be found.
			m_dlg.FindText = TsStringHelper.MakeTSS("a\u0301", Cache.WritingSystemFactory.GetWsFromStr("fr"));

			m_dlg.SimulateFindButtonClick();
			Assert.IsTrue(m_dlg.FindEnvironment.FoundMatch);
			m_dlg.VerifySelection(0, 0, 1, 6, 8);
			m_dlg.SimulateFindButtonClick();
			Assert.IsTrue(m_dlg.FindEnvironment.FoundMatch);
			m_dlg.VerifySelection(0, 0, 2, 6, 8);
			m_dlg.SimulateFindButtonClick();
			Assert.IsTrue(m_dlg.FindEnvironment.FoundMatch);
			m_dlg.VerifySelection(0, 0, 0, 6, 8);

			m_dlg.SimulateFindButtonClick();
			Assert.IsFalse(m_dlg.FindEnvironment.FoundMatch);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test an initial search when finding a string which matches on a whole word.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindWithMatchWholeWord()
		{
			IStTxtPara para = m_text[0];
			ITsStrBldr bldr = para.Contents.GetBldr();
			bldr.Replace(6, 6, "more", null);
			para.Contents = bldr.GetString();
			m_vwRootsite.RootBox.Reconstruct();
			m_vwRootsite.RootBox.MakeSimpleSel(true, true, false, true);

			m_vwPattern.MatchWholeWord = true;
			m_dlg.SetDialogValues(Cache, m_vwPattern, m_vwRootsite, false, false,
				null, null, null);
			Assert.IsTrue(m_dlg.MatchWholeWordCheckboxChecked);

			m_dlg.FindText = TsStringHelper.MakeTSS("blah", Cache.WritingSystemFactory.GetWsFromStr("fr"));

			m_dlg.PrevPatternText = null;
			m_dlg.SimulateFindButtonClick();
			Assert.IsTrue(m_dlg.FindEnvironment.FoundMatch);
			m_dlg.VerifySelection(0, 0, 0, 0, 4);
			m_dlg.SimulateFindButtonClick();
			Assert.IsTrue(m_dlg.FindEnvironment.FoundMatch);
			m_dlg.VerifySelection(0, 0, 0, 16, 20);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test an initial search when finding a string whose case matches.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindWithMatchCase()
		{
			m_vwPattern.MatchCase = true;
			m_dlg.SetDialogValues(Cache, m_vwPattern, m_vwRootsite, false, false,
				null, null, null);
			Assert.IsTrue(m_dlg.MatchCaseCheckboxChecked);

			m_dlg.FindText = TsStringHelper.MakeTSS("Blah", Cache.WritingSystemFactory.GetWsFromStr("fr"));

			m_dlg.PrevPatternText = null;
			m_dlg.SimulateFindButtonClick();
			Assert.IsTrue(m_dlg.FindEnvironment.FoundMatch);
			m_dlg.VerifySelection(0, 0, 0, 0, 4);
			m_dlg.SimulateFindButtonClick();
			Assert.IsTrue(m_dlg.FindEnvironment.FoundMatch);
			m_dlg.VerifySelection(0, 0, 1, 0, 4);
		}
		#endregion
	}
	#endregion

	#region class FwFindReplaceDlgWithLiteralStringsTests
	/// -----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for Find/Replace dialog when view includes literal strings (such as labels in the
	/// TE Notes view) that are added by the view constructor.
	/// </summary>
	/// -----------------------------------------------------------------------------------------
	[TestFixture]
	public class FwFindReplaceDlgWithLiteralStringsTests : FwFindReplaceDlgBaseTests
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create the dialog
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public override void TestSetup()
		{
			base.TestSetup();

			m_vwRootsite.DisplayType = DummyBasicViewVc.DisplayType.kNormal |
				DummyBasicViewVc.DisplayType.kLiteralStringLabels;
		}

	#region Find tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test an initial search when finding a next match when the selection is in a literal
		/// string added by the VC.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindFromLiteralString()
		{
			// There is a string added by the VC at the beginning of our view.
			m_vwRootsite.RootBox.MakeSimpleSel(true, false, false, true);

			m_dlg.SetDialogValues(Cache, m_vwPattern, m_vwRootsite, false, false,
				null, null, null);

			m_dlg.FindText = TsStringHelper.MakeTSS("Blah", Cache.WritingSystemFactory.GetWsFromStr("fr"));

			m_dlg.PrevPatternText = null;
			m_dlg.SimulateFindButtonClick();
			m_dlg.VerifySelection(0, 0, 0, 0, 4);
			Assert.IsTrue(m_dlg.FindEnvironment.FoundMatch);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test when finding all matches when the selection is in a literal string added by
		/// the VC. After wrapping and getting back around to starting location, we should
		/// inform the user.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindFromLiteralString_StopWhenPassedLimit()
		{
			// There is a string added by the VC at the beginning of our view.
			m_vwRootsite.RootBox.MakeSimpleSel(true, false, false, true);

			m_dlg.SetDialogValues(Cache, m_vwPattern, m_vwRootsite, false, false,
				null, null, null);

			m_dlg.FindText = TsStringHelper.MakeTSS("Blah, blah, blah!", Cache.WritingSystemFactory.GetWsFromStr("fr"));

			m_dlg.PrevPatternText = null;
			m_dlg.SimulateFindButtonClick();
			Assert.IsTrue(m_dlg.FindEnvironment.FoundMatch);
			m_dlg.VerifySelection(0, 0, 0, 0, 17);
			m_dlg.SimulateFindButtonClick();
			Assert.IsTrue(m_dlg.FindEnvironment.FoundMatch);
			m_dlg.VerifySelection(0, 0, 1, 0, 17);
			m_dlg.SimulateFindButtonClick();
			Assert.IsTrue(m_dlg.FindEnvironment.FoundMatch);
			m_dlg.VerifySelection(0, 0, 2, 0, 17);
			m_dlg.SimulateFindButtonClick();
			Assert.IsFalse(m_dlg.FindEnvironment.FoundMatch);
			m_dlg.VerifySelection(0, 0, 2, 0, 17);
		}
		#endregion
	}
	#endregion

	#region class FwFindReplaceDlgWithObjectsRepeatedInDisplayTests
	/// -----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for Find/Replace dialog when view includes literal strings (such as labels in the
	/// TE Notes view) that are added by the view constructor.
	/// </summary>
	/// -----------------------------------------------------------------------------------------
	[TestFixture]
	public class FwFindReplaceDlgWithObjectsRepeatedInDisplayTests : ScrInMemoryFdoTestBase
	{
		#region Data members
		private const string m_kTitleText = "Blah, blah, blah!";

		private DummyFwFindReplaceDlg m_dlg;
		private DummyBasicView m_vwRootsite;
		private IVwPattern m_vwPattern;
		private IVwStylesheet m_Stylesheet;
		private IStText m_text;
		#endregion

		#region setup & teardown
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create the dialog
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public override void TestSetup()
		{
			base.TestSetup();

			m_vwPattern = VwPatternClass.Create();
			m_Stylesheet = new TestFwStylesheet();

			m_vwRootsite = new DummyBasicView();
			m_vwRootsite.StyleSheet = m_Stylesheet;
			m_vwRootsite.Cache = Cache;
			m_vwRootsite.DisplayType = DummyBasicViewVc.DisplayType.kNormal |
				DummyBasicViewVc.DisplayType.kDuplicateParagraphs;
			m_vwRootsite.MakeRoot(m_text.Hvo, ScrBookTags.kflidTitle, 3);

			m_dlg = new DummyFwFindReplaceDlg();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the test data.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			base.CreateTestData();
			m_text = AddTitleToMockedBook(AddBookToMockedScripture(1, "Genesis"),
				m_kTitleText, Cache.ServiceLocator.WritingSystemManager.UserWs);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Dispose of the dialog
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public override void TestTearDown()
		{
			base.TestTearDown();

			if (m_dlg != null)
			{
				if (m_dlg.IsHandleCreated)
					m_dlg.Close();
				m_dlg.Dispose();
				m_dlg = null;
			}

			if (m_vwRootsite != null)
			{
				m_vwRootsite.Dispose();
				m_vwRootsite = null;
			}
			if (m_vwPattern != null)
			{
				if (Marshal.IsComObject(m_vwPattern))
					Marshal.ReleaseComObject(m_vwPattern);
				m_vwPattern = null;
			}
			if (m_Stylesheet != null)
			{
				if (Marshal.IsComObject(m_Stylesheet))
					Marshal.ReleaseComObject(m_Stylesheet);
				m_Stylesheet = null;
			}
			m_text = null;
		}
		#endregion

		#region Find tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test when finding matches in a view that has objects displayed more than once
		/// (like twice).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindNextWithDuplicateParagraphs()
		{
			m_vwRootsite.RootBox.MakeSimpleSel(true, true, false, true);

			m_dlg.SetDialogValues(Cache, m_vwPattern, m_vwRootsite, false, false,
				null, null, null);

			m_dlg.FindText = TsStringHelper.MakeTSS("Blah, blah, blah!", Cache.WritingSystemFactory.GetWsFromStr("fr"));

			m_dlg.PrevPatternText = null;
			m_dlg.SimulateFindButtonClick();
			Assert.IsTrue(m_dlg.FindEnvironment.FoundMatch);
			m_dlg.VerifySelection(0, 0, 0, 0, 17);
			m_dlg.SimulateFindButtonClick();
			Assert.IsTrue(m_dlg.FindEnvironment.FoundMatch);
			m_dlg.VerifySelection(0, 0, 1, 0, 17);
			m_dlg.SimulateFindButtonClick();
			Assert.IsTrue(m_dlg.FindEnvironment.FoundMatch);
			m_dlg.VerifySelection(0, 0, 2, 0, 17);
			m_dlg.SimulateFindButtonClick();
			Assert.IsTrue(m_dlg.FindEnvironment.FoundMatch);
			m_dlg.VerifySelection(1, 0, 0, 0, 17);
			m_dlg.SimulateFindButtonClick();
			Assert.IsTrue(m_dlg.FindEnvironment.FoundMatch);
			m_dlg.VerifySelection(1, 0, 1, 0, 17);
			m_dlg.SimulateFindButtonClick();
			Assert.IsTrue(m_dlg.FindEnvironment.FoundMatch);
			m_dlg.VerifySelection(1, 0, 2, 0, 17);
			m_dlg.SimulateFindButtonClick();
			Assert.IsFalse(m_dlg.FindEnvironment.FoundMatch);
		}
		#endregion
	}
	#endregion
}
