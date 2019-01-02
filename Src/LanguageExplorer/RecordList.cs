// Copyright (c) 2004-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using LanguageExplorer.Areas;
using LanguageExplorer.Controls.XMLViews;
using LanguageExplorer.Filters;
using LanguageExplorer.LcmUi;
using LanguageExplorer.LcmUi.Dialogs;
using SIL.Code;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainImpl;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Utils;
using SIL.Reporting;

namespace LanguageExplorer
{
	/// <summary>
	/// RecordList is a vector of objects
	/// </summary>
	internal class RecordList : IRecordList
	{
		#region Data members
		protected const int RecordListFlid = 89999956;
		/// <summary>
		/// we want to keep a reference to our window, separate from the PropertyTable
		/// in case there's a race condition during dispose with the window
		/// getting removed from the PropertyTable before it's been disposed.
		/// </summary>
		private Form m_windowPendingOnActivated;
		protected LcmCache m_cache;
		/// <summary>
		/// Use this to do the Add/RemoveNotifications, since it can be used in the unmanaged section of Dispose.
		/// (If m_sda is COM, that is.)
		/// Doing it there will be safer, since there was a risk of it not being removed
		/// in the mananaged section, as when disposing was done by the Finalizer.
		/// </summary>
		private ISilDataAccess m_sda;
		/// <summary>
		/// A reference to a sorter.
		/// </summary>
		protected RecordSorter m_sorter;
		/// <summary>
		/// A reference to a filter.
		/// </summary>
		protected RecordFilter m_filter;
		protected RecordFilter m_filterPrev;
		protected string m_fontName;
		protected int m_typeSize = 10;
		protected bool m_reloadingList;
		private bool m_suppressingLoadList;
		protected bool m_requestedLoadWhileSuppressed;
		protected bool m_deletingObject;
		protected int m_oldLength;
		protected bool m_usingAnalysisWs;
		/// <summary>
		/// The actual database flid from which we get our list of objects, and apply a filter to.
		/// </summary>
		protected int m_flid;
		/// <summary>
		/// This is true if the list is the LexDb/LexEntries, and one of the entries has
		/// changed.
		/// </summary>
		protected bool m_fReloadLexEntries;
		/// <summary>
		/// Collection of ClassAndPropInfo objects.
		/// </summary>
		protected List<ClassAndPropInfo> m_insertableClasses;
		/// <summary>
		/// Index of the currently selected item. May be -1 to indicate that no item is selected
		/// (perhaps because the list is empty).
		/// Note: MUST always update this through the CurrentIndex setter, so that m_hvoCurrent
		/// is kept in sync.
		/// </summary>
		protected int m_currentIndex = -1;
		private int m_indexToRestoreDuringReload = -1;
		/// <summary>
		/// Basically RootObjectAt(m_currentIndex) (unless m_currentIndex is -1, when it is zero).
		/// We maintain this so we can detect when PropChanged() represents a deletion of the current
		/// object, allowing us to update the list more efficiently than reloading it.
		/// </summary>
		protected int m_hvoCurrent;
		/// <summary>
		/// This is now a collection of ManyOnePathSortItems (JohnT, 3 June 05).
		/// </summary>
		/// <remarks>
		/// It is tempting to use List&lt;ManyOnePathSortItems&gt;, but it won't work,
		/// since the sorter classes are stuck on the idea of getting an Arraylist,
		/// and they deal with various kinds of things.
		/// </remarks>
		protected ArrayList m_sortedObjects;
		/// <summary>
		/// This is the database object whose vector we are editing.
		/// </summary>
		protected ICmObject m_owningObject;
		/// <summary>
		/// This enables/disables SendPropChangedOnListChange().  The default is enabled (true).
		/// </summary>
		protected bool m_fEnableSendPropChanged = true;
		/// <summary>
		/// This enables/disables filtering independent of assigning a filter.  The default is enabled (true).
		/// </summary>
		protected bool m_fEnableFilters = true;
		protected StatusBar _statusBar;
		/// <summary />
		protected IRecordChangeHandler _recordChangeHandler;
		private ListUpdateHelper _bulkEditListUpdateHelper;
		private const string SelectedListBarNodeErrorMessage = "An item stored in the Property Table under SelectedListBarNode (typically from the ListView of an xWindow's record bar) should have an Hvo stored in its Tag property.";
		private bool _isDefaultSort;
		private RecordSorter _defaultSorter;
		private bool _reloadingDueToMissingObject;
		/// <summary>
		/// We need to store what filter we are responsible for setting, locally, so
		/// that when the user says "no filter/all records",
		/// we can selectively remove just this filter from the set that is being kept by the
		/// RecordList. That list would contain filters contributed from other sources, in particular
		/// the FilterBar.
		/// </summary>
		protected RecordFilter _activeMenuBarFilter;
		/// <summary>
		/// false, if the dependent record list is to handle deletion, as for reversals.
		/// </summary>
		private bool _shouldHandleDeletion = true;
		/// <summary>
		/// this is an object which gives us the list of filters which we should offer to the user from the UI.
		/// this does not include the filters they can get that by using the FilterBar.
		/// </summary>
		protected IRecordFilterListProvider _filterProvider;
		private RecordFilter _defaultFilter;
		private string _defaultSortLabel;
		/// <summary>
		/// true during delete and insert and ShowRecord calls caused by them.
		/// </summary>
		private bool _suppressSaveOnChangeRecord;
		/// <summary>
		/// All of the sorters for the record list.
		/// </summary>
		protected Dictionary<string, PropertyRecordSorter> _allSorters;
		private EditFilterMenuHandler _editFilterMenuHandler;
		private Dictionary<Navigation, bool> _canMoveTo = new Dictionary<Navigation, bool>
		{
			{ Navigation.First, false },
			{ Navigation.Next, false },
			{ Navigation.Previous, false },
			{ Navigation.Last, false }
		};

		#endregion Data members

		#region Constructors

		protected RecordList(ISilDataAccessManaged decorator)
		{
			Guard.AgainstNull(decorator, nameof(decorator));

			VirtualListPublisher = new ObjectListPublisher(decorator, RecordListFlid);
		}

		internal RecordList(string id, StatusBar statusBar, ISilDataAccessManaged decorator, bool usingAnalysisWs, VectorPropertyParameterObject vectorPropertyParameterObject, RecordFilterParameterObject recordFilterParameterObject = null, RecordSorter defaultSorter = null)
			: this(decorator)
		{
			Guard.AgainstNullOrEmptyString(id, nameof(id));
			Guard.AgainstNull(statusBar, nameof(statusBar));
			Guard.AgainstNull(vectorPropertyParameterObject, nameof(vectorPropertyParameterObject));

			if (recordFilterParameterObject == null)
			{
				recordFilterParameterObject = new RecordFilterParameterObject();
			}
			Id = id;
			_statusBar = statusBar;
			_defaultSorter = defaultSorter ?? new PropertyRecordSorter(AreaServices.ShortName);
			_defaultSortLabel = AreaServices.Default;
			_defaultFilter = recordFilterParameterObject.DefaultFilter;
			Editable = recordFilterParameterObject.AllowDeletions;
			_shouldHandleDeletion = recordFilterParameterObject.ShouldHandleDeletion;
			m_owningObject = vectorPropertyParameterObject.Owner;
			PropertyName = vectorPropertyParameterObject.PropertyName;
			m_flid = vectorPropertyParameterObject.Flid;
			m_usingAnalysisWs = usingAnalysisWs;
			m_oldLength = 0;
		}

		internal RecordList(string id, StatusBar statusBar, ISilDataAccessManaged decorator, bool usingAnalysisWs, VectorPropertyParameterObject vectorPropertyParameterObject, Dictionary<string, PropertyRecordSorter> sorters, RecordFilterParameterObject recordFilterParameterObject = null)
			: this(id, statusBar, decorator, usingAnalysisWs, vectorPropertyParameterObject, recordFilterParameterObject, sorters[AreaServices.Default])
		{
			Guard.AgainstNull(sorters, nameof(sorters));

			_allSorters = sorters;
		}

		#endregion Construction

		#region All interface implementations

		#region Implementation of IAnalysisOccurrenceFromHvo nested in IRecordList

		public IParaFragment OccurrenceFromHvo(int hvo)
		{
			return (((DomainDataByFlidDecoratorBase)VirtualListPublisher).BaseSda as IAnalysisOccurrenceFromHvo)?.OccurrenceFromHvo(hvo);
		}

		#endregion Implementation of IAnalysisOccurrenceFromHvo nested in IRecordList

		#region Implementation of IBulkPropChanged nested in IRecordList

		public void BeginBroadcastingChanges(int count)
		{
			// If we're starting a multi-property change, suppress list sorting until all the notifications have been received.
			// The null check is just for sanity, it should always be null when starting an operation.
			if (count > 1 && _bulkEditListUpdateHelper == null)
			{
				_bulkEditListUpdateHelper = new ListUpdateHelper(new ListUpdateHelperParameterObject { MyRecordList = this });
			}
		}

		public void EndBroadcastingChanges()
		{
			// If we're ending a bulk edit, end the sorting suppression (if any).
			if (_bulkEditListUpdateHelper == null)
			{
				return;
			}
			_bulkEditListUpdateHelper.Dispose();
			_bulkEditListUpdateHelper = null;
		}

		#endregion Implementation of IBulkPropChanged nested in IRecordList

		#region Implementation of IDisposable nested in IRecordList

		~RecordList()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected bool IsDisposed { get; set; }

		protected virtual void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				_editFilterMenuHandler?.Dispose();
				UnregisterMessageHandlers();
				RemoveNotification(); // before disposing list, we need it to get to the Cache.
				if (m_cache != null && RecordedFocusedObject != null)
				{
					m_cache.ServiceLocator.ObjectRepository.RemoveFocusedObject(RecordedFocusedObject);
				}
				RecordedFocusedObject = null;
				// make sure we uninstall any remaining event handler,
				// irrespective of whether we're active or not.
				if (m_windowPendingOnActivated != null && !m_windowPendingOnActivated.IsDisposed)
				{
					UninstallWindowActivated();
				}
				m_sda?.RemoveNotification(this);
				m_insertableClasses?.Clear();
				m_sortedObjects?.Clear();
				_recordChangeHandler?.Dispose();
				_bulkEditListUpdateHelper?.Dispose();
				if (IsControllingTheRecordTreeBar)
				{
					PropertyTable.RemoveProperty("ActiveListSelectedObject");
				}
				PropertyTable.RemoveProperty(RecordListSelectedObjectPropertyId(Id));
			}

			m_sda = null;
			m_cache = null;
			m_sorter = null;
			m_filter = null;
			m_owningObject = null;
			PropertyName = null;
			m_fontName = null;
			m_insertableClasses = null;
			m_sortedObjects = null;
			m_owningObject = null;
			VirtualListPublisher = null;
			_statusBar = null;
			_recordChangeHandler = null;
			_bulkEditListUpdateHelper = null;
			_defaultSorter = null;
			_defaultFilter = null;
			_editFilterMenuHandler = null;

			PropertyTable = null;
			Publisher = null;
			Subscriber = null;

			IsDisposed = true;
		}

		#endregion Implementation of IDisposable nested in IRecordList

		#region Implementation of IFlexComponent

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		public virtual void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			FlexComponentParameters.CheckInitializationValues(flexComponentParameters, new FlexComponentParameters(PropertyTable, Publisher, Subscriber));

			PropertyTable = flexComponentParameters.PropertyTable;
			Publisher = flexComponentParameters.Publisher;
			Subscriber = flexComponentParameters.Subscriber;

			m_cache = PropertyTable.GetValue<LcmCache>(LanguageExplorerConstants.cache);
			CurrentIndex = -1;
			m_hvoCurrent = 0;
			m_oldLength = 0;
			var wsContainer = m_cache.ServiceLocator.WritingSystems;
			m_fontName = m_usingAnalysisWs ? wsContainer.DefaultAnalysisWritingSystem.DefaultFontName : wsContainer.DefaultVernacularWritingSystem.DefaultFontName;
			m_typeSize = FontHeightAdjuster.GetFontHeightFromStylesheet(m_cache, PropertyTable, m_usingAnalysisWs);
			if (m_owningObject != null)
			{
				UpdatePrivateList();
			}
			TryRestoreSorter();
			TryRestoreFilter();
			var setFilterMenu = false;
			if (_filterProvider != null)
			{
				// There is only one record list (concordanceWords) that sets _filterProvider to a provider.
				// That provider is the class WfiRecordFilterListProvider.
				// That record list is used in these tools: AnalysesTool, BulkEditWordformsTool, & WordListConcordanceTool
				// That WfiRecordFilterListProvider instance is (ok: "will be") provided in one of the RecordList contructor overloads.
				if (Filter != null)
				{
					// There is only one record list (concordanceWords) that sets _filterProvider to a provider.
					// That record list is used in these tools: AnalysesTool, BulkEditWordformsTool, & WordListConcordanceTool
					// find any matching persisted menubar filter
					// NOTE: for now assume we can only set/persist one such menubar filter at a time.
					foreach (RecordFilter menuBarFilterOption in _filterProvider.Filters)
					{
						if (!Filter.Contains(menuBarFilterOption))
						{
							continue;
						}
						_activeMenuBarFilter = menuBarFilterOption;
						_filterProvider.OnAdjustFilterSelection(_activeMenuBarFilter);
						PropertyTable.SetDefault(CurrentFilterPropertyTableId, _activeMenuBarFilter.id);
						setFilterMenu = true;
						break;
					}
				}
			}
			if (!setFilterMenu)
			{
				OnAdjustFilterSelection();
			}
#if RANDYTODO
			//we handled the tree bar only if we are the root record list
			if (m_recordListProvidingRootObject == null)
			{
				m_recordBarHandler = RecordBarHandler.Create(PropertyTable, m_clerkConfiguration);//,m_flid);
			}
			else
			{
				IRecordList recordListProvidingRootObject;
				Debug.Assert(TryListProvidingRootObject(out recordListProvidingRootObject), "We expected to find recordListProvidingOwner '" + m_recordListProvidingRootObject + "'. Possibly misspelled.");
			}
#endif
#if RANDYTODO
			// TODO: In original, optimized, version, we don't load the data until the record list is used in a newly activated window.
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

		#region Implementation of IRecordListUpdater nested in IRecordList

		/// <summary>Set the IRecordChangeHandler object for this list.</summary>
		public IRecordChangeHandler RecordChangeHandler { get; set; }

		/// <summary>Update the list, possibly calling IRecordChangeHandler.Fixup() first.
		/// </summary>
		public void UpdateList(bool fRefreshRecord, bool forceSort = false)
		{
			// By default, we don't force the sort
			if (fRefreshRecord)
			{
				// No need to recursively update the list!
				_recordChangeHandler?.Fixup(false);
			}
			var fReload = forceSort || NeedToReloadList();
			if (fReload)
			{
				ForceReloadList();
			}
		}

		/// <summary>
		/// just update the current record
		/// </summary>
		public void RefreshCurrentRecord()
		{
			ReplaceListItem(CurrentObjectHvo);
		}

		#endregion Implementation of IRecordListUpdater nested in IRecordList

		#region Implementation of ISortItemProvider

		/// <summary>
		/// Get the nth item in the main list.
		/// </summary>
		public IManyOnePathSortItem SortItemAt(int index)
		{
			return SortedObjects.Count == 0 ? null : SortedObjects[index] as IManyOnePathSortItem;
		}

		/// <summary>
		/// An item is being added to your master list. Add the corresponding sort items to
		/// your fake list.
		/// </summary>
		/// <returns>the number of items added.</returns>
		public int AppendItemsFor(int hvo)
		{
			var start = SortedObjects.Count;
			var result = MakeItemsFor(SortedObjects, hvo);
			if (result != 0)
			{
				var newItems = new int[result];
				for (var i = 0; i < result; i++)
				{
					newItems[i] = (SortedObjects[i + start] as IManyOnePathSortItem).RootObjectHvo;
				}
				((ObjectListPublisher)VirtualListPublisher).Replace(m_owningObject.Hvo, start, newItems, 0);
			}
			return result;
		}

		/// <summary>
		/// Remove the corresponding list items. And issue propchanged.
		/// </summary>
		public void RemoveItemsFor(int hvoToRemove)
		{
			ReplaceListItem(null, hvoToRemove, false);
			SendPropChangedOnListChange(CurrentIndex, SortedObjects, ListChangedActions.Normal);
		}

		/// <summary>
		/// get the index of the given hvo, where it occurs as the root object
		/// (of a IManyOnePathSortItem) in m_sortedObjects.
		/// </summary>
		/// <param name="hvo"></param>
		/// <returns>-1 if the object is not in the list</returns>
		public int IndexOf(int hvo)
		{
			// Creating a list item and then jumping to the tool can leave the list stale, putting us
			// in a bogus state where the edit pane never gets painted.  (See LT-7580.)
			if (RequestedLoadWhileSuppressed)
			{
				ReloadList();
			}
			return IndexOf(SortedObjects, hvo);
		}

		/// <summary>
		/// get the class of the items in this list.
		/// </summary>
		public virtual int ListItemsClass => VirtualListPublisher.MetaDataCache.GetDstClsId(m_flid);

		#endregion Implementation of ISortItemProvider

		#region Implementation of IVwNotifyChange

		public virtual void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			if (UpdatingList || m_reloadingList)
			{
				return; // we're already in the process of changing our list.
			}
			// If this list contains WfiWordforms, and the tag indicates a change to the
			// virtual property of all wordforms, then assume this list also is affected
			// and pretend that this PropChanged is really for us.  See FWR-3617 for
			// justification of this assumption.
			if (m_owningObject != null && hvo == m_owningObject.Hvo && tag != m_flid && cvIns > 0)
			{
				if (ListItemsClass == WfiWordformTags.kClassId && hvo == m_cache.LangProject.Hvo && tag == m_cache.ServiceLocator.GetInstance<Virtuals>().LangProjectAllWordforms)
				{
					tag = m_flid;
				}
			}
			if (TryHandleUpdateOrMarkPendingReload(hvo, tag, ivMin, cvIns, cvDel))
			{
				return;
			}
			// Try to catch things that don't obviously affect us, but will cause problems.
			TryReloadForInvalidPathObjectsOnCurrentObject(tag, cvDel);
			(VirtualListPublisher as IVwNotifyChange)?.PropChanged(hvo, tag, ivMin, cvIns, cvDel);
		}

		#endregion Implementation of IVwNotifyChange

		#region Implementation of ISubscriberProvider

		/// <summary>
		/// Get the ISubscriber.
		/// </summary>
		public ISubscriber Subscriber { get; private set; }

		#endregion Implementation of ISubscriberProvider

		#region Implementation of IRecordList

		public event FilterChangeHandler FilterChangedByList;
		public event RecordNavigationInfoEventHandler RecordChanged;
		public event SelectObjectEventHandler SelectedObjectChanged;
		public event EventHandler SorterChangedByList;

		public virtual void ActivateUI(bool updateStatusBar = true)
		{
			if (PropertyTable.GetValue<IRecordListRepository>(LanguageExplorerConstants.RecordListRepository).ActiveRecordList != this)
			{
				RecordListServices.SetRecordList(PropertyTable.GetValue<Form>(FwUtils.window).Handle, this);
			}
			if (IsActiveInGui)
			{
				return; // Only do it once.
			}
			IsActiveInGui = true;
			SetupFilterMenu();
			ReloadIfNeeded();
			RegisterMessageHandlers();
			AddNotification();
			ActivateRecordBar();
			if (!updateStatusBar)
			{
				return;
			}
			UpdateFilterStatusBarPanel();
			UpdateSortStatusBarPanel();
			// Enable in next commit:
			// 1. RecordList needs MajorFlexComponentParameters to feed to next method, or perhaps some new param object with the three required bits.
			//RecordListServices.SetRecordList();
		}

		public bool AreCustomFieldsAProblem(int[] clsids)
		{
			var mdc = m_cache.GetManagedMetaDataCache();
			var rePunct = new Regex(@"\p{P}");
			foreach (var clsid in clsids)
			{
				var flids = mdc.GetFields(clsid, true, (int)CellarPropertyTypeFilter.All);
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
					var msg = string.Format(LanguageExplorerResources.PunctInFieldNameWarning, name);
					// The way this is worded, 'Yes' means go on with the export. We won't bother them reporting
					// other messed-up fields. A 'no' answer means don't continue, which means it's a problem.
					return (MessageBox.Show(Form.ActiveForm, msg, LanguageExplorerResources.PunctInfieldNameCaption, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes);
				}
			}
			return false; // no punctuation in custom fields.
		}

		public virtual void BecomeInactive()
		{
			TearDownFilterMenu();
			UnregisterMessageHandlers(); // No sense handling messages, when dormant.
			IsActiveInGui = false;
			RemoveNotification();
			// If list loading was suppressed by this view (e.g., bulk edit to prevent changed items
			// disappearing from filter), stop that now, so it won't affect any future use of the list.
			ListLoadingSuppressed = false;
		}

		/// <summary>
		/// Get whether the Clear filter toolbar button can be enabled, or not.
		/// </summary>
		public bool CanChangeFilterClearAll => IsPrimaryRecordList && Filter != null && Filter.IsUserVisible;

		public IReadOnlyDictionary<Navigation, bool> CanMoveToOptions()
		{
			var currentIndex = CurrentIndex;
			foreach (var key in _canMoveTo.Keys.ToList())
			{
				bool canMoveTo;
				switch (key)
				{
					case Navigation.First:
						var firstItemIndex = FirstItemIndex;
						_canMoveTo[key] = firstItemIndex != -1 && firstItemIndex != currentIndex;
						break;
					case Navigation.Next:
						var nextItemIndex = NextItemIndex;
						_canMoveTo[key] = nextItemIndex != -1 && nextItemIndex != currentIndex;
						break;
					case Navigation.Previous:
						var prevItemIndex = PrevItemIndex;
						_canMoveTo[key] = prevItemIndex != -1 && prevItemIndex != currentIndex;
						break;
					case Navigation.Last:
						var lastItemIndex = LastItemIndex;
						_canMoveTo[key] = lastItemIndex != -1 && lastItemIndex != currentIndex;
						break;
					default:
						throw new IndexOutOfRangeException($"I don't know if one can move to '{key}'.");
				}
			}
			return _canMoveTo;
		}

		/// <summary>
		/// will return -1 if the vector is empty.
		/// </summary>
		public virtual int CurrentIndex
		{
			get
			{
				// This makes it self-correcting. Somehow it can get thrown off, when switching views VERY fast.
				if (m_sortedObjects == null || m_sortedObjects.Count == 0)
				{
					if (m_currentIndex != -1)
					{
						Debug.WriteLine("RecordList index should be negative one since vector is empty");
						CurrentIndex = -1;
					}
				}
				else
				{
					if (m_currentIndex < 0 || m_currentIndex >= m_sortedObjects.Count)
					{
						Debug.WriteLine("RecordList index out of range");
						CurrentIndex = 0;
					}
				}
				return m_currentIndex;
			}
			set
			{
				if (value < -1 || value >= (m_sortedObjects?.Count ?? 0))
				{
					throw new IndexOutOfRangeException();
				}
				// We don't want multiple log entries for the same record, and it seems this frequently
				// gets called repeatedly for the same one. We check both the index and the object,
				// because it's possible with deletions or insertions that the current object changes
				// even though the index does not.
				var hvoCurrent = value < 0 ? 0 : SortItemAt(value).RootObjectHvo;
				if (m_currentIndex == value && m_hvoCurrent == hvoCurrent)
				{
					return;
				}
				m_currentIndex = value;
				m_hvoCurrent = hvoCurrent;
				ICmObject newFocusedObject = null;
				if (m_currentIndex < 0)
				{
					Logger.WriteEvent("No current record");
				}
				else
				{
					if (CurrentObjectHvo != 0)
					{
						if (IsCurrentObjectValid())
						{
							newFocusedObject = m_cache.ServiceLocator.GetObject(CurrentObjectHvo);
							Logger.WriteEvent("Current Record = " + newFocusedObject.ShortName);
						}
						else
						{
							Logger.WriteEvent("Current Record not valid or dummy: " + CurrentObjectHvo);
						}
					}
					else
					{
						Logger.WriteEvent("No Current Record");
					}
				}
				if (newFocusedObject == RecordedFocusedObject)
				{
					return;
				}
				var repo = m_cache.ServiceLocator.ObjectRepository;
				repo.RemoveFocusedObject(RecordedFocusedObject);
				RecordedFocusedObject = newFocusedObject;
				repo.AddFocusedObject(RecordedFocusedObject);
			}
		}

		/// <summary>
		/// The currently selected object. Use with care...will do suboptimal things if using fake HVOs.
		/// Use CurrentObjectHvo if it will serve.
		/// </summary>
		public ICmObject CurrentObject
		{
			get
			{
				var hvo = CurrentObjectHvo;
				if (hvo <= 0)
				{
					return null;
				}
				ICmObject currentObject;
				if (m_cache.ServiceLocator.ObjectRepository.TryGetObject(hvo, out currentObject))
				{
					return currentObject;
				}
				CurrentIndex = -1;
				return null;
			}
		}

		public int CurrentObjectHvo
		{
			get
			{
				if (m_sortedObjects == null || m_sortedObjects.Count == 0 || m_currentIndex == -1)
				{
					CurrentIndex = -1;
					return 0;
				}
				return SortItemAt(m_currentIndex).RootObjectHvo;
			}
		}

		public bool Editable { get; set; } = true;

		public virtual RecordFilter Filter
		{
			get
			{
				return m_filter;
			}
			set
			{
				m_filter = value;
			}
		}

		public string FontName => m_fontName;

		public bool HasEmptyList => SortedObjects.Count == 0;

		public string Id { get; protected set; }

		public bool IsActiveInGui { get; protected set; }

		public virtual bool IsControllingTheRecordTreeBar { get; set; }

		public bool IsDefaultSort { get; set; }

		public void JumpToIndex(int index, bool suppressFocusChange = false)
		{
			// If we aren't changing the index, just bail out. (Fixes, LT-11401)
			if (CurrentIndex == index)
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
				CurrentIndex = index;
			}
			catch (IndexOutOfRangeException error)
			{
				throw new IndexOutOfRangeException("The record list tried to jump to a record which is not in the current active set of records.", error);
			}
			// This broadcast will often cause a save of the record, which clears the undo stack.
			BroadcastChange(suppressFocusChange);
		}

		public void JumpToRecord(int jumpToHvo, bool suppressFocusChange = false)
		{
			var index = IndexOf(jumpToHvo);
			if (index < 0)
			{
				return; // not found (maybe suppressed by filter?)
			}
			JumpToIndex(index, suppressFocusChange);
		}

		/// <summary>
		/// This may be set to suppress automatic reloading of the list. When set false again, if the list
		/// would have been reloaded at least once, it will be at that point. Use ListModificationInProgress instead
		/// if you know for sure that modifications to the list will occur and require reloading.
		///
		/// Enhance: Instead of depending upon PropChanged calls to ReloadList to set "m_requestedLoadWhileSuppressed",
		/// we could check IsValidObject on all the sortObject items in the list after ListLoadingSuppressed is set to false,
		/// to detect the need to reload (and avoid Record.PropChanged checks).
		/// </summary>
		public bool ListLoadingSuppressed
		{
			get { return m_suppressingLoadList; }
			set
			{
				if (m_suppressingLoadList == value)
				{
					return;
				}
				m_suppressingLoadList = value;
				if (!m_suppressingLoadList)
				{
					UninstallWindowActivated();
				}
				if (m_suppressingLoadList)
				{
					// We were previously NOT suppressing it; init the flag
					m_requestedLoadWhileSuppressed = false;
				}
				else
				{
					// We just stopped suppressing it; if necessary, load now.
					if (m_requestedLoadWhileSuppressed)
					{
						m_requestedLoadWhileSuppressed = false;
						ForceReloadList();
					}
				}
			}
		}

		public bool ListLoadingSuppressedNoSideEffects { get; set; }

		/// <summary>
		/// Used to suppress reloading the list until all modifications to the list have been performed.
		/// If you know that you are making modifications to a list and it needs to be reloaded afterwards use this
		/// property instead of ListLoadingSuppressed. ListLoadingSuppressed depends upon PropChange triggering ReloadList,
		/// and PropChange (for performance reasons) might not cover all the cases it needs to determine whether
		/// to reload the list.
		/// </summary>
		public bool ListModificationInProgress
		{
			get
			{
				return UpdatingList;
			}
			set
			{
				UpdatingList = value;
				ListLoadingSuppressed = value;
				if (ListLoadingSuppressed)
				{
					m_requestedLoadWhileSuppressed = true;
				}
			}
		}

		public int ListSize => SortedObjects.Count;

		public void MoveToIndex(Navigation navigateTo)
		{
			int newIndex;
			switch (navigateTo)
			{
				case Navigation.First:
					newIndex = FirstItemIndex;
					break;
				case Navigation.Next:
					// NB: This may be used in situations where there is no next record, as
					// when the current record has been deleted but it was the last record.
					newIndex = NextItemIndex;
					break;
				case Navigation.Previous:
					newIndex = PrevItemIndex;
					break;
				case Navigation.Last:
					newIndex = LastItemIndex;
					break;
				default:
					throw new IndexOutOfRangeException($"I don't know how to move to '{navigateTo}'.");
			}
			CurrentIndex = newIndex;
			BroadcastChange(false);
		}

		public virtual ITreeBarHandler MyTreeBarHandler => null;

		/// <summary>
		/// Handle adding and/or removing a filter.
		/// </summary>
		public virtual void OnChangeFilter(FilterChangeEventArgs args)
		{
			using (new WaitCursor(PropertyTable.GetValue<Form>(FwUtils.window)))
			{
				Logger.WriteEvent("Changing filter.");
				// if our record list is in the state of suspending loading the list, reset it now.
				if (SuspendLoadListUntilOnChangeFilter)
				{
					PropertyTable.SetProperty("SuspendLoadListUntilOnChangeFilter", string.Empty, settingsGroup: SettingsGroup.LocalSettings);
				}
				if (m_filter == null)
				{
					// Had no filter to begin with
					Debug.Assert(args.Removed == null);
					m_filter = args.Added is NullFilter ? null : args.Added;
				}
				else if (m_filter.SameFilter(args.Removed))
				{
					// Simplest case: we had just one filter, the one being removed.
					// Change filter to whatever (if anything) replaces it.
					m_filter = args.Added is NullFilter ? null : args.Added;
				}
				else if (m_filter is AndFilter)
				{
					var af = (AndFilter)m_filter;
					if (args.Removed != null)
					{
						af.Remove(args.Removed);
					}
					if (args.Added != null)
					{
						// When the user chooses "all records/no filter", the RecordList will remove
						// its previous filter and add a NullFilter. In that case, we don't really need to add
						// that filter. Instead, we can just add nothing.
						if (!(args.Added is NullFilter))
						{
							af.Add(args.Added);
						}
					}
					// Remove AndFilter if we get down to one.
					// This is not just an optimization, it allows the last filter to be removed
					// leaving empty, so the status bar can show that there is then no filter.
					if (af.Filters.Count == 1)
					{
						m_filter = af.Filters[0] as RecordFilter;
					}
				}
				else
				{
					// m_filter is not an AndFilter, so can't contain the one we're removing, nor IS it the one
					// we're removing...so we have no way to remove, and it's an error if we're trying to.
					Debug.Assert(args.Removed == null || args.Removed is NullFilter);
					if (args.Added != null && !(args.Added is NullFilter)) // presumably true or nothing changed, but for paranoia..
					{
						// We already checked for m_filter being null, so we now have two filters,
						// and need to make an AndFilter.
						m_filter = CreateNewAndFilter(m_filter, args.Added);
					}
				}
				// Now we have a new filter, we have to recompute what to show.
				ReloadList();
				// Remember the active filter for this list.
				var persistFilter = DynamicLoader.PersistObject(Filter, "filter");
				PropertyTable.SetProperty(FilterPropertyTableId, persistFilter, true, true, SettingsGroup.LocalSettings);
				// adjust menu bar items according to current state of Filter, where needed.
				Publisher.Publish("AdjustFilterSelection", Filter);
				UpdateFilterStatusBarPanel();
				if (Filter != null)
				{
					Logger.WriteEvent("Filter changed: " + Filter);
				}
				else
				{
					Logger.WriteEvent("Filter changed: (no filter)");
				}
				// notify clients of this change.
				FilterChangedByList?.Invoke(this, args);
			}
		}

		public void OnChangeFilterClearAll()
		{
			_activeMenuBarFilter = null; // there won't be a menu bar filter after this.
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
				if (count > 0)
				{
					var af2 = CreateNewAndFilter(childrenToKeep.ToArray());
					OnChangeFilter(new FilterChangeEventArgs(af2, Filter));
					return;
				}
				// Otherwise none of the children need to be kept, get rid of the whole filter.
			}
			OnChangeFilter(new FilterChangeEventArgs(null, Filter));
		}

		public void OnChangeListItemsClass(int listItemsClass, int newTargetFlid, bool force)
		{
			ReloadList(listItemsClass, newTargetFlid, force);
		}

		public bool DeleteRecord(string uowBaseText, StatusBarProgressPanel panel)
		{
			// Don't handle this message if you're not the primary record list.  This allows, for
			// example, XmlBrowseRDEView.cs to handle the message instead.
#if RANDYTODO
			// Note from RandyR: One of these days we should probably subclass the record list more.
			// The "reversalEntries" record list wants to handle the message, even though it isn't the primary record list.
			// The m_shouldHandleDeletion member was also added, so the "reversalEntries" record list's primary record list
			// would not handle the message, and delete an entire reversal index.
#endif
			if (ShouldNotHandleDeletionMessage)
			{
				return false;
			}
			// It may be null:
			// 1. if the objects are being deleted using the keys,
			// 2. the last one has been deleted, and
			// 3. the user keeps pressing the del key.
			// It looks like the command is not being disabled at all or fast enough.
			if (CurrentObjectHvo == 0)
			{
				return true;
			}
			// Don't allow an object to be deleted if it shouldn't be deleted.
			if (!CanDelete())
			{
				ReportCannotDelete();
				return true;
			}
			var thingToDelete = GetObjectToDelete(CurrentObject);
			using (var dlg = new ConfirmDeleteObjectDlg(PropertyTable.GetValue<IHelpTopicProvider>(LanguageExplorerConstants.HelpTopicProvider)))
			{
				using (var uiObj = CmObjectUi.MakeLcmModelUiObject(thingToDelete))
				{
					uiObj.InitializeFlexComponent(new FlexComponentParameters(PropertyTable, Publisher, Subscriber));
					string cannotDeleteMsg;
					if (uiObj.CanDelete(out cannotDeleteMsg))
					{
						dlg.SetDlgInfo(uiObj, m_cache, PropertyTable);
					}
					else
					{
						dlg.SetDlgInfo(uiObj, m_cache, PropertyTable, TsStringUtils.MakeString(cannotDeleteMsg, m_cache.DefaultUserWs));
					}
				}
				var window = PropertyTable.GetValue<Form>(FwUtils.window);
				if (DialogResult.Yes == dlg.ShowDialog(window))
				{
					using (new WaitCursor(window))
					using (var state = ProgressState.CreatePredictiveProgressState(panel, "Delete record"))
					{
						state.SetMilestone(LanguageExplorerResources.DeletingTheObject);
						state.Breath();
						// We will certainly switch records, but we're going to suppress the usual Save after we
						// switch, so the user can at least Undo one level, the actual deletion. But Undoing
						// that may not get us back to the current record, so we'd better not allow anything
						// that's already on the stack to be undone.
						SaveOnChangeRecord();
						SuppressSaveOnChangeRecord = true;
						try
						{
							RemoveItemsFor(thingToDelete.Hvo);
							AreaServices.UndoExtension(uowBaseText, m_cache.ActionHandlerAccessor, () => DeleteCurrentObject(thingToDelete));
							UpdateRecordTreeBar();
						}
						finally
						{
							SuppressSaveOnChangeRecord = false;
						}
					}
					PropertyTable.GetValue<IFwMainWnd>(FwUtils.window).RefreshAllViews();
				}
			}
			return true; //we handled this, no need to ask anyone else.
		}

		public bool OnExport(object argument)
		{
			// It's somewhat unfortunate that this bit of code knows what classes can have custom fields.
			// However, we put in code to prevent punctuation in custom field names at the same time as this check (which is therefore
			// for the benefit of older projects), so it should not be necessary to check any additional classes we allow to have them.
			if (AreCustomFieldsAProblem(new[] { LexEntryTags.kClassId, LexSenseTags.kClassId, LexExampleSentenceTags.kClassId, MoFormTags.kClassId }))
			{
				return true;
			}
			using (var dlg = new ExportDialog(_statusBar))
			{
				dlg.InitializeFlexComponent(new FlexComponentParameters(PropertyTable, Publisher, Subscriber));
				dlg.ShowDialog(PropertyTable.GetValue<Form>(FwUtils.window));
			}
			ActivateUI();
			return true;    // handled
		}

		/// <summary>
		/// true if the list is non empty on we are on the first record
		/// </summary>
		public bool OnFirst => (SortedObjects.Count > 0) && m_currentIndex == 0;

		/// <summary />
		public bool OnInsertItemInVector(object argument)
		{
			if (!Editable)
			{
				return false;
			}
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
			{
				return false;
			}
#endif
			var result = false;
#if RANDYTODO
			m_suppressSaveOnChangeRecord = true;
			try
			{
				UndoableUnitOfWorkHelper.Do(string.Format(LanguageExplorerResources.ksUndoInsert0, command.UndoRedoTextInsert),
					string.Format(LanguageExplorerResources.ksRedoInsert0, command.UndoRedoTextInsert), Cache.ActionHandlerAccessor, () =>
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

		public void OnItemDataModified(object argument)
		{
			var da = VirtualListPublisher;
			while (da != null)
			{
				if (da.GetType().GetMethod("OnItemDataModified") != null)
				{
					ReflectionHelper.CallMethod(da, "OnItemDataModified", argument);
				}
				var decorator = da as DomainDataByFlidDecoratorBase;
				if (decorator == null)
				{
					break;
				}
				da = decorator.BaseSda;
			}
		}

		public bool OnJumpToRecord(object argument)
		{
			try
			{
				var hvoTarget = (int)argument;
				var index = IndexOfObjOrChildOrParent(hvoTarget);
				if (index == -1)
				{
					// See if this is a subclass that knows how to add items.
					if (AddItemToList(hvoTarget))
					{
						index = IndexOfObjOrChildOrParent(hvoTarget);
					}
				}
				if (Filter != null && index == -1)
				{
					// We can get here with an irrelevant target hvo, for example by inserting a new
					// affix allomorph in an entry (see LT-4025).  So make sure we have a suitable
					// target before complaining to the user about a filter being on.
					var mdc = VirtualListPublisher.GetManagedMetaDataCache();
					var clidList = mdc.FieldExists(m_flid) ? mdc.GetDstClsId(m_flid) : -1;
					var clidObj = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoTarget).ClassID;
					// If (int) clidList is -1, that means it was for a decorator property and the IsSameOrSubclassOf
					// test won't be valid.
					// Enhance JohnT/CurtisH: It would be better to if decorator properties were recorded in the MDC, or
					// if we could access the decorator MDC.
					var fSuitableTarget = clidList == -1 || DomainObjectServices.IsSameOrSubclassOf(mdc, clidObj, clidList);
					if (fSuitableTarget)
					{
						var dr = MessageBox.Show(LanguageExplorerResources.LinkTargetNotAvailableDueToFilter, LanguageExplorerResources.TargetNotFound, MessageBoxButtons.YesNo);
						if (dr == DialogResult.Yes)
						{
							// We had changed from OnChangeFilter to SendMessage("RemoveFilters") to solve a filterbar
							// update issue reported in (LT-2448). However, that message only works in the context of a
							// BrowseViewer, not a document view (e.g. Dictionary) (see LT-7298). So, I've
							// tested OnChangeFilterClearAll, and it seems to solve both problems now.
							OnChangeFilterClearAll();
							_activeMenuBarFilter = null;
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
					ReloadList();
					index = IndexOfObjOrChildOrParent(hvoTarget);
					if (index == -1)
					{
						// It may be the wrong record list, so just bail out.
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
		public bool OnLast => (SortedObjects.Count > 0) && (m_currentIndex == (SortedObjects.Count - 1));

		public virtual void OnPropertyChanged(string name)
		{
			// This happens when the user chooses a MenuItem or sidebar item that selects a filter
			if (name != CurrentFilterPropertyTableId)
			{
				return;
			}
			OnChangeFilterToCheckedListPropertyChoice();
		}

		public virtual bool OnRefresh(object argument)
		{
			using (new WaitCursor(PropertyTable.GetValue<Form>(FwUtils.window)))
			{
				_recordChangeHandler?.Fixup(false);     // no need to recursively refresh!
				ReloadList();
				return false;   //that other colleagues do a refresh, too.
			}
		}

		public void OnSorterChanged(RecordSorter sorter, string sortName, bool isDefaultSort)
		{
			_isDefaultSort = isDefaultSort;
			SortName = sortName;
			PropertyTable.SetProperty(SortNamePropertyTableId, SortName, true, true, SettingsGroup.LocalSettings);
			ChangeSorter(sorter);
			// Remember how we're sorted.
			var persistSorter = DynamicLoader.PersistObject(Sorter, "sorter");
			PropertyTable.SetProperty(SorterPropertyTableId, persistSorter, true, true, SettingsGroup.LocalSettings);
			UpdateSortStatusBarPanel();
		}

		/// <summary>
		/// the object which owns the elements which make up this list
		/// </summary>
		public ICmObject OwningObject
		{
			get
			{
				return m_owningObject;
			}
			set
			{
				if (ReferenceEquals(m_owningObject, value))
				{
					return; // no need to reload.
				}
				m_owningObject = value;
				m_oldLength = 0;
				ReloadList();
			}
		}

		public int OwningFlid { get; }

		public IRecordList ParentList { get; }

		public string PersistedIndexProperty { get; }

		public void PersistListOn(string pathname)
		{
			if (!IsPrimaryRecordList)
			{
				return;
			}
			// Ensure that all the items in the sorted list are valid ICmObject references before
			// actually persisting anything.
			if (m_sortedObjects == null || m_sortedObjects.Count == 0)
			{
				return;
			}
			var repo = m_cache.ServiceLocator.ObjectRepository;
			foreach (var obj in m_sortedObjects)
			{
				var item = obj as ManyOnePathSortItem;
				if (item == null)
				{
					return;
				}
				// The object might have been deleted.  See LT-11169.
				if (item.KeyObject <= 0 || item.RootObjectHvo <= 0 || !repo.IsValidObjectId(item.KeyObject) || !repo.IsValidObjectId(item.RootObjectHvo))
				{
					return;
				}
			}
			try
			{
				using (var stream = new StreamWriter(pathname))
				{
					ManyOnePathSortItem.WriteItems(m_sortedObjects, stream, repo);
					stream.Close();
				}
			}
			// LT-11395 and others: somehow the current list contains a deleted object.
			// Writing out this file is just an optimization, so if we can't do it, just skip it.
			catch (KeyNotFoundException)
			{
				TryToDelete(pathname);
			}
			catch (IOException)
			{
				TryToDelete(pathname);
			}
		}

		public string PropertyName { get; protected set; } = string.Empty;

		public void ReloadFilterProvider()
		{
			_filterProvider?.ReLoad();
		}

		public virtual void ReloadIfNeeded()
		{
			if (OwningObject != null && VirtualListPublisher != null)
			{
				// A full refresh wipes out all caches as of 26 October 2005,
				// so we have to reload it. This fixes the sextuplets:
				// LT-5393, LT-6102, LT-6154, LT-6084, LT-6059, LT-6062.
				ReloadList();
			}
		}

		/// <summary>
		/// Indicates whether we tried to reload the list while we were suppressing the reload.
		/// </summary>
		public virtual bool RequestedLoadWhileSuppressed
		{
			get { return m_requestedLoadWhileSuppressed; }
			set { m_requestedLoadWhileSuppressed = value; }
		}

		public void ResetFilterToDefault()
		{
			OnChangeFilter(new FilterChangeEventArgs(_defaultFilter, Filter));
		}

		/// <summary>
		/// Returns true if it successfully set m_sortedObjects to a restored list.
		/// </summary>
		public virtual bool RestoreListFrom(string pathname)
		{
			// If something has created instances of the class we display, the persisted list may be
			// missing things. For example, if the program starts up in interlinear text view, and the user
			// creates entries as a side effect of glossing texts, we shouldn't use the saved list of
			// lex entries when we switch to the lexicon view.
			if (m_cache.ServiceLocator.ObjectRepository.InstancesCreatedThisSession(ListItemsClass))
			{
				return false;
			}
			using (var stream = new StreamReader(pathname))
			{
				ManyOnePathSortItem.ReadItems(stream, m_cache.ServiceLocator.ObjectRepository);
				stream.Close();
			}
			// This particular cache cannot reliably be used again, since items may be created or deleted
			// while the program is running. In case a crash occurs, we don't want to reload an obsolete
			// list the next time we start up.
			FileUtils.Delete(pathname);
			return false; // could not restore, bad file or deleted objects or...
		}

		public void SaveOnChangeRecord()
		{
#if RANDYTODO
			// TODO: Work up non static test that can use IFwMainWnd
#endif
			if (_suppressSaveOnChangeRecord || m_cache == null)
			{
				return;
			}
			using (new WaitCursor(Form.ActiveForm))
			{
				// Commit() was too drastic here, resulting in Undo/Redo stack being cleared.
				// (See LT-13397)
				var actionHandler = m_cache.ActionHandlerAccessor;
				if (actionHandler.CurrentDepth > 0)
				{
					// EndOuterUndoTask() is not implemented, so we better call EndUndoTask().
					// (This fixes LT-16673)
					actionHandler.EndUndoTask();
				}
			}
		}

		public void SelectedRecordChanged(bool suppressFocusChange, bool fSkipRecordNavigation = false)
		{
			if (CurrentObjectHvo != 0 && !CurrentObjectIsValid)
			{
				ReloadList(); // clean everything up
			}
			if (IgnoreStatusPanel)
			{
				return;
			}
			UpdateStatusBarForRecordBar();

#if RANDYTODO
			// TODO: Work up non-static test that can use IFwMainWnd
#endif
			// This is used by DependentRecordLists
			var rni = new RecordNavigationInfo(this, _suppressSaveOnChangeRecord, SkipShowRecord, suppressFocusChange);
			// As of 21JUL17 nobody cares about that 'propName' changing, so skip the broadcast.
			PropertyTable.SetProperty(PersistedIndexProperty, CurrentIndex, true, settingsGroup: SettingsGroup.LocalSettings);
			UpdateSelectionForRecordBar();
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
		public bool ShouldNotModifyList => m_reloadingList || m_deletingObject;

		public bool ShouldNotHandleDeletionMessage => Id != "reversalEntries" && (!Editable || !IsPrimaryRecordList || !_shouldHandleDeletion);

		public bool SkipShowRecord { get; set; }

		/// <summary>
		/// This is now a list of ManyOnePathSortItems! (JohnT, 3 June 05)
		/// </summary>
		public ArrayList SortedObjects
		{
			get
			{
				return m_sortedObjects ?? (m_sortedObjects = new ArrayList());
			}
			set
			{
				m_sortedObjects = value;
			}
		}

		/// <summary>
		/// Gets or sets a sorter.
		/// </summary>
		public RecordSorter Sorter
		{
			get
			{
				return m_sorter;
			}
			set
			{
				m_sorter = value;
				m_sorter.Cache = m_cache;
			}
		}

		/// <summary>
		/// The display name of what is currently being sorted. This variable is persisted as a user
		/// setting. When the sort name is null it indicates that the items in the record list are not
		/// being sorted or that the current sorting should not be displayed (i.e. the default column
		/// is being sorted).
		/// </summary>
		public string SortName { get; set; }

		public bool SuppressSaveOnChangeRecord { get; set; }

		public bool SuspendLoadingRecordUntilOnJumpToRecord { get; set; }

		public int TypeSize => m_typeSize;

		public bool UpdateFiltersAndSortersIfNeeded()
		{
			var fRestoredSorter = TryRestoreSorter();
			var fRestoredFilter = TryRestoreFilter();
			UpdateFilterStatusBarPanel();
			UpdateSortStatusBarPanel();
			return fRestoredSorter || fRestoredFilter;
		}

		/// <summary>
		/// Indicates whether RecordList is being modified
		/// </summary>
		public bool UpdatingList { get; set; }

		public void UpdateOwningObjectIfNeeded()
		{
			UpdateOwningObject(true);
		}

		public virtual void UpdateRecordTreeBar()
		{
			// Subclasses that actually know about a record bar (e.g.; TreeBarHandlerAwarePossibilityRecordList) should override this method.
		}

		public virtual void UpdateRecordTreeBarIfNeeded()
		{
			// Subclasses that actually know about a record bar (e.g.; TreeBarHandlerAwarePossibilityRecordList) should override this method.
		}

		public void UpdateStatusBarRecordNumber(string noRecordsText)
		{
			string message;
			var len = SortedObjects.Count;
			if (len > 0)
			{
				message = (1 + CurrentIndex) + @"/" + len;
			}
			else
			{
				message = noRecordsText;
			}
			StatusBarPanelServices.SetStatusPanelRecordNumber(_statusBar, message);
			ResetStatusBarMessageForCurrentObject();
		}

		public void ViewChangedSelectedRecord(FwObjectSelectionEventArgs e)
		{
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
			if (e.Index >= 0 && SortedObjects.Count > 0)
			{
				var ourHvo = SortItemAt(e.Index).RootObjectHvo;
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

		/// <summary>
		/// the made-up flid that we address the sequence of hvos in the cache.
		/// </summary>
		public int VirtualFlid => RecordListFlid;

		/// <summary>
		/// This property provides a decorator for the ISilDataAccess in the cache, which provides
		/// access to the filtered, sorted virtual list that the record list maintains for views.
		/// Views can also be registered for notification of changes to that property.
		/// If the record list's spec includes a decorator class, we will create an instance of that,
		/// which wraps the main SDA and is wrapped by our ObjectListPublisher.
		/// Otherwise, our ObjectListPublisher wraps the main SDA directly.
		/// </summary>
		public ISilDataAccessManaged VirtualListPublisher
		{
			get;
			private set;
		}

		#endregion Implementation of IRecordList

		#endregion All interface implementations

		#region Non-interface code

		#region Internal stuff

		/// <summary>
		/// verifies that the two classes match, if not, throws message.
		/// </summary>
		internal static void CheckExpectedListItemsClassInSync(int beExpectedListItemsClass, int recordListExpectedListItemsClass)
		{
			if (beExpectedListItemsClass != 0 && recordListExpectedListItemsClass != 0 && beExpectedListItemsClass != recordListExpectedListItemsClass)
			{
				throw new ApplicationException($"for some reason BulkEditBar.ExpectedListItemsClassId({beExpectedListItemsClass}) does not match SortItemProvider.ListItemsClass({recordListExpectedListItemsClass}).");
			}
		}

		internal static string RecordListSelectedObjectPropertyId(string recordListId)
		{
			return $"{recordListId}-selected";
		}

		internal static string GetCorrespondingPropertyName(string vectorName)
		{
			return $"RecordList-{vectorName}";
		}

		#endregion Internal stuff

		#region Private stuff

		/// <summary>
		/// This version of ReloadList assumes that there is a correct current list except that
		/// property m_flid of object m_owningObject has been modified by inserting cvIns objects
		/// and/or deleting cvDel objects at ivMin. May call the regular ReloadList, or
		/// optimize for special cases.
		/// </summary>
		private void ReloadList(int ivMin, int cvIns, int cvDel)
		{
			if (RequestedLoadWhileSuppressed)
			{
				// if a previous reload was requested, but suppressed, try to reload the entire list now
				ReloadList();
			}
			// If m_currentIndex is negative the list is empty so we may as well load it fully.
			// This saves worrying about various special cases in the code below.
			else if (cvIns == 1 && (cvDel == 1 || cvDel == 0) && m_owningObject != null && m_hvoCurrent != 0 && m_currentIndex >= 0)
			{
				var cList = VirtualListPublisher.get_VecSize(m_owningObject.Hvo, m_flid);
				if (cList == 1)
				{
					// we only have one item in our list, so let's just do a full reload.
					// We don't want to insert completely new items in an obsolete list (Cf. LT-6741,6845).
					ReloadList();
					return;
				}
				if (cvDel > 0)
				{
					// Before we try to insert a new one, need to delete any items for deleted stuff,
					// otherwise it may crash as it tries to compare the new item with an invalid one.
					ClearOutInvalidItems();
				}
				if (ivMin < cList)
				{
					ReplaceListItem(VirtualListPublisher.get_VecItem(m_owningObject.Hvo, m_flid, ivMin));
				}
			}
			else if (cvIns == 0 && cvDel == 0 && m_owningObject != null && m_hvoCurrent != 0)
			{
				UpdateListItemName(m_hvoCurrent);
			}
			else
			{
				ReloadList();
			}
		}

		private bool IgnoreStatusPanel { get; set; }

		private void UpdateSortStatusBarPanel()
		{
			if (!IsControllingTheRecordTreeBar || IgnoreStatusPanel)
			{
				return; // none of our business!
			}
			var newSortMessage = Sorter == null || SortName == null || (_isDefaultSort && _defaultSorter != null) ? string.Empty : string.Format(LanguageExplorerResources.SortedBy0, SortName);
			StatusBarPanelServices.SetStatusBarPanelSort(_statusBar, newSortMessage);
		}

		private string FilterPropertyTableId => PropertyTableId("filter");

		/// <summary>
		/// This keeps track of the object if any that we have noted in the CmObjectRepository as focused.
		/// My intention is to reserve it for this purpose ONLY.
		/// </summary>
		private ICmObject RecordedFocusedObject;

		private void TryReloadForInvalidPathObjectsOnCurrentObject(int tag, int cvDel)
		{
			// see if the property is the VirtualFlid of the owning record list. If so,
			// the owning record list has reloaded, so we should also reload.
			IRecordList listProvidingRootObject;
			if (TryListProvidingRootObject(out listProvidingRootObject) && listProvidingRootObject.VirtualFlid == tag && cvDel > 0)
			{
				// we're deleting or replacing items, so assume need to reload.
				// we want to wait until after everything is finished reloading, however.
				m_requestedLoadWhileSuppressed = true;
			}

			// Try to catch things that don't obviously affect us, but will cause problems.
			if (cvDel <= 0 || CurrentIndex < 0 || !IsPropOwning(tag))
			{
				return;
			}
			// We've deleted an object. Unfortunately we can't find out what object was deleted.
			// Therefore it will be too slow to check every item in our list. Checking out the current one
			// thoroughly prevents many problems (e.g., LT-4880)
			var item = SortedObjects[CurrentIndex];
			if (!(item is IManyOnePathSortItem))
			{
				return;
			}
			var asManyOnePathSortItem = (IManyOnePathSortItem)item;
			if (!m_cache.ServiceLocator.IsValidObjectId(asManyOnePathSortItem.KeyObject))
			{
				ReloadList();
				return;
			}
			for (var i = 0; i < asManyOnePathSortItem.PathLength; i++)
			{
				if (m_cache.ServiceLocator.IsValidObjectId(asManyOnePathSortItem.PathObject(i)))
				{
					continue;
				}
				ReloadList();
				return;
			}
		}

		private bool ReloadLexEntries => m_fReloadLexEntries;

		private bool IsPropOwning(int tag)
		{
			var mdc = VirtualListPublisher.MetaDataCache;
			var fieldType = (CellarPropertyType)mdc.GetFieldType(tag);
			switch (fieldType)
			{
				case CellarPropertyType.OwningAtomic:
				case CellarPropertyType.OwningCollection:
				case CellarPropertyType.OwningSequence:
					return true;
				case CellarPropertyType.ReferenceSequence:
					// In addition to normal owning properties, we want to treat the master virtual for all
					// lex entries as owning. I haven't actually made it owning, because we don't currently
					// have any owning virtual properties, and I'm nervous about the consequences of a
					// property which claims to be owning where the target objects don't record the source
					// one as their owner. But RecordList does need to do the checks it does for deleted
					// objects when when a LexEntry goes away.
					var entriesTag = mdc.GetFieldId2(LexDbTags.kClassId, "Entries", false);
					if (tag == entriesTag)
					{
						return true;
					}

					break;
			}
			return false;
		}

		private int RootHvoAt(int index)
		{
			return SortItemAt(index).RootObjectHvo;
		}

		private int[] IndicesOfSortItems(List<int> hvoTargets, bool fStopAtFirstFound = false)
		{
			var indices = new List<int>();
			if (hvoTargets == null || hvoTargets.Count <= 0)
			{
				return indices.ToArray();
			}
			for (var i = 0; i < SortedObjects.Count; i++)
			{
				var hvoItem = SortItemAt(i).RootObjectHvo;
				if (!hvoTargets.Contains(hvoItem))
				{
					continue;
				}
				indices.Add(i);
				if (fStopAtFirstFound)
				{
					break;
				}
			}
			return indices.ToArray();
		}

		private void ClearOutInvalidItems()
		{
			for (var i = 0; i < SortedObjects.Count;) // not foreach: we may modify the list
			{
				if (IsInvalidItem((ManyOnePathSortItem)SortedObjects[i]))
				{
					SortedObjects.RemoveAt(i);
				}
				else
				{
					i++;
				}
			}
		}

		private bool IsInvalidItem(ManyOnePathSortItem item)
		{
			if (!m_cache.ServiceLocator.IsValidObjectId(item.KeyObject))
			{
				return true;
			}
			for (var i = 0; i < item.PathLength; i++)
			{
				if (!m_cache.ServiceLocator.IsValidObjectId(item.PathObject(i)))
				{
					return true;
				}
			}
			return false;
		}

		// stub to make empty action.
		private static void DoNothing(int ignoreMe)
		{
		}

		private ArrayList HandleInvalidFilterSortField()
		{
			m_filter = null;
			m_sorter = null;
			MessageBox.Show(Form.ActiveForm, LanguageExplorerResources.ksInvalidFieldInFilterOrSorter, LanguageExplorerResources.ksErrorCaption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
			var newSortedObjects = GetFilteredSortedList();
			return newSortedObjects;
		}

		private ArrayList GetFilteredSortedList()
		{
			var newSortedObjects = new ArrayList();
			if (m_filter != null)
			{
				m_filter.Preload(OwningObject);
			}
			else
			{
				// Preload the sorter (if any) only if we do NOT have a filter.
				// If we also have a filter, it's pretty well certain currently that it already did
				// the preloading. If we ever have a filter and sorter that want to preload different
				// data, we'll need to refactor so we can determine this, because we don't want to do it twice!
				m_sorter?.Preload(OwningObject);
			}
			var panel = PropertyTable.GetValue<StatusBarProgressPanel>("ProgressBar");
			using (var progress = (panel == null) ? new NullProgressState() : new ProgressState(panel))
			{
				progress.SetMilestone(LanguageExplorerResources.ksSorting);
				// Allocate an arbitrary 20% for making the items.
				var objectSet = GetObjectSet();
				var count = objectSet.Count();
				var done = 0;
				foreach (var obj in objectSet)
				{
					done++;
					var newPercentDone = done * 20 / count;
					if (progress.PercentDone != newPercentDone)
					{
						progress.PercentDone = newPercentDone;
						progress.Breath();
					}
					MakeItemsFor(newSortedObjects, obj);
				}
				SortList(newSortedObjects, progress);
			}
			return newSortedObjects;
		}

		private void RequestReloadOnActivation(Form window)
		{
			window.Activated -= window_Activated;
			window.Activated += window_Activated;
			m_windowPendingOnActivated = window;
		}

		/// <summary>
		/// it's possible that we'll want to reload once we become the main active window (cf. LT-9251)
		/// </summary>
		private void window_Activated(object sender, EventArgs e)
		{
			UninstallWindowActivated();
			if (NeedToReloadList())
			{
				ReloadList();
			}
		}

		private void UninstallWindowActivated()
		{
			if (m_windowPendingOnActivated != null)
			{
				m_windowPendingOnActivated.Activated -= window_Activated;
			}
			m_windowPendingOnActivated = null;
		}

		private void TryToDelete(string pathname)
		{
			if (!File.Exists(pathname))
			{
				return;
			}
			try
			{
				File.Delete(pathname);
			}
			catch (IOException)
			{
			}
		}

		/// <summary>
		/// LT-12780:  On reloading FLEX there were situations where it reloaded on the first element of a list instead of
		/// the last item the user was working on before closing Flex. The CurrentIndex was being set to zero. This method
		/// will access the persisted index but also make sure it does not use an index which is out of range.
		/// </summary>
		private int GetPersistedCurrentIndex(int numberOfObjectsInList)
		{
			var persistedCurrentIndex = PropertyTable.GetValue(PersistedIndexProperty, 0, SettingsGroup.LocalSettings);
			if (persistedCurrentIndex >= numberOfObjectsInList)
			{
				persistedCurrentIndex = numberOfObjectsInList - 1;
			}
			return persistedCurrentIndex;
		}

		private void RegisterMessageHandlers()
		{
			var window = PropertyTable.GetValue<IFwMainWnd>(FwUtils.window);
			var recordBarControl = window?.RecordBarControl; // Tests may not have a window.
			if (recordBarControl != null)
			{
				UnregisterMessageHandlers(); // Unwire them, in case (more likely 'since') this is re-entrant.
				Subscriber.Subscribe("SelectedTreeBarNode", SelectedTreeBarNode_Message_Handler);
				Subscriber.Subscribe("SelectedListBarNode", SelectedListBarNode_Message_Handler);
			}
		}

		private void UnregisterMessageHandlers()
		{
			var window = PropertyTable.GetValue<IFwMainWnd>(FwUtils.window);
			// Some tests don't have a window or RecordBarControl.
			var recordBarControl = window?.RecordBarControl;
			if (recordBarControl != null)
			{
				Subscriber.Unsubscribe("SelectedTreeBarNode", SelectedTreeBarNode_Message_Handler);
				Subscriber.Unsubscribe("SelectedListBarNode", SelectedListBarNode_Message_Handler);
			}
		}

		private void SelectedListBarNode_Message_Handler(object obj)
		{
			if (!IsControllingTheRecordTreeBar)
			{
				return;
			}
			var item = PropertyTable.GetValue<ListViewItem>("SelectedListBarNode");
			if (item == null)
			{
				return;
			}
			if (!(item.Tag is int))
			{
				throw new ArgumentException(SelectedListBarNodeErrorMessage);
			}
			var hvo = (int)item.Tag;
			if (CurrentObjectHvo == 0 || hvo != CurrentObjectHvo)
			{
				JumpToRecord(hvo);
			}
		}

		private void SelectedTreeBarNode_Message_Handler(object obj)
		{
			if (!IsControllingTheRecordTreeBar)
			{
				return;
			}
			var node = PropertyTable.GetValue<TreeNode>("SelectedTreeBarNode");
			if (node == null)
			{
				return;
			}
			if (!(node.Tag is int))
			{
				throw new ArgumentException(SelectedListBarNodeErrorMessage);
			}
			var hvo = (int)node.Tag;
			if (CurrentObjectHvo == 0 || hvo != CurrentObjectHvo)
			{
				JumpToRecord(hvo);
			}
		}

		private void BroadcastChange(bool suppressFocusChange)
		{
			ClearInvalidSubitem();
			if (CurrentObjectHvo != 0 && !CurrentObjectIsValid)
			{
				MessageBox.Show(LanguageExplorerResources.SelectedObjectHasBeenDeleted, LanguageExplorerResources.DeletedObjectDetected, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				_reloadingDueToMissingObject = true;
				try
				{
					var idx = CurrentIndex;
					var cobj = ListSize;
					if (cobj == 0)
					{
						CurrentIndex = -1;
					}
					else
					{
						CurrentIndex = FindClosestValidIndex(idx, cobj);
					}
					Publisher.Publish("StopParser", null);  // stop parser if it's running.
				}
				finally
				{
					_reloadingDueToMissingObject = false;
				}
				return; // typically leads to another call to this.
			}
			SelectedRecordChanged(suppressFocusChange);
		}

		private string SortNamePropertyTableId => PropertyTableId("sortName");

		private string SorterPropertyTableId => PropertyTableId("sorter");

		private void ResetStatusBarMessageForCurrentObject()
		{
			var msg = string.Empty;
			if (CurrentObjectHvo != 0)
			{
				// deleted objects don't have a cache (and other properties) so it was crashing.  LT-3160, LT-3121,...
				msg = GetStatusBarMsgForCurrentObject();
			}
			StatusBarPanelServices.SetStatusPanelMessage(_statusBar, msg);
		}

		private int FindClosestValidIndex(int idx, int cobj)
		{
			for (var i = idx + 1; i < cobj; ++i)
			{
				var item = (IManyOnePathSortItem)SortedObjects[i];
				if (!m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(item.KeyObject).IsValidObject)
				{
					continue;
				}
				var fOk = true;
				for (var j = 0; fOk && j < item.PathLength; j++)
				{
					fOk = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(item.PathObject(j)).IsValidObject;
				}
				if (fOk)
				{
					return i;
				}
			}
			for (var i = idx - 1; i >= 0; --i)
			{
				var item = (IManyOnePathSortItem)SortedObjects[i];
				if (!m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(item.KeyObject).IsValidObject)
				{
					continue;
				}
				var fOk = true;
				for (var j = 0; fOk && j < item.PathLength; j++)
				{
					fOk = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(item.PathObject(j)).IsValidObject;
				}
				if (fOk)
				{
					return i;
				}
			}
			return -1;
		}

		/// <summary>
		/// Stop notifications of prop changes
		/// </summary>
		private void AddNotification()
		{
			if (IsDisposed || m_cache == null || m_cache.IsDisposed || m_cache.DomainDataByFlid == null)
			{
				return;
			}
			m_cache.DomainDataByFlid.AddNotification(this);
		}

		/// <summary>
		/// Stop notifications of prop changes
		/// </summary>
		private void RemoveNotification()
		{
			// We need the list to get the cache.
			if (IsDisposed || m_cache == null || m_cache.IsDisposed || m_cache.DomainDataByFlid == null)
			{
				return;
			}
			m_cache.DomainDataByFlid.RemoveNotification(this);
		}

		/// <summary>
		/// Find the index of hvoTarget in m_list; or, if it does not occur, the index of a child of hvoTarget.
		/// </summary>
		private int IndexOfObjOrChildOrParent(int hvoTarget)
		{
			var index = IndexOf(hvoTarget);
			if (index == -1)
			{
				// In case we can't find the argument in the list, see if it is an owner of anything
				// in the list. This is useful, for example, when asked to find a LexEntry in a list of senses.
				index = IndexOfChildOf(hvoTarget);
			}
			if (index == -1)
			{
				// Still no luck. See if the argument's owner is in the list (e.g., may be a subrecord
				// in DN, and only parent currently showing).
				index = IndexOfParentOf(hvoTarget);
			}
			return index;
		}

		/// <summary>
		/// Change the list filter to the currently selected (checked) FilterList item.
		/// This selection is stored in the property table based on the name of the filter
		/// associated with the current record list.
		/// </summary>
		private void OnChangeFilterToCheckedListPropertyChoice()
		{
			var filterName = PropertyTable.GetValue(CurrentFilterPropertyTableId, string.Empty, SettingsGroup.LocalSettings);
			RecordFilter addf = null;
			RecordFilter remf = null;
			var nof = new NoFilters();
			// Check for special cases.
			if (filterName == FiltersStrings.ksUncheckAll)
			{
				// we're simply unselecting all items in the list.
				// no need for further changes.
				return;
			}
			if (filterName == nof.Name)
			{
				OnChangeFilterClearAll(); // get rid of all the ones we're allowed to.
				return;
			}
			if (_filterProvider != null)
			{
				addf = _filterProvider.GetFilter(filterName);
				if (addf == null || addf is NullFilter)
				{
					// If we have no filter defined for this name, it is effectively another way to turn
					// filters off. Turn off all we can.
					OnChangeFilterClearAll(); // get rid of all the ones we're allowed to.
					return;
				}
				// If we have a menu-type filter active, remove it. Otherwise don't remove anything.
				remf = _activeMenuBarFilter;
				_activeMenuBarFilter = addf;
			}
			OnChangeFilter(new FilterChangeEventArgs(addf, remf));
		}

		private void SetupDataContext(bool floadList)
		{
			InitLoad(floadList);

			UpdateOwningObject();
		}

		private void AboutToReload()
		{
			// This used to be a BroadcastMessage, but now broadcast is deferred.
			// To keep the same logic it's now using the SendMessageToAllNow.  This
			// is different from SendMessage as it is sent to all even if handled.
			// To avoid hitting the " For now, we'll not try to be concerned about restoring scroll position
			// in a context where we're reloading after suppressing a reload.
			if (!_reloadingDueToMissingObject)
			{
				Publisher.Publish("SaveScrollPosition", this);
			}
		}

		private void DoneReload()
		{
			// This used to be a BroadcastMessage, but now broadcast is deferred.
			// To keep the same logic it's now using the SendMessageToAllNow.  This
			// is different from SendMessage as it is sent to all even if handled.
			if (!_reloadingDueToMissingObject)
			{
				Publisher.Publish("RestoreScrollPosition", this);
			}
		}

		/// <summary>
		/// Creates a new AndFilter and registers it for later disposal.
		/// </summary>
		private static AndFilter CreateNewAndFilter(params RecordFilter[] filters)
		{
			Debug.Assert(filters.Length > 1, "Need at least two filters to construct an AndFilter");
			var af = new AndFilter();
			foreach (var filter in filters)
			{
				af.Add(filter);
			}
			return af;
		}

		private ICmObject CreateNewObject(int hvoOwner, IList<ClassAndPropInfo> cpiPath)
		{
			if (cpiPath.Count > 2)
			{
				throw new ArgumentException("We currently only support up to 2 levels for creating a new object.");
			}
			if (cpiPath.Count == 2)
			{
				if (cpiPath[1].isVector)
				{
					throw new ArgumentException("We expect the second level to be an atomic property.");
				}
			}
			if (!cpiPath[0].isVector)
			{
				throw new ArgumentException("We expect the first level to be a vector property.");
			}
			ISilDataAccess sda = VirtualListPublisher;
			// assume we need to insert a new object in the vector field following hvoOwner
			var cpi = cpiPath[0];
			var flid = cpi.flid;
			var insertPosition = 0;
			switch ((CellarPropertyType)sda.MetaDataCache.GetFieldType(flid))
			{
				case CellarPropertyType.OwningCollection:
					insertPosition = -1;
					break;
				case CellarPropertyType.OwningSequence:
					if (CurrentObjectHvo != 0 && cpiPath.Count == 1)
					{
						hvoOwner = CurrentObject.Owner.Hvo;
						flid = CurrentObject.OwningFlid;
						var oldIndex = sda.GetObjIndex(hvoOwner, flid, CurrentObjectHvo);
						insertPosition = oldIndex + 1;
					}
					break;
				default:
					// Just possible it's some kind of virtual we can't insert a new object into.
					return null;
			}
			var hvoNew = sda.MakeNewObject(cpi.signatureClsid, hvoOwner, flid, insertPosition);
			// we may need to insert another new class.
			if (cpiPath.Count > 1)
			{
				// assume this is an atomic property.
				var cpiLevel2 = cpiPath[1];
				hvoOwner = hvoNew;
				flid = cpiLevel2.flid;
				hvoNew = sda.MakeNewObject(cpiLevel2.signatureClsid, hvoOwner, flid, -2);
			}
			return hvoNew != 0 ? m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoNew) : null;
		}

		/// <summary>
		/// Answer true if the current object is valid. Currently we just check for deleted objects.
		/// We could try to get an ICmObject and call IsValidObject, but currently that doesn't do
		/// any more than this, and it fails (spuriously) when working with fake objects.
		/// </summary>
		private bool CurrentObjectIsValid
		{
			get
			{
				var hvo = CurrentObjectHvo;
				return hvo != 0 && hvo != (int)SpecialHVOValues.kHvoObjectDeleted;
			}
		}

		/// <summary>
		/// get the index of an item in the list that has a root object that is owned by hvo
		/// </summary>
		/// <param name="hvoTarget"></param>
		/// <returns>-1 if the object is not in the list</returns>
		private int IndexOfChildOf(int hvoTarget)
		{
			// If the list is made up of fake objects, we can't find one of them owned by our target,
			// and trying to will crash, so give up.
			if (SortedObjects.Count == 0 || !m_cache.ServiceLocator.ObjectRepository.IsValidObjectId(((IManyOnePathSortItem)SortedObjects[0]).RootObjectHvo) || !m_cache.ServiceLocator.ObjectRepository.IsValidObjectId(hvoTarget))
			{
				return -1;
			}
			var i = 0;
			foreach (IManyOnePathSortItem item in SortedObjects)
			{
				var rootObject = item.RootObjectUsing(m_cache);
				if (rootObject == null)
				{
					continue;  // may be something that has been deleted?
				}
				for (var owner = rootObject.Owner; owner != null; owner = owner.Owner)
				{
					if (owner.Hvo == hvoTarget)
					{
						return i;
					}
				}
				++i;
			}
			return -1;
		}

		private bool IsCurrentObjectValid()
		{
			return m_cache.ServiceLocator.IsValidObjectId(CurrentObjectHvo);
		}

		private void OnAdjustFilterSelection()
		{
			// NOTE: ListPropertyChoice compares its Value to its parent (ChoiceGroup).SinglePropertyValue
			// to determine whether or not it is the Checked item in the list.
			// Is there any way we could do this in a less kludgy/backdoor sort of way?
			//If we have a filter selected, make sure "No Filter" is not still selected in the menu or toolbar.
			if (_activeMenuBarFilter == null && Filter != null)
			{
				// Resetting the table property value to "Uncheck all" will effectively uncheck this item.
				PropertyTable.SetProperty(CurrentFilterPropertyTableId, FiltersStrings.ksUncheckAll, true, settingsGroup: SettingsGroup.LocalSettings);
			}
			// if no filter is set, then we always want the "No Filter" item selected.
			else if (Filter == null)
			{
				// Resetting the table property value to "No Filter" checks this item.
				PropertyTable.SetProperty(CurrentFilterPropertyTableId, FiltersStrings.ksNoFilter, true, settingsGroup: SettingsGroup.LocalSettings);
			}
		}

		/// <summary>
		/// replace any matching items in our sort list.
		/// </summary>
		private void ReplaceListItem(int hvoReplaced, ListChangedActions listChangeAction = ListChangedActions.Normal)
		{
			var fUpdatingListOrig = UpdatingList;
			UpdatingList = true;
			try
			{
				var hvoOldCurrentObj = CurrentObjectHvo != 0 ? CurrentObjectHvo : 0;
				var newSortItems = new ArrayList();
				var objReplaced = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoReplaced);
				newSortItems.AddRange(ReplaceListItem(objReplaced, hvoReplaced, true));
				if (newSortItems.Count > 0)
				{
					// in general, when adding new items, we want to try to maintain the previous selected *object*,
					// which may have changed index.  so try to find its new index location.
					var indexOfCurrentObj = CurrentIndex;
					if (hvoOldCurrentObj != 0 && CurrentObjectHvo != hvoOldCurrentObj)
					{
						var indexOfOldCurrentObj = IndexOf(hvoOldCurrentObj);
						if (indexOfOldCurrentObj >= 0)
						{
							indexOfCurrentObj = indexOfOldCurrentObj;
						}
					}
					SendPropChangedOnListChange(indexOfCurrentObj, SortedObjects, listChangeAction);
				}
			}
			finally
			{
				UpdatingList = fUpdatingListOrig;
			}
		}

		private bool SuspendLoadListUntilOnChangeFilter
		{
			get
			{
				var toolNameThatExpectsTheSuspend = PropertyTable.GetValue("SuspendLoadListUntilOnChangeFilter", string.Empty, SettingsGroup.LocalSettings);
				return !string.IsNullOrEmpty(toolNameThatExpectsTheSuspend) && toolNameThatExpectsTheSuspend == PropertyTable.GetValue<string>(AreaServices.ToolChoice);
			}
		}

		private void SetupFilterMenu()
		{
			_editFilterMenuHandler = new EditFilterMenuHandler(this);
		}

		private void TearDownFilterMenu()
		{
			_editFilterMenuHandler.Dispose();
			_editFilterMenuHandler = null;
		}

		#endregion Private stuff

		#region Protected stuff

		protected string CurrentFilterPropertyTableId => "currentFilterForRecordList_" + Id;

		/// <summary>
		/// Generally only one record list should respond to record navigation in the user interface.
		/// The "primary" record list is the one that should respond to record navigation.
		/// </summary>
		/// <returns>true iff this record list should respond to record navigation</returns>
		protected virtual bool IsPrimaryRecordList => true;

		protected virtual void UpdateOwningObject(bool fUpdateOwningObjectOnlyIfChanged = false)
		{
			// Subclasses that actually know about a record bar (e.g.; TreeBarHandlerAwarePossibilityRecordList) should override this method.
		}

		protected virtual string GetStatusBarMsgForCurrentObject()
		{
			string msg;
			if (!m_cache.ServiceLocator.IsValidObjectId(CurrentObjectHvo))
			{
				msg = LanguageExplorerResources.ADeletedObject;
			}
			else
			{
				msg = CurrentObject.ToStatusBar();
			}
			return msg;
		}

		/// <summary />
		/// <returns><c>true</c> if we changed or initialized a new filter,
		/// <c>false</c> if the one installed matches the one we had stored to persist.</returns>
		protected virtual bool TryRestoreFilter()
		{
			RecordFilter filter = null;
			var persistFilter = PropertyTable.GetValue<string>(FilterPropertyTableId, SettingsGroup.LocalSettings);
			if (Filter != null)
			{
				// if the persisted object string of the existing filter matches the one in the property table
				// do nothing.
				var currentFilter = DynamicLoader.PersistObject(Filter, "filter");
				if (currentFilter == persistFilter)
				{
					return false;
				}
			}
			if (persistFilter != null)
			{
				try
				{
					filter = DynamicLoader.RestoreObject(persistFilter) as RecordFilter;
					if (filter != null)
					{
						// (LT-9515) restored filters need these set, because they can't be persisted.
						filter.Cache = m_cache;
					}
				}
				catch
				{
					filter = null; // If anything goes wrong, ignore the persisted value.
				}
			}
			if (filter == null || !filter.IsValid)
			{
				filter = _defaultFilter;
			}
			if (Filter == filter)
			{
				return false;
			}
			Filter = filter;
			return true;
		}

		/// <summary />
		/// <returns><c>true</c> if we changed or initialized a new sorter,
		/// <c>false</c>if the one installed matches the one we had stored to persist.</returns>
		protected virtual bool TryRestoreSorter()
		{
			SortName = PropertyTable.GetValue<string>(SortNamePropertyTableId, SettingsGroup.LocalSettings);
			var persistSorter = PropertyTable.GetValue<string>(SorterPropertyTableId, null, SettingsGroup.LocalSettings);
			if (Sorter != null)
			{
				// if the persisted object string of the existing sorter matches the one in the property table
				// do nothing
				var currentSorter = DynamicLoader.PersistObject(Sorter, "sorter");
				if (currentSorter == persistSorter)
				{
					return false;
				}
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
				sorter = _defaultSorter;
				SortName = _defaultSortLabel;
			}
			// If sorter is still null, allow any sorter which may have been installed during
			// record list initialization to prevail.
			if (sorter == null)
			{
				return false; // we didn't change anything.
			}
			// (LT-9515) restored sorters need to set some properties that could not be persisted.
			sorter.Cache = m_cache;
			if (sorter is GenRecordSorter)
			{
				var comparer = ((GenRecordSorter)sorter).Comparer;
				WritingSystemComparer subComparer = null;
				if (comparer != null)
				{
					subComparer = ((StringFinderCompare)comparer).SubComparer as WritingSystemComparer;
				}
				if (subComparer != null)
				{
					var subComparerWsId = subComparer.WsId;
					var wsId = m_cache.WritingSystemFactory.GetWsFromStr(subComparerWsId);
					if (wsId == 0)
					{
						return false;
					}
				}
			}
			if (Sorter == sorter)
			{
				return false;
			}
			// (LT-9515) restored sorters need to set some properties that could not be persisted.
			Sorter = sorter;
			return true;
		}

		/// <summary>
		/// update the contents of the tree bar and anything else that should change when,
		/// for example, the filter or sort order changes.
		/// </summary>
		protected virtual void OnListChanged(int hvo = 0, ListChangedActions actions = ListChangedActions.Normal)
		{
			switch (actions)
			{
				case ListChangedActions.SkipRecordNavigation:
				case ListChangedActions.UpdateListItemName:
					SelectedRecordChanged(false, true);
					break;
				case ListChangedActions.SuppressSaveOnChangeRecord:
					var oldSuppressSaveChangeOnRecord = SuppressSaveOnChangeRecord;
					SuppressSaveOnChangeRecord = true;
					try
					{
						BroadcastChange(false);
					}
					finally
					{
						SuppressSaveOnChangeRecord = oldSuppressSaveChangeOnRecord;
					}

					break;
				case ListChangedActions.Normal:
					BroadcastChange(false);
					break;
				default:
					throw new NotSupportedException("An enum choice for ListChangedEventArgs was selected that OnListChanged is not aware of.");
			}
		}

		/// <summary>
		/// Override this (initially only in InterlinTextsRecordList) if the record list knows how to add an
		/// item to the current list/filter on request.
		/// </summary>
		protected virtual bool AddItemToList(int hvoItem)
		{
			return false;
		}

		/// <summary>
		/// Override this if there are special cases where you need more control over which objects can be deleted.
		/// </summary>
		protected virtual bool CanDelete()
		{
			return CurrentObject.CanDelete;
		}

		/// <summary>
		/// By default we just silently don't delete things that shouldn't be. Override if you want to give a message.
		/// </summary>
		protected virtual void ReportCannotDelete()
		{
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
			// to be safe we just do a full refresh.
			PropertyTable.GetValue<IApp>(LanguageExplorerConstants.App).RefreshAllViews();
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
			{
				return; // none of our business!
			}
			var msg = FilterStatusContents(Filter != null && Filter.IsUserVisible);
			if (IgnoreStatusPanel)
			{
				Publisher.Publish("DialogFilterStatus", msg);
			}
			else
			{
				StatusBarPanelServices.SetStatusBarPanelFilter(_statusBar, msg);
			}
		}

		protected virtual string FilterStatusContents(bool listIsFiltered)
		{
			return listIsFiltered ? LanguageExplorerResources.Filtered : string.Empty;
		}

		/// <summary>
		/// Gets or sets a flag indicating whether the list was already loaded in order and
		/// does not need to be sorted further.
		/// </summary>
		protected virtual bool ListAlreadySorted => false;

		protected virtual bool TryHandleUpdateOrMarkPendingReload(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			if (tag == m_flid)
			{
				if (m_owningObject != null && m_owningObject.Hvo != hvo)
				{
					return true;        // This PropChanged doesn't really apply to us.
				}
				ReloadList(ivMin, cvIns, cvDel);
				return true;
			}
			if (tag == SegmentTags.kflidAnalyses && ((ObjectListPublisher)VirtualListPublisher).OwningFieldName == "Wordforms")
			{
				// Changing this potentially changes the list of wordforms that occur in the interesting texts.
				// Hopefully we don't rebuild the list every time; usually this can only be changed in another view.
				// In case we DO have a concordance active in one window while editing another, if this isn't the
				// active window postpone until it is.
				var window = PropertyTable.GetValue<Form>(FwUtils.window);
				if (window != Form.ActiveForm)
				{
					RequestedLoadWhileSuppressed = true;
					RequestReloadOnActivation(window);
					return true;
				}
				ReloadList();
				return true;
			}
			// This may be a change to content we depend upon.
			// 1) see if the property is the VirtualFlid of the owning record list. If so,
			// the owning record list has reloaded, so we should also reload.
			IRecordList listProvidingRootObject;
			if (TryListProvidingRootObject(out listProvidingRootObject) && listProvidingRootObject.VirtualFlid == tag && cvDel > 0)
			{
				// we're deleting or replacing items, so assume need to reload.
				// we want to wait until after everything is finished reloading, however.
				m_requestedLoadWhileSuppressed = true;
				return true;
			}
			// 2) Entries depend upon a few different properties.
			if (m_flid != m_cache.ServiceLocator.GetInstance<Virtuals>().LexDbEntries || !EntriesDependsUponProp(tag))
			{
				return false;
			}
			MarkEntriesForReload();
			return true;
		}

		protected virtual void MarkEntriesForReload()
		{
			m_fReloadLexEntries = true;
		}

		/// <summary>
		/// see if a particular list can modify its list in place, instead of triggering an entire reload.
		/// </summary>
		protected virtual bool TryModifyingExistingList(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			return false;
		}

		protected bool EntriesDependsUponProp(int tag)
		{
			return false;
		}

		/// <summary />
		/// <returns>indices of invalid sort items in reverse order, so that the caller can
		/// remove those invalid indices from SortedObjects without affecting the validity of the next invalid index.</returns>
		protected int[] IndicesOfInvalidSortItems()
		{
			var indices = new List<int>();
			var objectSet = new HashSet<int>(GetObjectSet());
			for (var i = 0; i < SortedObjects.Count; i++)
			{
				var rootHvo = RootHvoAt(i);
				if (!m_cache.ServiceLocator.IsValidObjectId(rootHvo) || !objectSet.Contains(rootHvo))
				{
					indices.Insert(0, i);
				}
			}
			return indices.ToArray();
		}

		protected int IndexOfFirstSortItem(List<int> hvoTargets)
		{
			var indices = IndicesOfSortItems(hvoTargets, true);
			return indices.Length > 0 ? indices[0] : -1;
		}

		/// <summary>
		/// this should refresh the display of field values for existing items, rather than altering the
		/// size of the list (e.g. updating list item names)
		/// </summary>
		protected void UpdateListItemName(int hvo)
		{
			var cList = VirtualListPublisher.get_VecSize(m_owningObject.Hvo, m_flid);
			if (cList != 0)
			{
				OnListChanged(hvo, ListChangedActions.UpdateListItemName);
			}
			else
			{
				// we don't have any more items in our list, so we don't have an item name to update.
				if (SortedObjects.Count > 0)
				{
					// somehow in the process of deleting objects we didn't also remove our SortObjects
					// (e.g. LT-8735), so reload our list. this shouldn't take much time
					// since we don't expect it to have any items.
					ReloadList();
				}
			}
		}

		/// <summary>
		/// Replace SortObjects corresponding to hvoToReplace with new SortObjects for newObj.
		/// </summary>
		/// <param name="newObj"></param>
		/// <param name="hvoToReplace"></param>
		/// <param name="fAssumeSame">if true, we'll try to replace sort objects for hvoToReplace with newObj at the same indices.
		/// if false, we'll rely upon sorter to merge the new item into the right index, or else add to the end.
		/// Enhance: Is there some way we can compare the sort/filter results for newObj and hvoToReplace that is hvo indepedendent?</param>
		/// <returns>resulting list of newSortItems added to SortedObjects</returns>
		protected ArrayList ReplaceListItem(ICmObject newObj, int hvoToReplace, bool fAssumeSame)
		{
			var newSortItems = new ArrayList();
			var indicesOfSortItemsToRemove = new List<int>(IndicesOfSortItems(new List<int>(new int[] { hvoToReplace })));
			var remainingInsertItems = new ArrayList();
			var hvoNewObject = 0;
			if (newObj != null)
			{
				hvoNewObject = newObj.Hvo;
				// we don't want to add new sort items, if we've already added them, but we do want to allow
				// a replacement.
				if (hvoToReplace == hvoNewObject || IndexOfFirstSortItem(new List<int>(new[] { hvoNewObject })) < 0)
				{
					MakeItemsFor(newSortItems, newObj.Hvo);
				}
				remainingInsertItems = (ArrayList)newSortItems.Clone();
				if (fAssumeSame)
				{
					//assume we're converting a dummy item to a real one.
					//In that case, the real item should have same basic content as the dummy item we are replacing,
					//so we can replace the item at the same sortItem indices.
					foreach (var itemToInsert in newSortItems)
					{
						if (indicesOfSortItemsToRemove.Count > 0)
						{
							var iToReplace = indicesOfSortItemsToRemove[0];
							SortedObjects.RemoveAt(iToReplace);
							SortedObjects.Insert(iToReplace, itemToInsert);
							indicesOfSortItemsToRemove.RemoveAt(0);
							remainingInsertItems.RemoveAt(0);
						}
						else
						{
							break;
						}
					}
				}
			}
			// Although, ideally, during a dummy conversion there should be a one-to-one correspondence between
			// the sort items found for the dummy object, and the sort items generated for its real object,
			// it's possible that at the time we added the dummy item to the record sort list, it didn't
			// have the same properties matching a filter or sorter as the real item. Try to do the best we
			// can by removing remaining sort items for the dummy object and then adding any additional sort items
			// for the real object.
			// remove the remaining items.
			indicesOfSortItemsToRemove.Sort();
			indicesOfSortItemsToRemove.Reverse();
			foreach (var iToRemove in indicesOfSortItemsToRemove)
			{
				SortedObjects.RemoveAt(iToRemove);
			}
			// add the remaining items.
			if (m_sorter != null)
			{
				m_sorter.DataAccess = VirtualListPublisher; // for safety, ensure this any time we use it.
				m_sorter.SetPercentDone = DoNothing; // no progress for inserting one item
				m_sorter.MergeInto(SortedObjects, remainingInsertItems);
			}
			else
			{
				var items = GetObjectSet().ToList();
				// if we're inserting only one item, try to guess the best position.
				// we can try to assume that SortedObjects is in the same order as items if there is no filter.
				// so go through SortedObjects and insert the new sortedItem at the same place as in items.
				if (remainingInsertItems.Count == 1 && remainingInsertItems[0] is IManyOnePathSortItem && m_filter == null && SortedObjects.Count == (items.Count - 1))
				{
					var newSortedObject = remainingInsertItems[0] as IManyOnePathSortItem;
					for (var i = 0; i < SortedObjects.Count; i++)
					{
						if (items[i] == SortItemAt(i).RootObjectHvo)
						{
							continue; // already in sorted objects.
						}
						if (items[i] == newSortedObject.RootObjectHvo)
						{
							SortedObjects.Insert(i, newSortedObject);
						}
						else
						{
							// something didn't line up as we expect, so just insert at end.
							SortedObjects.Add(newSortedObject);
						}
						remainingInsertItems.Remove(newSortedObject);
						break;
					}
					// if we still haven't added newSortedObject, just add it to the end of the
					// SortedObjects below.
				}
				if (remainingInsertItems.Count > 0)
				{
					// Add at the end.
					SortedObjects.AddRange(remainingInsertItems);
				}
			}
			// update our current selected hvo, if necessary
			if (m_hvoCurrent == hvoToReplace)
			{
				m_hvoCurrent = hvoNewObject;
			}
			return newSortItems;
		}

		protected void AdjustCurrentIndex()
		{
			if (m_currentIndex < 0 && SortedObjects.Count == 1)
			{
				// Prevent assertion accessing CurrentIndex when inserting the very first item in a list.
				CurrentIndex = 0;
				m_hvoCurrent = CurrentObjectHvo;
			}
			else
			{
				// Update m_currentIndex if necessary.  The index should either stay the
				// same (if the added object comes later in the list) or be incremented by
				// one (if the added object comes earlier in the list).
				if (CurrentObjectHvo != m_hvoCurrent)
				{
					++CurrentIndex;
					Debug.Assert(CurrentObjectHvo == m_hvoCurrent);
				}
			}
		}

		protected ArrayList MakeSortItemsFor(int[] hvos)
		{
			var newSortedObjects = new ArrayList(hvos.Length);
			foreach (var hvo in hvos)
			{
				MakeItemsFor(newSortedObjects, hvo);
			}
			return newSortedObjects;
		}

		protected virtual bool UpdatePrivateList()
		{
			return false;
		}

		protected virtual void FinishedReloadList()
		{
			m_fReloadLexEntries = false;
		}

		protected virtual int GetNewCurrentIndex(ArrayList newSortedObjects, int hvoCurrent)
		{
			return IndexOf(newSortedObjects, hvoCurrent);
		}

		protected void SortList(ArrayList newSortedObjects, ProgressState progress)
		{
			if (m_sorter == null || ListAlreadySorted)
			{
				return;
			}
			m_sorter.DataAccess = VirtualListPublisher;
			if (m_sorter is IReportsSortProgress)
			{
				// Uses the last 80% of the bar (first part used for building the list).
				((IReportsSortProgress)m_sorter).SetPercentDone = percent =>
				{
					progress.PercentDone = 20 + percent * 4 / 5;
					progress.Breath();
				};
			}
			m_sorter.Sort( /*ref*/ newSortedObjects); //YiSpeed 1 secs
			m_sorter.SetPercentDone = DoNothing; // progress about to be disposed.
		}

		protected int MakeItemsFor(ArrayList sortedObjects, int hvo)
		{
			var start = sortedObjects.Count;
			if (m_sorter == null)
			{
				sortedObjects.Add(new ManyOnePathSortItem(hvo, null, null));
			}
			else
			{
				m_sorter.DataAccess = VirtualListPublisher;
				m_sorter.CollectItems(hvo, sortedObjects);
			}
			if (m_filter != null && m_fEnableFilters)
			{
				m_filter.DataAccess = VirtualListPublisher;
				for (var i = start; i < sortedObjects.Count;)
				{
					if (m_filter.Accept(sortedObjects[i] as IManyOnePathSortItem))
					{
						i++; // advance loop if we don't delete!
					}
					else
					{
						sortedObjects.RemoveAt(i);
					}
				}
			}
			return sortedObjects.Count - start;
		}

		protected void SendPropChangedOnListChange(int newCurrentIndex, ArrayList newSortedObjects, ListChangedActions actions)
		{
			//Populate the virtual cache property which will hold this set of hvos, in this order.
			var hvos = new int[newSortedObjects.Count];
			var i = 0;
			foreach (IManyOnePathSortItem item in newSortedObjects)
			{
				hvos[i++] = item.RootObjectHvo;
			}
			// In case we're already displaying it, we must have an accurate old length, or if it gets shorter
			// the extra ones won't get deleted. But we must check whether the property is already cached...
			// if it isn't and we try to read it, the code will try to load this fake property from the database,
			// with unfortunate results. (It's not exactly a reference sequence, but any sequence type will do here.)
			AboutToReload();
			// Must not actually change anything before we do AboutToReload! Then do it all at once
			// before we make any notifications...we want the cache value to always be consistent
			// with m_sortedObjects, and m_currentIndex always in range
			SortedObjects = newSortedObjects;
			// if we haven't already set an index, see if we can restore one from the property table.
			if (SortedObjects.Count > 0 && (newCurrentIndex == -1 || m_hvoCurrent == 0))
			{
				newCurrentIndex = PropertyTable.GetValue(PersistedIndexProperty, 0, SettingsGroup.LocalSettings);
			}
			// Ensure the index is in bounds.  See LT-10349.
			if (SortedObjects.Count > 0)
			{
				// The stored value may be past the end of the current collection,
				// so set it to 0 in that case.
				// It sure beats the alternative of a drop dead crash. :-)
				if (newCurrentIndex > SortedObjects.Count - 1 || newCurrentIndex < 0)
				{
					newCurrentIndex = 0; // out of bounds, so set it to 0.
				}
			}
			else
			{
				newCurrentIndex = -1;
			}
			CurrentIndex = newCurrentIndex;
			((ObjectListPublisher)VirtualListPublisher).CacheVecProp(m_owningObject.Hvo, hvos);
			m_oldLength = hvos.Length;
			//TODO: try to stay on the same record
			// Currently Attempts to keep us at the same index as we were.
			// we should try hard to keep us on the actual record that we were currently on,
			// since the reload may be a result of changing the sort order, in which case the index is meaningless.
			//make sure the hvo index is in a reasonable spot
			if (m_currentIndex >= hvos.Length)
			{
				CurrentIndex = hvos.Length - 1;
			}
			if (m_currentIndex < 0)
			{
				CurrentIndex = (hvos.Length > 0) ? 0 : -1;
			}
			DoneReload();
			// Notify any delegates that the selection of the main object in the vector has changed.
			if (m_fEnableSendPropChanged)
			{
				OnListChanged(0, actions);
			}
		}

		protected virtual IEnumerable<int> GetObjectSet()
		{
			return DomainObjectServices.GetObjectSet(VirtualListPublisher, m_cache.ServiceLocator.GetInstance<ICmObjectRepository>(), m_owningObject, m_flid);
		}

		// get the index of the given hvo, where it occurs as a root object in
		// one of the IManyOnePathSortItems in the given list.
		protected int IndexOf(ArrayList objects, int hvo)
		{
			var i = 0;
			if (objects != null && hvo != 0)
			{
				foreach (IManyOnePathSortItem item in objects)
				{
					if (item.RootObjectHvo == hvo)
					{
						return i;
					}
					++i;
				}
			}
			return -1;
		}

		protected virtual ClassAndPropInfo GetMatchingClass(string className)
		{
			return m_insertableClasses.FirstOrDefault(cpi => cpi.signatureClassName == className);
		}

		protected void ComputeInsertableClasses()
		{
			m_insertableClasses = new List<ClassAndPropInfo>();
			if (OwningObject is ICmPossibilityList && (OwningObject as ICmPossibilityList).IsClosed)
			{
				// You can't insert anything in a closed list!
				return;
			}
			if (m_flid != ObjectListPublisher.OwningFlid)
			{
				m_cache.AddClassesForField(m_flid, true, m_insertableClasses);
			}
		}

		protected virtual void ActivateRecordBar()
		{
			// Subclasses that actually know about a record bar (e.g.; TreeBarHandlerAwarePossibilityRecordList) should override this method.
		}

		protected virtual void UpdateStatusBarForRecordBar()
		{
			// Subclasses that actually know about a record bar (e.g.; TreeBarHandlerAwarePossibilityRecordList) should override this method.
		}

		protected virtual void UpdateSelectionForRecordBar()
		{
			// Subclasses that actually know about a record bar (e.g.; TreeBarHandlerAwarePossibilityRecordList) should override this method.
		}

		/// <summary>
		/// Sort and filter the underlying property to create the current list of objects.
		/// </summary>
		protected virtual void ReloadList()
		{
			// Skip multiple reloads and reloading when our record list is not active.
			if (m_reloadingList)
			{
				return;
			}
			if (m_suppressingLoadList || !IsActiveInGui || SuspendLoadListUntilOnChangeFilter)
			{
				// if we need to reload the list
				// clear the views property until we are no longer suppressed, so dependent views don't try to access objects
				// that have possibly been deleted.
				if (m_owningObject != null && SortedObjects.Count > 0 && ((UpdateHelper != null && UpdateHelper.ClearBrowseListUntilReload) || !IsActiveInGui))
				{
					// try to restore this index during reload.
					m_indexToRestoreDuringReload = CurrentIndex;
					// clear everything for now, including the current index, but don't issue a RecordNavigation.
					SendPropChangedOnListChange(-1, new ArrayList(), ListChangedActions.SkipRecordNavigation);
				}
				m_requestedLoadWhileSuppressed = true;
				// it's possible that we'll want to reload once we become the main active window (cf. LT-9251)
				var window = PropertyTable.GetValue<Form>(FwUtils.window);
				var app = PropertyTable.GetValue<IApp>(LanguageExplorerConstants.App);
				if (window != null && app != null && window != app.ActiveMainWindow)
				{
					// make sure we don't install more than one.
					RequestReloadOnActivation(window);
				}
				return;
			}
			try
			{
				m_requestedLoadWhileSuppressed = false;
				if (UpdateHelper != null && UpdateHelper.ClearBrowseListUntilReload)
				{
					if (m_indexToRestoreDuringReload != -1)
					{
						// restoring m_currentIndex directly isn't effective until SortedObjects
						// is greater than 0.
						// so, try to force to restore the current index to what we persist.
						CurrentIndex = -1;
#if RANDYTODO
// As of 21JUL17 nobody cares about that 'PersistedIndexProperty' changing, so skip the broadcast.
#endif
						PropertyTable.SetProperty(PersistedIndexProperty, m_indexToRestoreDuringReload, true, settingsGroup: SettingsGroup.LocalSettings);
						m_indexToRestoreDuringReload = -1;
					}
					UpdateHelper.ClearBrowseListUntilReload = false;
				}
				m_reloadingList = true;
				if (UpdatePrivateList())
				{
					return; // Cannot complete the reload until PropChangeds complete.
				}
				var newCurrentIndex = CurrentIndex;
#if RANDYTODO
				// TODO: Replace use of ArrayList with List<IManyOnePathSortItem>.
#endif
				ArrayList newSortedObjects;
				ListChangedActions actions;

				// Get the HVO of the current object (but only if it hasn't been deleted).
				// If it has, don't modify m_currentIndex, as the old position is less likely to
				// move the list than going to the top.
				// (We want to keep the current OBJECT, not index, if it's still a real object,
				// because the change that produced the regenerate might be a change of filter or sort
				// that moves it a lot. But if the change is an object deletion, we want to not change
				// the position if we can help it.)
				var hvoCurrent = 0;
				if (m_sortedObjects != null && m_currentIndex != -1 && m_sortedObjects.Count > m_currentIndex && CurrentObjectIsValid)
				{
					hvoCurrent = CurrentObjectHvo;
				}
				//this happens when the set is dependent on another one, but no item is selected in the
				//primary list. For example, if there are no word forms, then the list which holds the analyses
				//of a selected wordform will not have been owning object from which to pull analyses.
				//or in the case of FWR-3171 the owning object has been deleted.
				//But the following doesn't work because CurrentObjectIsValid also checks for hvo=0.
				//if (m_owningObject == null || !CurrentObjectIsValid)
				if (m_owningObject == null || m_owningObject.Hvo == (int)SpecialHVOValues.kHvoObjectDeleted)
				{
					SortedObjects = new ArrayList(0);
					// We should not do SendPropChangedOnListChange, because that caches a property
					// on m_owningObject, and issues various notifications based on its existence.
					// Nothing should be displaying the list if there is no root object.
					// However, as a safety precaution in case we previously had one, let's fix the
					// current object information.
					CurrentIndex = -1;
					m_hvoCurrent = 0;
					// we still need to broadcast the list changed to update dependent views. (LT-5987)
					OnListChanged();
					return;
				}
				try
				{
					newSortedObjects = GetFilteredSortedList();
				}
				catch (LcmInvalidFieldException)
				{
					newSortedObjects = HandleInvalidFilterSortField();
				}
				catch (FwConfigurationException ce)
				{
					if (ce.InnerException is LcmInvalidFieldException)
					{
						newSortedObjects = HandleInvalidFilterSortField();
					}
					else
					{
						throw;
					}
				}
				// Try to stay on the same object if possible.
				if (hvoCurrent != 0 && newSortedObjects.Count > 0)
				{
					newCurrentIndex = GetNewCurrentIndex(newSortedObjects, hvoCurrent);
					if (newCurrentIndex < 0 && newSortedObjects.Count > 0)
					{
						newCurrentIndex = 0; // expected but not found: move to top, but only if there are items in the list.
						actions = ListChangedActions.Normal; // This is a full-blown record change
					}
					else
					{
						// The index changed, so we need to broadcast RecordNavigate, but since we didn't actually change objects,
						// we shouldn't do a save
						actions = ListChangedActions.SuppressSaveOnChangeRecord;
					}
				}
				else
				{
					// We didn't even expect to find it, probably it's been deleted or sorted list has become empty.
					// Keep the current position as far as possible.
					newCurrentIndex = newCurrentIndex >= newSortedObjects.Count ? newSortedObjects.Count - 1 : GetPersistedCurrentIndex(newSortedObjects.Count);
					actions = ListChangedActions.Normal; // We definitely changed records
				}
				SendPropChangedOnListChange(newCurrentIndex, newSortedObjects, actions);
				//YiSpeed 6.5 secs (mostly filling tree bar)
				FinishedReloadList();
			}
			finally
			{
				m_reloadingList = false;
			}
		}

		protected virtual void ReloadList(int newListItemsClass, int newTargetFlid, bool force)
		{
			// Let a bulk-edit record list handle this.
		}

		protected virtual bool CanInsertClass(string className)
		{
			return (GetMatchingClass(className) != null);
		}

		/// <summary>
		/// Change the sorter...and resort if the list already exists.
		/// </summary>
		/// <param name="sorter"></param>
		protected virtual void ChangeSorter(RecordSorter sorter)
		{
			Sorter = sorter;

			// JohnT: a different sorter may create a quite different set of IManyOnePathSortItems.
			// Optimize: it may be possible to find some cases in which we don't need to reload fully,
			// for example, when reversing the order on the same column.
			if (m_sortedObjects != null)
			{
				ReloadList();
			}
		}

		/// <summary>
		/// Create an object of the specified class.
		/// </summary>
		/// <returns>true if successful (the class is known)</returns>
		protected virtual bool CreateAndInsert(string className)
		{
			var cpi = GetMatchingClass(className);
			Debug.Assert(cpi != null, "This object should not have been asked to insert an object of the class " + className + ".");
			if (cpi == null)
			{
				return false;
			}
			var cpiPath = new List<ClassAndPropInfo>(new[] { cpi });
			var createAndInsertMethodObj = new CpiPathBasedCreateAndInsert(m_owningObject.Hvo, cpiPath, this);
			var newObj = DoCreateAndInsert(createAndInsertMethodObj);
			var hvoNew = newObj?.Hvo ?? 0;
			return hvoNew != 0; // If we get zero, we couldn't do it for some reason.
		}

		/// <summary>
		/// Delete the current object.
		/// In some cases thingToDelete is not actually the current object, but it should always
		/// be related to it.
		/// </summary>
		protected virtual void DeleteCurrentObject(ICmObject thingToDelete = null)
		{
			if (thingToDelete == null)
			{
				thingToDelete = CurrentObject;
			}
			try
			{
				// This can happen in some bizarre cases, such as reconciling with another client
				// just before delete, if the other client also deleted the same object.
				if (!IsCurrentObjectValid() || !thingToDelete.IsValidObject)
				{
					return;
				}
				m_deletingObject = true;
				using (new ListUpdateHelper(new ListUpdateHelperParameterObject { MyRecordList = this }))
				{
					var updatingListOrig = UpdatingList;
					UpdatingList = true;
					try
					{
						thingToDelete.Delete();
					}
					finally
					{
						UpdatingList = updatingListOrig;
					}
				}
			}
			finally
			{
				m_deletingObject = false;
			}
		}

		/// <summary>
		/// Create and Insert an item in a list. If this is a hierarchical list, then insert it at the same level
		/// as the current object.
		/// SuppressSaveOnChangeRecord will be true, to allow the user to Undo this action.
		/// NOTE: the caller may want to call SaveOnChangeRecord() before calling this.
		/// </summary>
		protected TObj DoCreateAndInsert<TObj>(ICreateAndInsert<TObj> createAndInsertMethodObj)
			where TObj : ICmObject
		{
			TObj newObj;
			int hvoNew;
			var options = new ListUpdateHelperParameterObject
			{
				MyRecordList = this,
				SuspendPropChangedDuringModification = true
			};
			using (new ListUpdateHelper(options))
			{
				newObj = createAndInsertMethodObj.Create();
				hvoNew = newObj != null ? newObj.Hvo : 0;
				if (hvoNew != 0)
				{
					ReplaceListItem(hvoNew, ListChangedActions.SuppressSaveOnChangeRecord);
				}
			}
			CurrentIndex = IndexOf(hvoNew);
			return newObj;
		}

		/// <summary>
		/// Return the index (in m_sortedObjects) of the first displayed object.
		/// (In hierarchical lists, this is not necessarily the first item.)
		/// If the list is empty return -1.
		/// </summary>
		protected virtual int FirstItemIndex => m_sortedObjects == null || m_sortedObjects.Count == 0 ? -1 : 0;

		/// <summary>
		/// Used on occasions like changing views, this should suppress any optimization that prevents real reloads.
		/// </summary>
		protected virtual void ForceReloadList()
		{
			ReloadList();  // By default nothing special is needed.
		}

		/// <summary>
		/// get the index of an item in the list that has a root object that (directly or indirectly) owns hvo
		/// </summary>
		/// <returns>-1 if the object is not in the list</returns>
		protected int IndexOfParentOf(int hvoTarget)
		{
			// If the list is made up of fake objects, we can't find one of them that our target owns,
			// and trying to will crash, so give up.
			if (SortedObjects.Count == 0 || !m_cache.ServiceLocator.ObjectRepository.IsValidObjectId(((IManyOnePathSortItem)SortedObjects[0]).RootObjectHvo))
			{
				return -1;
			}
			var target = m_cache.ServiceLocator.ObjectRepository.GetObject(hvoTarget);
			var owners = new HashSet<int>();
			for (var owner = target.Owner; owner != null; owner = owner.Owner)
			{
				owners.Add(owner.Hvo);
			}
			var i = 0;
			foreach (IManyOnePathSortItem item in SortedObjects)
			{
				if (owners.Contains(item.RootObjectHvo))
				{
					return i;
				}
				++i;
			}
			return -1;
		}

		protected virtual void InitLoad(bool loadList)
		{
			m_sda = VirtualListPublisher; // needed before ComputeInsertableClasses().
			m_sda.AddNotification(this);
			ComputeInsertableClasses();
			CurrentIndex = -1;
			m_hvoCurrent = 0;
			if (loadList)
			{
				ReloadList();
			}
			else
			{
				ListLoadingSuppressed = true;
				RequestedLoadWhileSuppressed = true;
			}
		}

		/// <summary>
		/// Return the index (in m_sortedObjects) of the last displayed object.
		/// (In hierarchical lists, this is not necessarily the last item.)
		/// If the list is empty return -1.
		/// </summary>
		protected virtual int LastItemIndex => m_sortedObjects == null || m_sortedObjects.Count == 0 ? -1 : m_sortedObjects.Count - 1;

		protected virtual bool NeedToReloadList()
		{
			var fReload = RequestedLoadWhileSuppressed;
			if (m_flid == m_cache.ServiceLocator.GetInstance<Virtuals>().LexDbEntries)
			{
				fReload |= ReloadLexEntries;
			}
			return fReload;
		}

		/// <summary>
		/// Return the index (in m_sortedObjects) of the object that follows the one
		/// at m_currentIndex.
		/// (In hierarchical lists, this is not necessarily the item at index + 1.)
		/// If the list is empty return -1.
		/// If the current object is the last return m_currentIndex.
		/// If m_currentIndex is -1 return -1.
		/// </summary>
		protected virtual int NextItemIndex => m_sortedObjects == null || m_sortedObjects.Count == 0 || m_currentIndex == -1 ? -1 : Math.Min(m_currentIndex + 1, m_sortedObjects.Count - 1);

		protected void OnChangeSorter()
		{
			using (new WaitCursor(PropertyTable.GetValue<Form>(FwUtils.window)))
			{
				Logger.WriteEvent($"Sorter changed: {Sorter?.ToString() ?? "(no sorter)"}");
				SorterChangedByList?.Invoke(this, EventArgs.Empty);
			}
		}

		/// <summary>
		/// Return the index (in m_sortedObjects) of the object that precedes the one
		/// at m_currentIndex.
		/// (In hierarchical lists, this is not necessarily the item at index - 1.)
		/// If the list is empty return -1.
		/// If the current object is the first return m_currentIndex.
		/// If m_currentIndex is -1 return -1.
		/// </summary>
		protected virtual int PrevItemIndex => m_sortedObjects == null || m_sortedObjects.Count == 0 || m_currentIndex == -1 ? -1 : Math.Max(m_currentIndex - 1, 0);

		protected virtual string PropertyTableId(string sorterOrFilter)
		{
			// Dependent lists do not have owner/property set. Rather they have class/field.
			var className = VirtualListPublisher.MetaDataCache.GetOwnClsName(m_flid);
			var fieldName = VirtualListPublisher.MetaDataCache.GetFieldName(m_flid);
			if (string.IsNullOrEmpty(PropertyName) || PropertyName == fieldName)
			{
				return $"{className}.{fieldName}_{sorterOrFilter}";
			}
			return $"{className}.{PropertyName}_{sorterOrFilter}";
		}

		protected virtual bool TryListProvidingRootObject(out IRecordList recordListProvidingRootObject)
		{
			recordListProvidingRootObject = null;
			return false;
		}

		internal ListUpdateHelper UpdateHelper { get; set; }

		#endregion Protected stuff

		#endregion Non-interface code

		private sealed class CpiPathBasedCreateAndInsert : ICreateAndInsert<ICmObject>
		{
			internal CpiPathBasedCreateAndInsert(int hvoOwner, IList<ClassAndPropInfo> cpiPath, RecordList list)
			{
				HvoOwner = hvoOwner;
				CpiPath = cpiPath;
				List = list;
			}

			private readonly int HvoOwner;
			private readonly IList<ClassAndPropInfo> CpiPath;
			private readonly RecordList List;

			#region ICreateAndInsert<ICmObject> Members

			public ICmObject Create()
			{
				return List.CreateNewObject(HvoOwner, CpiPath);
			}

			#endregion
		}

		private sealed class EditFilterMenuHandler : IDisposable
		{
			private ToolStripMenuItem _viewFilterMenuItem;
			private RecordList _recordList;

			internal EditFilterMenuHandler(RecordList recordList)
			{
				_recordList = recordList;
				var majorFlexComponentParameters = _recordList.PropertyTable?.GetValue<MajorFlexComponentParameters>(LanguageExplorerConstants.MajorFlexComponentParameters);
				if (majorFlexComponentParameters == null)
				{
					// Tests may not have the property set.
					return;
				}
				_viewFilterMenuItem = MenuServices.GetViewFilterMenu(majorFlexComponentParameters.MenuStrip);
				CreateFilterMenus();
			}

			/// <summary>
			/// Create any needed filters and wire up event handler(s) for user provided filters,
			/// and for the permanent "No Filters" submenu.
			/// </summary>
			private void CreateFilterMenus()
			{
				_viewFilterMenuItem.DropDownItems[0].Click += NoFiltersMenu_Clicked;
				_viewFilterMenuItem.DropDownItems[0].Tag = new NoFilters();
				if (_recordList._filterProvider != null)
				{
					foreach (var filter in _recordList._filterProvider.Filters)
					{
						var filterMenu = new ToolStripMenuItem(FiltersStrings.ksUnknown, LanguageExplorerResources.FWFilterBasic_Small, OtherFilterMenu_Clicked)
						{
							Tag = filter
						};
						_viewFilterMenuItem.DropDownItems.Add(filterMenu);
					}
				}
			}

			private void NoFiltersMenu_Clicked(object sender, EventArgs e)
			{
				FilterMenuClickedCommon(FiltersStrings.ksUncheckAll);
			}

			private void OtherFilterMenu_Clicked(object sender, EventArgs eventArgs)
			{
				FilterMenuClickedCommon(FiltersStrings.ksNoFilter);
			}

			private void FilterMenuClickedCommon(string newPropertyValue)
			{
				_recordList.PropertyTable.SetProperty(_recordList.CurrentFilterPropertyTableId, newPropertyValue, true, settingsGroup: SettingsGroup.LocalSettings);
				_recordList.OnChangeFilterToCheckedListPropertyChoice();
			}

			#region IDisposable

			private bool _isDisposed;

			/// <summary>
			/// Finalizer, in case client doesn't dispose it.
			/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
			/// </summary>
			~EditFilterMenuHandler()
			{
				Dispose(false);
				// The base class finalizer is called automatically.
				GC.SuppressFinalize(this);
			}

			/// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
			public void Dispose()
			{
				Dispose(true);
				// This object will be cleaned up by the Dispose method.
				// Therefore, you should call GC.SuppressFinalize to
				// take this object off the finalization queue
				// and prevent finalization code for this object
				// from executing a second time.
				GC.SuppressFinalize(this);
			}

			private void Dispose(bool disposing)
			{
				Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
				if (_isDisposed)
				{
					// No need to do it more than once.
					return;
				}

				if (disposing)
				{
					if (_viewFilterMenuItem != null)
					{
						var goners = new List<ToolStripMenuItem>();
						for (var idx = 0; idx < _viewFilterMenuItem.DropDownItems.Count; ++idx)
						{
							var currentMenu = (ToolStripMenuItem)_viewFilterMenuItem.DropDownItems[idx];
							currentMenu.Tag = null;
							if (idx == 0)
							{
								// Just unwire handler, but leave menu in.
								currentMenu.Click -= NoFiltersMenu_Clicked;
							}
							else
							{
								// Unwire event handler and remove menu item.
								currentMenu.Click -= OtherFilterMenu_Clicked;
								goners.Add(currentMenu);
							}
						}
						foreach (var goner in goners)
						{
							_viewFilterMenuItem.DropDownItems.Remove(goner);
							goner.Dispose();
						}
					}
				}
				_viewFilterMenuItem = null;
				_recordList = null;

				_isDisposed = true;
			}
			#endregion
		}
	}
}