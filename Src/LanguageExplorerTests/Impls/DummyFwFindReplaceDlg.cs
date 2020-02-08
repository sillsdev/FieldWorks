// Copyright (c) 2003-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using LanguageExplorer;
using LanguageExplorer.Impls;
using NUnit.Framework;
using RootSite.TestUtilities;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FwCoreDlgs.Controls;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorerTests.Impls
{
	/// <summary>
	/// Dummy find dialog
	/// </summary>
	internal class DummyFwFindReplaceDlg : FwFindReplaceDlg
	{
		/// <summary />
		internal MatchType m_matchNotFoundType = MatchType.NotSet;
		/// <summary />
		public string m_matchMsg = string.Empty;
		/// <summary><c>true</c> if the "Invalid RegEx" message box was displayed</summary>
		public bool m_fInvalidRegExDisplayed;

		/// <summary />
		public DummyFwFindReplaceDlg()
		{
			MatchNotFound += OnMatchNotFound;
		}

		/// <summary>
		/// Displays a message box that the regular expression is invalid.
		/// </summary>
		/// <param name="errorMessage">The error message.</param>
		protected override void DisplayInvalidRegExMessage(string errorMessage)
		{
			m_fInvalidRegExDisplayed = true;
		}

		/// <summary />
		private bool OnMatchNotFound(object sender, string defaultMsg, MatchType matchType)
		{
			m_matchMsg = defaultMsg;
			m_matchNotFoundType = matchType;
			return false;
		}

		/// <summary>
		/// Exposes the m_prevSearchText member variable.
		/// </summary>
		public ITsString PrevPatternText
		{
			get
			{
				return m_prevSearchText;
			}
			set
			{
				m_prevSearchText = value;
			}
		}

		/// <summary>
		/// Exposes the Find Text box on the Find/Replace dialog
		/// </summary>
		public FwTextBox FindTextControl => fweditFindText;

		/// <summary>
		/// Exposes the Find Format label on the Find/Replace dialog
		/// </summary>
		public Label FindFormatLabel => lblFindFormat;

		/// <summary>
		/// Exposes the Find Format Text label on the Find/Replace dialog
		/// </summary>
		public Label FindFormatTextLabel => lblFindFormatText;

		/// <summary>
		/// Exposes the Replace Text box on the Find/Replace dialog
		/// </summary>
		public FwTextBox ReplaceTextControl => fweditReplaceText;

		/// <summary>
		/// Exposes the Replace Format label on the Find/Replace dialog
		/// </summary>
		public Label FindReplaceLabel => lblReplaceFormat;

		/// <summary>
		/// Exposes the Find Replace Text label on the Find/Replace dialog
		/// </summary>
		public Label FindReplaceTextLabel => lblReplaceFormatText;

		/// <summary>
		/// Exposes the find collector environment.
		/// </summary>
		internal FindCollectorEnv FindEnvironment => m_findEnvironment;

		/// <summary>
		/// Gets whether or not the "Match Diacritics" checkbox on the Find/Replace dialog is
		/// checked.
		/// </summary>
		public bool MatchDiacriticsCheckboxChecked => chkMatchDiacritics.Checked;

		/// <summary>
		/// Gets whether or not the "Match Writing System" checkbox on the Find/Replace dialog
		/// is checked.
		/// </summary>
		public bool MatchWsCheckboxChecked
		{
			get
			{
				return chkMatchWS.Checked;
			}
			set
			{
				chkMatchWS.Checked = value;
			}
		}

		/// <summary>
		/// Gets whether or not the "Match Case" checkbox on the Find/Replace dialog is
		/// checked.
		/// </summary>
		public bool MatchCaseCheckboxChecked => chkMatchCase.Checked;

		/// <summary>
		/// Gets whether or not the "Match Whole Word" checkbox on the Find/Replace dialog is
		/// checked.
		/// </summary>
		public bool MatchWholeWordCheckboxChecked => chkMatchWholeWord.Checked;

		/// <summary>
		/// Gets whether or not the "Use Regular Expressions" checkbox on the Find/Replace
		/// dialog is checked.
		/// </summary>
		public bool UseRegExCheckboxChecked
		{
			get
			{
				return chkUseRegularExpressions.Checked;
			}
			set
			{
				chkUseRegularExpressions.Checked = value;
			}
		}

		/// <summary>
		/// Gets whether or not the "More Controls" panel on the Find/Replace dialog is visible.
		/// </summary>
		public bool MoreControlsPanelVisible => panelSearchOptions.Visible;

		/// <summary />
		public void SimulateFindPrevButtonClick()
		{
			FindPrevious();
		}

		/// <summary />
		public void SimulateFindButtonClick()
		{
			m_fInvalidRegExDisplayed = false;
			OnFindNext(null, new EventArgs());
		}

		/// <summary />
		public void SimulateReplaceButtonClick()
		{
			DoReplace(CurrentSelection);
			m_cache.ServiceLocator.GetInstance<IActionHandler>().BreakUndoTask("blah", "blah");
		}

		/// <summary />
		public void SimulateReplaceAllButtonClick()
		{
			DoReplaceAll();
		}

		/// <summary />
		public Button CloseButton => btnClose;

		/// <summary>
		/// Exposes the style menu
		/// </summary>
		public MenuItem StyleMenu => mnuStyle;

		/// <summary>
		/// Exposes the ApplyStyle method
		/// </summary>
		/// <param name="fwTextBox">The Tss edit control whose selection should have the
		/// specified style applied to it.</param>
		/// <param name="sStyle">The name of the style to apply</param>
		public override void ApplyStyle(FwTextBox fwTextBox, string sStyle)
		{
			base.ApplyStyle(fwTextBox, sStyle);
			Focus();
		}

		/// <summary>
		/// Exposes the writing system menu
		/// </summary>
		public MenuItem WritingSystemMenu => mnuWritingSystem;

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
		public void VerifySelection(int iInstancePara, int iPara, int iInstanceString, int ichAnchor, int ichEnd)
		{
			var helper = ((DummyBasicView)m_vwRootsite).EditingHelper.CurrentSelection;
			var selLevels = helper.GetLevelInfo(SelLimitType.Anchor);
			Assert.AreEqual(1, selLevels.Length);
			Assert.AreEqual(iPara, selLevels[0].ihvo);
			Assert.AreEqual(14001, selLevels[0].tag);
			Assert.AreEqual(iInstancePara, selLevels[0].cpropPrevious);

			selLevels = helper.GetLevelInfo(SelLimitType.End);
			Assert.AreEqual(1, selLevels.Length);
			Assert.AreEqual(iPara, selLevels[0].ihvo);
			Assert.AreEqual(14001, selLevels[0].tag);
			Assert.AreEqual(iInstancePara, selLevels[0].cpropPrevious);

			Assert.AreEqual(ichAnchor, helper.IchAnchor);
			Assert.AreEqual(ichEnd, helper.IchEnd);
			Assert.AreEqual(16002, helper.GetTextPropId(SelLimitType.Anchor));
			Assert.AreEqual(16002, helper.GetTextPropId(SelLimitType.End));
			Assert.AreEqual(iInstanceString, helper.GetNumberOfPreviousProps(SelLimitType.Anchor));
			Assert.AreEqual(iInstanceString, helper.GetNumberOfPreviousProps(SelLimitType.End));
		}
	}
}