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
// File: RecordClerk.cs
// Authorship History: John Hatton
// Last reviewed:
//
// <remarks>
//	This class, essentially, adapts a RecordList to the xCore/xWorks environment.
//	This class is entered into the XCore PropertyTable, so that it is persistent even when
//	the use or the current tools are changing.  This allows us to not lose track of what record
//	the user was looking at last time he looked at this vector.  The subject will be shared by multiple views
//  of the same vector, so that they too will not lose track of the current context.
// </remarks>
			/*
			 * important note about messages and record clerk. I know this is important, because I wrote all this code
			 * but then I just spent 20 minutes trying to figure out why I was not getting a message.
			 * things to keep in mind:
				* there is one record clerk for each vector.
				* the record clerks, once created, never go away.
				* They are stored in the PropertyTable; this is what we want.
				* however, they only receive messages when they have, shall we say, a sponsor.
			 * that is, they are never in the list of colleagues kept by the mediator.
			 * this is important, because you don't want arbitrary or multiple record clerks all responding
			 * to the latest "add", "delete", etc. message.
			 *
			 * SO....if you record clerk is not getting the some message you think it should be getting,
			 * it's probably because the current content control is not including it in its list of message targets.
			*/

// --------------------------------------------------------------------------------------------
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Forms;
using System.Xml;
using System.Collections;
using System.Collections.Generic;

using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FdoUi;
using SIL.FieldWorks.Filters;
using SIL.Utils;
using XCore;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Takes care of a list of records, standing between it and the UI.
	/// </summary>
	public class RecordClerk : IFWDisposable, IxCoreColleague, IRecordListUpdater
	{
		static protected RecordClerk s_lastClerkToLoadTreeBar;

		protected string m_id;
		protected Mediator m_mediator;
		private XmlNode m_clerkConfiguration;
		/// <summary>
		/// this will be null is this clerk is dependent on another one. Only the top-level clerk
		/// gets to be represented by and interact with the tree bar.
		/// </summary>
		protected RecordBarHandler m_recordBarHandler;
		protected RecordList m_list;

		/// <summary>
		/// when this is not in all, that means there is another clerk managing a list,
		/// and the selected item of that list provides the object that this
		/// RecordClerk gets items out of. For example, the WfiAnalysis clerk
		/// is dependent on the WfiWordform clerk to tell at which wordform it is supposed to
		/// the displaying the analyses of.
		/// </summary>
		protected string m_clerkProvidingRootObject;

		/// <summary>
		/// this is an object which gives us the list of filters which we should offer to the user from the UI.
		/// this does not include the filters they can get that by using the FilterBar.
		/// </summary>
		protected RecordFilterListProvider m_filterProvider;

		private bool m_editable = true;
		private bool m_suppressSaveOnChangeRecord = false; // true during delete and insert and ShowRecord calls caused by them.
		private bool m_skipShowRecord = false; // skips navigations while a user is editing something.
		private bool m_shouldHandleDeletion = true; // false, if the dependent clerk is to handle deletion, as for reversals.
		private bool m_fAllowDeletions = true;	// false if nothing is to be deleted for this record clerk.

		/// <summary>
		/// We need to store what filter we are responsible for setting, locally, so
		/// that when the user says "no filter/all records",
		/// we can selectively remove just this filter from the set that is being kept by the
		/// RecordList. That list would contain filters contributed from other sources, in particular
		/// the FilterBar.
		/// </summary>
		protected RecordFilter m_activeMenuBarFilter;
		protected IRecordChangeHandler m_rch = null;

		/// <summary>
		/// Store the XmlNode that configured the default filter for the clerk in Init().
		/// </summary>
		XmlNode m_filterNode = null;

		/// <summary>
		/// The display name of what is currently being sorted. This variable is persisted as a user
		/// setting. When the sort name is null it indicates that the items in the clerk are not
		/// being sorted or that the current sorting should not be displayed (i.e. the default column
		/// is being sorted).
		/// </summary>
		private string m_sortName = null;
		private bool m_isDefaultSort = false;

		#region Event Handling
		public event FilterChangeHandler FilterChangedByClerk;
		#endregion Event Handling

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="RecordClerk"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public RecordClerk()
		{
		}

		#region IDisposable & Co. implementation
		// Region last reviewed: never

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
		~RecordClerk()
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
				//ResetStatusBarPanel("StatusPanelRecordNumber", "");
				//ResetStatusBarPanel("StatusPanelMessage", "");
				m_list.ListChanged -= new ListChangedEventHandler(OnListChanged);
				m_list.AboutToReload -= new EventHandler(m_list_AboutToReload);
				m_list.DoneReload -= new EventHandler(m_list_DoneReload);
				m_list.Dispose();
				if (m_mediator != null)
				{
					m_mediator.RemoveColleague(this);
				}
				if (m_filterProvider != null)
					m_filterProvider.Dispose();
				if (m_rch != null && m_rch is IDisposable)
					(m_rch as IDisposable).Dispose();
				if (m_recordBarHandler != null)
					m_recordBarHandler.Dispose();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_mediator = null; // This has to be done after the calls to ResetStatusBarPanel in the 'true' section.
			m_list = null;
			m_id = null;
			m_clerkProvidingRootObject = null;
			m_recordBarHandler = null;
			m_filterProvider = null;
			m_activeMenuBarFilter = null;
			m_rch = null;
			m_fIsActiveInGui = false;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		#region IxCoreColleague implementation

		/// <summary>
		/// Initialize the IxCoreColleague
		/// </summary>
		/// <param name="mediator"></param>
		/// <param name="viewConfiguration"></param>
		public virtual void Init(Mediator mediator, XmlNode viewConfiguration)
		{
			CheckDisposed();

			XmlNode clerkConfiguration = ToolConfiguration.GetClerkNodeFromToolParamsNode(viewConfiguration);
			m_mediator = mediator;
			m_clerkConfiguration = clerkConfiguration;
			m_id = XmlUtils.GetOptionalAttributeValue(clerkConfiguration, "id", "missingId");
			m_clerkProvidingRootObject = XmlUtils.GetOptionalAttributeValue(clerkConfiguration,"clerkProvidingOwner");
			m_shouldHandleDeletion = XmlUtils.GetOptionalBooleanAttributeValue(clerkConfiguration, "shouldHandleDeletion", true);
			m_fAllowDeletions = XmlUtils.GetOptionalBooleanAttributeValue(clerkConfiguration, "allowDeletions", true);
			FdoCache cache = (FdoCache)m_mediator.PropertyTable.GetValue("cache");
			m_list = RecordList.Create(cache, mediator, clerkConfiguration.SelectSingleNode("recordList"));
			m_list.Clerk = this;

			TryRestoreSorter(mediator, clerkConfiguration, cache);
			TryRestoreFilter(mediator, clerkConfiguration, cache);
			m_list.ListChanged += new ListChangedEventHandler(OnListChanged);
			m_list.AboutToReload += new EventHandler(m_list_AboutToReload);
			m_list.DoneReload += new EventHandler(m_list_DoneReload);

			XmlNode recordFilterListProviderNode = ToolConfiguration.GetDefaultRecordFilterListProvider(clerkConfiguration);
			bool fSetFilterMenu = false;
			if (recordFilterListProviderNode != null)
			{
				m_filterProvider = RecordFilterListProvider.Create(m_mediator, recordFilterListProviderNode);
				if (m_filterProvider != null && m_list.Filter != null)
				{
					// find any matching persisted menubar filter
					// NOTE: for now assume we can only set/persist one such menubar filter at a time.
					for (int i = 0; i < m_filterProvider.Filters.Count; ++i)
					{
						RecordFilter menuBarFilterOption = m_filterProvider.Filters[i] as RecordFilter;
						if (m_list.Filter.Contains(menuBarFilterOption))
						{
							m_activeMenuBarFilter = menuBarFilterOption;
							m_filterProvider.OnAdjustFilterSelection(m_activeMenuBarFilter);
							m_mediator.PropertyTable.SetDefault(this.CurrentFilterPropertyTableId, m_activeMenuBarFilter.id, false, PropertyTable.SettingsGroup.LocalSettings);
							fSetFilterMenu = true;
							break;
						}
					}
				}
			}
			if (!fSetFilterMenu)
			{
				OnAdjustFilterSelection(null);
			}

			// we never want to persist this value, since it is dependent upon the filter property.
			m_mediator.PropertyTable.SetPropertyPersistence(this.CurrentFilterPropertyTableId, false, PropertyTable.SettingsGroup.LocalSettings);

			//we handled the tree bar only if we are the root clerk
			if (m_clerkProvidingRootObject == null)
			{
				m_recordBarHandler = TreeBarHandler.Create(m_mediator, clerkConfiguration);//,m_flid);
				//if (m_list is PossibilityRecordList)
				//{
				//    m_recordBarHandler = TreeBarHandler.Create(m_mediator, clerkConfiguration);//,m_flid);
				//}
				//else
				//{
				//    // Don't use a RecordBarHandler for non-PossibilityRecordList. Use RecordBrowseView instead.
				//}
			}
			else
			{
				RecordClerk clerkProvidingRootObject;
				Debug.Assert(TryClerkProvidingRootObject(out clerkProvidingRootObject),
					"We expected to find clerkProvidingOwner '" + m_clerkProvidingRootObject +  "'. Possibly misspelled.");
			}

			//mediator. TraceLevel = TraceLevel.Info;

			//we do not want to be a top-level colleague, because
			//if we were, we would always receive events, for example navigation events
			//which might be intended for another RecordClerk, specifically the RecordClerk
			//being used by the currently active vector editor, browse view, etc.

			//so, instead, we let the currently active view include us as a "child" colleague.
			//NO! mediator.AddColleague(this);



			// Install this object in the PropertyTable so that others can find it.
			// NB: This *must* be done before the call to SetupDataContext,
			// or we are asking for an infinite loop, has SetupDataContext()
			// causes user interface widgets to wake up and look for this object.
			// If we have not registered the existence of this object yet in the property table,
			// those widgets will be inclined to try to create us.  Hence, the infinite loop.

			//Note that, on the downside, this means that we need to be careful
			//not to broadcast any record changes until we are actually initialize enough
			//to deal with the resulting request that will come from those widgets.

			StoreClerkInPropertyTable(clerkConfiguration);

			SetupDataContext();

		}

		private string SortNamePropertyTableId
		{
			get
			{
				CheckDisposed();
				return m_list.PropertyTableId("sortName");
			}
		}

		private string FilterPropertyTableId
		{
			get
			{
				CheckDisposed();
				return m_list.PropertyTableId("filter");
			}
		}

		private string SorterPropertyTableId
		{
			get
			{
				CheckDisposed();
				return m_list.PropertyTableId("sorter");
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="mediator"></param>
		/// <param name="clerkConfiguration"></param>
		/// <param name="cache"></param>
		/// <returns><c>true</c> if we changed or initialized a new filter,
		/// <c>false</c> if the one installed matches the one we had stored to persist.</returns>
		protected virtual bool TryRestoreFilter(Mediator mediator, XmlNode clerkConfiguration, FdoCache cache)
		{
			RecordFilter filter = null;
			string persistFilter = mediator.PropertyTable.GetStringProperty(FilterPropertyTableId, null, PropertyTable.SettingsGroup.LocalSettings);
			if (m_list.Filter != null)
			{
				// if the persisted object string of the existing filter matches the one in the property table
				// do nothing.
				string currentFilter = DynamicLoader.PersistObject(m_list.Filter, "filter");
				if (currentFilter == persistFilter)
					return false;
			}
			if (persistFilter != null)
			{
				try
				{
					filter = DynamicLoader.RestoreObject(persistFilter) as RecordFilter;
					if (filter != null)
					{
						// (LT-9515) restored filters need these set, because they can't be persisted.
						filter.Cache = cache;
						filter.StringTable = m_mediator.StringTbl;
					}
				}
				catch
				{
					filter = null; // If anything goes wrong, ignore the persisted value.
				}
			}
			if (filter == null || !filter.IsValid)
			{
				m_filterNode = ToolConfiguration.GetDefaultFilter(clerkConfiguration);
				if (m_filterNode != null)
					filter = RecordFilter.Create(cache, m_filterNode);
				else
					filter = null;
			}
			m_list.Filter = filter;
			return true;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="mediator"></param>
		/// <param name="clerkConfiguration"></param>
		/// <param name="cache"></param>
		/// <returns><c>true</c> if we changed or initialized a new sorter,
		/// <c>false</c>if the one installed matches the one we had stored to persist.</returns>
		protected virtual bool TryRestoreSorter(Mediator mediator, XmlNode clerkConfiguration, FdoCache cache)
		{
			m_sortName = mediator.PropertyTable.GetStringProperty(SortNamePropertyTableId, null, PropertyTable.SettingsGroup.LocalSettings);

			string persistSorter = mediator.PropertyTable.GetStringProperty(SorterPropertyTableId, null, PropertyTable.SettingsGroup.LocalSettings);
			if (m_list.Sorter != null)
			{
				// if the persisted object string of the existing sorter matches the one in the property table
				// do nothing
				string currentSorter = DynamicLoader.PersistObject(m_list.Sorter, "sorter");
				if (currentSorter == persistSorter)
					return false;
			}
			RecordSorter sorter = null;
			if (persistSorter != null)
			{
				try
				{
					sorter = DynamicLoader.RestoreObject(persistSorter) as RecordSorter;
				}
				catch
				{
					sorter = null; // If anything goes wrong, ignore the persisted value.
				}
			}
			if (sorter == null)
			{
				XmlNode sorterNode = ToolConfiguration.GetDefaultSorter(clerkConfiguration);
				if (sorterNode != null)
				{
					sorter = PropertyRecordSorter.Create(cache, sorterNode);
				}
			}
			// If sorter is null, allow any sorter which may have been installed during
			// record list initialization to prevail.
			if (sorter != null)
			{
				// (LT-9515) restored sorters need to set some properties that could not be persisted.
				sorter.Cache = cache;
				sorter.StringTable = m_mediator.StringTbl;
				m_list.Sorter = sorter;
			}
			return true;
		}

		/// <summary>
		/// Compares the state of the filters and sorters to persisted values in property table
		/// and re-establishes them from the property table if they have changed.
		/// </summary>
		/// <returns>true if we restored either a sorter or a filter.</returns>
		internal bool UpdateFiltersAndSortersIfNeeded()
		{
			bool fRestoredSorter = TryRestoreSorter(m_mediator, m_clerkConfiguration, Cache);
			bool fRestoredFilter = TryRestoreFilter(m_mediator, m_clerkConfiguration, Cache);
			UpdateFilterStatusBarPanel();
			UpdateSortStatusBarPanel();
			return fRestoredSorter || fRestoredFilter;
		}

		protected virtual void StoreClerkInPropertyTable(XmlNode clerkConfiguration)
		{
			string property = RecordClerk.GetCorrespondingPropertyName(ToolConfiguration.GetIdOfTool(clerkConfiguration));
			m_mediator.PropertyTable.SetProperty(property, this);
			m_mediator.PropertyTable.SetPropertyPersistence(property, false);
			m_mediator.PropertyTable.SetPropertyDispose(property, true);
		}

		/// <summary>
		/// True if our clerk is the active clerk.
		/// </summary>
		internal protected bool IsActiveClerk
		{
			get
			{
				if (m_mediator == null)
					return false;
				RecordClerk activeClerk = m_mediator.PropertyTable.GetValue("ActiveClerk") as RecordClerk;
				return activeClerk != null && activeClerk.Id == this.Id;
			}
		}

		/// <summary>
		/// True if the Clerk is being used in a Gui.
		/// </summary>
		protected bool m_fIsActiveInGui = false;
		internal protected bool IsActiveInGui
		{
			get { return m_fIsActiveInGui; }
		}

		/// <summary>
		/// determine if we're in the (given) tool
		/// </summary>
		/// <param name="desiredTool"></param>
		/// <returns></returns>
		protected bool InDesiredTool(string desiredTool)
		{
			string toolChoice = m_mediator.PropertyTable.GetStringProperty("currentContentControl", null);
			return toolChoice != null && toolChoice == desiredTool;
		}

		/// <summary>
		/// determine if we're in the (given) area
		/// </summary>
		/// <param name="desiredTool"></param>
		/// <returns></returns>
		protected bool InDesiredArea(string desiredArea)
		{
			string areaChoice = m_mediator.PropertyTable.GetStringProperty("areaChoice", null);
			return areaChoice != null && areaChoice == desiredArea;
		}

		/// <summary>
		/// Indicate whether the Clerk should suspend reloading its list until it gets OnChangeFilter (cf. TE-6493).
		/// This helps performance for links that setup filters for tools they are in the process of jumping to.
		/// When OnChangeFilter is being handled for the appropriate clerk, the setter is set to false to reset the property table
		/// property.
		/// (EricP) We could broaden this to not be so tied to OnChangeFilter, if we could suspend for other durations.
		/// </summary>
		internal bool SuspendLoadListUntilOnChangeFilter
		{
			get
			{
				string toolName = m_mediator.PropertyTable.GetStringProperty("SuspendLoadListUntilOnChangeFilter", null,
					PropertyTable.SettingsGroup.LocalSettings);
				return (!string.IsNullOrEmpty(toolName)) && InDesiredTool(toolName);
			}
			set
			{
				Debug.Assert(value == false, "This property should only be reset by setting this property to false.");
				if (value == false && SuspendLoadListUntilOnChangeFilter)
				{
					// reset this property.
					m_mediator.PropertyTable.SetProperty("SuspendLoadListUntilOnChangeFilter", "",
						PropertyTable.SettingsGroup.LocalSettings);
				}
			}
		}

		/// <summary>
		/// Indicate whether we want to suspend loading a record (or records) because we expect to handle a subsequent
		/// OnJumpToRecord message.
		/// </summary>
		public bool SuspendLoadingRecordUntilOnJumpToRecord
		{
			get
			{
				string jumpToInfo = m_mediator.PropertyTable.GetStringProperty("SuspendLoadingRecordUntilOnJumpToRecord", "",
					PropertyTable.SettingsGroup.LocalSettings);
				if (String.IsNullOrEmpty(jumpToInfo))
					return false;
				string[] jumpToParams = jumpToInfo.Split(new char[] { ',' });
				return jumpToParams.Length > 0 && IsActiveClerk && InDesiredTool(jumpToParams[0]);
			}
			set
			{
				Debug.Assert(value == false, "This property should only be reset by setting this property to false.");
				if (value == false && SuspendLoadingRecordUntilOnJumpToRecord)
				{
					// reset this property.
					m_mediator.PropertyTable.SetProperty("SuspendLoadingRecordUntilOnJumpToRecord", "",
						PropertyTable.SettingsGroup.LocalSettings);
				}

			}
		}

		/// <summary>
		/// our list's FdoCache
		/// </summary>
		protected FdoCache Cache
		{
			get { return m_list.Cache; }
		}

		/// <summary>
		/// Prevents the user from adding or removing records (e.g. semantic domain list in the rapid data entry view)
		/// </summary>
		public bool Editable
		{
			get
			{
				CheckDisposed();
				return m_editable;
			}
			set
			{
				CheckDisposed();
				m_editable = value;
			}
		}

		/// <summary>
		/// return an array of all of the objects which should
		/// 1) be queried when looking for someone to deliver a message to
		/// 2) be potential recipients of a broadcast
		/// </summary>
		/// <returns></returns>
		public IxCoreColleague[] GetMessageTargets()
		{
			CheckDisposed();

			List<IxCoreColleague> colleagues = new List<IxCoreColleague>();
			if (m_list != null && m_list is IxCoreColleague)
				colleagues.AddRange((m_list as IxCoreColleague).GetMessageTargets());
			colleagues.Add(this);
			return colleagues.ToArray();
		}

		#endregion

		#region IRecordListUpdater implementation

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Set our IRecordChangeHandler object from the argument.
		/// </summary>
		/// <remarks>Part of the IRecordListUpdater interface.</remarks>
		/// -----------------------------------------------------------------------------------
		public IRecordChangeHandler RecordChangeHandler
		{
			set
			{
				CheckDisposed();

				if (m_rch != null && m_rch is IDisposable)
				{
					// Store it, since we need to clear out the
					// data member to avoid an infinite loop in calling its Dispose method,
					// which then tries to call this setter with null.
					IDisposable gonner = (m_rch as IDisposable);
					m_rch = null;
					gonner.Dispose();
				}
				m_rch = value;
			}
		}

		/// <summary>
		/// refresh current record, by deleting and re-inserting it into our list.
		/// </summary>
		public void RefreshCurrentRecord()
		{
		   m_list.ReplaceListItem(CurrentObject.Hvo);
		}

		public void UpdateList(bool fRefreshRecord)
		{
			CheckDisposed();

			// By default, we don't force the sort
			UpdateList(fRefreshRecord, false);
		}

		internal protected bool RequestedLoadWhileSuppressed
		{
			get { return m_list.RequestedLoadWhileSuppressed; }
		}

		internal protected bool ListLoadingSuppressedNoSideEffects
		{
			get { return m_list.ListLoadingSuppressed; }
			set { m_list.SetSuppressingLoadList(value); }
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Update the list, and possibly the record referenced by our stored
		/// IRecordChangeHandler object.
		/// </summary>
		/// <remarks>Part of the IRecordListUpdater interface.</remarks>
		/// -----------------------------------------------------------------------------------
		internal void UpdateList(bool fRefreshRecord, bool forceSort)
		{
			CheckDisposed();

			if (fRefreshRecord && m_rch != null)
				m_rch.Fixup(false);		// no need to recursively update the list!
			bool fReload = forceSort || m_list.NeedToReloadList();
			if (fReload)
				m_list.ReloadList();
		}

		#endregion

		public string Id
		{
			get
			{
				CheckDisposed();

				return m_id;
			}
		}

		/// <summary>
		/// Fabricate a name for storing the list index of the currently edited object based on
		/// the database and clerk.
		/// </summary>
		internal string PersistedIndexProperty
		{
			get
			{
				CheckDisposed();

				return String.Format("{0}-Index", m_id);
			}
		}

		/// <summary>
		/// Get the thing that provides sort items (typically to the browse view).
		/// This is actually the list, but to maximize encapsulation, we only admit
		/// that it supports this one interface.
		/// </summary>
		public ISortItemProvider SortItemProvider
		{
			get
			{
				CheckDisposed();
				return m_list;
			}
		}

		private void SetupDataContext()
		{
			m_list.InitLoad();

			//NB: we need to be careful
			//not to broadcast any record changes until we are actually initialize enough
			//to deal with the resulting request that will come from those widgets.


			//if we are not dependent on some other selection, then select the first record.
			//			if (m_clerkProvidingRootObject ==null)
			//				this.OnFirstRecord(this);

			//BroadcastChange();

			UpdateOwningObject();
		}

		#region XCORE Message Handlers

		/// <summary>
		/// Called by a view (e.g. browseView) when, internally, it changes the currently selected record.
		/// </summary>
		public void ViewChangedSelectedRecord(FwObjectSelectionEventArgs e)
		{
			CheckDisposed();

			// Don't do anything if we haven't changed our selection.
			int hvoCurrent = 0;
			if (CurrentObject != null)
				hvoCurrent = CurrentObject.Hvo;
			if (e.Index >= 0 && CurrentIndex == e.Index && hvoCurrent == e.Hvo ||
				e.Index < 0 && hvoCurrent == e.Hvo)
			{
				return;
			}
			// In some cases (e.g. sorting LexEntries by Gloss), results in a list that
			// contains multiple rows referring to the same object. In that case
			// we want to try to JumpToRecord of the same index, since jumping to the hvo
			// jumps to the first instance of that object (LT-4691).
			if (e.Index >= 0)
			{
				ICmObject co = m_list.RootObjectAt(e.Index);
				// if for some reason the index doesn't match the hvo, we'll jump to the Hvo.
				Debug.Assert(e.Hvo == co.Hvo, "the index (" + e.Index + ") for selected object (" + e.Hvo +
					") does not match the object (" + co.Hvo + " in our list at that index.)");
				if (co.Hvo != e.Hvo)
					JumpToRecord(e.Hvo);
				else
					JumpToIndex(e.Index);
			}
			else if (e.Hvo > 0)
			{
				JumpToRecord(e.Hvo);
			}
		}

		public bool OnFirstRecord(object argument)
		{
			CheckDisposed();

			m_list.CurrentIndex = m_list.FirstItemIndex;
			BroadcastChange();
			return true;	//we handled this.
		}

		/// <summary>
		/// move to the next or last record in the current set of records
		/// </summary>
		/// <param name="argument"></param>
		/// <returns></returns>
		public bool OnNextRecord(object argument)
		{
			CheckDisposed();

			//nb: this may be used in situations where there is no next record, as
			//when the current record has been deleted but it was the last record.

			/*m_list.CurrentIndex = m_list.CurrentIndex >= m_list.LastItemIndex ? m_list.LastItemIndex : m_list.CurrentIndex + 1;*/
			m_list.CurrentIndex = m_list.NextItemIndex;
			BroadcastChange();
			return true;	//we handled this.
		}

		public bool OnPreviousRecord(object argument)
		{
			CheckDisposed();

			/*m_list.CurrentIndex = m_list.CurrentIndex <= m_list.FirstItemIndex ? m_list.FirstItemIndex : m_list.CurrentIndex - 1;*/
			m_list.CurrentIndex = m_list.PrevItemIndex;
			BroadcastChange();
			return true;	//we handled this.
		}

		/// <summary>
		/// Find the index of hvoTarget in m_list; or, if it does not occur, the index of a child of hvoTarget.
		/// </summary>
		/// <param name="hvoTarget"></param>
		/// <returns></returns>
		private int IndexOfObjOrChild(int hvoTarget)
		{
			int index = m_list.IndexOf(hvoTarget);
			if (index == -1)
			{
				// In case we can't find the argument in the list, see if it is an owner of anything
				// in the list. This is useful, for example, when asked to find a LexEntry in a list of senses.
				index = m_list.IndexOfChildOf(hvoTarget, (FdoCache)m_mediator.PropertyTable.GetValue("cache"));
			}
			return index;
		}

		/// <summary>
		/// display the given record
		/// </summary>
		/// <param name="argument">the hvo of the record</param>
		/// <returns></returns>
		public bool OnJumpToRecord(object argument)
		{
			CheckDisposed();

			bool fHandled = false;
			try
			{
				int hvoTarget = (int)argument;
				int index = IndexOfObjOrChild(hvoTarget);
				if (index == -1)
				{
					// See if base property is a virtual handler that knows how to add items.
					IAddItemToVirtualProperty adder =
						m_list.Cache.VwCacheDaAccessor.GetVirtualHandlerId(m_list.Flid) as IAddItemToVirtualProperty;
					if (adder != null && adder.Add(m_list.OwningObject.Hvo, hvoTarget))
						index = IndexOfObjOrChild(hvoTarget);
				}
				if (m_list.Filter != null && index == -1)
				{
					// We can get here with an irrelevant target hvo, for example by inserting a new
					// affix allomorph in an entry (see LT-4025).  So make sure we have a suitable
					// target before complaining to the user about a filter being on.
					FdoCache cache = m_list.Cache;
					IFwMetaDataCache mdc = cache.MetaDataCacheAccessor;
					uint clidList = mdc.GetDstClsId((uint)m_list.Flid);
					int clidObj = cache.GetClassOfObject(hvoTarget);

					// If (int) clidList is -1, that means it was for a virtual property and the IsSameOrSubclassOf
					// test won't be valid.
					// Enhance JohnT/CurtisH: It would be better to if virtual properties actually knew about their
					// destination class.
					bool fSuitableTarget;
					if ((int)clidList == -1)
						fSuitableTarget = true;
					else
						fSuitableTarget = cache.IsSameOrSubclassOf(clidObj, (int)clidList);

					if (fSuitableTarget)
					{
						DialogResult dr = MessageBox.Show(
							xWorksStrings.LinkTargetNotAvailableDueToFilter,
							xWorksStrings.TargetNotFound, MessageBoxButtons.YesNo);
						if (dr == DialogResult.Yes)
						{
							// We had changed from OnChangeFilter to SendMessage("RemoveFilters") to solve a filterbar
							// update issue reported in (LT-2448). However, that message only works in the context of a
							// BrowseViewer, not a document view (e.g. Dictionary) (see LT-7298). So, I've
							// tested OnChangeFilterClearAll, and it seems to solve both problems now.
							OnChangeFilterClearAll(null);
							m_activeMenuBarFilter = null;
							index = IndexOfObjOrChild(hvoTarget);
						}
						else
						{
							fHandled = true; // user wants to give up
							return fHandled;
						}
					}
				}

				if (index == -1)
				{
					// May be the item was just created by another program or tool (e.g., LT-8827)
					m_list.ReloadList();
					index = IndexOfObjOrChild(hvoTarget);
				}

				if (index == -1)
				{
					// It may be the wrong clerk, so just bail out.
					//MessageBox.Show("The list target is no longer available. It may have been deleted.",
					//	"Target not found", MessageBoxButtons.OK);
					return false;
				}
				else
				{
					JumpToIndex(index);
				}
				fHandled = true;
				return fHandled;	//we handled this.
			}
			finally
			{
				// Even if we didn't handle it, that might just be because something prevented us from
				// finding the object. But if we leave load-record suspended, a pane may never again get
				// drawn this whole session!
				this.SuspendLoadingRecordUntilOnJumpToRecord = false;
			}
		}

		public bool OnLastRecord(object argument)
		{
			CheckDisposed();

			m_list.CurrentIndex = m_list.LastItemIndex;
			BroadcastChange();
			return true;	//we handled this.
		}

		public bool OnRefresh(object argument)
		{
			CheckDisposed();

			Form window = (Form)m_mediator.PropertyTable.GetValue("window");
			using (new SIL.FieldWorks.Common.Utils.WaitCursor(window))
			{
				if (m_rch != null)
					m_rch.Fixup(false);		// no need to recursively refresh!
				m_list.ReloadList();
				return false;	//that other colleagues do a refresh, too.
			}
		}

		public bool OnExport(object argument)
		{
			CheckDisposed();

			using (ExportDialog dlg = new ExportDialog(m_mediator))
			{
				dlg.ShowDialog();
			}

			return true;	// handled
		}

		/// <summary>
		/// Receives the broadcast message "PropertyChanged"
		/// </summary>
		public virtual void OnPropertyChanged(string name)
		{
			CheckDisposed();

			switch(name)
			{
				case "ShowRecordList":
					if (IsControllingTheRecordTreeBar)//m_treeBarHandler!= null)
						m_recordBarHandler.PopulateRecordBar(m_list);
					break;
				case "SelectedTreeBarNode":
					if (!IsControllingTheRecordTreeBar) //m_treeBarHandler== null)
						break;
					TreeNode node = (TreeNode) m_mediator.PropertyTable.GetValue(name);
					if (node == null)
						return;
					int hvo = (int) node.Tag;
					if(this.CurrentObject == null || hvo != this.CurrentObject.Hvo)
						this.JumpToRecord(hvo);
					break;
				case "SelectedListBarNode":
					if (!IsControllingTheRecordTreeBar) //m_treeBarHandler== null)
						break;
					ListViewItem item = (ListViewItem) m_mediator.PropertyTable.GetValue(name);
					if (item == null)
						return;
					hvo = (int) item.Tag;
					if(this.CurrentObject == null || hvo != this.CurrentObject.Hvo)
						this.JumpToRecord(hvo);
					break;

				default:
					//this happens when the user chooses a MenuItem or sidebar item that selects a filter
					if(name == this.CurrentFilterPropertyTableId)
					{
						OnChangeFilterToCheckedListPropertyChoice();
						break;
					}

					//if we are  "dependent" on another clerk to provide the owning object of our list:
					if (m_clerkProvidingRootObject !=null)
					{
						if(name == DependentPropertyName)
						{
							UpdateOwningObjectIfNeeded();
						}
					}
					break;
			}
		}

		/// <summary>
		/// Change the list filter to the currently selected (checked) FilterList item.
		/// This selection is stored in the property table based on the name of the filter
		/// associated with the current clerk.
		/// </summary>
		private void OnChangeFilterToCheckedListPropertyChoice()
		{
			string filterName = m_mediator.PropertyTable.GetStringProperty(this.CurrentFilterPropertyTableId, "", PropertyTable.SettingsGroup.LocalSettings);
			RecordFilter addf = null;
			RecordFilter remf = null;
			NoFilters nof = new NoFilters();
			// Check for special cases.
			if (filterName == (new Filters.UncheckAll()).Name)
			{
				// we're simply unselecting all items in the list.
				// no need for further changes.
				return;
			}
			else if (filterName == nof.Name)
			{
				remf = Filter;
				m_activeMenuBarFilter = null;
			}
			else if (m_filterProvider != null)
			{
				addf = (RecordFilter)m_filterProvider.GetFilter(filterName);
				// If we have a filter defined for this filterName, then only remove the activeMenuBarFilter.
				// Otherwise remove all filters.
				if (addf != null)
					remf = m_activeMenuBarFilter;
				else
					remf = Filter;
				m_activeMenuBarFilter = addf is NullFilter ? null : addf;
			}
			OnChangeFilter(new FilterChangeEventArgs(addf, remf));
		}

		private string DependentPropertyName
		{
			get
			{
				if (m_clerkProvidingRootObject !=null)
				{
					return ClerkSelectedObjectPropertyId(m_clerkProvidingRootObject);
				}
				//don't do this, because then you die when the debugger tries to show the RecordClerk.
				//Debug.Fail("Why is this property being called when the clerk is not dependent on another clerk?");
				return null;

			}
		}

		static internal string ClerkSelectedObjectPropertyId(string clerkId)
		{
			return clerkId + "-selected";
		}

		/// <summary>
		/// Very like UpdateOwningObject, but needs to be internal, and don't want to do anything
		/// (like reloading list) if it didn't change.
		/// </summary>
		public void UpdateOwningObjectIfNeeded()
		{
			CheckDisposed();

			UpdateOwningObject(true);
		}

		private void UpdateOwningObject()
		{
			UpdateOwningObject(false);
		}

		private void UpdateOwningObject(bool fUpdateOwningObjectOnlyIfChanged)
		{
			//if we're not dependent on another clerk, then we don't ever change our owning object.
			if (m_clerkProvidingRootObject !=null)
			{
				ICmObject old = m_list.OwningObject;
				ICmObject newObj = null;
				RecordNavigationInfo rni = (RecordNavigationInfo)m_mediator.PropertyTable.GetValue(DependentPropertyName);
				if (rni != null)
					newObj = rni.Clerk.CurrentObject;
				using (ListUpdateHelper luh = new ListUpdateHelper(this))
				{
					// in general we want to actually reload the list if something as
					// radical as changing the OwningObject occurs, since many subsequent
					// events and messages depend upon this information.
					luh.TriggerPendingReloadOnDispose = true;
					luh.SkipShowRecord = rni.SkipShowRecord;
					if (!fUpdateOwningObjectOnlyIfChanged)
						m_list.OwningObject = newObj;
					if (old != newObj)
					{
						if (fUpdateOwningObjectOnlyIfChanged)
							m_list.OwningObject = newObj;
					}
				}
				if (old != newObj)
					m_mediator.SendMessage("ClerkOwningObjChanged", this);
			}
		}

		/// <summary>
		/// deletes invalidated sort items from the list. (e.g. as result from Undo/Redo).
		/// </summary>
		public void RemoveInvalidItems()
		{
			m_list.RemoveUnwantedSortItems(null);
		}

		/// <summary>
		/// deletes the given item from the list.  needed to fix LT-9230 without full reload.
		/// </summary>
		/// <param name="hvoToRemove"></param>
		public void RemoveItemsFor(int hvoToRemove)
		{
			m_list.RemoveItemsFor(hvoToRemove);
		}

		/// <summary>
		///	see if it makes sense to provide the "delete record" command now
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public bool OnDisplayDeleteRecord(object commandObject,
			ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			// Don't handle this message if you're not the primary clerk.  This allows, for
			// example, XmlBrowseRDEView.cs to handle the message instead.

			// Note from RandyR: One of these days we should probably subclass this obejct, and perhaps the record list more.
			// The "reversalEntries" clerk wants to handle the message, even though it isn't the primary clerk.
			// The m_shouldHandleDeletion member was also added, so the "reversalEntries" clerk's primary clerk
			// would not handle the message, and delete an entire reversal index.
			if (ShouldNotHandleDeletionMessage)
				return false;

			display.Enabled = m_fAllowDeletions && m_list.CurrentObject != null;
			return true;//we handled this, no need to ask anyone else.
		}

		private bool ShouldNotHandleDeletionMessage
		{
			get { return Id != "reversalEntries" && (!Editable || !IsPrimaryClerk || !m_shouldHandleDeletion); }
		}

		public bool OnDeleteRecord(object commandObject)
		{
			CheckDisposed();

			// Don't handle this message if you're not the primary clerk.  This allows, for
			// example, XmlBrowseRDEView.cs to handle the message instead.

			// Note from RandyR: One of these days we should probably subclass this obejct, and perhaps the record list more.
			// The "reversalEntries" clerk wants to handle the message, even though it isn't the primary clerk.
			// The m_shouldHandleDeletion member was also added, so the "reversalEntries" clerk's primary clerk
			// would not handle the message, and delete an entire reversal index.
			if (ShouldNotHandleDeletionMessage)
				return false;

			// It may be null:
			// 1. if the objects are bing deleted using the keys,
			// 2. the last one has been deleted, and
			// 3. the user keeps pressing the del key.
			// It looks like the command is not being disabled at all or fast enough.
			if (CurrentObject == null)
				return true;

			// Don't allow an object to be deleted if it shouldn't be deleted.
			if (CurrentObject.ValidateOkToDelete() == false)
				return true;

			//when we are doing an automated test, we don't know how to click the "yes" button, so
			//look into the property table to see if there is a property controlling what we should do.
			bool doingAutomatedTest = m_mediator.PropertyTable.GetBoolProperty("DoingAutomatedTest", false);

			using (ConfirmDeleteObjectDlg dlg = new ConfirmDeleteObjectDlg())
			{
				using (CmObjectUi uiObj = CmObjectUi.MakeUi(CurrentObject))
				{
					dlg.SetDlgInfo(uiObj, Cache, m_mediator);
				}
				Form window = (Form)m_mediator.PropertyTable.GetValue("window");
				if (doingAutomatedTest ||
					DialogResult.Yes == dlg.ShowDialog(window))
				{
					window.Cursor = Cursors.WaitCursor;
					using (ProgressState state = FwXWindow.CreatePredictiveProgressState(m_mediator, "Delete record"))
					{
						state.SetMilestone(xWorksStrings.DeletingTheObject);
						state.Breath();
						// We will certainly switch records, but we're going to suppress the usual Save after we
						// switch, so the user can at least Undo one level, the actual deletion. But Undoing
						// that may not get us back to the current record, so we'd better not allow anything
						// that's already on the stack to be undone.
						SaveOnChangeRecord();
						m_suppressSaveOnChangeRecord = true;
						try
						{
							using (UndoRedoCommandHelper undoRedoTask = new UndoRedoCommandHelper(m_list.Cache, commandObject as Command))
							{
								m_list.DeleteCurrentObject(state);
							}
							state.SetMilestone(xWorksStrings.UpdatingTheDisplay);
							state.Breath();
							BroadcastChange();
							state.Breath();
						}
						finally
						{
							m_suppressSaveOnChangeRecord = false;
						}
					}
					window.Cursor = Cursors.Default;
				}
			}
			return true; //we handled this, no need to ask anyone else.
		}

		/// <summary>
		/// Return true if some activity is underway that makes it a BAD idea to insert or delete things
		/// from the list. For example, IText should not insert an object because the list is empty
		/// WHILE we are processing the delete object.
		/// </summary>
		public bool ShouldNotModifyList
		{
			get
			{
				CheckDisposed();
				return m_list.ShouldNotModifyList;
			}
		}

		/// <summary>
		/// Used to suppress Reloading our list multiple times until we're finished with PropChanges.
		/// Also used (e.g., in Change spelling dialog) when we do NOT want the contents of the list
		/// to change owing to changed filter properties.
		/// </summary>
		public bool ListLoadingSuppressed
		{
			get { return m_list.ListLoadingSuppressed; }
			set { m_list.ListLoadingSuppressed = value; }
		}


		/// <summary>
		/// This may be set to suppress automatic reloading of the list while we are potentially making
		/// more than one change to the list.
		/// </summary>
		public bool ListModificationInProgress
		{
			get
			{
				CheckDisposed();
				return m_list.ListModificationInProgress;
			}
			set
			{
				CheckDisposed();
				m_list.ListModificationInProgress = value;
			}
		}

		/// <summary>
		/// This method is called by views that display a single record to cause a Save when switching records.
		/// The purpose is to allow the special Save to be suppressed in certain cases, such as Delete record
		/// (and perhaps eventually create record).
		/// </summary>
		public void SaveOnChangeRecord()
		{
			CheckDisposed();

			FdoCache cache = m_list.Cache;
			if ((!m_suppressSaveOnChangeRecord) && cache != null && !cache.DatabaseAccessor.IsTransactionOpen()
				&& !FwXWindow.InUndoRedo)
			{
				cache.Save();
			}
		}

		/// <summary>
		/// When deleting and inserting objects, we don't want the auto-save AFTER we switch records
		/// (during the Broadcast of RecordNavigation). We achieve this by broadcasting the
		/// RecordNavigation message with an argument that is a RecordNavigationInfo object with
		/// ShouldSaveOnChangeRecord set to false (because this propery is true at the time
		/// BroadcastChange is called).
		/// We use it again in OnRecordNavigation handlers, which set it while calling SendRecord(),
		/// the much-overridden method that eventually does the Save. This controls the actual
		/// SaveOnChangeRecord method.
		/// </summary>
		public bool SuppressSaveOnChangeRecord
		{
			get
			{
				CheckDisposed();
				return m_suppressSaveOnChangeRecord;
			}
			set
			{
				CheckDisposed();
				m_suppressSaveOnChangeRecord = value;
			}
		}

		/// <summary>
		/// returns the Clerk that governs the OwningObject of our clerk.
		/// </summary>
		/// <returns></returns>
		internal RecordClerk ParentClerk()
		{
			RecordClerk parentClerk;
			TryClerkProvidingRootObject(out parentClerk);
			return parentClerk;
		}

		/// <summary>
		/// Some user actions (e.g. editing) should not result in record navigation
		/// because it may cause the editing pane to disappear
		/// (thus losing the user's place in editing). This is used by ListUpdateHelper
		/// to skip record navigations while such user actions are taking place.
		/// </summary>
		internal bool SkipShowRecord
		{
			get
			{
				CheckDisposed();
				if (m_skipShowRecord)
					return true;
				// if this Clerk is dependent upon a ParentClerk then
				// inherit its state.
				RecordClerk parentClerk = ParentClerk();
				return parentClerk != null ? parentClerk.SkipShowRecord : false;
			}
			set
			{
				CheckDisposed();
				m_skipShowRecord = value;
			}
		}

		private string CurrentFilterPropertyTableId
		{
			get { return "currentFilterForRecordClerk_"+this.Id; }
		}

		/// <summary>
		/// this is called when XCore wants to display something that relies on the list with the id "FiltersList"
		/// </summary>
		/// <param name="parameters"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public bool OnDisplayFiltersList(object parameters, ref UIListDisplayProperties display)
		{
			CheckDisposed();

			//set the filters list property to the one that we are monitoring
			display.PropertyName = this.CurrentFilterPropertyTableId;

			display.List.Clear();
			// Add an item for clearing all filters.
			AddFilterChoice(new NoFilters(), display);
			if (m_filterProvider!= null)
			{
				foreach(RecordFilter filter in m_filterProvider.Filters)
				{
					AddFilterChoice(filter, display);
				}
			}
			return true;//we handled this, no need to ask anyone else.
		}

		private void AddFilterChoice(RecordFilter filter, UIListDisplayProperties display)
		{
			string value = filter.id;
			string imageName = filter.imageName;
			XmlNode controlElement = null;//since we are storing actual, instantiated filter objects, there's no point and also keeping their configuration information separately. Any configuration information they had would have already been sucked in when the filter was created.
			display.List.Add(filter.Name, value, imageName, controlElement);
		}

		#endregion // XCORE Message Handlers

		/// <summary>
		/// update the status bar, selected node of the tree bar, etc. and broadcast record navigation
		/// </summary>
		public void SelectedRecordChanged()
		{
			SelectedRecordChanged(false);
		}

		/// <summary>
		/// update the status bar, selected node of the tree bar, etc.
		/// </summary>
		public void SelectedRecordChanged(bool fSkipRecordNavigation)
		{
			CheckDisposed();

			ICmObject currentObj = m_list.CurrentObject;
			if (currentObj != null && !currentObj.IsValidObject())
			{
				m_list.ReloadList(); // clean everything up
			}
			bool fIgnore = m_mediator.PropertyTable.GetBoolProperty("IgnoreStatusPanel", false);
			if (fIgnore)
				return;
			if (IsControllingTheRecordTreeBar)
			{
				// JohnT: if we're not controlling the record list, we probably have no business trying to
				// control the status bar. But we may need a separate control over this.
				// Note that it can be definitely wrong to update it; this Clerk may not have anything
				// to do with the current window contents.
				string s;
				int len = m_list.SortedObjects.Count;
				if (len > 0)
					s = (1 + m_list.CurrentIndex).ToString() + @"/" + len.ToString();
				else
					s = m_mediator.StringTbl.GetString("No Records", "Misc");// FwXApp.XWorksResources.GetString("stidNoRecords");
				ResetStatusBarPanel("StatusPanelRecordNumber", s);
				string msg = "";
				if (currentObj != null)
				{
					// deleted objects don't have a cache (and other properties) so it was crashing.  LT-3160, LT-3121,...
					if (!currentObj.IsValidObject())
					{
						msg = xWorksStrings.ADeletedObject;
					}
					else
					{
						using (CmObjectUi uiObj = CmObjectUi.MakeUi(currentObj))
						{
							msg = uiObj.ToStatusBar();
						}
					}
				}
				ResetStatusBarPanel("StatusPanelMessage", msg);
			}

			//this is used by DependantRecordLists
			RecordNavigationInfo rni = new RecordNavigationInfo(this, m_suppressSaveOnChangeRecord || FwXWindow.InUndoRedo, SkipShowRecord);
			m_mediator.PropertyTable.SetProperty(RecordClerk.ClerkSelectedObjectPropertyId(this.Id), rni);
			m_mediator.PropertyTable.SetPropertyPersistence(RecordClerk.ClerkSelectedObjectPropertyId(this.Id), false);

			// save the selected record index.
			string propName = this.PersistedIndexProperty;
			m_mediator.PropertyTable.SetProperty(propName, this.CurrentIndex, PropertyTable.SettingsGroup.LocalSettings);
			m_mediator.PropertyTable.SetPropertyPersistence(propName, true, PropertyTable.SettingsGroup.LocalSettings);

			if (IsControllingTheRecordTreeBar)
			{
				if (m_recordBarHandler != null)
					m_recordBarHandler.UpdateSelection(currentObj);
				//used to enable certain dialogs, such as the "change entry type dialog"
				m_mediator.PropertyTable.SetProperty("ActiveClerkSelectedObject", currentObj);
				m_mediator.PropertyTable.SetPropertyPersistence("ActiveClerkSelectedObject", false);
			}

			// We want an auto-save when we process the change record UNLESS we are deleting or inserting an object,
			// or performing an Undo/Redo.
			// Note: Broadcasting "OnRecordNavigation" even if a selection doesn't change allows the browse view to
			// scroll to the right index if it hasn't already done so.
			if (!fSkipRecordNavigation)
			{
				m_mediator.BroadcastMessage("RecordNavigation", rni);
			}
		}

		private void ResetStatusBarPanel(string panel, string msg)
		{
			m_mediator.PropertyTable.SetProperty(panel, msg);
			m_mediator.PropertyTable.SetPropertyPersistence(panel, false);
		}

		/// <summary>
		/// Generally only one clerk should respond to record navigation in the user interface.
		/// The "primary" clerk is the one that should respond to record navigation.
		/// </summary>
		/// <returns>true iff this clerk should respond to record navigation</returns>
		public bool IsPrimaryClerk
		{
			get
			{
				CheckDisposed();
				return (this.m_clerkProvidingRootObject == null);
			}
		}

		internal bool TryClerkProvidingRootObject(out RecordClerk clerkProvidingRootObject)
		{
			clerkProvidingRootObject = null;
			if (IsPrimaryClerk)
				return false;
			clerkProvidingRootObject = RecordClerk.FindClerk(m_mediator, m_clerkProvidingRootObject);
			return clerkProvidingRootObject != null;
		}

		/// <summary>
		/// finds an existing RecordClerk by the given id.
		/// </summary>
		/// <param name="mediator"></param>
		/// <param name="id"></param>
		/// <returns>null if couldn't find an existing clerk.</returns>
		public static RecordClerk FindClerk(Mediator mediator, string id)
		{
			string name = RecordClerk.GetCorrespondingPropertyName(id);
			return (RecordClerk)mediator.PropertyTable.GetValue(name);
		}

		/// <summary>
		/// tells whether this RecordClerk object should be updating the record tree bar when its list changes
		/// </summary>
		/// <remarks>
		/// A RecordClerk can be in one of three states with respect to control of the
		/// user interface. It can be the RecordClerk being used to control the tree bar,
		/// a can be a dependent clerk which does not have any relationship to the tree bar,
		/// or it can be in the background altogether, because the tool(s) it is associated with are not
		/// currently active.
		///
		/// This property was added when we ran into the following problem: when a user added a new entry
		/// from the interlinear text section, that generated a notification that a new entry had been added to
		/// the list of entries. The RecordClerk that was responsible for that list then woke up, and tried
		/// to update the tree bar to show this new item. But of course, the tree bottle was currently showing the list
		/// of texts, so this was a *bad idea*. Hence this new property, which relies on a new
		/// property in the property table to say which RecordClerk claims to be the current master
		/// of the tree bar.
		/// </remarks>
		virtual public bool IsControllingTheRecordTreeBar
		{
			set
			{
				CheckDisposed();

				Debug.Assert(value==true);
				RecordClerk oldActiveClerk = m_mediator.PropertyTable.GetValue("ActiveClerk") as RecordClerk;
				if (oldActiveClerk != this)
				{
					if (oldActiveClerk != null)
						oldActiveClerk.BecomeInactive();
					m_mediator.PropertyTable.SetProperty("ActiveClerk", this);
					m_mediator.PropertyTable.SetPropertyPersistence("ActiveClerk", false);
					// We are adding this property so that EntryDlgListener can get access to the owning object
					// without first getting a RecordClerk, since getting a RecordClerk at that level causes a
					// circular dependency in compilation.
					m_mediator.PropertyTable.SetProperty("ActiveClerkOwningObject", OwningObject);
					m_mediator.PropertyTable.SetPropertyPersistence("ActiveClerkOwningObject", false);
				}
				ReloadIfNeeded();
			}
			get
			{
				CheckDisposed();

				return   (m_recordBarHandler!= null) &&	m_mediator.PropertyTable.GetValue("ActiveClerk") == this;
			}
		}

		private void ReloadIfNeeded()
		{
			if (OwningObject != null && !m_list.Cache.MainCacheAccessor.get_IsPropInCache(OwningObject.Hvo, m_list.VirtualFlid,
							(int)CellarModuleDefns.kcptReferenceSequence, 0))
			{
				// A full refresh wipes out all caches as of 26 October 2005,
				// so we have to reload it. This fixes the sextuplets:
				// LT-5393, LT-6102, LT-6154, LT-6084, LT-6059, LT-6062.
				m_list.ReloadList();
			}
		}

		/// <summary>
		/// Tell the RecordClerk that it may now be the new master of the tree bar, if it is not a dependent clerk.
		/// Use DeactivatedGui to tell RecordClerk that it's not currently being used in a Gui.
		/// </summary>
		virtual public void ActivateUI(bool useRecordTreeBar)
		{
			m_fIsActiveInGui = true;
			CheckDisposed();

			if (m_recordBarHandler != null)
			{
				IsControllingTheRecordTreeBar = true;
				if (useRecordTreeBar && s_lastClerkToLoadTreeBar != this)//optimization
				{
					s_lastClerkToLoadTreeBar = this;
					m_recordBarHandler.PopulateRecordBar(m_list);
				}
			}
			else
			{
				ReloadIfNeeded();
			}
			UpdateFilterStatusBarPanel();
			UpdateSortStatusBarPanel();
		}

		/// <summary>
		/// Tell RecordClerk that we're not currently being used in a Gui.
		/// </summary>
		virtual public void BecomeInactive()
		{
			m_fIsActiveInGui = false;
			if (m_recordBarHandler != null)
				m_recordBarHandler.ReleaseRecordBar();
		}

		/// <summary>
		/// If the record bar is visible and needs to be repopulated, do it.
		/// </summary>
		public void UpdateRecordTreeBarIfNeeded()
		{
			CheckDisposed();

			if (IsControllingTheRecordTreeBar) // m_treeBarHandler!= null)
				m_recordBarHandler.PopulateRecordBarIfNeeded(m_list);
		}

		/// <summary>
		/// update the contents of the tree bar and anything else that should change when,
		/// for example, the filter or sort order changes.
		/// </summary>
		protected void OnListChanged(object src, ListChangedEventArgs arguments)
		{
			if (IsControllingTheRecordTreeBar) // m_treeBarHandler!= null)
			{
				if (arguments.Actions == ListChangedEventArgs.ListChangedActions.UpdateListItemName)
				{
					// ******************************************************************************
					// In the case where there are no other items and the Current object isn't valid,
					// then just don't do anything.  LT-5849.
					// A more robust solution would be to have in our design a way to produce
					// a 'defered' prop changed so that the current actions can finish before
					// others are notified of the change (which is often incomplete at that time).
					// The stack for this issue showed the RecordList and RecordClerk being
					// re-entered while they were deleting an object in a previous stack frame.
					// This is not the only case where this has been noted, but a solution has
					// not yet been thought of.
					// In the meantime, this fixed the crash .. <sigh> but doesn't help at all
					// for the other cases where this can happen.
					// ******************************************************************************
					if (m_recordBarHandler is TreeBarHandler && m_list.CurrentObject != null &&
						(m_list.CurrentObject.Cache != null || m_list.SortedObjects.Count != 1))
					{
						// all we need to do is replace the currently selected item in the tree.
						m_recordBarHandler.ReloadItem(m_list.CurrentObject);
					}
				}
				else
				{
					m_recordBarHandler.PopulateRecordBar(m_list);
				}
			}

			if (arguments.Actions == ListChangedEventArgs.ListChangedActions.SkipRecordNavigation ||
				arguments.Actions == ListChangedEventArgs.ListChangedActions.UpdateListItemName)
			{
				SelectedRecordChanged(true);
			}
			else if (arguments.Actions == ListChangedEventArgs.ListChangedActions.SuppressSaveOnChangeRecord)
			{
				bool oldSuppressSaveChangeOnRecord = SuppressSaveOnChangeRecord;
				SuppressSaveOnChangeRecord = true;
				try
				{
					BroadcastChange();
				}
				finally
				{
					SuppressSaveOnChangeRecord = oldSuppressSaveChangeOnRecord;
				}
			}
			else if (arguments.Actions == ListChangedEventArgs.ListChangedActions.Normal)
			{
				BroadcastChange();
			}
			else
			{
				throw new NotImplementedException("An enum choice for ListChangedEventArgs was selected that OnListChanged is not aware of.");
			}
		}

		/// <summary>
		/// update the contents of the filter list.
		/// </summary>
		protected void OnFilterListChanged(object argument)
		{
			//review: I (JH) couldn't find where/if this method is called.
			if (m_filterProvider != null)
			{
				m_filterProvider.ReLoad();
			}
		}
		public ICmObject CurrentObject
		{
			get
			{
				CheckDisposed();

				return m_list.CurrentObject;
			}
		}
		public int CurrentIndex
		{
			get
			{
				CheckDisposed();

				return m_list.CurrentIndex;
			}
		}
		public bool HasEmptyList
		{
			get
			{
				CheckDisposed();

				return m_list.SortedObjects.Count == 0;
			}
		}
		public int ListSize
		{
			get
			{
				CheckDisposed();

				return m_list.SortedObjects.Count;
			}
		}
		public int VirtualFlid
		{
			get
			{
				CheckDisposed();
				return m_list.VirtualFlid;
			}
		}

		bool m_fReloadingDueToMissingObject = false;
		private void BroadcastChange()
		{
			if (CurrentObject != null && !CurrentObject.IsValidObject())
			{
				MessageBox.Show(xWorksStrings.SelectedObjectHasBeenDeleted,
					xWorksStrings.DeletedObjectDetected,
					MessageBoxButtons.OK, MessageBoxIcon.Warning);
				m_fReloadingDueToMissingObject = true;
				try
				{
					int idx = m_list.CurrentIndex;
					int cobj = this.ListSize;
					if (cobj == 0)
					{
						m_list.CurrentIndex = -1;
					}
					else
					{
						m_list.CurrentIndex = FindClosestValidIndex(idx, cobj);
					}
					m_mediator.SendMessage("StopParser", null);	// stop parser if it's running.
					if (!FwApp.App.SyncFromDb())
					{
						// if a full refresh was not performed by the DB sync, make sure that the record list has
						// been properly refreshed
						RefreshAfterInvalidObject();
					}
				}
				finally
				{
					m_fReloadingDueToMissingObject = false;
				}
				return; // typically leads to another call to this.
			}
			SelectedRecordChanged();
		}

		/// <summary>
		/// Handles refreshing the record list after an object was deleted.
		/// </summary>
		/// <remarks>This should be overriden to perform more efficient refreshing of the record list display</remarks>
		protected virtual void RefreshAfterInvalidObject()
		{
			// to be safe we just do a full refresh
			FwApp.App.RefreshAllViews();
		}

		private int FindClosestValidIndex(int idx, int cobj)
		{
			for (int i = idx + 1; i < cobj; ++i)
			{
				ManyOnePathSortItem item = m_list.SortedObjects[i] as ManyOnePathSortItem;
				if (!Cache.IsValidObject(item.KeyObject))
					continue;
				bool fOk = true;
				for (int j = 0; fOk && j < item.PathLength; j++)
					fOk = Cache.IsValidObject(item.PathObject(j));
				if (fOk)
					return i;
			}
			for (int i = idx - 1; i >= 0; --i)
			{
				ManyOnePathSortItem item = m_list.SortedObjects[i] as ManyOnePathSortItem;
				if (!Cache.IsValidObject(item.KeyObject))
					continue;
				bool fOk = true;
				for (int j = 0; fOk && j < item.PathLength; j++)
					fOk = Cache.IsValidObject(item.PathObject(j));
				if (fOk)
					return i;
			}
			return -1;
		}

		/// <summary>
		/// true if the list is non empty and we are on the last record.
		/// </summary>
		public bool OnLast
		{
			get
			{
				CheckDisposed();
				return m_list.OnLast;
			}
		}

		/// <summary>
		/// Jump to the specified object.
		/// </summary>
		/// <param name="jumpToHvo"></param>
		public void JumpToRecord(int jumpToHvo)
		{
			CheckDisposed();

			JumpToIndex(m_list.IndexOf(jumpToHvo));
		}

		/// <summary>
		/// Jump to the specified index in the list.
		/// </summary>
		/// <param name="index"></param>
		public void JumpToIndex(int index)
		{
			CheckDisposed();

			try
			{
				m_list.CurrentIndex = index;
			}
			catch(IndexOutOfRangeException error)
			{
				throw new IndexOutOfRangeException("The RecordClerk tried to jump to a record which is not in the current active set of records.", error);
			}
			BroadcastChange();
		}

		#region Insertion

		/// <summary>
		/// Influence the display of a particular *command* (which we don't know the name of)
		/// by giving an opinion on whether we are prepared to handle its corresponding "InsertItemInVector"
		/// *message*.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public bool OnDisplayInsertItemInVector(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			Command command = (Command)commandObject;
			string className = XmlUtils.GetManditoryAttributeValue(command.Parameters[0], "className");

			string restrictToClerkID = XmlUtils.GetOptionalAttributeValue(command.Parameters[0], "restrictToClerkID");
			if (restrictToClerkID != null && restrictToClerkID != Id)
				return display.Enabled = false;

			string restrictFromClerkID = XmlUtils.GetOptionalAttributeValue(command.Parameters[0], "restrictFromClerkID");
			if (restrictFromClerkID != null && restrictFromClerkID.Contains(Id))
				return display.Enabled = false;

			if (Editable && m_list.CanInsertClass(className))
			{
				m_list.AdjustInsertCommandName(command, display);
				display.Enabled = true;
			}
			else
			{
				display.Enabled = false;
			}

			return display.Enabled;
		}

		/// <summary>
		/// this is triggered by any command whose message attribute is "InsertItemInVector"
		/// </summary>
		/// <param name="argument"></param>
		/// <returns>true if successful (the class is known)</returns>
		public bool OnInsertItemInVector(object argument)
		{
			CheckDisposed();

			if(!Editable)
				return false;
			// We will certainly switch records, but we're going to suppress the usual Save after we
			// switch, so the user can at least Undo one level, the actual insertion. But Undoing
			// that may not get us back to the current record, so we'd better not allow anything
			// that's already on the stack to be undone.
			// Enhance JohnT: if a dialog is brought up, the user could cancel, in which case,
			// we don't really need to throw away the Undo stack.
			SaveOnChangeRecord();

			//if there is a listener who wants to do bring up a dialog box rather than just creating a new item,
			//give them a chance.
			m_suppressSaveOnChangeRecord = true;
			try
			{
				if (m_mediator.SendMessage("DialogInsertItemInVector", argument))
					return true;
			}
			finally
			{
				m_suppressSaveOnChangeRecord = false;
			}

			Command command = (Command)argument;
			string className = "";
			try
			{
				className = XmlUtils.GetManditoryAttributeValue(command.Parameters[0], "className");
			}
			catch (ApplicationException)
			{
				throw new ConfigurationException("Could not get the necessary parameter information from this command",
					command.ConfigurationNode);
			}
			try
			{
				if (!m_list.CanInsertClass(className))
					return false;
				using (UndoRedoCommandHelper undoRedoTask = new UndoRedoCommandHelper(m_list.Cache, argument as Command))
				{
					return InsertItemInVector(className);
				}
			}
			// This doesn't make sense, we return false if we can't add that class here, unless
			// something worse went wrong.
			//catch (InvalidEnumArgumentException)
			//{
			//    throw new ConfigurationException("The class name specified in this command cannot be added here.",
			//        command.ConfigurationNode);
			//}
			catch (ApplicationException ae)
			{
				throw new ApplicationException("Could not insert the item requested by the command " + command.ConfigurationNode, ae);
			}
		}

		/// <summary>
		/// Insert a new item in the vector with the specified class name.
		/// </summary>
		/// <param name="className"></param>
		/// <returns>true if successful (the class is known)</returns>
		public bool InsertItemInVector(string className)
		{
			CheckDisposed();

			// We will certainly switch records, but we're going to suppress the usual Save after we
			// switch, so the user can at least Undo one level, the actual insertion. But Undoing
			// that may not get us back to the current record, so we'd better not allow anything
			// that's already on the stack to be undone.
			SaveOnChangeRecord();
			m_suppressSaveOnChangeRecord = true;
			bool result;
			try
			{
				result = m_list.CreateAndInsert(className);
				BroadcastChange();
			}
			finally
			{
				m_suppressSaveOnChangeRecord = false;
			}
			// If a data entry view is active, make it focus (typically on the new object).
			m_mediator.BroadcastMessage("FocusFirstPossibleSlice", null);
			return result;

		}
		#endregion

		/// <summary>
		///	see if it makes sense to provide the "next record" command now
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public bool OnDisplayNextRecord(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			/*int nextIndex = m_list.CurrentIndex >= m_list.LastItemIndex ? m_list.LastItemIndex : m_list.CurrentIndex + 1;*/
			int nextIndex = m_list.NextItemIndex;
			display.Enabled = nextIndex != -1 && nextIndex != m_list.CurrentIndex;
			return true;//we handled this, no need to ask anyone else.
		}

		/// <summary>
		///	see if it makes sense to provide the "previous record" command now
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public bool OnDisplayPreviousRecord(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			/*int prevIndex = m_list.CurrentIndex <= m_list.FirstItemIndex ? m_list.FirstItemIndex : m_list.CurrentIndex - 1;*/
			int prevIndex = m_list.PrevItemIndex;
			display.Enabled = prevIndex != -1 && prevIndex != m_list.CurrentIndex;
			return true;//we handled this, no need to ask anyone else.
		}

		/// <summary>
		///	see if it makes sense to provide the "next record" command now
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public bool OnDisplayLastRecord(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			int lastIndex = m_list.LastItemIndex;
			display.Enabled =  lastIndex != -1 && lastIndex != m_list.CurrentIndex;
			return true;//we handled this, no need to ask anyone else.
		}

		/// <summary>
		///	see if it makes sense to provide the "previous record" command now
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public bool OnDisplayFirstRecord(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			int firstIndex = m_list.FirstItemIndex;
			display.Enabled = firstIndex != -1 && firstIndex != m_list.CurrentIndex;
			return true;//we handled this, no need to ask anyone else.
		}

		/// <summary>
		/// the object which owns the elements which make up this list
		/// </summary>
		public ICmObject OwningObject
		{
			set
			{
				CheckDisposed();
				m_list.OwningObject = value;
			}
			get
			{
				CheckDisposed();
				return m_list.OwningObject;
			}
		}

		/// <summary>
		///
		/// </summary>
		internal int OwningFlid
		{
			get
			{
				CheckDisposed();
				return m_list.Flid;
			}
		}

		public static string GetCorrespondingPropertyName(string vectorName)
		{
			return "RecordClerk-" + vectorName;
		}

		/// <summary>
		/// If a filter becomes invalid, it has to be reset somehow.  This resets it to the default filter
		/// for this clerk (possibly null).
		/// </summary>
		internal void ResetFilterToDefault()
		{
			RecordFilter defaultFilter = null;
			if (m_filterNode != null)
				defaultFilter = RecordFilter.Create(Cache, m_filterNode);
			OnChangeFilter(new FilterChangeEventArgs(defaultFilter, m_list.Filter));
		}

		public void OnChangeFilter(FilterChangeEventArgs args)
		{
			CheckDisposed();

			Form window = (Form)m_mediator.PropertyTable.GetValue("window");
			System.Windows.Forms.Cursor oldCursor = window.Cursor;
			window.Cursor = System.Windows.Forms.Cursors.WaitCursor;
			Logger.WriteEvent("Changing filter.");
			// if our clerk is in the state of suspending loading the list, reset it now.
			if (SuspendLoadListUntilOnChangeFilter)
				SuspendLoadListUntilOnChangeFilter = false;
			m_list.OnChangeFilter(args);
			// Remember the active filter for this list.
			string persistFilter = DynamicLoader.PersistObject(Filter, "filter");
			m_mediator.PropertyTable.SetProperty(FilterPropertyTableId, persistFilter, PropertyTable.SettingsGroup.LocalSettings);
			// adjust menu bar items according to current state of Filter, where needed.
			m_mediator.BroadcastMessage("AdjustFilterSelection", Filter);
			UpdateFilterStatusBarPanel();
			if (m_list.Filter != null)
				Logger.WriteEvent("Filter changed: "+m_list.Filter.ToString());
			else
				Logger.WriteEvent("Filter changed: (no filter)");
			// notify clients of this change.
			if (FilterChangedByClerk != null)
				FilterChangedByClerk(this, args);
			window.Cursor = oldCursor;
		}

		/// <summary>
		/// Make Filters menuBar item selection adjustments as necessary.
		/// </summary>
		/// <param name="argument"></param>
		/// <returns></returns>
		public bool OnAdjustFilterSelection(object argument)
		{
			CheckDisposed();

			// NOTE: ListPropertyChoice compares its Value to its parent (ChoiceGroup).SinglePropertyValue
			// to determine whether or not it is the Checked item in the list.
			// Is there any way we could do this in a less kludgy/backdoor sort of way?

			//If we have a filter selected, make sure "No Filter" is not still selected in the menu or toolbar.
			if (m_activeMenuBarFilter == null && Filter != null)
			{
				// Resetting the table property value to a bogus value will effectively uncheck this item.
				m_mediator.PropertyTable.SetProperty(this.CurrentFilterPropertyTableId, (new Filters.UncheckAll()).Name, false, PropertyTable.SettingsGroup.LocalSettings);
			}
				// if no filter is set, then we always want the "No Filter" item selected.
			else if (Filter == null)
			{
				// Resetting the table property value to NoFilters checks this item.
				m_mediator.PropertyTable.SetProperty(this.CurrentFilterPropertyTableId, (new NoFilters()).Name, false, PropertyTable.SettingsGroup.LocalSettings);
			}
			// allow others to process this.
			return false;
		}

		/// <summary>
		/// Get/set a progress reporter (encapsulated in an IAdvInd4 interface).  This is used
		/// only by the list.  (at least when first implemented)
		/// </summary>
		public IAdvInd4 ProgressReporter
		{
			get
			{
				CheckDisposed();
				if (m_list != null)
					return m_list.ProgressReporter;
				else
					return null;
			}
			set
			{
				CheckDisposed();
				if (m_list != null)
					m_list.ProgressReporter = value;
			}
		}


		private void UpdateFilterStatusBarPanel()
		{
			if (!this.IsControllingTheRecordTreeBar)
				return; // none of our business!
			bool fIgnore = m_mediator.PropertyTable.GetBoolProperty("IgnoreStatusPanel", false);
			if (fIgnore)
			{
				// Set the value so that it can be picked up by the dialog (or whoever).
				if (m_list.Filter == null || !m_list.Filter.IsUserVisible)
					ResetStatusBarPanel("DialogFilterStatus", String.Empty);
				else
					ResetStatusBarPanel("DialogFilterStatus", xWorksStrings.Filtered);
				return;
			}
			StatusBarTextBox b = m_mediator.PropertyTable.GetValue("Filter") as StatusBarTextBox;
			if(b == null) //Other xworks apps may not have this panel
				return;
			if (m_list.Filter == null || !m_list.Filter.IsUserVisible)
			{
				b.BackBrush = System.Drawing.Brushes.Transparent;
				b.TextForReal = "";
			}
			else
			{
				b.BackBrush = System.Drawing.Brushes.Yellow;
				b.TextForReal = xWorksStrings.Filtered;
			}
		}

		private void UpdateSortStatusBarPanel()
		{
			if (!this.IsControllingTheRecordTreeBar)
				return; // none of our business!
			bool fIgnore = m_mediator.PropertyTable.GetBoolProperty("IgnoreStatusPanel", false);
			if (fIgnore)
				return;

			StatusBarTextBox b = m_mediator.PropertyTable.GetValue("Sort") as StatusBarTextBox;
			if (b == null) //Other xworks apps may not have this panel
				return;

			if (m_list.Sorter == null || m_sortName == null
				|| (m_isDefaultSort && ToolConfiguration.GetDefaultSorter(m_clerkConfiguration) != null))
			{
				b.BackBrush = System.Drawing.Brushes.Transparent;
				b.TextForReal = "";
			}
			else
			{
				b.BackBrush = System.Drawing.Brushes.Lime;
				b.TextForReal = string.Format(xWorksStrings.SortedBy, m_sortName);
			}
		}

		public bool OnDisplayChangeFilterClearAll(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			if (Filter != null && Filter.IsUserVisible)
			{
				display.Visible = true;
				display.Enabled = true;
			}
			else
			{
				display.Enabled = false;
			}
			return true;//we handled this, no need to ask anyone else.
		}

		public void OnChangeFilterClearAll(object commandObject)
		{
			CheckDisposed();

			this.OnChangeFilter(new FilterChangeEventArgs(null, Filter));
		}

		/// <summary>
		/// Called when the sorter changes. A name indicating what is being sorted is
		/// passed along with the record sorter.
		/// </summary>
		/// <param name="sorter">The sorter.</param>
		/// <param name="sortName">The sort name.</param>
		/// <param name="isDefaultSort"><c>true</c> if default sorting is being used, otherwise <c>false</c>.</param>
		public void OnSorterChanged(RecordSorter sorter, string sortName, bool isDefaultSort)
		{
			CheckDisposed();

			m_isDefaultSort = isDefaultSort;

			m_sortName = sortName;
			m_mediator.PropertyTable.SetProperty(SortNamePropertyTableId, m_sortName, PropertyTable.SettingsGroup.LocalSettings);

			m_list.ChangeSorter(sorter);
			// Remember how we're sorted.
			string persistSorter = DynamicLoader.PersistObject(Sorter, "sorter");
			m_mediator.PropertyTable.SetProperty(SorterPropertyTableId, persistSorter, PropertyTable.SettingsGroup.LocalSettings);

			UpdateSortStatusBarPanel();
		}

		/// <summary>
		/// Gets or sets a value indicating whether the clerk is using the default sorting.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if default sorting is being used, otherwise <c>false</c>.
		/// </value>
		public bool IsDefaultSort
		{
			get
			{
				CheckDisposed();
				return m_isDefaultSort;
			}

			set
			{
				CheckDisposed();
				m_isDefaultSort = value;
				UpdateSortStatusBarPanel();
			}
		}

		/// <summary>
		/// </summary>
		/// <param name="listItemsClass">class of list of objects expected by this clerk.</param>
		/// <param name="force">if true, force reload even if same list items class</param>
		internal void OnChangeListItemsClass(int listItemsClass, bool force)
		{
			CheckDisposed();
			m_list.ReloadList(listItemsClass, force);
		}

		public RecordFilter Filter
		{
			get
			{
				CheckDisposed();
				return m_list.Filter;
			}
		}

		public RecordSorter Sorter
		{
			get
			{
				CheckDisposed();
				return m_list.Sorter;
			}
		}

		private void m_list_AboutToReload(object sender, EventArgs e)
		{
			// This used to be a BroadcastMessage, but now broadcast is deferred.
			// To keep the same logic it's now using the SendMessageToAllNow.  This
			// is different from SendMessage as it is sent to all even if handled.
			// To avoid hitting the " For now, we'll not try to be concerned about restoring scroll position
			// in a context where we're reloading after suppressing a reload.
			if (!m_fReloadingDueToMissingObject)
				m_mediator.SendMessageToAllNow("SaveScrollPosition", this);
		}

		private void m_list_DoneReload(object sender, EventArgs e)
		{
			// This used to be a BroadcastMessage, but now broadcast is deferred.
			// To keep the same logic it's now using the SendMessageToAllNow.  This
			// is different from SendMessage as it is sent to all even if handled.
			if (!m_fReloadingDueToMissingObject)
				m_mediator.SendMessageToAllNow("RestoreScrollPosition", this);
		}

		ListUpdateHelper m_luh = null;
		internal ListUpdateHelper UpdateHelper
		{
			get { return m_luh; }
			set { m_luh = value; }
		}

		/// <summary>
		/// This class helps manage multiple changes to a record list.
		/// By default, it will suspend full Reloads initiated by PropChanged until we finish.
		/// During dispose, we'll ReloadList if we tried to reload the list via PropChanged.
		/// </summary>
		public class ListUpdateHelper : FwDisposableBase
		{
			RecordClerk m_clerk;
			private WaitCursor m_waitCursor = null;
			bool m_fOriginalListLoadingSuppressedState = false;
			bool m_fOriginalSkipRecordNavigationState = false;
			bool m_fOriginalLoadRequestedWhileSuppressed = false;
			ListUpdateHelper m_originalUpdateHelper = null;

			internal ListUpdateHelper(RecordList list, Control parentForWaitCursor)
				: this(list != null ? list.Clerk : null, parentForWaitCursor)
			{
			}

			/// <summary>
			///
			/// </summary>
			/// <param name="clerk"></param>
			/// <param name="parentForWaitCursor">for wait cursor</param>
			public ListUpdateHelper(RecordClerk clerk, Control parentForWaitCursor)
				: this(clerk)
			{
				if (parentForWaitCursor != null)
					m_waitCursor = new WaitCursor(parentForWaitCursor);
			}

			/// <summary>
			///
			/// </summary>
			/// <param name="clerk">clerk we want to suspend reloading for. if null, we don't do anything.</param>
			public ListUpdateHelper(RecordClerk clerk)
			{
				m_clerk = clerk;
				if (m_clerk != null)
				{
					m_fOriginalListLoadingSuppressedState = m_clerk.ListLoadingSuppressed;
					m_fOriginalSkipRecordNavigationState = m_clerk.m_skipShowRecord;
					m_fOriginalLoadRequestedWhileSuppressed = m_clerk.RequestedLoadWhileSuppressed;
					// monitor whether ReloadList was requested during the life of this ListUpdateHelper
					m_clerk.m_list.RequestedLoadWhileSuppressed = false;

					m_originalUpdateHelper = m_clerk.UpdateHelper;
					// if we're already suppressing the list, we don't want to auto reload since
					// the one who is suppressing the list expects to be able to handle that later.
					// or if the parent clerk is suppressing, we should wait until the parent reloads.
					RecordClerk parentClerk = clerk.ParentClerk();
					if (m_fOriginalListLoadingSuppressedState ||
						parentClerk != null && parentClerk.ListLoadingSuppressed)
					{
						m_fTriggerPendingReloadOnDispose = false;
					}
					m_clerk.ListLoadingSuppressedNoSideEffects = true;
					m_clerk.UpdateHelper = this;
				}
			}

			/// <summary>
			/// Indicates whether the list needs to reload as a side effect of PropChanges
			/// </summary>
			public bool NeedToReloadList
			{
				get { return m_clerk.RequestedLoadWhileSuppressed; }
			}

			bool m_fClearBrowseListUntilReload = false;
			/// <summary>
			/// Indicate that we want to clear browse items while we are
			/// waiting for a pending reload, so that the display will not
			/// try to access invalid objects.
			/// </summary>
			public bool ClearBrowseListUntilReload
			{
				get { return m_fClearBrowseListUntilReload; }
				set { m_fClearBrowseListUntilReload = value; }
			}

			/// <summary>
			/// Some user actions (e.g. editing) should not result in record navigation
			/// because it may cause the editing pane to disappear
			/// (thus losing the user's place in editing). This is used by ListUpdateHelper
			/// to skip record navigations while such user actions are taking place.
			/// </summary>
			public bool SkipShowRecord
			{
				get
				{
					return m_clerk != null && m_clerk.SkipShowRecord;
				}
				set
				{
					if (m_clerk != null)
						m_clerk.SkipShowRecord = value;
				}
			}

			/// <summary>
			/// Set to false if you don't want to automatically reload pending reload OnDispose.
			/// true, by default.
			/// </summary>
			bool m_fTriggerPendingReloadOnDispose = true;
			public bool TriggerPendingReloadOnDispose
			{
				get { return m_fTriggerPendingReloadOnDispose; }
				set { m_fTriggerPendingReloadOnDispose = value; }
			}

			#region FwDisposableBase Members

			protected override void DisposeManagedResources()
			{
				if (m_waitCursor != null)
					m_waitCursor.Dispose();
				if (m_clerk != null)
				{
					bool fHandledReload = false;
					if (m_fTriggerPendingReloadOnDispose && m_clerk.m_list.RequestedLoadWhileSuppressed)
					{
						m_clerk.ListLoadingSuppressed = m_fOriginalListLoadingSuppressedState;
						// if the requested while suppressed flag was reset, we handled it.
						if (m_clerk.m_list.RequestedLoadWhileSuppressed == false)
							fHandledReload = true;
					}
					else
					{
						m_clerk.ListLoadingSuppressedNoSideEffects = m_fOriginalListLoadingSuppressedState;
					}
					// if we didn't handle a pending reload, someone else needs to handle it.
					if (!fHandledReload)
						m_clerk.m_list.RequestedLoadWhileSuppressed |= m_fOriginalLoadRequestedWhileSuppressed;

					// reset this after we possibly reload the list.
					m_clerk.SkipShowRecord = m_fOriginalSkipRecordNavigationState;
					m_clerk.UpdateHelper = m_originalUpdateHelper;
				}
			}

			protected override void DisposeUnmanagedResources()
			{
				m_clerk = null;
				m_waitCursor = null;
			}

			#endregion FwDisposableBase
		}
	}

	/// <summary>
	/// This is a record clerk that can be used in a disposable context such as in a
	/// guicontrol in a dialog. For example, a normal RecordClerk will publish that it has become the "ActiveClerk"
	/// whenever ActivateUI is called. We don't want this to happen for record clerks that will only be used in a dialog,
	/// because the "ActiveClerk" will then become disposed after the dialog closes.
	/// </summary>
	public class TemporaryRecordClerk : RecordClerk
	{
		public override void ActivateUI(bool useRecordTreeBar)
		{
			// by default, we won't publish that we're the "ActiveClerk" or other usual effects.
			// but we do want to say that we're being actively used in a gui.
			m_fIsActiveInGui = true;
		}
		public override bool IsControllingTheRecordTreeBar
		{
			get
			{
				return true; // assume this will be true, say for instance in the context of a dialog.
			}
			set
			{
				// do not do anything here, unless you want to manage the "ActiveClerk" property.
			}
		}
		public override void OnPropertyChanged(string name)
		{
			// Objects of this class do not respond to 'propchanged' actions.
		}
		public override void Init(Mediator mediator, XmlNode viewConfiguration)
		{
			base.Init(mediator, viewConfiguration);
			// If we have a RecordList, it shouldn't generate PropChanged messages.
			if (m_list != null)
				m_list.EnableSendPropChanged = false;
		}
	}

	/// <summary>
	/// This is a record clerk that can be used in a guicontrol where the parent control knows
	/// when the list contents have changed, and to what.  You must use a MatchingItemsRecordList
	/// whenever you use a MatchingItemsRecordClerk.
	/// </summary>
	public class MatchingItemsRecordClerk : TemporaryRecordClerk
	{
		public MatchingItemsRecordClerk()
		{
		}

		public void UpdateList(int[] rghvo)
		{
			(m_list as MatchingItemsRecordList).UpdateList(rghvo);
		}


		protected override void StoreClerkInPropertyTable(XmlNode clerkConfiguration)
		{
			// Don't bother storing in the property table.
		}

		/// <summary>
		/// Set the specified index in the list.
		/// </summary>
		/// <param name="index"></param>
		public void SetListIndex(int index)
		{
			CheckDisposed();

			try
			{
				m_list.CurrentIndex = index;
			}
			catch (IndexOutOfRangeException error)
			{
				throw new IndexOutOfRangeException("The MatchingItemsRecordClerk tried to jump to a record which is not in the current active set of records.", error);
			}
		}

		/// <summary>
		/// Allow the sorter to be set according to the search criteria.
		/// </summary>
		public void SetSorter(RecordSorter sorter)
		{
			m_list.Sorter = sorter;
		}

		protected override bool TryRestoreFilter(Mediator mediator, XmlNode clerkConfiguration, FdoCache cache)
		{
			return false;
		}

		protected override bool TryRestoreSorter(Mediator mediator, XmlNode clerkConfiguration, FdoCache cache)
		{
			return false;
		}
	}

	/// <summary>
	/// This class creates a RecordClerk, or one of its subclasses, based on what is declared in the main <clerk> element.
	/// </summary>
	public class RecordClerkFactory
	{
		static public RecordClerk CreateClerk(Mediator mediator, XmlNode configurationNode)
		{
			/*
				<dynamicloaderinfo/>
			*/
			RecordClerk newClerk = null;
			XmlNode clerkNode = ToolConfiguration.GetClerkNodeFromToolParamsNode(configurationNode);
			Debug.Assert(clerkNode != null, "Could not find clerk.");
			XmlNode customClerkNode = clerkNode.SelectSingleNode("dynamicloaderinfo");
			if (customClerkNode == null)
				newClerk = new RecordClerk();
			else
				newClerk = (RecordClerk)DynamicLoader.CreateObject(customClerkNode);
			newClerk.Init(mediator, configurationNode);

			return newClerk;
		}
	}

	public class ToolConfiguration
	{
		static public string GetIdOfTool(XmlNode node)
		{
			return XmlUtils.GetManditoryAttributeValue(node,"id");
		}

		/// <summary>
		/// from the context of a sibling (tool (formerly view)) node, find the specified clerk definition.
		/// </summary>
		/// <param name="parameterNode"></param>
		/// <returns></returns>
		static public XmlNode GetClerkNodeFromToolParamsNode(XmlNode parameterNode)
		{
			string clerk = XmlUtils.GetManditoryAttributeValue(parameterNode, "clerk");
			XmlNode clerkNode = parameterNode.SelectSingleNode(@"ancestor::parameters/clerks/clerk[@id='" + clerk + "']");
			if (clerkNode == null)
				throw new ConfigurationException("Could not find <clerk id=" + clerk + ">.");
			return clerkNode;
		}

		static public XmlNode GetDefaultRecordFilterListProvider(XmlNode node)
		{
			try
			{
				//just define the first one to be the default, for now
				return node.SelectSingleNode(@"recordFilterListProvider");
			}
			catch(Exception)
			{
				return null;//no filters defined
			}
		}

		/// <summary>
		/// get the clerk associated with a tool's configuration parameters.
		/// </summary>
		/// <param name="mediator"></param>
		/// <param name="toolParameters"></param>
		/// <returns></returns>
		static public RecordClerk FindClerk(Mediator mediator, XmlNode parameterNode)
		{
			XmlNode node = ToolConfiguration.GetClerkNodeFromToolParamsNode(parameterNode);
			// Set the clerk id if the parent control hasn't already set it.
			string vectorName = ToolConfiguration.GetIdOfTool(node);
			return RecordClerk.FindClerk(mediator, vectorName);
		}

		static public XmlNode GetDefaultFilter(XmlNode node)
		{
			try
			{
				//just define the first one to be the default, for now
				return node.SelectSingleNode(@"filters/filter");
			}
			catch(Exception)
			{
				return null;//no filters defined
			}
		}
		static public XmlNode GetDefaultSorter(XmlNode node)
		{
			try
			{
				//just define the first one to be the default, for now
				return node.SelectSingleNode(@"sortMethods/sortMethod");
			}
			catch(Exception)
			{
				return null;//no sorter defined
			}
		}
	}

	/// <summary>
	/// The argument used when we broadcast OnRecordNavigation.
	/// </summary>
	public class RecordNavigationInfo : IComparable
	{
		RecordClerk m_clerk; // the clerk that sent the notification.
		int m_hvoCurrentObj = 0;
		bool m_fSuppressSaveOnChangeRecord;
		bool m_fSkipShowRecord;

		/// <summary>
		/// Make one.
		/// </summary>
		/// <param name="clerk"></param>
		/// <param name="shouldSaveOnChangeRecord"></param>
		public RecordNavigationInfo(RecordClerk clerk, bool suppressSaveOnChangeRecord, bool skipShowRecord)
		{
			m_clerk = clerk;
			m_hvoCurrentObj = m_clerk != null && m_clerk.CurrentObject != null ? m_clerk.CurrentObject.Hvo : 0;
			m_fSuppressSaveOnChangeRecord = suppressSaveOnChangeRecord;
			m_fSkipShowRecord = skipShowRecord;
		}

		/// <summary>
		///  The clerk that broadcast the change.
		/// </summary>
		public RecordClerk Clerk
		{
			get { return m_clerk; }
		}

		/// <summary>
		/// Whether a change of record should result in a save (and discard of undo items).
		/// This is suppressed if the change is caused by creating or deleting a record.
		/// </summary>
		public bool SuppressSaveOnChangeRecord
		{
			get { return m_fSuppressSaveOnChangeRecord; }
		}

		/// <summary>
		/// Indicates whether the this action should skip ShowRecord
		/// (e.g. to avoid losing the context/pane where the user may be editing.)
		/// </summary>
		public bool SkipShowRecord
		{
			get { return m_fSkipShowRecord; }
		}

		/// <summary>
		/// HvoOfClerkAtTimeOfNavigation is needed in Equals() for determining whether or not
		/// RecordNavigationInfo has changed in the property table.
		/// </summary>
		public int HvoOfCurrentObjAtTimeOfNavigation
		{
			get
			{
				return m_hvoCurrentObj;
			}
		}

		/// <summary>
		/// Given an argument from OnRecordNavigation, expected to be a RecordNavigationInfo,
		/// if it really is return it's clerk. Otherwise return null.
		/// </summary>
		/// <param name="argument"></param>
		/// <returns></returns>
		public static RecordClerk GetSendingClerk(object argument)
		{
			RecordNavigationInfo info = argument as RecordNavigationInfo;
			if (info == null)
				return null;
			return info.Clerk;
		}

		#region IComparable Members

		/// <summary>
		/// RecordNavigation info can be considered equivalent if
		/// the CurrentObject hasn't changed.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj)
		{
			return CompareTo(obj) == 0;
		}

		public override int GetHashCode()
		{
			return m_clerk.VirtualFlid & m_hvoCurrentObj;
		}

		public int CompareTo(object obj)
		{
			if (ReflectionHelper.HaveSamePropertyValues(this, obj))
				return 0;
			else
				return -1;
		}

		#endregion
	}

	/// <summary>
	/// This interface may be implemented by a virtual handler to request that a specified item be added to the list.
	/// The initial use is when following a link to a Scripture section that is not in the current Scripture filter.
	/// </summary>
	public interface IAddItemToVirtualProperty
	{
		/// <summary>
		/// Add the item to the property this handler represents, if possible. Return the index where it was
		/// inserted, or -1 if it could not be inserted.
		/// </summary>
		bool Add(int hvoOwner, int hvoItem);
	}
}