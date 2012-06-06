using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites.Properties;
using SIL.Utils;

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary>
	/// Represents a key sent to ibus.
	/// </summary>
	internal struct IBusKey
	{
		// otherwise known as keysym.
		public uint keyval;
		// this is a scan code.
		public uint keycode;
		// contains which modifiyer keys are pressed.
		public uint state;
	}

		/// <summary>
	/// Helper class to convert winform keypress information into the form needed by ibus.
		/// </summary>
	internal class KeysConverter
	{
		// Converts WM_CHAR lParam + ModifyerKeys to an IBusKey.
		public static IBusKey Convert(uint lParam, Keys modifyerKeys)
		{
			// lparm & 0x0f == repeat count
			// (lParam & 0xFF0000) >> 16 ==  scan code
			// (lParam & 0xFF0000) >> 16 == extended keys
			// (lParam & 20000000) >> 29 == is Alt

			IBusKey ret = new IBusKey();
			uint scancode = (lParam & 0xFF0000) >> 16;
			bool shifted = ((modifyerKeys & Keys.CapsLock) == 0 && (modifyerKeys & Keys.Shift) != 0 ||
				((modifyerKeys & Keys.CapsLock) != 0 && (modifyerKeys & Keys.Shift) == 0));
			ret.keyval = KeycodeToKeysym(scancode, shifted);
			ret.keycode = scancode;
			ret.state = ModifyerKeysToState(modifyerKeys);

			return ret;
		}

		[DllImport ("libX11", EntryPoint="XOpenDisplay")]
		private static extern IntPtr XOpenDisplay(IntPtr displayName);

		[DllImport ("libX11", EntryPoint="XKeycodeToKeysym")]
		private static extern uint XKeycodeToKeysym (IntPtr display, uint keycode, uint index);

		[DllImport ("libX11", EntryPoint="XCloseDisplay")]
		private static extern uint XCloseDisplay (IntPtr display);

		private class KeyCodeStore
		{
			public uint KeyCode { get; private set; }
			public bool Shifted { get; private set; }

			private KeyCodeStore()
			{
				KeyCode = 0;
				Shifted = false;
			}

			public KeyCodeStore(uint keyCode, bool shifted)
			{
				KeyCode = keyCode;
				Shifted = shifted;
			}
		}

		private static Dictionary<KeyCodeStore, uint> m_KeyCodeStore = new Dictionary<KeyCodeStore, uint>();

		static uint KeycodeToKeysym(uint keycode, bool shifted)
		{
			uint keysym = 0;
			var key = new KeyCodeStore(keycode, shifted);
			if (!m_KeyCodeStore.TryGetValue(key, out keysym))
			{
				IntPtr display = IntPtr.Zero;
				try
				{
					display = XOpenDisplay(IntPtr.Zero);
					if (display != IntPtr.Zero)
					{
						// index of XKeycodeToKeysym, value 0 for lower case, 1 for upper case.
						keysym = XKeycodeToKeysym(display, keycode, (uint)(shifted ? 1 : 0));
						m_KeyCodeStore.Add(key, keysym);
					}
					else
						Console.WriteLine("KeysConverter.KeycodeToKeysym: XOpenDisplay failed!");
				}
				finally
				{
					if (display != IntPtr.Zero)
						XCloseDisplay(display);
				}
			}
			return keysym;
		}

		/// <summary>
		/// Note: mono's Modifyer Keys don't seem to contain Keys.LWin and Keys.RWIN (windows key)
		/// </summary>
		static uint ModifyerKeysToState(Keys modifyerKeys)
		{
			const uint numlock = 0x10;
			const uint shift = 0x1;
			const uint capslock = 0x2;
			const uint control = 0x4;
			const uint alt = 0x8;
			const uint windowsKey = 0x40;

			uint returnState = 0x0;

			if ((modifyerKeys & Keys.Shift) != 0)
				returnState |= shift;
			if ((modifyerKeys & Keys.Control) != 0)
				returnState |= control;
			if ((modifyerKeys & Keys.Alt) != 0)
				returnState |= alt;
			if ((modifyerKeys & Keys.CapsLock) != 0)
				returnState |= capslock;

			return returnState;
		}
	}

	/// <summary>
	/// Linux only class is responsible for controlling SimpleRootSites interaction with ibus.
	/// </summary>
	public class InputBusController : IDisposable
	{
		#region protected member variables and properties

		/// <summary>
		/// The IBusCommunicator instance connected to the AssociatedSimpleRootSite.
		/// </summary>
		public IIBusCommunicator IBusCommunicator { get; internal set;}

		/// <summary>
		/// Store the current preedit selection
		/// </summary>
		protected SelectionHelper m_savedPreeditSelection;

		/// <summary>
		/// Associated SimpleRootSite which this class is handling the ibus interaction for
		/// </summary>
		protected SimpleRootSite AssociatedSimpleRootSite
		{
			get;
			set;
		}

		/// <summary>
		/// Stores if the underlying Data is being changed by an external source.
		/// </summary>
		protected bool m_dataChanging;

		/// <summary>
		/// Some IME send an initial blank preedit, after focusing on a input context
		/// This can cause a control with an inital selection to be deleted.
		/// </summary>
		protected bool m_ignoreInitialPreedit;

		#endregion

		#region public methods

		/// <summary>
		/// Creates a new instance of InputBusController for a supplied instance of a SimpleRootSite.
		/// </summary>
		public InputBusController(SimpleRootSite simpleRootSite, IIBusCommunicator ibusCommunicator)
		{
			if (simpleRootSite == null)
				throw new ArgumentException("simpleRootSite");

			if (ibusCommunicator == null)
				throw new ArgumentException("ibusCommunicator");

			IBusCommunicator = ibusCommunicator;
			if (!IBusCommunicator.Connected)
				return;

			AssociatedSimpleRootSite = simpleRootSite;

			IBusCommunicator.CreateInputContext("SimpleRootSite");
			IBusCommunicator.CommitText += CommitTextEventHandler;
			IBusCommunicator.UpdatePreeditText += UpdatePreeditTextEventHandler;
			IBusCommunicator.HidePreeditText += HidePreeditTextEventHandler;
			IBusCommunicator.ForwardKeyEvent += HandleIBusCommunicatorForwardKeyEvent;
		}

		#region IDisposable Members
#if DEBUG
		/// <summary/>
		~InputBusController()
		{
			Dispose(false);
		}
#endif
		/// <summary>
		/// Dispose the connection
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary/>
		protected virtual void Dispose(bool fDisposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!fDisposing,
				"****************** Missing Dispose() call for " + GetType() + "******************");
			if (fDisposing)
			{
				if (IBusCommunicator != null)
				{
					IBusCommunicator.CommitText -= CommitTextEventHandler;
					IBusCommunicator.UpdatePreeditText -= UpdatePreeditTextEventHandler;
					IBusCommunicator.HidePreeditText -= HidePreeditTextEventHandler;
					IBusCommunicator.ForwardKeyEvent -= HandleIBusCommunicatorForwardKeyEvent;
					IBusCommunicator.Dispose();
				}
			}
			IBusCommunicator = null;
		}
		#endregion

		/// <summary>
		/// Updates the location where the preedit box will be shown.
		/// </summary>
		void SetImeCursorLocation()
		{
			var rootSite = AssociatedSimpleRootSite;
			IVwGraphics graphics;
			Rect rcSrcRoot, rcDstRoot;
			rootSite.GetGraphics(rootSite.RootBox, out graphics, out rcSrcRoot, out rcDstRoot);
			try
			{
				// we have to set location of the cursor so that a preedit box shows up correctly.
				if (rootSite.RootBox != null)
				{
					var sel = rootSite.RootBox.Selection;
					if (sel != null)
					{
						Rect rcPrimary;
						Rect rcSecondary;
						bool fSplit;
						bool fEndBeforeAnchor;

						sel.Location(graphics, rcSrcRoot, rcDstRoot,
							out rcPrimary, out rcSecondary, out fSplit,
							out fEndBeforeAnchor);
						var rectScreen = rootSite.RectangleToScreen(rcPrimary);
						IBusCommunicator.SetCursorLocation(rectScreen.Left, rectScreen.Top,
							0, rectScreen.Height);
					}
				}
			}
			finally
			{
				rootSite.ReleaseGraphics(rootSite.RootBox, graphics);
			}
		}

		/// <summary>
		/// Focus the input context
		/// </summary>
		public void Focus()
		{
			if (!IBusCommunicator.Connected)
				return;

			m_ignoreInitialPreedit = true;

			IBusCommunicator.FocusIn();
			SetImeCursorLocation();
		}

		/// <summary>
		/// Unfocus the input context
		/// </summary>
		public void KillFocus()
		{
			if (!IBusCommunicator.Connected)
				return;

			IBusCommunicator.FocusOut();
		}

		/// <summary>
		/// Inform input bus of Keydown events
		/// This is useful to get warning of key that should stop the preedit
		/// </summary>
		/// <returns>
		/// true if we handled keydown
		/// false if keydown needs handling by someone else
		/// </returns>
		public bool NotifyKeyDown(Message msg, Keys modifierKeys)
		{
			if (!IBusCommunicator.Connected)
				return false;

			m_ignoreInitialPreedit = false;

			var key = (Keys)msg.WParam.ToInt32();
			switch (key)
			{
			case Keys.Escape:
				// These should end a preedit, so wait until that has happened
				// before allowing the key to be proceessed.
				ResetAndWaitForCommit();
				return false;
			case Keys.Up:
			case Keys.Down:
			case Keys.Left:
			case Keys.Right:
			case Keys.Delete:
				// pass cursor keys to ibus
				return NotifyKeyPress((uint)key, (uint)msg.LParam, modifierKeys);
			case Keys.Back:
				// we'll get a WM_CHAR for this and have ibus handle it then
				return false;
			}
			// pass function keys onto ibus since they don't appear (on mono at least) as WM_SYSCHAR
			if (key >= Keys.F1 && key <= Keys.F24)
				return NotifyKeyPress((uint)key, (uint)msg.LParam, modifierKeys);
			return false;
		}

		/// <summary>
		/// Handle actual key presses.
		/// </summary>
		/// <returns>
		/// true if we handled keypress
		/// false if keypress needs handling by someone else
		/// </returns>
		public bool NotifyKeyPress(uint charCodeUtf16, uint lParam, Keys modifierKeys)
		{
			if (!IBusCommunicator.Connected)
				return false;

			m_ignoreInitialPreedit = false;

			// reset saved preedit selection that we might have set in NotifyMouseClick().
			m_savedPreeditSelection = null;

			// modifierKeys doesn't contains CapsLock and
			// mono Control.IsKeyLocked(Keys.CapsLock) doesn't work on mono.
			// so we guess the caps state by unicode value and the shift state
			// this is far from ideal.
			if (char.IsUpper((char)charCodeUtf16) && (modifierKeys & Keys.Shift) == 0)
				modifierKeys |= Keys.CapsLock;
			else if (char.IsLower((char)charCodeUtf16) && (modifierKeys & Keys.Shift) != 0)
				modifierKeys |= Keys.CapsLock;

			IBusKey key = KeysConverter.Convert(lParam, modifierKeys);

			if (IBusCommunicator.ProcessKeyEvent(key.keyval, key.keycode, key.state))
				return true;

			// if ProcessKeyEvent doesn't consume the key
			// we need to kill any preedits and
			// sync before continuing processing the keypress
			ResetAndWaitForCommit();

			return false;
		}

		/// <summary>
		/// Inform InputBusController that the user has performed a mouse click on the SimpleRootSite
		/// </summary>
		public void NotifyMouseClick()
		{
			if (!IBusCommunicator.Connected)
				return;

			// save the current selection so that we can use it when we commit the pre-edit text
			m_savedPreeditSelection = AssociatedSimpleRootSite.EditingHelper.CurrentSelection;

			ResetAndWaitForCommit();
		}

		/// <summary>
		/// Notify the InputBusController that the underlying data of SimpleRootSite is about to
		/// change.
		/// </summary>
		public void NotifyDataChanging()
		{
			if (!IBusCommunicator.Connected)
				return;

			m_dataChanging = true;
		}

		/// <summary>
		/// Notify the InputBusController that the underlying data of SimpleRootSite has changed.
		/// This is currently used to notify Undo/Redo notificaitons.
		/// At the very lest we need to clear the ibus preedit buffer, when this happens.
		/// There may be other state to reset as well.
		/// </summary>
		public void NotifyDataChanged()
		{
			if (!IBusCommunicator.Connected)
				return;

			m_dataChanging = false;
			ResetAndWaitForCommit();
		}

		/// <summary>
		/// Synchronize on a commit.
		/// </summary>
		protected void ResetAndWaitForCommit()
		{
			IBusCommunicator.Reset();

			// This should allow any generated commits to be handled by the message pump.
			// TODO: find a better way to synchronize
			Application.DoEvents();
		}

		#endregion

		#region protected helper methods

		/// <summary>
		/// Saves the current preedit selection to be restored with the RestorePreditSelection()
		/// method.
		/// </summary>
		protected SelectionHelper SavePreeditSelection()
		{
			return SelectionHelper.Create(AssociatedSimpleRootSite);
		}

		/// <summary>
		/// Restores the selection stored in selHelper.
		/// </summary>
		protected void RestorePreeditSelection(SelectionHelper selHelper)
		{
			if (selHelper == null || AssociatedSimpleRootSite.RootBox.Height <= 0)
				return;

			if (AssociatedSimpleRootSite.ReadOnlyView)
			{
				// if we are a read-only view, then we can't make a writable selection
				try
				{
					AssociatedSimpleRootSite.RootBox.MakeSimpleSel(true, false, false, true);
				}
				catch
				{
					// Just ignore any errors - don't get a selection but who cares.
				}
				return;
			}

			bool selectionRestored = (selHelper.SetSelection(AssociatedSimpleRootSite, true, false) != null);

			if (!selectionRestored)
			{
				try
				{
					// Any selection is better than no selection...
					AssociatedSimpleRootSite.RootBox.MakeSimpleSel(true, true, false, true);
				}
				catch
				{
					// Just ignore any errors - don't get a selection but who cares.
				}
			}
		}

		/// <summary>
		/// Helper method that returns a TsString with properties read from Selection.
		/// TODO: typing next to verse numbers shouldn't preserve selection.
		/// </summary>
		ITsString CreateTsStringUsingSelectionProps(string text, SelectionHelper selectionHelper)
		{
			IVwSelection selection = selectionHelper.Selection;
			TsStrBldr bld = TsStrBldrClass.Create();

			ITsTextProps[] vttp;
			IVwPropertyStore[] vvps;
			int cttp;
			SelectionHelper.GetSelectionProps(selection, out vttp, out vvps, out cttp);

			// handle the unlikely event of no selection props.
			if (cttp == 0)
				return StringUtils.MakeTss(text, AssociatedSimpleRootSite.WritingSystemFactory.UserWs)
					.get_NormalizedForm(FwNormalizationMode.knmNFD);

			bld.ReplaceRgch(0, bld.Length, text, text.Length, vttp[0]);
			return bld.GetString().get_NormalizedForm(FwNormalizationMode.knmNFD);
		}

		#endregion

		#region Handle ibus signals

		delegate void ShowPreeditDelegate(string text, bool visible);

		delegate void CommitDelegate(string text);

		delegate void ForwardKeyDelegate(uint keyval, uint keycode, uint modifiers);

		/// <summary>
		/// Handle the HidePreeditText event transfering to GUI thread if neccessary.
		/// </summary>
		private void HidePreeditTextEventHandler()
		{
			if (AssociatedSimpleRootSite.InvokeRequired)
			{
				AssociatedSimpleRootSite.SafeBeginInvoke(new ShowPreeditDelegate(ShowPreedit),
					new object[] { String.Empty, false });
				return;
			}

			ShowPreedit(String.Empty, false);
		}

		private void HandleIBusCommunicatorForwardKeyEvent(uint keyval, uint keycode, uint modifiers)
		{
			// Handle the event by transfering to GUI thread if neccessary.
			if (AssociatedSimpleRootSite.InvokeRequired)
			{
				AssociatedSimpleRootSite.SafeBeginInvoke(new ForwardKeyDelegate(
					HandleIBusCommunicatorForwardKeyEvent), new object[] { keyval, keycode, modifiers });
				return;
			}

			char inChar = (char)(0x00FF & keyval);

			CommitTextEventHandler(inChar.ToString());
		}

		/// <summary>
		/// Handle the UpdatePreeditText event transfering to GUI thread if neccessary.
		/// </summary>
		private void UpdatePreeditTextEventHandler(string text, uint cursor_pos, bool visible)
		{
			if (AssociatedSimpleRootSite.InvokeRequired)
			{
				AssociatedSimpleRootSite.SafeBeginInvoke(
					new ShowPreeditDelegate(ShowPreedit), new object[] { text, visible });
				return;
			}

			ShowPreedit(text, visible);
		}

		/// <summary>
		/// This method must be called by the GUI thread only.
		/// It shows the current Preedit as a selection.
		/// </summary>
		private void ShowPreedit(string text, bool visible)
		{
			// Don't allow modifying the preedit if an undo or redo is in progress.
			if (m_dataChanging)
				return;

			if (AssociatedSimpleRootSite.InvokeRequired)
				throw new ApplicationException(
					"Programming Error: ShowPreedit should only be called on GUI thread");

			if (AssociatedSimpleRootSite.RootBox == null)
				return;

			if (AssociatedSimpleRootSite.RootBox.Selection == null)
				return;

			// Some IME send a initial empty preedit, which can clear an existing selection
			// TODO: we could also check the selection is a range if this is neccessary
			if (string.IsNullOrEmpty(text) && m_ignoreInitialPreedit)
			{
				return;
			}

			m_ignoreInitialPreedit = false;

			IActionHandler actionHandler = AssociatedSimpleRootSite.DataAccess.GetActionHandler();

			try
			{
				if (actionHandler != null)
					actionHandler.BeginUndoTask(Resources.ksUndoTyping, Resources.ksRedoTyping);

				AssociatedSimpleRootSite.EditingHelper.DeleteRangeIfComplex(
					AssociatedSimpleRootSite.RootBox);

				// update the current preedit selection with new preedit text.
				var selHelper = new SelectionHelper(
					AssociatedSimpleRootSite.EditingHelper.CurrentSelection);
				ITsString str = CreateTsStringUsingSelectionProps(text, selHelper);
				selHelper.Selection.ReplaceWithTsString(str);

				// make the selection fit the new text
				selHelper.SetIch(SelectionHelper.SelLimitType.Anchor,
					selHelper.GetIch(SelectionHelper.SelLimitType.Top));
				selHelper.SetIch(SelectionHelper.SelLimitType.End,
					selHelper.IchAnchor + str.Length);

				// make the selection visible
				selHelper.SetSelection(true);
			}
			finally
			{
				if (actionHandler != null)
					actionHandler.EndUndoTask();
			}

			// Update the position where a preedit window will appear
			SetImeCursorLocation();
		}

		/// <summary>
		/// Handle the CommitText event. Transfer to GUI thread if neccessary.
		/// </summary>
		private void CommitTextEventHandler(string text)
		{
			if (AssociatedSimpleRootSite.InvokeRequired)
			{
				AssociatedSimpleRootSite.SafeBeginInvoke(
					new CommitDelegate(CommitTextEventHandler), new object[] { text });
				return;
			}

			IActionHandler actionHandler = AssociatedSimpleRootSite.DataAccess.GetActionHandler();

			try
			{
				if (actionHandler != null)
					actionHandler.BeginUndoTask(Resources.ksUndoTyping, Resources.ksRedoTyping);

				// Save existing Preedit Selection and existing left-over preedit string.
				var preeditSelection = SavePreeditSelection();
				ITsString preedit;
				preeditSelection.Selection.GetSelectionString(out preedit, String.Empty);

				// Change selection to a insertion point (unless we moved the selection before,
				// which happens when we come here as part of processing a mouse click)
				// And insert commit text.
				var selHelper = new SelectionHelper(
					m_savedPreeditSelection ?? AssociatedSimpleRootSite.EditingHelper.CurrentSelection);
				if (m_savedPreeditSelection == null)
					selHelper.ReduceToIp(SelectionHelper.SelLimitType.Anchor);

				selHelper.SetSelection(true);
				AssociatedSimpleRootSite.EditingHelper.OnCharAux(text, VwShiftStatus.kfssNone,
					Keys.None);

				int deletedChars = TrimBeginningBackspaces(ref text);

				// Update the saved preedit selection to take account of the inserted text
				// text is in NFC, but the view uses it in NFD, so we have to convert it.
				// We don't do this if we moved the selection prior to this method.
				int textLenNFD = m_savedPreeditSelection != null ? 0 :
					Icu.Normalize(text, Icu.UNormalizationMode.UNORM_NFD).Length;
				int anchor = preeditSelection.IchAnchor + textLenNFD - deletedChars;

				// select the text we just inserted
				// TODO: underline the text so that it is more obvious that this is just preedit text
				preeditSelection.SetIch(SelectionHelper.SelLimitType.Anchor, anchor);
				preeditSelection.SetIch(SelectionHelper.SelLimitType.End,
					preeditSelection.IchEnd + textLenNFD - deletedChars);

				// reshow the preedit selection
				RestorePreeditSelection(preeditSelection);
				preeditSelection.Selection.ReplaceWithTsString(preedit);
				preeditSelection.SetSelection(true);
			}
			finally
			{
				m_savedPreeditSelection = null;
				if (actionHandler != null)
					actionHandler.EndUndoTask();
			}
		}

		/// <summary>
		/// Trim beginning backspaces, if any. Return number trimmed.
		/// </summary>
		private int TrimBeginningBackspaces(ref string text)
		{
			const char BackSpace = '\b'; // 0x0008

			if (!text.StartsWith(BackSpace.ToString()))
				return 0;

			int count = text.Length - text.TrimStart(BackSpace).Length;
			text = text.TrimStart(BackSpace);
			return count;
		}
		#endregion
	}
}
