// Copyright (c) 2013, SIL International.
// Distributable under the terms of the MIT license (http://opensource.org/licenses/MIT).
#if __MonoCS__
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using IBusDotNet;
using Palaso.UI.WindowsForms.Extensions;
using Palaso.UI.WindowsForms.Keyboarding.Interfaces;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites.Properties;
using SIL.Utils;

namespace SIL.FieldWorks.Common.RootSites
{

	/// <summary>
	/// Views code specific handler of IBus events
	/// </summary>
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification="AssociatedSimpleRootSite is a reference")]
	public class IbusRootSiteEventHandler: IIbusEventHandler
	{
		/// <summary>
		/// Initializes a new instance of the
		/// <see cref="SIL.FieldWorks.Common.RootSites.IbusRootSiteEventHandler"/> class.
		/// </summary>
		public IbusRootSiteEventHandler(SimpleRootSite simpleRootSite)
		{
			AssociatedSimpleRootSite = simpleRootSite;
		}

		private SimpleRootSite AssociatedSimpleRootSite { get; set; }

		private SelectionHelper m_InitialSelHelper;
		private SelectionHelper m_EndOfPreedit;
		private IActionHandler m_ActionHandler;
		private int m_Depth;
		private bool m_InReset;

		/// <summary>
		/// Reset the selection and optionally cancel any open compositions.
		/// </summary>
		/// <param name="cancel">Set to <c>true</c> to also cancel the open composition.</param>
		/// <returns><c>true</c> if there was an open composition that we cancelled, otherwise
		/// <c>false</c>.</returns>
		private bool Reset(bool cancel)
		{
			if (m_InReset)
				return false;

			bool retVal = false;
			m_InReset = true;
			try
			{
				if (cancel && m_ActionHandler != null)
				{
					if (m_ActionHandler.CanUndo())
						m_ActionHandler.Rollback(m_Depth);
					retVal = true;
				}
				m_ActionHandler = null;

				if (m_InitialSelHelper != null)
					m_InitialSelHelper.SetSelection(true);
				m_InitialSelHelper = null;
				m_EndOfPreedit = null;
				return retVal;
			}
			finally
			{
				m_InReset = false;
			}
		}

		/// <summary>
		/// Trim beginning backspaces, if any. Return number trimmed.
		/// </summary>
		private static int TrimBeginningBackspaces(ref string text)
		{
			const char backSpace = '\b'; // 0x0008

			if (!text.StartsWith(backSpace.ToString()))
				return 0;

			int count = text.Length - text.TrimStart(backSpace).Length;
			text = text.TrimStart(backSpace);
			return count;
		}

		/// <summary>
		/// Helper method that returns a TsString with properties read from Selection.
		/// TODO: typing next to verse numbers shouldn't preserve selection.
		/// </summary>
		private ITsString CreateTsStringUsingSelectionProps(string text, SelectionHelper selectionHelper,
			bool underLine)
		{
			IVwSelection selection = selectionHelper.Selection;
			TsStrBldr bldr = TsStrBldrClass.Create();

			ITsTextProps[] vttp;
			IVwPropertyStore[] vvps;
			int cttp;
			SelectionHelper.GetSelectionProps(selection, out vttp, out vvps, out cttp);

			// handle the unlikely event of no selection props.
			if (cttp == 0)
				return TsStringUtils.MakeTss(text, AssociatedSimpleRootSite.WritingSystemFactory.UserWs);

			bldr.ReplaceRgch(0, bldr.Length, text, text.Length, vttp[0]);
			if (underLine)
			{
				// Underline the pre-edit text
				// REVIEW: this code seems to work, but it asserts in
				// SIL.FieldWorks.FDO.DomainImpl.MultiUnicodeAccessor.set_String (The given
				// ITsString has more than one run in it). Is there a way to make it work
				// without assertion?
				bldr.SetIntPropValues(0, bldr.Length, (int)FwTextPropType.ktptUnderline,
					(int)FwTextPropVar.ktpvEnum, (int)FwUnderlineType.kuntSingle);
				bldr.SetIntPropValues(0, bldr.Length, (int)FwTextPropType.ktptUnderColor,
					(int)FwTextPropVar.ktpvDefault, (int)ColorUtil.ConvertColorToBGR(Color.Gray));
			}
			return bldr.GetString().get_NormalizedForm(FwNormalizationMode.knmNFD);
		}

		private bool SetupForTypingEventHandler(bool checkIfFocused)
		{
			if ((!AssociatedSimpleRootSite.Focused && checkIfFocused) ||
				AssociatedSimpleRootSite.RootBox == null ||
				AssociatedSimpleRootSite.RootBox.Selection == null)
			{
				return false;
			}
			if (m_ActionHandler == null)
			{
				m_ActionHandler = AssociatedSimpleRootSite.DataAccess.GetActionHandler();
				m_Depth = m_ActionHandler.CurrentDepth;
				m_ActionHandler.BeginUndoTask(Resources.ksUndoTyping, Resources.ksRedoTyping);
			}

			return true;
		}

		private void OnCommitText(string text, bool checkIfFocused)
		{
			if (!SetupForTypingEventHandler(checkIfFocused))
				return;

			try
			{
				m_ActionHandler.Rollback(m_Depth);
				m_Depth = m_ActionHandler.CurrentDepth;
				m_ActionHandler.BeginUndoTask(Resources.ksUndoTyping, Resources.ksRedoTyping);

				// Make the correct selection
				var selHelper = m_InitialSelHelper;
				if (selHelper == null)
					selHelper = new SelectionHelper(AssociatedSimpleRootSite.EditingHelper.CurrentSelection);

				var countBackspace = TrimBeginningBackspaces(ref text);
				var bottom = selHelper.GetIch(SelectionHelper.SelLimitType.Bottom);
				selHelper.IchAnchor = Math.Max(0,
					selHelper.GetIch(SelectionHelper.SelLimitType.Top) - countBackspace);
				selHelper.IchEnd = bottom;
				selHelper.SetSelection(true);

				// Insert 'text'
				AssociatedSimpleRootSite.EditingHelper.DeleteRangeIfComplex(
					AssociatedSimpleRootSite.RootBox);
				ITsString str = CreateTsStringUsingSelectionProps(text, selHelper, false);
				try
				{
					selHelper.Selection.ReplaceWithTsString(str);
				}
				catch (Exception ex)
				{
					throw;
				}
			}
			finally
			{
				m_InitialSelHelper = null;
				m_EndOfPreedit = null;
				if (m_ActionHandler != null)
					m_ActionHandler.EndUndoTask();
				m_ActionHandler = null;
			}
		}

		// When the application loses focus the user expects different behavior for different
		// ibus keyboards: for some keyboards (those that do the editing in-place and don't display
		// a selection window, e.g. "Danish - post (m17n)") the user expects that what he
		// typed remains, i.e. gets committed. Otherwise (e.g. with the Danish keyboard) it's not
		// possible to type an "a" and then click in a different field or switch applications.
		//
		// For other keyboards (e.g. Chinese Pinyin) the commit is made when the user selects
		// one of the possible characters in the pop-up window. If he clicks in a different
		// field while the pop-up window is open the composition should be deleted.
		//
		// There doesn't seem to be a way to ask an IME keyboard if it shows a pop-up window or
		// if we should commit or reset the composition. One indirect way however seems to be to
		// check the attributes: it seems that keyboards where we can/should commit set the
		// underline attribute to IBusAttrUnderline.None.
		private bool IsCommittingKeyboard { get; set; }

		private void CheckAttributesForCommittingKeyboard(IBusText text)
		{
			IsCommittingKeyboard = false;
			foreach (var attribute in text.Attributes)
			{
				var iBusUnderlineAttribute = attribute as IBusUnderlineAttribute;
				if (iBusUnderlineAttribute != null && iBusUnderlineAttribute.Underline == IBusAttrUnderline.None)
					IsCommittingKeyboard = true;
			}
		}

		#region IIbusEventHandler implementation

		/// <summary>
		/// Called by the IBusKeyboardAdapter to cancel any open compositions, e.g. after the
		/// user pressed the ESC key or if the application loses focus.
		/// </summary>
		/// <returns><c>true</c> if there was an open composition that got cancelled, otherwise
		/// <c>false</c>.</returns>
		public bool Reset()
		{
			Debug.Assert(!AssociatedSimpleRootSite.InvokeRequired,
				"Reset() should only be called on the GUI thread");
			if (!AssociatedSimpleRootSite.Focused)
				return false;

			return Reset(true);
		}

		/// <summary>
		/// Commits the or reset.
		/// </summary>
		/// <returns><c>true</c>, if or reset was commited, <c>false</c> otherwise.</returns>
		public bool CommitOrReset()
		{
			// don't check if we have focus - we won't if this gets called from OnLostFocus.
			// However, the IbusKeyboardAdapter calls this method only for the control that just
			// lost focus, so it's ok not to check :-)

			if (IsCommittingKeyboard)
			{
				if (m_InitialSelHelper != null && m_EndOfPreedit != null)
				{
					ITsString tss;
					var selection = AssociatedSimpleRootSite.RootBox.MakeRangeSelection(
						m_InitialSelHelper.SetSelection(AssociatedSimpleRootSite),
						m_EndOfPreedit.SetSelection(AssociatedSimpleRootSite), true);
					selection.GetSelectionString(out tss, string.Empty);
					OnCommitText(tss.Text, false);

					m_InitialSelHelper = null;
					m_EndOfPreedit = null;
				}
				return false;
			}

			return Reset(true);
		}

		/// <summary>
		/// This method gets called when the IBus CommitText event is raised and indicates that
		/// the composition is ending. The temporarily inserted composition string will be
		/// replaced with <paramref name="ibusText"/>.
		/// It's in the discretion of the IBus 'keyboard' to decide when it calls OnCommitText.
		/// Typically this is done when the user selects a string in the pop-up composition
		/// window, or when he types a character that isn't part of the previous composition
		/// sequence.
		/// </summary>
		public void OnCommitText(object ibusText)
		{
			if (AssociatedSimpleRootSite.InvokeRequired)
			{
				AssociatedSimpleRootSite.BeginInvoke(() => OnCommitText(ibusText));
				return;
			}

			OnCommitText(((IBusText)ibusText).Text, true);
		}

		/// <summary>
		/// Called when the IBus UpdatePreeditText event is raised to update the composition.
		/// </summary>
		/// <param name="obj">New composition string that will replace the existing
		/// composition (sub-)string.</param>
		/// <param name="cursorPos">1-based index in the composition (pre-edit window). The
		/// composition string will be replaced with <paramref name="obj"/> starting
		/// at this position.</param>
		public void OnUpdatePreeditText(object obj, int cursorPos)
		{
			if (AssociatedSimpleRootSite.InvokeRequired)
			{
				AssociatedSimpleRootSite.BeginInvoke(() => OnUpdatePreeditText(obj, cursorPos));
				return;
			}

			if (!SetupForTypingEventHandler(true))
				return;

			var ibusText = obj as IBusText;
			var compositionText = ibusText.Text;
			CheckAttributesForCommittingKeyboard(ibusText);

			if (m_InitialSelHelper == null)
			{
				m_InitialSelHelper = new SelectionHelper(
					AssociatedSimpleRootSite.EditingHelper.CurrentSelection);
			}

			if (cursorPos > 0)
			{
				// make cursorPos 0-based
				cursorPos--;
			}

			// Make the correct selection
			var selHelper = new SelectionHelper(
				AssociatedSimpleRootSite.EditingHelper.CurrentSelection);
			if (m_EndOfPreedit != null)
				selHelper = m_EndOfPreedit;

			// Replace any previous pre-edit text after cursorPos. selHelper points to
			// the position after inserting the previous pre-edit text, so it will be the
			// end of our range selection. The bottom of m_InitialSelHelper is position at
			// the end of the initial range selection, so it will be part of the anchor.
			selHelper.IchEnd = selHelper.GetIch(SelectionHelper.SelLimitType.Bottom);
			selHelper.IchAnchor = m_InitialSelHelper.GetIch(SelectionHelper.SelLimitType.Bottom)
				+ cursorPos;
			selHelper.SetSelection(true);

			// Update the pre-edit text
			ITsString str = CreateTsStringUsingSelectionProps(compositionText, selHelper, true);
			selHelper.Selection.ReplaceWithTsString(str);

			m_EndOfPreedit = new SelectionHelper(
				AssociatedSimpleRootSite.EditingHelper.CurrentSelection);

			if (m_InitialSelHelper.IsRange)
			{
				// keep the range selection
				selHelper = m_InitialSelHelper;
			}
			else
				selHelper = m_EndOfPreedit;

			// make the selection visible
			selHelper.SetSelection(true);
		}

		/// <summary>
		/// Called when the IBus DeleteSurroundingText is raised to delete surrounding
		/// characters.
		/// </summary>
		/// <param name="offset">The character offset from the cursor position of the text to be
		/// deleted. A negative value indicates a position before the cursor.</param>
		/// <param name="nChars">The number of characters to be deleted.</param>
		public void OnDeleteSurroundingText(int offset, int nChars)
		{
			if (AssociatedSimpleRootSite.InvokeRequired)
			{
				AssociatedSimpleRootSite.BeginInvoke(() => OnDeleteSurroundingText(offset, nChars));
				return;
			}

			if (!SetupForTypingEventHandler(true) || nChars <= 0)
				return;

			try
			{
				var selHelper = new SelectionHelper(AssociatedSimpleRootSite.EditingHelper.CurrentSelection);

				var selectionStart = selHelper.GetIch(SelectionHelper.SelLimitType.Top);
				var startIndex = selectionStart + offset;
				if (startIndex + nChars <= 0)
					return;

				startIndex = Math.Max(startIndex, 0);
				selHelper.IchAnchor = startIndex;
				selHelper.IchEnd = startIndex + nChars;
				selHelper.SetSelection(true);

				ITsString str = CreateTsStringUsingSelectionProps(string.Empty, selHelper, true);
				selHelper.Selection.ReplaceWithTsString(str);

				if (startIndex < selectionStart)
					selectionStart = Math.Max(selectionStart - nChars, 0);

				selHelper.IchAnchor = selectionStart;
				selHelper.IchEnd = selectionStart;

				// make the selection visible
				selHelper.SetSelection(true);
			}
			finally
			{
				if (m_ActionHandler != null)
					m_ActionHandler.EndUndoTask();
				m_ActionHandler = null;
			}
		}

		/// <summary>
		/// Called when the IBus HidePreeditText event is raised to cancel/remove the composition,
		/// e.g. after the user pressed the ESC key or the application lost focus.
		/// </summary>
		public void OnHidePreeditText()
		{
			if (AssociatedSimpleRootSite.InvokeRequired)
			{
				AssociatedSimpleRootSite.BeginInvoke(OnHidePreeditText);
				return;
			}

			if (!AssociatedSimpleRootSite.Focused)
				return;

			Reset(true);
		}

		/// <summary>
		/// Called when the IBus ForwardKeyEvent is raised, i.e. when IBus wants the application
		/// to process a key event. One example is when IBus raises the ForwardKeyEvent and
		/// passes backspace to delete the character to the left of the cursor so that it can be
		/// replaced with a new modified character.
		/// </summary>
		/// <param name="keySym">Key symbol.</param>
		/// <param name="scanCode">Scan code.</param>
		/// <param name="index">Index of the KeyCode vector. This more or less tells the state of
		/// the modifier keys: 0=unshifted, 1=shifted (the other values I don't know and are irrelevant).
		/// </param>
		public void OnIbusKeyPress(int keySym, int scanCode, int index)
		{
			if (AssociatedSimpleRootSite.InvokeRequired)
			{
				AssociatedSimpleRootSite.BeginInvoke(() => OnIbusKeyPress(keySym, scanCode, index));
				return;
			}
			if (!AssociatedSimpleRootSite.Focused)
				return;

			var inChar = (char)(0x00FF & keySym);
			OnCommitText(inChar.ToString(), true);
		}

		/// <summary>
		/// Called by the IBusKeyboardAdapter to get the position (in pixels) and line height of
		/// the end of the selection. The position is relative to the screen in the
		/// PointToScreen sense, that is (0,0) is the top left of the primary monitor.
		/// IBus will use this information when it opens a pop-up window to present a list of
		/// composition choices.
		/// </summary>
		public Rectangle SelectionLocationAndHeight
		{
			get
			{
				Debug.Assert(!AssociatedSimpleRootSite.InvokeRequired,
					"SelectionLocationAndHeight should only be called on the GUI thread");

				var selHelper = AssociatedSimpleRootSite.EditingHelper.CurrentSelection;
				if (selHelper == null)
					return new Rectangle();

				var location = selHelper.GetLocation();
				AssociatedSimpleRootSite.ClientToScreen(AssociatedSimpleRootSite.RootBox, ref location);
				// TODO: get line height from selection or style instead of hardcoding a value!
				return new Rectangle(location.X, location.Y, 0, 20);
			}
		}
		#endregion

	}
}
#endif
