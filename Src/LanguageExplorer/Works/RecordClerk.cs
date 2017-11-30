// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// <remarks>
#if RANDYTODO
// TODO: If the first line in the remark is true, then think about merging RecordClerk & RecordList,
// TODO: since that "xCore/xWorks environment" is headed to the bitbucket.
#endif
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
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using LanguageExplorer.Areas;
using LanguageExplorer.Controls.XMLViews;
using LanguageExplorer.LcmUi;
using LanguageExplorer.LcmUi.Dialogs;
using SIL.Code;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.Text;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using SIL.FieldWorks.Filters;
using SIL.Reporting;
using SIL.LCModel.Utils;
using WaitCursor = SIL.FieldWorks.Common.FwUtils.WaitCursor;

namespace LanguageExplorer.Works
{
	/// <summary>
	/// Takes care of a list of records, standing between it and the UI.
	/// </summary>
	public class RecordClerk : IRecordClerk
	{
#if RANDYTODO
		// TODO: Think about not using the static, but also not adding IRecordClerkRepository to the property table.
		// TODO: A new intance is included in MajorFlexComponentParameters,
		// TODO: which is then passed to the Activate/Deactivate of IMajorFlexComponent implementations.
		// TODO: Perhaps some other interface can be created that would allow IRecordClerkRepository to be passed
		// TODO: further down the chain to clients who try to fetch a record clerk from the property table.
		// TODO: They would then get it from the repository, and not the property table.
		// TODO: At that point this static can be removed.
		// TODO: The risk of using the static is that there can be multiple windows,
		// TODO: each of which has its own repository, property table, etc.
		// TODO: That means each window has to reset the static, when it becomes active (which now happens).
#endif
		#region Data members
		/// <summary>
		/// Holder of the repository (for now, at least).
		/// </summary>
		internal static IRecordClerkRepository ActiveRecordClerkRepository { get; set; }
		private const string SelectedListBarNodeErrorMessage = "An item stored in the Property Table under SelectedListBarNode (typically from the ListView of an xWindow's record bar) should have an Hvo stored in its Tag property.";
		private ListUpdateHelper m_bulkEditUpdateHelper;
		/// <summary>
		/// All of the sorters for the clerk.
		/// </summary>
		protected Dictionary<string, PropertyRecordSorter> m_allSorters;
		protected static IRecordClerk s_lastClerkToLoadTreeBar;
		protected readonly string m_id;
		/// <summary>
		/// True if the Clerk is being used in a Gui.
		/// </summary>
		protected bool m_fIsActiveInGui;
		/// <summary>
		/// this will be null if this clerk is dependent on another one. Only the top-level clerk
		/// gets to be represented by and interact with the tree bar.
		/// </summary>
		protected RecordBarHandler m_recordBarHandler;
		/// <summary>
		/// when this is not null, that means there is another clerk managing a list,
		/// and the selected item of that list provides the object that this
		/// RecordClerk gets items out of. For example, the WfiAnalysis clerk
		/// is dependent on the WfiWordform clerk to tell it which wordform it is supposed to
		/// be displaying the analyses of.
		/// </summary>
		protected IRecordClerk m_clerkProvidingRootObject;
		/// <summary>
		/// this is an object which gives us the list of filters which we should offer to the user from the UI.
		/// this does not include the filters they can get that by using the FilterBar.
		/// </summary>
		protected RecordFilterListProvider m_filterProvider;
		private bool m_editable = true;
		private bool m_suppressSaveOnChangeRecord; // true during delete and insert and ShowRecord calls caused by them.
		private bool m_skipShowRecord; // skips navigations while a user is editing something.
		private readonly bool m_shouldHandleDeletion = true; // false, if the dependent clerk is to handle deletion, as for reversals.
		private bool m_allowDeletions = true;	// false if nothing is to be deleted for this record clerk.
		/// <summary>
		/// We need to store what filter we are responsible for setting, locally, so
		/// that when the user says "no filter/all records",
		/// we can selectively remove just this filter from the set that is being kept by the
		/// RecordList. That list would contain filters contributed from other sources, in particular
		/// the FilterBar.
		/// </summary>
		protected RecordFilter m_activeMenuBarFilter;
		/// <summary />
		protected IRecordChangeHandler m_rch;
		/// <summary>
		/// The display name of what is currently being sorted. This variable is persisted as a user
		/// setting. When the sort name is null it indicates that the items in the clerk are not
		/// being sorted or that the current sorting should not be displayed (i.e. the default column
		/// is being sorted).
		/// </summary>
		private bool m_isDefaultSort;
		private StatusBar m_statusBar;
		private RecordSorter m_defaultSorter;
		private string m_defaultSortLabel;
		private RecordFilter m_defaultFilter;
		bool m_fReloadingDueToMissingObject;

		#endregion Data members

		#region Constructors

		/// <summary />
		/// <param name="id">Clerk id/name.</param>
		/// <param name="statusBar"></param>
		/// <param name="recordList">Record list for the clerk.</param>
		/// <param name="defaultSorter">The default record sorter.</param>
		/// <param name="defaultSortLabel"></param>
		/// <param name="defaultFilter">The default filter to use.</param>
		/// <param name="allowDeletions"></param>
		/// <param name="shouldHandleDeletion"></param>
		internal RecordClerk(string id, StatusBar statusBar, IRecordList recordList, RecordSorter defaultSorter, string defaultSortLabel, RecordFilter defaultFilter, bool allowDeletions, bool shouldHandleDeletion)
		{
			Guard.AgainstNullOrEmptyString(id, nameof(id));
			Guard.AgainstNull(statusBar, nameof(statusBar));
			Guard.AgainstNull(recordList, nameof(recordList));
			Guard.AgainstNull(defaultSorter, nameof(defaultSorter));
			Guard.AgainstNullOrEmptyString(defaultSortLabel, nameof(defaultSortLabel));

			m_id = id;
			m_statusBar = statusBar;
			MyRecordList = recordList;
			MyRecordList.Clerk = this;
			m_defaultSorter = defaultSorter;
			m_defaultSortLabel = defaultSortLabel;
			m_defaultFilter = defaultFilter; // Null is fine.
			m_allowDeletions = allowDeletions;
			m_shouldHandleDeletion = shouldHandleDeletion;
			IgnoreStatusPanel = false;
		}

		/// <summary />
		/// <param name="id">Clerk id/name.</param>
		/// <param name="statusBar"></param>
		/// <param name="recordList">Record list for the clerk.</param>
		/// <param name="sorters">The record sorters for the clerk.</param>
		/// <param name="defaultFilter">The default filter to use.</param>
		/// <param name="allowDeletions"></param>
		/// <param name="shouldHandleDeletion"></param>
		internal RecordClerk(string id, StatusBar statusBar, IRecordList recordList, Dictionary<string, PropertyRecordSorter> sorters, RecordFilter defaultFilter, bool allowDeletions, bool shouldHandleDeletion)
		{
			Guard.AgainstNullOrEmptyString(id, nameof(id));
			Guard.AgainstNull(statusBar, nameof(statusBar));
			Guard.AgainstNull(recordList, nameof(recordList));
			Guard.AgainstNull(sorters, nameof(sorters));

			m_id = id;
			m_statusBar = statusBar;
			MyRecordList = recordList;
			MyRecordList.Clerk = this;
			m_allSorters = sorters;
			m_defaultSorter = sorters[AreaServices.Default];
			m_defaultSortLabel = AreaServices.Default;
			m_defaultFilter = defaultFilter; // Null is fine.
			m_allowDeletions = allowDeletions;
			m_shouldHandleDeletion = shouldHandleDeletion;
		}

		/// <summary />
		/// <param name="id">Clerk id/name.</param>
		/// <param name="statusBar"></param>
		/// <param name="recordList">Record list for the clerk.</param>
		/// <param name="defaultSorter">The default record sorter.</param>
		/// <param name="defaultSortLabel"></param>
		/// <param name="defaultFilter">The default filter to use.</param>
		/// <param name="allowDeletions"></param>
		/// <param name="shouldHandleDeletion"></param>
		/// <param name="filterProvider"></param>
		internal RecordClerk(string id, StatusBar statusBar, IRecordList recordList, RecordSorter defaultSorter, string defaultSortLabel, RecordFilter defaultFilter, bool allowDeletions, bool shouldHandleDeletion, RecordFilterListProvider filterProvider)
			: this(id, statusBar, recordList, defaultSorter, defaultSortLabel, defaultFilter, allowDeletions, shouldHandleDeletion)
		{
			Guard.AgainstNull(filterProvider, nameof(filterProvider));

			m_filterProvider = filterProvider;
		}

		/// <summary />
		/// <param name="id">Clerk id/name.</param>
		/// <param name="statusBar"></param>
		/// <param name="recordList">Record list for the clerk.</param>
		/// <param name="defaultSorter">The default record sorter.</param>
		/// <param name="defaultSortLabel"></param>
		/// <param name="defaultFilter">The default filter to use.</param>
		/// <param name="allowDeletions"></param>
		/// <param name="shouldHandleDeletion"></param>
		/// <param name="recordBarHandler"></param>
		internal RecordClerk(string id, StatusBar statusBar, IRecordList recordList, RecordSorter defaultSorter, string defaultSortLabel, RecordFilter defaultFilter, bool allowDeletions, bool shouldHandleDeletion, RecordBarHandler recordBarHandler)
			: this(id, statusBar, recordList, defaultSorter, defaultSortLabel, defaultFilter, allowDeletions, shouldHandleDeletion)
		{
			Guard.AgainstNull(recordBarHandler, nameof(recordBarHandler));

			m_recordBarHandler = recordBarHandler;
		}

		/// <summary />
		/// <param name="id">Clerk id/name.</param>
		/// <param name="statusBar"></param>
		/// <param name="recordList">Record list for the clerk.</param>
		/// <param name="defaultSorter">The default record sorter.</param>
		/// <param name="defaultSortLabel"></param>
		/// <param name="defaultFilter">The default filter to use.</param>
		/// <param name="allowDeletions"></param>
		/// <param name="shouldHandleDeletion"></param>
		/// <param name="clerkProvidingRootObject">Main clerk</param>
		internal RecordClerk(string id, StatusBar statusBar, IRecordList recordList, RecordSorter defaultSorter, string defaultSortLabel, RecordFilter defaultFilter, bool allowDeletions, bool shouldHandleDeletion, IRecordClerk clerkProvidingRootObject)
			: this(id, statusBar, recordList, defaultSorter, defaultSortLabel, defaultFilter, allowDeletions, shouldHandleDeletion)
		{
			Guard.AgainstNull(clerkProvidingRootObject, nameof(clerkProvidingRootObject));

			m_clerkProvidingRootObject = clerkProvidingRootObject;
		}

		#endregion Constructors

		#region All interface implementations

		#region Implementation of IRecordClerk

		public event FilterChangeHandler FilterChangedByClerk;
		/// <summary>
		/// Let interested parties know about a change in the current record.
		/// </summary>
		public event RecordNavigationInfoEventHandler RecordChanged;
		/// <summary>
		/// Let interested parties know about a change in the currently selected object.
		/// </summary>
		public event SelectObjectEventHandler SelectedObjectChanged;
		public event EventHandler SorterChangedByClerk;

		/// <summary>
		/// Tell the RecordClerk that it may now be the new master of the tree bar, if it is not a dependent clerk.
		/// </summary>
		/// <remarks>
		/// Use DeactivatedGui to tell RecordClerk that it's not currently being used in a Gui.
		/// </remarks>
		public virtual void ActivateUI(bool useRecordTreeBar, bool updateStatusBar = true)
		{
			CheckDisposed();
			if (m_fIsActiveInGui)
			{
				return; // Only do it once.
			}
			m_fIsActiveInGui = true;

			RegisterMessageHandlers();
			AddNotification();

			if (m_recordBarHandler != null)
			{
				IsControllingTheRecordTreeBar = true;
				if (useRecordTreeBar && s_lastClerkToLoadTreeBar != this)//optimization
				{
					s_lastClerkToLoadTreeBar = this;
					m_recordBarHandler.PopulateRecordBar(MyRecordList);
				}
			}

			if (!updateStatusBar)
			{
				return;
			}
			UpdateFilterStatusBarPanel();
			UpdateSortStatusBarPanel();
		}

		public bool AreCustomFieldsAProblem(int[] clsids)
		{
			var mdc = (IFwMetaDataCacheManaged)Cache.MetaDataCacheAccessor;
			var rePunct = new Regex(@"\p{P}");
			foreach (var clsid in clsids)
			{
				var flids = mdc.GetFields(clsid, true, (int) CellarPropertyTypeFilter.All);
				foreach (var flid in flids)
				{
					if (!mdc.IsCustom(flid))
					{
						continue;
					}
					var name = mdc.GetFieldName(flid);
					if (!rePunct.IsMatch(name))
					{
						continue;
					}
					var msg = string.Format(xWorksStrings.PunctInFieldNameWarning, name);
					// The way this is worded, 'Yes' means go on with the export. We won't bother them reporting
					// other messed-up fields. A 'no' answer means don't continue, which means it's a problem.
					return (MessageBox.Show(Form.ActiveForm, msg, xWorksStrings.PunctInfieldNameCaption, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes);
				}
			}
			return false; // no punctuation in custom fields.
		}

		/// <summary>
		/// Test to see if the two given sorters are compatible, override if you need to check for something
		/// beyond what the RecordSorter.CompatibleSorter() will test.
		/// </summary>
		/// <param name="first"></param>
		/// <param name="second"></param>
		/// <returns></returns>
		public virtual bool AreSortersCompatible(RecordSorter first, RecordSorter second)
		{
			return first.CompatibleSorter(second);
		}

		/// <summary>
		/// The record list might need access to this just to check membership of an object quickly.
		/// </summary>
		public RecordBarHandler BarHandler => m_recordBarHandler;

		/// <summary>
		/// Tell RecordClerk that we're not currently being used in a Gui.
		/// </summary>
		public virtual void BecomeInactive()
		{
			UnregisterMessageHandlers(); // No sense handling messages, when dormant.
			m_fIsActiveInGui = false;
			m_recordBarHandler?.ReleaseRecordBar();
			RemoveNotification();
			// If list loading was suppressed by this view (e.g., bulk edit to prevent changed items
			// disappearing from filter), stop that now, so it won't affect any future use of the list.
			if (MyRecordList != null)
			{
				MyRecordList.ListLoadingSuppressed = false;
			}
		}

		/// <summary>
		/// our list's LcmCache
		/// </summary>
		public LcmCache Cache => MyRecordList.Cache;

		public bool CanMoveTo(Navigation navigateTo)
		{
			bool canMoveTo;
			switch (navigateTo)
			{
				case Navigation.First:
					canMoveTo = MyRecordList.FirstItemIndex != -1 && MyRecordList.FirstItemIndex != MyRecordList.CurrentIndex;
					break;
				case Navigation.Next:
					canMoveTo = MyRecordList.NextItemIndex != -1 && MyRecordList.NextItemIndex != MyRecordList.CurrentIndex;
					break;
				case Navigation.Previous:
					canMoveTo = MyRecordList.PrevItemIndex != -1 && MyRecordList.PrevItemIndex != MyRecordList.CurrentIndex;
					break;
				case Navigation.Last:
					canMoveTo = MyRecordList.LastItemIndex != -1 && MyRecordList.LastItemIndex != MyRecordList.CurrentIndex;
					break;
				default:
					throw new IndexOutOfRangeException($"I don't know if one can move to '{navigateTo}'.");
			}
			return canMoveTo;
		}

		public int CurrentIndex
		{
			get
			{
				CheckDisposed();

				return MyRecordList.CurrentIndex;
			}
			set
			{
				MyRecordList.CurrentIndex = value;
			}
		}

		public ICmObject CurrentObject
		{
			get
			{
				CheckDisposed();
				if (MyRecordList.IsCurrentObjectValid())
					return MyRecordList.CurrentObject;
				return null;
			}
		}

		/// <remarks>virtual for tests</remarks>
		public virtual int CurrentObjectHvo => MyRecordList.CurrentObjectHvo;

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

		public RecordFilter Filter
		{
			get
			{
				CheckDisposed();
				return MyRecordList.Filter;
			}
			set
			{
				MyRecordList.Filter = value;
			}
		}

		public bool HasEmptyList
		{
			get
			{
				CheckDisposed();

				return MyRecordList.SortedObjects.Count == 0;
			}
		}

		public string Id
		{
			get
			{
				CheckDisposed();

				return m_id;
			}
		}

		public bool IsActiveInGui => m_fIsActiveInGui;

#if RANDYTODO
		// TODO: Odds are high that this won't need to be refactored, as suggested, but simply removed.
		// TODO: My plan is to not have the clerk fret about whether it should, or should not,
		// TODO: mess with the record tree bar, since the ITool implementation will know if one was used, or not.
#endif
		/// <summary>
		/// Tells whether this RecordClerk object should be updating the record tree bar when its list changes
		/// </summary>
		/// <remarks>
		/// A RecordClerk can be in one of three states with respect to control of the
		/// user interface. It can be the RecordClerk being used to control the tree bar,
		/// a can be a dependent clerk which does not have any relationship to the tree bar,
		/// or it can be in the background altogether, because the tool(s) it is associated with are not
		/// currently active.
		///
		/// note: PLEASE REFACTOR: If this method is causing problems again please re-factor.
		/// This code has proven to cause bugs elsewhere and is handling things in a non-obvious way.
		/// Ideally this whole property would be removed or re-written and the original problem fixed in a different way.
		/// Some code in LanguageExplorer.Areas.TextsAndWords.Interlinear.InfoPane.InitializeInfoView() is a patch to unwanted side effects of this method.
		/// Naylor, Thomson 12-2011
		///
		/// This property was added when we ran into the following problem: when a user added a new entry
		/// from the interlinear text section, that generated a notification that a new entry had been added to
		/// the list of entries. The RecordClerk that was responsible for that list then woke up, and tried
		/// to update the tree bar to show this new item. But of course, the tree bar was currently showing the list
		/// of texts, so this was a *bad idea*. Hence this new property, which relies on a new
		/// property in the property table to say which RecordClerk claims to be the current master
		/// of the tree bar.
		/// </remarks>
		public virtual bool IsControllingTheRecordTreeBar
		{
			set
			{
				CheckDisposed();

				Debug.Assert(value);
				var oldActiveClerk = ActiveRecordClerkRepository?.ActiveRecordClerk;
				if (oldActiveClerk != this)
				{
					oldActiveClerk?.BecomeInactive();
					ActiveRecordClerkRepository.ActiveRecordClerk = this;
					// We are adding this property so that EntryDlgListener can get access to the owning object
					// without first getting a RecordClerk, since getting a RecordClerk at that level causes a
					// circular dependency in compilation.
					PropertyTable.SetProperty("ActiveClerkOwningObject", OwningObject, false, true);
					PropertyTable.SetProperty("ActiveClerkSelectedObject", CurrentObject, false, true);
					Cache.DomainDataByFlid.AddNotification(this);
				}
			}
			get
			{
				CheckDisposed();

#if RANDYTODO
				return (m_recordBarHandler != null) && RecordClerkRepository.ActiveRecordClerk == this && IsActiveInGui;
#else
				//return (m_recordBarHandler != null) && IsPrimaryClerk;
				return IsPrimaryClerk;
#endif
			}
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
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed { get; private set; }

		/// <summary>
		/// Jump to the specified index in the list.
		/// </summary>
		/// <param name="index">The index.</param>
		/// <param name="suppressFocusChange">if set to <c>true</c> focus changes will be suppressed.</param>
		public void JumpToIndex(int index, bool suppressFocusChange = false)
		{
			CheckDisposed();
			// If we aren't changing the index, just bail out. (Fixes, LT-11401)
			if (MyRecordList.CurrentIndex == index)
			{
				//Refactor: we would prefer to bail out without broadcasting anything but...
				//There is a chain of messages and events that I don't yet understand which relies on
				//the RecordNavigation event being sent when we jump to the record we are already on.
				//The back button navigation in particular has major problems if we don't do this.
				//I suspect that something is suppressing the event handling initially, and I have found evidence in
				//RecordBrowseView line 483 and elsewhere that we rely on the re-broadcasting.
				//in order to maintain the LT-11401 fix we directly use the mediator here and pass true in the
				//second parameter so that we don't save the record and lose the undo history. -naylor 2011-11-03
				OnRecordChanged(new RecordNavigationEventArgs(new RecordNavigationInfo(this, true, SkipShowRecord, suppressFocusChange)));
				return;
			}
			try
			{
				MyRecordList.CurrentIndex = index;
			}
			catch (IndexOutOfRangeException error)
			{
				throw new IndexOutOfRangeException("The RecordClerk tried to jump to a record which is not in the current active set of records.", error);
			}
			// This broadcast will often cause a save of the record, which clears the undo stack.
			BroadcastChange(suppressFocusChange);
		}

		/// <summary>
		/// Jump to the specified object.
		/// </summary>
		/// <param name="jumpToHvo">The jump to hvo.</param>
		/// <param name="suppressFocusChange">if set to <c>true</c> focus changes will be suppressed.</param>
		public void JumpToRecord(int jumpToHvo, bool suppressFocusChange = false)
		{
			CheckDisposed();

			var index = MyRecordList.IndexOf(jumpToHvo);
			if (index < 0)
				return; // not found (maybe suppressed by filter?)
			JumpToIndex(index, suppressFocusChange);
		}

		public void JumpToRecord(Guid jumpToGuid, bool suppressFocusChange = false)
		{
			ICmObject obj;
			if (Cache.ServiceLocator.GetInstance<ICmObjectRepository>().TryGetObject(jumpToGuid, out obj))
			{
				JumpToRecord(obj.Hvo, suppressFocusChange);
			}
		}

		/// <summary>
		/// get the class of the items in this list.
		/// </summary>
		public int ListItemsClass => ((ISortItemProvider)MyRecordList).ListItemsClass;

		/// <summary>
		/// Used to suppress Reloading our list multiple times until we're finished with PropChanges.
		/// Also used (e.g., in Change spelling dialog) when we do NOT want the contents of the list
		/// to change owing to changed filter properties.
		/// </summary>
		public bool ListLoadingSuppressed
		{
			get { return MyRecordList.ListLoadingSuppressed; }
			set { MyRecordList.ListLoadingSuppressed = value; }
		}

		public bool ListLoadingSuppressedNoSideEffects
		{
			get { return MyRecordList.ListLoadingSuppressed; }
			set { MyRecordList.SetSuppressingLoadList(value); }
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
				return MyRecordList.ListModificationInProgress;
			}
			set
			{
				CheckDisposed();
				MyRecordList.ListModificationInProgress = value;
			}
		}

		public int ListSize
		{
			get
			{
				CheckDisposed();

				return MyRecordList.SortedObjects.Count;
			}
		}

		public void MoveToIndex(Navigation navigateTo)
		{
			CheckDisposed();

			int newIndex;
			switch (navigateTo)
			{
				case Navigation.First:
					newIndex = MyRecordList.FirstItemIndex;
					break;
				case Navigation.Next:
					// NB: This may be used in situations where there is no next record, as
					// when the current record has been deleted but it was the last record.
					newIndex = MyRecordList.NextItemIndex;
					break;
				case Navigation.Previous:
					newIndex = MyRecordList.PrevItemIndex;
					break;
				case Navigation.Last:
					newIndex = MyRecordList.LastItemIndex;
					break;
				default:
					throw new IndexOutOfRangeException($"I don't know how to move to '{navigateTo}'.");
			}
			MyRecordList.CurrentIndex = newIndex;
			BroadcastChange(false);
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
				PropertyTable.SetProperty(CurrentFilterPropertyTableId, (new UncheckAll()).Name, SettingsGroup.LocalSettings, true, false);
			}
			// if no filter is set, then we always want the "No Filter" item selected.
			else if (Filter == null)
			{
				// Resetting the table property value to NoFilters checks this item.
				PropertyTable.SetProperty(CurrentFilterPropertyTableId, (new NoFilters()).Name, SettingsGroup.LocalSettings, true, false);
			}
			// allow others to process this.
			return false;
		}

		public void OnChangeFilter(FilterChangeEventArgs args)
		{
			CheckDisposed();

			var window = PropertyTable.GetValue<Form>("window");
			using (new WaitCursor(window))
			{
				Logger.WriteEvent("Changing filter.");
				// if our clerk is in the state of suspending loading the list, reset it now.
				if (SuspendLoadListUntilOnChangeFilter)
					SuspendLoadListUntilOnChangeFilter = false;
				MyRecordList.OnChangeFilter(args);
				// Remember the active filter for this list.
				var persistFilter = DynamicLoader.PersistObject(Filter, "filter");
				PropertyTable.SetProperty(FilterPropertyTableId, persistFilter, SettingsGroup.LocalSettings, true, true);
				// adjust menu bar items according to current state of Filter, where needed.
				Publisher.Publish("AdjustFilterSelection", Filter);
				UpdateFilterStatusBarPanel();
				if (MyRecordList.Filter != null)
				{
					Logger.WriteEvent("Filter changed: " + MyRecordList.Filter);
				}
				else
				{
					Logger.WriteEvent("Filter changed: (no filter)");
				}
				// notify clients of this change.
				FilterChangedByClerk?.Invoke(this, args);
			}
		}

#if RANDYTODO
		public bool OnDisplayChangeFilterClearAll(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			if (!IsPrimaryClerk)
				return false; // Let the primary clerk have a chance to handle this! (See LT-12786)

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
#endif

		public void OnChangeFilterClearAll(object commandObject)
		{
			CheckDisposed();

			m_activeMenuBarFilter = null; // there won't be a menu bar filter after this.
			if (Filter is AndFilter)
			{
				// If some parts are not user visible we should not remove them.
				var af = (AndFilter)Filter;
				var children = af.Filters;
				var childrenToKeep = from RecordFilter filter in children where !filter.IsUserVisible select filter;
				var count = childrenToKeep.Count();
				if (count == 1)
				{
					OnChangeFilter(new FilterChangeEventArgs(childrenToKeep.First(), af));
					return;
				}
				else if (count > 0)
				{
					var af2 = MyRecordList.CreateNewAndFilter(childrenToKeep.ToArray());
					OnChangeFilter(new FilterChangeEventArgs(af2, Filter));
					return;
				}
				// Otherwise none of the children need to be kept, get rid of the whole filter.
			}

			OnChangeFilter(new FilterChangeEventArgs(null, Filter));
		}

		/// <summary>
		/// Handle a change to the class of items we want to bulk edit (and sometimes the field we want to bulk edit matters, too).
		/// </summary>
		/// <param name="listItemsClass">class of list of objects expected by this clerk.</param>
		/// <param name="newTargetFlid">If non-zero, the field we want to bulk edit.</param>
		/// <param name="force">if true, force reload even if same list items class</param>
		public void OnChangeListItemsClass(int listItemsClass, int newTargetFlid, bool force)
		{
			CheckDisposed();
			MyRecordList.ReloadList(listItemsClass, newTargetFlid, force);
		}

		public void OnChangeSorter()
		{
			CheckDisposed();

			var window = PropertyTable.GetValue<Form>("window");
			using (new WaitCursor(window))
			{
				Logger.WriteEvent($"Sorter changed: {MyRecordList.Sorter?.ToString() ?? "(no sorter)"}");
				SorterChangedByClerk?.Invoke(this, EventArgs.Empty);
			}
		}

		public bool OnDeleteRecord(object commandObject)
		{
			CheckDisposed();

			// Don't handle this message if you're not the primary clerk.  This allows, for
			// example, XmlBrowseRDEView.cs to handle the message instead.

			// Note from RandyR: One of these days we should probably subclass this object, and perhaps the record list more.
			// The "reversalEntries" clerk wants to handle the message, even though it isn't the primary clerk.
			// The m_shouldHandleDeletion member was also added, so the "reversalEntries" clerk's primary clerk
			// would not handle the message, and delete an entire reversal index.
			if (ShouldNotHandleDeletionMessage)
				return false;

			// It may be null:
			// 1. if the objects are being deleted using the keys,
			// 2. the last one has been deleted, and
			// 3. the user keeps pressing the del key.
			// It looks like the command is not being disabled at all or fast enough.
			if (CurrentObjectHvo == 0)
				return true;

			// Don't allow an object to be deleted if it shouldn't be deleted.
			if (!CanDelete())
			{
				ReportCannotDelete();
				return true;
			}

			ICmObject thingToDelete = GetObjectToDelete(CurrentObject);

			using (var dlg = new ConfirmDeleteObjectDlg(PropertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider")))
			{
				using (var uiObj = CmObjectUi.MakeUi(thingToDelete))
				{
					uiObj.InitializeFlexComponent(new FlexComponentParameters(PropertyTable, Publisher, Subscriber));
					string cannotDeleteMsg;
					if (uiObj.CanDelete(out cannotDeleteMsg))
					{
						dlg.SetDlgInfo(uiObj, Cache, PropertyTable);
					}
					else
					{
						dlg.SetDlgInfo(uiObj, Cache, PropertyTable, TsStringUtils.MakeString(cannotDeleteMsg, Cache.DefaultUserWs));
					}
				}
				var window = PropertyTable.GetValue<Form>("window");
				if (DialogResult.Yes == dlg.ShowDialog(window))
				{
#if RANDYTODO
					using (new WaitCursor(window))
					{
						using (ProgressState state = FwXWindow.CreatePredictiveProgressState(PropertyTable, "Delete record"))
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
								var cmd = (Command) commandObject;
								UndoableUnitOfWorkHelper.Do(cmd.UndoText, cmd.RedoText, Cache.ActionHandlerAccessor,
															() => m_list.DeleteCurrentObject(state, thingToDelete));
							}
							finally
							{
								m_suppressSaveOnChangeRecord = false;
							}
						}
					}
					PropertyTable.GetValue<IFwMainWnd>("window").RefreshAllViews();
#endif
				}
			}
			return true; //we handled this, no need to ask anyone else.
		}

		public bool OnExport(object argument)
		{
			CheckDisposed();

			// It's somewhat unfortunate that this bit of code knows what classes can have custom fields.
			// However, we put in code to prevent punctuation in custom field names at the same time as this check (which is therefore
			// for the benefit of older projects), so it should not be necessary to check any additional classes we allow to have them.
			if (AreCustomFieldsAProblem(new[] { LexEntryTags.kClassId, LexSenseTags.kClassId, LexExampleSentenceTags.kClassId, MoFormTags.kClassId }))
			{
				return true;
			}
			using (var dlg = new ExportDialog())
			{
				dlg.InitializeFlexComponent(new FlexComponentParameters(PropertyTable, Publisher, Subscriber));
				dlg.ShowDialog(PropertyTable.GetValue<Form>("window"));
			}
			ActivateUI(true);
			return true;    // handled
		}

#if RANDYTODO
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

			var command = (Command)commandObject;
			string className = XmlUtils.GetMandatoryAttributeValue(command.Parameters[0], "className");

			string restrictToClerkID = XmlUtils.GetOptionalAttributeValue(command.Parameters[0], "restrictToClerkID");
			if (restrictToClerkID != null && restrictToClerkID != Id)
				return display.Enabled = false;

			string restrictFromClerkID = XmlUtils.GetOptionalAttributeValue(command.Parameters[0], "restrictFromClerkID");
			if (restrictFromClerkID != null && restrictFromClerkID.Contains(Id))
				return display.Enabled = false;

			if (!CheckValidOperation(command, className))
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

		private bool CheckValidOperation(Command command, string className)
		{
			switch (m_list.ListItemsClass)
			{
				case RnGenericRecTags.kClassId:
					if (className == "RnGenericRec")
					{
						bool fSub = XmlUtils.GetOptionalBooleanAttributeValue(command.Parameters[0], "subrecord", false);
						bool fSubsub = XmlUtils.GetOptionalBooleanAttributeValue(command.Parameters[0], "subsubrecord", false);
						ICmObject obj = this.CurrentObject;
						if (obj == null)
						{
							// Can't insert subrecords if there aren't any primary records!
							if (fSub || fSubsub)
								return false;
						}
						else if (obj.Owner is IRnResearchNbk)
						{
							// if primary record, can't insert subsubrecord!
							if (fSubsub)
								return false;
						}
					}
					break;
				default:
					break;
			}
			return true;
		}
#endif

		/// <summary>
		/// this is triggered by any command whose message attribute is "InsertItemInVector"
		/// </summary>
		/// <param name="argument"></param>
		/// <returns>true if successful (the class is known)</returns>
		public bool OnInsertItemInVector(object argument)
		{
			CheckDisposed();

			if (!Editable)
				return false;
			// We will certainly switch records, but we're going to suppress the usual Save after we
			// switch, so the user can at least Undo one level, the actual insertion. But Undoing
			// that may not get us back to the current record, so we'd better not allow anything
			// that's already on the stack to be undone.
			// Enhance JohnT: if a dialog is brought up, the user could cancel, in which case,
			// we don't really need to throw away the Undo stack.
			SaveOnChangeRecord();

#if RANDYTODO
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

			var command = (Command) argument;
			string className;
			try
			{
				className = XmlUtils.GetMandatoryAttributeValue(command.Parameters[0], "className");
			}
			catch (ApplicationException e)
			{
				throw new FwConfigurationException("Could not get the necessary parameter information from this command",
					command.ConfigurationNode, e);
			}

			if (!m_list.CanInsertClass(className))
				return false;
#endif

			var result = false;
#if RANDYTODO
			m_suppressSaveOnChangeRecord = true;
			try
			{
				UndoableUnitOfWorkHelper.Do(string.Format(xWorksStrings.ksUndoInsert, command.UndoRedoTextInsert),
					string.Format(xWorksStrings.ksRedoInsert, command.UndoRedoTextInsert), Cache.ActionHandlerAccessor, () =>
				{
					result = m_list.CreateAndInsert(className);
				});
			}
			catch (ApplicationException ae)
			{
				throw new ApplicationException("Could not insert the item requested by the command " + command.ConfigurationNode, ae);
			}
			finally
			{
				m_suppressSaveOnChangeRecord = false;
			}
#endif

			Publisher.Publish("FocusFirstPossibleSlice", null);
			return result;
		}

		/// <summary>
		/// This is invoked by reflection when something might want to know about a change.
		/// The initial usage is for the respelling dialog to let ConcDecorators know about spelling changes.
		/// The notification is passed on to any SDAs that understand it, including embedded ones.
		/// </summary>
		/// <param name="argument"></param>
		public void OnItemDataModified(object argument)
		{
			var da = MyRecordList.VirtualListPublisher;
			while (da != null)
			{
				if (da.GetType().GetMethod("OnItemDataModified") != null)
					ReflectionHelper.CallMethod(da, "OnItemDataModified", new[] { argument });
				var decorator = da as DomainDataByFlidDecoratorBase;
				if (decorator == null)
					break;
				da = decorator.BaseSda;
			}
		}

		/// <summary>
		/// display the given record
		/// </summary>
		/// <param name="argument">the hvo of the record</param>
		/// <returns></returns>
		public bool OnJumpToRecord(object argument)
		{
			CheckDisposed();

			try
			{
				var hvoTarget = (int)argument;

				var index = IndexOfObjOrChildOrParent(hvoTarget);
				if (index == -1)
				{
					// See if this is a subclass that knows how to add items.
					if (AddItemToList(hvoTarget))
						index = IndexOfObjOrChildOrParent(hvoTarget);
				}
				if (MyRecordList.Filter != null && index == -1)
				{
					// We can get here with an irrelevant target hvo, for example by inserting a new
					// affix allomorph in an entry (see LT-4025).  So make sure we have a suitable
					// target before complaining to the user about a filter being on.
					var mdc = (IFwMetaDataCacheManaged)MyRecordList.VirtualListPublisher.MetaDataCache;
					var clidList = mdc.FieldExists(MyRecordList.Flid) ? mdc.GetDstClsId(MyRecordList.Flid) : -1;
					var clidObj = Cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoTarget).ClassID;

					// If (int) clidList is -1, that means it was for a decorator property and the IsSameOrSubclassOf
					// test won't be valid.
					// Enhance JohnT/CurtisH: It would be better to if decorator properties were recorded in the MDC, or
					// if we could access the decorator MDC.
					var fSuitableTarget = clidList == -1 || DomainObjectServices.IsSameOrSubclassOf(mdc, clidObj, clidList);

					if (fSuitableTarget)
					{
						var dr = MessageBox.Show(xWorksStrings.LinkTargetNotAvailableDueToFilter, xWorksStrings.TargetNotFound, MessageBoxButtons.YesNo);
						if (dr == DialogResult.Yes)
						{
							// We had changed from OnChangeFilter to SendMessage("RemoveFilters") to solve a filterbar
							// update issue reported in (LT-2448). However, that message only works in the context of a
							// BrowseViewer, not a document view (e.g. Dictionary) (see LT-7298). So, I've
							// tested OnChangeFilterClearAll, and it seems to solve both problems now.
							OnChangeFilterClearAll(null);
							m_activeMenuBarFilter = null;
							index = IndexOfObjOrChildOrParent(hvoTarget);
						}
						else
						{
							// user wants to give up
							return true;
						}
					}
				}

				if (index == -1)
				{
					// May be the item was just created by another program or tool (e.g., LT-8827)
					MyRecordList.ReloadList();
					index = IndexOfObjOrChildOrParent(hvoTarget);
					if (index == -1)
					{
						// It may be the wrong clerk, so just bail out.
						//MessageBox.Show("The list target is no longer available. It may have been deleted.",
						//	"Target not found", MessageBoxButtons.OK);
						return false;
					}
				}
				JumpToIndex(index);
				return true;    //we handled this.
			}
			finally
			{
				// Even if we didn't handle it, that might just be because something prevented us from
				// finding the object. But if we leave load-record suspended, a pane may never again get
				// drawn this whole session!
				SuspendLoadingRecordUntilOnJumpToRecord = false;
			}
		}

		/// <summary>
		/// true if the list is non empty and we are on the last record.
		/// </summary>
		public bool OnLast
		{
			get
			{
				CheckDisposed();
				return MyRecordList.OnLast;
			}
		}

		/// <summary>
		/// Receives the broadcast message "PropertyChanged"
		/// </summary>
		public virtual void OnPropertyChanged(string name)
		{
			CheckDisposed();

			//this happens when the user chooses a MenuItem or sidebar item that selects a filter
			if (name == CurrentFilterPropertyTableId)
			{
				OnChangeFilterToCheckedListPropertyChoice();
				return;
			}

			//if we are  "dependent" on another clerk to provide the owning object of our list:
			if (m_clerkProvidingRootObject != null)
			{
				if (name == DependentPropertyName)
				{
					UpdateOwningObjectIfNeeded();
				}
			}
		}

		public virtual bool OnRefresh(object argument)
		{
			CheckDisposed();

			using (new WaitCursor(PropertyTable.GetValue<Form>("window")))
			{
				m_rch?.Fixup(false);     // no need to recursively refresh!
				MyRecordList.ReloadList();
				return false;   //that other colleagues do a refresh, too.
			}
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

			SortName = sortName;
			PropertyTable.SetProperty(SortNamePropertyTableId, SortName, SettingsGroup.LocalSettings, true, true);

			MyRecordList.ChangeSorter(sorter);
			// Remember how we're sorted.
			string persistSorter = DynamicLoader.PersistObject(Sorter, "sorter");
			PropertyTable.SetProperty(SorterPropertyTableId, persistSorter, SettingsGroup.LocalSettings, true, true);

			UpdateSortStatusBarPanel();
		}

		/// <summary>
		/// the object which owns the elements which make up this list
		/// </summary>
		public ICmObject OwningObject
		{
			set
			{
				CheckDisposed();
				MyRecordList.OwningObject = value;
			}
			get
			{
				CheckDisposed();
				return MyRecordList.OwningObject;
			}
		}

		/// <summary>
		///
		/// </summary>
		public int OwningFlid
		{
			get
			{
				CheckDisposed();
				return MyRecordList.Flid;
			}
		}

		/// <summary>
		/// returns the Clerk that governs the OwningObject of our clerk.
		/// </summary>
		/// <returns></returns>
		public IRecordClerk ParentClerk
		{
			get
			{
				IRecordClerk parentClerk;
				TryClerkProvidingRootObject(out parentClerk);
				return parentClerk;
			}
		}

		/// <summary>
		/// Fabricate a name for storing the list index of the currently edited object based on
		/// the database and clerk.
		/// </summary>
		public string PersistedIndexProperty
		{
			get
			{
				CheckDisposed();

				return $"{m_id}-Index";
			}
		}

		/// <summary>
		/// Persist this list for retrieval by RestoreListFrom, if we are a primary Clerk.
		/// </summary>
		public void PersistListOn(string pathname)
		{
			if (IsPrimaryClerk)
				MyRecordList.PersistOn(pathname);
		}

		/// <summary>
		/// Get/set a progress reporter (encapsulated in an IAdvInd4 interface).  This is used
		/// only by the list.  (at least when first implemented)
		/// </summary>
		public IProgress ProgressReporter
		{
			get
			{
				CheckDisposed();
				return MyRecordList?.ProgressReporter;
			}
			set
			{
				CheckDisposed();
				if (MyRecordList != null)
				{
					MyRecordList.ProgressReporter = value;
				}
			}
		}

		public IRecordList MyRecordList { get; }

		public void ReloadFilterProvider()
		{
			m_filterProvider?.ReLoad();
		}

		/// <summary>
		/// This is public for now just so MatchingReversalEntriesBrowser.Initialize can call it,
		/// but I (JohnT) haven't yet fully determined whether it needs to. I just needed to
		/// stop it being called in another, much more common context, so moved a call there.
		/// </summary>
		public virtual void ReloadIfNeeded()
		{
			if (OwningObject != null && MyRecordList.IsVirtualPublisherCreated)
			{
				// A full refresh wipes out all caches as of 26 October 2005,
				// so we have to reload it. This fixes the sextuplets:
				// LT-5393, LT-6102, LT-6154, LT-6084, LT-6059, LT-6062.
				MyRecordList.ReloadList();
			}
		}

		/// <summary>
		/// deletes invalidated sort items from the list. (e.g. as result from Undo/Redo).
		/// </summary>
		public void RemoveInvalidItems()
		{
			MyRecordList.RemoveUnwantedSortItems();
		}

		/// <summary>
		/// deletes the given item from the list.  needed to fix LT-9230 without full reload.
		/// </summary>
		/// <param name="hvoToRemove"></param>
		public void RemoveItemsFor(int hvoToRemove)
		{
			((ISortItemProvider)MyRecordList).RemoveItemsFor(hvoToRemove);
		}

		public bool RequestedLoadWhileSuppressed
		{
			get { return MyRecordList.RequestedLoadWhileSuppressed; }
			set { MyRecordList.RequestedLoadWhileSuppressed = value; }
		}

		/// <summary>
		/// If a filter becomes invalid, it has to be reset somehow.  This resets it to the default filter
		/// for this clerk (possibly null).
		/// </summary>
		public void ResetFilterToDefault()
		{
			OnChangeFilter(new FilterChangeEventArgs(m_defaultFilter, MyRecordList.Filter));
		}

		/// <summary>
		/// Returns true if successful, false if some problem reading the file, including
		/// detecting that part of a key is a deleted object. Return false if this is not
		/// the primary clerk.
		/// </summary>
		public bool RestoreListFrom(string pathname)
		{
			return IsPrimaryClerk && MyRecordList.RestoreListFrom(pathname);
		}

		/// <summary>
		/// This method is called by views that display a single record to cause a Save when switching records.
		/// The purpose is to allow the special Save to be suppressed in certain cases, such as Delete record
		/// (and perhaps eventually create record).
		/// </summary>
		public void SaveOnChangeRecord()
		{
			CheckDisposed();

#if RANDYTODO
// Work up non static test that can use IFwMainWnd
#endif
			if (m_suppressSaveOnChangeRecord || Cache == null/* || FwXWindow.InUndoRedo*/)
				return;
			using (new WaitCursor(Form.ActiveForm))
			{
				// Commit() was too drastic here, resulting in Undo/Redo stack being cleared.
				// (See LT-13397)
				var actionHandler = Cache.ActionHandlerAccessor;
				if (actionHandler.CurrentDepth > 0)
				{
					// EndOuterUndoTask() is not implemented, so we better call EndUndoTask().
					// (This fixes LT-16673)
					actionHandler.EndUndoTask();
				}
			}
		}

		/// <summary>
		/// update the status bar, selected node of the tree bar, etc.
		/// </summary>
		public void SelectedRecordChanged(bool suppressFocusChange, bool fSkipRecordNavigation = false)
		{
			CheckDisposed();

			if (CurrentObjectHvo != 0 && !MyRecordList.CurrentObjectIsValid)
			{
				MyRecordList.ReloadList(); // clean everything up
			}
			if (IgnoreStatusPanel)
			{
				return;
			}
			if (IsControllingTheRecordTreeBar)
			{
				// JohnT: if we're not controlling the record list, we probably have no business trying to
				// control the status bar. But we may need a separate control over this.
				// Note that it can be definitely wrong to update it; this Clerk may not have anything
				// to do with the current window contents.
				UpdateStatusBarRecordNumber(StringTable.Table.GetString("No Records", "Misc"));
			}

#if RANDYTODO
			// Work up non-static test that can use IFwMainWnd
#endif
			// This is used by DependantRecordLists
			var rni = new RecordNavigationInfo(this, m_suppressSaveOnChangeRecord/* || FwXWindow.InUndoRedo*/, SkipShowRecord, suppressFocusChange);

			// As of 21JUL17 nobody cares about that 'propName' changing, so skip the broadcast.
			PropertyTable.SetProperty(PersistedIndexProperty, CurrentIndex, SettingsGroup.LocalSettings, true, false);

			if (IsControllingTheRecordTreeBar)
			{
				m_recordBarHandler?.UpdateSelection(CurrentObject);
				OnSelectedObjectChanged(new SelectObjectEventArgs(CurrentObject));
			}

			// We want an auto-save when we process the change record UNLESS we are deleting or inserting an object,
			// or performing an Undo/Redo.
			// Note: Broadcasting "OnRecordNavigation" even if a selection doesn't change allows the browse view to
			// scroll to the right index if it hasn't already done so.
			if (!fSkipRecordNavigation)
			{
				OnRecordChanged(new RecordNavigationEventArgs(rni));
			}
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
				return MyRecordList.ShouldNotModifyList;
			}
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
				CheckDisposed();
				if (m_skipShowRecord)
				{
					return true;
				}
				// if this Clerk is dependent upon a ParentClerk then
				// inherit its state.
				var parentClerk = ParentClerk;
				return parentClerk?.SkipShowRecord ?? false;
			}
			set
			{
				CheckDisposed();
				m_skipShowRecord = value;
			}
		}

		public RecordSorter Sorter
		{
			get
			{
				CheckDisposed();
				return MyRecordList.Sorter;
			}
			set
			{
				MyRecordList.Sorter = value;
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
				return MyRecordList;
			}
		}
		public string SortName { get; internal set; }

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
		/// Indicate whether we want to suspend loading a record (or records) because we expect to handle a subsequent
		/// OnJumpToRecord message.
		/// </summary>
		public bool SuspendLoadingRecordUntilOnJumpToRecord
		{
			get
			{
				var jumpToInfo = PropertyTable.GetValue("SuspendLoadingRecordUntilOnJumpToRecord", SettingsGroup.LocalSettings, string.Empty);
				if (string.IsNullOrEmpty(jumpToInfo))
				{
					return false;
				}
				var jumpToParams = jumpToInfo.Split(',');
				return jumpToParams.Length > 0 && IsActiveClerk && InDesiredTool(jumpToParams[0]);
			}
			set
			{
				Debug.Assert(value == false, "This property should only be reset by setting this property to false.");
				if (value == false && SuspendLoadingRecordUntilOnJumpToRecord)
				{
					// reset this property.
					// Nobody is watching this property change in develop. so don't bother publishing it.
					PropertyTable.SetProperty("SuspendLoadingRecordUntilOnJumpToRecord", string.Empty, SettingsGroup.LocalSettings, false, false);
				}

			}
		}

		/// <summary>
		/// Indicate whether the Clerk should suspend reloading its list until it gets OnChangeFilter (cf. TE-6493).
		/// This helps performance for links that setup filters for tools they are in the process of jumping to.
		/// When OnChangeFilter is being handled for the appropriate clerk, the setter is set to false to reset the property table
		/// property.
		/// (EricP) We could broaden this to not be so tied to OnChangeFilter, if we could suspend for other durations.
		/// </summary>
		public bool SuspendLoadListUntilOnChangeFilter
		{
			get
			{
				var toolName = PropertyTable.GetValue<string>("SuspendLoadListUntilOnChangeFilter", SettingsGroup.LocalSettings);
				return (!string.IsNullOrEmpty(toolName)) && InDesiredTool(toolName);
			}
			set
			{
				Debug.Assert(value == false, "This property should only be reset by setting this property to false.");
				if (value == false && SuspendLoadListUntilOnChangeFilter)
				{
					// reset this property.
					PropertyTable.SetProperty("SuspendLoadListUntilOnChangeFilter", "", SettingsGroup.LocalSettings, true, true);
				}
			}
		}

		public bool TryClerkProvidingRootObject(out IRecordClerk clerkProvidingRootObject)
		{
			clerkProvidingRootObject = null;
			if (IsPrimaryClerk)
			{
				return false;
			}
			clerkProvidingRootObject = m_clerkProvidingRootObject;
			return clerkProvidingRootObject != null;
		}

		/// <summary>
		/// Compares the state of the filters and sorters to persisted values in property table
		/// and re-establishes them from the property table if they have changed.
		/// </summary>
		/// <returns>true if we restored either a sorter or a filter.</returns>
		public bool UpdateFiltersAndSortersIfNeeded()
		{
			var fRestoredSorter = TryRestoreSorter();
			var fRestoredFilter = TryRestoreFilter();
			UpdateFilterStatusBarPanel();
			UpdateSortStatusBarPanel();
			return fRestoredSorter || fRestoredFilter;
		}

		public ListUpdateHelper UpdateHelper { get; set; }

		/// <summary>
		/// Very like UpdateOwningObject, but needs to be internal, and don't want to do anything
		/// (like reloading list) if it didn't change.
		/// </summary>
		public void UpdateOwningObjectIfNeeded()
		{
			CheckDisposed();

			UpdateOwningObject(true);
		}

		/// <summary>
		/// If the record bar is visible and needs to be repopulated, do it.
		/// </summary>
		public void UpdateRecordTreeBarIfNeeded()
		{
			CheckDisposed();

			m_recordBarHandler?.PopulateRecordBarIfNeeded(MyRecordList);
		}

		/// <summary />
		/// <param name="noRecordsText"></param>
		public void UpdateStatusBarRecordNumber(string noRecordsText)
		{
			string message;
			var len = MyRecordList.SortedObjects.Count;
			if (len > 0)
			{
				message = (1 + MyRecordList.CurrentIndex) + @"/" + len;
			}
			else
			{
				message = noRecordsText;
			}

			StatusBarPanelServices.SetStatusPanelRecordNumber(m_statusBar, message);
			ResetStatusBarMessageForCurrentObject();
		}

		/// <summary>
		/// Called by a view (e.g. browseView) when, internally, it changes the currently selected record.
		/// </summary>
		public void ViewChangedSelectedRecord(FwObjectSelectionEventArgs e)
		{
			CheckDisposed();

			// Don't do anything if we haven't changed our selection.
			var hvoCurrent = 0;
			if (CurrentObjectHvo != 0)
			{
				hvoCurrent = CurrentObjectHvo;
			}
			if (e.Index >= 0 && CurrentIndex == e.Index && hvoCurrent == e.Hvo || e.Index < 0 && hvoCurrent == e.Hvo)
			{
				return;
			}
			// In some cases (e.g. sorting LexEntries by Gloss), results in a list that
			// contains multiple rows referring to the same object. In that case
			// we want to try to JumpToRecord of the same index, since jumping to the hvo
			// jumps to the first instance of that object (LT-4691).
			// Through deletion of Reversal Index entry it was possible to arrive here with
			// no sorted objects. (LT-13391)
			if (e.Index >= 0 && MyRecordList.SortedObjects.Count > 0)
			{
				var ourHvo = MyRecordList.SortItemAt(e.Index).RootObjectHvo;
				// if for some reason the index doesn't match the hvo, we'll jump to the Hvo.
				// But we don't think that should happen, so Assert to help catch the problems.
				// JohnT Nov 2010: Someone had marked this as not ported to 7.0 with the comment "assert fires".
				// But I can't find any circumstances in which e.Index >= 0, much less a case where it fires.
				// If you feel a need to take this Assert out again, which would presumably mean you know a
				// very repeatable scenario for making it fire, please let me know what it is.
				// Original do nothing version from the day it was added: Debug.Assert(e.Hvo == e.Hvo, "the index (" + e.Index + ") for selected object (" + e.Hvo + ") does not match the object (" + e.Hvo + " in our list at that index.)");
				Debug.Assert(ourHvo == e.Hvo, "the index (" + e.Index + ") for selected object (" + ourHvo + ") does not match the object (" + e.Hvo + " in our list at that index.)");
				if (ourHvo != e.Hvo)
				{
					JumpToRecord(e.Hvo);
				}
				else
				{
					JumpToIndex(e.Index);
				}
			}
			else if (e.Hvo > 0)
			{
				JumpToRecord(e.Hvo);
			}
		}
		public int VirtualFlid
		{
			get
			{
				CheckDisposed();
				return MyRecordList.VirtualFlid;
			}
		}

		public ISilDataAccessManaged VirtualListPublisher => MyRecordList.VirtualListPublisher;

		#endregion Implementation of IRecordClerk

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

			MyRecordList.InitializeFlexComponent(flexComponentParameters);

			TryRestoreSorter();
			TryRestoreFilter();
			MyRecordList.ListChanged += OnListChanged;
			MyRecordList.AboutToReload += m_list_AboutToReload;
			MyRecordList.DoneReload += m_list_DoneReload;
			var fSetFilterMenu = false;
			if (m_filterProvider != null)
			{
				// There is only one clerk (concordanceWords) that sets m_filterProvider to a provider.
				// That provider is the class WfiRecordFilterListProvider.
				// That clerk is used in these tools: AnalysesTool, BulkEditWordformsTool, & WordListConcordanceTool
				// That WfiRecordFilterListProvider instance is (ok: "will be") provided in one of the RecordClerk contructor overloads.
				if (MyRecordList.Filter != null)
				{
					// There is only one clerk (concordanceWords) that sets m_filterProvider to a provider.
					// That clerk is used in these tools: AnalysesTool, BulkEditWordformsTool, & WordListConcordanceTool
					// find any matching persisted menubar filter
					// NOTE: for now assume we can only set/persist one such menubar filter at a time.
					foreach (RecordFilter menuBarFilterOption in m_filterProvider.Filters)
					{
						if (!MyRecordList.Filter.Contains(menuBarFilterOption))
						{
							continue;
						}
						m_activeMenuBarFilter = menuBarFilterOption;
						m_filterProvider.OnAdjustFilterSelection(m_activeMenuBarFilter);
						PropertyTable.SetDefault(CurrentFilterPropertyTableId, m_activeMenuBarFilter.id, SettingsGroup.LocalSettings, false, false);
						fSetFilterMenu = true;
						break;
					}
				}
			}

			if (!fSetFilterMenu)
			{
				OnAdjustFilterSelection(null);
			}

#if RANDYTODO
			//we handled the tree bar only if we are the root clerk
			if (m_clerkProvidingRootObject == null)
			{
				m_recordBarHandler = RecordBarHandler.Create(PropertyTable, m_clerkConfiguration);//,m_flid);
			}
			else
			{
				IRecordClerk clerkProvidingRootObject;
				Debug.Assert(TryClerkProvidingRootObject(out clerkProvidingRootObject),
					"We expected to find clerkProvidingOwner '" + m_clerkProvidingRootObject + "'. Possibly misspelled.");
			}
#endif

#if RANDYTODO
			// TODO: In original, optimized, version, we don't load the data until the clerk is used in a newly activated window.
			SetupDataContext(false);
#else
			SetupDataContext(true);
#endif
		}

		#endregion Implementation of IFlexComponent

		#region Implementation of IPropertyTableProvider

		/// <summary>
		/// Placement in the IPropertyTableProvider interface lets FwApp call IPropertyTable.DoStuff.
		/// </summary>
		public IPropertyTable PropertyTable { get; private set; }

		#endregion Implementation of IPropertyTableProvider

		#region Implementation of IPublisherProvider

		/// <summary>
		/// Get the IPublisher.
		/// </summary>
		public IPublisher Publisher { get; private set; }

		#endregion Implementation of IPublisherProvider

		#region Implementation of ISubscriberProvider

		/// <summary>
		/// Get the ISubscriber.
		/// </summary>
		public ISubscriber Subscriber { get; private set; }

		#endregion Implementation of ISubscriberProvider

		#region Implementation of IRecordListUpdater

		/// <summary>
		/// Set our IRecordChangeHandler object from the argument.
		/// </summary>
		/// <remarks>Part of the IRecordListUpdater interface.</remarks>
		public IRecordChangeHandler RecordChangeHandler
		{
			set
			{
				CheckDisposed();

				if (m_rch != null && !ReferenceEquals(m_rch, value))
				{
					// Store it, since we need to clear out the
					// data member to avoid an infinite loop in calling its Dispose method,
					// which then tries to call this setter with null.
					var goner = m_rch;
					m_rch = null;
					goner.Dispose();
				}
				m_rch = value;
			}
		}

		/// <summary>
		/// refresh current record, by deleting and re-inserting it into our list.
		/// </summary>
		public void RefreshCurrentRecord()
		{
			MyRecordList.ReplaceListItem(CurrentObjectHvo);
		}

		public void UpdateList(bool fRefreshRecord, bool forceSort = false)
		{
			CheckDisposed();

			// By default, we don't force the sort
			if (fRefreshRecord)
			{
				// No need to recursively update the list!
				m_rch?.Fixup(false);
			}
			var fReload = forceSort || MyRecordList.NeedToReloadList();
			if (fReload)
			{
				MyRecordList.ForceReloadList();
			}
		}

		#endregion Implementation of IRecordListUpdater

		#region Implementation of IAnalysisOccurrenceFromHvo

		/// <summary>
		/// If we're wrapping a ConcDecorator, we can extract its AnalysisOccurrence.
		/// </summary>
		public IParaFragment OccurrenceFromHvo(int hvo)
		{
			return (((DomainDataByFlidDecoratorBase)VirtualListPublisher).BaseSda as IAnalysisOccurrenceFromHvo)?.OccurrenceFromHvo(hvo);
		}

		#endregion Implementation of IAnalysisOccurrenceFromHvo

		#region Implementation of IBulkPropChanged

		/// <summary>
		/// Called at the start of broadcasting PropChanged messages, passed the count of changes.
		/// Currently this used so as to not do anything to batch them if there is only one.
		/// </summary>
		public void BeginBroadcastingChanges(int count)
		{
			// If we're starting a multi-property change, suppress list sorting until all the notifications have been received.
			// The null check is just for sanity, it should always be null when starting an operation.
			if (count > 1 && m_bulkEditUpdateHelper == null)
			{
				m_bulkEditUpdateHelper = new ListUpdateHelper(this);
			}
		}

		/// <summary>
		/// Called after broadcasting all changes.
		/// </summary>
		public void EndBroadcastingChanges()
		{
			// If we're ending a bulk edit, end the sorting suppression (if any).
			if (m_bulkEditUpdateHelper == null)
			{
				return;
			}
			m_bulkEditUpdateHelper.Dispose();
			m_bulkEditUpdateHelper = null;
		}

		#endregion Implementation of IBulkPropChanged

		#region Implementation of IVwNotifyChange

		/// <summary>
		/// We watch for changes to DateModified and update the status bar if we are controlling it.
		/// </summary>
		public void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			if (hvo != CurrentObjectHvo)
			{
				return;
			}
			// Check the tag because it might be some fake property known only to a decorator
			if (((IFwMetaDataCacheManaged)Cache.MetaDataCacheAccessor).FieldExists(tag) && Cache.MetaDataCacheAccessor.GetFieldName(tag) == "DateModified" && IsControllingTheRecordTreeBar)
			{
				ResetStatusBarMessageForCurrentObject();
			}
		}

		#endregion Implementation of IVwNotifyChange

		#region Implementation of IDisposable & Co.

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		protected void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException($"'{GetType().Name}' in use after being disposed.");
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
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
				return;


			if (disposing)
			{
				// Dispose managed resources here.
				UnregisterMessageHandlers();
				MyRecordList.ListChanged -= OnListChanged;
				MyRecordList.AboutToReload -= m_list_AboutToReload;
				MyRecordList.DoneReload -= m_list_DoneReload;
				RemoveNotification(); // before disposing list, we need it to get to the Cache.
				MyRecordList.Dispose();
				m_rch?.Dispose();
				m_recordBarHandler?.Dispose();
				if (IsControllingTheRecordTreeBar)
				{
					PropertyTable.RemoveProperty("ActiveClerkSelectedObject");
				}
				PropertyTable.RemoveProperty(ClerkSelectedObjectPropertyId(Id));
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_clerkProvidingRootObject = null;
			m_recordBarHandler = null;
			m_filterProvider = null;
			m_activeMenuBarFilter = null;
			m_rch = null;
			m_fIsActiveInGui = false;
			m_defaultSorter = null;
			m_defaultSortLabel = null;
			m_defaultFilter = null;

			PropertyTable = null;
			Publisher = null;
			Subscriber = null;

			IsDisposed = true;
		}

		#endregion Implementation of IDisposable & Co.

		#endregion All interface implementations

		#region Non-interface code

		internal static string ClerkSelectedObjectPropertyId(string clerkId)
		{
			return clerkId + "-selected";
		}

#if RANDYTODO
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

			// Note from RandyR: One of these days we should probably subclass this object, and perhaps the record list more.
			// The "reversalEntries" clerk wants to handle the message, even though it isn't the primary clerk.
			// The m_shouldHandleDeletion member was also added, so the "reversalEntries" clerk's primary clerk
			// would not handle the message, and delete an entire reversal index.
			if (ShouldNotHandleDeletionMessage)
				return false;

			display.Enabled = m_allowDeletions && m_list.IsCurrentObjectValid() && m_list.CurrentObject != null;
			if (display.Text.Contains("{0}") && m_list.IsCurrentObjectValid())
			{
				// Insert the class name of the thing we will delete
				var obj = m_list.CurrentObject;
				if (obj != null)
				{
					display.Text = String.Format(display.Text, GetTypeNameForUi(obj));
				}
				else
				{
					// Try to get a plausible substitution when the list is empty, so
					// it doesn't look too wierd to the user.
					string className = Cache.MetaDataCacheAccessor.GetClassName(m_list.ListItemsClass);
					string displayName = StringTable.Table.GetString(className, "ClassNames");
					display.Text = String.Format(display.Text, displayName);
				}
			}
			return true;//we handled this, no need to ask anyone else.
		}

		/// <summary>
		/// Figure a tooltop for the DeleteRecord command.
		/// </summary>
		/// <param name="holder"></param>
		/// <returns></returns>
		public bool OnDeleteRecordToolTip(object holder)
		{
			if (ShouldNotHandleDeletionMessage)
				return false;
			var realHolder = (ToolTipHolder)holder;
			if (m_list.IsCurrentObjectValid() && realHolder.ToolTip.Contains("{0}"))
			{
				realHolder.ToolTip = string.Format(realHolder.ToolTip, GetTypeNameForUi(m_list.CurrentObject));
			}
			return true;
		}

		/// <summary>
		/// this is called when XCore wants to display something that relies on the list with the id "FiltersList"
		/// </summary>SuspendLoadingRecordUntilOnJumpToRecord
		/// <param name="parameters"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public bool OnDisplayFiltersList(object parameters, ref UIListDisplayProperties display)
		{
			CheckDisposed();

			//set the filters list property to the one that we are monitoring
			display.PropertyName = CurrentFilterPropertyTableId;

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

		private static void AddFilterChoice(RecordFilter filter, UIListDisplayProperties display)
		{
			string value = filter.id;
			string imageName = filter.imageName;
			// Since we are storing actual, instantiated filter objects, there's no point in also keeping
			// their configuration information separately. Any configuration information they had would
			// have already been sucked in when the filter was created.
			display.List.Add(filter.Name, value, imageName, null);
		}
#endif

		#region Private stuff

		private void SelectedListBarNode_Message_Handler(object obj)
		{
			if (!IsControllingTheRecordTreeBar)
				return;
			var item = PropertyTable.GetValue<ListViewItem>("SelectedListBarNode");
			if (item == null)
				return;
			if (!(item.Tag is int))
				throw new ArgumentException(SelectedListBarNodeErrorMessage);
			var hvo = (int)item.Tag;
			if (CurrentObjectHvo == 0 || hvo != CurrentObjectHvo)
				JumpToRecord(hvo);
		}

		private void SelectedTreeBarNode_Message_Handler(object obj)
		{
			if (!IsControllingTheRecordTreeBar)
				return;
			var node = PropertyTable.GetValue<TreeNode>("SelectedTreeBarNode");
			if (node == null)
				return;
			if (!(node.Tag is int))
				throw new ArgumentException(SelectedListBarNodeErrorMessage);
			var hvo = (int)node.Tag;
			if (CurrentObjectHvo == 0 || hvo != CurrentObjectHvo)
				JumpToRecord(hvo);
		}

		private string SortNamePropertyTableId => MyRecordList.PropertyTableId("sortName");

		private string FilterPropertyTableId => MyRecordList.PropertyTableId("filter");

		private string SorterPropertyTableId => MyRecordList.PropertyTableId("sorter");

		private void SetupDataContext(bool floadList)
		{
			MyRecordList.InitLoad(floadList);

			//NB: we need to be careful
			//not to broadcast any record changes until we are actually initialize enough
			//to deal with the resulting request that will come from those widgets.


			//if we are not dependent on some other selection, then select the first record.
			//			if (m_clerkProvidingRootObject ==null)
			//				this.OnFirstRecord(this);

			//BroadcastChange();

			UpdateOwningObject();
		}

		/// <summary>
		/// Find the index of hvoTarget in m_list; or, if it does not occur, the index of a child of hvoTarget.
		/// </summary>
		/// <param name="hvoTarget"></param>
		/// <returns></returns>
		private int IndexOfObjOrChildOrParent(int hvoTarget)
		{
			var index = MyRecordList.IndexOf(hvoTarget);
			if (index == -1)
			{
				// In case we can't find the argument in the list, see if it is an owner of anything
				// in the list. This is useful, for example, when asked to find a LexEntry in a list of senses.
				index = MyRecordList.IndexOfChildOf(hvoTarget);
			}
			if (index == -1)
			{
				// Still no luck. See if the argument's owner is in the list (e.g., may be a subrecord
				// in DN, and only parent currently showing).
				index = MyRecordList.IndexOfParentOf(hvoTarget);
			}
			return index;
		}

		/// <summary>
		/// Change the list filter to the currently selected (checked) FilterList item.
		/// This selection is stored in the property table based on the name of the filter
		/// associated with the current clerk.
		/// </summary>
		private void OnChangeFilterToCheckedListPropertyChoice()
		{
			string filterName = PropertyTable.GetValue(CurrentFilterPropertyTableId, SettingsGroup.LocalSettings, string.Empty);
			RecordFilter addf = null;
			RecordFilter remf = null;
			var nof = new NoFilters();
			// Check for special cases.
			if (filterName == (new UncheckAll()).Name)
			{
				// we're simply unselecting all items in the list.
				// no need for further changes.
				return;
			}

			if (filterName == nof.Name)
			{
				OnChangeFilterClearAll(null); // get rid of all the ones we're allowed to.
				return;
			}

			if (m_filterProvider != null)
			{
				addf = (RecordFilter)m_filterProvider.GetFilter(filterName);
				if (addf == null || addf is NullFilter)
				{
					// If we have no filter defined for this name, it is effectively another way to turn
					// filters off. Turn off all we can.
					OnChangeFilterClearAll(null); // get rid of all the ones we're allowed to.
					return;
				}
				// If we have a menu-type filter active, remove it. Otherwise don't remove anything.
				remf = m_activeMenuBarFilter;
				m_activeMenuBarFilter = addf is NullFilter ? null : addf;
			}
			OnChangeFilter(new FilterChangeEventArgs(addf, remf));
		}

		private string DependentPropertyName
		{
			get
			{
				if (m_clerkProvidingRootObject != null)
				{
					return ClerkSelectedObjectPropertyId(m_clerkProvidingRootObject.Id);
				}
				//don't do this, because then you die when the debugger tries to show the RecordClerk.
				//Debug.Fail("Why is this property being called when the clerk is not dependent on another clerk?");
				return null;

			}
		}

		private void UpdateOwningObject(bool fUpdateOwningObjectOnlyIfChanged = false)
		{
			if (m_clerkProvidingRootObject == null)
			{
				//if we're not dependent on another clerk, then we don't ever change our owning object.
				return;
			}

			var old = MyRecordList.OwningObject;
			ICmObject newObj = null;
			var rni = PropertyTable.GetValue<RecordNavigationInfo>(DependentPropertyName);
			if (rni != null)
			{
				newObj = rni.Clerk.CurrentObject;
			}
			using (var luh = new ListUpdateHelper(this))
			{
				// in general we want to actually reload the list if something as
				// radical as changing the OwningObject occurs, since many subsequent
				// events and messages depend upon this information.
				luh.TriggerPendingReloadOnDispose = true;
				if (rni != null)
				{
					luh.SkipShowRecord = rni.SkipShowRecord;
				}
				if (!fUpdateOwningObjectOnlyIfChanged || !ReferenceEquals(old, newObj))
				{
					MyRecordList.OwningObject = newObj;
				}
			}
			if (!ReferenceEquals(old, newObj))
			{
				Publisher.Publish("ClerkOwningObjChanged", this);
			}
		}

		private string GetTypeNameForUi(ICmObject obj)
		{
			var poss = obj as ICmPossibility;
			if (poss != null)
			{
				return poss.ItemTypeName();
			}
			var featsys = obj.OwnerOfClass(FsFeatureSystemTags.kClassId) as IFsFeatureSystem;
			if (featsys?.OwningFlid == LangProjectTags.kflidPhFeatureSystem)
			{
				var sClass = StringTable.Table.GetString(obj.ClassName, "ClassNames");
				return StringTable.Table.GetString(sClass + "-Phonological", "AlternativeTypeNames");
			}
			return StringTable.Table.GetString(obj.ClassName, "ClassNames");
		}

		private bool ShouldNotHandleDeletionMessage => Id != "reversalEntries" && (!Editable || !IsPrimaryClerk || !m_shouldHandleDeletion);

		private string CurrentFilterPropertyTableId => "currentFilterForRecordClerk_" + Id;

		private void ResetStatusBarMessageForCurrentObject()
		{
			var msg = string.Empty;
			if (CurrentObjectHvo != 0)
			{
				// deleted objects don't have a cache (and other properties) so it was crashing.  LT-3160, LT-3121,...
				msg = GetStatusBarMsgForCurrentObject();
			}
			StatusBarPanelServices.SetStatusPanelMessage(m_statusBar, msg);
		}

		/// <summary>
		/// Generally only one clerk should respond to record navigation in the user interface.
		/// The "primary" clerk is the one that should respond to record navigation.
		/// </summary>
		/// <returns>true iff this clerk should respond to record navigation</returns>
		private bool IsPrimaryClerk => (m_clerkProvidingRootObject == null);

		/// <summary>
		/// Stop notifications of prop changes
		/// </summary>
		private void AddNotification()
		{
			// We need the list to get the cache.
			if (MyRecordList == null || MyRecordList.IsDisposed || Cache == null || Cache.IsDisposed || Cache.DomainDataByFlid == null)
				return;
			Cache.DomainDataByFlid.AddNotification(this);
		}

		/// <summary>
		/// Stop notifications of prop changes
		/// </summary>
		private void RemoveNotification()
		{
			// We need the list to get the cache.
			if (MyRecordList == null || MyRecordList.IsDisposed || Cache == null || Cache.IsDisposed || Cache.DomainDataByFlid == null)
				return;
			Cache.DomainDataByFlid.RemoveNotification(this);
		}

		private void UnregisterMessageHandlers()
		{
			Subscriber.Unsubscribe("SelectedTreeBarNode", SelectedTreeBarNode_Message_Handler);
			Subscriber.Unsubscribe("SelectedListBarNode", SelectedListBarNode_Message_Handler);
		}

		private void RegisterMessageHandlers()
		{
			var window = PropertyTable.GetValue<IFwMainWnd>("window");
			if (window.TreeStyleRecordList != null)
			{
				Subscriber.Subscribe("SelectedTreeBarNode", SelectedTreeBarNode_Message_Handler);
			}
			if (window.ListStyleRecordList != null)
			{
				Subscriber.Subscribe("SelectedListBarNode", SelectedListBarNode_Message_Handler);
			}
		}

		private void BroadcastChange(bool suppressFocusChange)
		{
			ClearInvalidSubitem();
			if (CurrentObjectHvo != 0 && !MyRecordList.CurrentObjectIsValid)
			{
				MessageBox.Show(xWorksStrings.SelectedObjectHasBeenDeleted,
					xWorksStrings.DeletedObjectDetected,
					MessageBoxButtons.OK, MessageBoxIcon.Warning);
				m_fReloadingDueToMissingObject = true;
				try
				{
					int idx = MyRecordList.CurrentIndex;
					int cobj = ListSize;
					if (cobj == 0)
					{
						MyRecordList.CurrentIndex = -1;
					}
					else
					{
						MyRecordList.CurrentIndex = FindClosestValidIndex(idx, cobj);
					}
					Publisher.Publish("StopParser", null);  // stop parser if it's running.
				}
				finally
				{
					m_fReloadingDueToMissingObject = false;
				}
				return; // typically leads to another call to this.
			}
			SelectedRecordChanged(suppressFocusChange);
		}

		private int FindClosestValidIndex(int idx, int cobj)
		{
			for (int i = idx + 1; i < cobj; ++i)
			{
				var item = (IManyOnePathSortItem)MyRecordList.SortedObjects[i];
				if (!Cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(item.KeyObject).IsValidObject)
					continue;
				bool fOk = true;
				for (int j = 0; fOk && j < item.PathLength; j++)
					fOk = Cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(item.PathObject(j)).IsValidObject;
				if (fOk)
					return i;
			}
			for (int i = idx - 1; i >= 0; --i)
			{
				var item = (IManyOnePathSortItem)MyRecordList.SortedObjects[i];
				if (!Cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(item.KeyObject).IsValidObject)
					continue;
				bool fOk = true;
				for (int j = 0; fOk && j < item.PathLength; j++)
					fOk = Cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(item.PathObject(j)).IsValidObject;
				if (fOk)
					return i;
			}
			return -1;
		}

		private bool IgnoreStatusPanel { get; set; }

		private void UpdateSortStatusBarPanel()
		{
			if (!IsControllingTheRecordTreeBar)
			{
				return; // none of our business!
			}
			if (IgnoreStatusPanel)
			{
				return;
			}

			var newSortMessage = MyRecordList.Sorter == null || SortName == null || (m_isDefaultSort && m_defaultSorter != null) ? string.Empty : string.Format(xWorksStrings.SortedBy, SortName);
			StatusBarPanelServices.SetStatusBarPanelSort(m_statusBar, newSortMessage);
		}

		private void m_list_AboutToReload(object sender, EventArgs e)
		{
			// This used to be a BroadcastMessage, but now broadcast is deferred.
			// To keep the same logic it's now using the SendMessageToAllNow.  This
			// is different from SendMessage as it is sent to all even if handled.
			// To avoid hitting the " For now, we'll not try to be concerned about restoring scroll position
			// in a context where we're reloading after suppressing a reload.
			if (!m_fReloadingDueToMissingObject)
			{
				Publisher.Publish("SaveScrollPosition", this);
			}
		}

		private void m_list_DoneReload(object sender, EventArgs e)
		{
			// This used to be a BroadcastMessage, but now broadcast is deferred.
			// To keep the same logic it's now using the SendMessageToAllNow.  This
			// is different from SendMessage as it is sent to all even if handled.
			if (!m_fReloadingDueToMissingObject)
			{
				Publisher.Publish("RestoreScrollPosition", this);
			}
		}

		#endregion Private stuff

		#region Protected stuff

		/// <summary>
		/// Override this (initially only in InterlinTextsRecordClerk) if the clerk knows how to add an
		/// item to the current list/filter on request.
		/// </summary>
		protected virtual bool AddItemToList(int hvoItem)
		{
			return false;
		}

		/// <summary />
		/// <returns><c>true</c> if we changed or initialized a new filter,
		/// <c>false</c> if the one installed matches the one we had stored to persist.</returns>
		protected virtual bool TryRestoreFilter()
		{
			RecordFilter filter = null;
			var persistFilter = PropertyTable.GetValue<string>(FilterPropertyTableId, SettingsGroup.LocalSettings);
			if (MyRecordList.Filter != null)
			{
				// if the persisted object string of the existing filter matches the one in the property table
				// do nothing.
				var currentFilter = DynamicLoader.PersistObject(MyRecordList.Filter, "filter");
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
						filter.Cache = Cache;
					}
				}
				catch
				{
					filter = null; // If anything goes wrong, ignore the persisted value.
				}
			}
			if (filter == null || !filter.IsValid)
			{
				filter = m_defaultFilter;
			}
			if (MyRecordList.Filter == filter)
				return false;
			MyRecordList.Filter = filter;
			return true;
		}

		/// <summary>
		///
		/// </summary>
		/// <returns><c>true</c> if we changed or initialized a new sorter,
		/// <c>false</c>if the one installed matches the one we had stored to persist.</returns>
		protected virtual bool TryRestoreSorter()
		{
			SortName = PropertyTable.GetValue<string>(SortNamePropertyTableId, SettingsGroup.LocalSettings);

			var persistSorter = PropertyTable.GetValue<string>(SorterPropertyTableId, SettingsGroup.LocalSettings, null);
			if (MyRecordList.Sorter != null)
			{
				// if the persisted object string of the existing sorter matches the one in the property table
				// do nothing
				var currentSorter = DynamicLoader.PersistObject(MyRecordList.Sorter, "sorter");
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
				sorter = m_defaultSorter;
				SortName = m_defaultSortLabel;
			}
			// If sorter is still null, allow any sorter which may have been installed during
			// record list initialization to prevail.
			if (sorter == null)
			{
				return false; // we didn't change anything.
			}
			// (LT-9515) restored sorters need to set some properties that could not be persisted.
			var cache = PropertyTable.GetValue<LcmCache>("cache");
			sorter.Cache = cache;
			if (sorter is GenRecordSorter)
			{
				var comparer = ((GenRecordSorter)sorter).Comparer;
				WritingSystemComparer subComparer = null;
				if (comparer != null)
					subComparer = ((StringFinderCompare)comparer).SubComparer as WritingSystemComparer;
				if (subComparer != null)
				{
					var subComparerWsId = subComparer.WsId;
					var wsId = cache.WritingSystemFactory.GetWsFromStr(subComparerWsId);
					if (wsId == 0)
						return false;
				}
			}
			if (MyRecordList.Sorter == sorter)
				return false;
			// (LT-9515) restored sorters need to set some properties that could not be persisted.
			MyRecordList.Sorter = sorter;
			MyRecordList.TransferOwnership(sorter as IDisposable);
			return true;
		}

		/// <summary>
		/// True if our clerk is the active clerk.
		/// </summary>
		protected internal bool IsActiveClerk
		{
			get
			{
				if (PropertyTable == null)
					return false;
				var activeClerk = ActiveRecordClerkRepository.ActiveRecordClerk;
				return activeClerk != null && activeClerk.Id == Id;
			}
		}

		/// <summary>
		/// determine if we're in the (given) tool
		/// </summary>
		/// <param name="desiredTool"></param>
		/// <returns></returns>
		protected bool InDesiredTool(string desiredTool)
		{
			string toolChoice = PropertyTable.GetValue<string>("toolChoice");
			return toolChoice != null && toolChoice == desiredTool;
		}

		/// <summary>
		/// determine if we're in the (given) area
		/// </summary>
		/// <param name="desiredArea">The desired area.</param>
		/// <returns></returns>
		protected bool InDesiredArea(string desiredArea)
		{
			var areaChoice = PropertyTable.GetValue<string>("areaChoice");
			return areaChoice != null && areaChoice == desiredArea;
		}

		/// <summary>
		/// By default DeleteRecord deletes the current record. Override if you need to delete something else.
		/// For example, in interlinear text we delete the owning Text.
		/// </summary>
		protected virtual ICmObject GetObjectToDelete(ICmObject currentObject)
		{
			return currentObject;
		}

		/// <summary>
		/// By default we just silently don't delete things that shouldn't be. Override if you want to give a message.
		/// </summary>
		protected virtual void ReportCannotDelete()
		{
		}

		/// <summary>
		/// Override this if there are special cases where you need more control over which objects can be deleted.
		/// </summary>
		/// <returns></returns>
		protected virtual bool CanDelete()
		{
			return CurrentObject.CanDelete;
		}

		protected virtual string GetStatusBarMsgForCurrentObject()
		{
			string msg;
			if (!Cache.ServiceLocator.IsValidObjectId(CurrentObjectHvo))
			{
				msg = xWorksStrings.ADeletedObject;
			}
			else
			{
				using (CmObjectUi uiObj = CmObjectUi.MakeUi(CurrentObject))
				{
					uiObj.InitializeFlexComponent(new FlexComponentParameters(PropertyTable, Publisher, Subscriber));
					msg = uiObj.ToStatusBar();
				}
			}
			return msg;
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
					if (m_recordBarHandler is TreeBarHandler && MyRecordList.CurrentObject != null &&
						(MyRecordList.CurrentObject.Cache != null || MyRecordList.SortedObjects.Count != 1))
					{
						// all we need to do is replace the currently selected item in the tree.
						var hvoItem = arguments.ItemHvo;
						ICmObject obj = null;
						if (hvoItem != 0)
							Cache.ServiceLocator.GetInstance<ICmObjectRepository>().TryGetObject(hvoItem, out obj);
						if (obj == null)
							obj = MyRecordList.CurrentObject;
						m_recordBarHandler.ReloadItem(obj);
					}
				}
				else if (m_recordBarHandler != null)
				{
					m_recordBarHandler.PopulateRecordBar(MyRecordList);
				}
			}

			if (arguments.Actions == ListChangedEventArgs.ListChangedActions.SkipRecordNavigation ||
				arguments.Actions == ListChangedEventArgs.ListChangedActions.UpdateListItemName)
			{
				SelectedRecordChanged(false, true);
			}
			else if (arguments.Actions == ListChangedEventArgs.ListChangedActions.SuppressSaveOnChangeRecord)
			{
				bool oldSuppressSaveChangeOnRecord = SuppressSaveOnChangeRecord;
				SuppressSaveOnChangeRecord = true;
				try
				{
					BroadcastChange(false);
				}
				finally
				{
					SuppressSaveOnChangeRecord = oldSuppressSaveChangeOnRecord;
				}
			}
			else if (arguments.Actions == ListChangedEventArgs.ListChangedActions.Normal)
			{
				BroadcastChange(false);
			}
			else
			{
				throw new NotImplementedException("An enum choice for ListChangedEventArgs was selected that OnListChanged is not aware of.");
			}
		}

		/// <summary>
		/// A hook to allow a subclass to remove an invalid subitem.
		/// </summary>
		protected virtual void ClearInvalidSubitem()
		{
		}

		/// <summary>
		/// Handles refreshing the record list after an object was deleted.
		/// </summary>
		/// <remarks>This should be overriden to perform more efficient refreshing of the record list display</remarks>
		protected virtual void RefreshAfterInvalidObject()
		{
			// to be safe we just do a full refresh
			PropertyTable.GetValue<IApp>("App").RefreshAllViews();
		}

		protected virtual void OnSelectedObjectChanged(SelectObjectEventArgs e)
		{
			SelectedObjectChanged?.Invoke(this, e);
		}

		protected virtual void OnRecordChanged(RecordNavigationEventArgs e)
		{
			RecordChanged?.Invoke(this, e);
		}

		/// <summary>
		/// Figure out what should show in the filter status panel and make it so.
		/// </summary>
		protected void UpdateFilterStatusBarPanel()
		{
			if (!IsControllingTheRecordTreeBar)
				return; // none of our business!

			var msg = FilterStatusContents(MyRecordList.Filter != null && MyRecordList.Filter.IsUserVisible) ?? string.Empty;
			if (IgnoreStatusPanel)
			{
				Publisher.Publish("DialogFilterStatus", msg);
			}
			else
			{
				StatusBarPanelServices.SetStatusBarPanelFilter(m_statusBar, msg);
			}
		}

		protected virtual string FilterStatusContents(bool listIsFiltered)
		{
			return listIsFiltered ? xWorksStrings.Filtered : string.Empty;
		}

		#endregion Protected stuff
		#endregion Non-interface code
	}
}
