// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Areas;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.DomainServices;
using LanguageExplorer.Filters;
using SIL.FieldWorks.Resources;
using SIL.Reporting;
using SIL.LCModel.Utils;
using SIL.Xml;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// BrowseViewer is a container for various windows related to browsing. At a minimum it contains
	/// a DhListView which provides the column headers and an XmlBrowseView that contains the
	/// actual browse view. It may also have a FilterBar, and eventually other controls, e.g.,
	/// for filling in columns of data.
	/// </summary>
	internal class BrowseViewer : MainUserControl, ISnapSplitPosition, IMainContentControl, IPostLayoutInit, IRefreshableRoot
	{
		private readonly DisposableObjectsSet<RecordSorter> m_SortersToDispose = new DisposableObjectsSet<RecordSorter>();
		private XElement m_configParamsElement;
		/// <summary />
		protected DhListView m_lvHeader;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private Container components;

		/// <summary />
		protected internal RecordFilter m_currentFilter;
#if RANDYTODO
		// TODO: Think about the refactor idea. No sense in having "bad design" hang around, if it can be fixed up.
#endif
		private int m_lastLayoutWidth;

		/// <summary />
		protected int m_icolCurrent;
		/// <summary />
		protected Button m_configureButton;
		private Button m_checkMarkButton;

		private ToolTip m_tooltip;

		private int[] m_colWidths; // Last values computed and set by AdjustColumnWidths.
		// This flag is used to minimize redoing the filtering and sorting when
		// changing the list of columns shown.
		private bool m_fUpdatingColumnList;

		/// <summary />
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
		public event FwSelectionChangedEventHandler SelectionChanged;
		/// <summary>
		/// This event notifies you that a selection was made with a double click, passing an argument
		/// containing both the index and the hvo of the selected object.  This happens currently only
		/// for read-only views.
		/// </summary>
		public event FwSelectionChangedEventHandler SelectionMade;
		/// <summary>
		/// This event notifies you that the selected index changed. You can find the current index from
		/// the SelectedIndex property, and look up the object if needed...but if you mainly care about
		/// the object, it is probably better to use SelectionChangedEvent.
		/// This is very nearly redundant...perhaps it would be better to just add an index to the
		/// SelectionChanged event and fire it if either index or object changes...
		/// </summary>
		public event EventHandler SelectedIndexChanged;

		/// <summary />
		public event EventHandler SorterChanged;

		/// <summary> Target Column can be selected by the BulkEdit bar which may need to reorient the
		/// RecordList to build a list based upon another RootObject class.</summary>
		public event TargetColumnChangedHandler TargetColumnChanged;

		/// <summary>
		/// Fired whenever a column width or column order changes
		/// </summary>
		public event EventHandler ColumnsChanged;

		/// <summary />
		public event EventHandler ListModificationInProgressChanged;

		/// <summary />
		public event EventHandler SelectionDrawingFailure;
		/// <summary>
		/// EventHandler used when a refresh of the BrowseView has been performed.
		/// </summary>
		public event EventHandler RefreshCompleted;

		/// <summary>
		/// Handler to use when checking if two sorters are compatible, should be implemented in the record list
		/// </summary>
		public event SortCompatibleHandler SortersCompatible;

		/// <summary>
		/// True during a major change that affects the master list of objects.
		/// The current example is deleting multiple objects from the list at once.
		/// This is set true (and the corresponding event raised) at the start of the operation,
		/// and back to false at the end. A client may want to suppress handling PropChanged
		/// during the complex operation, and simply reload the list at the end.
		/// </summary>
		public bool ListModificationInProgress { get; private set; }

		/// <summary>
		/// calls Focus on the important child control
		/// </summary>
		protected override void OnGotFocus(EventArgs e)
		{
			base.OnGotFocus(e);
			if (BrowseView != null && !BrowseView.Focused)
			{
				BrowseView.Focus();
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
				var val = SpecialCache.get_IntProp(RootObjectHvo, XMLViewsDataCache.ktagActiveColumn);
				return val <= 0 ? -1 : val - 1;
			}
			set
			{
				using (new ReconstructPreservingBVScrollPosition(this))
				{
					// No direct caching of fake stuff, but we can use the 'Decorator' SDA,
					// as it won't affect the data store wit this flid.
					//Cache.VwCacheDaAccessor.CacheIntProp(RootObjectHvo, XmlBrowseViewVc.ktagActiveColumn, value + 1);
					SpecialCache.SetInt(RootObjectHvo, XMLViewsDataCache.ktagActiveColumn, value + 1);

					if (!BrowseView.Vc.HasPreviewArrow && value >= 0)
					{
						BrowseView.Vc.PreviewArrow = BulkEditBar.PreviewArrowStatic;
					}
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
		public int PreviewValuesTag => XMLViewsDataCache.ktagAlternateValue;

		/// <summary>
		/// Yet another part of enabling preview...must be set for each item that is 'enabled', that is,
		/// it makes sense for it to be changed.
		/// </summary>
		public int PreviewEnabledTag => XMLViewsDataCache.ktagItemEnabled;

		internal void SetListModificationInProgress(bool val)
		{
			if (ListModificationInProgress == val)
			{
				return;
			}
			ListModificationInProgress = val;
			ListModificationInProgressChanged?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>
		/// Receive a notification that a mouse-up has completed in the browse view.
		/// </summary>
		internal virtual void BrowseViewMouseUp(MouseEventArgs e)
		{
			var dpiX = GetDpiX();
			var selColWidth = BrowseView.Vc.SelectColumnWidth * dpiX / 72000;
			if (BrowseView.Vc.HasSelectColumn && e.X < selColWidth)
			{
				var hvosChanged = new[] { BrowseView.SelectedObject };
				// we've changed the state of a check box.
				OnCheckBoxChanged(hvosChanged);
			}

			if (BulkEditBar != null && BulkEditBar.Visible)
			{
				BulkEditBar.UpdateEnableItems(BrowseView.SelectedObject);
			}
		}

		/// <summary />
		protected virtual void OnCheckBoxChanged(int[] hvosChanged)
		{
			try
			{
				if (CheckBoxChanged == null)
				{
					return;
				}
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
		public XmlBrowseViewBase BrowseView { get; protected internal set; }

		/// <summary>
		/// bulk edit bar, if one is installed. <c>null</c> if not.
		/// </summary>
		public BulkEditBar BulkEditBar { get; protected internal set; }

		internal LcmCache Cache { get; private set; }

		/// <summary>
		/// Get the special 'Decorator' ISilDataAccess cache.
		/// </summary>
		public XMLViewsDataCache SpecialCache { get; private set; }

		protected internal FilterBar FilterBar { get; set; }

		/// <summary>
		/// Gets or sets the sort item provider.
		/// </summary>
		public ISortItemProvider SortItemProvider { get; internal set; }

		internal int ListItemsClass => BrowseView.Vc.ListItemsClass;

		/// <summary>
		/// flags if the BrowseViewer has already sync'd its filters to the record list.
		/// </summary>
		private bool FilterInitializationComplete { get; set; }

		/// <summary>
		/// If the view has or might have a filter bar, call this after initialization to
		/// ensure that it is synchronized with the current filter in the record list.
		/// </summary>
		/// <param name="currentFilter">The current filter.</param>
		public void UpdateFilterBar(RecordFilter currentFilter)
		{
			if (FilterBar != null)
			{
				m_currentFilter = currentFilter;

				if (!FilterInitializationComplete)
				{
					// If we can append matching columns, then sync to the new list.
					if (AppendMatchingHiddenColumns(currentFilter))
					{
						UpdateColumnList(); // adjusts columns and syncs all filters.
						FilterInitializationComplete = true;
						return;
					}
				}
				else
				{
					// May have some active already. Remove them.
					OnRemoveFilters(this);
					if (currentFilter == null)
					{
						return;
					}
				}
				// syncs filters to currently shown columns.
				FilterBar.UpdateActiveItems(currentFilter);
			}
			SetBrowseViewSpellingStatus();
			FilterInitializationComplete = true;
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
			{
				return false;
			}
			if (m_currentFilter is FilterBarCellFilter)
			{
				return ((FilterBarCellFilter)m_currentFilter).Matcher is BadSpellingMatcher;
			}
			if (!(m_currentFilter is AndFilter))
			{
				return false;
			}
			foreach (var item in ((AndFilter)m_currentFilter).Filters)
			{
				if ((item as FilterBarCellFilter)?.Matcher is BadSpellingMatcher)
				{
					return true;
				}
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
				return BrowseView.SelectedIndex;
			}
			set
			{
				BrowseView.SelectedIndex = value;
			}
		}

		/// <summary>
		/// Gets the column count. This count does not include the check box column.
		/// </summary>
		public int ColumnCount => ColumnSpecs.Count;

		/// <summary>
		/// Gets the name of the specified column. The specified index is zero-based, it should
		/// not include the check box column.
		/// </summary>
		public string GetColumnName(int icol)
		{
			return XmlUtils.GetOptionalAttributeValue(ColumnSpecs[icol], "label");
		}

		/// <summary>
		/// Gets the default sort column. The returned index is zero-based, it does not include
		/// the check box column.
		/// </summary>
		public virtual int DefaultSortColumn => 0;

		/// <summary>
		/// Gets the sorted columns. The returned indices are zero-based, they do not include
		/// the check box column. Based off of InitSorter()
		/// </summary>
		public List<int> SortedColumns
		{
			get
			{
				var cols = new List<int>();
				if (FilterBar == null || FilterBar.ColumnInfo.Length <= 0 || Sorter == null)
				{
					return cols;
				}
				if (Sorter is AndSorter)
				{
					var sorters = ((AndSorter)Sorter).Sorters;
					foreach (var sorterObj in sorters)
					{
						var icol = ColumnInfoIndexOfCompatibleSorter(sorterObj as RecordSorter);
						if (icol >= 0)
						{
							cols.Add(icol);
						}
					}
				}
				else
				{
					var icol = ColumnInfoIndexOfCompatibleSorter(Sorter);
					if (icol >= 0)
					{
						cols.Add(icol);
					}
				}
				return cols;
			}
		}

		/// <summary>
		/// Set/Get the sorter. Setting using this does not raise SorterChanged...it's meant
		/// to be used to initialize it by the client.
		/// </summary>
		public RecordSorter Sorter { get; set; }

		/// <summary>
		/// Set the sorter and raise the SortChanged event.
		/// </summary>
		private void SetAndRaiseSorter(RecordSorter sorter, bool fTriggerChanged)
		{
			SyncSortArrows(sorter);

			Sorter = sorter;
			if (fTriggerChanged)
			{
				SorterChanged?.Invoke(this, EventArgs.Empty);
			}
		}

		internal void RaiseSelectionDrawingFailure()
		{
			SelectionDrawingFailure?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>
		/// Set up all of the sort arrows based on the sorter
		/// </summary>
		private void SyncSortArrows(RecordSorter sorter)
		{
			ResetSortArrowColumn();
			if (sorter == null)
			{
				return;
			}

			ArrayList sorters;
			if (sorter is AndSorter)
			{
				sorters = ((AndSorter)sorter).Sorters;
			}
			else
			{
				sorters = new ArrayList
				{
					sorter
				};
			}

			for (var i = 0; i < sorters.Count; i++)
			{
				// set our current column to the one we are sorting.
				var ifsi = ColumnInfoIndexOfCompatibleSorter((RecordSorter)sorters[i]);

				// set our header column arrow
				var order = SortOrder.Ascending;
				var grs = sorters[i] as GenRecordSorter;
				var sfc = grs?.Comparer as StringFinderCompare;
				if (sfc != null)
				{
					order = sfc.Order;
				}
				var iHeaderColumn = ColumnHeaderIndex(ifsi);

				switch (i)
				{
					case 0:
						SetSortArrowColumn(iHeaderColumn, order, ArrowSize.Large);
						break;
					case 1:
						SetSortArrowColumn(iHeaderColumn, order, ArrowSize.Medium);
						break;
					default:
						SetSortArrowColumn(iHeaderColumn, order, ArrowSize.Small);
						break;
				}

				Logger.WriteEvent($"Sort on {m_lvHeader.ColumnsInDisplayOrder[iHeaderColumn].Text} {order} ({i})");
			}
			m_lvHeader.Refresh();
		}

		// Sets the sort arrow for the given header column. Also sets the current column if
		// its a valid header column.
		private void SetSortArrowColumn(int iSortArrowColumn, SortOrder order, ArrowSize size)
		{
			if (iSortArrowColumn >= 0)
			{
				m_icolCurrent = iSortArrowColumn;
			}

			m_lvHeader.ShowHeaderIcon(iSortArrowColumn, order, size);
		}

		// Resets all column sort arrows
		private void ResetSortArrowColumn()
		{
			for (var i = 0; i < m_lvHeader.Columns.Count; i++)
			{
				m_lvHeader.ShowHeaderIcon(i, SortOrder.None, ArrowSize.Large);
			}
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
				return BrowseView.RootObjectHvo;
			}
			set
			{
				BrowseView.RootObjectHvo = value;
			}
		}

		/// <summary>
		/// Set the stylesheet used to display information.
		/// </summary>
		public IVwStylesheet StyleSheet
		{
			get
			{
				return BrowseView.StyleSheet;
			}
			set
			{
				BrowseView.StyleSheet = value;
				FilterBar?.SetStyleSheet(value);
				BulkEditBar?.SetStyleSheet(value);
			}
		}

		/// <summary>
		/// The top-level property of RootObjectHvo that we are displaying.
		/// </summary>
		public int MainTag => BrowseView.MainTag;

		/// <summary>
		/// Default constructor is required for some reason I (JohnT) forget, probably to do with
		/// design mode. Don't use it.
		/// </summary>
		public BrowseViewer()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			// TODO: Add any initialization after the InitForm call
			AccNameDefault = "BrowseViewer";	// default accessibility name
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:BrowseViewer"/> class.
		/// The sortItemProvider is typically the RecordList that impelements sorting and
		/// filtering of the items we are displaying.
		/// The data access passed typically is a decorator for the one in the cache, adding
		/// the sorted, filtered list of objects accessed as property madeUpFieldIdentifier of hvoRoot.
		/// </summary>
		public BrowseViewer(XElement configParamsElement, int hvoRoot, LcmCache cache, ISortItemProvider sortItemProvider, ISilDataAccessManaged sda)
		{
			ContructorSurrogate(configParamsElement, hvoRoot, cache, sortItemProvider, sda);
		}

		/// <summary>
		/// This caches selected states for objects in bulk edit views.
		/// We want to allow a second copy of such a bulk edit to have different selections.
		/// So we only store something here when we dispose of an old view. The next one opened
		/// with the same parameters will have the same selections as the last one closed.
		/// The key is the nodeSpec passed to the constructor surrogate and the hvoRoot.
		/// The value is the
		/// </summary>
		static Dictionary<Tuple<XElement, int>, Tuple<Dictionary<int, int>, bool>> s_selectedCache = new Dictionary<Tuple<XElement, int>, Tuple<Dictionary<int, int>, bool>>();

		private void ContructorSurrogate(XElement configParamsElement, int hvoRoot, LcmCache cache, ISortItemProvider sortItemProvider, ISilDataAccessManaged sda)
		{
			m_configParamsElement = configParamsElement;
			Cache = cache;
			Tuple<Dictionary<int, int>, bool> selectedInfo;
			var key = new Tuple<XElement, int>(configParamsElement, hvoRoot);
			if (s_selectedCache.TryGetValue(key, out selectedInfo))
			{
				SpecialCache = new XMLViewsDataCache(sda, selectedInfo.Item2, selectedInfo.Item1);
				s_selectedCache.Remove(key); // don't reuse again while this view is open
			}
			else
			{
				SpecialCache = new XMLViewsDataCache(sda, XmlUtils.GetOptionalBooleanAttributeValue(m_configParamsElement, "defaultChecked", true));
			}
			m_lvHeader = new DhListView(this);
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
			SuspendLayout();
			Scroller = new BrowseViewScroller(this)
			{
				AutoScroll = true,
				TabStop = false
			};
			Controls.Add(Scroller);
			ScrollBar = new VScrollBar();
			ScrollBar.ValueChanged += m_scrollBar_ValueChanged;
			ScrollBar.TabStop = false;
			Controls.Add(ScrollBar);
			// Set this before creating the browse view class so that custom parts can be
			// generated properly.
			SortItemProvider = sortItemProvider;
		}

		/// <summary>
		/// Finish initializing the class.
		/// </summary>
		public void FinishInitialization(int hvoRoot, int madeUpFieldIdentifier)
		{
			BrowseView.Init(m_configParamsElement, hvoRoot, madeUpFieldIdentifier, Cache, this);
			BrowseView.SelectionChangedEvent += OnSelectionChanged;
			BrowseView.SelectedIndexChanged += m_xbv_SelectedIndexChanged;
			// Sometimes we get a spurious "out of memory" error while trying to create a handle for the
			// RootSite if its cache isn't set before we add it to its parent.
			// This is now handled in the above Init method.
			//m_xbv.Cache = m_cache;
			AddControl(BrowseView);

			// This would eventually get set in the startup process later, but we need it at least by the time
			// we make the filter bar so the LayoutCache exists.
			BrowseView.Vc.Cache = Cache;
			BrowseView.Vc.DataAccess = SpecialCache;
			Scroller.SuspendLayout();
			//
			// listView1
			//
			//m_lvHeader.Dock = System.Windows.Forms.DockStyle.Top;
			m_lvHeader.Name = "HeaderListView";
			m_lvHeader.Size = new Size(4000, 22);
			m_lvHeader.TabIndex = 0;
			m_lvHeader.View = View.Details;
#if __MonoCS__	// FWNX-224
			m_lvHeader.ColumnLeftClick += m_lvHeader_ColumnLeftClick;
#else
			m_lvHeader.ColumnClick += m_lvHeader_ColumnLeftClick;
#endif
			m_lvHeader.ColumnRightClick += m_lvHeader_ColumnRightClick;
			m_lvHeader.ColumnDragDropReordered += m_lvHeader_ColumnDragDropReordered;
			m_lvHeader.AllowColumnReorder = true;
			m_lvHeader.AccessibleName = "HeaderListView";
			m_lvHeader.Scrollable = false; // don't EVER show scroll bar in it!!
			m_lvHeader.TabStop = false;

			Name = "BrowseView";
			Size = new Size(400, 304);
			if (ColumnIndexOffset() > 0)
			{
				var ch = new ColumnHeader { Text = "" };
				m_lvHeader.Columns.Add(ch);
			}

			if (BrowseView.Vc.ShowColumnsRTL)
			{
				for (var i = ColumnSpecs.Count - 1; i >= 0; --i)
				{
					var node = ColumnSpecs[i];
					var ch = MakeColumnHeader(node);
					m_lvHeader.Columns.Add(ch);
				}
			}
			else
			{
				foreach (var node in ColumnSpecs)
				{
					var ch = MakeColumnHeader(node);
					m_lvHeader.Columns.Add(ch);
				}
			}
			// set default property, IFF it does not exist, so it doesn't accidentally get set
			// in OnPropertyChanged() when user right clicks for the first time (cf. LT-2789).
			PropertyTable.SetDefault("SortedFromEnd", false, true);
			// set default property, IFF it does not exist, so it doesn't accidentally get set
			// in OnPropertyChanged() when user right clicks for the first time (cf. LT-2789).
			PropertyTable.SetDefault("SortedByLength", false, true);

			//
			// FilterBar
			//
			var xa = m_configParamsElement.Attribute("filterBar");
			if (xa != null && xa.Value == "true")
			{
				FilterBar = new FilterBar(this, PropertyTable.GetValue<IApp>("App"));
				FilterBar.FilterChanged += FilterChangedHandler;
				FilterBar.Name = "FilterBar";
				FilterBar.AccessibleName = "FilterBar";
				m_lvHeader.TabIndex = 1;
				AddControl(FilterBar);
			}
			AddControl(m_lvHeader); // last so on top of z-order, puts it above other things docked at top.
			xa = m_configParamsElement.Attribute("bulkEdit");
			if (xa != null && xa.Value == "true")
			{
				BulkEditBar = CreateBulkEditBar(this, m_configParamsElement, PropertyTable, Cache);
				BulkEditBar.Dock = DockStyle.Bottom;
				BulkEditBar.Name = "BulkEditBar";
				BulkEditBar.AccessibleName = "BulkEditBar";
				Controls.Add(BulkEditBar);
				BrowseView.Vc.PreviewArrow = BulkEditBar.PreviewArrow;
				// Enhance JohnT: if we ever allow editing within the browse part
				// of bulk edit, we'll need to stop doing this. However, if there really are
				// no columns where you can edit, if we don't do this, SimpleRootSite eventually
				// tries to make a selection somewhere that allows editing, and spends forever
				// looking for an editable place if the database is large.
				BrowseView.ReadOnlyView = true;
			}
			if (BrowseView.Vc.HasSelectColumn)
			{
				// Do our own setup for checkmark header, since it doesn't display reliably when implemented in DhListView (LT-4473).
				// In DhListView, an implementation of  checkmark header was not reliably receiving system Paint messages after
				// an Invalidate(), Refresh(), or Update(). Until we find a solution to that problem, the parent of DhListView (e.g. BrowseViewer)
				// will have to be responsible for setting up this button.

				m_checkMarkButton = new Button
				{
					Image = ResourceHelper.CheckMarkHeader,
					Height = m_lvHeader.Height - 6,
					FlatStyle = FlatStyle.Flat,
					BackColor = Color.Transparent,
					ForeColor = Color.Transparent,
					Top = 2,
					Left = 1
				};
				m_checkMarkButton.Width = m_checkMarkButton.Image.Width + 5;
				m_checkMarkButton.Click += m_checkMarkButton_Click;
				var ttip = new ToolTip();
				ttip.SetToolTip(m_checkMarkButton, XMLViewsStrings.ksTipCheck);
				Scroller.Controls.Add(m_checkMarkButton);
				m_checkMarkButton.BringToFront();
			}

			// We make an overlay button to occupy the space above the top of the scroll bar where there is no column.
			// It might be possible to make a dummy column here and just put the icon in it...
			// but I think I (JT) found that if the total width of the columns is any more than it is now,
			// DotNet adds a horizontal scroll bar which totally hides the labels.
			m_configureButton = new Button
			{
				Visible = !XmlUtils.GetOptionalBooleanAttributeValue(m_configParamsElement, "disableConfigButton", false)
			};
			m_configureButton.Click += m_configureButton_Click;
			// Don't dock the button, this destroys all control over its height, and it overwrites the line at the bottom
			// of the header bar.
			var blueArrow = ResourceHelper.ColumnChooser;

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
			Controls.Add(m_configureButton);
			BrowseView.AutoScroll = false;

			m_configureButton.ImageAlign = ContentAlignment.MiddleCenter;
			m_configureButton.TabStop = false;

			AutoScroll = true;
			VScroll = false;
			HScroll = true;
			Scroller.ResumeLayout(false);
			ResumeLayout(false);
		}

		/// <summary>
		/// indicates the last class of items that the user selected something.
		/// </summary>
		protected int m_lastChangedSelectionListItemsClass;

		private XElement m_modifiedColumn;

		/// <summary>
		/// This is called just before the TargetComboSelecctedIndexChanged event.
		/// It may set the ForceReload flag on the event for the benefit of other callers.
		/// </summary>
		/// REFACTOR: This method should go away, some of it should go into the TargetComboSelectedIndexChanged
		/// and the rest should be place in the BulkEditBar class
		internal void BulkEditTargetComboSelectedIndexChanged(TargetColumnChangedEventArgs e)
		{
			var typeChanged = false;
			if (e.ExpectedListItemsClass != 0)
			{
				if (BrowseView.Vc.ListItemsClass != e.ExpectedListItemsClass)
				{
					OnListItemsAboutToChange(AllItems);
					typeChanged = true;
				}
				BrowseView.Vc.ListItemsClass = e.ExpectedListItemsClass;
			}
			if (m_modifiedColumn != null)
			{
				XmlUtils.SetAttribute(m_modifiedColumn, "layout", XmlUtils.GetMandatoryAttributeValue(m_modifiedColumn, "normalLayout"));
				m_modifiedColumn = null;
				e.ForceReload = true;
			}
			// One way this fails is that items for bulk delete can have -1 as column number (since deleting
			// whole rows doesn't apply to particular columns). Checking the other end of the range is just paranoia.
			if (e.ColumnIndex >= 0 && e.ColumnIndex < ColumnSpecs.Count)
			{
				var column = ColumnSpecs[e.ColumnIndex];
				var editLayout = XmlUtils.GetOptionalAttributeValue(column, "editLayout");
				var layout = XmlUtils.GetOptionalAttributeValue(column, "layout");
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
			if(typeChanged || e.ForceReload)
			{
				BulkEditBar.ResumeRecordListRowChanges(); // <-- against my better judgement. naylor 11-2011
			}
			TargetColumnChanged?.Invoke(this, e);
		}

		/// <summary>
		/// Save the current state of given selection items, so
		/// the next list can be consistent with those selections (LT-8986)
		/// UpdateCheckedItems() uses this information to put the next list items
		/// in the proper state.
		/// </summary>
		protected void OnListItemsAboutToChange(IList<int> selectionItemsToSave)
		{
			if (!m_fIsInitialized)
			{
				return;
			}
			m_lastChangedSelectionListItemsClass = (int) BrowseView.Vc.ListItemsClass;
			SaveSelectionItems(new HashSet<int>(selectionItemsToSave));
		}

		/// <summary />
		protected virtual BulkEditBar CreateBulkEditBar(BrowseViewer bv, XElement spec, IPropertyTable propertyTable, LcmCache cache)
		{
			return new BulkEditBar(bv, spec, propertyTable, cache);
		}

		/// <summary/>
		protected void AddControl(Control control)
		{
			Scroller.Controls.Add(control);
		}

		/// <summary>
		/// Initialize the sorter (typically starting with the record list's sorter, but if the columns are reordered
		/// this should re-sync things).
		/// 1) If we are not given a GenRecordSorter we will raise to resort to our first sortable column.
		/// 2) If we are given a GenRecordSorter but we can't find a corresponding column in our browseview,
		///		then accept the given sorter and clear the column header sort arrow.
		/// 3) Otherwise, use a column's matched sorter and copy the needed information from the given sorter.
		/// </summary>
		/// <param name="sorter"></param>
		/// <param name="fSortChanged">true to force updating the sorter (even if the new one is the same)</param>
		/// <returns>true if it changed the sorter.</returns>
		public bool InitSorter(RecordSorter sorter, bool fSortChanged = false)
		{
			if (FilterBar == null || FilterBar.ColumnInfo.Length <= 0)
			{
				return false;
			}

			ArrayList sorters;
			var andSorter = sorter as AndSorter;
			if (andSorter != null)
			{
				sorters = andSorter.Sorters;
			}
			else
			{
				sorters = new ArrayList();
				if (sorter != null)
				{
					sorters.Add(sorter);
				}
			}

			var newSorters = new ArrayList();
			for(var i = 0; i < sorters.Count; i++)
			{
				var ifsi = ColumnInfoIndexOfCompatibleSorter(sorters[i] as RecordSorter);
				if (sorters[i] as GenRecordSorter == null && ifsi < 0)
				{
					// we don't want to use this sorter because it's not the kind that our filters
					// are set up for. Let's change the sorter to one of our own.
					sorters[i] = FilterBar.ColumnInfo[0].Sorter;
					continue;
				}
				if (ifsi < 0)
				{
					// The given sorter is a GenRecordSorter, but we couldn't find its column
					// in our browse view columns.
					//newSorter = (GenRecordSorter)sorters[i];

					// Until adding the column dynamically is implemented, we'll just drop this sorter
					continue;
				}

				// ifsi >= 0
				// We found a sorter in our columns that matches the given one,
				// so, preserve the given sorter's Order and SortFromEnd states.
				var grs = sorters[i] as GenRecordSorter;
				var sfc = grs.Comparer as StringFinderCompare;
				var newGrs = FilterBar.ColumnInfo[ifsi].Sorter as GenRecordSorter;
				if (!newGrs.Equals(grs))
				{
					sfc.CopyTo(newGrs.Comparer as StringFinderCompare);	// copy Order and SortFromEnd
					fSortChanged = true;
				}
				if (andSorter != null)
				{
					andSorter.ReplaceAt(i, grs, newGrs);
				}
				else
				{
					sorters[i] = newGrs;
				}
				newSorters.Add(sorters[i]);
			}
			if (newSorters.Count != sorters.Count || sorter == null)
			{
				fSortChanged = true;
			}
			if (newSorters.Count > 1)
			{
				var asorter = new AndSorter(newSorters);
				Sorter = asorter;
				m_SortersToDispose.Add(asorter);
			}
			else if (newSorters.Count == 1)
			{
				Sorter = newSorters[0] as RecordSorter;
			}
			else
			{
				// No sorters could get transfered over, we've got to have one, so we'll take whatever
				// is left over in the first column
				Sorter = FilterBar.ColumnInfo[0].Sorter;
			}
			if (fSortChanged || !m_fUpdatingColumnList)
			{
				SetAndRaiseSorter(Sorter, fSortChanged);
			}
			return fSortChanged;
		}

		/// <summary />
		protected int ColumnInfoIndexOfCompatibleSorter(RecordSorter sorter)
		{
			var iSortColumn = -1;
			for (var icol = 0; icol < FilterBar.ColumnInfo.Length; icol++)
			{
				var fsi = FilterBar.ColumnInfo[icol];
				var thisSorter = fsi.Sorter;
				if (SortersCompatible(thisSorter, sorter))
				{
					iSortColumn = icol;
					break;
				}
			}
			return iSortColumn;
		}

		internal void HideBulkEdit()
		{
			Controls.Remove(BulkEditBar);
		}

		/// <summary>
		/// Make a column header for a specified XElement that is a "column" element.
		/// </summary>
		private ColumnHeader MakeColumnHeader(XElement node)
		{
			// Currently, if you add a new attribute here,
			// you need to update the conditionals in LayoutFinder.SameFinder (cf. LT-2858).
			var label = StringTable.Table.LocalizeAttributeValue(XmlUtils.GetOptionalAttributeValue(node, "label", null));
			if (label == null)
			{
				if (node.Attribute("label") == null)
				{
					throw new ApplicationException("column must have label attr");
				}
				label = node.Attribute("label").Value;
			}
			var ch = new ColumnHeader
			{
				Text = label
			};
			return ch;
		}

		bool m_fSavedSelectionsDuringFilterChange;
		bool m_fInFilterChangedHandler;
		private void FilterChangedHandler(object sender, FilterChangeEventArgs args)
		{
			// This shouldn't reenter itself!  See FWR-2335.
			if (m_fInFilterChangedHandler)
			{
				return;
			}
			try
			{
				m_fInFilterChangedHandler = true;
				if (FilterChanged != null)
				{
					// before we actually change the filter, save all the selected and unselected items.
					m_fSavedSelectionsDuringFilterChange = true;
					SaveAllSelectionItems();
					FilterChanged(this, args);
				}
				SetBrowseViewSpellingStatus();
			}
			finally
			{
				m_fInFilterChangedHandler = false;
			}
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
			SaveSelectionItems(new HashSet<int>(AllItems));
		}

		/// <summary />
		/// <param name="itemsToSaveSelectionState">items that need to be saved,
		/// especially those that the user has changed in selection status</param>
		private void SaveSelectionItems(HashSet<int> itemsToSaveSelectionState)
		{
			if (!BrowseView.Vc.HasSelectColumn || BulkEditBar == null || !(SortItemProvider is IMultiListSortItemProvider))
			{
				return;
			}
			var sourceTag = (SortItemProvider as IMultiListSortItemProvider).ListSourceToken;
			foreach (var hvoItem in itemsToSaveSelectionState)
			{
				if (IsItemChecked(hvoItem))
				{
					// try to remove the item from the unselected list and
					// update our selected items, if we haven't already.
					UpdateMutuallyExclusiveDictionaries(hvoItem, sourceTag, ref m_unselectedItems, ref m_selectedItems);
				}
				else
				{
					// try to remove the item from the selected list and
					// update our unselected items, if we haven't already.
					UpdateMutuallyExclusiveDictionaries(hvoItem, sourceTag, ref m_selectedItems, ref m_unselectedItems);
				}
			}
		}

		private void UpdateMutuallyExclusiveDictionaries<TKey, TValue>(TKey key, TValue value,
			ref IDictionary<TKey, TValue> dictionaryToRemove, ref IDictionary<TKey, TValue> dictionaryToAdd)
		{
			dictionaryToRemove.Remove(key);
			if (!dictionaryToAdd.ContainsKey(key))
			{
				dictionaryToAdd.Add(key, value);
			}
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
			(SortItemProvider as IMultiListSortItemProvider).ConvertItemsToRelativesThatApplyToCurrentList(ref items);
			foreach (var relative in items.Keys)
			{
				// cache the selection on the relative (even if it's not yet visible, i.e. in "currentItems")
				SetItemCheckedState(relative, selectItem, false);
			}
		}

		/// <summary>
		/// Remove items from the given dictionary if they are:
		/// 1) "Invalid" in that their selection state does not match expectations;
		/// 2) Not in the ObjectRepository because they have been deleted (see LTB-1650);
		/// </summary>
		private void RemoveInvalidOldSelectedItems(ref IDictionary<int, object> items, bool fExpectToBeSelected)
		{
			var objRepo = Cache.ServiceLocator.ObjectRepository;
			var invalidSelectedItems = new HashSet<int>();
			foreach (var item in items)
			{
				// LTB-1650 - test if item still exists:
				if (!objRepo.IsValidObjectId(item.Key))
				{
					invalidSelectedItems.Add(item.Key);
					continue;
				}
				var fActuallySelected = IsItemChecked(item.Key);

				if (fExpectToBeSelected && !fActuallySelected || !fExpectToBeSelected && fActuallySelected)
				{
					invalidSelectedItems.Add(item.Key);
				}
			}
			foreach (var item in invalidSelectedItems)
			{
				items.Remove(item);
			}
		}

		/// <summary>
		/// restore our saved select items.
		/// NOTE: in case of overlapping items (and thus conflicting selection agendas),
		/// we make selections to win over unselections
		/// </summary>
		private void RestoreSelectionItems()
		{
			if (!BrowseView.Vc.HasSelectColumn || BulkEditBar == null)
			{
				return;
			}
			var selectedItems = m_selectedItems.Keys;
			var unselectedItems = m_unselectedItems.Keys;
			foreach (var hvoItem in AllItems)
			{
				// NOTE: in case of overlapping items (and thus conflicting selection agendas), we want
				// selected items to win over unselected items.
				var fIsItemChecked = IsItemChecked(hvoItem);
				if (selectedItems.Contains(hvoItem))
				{
					if (!fIsItemChecked)
					{
						SetItemCheckedState(hvoItem, true, false);
					}
				}
				else if (unselectedItems.Contains(hvoItem))
				{
					if (fIsItemChecked)
					{
						SetItemCheckedState(hvoItem, false, false);
					}
				}
				else
				{
					SetItemCheckedState(hvoItem, BrowseView.Vc.DefaultChecked, false);
				}
			}
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			// Must not be run more than once.
			if (IsDisposed)
			{
				return;
			}

			if( disposing )
			{
				Subscriber.Unsubscribe("LinkFollowed", LinkFollowed_Handler);
				if (m_configParamsElement != null && SpecialCache != null && BrowseView != null && RootObjectHvo != 0)
				{
					s_selectedCache[new Tuple<XElement, int>(m_configParamsElement, RootObjectHvo)] = new Tuple<Dictionary<int, int>, bool>(SpecialCache.SelectedCache, SpecialCache.DefaultSelected);
				}
				// If these controls are child controls of either 'this' or of m_scrollContainer,
				// they will automatically be disposed. If they have been removed for whatever reason,
				// we need to dispose of them here.
				if (m_configureButton != null)
				{
					m_configureButton.Click -= m_configureButton_Click;
					if (!Controls.Contains(m_configureButton))
					{
						m_configureButton.Dispose();
					}
				}
				if (ScrollBar != null)
				{
					//m_scrollBar.Scroll -= new ScrollEventHandler(m_scrollBar_Scroll);
					ScrollBar.ValueChanged -= m_scrollBar_ValueChanged;
					if (!Controls.Contains(ScrollBar))
					{
						ScrollBar.Dispose();
					}
				}
				if (Scroller != null)
				{
					if (m_lvHeader != null)
					{
#if __MonoCS__	// FWNX-224
						m_lvHeader.ColumnLeftClick -= m_lvHeader_ColumnLeftClick;
#else
						m_lvHeader.ColumnClick -= m_lvHeader_ColumnLeftClick;
#endif
						m_lvHeader.ColumnRightClick -= m_lvHeader_ColumnRightClick;
						m_lvHeader.ColumnDragDropReordered -= m_lvHeader_ColumnDragDropReordered;
						if (!Scroller.Controls.Contains(m_lvHeader))
						{
							m_lvHeader.Dispose();
						}
					}
					if (BrowseView != null)
					{
						BrowseView.SelectionChangedEvent -= OnSelectionChanged;
						BrowseView.SelectedIndexChanged -= m_xbv_SelectedIndexChanged;
						if (!Scroller.Controls.Contains(BrowseView))
						{
							BrowseView.Dispose();
						}
					}
					if (FilterBar != null)
					{
						FilterBar.FilterChanged -= FilterChangedHandler;
						if (!Scroller.Controls.Contains(FilterBar))
						{
							FilterBar.Dispose();
						}
					}
					if (BulkEditBar != null && !Scroller.Controls.Contains(BulkEditBar))
					{
						BulkEditBar.Dispose();
					}
					if (!Controls.Contains(Scroller))
					{
						Scroller.Dispose();
					}
				} // end of m_scrollContainer != null
				m_tooltip?.Dispose();

				components?.Dispose();
				m_SortersToDispose.Dispose();
			}
			m_configureButton = null;
			ScrollBar = null;
			m_lvHeader = null;
			BrowseView = null;
			FilterBar = null;
			BulkEditBar = null;
			Scroller = null;
			Cache = null;
			m_configParamsElement = null;
			SortItemProvider = null;
			Sorter = null;
			m_tooltip = null;
			m_currentFilter = null;

			base.Dispose(disposing);

			// Since it was in a static data member,
			// that may have been the only thing keeping it from being collected,
			// but we want to survive through the base Dispose call, at least.
			GC.KeepAlive(this);
		}

		/// <summary>
		///	invoked when our BrowseViewView selection changes
		/// </summary>
		public void OnSelectionChanged(object sender, FwObjectSelectionEventArgs e)
		{
			//we don't do anything but pass this on to objects which have subscribed to this event
			SelectionChanged?.Invoke(sender, e);
		}

		/// <summary>
		/// Return a rectangle relative to the top left of the client area of the window whose top and bottom
		/// match the selected row and whose left and right match the named column.
		/// </summary>
		public Rectangle LocationOfCellInSelectedRow(string colName)
		{
			var index = IndexOfColumn(colName);
			if (index < 0 || BrowseView.SelectedIndex < 0)
			{
				return new Rectangle();
			}
			var width = m_colWidths[index];
			var row = BrowseView.LocationOfSelectedRow();
			var pos = 0;
			for (var i = 0; i < index; i++)
			{
				pos += m_colWidths[i];
			}
			return new Rectangle(pos, row.Top + BrowseView.Top, width, row.Height);
		}

		/// <summary>
		/// invoked when the BrowseView selection is chosen by a double click.  This is currently
		/// invoked only for read-only views.
		/// </summary>
		public void OnDoubleClick(FwObjectSelectionEventArgs e)
		{
			SelectionMade?.Invoke(this, e);
		}

		/// <summary />
		protected override void OnLayout(LayoutEventArgs levent)
		{
			AdjustControls();
			base.OnLayout(levent); // doesn't do much, now we're not docking
		}

		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Control.SizeChanged"/> event.
		/// </summary>
		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged (e);
			// the != 0 check makes sure we don't do this until we've been laid out for real
			// at least once, which doesn't happen until we have all the requisite stuff created.
			if (Width != m_lastLayoutWidth && m_lastLayoutWidth != 0)
			{
				AdjustControls();
			}

#if __MonoCS__ // FWNX-425
			EnsureScrollContainerIsCorrectWidth();
#endif
		}

		/// <summary>
		/// Adjusts the controls.
		/// </summary>
		protected void AdjustControls()
		{
			if (m_configureButton == null)
			{
				return;		// layout hasn't actually occurred yet -- see FWNX-733.
			}
			m_lastLayoutWidth = Width;

			// The -5 seems to allow for various borders and put the actual icon in about the right place.
			m_configureButton.Left = Width - m_configureButton.Width;
			// -1 allows the border of the button to be hidden (clipped because it's outside the
			// client area of its parent), and the blue circle icon to be right at the top of the available space.
			m_configureButton.Top = -1;
			// -6 seems to be about right to allow for various borders and prevent the button from
			// overwriting the bottom border of the header bar.
			m_configureButton.Height = m_lvHeader.Height;

			var sbHeight = Height - m_configureButton.Height;
			var scrollContHeight = Height;
			if (BulkEditBar != null && BulkEditBar.Visible)
			{
				sbHeight -= BulkEditBar.Height;
				scrollContHeight -= BulkEditBar.Height;
			}
			ScrollBar.Height = sbHeight;
			ScrollBar.Location = new Point(Width - ScrollBar.Width, m_configureButton.Height);
			BrowseView?.SetScrollBarParameters(ScrollBar);

			Scroller.Location = new Point(0,0);
			Scroller.Height = scrollContHeight;
			EnsureScrollContainerIsCorrectWidth();
		}

		internal void EnsureScrollContainerIsCorrectWidth()
		{
			if (Scroller == null)
			{
				return;
			}

			if (m_configureButton == null)
			{
				return;
			}

			if (ScrollBar == null)
			{
				return;
			}

			Scroller.Width = Width - Math.Max(m_configureButton.Width, ScrollBar.Width);
		}

		/// <summary>
		/// Called by a layout event in the BrowseViewScroller. Adjusts the controls inside it.
		/// </summary>
		internal void LayoutScrollControls()
		{
			if (BrowseView == null || m_lvHeader == null || m_lvHeader.Columns.Count == 0 || Scroller.Width < DhListView.kgapForScrollBar)
			{
				return; // sometime called very early in construction process.
			}
			var widthTotal = Math.Max(SetSavedOrDefaultColWidths(Scroller.Width), Scroller.ClientRectangle.Width);
			var xPos = Scroller.AutoScrollPosition.X;
			// This simulates docking, except that we don't adjust the width if we are
			// scrolling horizontally.
			m_lvHeader.Location = new Point(xPos, 0);
			m_lvHeader.Width = widthTotal;
			var top = m_lvHeader.Height;
			if (FilterBar != null && FilterBar.Visible)
			{
				FilterBar.Location = new Point(xPos, top);
				FilterBar.Width = widthTotal;
				top += FilterBar.Height;
			}
			BrowseView.Width = widthTotal;
			var bottom = Height;
			if (Scroller.Width < widthTotal)
			{
				bottom -= 22; // leave room for horizontal scroll to prevent vertical scroll.
			}
			if (BulkEditBar != null && BulkEditBar.Visible)
			{
				bottom -= BulkEditBar.Height;
			}
			BrowseView.Location = new Point(xPos, top);
			BrowseView.Height = (bottom - top);
			// Simulate a drag to align the columns.
			// Note that this isn't enough to make it right initially, it seems there isn't
			// enough of the view present to fix yet.
			AdjustColumnWidths(false);
			m_lvHeader.PerformLayout();
			BrowseView.SetScrollBarParameters(ScrollBar);
		}

		/// <summary>
		/// gets the Scrollbar of the viewer
		/// </summary>
		public ScrollBar ScrollBar { get; protected set; }

		/// <summary>
		/// gets the (horizontal) Scrollbar of the viewer
		/// </summary>
		public BrowseViewScroller Scroller { get; protected set; }

		private int SetSavedOrDefaultColWidths(int idealWidth)
		{
			var widthTotal = 0;
			var widthExtra = 0; // how much wider header is than total data column width
			try
			{
				m_lvHeader.AdjustingWidth = true;
				// The width available for columns is less than the total width of the browse view by an amount that appears
				// a little wider than the scroll bar. 23 seems to be the smallest value that suppresses scrolling within
				// the header control...hopefully we can get some sound basis for it eventually.
				var widthAvail = idealWidth;
				var columns = ColumnSpecs;
				var count = columns.Count;
				var dpiX = GetDpiX();
				if (ColumnIndexOffset() > 0)
				{
					var selColWidth = BrowseView.Vc.SelectColumnWidth * dpiX / 72000;
					m_lvHeader.ColumnsInDisplayOrder[0].Width = selColWidth;
					widthAvail -= selColWidth;
					widthExtra += selColWidth;
				}
				for (var i = 0; i < count; i++)
				{
					// If the user previously altered the column width, it will be available
					// in the Property table, as an absolute value. If not, use node.Attributes
					// to get a percentage value.
					var width = GetPersistedWidthForColumn(i);
					if (width < 0)
					{
						width = GetInitialColumnWidth(columns[i], widthAvail, dpiX);
					}
					widthTotal += width;
					if (widthTotal + widthExtra + 1 > m_lvHeader.Width)
					{
						m_lvHeader.Width = widthTotal + widthExtra + 1; // otherwise it may truncate the width we set.
					}
					var ch = m_lvHeader.ColumnsInDisplayOrder[ColumnHeaderIndex(i)];
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
		/// <param name="iCol">index to the column node</param>
		/// <returns>width if property value found, otherwise -1.</returns>
		private int GetPersistedWidthForColumn(int iCol)
		{
			var width = -1; // default to trigger percentage width calculation.
			if (PropertyTable != null)
			{
				var propName = FormatColumnWidthPropertyName(iCol);
				width = PropertyTable.GetValue(propName, -1, SettingsGroup.LocalSettings);
			}
			return width;
		}
		/// <summary>
		/// Get current DPI in the X coordinate
		/// </summary>
		protected int GetDpiX()
		{
			using (var g = CreateGraphics())
			{
				var dpiX = (int)g.DpiX;
				return dpiX;
			}
		}

		private static int GetInitialColumnWidth(XElement node, int widthAvail, int dpiX)
		{
			int width;
			var strWidth = XmlUtils.GetOptionalAttributeValue(node, "width", "48000");
			if (strWidth.Length > 1 && strWidth[strWidth.Length - 1] == '%')
			{
				var widthPercent = Convert.ToInt32(strWidth.Substring(0, strWidth.Length - 1)); // strip percent sign (assumed).
				width = widthPercent * widthAvail / 100;
			}
			else
			{
				// Convert to pixels from millipoints.
				width = Convert.ToInt32(strWidth) * dpiX / 72000;
			}
			return width;
		}

		static bool EqualIntArrays(int[] first, int[] second)
		{
			if (first == null)
			{
				return second == null;
			}
			if (first.Length != second?.Length)
			{
				return false;
			}
			for (var i = 0; i < first.Length; i++)
			{
				if (first[i] != second[i])
				{
					return false;
				}
			}
			return true;
		}

		private sealed class OneColumnXmlBrowseView : XmlBrowseViewBase
		{
			private OneColumnXmlBrowseView()
			{
			}

			internal OneColumnXmlBrowseView(BrowseViewer bv, int icolLvHeaderToAdd)
				: this(bv.m_configParamsElement, bv.RootObjectHvo, bv.MainTag, bv.Cache, bv.PropertyTable, bv.StyleSheet, bv, icolLvHeaderToAdd)
			{
				// add only the specified column to this browseview.
				(Vc as OneColumnXmlBrowseViewVc).SetupOneColumnSpec(bv, icolLvHeaderToAdd);
			}

			private OneColumnXmlBrowseView(XElement nodeSpec, int hvoRoot, int mainTag, LcmCache cache, IPropertyTable propertyTable,
				IVwStylesheet styleSheet, BrowseViewer bv, int icolLvHeaderToAdd)
			{
#if RANDYTODO
				base.Init(mediator, propertyTable, nodeSpec);
				base.Init(nodeSpec, hvoRoot, mainTag, cache, mediator, bv);
#endif

				m_styleSheet = styleSheet;

				// add only the specified column to this browseview.
				(Vc as OneColumnXmlBrowseViewVc).SetupOneColumnSpec(bv, icolLvHeaderToAdd);

				MakeRoot();
				// note: bv was used to initialize SortItemProvider. But we don't need it after init so null it out.
				m_bv = null;
			}

			public override void MakeRoot()
			{
				base.MakeRoot();

				ReadOnlyView = ReadOnlySelect;
				Vc.Cache = Cache;
				m_rootb.SetRootObject(m_hvoRoot, Vc, XmlBrowseViewBaseVc.kfragRoot, m_styleSheet);
				m_rootb.DataAccess = m_cache.MainCacheAccessor;
				m_dxdLayoutWidth = kForceLayout; // Don't try to draw until we get OnSize and do layout.
			}

			/// <summary>
			/// No resources need to be cleaned up specific to the OneColumnBrowseView and we need to override
			/// so we don't try to dispose content owned by the BrowseViewer that we are constructed with.
			/// </summary>
			protected override void Dispose(bool disposing)
			{
			}

			public override Point ScrollPosition
			{
				get
				{
					return base.ScrollPosition;
				}
				set { }
			}

			/// <summary>
			/// override with our own simple constructor
			/// </summary>
			internal override XmlBrowseViewBaseVc Vc => m_xbvvc ?? (m_xbvvc = new OneColumnXmlBrowseViewVc(m_nodeSpec, MainTag, this));

			/// <summary>
			/// effectively simulate infinite length so we do not wrap cell contents.
			/// </summary>
			public override int GetAvailWidth(IVwRootBox prootb)
			{
				return 1000000;
			}

			/// <summary>
			/// Return column width information for one column that takes up 100%
			/// of the available width.
			/// </summary>
			public override VwLength[] GetColWidthInfo()
			{
				Debug.Assert(Vc.ColumnSpecs.Count == 1, "Only support one column in this browse view");
				var rglength = new VwLength[1];
				rglength[0].unit = VwUnit.kunPercent100;
				rglength[0].nVal = 10000;
				return rglength;
			}

			/// <summary>
			/// we don't care about the sort order in this browseview
			/// </summary>
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
				int maxWidth;
				using (var g = Graphics.FromHwnd(Handle))
				{
					// get a best  estimate to determine row needing the greatest column width.
					var env = new MaxStringWidthForColumnEnv(StyleSheet, SpecialCache, RootObjectHvo, g, 0);
					Vc.Display(env, RootObjectHvo, XmlBrowseViewBaseVc.kfragRoot);
					maxWidth = env.MaxStringWidth;
				}
				return maxWidth;
			}

		}

		private sealed class OneColumnXmlBrowseViewVc : XmlBrowseViewVc
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
				HasSelectColumn = false;
			}

			internal void SetupOneColumnSpec(BrowseViewer bv, int icolToAdd)
			{
				ColumnSpecs = new List<XElement>(new[] { bv.ColumnSpecs[icolToAdd - bv.ColumnIndexOffset()] });
				m_icolAdded = icolToAdd;
				// if we have a bulk edit bar, we need to process strings added for Preview
				if (bv.BulkEditBar != null)
				{
					PreviewArrow = bv.BulkEditBar.PreviewArrow;
				}
			}

			/// <summary>
			/// in a OneColumn browse view, the column will be "Active"
			/// if we are based upon a column being previewed in the original browse view.
			/// </summary>
			/// <returns>1 if we're based on a column with "Preview" enabled, -1 otherwise.</returns>
			protected override int GetActiveColumn(IVwEnv vwenv, int hvoRoot)
			{
				var iActiveColumn = base.GetActiveColumn(vwenv, hvoRoot);
				if (iActiveColumn == m_icolAdded)
				{
					return 1;
				}
				return -1;
			}

			public OneColumnXmlBrowseViewVc(XElement xnSpec, int madeUpFieldIdentifier, XmlBrowseViewBase xbv)
				: base(xnSpec, madeUpFieldIdentifier, xbv)
			{
			}
		}

		/// <summary>
		/// Adjust column width to the content width
		/// References are in display order.
		/// </summary>
		internal void AdjustColumnWidthToMatchContents(int icolLvHeaderToAdjust)
		{
			if (BrowseView.RowCount == 0 || BrowseView.Vc.HasSelectColumn && icolLvHeaderToAdjust == 0)
			{
				return; // don't auto-size a select column.
			}

			// by default '0' will not change the size of the column.
			var maxStringWidth = 0;

			// setup a simple browse view that lays out the given column with infinite width (no wrapping)
			// so we can find the width of the maximum string content.
			using (new WaitCursor(this))
			{
				using (var xbv = new OneColumnXmlBrowseView(this, icolLvHeaderToAdjust))
				{
					maxStringWidth = xbv.GetMaxCellContentsWidth();
				}
			}
			// adjust column header widths
			if (maxStringWidth > 0)
			{
				// update the column according to the maxStringWidth.
				// add in a little margin to prevent wrapping in some cases.
				m_lvHeader.ColumnsInDisplayOrder[icolLvHeaderToAdjust].Width = maxStringWidth + 10;

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
			if (BrowseView?.RootBox == null)
			{
				return;
			}
			VwLength[] rglength;
			int[] widths;
			GetColWidthInfo(out rglength, out widths);
			// This is very worth checking, because this routine gets called once per column
			// while turning the sort arrow on and off, with no actual width changes.
			// I've observed that to cost 3 seconds on a large table.
			if (EqualIntArrays(widths, m_colWidths))
			{
				return;
			}
			m_colWidths = widths;
			BrowseView.RootBox.SetTableColWidths(rglength, rglength.Length);
			FilterBar?.SetColWidths(widths);
			if (fPersistNew)
			{
				SaveColumnWidths();
			}
			FireColumnsChangedEvent(true);
		}

		private void FireColumnsChangedEvent(bool isWidthChange)
		{
			ColumnsChanged?.Invoke(this, isWidthChange ? new ColumnWidthChangedEventArgs(0) : new EventArgs());
		}

		/// <summary>
		/// Save the column widths based on the current header columns. This information is used in OnLayout
		/// to restore saved column widths. Be careful to keep the way the select column
		/// is handled consistent between the two places.
		/// </summary>
		private void SaveColumnWidths()
		{
			for (var iCol = 0; iCol < m_lvHeader.Columns.Count - ColumnIndexOffset(); ++iCol)
			{
				var nNewWidth = m_lvHeader.ColumnsInDisplayOrder[ColumnHeaderIndex(iCol)].Width;
				var propName = FormatColumnWidthPropertyName(iCol);
				PropertyTable.SetProperty(propName, nNewWidth, true, true, SettingsGroup.LocalSettings);
			}
		}

		private string FormatColumnWidthPropertyName(int iCol)
		{
			return PropertyTable.GetValue<string>(AreaServices.ToolChoice) + "_" + BrowseView.GetCorrespondingPropertyName("Column") + "_" + iCol + "_Width";
		}

		/// <summary>
		/// Get the widths of the columns, both as VwLengths (for the view tables) and as actual
		/// widths (used for the filter bar).
		/// </summary>
		/// <param name="rglength"></param>
		/// <param name="widths"></param>
		public void GetColWidthInfo(out VwLength[] rglength, out int[] widths)
		{
			int dpiX;
			using (var g = CreateGraphics())
			{
				dpiX = (int)g.DpiX;
			}
			var count = m_lvHeader.Columns.Count;
			rglength = new VwLength[count];
			widths = new int[count];

			var columns = m_lvHeader.ColumnsInDisplayOrder;
			for (var i = 0; i < count; i++)
			{
				rglength[i].unit = VwUnit.kunPoint1000;
				var width = columns[i].Width;
				rglength[i].nVal = width * 72000 / dpiX;
				widths[i] = width;
			}
		}

		/// <summary>
		/// Expand the widths of the columns proportionately to fit the width of the view.
		/// </summary>
		public void MaximizeColumnWidths()
		{
			var wTotal = m_lvHeader.Width;
			var count = m_lvHeader.Columns.Count;
			var rgw = new int[count];
			var wSum = 0;
			for (var i = 0; i < count; ++i)
			{
				rgw[i] = m_lvHeader.Columns[i].Width;
				wSum += rgw[i];
			}
			if (wSum + 40 < wTotal)
			{
				var columns = m_lvHeader.ColumnsInDisplayOrder;
				for (var i = 0; i < count; ++i)
				{
					columns[i].Width = (rgw[i] * wTotal) / wSum;
				}
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
			components = new Container();
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
		protected void m_lvHeader_ColumnLeftClick(object sender, ColumnClickEventArgs e)
		{
			if (FilterBar == null)
			{
				return; // for now we can't sort without a filter bar.
			}
			var icolumn = e.Column;
			var ifsi = OrderForColumnsDisplay[icolumn - FilterBar.ColumnOffset];
			if (ifsi < 0)
			{
				// Click on the check box column; todo: maybe we could sort checked before
				// unchecked?
				return;
			}
			var fsi = FilterBar.ColumnInfo[ifsi];
			if (fsi == null)
			{
				return;
			}

			var newSorter = fsi.Sorter;
			if (newSorter.CompatibleSorter(Sorter))
			{
				// Choosing the same one again...reverse direction.
				newSorter = reverseNewSorter(newSorter, Sorter);
			}
			else if ((ModifierKeys & Keys.Shift) == Keys.Shift)
			{
				// If the user is holding shift down, we need to be working with an AndSorter
				AndSorter asorter;
				if (Sorter is AndSorter)
				{
					asorter = (AndSorter)Sorter;
					if (asorter.CompatibleSorter(newSorter))
					{
						// Same one, just reversing the direction
						var i = asorter.CompatibleSorterIndex(newSorter);
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
					asorter.Add(Sorter);
					asorter.Add(newSorter);
					m_SortersToDispose.Add(asorter);
				}
				newSorter = asorter;
			}

			SetAndRaiseSorter(newSorter, true);
		}

		private RecordSorter reverseNewSorter(RecordSorter newSorter, RecordSorter oldSorter)
		{
			var origNewSorter = newSorter; // In case of complete failure
			var grs = oldSorter as GenRecordSorter;
			if (grs == null)
			{
				return origNewSorter; // should not happen.
			}
			var sfc = grs.Comparer as StringFinderCompare;
			if (sfc == null)
			{
				return origNewSorter; // should not happen
			}
			sfc.Reverse();
			if (newSorter == oldSorter)
			{
				return newSorter;
			}
			var grs2 = newSorter as GenRecordSorter;
			if (grs2 == null)
			{
				return origNewSorter; // should not happen.
			}
			sfc.CopyTo(grs2.Comparer as StringFinderCompare);

			return newSorter;
		}

		/// <summary>
		/// Used for display purposes, we return true if the column has an active sorter and has been set to be sorted from end.
		/// </summary>
		public bool ColumnActiveAndSortedFromEnd(int icol)
		{
			if (Sorter == null)
			{
				return false; // If there isn't a sorter, no columns can be actively sorted
			}

			var sorters = Sorter is AndSorter ? ((AndSorter)Sorter).Sorters : new ArrayList {Sorter};

			// This loop attempts to locate the sorter in the list of active sorters that responsible for the column in question
			foreach (RecordSorter rs in sorters)
			{
				var ifsi = ColumnInfoIndexOfCompatibleSorter(rs);
				if (ifsi == icol - FilterBar.ColumnOffset)
				{
					return SpecificColumnSortedFromEnd(icol);
				}
			}

			// Otherwise, we return false
			return false;
		}

		/// <summary>
		/// Gets the "sorted from end" flag for the specified column.  This is implemented seperately from
		/// the CurrentColumnSortedFromEnd property because the XmlBrowseViewBase needs to know for columns
		/// other than the current one.
		/// </summary>
		private bool SpecificColumnSortedFromEnd(int icol)
		{
			var grs = GetColumnSorter(icol) as GenRecordSorter;
			if (grs == null)
			{
				return false;
			}
			Debug.Assert(grs.Comparer as StringFinderCompare != null, "Current Column does not have one of our sorters.");
			return grs.Comparer as StringFinderCompare != null && (grs.Comparer as StringFinderCompare).SortedFromEnd;
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
				var grs = GetCurrentColumnSorter() as GenRecordSorter;
				Debug.Assert(grs != null, "Current Column does not have a sorter.");
				if (grs == null)
				{
					return;
				}
				Debug.Assert(grs.Comparer as StringFinderCompare != null, "Current Column does not have one of our sorters.");
				if (grs.Comparer as StringFinderCompare != null)
				{
					(grs.Comparer as StringFinderCompare).SortedFromEnd = value;
				}
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
				var grs = GetCurrentColumnSorter() as GenRecordSorter;
				if (grs == null)
				{
					return false;
				}
				Debug.Assert(grs.Comparer as StringFinderCompare != null, "Current Column does not have one of our sorters.");
				if (grs.Comparer as StringFinderCompare != null)
				{
					return (grs.Comparer as StringFinderCompare).SortedByLength;
				}
				return false;
			}
			set
			{
				var grs = GetCurrentColumnSorter() as GenRecordSorter;
				Debug.Assert(grs != null, "Current Column does not have a sorter.");
				if (grs == null)
				{
					return;
				}
				Debug.Assert(grs.Comparer as StringFinderCompare != null, "Current Column does not have one of our sorters.");
				if (grs.Comparer as StringFinderCompare != null)
				{
					(grs.Comparer as StringFinderCompare).SortedByLength = value;
				}
			}
		}

		/// <summary>
		/// Handle right mouse button clicks in the column headers.  If the column supports
		/// sorting, bring up a menu that allows the user to choose sorting from the ends of
		/// words instead of the beginnings.  (This is useful for grouping words by suffix.)
		/// </summary>
		private void m_lvHeader_ColumnRightClick(object sender, ColumnRightClickEventArgs e)
		{
			if (FilterBar == null)
			{
				return;			// for now we can't sort without a filter bar.
			}
			var ifsi = e.Column - FilterBar.ColumnOffset;
			if (ifsi < 0)
			{
				return;			// Clicked on the check box column.
			}
			var fsi = FilterBar.ColumnInfo[ifsi];
			if (fsi == null)
			{
				return;			// Can't sort by this column.
			}
			m_icolCurrent = e.Column;

#if RANDYTODO
			IFwMainWnd window = PropertyTable.GetValue<IFwMainWnd>("window");
			window.ShowContextMenu("mnuBrowseHeader",
				new Point(Cursor.Position.X, Cursor.Position.Y),
				new TemporaryColleagueParameter(m_xbv.Mediator, this, false),
				null); // No MessageSequencer
#endif
		}

		/// <summary>
		/// Handles the config icon click.
		/// </summary>
		private void HandleConfigIconClick(ConfigIconClickEventArgs e)
		{
			var menu = components.ContextMenu("configIconContextMenu");
			// add items
			foreach (var node in BrowseView.Vc.ComputePossibleColumns())
			{
				// Show those nodes that have visibility="always" or "menu" (or none specified)
				var vis = XmlUtils.GetOptionalAttributeValue(node, "visibility", "always");
				if (vis != "always" && vis != "menu")
				{
					continue;
				}

				var label = StringTable.Table.LocalizeAttributeValue(XmlUtils.GetOptionalAttributeValue( node, "label", null)) ?? XmlUtils.GetMandatoryAttributeValue(node, "label");
				var mi = new MenuItem(label, ConfigItemClicked);

				// tick the checkbox for items that match something in current visible list.
				//Check an option if the label matches, or the unaltered label matches (for multiunicode fields)
				if (XmlViewsUtils.FindNodeWithAttrVal(ColumnSpecs, "label", label) != null || XmlViewsUtils.FindNodeWithAttrVal(ColumnSpecs, "originalLabel", label) != null)
				{
					mi.Checked = true;
				}

				menu.MenuItems.Add(mi);
			}
			menu.MenuItems.Add("-");
			menu.MenuItems.Add(XMLViewsStrings.ksMoreColumns, ConfigMoreChoicesItemClicked);

			menu.Show(this, new Point(e.Location.Left, e.Location.Bottom));
		}

		/// <summary>
		/// Remap a column index for ColumnSpecs to its matching column in m_lvHeader.Columns.
		/// This is necessary because when there is a check-box column in m_lvHeader.Columns[0],
		/// ColumnSpec[0] column == m_lvHeader.Columns[1] (everything shifts over one).
		/// </summary>
		/// <param name="columnSpecsIndex">index compatible with ColumnSpecs</param>
		/// <returns>the index that returns the ColumnSpecs equivalent node from m_lvHeader.Columns</returns>
		private int ColumnHeaderIndex(int columnSpecsIndex)
		{
			if (BrowseView.Vc.ShowColumnsRTL)
			{
				return m_lvHeader.Columns.Count - (columnSpecsIndex + 1);
			}
			if (BrowseView.Vc.HasSelectColumn)
			{
				return columnSpecsIndex + 1;
			}
			return columnSpecsIndex;
		}

		/// <summary>
		/// Since ColumnHeaderIndex(0) is no longer reliable for adjusting for the select
		/// column, here's a new method that is.
		/// </summary>
		protected int ColumnIndexOffset()
		{
			return BrowseView.Vc.HasSelectColumn ? 1 : 0;
		}

		// Handle the 'more column choices' item.
		private void ConfigMoreChoicesItemClicked(object sender, EventArgs args)
		{
			using (var dlg = new ColumnConfigureDialog(BrowseView.Vc.PossibleColumnSpecs, new List<XElement>(ColumnSpecs), PropertyTable))
			{
				dlg.RootObjectHvo = RootObjectHvo;
				dlg.FinishInitialization();

				if (BulkEditBar != null)
				{
					// If we have a Bulk Edit bar, we should show the helpful icons
					dlg.ShowBulkEditIcons = true;
				}

				if (dlg.ShowDialog(this) == DialogResult.OK)
				{
					InstallNewColumns(dlg.CurrentSpecs);
				}
			}
		}

		private static bool AreRemovingColumns(List<XElement> oldSpecs, List<XElement> newSpecs)
		{
			if (oldSpecs.Count > newSpecs.Count)
			{
				return true;
			}
			foreach (var oldSpec in oldSpecs)
			{
				if (!newSpecs.Contains(oldSpec))
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// This should be called only if AreAddingColumns() returns true!
		/// </summary>
		private static bool IsColumnOrderDifferent(List<XElement> oldSpecs, List<XElement> newSpecs)
		{
			Debug.Assert(oldSpecs.Count <= newSpecs.Count);
			return oldSpecs.Where((t, i) => t != newSpecs[i]).Any();
		}

		internal void InstallNewColumns(List<XElement> newColumnSpecs)
		{
			BulkEditBar?.SaveSettings(); // before we change column list!
			var fRemovingColumn = AreRemovingColumns(ColumnSpecs, newColumnSpecs);
			var fOrderChanged = true;
			if (!fRemovingColumn)
			{
				fOrderChanged = IsColumnOrderDifferent(ColumnSpecs, newColumnSpecs);
			}
			// We begin by saving the current column widths to preserve widths as far as possible
			// when recreating the list in OK.
			Dictionary<XElement, int> widths;
			StoreColumnWidths(ColumnSpecs, out widths);

			// Copy configured list back to ColumnSpecs, update display, etc.x
			ColumnSpecs = newColumnSpecs;
			// Rebuild header columns based on the widths we stored
			RebuildHeaderColumns(ColumnSpecs, widths);
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
			Scroller.PerformLayout();
		}

		private void RebuildHeaderColumns(List<XElement> colSpecs, Dictionary<XElement, int> widths)
		{
			m_lvHeader.BeginUpdate();
			var fSave = m_lvHeader.AdjustingWidth;
			m_lvHeader.AdjustingWidth = true;	// Don't call AdjustColumnWidths() for each column.
			// Remove all columns (except the 'select' column if any).
			// We don't need to use ColumnsInDisplayOrder here because we don't allow the select column to be re-ordered.
			while (m_lvHeader.Columns.Count > ColumnIndexOffset())
			{
				m_lvHeader.Columns.RemoveAt(m_lvHeader.Columns.Count - 1);
			}
			// Build the list of column headers.
			var dpiX = GetDpiX();
			var rghdr = new ColumnHeader[colSpecs.Count];
			for (var i = 0; i < colSpecs.Count; ++i)
			{
				var node = colSpecs[i];
				var ch = MakeColumnHeader(node);
				//Set the width either to the temporarily stored width, or the default initial column width
				int width;
				ch.Width = widths.TryGetValue(node, out width) ? width : GetInitialColumnWidth(node, Scroller.ClientRectangle.Width, dpiX);
				if (BrowseView.Vc.ShowColumnsRTL)
				{
					var iRev = colSpecs.Count - (i + 1);
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

		private void StoreColumnWidths(List<XElement> colSpecs, out Dictionary<XElement, int> widths)
		{
			widths = new Dictionary<XElement, int>();
			var index = 0;
			foreach(var node in colSpecs)
			{
				widths.Add(node, m_lvHeader.ColumnsInDisplayOrder[ColumnHeaderIndex(index)].Width);
				index++;
			}
		}

		private void m_lvHeader_ColumnDragDropReordered(object sender, ColumnDragDropReorderedEventArgs e)
		{
			// If we have changes we need to commit, do it before we mess up the column sequence.
			BulkEditBar?.SaveSettings(); // before we change column list!
			// Sync the Browse View columns with the new column header order.
			var columnsOrder = e.DragDropColumnOrder;
			var oldSpecs = new List<XElement>(ColumnSpecs);
			var delta = ColumnIndexOffset();
			Debug.Assert(columnsOrder.Count == ColumnSpecs.Count + delta);
			for (var i = 0; i < ColumnSpecs.Count; ++i)
			{
				BrowseView.Vc.ColumnSpecs[i] = oldSpecs[columnsOrder[i + delta] - delta];
			}
			// The drag and drop operation had a side effect of calling
			// AdjustColumnWidths. But it is too soon to do that for some things, because we
			// haven't updated all the column lists (see e.g. LT-4868). Make sure the next
			// AdjustColumnWidths, after we have finished setting up data, will really adjust.
			m_colWidths = null;

			// Display the browse view with the new column order
			UpdateColumnList();

			SyncSortArrows(Sorter); // Sync sort arrows
		}

		/// <summary>
		/// Method for handling the event of a normal item in the configure menu being clicked on.
		/// </summary>
		private void ConfigItemClicked(object sender, EventArgs args)
		{
			// If we have changes we need to commit, do it before we mess up the column sequence.
			BulkEditBar?.SaveSettings(); // before we change column list!
			var mi = sender as MenuItem;
			var newColumns = new List<XElement>(ColumnSpecs);
			var possibleColumns = BrowseView.Vc.PossibleColumnSpecs;
			//set the column to any column in the specs that matches the menu item text
			// or the unaltered text (for multiunicode fields).
			var column = XmlViewsUtils.FindNodeWithAttrVal(ColumnSpecs, "label", mi.Text) ?? XmlViewsUtils.FindNodeWithAttrVal(ColumnSpecs, "originalLabel", mi.Text);
			var fRemovingColumn = true;
			var fOrderChanged = false;
			//The column with this label was not found in the current columns
			if (column == null)
			{
				//therefore we are inserting, not removing
				fRemovingColumn = false;
				//find the column with the matching label in the possible columns
				column = XmlViewsUtils.FindNodeWithAttrVal(possibleColumns, "label", mi.Text);
			}
			var position = XmlViewsUtils.FindIndexOfMatchingNode(ColumnSpecs, column);
			if (fRemovingColumn)
			{
				// Was visible, make it go away. (But not the very last item.)
				if (newColumns.Count == 1)
				{
					MessageBox.Show(this, XMLViewsStrings.ksBrowseNeedsAColumn, XMLViewsStrings.ksCannotRemoveColumn);
					return;
				}
				newColumns.RemoveAt(position);
			}
			else
			{
				// Was invisible, make it appear.
				// Figure where to put it based on how many active columns come before it.
				var menu = mi.Parent;
				position = 0; // will become the number of checked items before mi
				for (var i = 0; i < mi.Index; i++)
				{
					if (menu.MenuItems[i].Checked)
					{
						position++;
					}
				}
				if (position < newColumns.Count)
				{
					fOrderChanged = true;
				}
				newColumns.Insert(position, column);
			}
			InstallNewColumns(newColumns);
		}

		private FilterBarCellFilter MakeFilter(List<XElement> possibleColumns, string colName, IMatcher matcher)
		{
			var colSpec = XmlViewsUtils.FindNodeWithAttrVal(possibleColumns, "label", colName);
			if (colSpec == null)
			{
				return null;
			}
			var app = PropertyTable.GetValue<IApp>("App");
			var finder = LayoutFinder.CreateFinder(Cache, colSpec, BrowseView.Vc, app);
			return new FilterBarCellFilter(finder, matcher);
		}

		/// <summary>
		/// This method is used to set the filters for
		/// Edit Spelling Status   "ReviewUndecidedSpelling"
		/// and
		/// View Incorrect Words in use  "CorrectSpelling"
		/// and
		/// Filter for Lexical Entries with this category  "LexiconEditFilterAnthroItems"
		/// and
		/// Filter for Notebook Records with this category  "NotebookEditFilterAnthroItems"
		///
		/// Some aspects of the initial state of the tab can be controlled by setting properties from
		/// an FwLink which brought us here. If this is the case, generate a corresponding filter and
		/// return it; otherwise, return null.
		/// </summary>
		internal RecordFilter FilterFromLink(FwLinkArgs linkArgs)
		{
			if (linkArgs == null)
			{
				return null;
			}
			var linkProperties = linkArgs.LinkProperties;
			var linkSetupInfoProperty = linkProperties.FirstOrDefault(prop => prop.Name == "LinkSetupInfo");
			var linkSetupInfoValue = linkSetupInfoProperty?.Value as string;
			if (string.IsNullOrWhiteSpace(linkSetupInfoValue))
			{
				return null;
			}
			if (linkSetupInfoValue != "ReviewUndecidedSpelling" && linkSetupInfoValue != "CorrectSpelling" && linkSetupInfoValue != "FilterAnthroItems")
			{
				return null; // Only settings we know as yet.
			}

			var possibleColumns = BrowseView.Vc.ComputePossibleColumns();

			switch (linkSetupInfoValue)
			{
				case "ReviewUndecidedSpelling":
				case "CorrectSpelling":
				{
					var colSpec = XmlViewsUtils.FindNodeWithAttrVal(possibleColumns, "label", "Spelling Status");
					if (colSpec == null)
					{
						return null;
					}
					int desiredItem;
					if (linkSetupInfoValue == "CorrectSpelling")
					{
						desiredItem = (int)SpellingStatusStates.correct;
					}
					else //linkSetupInfo == "ReviewUndecidedSpelling"
					{
						desiredItem = (int)SpellingStatusStates.undecided;
					}

					var labels = BrowseView.GetStringList(colSpec);
					if (labels == null || labels.Length < desiredItem)
					{
						return null;
					}
					var correctLabel = labels[desiredItem];

					FilterBarCellFilter spellFilter;
					if (linkSetupInfoValue == "CorrectSpelling") //"Exclude Correct" TE-8200
					{
						// Use this one for NOT Correct ("Exclude Correct"), that is, undecided OR incorrect,
						// all things that have squiggles. (could also be "Exclude Incorrect" or "Exclude Undecided")
						spellFilter = MakeFilter(possibleColumns, "Spelling Status", new InvertMatcher(new ExactMatcher(FilterBar.MatchExactPattern(correctLabel))));
					}
					else // linkSetupInfo == "ReviewUndecidedSpelling"   --> "Undecided"
					{
						spellFilter = MakeFilter(possibleColumns, "Spelling Status", new ExactMatcher(FilterBar.MatchExactPattern(correctLabel)));
					}

					var occurrenceFilter = MakeFilter(possibleColumns, "Number in Corpus", new RangeIntMatcher(1, Int32.MaxValue));
					var andFilter = new AndFilter();
					andFilter.Add(spellFilter);
					andFilter.Add(occurrenceFilter);
					// If we need to change the list of objects, best to do it now, before we load the list
					// because of the change filter, and suppress the next load.
					return andFilter;
				}
				case "FilterAnthroItems":
				{
					var hvoOfAnthroItemProperty = linkProperties.FirstOrDefault(prop => prop.Name == "HvoOfAnthroItem");
					var itemHvo = hvoOfAnthroItemProperty?.Value as string;
					if (string.IsNullOrWhiteSpace(linkSetupInfoValue))
					{
						return null;
					}

					var colSpec = XmlViewsUtils.FindNodeWithAttrVal(possibleColumns, "label", "Anthropology Categories");
					if (colSpec == null)
					{
						return null;
					}

					var chosenHvos = ParseCommaDelimitedHvoString(itemHvo);
					ListChoiceFilter filterListChoice = new ColumnSpecFilter(Cache, ListMatchOptions.Any, chosenHvos, colSpec);
					filterListChoice.MakeUserVisible(true);
					return filterListChoice;
				}
			}
			return null;
		}

		private static int[] ParseCommaDelimitedHvoString(string itemHvos)
		{
			var shvoArray = itemHvos.Split(new[] {","}, StringSplitOptions.RemoveEmptyEntries);
			var chosenHvos = new int[shvoArray.Length];
			for (var i = 0; i < shvoArray.Length; i++)
			{
				chosenHvos[i] = Convert.ToInt32(shvoArray[i]);
			}
			return chosenHvos;
		}

		/// <summary>
		/// This is called after a link is followed. It allows us to set up the desired filter
		/// etc. even if the desired tool was already active.
		/// </summary>
		private void LinkFollowed_Handler(object newValue)
		{
			var filter = FilterFromLink((FwLinkArgs)newValue);
			if (filter == null)
			{
				return;
			}
			if (filter == m_currentFilter)
			{
				return;
			}
			FilterChanged?.Invoke(this, new FilterChangeEventArgs(filter, m_currentFilter));
			FilterInitializationComplete = false; // allows UpdateFilterBar to add columns
			UpdateFilterBar(filter);
			Scroller.PerformLayout(); // cause scroll bar to appear or disappear, etc.
		}

		/// <summary>
		/// Allows client to substitute a different column spec for a current column.
		/// </summary>
		public XElement ReplaceColumn(int index, XElement colSpec)
		{
			// If we have changes we need to commit, do it before we mess up the column sequence.
			BulkEditBar?.SaveSettings(); // before we change column list!
			var result = ColumnSpecs[index];
			ColumnSpecs[index] = colSpec; // will throw if bad index...but we'd just throw anyway.
			UpdateColumnList();
			return result;
		}

		/// <summary>
		/// Allows client to substitute a different column spec for a current column.
		/// </summary>
		public XElement ReplaceColumn(string colName, XElement colSpec)
		{
			var index = IndexOfColumn(colName);
			Debug.Assert(index >= 0, "looking for invalid column in view");
			return ReplaceColumn(index, colSpec);
		}

		/// <summary>
		/// Get the index of the column that has the specified name/label
		/// </summary>
		private int IndexOfColumn(string colName)
		{
			return ColumnSpecs.FindIndex(item => XmlUtils.GetOptionalAttributeValue(item, "label") == colName);
		}

		/// <summary>
		/// Insert column at the given position.
		/// </summary>
		protected void InsertColumn(XElement columnSpecification, int columnIndex)
		{
			var columns = ColumnSpecs;
			columns.Insert(columnIndex, columnSpecification);
			var colHeader = MakeColumnHeader(columnSpecification);
			var colWidth = GetInitialColumnWidth(columnSpecification, Scroller.ClientRectangle.Width, GetDpiX());
			colHeader.Width = colWidth;
			m_lvHeader.SuppressColumnWidthChanges = true;
			m_lvHeader.Columns.Insert(ColumnHeaderIndex(columnIndex), colHeader);
			m_lvHeader.SuppressColumnWidthChanges = false;
			// Setting the column width and inserting it into the header had a side effect of calling
			// AdjustColumnWidths. But it is too soon to do that for some things, because we
			// haven't updated all the column lists (see e.g. LT-4804). Make sure the next
			// AdjustColumnWidths, after we have finished setting up data, will really adjust.
			m_colWidths = null;
			var fNeedLayout = colHeader.Width < colWidth;
			if (fNeedLayout)
			{
				// truncated because the header as a whole is too narrow.
				m_lvHeader.Width += colWidth - colHeader.Width;
				colHeader.Width = colWidth;
			}
		}

		// Note: often we also want to update LayoutCache.LayoutVersionNumber.
		// (last updated by Jason Naylor, Nov 16, 2016, for ExtendedNote Bulk editing)
		internal const int kBrowseViewVersion = 18;

		/// <summary>
		/// Column has been added or removed, update all child windows.
		/// </summary>
		protected void UpdateColumnList()
		{
			using (new ReconstructPreservingBVScrollPosition(this))
			{
				//m_xbv.UpdateColumnList(); // Only did RootBox.Reconstruct()
				if (FilterBar != null)
				{
					FilterBar.UpdateColumnList();
					// see if we can re-enstate our column filters
					FilterBar.UpdateActiveItems(m_currentFilter);
					// see if we need to re-enstate our column header sort arrow.
					InitSorter(Sorter);
				}
				BulkEditBar?.UpdateColumnList();
				// That doesn't fix columns added at the end, which the .NET code helpfully adjusts to
				// one pixel wide each if the earlier columns use all available space!
				var ccols = m_lvHeader.Columns.Count;
				var columns = m_lvHeader.ColumnsInDisplayOrder;
				for (var iAdjustCol = ccols - 1; iAdjustCol > 0; iAdjustCol--)
				{
					var adjust = DhListView.kMinColWidth - columns[ccols - 1].Width;
					if (adjust <= 0)
					{
						break; // only narrow columns at the end are a problem
					}
					for (var icol = iAdjustCol - 1; icol >= 0 && adjust > 0; icol--)
					{
						// See if we can narrow column icol by enough to fix things.
						var avail = columns[icol].Width - DhListView.kMinColWidth;
						if (avail > 0)
						{
							var delta = Math.Min(avail, adjust);
							adjust -= delta;
							columns[icol].Width -= delta;
						}
					}
				}

				// Make everthing else match, but do NOT remember settings.
				// This method gets called when adding a column to make a filter visible,
				// and should not interfere with any widths previously saved.
				AdjustColumnWidths(false); //Column widths may be needed during the RootBoxReconstruct() LT-10315
			} // End using(ReconstructPreservingBVScrollPosition) [Does RootBox.Reconstruct() here.]

			FireColumnsChangedEvent(false);

			// And have the property table remember the list of columns the user wants.
			// This information is used in the XmlBrowseViewBaseVc constructor to select the
			// columns to display.
			// Exception: if any column has 'doNotPersist="true"' skip saving.
			var colList = new StringBuilder();
			colList.Append("<root version=\"" + kBrowseViewVersion + "\">");
			foreach (var node in ColumnSpecs)
			{
				if (XmlUtils.GetOptionalBooleanAttributeValue(node, "doNotPersist", false))
				{
					return; // without saving column info!
				}
				var layout = XmlUtils.GetOptionalAttributeValue(node, "layout");
				var normalLayout = XmlUtils.GetOptionalAttributeValue(node, "normalLayout");
				if (layout != null && normalLayout != null && layout != normalLayout)
				{
					// persist it with the normal layout, not the special edit one.
					XmlUtils.SetAttribute(node, "layout", normalLayout);
					colList.Append(node);
					XmlUtils.SetAttribute(node, "layout", layout);
				}
				else
				{
					colList.Append(node);
				}
			}
			colList.Append("</root>");
			PropertyTable.SetProperty(BrowseView.Vc.ColListId, colList.ToString(), true, true, SettingsGroup.LocalSettings);
		}

		/// <summary>
		/// Append hidden columns that match the given (active) filter to our active columns list.
		/// </summary>
		internal bool AppendMatchingHiddenColumns(RecordFilter filter)
		{
			if (FilterInitializationComplete || filter == null)
			{
				return false;
			}
			var fInsertedColumn = false;
			foreach (var colSpec in BrowseView.Vc.ComputePossibleColumns())
			{
				// detect if hidden column (hidden columns are not in our active ColumnSpecs)
				if (!IsColumnHidden(colSpec) || !FilterBar.CanActivateFilter(filter, colSpec))
				{
					continue;
				}
				// append column to end of column list
				AppendColumn(colSpec);
				fInsertedColumn = true;
			}
			return fInsertedColumn;
		}

		/// <summary />
		protected internal bool IsColumnShowing(XElement colSpec)
		{
			//for some reason column specs may not have a layout set, fall back to the old test
			if (colSpec == null || !colSpec.HasAttributes || colSpec.Attribute("layout") == null)
			{
				return XmlViewsUtils.FindIndexOfMatchingNode(ColumnSpecs, colSpec) >= 0;
			}
			//Be as non-specific about the column as we can, writing system options and width and other things may give false negatives
			return XmlViewsUtils.FindNodeWithAttrVal(ColumnSpecs, "layout", colSpec.Attribute("layout").Value) != null;
		}

		/// <summary />
		protected internal bool IsColumnHidden(XElement colSpec)
		{
			return !IsColumnShowing(colSpec);
		}

		/// <summary />
		protected internal void AppendColumn(XElement colSpec)
		{
			InsertColumn(colSpec, ColumnSpecs.Count);
		}

		internal List<XElement> ColumnSpecs
		{
			get
			{
				return BrowseView.Vc.ColumnSpecs;
			}
			set
			{
				BrowseView.Vc.ColumnSpecs = value;
			}
		}

		/// <summary>
		/// Create a sorter for the first column that is in the desired writing system
		/// class (vernacular or analysis).
		/// </summary>
		/// <remarks>This is needed for LT-10293.</remarks>
		public RecordSorter CreateSorterForFirstColumn(int ws)
		{
			XElement colSpec = null;
			CoreWritingSystemDefinition colWs = null;
			foreach (var curSpec in BrowseView.Vc.ColumnSpecs)
			{
				var curWs = WritingSystemServices.GetWritingSystem(Cache, FwUtils.ConvertElement(curSpec), null, 0);
				if (curWs.Handle == ws)
				{
					colSpec = curSpec;
					colWs = curWs;
					break;
				}
			}

			if (colSpec != null)
			{
				var finder = LayoutFinder.CreateFinder(Cache, colSpec, BrowseView.Vc, PropertyTable.GetValue<IApp>("App"));
				return new GenRecordSorter(new StringFinderCompare(finder, new WritingSystemComparer(colWs)));
			}
			return null;
		}

		private void m_xbv_SelectedIndexChanged(object sender, EventArgs e)
		{
			SelectedIndexChanged?.Invoke(this, new EventArgs());
		}

#if RANDYTODO
		/// <summary>
		/// Enable the "Sort from end" menu command.
		/// </summary>
		/// <param name="commandObject">The command object.</param>
		/// <param name="display">The display.</param>
		/// <returns></returns>
		public bool OnDisplaySortedFromEnd(object commandObject,
			ref UIItemDisplayProperties display)
		{
			display.Visible = true;
			display.Enabled = true;
			display.Checked = CurrentColumnSortedFromEnd;
			return true;
		}

		/// <summary>
		/// Enable the "Sort from end" menu command.
		/// </summary>
		/// <param name="commandObject">The command object.</param>
		/// <param name="display">The display.</param>
		/// <returns></returns>
		public bool OnDisplaySortedByLength(object commandObject, ref UIItemDisplayProperties display)
		{
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
				display.Checked = CurrentColumnSortedByLength;
			}
			return true;
		}
#endif

		private RecordSorter GetCurrentColumnSorter()
		{
			return GetColumnSorter(m_icolCurrent);
		}

		private RecordSorter GetColumnSorter(int icol)
		{
			if (FilterBar == null)
			{
				return null;
			}
			var ifsi = icol - FilterBar.ColumnOffset;
			if (ifsi < 0)
			{
				// Should not happen.
				return null;
			}
			var fsi = FilterBar.ColumnInfo[ifsi];
			return fsi?.Sorter;
		}

		/// <summary>
		/// Receives the broadcast message "PropertyChanged".  This message results from left clicking
		/// in the context menu generated by a right mouse button click in the column headers.
		/// </summary>
		public virtual void OnPropertyChanged(string name)
		{
			if (FilterBar == null || Sorter == null)
			{
				return;
			}

			// TODO: REVISIT: If more than one BrowseViewer gets this message, how do
			// we know which one should handle it?  Can't we handle this some other way?
			switch(name)
			{
				default:
					return;
				case "SortedFromEnd":
					CurrentColumnSortedFromEnd = !CurrentColumnSortedFromEnd;
					break;
				case "SortedByLength":
					CurrentColumnSortedByLength = !CurrentColumnSortedByLength;
					break;
			}
			SetAndRaiseSorter(Sorter, true);
		}

		private void m_checkMarkButton_Click(object sender, EventArgs e)
		{
			CheckIconClick(this, new ConfigIconClickEventArgs(RectangleToClient(m_checkMarkButton.RectangleToScreen(m_checkMarkButton.ClientRectangle))));
		}

		private void CheckIconClick(object sender, ConfigIconClickEventArgs e)
		{
			var menu = components.ContextMenu("CheckIconClickContextMenuStrip", false);
			if (menu.MenuItems.Count == 0)
			{
				menu.MenuItems.Add(XMLViewsStrings.ksCheckAll, OnCheckAll);
				menu.MenuItems.Add(XMLViewsStrings.ksUncheckAll, OnUncheckAll);
				menu.MenuItems.Add(XMLViewsStrings.ksToggle, OnToggleAll);
			}
			menu.Show(this, new Point(e.Location.Left, e.Location.Bottom));
		}

		/// <summary>
		/// Get all the main items in this browse view.
		/// </summary>
		public List<int> AllItems
		{
			get
			{
				var chvo = SpecialCache.get_VecSize(RootObjectHvo, MainTag);
				int[] contents;
				using (var arrayPtr = MarshalEx.ArrayToNative<int>(chvo))
				{
					SpecialCache.VecProp(RootObjectHvo, MainTag, chvo, out chvo, arrayPtr);
					contents = MarshalEx.NativeToArray<int>(arrayPtr, chvo);
				}
				return new List<int>(contents);
			}
		}

		/// <summary>
		/// Get all the main items with a check mark in this browse view;
		/// </summary>
		public List<int> CheckedItems
		{
			get
			{
				if (!BrowseView.Vc.HasSelectColumn)
				{
					return null;
				}

				var hvoRoot = RootObjectHvo;
				var tagMain = MainTag;
				var checkedItems = new List<int>();
				ISilDataAccess sda = SpecialCache;
				var citems = sda.get_VecSize(hvoRoot, tagMain);
				for (var i = 0; i < citems; ++i)
				{
					var hvoItem = sda.get_VecItem(hvoRoot, tagMain, i);
					// If there's no value in the cache, we still want to add it
					// if the default state is CHECKED (cf XmlBrowseViewBaseVc.LoadData).
					if (GetCheckState(hvoItem) == 1)
					{
						checkedItems.Add(hvoItem);
					}
				}
				return checkedItems;
			}
		}

		/// <summary>
		/// Make sure all items in rghvo are checked.
		/// </summary>
		public void SetCheckedItems(IList<int> rghvo)
		{
			if (!BrowseView.Vc.HasSelectColumn)
			{
				return;
			}

			var chvo = SpecialCache.get_VecSize(RootObjectHvo, MainTag);
			int[] contents;
			using (var arrayPtr = MarshalEx.ArrayToNative<int>(chvo))
			{
				SpecialCache.VecProp(RootObjectHvo, MainTag, chvo, out chvo, arrayPtr);
				contents = MarshalEx.NativeToArray<int>(arrayPtr, chvo);
			}

			var changedHvos = new List<int>();
			foreach (var hvoItem in contents)
			{
				var newVal = (rghvo != null && rghvo.Contains(hvoItem)) ? 1 : 0;
				var currentValue = GetCheckState(hvoItem);
				SetItemCheckedState(hvoItem, newVal, currentValue != newVal);
				if (currentValue != newVal)
				{
					changedHvos.Add(hvoItem);
				}
			}
			OnCheckBoxChanged(changedHvos.ToArray());
		}

		/// <summary>
		/// Determine whether the specified item is currently considered to be checked.
		/// </summary>
		public bool IsItemChecked(int hvoItem)
		{
			return GetCheckState(hvoItem) == 1;
		}

		private bool m_fIsInitialized;
		/// <summary />
		protected bool m_fInUpdateCheckedItems;
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
				if (BrowseView?.Vc != null && BrowseView.Vc.HasSelectColumn && BulkEditBar != null)
				{
					// everything seems to be setup, and UpdateCheckedItems should be called
					// after everything is setup.
					m_fIsInitialized = true;
					// we want the current items list to inherit their selection status
					// from its relatives on a previous list
					// (LT-8986)
					if (SortItemProvider is IMultiListSortItemProvider)
					{
						if (m_fSavedSelectionsDuringFilterChange)
						{
							RestoreSelectionItems();
							m_fSavedSelectionsDuringFilterChange = false;
						}
						else if (m_lastChangedSelectionListItemsClass != 0 && m_lastChangedSelectionListItemsClass != BrowseView.Vc.ListItemsClass)
						{
							ConvertItemsToRelativesThatApplyToCurrentListSelections();
							RestoreSelectionItems();
						}
					}
					BulkEditBar.UpdateCheckedItems();
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
		internal virtual int GetCheckState(int hvoItem)
		{
			return SpecialCache.get_IntProp(hvoItem, XMLViewsDataCache.ktagItemSelected);
		}

		private void OnCheckAll(object sender, EventArgs e)
		{
			ResetAll(BrowseViewerCheckState.CheckAll);
		}

		/// <summary>
		/// Actually checks if val is 1, unchecks if val is 0.
		/// Toggles if value is -1
		/// </summary>
		/// <param name="newState">The new state.</param>
		internal virtual void ResetAll(BrowseViewerCheckState newState)
		{
			var changedHvos = ResetAllCollectChangedHvos(newState);
			ResetAllHandleBulkEditBar();
			OnCheckBoxChanged(changedHvos.ToArray());
			ResetAllHandleReconstruct();
		}

		/// <summary />
		protected void ResetAllHandleReconstruct()
		{
			using (new ReconstructPreservingBVScrollPosition(this))
			{
			}
		}

		/// <summary />
		protected List<int> ResetAllCollectChangedHvos(BrowseViewerCheckState newState)
		{
			var chvo = SpecialCache.get_VecSize(RootObjectHvo, MainTag);
			int[] contents;
			using (var arrayPtr = MarshalEx.ArrayToNative<int>(chvo))
			{
				SpecialCache.VecProp(RootObjectHvo, MainTag, chvo, out chvo, arrayPtr);
				contents = MarshalEx.NativeToArray<int>(arrayPtr, chvo);
			}

			var changedHvos = new List<int>();
			foreach (var hvoItem in contents)
			{
				var newVal = 0;
				var currentValue = GetCheckState(hvoItem);
				switch (newState)
				{
					case BrowseViewerCheckState.ToggleAll:
						newVal = currentValue == 0 ? 1 : 0;
						break;
					case BrowseViewerCheckState.CheckAll:
						newVal = 1;
						break;
					case BrowseViewerCheckState.UncheckAll:
						newVal = 0;
						break;
				}
				SetItemCheckedState(hvoItem, newVal, false);
				if (currentValue != newVal)
				{
					changedHvos.Add(hvoItem);
				}
			}
			return changedHvos;
		}

		/// <summary />
		protected void ResetAllHandleBulkEditBar()
		{
			if (BulkEditBar != null && BulkEditBar.Visible)
			{
				BulkEditBar.SetEnabledIfShowing();
			}
		}

		internal void SetItemCheckedState(int hvoItem, bool selectItem, bool propertyDidChange)
		{
			SetItemCheckedState(hvoItem, Convert.ToInt32(selectItem), propertyDidChange);
		}

		/// <summary />
		protected void SetItemCheckedState(int hvoItem, int newVal, bool propertyDidChange)
		{
			// Note that we want to set it even if the value apparently hasn't changed,
			// because a value of zero might just be a default from finding nothing in
			// the cache, but 'not found' might actually be interpreted as 'checked'.

			// No.
			//Cache.VwCacheDaAccessor.CacheIntProp(hvoItem, XMLViewsDataCache.ktagItemSelected, newVal);
			// Use m_specialCache instead.
			SpecialCache.SetInt(hvoItem, XMLViewsDataCache.ktagItemSelected, newVal);
		}

		/// <summary />
		protected void OnUncheckAll(object sender, EventArgs e)
		{
			ResetAll(BrowseViewerCheckState.UncheckAll);
		}

		/// <summary />
		protected void OnToggleAll(object sender, EventArgs e)
		{
			ResetAll(BrowseViewerCheckState.ToggleAll);
		}

		/// <summary />
		protected void m_configureButton_Click(object sender, EventArgs e)
		{
			HandleConfigIconClick(new ConfigIconClickEventArgs(RectangleToClient(m_configureButton.RectangleToScreen(m_configureButton.ClientRectangle))));
		}

		private void m_scrollBar_ValueChanged(object sender, EventArgs e)
		{
			BrowseView.ScrollPosition = new Point(0, ScrollBar.Value);
		}

		/// <summary>
		/// Snaps the split position.
		/// </summary>
		public bool SnapSplitPosition(ref int width)
		{
			if (m_lvHeader == null || m_lvHeader.Columns.Count < 1)
			{
				return false;
			}
			var snapPos = m_lvHeader.ColumnsInDisplayOrder[0].Width + ScrollBar.Width;
			// This guards against snapping when we have just one column that is stretching.
			// When that happens, 'snapping' just prevents other behaviors, like
			if (m_lvHeader.Columns.Count == 1 && snapPos >= width - 2)
			{
				return false;
			}
			if (width < snapPos + 10 && width > snapPos / 2)
			{
				width = snapPos;
				return true;
			}
			return false;
		}

		/// <summary>
		/// Called when [remove filters].
		/// </summary>
		public void OnRemoveFilters(object sender)
		{
			FilterBar?.RemoveAllFilters();
		}

		/// <summary>
		/// Launch dialog for configuring browse view column choices.
		/// </summary>
		internal void OnConfigureColumns(object sender)
		{
			ConfigMoreChoicesItemClicked(sender, new EventArgs());
		}

		#region IMainContentControl Members

		/// <summary />
		public string AreaName => PropertyTable.GetValue<string>(AreaServices.AreaChoice);

		/// <summary>
		/// This is called on a MasterRefresh
		/// </summary>
		/// <returns></returns>
		public bool PrepareToGoAway()
		{
			BulkEditBar?.SaveSettings();
			return true;
		}

		#endregion IMainContentControl Members

		#region ICtrlTabProvider Members

		/// <summary />
		public Control PopulateCtrlTabTargetCandidateList(List<Control> targetCandidates)
		{
			// Note: when switching panes, we want to give the focus to the BrowseView, not the BrowseViewer.
			Control focusedControl = null;
			if (BrowseView != null)
			{
				targetCandidates.Add(BrowseView);
				if (BrowseView.ContainsFocus)
				{
					focusedControl = BrowseView;
				}
			}
			if (BulkEditBar != null)
			{
				targetCandidates.Add(BulkEditBar);
				if (focusedControl == null && BulkEditBar.ContainsFocus)
				{
					focusedControl = BulkEditBar;
				}
			}
			return focusedControl;
		}

		#endregion

		/// <summary>
		/// Expose the property of the underlying view.
		/// </summary>
		public SelectionHighlighting SelectedRowHighlighting
		{
			get { return BrowseView.SelectedRowHighlighting; }
			set { BrowseView.SelectedRowHighlighting = value; }
		}

		/// <summary>
		/// This is used to keep track of the positions of columns after they have been dragged and dropped.
		/// OrderForColumnsDisplay[i] is the index of the position where the column at
		/// position i in the orginal Columns collection is actually displayed.
		/// </summary>
		public List<int> OrderForColumnsDisplay
		{
			get
			{
				// gracefully handle the situation where the list view has not initialized this yet.
				// If columns have been added since any reordering, then ensure that's reflected here.
				// (See LT-14879.)
				if (m_orderForColumnsDisplay.Count < ColumnCount)
				{
					var min = m_orderForColumnsDisplay.Count;
					for (var i = min; i < ColumnCount; i++)
					{
						m_orderForColumnsDisplay.Add(i);
					}
				}
				Debug.Assert(m_orderForColumnsDisplay.Count == ColumnCount);
				return m_orderForColumnsDisplay;
			}
			set { m_orderForColumnsDisplay = value; }
		}
		private List<int> m_orderForColumnsDisplay = new List<int>();

		// In your AllItems list, the specified objects have been replaced (typically dummy to real).
		internal void FixReplacedItems(Dictionary<int, int> replacedObjects)
		{
			// As far as I can tell, this never happens in 7.0+; probably it was ONLY used for replacing dummy with real objects,
			// which no longer happens. The necessary action is possibly to update the list of items shown in the browse view
			// by replacing the keys in replacedObjects with the corresponding values (but this might happen anyway as a
			// result of some automatic PropChanged), and also to set the selected status of the replacement to match
			// the selected status of the original in the decorator that manages the check marks on the rows.
			throw new NotSupportedException("Attempt to execute FW 6.0 code that was believed obsolete and not ported");
		}

		/// <summary>
		/// Pass the message on to the main browse view so it can adjust its scroll position.
		/// </summary>
		public void PostLayoutInit()
		{
			BrowseView.PostLayoutInit();
		}

		#region IRefreshableRoot Members
		/// <summary>
		/// Handle the refresh display, notify the editbar that we have refreshed, return false
		/// so our child controls can actually handle the re-drawing.
		/// </summary>
		/// <returns></returns>
		public bool RefreshDisplay()
		{
			RefreshCompleted?.Invoke(this, new EventArgs());
			SetListModificationInProgress(false);
			return false;
		}

		#endregion

		#region Implementation of IPropertyTableProvider

		/// <summary>
		/// Placement in the IPropertyTableProvider interface lets FwApp call IPropertyTable.DoStuff.
		/// </summary>
		public IPropertyTable PropertyTable { get; private set; }

		#endregion

		#region Implementation of IPublisherProvider

		/// <summary>
		/// Get the IPublisher.
		/// </summary>
		public IPublisher Publisher { get; private set; }

		#endregion

		#region Implementation of ISubscriberProvider

		/// <summary>
		/// Get the ISubscriber.
		/// </summary>
		public ISubscriber Subscriber { get; private set; }

		#endregion

		#region Implementation of IFlexComponent

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		public virtual void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			FlexComponentCheckingService.CheckInitializationValues(flexComponentParameters, new FlexComponentParameters(PropertyTable, Publisher, Subscriber));

			PropertyTable = flexComponentParameters.PropertyTable;
			Publisher = flexComponentParameters.Publisher;
			Subscriber = flexComponentParameters.Subscriber;

			// Make the right subclass of XmlBrowseViewBase first, the column header creation uses information from it.
			if (m_configParamsElement == null)
			{
				// Tests.
			}
			else if (m_configParamsElement.Attribute("editRowModelClass") != null)
			{
				BrowseView = new XmlBrowseRDEView(); // Use special RDE class.
			}
			else
			{
				BrowseView = new XmlBrowseView();
			}
			BrowseView.InitializeFlexComponent(flexComponentParameters);
			BrowseView.AccessibleName = "BrowseViewer";

			Subscriber.Subscribe("LinkFollowed", LinkFollowed_Handler);
		}

		#endregion
	}
}