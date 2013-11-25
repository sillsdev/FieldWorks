// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: RecordBrowseView.cs
// Responsibility: WordWorks
// Last reviewed:
//
// <remarks>
// </remarks>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using System.Xml;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.FdoUi;
using SIL.FieldWorks.Filters;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.XWorks
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
		private bool m_fHandlingFilterChangedByClerk;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private readonly System.ComponentModel.Container components;

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
				// NO. The simple Clerk property guarantees is will not be null.
				//if (Clerk != null)
				//	Clerk.FilterChangedByClerk -= new FilterChangeHandler(Clerk_FilterChangedByClerk);
				if (ExistingClerk != null)
				{
					PersistSortSequence();
					ExistingClerk.FilterChangedByClerk -= Clerk_FilterChangedByClerk;
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
				if (components != null)
					components.Dispose();
			}
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

		public bool OnClerkOwningObjChanged(object sender)
		{
			CheckDisposed();

			if (Clerk != sender || (m_browseViewer==null))
				return false;

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
			return false; //allow others clients of this clerk to know about it as well.
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
						e.EventHandled = ui.HandleRightClick(m_mediator, sender, true, "mnuBrowseView");
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
			bool fNeedReload = Clerk.UpdateFiltersAndSortersIfNeeded();
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
				m_mediator.PropertyTable.SetProperty(Clerk.Id + "_AlwaysRecomputeVirtualOnReloadList", true);
				// (EricP) when called by RecordView.InitBase() in the context of ListUpdateHelper.ClearBrowseListUntilReload
				// the list does not get reloaded until ListUpdateHelper is disposed, but the views property
				// will get cleared to prevent these views from accessing invalid objects.
				Clerk.UpdateList(false, true);
			}

			m_browseViewer = CreateBrowseViewer(m_configurationParameters, hvo, m_fakeFlid, Cache, m_mediator,
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
		}

		/// <summary>
		/// Set up the current 'interesting texts' to include the part of Scripture (currently only 'all' is
		/// supported) specified byt the mediator property "LinkScriptureBooksWanted". Then remeove that property.
		/// </summary>
		void SetupLinkScripture()
		{
			string booksWanted = m_mediator.PropertyTable.GetStringProperty("LinkScriptureBooksWanted", null);
			if (booksWanted == null)
				return;
			m_mediator.PropertyTable.RemoveProperty("LinkScriptureBooksWanted");
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

			var interestingTexts = InterestingTextsDecorator.GetInterestingTextList(m_mediator, Cache.ServiceLocator);
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

		protected virtual BrowseViewer CreateBrowseViewer(XmlNode nodeSpec, int hvoRoot, int fakeFlid, FdoCache cache, Mediator mediator,
			ISortItemProvider sortItemProvider,ISilDataAccessManaged sda)
		{
			return new BrowseViewer(nodeSpec,
						 hvoRoot, fakeFlid,
						 cache, mediator, sortItemProvider, sda);
		}

		private void SetStyleSheet()
		{
			if (m_browseViewer == null || m_browseViewer.StyleSheet != null)
				return;

			m_browseViewer.StyleSheet = FontHeightAdjuster.StyleSheetFromMediator(m_mediator);
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
				XmlViewsUtils.TryFindString(StringTbl, "AlternativeTitles", titleId, out titleStr);
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
					StringTbl, Clerk.OwningFlid, out titleStr);
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

			int storedIndex = m_mediator.PropertyTable.GetIntProperty(Clerk.PersistedIndexProperty, currentIndex, PropertyTable.SettingsGroup.LocalSettings);
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
		/// <param name="configurationParameters"></param>
		public override void Init(Mediator mediator, XmlNode configurationParameters)
		{
			CheckDisposed();
			InitBase(mediator, configurationParameters);
			m_browseViewer.Init(mediator, configurationParameters);
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

		public bool OnConsideringClosing(object argument, System.ComponentModel.CancelEventArgs args)
		{
			CheckDisposed();

			args.Cancel = !PrepareToGoAway();
			return args.Cancel; // if we want to cancel, others don't need to be asked.
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
			m_mediator.PropertyTable.SetProperty(propName, Clerk.CurrentIndex, PropertyTable.SettingsGroup.LocalSettings);
			m_mediator.PropertyTable.SetPropertyPersistence(propName, true, PropertyTable.SettingsGroup.LocalSettings);
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

		private void m_browseViewer_ListModificationInProgressChanged(object sender, EventArgs e)
		{
			Clerk.ListModificationInProgress = m_browseViewer.ListModificationInProgress;
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

		protected override BrowseViewer CreateBrowseViewer(XmlNode nodeSpec, int hvoRoot, int fakeFlid, FdoCache cache, Mediator mediator,
					ISortItemProvider sortItemProvider, ISilDataAccessManaged sda)
		{
			var viewer = new BrowseActiveViewer(nodeSpec,
						 hvoRoot, fakeFlid,
						 cache, mediator, sortItemProvider, sda);
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
					case FDO.PhRegularRuleTags.kClassId: // fall through
					case FDO.PhMetathesisRuleTags.kClassId:
						var segmentRule = obj as FDO.IPhSegmentRule;
						segmentRule.Disabled = !segmentRule.Disabled;
						break;
					case FDO.MoEndoCompoundTags.kClassId: // fall through
					case FDO.MoExoCompoundTags.kClassId:
						var compoundRule = obj as FDO.IMoCompoundRule;
						compoundRule.Disabled = !compoundRule.Disabled;
						break;
				}
			}
		}
	}
}
