// Copyright (c) 2005-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: XmlBrowseViewBase.cs
// Responsibility: Randy Regnier
// Last reviewed:
//
// <remarks>
// </remarks>

using System;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;
using System.Diagnostics;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO.Application;
using SIL.Utils;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using XCore;
using SIL.FieldWorks.Common.FwUtils;
using System.Diagnostics.CodeAnalysis;

namespace SIL.FieldWorks.Common.Controls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Control to show rows of data
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class XmlBrowseViewBase : RootSite, IVwNotifyChange, IPostLayoutInit, IClearValues
	{
		#region Events

		/// <summary>
		/// This event notifies you that the selected object changed, passing an argument from which you can
		/// directly obtain the new object. If you care more about the position of the object in the list
		/// (especially if the list may contain duplicates), you may wish to use the SelectedIndexChanged
		/// event instead. This SelectionChangedEvent will not fire if the selection moves from one
		/// occurrene of an object to another occurrence of the same object.
		/// </summary>
		public event FwSelectionChangedEventHandler SelectionChangedEvent;
		/// <summary>
		/// This event notifies you that the selected index changed. You can find the current index from
		/// the SelectedIndex property, and look up the object if needed...but if you mainly care about
		/// the object, it is probably better to use SelectionChangedEvent.
		/// </summary>
		public event EventHandler SelectedIndexChanged;

		#endregion Events

		#region Data members

		/// <summary></summary>
		protected XmlBrowseViewBaseVc m_xbvvc;
		/// <summary></summary>
		protected int m_hvoRoot;
		/// <summary></summary>
		protected int m_fakeFlid;
		/// <summary>
		/// the sda in which looking up m_fakeFlid as a property of m_hvoRoot works.
		/// </summary>
		protected ISilDataAccessManaged m_sda;
		/// <summary></summary>
		protected XmlNode m_nodeSpec;
		/// <summary></summary>
		internal protected BrowseViewer m_bv;
		/// <summary> record list supplying browse view content </summary>
		protected ISortItemProvider m_sortItemProvider;
		/// <summary></summary>
		protected int m_hvoOldSel = 0;
		/// <summary></summary>
		protected StringTable m_stringTable;
		/// <summary></summary>
		protected bool m_wantScrollIntoView = true;
		/// <summary></summary>
		protected string m_id;
		/// <summary></summary>
		protected int m_selectedIndex = -1; // index of selected row, initially none is selected.
		/// <summary></summary>
		protected SelectionHighlighting m_fSelectedRowHighlighting = SelectionHighlighting.border;
		/// <summary></summary>
		protected bool m_rootObjectHasBeenSet = false;
		/// <summary></summary>
		protected int m_iTopOfScreenObjectForScrollPosition; // see OnSaveScrollPosition
		/// <summary></summary>
		protected int m_dyTopOfScreenOffset; // see OnSaveScrollPosition
		/// <summary></summary>
		protected int m_tagMe = XMLViewsDataCache.ktagTagMe;
		/// <summary></summary>
		protected bool m_fHandlingMouseUp = false;

		private System.ComponentModel.IContainer components = null;

		#endregion Data members

		#region Properties

		/// <summary>
		/// a look up table for getting the correct version of strings that the user will see.
		/// </summary>
		public StringTable StringTbl
		{
			get
			{
				CheckDisposed();
				return m_stringTable;
			}
			set
			{
				CheckDisposed();
				m_stringTable = value;
			}
		}

		class LineCollector : StringCollectorEnv
		{
			public LineCollector(IVwEnv baseEnv, ISilDataAccess sda, int hvoRoot) : base(baseEnv, sda, hvoRoot)
			{
			}

			public override void AddObjProp(int tag, IVwViewConstructor vc, int frag)
			{
				base.AddObjProp(tag, vc, frag);
				if (frag == XmlBrowseViewBaseVc.kfragListItemInner)
					m_builder.AppendLine();
			}
		}

		/// <summary>
		/// Export the contents of the browse view to the debug window. Answer false so other browse views can export theirs also.
		/// </summary>
		public bool OnExportText(object args)
		{
			Debug.WriteLine("--------------");
			int hvo, frag;
			IVwViewConstructor vc;
			IVwStylesheet ss;
			m_rootb.GetRootObject(out hvo, out vc, out frag, out ss);
			var collector = new LineCollector(null, m_sda, hvo);
			m_xbvvc.Display(collector, hvo, frag);
			Debug.WriteLine(collector.Result);
			Debug.WriteLine("--------------");
			return false;
		}

		/// <summary>
		///
		/// </summary>
		internal ISortItemProvider SortItemProvider
		{
			get { return m_sortItemProvider; }
		}

		/// <summary>
		/// It's better to return our SDA directly rather than going to the root box, because occasionally
		/// when filtering or sorting we may need to obtain it before the root box is created.
		/// </summary>
		public override ISilDataAccess DataAccess { get { return m_sda; } }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return the VC. It has some important functions related to interpreting fragment IDs
		/// that the filter bar needs.
		/// </summary>
		/// <value>The vc.</value>
		/// ------------------------------------------------------------------------------------
		public virtual XmlBrowseViewBaseVc Vc
		{
			get
			{
				CheckDisposed();
				return m_xbvvc;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when [request debug info].
		/// </summary>
		/// <param name="commandObj">The command obj.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool OnRequestDebugInfo(object commandObj)
		{
			CheckDisposed();

			using (var logger = new SimpleLogger())
			{
				m_xbvvc.LogStream = logger;
				int cv = m_sda.get_VecSize(m_hvoRoot, m_fakeFlid);
				if (cv > 0)
				{
					int hvoObjSel = m_sda.get_VecItem(m_hvoRoot,
													  m_fakeFlid, m_selectedIndex < 0 ? 0 : m_selectedIndex);
					m_rootb.PropChanged(hvoObjSel, m_tagMe, 0, 0, 0);
					Update(); // causes the PropChanged to actually invoke the VC.
				}
				else
				{
					logger.WriteLine("The record list is empty.");
				}
				MessageBox.Show(this, logger.Content, "DEBUG: Row generation info");
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override bool RefreshDisplay()
		{
			CheckDisposed();

			bool fChanged = m_xbvvc.RemoveInvalidColumns();
			if (fChanged)
				m_bv.InstallNewColumns(m_xbvvc.ColumnSpecs);
			base.RefreshDisplay();
			if (!Cache.ServiceLocator.IsValidObjectId(m_hvoRoot))
			{
				m_hvoRoot = 0;
				m_selectedIndex = -1;
			}

			int chvo = m_sda.get_VecSize(m_hvoRoot, m_fakeFlid);
			if (m_selectedIndex >= chvo)
			{
				m_selectedIndex = chvo - 1;
			}
			if (m_selectedIndex >= 0)
			{
				int hvoNewObj = m_sda.get_VecItem(m_hvoRoot, m_fakeFlid, m_selectedIndex);
						DoSelectAndScroll(hvoNewObj, m_selectedIndex);
			}
			//Enhance: if all the RefreshDisplay work has been done for all the descendants then return true here.
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the HVO of the selected object.
		/// </summary>
		/// <value>The selected object.</value>
		/// ------------------------------------------------------------------------------------
		public int SelectedObject
		{
			get
			{
				CheckDisposed();

				if (SelectedIndex < 0)
					return 0;
				if (m_sda.get_VecSize(m_hvoRoot, m_fakeFlid) <= SelectedIndex)
				{
					return 0; // The only time this happens is during refresh.
				}
				return m_sda.get_VecItem(m_hvoRoot, m_fakeFlid, SelectedIndex);
			}
		}

		/// <summary>
		/// Return the number of rows in the view.
		/// </summary>
		internal virtual int RowCount
		{
			get
			{
				CheckDisposed();
				return m_sda.get_VecSize(m_hvoRoot, m_fakeFlid);
			}
		}

		/// <summary>
		/// Return the index of the 'selected' row in the view.
		/// Returns -1 if nothing is selected, or there are no rows at all.
		/// If the selection spans multiple rows, returns the anchor row.
		/// If in select-only mode, there may be no selection, but it will then be the row
		/// last clicked.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">
		/// Thrown on an attempt to set the index to less than -1, or more than the vector count.
		/// The new index can only be set to -1, IFF there are no elements in the vector.
		/// </exception>
		public int SelectedIndex
		{
			get
			{
				CheckDisposed();
				return m_selectedIndex;
			}
			set
			{
				CheckDisposed();

				Debug.Assert(value >= -1, "The selected index cannot be set to less than -1.");
				// Just in case it isn't caught in a Debug build...
				if (value < -1)
					throw new ArgumentOutOfRangeException("XmlBrowseViewBase.SelectedIndex", value.ToString(), "Index cannot be set to less than -1.");
				if (m_selectedIndex == value)
				{
					// It's useful to check this anyway, since the width of the window or something else
					// that affects visibility may have changed...but don't CHANGE the selection, the user may be editing...(LT-12092)
					if (value >= 0 && m_wantScrollIntoView)
						MakeSelectionVisible(GetRowSelection(value));
					return;
				}

				int oldIndex = m_selectedIndex;

				int cobj = m_sda.get_VecSize(m_hvoRoot, m_fakeFlid);
				Debug.Assert((cobj == 0 && value == -1) || (cobj > 0 && value >= 0), "The new index must be -1, if there are no items in the list, or the new value must be zero, or greater.");
				Debug.Assert(value < cobj, "You cannot set the index to a value greater then number of objects.");

				// Just in case it isn't caught in a Debug build...technically, this is an error,
				// as indicated in the Asserts above, even if cobj == 0; we shouldn't be setting an index of 0
				// when there are no objects. But it's happening, as of Sep 20 2006, and we can't pin down
				// repeatable circumstances. So if someone tries to select the first object of an empty
				// list, for now we'll just treat it as selecting nothing.
				if (value >= cobj && cobj > 0)
					throw new ArgumentOutOfRangeException("XmlBrowseViewBase.SelectedIndex", value.ToString(), "Index cannot be set to more than are in the collection.");
				// One may be tempted to move this above the assert and exception,
				// but no one who knows anything about the contents should be trying
				// to set the index of an empty collection.
				if (cobj == 0)
				{
					// Nobody home, so quit.
					Debug.Assert(value == -1, "Cannot set the index to anything except -1, when there are no items in the list.");
					// Just in case it isn't caught in a Debug build...
					// Allow zero on release builds since that's the common case, and this bug
					// is not reliably repeatable, it just happens often enough to be worrisome.
					if (value > 0)
						throw new ArgumentOutOfRangeException("XmlBrowseViewBase.SelectedIndex", value.ToString(), "Cannot set the index to anything except -1, when there are no items in the list.");
					m_hvoOldSel = 0;
					m_selectedIndex = -1;
					// Clearing out the list changes the selection, so let everyone know.
					m_mediator.IdleQueue.Add(IdleQueuePriority.Medium, FireSelectionChanged);
					return;
				}
				int hvoObjNewSel = GetNewSelectionObject(value);

				// Set the member variable before firing the events,
				// in case the event handlers access the Selectedindex property.
				// Wouldn't want them to get the wrong answer, and be confused.
				m_selectedIndex = value;
				if (hvoObjNewSel != m_hvoOldSel)
				{
					m_hvoOldSel = hvoObjNewSel;
					// Notify any delegates that the selection of the main object in the vector
					// has changed.
					if (SelectionChangedEvent != null && hvoObjNewSel != 0)
					{
						SelectionChangedEvent(this, new FwObjectSelectionEventArgs(hvoObjNewSel, value));
						// Recalculate the vector size since somebody somewhere may have deleted something.
						// See LT-6884 for an example.
						cobj = m_sda.get_VecSize(m_hvoRoot, m_fakeFlid);
					}
				}

				// Some of the changes below may destroy the new selection, especially setting the new index, but
				// possibly also clearing the old one. So save the selection info in order to restore it afterwards.
				SelectionHelper selection = null;
				int oldSelectionAnchorIndex = -1;
				int oldSelectionEndIndex = -1;
				if (RootBox != null)
				{
					var vwSelection = RootBox.Selection;
					oldSelectionEndIndex = GetRowIndexFromSelection(vwSelection, true);
					oldSelectionAnchorIndex = GetRowIndexFromSelection(vwSelection, false);
					selection = SelectionHelper.GetSelectionInfo(vwSelection, this);
				}
				// Don't set the data mamber here, as it may be too late for the clients of the above event.
				// If they access the SelectedIndex property between the event firing and this setting,
				// they would get the old value, which is wrong.
				//m_selectedIndex = value;
				if (SelectedIndexChanged != null)
					SelectedIndexChanged(this, new EventArgs());

				if (oldIndex >= 0 && oldIndex < cobj)
				{
					// Turn off the highlighting of the old item.
					int hvoObjOldSel = m_sda.get_VecItem(m_hvoRoot, m_fakeFlid, oldIndex);
					try
					{
						m_rootb.PropChanged(hvoObjOldSel, m_tagMe, 0, 0, 0);
					}
					catch (Exception)
					{
						m_bv.RaiseSelectionDrawingFailure();
						//throw new SelectionDrawingError("failed to clear old selection for object " + hvoObjOldSel, e);
					}
				}
				// Turn on the highlighting of the new item.
				try
				{
					if (m_rootb != null) // programmatic selection could be made before we create the root.
						m_rootb.PropChanged(hvoObjNewSel, m_tagMe, 0, 0, 0);
				}
				catch (Exception)
				{
					m_bv.RaiseSelectionDrawingFailure();
				}

				// TE-6912: This RestoreSelection scrolled back to top
				// This is tricky: if we have a multi-row selection, e.g., from a long drag or select all, we
				// DO want to restore it. Also if it is entirely within the new selected row, where the user might also have dragged.
				// OTOH, we must NOT restore one somewhere else, like the old selected row, because that will
				// move the selected row back there!
				if (!ReadOnlySelect && selection != null &&
					(SelectedIndex == oldSelectionEndIndex || oldSelectionEndIndex != oldSelectionAnchorIndex))
				{
					selection.RestoreSelectionAndScrollPos();
				}

				// do Selection & Scroll after highlighting, so insertion point will show up.
				if (m_wantScrollIntoView)
				{
					DoSelectAndScroll(hvoObjNewSel, value);
					// allow preventing setting the focus (LT-9481)
					// Don't steal the focus from another window.  See FWR-1795.
					if (CanFocus && ParentForm == Form.ActiveForm)
						Focus(); // Note: used to be part of DoSelectAndScroll, but I'm not sure why...
				}
				Update();
				// actual selection changed event only on idle; this makes the browse view more responsive,
				// especially to arrow keys on auto-repeat.
				m_mediator.IdleQueue.Add(IdleQueuePriority.Medium, FireSelectionChanged);
			}
		}

		bool FireSelectionChanged(object parameter)
		{
			if (IsDisposed || m_rootb == null)
				return true; // presumably we've been disposed; this happens (at least) in tests where a later test may simulate idle events.
			int hvoObjNewSel = GetNewSelectionObject(m_selectedIndex);
			if (hvoObjNewSel == 0)
			{
				if (m_selectedIndex == 0)
				{
					m_selectedIndex = -1;
				}
				else if (m_selectedIndex > 0)
				{
					int cobj = m_sda.get_VecSize(m_hvoRoot, m_fakeFlid);
					Debug.Assert(m_selectedIndex >= cobj);
					m_selectedIndex = cobj - 1;
					hvoObjNewSel = GetNewSelectionObject(m_selectedIndex);
				}
			}
			if (hvoObjNewSel != m_hvoOldSel)
			{
				m_hvoOldSel = hvoObjNewSel;
				// Notify any delegates that the selection of the main object in the vector
				// has changed.
				if (SelectionChangedEvent != null)
				{
					SelectionChangedEvent(this, new FwObjectSelectionEventArgs(hvoObjNewSel, m_selectedIndex));
				}
			}
			if (SelectedIndexChanged != null)
				SelectedIndexChanged(this, new EventArgs());
			return true;
		}

		/// <summary>
		/// Because this is a simple root site, which is derived from UserControl, we can't prevent MouseDown from
		/// giving it focus. But we can give other linked windows a chance to take it back.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnGotFocus(EventArgs e)
		{
			base.OnGotFocus(e);
			if (ReadOnlySelect)
			{
				// We should NOT have focus! See if someone can take it back.
				m_bv.Mediator.BroadcastMessage("BrowseViewStoleFocus", this);
			}
		}

		/// <summary>
		/// Convert the new selection into a real object before making any selection.
		/// Otherwise the views code might convert it during the selection, making the selection invalid.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		private int GetNewSelectionObject(int index)
		{
			if (index < 0)
				return 0;
			int cobj = m_sda.get_VecSize(m_hvoRoot, m_fakeFlid);
			if (cobj == 0 || index >= cobj)
				return 0;
			return m_sda.get_VecItem(m_hvoRoot, m_fakeFlid, index);
		}

		/// <summary>
		/// read configuration for the browse view to determine whether to try to convert dummy objects to real objects
		/// when as they become painted on the screen. (Default is false)
		/// </summary>
		/// <returns></returns>
		internal bool ShouldConvertDummiesInView()
		{
			return m_nodeSpec != null && XmlUtils.GetOptionalBooleanAttributeValue(m_nodeSpec, "convertDummiesInView", false);
		}

		/// <summary></summary>
		protected int m_ydSelTop = 0;
		/// <summary></summary>
		protected int m_ydSelBottom = 0;
		/// <summary></summary>
		protected int m_ydSelScrollPos = 0;
		/// <summary></summary>
		protected int m_iSelIndex = 0;
		/// <summary>
		/// Handle the special aspects of adjusting the scroll position for a table of cells
		/// like we have in the browse view.  See LT-3607 for details of what can go wrong
		/// without this override.
		/// </summary>
		/// <param name="dxdSize">change in the horizontal size</param>
		/// <param name="dxdPosition">horizontal position where change occurred</param>
		/// <param name="dydSize">change in the vertical size</param>
		/// <param name="dydPosition">vertical position where change occurred</param>
		/// <returns></returns>
		protected override bool AdjustScrollRange1(int dxdSize, int dxdPosition, int dydSize,
			int dydPosition)
		{
			int dydRangeNew = AdjustedScrollRange.Height;
			// Remember: ScrollPosition returns negative values!
			int dydPosOld = -ScrollPosition.Y;
			int dydPosNew;
			// If the current position is after where the change occurred, it needs to
			// be adjusted by the same amount.
			if (dydPosOld > dydPosition)
				dydPosNew = dydPosOld + dydSize;
			else
				dydPosNew = dydPosOld;
			//Debug.WriteLine(
			//	"XmlBrowseViewBase.AdjustScrollRange(dxdSize=" + dxdSize.ToString() +
			//	", dxdPos=" + dxdPosition.ToString() + ", dydSize=" + dydSize.ToString() +
			//	", dydPos=" + dydPosition.ToString() +
			//	"): dydRangeNew=" + dydRangeNew.ToString() +
			//	", dydPosOld=" + dydPosOld.ToString() + ", dydPosNew=" + dydPosNew.ToString());
			// But that doesn't quite work for browse view, because we want the whole row to
			// show, either at the top or the bottom.
			// Use values stored by MakeSelectionVisible if they're available.
			int iSelIndex = m_iSelIndex;
			if (iSelIndex == SelectedIndex &&
				(m_ydSelTop != 0 || m_ydSelBottom != 0))
			{
				int dyBottomNew = dydPosNew + ClientHeight - m_dyHeader;
				int dySelTop = m_ydSelTop;
				int dySelBottom = m_ydSelBottom;
				//int dySelScrollPos = m_ydSelScrollPos;
				// clear these values so they won't be used when stale.
				m_ydSelTop = 0;
				m_ydSelBottom = 0;
				m_ydSelScrollPos = 0;
				m_iSelIndex = 0;
				if (dySelTop > dydPosition)
				{
					dySelTop += dydSize;
					dySelBottom += dydSize;
				}
				if (dySelTop < dydPosNew)
				{
					//Debug.WriteLine("    XmlBrowseViewBase.AdjustScrollRange() - dySelTop=" +
					//	dySelTop.ToString());
					dydPosNew = dySelTop;
				}
				else if (dySelBottom > dyBottomNew)
				{
					int deltaY = dySelBottom - dyBottomNew;
					dydPosNew += deltaY;
					//Debug.WriteLine("    XmlBrowseViewBase.AdjustScrollRange() - deltaY=" +
					//	deltaY.ToString());
				}
				//Debug.WriteLine(
				//	"XmlBrowseViewBase.AdjustScrollRange(dxdSize=" + dxdSize.ToString() +
				//	", dxdPos=" + dxdPosition.ToString() + ", dydSize=" + dydSize.ToString() +
				//	", dydPos=" + dydPosition.ToString() +
				//	"): dydPosNew=" + dydPosNew.ToString() +
				//	", dySelTop=" + dySelTop.ToString() +
				//	", dySelBottom=" + dySelBottom.ToString());
				//int dyBottomOld = dydPosOld + ClientHeight - m_dyHeader;
				//Debug.WriteLine("    XmlBrowseViewBase.AdjustScrollRange(): height=" +
				//	(ClientHeight - m_dyHeader).ToString() +
				//	", SelScrollPos=" + dySelScrollPos.ToString() +
				//	", dyBottomOld=" + dyBottomOld.ToString() +
				//	", dyBottomNew=" + dyBottomNew.ToString());
			}

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

		/// <summary>
		/// Make the selection that would be made by clicking at the specified mouse event,
		/// but don't install it.
		/// </summary>
		/// <param name="e"></param>
		/// <returns></returns>
		internal IVwSelection MakeSelectionAt(MouseEventArgs e)
		{
			CheckDisposed();

			Point pt;
			Rectangle rcSrcRoot;
			Rectangle rcDstRoot;
			using(new HoldGraphics(this))
			{
				pt = PixelToView(new Point(e.X, e.Y));
				GetCoordRects(out rcSrcRoot, out rcDstRoot);
				// This can legitimately return null,
				// e.g. because they selected beyond the last item.
				return m_rootb.MakeSelAt(pt.X, pt.Y, rcSrcRoot, rcDstRoot, false);
			}
		}

		internal void DoSelectionSideEffects(MouseEventArgs e)
		{
			CheckDisposed();

			IVwSelection vwselNew = MakeSelectionAt(e);
			if (vwselNew != null)
			{
				//m_wantScrollIntoView = false; // It should already be visible here, since they clicked here.
				DoSelectionSideEffects(vwselNew);
				//m_wantScrollIntoView = true;
			}
		}

		private bool m_fHandlingSideEffects = false;

		/// <summary>
		/// Given a selection in the view, return the row index. Rarely may return -1 if unable to
		/// identify a row.
		/// </summary>
		/// <param name="sel"></param>
		/// <param name="fEndPoint">true to get index based on end of selection, false based on anchor.
		/// deafault is true, so in a long drag we get the mouse-up row.</param>
		/// <returns></returns>
		internal int GetRowIndexFromSelection(IVwSelection sel, bool fEndPoint)
		{
			if (sel == null)
				return -1;
			try
			{
				int clev = sel.CLevels(fEndPoint);
				int hvoRoot, tag, ihvo, cpropPrevious;
				IVwPropertyStore vps;
				sel.PropInfo(fEndPoint, clev - 1, out hvoRoot, out tag, out ihvo, out cpropPrevious, out vps);
				if (tag != m_fakeFlid) // not sure how this could happen, but the precaution was in an earlier version.
					return -1;
				return ihvo;
			}
			catch (System.Runtime.InteropServices.COMException)
			{
				// This shouldn't happen, but don't let it be catastrophic if it does.
			}
			return -1;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Common code to HandleSelectionChange and MouseDown.
		/// </summary>
		/// <param name="sel"></param>
		/// ------------------------------------------------------------------------------------
		protected void DoSelectionSideEffects(IVwSelection sel)
		{
			// There is tricky and subtle stuff going on here. Se LT-3565, 6501, and 9192 for things
			// that should not be broken by any changes.
			// If we're in the middle of a drag, or processing mouse down, we don't want to change
			// the selected row...we're postponing that to mouse up.
			if (!m_fMouseUpEnabled)
			SetSelectedIndex(GetRowIndexFromSelection(sel, true));
		}

		internal void SetSelectedIndex(int ihvo)
		{
			if (m_fHandlingSideEffects)
				return;
			if (ihvo == -1)
			{
				// we seem to have cleared our list, so make sure we
				// can't use this value to get a list item.
				m_selectedIndex = -1;
				return;
			}
			if (SelectedIndex != ihvo) // No sense in waking up the beast for no reason.
			{
				m_fHandlingSideEffects = true;
				SelectedIndex = ihvo;
				m_fHandlingSideEffects = false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the row/cell information of the current selection.
		/// </summary>
		/// <param name="vwsel"></param>
		/// <param name="iLevel"></param>
		/// <param name="iBox"></param>
		/// <param name="iTableBox"></param>
		/// <param name="cTableBoxes"></param>
		/// <param name="iTableLevel"></param>
		/// <param name="iCellBox"></param>
		/// <param name="cCellBoxes"></param>
		/// <param name="iCellLevel"></param>
		/// ------------------------------------------------------------------------------------
		internal static void GetCurrentTableCellInfo(IVwSelection vwsel, out int iLevel, out int iBox, out int iTableBox, out int cTableBoxes, out int iTableLevel, out int iCellBox, out int cCellBoxes, out int iCellLevel)
		{
			int cBoxes = -1;
			iBox = -1;
			iTableBox = -1;
			cTableBoxes = -1;
			iTableLevel = -1;
			int iRowBox = -1;
			int cRowBoxes = -1;
			int iRowLevel = -1;
			iCellBox = -1;
			cCellBoxes = -1;
			iCellLevel = -1;
			// Find the current table cell and advance to the next one, possibly on the
			// next or previous row.
			int cLevels = vwsel.get_BoxDepth(true);
			VwBoxType vbt = VwBoxType.kvbtUnknown;
			for (iLevel = 0; iLevel < cLevels; ++iLevel)
			{
				cBoxes = vwsel.get_BoxCount(true, iLevel);
				iBox = vwsel.get_BoxIndex(true, iLevel);
				vbt = vwsel.get_BoxType(true, iLevel);
				switch (vbt)
				{
					case VwBoxType.kvbtTable:
						// Note that the layout should one (visible) row per "table", and
						// stacks the "table" boxes to form the visual table.  See JohnT
						// for an explanation of this nonintuitive use of tables and rows.
						// At least, i think JohnT knows why -- maybe it's RandyR?
						iTableBox = iBox;
						cTableBoxes = cBoxes;
						iTableLevel = iLevel;
						break;
					case VwBoxType.kvbtTableRow:
						iRowBox = iBox;
						cRowBoxes = cBoxes;
						iRowLevel = iLevel;
						break;
					case VwBoxType.kvbtTableCell:
						iCellBox = iBox;
						cCellBoxes = cBoxes;
						iCellLevel = iLevel;
						break;
				}
			}
			// Some simple sanity checking.
			Debug.Assert(cBoxes != -1);
			Debug.Assert(iBox != -1);
			Debug.Assert(iTableBox != -1);
			Debug.Assert(cTableBoxes != -1);
			Debug.Assert(iTableLevel != -1);
			Debug.Assert(iRowBox != -1);
			Debug.Assert(cRowBoxes != -1);
			Debug.Assert(iRowLevel != -1);
			Debug.Assert(iCellBox != -1);
			Debug.Assert(cCellBoxes != -1);
			Debug.Assert(iCellLevel != -1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the OnKeyDown event
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);

			// if we've done a tab, expand the current selection to whole cell contents.
			IVwSelection vwsel = RootBox.Selection;
			if (e.KeyCode == Keys.Tab && vwsel != null)
			{
				SelectContentsOfCell(vwsel);
			}

		}

		/// <summary>
		///
		/// </summary>
		protected void SelectContentsOfCell(IVwSelection vwsel)
		{
			if (vwsel == null)
				return;
			int iLevel;
			int iBox;
			int iTableBox;
			int cTableBoxes;
			int iTableLevel;
			int iCellBox;
			int cCellBoxes;
			int iCellLevel;
			GetCurrentTableCellInfo(vwsel, out iLevel, out iBox, out iTableBox,
									out cTableBoxes, out iTableLevel, out iCellBox, out cCellBoxes,
									out iCellLevel);
			IVwSelection vwsel2 = m_rootb.MakeSelInBox(vwsel, true, iCellLevel, iCellBox,
													   true, true, false);
			if (vwsel2 == null)
				return; // can't do anything, so give up.  See LT-9706.
			// Make sure it's in the same cell. In pathological cases, for example, tabbing
			// from a bulk edit check box in a row that is all read-only, the resulting
			// selection may be in another row. If we can't make a valid selection where
			// we're trying to, don't change it at all.
			int iLevel2;
			int iBox2;
			int iTableBox2;
			int iCellBox2;
			GetCurrentTableCellInfo(vwsel2, out iLevel2, out iBox2, out iTableBox2,
									out cTableBoxes, out iTableLevel, out iCellBox2, out cCellBoxes,
									out iCellLevel);
			if (iLevel2 == iLevel && iBox2 == iBox && iTableBox2 == iTableBox
				&& iCellBox2 == iCellBox)
			{
				vwsel2.Install();
			}
		}

		/// <summary>
		/// We only want to do something on mouse up events if the XmlBrowseView has gotten a
		/// mouseDown event.
		/// </summary>
		protected bool m_fMouseUpEnabled = false;

		/// <summary>
		/// MouseUp actions on XmlBrowseView should only be enabled if a preceeding MouseDown occured
		/// in the view.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnMouseDown(MouseEventArgs e)
		{
			m_fMouseUpEnabled = true;
			base.OnMouseDown(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Process left or right mouse button down
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnMouseUp(MouseEventArgs e)
		{
			//If XmlBrouseView did not receive a mouse down event then we do not want to
			//do anything on the mouseUp because the mouseUp would have come from clicking
			//somewhere else. LT-8939
			if (!m_fMouseUpEnabled)
				return;

			try
			{
			int newSelectedIndex = GetRowIndexFromSelection(MakeSelectionAt(e), true);
			// If we leave this set, the base method call's side effects like updating the WS combo
			// don't happen.
			m_fHandlingMouseUp = false;
			// Do this before we do stuff that might mess up scroll positions and make the mouse
			// position invalid.
			base.OnMouseUp(e);
			m_fHandlingMouseUp = true;
			// preserve any selection the mouse down made, which may be destroyed by the process
			// of moving the highlight.
			SelectionHelper helper = SelectionHelper.Create(this);
			SetSelectedIndex(newSelectedIndex);
			if (helper != null)
				helper.SetSelection(this, true, false, VwScrollSelOpts.kssoDefault);
			if (m_bv != null)
				m_bv.BrowseViewMouseUp(e);
			}
			finally
			{
				m_fMouseUpEnabled = false;
			m_fHandlingMouseUp = false;
		}
		}

		/// <summary>
		/// A browse view does not HAVE to have a selection; forcing one (e.g., after clicking on a
		/// check box) and scrolling to the one we made at the start of the view is very disconcerting.
		/// </summary>
		protected override void EnsureDefaultSelection()
		{
			return;
		}
		///// <summary>
		///// Side effects like record navigation happen after the mouse down itself is done, but hopefully
		///// before the mouse up.
		///// </summary>
		///// <param name="e"></param>
		//protected override void OnMouseUp(MouseEventArgs e)
		//{
		//    base.OnMouseUp(e);
		//    m_fHandlingMouseDown = false;
		//}

		/// <summary></summary>
		public enum SelectionHighlighting
		{
			/// <summary></summary>
			all,
			/// <summary></summary>
			border,
			/// <summary></summary>
			none
		};


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets whether the selected row is indicated by highlighting.  Intended to be used from SetSelectedRowHighlighting.  Do not
		/// use directly, but override SetSelectedRowHighlighting if a different kind of highlighting is needed.
		/// </summary>
		/// <value>The selected row highlighting.</value>
		/// ------------------------------------------------------------------------------------
		internal SelectionHighlighting SelectedRowHighlighting
		{
			get
			{
				CheckDisposed();
				return m_fSelectedRowHighlighting;
			}
			set
			{
				CheckDisposed();

				if (m_fSelectedRowHighlighting == value)
					return; // Nothing to do.

				m_fSelectedRowHighlighting = value;
				// Turn on or off the highlighting of the current row.
				if (m_selectedIndex >= 0 && m_rootb != null)
				{
					int hvoObjSel = m_sda.get_VecItem(m_hvoRoot,
						m_fakeFlid, m_selectedIndex);
					m_rootb.PropChanged(hvoObjSel, m_tagMe, 0, 0, 0);
				}
			}
		}

		/// <summary>
		/// True if we are running the read-only version of the view that is primarily used for
		/// selecting.
		/// </summary>
		protected virtual bool ReadOnlySelect
		{
			get
			{
				if (m_mediator == null)
					return false;
				// if we have an editable attribute defined use its value
				if (m_nodeSpec.Attributes["editable"] != null)
				{
					bool fEditable = XmlUtils.GetBooleanAttributeValue(m_nodeSpec, "editable");
					return !fEditable;
				}
				else
				{
					return false; // "_readOnlyBrowse" properties are deprecated.
				}

			}
		}

		/// <summary>
		/// the object that has properties that are shown by this view.
		/// </summary>
		/// <remarks> this will be changed often in the case where this view is dependent on another one;
		/// that is, or some other browse view has a list and each time to selected item changes, our
		/// root object changes.
		/// </remarks>
		public virtual int RootObjectHvo
		{
			get
			{
				CheckDisposed();
				return m_hvoRoot;
			}
			set
			{
				CheckDisposed();

				if (m_rootObjectHasBeenSet && m_hvoRoot == value)
					return; // No sense getting all worked up, if it is the same as before.
				m_hvoRoot = value;
				m_rootb.SetRootObject(m_hvoRoot, m_xbvvc, (int)XmlBrowseViewVc.kfragRoot, m_styleSheet);
				m_rootObjectHasBeenSet = true;
				// This seems to be necessary to get the data entry row to resize even if the new
				// list is the same length as the old. Must NOT remember new positions, because
				// this can be called before Layout retrieves them!
				m_bv.AdjustColumnWidths(false);
				// The old index and selected object must be wrong by now, so reset them.
				m_hvoOldSel = 0;
				int chvo = m_sda.get_VecSize(m_hvoRoot, m_fakeFlid);
				if (chvo == 0)
				{
					m_selectedIndex = -1;
				}
				else
				{
					int hvo = m_sda.get_VecItem(m_hvoRoot, m_fakeFlid, 0);
					if (hvo == (int) SpecialHVOValues.kHvoObjectDeleted)
					{
						// Deleting everything in one view doesn't seem to fix the RecordList in
						// related views.  See LT-9711.
						IRecordListUpdater x = Mediator.PropertyTable.GetValue("ActiveClerk") as IRecordListUpdater;
						if (x != null)
						{
							using (new WaitCursor(this))
							{
								//update the list, forcing a recursive refresh
								x.UpdateList(true);
							}
						}
						chvo = m_sda.get_VecSize(m_hvoRoot, m_fakeFlid);
						if (chvo == 0)
							m_selectedIndex = -1;
						else
							SelectedIndex = 0;
					}
					else
					{
						SelectedIndex = 0;
					}
				}
			}
		}

		/// <summary>
		/// The identifier of the top-level list property being displayed.
		/// </summary>
		public int MainTag
		{
			get
			{
				CheckDisposed();
				return m_fakeFlid;
			}
		}

		#endregion Properties

		#region Construction, Initialization and disposal

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:XmlBrowseViewBase"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public XmlBrowseViewBase() : base(null)
		{
			// This call is required by the Windows Form Designer.
			InitializeComponent();

			AccessibleName = "XmlBrowseViewBase";
			//Dock = System.Windows.Forms.DockStyle.Fill;
			BackColor = SystemColors.Window;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inits the specified node spec.
		/// </summary>
		/// <param name="nodeSpec">The node spec.</param>
		/// <param name="hvoRoot">The hvo root.</param>
		/// <param name="fakeFlid">The fake flid.</param>
		/// <param name="cache">The cache.</param>
		/// <param name="mediator">The mediator.</param>
		/// <param name="bv">The bv. Also used to set SortItemProvider</param>
		/// ------------------------------------------------------------------------------------
		public virtual void Init(XmlNode nodeSpec, int hvoRoot, int fakeFlid,
			FdoCache cache, Mediator mediator, BrowseViewer bv)
		{
			CheckDisposed();

			Debug.Assert((m_selectedIndex == -1), "Cannot set the index to less than zero before initializing.");
			Debug.Assert(m_nodeSpec == null || m_nodeSpec == nodeSpec, "XmlBrowseViewBase.Init: Mismatched configuration parameters.");

			m_hvoRoot = hvoRoot;
			m_fakeFlid = fakeFlid;
			if (m_nodeSpec == null)
				m_nodeSpec = nodeSpec;
			m_bv = bv;
			StringTbl = mediator.StringTbl;
			m_mediator = mediator;
			m_fdoCache = cache;
			m_sda = m_bv.SpecialCache;
			// This is usually done in MakeRoot, but we need it to exist right from the start
			// because right after we make this window we use info from the VC to help make
			// the column headers.
			if (bv != null)
				m_sortItemProvider = bv.SortItemProvider;
			m_xbvvc = Vc;
			string sDefaultCursor = XmlUtils.GetOptionalAttributeValue(nodeSpec,
				"defaultCursor", null);
			// Set a default cursor for a ReadOnly view, if none is given.
			if (sDefaultCursor == null && ReadOnlySelect)
				sDefaultCursor = "Arrow";
			if (sDefaultCursor != null)
			{
				switch (sDefaultCursor)
				{
					case "IBeam":
						EditingHelper.DefaultCursor = Cursors.IBeam;
						break;
					case "Hand":
						EditingHelper.DefaultCursor = Cursors.Hand;
						break;
					case "Arrow":
						EditingHelper.DefaultCursor = Cursors.Arrow;
						break;
					case "Cross":
						EditingHelper.DefaultCursor = Cursors.Cross;
						break;
				}
			}
		}

		/// <summary>
		/// If the text for pasting is too long, truncate it and warn the user.
		/// </summary>
		void OnPasteFixTssEvent(EditingHelper sender, FwPasteFixTssEventArgs e)
		{
			if (!sender.Editable)
				return;
			TextSelInfo tsi = e.TextSelInfo;
			if (tsi == null)
				return;
			int flid = tsi.TagAnchor;
			if (flid == 0)
				return;
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				if (m_bv != null && !m_bv.IsDisposed && m_bv.SpecialCache != null)
					m_bv.SpecialCache.RemoveNotification(this);
			}

			base.Dispose(disposing);

			if( disposing )
			{
				if (components != null)
				{
					components.Dispose();
				}
				if (m_bv != null && !m_bv.IsDisposed)
					m_bv.Dispose();
			}
			m_xbvvc = null;
			m_nodeSpec = null;
			m_stringTable = null;
			m_bv = null;
		}

		#endregion Construction, Initialization and disposal

		#region Other methods

		/// <summary>
		/// Get from the specified node a list of strings, as used in filter bar and bulk edit bar,
		/// where the node is expected to have a [stringList] first child element which is interpreted
		/// in the context of our string table.
		/// </summary>
		/// <param name="spec"></param>
		/// <returns></returns>
		internal string[] GetStringList(XmlNode spec)
		{
			if (StringTbl == null)
				return null;
			XmlNode stringList = XmlUtils.GetFirstNonCommentChild(spec);
			if (stringList == null || stringList.Name != "stringList")
				return null;
			return StringTbl.GetStringsFromStringListNode(stringList);
		}

		/// <summary>
		/// a look up table for getting the correct version of strings that the user will see.
		/// </summary>
		public virtual bool ColumnSortedFromEnd(int icol)
		{
			CheckDisposed();

			return m_bv.ColumnActiveAndSortedFromEnd(icol);
		}

		// /// <summary>
		// /// Called when something has changed the ColumnSpecs of the vc. No longer used.
		// /// </summary>
		//internal void UpdateColumnList()
		//{
		//    CheckDisposed();

		//    RootBox.Reconstruct();
		//}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the value of the AutoScroll property. When we're part of a root site
		/// group and we're not the scrolling controller, then setting this property is
		/// ignored.
		/// </summary>
		/// <value></value>
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

				base.AutoScroll = value;
			}
		}

		/// <summary>
		/// The scroll range can't be stored in the AutoScrollMinSize like we'd like,
		/// because setting that turns on the view's own scroll bar.
		///
		/// This is the requested maximum range of the scrollbar.
		/// MSDN says of AutoScrollMinSize "A Size that determines the minimum size of the virtual area through which the user can scroll."
		/// </summary>
		public override Size ScrollMinSize
		{
			set
			{
				CheckDisposed();
				Debug.Assert(!IsVertical, "Unexpected vertical XmlBrowseViewBase");

				if (m_bv != null && m_bv.ScrollBar != null)
					m_bv.ScrollBar.Maximum = value.Height;

				// A new rootbox height will lead to a new MeanRowHeight, so ScrollBar.LargeChange and .SmallChange need
				// to be updated.
				SetScrollBarParameters(m_bv.ScrollBar);
			}
		}

		/// <summary>
		/// Zero if no rows or null RootBox.
		/// </summary>
		public int MeanRowHeight
		{
			get
			{
				CheckDisposed();
				if (RootBox == null)
					return 0;
				if (RowCount == 0)
					return 0;
				return RootBox.Height / RowCount;
			}
		}

		/// <summary>
		/// The amount of content that the user can scroll through within the content display area.
		/// This height, the maximum value that a user can reach through the UI, is different than the ScrollBar.Maximum
		/// amount (see http://msdn.microsoft.com/en-us/library/vstudio/system.windows.forms.scrollbar.maximum).
		/// Upper boundary on ScrollPosition.
		/// </summary>
		public int ScrollPositionMaxUserReachable
		{
			get
			{
				CheckDisposed();
				Debug.Assert(!IsVertical, "Unexpected vertical XmlBrowseViewBase");
				int contentHeight = RootBox.Height;
				int desiredMaxUserReachable = contentHeight - ClientHeight;
				if (desiredMaxUserReachable < 0)
					desiredMaxUserReachable = 0;
				return desiredMaxUserReachable;
			}
		}

		private Point m_ScrollPosition = new Point(0,0);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Because we turn AutoScroll off to suppress the scroll bars, we need our own
		/// private representation of the actual scroll position.
		/// The setter has to behave in the same bizarre way as AutoScrollPosition,
		/// that setting it to (x,y) results in the new value being (-x, -y).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override Point ScrollPosition
		{
			get
			{
				CheckDisposed();
				return m_ScrollPosition;
			}

			set
			{
				CheckDisposed();

				Debug.Assert(!IsVertical, "Unexpected vertical XmlBrowseViewBase");

				int newValue = value.Y;

				int minValue = 0;
				int maxValue = ScrollPositionMaxUserReachable;

				if (newValue < minValue)
					newValue = minValue;

				// Don't scroll so far down so you can't see any rows.
				if (newValue > maxValue)
					newValue = maxValue;

				m_ScrollPosition.Y = -newValue;
				Debug.Assert(m_bv.ScrollBar.Maximum >= 0, "ScrollBar.Maximum is unexpectedly a negative value");
				// The assignment to 'Value' can (and was LT-3091) throw an exception
				Debug.Assert(m_bv.ScrollBar.Minimum <= minValue, "minValue setting could allow attempt to set out of bounds");
				Debug.Assert(m_bv.ScrollBar.Maximum >= maxValue, "maxValue setting could allow attempt to set out of bounds");
				// to minimise recursive calls, don't set the scroll bar unless it's wrong.
				if (m_bv.ScrollBar.Value != newValue)
					m_bv.ScrollBar.Value = newValue;

				// Achieve the scroll by just invalidating. We'd like to optimize this sometime...
				Invalidate();
			}
		}

		/// <returns>
		/// Desired scrollbar LargeChange value
		/// </returns>
		public int DesiredScrollBarLargeChange
		{
			get
			{
				CheckDisposed();
				var desiredLargeChange = ClientHeight - MeanRowHeight;
				// Two reasons to make this 1 rather than 0:
				// 1. Don't want a click in the large change area to produce no change at all.
				// 2. scroll range gets set to ScrollPositionMaxUserReachable + (our return value) - 1.
				// Scroll range should never be LESS than ScrollPositionMaxUserReachable
				// (See asserts in ScrollPosition setter.)
				// This typically only happens during premature window layout while ClientHeight is very small.
				// But a zero here can trigger asserts and possibly crashes (LT-14544)
				if (desiredLargeChange <= 0)
					desiredLargeChange = 1;
				return desiredLargeChange;
			}
		}

		/// <summary>
		/// Set a controlling scrollbar's parameters using information this object owns.
		/// </summary>
		public void SetScrollBarParameters(ScrollBar scrollBar)
		{
			CheckDisposed();
			scrollBar.Minimum = 0;
			scrollBar.SmallChange = MeanRowHeight;
			scrollBar.LargeChange = DesiredScrollBarLargeChange;
		}

		/// <summary>
		/// True if the control is being scrolled and should have its ScrollMinSize
		/// adjusted and its AutoScrollPosition modified. An XmlBrowseViewBase
		/// scrolls, but using a separate scroll bar.
		/// </summary>
		public override bool DoingScrolling
		{
			get
			{
				CheckDisposed();
				return true;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether automatic vertical scrolling to show the selection should
		/// occur. XmlBrowseViews want this behavior, even though they don't have an auto
		/// scroll bar.
		/// </summary>
		/// <returns></returns>
		/// -----------------------------------------------------------------------------------
		protected override bool DoAutoVScroll
		{
			get { return true; }
		}

		internal ISilDataAccessManaged SpecialCache
		{
			get { return m_sda; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when [prepare to refresh].
		/// </summary>
		/// <param name="args">The args.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool OnPrepareToRefresh(object args)
		{
			CheckDisposed();

			OnSaveScrollPosition(args);
			return false; // other things may wish to prepare too.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new selection restorer.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override SelectionRestorer CreateSelectionRestorer()
		{
			return new XmlBrowseViewSelectionRestorer(this);
		}

		/// <summary>
		/// Called through mediator by reflection.
		/// Save the current scroll position for later restoration, in a form that will survive
		/// having the view contents replaced by a lazy box (that is, it's not good enough to
		/// just save AutoScrollPosition.y, we need enough information to create a selection
		/// at the top of the screen and get a corresponding selection back there.
		/// This class implements this by figuring out the index of the record at the top of
		/// the screen and saving that.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		public bool OnSaveScrollPosition(object args)
		{
			CheckDisposed();

			IVwSelection sel = null;
			if (!IsHandleCreated || RootBox == null)
			{
				// JohnT: really nasty things can happen if we create the root box as part of
				// HoldGraphics here (see for example LT-2100). And there's no need: if we
				// haven't even made our root box we can't have a meaningful scroll position to
				// save.
				m_iTopOfScreenObjectForScrollPosition = -1; // in case we can't figure one.
				return false;
			}
			try
			{
				using (new HoldGraphics(this))
				{
					Rectangle rcSrcRoot;
					Rectangle rcDstRoot;
					GetCoordRects(out rcSrcRoot, out rcDstRoot);
					sel = this.RootBox.MakeSelAt(1, 0, rcSrcRoot, rcDstRoot, false);
					m_iTopOfScreenObjectForScrollPosition = -1; // in case we can't figure one.
					if (sel == null)
						return false;
					// This gets us the index of the object at the top of the screen in the list of
					// objects we are browsing.
					int hvoObj, tag, cpropPrevious; // dummies
					IVwPropertyStore vps; // dummy
					sel.PropInfo(false, sel.CLevels(false) - 1, out hvoObj, out tag,
						out m_iTopOfScreenObjectForScrollPosition, out cpropPrevious, out vps);
					// Get a selection of that whole object. This is just in case there might be a pixel or two difference
					// between the top of an IP and the top of the rectangle that encloses the whole object.
					SelLevInfo[] rgvsli = new SelLevInfo[1];
					rgvsli[0].ihvo = m_iTopOfScreenObjectForScrollPosition;
					rgvsli[0].tag = m_fakeFlid;
					sel = RootBox.MakeTextSelInObj(0, 1, rgvsli, 0, null, false, false, false, true, false);
					if (sel == null)
					{
						m_iTopOfScreenObjectForScrollPosition = -1; // in case we can't figure one.
						return false;
					}

					//sel = RootBox.MakeSelInObj(0, 1, rgvsli, 0, false);
					// Get its position, specifically, we save the distance from the top of the client area to the top of the object.
					// Often this will be negative because the top of the TOS object is just above the top of the client area.
					Rect rcPrimary, rcSec;
					bool fSplit, fEndBeforeAnchor;
					sel.Location(m_graphicsManager.VwGraphics, rcSrcRoot, rcDstRoot, out rcPrimary, out rcSec, out fSplit, out fEndBeforeAnchor);
					m_dyTopOfScreenOffset = rcPrimary.top;
				}
			}
			catch
			{
				m_iTopOfScreenObjectForScrollPosition = -1; // in case we can't figure one.
				return false;
			}
			return true; // indicates success
		}

		/// <summary>
		/// Called through mediator by reflection. (Maybe?)
		/// This routine attempts to restore the scroll position previously saved by OnSaveScrollPosition.
		/// Specifically, it attempts to scroll to a position such that the top of the object at index
		/// m_iTopOfScreenObjectForScrollPosition is m_dyTopOfScreenOffset pixels below the top of the
		/// client area (or above, if m_dyTopOfScreenOffset is negative).
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		public bool OnRestoreScrollPosition(object args)
		{
			CheckDisposed();

			return RestoreScrollPosition(m_iTopOfScreenObjectForScrollPosition);
		}

		/// <summary>
		/// Called by BulkEditBar if Preview makes the highlighted row scroll off the screen.
		/// It attempts to scroll to a position such that the top of the object at index
		/// irow is m_dyTopOfScreenOffset pixels below the top of the
		/// client area (or above, if m_dyTopOfScreenOffset is negative).
		/// </summary>
		/// <param name="irow">index of highlighted row</param>
		/// <returns></returns>
		internal bool RestoreScrollPosition(int irow)
		{
			if (irow < 0 ||
				irow > m_sda.get_VecSize(m_hvoRoot, m_fakeFlid))
			{
				// we weren't able to save a scroll position for some reason, or the position we saved is
				// out of range following whatever changed, so we can't restore.
				return false;
			}
			// Get a selection of the whole target object. Do this OUTSIDE the HoldGraphics/GetCoordRects block,
			// since it may change the scroll position as it expands lazy boxes, modifying the dest rect.
			try
			{
				SelLevInfo[] rgvsli = new SelLevInfo[1];
				rgvsli[0].ihvo = irow;
				rgvsli[0].tag = m_fakeFlid;
				IVwSelection sel = RootBox.MakeTextSelInObj(0, 1, rgvsli, 0, null, false, false, false, true, false);
				if (sel == null)
				{
					// Just ignore it if we couldn't make a selection.
					Debug.WriteLine("restore scroll position failed");
					return true;
				}
				using (new HoldGraphics(this))
				{
					Rectangle rcSrcRoot;
					Rectangle rcDstRoot;
					GetCoordRects(out rcSrcRoot, out rcDstRoot);
					// Get its position, specifically, we save the distance from the top of the client area to the top of the object.
					// Often this will be negative because the top of the TOS object is just above the top of the client area.
					Rect rcPrimary, rcSec;
					bool fSplit, fEndBeforeAnchor;
					sel.Location(m_graphicsManager.VwGraphics, rcSrcRoot, rcDstRoot, out rcPrimary, out rcSec, out fSplit, out fEndBeforeAnchor);
					int dyCurrentOffset = rcPrimary.top;
					int scrollDistance = dyCurrentOffset - m_dyTopOfScreenOffset; // positive means move window contents down.
					Point currentAsp = ScrollPosition; // negative!!
					ScrollPosition = new Point(-currentAsp.X, -currentAsp.Y + scrollDistance);
				}
			}
			catch
			{
				// Just ignore it if we couldn't make a selection or something else goes wrong.
				Debug.WriteLine("restore scroll position failed");
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the corresponding property.
		/// </summary>
		/// <param name="property">The property.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public string GetCorrespondingPropertyName (string property)
		{
			CheckDisposed();

			return m_id + "_" +property;
		}

		/// <summary>
		/// Cause the behavior to switch to the current setting of ReadOnlyBrowse.
		/// Override if the behaivor should be different than this.
		/// </summary>
		public virtual void SetSelectedRowHighlighting()
		{
			CheckDisposed();

			switch (XmlUtils.GetAttributeValue(m_nodeSpec, "selectionStyle", null))
			{
				case "all":
					SelectedRowHighlighting = SelectionHighlighting.all;
					break;
				case "border":
					SelectedRowHighlighting = SelectionHighlighting.border;
					break;
				case "none":
					SelectedRowHighlighting = SelectionHighlighting.none;
					break;
				default:
					SelectedRowHighlighting = ReadOnlySelect ? SelectionHighlighting.all : SelectionHighlighting.border;
					break;
			}
		}

		/// <summary>
		/// Get the HVO of the object at the specified index.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public int HvoAt(int index)
		{
			CheckDisposed();

			return m_sda.get_VecItem(m_hvoRoot, m_fakeFlid, index);
		}

		internal Rectangle LocationOfSelectedRow()
		{
			IVwSelection sel = GetRowSelection(m_selectedIndex);
			using (HoldGraphics hg = new HoldGraphics(this))
			{
				Rect rcPrimary;
				Rect rcSecondary;
				bool fSplit;
				Rectangle rcSrcRoot;
				Rectangle rcDstRoot;
				GetCoordRects(out rcSrcRoot, out rcDstRoot);
				bool fEndBeforeAnchor;

				sel.Location(m_graphicsManager.VwGraphics, rcSrcRoot, rcDstRoot, out rcPrimary, out rcSecondary,
					out fSplit, out fEndBeforeAnchor);

				return new Rectangle(rcPrimary.left, rcPrimary.top, rcPrimary.right - rcPrimary.left, rcPrimary.bottom - rcPrimary.top);
			}
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the row selection.
		/// </summary>
		/// <param name="index">The index.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected IVwSelection GetRowSelection(int index)
		{
			//if (m_rootb.Selection != null)
			//{
			//	  // Collect all the information we can about the selection.
			//	  int ihvoRoot0 = 0;
			//	  int tagTextProp0 = 0;
			//	  int cpropPrevious0 = 0;
			//	  int ichAnchor0 = 0;
			//	  int ichEnd0 = 0;
			//	  int ws0 = 0;
			//	  bool fAssocPrev0 = false;
			//	  int ihvoEnd0 = 0;
			//	  ITsTextProps ttpBogus0 = null;
			//	  SelLevInfo[] rgvsli0 = new SelLevInfo[0];
			//	  int cvsli0 = m_rootb.Selection.CLevels(false) - 1;
			//	  // Main array of information retrived from sel that made combo.
			//	  rgvsli0 = SelLevInfo.AllTextSelInfo(m_rootb.Selection, cvsli0,
			//		  out ihvoRoot0, out tagTextProp0, out cpropPrevious0, out ichAnchor0, out ichEnd0,
			//		  out ws0, out fAssocPrev0, out ihvoEnd0, out ttpBogus0);
			//	  if (ihvoRoot0 == index)
			//		  return m_rootb.Selection;
			//}
			SelLevInfo[] rgvsli = new SelLevInfo[1];
			rgvsli[0].ihvo = index;
			rgvsli[0].cpropPrevious = 0;
			rgvsli[0].tag = m_fakeFlid;
			IVwSelection selRow = null;
			try
			{
				selRow = m_rootb.MakeTextSelInObj(0, 1, rgvsli, 0, null,
					false, false, false, true, false);
			}
			catch
			{
				// This can happen if the only columns specified are custom fields which have
				// been deleted. We don't want to crash -- see LT-6449.
			}
			return selRow;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Does the select and scroll.
		/// </summary>
		/// <param name="hvo">The hvo.</param>
		/// <param name="index">The index.</param>
		/// ------------------------------------------------------------------------------------
		protected virtual void DoSelectAndScroll(int hvo, int index)
		{
			m_iSelIndex = 0;
			m_ydSelBottom = 0;
			m_ydSelScrollPos = 0;
			m_ydSelTop = 0;
			if (m_rootb == null)
				return;
			IVwSelection selRow = GetRowSelection(index);
			if (selRow != null)
				MakeSelectionVisible(selRow, true, true, true);
			// Make default insertion point (MouseDown already does it's own).
			if (!m_fHandlingMouseUp)
			{
				IVwSelection vwSel = m_rootb.Selection;
				bool fWantNewIp = true;
				if (vwSel != null)
				{
					try
					{
						// If selection is already on the right row don't move it; might interfere with editing.
						// Sometimes (e.g., in Words RDE) we get RecordNavigation during editing.
						int clev = vwSel.CLevels(false); // anchor
						int hvoRoot, tag, ihvo, cpropPrevious;
						IVwPropertyStore vps;
						// The first argument below was true, which seems wrong.  See LT-8506, which
						// tends to prove that the argument must match that used in vwSel.CLevels() above.
						vwSel.PropInfo(false, clev - 1, out hvoRoot, out tag, out ihvo, out cpropPrevious, out vps);

						// If it's multi-line also don't change it.
						clev = vwSel.CLevels(true); // end
						int ihvoEnd;
						vwSel.PropInfo(true, clev - 1, out hvoRoot, out tag, out ihvoEnd, out cpropPrevious, out vps);
						fWantNewIp = (ihvo == ihvoEnd && ihvo != SelectedIndex);
					}
					catch
					{
						fWantNewIp = true;
					}
				}

				if (fWantNewIp)
					SetDefaultInsertionPointInRow(index);
			}
			// This causes problems, e.g., LT-3746, when something calls this and the index
			// is not really changing. It can move focus from the detail pane to the browse
			// pane undesirably. Usually, we move it back as we change record and make an
			// initial selection in the detail pane, but if the selection isn't changing
			// the detail pane doesn't get updated. So leave the decision to the caller.
			//this.Focus();
		}

		/// <summary>
		/// Set an insertion point somewhere in the given row,
		/// and install the selection if it exists.
		/// </summary>
		/// <param name="index">record index</param>
		/// <returns>true if selection was installed.</returns>
		private bool SetDefaultInsertionPointInRow(int index)
		{
			SelLevInfo[] rgvsli = new SelLevInfo[1];
			rgvsli[0].ihvo = index;
			rgvsli[0].cpropPrevious = 0;
			rgvsli[0].tag = m_fakeFlid;
			IVwSelection vwselNew = null;
			bool isEditable = XmlUtils.GetOptionalBooleanAttributeValue(m_nodeSpec, "editable", true);
			bool fInstalledNewSelection = false;
			if (isEditable) //hack to get around bug XW-38
			{
				try
				{
					// Try to get an IP in an editable area.
					vwselNew = m_rootb.MakeTextSelInObj(0,
						1, rgvsli, 0, null,	//1, rgvsli,
						true, // fInitial
						true, // fEdit
						false, // fRange
						false, // fWholeObj
						false); // fInstall
					// if we find an editable selection, make sure it's in the same record.
					if (vwselNew != null)
					{
						int clev = vwselNew.CLevels(false); // anchor
						int hvoRoot, tag, ihvo, cpropPrevious;
						IVwPropertyStore vps;
						vwselNew.PropInfo(false, clev - 1, out hvoRoot, out tag, out ihvo, out cpropPrevious, out vps);
						if (ihvo == index)
						{
							// install the selection this time.
							vwselNew.Install();
							fInstalledNewSelection = true;
						}
					}
				}
				catch (ObjectDisposedException ode)
				{
					throw ode; // We should die, if we get into this one.
				}
				catch
				{
					fInstalledNewSelection = false;
				}
			}
			if (!fInstalledNewSelection)
			{
				// Try something else.
				vwselNew = m_rootb.MakeTextSelInObj(0,
					1, rgvsli, 0, null,	//1, rgvsli,
					true, // fInitial
					false, // fEdit
					false, // fRange
					false, // fWholeObj
					true); // fInstall
				fInstalledNewSelection = true;
				if (vwselNew == null)
				{
					// not much we can do to handle errors, but don't let the program die just
					// because the display hasn't yet been laid out, so selections can't fully be
					// created and displayed.
					fInstalledNewSelection = false;
					Debug.WriteLine("XmlBrowseViewBase::SetDefaultInsertionPointInRow: Caught exception while trying to scroll a non-editable object into view.");
				}
			}
			if (vwselNew != null && fInstalledNewSelection)
				MakeSelectionVisible(vwselNew, true, true, true);
			return fInstalledNewSelection;
		}

		/// <summary>
		/// Get the widths of the columns as VwLengths (for the view tables).
		/// </summary>
		public virtual VwLength[] GetColWidthInfo()
		{
			CheckDisposed();

			int[] widths; // not wanted here.
			VwLength[] rglength;
			m_bv.GetColWidthInfo(out rglength, out widths);
			return rglength;
		}

		/// <summary>
		/// Can't find a .NET definition of this constant. One 'notch' on a mouse wheel is supposed to produce this
		/// value for MouseEventArgs.Delta. Supposedly it might change in future versions.
		/// </summary>
		const int WHEEL_DATA = 120;
		/// <summary>
		/// See if we can interpret a mouse wheel movement in the XmlBrowse view as a vertical
		/// scroll in the separate scroll bar maintained by the browse viewer to scroll this.
		/// Return true if the event was reinterpreted as a vertical scroll.
		/// </summary>
		/// <param name="e"></param>
		/// <returns></returns>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "scrollBar is a reference")]
		private bool DoMouseWheelVScroll(MouseEventArgs e)
		{
			if (m_bv == null)
				return false;
			ScrollBar scrollBar = m_bv.ScrollBar;
			if (scrollBar == null)
				return false;
			if (scrollBar.Maximum < scrollBar.LargeChange)
				return false;
			// Supposedly an e.Delta of one WHEEL_DATA is a unit of movement, and MouseWheelScrollLines tells how many lines
			// to scroll for one unit (or one rotation?? doc isn't clear), and SmallChange tells how far one line is...
			int newVal = scrollBar.Value
				- e.Delta * System.Windows.Forms.SystemInformation.MouseWheelScrollLines * scrollBar.SmallChange / WHEEL_DATA;
			newVal = Math.Max(0, newVal);
			newVal = Math.Min(newVal, scrollBar.Maximum - scrollBar.LargeChange);
			scrollBar.Value = newVal;
			return true;
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
			if (DoMouseWheelVScroll(e))
			{
				// Note: the return here does not seem to have any effect in suppressing horizontal
				// scrolling by the mouse wheel. See the version of OnMouseWheel in BrowseViewScroller
				// for the code which succeeds in doing that.
				// Enhance JohnT: might it work (and make things clearer) to do all the mouse wheel
				// processing in that class?
				return; // skip the normal processing, which tends to scroll horizontally.
			}
			base.OnMouseWheel(e);
		}

		#endregion Other methods

		#region Overrides of RootSite
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make the root box.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void MakeRoot()
		{
			CheckDisposed();

			if (m_fdoCache == null || DesignMode)
				return;

			m_rootb = VwRootBoxClass.Create();
			m_rootb.SetSite(this);
			// Only change it if it is null or different.
			// Otherwise, it does an uneeded disposal/creation of the layout cache.
			if (m_xbvvc.Cache == null || m_xbvvc.Cache != m_fdoCache)
				m_xbvvc.Cache = m_fdoCache;
			SetSelectedRowHighlighting();
			this.ReadOnlyView = this.ReadOnlySelect;

			// This is where the 'Decorator' SDA is added.
			// This SDA can handle fake stuff, if the SDA overrides are all implemented properly.
			m_sda = m_bv.SpecialCache;
			m_rootb.DataAccess = m_sda;

			RootObjectHvo = m_hvoRoot;
			base.MakeRoot();
			m_bv.SpecialCache.AddNotification(this);
			m_dxdLayoutWidth = kForceLayout; // Don't try to draw until we get OnSize and do layout.
			// Filter bar uses info from our VC and can't fininish init until we make it.
			if (m_bv.FilterBar != null)
				m_bv.FilterBar.MakeItems();
			// Simulate a column drag to get the columns resized.
			// No good, we can't do this until we've had one layout call.
			//m_bv.ColumnDragged();
			//TODO:
			//ptmw->RegisterRootBox(qrootb);
			if (RootSiteEditingHelper != null)
			{
				RootSiteEditingHelper.PasteFixTssEvent -= new FwPasteFixTssEventHandler(OnPasteFixTssEvent);
				RootSiteEditingHelper.PasteFixTssEvent += new FwPasteFixTssEventHandler(OnPasteFixTssEvent);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overridden to fix TE-4146
		/// </summary>
		/// <param name="levent"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnLayout(LayoutEventArgs levent)
		{
			int oldWidth = m_dxdLayoutWidth;
			base.OnLayout(levent);
			// If being laid out for the first time, synchronize column widths.
			if (m_dxdLayoutWidth > 0 && m_dxdLayoutWidth != oldWidth)
			{
				m_bv.AdjustColumnWidths(false);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Call Draw() which does all the real painting
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnPaint(PaintEventArgs e)
		{
			// let the VC know when we're actually doing an OnPaint().
			m_xbvvc.InOnPaint = true;
			base.OnPaint(e);
			m_xbvvc.InOnPaint = false;
		}

		/// <summary>
		/// Notifies us that the selection changed. When next idle, we want to delete any unhelpful
		/// selection.
		/// </summary>
		/// <param name="prootb"></param>
		/// <param name="vwselNew"></param>
		protected override void HandleSelectionChange(IVwRootBox prootb, IVwSelection vwselNew)
		{
			base.HandleSelectionChange(prootb, vwselNew);
			m_mediator.IdleQueue.Add(IdleQueuePriority.Medium, RemoveRootBoxSelectionOnIdle);
		}

		bool RemoveRootBoxSelectionOnIdle(object parameter)
		{
			if (IsDisposed || m_rootb == null)
				return true;

			// This is a good time to check that we don't have a useless IP selection.
			// Right after we make it is too soon, because the current row's editing properties
			// aren't set until we paint it.
			IVwSelection sel = m_rootb.Selection;
			if (sel != null && !sel.IsRange)
			{
				// an insertion point where you can't edit is just confusing.
				// Also, sometimes, trying to make an editable one we end up with one on another row.
				// We don't want that either.
				int idxFromSel = GetRowIndexFromSelection(sel, true);
				if (m_fSelectedRowHighlighting != SelectionHighlighting.none && idxFromSel != m_selectedIndex)
				{
					m_rootb.DestroySelection();
				}
				else if (!sel.IsEditable)
				{
					m_rootb.DestroySelection();
					if (idxFromSel == m_selectedIndex)
						SetDefaultInsertionPointInRow(idxFromSel);
				}
			}
			return true;
		}

		/// <summary>
		/// Tries to be smart about whether our RootSite should print or not.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		public override bool OnPrint(object args)
		{
			CheckDisposed();

			object tool = m_mediator.PropertyTable.GetValue("currentContentControlObject", null);
			if (tool != null && tool is MultiPane)
			{
				// We want to print only if this tool is selected for printing, or if nothing has been selected.
				// or if no default has been specified.
				if ((tool as MultiPane).PrintPane == m_id ||
					(tool as MultiPane).PrintPane == "")
				{
					return base.OnPrint(args);
				}
				else
				{
					// allow the specified default RootSite to Print.
					return false;
				}
			}
			else
			{
				// It's not a MultiPane, so we should just have one RootSite to Print.
				return base.OnPrint(args);
			}
		}

		/// <remarks>
		/// The ScrollBar.Maximum is equal to the maxUserReachable + ScrollBar.LargeChange - 1, which
		/// in this implementation is also equal to RootBox.Height - MeanRowHeight - 1 in cases
		/// where RootBox.Height > ClientHeight.
		///
		/// Because ScrollBar.LargeChange.get is partly bounded by ScrollBar.Maximum, we can't reliably set and use
		/// ScrollBar.LargeChange before setting ScrollBar.Maximum. But we can know the value of what ScrollBar.LargeChange
		/// should be from DesiredScrollBarLargeChange.
		/// </remarks>
		protected override Size ScrollRange
		{
			get
			{
				int desiredMaxUserReachable = ScrollPositionMaxUserReachable;
				// http://msdn.microsoft.com/en-us/library/vstudio/system.windows.forms.scrollbar.maximum
				int desiredMaxScrollbarHeight = desiredMaxUserReachable + DesiredScrollBarLargeChange - 1;
				return new Size(base.Width, desiredMaxScrollbarHeight);
			}
		}
		#endregion

		#region XCore Colleague overrides

		/// <summary>
		///	see if it makes sense to provide the "delete record" command now
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayDeleteRecord(object commandObject,
			ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when [delete record].
		/// </summary>
		/// <param name="commandObject">The command object.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public virtual bool OnDeleteRecord(object commandObject)
		{
			CheckDisposed();

			return false;
		}


		/// <summary>
		/// Allows xCore-specific initialization. We don't need any.
		/// </summary>
		/// <param name="mediator"></param>
		/// <param name="configurationParameters"></param>
		public override void Init(XCore.Mediator mediator, System.Xml.XmlNode configurationParameters)
		{
			CheckDisposed();

			// Do this early...we need the ID to restore the columns when the VC is created.
			m_id = XmlUtils.GetOptionalAttributeValue(configurationParameters, "id", "NeedsId");

			base.Init(mediator, configurationParameters);
			// The call to the superclass method ignores "configurationParameters",
			// so set it here if it hasn't already been done.
			if (m_nodeSpec == null)
				m_nodeSpec = configurationParameters;
			Debug.Assert(m_nodeSpec == configurationParameters, "XmlBrowseViewBase.Init (XCore version): Mis-matched configuration parameters.");

			SetSelectedRowHighlighting();//read the property table
		}

		#endregion XCore Colleague overrides

		#region Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			components = new System.ComponentModel.Container();
		}
		#endregion

		/// <summary>
		/// Save the location information for the current selection being made visible.
		/// </summary>
		/// <param name="rcIdeal">rectangle of selection</param>
		/// <param name="ydTop">current scroll position</param>
		protected override void SaveSelectionInfo(Rectangle rcIdeal, int ydTop)
		{
			m_ydSelTop = rcIdeal.Top;
			m_ydSelBottom = rcIdeal.Bottom;
			// The other two values are for validation.
			m_iSelIndex = SelectedIndex;
			m_ydSelScrollPos = ydTop;
		}

		#region IVwNotifyChange Members

		ICmObjectRepository m_repo;
		/// <summary>
		///
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="ivMin"></param>
		/// <param name="cvIns"></param>
		/// <param name="cvDel"></param>
		public void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			if (tag == MainTag)
			{
				// something has changed in the list we're displaying. make sure
				// our checkboxes are up to date, before we process OnPaint
				if (Vc != null && Vc.HasSelectColumn)
				{
					// there can be a circular dependency between checked items in a browse viewer
					// and the record list managing that list.
					// The record list can depend upon BulkEdit settings for loading its list
					// (e.g. ListItemsClass), so the bulk edit bar must load before the RecordList.
					// But, the BrowseViewer and BulkEditBar (Delete tab) also needs to know when
					// RecordList has been loaded, so that they can manage checkbox behavior for
					// the actual items being displayed.
					if (m_bv != null)
						m_bv.UpdateCheckedItems();
				}
				if (SelectedObject != m_hvoOldSel)
				{
					// The selected object has changed even though the index didn't, e.g., because we
					// changed the sorting of the list while leaving the selected index fixed.
					// We need to fire the notification saying it changed, anyway.
					// (But don't update m_hvoOldSelection; FireSelectionChanged must find the old one to register a change.)
					m_mediator.IdleQueue.Add(IdleQueuePriority.Medium, FireSelectionChanged);
				}
			}
			else if (RootBox != null && hvo > 0 && SelectedObject > 0)
			{
				// Check whether the changed object is either the selected object or owned
				// by the selected object.  If so, do a fake PropChanged that causes the whole
				// row to be regenerated. This ensures updating of virtual properties of the
				// object in other columns (e.g., when user opinion of this object changes,
				// the count of approved analyses should change).
				// This might become unnecessary if we can find a better way to generate
				// PropChanged when virtuals change.
				// See FWR-661.
				if (m_repo == null)
					m_repo = this.Cache.ServiceLocator.GetInstance<ICmObjectRepository>();
				ICmObject objSel;
				if (!m_repo.TryGetObject(SelectedObject, out objSel))
					return;
				ICmObject obj;
				if (!m_repo.TryGetObject(hvo, out obj))
					return;
				if (obj == objSel || obj.OwnerOfClass(objSel.ClassID) == objSel)
				{
					// Reconstruct the current row (by pretending to replace the object),
					// preserving the selection if any (otherwise, the selection disappears
					// after each letter typed in a browse view...FWR-690).
					m_mediator.IdleQueue.Add(IdleQueuePriority.Medium, UpdateSelectedRow);
				}
			}
		}

		private bool UpdateSelectedRow(object args)
		{
			if (IsDisposed || m_rootb == null)
				return true; // presumably we've been disposed; this happens (at least) in tests where a later test may simulate idle events.
			if (SelectedIndex < 0)
				return true; // no current row to update
			SelectionHelper helper = null;
			if (RootBox.Selection != null)
				helper = SelectionHelper.Create(this);
			RootBox.PropChanged(m_hvoRoot, MainTag, SelectedIndex, 1, 1);
			if (helper != null)
				helper.MakeBest(false);
			return true; // we did it.
		}

		#endregion

		/// <summary>
		/// After we have our true size, make sure the interesting row is visible.
		/// </summary>
		public void PostLayoutInit()
		{
			if (m_rootb == null || SelectedIndex < 0)
				return;
			MakeSelectionVisible(GetRowSelection(SelectedIndex), true, true, true);
		}

		/// <summary>
		/// Clear dangerous data values out of any of your decorated SDAs that require it.
		/// </summary>
		public void ClearValues()
		{
			var sda = m_sda;
			while (sda != null)
			{
				var cv = sda as IClearValues;
				if (cv != null)
					cv.ClearValues();
				var decorator = sda as DomainDataByFlidDecoratorBase;
				if (decorator == null)
					break;
				sda = decorator.BaseSda;
			}
		}
	}

	/// <summary>
	/// This interface indicates a control or other object that may need to clear its values,
	/// typically as one stage of a Refresh that will ultimately restore current values.
	/// Typically this is because the old values are dummy ones that are no longer valid,
	/// and there is danger of an intermediate stage of the Refresh attempting to use them and crashing.
	/// </summary>
	public interface IClearValues
	{
		/// <summary>
		/// Clear values that might otherwise be reused before being fully reset.
		/// </summary>
		void ClearValues();
	}
}
