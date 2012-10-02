// --------------------------------------------------------------------------------------------
#region /// Copyright (c) 2002, SIL International. All Rights Reserved.
// <copyright from='2002' to='2002' company='SIL International'>
//		Copyright (c) 2002, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: RootSite.cs
// Responsibility: TE Team
// Last reviewed:
//
// <remarks>
// Implementation of RootSite (formerly AfVwRootSite and AfVwScrollWndBase).
// This class does most of the interesting work relating to hosting a FieldWorks View
// represented as an IVwRootBox. It provides an implementation of IVwRootSite,
// passes interesting events to the root box, and handles various common menu
// commands and toolbar functions.
//
// The original RootSite class contained most of the code of this file, but this was later
// refactored to enable a distinction between a SimpleRootSite that does not know what cache
// is being used for the view, and RootSite which has an FdoCache member variable.
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using XCore;

// How to debug COM reference counts:
// a) create a global variable that contains a file handle:
// 		HANDLE hLogFile = CreateFile(_T("c:\\log.txt"), GENERIC_WRITE,
//			FILE_SHARE_WRITE | FILE_SHARE_READ, NULL, CREATE_ALWAYS,
//			FILE_ATTRIBUTE_NORMAL | FILE_FLAG_WRITE_THROUGH, NULL);
// b) in your c'tor, call:
//		_CrtSetReportMode(_CRT_WARN, _CRTDBG_MODE_FILE);
//		_CrtSetReportFile(_CRT_WARN, hLogFile);
// c) any output to _CrtDebugReport goes now to c:\log.txt, e.g.
//		_CrtDbgReport(_CRT_WARN, NULL, 0, NULL, "VwGraphics::VwGraphics\n");
// d) use Cygwin's "tail.exe" to display the debug messages while the app is running:
//		tail -f log.txt

namespace SIL.FieldWorks.Common.RootSites
{
	#region Class ErrorChangeWatcher

	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// This class watches for errors that should be beeped at and eventually marked with
	/// a red squiggly.
	/// This may eventually go away in lieu of something more better.
	/// </summary>
	/// ------------------------------------------------------------------------------------
	public class ErrorChangeWatcher : ChangeWatcher
	{
		/*
		/// <summary>
		/// The Scripture object that handles parsing when a paragraph changes
		/// </summary>
		//protected FdoCache m_cache; // Defined
		*/

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is the publicly accessible way to create a ErrorChangeWatcher. Upon
		/// construction, the change watcher automatically registers itself and assumes a life
		/// of its own, so nothing is returned to the caller.
		/// </summary>
		/// <param name="cache">FdoCache from which we get the DB access info</param>
		/// ------------------------------------------------------------------------------------
		public static void Create(FdoCache cache)
		{
			Debug.Assert(cache != null);
			if (cache.ChangeWatchers != null)
			{
				foreach (ChangeWatcher cw in cache.ChangeWatchers)
				{
					if (cw is ErrorChangeWatcher)
						return; // Only one of these allowed per DB connection
				}
			}
			// No need to assign this to a member variable because the change watcher registers
			// itself (and thereby takes on a life of its own).
			new ErrorChangeWatcher(cache);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="cache">FdoCache from which we get the DB access info</param>
		/// ------------------------------------------------------------------------------------
		protected ErrorChangeWatcher(FdoCache cache) :
			base(cache, StTxtPara.ktagErrorInPara)
		{
			//m_cache = cache;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Calls ParseStTxtPara to carry out the desired side effects: re-parsing the paragraph
		/// for wordforms, ???.
		/// </summary>
		/// <param name="hvoPara">The Paragraph that was changed</param>
		/// <param name="ivMin">the starting character index where the change occurred</param>
		/// <param name="cvIns">the number of characters inserted</param>
		/// <param name="cvDel">the number of characters deleted</param>
		/// ------------------------------------------------------------------------------------
		protected override void DoEffectsOfPropChange(int hvoPara, int ivMin, int cvIns, int cvDel)
		{
			// TODO: Remove this code when error annotations and red squigglies are
			// implemented properly. This is temporary code to beep once when
			// a new error is identified.
			if (m_cache.GetIntProperty(hvoPara, StTxtPara.ktagErrorInPara) > 0)
			{
				// When Beep code is removed, also remove reference
				MiscUtils.ErrorBeep();

				// This line causes Bad Things(tm) and probably isn't really desirable anyhow.
				// Was originally intended to prevent nonstop beeping, but this doesn't seem
				// to occur anyways.
				//m_cache.SetIntProperty(hvoPara,	StTxtPara.ktagErrorInPara, 0);
			}
		}
	}
	#endregion

	#region Class RootSite
	/// -----------------------------------------------------------------------------------------
	/// <summary>
	/// RootSite is the most commonly used implementation of IVwRootSite for applications that
	/// don't want to use the FieldWorks Framework classes (if using them is OK, FwRootSite
	/// adds considerable functionality, such as stylesheets and find/replace). It requires
	/// initialization with an fdoCache.
	/// </summary>
	/// -----------------------------------------------------------------------------------------
	public class RootSite : SimpleRootSite, IHeightEstimator, IVwObjDelNotification,
		IRootSiteSlave
	{
		#region Member variables
		/// <summary>The height in points of an average paragraph when the rootsite is
		/// <c>kdxBaselineRootsiteWidth</c> wide</summary>
		public const int kdypBaselineParagraphHeight = 12;
		/// <summary>The baseline width in pixels of a rootsite, used for storing height
		/// estimates that are independent of the actual width of the rootsite.
		/// </summary>
		public const int kdxBaselineRootsiteWidth = 1500;

		/// <summary>The FDO cache</summary>
		protected FdoCache m_fdoCache;
		/// <summary>
		/// the group root site that controls this one.
		/// May also be null, in which case it behaves like an ordinary root site.
		/// </summary>
		protected IRootSiteGroup m_group;
		/// <summary>Set to true while we are in the SelectionChanged method. We don't want
		/// to process any other selection changes in any other view while we're not done
		/// with the first one.</summary>
		protected static bool s_fInSelectionChanged;
		/// <summary>The real average paragraph height in points, based on the available width
		/// of the client rectangle. To speed things up we store it here, but it needs to be
		/// updated if the zoom, width or horizontal margin changes.</summary>
		protected int m_ParaHeightInPoints;
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize one. It doesn't have a scroll bar because the containing Group is
		/// meant to handle scrolling.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public RootSite(FdoCache cache) : base()
		{
			base.AutoScroll = true;
			Cache = cache; // make sure to set the property, not setting m_fdoCache directly
		}

		/// <summary>
		/// RootSite shouldn't handle tabs like a control
		/// </summary>
		public override bool HandleTabAsControl
		{
			get
			{
				CheckDisposed();

				return false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="disposing"></param>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (m_rootb != null)
			{
				CloseRootBox();
				m_rootb = null;
			}

			base.Dispose(disposing);

			if (disposing)
			{
				// Not good here, since it causes infinite loop.
				//if (m_group != null)
				//	m_group.Dispose();
			}
			m_fdoCache = null;
			m_group = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This MUST be called by the MakeRoot method or something similar AFTER the
		/// root box is created but BEFORE the view is laid out. Even after it is called,
		/// MakeRoot must not do anything that would cause layout; that should not happen
		/// until all roots are synchronized.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void Synchronize(IVwRootBox rootb)
		{
			if (m_group != null)
				m_group.Synchronize(rootb);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Calculates the average paragraph height in points, based on the available width.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void CalculateAvgParaHeightInPoints()
		{
			int width = GetAvailWidth(null);
			//Debug.Assert(width != 0);
			if (width <= 0)
				width = 1;
			m_ParaHeightInPoints = (RootSite.kdxBaselineRootsiteWidth * AverageParaHeight)
				/ width;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If the text for pasting is too long, truncate it and warn the user.
		/// This is shared by subclasses of RootSite for implementing handlers for
		/// RootSiteEditingHelper.PasteFixTssEvent.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void TruncatePasteIfNecessary(FwPasteFixTssEventArgs e, int flid)
		{
			int type = Cache.MetaDataCacheAccessor.GetFieldType((uint)flid);
			if (type == (int)CellarModuleDefns.kcptMultiString ||
				type == (int)CellarModuleDefns.kcptMultiUnicode ||
				type == (int)CellarModuleDefns.kcptString ||
				type == (int)CellarModuleDefns.kcptUnicode)
			{
				ITsString tssCurrent = EditingHelper.CurrentSelection.GetTss(SelectionHelper.SelLimitType.End);
				int cchSel = 0;
				if (EditingHelper.CurrentSelection.IsRange)
					cchSel = Math.Abs(EditingHelper.CurrentSelection.IchAnchor - EditingHelper.CurrentSelection.IchEnd);
				int cchMax = Cache.MaxFieldLength(flid);
				int cchIncoming = e.TsString.Length;
				if (tssCurrent.Length + cchIncoming - cchSel > cchMax)
				{
					int cchPaste = cchMax - (tssCurrent.Length - cchSel);
					if (cchPaste < 0)
						cchPaste = 0;
					System.Windows.Forms.MessageBox.Show(
						String.Format(SIL.FieldWorks.Resources.ResourceHelper.GetResourceString("kstidTruncatingPaste"), cchIncoming, cchPaste, cchMax),
						SIL.FieldWorks.Resources.ResourceHelper.GetResourceString("kstidWarning"),
						System.Windows.Forms.MessageBoxButtons.OK,
						System.Windows.Forms.MessageBoxIcon.Warning);
					ITsStrBldr bldr = e.TsString.GetBldr();
					bldr.ReplaceTsString(cchPaste, cchIncoming, null);
					e.TsString = bldr.GetString();
				}
			}
		}

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the editing helper for RootSite.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public RootSiteEditingHelper RootSiteEditingHelper
		{
			get
			{
				CheckDisposed();
				return EditingHelper as RootSiteEditingHelper;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Helper used for processing editing requests.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override EditingHelper EditingHelper
		{
			get
			{
				CheckDisposed();

				if (m_editingHelper == null)
					m_editingHelper = new RootSiteEditingHelper(m_fdoCache, this);
				return m_editingHelper;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the average paragraph height in points.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual int AverageParaHeight
		{
			get
			{
				CheckDisposed();
				return kdypBaselineParagraphHeight;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Override the getter to obtain a WSF from the FdoCache, if we don't have
		/// one set independently, as is usually the case for this class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override ILgWritingSystemFactory WritingSystemFactory
		{
			get
			{
				CheckDisposed();

				if (m_wsf == null && m_fdoCache != null)
					return m_fdoCache.LanguageWritingSystemFactoryAccessor;
				return m_wsf;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This tests whether the class has a cache (in this RootSite subclass) or
		/// (in the SimpleRootSite base class) whether it has a ws. This is often used to determine whether
		/// we are sufficiently initialized to go ahead with some operation that may get called
		/// prematurely by something in the .NET framework.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override bool GotCacheOrWs
		{
			get
			{
				CheckDisposed();
				return m_fdoCache != null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a string representation of the object suitable to put on the clipboard.
		/// </summary>
		/// <param name="_guid">The guid of the object in the DB</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override string get_TextRepOfObj(ref Guid _guid)
		{
			CheckDisposed();

			return EditingHelper.TextRepOfObj(m_fdoCache, _guid);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Show the writing system choices?
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected override bool IsSelectionFormattable
		{
			get
			{
				if (DesignMode || m_rootb == null)
					return false;

				if (EditingHelper == null ||
					EditingHelper.CurrentSelection == null ||
					EditingHelper.Editable == false)
				{
					return false;
				}

				//todo: in some complex selection, we will want to just "say no". For now, we just
				//look at the start (anchor) of the selection.
				int flid = EditingHelper.CurrentSelection.GetTextPropId(
					SelectionHelper.SelLimitType.Anchor);
				if (flid == 0) // can happen for e.g. icons
					return false;

				FieldType type = m_fdoCache.GetFieldType(flid);
				return !(type == FieldType.kcptUnicode
					|| type == FieldType.kcptBigUnicode
					|| type == FieldType.kcptMultiUnicode
					|| type == FieldType.kcptMultiBigUnicode);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the status of all the slaves in the group whether they are ready to layout.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override bool OkayToLayOut
		{
			get
			{
				if (m_group != null && m_group.Slaves.Count > 0)
				{
					foreach (RootSite slave in m_group.Slaves)
						if (!slave.OkayToLayOutAtCurrentWidth)
							return false;
					return true;
				}
				return base.OkayToLayOut;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the FDO cache
		/// </summary>
		/// <value>A <see cref="FdoCache"/></value>
		/// -----------------------------------------------------------------------------------
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public virtual FdoCache Cache
		{
			get
			{
				CheckDisposed();
				return m_fdoCache;
			}
			set
			{
				CheckDisposed();

				m_fdoCache = value;
				if (m_fdoCache != null)
				{
					ErrorChangeWatcher.Create(m_fdoCache);
					if (m_editingHelper is RootSiteEditingHelper)
						RootSiteEditingHelper.Cache = m_fdoCache;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The group that organizes several roots scrolling together.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IRootSiteGroup Group
		{
			get
			{
				CheckDisposed();
				return m_group;
			}
			set
			{
				CheckDisposed();

				m_group = value;
				base.AutoScroll = (m_group != null) ? (m_group.ScrollingController == this) : false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the value of the AutoScroll property. When we're part of a root site
		/// group and we're not the scrolling controller, then setting this property is
		/// ignored.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override bool AutoScroll
		{
			get
			{
				CheckDisposed();
				return base.AutoScroll;
			}
			set
			{
				CheckDisposed();

				// should only be set if we are the scrolling controller
				if (m_group == null || m_group.ScrollingController == this)
					base.AutoScroll = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override float Zoom
		{
			get
			{
				CheckDisposed();
				return base.Zoom;
			}
			set
			{
				CheckDisposed();

				if (m_group == null || m_group.Slaves.Count == 0)
					base.Zoom = value;
				else
				{
					foreach (IRootSiteSlave slave in m_group.Slaves)
					{
						// we can't call slave.Zoom because that will call us again -
						// eventually we'll get a stack overflow...
						if (slave is RootSite)
							((RootSite)slave).m_Zoom = value;
					}
					// RefreshDisplay now happens through all sync'd views in the Views code.
					m_group.ScrollingController.RefreshDisplay();
				}

				CalculateAvgParaHeightInPoints();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the scrolling position for the control. When we're not the scrolling
		/// controller then we're part of a group then gets or sets the scrolling
		/// controller's value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override Point ScrollPosition
		{
			get
			{
				CheckDisposed();

				return (m_group == null || this == m_group.ScrollingController ?
					base.ScrollPosition : m_group.ScrollingController.ScrollPosition);
			}
			set
			{
				CheckDisposed();

				if (m_group == null || this == m_group.ScrollingController)
					base.ScrollPosition = value;
				else
				{
					m_group.ScrollingController.ScrollPosition = value;
					Invalidate();
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the horizontal margin
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override int HorizMargin
		{
			get { return base.HorizMargin; }
			set
			{
				base.HorizMargin = value;
				CalculateAvgParaHeightInPoints();
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the scrolling range for the control. When we're not the scrolling
		/// controller then we're part of a group then gets or sets the scrolling
		/// controller's value.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public override Size ScrollMinSize
		{
			get
			{
				CheckDisposed();

				return (m_group == null || this == m_group.ScrollingController ?
					base.ScrollMinSize : m_group.ScrollingController.ScrollMinSize);
			}
			set
			{
				CheckDisposed();

				if (m_group == null || this == m_group.ScrollingController)
					base.ScrollMinSize = value;
				else
					m_group.ScrollingController.ScrollMinSize = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// It sometimes helps to use the max of the sizes of all sites. I (JT) think that
		/// while adjusting the scroll range of one pane because of expanded lazy boxes,
		/// the size of the slaves is set to the appropriate root box. This can lead to
		/// an adjust scroll range for the other pane at a time when its range is much
		/// less, perhaps because it hasn't been synchronized yet.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override Size AdjustedScrollRange
		{
			get
			{
				if (m_group == null)
					return ScrollRange;
				Size result = ScrollRange;
				foreach (RootSite slave in m_group.Slaves)
				{
					if (slave != this && slave.RootBox != null)
						result.Height = Math.Max(result.Height, slave.ScrollRange.Height);
				}

				return result;
			}
		}
		#endregion

		#region Overridden Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overridden to fix TE-4146
		/// </summary>
		/// <param name="levent"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnLayout(LayoutEventArgs levent)
		{
			if (m_group == null)
				callBaseLayout(levent);
			else if (this == m_group.ScrollingController)
			{
				// If we changed width and we are the scrolling controller, then make sure
				// all of the other slaves re-layout. This causes lazy boxes to recalculate
				// their sizes. (fixes TE-4146)
				foreach (RootSite slave in m_group.Slaves)
				{
					if (slave.m_dxdLayoutWidth != slave.GetAvailWidth(m_rootb))
						slave.m_dxdLayoutWidth = kForceLayout;
					slave.callBaseLayout(levent);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Lets people call the base implementation of OnLayout()
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void callBaseLayout(LayoutEventArgs levent)
		{
			base.OnLayout(levent);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// We override this method to make a selection in all of the views that are in a
		/// snynced group. This fixes problems where the user changes the selection in one of
		/// the slaves, but the master is not updated. Thus the view is not scrolled as the
		/// groups scroll position only scrolls the master's selection into view. (TE-3380)
		/// </summary>
		/// <param name="prootb"></param>
		/// <param name="vwselNew"></param>
		/// ------------------------------------------------------------------------------------
		public override void SelectionChanged(IVwRootBox prootb, IVwSelection vwselNew)
		{
			CheckDisposed();

			Debug.Assert( vwselNew != null );

			if (s_fInSelectionChanged)
				return;

			// We need to check if this is our first time in the function because our
			//	call to helper.SetSelection below will end up calling this function..
			//	this results in a ugly infinite loop.  A more elegant solution would be
			//	nice...but not practical with the views architecture.
			if (m_group != null && !vwselNew.IsRange)
			{
				s_fInSelectionChanged = true;

				// We are after the y position of the top of the paragraph
				Rectangle rcSrcRoot;
				Rectangle rcDstRoot;
				GetCoordRects( out rcSrcRoot, out rcDstRoot);

				Rect paraRect;
				vwselNew.GetParaLocation( out paraRect );

				foreach(RootSite site in m_group.Slaves )
				{
					if (site != this)
					{
						// Recursive call to SelectionChanged happens here =(
						//	Add 2 pixels so we aren't on the end of the rectangle
						//		(improves accuracy)
						site.RootBox.MakeSelAt(0, paraRect.top + 2, rcSrcRoot, rcDstRoot, true);
					}
				}

				// Reset at this point.
				s_fInSelectionChanged = false;
			}

			base.SelectionChanged(prootb, vwselNew);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a new object, given a text representation (e.g., from the clipboard).
		/// </summary>
		/// <param name="bstrText">Text representation of object</param>
		/// <param name="_selDst">Provided for information in case it's needed to generate
		/// the new object (E.g., footnotes might need it to generate the proper sequence
		/// letter)</param>
		/// <param name="kodt">The object data type to use for embedding the new object
		/// </param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override Guid get_MakeObjFromText(string bstrText, IVwSelection _selDst,
			out int kodt)
		{
			CheckDisposed();

			return EditingHelper.MakeObjFromText(m_fdoCache, bstrText, _selDst, out kodt);
		}

		// Commented out this method as part of fix for TE-3537. We have to adjust the scroll
		// range for all views, otherwise scrolling to the end doesn't work reliably e.g. if
		// IP is in style pane.
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
		/// We want to call AdjustScrollRange1 for every slave not only for the
		/// ScrollingController, otherwise we have problems determining the correct scroll size
		/// of a synced view (TE-3537). But we don't want to adjust the scroll range/position for any
		/// other view then the ScrollingController, otherwise we sum the size changes which
		/// changes the position (TE-3574).
		/// </remarks>
		///------------------------------------------------------------------------------------
		protected override bool AdjustScrollRange1(int dxdSize, int dxdPosition, int dydSize,
			int dydPosition)
		{
			// This was taken out because it broke scrolling in the TE back translation split
			// view. If this breaks something else, then a better workaround needs to be found
			// for TE-3576.
			//if (m_group != null && m_group.ScrollingController != this)
			//    dydSize = 0;
			return base.AdjustScrollRange1(dxdSize, dxdPosition, dydSize, dydPosition);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When we get a mouse wheel event for windows other than the scrolling controller
		/// then pass on the message to the scrolling controller.
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnMouseWheel(MouseEventArgs e)
		{
			if (m_group != null && this != m_group.ScrollingController &&
				m_group.ScrollingController is RootSite)
			{
				((RootSite)m_group.ScrollingController).OnMouseWheel(e);
				return;
			}

			base.OnMouseWheel(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When the client size changed we have to recalculate the average paragraph height
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);
			if (Visible)
				CalculateAvgParaHeightInPoints();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This gets sent in a pathological case where expanding a lazy box forces a
		/// change in scroll position because of a reduction in the overall scroll
		/// bar range (usually while trying to expand the boxes needed to display the
		/// final screen full). If the pane is in a group, we need to invalidate
		/// everything in the group since all of their scroll positions have been
		/// changed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void InvalidateForLazyFix()
		{
			CheckDisposed();

			if (m_group != null)
				m_group.InvalidateForLazyFix();
			else
				base.InvalidateForLazyFix();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Scroll to the top
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public override void ScrollToTop()
		{
			CheckDisposed();

			if (m_group != null && this != m_group.ScrollingController)
				m_group.ScrollingController.ScrollToTop();
			else
				base.ScrollToTop ();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Scroll to the bottom.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public override void ScrollToEnd()
		{
			CheckDisposed();

			if (m_group != null && this != m_group.ScrollingController)
			{
				m_group.ScrollingController.ScrollToEnd();
				MakeSelectionVisible(null);
			}
			else
				base.ScrollToEnd ();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the writing system for the HVO. This could either be the vernacular or
		/// analysis writing system.
		/// </summary>
		/// <param name="hvo">HVO</param>
		/// <returns>Writing system</returns>
		/// ------------------------------------------------------------------------------------
		public override int GetWritingSystemForHvo(int hvo)
		{
			CheckDisposed();

			return m_fdoCache.DefaultVernWs;
		}
		#endregion

		#region IVwObjDelNotification implementation
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is called when the view code is about to delete a paragraph. We need to save
		/// the back translations by moving them to whatever paragraph the deleted one is
		/// merging with.
		/// </summary>
		/// <param name="sel">The selection</param>
		/// <param name="rootb">The rootbox</param>
		/// <param name="hvoObject">Paragraph to be deleted</param>
		/// <param name="hvoOwner">StText that owns the para</param>
		/// <param name="tag">flid in which para is owned</param>
		/// <param name="ihvo">index of paragraph in text</param>
		/// <param name="fMergeNext"><c>true</c> if this paragraph is merging with the
		/// following paragraph.</param>
		/// ------------------------------------------------------------------------------------
		public virtual void AboutToDelete(IVwSelection sel, IVwRootBox rootb, int hvoObject,
			int hvoOwner, int tag, int ihvo, bool fMergeNext)
		{
			CheckDisposed();

			SelectionHelper selHelper = SelectionHelper.Create(sel, this);
			RootSiteEditingHelper.AboutToDelete(selHelper, hvoObject, hvoOwner, tag, ihvo,
				fMergeNext);
		}
		#endregion

		#region IHeightEstimator implementation
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This routine is used to estimate the height of an item. The item will be one of
		/// those you have added to the environment using AddLazyItems.
		/// The first three arguments are as for Display, that is, you are being asked to
		/// estimate how much vertical space is needed to display this item in the available width.
		/// </summary>
		/// <param name="hvo">Item whose height is to be estimated</param>
		/// <param name="frag">Basically indicates what kind of thing the HVO represents (or
		/// else we're in trouble)</param>
		/// <param name="availableWidth"></param>
		/// <returns>The estimated height for the specified hvo in paragraphs</returns>
		/// ------------------------------------------------------------------------------------
		public virtual int EstimateHeight(int hvo, int frag, int availableWidth)
		{
			CheckDisposed();

			return (int)(10 * Zoom);
		}
		#endregion
	}
	#endregion

	#region Class RootSiteGroup
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// This class acts as a master root site for one or more slaves. It is a wrapper
	/// UserControl which contains the scroll bar. Certain invalidate operations
	/// that are a result of scroll position changes need to affect all slaves.
	/// </summary>
	/// ------------------------------------------------------------------------------------
	public class RootSiteGroup : Control, IRootSite, IxCoreColleague, IHeightEstimator,
		IFWDisposable, IRootSiteGroup
	{
		#region Member variables
		// m_slaves holds RootSite objects.
		private List<IRootSiteSlave> m_slaves = new List<IRootSiteSlave>(3);
		private IVwSynchronizer m_sync = VwSynchronizerClass.Create();
		private ActiveViewHelper m_activeViewHelper = null;
		private IRootSiteSlave m_scrollingController;

		/// <summary>
		/// When we adjust the scroll range of a root site slave, we get spurious OnSizeChanged
		/// calls and do NOT want them to force scrolling to show the selection, nor to do
		/// a layout, in ANY of the slaves.
		/// </summary>
		private bool m_fSuppressSizeChangedEffects;
		private IParagraphCounter m_paraCounter;
		#endregion

		#region Constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Should only be used for tests!
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public RootSiteGroup() : this(null, 0)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="cache">The Fdo Cache</param>
		/// <param name="viewTypeId">An identifier for a group of views that share the same
		/// height estimates</param>
		/// ------------------------------------------------------------------------------------
		public RootSiteGroup(FdoCache cache, int viewTypeId)
		{
			// NOTE: This ParagraphCounter is shared among multiple views (i.e. references to
			// the same counter will be used in each RootSiteGroup with the same cache and
			// viewTypeId)
			m_paraCounter = ParagraphCounterManager.GetParaCounter(cache, viewTypeId);
		}
		#endregion

		#region IDisposable override

		/// ------------------------------------------------------------------------------------
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
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				Debug.Assert(m_scrollingController == null ||
					Controls.Contains(m_scrollingController as Control));
				if (m_slaves != null)
				{
					// We need to close all of the rootboxes because when controls are
					// destroyed they cause the other controls still on the parent to
					// resize. If the rootbox is sync'd with other views then the other
					// views will try to layout their rootboxes. This is BAD!!! :)
					foreach (RootSite site in m_slaves)
						site.CloseRootBox();

					foreach (Control ctrl in m_slaves)
					{
						if (!Controls.Contains(ctrl))
							ctrl.Dispose();
					}
				}
				if (m_slaves != null)
					m_slaves.Clear();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_slaves = null;
			if (m_sync != null)
			{
				Marshal.ReleaseComObject(m_sync);
				m_sync = null;
			}
			m_activeViewHelper = null;
			m_scrollingController = null;

			base.Dispose(disposing);
		}

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		#endregion IDisposable override

		#region Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="oldPos"></param>
		/// <param name="newPos"></param>
		/// ------------------------------------------------------------------------------------
		private void HandleVerticalScrollPositionChanged(object sender, int oldPos, int newPos)
		{
			foreach (RootSite slave in m_slaves)
			{
				if (slave != sender)
					slave.ScrollPosition = new Point(-slave.ScrollPosition.X, newPos);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This MUST be called by the MakeRoot method or something similar AFTER the
		/// root box is created but BEFORE the view is laid out. Even after it is called,
		/// MakeRoot must not do anything that would cause layout; that should not happen
		/// until all roots are synchronized.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void Synchronize(IVwRootBox rootb)
		{
			CheckDisposed();

			Synchronizer.AddRoot(rootb);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add another slave to the synchronization group.
		/// Note that it is usually also necessary to add it to the Controls collection.
		/// That isn't done here to give the client more control over when it is done.
		/// </summary>
		/// <param name="rootsite"></param>
		/// ------------------------------------------------------------------------------------
		public void AddToSyncGroup(IRootSiteSlave rootsite)
		{
			CheckDisposed();

			if (rootsite == null)
				return;
			m_slaves.Add(rootsite);
			rootsite.Group = this;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// See RootSite.InvalidateForLazyFix for explanation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void InvalidateForLazyFix()
		{
			CheckDisposed();

			foreach (RootSite rootsite in m_slaves)
				rootsite.Invalidate();
		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets all of the slaves in this group
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public List<IRootSiteSlave> Slaves
		{
			get
			{
				CheckDisposed();
				return m_slaves;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the member of the rootsite group that controls scrolling (i.e. the one
		/// with the vertical scroll bar).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IRootSiteSlave ScrollingController
		{
			get
			{
				CheckDisposed();
				return m_scrollingController;
			}
			set
			{
				CheckDisposed();

				if (m_scrollingController != null)
				{
					m_scrollingController.VerticalScrollPositionChanged -=
						new ScrollPositionChanged(HandleVerticalScrollPositionChanged);
				}

				m_scrollingController = value;
				Debug.Assert(m_slaves.Contains(m_scrollingController));

				if (m_scrollingController != null)
				{
					m_scrollingController.VerticalScrollPositionChanged +=
						new ScrollPositionChanged(HandleVerticalScrollPositionChanged);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Controls whether size change suppression is in effect.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool SizeChangedSuppression
		{
			get
			{
				CheckDisposed();
				return m_fSuppressSizeChangedEffects;
			}
			set
			{
				CheckDisposed();
				m_fSuppressSizeChangedEffects = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets and sets which slave rootsite is the active, or focused, one. Commands such as
		/// Find/Replace will pertain to the active rootsite.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IRootSite FocusedRootSite
		{
			get
			{
				CheckDisposed();

				if (m_activeViewHelper == null)
					m_activeViewHelper = new ActiveViewHelper(this);

				if (m_activeViewHelper.ActiveView == this)
					return null;

				if (m_activeViewHelper.ActiveView is IRootSiteGroup)
					return ((IRootSiteGroup)m_activeViewHelper.ActiveView).FocusedRootSite;

				return m_activeViewHelper.ActiveView;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the object that synchronizes all the root boxes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IVwSynchronizer Synchronizer
		{
			get
			{
				CheckDisposed();
				return m_sync;
			}
		}
		#endregion

		#region Event related methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If possible, pass focus to your currently focused control.
		/// If you don't know one, try to pass it to your first slave.
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnGotFocus(EventArgs e)
		{
			if (FocusedRootSite is Control)
				(FocusedRootSite as Control).Focus();
			else if (m_slaves.Count > 0 && m_slaves[0] is Control)
				(m_slaves[0] as Control).Focus();
			else
				Debug.Assert(false, "RootSiteGroup should not get focus with no slaves");
		}
		#endregion

		#region Implementation of IRootSite
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Scroll the selection in view and set the IP at the given client position.
		/// </summary>
		/// <param name="sel">The selection</param>
		/// <param name="dyPos">Position from top of client window where IP should be set</param>
		/// ------------------------------------------------------------------------------------
		public void ScrollSelectionToLocation(IVwSelection sel, int dyPos)
		{
			CheckDisposed();

			throw new NotImplementedException("ScrollSelectionToLocation is not implemented for RootSiteGroup");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Refreshes the display :)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void RefreshDisplay()
		{
			CheckDisposed();

			Debug.Assert(ScrollingController != null);
			// RefreshDisplay now happens through all sync'd views in the Views code.
			ScrollingController.RefreshDisplay();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void CloseRootBox()
		{
			CheckDisposed();

			for (int i = 0; i < m_slaves.Count; i++)
			{
				if (m_slaves[i] is IRootSite)
					((IRootSite)m_slaves[i]).CloseRootBox();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Commit any outstanding changes
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void Commit()
		{
			CheckDisposed();

			for (int i = 0; i < m_slaves.Count; i++)
			{
				if (m_slaves[i] is IRootSite)
					((IRootSite)m_slaves[i]).Commit();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Allows the IRootSite to be cast as an IVwRootSite. This will recurse through nested
		/// rootsite slaves until it finds a real IVwRootSite.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public virtual IVwRootSite CastAsIVwRootSite()
		{
			CheckDisposed();

			IRootSite rootSite = FocusedRootSite;
			// If we didn't find the focused rootsite then find the first slave that is an
			// IRootSite.
			if (rootSite == null)
			{
				for (int i = 0; i < m_slaves.Count; i++)
				{
					if (m_slaves[i] is IRootSite)
					{
						rootSite = (IRootSite)m_slaves[i];
						if (rootSite is Control)
							((Control)rootSite).Focus();
						break;
					}
				}
			}
			return (rootSite == null)? null : rootSite.CastAsIVwRootSite();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets editing helper from focused root site, if there is one.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual EditingHelper EditingHelper
		{
			get
			{
				CheckDisposed();
				return (FocusedRootSite == null ? null : FocusedRootSite.EditingHelper);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// A list of zero or more internal rootboxes.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public virtual List<IVwRootBox> AllRootBoxes()
		{
			CheckDisposed();

			List<IVwRootBox> rootboxes = new List<IVwRootBox>();
			for (int i = 0; i < m_slaves.Count; i++)
			{
				if (m_slaves[i] is IRootSite)
				{
					IRootSite rs = (IRootSite)m_slaves[i];
					rootboxes.AddRange(rs.AllRootBoxes());
				}
			}
			return rootboxes;
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
				return true;
			}
			set
			{
				CheckDisposed();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether or not to allow layout.
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public bool AllowLayout
		{
			get { return true; }
			set { }
		}

		/// ------------------------------------------------------------------------------------
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
		/// </summary>
		/// <param name="prootb">The root box</param>
		/// <returns>Width available for layout</returns>
		/// ------------------------------------------------------------------------------------
		public int GetAvailWidth(IVwRootBox prootb)
		{
			throw new NotImplementedException("The method or operation is not implemented.");
		}

		#endregion

		#region Implementation of IHeightEstimator
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This routine is used to estimate the height of an item. The item will be one of
		/// those you have added to the environment using AddLazyItems. Note that the calling
		/// code does NOT ensure that data for displaying the item in question has been loaded.
		/// The first three arguments are as for Display, that is, you are being asked to
		/// estimate how much vertical space is needed to display this item in the available width.
		/// </summary>
		/// <param name="hvo">Item whose height is to be estimated</param>
		/// <param name="frag">Basically indicates what kind of thing the HVO represents (or
		/// else we're in trouble)</param>
		/// <param name="availableWidth"></param>
		/// <returns>Height of an item in points</returns>
		/// ------------------------------------------------------------------------------------
		public int EstimateHeight(int hvo, int frag, int availableWidth)
		{
			CheckDisposed();

			// Find maximum height of all rootsite slaves in view
			int maxHeight = 0;
			int paraHeight;
			if (m_slaves.Count > 0)
			{
				Debug.Assert(m_paraCounter != null,
					"Need to set ParagraphCounterManager.ParagraphCounterType before creating RootSiteGroup");
				int slaveWidth;
				foreach (RootSite slave in m_slaves)
				{
					slaveWidth = slave.GetAvailWidth(null);
					Debug.Assert(slaveWidth != 0 || !slave.Visible);
					slaveWidth = (slaveWidth == 0) ? 1 : slaveWidth;
					paraHeight = (RootSite.kdxBaselineRootsiteWidth * slave.AverageParaHeight) /
						slaveWidth;
					maxHeight = Math.Max(m_paraCounter.GetParagraphCount(hvo, frag) * paraHeight,
						maxHeight);
				}
			}
			else
			{
				Debug.Fail("Need to handle this!");
			}

			return maxHeight;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the average paragraph height in points.
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public int AverageParaHeight
		{
			get { throw new NotImplementedException("The method or operation is not implemented."); }
		}

		#endregion

		#region IxCoreColleague Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Not used
		/// </summary>
		/// <param name="mediator"></param>
		/// <param name="configurationParameters"></param>
		/// ------------------------------------------------------------------------------------
		public void Init(Mediator mediator, System.Xml.XmlNode configurationParameters)
		{
			CheckDisposed();

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the message target, i.e. 'this'
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public virtual IxCoreColleague[] GetMessageTargets()
		{
			CheckDisposed();

			// return list of view windows with focused window being the first one
			List<IxCoreColleague> targets = new List<IxCoreColleague>();
			foreach (Control ctrl in Controls)
			{
				if (ctrl is IxCoreColleague && ctrl.ContainsFocus)
				{
					targets.Add((IxCoreColleague)ctrl);
					break;
				}
			}

			targets.Add(this);
			return targets.ToArray();
		}

		#endregion

	}

	#endregion
}
