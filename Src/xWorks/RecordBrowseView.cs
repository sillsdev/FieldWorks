// Copyright (c) 2003-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using static SIL.FieldWorks.Common.FwUtils.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Widgets;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Application;
using SIL.LCModel.Infrastructure;
using SIL.LCModel.Utils;
using SIL.Reporting;
using SIL.FieldWorks.FdoUi;
using SIL.FieldWorks.Filters;
using SIL.Utils;
using XCore;
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;
using ECInterfaces;
using SilEncConverters40;
using SIL.FieldWorks.FwCoreDlgs;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// RecordBrowseView is a table oriented view of the collection
	/// </summary>
	public class RecordBrowseView : RecordView, ISnapSplitPosition, IPostLayoutInit, IFocusablePanePortion,
		IBulkEditBarHost
	{
		public event CheckBoxChangedEventHandler CheckBoxChanged;

		#region Data members
		/// <summary>
		///
		/// </summary>
		protected BrowseViewer m_browseViewer;
		// Stage 3 product wiring: when UIMode=New for the lexiconBrowse tool, an Avalonia table is shown
		// on top of the (still fully-functional) legacy BrowseViewer as a read-only mirror; selecting a
		// row forwards to the clerk. Null and inert in the default (Legacy) path.
		private LexicalBrowseHostControl m_avaloniaBrowseHost;
		private ClerkBrowseRowSource m_avaloniaRowSource;
		// The column/cell/sort/filter seam the owned table sources from. In F1 this IS the live
		// BrowseViewer; held as the interface so the F2 viewer-free provider drops in with no logic change
		// (BuildColumnDefinition/the row source already read only IBrowseColumnSource members).
		private IBrowseColumnSource m_avaloniaColumnSource;
		// Configure-Columns (Avalonia P1): the per-tool persisted column choices (show/hide/reorder + width)
		// and the LCModel-free owned model built from the column catalog + the store. Built lazily from
		// Cache.ProjectId, mirroring RecordEditView.ViewOverrideStore.
		private BrowseColumnConfigStore m_browseColumnStore;
		private BrowseColumnModel m_browseColumnModel;
		// Reentrancy guard for the two-way selection bridge (Avalonia row <-> clerk current record).
		private bool m_syncingAvaloniaSelection;
		private bool m_suppressRecordNavigation;
		protected bool m_suppressShowRecord;
		private bool m_fHandlingFilterChangedByClerk;

		/// <summary>
		/// Required designer variable.
		/// </summary>
#pragma warning disable CS0649 // Field is never assigned to
		private readonly System.ComponentModel.Container components;
#pragma warning restore CS0649
		#endregion // Data members

		#region Construction and disposal

		public RecordBrowseView()
		{
			InitializeComponent();
			AccNameDefault = "RecordBrowseView";	// default accessibility name
			Name = "RecordBrowseView";
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				Subscriber.Unsubscribe(EventConstants.ClerkOwningObjChanged, ClerkOwningObjChanged);
				Subscriber.Unsubscribe(EventConstants.ConsideringClosing, ConsideringClosing);
				Subscriber.Unsubscribe(EventConstants.RestoreScrollPosition, OnClerkListReloaded);

				if (ExistingClerk != null) // ExistingClerk, *not* Clerk (see doc on ExistingClerk)
				{
					PersistSortSequence();
					ExistingClerk.FilterChangedByClerk -= Clerk_FilterChangedByClerk;
					ExistingClerk.SorterChangedByClerk -= Clerk_SorterChangedByClerk;
				}
				if (m_browseViewer != null)
				{
					m_browseViewer.SelectionChanged -= OnSelectionChanged;
					m_browseViewer.SelectedIndexChanged -= m_browseViewer_SelectedIndexChanged;
					m_browseViewer.FilterChanged -= FilterChangedHandler;
					m_browseViewer.SorterChanged -= SortChangedHandler;
					if (m_browseViewer.BulkEditBar != null)
						m_browseViewer.BulkEditBar.TargetComboSelectedIndexChanged -= TargetColumnChanged;
					m_browseViewer.BrowseView.RightMouseClickedEvent -= OnFwRightMouseClick;
					m_browseViewer.ListModificationInProgressChanged -= m_browseViewer_ListModificationInProgressChanged;
					m_browseViewer.SelectionDrawingFailure -= OnBrowseSelectionDrawingFailed;
					m_browseViewer.CheckBoxChanged -= OnCheckBoxChanged;
					m_browseViewer.SortersCompatible -= Clerk.AreSortersCompatible;
				}
				if (m_avaloniaEditSda != null && m_avaloniaEditNotifier != null)
					m_avaloniaEditSda.RemoveNotification(m_avaloniaEditNotifier);
				if (m_avaloniaBrowseHost != null)
				{
					m_avaloniaBrowseHost.RowSelected -= OnAvaloniaRowSelected;
					m_avaloniaBrowseHost.ConfigureColumnsRequested -= OnConfigureColumnsRequested;
					m_avaloniaBrowseHost.FilterForRequested -= OnFilterForRequested;
					m_avaloniaBrowseHost.RestrictDateRequested -= OnRestrictDateRequested;
					m_avaloniaBrowseHost.ChooseListRequested -= OnChooseListRequested;
					m_avaloniaBrowseHost.ColumnWidthChanged -= OnAvaloniaColumnWidthChanged;
					m_avaloniaBrowseHost.ColumnReordered -= OnAvaloniaColumnReordered;
					m_avaloniaBrowseHost.RowCommandInvoked -= OnAvaloniaRowCommandInvoked;
					m_avaloniaBrowseHost.Dispose();
				}
				if (components != null)
					components.Dispose();
			}
			m_avaloniaBrowseHost = null;
			m_avaloniaRowSource = null;
			m_avaloniaColumnSource = null;
			m_browseColumnStore = null;
			m_browseColumnModel = null;
			m_browseViewer = null;
			// m_mediator = null; // No, or the superclass call will crash.

			base.Dispose(disposing);
		}

		#endregion // Construction and disposal

		#region Message Handlers

		/// <summary>
		/// Signal the clerk to change its filter to the user selected value.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void FilterChangedHandler(object sender, FilterChangeEventArgs args)
		{
			// If we're in the process of notifying clients that the clerk has changed
			// the filter, we don't need to tell the clerk to change the filter (again)!
			if (m_fHandlingFilterChangedByClerk)
				return;
			Clerk.OnChangeFilter(args);
		}

		/// <summary>
		/// Notify clients that the clerk has changed the filter.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Clerk_FilterChangedByClerk(object sender, FilterChangeEventArgs e)
		{
			m_fHandlingFilterChangedByClerk = true;
			// Let the client(s) know about the change.
			m_browseViewer.UpdateFilterBar(Clerk.Filter);
			m_fHandlingFilterChangedByClerk = false;
		}

		private void Clerk_SorterChangedByClerk(object sender, EventArgs e)
		{
			m_browseViewer.InitSorter(Clerk.Sorter, true);
		}

		private void SortChangedHandler(object sender, EventArgs args)
		{
			using (new WaitCursor(this))
			{
				HandleSortChange();
			}
		}

		private void HandleSortChange()
		{
			bool isDefaultSort = false;
			string colName = null;
			List<int> sortedCols = m_browseViewer.SortedColumns;
			// if the default column is NOT being sorted, we tell the clerk to display what the items are being sorted by
			if (sortedCols.Count > 0)
			{
				colName = m_browseViewer.GetColumnName(sortedCols[0]);
				isDefaultSort = sortedCols[0] == m_browseViewer.DefaultSortColumn;
			}
			Clerk.OnSorterChanged(m_browseViewer.Sorter, colName, isDefaultSort);
		}

		private void TargetColumnChanged(object sender, TargetColumnChangedEventArgs e)
		{
			if (e.ExpectedListItemsClass == 0)
				return;	// no target column selected, so it shouldn't matter what the class of the clerk's list items are.
			using (new WaitCursor(this))
			{
				// we're changing the class of our list items.
				// use ListUpdateHelper to suspend reloading the list until we've changed the class
				// and recomputed the columns. Otherwise, we'll try to reload the list and redraw the display
				// with columns that may not have all their parts in place (e.g. for generated custom fields)
				using (new RecordClerk.ListUpdateHelper(ExistingClerk))
				{
					// change the list items class, but don't do the reload && refresh display
					// until after we've recomputed our columns to allow regenerating custom field parts
					// for that new class.
					ExistingClerk.OnChangeListItemsClass(e.ExpectedListItemsClass, e.TargetFlid, e.ForceReload);
					CheckExpectedListItemsClassInSync();
					// Recompute the possible columns, so the layout/parts system
					// can generate parts for custom fields based upon a new root object class.
					// this should not actually change the column specs themselves since
					// they refer to those parts through "layout" references.
					m_browseViewer.BrowseView.Vc.ComputePossibleColumns();
				}
			}
		}

		private void ClerkOwningObjChanged(object sender)
		{
			CheckDisposed();

			if (Clerk != sender || (m_browseViewer==null))
				return;

			if (Clerk.OwningObject == null)
			{
				//this happens, for example, when they user sets a filter on the
				//list we are dependent on, but no records are selected by the filter.
				//thus, we now do not have an object to get records out of,
				//so we need to just show a blank list.
				m_browseViewer.RootObjectHvo = -1;
			}
			else
			{
				m_browseViewer.RootObjectHvo = Clerk.OwningObject.Hvo;
				SetInfoBarText();
			}
		}

		/// <summary>
		/// This gets triggered when something goes wrong during drawing or hiding the selection
		/// in the browse view. Usually this is because of incomplete refresh, when some crucial object
		/// got deleted. Reconstructing the list often fixes things.
		/// </summary>
		public void OnBrowseSelectionDrawingFailed(object sender, EventArgs args)
		{
			CheckDisposed();

			Clerk.OnRefresh(null);
		}

		public void OnFwRightMouseClick(SimpleRootSite sender, FwRightMouseClickEventArgs e)
		{
			CheckDisposed();

			var browseView = sender as XmlBrowseView;
			if (browseView != null)
			{
				IVwSelection sel = e.Selection;
				int clev = sel.CLevels(false); // anchor
				int hvoRoot, tag, ihvo, cpropPrevious;
				IVwPropertyStore vps;
				sel.PropInfo(false, clev - 1, out hvoRoot, out tag, out ihvo, out cpropPrevious, out vps);
				// First make the selection so it will be highlighted before the context menu popup.
				if (browseView.SelectedIndex != ihvo) // No sense in waking up the beast for no reason.
					browseView.SelectedIndex = ihvo;
				int hvo = browseView.HvoAt(ihvo);
				if (Cache.ServiceLocator.IsValidObjectId(hvo)) // may be fake one from decorator.
				{
					CmObjectUi ui = CmObjectUi.MakeUi(Cache, hvo); // Disposes of itself when menu closes since true passed in lext line.
					if (ui != null)
					{
						ui.Mediator = m_mediator;
						ui.PropTable = m_propertyTable;
						e.EventHandled = ui.HandleRightClick(m_mediator, m_propertyTable, sender, true, "mnuBrowseView");
					}
				}
			}
		}

		#endregion // Message Handlers

		#region Other methods

		protected override void ReadParameters()
		{
			base.ReadParameters();
			// TODO: Deal with Browse XML file?
			// They are in the XmlNode: m_configurationParameters.
		}

		protected override void SetupDataContext()
		{
			base.SetupDataContext();
			// Make sure our persisted sorter/filters are up to date so browse viewer
			// has the latest set for configuring/activating the filterBar
			bool fNeedReload = Clerk.UpdateFiltersAndSortersIfNeeded(true);
			// This is mainly to handle the possibility that one of our objects in a virtual
			// property has been deleted, either by some other tool, or by another client altogether.
			// Enhance: it would be very nice not to do this any time we can be sure it isn't needed.

			// The second 'true' here is to make it skip the sort.  The sort has to be skipped at this
			// point because our VC has been disposed, and we haven't made a new one yet.  The sort
			// will happen later in the sequence of Init when InitSorter is called

			int hvo = 0;
			if (Clerk.OwningObject != null)
				hvo = Clerk.OwningObject.Hvo;
			// We must update the list if needed BEFORE we create the actual view, otherwise, if it is trying
			// to display an out-of-date list containing deleted objects, all kinds of things may go wrong.
			if (XmlUtils.GetOptionalBooleanAttributeValue(m_configurationParameters, "forceReloadListOnInitOrChangeRoot", false))
			{
				m_propertyTable.SetProperty(Clerk.Id + "_AlwaysRecomputeVirtualOnReloadList", true, true);
				// (EricP) when called by RecordView.InitBase() in the context of ListUpdateHelper.ClearBrowseListUntilReload
				// the list does not get reloaded until ListUpdateHelper is disposed, but the views property
				// will get cleared to prevent these views from accessing invalid objects.
				Clerk.UpdateList(false, true);
			}

			m_browseViewer = CreateBrowseViewer(m_configurationParameters, hvo, m_fakeFlid, Cache,
				m_mediator, m_propertyTable,
				Clerk.SortItemProvider, Clerk.VirtualListPublisher);
			m_browseViewer.SortersCompatible += Clerk.AreSortersCompatible;
			// If possible make it use the style sheet appropriate for its main window.
			m_browseViewer.SuspendLayout();
			SetStyleSheet();
			m_browseViewer.Dock = DockStyle.Fill;
			RecordFilter linkFilter = m_browseViewer.FilterFromLink();
			SetupLinkScripture();
			if (linkFilter != null)
				Clerk.OnChangeFilter(new FilterChangeEventArgs(linkFilter, Clerk.Filter));
			if (Clerk.Filter != null && !Clerk.Filter.IsValid)
				Clerk.ResetFilterToDefault();
			m_browseViewer.UpdateFilterBar(Clerk.Filter);
			bool fSortChanged = m_browseViewer.InitSorter(Clerk.Sorter); // true if we had to change sorter
			// Do this AFTER we init the sorter and filter, so if any changes are made to the
			// sorter or filter as we install, we still get the right load.
			if (fSortChanged)
			{
				HandleSortChange();
				// Keep the current index -- see LT-8755.
			}
			else
			{
				List<int> sortedCols = m_browseViewer.SortedColumns;
				Clerk.IsDefaultSort = sortedCols.Count > 0 && sortedCols[0] == m_browseViewer.DefaultSortColumn;
				// This won't actually load if in the context of UpdateListHelper()
				Clerk.UpdateList(true, fNeedReload);
			}
			// Do this very late, it can't display properly until its record list has been built and sorted.
			Controls.Add(m_browseViewer);
			m_browseViewer.BringToFront();
			m_browseViewer.ResumeLayout();

			TryActivateAvaloniaBrowse();
		}

		/// <summary>
		/// Stage 3 product wiring: when the lexiconBrowse tool is in `UIMode = New`, overlay the owned
		/// Avalonia table (read-only mirror) on top of the legacy <see cref="BrowseViewer"/>. The legacy
		/// viewer stays constructed and fully functional underneath (it keeps driving the clerk, sort,
		/// filter, and the rest of FLEx); the Avalonia table forwards row selection to the clerk so
		/// navigation works. In the default `Legacy` mode this is a no-op and nothing changes.
		/// </summary>
		private void TryActivateAvaloniaBrowse()
		{
			if (m_browseViewer == null || m_avaloniaBrowseHost != null)
				return;

			var toolName = m_propertyTable.GetStringProperty("currentContentControl", string.Empty);
			var uiMode = m_propertyTable.GetStringProperty(
				LexicalEditSurfaceResolver.UIModePropertyName, LexicalEditSurfaceResolver.LegacyUIMode);
			if (LexicalEditSurfaceResolver.ResolveBrowse(null, uiMode, toolName) != LexicalEditSurface.Avalonia)
				return;

			m_avaloniaColumnSource = m_browseViewer; // F1: the live viewer is the column source (F2 swaps a viewer-free provider here)
			// Configure-Columns: align the live viewer's shown columns to the per-tool persisted choices BEFORE
			// the first definition is built, so a returning user sees their saved show/hide/reorder. Builds the
			// owned model from the catalog + store and (when a saved set exists) installs it on the viewer.
			BuildBrowseColumnModel();
			var definition = BuildColumnDefinition();
			m_avaloniaRowSource = new ClerkBrowseRowSource(Clerk, m_avaloniaColumnSource, Cache);
			m_avaloniaBrowseHost = new LexicalBrowseHostControl { Dock = DockStyle.Fill };
			m_avaloniaBrowseHost.RowSelected += OnAvaloniaRowSelected;
			m_avaloniaBrowseHost.ConfigureColumnsRequested += OnConfigureColumnsRequested;
			m_avaloniaBrowseHost.FilterForRequested += OnFilterForRequested;
			m_avaloniaBrowseHost.RestrictDateRequested += OnRestrictDateRequested;
			m_avaloniaBrowseHost.ChooseListRequested += OnChooseListRequested;
			m_avaloniaBrowseHost.ColumnWidthChanged += OnAvaloniaColumnWidthChanged;
			m_avaloniaBrowseHost.ColumnReordered += OnAvaloniaColumnReordered;
			m_avaloniaBrowseHost.RowCommandInvoked += OnAvaloniaRowCommandInvoked;
			Controls.Add(m_avaloniaBrowseHost);
			m_avaloniaBrowseHost.BringToFront(); // paint over the legacy viewer (which stays functional)
			// Legacy parity: the BrowseViewer shows a per-row select column + select-all; enable the same on
			// the Avalonia surface so the product browse offers selection. The user's checked objects are
			// readable via m_avaloniaBrowseHost.CheckedRows (hvos) — the prerequisite for product bulk-edit.
			// Passing THIS as the bulk-edit host docks the Phase-1 List Choice bar under the table; the bar
			// stays LCModel-free and drives preview/apply back through this edge over the checked rows.
			m_avaloniaBrowseHost.ShowBrowse(definition, m_avaloniaRowSource, showCheckboxColumn: true,
				bulkEditHost: this, columnWidths: BuildColumnWidthMap());
			RegisterAvaloniaEditNotification();
			// Subscribe to the clerk's post-reload publish HERE — during SetupDataContext, i.e. BEFORE the
			// deferred initial list load completes (RecordView's ListUpdateHelper loads on dispose at the end
			// of InitBase). Subscribing now means the INITIAL DoneReload is caught and the mirror refreshes to
			// the full row count; subscribing later (in Init) missed it and could leave only a subset showing.
			Subscriber.Subscribe(EventConstants.RestoreScrollPosition, OnClerkListReloaded, m_propertyTable.GetWindow());
		}

		/// <summary>
		/// Mediator broadcast handler (xCore reflection-invoked). When the UI mode flips between Legacy and
		/// New, switch the table to match — symmetrically with <see cref="RecordEditView"/>, so the left
		/// Entries pane and the detail pane never end up half-switched (one on Avalonia, one on legacy).
		/// New→legacy tears the overlay down and reveals the still-live legacy BrowseViewer; legacy→New
		/// re-activates the overlay and re-syncs the current record.
		/// </summary>
		public void OnPropertyChanged(string name)
		{
			CheckDisposed();
			if (name != LexicalEditSurfaceResolver.UIModePropertyName || m_browseViewer == null)
				return;

			var toolName = m_propertyTable.GetStringProperty("currentContentControl", string.Empty);
			var uiMode = m_propertyTable.GetStringProperty(
				LexicalEditSurfaceResolver.UIModePropertyName, LexicalEditSurfaceResolver.LegacyUIMode);
			var wantAvalonia = LexicalEditSurfaceResolver.ResolveBrowse(null, uiMode, toolName) == LexicalEditSurface.Avalonia;

			if (wantAvalonia && m_avaloniaBrowseHost == null)
			{
				TryActivateAvaloniaBrowse();
				MirrorClerkSelectionToAvalonia();
			}
			else if (!wantAvalonia && m_avaloniaBrowseHost != null)
			{
				DeactivateAvaloniaBrowse();
			}
		}

		// Tears the Avalonia overlay down (live legacy↔New switch). The legacy BrowseViewer was never
		// disposed — it stays docked underneath — so revealing it is just removing the overlay and
		// bringing it forward. Mirrors the disposal sequence in Dispose so a later Dispose is a no-op.
		private void DeactivateAvaloniaBrowse()
		{
			// Balance the subscription set up in TryActivateAvaloniaBrowse, so a later re-activate does not
			// double-subscribe (which would refresh the mirror twice per reload).
			Subscriber.Unsubscribe(EventConstants.RestoreScrollPosition, OnClerkListReloaded);
			if (m_avaloniaEditSda != null && m_avaloniaEditNotifier != null)
				m_avaloniaEditSda.RemoveNotification(m_avaloniaEditNotifier);
			m_avaloniaEditNotifier = null;
			m_avaloniaEditSda = null;
			if (m_avaloniaBrowseHost != null)
			{
				m_avaloniaBrowseHost.RowSelected -= OnAvaloniaRowSelected;
				m_avaloniaBrowseHost.ConfigureColumnsRequested -= OnConfigureColumnsRequested;
				m_avaloniaBrowseHost.FilterForRequested -= OnFilterForRequested;
				m_avaloniaBrowseHost.RestrictDateRequested -= OnRestrictDateRequested;
				m_avaloniaBrowseHost.ChooseListRequested -= OnChooseListRequested;
				m_avaloniaBrowseHost.ColumnWidthChanged -= OnAvaloniaColumnWidthChanged;
				m_avaloniaBrowseHost.ColumnReordered -= OnAvaloniaColumnReordered;
				m_avaloniaBrowseHost.RowCommandInvoked -= OnAvaloniaRowCommandInvoked;
				Controls.Remove(m_avaloniaBrowseHost);
				m_avaloniaBrowseHost.Dispose();
				m_avaloniaBrowseHost = null;
			}
			m_avaloniaRowSource = null;
			m_avaloniaColumnSource = null;
			m_browseColumnModel = null;
			m_browseViewer?.BringToFront();
		}

		private ISilDataAccess m_avaloniaEditSda;
		private IVwNotifyChange m_avaloniaEditNotifier;
		private bool m_avaloniaRefreshPending;

		// Refresh the table when a model change touches the current row's object (or one it owns),
		// coalescing a unit-of-work's many PropChanged into one refresh. The legacy browse does this via
		// its RootSite PropChanged sink; the Avalonia mirror has none, so an edit in the detail pane that
		// stays on the same record would otherwise leave the table stale.
		private void RegisterAvaloniaEditNotification()
		{
			if (m_avaloniaEditNotifier != null)
				return;
			m_avaloniaEditSda = Cache.DomainDataByFlid;
			m_avaloniaEditNotifier = new AvaloniaBrowseEditNotifier(this);
			m_avaloniaEditSda.AddNotification(m_avaloniaEditNotifier);
		}

		private void OnAvaloniaModelChanged(int hvo)
		{
			if (m_avaloniaBrowseHost == null || m_avaloniaRefreshPending || !IsHandleCreated || !AffectsCurrentRow(hvo))
				return;
			m_avaloniaRefreshPending = true;
			BeginInvoke((Action)(() =>
			{
				m_avaloniaRefreshPending = false;
				if (m_avaloniaBrowseHost == null || IsDisposed)
					return;
				var prev = m_syncingAvaloniaSelection;
				m_syncingAvaloniaSelection = true;
				try
				{
					m_avaloniaBrowseHost.RefreshRows();
					if (Clerk.CurrentIndex >= 0)
						m_avaloniaBrowseHost.SelectRow(Clerk.CurrentIndex);
				}
				finally { m_syncingAvaloniaSelection = prev; }
			}));
		}

		// Thin test seam (Task 23c): the owner-walk that decides whether a model change touches the current
		// row's object (or one it owns) is the gate for the Avalonia mirror's refresh; this lets a
		// real-clerk/real-cache test assert it without driving a PropChanged through the notifier.
		internal bool AffectsCurrentRowForTest(int hvo) => AffectsCurrentRow(hvo);

		private bool AffectsCurrentRow(int hvo)
		{
			var current = Clerk?.CurrentObject;
			if (current == null || hvo == 0)
				return false;
			if (hvo == current.Hvo)
				return true;
			if (!Cache.ServiceLocator.IsValidObjectId(hvo))
				return false;
			for (var obj = Cache.ServiceLocator.GetObject(hvo); obj != null; obj = obj.Owner)
				if (obj.Hvo == current.Hvo)
					return true;
			return false;
		}

		private sealed class AvaloniaBrowseEditNotifier : IVwNotifyChange
		{
			private readonly RecordBrowseView _owner;
			public AvaloniaBrowseEditNotifier(RecordBrowseView owner) { _owner = owner; }
			public void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
				=> _owner.OnAvaloniaModelChanged(hvo);
		}

		private ViewDefinitionModel BuildColumnDefinition()
		{
			// Own the column model: snapshot each column's real metadata (label, stable field token from
			// field/transduce, writing system, editability) into a managed BrowseColumnSpec, then project
			// to the typed view definition — instead of fabricating "col{i}" tokens. The snapshot is the
			// seam that decouples the table's columns from the live viewer (F2 sources it without one).
			// After Configure-Columns, the live viewer's ColumnSpecs ARE the shown set/order (we installed
			// them via InstallColumnsByKey), so the snapshot already honors the model's shown set + order.
			return BrowseColumnSpec.ToViewDefinition(BrowseColumnSpec.Snapshot(m_avaloniaColumnSource));
		}

		// The configure-columns tool key the per-tool store persists under (the active tool name).
		private string BrowseColumnToolName()
			=> m_propertyTable.GetStringProperty("currentContentControl", string.Empty);

		// advanced-entry-view: the per-tool browse-column config file lives in this project's
		// ConfigurationSettings folder (mirrors RecordEditView.ViewOverrideStore). Built lazily and reused.
		private BrowseColumnConfigStore BrowseColumnStore
		{
			get
			{
				if (m_browseColumnStore == null && Cache?.ProjectId?.ProjectFolder != null)
				{
					m_browseColumnStore = new BrowseColumnConfigStore(
						LcmFileHelper.GetConfigSettingsDir(Cache.ProjectId.ProjectFolder));
				}
				return m_browseColumnStore;
			}
		}

		// Builds the LCModel-free owned column model from the live viewer's catalog + the per-tool persisted
		// choices, and — when a saved configuration exists — installs that shown set/order on the live viewer
		// so the first Avalonia definition (and the legacy viewer underneath) both reflect the user's choices.
		private void BuildBrowseColumnModel()
		{
			var available = new List<BrowseColumnChoice>();
			foreach (var info in m_avaloniaColumnSource.GetAvailableColumns())
				available.Add(new BrowseColumnChoice(info.Key, info.Label, info.HasWritingSystemOption));

			var saved = BrowseColumnStore?.TryGet(BrowseColumnToolName(),
				(path, error) => Logger.WriteError("Failed to load browse columns '" + path
					+ "'; using the shipped default columns.", error));

			IReadOnlyList<BrowseColumnEntry> shown;
			if (saved != null && saved.Count > 0)
			{
				shown = saved.Select(c => new BrowseColumnEntry(c.Key, c.Width)).ToList();
				// Re-align the live viewer to the saved keys so the snapshot below honors the saved set/order.
				m_browseViewer.InstallColumnsByKey(saved.Select(c => c.Key).ToList());
			}
			else
			{
				// No saved config: the shipped default is whatever the viewer currently shows.
				var defaults = new List<BrowseColumnEntry>();
				for (var i = 0; i < m_avaloniaColumnSource.ColumnCount; i++)
					defaults.Add(new BrowseColumnEntry(m_avaloniaColumnSource.GetColumnKey(i)));
				shown = defaults;
			}
			m_browseColumnModel = new BrowseColumnModel(available, shown);
		}

		// Projects the model's per-column widths into a field-token→width map the Avalonia view seeds its
		// column widths from. The view keys widths by the column's Field (ViewNode.Field == BrowseColumnSpec
		// StableField), so map the model's catalog KEY-keyed width to the shown column's StableField.
		private IReadOnlyDictionary<string, double> BuildColumnWidthMap()
		{
			var map = new Dictionary<string, double>();
			if (m_browseColumnModel == null)
				return map;
			var specs = BrowseColumnSpec.Snapshot(m_avaloniaColumnSource);
			for (var i = 0; i < specs.Count && i < m_avaloniaColumnSource.ColumnCount; i++)
			{
				var key = m_avaloniaColumnSource.GetColumnKey(i);
				var width = m_browseColumnModel.WidthOf(key);
				if (width.HasValue && width.Value > 0)
					map[specs[i].StableField] = width.Value;
			}
			return map;
		}

		// Header context-menu "Configure Columns…" (P1 step 7): launch the LCModel-free dialog over the owned
		// model's catalog + shown keys; on OK persist, re-align the live viewer, reset stale column sort/filter,
		// and rebuild the Avalonia surface preserving selection + checked set (P1 steps 4 & 6).
		private void OnConfigureColumnsRequested(object sender, EventArgs e)
		{
			if (m_avaloniaBrowseHost == null || m_browseColumnModel == null)
				return;

			var available = m_browseColumnModel.Available
				.Select(c => new FwAvaloniaDialogs.ColumnChoiceItem(c.Key, c.Label))
				.ToList();
			var vm = new FwAvaloniaDialogs.ConfigureColumnsDialogViewModel(available, m_browseColumnModel.ShownKeys);
			var view = new FwAvaloniaDialogs.ConfigureColumnsDialogView { DataContext = vm };
			var accepted = AvaloniaDialogHost.ShowModal(FindForm(), view, vm,
				FwAvaloniaDialogs.FwAvaloniaDialogsStrings.ConfigureColumnsTitle);
			if (accepted != true || vm.ResultKeys == null)
				return;

			ApplyConfiguredColumns(vm.ResultKeys);
		}

		// Filter flyout "Filter For…" (browse column-filter parity): launch the LCModel-free pattern-setup
		// dialog (the Avalonia counterpart of the legacy FilterBar SimpleMatchDlg) over the owning form; on OK
		// translate the dialog result into the FwAvalonia-layer spec and route it back to the table, which
		// drives the clerk's pattern filter through the row source.
		private void OnFilterForRequested(object sender, int columnIndex)
		{
			if (m_avaloniaBrowseHost == null)
				return;

			var vm = new FwAvaloniaDialogs.FilterForDialogViewModel();
			var view = new FwAvaloniaDialogs.FilterForDialogView { DataContext = vm };
			var accepted = AvaloniaDialogHost.ShowModal(FindForm(), view, vm,
				FwAvaloniaDialogs.FwAvaloniaDialogsStrings.FilterForTitle);
			if (accepted != true || vm.Result == null)
				return;

			m_avaloniaBrowseHost.ApplyFilterPattern(columnIndex, ToFilterForSpec(vm.Result));
		}

		// The browse host/row-source speak the FwAvalonia-layer BrowseFilterForSpec (which cannot reference the
		// dialog kit); the dialog speaks FilterForPattern. This translator is the single seam between them, kept
		// here at the product edge where both layers are visible (mirrors ToPattern/FromPattern for replace).
		private static SIL.FieldWorks.Common.FwAvalonia.Region.BrowseFilterForSpec ToFilterForSpec(
			FwAvaloniaDialogs.FilterForPattern pattern)
		{
			return new SIL.FieldWorks.Common.FwAvalonia.Region.BrowseFilterForSpec
			{
				MatchText = pattern.MatchText ?? string.Empty,
				MatchType = ToBrowseMatch(pattern.MatchType),
				MatchCase = pattern.MatchCase
			};
		}

		private static SIL.FieldWorks.Common.FwAvalonia.Region.BrowsePatternMatch ToBrowseMatch(
			FwAvaloniaDialogs.FilterForMatchType type)
		{
			switch (type)
			{
				case FwAvaloniaDialogs.FilterForMatchType.AtStart:
					return SIL.FieldWorks.Common.FwAvalonia.Region.BrowsePatternMatch.AtStart;
				case FwAvaloniaDialogs.FilterForMatchType.AtEnd:
					return SIL.FieldWorks.Common.FwAvalonia.Region.BrowsePatternMatch.AtEnd;
				case FwAvaloniaDialogs.FilterForMatchType.WholeItem:
					return SIL.FieldWorks.Common.FwAvalonia.Region.BrowsePatternMatch.WholeItem;
				case FwAvaloniaDialogs.FilterForMatchType.Regex:
					return SIL.FieldWorks.Common.FwAvalonia.Region.BrowsePatternMatch.Regex;
				default:
					return SIL.FieldWorks.Common.FwAvalonia.Region.BrowsePatternMatch.Anywhere;
			}
		}

		// Filter flyout "Restrict Date…" (date/genDate column-filter parity): launch the LCModel-free date-range
		// dialog (the Avalonia counterpart of the legacy FilterBar SimpleDateMatchDlg) over the owning form; on OK
		// translate the dialog result into the FwAvalonia-layer spec and route it back to the table, which drives
		// the clerk's DateTimeMatcher through the row source. The genDate-vs-date distinction is read off the
		// column spec's sortType through the row-source metadata seam.
		private void OnRestrictDateRequested(object sender, int columnIndex)
		{
			if (m_avaloniaBrowseHost == null || m_avaloniaRowSource == null)
				return;

			var sortType = m_avaloniaRowSource.GetColumnSpecAttribute(columnIndex, "sortType");
			var handleGenDate = string.Equals(sortType, "genDate", System.StringComparison.OrdinalIgnoreCase);

			var vm = new FwAvaloniaDialogs.DateRangeFilterDialogViewModel(null, handleGenDate);
			var view = new FwAvaloniaDialogs.DateRangeFilterDialogView { DataContext = vm };
			var accepted = AvaloniaDialogHost.ShowModal(FindForm(), view, vm,
				FwAvaloniaDialogs.FwAvaloniaDialogsStrings.RestrictDateTitle);
			if (accepted != true || vm.Result == null)
				return;

			m_avaloniaBrowseHost.ApplyFilterDate(columnIndex, ToDateFilterSpec(vm.Result));
		}

		// The browse host/row-source speak the FwAvalonia-layer BrowseDateFilterSpec; the dialog speaks
		// DateRangeFilterPattern. This translator is the single seam between them (mirrors ToFilterForSpec).
		private static SIL.FieldWorks.Common.FwAvalonia.Region.BrowseDateFilterSpec ToDateFilterSpec(
			FwAvaloniaDialogs.DateRangeFilterPattern pattern)
		{
			return new SIL.FieldWorks.Common.FwAvalonia.Region.BrowseDateFilterSpec
			{
				MatchType = ToBrowseDateMatch(pattern.MatchType),
				Start = pattern.Start,
				End = pattern.End,
				HandleGenDate = pattern.HandleGenDate
			};
		}

		private static SIL.FieldWorks.Common.FwAvalonia.Region.BrowseDateMatch ToBrowseDateMatch(
			FwAvaloniaDialogs.DateRangeMatchType type)
		{
			switch (type)
			{
				case FwAvaloniaDialogs.DateRangeMatchType.NotOn:
					return SIL.FieldWorks.Common.FwAvalonia.Region.BrowseDateMatch.NotOn;
				case FwAvaloniaDialogs.DateRangeMatchType.OnOrBefore:
					return SIL.FieldWorks.Common.FwAvalonia.Region.BrowseDateMatch.OnOrBefore;
				case FwAvaloniaDialogs.DateRangeMatchType.OnOrAfter:
					return SIL.FieldWorks.Common.FwAvalonia.Region.BrowseDateMatch.OnOrAfter;
				case FwAvaloniaDialogs.DateRangeMatchType.Between:
					return SIL.FieldWorks.Common.FwAvalonia.Region.BrowseDateMatch.Between;
				default:
					return SIL.FieldWorks.Common.FwAvalonia.Region.BrowseDateMatch.On;
			}
		}

		// Filter flyout "Choose…" (chooser column-filter parity): build the chooser items from the column's
		// possibility list (through the row-source metadata seam, LCModel-free RegionChoiceOptions), launch the
		// SHARED Avalonia ChooserDialog (multi-select, flat or hierarchical when the items carry depth), and on OK
		// route the chosen possibility keys back to the table, which drives the clerk's ListChoiceFilter through
		// the row source. Reuses the existing ChooserDialog — does not duplicate it.
		private void OnChooseListRequested(object sender, int columnIndex)
		{
			if (m_avaloniaBrowseHost == null || m_avaloniaRowSource == null)
				return;

			var options = m_avaloniaRowSource.GetColumnChooserList(columnIndex);
			if (options == null || options.Count == 0)
				return;

			var hierarchical = false;
			foreach (var o in options)
				if (o.Depth > 0) { hierarchical = true; break; }

			var input = new FwAvaloniaDialogs.ChooserDialogInput
			{
				Candidates = options,
				SelectionMode = FwAvaloniaDialogs.ChooserSelectionMode.Multi,
				Hierarchical = hierarchical,
				ForbidEmptySelection = true,
				Prompt = FwAvaloniaDialogs.FwAvaloniaDialogsStrings.FilterChoosePrompt
			};
			var vm = new FwAvaloniaDialogs.ChooserDialogViewModel(input);
			var view = new FwAvaloniaDialogs.ChooserDialogView { DataContext = vm };
			var accepted = AvaloniaDialogHost.ShowModal(FindForm(), view, vm,
				FwAvaloniaDialogs.FwAvaloniaDialogsStrings.FilterChooseTitle);
			if (accepted != true || vm.ChosenKeys == null || vm.ChosenKeys.Count == 0)
				return;

			m_avaloniaBrowseHost.ApplyFilterListChoice(columnIndex, vm.ChosenKeys);
		}

		// Applies a new shown column set/order from the dialog (also reused by tests): install on the live
		// viewer by key, rebuild the owned model preserving widths, persist, clear stale column sort/filter so
		// a reorder never misapplies, and rebuild the Avalonia surface preserving selection + checked set.
		internal void ApplyConfiguredColumns(IReadOnlyList<string> orderedKeys)
		{
			if (orderedKeys == null || orderedKeys.Count == 0 || m_browseViewer == null)
				return;

			// 1) Re-align the live legacy viewer underneath (width preservation + sorter re-init) and learn the
			//    keys that actually survived (a key with no matching column is dropped).
			var installed = m_browseViewer.InstallColumnsByKey(orderedKeys);
			if (installed.Count == 0)
				return;

			// 2) Rebuild the owned model from the catalog + the new shown set, carrying any known widths forward.
			var available = m_browseColumnModel.Available;
			var newShown = installed.Select(k => new BrowseColumnEntry(k, m_browseColumnModel.WidthOf(k))).ToList();
			m_browseColumnModel = new BrowseColumnModel(available, newShown);

			// 3) Persist the per-tool choices (empty/default deletes the file).
			PersistBrowseColumns();

			// 4) A reorder/show/hide shifts column indexes, so any column-index-keyed clerk filter/preview at the
			//    row-source seam would now misapply to a DIFFERENT field — clear them (P1 step 6). The clerk
			//    reload that follows republishes and refreshes the table.
			m_avaloniaRowSource?.ResetColumnState();

			// 5) Rebuild the Avalonia surface from the new definition, preserving the row source, selection, and
			//    the object-keyed checked set (P1 step 4).
			m_avaloniaBrowseHost.RebuildColumns(BuildColumnDefinition(), BuildColumnWidthMap());
		}

		// Persists the model's shown columns (key + width) under the active tool; an empty/default set deletes.
		private void PersistBrowseColumns()
		{
			var store = BrowseColumnStore;
			if (store == null || m_browseColumnModel == null)
				return;
			var columns = m_browseColumnModel.Shown
				.Select(e => new BrowseColumnConfigEntry(e.Key, e.Width))
				.ToList();
			store.Save(BrowseColumnToolName(), columns);
		}

		// A finished column-width drag (P1 step 5): record the width on the model (mapping the dragged column's
		// StableField back to its catalog key) and persist it under the active tool.
		private void OnAvaloniaColumnWidthChanged(object sender, BrowseColumnWidthChange e)
		{
			if (m_browseColumnModel == null)
				return;
			var specs = BrowseColumnSpec.Snapshot(m_avaloniaColumnSource);
			for (var i = 0; i < specs.Count && i < m_avaloniaColumnSource.ColumnCount; i++)
			{
				if (specs[i].StableField == e.Field)
				{
					m_browseColumnModel.SetWidth(m_avaloniaColumnSource.GetColumnKey(i), e.Width);
					PersistBrowseColumns();
					return;
				}
			}
		}

		private void OnAvaloniaRowSelected(object sender, int rowIndex)
		{
			if (m_syncingAvaloniaSelection)
				return; // selection originated from the clerk mirror, not the user — don't echo it back
			var hvo = m_avaloniaRowSource?.HvoAt(rowIndex) ?? 0;
			if (hvo != 0)
				Clerk.JumpToRecord(hvo);
		}

		// 19f.6: a header drag-reorder. Move the dragged column key from its old display position to the
		// new one in the current shown-key order, then reuse the SAME ApplyConfiguredColumns path the
		// Configure-Columns dialog uses (re-align live viewer, rebuild + persist the model, clear stale
		// column state, rebuild the surface preserving selection + checked set). The legacy BrowseViewer
		// .m_lvHeader_ColumnDragDropReordered did the same reorder-then-rebuild on Vc.ColumnSpecs.
		private void OnAvaloniaColumnReordered(object sender, (int FromIndex, int ToIndex) e)
		{
			if (m_browseColumnModel == null)
				return;
			var keys = m_browseColumnModel.ShownKeys?.ToList();
			if (keys == null || e.FromIndex < 0 || e.FromIndex >= keys.Count
				|| e.ToIndex < 0 || e.ToIndex >= keys.Count || e.FromIndex == e.ToIndex)
				return;
			var moved = keys[e.FromIndex];
			keys.RemoveAt(e.FromIndex);
			keys.Insert(e.ToIndex, moved);
			ApplyConfiguredColumns(keys);
		}

		// 19f.1: a data-row context-menu command. Route the chosen command key the SAME way the legacy
		// RightMouseClickedEvent host did: jump the clerk to the right-clicked row's object (so the command
		// acts on it), then broadcast the command id through the mediator. A command with no handler is a
		// harmless no-op (matching the legacy update-handler gating).
		private void OnAvaloniaRowCommandInvoked(object sender, (int RowIndex, string CommandKey) e)
		{
			if (string.IsNullOrEmpty(e.CommandKey) || m_avaloniaRowSource == null)
				return;
			var hvo = m_avaloniaRowSource.HvoAt(e.RowIndex);
			if (hvo != 0)
				Clerk.JumpToRecord(hvo);
			// Broadcast via the modern Publisher (Mediator.SendMessage is obsolete); no subscriber = no-op.
				Publisher?.Publish(new PublisherParameterObject(e.CommandKey, null, m_propertyTable.GetWindow()));
		}

		// Revision 2 (architecture-review): mirror the clerk's current record into the Avalonia table so
		// it follows external navigation (links, the edit view changing record). Guarded against the
		// Avalonia->clerk echo above.
		private void MirrorClerkSelectionToAvalonia()
		{
			if (m_avaloniaBrowseHost == null || m_syncingAvaloniaSelection)
				return;
			m_syncingAvaloniaSelection = true;
			try
			{
				m_avaloniaBrowseHost.RefreshRows();
				m_avaloniaBrowseHost.SelectRow(Clerk.CurrentIndex);
			}
			finally
			{
				m_syncingAvaloniaSelection = false;
			}
		}

		/// <summary>
		/// Set up the current 'interesting texts' to include the part of Scripture (currently only 'all' is
		/// supported) specified byt the mediator property "LinkScriptureBooksWanted". Then remeove that property.
		/// </summary>
		void SetupLinkScripture()
		{
			string booksWanted = m_propertyTable.GetStringProperty("LinkScriptureBooksWanted", null);
			if (booksWanted == null)
				return;
			m_propertyTable.RemoveProperty("LinkScriptureBooksWanted");
			if (booksWanted != "all")
				return; // Enhance JohnT: accept a list of books in some form or other.
			var books = Cache.LanguageProject.TranslatedScriptureOA.ScriptureBooksOS;
			List<IStText> texts = new List<IStText>(0);
			foreach (var book in books)
			{
				foreach (var section in book.SectionsOS)
				{
					texts.Add(section.ContentOA);
					texts.Add(section.HeadingOA);
				}
			}

			var interestingTexts = InterestingTextsDecorator.GetInterestingTextList(m_mediator, m_propertyTable, Cache.ServiceLocator);
			interestingTexts.SetInterestingTexts(texts);
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
			return m_browseViewer.FollowLink(args);
		}


		/// <summary>
		/// This is the best way I can find to catch when the control is going away.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnParentChanged(EventArgs e)
		{
			if (Parent == null)
				PersistSortSequence();
			base.OnParentChanged(e);
		}

		protected virtual BrowseViewer CreateBrowseViewer(XmlNode nodeSpec, int hvoRoot, int fakeFlid, LcmCache cache,
			Mediator mediator, PropertyTable propertyTable,
			ISortItemProvider sortItemProvider,ISilDataAccessManaged sda)
		{
			return new BrowseViewer(nodeSpec,
						 hvoRoot, fakeFlid,
						 cache, mediator, propertyTable, sortItemProvider, sda);
		}

		private void SetStyleSheet()
		{
			if (m_browseViewer == null || m_browseViewer.StyleSheet != null)
				return;

			m_browseViewer.StyleSheet = FontHeightAdjuster.StyleSheetFromPropertyTable(m_propertyTable);
		}

		/// <summary>
		/// Sometimes SetupDataContext is called before we're in a form and can get a stylesheet.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnHandleCreated(EventArgs e)
		{
				SetStyleSheet();
			base.OnHandleCreated (e);
		}

		/// <summary>
		/// And sometimes OnHandleCreated() is called before we're in a form, but we can intercept
		/// another event...
		/// </summary>
		/// <param name="e"></param>
		protected override void OnPaintBackground(PaintEventArgs e)
		{
				SetStyleSheet();
			base.OnPaintBackground(e);
		}

		protected override void SetInfoBarText()
		{
			if (m_informationBar == null)
				return;

			string titleStr = "";
			// See if we have an AlternativeTitle string table id for an alternate title.
			string titleId = XmlUtils.GetAttributeValue(m_configurationParameters,
				"altTitleId");
			if (titleId != null)
			{
				XmlViewsUtils.TryFindString("AlternativeTitles", titleId, out titleStr);
				// if they specified an altTitleId, but it wasn't found, they need to do something,
				// so just return *titleId*
				if (Clerk.OwningObject != null && titleId.StartsWith("Reversal") &&
					XmlUtils.GetBooleanAttributeValue(m_configurationParameters, "ShowOwnerShortname"))
				{
					// Originally this option was added to enable Bulk Edit Reversal Entries title bar to show
					// which reversal index was being shown. If the 'titleId.StartsWith("Reversal")' in the 'if'
					// above is removed then the Word List Concordance shows the word being concorded in the
					// right pane title bar.
					titleStr = string.Format(xWorksStrings.ksXReversalIndex, Clerk.OwningObject.ShortName, titleStr);
				}
			}
			else if (Clerk.OwningObject != null)
			{
				if (XmlUtils.GetBooleanAttributeValue(m_configurationParameters,
					"ShowOwnerShortname"))
				{
					titleStr = Clerk.OwningObject.ShortName;
				}
			}
			if (String.IsNullOrEmpty(titleStr))
			{
				XmlViewsUtils.TryFindPluralFormFromFlid(Clerk.VirtualListPublisher.MetaDataCache,
					Clerk.OwningFlid, out titleStr);
			}

			bool fBaseCalled = false;
			if (String.IsNullOrEmpty(titleStr))
			{
				base.SetInfoBarText();
				fBaseCalled = true;
//				titleStr = ((IPaneBar)m_informationBar).Text;	// can't get to work.
				// (EricP) For some reason I can't provide an IPaneBar get-accessor to return
				// the new Text value. If it's desirable to allow TitleFormat to apply to
				// Clerk.CurrentObject, then we either have to duplicate what the
				// base.SetInfoBarText() does here, or get the string set by the base.
				// for now, let's just return.
				if (string.IsNullOrEmpty(titleStr))
					return;
			}

			// If we have a format attribute, format the title accordingly.
			string sFmt = XmlUtils.GetAttributeValue(m_configurationParameters,
				"TitleFormat");
			if (sFmt != null)
			{
				 titleStr = String.Format(sFmt, titleStr);
			}

			// if we haven't already set the text through the base,
			// or if we had some formatting to do, then set the infoBar text.
			if (!fBaseCalled || sFmt != null)
				((IPaneBar)m_informationBar).Text = titleStr;
		}

		protected override void ShowRecord()
		{
			// ShowRecord is called by InitBase,
			// but it isn't set up enough to do anything at that call.
			// Out own Init method needs to call it again.
			// Either that, or we can try initializing the browse viewer, before calling InitBase,
			// but that may be worse.
			if (!m_fullyInitialized || m_suppressShowRecord)
				return;
			Debug.Assert(m_browseViewer != null, "RecordBrowseView.SetupDataContext() has to be called before RecordBrowseView.ShowRecord().");

			RecordClerk clerk = Clerk;

			// This is a bizarre situation that occurs when the root object is changing and
			// notifications get sent in non-optimal order. There will be another
			// ShowRecord call after the two get synchronized.
			if (clerk.OwningObject != null && clerk.OwningObject.Hvo != m_browseViewer.RootObjectHvo)
				return;
			int currentIndex = clerk.CurrentIndex;

			int storedIndex = m_propertyTable.GetIntProperty(Clerk.PersistedIndexProperty, currentIndex, PropertyTable.SettingsGroup.LocalSettings);
			if (storedIndex != currentIndex && storedIndex >= 0 && !clerk.HasEmptyList)
			{
				try
				{
					clerk.JumpToIndex(storedIndex);
					currentIndex = clerk.CurrentIndex;
					Debug.Assert(currentIndex == storedIndex);
				}
				catch
				{
					if (currentIndex >= 0)
						clerk.JumpToIndex(currentIndex);
				}
			}
			// all that the base method currently does is put the class name of the selected object
			// into our information bar. If we're supposed to be showing the short name of the root
			// object, that needs to be suppressed.
			//			if (!XmlUtils.GetBooleanAttributeValue(m_configurationParameters, "ShowOwnerShortname"))
			//				base.ShowRecord();
			//			else
			//SetInfoBarText();
			base.ShowRecord();
			try
			{
				// NOTE: If the clerk's current index is less than zero,
				// or greater than the number of objects in the vector,
				// SelectedIndex will assert in a debug build,
				// and throw an exception in a release build.

				// The call to m_browseViewer.SelectedIndex will trigger an event,
				// which will run the OnRecordNavigation method,
				// which will again try and set m_browseViewer.SelectedIndex,
				// so we set this to true, so OnRecordNavigation bails out.
				m_suppressRecordNavigation = true;
				m_browseViewer.SelectedIndex = currentIndex;
			}
			finally
			{
				m_suppressRecordNavigation = false;
			}
		}

		/// <summary>
		/// Record browse view implements ShowRecord by scrolling to show the current record. However,
		/// that may well not be all the response to selecting that record in the Clerk: for example,
		/// another pane may show a more detailed view of the selected record. Therefore, RecordBrowseView
		/// never claims to have 'handled' this event.
		/// </summary>
		/// <param name="argument"></param>
		/// <returns>
		/// false; we didn't fully handle it, even though we may have done something.
		/// </returns>
		public override bool OnRecordNavigation(object argument)
		{
			CheckDisposed();

			// Can't do anything if it isn't fully initialized,
			// and we don't want to do anything, if we are told not to.
			if (!m_fullyInitialized || m_suppressRecordNavigation)
				return false;
			Debug.Assert(m_browseViewer != null, "RecordBrowseView.SetupDataContext() has to be called before RecordBrowseView.OnRecordNavigation().");

			if (m_browseViewer == null || m_browseViewer.BrowseView == null || m_browseViewer.BrowseView.RootBox == null)
				return false; // can't do anything useful without a root box to select in.

			var rni = (RecordNavigationInfo)argument;
			m_suppressShowRecord = rni.SkipShowRecord;
			m_suppressRecordNavigation = rni.SuppressSaveOnChangeRecord;
			bool bvEnabled = m_browseViewer.Enabled;
			if (rni.SuppressFocusChange && bvEnabled)
				m_browseViewer.Enabled = false;
			try
			{
				RecordClerk clerk = Clerk;
				RecordClerk sendingClerk = RecordNavigationInfo.GetSendingClerk(argument);
				// NOTE: If the clerk's current index is less than zero,
				// or greater than the number of objects in the vector,
				// SelectedIndex will assert in a debug build,
				// and throw an exception in a release build.
				if (clerk != null && sendingClerk == clerk && clerk.IsActiveInGui)
				{
					m_browseViewer.SelectedIndex = clerk.CurrentIndex;
					// go ahead and SetInfoBarText even if we didn't change indices
					// we may have changed objects or root object classes (from Entries to Senses)
					SetInfoBarText();
				}
				//base.OnRecordNavigation(argument);
			}
			finally
			{
				m_suppressShowRecord = false;
				m_suppressRecordNavigation = false;
				if (rni.SuppressFocusChange && bvEnabled)
					m_browseViewer.Enabled = true;
			}

			return false;
		}

		#endregion // Other methods

		#region IxCoreColleague implementation

		/// <summary>
		/// Initialize this as an IxCoreColleague
		/// </summary>
		/// <param name="mediator"></param>
		/// <param name="propertyTable"></param>
		/// <param name="configurationParameters"></param>
		public override void Init(Mediator mediator, PropertyTable propertyTable, XmlNode configurationParameters)
		{
			CheckDisposed();
			InitBase(mediator, propertyTable, configurationParameters);
			m_browseViewer.Init(mediator, propertyTable, configurationParameters);
			m_fullyInitialized = true;
			// These have to be done here, rather than in SetupDataContext(),
			// or the record clerk resets its current object,
			// when the root object gets set in the borwse view's MakeRoot,
			// which, in  turn, resets its current index to zero,
			// which fires events. By connecting them here,
			// they won't be ready to hand off to clients.
			m_browseViewer.SelectionChanged += OnSelectionChanged;
			m_browseViewer.SelectedIndexChanged += m_browseViewer_SelectedIndexChanged;
			m_browseViewer.FilterChanged += FilterChangedHandler;
			m_browseViewer.SorterChanged += SortChangedHandler;
			m_browseViewer.ListModificationInProgressChanged += m_browseViewer_ListModificationInProgressChanged;
			m_browseViewer.BrowseView.RightMouseClickedEvent += OnFwRightMouseClick;
			m_browseViewer.SelectionDrawingFailure += OnBrowseSelectionDrawingFailed;
			m_browseViewer.CheckBoxChanged += OnCheckBoxChanged;
			Clerk.FilterChangedByClerk += Clerk_FilterChangedByClerk;
			Clerk.SorterChangedByClerk += Clerk_SorterChangedByClerk;
			if (m_browseViewer.BulkEditBar != null)
			{
				// We have a browse viewer that is using a bulk edit bar, so make sure our RecordClerk
				// is properly setup/sync'd with its saved settings.
				m_browseViewer.BulkEditBar.TargetComboSelectedIndexChanged += TargetColumnChanged;
				if (m_browseViewer.BulkEditBar.ExpectedListItemsClassId != 0)
				{
					m_browseViewer.BulkEditBar.OnTargetComboSelectedIndexChanged();
					CheckExpectedListItemsClassInSync();
				}
				else
				{
					// now that we're finished setting up the bulk edit bar, we need to make
					// sure our clerk loads its defaults, since bulk edit didn't provide information
					// for which list items class to load objects for.
					if (Clerk.ListSize == 0)
						Clerk.OnChangeListItemsClass(Clerk.SortItemProvider.ListItemsClass, 0, false);
				}
			}

			// We're seeing an odd crash occurring during Init.ShowRecord() (see LT-9498)
			// where the Display is getting updated in RestoreSelectionAndScrollPos
			// after ShowRecord() below sets m_browseViewer.CurrentIndex.
			// As far as I (EricP) can tell "RestoreSelectionAndScrollPos" should only occur
			// after Init() (ie. Application.Idle()) and the user has created a new selection based upon
			// clicking or keyboard input. In otherwords, there is no reason to try to
			// restore a BrowseView Selection that has occurred before the user has
			// done anything to create a selection with the cursor.
			// In effort to avoid this crashing path, we clear any RootBox.Selection that
			// has been set by the program up to this point. If a Selection exists,
			// we will display a Debug message alerting programmers to investigate
			// how they got into this state.
			// The only reliable ways to trigger the assertion below seemed to involve active filters
			// and switching between areas (not tools). Since clicking Ignore seems to not cause
			// any problems, I am commenting out the assertion for now.
			if (m_browseViewer.BrowseView != null &&
				m_browseViewer.BrowseView.RootBox != null &&
				m_browseViewer.BrowseView.RootBox.Selection != null)
			{
				//Debug.Fail("Not sure how/why we have a RootBox.Selection at this point in initialization. " +
				//	"Please comment in LT-9498 how you reproduced this. Perhaps it would indicate how to reproduce this crash.");

				m_browseViewer.BrowseView.RootBox.DestroySelection();
			}

			// ShowRecord() was called in InitBase, but quit without doing anything,
			// so call it again, since we are ready now.
			ShowRecord();

			Subscriber.Subscribe(EventConstants.ClerkOwningObjChanged, ClerkOwningObjChanged, m_propertyTable.GetWindow());
			Subscriber.Subscribe(EventConstants.ConsideringClosing, ConsideringClosing, m_propertyTable.GetWindow());

			// Backstop refresh: by now InitBase has finished and the list is fully loaded, so re-read the
			// full row count into the mirror. The clerk-reload subscription was set up earlier (in
			// TryActivateAvaloniaBrowse) so the initial DoneReload — and every later reload — already
			// refreshes the mirror; this one-time refresh covers the case where no reload was published.
			if (m_avaloniaBrowseHost != null)
				MirrorClerkSelectionToAvalonia();
		}

		// Fires after the clerk's record list finishes (re)loading — e.g. the initial load, a filter or sort
		// change, or a refresh. Re-reads the (now complete) row count into the Avalonia mirror.
		private void OnClerkListReloaded(object sender)
		{
			if (m_avaloniaBrowseHost == null || !ReferenceEquals(sender, Clerk))
				return;
			MirrorClerkSelectionToAvalonia();
		}

		private void CheckExpectedListItemsClassInSync()
		{
			int beExpectedListItemsClass = m_browseViewer.BulkEditBar.ExpectedListItemsClassId;
			int clerkExpectedListItemsClass = Clerk.SortItemProvider.ListItemsClass;
			RecordList.CheckExpectedListItemsClassInSync(beExpectedListItemsClass, clerkExpectedListItemsClass);
		}

		/// <summary>
		/// Get additional message targets...the browse view's targets are the only ones we have.
		/// </summary>
		/// <returns></returns>
		protected override void GetMessageAdditionalTargets(List<IxCoreColleague> collector)
		{
			if (m_browseViewer != null)
				collector.AddRange(m_browseViewer.GetMessageTargets());
			base.GetMessageAdditionalTargets(collector);
		}

		#endregion // IxCoreColleague implementation

		private void ConsideringClosing(object obj)
		{
			CheckDisposed();

			if (!(obj is CancelEventArgs args))
			{
				Debug.Assert(false, "Received unexpected object type.");
				return;
			}
			// Return if the close has already been canceled by another Subscriber.
			if (args.Cancel)
			{
				return;
			}
			args.Cancel = !PrepareToGoAway();
		}

		#region IxCoreContentControl implementation

		public override int Priority
		{
			get
			{
				return (int)ColleaguePriority.Medium;
			}
		}

		public override bool PrepareToGoAway()
		{
			if (m_browseViewer != null)
				m_browseViewer.PrepareToGoAway();
			return base.PrepareToGoAway();
		}

		#endregion // IxCoreContentControl implementation

		#region IxCoreCtrlTabProvider implementation

		public override Control PopulateCtrlTabTargetCandidateList(List<Control> targetCandidates)
		{
			if (targetCandidates == null)
				throw new ArgumentNullException("targetCandidates", "'targetCandidates' is null.");

			if (m_browseViewer != null)
			{
				int targetCount = targetCandidates.Count;
				Control focusedControl = m_browseViewer.PopulateCtrlTabTargetCandidateList(targetCandidates);
				// if we added any targets, use those candidates, otherwise use the base candidates.
				if (targetCandidates.Count > targetCount)
					return focusedControl;
			}
			return base.PopulateCtrlTabTargetCandidateList(targetCandidates);
		}

		#endregion  IxCoreCtrlTabProvider implementation

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			/*
			this.SuspendLayout();
			this.Controls.Add((UserControl)this.m_informationBar);
			this.ResumeLayout(false);*/
		}
		#endregion

		/// <summary>
		///	invoked when our BrowseView selection changes
		/// </summary>
		/// <param name="sender">unused</param>
		/// <param name="e">the event arguments</param>
		virtual public void OnSelectionChanged(object sender, FwObjectSelectionEventArgs e)
		{
			CheckDisposed();

			if (!m_suppressRecordNavigation || ! m_suppressShowRecord)
			{
				Clerk.ViewChangedSelectedRecord(e);
				SetInfoBarText();
			}
		}

		/// <summary>
		/// Event handler, which just passes the event on, if possible.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public virtual void OnCheckBoxChanged(object sender, CheckBoxChangedEventArgs e)
		{
			CheckDisposed();
			if (CheckBoxChanged != null)
				CheckBoxChanged(sender, e);
		}

		private void m_browseViewer_SelectedIndexChanged(object sender, EventArgs e)
		{
			string propName = Clerk.PersistedIndexProperty;
			m_propertyTable.SetProperty(propName, Clerk.CurrentIndex, PropertyTable.SettingsGroup.LocalSettings, true);
			m_propertyTable.SetPropertyPersistence(propName, true, PropertyTable.SettingsGroup.LocalSettings);
			MirrorClerkSelectionToAvalonia();
		}

		/// <summary>
		/// if the XML configuration does not specify the availability of the treebar
		/// (e.g. treeBarAvailability="Required"), then use this.
		/// </summary>
		protected override TreebarAvailability DefaultTreeBarAvailability
		{
			get
			{
				return TreebarAvailability.NotAllowed;
			}
		}

		public bool SnapSplitPosition(ref int width)
		{
			CheckDisposed();

			if (m_browseViewer == null)
				return false;
			return m_browseViewer.SnapSplitPosition(ref width);
		}

		public List<int> CheckedItems
		{
			get { return m_browseViewer.CheckedItems; }
		}

		/// <summary>
		/// The hvos of the objects the user has checked in the browse select column. When the Avalonia
		/// table overlay is active it reflects that surface's selection (its checkbox column is enabled for
		/// legacy parity); otherwise it falls back to the legacy <see cref="BrowseViewer.CheckedItems"/>.
		/// This is the read the product uses to drive bulk-edit over the selected rows.
		/// </summary>
		public IReadOnlyList<int> CheckedObjectHvos
		{
			get
			{
				if (m_avaloniaBrowseHost != null)
					return m_avaloniaBrowseHost.CheckedRows;
				return m_browseViewer != null ? m_browseViewer.CheckedItems : (IReadOnlyList<int>)new List<int>();
			}
		}

		private void m_browseViewer_ListModificationInProgressChanged(object sender, EventArgs e)
		{
			Clerk.ListModificationInProgress = m_browseViewer.ListModificationInProgress;
		}

		#region IBulkEditBarHost (Phase 1 List Choice)

		// The product edge for the Avalonia bulk-edit bar. The bar/VM are LCModel-free; this edge supplies
		// the eligible target columns and their options from the clerk-backed row source and runs
		// preview/apply over the owned table's CHECKED rows. Inert when the Avalonia overlay is not active.

		/// <inheritdoc />
		public IReadOnlyList<BulkEditTarget> ListChoiceTargets()
		{
			if (m_avaloniaRowSource == null)
				return new List<BulkEditTarget>();
			var targets = new List<BulkEditTarget>();
			foreach (var t in m_avaloniaRowSource.ListChoiceTargets())
				targets.Add(new BulkEditTarget(t.Column, t.Label));
			return targets;
		}

		/// <inheritdoc />
		public IReadOnlyList<RegionChoiceOption> OptionsFor(int column)
			=> m_avaloniaRowSource == null
				? new List<RegionChoiceOption>()
				: m_avaloniaRowSource.ListChoiceOptions(column);

		/// <inheritdoc />
		public int CheckedRowCount => CheckedObjectHvos.Count;

		/// <inheritdoc />
		public void Preview(int column, RegionChoiceOption option)
		{
			if (m_avaloniaBrowseHost == null || option == null)
				return;
			// The overlay shows the chosen option's DISPLAY NAME (PreviewBulkEdit stores the display value).
			m_avaloniaBrowseHost.PreviewBulkEdit(column, option.Name);
		}

		/// <inheritdoc />
		public void ClearPreview()
		{
			if (m_avaloniaRowSource == null)
				return;
			m_avaloniaRowSource.ClearBulkEditPreview();
			// Also drop any staged Delete-Rows preview marking so switching mode / re-targeting clears it too.
			m_avaloniaBrowseHost?.ClearDeletePreview();
			m_avaloniaBrowseHost?.RefreshAfterPreviewChange();
		}

		/// <inheritdoc />
		public void Apply(int column, RegionChoiceOption option)
		{
			if (m_avaloniaBrowseHost == null || option == null)
				return;
			// Apply writes the option KEY (the possibility guid) — the preview overlay held the display name.
			m_avaloniaBrowseHost.ApplyBulkEdit(column, option.Key);
		}

		// The separator inserted between an existing (non-empty) target and the appended source text in
		// Bulk Copy Append mode — a single space, matching the legacy bar's default.
		private const string BulkCopySeparator = " ";

		/// <inheritdoc />
		public IReadOnlyList<BulkEditTarget> CopySourceColumns()
		{
			if (m_avaloniaRowSource == null)
				return new List<BulkEditTarget>();
			var cols = new List<BulkEditTarget>();
			foreach (var c in m_avaloniaRowSource.CopySourceColumns())
				cols.Add(new BulkEditTarget(c.Column, c.Label));
			return cols;
		}

		/// <inheritdoc />
		public IReadOnlyList<BulkEditTarget> CopyTargets()
		{
			if (m_avaloniaRowSource == null)
				return new List<BulkEditTarget>();
			var targets = new List<BulkEditTarget>();
			foreach (var t in m_avaloniaRowSource.CopyTargets())
				targets.Add(new BulkEditTarget(t.Column, t.Label));
			return targets;
		}

		/// <inheritdoc />
		public void PreviewCopy(int sourceColumn, int targetColumn, BulkCopyMode mode)
			=> m_avaloniaBrowseHost?.PreviewBulkCopy(sourceColumn, targetColumn, mode, BulkCopySeparator);

		/// <inheritdoc />
		public void ApplyCopy(int sourceColumn, int targetColumn, BulkCopyMode mode)
			=> m_avaloniaBrowseHost?.ApplyBulkCopy(sourceColumn, targetColumn, mode, BulkCopySeparator);

		// ----- Phase 3: Bulk Clear -----

		/// <inheritdoc />
		public IReadOnlyList<BulkEditTarget> ClearTargets()
		{
			if (m_avaloniaRowSource == null)
				return new List<BulkEditTarget>();
			var targets = new List<BulkEditTarget>();
			foreach (var t in m_avaloniaRowSource.ClearTargets())
				targets.Add(new BulkEditTarget(t.Column, t.Label));
			return targets;
		}

		/// <inheritdoc />
		public void PreviewClear(int targetColumn)
			=> m_avaloniaBrowseHost?.PreviewBulkClear(targetColumn);

		/// <inheritdoc />
		public void ApplyClear(int targetColumn)
			=> m_avaloniaBrowseHost?.ApplyBulkClear(targetColumn);

		// ----- Delete Rows (the destructive mode of the legacy Delete tab) -----

		/// <inheritdoc />
		public bool CanDeleteRows => m_avaloniaBrowseHost?.CanDeleteRows ?? false;

		/// <inheritdoc />
		public int PreviewDeleteRows() => m_avaloniaBrowseHost?.PreviewDeleteRows() ?? 0;

		/// <inheritdoc />
		public int ApplyDeleteRows()
		{
			if (m_avaloniaBrowseHost == null)
				return 0;

			// Mirror CheckMultiDeleteConditionsAndReport: count the rows the per-row guards actually allow, then
			// CONFIRM with the user before any destructive work. Nothing deletable / Cancel deletes nothing.
			var deletableCount = m_avaloniaBrowseHost.CountDeletableRows();
			if (deletableCount == 0)
			{
				m_avaloniaBrowseHost.ClearDeletePreview();
				return 0;
			}

			var message = string.Format(SIL.FieldWorks.Common.FwAvalonia.FwAvaloniaStrings.BulkDeleteConfirmMessageFormat,
				deletableCount);
			var result = FwAvaloniaDialogs.FwMessageBox.Show(FindForm(), message,
				SIL.FieldWorks.Common.FwAvalonia.FwAvaloniaStrings.BulkDeleteConfirmTitle,
				FwAvaloniaDialogs.FwMessageBoxButtons.OkCancel,
				FwAvaloniaDialogs.FwMessageBoxIcon.Warning);
			if (result != FwAvaloniaDialogs.FwMessageBoxResult.Ok)
			{
				// Cancel: leave the preview marking up so the user sees what would have been deleted; no mutation.
				return 0;
			}

			// Confirmed: delete the checked, allowed objects in ONE undoable UOW (plus orphan cleanup), then the
			// host clears the preview and refreshes the row set.
			return m_avaloniaBrowseHost.ApplyDeleteRows();
		}

		// ----- Find/Replace Phase 1: Bulk Replace -----

		/// <inheritdoc />
		public IReadOnlyList<BulkEditTarget> ReplaceTargets()
		{
			if (m_avaloniaRowSource == null)
				return new List<BulkEditTarget>();
			var targets = new List<BulkEditTarget>();
			foreach (var t in m_avaloniaRowSource.ReplaceTargets())
				targets.Add(new BulkEditTarget(t.Column, t.Label));
			return targets;
		}

		/// <inheritdoc />
		public BulkReplaceSpec ShowFindReplaceSetup(BulkReplaceSpec current)
		{
			if (m_avaloniaBrowseHost == null)
				return null;

			// Translate the foundation spec into the dialog kit's FindReplacePattern, run the spec-only modal
			// over the owning WinForms form, and translate the OK result back. Cancel returns null (no change).
			var pattern = ToPattern(current);
			var vm = new FwAvaloniaDialogs.FindReplaceDialogViewModel(pattern);
			var view = new FwAvaloniaDialogs.FindReplaceDialogView { DataContext = vm };
			var accepted = AvaloniaDialogHost.ShowModal(FindForm(), view, vm,
				FwAvaloniaDialogs.FwAvaloniaDialogsStrings.FindReplaceTitle);
			if (accepted != true || vm.Result == null)
				return null;
			return FromPattern(vm.Result);
		}

		/// <inheritdoc />
		public void PreviewReplace(int targetColumn, BulkReplaceSpec spec)
			=> m_avaloniaBrowseHost?.PreviewBulkReplace(targetColumn, spec);

		/// <inheritdoc />
		public void ApplyReplace(int targetColumn, BulkReplaceSpec spec)
			=> m_avaloniaBrowseHost?.ApplyBulkReplace(targetColumn, spec);

		// The bar/row-source speak the FwAvalonia-layer BulkReplaceSpec (which cannot reference the dialog kit);
		// the dialog speaks FindReplacePattern. These two translators are the single seam between them, kept here
		// at the product edge where both layers are visible.
		private static FwAvaloniaDialogs.FindReplacePattern ToPattern(BulkReplaceSpec spec)
		{
			spec = spec ?? new BulkReplaceSpec();
			return new FwAvaloniaDialogs.FindReplacePattern
			{
				FindText = spec.FindText ?? string.Empty,
				ReplaceText = spec.ReplaceText ?? string.Empty,
				MatchCase = spec.MatchCase,
				MatchDiacritics = spec.MatchDiacritics,
				MatchWholeWord = spec.MatchWholeWord,
				MatchWritingSystem = spec.MatchWritingSystem,
				UseRegularExpressions = spec.UseRegularExpressions
			};
		}

		private static BulkReplaceSpec FromPattern(FwAvaloniaDialogs.FindReplacePattern pattern)
		{
			return new BulkReplaceSpec
			{
				FindText = pattern.FindText ?? string.Empty,
				ReplaceText = pattern.ReplaceText ?? string.Empty,
				MatchCase = pattern.MatchCase,
				MatchDiacritics = pattern.MatchDiacritics,
				MatchWholeWord = pattern.MatchWholeWord,
				MatchWritingSystem = pattern.MatchWritingSystem,
				UseRegularExpressions = pattern.UseRegularExpressions
			};
		}

		// ----- Process / Transduce -----

		// An LCModel/EncConverters-free view of one Unicode-to-Unicode IEncConverter for the bar/row-source: its
		// display name and a Convert(text) transform. The bar and the headless ClerkBrowseRowSource speak this
		// (they cannot reference SilEncConverters40); this product edge is the single seam that wraps the real
		// converter. Convert delegates to IEncConverter.Convert, which is what the legacy TransduceMethod runs.
		private sealed class EncConverterAdapter : IBulkTransduceConverter
		{
			private readonly IEncConverter _converter;
			public EncConverterAdapter(string name, IEncConverter converter)
			{
				Name = name;
				_converter = converter;
			}
			public string Name { get; }
			public string Convert(string input) => _converter.Convert(input ?? string.Empty);
		}

		/// <inheritdoc />
		public IReadOnlyList<BulkEditTarget> TransduceSourceColumns()
		{
			if (m_avaloniaRowSource == null)
				return new List<BulkEditTarget>();
			var cols = new List<BulkEditTarget>();
			foreach (var c in m_avaloniaRowSource.CopySourceColumns())
				cols.Add(new BulkEditTarget(c.Column, c.Label));
			return cols;
		}

		/// <inheritdoc />
		public IReadOnlyList<BulkEditTarget> TransduceColumns()
		{
			if (m_avaloniaRowSource == null)
				return new List<BulkEditTarget>();
			var targets = new List<BulkEditTarget>();
			foreach (var t in m_avaloniaRowSource.TransduceColumns())
				targets.Add(new BulkEditTarget(t.Column, t.Label));
			return targets;
		}

		/// <inheritdoc />
		public IReadOnlyList<IBulkTransduceConverter> AvailableConverters()
		{
			// Mirror the legacy InitConverterCombo filter: only the Unicode-to-Unicode (and Unicode-encoding)
			// converters from the EncConverters pool are relevant to a plain-text transduce. A pool that cannot
			// be accessed yields an empty list (the bar then offers no converter and Apply stays disabled).
			var result = new List<IBulkTransduceConverter>();
			IEncConverters pool;
			try
			{
				pool = new EncConverters();
			}
			catch
			{
				return result;
			}
			foreach (string convName in pool.Keys)
			{
				IEncConverter conv;
				try
				{
					conv = pool[convName];
				}
				catch
				{
					continue;
				}
				if (conv != null
					&& (conv.ConversionType == ConvType.Unicode_to_Unicode
						|| conv.ConversionType == ConvType.Unicode_to_from_Unicode))
					result.Add(new EncConverterAdapter(convName, conv));
			}
			return result;
		}

		/// <inheritdoc />
		public IReadOnlyList<IBulkTransduceConverter> LaunchConverterSetup()
		{
			// Launch the EncConverters management dialog exactly as the legacy m_transduceSetupButton_Click does
			// (AddCnvtrDlg over the owning WinForms form), then re-read the converter list so a newly-added
			// converter appears in the picker. The dialog is a WinForms modal owned by this product edge; the
			// Avalonia bar only invokes this hook. Inert when the overlay is not active or the app/help services
			// are unavailable.
			if (m_avaloniaRowSource == null || m_propertyTable == null)
				return null;
			try
			{
				var app = m_propertyTable.GetValue<IApp>("App");
				var help = m_propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider");
				using (var dlg = new AddCnvtrDlg(help, app, null))
					dlg.ShowDialog(FindForm());
			}
			catch
			{
				// A failure to open the dialog (missing services / EC pool) must not crash the bar; just return
				// the current converter list so the picker stays usable.
			}
			return AvailableConverters();
		}

		/// <inheritdoc />
		public void PreviewTransduce(int sourceColumn, int targetColumn, IBulkTransduceConverter converter, BulkCopyMode mode)
			=> m_avaloniaBrowseHost?.PreviewBulkTransduce(sourceColumn, targetColumn, converter, mode, BulkCopySeparator);

		/// <inheritdoc />
		public void ApplyTransduce(int sourceColumn, int targetColumn, IBulkTransduceConverter converter, BulkCopyMode mode)
			=> m_avaloniaBrowseHost?.ApplyBulkTransduce(sourceColumn, targetColumn, converter, mode, BulkCopySeparator);

		// ----- Click Copy (interactive per-click copy of a clicked source cell into a target column) -----

		/// <inheritdoc />
		public IReadOnlyList<BulkEditTarget> ClickCopyTargets()
		{
			if (m_avaloniaRowSource == null)
				return new List<BulkEditTarget>();
			var targets = new List<BulkEditTarget>();
			foreach (var t in m_avaloniaRowSource.ClickCopyTargets())
				targets.Add(new BulkEditTarget(t.Column, t.Label));
			return targets;
		}

		/// <inheritdoc />
		public void ApplyClickCopy(int sourceColumn, int targetColumn, int rowIndex, int charOffset, ClickCopyMode mode,
			string separator, bool append)
			=> m_avaloniaBrowseHost?.ApplyClickCopy(sourceColumn, targetColumn, rowIndex, charOffset, mode, separator, append);

		#endregion IBulkEditBarHost

		/// <summary>
		/// Gives access to the BrowseViewer which implements most of the functionality.
		/// </summary>
		public BrowseViewer BrowseViewer
		{
			get { return m_browseViewer; }
		}

		public void PostLayoutInit()
		{
			m_browseViewer.PostLayoutInit();
		}

		public bool IsFocusedPane
		{
			get; set;
		}
	}
	/// <summary>
	/// A browse view which has the select column hooked to an Active boolean
	///  (which is the UI name of the Disabled property of phonological rules,
	///   compound rules, ad hoc rules, and inflectional affix templates).  We
	///  only use this view with phonological rules and compound rules.
	/// </summary>
	public class RecordBrowseActiveView : RecordBrowseView
	{

		protected override BrowseViewer CreateBrowseViewer(XmlNode nodeSpec, int hvoRoot, int fakeFlid, LcmCache cache,
			Mediator mediator, PropertyTable propertyTable,
			ISortItemProvider sortItemProvider, ISilDataAccessManaged sda)
		{
			var viewer = new BrowseActiveViewer(nodeSpec,
						 hvoRoot, fakeFlid,
						 cache, mediator, propertyTable, sortItemProvider, sda);
			viewer.CheckBoxActiveChanged += OnCheckBoxActiveChanged;
			return viewer;
		}

		/// <summary>
		/// Event handler, which makes any changes to the Active flag.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void OnCheckBoxActiveChanged(object sender, CheckBoxActiveChangedEventArgs e)
		{
			base.OnCheckBoxChanged(sender, e);
			var changedHvos = e.HvosChanged;
			UndoableUnitOfWorkHelper.Do(e.UndoMessage, e.RedoMessage, Cache.ActionHandlerAccessor, () =>
				ChangeAnyDisabledFlags(changedHvos));
		}
		private void ChangeAnyDisabledFlags(int[] changedHvos)
		{
			foreach (var hvo in changedHvos)
			{
				ICmObject obj = Cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo);
				switch (obj.ClassID)
				{
					case PhRegularRuleTags.kClassId: // fall through
					case PhMetathesisRuleTags.kClassId:
						var segmentRule = obj as IPhSegmentRule;
						segmentRule.Disabled = !segmentRule.Disabled;
						break;
					case MoEndoCompoundTags.kClassId: // fall through
					case MoExoCompoundTags.kClassId:
						var compoundRule = obj as IMoCompoundRule;
						compoundRule.Disabled = !compoundRule.Disabled;
						break;
				}
			}
		}
	}
}
