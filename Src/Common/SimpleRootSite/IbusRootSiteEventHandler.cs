// Copyright (c) 2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

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

		private class SelectionWrapper
		{
			private readonly ITsTextProps[] m_TextProps;

			[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
				Justification = "SimpleRootSite.EditingHelper is a reference")]
			public SelectionWrapper(SimpleRootSite rootSite)
			{
				SelectionHelper = new SelectionHelper(rootSite.EditingHelper.CurrentSelection);

				ITsTextProps[] textProps;
				IVwPropertyStore[] propertyStores;
				int numberOfProps;
				SelectionHelper.GetSelectionProps(SelectionHelper.Selection,
					out textProps, out propertyStores, out numberOfProps);
				if (numberOfProps > 0)
					m_TextProps = textProps;
			}

			public SelectionHelper SelectionHelper { get; private set;}

			public IVwSelection RestoreSelection()
			{
				if (SelectionHelper == null)
					return null;

				var selection = SelectionHelper.SetSelection(true);
				if (selection != null && m_TextProps != null)
					selection.SetSelectionProps(m_TextProps.Length, m_TextProps);
				return selection;
			}
		}

		private SelectionWrapper m_InitialSelection;
		private SelectionHelper m_EndOfPreedit;
		private IActionHandler m_ActionHandler;
		private int m_Depth;
		private bool m_InReset;

		/// <summary>
		/// Preedit event handler.
		/// </summary>
		public delegate void PreeditEventHandler(object sender, EventArgs e);

		/// <summary>
		/// Occurs when the preedit started.
		/// </summary>
		public event PreeditEventHandler PreeditOpened;

		/// <summary>
		/// Occurs when the preedit gets closed.
		/// </summary>
		public event PreeditEventHandler PreeditClosed;

		private void OnPreeditOpened()
		{
			if (PreeditOpened != null)
				PreeditOpened(this, EventArgs.Empty);
		}

		private void OnPreeditClosed()
		{
			if (PreeditClosed != null)
				PreeditClosed(this, EventArgs.Empty);
		}

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
				if (cancel)
				{
					if (m_ActionHandler != null && m_ActionHandler.get_TasksSinceMark(true))
					{
						m_ActionHandler.Rollback(m_Depth);
						retVal = true;
					}
					else if (m_InitialSelection != null && m_EndOfPreedit != null)
					{
						var selHelper = SetupForTypingEventHandler(false, true);
						if (selHelper != null)
						{
							// Update selection so that we can delete the preedit-text
							UpdateSelectionForReplacingPreeditText(selHelper, 0);
							selHelper.SetSelection(true);

							if (selHelper.IsValid && selHelper.IsRange)
							{
								var tss = CreateTsStringUsingSelectionProps(string.Empty, null, false);
								selHelper.Selection.ReplaceWithTsString(tss);
							}
						}
					}
				}
				m_ActionHandler = null;

				if (m_InitialSelection != null)
					m_InitialSelection.RestoreSelection();

				m_InitialSelection = null;
				m_EndOfPreedit = null;
				return retVal;
			}
			finally
			{
				m_InReset = false;
				OnPreeditClosed();
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

		private static ITsTextProps[] GetSelectionProps(SelectionHelper selectionHelper)
		{
			IVwSelection selection = selectionHelper.Selection;
			ITsTextProps[] vttp;
			IVwPropertyStore[] vvps;
			int cttp;
			SelectionHelper.GetSelectionProps(selection, out vttp, out vvps, out cttp);

			// ENHANCE: We probably should call a method similar to VwTextSel::CleanPropertiesForTyping
			// to get rid of any unwanted properties.

			return vttp;
		}

		/// <summary>
		/// Helper method that returns a TsString with properties read from Selection.
		/// TODO: typing next to verse numbers shouldn't preserve selection.
		/// </summary>
		private ITsString CreateTsStringUsingSelectionProps(string text, ITsTextProps[] selectionProps,
			bool underLine)
		{
			// handle the unlikely event of no selection props.
			if (selectionProps == null || selectionProps.Length == 0)
				return TsStringUtils.MakeTss(text, AssociatedSimpleRootSite.WritingSystemFactory.UserWs);

			var textProps = selectionProps[0];
			var propsBuilder = TsPropsBldrClass.Create();
			var colorGray = (int)ColorUtil.ConvertColorToBGR(Color.Gray);
			for (int i = 0; i < textProps.IntPropCount; i++)
			{
				int type, variation;
				var value = textProps.GetIntProp(i, out type, out variation);

				if (!underLine)
				{
					if (type == (int)FwTextPropType.ktptUnderline && value == (int)FwUnderlineType.kuntSingle ||
						type == (int)FwTextPropType.ktptUnderColor && value == colorGray)
					{
						// ignore
						continue;
					}
				}
				propsBuilder.SetIntPropValues(type, variation, value);
			}
			if (underLine)
			{
				// REVIEW: this code seems to work, but it asserts in
				// SIL.FieldWorks.FDO.DomainImpl.MultiUnicodeAccessor.set_String (The given
				// ITsString has more than one run in it). Is there a way to make it work
				// without assertion?
				propsBuilder.SetIntPropValues((int)FwTextPropType.ktptUnderline,
					(int)FwTextPropVar.ktpvEnum, (int)FwUnderlineType.kuntSingle);
				propsBuilder.SetIntPropValues((int)FwTextPropType.ktptUnderColor,
					(int)FwTextPropVar.ktpvDefault, colorGray);
			}

			var tssFactory = TsStrFactoryClass.Create();
			return tssFactory.MakeStringWithPropsRgch(text, text.Length,
				propsBuilder.GetTextProps()).get_NormalizedForm(FwNormalizationMode.knmNFD);
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "AssociatedSimpleRootSite.EditingHelper returns a reference")]
		private SelectionHelper SetupForTypingEventHandler(bool checkIfFocused,
			bool rollBackPreviousTask)
		{
			if (AssociatedSimpleRootSite.ReadOnlyView ||
				AssociatedSimpleRootSite.RootBox == null ||
				AssociatedSimpleRootSite.RootBox.Selection == null ||
				(checkIfFocused && (!AssociatedSimpleRootSite.Focused ||
					(AssociatedSimpleRootSite.TopLevelControl != null && !AssociatedSimpleRootSite.TopLevelControl.Enabled))))
			{
				return null;
			}
			var selHelper = new SelectionHelper(AssociatedSimpleRootSite.EditingHelper.CurrentSelection);
			if (m_ActionHandler == null)
			{
				m_ActionHandler = AssociatedSimpleRootSite.DataAccess.GetActionHandler();
			}
			else if (rollBackPreviousTask)
			{
				if (m_ActionHandler.get_TasksSinceMark(true))
				{
					m_ActionHandler.Rollback(m_Depth);
					selHelper = new SelectionHelper(m_InitialSelection.SelectionHelper);
				}
				else if (m_InitialSelection.SelectionHelper.IsRange)
					return selHelper;
				else
					return new SelectionHelper(m_InitialSelection.SelectionHelper);
			}
			else
				return selHelper;

			if (m_ActionHandler != null)
			{
				m_Depth = m_ActionHandler.CurrentDepth;
				m_ActionHandler.BeginUndoTask(Resources.ksUndoTyping, Resources.ksRedoTyping);
			}
			return selHelper;
		}

		private void OnCommitText(string text, bool checkIfFocused)
		{
			var selHelper = SetupForTypingEventHandler(checkIfFocused, true);
			if (selHelper == null)
				return;
			try
			{
				var selectionProps = GetSelectionProps(selHelper);

				var countBackspace = TrimBeginningBackspaces(ref text);
				var bottom = selHelper.GetIch(SelectionHelper.SelLimitType.Bottom);
				selHelper.IchAnchor = Math.Max(0,
					selHelper.GetIch(SelectionHelper.SelLimitType.Top) - countBackspace);
				selHelper.IchEnd = bottom;
				selHelper.SetSelection(true);

				UpdateSelectionForReplacingPreeditText(selHelper, countBackspace);

				// Insert 'text'
				ITsString str = CreateTsStringUsingSelectionProps(text, selectionProps, false);
				selHelper.Selection.ReplaceWithTsString(str);
			}
			finally
			{
				m_InitialSelection = null;
				m_EndOfPreedit = null;
				if (m_ActionHandler != null)
				{
					m_ActionHandler.EndUndoTask();
					m_ActionHandler = null;
				}
				OnPreeditClosed();
			}
		}

		private void UpdateSelectionForReplacingPreeditText(SelectionHelper selHelper, int countBackspace)
		{
			if ((m_ActionHandler == null || !m_ActionHandler.get_TasksSinceMark(true))
				&& m_InitialSelection != null && m_EndOfPreedit != null)
			{
				// we don't have an action handler (or we have nothing to rollback) which means
				// we didn't roll back the preedit. This means we have to create a range selection
				// that deletes the preedit.
				var bottom = selHelper.GetIch(SelectionHelper.SelLimitType.Bottom);
				selHelper.IchAnchor = Math.Max(0,
					m_InitialSelection.SelectionHelper.GetIch(SelectionHelper.SelLimitType.Top) - countBackspace);
				selHelper.IchEnd = Math.Max(bottom,
					m_EndOfPreedit.GetIch(SelectionHelper.SelLimitType.Bottom));
				selHelper.SetSelection(true);
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
				if (m_InitialSelection != null && m_EndOfPreedit != null)
				{
					ITsString tss;
					var selection = AssociatedSimpleRootSite.RootBox.MakeRangeSelection(
						m_InitialSelection.RestoreSelection(),
						m_EndOfPreedit.SetSelection(AssociatedSimpleRootSite), true);
					selection.GetSelectionString(out tss, string.Empty);
					OnCommitText(tss.Text, false);

					m_InitialSelection = null;
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
		/// <param name="cursorPos">0-based position where the cursor should be put after
		/// updating the composition (pre-edit window). This position is relative to the
		/// composition/preedit text.</param>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "SimpleRootSite.EditingHelper is a reference")]
		public void OnUpdatePreeditText(object obj, int cursorPos)
		{
			if (AssociatedSimpleRootSite.InvokeRequired)
			{
				AssociatedSimpleRootSite.BeginInvoke(() => OnUpdatePreeditText(obj, cursorPos));
				return;
			}

			var selHelper = SetupForTypingEventHandler(true, false);
			if (selHelper == null)
				return;

			if (m_InitialSelection == null)
			{
				// Create a new, independent selection helper for m_InitialSelHelper - we want
				// to remember the current selection
				m_InitialSelection = new SelectionWrapper(AssociatedSimpleRootSite);
				OnPreeditOpened();
			}

			var ibusText = obj as IBusText;
			var compositionText = ibusText.Text;
			CheckAttributesForCommittingKeyboard(ibusText);

			// Make the correct selection
			if (m_EndOfPreedit != null)
				selHelper = m_EndOfPreedit;

			var selectionProps = GetSelectionProps(selHelper);

			// Replace any previous pre-edit text.
			if (m_InitialSelection.SelectionHelper.Selection.EndBeforeAnchor)
			{
				// If we have a backwards selection we insert the pre-edit before the selected
				// text. selHelper points to the originally selected text (which got moved because
				// we inserted the pre-edit before it). The top of m_InitialSelection is the
				// start of the pre-edit, the top of selHelper is the position before the
				// originally selected text
				Debug.Assert(m_InitialSelection.SelectionHelper.IsRange);
				selHelper.IchEnd = selHelper.GetIch(SelectionHelper.SelLimitType.End);
				selHelper.IchAnchor = m_InitialSelection.SelectionHelper.GetIch(SelectionHelper.SelLimitType.Top);
			}
			else
			{
				// selHelper points to the position after inserting the previous pre-edit text,
				// so it will be the end of our range selection. The bottom of m_InitialSelection
				// is the position at the end of the initial range selection, so it will be part
				// of the anchor.
				selHelper.IchEnd = selHelper.GetIch(SelectionHelper.SelLimitType.Bottom);
				selHelper.IchAnchor = m_InitialSelection.SelectionHelper.GetIch(SelectionHelper.SelLimitType.Bottom);
			}
			selHelper.SetSelection(true);

			// Update the pre-edit text
			ITsString str = CreateTsStringUsingSelectionProps(compositionText, selectionProps, true);
			selHelper.Selection.ReplaceWithTsString(str);

			m_EndOfPreedit = new SelectionHelper(
				AssociatedSimpleRootSite.EditingHelper.CurrentSelection);

			if (m_InitialSelection.SelectionHelper.IsRange)
			{
				// keep the range selection
				if (m_InitialSelection.SelectionHelper.Selection.EndBeforeAnchor)
				{
					// we inserted before the original selection but we want to keep the original
					// text selected, so we have to adjust
					selHelper = new SelectionHelper(m_InitialSelection.SelectionHelper);
					selHelper.IchAnchor += str.Length;
					selHelper.IchEnd += str.Length;
				}
				else
					selHelper = m_InitialSelection.SelectionHelper;
			}
			else
			{
				// Set the IP to the position IBus told us. This is tricky because compositionText
				// might be in NFC but we have converted it to NFD, so the position needs to
				// change. To simplify this we expect for now that IBus sets the cursor always
				// either at the start or the end of the composition string.
				selHelper = new SelectionHelper(AssociatedSimpleRootSite.EditingHelper.CurrentSelection);
				selHelper.IchAnchor = m_InitialSelection.SelectionHelper.GetIch(SelectionHelper.SelLimitType.Bottom);
				if (compositionText.Length == cursorPos)
					selHelper.IchAnchor += str.Length;
				else
				{
					Debug.Assert(cursorPos == 0,
						"IBus told us a cursor position that changed because of nfc->nfd normalization");
					selHelper.IchAnchor += cursorPos;
				}
				selHelper.IchEnd = selHelper.IchAnchor;
			}

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

			var selHelper = SetupForTypingEventHandler(true, false);
			if (selHelper == null || nChars <= 0)
				return;

			try
			{
				var selectionStart = selHelper.GetIch(SelectionHelper.SelLimitType.Top);
				var startIndex = selectionStart + offset;
				if (startIndex + nChars <= 0)
					return;

				var selectionProps = GetSelectionProps(selHelper);

				startIndex = Math.Max(startIndex, 0);
				selHelper.IchAnchor = startIndex;
				selHelper.IchEnd = startIndex + nChars;
				selHelper.SetSelection(true);

				ITsString str = CreateTsStringUsingSelectionProps(string.Empty, selectionProps, true);
				selHelper.Selection.ReplaceWithTsString(str);

				if (startIndex < selectionStart)
					selectionStart = Math.Max(selectionStart - nChars, 0);

				selHelper.IchAnchor = selectionStart;
				selHelper.IchEnd = selectionStart;

				// make the selection visible
				var selection = selHelper.SetSelection(true);
				selection.SetSelectionProps(selectionProps.Length, selectionProps);
			}
			finally
			{
				if (m_ActionHandler != null)
				{
					m_ActionHandler.EndUndoTask();
					m_ActionHandler = null;
				}
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

			if (!AssociatedSimpleRootSite.Focused || AssociatedSimpleRootSite.ReadOnlyView)
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
			if (!AssociatedSimpleRootSite.Focused || AssociatedSimpleRootSite.ReadOnlyView)
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
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "SimpleRootSite.EditingHelper is a reference")]
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
				var lineHeight = AssociatedSimpleRootSite.LineHeight;
				return new Rectangle(location.X, location.Y, 0, lineHeight);
			}
		}

		/// <summary>
		/// Called by the IbusKeyboardAdapter to find out if a preedit is active.
		/// </summary>
		public bool IsPreeditActive
		{
			get { return m_InitialSelection != null; }
		}

		#endregion
	}
}
#endif
