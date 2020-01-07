// Copyright (c) 2002-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites.Properties;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.Keyboarding;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Utils;
using SIL.PlatformUtilities;
using SIL.Reporting;

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary>
	/// This class encapsulates some of the common behavior of SimpleRootSite and
	/// PublicationControl that has to do with forwarding keyboard events to the
	/// root box that has focus.
	/// </summary>
	public class EditingHelper : IDisposable, ISelectionChangeNotifier
	{
		#region Events
		/// <summary>
		/// Event handler for specialized work when the selection changes.
		/// </summary>
		public event EventHandler<VwSelectionArgs> VwSelectionChanged;

		#endregion

		#region Member variables
		/// <summary>Object that provides editing callback methods (in production code, this is usually (always?) the rootsite)</summary>
		protected IEditingCallbacks m_callbacks;
		/// <summary>The default cursor to use</summary>
		private Cursor m_defaultCursor;

		/// <summary>A SelectionHelper that holds the info for the current selection (updated
		/// every time the selection changes) Protected to allow for testing - production
		/// subclasses should not access this member directly</summary>
		protected SelectionHelper m_currentSelection;

		/// <summary>Event for changing properties of a pasted TsString</summary>
		public event FwPasteFixTssEventHandler PasteFixTssEvent;

		private int _lastWritingSystemProcessed = int.MinValue;

		/// <summary>Flag to prevent reentrancy while setting keyboard.</summary>
		private bool m_fSettingKeyboards;
		#endregion

		/// <summary>
		/// This constructor is for testing so the class can be mocked.
		/// </summary>
		public EditingHelper() : this(null)
		{
		}

		/// <summary />
		public EditingHelper(IEditingCallbacks callbacks)
		{
			m_callbacks = callbacks;
			Control = callbacks as UserControl;
		}

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		protected bool IsDisposed { get; set; }

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~EditingHelper()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <inheritdoc />
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SuppressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

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
		protected virtual void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + " ******************");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				// Dispose managed resources here.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			Control = null;
			m_callbacks = null;
			m_currentSelection = null;

			IsDisposed = true;
		}

		#region Writing system methods
		/// <summary>
		/// Get in the vector the list of writing system identifiers currently installed in the
		/// writing system factory for the current root box. The current writing system for the
		/// selection is duplicated as the first item in the array (this causes it to be found
		/// first in searches).
		/// </summary>
		public List<int> GetWsList(out ILgWritingSystemFactory wsf)
		{
			// Get the writing system factory associated with the root box.
			wsf = WritingSystemFactory;
			var cws = wsf.NumberOfWs;
			if (cws == 0)
			{
				return null;
			}
			using (var ptr = MarshalEx.ArrayToNative<int>(cws))
			{
				wsf.GetWritingSystems(ptr, cws);
				var vwsT = MarshalEx.NativeToArray<int>(ptr, cws);
				if (cws == 1 && vwsT[0] == 0)
				{
					return null;    // no writing systems to work with
				}
				return new List<int>(vwsT);
			}
		}

		/// <summary>
		/// Set the writing system of the current selection.
		/// </summary>
		public void ApplyWritingSystem(int hvoWsNew)
		{
			if (Callbacks?.EditedRootBox == null)
			{
				return;
			}
			var vwsel = Callbacks.EditedRootBox.Selection;
			ITsTextProps[] vttp;
			IVwPropertyStore[] vvps;
			int cttp;
			SelectionHelper.GetSelectionProps(vwsel, out vttp, out vvps, out cttp);
			var fChanged = false;
			for (var ittp = 0; ittp < cttp; ++ittp)
			{
				int hvoWsOld, var;
				var ttp = vttp[ittp];
				// Change the writing system only if it is different and not a user prompt.
				hvoWsOld = ttp.GetIntPropValues((int)FwTextPropType.ktptWs, out var);
				if (ttp.GetIntPropValues(SimpleRootSite.ktptUserPrompt, out var) == -1 && hvoWsOld != hvoWsNew)
				{
					var tpb = ttp.GetBldr();
					tpb.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, hvoWsNew);
					vttp[ittp] = tpb.GetTextProps();
					fChanged = true;
				}
				else
				{
					vttp[ittp] = null;
				}
			}
			if (fChanged)
			{
				ChangeWritingSystem(vwsel, vttp, cttp);
				HandleSelectionChange(Callbacks.EditedRootBox, vwsel);
			}
		}

		/// <summary>
		/// Changes the writing system.
		/// </summary>
		/// <param name="sel">The selection.</param>
		/// <param name="props">The properties specifying the new writing system.</param>
		/// <param name="numProps">The number of ITsTextProps.</param>
		protected virtual void ChangeWritingSystem(IVwSelection sel, ITsTextProps[] props, int numProps)
		{
			Debug.Assert(sel != null);
			Debug.Assert(props != null);

			sel.SetSelectionProps(numProps, props);
		}

		#endregion

		#region Character processing methods
		/// <summary>
		/// Handle a WM_CHAR message.
		/// Caller should ensure this is wrapped in a UOW (typically done in an override of
		/// OnKeyPress in RootSiteEditingHelper, since SimpleRootSite does not have access
		/// to LCM and UOW).
		/// </summary>
		public virtual void OnKeyPress(KeyPressEventArgs e, Keys modifiers)
		{
			if (!IsIgnoredKey(e, modifiers) && CanEdit()) // Only process keys that aren't ignored
			{
				HandleKeyPress(e.KeyChar, modifiers);
			}
		}

		/// <summary>
		/// User pressed a key.
		/// </summary>
		/// <returns><c>true</c> if we handled the key, <c>false</c> otherwise (e.g. we're
		/// already at the end of the rootbox and the user pressed down arrow key).</returns>
		public virtual bool OnKeyDown(KeyEventArgs e)
		{
			if (Callbacks?.EditedRootBox == null)
			{
				return true;
			}
			var fRet = true;
			switch (e.KeyCode)
			{
				case Keys.PageUp:
				case Keys.PageDown:
				case Keys.End:
				case Keys.Home:
				case Keys.Left:
				case Keys.Up:
				case Keys.Right:
				case Keys.Down:
				case Keys.F7: // the only two function keys currently known to the Views code,
				case Keys.F8: // used for left and right arrow by string character amounts.
				case Keys.Enter:
					var ss = GetShiftStatus(e.Modifiers);
					if (e.KeyCode == Keys.Enter && (ss == VwShiftStatus.kfssShift || !CanEdit()))
					{
						return fRet;
					}
					var keyVal = e.KeyValue;
					if (Control is SimpleRootSite)
					{
						keyVal = ((SimpleRootSite)Control).ConvertKeyValue(keyVal);
					}
					fRet = CallOnExtendedKey(keyVal, ss);

					// REVIEW (EberhardB): I'm not sure if it's generally valid
					// to call ScrollSelectionIntoView from HandleKeyDown
					HandleKeyDown(e, ss);

					// The properties of the selection may be changed by pressing these
					// navigation keys even if the selection does not move (e.g. TE-7098
					// when the right arrow key is pressed after a chapter number when
					// there is no text following the chapter number).
					ClearCurrentSelection();
					break;

				case Keys.Delete:
					if (!CanEdit())
					{
						return fRet;
					}
					// The Microsoft world apparently doesn't know that <DEL> is an ASCII
					// character just as  much as <BS>, so TranslateMessage generates a
					// WM_CHAR message for <BS>, but not for <DEL>! I think the reason for this
					// probably has to do with the ability to use Del as a menu command shortcut.
					OnKeyPress(new KeyPressEventArgs((char)(int)VwSpecialChars.kscDelForward), e.Modifiers);
					break;

				case Keys.Space:
					if (CanEdit() && (e.Modifiers & Keys.Control) == Keys.Control)
					{
						e.Handled = true;
						RemoveCharFormatting();
					}
					break;

				case Keys.F10:
					if (GetShiftStatus(e.Modifiers) == VwShiftStatus.kfssShift)
					{
						Callbacks.ShowContextMenuAtIp(Callbacks.EditedRootBox);
					}
					break;

				case Keys.Apps:
					// Handle the user pressing the context menu key (i.e. the Apps. key).
					// we display the context menu here manually so that it shows
					// at the right location. If we rely on .NET it doesn't display
					// it at the IP location.
					Callbacks.ShowContextMenuAtIp(Callbacks.EditedRootBox);
					break;

				case Keys.Tab:
					ss = GetShiftStatus(e.Modifiers);
					keyVal = e.KeyValue;
					if (Control is SimpleRootSite)
					{
						keyVal = ((SimpleRootSite)Control).ConvertKeyValue(keyVal);
					}
					fRet = CallOnExtendedKey(keyVal, ss);

					// REVIEW (EberhardB): I'm not sure if it's generally valid
					// to call ScrollSelectionIntoView from HandleKeyDown
					HandleKeyDown(e, ss);
					break;

				default:
					break;
			}

			return fRet;
		}

		/// <summary>
		/// Checks input characters to see if they should be processsed. Static to allow
		/// function to be shared with PublicationControl.
		/// </summary>
		/// <param name="e"></param>
		/// <param name="modifiers">Control.ModifierKeys</param>
		/// <returns><code>true</code> if character should be ignored on input</returns>
		public static bool IsIgnoredKey(KeyPressEventArgs e, Keys modifiers)
		{
			var ignoredKey = false;
			if ((modifiers & Keys.Alt) == Keys.Alt)
			{
				// For some languages, Alt is commonly used for keyboard input.  See LT-4182.
			}
			else if ((modifiers & Keys.Control) == Keys.Control)
			{
				// control-backspace, control-forward delete and control-M (same as return
				// key) will be passed on for processing
				ignoredKey = !(e.KeyChar == (int)VwSpecialChars.kscBackspace ||
					e.KeyChar == (int)VwSpecialChars.kscDelForward ||
					e.KeyChar == '\r');
			}
			// Ignore control characters (most can only be generated using control key, see above; but Escape otherwise gets through...)
			// One day we might want to allow tab, though I don't think it comes through this method anyway...
			return e.KeyChar < 0x20 && e.KeyChar != '\r' && e.KeyChar != '\b' || ignoredKey;
		}

		/// <summary>
		/// Handle a key press.
		/// Caller should ensure this is wrapped in a UOW (typically done in an override of
		/// OnKeyPress in RootSiteEditingHelper, since SimpleRootSite does not have access
		/// to LCM and UOW).
		/// </summary>
		/// <param name="keyChar">The pressed character key</param>
		/// <param name="modifiers">key modifies - shift status, etc.</param>
		public void HandleKeyPress(char keyChar, Keys modifiers)
		{
			// REVIEW (EberhardB): .NETs Unicode character type is 16bit, whereas AppCore used
			// 32bit (int), so how do we handle this?
			if (Callbacks != null && Callbacks.EditedRootBox != null)
			{
				var ss = GetShiftStatus(modifiers);
				var buffer = new StringBuilder();

				CollectTypedInput(keyChar, buffer);

				OnCharAux(buffer.ToString(), ss, modifiers);
			}
		}

		/// <summary>
		/// Returns the ShiftStatus that shows if Ctrl and/or Shift keys were pressed
		/// </summary>
		/// <param name="keys">The key state</param>
		/// <returns>The shift status</returns>
		public static VwShiftStatus GetShiftStatus(Keys keys)
		{
			// Test whether the Ctrl and/or Shift keys are also being pressed.
			var ss = VwShiftStatus.kfssNone;
			if ((keys & Keys.Shift) == Keys.Shift)
			{
				ss = VwShiftStatus.kfssShift;
			}
			if ((keys & Keys.Control) == Keys.Control)
			{
				ss = ss != VwShiftStatus.kfssNone ? VwShiftStatus.kgrfssShiftControl : VwShiftStatus.kfssControl;
			}
			return ss;
		}

		/// <summary>
		/// Allows subclass to be more selective about combining multiple keystrokes into one event.
		/// Contract: may always return true if buffer is empty.
		/// Must return false if the buffer is not empty and the next WM_CHAR is delete or return.
		/// </summary>
		/// <param name="nextChar">The next char that will be processed</param>
		public virtual bool KeepCollectingInput(int nextChar)
		{
			return nextChar >= ' ' && nextChar != (int)VwSpecialChars.kscDelForward;
		}

		/// <summary>
		/// Collect whatever keyboard input is available--whatever the user has typed ahead.
		/// Includes backspaces and delete forwards, but not any more special keys like arrow keys.
		/// </summary>
		/// <param name="chsFirst">the first character the user typed, which started the whole
		/// process.</param>
		/// <param name="buffer">output is accumulated here (starting with chsFirst, unless
		/// it gets deleted by a subsequent backspace).</param>
		protected void CollectTypedInput(char chsFirst, StringBuilder buffer)
		{
			bool needToVerifySurrogates = char.IsSurrogate(chsFirst);
			// The first character goes into the buffer
			buffer.Append(chsFirst);
			if (Platform.IsMono)
			{
				return;
			}
			// Note: When/if porting to MONO, the following block of code can be removed
			// and still work.
			if (chsFirst < ' ' || chsFirst == (char)VwSpecialChars.kscDelForward)
			{
				return;
			}
			if (Control == null)
			{
				return;
			}
			// We need to disable type-ahead when using a Keyman keyboard since it can
			// mess with the keyboard functionality. (FWR-2205)
			var activeKbIsKeyMan = false;
			if (Keyboard.Controller != null && Keyboard.Controller.ActiveKeyboard != null)
			{
				activeKbIsKeyMan = Keyboard.Controller.ActiveKeyboard.Format == KeyboardFormat.Keyman
				                   || Keyboard.Controller.ActiveKeyboard.Format == KeyboardFormat.CompiledKeyman;
			}
			if (activeKbIsKeyMan)
			{
				return;
			}
			// Collect any characters that are currently in the message queue
			var msg = new Win32.MSG();
			while (true)
			{
				if (Win32.PeekMessage(ref msg, Control.Handle, (uint)Win32.WinMsgs.WM_KEYDOWN, (uint)Win32.WinMsgs.WM_KEYUP, (uint)Win32.PeekFlags.PM_NOREMOVE))
				{
					// If the key is the delete key, then process it normally because some
					// applications may use the DEL as a menu hotkey, which by this time has
					// already processed the keydown message. When that happens, the only
					// time we would get here for a DEL key is because we found the WM_KEYUP
					// message in the queue. In that case, TranslateMessage fails because
					// it only works when both the down and up are translated. The worst that
					// should happen with this special DEL key processing is that we don't
					// collect the delete keys and they happen one at a time.
					if ((int)msg.wParam == (int)Keys.Delete)
					{
						break;
					}
					// Now that we know we're going to translate the message, we need to
					// make sure it's removed from the message queue.
					Win32.PeekMessage(ref msg, Control.Handle, (uint)Win32.WinMsgs.WM_KEYDOWN, (uint)Win32.WinMsgs.WM_KEYUP, (uint)Win32.PeekFlags.PM_REMOVE);
					Win32.TranslateMessage(ref msg);
				}
				else if (Win32.PeekMessage(ref msg, Control.Handle, (uint)Win32.WinMsgs.WM_CHAR, (uint)Win32.WinMsgs.WM_CHAR, (uint)Win32.PeekFlags.PM_NOREMOVE))
				{
					var nextChar = (char)msg.wParam;
					if (!KeepCollectingInput(nextChar))
					{
						break;
					}
					// Since the previous peek didn't remove the message and by this point
					// we know we want to handle the message ourselves, we need to remove
					// the keypress from the message queue.
					Win32.PeekMessage(ref msg, Control.Handle, (uint)Win32.WinMsgs.WM_CHAR, (uint)Win32.WinMsgs.WM_CHAR, (uint)Win32.PeekFlags.PM_REMOVE);

					switch ((int)nextChar)
					{
						case (int)VwSpecialChars.kscBackspace:
							// handle backspace characters.  If there are are characters in
							// the buffer then remove the last one.  If not, then count
							// the backspace so it will be processed later.
							if (buffer.Length > 0)
							{
								if (buffer[0] == 8 || buffer[0] == 0x7f)
								{
									throw new InvalidOperationException("KeepCollectingInput should not allow more than one backspace");
								}
								buffer.Remove(buffer.Length - 1, 1);
							}
							else
							{
								buffer.Append(nextChar);
							}
							return; // only one backspace currently allowed (except canceling earlier data)

						case (int)VwSpecialChars.kscDelForward:
						case '\r':
							if (buffer.Length > 0)
							{
								throw new InvalidOperationException("KeepCollectingInput should not allow more than one delete or return");
							}
							buffer.Append(nextChar);
							return; // only one del currently allowed.
						default:
							needToVerifySurrogates = needToVerifySurrogates || char.IsSurrogate(nextChar);
							// regular characters get added to the buffer
							buffer.Append(nextChar);
							break;
					}
				}
				else
				{
					break;
				}
			}
			// If there were surrogate characters in the typed input verify that they are all matched pairs
			// and clear out the buffer if they are not.
			if (needToVerifySurrogates)
			{
				for (var i = 0; i < buffer.Length; ++i)
				{
					// if we see a trailing surrogate first, or if we see a leading surrogate with no trailing surrogate
					// then alert and clear the buffer.
					if (char.IsLowSurrogate(buffer[i]) ||
						char.IsHighSurrogate(buffer[i]) && (i == buffer.Length || !char.IsLowSurrogate(buffer[i + 1])))
					{
						MessageBox.Show("Unmatched surrogate found in key presses.");
						buffer.Clear();
						break;
					}
					if (char.IsHighSurrogate(buffer[i]))
					{
						// If we get here we had a valid pair so skip the second half
						++i;
					}
				}
			}
		}

		/// <summary>
		/// Helper method that wraps DeleteRangeIfComplex
		/// </summary>
		internal bool DeleteRangeIfComplex(IVwRootBox rootbox)
		{
			bool fWasComplex;
			var vg = GetGraphics();
			try
			{
				rootbox.DeleteRangeIfComplex(vg, out fWasComplex);
			}
			finally
			{
				EditedRootBox.Site.ReleaseGraphics(rootbox, vg);
			}

			return fWasComplex;
		}

		/// <summary>
		/// Handle typed character.
		/// Caller should ensure this is wrapped in a UOW (typically done in an override of
		/// OnKeyPress in RootSiteEditingHelper, since SimpleRootSite does not have access
		/// to LCM and UOW).
		/// </summary>
		/// <param name="input">input string</param>
		/// <param name="shiftStatus">Status of Shift/Control/Alt key</param>
		/// <param name="modifiers">key modifiers - shift status, etc.</param>
		protected internal virtual void OnCharAux(string input, VwShiftStatus shiftStatus, Keys modifiers)
		{
			if (string.IsNullOrEmpty(input))
			{
				return;
			}
			if (Callbacks?.EditedRootBox != null)
			{
				var rootb = Callbacks.EditedRootBox;
				var fWasComplex = DeleteRangeIfComplex(rootb);
				// If DeleteRangeIfComplex handled the deletion, then we don't want to
				// try to handle the delete again.
				var delOrBkspWasPressed = input.Contains(new string((char)VwSpecialChars.kscBackspace, 1)) || input.Contains(new string((char)VwSpecialChars.kscDelForward, 1));
				if (!fWasComplex || !delOrBkspWasPressed)
				{
					if (input == "\r" && shiftStatus == VwShiftStatus.kfssShift)
					{
						CallOnExtendedKey(input[0], shiftStatus);
					}
					else
					{
						// We must (temporarily) have two units of work, since in many cases we need the view to be in the
						// state it gets updated to by the complex delete, before we try to insert, so here we split this
						// into two undo tasks. Eventually we merge the two units of work so they look like a single Undo task.
						if (fWasComplex && rootb.DataAccess.GetActionHandler() != null)
						{
							rootb.DataAccess.GetActionHandler().BreakUndoTask(Resources.ksUndoTyping, Resources.ksRedoTyping);
						}
						CallOnTyping(input, modifiers);
						if (fWasComplex && rootb.DataAccess.GetActionHandler() != null)
						{
							MergeLastTwoUnitsOfWork();
						}
					}
				}

				// It is possible that typing destroyed or changed the active rootbox, so we
				// better use the new one.
				rootb = Callbacks.EditedRootBox;
				rootb.Site.ScrollSelectionIntoView(rootb.Selection, VwScrollSelOpts.kssoDefault);
			}
		}

		/// <summary>
		/// Another case of something we can currently only do in the LCM-aware subclass.
		/// </summary>
		protected virtual void MergeLastTwoUnitsOfWork()
		{
		}

		/// <summary>
		/// Call the root box's OnTyping method. Virtual for testing purposes.
		/// </summary>
		protected virtual void CallOnTyping(string str, Keys modifiers)
		{
			if (Callbacks?.EditedRootBox == null)
			{
				return;
			}

			// The user has pressed Ctrl-Space - do not generate a character.
			if ((modifiers & Keys.Control) != Keys.Control || str.CompareTo(" ") != 0)
			{
				// This needs to be set iff a change of writing system occurs while there is a range
				// selection because of a change of system input language.
				var wsPending = Callbacks.WsPending; // Todo JohnT: hook to client somehow.
				var vg = GetGraphics();
				try
				{
					Callbacks.EditedRootBox.OnTyping(vg, str, GetShiftStatus(modifiers),
						ref wsPending);
				}
				catch (Exception ex)
				{
					var fNotified = false;
					for (var ex1 = ex; ex1 != null; ex1 = ex1.InnerException)
					{
						if (!(ex1 is ArgumentOutOfRangeException))
						{
							continue;
						}

						MessageBox.Show(ex1.Message, Resources.ksWarning, MessageBoxButtons.OK,
							MessageBoxIcon.Warning);
						Callbacks.EditedRootBox
							.Reconstruct(); // Restore the actual value visually.
						fNotified = true;
						break;
					}

					if (!fNotified)
					{
						throw;
					}
				}
				finally
				{
					EditedRootBox.Site.ReleaseGraphics(EditedRootBox, vg);
				}

				Callbacks.WsPending = wsPending;
			}
		}

		/// <summary>
		/// Handle extended keys. Returns false if it wasn't handled (e.g., arrow key beyond valid
		/// characters).
		/// </summary>
		protected virtual bool CallOnExtendedKey(int chw, VwShiftStatus ss)
		{
			if (Platform.IsMono)
			{
				chw &= 0xffff; // OnExtendedKey only expects chw to contain the key info not the modifer info
			}
			if (Callbacks == null || Callbacks.EditedRootBox == null)
			{
				return false;
			}
			// using these keys suppresses prior input lang change.
			Callbacks.WsPending = -1;
			// sets the arrow direction to physical or logical based on LTR or RTL
			var nFlags = Callbacks.ComplexKeyBehavior(chw, ss);
			var retVal = Callbacks.EditedRootBox.OnExtendedKey(chw, ss, (int)nFlags);
			Marshal.ThrowExceptionForHR(retVal); // Don't ignore error HRESULTs
			return retVal != 1;
		}

		/// <summary>
		/// Special handling for Ctrl-Home/End and scrolling the selection into view
		/// </summary>
		protected virtual void HandleKeyDown(KeyEventArgs e, VwShiftStatus ss)
		{
			if (Callbacks?.EditedRootBox == null)
			{
				return;
			}
			if (e.KeyCode == Keys.End && ss == VwShiftStatus.kfssControl)
			{
				// Control end is supposed to scroll all the way to the end.
				// We only know how to do this reliably for one class at present; otherwise,
				// settle for making visible the selection we made at the end.
				Callbacks.ScrollToEnd();
				return;
			}
			if (e.KeyCode == Keys.Home && ss == VwShiftStatus.kfssControl)
			{
				// Control home is supposed to scroll all the way to the top
				Callbacks.ScrollToTop();
				return;
			}
			if (e.KeyCode == Keys.PageDown && (ss == VwShiftStatus.kfssNone || ss == VwShiftStatus.kfssShift))
			{
				Callbacks.ScrollPosition = new Point(-Callbacks.ScrollPosition.X, -Callbacks.ScrollPosition.Y + Control.Height);
				// need to call ScrollSelectionIntoView below
			}
			else if (e.KeyCode == Keys.PageUp && (ss == VwShiftStatus.kfssNone || ss == VwShiftStatus.kfssShift))
			{
				Callbacks.ScrollPosition = new Point(-Callbacks.ScrollPosition.X, -Callbacks.ScrollPosition.Y - Control.Height);
				// need to call ScrollSelectionIntoView below
			}
			var rootb = Callbacks.EditedRootBox;
			if (MiscUtils.IsUnix && (e.KeyCode == Keys.Right || e.KeyCode == Keys.Left || e.KeyCode == Keys.F7 || e.KeyCode == Keys.F8) && ss == VwShiftStatus.kfssNone)
			{
				// FWNX-456 fix for refreshing lines that cursor is not properly invalidating
				if (Control is SimpleRootSite)
				{
					var controlAsSimpleRootSite = (SimpleRootSite)Control;
					var ip = controlAsSimpleRootSite.IPLocation;
					Rect src, dst;
					controlAsSimpleRootSite.GetTransformAtDst(rootb, ip, out src, out dst);
					const int IPWidth = 2;
					const int LineHeightFudgeFactor = 3;
					var rect = new Rectangle(ip.X - dst.left, -dst.top, IPWidth, controlAsSimpleRootSite.LineHeight + LineHeightFudgeFactor);
					controlAsSimpleRootSite.InvalidateRect(rootb, rect.Left, rect.Top, rect.Width, rect.Height);
				}
			}
			if (!rootb.Site.ScrollSelectionIntoView(rootb.Selection, VwScrollSelOpts.kssoDefault))
			{
				Control.Update();
			}
		}
		#endregion

		#region Properties
		/// <summary>
		/// Gets the view constructor.
		/// </summary>
		public IVwViewConstructor ViewConstructor
		{
			get
			{
				int hvoRoot, frag;
				IVwViewConstructor vc;
				IVwStylesheet ss;
				EditedRootBox.GetRootObject(out hvoRoot, out vc, out frag, out ss);
				return vc;
			}
		}

		/// <summary>
		/// Gets a SelectionHelper object set to the current selection in the view
		/// (updated any time the selection changes)
		/// </summary>
		public virtual SelectionHelper CurrentSelection
		{
			get
			{
				if (IsCurrentSelectionOutOfDate)
				{
					if (Callbacks?.EditedRootBox?.Site == null)
					{
						ClearCurrentSelection();
					}
					else
					{
						m_currentSelection = SelectionHelper.Create(Callbacks.EditedRootBox.Site);
					}
				}
				return m_currentSelection;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this instance is current selection out of date.
		/// Changing the selection to another cell in the same row of a browse view doesn't
		/// always result in SelectionChanged() being called, or in the stored selection
		/// becoming invalid.  So we check a little more closely here. (See LT-3787.)
		/// </summary>
		protected virtual bool IsCurrentSelectionOutOfDate
		{
			get
			{
				if (m_currentSelection?.Selection == null)
				{
					return true;
				}
				// If it's invalid, it's obviously out-of-date
				return (!m_currentSelection.Selection.IsValid || m_currentSelection.Selection != RootBoxSelection);
			}
		}

		/// <summary>
		/// Gets the selection from the root box that is currently being edited (can be null).
		/// </summary>
		public virtual IVwSelection RootBoxSelection => Callbacks?.EditedRootBox?.Selection;

		/// <summary>
		/// gets/sets the editable state. This should only be set from
		/// SimpleRootSite.ReadOnlyView.
		/// </summary>
		public bool Editable { get; set; } = true;

		/// <summary>
		/// Returns the edited rootbox from the callbacks for this EditingHelper.
		/// </summary>
		public IVwRootBox EditedRootBox => m_callbacks.EditedRootBox;

		/// <summary>
		/// Retrieve the WSF from the root box's cache.
		/// </summary>
		protected virtual ILgWritingSystemFactory WritingSystemFactory => Callbacks?.EditedRootBox?.DataAccess.WritingSystemFactory;

		/// <summary>
		/// Gets callbacks object.
		/// </summary>
		public IEditingCallbacks Callbacks => m_callbacks;

		/// <summary>
		/// Gets control associated with callback object.
		/// </summary>
		public UserControl Control { get; private set; }

		/// <summary>
		/// Gets or sets the cursor that will always be shown.
		/// </summary>
		/// <value>A <c>Cursor</c> that is shown all the time, regardless of the context
		/// the mouse pointer is over (except when overriden by a derived class).</value>
		/// <remarks>To use the built-in cursors of RootSite, set <c>DefaultCursor</c> to
		/// <c>null</c>.</remarks>
		public virtual Cursor DefaultCursor
		{
			get { return m_defaultCursor; }
			set
			{
				m_defaultCursor = value;
				// set the cursor shown in the current control.
				Control.Cursor = m_defaultCursor ?? GetCursor(false, false, FwObjDataTypes.kodtContextString);
			}
		}

		/// <summary>
		/// Gets/sets the cursor which replaces the IBeam when the mouse is over read-only text.
		/// </summary>
		public Cursor ReadOnlyTextCursor { get; set; }
		#endregion

		#region Mouse related methods
		/// <summary>
		/// Hook method for doing application specific processing on a mouse down event.
		/// </summary>
		public virtual void HandleMouseDown()
		{
		}
		#endregion

		#region Navigation
		/// <summary>
		/// Go to the next paragraph looking at the selection information.
		/// </summary>
		public void GoToNextPara()
		{
			var level = CurrentSelection.GetNumberOfLevels(SelLimitType.Top) - 1;
			var fEnd = CurrentSelection.Selection.EndBeforeAnchor;
			while (level >= 0)
			{
				var iBox = CurrentSelection.Selection.get_BoxIndex(fEnd, level);
				var sel = Callbacks.EditedRootBox.MakeSelInBox(CurrentSelection.Selection, fEnd, level, iBox + 1, true, false, true);
				if (sel != null)
				{
					CurrentSelection.RootSite.ScrollSelectionIntoView(sel, VwScrollSelOpts.kssoDefault);
					return;
				}
				// Try the next level up
				level--;
			}
		}
		#endregion

		#region Other methods
		/// <summary>
		/// Clears the cached current selection. When CurrentSelection is requested, a new
		/// one will be cached with the updated information.
		/// </summary>
		public virtual void ClearCurrentSelection()
		{
			m_currentSelection = null;
		}

		/// <summary>
		/// Gets the IVwGraphics object from the IvwRootsite. NOTE: The graphics object returned
		/// from this method MUST be released with a call to EditedRootBox.Site.ReleaseGraphics()!
		/// </summary>
		private IVwGraphics GetGraphics()
		{
			IVwGraphics vg;
			Rect rcSrcRoot;
			Rect rcDstRoot;
			EditedRootBox.Site.GetGraphics(EditedRootBox, out vg, out rcSrcRoot, out rcDstRoot);
			return vg;
		}

		/// <summary>
		/// Set the cursor that fits the current context
		/// </summary>
		/// <param name="mousePos">The location of the mouse</param>
		/// <param name="rootb">The rootbox</param>
		/// <remarks>If a <see cref="DefaultCursor"/> is set it will be shown. If
		/// <see cref="DefaultCursor"/> is <c>null</c>, then a Hand cursor will be shown if the
		/// mouse pointer is over an object, otherwise the IBeam cursor.
		/// Note that using a single-column BrowseView as a selector column requires setting a
		/// default cursor. For some reason, the rootb.MakeSelAt() call caused every record to
		/// be loaded from the database.
		/// </remarks>
		public void SetCursor(Point mousePos, IVwRootBox rootb)
		{
			if (rootb == null)
			{
				return;
			}
			if (DefaultCursor != null)
			{
				// If we have a default cursor, use it without any further computation.
				Control.Cursor = DefaultCursor;
			}
			else
			{
				IVwGraphics vg;
				Rect rcSrcRoot;
				Rect rcDstRoot;
				rootb.Site.GetGraphics(rootb, out vg, out rcSrcRoot, out rcDstRoot);
				rootb.Site.ReleaseGraphics(rootb, vg); // this needs to be called!

				// Check whether we need a hand or I-beam cursor.
				var fInPicture = false;
				int objDataType;
				var fInObject = rootb.get_IsClickInObject(mousePos.X, mousePos.Y, rcSrcRoot, rcDstRoot, out objDataType);

				// Don't display the hand cursor if we have a range selection
				if (rootb.Selection != null && rootb.Selection.IsRange)
				{
					fInObject = false;
				}
				IVwSelection sel = null;
				try
				{
					sel = rootb.MakeSelAt(mousePos.X, mousePos.Y, rcSrcRoot, rcDstRoot, false);
				}
				catch (COMException)
				{
					// Ignore errors
				}
				if (sel != null)
				{
					fInPicture = sel.SelType == VwSelType.kstPicture;

					// if an application has overridden the cursor then just return.
					if (SetCustomCursor(sel))
					{
						return;
					}
					if (ReadOnlyTextCursor != null && !CanEdit(sel))
					{
						Control.Cursor = ReadOnlyTextCursor;
						return;
					}
				}
				Control.Cursor = GetCursor(fInObject, fInPicture, (FwObjDataTypes)objDataType);
			}
		}

		/// <summary>
		/// Allows an application to set a cursor for the given selection.
		/// </summary>
		protected virtual bool SetCustomCursor(IVwSelection sel)
		{
			return false;
		}

		/// <summary>
		/// Get a cursor suitable for the context indicated by the arguments.
		/// </summary>
		/// <param name="fInObject"><c>True</c> if the mouse pointer is over an object</param>
		/// <param name="fInPicture">True if mouse is over a picture (or icon) (This should be
		/// false if the picture is an ORC-replacement icon.)</param>
		/// <param name="objDataType">The type of the object the mouse pointer is over</param>
		private Cursor GetCursor(bool fInObject, bool fInPicture, FwObjDataTypes objDataType)
		{
			if (fInPicture)
			{
				return Cursors.Arrow;
			}
			if (fInObject && (objDataType == FwObjDataTypes.kodtNameGuidHot
				|| objDataType == FwObjDataTypes.kodtExternalPathName
				|| objDataType == FwObjDataTypes.kodtOwnNameGuidHot
				|| objDataType == FwObjDataTypes.kodtPictEvenHot
				|| objDataType == FwObjDataTypes.kodtPictOddHot))
			{
				return Cursors.Hand;
			}

			return Control is SimpleRootSite ? ((SimpleRootSite)Control).IBeamCursor : Cursors.IBeam;
		}

		/// <summary>
		/// Convert this instance to type T.
		/// </summary>
		/// <typeparam name="T">Desired type to cast to.</typeparam>
		/// <returns>Editing helper cast as T.</returns>
		/// <remarks>We added this method so that we can retrieve the TeEditingHelper from the
		/// PublicationEditingHelper (which internally contains an TeEditingHelper).</remarks>
		public virtual T CastAs<T>() where T : EditingHelper
		{
			return this as T;
		}
		#endregion

		#region Style related methods
		/// <summary>
		/// If the current selection contains one run with a character style or multiple runs
		/// with the same character style this method returns the character style; otherwise
		/// returns the paragraph style unless multiple paragraphs are selected that have
		/// different paragraph styles.
		/// </summary>
		/// <param name="styleName">Gets the styleName</param>
		/// <returns>The styleType or -1 if no style type can be found or multiple style types
		/// are in the selection.  Otherwise returns the styletype</returns>
		public virtual int GetStyleNameFromSelection(out string styleName)
		{
			try
			{
				IVwSelection vwsel = null;
				if (Callbacks?.EditedRootBox != null)
				{
					vwsel = Callbacks.EditedRootBox.Selection;
				}
				if (vwsel == null)
				{
					styleName = string.Empty;
					return -1;
				}
				styleName = GetCharStyleNameFromSelection(vwsel);
				if (!string.IsNullOrEmpty(styleName))
				{
					return (int)StyleType.kstCharacter;
				}
				styleName = GetParaStyleNameFromSelection();
				return styleName != null ? (int)StyleType.kstParagraph : -1;
			}
			catch
			{
				styleName = string.Empty;
				return -1;
			}
		}

		/// <summary>
		/// Gets the paragraph style name from the selection
		/// </summary>
		/// <returns>The style name or empty string if there are multiple styles or
		/// no selection</returns>
		public string GetParaStyleNameFromSelection()
		{
			var styleName = string.Empty;
			ITsTextProps[] vttp;
			try
			{
				vttp = GetParagraphTextPropsFromSelection();
			}
			catch
			{
				return styleName;
			}
			if (vttp != null)
			{
				styleName = GetStyleNameFromTextProps(vttp, (int)StyleType.kstParagraph) ?? string.Empty;
			}
			return styleName;
		}

		/// <summary>
		/// Gets the style name from the specified ITsTextProps array if all ITsTextProps runs
		/// contain the same style name.
		/// </summary>
		/// <param name="vttp">The array of ITsTextProps</param>
		/// <param name="styleType">The type of style expected to find in each ITsTextProps
		/// (e.g. character, paragraph).</param>
		/// <returns>Gets the style name from the specified ITsTextProps array if all
		/// ITsTextProps runs contain the same style name, otherwise null</returns>
		private string GetStyleNameFromTextProps(ITsTextProps[] vttp, int styleType)
		{
			Debug.Assert(vttp != null);
			string styleName;
			var prevStyleName = styleName = string.Empty;
			for (var ittp = 0; ittp < vttp.Length; ittp++)
			{
				styleName = (vttp[ittp] != null ? vttp[ittp].GetStrPropValue((int)FwTextPropType.ktptNamedStyle) : string.Empty) ?? string.Empty;
				if (styleName == string.Empty && styleType == (int)StyleType.kstParagraph)
				{
					styleName = DefaultNormalParagraphStyleName;
				}
				if (ittp > 0 && prevStyleName != styleName)
				{
					return null;
				}
				prevStyleName = styleName;
			}

			return styleName;
		}

		/// <summary>
		/// Gets the default "Normal" paragraph style name. This base implementation just returns
		/// a hardcoded string. It will probably never be used, so it doesn't matter.
		/// </summary>
		protected virtual string DefaultNormalParagraphStyleName => "Normal";

		/// <summary>
		/// Get the view selection and paragraph properties.
		/// </summary>
		/// <param name="vwsel">[out] The selection</param>
		/// <param name="hvoText">[out] The HVO of the paragraph's owner</param>
		/// <param name="flidParaOwner">[out] The flid in which this paragraph is owned</param>
		/// <param name="vqvps">[out] The paragraph properties</param>
		/// <param name="ihvoFirst">[out] Start index of selection</param>
		/// <param name="ihvoLast">[out] End index of selection</param>
		/// <param name="vqttp">[out] The style rules</param>
		/// <returns>Return false if there is not a selection or selection is not in a paragraph
		/// for which we can get properties. Otherwise return true.</returns>
		public bool GetParagraphProps(out IVwSelection vwsel, out int hvoText, out int flidParaOwner, out IVwPropertyStore[] vqvps, out int ihvoFirst, out int ihvoLast, out ITsTextProps[] vqttp)
		{
			ihvoFirst = 0;
			ihvoLast = 0;
			vqttp = null;

			int ihvoAnchor, ihvoEnd;
			if (!IsParagraphProps(out vwsel, out hvoText, out flidParaOwner, out vqvps, out ihvoAnchor, out ihvoEnd))
			{
				return false;
			}
			// OK, we're going to do it!
			ihvoFirst = ihvoAnchor;
			ihvoLast = ihvoEnd;
			if (ihvoFirst > ihvoLast)
			{
				ihvoFirst = ihvoLast;
				ihvoLast = ihvoAnchor;
			}
			var sda = Callbacks.EditedRootBox.DataAccess;
			if (sda == null) // Very unlikely, but it's a COM interface...
			{
				return false;
			}
			if (HandleSpecialParagraphType(flidParaOwner, out vqttp))
			{
				return true;
			}
			vqttp = new ITsTextProps[ihvoLast - ihvoFirst + 1];
			var index = 0;
			for (var ihvo = ihvoFirst; ihvo <= ihvoLast; ihvo++)
			{
				var hvoPara = sda.get_VecItem(hvoText, flidParaOwner, ihvo);
				var ttp = sda.get_UnknownProp(hvoPara, ParagraphPropertiesTag) as ITsTextProps;
				vqttp[index] = ttp;
				index++;
			}
			return true;
		}

		/// <summary>
		/// Allows subclasses to examine the given flid to see if it is a type that requires
		/// special handling, as opposed to just getting an array of style props for each
		/// paragraph in the property represented by that flid.
		/// </summary>
		/// <param name="flidParaOwner">The flid in which the paragraph is owned</param>
		/// <param name="vqttp">array of text props representing the paragraphs in</param>
		/// <returns><c>true</c> if handled; <c>false</c> otherwise.</returns>
		protected virtual bool HandleSpecialParagraphType(int flidParaOwner, out ITsTextProps[] vqttp)
		{
			vqttp = null;
			return false;
		}

		/// <summary>
		/// Gets the Character style name from the selection
		/// </summary>
		/// <returns>The style name or null if there are multiple styles or an empty string if
		/// there is no character style</returns>
		public string GetCharStyleNameFromSelection()
		{
			var sel = RootBoxSelection;
			return sel == null ? string.Empty : GetCharStyleNameFromSelection(sel);
		}

		/// <summary>
		/// Gets the Character style name from the selection
		/// </summary>
		/// <param name="vwsel">The IVwSelection to get the style name from</param>
		/// <returns>The style name or null if there are multiple styles or an empty string
		/// if there is no character style</returns>
		protected string GetCharStyleNameFromSelection(IVwSelection vwsel)
		{
			if (vwsel == null)
			{
				return null;
			}
			try
			{
				ITsTextProps[] vttp;
				IVwPropertyStore[] vvps;
				int cttp;
				SelectionHelper.GetSelectionProps(vwsel, out vttp, out vvps, out cttp);
				return cttp == 0 ? null : GetStyleNameFromTextProps(vttp, (int)StyleType.kstCharacter);
			}
			catch
			{
				return null;
			}
		}

		/// <summary>
		/// Gets paragraph text properties for the current selection
		/// </summary>
		/// <returns>An array of ITsTextProps objects</returns>
		protected ITsTextProps[] GetParagraphTextPropsFromSelection()
		{
			IVwSelection vwsel;
			int hvoText;
			int tagText;
			IVwPropertyStore[] vvps;
			int ihvoFirst;
			int ihvoLast;
			ITsTextProps[] vttp;
			return GetParagraphProps(out vwsel, out hvoText, out tagText, out vvps, out ihvoFirst, out ihvoLast, out vttp) ? vttp : null;
		}

		/// <summary>
		/// Get all the information that we need for applying styles: get paragraph properties
		/// from the selection.
		/// </summary>
		/// <param name="vwsel">[out] The selection</param>
		/// <param name="hvoText">[out] HVO of text in selected paragraph</param>
		/// <param name="tagText">[out] Tag of text in selected paragraph</param>
		/// <param name="vttp">[out] Vector of text props</param>
		/// <param name="vvps">[out] Vector of VwPropertyStore</param>
		/// <param name="ihvoFirst">[out] Start index of selection</param>
		/// <param name="ihvoLast">[out] End index of selection</param>
		/// <param name="vttpHard">[out] Vector of text props for hard formatting</param>
		/// <param name="vvpsSoft">[out] Vector of prop stores for soft formatting</param>
		/// <param name="fRet">[out] <c>false</c> if there is neither a selection nor a
		/// paragraph property; otherwise true.</param>
		/// <returns><c>false</c> if method exited because <paramref name='fRet'/> is
		/// <c>false</c> or there are no TsTextProps in the paragraph, otherwise <c>true</c>
		/// </returns>
		protected bool GetAllParagraphProps(out IVwSelection vwsel, out int hvoText, out int tagText, out ITsTextProps[] vttp,
			out IVwPropertyStore[] vvps, out int ihvoFirst, out int ihvoLast, out ITsTextProps[] vttpHard, out IVwPropertyStore[] vvpsSoft, out bool fRet)
		{
			vttpHard = null;
			vvpsSoft = null;
			fRet = true;

			// Get the paragraph properties from the selection. If there is neither a selection
			// nor a paragraph property, return false.
			if (!GetParagraphProps(out vwsel, out hvoText, out tagText, out vvps, out ihvoFirst, out ihvoLast, out vttp))
			{
				fRet = false;
				return false;
			}
			// If there are no TsTextProps for the paragraph(s), return true. There is nothing
			// to format.
			if (0 == vttp.Length)
			{
				return false;
			}
			var cttp = vttp.Length;
			using (var ptrHard = MarshalEx.ArrayToNative<ITsTextProps>(cttp))
			using (var ptrSoft = MarshalEx.ArrayToNative<IVwPropertyStore>(cttp))
			{
				vwsel.GetHardAndSoftParaProps(cttp, vttp, ptrHard, ptrSoft, out cttp);
				vttpHard = MarshalEx.NativeToArray<ITsTextProps>(ptrHard, cttp);
				vvpsSoft = MarshalEx.NativeToArray<IVwPropertyStore>(ptrSoft, cttp);
			}
			return true;
		}

		/// <summary>
		/// Check for presence of proper paragraph properties.
		/// </summary>
		/// <param name="vwsel">[out] The selection</param>
		/// <param name="hvoText">[out] The HVO</param>
		/// <param name="tagText">[out] The tag</param>
		/// <param name="vqvps">[out] The paragraph properties</param>
		/// <param name="ihvoAnchor">[out] Start index of selection</param>
		/// <param name="ihvoEnd">[out] End index of selection</param>
		/// <returns>Return <c>false</c> if neither selection nor paragraph property. Otherwise
		/// return <c>true</c>.</returns>
		public bool IsParagraphProps(out IVwSelection vwsel, out int hvoText, out int tagText, out IVwPropertyStore[] vqvps, out int ihvoAnchor, out int ihvoEnd)
		{
			hvoText = 0;
			tagText = 0;
			vqvps = null;
			ihvoAnchor = 0;
			ihvoEnd = 0;

			// Get the selection. Can't do command unless we have one.
			vwsel = RootBoxSelection;
			if (vwsel == null || !vwsel.IsValid)
			{
				return false;
			}
			// First check the anchor to see if we can find a paragraph at some level.
			if (!GetParagraphLevelInfoForSelection(vwsel, false, out hvoText, out tagText, out ihvoAnchor))
			{
				return false;
			}
			// Next check the end to see if we have a paragraph in the same text and flid
			int hvoEnd;
			int tagEnd;
			if (!GetParagraphLevelInfoForSelection(vwsel, true, out hvoEnd, out tagEnd, out ihvoEnd))
			{
				return false;
			}
			// Make sure it's the same property.
			if (tagEnd != tagText || hvoText != hvoEnd)
			{
				return false;
			}
			GetParaPropStores(vwsel, out vqvps);
			// make sure we have one prop for each paragraph that is selected.
			var ihvoMin = vwsel.EndBeforeAnchor ? ihvoEnd : ihvoAnchor;
			var ihvoMax = vwsel.EndBeforeAnchor ? ihvoAnchor : ihvoEnd;
			if (vqvps.Length != ihvoMax - ihvoMin + 1)
			{
				// Got rid of this assertion. It seems to be fired from selecting near a
				// picture.
				//Debug.Assert(false, "This isn't handled well!");
				Debug.WriteLine("Got a different number of properties from the number of paras we think are selected");
				return false;
			}
			return true;
		}

		/// <summary>
		/// Gets the paragraph level info from the requested end of the given selection.
		/// </summary>
		/// <param name="vwsel">The selection</param>
		/// <param name="fEnd">if set to <c>true</c> get the info from the end of the selection;
		/// otherwise, get the info from the anchor of the selection.</param>
		/// <param name="hvoText">The hvo of the text that owns the paragraph.</param>
		/// <param name="tagText">The flid of the property in which the paragraphs are owned
		/// (probably almost always 14001).</param>
		/// <param name="ihvo">The ihvo of the paragraph.</param>
		/// <returns><c>true</c> if a paragraph is found at some level and is not displayed in
		/// any previous instance</returns>
		private bool GetParagraphLevelInfoForSelection(IVwSelection vwsel, bool fEnd, out int hvoText, out int tagText, out int ihvo)
		{
			hvoText = 0;
			tagText = 0;
			ihvo = 0;

			// We need a two-level or more selection.
			var clev = vwsel.CLevels(fEnd);
			if (clev < 2)
			{
				return false;
			}
			var cpropPrevious = 0;
			IVwPropertyStore vps;
			var fFoundParagraphLevel = false;
			for (var lev = 0; lev < clev && !fFoundParagraphLevel; lev++)
			{
				// At this point, we know how to do this command only for structured text paragraphs.
				vwsel.PropInfo(fEnd, lev, out hvoText, out tagText, out ihvo, out cpropPrevious, out vps);
				fFoundParagraphLevel = IsParagraphLevelTag(tagText);
			}
			// If we didn't find the paragraph level or we found a level for a property that
			// was not the first occurrence of the paragraph, then give up.
			return fFoundParagraphLevel && cpropPrevious == 0;
		}

		/// <summary>
		/// Determines whether the given tag represents paragraph-level information
		/// </summary>
		protected virtual bool IsParagraphLevelTag(int tag)
		{
			return false;
		}

		/// <summary>
		/// The default tag/flid containing the contents of ordinary paragraphs
		/// </summary>
		protected virtual int ParagraphContentsTag
		{
			get { throw new NotSupportedException(); }
		}

		/// <summary>
		/// The default tag/flid containing the properties of ordinary paragraphs
		/// </summary>
		protected virtual int ParagraphPropertiesTag
		{
			get { throw new NotSupportedException(); }
		}

		/// <summary>
		/// Gets an array of property stores, one for each paragraph in the given selection.
		/// </summary>
		/// <param name="vwsel">The selection.</param>
		/// <param name="vqvps">The property stores.</param>
		protected virtual void GetParaPropStores(IVwSelection vwsel, out IVwPropertyStore[] vqvps)
		{
			int cvps;
			SelectionHelper.GetParaProps(vwsel, out vqvps, out cvps);
		}

		/// <summary>
		/// Get the selection and character properties for the current selection in a
		/// particular root box (this is a static method).
		/// </summary>
		/// <param name="rootb">root box</param>
		/// <param name="vwsel">selection</param>
		/// <param name="vttp">Array of ITsTextProps</param>
		/// <param name="vvps">Array of IVwPropertyStore</param>
		/// <returns>Return <c>false</c> if there is no root box, or if it has no selection.
		/// Otherwise return <c>true</c>.</returns>
		public static bool GetCharacterProps(IVwRootBox rootb, out IVwSelection vwsel, out ITsTextProps[] vttp, out IVwPropertyStore[] vvps)
		{
			vwsel = null;
			vttp = null;
			vvps = null;

			// Get the selection. Can't do command unless we have one.
			if (rootb == null)
			{
				return false;
			}
			vwsel = rootb.Selection;
			if (vwsel == null)
			{
				return false;
			}
			int cttp;
			SelectionHelper.GetSelectionProps(vwsel, out vttp, out vvps, out cttp);

			return cttp != 0;
		}

		/// <summary>
		/// Get the view selection and character properties. Return false if there is neither a
		/// selection nor any text selected. Otherwise return true.
		/// </summary>
		public bool GetCharacterProps(out IVwSelection vwsel, out ITsTextProps[] vttp, out IVwPropertyStore[] vvps)
		{
			return GetCharacterProps(Callbacks?.EditedRootBox, out vwsel, out vttp, out vvps);
		}
		#endregion

		#region Apply style changes
		/// <summary>
		/// Apply the selected style with only the specified style name.
		/// </summary>
		/// <param name="sStyleToApply">Style name (this could be a paragraph or character
		/// style).</param>
		public virtual void ApplyStyle(string sStyleToApply)
		{
			IVwSelection vwsel;
			IVwPropertyStore[] vvpsChar;
			ITsTextProps[] vttpChar;
			GetCharacterProps(out vwsel, out vttpChar, out vvpsChar);

			IVwPropertyStore[] vvpsPara;
			ITsTextProps[] vttpPara;
			int hvoText;
			int tagText;
			int ihvoFirst;
			int ihvoLast;
			GetParagraphProps(out vwsel, out hvoText, out tagText, out vvpsPara, out ihvoFirst, out ihvoLast, out vttpPara);

			ApplyStyle(sStyleToApply, vwsel, vttpPara, vttpChar);
		}

		/// <summary>
		/// Apply the selected style
		/// </summary>
		/// <param name="sStyleToApply">Style name</param>
		/// <param name="vwsel">Selection</param>
		/// <param name="vttpPara">Paragraph properties</param>
		/// <param name="vttpChar">Character properties</param>
		public virtual void ApplyStyle(string sStyleToApply, IVwSelection vwsel, ITsTextProps[] vttpPara, ITsTextProps[] vttpChar)
		{
			if (Callbacks?.EditedRootBox?.DataAccess == null)
			{
				return;
			}
			var stylesheet = Callbacks.EditedRootBox.Stylesheet;
			if (stylesheet == null)
			{
				return;
			}
			var stType = string.IsNullOrEmpty(sStyleToApply)
				? StyleType.kstCharacter
				: (StyleType)stylesheet.GetType(sStyleToApply);

			switch (stType)
			{
				default:
					Debug.Assert(false); // This should not happen.
					break;

				case StyleType.kstParagraph:
					// Set the style name of the paragraph(s) to stuStyleName and redraw.
					ApplyParagraphStyle(sStyleToApply);
					break;

				case StyleType.kstCharacter:
					RemoveCharFormatting(vwsel, ref vttpChar, sStyleToApply);
					break;
			}
		}

		/// <summary>
		/// Apply the paragraph style, that is modify the TsTextProps object that is the style
		/// of each of the selected (or partly selected) paragraphs and clear all explicit
		/// paragraph formatting.
		/// </summary>
		/// <param name="strNewVal">the name of a new style</param>
		/// <returns>Returns <c>false</c> if the paragraph properties can't be retrieved,
		/// otherwise <c>true</c>.</returns>
		///
		/// <remarks>
		/// <p>Formerly <c>AfVwRootSite::FormatParas</c>.</p>
		/// <p>The functionality for the other cases when FormatParas was called still needs
		/// to be ported when we need it. They should be put in separate methods.</p>
		/// <p>ApplyParagraphStyle begins by getting the paragraph properties from the
		/// selection. If paragraph properties cannot be retrieved through a selection,
		/// ApplyParagraphStyle returns false. If no text properties are retrieved in vttp,
		/// ApplyParagraphStyle returns true since there is nothing to do.</p>
		/// <p>Next, immediate changes are made to paragraph properties retrieved from the
		/// selection, and the variable vttp is updated.</p>
		///	<p>If ApplyParagraphStyle has not returned as described above, it narrows the range
		///	of TsTextProps to those that are not <c>null</c>. Then, it saves the view selection
		///	level information by calling AllTextSelInfo on the selection. To "fake" a property
		///	change, PropChanged is called on the SilDataAccess pointer. Finally, the selection
		///	is restored by a call to MakeTextSelection on the RootBox pointer.</p>
		/// </remarks>
		public virtual bool ApplyParagraphStyle(string strNewVal)
		{
			IVwSelection vwsel;
			int hvoText;
			int tagText;
			int ihvoFirst, ihvoLast;
			ITsTextProps[] vttp;
			IVwPropertyStore[] vqvps;
			ITsTextProps[] vttpHard;
			IVwPropertyStore[] vvpsSoft;
			bool fRet;
			if (!GetAllParagraphProps(out vwsel, out hvoText, out tagText, out vttp, out vqvps, out ihvoFirst, out ihvoLast, out vttpHard, out vvpsSoft, out fRet))
			{
				return fRet;
			}

			// Make a new TsTextProps object, and set its NamedStyle.
			var tpb = TsStringUtils.MakePropsBldr();
			tpb.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, strNewVal);
			var newProps = tpb.GetTextProps();
			var fChangedStyle = false;
			for (var ittp = 0; ittp < vttp.Length; ++ittp)
			{
				string oldStyle = null;
				if (vttp[ittp] != null)
				{
					// this can happen if para never had style set explicitly.
					oldStyle = vttp[ittp].GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
				}
				fChangedStyle |= (oldStyle != strNewVal);

				// ENHANCE JohnT: it would be nice to detect we are applying the
				// same style, and if there is explicit formatting put up the dialog
				// asking whether to change the style defn.
				// NOTE: that is probably not what we want to do in TE!
				vttp[ittp] = newProps;
			}
			if (!fChangedStyle)
			{
				return true; // Nothing really changed!
			}
			var helper = SelectionHelper.Create(vwsel, Callbacks.EditedRootBox.Site);
			// Narrow the range of TsTextProps to only include those that are not NULL.
			int ihvoFirstMod;
			int ihvoLastMod;
			NarrowRangeOfTsTxtProps(hvoText, tagText, vttp, vvpsSoft, true, ihvoFirst, ihvoLast, out ihvoFirstMod, out ihvoLastMod);
			RestoreSelectionAtEndUow(vwsel, helper);
			return true;
		}

		/// <summary>
		/// Remove character formatting.
		/// </summary>
		public void RemoveCharFormatting()
		{
			RemoveCharFormatting(false);
		}

		/// <summary>
		/// Remove character formatting.
		/// </summary>
		/// <param name="removeAllStyles">if true, all styles in selection will be removed</param>
		public virtual void RemoveCharFormatting(bool removeAllStyles)
		{
			IVwSelection vwsel;
			ITsTextProps[] vttp;
			IVwPropertyStore[] vvps;
			if (!GetCharacterProps(out vwsel, out vttp, out vvps))
			{
				return;
			}
			if (Callbacks?.EditedRootBox == null)
			{
				return;
			}
			RemoveCharFormatting(vwsel, ref vttp, null, removeAllStyles);
		}

		/// <summary>
		/// Allow certain style names to be given special sematic meaning so they will be
		/// skipped over when removing character styles.
		/// </summary>
		/// <param name="name">style name to check</param>
		/// <returns>true to apply meaning to the style and skip it.</returns>
		public virtual bool SpecialSemanticsCharacterStyle(string name)
		{
			return false;
		}

		/// <summary>
		/// Remove character formatting, as when the user types ctrl-space, or chooses a named
		/// style. Assumes an Undo action is active if wanted. Clears all formatting
		/// controlled by the Format/Font dialog, and sets the specified named style, or
		/// clears that too if it is null or empty. (Pass null to choose "default paragraph
		/// style".)
		/// </summary>
		/// <remarks>The method is public so it can be used by the Find/Replace dialog.</remarks>
		public void RemoveCharFormatting(IVwSelection vwsel, ref ITsTextProps[] vttp, string sStyle)
		{
			RemoveCharFormatting(vwsel, ref vttp, sStyle, false);
		}

		/// <summary>
		/// Remove character formatting, as when the user types ctrl-space, or chooses a named
		/// style. Assumes an Undo action is active if wanted. Clears all formatting
		/// controlled by the Format/Font dialog, and sets the specified named style, or
		/// clears that too if it is null or empty. (Pass null to choose "default paragraph
		/// style".)
		/// </summary>
		public void RemoveCharFormatting(IVwSelection vwsel, ref ITsTextProps[] vttp, string sStyle, bool removeAllStyles)
		{
			var fPropsModified = false;
			Debug.Assert(vttp != null, "This shouldn't happen. Please look at TE-6499.");
			if (vttp == null)
			{
				return;
			}
			var cttp = vttp.Length;
			ITsTextProps ttpEmpty = null;
			for (var ittp = 0; ittp < cttp; ittp++)
			{
				if (vwsel.IsRange)
				{
					var objData = vttp[ittp].GetStrPropValue((int)FwTextPropType.ktptObjData);
					if (objData != null)
					{
						// We don't want to clear most object data, because it has the effect of making
						// ORCs unusable. A special case is LinkedFiles, which are applied to regular
						// characters, and annoying not to be able to remove.
						if (objData.Length == 0 || objData[0] != Convert.ToChar((int)FwObjDataTypes.kodtExternalPathName))
						{
							continue; // skip this run.
						}
					}
				}
				// Skip user prompt strings. A user prompt string will (hopefully) have a
				// dummy property set on it to indicate this.
				int nvar;
				if (vttp[ittp].GetIntPropValues(SimpleRootSite.ktptUserPrompt, out nvar) == 1)
				{
					continue;
				}
				// Allow a subclass to exclude styles that may have special semantics that should not
				// removed by applying a style when they are a part of a selection with multiple runs.
				if (!removeAllStyles && cttp > 1)
				{
					var oldStyle = vttp[ittp].GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
					if (SpecialSemanticsCharacterStyle(oldStyle))
					{
						continue;
					}
				}
				if (ttpEmpty == null)
				{
					var tpbEmpty = vttp[ittp].GetBldr();
					tpbEmpty.SetStrPropValue((int)FwTextPropType.ktptFontFamily, null);
					tpbEmpty.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, sStyle);
					tpbEmpty.SetIntPropValues((int)FwTextPropType.ktptItalic, -1, -1);
					tpbEmpty.SetIntPropValues((int)FwTextPropType.ktptBold, -1, -1);
					tpbEmpty.SetIntPropValues((int)FwTextPropType.ktptSuperscript, -1, -1);
					tpbEmpty.SetIntPropValues((int)FwTextPropType.ktptUnderline, -1, -1);
					tpbEmpty.SetIntPropValues((int)FwTextPropType.ktptFontSize, -1, -1);
					tpbEmpty.SetIntPropValues((int)FwTextPropType.ktptOffset, -1, -1);
					tpbEmpty.SetIntPropValues((int)FwTextPropType.ktptForeColor, -1, -1);
					tpbEmpty.SetIntPropValues((int)FwTextPropType.ktptBackColor, -1, -1);
					tpbEmpty.SetIntPropValues((int)FwTextPropType.ktptUnderColor, -1, -1);
					tpbEmpty.SetStrPropValue((int)FwTextPropType.ktptFontVariations, null);
					tpbEmpty.SetStrPropValue((int)FwTextPropType.ktptObjData, null);
					ttpEmpty = tpbEmpty.GetTextProps();
				}
				vttp[ittp] = ttpEmpty;
				fPropsModified = true;
			}

			if (fPropsModified)
			{
				// Setting the selection props might cause our selection to get destroyed because
				// it might recreate paragraph boxes. Therefore we remember our current
				// selection and afterwards try to restore it again.
				SelectionHelper helper = null;
				if (Callbacks != null) // might be null when running tests
				{
					helper = SelectionHelper.Create(vwsel, Callbacks.EditedRootBox.Site);
				}
				ChangeCharacterStyle(vwsel, vttp, cttp);
				RestoreSelectionAtEndUow(vwsel, helper);
			}
			if (Callbacks != null)
			{
				Callbacks.WsPending = -1;
			}
			ClearCurrentSelection();
		}

		/// <summary>
		/// Changes the character style.
		/// </summary>
		/// <param name="sel">The selection.</param>
		/// <param name="props">The properties specifying the new writing system.</param>
		/// <param name="numProps">The number of ITsTextProps.</param>
		protected virtual void ChangeCharacterStyle(IVwSelection sel, ITsTextProps[] props, int numProps)
		{
			Debug.Assert(sel != null);
			Debug.Assert(props != null);

			sel.SetSelectionProps(numProps, props);
		}

		/// <summary>
		/// This can't be implemented at this level, but some methods here call it so that subclasses that know about
		/// UOW can implement it helpfully. If it's not implemented some edits (e.g., applying a paragraph style) will
		/// typically make the selection go away.
		/// </summary>
		protected virtual void RestoreSelectionAtEndUow(IVwSelection sel, SelectionHelper helper)
		{
		}

		/// <summary>
		/// Narrow the range of TsTextProps to only include those that are not null.
		/// </summary>
		/// <param name="hvoText">HVO of the text</param>
		/// <param name="tagText">tag of the text</param>
		/// <param name="vttp">Vector of text props for text</param>
		/// <param name="vvpsSoft">Vector of property stores for text</param>
		/// <param name="fParagraphStyle"><c>true</c> if called for paragraph style</param>
		/// <param name="ihvoFirst">Index of first textprop in <paramref name='vttp'/></param>
		/// <param name="ihvoLast">Index of last textprop in <paramref name='vttp'/></param>
		/// <param name="ihvoFirstMod">Index of adjusted first textprop in
		/// <paramref name='vttp'/></param>
		/// <param name="ihvoLastMod">Index of adjusted last textprop in <paramref name='vttp'/>
		/// </param>
		private void NarrowRangeOfTsTxtProps(int hvoText, int tagText, ITsTextProps[] vttp, IVwPropertyStore[] vvpsSoft, bool fParagraphStyle,
			int ihvoFirst, int ihvoLast, out int ihvoFirstMod, out int ihvoLastMod)
		{
			var sda = Callbacks.EditedRootBox.DataAccess;
			ihvoFirstMod = -1;
			ihvoLastMod = -1;
			for (var ihvo = ihvoFirst; ihvo <= ihvoLast; ihvo++)
			{
				ITsTextProps ttp;
				ttp = vttp[ihvo - ihvoFirst];
				if (ttp != null)
				{
					// If we set a style for a paragraph at all, it must specify a named style.
					// The "default Normal" mechanism (see StVc.cpp) only works for paragraphs
					// which lack a style altogether. Any actual style must specify "Normal" unless
					// it specifies something else.
					var strNamedStyle = ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
					if (strNamedStyle.Length == 0)
					{
						ITsPropsBldr tpb;
						tpb = ttp.GetBldr();
						tpb.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, DefaultNormalParagraphStyleName);
						ttp = tpb.GetTextProps();
					}
					ihvoLastMod = ihvo;
					if (ihvoFirstMod < 0)
					{
						ihvoFirstMod = ihvo;
					}
					int hvoPara;
					// TODO (EberhardB): when we have the C# cache we can do this differently
					hvoPara = sda.get_VecItem(hvoText, tagText, ihvo);
					ITsTextProps ttpRet;
					var vpsSoft = vvpsSoft[ihvo - ihvoFirst];
					if (RemoveRedundantHardFormatting(vpsSoft, ttp, fParagraphStyle, hvoPara, out ttpRet))
					{
						ttp = ttpRet;
					}
					ChangeParagraphStyle(sda, ttp, hvoPara);
				}
			}
		}

		/// <summary>
		/// Change the paragraph style.
		/// </summary>
		protected virtual void ChangeParagraphStyle(ISilDataAccess sda, ITsTextProps ttp, int hvoPara)
		{
			sda.SetUnknown(hvoPara, ParagraphPropertiesTag, ttp);
		}

		/// <summary>
		/// Remove any hard-formatting that duplicates formatting in the styles.
		/// </summary>
		/// <param name="vpsSoft">Soft formatting</param>
		/// <param name="ttpHard">Hard formatting</param>
		/// <param name="fParaStyle"><c>true</c> if applied on a paragraph style</param>
		/// <param name="hvoPara">HVO of the paragraph</param>
		/// <param name="ttpRet">Resulting text props. This is <c>null</c> if no changes are
		/// made.</param>
		/// <returns>Return <c>true</c> if any change was made to <paramref name='ttpHard'/>
		/// </returns>
		protected bool RemoveRedundantHardFormatting(IVwPropertyStore vpsSoft, ITsTextProps ttpHard, bool fParaStyle, int hvoPara, out ITsTextProps ttpRet)
		{
			ttpRet = null;
			ITsPropsBldr tpb = null;
			if (fParaStyle)
			{
				// Setting a paragraph style automatically removes any paragraph hard formatting.
				// But what we need to fix is the character hard formatting for each run in the
				// paragraph.
				// First, apply the new style to the "soft" property store.
				string strStyle;
				strStyle = ttpHard.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
				ITsPropsBldr tpbStyle;
				tpbStyle = TsStringUtils.MakePropsBldr();
				tpbStyle.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, strStyle);
				ITsTextProps ttpStyle;
				ttpStyle = tpbStyle.GetTextProps();
				IVwPropertyStore vpsSoftPlusStyle;
				vpsSoftPlusStyle = vpsSoft.get_DerivedPropertiesForTtp(ttpStyle);

				ITsPropsBldr tpbEnc;
				tpbEnc = TsStringUtils.MakePropsBldr();

				var sda = Callbacks.EditedRootBox.DataAccess;
				ITsString tss;
				tss = sda.get_StringProp(hvoPara, ParagraphContentsTag);

				ITsStrBldr tsb;
				tsb = tss.GetBldr();

				int crun;
				crun = tss.RunCount;
				var fChanged = false;
				for (var irun = 0; irun < crun; irun++)
				{
					// Get the run's properties.
					TsRunInfo tri;
					ITsTextProps ttpRun;
					ttpRun = tss.FetchRunInfo(irun, out tri);

					// Create a property store based on the soft properties but with the writing
					// system/ows specified. This has the effect of applying any character
					// properties for the given writing system that are already in the property
					// store.
					IVwPropertyStore vpsForRun;
					int ws, ows;
					ws = ttpRun.GetIntPropValues((int)FwTextPropType.ktptWs, out ows);
					tpbEnc.SetIntPropValues((int)FwTextPropType.ktptWs, ows, ws);
					ITsTextProps ttpEnc;
					ttpEnc = tpbEnc.GetTextProps();
					vpsForRun = vpsSoftPlusStyle.get_DerivedPropertiesForTtp(ttpEnc);
					// Compare the adjusted property store to the run's properties.
					ITsTextProps ttpFixed;
					if (RemoveRedundantHardFormatting(vpsForRun, ttpRun, false, 0, out ttpFixed))
					{
						// Make the change in the string builder.
						tsb.SetProperties(tri.ichMin, tri.ichLim, ttpFixed);
						fChanged = true;
					}
				}
				if (fChanged)
				{
					// Update the string in the data cache.
					ITsString tssFixed;
					tssFixed = tsb.GetString();
					sda.SetString(hvoPara, ParagraphContentsTag, tssFixed);
				}
				return false;
			}
			var ctpt = ttpHard.IntPropCount;
			for (var itpt = 0; itpt < ctpt; itpt++)
			{
				int tpt;
				int nVarHard;
				var nValHard = ttpHard.GetIntProp(itpt, out tpt, out nVarHard);
				int nValSoft, nVarSoft;
				switch ((FwTextPropType)tpt)
				{
					case FwTextPropType.ktptLineHeight:
						nValSoft = vpsSoft.get_IntProperty(tpt);
						var nRelHeight = vpsSoft.get_IntProperty((int)VwStyleProperty.kspRelLineHeight);
						if (nRelHeight != 0)
						{
							nVarSoft = (int)FwTextPropVar.ktpvRelative;
							nValSoft = nRelHeight;
						}
						// By default, we have no min spacing; interpret this as single-space.
						else if (nValSoft == 0)
						{
							nVarSoft = (int)FwTextPropVar.ktpvRelative;
							nValSoft = (int)FwTextPropConstants.kdenTextPropRel;
						}
						// Otherwise interpret as absolute. Use the value we already.
						else
						{
							nVarSoft = (int)FwTextPropVar.ktpvMilliPoint;
						}
						break;
					case FwTextPropType.ktptBold:
						// For an inverting property, a value of invert is never redundant.
						if (nValHard == (int)FwTextToggleVal.kttvInvert)
						{
							continue;
						}
						var nWeight = vpsSoft.get_IntProperty(tpt);
						nValSoft = (nWeight > 550) ? (int)FwTextToggleVal.kttvInvert : (int)FwTextToggleVal.kttvOff;
						nVarSoft = (int)FwTextPropVar.ktpvEnum;
						break;
					case FwTextPropType.ktptItalic:
						// For an inverting property, a value of invert is never redundant.
						if (nValHard == (int)FwTextToggleVal.kttvInvert)
						{
							continue;
						}
						nValSoft = vpsSoft.get_IntProperty(tpt);
						nVarSoft = (int)FwTextPropVar.ktpvEnum;
						break;
					case FwTextPropType.ktptUnderline:
					case FwTextPropType.ktptSuperscript:
					case FwTextPropType.ktptRightToLeft:
					case FwTextPropType.ktptKeepWithNext:
					case FwTextPropType.ktptKeepTogether:
					case FwTextPropType.ktptWidowOrphanControl:
					case FwTextPropType.ktptAlign:
					case FwTextPropType.ktptBulNumScheme:
						nValSoft = vpsSoft.get_IntProperty(tpt);
						nVarSoft = (int)FwTextPropVar.ktpvEnum;
						break;
					case FwTextPropType.ktptFontSize:
					case FwTextPropType.ktptOffset:
					case FwTextPropType.ktptLeadingIndent:
					case FwTextPropType.ktptTrailingIndent:
					case FwTextPropType.ktptFirstIndent:
					case FwTextPropType.ktptSpaceBefore:
					case FwTextPropType.ktptSpaceAfter:
					case FwTextPropType.ktptBorderTop:
					case FwTextPropType.ktptBorderBottom:
					case FwTextPropType.ktptBorderLeading:
					case FwTextPropType.ktptBorderTrailing:
					case FwTextPropType.ktptMarginTop:
					case FwTextPropType.ktptPadTop:
					case FwTextPropType.ktptPadBottom:
					case FwTextPropType.ktptPadLeading:
					case FwTextPropType.ktptPadTrailing:
						nValSoft = vpsSoft.get_IntProperty(tpt);
						nVarSoft = (int)FwTextPropVar.ktpvMilliPoint;
						break;
					case FwTextPropType.ktptForeColor:
					case FwTextPropType.ktptBackColor:
					case FwTextPropType.ktptUnderColor:
					case FwTextPropType.ktptBorderColor:
					case FwTextPropType.ktptBulNumStartAt:
						nValSoft = vpsSoft.get_IntProperty(tpt);
						nVarSoft = (int)FwTextPropVar.ktpvDefault;
						break;
					default:
						// Ignore.
						continue;
				}

				if (nValHard == nValSoft && nVarHard == nVarSoft)
				{
					// Clear.
					if (tpb == null)
					{
						tpb = ttpHard.GetBldr();
					}
					tpb.SetIntPropValues(tpt, -1, -1);
				}
			}
			// String properties.
			ctpt = ttpHard.StrPropCount;
			for (var itpt = 0; itpt < ctpt; itpt++)
			{
				int tpt;
				var strHard = ttpHard.GetStrProp(itpt, out tpt);
				switch ((FwTextPropType)tpt)
				{
					case FwTextPropType.ktptFontFamily:
					case FwTextPropType.ktptWsStyle:
					case FwTextPropType.ktptFontVariations:
					case FwTextPropType.ktptBulNumTxtBef:
					case FwTextPropType.ktptBulNumTxtAft:
					case FwTextPropType.ktptBulNumFontInfo:
						break; // Process.
					default:
						// Ignore.
						continue;
				}
				var strSoft = vpsSoft.get_StringProperty(tpt);
				if (strHard == strSoft)
				{
					// Clear.
					if (tpb == null)
					{
						tpb = ttpHard.GetBldr();
					}
					tpb.SetStrPropValue(tpt, string.Empty);
				}
			}
			if (tpb != null)
			{
				// Something changed.
				ttpRet = tpb.GetTextProps();
				return true;
			}
			return false;
		}
		#endregion

		#region Selection methods
		/// <summary>
		/// Called from SimpleRootSite when the selection changes on its rootbox.
		/// </summary>
		/// <remarks>
		/// ENHANCE (FWR-1940): This should be protected internal instead of public
		/// </remarks>
		public virtual void SelectionChanged()
		{
			ClearCurrentSelection(); // Make sure the cached selection is updated

			var rootb = EditedRootBox;
			HandleSelectionChange(rootb, rootb.Selection);
		}

		/// <summary>
		/// Handles any changes that need to be done when a rootbox's selection changes.
		/// Change the system keyboard when the selection changes.
		/// </summary>
		public virtual void HandleSelectionChange(IVwRootBox rootb, IVwSelection vwselNew)
		{
			// Allow containing forms, etc. to do special handling when the selection changes.
			VwSelectionChanged?.Invoke(this, new VwSelectionArgs(rootb, vwselNew));
			if (!vwselNew.IsValid)
			{
				// Handling the selection change can sometimes invalidate the selection and
				// install a new one, in which case, we can get out.
				Debug.Assert(vwselNew != rootb.Selection, "Invalid selection");
				return;
			}
			// TimS/EberhardB: If we don't have focus we don't want to change the keyboard,
			// otherwise it might mess up the window that has focus!
			if (!Control.Focused)
			{
				return;
			}
			SetKeyboardForSelection(vwselNew);
		}

		/// <summary />
		private void SetWritingSystemPropertyFromSelection(IVwRootBox rootbox, IVwSelection selection)
		{
			// For now, we are only handling SimpleRootSite cases, e.g. for the Data Tree.
			// If we need this in print layout, consider adding the mediator to the Callbacks
			// interface.
			var rs = rootbox.Site as SimpleRootSite;
			if (rs?.PropertyTable == null || selection == null)
			{
				return;
			}
			// Review: Or should it be this? But it returns 0 if there are multiple ws's...
			// which may be good if the combo can handle it; i.e. there is no *one* ws so
			// we shouldn't show one in the combo
			var ws = SelectionHelper.GetWsOfEntireSelection(rootbox.Selection);
			var s = rs.PropertyTable.GetValue(FwUtils.FwUtils.WritingSystemHvo, "-1");
			var oldWritingSystemHvo = int.Parse(s);
			if (oldWritingSystemHvo == ws)
			{
				// The ws didn't change, so don't bother setting and broadcasting.
				return;
			}
			WritingSystemHvoChanged(ws);
			// As of 28JUN2019, there are only two known subscribers for "WritingSystemHvo":
			//	1. SimpleRootSite (which calls the method "WritingSystemHvoChanged", below, and
			//	2. WritingSystemListHandler, which updates the toolbar combobox to the newly selected WS.
			if (rs.PropertyTable.GetValue<string>(FwUtils.FwUtils.WritingSystemHvo) != ws.ToString())
			{
				rs.PropertyTable.SetProperty(FwUtils.FwUtils.WritingSystemHvo, ws.ToString(), doBroadcastIfChanged: true);
			}
		}

		internal void WritingSystemHvoChanged(int writingSystemHvo)
		{
			if (_lastWritingSystemProcessed == writingSystemHvo)
			{
				// Only do it once.
				return;
			}
			_lastWritingSystemProcessed = writingSystemHvo;
			// For now, we are only handling SimpleRootSite cases, e.g. for the Data Tree.
			// If we need this in print layout, consider adding the mediator to the Callbacks
			// interface.
			var simpleRootSite = m_callbacks as SimpleRootSite;
			// This property can be changed by selecting an item in the writing system combo.
			// When the user does this we try to update the writing system of the selection.
			// It also gets updated (in order to control the current item in the combo) when
			// the selection changes. We have to be careful this does not trigger an attempt to
			// modify the data.
			if ((simpleRootSite != null && !simpleRootSite.WasFocused()) || simpleRootSite?.RootBox?.Selection == null)
			{
				return; //e.g, the dictionary preview pane isn't focused and shouldn't respond.
			}
			simpleRootSite.Focus();
			// will get zero when the selection contains multiple ws's and the ws is
			// in fact different from the current one
			if (writingSystemHvo > 0 && writingSystemHvo != SelectionHelper.GetWsOfEntireSelection(simpleRootSite.RootBox.Selection))
			{
				ApplyWritingSystem(writingSystemHvo);
			}
		}

		/// <summary>
		/// Set the keyboard to match the writing system.
		/// </summary>
		public void SetKeyboardForWs(CoreWritingSystemDefinition ws)
		{
			if (Callbacks == null || ws == null)
			{
				ActivateDefaultKeyboard();
				return;
			}
			if (m_fSettingKeyboards)
			{
				return;
			}
			try
			{
				m_fSettingKeyboards = true;
				ws.LocalKeyboard?.Activate();
			}
			catch
			{
				ActivateDefaultKeyboard();
			}
			finally
			{
				m_fSettingKeyboards = false;
			}
		}

		/// <summary>
		/// Set the keyboard to match what is needed for the selection
		/// </summary>
		public void SetKeyboardForSelection(IVwSelection vwsel)
		{
			if (vwsel == null || Callbacks == null || !Callbacks.GotCacheOrWs)
			{
				return;         // Can't do anything useful, so let's not do anything at all.
			}
			var nWs = SelectionHelper.GetFirstWsOfSelection(vwsel);
			if (nWs == 0)
			{
				return;
			}
			CoreWritingSystemDefinition ws = null;
			var writingSystemManager = WritingSystemFactory as WritingSystemManager;
			if (writingSystemManager != null) // this sometimes happened in our tests when the window got/lost focus
			{
				ws = writingSystemManager.Get(nWs);
			}

			SetKeyboardForWs(ws);

			// Should also set the WS property. Otherwise the ws combo box doesn't get
			// updated when using tab key to go to the next field.
			SetWritingSystemPropertyFromSelection(Callbacks.EditedRootBox, vwsel);
		}

		/// <summary>
		/// Activates the default keyboard.
		/// </summary>
		private void ActivateDefaultKeyboard()
		{
			Keyboard.Controller.ActivateDefaultKeyboard();
		}

		/// <summary>
		/// Allow a change to BestStyleName not to cause all the work in the next function.
		/// </summary>
		public bool SuppressNextBestStyleNameChanged { get; set; }

		internal string BestStyleNameChanged(BaseStyleInfo newValue)
		{
			if (SuppressNextBestStyleNameChanged)
			{
				SuppressNextBestStyleNameChanged = false;
				return string.Empty;
			}
			// For now, we are only handling SimpleRootSite cases, e.g. for the Data Tree.
			// If we need this in print layout, figure out what to do.
			// The develop branch suggests: "consider adding the mediator to the Callbacks interface."
			var simpleRootSite = m_callbacks as SimpleRootSite;
			// This property can be changed by selecting an item in the combined styles combo.
			// When the user does this we try to update the style of the selection.
			// It also gets updated (in order to control the current item in the combo) when
			// the selection changes. We have to be careful this does not trigger an attempt to
			// modify the data.
			if (simpleRootSite != null && !simpleRootSite.WasFocused())
			{
				return string.Empty; //e.g, the dictionary preview pane isn't focused and shouldn't respond.
			}
			if (simpleRootSite?.RootBox?.Selection == null)
			{
				return string.Empty;
			}
			var styleName = newValue?.Name;
			if (styleName == null)
			{
				return string.Empty;
			}
			simpleRootSite.Focus();
			var paraStyleName = GetParaStyleNameFromSelection();
			var charStyleName = GetCharStyleNameFromSelection();
			if (styleName == string.Empty && charStyleName != string.Empty || (paraStyleName != styleName && charStyleName != styleName))
			{
				ApplyStyle(styleName);
			}
			return styleName; // Maybe changed the style, so let caller know.
		}

		/// <summary>
		/// Deal with losing focus
		/// </summary>
		/// <param name="newFocusedControl">The new focused control.</param>
		/// <param name="fIsChildWindow"><c>true</c> if the <paramref name="newFocusedControl"/>
		/// is a child window of the current application.</param>
		public void LostFocus(Control newFocusedControl, bool fIsChildWindow)
		{
			// Switch back to the UI keyboard so edit boxes in dialogs, toolbar controls, etc.
			// won't be using the UI of the current run in this view. But only if the current
			// focus pane is not another view...switching the input language AFTER another view
			// has received focus behaves like the user selecting the input language of this
			// view in the context of the other one, with bad consequences.
			if (ShouldRestoreKeyboardSwitchingTo(newFocusedControl, fIsChildWindow))
			{
				ActivateDefaultKeyboard();
			}
		}

		private static bool ShouldRestoreKeyboardSwitchingTo(Control newFocusedControl, bool fIsChildWindow)
		{
			// On Linux we want to restore the default keyboard if we're switching to another
			// application. On Windows the OS will take care of switching the keyboard.
			return Platform.IsUnix && newFocusedControl == null || !(newFocusedControl is IRootSite) && !(newFocusedControl is ISuppressDefaultKeyboardOnKillFocus) && fIsChildWindow;
		}

		/// <summary>
		/// Special processing to be done associated root site gets focus.
		/// </summary>
		internal void GotFocus()
		{
		}
		#endregion

		#region Cut/copy/paste handling methods
		/// <summary>
		/// Useful in stand-alone RootSites such as FwTextBox and LabeledMultiStringView to
		/// process cut/copy/paste keys in the OnKeyDown handler.
		/// </summary>
		/// <param name="e">The KeyEventArgs parameter</param>
		/// <returns>True if the key was handled, false if it was not</returns>
		public bool HandleOnKeyDown(KeyEventArgs e)
		{
			if (Control == null || !Control.Visible)
			{
				return false;
			}
			// look for Ctrl-X to cut
			if (e.KeyCode == Keys.X && e.Control)
			{
				if (!CopySelection())
				{
					return false;
				}
				// The copy succeeded (otherwise we would have got an exception and wouldn't be
				// here), now delete the range of text that has been copied to the
				// clipboard.
				DeleteSelection();
				return true;
			}
			// look for Ctrl-C to copy
			if (e.KeyCode == Keys.C && e.Control)
			{
				CopySelection();
				return true;
			}
			// Look for Ctrl-V to paste. After the paste, we need to make sure that no more
			// than one paragraph of data was pasted.
			return e.KeyCode == Keys.V && e.Control && PasteClipboard();
		}

		/// <summary>
		/// Copy the current selection to the clipboard
		/// </summary>
		/// <returns><c>true</c> if copying was successful, otherwise <c>false</c>.</returns>
		public bool CopySelection()
		{
			if (!CanCopy())
			{
				return false;
			}
			var vwsel = EditedRootBox.Selection;
			// Get a copy of the selection as a TsString, and store it in the clipboard, together with
			// the writing system factory.
			ITsString tss = null;
			var vwsite = EditedRootBox.Site as SimpleRootSite;
			if (vwsite != null)
			{
				tss = vwsite.GetTsStringForClipboard(vwsel);
			}
			if (tss == null)
			{
				vwsel.GetSelectionString(out tss, "; ");
			}
			// This is pathological for a range, but apparently it can happen, e.g., for a picture selection.
			// See LT-8147.
			if (tss == null || tss.Length == 0)
			{
				return false;
			}
			CopyTssToClipboard(tss);

			return true;
		}

		private const int MAX_RETRY = 3;
		private const int SLEEP_INTERVAL = 200; // 2/10ths of a second

		/// <summary>
		/// Put a ITsString on the clipboard
		/// </summary>
		public static void SetTsStringOnClipboard(ITsString tsString, bool fCopy, ILgWritingSystemFactory writingSystemFactory)
		{
			var wrapper = new TsStringWrapper(tsString, writingSystemFactory);
			IDataObject dataObject = new DataObject();
			dataObject.SetData(TsStringWrapper.TsStringFormat, false, wrapper);
			dataObject.SetData(DataFormats.Serializable, true, wrapper);
			dataObject.SetData(DataFormats.UnicodeText, true, tsString.Text.Normalize(NormalizationForm.FormC));
			// In some circumstances (especially with virtual machines) SetDataObject() could
			// throw an InteropServices.ExternalException. Catch and retry a few times.
			// If unsuccessful, tell the user the copy failed (LT-14822).
			try
			{
				ClipboardUtils.SetDataObject(dataObject, fCopy, MAX_RETRY, SLEEP_INTERVAL);
			}
			catch (ExternalException e)
			{
				MessageBox.Show(Resources.ksCopyFailed + e.Message);
			}
		}

		/// <summary>
		/// Gets a ITsString from the clipboard
		/// </summary>
		public ITsString GetTsStringFromClipboard(ILgWritingSystemFactory writingSystemFactory)
		{
			var dataObj = ClipboardUtils.GetDataObject();
			if (dataObj == null)
			{
				return null;
			}
			var wrapper = dataObj.GetData(TsStringWrapper.TsStringFormat) as TsStringWrapper;
			var tss = wrapper?.GetTsString(writingSystemFactory);
			if (tss == null)
			{
				return null;
			}
			var wsf = writingSystemFactory;
			int destWs;
			var pasteStatus = DeterminePasteWs(wsf, out destWs);
			switch (pasteStatus)
			{
				case PasteStatus.PreserveWs:
					break;
				case PasteStatus.UseDestWs:
					Debug.Assert(destWs > 0);
					return wrapper.GetTsStringUsingWs(destWs, writingSystemFactory);
				case PasteStatus.CancelPaste:
					return null;
			}
			return tss;
		}

		/// <summary>
		/// Copies the given ITsString to clipboard.
		/// </summary>
		/// <param name="tss">ITsString to copy to clipboard.</param>
		internal void CopyTssToClipboard(ITsString tss)
		{
			// if this asserts it is likely that
			// the user selected a footnote marker but the TextRepOfObj() method isn't
			// implemented.
			Debug.Assert(tss != null && tss.Length > 0);

			SetTsStringOnClipboard(tss, false, WritingSystemFactory);
		}

		/// <summary>
		/// Call DeleteSelection, wrapping it in an Undo task.
		/// Typically overridden to make a proper UOW in RootSiteEditingHelper.
		/// </summary>
		protected virtual void DeleteSelectionTask(string undoLabel, string redoLabel)
		{
			Callbacks.EditedRootBox.DataAccess.GetActionHandler().BeginUndoTask(undoLabel, redoLabel);
			DeleteSelection();
			Callbacks.EditedRootBox.DataAccess.GetActionHandler().EndUndoTask();
		}

		/// <summary>
		/// Cuts the current selection
		/// </summary>
		/// <returns><c>true</c> if cutting was successful, otherwise <c>false</c>.</returns>
		public bool CutSelection()
		{
			try
			{
				if (!Editable || !CopySelection())
				{
					return false;
				}
				// The copy succeeded (otherwise we would have got an exception and wouldn't be
				// here), now delete the range of text that has been copied to the
				// clipboard.
				DeleteSelectionTask(Resources.ksUndoCut, Resources.ksRedoCut);
				return true;
			}
			catch (Exception ex)
			{
				MessageBox.Show(string.Format(Resources.ksCutFailed, ex.Message), Resources.ksCutFailedCaption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return false;
			}
		}

		/// <summary>
		/// Delete the current selection. Caller is responsible for UOW. Actually simulates
		/// pressing the delete key; will delete one char forward if selection is an IP.
		/// </summary>
		/// <returns><c>true</c> if deleting was successful, otherwise <c>false</c>.</returns>
		public void DeleteSelection()
		{
			if (Control == null || m_callbacks == null || m_callbacks.EditedRootBox == null || !m_callbacks.GotCacheOrWs)
			{
				return;
			}
			HandleKeyPress((char)(int)VwSpecialChars.kscDelForward, Keys.None);
		}

		/// <summary>
		/// Answer true if the given selection is in a single property (it may be an IP or range),
		/// and that property is editable.
		/// </summary>
		protected bool IsSelectionInOneEditableProp(IVwSelection sel)
		{
			if (sel == null || sel.SelType != VwSelType.kstText || !sel.IsEditable)
			{
				return false;
			}
			int ichE, ichA, hvoObjE, hvoObjA, tagE, tagA, wsE, wsA;
			ITsString tssE, tssA;
			bool fAssocPrev;
			sel.TextSelInfo(true, out tssE, out ichE, out fAssocPrev, out hvoObjE, out tagE, out wsE);
			sel.TextSelInfo(false, out tssA, out ichA, out fAssocPrev, out hvoObjA, out tagA, out wsA);
			if (hvoObjE != hvoObjA || tagE != tagA || wsE != wsA)
			{
				return false;
			}
			var cLevA = sel.CLevels(false);
			var cLevE = sel.CLevels(true);
			if (cLevA != cLevE)
			{
				return false;
			}
			for (var i = 0; i < cLevA - 1; i++)
			{
				int ihvoA, ihvoE, cPropPreviousA, cPropPreviousE;
				IVwPropertyStore vps;
				sel.PropInfo(true, i, out hvoObjE, out tagE, out ihvoE, out cPropPreviousE, out vps);
				sel.PropInfo(false, i, out hvoObjA, out tagA, out ihvoA, out cPropPreviousA, out vps);
				if (hvoObjE != hvoObjA || tagE != tagA || ihvoE != ihvoA || cPropPreviousA != cPropPreviousE)
				{
					return false;
				}
			}

			return sel.CanFormatChar;
		}

		/// <summary>
		/// Answer true if the selection is in a single property (it may be an IP or range),
		/// and that property is editable.
		/// </summary>
		public bool IsSelectionInOneEditableProp()
		{
			if (m_callbacks?.EditedRootBox == null)
			{
				return false;
			}
			return IsSelectionInOneEditableProp(m_callbacks.EditedRootBox.Selection);
		}

		/// <summary>
		/// This is stronger than IsSelectionInOneEditableProp, in addition, it must be a
		/// property that can really store formatting information
		/// </summary>
		public bool IsSelectionInOneFormattableProp()
		{
			if (!IsSelectionInOneEditableProp())
			{
				return false;
			}
			if (m_callbacks?.EditedRootBox == null)
			{
				return false;
			}
			var sel = m_callbacks.EditedRootBox.Selection;
			int ich, hvoObj, tag, ws;
			bool fAssocPrev;
			ITsString tss;
			sel.TextSelInfo(true, out tss, out ich, out fAssocPrev, out hvoObj, out tag, out ws);
			var sda = m_callbacks.EditedRootBox.DataAccess;
			var mdc = sda.MetaDataCache;
			if (mdc == null)
			{
				return true; // no further info, assume OK.
			}
			if (tss == null || tag == 0)
			{
				return false; // No string to check.
			}
			if (IsUnknownProp(mdc, tag))
			{
				return false;
			}
			var cpt = (CellarPropertyType)mdc.GetFieldType(tag);
			// These four types can store embedded formatting.
			return cpt == CellarPropertyType.String || cpt == CellarPropertyType.MultiString;
		}

		/// <summary>
		/// Return true if this is a property the mdc can't tell us about.
		/// This is overridden in RootSite, where we can cast it to IFwMetaDataCacheManaged and really find out.
		/// </summary>
		protected virtual bool IsUnknownProp(IFwMetaDataCache mdc, int tag)
		{
			return false;
		}

		/// <summary>
		/// Get the clipboard contents as a string, or return null string if not found
		/// </summary>
		public string GetClipboardAsString()
		{
			try
			{
				return (string)ClipboardUtils.GetDataObject().GetData(DataFormats.StringFormat);
			}
			catch
			{
				return null;
			}
		}

		/// <summary>
		/// Paste data from the clipboard into the view. Caller is responsible to make UOW.
		/// </summary>
		/// <returns>True if the paste succeeded, false otherwise</returns>
		public virtual bool PasteClipboard()
		{
			// Do nothing if command is not enabled. Needed for Ctrl-V keypress.
			if (!CanPaste() || Callbacks?.EditedRootBox == null || !Callbacks.GotCacheOrWs || CurrentSelection == null)
			{
				return false;
			}
			var vwsel = CurrentSelection.Selection;
			if (vwsel == null)
			{
				return false;
			}
			// Handle anything needed immediately before the paste.
			Callbacks.PrePasteProcessing();

			ITsTextProps[] vttp;
			IVwPropertyStore[] vvps;
			int cttp;
			SelectionHelper.GetSelectionProps(vwsel, out vttp, out vvps, out cttp);
			if (cttp == 0)
			{
				return false;
			}
			ChangeStyleForPaste(vwsel, ref vttp);
			return PasteCore(GetTextFromClipboard(vwsel, vwsel.CanFormatChar, vttp[0]));
		}

		/// <summary>
		/// Core logic for Paste command (without modifying the clipboard, to support testing).
		/// </summary>
		/// <param name="tss">The TsString to paste.</param>
		/// <returns>True if that paste succeeded, false otherwise</returns>
		public bool PasteCore(ITsString tss)
		{
			var vwsel = EditedRootBox.Selection;
			try
			{
				if (tss != null)
				{
					// At this point, we may need to override internal formatting values
					// for certain target rootsites. We do this with an event handler that the
					// rootsite can register for its editing helper. (See LT-1445.)
					if (PasteFixTssEvent != null)
					{
						var args = new FwPasteFixTssEventArgs(tss, new TextSelInfo(EditedRootBox));
						PasteFixTssEvent(this, args);
						tss = args.TsString;
					}
					// Avoid possible crashes if we know we can't paste.  (See LT-11150 and LT-11219.)
					if (vwsel == null || !vwsel.IsValid || tss == null)
					{
						FwUtils.FwUtils.ErrorBeep();
						return false;
					}
					// ENHANCE (FWR-1732):
					// Will need to split current UOW into 2 when paste involves deleting one or more paragraphs
					// because of a range selection - the delete processing requests a new selection at the end of the
					// UOW which destroys the current selection and makes the paste fail. However, don't want to
					// re-introduce FWR-1734 where delete succeeds and when paste fails. Would need to do undo to
					// restore deleted text, but may seem strange to user since a redo task will now appear.
					vwsel.ReplaceWithTsString(TsStringUtils.RemoveIllegalXmlChars(tss));
				}
			}
			catch (Exception e)
			{
				if (e is COMException && (uint)((COMException)e).ErrorCode == 0x80004005) // E_FAIL
				{
					FwUtils.FwUtils.ErrorBeep();
					return false;
				}
				Logger.WriteError(e); // TE-6908/LT-6781
				throw new ContinuableErrorException("Error during paste. Paste has been undone.", e);
			}
			EditedRootBox.Site.ScrollSelectionIntoView(null, VwScrollSelOpts.kssoDefault);
			return true;
		}

		/// <summary>
		/// Gets the text from clipboard.
		/// </summary>
		/// <param name="vwsel">The selection.</param>
		/// <param name="fCanFormat">set to <c>true</c> to allow hard formatting in the text,
		/// otherwise <c>false</c>.</param>
		/// <param name="ttpSel">The text properties of the selection.</param>
		/// <returns>Text from clipboard</returns>
		protected ITsString GetTextFromClipboard(IVwSelection vwsel, bool fCanFormat, ITsTextProps ttpSel)
		{
			var tss = GetTsStringFromClipboard(WritingSystemFactory);
			if (tss != null && !ValidToReplaceSelWithTss(vwsel, tss))
			{
				tss = null;
			}
			if (!fCanFormat && tss != null)
			{
				// remove formatting from the TsString
				var str = tss.Text;
				tss = TsStringUtils.MakeString(str, ttpSel);
			}
			if (tss == null)
			{   // all else didn't work, so try with an ordinary string
				var str = ClipboardUtils.GetText();
				tss = TsStringUtils.MakeString(str, ttpSel);
			}
			return tss;
		}

		/// <summary>
		/// Determines whether the given TsString can be pasted in the context of the target
		/// selection. Applications can override this to enforce specific exceptions.
		/// </summary>
		/// <param name="vwselTargetLocation">selection to be replaced by paste operation</param>
		/// <param name="tss">The TsString from the clipboard</param>
		/// <returns>Base implementation always returns <c>true</c></returns>
		protected virtual bool ValidToReplaceSelWithTss(IVwSelection vwselTargetLocation, ITsString tss)
		{
			return true;
		}

		/// <summary>
		/// Used to change the style at a paste location right before the paste begins
		/// </summary>
		protected virtual void ChangeStyleForPaste(IVwSelection vwsel, ref ITsTextProps[] vttp)
		{
			// default is to do nothing
		}

		/// <summary>
		/// Determine if the deleting of text is possible.
		/// </summary>
		/// <returns>Returns <c>true</c> if cutting is possible.</returns>
		public virtual bool CanDelete()
		{
			if (Callbacks?.EditedRootBox != null && Editable)
			{
				var vwsel = Callbacks.EditedRootBox.Selection;
				if (vwsel != null)
				{
					// CanFormatChar is true only if the selected text is editable.
					// TE-5774 Added VwSelType.kstPicture selection type comparison because
					//	delete was being disabled for pictures
					return (vwsel.CanFormatChar || vwsel.SelType == VwSelType.kstPicture);
				}
			}
			return false;
		}

		/// <summary>
		/// Determine if the cutting of text into the clipboard is possible.
		/// </summary>
		/// <returns>Returns <c>true</c> if cutting is possible.</returns>
		/// <remarks>Formerly <c>AfVwRootSite::CanCut()</c>.</remarks>
		public virtual bool CanCut()
		{
			if (Callbacks != null && Callbacks.GotCacheOrWs && Callbacks.EditedRootBox != null && Editable)
			{
				var vwsel = Callbacks.EditedRootBox.Selection;
				if (vwsel != null)
				{
					// CanFormatChar is true only if the selected text is editable.
					return vwsel.IsRange && vwsel.CanFormatChar;
				}
			}
			return false;
		}

		/// <summary>
		/// Determine if the copying of text into the clipboard is possible.
		/// </summary>
		/// <returns>Returns <c>true</c> if copying is possible.</returns>
		public virtual bool CanCopy()
		{
			if (Callbacks != null && Callbacks.GotCacheOrWs && Callbacks.EditedRootBox != null)
			{
				var vwsel = Callbacks.EditedRootBox.Selection;
				if (vwsel == null || !vwsel.IsRange || !vwsel.IsValid)
				{
					return false; // No text selected.
				}
				int cttp;
				vwsel.GetSelectionProps(0, ArrayPtr.Null, ArrayPtr.Null, out cttp);
				// No text selected.
				return cttp != 0;
			}
			return false;
		}

		/// <summary />
		/// <returns>true if we are in an editable location</returns>
		public virtual bool CanEdit()
		{
			if (Callbacks != null && Callbacks.EditedRootBox != null)
			{
				return CanEdit(Callbacks.EditedRootBox.Selection);
			}
			return false;
		}

		/// <summary />
		/// <returns>true if editing would be possible for the specified selection</returns>
		public virtual bool CanEdit(IVwSelection vwsel)
		{
			if (Editable)
			{
				// CanFormatChar is true only if the selected text is editable.
				if (vwsel != null)
				{
					// When "&& vwsel.CanFormatChar" is included in the conditional,
					//  it causes a problem in TE (TE-3339)
					// But (JohnT, 18 Nov), we want to be able to get some idea, especially
					// for insertion points, since this is used to help control mouse pointers.
					return vwsel.IsRange || vwsel.IsEditable;
				}
				return false;
			}
			return false;
		}

		/// <summary />
		public bool ClipboardContainsString()
		{
			try
			{
				// Get the type of object on the clipboard, and check whether it is compatible
				// with being pasted into text.
				return ClipboardUtils.ContainsText();
			}
			catch
			{
				// This can happen if the clipboard is in some unknown state.  .Net
				// throws an error instead of returning something useful :)
				// (fixes TE-1717)
				return false;
			}
		}

		/// <summary>
		/// Determine if pasting of text from the clipboard is possible.
		/// </summary>
		/// <returns>Returns <c>true</c> if pasting is possible.</returns>
		/// <remarks>Formerly <c>AfVwRootSite::CanPaste()</c>.</remarks>
		public virtual bool CanPaste()
		{
			if (m_callbacks?.EditedRootBox != null && Editable && CurrentSelection != null && Control != null && Control.Visible)
			{
				var vwsel = CurrentSelection.Selection;
				// CanFormatChar is true only if the selected text is editable.
				if (vwsel != null && vwsel.CanFormatChar)
				{
					return ClipboardContainsString();
				}
			}
			return false;
		}

		/// <summary>
		/// Make a selection that includes all the text
		/// </summary>
		public void SelectAll()
		{
			if (m_callbacks == null || m_callbacks.EditedRootBox == null || !m_callbacks.GotCacheOrWs || Control == null)
			{
				return;
			}
			Control.Focus();
			var rootb = m_callbacks.EditedRootBox;

			using (new WaitCursor(Control))
			{
				// creates a wait cursor and makes it active until the end of the block.
				// Due to some code we don't understand in the arrow key functions, this simulating
				// control-end has no effect unless this pane has focus. So don't use this old approach.
				//rootb.MakeSimpleSel(true, false, false, true);
				//// Simulate a Ctrl-Shift-End keypress:
				//rootb.OnExtendedKey((int)Keys.End, VwShiftStatus.kgrfssShiftControl,
				//    1); // logical arrow key behavior
				var selStart = rootb.MakeSimpleSel(true, false, false, false);
				var selEnd = rootb.MakeSimpleSel(false, false, false, false);
				if (selStart != null && selEnd != null)
				{
					rootb.MakeRangeSelection(selStart, selEnd, true);
				}
			}
		}

		/// <summary>
		/// Get a value determining if the new writing systems should be created as a side-effect
		/// of a paste operation.
		/// </summary>
		/// <param name="wsf">writing system factory containing the new writing systems</param>
		/// <param name="destWs">The destination writing system (writing system used at the
		/// selection).</param>
		/// <returns>
		/// 	an indication of how the paste should be handled.
		/// </returns>
		public virtual PasteStatus DeterminePasteWs(ILgWritingSystemFactory wsf, out int destWs)
		{
			destWs = -1;
			return PasteStatus.PreserveWs;
		}
		#endregion
	}
}