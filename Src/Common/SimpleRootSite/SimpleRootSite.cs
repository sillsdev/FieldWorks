// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2004' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: SimpleRootSite.cs
// Responsibility: FW Team
// --------------------------------------------------------------------------------------------
using Accessibility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Printing;
using System.Runtime.InteropServices;
using System.Diagnostics;

using SIL.Utils;
using SIL.FieldWorks.Common.COMInterfaces;
using XCore;
using System.Windows.Automation.Provider;
using SIL.CoreImpl;

namespace SIL.FieldWorks.Common.RootSites
{
	#region SuspendDrawing class
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Suspends drawing the parent object
	/// </summary>
	/// <example>
	/// REQUIRED usage:
	/// using(new SuspendDrawing(Handle)) // this sends the WM_SETREDRAW message to the window
	/// {
	///		doStuff();
	/// } // this resumes drawing the parent object
	/// </example>
	/// ------------------------------------------------------------------------------------
	public class SuspendDrawing : IFWDisposable
	{
		private IRootSite m_Parent;

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Suspend drawing of the parent.
		/// </summary>
		/// <param name="parent">Containing rootsite</param>
		/// --------------------------------------------------------------------------------
		public SuspendDrawing(IRootSite parent)
		{
			m_Parent = parent;
			m_Parent.AllowPainting = false;
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
		~SuspendDrawing()
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
				if (m_Parent != null)
					m_Parent.AllowPainting = true;
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_Parent = null;

			m_isDisposed = true;
		}

		/// <summary>
		/// Throw if the IsDisposed property is true
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException("SuspendDrawing", "This object is being used after it has been disposed: this is an Error.");
		}

		#endregion IDisposable & Co. implementation
	}
	#endregion

	#region SimpleRootSite class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Base class for hosting a view in an application.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class SimpleRootSite : UserControl, IVwRootSite, IRootSite, IxCoreColleague,
		IEditingCallbacks, IReceiveSequentialMessages, IMessageFilter, IFWDisposable
	{
		#region Class LcidKeyboardStatus
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Holds information about a specific keyboard, especially for IMEs (e.g. whether
		/// English input mode is selected). This is necessary to restore the current setting
		/// when switching between fields with differing keyboards. The user expects that a
		/// keyboard keeps its state between fields.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected class LcidKeyboardMode
		{
			// we use Nullable<T> for our two fields
			private int? m_ConversionMode;
			private int? m_SentenceMode;

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="LcidKeyboardMode"/> class.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public LcidKeyboardMode()
			{
				m_ConversionMode = null;
				m_SentenceMode = null;
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="LcidKeyboardMode"/> class.
			/// </summary>
			/// <param name="conversionMode">The conversion mode.</param>
			/// <param name="sentenceMode">The sentence mode.</param>
			/// --------------------------------------------------------------------------------
			public LcidKeyboardMode(int conversionMode, int sentenceMode)
			{
				m_ConversionMode = conversionMode;
				m_SentenceMode = sentenceMode;
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets a value indicating whether the conversion or sentence mode properties have
			/// a value.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public bool HasValue
			{
				get { return m_ConversionMode.HasValue || m_SentenceMode.HasValue; }
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets or sets the conversion mode.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public int ConversionMode
			{
				get { return m_ConversionMode ?? 0; }
				set { m_ConversionMode = value; }
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets or sets the sentence mode.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public int SentenceMode
			{
				get { return m_SentenceMode ?? 0; }
				set { m_SentenceMode = value; }
			}
		}
		#endregion

		#region Events
		/// <summary>
		/// This event notifies you that the right mouse button was clicked,
		/// and gives you the click location, and the selection at that point.
		/// </summary>
		public event FwRightMouseClickEventHandler RightMouseClickedEvent;

		/// <summary>This event gets fired when the AutoScrollPosition value changes</summary>
		public event ScrollPositionChanged VerticalScrollPositionChanged;
		#endregion Events

		#region Member variables
		/// <summary>Value for the available width to tell the view that we want to do a
		/// layout</summary>
		public const int kForceLayout = -50000;
		/// <summary>Tag we use for user prompts</summary>
		/// <remarks>The process for setting up a user prompt has several aspects.
		/// 1. When displaying the property, if it is empty, call vwenv.AddProp(SimpleRootSite.kTagUserPrompt, this, frag);
		/// Also NoteDependency on the empty property.
		/// 2. Optionally, if it is NOT empty, to make the prompt appear if it becomes empty,
		/// use NoteStringValDependency to get a regenerate when the value is no longer empty.
		/// 3. Implement DisplayVariant to recognize kTagUserPrompt and insert the appropriate prompt.
		/// Usually this is all DisplayVariant is used for, so the frag argument can be used in any convenient way,
		/// for example, to indicate which prompt, or which writing system.
		/// Typically, the entire prompt should be given the property ktptUserPrompt (this suppresses various formatting commands).
		/// Typically, the text of the prompt should be given the ktptSpellCheck/DoNotCheck property.
		/// At the start of the prompt, insert "\u200B" (a zero-width space) in the desired writing system; this ensures
		/// that anything the user types will be in that writing system and that the right keyboard will be active.
		/// 4. Implement UpdateProp. See InterlinVc for an example. This is responsible to transfer any value typed
		/// over the prompt to the real property, and to restore the selection, which will be destroyed by
		/// updating the real property. It needs to save selection info, remove the kttpUserPrompt and doNotSpellCheck properties
		/// from what the user typed, set that as the value of the appropriate real property, and restore a selection that is
		/// similar but in the real property rather than in ktagUserPrompt. This should not be done while an IME composition
		/// is currently in progress (use IVwRootBox.IsCompositionInProgress), otherwise the composition will be terminated
		/// when the real property is updated and the selection is destroyed.
		/// 5. Implement SelectionChanged on the parent rootsite to notice that the selection is in a prompt
		/// and extend the selection to the whole property. The selection should not be extended while an IME composition
		/// is in progress, since the user prompt is about to be replaced by the real property.
		/// </remarks>
		public const int kTagUserPrompt = 1000000001;

		/// <summary>Property for indicating user prompt strings.
		/// This is an arbitrary number above 10,000.  Property numbers above 10,000 are
		/// "user-defined" and will be ignored by the property store.</summary>
		public const int ktptUserPrompt = 10537;

		/// <summary>If 0 we allow OnPaint to execute, if non-zero we don't perform OnPaint.
		/// This is used to prevent redraws from happening while we do a RefreshDisplay.
		/// This also sends the WM_SETREDRAW message.
		/// </summary>
		/// <remarks>Access to this variable should only be done through the property
		/// AllowPaint.</remarks>
		private int m_nAllowPaint = 0;

		/// <summary>
		/// Subclasses can set this flag to keep SimpleRootSite from handling OnPrint.
		/// </summary>
		protected bool SuppressPrintHandling { get; set; }

#if __MonoCS__
		/// <summary>
		/// This allows storing of the AutoScrollPosition when AllowPainting == false
		/// as on Mono Setting AutoScrollPosition causes a redraw even when AllowPainting == false
		/// </summary>
		private Point? cachedAutoScrollPosition = null;
#endif

		/// <summary>Used to draw the rootbox</summary>
		private IVwDrawRootBuffered m_vdrb;
		private bool m_haveCachedDrawForDisabledView = false;

		//The message filter is a major kludge to prevent a spurious WM_KEYUP for VK_CONTROL from
		// interrupting a mouse click.  We remember when it is installed with this bool.
		private bool m_messageFilterInstalled = false;

		/// <summary>
		/// This variable is set at the start of OnGotFocus, and thus notes the root site that
		/// most recently received the input focus. It is cleared to null at the end of
		/// OnLostFocus...but ONLY if it is equal to this. Reason: OnGotFocus for the new
		/// focus window apparently can happen BEFORE OnLostFocus for the old window. We detect
		/// that situation by the fact that, in OnLostFocus, this != g_focusRootSite.Target.
		/// We use a WeakReference just in case so that if for some reason this doesn't get
		/// set to null we don't keep a rootsite around.
		/// </summary>
		private static WeakReference g_focusRootSite = new WeakReference(null);
		private IContainer components;
		/// <summary>True to allow layouts to take place, false otherwise (We use this instead
		/// of SuspendLayout because SuspendLayout didn't work)</summary>
		protected bool m_fAllowLayout = true;

		/// <summary>True if we are waiting to do a refresh on the view (will be done when the view
		/// becomes visible); false otherwise</summary>
		protected bool m_fRefreshPending = false;

		/// <summary>True to show range selections when focus is lost; false otherwise</summary>
		protected bool m_fShowRangeSelAfterLostFocus = false;
		/// <summary>True if this is a "text box" (or "combo box"); false otherwise</summary>
		protected bool m_fIsTextBox = false;

		/// <summary>True if <see cref="MakeRoot"/> was called</summary>
		protected bool m_fRootboxMade = false;

		/// <summary>Manages the VwGraphics creation and useage</summary>
		protected GraphicsManager m_graphicsManager;

		/// <summary>The root box</summary>
		protected IVwRootBox m_rootb = null;

		/// <summary>handler for typing and other edit requests</summary>
		protected EditingHelper m_editingHelper;

		/// <summary>
		/// A writing system factory used to interpret data in the view. Subclasses of
		/// SimpleRootSite should set this before doing much with the root site. The RootSite
		/// subclass can obtain one automatically from its FdoCache.
		/// </summary>
		protected ILgWritingSystemFactory m_wsf;

		/// <summary>The width returned by GetAvailWidth() when the root box was last laid out,
		/// or a large negative number if it has never been successfully laid out.</summary>
		protected int m_dxdLayoutWidth;

		/// <summary>The zoom ratio</summary>
		protected float m_Zoom = 1;

		/// <summary>Contains horizontal and vertical dpi</summary>
		protected Point m_Dpi = new Point(96, 96);

		/// <summary>
		/// See <see cref="WsPending"/>.
		/// </summary>
		protected int m_wsPending;

		/// <summary>Keyman select language message</summary>
		protected static readonly uint s_wm_kmselectlang =
#if !__MonoCS__
 Win32.RegisterWindowMessage("WM_KMSELECTLANG");
#else
			// This Keyman stuff will not work on Linux.
			0;
#endif
		/// <summary>Keyman change keyboard message</summary>
		protected static readonly uint s_wm_kmkbchange =
#if !__MonoCS__
 Win32.RegisterWindowMessage("WM_KMKBCHANGE");
#else
			// This Keyman stuff will not work on Linux.
			0;
#endif

		/// <summary>height of an optional fixed header at the top of the client window.</summary>
		protected int m_dyHeader;

		/// <summary>list of drawing err messages that have been shown to the user</summary>
		protected static List<string> s_vstrDrawErrMsgs = new List<string>();

		// This is used for the LinkedFiles Link tooltip.
		//HWND m_hwndExtLinkTool;

		private System.Windows.Forms.Timer m_Timer;
		private int m_nHorizMargin = 2;

		/// <summary>The style sheet</summary>
		protected IVwStylesheet m_styleSheet;

		/// <summary>
		/// The mediator provided by XCoreColleague.Init. Not (currently) used by the root site,
		/// but saved and made accessible for clients.
		/// </summary>
		protected Mediator m_mediator;
		/// <summary>
		/// Supports the LayoutSizeChanged event by maintaining a list of who wants it.
		/// </summary>
		public event EventHandler LayoutSizeChanged;
		/// <summary>
		/// Flag used to prevent mouse move events from entering CallMouseMoveDrag multiple
		/// times before prior ones have exited.  Otherwise we get lines displayed multiple
		/// times while scrolling during a selection.
		/// </summary>
		private bool m_fMouseInProcess = false;

		/// <summary>Flag used to ensure that OnInputLangChanged gets registered once.</summary>
		private bool m_fRegisteredOnInputLangChanged = false;

#if !__MonoCS__
		/// <summary>Keep track of the keyboard states for the different LCIDs (writing systems).
		/// This variable can be static since the same keyboard should behave the same in all
		/// fields in this application.</summary>
		private static Dictionary<int, LcidKeyboardMode> s_KeyboardModes =
			new Dictionary<int, LcidKeyboardMode>();
#endif

		/// <summary>
		/// This is set true during processing of the OnPaint message. It serves to suppress
		/// certain behavior that ought not to happen during a paint.
		/// For example, when this root site is part of a group, and expanding lazy boxes
		/// changes the scroll range, we change the AutoScrollMinSize of the RootSiteGroup.
		/// This causes a Layout() of the RootSiteGroup, which among other things sends
		/// an OnSizeChanged to the original root site, which tries to make the selection
		/// visible, and can produce a recursive call to OnPaint.
		/// </summary>
		protected bool m_fInPaint;
		/// <summary>
		/// This is set true during processing of the OnLayout message. This is used to
		/// deal with re-entrant Window messages.
		/// </summary>
		protected bool m_fInLayout;

		/// <summary>We seem to get spurious OnSizeChanged messages when the size didn't
		/// really change... ignore them.</summary>
		private Size m_sizeLast;

		/// <summary>This gets around a bug in OnGotFocus generating a WM_INPUTLANGCHANGE
		/// message with the previous language value when we want to set our own that we know.
		/// If the user causes this message, we do want to change language/keyboard, but not
		/// if OnGotFocus causes the message.</summary>
		private bool m_fHandlingOnGotFocus = false;

		/// <summary>
		/// This tells the rootsite whether to attempt to construct the rootbox automatically
		/// when the window handle is created. For simple views, this is generally desirable,
		/// but views which are part of synchronously scrolling groups usually have their root
		/// objects set explicitly at the same time as the other views in their group.
		/// </summary>
		protected bool m_fMakeRootWhenHandleIsCreated = true;

		private int m_lastVerticalScrollPosition = 1;
		// Used to force certain events to occur sequentially. See comments on the class.
		private MessageSequencer m_messageSequencer;
		private bool m_fDisposed;

		private OrientationManager m_orientationManager;

#if __MonoCS__
		/// <summary> manage SimpleRootSite ibus interaction </summary>
		protected InputBusController m_inputBusController;
#endif
		private bool m_acceptsTab;
		private bool m_acceptsReturn;

		/// <summary>
		/// We suppress MouseMove when a paint is pending, since we don't want mousemoved to ask
		/// about a lazy box under the cursor and start expanding things; this can lead to recursive
		/// calls to paint, and hence to recursive calls to expand, and all kinds of pain (e.g., FWR-3103).
		/// </summary>
		public bool MouseMoveSuppressed { get; private set; }

		#endregion

		#region Constructor, Dispose, Designer generated code
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public SimpleRootSite()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			m_dxdLayoutWidth = kForceLayout; // Unlikely to be real current window width!
			m_wsPending = -1;
			BackColor = SystemColors.Window;
			//AllowScrolling = true;
			m_messageSequencer = new MessageSequencer(this);
			m_graphicsManager = CreateGraphicsManager();
			m_orientationManager = CreateOrientationManager();
			if (UIAutomationServerProviderFactory == null)
				UIAutomationServerProviderFactory = () => new SimpleRootSiteDataProvider(this);
			CreateInputBusController();
		}

#if DEBUG
		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		~SimpleRootSite()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}
#endif
		/// <summary> Create connection to IME - This is a NullOp on Windows. </summary>
		protected virtual void CreateInputBusController()
		{
			#if __MonoCS__
			m_inputBusController = new InputBusController(this, new IBusCommunicator());
			#endif
		}

		/// <summary>
		/// The default creates a normal horizontal orientation manager. Override to create one of the other
		/// classes as needed.
		/// </summary>
		/// <returns></returns>
		protected virtual OrientationManager CreateOrientationManager()
		{
			return new OrientationManager(this);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + "******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
#if __MonoCS__
				if (m_inputBusController != null)
				{
					m_inputBusController.Dispose();
					m_inputBusController = null;
				}
#endif
				// Do this here, before disposing m_messageSequencer,
				// as we still get messages during dispose.
				// Once the the base class has shut down the window handle,
				// we are good to go on.
				// If we find we getting messages during this call,
				// we will be forced to call DestroyHandle() first.
				// That is done part way through the base method code,
				// but it may not be soon enough, for all the re-entrant events
				// that keep showing up.
				m_fAllowLayout = false;
				DestroyHandle();
			}

			base.Dispose(disposing);

			if (disposing)
			{
				if (m_rootb != null)
					CloseRootBox();

				if (m_Timer != null)
				{
					m_Timer.Stop();
					m_Timer.Tick -= OnTimer;
					m_Timer.Dispose();
				}

				// Remove the filter when we are disposed now.
				if (m_messageFilterInstalled)
				{
					Application.RemoveMessageFilter(this);
					m_messageFilterInstalled = false;
				}

				if (m_editingHelper != null)
					m_editingHelper.Dispose();
				if (m_messageSequencer != null)
					m_messageSequencer.Dispose();
				if (m_graphicsManager != null)
				{
					// Uninit() first in case we're in the middle of displaying something.  See LT-7365.
					m_graphicsManager.Uninit();
					m_graphicsManager.Dispose();
				}
				if (components != null)
					components.Dispose();
			}

			if (m_vdrb != null && Marshal.IsComObject(m_vdrb))
				Marshal.ReleaseComObject(m_vdrb);
			m_vdrb = null;
			if (m_styleSheet != null && Marshal.IsComObject(m_styleSheet))
				Marshal.ReleaseComObject(m_styleSheet);
			if (m_rootb != null && Marshal.IsComObject(m_rootb))
				Marshal.ReleaseComObject(m_rootb);
			m_rootb = null;
			m_styleSheet = null;
			m_graphicsManager = null;
			m_editingHelper = null;
			m_Timer = null;
			m_mediator = null;
			m_wsf = null;
			m_messageSequencer = null;

			// Don't do it here.
			//base.Dispose( disposing );

			m_fDisposed = true;
		}

		/// <summary>
		/// Throw if the IsDisposed property is true
		/// </summary>
		public void CheckDisposed()
		{
			if (m_fDisposed)
				throw new ObjectDisposedException(ToString(), "This object is being used after it has been disposed: this is an Error.");
		}

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.m_Timer = new System.Windows.Forms.Timer(this.components);
			//
			// RootSite
			//
			this.Name = "SimpleRootSite";
		}
		#endregion

		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets an image that can be used to represent graphically an image that cannot be
		/// found (similar to what IE does).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static Bitmap ImageNotFoundX
		{
			get { return Properties.Resources.ImageNotFoundX; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the graphics manager.
		/// </summary>
		/// <remarks>We do this in a method for testing.</remarks>
		/// <returns>A new graphics manager.</returns>
		/// ------------------------------------------------------------------------------------
		protected virtual GraphicsManager CreateGraphicsManager()
		{
			return new GraphicsManager(this);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether it's possible to do meaningful layout.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual bool OkayToLayOut
		{
			get { return OkayToLayOutAtCurrentWidth; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If layout width is less than 2 pixels, probably the window has not received its
		/// initial OnSize message yet, and we can't do a meaningful layout.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OkayToLayOutAtCurrentWidth
		{
			get { return m_dxdLayoutWidth >= 2 || m_dxdLayoutWidth == kForceLayout; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets whether or not to show range selections when focus is lost
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual bool ShowRangeSelAfterLostFocus
		{
			get
			{
				CheckDisposed();
				return m_fShowRangeSelAfterLostFocus;
			}
			set
			{
				CheckDisposed();
				m_fShowRangeSelAfterLostFocus = value;
				if (!Focused && m_rootb != null)
					UpdateSelectionEnabledState(null);
			}
		}

		/// <summary>
		/// Gets/sets whether or not this is a "TextBox" (possibly embedded in a "ComboBox").
		/// </summary>
		public bool IsTextBox
		{
			get
			{
				CheckDisposed();
				return m_fIsTextBox;
			}
			set
			{
				CheckDisposed();
				m_fIsTextBox = value;
				if (!Focused && m_rootb != null)
					UpdateSelectionEnabledState(null);
			}
		}
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the horizontal margin.
		/// Note: this is always considered the margin to either SIDE of the text; thus, in
		/// effect it becomes a vertical margin when displaying vertical text.
		/// </summary>
		/// <value>A <c>int</c> that represents the horizontal margin.</value>
		/// -----------------------------------------------------------------------------------
		[DefaultValue(2)]
		protected internal virtual int HorizMargin
		{
			get
			{
				CheckDisposed();
				return m_nHorizMargin;
			}
			set
			{
				CheckDisposed();
				m_nHorizMargin = value;
			}
		}

		/// <summary>
		/// Retrieve the mediator, typically obtained from xCoreColleague.Init, though somtimes
		/// it may be set directly.
		/// </summary>
		public XCore.Mediator Mediator
		{
			get
			{
				CheckDisposed();
				return m_mediator;
			}
			set
			{
				CheckDisposed();
				m_mediator = value;
			}
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The paragraph style name for the current selection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public virtual string CurrentParagraphStyle
		{
			get
			{
				CheckDisposed();
				if (DesignMode)
					return string.Empty;

				return EditingHelper.GetParaStyleNameFromSelection();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public virtual bool AllowLayout
		{
			get
			{
				CheckDisposed();
				return m_fAllowLayout;
			}
			set
			{
				CheckDisposed();
				m_fAllowLayout = value;
				if (m_fAllowLayout)
					PerformLayout();
			}
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// If we need to make a selection, but we can't because edits haven't been updated in the
		/// view, this method requests creation of a selection after the unit of work is complete.
		/// Derived classes should implement this if they have any hope of supporting multi-
		/// paragraph editing.
		/// </summary>
		/// <param name="rootb">The rootbox</param>
		/// <param name="ihvoRoot">Index of root element</param>
		/// <param name="cvlsi">count of levels</param>
		/// <param name="rgvsli">levels</param>
		/// <param name="tagTextProp">tag or flid of property containing the text (TsString)</param>
		/// <param name="cpropPrevious">number of previous occurrences of the text property</param>
		/// <param name="ich">character offset into the text</param>
		/// <param name="wsAlt">The id of the writing system for the selection.</param>
		/// <param name="fAssocPrev">Flag indicating whether to associate the insertion point
		/// with the preceding character or the following character</param>
		/// <param name="selProps">The selection properties.</param>
		/// --------------------------------------------------------------------------------
		public virtual void RequestSelectionAtEndOfUow(IVwRootBox rootb, int ihvoRoot,
			int cvlsi, SelLevInfo[] rgvsli, int tagTextProp, int cpropPrevious, int ich,
			int wsAlt, bool fAssocPrev, ITsTextProps selProps)
		{
			throw new NotImplementedException("Method RequestSelectionAtEndOfUow is not implemented.");
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// If we need to make a selection, but we can't because edits haven't been updated in
		/// the view, this method requests creation of a selection after the unit of work is
		/// complete. It will also scroll the selection into view.
		/// Derived classes should implement this if they have any hope of supporting multi-
		/// paragraph editing.
		/// </summary>
		/// <param name="helper">The selection to restore</param>
		/// --------------------------------------------------------------------------------
		public virtual void RequestVisibleSelectionAtEndOfUow(SelectionHelper helper)
		{
			throw new NotImplementedException("Method RequestSelectionAtEndOfUow is not implemented.");
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets the root box
		/// </summary>
		/// <value>A <c>IVwRootBox</c> object</value>
		/// <remarks>Used to implement IVwRootSite::get_RootBox</remarks>
		/// -----------------------------------------------------------------------------------
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public IVwRootBox RootBox
		{
			get
			{
				CheckDisposed();
				return m_rootb;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the associated style sheet
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public virtual IVwStylesheet StyleSheet
		{
			get
			{
				CheckDisposed();
				return m_styleSheet;
			}
			set
			{
				CheckDisposed();
				if (m_styleSheet == value)
					return;
				if (m_styleSheet != null && Marshal.IsComObject(m_styleSheet))
					Marshal.ReleaseComObject(m_styleSheet);
				m_styleSheet = value;
				if (RootBox != null)
				{
					// Clean up. This code assumes there is only one root, almost universally true.
					// If not, either make sure the stylesheet is set early, or override.
					int hvoRoot, frag;
					IVwViewConstructor vc;
					IVwStylesheet ss;
					RootBox.GetRootObject(out hvoRoot, out vc, out frag, out ss);
					RootBox.SetRootObject(hvoRoot, vc, frag, m_styleSheet);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the location of the insertion point.
		/// NOTE: This is the point relative to the top of visible area not the location with
		/// in the rootsite.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Point IPLocation
		{
			get
			{
				CheckDisposed();
				Point pt = Point.Empty;

				if (m_rootb != null && !DesignMode)
				{
					IVwSelection vwsel = m_rootb.Selection;
					if (vwsel != null)
					{
						// insertion point location is actually the endpoint of a span
						if (vwsel.IsRange == true)
						{
							vwsel = vwsel.EndPoint(true);
							Debug.Assert(vwsel != null);
						}

						using (new HoldGraphics(this))
						{
							Rectangle rcSrcRoot, rcDstRoot;
							Rect rcSec, rcPrimary;
							bool fSplit, fEndBeforeAnchor;
							GetCoordRects(out rcSrcRoot, out rcDstRoot);
							vwsel.Location(m_graphicsManager.VwGraphics, rcSrcRoot, rcDstRoot, out rcPrimary,
								out rcSec, out fSplit, out fEndBeforeAnchor);

							pt = new Point((rcPrimary.right + rcPrimary.left) / 2,
								(rcPrimary.top + rcPrimary.bottom) / 2);
						}
					}
				}

				return pt;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value that tells this view that it is entirely read-only or not.
		/// If you call this before m_rootb is set (e.g., during an override of MakeRoot) and
		/// set it to false, consider setting MaxParasToScan to zero on the root box.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool ReadOnlyView
		{
			get
			{
				CheckDisposed();
				return !EditingHelper.Editable;
			}
			set
			{
				CheckDisposed();
				EditingHelper.Editable = !value;
				// If the view is read-only, we don't want to waste time looking for an
				// editable insertion point when moving the cursor with the cursor movement
				// keys.  Setting this value to 0 accomplishes this.
				if (value && m_rootb != null)
					m_rootb.MaxParasToScan = 0;
			}
		}

		/// <summary>
		/// Indicates that we expect the view to have an editable field.
		/// </summary>
		internal bool IsEditable
		{
			get { return !ReadOnlyView; }

			// Note: if you add a set{}, make sure is compatible with ReadOnlyView state.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the zoom multiplier that magnifies (or shrinks) the view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual float Zoom
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
				RefreshDisplay();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether the root can be constructed in design mode.
		/// </summary>
		/// <value>The default implementation always returns <c>false</c>. Override in your
		/// derived class to allow MakeRoot being called when the view is opened in designer in
		/// Visual Studio.
		/// </value>
		/// ------------------------------------------------------------------------------------
		protected virtual bool AllowPaintingInDesigner
		{
			get { return false; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// (For internal use only.) Determine whether or not client posted a "FollowLink"
		/// message, in which case we are about to switch tools.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool IsFollowLinkMsgPending
		{
			get
			{
				return (m_mediator != null) && m_mediator.IsMessageInPendingQueue("FollowLink");
				// NOTE: if m_mediator == null, the message could still have been posted.
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the data access (corresponds to a DB connection) for the rootbox of this
		/// rootsite.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual ISilDataAccess DataAccess
		{
			get { return (m_rootb == null) ? null : m_rootb.DataAccess; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Helper used for processing editing requests.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public EditingHelper EditingHelper
		{
			get
			{
				CheckDisposed();
				if (m_editingHelper == null)
				{
					m_editingHelper = CreateEditingHelper();
					OnEditingHelperCreated();
				}
				return m_editingHelper;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the editing helper is created.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void OnEditingHelperCreated()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new EditingHelper of the proper type.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual EditingHelper CreateEditingHelper()
		{
			return new EditingHelper(this);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the dpi.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected internal Point Dpi
		{
			get { return m_Dpi; }
			set { m_Dpi = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Usually Cursors.IBeam; overridden in vertical windows.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal Cursor IBeamCursor
		{
			get { return m_orientationManager.IBeamCursor; }
		}
		#endregion // Properties

		#region Implementation of IVwRootSite
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Adjust the scroll range when some lazy box got expanded. Needs to be done for both
		/// panes if we have more than one.
		/// </summary>
		/// <param name="prootb"></param>
		/// <param name="dxdSize"></param>
		/// <param name="dxdPosition"></param>
		/// <param name="dydSize"></param>
		/// <param name="dydPosition"></param>
		/// <returns></returns>
		/// -----------------------------------------------------------------------------------
		public bool AdjustScrollRange(IVwRootBox prootb, int dxdSize, int dxdPosition,
			int dydSize, int dydPosition)
		{
			CheckDisposed();
			return AdjustScrollRange1(dxdSize, dxdPosition, dydSize, dydPosition);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Cause the immediate update of the display of the root box. This should cause all pending
		/// paint operations to be done immediately, at least for the screen area occupied by the
		/// root box. It is typically called after processing key strokes, to ensure that the updated
		/// text is displayed before trying to process any subsequent keystrokes.
		/// </summary>
		/// <param name="prootb"></param>
		/// -----------------------------------------------------------------------------------
		public virtual void DoUpdates(IVwRootBox prootb)
		{
			CheckDisposed();
			//	Console.WriteLine("DoUpdates");

			//Removed PerformLayout to reduce flashing
			//PerformLayout();
			Update();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Get the width available for laying things out in the view.
		/// Return the layout width for the window, depending on whether or not there is a
		/// scroll bar. If there is no scroll bar, we pretend that there is, so we don't have
		/// to keep adjusting the width back and forth based on the toggling on and off of
		/// vertical and horizontal scroll bars and their interaction.
		/// The return result is in pixels.
		/// The only common reason to override this is to answer instead a very large integer,
		/// which has the effect of turning off line wrap, as everything apparently fits on
		/// a line.
		/// N.B. If it is necessary to override this, it is not advisable to use Int32.MaxValue
		/// for fear of overflow caused by VwSelection::InvalidateSel() adjustments.
		/// </summary>
		/// <param name="prootb"></param>
		/// <returns>Width available for layout</returns>
		/// -----------------------------------------------------------------------------------
		public virtual int GetAvailWidth(IVwRootBox prootb)
		{
			CheckDisposed();
			// The default -4 allows two pixels right and left to keep data clear of the margins.
			return m_orientationManager.GetAvailWidth() - HorizMargin * 2;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Invalidate rectangle
		/// </summary>
		/// <param name="root">The sender</param>
		/// <param name="xsLeft">Relative to top left of root box</param>
		/// <param name="ysTop"></param>
		/// <param name="xsWidth"></param>
		/// <param name="ysHeight"></param>
		/// -----------------------------------------------------------------------------------
		public virtual void InvalidateRect(IVwRootBox root, int xsLeft, int ysTop, int xsWidth,
			int ysHeight)
		{
			CheckDisposed();
			if (xsWidth <= 0 || ysHeight <= 0)
				return; // empty rectangle, may not produce paint.
			// REVIEW: We found that InvalidateRect was being called twice with the same rectangle for
			// every keystroke.  We assume that this problem is originating within the rootbox code.

			// Convert from coordinates relative to the root box to coordinates relative to
			// the current client rectangle.
			Rectangle rcSrcRoot, rcDstRoot;
			using (new HoldGraphics(this))
			{
				GetCoordRects(out rcSrcRoot, out rcDstRoot);
			}
			int left = MapXTo(xsLeft, rcSrcRoot, rcDstRoot);
			int top = MapYTo(ysTop, rcSrcRoot, rcDstRoot);
			int right = MapXTo(xsLeft + xsWidth, rcSrcRoot, rcDstRoot);
			int bottom = MapYTo(ysTop + ysHeight, rcSrcRoot, rcDstRoot);
			Rectangle rect = new Rectangle(left, top, right - left, bottom - top);
			CallInvalidateRect(m_orientationManager.RotateRectDstToPaint(rect), true);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Get a graphics object in an appropriate state for drawing and measuring in the view.
		/// The calling method should pass the IVwGraphics back to ReleaseGraphics() before
		/// it returns. In particular, problems will arise if OnPaint() gets called before the
		/// ReleaseGraphics() method.
		/// </summary>
		/// <remarks>
		/// REVIEW JohnT(?): We probably need a better way to handle this. Most likely: make
		/// the VwGraphics object we cache a true COM object so its reference count is
		/// meaningful; have this method create a new one. Problem: a useable VwGraphics object
		/// has a device context that is linked to a particular window; if the window closes,
		/// the VwGraphics is not useable, whatever its reference count says. It may therefore
		/// be that we just need to allocate a copy in this method, leaving the member variable
		/// alone. Or, the current strategy may prove adequate.
		/// </remarks>
		/// <param name="prootb"></param>
		/// <param name="pvg"></param>
		/// <param name="rcSrcRoot"></param>
		/// <param name="rcDstRoot"></param>
		/// -----------------------------------------------------------------------------------
		public virtual void GetGraphics(IVwRootBox prootb, out IVwGraphics pvg, out Rect rcSrcRoot,
			out Rect rcDstRoot)
		{
			CheckDisposed();
			InitGraphics();
			pvg = m_graphicsManager.VwGraphics;

			Rectangle rcSrc, rcDst;
			GetCoordRects(out rcSrc, out rcDst);

			rcSrcRoot = rcSrc;
			rcDstRoot = rcDst;
		}
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Get a graphics object in an appropriate state for drawing and measuring in the view.
		/// The calling method should pass the IVwGraphics back to ReleaseGraphics() before
		/// it returns. In particular, problems will arise if OnPaint() gets called before the
		/// ReleaseGraphics() method.
		/// </summary>
		/// <param name="prootb"></param>
		/// <returns></returns>
		/// -----------------------------------------------------------------------------------
		IVwGraphics IVwRootSite.get_LayoutGraphics(IVwRootBox prootb)
		{
			CheckDisposed();
			InitGraphics();
			return m_graphicsManager.VwGraphics;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Get a transform for a given destination point...same for all points in this
		/// simple case.
		/// </summary>
		/// <param name="root"></param>
		/// <param name="pt"></param>
		/// <param name="rcSrcRoot"></param>
		/// <param name="rcDstRoot"></param>
		/// -----------------------------------------------------------------------------------
		public void GetTransformAtDst(IVwRootBox root, Point pt, out Rect rcSrcRoot,
			out Rect rcDstRoot)
		{
			CheckDisposed();
			using (new HoldGraphics(this))
			{
				Rectangle rcSrc, rcDst;
				GetCoordRects(out rcSrc, out rcDst);

				rcSrcRoot = rcSrc;
				rcDstRoot = rcDst;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Get a transform for a given layout point...same for all points in this
		/// simple case.
		/// </summary>
		/// <param name="root"></param>
		/// <param name="pt"></param>
		/// <param name="rcSrcRoot"></param>
		/// <param name="rcDstRoot"></param>
		/// -----------------------------------------------------------------------------------
		public void GetTransformAtSrc(IVwRootBox root, Point pt, out Rect rcSrcRoot,
			out Rect rcDstRoot)
		{
			CheckDisposed();
			GetTransformAtDst(root, pt, out rcSrcRoot, out rcDstRoot);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Real drawing VG same as layout one for simple view.
		/// </summary>
		/// <param name="_Root"></param>
		/// <returns></returns>
		/// -----------------------------------------------------------------------------------
		public IVwGraphics get_ScreenGraphics(IVwRootBox _Root)
		{
			CheckDisposed();
			InitGraphics();
			return m_graphicsManager.VwGraphics;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Inform the container when done with the graphics object.
		/// </summary>
		/// <remarks>
		/// REVIEW JohnT(?): could we somehow have this handled by the Release method
		/// of the IVwGraphics? But that method does not know anything about the status or
		/// source of its hdc.
		/// </remarks>
		/// <param name="prootb"></param>
		/// <param name="pvg"></param>
		/// -----------------------------------------------------------------------------------
		public virtual void ReleaseGraphics(IVwRootBox prootb, IVwGraphics pvg)
		{
			CheckDisposed();
			Debug.Assert(pvg == m_graphicsManager.VwGraphics);
			UninitGraphics();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Notifies the site that something about the selection has changed.
		/// </summary>
		/// <param name="rootb">The rootbox whose selection changed</param>
		/// <param name="vwselNew">The new selection</param>
		/// <remarks>Don't you dare make this virtual!</remarks>
		/// -----------------------------------------------------------------------------------
		public void SelectionChanged(IVwRootBox rootb, IVwSelection vwselNew)
		{
			CheckDisposed();

			Debug.Assert(rootb == EditingHelper.EditedRootBox);
			Debug.Assert(vwselNew == rootb.Selection);

			EditingHelper.SelectionChanged();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Notifies the site that the size of the root box changed; scroll ranges and/or
		/// window size may need to be updated. The standard response is to update the scroll range.
		/// </summary>
		/// <remarks>
		/// Review JohnT: might this also be the place to make sure the selection is still visible?
		/// Should we try to preserve the scroll position (at least the top left corner, say) even
		/// if the selection is not visible? Which should take priority?
		/// </remarks>
		/// <param name="prootb"></param>
		/// -----------------------------------------------------------------------------------
		public virtual void RootBoxSizeChanged(IVwRootBox prootb)
		{
			CheckDisposed();
			if (!AllowLayout)
				return;
			UpdateScrollRange();

			OnLayoutSizeChanged(new EventArgs());
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Required method to implement the LayoutSizeChanged event.
		/// </summary>
		/// <param name="e"></param>
		/// -----------------------------------------------------------------------------------
		protected virtual void OnLayoutSizeChanged(EventArgs e)
		{
			if (LayoutSizeChanged != null)
			{
				//Invokes the delegates.
				LayoutSizeChanged(this, e);
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// When the state of the overlays changes, it propagates this to its site.
		/// </summary>
		/// <param name="prootb"></param>
		/// <param name="vo"></param>
		/// -----------------------------------------------------------------------------------
		public virtual void OverlayChanged(IVwRootBox prootb, IVwOverlay vo)
		{
			CheckDisposed();
			// do nothing
		}


		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Return true if this kind of window uses semi-tagging.
		/// </summary>
		/// <param name="prootb"></param>
		/// -----------------------------------------------------------------------------------
		public virtual bool get_SemiTagging(IVwRootBox prootb)
		{
			CheckDisposed();
			return false;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Member ScreenToClient
		/// </summary>
		/// <param name="prootb"></param>
		/// <param name='pt'>Pont to convert</param>
		/// -----------------------------------------------------------------------------------
		public virtual void ScreenToClient(IVwRootBox prootb, ref System.Drawing.Point pt)
		{
			CheckDisposed();
			pt = PointToClient(pt);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Member ClientToScreen
		/// </summary>
		/// <param name="prootb"></param>
		/// <param name='pt'>Point to convert</param>
		/// -----------------------------------------------------------------------------------
		public virtual void ClientToScreen(IVwRootBox prootb, ref System.Drawing.Point pt)
		{
			CheckDisposed();
			pt = PointToScreen(pt);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>If there is a pending writing system that should be applied to typing,
		/// return it; also clear the state so that subsequent typing will not have a pending
		/// writing system until something sets it again.  (This is mainly used so that
		/// keyboard-change commands can be applied while the selection is a range.)</summary>
		/// <param name="prootb"></param>
		/// <returns>Pending writing system</returns>
		/// -----------------------------------------------------------------------------------
		public virtual int GetAndClearPendingWs(IVwRootBox prootb)
		{
			CheckDisposed();
			int ws = m_wsPending;
			m_wsPending = -1;
			return ws;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Answer whether boxes in the specified range of destination coordinates
		/// may usefully be converted to lazy boxes. Should at least answer false
		/// if any part of the range is visible. The default implementation avoids
		/// converting stuff within about a screen's height of the visible part(s).
		/// </summary>
		/// <param name="prootb"></param>
		/// <param name="ydBottom"></param>
		/// <param name="ydTop"></param>
		/// -----------------------------------------------------------------------------------
		public virtual bool IsOkToMakeLazy(IVwRootBox prootb, int ydTop, int ydBottom)
		{
			CheckDisposed();
			return false; // Todo JohnT or TE team: make similar to AfVwWnd impl.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The user has attempted to delete something which the system does not inherently
		/// know how to delete. The dpt argument indicates the type of problem.
		/// </summary>
		/// <param name="sel">The selection</param>
		/// <param name="dpt">Problem type</param>
		/// <returns><c>true</c> to abort</returns>
		/// ------------------------------------------------------------------------------------
		public virtual VwDelProbResponse OnProblemDeletion(IVwSelection sel,
			VwDelProbType dpt)
		{
			CheckDisposed();
			return VwDelProbResponse.kdprFail; // give up quietly.
			// Review team (JohnT): a previous version threw NotImplementedException. This seems
			// overly drastic.
		}

		/// ----------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="ttpDest"></param>
		/// <param name="cPara"></param>
		/// <param name="ttpSrc"></param>
		/// <param name="tssParas"></param>
		/// <param name="tssTrailing"></param>
		/// <param name="prootb"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public virtual VwInsertDiffParaResponse OnInsertDiffParas(IVwRootBox prootb,
			ITsTextProps ttpDest, int cPara, ITsTextProps[] ttpSrc, ITsString[] tssParas,
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

		/// ----------------------------------------------------------------------------------
		/// <summary>
		/// Needs a cache in order to provide a meaningful implementation. SimpleRootsite
		/// should never have objects cut, copied, pasted.
		/// </summary>
		/// <returns>An empty string so that the caller will just discard the run containing
		/// the ORC</returns>
		/// ------------------------------------------------------------------------------------
		public virtual string get_TextRepOfObj(ref Guid guid)
		{
			CheckDisposed();
			Debug.Fail("This should never get called for a simple rootsite.");
			return string.Empty;
		}

		/// ----------------------------------------------------------------------------------
		/// <summary>
		/// Needs a cache in order to provide a meaningful implementation. SimpleRootsite does
		/// not know how to handle GUIDs so just return an empty GUID which will cause the
		/// run to be deleted.
		/// </summary>
		/// <param name="bstrText">Text representation of object</param>
		/// <param name="_selDst">Provided for information in case it's needed to generate
		/// the new object (E.g., footnotes might need it to generate the proper sequence
		/// letter)</param>
		/// <param name="kodt">The object data type to use for embedding the new object
		/// </param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public virtual Guid get_MakeObjFromText(string bstrText, IVwSelection _selDst, out int kodt)
		{
			CheckDisposed();
			kodt = -1;
			return Guid.Empty;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Scrolls the selection into view, positioning it as requested
		/// </summary>
		/// <param name="sel">The selection, or <c>null</c> to use the current selection</param>
		/// <param name="scrollOption">The VwScrollSelOpts specification.</param>
		/// <returns>True if the selection was moved into view, false if this function did
		/// nothing</returns>
		/// ------------------------------------------------------------------------------------
		public virtual bool ScrollSelectionIntoView(IVwSelection sel,
			VwScrollSelOpts scrollOption)
		{
			CheckDisposed();
			switch (scrollOption)
			{
				case VwScrollSelOpts.kssoDefault:
					return MakeSelectionVisible(sel, true);
				case VwScrollSelOpts.kssoNearTop:
					return ScrollSelectionToLocation(sel, LineHeight);
				case VwScrollSelOpts.kssoTop:
					return ScrollSelectionToLocation(sel, 1);
				case VwScrollSelOpts.kssoBoth:
					return MakeSelectionVisible(sel, true, true, true);
				default:
					throw new ArgumentException("Unsupported VwScrollSelOpts");
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets the root box
		/// </summary>
		/// <value>A <c>IVwRootBox</c> object</value>
		/// -----------------------------------------------------------------------------------
		IVwRootBox IVwRootSite.RootBox
		{
			get
			{
				CheckDisposed();
				return m_rootb;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets the HWND.
		/// </summary>
		/// <value>A <c>HWND</c> handle</value>
		/// -----------------------------------------------------------------------------------
		uint IVwRootSite.Hwnd
		{
			get
			{
				CheckDisposed();
				return (uint)this.Handle;
			}
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
				Point newPos = value;
				if (this.AutoScroll)
				{
					newPos.X = Math.Abs(newPos.X);
					newPos.Y = Math.Abs(newPos.Y);
				}
				else
				{
					// If we're not autoscrolling, don't do it!
					newPos.X = 0;
					newPos.Y = 0;
				}
#if !__MonoCS__
				AutoScrollPosition = newPos;
#else
				if (AllowPainting == true) // FWNX-235
					AutoScrollPosition = newPos;
				else
					cachedAutoScrollPosition = newPos;
#endif
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// We'd like to be able to override the setter for AutoScrollMinSize, but
		/// it isn't virtual so we use this instead.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public virtual Size ScrollMinSize
		{
			get
			{
				CheckDisposed();
				return AutoScrollMinSize;
			}
			set
			{
				CheckDisposed();
#if !__MonoCS__
				AutoScrollMinSize = value;
#else
				// TODO-Linux
				// Due to difference in mono scrolling bar behaviour
				// possibly partly due to bug https://bugzilla.novell.com/show_bug.cgi?id=500796
				// although there are probably other problems.
				// possibly causes other unit test issues
				// Don't adjust this unless it's needed.  See FWNX-561.
				AutoScrollMinSize = value - new Size(VScroll ? SystemInformation.VerticalScrollBarWidth : 0,
													 HScroll ? SystemInformation.HorizontalScrollBarHeight : 0);
#endif

#if !__MonoCS__
				// Following line is necessary so that the DisplayRectangle gets updated.
				// This calls PerformLayout().
				AdjustFormScrollbars(HScroll || VScroll);
#else
				try
				{
					AdjustFormScrollbars(HScroll || VScroll);
				}
				catch(System.ArgumentOutOfRangeException )
				{
					// TODO-Linux: Invesigate real cause of this.
					// I think it caused by mono setting ScrollPosition to 0 when Scrollbar isn't visible
					// it should possibly set it to Minimun value instead.
					// Currently occurs when dropping down scrollbars in Flex.
				}

#endif
			}
		}

		/// <summary>
		/// We want to allow clients to tell whether we are showing the horizontal scroll bar.
		/// </summary>
		public bool IsHScrollVisible
		{
			get { return WantHScroll && AutoScrollMinSize.Width > Width; }
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Root site slaves sometimes need to suppress the effects of OnSizeChanged.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public virtual bool SizeChangedSuppression
		{
			get
			{
				CheckDisposed();
				return false;
			}
			set
			{
				CheckDisposed();
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Typically the client rectangle height, but this gives a bizarre value for
		/// root sites in a group, so it is overridden.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public virtual int ClientHeight
		{
			get
			{
				CheckDisposed();
				return ClientRectangle.Height;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// This returns the client rectangle that we want the selection to be inside.
		/// For a normal root site this is just its client rectangle.
		/// RootSite overrides.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public virtual Rectangle AdjustedClientRectangle
		{
			get
			{
				CheckDisposed();
				return ClientRectangle;
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
			if (DoingScrolling)
				ScrollPosition = new Point(0, 0);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Scroll to the bottom. This is somewhat tricky because after scrolling to the bottom of
		/// the range as we currently estimate it, expanding a closure may change things.
		/// <seealso cref="GoToEnd"/>
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public virtual void ScrollToEnd()
		{
			CheckDisposed();
			if (DoingScrolling && !DesignMode)
			{
				// dy gets added to the scroll offset. This means a positive dy causes there to be more
				// of the view hidden above the top of the screen. This is the same effect as clicking a
				// down arrow, which paradoxically causes the window contents to move up.
				int dy = 0;
				int ydCurr = -ScrollPosition.Y; // Where the window thinks it is now.

				using (new HoldGraphics(this))
				{
					// This loop repeats until we have figured out a scroll distance AND confirmed
					// that we can draw that location without messing things up.
					for (; ; )
					{
						int ydMax = DisplayRectangle.Height - ClientHeight + 1;
						dy = ydMax - ydCurr;
						// OK, we need to move by dy. But, we may have to expand a lazy box there in order
						// to display a whole screen full. If the size estimate is off (which it usually is),
						// that would affect the scroll position we need to be at the very bottom.
						// To avoid this, we make the same PrepareToDraw call
						// that the rendering code will make before drawing after the scroll.
						Rectangle rcSrcRoot;
						Rectangle rcDstRoot;
						GetCoordRects(out rcSrcRoot, out rcDstRoot);
						rcDstRoot.Offset(0, -dy);

						int dyRange = m_rootb.Height;
						Rectangle r = AdjustedClientRectangle;
						SIL.Utils.Rect clipRect = new SIL.Utils.Rect(r.Left, r.Top, r.Right, r.Bottom);

						if (m_graphicsManager.VwGraphics is IVwGraphicsWin32)
							((IVwGraphicsWin32)m_graphicsManager.VwGraphics).SetClipRect(ref clipRect);

						if (m_rootb != null && (m_dxdLayoutWidth > 0))
							PrepareToDraw(rcSrcRoot, rcDstRoot);

						ydCurr = -ScrollPosition.Y; // Where the window thinks it is now. (May have changed expanding.)
						// If PrepareToDraw didn't change the scroll range, it didn't mess anything up and we
						// can use the dy we figured. Otherwise, loop and figure it again with more complete
						// information, because something at a relevant point has been expanded to real boxes.
						if (m_rootb.Height == dyRange)
							break;
						dy = 0; // Back to initial state.
					}

					if (dy != 0)
					{
						// Update the scroll bar.
						// We have to pass a positive value, although ScrollPosition
						// returns a negative one
						ScrollPosition = new Point(-ScrollPosition.X, ydCurr + dy);
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Show the context menu for the specified root box at the location of
		/// its selection (typically an IP).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void ShowContextMenuAtIp(IVwRootBox rootb)
		{
			CheckDisposed();
			if (ContextMenu != null)
				ContextMenu.Show(this, IPLocation);
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
				return 14 * Dpi.Y / 72; // 14 points is typically about a line.
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
		/// RootBox being edited.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IVwRootBox EditedRootBox
		{
			get
			{
				CheckDisposed();
				return m_rootb;
			}
		}

		/// <summary>
		/// This tests whether the class has a cache (in the common RootSite subclass) or
		/// (in this base class) whether it has a ws. This is often used to determine whether
		/// we are sufficiently initialized to go ahead with some operation that may get called
		/// prematurely by something in the .NET framework.
		/// </summary>
		public virtual bool GotCacheOrWs
		{
			get
			{
				CheckDisposed();
				return m_wsf != null;
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
			throw new NotImplementedException();
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

		#region Implementation of IRootSite
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Refreshes the Display :)
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public virtual bool RefreshDisplay()
		{
			CheckDisposed();
			if (m_rootb == null || m_rootb.Site == null)
				return false;

			if (m_rootb.DataAccess is IRefreshable)
				((IRefreshable)m_rootb.DataAccess).Refresh();

			// If we aren't visible or don't belong to a form, then set a flag to do a refresh
			// the next time we go visible.
			if (!Visible || FindForm() == null)
			{
				m_fRefreshPending = true;
				return false;
			}

			// Rebuild the display... the drastic way.
			SelectionRestorer restorer = CreateSelectionRestorer();
			try
			{
				using (new SuspendDrawing(this))
				{
					m_rootb.Reconstruct();
					m_fRefreshPending = false;
				}
			}
			finally
			{
				if (restorer != null)
					restorer.Dispose();
			}
			//Enhance: If all refreshable descendants are handled this should return true
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new selection restorer.
		/// </summary>
		/// <remarks>Overriding this method to return null will keep the selection from being
		/// restored</remarks>
		/// ------------------------------------------------------------------------------------
		protected virtual SelectionRestorer CreateSelectionRestorer()
		{
			return new SelectionRestorer(this);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Allows the IRootSite to be cast as an IVwRootSite
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public virtual IVwRootSite CastAsIVwRootSite()
		{
			CheckDisposed();
			return this;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return the internal rootbox as a list, or an empty list.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public virtual List<IVwRootBox> AllRootBoxes()
		{
			CheckDisposed();
			List<IVwRootBox> result = new List<IVwRootBox>();
			if (m_rootb != null)
				result.Add(m_rootb);

			return result;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// <c>false</c> to prevent OnPaint from happening, <c>true</c> to perform
		/// OnPaint. This is used to prevent redraws from happening while we do a RefreshDisplay.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[BrowsableAttribute(false)]
		[DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Hidden)]
		public bool AllowPainting
		{
			get
			{
				CheckDisposed();
				return (m_nAllowPaint == 0);
			}
			set
			{
				CheckDisposed();
				// we use WM_SETREDRAW to prevent the scrollbar from jumping around when we are
				// in the middle of a Reconstruct. The m_nAllowPaint flag takes care of (not)
				// painting the view.

				if (value)
				{	// allow painting
					if (m_nAllowPaint > 0)
						m_nAllowPaint--;

					if (m_nAllowPaint == 0 && Visible && IsHandleCreated)
					{
#if !__MonoCS__
						Win32.SendMessage(Handle, (int)Win32.WinMsgs.WM_SETREDRAW, 1, 0);
						Update();
						Invalidate();
#else
						this.ResumeLayout();
						Update();
						Invalidate();

						// FWNX-235
						if (cachedAutoScrollPosition != null)
							AutoScrollPosition = (Point)cachedAutoScrollPosition;
						cachedAutoScrollPosition = null;
#endif
					}
				}
				else
				{   // prevent painting
					if (m_nAllowPaint == 0 && Visible && IsHandleCreated)
#if !__MonoCS__
						Win32.SendMessage(Handle, (int)Win32.WinMsgs.WM_SETREDRAW, 0, 0);
#else
						this.SuspendLayout();
#endif

					m_nAllowPaint++;
				}
			}
		}
		#endregion

		#region Implementation of IxCoreColleague
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Allows xCore-specific initialization. We don't need any.
		/// </summary>
		/// <param name="mediator">The mediator</param>
		/// <param name="configurationParameters">Not used</param>
		/// ------------------------------------------------------------------------------------
		public virtual void Init(XCore.Mediator mediator, System.Xml.XmlNode configurationParameters)
		{
			CheckDisposed();
			// Save the mediator in case a client or subclass wants it.
			m_mediator = mediator;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets objects known to this site that can handle xCore command messages.
		/// For rootsite, the only such object it knows about is itself.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public virtual XCore.IxCoreColleague[] GetMessageTargets()
		{
			CheckDisposed();
			return new XCore.IxCoreColleague[] { this };
		}

		/// <summary>
		/// Should not be called if disposed.
		/// </summary>
		public bool ShouldNotCall
		{
			get { return IsDisposed; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Receives the xcore broadcast message "PropertyChanged"
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void OnPropertyChanged(string name)
		{
			CheckDisposed();
			switch (name)
			{
				case "WritingSystemHvo":
					EditingHelper.WritingSystemHvoChanged();
					break;

				case "BestStyleName":
					EditingHelper.BestStyleNameChanged();
					break;

				default:
					break;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called (by xcore) to control display params of the writing system menu, e.g. whether
		/// it should be enabled
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public virtual bool OnDisplayWritingSystemHvo(object commandObject,
			ref UIItemDisplayProperties display)
		{
			CheckDisposed();
			if (!Focused)
				return false;
			display.Enabled = IsSelectionFormattable;
			return true;//we handled this, no need to ask anyone else.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called (by xcore) to control display params of the Styles menu, e.g. whether
		/// it should be enabled
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual bool OnDisplayBestStyleName(object commandObject,
			ref UIItemDisplayProperties display)
		{
			CheckDisposed();
			if (!Focused)
				return false;
			display.Enabled = CanApplyStyle;
			display.Text = BestSelectionStyle;
			return true;		// we handled this, no need to ask anyone else.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when XCore wants to display something that relies on the list with the id "CombinedStylesList"
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool OnDisplayCombinedStylesList(object parameter, ref UIListDisplayProperties display)
		{
			CheckDisposed();
			if (!Focused || m_rootb == null)
				return false;
			IVwStylesheet stylesheet = m_rootb.Stylesheet;
			if (stylesheet == null)
				return false;
			FillInStylesComboList(display, stylesheet);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fill in the list of style names.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void FillInStylesComboList(UIListDisplayProperties display, IVwStylesheet stylesheet)
		{
		}
		#endregion

		#region Print-related methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle Print command
		/// </summary>
		/// <param name="args">ignored</param>
		/// ------------------------------------------------------------------------------------
		public virtual bool OnFilePrint(object args)
		{
			CheckDisposed();
			return OnPrint(args);
		}

		/// <summary>
		/// This is equivalent. These two names accommodate two conventions for naming
		/// commands...xCore labels commands just with what they do, TE includes the menu name.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		public virtual bool OnPrint(object args)
		{
			CheckDisposed();
			if (SuppressPrintHandling)
				return false;

			using (PrintDocument pd = new PrintDocument())
			{
				//			PageSetupDialog pageDlg = new PageSetupDialog();
				//			pageDlg.Document = pd;
				//			pageDlg.ShowDialog();

				using (PrintDialog dlg = new PrintDialog())
				{
					dlg.Document = pd;
					dlg.AllowSomePages = true;
					dlg.AllowSelection = false;
					dlg.PrinterSettings.FromPage = 1;
					dlg.PrinterSettings.ToPage = 1;
					SetupPrintHelp(dlg);
					AdjustPrintDialog(dlg);

					if (dlg.ShowDialog() != DialogResult.OK)
						return true;

					if (MiscUtils.IsUnix)
					{
						using (PageSetupDialog pageDlg = new PageSetupDialog())
						{
							pageDlg.Document = dlg.Document;
							pageDlg.AllowPrinter = false;
							if (pageDlg.ShowDialog() != DialogResult.OK)
								return true;
						}
					}

					// REVIEW: .NET does not appear to handle the collation setting correctly
					// so for now, we do not support non-collated printing.  Forcing the setting
					// seems to work fine.
					pd.PrinterSettings.Collate = true;
					Print(pd);
				}
			}
			return true;
		}

		/// <summary>
		/// By default this does nothing. Override to, for example, enable the 'Selection' button.
		/// See XmlSeqView for an example.
		/// </summary>
		/// <param name="dlg"></param>
		public virtual void AdjustPrintDialog(PrintDialog dlg)
		{
			CheckDisposed();
		}

		/// <summary>
		/// If help is available for the print dialog, set ShowHelp to true,
		/// and add an event handler that can display some help.
		/// See DraftView in TeDll for an example.
		/// </summary>
		/// <param name="dlg"></param>
		protected virtual void SetupPrintHelp(PrintDialog dlg)
		{
			CheckDisposed();
			dlg.ShowHelp = false;
		}

		/// <summary>
		/// Default is to print the exact same thing as displayed in the view, but
		/// subclasses (e.g., ConstChartBody) can override.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="vc"></param>
		/// <param name="frag"></param>
		/// <param name="ss"></param>
		protected virtual void GetPrintInfo(out int hvo, out IVwViewConstructor vc, out int frag, out IVwStylesheet ss)
		{
			m_rootb.GetRootObject(out hvo, out vc, out frag, out ss);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// SimpleRootSite Print(pd) method, overridden by e.g. XmlSeqView.
		/// Note: this does not implement the lower level IPrintRootSite.Print(pd).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void Print(PrintDocument pd)
		{
			CheckDisposed();
			if (m_rootb == null || DataAccess == null)
				return;
			int hvo;
			IVwViewConstructor vc;
			int frag;
			IVwStylesheet ss;
			GetPrintInfo(out hvo, out vc, out frag, out ss);

			IPrintRootSite printRootSite = new PrintRootSite(DataAccess, hvo, vc, frag, ss);
			PrintWithErrorHandling(printRootSite, pd, FindForm());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Helper method to allow for standard error reporting when something goes wrong during
		/// printing of the contents of a rootsite.
		/// </summary>
		/// <param name="printRootSite">The actual rootsite responsible for printing</param>
		/// <param name="printDoc">The document to print</param>
		/// <param name="parentForm">Used if something goes wrong and a message box needs to be
		/// shown</param>
		/// ------------------------------------------------------------------------------------
		public static void PrintWithErrorHandling(IPrintRootSite printRootSite,
			PrintDocument printDoc, Form parentForm)
		{
			try
			{
				printRootSite.Print(printDoc);
			}
			catch (ContinuableErrorException e)
			{
				throw e;
			}
			catch (Exception e)
			{
				string errorMsg = String.Format(Properties.Resources.kstidPrintingException, e.Message);
				MessageBox.Show(parentForm, errorMsg, Properties.Resources.kstidPrintErrorCaption,
					MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
		#endregion

		#region Scrolling-related methods
		// We don't need AfVwScrollWnd::ScrollBy - can be done directly in .NET

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Finds the distance between the scroll position and the IP (i.e. the distance
		/// between the top of the window and the IP).
		/// </summary>
		/// <param name="sel">The selection used to get the IP's location. If
		/// this value is null, the rootsite's current selection will be used.</param>
		/// -----------------------------------------------------------------------------------
		public int IPDistanceFromWindowTop(IVwSelection sel)
		{
			CheckDisposed();
			if (sel == null && m_rootb != null)
				sel = m_rootb.Selection;

			if (sel == null)
				return 0;

			using (new HoldGraphics(this))
			{
				Rectangle rcSrcRoot;
				Rectangle rcDstRoot;
				GetCoordRects(out rcSrcRoot, out rcDstRoot);

				Rect rcPrimary;
				Rect rcSecondary;
				bool fSplit;
				bool fEndBeforeAnchor;

				sel.Location(m_graphicsManager.VwGraphics, rcSrcRoot, rcDstRoot,
					out rcPrimary, out rcSecondary, out fSplit, out fEndBeforeAnchor);

				return rcPrimary.top;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Invalidate the pane because some lazy box expansion messed up the scroll position.
		/// Made a separate method so we can override.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public virtual void InvalidateForLazyFix()
		{
			CheckDisposed();
			Invalidate();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Scroll by the specified amount (positive is down, that is, added to the scroll offset).
		/// If this would exceed the scroll range, move as far as possible. Update both actual display
		/// and scroll bar position. (Can also scroll up, if dy is negative. Name is just to indicate
		/// positive direction.)
		/// </summary>
		/// <param name="dy"></param>
		/// -----------------------------------------------------------------------------------
		protected internal virtual void ScrollDown(int dy)
		{
			CheckDisposed();
			int xd, yd;
			GetScrollOffsets(out xd, out yd);
			int ydNew = yd + dy;
			if (ydNew < 0)
				ydNew = 0;
			if (ydNew > DisplayRectangle.Height)
				ydNew = DisplayRectangle.Height;
			if (ydNew != yd)
				ScrollPosition = new Point(xd, ydNew);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Use this method instead of <see cref="ScrollToEnd"/> if you need to go to the end
		/// of the view programmatically (not in response to a Ctrl-End). The code for handling
		/// Ctrl-End uses CallOnExtendedKey() in OnKeyDown() to handle setting the IP.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public void GoToEnd()
		{
			CheckDisposed();
			ScrollToEnd();
			// The code for handling Ctrl-End doesn't do it this way:
			RootBox.MakeSimpleSel(false, IsEditable, false, true);
			// This method is not used ... possibly move to DummyFootnoteView ??
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether automatic horizontal scrolling to show the selection should
		/// occur. Normally this is the case only if showing a horizontal scroll bar,
		/// but FwTextBox is an exception.
		/// </summary>
		/// <returns></returns>
		/// -----------------------------------------------------------------------------------
		protected virtual bool DoAutoHScroll
		{
			get { return DoingScrolling && HScroll; }
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether automatic vertical scrolling to show the selection should
		/// occur. Usually this is only appropriate if the window autoscrolls and has a
		/// vertical scroll bar, but TE's draft view needs to allow it anyway, because in
		/// syncrhonized scrolling only one of the sync'd windows has a scroll bar.
		/// </summary>
		/// <returns></returns>
		/// -----------------------------------------------------------------------------------
		protected virtual bool DoAutoVScroll
		{
			get { return DoingScrolling && VScroll; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the rectangle of the selection. If it is a split selection we combine the
		/// two rectangles.
		/// </summary>
		/// <param name="vwsel">Selection</param>
		/// <param name="rcIdeal">Contains the rectangle of the selection on return</param>
		/// <param name="fEndBeforeAnchor">[Out] <c>true</c> if the end is before the anchor,
		/// otherwise <c>false</c>.</param>
		/// ------------------------------------------------------------------------------------
		protected internal void SelectionRectangle(IVwSelection vwsel, out Rectangle rcIdeal,
			out bool fEndBeforeAnchor)
		{
			Debug.Assert(vwsel != null);
			Debug.Assert(m_graphicsManager.VwGraphics != null);

			Rect rcPrimary;
			Rect rcSecondary;
			bool fSplit;
			Rectangle rcSrcRoot;
			Rectangle rcDstRoot;
			GetCoordRects(out rcSrcRoot, out rcDstRoot);

			vwsel.Location(m_graphicsManager.VwGraphics, rcSrcRoot, rcDstRoot, out rcPrimary, out rcSecondary,
				out fSplit, out fEndBeforeAnchor);
			rcIdeal = rcPrimary;

			if (fSplit)
			{
				rcIdeal = Rectangle.Union(rcIdeal, rcSecondary);
				if ((AutoScroll && VScroll && rcIdeal.Height > ClientHeight) ||
					(DoAutoHScroll && rcIdeal.Width > ClientRectangle.Width))
				{
					rcIdeal = rcPrimary; // Revert to just showing main IP.
				}
			}
		}

		/// <summary>
		/// Makes a default selection if no selection currently exists in our RootBox.
		/// </summary>
		protected virtual void EnsureDefaultSelection()
		{
			EnsureDefaultSelection(IsEditable);
		}

		/// <summary>
		///  Makes a default selection if no selection currently exists in our RootBox.
		/// </summary>
		/// <param name="fMakeSelInEditable">if true, first try selecting in editable position.</param>
		protected void EnsureDefaultSelection(bool fMakeSelInEditable)
		{
			if (RootBox == null)
				return;
			if (RootBox.Selection == null)
			{
				try
				{
					RootBox.MakeSimpleSel(true, fMakeSelInEditable, false, true);
				}
				catch
				{
					// Eat the exception, since it couldn't make a selection.
				}
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Scroll to make the selection visible.
		/// In general, scroll the minimum distance to make it entirely visible.
		/// If the selection is higher than the window, scroll the minimum distance to make it
		/// fill the window.
		/// If the window is too small to show both primary and secondary, show primary.
		/// </summary>
		/// <param name="sel">Selection</param>
		/// <remarks>
		/// Note: subclasses for which scrolling is disabled should override.
		/// If <paramref name="sel"/> is null, make the current selection visible.
		/// </remarks>
		/// <returns>True if the selection was made visible, false if it did nothing</returns>
		/// -----------------------------------------------------------------------------------
		protected bool MakeSelectionVisible(IVwSelection sel)
		{
			return MakeSelectionVisible(sel, false);
		}

		/// <summary>
		/// Returns the selection that should be made visible if null is passed to
		/// MakeSelectionVisible. FwTextBox overrides to pass a selection that is the whole
		/// range, if nothing is selected.
		/// </summary>
		protected virtual IVwSelection SelectionToMakeVisible
		{
			get { return m_rootb.Selection; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Scroll to make the selection visible.
		/// In general, scroll the minimum distance to make it entirely visible.
		/// If the selection is higher than the window, scroll the minimum distance to make it
		/// fill the window.
		/// If the window is too small to show both primary and secondary, show primary.
		/// If fWantOneLineSpace is true we make sure that at least 1 line is visible above
		/// and below the selection.
		/// By default ranges (that are not pictures) are allowed to be only partly visible.
		/// </summary>
		/// <param name="sel">The sel.</param>
		/// <param name="fWantOneLineSpace">if set to <c>true</c> [f want one line space].</param>
		/// <returns>Flag indicating whether the selection was made visible</returns>
		/// ------------------------------------------------------------------------------------
		protected virtual bool MakeSelectionVisible(IVwSelection sel, bool fWantOneLineSpace)
		{
			if (m_rootb == null)
				return false;
			IVwSelection vwsel = (sel ?? SelectionToMakeVisible);
			return (vwsel != null &&
				MakeSelectionVisible(vwsel, fWantOneLineSpace, DefaultWantBothEnds(vwsel), false));
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Scroll to make the selection visible.
		/// In general, scroll the minimum distance to make it entirely visible.
		/// If the selection is higher than the window, scroll the minimum distance to make it
		/// fill the window.
		/// If the window is too small to show both primary and secondary, show primary.
		/// If fWantOneLineSpace is true we make sure that at least 1 line is visible above
		/// and below the selection.
		/// </summary>
		/// <param name="vwsel">Selection</param>
		/// <param name="fWantOneLineSpace">True if we want at least 1 extra line above or
		/// below the selection, false otherwise</param>
		/// <param name="fWantBothEnds">if true, we want to be able to see both ends of
		/// the selection, if possible; if it is too large, align top to top.</param>
		/// <param name="fForcePrepareToDraw">Pass this as true when the selection was made under program
		/// control, not from a click. It indicates that even though the selection may be initially
		/// visible, there might be lazy boxes on screen; and when we come to paint, expanding them
		/// might make the selection no longer visible. Therefore we should not skip the full
		/// process just because the selection is already visible.</param>
		/// <remarks>
		/// Note: subclasses for which scrolling is disabled should override.
		/// If <paramref name="vwsel"/> is null, make the current selection visible.
		/// </remarks>
		/// <returns>True if the selection was made visible, false if it did nothing</returns>
		/// -----------------------------------------------------------------------------------
		protected virtual bool MakeSelectionVisible(IVwSelection vwsel, bool fWantOneLineSpace,
			bool fWantBothEnds, bool fForcePrepareToDraw)
		{
			// TODO: LT-2268,2508 - Why is this selection going bad...?
			// The if will handle the crash, but there is still the problem
			// of the selections getting invalid.
			if (!vwsel.IsValid)
				return false; // can't work with an invalid selection

			if (fWantOneLineSpace && ClientHeight < LineHeight * 3)
			{
				// The view is too short to have a line at the top and/or bottom of the line
				// we want to scroll into view. Since we can't possibly do a good job of
				// scrolling the selection into view in this circumstance, we just don't
				// even try.
				fWantOneLineSpace = false;
			}

			// if we're not forcing prepare to draw and the entire selection is visible then there is nothing to do
			if (!fForcePrepareToDraw && IsSelectionVisible(vwsel, fWantOneLineSpace, fWantBothEnds))
				return false;

			using (new HoldGraphics(this))
			{
				bool fEndBeforeAnchor;
				Rectangle rcIdeal;
				SelectionRectangle(vwsel, out rcIdeal, out fEndBeforeAnchor);

				//rcIdeal.Inflate(0, 1); // for good measure
				// OK, we want rcIdeal to be visible.
				// dy gets added to the scroll offset. This means a positive dy causes there to be more
				// of the view hidden above the top of the screen. This is the same effect as clicking a
				// down arrow, which paradoxically causes the window contents to move up.
				int dy = 0;
				int dyRange = 0;
				int ydTop;

				#region DoAutoVScroll
				if (DoAutoVScroll)
				{
					// This loop repeats until we have figured out a scroll distance AND confirmed
					// that we can draw that location without messing things up.
					for (; ; )
					{
						// Get the amount to add to rdIdeal to make it comparable with the window
						// postion.
						ydTop = -ScrollPosition.Y; // Where the window thinks it is now.
						// Adjust for that and also the height of the (optional) header.
						rcIdeal.Offset(0, ydTop - m_dyHeader); // Was in drawing coords, adjusted by top.
						int ydBottom = ydTop + ClientHeight - m_dyHeader;

						// Is the end of the selection partly off the top of the screen?
						int extraSpacing = fWantOneLineSpace ? LineHeight : 0;
						if (!fWantBothEnds)
						{
							// For a range (that is not a picture) we scroll just far enough to show the end.
							if (fEndBeforeAnchor)
							{
								if (rcIdeal.Top < ydTop + extraSpacing)
									dy = rcIdeal.Top - (ydTop + extraSpacing);
								else if (rcIdeal.Top > ydBottom - extraSpacing)
									dy = rcIdeal.Top - (ydBottom - extraSpacing) + LineHeight;
							}
							else
							{
								if (rcIdeal.Bottom < ydTop + extraSpacing)
									dy = rcIdeal.Bottom - (ydTop + extraSpacing) - LineHeight;
								else if (rcIdeal.Bottom > ydBottom - extraSpacing)
									dy = rcIdeal.Bottom - (ydBottom - extraSpacing);
							}
						}
						else
						{
							// we want both ends to be visible if possible; if not, as much as possible at the top.
							// Also if it is completely off the bottom of the screen, a good default place to put it is near
							// the top.
							if (rcIdeal.Top < ydTop + extraSpacing || rcIdeal.Height > ydBottom - ydTop - extraSpacing * 2 || rcIdeal.Top > ydBottom)
							{
								// The top is not visible, or there isn't room to show it all: scroll till the
								// top is just visible, after the specified gap.
								dy = rcIdeal.Top - (ydTop + extraSpacing);
							}
							else if (rcIdeal.Bottom > ydBottom - extraSpacing)
							{
								// scroll down minimum to make bottom visible. This involves
								// hiding more text at the top: positive dy.
								dy = rcIdeal.Bottom - (ydBottom - extraSpacing);
							}
						}
						// Else the end of the selection is already visible, do nothing.
						// (But still make sure we can draw the requisite screen full
						// without messing stuff up. Just in case a previous lazy box
						// expansion looked like making it visible, but another makes it
						// invisible again...I'm not sure this can happen, but play safe.)

						// OK, we need to move by dy. But, if that puts the selection near the
						// bottom of the screen, we may have to expand a lazy box above it in
						// order to display a whole screen full. If the size estimate is off
						// (which it usually is), that would affect the position' where the
						// selection gets moved to. To avoid this, we make the same PrepareToDraw
						// call that the rendering code will make before drawing after the scroll.
						Rectangle rcSrc, rcDst;
						GetCoordRects(out rcSrc, out rcDst);
						rcDst.Offset(0, -dy); // Want to draw at the position we plan to move to.

						// Get the whole range we are scrolling over. We use this later to see whether
						// PrepareToDraw messed anything up.
						dyRange = m_rootb.Height;

						if (m_rootb != null && m_dxdLayoutWidth > 0)
						{
							SaveSelectionInfo(rcIdeal, ydTop);
							VwPrepDrawResult xpdr = VwPrepDrawResult.kxpdrAdjust;
							// I'm not sure this loop is necessary because we repeat the outer loop until no
							// change in the root box height (therefore presumably no adjustment of scroll position)
							// happens. But it's harmless and makes it more obvious that the code is correct.
							while (xpdr == VwPrepDrawResult.kxpdrAdjust)
							{
								// In case the window contents aren't visible yet, it's important to do this as if it were.
								// When the window is invisible, the default clip rectangle is empty, and we don't
								// 'prepare to draw' very much. We want to be sure that we can draw everything in the window
								// without somehow affecting the position of this selection.
								Rect clipRect = new Rect(0, 0, ClientRectangle.Width, ClientHeight);
								((IVwGraphicsWin32)m_graphicsManager.VwGraphics).SetClipRect(ref clipRect);
								xpdr = PrepareToDraw(rcSrc, rcDst);
								GetCoordRects(out rcSrc, out rcDst);
							}
						}

						// If PrepareToDraw didn't change the scroll range, it didn't mess
						// anything up and we can use the dy we figured. Otherwise, loop and
						// figure it again with more complete information, because something at a
						// relevant point has been expanded to real boxes.
						if (m_rootb.Height == dyRange)
							break;

						// Otherwise we need another iteration, we need to recompute the
						// selection location in view of the changes to layout.
						SelectionRectangle(vwsel, out rcIdeal, out fEndBeforeAnchor);
						dy = 0; // Back to initial state.
					} // for loop
				}
				#endregion
				ydTop = -ScrollPosition.Y; // Where the window thinks it is now.
				SaveSelectionInfo(rcIdeal, ydTop);

				if (dy - ScrollPosition.Y < 0)
					dy = ScrollPosition.Y; // make offset 0 if it would have been less than that

				// dx gets added to the scroll offset. This means a positive dx causes there to
				// be moreof the view hidden left of the screen. This is the same effect as
				// clicking a right arrow, which paradoxically causes the window contents to
				// move left.
				int dx = 0;
				int xdLeft = -ScrollPosition.X; // Where the window thinks it is now.
				#region DoAutoHScroll
				if (DoAutoHScroll)
				{
					if (m_orientationManager.IsVertical)
					{
						// In all current vertical views we have no vertical scrolling, so only need
						// to consider horizontal. Also we have no laziness, so no need to mess with
						// possible effects of expanding lazy boxes that become visible.
						// In this case, rcPrimary's top is the distance from the right of the ClientRect to the
						// right of the selection, and the height of rcPrimary is a distance further left.
						int right = rcIdeal.Top; // distance to left of right edge of window
						int left = right + rcIdeal.Height;
						if (fWantOneLineSpace)
						{
							right -= LineHeight;
							left += LineHeight;
						}
						if (right < 0)
						{
							// selection is partly off the right of the window
							dx = -right; // positive dx to move window contents left.
						}
						else if (left > ClientRectangle.Width)
						{
							dx = ClientRectangle.Width - left; // negative to move window contents right
						}
					}
					else // not a vertical window, normal case
					{

						rcIdeal.Offset(xdLeft, 0); // Was in drawing coords, adjusted by left.
						// extra 4 pixels so Ip doesn't disappear at right.
						int xdRight = xdLeft + ClientRectangle.Width;

						// Is the selection right of the right side of the screen?
						if (rcIdeal.Right > xdRight)
							dx = rcIdeal.Right - xdRight;

						// Is the selection partly off the left of the screen?
						if (rcIdeal.Left < xdLeft)
						{
							// Is it bigger than the screen?
							if (rcIdeal.Width > ClientRectangle.Width && !fEndBeforeAnchor)
							{
								// Is it bigger than the screen?
								if (rcIdeal.Width > ClientRectangle.Width && !fEndBeforeAnchor)
								{
									// Left is off, and though it is too big to show entirely, we can show
									// more. Move the window contents right (negative dx).
									dx = rcIdeal.Right - xdRight;
								}
								else
								{
									// Partly off left, and fits: move window contents right (less is hidden,
									// neg dx).
									dx = rcIdeal.Left - xdLeft;
								}
							}
							else
							{
								// Left of selection is right of (or at) the left side of the screen.
								// Is right of selection right of the right side of the screen?
								if (rcIdeal.Right > xdRight)
								{
									if (rcIdeal.Width > ClientRectangle.Width && fEndBeforeAnchor)
									{
										// Left is visible, right isn't: move until lefts coincide to show as much
										// as possible. This is hiding more text left of the window: positive dx.
										dx = rcIdeal.Left - xdLeft;
									}
									else
									{
										// Fits entirely: scroll left minimum to make right visible. This involves
										// hiding more text at the left: positive dx.
										dx = rcIdeal.Right - xdRight;
									}
								}
								// Else it is already entirely visible, do nothing.
							}
							if (dx > Width - ClientRectangle.Width - 1 + ScrollPosition.X)
							{
								// This value makes it the maximum it can be, except this may make it negative
								dx = Width - ClientRectangle.Width - 1 + ScrollPosition.X;
							}
							if (dx - ScrollPosition.X < 0)
								dx = ScrollPosition.X; // make offset 0 if it would have been less than that
						}
					}
					int ScrollRangeX = ScrollRange.Width;
					if (dx > ScrollRangeX - ClientRectangle.Width - 1 + ScrollPosition.X)
					{
						// This value makes it the maximum it can be, except this may make it negative
						dx = ScrollRangeX - ClientRectangle.Width - 1 + ScrollPosition.X;
					}
					if (dx - ScrollPosition.X < 0)
						dx = ScrollPosition.X; // make offset 0 if it would have been less than that
				}
				#endregion
				if (dx != 0 || dy != 0)
				{
					// Update the scroll bar.
					ScrollPosition = new Point(xdLeft + dx, ydTop + dy);
				}
				Invalidate();
				//Update();
			}

			return true;
		}

		/// <summary>
		/// Save some selection location information if needed.
		/// </summary>
		/// <param name="rcIdeal"></param>
		/// <param name="ydTop"></param>
		protected virtual void SaveSelectionInfo(Rectangle rcIdeal, int ydTop)
		{
			// Some subclasses (XmlBrowseViewBase to be exact) need to store some of this information.
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>Position the insertion point at the page top</summary>
		/// <param name="fIsShiftPressed">True if the shift key is pressed and selection is
		/// desired</param>
		/// -----------------------------------------------------------------------------------
		protected void GoToPageTop(bool fIsShiftPressed)
		{
			// commented the content of this method out as it isn't working correctly. This may be a good starting point :)

			int newX = this.ClientRectangle.Left;
			int newY = this.ClientRectangle.Top;

			Debug.Assert(m_rootb != null);
			if (m_rootb == null)
				return;

			Rectangle rcSrcRoot;
			Rectangle rcDstRoot;
			using (new HoldGraphics(this))
			{
				GetCoordRects(out rcSrcRoot, out rcDstRoot);
			}

			if (fIsShiftPressed)
			{
				m_rootb.MouseDownExtended(newX, newY, rcSrcRoot, rcDstRoot);
			}
			else
			{
				m_rootb.MouseDown(newX, newY, rcSrcRoot, rcDstRoot);
			}

		}

		/// -----------------------------------------------------------------------------------
		/// <summary>Position the insertion point at the page bottom</summary>
		/// <param name="fIsShiftPressed">True if the shift key is pressed and selection is
		/// desired</param>
		/// -----------------------------------------------------------------------------------
		protected void GoToPageBottom(bool fIsShiftPressed)
		{
			// commented the content of this method out as it isn't working correctly. This may be a good starting point :)

			int newX = this.ClientRectangle.Right;
			int newY = this.ClientRectangle.Bottom;

			Debug.Assert(m_rootb != null);
			if (m_rootb == null)
				return;

			Rectangle rcSrcRoot;
			Rectangle rcDstRoot;
			using (new HoldGraphics(this))
			{
				GetCoordRects(out rcSrcRoot, out rcDstRoot);
			}

			if (fIsShiftPressed)
				m_rootb.MouseDownExtended(newX, newY, rcSrcRoot, rcDstRoot);
			else
				m_rootb.MouseDown(newX, newY, rcSrcRoot, rcDstRoot);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Scroll the selection in to the given client position.
		/// </summary>
		/// <param name="sel">The selection</param>
		/// <param name="dyPos">Position from top of client window where sel should be scrolled</param>
		/// <returns>True if the selection was scrolled into view, false if this function did
		/// nothing</returns>
		/// ------------------------------------------------------------------------------------
		public bool ScrollSelectionToLocation(IVwSelection sel, int dyPos)
		{
			CheckDisposed();
			if (m_rootb == null)
				return false;

			if (sel == null)
				sel = m_rootb.Selection;
			if (sel == null)
				return false;

			using (new HoldGraphics(this))
			{
				// Put IP at top of window.
				MakeSelectionVisible(sel);

				Rectangle rcSrcRoot;
				Rectangle rcDstRoot;

				// Expand the lazy boxes 1 screen up
				GetCoordRects(out rcSrcRoot, out rcDstRoot);
				rcDstRoot.Offset(0, -ClientRectangle.Height);
				PrepareToDraw(rcSrcRoot, rcDstRoot);

				// Expand the lazy boxes 1 screen down
				GetCoordRects(out rcSrcRoot, out rcDstRoot);
				rcDstRoot.Offset(0, ClientRectangle.Height);
				PrepareToDraw(rcSrcRoot, rcDstRoot);

				// Get the difference between where we want the selection to be and its
				// current position
				Rect rcPrimary;
				Rect rcSecondary;
				bool fSplit;
				bool fEndBeforeAnchor;
				GetCoordRects(out rcSrcRoot, out rcDstRoot);
				sel.Location(m_graphicsManager.VwGraphics, rcSrcRoot, rcDstRoot, out rcPrimary,
					out rcSecondary, out fSplit, out fEndBeforeAnchor);
				int difference = dyPos - rcPrimary.top;

				// Now move the scroll position so the IP will be where we want it.
				ScrollPosition = new Point(-ScrollPosition.X, -ScrollPosition.Y - difference);
			}

			// If the selection is still not visible (which should only be the case if
			// we're at the end of the view), just take whatever MakeSelectionVisible()
			// gives us).
			if (!IsSelectionVisible(sel) && dyPos >= 0 && dyPos <= ClientHeight)
				MakeSelectionVisible(sel);
			//Update();
			return true;
		}

		/// <summary>
		/// Wraps PrepareToDraw calls so as to suppress attempts to paint or any similar re-entrant call
		/// we might make while getting ready to do it.
		/// </summary>
		/// <param name="rcDstRoot"></param>
		/// <param name="rcSrcRoot"></param>
		protected VwPrepDrawResult PrepareToDraw(Rectangle rcSrcRoot, Rectangle rcDstRoot)
		{
			if (m_fInPaint || m_fInLayout)
				return VwPrepDrawResult.kxpdrNormal; // at least prevent loops

			m_fInPaint = true;
			try
			{
				return m_rootb.PrepareToDraw(m_graphicsManager.VwGraphics, rcSrcRoot, rcDstRoot);
			}
			finally
			{
				m_fInPaint = false;
			}
		}

		// Translation help between SCROLLINFO and .NET:
		// sinfo.nMax	- DisplayRectangle.Height
		// sinfo.nPage - Height or ClientHeight
		// sinfo.nPos  - ScrollPosition.Y
		#endregion

		#region Command handlers
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Handle the Edit/Select All menu command.
		/// </summary>
		/// <param name="args">The arguments</param>
		/// <returns><c>true</c> if message handled, otherwise <c>false</c>.</returns>
		/// -----------------------------------------------------------------------------------
		protected bool OnEditSelectAll(object args)
		{
			using (new WaitCursor(this))
			{
				if (DataUpdateMonitor.IsUpdateInProgress())
					return true;

				if (Visible && !DesignMode)
				{
					SelectAll();
					return true;
				}
				return false;
			}
		}
		#endregion

		#region Event handling methods
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Flash the insertion point.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// -----------------------------------------------------------------------------------
		protected virtual void OnTimer(object sender, EventArgs e)
		{
			if (m_rootb != null && Focused)
				m_rootb.FlashInsertionPoint(); // Ignore any error code.
		}

		/// <summary>
		/// Allow the orientation manager to convert arrow key codes.
		/// </summary>
		/// <param name="keyValue"></param>
		/// <returns></returns>
		internal int ConvertKeyValue(int keyValue)
		{
			return m_orientationManager.ConvertKeyValue(keyValue);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>The Keyman keyboard has changed. Determine the writing system that is
		/// probably implied, and apply it to the current range and/or future typing.</summary>
		/// ------------------------------------------------------------------------------------
		public void OnKeymanKeyboardChange(IntPtr wpFlags, IntPtr lpHKL)
		{
			CheckDisposed();
			if (m_rootb == null)
				return; // For paranoia.
			IVwSelection vwsel = m_rootb.Selection;
			if (vwsel == null)
				return; // can't do anything useful.

			ILgWritingSystemFactory wsf;
			List<int> vws = EditingHelper.GetWsListCurrentFirst(vwsel, out wsf);
			if (vws == null)
				return; // not enough valid writing systems to make it worth changing.

			string sKeymanKbd = KeyboardHelper.ActiveKeymanKeyboard;

			int wsMatch = -1;
			foreach (int ws in vws)
			{
				// Don't consider switching to the default, dummy writing system.
				if (ws == 0)
					continue;

				ILgWritingSystem lgws = wsf.get_EngineOrNull(ws);
				if (lgws == null)
					continue;

				string sWsKbd = lgws.Keyboard;
				if (sKeymanKbd == sWsKbd)
				{
					wsMatch = ws;
					if (wsMatch == vws[0])
						return; // no change from current.
					break;
				}
			}

			if (wsMatch == -1) // no known writing system uses this keyboard
				return;

			m_wsPending = -1;
			bool fRange = vwsel.IsRange;
			if (fRange)
			{
				// Delay handling it until we get an insertion point.
				m_wsPending = wsMatch;
				return;
			}

			// props of current selection, an IP (therefore only 1 lot of props).
			ITsTextProps[] vttpTmp;
			IVwPropertyStore[] vvpsTmp;
			int cttp;
			SelectionHelper.GetSelectionProps(vwsel, out vttpTmp, out vvpsTmp, out cttp);

			ITsPropsBldr tpb = vttpTmp[0].GetBldr();
			tpb.SetIntPropValues((int)FwTextPropType.ktptWs,
				(int)FwTextPropVar.ktpvDefault, wsMatch);
			ITsTextProps[] rgttpNew = new ITsTextProps[1];
			rgttpNew[0] = tpb.GetTextProps();
			vwsel.SetSelectionProps(1, rgttpNew);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the accessible name that the root box will return for this root site.
		/// </summary>
		/// <param name="name"></param>
		/// ------------------------------------------------------------------------------------
		public void SetAccessibleName(string name)
		{
			CheckDisposed();
			IAccessible acc = AccessibleRootObject as IAccessible;
			if (acc != null)
				acc.set_accName(null, name);
			// TODO:
			// After review with JT, this symptom is likely a problem with a DataTreeView not
			// not removing it's Slices / views.  This should be investigated further.
			//			else
			//				Debug.WriteLine("IAccessible name Crash: " + name);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the accessible object from the root box (implements IAccessible)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public object AccessibleRootObject
		{
			get
			{
#if __MonoCS__
				return null; // TODO-Linux IOleServiceProvider not listed in QueryInterface issue.
#else
				CheckDisposed();
				Guid guid = Marshal.GenerateGuidForType(typeof(IOleServiceProvider));
				if (m_rootb == null)
				{
					return null;
				}
				object obj = null;
				if (m_rootb is IOleServiceProvider)
				{
					IOleServiceProvider sp = (IOleServiceProvider)m_rootb;
					if (sp == null)
					{
						// REVIEW (TomB): Shouldn't this just throw an exception?
						MessageBox.Show("Null IServiceProvider from root");
						Debug.Fail("Null IServiceProvider from root");
					}
					Guid guidAcc = Marshal.GenerateGuidForType(typeof(IAccessible));
					// 1st guid currently ignored.
					sp.QueryService(ref guidAcc, ref guidAcc, out obj);
				}
				return obj;
#endif
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Override the WndProc to handle WM_GETOBJECT so we can return the
		/// IAccessible implementation from the root box, rather than wrapping it
		/// as an AccessibleObject in .NET style. This is important because the test
		/// harness wants to be able to get back to the root box.  There are a couple
		/// of other messages we must handle at this level as well.
		///
		/// This override is now delegated through the message sequencer; see OriginalWndProc.
		/// </summary>
		/// <param name="m"></param>
		/// ------------------------------------------------------------------------------------
		protected override void WndProc(ref Message m)
		{
			CheckDisposed();

			m_messageSequencer.SequenceWndProc(ref m);
		}

		#endregion // Event handling methods

		#region Overriden methods (of UserControl)
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// When we go visible and we are waiting to refresh the display (do a rebuild) then
		/// call RefreshDisplay()
		/// </summary>
		/// <param name="e"></param>
		/// -----------------------------------------------------------------------------------
		protected override void OnVisibleChanged(EventArgs e)
		{
			CheckDisposed();

			base.OnVisibleChanged(e);
			if (Visible && m_fRootboxMade && m_rootb != null && m_fRefreshPending)
				RefreshDisplay();
		}

		/// <summary>
		/// Return a wrapper around the COM IAccessible for the root box.
		/// </summary>
		/// <returns></returns>
		protected override AccessibleObject CreateAccessibilityInstance()
		{
			AccessibleObject result = new AccessibilityWrapper(this, AccessibleRootObject as Accessibility.IAccessible);
			return result;
		}

		/// <summary>
		/// This provides an override opportunity for non-read-only views which do NOT
		/// want an initial selection made in OnLoad (e.g., InterlinDocForAnalysis) or OnGotFocus().
		/// </summary>
		public virtual bool WantInitialSelection
		{
			get
			{
				CheckDisposed();
				return IsEditable;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnLoad(EventArgs e)
		{
			CheckDisposed();

			base.OnLoad(e);

			if (m_fRootboxMade && m_rootb != null && m_fAllowLayout)
			{
				PerformLayout();

				if (m_rootb.Selection == null && WantInitialSelection && m_dxdLayoutWidth > 0)
				{
					try
					{
						m_rootb.MakeSimpleSel(true, IsEditable, false, true);
					}
					catch (COMException)
					{
						// Ignore
					}
				}
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// The window is first being created.
		/// </summary>
		/// <param name="e"></param>
		/// -----------------------------------------------------------------------------------
		protected override void OnHandleCreated(EventArgs e)
		{
			CheckDisposed();

			base.OnHandleCreated(e);

			if (DesignMode && !AllowPaintingInDesigner)
				return;

			// If it is the second pane of a split window, it may have been given a copy of the
			// first child's root box before the window gets created.
			if (m_rootb == null && m_fMakeRootWhenHandleIsCreated)
			{
				using (new HoldGraphics(this))
				{
					MakeRoot();
					m_dxdLayoutWidth = kForceLayout; // Don't try to draw until we get OnSize and do layout.
				}
				// TODO JohnT: In case of an exception, do we have to display some message to
				// the user? Or go ahead with create, but set up Paint to display some message?
			}

			RegisterForInputLanguageChanges();

			// Create a timer used to flash the insertion point if any.
			// We flash every half second (500 ms).
			// Note that the timer is not started until we get focus.
			m_Timer.Interval = 500;
			m_Timer.Tick += new EventHandler(OnTimer);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Do cleaning up when handle gets destroyed
		/// </summary>
		/// <param name="e"></param>
		/// <remarks>Formerly AfVwRootSite::OnReleasePtr()</remarks>
		/// -----------------------------------------------------------------------------------
		protected override void OnHandleDestroyed(EventArgs e)
		{
			CheckDisposed();

			base.OnHandleDestroyed(e);

			if (DesignMode)
				return;

			// We generally need to close the rootbox here or we get memory leaks all over. But
			// if we always do it, we break switching fields in DE views, and anywhere else a
			// root box is shared.
			// Subclasses should override CloseRootBox if something else is still using it
			// after the window closes.
			CloseRootBox();

			// we used to call Marshal.ReleaseComObject here, but this proved to be unnecessary,
			// because we're going out of scope and so the GC takes care of that.
			// NOTE: ReleaseComObject() returned a high number of references. These are
			// references to the RCW (Runtime Callable Wrapper), not to the C++ COM object,
			// and the GC handles that.
			m_rootb = null;
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
			// Can't set/get focus if we don't have a rootbox.  For some reason we may try
			// to process this message under those circumstances during an Undo() in IText (LT-2663).
			if (IsDisposed || m_rootb == null)
				return;
			try
			{
				//Control lost = Control.FromHandle(m.WParam);
				//Debug.WriteLine(string.Format("SimpleRootSite.OnSetFocus:\n\t\t\tlost {0} ({1}), Name={2}\n\t\t\tnew {3} ({4}), Name={5}",
				//    lost != null ? lost.ToString() : "<null>",
				//    lost != null ? lost.Handle.ToInt32() : -1,
				//    lost != null ? lost.Name : "<empty>",
				//    this, Handle, Name));
				m_fHandlingOnGotFocus = true;
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
#if !__MonoCS__
				IntPtr hwndOld = m.WParam;
				int procIdOld, procIdThis;
				Win32.GetWindowThreadProcessId(hwndOld, out procIdOld);
				Win32.GetWindowThreadProcessId(Handle, out procIdThis);
				if (procIdOld == procIdThis && m_rootb != null && EditingHelper != null)
					EditingHelper.SetKeyboardForSelection(m_rootb.Selection);
#else
			 // TODO-Linux: possibly need to call SetKeyboardForSelection here
#endif

				// Start the blinking cursor timer here and stop it in the OnKillFocus handler later.
				if (m_Timer != null)
					m_Timer.Start();
			}
			finally
			{
				m_fHandlingOnGotFocus = false;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// View is getting focus: Activate the rootbox and set the appropriate keyboard
		/// </summary>
		/// <param name="e"></param>
		/// -----------------------------------------------------------------------------------
		protected override void OnGotFocus(EventArgs e)
		{
			CheckDisposed();
#if __MonoCS__
			if (m_inputBusController != null)
				m_inputBusController.Focus();
#endif

			//Debug.WriteLine("SimpleRootSite.OnGotFocus() hwnd = " + this.Handle);
			// If it hasn't been registered yet, register OnInputLanguageChanged().
			if (m_fRegisteredOnInputLangChanged == false)
				RegisterForInputLanguageChanges();

			g_focusRootSite.Target = this;
			base.OnGotFocus(e);
			if (DesignMode || !m_fRootboxMade || m_rootb == null)
				return;

			// Enable selection display
			if (WantInitialSelection)
				this.EnsureDefaultSelection();
			Activate(VwSelectionState.vssEnabled);

			// Restore the state of the new keyboard to the previous value. If we don't do
			// that e.g. in Chinese IME the input mode will toggle between English and
			// Chinese (LT-7487 et al).
			// We used to do this only in OnInputLanguageChanged(), but WM_INPUTLANGCHANGED
			// messages are no longer sequenced (but WM_SETFOCUS is), and so the field doesn't
			// have focus yet when we process WM_INPUTLANGCHANGED. This means we don't call
			// RestoreKeyboardStatus which results in toggling between English and Chinese
			// input of Chinese IME.
			int wsSel = SelectionHelper.GetFirstWsOfSelection(m_rootb.Selection);
			if (wsSel != 0)
			{
				ILgWritingSystem qws = WritingSystemFactory.get_EngineOrNull(wsSel);
				if (qws != null)
					RestoreKeyboardStatus(qws.LCID);
			}

			EditingHelper.GotFocus();
		}

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
#if __MonoCS__
			if (m_inputBusController != null)
				m_inputBusController.KillFocus();
#endif
			// If we have a timer (used for making the insertion point blink), then we want to stop it.
			if (m_Timer != null)
				m_Timer.Stop();

			SimpleRootSite newFocusRootSite = null;
			// If g_focusRootSite is something other than this (or null), then some other root
			// site received OnGotFocus before this one received OnLostFocus. Leave it recorded
			// as the current focus root site. If this is still the focus root site, then
			// focus has not yet switched to another root site, so there is currently none.
			if (g_focusRootSite.Target != this)
				newFocusRootSite = g_focusRootSite.Target as SimpleRootSite;
			else
			{
				// This is not a nice kludge, but it takes care of the special case where
				// a Windows ComboBox grabs focus willy-nilly, and will allow tests for the
				// 'real' focus + maintaing g_focusRootSite in sensible manner.
				if (newWindow != null && (newWindow.GetType().Name != "ToolStripComboBoxControl"))
				{
					// We can't set g_focusRootSite.Target to null, so instead we create a new
					// empty weak reference.
					g_focusRootSite = new WeakReference(null);
				}
			}
			//Debug.WriteLine("SimpleRootSite.OnKillFocus() - hwnd = " + this.Handle +
			//    ", newFocusRootSite = " + (newFocusRootSite == null ? "null" : "this"));

			if (DesignMode || m_rootb == null)
				return;
			//Debug.WriteLine("Losing focus");
			// This event may occur while a large EditPaste is inserting text,
			// and if so we need to bail out to avoid a crash.
			if (DataUpdateMonitor.IsUpdateInProgress())
				return;

			// NOTE: Do not call RemoveCmdHandler or SetActiveRootBox(NULL) here. There are many
			// cases where the view window loses focus, but we still want to keep track of the
			// last view window.  If it is necessary to forget about the view window, do it
			// somewhere else.  If nothing else sets the last view window to NULL, it will be
			// cleared when the view window gets destroyed.

			m_rootb.LoseFocus();

			UpdateSelectionEnabledState(newWindow);

			UpdateCurrentKeyboardStatus();

			// This window is losing control of the keyboard, so make sure when we get the focus
			// again we reset it to what we want.
			EditingHelper.LostFocus(newWindow, fIsChildWindow);

			if (newFocusRootSite == null)
			{
				//Debug.WriteLine("SimpleRootSite.OnLostFocus() - set keyboard to default - for " + Name);
				//Debug.WriteLine("SimpleRootSite.OnLostFocus() - newWindow = " + (newWindow == null ? "<null>" : newWindow.Name));
				// Not switching to another root site...or at least, we haven't told that root
				// site about it yet, so it will have a chance to fix things up when it gets focus.

				//This is a major kludge to prevent a spurious WM_KEYUP for VK_CONTROL from
				// interrupting a mouse click.
				// If mouse button was pressed elsewhere causing this loss of focus,
				//  activate a message pre-filter in this root site to throw away the
				// spurious WM_KEYUP for VK_CONTROL that happens as a result of switching
				// to the default keyboard layout.
				if ((MouseButtons & MouseButtons.Left) != 0 && !m_messageFilterInstalled)
				{
					Application.AddMessageFilter(this);
					m_messageFilterInstalled = true;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the current keyboard status so that we can restore this state.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void UpdateCurrentKeyboardStatus()
		{
#if !__MonoCS__
			IntPtr context = Win32.ImmGetContext(new HandleRef(this, Handle));
			if (context != IntPtr.Zero)
			{
				int conversionMode;
				int sentenceMode;
				Win32.ImmGetConversionStatus(new HandleRef(this, context),
					out conversionMode, out sentenceMode);
				int lcid = LcidHelper.LangIdFromLCID(InputLanguage.CurrentInputLanguage.Culture.LCID);
				s_KeyboardModes[lcid] = new LcidKeyboardMode(conversionMode, sentenceMode);
			}
#else
			// TODO-Linux: May have to do something with keyboard here
#endif
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Restores the IME keyboard status.
		/// </summary>
		/// <param name="lcid">The locale id.</param>
		/// ------------------------------------------------------------------------------------
		private void RestoreKeyboardStatus(int lcid)
		{
#if !__MonoCS__
			// Restore the state of the new keyboard to the previous value. If we don't do
			// that e.g. in Chinese IME the input mode will toggle between English and
			// Chinese (LT-7488).
			LcidKeyboardMode keyboardMode;
			if (s_KeyboardModes.TryGetValue(lcid, out keyboardMode) && keyboardMode.HasValue)
			{
				IntPtr context = Win32.ImmGetContext(new HandleRef(this, Handle));
				if (context != IntPtr.Zero)
				{
					Win32.ImmSetConversionStatus(new HandleRef(this, context),
						keyboardMode.ConversionMode, keyboardMode.SentenceMode);
				}
			}
#else
		// TODO-Linux: do we need to port this?
#endif
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Process mouse move
		/// </summary>
		/// <param name="e"></param>
		/// -----------------------------------------------------------------------------------
		protected override void OnMouseMove(MouseEventArgs e)
		{
			CheckDisposed();

			base.OnMouseMove(e);

			if (DataUpdateMonitor.IsUpdateInProgress() || MouseMoveSuppressed)
				return;

			// REVIEW: Do we need the stuff implemented in AfVwWnd::OnMouseMove?

			// Convert to box coords and pass to root box (if any)
			Point mousePos = new Point(e.X, e.Y);
			if (m_rootb != null)
			{
				using (new HoldGraphics(this))
				{
					Rectangle rcSrcRoot;
					Rectangle rcDstRoot;
					GetCoordRects(out rcSrcRoot, out rcDstRoot);

					// For now at least we care only if the mouse is down.
					if (e.Button == MouseButtons.Left)
						CallMouseMoveDrag(PixelToView(mousePos), rcSrcRoot, rcDstRoot);
				}

				// TODO (TE-8540, LT-10281): Implement tooltips for hyperlinks
				bool fFoundLinkStyle = false;
				if (fFoundLinkStyle) //fInObject &&
				//(FwObjDataTypes)odt == FwObjDataTypes.kodtExternalPathName)
				{
					IVwSelection vwsel;
					string strbFile;
					if (GetExternalLinkSel(out fFoundLinkStyle, out vwsel, out strbFile, mousePos)
						&& fFoundLinkStyle)
					{
						//						if (!m_hwndExtLinkTool)
						//						{
						//							m_hwndExtLinkTool = ::CreateWindow(TOOLTIPS_CLASS, NULL, TTS_ALWAYSTIP,
						//													  0, 0, 0, 0, Window()->Hwnd(), 0, ModuleEntry::GetModuleHandle(), NULL);
						//						}
						//
						//						// Adjust the filename if needed.
						//						if (AfApp::Papp())
						//										 {
						//											 LpMainWnd * pwnd = dynamic_cast<LpMainWnd *>(AfApp::Papp()->GetCurMainWnd());
						//											 if (pwnd)
						//											 {
						//												 AfLpInfo * plpi = pwnd->GetLpInfo();
						//												 AssertPtr(plpi);
						//												 plpi->MapExternalLink(strbFile);
						//											 }
						//										 }
						//
						//						HoldGraphics hg(this);
						//						Rect rcPrimary;
						//						Rect rcSecondary;
						//						ComBool fSplit;
						//						ComBool fEndBeforeAnchor;
						//						CheckHr(qvwsel->Location(hg.m_pvg, hg.m_rcSrcRoot, hg.m_rcDstRoot, &rcPrimary,
						//							&rcSecondary, &fSplit, &fEndBeforeAnchor));
						//
						//						// See if this LinkedFiles Link has already been added to the tooltip control.
						//						// We use the top left corner of the LinkedFiles Link as its ID.
						//						TOOLINFO ti = { TTTOOLINFOA_V2_SIZE, TTF_TRANSPARENT | TTF_SUBCLASS};
						//						ti.hwnd = Window()->Hwnd();
						//						ti.uId = MAKELPARAM(rcPrimary.left, rcPrimary.top);
						//						ti.rect = rcPrimary;
						//						ti.hinst = ModuleEntry::GetModuleHandle();
						//						ti.lpszText = (LPSTR)strbFile.Chars();
						//						if (!::SendMessage(m_hwndExtLinkTool, TTM_GETTOOLINFO, 0, (LPARAM)&ti))
						//																							  {
						//																								  // Add this LinkedFiles Link rectangle to the tooltip.
						//																								  ::SendMessage(m_hwndExtLinkTool, TTM_ADDTOOL, 0, (LPARAM)&ti);
						//
						//																								  // Remove any other tools in the tooltip.
						//																								  if (::SendMessage(m_hwndExtLinkTool, TTM_GETTOOLCOUNT, 0, 0) > 1)
						//																																								  {
						//																																									  ::SendMessage(m_hwndExtLinkTool, TTM_ENUMTOOLS, 0, (LPARAM)&ti);
						//																																									  ::SendMessage(m_hwndExtLinkTool, TTM_DELTOOL, 0, (LPARAM)&ti);
						//																																								  }
						//																							  }
					}
				}
				//				if (!fFoundLinkStyle)
				//				{
				//					// Remove all tools in the tooltip.
				//					if (::SendMessage(m_hwndExtLinkTool, TTM_GETTOOLCOUNT, 0, 0) == 1)
				//																					 {
				//																						 TOOLINFO ti = { TTTOOLINFOA_V2_SIZE };
				//																						 ::SendMessage(m_hwndExtLinkTool, TTM_ENUMTOOLS, 0, (LPARAM)&ti);
				//																						 ::SendMessage(m_hwndExtLinkTool, TTM_DELTOOL, 0, (LPARAM)&ti);
				//																					 }
				//				}
			}

			OnMouseMoveSetCursor(mousePos);
		}

		/// <summary>
		/// Allow clients to override cursor type during OnMouseMove.
		/// </summary>
		/// <param name="mousePos"></param>
		protected virtual void OnMouseMoveSetCursor(Point mousePos)
		{
			EditingHelper.SetCursor(mousePos, m_rootb);
		}

		/// <summary>
		/// Return true if the view is currently painting itself.
		/// (This is useful because of Windows' pathological habit of calling the WndProc re-entrantly.
		/// Some things should not happen while the paint is in progress and the view may be
		/// under construction.)
		/// </summary>
		public bool PaintInProgress
		{
			get { return m_fInPaint; }
		}

		/// <summary>
		/// Return true if a layout of the view is in progress.
		/// (This is useful because of Windows' pathological habit of calling the WndProc re-entrantly.
		/// Some things should not happen while the paint is in progress and the view may be
		/// under construction.)
		/// </summary>
		public bool LayoutInProgress
		{
			get { return m_fInLayout; }
		}

		/// <summary>
		/// Return true if a paint needs to be aborted because something like a paint or layout
		/// is already in progress. Arranges for a postponed paint if so.
		/// </summary>
		protected bool CheckForRecursivePaint()
		{
			if (m_fInPaint || m_fInLayout || m_rootb != null && m_rootb.IsPropChangedInProgress)
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
				// Calling Invalidate directly can cause an infinite loop of paint calls in certain
				// circumstances.  But we don't want to leave the window incorrectly painted.
				// Postponing the new Invalidate until the application is idle seems a good compromise.
				// In THEORY it should only paint when idle anyway, except for explicit Update calls.
				Application.Idle += PostponedInvalidate;
				return true;
			}
			return false;
		}
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Call Draw() which does all the real painting
		/// </summary>
		/// <param name="e"></param>
		/// -----------------------------------------------------------------------------------
		protected override void OnPaint(PaintEventArgs e)
		{
			CheckDisposed();

			if (!AllowPainting)
				return;

			// Inform subscribers that the view changed.
			if (VerticalScrollPositionChanged != null &&
				m_lastVerticalScrollPosition != -AutoScrollPosition.Y)
			{
				VerticalScrollPositionChanged(this, m_lastVerticalScrollPosition,
					-AutoScrollPosition.Y);
				m_lastVerticalScrollPosition = -AutoScrollPosition.Y;
			}

			// NOTE: This "if" needs to stay in synch with the "if" in OnPaintBackground to
			// paint something when the views code isn't painting.
			if (!m_fAllowLayout)
			{
				base.OnPaint(e);
				return;
			}

			if (CheckForRecursivePaint())
				return;

			m_fInPaint = true;
			try
			{
				//long ticks = DateTime.Now.Ticks;
				Draw(e);
				//int elapsed = (int)(DateTime.Now.Ticks - ticks);
				//totalPaintTime += elapsed;
				//if (++cPaint == 10)
				//{
				//    cPaint = 0;
				//    Debug.WriteLine("Last ten paints averaged " + (totalPaintTime / 100) + " microseconds");
				//    totalPaintTime = 0;
				//}
			}
			finally
			{
				m_fInPaint = false;
			}
			MouseMoveSuppressed = false; // successful paint, display is stable to do MouseMoved.
		}

		/// <summary>
		/// Executed on the Application Idle queue, this method invalidates the whole view.
		/// It is used when we cannot properly complete a paint because we detect that it
		/// is a recursive call, to ensure that the window is eventually painted properly.
		/// </summary>
		void PostponedInvalidate(object sender, EventArgs e)
		{
			Application.Idle -= PostponedInvalidate;
			Invalidate();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Ignore the PaintBackground event because we do it ourself (in the views code).
		/// </summary>
		/// <param name="e"></param>
		/// -----------------------------------------------------------------------------------
		protected override void OnPaintBackground(PaintEventArgs e)
		{
			CheckDisposed();

#pragma warning disable 219
			Form parent = FindForm();
#pragma warning restore 219
			// Not everything has a form, when it is being designed.
			if (!m_fAllowLayout && AllowPainting)
				base.OnPaintBackground(e);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Recompute the layout
		/// </summary>
		/// <param name="levent"></param>
		/// -----------------------------------------------------------------------------------
		protected override void OnLayout(LayoutEventArgs levent)
		{
			CheckDisposed();

			if ((!DesignMode || AllowPaintingInDesigner) && m_fRootboxMade && m_fAllowLayout &&
				IsHandleCreated && !m_fInLayout)
			{
				// Recompute your layout and redraw completely, unless the width has not
				// actually changed.
				// We have to do this before we call base.OnLayout, because the base class copies
				// the scroll range from AutoScrollMinSize to the DisplayRectangle. If we call
				// base.OnLayout first, we get bugs when switching back and forth between maximize
				// and normal.
				using (new HoldGraphics(this))
				{
					if (DoLayout())
						Invalidate();
				}
			}

#if !__MonoCS__
			base.OnLayout(levent);
#else
			// TODO-Linux: Create simple test case for this and fix mono bug
			try
			{
				base.OnLayout(levent);
			}
			catch(System.ArgumentOutOfRangeException)
			{
				// TODO-Linux: Invesigate real cause of this.
				// I think it caused by mono setting ScrollPosition to 0 when Scrollbar isn't visible
				// it should possibly set it to Minimun value instread.
				// Currently occurs when dropping down scrollbars in Flex.
			}
#endif
		}

		/// <summary>
		/// Get a selection at the indicated point (as in MouseEventArgs.Location). If fInstall is true make it the
		/// active selection.
		/// </summary>
		public IVwSelection GetSelectionAtPoint(Point position, bool fInstall)
		{
			return GetSelectionAtViewPoint(PixelToView(position), fInstall);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a selection at the indicated point.
		/// </summary>
		/// <param name="position">Point where the selection is to be made</param>
		/// <param name="fInstall">Indicates whether or not to "install" the seleciton</param>
		/// ------------------------------------------------------------------------------------
		public IVwSelection GetSelectionAtViewPoint(Point position, bool fInstall)
		{
			Rectangle rcSrcRoot, rcDstRoot;
			GetCoordRects(out rcSrcRoot, out rcDstRoot);
			try
			{
				return RootBox.MakeSelAt(position.X, position.Y, rcSrcRoot, rcDstRoot, false);
			}
			catch
			{
				// Ignore errors
				return null;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Process left or right mouse button down
		/// </summary>
		/// <param name="e"></param>
		/// -----------------------------------------------------------------------------------
		protected override void OnMouseDown(MouseEventArgs e)
		{
			CheckDisposed();

			base.OnMouseDown(e);

#if __MonoCS__
			if (m_inputBusController != null)
				m_inputBusController.NotifyMouseClick();
#endif

			if (m_rootb == null || DataUpdateMonitor.IsUpdateInProgress())
				return;
			// If we have posted a FollowLink message, then don't process any other
			// mouse event, since will be changing areas, invalidating our MouseEventArgs.
			if (IsFollowLinkMsgPending)
				return;

			SwitchFocusHere();

			// Convert to box coords and pass to root box (if any).
			Point pt;
			Rectangle rcSrcRoot;
			Rectangle rcDstRoot;
			using (new HoldGraphics(this))
			{
				pt = PixelToView(new Point(e.X, e.Y));
				GetCoordRects(out rcSrcRoot, out rcDstRoot);

				// Note we need to UninitGraphics before processing CallMouseDown since that call
				// may jump to another record which invalidates this graphics object.
			}

			if (e.Button == MouseButtons.Right)
			{
				if (DataUpdateMonitor.IsUpdateInProgress())
					return; //discard this event
				if (IsFollowLinkMsgPending)
				{
					// REVIEW (TimS): This uninit graphics doesn't appear to be needed... is there
					// a reason it was put in? It could cause problems because we init the graphics
					// outside of this method and have no idea what might happen if this method and
					// the method that calls this method both uninit the graphics object. Although
					// no known problems have been seen, it was commented out.
					//UninitGraphics();
					return; //discard this event
				}
				using (new HoldGraphics(this))
				{
					IVwSelection sel = (IVwSelection)m_rootb.Selection;
					// Use the existing selection if it covers the point clicked, and if it's
					// either a range or editable.
					if (sel != null && (sel.IsRange || SelectionHelper.IsEditable(sel)))
					{
						// TODO KenZ: We need a better way to determine if the cursor is in a selection
						// when the selection spans partial lines.

						// We don't want to destroy a range selection if we are within the range, since it
						// is quite likely the user will want to do a right+click cut or paste.
						Rect rcPrimary;
						Rect rcSecondary;
						bool fSplit;
						bool fEndBeforeAnchor;

						Debug.Assert(m_graphicsManager.VwGraphics != null);

						sel.Location(m_graphicsManager.VwGraphics, rcSrcRoot, rcDstRoot,
							out rcPrimary, out rcSecondary, out fSplit,
							out fEndBeforeAnchor);

						if (pt.X >= rcPrimary.left && pt.X < rcPrimary.right
							&& pt.Y >= rcPrimary.top && pt.Y < rcPrimary.bottom)
						{
							return;
						}
					}

					try
					{
						// Make an invisible selection to see if we are in editable text.
						sel = m_rootb.MakeSelAt(pt.X, pt.Y, rcSrcRoot, rcDstRoot, false);
						if (sel != null && SelectionHelper.IsEditable(sel))
						{
							// Make a simple text selection without executing a mouse click. This is
							// needed in order to not launch a hot link when we right+click.
							m_rootb.MakeSelAt(pt.X, pt.Y, rcSrcRoot, rcDstRoot, true);
							return;
						}
					}
					catch
					{
					}
				}
			}

			if ((ModifierKeys & Keys.Shift) == Keys.Shift)
				CallMouseDownExtended(pt, rcSrcRoot, rcDstRoot);
			else
				CallMouseDown(pt, rcSrcRoot, rcDstRoot);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Process right mouse button up (typically show a context menu).
		/// Was mouse Down in an earlier life, but we concluded that the usual convention
		/// is context menu on mouse up. There may be vestiges.
		/// </summary>
		/// <param name="pt"></param>
		/// <param name="rcSrcRoot"></param>
		/// <param name="rcDstRoot"></param>
		/// <returns>true if handled, false otherwise</returns>
		/// -----------------------------------------------------------------------------------
		protected virtual bool OnRightMouseUp(Point pt, Rectangle rcSrcRoot,
			Rectangle rcDstRoot)
		{
			IVwSelection invSel = null;
			try
			{
				// Make an invisible selection to see if we are in editable text, also it may
				// be useful to DoContextMenu.
				invSel = m_rootb.MakeSelAt(pt.X, pt.Y, rcSrcRoot, rcDstRoot, false);
				// Notify any delegates.
				if (RightMouseClickedEvent != null)
				{
					if (invSel != null)
					{
						FwRightMouseClickEventArgs args = new FwRightMouseClickEventArgs(pt, invSel);
						RightMouseClickedEvent(this, args);
						if (args.EventHandled)
							return true;
					}
				}
			}
			catch
			{
			}

			if (DataUpdateMonitor.IsUpdateInProgress())
				return true; //discard this event
			if (IsFollowLinkMsgPending)
			{
				// REVIEW (TimS): This uninit graphics doesn't appear to be needed... is there
				// a reason it was put in? It could cause problems because we init the graphics
				// outside of this method and have no idea what might happen if this method and
				// the method that calls this method both uninit the graphics object. Although
				// no known problems have been seen, it was commented out.
				//UninitGraphics();
				return true; //discard this event
			}

			return DoContextMenu(invSel, pt, rcSrcRoot, rcDstRoot);
		}

		/// <summary>
		/// The user has chosen a keyboard combination which requests a context menu.
		/// Handle it, given the active selection and a point around the center of it.
		/// </summary>
		protected virtual bool HandleContextMenuFromKeyboard(IVwSelection vwsel, Point center)
		{
			if (RightMouseClickedEvent != null)
			{
				FwRightMouseClickEventArgs args = new FwRightMouseClickEventArgs(center, vwsel);
				RightMouseClickedEvent(this, args);
				if (args.EventHandled)
					return true;
			}
			return false;
		}

		/// <summary>
		/// Override to provide default handling of Context manu key.
		/// </summary>
		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			if (keyData == Keys.Apps || keyData == (Keys.F10 | Keys.Shift))
			{
				if (RootBox != null && RootBox.Selection != null)
				{
					Point pt;
					// Set point to somewhere around the middle of the selection in window coords.
					using (new HoldGraphics(this))
					{
						Rectangle rcSrcRoot, rcDstRoot;
						Rect rcSec, rcPrimary;
						bool fSplit, fEndBeforeAnchor;
						GetCoordRects(out rcSrcRoot, out rcDstRoot);
						RootBox.Selection.Location(m_graphicsManager.VwGraphics, rcSrcRoot, rcDstRoot, out rcPrimary,
							out rcSec, out fSplit, out fEndBeforeAnchor);

						pt = new Point((rcPrimary.right + rcPrimary.left)/2,
							(rcPrimary.top + rcPrimary.bottom)/2);
					}
					if (HandleContextMenuFromKeyboard(RootBox.Selection, pt))
						return true;
					// These two checks are copied from OnRightMouseUp; not sure why (or whether) they are needed.
					if (DataUpdateMonitor.IsUpdateInProgress())
						return true; //discard this event
					if (IsFollowLinkMsgPending)
					{
						return true; //discard this event
					}
					using (var hg = new HoldGraphics(this))
					{
						Rectangle rcSrcRoot, rcDstRoot;
						GetCoordRects(out rcSrcRoot, out rcDstRoot);
						return DoContextMenu(RootBox.Selection, pt, rcSrcRoot, rcDstRoot);
					}

				}
				return true;
			}
			return base.ProcessCmdKey(ref msg, keyData);
		}


		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Overide this to provide a context menu for some subclass.
		/// </summary>
		/// <param name="invSel"></param>
		/// <param name="pt"></param>
		/// <param name="rcSrcRoot"></param>
		/// <param name="rcDstRoot"></param>
		/// <returns></returns>
		/// -----------------------------------------------------------------------------------
		protected virtual bool DoContextMenu(IVwSelection invSel, Point pt, Rectangle rcSrcRoot,
			Rectangle rcDstRoot)
		{
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
			CheckDisposed();

			base.OnDoubleClick(e);

			// Convert to box coords and pass to root box (if any).
			if (m_rootb != null && !DataUpdateMonitor.IsUpdateInProgress())
			{
				SwitchFocusHere();
				using (new HoldGraphics(this))
				{
					Point pt = PointToClient(Cursor.Position);

					Rectangle rcSrcRoot;
					Rectangle rcDstRoot;
					GetCoordRects(out rcSrcRoot, out rcDstRoot);

					CallMouseDblClk(PixelToView(pt), rcSrcRoot, rcDstRoot);
				}
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Process mouse button up event
		/// </summary>
		/// <param name="e"></param>
		/// -----------------------------------------------------------------------------------
		protected override void OnMouseUp(MouseEventArgs e)
		{
			CheckDisposed();

			base.OnMouseUp(e);
			if (DataUpdateMonitor.IsUpdateInProgress())
				return;

			// Convert to box coords and pass to root box (if any).
			if (m_rootb != null)
			{
				using (new HoldGraphics(this))
				{
					Point pt = PixelToView(new Point(e.X, e.Y));
					Rectangle rcSrcRoot;
					Rectangle rcDstRoot;
					GetCoordRects(out rcSrcRoot, out rcDstRoot);
					CallMouseUp(pt, rcSrcRoot, rcDstRoot);

					if (e.Button == MouseButtons.Right)
					{
						bool fHandled = OnRightMouseUp(pt, rcSrcRoot, rcDstRoot);
						if (fHandled)
							return;
					}
				}
			}
		}

		/// <summary>
		/// Handles OnKeyPress. Passes most things to EditingHelper.OnKeyPress
		/// </summary>
		/// <param name="e"></param>
		protected override void OnKeyPress(KeyPressEventArgs e)
		{
			CheckDisposed();

			base.OnKeyPress(e);
			if (!e.Handled)
			{
				// Let the views code handle the key
				using (new HoldGraphics(this))
				{
					Debug.Assert(m_graphicsManager.VwGraphics != null);
					Keys modifiers = ModifierKeys;
					// Ctrl-Backspace actually comes to us having the same key code as Delete, so we fix it here.
					// (Ctrl-Delete doesn't come through this code path at all.)
					if (modifiers == Keys.Control && e.KeyChar == '\u007f')
						e.KeyChar = '\b';
					EditingHelper.OnKeyPress(e, modifiers);
				}
			}
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
				ignoredKey = true;
			else if ((modifiers & Keys.Control) == Keys.Control)
			{
				// only backspace, forward delete and control-M (same as return key) will be
				// passed on for processing
				ignoredKey = !((int)e.KeyChar == (int)VwSpecialChars.kscBackspace ||
					(int)e.KeyChar == (int)VwSpecialChars.kscDelForward ||
					e.KeyChar == '\r');
			}

			return ignoredKey;
		}

		/// <summary>
		/// Gets or sets a value indicating whether pressing the TAB key will be handled by this control
		/// instead of moving the focus to the next control in the tab order.
		/// </summary>
		public bool AcceptsTab
		{
			get
			{
				CheckDisposed();
				return m_acceptsTab;
			}

			set
			{
				CheckDisposed();
				m_acceptsTab = value;
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether pressing the ENTER key will be handled by this control
		/// instead of activating the default button for the form.
		/// </summary>
		public bool AcceptsReturn
		{
			get
			{
				CheckDisposed();
				return m_acceptsReturn;
			}

			set
			{
				CheckDisposed();
				m_acceptsReturn = value;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Clean up after page scrolling.
		/// </summary>
		/// <param name="e"></param>
		/// -----------------------------------------------------------------------------------
		protected override void OnKeyUp(KeyEventArgs e)
		{
			CheckDisposed();

			if (DataUpdateMonitor.IsUpdateInProgress())
				return; //throw this event away

			base.OnKeyUp(e);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// User pressed a key.
		/// </summary>
		/// <param name="e"></param>
		/// -----------------------------------------------------------------------------------
		protected override void OnKeyDown(KeyEventArgs e)
		{
			CheckDisposed();

			if (DataUpdateMonitor.IsUpdateInProgress())
				return; //throw this event away


			using (new HoldGraphics(this))
			{
				if (e.KeyCode == Keys.PageUp && ((e.Modifiers & Keys.Control) == Keys.Control))
				{
					GoToPageTop((e.Modifiers & Keys.Shift) == Keys.Shift);
				}
				else if (e.KeyCode == Keys.PageDown && ((e.Modifiers & Keys.Control) == Keys.Control))
				{
					GoToPageBottom((e.Modifiers & Keys.Shift) == Keys.Shift);
				}
				else
				{
					base.OnKeyDown(e);
				}

				Debug.Assert(m_graphicsManager.VwGraphics != null);

				EditingHelper.OnKeyDown(e);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Allow derived classes to override this to control showing context menus.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Obsolete("Assign the context menu to the ContextMenu property and override the popup " +
			 "event for any additional initializations")]
		protected virtual void OnContextMenu(Point pt)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified key is a regular input key or a special key that
		/// requires preprocessing.
		/// Default implementation always returns true because we want to handle all keys!
		/// </summary>
		/// <param name="keyData">One of the <see cref="T:System.Windows.Forms.Keys"/> values.</param>
		/// <returns>
		/// true if the specified key is a regular input key; otherwise, false.
		/// </returns>
		/// <remarks>IsInputKey gets called while processing WM_KEYDOWN or WM_SYSKEYDOWN.</remarks>
		/// ------------------------------------------------------------------------------------
		protected override bool IsInputKey(Keys keyData)
		{
			CheckDisposed();

			if ((keyData & Keys.Alt) != Keys.Alt)
			{
				switch (keyData & Keys.KeyCode)
			{
					case Keys.Return:
						return m_acceptsReturn;

					case Keys.Escape:
						return false;

					case Keys.Tab:
						return m_acceptsTab && (keyData & Keys.Control) == Keys.None;
				}
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This also helps us handle all input keys...the documentation doesn't make very
		/// clear the distinction between this and IsInputKey, but failing to override this
		/// one can cause typing in the view to invoke buttons in the parent form. If you
		/// want to remove this, make sure it doesn't mess up windows that contain both views
		/// and buttons, such as the LexText interlinear view.
		/// </summary>
		/// <param name="charCode">The character to test.</param>
		/// <returns>
		/// true if the character should be sent directly to the control and not preprocessed;
		/// otherwise, false.
		/// </returns>
		/// <remarks>IsInputChar gets called while processing WM_CHAR or WM_SYSCHAR.</remarks>
		/// ------------------------------------------------------------------------------------
		protected override bool IsInputChar(char charCode)
		{
			CheckDisposed();

			return true;
			//return base.IsInputChar (charCode);
		}

		/// <summary>
		/// Gets a value indicating whether a selection changed is being handled.
		/// </summary>
		public virtual bool InSelectionChanged
		{
			get { return false; }
			set { }
		}

		/// <summary>
		/// Subclasses can override if they handle the scrolling for OnSizeChanged().
		/// </summary>
		protected virtual bool ScrollToSelectionOnSizeChanged
		{
			get { return true; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Size changed. Ensure that the selection is still visible.
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnSizeChanged(EventArgs e)
		{
			CheckDisposed();

			// Ignore if our size didn't really change, also if we're in the middle of a paint.
			// (But, don't ignore if not previously laid out successfully...this can suppress
			// a necessary Layout after the root box is made.)
			if (m_fInPaint || m_fInLayout || m_rootb == null || (Size == m_sizeLast && m_dxdLayoutWidth >= 0))
			{
				// This is needed to handle .Net layout for the Control.
				base.OnSizeChanged(e);
				return;
			}

			// If the selection is not visible, then make a selection at the top of the view
			// that we can scroll to the top after the size changes. This keeps the view from
			// scrolling all over the place when the size is changed.
			IVwSelection selToScroll = null;
			if (ScrollToSelectionOnSizeChanged && !IsSelectionVisible(null) && AllowLayout)
			{
				using (new HoldGraphics(this))
				{
					Rectangle rcSrc;
					Rectangle rcDst;
					GetCoordRects(out rcSrc, out rcDst);
					m_fInLayout = true;
					try
					{
						using (new SuspendDrawing(this))
							selToScroll = m_rootb.MakeSelAt(5, 5, rcSrc, rcDst, false);
					}
					catch
					{
						// just ignore any errors we get...
						selToScroll = null;
					}
					finally
					{
						m_fInLayout = false;
					}
				}
			}
#if !__MonoCS__
			base.OnSizeChanged(e);
#else
			try
			{
				base.OnSizeChanged(e);
			}
			catch(System.ArgumentOutOfRangeException)
			{
				// TODO-Linux: Invesigate real cause of this.
				// I think it caused by mono setting ScrollPosition to 0 when Scrollbar isn't visible
				// it should possibly set it to Minimun value instread.
				// Currently occurs when dropping down scrollbars in Flex.
			}
#endif

			// Sometimes (e.g., I tried this in InterlinDocChild), we destroy the root box when
			// the pane is collapsed below a workable size.
			if (m_rootb == null)
				return;

			// This was moved down here because the actual size change happens in
			// base.OnSizeChanged() so our size property could be wrong before that call
			m_sizeLast = Size;

			// REVIEW: We don't think it makes sense to do anything more if our width is 0.
			// Is this right?
			if (m_sizeLast.Width <= 0)
				return;


			if (ScrollToSelectionOnSizeChanged &&
				!SizeChangedSuppression &&
				!m_fRefreshPending &&
				IsHandleCreated)
			{
				if (selToScroll == null)
					ScrollSelectionIntoView(null, VwScrollSelOpts.kssoDefault);
				else
				{
					// we want to scroll the selection into view at the top of the window
					// so that the text pretty much stays at the same position even when
					// the IP isn't visible.
					ScrollSelectionIntoView(selToScroll, VwScrollSelOpts.kssoTop);
				}
			}
		}

		// ENHANCE (EberhardB): implement passing of System characters to rootbox. Since the
		// rootbox doesn't do anything with that at the moment, I left that for later.

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether the <see cref="P:System.Windows.Forms.Control.ImeMode"/>
		/// property can be set to an active value, to enable IME support.
		/// </summary>
		/// <returns>true in all cases.</returns>
		/// <remarks>This fixes part of LT-7487. This property requires .NET 2.0 SP1 or higher.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		protected override bool CanEnableIme
		{
			get { return true; }
		}
		#endregion

		#region Other virtual methods
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Set/Get the writing system factory used by this root site. The main RootSite
		/// subclass obtains this from the FdoCache if it has not been set.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public virtual ILgWritingSystemFactory WritingSystemFactory
		{
			get
			{
				CheckDisposed();
				return m_wsf;
			}
			set
			{
				CheckDisposed();
				m_wsf = value;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Returns the scroll position. Values are positive.
		/// </summary>
		/// <param name="dxd"></param>
		/// <param name="dyd"></param>
		/// -----------------------------------------------------------------------------------
		protected internal virtual void GetScrollOffsets(out int dxd, out int dyd)
		{
			dxd = -ScrollPosition.X;
			dyd = -ScrollPosition.Y;
		}

		///------------------------------------------------------------------------------------
		/// <summary>
		/// Adjust the scroll range when some lazy box got expanded.
		/// This is rather similar to SizeChanged, but is used when the size changed
		/// as a result of recomputing something that is invisible (typically about to become
		/// visible, but not currently on screen). Thus, the scroll bar range and possibly
		/// position need adjusting, but it isn't necessary to actually redraw anything except
		/// the scroll bar--unless the scroll position is forced to change, because we were
		/// in the process of scrolling to somewhere very close to the end, and the expansion
		/// was smaller than predicted, and the total range is now less than the current
		/// position.
		/// </summary>
		/// <param name="dxdSize"><paramref name="dydSize"/></param>
		/// <param name="dxdPosition"><paramref name="dydPosition"/></param>
		/// <param name="dydSize">The change (positive means larger) in the overall size of the
		/// root box</param>
		/// <param name="dydPosition">The position where the change happened. In general it may be
		/// assumed that if this change is above the thumb position, everything that changed
		/// is above it, and it needs to be increased by dydSize; otherwise, everything is below
		/// the screen, and no change to the thumb position is needed.</param>
		/// <returns><c>true</c> when the scroll position changed, otherwise <c>false</c>.
		/// </returns>
		/// <remarks>
		/// The <see cref="ScrollableControl.DisplayRectangle"/> determines the scroll range.
		/// To set it, you have to set <see cref="ScrollableControl.AutoScrollMinSize"/> and do
		/// a PerformLayout.
		/// </remarks>
		///------------------------------------------------------------------------------------
		protected virtual bool AdjustScrollRange1(int dxdSize, int dxdPosition, int dydSize,
			int dydPosition)
		{
			int dydRangeNew = AdjustedScrollRange.Height;
			int dydPosNew = -ScrollPosition.Y;

			// Remember: ScrollPosition returns negative values!

			// If the current position is after where the change occurred, it needs to
			// be adjusted by the same amount.
			if (dydPosNew > dydPosition)
				dydPosNew += dydSize;

			int dxdRangeNew = AutoScrollMinSize.Width + dxdSize;
			int dxdPosNew = -ScrollPosition.X;

			if (HScroll)
			{
				// Similarly for horizontal scroll bar.
				if (dxdPosNew > dxdPosition)
					dxdPosNew += dxdSize;
			}

			return UpdateScrollRange(dxdRangeNew, dxdPosNew, dydRangeNew, dydPosNew);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Draw to the given clip rectangle.
		/// </summary>
		/// <remarks>OPTIMIZE JohnT: pass clip rect to VwGraphics and make use of it.</remarks>
		/// <param name="e"></param>
		/// -----------------------------------------------------------------------------------
		protected virtual void Draw(PaintEventArgs e)
		{
			if (m_rootb != null && m_dxdLayoutWidth > 0 &&
				(!DesignMode || AllowPaintingInDesigner))
			{
				Debug.Assert(m_vdrb != null, "Need to call MakeRoot() before drawing");
				IntPtr hdc = e.Graphics.GetHdc();

				try
				{
					Form parent = FindForm();
					if (parent != null && !parent.Enabled)
					{
						if (m_haveCachedDrawForDisabledView)
							m_vdrb.ReDrawLastDraw(hdc, e.ClipRectangle);
						else
						{
							m_orientationManager.DrawTheRoot(m_vdrb, m_rootb, hdc, ClientRectangle,
								ColorUtil.ConvertColorToBGR(BackColor), true, ClientRectangle);
							m_haveCachedDrawForDisabledView = true;
						}
					}
					else
					{
						m_haveCachedDrawForDisabledView = false;
						// NOT (uint)(BackColor.ToArgb() & 0xffffff); this orders the components
						// as RRGGBB where C++ View code using the RGB() function requires BBGGRR.
						uint rgbBackColor = ColorUtil.ConvertColorToBGR(BackColor);
						//Debug.WriteLine("Drawing " + this.Handle + " " + e.ClipRectangle + " " + this.RectangleToScreen(e.ClipRectangle));
						//Debug.WriteLine("  Offset is " + this.ScrollPosition);

#if __MonoCS__ // TODO-Linux: FWNX-449: cairo 1.10.0 has a maximun size limitation of 32767.

						// The following code is a work around for a cairo limitation.
						// when the ClipRectangle Height exceeded 32767
						// (cairo is only use with mono)
						// This has the added benefit of increasing the scrolling speed in TE.

						m_orientationManager.DrawTheRoot(m_vdrb, m_rootb, hdc, ClientRectangle,
							rgbBackColor, true,
							Rectangle.Intersect(e.ClipRectangle, ClientRectangle));
#else
						m_orientationManager.DrawTheRoot(m_vdrb, m_rootb, hdc, ClientRectangle,
							rgbBackColor, true, e.ClipRectangle);
#endif
					}
				}
				catch
				{
					Debug.WriteLine("Draw exception.");	// Just eat it
				}
				finally
				{
					e.Graphics.ReleaseHdc(hdc);
				}
				try
				{
					InitGraphics();
					m_rootb.DrawingErrors(m_graphicsManager.VwGraphics);
				}
				catch (Exception ex)
				{
					ReportDrawErrMsg(ex);
				}
				finally
				{
					UninitGraphics();
				}
			}
			else
			{
				e.Graphics.FillRectangle(SystemBrushes.Window, this.ClientRectangle);
				e.Graphics.DrawString("Empty " + this.GetType().ToString(),
					SystemInformation.MenuFont, SystemBrushes.WindowText, this.ClientRectangle);
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Set focus to our window
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private void SwitchFocusHere()
		{
			if (FindForm() == Form.ActiveForm)
				Focus();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Override this method in your subclass.
		/// It should make a root box and initialize it with appropriate data and
		/// view constructor, etc.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void MakeRoot()
		{
			CheckDisposed();
			if (!GotCacheOrWs || (DesignMode && !AllowPaintingInDesigner))
				return;

			SetAccessibleName(Name);

#if !__MonoCS__
			m_vdrb = VwDrawRootBufferedClass.Create();
#else
			m_vdrb = new SIL.FieldWorks.Views.VwDrawRootBuffered(); // Managed object on Linux
#endif
			m_fRootboxMade = true;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Subclasses should override CloseRootBox if something else is still using it
		/// after the window closes. In that case the subclass should do nothing.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public virtual void CloseRootBox()
		{
			CheckDisposed();
			if (DesignMode || m_rootb == null)
				return;

			Form topForm = FindForm();
			if (topForm != null)
			{
				topForm.InputLanguageChanged -= OnInputLangChanged;
				m_fRegisteredOnInputLangChanged = false;
			}

			if (m_Timer != null)
				m_Timer.Stop();
			// Can't flash IP, if the root box is toast.

			m_rootb.Close();
			// After the rootbox is closed its useless...
			if (Marshal.IsComObject(m_rootb))
				Marshal.ReleaseComObject(m_rootb);
			m_rootb = null;
		}

		/// <summary>
		/// If a particular rootsite needs to do something special for putting the current
		/// selection on the keyboard, implement this method to do it, and have it return
		/// the filled-in ITsString object.  See LT-9475 for justification.
		/// </summary>
		public virtual ITsString GetTsStringForClipboard(IVwSelection vwsel)
		{
			return null;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Show the writing system choices?
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public virtual bool IsSelectionFormattable
		{
			get { return true; }
		}

		/// <summary>
		/// Answer true if the Apply Styles menu option should be enabled.
		/// </summary>
		public virtual bool CanApplyStyle
		{
			get { return IsSelectionFormattable; }
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Get the best style name that suits the selection.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected virtual string BestSelectionStyle
		{
			get { return String.Empty; }
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Show paragraph styles?
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected virtual bool IsSelectionInParagraph
		{
			get { return false; }
		}
		#region Methods that delegate events to the rootbox
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Call MouseDown on the rootbox
		/// </summary>
		/// <param name="point"></param>
		/// <param name="rcSrcRoot"></param>
		/// <param name="rcDstRoot"></param>
		/// -----------------------------------------------------------------------------------
		protected virtual void CallMouseDown(Point point, Rectangle rcSrcRoot,
			Rectangle rcDstRoot)
		{
			if (m_rootb != null)
			{
				IVwSelection oldSel = m_rootb.Selection;
				m_wsPending = -1;

				try
				{
					m_rootb.MouseDown(point.X, point.Y, rcSrcRoot, rcDstRoot);
				}
				catch
				{
					// This was causing unhandled exceptions from the MakeSelection call in the C++ code
#if __MonoCS__
					Debug.Assert(false, "m_rootb.MouseDown threw exception."); // So exception is silently hidden.
#endif
				}

				EditingHelper.HandleMouseDown();
				IVwSelection newSel = m_rootb.Selection;
				// Some clicks (e.g., on a selection check box in a bulk edit view) don't result in
				// a new selection. If something old is selected, it can be bad to scroll away from
				// the user's focus to the old selection.
				if (newSel != null && newSel != oldSel)
					MakeSelectionVisible(null);
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Call MouseDblClk on rootbox
		/// </summary>
		/// <param name="pt"></param>
		/// <param name="rcSrcRoot"></param>
		/// <param name="rcDstRoot"></param>
		/// -----------------------------------------------------------------------------------
		protected virtual void CallMouseDblClk(Point pt, Rectangle rcSrcRoot,
			Rectangle rcDstRoot)
		{
			if (m_rootb != null)
				m_rootb.MouseDblClk(pt.X, pt.Y, rcSrcRoot, rcDstRoot);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Call MouseDownExtended on the rootbox
		/// </summary>
		/// <param name="pt"></param>
		/// <param name="rcSrcRoot"></param>
		/// <param name="rcDstRoot"></param>
		/// -----------------------------------------------------------------------------------
		protected virtual void CallMouseDownExtended(Point pt, Rectangle rcSrcRoot,
			Rectangle rcDstRoot)
		{
			if (m_rootb != null)
			{
				m_wsPending = -1;

				m_rootb.MouseDownExtended(pt.X, pt.Y, rcSrcRoot, rcDstRoot);
				MakeSelectionVisible(null);
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Call MouseMoveDrag on the rootbox
		/// </summary>
		/// <param name="pt"></param>
		/// <param name="rcSrcRoot"></param>
		/// <param name="rcDstRoot"></param>
		/// -----------------------------------------------------------------------------------
		protected virtual void CallMouseMoveDrag(Point pt, Rectangle rcSrcRoot, Rectangle rcDstRoot)
		{
			if (m_rootb != null && m_fMouseInProcess == false)
			{
				// various scrolling speeds, i.e. 10 pixels per mousemovedrag event when
				// dragging beyond the top or bottom of of the rootbox
				const int LOSPEED = 10;
				const int HISPEED = 30;
				int speed;
				const int SLOW_BUFFER_ZONE = 30;

				m_fMouseInProcess = true;

				if (DoAutoVScroll && pt.Y < 0)  // if drag above the top of rootbox
				{
					// if mouse is within 30 pixels of top of rootbox
					if (pt.Y > -SLOW_BUFFER_ZONE)
						speed = LOSPEED;
					else
						speed = HISPEED;
					// regardless of where pt.Y is, select only to top minus "speed",
					// then scroll up by "speed" number of pixels, so top of selection is
					// always at top of rootbox
					m_rootb.MouseMoveDrag(pt.X, -speed, rcSrcRoot, rcDstRoot);
					ScrollPosition = new Point(-ScrollPosition.X, -ScrollPosition.Y - speed);
					Refresh();
				}
				else if (DoAutoVScroll && pt.Y > Bottom)  // if drag below bottom of rootbox
				{
					// if mouse is within 30 pixels of bottom of rootbox
					if ((pt.Y - Bottom) < SLOW_BUFFER_ZONE)
						speed = LOSPEED;
					else
						speed = HISPEED;
					// regardless of where pt.Y is, select only to bottom plus "speed",
					// then scroll down by "speed" number of pixels, so bottom of selection
					// is always at bottom of rootbox
					m_rootb.MouseMoveDrag(pt.X, Bottom + speed, rcSrcRoot, rcDstRoot);
					ScrollPosition = new Point(-ScrollPosition.X, -ScrollPosition.Y + speed);
					Refresh();
				}
				else  // selection and drag is occuring all within window boundaries
				{
					m_rootb.MouseMoveDrag(pt.X, pt.Y, rcSrcRoot, rcDstRoot);
				}
				m_fMouseInProcess = false;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Call MouseUp on the rootbox
		/// </summary>
		/// <param name="pt"></param>
		/// <param name="rcSrcRoot"></param>
		/// <param name="rcDstRoot"></param>
		/// -----------------------------------------------------------------------------------
		protected virtual void CallMouseUp(Point pt, Rectangle rcSrcRoot, Rectangle rcDstRoot)
		{
			int y = pt.Y;

			if (m_rootb != null)
			{
				if (Parent is ScrollableControl)
					y += ((ScrollableControl)Parent).AutoScrollPosition.Y;

				// if we're dragging to create or extend a selection
				// don't select text that is currently above or below current viewable area
				if (y < 0)  // if mouse is above viewable area
				{
					m_rootb.MouseUp(pt.X, 0, rcSrcRoot, rcDstRoot);
				}
				else if (y > Bottom)  // if mouse is below viewable area
				{
					m_rootb.MouseUp(pt.X, Bottom, rcSrcRoot, rcDstRoot);
				}
				else  // mouse is inside viewable area
				{
					m_rootb.MouseUp(pt.X, pt.Y, rcSrcRoot, rcDstRoot);
				}
			}
		}

		#endregion // Methods that delegate events to the rootbox

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// The system keyboard has changed. Determine the corresponding codepage to use when
		/// processing characters.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// <remarks>Because .NET uses only Unicode characters, there is no need to set a code
		/// page</remarks>
		/// -----------------------------------------------------------------------------------
		protected virtual void OnInputLangChanged(object sender, InputLanguageChangedEventArgs e)
		{
			if (IsDisposed || m_rootb == null || DataUpdateMonitor.IsUpdateInProgress())
				return;

			//Debug.WriteLine(string.Format("OnInputLangChanged: Handle={2}, g_focusRootSite={0}, culture={1}",
			//    g_focusRootSite.Target, e.InputLanguage.Culture.ToString(), Handle));
			// JT: apparently this comes to all the views, but only the active keyboard
			// needs to handle it.
			// SMc: furthermore, this is not really focused until OnGotFocus() has run.
			// Responding before that causes a nasty bug in language/keyboard selection.
			// SMc: also, we trust the lcid derived from vwsel more than we trust the one
			// passed in as e.CultureInfo.LCID.
			if (this.Focused && g_focusRootSite.Target == this)
			{
				// If possible, adjust the language of the selection to be one that matches
				// the keyboard just selected.
				IVwSelection vwsel = m_rootb.Selection;

				int lcid = LcidHelper.LangIdFromLCID(e.Culture.LCID);
				// Since we're being told it changed, assume this really is current, as opposed
				// to whatever we last set it to.
				m_editingHelper.ActiveLanguageId = lcid;
				if (m_fHandlingOnGotFocus)
				{
					int wsSel = SelectionHelper.GetFirstWsOfSelection(vwsel);
					if (wsSel != 0)
					{
						ILgWritingSystem qws = WritingSystemFactory.get_EngineOrNull(wsSel);
						if (qws != null)
							lcid = qws.LCID;
					}
				}
				HandleKeyboardChange(vwsel, (short)lcid);

				// The following line is needed to get Chinese IMEs to fully initialize.
				// This causes Text Services to set its focus, which is the crucial bit
				// of behavior.  See LT-7488 and LT-5345.
				Activate(VwSelectionState.vssEnabled);

				RestoreKeyboardStatus(lcid);

				//Debug.WriteLine("End SimpleRootSite.OnInputLangChanged(" + lcid +
				//    ") [hwnd = " + this.Handle + "] -> HandleKeyBoardChange(vwsel, " + lcid +
				//    ")");
			}
			//else
			//{
			//    Debug.WriteLine("End SimpleRootSite.OnInputLangChanged(" + LcidHelper.LangIdFromLCID(e.Culture.LCID).ToString() +
			//        ") [hwnd = " + this.Handle + "] -> no action");
			//}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// When the user has selected a keyboard from the system tray, adjust the language of
		/// the selection to something that matches, if possible.
		/// </summary>
		/// <param name="vwsel">Selection</param>
		/// <param name="nLangId">Language identification</param>
		/// -----------------------------------------------------------------------------------
		public virtual void HandleKeyboardChange(IVwSelection vwsel, short nLangId)
		{
			CheckDisposed();
			// Get the writing system factory associated with the root box.
			if (m_rootb == null || !GotCacheOrWs)
				return; // For paranoia.

			//			Debug.WriteLine("HandleKeyboardChange nLangId=" + nLangId + "; " + Name +
			//				"/" + this);

			ILgWritingSystemFactory wsf = WritingSystemFactory;

			int cws = wsf.NumberOfWs;
			if (cws < 2)
				return;	// no writing systems to work with

			int[] vwsTemp = new int[0];
			using (ArrayPtr ptr = MarshalEx.ArrayToNative<int>(cws))
			{
				wsf.GetWritingSystems(ptr, cws);
				vwsTemp = MarshalEx.NativeToArray<int>(ptr, cws);
			}

			// resize the array leaving slot 0 empty
			int[] vws = new int[++cws];
			Array.Copy(vwsTemp, 0, vws, 1, vwsTemp.Length);

			// Put the writing system of the selection first in the list, which gives it
			// priority -- we'll find it first if it matches.
			int wsSel = SelectionHelper.GetFirstWsOfSelection(vwsel);
			vws[0] = wsSel != 0 ? wsSel : vws[1];

			InputLanguage lngDefault = InputLanguage.DefaultInputLanguage;
			short defaultLangId = LcidHelper.LangIdFromLCID(lngDefault.Culture.LCID);
			int wsMatch = -1;
			int wsDefault = -1;
			int wsCurrentLang = -1; // used to note first ws whose CurrentInputLanguage matches.
			for (int iws = 0; iws < cws; iws++)
			{
				if (vws[iws] == 0)
					continue;

				ILgWritingSystem ws = wsf.get_EngineOrNull(vws[iws]);
				if (ws == null)
					continue;

				// REVIEW SteveMc, SharonC, KenZ, JohnT: nail down where the locale/langid belongs, in
				// the writing system or in the old writing system.
				int nLocale = ws.LCID;
				int nLangIdWs = LcidHelper.LangIdFromLCID(nLocale);

				if (nLangIdWs != 0 && nLangIdWs == nLangId)
				{
					wsMatch = vws[iws];
					break;
				}
				if (iws == 0 && nLangIdWs == 0 && nLangId == defaultLangId)
				{
					// The writing system of the current selection doesn't have any keyboard specified,
					// and we've set the keyboard to the default. This is acceptable; leave as is.
					wsMatch = vws[iws];
					break;
				}
				if (nLangIdWs == 0 && nLangId == defaultLangId && wsDefault == -1)
				{
					// Use this old writing system as the default.
					wsDefault = vws[iws];
				}
				if (wsCurrentLang == -1)
				{
					int nLangIdCurrent = ws.CurrentLCID;
					if (nLangId == nLangIdCurrent)
						wsCurrentLang = vws[iws];
				}
			}

			if (wsMatch == -1)
			{
				wsMatch = wsDefault;
			}
			m_wsPending = -1;
			// Next, see if it is the current langid of any ws. This will leave it -1 if we didn't find such a match.
			if (wsMatch == -1)
				wsMatch = wsCurrentLang;

			if (wsMatch == -1)
			{
				// Nothing matched.
				if (defaultLangId == nLangId) // We're trying to set to the default keyboard
				{
					// The default keyboard sets set for odd reasons. Just ignore it.
					// Review: what if the HKL's are different versions of the same language,
					// eg UK and US English?
				}
				else
				{
					// We will make this the current input language for the current writing system for the current session.
					ILgWritingSystem wsCurrent = wsf.get_EngineOrNull(wsSel);
					if (wsCurrent != null)
						wsCurrent.CurrentLCID = nLangId;
				}
				return;
			}

			// We are going to make wsMatch the current writing system.
			// Make sure it is set to use the langid that the user just selected.
			// (This cleans up any earlier overrides).
			ILgWritingSystem wsMatchEng = wsf.get_EngineOrNull(wsMatch);
			if (wsMatchEng != null)
				wsMatchEng.CurrentLCID = nLangId;

			if (vwsel == null)
			{
				// Delay handling it until we get a selection.
				m_wsPending = wsMatch;
				return;
			}
			bool fRange = vwsel.IsRange;
			if (fRange)
			{
				// Delay handling it until we get an insertion point.
				m_wsPending = wsMatch;
				return;
			}

			ITsTextProps[] vttp;
			IVwPropertyStore[] vvps;
			int cttp;

			SelectionHelper.GetSelectionProps(vwsel, out vttp, out vvps, out cttp);

			if (cttp == 0)
				return;
			Debug.Assert(cttp == 1);

			// If nothing changed, avoid the infinite loop that happens when we change the selection
			// and update the system keyboard, which in turn tells the program to change its writing system.
			// (This is a problem on Windows 98.)
			int wsTmp;
			int var;
			wsTmp = vttp[0].GetIntPropValues((int)FwTextPropType.ktptWs, out var);
			if (wsTmp == wsMatch)
				return;

			ITsPropsBldr tpb = vttp[0].GetBldr();
			tpb.SetIntPropValues((int)FwTextPropType.ktptWs,
				(int)FwTextPropVar.ktpvDefault, wsMatch);
			vttp[0] = tpb.GetTextProps();
			vwsel.SetSelectionProps(cttp, vttp);
			SelectionChanged(m_rootb, vwsel);
		}
		#endregion // Other virtual methods

		#region Other non-virtual methods

		/// <summary>
		/// Get the primary rectangle occupied by a selection (relative to the top left of the client rectangle).
		/// </summary>
		public Rect GetPrimarySelRect(IVwSelection sel)
		{
			Rect rcPrimary;
			using (new HoldGraphics(this))
			{
				Rectangle rcSrcRoot, rcDstRoot;
				GetCoordRects(out rcSrcRoot, out rcDstRoot);
				Rect rcSec;
				bool fSplit, fEndBeforeAnchor;
				sel.Location(m_graphicsManager.VwGraphics, rcSrcRoot, rcDstRoot, out rcPrimary,
							 out rcSec, out fSplit, out fEndBeforeAnchor);
			}
			return rcPrimary;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the state of the selection (enabled/disabled).
		/// </summary>
		/// <param name="newWindow">The window about to receive focus</param>
		/// ------------------------------------------------------------------------------------
		private void UpdateSelectionEnabledState(Control newWindow)
		{
			m_rootb.Activate(GetNonFocusedSelectionState(newWindow));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Unless overridden, this will return a value indicating that range selections
		/// should be hidden.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual VwSelectionState GetNonFocusedSelectionState(Control windowGainingFocus)
		{
			// If the selection exists, disable it.  This fixes LT-1203 and LT-1488.  (At one
			// point, we disabled only IP selections, but the leftover highlighted ranges
			// confused everyone.)
			// NOTE (TimS): The fix for LT-1203/LT-1488 broke TE's find replace so the
			// property ShowRangeSelAfterLostFocus was created.
			// Change (7/19/2006): Depending on the window that gets focus we leave the selection.
			// If user clicked e.g. on the toolbar it should still be enabled, but if user clicked
			// in a different view window we want to hide it (TE-3977). If user clicked on a
			// different app the selection should be hidden. If the user clicked on a second
			// window of our app we want to hide the selection.
			if ((m_fIsTextBox || windowGainingFocus == null || windowGainingFocus is SimpleRootSite ||
				ParentForm == null || !ParentForm.Contains(windowGainingFocus)) &&
				!ShowRangeSelAfterLostFocus)
			{
				return VwSelectionState.vssDisabled;
			}

			return VwSelectionState.vssOutOfFocus;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method is designed to be used by classes whose root object may be
		/// determined after the window's handle is created (when MakeRoot is normally
		/// called) and may be subsequently changed. It is passed the required
		/// arguments for SetRootObject, and if the root box already exists, all it
		/// does is call SetRootObject. If the root box does not already exist, it
		/// calls MakeRoot immediately, and arranges for the root box so created to
		/// be laid out, since it is possible that we have 'missed our chance'
		/// to have this happen when the window gets its initial OnSizeChanged message.
		/// </summary>
		/// <param name="hvoRoot"></param>
		/// <param name="vc"></param>
		/// <param name="frag"></param>
		/// <param name="styleSheet"></param>
		/// ------------------------------------------------------------------------------------
		public void ChangeOrMakeRoot(int hvoRoot, IVwViewConstructor vc, int frag,
			IVwStylesheet styleSheet)
		{
			CheckDisposed();
			if (RootBox == null)
			{
				MakeRoot();
				if (m_dxdLayoutWidth < 0)
				{
					OnSizeChanged(new EventArgs());
				}
			}
			else
				this.RootBox.SetRootObject(hvoRoot, vc, frag, styleSheet);
		}

		/// <summary>
		/// In DetailViews there are some root boxes which are created but (because scrolled out
		/// of view) never made visible. In certain circumstances, as their containers are
		/// disposed, something gets called that may cause calls to methods like OnLoad or
		/// OnHandleCreated. When we know a root site is about to become garbage, we want these
		/// methods to do as little as possible, both for performance and to prevent possible
		/// crashes as windows are created for (e.g.) objects that have been deleted.
		/// </summary>
		public void AboutToDiscard()
		{
			CheckDisposed();
			AllowLayout = false;
			m_fMakeRootWhenHandleIsCreated = false;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Make a selection that includes all the text.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public void SelectAll()
		{
			CheckDisposed();
			EditingHelper.SelectAll();
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find out if this RootSite had Focus before being grabbed by some other entity (such as a Windows ComboBox)
		/// </summary>
		/// ------------------------------------------------------------------------------------

		public bool WasFocused()
		{
			return g_focusRootSite.Target == this;
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Register for the InputLanguageChanged event of the form.  Don't do anything if it
		/// has already been registered.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void RegisterForInputLanguageChanges()
		{
			CheckDisposed();
			if (m_fRegisteredOnInputLangChanged == false)
			{
				Form topForm = FindForm();
				if (topForm != null)
				{
					topForm.InputLanguageChanged += OnInputLangChanged;
					m_fRegisteredOnInputLangChanged = true;
				}
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Some situations lead to invalidating very large rectangles. Something seems to go wrong
		/// if they are way bigger than the client rectangle. Finding the intersection makes it more
		/// reliable.
		/// </summary>
		/// <param name="rect"></param>
		/// <param name="fErase"></param>
		/// -----------------------------------------------------------------------------------
		private void CallInvalidateRect(Rectangle rect, bool fErase)
		{
			rect.Intersect(ClientRectangle);
			if (rect.Height <= 0 || rect.Width <= 0)
				return; // no overlap, may not produce paint.
			MouseMoveSuppressed = true; // until we paint and have a stable display.
			Invalidate(rect, fErase);

			//			Console.WriteLine("CallInvalidateRect: rect={0}, fErase={1}", rect, fErase);

			//			InitGraphics();
			//			Rect r = rect;
			//			((IVwGraphicsWin32)m_graphicsManager.VwGraphics).SetClipRect(ref r);
			//			UninitGraphics();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Construct coord transformation rectangles. Height and width are dots per inch.
		/// src origin is 0, dest origin is controlled by scrolling.
		/// </summary>
		/// <param name="rcSrcRoot"></param>
		/// <param name="rcDstRoot"></param>
		/// -----------------------------------------------------------------------------------
		protected internal void GetCoordRects(out Rectangle rcSrcRoot, out Rectangle rcDstRoot)
		{
			CheckDisposed();
			m_orientationManager.GetCoordRects(out rcSrcRoot, out rcDstRoot);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Allows RootSite to override, and still access the non-overridden ranges.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected virtual Size AdjustedScrollRange
		{
			get { return ScrollRange; }
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets the scroll ranges in both directions.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected Size ScrollRange
		{
			get
			{
				int dysHeight;
				int dxsWidth;

				try
				{
					dysHeight = m_rootb.Height;
					dxsWidth = m_rootb.Width;
				}
				catch (COMException e)
				{
					System.Diagnostics.Debug.WriteLine(string.Format("RootSite.UpdateScrollRange()" +
						": Unable to get height/width of rootbox. Source={0}, Method={1}, Message={2}",
						e.Source, e.TargetSite, e.Message));
					m_dxdLayoutWidth = kForceLayout; // No drawing until we get successful layout.
					return new Size(0, 0);
				}
				// Refactor JohnT: would it be clearer to extract this into a method of orientation manager?
				if (m_orientationManager.IsVertical)
				{
					//swap and add margins to height
					int temp = dysHeight;
					dysHeight = dxsWidth;
					dxsWidth = temp;
					dysHeight += HorizMargin * 2;
				}
				else
				{
					dxsWidth += HorizMargin * 2; // normal, add margins to width
				}

				Rectangle rcSrcRoot;
				Rectangle rcDstRoot;
				using (new HoldGraphics(this))
				{
					GetCoordRects(out rcSrcRoot, out rcDstRoot);
				}

				Size result = new Size(0, 0);

				// 32 bits should be enough for a scroll range but it may not be enough for
				// the intermediate result scroll range * 96 or so.
				// Review JohnT: should we do this adjustment differently if vertical?
				result.Height = (int)(((long)dysHeight) * rcDstRoot.Height / rcSrcRoot.Height);

				result.Width = (int)((long)dxsWidth * rcDstRoot.Width / rcSrcRoot.Width);

				// Add 8 to get well clear of the descenders of the last line;
				// about 4 is needed but we add a few more for good measure
				if (m_orientationManager.IsVertical)
					result.Width += 8;
				else
					result.Height += 8;
				return result;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// True if the control is being scrolled and should have its ScrollMinSize
		/// adjusted and its AutoScrollPosition modified. For example, an XmlBrowseViewBase
		/// scrolls, but using a separate scroll bar.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public virtual bool DoingScrolling
		{
			get
			{
				CheckDisposed();
				return AutoScroll;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Update your scroll range to reflect current conditions.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected void UpdateScrollRange()
		{
			if (!DoingScrolling || m_rootb == null)
				return;

			Size range = AdjustedScrollRange;

			Point scrollpos = ScrollPosition;
			UpdateScrollRange(range.Width, -scrollpos.X, range.Height, -scrollpos.Y);
		}

		/// <summary>
		/// Indicates whether a view wants a horizontal scroll bar.
		/// Note that just setting HScroll true cannot be used as an indication of this,
		/// because if ever the content width is small enough not to require a horizontal
		/// scroll bar, HScroll is set false by setting ScrollMinSize. If we use it as a flag,
		/// we will never after set a non-zero horizontal scroll min size.
		/// </summary>
		protected virtual bool WantHScroll
		{
			get { return m_orientationManager.IsVertical; }
		}

		/// <summary>
		/// Indicates whether we want a vertical scroll bar. For example, in a slice, we
		/// do not, even if we are autoscrolling to produce a horizontal one when needed.
		/// </summary>
		protected virtual bool WantVScroll
		{
			get { return true; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Update the scroll range with the new range and position.
		/// </summary>
		/// <param name="dxdRange">The new horizontal scroll range</param>
		/// <param name="dxdPos">The new horizontal scroll position</param>
		/// <param name="dydRange">The new vertical scroll range</param>
		/// <param name="dydPos">The new vertical scroll position</param>
		/// <returns><c>true</c> if the scroll position had to be changed because it ended up
		/// below the scroll range.</returns>
		/// ------------------------------------------------------------------------------------
		protected bool UpdateScrollRange(int dxdRange, int dxdPos, int dydRange, int dydPos)
		{
			bool fRet = false;
			int dydWindHeight = ClientHeight;
			int dxdWindWidth = ClientRectangle.Width;

			if (WantVScroll)
			{
				// If it is now too big, adjust it. Also, this means we must be in the
				// middle of a draw that is failing, so invalidate and set the return flag.
				if (dydPos > Math.Max(dydRange - dydWindHeight, 0))
				{
					dydPos = Math.Max(dydRange - dydWindHeight, 0);
					InvalidateForLazyFix();
					fRet = true;
				}
				// It is also possible that we've made it too small. This can happen if
				// expanding a lazy box reduces the real scroll range to zero.
				if (dydPos < 0)
				{
					dydPos = 0;
					InvalidateForLazyFix();
					fRet = true;
				}

				bool fOldSizeChangedSuppression = SizeChangedSuppression;
				bool fOldInLayout = m_fInLayout;
				try
				{
					// If setting the scroll range causes OnSizeChanged, which it does
					// in root site slaves, do NOT try to scroll to make the selection visible.
					SizeChangedSuppression = true;
					// It's also important to suppress our internal Layout code while we do this.
					// For some obscure reason, setting ScrollMinSize (at least) triggers a layout
					// event. This can be disastrous if we actually do a layout on the root box.
					// UpdateScrollRange can occur during expansion of lazy boxes, which in turn can
					// occur during loops where the current item might be a string box.
					// A Relayout at that point can change the state of the current box we are processing.
					// In any case it is a waste: we change the scroll position and range as a result of
					// laying out our contents; changing the scroll range and position do not affect
					// our contents.
					m_fInLayout = true;
					// Make the actual adjustment. Note we need to reset the page, because
					// Windows does not allow nPage to be more than the scroll range, so if we
					// ever (even temporarily) compute a smaller scroll range, something has to
					// set the page size back to its proper value of the window height.

					// In order to keep the scrollbar from bottoming out we must set the
					// size and the position in the correct order.  If the new position
					// is greater than or equal to the old, we must increase the size first.
					// If the new position is less than the old, we must set the position first.
					// (Using greater than instead of greater than or equal causes LT-4990/5164.)
					if (dydPos >= -ScrollPosition.Y)
					{
						ScrollMinSize = new Size(AutoScrollMinSize.Width, dydRange);
						ScrollPosition = new Point(-ScrollPosition.X, dydPos);
					}
					else
					{
						ScrollPosition = new Point(-ScrollPosition.X, dydPos);
						ScrollMinSize = new Size(AutoScrollMinSize.Width, dydRange);
					}
				}
				finally
				{
					// We have to remember the old value because setting the autoscrollminsize
					// in a root site slave can produce a recursive call as our own size is changed.
					SizeChangedSuppression = fOldSizeChangedSuppression;
					m_fInLayout = fOldInLayout;
				}
			}

			if (WantHScroll)
			{
				// Similarly for horizontal scroll bar.
				if (dxdPos > Math.Max(dxdRange - dxdWindWidth, 0))
				{
					dxdPos = Math.Max(dxdRange - dxdWindWidth, 0);
					InvalidateForLazyFix();
					fRet = true;
				}
				// It is also possible that we've made it too small. This can happen if
				// expanding a lazy box reduces the real scroll range to zero.
				if (dxdPos < 0)
				{
					dxdPos = 0;
					InvalidateForLazyFix();
					fRet = true;
				}

				if (dxdPos >= -ScrollPosition.X)
				{
					ScrollMinSize = new Size(dxdRange, (WantVScroll ? dydRange : 0));
					ScrollPosition = new Point(dxdPos, dydPos);
				}
				else
				{
					ScrollPosition = new Point(dxdPos, dydPos);
					ScrollMinSize = new Size(dxdRange, (WantVScroll ? dydRange : 0));
				}
			}

			return fRet;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Lay out your root box. If nothing significant has changed since the last layout, answer
		/// false; if it has, return true. Assumes that m_graphicsManager.VwGraphics is in a
		/// valid state (having a DC).
		/// </summary>
		/// <remarks>We assume that the VwGraphics object has already been setup for us
		/// </remarks>
		/// <returns>true if significant change since last layout, otherwise false</returns>
		/// -----------------------------------------------------------------------------------
		protected virtual bool DoLayout()
		{
			if (DesignMode && !AllowPaintingInDesigner)
				return false;

			Debug.Assert(m_graphicsManager.VwGraphics != null);

			if (m_rootb == null || Height == 0)
			{
				// Make sure we don't think we have a scroll range if we have no data!
				UpdateScrollRange();
				return false; // Nothing to do.
			}

			int dxdAvailWidth = GetAvailWidth(m_rootb);
			if (dxdAvailWidth != m_dxdLayoutWidth)
			{
				m_dxdLayoutWidth = dxdAvailWidth;
				if (!OkayToLayOut)
				{
					// No drawing until we get reasonable size.
					if (!OkayToLayOutAtCurrentWidth)
						m_dxdLayoutWidth = kForceLayout; // REVIEW (TomB): Not really sure why we have to do this.
					return true;
				}
				SetupVc();

				m_fInLayout = true;
				try
				{
					using (new SuspendDrawing(this))
						m_rootb.Layout(m_graphicsManager.VwGraphics, dxdAvailWidth);
					MoveChildWindows();
				}
				catch (System.Runtime.InteropServices.ExternalException objException)
				{
					// In one case, a user in India got this failure whenever starting Flex. He was
					// using a Tibetan font, but after reformatting his drive and reinstalling FW
					// he had failed to enable complex script support in Windows.
					m_dxdLayoutWidth = kForceLayout; // No drawing until we get successful layout.
					throw new Exception("Views Layout failed", objException);
				}
				finally
				{
					m_fInLayout = false;
				}
			}

			// Update the scroll range anyway, because height may have changed.
			UpdateScrollRange();

			return true;
		}

		/// <summary>
		/// This is called in Layout to notify the VC of anything it needs to know.
		/// </summary>
		private void SetupVc()
		{
			if (m_mediator == null)
				return;
			int hvo, frag;
			IVwViewConstructor vc;
			IVwStylesheet ss;
			m_rootb.GetRootObject(out hvo, out vc, out frag, out ss);
			if (vc is VwBaseVc)
			{
				// This really only needs to be done once but I can't find another reliable way to do it.
				((VwBaseVc) vc).Mediator = m_mediator;
			}
		}

		/// <summary>
		/// This hook provides an opportunity for subclasses to move child windows.
		/// For example, InterlinDocChild moves its Sandbox window to correspond
		/// to the position of the selected word in the text. It may be important to
		/// do this before updating the scroll range...for example, Windows forms may
		/// not allow the scroll range to be less than enough to show the whole of
		/// the (old position of) the child window.
		/// </summary>
		protected virtual void MoveChildWindows()
		{
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// This function returns a selection that includes the entire LinkedFile link at the current
		/// insertion point. It doesn't actually make the selection active.
		/// If the return value is false, none of the paramters should be looked at.
		/// If pfFoundLinkStyle is true, ppvwsel will contain the entire LinkedFiles Link string and
		/// pbstrFile will contain the filename the LinkedFiles link is pointing to.
		/// If pfFoundLinkStyle is false, the selection will still be valid, but it couldn't find
		/// any LinkedFiles link at the current insertion point.
		/// If ppt is not NULL, it will look for an LinkedFiles Link at that point. Otherwise,
		/// the current insertion point will be used.
		/// </summary>
		/// <param name="fFoundLinkStyle"></param>
		/// <param name="vwselParam"></param>
		/// <param name="strbFile"></param>
		/// <param name="pt"></param>
		/// <returns></returns>
		/// -----------------------------------------------------------------------------------
		protected bool GetExternalLinkSel(out bool fFoundLinkStyle, out IVwSelection vwselParam,
			out string strbFile, Point pt)
		{
			Debug.WriteLine("WARNING: RootSite.GetExternalLinkSel() hasn't been tested yet!");
			fFoundLinkStyle = false;
			vwselParam = null;
			strbFile = null;

			if (m_rootb == null)
				return false;
			IVwSelection vwsel;
			if (!pt.IsEmpty)
			{
				Rectangle rcSrcRoot;
				Rectangle rcDstRoot;
				using (new HoldGraphics(this))
				{
					GetCoordRects(out rcSrcRoot, out rcDstRoot);
				}
				vwsel = m_rootb.MakeSelAt(pt.X, pt.Y, rcSrcRoot, rcDstRoot, false);
			}
			else
			{
				vwsel = m_rootb.Selection;
			}
			if (vwsel == null)
				return false;

			ITsString tss;
			int ich;
			bool fAssocPrev;
			//HVO hvoObj;
			int hvoObj;
			//PropTag tag;
			int propTag;
			int ws;
			int irun;
			int crun;
			int irunMin;
			int irunLim;
			TsRunInfo tri;
			ITsTextProps ttp;
			string sbstr;
			string sbstrMain = "";
			vwsel.TextSelInfo(false, out tss, out ich, out fAssocPrev, out hvoObj, out propTag, out ws);
			if (tss == null)
				return false; // No string to check.
			// The following test is covering a bug in TextSelInfo until JohnT fixes it. If you right+click
			// to the right of a tags field (with one tag), TextSelInfo returns a qtss with a null
			// string and returns ich = length of the entire string. As a result get_RunAt fails
			// below because it is asking for a run at a non-existent location.
			int cch = tss.Length;
			if (ich >= cch)
				return false; // No string to check.
			crun = tss.RunCount;
			irun = tss.get_RunAt(ich);
			for (irunMin = irun; irunMin >= 0; irunMin--)
			{
				ttp = tss.FetchRunInfo(irunMin, out tri);
				sbstr = ttp.GetStrPropValue((int)FwTextPropType.ktptObjData);
				if (sbstr.Length == 0 || sbstr[0] != (byte)FwObjDataTypes.kodtExternalPathName)
					break;
				if (!fFoundLinkStyle)
				{
					fFoundLinkStyle = true;
					sbstrMain = sbstr;
				}
				else if (!sbstr.Equals(sbstrMain))
				{
					// This LinkedFiles Link is different from the other one, so
					// we've found the beginning.
					break;
				}
			}
			irunMin++;
			// If fFoundLinkStyle is true, irunMin now points to the first run
			// that has the LinkedFiles Link style.
			// If fFoundLinkStyle is false, there's no point in looking at
			// following runs.
			if (!fFoundLinkStyle)
				return true;

			for (irunLim = irun + 1; irunLim < crun; irunLim++)
			{
				ttp = tss.FetchRunInfo(irunLim, out tri);
				sbstr = ttp.GetStrPropValue((int)FwTextPropType.ktptObjData);
				if (sbstr.Length == 0 || sbstr[0] != (byte)FwObjDataTypes.kodtExternalPathName)
					break;
				if (!sbstr.Equals(sbstrMain))
				{
					// This LinkedFiles Link is different from the other one, so
					// we've found the ending.
					break;
				}
			}

			// We can now calculate the character range of this TsString that has
			// the LinkedFiles Link style applied to it.
			int ichMin;
			int ichLim;
			ichMin = tss.get_MinOfRun(irunMin);
			ichLim = tss.get_LimOfRun(irunLim - 1);

			MessageBox.Show("This code needs work! I don't think that it works like it should!");

			int cvsli = vwsel.CLevels(false);
			cvsli--; // CLevels includes the string property itself, but AllTextSelInfo doesn't need it.

			int ihvoRoot;
			int tagTextProp;
			int cpropPrevious;
			int ichAnchor;
			int ichEnd;
			int ihvoEnd;
			SelLevInfo[] rgvsli = SelLevInfo.AllTextSelInfo(vwsel, cvsli,
				out ihvoRoot, out tagTextProp, out cpropPrevious, out ichAnchor, out ichEnd,
				out ws, out fAssocPrev, out ihvoEnd, out ttp);

			// This does not actually make the selection active.
			m_rootb.MakeTextSelection(ihvoRoot, cvsli, rgvsli, tagTextProp,
				cpropPrevious, ichMin, ichLim, ws, fAssocPrev, ihvoEnd, ttp, false);

			strbFile = sbstrMain.Substring(1);
			return true;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="vss"></param>
		/// -----------------------------------------------------------------------------------
		protected virtual void Activate(VwSelectionState vss)
		{
			if (m_rootb != null && AllowDisplaySelection)
				m_rootb.Activate(vss);
		}

		/// <summary>
		/// allows Activate(VwSelectionState) to activate the selection.
		/// By default, we don't do this when ReadOnlyView is set to true.
		/// So, ReadOnlyView subclasses should override when they want Selections to be displayed.
		/// </summary>
		protected virtual bool AllowDisplaySelection
		{
			get { return IsEditable; }
		}


		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Adjust a point to view coords from device coords. This is the translation from a
		/// point obtained from a windows message like WM_LBUTTONDOWN to a point that can be
		/// passed to the root box. Currently it does nothing, as any conversion is handled
		/// by the source and destination rectangles passed to the mouse routines. It is
		/// retained for possible future use.
		/// </summary>
		/// <param name="pt"></param>
		/// -----------------------------------------------------------------------------------
		protected Point PixelToView(Point pt)
		{
			return m_orientationManager.RotatePointPaintToDst(pt);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Returns the ShiftStatus that shows if Ctrl and/or Shift keys were pressed
		/// </summary>
		/// <returns>The shift status</returns>
		/// -----------------------------------------------------------------------------------
		protected VwShiftStatus GetShiftStatus()
		{
			return EditingHelper.GetShiftStatus(ModifierKeys);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// An error has occurred during drawing, and the component in which it occurred should
		/// have recorded a system error information object describing the problem
		/// </summary>
		/// <param name="e"></param>
		/// -----------------------------------------------------------------------------------
		public static void ReportDrawErrMsg(Exception e)
		{
			if (e == null)
				return;

			// Look through the list to see if we've already given this message before.
			for (int i = 0; i < s_vstrDrawErrMsgs.Count; i++)
			{
				if ((string)s_vstrDrawErrMsgs[i] == e.Message)
					return;
			}

			s_vstrDrawErrMsgs.Add(e.Message);

			throw new ContinuableErrorException("Drawing Error", e);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Add or remove the tag in the given overlay of the current string.
		/// </summary>
		/// <param name="fApplyTag">True to add the tag, false to remove it</param>
		/// <param name="pvo">Overlay</param>
		/// <param name="itag">Index of tag</param>
		/// -----------------------------------------------------------------------------------
		public void ModifyOverlay(bool fApplyTag, IVwOverlay pvo, int itag)
		{
			CheckDisposed();
			if (m_rootb == null)
				return;

			Debug.WriteLine("WARNING: RootSite.ModifyOverlay() isn't tested yet");
			int hvo;
			uint clrFore;
			uint clrBack;
			uint clrUnder;
			int unt;
			bool fHidden;
			string uid;
			using (ArrayPtr arrayPtr = MarshalEx.StringToNative((int)VwConst1.kcchGuidRepLength + 1, true))
			{
				pvo.GetDbTagInfo(itag, out hvo, out clrFore, out clrBack, out clrUnder, out unt,
					out fHidden, arrayPtr);
				uid = MarshalEx.NativeToString(arrayPtr,
					(int)VwConst1.kcchGuidRepLength, false);
			}

			IVwSelection vwsel;
			ITsTextProps[] vttp;
			IVwPropertyStore[] vvps;
			if (EditingHelper.GetCharacterProps(out vwsel, out vttp, out vvps))
			{
				int cttp = vttp.Length;
				for (int ittp = 0; ittp < cttp; ittp++)
				{
					string strGuid = vttp[ittp].GetStrPropValue(
						(int)FwTextPropType.ktptTags);

					// REVIEW (EberhardB): I'm not sure if this works
					int cGuids = strGuid.Length / Marshal.SizeOf(typeof(Guid));
					List<string> guids = new List<string>();
					for (int i = 0; i < cGuids; i++)
						guids.Add(strGuid.Substring(i, Marshal.SizeOf(typeof(Guid))));

					if (fApplyTag)
					{
						// Add the tag if it does not exist
						if (guids.BinarySearch(uid) >= 0)
						{
							// The tag has already been applied to the textprop, so it doesn't
							// need to be modified.
							vttp[ittp] = null;
							continue;
						}
						else
						{
							// We need to add the tag to the textprop.
							guids.Add(uid);
							guids.Sort();
						}
					}
					else
					{
						// Remove the tag from the textprop.
						guids.Remove(uid);
					}

					ITsPropsBldr tpb = vttp[ittp].GetBldr();
					tpb.SetStrPropValue((int)FwTextPropType.ktptTags,
						guids.ToString());

					vttp[ittp] = tpb.GetTextProps();
				}
				vwsel.SetSelectionProps(cttp, vttp);

				/*
				 * ENHANCE (EberhardB): Implement this if we need it. It probably should be
				 * implemented in a derived class (view class for DataNotebook?)
				// Update the RnGenericRec_PhraseTags table as necessary.
				// (Yes, this is special case code!)

				AfDbInfo * pdbi = NULL;
				AfMainWnd * pamw = m_pwndSubclass->MainWindow();
				if (pamw)
				{
					AfMdiMainWnd * pammw = dynamic_cast<AfMdiMainWnd *>(pamw);
					AfLpInfo * plpi = NULL;
					if (pammw)
					{
						plpi = pammw->GetLpInfo();
						if (plpi)
							pdbi = plpi->GetDbInfo();
					}
				}
				if (pdbi)
				{
					int clevEnd;
					int clevAnchor;
					HVO hvoEnd;
					HVO hvoAnchor;
					PropTag tagEnd;
					PropTag tagAnchor;
					int ihvo;
					int cpropPrev;
					IVwPropertyStorePtr qvps;
					CheckHr(qvwsel->CLevels(true, &clevEnd));
					Assert(clevEnd >= 1);
					CheckHr(qvwsel->CLevels(false, &clevAnchor));
					Assert(clevAnchor >= 1);
					CheckHr(qvwsel->PropInfo(true, clevEnd - 1, &hvoEnd, &tagEnd, &ihvo,
						&cpropPrev, &qvps));
					CheckHr(qvwsel->PropInfo(false, clevAnchor - 1, &hvoAnchor, &tagAnchor, &ihvo,
						&cpropPrev, &qvps));

					IOleDbEncapPtr qode;
					pdbi->GetDbAccess(&qode);
				DbStringCrawler::UpdatePhraseTagsTable(kflidRnGenericRec_PhraseTags, fApplyTag, qode,
									 hvo, hvoEnd, hvoAnchor);
				}
				*/
			}
			if (FindForm() == Form.ActiveForm)
				Focus();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Checks if selection is visible. For a range selection we check the end of the
		/// selection, for an IP the entire selection must be visible.
		/// </summary>
		/// <remarks>
		/// This version doesn't test the secondary part of the selection if the combined
		/// primary and secondary selection is higher or wider then the ClientRectangle.
		/// </remarks>
		/// <param name="sel">The selection</param>
		/// <returns>Returns true if selection is visible</returns>
		/// -----------------------------------------------------------------------------------
		public bool IsSelectionVisible(IVwSelection sel)
		{
			CheckDisposed();
			return IsSelectionVisible(sel, false);
		}

		/// <summary>
		/// Checks if selection is visible. For a range selection (that is not a picture) we check the end of the
		/// selection, otherwise the entire selection must be visible.
		/// </summary>
		public bool IsSelectionVisible(IVwSelection sel, bool fWantOneLineSpace)
		{
			CheckDisposed();
			if (m_rootb == null)
				return false; // For paranoia.
			IVwSelection vwsel = (sel == null ? SelectionToMakeVisible : sel);
			if (vwsel == null)
				return false; // Nothing we can test.
			return IsSelectionVisible(vwsel, fWantOneLineSpace, DefaultWantBothEnds(vwsel));
		}

		private bool DefaultWantBothEnds(IVwSelection vwsel)
		{
			if (vwsel.SelType == VwSelType.kstPicture)
				return true; // We'd like to see all of a picture.
			return !vwsel.IsRange; // by default we want an IP but not a range wholly visible
		}
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Checks if selection is visible, according to the parameters.
		/// </summary>
		/// <remarks>
		/// <para>This version doesn't test the secondary part of the selection if the combined
		/// primary and secondary selection is higher or wider then the ClientRectangle.</para>
		/// <para>This method tests that the selection rectangle is inside of the
		/// client rectangle, but it doesn't test if the selection is actually enabled!</para>
		/// </remarks>
		/// <param name="vwsel">The selection</param>
		/// <param name="fWantOneLineSpace">True if we want at least 1 extra line above or
		/// below the selection to be considered visible, false otherwise</param>
		/// <param name="fWantBothEnds">true if both ends must be visible; otherwise, it's
		/// good enough if the end is.</param>
		/// <returns>Returns true if selection is visible</returns>
		/// -----------------------------------------------------------------------------------
		public bool IsSelectionVisible(IVwSelection vwsel, bool fWantOneLineSpace, bool fWantBothEnds)
		{
			Rectangle rcPrimary;
			bool fEndBeforeAnchor;
			int ydTop;

			// make sure we have a m_graphicsManager.VwGraphics
			using (new HoldGraphics(this))
			{
				SelectionRectangle(vwsel, out rcPrimary, out fEndBeforeAnchor);
			}
			if (m_orientationManager.IsVertical)
			{
				// In all current vertical views we have no vertical scrolling, so only need
				// to consider horizontal.
				// In this case, rcPrimary's top is the distance from the right of the ClientRect to the
				// right of the selection, and the height of rcPrimary is a distance further left.
				int right = rcPrimary.Top; // distance to left of right of window
				int left = right + rcPrimary.Height;
				if (fWantOneLineSpace)
				{
					right -= LineHeight;
					left += LineHeight;
				}
				return right >= 0 && left <= ClientRectangle.Width;
			}

			ydTop = -ScrollPosition.Y; // Where the window thinks it is now.
			// Adjust for that and also the height of the (optional) header.
			rcPrimary.Offset(0, ydTop - m_dyHeader); // Was in drawing coords, adjusted by top.

			// OK, we want rcIdealPrimary to be visible.
			int ydBottom = ydTop + ClientHeight - m_dyHeader;

			if (fWantOneLineSpace)
			{
				ydTop += LineHeight;
				ydBottom -= LineHeight;
			}

			bool isVisible = false;

			// Does the selection rectangle overlap the screen one?
			// Note that for insertion points and pictures we want the selection to be
			// entirely visible.
			// Note that if we support horizontal scrolling we will need to enhance this.
			if (fWantBothEnds)
			{
				isVisible = rcPrimary.Top >= ydTop && rcPrimary.Bottom <= ydBottom;
			}
			else
			{
				if (fEndBeforeAnchor)
					isVisible = (rcPrimary.Top > ydTop && rcPrimary.Top < ydBottom - LineHeight);
				else
					isVisible = (rcPrimary.Bottom > ydTop + LineHeight && rcPrimary.Bottom < ydBottom);
			}

			// If not visible vertically, we don't care about horizontally.
			if (!isVisible)
				return false;
			// If not scrolling horizontally, vertically is good enough.
			if (!DoAutoHScroll)
				return isVisible;
			int ydLeft = -ScrollPosition.X + HorizMargin;
			int ydRight = ydLeft + ClientRectangle.Width - (HorizMargin * 2);
			return rcPrimary.Left >= ydLeft && rcPrimary.Right <= ydRight;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Make sure the graphics object has a DC. If it already has, increment a count,
		/// so we know when to really free the DC.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		internal void InitGraphics()
		{
			// EberhardB: we used to check for HandleCreated, but if we do this it is
			// impossible to run tests without showing a window. Not checking the creation
			// of the handle seems to work in both test and production.
			// DavidO: We check for IsDisposed because InitGraphics is called when
			// SimpleRootSite receives an OnKeyDown event. However, some OnKeyDown events in
			// the delegate chain may close the window on which a SimpleRootSite has been
			// placed. In such cases, InitGraphics was being called after the SimpleRootSite's
			// handle was disposed of, thus causing a crash when CreateGraphics() was called.
			// I (RandyR) added a call to DestroyHandle() very early on in the Dispose method,
			// so that should take care of re-entrant events.
			if (DesignMode && !AllowPaintingInDesigner) // || IsDisposed) //  || !IsHandleCreated
				return;

			lock (m_graphicsManager)
			{
				m_graphicsManager.Init(Zoom);
				Dpi = new Point(m_graphicsManager.VwGraphics.XUnitsPerInch,
					m_graphicsManager.VwGraphics.YUnitsPerInch);
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Uninitialize the graphics object by releasing the DC.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		internal void UninitGraphics()
		{
			lock (m_graphicsManager)
			{
				m_graphicsManager.Uninit();
			}
		}
		#endregion // Other non-virtual methods

		#region static methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Performs the same coordinate transformation as C++ UtilRect rcSrc.MapXTo(x, rcDst).
		/// </summary>
		/// <param name="x"></param>
		/// <param name="rcSrc"></param>
		/// <param name="rcDst"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static int MapXTo(int x, Rectangle rcSrc, Rectangle rcDst)
		{
			int dxs = rcSrc.Width;
			Debug.Assert(dxs > 0);

			int dxd = rcDst.Width;

			if (dxs == dxd)
				return x + rcDst.Left - rcSrc.Left;

			return rcDst.Left + (x - rcSrc.Left) * dxd / dxs;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Performs the same coordinate transformation as C++ UtilRect rcSrc.MapYTo(y, rcDst).
		/// </summary>
		/// <param name="y"></param>
		/// <param name="rcSrc"></param>
		/// <param name="rcDst"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static int MapYTo(int y, Rectangle rcSrc, Rectangle rcDst)
		{
			int dys = rcSrc.Height;
			Debug.Assert(dys > 0);

			int dyd = rcDst.Height;

			if (dys == dyd)
				return y + rcDst.Top - rcSrc.Top;

			return rcDst.Top + (y - rcSrc.Top) * dyd / dys;
		}
		#endregion

		#region implementation of IReceiveSequentialMessages

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public MessageSequencer Sequencer
		{
			get
			{
				CheckDisposed();
				return m_messageSequencer;
			}
		}

		//		LRESULT LresultFromObject(
		//			REFIID riid,
		//			WPARAM wParam,
		//			LPUNKNOWN pAcc
		//			);
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Entry point we are required to call in response to WM_GETOBJECT
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[DllImport("oleacc.DLL", EntryPoint = "LresultFromObject", SetLastError = true,
			 CharSet = CharSet.Unicode)]
		public static extern IntPtr LresultFromObject(ref Guid riid, IntPtr wParam,
			[MarshalAs(UnmanagedType.Interface)] object pAcc);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes Windows messages.
		/// </summary>
		/// <param name="msg">The Windows Message to process.</param>
		/// ------------------------------------------------------------------------------------
		public virtual void OriginalWndProc(ref Message msg)
		{
			CheckDisposed();
			switch (msg.Msg)
			{
#if __MonoCS__
			case (int)Win32.WinMsgs.WM_KEYDOWN:
				if (m_inputBusController != null && m_inputBusController.NotifyKeyDown(msg, ModifierKeys))
					return;
				break;
			case (int)Win32.WinMsgs.WM_CHAR:
				if (m_inputBusController != null && m_inputBusController.NotifyKeyPress((uint)msg.WParam, (uint)msg.LParam, ModifierKeys))
					return;
				break;
			case (int)Win32.WinMsgs.WM_DESTROY:
					if (m_inputBusController != null)
					{
				m_inputBusController.Dispose();
						m_inputBusController = null;
					}
				break;
#endif

#if !__MonoCS__ // Disable use of UIAutomationProvider.dll on Linux
				case 61: // WM_GETOBJECT
					{
						// FWR-2874, FWR-3045: This code was commented out to prevent a crash
						// that can happen when using the Windows 7 On-Screen Keyboard.
						//if (msg.LParam.ToInt32() == AutomationInteropProvider.RootObjectId)
						//{
						//    msg.Result = AutomationInteropProvider.ReturnRawElementProvider(
						//            this.Handle, msg.WParam, msg.LParam,
						//            UIAutomationServerProviderFactory());
						//    return;
						//}

						object obj = AccessibleRootObject;
						//IAccessible acc = (IAccessible)obj;
						if (obj == null)
						{
							// If for some reason the root site isn't sufficiently initialized
							// to have a root box, the best we can do is the default IAccessible.
							//MessageBox.Show("Null root in IAccessible");
							base.WndProc(ref msg);
						}
						else
						{
							Guid guidAcc = Marshal.GenerateGuidForType(typeof(IAccessible));
							msg.Result = LresultFromObject(ref guidAcc, msg.WParam, obj);
						}
						return;
					}
#endif
				case 0x286:	// WM_IME_CHAR
					{
						// We must handle this directly so that duplicate WM_CHAR messages don't get
						// posted, resulting in duplicated input.  (I suspect this may be a bug in the
						// .NET framework. - SMc)
						OnKeyPress(new KeyPressEventArgs((char)msg.WParam));
						return;
					}
				case (int)Win32.WinMsgs.WM_SETFOCUS:
					OnSetFocus(msg);
#if __MonoCS__
					// In Linux+Mono, if you .Focus() a SimpleRootSite, checking .Focused reports false unless
					// we comment out this case for intercepting WM_SETFOCUS, or call base.WndProc() to
					// presumably let Mono handle WM_SETFOCUS as well by successfully setting focus on the
					// base Control.
					// Affects six unit tests in FwCoreDlgsTests FwFindReplaceDlgTests: eg ApplyStyle_ToEmptyTextBox.
					//
					// Intercepting WM_SETFOCUS in Windows relates to focus switching with respect to Keyman.

					base.WndProc(ref msg);
#endif // __MonoCS__
					return;
				case (int)Win32.WinMsgs.WM_KILLFOCUS:
					base.WndProc(ref msg);
					OnKillFocus(Control.FromHandle(msg.WParam),
						MiscUtils.IsChildWindowOfForm(ParentForm, msg.WParam));
					return;
				default:
					{
						if (msg.Msg == s_wm_kmselectlang)
						{
							Debug.Assert(s_wm_kmselectlang != 0);
							if (msg.WParam == (IntPtr)4)
							{
								OnKeymanKeyboardChange(msg.WParam, msg.LParam);
							}
							else if (msg.WParam == (IntPtr)1)
							{
								// We get these both as a result of our own changes, and changes
								// resulting from control keys. If we just initiated a change ourselves,
								// ignore it.
								if (EditingHelper.SelectLangPending > 0)
									EditingHelper.SelectLangPending--;
								else
									OnKeymanKeyboardChange(msg.WParam, msg.LParam);
							}
						}
						else if (msg.Msg == s_wm_kmkbchange)
							Debug.Assert(s_wm_kmkbchange != 0);
						break;
					}
			}
			base.WndProc(ref msg);
		}

		#region UIAutomationServerProvider

		/// <summary>
		/// Gets or sets the factory for our UI automation server provider.
		/// </summary>
		/// <value>The UI automation server provider factory.</value>
		protected Func<IRawElementProviderFragmentRoot> UIAutomationServerProviderFactory
		{ get; set; }

		#endregion

		/// <summary>
		/// Required by interface, but not used, because we don't user the MessageSequencer
		/// to sequence OnPaint calls.
		/// </summary>
		/// <param name="e"></param>
		public void OriginalOnPaint(PaintEventArgs e)
		{
			CheckDisposed();
			Debug.Assert(false);
		}

		#endregion

		#region Sequential message processing enforcement

		private XCore.IxWindow ContainingXWindow()
		{
			for (Control parent = this.Parent; parent != null; parent = parent.Parent)
			{
				if (parent is IxWindow)
					return parent as IxWindow;
			}
			return null;
		}

		/// <summary>
		/// Begin a block of code which, even though it is not itself a message handler,
		/// should not be interrupted by other messages that need to be sequential.
		/// This may be called from within a message handler.
		/// EndSequentialBlock must be called without fail (use try...finally) at the end
		/// of the block that needs protection.
		/// </summary>
		/// <returns></returns>
		public void BeginSequentialBlock()
		{
			CheckDisposed();
			IxWindow mainWindow = ContainingXWindow();
			if (mainWindow != null)
				mainWindow.SuspendIdleProcessing();
			Sequencer.BeginSequentialBlock();
		}

		/// <summary>
		/// See BeginSequentialBlock.
		/// </summary>
		public void EndSequentialBlock()
		{
			CheckDisposed();
			Sequencer.EndSequentialBlock();
			IxWindow mainWindow = ContainingXWindow();
			if (mainWindow != null)
				mainWindow.ResumeIdleProcessing();
		}
		#endregion

		#region IMessageFilter Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is a major kludge to prevent a spurious WM_KEYUP for VK_CONTROL from
		/// interrupting a mouse click.
		/// If mouse button was pressed elsewhere causing this root site to loose focus,
		/// this message filter is installed to throw away the spurious WM_KEYUP for VK_CONTROL
		/// that happens as a result of switching to the default keyboard layout.
		/// See OnKillFocus for the corresponding code that installs this message filter.
		/// </summary>
		/// <param name="m">The message to be dispatched. You cannot modify this message.</param>
		/// <returns>
		/// true to filter the message and stop it from being dispatched; false to allow the
		/// message to continue to the next filter or control.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public bool PreFilterMessage(ref Message m)
		{
			CheckDisposed();  // OK now that the filter is being removed in the Dispose method

			switch (m.Msg)
			{
				case (int)Win32.WinMsgs.WM_KEYUP:
				case (int)Win32.WinMsgs.WM_LBUTTONUP:
				case (int)Win32.WinMsgs.WM_KEYDOWN:
					// If user-initiated messages come (or our spurious one, which we check
					// for below), remove this filter.
					Application.RemoveMessageFilter(this);
					m_messageFilterInstalled = false;

					// Now check for the spurious CTRL-UP message
					if (m.Msg == (int)Win32.WinMsgs.WM_KEYUP &&
						m.WParam.ToInt32() == (int)Win32.VirtualKeycodes.VK_CONTROL)
					{
						return true; // discard this message
					}
					break;
			}
			return false; // Let the message be dispatched
		}
		#endregion
	}
	#endregion
}
