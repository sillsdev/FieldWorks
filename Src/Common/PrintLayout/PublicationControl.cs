// --------------------------------------------------------------------------------------------
#region /// Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: PublicationControl.cs
// Responsibility: TE Team
//
// <remarks>
// Base class for all types of publications.
// </remarks>
// --------------------------------------------------------------------------------------------
using Accessibility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Resources;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using XCore;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Application;

namespace SIL.FieldWorks.Common.PrintLayout
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Base class for layout of a printable publication in a FieldWorks application.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public abstract class PublicationControl : UserControl, IFWDisposable, ISelectableView,
		IEditingCallbacks, IRootSite, IPrintRootSite, IVwRootSite, IPublicationView,
		IReceiveSequentialMessages
	{
		#region Constants
		/// <summary>default normal line spacing in millipoints (negative for "exact" line spacing
		/// </summary>
		private const int kDefaultNormalLineSpacing = -12000;
		#endregion

		#region Data Members
		/// <summary>Collection of DivisionLayoutMgr</summary>
		protected List<DivisionLayoutMgr> m_divisions = new List<DivisionLayoutMgr>(1);
		/// <summary>Collection of Pages</summary>
		/// <remarks>readonly means we can't replace the list with a new one, but we still can
		/// modify the contents</remarks>
		protected readonly List<Page> m_pages = new List<Page>();
		/// <summary>Used to keep track of page currently being laid out so that we can accurately
		/// determine the available page width when required.</summary>
		private Page m_pageBeingLaidOut;
		private bool m_fRefreshPending = false;
		/// <summary></summary>
		protected IPublication m_publication;
		/// <summary>Indicates whether this publication has ever been configured before.</summary>
		protected bool m_fConfigured = false;
		/// <summary>Any substreams which are shared among divisions need to be owned by the
		/// publication so they can be refreshed and disposed of at the right times.</summary>
		protected List<IVwLayoutStream> m_sharedStreams = new List<IVwLayoutStream>();
		/// <summary>The original (non-overridden) stylesheet</summary>
		protected FwStyleSheet m_origStylesheet;
		/// <summary>The stylesheet used for laying out this publication (can be an
		/// in-memory override of the original)</summary>
		protected FwStyleSheet m_stylesheet;
		/// <summary>The database cache.</summary>
		protected FdoCache m_cache;
		/// <summary>if set to <c>true</c>, apply style overrides.</summary>
		private bool m_fApplyStyleOverrides;
		/// <summary>if set to <c>true</c> apply uniform line spacing, rather than
		/// proportional spacing.</summary>
		protected bool m_fUniformLineSpacing;
		private bool m_fInPaint;
		private bool m_fDisposingPages = false;
		/// <summary>1-based page number of the page currently being printed (if any)</summary>
		protected int m_currentPrintPage;

		private volatile float m_Zoom = 1;
		private int m_dympPageHeight = MiscUtils.kdzmpInch * 11; // 11 inches, US Letter
		private int m_dxmpPageWidth = MiscUtils.kdzmpInch * 17 / 2; // 8.5 inches, US Letter
		private MessageSequencer m_sequencer;

		/// <summary>If 0 we allow OnPaint to execute, if non-zero we don't perform OnPaint.
		/// This is used to prevent redraws from happening while we do a RefreshDisplay.
		/// This also sends the WM_SETREDRAW message.
		/// </summary>
		/// <remarks>Access to this variable should only be done through the property
		/// AllowPaint.</remarks>
		private volatile int m_nAllowPaint;

		private GraphicsManager m_screenGraphics;
		private GraphicsManager m_printerGraphics;

		/// <summary>Holds the date/time that the publication is printed</summary>
		protected DateTime m_printDateTime;

		/// <summary>printer resolution values.  These start at 300dpi as a default.</summary>
		protected float m_printerDpiX = 300.0f;
		/// <summary>printer resolution values.  These start at 300dpi as a default.</summary>
		protected float m_printerDpiY = 300.0f;
		/// <summary>screen resolution values.  These start at 96dpi as a default.</summary>
		protected float m_screenDpiX = 96.0f;
		/// <summary>screen resolution values.  These start at 96dpi as a default.</summary>
		protected float m_screenDpiY = 96.0f;

		/// <summary>
		/// <see cref="T:WsPending"/>
		/// </summary>
		int m_wsPending = -1;

		/// <summary>This captures the notion that one of our root boxes, and only one, has
		/// focus: typing that comes to this control goes to it, it displays its selection, and
		/// so forth.</summary>
		protected IVwLayoutStream m_focusedStream;

		private Timer m_Timer; // used to flash insertion point.
		private IContainer components;
		/// <summary>Helper object for editing.</summary>
		/// <remarks>Access to this variable should only be done through the property
		/// EditingHelper.</remarks>
		private PubEditingHelper m_editingHelper;
		/// <summary>
		/// base caption string that this client window should display in the info bar
		/// </summary>
		private string m_baseInfoBarCaption;

		private IHelpTopicProvider m_helpTopicProvider;

		private delegate void BoolPropertyInvoker(bool f);
		private delegate void SizePropertyInvoker(Size s);
		#endregion

		#region Constructor & Destructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:PublicationControl"/> class with
		/// no divisions.
		/// </summary>
		/// <param name="stylesheet">The stylesheet.</param>
		/// <param name="publication">The publication that this PublicationControl gets
		/// it's information from (or null to keep the defaults)</param>
		/// <param name="printDateTime">The print date time.</param>
		/// <param name="fApplyStyleOverrides">if set to <c>true</c>, apply style overrides.</param>
		/// <param name="fUniformLineSpacing">if set to <c>true</c> apply uniform line spacing,
		/// rather than proportional spacing.</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// ------------------------------------------------------------------------------------
		public PublicationControl(FwStyleSheet stylesheet, IPublication publication,
			DateTime printDateTime, bool fApplyStyleOverrides, bool fUniformLineSpacing,
			IHelpTopicProvider helpTopicProvider)
		{
			m_origStylesheet = stylesheet;
			if (publication != null) // Can be null in tests
				m_cache = publication.Cache;
			m_publication = publication;
			m_printDateTime = printDateTime;
			m_fApplyStyleOverrides = fApplyStyleOverrides;
			m_fUniformLineSpacing = fUniformLineSpacing;
			m_helpTopicProvider = helpTopicProvider;

			AutoScroll = true;
			BackColor = SystemColors.Window;
			components = new System.ComponentModel.Container();
			m_Timer = new Timer(this.components);
			m_sequencer = new MessageSequencer(this);

			SetInfoFromDB();

			PrinterUtils.GetDefaultPrinterDPI(out m_printerDpiX, out m_printerDpiY);
			m_screenGraphics = new GraphicsManager(this);
			m_printerGraphics = new GraphicsManager(this);

			// If view overrides the stylesheet
			if (fApplyStyleOverrides)
			{
				m_origStylesheet.InitStylesheet += new EventHandler(OnInitOriginalStylesheet);

				ApplyPubOverrides();
			}
			else
				m_stylesheet = m_origStylesheet;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:PublicationControl"/> class with
		/// a single division.
		/// </summary>
		/// <param name="divLayoutMgr">The division</param>
		/// <param name="stylesheet">The stylesheet.</param>
		/// <param name="publication">The publication that this PublicationControl gets
		/// it's information from (or null to keep the defaults)</param>
		/// <param name="printDateTime">The printing date and time</param>
		/// <param name="fApplyStyleOverrides">if set to <c>true</c>, apply style overrides.</param>
		/// <param name="fUniformLineSpacing">if set to <c>true</c> apply uniform line spacing,
		/// rather than proportional spacing.</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// <remarks>Only used for testing</remarks>
		/// ------------------------------------------------------------------------------------
		protected PublicationControl(DivisionLayoutMgr divLayoutMgr, FwStyleSheet stylesheet,
			IPublication publication, DateTime printDateTime, bool fApplyStyleOverrides,
			bool fUniformLineSpacing, IHelpTopicProvider helpTopicProvider)
			: this(stylesheet, publication, printDateTime, fApplyStyleOverrides, fUniformLineSpacing,
			helpTopicProvider)
		{
			AddDivision(divLayoutMgr);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CheckDisposed()
		{
			if (IsDisposed)
			{
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.",
					GetType().Name));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Destroy all divisions and pages
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged resources;
		/// <c>false</c> to release only unmanaged resources.</param>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once. Also don't call again while we're still in here
			// (which will happen because base.Dispose() calls DestroyHandle which will call
			// this.Dispose.
			if (IsDisposed || Disposing)
				return;

			if( disposing )
			{
				if(components != null)
					components.Dispose();

				// Even if disposing is false, we still dispose of our pages and and divisions
				// to force their rootboxes to close.
				// NOTE from RandyR: No. If it is false,
				// then the other disposable stuff may have been finalized already,
				// and accessing those kinds of objects will throw exceptions.
				DisposePages();	// also calls m_pages.Clear();

				if (m_divisions != null)
				{
					foreach (DivisionLayoutMgr div in m_divisions)
						div.Dispose();
				}
				if (m_divisions != null)
					m_divisions.Clear();

				if (m_editingHelper != null)
					m_editingHelper.Dispose();

				if (m_sharedStreams != null)
				{
					foreach (IVwRootBox rootb in m_sharedStreams)
						rootb.Close();
				}

				if (m_screenGraphics != null)
					m_screenGraphics.Dispose();
				if (m_printerGraphics != null)
					m_printerGraphics.Dispose();
				if (m_Timer != null)
					m_Timer.Dispose();
			}
			m_divisions = null;
			m_sharedStreams = null;
			components = null;
			m_editingHelper = null;
			m_screenGraphics = null;
			m_printerGraphics = null;
			m_Timer = null;

			base.Dispose(disposing);

			if (disposing)
			{
				if (m_sequencer != null)
					m_sequencer.Dispose();
		}
			m_sequencer = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Dispose all defined pages.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected internal void DisposePages()
		{
			m_fDisposingPages = true; // Added for TE-5494 because the lock did not seem to be working.
								 // Paint was called while disposing pages.
			lock (m_pages)
			{
				foreach (Page page in m_pages)
					page.Dispose();
				m_pages.Clear();
			}
			m_fDisposingPages = false;
		}
		#endregion

		#region overrides of UserControl
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// We want to handle all keys!
		/// </summary>
		/// <param name="keyData"></param>
		/// <returns></returns>
		/// -----------------------------------------------------------------------------------
		protected override bool IsInputKey(Keys keyData)
		{
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// See comments in SimpleRootSite.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override bool IsInputChar(char charCode)
		{
			CheckDisposed();

			return true;
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize contents when window handle is created
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated (e);
			Configure();
			// Start a timer used to flash the insertion point if any.
			// We flash every half second (500 ms)
			m_Timer.Interval = 500;
			m_Timer.Tick += new EventHandler(OnTimer);
			m_Timer.Start();

			using (Graphics g = CreateGraphics())
			{
				m_screenDpiX = g.DpiX;
				m_screenDpiY = g.DpiY;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return coordinate transformation suitable for screen drawing in the specified
		/// element of the specified page.
		/// </summary>
		/// <param name="page"></param>
		/// <param name="pe"></param>
		/// <param name="rcSrcRoot"></param>
		/// <param name="rcDstRoot"></param>
		/// ------------------------------------------------------------------------------------
		protected void GetCoordRectsForElt(Page page, PageElement pe, out Rect rcSrcRoot,
			out Rect rcDstRoot)
		{
			// The origin of this rectangle is the offset from the origin of this rootbox's
			// data to the origin of this element (in printer pixels). Size is DPI.
			Rectangle rectSrc = new Rectangle(0, pe.OffsetToTopPageBoundary - pe.OverlapWithPreviousElement,
				(int)(DpiXPrinter), (int)(DpiYPrinter));
			rcSrcRoot = new Rect(rectSrc.Left, rectSrc.Top, rectSrc.Right, rectSrc.Bottom);

			Rectangle rectElement = pe.PositionInLayoutForScreen(IndexOfPage(page), this,
				DpiXScreen, DpiYScreen);
			// The origin of this rectangle is the offset from the origin of this element
			// to the origin of the clip rectangle (the part of the rc that
			// actually pertains to this element) (in screen pixels)
			Rectangle rectDst = new Rectangle(rectElement.Left,
				rectElement.Top, (int)(DpiXScreen * Zoom), (int)(DpiYScreen * Zoom));
			rcDstRoot = new Rect(rectDst.Left, rectDst.Top, rectDst.Right, rectDst.Bottom);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Process left or right mouse button down
		/// </summary>
		/// <param name="e"></param>
		/// -----------------------------------------------------------------------------------
		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);

			SwitchFocusHere();

			// Figure which element was clicked.
			Point pt = new Point(e.X, e.Y);
			Point ptLayout;
			Page page;
			PageElement pe = ElementFromPoint(pt, ScrollPosition, out ptLayout, out page);
			if (pe == null || pe.m_fPageElementOwnsStream)
				return;
			FocusedStream = pe.m_stream;
			IVwRootBox rootb = (IVwRootBox)pe.m_stream;

			// Convert to box coords and pass to root box (if any).
			Rect rcSrcRoot, rcDstRoot;
			GetCoordRectsForElt(page, pe, out rcSrcRoot, out rcDstRoot);

			if (e.Button == MouseButtons.Right &&
				OnRightMouseDown(pt, rootb, rcSrcRoot, rcDstRoot))
			{
				// the event was handled
				return;
			}

			if ((ModifierKeys & Keys.Shift) == Keys.Shift)
				rootb.MouseDownExtended(pt.X, pt.Y, rcSrcRoot, rcDstRoot);
			else
			{
				rootb.MouseDown(pt.X, pt.Y, rcSrcRoot, rcDstRoot);
				EditingHelper.HandleMouseDown();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Process right mouse button down
		/// </summary>
		/// <param name="pt">The point where the mouse was pressed.</param>
		/// <param name="rootb">The root box.</param>
		/// <param name="rcSrcRoot">The rc SRC root.</param>
		/// <param name="rcDstRoot">The rc DST root.</param>
		/// <returns>true if handled, false otherwise</returns>
		/// ------------------------------------------------------------------------------------
		protected virtual bool OnRightMouseDown(Point pt, IVwRootBox rootb, Rectangle rcSrcRoot,
			Rectangle rcDstRoot)
		{
			InitScreenGraphics();
			try
			{
				IVwSelection sel = (IVwSelection)rootb.Selection;
				if (sel != null && sel.IsRange)
				{
					// TODO KenZ: We need a better way to determine if the cursor is in a selection
					// when the selection spans partial lines.

					// We don't want to destroy a range selection if we are within the range, since it
					// is quite likely the user will want to do a right+click cut or paste.
					Rect rcPrimary;
					Rect rcSecondary;
					bool fSplit;
					bool fEndBeforeAnchor;

					Debug.Assert(m_screenGraphics.VwGraphics != null);

					sel.Location(m_screenGraphics.VwGraphics, rcSrcRoot, rcDstRoot,
						out rcPrimary, out rcSecondary, out fSplit,
						out fEndBeforeAnchor);

					if (pt.X >= rcPrimary.left && pt.X < rcPrimary.right
						&& pt.Y >= rcPrimary.top && pt.Y < rcPrimary.bottom)
					{
						return true;
					}
				}

				try
				{
					// Make an invisible selection to see if we are in editable text.
					sel = rootb.MakeSelAt(pt.X, pt.Y, rcSrcRoot, rcDstRoot, false);
					if (sel != null && SelectionHelper.IsEditable(sel))
					{
						// Make a simple text selection without executing a mouse click. This is
						// needed in order to not launch a hot link when we right+click.
						rootb.MakeSelAt(pt.X, pt.Y, rcSrcRoot, rcDstRoot, true);
						return true;
					}
				}
				catch
				{
				}
			}
			finally
			{
				UninitScreenGraphics();
			}
			return false;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Process mouse double click
		/// </summary>
		/// <param name="e"></param>
		/// -----------------------------------------------------------------------------------
		protected override void OnDoubleClick(EventArgs e)
		{
			base.OnDoubleClick(e);

			// Convert to box coords and pass to root box (if any).
			if (DataUpdateMonitor.IsUpdateInProgress())
				return; //throw this event away

			SwitchFocusHere();
			// Figure which element was clicked.
			Point pt = PointToClient(Cursor.Position);
			Point ptLayout;
			Page page;
			PageElement pe = ElementFromPoint(pt, ScrollPosition, out ptLayout, out page);
			if (pe == null || pe.m_fPageElementOwnsStream)
				return;
			FocusedStream = pe.m_stream;
			IVwRootBox rootb = (IVwRootBox)pe.m_stream;

			// Convert to box coords and pass to root box (if any).
			Rect rcSrcRoot, rcDstRoot;
			GetCoordRectsForElt(page, pe, out rcSrcRoot, out rcDstRoot);

			rootb.MouseDblClk(pt.X, pt.Y, rcSrcRoot, rcDstRoot);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Process mouse move
		/// </summary>
		/// <param name="e"></param>
		/// -----------------------------------------------------------------------------------
		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);

			//			if (DataUpdateMonitor.IsUpdateInProgress(DataAccess))
			//				return;

			// Figure which stream and page and page element and get the appropriate
			// coordinate transformation.
			Point mousePos = new Point(e.X, e.Y);
			Point ptLayout;
			Page page;
			PageElement pe = ElementFromPoint(mousePos, ScrollPosition,
				out ptLayout, out page);
			if (pe == null)
				return;
			//FocusedStream = pe.m_stream;
			IVwRootBox rootb = (IVwRootBox)pe.m_stream;
			Rect rcSrcRoot, rcDstRoot;
			GetCoordRectsForElt(page, pe, out rcSrcRoot, out rcDstRoot);

			// Convert to box coords and pass to root box (if any)
			if (rootb != null)
			{
				// For now at least we care only if the mouse is down.
				if (e.Button == MouseButtons.Left && rootb == FocusedRootBox)
				{
					// Enhance TE team (JohnT): see CallMouseMoveDrag in RootSite.cs for
					// how to auto-scroll if dragging outside element..needs reworking, here
					// root box does not occupy whole window.
					rootb.MouseMoveDrag(e.X, e.Y, rcSrcRoot, rcDstRoot);
				}
				// Enhance TE team (JohnT): possibly show tooltip if over external file link.
				// See RootSite method for a (small) start on how to do this.
			}

			EditingHelper.SetCursor(mousePos, rootb);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Process mouse button up event
		/// </summary>
		/// <param name="e"></param>
		/// -----------------------------------------------------------------------------------
		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);

			//			if (DataUpdateMonitor.IsUpdateInProgress(DataAccess))
			//				return;

			// Figure which stream and page and page element and get the appropriate
			// coordinate transformation.
			Point pt = new Point(e.X, e.Y);
			Point ptLayout;
			Page page;
			PageElement pe = ElementFromPoint(pt, ScrollPosition, out ptLayout, out page);
			if (pe == null)
				return;
			//FocusedStream = pe.m_stream;
			IVwRootBox rootb = (IVwRootBox)pe.m_stream;
			Rect rcSrcRoot, rcDstRoot;
			GetCoordRectsForElt(page, pe, out rcSrcRoot, out rcDstRoot);

			// Convert to box coords and pass to root box (if any).
			if (rootb != null && rootb == FocusedRootBox)
			{
				// Enhance TE Team: for autoscrolling, may need some extra behavior as in
				// SimpleRootSite.CallMouseUp.
				rootb.MouseUp(e.X, e.Y, rcSrcRoot, rcDstRoot);
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// We intercept WM_SETFOCUS in our WndProc and call this because we need the
		/// information about the previous focus window, which .NET does not provide.
		/// </summary>
		/// <param name="m"></param>
		/// -----------------------------------------------------------------------------------
		protected void OnSetFocus(Message m)
		{
			OnGotFocus(EventArgs.Empty);
			// A tricky problem is that the old hwnd may be the Keyman tray icon, in which the
			// user has just selected a keyboard. If we SetKeyboardForSelection in that case,
			// we change the current keyboard before we find out what the user selected.
			// In most other cases, we'd like to make sure the right keyboard is selected.
			// If the user switches back to this application from some other without changing the
			// focus in this application, we should already have the right keyboard, since the OS
			// maintains a keyboard selection for each application. If he clicks in a view to
			// switch back, he will make a selection, which results in a SetKeyboardForSelection call.
			// If he clicks in a non-view, eventually the focus will move back to the view, and
			// the previous window will belong to our process, so the following code will fire.
			// So, unless there's a case I (JohnT) haven't thought of, the following should set
			// the keyboard at least as often as we need to, and not in the one crucial case where
			// we must not.
			// (Note: setting a WS by setting the Keyman keyboard is still not working, because
			// it seems .NET applications don't get the notification from Keyman.)
			IntPtr hwndOld = m.WParam;
			int procIdOld, procIdThis;
			Win32.GetWindowThreadProcessId(hwndOld, out procIdOld);
			Win32.GetWindowThreadProcessId(Handle, out procIdThis);

			if (procIdOld == procIdThis && EditedRootBox != null && EditingHelper != null)
				EditingHelper.SetKeyboardForSelection(EditedRootBox.Selection);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle getting focus by allowing the appropriate root box to draw its selection.
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnGotFocus(EventArgs e)
		{
			base.OnGotFocus (e);
			if (FocusedRootBox != null)
				FocusedRootBox.Activate(VwSelectionState.vssEnabled); // Enable selection display.
			// Todo: much other stuff from RootSite.OnGotFocus, such as requesting language
			// change notification,...
		}

		///// ------------------------------------------------------------------------------------
		///// <summary>
		///// On losing focus, we may want to disable the focused root box. For now we do...
		///// compare with RootSite::OnLostFocus for more sophisticated idea. If the focus is
		///// just moving to a control, we may hide the selection only if it is an IP.
		///// That requires us to keep track of the root box that appears to have focus, though,
		///// and that in turn needs to be shared with RootSite.
		///// </summary>
		///// <param name="e"></param>
		///// ------------------------------------------------------------------------------------
		//protected override void OnLostFocus(EventArgs e)
		//{
		//    base.OnLostFocus (e);
		//    if (FocusedRootBox != null)
		//        (FocusedRootBox).Activate(VwSelectionState.vssDisabled);
		//    EditingHelper.LostFocus(null, true);
		//}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the focus is lost to another window.
		/// </summary>
		/// <param name="newWindow">The new window. Might be <c>null</c>.</param>
		/// <param name="fIsChildWindow"><c>true</c> if the <paramref name="newWindow"/> is
		/// a child window of the current application.</param>
		/// ------------------------------------------------------------------------------------
		protected virtual void OnKillFocus(Control newWindow, bool fIsChildWindow)
		{
			if (FocusedRootBox != null)
				(FocusedRootBox).Activate(GetNonFocusedSelectionState(newWindow));

			EditingHelper.LostFocus(newWindow, fIsChildWindow);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Unless overridden, this will return a value indicating that range selections
		/// should be hidden.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual VwSelectionState GetNonFocusedSelectionState(Control windowGainingFocus)
		{
			return VwSelectionState.vssDisabled;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// User pressed a key.
		/// </summary>
		/// <param name="e"></param>
		/// -----------------------------------------------------------------------------------
		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (FocusedRootBox == null)
				return;

			base.OnKeyDown(e);

			EditingHelper.OnKeyDown(e);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Handle a WM_CHAR message.
		/// </summary>
		/// <param name="e"></param>
		/// -----------------------------------------------------------------------------------
		protected override void OnKeyPress(KeyPressEventArgs e)
		{
			if (FocusedRootBox == null)
				return;
			if (DataUpdateMonitor.IsUpdateInProgress())
				return; //throw this event away

			base.OnKeyPress(e);

			EditingHelper.OnKeyPress(e, ModifierKeys);

			// We can't do this inside the try..finally, because it might destroy ourselves,
			// and dispose of the IVwGraphics object prematurely before the finally block.
			// See LT-7365.
			if (e.KeyChar == (char)Win32.VirtualKeycodes.VK_ESCAPE)
			{
				Form form = FindForm();
				if (form != null && form.CancelButton != null)
					form.CancelButton.PerformClick();
			}

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Override of UserControl.OnPaint
		/// </summary>
		/// <param name="e">Info needed to paint</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint (e);
			if (FindForm() == null)
				return;
			if (!FindForm().Enabled || m_fDisposingPages || !AllowPainting)
				return;

			if (m_fInPaint)
			{
				// Somehow, typically some odd side effect of expanding lazy boxes, we
				// got a recursive OnPaint call. This can produce messy results, even
				// crashes, if we let it proceed...in particular, we are probably in
				// the middle of expanding a lazy box, and if PrepareToDraw tries to do
				// further expansion it may conflict with the one already in process
				// with unpredictably disastrous results. So don't do the recursive paint...
				// on the other hand, the paint that is in progress may have been
				// messed up, so request another one.
				Debug.WriteLine("Recursive OnPaint call");
				return;
			}
			m_fInPaint = true;

			try
			{
				lock (m_pages)
				{
					//e.Graphics.FillRectangle(new SolidBrush(BackColor), e.ClipRectangle);
					if (m_pages.Count == 0)
						CreatePages();
					List<Page> pages = PrepareToDrawPages(e.ClipRectangle.Top,
						e.ClipRectangle.Bottom);
					if (pages != null)
					{
						foreach (Page page in pages)
						{
							Debug.Assert(!page.IsDisposed);
							// REVIEW (TimS): Is it OK to check for IsDisposed here? Don't we want
							// it to crash if we try to draw a disposed page as that would be part
							// of a bigger problem? TomB says: In the process of drawing pages,
							// if we have to lay anything out we can delete other pages, which
							// would cause them to be disposed, but not removed from this local
							// list of pages.
							if (!page.IsDisposed)
								page.Draw(e.Graphics, e.ClipRectangle, m_Zoom, true);
						}
					}
				}
			}
			finally
			{
				m_fInPaint = false;
			}
			//System.Threading.Thread.Sleep(1000);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// If we have pages, ignore the PaintBackground event because we do it ourself (in the
		/// views code).
		/// </summary>
		/// <param name="e"></param>
		/// <remarks>We don't want to do it here because that causes ugly flicker if we
		/// make a selection!</remarks>
		/// -----------------------------------------------------------------------------------
		protected override void OnPaintBackground(PaintEventArgs e)
		{
			lock (m_pages)
			{
				if (m_pages.Count == 0 || PageHeightPlusGapInScreenPixels * m_pages.Count < this.Height)
					e.Graphics.FillRectangle(new SolidBrush(SystemColors.Control), e.ClipRectangle);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Print a single page.
		/// </summary>
		/// <param name="e">PrintPage event args as received by the event</param>
		/// <param name="pageIndex">0-based page number to print</param>
		/// <returns>true if there was a page to print, otherwise false</returns>
		/// ------------------------------------------------------------------------------------
		public bool PrintPage(PrintPageEventArgs e, int pageIndex)
		{
			CheckDisposed();

			lock (m_pages)
			{
				float dpix = e.Graphics.DpiX;
				float dpiy = e.Graphics.DpiY;
				if (MiscUtils.IsUnix)
				{
					dpix = 72.0f;
					dpiy = 72.0f;
				}
				// layout to this page if needed
				LayoutToPageIfNeeded(pageIndex, dpix, dpiy);

				// make sure that the requested page exists
				if (pageIndex < 0 || m_pages.Count == 0 || pageIndex >= m_pages.Count)
					return false;

				// convert the page margin values to device coordinates.  This rect is used
				// to draw in.  The printer margin rect is in 100ths of an inch.
				Rectangle rect = new Rectangle((e.PageBounds.Left * (int)dpix) / 100,
					(e.PageBounds.Top * (int)dpiy) / 100,
					(e.PageBounds.Width * (int)dpix) / 100,
					(e.PageBounds.Height * (int)dpiy) / 100);

				// Draw the text for this page.
				m_pages[pageIndex].Draw(e.Graphics, rect, (float)1.0, false);
				return true;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Override of UserControl.OnSizeChanged. We adjust the zoom factor so that the
		/// page is exactly as wide as the window, which affects the on-screen length of a
		/// page and hence the autoscroll range (and possibly position? Do we need to do
		/// something to keep the same part of the document visible?)
		/// </summary>
		/// <param name="e">Info needed to paint</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged (e);
			SetZoomToPageWidth();
			Invalidate();
			MakeSureSelectionIsVisible();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Dispose of all pages and divisions (in that order) so that all rootboxes can be
		/// closed.
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnHandleDestroyed(EventArgs e)
		{
#if __MonoCS__
		// TODO-Linux: Work around for mono bug - yet to report and write test case.
		// nunit-console2 -labels PrintLayoutTests.dll –run=SIL.FieldWorks.Common.PrintLayout.LazyPrintLayoutTests.DeletePages
		// Circular loop causes stack overflow.
		return;
#else
			base.OnHandleDestroyed(e);
		Dispose();
#endif
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="e">An <see cref="T:System.EventArgs"/> that contains the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnVisibleChanged(EventArgs e)
		{
			base.OnVisibleChanged(e);
			if (Visible && m_pages != null && m_pages.Count > 0 && m_fRefreshPending)
				RefreshDisplay();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes Windows messages.
		/// </summary>
		/// <param name="msg">The Windows Message to process.</param>
		/// ------------------------------------------------------------------------------------
		protected override void WndProc(ref Message msg)
		{
			CheckDisposed();
			m_sequencer.SequenceWndProc(ref msg);
		}
		#endregion

		#region IReceiveSequentialMessages Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Needed by the interface, but not used
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		public void OriginalOnPaint(PaintEventArgs e)
		{
			Debug.Fail("This method should not be used");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the message sequencer
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public MessageSequencer Sequencer
		{
			get
			{
				CheckDisposed();
				return m_sequencer;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes Windows messages.
		/// </summary>
		/// <param name="msg">The Windows Message to process.</param>
		/// ------------------------------------------------------------------------------------
		public void OriginalWndProc(ref Message msg)
		{
			switch (msg.Msg)
			{
				case (int)Win32.WinMsgs.WM_SETFOCUS:
					OnSetFocus(msg);
#if __MonoCS__
					// TODO-Linux: See Comment in SimpleRootSite.cs:OriginalWndProc for WM_SETFOCUS
					base.WndProc(ref msg);
#endif
					return;
				case (int)Win32.WinMsgs.WM_KILLFOCUS:
					base.WndProc(ref msg);

					OnKillFocus(Control.FromHandle(msg.WParam),
						MiscUtils.IsChildWindowOfForm(ParentForm, msg.WParam));
					return;
				// Might need to handle more messages here...
			}
			base.WndProc(ref msg);
		}
		#endregion

		#region Other virtual methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Scrolls the current selection so it is visible in the view
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void MakeSureSelectionIsVisible()
		{
			try
			{
				DivisionLayoutMgr mgr = FocusedDivision as DivisionLayoutMgr;
				if (mgr != null && mgr.MainRootBox != null)
				{
					ScrollSelectionIntoView(mgr.MainRootBox.Selection,
						VwScrollSelOpts.kssoDefault);
				}
			}
			catch
			{
				// something went wrong... just ignore it.
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Set focus to our window
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private void SwitchFocusHere()
		{
			InvokeGotFocus(this, new EventArgs());
		}
		#endregion

		#region Abstract methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows the context menu. Derived classes must implement this method.
		/// </summary>
		/// <param name="Loc">The location where the menu will appear.</param>
		/// ------------------------------------------------------------------------------------
		public abstract void ShowContextMenu(Point Loc);
		#endregion

		#region Configuration methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Append a division to this publication.
		/// </summary>
		/// <param name="divLayoutMgr"></param>
		/// <remarks>No testing has been done to support multiple divisions yet</remarks>
		/// ------------------------------------------------------------------------------------
		public void AddDivision(DivisionLayoutMgr divLayoutMgr)
		{
			CheckDisposed();

			m_divisions.Add(divLayoutMgr);
			divLayoutMgr.Publication = this;

			// Configure the division only if the publication is already configured,
			// otherwise we will do it in PublicationControl.Configure().
			if (m_fConfigured)
				divLayoutMgr.Configure();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a shared substream.
		/// </summary>
		/// <param name="subStream">The substream.</param>
		/// ------------------------------------------------------------------------------------
		protected void AddSharedSubstream(IVwLayoutStream subStream)
		{
			(subStream as IVwRootBox).SetSite(this);
			m_sharedStreams.Add(subStream);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Configure the divisions.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void Configure()
		{
			if (DesignMode || m_fConfigured)
				return;

			// OPTIMIZE: may only need to configure each div when we actually need to paint it.
			foreach (DivisionLayoutMgr div in m_divisions)
				div.Configure();

			m_fConfigured = true;

			// Start with the main stream of the first division in focus so the selection
			// we made at the start of it shows.
			if(m_divisions.Count > 0)
				FocusedStream = m_divisions[0].MainLayoutStream;
		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the cache.
		/// </summary>
		/// <value>The cache.</value>
		/// ------------------------------------------------------------------------------------
		internal protected FdoCache Cache
		{
			get { return m_cache; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the font size (in millipoints) to be used when the publication doesn't specify
		/// it explicitly.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual int DefaultFontSize
		{
			get
			{
				CheckDisposed();
				return m_origStylesheet.NormalFontSize;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the line height (in millipoints) to be used when the publication doesn't
		/// specify it explicitly. (Value is negative for "exact" line spacing.)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual int DefaultLineHeight
		{
			get
			{
				CheckDisposed();
				return GetNormalLineHeight(m_origStylesheet);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the number of pages in the publication
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int PageCount
		{
			get
			{
				CheckDisposed();
				return m_pages.Count;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the printing date/time
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DateTime PrintDateTime
		{
			get
			{
				CheckDisposed();
				return m_printDateTime;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the location of the insertion point.
		/// </summary>
		/// <param name="rootbox">The rootbox.</param>
		/// <returns>
		/// location of selection in the given rootbox.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Point IPLocation(IVwRootBox rootbox)
		{
			CheckDisposed();
			Point pt = Point.Empty;

			if (rootbox != null && !DesignMode)
			{
				IVwSelection vwsel = rootbox.Selection;
				if (vwsel != null)
				{
					Rect rcSrcRoot, rcDstRoot;
					GetCoordRects(out rcSrcRoot, out rcDstRoot);

					Rect rcSec, rcPrimary;
					bool fSplit, fEndBeforeAnchor;
					try
					{
						vwsel.Location(ScreenGraphics, rcSrcRoot, rcDstRoot, out rcPrimary,
							out rcSec, out fSplit, out fEndBeforeAnchor);

						pt = new Point(rcPrimary.right, rcPrimary.bottom);
					}
					finally
					{
						UninitScreenGraphics();
					}
				}
			}

			return pt;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the stream that currently has focus: shows selection, keyboard input
		/// goes to it (if this control has focus at the system level).
		/// (Note: we were not able to devise a test for this function, as there is no way
		/// to retrieve the current value set by the Activate() method of RootBox.)
		/// Enhance: some of the functionality of RootSite.OnLostFocus may come here.
		/// If for some reason the focused stream is being set to null (will that ever happen?)
		/// we might not want to disable the old root box yet.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IVwLayoutStream FocusedStream
		{
			get
			{
				CheckDisposed();
				return m_focusedStream;
			}
			set
			{
				CheckDisposed();

				if (m_focusedStream == value)
					return;
				if (this.Focused && m_focusedStream != null)
					((IVwRootBox)m_focusedStream).Activate(VwSelectionState.vssDisabled);
				m_focusedStream = value;

				// In case the new rootbox doesn't have a selection we create one at the top
				IVwRootBox rootb = m_focusedStream as IVwRootBox;
				if (rootb != null && rootb.Selection == null)
				{
					try
					{
						rootb.MakeSimpleSel(true, false, false, true);
					}
					catch (Exception ex)
					{
						Debug.WriteLine("unable to create selection in rootbox: " +
							ex.StackTrace);
					}
				}

				if (this.Focused && rootb != null)
					rootb.Activate(VwSelectionState.vssEnabled);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the focused rootbox which can be accessed through the focused stream
		/// via its IVwRootBox interface.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IVwRootBox FocusedRootBox
		{
			get
			{
				CheckDisposed();
				return (IVwRootBox)m_focusedStream;
			}
			set
			{
				CheckDisposed();
				FocusedStream = value as IVwLayoutStream;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the division that currently has focus (i.e., the division of the
		/// <see cref="T:FocusedStream"/>)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IVwLayoutManager FocusedDivision
		{
			get
			{
				CheckDisposed();
				int iDiv = DivisionIndexForMainStream(FocusedStream);
				return iDiv >= 0 ? Divisions[iDiv] : null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the horizontal printer DPI setting
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected internal virtual float DpiXPrinter
		{
			get { return m_printerDpiX; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the vertical printer DPI setting
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected internal virtual float DpiYPrinter
		{
			get { return m_printerDpiY; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the horizontal screen DPI setting
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected internal float DpiXScreen
		{
			get { return m_screenDpiX; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the vertical screen DPI setting
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected internal float DpiYScreen
		{
			get { return m_screenDpiY; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a VwGraphics object suitable for measuring and drawing in this window.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IVwGraphics ScreenGraphics
		{
			get
			{
				CheckDisposed();

				InitScreenGraphics();
				return m_screenGraphics.VwGraphics;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a VwGraphics object suitable for measuring on and drawing to a printer.
		/// TODO JohnT: Eventually we need to distinguish between a VwGraphics suitable
		/// for layout measurements (typically in printer resolution) and one suitable
		/// for actually painting the window (in screen resolution).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IVwGraphics PrinterGraphics
		{
			get
			{
				CheckDisposed();

				InitPrinterGraphics();
				return m_printerGraphics.VwGraphics;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the list of divisions in this publication
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected internal List<DivisionLayoutMgr> Divisions
		{
			get
			{
				return m_divisions;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets and sets the height of the page in millipoints.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int PageHeight
		{
			get
			{
				CheckDisposed();

				return m_dympPageHeight;
			}
			set
			{
				CheckDisposed();

				m_dympPageHeight = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the height of the page in printer pixels.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int PageHeightInPrinterPixels
		{
			get
			{
				CheckDisposed();

				// Multiply before dividing to reduce rounding errors.
				return (int)(PageHeight * DpiYPrinter / MiscUtils.kdzmpInch);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the actual height currently occupied by a page, including gap,
		/// but allowing for the current zoom level, in actual screen pixels.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int PageHeightPlusGapInScreenPixels
		{
			get
			{
				CheckDisposed();

				float pageHeightInInches = (float)PageHeight / MiscUtils.kdzmpInch;
				return (int)(pageHeightInInches * Zoom * DpiYScreen) + Gap;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets and sets the width of the page in millipoints.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int PageWidth
		{
			get
			{
				CheckDisposed();

				return m_dxmpPageWidth;
			}
			set
			{
				CheckDisposed();

				m_dxmpPageWidth = value;
				SetZoomToPageWidth();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the width of the page in printer pixels.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int PageWidthInPrinterPixels
		{
			get
			{
				CheckDisposed();

				// Multiply before dividing to reduce rounding errors.
				return (int)(m_dxmpPageWidth * DpiXPrinter / MiscUtils.kdzmpInch);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the zoom multiplier that magnifies (or shrinks) the view.
		/// Review TomB(JohnT): should this be public? Or is it determined by the window width
		/// as compared to the page width?
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public float Zoom
		{
			get
			{
				CheckDisposed();
				return m_Zoom;
			}
			set
			{
				CheckDisposed();

				m_Zoom = value;
				//RefreshDisplay(); Todo: implement and call.
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the collection of pages in the publication. This list may include pages
		/// that have not been laid out and may not represent the exact number of pages needed
		/// to lay out the entire publication.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public List<Page> Pages
		{
			get
			{
				CheckDisposed();

				return m_pages;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the gap between pages, in screen pixels (this is the only type of gap that
		/// matters, since we don't draw the gap on the printer).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int Gap
		{
			get
			{
				CheckDisposed();

				return 4;
			}
			// ENHANCE: Add a setter and UI, and persist this.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets and sets a value that tells whether this publication is left-bound or not.
		/// TODO: Probably need to re-layout everything if this changes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsLeftBound
		{
			get
			{
				CheckDisposed();
				return m_publication.IsLeftBound;
			}
			set
			{
				CheckDisposed();
				m_publication.IsLeftBound = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value that indicates the multiple-page option for this publication.
		/// ENHANCE: When we add UI to allow the user to set this, we probably need to re-layout
		/// everything if this changes after we're already configured.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual MultiPageLayout PageLayoutMode
		{
			get
			{
				CheckDisposed();
				return m_publication.SheetLayout;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets an instance of the EditingHelper used to process editing requests.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public EditingHelper EditingHelper
		{
			get
			{
				CheckDisposed();

				if (m_editingHelper == null)
					m_editingHelper = new PubEditingHelper(GetInternalEditingHelper(), Cache, this);
				return m_editingHelper;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets an instance of the EditingHelper used to process editing requests.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public RootSiteEditingHelper RootSiteEditingHelper
		{
			get { CheckDisposed(); return EditingHelper as RootSiteEditingHelper; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets publication for this control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IPublication Publication
		{
			get
			{
				CheckDisposed();
				return m_publication;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the internal editing helper.
		/// </summary>
		/// <returns></returns>
		/// <remarks>If subclasses want to use a different editing helper, they should return
		/// it here. The default implementation returns <c>null</c> which causes the default
		/// implementation of EditingHelper to be used.</remarks>
		/// ------------------------------------------------------------------------------------
		protected virtual RootSiteEditingHelper GetInternalEditingHelper()
		{
			return null;
		}
		#endregion

		#region Protected page-layout methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates as many pages as we think we might need to layout the entire publication
		/// (all divisions).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void CreatePages()
		{
			// if pages already defined, clear them and close the streams for them.
			DisposePages();

			Debug.WriteLine(string.Format("Creating pages for {0} divisions (printer dpiY={1})", m_divisions.Count, DpiYPrinter));
			int dydSpaceLeft = 0;
			for (int iDiv = 0; iDiv < m_divisions.Count; iDiv++)
				dydSpaceLeft = CreatePagesForDiv(m_divisions[iDiv], iDiv, dydSpaceLeft);

			Debug.WriteLine("Space left: " + dydSpaceLeft);
			AdjustAutoScrollRange();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates as many pages as we think we might need to lay out the entire publication
		/// (for one division).
		/// </summary>
		/// <param name="div">The div.</param>
		/// <param name="iDiv">Index of division</param>
		/// <param name="spaceLeftFromPrevDiv">The (estimated) space left on the last page of
		/// the previous division. If the new division is continous and there is space left we
		/// should use up that space before we add a new page.</param>
		/// <returns>The space left on the bottom of the page for this division.</returns>
		/// <remarks>Since the heights we're using here are estimated, the offset of a division
		/// on a page very much depends on good estimates. The reason we care about the start
		/// position is that if we do a re-layout we want to be as close as possible to the
		/// previous layed out value, otherwise if we're in the second section that starts in
		/// the middle of the page, and we do a refresh we end up with the second section
		/// starting at the top of the page.</remarks>
		/// ------------------------------------------------------------------------------------
		protected int CreatePagesForDiv(DivisionLayoutMgr div, int iDiv,
			int spaceLeftFromPrevDiv)
		{
			lock (m_pages)
			{
				int pageNumber = m_pages.Count + 1;
				Debug.WriteLine(string.Format("Creating page {0} for division {1}, spaceLeftFromPrevDiv={2}", pageNumber, iDiv, spaceLeftFromPrevDiv));

				int dxpLayoutWidth = div.AvailableMainStreamColumWidthInPrinterPixels;
				int dypHeightOfDiv = div.EstimateHeight(dxpLayoutWidth);

				// If a division wants to start on a new page or if it doesn't share the
				// (footnote) substream then we have to start at the top of the new page.
				// REVIEW (EberhardB): This might not work if we have any other kind of
				// substream.
				if (div.StartAt != DivisionStartOption.Continuous || div.HasOwnedSubStreams)
					spaceLeftFromPrevDiv = 0;

				int dypLayoutHeight = div.AvailablePageHeightInPrinterPixels;
				int numberOfPages = PagesPerDiv(dypHeightOfDiv - spaceLeftFromPrevDiv, dypLayoutHeight);
				Debug.WriteLine(string.Format("dxpLayoutWidth={0}, dypHeightOfDiv={1},\nspaceLeftFromPrevDiv={2}, dypLayoutHeight={3},\nnumberOfPages={4}", dxpLayoutWidth, dypHeightOfDiv, spaceLeftFromPrevDiv, dypLayoutHeight, numberOfPages));
				Debug.WriteLine(string.Format("div.TopMargin={0}, div.BottomMargin={1}", div.TopMarginInPrinterPixels, div.BottomMarginInPrinterPixels));
				for (int i = 0; i < numberOfPages; i++)
					m_pages.Add(CreatePage(this, iDiv, spaceLeftFromPrevDiv + i * dypLayoutHeight,
						pageNumber++, div.TopMarginInPrinterPixels, div.BottomMarginInPrinterPixels));

				return SpaceLeftOnLastPageForDiv(dypHeightOfDiv, dypLayoutHeight);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the page.
		/// </summary>
		/// <param name="pub">Publication that owns this page</param>
		/// <param name="iFirstDivOnPage">Index (into array of divisions in the publication) of
		/// the first division expected to lay out on this page</param>
		/// <param name="ypOffsetFromTopOfDiv">Estimated number of pixels (in source/layout
		/// units) from the top of the division to the top of this page</param>
		/// <param name="pageNumber">The page number for this page. Page numbers can restart for
		/// different divisions, so this should not be regarded as an index into an array of
		/// pages.</param>
		/// <param name="dypTopMarginInPrinterPixels">The top margin in printer pixels.</param>
		/// <param name="dypBottomMarginInPrinterPixels">The bottom margin in printer pixels.
		/// </param>
		/// <returns>The new page</returns>
		/// <remarks>We do this in a virtual method so that tests can create special pages.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		protected virtual Page CreatePage(PublicationControl pub, int iFirstDivOnPage,
			int ypOffsetFromTopOfDiv, int pageNumber, int dypTopMarginInPrinterPixels,
			int dypBottomMarginInPrinterPixels)
		{
			return new Page(pub, iFirstDivOnPage, ypOffsetFromTopOfDiv, pageNumber,
				dypTopMarginInPrinterPixels, dypBottomMarginInPrinterPixels);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Calculate the number of pages in this division
		/// </summary>
		/// <param name="dypHeightOfDiv">The height of the division</param>
		/// <param name="dypLayoutHeight">The height of one page</param>
		/// <returns>Number of pages</returns>
		/// ------------------------------------------------------------------------------------
		private int PagesPerDiv(int dypHeightOfDiv, int dypLayoutHeight)
		{
			return (int)Math.Round((double)dypHeightOfDiv / dypLayoutHeight + .5,
				MidpointRounding.AwayFromZero);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Calculate the space left on the last page for this division.
		/// </summary>
		/// <param name="dypHeightOfDiv">The height of the division</param>
		/// <param name="dypLayoutHeight">The height of one page</param>
		/// <returns>Space left</returns>
		/// ------------------------------------------------------------------------------------
		private int SpaceLeftOnLastPageForDiv(int dypHeightOfDiv, int dypLayoutHeight)
		{
			return dypLayoutHeight - (int)Math.Round((double)dypHeightOfDiv % dypLayoutHeight + .5,
				MidpointRounding.AwayFromZero);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Figure out what is going to be placed on each page and where, for the given area.
		/// </summary>
		/// <param name="dypTop">offset in screen pixels to the top of the rectangle that is
		/// about to be laid out (relative to the client rectangle top)</param>
		/// <param name="dypBottom">offset in screen pixels to the bottom of the rectangle that
		/// is about to be laid out (relative to the client rectangle top)</param>
		/// <returns>A list of pages to be drawn</returns>
		/// ------------------------------------------------------------------------------------
		public List<Page> PrepareToDrawPages(int dypTop, int dypBottom)
		{
			CheckDisposed();

			if (dypBottom <= dypTop || PageHeight <= 0 || m_pages.Count == 0)
				return null;

			lock (m_pages)
			{
				int dypPageHeight = PageHeightPlusGapInScreenPixels;
				Debug.WriteLine("dyPageHeight=" + dypPageHeight);

				List<Page> pages = new List<Page>();

				// Before we start laying out the pages in detail, make sure there are real boxes
				// at least at the top of the area needing to be laid out.
				// Note that side effects of this (happening in the AdjustScrollPosition callback
				// to DivisionLayoutMgr) may change the collection of pages and/or the scroll
				// position. If that happens, the method returns true, and we need to recompute
				// which is the first page we want to lay out, and make sure that we don't have any
				// lazy boxes at that position. Any time that EnsureRealBoxAt returns true, something
				// has been converted from lazy to real, so the process is sure to terminate.
				while ((dypTop - ScrollPosition.Y) / dypPageHeight < m_pages.Count &&
					EnsureRealBoxAt(m_pages[(dypTop - ScrollPosition.Y) / dypPageHeight]))
					;

				// We need to call LayoutIfNeeded for any page that intersects the client
				// rectangle, not just the clip rectangle. This is because, in the case of 'broken'
				// pages that have been messed up by editing in the view, the Layout method of the
				// page may actually detect additional screen areas requiring redrawing.
				// On the other hand, if the clip rectangle is larger than the client rectangle,
				// we need to use those dimensions...mainly because we pass large rectangles
				// for testing purposes.
				dypTop = 0;
				dypBottom = Math.Max(dypBottom, this.ClientRectangle.Bottom);

				// The last page to be laid out will be the last page whose top is less than
				// the bottom of the rectangle being painted.
				// Note: do not calculate limit allowing for m_pages.Count, as that may increase
				// during the process of laying out pages.
				int limPage = (dypBottom - ScrollPosition.Y + dypPageHeight - 1) / dypPageHeight;
				Debug.WriteLine(string.Format("dypBottom={0}, limPage={1}, startPage={2}, ScrollPos.Y={3}, pages.Count={4}",
					dypBottom, limPage, (dypTop - ScrollPosition.Y) / (dypPageHeight), ScrollPosition.Y, m_pages.Count));

				// The first page to be laid out will be the first page whose bottom is greater than
				// or equal to the top of the rectangle being painted.
				for (int iPage = (dypTop - ScrollPosition.Y) / (dypPageHeight);
					iPage < limPage && iPage < m_pages.Count;
					iPage++)
				{
					// We need to store the page in a temp variable because it is, theoretically,
					// possible that something else like ScrollSelectionIntoView() might get
					// called during the layout (probably another problem) and set
					// m_pageBeingLaidOut to null before we add it to the page list.
					Page page = m_pageBeingLaidOut = m_pages[iPage];
					Debug.WriteLine(string.Format("Laying out page {0} (iPage={1})", m_pageBeingLaidOut.PageNumber, iPage));
					page.LayOutIfNeeded();
//					page.ReadyForDrawing = true;
					pages.Add(page);
				}
				m_pageBeingLaidOut = null;
				Debug.WriteLine("Done preparing pages to draw");

				return pages;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Ensure that the data corresponding to the location of the given page consists of
		/// real boxes, not lazy ones.
		/// </summary>
		/// <param name="page"></param>
		/// <returns>true if it had to do anything...the caller should recheck the scroll
		/// position and which pages need drawing, and call again.</returns>
		/// ------------------------------------------------------------------------------------
		public bool EnsureRealBoxAt(Page page)
		{
			CheckDisposed();

			bool result = false;
			DivisionLayoutMgr div = m_divisions[page.FirstDivOnPage];
			// ENHANCE (TE-5866): Do this for all divisions on the page.

			IVwGraphics vg = PrinterGraphics;
			try
			{
				int offset = page.OffsetFromTopOfDiv(div);
				Rect rcPage = new Rect(0, offset, PageWidthInPrinterPixels,
					offset + PageHeightInPrinterPixels);
				((IVwGraphicsWin32)vg).SetClipRect(ref rcPage);
				Rect rcTrans = new Rect(0, 0, (int)DpiXPrinter, (int)DpiYPrinter);
				// Make sure we have real data in this section of the root.
				if (div.MainRootBox != null)
				{
					result = div.MainRootBox.PrepareToDraw(vg, rcTrans, rcTrans) !=
						VwPrepDrawResult.kxpdrNormal;
				}
			}
			finally
			{
				ReleaseGraphics(vg);
			}
			return result;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure all of the pages are laid out to and including the given page.
		/// </summary>
		/// <param name="pageIndex">page index to layout through</param>
		/// <param name="dpiX">The X dpi</param>
		/// <param name="dpiY">The Y dpi</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected internal bool LayoutToPageIfNeeded(int pageIndex, float dpiX, float dpiY)
		{
			return LayoutToPageIfNeeded(pageIndex, dpiX, dpiY, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure all of the pages are laid out to and including the given page.
		/// </summary>
		/// <param name="pageIndex">page index to layout through</param>
		/// <param name="dpiX">The X dpi</param>
		/// <param name="dpiY">The Y dpi</param>
		/// <param name="dlg">Progress dialog to display the progress of the layout, null
		/// to not display one.</param>
		/// <returns>
		/// True if it did a layout of a page, false otherwise
		/// </returns>
		/// ------------------------------------------------------------------------------------
		protected bool LayoutToPageIfNeeded(int pageIndex, float dpiX, float dpiY, IThreadedProgress dlg)
		{
			lock (m_pages)
			{
				if (m_pages.Count == 0 || m_printerDpiX != dpiX || m_printerDpiY != dpiY)
				{
					m_printerDpiX = dpiX;
					m_printerDpiY = dpiY;
					CreatePages();
				}
			}

			// ENHANCE (TE-5651; EberhardB): right now we lay out everything up to the desired
			// page. This might take a loooong time if you have a large project. Now that we have
			// multiple divisions (and each book starting on a new page) it should be possible
			// to start with the page that starts the division that is on the desired page,
			// or go back through the divisions and start with the page that starts a
			// division that begins on a new page.
			bool fDidLayout = false;
			for (int iPage = 0; (iPage <= pageIndex) && (iPage < m_pages.Count); iPage++)
			{
				if (dlg != null)
				{
					dlg.Minimum = 0;
					dlg.Maximum = m_pages.Count;
					dlg.Message = string.Format(
						ResourceHelper.GetResourceString("kstidUpdatePageBreakPageCount"),
						iPage + 1, m_pages.Count);
				}

				// To be completely thread safe, we need to check the page count again
				// just in case the count changed while the lock was released.
				if (iPage < m_pages.Count && m_pages[iPage].LayOutIfNeeded())
				{
					if (dlg != null)
					{
						dlg.Position = iPage + 1;
						//Debug.WriteLine(string.Format("Page {0} of {1}", dlg.Value, dlg.Maximum));
						if (dlg.Canceled)
							break;
					}
					fDidLayout = true;
				}
			}

			return fDidLayout;
		}
		#endregion

		#region Other non-virtual methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the accessible object from the root box (implements IAccessible)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static object AccessibleRootObject(IVwLayoutStream stream)
		{
			if (stream == null)
			{
				return null;
			}
			object obj = null;
#if !__MonoCS__
			if (stream is IOleServiceProvider)
			{
				IOleServiceProvider sp = (IOleServiceProvider)stream;
				Debug.Assert(sp != null);
				Guid guidAcc = Marshal.GenerateGuidForType(typeof(IAccessible));
				// 1st guid currently ignored.
				sp.QueryService(ref guidAcc, ref guidAcc, out obj);
			}
#else
			// TODO-Linux: for a VwLayoutStream, stream is IOleServiceProvider is returning true while QueryInterface (cast) throws exceptions a VwLayoutStream
			// QueryInterface doesn't report that its a VwLayoutStream.
#endif
			return obj;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the accessible name of the stream.
		/// </summary>
		/// <param name="stream">The stream.</param>
		/// <param name="name">The name.</param>
		/// ------------------------------------------------------------------------------------
		public static void SetAccessibleStreamName(IVwLayoutStream stream, string name)
		{
			IAccessible acc = AccessibleRootObject(stream) as IAccessible;
			if (acc != null)
				acc.set_accName(null, name);
		}

		/// <summary>
		/// Attempt to find the page for a location quickly, if it is the
		/// main stream for some division and the location is within one of the
		/// elements for that division.
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="fUp"></param>
		/// <param name="strm"></param>
		/// <param name="iDiv"></param>
		/// <returns></returns>
		private Page PossiblePageFromPrinter(int x, int y, bool fUp, IVwLayoutStream strm, out int iDiv)
		{
			iDiv = DivisionIndexForMainStream(strm);
			if (iDiv < 0 || Pages == null || Pages.Count == 0)
				return null; // can't find it this way unless it's a main stream and we have pages.

			DivisionLayoutMgr divMgr = Divisions[iDiv];
			int divOffset;
			for (int i = 0; i < Pages.Count; i++)
			{
				Page page = Pages[i];
				if (page.FirstDivOnPage > iDiv)
				{
					// No more pages of the required division.
					// If there is a previous page, it might be on there, otherwise we won't find it.
					if (i > 0)
						return Pages[i - 1];
					else
						return null;
				}
				PageElement firstPe;
				divOffset = page.OffsetFromTopOfDiv(strm, divMgr, out firstPe);
				if (y <= divOffset)
				{
					// We've found the first page of this division which starts at or beyond y.
					// Typically we want the previous page, since this one starts after y.
					// If there is no previous page return this one.
					if (i == 0)
						return page;
					// If we're exactly on the boundary and preferring down, we want this one;
					if (y == divOffset && !fUp)
						return page;
					// If we're in the overlap region we need to investigate. If it's actually on
					// this page we want this one.
					if (firstPe != null && y > divOffset - firstPe.OverlapWithPreviousElement)
					{
						Point locOnPage;
						if (page.GetPositionOnPage(strm, new Point(x, y), out locOnPage))
							return page;
					}
					return Pages[i - 1];
				}
			}

			// If we didn't find a page, see if the position is on the last page
			return Pages[Pages.Count - 1];
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Given a point in printer(layout) pixels relative to the top left of the stream,
		/// determine which page it occurs on, and return that page, along with the
		/// distance from the top of the page to the point. If the page does not currently
		/// contain valid elements, this may be somewhat approximate.
		/// </summary>
		/// <param name="x">X coord, important where pages overlap</param>
		/// <param name="y">Y coordinate (in printer pixels), from the top of the stream.</param>
		/// <param name="fUp">If the point is right at a page boundary, return the first
		/// page if this is true, the second if it is false.</param>
		/// <param name="strm">The stream that contains the y position.</param>
		/// <param name="dyPageScreen">Distance in screen coords from top of page to
		/// the specified location.</param>
		/// <param name="iDiv">Index of the division at the specified location.</param>
		/// <param name="layedOutPage">Flag returned as <c>true</c> if a page was layed out during
		/// this call</param>
		/// <returns>The page that contains the y-position, or <c>null</c>.</returns>
		/// ------------------------------------------------------------------------------------
		protected internal Page PageFromPrinterY(int x, int y, bool fUp, IVwLayoutStream strm,
			out int dyPageScreen, out int iDiv, out bool layedOutPage)
		{
			layedOutPage = false;
			Page possiblePage = PossiblePageFromPrinter(x, y, fUp, strm, out iDiv);
			if (possiblePage != null)
			{
				Point locOnPage;
				if (possiblePage.GetPositionOnPage(strm, new Point(x, y), out locOnPage))
				{
					dyPageScreen = ConvertPrintEltOffsetToPixelsY(locOnPage.Y, DpiYScreen);
					return possiblePage;
				}
			}

			// footnotes aren't in page element stream, no use looking for it
			if (strm == null)
			{
				dyPageScreen = 0; // satisfy compiler
				return null;
			}

			// If it's not the main stream or we didn't find it the quick way, all we can
			// do is look for a hopeful element. Since the thing might well be on even a
			// broken page, for this we consider even broken pages...to make things more
			// accurate we fix any broken page.
			if (Pages != null && Pages.Count > 0)
			{
				bool layedOutSomething;
				bool foundPageOnStream = false;
				do
				{
					layedOutSomething = false;
					foreach (Page page in Pages)
					{
						if (page.LayOutIfNeeded())
						{
							layedOutSomething = true;
							layedOutPage = true;
							break;
						}
						bool foundElemOnStream = false;
						foreach (PageElement pe in page.PageElements)
						{
							if (pe.m_stream != strm)
								continue;
							foundPageOnStream = true;
							foundElemOnStream = true;
							// If y is in the range for this page element, we probably stop here...
							if (y >= pe.OffsetToTopPageBoundary - pe.OverlapWithPreviousElement &&
								y <= pe.OffsetToTopPageBoundary + pe.LocationOnPage.Height)
							{
								if (y < pe.OffsetToTopPageBoundary && !pe.IsInOurPartOfOverlap(new Point(x, y)))
									continue;
								// Except, if we're exactly at the bottom and want to be at the top,
								// keep trying. (Enhance JohnT: not sure we can depend on fUp any more,
								// now we have overlap...)
								if ((!fUp) && y == pe.OffsetToTopPageBoundary + pe.LocationOnPage.Height)
									continue;
								// Also, in case the element might be out of order, if we we're
								// exactly at the top and want to be at the bottom, keep trying
								if (fUp && y <= pe.OffsetToTopPageBoundary - pe.OverlapWithPreviousElement)
									continue;

								int yOnPage = y - pe.OffsetToTopPageBoundary - pe.OverlapWithPreviousElement + pe.LocationOnPage.Top;

								dyPageScreen = ConvertPrintEltOffsetToPixelsY(yOnPage, DpiYScreen);
								return page;
							}
							else if (y < pe.OffsetToTopPageBoundary)
							{
								// have gone too far, can end the search. Y must be on
								// a page that hasn't been layed out.
								dyPageScreen = 0;	// satisfy compiler
								return null;
							}
						}
						// another check to see that we've gone too far
						if (foundPageOnStream && !foundElemOnStream)
						{
							dyPageScreen = 0; // satisfy compiler
							return null;
						}
					}
				}
				while (layedOutSomething);
			}
			dyPageScreen = 0;	// satisfy compiler
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// dx is a distance from the left of the page element in printer pixels.
		/// convert it (the same way view rendering does) to an offset in screen pixels
		/// from the left of the page.
		/// </summary>
		/// <param name="dx">The dx.</param>
		/// <param name="dpiXf">actual dpi from the Graphics object</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private int ConvertPrintEltOffsetToPixelsX(int dx, float dpiXf)
		{
			int dpiX = (int)(dpiXf * Zoom);
			return dx * dpiX / (int)DpiXPrinter;
			//return dxScreen + ConvertPrintDistanceToTargetX(pe.LocationOnPage.Left, dpiXf);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// dy is a distance from the top of the page element in printer pixels.
		/// convert it (the same way view rendering does) to an offset in screen pixels
		/// from the top of the page element.
		/// </summary>
		/// <param name="dy">The dy.</param>
		/// <param name="dpiYf">The dpi yf.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private int ConvertPrintEltOffsetToPixelsY(int dy, float dpiYf)
		{
			int dpiY = (int)(dpiYf * Zoom);
			return dy * dpiY / (int)DpiYPrinter;
			//return dYScreen + ConvertPrintDistanceToTargetY(pe.LocationOnPage.Top, dpiYf);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the index of the division for the given stream
		/// </summary>
		/// <returns>The index of the division for the given stream if it is the main stream
		/// for any division in this publication; Otherwise -1.</returns>
		/// ------------------------------------------------------------------------------------
		public int DivisionIndexForMainStream(IVwLayoutStream stream)
		{
			CheckDisposed();

			if (stream == null)
				return -1;

			for (int i = 0; i < m_divisions.Count; i++)
			{
				if (m_divisions[i].IsManagerForStream(stream))
					return i;
			}
			return -1;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserts a new page after the given one.
		/// </summary>
		/// <param name="indexOfDiv">The index of this division in the publication</param>
		/// <param name="ypOffsetFromTopOfDiv">The offset from top of this division to the
		/// start of the page to insert.</param>
		/// <param name="pageToInsertAfter">The page to insert after.</param>
		/// <returns>The newly inserted page</returns>
		/// ------------------------------------------------------------------------------------
		internal Page InsertPage(int indexOfDiv, int ypOffsetFromTopOfDiv, Page pageToInsertAfter)
		{
			DivisionLayoutMgr div = Divisions[indexOfDiv];
			Page newPage = CreatePage(this, indexOfDiv, ypOffsetFromTopOfDiv,
				pageToInsertAfter.PageNumber + 1, div.TopMarginInPrinterPixels,
				div.BottomMarginInPrinterPixels);
			InsertPageAfter(pageToInsertAfter, newPage);
			return newPage;
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Inserts additonal pages if needed at dydPosition to approximately match the new
		/// estimated height when a lazy box has been expanded.
		/// </summary>
		/// <param name="changedSizePage">The last page whose top doesn't move (i.e., the one the
		/// lazy box is actually on). We may need to insert pages just after this.</param>
		/// <param name="strm">The IVwLayoutStream interface of the rootbox that is the main
		/// stream on at least some of the pages of this publication (i.e., this publication
		/// should be the IVwRootsite of the given rootbox).</param>
		/// <param name="dydSize">The amount the rootbox grew, in printer pixels. This is
		/// guaranteed to be positive.</param>
		/// <param name="dydPosition">The top of the lazy box which was (partially) expanded,
		/// split, etc (in printer pixels). Or the position of the top of the real box that got
		/// relazified.
		/// </param>
		/// <returns>true if there are not enough pages for the AutoScrollPosition to increase as
		/// much as required.</returns>
		/// -------------------------------------------------------------------------------------
		private bool InsertAdditionalPages(Page changedSizePage, IVwLayoutStream strm,
			int dydSize, int dydPosition)
		{
			Debug.Assert(changedSizePage != null);
			Debug.Assert(dydSize > 0);

			Page nextPage = PageAfter(changedSizePage);
			int heightOfChangedPage; // Current height, to next page or estimated end of doc.
			int iDiv = DivisionIndexForMainStream(strm);
			DivisionLayoutMgr divMgr = Divisions[iDiv];

			if (nextPage == null)
			{
				heightOfChangedPage = dydSize;
				// An empty page may have nothing from the main stream - can happen when project is
				// empty or filter excludes all content
				PageElement pe = changedSizePage.GetFirstElementForStream(strm);
				if (pe != null)
					heightOfChangedPage -= pe.OffsetToTopPageBoundary;
			}
			else
			{
				heightOfChangedPage = nextPage.OffsetFromTopOfDiv(divMgr) -
					changedSizePage.OffsetFromTopOfDiv(divMgr);
			}

			int desiredPageHeight = divMgr.AvailablePageHeightInPrinterPixels;
			// This computes the number of pages to add, if any, so that
			// if the tops of the pages are more than a page and a half apart we
			// insert enough pages so no page is more than an ideal page and a half long.
			// Subtracting half the desired page height accounts both for rounding and
			// the fact that one page (rather than zero) is the desired page height.
			int numberOfPagesToAdd = (heightOfChangedPage - desiredPageHeight / 2) /
				desiredPageHeight;
			// Pathologically, it might be possible for that calculation to produce
			// a negative number, if this page has shrunk to less than a half page from
			// previous lazy box expansions.
			if (numberOfPagesToAdd < 0)
				numberOfPagesToAdd = 0;
			Page pageToInsertAfter = changedSizePage;
			for (int i = 0; i < numberOfPagesToAdd; i++)
			{
				// Insert a new page.
				pageToInsertAfter = InsertPage(iDiv,
					pageToInsertAfter.OffsetFromTopOfDiv(divMgr) + desiredPageHeight,
					pageToInsertAfter);
			}
			// If we're increasing the autoscroll position, do it AFTER we possibly adjust
			// the autoscroll range, otherwise, the increased value might be greater than
			// the old range and get truncated.
			// Remember: Microsoft's idea of how to set AutoScrollPosition is brain-dead. We read
			// it as negative and have to set it as positive.
			int dydPosScreen = (int)(dydPosition * DpiYScreen / DpiYPrinter);
			if (-AutoScrollPosition.Y > dydPosScreen)
			{
				int dydDesiredAutoScrollY = (int)(AutoScrollPosition.Y - (dydSize * DpiYScreen /
					DpiYPrinter)); // larger negative
				AutoScrollPosition = new Point(
					-AutoScrollPosition.X, -dydDesiredAutoScrollY);
				// Something messy happened if we can't achieve that position (presumably
				// it is out of range, we didn't add enough pages).
				return (AutoScrollPosition.Y != dydDesiredAutoScrollY);
			}
			return false; // We get here only if we didn't have to adjust scroll position at all.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Delete any extra pages no longer needed after expansion of a lazy box resulted in
		/// a smaller document size (requiring fewer total pages)
		/// </summary>
		/// <param name="dydSize">The amount the rootbox grew or shrunk, in printer pixels. This
		/// is guaranteed to be negative.</param>
		/// <param name="dydPosition">The top of the lazy box which was (partially) expanded,
		/// shrunk, split, etc (in printer pixels). Or the position of the top of the real box
		/// that got relazified.
		/// </param>
		/// <returns>true if deletion of pages forced AutoScrollPosition to decrease by MORE
		/// than the amount caused by adding dydSize</returns>
		/// ------------------------------------------------------------------------------------
		private bool DeleteExtraPages(int dydSize, int dydPosition)
		{
			Debug.Assert(dydSize < 0);
			// The position in screen pixels
			int dydPosScreen = (int)(dydPosition * DpiYScreen / DpiYPrinter);
			// If we're reducing the autoscroll position, do it BEFORE we possibly adjust
			// the autoscroll range, otherwise, reducing the range might also reduce the position.
			// Remember: Microsoft's idea of how to set AutoScrollPosition is brain-dead. We read
			// it as negative and have to set it as positive.
			int ydAdjustedAutoScrollY = -AutoScrollPosition.Y;
			if (ydAdjustedAutoScrollY > dydPosScreen)
			{
				// The size in screen pixels
				int dydSizeScreen = (int)(dydSize * DpiYScreen / DpiYPrinter);
				// if the size of the stream shrunk (dydSize < 0) and the autoscrollposition is
				// inside the deleted range, then we want to set the autoscrollposition to the
				// position where the change happened. If autoscrollposition is below the deleted
				// range or the size increased, we adjust the scroll position according to the
				// size change.
				if (ydAdjustedAutoScrollY <= dydPosScreen - dydSizeScreen)
					ydAdjustedAutoScrollY = dydPosScreen;
				else
					ydAdjustedAutoScrollY += dydSizeScreen;

				AutoScrollPosition = new Point(
					-AutoScrollPosition.X, ydAdjustedAutoScrollY);
			}
			// Delete pages that are out of order (i.e., any page whose top is less than the top of
			// the previous page). (This shouldn't include any 'real' pages, because those are
			// entirely made up of non-lazy boxes.)
			int ydTopPrevPage = 0; // OK as initial state as no page has offset less than 0.
			int firstDivOnPrevPage = -1;
			ArrayList pagesToDelete = new ArrayList();
			foreach (Page page in Pages)
			{
				// If the page is out of order delete it.
				if (page.FirstDivOnPage < firstDivOnPrevPage)
				{
					pagesToDelete.Add(page);
				}
				else
				{
					int offset = page.OffsetFromTopOfDiv(Divisions[page.FirstDivOnPage]);
					if (page.FirstDivOnPage > firstDivOnPrevPage)
					{
						firstDivOnPrevPage = page.FirstDivOnPage;
						ydTopPrevPage = offset;
					}
					else // page.FirstDivOnPage == firstDivOnPrevPage
					{
						if (offset < ydTopPrevPage)
							pagesToDelete.Add(page);
						// Now we're done with this page, remember its position and first division
						// for the next iteration.
						ydTopPrevPage = Math.Max(ydTopPrevPage, offset);
					}
				}
			}


			foreach (Page page in pagesToDelete)
				DeletePage(page);
			// Something nasty happened if the current ASP is not what it was after we had
			// (possibly) adjusted it at the start of the loop. Perhaps we were trying to
			// scroll to a page near the end of the document and that page got deleted when
			// we expanded a lazy box and got something much smaller than expected. If that
			// happens, return false as a warning to the caller.
			return AutoScrollPosition.Y == ydAdjustedAutoScrollY;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the info for the Publication from the DB
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void SetInfoFromDB()
		{
			CheckDisposed();

			if (m_publication != null)
			{
				if (m_publication.PageWidth == 0 || m_publication.PageHeight == 0)
				{
					// Page size of 0 means "Full Page".
					if (m_publication.PaperHeight > 0 && m_publication.PaperWidth > 0)
					{
						m_dympPageHeight = m_publication.PaperHeight;
						m_dxmpPageWidth = m_publication.PaperWidth;
					}
					else
					{
						// Paper size not specified in database, so get it from the default paper size
						// on the default printer.
						PrinterUtils.GetDefaultPaperSizeInMp(out m_dympPageHeight, out m_dxmpPageWidth);
					}
					// Reduce the page size according to the gutter so that the print layout
					// views have the same "margin" as the printed page.
					if (m_publication.BindingEdge == BindingSide.Top)
						m_dympPageHeight -= m_publication.GutterMargin;
					else
						m_dxmpPageWidth -= m_publication.GutterMargin;
				}
				else
				{
					// The page size already excludes the gutter.
					m_dxmpPageWidth = m_publication.PageWidth;
					m_dympPageHeight = m_publication.PageHeight;
				}
				foreach (DivisionLayoutMgr division in m_divisions)
					division.SetInfoFromDB();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Invalidate the specified rectangle, which is in printer coordinates relative
		/// to the top left of the page.
		/// </summary>
		/// <param name="page"></param>
		/// <param name="rect"></param>
		/// ------------------------------------------------------------------------------------
		internal void InvalidatePrintRect(Page page, Rectangle rect)
		{
			CheckDisposed();

			int indexOfPage = this.IndexOfPage(page);
			int topOfPage = indexOfPage * PageHeightPlusGapInScreenPixels;
			// Relative to page, but in screen coordinates.
			Rectangle rectOnPageScreen = Rectangle.FromLTRB(
				this.ConvertPrintDistanceToTargetX(rect.Left, DpiXScreen),
				this.ConvertPrintDistanceToTargetY(rect.Top, DpiYScreen),
				this.ConvertPrintDistanceToTargetX(rect.Right, DpiXScreen),
				this.ConvertPrintDistanceToTargetY(rect.Bottom, DpiYScreen));
			Rectangle rectToInvert = rectOnPageScreen;
			rectToInvert.Offset(ScrollPosition.X, ScrollPosition.Y + topOfPage);
			rectToInvert.Inflate(0, 2); // for out-of-bounds diacritics and descenders, etc.
			this.Invalidate(rectToInvert, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find the element which contains the specified point.
		/// This should be a point that is actually visible on the screen, so that lazy
		/// stuff doesn't have to be expanded.
		/// If there is no element at the specified point we'll look for the first page
		/// element with the same Y position. If there is none with the same Y position
		/// we return <c>null</c>.
		/// </summary>
		/// <param name="pt">Point at display resolution relative to top left of client area</param>
		/// <param name="scrollPos">AutoScrollPosition (an argument for testing purposes).</param>
		/// <param name="ptLayout">Point at layout resolution relative to top left of
		/// document (for the element's stream).</param>
		/// <param name="page">The page.</param>
		/// <returns>Element at position <paramref name="pt"/> (or with the same Y-position),
		/// or <c>null</c> if there is no element at position <paramref name="pt"/>.</returns>
		/// <remarks>ENHANCE: find closest element instead of getting first element with same
		/// Y position if there is no element at specified position.</remarks>
		/// ------------------------------------------------------------------------------------
		public PageElement ElementFromPoint(Point pt, Point scrollPos, out Point ptLayout,
			out Page page)
		{
			CheckDisposed();

			ptLayout = pt; // arbitrary, but compiler insists.
			page = null;
			if (Pages == null || Pages.Count == 0)
				return null;

			Point ptDoc = pt;
			ptDoc.Offset(-scrollPos.X, -scrollPos.Y); // relative to whole seq of pages
			int ipage = ptDoc.Y / PageHeightPlusGapInScreenPixels;

			if (ipage < 0 || ipage >= Pages.Count)
				return null;

			Point ptPageScrn = ptDoc; // relative to page in screen pixels
			ptPageScrn.Offset(0, -ipage * PageHeightPlusGapInScreenPixels);
			Point ptPage = new Point(ConvertScreenDistanceToPrinterX(ptPageScrn.X, DpiXScreen),
				ConvertScreenDistanceToPrinterY(ptPageScrn.Y, DpiYScreen));
			page = Pages[ipage] as Page;
			if (page.NeedsLayout)
				return null;
			foreach (PageElement pe in page.PageElements)
			{
				if (pe.LocationOnPage.Contains(ptPage))
				{
					ptLayout = ptPage;
					ptLayout.Offset(-pe.LocationOnPage.Left,
						pe.OffsetToTopPageBoundary - pe.LocationOnPage.Top);
					return pe;
				}
			}

			// No page element at the given point. Is there one to the right or left
			// of it?
			PageElement bestChoice = null;
			foreach (PageElement pe in page.PageElements)
			{
				if (pe.LocationOnPage.Top <= ptPage.Y && ptPage.Y <= pe.Bottom)
				{
					if (bestChoice == null ||
						DistanceXFromRect(bestChoice.LocationOnPage, ptPage) >
						DistanceXFromRect(pe.LocationOnPage, ptPage))
					{
						bestChoice = pe;
					}
				}
			}

			if (bestChoice != null)
			{
				ptLayout = ptPage;
				ptLayout.Offset(-bestChoice.LocationOnPage.Left,
					bestChoice.OffsetToTopPageBoundary - bestChoice.LocationOnPage.Top);
			}
			return bestChoice;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Computes the X distance of the specified point from the closest side of the specified
		/// rectangle. Result is always positive.
		/// </summary>
		/// <param name="rect">The rectangle.</param>
		/// <param name="pt">The point.</param>
		/// <returns>The X distance of the specified point from the closest side of the specified
		/// rectangle</returns>
		/// ------------------------------------------------------------------------------------
		private int DistanceXFromRect(Rectangle rect, Point pt)
		{
			int dist = Math.Abs(rect.Left - pt.X);
			dist = Math.Min(dist, Math.Abs(rect.Right - pt.X));
			return dist;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Convert the horizontal measure in printer pixels to target device pixels.
		/// </summary>
		/// <param name="dxp">horizontal measure in printer pixels</param>
		/// <param name="dpiTargetX">DPI setting for the target device</param>
		/// <returns>horizontal measure in target device pixels</returns>
		/// ------------------------------------------------------------------------------------
		public int ConvertPrintDistanceToTargetX(int dxp, float dpiTargetX)
		{
			CheckDisposed();

			// For some reason this version seems to be even worse for computing rectangles to invalidate.
			//			int dpiX = (int)(dpiTargetX * Zoom); // need to round the same way the View code does
			//			return dxp * dpiX / (int) DpiXPrinter;
			return (int)(dxp * dpiTargetX / DpiXPrinter * Zoom);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Convert from screen resolution to printer (layout). Rounding errors are likely,
		/// as printer pixels are usually smaller.
		/// </summary>
		/// <param name="dxp"></param>
		/// <param name="dpiTargetX"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public int ConvertScreenDistanceToPrinterX(int dxp, float dpiTargetX)
		{
			CheckDisposed();

			return (int)(dxp * DpiXPrinter / dpiTargetX / Zoom);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Convert the vertical measure in printer pixels to target device pixels.
		/// </summary>
		/// <param name="dyp">vertical measure in printer pixels</param>
		/// <param name="dpiTargetY">DPI setting for the target device</param>
		/// <returns>vertical measure in target device pixels</returns>
		/// ------------------------------------------------------------------------------------
		public int ConvertPrintDistanceToTargetY(int dyp, float dpiTargetY)
		{
			CheckDisposed();

			return (int)(dyp * dpiTargetY / DpiYPrinter * Zoom);
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Convert from screen resolution to printer (layout). Rounding errors are likely,
		/// as printer pixels are usually smaller.
		/// </summary>
		/// <param name="dyp">The dyp.</param>
		/// <param name="dpiTargetY">The dpi target Y.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public int ConvertScreenDistanceToPrinterY(int dyp, float dpiTargetY)
		{
			CheckDisposed();

			return (int)(dyp * DpiYPrinter / dpiTargetY / Zoom);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Calculate the overall size of the document in screen pixels.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void AdjustAutoScrollRange()
		{
			CheckDisposed();

			Size size = new Size(0, 0);
			if (m_pages.Count > 0)
				size = new Size(0, PageHeightPlusGapInScreenPixels * m_pages.Count);

			if (InvokeRequired)
			{
				SizePropertyInvoker prop = delegate(Size s) { AutoScrollMinSize = s; };
				Invoke(prop, size);
			}
			else
				AutoScrollMinSize = size;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find index of the given page. This is broken out as a method in case
		/// we decide to change to a linked list or something more complex.
		/// </summary>
		/// <param name="div"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public int IndexOfDiv(DivisionLayoutMgr div)
		{
			CheckDisposed();

			return m_divisions.IndexOf(div);
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find index of the given page. This is broken out as a method in case
		/// we decide to change to a linked list or something more complex.
		/// </summary>
		/// <param name="page"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public int IndexOfPage(Page page)
		{
			CheckDisposed();

			return m_pages.IndexOf(page);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find the page following the given page. This is broken out as a method in case
		/// we decide to change to a linked list or something more complex.
		/// </summary>
		/// <param name="page"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public Page PageAfter(Page page)
		{
			CheckDisposed();

			int iPage = IndexOfPage(page) + 1;
			if (iPage >= m_pages.Count)
				return null;
			return m_pages[iPage];
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find the page preceding the given page. This is broken out as a method in case
		/// we decide to change to a linked list or something more complex.
		/// </summary>
		/// <param name="page"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public Page PageBefore(Page page)
		{
			CheckDisposed();

			int iPage = IndexOfPage(page) - 1;
			if (iPage < 0)
				return null;
			return m_pages[iPage];
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Insert a page following the given page. This is broken out as a method in case
		/// we decide to change to a linked list or something more complex.
		/// </summary>
		/// <param name="page"></param>
		/// <param name="newPage"></param>
		/// ------------------------------------------------------------------------------------
		public void InsertPageAfter(Page page, Page newPage)
		{
			CheckDisposed();

			lock (m_pages)
			{
				int index = IndexOfPage(page);
				m_pages.Insert(index + 1, newPage);
				AdjustPagesAfter(index);
				AdjustAutoScrollRange();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adjust the pages from the specified index onwards by making their numbers
		/// correct. The page at the specified index is correct, unless index is zero.
		/// Enhance: one day a division may be allowed to specify a first page number, and
		/// we will have to stop the adjustment when we come to such a division, as
		/// well as check whether the zeroth page has such a setting.
		/// </summary>
		/// <param name="indexOfLastCorrectPage">or -1 if no page is sure to be right.</param>
		/// ------------------------------------------------------------------------------------
		void AdjustPagesAfter(int indexOfLastCorrectPage)
		{
			lock (m_pages)
			{
				int pn;
				if (indexOfLastCorrectPage >= 0)
					pn = m_pages[indexOfLastCorrectPage].PageNumber + 1;
				else
				{
					pn = 1; // for first page
					indexOfLastCorrectPage = -1;
				}
				for (int i = indexOfLastCorrectPage + 1; i < m_pages.Count; i++, pn++)
				{
					m_pages[i].PageNumber = pn;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Insert a page before the given page. This is broken out as a method in case
		/// we decide to change to a linked list or something more complex.
		/// </summary>
		/// <param name="page"></param>
		/// <param name="newPage"></param>
		/// ------------------------------------------------------------------------------------
		public void InsertPageBefore(Page page, Page newPage)
		{
			CheckDisposed();

			lock (m_pages)
			{
				int index = IndexOfPage(page);
				m_pages.Insert(index, newPage);
				AdjustPagesAfter(index - 1);
				AdjustAutoScrollRange();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Delete the given page. This is broken out as a method in case
		/// we decide to change to a linked list or something more complex.
		/// </summary>
		/// <param name="page"></param>
		/// ------------------------------------------------------------------------------------
		public void DeletePage(Page page)
		{
			CheckDisposed();

			lock (m_pages)
			{
				int pageIndex = IndexOfPage(page);
				m_pages.Remove(page);
				AdjustPagesAfter(pageIndex - 1);
				AdjustAutoScrollRange();
				// TODO: If autoscroll position changes, need to invalidate
				page.Dispose();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a <see cref="T:Page"/> object from a handle.
		/// </summary>
		/// <param name="hPage">Handle of the page</param>
		/// <returns>The page, or <c>null</c> if not found</returns>
		/// ------------------------------------------------------------------------------------
		public Page FindPage(int hPage)
		{
			CheckDisposed();

			if (m_pages.Count == 0)
				return null;

			lock (m_pages)
			{
				// OPTIMIZE: Might want to use a hash table to make this faster
				foreach (Page page in m_pages)
				{
					if (page.Handle == hPage)
						return page;
				}
			}
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Release graphics
		/// Todo: probably eventually we will have a distinct printer and window Graphics
		/// object and this will need an argument so we know which we are releasing.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void ReleaseGraphics(IVwGraphics vg)
		{
			CheckDisposed();

			if (vg == m_printerGraphics.VwGraphics)
				UninitPrinterGraphics();
			else if (vg == m_screenGraphics.VwGraphics)
				UninitScreenGraphics();
			else
				Debug.Assert(false); // should be releasing a VwGraphics we made!
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Make sure the graphics object has a DC. If it already has, increment a count,
		/// so we know when to really free the DC.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		internal void InitScreenGraphics()
		{
			CheckDisposed();

			lock (m_screenGraphics)
			{
				m_screenGraphics.Init(Zoom);
			}
		}
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Make sure the printer graphics object has a DC. If it already has, increment a count,
		/// so we know when to really free the DC.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		internal void InitPrinterGraphics()
		{
			CheckDisposed();

			lock (m_printerGraphics)
			{
				m_printerGraphics.Init((int)DpiXPrinter, (int)DpiYPrinter);
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Uninitialize the graphics object by releasing the DC.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		internal void UninitScreenGraphics()
		{
			CheckDisposed();

			lock (m_screenGraphics)
			{
				m_screenGraphics.Uninit();
			}
		}
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Uninitialize the graphics object by releasing the DC.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		internal void UninitPrinterGraphics()
		{
			CheckDisposed();

			lock (m_printerGraphics)
			{
				m_printerGraphics.Uninit();
			}
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Calculate the zoom factor such that the physical page occupies the entire available
		/// width of the publication control.
		/// </summary>
		/// -------------------------------------------------------------------------------------
		public void SetZoomToPageWidth()
		{
			CheckDisposed();

			if (PageWidth == 0 || Width == 0)
			{
				Zoom = 1;
				return;
			}
			double pageWidthInInches = (double)PageWidth / MiscUtils.kdzmpInch;
			Zoom = (float)(Width / pageWidthInInches / DpiXScreen);

			AdjustAutoScrollRange();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the coord rects.
		/// </summary>
		/// <param name="rectSrcRoot">The rect SRC root.</param>
		/// <param name="rectDstRoot">The rect DST root.</param>
		/// ------------------------------------------------------------------------------------
		protected internal void GetCoordRects(out Rect rectSrcRoot, out Rect rectDstRoot)
		{
			// TODO: getting the Cursor.Position is very likely not to work if user
			// moves mouse pointer out of the window!
			Point pt = PointToClient(Cursor.Position);
			Point ptLayout;
			Page page;
			PageElement pe = ElementFromPoint(pt, ScrollPosition, out ptLayout, out page);

			Debug.Assert(page != null);
			if (pe == null)
			{
				Debug.Assert(page.PageElements.Count > 0);
				pe = (PageElement)page.PageElements[0];
			}
			Debug.Assert(pe != null);

			// Convert to box coords
			GetCoordRectsForElt(page, pe, out rectSrcRoot, out rectDstRoot);

			// This is a try to get the coordinates by not using Cursor.Position.
			// However, we got stuck with printer/screen coordinates and getting
			// the right source and dest rectangles.
//			InitScreenGraphics();
//			try
//			{
//				Rectangle rcSrcRoot;
//				Rectangle rcDstRoot;
//				rcSrcRoot = new Rectangle(0, 0, (int)m_printerDpiX, (int)m_printerDpiY);
//				rcDstRoot = new Rectangle(ScrollPosition.X, ScrollPosition.Y,
//					m_screenVwGraphics.XUnitsPerInch,
//					m_screenVwGraphics.YUnitsPerInch);
//
//				Rect rcPrimary = new Rect(0, 0, 0, 0);
//				Rect rcSecondary;
//				bool fSplit;
//				bool fEndBeforeAnchor;
//				IVwSelection sel = EditedRootBox.Selection;
//				if (sel != null)
//				{
//					sel.Location(m_screenVwGraphics, rcSrcRoot, rcDstRoot,
//						out rcPrimary, out rcSecondary, out fSplit, out fEndBeforeAnchor);
//				}
//				Point pt = new Point(rcPrimary.left, rcPrimary.top);
//				Point ptLayout;
//				Page page;
//				PageElement pe = ElementFromPoint(pt, ScrollPosition, out ptLayout, out page);
//
//				Debug.Assert(page != null);
//				if(pe == null)
//				{
//					Debug.Assert(page.PageElements.Count > 0);
//					pe = (PageElement)page.PageElements[0];
//				}
//				Debug.Assert(pe != null);
//
//				// Convert to box coords
//				GetCoordRectsForElt(page, pe, out rectSrcRoot, out rectDstRoot);
//			}
//			finally
//			{
//				ReleaseGraphics(m_screenVwGraphics);
//			}
		}

		// The ScrollPage method is not currently used and it was using the DoEvents
		// method of Application (which is being removed from the system).  So this
		// method is being removed.

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the line height from the normal style.
		/// </summary>
		/// <param name="stylesheet">The stylesheet.</param>
		/// <returns>line height value in millipoints (negative for "exact" line spacing)</returns>
		/// ------------------------------------------------------------------------------------
		public static int GetNormalLineHeight(FwStyleSheet stylesheet)
		{
			FwTextPropVar var;
			int normalLineSpacing = stylesheet.GetNormalLineSpacing(out var);

			// If the user changed the stylesheet to use relative (single/1.5/double) line
			// spacing instead of a specific value, we'll snap it back to our factory default.
			if (var == FwTextPropVar.ktpvRelative)
				normalLineSpacing = kDefaultNormalLineSpacing;
			return normalLineSpacing;
		}
		#endregion

		#region ISelectableView implementation
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Make it active by showing it.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public virtual void ActivateView()
		{
			CheckDisposed();

			Show();
			Focus();

			try
			{
				// If we lost our selection for some reason (e.g. structure of the text got
				// changed in a different view) then make a selection at the top of the
				// current rootbox. During startup we may not even have a root box yet.
				if (RootBox != null && RootBox.Selection == null)
					RootBox.MakeSimpleSel(true, true, false, true);
			}
			catch
			{
				// ignore any errors! This can happen if, for example, the view is empty.
			}

			MakeSureSelectionIsVisible();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Make it inactive by hiding it.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public void DeactivateView()
		{
			CheckDisposed();

			Hide();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the base caption string that should display in
		/// the info bar. This is used only if this is the client window.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string BaseInfoBarCaption
		{
			get
			{
				CheckDisposed();
				return m_baseInfoBarCaption;
			}
			set
			{
				CheckDisposed();

				m_baseInfoBarCaption = value == null ? null :
					value.Replace(Environment.NewLine, " ");
			}
		}
		#endregion

		#region General Event handling methods
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Flash the insertion point.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// -----------------------------------------------------------------------------------
		protected void OnTimer(object sender, EventArgs e)
		{
			if (FocusedRootBox != null && Focused)
				FocusedRootBox.FlashInsertionPoint(); // Ignore any error code.
		}
		#endregion

		#region Printing and Pagination Update methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle Print command
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual bool OnFilePrint()
		{
			CheckDisposed();

			// In case the print layout view has never been displayed, we configure it now.
			Configure();

			using (PrintDialog dlg = new PrintDialog())
			{
				dlg.Reset();
				dlg.Document = new PrintDocument();
				dlg.AllowSomePages = true;
				dlg.AllowSelection = false;
				dlg.ShowHelp = true;
				dlg.HelpRequest += new EventHandler(ShowPrintDlgHelp);
				dlg.PrinterSettings.MinimumPage = 1;
				dlg.PrinterSettings.FromPage = 1;
				dlg.PrinterSettings.ToPage = 1;
				// ENHANCE: Would "dlg.UseEXDialog" get us any of the enhancements we need?

				// ENHANCE: Displaying this dialog is slow (loads a lot of DLLs), at least in Debug
				// mode. Tried displaying a wait cursor (wrapped around this entire method) but it
				// didn't work.
				if (dlg.ShowDialog(this) != DialogResult.OK)
					return true;

				if (MiscUtils.IsUnix)
					dlg.Document.DefaultPageSettings.PaperSize = new PaperSize("Custom",
						Publication.PaperWidth/720, Publication.PaperHeight/720);

				// REVIEW: .NET does not appear to handle the collation setting correctly
				// so for now, we do not support non-collated printing.  Forcing the setting
				// seems to work fine.
				if (dlg.PrinterSettings.Copies > 1)
					dlg.Document.PrinterSettings.Collate = true;

				switch (dlg.PrinterSettings.PrintRange)
				{
					case PrintRange.AllPages:
						m_currentPrintPage = 1;
						break;

					case PrintRange.Selection:
						// TODO:  This is not really the selection, for now, it will just print
						// page 1 because we are not sure what the selection is.  Also, we want
						// to consider not supporting printing the selection.
						m_currentPrintPage = 1;
						break;

					case PrintRange.SomePages:
						m_currentPrintPage = dlg.PrinterSettings.FromPage;
						break;
				}
				SimpleRootSite.PrintWithErrorHandling(this, dlg.Document, FindForm());
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Show help for the print dialog
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void ShowPrintDlgHelp(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, "khtpPrint");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles paginating the whole document
		/// </summary>
		/// <param name="args">ignored</param>
		/// <returns><c>true</c> because we handled it</returns>
		/// ------------------------------------------------------------------------------------
		public virtual bool OnViewUpdatePageBreak(object args)
		{
			CheckDisposed();

			IVwRootBox focusedRootBox = FocusedRootBox;
			using (var dialog = new ProgressDialogWithTask(FindForm(), Cache.ThreadHelper))
			{
				dialog.CancelButtonText = ResourceHelper.GetResourceString("kstidUpdatePageBreakButtonText");
				dialog.Title = ResourceHelper.GetResourceString("kstidUpdatePageBreakWindowCaption");
				dialog.AllowCancel = true;
				dialog.RunTask(true, UpdatePageBreaks, DpiXPrinter, DpiYPrinter);
			}

			// make sure the selection is in view
			if (focusedRootBox != null && focusedRootBox.Selection != null &&
				focusedRootBox.Selection.IsValid)
				ScrollSelectionIntoView(focusedRootBox.Selection, VwScrollSelOpts.kssoDefault);

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the page breaks.
		/// </summary>
		/// <param name="dialog">The progress dialog.</param>
		/// <param name="parameters">Float X dpi as first parameter, float Y dpi as second</param>
		/// ------------------------------------------------------------------------------------
		private object UpdatePageBreaks(IThreadedProgress dialog, object[] parameters)
		{
			Debug.Assert(parameters.Length == 2);
			float dpiX = (float)parameters[0];
			float dpiY = (float)parameters[1];
			while (true)
			{
				if (dialog.Canceled)
					break;
				bool moreLayoutRequired;
				if (InvokeRequired)
					moreLayoutRequired = (bool)Invoke((Func<bool>)(() => LayoutToPageIfNeeded(m_pages.Count - 1, dpiX, dpiY, dialog)));
				else
					moreLayoutRequired = LayoutToPageIfNeeded(m_pages.Count - 1, dpiX, dpiY, dialog);
				if (!moreLayoutRequired)
					break;
			}
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Print the page layout.
		/// Implements interface IPrintRootSite. Caller is responsible to catch any exceptions.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Print(PrintDocument pd)
		{
			SetZoomToPageWidth();
			if (m_fRefreshPending)
				RefreshDisplay(Visible);
			m_printDateTime = DateTime.Now;
			pd.PrintPage += PrintLayout_PrintPage;
			pd.Print();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The PrintPage event is raised for each page to be printed.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		public void PrintLayout_PrintPage(object sender, PrintPageEventArgs e)
		{
			CheckDisposed();

			// Print the next page. PrintPage requires a 0-based page number.
			if (PrintPage(e, m_currentPrintPage - 1))
				e.HasMorePages = Advance(e);
			else
				e.HasMorePages = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Advance the page number to the next page to print.
		/// </summary>
		/// <returns>true if there are more pages, else false</returns>
		/// ------------------------------------------------------------------------------------
		private bool Advance(PrintPageEventArgs e)
		{
			switch (e.PageSettings.PrinterSettings.PrintRange)
			{
				case PrintRange.AllPages:
					m_currentPrintPage++;
					return (m_currentPrintPage <= m_pages.Count);

				case PrintRange.Selection:
					return false;

				case PrintRange.SomePages:
					m_currentPrintPage++;
					return (m_currentPrintPage <= e.PageSettings.PrinterSettings.ToPage &&
						m_currentPrintPage <= m_pages.Count);
			}
			return false;
		}
		#endregion

		#region Implementation of IEditingCallbacks
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Use this instead of AutoScrollPosition, which works only for Rootsites with scroll
		/// bars, for some reason.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public virtual Point ScrollPosition
		{
			get
			{
				CheckDisposed();
				return AutoScrollPosition;
			}
			set
			{
				CheckDisposed();
				AutoScrollPosition = value;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>Pending writing system</summary>
		/// <remarks>This gets set when there was a switch in the system keyboard,
		/// and at that point there was no insertion point on which to change the writing system.
		/// We store the information here until either they get an IP and start typing
		/// (the writing system is set using these) or the selection changes (throw the
		/// informtion away and reset the keyboard).
		/// </remarks>
		/// -----------------------------------------------------------------------------------
		public int WsPending
		{
			get
			{
				CheckDisposed();
				return m_wsPending;
			}
			set
			{
				CheckDisposed();
				m_wsPending = value;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Scroll to the top
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public virtual void ScrollToTop()
		{
			CheckDisposed();

			if (AutoScroll)
				ScrollPosition = new Point(0, 0);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Scroll to the bottom. This is somewhat tricky because after scrolling to the bottom of
		/// the range as we currently estimate it, expanding a closure may change things.
		/// Revview TeAnalyst: do we want to scroll to the end of the last simulated page,
		/// or the end of the data? What if there are footnotes at the end of a long gap on
		/// the last page?
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public virtual void ScrollToEnd()
		{
			CheckDisposed();

			if (AutoScroll && !DesignMode)
			{
				// For now just set the scroll position to its absolute maximum. Expanding
				// lazy stuff at that point may reveal that there are more (or fewer?) pages,
				// which we will want to do something about sometime, but it won't be exactly
				// the same as the code in the similar RootSite method.
				ScrollPosition = new Point(-ScrollPosition.X, AutoScrollMinSize.Height);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Show the context menu for the specified root box at the location of
		/// its selection (typically an IP).
		/// </summary>
		/// <param name="rootb"></param>
		/// ------------------------------------------------------------------------------------
		public void ShowContextMenuAtIp(IVwRootBox rootb)
		{
			CheckDisposed();
			ShowContextMenu(IPLocation(rootb));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the (estimated) height of one line
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int LineHeight
		{
			get
			{
				CheckDisposed();

				return (int) (14 * DpiYScreen / 72 * Zoom); // 14 points is typically about a line.
			}
		}
		/// -----------------------------------------------------------------------------------
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
		/// -----------------------------------------------------------------------------------
		public virtual EditingHelper.CkBehavior ComplexKeyBehavior(int chw,
			VwShiftStatus ss)
		{
			CheckDisposed();

			return EditingHelper.CkBehavior.Logical;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the focused root box for editing.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IVwRootBox EditedRootBox
		{
			get
			{
				CheckDisposed();

				if (FocusedRootBox != null)
					return FocusedRootBox;
				else
				{
					// No focused rootbox, return the main box of the first division
					// if this is available.
					if (Divisions.Count > 0)
					{
						DivisionLayoutMgr div = (DivisionLayoutMgr) Divisions[0];
						return div.MainRootBox;
					}
					return null;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Assume always have a cache or WS, for now.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual bool GotCacheOrWs
		{
			get
			{
				CheckDisposed();
				return true;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the writing system for the HVO. This could either be the vernacular or
		/// analysis writing system.
		/// </summary>
		/// <param name="hvo">HVO</param>
		/// <returns>Writing system</returns>
		/// ------------------------------------------------------------------------------------
		public virtual int GetWritingSystemForHvo(int hvo)
		{
			CheckDisposed();
			return Cache.DefaultVernWs;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Perform any processing needed immediately prior to a paste operation.  This is very
		/// rarely implemented, but always called by EditingHelper.PasteClipboard.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void PrePasteProcessing()
		{
			CheckDisposed();
		}
		#endregion

		#region IRootSite Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CloseRootBox()
		{
			CheckDisposed();

			foreach (DivisionLayoutMgr div in m_divisions)
				div.CloseRootBox();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Allows the IRootSite to be cast as an IVwRootSite
		/// </summary>
		/// <returns>This object.</returns>
		/// ------------------------------------------------------------------------------------
		public IVwRootSite CastAsIVwRootSite()
		{
			CheckDisposed();
			return this;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Refreshes the display if necessary and good. :)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool RefreshDisplay()
		{
			CheckDisposed();

			// If we aren't visible or don't belong to a form, then set a flag to do a refresh
			// the next time we go visible.
			if (!Visible || FindForm() == null)
			{
				m_fRefreshPending = true;
				//if we aren't visible, any theoretical descendant controls aren't visible, so return true
				return true;
			}

			RefreshDisplay(true);
			//Enhance: if all descendant controls have been Refreshed return true
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Refreshes the display :)
		/// </summary>
		/// <param name="fPreserveSelection">True to save the selection and restore it afterwards,
		/// false otherwise</param>
		/// ------------------------------------------------------------------------------------
		protected virtual void RefreshDisplay(bool fPreserveSelection)
		{
			SetInfoFromDB();

			DisposePages();
			SetZoomToPageWidth();

			// Save where the selection is so we can try to restore it after reconstructing.
			SelectionHelper selHelper = (fPreserveSelection) ? SelectionHelper.Create(this) : null;

			using (new SuspendDrawing(this))
			{
				// Refresh shared substreams
				foreach (IVwRootBox stream in m_sharedStreams)
					stream.Reconstruct();

				CreatePages();
				m_fRefreshPending = false;
			}

			if (selHelper != null)
				selHelper.RestoreSelectionAndScrollPos();

			Invalidate();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// return the list of internal rootboxes, or null if none.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public virtual List<IVwRootBox> AllRootBoxes()
		{
			CheckDisposed();

			List<IVwRootBox> rootboxes = new List<IVwRootBox>();
			foreach (DivisionLayoutMgr div in m_divisions)
			{
				if (div.MainRootBox != null)
					rootboxes.Add(div.MainRootBox);
			}
			return rootboxes;
		}

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
		public int GetStyleNameFromSelection(out string styleName)
		{
			CheckDisposed();

			// TODO TE-1446: this needs to return current style name from the selection. See
			// SimpleRootSite for implementation (probably needs to be refactored to a common
			// "helper" class.
			styleName = string.Empty;
			return -1;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// <c>false</c> to prevent OnPaint from happening, <c>true</c> to perform
		/// OnPaint. This is used to prevent redraws from happening while we do a RefreshDisplay.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool AllowPainting
		{
			get
			{
				CheckDisposed();
				return (m_nAllowPaint == 0);
			}
			set
			{
				if (InvokeRequired)
				{
					BoolPropertyInvoker prop = delegate(bool f){ AllowPainting = f; };
					Invoke(prop, value);
					return;
				}

				CheckDisposed();

				// we use WM_SETREDRAW to prevent the scrollbar from jumping around when we are
				// in the middle of a Reconstruct. The m_nAllowPaint flag takes care of (not)
				// painting the view.

				if (value)
				{	// allow painting
					m_nAllowPaint--;

					if (m_nAllowPaint == 0 && Visible)
					{
						Win32.SendMessage(Handle, (int)Win32.WinMsgs.WM_SETREDRAW, 1, 0);
						Update();
						Invalidate();
					}
				}
				else
				{   // prevent painting
					if (m_nAllowPaint == 0 && Visible)
						Win32.SendMessage(Handle, (int)Win32.WinMsgs.WM_SETREDRAW, 0, 0);

					m_nAllowPaint++;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Scroll the selection in view and set the IP at the given client position.
		/// </summary>
		/// <param name="sel">The selection</param>
		/// <param name="dyPos">Position from top of client window where IP should be set</param>
		/// ------------------------------------------------------------------------------------
		public bool ScrollSelectionToLocation(IVwSelection sel, int dyPos)
		{
			return ScrollSelectionIntoView(sel, VwScrollSelOpts.kssoDefault);
		}
		#endregion

		#region IVwRootSite Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method informs us that, due to converting lazy boxes into real ones (or, eventually,
		/// perhaps due to turning real boxes into lazy ones), the size of something changed.
		/// Currently, laziness is only done vertically, so we can ignore dxdSize and dxdPosition.
		/// Specifically, the change resulted in the size of the root box changing by the specified
		/// amount (positive larger) at the specified position.
		///
		/// These values are currently in 'destination' coordinates, as specified by the conversion
		/// returned by GetGraphics. In this publication control, GetGraphics currently returns a
		/// null conversion, so they are also source coordinates (printer pixels relative to the
		/// top left of the view). As we figure out what to really do about GetGraphics, we may
		/// need to adjust this method, the Views code that calls it, or both. We may need another
		/// interface method that simply receives the notification in source coordinates.
		///
		/// What the method does is to adjust the OffsetToTopPageBoundary for any elements on any
		/// following pages, and possibly add or delete page objects, so that
		///		(1) for any real pages (that have elements of this stream), OffsetFromTopOfDiv
		///			remains accurately aligned with what is in the view; and
		///		(2) the overall number of pages remains reasonably consistent with the
		///			current estimate of the size of the division contents; and
		///		(3) the 'real' pages remain in roughly the right relative positions.
		///
		///	All the material on 'real' pages consists of real boxes; therefore, the part of
		///	the old document that has 'disappeared' if dydSize is negative should not
		///	overlap any existing 'real' page.
		/// </summary>
		/// <param name="rootbox">A rootbox that should be a stream on at least some of the pages
		/// of publication (i.e., this publication should be the IVwRootsite of the given rootbox)
		/// </param>
		/// <param name="dxdSize">Ignored</param>
		/// <param name="dxdPosition">Ignored</param>
		/// <param name="dydSize">The amount the rootbox grew or shrunk, in printer pixels
		/// </param>
		/// <param name="dydPosition">The top of the lazy box which was (partially) expanded,
		/// shrunk, split, etc (in printer pixels). Or the position of the top of the real box
		/// that got relazified.
		/// </param>
		/// <returns>true if deletion of pages forced AutoScrollPosition to decrease by MORE
		/// than the amount caused by adding dydSize; or if there are not enough pages
		/// for it to increase as much as required.</returns>
		/// ------------------------------------------------------------------------------------
		public bool AdjustScrollRange(IVwRootBox rootbox, int dxdSize, int dxdPosition,
			int dydSize, int dydPosition)
		{
			CheckDisposed();

			if (dydSize == 0 || Pages == null)
				return false;

			// For each page which cares about this element, if the top of that
			// element >= dydPosition, then adjust top by dydSize.
			IVwLayoutStream strm = rootbox as IVwLayoutStream;
			Debug.Assert(strm != null); // we shouldn't be the manager of a non-stream root.
			// The last page whose top doesn't move is the one the lazy box is actually on,
			// which changed in size. We may need to delete or insert pages just after this.
			Page changedSizePage = null;
			int iDiv = DivisionIndexForMainStream(strm);
			DivisionLayoutMgr divMgr = (iDiv >= 0) ? Divisions[iDiv] : null;
			PageElement pe;
			Page prevPage = null;
			foreach (Page page in Pages)
			{
				// It is almost arbitrary whether we use > or >=, since any real page
				// consists entirely of real boxes and the position of the lazy box
				// we're changing should be well clear. However, using > prevents
				// moving the top of the very first page when expanding the very first
				// lazy box (or any other time).
				int origOffset = page.OffsetFromTopOfDiv(strm, divMgr, out pe);
				if (origOffset > dydPosition)
				{
					page.AdjustOffsetFromTopOfDiv(pe, origOffset + dydSize);
					if (changedSizePage == null)
						changedSizePage = prevPage;
				}
				prevPage = page;
			}

			// If it's a main stream, do additional work to adjust page list and
			// scroll position. Otherwise we're done.
			if (iDiv == -1)
				return false;

			if (dydSize < 0)
				return DeleteExtraPages(dydSize, dydPosition);
			else // overall doc size is increasing
			{
				if (changedSizePage == null) // Probably trying to lay out a page that can't exist ???
					return false;
				return InsertAdditionalPages(changedSizePage, strm, dydSize, dydPosition);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Member ClientToScreen
		/// </summary>
		/// <param name="_Root">_Root</param>
		/// <param name="_pnt">_pnt</param>
		/// ------------------------------------------------------------------------------------
		public void ClientToScreen(IVwRootBox _Root, ref System.Drawing.Point _pnt)
		{
			CheckDisposed();

			// TODO:  Add PublicationControl.ClientToScreen implementation
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Member get_SemiTagging
		/// </summary>
		/// <param name="_Root">_Root</param>
		/// <returns>A System.Boolean</returns>
		/// ------------------------------------------------------------------------------------
		public bool get_SemiTagging(IVwRootBox _Root)
		{
			CheckDisposed();

			// TODO:  Add PublicationControl.get_SemiTagging implementation
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Allow default paste behavior.
		/// </summary>
		/// <param name="rootBox">the sender</param>
		/// <param name="ttpDest">properties of destination paragraph</param>
		/// <param name="cPara">number of paragraphs to be inserted</param>
		/// <param name="ttpSrcArray">Array of props of each para to be inserted</param>
		/// <param name="tssParas">Array of TsStrings for each para to be inserted</param>
		/// <param name="tssTrailing">Text of an incomplete paragraph to insert at end (with
		/// the properties of the destination paragraph.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public virtual VwInsertDiffParaResponse OnInsertDiffParas(IVwRootBox rootBox,
			ITsTextProps ttpDest, int cPara, ITsTextProps[] ttpSrcArray, ITsString[] tssParas,
			ITsString tssTrailing)
		{
			CheckDisposed();

			return VwInsertDiffParaResponse.kidprDefault;
		}

		/// <summary> see OnInsertDiffParas </summary>
		public virtual VwInsertDiffParaResponse OnInsertDiffPara(IVwRootBox prootb,
			ITsTextProps ttpDest, ITsTextProps ttpSrc, ITsString tssParas,
			ITsString tssTrailing)
		{
			CheckDisposed();

			return VwInsertDiffParaResponse.kidprDefault;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Member IsOkToMakeLazy
		/// </summary>
		/// <param name="_Root">_Root</param>
		/// <param name="ydTop">ydTop</param>
		/// <param name="ydBottom">ydBottom</param>
		/// <returns>A System.Boolean</returns>
		/// ------------------------------------------------------------------------------------
		public bool IsOkToMakeLazy(IVwRootBox _Root, int ydTop, int ydBottom)
		{
			CheckDisposed();

			// TODO:  Add PublicationControl.IsOkToMakeLazy implementation
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return the available width for laying out the data. Normally this is the page width,
		/// less side margins, in printer pixels. For streams which are divided into multiple
		/// columns, this is the column width.
		/// </summary>
		/// <param name="_Root"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public int GetAvailWidth(IVwRootBox _Root)
		{
			CheckDisposed();

			IVwLayoutStream strm = _Root as IVwLayoutStream;
			int iDiv = DivisionIndexForMainStream(strm);
			if (iDiv >= 0)
				return Divisions[iDiv].AvailableMainStreamColumWidthInPrinterPixels;

			foreach (Page page in Pages)
				foreach (PageElement pe in page.PageElements)
					if (pe.m_stream == strm)
						return Divisions[page.FirstDivOnPage].AvailablePageWidthInPrinterPixels;

			if (m_pageBeingLaidOut != null && !m_pageBeingLaidOut.IsDisposed)
				return Divisions[m_pageBeingLaidOut.FirstDivOnPage].AvailablePageWidthInPrinterPixels;

			foreach (IVwLayoutStream sharedStream in m_sharedStreams)
				if (sharedStream == strm)
					return Divisions[0].AvailablePageWidthInPrinterPixels;

			//// As a fallback, we return the margins of the first division in the publication
			//Debug.Fail("We don't think we should ever get here.");
			return Divisions[0].AvailablePageWidthInPrinterPixels;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If we need to make a selection, but we can't because edits haven't been updated in the
		/// view, this method requests creation of a selection after the unit of work is complete.
		/// Specifically, when all PropChanged calls have been made for the UOW.
		/// The arguments are as for MakeTextSelection, except that we only include the ones
		/// necessary to specify an insertion point, and of course don't return the selection,
		/// which typically won't be made until later. The selection made is always installed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void RequestSelectionAtEndOfUow(IVwRootBox rootb, int ihvoRoot, int cvlsi,
			SelLevInfo[] rgvsli, int tagTextProp, int cpropPrevious, int ich, int wsAlt,
			bool fAssocPrev, ITsTextProps selProps)
		{
			// Creating one hooks it up; it will free itself when invoked.
			new RequestSelectionHelper((IActionHandlerExtensions)m_cache.ActionHandlerAccessor,
				rootb, ihvoRoot, rgvsli, tagTextProp, cpropPrevious, ich, wsAlt, fAssocPrev, selProps);

			// We don't want to continue using the old, out-of-date selection.
			rootb.DestroySelection();
		}

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
		public void RequestVisibleSelectionAtEndOfUow(SelectionHelper helper)
		{
			// Creating one hooks it up; it will free itself when invoked.
			new RequestSelectionByHelper((IActionHandlerExtensions)m_cache.ActionHandlerAccessor, helper);

			// We don't want to continue using the old, out-of-date selection.
			if (helper.RootSite != null && helper.RootSite.RootBox != null)
				helper.RootSite.RootBox.DestroySelection();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Answer your root box. It's debatable which one will best fit the need, but
		/// for at least one purpose...committing typing...answering the control's focused
		/// root box is helpful. Try to minimize use of this method; the assumption that
		/// a root site has just one root box is obsolete.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public IVwRootBox RootBox
		{
			get
			{
				CheckDisposed();
				return FocusedRootBox;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the window handle that the control is bound to.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public uint Hwnd
		{
			get
			{
				CheckDisposed();
				return (uint)Handle;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Member GetAndClearPendingWs
		/// </summary>
		/// <param name="_Root">_Root</param>
		/// <returns>A System.Int32</returns>
		/// ------------------------------------------------------------------------------------
		public int GetAndClearPendingWs(IVwRootBox _Root)
		{
			CheckDisposed();

			// -1 signifies no pending writing system. A full implementation will have
			// a member variable for this, which gets set when the user selects an input
			// language using the language bar (see RootSite.cs).
			return -1;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Member DoUpdates
		/// </summary>
		/// <param name="_Root">_Root</param>
		/// ------------------------------------------------------------------------------------
		public void DoUpdates(IVwRootBox _Root)
		{
			CheckDisposed();

			// TODO:  Add PublicationControl.DoUpdates implementation
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Member OverlayChanged
		/// </summary>
		/// <param name="_Root">_Root</param>
		/// <param name="_vo">_vo</param>
		/// ------------------------------------------------------------------------------------
		public void OverlayChanged(IVwRootBox _Root, IVwOverlay _vo)
		{
			CheckDisposed();

			// TODO:  Add PublicationControl.OverlayChanged implementation
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Member SizeChanged
		/// </summary>
		/// <param name="_Root">_Root</param>
		/// ------------------------------------------------------------------------------------
		void IVwRootSite.RootBoxSizeChanged(IVwRootBox _Root)
		{
			CheckDisposed();

			// Because we're not treating them as full subordinate streams, private page
			// dependent object (footnote) streams may not give us an adequate page broken
			// notification when their size changes.
			foreach (Page page in m_pages)
			{
				if (page != null)
					page.NoteRootSizeChanged(_Root);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Member OnProblemDeletion
		/// </summary>
		/// <param name="_sel">_sel</param>
		/// <param name="dpt">dpt</param>
		/// <returns>A VwDelProbResponse</returns>
		/// ------------------------------------------------------------------------------------
		public virtual VwDelProbResponse
			OnProblemDeletion(IVwSelection _sel, VwDelProbType dpt)
		{
			CheckDisposed();

			// Treat as not implemented and accept the default behavior.
			throw new NotImplementedException();
		}

		/// <summary>
		/// This is currently used to get a printer device context for doing layout
		/// (e.g., while expanding lazy boxes, or making the initial selection
		/// before the root has been constructed.) There is Views code that also
		/// expects this to be a screen VwGraphics that it can use for actual drawing.
		/// We need to change the Views code to tease these two uses apart, or eliminate
		/// the need for the second one.
		/// </summary>
		/// <param name="root"></param>
		/// <param name="pvg"></param>
		/// <param name="rcSrcRoot"></param>
		/// <param name="rcDstRoot"></param>
		public void GetGraphics(IVwRootBox root, out IVwGraphics pvg, out Rect rcSrcRoot, out Rect rcDstRoot)
		{
			CheckDisposed();

			Debug.Assert(m_publication != null);
			pvg = PrinterGraphics;

			rcDstRoot = rcSrcRoot = new Rect(0, 0, pvg.XUnitsPerInch, pvg.YUnitsPerInch);

#if __MonoCS__
			// TODO-Linux: Marshing bug - fix mono
			// When this method was called from native code and same referense was assigned to both out values
			// Then second value was unitialized. on return from native call.
			// This is probebly a COM marshing bug needs looking at as same bug could occur in other places.
			rcDstRoot = new Rect(0, 0, pvg.XUnitsPerInch, pvg.YUnitsPerInch);
#endif
			// TODO Get Coordniate rectangles
			//			Rectangle rcSrc, rcDst;
			//			GetCoordRects(out rcSrc, out rcDst);
			//
			//			rcSrcRoot = rcSrc;
			//			rcDstRoot = rcDst;

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the coordinate transformation appropriate to the given screen point.
		/// </summary>
		/// <param name="root">The root.</param>
		/// <param name="pt">The pt.</param>
		/// <param name="rcSrcRoot">The rc SRC root.</param>
		/// <param name="rcDstRoot">The rc DST root.</param>
		/// <remarks>TODO: implement. (The following seems to be good enough for the things that
		/// use it in Print Layout view?? Or is this why the hand cursor never appears?)
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public void GetTransformAtDst(IVwRootBox root, Point pt, out Rect rcSrcRoot, out Rect rcDstRoot)
		{
			CheckDisposed();

			Page page;
			Point ptLayout;
			PageElement pe = ElementFromPoint(pt,
				AutoScrollPosition, out ptLayout, out page);
			if (pe == null)
				throw new Exception("GetTransformAtDst mysteriously failed");
			if (root as IVwLayoutStream != pe.m_stream)
				throw new Exception("point not in requester of transform");

			GetCoordRectsForElt(page, pe, out rcSrcRoot, out rcDstRoot);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the coordinate transformation appropriate to the given layout point.
		/// </summary>
		/// <param name="root">The root.</param>
		/// <param name="pt">The pt.</param>
		/// <param name="rcSrcRoot">The rc SRC root.</param>
		/// <param name="rcDstRoot">The rc DST root.</param>
		/// ------------------------------------------------------------------------------------
		public void GetTransformAtSrc(IVwRootBox root, Point pt, out Rect rcSrcRoot, out Rect rcDstRoot)
		{
			CheckDisposed();

			int dyPageScreen;
			IVwLayoutStream strm = (IVwLayoutStream)root;
			int iDiv;
			bool layedOutPage;
			Page page = PageFromPrinterY(pt.X, pt.Y, false, strm, out dyPageScreen, out iDiv, out layedOutPage);
			if (page == null)
				throw new Exception("Trying to use a point not on any page");
			page.LayOutIfNeeded();
			PageElement pe = page.GetFirstElementForStream(strm);
			if (pe == null)
				throw new Exception("GetTransformAtSrc mysteriously failed");
			GetCoordRectsForElt(page, pe, out rcSrcRoot, out rcDstRoot);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Invalidate the visible parts of the indicated rectangle.
		/// </summary>
		/// <param name="root">The root.</param>
		/// <param name="xsLeft">The xs left.</param>
		/// <param name="ysTop">The ys top.</param>
		/// <param name="dxsWidth">Width of the DXS.</param>
		/// <param name="dysHeight">Height of the dys.</param>
		/// ------------------------------------------------------------------------------------
		public void InvalidateRect(IVwRootBox root, int xsLeft, int ysTop, int dxsWidth,
			int dysHeight)
		{
			CheckDisposed();

			if (Pages == null)
				return; // layout has not happened and nothing has been drawn yet.
			List<Rect> rects = InvalidRects(root, xsLeft, ysTop, dxsWidth, dysHeight,
				AutoScrollPosition);

			foreach (Rectangle r in rects)
			{
				r.Inflate(2, 2); // Allows for small rounding errors.
				Invalidate(r, true);
				//using (Graphics g = this.CreateGraphics())
				//{
				//    g.FillRectangle(new SolidBrush(Color.Red), r);
				//    System.Threading.Thread.Sleep(1000);
				//}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Given the same arguments as InvalidateRect, return a collection of rectangles, relative to
		/// the client area of the publication window, that need to be invalidated. This method
		/// is separated out mainly for testing.
		/// </summary>
		/// <param name="root">The root.</param>
		/// <param name="xsLeft">The xs left.</param>
		/// <param name="ysTop">The ys top.</param>
		/// <param name="dxsWidth">Width of the DXS.</param>
		/// <param name="dysHeight">Height of the dys.</param>
		/// <param name="scrollPosition">The AutoScrollPosition of the containing publication</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public List<Rect> InvalidRects(IVwRootBox root, int xsLeft, int ysTop,
			int dxsWidth, int dysHeight, Point scrollPosition)
		{
			CheckDisposed();

			IVwLayoutStream strm = root as IVwLayoutStream;
			Debug.Assert(strm != null);
			List<Rect> result = new List<Rect>();
			foreach (Page page in Pages)
			{
				if (page.NeedsLayout)
					continue;

				bool fDone = false;
				foreach (PageElement pe in page.PageElements)
				{
					if (pe.m_stream != strm)
						continue;
					// if the invalid region starts after the bottom of this page, skip it
					if (ysTop >= pe.OffsetToTopPageBoundary + pe.LocationOnPage.Height)
						continue;
					// If the invalid region is entirely above this page we are done.
					if (ysTop + dysHeight < pe.OffsetToTopPageBoundary - pe.OverlapWithPreviousElement)
					{
						fDone = true; // but don't break this loop in case elements are out of order
					}
					else
					{
						// Invalid rectangle initially relative to document, then to page element
						Rectangle invalidRectOnPageElt = Rectangle.FromLTRB(
							ConvertPrintEltOffsetToPixelsX(xsLeft, DpiXScreen),
							ConvertPrintEltOffsetToPixelsY(ysTop - (pe.OffsetToTopPageBoundary - pe.OverlapWithPreviousElement), DpiYScreen),
							ConvertPrintEltOffsetToPixelsX(xsLeft + dxsWidth, DpiXScreen),
							ConvertPrintEltOffsetToPixelsY(ysTop - (pe.OffsetToTopPageBoundary - pe.OverlapWithPreviousElement) + dysHeight,
								DpiYScreen)
							);
						if (invalidRectOnPageElt.IsEmpty)
							continue;
						Rectangle invalidRectOnDisplay = invalidRectOnPageElt;
						// First make it relative to page.
						invalidRectOnDisplay.Offset(
							ConvertPrintDistanceToTargetX(pe.LocationOnPage.Left, DpiXScreen),
							ConvertPrintDistanceToTargetY(pe.LocationOnPage.Top, DpiYScreen));
						// Now correct for scroll offset and previous pages.
						invalidRectOnDisplay.Offset(scrollPosition.X,
							scrollPosition.Y + IndexOfPage(page) *
							PageHeightPlusGapInScreenPixels);
						result.Add(invalidRectOnDisplay);
					}
				}
				if (fDone)
					break;
			}
			return result;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Scroll the given or current selection into view.
		/// </summary>
		/// <param name="sel"></param>
		/// <param name="ssoFlag"></param>
		/// ------------------------------------------------------------------------------------
		public bool ScrollSelectionIntoView(IVwSelection sel, VwScrollSelOpts ssoFlag)
		{
			CheckDisposed();

			IVwSelection vwsel = sel;
			if (vwsel == null)
			{
				if (FocusedRootBox != null)
					vwsel = FocusedRootBox.Selection;
			}

			if (vwsel == null)
				return false; // nothing to do, no selection

			IVwGraphics vg = PrinterGraphics;
			try
			{
				// Loop until we have figured a scroll offset and confirmed the pages that
				// are visible there are laid out ready to draw.
				for (; ; )
				{
					// We pass this as source AND destination rectangle to get the selection
					// location relative to the whole document in printer coordinates.
//					Rect rcSrc = new Rect(0, 0, (int)DpiXPrinter, (int)DpiYPrinter);
					bool fEndBeforeAnchor;
					Rectangle rcSelPrinter;
					int clientWidthPrinter = ConvertScreenDistanceToPrinterX(ClientRectangle.Width,
						DpiXScreen);
					rcSelPrinter = GetSelectionRectangle(vwsel, vg, out fEndBeforeAnchor);
					// At this point, rcIdeal is a rectangle that we want on the screen, in
					// printer coordinates relative to the top of the document.
					IVwLayoutStream stream = (IVwLayoutStream)(vwsel.RootBox);
					int iDivTop; // index of division containing top of ideal rectangle
					int dysPageTopScreen; // top of page to top of rectangle in screen coords.
					bool layedOutPage;
					Page pageTop = PageFromPrinterY(rcSelPrinter.Left, rcSelPrinter.Top, false, stream,
						out dysPageTopScreen, out iDivTop, out layedOutPage);
					if (layedOutPage)
						continue;
					int iDivBottom; // index of division containing bottom of ideal rectangle
					int dysPageBottomScreen; // top of page to bottom of rectangle in screen coords.
					Page pageBottom = PageFromPrinterY(rcSelPrinter.Right, rcSelPrinter.Bottom, true, stream,
						out dysPageBottomScreen, out iDivBottom, out layedOutPage);
					if (layedOutPage)
						continue;
					// Enhance: this happens when moving within a secondary stream when the page that
					// will eventually contain the footnote (or whatever) has not been laid out.
					// This generic code has no idea which page will eventually contain the anchor for
					// this object.
					// Options:
					// 1. Leave it like this...fail to make the selection visible. (Maybe dialog?)
					// 2. Create real pages sequentially through the document until PageFromPrinterY
					//	succeeds. (This might take a while!).
					// 3. Make use of a virtual function, the default version of which fails,
					//	which can be asked, given a selection in a secondary stream, for a selection at
					//	the corresponding anchor. (This might be useful for other purposes...could be a
					//	method of the configurer.) Then we lay out the page that contains the anchor,
					//	after which we should be able to retry PageFromPrinterY successfully.
					if (pageTop == null || pageBottom == null)
						return false;
					// If those aren't properly laid-out pages, make them so and try again.
					// We repeat all the logic, because making them real may have expanded lazy
					// boxes and invalidated all the positions we worked out.
					// The process must terminate, because eventually (in a worst case) all pages are
					// laid out, all lazy boxes expanded. In practice it should terminate much sooner.
					if (EnsureRealBoxAt(pageTop) || EnsureRealBoxAt(pageBottom))
						continue;
					Page oldPageBeingLaidOut = m_pageBeingLaidOut;
					try
					{
						m_pageBeingLaidOut = pageTop;
						if (pageTop.LayOutIfNeeded())
							continue;
						m_pageBeingLaidOut = pageBottom;
						if (pageBottom.LayOutIfNeeded())
							continue;
					}
					finally
					{
						// We need to restore the page that was being laid out if we got here
						// during a layout in another place like PrepareToDrawPages()
						m_pageBeingLaidOut = oldPageBeingLaidOut;
					}
					int indexOfTopPage = IndexOfPage(pageTop);
					int indexOfBottomPage = indexOfTopPage;
					if (pageTop != pageBottom)
						indexOfBottomPage = IndexOfPage(pageBottom);
					int ysTopOfTopPageScreen = indexOfTopPage * PageHeightPlusGapInScreenPixels;
					int ysTopOfBottomPageScreen = indexOfBottomPage *
						PageHeightPlusGapInScreenPixels;
					int ysTopOfIdealScreen = dysPageTopScreen + ysTopOfTopPageScreen;
					int ysBottomOfIdealScreen = dysPageBottomScreen + ysTopOfBottomPageScreen;
					int scrollPosScreen = -AutoScrollPosition.Y;
					int clientHeightScreen = ClientRectangle.Height;
					// The amount we need to scroll by. This gets subtracted from the distance
					// we are scrolled (autoscrollposition.Y = -autoscrollposition.Y -
					// dysScrollPos), so a positive value causes the document to move up in the
					// window (thumb moves down).
					int dysScrollPosScreen = 0; // distance we need to scroll (positive = down)
					// The screen height of the rectangle we want to be visible.
					int dysIdealHeightScreen = ysBottomOfIdealScreen - ysTopOfIdealScreen;
					// The amount of space to spare...what we have to play with as we
					// decide where on the screen to try to place the rectangle we want to see.
					int dysSlackScreen = clientHeightScreen - dysIdealHeightScreen;
					// Reduce the height of the rectangle we want to make visible,if necessary, so
					// it fits on the client window. Reduce it at the 'anchor' not the 'end'.
					if (dysSlackScreen < 0)
					{
						if (!fEndBeforeAnchor)
						{
							ysTopOfIdealScreen -= dysSlackScreen;
						}
						else
							ysBottomOfIdealScreen += dysSlackScreen;
						dysIdealHeightScreen = clientHeightScreen;
						dysSlackScreen = 0;
					}
					if (rcSelPrinter.Width > clientWidthPrinter)
					{
						int delta = rcSelPrinter.Width - clientWidthPrinter;
						rcSelPrinter.Width = rcSelPrinter.Width - delta;
					}
					if (scrollPosScreen + clientHeightScreen < ysBottomOfIdealScreen)
					{
						// need to move doc up in window: dysScrollPos is negative
						dysScrollPosScreen = scrollPosScreen + clientHeightScreen - ysBottomOfIdealScreen;
					}
					else if (scrollPosScreen > ysTopOfIdealScreen)
					{
						// need to move doc down in window: dysScrollPos is positive.
						dysScrollPosScreen = scrollPosScreen - ysTopOfIdealScreen;
					}
					if (dysScrollPosScreen == 0)
					{
						// No need to move!! Unless...maybe we moved a long way, and redrawing
						// is going to expand a lazy box above our selection on the screen, and
						// it will grow so much we can't see the selection after all...
						// This ensures that everything in the client rectangle is real boxes.
						PrepareToDrawPages(0, clientHeightScreen);
						if (!vwsel.IsValid)
						{
							// At least one case where it might not be is typing in a footnote using smushing.
							// If the height of the footnote paragraph changes it gets regenerated. A new substitute
							// selection is created, but the old actual selection object isn't valid.
							// Of course, it's POSSIBLE that the selection we're trying to scroll isn't the
							// focussed root box's one, but that's the best guess I can see to make. It's right
							// for the only case so far where this has happened.
							vwsel = FocusedRootBox.Selection;
						}
						Rect rcPrimary2 = GetSelectionRectangle(vwsel, vg, out fEndBeforeAnchor);
						if (rcPrimary2.top != rcSelPrinter.Top)
							continue; // PrepareToDrawPages expanded something, try again.
						return true;
					}
					if (dysScrollPosScreen > 0)
					{
						// doc moving down. dysScrollPos as currently computed will put the rectangle
						// we want to see at the very top of the screen. That is good both for
						// case default (it's the smallest movement that works) and for a request
						// to put it at the top. Since it's at the top, no previous page can be
						// visible above it, so we will just use this value.
						// It we wanted to, say, center it, we would add half of dysSlack, then
						// make sure no previous page is showing by taking the min of that and
						// the distance to move the top of page to top of screen.
					}
					else
					{
						// doc moving up. (-dysScrollPos) is the distance to align the
						// bottom of the ideal rectangle with the bottom of the screen.
						// That is our default. If aligning to the top, we want to move more.
						if (ssoFlag == VwScrollSelOpts.kssoNearTop)
						{
							// To align the tops instead, make dysScrollPos more negative
							// by the difference between the rectangle heights.
							dysScrollPosScreen -= dysSlackScreen;
						}
						// Now we need to check that no previous page is visible. We avoid showing
						// a previous page partly because it tends to put a lot of white space
						// on the screen that may not be helpful to the user, but mainly because
						// it is possible that the previous page has not been laid out, and
						// laying it out will expand lazy boxes and mess up our prediction of
						// where the ideal rectangle will appear. We could all PrepareToDraw to
						// check whether this happens and then 'continue' to try again if
						// necessary, but we suspect it would make things jerky.
						int ysTopOfTopPageScrn = ysTopOfTopPageScreen + AutoScrollPosition.Y;
						if (ysTopOfTopPageScrn + dysScrollPosScreen > 0)
						{
							// Need to scroll more so that no previous page shows.
							// Scroll to put top of page exactly at top of screen.
							dysScrollPosScreen = -ysTopOfTopPageScrn;
						}
					}
					// OK, moving by dysScrollPos should make the selection visible
					// without expanding anything lazy above it. Do it and end the loop!
					AutoScrollPosition =
						new Point(-AutoScrollPosition.X,
						-AutoScrollPosition.Y - dysScrollPosScreen);
					return true;
				}
			}
			finally
			{
				ReleaseGraphics(vg);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the selection rectangle (in printer pixels).
		/// </summary>
		/// <param name="vwsel">The selection.</param>
		/// <param name="vg">The VwGraphics object.</param>
		/// <param name="fEndBeforeAnchor"><c>true</c> if the end of the selection is before
		/// the anchor.</param>
		/// <returns>The rectangle of the selection in printer pixels.</returns>
		/// ------------------------------------------------------------------------------------
		internal Rectangle GetSelectionRectangle(IVwSelection vwsel, IVwGraphics vg,
			out bool fEndBeforeAnchor)
		{
			Rectangle rcSelPrinter;

			if (vwsel == null)
				vwsel = FocusedRootBox.Selection;

			if (vwsel == null)
			{
				Debug.Fail("Couldn't find a selection");
				fEndBeforeAnchor = false;
				return Rectangle.Empty;
			}

			// We pass this as source AND destination rectangle to get the selection
			// location relative to the whole document in printer coordinates.
			Rect rcSrc = new Rect(0, 0, (int)DpiXPrinter, (int)DpiYPrinter);
			// Main or only place the selection is (in printer coords,
			// relative to the rootbox!)
			// Secondary place if the selection is a split cursor.
			// true if it is split (if false, do NOT use rcSecondary, it is meaningless).

			// true if end of selection comes before anchor, so the BOTTOM is what
			// we want to be sure to see.
			Rect rcPrimary, rcSecondary;
			bool fSplit;
			vwsel.Location(vg, rcSrc, rcSrc, out rcPrimary, out rcSecondary, out fSplit,
				out fEndBeforeAnchor);
			rcSelPrinter = Rectangle.FromLTRB(rcPrimary.left, rcPrimary.top,
				rcPrimary.right, rcPrimary.bottom);
			int clientWidthPrinter = ConvertScreenDistanceToPrinterX(
				ClientRectangle.Width, DpiXScreen);
			int clientHeightPrinter = ConvertScreenDistanceToPrinterY(
				ClientRectangle.Height, DpiYScreen);
			if (fSplit)
			{
				// try to get the secondary in there also
				Rectangle rcSel2Printer = Rectangle.FromLTRB(
					Math.Min(rcSelPrinter.Left, rcSecondary.left),
					Math.Min(rcSelPrinter.Top, rcSecondary.top),
					Math.Max(rcSelPrinter.Right, rcSecondary.right),
					Math.Max(rcSelPrinter.Bottom, rcSecondary.bottom));
				if (rcSel2Printer.Width <= clientWidthPrinter &&
					rcSel2Printer.Height <= clientHeightPrinter)
					rcSelPrinter = rcSel2Printer;
			}

			return rcSelPrinter;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a string representation of the object suitable to put on the clipboard.
		/// </summary>
		/// <param name="guid">The guid of the object in the DB</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public string get_TextRepOfObj(ref Guid guid)
		{
			CheckDisposed();
			return RootSiteEditingHelper.TextRepOfObj(m_cache, guid);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a new object, given a text representation (e.g., from the clipboard).
		/// </summary>
		/// <param name="bstrText">Text representation of object</param>
		/// <param name="selDst">Provided for information in case it's needed to generate
		/// the new object (E.g., footnotes might need it to generate the proper sequence
		/// letter)</param>
		/// <param name="kodt">The object data type to use for embedding the new object
		/// </param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public Guid get_MakeObjFromText(string bstrText, IVwSelection selDst, out int kodt)
		{
			CheckDisposed();
			return RootSiteEditingHelper.MakeObjFromText(m_cache, bstrText, selDst, out kodt);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Releases the graphics.
		/// </summary>
		/// <param name="root">The root.</param>
		/// <param name="vg">The vg.</param>
		/// ------------------------------------------------------------------------------------
		public void ReleaseGraphics(IVwRootBox root, IVwGraphics vg)
		{
			CheckDisposed();

			ReleaseGraphics(vg);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a VwGraphics object at printer resolution, ready for layout operations.
		/// </summary>
		/// <param name="_Root"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public IVwGraphics get_LayoutGraphics(IVwRootBox _Root)
		{
			CheckDisposed();

			return PrinterGraphics;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a graphics suitable for measuring on the screen.
		/// </summary>
		/// <param name="_Root"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public IVwGraphics get_ScreenGraphics(IVwRootBox _Root)
		{
			CheckDisposed();

			return ScreenGraphics;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Member ScreenToClient
		/// </summary>
		/// <param name="_Root">_Root</param>
		/// <param name="_pnt">_pnt</param>
		/// ------------------------------------------------------------------------------------
		public void ScreenToClient(IVwRootBox _Root, ref System.Drawing.Point _pnt)
		{
			CheckDisposed();

			// TODO:  Add PublicationControl.ScreenToClient implementation
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Selections has changed.
		/// </summary>
		/// <param name="rootb">The rootb.</param>
		/// <param name="vwselNew">The vwsel new.</param>
		/// ------------------------------------------------------------------------------------
		public void SelectionChanged(IVwRootBox rootb, IVwSelection vwselNew)
		{
			CheckDisposed();

			if (rootb == EditingHelper.EditedRootBox)
			{
				Debug.Assert(vwselNew == rootb.Selection);
				EditingHelper.SelectionChanged();
			}
		}
		#endregion

		#region Font/line-space scaling methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Applies the overrides to the in memory stylesheet
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ApplyPubOverrides()
		{
			Debug.Assert(m_publication != null);
			// Update the styles for the publication using the base character size and line spacing.
			m_stylesheet = new InMemoryStyleSheet();
			int pubFontSize = m_publication.BaseFontSize;
			int pubLineSpacing = m_publication.BaseLineSpacing;

			// If font size or line spacing is not specified in the publication, get it from the
			// style sheet.
			if (pubFontSize == 0)
				pubFontSize = DefaultFontSize;

			if (pubLineSpacing == 0)
				pubLineSpacing = DefaultLineHeight;

			ApplyPubOverrides(pubFontSize, pubLineSpacing);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Applies the overrides on styles in the Scripture publication.
		/// </summary>
		/// <param name="pubCharSize">Base character size to use for normal in the stylesheet
		/// in millipoints.</param>
		/// <param name="pubLineSpacing">The line spacing to use for normal in the stylesheet
		/// in millipoints (negative for exact line spacing, which is all we really support
		/// properly).</param>
		/// ------------------------------------------------------------------------------------
		public void ApplyPubOverrides(decimal pubCharSize, decimal pubLineSpacing)
		{
			if (!m_fApplyStyleOverrides)
				return;

			using (new WaitCursor(this))
			{
				Debug.Assert(m_origStylesheet != null);

				if (!(m_stylesheet is InMemoryStyleSheet))
					m_stylesheet = new InMemoryStyleSheet();

				InMemoryStyleSheet printLayoutStylesheet = (InMemoryStyleSheet)m_stylesheet;

				// Get current values from the normal style to initialize the in-memory stylesheet.
				int prevNormalFontSize = m_origStylesheet.NormalFontSize;
				int prevNormalLineSpacing = GetNormalLineHeight(m_origStylesheet);

				// Determine the percentage that the font and line spacing need to change
				// (in proportion to the change in normal).
				decimal fontSizeChangePercent = pubCharSize / prevNormalFontSize;
				decimal lineSpacingChangePercent = prevNormalLineSpacing != 0 ?
					Math.Abs(pubLineSpacing / prevNormalLineSpacing) : 0;

				// Apply overrides to normal.
				printLayoutStylesheet.AddIntPropOverride(m_stylesheet.GetDefaultBasedOnStyleName(),
					FwTextPropType.ktptFontSize, FwTextPropVar.ktpvMilliPoint, (int)pubCharSize);
				printLayoutStylesheet.AddIntPropOverride(m_stylesheet.GetDefaultBasedOnStyleName(),
					FwTextPropType.ktptLineHeight, FwTextPropVar.ktpvMilliPoint, (int)pubLineSpacing);

				// For any style that overrides normal font size or line height,
				// calculate apply the difference between the normal style and the given
				// style.
				for (int iStyle = 0; iStyle < m_origStylesheet.CStyles; iStyle++)
				{
					string styleName = m_origStylesheet.get_NthStyleName(iStyle);
					if (styleName == m_stylesheet.GetDefaultBasedOnStyleName())
						continue;

					//bool fStyleLineMustBeHandled = false;
					ITsTextProps styleProps = m_origStylesheet.GetStyleRgch(0, styleName);
					int var;
					int lineHeight = styleProps.GetIntPropValues(
						(int)FwTextPropType.ktptLineHeight, out var);
					// Make sure line height is positive.
					lineHeight = lineHeight == -1 ? -1 : Math.Abs(lineHeight);

					// Apply any applicable overrides to font size.
					ScaleProperty(printLayoutStylesheet, fontSizeChangePercent, styleName,
						styleProps, FwTextPropType.ktptFontSize);

					if (m_fUniformLineSpacing || lineSpacingChangePercent == 0)
					{
						// Add override for line height as a negative value for exact line spacing
						printLayoutStylesheet.AddIntPropOverride(styleName, FwTextPropType.ktptLineHeight,
							FwTextPropVar.ktpvMilliPoint, (int)pubLineSpacing);
						// REVIEW: Should we zero out any space before or after for the correction printout view to
						// achieve consistent line spacing?
					}
					else
					{
						// Apply any applicable overrides to line spacing, space before and space after.
						ScaleProperty(printLayoutStylesheet, lineSpacingChangePercent, styleName, styleProps,
							FwTextPropType.ktptLineHeight);
						ScaleProperty(printLayoutStylesheet, lineSpacingChangePercent, styleName, styleProps,
							FwTextPropType.ktptSpaceBefore);
						ScaleProperty(printLayoutStylesheet, lineSpacingChangePercent, styleName, styleProps,
							FwTextPropType.ktptSpaceAfter);
					}
				}

				printLayoutStylesheet.Init(m_cache, m_origStylesheet.RootObjectHvo,
					m_origStylesheet.StyleListTag);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the original stylesheet gets initialized.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		private void OnInitOriginalStylesheet(object sender, EventArgs e)
		{
			ApplyPubOverrides();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Scales the given property based on the scaling factor and sets it in the in-memory
		/// stylesheet.
		/// </summary>
		/// <param name="stylesheet">The in-memory stylesheet used for overriding properties.
		/// </param>
		/// <param name="scalingFactor">The scaling factor.</param>
		/// <param name="styleName">Name of the style.</param>
		/// <param name="styleProps">The style props.</param>
		/// <param name="prop">The type of property to be scaled.</param>
		/// ------------------------------------------------------------------------------------
		private static void ScaleProperty(InMemoryStyleSheet stylesheet,
			decimal scalingFactor, string styleName, ITsTextProps styleProps,
			FwTextPropType prop)
		{
			int var;
			int propValue = styleProps.GetIntPropValues((int)prop, out var);
			if (propValue != -1 && var == (int)FwTextPropVar.ktpvMilliPoint)
			{
				if (prop == FwTextPropType.ktptFontSize)
				{
					Debug.Assert(propValue >= 4);
					// Calculate new value for this style, rounding to reasonable value.
					propValue = ScaleAndRoundProp(scalingFactor, propValue, 1000);
					// Adjusted font size must be at least 4 pts
					propValue = Math.Max(propValue, 4000);
				}
				else
					propValue = ScaleAndRoundProp(scalingFactor, propValue, 1);

				stylesheet.AddIntPropOverride(styleName, prop,
					FwTextPropVar.ktpvMilliPoint, propValue);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Scales the given property and rounds to nearest point.
		/// </summary>
		/// <param name="scalingFactor">The scaling factor.</param>
		/// <param name="propValue">The prop value.</param>
		/// <param name="roundToTheNearest">The rounding precision, e.g. 1000 rounds to the
		/// nearest point.</param>
		/// <returns>the scaled property value</returns>
		/// ------------------------------------------------------------------------------------
		private static int ScaleAndRoundProp(decimal scalingFactor, decimal propValue,
			decimal roundToTheNearest)
		{
			propValue = propValue * scalingFactor;
			return (int)(decimal.Round((propValue / roundToTheNearest), 0,
				MidpointRounding.AwayFromZero) * roundToTheNearest);
		}
		#endregion
	}
}

// todo: compute and set autoscroll range...keep consistent with current #pages and current zoom
