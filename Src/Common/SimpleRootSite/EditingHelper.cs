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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Enchant;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Resources;
using SIL.Utils;
using XCore;
using SIL.FieldWorks.Common.UIAdapters;
using SIL.FieldWorks.Common.FwUtils;

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
	}
	#endregion

	#region FwPasteFixTssEvent handler and args class
	/// <summary>
	///
	/// </summary>
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
		private ITsString m_tss;
		private bool m_fHandled;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:FwPasteFixWsEventArgs"/> class.
		/// </summary>
		/// <param name="tss">The ITsString to paste.</param>
		/// ------------------------------------------------------------------------------------
		public FwPasteFixTssEventArgs(ITsString tss)
		{
			m_tss = tss;
			m_fHandled = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the ts string.
		/// </summary>
		/// <value>The ts string.</value>
		/// ------------------------------------------------------------------------------------
		public ITsString TsString
		{
			get { return m_tss; }
			set { m_tss = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether [event handled].
		/// </summary>
		/// <value><c>true</c> if [event handled]; otherwise, <c>false</c>.</value>
		/// ------------------------------------------------------------------------------------
		public bool EventHandled
		{
			get { return m_fHandled; }
			set { m_fHandled = value; }
		}
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
	public class EditingHelper : IVwNotifyObjCharDeletion, IFWDisposable
	{
		#region Enums
		/// <summary></summary>
		public enum SpellCheckStatus
		{
			/// <summary>spell checking is enabled but word is in dictionary</summary>
			WordInDictionary,
			/// <summary>spell checking enabled with word not in dictionary,
			/// whether or not suggestions exist</summary>
			Enabled,
			/// <summary>spell checking disabled</summary>
			Disabled
		}

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
		#endregion

		#region Constants
		/// <summary>maximum number of spelling suggestions that will appear in the
		/// root level of the context menu. Additional suggestions will appear in a submenu.
		/// </summary>
		private int kMaxSpellingSuggestionsInRootMenu = 7;
		#endregion

		#region Member variables
		private UserControl m_control; // currently either SimpleRootSite or PublicationControl.
		private IEditingCallbacks m_callbacks;
		/// <summary>handle of current system keyboard/language</summary>
		private IntPtr m_hklActive;
		/// <summary>current Keyman keyboard</summary>
		private string m_sActiveKeymanKbd; //"xxxUnknownyyy";
		/// <summary>Current keyboard's langid</summary>
		private int m_nActiveLangId;
		/// <summary>count of pending Keyman keyboard changes to ignore</summary>
		private int m_cSelectLangPending;
		/// <summary>True if we support right-to-left in this DB</summary>
		private bool m_fCanDoRtl = true;
		/// <summary>The default cursor to use</summary>
		private Cursor m_defaultCursor;

		private SpellCheckStatus m_spellCheckStatus = SpellCheckStatus.Disabled;
		/// <summary>
		/// This overrides the normal Ibeam cursor when over text (not when over hot links or
		/// hot pictures) if the cursor is over something that can't be edited.
		/// </summary>
		private Cursor m_readOnlyCursor;
		/// <summary>Pointer to the data object stored on the clipboard</summary>
		private static ILgTsDataObject s_dobjClipboard;
		/// <summary>True if editing commands should be handled, false otherwise</summary>
		private bool m_fEditable = true;
		/// <summary>A SelectionHelper that holds the info for the current selection (updated
		/// every time the selection changes)</summary>
		protected SelectionHelper m_viewSelection;
		private int m_lastKeyboardWS = -1;
		/// <summary>Flag to prevent deletion of an object</summary>
		protected bool m_preventObjDeletions;
		private bool m_testMode;	// allows tests to skip some code
		/// <summary>  Snapshot of the selection state before an edit.</summary>
		protected TextSelInfo m_tsiOrig;

		/// <summary>Event for changing properties of a pasted TsString</summary>
		public event FwPasteFixTssEventHandler PasteFixTssEvent;
		#endregion

		#region Enumerations
		/// <summary>The source that kicked off the <see cref="CommitIfWord"/>
		/// method</summary>
		public enum WordEventSource
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
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			// Should this be zapped?
			//Marshal.FreeCoTaskMem(m_hklActive);
			//m_hklActive = IntPtr.Zero;
			m_control = null;
			m_callbacks = null;
			m_viewSelection = null;
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clears the ts string clipboard.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void ClearTsStringClipboard()
		{
			// Free data object put on the clipboard. Otherwise we'll get memory leaks.
			// Flushing the clipboard actually doesn't remove it from the clipboard.
			// It frees the data object, but first gets a text-only (no TsString)
			// representation of the object.
			if (s_dobjClipboard != null)
			{
				Application.OleRequired();
				if (Win32.OleIsCurrentClipboard(s_dobjClipboard))
					Win32.OleFlushClipboard();
			}
			s_dobjClipboard = null;
			// Don't need this notification any more.
			Application.ApplicationExit -= new EventHandler(Application_ApplicationExit);
		}

		#endregion IDisposable & Co. implementation

		#region IVwNotifyObjCharDeletion implementation
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method is used to get a notification when an owning object-replacement character
		/// (ORC) is deleted. ORCs are used to mark locations in the text of things like pictures
		/// or footnotes. In the case of footnotes, when an owning footnote ORC is deleted, we
		/// need to find the corresponding footnote and delete it.
		/// </summary>
		/// <param name="guid">The GUID of the footnote being deleted.</param>
		/// ------------------------------------------------------------------------------------
		public virtual void ObjDeleted(ref Guid guid)
		{
			CheckDisposed();
			// Needs to be implemented in a derived class.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether object deletion is allowed.
		/// </summary>
		/// <value><c>true</c> if preventing object deletions; otherwise, <c>false</c>.</value>
		/// ------------------------------------------------------------------------------------
		public virtual bool PreventObjDeletions
		{
			get { return m_preventObjDeletions; }
			set { m_preventObjDeletions = value; }
		}
		#endregion

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
			using (ArrayPtr ptr = MarshalEx.ArrayToNative(cws, typeof(int)))
			{
				wsf.GetWritingSystems(ptr, cws);
				int[] vwsT = (int[])MarshalEx.NativeToArray(ptr, cws, typeof(int));
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
			{
				return;
			}
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
				// Some change was made.
				vwsel.SetSelectionProps(cttp, vttp);
				SelectionChanged(Callbacks.EditedRootBox, vwsel);
			}
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

			using (ArrayPtr ptr = MarshalEx.ArrayToNative(cws, typeof(int)))
			{
				wsf.GetWritingSystems(ptr, cws);
				int[] vws = (int[])MarshalEx.NativeToArray(ptr, cws, typeof(int));

				IWritingSystem ws;
				for (int iws = 0; iws < cws; iws++)
				{
					if (vws[iws] == 0)
						continue;
					ws = wsf.get_EngineOrNull(vws[iws]);
					if (ws == null || WritingSystemFactory.GetWsFromStr(ws.IcuLocale) == 0)
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
		/// </summary>
		/// <param name="e"></param>
		/// <param name="modifiers"></param>
		/// <param name="graphics"></param>
		/// -----------------------------------------------------------------------------------
		public virtual void OnKeyPress(KeyPressEventArgs e, Keys modifiers, IVwGraphics graphics)
		{
			CheckDisposed();
			bool handled = false;

			Form form = m_control.FindForm();
			if (form != null)
			{
				switch (e.KeyChar)
				{
					// (EberhardB): This code is reimplementing Windows.System.Forms code. We
					// should not do this but rather use the "official" mechanisms, i.e. let
					// the control class deal with Tab key and set our properties correctly.
					// IF we decide that we need code similiar to this we should at least
					// use Control.SelectNextControl().
					// This comment is part of the fix for LT-9049.
					//case '\t':
					//    if (m_control is SimpleRootSite && (m_control as SimpleRootSite).HandleTabAsControl)
					//    {
					//        bool fTabForward = (System.Windows.Forms.Control.ModifierKeys != Keys.Shift);
					//        Control nextControl = NextTabStop(m_control, fTabForward);
					//        if (nextControl != m_control)
					//            nextControl.Focus();
					//        handled = true;
					//    }
					//    break;
					case '\r':
						// REVIEW (EberhardB): Why can't we let the Control class deal with
						// this? We might have to change SimpleRootSite.IsInputKey/IsInputChar.
						if (form.AcceptButton != null)
							handled = HandleEnterKey();
						break;
					case (char)Win32.VirtualKeycodes.VK_ESCAPE:
						if (form.CancelButton != null)
							return;		// we'll handle this in the caller.  See SimpleRootSite for details.
						break;
				}
			}

			if (!handled && !IsIgnoredKey(e, modifiers) && CanEdit()) // Only process keys that aren't ignored and haven't been handled
			{
				CommitIfWord(new WordEventArgs(WordEventSource.Character, e.KeyChar));
				HandleKeyPress(e.KeyChar, false, modifiers, graphics);
			}
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the enter key.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected virtual bool HandleEnterKey()
		{
			Form form = m_control.FindForm();
			if (form != null)
			{
				form.AcceptButton.PerformClick();
				return true;
			}
			return false;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// User pressed a key.
		/// </summary>
		/// <param name="e"></param>
		/// <param name="graphics"></param>
		/// <returns><c>true</c> if we handled the key, <c>false</c> otherwise (e.g. we're
		/// already at the end of the rootbox and the user pressed down arrow key).</returns>
		/// -----------------------------------------------------------------------------------
		public virtual bool OnKeyDown(KeyEventArgs e, IVwGraphics graphics)
		{
			CheckDisposed();
			if (Callbacks == null || Callbacks.EditedRootBox == null)
				return true;

			bool fRet = true;
			CommitIfWord(new WordEventArgs(WordEventSource.KeyDown, e.KeyCode));
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
				if (e.KeyCode == Keys.Enter && !CanEdit())
					return fRet;

				VwShiftStatus ss = GetShiftStatus(e.Modifiers);
				int keyVal = e.KeyValue;
				if (Control is SimpleRootSite)
					keyVal = (Control as SimpleRootSite).ConvertKeyValue(keyVal);
				fRet = CallOnExtendedKey(keyVal, ss);

				// REVIEW (EberhardB): I'm not sure if it's generally valid
				// to call ScrollSelectionIntoView from HandleKeyDown
				HandleKeyDown(e, ss);
				// The properties of the selection may be changed by pressing these
				// navigation keys even if the selection does not move (e.g. TE-7098
				// when the right arrow key is pressed after a chapter number when
				// there is no text following the chapter number).
				m_viewSelection = null;
				break;

			case Keys.Delete:
				if (!CanEdit())
					return fRet;
				// The Microsoft world apparently doesn't know that <DEL> is an ASCII
				// character just as  much as <BS>, so TranslateMessage generates a
				// WM_CHAR message for <BS>, but not for <DEL>!  Is there a better
				// way to overcome this braindeadness?
				HandleKeyPress((char)(int)VwSpecialChars.kscDelForward, true,
					e.Modifiers, graphics);
				break;

			case Keys.Space:
				if (CanEdit() && (e.Modifiers & Keys.Control) == Keys.Control)
				{
					e.Handled = true;
					RemoveCharFormattingWithUndo();
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

			if ((modifiers & Keys.Shift) == Keys.Shift && e.KeyChar == '\r')
			{
				ignoredKey = true;
			}
			else if ((modifiers & Keys.Alt) == Keys.Alt)
			{
				// For some languages, Alt is commonly used for keyboard input.  See LT-4182.
				ignoredKey = false;
			}
			else if ((modifiers & Keys.Control) == Keys.Control)
			{
				// only control-backspace, control-forward delete and control-M (same as return
				// key) will be passed on for processing
				ignoredKey = !((int)e.KeyChar == (int)VwSpecialChars.kscBackspace ||
					(int)e.KeyChar == (int)VwSpecialChars.kscDelForward ||
					e.KeyChar == '\r');
			}

			return ignoredKey;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Call commit if user leaves a word
		/// </summary>
		/// <param name="args">Information about what action happened and what key
		/// was pressed</param>
		/// -----------------------------------------------------------------------------------
		public void CommitIfWord(WordEventArgs args)
		{
			CheckDisposed();
			if (IsWordBreak(args) && Callbacks != null && Callbacks.EditedRootBox != null)
			{
				Commit();
			}
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
		protected virtual bool IsWordBreak(WordEventArgs args)
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
					charProps = WritingSystemFactory.UnicodeCharProps;
					Debug.Assert(charProps != null, "UnicodeCharProps returned null");

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
		/// </summary>
		/// <param name="keyChar">The pressed character key</param>
		/// <param name="fCalledFromKeyDown">true if this method gets called from OnKeyDown
		/// (to handle Delete)</param>
		/// <param name="modifiers">key modifies - shift status, etc.</param>
		/// <param name="graphics">graphics object for process input</param>
		/// -----------------------------------------------------------------------------------
		public virtual void HandleKeyPress(char keyChar, bool fCalledFromKeyDown, Keys modifiers,
			IVwGraphics graphics)
		{
			CheckDisposed();
			// REVIEW (EberhardB): .NETs Unicode character type is 16bit, whereas AppCore used
			// 32bit (UINT), so how do we handle this?

			//	TODO 1735(JohnT): handle surrogates! Currently we ignore them.
			if (char.GetUnicodeCategory(keyChar) == UnicodeCategory.Surrogate)
			{
				MessageBox.Show("DEBUG: Got a surrogate!");
				return;
			}

			if (Callbacks != null && Callbacks.EditedRootBox != null)
			{
				string stuInput;
				int cchBackspace;
				int cchDelForward;

				VwShiftStatus ss = GetShiftStatus(modifiers);
				CollectTypedInput(keyChar, out stuInput, out cchBackspace, out cchDelForward);

				string stUndo, stRedo;
				if (cchDelForward > 0 && cchBackspace <=0 && stuInput.Length == 0)
					ResourceHelper.MakeUndoRedoLabels("kstidUndoDelete", out stUndo, out stRedo);
				else
				{
					ResourceHelper.MakeUndoRedoLabels("kstidUndoTyping", out stUndo, out stRedo);
					if (keyChar == '\r' && stuInput.Length == 1)
					{
						stUndo = string.Format(stUndo, "CR");
						stRedo = string.Format(stRedo, "CR");
					}
					else
					{
						stUndo = string.Format(stUndo, stuInput);
						stRedo = string.Format(stRedo, stuInput);
					}
				}

				IVwRootBox rootb = Callbacks.EditedRootBox;
				using(new UndoTaskHelper(rootb.DataAccess, rootb.Site, stUndo, stRedo, false))
				{
					OnCharAux(keyChar, fCalledFromKeyDown, stuInput, cchBackspace, cchDelForward, ss,
						graphics, modifiers);
				}
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
		/// Allows subclass to be more selective about combining multiple keystrokes into one event
		/// </summary>
		public virtual bool KeepCollectingInput(int nextChr)
		{
			return true;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Collect whatever keyboard input is available--whatever the user has typed ahead.
		/// Includes backspaces and delete forwards, but not any more special keys like arrow keys.
		/// </summary>
		/// <param name="chsFirst">the first character the user typed, which started the whole
		/// process.</param>
		/// <param name="stuBuffer">buffer in which to put data characters.</param>
		/// <param name="cchBackspace">number of backspaces the user typed, in addition to any that
		/// just cancel stuff from the input buffer.</param>
		/// <param name="cchDelForward">any del forward keys the user typed</param>
		/// <remarks>
		/// <para>REVIEW JohnT: probably we should not accumulate both typed characters and
		/// sequences of extra Dels and Bs's, as it leads to ambiguity in character properties.</para>
		/// </remarks>
		/// -----------------------------------------------------------------------------------
		protected void CollectTypedInput(char chsFirst, out string stuBuffer,
			out int cchBackspace, out int cchDelForward)
		{
			cchBackspace = 0;
			cchDelForward = 0;
			StringBuilder stuTmpBuffer = new StringBuilder();
			// The first character goes into the buffer, unless it is a backspace or delete,
			// in which case it affects the counts.
			switch ((int)chsFirst)
			{
			case (int)VwSpecialChars.kscBackspace:
				cchBackspace++;
				break;
			case (int)VwSpecialChars.kscDelForward:
				cchDelForward++;
				break;
			default:
				stuTmpBuffer.Append(chsFirst);
				break;
			}

			// Collect any characters that are currently in the message queue
			// Note: When/if porting to MONO, the following block of code can be removed
			// and still work. However, make sure the final line in the method still remains
			// (i.e. the line where stuBuffer is being set).
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
					// it only works when both the down and up are translated. The worse that
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
							if (stuTmpBuffer.Length > 0)
								stuTmpBuffer.Remove(stuTmpBuffer.Length - 1, 1);
							else
								cchBackspace++;
							break;

						case (int)VwSpecialChars.kscDelForward:
							// let DEL keys be handled later by the views framework
							cchDelForward++;
							break;

						default:
							// regular characters get added to the buffer
							stuTmpBuffer.Append(nextChar);
							break;
					}
				}
				else
					break;
			}
			stuBuffer = stuTmpBuffer.ToString();

			// Shows that the buffering is working
			//			if (stuBuffer.Length > 1)
			//				Debug.WriteLine("typeahead : >" + stuBuffer + "< len = " + stuBuffer.Length);
		}

		/// <summary>
		/// Subclasses can override to allow more precise monitoring of text edits and resulting prop changes.
		/// See especially subclasses that use AnnotationAdjuster.
		/// </summary>
		public virtual bool MonitorTextEdits
		{
			get { return false; }
		}
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Handle typed character
		/// </summary>
		/// <param name="ch">Typed character</param>
		/// <param name="fCalledFromKeyDown">True if this method gets called from OnKeyDown</param>
		/// <param name="stuInput">input string</param>
		/// <param name="cchBackspace">number of backspace characters in stuInput</param>
		/// <param name="cchDelForward">number of delete characters in stuInput</param>
		/// <param name="ss">Status of Shift/Control/Alt key</param>
		/// <param name="graphics">graphics for processing input</param>
		/// <param name="modifiers">key modifiers - shift status, etc.</param>
		/// <remarks>I (EberhardB) added the parameter <paramref name="fCalledFromKeyDown"/>
		/// to be able to distinguish between Ctrl-Delete and Ctrl-Backspace.</remarks>
		/// -----------------------------------------------------------------------------------
		protected virtual void OnCharAux(char ch, bool fCalledFromKeyDown, string stuInput,
			int cchBackspace, int cchDelForward, VwShiftStatus ss,
			IVwGraphics graphics, Keys modifiers)
		{
			if ((int)ch == (int)VwSpecialChars.kscDelForward && cchBackspace == 0
				&& cchDelForward == 1 && stuInput.Length == 0)
			{
				// This may be a Ctrl-Backspace or Ctrl-Delete instead of a plain Delete.
				if (ss == VwShiftStatus.kfssControl)
				{
					if (fCalledFromKeyDown)
					{
						// We actually have a Ctrl-Delete, not a plain Delete.
						cchDelForward = -1;	// Signal delete forward one word.
					}
					else
					{
						// We actually have a Ctrl-Backspace that's been converted earlier to look
						// like a Delete.
						ch = '\b';
						cchBackspace = -1;		// Signal delete back one word.
						cchDelForward = 0;
					}
				}
				else if (ss != VwShiftStatus.kfssNone)
				{
					// REVIEW JohnT(SteveMc):Ignore Shift-Delete, Ctrl-Shift-Delete, etc.
					// What do they mean, anyway?
					// No, don't return here; we can get a combination of shift and backspace
					// from KeyMan.
					//			return;
				}
			}
			else if ((int)ch == (int)VwSpecialChars.kscBackspace && cchBackspace == 1
				&& cchDelForward == 0 && stuInput.Length == 0)
			{
				if (ss == VwShiftStatus.kfssControl)
				{
					// I don't think we can get here, but just in case...
					cchBackspace = -1;
				}
				else if (ss != VwShiftStatus.kfssNone)
				{
					// I don't know if we can get here, but just in case...
					// REVIEW JohnT(SteveMc): Ignore Shift-Backspace, Ctrl-Shift-Backspace, etc.
					// What do they mean, anyway?
					// No, don't return here; we can get a combination of shift and backspace
					// from KeyMan.
					//			return;
				}
			}
			string stUndo, stRedo;
			if (cchDelForward > 0 && cchBackspace <=0 && stuInput.Length == 0)
				ResourceHelper.MakeUndoRedoLabels("kstidUndoDelete", out stUndo, out stRedo);
			else
			{
				ResourceHelper.MakeUndoRedoLabels("kstidUndoTyping", out stUndo, out stRedo);
				stUndo = string.Format(stUndo, stuInput);
				stRedo = string.Format(stRedo, stuInput);
			}
			if (Callbacks != null && Callbacks.EditedRootBox != null)
			{
				IVwRootBox rootb = Callbacks.EditedRootBox;
				using (new DataUpdateMonitor(Control, EditedRootBox.DataAccess, rootb.Site, "Typing", MonitorTextEdits, false))
				using (new UndoTaskHelper(rootb.DataAccess, rootb.Site, stUndo, stRedo, false))
				{
					// Make the OnTyping event a virtual method for testing purposes.
					CallOnTyping(graphics, stuInput, cchBackspace, cchDelForward, ch, modifiers);
				}

				// It is possible that typing destroyed or changed the active rootbox, so we
				// better use the new one.
				rootb = Callbacks.EditedRootBox;
				rootb.Site.ScrollSelectionIntoView(rootb.Selection,
					VwScrollSelOpts.kssoDefault);
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Call the root box's OnTyping method. Virtual for testing purposes.
		/// </summary>
		/// <param name="vg"></param>
		/// <param name="str"></param>
		/// <param name="cchBackspace"></param>
		/// <param name="cchDelForward"></param>
		/// <param name="chFirst"></param>
		/// <param name="modifiers"></param>
		/// -----------------------------------------------------------------------------------
		protected virtual void CallOnTyping(IVwGraphics vg, string str, int cchBackspace,
			int cchDelForward, char chFirst, Keys modifiers)
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
				Callbacks.EditedRootBox.OnTyping(vg, str, cchBackspace, cchDelForward, chFirst,
					ref wsPending);
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
			if(Callbacks == null || Callbacks.EditedRootBox == null)
			{
				return false;
			}
			Callbacks.WsPending = -1; // using these keys suppresses prior input lang change.

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

			Control.Update();
			IVwRootBox rootb = Callbacks.EditedRootBox;
			rootb.Site.ScrollSelectionIntoView(rootb.Selection,
				VwScrollSelOpts.kssoDefault);
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
		/// Gets a value indicating whether the PropChanges during a paste should be delayed
		/// until the paste is complete.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual bool DelayPastePropChanges
		{
			get { return false; }
		}

		/// <summary>
		/// Get/Set the active input language.
		/// </summary>
		internal int ActiveLanguageId
		{
			get { return m_nActiveLangId; }
			set { m_nActiveLangId = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get/set test mode flag
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool InTestMode
		{
			get
			{
				CheckDisposed();
				return m_testMode;
			}
			set
			{
				CheckDisposed();
				m_testMode = value;
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
				if (m_viewSelection != null && m_viewSelection.Selection != null &&
					!m_viewSelection.Selection.IsValid)
				{
					m_viewSelection = null;
				}
				// Changing the selection to another cell in the same row of a browse view
				// doesn't always result in SelectionChanged() being called, or in the stored
				// selection becoming invalid.  So we check a little more closely here.
				// (See LT-3787.)
				if ((m_viewSelection == null ||
					(!InTestMode && m_viewSelection.Selection != RootBoxSelection)) &&
					Callbacks != null && Callbacks.EditedRootBox != null)
				{
					m_viewSelection = SelectionHelper.Create(Callbacks.EditedRootBox.Site);
				}
				return m_viewSelection;
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
				if(Callbacks == null)
				{
					return null;
				}
				IVwRootBox rootb = Callbacks.EditedRootBox;
				if (rootb != null)
					return rootb.DataAccess.WritingSystemFactory;
				else
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get/Set count indicating how many language selections are pending.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int SelectLangPending
		{
			get { CheckDisposed(); return m_cSelectLangPending; }
			set { CheckDisposed(); m_cSelectLangPending = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get/Set right to left flag.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool CanDoRtL
		{
			get { CheckDisposed(); return m_fCanDoRtl; }
			set { CheckDisposed(); m_fCanDoRtl = value; }
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
				//set the cursor shown in the current control
				if (m_defaultCursor != null)
					Control.Cursor = m_defaultCursor;
				else
					// get the cursor normally shown for editing when not over a hot link or picture
					Control.Cursor = GetCursor1(false, false, FwObjDataTypes.kodtContextString);
				//if (Callbacks.EditedRootBox != null)
				//	SetCursor(Point.Empty, Callbacks.EditedRootBox);
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the default CmFolder for storing (non-cataloged) pictures.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static string DefaultPictureFolder
		{
			get
			{
				return StringUtils.LocalPictures;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the caption props.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual ITsTextProps CaptionProps
		{
			get
			{
				throw new NotImplementedException();
			}
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

		#region Other methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If any edits have been performed, commit them. This typically involves parsing
		/// integers or dates, and closing an Undo record, not an actual save to the database.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Commit()
		{
			CheckDisposed();
			if (Callbacks != null && Callbacks.EditedRootBox != null)
			{
				IVwSelection vwsel = Callbacks.EditedRootBox.Selection;
				Commit(vwsel);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overload of Commit, useful if you already have a selection.
		/// </summary>
		/// <param name="vwsel"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool Commit(IVwSelection vwsel)
		{
			if (Callbacks == null || Callbacks.EditedRootBox == null)
				return false;
			return DataUpdateMonitor.Commit(vwsel, Callbacks.EditedRootBox.DataAccess);
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
					 sel = rootb.MakeSelAt(mousePos.X, mousePos.Y, rcSrcRoot, rcDstRoot,
						false);
				}
				catch
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
				Control.Cursor = GetCursor1(fInObject, fInPicture,
					(FwObjDataTypes)objDataType);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Displays the specified context menu for the specified rootsite at the specified
		/// mouse position. This will also determine whether or not to include the spelling
		/// correction options.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void ShowContextMenu(Point mousePos, ITMAdapter tmAdapter,
			SimpleRootSite rootsite, string contextMenuName, string addToDictMenuName,
			string insertBeforeMenuName, string changeMultipleMenuName, bool fShowSpellingOptions)
		{
			m_spellCheckStatus = SpellCheckStatus.Disabled;
			List<string> menuItemNames = null;

			if (fShowSpellingOptions)
			{
				Rectangle rcSrcRoot, rcDstRoot;
				rootsite.GetCoordRects(out rcSrcRoot, out rcDstRoot);
				bool fWordInDictionary;

				menuItemNames = MakeSpellCheckMenuOptions(mousePos, Callbacks.EditedRootBox,
					rcSrcRoot, rcDstRoot, tmAdapter, contextMenuName, addToDictMenuName,
					insertBeforeMenuName, changeMultipleMenuName, out fWordInDictionary);

				m_spellCheckStatus = (menuItemNames == null ?
					SpellCheckStatus.WordInDictionary : SpellCheckStatus.Enabled);
			}

			Point pt = rootsite.PointToScreen(mousePos);
			tmAdapter.PopupMenu(contextMenuName, pt.X, pt.Y, menuItemNames);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating what the status of the spell checking system is for the
		/// sake of the the context menu that's being shown in the ShowContextMenu method.
		/// This property is only valid when the context menu popped-up in that method is
		/// in the process of being shown. This property is used for the SimpleRootSites who
		/// need to handle the update messages for those spelling options.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public SpellCheckStatus SpellCheckingStatus
		{
			get { return m_spellCheckStatus; }
		}

		/// <summary>
		/// Determine the default font to use for the specified writing system,
		/// displayed in the default Normal style of the specified stylesheet.
		/// Currently duplicated from Widgets.FontHeightAdjuster. Grrr.
		/// </summary>
		/// <param name="hvoWs"></param>
		/// <param name="styleSheet"></param>
		/// <param name="wsf"></param>
		/// <returns></returns>
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
			IWritingSystem ws = wsf.get_EngineOrNull(hvoWs);
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

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// If the mousePos is part of a word that is not properly spelled, add to the menu
		/// options for correcting it.
		/// </summary>
		/// <param name="mousePos">The location of the mouse</param>
		/// <param name="rootb">The rootbox</param>
		/// <param name="rcSrcRoot"></param>
		/// <param name="rcDstRoot"></param>
		/// <param name="menu">to add items to.</param>
		/// <returns>the number of menu items added (not counting a possible separator line)</returns>
		/// -----------------------------------------------------------------------------------
		public virtual int MakeSpellCheckMenuOptions(Point mousePos, IVwRootBox rootb, Rectangle rcSrcRoot, Rectangle rcDstRoot,
			ContextMenuStrip menu)
		{
			CheckDisposed();
			int hvoObj, tag, wsAlt, wsText;
			string word;
			Dictionary dict;
			bool nonSpellingError;
			ICollection<SpellCorrectMenuItem> suggestions = GetSuggestions(mousePos, rootb, rcSrcRoot, rcDstRoot,
				out hvoObj, out tag, out wsAlt, out wsText, out word, out dict, out nonSpellingError);
			if (suggestions == null)
				return 0; // no detectable spelling problem.

			// Note that items are inserted in order starting at the beginning, rather than
			// added to the end.  This is to support TE-6901.
			// If the menu isn't empty, add a separator.
			if (menu.Items.Count > 0)
				menu.Items.Insert(0, new ToolStripSeparator());

			// Make the menu option.
			ToolStripMenuItem itemExtras = null;
			int count = 0;
			int index = 0;
			foreach (SpellCorrectMenuItem subItem in suggestions)
			{
				subItem.Click += new EventHandler(subItem_Click);
				if (count++ < kMaxSpellingSuggestionsInRootMenu)
				{
					Font font = subItem.Font;
					if (wsText != 0)
					{
						font = GetFontForNormalStyle(wsText, rootb.Stylesheet, rootb.DataAccess.WritingSystemFactory);
						//string familyName = rootb.DataAccess.WritingSystemFactory.get_EngineOrNull(wsText).DefaultBodyFont;
						//font = new Font(familyName, font.Size, FontStyle.Bold);
					}

					subItem.Font = new Font(font, FontStyle.Bold);

					menu.Items.Insert(index++, subItem);
				}
				else
				{
					if (itemExtras == null)
					{
						itemExtras = new ToolStripMenuItem(SimpleRootSiteStrings.ksAdditionalSuggestions);
						menu.Items.Insert(index++, itemExtras);
					}
					itemExtras.DropDownItems.Add(subItem);
				}
			}
			if (suggestions.Count == 0)
			{
				ToolStripMenuItem noSuggestItems = new ToolStripMenuItem(SimpleRootSiteStrings.ksNoSuggestions);
				menu.Items.Insert(index++, noSuggestItems);
				noSuggestItems.Enabled = false;
			}
			ToolStripMenuItem itemAdd = new AddToDictMenuItem(dict, word, rootb.DataAccess,
				hvoObj, tag, wsAlt, wsText, SimpleRootSiteStrings.ksAddToDictionary, this);
			if (nonSpellingError)
				itemAdd.Enabled = false;
			menu.Items.Insert(index++, itemAdd);
			itemAdd.Image = SIL.FieldWorks.Resources.ResourceHelper.SpellingIcon;
			itemAdd.Click += new EventHandler(itemAdd_Click);
			return index;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make spell checking menu options using the DotNetBar adapter.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public List<string> MakeSpellCheckMenuOptions(Point mousePos, IVwRootBox rootb,
			Rectangle rcSrcRoot, Rectangle rcDstRoot, ITMAdapter tmAdapter, string menuName,
			string addToDictMenuName, string changeMultipleMenuName, string insertBeforeMenuName, out bool wordInDictionary)
		{
			wordInDictionary = true;

			int hvoObj, tag, wsAlt, wsText;
			string word;
			Dictionary dict;
			bool nonSpellingError;
			ICollection<SpellCorrectMenuItem> suggestions = GetSuggestions(mousePos, rootb, rcSrcRoot,
				rcDstRoot, out hvoObj, out tag, out wsAlt, out wsText, out word, out dict, out nonSpellingError);

			// These two menu items are disabled for non-spelling errors. In addition, for addToDict, we need
			// to set the tag to an AddToDictMenuItem which can actually do the work.
			UpdateItemProps(tmAdapter, addToDictMenuName, nonSpellingError, new AddToDictMenuItem(dict, word, rootb.DataAccess,
				hvoObj, tag, wsAlt, wsText, SimpleRootSiteStrings.ksAddToDictionary, this));
			// any non-null value of tag will indicate the item should be enabled, tested in TeMainWnd.UpdateSpellingMenus.
			UpdateItemProps(tmAdapter, changeMultipleMenuName, nonSpellingError, "ok to change");

			if (suggestions == null)
				return null;

			wordInDictionary = false;

			// Make the menu options.
			List<string> menuItemNames = new List<string>();
			TMItemProperties itemProps;
			if (suggestions.Count == 0)
			{
				itemProps = new TMItemProperties();
				itemProps.Name = "noSpellingSuggestion";
				itemProps.Text = SimpleRootSiteStrings.ksNoSuggestions;
				itemProps.Enabled = false;
				menuItemNames.Add(itemProps.Name);
				tmAdapter.AddContextMenuItem(itemProps, menuName, insertBeforeMenuName);
			}

			int count = 0;
			string additionalSuggestionsMenuName = "additionalSpellSuggestion";

			foreach (SpellCorrectMenuItem scmi in suggestions)
			{

				itemProps = new TMItemProperties();
				itemProps.Name = "spellSuggestion" + scmi.Text;
				itemProps.Text = scmi.Text;
				itemProps.Message = "SpellingSuggestionChosen";
				itemProps.CommandId = "CmdSpellingSuggestionChosen";
				itemProps.Tag = scmi;
				itemProps.Font = (wsText == 0) ? null : GetFontForNormalStyle(wsText,
					rootb.Stylesheet, rootb.DataAccess.WritingSystemFactory);

				if (count == kMaxSpellingSuggestionsInRootMenu)
				{
					TMItemProperties tmpItemProps = new TMItemProperties();
					tmpItemProps.Name = additionalSuggestionsMenuName;
					tmpItemProps.Text = SimpleRootSiteStrings.ksAdditionalSuggestions;
					menuItemNames.Add(tmpItemProps.Name);
					tmAdapter.AddContextMenuItem(tmpItemProps, menuName, insertBeforeMenuName);
					insertBeforeMenuName = null;
				}

				if (insertBeforeMenuName != null)
				{
					menuItemNames.Add(itemProps.Name);
					tmAdapter.AddContextMenuItem(itemProps, menuName, insertBeforeMenuName);
				}
				else
				{
					tmAdapter.AddContextMenuItem(itemProps, menuName,
						additionalSuggestionsMenuName, null);
				}

				count++;
			}

			return menuItemNames;
		}

		void UpdateItemProps(ITMAdapter tmAdapter, string menuName, bool nonSpellingError, object tag)
		{
			TMItemProperties itemProps = tmAdapter.GetItemProperties(menuName);
			if (itemProps != null)
			{
				if (nonSpellingError)
				{
					itemProps.Tag = null; // disable
				}
				else
				{
					itemProps.Tag = tag;
				}
				itemProps.Update = true;
				tmAdapter.SetItemProperties(menuName, itemProps);
			}
		}

		// This is part of an alternate strategy for spell checking that allows spell checking options to be added
		// to an xCore-managed menu. It was developed to the point of a submenu, but not enhanced to allow the
		// top seven suggestions to be at the top level. If we want to do that, I think we'll need to enhance the
		// colleague with specific methods to enable and change the text of seven distinct menu items (which will
		// need to be defined in the XML), like CmdFirstSpellSuggestion, CmdSecondSpellSuggestion, etc.
		// For example, something like this in main.xml, at the start of mnuObjectChoices, works with the current code
		// (see e.g., call to MakeSpellCheckColleague commented out in SandboxBase).
		//		<menu label="Correct Spelling" id="CorrectSpelling" list="PossibleCorrections" behavior="command" message="CorrectSpelling" defaultVisible="false"/>
		//		<item command="CmdAddToSpellDict" defaultVisible="false"/>
		// The enhaned version might look more like this:
		//		<item command="CmdFirstSuggestion" defaultVisible="false"/>
		//		<item command="CmdSecondSuggestion" defaultVisible="false"/>
		//		<menu label="Other Suggestions" id="OtherSuggestions" list="OtherPossibleCorrections" behavior="command" message="OtherSuggestions" defaultVisible="false"/>
		//		<item command="CmdAddToSpellDict" defaultVisible="false"/>
		// Also need to define the commands, e.g.,
		//		<command id="CmdCorrectSpelling" label="Correct Spelling" message="CorrectSpelling"/>
		//		<command id="CmdAddToSpellDict" label="Add to Spelling Dictionary" message="AddToSpellDict"/>
		///// -----------------------------------------------------------------------------------
		///// <summary>
		///// If the mousePos is part of a word that is not properly spelled, make a colleague
		///// which can handle the menu options for correcting it or adding it to the dictionary.
		///// Otherwise, answer null.
		///// </summary>
		///// <param name="mousePos">The location of the mouse</param>
		///// <param name="rootb">The rootbox</param>
		///// <param name="rcSrcRoot"></param>
		///// <param name="rcDstRoot"></param>
		///// -----------------------------------------------------------------------------------
		//public IxCoreColleague MakeSpellCheckColleague(Point mousePos, IVwRootBox rootb,
		//    Rectangle rcSrcRoot, Rectangle rcDstRoot)
		//{
		//    CheckDisposed();
		//    int ichMin, ichLim, hvoObj, tag, wsAlt, wsText;
		//    string word;
		//    Dictionary dict;
		//    ICollection<string> suggestions = GetSuggestions(mousePos, rootb, rcSrcRoot, rcDstRoot,
		//        out hvoObj, out tag, out wsAlt, out wsText, out ichMin, out ichLim, out word, out dict);
		//    if (suggestions == null)
		//        return null;
		//    return new SpellCorrectColleague(rootb, suggestions, hvoObj, tag, wsAlt, wsText, ichMin, ichLim, word, dict, this);
		//

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a list of suggested corrections if the selection is a spelling or similar error.
		/// Returns null if there is no problem at the selection location.
		/// Note that it may also return an empty list; this has a distinct meaning, namely,
		/// that there IS a problem, but we have no useful suggestions for what to change it to.
		/// nonSpellingError is set true when the error is not simply a mis-spelled word in a
		/// single writing system; currently this should disable or hide the commands to add
		/// the word to the dictionary or change multiple occurrences.
		/// The input arguments indicate where the user clicked and allow us to find the
		/// text he might be trying to correct. The other output arguments indicate which WS
		/// (wasAlt -- 0 for simple string) of which property (tag) of which object (hvoObj)
		/// is affected by the change, the ws of the mis-spelled word, and the corresponding
		/// Enchant dictionary. Much of this information is already known to the
		/// SpellCorrectMenuItems returned, but some clients use it in creating other menu options.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ICollection<SpellCorrectMenuItem> GetSuggestions(Point mousePos, IVwRootBox rootb,
			Rectangle rcSrcRoot, Rectangle rcDstRoot, out int hvoObj, out int tag,
			out int wsAlt, out int wsText, out string word, out Dictionary dict, out bool nonSpellingError)
		{
			hvoObj = tag = wsAlt = wsText = 0; // make compiler happy for early returns
			word = null;
			dict = null;
			nonSpellingError = true;

			if (rootb == null)
				return null;

			// Get a selection at the indicated point.
			IVwSelection sel = null;
			try
			{
				sel = rootb.MakeSelAt(mousePos.X, mousePos.Y, rcSrcRoot, rcDstRoot, false);
			}
			catch
			{
				// Ignore errors
			}
			if (sel == null)
				return null;

			// Get the selected word and verify that it is a single run within a single
			// editable string.
			sel = sel.GrowToWord();
			if (sel == null || !sel.IsRange || sel.SelType != VwSelType.kstText || !SelectionHelper.IsEditable(sel))
				return null;
			ITsString tss;
			bool fAssocPrev;
			int ichAnchor;
			sel.TextSelInfo(false, out tss, out ichAnchor, out fAssocPrev, out hvoObj, out tag, out wsAlt);
			int ichEnd, hvoObjE, tagE, wsE;
			sel.TextSelInfo(true, out tss, out ichEnd, out fAssocPrev, out hvoObjE, out tagE, out wsE);
			if (hvoObj != hvoObjE || tag != tagE || wsAlt != wsE)
				return null;

			int ichMin = Math.Min(ichEnd, ichAnchor);
			int ichLim = Math.Max(ichEnd, ichAnchor);

			// May need to enlarge the word beyond what GrowToWord does, if there is adjacent wordforming material.
			int ichMinAdjust = AdjustWordBoundary(tss, ichMin, -1, 0) + 1; // further expanded start of word.
			int ichLimAdjust = AdjustWordBoundary(tss, ichLim - 1, 1, tss.Length); // further expanded lim of word.
			// From the ends we can strip stuff with different spell-checking properties.
			IVwStylesheet styles = Callbacks.EditedRootBox.Stylesheet;
			int spellProps = SpellCheckProps(tss, ichMin, styles);
			for (; ichMinAdjust < ichMin && SpellCheckProps(tss, ichMinAdjust, styles) != spellProps; ichMinAdjust++)
				;
			for (; ichLimAdjust > ichLim && SpellCheckProps(tss, ichLimAdjust - 1, styles) != spellProps; ichLimAdjust--)
				;
			ichMin = ichMinAdjust;
			ichLim = ichLimAdjust;

			ITsStrFactory tsf = TsStrFactoryClass.Create();

			// Now we have the specific range we will check. Get the actual string.
			ITsStrBldr bldr = tss.GetBldr();
			if (ichLim < bldr.Length)
				bldr.ReplaceTsString(ichLim, bldr.Length, null);
			if (ichMin > 0)
				bldr.ReplaceTsString(0, ichMin, null);
			ITsString tssWord = bldr.GetString();

			// See whether we need the special blue underline, which is used mainly for adjacent words in different writing systems.
			List<int> wss = StringUtils.GetWritingSystems(tssWord);
			if (wss.Count > 1)
				return MakeWssSuggestions(tssWord, wss, rootb.DataAccess, hvoObj, tag, wsAlt, ichMin, ichLim);
			ITsString keepOrcs; // holds any ORCs we found in the original word that we need to keep rather than reporting.
			IList<SpellCorrectMenuItem> result = MakeEmbeddedNscSuggestion(ref tssWord, styles, rootb.DataAccess,
				hvoObj, tag, wsAlt, ichMin, ichLim, out keepOrcs);
			if (result.Count > 0)
				return result;

			// Determine whether it is a spelling problem.
			wsText = StringUtils.GetWsOfRun(tssWord, 0);
			dict = GetDictionary(wsText);
			if (dict == null)
				return null;
			word = tssWord.get_NormalizedForm(FwNormalizationMode.knmNFC).Text;
			if (word == null)
				return null; // don't think this can happen, but...
			if (dict.Check(word))
				return null; // not mis-spelled.

			// Get suggestions. Make sure to return an empty collection rather than null, even if no suggestions,
			// to indicate an error.
			ICollection<string> suggestions = dict.Suggest(word);
			foreach (string suggest in suggestions)
			{
				ITsString replacement = tsf.MakeStringRgch(suggest, suggest.Length, wsText);
				if (keepOrcs != null)
				{
					ITsStrBldr bldrRep = keepOrcs.GetBldr();
					bldrRep.ReplaceTsString(0, 0, replacement);
					replacement = bldrRep.GetString();
				}
				result.Add(new SpellCorrectMenuItem(rootb.DataAccess, hvoObj, tag, wsAlt, ichMin, ichLim, suggest,
					replacement));
			}
			nonSpellingError = false; // it IS a spelling problem.
			return result;
		}

		/// <summary>
		/// Make 'suggestion' menu items for the case where the 'word' contains embedded stuff that we don't spell-check
		/// (typically CV numbers) or ORCs. The current suggestion inserts spaces adjacent to the embedded stuff.
		/// If possible, we show what the result will look like; this isn't possible for orcs, so we just display "insert missing spaces".
		/// Enhance JohnT: we want two more menu items, one offering to move all the problem stuff to the start, one to the end.
		/// This is tricky to word; we will need an override that subclasses implement to provide a user-friendly description
		/// of a caller.
		/// There is a special case for certain ORCs which are not visible (picture anchors, basically). These should not
		/// affect the menu options offered, but they must not be replaced by a new spelling. If these are found, we update
		/// tssWord to something that does not contain them, and retain the orcs to append to any substitutions (returned in tssKeepOrcs).
		/// </summary>
		private IList<SpellCorrectMenuItem> MakeEmbeddedNscSuggestion(ref ITsString tssWord, IVwStylesheet styles, ISilDataAccess iSilDataAccess,
			int hvoObj, int tag, int wsAlt, int ichMin, int ichLim, out ITsString tssKeepOrcs)
		{
			List<SpellCorrectMenuItem> result = new List<SpellCorrectMenuItem>();
			// Make an item with inserted spaces.
			ITsStrBldr bldr = tssWord.GetBldr();
			int spCur = SpellCheckProps(tssWord, 0, styles);
			int offset = 0;
			bool foundDiff = false;
			bool fHasOrc = false;
			ITsStrBldr bldrWord = null;
			ITsStrBldr bldrKeepOrcs = null;
			int bldrWordOffset = 0;
			// Start at 0 even though we already got its props, because it just might be an ORC.
			for (int ich = 0; ich < tssWord.Length; ich++)
			{
				if (tssWord.GetChars(ich, ich + 1) == "\xfffc")
				{
					ITsTextProps ttp = tssWord.get_PropertiesAt(ich);
					string objData = ttp.GetStrPropValue((int)FwTextPropType.ktptObjData);
					if (objData.Length == 0 || objData[0] != Convert.ToChar((int)FwObjDataTypes.kodtGuidMoveableObjDisp))
					{
						fHasOrc = true;
						int ichInsert = ich + offset;
						bldr.Replace(ichInsert, ichInsert, " ", null);
						spCur = -50 - ich; // Same trick as SpellCheckProps to ensure won't match anything following.
						offset++;
						foundDiff = true;
					}
					else
					{
						// An ORC we want to ignore, but not lose. We will strip it out of the word we will
						// actually spell-check if we don't find other ORC problems, but save it to be
						// inserted at the end of any correction word. We might still use
						// our own "insert missing spaces" option, too, if we find another ORC of a different type.
						// In that case, this ORC just stays as part of the string, without spaces inserted.
						if (bldrWord == null)
						{
							bldrWord = tssWord.GetBldr();
							bldrKeepOrcs = TsStrBldrClass.Create();
						}
						bldrWord.Replace(ich - bldrWordOffset, ich - bldrWordOffset + 1, "", null);
						bldrKeepOrcs.Replace(bldrKeepOrcs.Length, bldrKeepOrcs.Length, "\xfffc", ttp);
						bldrWordOffset++;
					}
				}
				else // not an orc, see if props changed.
				{
					int spNew = SpellCheckProps(tssWord, ich, styles);
					if (spNew != spCur)
					{
						int ichInsert = ich + offset;
						bldr.Replace(ichInsert, ichInsert, " ", null);
						spCur = spNew;
						offset++;
						foundDiff = true;
					}
				}
			}
			if (bldrWord != null)
			{
				tssWord = bldrWord.GetString();
				tssKeepOrcs = bldrKeepOrcs.GetString();
			}
			else
			{
				tssKeepOrcs = null;
			}
			if (!foundDiff)
				return result;
			ITsString suggest = bldr.GetString();
			// There might still be an ORC in the string, in the pathological case of a picture anchor and embedded verse number
			// in the same word(!). Leave it in the replacement, but not in the menu item.
			string menuItemText = suggest.Text.Replace("\xfffc", "");
			if (fHasOrc)
				menuItemText = SimpleRootSiteStrings.ksInsertMissingSpaces;
			result.Add(new SpellCorrectMenuItem(iSilDataAccess, hvoObj, tag, wsAlt, ichMin, ichLim, menuItemText, suggest));
			return result;
		}

		private ICollection<SpellCorrectMenuItem> MakeWssSuggestions(ITsString tssWord, List<int> wss, ISilDataAccess iSilDataAccess,
			int hvoObj, int tag, int wsAlt, int ichMin, int ichLim)
		{
			List<SpellCorrectMenuItem> result = new List<SpellCorrectMenuItem>(wss.Count + 1);

			// Make an item with inserted spaces.
			ITsStrBldr bldr = tssWord.GetBldr();
			int wsFirst = StringUtils.GetWsOfRun(tssWord, 0);
			int offset = 0;
			for (int irun = 1; irun < tssWord.RunCount; irun++)
			{
				int wsNew = StringUtils.GetWsOfRun(tssWord, irun);
				if (wsNew != wsFirst)
				{
					int ichInsert = tssWord.get_MinOfRun(irun) + offset;
					bldr.Replace(ichInsert, ichInsert, " ", null);
					wsFirst = wsNew;
					offset++;
				}
			}
			ITsString suggest = bldr.GetString();
			string menuItemText = suggest.Text;
			result.Add(new SpellCorrectMenuItem(iSilDataAccess, hvoObj, tag, wsAlt, ichMin, ichLim, menuItemText, suggest));

			// And items for each writing system.
			foreach (int ws in wss)
			{
				bldr = tssWord.GetBldr();
				bldr.SetIntPropValues(0, bldr.Length, (int) FwTextPropType.ktptWs, (int) FwTextPropVar.ktpvDefault, ws);
				suggest = bldr.GetString();
				ILgWritingSystemFactory wsf = iSilDataAccess.WritingSystemFactory;
				IWritingSystem engine = wsf.get_EngineOrNull(ws);
				string wsName = engine.get_Name(wsf.UserWs);
				string itemText = string.Format(SimpleRootSiteStrings.ksMlStringIsMono, tssWord.Text, wsName);
				result.Add(new SpellCorrectMenuItem(iSilDataAccess, hvoObj, tag, wsAlt, ichMin, ichLim, itemText, suggest));
			}

			return result;
		}

		/// <summary>
		/// Answer the spelling status of the indicated character in the string, unless it is an ORC,
		/// in which case, for each ORC we answer a different value (that is not any of the valid spelling statuses).
		/// Enhance JohnT: we don't want to consider embedded-picture ORCs to count as different; we may
		/// strip them out before we start checking the word.
		/// </summary>
		int SpellCheckProps(ITsString tss, int ich, IVwStylesheet styles)
		{
			// For our purposes here, ORC (0xfffc) is considered to have a different spelling status from everything else,
			// even from every other ORC in the string. This means we always offer to insert spaces adjacent to them.
			if (ich < tss.Length && tss.GetChars(ich, ich + 1)[0] == 0xfffc)
			{
				return -50 - ich;
			}
			ITsTextProps props = tss.get_PropertiesAt(ich);
			string style = props.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
			int var, val;
			if (styles != null && !string.IsNullOrEmpty(style))
			{
				ITsTextProps styleProps = styles.GetStyleRgch(style.Length, style);
				val = styleProps.GetIntPropValues((int)FwTextPropType.ktptSpellCheck, out var);
				if (var != -1)
					return val; // style overrides
			}
			val = props.GetIntPropValues((int)FwTextPropType.ktptSpellCheck, out var);
			if (var == -1)
				return 0; // treat unspecified the same as default.
			else
				return val;
		}

		bool BeyondLim(int ich, int delta, int lim)
		{
			if (delta < 0)
				return ich < lim;
			else
				return ich >= lim;
		}

		/// <summary>
		/// Given a start character position that is within a word, and an delta that is +/- 1,
		/// return the index of the first non-wordforming (and non-number) character in that direction,
		/// or -1 if the start of the string is reached, or string.Length if the end is reached.
		/// For our purposes here, ORC (0xfffc) is considered word-forming.
		/// </summary>
		int AdjustWordBoundary(ITsString tss, int ichStart, int delta, int lim)
		{
			string text = tss.Text;
			int ich;
			for(ich = ichStart + delta; !BeyondLim(ich, delta, lim); ich += delta)
			{
				ILgCharacterPropertyEngine cpe = StringUtils.GetCharPropEngineAtOffset(tss,
					Callbacks.EditedRootBox.DataAccess.WritingSystemFactory, ich);
				char ch = text[ich];
				if (!cpe.get_IsWordForming(ch) && !cpe.get_IsNumber(ch) && ch != 0xfffc)
					break;
			}
			return ich;
		}

		void itemAdd_Click(object sender, EventArgs e)
		{
			(sender as AddToDictMenuItem).AddWordToDictionary();
		}

		void subItem_Click(object sender, EventArgs e)
		{
			SpellCorrectMenuItem item = sender as SpellCorrectMenuItem;
			Debug.Assert(item != null, "invalid sender of spell check item");
			item.DoIt();
		}

		/// <summary>
		/// Return the enchant dictionary which should be used for the specified writing system.
		/// </summary>
		/// <param name="ws"></param>
		/// <returns></returns>
		public Dictionary GetDictionary(int ws)
		{
			return EnchantHelper.GetDictionary(ws, WritingSystemFactory);
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
		/// Review TE Team(JohnT): why is this method static and public? It is only defined
		/// and used from non-static methods of EditingHelper, and we could avoid the
		/// GetCursor1 mess if it was non-static.
		/// </summary>
		/// <param name="fInObject"><c>True</c> if the mouse pointer is over an object</param>
		/// <param name="fInPicture">True if mouse is over a picture (or icon)</param>
		/// <param name="objDataType">The type of the object the mouse pointer is over</param>
		/// ------------------------------------------------------------------------------------
		public static Cursor GetCursor(bool fInObject, bool fInPicture,
			FwObjDataTypes objDataType)
		{
			if (fInPicture)
				return Cursors.Arrow;

			if (fInObject && (objDataType == FwObjDataTypes.kodtNameGuidHot
				|| objDataType == FwObjDataTypes.kodtExternalPathName
				|| objDataType == FwObjDataTypes.kodtOwnNameGuidHot))
				return Cursors.Hand;
			else
				return Cursors.IBeam;
		}

		/// <summary>
		/// non-static version of GetCursor can use the IBeamCursor method of the Control,
		/// if it is a simple root site (or subclass).
		/// </summary>
		/// <param name="fInObject"></param>
		/// <param name="fInPicture"></param>
		/// <param name="objDataType"></param>
		/// <returns></returns>
		internal Cursor GetCursor1(bool fInObject, bool fInPicture,
			FwObjDataTypes objDataType)
		{
			Cursor result = GetCursor(fInObject, fInPicture, objDataType);
			if (result == Cursors.IBeam && Control is SimpleRootSite)
				return (Control as SimpleRootSite).IBeamCursor;
			return result;
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
					styleName = StStyle.NormalStyleName;
				}

				if (ittp > 0 && prevStyleName != styleName)
					return null;

				prevStyleName = styleName;
			}

			return styleName;
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
				return true; // Finished handling the command, anyway.

			if (flidParaOwner == (int)CmPicture.CmPictureTags.kflidCaption)
			{
				vqttp = new ITsTextProps[] { CaptionProps };
				return true;
			}

			vqttp = new ITsTextProps[ihvoLast - ihvoFirst + 1];
			int index = 0;
			for (int ihvo = ihvoFirst; ihvo <= ihvoLast; ihvo++)
			{
				int hvoPara = sda.get_VecItem(hvoText, flidParaOwner, ihvo);
				ITsTextProps ttp = sda.get_UnknownProp(hvoPara, (int)StPara.StParaTags.kflidStyleRules)
					as ITsTextProps;
				vqttp[index] = ttp;
				index++;
			}
			return true;
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
			using (ArrayPtr ptrHard = MarshalEx.ArrayToNative(cttp, typeof(ITsTextProps)))
			{
				using (ArrayPtr ptrSoft = MarshalEx.ArrayToNative(cttp, typeof(IVwPropertyStore)))
				{
					vwsel.GetHardAndSoftParaProps(cttp, vttp, ptrHard, ptrSoft, out cttp);
					vttpHard = (ITsTextProps[])MarshalEx.NativeToArray(ptrHard, cttp, typeof(ITsTextProps));
					vvpsSoft = (IVwPropertyStore[])MarshalEx.NativeToArray(ptrSoft, cttp,
						typeof(IVwPropertyStore));
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

			// Commit any outstanding edits.
			if (!Commit(vwsel))
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
			for (int lev = 0; lev < clev; lev++)
			{
				// At this point, we know how to do this command only for structured text paragraphs.
				vwsel.PropInfo(fEnd, lev, out hvoText, out tagText, out ihvo, out cpropPrevious,
					out vps);

				// Make sure it's the right property.
				if (tagText == (int)StText.StTextTags.kflidParagraphs ||
					tagText == (int)CmPicture.CmPictureTags.kflidCaption)
				{
					break;
				}
			}

			if (tagText != (int)StText.StTextTags.kflidParagraphs &&
				tagText != (int)CmPicture.CmPictureTags.kflidCaption)
			{
				return false;
			}

			// And nothing bizarre about other values...
			if (cpropPrevious > 0)
				return false;

			return true;
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

			// Create a new undo-task corresponding to the applying of the style.
			string sUndo, sRedo;
			ResourceHelper.MakeUndoRedoLabels("kstidUndoApplyStyle", out sUndo, out sRedo);
			using (new UndoTaskHelper(Callbacks.EditedRootBox.DataAccess,
				Callbacks.EditedRootBox.Site, sUndo, sRedo, true))
			{
				StyleType stType;
				if (sStyleToApply == null || sStyleToApply.Length == 0)
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

			// Narrow the range of TsTextProps to only include those that are not NULL.
			int ihvoFirstMod;
			int ihvoLastMod;
			NarrowRangeOfTsTxtProps(hvoText, tagText, vttp, vvpsSoft, true, ihvoFirst,
				ihvoLast, out ihvoFirstMod, out ihvoLastMod);

			if (ihvoFirstMod < 0)
				return true; // There are no paragraph properties changes.

			ForceRedrawByFakingPropChanged(vwsel, hvoText, tagText, ihvoFirstMod,
				ihvoLastMod);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Remove character formatting after beginning an undo task.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void RemoveCharFormattingWithUndo()
		{
			CheckDisposed();
			RemoveCharFormattingWithUndo(false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Remove character formatting after beginning an undo task.
		/// </summary>
		/// <param name="removeAllStyles">if true, all styles in selection will be removed</param>
		/// ------------------------------------------------------------------------------------
		public void RemoveCharFormattingWithUndo(bool removeAllStyles)
		{
			CheckDisposed();
			IVwSelection vwsel;
			ITsTextProps[] vttp;
			IVwPropertyStore[] vvps;

			if (!GetCharacterProps(out vwsel, out vttp, out vvps))
				return;

			if(Callbacks == null || Callbacks.EditedRootBox == null)
			{
				return;
			}
			using(new UndoTaskHelper(Callbacks.EditedRootBox.Site,
				"kstidUndoStyleChanges", false))
			{
				RemoveCharFormatting(vwsel, ref vttp, null, removeAllStyles);
			}
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
			int	cActualProps = 0;
			Debug.Assert(vttp != null, "This shouldn't happen. Please look at TE-6499.");
			if (vttp == null)
				return;

			int cttp = vttp.Length;

			for (int ittp = 0; ittp < cttp; ittp++)
			{
				if (vwsel.IsRange)
				{
					string objData = vttp[ittp].GetStrPropValue((int)FwTextPropType.ktptObjData);
					if (objData != null)
					{
						// We don't want to clear most object data, because it has the effect of making
						// ORCs unuseable. A special case is external links, which are applied to regular
						// characters, and annoying not to be able to remove.
						if (objData.Length == 0 ||
							objData[0] != Convert.ToChar((int)FwObjDataTypes.kodtExternalPathName))
						{
							continue; // skip this run.
						}
					}
				}

				// Skip user prompt strings.  A user prompt string will (hopefully) have a
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

				// count this property
				cActualProps++;

				// Create an empty builder.
				ITsPropsBldr tpb = vttp[ittp].GetBldr();

				tpb.SetStrPropValue((int)FwTextPropType.ktptFontFamily, null);
				tpb.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, sStyle);
				tpb.SetIntPropValues((int)FwTextPropType.ktptItalic, -1, -1);
				tpb.SetIntPropValues((int)FwTextPropType.ktptBold, -1, -1);
				tpb.SetIntPropValues((int)FwTextPropType.ktptSuperscript, -1, -1);
				tpb.SetIntPropValues((int)FwTextPropType.ktptUnderline, -1, -1);
				tpb.SetIntPropValues((int)FwTextPropType.ktptFontSize, -1, -1);
				tpb.SetIntPropValues((int)FwTextPropType.ktptOffset, -1, -1);
				tpb.SetIntPropValues((int)FwTextPropType.ktptForeColor, -1, -1);
				tpb.SetIntPropValues((int)FwTextPropType.ktptBackColor, -1, -1);
				tpb.SetIntPropValues((int)FwTextPropType.ktptUnderColor, -1, -1);
				tpb.SetStrPropValue((int)FwTextPropType.ktptFontVariations, null);
				tpb.SetStrPropValue((int)FwTextPropType.ktptObjData, null);

				// Update the selection.
				ITsTextProps ttp = tpb.GetTextProps();
				vttp[ittp] = ttp;
			}

			// Assume some change was made.

			if (cActualProps > 0)
			{
				// setting the selection props might cause our selection to get destroyed because
				// it might recreate paragraph boxes. Therefore we remember our current
				// selection and afterwards try to restore it again.
				SelectionHelper helper = null;
				if (Callbacks != null) // might be null when running tests
					helper = SelectionHelper.Create(vwsel, Callbacks.EditedRootBox.Site);
				vwsel.SetSelectionProps(cttp, vttp);
				if (!vwsel.IsValid && helper != null)
					helper.RestoreSelectionAndScrollPos();
			}

			if (Callbacks != null)
				Callbacks.WsPending = -1;
			m_viewSelection = null;
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
						tpb.SetStrPropValue((int)FwTextPropType.ktptNamedStyle,
							StStyle.NormalStyleName);
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
					sda.SetUnknown(hvoPara, (int)StPara.StParaTags.kflidStyleRules, ttp);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Force a redraw by faking a property change.
		/// </summary>
		/// <param name="vwsel">The selection</param>
		/// <param name="hvoText">HVO of the text</param>
		/// <param name="tagText">tag of the text</param>
		/// <param name="ihvoFirst">Index of first textprop</param>
		/// <param name="ihvoLast">Index of last textprop</param>
		/// ------------------------------------------------------------------------------------
		private void ForceRedrawByFakingPropChanged(IVwSelection vwsel, int hvoText, int tagText,
			int ihvoFirst, int ihvoLast)
		{
			if (Callbacks == null || Callbacks.EditedRootBox == null)
				return;
			ISilDataAccess sda = Callbacks.EditedRootBox.DataAccess;

			// If we modified anything, force redraw by faking a property change.
			// This will destroy the selection, so first, save it.
			int cvsli = vwsel.CLevels(false);
			cvsli--; // CLevels includes the string property itself, but AllTextSelInfo doesn't need it.
			int ihvoRoot;
			int tagTextProp;
			int cpropPrevious;
			int ichAnchor;
			int ichEnd;
			int ws;
			bool fAssocPrev;
			int ihvoEnd;
			ITsTextProps ttp;
			SelLevInfo[] rgvsli = SelLevInfo.AllTextSelInfo(vwsel, cvsli,
				out ihvoRoot, out tagTextProp, out cpropPrevious, out ichAnchor, out ichEnd,
				out ws, out fAssocPrev, out ihvoEnd, out ttp);

			// Broadcast the fake property change to all roots, by calling PropChanged on the
			// SilDataAccess pointer. Pretend we deleted and re-inserted the changed items.
			int chvoChanged = ihvoLast - ihvoFirst + 1;
			IVwRootBox rootb = Callbacks.EditedRootBox;
			sda.PropChanged(rootb, (int)PropChangeType.kpctNotifyMeThenAll, hvoText,
				tagText, ihvoFirst, chvoChanged, chvoChanged);

			// Now restore the selection by a call to MakeTextSelection on the RootBox pointer.
			// DO NOT CheckHr. This may legitimately fail, e.g., if there is no editable field.
			// REVIEW JohnT: Should we try again, e.g., to make a non-editable one?
			try
			{
				rootb.MakeTextSelection(ihvoRoot, cvsli, rgvsli, tagTextProp, cpropPrevious,
					ichAnchor, ichEnd, ws, fAssocPrev, ihvoEnd, null, true);
			}
			catch
			{
				// Something went wrong... just ignore it!
			}
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
				tss = sda.get_StringProp(hvoPara, (int)StTxtPara.StTxtParaTags.kflidContents);

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
					sda.SetString(hvoPara, (int)StTxtPara.StTxtParaTags.kflidContents, tssFixed);
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
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Notifies the site that something about the selection has changed.
		/// Change the system keyboard when the selection changes.
		/// </summary>
		/// <param name="prootb"></param>
		/// <param name="vwselNew">Selection</param>
		/// <remarks>When overriding you should call the base class first.</remarks>
		/// -----------------------------------------------------------------------------------
		public virtual void SelectionChanged(IVwRootBox prootb, IVwSelection vwselNew)
		{
			CheckDisposed();
			try
			{
				// Commit any outstanding edits.
				// It is weird to do it here when a selection changes, but otherwise the action
				// doesn't get added to the Undo stack and thus isn't undoable.
				// DataNotebook calls Commit in the Update handler for the menu, so calling Commit
				// here after the selection changed should be ok.
				// This fixes TE-200.
				Commit();
				// JohnT: it's remotely possible that commit made this
				// selection no longer useable.
				if (!vwselNew.IsValid)
					return;

				// update the current selection helper
				if(Callbacks == null || Callbacks.EditedRootBox == null)
				{
					return;
				}
				m_viewSelection = SelectionHelper.Create(vwselNew,
					Callbacks.EditedRootBox.Site);

				// TimS/EberhardB: If we don't have focus we don't want to change the keyboard,
				// otherwise it might mess up the window that has focus!
				if (!m_control.Focused)
					return;

				SetKeyboardForSelection(vwselNew);
			}
			catch(Exception e)
			{
				Debug.WriteLine("Got exception in RootSite.HandleSelectionChanged: "
					+ e.Message);
			}
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

				rs.Mediator.PropertyTable.SetProperty("WritingSystemHvo", ws.ToString());
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Set the keyboard to match the writing system.
		/// </summary>
		/// <param name="ws">writing system object</param>
		/// -----------------------------------------------------------------------------------
		protected void SetKeyboardForWs(IWritingSystem ws)
		{
			if(Callbacks == null)
			{
				return;
			}
			IVwRootBox rootb = Callbacks.EditedRootBox;

			if (ws == null || rootb == null)
			{
				//Debug.WriteLine("EditingHelper.SetKeyboardForWs(" +
				//	ws.WritingSystem + "[" + ws.IcuLocale +
				//	"]) -> ActivateDefaultKeyboard()");
				ActivateDefaultKeyboard();
				return;
			}
			try
			{
				int nWs = ws.WritingSystem;
				if (nWs == m_lastKeyboardWS)
					return;
				m_lastKeyboardWS = nWs;

				//Debug.WriteLine("EditingHelper.SetKeyboardForWs(" +
				//	ws.WritingSystem + "(" + ws.IcuLocale +
				//	") -> rootb.SetKeyboardForWs(hklActive = " + (int)hklActive + ")");

				int hklActive = (int)m_hklActive;

				bool fSelectLangPending = false;
				rootb.SetKeyboardForWs(ws, ref m_sActiveKeymanKbd, ref m_nActiveLangId,
					ref hklActive, ref fSelectLangPending);
				if (fSelectLangPending)
					m_cSelectLangPending++;
				m_hklActive = (IntPtr)hklActive;

				//Debug.WriteLine("EditingHelper.SetKeyboardForWs(" +
				//	ws.WritingSystem + "(" + ws.IcuLocale +
				//	") - after rootb.SetKeyboardForWs(), LangId = " + m_nActiveLangId +
				//	", hklActive = " + (int)hklActive);
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
		/// <returns>The previous writing system</returns>
		/// ------------------------------------------------------------------------------------
		public int SetKeyboardForWs(int newWs)
		{
			CheckDisposed();

			int oldWs = m_lastKeyboardWS;
			if (Callbacks == null || !Callbacks.GotCacheOrWs || WritingSystemFactory == null)
				return oldWs;			// Can't do anything useful, so let's not do anything at all.

			IWritingSystem ws = WritingSystemFactory.get_EngineOrNull(newWs);
			SetKeyboardForWs(ws);
			return oldWs;
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
			IWritingSystem ws = null;

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
		/// <remarks>On Windows 98, sending this message unnecessarily destroys
		/// the current keystroke context, so only do it when we're actually switching
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		private void ActivateDefaultKeyboard()
		{
			InputLanguage inputLng = InputLanguage.DefaultInputLanguage;

			Debug.Assert(inputLng != null);
			// REVIEW: Do we really need to try to keep track of m_hklActive and have this
			// logic to prevent "switching" when we're going to the same keyboard? The above
			// remark suggests this was needed only for Windows 98.
			if (inputLng == null || (m_hklActive != (IntPtr)0 && inputLng.Handle == m_hklActive))
				return;

			//Debug.WriteLine("EditingHelper.ActivateKeyboard() - inputLng = " +
			//	inputLng.ToString() +
			//	" [" + (int)inputLng.Handle + "],  m_hklActive = " + (int)m_hklActive);

			if (KeyboardHelper.ActivateKeyboard(inputLng.Culture.LCID, ref m_nActiveLangId,
				ref m_sActiveKeymanKbd))
			{
				m_cSelectLangPending++;
			}

			m_hklActive = inputLng.Handle;
			m_sActiveKeymanKbd = null;
			// REVIEW: this is not quite right if the sort is not 0 (default).
			m_nActiveLangId = inputLng.Culture.LCID;
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
			m_hklActive = (IntPtr) 0;
			m_sActiveKeymanKbd = null; //"xxxUnknownyyy";
			m_nActiveLangId = 0;
			m_cSelectLangPending = 0;
			m_lastKeyboardWS = -1;

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
			if ((newFocusedControl == null || !(newFocusedControl is IRootSite)) && fIsChildWindow)
				ActivateDefaultKeyboard();
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
			// look for Ctrl-X to cut
			if (e.KeyCode == Keys.X && e.Control)
			{
				if (!CopySelection())
					return false;

				// The copy succeeded (otherwise we would have got an exception and wouldn't be
				// here), now delete the range of text that has been copied to the
				// clipboard.
				DeleteSelection("kstidEditCut");
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
			{
				PasteClipboard(true);
				return true;
			}

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

			IVwSelection vwsel = EditedRootBox.Selection;
			// Get a copy of the selection as a TsString, and store it in the clipboard, together with
			// the writing system factory.
			ITsString tss = null;
			IVwRootSite vwsite;
			vwsite = EditedRootBox.Site;
			if (vwsite is SimpleRootSite)
				tss = (vwsite as SimpleRootSite).GetTsStringForClipboard(vwsel);
			if (tss == null)
				vwsel.GetSelectionString(out tss, "; ");

			// This is pathological for a range, but apparently it can happen, e.g., for a picture selection.
			// See LT-8147.
			if (tss == null || tss.Length == 0)
				return false;

			CopyTssToClipboard(tss);

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copies the given ITsString to clipboard, along with the writing system.
		/// </summary>
		/// <param name="tss">ITsString to copy to clipboard.</param>
		/// ------------------------------------------------------------------------------------
		internal void CopyTssToClipboard(ITsString tss)
		{
			Debug.Assert(tss != null && tss.Length > 0); // if this asserts it is likely that
			// the user selected a footnote marker but the TextRepOfObj() method isn't
			// implemented.

			ILgTsStringPlusWss tssencs = LgTsStringPlusWssClass.Create();
			tssencs.set_String(WritingSystemFactory, tss);
			s_dobjClipboard = LgTsDataObjectClass.Create();
			s_dobjClipboard.Init(tssencs);
			// We want this event only once.
			Application.ApplicationExit -= new EventHandler(Application_ApplicationExit);
			Application.ApplicationExit += new EventHandler(Application_ApplicationExit);
			Clipboard.SetDataObject(s_dobjClipboard, false);
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
				return DeleteSelection("kstidEditCut");
			}
			catch
			{
				return false;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Delete the current selection
		/// </summary>
		/// <param name="stidUndoTask">String identifier for a string in <c>Strings.resx</c>
		/// file. That string is used for the Undo/Redo task description.</param>
		/// <returns><c>true</c> if deleting was successful, otherwise <c>false</c>.</returns>
		/// -----------------------------------------------------------------------------------
		public bool DeleteSelection(string stidUndoTask)
		{
			CheckDisposed();

			if (!Control.Visible || Callbacks == null ||
				Callbacks.EditedRootBox == null || !Callbacks.GotCacheOrWs)
			{
				return false;
			}

			IVwRootSite site = EditedRootBox.Site;

			// REVIEW JohnT(SteveMc): is this deletion undoable?  if not, what else needs to be done?
			using (new UndoTaskHelper(site, stidUndoTask, false))
			using (new DataUpdateMonitor(Control, EditedRootBox.DataAccess, site, "DeleteSelection"))
			{
				IVwGraphics vg;
				Rect rcSrcRoot;
				Rect rcDstRoot;
				site.GetGraphics(EditedRootBox, out vg, out rcSrcRoot, out rcDstRoot);
				try
				{
					OnKeyDown(new KeyEventArgs(Keys.Delete), vg);
				}
				finally
				{
					// this needs to be called!
					site.ReleaseGraphics(EditedRootBox, vg);
				}
			}

			return true;
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
			if (sel == null || sel.SelType != VwSelType.kstText)
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
			int cpt = mdc.GetFieldType((uint) tag);
			// These four types can store embedded formatting.
			return cpt == (int)CellarModuleDefns.kcptString
				|| cpt == (int)CellarModuleDefns.kcptBigString
				|| cpt == (int)CellarModuleDefns.kcptMultiString
				|| cpt == (int)CellarModuleDefns.kcptMultiBigString;
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
				IDataObject dobj = Clipboard.GetDataObject();
				return (string)dobj.GetData(DataFormats.StringFormat);
			}
			catch
			{
				return null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine whether we can paste a URL at the current location.
		/// Requires a suitable selection and a URL in the clipboard.
		/// A file name is acceptable as a URL.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool CanPasteUrl()
		{
			CheckDisposed();
			if (!IsSelectionInOneFormattableProp())
				return false;
			if (!ClipboardContainsString())
				return false;
			string clip = GetClipboardAsString();
			if (clip == null || clip.Length == 0)
				return false;
			return true; // prefer: ::UrlIs(clip, URLIS_URL) if we can find .NET equivalent.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Insert an external link.
		/// </summary>
		/// <param name="stylesheet">The stylesheet.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool InsertExternalLink(/*Control control, */FwStyleSheet stylesheet)
		{
			CheckDisposed();
			if (!CanInsertExternalLink())
				return false;
			// This didn't work...we need the first argument non-null, and I don't know how to get a useful instance.
			//string url = System.Web.UI.Design.UrlBuilder.BuildUrl(null, control, "", "Choose target to link to", "");
			using (OpenFileDialog fileDialog = new OpenFileDialog())
			{
				fileDialog.Filter = ResourceHelper.FileFilter(FileFilterType.AllFiles);
				fileDialog.RestoreDirectory = true;
				if (fileDialog.ShowDialog() != DialogResult.OK)
					return false;
				string pathname = fileDialog.FileName;
				if (pathname == null || pathname == "")
					return false;
				ConvertSelToLink(pathname, stylesheet);
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Indicate whether we can insert an external link. Currently this is the same as
		/// the method called, but in case we think of more criteria...
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool CanInsertExternalLink()
		{
			CheckDisposed();
			return IsSelectionInOneFormattableProp();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Paste the contents of the clipboard as a hot link.
		/// </summary>
		/// <param name="stylesheet">The stylesheet.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool PasteUrl(FwStyleSheet stylesheet)
		{
			CheckDisposed();
			if (!CanPasteUrl())
				return false;
			string clip = GetClipboardAsString();

			ConvertSelToLink(clip, stylesheet);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Applies the given URL as a hotlink to the currently selected text, if any, or
		/// inserts a link to the URL.
		/// </summary>
		/// <param name="clip">The URL.</param>
		/// <param name="stylesheet">The stylesheet.</param>
		/// ------------------------------------------------------------------------------------
		void ConvertSelToLink(string clip, FwStyleSheet stylesheet)
		{
			CheckDisposed();

			if (m_callbacks == null || m_callbacks.EditedRootBox == null)
			{
				return;
			}
			IVwSelection sel = m_callbacks.EditedRootBox.Selection;
			ISilDataAccess sda = m_callbacks.EditedRootBox.DataAccess;
			ITsString tssLink;
			bool fGotItAll;
			sel.GetFirstParaString(out tssLink, " ", out fGotItAll);
			ITsStrBldr tsb = tssLink.GetBldr();
			if (sel.IsRange)
			{
				// Use the text of the selection as the text of the link
				int ich = tssLink.Text.IndexOf(Environment.NewLine);
				if (!fGotItAll || ich >= 0)
				{
					tsb.ReplaceTsString(ich, tsb.Length, null);
					int ichTop;
					if (sel.EndBeforeAnchor)
						ichTop = sel.get_ParagraphOffset(true);
					else
						ichTop = sel.get_ParagraphOffset(false);
					SelectionHelper helper =
						SelectionHelper.Create(sel, EditedRootBox.Site);
					helper.IchAnchor = ichTop;
					helper.IchEnd = ich;
					sel = helper.Selection;
				}
				//sel.GetSelectionString(out tssLink, " ");
			}
			if (!sel.IsRange)
			{
				ITsStrFactory tsf = TsStrFactoryClass.Create();
				tssLink = tsf.MakeString(clip, sda.WritingSystemFactory.UserWs);
				tsb = tssLink.GetBldr();
			}

			if (!TsStringAccessor.MarkTextInBldrAsHyperlink(tsb, 0, tsb.Length, clip, stylesheet))
				return;
			tssLink = tsb.GetString();

			sda.BeginUndoTask(SimpleRootSiteStrings.ksUndoInsertLink, SimpleRootSiteStrings.ksRedoInsertLink);
			sel.ReplaceWithTsString(tssLink);
			sda.EndUndoTask();
			// Arrange that immediate further typing won't extend link.
			sel = Callbacks.EditedRootBox.Selection; // may have been changed.
			if (sel == null)
				return;
			ITsPropsBldr pb = tssLink.get_PropertiesAt(0).GetBldr();
			pb.SetStrPropValue((int)FwTextPropType.ktptObjData, null);
			pb.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, null);
			sel.SetIpTypingProps(pb.GetTextProps());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Appends the given text as a hyperlink to the given URL.
		/// </summary>
		/// <param name="strBldr">The string builder.</param>
		/// <param name="ws">The HVO of the writing system to use for the added text.</param>
		/// <param name="sLinkText">The text which should appear as the hyperlink text</param>
		/// <param name="sUrl">The URL that is the target of the hyperlink.</param>
		/// <param name="stylesheet">The stylesheet.</param>
		/// <returns><c>true</c> if the hyperlink was successfully inserted; <c>false</c>
		/// otherwise (indicating that the hyperlink style could not be found in the given
		/// stylesheet). In either case, the link text will be appended to the string builder.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public static bool AddHyperlink(ITsStrBldr strBldr, int ws, string sLinkText, string sUrl,
			FwStyleSheet stylesheet)
		{
			int ichStart = strBldr.Length;
			strBldr.Replace(ichStart, ichStart, sLinkText, StyleUtils.CharStyleTextProps(null, ws));
			return TsStringAccessor.MarkTextInBldrAsHyperlink(strBldr, ichStart, strBldr.Length,
				sUrl, stylesheet);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Paste data from the clipboard into the view.
		/// </summary>
		/// <param name="pasteOneParagraph">true to only paste the first paragraph
		/// of the selection</param>
		/// ------------------------------------------------------------------------------------
		public virtual void PasteClipboard(bool pasteOneParagraph)
		{
			CheckDisposed();
			// Do nothing if command is not enabled. Needed for Ctrl-V keypress.
			if (!CanPaste() || Callbacks == null || Callbacks.EditedRootBox == null ||
				!Callbacks.GotCacheOrWs  || CurrentSelection == null)
				return;

			// Commit any changes.
			IVwSelection vwsel = CurrentSelection.Selection;
			if (vwsel == null || !Commit(vwsel))
				return;

			// Handle anything needed immediately before the paste.
			Callbacks.PrePasteProcessing();

			ITsTextProps[] vttp;
			IVwPropertyStore[] vvps;
			int cttp;
			SelectionHelper.GetSelectionProps(vwsel, out vttp, out vvps, out cttp);
			if (cttp == 0)
				return;

			ChangeStyleForPaste(vwsel, ref vttp);

			bool fCanFormat = vwsel.CanFormatChar;
			ITsTextProps ttpSel = vttp[0];

			ITsString tss = GetTextFromClipboard(vwsel, fCanFormat, ttpSel);
			if (tss != null && pasteOneParagraph)
				tss = RemoveExtraParagraphs(tss);

			PasteCore(tss);
		}

		/// <summary>
		/// Pull out the core of Paste for testing (without modifying the clipboard).
		/// </summary>
		/// <param name="tss"></param>
		/// <returns></returns>
		public void PasteCore(ITsString tss)
		{
			IVwSelection vwsel = EditedRootBox.Selection;
			IVwRootSite rootSite = EditedRootBox.Site;
			using (UndoTaskHelper undoHelper = new UndoTaskHelper(rootSite, "kstidEditPaste", false))
			{
				try
				{
					using (DataUpdateMonitor dum = new DataUpdateMonitor(Control,
						EditedRootBox.DataAccess, rootSite, "EditPaste"))
					{
						if (tss != null)
						{	// insert the text

							// At this point, we may need to override internal formatting values
							// for certain target rootsites.  We do this with an event handler that the
							// rootsite can register for its editing helper.  (See LT-1445.)
							if (PasteFixTssEvent != null)
							{
								try
								{
									FwPasteFixTssEventArgs args = new FwPasteFixTssEventArgs(tss);
									PasteFixTssEvent(this, args);
									tss = args.TsString;
								}
								catch { }
							}

							// The replace with TsString can cause propchanges to occur before
							// the view is ready to process them so wait until the database is
							// fully updated before doing the prop changes. (TE-8048)
							VwCacheDa cda = EditedRootBox.DataAccess as VwCacheDa;
							if (DelayPastePropChanges && cda != null)
								cda.SuppressPropChanges();
							try
							{
								vwsel.ReplaceWithTsString(tss);
							}
							finally
							{
								if (DelayPastePropChanges && cda != null)
									cda.ResumePropChanges();
							}

							if (vwsel.IsValid)
							{
								Commit(vwsel); // Nothing sensible to do if not Ok...
								dum.InsertedTss = tss;
							}
						}
					}
					rootSite.ScrollSelectionIntoView(null, VwScrollSelOpts.kssoDefault);
				}
				catch (Exception e)
				{
					// TE-6908/LT-6781
					Logger.WriteError(e);

					if (vwsel.IsValid)
						Commit(vwsel); // Nothing sensible to do if not Ok...
					// REVIEW: Using EndUndoTask was causing previous undo
					// action to be undone.
					undoHelper.EndUndoTask = false;
					if (!(e is COMException) || (uint)((COMException)e).ErrorCode != 0x80004005) // E_FAIL
						throw new ContinuableErrorException("Error during paste. Paste has been undone.", e);
					else
						MiscUtils.ErrorBeep();
				}
			}
		}

		/// <summary>
		/// Snapshot of the selection state before the edit.
		/// if non-null it should be valid while DataUpdate monitor is in use (including its final PropChange in dispose).
		/// </summary>
		public virtual TextSelInfo TextSelInfoBeforeEdit
		{
			get { return m_tsiOrig; }
			set { m_tsiOrig = value; }
		}

		/// <summary>
		/// called before using editing helper to peform an edit task
		/// </summary>
		public virtual void OnAboutToEdit()
		{
			m_tsiOrig = new TextSelInfo(EditedRootBox);
		}

		/// <summary>
		/// call after using editing helper to perform an edit task.
		/// </summary>
		public virtual void OnFinishedEdit()
		{
			TextSelInfoBeforeEdit = null;
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
			// Get the data currently stored on the clipboard.
			// If the clipboard is storing an actual TsString (and LgWritingSystemFactory),
			// paste it in. It must be from our nonstandard implementation of IDataObject,
			// LgTsDataObject.
			ITsString tss = null;
			object oleDataObject = null;
			try
			{
				oleDataObject = GetOleDataObject();
				if (oleDataObject is ILgTsDataObject)
				{
					ILgTsDataObject tsdo = (ILgTsDataObject)oleDataObject;
					FormatEtc format = new FormatEtc();
					StgMedium medium = new StgMedium();
					uint uFormat = 0;
					tsdo.GetClipboardType(out uFormat);
					format.cfFormat = uFormat;
					format.ptd = 0;
					format.dwAspect = DvAspect.Content;
					format.lindex = -1;
					format.tymed = Tymed.IStorage;
					int hr = ((IOleDataObject)tsdo).GetData(ref format, ref medium);
					if (hr == 0)
					{
						if (medium.tymed == Tymed.IStorage && medium.pstg != null)
						{
							ILgTsStringPlusWss tssencs = LgTsStringPlusWssClass.Create();
							tssencs.Deserialize(medium.pstg);
							Marshal.ReleaseComObject(medium.pstg);
							ILgWritingSystemFactory wsf = tssencs.WritingSystemFactory;
							int destWs;
							PasteStatus pasteStatus = DeterminePasteWs(wsf, out destWs);
							switch (pasteStatus)
							{
								case PasteStatus.PreserveWs:
									tss = tssencs.get_String(WritingSystemFactory);
									break;
								case PasteStatus.UseDestWs:
									Debug.Assert(destWs > 0);
									tss = tssencs.get_StringUsingWs(destWs);
									break;
								case PasteStatus.CancelPaste:
									return tss;
							}

							// REVIEW (EberhardB): Should this really be in here or is it specific to DN?
							//+ Begin fix for Raid bug 897B
							// Check for an embedded picture.
							int crun = tss.RunCount;
							bool fHasPicture = false;
							ITsTextProps ttp;
							for (int irun = 0; irun < crun; ++irun)
							{
								ttp = tss.get_Properties(irun);
								string str = ttp.GetStrPropValue((int)FwTextStringProp.kstpObjData);
								if (str != null)
								{
									char chType = str[0];
									if (chType == (int)FwObjDataTypes.kodtPictOdd ||
										chType == (int)FwObjDataTypes.kodtPictEven)
									{
										fHasPicture = true;
										break;
									}
								}
							}

							if (fHasPicture)
							{
								// Vars to call TextSelInfo and find out whether it is a structured
								// text field.
								ITsString tssDummy;
								int ich;
								bool fAssocPrev;
								int hvoObj;
								int tag;
								int wsTmp;
								vwsel.TextSelInfo(false, out tssDummy, out ich, out fAssocPrev,
									out hvoObj, out tag, out wsTmp);
								if (tag != (int)StTxtPara.StTxtParaTags.kflidContents)
								{
									// TODO (EberhardB): This seems to be Notebook specific!
									MessageBox.Show(ResourceHelper.GetResourceString("kstidPicsMultiPara"));
									tss = null;
								}
							}
							//- End fix for Raid bug 897B
							if (!fCanFormat && tss != null)
							{
								// remove formatting from the TsString
								ITsStrFactory tsf = TsStrFactoryClass.Create();
								string str = tss.Text;
								tss = tsf.MakeStringWithPropsRgch(str, str.Length, ttpSel);
							}
						}
					}
				}
			}
			finally
			{
				if (oleDataObject != null)
				{
					Marshal.ReleaseComObject(oleDataObject);
					oleDataObject = null;
				}
			}

			if (tss == null)
			{	// all else didn't work, so try with an ordinary string
				string str = Clipboard.GetText();
				ITsStrFactory tsf = TsStrFactoryClass.Create();
				tss = tsf.MakeStringWithPropsRgch(str, str.Length, ttpSel);
			}
			return tss;
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Remove data past the first paragraph in a TsString.
		/// </summary>
		/// <param name="tss">string to be checked</param>
		/// <returns>edited string</returns>
		/// ------------------------------------------------------------------------------------
		private ITsString RemoveExtraParagraphs(ITsString tss)
		{
			int length = tss.Length;

			if (length > 0)
			{
				// Get the style sheet. If there is no style sheet then it will not be possible
				// to check for style information.
				if(Callbacks == null || Callbacks.EditedRootBox == null)
				{
					return tss;
				}
				IVwStylesheet stylesheet = Callbacks.EditedRootBox.Stylesheet;
				if (stylesheet == null)
					return tss;

				TsRunInfo runInfo;
				ITsTextProps runProps;
				// ENHANCE (EberhardB): We should check for \r\n in addition to the
				// paragraph style!

				// Look at each of the runs after the first one to find a paragraph style.
				for (int run = 0; run < tss.RunCount; run++)
				{
					// If the run has a paragraph style then truncate the string at the beginning
					// of the run.
					runProps = tss.FetchRunInfo(run, out runInfo);
					string styleName = runProps.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
					if (styleName != null &&
						(StyleType) stylesheet.GetType(styleName) == StyleType.kstParagraph)
					{
						ITsStrBldr bldr = tss.GetBldr();
						bldr.Replace(runInfo.ichMin, length, string.Empty, null);
						return bldr.GetString();
					}
				}
			}
			return tss;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Retrieve the I(Ole)DataObject handle from the clipboard.
		/// </summary>
		/// <remarks>.NET uses a different IDataObject than COM. The COM one is called
		/// IOleDataObject internally by .NET. In order to be able to retrieve our
		/// TsString we have to get the IOleDataObject. Unfortunately there is no
		/// public method, so we have to do it via Reflection.</remarks>
		/// <returns><c>IOleDataObject</c> object if successful, otherwise <c>null</c>.</returns>
		/// -----------------------------------------------------------------------------------
		private object GetOleDataObject()
		{
			IDataObject dobj = Clipboard.GetDataObject();
			object oleDataObject = null;	// this will be our COM IDataObject
			FieldInfo fieldInfo = dobj.GetType().GetField("innerData",
				BindingFlags.NonPublic | BindingFlags.Instance);

			Debug.Assert(fieldInfo != null,
				"Oops! Getting DataObject.innerData via reflection failed.");
			if (fieldInfo != null)
			{
				IDataObject oleConverter = (IDataObject)fieldInfo.GetValue(dobj);

				Debug.Assert(oleConverter != null,
					"Oops! Getting OleConverter via reflection failed.");
				if (oleConverter != null)
				{
					fieldInfo = oleConverter.GetType().GetField("innerData",
						BindingFlags.NonPublic | BindingFlags.Instance);

					Debug.Assert(fieldInfo != null,
						"Oops! Getting OleConverter.innerData via reflection failed.");
					if (fieldInfo != null)
					{
						oleDataObject = fieldInfo.GetValue(oleConverter);
						Debug.Assert(oleDataObject != null,
							"Getting COM IDataObject (IOleDataObject) via reflection failed.");
					}

				}
			}

			// now oleDataObject should contain the IOleDataObject object
			return oleDataObject;
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
				return Clipboard.ContainsText();
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
			if (Callbacks != null && Callbacks.EditedRootBox != null && m_fEditable &&
				CurrentSelection != null && Control.Visible)
			{
				IVwSelection vwsel = CurrentSelection.Selection;
				// CanFormatChar is true only if the selected text is editable.
				if (vwsel != null && vwsel.CanFormatChar)
					return ClipboardContainsString();
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a string representation of the object suitable to put on the clipboard.
		/// </summary>
		/// <param name="cache">FDO cache representing the DB connection to use</param>
		/// <param name="guid">The guid of the object in the DB</param>
		/// ------------------------------------------------------------------------------------
		public string TextRepOfObj(FdoCache cache, Guid guid)
		{
			CheckDisposed();
			return cache.TextRepOfObj(guid);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a new object, given a text representation (e.g., from the clipboard).
		///
		/// </summary>
		/// <param name="cache">FDO cache representing the DB connection to use</param>
		/// <param name="sTextRep">Text representation of object</param>
		/// <param name="selDst">Provided for information in case it's needed to generate
		/// the new object (E.g., footnotes might need it to generate the proper sequence
		/// letter)</param>
		/// <param name="kodt">The object data type to use for embedding the new object
		/// </param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public virtual Guid MakeObjFromText(FdoCache cache, string sTextRep,
			IVwSelection selDst, out int kodt)
		{
			CheckDisposed();
			Guid guid = Guid.Empty;
			kodt = 0;
			// Keep trying different types of objects until one of them recognizes the string
			try
			{
				// try to make picture
				guid = MakePictureFromText(cache, sTextRep, DefaultPictureFolder);
				kodt = (int)FwObjDataTypes.kodtGuidMoveableObjDisp;
			}
			catch
			{
			}
			if (guid == Guid.Empty)
				throw new ArgumentException("Unexpected object representation string: " + sTextRep, "stextRep");
			return guid;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new CmPicture object and returns its guid, given a string representation
		/// of a picture (e.g., from the clipboard).
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="sTextRepOfPicture"></param>
		/// <param name="sFolder">Name of the folder where picture file is to be stored</param>
		/// ------------------------------------------------------------------------------------
		public Guid MakePictureFromText(FdoCache cache, string sTextRepOfPicture, string sFolder)
		{
			CheckDisposed();
			CmPicture pict = new CmPicture(cache, sTextRepOfPicture, sFolder);
			return cache.GetGuidFromId(pict.Hvo);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Make a selection that includes all the text
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public void SelectAll()
		{
			CheckDisposed();
			if (!Control.Visible || Callbacks == null || Callbacks.EditedRootBox == null || !Callbacks.GotCacheOrWs)
				return;

			Control.Focus();

			IVwRootBox rootb = Callbacks.EditedRootBox;

			using (new WaitCursor(Control))
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
		#endregion

		/// <summary>
		/// This is hooked up to the Application.ApplicationExit event, but that appears to be too late
		/// for the clipboard data to survive.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private static void Application_ApplicationExit(object sender, EventArgs e)
		{
			ClearTsStringClipboard();
		}

		/// <summary>
		/// If there is a mis-spelled word at the specified point, display the spell-check menu and return true.
		/// Otherwise, return false.
		/// </summary>
		/// <param name="pt"></param>
		/// <param name="control"></param>
		/// <param name="rcSrcRoot"></param>
		/// <param name="rcDstRoot"></param>
		/// <returns></returns>
		public bool DoSpellCheckContextMenu(Point pt, Control control, Rectangle rcSrcRoot, Rectangle rcDstRoot)
		{
			ContextMenuStrip menu = new ContextMenuStrip();
			MakeSpellCheckMenuOptions(pt, Callbacks.EditedRootBox, rcSrcRoot, rcDstRoot, menu);
			if (menu.Items.Count == 0)
				return false;
			menu.Show(control, pt);
			return true;
		}

		/// <summary>
		/// Add the word to the spelling dictionary. The commonly used subclass, RootSiteEditingHelper,
		/// overrides to also add to the wordform inventory.
		/// </summary>
		/// <param name="dict"></param>
		/// <param name="word"></param>
		/// <param name="ws">relevant writing system (important for override)</param>
		public virtual void AddToSpellDict(Dictionary dict, string word, int ws)
		{
			dict.Add(word);
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
	}
	#endregion

	// A helper class for MakeSpellCheckColleague. See comments there.
	///// <summary>
	///// A temporary colleague that contains enough information to correct a spelling error
	///// in a particular string.
	///// </summary>
	//internal class SpellCorrectColleague : IxCoreColleague
	//{
	//    IVwRootBox m_rootb;
	//    ICollection<string> m_suggestions;
	//    int m_hvoObj;
	//    int m_tag;
	//    int m_wsAlt;
	//    int m_wsText;
	//    int m_ichMin;
	//    int m_ichLim;
	//    string m_word; // supposedly incorrect word.
	//    Dictionary m_dict;
	//    EditingHelper m_helper;

	//    public SpellCorrectColleague(IVwRootBox rootb, ICollection<string> suggestions,
	//        int hvoObj, int tag, int wsAlt, int wsText, int ichMin, int ichLim, string word, Dictionary dict, EditingHelper helper)
	//    {
	//        m_rootb = rootb;
	//        m_suggestions = suggestions;
	//        m_hvoObj = hvoObj;
	//        m_tag = tag;
	//        m_wsAlt = wsAlt;
	//        m_wsText = wsText;
	//        m_ichMin = ichMin;
	//        m_ichLim = ichLim;
	//        m_word = word;
	//        m_dict = dict;
	//        m_helper = helper;
	//    }

	//    #region methods called by reflection (mediator.Broadcast)

	//    public bool OnDisplayPossibleCorrections(string wsList, UIListDisplayProperties display)
	//    {
	//        XCore.List items = display.List;
	//        XmlDocument doc = new XmlDocument();

	//        foreach (string suggestion in m_suggestions)
	//        {
	//            XmlNode paramNode = doc.CreateElement("param");
	//            XmlAttribute att = doc.CreateAttribute("correction");
	//            att.Value = suggestion;
	//            paramNode.Attributes.Append(att);
	//            items.Add(suggestion, suggestion, null, paramNode);
	//        }
	//        if (m_suggestions.Count == 0)
	//        {
	//            XmlNode paramNode = doc.CreateElement("param"); // dummy
	//            items.Add(SimpleRootSiteStrings.ksNoSuggestions, SimpleRootSiteStrings.ksNoSuggestions, null, paramNode);
	//        }
	//        return true;
	//    }

	//    /// <summary>
	//    /// We want to display the Correct Spelling item (and submenu) if we this colleague
	//    /// exists at all.
	//    /// </summary>
	//    /// <param name="arg"></param>
	//    /// <param name="display"></param>
	//    /// <returns></returns>
	//    public bool OnDisplayCorrectSpelling(object arg, UIItemDisplayProperties display)
	//    {
	//        display.Visible = true;
	//        display.Enabled = true;
	//        return true;
	//    }

	//    /// <summary>
	//    /// Mediator-called method to do spelling correction.
	//    /// </summary>
	//    /// <param name="arg"></param>
	//    /// <returns></returns>
	//    public bool OnCorrectSpelling(object arg)
	//    {
	//        XmlNode paramNode = arg as XmlNode;
	//        if (paramNode.Attributes["correction"] == null)
	//            return true; // "No suggestions" item.
	//        string correction = paramNode.Attributes["correction"].Value;
	//        m_rootb.DataAccess.BeginUndoTask(SimpleRootSiteStrings.ksUndoCorrectSpelling, SimpleRootSiteStrings.ksRedoSpellingChange);
	//        ITsStrBldr bldr = m_rootb.DataAccess.get_MultiStringAlt(m_hvoObj, m_tag, m_wsAlt).GetBldr();
	//        bldr.Replace(m_ichMin, m_ichLim, correction, null);
	//        m_rootb.DataAccess.SetMultiStringAlt(m_hvoObj, m_tag, m_wsAlt, bldr.GetString());
	//        m_rootb.DataAccess.PropChanged(null, (int)PropChangeType.kpctNotifyAll, m_hvoObj, m_tag, m_wsAlt, 1, 1);
	//        m_rootb.DataAccess.EndUndoTask();
	//        return true;
	//    }

	//    /// <summary>
	//    /// Mediator-called method to add 'incorrect' word to dictionary.
	//    /// </summary>
	//    /// <param name="arg"></param>
	//    /// <returns></returns>
	//    public bool OnAddToSpellDict(object arg)
	//    {
	//        m_helper.AddToSpellDict(m_dict, m_word, m_wsText);
	//        m_rootb.DataAccess.PropChanged(null, (int)PropChangeType.kpctNotifyAll, m_hvoObj, m_tag, m_wsAlt, 1, 1);
	//        return true;
	//    }
	//    #endregion methods called by reflection (mediator.Broadcast)

	//    #region IxCoreColleague Members

	//    /// <summary>
	//    /// No addtional ones, but include yourself.
	//    /// </summary>
	//    /// <returns></returns>
	//    public IxCoreColleague[] GetMessageTargets()
	//    {
	//        return new IxCoreColleague[] { this };
	//    }

	//    /// <summary>
	//    /// Required interface method, but nothing to do.
	//    /// </summary>
	//    /// <param name="mediator"></param>
	//    /// <param name="configurationParameters"></param>
	//    public void Init(Mediator mediator, System.Xml.XmlNode configurationParameters)
	//    {

	//    }

	//    #endregion
	//}

	/// <summary>
	/// Menu item subclass containing the information needed to correct a spelling error.
	/// </summary>
	public class SpellCorrectMenuItem : ToolStripMenuItem
	{
		ISilDataAccess m_sda;
		int m_hvoObj;
		int m_tag;
		int m_wsAlt; // 0 if not multilingual--not yet implemented.
		int m_ichMin; // where to make the change.
		int m_ichLim; // end of string to replace
		ITsString m_tssReplacement;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public SpellCorrectMenuItem(ISilDataAccess sda, int hvoObj, int tag, int wsAlt, int ichMin, int ichLim, string text, ITsString tss)
			: base(text)
		{
			m_sda = sda;
			m_hvoObj = hvoObj;
			m_tag = tag;
			m_wsAlt = wsAlt;
			m_ichMin = ichMin;
			m_ichLim = ichLim;
			m_tssReplacement = tss;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void DoIt()
		{
			m_sda.BeginUndoTask(SimpleRootSiteStrings.ksUndoCorrectSpelling, SimpleRootSiteStrings.ksRedoSpellingChange);
			ITsString tssInput;
			if (m_wsAlt == 0)
				tssInput = m_sda.get_StringProp(m_hvoObj, m_tag);
			else
				tssInput = m_sda.get_MultiStringAlt(m_hvoObj, m_tag, m_wsAlt);
			ITsStrBldr bldr = tssInput.GetBldr();
			bldr.ReplaceTsString(m_ichMin, m_ichLim, m_tssReplacement);
			if (m_wsAlt == 0)
				m_sda.SetString(m_hvoObj, m_tag, bldr.GetString());
			else
				m_sda.SetMultiStringAlt(m_hvoObj, m_tag, m_wsAlt, bldr.GetString());
			m_sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll, m_hvoObj, m_tag, m_wsAlt, 1, 1);
			m_sda.EndUndoTask();
		}
	}

	/// <summary>
	/// Menu item subclass containing the information needed to add an item to a dictionary.
	/// </summary>
	public class AddToDictMenuItem : ToolStripMenuItem
	{
		Dictionary m_dict;
		string m_word;
		ISilDataAccess m_sda;
		int m_hvoObj;
		int m_tag;
		int m_wsAlt; // 0 if not multilingual--not yet implemented.
		int m_wsText; // ws of actual word
		EditingHelper m_helper;

		/// <summary>
		///  Make one
		/// </summary>
		/// <param name="dict"></param>
		/// <param name="word"></param>
		/// <param name="sda"></param>
		/// <param name="hvoObj"></param>
		/// <param name="tag"></param>
		/// <param name="wsAlt"></param>
		/// <param name="wsText"></param>
		/// <param name="text"></param>
		/// <param name="helper"></param>
		internal AddToDictMenuItem(Dictionary dict, string word, ISilDataAccess sda,
			int hvoObj, int tag, int wsAlt, int wsText, string text, EditingHelper helper) : base(text)
		{
			m_sda = sda;
			m_dict = dict;
			m_word = word;
			m_hvoObj = hvoObj;
			m_tag = tag;
			m_wsAlt = wsAlt;
			m_wsText = wsText;
			m_helper = helper;
		}


		/// <summary>
		/// Add the current word to the dictionary.
		/// </summary>
		public void AddWordToDictionary()
		{
			m_sda.BeginUndoTask(SimpleRootSiteStrings.ksUndoAddToSpellDictionary,
				SimpleRootSiteStrings.ksRedoAddToSpellDictionary);
			if (m_sda.GetActionHandler() != null)
				m_sda.GetActionHandler().AddAction(new UndoAddToSpellDictAction(m_dict, m_word, m_sda,
				m_hvoObj, m_tag, m_wsAlt));
			m_helper.AddToSpellDict(m_dict, m_word, m_wsText);
			m_sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll, m_hvoObj, m_tag, m_wsAlt, 1, 1);
			m_sda.EndUndoTask();
		}

		/// <summary>
		/// This information is useful for an override of MakeSpellCheckMenuOptions in TeEditingHelper.
		/// </summary>
		public string Word
		{
			get { return m_word; }
		}

		/// <summary>
		/// The writing system of the actual mis-spelled word.
		/// </summary>
		public int WritingSystem
		{
			get { return m_wsText; }
		}
	}
	/// <summary>
	/// Supports undoing and redoing adding an item to a dictionary
	/// </summary>
	class UndoAddToSpellDictAction : IUndoAction
	{
		private Dictionary m_dict;
		private string m_word;
		int m_hvoObj;
		int m_tag;
		int m_wsAlt;
		ISilDataAccess m_sda;

		public UndoAddToSpellDictAction(Dictionary dict, string word, ISilDataAccess sda,
			int hvoObj, int tag, int wsAlt)
		{
			m_dict = dict;
			m_word = word;
			m_hvoObj = hvoObj;
			m_tag = tag;
			m_wsAlt = wsAlt;
			m_sda = sda;
		}

		#region IUndoAction Members

		public void Commit()
		{
		}

		public bool IsDataChange()
		{
			return true;
		}

		public bool IsRedoable()
		{
			return true;
		}

		public bool Redo(bool fRefreshPending)
		{
			m_dict.Add(m_word);
			m_sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll, m_hvoObj, m_tag, m_wsAlt, 1, 1);
			return true;
		}

		public bool RequiresRefresh()
		{
			return false;
		}

		public bool SuppressNotification
		{
			set { }
		}

		public bool Undo(bool fRefreshPending)
		{
			m_dict.Remove(m_word);
			m_sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll, m_hvoObj, m_tag, m_wsAlt, 1, 1);
			return true;
		}

		#endregion
	}
	#region WordEventArgs struct
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Holds the arguments for the IsWordBreak" and
	/// see CommitIfWord" methods.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public struct WordEventArgs
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
