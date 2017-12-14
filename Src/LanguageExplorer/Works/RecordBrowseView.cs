// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Controls.XMLViews;
using LanguageExplorer.LcmUi;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Widgets;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Infrastructure;
using SIL.FieldWorks.Filters;
using SIL.Xml;
using WaitCursor = SIL.FieldWorks.Common.FwUtils.WaitCursor;

namespace LanguageExplorer.Works
{
	/// <summary>
	/// RecordBrowseView is a table oriented view of the collection
	/// </summary>
	public class RecordBrowseView : RecordView, ISnapSplitPosition, IPostLayoutInit, IFocusablePanePortion
	{
		public event CheckBoxChangedEventHandler CheckBoxChanged;

		#region Data members
		/// <summary>
		///
		/// </summary>
		protected BrowseViewer m_browseViewer;
		private bool m_suppressRecordNavigation;
		protected bool m_suppressShowRecord;
		private bool m_fHandlingFilterChangedByRecordList;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private readonly System.ComponentModel.Container components;
		#endregion // Data members

		#region Construction and disposal

		public RecordBrowseView()
		{
			Init();
		}

		public RecordBrowseView(XElement browseViewDefinitions, LcmCache cache, IRecordList recordList)
			: base(browseViewDefinitions, cache, recordList)
		{
			Init();
		}

		private void Init()
		{
			InitializeComponent();
			AccNameDefault = "RecordBrowseView";	// default accessibility name
			Name = "RecordBrowseView";
		}

		#region Overrides of XWorksViewBase

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		public override void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			base.InitializeFlexComponent(flexComponentParameters);

			InitBase();
			m_fullyInitialized = true;
			// These have to be done here, rather than in SetupDataContext(),
			// or the record list resets its current object,
			// when the root object gets set in the browse view's MakeRoot,
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
			MyRecordList.FilterChangedByList += RecordList_FilterChangedByList;
			MyRecordList.SorterChangedByList += RecordList_SorterChangedByList;
			if (m_browseViewer.BulkEditBar != null)
			{
				// We have a browse viewer that is using a bulk edit bar, so make sure our RecordList
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
					// sure our record list loads its defaults, since bulk edit didn't provide information
					// for which list items class to load objects for.
					if (MyRecordList.ListSize == 0)
					{
						MyRecordList.OnChangeListItemsClass(MyRecordList.ListItemsClass, 0, false);
					}
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

			Subscriber.Subscribe("RecordListOwningObjChanged", RecordListOwningObjChanged_Message_Handler);

			ShowRecord();
		}

		#endregion

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
				Subscriber.Unsubscribe("RecordListOwningObjChanged", RecordListOwningObjChanged_Message_Handler);
				// Next 3 calls assume MyRecordList is not null. I (RBR) wonder if the assumption is good?
				PersistSortSequence();
				MyRecordList.FilterChangedByList -= RecordList_FilterChangedByList;
				MyRecordList.SorterChangedByList -= RecordList_SorterChangedByList;

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
					m_browseViewer.SortersCompatible -= AreSortersCompatible;
				}
				components?.Dispose();
			}
			m_browseViewer = null;
			// m_mediator = null; // No, or the superclass call will crash.

			base.Dispose(disposing);
		}

		#endregion // Construction and disposal

		#region Message Handlers

		private bool AreSortersCompatible(RecordSorter first, RecordSorter second)
		{
			return first.CompatibleSorter(second);
		}

		/// <summary>
		/// Signal the record list to change its filter to the user selected value.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void FilterChangedHandler(object sender, FilterChangeEventArgs args)
		{
			// If we're in the process of notifying clients that the record list has changed
			// the filter, we don't need to tell the record list to change the filter (again)!
			if (m_fHandlingFilterChangedByRecordList)
				return;
			MyRecordList.OnChangeFilter(args);
		}

		/// <summary>
		/// Notify clients that the record list has changed the filter.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void RecordList_FilterChangedByList(object sender, FilterChangeEventArgs e)
		{
			m_fHandlingFilterChangedByRecordList = true;
			// Let the client(s) know about the change.
			m_browseViewer.UpdateFilterBar(MyRecordList.Filter);
			m_fHandlingFilterChangedByRecordList = false;
		}

		private void RecordList_SorterChangedByList(object sender, EventArgs e)
		{
			m_browseViewer.InitSorter(MyRecordList.Sorter, true);
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
			// if the default column is NOT being sorted, we tell the record list to display what the items are being sorted by
			if (sortedCols.Count > 0)
			{
				colName = m_browseViewer.GetColumnName(sortedCols[0]);
				isDefaultSort = sortedCols[0] == m_browseViewer.DefaultSortColumn;
			}
			MyRecordList.OnSorterChanged(m_browseViewer.Sorter, colName, isDefaultSort);
		}

		private void TargetColumnChanged(object sender, TargetColumnChangedEventArgs e)
		{
			if (e.ExpectedListItemsClass == 0)
				return; // no target column selected, so it shouldn't matter what the class of the record list's list items are.
			using (new WaitCursor(this))
			{
				// we're changing the class of our list items.
				// use ListUpdateHelper to suspend reloading the list until we've changed the class
				// and recomputed the columns. Otherwise, we'll try to reload the list and redraw the display
				// with columns that may not have all their parts in place (e.g. for generated custom fields)
				using (new ListUpdateHelper(MyRecordList))
				{
					// change the list items class, but don't do the reload && refresh display
					// until after we've recomputed our columns to allow regenerating custom field parts
					// for that new class.
					MyRecordList.OnChangeListItemsClass(e.ExpectedListItemsClass, e.TargetFlid, e.ForceReload);
					CheckExpectedListItemsClassInSync();
					// Recompute the possible columns, so the layout/parts system
					// can generate parts for custom fields based upon a new root object class.
					// this should not actually change the column specs themselves since
					// they refer to those parts through "layout" references.
					m_browseViewer.BrowseView.Vc.ComputePossibleColumns();
				}
			}
		}

		private void RecordListOwningObjChanged_Message_Handler(object newValue)
		{
			if (m_browseViewer == null)
				return;

			if (MyRecordList.OwningObject == null)
			{
				//this happens, for example, when they user sets a filter on the
				//list we are dependent on, but no records are selected by the filter.
				//thus, we now do not have an object to get records out of,
				//so we need to just show a blank list.
				m_browseViewer.RootObjectHvo = -1;
			}
			else
			{
				m_browseViewer.RootObjectHvo = MyRecordList.OwningObject.Hvo;
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

			MyRecordList.OnRefresh(null);
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
						ui.InitializeFlexComponent(new FlexComponentParameters(PropertyTable, Publisher, Subscriber));
						e.EventHandled = ui.HandleRightClick(sender, true, "mnuBrowseView");
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
			// They are in the XElement: m_configurationParametersElement.
		}

		protected override void SetupDataContext()
		{
			base.SetupDataContext();
			// Make sure our persisted sorter/filters are up to date so browse viewer
			// has the latest set for configuring/activating the filterBar
			bool fNeedReload = MyRecordList.UpdateFiltersAndSortersIfNeeded();
			// This is mainly to handle the possibility that one of our objects in a virtual
			// property has been deleted, either by some other tool, or by another client altogether.
			// Enhance: it would be very nice not to do this any time we can be sure it isn't needed.

			// The second 'true' here is to make it skip the sort.  The sort has to be skipped at this
			// point because our VC has been disposed, and we haven't made a new one yet.  The sort
			// will happen later in the sequence of Init when InitSorter is called

			int hvo = 0;
			if (MyRecordList.OwningObject != null)
				hvo = MyRecordList.OwningObject.Hvo;
			// We must update the list if needed BEFORE we create the actual view, otherwise, if it is trying
			// to display an out-of-date list containing deleted objects, all kinds of things may go wrong.
			if (XmlUtils.GetOptionalBooleanAttributeValue(m_configurationParametersElement, "forceReloadListOnInitOrChangeRoot", false))
			{
				PropertyTable.SetProperty(MyRecordList.Id + "_AlwaysRecomputeVirtualOnReloadList", true, true, true);
				// (EricP) when called by RecordView.InitBase() in the context of ListUpdateHelper.ClearBrowseListUntilReload
				// the list does not get reloaded until ListUpdateHelper is disposed, but the views property
				// will get cleared to prevent these views from accessing invalid objects.
				MyRecordList.UpdateList(false, true);
			}

			m_browseViewer = CreateBrowseViewer(m_configurationParametersElement, hvo, Cache, MyRecordList, MyRecordList.VirtualListPublisher);
			m_browseViewer.InitializeFlexComponent(new FlexComponentParameters(PropertyTable, Publisher, Subscriber));
			m_browseViewer.FinishInitialization(hvo, m_madeUpFieldIdentifier);
			m_browseViewer.SortersCompatible += AreSortersCompatible;
			// If possible make it use the style sheet appropriate for its main window.
			m_browseViewer.SuspendLayout();
			SetStyleSheet();
			m_browseViewer.Dock = DockStyle.Fill;
			var linkFilter = m_browseViewer.FilterFromLink(PropertyTable.GetValue<FwLinkArgs>("FwLinkArgs")); // It will mostly be null.
			if (linkFilter != null)
			{
				MyRecordList.OnChangeFilter(new FilterChangeEventArgs(linkFilter, MyRecordList.Filter));
			}
			if (MyRecordList.Filter != null && !MyRecordList.Filter.IsValid)
			{
				MyRecordList.ResetFilterToDefault();
			}
			m_browseViewer.UpdateFilterBar(MyRecordList.Filter);
			bool fSortChanged = m_browseViewer.InitSorter(MyRecordList.Sorter); // true if we had to change sorter
			// Do this AFTER we init the sorter and filter, so if any changes are made to the
			// sorter or filter as we install, we still get the right load.
			if (fSortChanged)
			{
				HandleSortChange();
				// Keep the current index -- see LT-8755.
			}
			else
			{
				var sortedCols = m_browseViewer.SortedColumns;
				MyRecordList.IsDefaultSort = sortedCols.Count > 0 && sortedCols[0] == m_browseViewer.DefaultSortColumn;
				// This won't actually load if in the context of UpdateListHelper()
				MyRecordList.UpdateList(true, fNeedReload);
			}
			// Do this very late, it can't display properly until its record list has been built and sorted.
			Controls.Add(m_browseViewer);
			m_browseViewer.BringToFront();
			m_browseViewer.ResumeLayout();
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

		protected virtual BrowseViewer CreateBrowseViewer(XElement nodeSpec, int hvoRoot, LcmCache cache,
			ISortItemProvider sortItemProvider,ISilDataAccessManaged sda)
		{
			return new BrowseViewer(nodeSpec,
						 hvoRoot,
						 cache, sortItemProvider, sda);
		}

		private void SetStyleSheet()
		{
			if (m_browseViewer == null || m_browseViewer.StyleSheet != null)
				return;

			m_browseViewer.StyleSheet = FontHeightAdjuster.StyleSheetFromPropertyTable(PropertyTable);
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
			string titleId = XmlUtils.GetOptionalAttributeValue(m_configurationParametersElement,
				"altTitleId");
			if (titleId != null)
			{
				XmlViewsUtils.TryFindString("AlternativeTitles", titleId, out titleStr);
				// if they specified an altTitleId, but it wasn't found, they need to do something,
				// so just return *titleId*
				if (MyRecordList.OwningObject != null && titleId.StartsWith("Reversal") &&
					XmlUtils.GetBooleanAttributeValue(m_configurationParametersElement, "ShowOwnerShortname"))
				{
					// Originally this option was added to enable Bulk Edit Reversal Entries title bar to show
					// which reversal index was being shown. If the 'titleId.StartsWith("Reversal")' in the 'if'
					// above is removed then the Word List Concordance shows the word being concorded in the
					// right pane title bar.
					titleStr = string.Format(xWorksStrings.ksXReversalIndex, MyRecordList.OwningObject.ShortName, titleStr);
				}
			}
			else if (MyRecordList.OwningObject != null)
			{
				if (XmlUtils.GetBooleanAttributeValue(m_configurationParametersElement, "ShowOwnerShortname"))
				{
					titleStr = MyRecordList.OwningObject.ShortName;
				}
			}
			if (String.IsNullOrEmpty(titleStr))
			{
				XmlViewsUtils.TryFindPluralFormFromFlid(MyRecordList.VirtualListPublisher.MetaDataCache, MyRecordList.OwningFlid, out titleStr);
			}

			bool fBaseCalled = false;
			if (String.IsNullOrEmpty(titleStr))
			{
				base.SetInfoBarText();
				fBaseCalled = true;
//				titleStr = ((IPaneBar)m_informationBar).Text;	// can't get to work.
				// (EricP) For some reason I can't provide an IPaneBar get-accessor to return
				// the new Text value. If it's desirable to allow TitleFormat to apply to
				// RecordList.CurrentObject, then we either have to duplicate what the
				// base.SetInfoBarText() does here, or get the string set by the base.
				// for now, let's just return.
				if (string.IsNullOrEmpty(titleStr))
					return;
			}

			// If we have a format attribute, format the title accordingly.
			string sFmt = XmlUtils.GetOptionalAttributeValue(m_configurationParametersElement, "TitleFormat");
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
			// Our own Init method needs to call it again.
			// Either that, or we can try initializing the browse viewer, before calling InitBase,
			// but that may be worse.
			if (!m_fullyInitialized || m_suppressShowRecord)
				return;
			Debug.Assert(m_browseViewer != null, "RecordBrowseView.SetupDataContext() has to be called before RecordBrowseView.ShowRecord().");

			// This is a bizarre situation that occurs when the root object is changing and
			// notifications get sent in non-optimal order. There will be another
			// ShowRecord call after the two get synchronized.
			if (MyRecordList.OwningObject != null && MyRecordList.OwningObject.Hvo != m_browseViewer.RootObjectHvo)
				return;
			int currentIndex = MyRecordList.CurrentIndex;

			int storedIndex = PropertyTable.GetValue(MyRecordList.PersistedIndexProperty, SettingsGroup.LocalSettings, currentIndex);
			if (storedIndex != currentIndex && storedIndex >= 0 && !MyRecordList.HasEmptyList)
			{
				try
				{
					MyRecordList.JumpToIndex(storedIndex);
					currentIndex = MyRecordList.CurrentIndex;
					Debug.Assert(currentIndex == storedIndex);
				}
				catch
				{
					if (currentIndex >= 0)
						MyRecordList.JumpToIndex(currentIndex);
				}
			}
			// all that the base method currently does is put the class name of the selected object
			// into our information bar. If we're supposed to be showing the short name of the root
			// object, that needs to be suppressed.
			//			if (!XmlUtils.GetBooleanAttributeValue(m_configurationParametersElement, "ShowOwnerShortname"))
			//				base.ShowRecord();
			//			else
			//SetInfoBarText();
			base.ShowRecord();
			try
			{
				// NOTE: If the record list's current index is less than zero,
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
		/// that may well not be all the response to selecting that record in the record list: for example,
		/// another pane may show a more detailed view of the selected record. Therefore, RecordBrowseView
		/// never claims to have 'handled' this event.
		/// </summary>
		protected override void RecordList_RecordChanged_Handler(object sender, RecordNavigationEventArgs e)
		{
			// Don't call base, since we don't want that behavior.
			// Can't do anything if it isn't fully initialized,
			// and we don't want to do anything, if we are told not to.
			if (!m_fullyInitialized || m_suppressRecordNavigation)
				return;

			Debug.Assert(m_browseViewer != null, "RecordBrowseView.SetupDataContext() has to be called before RecordBrowseView.OnRecordNavigation().");

			if (m_browseViewer == null || m_browseViewer.BrowseView == null || m_browseViewer.BrowseView.RootBox == null)
				return; // can't do anything useful without a root box to select in.

			m_suppressShowRecord = e.RecordNavigationInfo.SkipShowRecord;
			m_suppressRecordNavigation = e.RecordNavigationInfo.SuppressSaveOnChangeRecord;
			bool bvEnabled = m_browseViewer.Enabled;
			if (e.RecordNavigationInfo.SuppressFocusChange && bvEnabled)
				m_browseViewer.Enabled = false;
			try
			{
				// NOTE: If the record list's current index is less than zero,
				// or greater than the number of objects in the vector,
				// SelectedIndex will assert in a debug build,
				// and throw an exception in a release build.
				if (MyRecordList != null && MyRecordList.IsActiveInGui)
				{
					m_browseViewer.SelectedIndex = MyRecordList.CurrentIndex;
					// go ahead and SetInfoBarText even if we didn't change indices
					// we may have changed objects or root object classes (from Entries to Senses)
					SetInfoBarText();
				}
			}
			finally
			{
				m_suppressShowRecord = false;
				m_suppressRecordNavigation = false;
				if (e.RecordNavigationInfo.SuppressFocusChange && bvEnabled)
					m_browseViewer.Enabled = true;
			}
		}

		#endregion // Other methods

		private void CheckExpectedListItemsClassInSync()
		{
			var beExpectedListItemsClass = m_browseViewer.BulkEditBar.ExpectedListItemsClassId;
			var recordListExpectedListItemsClass = MyRecordList.ListItemsClass;
			RecordList.CheckExpectedListItemsClassInSync(beExpectedListItemsClass, recordListExpectedListItemsClass);
		}

		public bool OnConsideringClosing(object argument, System.ComponentModel.CancelEventArgs args)
		{
			CheckDisposed();

			args.Cancel = !PrepareToGoAway();
			return args.Cancel; // if we want to cancel, others don't need to be asked.
		}

		#region IMainContentControl implementation

		public override bool PrepareToGoAway()
		{
			if (m_browseViewer != null)
				m_browseViewer.PrepareToGoAway();
			return base.PrepareToGoAway();
		}

		#endregion // IMainContentControl implementation

		#region ICtrlTabProvider implementation

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

		#endregion  ICtrlTabProvider implementation

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
				MyRecordList.ViewChangedSelectedRecord(e);
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
			string propName = MyRecordList.PersistedIndexProperty;
			PropertyTable.SetProperty(propName, MyRecordList.CurrentIndex, SettingsGroup.LocalSettings, true, true);
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

		public List<int> CheckedItems => m_browseViewer.CheckedItems;

		private void m_browseViewer_ListModificationInProgressChanged(object sender, EventArgs e)
		{
			MyRecordList.ListModificationInProgress = m_browseViewer.ListModificationInProgress;
		}

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

		protected override BrowseViewer CreateBrowseViewer(XElement nodeSpec, int hvoRoot, LcmCache cache,
			ISortItemProvider sortItemProvider, ISilDataAccessManaged sda)
		{
			var viewer = new BrowseActiveViewer(nodeSpec,
						 hvoRoot,
						 cache, sortItemProvider, sda);
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
