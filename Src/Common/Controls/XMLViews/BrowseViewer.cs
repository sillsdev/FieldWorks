// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: BrowseViewer.cs
// Responsibility: WordWorks
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Xml;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Text;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.Utils;
using SIL.Utils;
using SIL.FieldWorks.Filters;
using SIL.FieldWorks.Resources;
using XCore;
using SIL.FieldWorks.Common.Controls;
using System.Reflection;
using SIL.FieldWorks.FDO.LangProj;

namespace SIL.FieldWorks.Common.Controls
{

	/// <summary>
	/// This class is the arguments for a ClickCopyEventHandler.
	/// </summary>
	public class CheckBoxChangedEventArgs : EventArgs
	{
		int[] m_hvoChanged;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:CheckBoxChangedEventArgs"/> class.
		/// </summary>
		/// <param name="hvosChanged">The hvos changed.</param>
		public CheckBoxChangedEventArgs(int[] hvosChanged)
		{
			m_hvoChanged = hvosChanged;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the hvos changed.
		/// </summary>
		/// <value>The hvos changed.</value>
		/// ------------------------------------------------------------------------------------
		public int[] HvosChanged
		{
			get { return m_hvoChanged; }
		}
	}

	/// <summary>
	/// This is used for a slice to ask the data tree to display a context menu.
	/// </summary>
	public delegate void CheckBoxChangedEventHandler(object sender, CheckBoxChangedEventArgs e);

	#region ISortItemProvider declaration
	/// <summary>
	/// This interface is used for the BrowseViewer (specifically the Vc) to call back to the
	/// RecordList (which it can't otherwise know about, because of avoiding circular dependencies)
	/// and get the ManyOnePathSortItem for an item it is trying to display.
	/// </summary>
	public interface ISortItemProvider
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sorts the item at.
		/// </summary>
		/// <param name="index">The index.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		ManyOnePathSortItem SortItemAt(int index);
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Appends the items for.
		/// </summary>
		/// <param name="hvo">The hvo.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		int AppendItemsFor(int hvo);
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Removes the items for.
		/// </summary>
		/// <param name="hvo">The hvo.</param>
		/// ------------------------------------------------------------------------------------
		void RemoveItemsFor(int hvo);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the index of the given object, or -1 if it's not in the list.
		/// </summary>
		/// <param name="hvo"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		int IndexOf(int hvo);

		/// <summary>
		/// Class of objects being displayed in this list.
		/// </summary>
		int ListItemsClass { get; }
	}

	#endregion ISortItemProvider declaration

	#region BrowseViewer class
	/// <summary>
	/// BrowseViewer is a container for various windows related to browsing. At a minimum it contains
	/// a DnListView which provides the column headers and an XmlBrowseView that contains the
	/// actual browse view. It may also have a FilterBar, and eventually other controls, e.g.,
	/// for filling in columns of data.
	/// </summary>
	public class BrowseViewer : XCoreUserControl, IxCoreColleague, ISnapSplitPosition, IxCoreContentControl
	{
		/// <summary>
		/// Check state for items (check and uncheck only).
		/// </summary>
		private enum CheckState
		{
			ToggleAll,
			UncheckAll,
			CheckAll
		}

		FdoCache m_cache;
		XmlNode m_nodeSpec;
		private DhListView m_lvHeader;
		private RecordSorter m_sorter;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
		/// <summary></summary>
		protected internal XmlBrowseViewBase m_xbv;
		/// <summary></summary>
		protected internal FilterBar m_filterBar;
		/// <summary></summary>
		protected internal RecordFilter m_currentFilter;
		/// <summary></summary>
		protected internal BulkEditBar m_bulkEditBar;
		internal ISortItemProvider m_sortItemProvider;
		private int m_lastLayoutWidth = 0;

		/// <summary></summary>
		protected int m_icolCurrent = 0;
		bool m_doHScroll = false;
		private Button m_configureButton;
		private Button m_checkMarkButton;
		private BrowseViewScroller m_scrollContainer = null;
		private ScrollBar m_scrollBar;
		private ToolTip m_tooltip;
		private bool m_listModificationInProgress;
		private bool m_fFilterInitializationComplete = false;
		private int[] m_colWidths; // Last values computed and set by AdjustColumnWidths.

		// This flag is used to minimize redoing the filtering and sorting when
		// changing the list of columns shown.
		private bool m_fUpdatingColumnList = false;

		/// <summary></summary>
		protected internal Mediator m_mediator;

		/// <summary></summary>
		public event FilterChangeHandler FilterChanged;

		/// <summary>
		/// Invoked when check box status alters. Typically there is only one item changed
		/// (the one the user clicked on); in this case, client should generate PropChanged as needed
		/// to update the display. When the user does something like CheckAll, a list is sent; in this case,
		/// AFTER invoking the event, the browse view does a Reconstruct, so generating PropChanged is not
		/// necessary (or helpul, unless some other view also needs updating).
		/// </summary>
		public event CheckBoxChangedEventHandler CheckBoxChanged;


		/// <summary>
		/// This event notifies you that the selected object changed, passing an argument from which you can
		/// directly obtain the new object. If you care more about the position of the object in the list
		/// (especially if the list may contain duplicates), you may wish to use the SelectedIndexChanged
		/// event instead. This SelectionChangedEvent will not fire if the selection moves from one
		/// occurrene of an object to another occurrence of the same object.
		/// </summary>
		public event SIL.FieldWorks.Common.Utils.FwSelectionChangedEventHandler SelectionChanged;
		/// <summary>
		/// This event notifies you that a selection was made with a double click, passing an argument
		/// containing both the index and the hvo of the selected object.  This happens currently only
		/// for read-only views.
		/// </summary>
		public event SIL.FieldWorks.Common.Utils.FwSelectionChangedEventHandler SelectionMade;
		/// <summary>
		/// This event notifies you that the selected index changed. You can find the current index from
		/// the SelectedIndex property, and look up the object if needed...but if you mainly care about
		/// the object, it is probably better to use SelectionChangedEvent.
		/// This is very nearly redundant...perhaps it would be better to just add an index to the
		/// SelectionChanged event and fire it if either index or object changes...
		/// </summary>
		public event EventHandler SelectedIndexChanged;

		/// <summary></summary>
		public event EventHandler SorterChanged;
		/// <summary> Target Column can be selected by the BulkEdit bar which may need to reorient the
		/// RecordClerk to build a list based upon another RootObject class.</summary>
		public event TargetColumnChangedHandler TargetColumnChanged;
		/// <summary>
		/// Fired whenever a column width or column order changes
		/// </summary>
		public event EventHandler ColumnsChanged;

		/// <summary></summary>
		public event EventHandler ListModificationInProgressChanged;

		/// <summary></summary>
		public event EventHandler SelectionDrawingFailure;

		/// <summary>
		/// True during a major change that affects the master list of objects.
		/// The current example is deleting multiple objects from the list at once.
		/// This is set true (and the corresponding event raised) at the start of the operation,
		/// and back to false at the end. A client may want to suppress handling PropChanged
		/// during the complex operation, and simply reload the list at the end.
		/// </summary>
		public bool ListModificationInProgress
		{
			get
			{
				CheckDisposed();
				return m_listModificationInProgress;
			}
		}

		/// <summary>
		/// This supports external clients using the Bulk Edit Preview functionality.
		/// To turn on, set to the index of the column that should have the preview
		/// (zero based, not counting the check box column). To turn off, set to -1.
		/// </summary>
		public int PreviewColumn
		{
			get
			{
				int val = Cache.GetIntProperty(RootObjectHvo, XmlBrowseViewVc.ktagActiveColumn);
				if (val <= 0)
					return -1;
				else
					return val - 1;
			}
			set
			{
				using (new ReconstructPreservingBVScrollPosition(this))
				{
					Cache.VwCacheDaAccessor.CacheIntProp(RootObjectHvo, XmlBrowseViewVc.ktagActiveColumn, value + 1);
					if (!m_xbv.Vc.HasPreviewArrow && value >= 0)
						m_xbv.Vc.PreviewArrow = BulkEditBar.PreviewArrowStatic;
				} // Does RootBox.Reconstruct() here
			}
		}

		/// <summary>
		/// This also is part of making bulk edit-type previews available.
		/// For each object that should have a preview alternate, cache a (non-multi) string value
		/// that is the alternate, using this flid. Initialize all the values before setting the PreviewColumn.
		/// (Enhance JohnT: possibly we could provide an event that is fired when we need previews of
		/// a range of columns, triggered by LoadDataFor in the VC.)
		/// </summary>
		public int PreviewValuesTag
		{
			get { return XmlBrowseViewBaseVc.ktagAlternateValue; }
		}

		/// <summary>
		/// Yet another part of enabling preview...must be set for each item that is 'enabled', that is,
		/// it makes sense for it to be changed.
		/// </summary>
		public int PreviewEnabledTag
		{
			get { return XmlBrowseViewBaseVc.ktagItemEnabled; }
		}

		internal void SetListModificationInProgress(bool val)
		{
			CheckDisposed();

			if (m_listModificationInProgress == val)
				return;
			m_listModificationInProgress = val;
			if (ListModificationInProgressChanged != null)
				ListModificationInProgressChanged(this, new EventArgs());
		}

		/// <summary>
		/// Receive a notification that a mouse-up has completed in the browse view.
		/// </summary>
		internal void BrowseViewMouseUp(MouseEventArgs e)
		{
			CheckDisposed();

			if (m_xbv.Vc.HasSelectColumn && e.X < m_xbv.Vc.SelectColumnWidth)
			{
				int[] hvosChanged = new int[] {m_xbv.SelectedObject};
				// we've changed the state of a check box.
				OnCheckBoxChanged(hvosChanged);
			}

			if (m_bulkEditBar != null && m_bulkEditBar.Visible)
				m_bulkEditBar.UpdateEnableItems(m_xbv.SelectedObject);
		}

		private void OnCheckBoxChanged(int[] hvosChanged)
		{
			try
			{
				if (CheckBoxChanged == null)
					return;
				CheckBoxChanged(this, new CheckBoxChangedEventArgs(hvosChanged));
			}
			finally
			{
				// if a check box has changed by user, clear any record of preserving for a mixed class,
				// since we always want to try to stay consistent with the user's choice in checkbox state.
				if (!m_fInUpdateCheckedItems)
				{
					m_lastChangedSelectionListItemsClass = 0;
					OnListItemsAboutToChange(hvosChanged);
				}
			}
		}

		/// <summary>
		/// Gets the inner XmlBrowseViewBase class.
		/// </summary>
		public XmlBrowseViewBase BrowseView
		{
			get
			{
				CheckDisposed();
				return m_xbv;
			}
		}

		/// <summary>
		/// bulk edit bar, if one is installed. <c>null</c> if not.
		/// </summary>
		public BulkEditBar BulkEditBar
		{
			get
			{
				CheckDisposed();
				return m_bulkEditBar;
			}
		}

		internal FdoCache Cache
		{
			get
			{
				CheckDisposed();
				return m_cache;
			}
		}

		internal FilterBar FilterBar
		{
			get
			{
				CheckDisposed();
				return m_filterBar;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the sort item provider.
		/// </summary>
		/// <value>The sort item provider.</value>
		/// ------------------------------------------------------------------------------------
		public ISortItemProvider SortItemProvider
		{
			get
			{
				CheckDisposed();
				return m_sortItemProvider;
			}
		}

		internal uint ListItemsClass
		{
			get { return m_xbv.Vc.ListItemsClass; }
		}

		/// <summary>
		/// flags if the BrowseViewer has already sync'd its filters to the record clerk.
		/// </summary>
		private bool FilterInitializationComplete
		{
			get { return m_fFilterInitializationComplete; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If the view has or might have a filter bar, call this after initialization to
		/// ensure that it is synchronized with the current filter in the Clerk.
		/// </summary>
		/// <param name="currentFilter">The current filter.</param>
		/// ------------------------------------------------------------------------------------
		public void UpdateFilterBar(RecordFilter currentFilter)
		{
			CheckDisposed();

			if (m_filterBar != null)
			{
				m_currentFilter = currentFilter;

				if (!m_fFilterInitializationComplete)
				{
					// If we can append matching columns, then sync to the new list.
					if (this.AppendMatchingHiddenColumns(currentFilter))
					{
						UpdateColumnList();	// adjusts columns and syncs all filters.
						m_fFilterInitializationComplete = true;
						return;
					}
				}
				else if (currentFilter == null)
				{
					// Remove all filters.
					this.OnRemoveFilters(this);
					return;
				}
				// syncs filters to currently shown columns.
				m_filterBar.UpdateActiveItems(currentFilter);
			}
			SetBrowseViewSpellingStatus();
			m_fFilterInitializationComplete = true;
		}

		// Called when filter is initialized or changed, sets desired spelling status.
		private void SetBrowseViewSpellingStatus()
		{
			BrowseView.DoSpellCheck = WantBrowseSpellingStatus();
		}

		/// <summary>
		/// We want to show spelling status if any spelling filters are active.
		/// </summary>
		/// <returns></returns>
		private bool WantBrowseSpellingStatus()
		{
			if (m_currentFilter == null)
				return false;
			if (m_currentFilter is FilterBarCellFilter)
				return (m_currentFilter as FilterBarCellFilter).Matcher is BadSpellingMatcher;
			if (m_currentFilter is AndFilter)
			{
				foreach (object item in (m_currentFilter as AndFilter).Filters)
					if (item is FilterBarCellFilter && (item as FilterBarCellFilter).Matcher is BadSpellingMatcher)
						return true;
			}
			return false;
		}

		/// <summary>
		/// Return the index of the 'selected' row in the main browse view.
		/// Returns -1 if nothing is selected.
		/// If the selection spans multiple rows, returns the anchor row.
		/// If in select-only mode, there may be no selection, but it will then be the row
		/// last clicked.
		/// </summary>
		public int SelectedIndex
		{
			get
			{
				CheckDisposed();
				return m_xbv.SelectedIndex;
			}
			set
			{
				CheckDisposed();
				m_xbv.SelectedIndex = value;
			}
		}

		/// <summary>
		/// Gets the column count. This count does not include the check box column.
		/// </summary>
		/// <value>The column count.</value>
		public int ColumnCount
		{
			get
			{
				CheckDisposed();
				return ColumnSpecs.Count;
			}
		}

		/// <summary>
		/// Gets the name of the specified column. The specified index is zero-based, it should
		/// not include the check box column.
		/// </summary>
		/// <param name="icol">The index of the column.</param>
		/// <returns>The column name</returns>
		public string GetColumnName(int icol)
		{
			return XmlUtils.GetAttributeValue(ColumnSpecs[icol], "label");
		}

		/// <summary>
		/// Gets the default sort column. The returned index is zero-based, it does not include
		/// the check box column.
		/// </summary>
		/// <value>The default sort column.</value>
		public virtual int DefaultSortColumn
		{
			get
			{
				CheckDisposed();
				// the default column is always the first column
				return 0;
			}
		}

		/// <summary>
		/// Gets the sorted columns. The returned indices are zero-based, they do not include
		/// the check box column. Based off of InitSorter()
		/// </summary>
		/// <value>The sorted columns.</value>
		public List<int> SortedColumns
		{
			get
			{
				CheckDisposed();
				List<int> cols = new List<int>();
				if (m_filterBar != null && m_filterBar.ColumnInfo.Length > 0 && m_sorter != null)
				{
					if (m_sorter is AndSorter)
					{
						ArrayList sorters = (m_sorter as AndSorter).Sorters;
						foreach (object sorterObj in sorters)
						{
							int icol = ColumnInfoIndexOfCompatibleSorter(sorterObj as RecordSorter);
							if (icol >= 0)
								cols.Add(icol);
						}
					}
					else
					{
						int icol = ColumnInfoIndexOfCompatibleSorter(m_sorter);
						if (icol >= 0)
							cols.Add(icol);
					}
				}
				return cols;
			}
		}

		/// <summary>
		/// Default constructor is required for some reason I (JohnT) forget, probably to do with
		/// design mode. Don't use it.
		/// </summary>
		public BrowseViewer()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			// TODO: Add any initialization after the InitForm call
			base.AccNameDefault = "BrowseViewer";	// default accessibility name
		}

		/// <summary>
		/// Set/Get the sorter. Setting using this does not raise SorterChanged...it's meant
		/// to be used to initialize it by the client.
		/// </summary>
		public RecordSorter Sorter
		{
			get
			{
				CheckDisposed();
				return m_sorter;
			}
			set
			{
				CheckDisposed();

				if (m_sorter == value)
					return;
				//if (m_sorter != null && (m_sorter is IDisposable))
				//	(m_sorter as IDisposable).Dispose();
				m_sorter = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the sorter and raise the SortChanged event.
		/// </summary>
		/// <param name="sorter">The sorter.</param>
		/// <param name="fTriggerChanged">if set to <c>true</c> [f trigger changed].</param>
		/// ------------------------------------------------------------------------------------
		private void SetAndRaiseSorter(RecordSorter sorter, bool fTriggerChanged)
		{
			SyncSortArrows(sorter);

			this.Sorter = sorter;
			if (fTriggerChanged && SorterChanged != null)
				SorterChanged(this, new EventArgs());
		}

		internal void RaiseSelectionDrawingFailure()
		{
			CheckDisposed();

			if (SelectionDrawingFailure != null)
				SelectionDrawingFailure(this, new EventArgs());
		}

		/// <summary>
		/// Set up all of the sort arrows based on the sorter
		/// </summary>
		/// <param name="sorter">The sorter</param>
		private void SyncSortArrows(RecordSorter sorter)
		{
			ResetSortArrowColumn();
			if (sorter == null)
				return;

			ArrayList sorters;
			if (sorter is AndSorter)
				sorters = (sorter as AndSorter).Sorters;
			else
			{
				sorters = new ArrayList();
				sorters.Add(sorter);
			}

			for (int i = 0; i < sorters.Count; i++)
			{
				// set our current column to the one we are sorting.
				int ifsi = this.ColumnInfoIndexOfCompatibleSorter((RecordSorter)sorters[i]);

				// set our header column arrow
				SortOrder order = SortOrder.Ascending;
				GenRecordSorter grs = sorters[i] as GenRecordSorter;
				if (grs != null)
				{
					StringFinderCompare sfc = grs.Comparer as StringFinderCompare;
					if (sfc != null)
						order = sfc.Order;
				}
				int iHeaderColumn = ColumnHeaderIndex(ifsi);

				if (i == 0)
					SetSortArrowColumn(iHeaderColumn, order, DhListView.ArrowSize.Large);
				else if (i == 1)
					SetSortArrowColumn(iHeaderColumn, order, DhListView.ArrowSize.Medium);
				else
					SetSortArrowColumn(iHeaderColumn, order, DhListView.ArrowSize.Small);

				SIL.Utils.Logger.WriteEvent(String.Format("Sort on {0} {1} ({2})",
						m_lvHeader.Columns[iHeaderColumn].Text, order.ToString(), i));
			}
		}

		// Sets the sort arrow for the given header column. Also sets the current column if
		// its a valid header column.
		private void SetSortArrowColumn(int iSortArrowColumn, SortOrder order, DhListView.ArrowSize size)
		{
			if (iSortArrowColumn >= 0)
				m_icolCurrent = iSortArrowColumn;

			m_lvHeader.ShowHeaderIcon(iSortArrowColumn, order, size);
		}

		// Resets all column sort arrows
		private void ResetSortArrowColumn()
		{
			for (int i = 0; i < m_lvHeader.Columns.Count; i++)
				m_lvHeader.ShowHeaderIcon(i, SortOrder.None, DhListView.ArrowSize.Large);
		}

		/// <summary>
		/// the object that has properties that are shown by this view.
		/// </summary>
		/// <remarks> this will be changed often in the case where this view is dependent on another one;
		/// that is, or some other browse view has a list and each time to selected item changes, our
		/// root object changes.
		/// </remarks>
		public int RootObjectHvo
		{
			get
			{
				CheckDisposed();
				return m_xbv.RootObjectHvo;
			}
			set
			{
				CheckDisposed();
				m_xbv.RootObjectHvo = value;
			}
		}

		/// <summary>
		/// Set the stylesheet used to display information.
		/// </summary>
		public IVwStylesheet StyleSheet
		{
			get
			{
				CheckDisposed();
				return m_xbv.StyleSheet;
			}
			set
			{
				CheckDisposed();

				m_xbv.StyleSheet = value;
				if (m_filterBar != null)
					m_filterBar.SetStyleSheet(value);
				if (m_bulkEditBar != null)
					m_bulkEditBar.SetStyleSheet(value);
			}
		}

		/// <summary>
		/// The top-level property of RootObjectHvo that we are displaying.
		/// </summary>
		public int MainTag
		{
			get
			{
				CheckDisposed();
				return m_xbv.MainTag;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:BrowseViewer"/> class.
		/// </summary>
		/// <param name="nodeSpec">The node spec.</param>
		/// <param name="hvoRoot">The hvo root.</param>
		/// <param name="fakeFlid">The fake flid.</param>
		/// <param name="cache">The cache.</param>
		/// <param name="mediator">The mediator.</param>
		/// <param name="sortItemProvider">the record list supplying this list</param>
		/// ------------------------------------------------------------------------------------
		public BrowseViewer(XmlNode nodeSpec, int hvoRoot, int fakeFlid,
			FdoCache cache, Mediator mediator, ISortItemProvider sortItemProvider)
		{
			ContructorSurrogate(nodeSpec, hvoRoot, fakeFlid, cache, mediator, sortItemProvider);
		}

		internal void ContructorSurrogate(XmlNode nodeSpec, int hvoRoot, int fakeFlid,
			FdoCache cache, Mediator mediator, ISortItemProvider sortItemProvider)
		{
			CheckDisposed();

			m_nodeSpec = nodeSpec;
			m_cache = cache;
			m_mediator = mediator;
			m_lvHeader = new DhListView(this);
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
			this.SuspendLayout();
			m_scrollContainer = new BrowseViewScroller(this);
			m_scrollContainer.AutoScroll = true;
			m_scrollContainer.TabStop = false;
			this.Controls.Add(m_scrollContainer);
			m_scrollBar = new VScrollBar();
			//m_scrollBar.Scroll += new ScrollEventHandler(m_scrollBar_Scroll);
			m_scrollBar.ValueChanged += new EventHandler(m_scrollBar_ValueChanged);
			m_scrollBar.TabStop = false;
			this.Controls.Add(m_scrollBar);
			// Set this before creating the browse view class so that custom parts can be
			// generated properly.
			m_sortItemProvider = sortItemProvider;
			// Make the right subclass of XmlBrowseViewBase first, the column header creation uses information from it.
			CreateBrowseViewClass(hvoRoot, fakeFlid, mediator);
			// This would eventually get set in the startup process later, but we need it at least by the time
			// we make the filter bar so the LayoutCache exists.
			BrowseView.Vc.Cache = cache;
			m_scrollContainer.SuspendLayout();
			//
			// listView1
			//
			//this.m_lvHeader.Dock = System.Windows.Forms.DockStyle.Top;
			this.m_lvHeader.Name = "HeaderListView";
			this.m_lvHeader.Size = new System.Drawing.Size(4000, 22);
			this.m_lvHeader.TabIndex = 0;
			this.m_lvHeader.View = System.Windows.Forms.View.Details;
			this.m_lvHeader.ColumnClick += new ColumnClickEventHandler(m_lvHeader_ColumnClick);
			this.m_lvHeader.ColumnRightClick += new ColumnRightClickEventHandler(m_lvHeader_ColumnRightClick);
			this.m_lvHeader.ColumnDragDropReordered += new ColumnDragDropReorderedHandler(m_lvHeader_ColumnDragDropReordered);
			this.m_lvHeader.AllowColumnReorder = true;
			this.m_lvHeader.AccessibleName = "HeaderListView";
			m_lvHeader.Scrollable = false; // don't EVER show scroll bar in it!!
			m_lvHeader.TabStop = false;
			//AddControl(m_lvHeader); Do this after making the filter bar if any.

			this.Name = "BrowseView";
			this.Size = new System.Drawing.Size(400, 304);
			if (ColumnIndexOffset() > 0)
			{
				ColumnHeader ch = new ColumnHeader();
				ch.Text = "";
				m_lvHeader.Columns.Add(ch);
			}

			if (m_xbv.Vc.ShowColumnsRTL)
			{
				for (int i = ColumnSpecs.Count - 1; i >= 0; --i)
				{
					XmlNode node = ColumnSpecs[i];
					ColumnHeader ch = MakeColumnHeader(node);
					m_lvHeader.Columns.Add(ch);
				}
			}
			else
			{
				for (int i = 0; i < ColumnSpecs.Count; ++i)
				{
					XmlNode node = ColumnSpecs[i];
					ColumnHeader ch = MakeColumnHeader(node);
					m_lvHeader.Columns.Add(ch);
				}
			}
			// set default property, so it doesn't accidentally get set
			// in OnPropertyChanged() when user right clicks for the first time (cf. LT-2789).
			m_xbv.Mediator.PropertyTable.SetDefault("SortedFromEnd", false, false, PropertyTable.SettingsGroup.LocalSettings);
			// set default property, so it doesn't accidentally get set
			// in OnPropertyChanged() when user right clicks for the first time (cf. LT-2789).
			m_xbv.Mediator.PropertyTable.SetDefault("SortedByLength", false, false, PropertyTable.SettingsGroup.LocalSettings);

			//
			// FilterBar
			//
			XmlAttribute xa = m_nodeSpec.Attributes["filterBar"];
			if (xa != null && xa.Value == "true")
			{
				m_filterBar = new FilterBar(this, m_nodeSpec);
				m_filterBar.FilterChanged += new FilterChangeHandler(this.FilterChangedHandler);
				//m_filterBar.Dock = System.Windows.Forms.DockStyle.Top;
				m_filterBar.Name = "FilterBar";
				m_filterBar.AccessibleName = "FilterBar";
				m_lvHeader.TabIndex = 1;
				AddControl(m_filterBar);
			}
			AddControl(m_lvHeader); // last so on top of z-order, puts it above other things docked at top.
			xa = m_nodeSpec.Attributes["bulkEdit"];
			if (xa != null && xa.Value == "true")
			{
				m_bulkEditBar = CreateBulkEditBar(this, m_nodeSpec, mediator, m_cache);
				m_bulkEditBar.Dock = System.Windows.Forms.DockStyle.Bottom;
				m_bulkEditBar.Name = "BulkEditBar";
				m_bulkEditBar.AccessibleName = "BulkEditBar";
				Controls.Add(m_bulkEditBar);
				m_xbv.Vc.PreviewArrow = m_bulkEditBar.PreviewArrow;
				// Enhance JohnT: if we ever allow editing within the browse part
				// of bulk edit, we'll need to stop doing this. However, if there really are
				// no columns where you can edit, if we don't do this, SimpleRootSite eventually
				// tries to make a selection somewhere that allows editing, and spends forever
				// looking for an editable place if the database is large.
				m_xbv.ReadOnlyView = true;
			}
			if (m_xbv.Vc.HasSelectColumn)
			{
				// Don't enable DisplayCheckMarkHeader here, since it doesn't display reliably (LT-4473).
				// Do our own setup for this button.
				//m_lvHeader.DisplayCheckMarkHeader = true;
				//m_lvHeader.CheckIconClick += new ConfigIconClickHandler(m_lvHeader_CheckIconClick);
				m_checkMarkButton = new Button();
				m_checkMarkButton.Click += new EventHandler(m_checkMarkButton_Click);
				m_checkMarkButton.Image = ResourceHelper.CheckMarkHeader;
				m_checkMarkButton.Width = m_checkMarkButton.Image.Width + 5;
				m_checkMarkButton.Height = m_lvHeader.Height - 6;
				m_checkMarkButton.FlatStyle = FlatStyle.Flat;
				m_checkMarkButton.BackColor = Color.Transparent;
				m_checkMarkButton.ForeColor = Color.Transparent;
				m_checkMarkButton.Top = 2;
				m_checkMarkButton.Left = 1;
				ToolTip ttip = new ToolTip();
				ttip.SetToolTip(m_checkMarkButton, XMLViewsStrings.ksTipCheck);
				Controls.Add(m_checkMarkButton);
				m_checkMarkButton.BringToFront();
			}

			// We make an overlay button to occupy the space above the top of the scroll bar where there is no column.
			// It might be possible to make a dummy column here and just put the icon in it...
			// but I think I (JT) found that if the total width of the columns is any more than it is now,
			// DotNet adds a horizontal scroll bar which totally hides the labels.
			m_configureButton = new Button();
			m_configureButton.Visible = !XmlUtils.GetOptionalBooleanAttributeValue(m_nodeSpec, "disableConfigButton", false);
			m_configureButton.Click += new EventHandler(m_configureButton_Click);
			// Don't dock the button, this destroys all control over its height, and it overwrites the line at the bottom
			// of the header bar.
			//m_configureButton.Dock = DockStyle.Right;
			Image blueArrow = ResourceHelper.ColumnChooser;

			m_configureButton.Image = blueArrow;
			// We want the button to basically occupy the gap above the scroll bar on the main window.
			// The -5 seems to allow room for various borders etc and prevent it overwriting the vertical
			// line at the end of the right column.
			m_configureButton.Width = DhListView.kgapForScrollBar - 5;
			// A flat button using ControlLight for both background and foreground merges into the
			// background of the list view header and looks as if only the icon is there.
			m_configureButton.FlatStyle = FlatStyle.Flat;
			m_configureButton.BackColor = Color.FromKnownColor(KnownColor.ControlLight);
			m_configureButton.ForeColor = m_configureButton.BackColor;
			m_tooltip = new ToolTip();
			m_tooltip.SetToolTip(m_configureButton, XMLViewsStrings.ksTipConfig);
			this.Controls.Add(m_configureButton);
			m_xbv.AutoScroll = false;

			m_configureButton.ImageAlign = ContentAlignment.MiddleCenter;
			m_configureButton.TabStop = false;

			this.AutoScroll = true;
			this.VScroll = false;
			this.HScroll = true;
			m_doHScroll = true;
			m_scrollContainer.ResumeLayout(false);
			this.ResumeLayout(false);
		}

		/// <summary>
		/// Indicates the state of the checkboxes for all displayed items.
		/// </summary>
		enum CheckedStatus
		{
			NoItemsAreChecked,
			MixedCheckedStatus,
			AllItemsAreChecked
		}

		private CheckedStatus GetAllItemsCheckedStatus()
		{
			List<int> allItems = AllItems;
			List<int> checkedItems = CheckedItems;

			if (checkedItems.Count == 0)
				return CheckedStatus.NoItemsAreChecked;
			if (checkedItems.Count == allItems.Count)
				return CheckedStatus.AllItemsAreChecked;
			return CheckedStatus.MixedCheckedStatus;
		}

		/// <summary>
		/// indicates the last class of items that the user selected something.
		/// </summary>
		private int m_lastChangedSelectionListItemsClass = 0;

		private XmlNode m_modifiedColumn;

		/// <summary>
		/// This is called just before the TargetComboSelecctedIndexChanged event.
		/// It may set the ForceReload flag on the event for the benefit of other callers.
		/// </summary>
		internal void BulkEditTargetComboSelectedIndexChanged(TargetColumnChangedEventArgs e)
		{
			if (e.ExpectedListItemsClass != 0)
			{
				if (m_xbv.Vc.ListItemsClass != e.ExpectedListItemsClass)
					OnListItemsAboutToChange(AllItems);
				m_xbv.Vc.ListItemsClass = (uint) e.ExpectedListItemsClass;
			}
			if (m_modifiedColumn != null)
			{
				XmlUtils.SetAttribute(m_modifiedColumn, "layout",
									  XmlUtils.GetManditoryAttributeValue(m_modifiedColumn, "normalLayout"));
				m_modifiedColumn = null;
				e.ForceReload = true;
			}
			// One way this fails is that items for bulk delete can have -1 as column number (since deleting
			// whole rows doesn't apply to particular columns). Checking the other end of the range is just paranoia.
			if (e.ColumnIndex >= 0 && e.ColumnIndex < ColumnSpecs.Count)
			{
				XmlNode column = ColumnSpecs[e.ColumnIndex];
				string editLayout = XmlUtils.GetOptionalAttributeValue(column, "editLayout");
				string layout = XmlUtils.GetOptionalAttributeValue(column, "layout");
				if (layout != null && editLayout != null && editLayout != layout)
				{
					// This column (e.g., Reversals) needs a different layout to be used when bulk edited.
					// For example, when Reversals is not being edited, we want a distinct item for each
					// reversal, so things can be sorted differently.
					XmlUtils.SetAttribute(column, "normalLayout", layout);
					XmlUtils.SetAttribute(column, "layout", editLayout);
					m_modifiedColumn = column;
					e.ForceReload = true;
				}
			}
			if (TargetColumnChanged != null)
				TargetColumnChanged(this, e);
		}

		/// <summary>
		/// Save the current state of given selection items, so
		/// the next list can be consistent with those selections (LT-8986)
		/// UpdateCheckedItems() uses this information to put the next list items
		/// in the proper state.
		/// </summary>
		private void OnListItemsAboutToChange(IList<int> selectionItemsToSave)
		{
			if (!m_fIsInitialized)
				return;
			m_lastChangedSelectionListItemsClass = (int) m_xbv.Vc.ListItemsClass;
			SaveSelectionItems(new Set<int>(selectionItemsToSave));
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="bv"></param>
		/// <param name="spec"></param>
		/// <param name="mediator"></param>
		/// <param name="cache"></param>
		/// <returns></returns>
		protected virtual BulkEditBar CreateBulkEditBar(BrowseViewer bv, XmlNode spec, Mediator mediator, FdoCache cache)
		{
			return new BulkEditBar(bv, spec, mediator, cache);
		}

		private void AddControl(Control control)
		{
			m_scrollContainer.Controls.Add(control);
		}

		/// <summary>
		/// Initialize the sorter (typically starting with the Clerk's sorter, but if the columns are reordered
		/// this should re-sync things).
		/// 1) If we are not given a GenRecordSorter we will raise to resort to our first sortable column.
		/// 2) If we are given a GenRecordSorter but we can't find a corresponding column in our browseview,
		///		then accept the given sorter and clear the column header sort arrow.
		/// 3) Otherwise, use a column's matched sorter and copy the needed information from the given sorter.
		/// </summary>
		/// <param name="sorter"></param>
		/// <returns>true if it changed the sorter.</returns>
		public bool InitSorter(RecordSorter sorter)
		{
			CheckDisposed();

			if (m_filterBar == null || m_filterBar.ColumnInfo.Length <= 0 || sorter == null)
				return false;

			ArrayList sorters;
			if (sorter is AndSorter)
				sorters = (sorter as AndSorter).Sorters;
			else
			{
				sorters = new ArrayList();
				sorters.Add(sorter);
			}

			ArrayList newSorters = new ArrayList();
			bool fSortChanged = false;
			for(int i = 0; i < sorters.Count; i++)
			{
				int ifsi = ColumnInfoIndexOfCompatibleSorter(sorters[i] as RecordSorter);
				if ((sorters[i] as GenRecordSorter) == null && ifsi < 0)
				{
					// we don't want to use this sorter because it's not the kind that our filters
					// are set up for. Let's change the sorter to one of our own.
					sorters[i] = m_filterBar.ColumnInfo[0].Sorter;
					continue;
				}

				if (ifsi >= 0)
				{
					// We found a sorter in our columns that matches the given one,
					// so, preserve the given sorter's Order and SortFromEnd states.
					GenRecordSorter grs = sorters[i] as GenRecordSorter;
					StringFinderCompare sfc = grs.Comparer as StringFinderCompare;
					GenRecordSorter newGrs = m_filterBar.ColumnInfo[ifsi].Sorter as GenRecordSorter;
					if (!newGrs.Equals(grs))
					{
						sfc.CopyTo(newGrs.Comparer as StringFinderCompare);	// copy Order and SortFromEnd
						fSortChanged = true;
					}
					sorters[i] = newGrs;
				}
				else
				{
					// The given sorter is a GenRecordSorter, but we couldn't find its column
					// in our browse view columns.
					//newSorter = (GenRecordSorter)sorters[i];

					// Until adding the column dynamically is implemented, we'll just drop this sorter
					continue;
				}
				newSorters.Add(sorters[i]);
			}
			if (newSorters.Count != sorters.Count)
				fSortChanged = true;
			if (newSorters.Count > 1)
			{
				AndSorter asorter = new AndSorter(newSorters);
				Sorter = asorter;
			}
			else if (newSorters.Count == 1)
			{
				Sorter = newSorters[0] as RecordSorter;
			}
			else
			{
				// No sorters could get transfered over, we've got to have one, so we'll take whatever
				// is left over in the first column
				Sorter = m_filterBar.ColumnInfo[0].Sorter;
			}
			if (fSortChanged || !m_fUpdatingColumnList)
				SetAndRaiseSorter(Sorter, fSortChanged);
			return fSortChanged;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="sorter"></param>
		/// <returns></returns>
		protected int ColumnInfoIndexOfCompatibleSorter(RecordSorter sorter)
		{
			RecordSorter newSorter = null;
			int iSortColumn = -1;
			newSorter = null;
			for (int icol = 0; icol < m_filterBar.ColumnInfo.Length; icol++)
			{
				FilterSortItem fsi = m_filterBar.ColumnInfo[icol];
				RecordSorter thisSorter = fsi.Sorter;
				if (thisSorter.CompatibleSorter(sorter))
				{
					iSortColumn = icol;
					newSorter = thisSorter;
					break;
				}
			}
			return iSortColumn;
		}

		internal void HideBulkEdit()
		{
			CheckDisposed();

			Controls.Remove(m_bulkEditBar);
			m_lvHeader.DisplayCheckMarkHeader = false;
		}

		/// <summary>
		/// Make a column header for a specified XmlNode that is a "column" element.
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		private ColumnHeader MakeColumnHeader(XmlNode node)
		{
			// Currently, if you add a new attribute here,
			// you need to update the conditionals in LayoutFinder.SameFinder (cf. LT-2858).
			StringTable tbl = null;
			if (m_mediator != null && m_mediator.HasStringTable)
				tbl = m_mediator.StringTbl;
			string label = XmlUtils.GetLocalizedAttributeValue(tbl, node, "label", null);
			if (label == null)
			{
				if (node.Attributes["label"] == null)
					throw new ApplicationException("column must have label attr");
				label = node.Attributes["label"].Value;
			}
			ColumnHeader ch = new ColumnHeader();
			ch.Text = label;
			return ch;
		}

		private void CreateBrowseViewClass(int hvoRoot, int fakeFlid, Mediator mediator)
		{
			if (m_nodeSpec.Attributes["editRowModelClass"] != null)
				m_xbv = new XmlBrowseRDEView(); // Use special RDE class.
			else
				m_xbv = new XmlBrowseView();
			m_xbv.Init(mediator, m_nodeSpec); // BEFORE the init that makes the VC...that needs the ID.
			m_xbv.Init(m_nodeSpec, hvoRoot, fakeFlid, m_cache, mediator, this);
			m_xbv.SelectionChangedEvent += new FwSelectionChangedEventHandler(OnSelectionChanged);
			m_xbv.SelectedIndexChanged += new EventHandler(m_xbv_SelectedIndexChanged);
			// Sometimes we get a spurious "out of memory" error while trying to create a handle for the
			// RootSite if its cache isn't set before we add it to its parent.
			// This is now handled in the above Init method.
			//m_xbv.Cache = m_cache;
			AddControl(m_xbv);
		}

		bool m_fSavedSelectionsDuringFilterChange = false;
		private void FilterChangedHandler(object sender, FilterChangeEventArgs args)
		{
			if (FilterChanged != null)
			{
				// before we actually change the filter, save all the selected and unselected items.
				m_fSavedSelectionsDuringFilterChange = true;
				SaveAllSelectionItems();
				FilterChanged(this, args);
			}
			SetBrowseViewSpellingStatus();
		}

		/// <summary>
		/// keep track of the selection state of the items provided by IMultiListSortItemProvider
		/// so we can maintain consistency when switching lists (LT-8986)
		/// </summary>
		private IDictionary<int, object> m_selectedItems = new Dictionary<int, object>();
		private IDictionary<int, object> m_unselectedItems = new Dictionary<int, object>();

		/// <summary>
		/// TODO: Performance: only keep track of non-default selections
		/// </summary>
		private void SaveAllSelectionItems()
		{
			// save the latest selection state.
			SaveSelectionItems(new Set<int>(AllItems));
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="itemsToSaveSelectionState">items that need to be saved,
		/// especially those that the user has changed in selection status</param>
		private void SaveSelectionItems(Set<int> itemsToSaveSelectionState)
		{
			if (m_xbv.Vc.HasSelectColumn && BulkEditBar != null && m_sortItemProvider is IMultiListSortItemProvider)
			{
				object sourceTag = (m_sortItemProvider as IMultiListSortItemProvider).ListSourceToken;
				foreach (int hvoItem in itemsToSaveSelectionState)
				{
					if (IsItemChecked(hvoItem))
					{
						// try to remove the item from the unselected list and
						// update our selected items, if we haven't already.
						UpdateMutuallyExclusiveDictionaries<int, object>(hvoItem, sourceTag,
																		 ref m_unselectedItems,
																		 ref m_selectedItems);
					}
					else
					{
						// try to remove the item from the selected list and
						// update our unselected items, if we haven't already.
						UpdateMutuallyExclusiveDictionaries<int, object>(hvoItem, sourceTag,
																		 ref m_selectedItems,
																		 ref m_unselectedItems);
					}
				}
			}
		}

		private void UpdateMutuallyExclusiveDictionaries<TKey, TValue>(TKey key, TValue value,
			ref IDictionary<TKey, TValue> dictionaryToRemove, ref IDictionary<TKey, TValue> dictionaryToAdd)
		{
			dictionaryToRemove.Remove(key);
			if (!dictionaryToAdd.ContainsKey(key))
				dictionaryToAdd.Add(key, value);
		}

		/// <summary>
		/// items in m_selectedItems or m_unselectedItems may belong to a previous ListSourceToken
		/// and need to be converted to their relatives on the current ListSourceToken.
		/// </summary>
		private void ConvertItemsToRelativesThatApplyToCurrentListSelections()
		{
			// remove any old items that are no longer in their expected state
			RemoveInvalidOldSelectedItems(ref m_selectedItems, true);
			RemoveInvalidOldSelectedItems(ref m_unselectedItems, false);

			// NOTE: need to convert selected items after unselected, so that if they try to change the same parent
			// the selected one wins.
			ConvertItemsToRelativesThatApplyToCurrentListSelections(ref m_unselectedItems, false);
			ConvertItemsToRelativesThatApplyToCurrentListSelections(ref m_selectedItems, true);
		}

		private void ConvertItemsToRelativesThatApplyToCurrentListSelections(ref IDictionary<int, object> items, bool selectItem)
		{
			(m_sortItemProvider as IMultiListSortItemProvider).ConvertItemsToRelativesThatApplyToCurrentList(ref items);
			foreach (int relative in items.Keys)
			{
				// cache the selection on the relative (even if it's not yet visible, i.e. in "currentItems")
				SetItemCheckedState(relative, selectItem, false);
			}
		}

		private void RemoveInvalidOldSelectedItems(ref IDictionary<int, object> items, bool fExpectToBeSelected)
		{
			Set<int> invalidSelectedItems = new Set<int>();
			foreach (KeyValuePair<int, object> item in items)
			{
				bool fActuallySelected = IsItemChecked(item.Key);

				if (fExpectToBeSelected && !fActuallySelected ||
					!fExpectToBeSelected && fActuallySelected)
				{
					invalidSelectedItems.Add(item.Key);
				}
			}
			foreach (int item in invalidSelectedItems)
				items.Remove(item);
		}

		/// <summary>
		/// restore our saved select items.
		/// NOTE: in case of overlapping items (and thus conflicting selection agendas),
		/// we make selections to win over unselections
		/// </summary>
		private void RestoreSelectionItems()
		{
			ICollection<int> selectedItems = m_selectedItems.Keys;
			ICollection<int> unselectedItems = m_unselectedItems.Keys;
			if (m_xbv.Vc.HasSelectColumn && BulkEditBar != null)
			{
				foreach (int hvoItem in AllItems)
				{
					// NOTE: in case of overlapping items (and thus conflicting selection agendas), we want
					// selected items to win over unselected items.
					bool fIsItemChecked = IsItemChecked(hvoItem);
					if (selectedItems.Contains(hvoItem))
					{
						if (!fIsItemChecked)
							SetItemCheckedState(hvoItem, true, false);
					}
					else if (unselectedItems.Contains(hvoItem))
					{
						if (fIsItemChecked)
							SetItemCheckedState(hvoItem, false, false);
					}
					else
					{
						SetItemCheckedState(hvoItem, m_xbv.Vc.DefaultChecked, false);
					}
				}
			}
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

			if( disposing )
			{
				// If these controls are child controls of either 'this' or of m_scrollContainer,
				// they will automatically be disposed. If they have been removed for whatever reason,
				// we need to dispose of them here.
				if (m_configureButton != null)
				{
					m_configureButton.Click -= new EventHandler(m_configureButton_Click);
					if (!Controls.Contains(m_configureButton))
						m_configureButton.Dispose();
				}
				if (m_scrollBar != null)
				{
					//m_scrollBar.Scroll -= new ScrollEventHandler(m_scrollBar_Scroll);
					m_scrollBar.ValueChanged -= new EventHandler(m_scrollBar_ValueChanged);
					if (!Controls.Contains(m_scrollBar))
						m_scrollBar.Dispose();
				}
				if (m_scrollContainer != null)
				{
					if (m_lvHeader != null)
					{
						m_lvHeader.ColumnClick -= new ColumnClickEventHandler(m_lvHeader_ColumnClick);
						m_lvHeader.ColumnRightClick -= new ColumnRightClickEventHandler(m_lvHeader_ColumnRightClick);
						m_lvHeader.ColumnDragDropReordered -= new ColumnDragDropReorderedHandler(m_lvHeader_ColumnDragDropReordered);
						m_lvHeader.CheckIconClick -= new ConfigIconClickHandler(m_lvHeader_CheckIconClick);
						if (!m_scrollContainer.Controls.Contains(m_lvHeader))
							m_lvHeader.Dispose();
					}
					if (m_xbv != null)
					{
						m_xbv.SelectionChangedEvent -= new FwSelectionChangedEventHandler(OnSelectionChanged);
						m_xbv.SelectedIndexChanged -= new EventHandler(m_xbv_SelectedIndexChanged);
						m_xbv.Mediator.RemoveColleague(this);
						if (!m_scrollContainer.Controls.Contains(m_xbv))
							m_xbv.Dispose();
					}
					if (m_filterBar != null)
					{
						m_filterBar.FilterChanged -= new FilterChangeHandler(this.FilterChangedHandler);
						if (!m_scrollContainer.Controls.Contains(m_filterBar))
							m_filterBar.Dispose();
					}
					if (m_bulkEditBar != null && !m_scrollContainer.Controls.Contains(m_bulkEditBar))
						m_bulkEditBar.Dispose();
					if (!Controls.Contains(m_scrollContainer))
						m_scrollContainer.Dispose();
				}
				if (m_tooltip != null)
					m_tooltip.Dispose();
				if(components != null)
				{
					components.Dispose();
				}
			}
			m_configureButton = null;
			m_scrollBar = null;
			m_lvHeader = null;
			m_xbv = null;
			m_filterBar = null;
			m_bulkEditBar = null;
			m_scrollContainer = null;
			m_cache = null;
			m_nodeSpec = null;
			m_sortItemProvider = null;
			m_sorter = null;
			m_tooltip = null;
			m_currentFilter = null;

			base.Dispose( disposing );

			// Since it was in a static data member,
			// that may have been the only thing keeping it from being collected,
			// but we want to survive through the base Dispose call, at least.
			GC.KeepAlive(this);
		}

		/// <summary>
		///	invoked when our BrowseViewView selection changes
		/// </summary>
		/// <param name="sender">unused</param>
		/// <param name="e">the event arguments</param>
		public void OnSelectionChanged(object sender, FwObjectSelectionEventArgs e)
		{
			CheckDisposed();

			//we don't do anything but pass this on to objects which have subscribed to this event
			if (SelectionChanged != null)
				SelectionChanged(sender, e);
		}

		/// <summary>
		/// Return a rectangle relative to the top left of the client area of the window whose top and bottom
		/// match the selected row and whose left and right match the named column.
		/// </summary>
		/// <param name="colName"></param>
		/// <returns></returns>
		public Rectangle LocationOfCellInSelectedRow(string colName)
		{
			int index = IndexOfColumn(colName);
			if (index < 0 || m_xbv.SelectedIndex < 0)
				return new Rectangle();
			int width = m_colWidths[index];
			Rectangle row = m_xbv.LocationOfSelectedRow();
			int pos = 0;
			for (int i = 0; i < index; i++)
				pos += m_colWidths[i];
			return new Rectangle(pos, row.Top + m_xbv.Top, width, row.Height);
		}

		/// <summary>
		/// invoked when the BrowseView selection is chosen by a double click.  This is currently
		/// invoked only for read-only views.
		/// </summary>
		/// <param name="e"></param>
		public void OnDoubleClick(FwObjectSelectionEventArgs e)
		{
			CheckDisposed();

			if (SelectionMade != null)
				SelectionMade(this, e);
		}

		private bool HScrolling
		{
			get { return m_doHScroll; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// <param name="levent">A <see cref="T:System.Windows.Forms.LayoutEventArgs"/> that contains the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnLayout(LayoutEventArgs levent)
		{
			AdjustControls();
			base.OnLayout(levent); // doesn't do much, now we're not docking
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Control.SizeChanged"/> event.
		/// </summary>
		/// <param name="e">An <see cref="T:System.EventArgs"/> that contains the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged (e);
			// the != 0 check makes sure we don't do this until we've been laid out for real
			// at least once, which doesn't happen until we have all the requisite stuff created.
			if (Width != m_lastLayoutWidth && m_lastLayoutWidth != 0)
				AdjustControls();
		}

		const int ScrollLineHeight = 16;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adjusts the controls.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void AdjustControls()
		{
			m_lastLayoutWidth = Width;

			// The -5 seems to allow for various borders and put the actual icon in about the right place.
			m_configureButton.Left = this.Width - m_configureButton.Width;
			// -1 allows the border of the button to be hidden (clipped because it's outside the
			// client area of its parent), and the blue circle icon to be right at the top of the available space.
			m_configureButton.Top = -1;
			// -6 seems to be about right to allow for various borders and prevent the button from
			// overwriting the bottom border of the header bar.
			m_configureButton.Height = m_lvHeader.Height;

			int sbHeight = this.Height - m_configureButton.Height;
			int scrollContHeight = this.Height;
			if (m_bulkEditBar != null && m_bulkEditBar.Visible)
			{
				sbHeight -= m_bulkEditBar.Height;
				scrollContHeight -= m_bulkEditBar.Height;
			}
			m_scrollBar.Height = sbHeight;
			m_scrollBar.Location = new Point(Width - m_scrollBar.Width, m_configureButton.Height);
			m_scrollBar.Minimum = 0;
			m_scrollBar.LargeChange = Math.Max(this.Height - ScrollLineHeight, ScrollLineHeight);
			m_scrollBar.SmallChange = ScrollLineHeight;
			//m_scrollBar.Maximum = m_xbv.AutoScrollMinSize.Height;
			//m_scrollBar.Value = -m_xbv.AutoScrollPosition.Y;
			m_scrollContainer.Location = new Point(0,0);
			m_scrollContainer.Height = scrollContHeight;
			m_scrollContainer.Width = this.Width - Math.Max(m_configureButton.Width, m_scrollBar.Width);
		}

		/// <summary>
		/// Called by a layout event in the BrowseViewScroller. Adjusts the controls inside it.
		/// </summary>
		internal void LayoutScrollControls()
		{
			CheckDisposed();

			if (m_xbv == null || m_lvHeader == null || m_lvHeader.Columns.Count == 0 || m_scrollContainer.Width < DhListView.kgapForScrollBar)
				return; // sometime called very early in construction process.
			int widthTotal = Math.Max(SetSavedOrDefaultColWidths(m_scrollContainer.Width), m_scrollContainer.ClientRectangle.Width);
			int xPos = m_scrollContainer.AutoScrollPosition.X;
			// This simulates docking, except that we don't adjust the width if we are
			// scrolling horizontally.
			m_lvHeader.Location = new Point(xPos, 0);
			m_lvHeader.Width = widthTotal;
			int top = m_lvHeader.Height;
			if (m_filterBar != null && m_filterBar.Visible)
			{
				m_filterBar.Location = new Point(xPos, top);
				m_filterBar.Width = widthTotal;
				top += m_filterBar.Height;
			}
			m_xbv.Width = widthTotal;
			int bottom = this.Height;
			if (m_scrollContainer.Width < widthTotal)
				bottom -= 22; // leave room for horizontal scroll to prevent vertical scroll.
			if (m_bulkEditBar != null && m_bulkEditBar.Visible)
			{
				bottom -= m_bulkEditBar.Height;
			}
			m_xbv.Location = new Point(xPos, top);
			m_xbv.Height = (bottom - top);
			// Simulate a drag to align the columns.
			// Note that this isn't enough to make it right initially, it seems there isn't
			// enough of the view present to fix yet.
			AdjustColumnWidths(false);
			m_lvHeader.PerformLayout();
			m_scrollBar.LargeChange = Math.Max(m_xbv.Height - ScrollLineHeight, ScrollLineHeight);
		}

		/// <summary>
		/// gets the Scrollbar of the viewer
		/// </summary>
		public ScrollBar ScrollBar
		{
			get
			{
				CheckDisposed();
				return m_scrollBar;
			}
		}

#if !Later
		/// <summary>
		/// gets the (horizontal) Scrollbar of the viewer
		/// </summary>
		public BrowseViewScroller Scroller
		{
			get
			{
				CheckDisposed();
				return m_scrollContainer;
			}
		}
#endif
		private int SetSavedOrDefaultColWidths(int idealWidth)
		{
			int widthTotal = 0;
			int widthExtra = 0; // how much wider header is than total data column width
			try
			{
				m_lvHeader.AdjustingWidth = true;
				// The width available for columns is less than the total width of the browse view by an amount that appears
				// a little wider than the scroll bar. 23 seems to be the smallest value that suppresses scrolling within
				// the header control...hopefully we can get some sound basis for it eventually.
				int widthAvail = idealWidth;
				List<XmlNode> columns = ColumnSpecs;
				int count = columns.Count;
				int dpiX = GetDpiX();
				if (ColumnIndexOffset() > 0)
				{
					int selColWidth = m_xbv.Vc.SelectColumnWidth * dpiX / 72000;
					m_lvHeader.Columns[0].Width = selColWidth;
					m_lvHeader.RecordCheckWidth(selColWidth);
					widthAvail -= selColWidth;
					widthExtra += selColWidth;
				}
				for (int i = 0; i < count; i++)
				{
					// If the user previously altered the column width, it will be available
					// in the Property table, as an absolute value. If not, use node.Attributes
					// to get a percentage value.
					int width = GetPersistedWidthForColumn(columns, i);
					if (width < 0)
					{
						width = GetInitialColumnWidth(columns[i], widthAvail, dpiX);
					}
					widthTotal += width;
					if (widthTotal + widthExtra + 1 > m_lvHeader.Width)
						m_lvHeader.Width = widthTotal + widthExtra + 1; // otherwise it may truncate the width we set.
					ColumnHeader ch = m_lvHeader.Columns[ColumnHeaderIndex(i)];
					ch.Width = width;
					// If the header isn't wide enough for the column to be the width we're setting, fix it.
					while (ch.Width != width)
					{
						m_lvHeader.Width += width - ch.Width;
						ch.Width = width;
					}
				}
			}
			finally
			{
				m_lvHeader.AdjustingWidth = false;
			}
			return widthTotal + widthExtra;
		}

		/// <summary>
		/// Get the persisted property table value for the specified column.
		/// </summary>
		/// <param name="colSpecs">XmlNode column specs</param>
		/// <param name="iCol">index to the column node</param>
		/// <returns>width if property value found, otherwise -1.</returns>
		private int GetPersistedWidthForColumn(List<XmlNode> colSpecs, int iCol)
		{
			int width = -1; // default to trigger percentage width calculation.
			if (m_xbv.Mediator != null)
			{
				string PropName = FormatColumnWidthPropertyName(iCol);
				width = m_xbv.Mediator.PropertyTable.GetIntProperty(PropName, -1, PropertyTable.SettingsGroup.LocalSettings);
			}
			return width;
		}

		private int GetDpiX()
		{
			Graphics g = CreateGraphics();
			int dpiX = (int)g.DpiX;
			g.Dispose();
			return dpiX;
		}

		private int GetInitialColumnWidth(XmlNode node, int widthAvail, int dpiX)
		{
			int width;
			string strWidth = XmlUtils.GetOptionalAttributeValue(node, "width", "48000");
			if (strWidth.Length > 1 && strWidth[strWidth.Length - 1] == '%')
			{
				int widthPercent = Convert.ToInt32(strWidth.Substring(0, strWidth.Length - 1)); // strip percent sign (assumed).
				width = widthPercent * widthAvail / 100;
			}
			else
			{
				// Convert to pixels from millipoints.
				width = Convert.ToInt32(strWidth) * dpiX / 72000;
			}
			return width;
		}

		bool EqualIntArrays(int[] first, int[] second)
		{
			if (first == null)
				return second == null;
			if (second == null)
				return false;
			if (first.Length != second.Length)
				return false;
			for (int i = 0; i < first.Length; i++)
				if (first[i] != second[i])
					return false;
			return true;
		}

		private class OneColumnXmlBrowseView : XmlBrowseViewBase
		{
			private OneColumnXmlBrowseView()
				: base()
			{
			}

			internal OneColumnXmlBrowseView(BrowseViewer bv, int icolLvHeaderToAdd)
				: this(bv.m_nodeSpec, bv.RootObjectHvo, bv.MainTag, bv.Cache, bv.Mediator, bv.StyleSheet, bv)
			{
				// add only the specified column to this browseview.
				(this.Vc as OneColumnXmlBrowseViewVc).SetupOneColumnSpec(bv, icolLvHeaderToAdd);
			}

			private OneColumnXmlBrowseView(XmlNode nodeSpec, int hvoRoot, int mainTag, FdoCache cache, Mediator mediator,
				IVwStylesheet styleSheet, BrowseViewer bv)
				: base()
			{
				base.Init(mediator, nodeSpec);
				base.Init(nodeSpec, hvoRoot, mainTag, cache, mediator, bv);
				// note: bv was used to initialize SortItemProvider. But we don't need it after init so null it out.
				m_bv = null;

				m_styleSheet = styleSheet;
				MakeRoot();
			}

			public override void MakeRoot()
			{
				CheckDisposed();

				m_rootb = VwRootBoxClass.Create();
				m_rootb.SetSite(this);
				this.ReadOnlyView = this.ReadOnlySelect;
				Vc.Cache = Cache;
				m_rootb.SetRootObject(m_hvoRoot, Vc, (int)XmlBrowseViewVc.kfragRoot, m_styleSheet);
				m_rootb.DataAccess = m_fdoCache.MainCacheAccessor;
				m_dxdLayoutWidth = kForceLayout; // Don't try to draw until we get OnSize and do layout.
			}

			/// <summary>
			/// override with our own simple constructor
			/// </summary>
			public override XmlBrowseViewBaseVc Vc
			{
				get
				{
					if (m_xbvvc == null)
						m_xbvvc = new OneColumnXmlBrowseViewVc(m_nodeSpec, m_fakeFlid, m_stringTable, this);
					return m_xbvvc;
				}
			}

			/// <summary>
			/// effectively simulate infinite length so we do not wrap cell contents.
			/// </summary>
			/// <param name="prootb"></param>
			/// <returns></returns>
			public override int GetAvailWidth(IVwRootBox prootb)
			{
				return 1000000;
			}

			/// <summary>
			/// Return column width information for one column that takes up 100%
			/// of the available width.
			/// </summary>
			/// <returns></returns>
			public override VwLength[] GetColWidthInfo()
			{
				CheckDisposed();

				VwLength[] rglength;
				Debug.Assert(Vc.ColumnSpecs.Count == 1, "Only support one column in this browse view");
				rglength = new VwLength[1];
				rglength[0].unit = VwUnit.kunPercent100;
				rglength[0].nVal = 10000;
				return rglength;
			}

			/// <summary>
			/// we don't care about the sort order in this browseview
			/// </summary>
			/// <param name="icol"></param>
			/// <returns></returns>
			public override bool ColumnSortedFromEnd(int icol)
			{
				return false;
			}

			/// <summary>
			/// measures the width of the strings built by the display of a column and
			/// returns the maximumn width found.
			/// NOTE: you may need to add a small (e.g. under 10-pixel) margin to prevent wrapping in most cases.
			/// </summary>
			/// <returns>width in pixels</returns>
			public int GetMaxCellContentsWidth()
			{
				int maxWidth = 0;
				using (Graphics g = Graphics.FromHwnd(this.Handle))
				{
					// get a best  estimate to determine row needing the greatest column width.
					MaxStringWidthForColumnEnv env = new MaxStringWidthForColumnEnv(StyleSheet, Cache.MainCacheAccessor,
						RootObjectHvo, g, 0);
					Cache.EnableBulkLoadingIfPossible(true);
					try
					{
						this.Vc.Display(env, RootObjectHvo, (int)XmlBrowseViewBaseVc.kfragRoot);
					}
					finally
					{
						Cache.EnableBulkLoadingIfPossible(false);
					}
					maxWidth = env.MaxStringWidth;
				}
				return maxWidth;
			}

		}

		private class OneColumnXmlBrowseViewVc : XmlBrowseViewVc
		{
			/// <summary>
			/// for comparing to the "active" (i.e. preview column)
			/// </summary>
			private int m_icolAdded = -1;

			/// <summary>
			/// we only setup for regular columns, and only one at a time.
			/// </summary>
			protected override void SetupSelectColumn()
			{
				m_fShowSelected = false;
			}

			internal void SetupOneColumnSpec(BrowseViewer bv, int icolToAdd)
			{
				ColumnSpecs = new List<XmlNode>(new XmlNode[] { bv.ColumnSpecs[icolToAdd - bv.ColumnIndexOffset()] });
				m_icolAdded = icolToAdd;
				// if we have a bulk edit bar, we need to process strings added for Preview
				if (bv.BulkEditBar != null)
					this.PreviewArrow = bv.BulkEditBar.PreviewArrow;
			}

			/// <summary>
			/// in a OneColumn browse view, the column will be "Active"
			/// if we are based upon a column being previewed in the original browse view.
			/// </summary>
			/// <param name="vwenv"></param>
			/// <param name="hvoRoot"></param>
			/// <returns>1 if we're based on a column with "Preview" enabled, -1 otherwise.</returns>
			protected override int GetActiveColumn(IVwEnv vwenv, int hvoRoot)
			{
				int iActiveColumn = base.GetActiveColumn(vwenv, hvoRoot);
				if (iActiveColumn == m_icolAdded)
					return 1;
				return -1;
			}

			public OneColumnXmlBrowseViewVc(XmlNode xnSpec, int fakeFlid, StringTable stringTable, XmlBrowseViewBase xbv)
				: base(xnSpec, fakeFlid, stringTable, xbv)
			{
			}
		}


		internal void AdjustColumnWidthToMatchContents(int icolLvHeaderToAdjust)
		{
			if (m_xbv.Vc.HasSelectColumn && icolLvHeaderToAdjust == 0)
				return; // don't auto-size a select column.

			// by default '0' will not change the size of the column.
			int maxStringWidth = 0;

			// setup a simple browse view that lays out the given column with infinite width (no wrapping)
			// so we can find the width of the maximum string content.
			using (new WaitCursor(this))
			{
				using (OneColumnXmlBrowseView xbv = new OneColumnXmlBrowseView(this, icolLvHeaderToAdjust))
				{
					maxStringWidth = xbv.GetMaxCellContentsWidth();
				}
			}
			// adjust column header widths
			if (maxStringWidth > 0)
			{
				// update the column according to the maxStringWidth.
				// add in a little margin to prevent wrapping in some cases.
				m_lvHeader.Columns[icolLvHeaderToAdjust].Width = maxStringWidth + 10;

				// force browse view to match header columns.
				AdjustColumnWidths(true);
			}
		}

		/// <summary>
		/// Adjust column widths, typically after dragging of a column in the header,
		/// or after the user inserts or deletes a column.
		/// Also records the current widths of all columns in mediator properties.
		/// </summary>
		public void AdjustColumnWidths(bool fPersistNew)
		{
			CheckDisposed();

			if (m_xbv == null || m_xbv.RootBox == null)
				return;
			VwLength[] rglength;
			int[] widths;
			GetColWidthInfo(out rglength, out widths);
			// This is very worth checking, because this routine gets called once per column
			// while turning the sort arrow on and off, with no actual width changes.
			// I've observed that to cost 3 seconds on a large table.
			if (EqualIntArrays(widths, m_colWidths))
				return;
			m_colWidths = widths;
			m_xbv.RootBox.SetTableColWidths(rglength, rglength.Length);
			if (m_filterBar != null)
				m_filterBar.SetColWidths(widths);
			// BulkEditBar2 does not align to columns.
			//			if (m_bulkEditBar != null)
			//				m_bulkEditBar.SetColWidths(widths);
			if (fPersistNew)
			{
				SaveColumnWidths();
			}
			FireColumnsChangedEvent();
			// m_lvHeader.UpdateConfigureButton();
		}

		private void FireColumnsChangedEvent()
		{
			if (ColumnsChanged != null)
				ColumnsChanged(this, new EventArgs());
		}

		/// <summary>
		/// Save the column widths based on the current header columns. This information is used in OnLayout
		/// to restore saved column widths. Be careful to keep the way the select column
		/// is handled consistent between the two places.
		/// </summary>
		private void SaveColumnWidths()
		{
			for (int iCol = 0; iCol < m_lvHeader.Columns.Count - ColumnIndexOffset(); ++iCol)
			{
				int nNewWidth = m_lvHeader.Columns[ColumnHeaderIndex(iCol)].Width;
				string PropName = FormatColumnWidthPropertyName(iCol);
				m_xbv.Mediator.PropertyTable.SetProperty(PropName, nNewWidth, PropertyTable.SettingsGroup.LocalSettings);
			}
		}

		private string FormatColumnWidthPropertyName(int iCol)
		{
			string Id1 = m_xbv.Mediator.PropertyTable.GetStringProperty("currentContentControl", "");
			string Id2 = BrowseView.GetCorrespondingPropertyName("Column");
			string PropName = Id1 + "_" + Id2 + "_" + iCol + "_Width";
			return PropName;
		}

		/// <summary>
		/// Get the widths of the columns, both as VwLengths (for the view tables) and as actual
		/// widths (used for the filter bar).
		/// </summary>
		/// <param name="rglength"></param>
		/// <param name="widths"></param>
		public void GetColWidthInfo(out VwLength[] rglength, out int[] widths)
		{
			CheckDisposed();

			int dpiX;
			using (Graphics g = CreateGraphics())
			{
				dpiX = (int)g.DpiX;
			}
			int count = m_lvHeader.Columns.Count;
			rglength = new VwLength[count];
			widths = new int[count];
			int widthTotal = 0;

			for (int i = 0; i < count; i++)
			{
				rglength[i].unit = VwUnit.kunPoint1000;
				int width = m_lvHeader.Columns[i].Width;
				rglength[i].nVal = width * 72000 / dpiX;
				widthTotal += width;
				widths[i] = width;
			}
		}

		/// <summary>
		/// Expand the widths of the columns proportionately to fit the width of the view.
		/// </summary>
		public void MaximizeColumnWidths()
		{
			int wTotal = m_lvHeader.Width;
			int count = m_lvHeader.Columns.Count;
			int[] rgw = new int[count];
			int wSum = 0;
			for (int i = 0; i < count; ++i)
			{
				rgw[i] = m_lvHeader.Columns[i].Width;
				wSum += rgw[i];
			}
			if (wSum + 40 < wTotal)
			{
				for (int i = 0; i < count; ++i)
					m_lvHeader.Columns[i].Width = (rgw[i] * wTotal) / wSum;
				AdjustColumnWidths(false);
			}
		}

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.SuspendLayout();
			//
			// BrowseViewer
			//
			this.Name = "BrowseViewer";
			this.ResumeLayout(false);
		}
		#endregion

		/// <summary>
		/// Handle left mouse button clicks on the column headers.  If the column supports
		/// sorting, sort the rows by this column.  If the rows were already sorted by this
		/// column, change the direction (ascending/descending) of the sort.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e">This gives the column where the click occurred.</param>
		protected void m_lvHeader_ColumnClick(object sender, ColumnClickEventArgs e)
		{
			if (m_filterBar == null)
				return; // for now we can't sort without a filter bar.
			int icolumn = e.Column;
			int ifsi = icolumn - m_filterBar.ColumnOffset;
			if (ifsi < 0)
			{
				// Click on the check box column; todo: maybe we could sort checked before
				// unchecked?
				return;
			}
			FilterSortItem fsi = m_filterBar.ColumnInfo[ifsi];
			if (fsi == null)
				return;

			RecordSorter newSorter = fsi.Sorter;
			if (newSorter.CompatibleSorter(this.Sorter))
			{
				// Choosing the same one again...reverse direction.
				newSorter = reverseNewSorter(newSorter, Sorter);
			}
			else if ((ModifierKeys & Keys.Shift) == Keys.Shift)
			{
				// If the user is holding shift down, we need to be working with an AndSorter
				AndSorter asorter;
				if (this.Sorter is AndSorter)
				{
					asorter = this.Sorter as AndSorter;
					if (asorter.CompatibleSorter(newSorter))
					{
						// Same one, just reversing the direction
						int i = asorter.CompatibleSorterIndex(newSorter);
						asorter.Sorters[i] = reverseNewSorter(newSorter, (RecordSorter) asorter.Sorters[i]);
					}
					else
					{
						asorter.Add(newSorter);
					}
				}
				else
				{
					asorter = new AndSorter();
					asorter.Add(this.Sorter);
					asorter.Add(newSorter);
				}
				newSorter = asorter;
			}

			this.SetAndRaiseSorter(newSorter, true);
		}

		private RecordSorter reverseNewSorter(RecordSorter newSorter, RecordSorter oldSorter) {
			RecordSorter origNewSorter = newSorter; // In case of complete failure

			GenRecordSorter grs = oldSorter as GenRecordSorter;
			if (grs == null)
				return origNewSorter; // should not happen.
			StringFinderCompare sfc = grs.Comparer as StringFinderCompare;
			if (sfc == null)
				return origNewSorter; // should not happen
			sfc.Reverse();
			if (newSorter != oldSorter)
			{
				GenRecordSorter grs2 = newSorter as GenRecordSorter;
				if (grs2 == null)
					return origNewSorter; // should not happen.
				sfc.CopyTo(grs2.Comparer as StringFinderCompare);
			}

			return newSorter;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Used for display purposes, we return true if the column has an active sorter and has been set to be sorted from end.
		/// </summary>
		/// <param name="icol">The icol.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool ColumnActiveAndSortedFromEnd(int icol)
		{
			CheckDisposed();

			if (Sorter == null)
				return false; // If there isn't a sorter, no columns can be actively sorted
			ArrayList sorters;
			if (Sorter is AndSorter)
				sorters = (Sorter as AndSorter).Sorters;
			else
			{
				sorters = new ArrayList();
				sorters.Add(Sorter);
			}

			// This loop attempts to locate the sorter in the list of active sorters that responsible for the column in question
			foreach(RecordSorter rs in sorters)
			{
				int ifsi = this.ColumnInfoIndexOfCompatibleSorter(rs);
				if (ifsi == icol - m_filterBar.ColumnOffset)
					return this.SpecificColumnSortedFromEnd(icol);
			}

			// Otherwise, we return false
			return false;
		}

		/// <summary>
		/// Gets the "sorted from end" flag for the specified column.  This is implemented seperately from
		/// the CurrentColumnSortedFromEnd property because the XmlBrowseViewBase needs to know for columns
		/// other than the current one.
		/// </summary>
		/// <param name="icol">The column index</param>
		/// <returns></returns>
		private bool SpecificColumnSortedFromEnd(int icol)
		{
			GenRecordSorter grs = GetColumnSorter(icol) as GenRecordSorter;
			if (grs == null)
				return false;
			Debug.Assert(grs.Comparer as StringFinderCompare != null,
				"Current Column does not have one of our sorters.");
			if (grs.Comparer as StringFinderCompare != null)
				return (grs.Comparer as StringFinderCompare).SortedFromEnd;
			else
				return false;
		}

		/// <summary>
		/// Get/Set the "sorted from end" flag for the current column.  This is part of
		/// handling right mouse button clicks in the column headers.
		/// </summary>
		private bool CurrentColumnSortedFromEnd
		{
			get
			{
				// See notes on the method for why this is different than the other ones
				return SpecificColumnSortedFromEnd(m_icolCurrent);
			}
			set
			{
				GenRecordSorter grs = GetCurrentColumnSorter() as GenRecordSorter;
				Debug.Assert(grs != null, "Current Column does not have a sorter.");
				if (grs == null)
					return;
				Debug.Assert(grs.Comparer as StringFinderCompare != null,
					"Current Column does not have one of our sorters.");
				if (grs.Comparer as StringFinderCompare != null)
					(grs.Comparer as StringFinderCompare).SortedFromEnd = value;
			}
		}

		/// <summary>
		/// Get/Set the "sorted by word length" flag for the current column.  This is part of
		/// handling right mouse button clicks in the column headers.
		/// </summary>
		private bool CurrentColumnSortedByLength
		{
			get
			{
				GenRecordSorter grs = GetCurrentColumnSorter() as GenRecordSorter;
				if (grs == null)
					return false;
				Debug.Assert(grs.Comparer as StringFinderCompare != null,
					"Current Column does not have one of our sorters.");
				if (grs.Comparer as StringFinderCompare != null)
					return (grs.Comparer as StringFinderCompare).SortedByLength;
				else
					return false;
			}
			set
			{
				GenRecordSorter grs = GetCurrentColumnSorter() as GenRecordSorter;
				Debug.Assert(grs != null, "Current Column does not have a sorter.");
				if (grs == null)
					return;
				Debug.Assert(grs.Comparer as StringFinderCompare != null,
					"Current Column does not have one of our sorters.");
				if (grs.Comparer as StringFinderCompare != null)
					(grs.Comparer as StringFinderCompare).SortedByLength = value;
			}
		}

		/// <summary>
		/// Handle right mouse button clicks in the column headers.  If the column supports
		/// sorting, bring up a menu that allows the user to choose sorting from the ends of
		/// words instead of the beginnings.  (This is useful for grouping words by suffix.)
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void m_lvHeader_ColumnRightClick(object sender, ColumnRightClickEventArgs e)
		{
			if (m_filterBar == null)
				return;			// for now we can't sort without a filter bar.
			int ifsi = e.Column - m_filterBar.ColumnOffset;
			if (ifsi < 0)
				return;			// Clicked on the check box column.
			FilterSortItem fsi = m_filterBar.ColumnInfo[ifsi];
			if (fsi == null)
				return;			// Can't sort by this column.
			m_icolCurrent = e.Column;

			//Debug.WriteLine("Right click in header of column " + e.Column);

			XWindow window = (XWindow)m_xbv.Mediator.PropertyTable.GetValue("window");
			window.ShowContextMenu("mnuBrowseHeader",
				new Point(Cursor.Position.X, Cursor.Position.Y),
				new TemporaryColleagueParameter(m_xbv.Mediator, this, false),
				null); // No MessageSequencer
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the config icon click.
		/// </summary>
		/// <param name="e">The <see cref="T:SIL.FieldWorks.Common.Controls.ConfigIconClickEventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void HandleConfigIconClick(ConfigIconClickEventArgs e)
		{
			ContextMenu menu = new ContextMenu();

			StringTable tbl = null;
			if (m_mediator != null && m_mediator.HasStringTable)
				tbl = m_mediator.StringTbl;
			// add items
			foreach (XmlNode node in m_xbv.Vc.ComputePossibleColumns())
			{
				// Show those nodes that have visibility="always" or "menu" (or none specified)
				string vis = XmlUtils.GetOptionalAttributeValue(node, "visibility", "always");
				if (vis != "always" && vis != "menu")
					continue;

				string label = XmlUtils.GetLocalizedAttributeValue(tbl, node, "label", null);
				if (label == null)
					label = XmlUtils.GetManditoryAttributeValue(node, "label");
				MenuItem mi = new MenuItem(label, new EventHandler(ConfigItemClicked));
				// Check items that match something in current visible list.
				mi.Checked =  this.IsColumnShowing(node);
				menu.MenuItems.Add(mi);
			}
			menu.MenuItems.Add("------");
			menu.MenuItems.Add(XMLViewsStrings.ksMoreColumns, new EventHandler(ConfigMoreChoicesItemClicked));

			menu.Show(this, new Point(e.Location.Left, e.Location.Bottom));

		}

		private bool Includes(XmlNodeList nodes, XmlNode target)
		{
			foreach(XmlNode node in nodes)
				if (node == target)
					return true;
			return false;
		}

		/// <summary>
		/// Remap a column index for ColumnSpecs to its matching column in m_lvHeader.Columns.
		/// This is necessary because when there is a check-box column in m_lvHeader.Columns[0],
		/// ColumnSpec[0] column == m_lvHeader.Columns[1] (everything shifts over one).
		/// </summary>
		/// <param name="columnSpecsIndex">index compatible with ColumnSpecs</param>
		/// <returns>the index that returns the ColumnSpecs equivalent node from m_lvHeader.Columns</returns>
		int ColumnHeaderIndex(int columnSpecsIndex)
		{
			if (m_xbv.Vc.ShowColumnsRTL)
				return m_lvHeader.Columns.Count - (columnSpecsIndex + 1);
			else if (m_xbv.Vc.HasSelectColumn)
				return columnSpecsIndex + 1;
			else
				return columnSpecsIndex;
		}

		/// <summary>
		/// Since ColumnHeaderIndex(0) is no longer reliable for adjusting for the select
		/// column, here's a new method that is.
		/// </summary>
		/// <returns></returns>
		protected int ColumnIndexOffset()
		{
			return m_xbv.Vc.HasSelectColumn ? 1 : 0;
		}

		// Handle the 'more column choices' item.
		private void ConfigMoreChoicesItemClicked(object sender, EventArgs args)
		{
			StringTable stringTbl = null;
			if (m_mediator != null && m_mediator.HasStringTable)
				stringTbl = m_mediator.StringTbl;
			ColumnConfigureDialog dlg = new ColumnConfigureDialog(m_xbv.Vc.PossibleColumnSpecs,
				new List<XmlNode>(ColumnSpecs), m_cache, stringTbl);
			dlg.RootObjectHvo = this.RootObjectHvo;

			if (m_bulkEditBar != null) // If we have a Bulk Edit bar, we should show the helpful icons
				dlg.ShowBulkEditIcons = true;

			DialogResult result = dlg.ShowDialog(this);
			if (result == DialogResult.OK)
				InstallNewColumns(dlg.CurrentSpecs);
		}

		private bool AreRemovingColumns(List<XmlNode> oldSpecs, List<XmlNode> newSpecs)
		{
			if (oldSpecs.Count > newSpecs.Count)
				return true;
			for (int i = 0; i < oldSpecs.Count; ++i)
			{
				if (!newSpecs.Contains(oldSpecs[i]))
					return true;
			}
			return false;
		}

		/// <summary>
		/// This should be called only if AreAddingColumns() returns true!
		/// </summary>
		/// <param name="oldSpecs"></param>
		/// <param name="newSpecs"></param>
		/// <returns></returns>
		private bool IsColumnOrderDifferent(List<XmlNode> oldSpecs, List<XmlNode> newSpecs)
		{
			Debug.Assert(oldSpecs.Count <= newSpecs.Count);
			for (int i = 0; i < oldSpecs.Count; ++i)
			{
				if (oldSpecs[i] != newSpecs[i])
					return true;
			}
			return false;
		}

		private void InstallNewColumns(List<XmlNode> newColumnSpecs)
		{
			if (m_bulkEditBar != null)
				m_bulkEditBar.SaveSettings(); // before we change column list!
			bool fRemovingColumn = AreRemovingColumns(ColumnSpecs, newColumnSpecs);
			bool fOrderChanged = true;
			if (!fRemovingColumn)
				fOrderChanged = IsColumnOrderDifferent(ColumnSpecs, newColumnSpecs);
			// We begin by annotating the nodes for the existing columns with the current column widths.
			// This is used to preserve widths as far as possible when recreating the list in OK.
			StoreColumnWidthsInTempAttribute(ColumnSpecs);

			// Copy configured list back to ColumnSpecs, update display, etc.x
			ColumnSpecs = newColumnSpecs;
			// Rebuild header columns based on the widths we stored in the temp attribute
			RebuildHeaderColumns(ColumnSpecs);
			// Remove the temp width attribute before we do anything else with ColumnSpecs.
			// UpdateColumnList() for instance will persist the temp values,
			// and that causes problems with detecting active columns in our configuration menu.
			// (cf. LT-2797).
			RemoveTempWidthAttribute(ColumnSpecs);
			// And, we adjusted the column widths prematurely, before we had things set up
			// so the filter bar could adjust properly, so forget we did it and allow it to
			// happen again later.
			m_colWidths = null;

			try
			{
				m_fUpdatingColumnList = true;
				UpdateColumnListAndSortingAndScrollbar(fRemovingColumn, fOrderChanged);
			}

			finally
			{
				m_fUpdatingColumnList = false;
			}
		}

		private void UpdateColumnListAndSortingAndScrollbar(bool fRemovingColumn, bool fOrderChanged)
		{
			UpdateColumnList(); // and fix everything else.

			if (fRemovingColumn)
			{
				// Reinstate sorting.  We have to do a full InitSorter because we may have
				// dropped the previously used sorter.  This will require not only
				// selecting a new sorter, but also resorting the list.
				InitSorter(Sorter);
			}
			else if (fOrderChanged)
			{
				SyncSortArrows(Sorter);
			}
			// The record list will take care of triggering any needed layout or refresh.
			// Trying to do it explicitly here led to LT-8090 and other problems.

			//LT-9785 ensures horizontal scroll bar appears when needed.
			m_scrollContainer.PerformLayout();
		}

		private void RebuildHeaderColumns(List<XmlNode> colSpecs)
		{
			m_lvHeader.BeginUpdate();
			bool fSave = m_lvHeader.AdjustingWidth;
			m_lvHeader.AdjustingWidth = true;	// Don't call AdjustColumnWidths() for each column.
			// Remove all columns (except the 'select' column if any).
			while (m_lvHeader.Columns.Count > ColumnIndexOffset())
				m_lvHeader.Columns.RemoveAt(m_lvHeader.Columns.Count - 1);
			// Build the list of column headers.
			int dpiX = GetDpiX();
			ColumnHeader[] rghdr = new ColumnHeader[colSpecs.Count];
			for (int i = 0; i < colSpecs.Count; ++i)
			{
				XmlNode node = colSpecs[i];
				ColumnHeader ch = MakeColumnHeader(node);
				ch.Width = XmlUtils.GetOptionalIntegerValue(node, "XYZYwidth",
					GetInitialColumnWidth(node, m_scrollContainer.ClientRectangle.Width, dpiX));
				if (m_xbv.Vc.ShowColumnsRTL)
				{
					int iRev = colSpecs.Count - (i + 1);
					rghdr[iRev] = ch;
				}
				else
				{
					rghdr[i] = ch;
				}
			}
			// Add the column headers, adjust the column widths as needed, and allow layout.
			m_lvHeader.Columns.AddRange(rghdr);
			m_lvHeader.AdjustingWidth = fSave;
			AdjustColumnWidths(true);
			m_lvHeader.EndUpdate();
		}

		private void StoreColumnWidthsInTempAttribute(List<XmlNode> colSpecs)
		{
			int index = 0;
			foreach(XmlNode node in colSpecs)
			{
				XmlAttribute widthAtt = node.OwnerDocument.CreateAttribute("XYZYwidth");
				widthAtt.Value = m_lvHeader.Columns[ColumnHeaderIndex(index)].Width.ToString();
				node.Attributes.Append(widthAtt);
				index++;
			}
		}

		private void RemoveTempWidthAttribute(List<XmlNode> colSpecs)
		{
			// Clean out the XYZYwidth attributes.
			foreach(XmlNode node in colSpecs)
			{
				XmlAttribute xa = node.Attributes["XYZYwidth"];
				if (xa != null)
					node.Attributes.Remove(xa);
			}
		}

		private void m_lvHeader_ColumnDragDropReordered(object sender,
			ColumnDragDropReorderedEventArgs e)
		{
			// If we have changes we need to commit, do it before we mess up the column sequence.
			if (m_bulkEditBar != null)
				m_bulkEditBar.SaveSettings(); // before we change column list!
			// Sync the Browse View columns with the new column header order.
			List<int> columnsOrder = e.DragDropColumnOrder;
			List<XmlNode> oldSpecs = new List<XmlNode>(ColumnSpecs);
			int delta = ColumnIndexOffset();
			Debug.Assert(columnsOrder.Count == ColumnSpecs.Count + delta);
			for (int i = 0; i < ColumnSpecs.Count; ++i)
			{
				m_xbv.Vc.ColumnSpecs[i] = oldSpecs[columnsOrder[i + delta] - delta];
			}
			// The drag and drop operation had a side effect of calling
			// AdjustColumnWidths. But it is too soon to do that for some things, because we
			// haven't updated all the column lists (see e.g. LT-4868). Make sure the next
			// AdjustColumnWidths, after we have finished setting up data, will really adjust.
			m_colWidths = null;

			// Display the browse view with the new column order
			this.UpdateColumnList();

			SyncSortArrows(Sorter); // Sync sort arrows
		}

		private void ConfigItemClicked(object sender, EventArgs args)
		{
			// If we have changes we need to commit, do it before we mess up the column sequence.
			if (m_bulkEditBar != null)
				m_bulkEditBar.SaveSettings(); // before we change column list!
			MenuItem mi = sender as MenuItem;
			List<XmlNode> columns = ColumnSpecs;
			List<XmlNode> possibleColumns = m_xbv.Vc.PossibleColumnSpecs;
			StringTable stringTbl = null;
			if (m_mediator != null && m_mediator.HasStringTable)
				stringTbl = m_mediator.StringTbl;
			XmlNode column = XmlViewsUtils.FindNodeWithAttrVal(possibleColumns, "label", mi.Text, stringTbl);
			int position = XmlViewsUtils.FindIndexOfMatchingNode(ColumnSpecs, column);
			bool fRemovingColumn = false;
			bool fOrderChanged = false;
			if (position >= 0)
			{
				// Was visible, make it go away. (But not the very last item.)
				if (columns.Count == 1)
				{
					MessageBox.Show(this, XMLViewsStrings.ksBrowseNeedsAColumn,
						XMLViewsStrings.ksCannotRemoveColumn);
					return;
				}
				columns.RemoveAt(position);
				m_lvHeader.Columns.RemoveAt(ColumnHeaderIndex(position));
				fRemovingColumn = true;
			}
			else
			{
				// Was invisible, make it appear.
				// Figure where to put it based on how many active columns come before it.
				//column = XmlViewsUtils.CopyWithParamDefaults(column); // not needed, param marker ignored (and confuses match).
				Menu menu = mi.Parent;
				position = 0; // will become the number of checked items before mi
				for (int i = 0; i < mi.Index; i++)
				{
					if (menu.MenuItems[i].Checked)
						position++;
				}
				if (position < columns.Count)
					fOrderChanged = true;
				InsertColumn(column, position);
			}
			try
			{
				m_fUpdatingColumnList = true;
				UpdateColumnListAndSortingAndScrollbar(fRemovingColumn, fOrderChanged);
			}
			finally
			{
				m_fUpdatingColumnList = false;
			}
		}

		private FilterBarCellFilter MakeFilter(List<XmlNode> possibleColumns, string colName, IMatcher matcher)
		{
			XmlNode colSpec = XmlViewsUtils.FindNodeWithAttrVal(possibleColumns, "label", colName, null);
			if (colSpec == null)
				return null;
			IStringFinder finder = LayoutFinder.CreateFinder(m_cache, colSpec, BrowseView.Vc);
			return new FilterBarCellFilter(finder, matcher);
		}

		/// <summary>
		/// This method is used to set the filters for
		/// Edit Spelling Status   "TeReviewUndecidedSpelling"
		/// and
		/// View Incorrect Words in Use  "TeCorrectSpelling"
		///
		/// Some aspects of the initial state of the tab can be controlled by setting properties from
		/// an FwLink which brought us here. If this is the case, generate a corresponding filter and
		/// return it; otherwise, return null.
		/// </summary>
		public RecordFilter FilterFromLink()
		{
			string linkSetupInfo = m_mediator.PropertyTable.GetStringProperty("LinkSetupInfo", null);
			if (linkSetupInfo == null)
				return null;
			m_mediator.PropertyTable.RemoveProperty("LinkSetupInfo");
			if (linkSetupInfo != "TeReviewUndecidedSpelling" && linkSetupInfo != "TeCorrectSpelling")
				return null; // Only setting we know as yet.

			List<XmlNode> possibleColumns = m_xbv.Vc.ComputePossibleColumns();

			XmlNode colSpec = XmlViewsUtils.FindNodeWithAttrVal(possibleColumns, "label", "Spelling Status", null);
			if (colSpec == null)
				return null;
			int desiredItem;
			if (linkSetupInfo == "TeCorrectSpelling")
				desiredItem = (int)FDO.Ling.SpellingStatusStates.correct;
			else  //linkSetupInfo == "TeReviewSpelling"
				desiredItem = (int)FDO.Ling.SpellingStatusStates.undecided;

			string[] labels = BrowseView.GetStringList(colSpec);
			if (labels == null || labels.Length < desiredItem)
				return null;
			string correctLabel = labels[desiredItem];

			FilterBarCellFilter spellFilter;
			if (linkSetupInfo == "TeCorrectSpelling")   //"Exclude Correct" TE-8200
				// Use this one for NOT Correct ("Exclude Correct"), that is, undecided OR incorrect,
				// all things that have squiggles. (could also be "Exclude Incorrect" or "Exclude Undecided")
				spellFilter = MakeFilter(possibleColumns, "Spelling Status",
					new InvertMatcher(new ExactMatcher(m_filterBar.MatchExactPattern(correctLabel))));
			else // linkSetupInfo == "TeReviewUndecidedSpelling"   --> "Undecided"
				spellFilter = MakeFilter(possibleColumns, "Spelling Status",
					new ExactMatcher(m_filterBar.MatchExactPattern(correctLabel)));

			FilterBarCellFilter occurrenceFilter = MakeFilter(possibleColumns, "Number in Corpus",
				new RangeIntMatcher(1, Int32.MaxValue));
			AndFilter andFilter = new AndFilter();
			andFilter.Add(spellFilter);
			andFilter.Add(occurrenceFilter);
			// If we need to change the list of objects, best to do it now, before we load the list
			// because of the change filter, and suppress the next load.
			SetupLinkScripture();
			return andFilter;
		}

		/// <summary>
		/// This is broadcast after a link is followed. It allows us to set up the desired filter
		/// etc. even if the desired tool was already active.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		public bool OnLinkFollowed(object args)
		{
			SetupLinkScripture();
			RecordFilter filter = FilterFromLink();
			if (filter == null)
				return false;
			if (filter == m_currentFilter)
				return true;
			if (FilterChanged != null)
				FilterChanged(this, new FilterChangeEventArgs(filter, m_currentFilter));
			m_fFilterInitializationComplete = false; // allows UpdateFilterBar to add columns
			UpdateFilterBar(filter);
			m_scrollContainer.PerformLayout(); // cause scroll bar to appear or disappear, etc.
			return true;
		}

		void SetupLinkScripture()
		{
			string booksWanted = m_mediator.PropertyTable.GetStringProperty("LinkScriptureBooksWanted", null);
			if (booksWanted == null)
				return;
			m_mediator.PropertyTable.RemoveProperty("LinkScriptureBooksWanted");
			int[] bookHvos;
			if (booksWanted == "all")
				bookHvos = m_cache.LangProject.TranslatedScriptureOA.ScriptureBooksOS.HvoArray;
			else
				return; // Enhance JohnT: accept a list of books in some form or other.
			List<int> hvosFromScripture = new List<int>(0);
			foreach (int hvoBook in bookHvos)
			{
				IScrBook book = (IScrBook)CmObject.CreateFromDBObject(m_cache, hvoBook);
				foreach (IScrSection section in book.SectionsOS)
				{
					hvosFromScripture.Add(section.ContentOAHvo);
					hvosFromScripture.Add(section.HeadingOAHvo);
				}
			}

			// It's an  as InterlinearTextsVirtualHandler, but we can't use that class here because
			// it's implemented in ITextDll, which we can't reference without circularity.
			IVwVirtualHandler vh = BaseVirtualHandler.GetInstalledHandler(m_cache,
							"LangProject", "InterlinearTexts");
			if (vh == null)
				return; // can't do anything.
			PropertyInfo infoAsi = vh.GetType().GetProperty("CanAccessScriptureIds");
			if (infoAsi == null)
				return;
			MethodInfo info = vh.GetType().GetMethod("UpdateList");
			if (info == null)
				return; // can't do anything.
			infoAsi.SetValue(vh, true, null);
			info.Invoke(vh, new object[] { hvosFromScripture.ToArray() });
		}

		/// <summary>
		/// Allows client to substitute a different column spec for a current column.
		/// </summary>
		/// <param name="index"></param>
		/// <param name="colSpec"></param>
		public XmlNode ReplaceColumn(int index, XmlNode colSpec)
		{
			// If we have changes we need to commit, do it before we mess up the column sequence.
			if (m_bulkEditBar != null)
				m_bulkEditBar.SaveSettings(); // before we change column list!
			XmlNode result = ColumnSpecs[index];
			ColumnSpecs[index] = colSpec; // will throw if bad index...but we'd just throw anyway.
			UpdateColumnList();
			return result;
		}

		/// <summary>
		/// Allows client to substitute a different column spec for a current column.
		/// </summary>
		/// <param name="colName"></param>
		/// <param name="colSpec"></param>
		public XmlNode ReplaceColumn(string colName, XmlNode colSpec)
		{
			int index = IndexOfColumn(colName);
			Debug.Assert(index >= 0, "looking for invalid column in view");
			return ReplaceColumn(index, colSpec);
		}

		// Get the index of the column that has the specified name/label
		private int IndexOfColumn(string colName)
		{
			return ColumnSpecs.FindIndex(delegate(XmlNode item)
			{ return XmlUtils.GetAttributeValue(item, "label") == colName; });
		}

		/// <summary>
		/// Insert column at the given position.
		/// </summary>
		/// <param name="colSpec">xml specification for the column</param>
		/// <param name="position">column index</param>
		protected void InsertColumn(XmlNode colSpec, int position)
		{
			List<XmlNode> columns = ColumnSpecs;
			columns.Insert(position, colSpec);
			ColumnHeader colHeader = MakeColumnHeader(colSpec);
			int colWidth = GetInitialColumnWidth(colSpec, m_scrollContainer.ClientRectangle.Width, GetDpiX());
			colHeader.Width = colWidth;
			m_lvHeader.SuppressColumnWidthChanges = true;
			m_lvHeader.Columns.Insert(ColumnHeaderIndex(position), colHeader);
			m_lvHeader.SuppressColumnWidthChanges = false;
			// Setting the column width and inserting it into the header had a side effect of calling
			// AdjustColumnWidths. But it is too soon to do that for some things, because we
			// haven't updated all the column lists (see e.g. LT-4804). Make sure the next
			// AdjustColumnWidths, after we have finished setting up data, will really adjust.
			m_colWidths = null;
			bool fNeedLayout = colHeader.Width < colWidth;
			if (fNeedLayout)
			{
				// truncated because the header as a whole is too narrow.
				m_lvHeader.Width += colWidth - colHeader.Width;
				colHeader.Width = colWidth;
			}
		}

		// Note: often we also want to update LayoutCache.LayoutVersionNumber.
		internal const int kBrowseViewVersion = 13;

		/// <summary>
		/// Column has been added or removed, update all child windows.
		/// </summary>
		protected void UpdateColumnList()
		{
			using (new ReconstructPreservingBVScrollPosition(this))
			{
				//m_xbv.UpdateColumnList(); // Only did RootBox.Reconstruct()
				if (m_filterBar != null)
				{
					m_filterBar.UpdateColumnList();
					// see if we can re-enstate our column filters
					m_filterBar.UpdateActiveItems(m_currentFilter);
					// see if we need to re-enstate our column header sort arrow.
					this.InitSorter(this.Sorter);
				}
				if (m_bulkEditBar != null)
					m_bulkEditBar.UpdateColumnList();
				m_lvHeader.AdjustWidth(0); // adjust to fit
				// That doesn't fix columns added at the end, which the .NET code helpfully adjusts to
				// one pixel wide each if the earlier columns use all available space!
				int ccols = m_lvHeader.Columns.Count;
				for (int iAdjustCol = ccols - 1; iAdjustCol > 0; iAdjustCol--)
				{
					int adjust = DhListView.kMinColWidth - m_lvHeader.Columns[ccols - 1].Width;
					if (adjust <= 0)
						break; // only narrow columns at the end are a problem
					for (int icol = iAdjustCol - 1; icol >= 0 && adjust > 0; icol--)
					{
						// See if we can narrow column icol by enough to fix things.
						int avail = m_lvHeader.Columns[icol].Width - DhListView.kMinColWidth;
						if (avail > 0)
						{
							int delta = Math.Min(avail, adjust);
							adjust -= delta;
							m_lvHeader.Columns[icol].Width -= delta;
						}
					}
				}
			} // End using(ReconstructPreservingBVScrollPosition) [Does RootBox.Reconstruct() here.]

			// Make everthing else match, but do NOT remember settings.
			// This method gets called when adding a column to make a filter visible,
			// and should not interfere with any widths previously saved.
			AdjustColumnWidths(false);

			// And have the mediator remember the list of columns the user wants.
			// This information is used in the XmlBrowseViewBaseVc constructor to select the
			// columns to display.
			// Exception: if any column has 'doNotPersist="true"' skip saving.
			StringBuilder colList = new StringBuilder();
			colList.Append("<root version=\"" + kBrowseViewVersion + "\">");
			foreach (XmlNode node in ColumnSpecs)
			{
				if (XmlUtils.GetOptionalBooleanAttributeValue(node, "doNotPersist", false))
					return; // without saving column info!
				string layout = XmlUtils.GetOptionalAttributeValue(node, "layout");
				string normalLayout = XmlUtils.GetOptionalAttributeValue(node, "normalLayout");
				if (layout != null && normalLayout != null && layout != normalLayout)
				{
					// persist it with the normal layout, not the special edit one.
					XmlUtils.SetAttribute(node, "layout", normalLayout);
					colList.Append(node.OuterXml);
					XmlUtils.SetAttribute(node, "layout", layout);
				}
				else
				{
					colList.Append(node.OuterXml);
				}
			}
			colList.Append("</root>");
			m_xbv.Mediator.PropertyTable.SetProperty(m_xbv.Vc.ColListId, colList.ToString(), XCore.PropertyTable.SettingsGroup.LocalSettings);
		}

		/// <summary>
		/// Append hidden columns that match the given (active) filter to our active columns list.
		/// </summary>
		/// <param name="filter"></param>
		internal bool AppendMatchingHiddenColumns(RecordFilter filter)
		{
			CheckDisposed();

			if (FilterInitializationComplete || filter == null)
				return false;
			bool fInsertedColumn = false;
			foreach (XmlNode colSpec in m_xbv.Vc.ComputePossibleColumns())
			{
				// detect if hidden column (hidden columns are not in our active ColumnSpecs)
				if(IsColumnHidden(colSpec))
				{
					if(m_filterBar.CanActivateFilter(filter, colSpec))
					{
						// append column to end of column list
						this.AppendColumn(colSpec);
						fInsertedColumn = true;
					}
				}
			}
			return fInsertedColumn;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="colSpec"></param>
		/// <returns></returns>
		protected internal bool IsColumnShowing(XmlNode colSpec)
		{
			return XmlViewsUtils.FindIndexOfMatchingNode(ColumnSpecs, colSpec) >= 0;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="colSpec"></param>
		/// <returns></returns>
		protected internal bool IsColumnHidden(XmlNode colSpec)
		{
			return !IsColumnShowing(colSpec);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="colSpec"></param>
		protected internal void AppendColumn(XmlNode colSpec)
		{
			this.InsertColumn(colSpec, ColumnSpecs.Count);
		}

		internal List<XmlNode> ColumnSpecs
		{
			get
			{
				CheckDisposed();

				return m_xbv.Vc.ColumnSpecs;
			}
			set
			{
				CheckDisposed();

				m_xbv.Vc.ColumnSpecs = value;
			}
		}

		/// <summary>
		/// Create a sorter for the first column that is in the desired writing system
		/// class (vernacular or analysis).
		/// </summary>
		/// <remarks>This is needed for LT-10293.</remarks>
		public RecordSorter CreateSorterForFirstColumn(bool fVern, int ws)
		{
			RecordSorter sorter = null;
			XmlNode colSpec = null;
			string sWs = m_cache.LanguageWritingSystemFactoryAccessor.GetStrFromWs(ws);
			for (int i = 0; i < m_xbv.Vc.ColumnSpecs.Count; ++i)
			{
				XmlNode spec = m_xbv.Vc.ColumnSpecs[i];
				string sWsAttr = XmlUtils.GetOptionalAttributeValue(spec, "ws");
				string sOrigWsAttr = XmlUtils.GetOptionalAttributeValue(spec, "originalWs");
				if (String.IsNullOrEmpty(sWsAttr))
					continue;
				if (sWsAttr.StartsWith("$ws="))
					sWsAttr = sWsAttr.Substring(4);
				if (fVern)
				{
					if ((sWsAttr == "vernacular" && ws == m_cache.DefaultVernWs) ||
						sWsAttr == sWs)
					{
						colSpec = spec;
						break;
					}
				}
				else
				{
					if ((sWsAttr == "analysis" && ws == m_cache.DefaultAnalWs) ||
						sWsAttr == sWs)
					{
						colSpec = spec;
						break;
					}
				}
			}
			if (colSpec != null)
			{
				IStringFinder finder = LayoutFinder.CreateFinder(m_cache, colSpec, m_xbv.Vc);
				int wsT = LangProject.GetWritingSystem(colSpec, m_cache, null, 0);
				if (wsT == 0)
					wsT = fVern ? m_cache.DefaultVernWs : m_cache.DefaultAnalWs;
				string sWsT = m_cache.LanguageWritingSystemFactoryAccessor.GetStrFromWs(wsT);
				Debug.Assert(sWsT == sWs);
				sorter = new GenRecordSorter(new StringFinderCompare(finder, new IcuComparer(sWsT)));
			}
			return sorter;
		}

		private void m_xbv_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (SelectedIndexChanged != null)
				SelectedIndexChanged(this, new EventArgs());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Enable the "Sort from end" menu command.
		/// </summary>
		/// <param name="commandObject">The command object.</param>
		/// <param name="display">The display.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool OnDisplaySortedFromEnd(object commandObject,
			ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			display.Visible = true;
			display.Enabled = true;
			display.Checked = this.CurrentColumnSortedFromEnd;
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Enable the "Sort from end" menu command.
		/// </summary>
		/// <param name="commandObject">The command object.</param>
		/// <param name="display">The display.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool OnDisplaySortedByLength(object commandObject,
			ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			int icol = -1;	// can't sort without a filter bar!
			if (m_filterBar != null)
				icol = m_icolCurrent - m_filterBar.ColumnOffset;
			if (icol < 0)
			{
				display.Visible = false;	// should never reach here
				display.Enabled = false;
			}
			else
			{
				bool fCanSortByLength = XmlUtils.GetOptionalBooleanAttributeValue(
					ColumnSpecs[icol] as XmlElement, "cansortbylength", false);
				display.Visible = fCanSortByLength;
				display.Enabled = fCanSortByLength;
				display.Checked = this.CurrentColumnSortedByLength;
			}
			return true;
		}

		private RecordSorter GetCurrentColumnSorter()
		{
			return GetColumnSorter(m_icolCurrent);
		}

		private RecordSorter GetColumnSorter(int icol)
		{
			if (m_filterBar == null)
				return null;
			int ifsi = icol - m_filterBar.ColumnOffset;
			if (ifsi < 0)
				return null;			// Should not happen.
			FilterSortItem fsi = m_filterBar.ColumnInfo[ifsi];
			if (fsi == null)
				return null;			// Should not happen.
			return fsi.Sorter;
		}

		/// <summary>
		/// Receives the broadcast message "PropertyChanged".  This message results from left clicking
		/// in the context menu generated by a right mouse button click in the column headers.
		/// </summary>
		public void OnPropertyChanged(string name)
		{
			CheckDisposed();

			// REVISIT: If more than one BrowseViewer gets this message, how do
			// we know which one should handle it?  Can't we handle this some other way?
			if (name == "SortedFromEnd" && m_filterBar != null && this.Sorter != null)
			{
				this.CurrentColumnSortedFromEnd = !this.CurrentColumnSortedFromEnd;
				SetAndRaiseSorter(Sorter, true);
			}
			if (name == "SortedByLength" && m_filterBar != null && this.Sorter != null)
			{
				this.CurrentColumnSortedByLength = !this.CurrentColumnSortedByLength;
				SetAndRaiseSorter(Sorter, true);
			}
		}

		#region IxCoreColleague Members

		/// <summary>
		/// Initialize as an xCore colleague. Currently this just passes the information on to the
		/// main XmlBrowseView.
		/// </summary>
		/// <param name="mediator"></param>
		/// <param name="configurationParameters"></param>
		public void Init(Mediator mediator, XmlNode configurationParameters)
		{
			CheckDisposed();

			base.m_configurationParameters = configurationParameters;
			m_xbv.Init(mediator, configurationParameters);
			m_xbv.AccessibleName = "BrowseViewer";
			m_mediator = mediator;
		}

		internal Mediator Mediator
		{
			get
			{
				CheckDisposed();

				if (m_mediator != null)
					return m_mediator;
				if (m_xbv != null)
					return m_xbv.Mediator; // sometimes set before our own
				return null;
			}
		}

		/// <summary>
		/// Currently interesting targets are the browse view and this object.
		/// </summary>
		/// <returns></returns>
		public IxCoreColleague[] GetMessageTargets()
		{
			CheckDisposed();

			return new IxCoreColleague[] { m_xbv, this };
		}

		#endregion

		private void m_checkMarkButton_Click(object sender, EventArgs e)
		{
			m_lvHeader_CheckIconClick(this, new ConfigIconClickEventArgs(
					this.RectangleToClient(m_checkMarkButton.RectangleToScreen(m_checkMarkButton.ClientRectangle))));
		}

		private void m_lvHeader_CheckIconClick(object sender, ConfigIconClickEventArgs e)
		{
			ContextMenu menu = new ContextMenu();
			menu.MenuItems.Add(XMLViewsStrings.ksCheckAll, new EventHandler(OnCheckAll));
			menu.MenuItems.Add(XMLViewsStrings.ksUncheckAll, new EventHandler(OnUncheckAll));
			menu.MenuItems.Add(XMLViewsStrings.ksToggle, new EventHandler(OnToggleAll));
			menu.Show(this, new Point(e.Location.Left, e.Location.Bottom));
		}


		/// <summary>
		/// Get all the main items in this browse view.
		/// </summary>
		public List<int> AllItems
		{
			get
			{
				return new List<int>(Cache.GetVectorProperty(this.RootObjectHvo, this.MainTag, false));
			}
		}

		/// <summary>
		/// Get all the main items with a check mark in this browse view;
		/// </summary>
		public List<int> CheckedItems
		{
			get
			{
				CheckDisposed();
				if (!m_xbv.Vc.HasSelectColumn)
					return null;

				int hvoRoot = this.RootObjectHvo;
				int tagMain = this.MainTag;
				List<int> checkedItems = new List<int>();
				ISilDataAccess sda = this.Cache.MainCacheAccessor;
				int citems = sda.get_VecSize(hvoRoot, tagMain);
				for (int i = 0; i < citems; ++i)
				{
					int hvoItem = sda.get_VecItem(hvoRoot, tagMain, i);
					// If there's no value in the cache, we still want to add it
					// if the default state is CHECKED (cf XmlBrowseViewBaseVc.LoadData).
					if (GetCheckState(hvoItem) == 1)
						checkedItems.Add(hvoItem);
				}
				return checkedItems;
			}
		}

		/// <summary>
		/// Make sure all items in rghvo are checked.
		/// </summary>
		/// <param name="rghvo"></param>
		public void SetCheckedItems(IList<int> rghvo)
		{
			CheckDisposed();

			if (!m_xbv.Vc.HasSelectColumn)
				return;

			List<int> changedHvos = new List<int>();
			foreach (int hvoItem in Cache.GetVectorProperty(RootObjectHvo, MainTag, true))
			{
				int newVal = (rghvo != null && rghvo.Contains(hvoItem)) ? 1 : 0;
				int currentValue = GetCheckState(hvoItem);
				SetItemCheckedState(hvoItem, newVal, currentValue != newVal);
				if (currentValue != newVal)
					changedHvos.Add(hvoItem);
			}
			OnCheckBoxChanged(changedHvos.ToArray());
		}

		/// <summary>
		/// Determine whether the specified item is currently considered to be checked.
		/// </summary>
		/// <param name="hvoItem"></param>
		/// <returns></returns>
		public bool IsItemChecked(int hvoItem)
		{
			return GetCheckState(hvoItem) == 1;
		}

		private bool m_fIsInitialized = false;
		private bool m_fInUpdateCheckedItems = false;
		/// <summary>
		/// this is somewhat of a kludge, since there is kind of a circular dependency
		/// between checked items in a browse viewer (e.g. when in bulk edit Delete tab)
		/// and the record list managing that list.
		/// The record list depends upon the BulkEdit settings for loading its list, so
		/// the bulk edit bar loads before the RecordList. However, at least one BulkEdit
		/// tab (Delete) has logic that enables/disables certain items in that list,
		/// which won't become available until after the list has been setup.
		/// To get around this (for now), make sure the browse viewer now has a chance
		/// to put the checkboxes in the right state after the list has been loaded.
		/// </summary>
		public void UpdateCheckedItems()
		{
			m_fInUpdateCheckedItems = true;
			try
			{
				if (m_xbv != null && m_xbv.Vc != null && m_xbv.Vc.HasSelectColumn && this.BulkEditBar != null)
				{
					// everything seems to be setup, and UpdateCheckedItems should be called
					// after everything is setup.
					m_fIsInitialized = true;
					// we want the current items list to inherit their selection status
					// from its relatives on a previous list
					// (LT-8986)
					if (m_sortItemProvider != null && m_sortItemProvider is IMultiListSortItemProvider)
					{
						if (m_fSavedSelectionsDuringFilterChange)
						{
							RestoreSelectionItems();
							m_fSavedSelectionsDuringFilterChange = false;
						}
						else if (m_lastChangedSelectionListItemsClass != 0 &&
								 m_lastChangedSelectionListItemsClass != m_xbv.Vc.ListItemsClass)
						{
							ConvertItemsToRelativesThatApplyToCurrentListSelections();
							RestoreSelectionItems();
						}
					}
					this.BulkEditBar.UpdateCheckedItems();
				}
			}
			finally
			{
				m_fInUpdateCheckedItems = false;
			}
		}

		/// <summary>
		/// Forces the root box of the main browse view component to fully recompute.
		/// Attempts to preserve the scroll position; tries even harder to preserve
		/// the visibility of the selected row, if it is visible.
		/// </summary>
		public void ReconstructView()
		{
			using (new ReconstructPreservingBVScrollPosition(this))
			{

			}
		}

		/// <summary>
		/// Determine whether the specified item is currently considered to be checked.
		/// </summary>
		/// <param name="hvoItem"></param>
		/// <returns></returns>
		internal int GetCheckState(int hvoItem)
		{
			ISilDataAccess sda = m_cache.MainCacheAccessor;
			if (sda.get_IsPropInCache(hvoItem, XmlBrowseViewVc.ktagItemSelected,
				(int)CellarModuleDefns.kcptInteger, 0))
			{
				return sda.get_IntProp(hvoItem, XmlBrowseViewVc.ktagItemSelected);
			}
			// If there's no value in the cache, use the value implied by the Vc's default.
			return m_xbv.Vc.DefaultChecked ? 1 : 0;
		}

		private void OnCheckAll(object sender, EventArgs e)
		{
			ResetAll(CheckState.CheckAll);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Actually checks if val is 1, unchecks if val is 0.
		/// Toggles if value is -1
		/// </summary>
		/// <param name="newState">The new state.</param>
		/// ------------------------------------------------------------------------------------
		private void ResetAll(CheckState newState)
		{
			List<int> changedHvos = new List<int>();
			foreach (int hvoItem in Cache.GetVectorProperty(RootObjectHvo, MainTag, true))
			{
				int newVal = 0;
				int currentValue = GetCheckState(hvoItem);
				switch (newState)
				{
					case CheckState.ToggleAll:
						newVal = (currentValue == 0) ? 1 : 0;
						break;
					case CheckState.CheckAll:
						newVal = 1;
						break;
					case CheckState.UncheckAll:
						newVal = 0;
						break;
				}
				SetItemCheckedState(hvoItem, newVal, false);
				if (currentValue != newVal)
					changedHvos.Add(hvoItem);
			}
			if (m_bulkEditBar != null && m_bulkEditBar.Visible)
				m_bulkEditBar.SetEnabledIfShowing();
			OnCheckBoxChanged(changedHvos.ToArray());
			using (new ReconstructPreservingBVScrollPosition(this))
			{
			}
		}

		internal void SetItemCheckedState(int hvoItem, bool selectItem, bool propertyDidChange)
		{
			SetItemCheckedState(hvoItem, Convert.ToInt32(selectItem), propertyDidChange);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="hvoItem"></param>
		/// <param name="newVal"></param>
		/// <param name="propertyDidChange"></param>
		protected void SetItemCheckedState(int hvoItem, int newVal, bool propertyDidChange)
		{
			// Note that we want to set it even if the value apparently hasn't changed,
			// because a value of zero might just be a default from finding nothing in
			// the cache, but 'not found' might actually be interpreted as 'checked'.
			Cache.VwCacheDaAccessor.CacheIntProp(hvoItem, XmlBrowseViewVc.ktagItemSelected, newVal);
			if (propertyDidChange)
			{
				Cache.MainCacheAccessor.PropChanged(
					null,
					(int)PropChangeType.kpctNotifyAll,
					hvoItem,
					XmlBrowseViewVc.ktagItemSelected,
					0, 1, 1);
			}
		}
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		protected void OnUncheckAll(object sender, EventArgs e)
		{
			ResetAll(CheckState.UncheckAll);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		protected void OnToggleAll(object sender, EventArgs e)
		{
			ResetAll(CheckState.ToggleAll);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		protected void m_configureButton_Click(object sender, EventArgs e)
		{
			HandleConfigIconClick(new ConfigIconClickEventArgs(
				this.RectangleToClient(m_configureButton.RectangleToScreen(m_configureButton.ClientRectangle))));
		}

		//private void m_scrollBar_Scroll(object sender, ScrollEventArgs e)
		//{
		//    m_xbv.ScrollPosition = new Point(0, m_scrollBar.Value);
		//}
		void m_scrollBar_ValueChanged(object sender, EventArgs e)
		{
			m_xbv.ScrollPosition = new Point(0, m_scrollBar.Value);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Snaps the split position.
		/// </summary>
		/// <param name="width">The width.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool SnapSplitPosition(ref int width)
		{
			CheckDisposed();

			if (m_lvHeader == null || m_lvHeader.Columns.Count < 1)
				return false;
			int snapPos = m_lvHeader.Columns[0].Width + m_scrollBar.Width;
			// This guards against snapping when we have just one column that is stretching.
			// When that happens, 'snapping' just prevents other behaviors, like
			if (m_lvHeader.Columns.Count == 1 && snapPos >= width - 2)
				return false;
			if (width < snapPos + 10 && width > snapPos / 2)
			{
				width = snapPos;
				return true;
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when [remove filters].
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// ------------------------------------------------------------------------------------
		public void OnRemoveFilters(object sender)
		{
			CheckDisposed();

			if (m_filterBar != null)
				m_filterBar.RemoveAllFilters();
		}

		/// <summary>
		/// Launch dialog for configuring browse view column choices.
		/// </summary>
		/// <param name="sender">Command</param>
		/// <returns></returns>
		public bool OnConfigureColumns(object sender)
		{
			CheckDisposed();

			this.ConfigMoreChoicesItemClicked(sender, new EventArgs());
			return true;
		}

		#region IxCoreContentControl Members

		/// <summary>
		///
		/// </summary>
		public string AreaName
		{
			get
			{
				CheckDisposed();
				return XmlUtils.GetOptionalAttributeValue(m_configurationParameters, "area", "unknown");
			}
		}

		/// <summary>
		/// This is called on a MasterRefresh
		/// </summary>
		/// <returns></returns>
		public bool PrepareToGoAway()
		{
			CheckDisposed();
			if (m_bulkEditBar != null)
				m_bulkEditBar.SaveSettings();
			return true;
		}

		#endregion IxCoreContentControl Members

		#region IxCoreCtrlTabProvider Members

		/// <summary>
		///
		/// </summary>
		/// <param name="targetCandidates"></param>
		/// <returns></returns>
		public Control PopulateCtrlTabTargetCandidateList(List<Control> targetCandidates)
		{
			CheckDisposed();
			// Note: when switching panes, we want to give the focus to the BrowseView, not the BrowseViewer.
			Control focusedControl = null;
			if (this.BrowseView != null)
			{
				targetCandidates.Add(this.BrowseView);
				if (this.BrowseView.ContainsFocus)
					focusedControl = this.BrowseView;
			}
			if (this.m_bulkEditBar != null)
			{
				targetCandidates.Add(this.m_bulkEditBar);
				if (focusedControl == null && this.m_bulkEditBar.ContainsFocus)
					focusedControl = this.m_bulkEditBar;
			}
			return focusedControl;
		}

		#endregion

		/// <summary>
		/// Expose the property of the underlying view.
		/// </summary>
		public XmlBrowseViewBase.SelectionHighlighting SelectedRowHighlighting
		{
			get { return m_xbv.SelectedRowHighlighting; }
			set { m_xbv.SelectedRowHighlighting = value; }
		}

		// In your AllItems list, the specified objects have been replaced (typically dummy to real).
		internal void FixReplacedItems(Dictionary<int, int> replacedObjects)
		{
			int[] items = Cache.GetVectorProperty(this.RootObjectHvo, this.MainTag, false);
			IVwCacheDa cda = Cache.VwCacheDaAccessor;
			ISilDataAccess sda = Cache.MainCacheAccessor;
			for (int i = 0; i < items.Length; i++)
			{
				int rep;
				if (replacedObjects.TryGetValue(items[i], out rep))
				{
					int old = items[i];
					items[i] = rep;
					if (sda.get_IsPropInCache(old, XmlBrowseViewVc.ktagItemSelected,
						(int)CellarModuleDefns.kcptInteger, 0))
					{
						cda.CacheIntProp(rep, XmlBrowseViewVc.ktagItemSelected,
							sda.get_IntProp(old, XmlBrowseViewVc.ktagItemSelected));
					}
					cda.ClearInfoAbout(old, VwClearInfoAction.kciaRemoveObjectInfoOnly);
				}
			}
			cda.CacheVecProp(this.RootObjectHvo, this.MainTag, items, items.Length);
		}
	}

	/// <summary>
	/// This class manages the parts of the BrowseViewer that scroll horizontally in sync.
	/// </summary>
	public class BrowseViewScroller : UserControl, IFWDisposable
	{
		BrowseViewer m_bv;

		/// <summary>
		///
		/// </summary>
		/// <param name="bv"></param>
		public BrowseViewScroller(BrowseViewer bv)
		{
			m_bv = bv;
		}
		/// <summary>
		///
		/// </summary>
		/// <param name="levent"></param>
		protected override void OnLayout(LayoutEventArgs levent)
		{
			m_bv.LayoutScrollControls();
			// It's important to do this AFTER laying out the embedded controls, because it figures
			// out whether to display the scroll bar based on their sizes and positions.
			base.OnLayout (levent);
		}
		/// <summary>
		///
		/// </summary>
		/// <param name="e"></param>
		protected override void OnMouseWheel(MouseEventArgs e)
		{
			// Suppress horizontal scrolling if we are doing vertical.
			if (m_bv != null && m_bv.ScrollBar != null && m_bv.ScrollBar.Maximum >= m_bv.ScrollBar.LargeChange)
				return;
			base.OnMouseWheel(e);
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
		/// <summary>
		///
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
			}
			m_bv = null;

			base.Dispose( disposing );
		}
	}

	/// <summary>
	/// This class maintains the vertical scroll position in the BrowseViewer when a RootBox.Reconstruct() is necessary.
	/// It is intended to be used in a using() construct, so that its Dispose() forces a RootBox.Reconstruct()
	/// at the end of the using block and then makes sure the scroll position is valid.
	/// </summary>
	internal class ReconstructPreservingBVScrollPosition : IFWDisposable
	{
		BrowseViewer m_bv;
		int m_irow;
		bool m_fHiliteWasVisible;

		/// <summary>
		/// Ctor saves BrowseViewer Scroll Position. Dispose(true) does RootBox.Reconstruct() and restores scroll position.
		/// </summary>
		/// <param name="bv"></param>
		public ReconstructPreservingBVScrollPosition(BrowseViewer bv)
		{
			m_bv = bv;

			// Store location for restore after Reconstruct. (LT-8336)
			m_bv.BrowseView.OnSaveScrollPosition(null); // says it's called through Mediator, but not that I can see!

			// Figure out if highlighted row is visible or not
			m_irow = m_bv.SelectedIndex;
			m_fHiliteWasVisible = false;
			if (m_irow < 0)
				return;
			IVwSelection sel = MakeTestRowSelection(m_irow);
			if (sel == null)
				return;
			if (m_bv.BrowseView.IsSelectionVisible(sel))
				m_fHiliteWasVisible = true;
		}

		private IVwSelection MakeTestRowSelection(int iselRow)
		{
			SelLevInfo[] rgvsli = new SelLevInfo[1];
			rgvsli[0].ihvo = iselRow;
			rgvsli[0].tag = m_bv.MainTag;
			return m_bv.BrowseView.RootBox.MakeTextSelInObj(0, 1, rgvsli, 0, null, false, false, false, true, false);
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
		~ReconstructPreservingBVScrollPosition()
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
				// Do this in the context of Scenario 2,
				// since in #1 the m_bv may have been finalized already.
				// Restore scroll position here
				m_bv.BrowseView.RootBox.Reconstruct(); // Otherwise every cell redraws individually!

				m_bv.BrowseView.OnRestoreScrollPosition(null);

				if (m_fHiliteWasVisible)
				{
					// If there WAS a highlighted row visible and it is no longer visible, scroll to make it so.
					IVwSelection newSel = MakeTestRowSelection(m_irow);
					if (newSel != null && !m_bv.BrowseView.IsSelectionVisible(newSel)) // Need to scroll newSel into view
						m_bv.BrowseView.RestoreScrollPosition(Math.Max(0, m_irow - 2));
				}

				m_bv = null;
			}

			// Dispose unmanaged resources here, whether disposing is true or false.

			//// If our action handler is a COM object and it we received it through
			//// a COM object we have to release it. If it is a COM object but we
			//// received it from a managed object then the managed object will/should
			//// take care to release it.
			//if (Marshal.IsComObject(m_dataAccess))
			//{
			//    if (m_actionHandler != null && Marshal.IsComObject(m_actionHandler))
			//        Marshal.ReleaseComObject(m_actionHandler);
			//    if (m_acthTemp != null && Marshal.IsComObject(m_acthTemp))
			//        Marshal.ReleaseComObject(m_acthTemp);
			//}
			//m_acthTemp = null;
			//m_actionHandler = null;

			//m_vwRootSite = null;
			//m_dataAccess = null;

			m_isDisposed = true;
		}

		/// <summary>
		/// Throw if the IsDisposed property is true
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(ToString(), "This object is being used after it has been disposed: this is an Error.");
		}

		#endregion IDisposable & Co. implementation
	}

	#endregion BrowseViewer class
}
