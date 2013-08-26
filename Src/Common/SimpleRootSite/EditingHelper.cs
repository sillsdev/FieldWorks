//---------------------------------------------------------------------------------------------
#region /// Copyright (c) 2002-2008, SIL International. All Rights Reserved.
// <copyright from='2002' to='2008' company='SIL International'>
//    Copyright (c) 2008, SIL International. All Rights Reserved.
// </copyright>
#endregion
//
// File: EditingHelper.cs
// Responsibility: TE Team
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Palaso.UI.WindowsForms.Keyboarding;
using Palaso.WritingSystems;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites.Properties;
using SIL.Utils;

namespace SIL.FieldWorks.Common.RootSites
{
	#region IEditingCallbacks interface
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This interface, implemented currently by SimpleRootSite and PublicationControl,
	/// defines the functions that are not inherited from UserControl which must be
	/// implemented by the EditingHelper client. One argument to the constructor for
	/// EditingHelper is an IEditingCallbacks. It must be capable of being cast to
	/// UserControl.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IEditingCallbacks
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// See the comments on m_wsPending for SimpleRootSite. Used to manage
		/// writing system changes caused by selecting a system input language.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		int WsPending { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Typically the AutoScollPosition of the control, SimpleRootSite
		/// handles this specially.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		Point ScrollPosition { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return an indication of the behavior of some of the special keys (arrows, home,
		/// end).
		/// </summary>
		/// <param name="chw">Key value</param>
		/// <param name="ss">Shift status</param>
		/// <returns>Return <c>0</c> for physical behavior, <c>1</c> for logical behavior.
		/// </returns>
		/// <remarks>Physical behavior means that left arrow key goes to the left regardless
		/// of the direction of the text; logical behavior means that left arrow key always
		/// moves the IP one character (possibly plus diacritics, etc.) in the underlying text,
		/// in the direction that is to the left for text in the main paragraph direction.
		/// So, in a normal LTR paragraph, left arrow decrements the IP position; in an RTL
		/// paragraph, it increments it. Both produce a movement to the left in text whose
		/// direction matches the paragraph ("downstream" text). But where there is a segment
		/// of upstream text, logical behavior will jump almost to the other end of the
		/// segment and then move the 'wrong' way through it.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		EditingHelper.CkBehavior ComplexKeyBehavior(int chw, VwShiftStatus ss);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Scroll all the way to the top of the document.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void ScrollToTop();
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Scroll all the way to the end of the document.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void ScrollToEnd();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Show the context menu for the specified root box at the location of
		/// its selection (typically an IP).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void ShowContextMenuAtIp(IVwRootBox rootb);
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the (estimated) height of one line
		/// </summary>
		/// ------------------------------------------------------------------------------------
		int LineHeight { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// RootBox currently being edited.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		IVwRootBox EditedRootBox { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Flag indicating cache or writing system is available.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		bool GotCacheOrWs { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the writing system for the HVO. This could either be the vernacular or
		/// analysis writing system.
		/// </summary>
		/// <param name="hvo">HVO</param>
		/// <returns>Writing system</returns>
		/// ------------------------------------------------------------------------------------
		int GetWritingSystemForHvo(int hvo);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Perform any processing needed immediately prior to a paste operation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void PrePasteProcessing();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If we need to make a selection, but we can't because edits haven't been updated in
		/// the view, this method requests creation of a selection after the unit of work is
		/// complete. It will also scroll the selection into view.
		/// Derived classes should implement this if they have any hope of supporting multi-
		/// paragraph editing.
		/// </summary>
		/// <param name="helper">The selection to restore</param>
		/// ------------------------------------------------------------------------------------
		void RequestVisibleSelectionAtEndOfUow(SelectionHelper helper);
	}
	#endregion

	#region FwPasteFixTssEvent handler and args class
	/// <summary></summary>
	public delegate void FwPasteFixTssEventHandler(EditingHelper sender, FwPasteFixTssEventArgs e);
	/// <summary>
	/// This event argument class is used for fixing the text properties of Pasted text in
	/// EditingHelper objects whose owning SimpleRootSite object requires specific properties.
	/// See LT-1445 for motivation.  Other final adjustments to the ITsString value
	/// may also be made if there's any such need.  The handler function is called just before
	/// replacing the selection in the root box with the given ITsString.
	/// </summary>
	public class FwPasteFixTssEventArgs
	{
		private readonly TextSelInfo m_tsi;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:FwPasteFixWsEventArgs"/> class.
		/// </summary>
		/// <param name="tss">The ITsString to paste.</param>
		/// <param name="tsi">The TextSelInfo of the selection at the start of the paste.</param>
		/// ------------------------------------------------------------------------------------
		public FwPasteFixTssEventArgs(ITsString tss, TextSelInfo tsi)
		{
			TsString = tss;
			m_tsi = tsi;
			EventHandled = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the TsString to paste (handlers can modify this).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ITsString TsString { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The TextSelInfo of the selection at the start of the paste
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public TextSelInfo TextSelInfo
		{
			get { return m_tsi; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether the event was handled.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool EventHandled { get; set; }
	}
	#endregion

	#region EditingHelper class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This class encapsulates some of the common behavior of SimpleRootSite and
	/// PublicationControl that has to do with forwarding keyboard events to the
	/// root box that has focus.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class EditingHelper : IFWDisposable, ISelectionChangeNotifier
	{
		#region Events
		/// <summary>
		/// Event handler for specialized work when the selection changes.
		/// </summary>
		public event EventHandler<VwSelectionArgs> VwSelectionChanged;
		#endregion

		#region Member variables
		private UserControl m_control; // currently either SimpleRootSite or PublicationControl.
		/// <summary>Object that provides editing callback methods (in production code, this is usually (always?) the rootsite)</summary>
		protected IEditingCallbacks m_callbacks;
		/// <summary>The default cursor to use</summary>
		private Cursor m_defaultCursor;

		/// <summary>
		/// This overrides the normal Ibeam cursor when over text (not when over hot links or
		/// hot pictures) if the cursor is over something that can't be edited.
		/// </summary>
		private Cursor m_readOnlyCursor;
		/// <summary>True if editing commands should be handled, false otherwise</summary>
		private bool m_fEditable = true;
		/// <summary>A SelectionHelper that holds the info for the current selection (updated
		/// every time the selection changes) Protected to allow for testing - production
		/// subclasses should not access this member directly</summary>
		protected SelectionHelper m_currentSelection;
		/// <summary>Flag to prevent deletion of an object</summary>
		protected bool m_preventObjDeletions;

		/// <summary>Event for changing properties of a pasted TsString</summary>
		public event FwPasteFixTssEventHandler PasteFixTssEvent;

		private bool m_fSuppressNextWritingSystemHvoChanged;
		private bool m_fSuppressNextBestStyleNameChanged;
		#endregion

		#region Enumerations
		/// <summary>Paste status indicates how writing systems should be handled during a paste</summary>
		public enum PasteStatus
		{
			/// <summary>When pasting, use the writing system at the destination</summary>
			UseDestWs,
			/// <summary>When pasting, preserve the original writing systems, even if new writing systems
			/// would need to be created.</summary>
			PreserveWs,
			/// <summary>Cancel paste operation.</summary>
			CancelPaste
		}
		/// <summary>The action that initiated creation of the <see cref="WordEventArgs"/></summary>
		private enum WordEventSource
		{
			/// <summary>View loses focus</summary>
			LoseFocus,
			/// <summary>User clicked with the mouse</summary>
			MouseClick,
			/// <summary>User pressed any button</summary>
			KeyDown,
			/// <summary>User entered a character</summary>
			Character,
		}
		/// <summary>Behavior of certain keys like arrow key, home, end...</summary>
		/// <see cref="SimpleRootSite.ComplexKeyBehavior"/>
		public enum CkBehavior
		{
			/// <summary>Physical order</summary>
			Physical = 0,
			/// <summary>Logical order</summary>
			Logical = 1
		}
		#endregion // Enumerations

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This constructor is for testing so the class can be mocked.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public EditingHelper() : this(null)
		{
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Construct one.
		/// </summary>
		/// <param name="callbacks"></param>
		/// -----------------------------------------------------------------------------------
		public EditingHelper(IEditingCallbacks callbacks)
		{
			m_callbacks = callbacks;
			m_control = callbacks as UserControl;
		}

		#region IDisposable & Co. implementation
		// Region last reviewed: never

		/// <summary>
		/// True, if the object has been disposed.
		/// </summary>
		private bool m_isDisposed = false;

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed
		{
			get { return m_isDisposed; }
		}

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

		/// <summary>
		///
		/// </summary>
		/// <remarks>Must not be virtual.</remarks>
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
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
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + "******************");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_control = null;
			m_callbacks = null;
			m_currentSelection = null;
			// Don't do this here...causes TsStrings not to copy and paste properly from one view
			// to another in Flex (and elsewhere).
			//ClearTsStringClipboard();

			m_isDisposed = true;
		}

		/// <summary>
		/// Throw if the IsDisposed property is true
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		#endregion IDisposable & Co. implementation

		#region Writing system methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get in the vector the list of writing system identifiers currently installed in the
		/// writing system factory for the current root box. The current writing system for the
		/// selection is duplicated as the first item in the array (this causes it to be found
		/// first in searches).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public List<int> GetWsList(out ILgWritingSystemFactory wsf)
		{
			CheckDisposed();
			// Get the writing system factory associated with the root box.
			wsf = WritingSystemFactory;
			int cws = wsf.NumberOfWs;
			if (cws == 0)
				return null;
			using (ArrayPtr ptr = MarshalEx.ArrayToNative<int>(cws))
			{
				wsf.GetWritingSystems(ptr, cws);
				int[] vwsT = MarshalEx.NativeToArray<int>(ptr, cws);
				if (cws == 1 && vwsT[0] == 0)
					return null;	// no writing systems to work with
				return new List<int>(vwsT);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get in the vector the list of writing system identifiers currently installed in the
		/// writing system factory for the current root box. The current writing system for the
		/// selection is duplicated as the first item in the array (this causes it to be found
		/// first in searches).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public List<int> GetWsListCurrentFirst(IVwSelection vwsel,
			out ILgWritingSystemFactory wsf)
		{
			CheckDisposed();
			List<int> writingSystems = GetWsList(out wsf);
			if (writingSystems != null)
			{

				// Put the writing system of the selection first in the list, which gives it
				// priority--we'll find it first if it matches.
				int wsSel = SelectionHelper.GetFirstWsOfSelection(vwsel);
				if (vwsel != null && wsSel != 0)
				{
					writingSystems.Insert(0, wsSel);
				}
				else
				{
					writingSystems.Insert(0, writingSystems[0]);
				}
			}
			return writingSystems;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Set the writing system of the current selection.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public void ApplyWritingSystem(int hvoWsNew)
		{
			CheckDisposed();
			if(Callbacks == null || Callbacks.EditedRootBox == null)
				return;

			IVwSelection vwsel = Callbacks.EditedRootBox.Selection;

			ITsTextProps[] vttp;
			IVwPropertyStore[] vvps;
			int cttp;

			SelectionHelper.GetSelectionProps(vwsel, out vttp, out vvps, out cttp);

			bool fChanged = false;
			for (int ittp = 0; ittp < cttp; ++ittp)
			{
				int hvoWsOld, var;
				ITsTextProps ttp = vttp[ittp];
				// Change the writing system only if it is different and not a user prompt.
				hvoWsOld = ttp.GetIntPropValues((int)FwTextPropType.ktptWs, out var);
				if (ttp.GetIntPropValues(SimpleRootSite.ktptUserPrompt, out var) == -1 &&
					hvoWsOld != hvoWsNew)
				{
					ITsPropsBldr tpb = ttp.GetBldr();
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Changes the writing system.
		/// </summary>
		/// <param name="sel">The selection.</param>
		/// <param name="props">The properties specifying the new writing system.</param>
		/// <param name="numProps">The number of ITsTextProps.</param>
		/// ------------------------------------------------------------------------------------
		protected virtual void ChangeWritingSystem(IVwSelection sel, ITsTextProps[] props, int numProps)
		{
			Debug.Assert(sel != null);
			Debug.Assert(props != null);

			sel.SetSelectionProps(numProps, props);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines if all the writing systems in the given writing system factory are
		/// defined in the writing system factory of this editing helper.
		/// </summary>
		/// <param name="wsf">The given writing system factory.</param>
		/// <returns><c>true</c> if all writing systems are defined; <c>false</c> otherwise
		/// </returns>
		/// ------------------------------------------------------------------------------------
		protected bool AllWritingSystemsDefined(ILgWritingSystemFactory wsf)
		{
			// Check to see if all writing systems are defined.
			int cws = wsf.NumberOfWs;

			using (ArrayPtr ptr = MarshalEx.ArrayToNative<int>(cws))
			{
				wsf.GetWritingSystems(ptr, cws);
				int[] vws = MarshalEx.NativeToArray<int>(ptr, cws);

				ILgWritingSystem ws;
				for (int iws = 0; iws < cws; iws++)
				{
					if (vws[iws] == 0)
						continue;
					ws = wsf.get_EngineOrNull(vws[iws]);
					if (ws == null || WritingSystemFactory.GetWsFromStr(ws.Id) == 0)
						return false; // found writing system not in current project
				}
			}

			return true;
		}

		#endregion

		#region Character processing methods
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Handle a WM_CHAR message.
		/// Caller should ensure this is wrapped in a UOW (typically done in an override of
		/// OnKeyPress in RootSiteEditingHelper, since SimpleRootSite does not have access
		/// to FDO and UOW).
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public virtual void OnKeyPress(KeyPressEventArgs e, Keys modifiers)
		{
			CheckDisposed();

			if (!IsIgnoredKey(e, modifiers) && CanEdit()) // Only process keys that aren't ignored
				HandleKeyPress(e.KeyChar, modifiers);
		}

		// (EberhardB): This code is reimplementing System.Windows.Forms code. See comment on
		// OnKeyPress().
		// This comment is part of the fix for LT-9049.
		///// ------------------------------------------------------------------------------------
		///// <summary>
		///// Get the next control on the parent form
		///// </summary>
		///// <param name="control">The control.</param>
		///// <param name="fForward">true to look forward; false to look backward</param>
		///// <returns>
		///// The next control on the owning form which is is a tab stop. If no owning
		///// form is found or if no other tab-stop controls exist, <c>this</c> control will be
		///// returned.
		///// </returns>
		///// ------------------------------------------------------------------------------------
		//public static Control NextTabStop(Control control, bool fForward)
		//{
		//    Form parentForm = control.FindForm();
		//    if (parentForm != null)
		//    {
		//        Set<Control> visited = new Set<Control>();
		//        Control nextControl = control;
		//        visited.Add(nextControl);
		//        do
		//        {
		//            nextControl = parentForm.GetNextControl(nextControl, fForward);
		//            if (nextControl != null)
		//            {
		//                if (visited.Contains(nextControl))
		//                    break;
		//                visited.Add(nextControl);
		//            }
		//            if (nextControl != null && nextControl.Enabled &&
		//                nextControl.TabStop && nextControl.Visible &&
		//                // when looking backwards, the first control found will be the parent
		//                // of the current control. This causes our own control to get selected so
		//                // keep looking
		//                (fForward || control.TabIndex != 0 || !IsParentOf(control, nextControl)))
		//            {
		//                return nextControl;
		//            }
		//        } while (true);
		//    }
		//    return control;
		//}

		///// <summary>
		///// Answer true if possibleParent is a parent (even indirectly) of child
		///// </summary>
		///// <param name="child"></param>
		///// <param name="possibleParent"></param>
		///// <returns></returns>
		//private static bool IsParentOf(Control child, Control possibleParent)
		//{
		//    Control c = child.Parent;
		//    while (c != null)
		//    {
		//        if (c == possibleParent)
		//            return true;
		//        c = c.Parent;
		//    }
		//    return false;
		//}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// User pressed a key.
		/// </summary>
		/// <param name="e"></param>
		/// <returns><c>true</c> if we handled the key, <c>false</c> otherwise (e.g. we're
		/// already at the end of the rootbox and the user pressed down arrow key).</returns>
		/// -----------------------------------------------------------------------------------
		public virtual bool OnKeyDown(KeyEventArgs e)
		{
			CheckDisposed();
			if (Callbacks == null || Callbacks.EditedRootBox == null)
				return true;

			bool fRet = true;
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
					VwShiftStatus ss = GetShiftStatus(e.Modifiers);
					if (e.KeyCode == Keys.Enter && (ss == VwShiftStatus.kfssShift || !CanEdit()))
						return fRet;

					int keyVal = e.KeyValue;
					if (Control is SimpleRootSite)
						keyVal = ((SimpleRootSite)Control).ConvertKeyValue(keyVal);
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
						return fRet;
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
						Callbacks.ShowContextMenuAtIp(Callbacks.EditedRootBox);
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
						keyVal = (Control as SimpleRootSite).ConvertKeyValue(keyVal);
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks input characters to see if they should be processsed. Static to allow
		/// function to be shared with PublicationControl.
		/// </summary>
		/// <param name="e"></param>
		/// <param name="modifiers">Control.ModifierKeys</param>
		/// <returns><code>true</code> if character should be ignored on input</returns>
		/// ------------------------------------------------------------------------------------
		public static bool IsIgnoredKey(KeyPressEventArgs e, Keys modifiers)
		{
			bool ignoredKey = false;

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
			if (e.KeyChar < 0x20 && e.KeyChar != '\r' && e.KeyChar != '\b')
				return true;

			return ignoredKey;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Returns <c>true</c> if the action ends a word
		/// </summary>
		/// <remarks>The default implementation ends a word when losing the focus,
		/// when the user clicks with the mouse, when the user presses one of the cursor,
		/// page-up/down, home, end, backspace, del keys, or when he entered a non-wordforming
		/// character</remarks>
		/// <param name="args">Information about what action happened and what key
		/// was pressed</param>
		/// <returns><c>true</c> if the action ended a word, otherwise <c>false</c></returns>
		/// -----------------------------------------------------------------------------------
		private bool IsWordBreak(WordEventArgs args)
		{
			switch (args.Source)
			{
			case WordEventSource.LoseFocus:
			case WordEventSource.MouseClick:
				return true;
			case WordEventSource.KeyDown:
			{
				switch (args.Key)
				{
				case Keys.Left:
				case Keys.Up:
				case Keys.Right:
				case Keys.Down:
				case Keys.PageDown:
				case Keys.PageUp:
				case Keys.End:
				case Keys.Home:
				case Keys.Delete:
				case Keys.Back:
					return true;
				default:
					return false;
				}
			}
			case WordEventSource.Character:
			{
				ILgCharacterPropertyEngine charProps = null;
				try
				{
					charProps = LgIcuCharPropEngineClass.Create();
					return !charProps.get_IsWordForming(args.Char);
				}
				finally
				{
					if (charProps != null && Marshal.IsComObject(charProps))
						Marshal.ReleaseComObject(charProps);
				}
			}
			}
			return false;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Handle a key press.
		/// Caller should ensure this is wrapped in a UOW (typically done in an override of
		/// OnKeyPress in RootSiteEditingHelper, since SimpleRootSite does not have access
		/// to FDO and UOW).
		/// </summary>
		/// <param name="keyChar">The pressed character key</param>
		/// <param name="modifiers">key modifies - shift status, etc.</param>
		/// -----------------------------------------------------------------------------------
		public void HandleKeyPress(char keyChar, Keys modifiers)
		{
			CheckDisposed();
			// REVIEW (EberhardB): .NETs Unicode character type is 16bit, whereas AppCore used
			// 32bit (int), so how do we handle this?

			//	TODO 1735(JohnT): handle surrogates! Currently we ignore them.
			if (char.GetUnicodeCategory(keyChar) == UnicodeCategory.Surrogate)
			{
				MessageBox.Show("DEBUG: Got a surrogate!");
				return;
			}

			if (Callbacks != null && Callbacks.EditedRootBox != null)
			{
				VwShiftStatus ss = GetShiftStatus(modifiers);
				StringBuilder buffer = new StringBuilder();

				CollectTypedInput(keyChar, buffer);

				OnCharAux(buffer.ToString(), ss, modifiers);
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Returns the ShiftStatus that shows if Ctrl and/or Shift keys were pressed
		/// </summary>
		/// <param name="keys">The key state</param>
		/// <returns>The shift status</returns>
		/// -----------------------------------------------------------------------------------
		public static VwShiftStatus GetShiftStatus(Keys keys)
		{
			// Test whether the Ctrl and/or Shift keys are also being pressed.
			VwShiftStatus ss = VwShiftStatus.kfssNone;
			if ((keys & Keys.Shift) == Keys.Shift)
				ss = VwShiftStatus.kfssShift;
			if ((keys & Keys.Control) == Keys.Control)
			{
				if (ss != VwShiftStatus.kfssNone)
					ss = VwShiftStatus.kgrfssShiftControl;
				else
					ss = VwShiftStatus.kfssControl;
			}
			return ss;
		}

		/// <summary>
		/// Allows subclass to be more selective about combining multiple keystrokes into one event.
		/// Contract: may always return true if buffer is empty.
		/// Must return false if the buffer is not empty and the next WM_CHAR is delete or return.
		/// </summary>
		/// <param name="nextChar">The next char that will be processed</param>
		/// <returns></returns>
		public virtual bool KeepCollectingInput(int nextChar)
		{
			return nextChar >= ' ' && nextChar != (int)VwSpecialChars.kscDelForward;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Collect whatever keyboard input is available--whatever the user has typed ahead.
		/// Includes backspaces and delete forwards, but not any more special keys like arrow keys.
		/// </summary>
		/// <param name="chsFirst">the first character the user typed, which started the whole
		/// process.</param>
		/// <param name="buffer">output is accumulated here (starting with chsFirst, unless
		/// it gets deleted by a subsequent backspace).</param>
		/// -----------------------------------------------------------------------------------
		protected void CollectTypedInput(char chsFirst, StringBuilder buffer)
		{
			// The first character goes into the buffer
			buffer.Append(chsFirst);
#if !__MonoCS__
			// Note: When/if porting to MONO, the following block of code can be removed
			// and still work.
			if (chsFirst < ' ' || chsFirst == (char)VwSpecialChars.kscDelForward)
				return;

			// We need to disable type-ahead when using a Keyman keyboard since it can
			// mess with the keyboard functionality. (FWR-2205)
			if (Control == null || KeyboardHelper.ActiveKeymanKeyboard != string.Empty)
				return;

			// Collect any characters that are currently in the message queue
			Win32.MSG msg = new Win32.MSG();
			while (true)
			{
				if (Win32.PeekMessage(ref msg, Control.Handle, (uint)Win32.WinMsgs.WM_KEYDOWN,
					(uint)Win32.WinMsgs.WM_KEYUP, (uint)Win32.PeekFlags.PM_NOREMOVE))
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
						break;

					// Now that we know we're going to translate the message, we need to
					// make sure it's removed from the message queue.
					Win32.PeekMessage(ref msg, Control.Handle, (uint)Win32.WinMsgs.WM_KEYDOWN,
						(uint)Win32.WinMsgs.WM_KEYUP, (uint)Win32.PeekFlags.PM_REMOVE);

					Win32.TranslateMessage(ref msg);
				}
				else if (Win32.PeekMessage(ref msg, Control.Handle, (uint)Win32.WinMsgs.WM_CHAR,
					(uint)Win32.WinMsgs.WM_CHAR, (uint)Win32.PeekFlags.PM_NOREMOVE))
				{
					char nextChar = (char)msg.wParam;
					if (!KeepCollectingInput(nextChar))
						break;

					// Since the previous peek didn't remove the message and by this point
					// we know we want to handle the message ourselves, we need to remove
					// the keypress from the message queue.
					Win32.PeekMessage(ref msg, Control.Handle, (uint)Win32.WinMsgs.WM_CHAR,
						(uint)Win32.WinMsgs.WM_CHAR, (uint)Win32.PeekFlags.PM_REMOVE);

					switch ((int)nextChar)
					{
						case (int)VwSpecialChars.kscBackspace:
							// handle backspace characters.  If there are are characters in
							// the buffer then remove the last one.  If not, then count
							// the backspace so it will be processed later.
							if (buffer.Length > 0)
							{
								if (buffer[0] == 8 || buffer[0] == 0x7f)
									throw new InvalidOperationException(
										"KeepCollectingInput should not allow more than one backspace");
								buffer.Remove(buffer.Length - 1, 1);
							}
							else
								buffer.Append(nextChar);
							return; // only one backspace currently allowed (except canceling earlier data)

						case (int)VwSpecialChars.kscDelForward:
						case '\r':
							if (buffer.Length > 0)
							{
								throw new InvalidOperationException(
									"KeepCollectingInput should not allow more than one delete or return");
							}
							buffer.Append(nextChar);
							return; // only one del currently allowed.
						default:
							// regular characters get added to the buffer
							buffer.Append(nextChar);
							break;
					}
				}
				else
					break;
			}
#endif
			// Shows that the buffering is working
			//			if (buffer.Length > 1)
			//				Debug.WriteLine("typeahead : >" + buffer + "< len = " + buffer.Length);
		}

		/// <summary>
		/// Helper method that wraps DeleteRangeIfComplex
		/// </summary>
		internal bool DeleteRangeIfComplex(IVwRootBox rootbox)
		{
			bool fWasComplex = false;
			IVwGraphics vg = GetGraphics();
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

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Handle typed character.
		/// Caller should ensure this is wrapped in a UOW (typically done in an override of
		/// OnKeyPress in RootSiteEditingHelper, since SimpleRootSite does not have access
		/// to FDO and UOW).
		/// </summary>
		/// <param name="input">input string</param>
		/// <param name="shiftStatus">Status of Shift/Control/Alt key</param>
		/// <param name="modifiers">key modifiers - shift status, etc.</param>
		/// -----------------------------------------------------------------------------------
		protected internal virtual void OnCharAux(string input, VwShiftStatus shiftStatus, Keys modifiers)
		{
			if (string.IsNullOrEmpty(input))
				return;

			if (Callbacks != null && Callbacks.EditedRootBox != null)
			{
				IVwRootBox rootb = Callbacks.EditedRootBox;
				bool fWasComplex = DeleteRangeIfComplex(rootb);

				// If DeleteRangeIfComplex handled the deletion, then we don't want to
				// try to handle the delete again.
				bool delOrBkspWasPressed = input.Contains(new string((char)VwSpecialChars.kscBackspace, 1)) ||
					input.Contains(new string((char)VwSpecialChars.kscDelForward, 1));
				if (!fWasComplex || !delOrBkspWasPressed)
				{
					if (input == "\r" && shiftStatus == VwShiftStatus.kfssShift)
						CallOnExtendedKey(input[0], shiftStatus);
					else
					{
						// We must (temporarily) have two units of work, since in many cases we need the view to be in the
						// state it gets updated to by the complex delete, before we try to insert, so here we split this
						// into two undo tasks. Eventually we merge the two units of work so they look like a single Undo task.
						if (fWasComplex && rootb.DataAccess.GetActionHandler() != null)
							rootb.DataAccess.GetActionHandler().BreakUndoTask(Resources.ksUndoTyping, Resources.ksRedoTyping);
						CallOnTyping(input, modifiers);
						if (fWasComplex && rootb.DataAccess.GetActionHandler() != null)
							MergeLastTwoUnitsOfWork();
					}
				}

				// It is possible that typing destroyed or changed the active rootbox, so we
				// better use the new one.
				rootb = Callbacks.EditedRootBox;
				rootb.Site.ScrollSelectionIntoView(rootb.Selection, VwScrollSelOpts.kssoDefault);
			}
		}

		/// <summary>
		/// Another case of something we can currently only do in the FDO-aware subclass.
		/// </summary>
		protected virtual void MergeLastTwoUnitsOfWork()
		{
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Call the root box's OnTyping method. Virtual for testing purposes.
		/// </summary>
		/// <param name="str"></param>
		/// <param name="modifiers"></param>
		/// -----------------------------------------------------------------------------------
		protected virtual void CallOnTyping(string str, Keys modifiers)
		{
			//	Console.WriteLine("CallOnTyping : " + str);
			if(Callbacks == null || Callbacks.EditedRootBox == null)
			{
				return;
			}

			// The user has pressed Ctrl-Space - do not generate a character.
			if ((modifiers & Keys.Control) == Keys.Control && str.CompareTo(" ") == 0)
			{
				//				IVwSelection vwsel;
				//				ITsTextProps[] vttp;
				//				IVwPropertyStore[] vvps;
				//
				//				if (!GetCharacterProps(out vwsel, out vttp, out vvps))
				//					return;
				//
				//				RemoveCharFormatting(vwsel, ref vttp, null);
			}
			else
			{
				// This needs to be set iff a change of writing system occurs while there is a range
				// selection because of a change of system input language.
				int wsPending = Callbacks.WsPending; // Todo JohnT: hook to client somehow.
				IVwGraphics vg = GetGraphics();
				try
				{
					Callbacks.EditedRootBox.OnTyping(vg, str, GetShiftStatus(modifiers), ref wsPending);
				}
				catch (Exception ex)
				{
					var fNotified = false;
					for (var ex1 = ex; ex1 != null; ex1 = ex1.InnerException)
					{
						if (!(ex1 is ArgumentOutOfRangeException))
							continue;
						MessageBox.Show(ex1.Message, Resources.ksWarning, MessageBoxButtons.OK, MessageBoxIcon.Warning);
						Callbacks.EditedRootBox.Reconstruct();	// Restore the actual value visually.
						fNotified = true;
						break;
					}
					if (!fNotified)
						throw;
				}
				finally
				{
					EditedRootBox.Site.ReleaseGraphics(EditedRootBox, vg);
				}

				Callbacks.WsPending = wsPending;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Handle extended keys. Returns false if it wasn't handled (e.g., arrow key beyond valid
		/// characters).
		/// </summary>
		/// <param name="chw"></param>
		/// <param name="ss"></param>
		/// <returns>Returns false if it wasn't handled (e.g., arrow key beyond valid characters).
		/// </returns>
		/// -----------------------------------------------------------------------------------
		protected virtual bool CallOnExtendedKey(int chw, VwShiftStatus ss)
		{
#if __MonoCS__
			chw &= 0xffff; // OnExtenedKey only expectes chw to contain the key info not the modifer info
#endif

			if (Callbacks == null || Callbacks.EditedRootBox == null)
			{
				return false;
			}
			Callbacks.WsPending = -1; // using these keys suppresses prior input lang change.
			// sets the arrow direction to physical or logical based on LTR or RTL
			EditingHelper.CkBehavior nFlags = Callbacks.ComplexKeyBehavior(chw, ss);

			int retVal = Callbacks.EditedRootBox.OnExtendedKey(chw, ss, (int)nFlags);
			Marshal.ThrowExceptionForHR(retVal); // Don't ignore error HRESULTs
			return  retVal != 1;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Special handling for Ctrl-Home/End and scrolling the selection into view
		/// </summary>
		/// <param name="e"></param>
		/// <param name="ss"></param>
		/// -----------------------------------------------------------------------------------
		protected virtual void HandleKeyDown(KeyEventArgs e, VwShiftStatus ss)
		{
			if(Callbacks == null || Callbacks.EditedRootBox == null)
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
			else if (e.KeyCode == Keys.Home && ss == VwShiftStatus.kfssControl)
			{
				// Control home is supposed to scroll all the way to the top
				Callbacks.ScrollToTop();
				return;
			}
			else if (e.KeyCode == Keys.PageDown &&
				(ss == VwShiftStatus.kfssNone || ss == VwShiftStatus.kfssShift))
			{
				Callbacks.ScrollPosition = new Point(-Callbacks.ScrollPosition.X,
					-Callbacks.ScrollPosition.Y + Control.Height);
				// need to call ScrollSelectionIntoView below
			}
			else if (e.KeyCode == Keys.PageUp &&
				(ss == VwShiftStatus.kfssNone || ss == VwShiftStatus.kfssShift))
			{
				Callbacks.ScrollPosition = new Point(-Callbacks.ScrollPosition.X,
					-Callbacks.ScrollPosition.Y - Control.Height);
				// need to call ScrollSelectionIntoView below
			}

			IVwRootBox rootb = Callbacks.EditedRootBox;
			if (MiscUtils.IsUnix && (e.KeyCode == Keys.Right || e.KeyCode == Keys.Left ||
				  e.KeyCode == Keys.F7 || e.KeyCode == Keys.F8) && ss == VwShiftStatus.kfssNone)
			{
				// FWNX-456 fix for refreshing lines that cursor is not properly invalidating
				if(Control is SimpleRootSite)
				{
					Point ip = (Control as SimpleRootSite).IPLocation;
					Rect src, dst;
					(Control as SimpleRootSite).GetTransformAtDst(rootb, ip, out src, out dst);
					const int IPWidth = 2;
					const int LineHeightFudgeFactor = 3;
					Rectangle rect = new Rectangle(ip.X - dst.left, -dst.top, IPWidth,
						(Control as SimpleRootSite).LineHeight + LineHeightFudgeFactor);
					(Control as SimpleRootSite).InvalidateRect(rootb, rect.Left, rect.Top, rect.Width, rect.Height);
				}
			}
			if (!rootb.Site.ScrollSelectionIntoView(rootb.Selection, VwScrollSelOpts.kssoDefault))
				Control.Update();
		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the view constructor.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IVwViewConstructor ViewConstructor
		{
			get
			{
				CheckDisposed();
				int hvoRoot, frag;
				IVwViewConstructor vc;
				IVwStylesheet ss;
				EditedRootBox.GetRootObject(out hvoRoot, out vc, out frag, out ss);
				return vc;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether the current view is a Scripture view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual bool IsScriptureView
		{
			get
			{
				return false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a SelectionHelper object set to the current selection in the view
		/// (updated any time the selection changes)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual SelectionHelper CurrentSelection
		{
			get
			{
				CheckDisposed();
				if (IsCurrentSelectionOutOfDate)
				{
					if (Callbacks == null || Callbacks.EditedRootBox == null || Callbacks.EditedRootBox.Site == null)
						ClearCurrentSelection();
					else
						m_currentSelection = SelectionHelper.Create(Callbacks.EditedRootBox.Site);
				}
				return m_currentSelection;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this instance is current selection out of date.
		/// Changing the selection to another cell in the same row of a browse view doesn't
		/// always result in SelectionChanged() being called, or in the stored selection
		/// becoming invalid.  So we check a little more closely here. (See LT-3787.)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual bool IsCurrentSelectionOutOfDate
		{
			get
			{
				if (m_currentSelection == null || m_currentSelection.Selection == null)
					return true;

				// If it's invalid, it's obviously out-of-date
				return (!m_currentSelection.Selection.IsValid ||
					m_currentSelection.Selection != RootBoxSelection);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the selection from the root box that is currently being edited (can be null).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual IVwSelection RootBoxSelection
		{
			get
			{
				CheckDisposed();
				return (Callbacks != null && Callbacks.EditedRootBox != null) ?
					Callbacks.EditedRootBox.Selection : null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// gets/sets the editable state. This should only be set from
		/// SimpleRootSite.ReadOnlyView.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool Editable
		{
			get { CheckDisposed(); return m_fEditable; }
			set { CheckDisposed(); m_fEditable = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the edited rootbox from the callbacks for this EditingHelper.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IVwRootBox EditedRootBox
		{
			get { CheckDisposed(); return m_callbacks.EditedRootBox; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieve the WSF from the root box's cache.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual ILgWritingSystemFactory WritingSystemFactory
		{
			get
			{
				if (Callbacks == null)
					return null;

				var rootb = Callbacks.EditedRootBox;
				if (rootb != null)
					return rootb.DataAccess.WritingSystemFactory;
				return null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets callbacks object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IEditingCallbacks Callbacks
		{
			get { CheckDisposed(); return m_callbacks; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets control associated with callback object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public UserControl Control
		{
			get { CheckDisposed(); return m_control; }
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the cursor that will always be shown.
		/// </summary>
		/// <value>A <c>Cursor</c> that is shown all the time, regardless of the context
		/// the mouse pointer is over (except when overriden by a derived class).</value>
		/// <remarks>To use the built-in cursors of RootSite, set <c>DefaultCursor</c> to
		/// <c>null</c>.</remarks>
		/// -----------------------------------------------------------------------------------
		public virtual Cursor DefaultCursor
		{
			get { CheckDisposed(); return m_defaultCursor; }
			set
			{
				CheckDisposed();
				m_defaultCursor = value;
				// set the cursor shown in the current control.
				Control.Cursor = m_defaultCursor ?? GetCursor(false, false, FwObjDataTypes.kodtContextString);
			}
		}

		/// <summary>
		/// Gets/sets the cursor which replaces the IBeam when the mouse is over read-only text.
		/// </summary>
		public Cursor ReadOnlyTextCursor
		{
			get { CheckDisposed(); return m_readOnlyCursor; }
			set { CheckDisposed(); m_readOnlyCursor = value; }
		}
		#endregion

		#region Mouse related methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Hook method for doing application specific processing on a mouse down event.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void HandleMouseDown()
		{
		}
		#endregion

		#region Navigation
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Go to the next paragraph looking at the selection information.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void GoToNextPara()
		{
			int level = CurrentSelection.GetNumberOfLevels(SelectionHelper.SelLimitType.Top) - 1;
			bool fEnd = CurrentSelection.Selection.EndBeforeAnchor;
			while (level >= 0)
			{
				int iBox = CurrentSelection.Selection.get_BoxIndex(fEnd, level);
				IVwSelection sel = Callbacks.EditedRootBox.MakeSelInBox(CurrentSelection.Selection, fEnd, level,
					iBox + 1, true, false, true);
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
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clears the cached current selection. When CurrentSelection is requested, a new
		/// one will be cached with the updated information.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void ClearCurrentSelection()
		{
			m_currentSelection = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the IVwGraphics object from the IvwRootsite. NOTE: The graphics object returned
		/// from this method MUST be released with a call to EditedRootBox.Site.ReleaseGraphics()!
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private IVwGraphics GetGraphics()
		{
			IVwGraphics vg;
			Rect rcSrcRoot;
			Rect rcDstRoot;
			EditedRootBox.Site.GetGraphics(EditedRootBox, out vg, out rcSrcRoot, out rcDstRoot);
			return vg;
		}

		/// -----------------------------------------------------------------------------------
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
		/// -----------------------------------------------------------------------------------
		public void SetCursor(Point mousePos, IVwRootBox rootb)
		{
			CheckDisposed();
			if (rootb == null)
				return;
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
				bool fInPicture = false;
				int objDataType;
				bool fInObject = rootb.get_IsClickInObject(mousePos.X, mousePos.Y, rcSrcRoot,
					rcDstRoot, out objDataType);

				// Don't display the hand cursor if we have a range selection
				if (rootb.Selection != null && rootb.Selection.IsRange)
					fInObject = false;

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
						return;
					if (ReadOnlyTextCursor != null && !CanEdit(sel))
					{
						Control.Cursor = ReadOnlyTextCursor;
						return;
					}
				}
				Control.Cursor = GetCursor(fInObject, fInPicture, (FwObjDataTypes)objDataType);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine the default font to use for the specified writing system,
		/// displayed in the default Normal style of the specified stylesheet.
		/// Currently duplicated from Widgets.FontHeightAdjuster. Grrr.
		/// </summary>
		/// <param name="hvoWs"></param>
		/// <param name="styleSheet"></param>
		/// <param name="wsf"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		static public System.Drawing.Font GetFontForNormalStyle(int hvoWs, IVwStylesheet styleSheet,
			ILgWritingSystemFactory wsf)
		{
			ITsTextProps ttpNormal = styleSheet.NormalFontStyle;
			string styleName = "Normal";
			if (ttpNormal != null)
				styleName = ttpNormal.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);

			ITsPropsBldr ttpBldr = TsPropsBldrClass.Create();
			ttpBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, styleName);
			ttpBldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0, hvoWs);
			ITsTextProps ttp = ttpBldr.GetTextProps();

			IVwPropertyStore vwps = VwPropertyStoreClass.Create();
			vwps.Stylesheet = styleSheet;
			vwps.WritingSystemFactory = wsf;
			LgCharRenderProps chrps = vwps.get_ChrpFor(ttp);
			ILgWritingSystem ws = wsf.get_EngineOrNull(hvoWs);
			ws.InterpretChrp(ref chrps);
			int dympHeight = chrps.dympHeight;
			StringBuilder bldr = new StringBuilder(chrps.szFaceName.Length);
			for (int i = 0; i < chrps.szFaceName.Length; i++)
			{
				ushort ch = chrps.szFaceName[i];
				if (ch == 0)
					break; // null termination
				bldr.Append(Convert.ToChar(ch));
			}
			return new System.Drawing.Font(bldr.ToString(), (float)(dympHeight / 1000.0));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Allows an application to set a cursor for the given selection.
		/// </summary>
		/// <param name="sel"></param>
		/// <returns>True if a custom cursor was set, false otherwise</returns>
		/// ------------------------------------------------------------------------------------
		protected virtual bool SetCustomCursor(IVwSelection sel)
		{
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a cursor suitable for the context indicated by the arguments.
		/// </summary>
		/// <param name="fInObject"><c>True</c> if the mouse pointer is over an object</param>
		/// <param name="fInPicture">True if mouse is over a picture (or icon) (This should be
		/// false if the picture is an ORC-replacement icon.)</param>
		/// <param name="objDataType">The type of the object the mouse pointer is over</param>
		/// ------------------------------------------------------------------------------------
		private Cursor GetCursor(bool fInObject, bool fInPicture, FwObjDataTypes objDataType)
		{
			if (fInPicture)
				return Cursors.Arrow;

			if (fInObject && (objDataType == FwObjDataTypes.kodtNameGuidHot
				|| objDataType == FwObjDataTypes.kodtExternalPathName
				|| objDataType == FwObjDataTypes.kodtOwnNameGuidHot
				|| objDataType == FwObjDataTypes.kodtPictEvenHot
				|| objDataType == FwObjDataTypes.kodtPictOddHot))
			{
				return Cursors.Hand;
			}

			return (Control is SimpleRootSite) ? ((SimpleRootSite)Control).IBeamCursor : Cursors.IBeam;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Convert this instance to type T.
		/// </summary>
		/// <typeparam name="T">Desired type to cast to.</typeparam>
		/// <returns>Editing helper cast as T.</returns>
		/// <remarks>We added this method so that we can retrieve the TeEditingHelper from the
		/// PublicationEditingHelper (which internally contains an TeEditingHelper).</remarks>
		/// ------------------------------------------------------------------------------------
		public virtual T CastAs<T>() where T : EditingHelper
		{
			return this as T;
		}
		#endregion

		#region Style related methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If the current selection contains one run with a character style or multiple runs
		/// with the same character style this method returns the character style; otherwise
		/// returns the paragraph style unless multiple paragraphs are selected that have
		/// different paragraph styles.
		/// </summary>
		/// <param name="styleName">Gets the styleName</param>
		/// <returns>The styleType or -1 if no style type can be found or multiple style types
		/// are in the selection.  Otherwise returns the styletype</returns>
		/// ------------------------------------------------------------------------------------
		public virtual int GetStyleNameFromSelection(out string styleName)
		{
			CheckDisposed();
			styleName = null;

			try
			{
				IVwSelection vwsel = null;
				if (Callbacks != null && Callbacks.EditedRootBox != null)
					vwsel = Callbacks.EditedRootBox.Selection;
				if (vwsel == null)
				{
					styleName = string.Empty;
					return -1;
				}

				styleName = GetCharStyleNameFromSelection(vwsel);

				if (styleName != null && styleName != string.Empty)
					return (int)StyleType.kstCharacter;

				styleName = GetParaStyleNameFromSelection();
				return (styleName != null ? (int)StyleType.kstParagraph : -1);
			}
			catch
			{
				styleName = string.Empty;
				return -1;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the paragraph style name from the selection
		/// </summary>
		/// <returns>The style name or empty string if there are multiple styles or
		/// no selection</returns>
		/// ------------------------------------------------------------------------------------
		public string GetParaStyleNameFromSelection()
		{
			CheckDisposed();
			string styleName = string.Empty;
			ITsTextProps[] vttp = null;
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
				styleName = GetStyleNameFromTextProps(vttp, (int)StyleType.kstParagraph);
				if (styleName == null)
					styleName = string.Empty;
			}
			return styleName;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the style name from the specified ITsTextProps array if all ITsTextProps runs
		/// contain the same style name.
		/// </summary>
		/// <param name="vttp">The array of ITsTextProps</param>
		/// <param name="styleType">The type of style expected to find in each ITsTextProps
		/// (e.g. character, paragraph).</param>
		/// <returns>Gets the style name from the specified ITsTextProps array if all
		/// ITsTextProps runs contain the same style name, otherwise null</returns>
		/// ------------------------------------------------------------------------------------
		private string GetStyleNameFromTextProps(ITsTextProps[] vttp, int styleType)
		{
			Debug.Assert(vttp != null);
			string styleName;
			string prevStyleName = styleName = string.Empty;

			for (int ittp = 0; ittp < vttp.Length; ittp++)
			{
				styleName = (vttp[ittp] != null ? vttp[ittp].GetStrPropValue(
					(int)FwTextPropType.ktptNamedStyle) : string.Empty);

				if (styleName == null)
					styleName = string.Empty;

				if (styleName == string.Empty &&
					styleType == (int)StyleType.kstParagraph)
				{
					styleName = DefaultNormalParagraphStyleName;
				}

				if (ittp > 0 && prevStyleName != styleName)
					return null;

				prevStyleName = styleName;
			}

			return styleName;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the default "Normal" paragraph style name. This base implementation just returns
		/// a hardcoded string. It will probably never be used, so it doesn't matter.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual string DefaultNormalParagraphStyleName
		{
			get { return "Normal"; }
		}

		/// ------------------------------------------------------------------------------------
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
		/// ------------------------------------------------------------------------------------
		public bool GetParagraphProps(out IVwSelection vwsel, out int hvoText,
			out int flidParaOwner, out IVwPropertyStore[] vqvps, out int ihvoFirst,
			out int ihvoLast, out ITsTextProps[] vqttp)
		{
			CheckDisposed();
			ihvoFirst = 0;
			ihvoLast = 0;
			vqttp = null;

			int ihvoAnchor, ihvoEnd;

			if (!IsParagraphProps(out vwsel, out hvoText, out flidParaOwner, out vqvps,
				out ihvoAnchor, out ihvoEnd))
				return false;

			// OK, we're going to do it!
			ihvoFirst = ihvoAnchor;
			ihvoLast = ihvoEnd;
			if (ihvoFirst > ihvoLast)
			{
				ihvoFirst = ihvoLast;
				ihvoLast = ihvoAnchor;
			}

			ISilDataAccess sda = Callbacks.EditedRootBox.DataAccess;
			if (sda == null) // Very unlikely, but it's a COM interface...
				return false;

			if (HandleSpecialParagraphType(flidParaOwner, out vqttp))
				return true;

			vqttp = new ITsTextProps[ihvoLast - ihvoFirst + 1];
			int index = 0;
			for (int ihvo = ihvoFirst; ihvo <= ihvoLast; ihvo++)
			{
				int hvoPara = sda.get_VecItem(hvoText, flidParaOwner, ihvo);
				var ttp = sda.get_UnknownProp(hvoPara, ParagraphPropertiesTag)
					as ITsTextProps;
				vqttp[index] = ttp;
				index++;
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Allows subclasses to examine the given flid to see if it is a type that requires
		/// special handling, as opposed to just getting an array of style props for each
		/// paragraph in the property represented by that flid.
		/// </summary>
		/// <param name="flidParaOwner">The flid in which the paragraph is owned</param>
		/// <param name="vqttp">array of text props representing the paragraphs in</param>
		/// <returns><c>true</c> if handled; <c>false</c> otherwise.</returns>
		/// ------------------------------------------------------------------------------------
		protected virtual bool HandleSpecialParagraphType(int flidParaOwner, out ITsTextProps[] vqttp)
		{
			vqttp = null;
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the Character style name from the selection
		/// </summary>
		/// <returns>The style name or null if there are multiple styles or an empty string if
		/// there is no character style</returns>
		/// ------------------------------------------------------------------------------------
		public string GetCharStyleNameFromSelection()
		{
			CheckDisposed();
			IVwSelection sel = RootBoxSelection;
			return (sel == null) ? string.Empty : GetCharStyleNameFromSelection(sel);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the Character style name from the selection
		/// </summary>
		/// <param name="vwsel">The IVwSelection to get the style name from</param>
		/// <returns>The style name or null if there are multiple styles or an empty string
		/// if there is no character style</returns>
		/// ------------------------------------------------------------------------------------
		protected string GetCharStyleNameFromSelection(IVwSelection vwsel)
		{
			if (vwsel == null)
				return null;
			try
			{
				ITsTextProps[] vttp;
				IVwPropertyStore[] vvps;
				int cttp;

				SelectionHelper.GetSelectionProps(vwsel, out vttp, out vvps, out cttp);

				if (cttp == 0)
					return null;

				return GetStyleNameFromTextProps(vttp, (int)StyleType.kstCharacter);
			}
			catch
			{
				return null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets paragraph text properties for the current selection
		/// </summary>
		/// <returns>An array of ITsTextProps objects</returns>
		/// ------------------------------------------------------------------------------------
		protected ITsTextProps[] GetParagraphTextPropsFromSelection()
		{
			IVwSelection vwsel;
			int hvoText;
			int tagText;
			IVwPropertyStore[] vvps;
			int ihvoFirst;
			int ihvoLast;
			ITsTextProps[] vttp;

			return (GetParagraphProps(out vwsel, out hvoText, out tagText, out vvps,
				out ihvoFirst, out ihvoLast, out vttp) ? vttp : null);
		}

		/// ------------------------------------------------------------------------------------
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
		/// paragraph property; otherwise false.</param>
		/// <returns><c>false</c> if method exited because <paramref name='fRet'/> is
		/// <c>false</c> or there are no TsTextProps in the paragraph, otherwise <c>true</c>
		/// </returns>
		/// ------------------------------------------------------------------------------------
		protected bool GetAllParagraphProps(out IVwSelection vwsel,
			out int hvoText, out int tagText, out ITsTextProps[] vttp,
			out IVwPropertyStore[] vvps, out int ihvoFirst, out int ihvoLast,
			out ITsTextProps[] vttpHard, out IVwPropertyStore[] vvpsSoft, out bool fRet)
		{
			vwsel = null;
			hvoText = tagText = ihvoFirst = ihvoLast = 0;
			vttp = vttpHard = null;
			vvps = vvpsSoft = null;
			fRet = true;

			// Get the paragraph properties from the selection. If there is neither a selection
			// nor a paragraph property, return false.
			if (!GetParagraphProps(out vwsel, out hvoText, out tagText, out vvps,
				out ihvoFirst, out ihvoLast, out vttp))
			{
				fRet = false;
				return false;
			}
			// If there are no TsTextProps for the paragraph(s), return true. There is nothing
			// to format.
			if (0 == vttp.Length)
			{
				fRet = true;
				return false;
			}

			int cttp = vttp.Length;
			using (ArrayPtr ptrHard = MarshalEx.ArrayToNative<ITsTextProps>(cttp))
			{
				using (ArrayPtr ptrSoft = MarshalEx.ArrayToNative<IVwPropertyStore>(cttp))
				{
					vwsel.GetHardAndSoftParaProps(cttp, vttp, ptrHard, ptrSoft, out cttp);
					vttpHard = MarshalEx.NativeToArray<ITsTextProps>(ptrHard, cttp);
					vvpsSoft = MarshalEx.NativeToArray<IVwPropertyStore>(ptrSoft, cttp);
				}
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
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
		/// ------------------------------------------------------------------------------------
		public bool IsParagraphProps(out IVwSelection vwsel, out int hvoText,
			out int tagText, out IVwPropertyStore[] vqvps, out int ihvoAnchor, out int ihvoEnd)
		{
			CheckDisposed();
			hvoText = 0;
			tagText = 0;
			vqvps = null;
			ihvoAnchor = 0;
			ihvoEnd = 0;

			// Get the selection. Can't do command unless we have one.
			vwsel = RootBoxSelection;
			if (vwsel == null || !vwsel.IsValid)
				return false;

			// First check the anchor to see if we can find a paragraph at some level.
			if (!GetParagraphLevelInfoForSelection(vwsel, false, out hvoText, out tagText, out ihvoAnchor))
				return false;

			// Next check the end to see if we have a paragraph in the same text and flid
			int hvoEnd;
			int tagEnd;
			if (!GetParagraphLevelInfoForSelection(vwsel, true, out hvoEnd, out tagEnd, out ihvoEnd))
				return false;

			// Make sure it's the same property.
			if (tagEnd != tagText || hvoText != hvoEnd)
				return false;

			GetParaPropStores(vwsel, out vqvps);

			// make sure we have one prop for each paragraph that is selected.
			int ihvoMin = vwsel.EndBeforeAnchor ? ihvoEnd : ihvoAnchor;
			int ihvoMax = vwsel.EndBeforeAnchor ? ihvoAnchor : ihvoEnd;
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

		/// ------------------------------------------------------------------------------------
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
		/// ------------------------------------------------------------------------------------
		private bool GetParagraphLevelInfoForSelection(IVwSelection vwsel, bool fEnd,
			out int hvoText, out int tagText, out int ihvo)
		{
			hvoText = 0;
			tagText = 0;
			ihvo = 0;

			// We need a two-level or more selection.
			int clev = vwsel.CLevels(fEnd);
			if (clev < 2)
				return false;

			int cpropPrevious = 0;
			IVwPropertyStore vps;
			bool fFoundParagraphLevel = false;
			for (int lev = 0; lev < clev && !fFoundParagraphLevel; lev++)
			{
				// At this point, we know how to do this command only for structured text paragraphs.
				vwsel.PropInfo(fEnd, lev, out hvoText, out tagText, out ihvo, out cpropPrevious,
					out vps);
				fFoundParagraphLevel = IsParagraphLevelTag(tagText);
			}

			// If we didn't find the paragraph level or we found a level for a property that
			// was not the first occurrence of the paragraph, then give up.
			return (fFoundParagraphLevel && cpropPrevious == 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the given tag represents paragraph-level information
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual bool IsParagraphLevelTag(int tag)
		{
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The default tag/flid containing the contents of ordinary paragraphs
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual int ParagraphContentsTag
		{
			get {throw new NotImplementedException();}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The default tag/flid containing the properties of ordinary paragraphs
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual int ParagraphPropertiesTag
		{
			get { throw new NotImplementedException(); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets an array of property stores, one for each paragraph in the given selection.
		/// </summary>
		/// <param name="vwsel">The selection.</param>
		/// <param name="vqvps">The property stores.</param>
		/// ------------------------------------------------------------------------------------
		protected virtual void GetParaPropStores(IVwSelection vwsel, out IVwPropertyStore[] vqvps)
		{
			int cvps;
			SelectionHelper.GetParaProps(vwsel, out vqvps, out cvps);
		}

		/// -----------------------------------------------------------------------------------
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
		/// -----------------------------------------------------------------------------------
		public static bool GetCharacterProps(IVwRootBox rootb, out IVwSelection vwsel,
			out ITsTextProps[] vttp, out IVwPropertyStore[] vvps)
		{
			vwsel = null;
			vttp = null;
			vvps = null;

			// Get the selection. Can't do command unless we have one.
			if (rootb == null)
				return false;

			vwsel = rootb.Selection;
			if (vwsel == null)
				return false;

			int cttp;

			SelectionHelper.GetSelectionProps(vwsel, out vttp, out vvps, out cttp);

			return (cttp != 0);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Get the view selection and character properties. Return false if there is neither a
		/// selection nor any text selected. Otherwise return true.
		/// </summary>
		/// <param name="vwsel"></param>
		/// <param name="vttp"></param>
		/// <param name="vvps"></param>
		/// <returns></returns>
		/// -----------------------------------------------------------------------------------
		public bool GetCharacterProps(out IVwSelection vwsel, out ITsTextProps[] vttp,
			out IVwPropertyStore[] vvps)
		{
			CheckDisposed();
			IVwRootBox rootbox = null;
			if(Callbacks != null)
			{
				rootbox = Callbacks.EditedRootBox;
			}
			return GetCharacterProps(rootbox, out vwsel, out vttp, out vvps);
		}
		#endregion

		#region Apply style changes
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Apply the selected style with only the specified style name.
		/// </summary>
		/// <param name="sStyleToApply">Style name (this could be a paragraph or character
		/// style).</param>
		/// ------------------------------------------------------------------------------------
		public virtual void ApplyStyle(string sStyleToApply)
		{
			CheckDisposed();
			IVwSelection vwsel;
			IVwPropertyStore[] vvpsPara;
			IVwPropertyStore[] vvpsChar;
			ITsTextProps[] vttpPara;
			ITsTextProps[] vttpChar;
			int hvoText;
			int tagText;
			int ihvoFirst;
			int ihvoLast;

			GetCharacterProps(out vwsel, out vttpChar, out vvpsChar);
			GetParagraphProps(out vwsel, out hvoText, out tagText, out vvpsPara, out ihvoFirst,
				out ihvoLast, out vttpPara);

			ApplyStyle(sStyleToApply, vwsel, vttpPara, vttpChar);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Apply the selected style
		/// </summary>
		/// <param name="sStyleToApply">Style name</param>
		/// <param name="vwsel">Selection</param>
		/// <param name="vttpPara">Paragraph properties</param>
		/// <param name="vttpChar">Character properties</param>
		/// ------------------------------------------------------------------------------------
		public virtual void ApplyStyle(string sStyleToApply, IVwSelection vwsel,
			ITsTextProps[] vttpPara, ITsTextProps[] vttpChar)
		{
			CheckDisposed();
			if(Callbacks == null || Callbacks.EditedRootBox == null ||
				Callbacks.EditedRootBox.DataAccess == null)
				return;

			IVwStylesheet stylesheet = Callbacks.EditedRootBox.Stylesheet;
			if (stylesheet == null)
				return;

			StyleType stType;
			if (string.IsNullOrEmpty(sStyleToApply))
				stType = StyleType.kstCharacter;
			else
				stType = (StyleType)stylesheet.GetType(sStyleToApply);

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

		/// ------------------------------------------------------------------------------------
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
		/// ------------------------------------------------------------------------------------
		public virtual bool ApplyParagraphStyle(string strNewVal)
		{
			CheckDisposed();
			IVwSelection vwsel;
			int hvoText;
			int tagText;
			int ihvoFirst, ihvoLast;
			ITsTextProps[] vttp;
			IVwPropertyStore[] vqvps;
			ITsTextProps[] vttpHard;
			IVwPropertyStore[] vvpsSoft;
			bool fRet;

			if (!GetAllParagraphProps(out vwsel, out hvoText, out tagText, out vttp,
				out vqvps, out ihvoFirst, out ihvoLast, out vttpHard, out vvpsSoft, out fRet))
				return fRet;

			// Make a new TsTextProps object, and set its NamedStyle.
			ITsPropsBldr tpb = TsPropsBldrClass.Create();
			tpb.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, strNewVal);
			ITsTextProps newProps = tpb.GetTextProps();

			bool fChangedStyle = false;
			for (int ittp = 0; ittp < vttp.Length; ++ittp)
			{
				string oldStyle = null;
				if (vttp[ittp] != null)		// this can happen if para never had style set explicitly.
					oldStyle = vttp[ittp].GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
				fChangedStyle |= (oldStyle != strNewVal);

				// ENHANCE JohnT: it would be nice to detect we are applying the
				// same style, and if there is explicit formatting put up the dialog
				// asking whether to change the style defn.
				// NOTE: that is probably not what we want to do in TE!

				vttp[ittp] = newProps;
			}

			if (!fChangedStyle)
				return true; // Nothing really changed!

			var helper = SelectionHelper.Create(vwsel, Callbacks.EditedRootBox.Site);
			// Narrow the range of TsTextProps to only include those that are not NULL.
			int ihvoFirstMod;
			int ihvoLastMod;
			NarrowRangeOfTsTxtProps(hvoText, tagText, vttp, vvpsSoft, true, ihvoFirst,
				ihvoLast, out ihvoFirstMod, out ihvoLastMod);
			RestoreSelectionAtEndUow(vwsel, helper);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Remove character formatting.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void RemoveCharFormatting()
		{
			CheckDisposed();
			RemoveCharFormatting(false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Remove character formatting.
		/// </summary>
		/// <param name="removeAllStyles">if true, all styles in selection will be removed</param>
		/// ------------------------------------------------------------------------------------
		public virtual void RemoveCharFormatting(bool removeAllStyles)
		{
			CheckDisposed();
			IVwSelection vwsel;
			ITsTextProps[] vttp;
			IVwPropertyStore[] vvps;

			if (!GetCharacterProps(out vwsel, out vttp, out vvps))
				return;

			if(Callbacks == null || Callbacks.EditedRootBox == null)
				return;

			RemoveCharFormatting(vwsel, ref vttp, null, removeAllStyles);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Allow certain style names to be given special sematic meaning so they will be
		/// skipped over when removing character styles.
		/// </summary>
		/// <param name="name">style name to check</param>
		/// <returns>true to apply meaning to the style and skip it.</returns>
		/// ------------------------------------------------------------------------------------
		public virtual bool SpecialSemanticsCharacterStyle(string name)
		{
			CheckDisposed();
			return false;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Remove character formatting, as when the user types ctrl-space, or chooses a named
		/// style. Assumes an Undo action is active if wanted. Clears all formatting
		/// controlled by the Format/Font dialog, and sets the specified named style, or
		/// clears that too if it is null or empty. (Pass null to choose "default paragraph
		/// style".)
		/// </summary>
		/// <remarks>The method is public so it can be used by the Find/Replace dialog.</remarks>
		/// -----------------------------------------------------------------------------------
		public void RemoveCharFormatting(IVwSelection vwsel, ref ITsTextProps[] vttp,
			string sStyle)
		{
			CheckDisposed();
			RemoveCharFormatting(vwsel, ref vttp, sStyle, false);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Remove character formatting, as when the user types ctrl-space, or chooses a named
		/// style. Assumes an Undo action is active if wanted. Clears all formatting
		/// controlled by the Format/Font dialog, and sets the specified named style, or
		/// clears that too if it is null or empty. (Pass null to choose "default paragraph
		/// style".)
		/// </summary>
		/// <param name="vwsel"></param>
		/// <param name="vttp"></param>
		/// <param name="sStyle"></param>
		/// <param name="removeAllStyles">if true, all styles in selection will be removed</param>
		/// <remarks>The method is public so it can be used by the Find/Replace dialog.</remarks>
		/// -----------------------------------------------------------------------------------
		public void RemoveCharFormatting(IVwSelection vwsel, ref ITsTextProps[] vttp,
			string sStyle, bool removeAllStyles)
		{
			CheckDisposed();
			bool fPropsModified = false;
			Debug.Assert(vttp != null, "This shouldn't happen. Please look at TE-6499.");
			if (vttp == null)
				return;

			int cttp = vttp.Length;
			ITsTextProps ttpEmpty = null;

			for (int ittp = 0; ittp < cttp; ittp++)
			{
				if (vwsel.IsRange)
				{
					string objData = vttp[ittp].GetStrPropValue((int)FwTextPropType.ktptObjData);
					if (objData != null)
					{
						// We don't want to clear most object data, because it has the effect of making
						// ORCs unuseable. A special case is LinkedFiles, which are applied to regular
						// characters, and annoying not to be able to remove.
						if (objData.Length == 0 ||
							objData[0] != Convert.ToChar((int)FwObjDataTypes.kodtExternalPathName))
						{
							continue; // skip this run.
						}
					}
				}

				// Skip user prompt strings. A user prompt string will (hopefully) have a
				// dummy property set on it to indicate this.
				int nvar;
				if (vttp[ittp].GetIntPropValues(SimpleRootSite.ktptUserPrompt, out nvar) == 1)
					continue;

				// Allow a subclass to exclude styles that may have special semantics that should not
				// removed by applying a style when they are a part of a selection with multiple runs.
				if (!removeAllStyles &&	cttp > 1)
				{
					string oldStyle = vttp[ittp].GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
					if (SpecialSemanticsCharacterStyle(oldStyle))
						continue;
				}

				if (ttpEmpty == null)
				{
					ITsPropsBldr tpbEmpty = vttp[ittp].GetBldr();

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
					helper = SelectionHelper.Create(vwsel, Callbacks.EditedRootBox.Site);
				ChangeCharacterStyle(vwsel, vttp, cttp);
				RestoreSelectionAtEndUow(vwsel, helper);
			}

			if (Callbacks != null)
				Callbacks.WsPending = -1;
			ClearCurrentSelection();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Changes the character style.
		/// </summary>
		/// <param name="sel">The selection.</param>
		/// <param name="props">The properties specifying the new writing system.</param>
		/// <param name="numProps">The number of ITsTextProps.</param>
		/// ------------------------------------------------------------------------------------
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

		/// ------------------------------------------------------------------------------------
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
		/// ------------------------------------------------------------------------------------
		private void NarrowRangeOfTsTxtProps(int hvoText, int tagText,
			ITsTextProps[] vttp, IVwPropertyStore[] vvpsSoft, bool fParagraphStyle,
			int ihvoFirst, int ihvoLast, out int ihvoFirstMod, out int ihvoLastMod)
		{
			ISilDataAccess sda = Callbacks.EditedRootBox.DataAccess;
			ihvoFirstMod = -1;
			ihvoLastMod = -1;
			for (int ihvo = ihvoFirst; ihvo <= ihvoLast; ihvo++)
			{
				ITsTextProps ttp;
				ttp = vttp[ihvo - ihvoFirst];
				if (ttp != null)
				{
					// If we set a style for a paragraph at all, it must specify a named style.
					// The "default Normal" mechanism (see StVc.cpp) only works for paragraphs
					// which lack a style altogether. Any actual style must specify "Normal" unless
					// it specifies something else.
					string strNamedStyle =
						ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);

					if (strNamedStyle.Length == 0)
					{
						ITsPropsBldr tpb;
						tpb = ttp.GetBldr();
						tpb.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, DefaultNormalParagraphStyleName);
						ttp = tpb.GetTextProps();
					}

					ihvoLastMod = ihvo;
					if (ihvoFirstMod < 0)
						ihvoFirstMod = ihvo;
					int hvoPara;
					// TODO (EberhardB): when we have the C# cache we can do this differently
					hvoPara = sda.get_VecItem(hvoText, tagText, ihvo);

					ITsTextProps ttpRet;
					IVwPropertyStore vpsSoft = vvpsSoft[ihvo - ihvoFirst];
					if (RemoveRedundantHardFormatting(vpsSoft, ttp, fParagraphStyle, hvoPara,
						out ttpRet))
					{
						ttp = ttpRet;
					}
					ChangeParagraphStyle(sda, ttp, hvoPara);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Change the paragraph style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void ChangeParagraphStyle(ISilDataAccess sda, ITsTextProps ttp, int hvoPara)
		{
			sda.SetUnknown(hvoPara, ParagraphPropertiesTag, ttp);
		}

		/// ------------------------------------------------------------------------------------
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
		/// ------------------------------------------------------------------------------------
		protected bool RemoveRedundantHardFormatting(IVwPropertyStore vpsSoft,
			ITsTextProps ttpHard, bool fParaStyle, int hvoPara, out ITsTextProps ttpRet)
		{
			ttpRet = null;
			ISilDataAccess sda = Callbacks.EditedRootBox.DataAccess;
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
				tpbStyle = TsPropsBldrClass.Create();
				tpbStyle.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, strStyle);
				ITsTextProps ttpStyle;
				ttpStyle = tpbStyle.GetTextProps();
				IVwPropertyStore vpsSoftPlusStyle;
				vpsSoftPlusStyle = vpsSoft.get_DerivedPropertiesForTtp(ttpStyle);

				ITsPropsBldr tpbEnc;
				tpbEnc = TsPropsBldrClass.Create();

				ITsString tss;
				tss = sda.get_StringProp(hvoPara, ParagraphContentsTag);

				ITsStrBldr tsb;
				tsb = tss.GetBldr();

				int crun;
				crun = tss.RunCount;
				bool fChanged = false;
				for (int irun = 0; irun < crun; irun++)
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

			int ctpt;
			ctpt = ttpHard.IntPropCount;
			for (int itpt = 0; itpt < ctpt; itpt++)
			{
				int tpt;
				int nVarHard, nValHard;
				nValHard = ttpHard.GetIntProp(itpt, out tpt, out nVarHard);

				int nValSoft, nVarSoft;
				int nWeight, nRelHeight;
				switch ((FwTextPropType)tpt)
				{
				case FwTextPropType.ktptLineHeight:
					nValSoft = vpsSoft.get_IntProperty(tpt);
					nRelHeight = vpsSoft.get_IntProperty((int)VwStyleProperty.kspRelLineHeight);
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
						nVarSoft = (int)FwTextPropVar.ktpvMilliPoint;
					break;
				case FwTextPropType.ktptBold:
					// For an inverting property, a value of invert is never redundant.
					if (nValHard == (int)FwTextToggleVal.kttvInvert)
						continue;
					nWeight = vpsSoft.get_IntProperty(tpt);
					nValSoft = (nWeight > 550) ? (int)FwTextToggleVal.kttvInvert :
						(int)FwTextToggleVal.kttvOff;
					nVarSoft = (int)FwTextPropVar.ktpvEnum;
					break;
				case FwTextPropType.ktptItalic:
					// For an inverting property, a value of invert is never redundant.
					if (nValHard == (int)FwTextToggleVal.kttvInvert)
						continue;
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
				case FwTextPropType.ktptLeadingIndent:		// == ktptMarginLeading
				case FwTextPropType.ktptTrailingIndent:	// == ktptMarginTrailing
				case FwTextPropType.ktptFirstIndent:
				case FwTextPropType.ktptSpaceBefore:		// == ktptMswMarginTop
				case FwTextPropType.ktptSpaceAfter:		// == ktptMarginBottom
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
				};

				if (nValHard == nValSoft && nVarHard == nVarSoft)
				{
					// Clear.
					if (tpb == null)
						tpb = ttpHard.GetBldr();
					tpb.SetIntPropValues(tpt, -1, -1);
				}
			}

			// String properties.

			ctpt = ttpHard.StrPropCount;
			for (int itpt = 0; itpt < ctpt; itpt++)
			{
				int tpt;
				string strHard;
				strHard = ttpHard.GetStrProp(itpt, out tpt);

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

				string strSoft;
				strSoft = vpsSoft.get_StringProperty(tpt);
				if (strHard == strSoft)
				{
					// Clear.
					if (tpb == null)
						tpb = ttpHard.GetBldr();
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
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called from SimpleRootSite when the selection changes on its rootbox.
		/// </summary>
		/// <remarks>
		/// ENHANCE (FWR-1940): This should be protected internal instead of public
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public virtual void SelectionChanged()
		{
			ClearCurrentSelection(); // Make sure the cached selection is updated

			IVwRootBox rootb = EditedRootBox;
			HandleSelectionChange(rootb, rootb.Selection);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Handles any changes that need to be done when a rootbox's selection changes.
		/// Change the system keyboard when the selection changes.
		/// </summary>
		/// <param name="rootb"></param>
		/// <param name="vwselNew">Selection</param>
		/// -----------------------------------------------------------------------------------
		public virtual void HandleSelectionChange(IVwRootBox rootb, IVwSelection vwselNew)
		{
			CheckDisposed();

			// Allow containing forms, etc. to do special handling when the selection changes.
			if (VwSelectionChanged != null)
				VwSelectionChanged.Invoke(this, new VwSelectionArgs(rootb, vwselNew));

			if (!vwselNew.IsValid)
			{
				// Handling the selection change can sometimes invalidate the selection and
				// install a new one, in which case, we can get out.
				Debug.Assert(vwselNew != rootb.Selection, "Invalid selection");
				return;
			}

			// TimS/EberhardB: If we don't have focus we don't want to change the keyboard,
			// otherwise it might mess up the window that has focus!
			if (!m_control.Focused)
				return;

			SetKeyboardForSelection(vwselNew);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="rootbox"></param>
		/// <param name="selection"></param>
		/// ------------------------------------------------------------------------------------
		private void SetWritingSystemPropertyFromSelection(IVwRootBox rootbox,
			IVwSelection selection)
		{
			// For now, we are only handling SimpleRootSite cases, e.g. for the Data Tree.
			// If we need this in print layout, consider adding the mediator to the Callbacks
			// interface.
			SimpleRootSite rs = rootbox.Site as SimpleRootSite;
			if(rs != null && rs.Mediator != null && selection != null)
			{
				// int ws = SelectionHelper.GetFirstWsOfSelection(rootbox.Selection);
				// Review: Or should it be this? But it returns 0 if there are multiple ws's...
				// which may be good if the combo can handle it; i.e. there is no *one* ws so
				// we shouldn't show one in the combo
				int ws = SelectionHelper.GetWsOfEntireSelection(rootbox.Selection);
				string s = (string)rs.Mediator.PropertyTable.GetValue("WritingSystemHvo", "-1");
				int oldWritingSystemHvo = int.Parse(s);
				if (oldWritingSystemHvo != ws)
				{
					rs.Mediator.PropertyTable.SetProperty("WritingSystemHvo", ws.ToString());
					rs.Mediator.PropertyTable.SetPropertyPersistence("WritingSystemHvo", false);
					m_fSuppressNextWritingSystemHvoChanged = true;
				}
			}
		}

		internal void WritingSystemHvoChanged()
		{
			if (m_fSuppressNextWritingSystemHvoChanged)
			{
				m_fSuppressNextWritingSystemHvoChanged = false;
				return;
			}
			// For now, we are only handling SimpleRootSite cases, e.g. for the Data Tree.
			// If we need this in print layout, consider adding the mediator to the Callbacks
			// interface.
			SimpleRootSite rs = m_callbacks as SimpleRootSite;
			// This property can be changed by selecting an item in the writing system combo.
			// When the user does this we try to update the writing system of the selection.
			// It also gets updated (in order to control the current item in the combo) when
			// the selection changes. We have to be careful this does not trigger an attempt to
			// modify the data.
			if (rs != null && !rs.WasFocused())
				return; //e.g, the dictionary preview pane isn't focussed and shouldn't respond.
			if (rs.RootBox == null || rs.RootBox.Selection == null)
				return;
			string s = (string)rs.Mediator.PropertyTable.GetValue("WritingSystemHvo", "-1");
			rs.Focus();
			int writingSystemHvo = int.Parse(s);
			// will get zero when the selection contains multiple ws's and the ws is
			// in fact different from the current one
			if (writingSystemHvo > 0 &&
				writingSystemHvo != SelectionHelper.GetWsOfEntireSelection(rs.RootBox.Selection))
			{
				ApplyWritingSystem(writingSystemHvo);
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Set the keyboard to match the writing system.
		/// </summary>
		/// <param name="ws">writing system object</param>
		/// -----------------------------------------------------------------------------------
		protected void SetKeyboardForWs(ILgWritingSystem ws)
		{
			if(Callbacks == null || ws == null)
			{
				ActivateDefaultKeyboard();
				return;
			}

			try
			{
				var palasoWs = ((IWritingSystemManager)WritingSystemFactory).Get(ws.Handle) as IWritingSystemDefinition;
				if (palasoWs != null && palasoWs.LocalKeyboard != null)
					palasoWs.LocalKeyboard.Activate();
			}
			catch
			{
				//Debug.WriteLine("EditingHelper.SetKeyboardForWs(" +
				//	ws.WritingSystem + "(" + ws.IcuLocale +
				//	") -> ActivateDefaultKeyboard() in catch block");
				ActivateDefaultKeyboard();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the keyboard for a writing system.
		/// </summary>
		/// <param name="newWs">The new ws.</param>
		/// ------------------------------------------------------------------------------------
		public void SetKeyboardForWs(int newWs)
		{
			CheckDisposed();

			if (Callbacks == null || !Callbacks.GotCacheOrWs || WritingSystemFactory == null)
				return;			// Can't do anything useful, so let's not do anything at all.

			ILgWritingSystem ws = WritingSystemFactory.get_EngineOrNull(newWs);
			SetKeyboardForWs(ws);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Set the keyboard to match what is needed for the selection
		/// </summary>
		/// <param name="vwsel">Selection</param>
		/// -----------------------------------------------------------------------------------
		public void SetKeyboardForSelection(IVwSelection vwsel)
		{
			CheckDisposed();
			if (vwsel == null || Callbacks == null || !Callbacks.GotCacheOrWs)
				return;			// Can't do anything useful, so let's not do anything at all.

			int nWs = SelectionHelper.GetFirstWsOfSelection(vwsel);
			if (nWs == 0)
				return;

			//JohnT: was, LgWritingSystemFactoryClass.Create();
			ILgWritingSystem ws = null;

			if (WritingSystemFactory != null) // this sometimes happened in our tests when the window got/lost focus
				ws = WritingSystemFactory.get_EngineOrNull(nWs);

			SetKeyboardForWs(ws);

			// Should also set the WS property. Otherwise the ws combo box doesn't get
			// updated when using tab key to go to the next field.
			SetWritingSystemPropertyFromSelection(Callbacks.EditedRootBox, vwsel);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Activates the default keyboard.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ActivateDefaultKeyboard()
		{
			Keyboard.Controller.ActivateDefaultKeyboard();
		}

		/// <summary>
		/// Allow a change to BestStyleName not to cause all the work in the next function.
		/// </summary>
		public bool SuppressNextBestStyleNameChanged
		{
			get { return m_fSuppressNextBestStyleNameChanged; }
			set { m_fSuppressNextBestStyleNameChanged = value; }
		}

		internal void BestStyleNameChanged()
		{
			if (m_fSuppressNextBestStyleNameChanged)
			{
				m_fSuppressNextBestStyleNameChanged = false;
				return;
			}
			// For now, we are only handling SimpleRootSite cases, e.g. for the Data Tree.
			// If we need this in print layout, consider adding the mediator to the Callbacks
			// interface.
			SimpleRootSite rs = m_callbacks as SimpleRootSite;
			// This property can be changed by selecting an item in the combined styles combo.
			// When the user does this we try to update the style of the selection.
			// It also gets updated (in order to control the current item in the combo) when
			// the selection changes. We have to be careful this does not trigger an attempt to
			// modify the data.
			if (rs != null && !rs.WasFocused())
				return; //e.g, the dictionary preview pane isn't focussed and shouldn't respond.
			if (rs == null || rs.RootBox == null || rs.RootBox.Selection == null)
				return;
			string styleName = rs.Mediator.PropertyTable.GetStringProperty("BestStyleName", null);
			if (styleName == null)
				return;
			rs.Focus();
			string paraStyleName = GetParaStyleNameFromSelection();
			string charStyleName = GetCharStyleNameFromSelection();
			if ((styleName == String.Empty && charStyleName != String.Empty) ||
				(paraStyleName != styleName && charStyleName != styleName))
			{
				ApplyStyle(styleName);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deal with losing focus
		/// </summary>
		/// <param name="newFocusedControl">The new focused control.</param>
		/// <param name="fIsChildWindow"><c>true</c> if the <paramref name="newFocusedControl"/>
		/// is a child window of the current application.</param>
		/// ------------------------------------------------------------------------------------
		public void LostFocus(Control newFocusedControl, bool fIsChildWindow)
		{
			CheckDisposed();

			//Debug.WriteLine(string.Format("EditingHelper.LostFocus:\n\t\t\tlost: {3} ({4}), Name={5}\n\t\t\tnew: {0} ({1}), Name={2}",
			//    newFocusedControl != null ? newFocusedControl.ToString() : "<null>",
			//    newFocusedControl != null ? newFocusedControl.Handle.ToInt32() : -1,
			//    newFocusedControl != null ? newFocusedControl.Name : "<empty>",
			//    m_control, m_control.Handle, m_control.Name));
			// Switch back to the UI keyboard so edit boxes in dialogs, toolbar controls, etc.
			// won't be using the UI of the current run in this view. But only if the current
			// focus pane is not another view...switching the input language AFTER another view
			// has received focus behaves like the user selecting the input language of this
			// view in the context of the other one, with bad consequences.
			// We don't want to do this if we're switching to a different application. Windows
			// will take care of switching the keyboard.
			if ((ShouldRestoreKeyboardSwitchingTo(newFocusedControl)) && fIsChildWindow)
				ActivateDefaultKeyboard();
		}

		private static bool ShouldRestoreKeyboardSwitchingTo(Control newFocusedControl)
		{
			if (newFocusedControl is IRootSite)
				return false;
			if (newFocusedControl is SimpleRootSite.ISuppressDefaultKeyboardOnKillFocus)
				return false;
			return true;
		}

		/// <summary>
		/// Special processing to be done associated root site gets focus.
		/// </summary>
		internal void GotFocus()
		{
		}
		#endregion

		#region Cut/copy/paste handling methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Useful in stand-alone RootSites such as FwTextBox and LabeledMultiStringView to
		/// process cut/copy/paste keys in the OnKeyDown handler.
		/// </summary>
		/// <param name="e">The KeyEventArgs parameter</param>
		/// <returns>True if the key was handled, false if it was not</returns>
		/// ------------------------------------------------------------------------------------
		public bool HandleOnKeyDown(KeyEventArgs e)
		{
			CheckDisposed();

			if (m_control == null || !m_control.Visible)
				return false;

			// look for Ctrl-X to cut
			if (e.KeyCode == Keys.X && e.Control)
			{
				if (!CopySelection())
					return false;

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
			if (e.KeyCode == Keys.V && e.Control)
				return PasteClipboard();

			// if the key was not handled then pass it on.
			return false;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Copy the current selection to the clipboard
		/// </summary>
		/// <returns><c>true</c> if copying was successful, otherwise <c>false</c>.</returns>
		/// -----------------------------------------------------------------------------------
		public bool CopySelection()
		{
			CheckDisposed();
			if (!CanCopy())
				return false;

			var vwsel = EditedRootBox.Selection;
			// Get a copy of the selection as a TsString, and store it in the clipboard, together with
			// the writing system factory.
			ITsString tss = null;
			var vwsite = EditedRootBox.Site as SimpleRootSite;
			if (vwsite != null)
				tss = vwsite.GetTsStringForClipboard(vwsel);
			if (tss == null)
				vwsel.GetSelectionString(out tss, "; ");

			// This is pathological for a range, but apparently it can happen, e.g., for a picture selection.
			// See LT-8147.
			if (tss == null || tss.Length == 0)
				return false;

			CopyTssToClipboard(tss);

			return true;
		}
		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Put a ITsString on the clipboard
		/// </summary>
		/// --------------------------------------------------------------------------------
		public static void SetTsStringOnClipboard(ITsString tsString, bool fCopy,
			ILgWritingSystemFactory writingSystemFactory)
		{
			TsStringWrapper wrapper = new TsStringWrapper(tsString, writingSystemFactory);
			IDataObject dataObject = new DataObject();
			dataObject.SetData(TsStringWrapper.TsStringFormat, false, wrapper);
			dataObject.SetData(DataFormats.Serializable, true, wrapper);
			dataObject.SetData(DataFormats.UnicodeText, true, tsString.Text.Normalize(NormalizationForm.FormC));
			ClipboardUtils.SetDataObject(dataObject, fCopy);
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Gets a ITsString from the clipboard
		/// </summary>
		/// <returns></returns>
		/// --------------------------------------------------------------------------------
		public ITsString GetTsStringFromClipboard(ILgWritingSystemFactory writingSystemFactory)
		{
			IDataObject dataObj = ClipboardUtils.GetDataObject();
			if (dataObj == null)
				return null;
			TsStringWrapper wrapper = dataObj.GetData(TsStringWrapper.TsStringFormat) as TsStringWrapper;
			ITsString tss = wrapper == null ? null : wrapper.GetTsString(writingSystemFactory);
			if (tss == null)
				return null;

			ILgWritingSystemFactory wsf = writingSystemFactory;
			int destWs;
			PasteStatus pasteStatus = DeterminePasteWs(wsf, out destWs);
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copies the given ITsString to clipboard.
		/// </summary>
		/// <param name="tss">ITsString to copy to clipboard.</param>
		/// ------------------------------------------------------------------------------------
		internal void CopyTssToClipboard(ITsString tss)
		{
			Debug.Assert(tss != null && tss.Length > 0); // if this asserts it is likely that
			// the user selected a footnote marker but the TextRepOfObj() method isn't
			// implemented.

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

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Cuts the current selection
		/// </summary>
		/// <returns><c>true</c> if cutting was successful, otherwise <c>false</c>.</returns>
		/// -----------------------------------------------------------------------------------
		public bool CutSelection()
		{
			CheckDisposed();
			try
			{
				if (!m_fEditable || !CopySelection())
					return false;

				// The copy succeeded (otherwise we would have got an exception and wouldn't be
				// here), now delete the range of text that has been copied to the
				// clipboard.
				DeleteSelectionTask(Resources.ksUndoCut, Resources.ksRedoCut);
				return true;
			}
			catch (Exception ex)
			{
				MessageBox.Show(string.Format(Resources.ksCutFailed, ex.Message), Resources.ksCutFailedCaption,
					MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return false;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Delete the current selection. Caller is responsible for UOW. Actually simulates
		/// pressing the delete key; will delete one char forward if selection is an IP.
		/// </summary>
		/// <returns><c>true</c> if deleting was successful, otherwise <c>false</c>.</returns>
		/// -----------------------------------------------------------------------------------
		public void DeleteSelection()
		{
			CheckDisposed();

			if (m_control == null || m_callbacks == null || m_callbacks.EditedRootBox == null ||
				!m_callbacks.GotCacheOrWs)
			{
				return;
			}

			HandleKeyPress((char)(int)VwSpecialChars.kscDelForward, Keys.None);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Answer true if the given selection is in a single property (it may be an IP or range),
		/// and that property is editable.
		/// </summary>
		/// <param name="sel">The selection.</param>
		/// ------------------------------------------------------------------------------------
		protected bool IsSelectionInOneEditableProp(IVwSelection sel)
		{
			if (sel == null || sel.SelType != VwSelType.kstText || !sel.IsEditable)
				return false;
			int ichE, ichA, hvoObjE, hvoObjA, tagE, tagA, wsE, wsA;
			ITsString tssE, tssA;
			bool fAssocPrev;
			sel.TextSelInfo(true, out tssE, out ichE, out fAssocPrev, out hvoObjE, out tagE, out wsE);
			sel.TextSelInfo(false, out tssA, out ichA, out fAssocPrev, out hvoObjA, out tagA, out wsA);
			if (hvoObjE != hvoObjA || tagE != tagA || wsE != wsA)
				return false;
			int cLevA = sel.CLevels(false);
			int cLevE = sel.CLevels(true);
			if (cLevA != cLevE)
				return false;
			for (int i = 0; i < cLevA - 1; i++)
			{
				int ihvoA, ihvoE, cPropPreviousA, cPropPreviousE;
				IVwPropertyStore vps;
				sel.PropInfo(true, i, out hvoObjE, out tagE, out ihvoE, out cPropPreviousE, out vps);
				sel.PropInfo(false, i, out hvoObjA, out tagA, out ihvoA, out cPropPreviousA, out vps);
				if (hvoObjE != hvoObjA || tagE != tagA || ihvoE != ihvoA || cPropPreviousA != cPropPreviousE)
					return false;
			}

			return sel.CanFormatChar;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Answer true if the selection is in a single property (it may be an IP or range),
		/// and that property is editable.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsSelectionInOneEditableProp()
		{
			CheckDisposed();
			if (m_callbacks == null || m_callbacks.EditedRootBox == null)
				return false;
			return IsSelectionInOneEditableProp(m_callbacks.EditedRootBox.Selection);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is stronger than IsSelectionInOneEditableProp, in addition, it must be a
		/// property that can really store formatting information
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool IsSelectionInOneFormattableProp()
		{
			CheckDisposed();
			if (!IsSelectionInOneEditableProp())
				return false;
			if(m_callbacks == null || m_callbacks.EditedRootBox == null)
				return false;
			IVwSelection sel = m_callbacks.EditedRootBox.Selection;
			int ich, hvoObj, tag, ws;
			bool fAssocPrev;
			ITsString tss;
			sel.TextSelInfo(true, out tss, out ich, out fAssocPrev, out hvoObj, out tag, out ws);
			ISilDataAccess sda = m_callbacks.EditedRootBox.DataAccess;
			IFwMetaDataCache mdc = sda.MetaDataCache;
			if (mdc == null)
				return true; // no further info, assume OK.
			if (tss == null || tag == 0)
				return false; // No string to check.
			if (IsUnknownProp(mdc, tag))
				return false;
			CellarPropertyType cpt = (CellarPropertyType)mdc.GetFieldType(tag);
			// These four types can store embedded formatting.
			return cpt == CellarPropertyType.String ||
				cpt == CellarPropertyType.MultiString;
		}

		/// <summary>
		/// Return true if this is a property the mdc can't tell us about.
		/// This is overridden in RootSite, where we can cast it to IFwMetaDataCacheManaged and really find out.
		/// </summary>
		/// <returns></returns>
		protected virtual bool IsUnknownProp(IFwMetaDataCache mdc, int tag)
		{
			return false;
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the clipboard contents as a string, or return null string if not found
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public string GetClipboardAsString()
		{
			CheckDisposed();
			try
			{
				IDataObject dobj = ClipboardUtils.GetDataObject();
				return (string)dobj.GetData(DataFormats.StringFormat);
			}
			catch
			{
				return null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Paste data from the clipboard into the view. Caller is reponsible to make UOW.
		/// </summary>
		/// <returns>True if the paste succeeded, false otherwise</returns>
		/// ------------------------------------------------------------------------------------
		public virtual bool PasteClipboard()
		{
			CheckDisposed();
			// Do nothing if command is not enabled. Needed for Ctrl-V keypress.
			if (!CanPaste() || Callbacks == null || Callbacks.EditedRootBox == null ||
				!Callbacks.GotCacheOrWs  || CurrentSelection == null)
				return false;

			IVwSelection vwsel = CurrentSelection.Selection;
			if (vwsel == null)
				return false;

			// Handle anything needed immediately before the paste.
			Callbacks.PrePasteProcessing();

			ITsTextProps[] vttp;
			IVwPropertyStore[] vvps;
			int cttp;
			SelectionHelper.GetSelectionProps(vwsel, out vttp, out vvps, out cttp);
			if (cttp == 0)
				return false;

			ChangeStyleForPaste(vwsel, ref vttp);

			ITsString tss = GetTextFromClipboard(vwsel, vwsel.CanFormatChar, vttp[0]);
			return PasteCore(tss);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Core logic for Paste command (without modifying the clipboard, to support testing).
		/// </summary>
		/// <param name="tss">The TsString to paste.</param>
		/// <returns>True if that paste succeeded, false otherwise</returns>
		/// ------------------------------------------------------------------------------------
		public bool PasteCore(ITsString tss)
		{
			IVwSelection vwsel = EditedRootBox.Selection;
			try
			{
				if (tss != null)
				{
					// At this point, we may need to override internal formatting values
					// for certain target rootsites. We do this with an event handler that the
					// rootsite can register for its editing helper. (See LT-1445.)
					if (PasteFixTssEvent != null)
					{
						FwPasteFixTssEventArgs args = new FwPasteFixTssEventArgs(tss, new TextSelInfo(EditedRootBox));
						PasteFixTssEvent(this, args);
						tss = args.TsString;
					}
					// Avoid possible crashes if we know we can't paste.  (See LT-11150 and LT-11219.)
					if (vwsel == null || !vwsel.IsValid || tss == null)
					{
						MiscUtils.ErrorBeep();
						return false;
					}
					// ENHANCE (FWR-1732):
					// Will need to split current UOW into 2 when paste involves deleting one or more paragraphs
					// because of a range selection - the delete processing requests a new selection at the end of the
					// UOW which destoys the current selection and makes the paste fail. However, don't want to
					// re-introduce FWR-1734 where delete succeeds and when paste fails. Would need to do undo to
					// restore deleted text, but may seem strange to user since a redo task will now appear.
					vwsel.ReplaceWithTsString(TsStringUtils.RemoveIllegalXmlChars(tss));
				}
			}
			catch (Exception e)
			{
				if (e is COMException && (uint)((COMException)e).ErrorCode == 0x80004005) // E_FAIL
				{
					MiscUtils.ErrorBeep();
					return false;
				}
				Logger.WriteError(e); // TE-6908/LT-6781
				throw new ContinuableErrorException("Error during paste. Paste has been undone.", e);
			}

			IVwRootSite rootSite = EditedRootBox.Site;
			rootSite.ScrollSelectionIntoView(null, VwScrollSelOpts.kssoDefault);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the text from clipboard.
		/// </summary>
		/// <param name="vwsel">The selection.</param>
		/// <param name="fCanFormat">set to <c>true</c> to allow hard formatting in the text,
		/// otherwise <c>false</c>.</param>
		/// <param name="ttpSel">The text properties of the selection.</param>
		/// <returns>Text from clipboard</returns>
		/// ------------------------------------------------------------------------------------
		protected ITsString GetTextFromClipboard(IVwSelection vwsel, bool fCanFormat, ITsTextProps ttpSel)
		{
			ITsString tss = GetTsStringFromClipboard(WritingSystemFactory);
			if (tss != null && !ValidToReplaceSelWithTss(vwsel, tss))
				tss = null;

			if (!fCanFormat && tss != null)
			{
				// remove formatting from the TsString
				ITsStrFactory tsf = TsStrFactoryClass.Create();
				string str = tss.Text;
				tss = tsf.MakeStringWithPropsRgch(str, str.Length, ttpSel);
			}

			if (tss == null)
			{	// all else didn't work, so try with an ordinary string
				string str = ClipboardUtils.GetText();
				ITsStrFactory tsf = TsStrFactoryClass.Create();
				tss = tsf.MakeStringWithPropsRgch(str, str.Length, ttpSel);
			}

			return tss;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the given TsString can be pasted in the context of the target
		/// selection. Applications can override this to enforce specific exceptions.
		/// </summary>
		/// <param name="vwselTargetLocation">selection to be replaced by paste operation</param>
		/// <param name="tss">The TsString from the clipboard</param>
		/// <returns>Base implementation always returns <c>true</c></returns>
		/// ------------------------------------------------------------------------------------
		protected virtual bool ValidToReplaceSelWithTss(IVwSelection vwselTargetLocation, ITsString tss)
		{
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Used to change the style at a paste location right before the paste begins
		/// </summary>
		/// <param name="vwsel"></param>
		/// <param name="vttp"></param>
		/// ------------------------------------------------------------------------------------
		protected virtual void ChangeStyleForPaste(IVwSelection vwsel, ref ITsTextProps[] vttp)
		{
			// default is to do nothing
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Determine if the deleting of text is possible.
		/// </summary>
		/// <returns>Returns <c>true</c> if cutting is possible.</returns>
		/// -----------------------------------------------------------------------------------
		public virtual bool CanDelete()
		{
			CheckDisposed();
			if (Callbacks != null && Callbacks.EditedRootBox != null && m_fEditable)
			{
				IVwSelection vwsel = Callbacks.EditedRootBox.Selection;
				if (vwsel != null)
					// CanFormatChar is true only if the selected text is editable.
					// TE-5774 Added VwSelType.kstPicture selection type comparison because
					//	delete was being disabled for pictures
					return (vwsel.CanFormatChar || vwsel.SelType == VwSelType.kstPicture);
			}
			return false;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Determine if the cutting of text into the clipboard is possible.
		/// </summary>
		/// <returns>Returns <c>true</c> if cutting is possible.</returns>
		/// <remarks>Formerly <c>AfVwRootSite::CanCut()</c>.</remarks>
		/// -----------------------------------------------------------------------------------
		public virtual bool CanCut()
		{
			CheckDisposed();
			if (Callbacks != null && Callbacks.GotCacheOrWs && Callbacks.EditedRootBox != null && m_fEditable)
			{
				IVwSelection vwsel = Callbacks.EditedRootBox.Selection;
				if (vwsel != null)
					// CanFormatChar is true only if the selected text is editable.
					return vwsel.IsRange && vwsel.CanFormatChar;
			}
			return false;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Determine if the copying of text into the clipboard is possible.
		/// </summary>
		/// <returns>Returns <c>true</c> if copying is possible.</returns>
		/// -----------------------------------------------------------------------------------
		public virtual bool CanCopy()
		{
			CheckDisposed();
			if (Callbacks != null && Callbacks.GotCacheOrWs && Callbacks.EditedRootBox != null)
			{
				IVwSelection vwsel = Callbacks.EditedRootBox.Selection;
				if (vwsel == null || !vwsel.IsRange || !vwsel.IsValid)
					return false; // No text selected.

				int cttp;
				vwsel.GetSelectionProps(0, ArrayPtr.Null, ArrayPtr.Null, out cttp);
				// No text selected.
				if (cttp == 0)
					return false;
				return true;
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <returns>true if we are in an editable location</returns>
		/// ------------------------------------------------------------------------------------
		public virtual bool CanEdit()
		{
			CheckDisposed();
			if (Callbacks != null && Callbacks.EditedRootBox != null)
			{
				return CanEdit(Callbacks.EditedRootBox.Selection);
			}
			else
				return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <returns>true if editing would be possible for the specified selection</returns>
		/// ------------------------------------------------------------------------------------
		public virtual bool CanEdit(IVwSelection vwsel)
		{
			CheckDisposed();
			if (m_fEditable)
			{
				// CanFormatChar is true only if the selected text is editable.
				if (vwsel != null)
				{
					// When "&& vwsel.CanFormatChar" is included in the conditional,
					//  it causes a problem in TE (TE-3339)
					// But (JohnT, 18 Nov), we want to be able to get some idea, especially
					// for insertion points, since this is used to help control mouse pointers.
					if (vwsel.IsRange)
						return true; // Don't try to be smart, ranges are too ambiguous.
					return vwsel.IsEditable;
				}
				else
					return false;
			}
			else
			{
				return false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool ClipboardContainsString()
		{
			CheckDisposed();
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

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Determine if pasting of text from the clipboard is possible.
		/// </summary>
		/// <returns>Returns <c>true</c> if pasting is possible.</returns>
		/// <remarks>Formerly <c>AfVwRootSite::CanPaste()</c>.</remarks>
		/// -----------------------------------------------------------------------------------
		public virtual bool CanPaste()
		{
			CheckDisposed();
			if (m_callbacks != null && m_callbacks.EditedRootBox != null && m_fEditable &&
				CurrentSelection != null && m_control != null && m_control.Visible)
			{
				var vwsel = CurrentSelection.Selection;
				// CanFormatChar is true only if the selected text is editable.
				if (vwsel != null && vwsel.CanFormatChar)
					return ClipboardContainsString();
			}
			return false;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Make a selection that includes all the text
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public void SelectAll()
		{
			CheckDisposed();
			if (m_callbacks == null || m_callbacks.EditedRootBox == null ||
				!m_callbacks.GotCacheOrWs || m_control == null)
			{
				return;
			}

			m_control.Focus();

			IVwRootBox rootb = m_callbacks.EditedRootBox;

			using (new WaitCursor(m_control))
			{   // creates a wait cursor and makes it active until the end of the block.
				// Due to some code we don't understand in the arrow key functions, this simulating
				// control-end has no effect unless this pane has focus. So don't use this old approach.
				//rootb.MakeSimpleSel(true, false, false, true);
				//// Simulate a Ctrl-Shift-End keypress:
				//rootb.OnExtendedKey((int)Keys.End, VwShiftStatus.kgrfssShiftControl,
				//    1); // logical arrow key behavior
				IVwSelection selStart = rootb.MakeSimpleSel(true, false, false, false);
				IVwSelection selEnd = rootb.MakeSimpleSel(false, false, false, false);
				if (selStart != null && selEnd != null)
					rootb.MakeRangeSelection(selStart, selEnd, true);
			}
		}

		/// ------------------------------------------------------------------------------------
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
		/// ------------------------------------------------------------------------------------
		public virtual PasteStatus DeterminePasteWs(ILgWritingSystemFactory wsf, out int destWs)
		{
			destWs = -1;
			return PasteStatus.PreserveWs;
		}
		#endregion

		#region WordEventArgs struct
		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// Holds the arguments for the IsWordBreak method.
		/// </summary>
		/// ----------------------------------------------------------------------------------------
		private struct WordEventArgs
		{
			/// <summary>The source that kicked off the method</summary>
			public EditingHelper.WordEventSource Source;
			/// <summary>Character that user typed</summary>
			/// <remarks>Only valid if <see cref="Source"/> is
			/// <see cref="EditingHelper.WordEventSource.Character"/></remarks>
			public char Char;
			/// <summary>Key that user pressed</summary>
			/// <remarks>Only valid if <see cref="Source"/> is
			/// <see cref="EditingHelper.WordEventSource.KeyDown"/></remarks>
			public Keys Key;

			/// <summary>
			/// Initializes the struct for a <see cref="EditingHelper.WordEventSource.LoseFocus"/>
			/// or <see cref="EditingHelper.WordEventSource.MouseClick"/>
			/// </summary>
			/// <param name="source">The source that kicked off the method</param>
			public WordEventArgs(EditingHelper.WordEventSource source)
				: this(source, char.MinValue, Keys.None)
			{
			}

			/// <summary>
			/// Initializes the struct for a <see cref="EditingHelper.WordEventSource.Character"/>
			/// </summary>
			/// <param name="source">The source that kicked off the method</param>
			/// <param name="c">The character that the user entered</param>
			public WordEventArgs(EditingHelper.WordEventSource source, char c)
				: this(source, c, Keys.None)
			{
			}

			/// <summary>
			/// Initializes the struct for a <see cref="EditingHelper.WordEventSource.KeyDown"/>
			/// </summary>
			/// <param name="source">The source that kicked off the method</param>
			/// <param name="key">The key that the user pressed</param>
			public WordEventArgs(EditingHelper.WordEventSource source, Keys key)
				: this(source, char.MinValue, key)
			{
			}

			/// <summary>
			/// Initalizes all fields
			/// </summary>
			/// <param name="source">The source that kicked off the method</param>
			/// <param name="c">The character that the user entered</param>
			/// <param name="key">The key that the user pressed</param>
			public WordEventArgs(EditingHelper.WordEventSource source, char c, Keys key)
			{
				Source = source;
				Key = key;
				Char = c;
			}
		}
		#endregion
	}
	#endregion

}
