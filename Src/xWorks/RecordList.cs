// Copyright (c) 2004-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: RecordList.cs
// History: John Hatton, created
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.FDO.DomainImpl;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Filters;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Widgets;
using System.Windows.Forms;
using SIL.FieldWorks.Common.RootSites;
using SIL.CoreImpl;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// RecordList is a vector of objects
	/// </summary>
	public class RecordList : FwDisposableBase, IFlexComponent, IVwNotifyChange, ISortItemProvider
	{
		#region Events

		/// <summary>
		/// fired when the list changes
		/// </summary>
		public event ListChangedEventHandler ListChanged;
		public event EventHandler AboutToReload;
		public event EventHandler DoneReload;

		#endregion Events

		#region Data members

		private const int RecordListFlid = 89999956;

		/// <summary>
		/// we want to keep a reference to our window, separate from the PropertyTable
		/// in case there's a race condition during dispose with the window
		/// getting removed from the PropertyTable before it's been disposed.
		/// </summary>
		Form m_windowPendingOnActivated;
		protected FdoCache m_cache;
		/// <summary>
		/// Use this to do the Add/RemoveNotifications, since it can be used in the unmanged section of Dispose.
		/// (If m_sda is COM, that is.)
		/// Doing it there will be safer, since there was a risk of it not being removed
		/// in the mananged section, as when disposing was done by the Finalizer.
		/// </summary>
		private ISilDataAccess m_sda;
		protected RecordClerk m_clerk;
		/// <summary>
		/// A reference to a sorter.
		/// </summary>
		protected RecordSorter m_sorter;
		/// <summary>
		/// A reference to a filter.
		/// </summary>
		protected RecordFilter m_filter;
		protected RecordFilter m_filterPrev;
		protected string m_propertyName = string.Empty;
		protected string m_fontName;
		protected int m_typeSize = 10;
		protected bool m_reloadingList;
		private bool m_suppressingLoadList;
		protected bool m_requestedLoadWhileSuppressed;
		protected bool m_deletingObject;
		protected int m_oldLength;
		protected bool m_fUpdatingList;
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
		/// This enables/disables SendPropChangedOnListChange(). The default is enabled (true).
		/// </summary>
		protected bool m_fEnableSendPropChanged = true;
		/// <summary>
		/// This enables/disables filtering independent of assigning a filter. The default is enabled (true).
		/// </summary>
		protected bool m_fEnableFilters = true;

		/// <summary>
		/// This becomes the SilDataAccess for any views which want to see the filtered, sorted record list.
		/// </summary>
		private ObjectListPublisher m_objectListPublisher;

		/// <summary>
		/// Set of objects that we own and have to dispose
		/// </summary>
		private readonly DisposableObjectsSet<IDisposable> m_ObjectsToDispose =
			new DisposableObjectsSet<IDisposable>();

		#endregion Data members

		#region Construction & initialization

		private RecordList(ISilDataAccessManaged decorator, bool usingAnalysisWs)
		{
			if (decorator == null) throw new ArgumentNullException("decorator");

			m_objectListPublisher = new ObjectListPublisher(decorator, RecordListFlid); ;
			m_oldLength = 0;
			m_usingAnalysisWs = usingAnalysisWs;
		}

		/// <summary>
		/// Create bare-bones RecordList for made up owner and a property on it.
		/// </summary>
		internal RecordList(ISilDataAccessManaged decorator)
			: this(decorator, false)
		{
		}

		/// <summary>
		/// Create RecordList for SDA-made up property on the given owner.
		/// </summary>
		internal RecordList(ISilDataAccessManaged decorator, bool usingAnalysisWs, int flid, ICmObject owner, string propertyName)
			: this(decorator, usingAnalysisWs)
		{
			if (owner == null) throw new ArgumentNullException("owner");
			if (string.IsNullOrWhiteSpace(propertyName)) throw new ArgumentNullException("propertyName");

			m_owningObject = owner;
			m_propertyName = propertyName;
			m_flid = flid;
		}

		/// <summary>
		/// Create RecordList for ordinary (or virtual) property.
		/// </summary>
		internal RecordList(ISilDataAccessManaged decorator, bool usingAnalysisWs, int flid)
			: this(decorator, usingAnalysisWs)
		{
			m_propertyName = string.Empty;
			m_fontName = MiscUtils.StandardSansSerif;
			// Only other current option is to specify an ordinary property (or a virtual one).
			m_flid = flid;
			// Review JohnH(JohnT): This is only useful for dependent clerks, but I don't know how to check this is one.
			m_owningObject = null;
		}

		#endregion Construction & initialization

		/// <summary>
		/// Get the font size from the Stylesheet
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="propertyTable"></param>
		/// <param name="analysisWs">pass in 'true' for the DefaultAnalysisWritingSystem
		/// pass in 'false' for the DefaultVernacularWritingSystem</param>
		/// <returns>return Font size from stylesheet</returns>
		protected static int GetFontHeightFromStylesheet(FdoCache cache, IPropertyTable propertyTable, bool analysisWs)
		{
			int fontHeight;
			var stylesheet = FontHeightAdjuster.StyleSheetFromPropertyTable(propertyTable);
			var wsContainer = cache.ServiceLocator.WritingSystems;
			if (analysisWs)
			{
				fontHeight = FontHeightAdjuster.GetFontHeightForStyle(
				"Normal", stylesheet,
				wsContainer.DefaultAnalysisWritingSystem.Handle,
				cache.WritingSystemFactory) / 1000; //fontHeight is probably pixels
			}
			else
			{
				fontHeight = FontHeightAdjuster.GetFontHeightForStyle(
				"Normal", stylesheet,
				wsContainer.DefaultVernacularWritingSystem.Handle,
				cache.WritingSystemFactory) / 1000; //fontHeight is probably pixels
			}
			return fontHeight;
		}

		#region Properties

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
			get
			{
				return m_objectListPublisher;
			}
		}

		internal protected virtual string PropertyTableId(string sorterOrFilter)
		{
			// Dependent lists do not have owner/property set. Rather they have class/field.
			var className = VirtualListPublisher.MetaDataCache.GetOwnClsName(m_flid);
			var fieldName = VirtualListPublisher.MetaDataCache.GetFieldName(m_flid);
			if (string.IsNullOrEmpty(PropertyName) || PropertyName == fieldName)
			{
				return string.Format("{0}.{1}_{2}", className, fieldName, sorterOrFilter);
			}

			return string.Format("{0}.{1}_{2}", className, PropertyName, sorterOrFilter);
		}

		public string PropertyName
		{
			get
			{
				CheckDisposed();

				return m_propertyName;
			}
		}

		/// <summary>
		/// Gets or sets a sorter.
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
				m_sorter = value;
				m_sorter.Cache = m_cache;
			}
		}

		public virtual RecordFilter Filter
		{
			get
			{
				CheckDisposed();
				// we only have a reference to the filter which means that it might have been
				// disposed. In that case treat it as if we wouldn't have a filter.
				var disposable = m_filter as IFWDisposable;
				if (disposable != null && disposable.IsDisposed)
					m_filter = null;
				return m_filter;
			}
			set
			{
				CheckDisposed();

				if (m_filter == value)
					return;
				m_filter = value;
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
				return m_reloadingList || m_deletingObject;
			}
		}

		public bool IsEmpty
		{
			get
			{
				CheckDisposed();

				return SortedObjects.Count==0;
			}
		}

		/// <summary>
		/// the made-up flid that we address the sequence of hvos in the cache.
		/// </summary>
		public int VirtualFlid
		{
			get
			{
				CheckDisposed();
				return RecordListFlid;
			}
		}

		public string FontName
		{
			get
			{
				CheckDisposed();
				return m_fontName;
			}
		}

		public int TypeSize
		{
			get
			{
				CheckDisposed();
				return m_typeSize;
			}
		}

		/// <summary>
		/// the object which owns the elements which make up this list
		/// </summary>
		public ICmObject OwningObject
		{
			get
			{
				CheckDisposed();
				return m_owningObject;
			}
			set
			{
				CheckDisposed();
				if (m_owningObject == value)
					return; // no need to reload.

				m_owningObject = value;
				m_oldLength = 0;
				ReloadList();
			}
		}

		/// <summary>
		/// This is now a list of ManyOnePathSortItems! (JohnT, 3 June 05)
		/// </summary>
		public ArrayList SortedObjects
		{
			get
			{
				CheckDisposed();
				if (m_sortedObjects == null)
					m_sortedObjects = new ArrayList();
				return m_sortedObjects;
			}
			set
			{
				CheckDisposed();
				m_sortedObjects = value;
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
				CheckDisposed();

				int hvo = CurrentObjectHvo;
				if (hvo <= 0)
					return null;

				try
				{
					return m_cache.ServiceLocator.GetObject(hvo);
				}
				catch (KeyNotFoundException)//CmObject throws this when the object has been deleted todo:OR OTHERWISE BAD
				{
					CurrentIndex = -1;
					return null;
				}
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

		/// <summary>
		/// Our owning record clerk.
		/// </summary>
		internal protected RecordClerk Clerk
		{
			get { return m_clerk; }
			set { m_clerk = value; }
		}

		/// <summary>
		/// Gets or sets a flag indicating whether the list was already loaded in order and
		/// does not need to be sorted further.
		/// </summary>
		/// <value>true if the list was loaded in order and doesn't need further sorting.</value>
		virtual protected bool ListAlreadySorted
		{
			get
			{
				CheckDisposed();
				return false;
			}
		}

		/// <summary>
		///
		/// </summary>
		public int Flid
		{
			get
			{
				CheckDisposed();
				return m_flid;
			}
		}

		/// <summary>
		/// will return -1 if the vector is empty.
		/// </summary>
		public virtual int CurrentIndex
		{
			get
			{
				CheckDisposed();

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
//				Debug.Assert(m_sortedObjects.Count > 0 || m_currentIndex==-1, "index should be negative one since vector is empty");
//				Debug.Assert(m_sortedObjects.Count == 0 || m_currentIndex>-1, "index should not be negative since vector is not empty");
				return m_currentIndex;
			}
			set
			{
				CheckDisposed();

				if (value < -1 || value >= (m_sortedObjects == null ? 0 : m_sortedObjects.Count))
					throw new IndexOutOfRangeException();

				// We don't want multiple log entries for the same record, and it seems this frequently
				// gets called repeatedly for the same one. We check both the index and the object,
				// because it's possible with deletions or insertions that the current object changes
				// even though the index does not.
				int hvoCurrent;
				if (value < 0)
					hvoCurrent = 0;
				else
					hvoCurrent = SortItemAt(value).RootObjectHvo;
				if (m_currentIndex == value && m_hvoCurrent == hvoCurrent)
					return;



				m_currentIndex = value;
				m_hvoCurrent = hvoCurrent;

				ICmObject newFocusedObject = null;

				if (m_currentIndex < 0)
					Logger.WriteEvent("No current record");
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
						Logger.WriteEvent("No Current Record");
				}

				if (newFocusedObject == RecordedFocusedObject)
					return;
				var repo = m_cache.ServiceLocator.ObjectRepository;
				repo.RemoveFocusedObject(RecordedFocusedObject);
				RecordedFocusedObject = newFocusedObject;
				repo.AddFocusedObject(RecordedFocusedObject);
			}
		}

		/// <summary>
		/// This keeps track of the object if any that we have noted in the CmObjectRepository as focused.
		/// My intention is to reserve it for this purpose ONLY.
		/// </summary>
		internal ICmObject RecordedFocusedObject;

		internal bool IsCurrentObjectValid()
		{
			return m_cache.ServiceLocator.IsValidObjectId(CurrentObjectHvo);
		}

		/// <summary>
		/// true if the list is non empty and we are on the last record.
		/// </summary>
		public bool OnLast
		{
			get
			{
				CheckDisposed();
				return (SortedObjects.Count > 0) && (m_currentIndex == (SortedObjects.Count - 1));
			}
		}

		/// <summary>
		/// true if the list is non empty on we are on the first record
		/// </summary>
		public bool OnFirst
		{
			get
			{
				CheckDisposed();
				return (SortedObjects.Count > 0) && m_currentIndex == 0;
			}
		}

		/// <summary>
		/// True if we are in the middle of deleting an object.
		/// </summary>
		public bool DeletingObject
		{
			get
			{
				CheckDisposed();
				return m_deletingObject;
			}
		}

		/// <summary>
		/// False if SendPropChangedOnListChange() should be a no-op.
		/// </summary>
		protected internal bool EnableSendPropChanged
		{
			get
			{
				CheckDisposed();
				return m_fEnableSendPropChanged;
			}
			set
			{
				CheckDisposed();
				m_fEnableSendPropChanged = value;
			}
		}
		#endregion Properties

		#region FwDisposableBase

		protected override void DisposeManagedResources()
		{
			if (m_cache != null && RecordedFocusedObject != null)
				m_cache.ServiceLocator.ObjectRepository.RemoveFocusedObject(RecordedFocusedObject);
			RecordedFocusedObject = null;
			// make sure we uninstall any remaining event handler,
			// irrespective of whether we're active or not.
			if (m_windowPendingOnActivated != null && !m_windowPendingOnActivated.IsDisposed)
				UninstallWindowActivated();
			if (m_sda != null)
				m_sda.RemoveNotification(this);
			if (m_insertableClasses != null)
				m_insertableClasses.Clear();
			if (m_sortedObjects != null)
				m_sortedObjects.Clear();
			m_ObjectsToDispose.Dispose();
		}

		protected override void DisposeUnmanagedResources()
		{
			m_sda = null;
			m_cache = null;
			m_sorter = null;
			m_filter = null;
			m_owningObject = null;
			m_propertyName = null;
			m_fontName = null;
			m_insertableClasses = null;
			m_sortedObjects = null;
			m_owningObject = null;
			PropertyTable = null;
			Publisher = null;
			Subscriber = null;
		}

		#endregion FwDisposableBase

		#region IVwNotifyChange implementation

		public virtual void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			CheckDisposed();

			if (m_fUpdatingList || m_reloadingList)
				return;	// we're already in the process of changing our list.

			// If this list contains WfiWordforms, and the tag indicates a change to the
			// virtual property of all wordforms, then assume this list also is affected
			// and pretend that this PropChanged is really for us.  See FWR-3617 for
			// justification of this assumption.
			if (m_owningObject != null && hvo == m_owningObject.Hvo && tag != m_flid && cvIns > 0)
			{
				if (ListItemsClass == WfiWordformTags.kClassId &&
					hvo == m_cache.LangProject.Hvo &&
					tag == m_cache.ServiceLocator.GetInstance<Virtuals>().LangProjectAllWordforms)
				{
					tag = m_flid;
				}
			}
			if (TryHandleUpdateOrMarkPendingReload(hvo, tag, ivMin, cvIns, cvDel))
				return;

			// Try to catch things that don't obviously affect us, but will cause problems.
			TryReloadForInvalidPathObjectsOnCurrentObject(tag, cvDel);

			var noteChange = VirtualListPublisher as IVwNotifyChange;
			if (noteChange != null)
				noteChange.PropChanged(hvo, tag, ivMin, cvIns, cvDel);
		}

		private void TryReloadForInvalidPathObjectsOnCurrentObject(int tag, int cvDel)
		{
			// see if the property is the VirtualFlid of the owning clerk. If so,
			// the owning clerk has reloaded, so we should also reload.
			RecordClerk clerkProvidingRootObject;
			if (Clerk.TryClerkProvidingRootObject(out clerkProvidingRootObject) &&
				clerkProvidingRootObject.VirtualFlid == tag &&
				cvDel > 0)
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

		protected virtual bool TryHandleUpdateOrMarkPendingReload(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			if (tag == m_flid)
			{
				if (m_owningObject != null && m_owningObject.Hvo != hvo)
				{
					return true;		// This PropChanged doesn't really apply to us.
				}
				ReloadList(ivMin, cvIns, cvDel);
				return true;
			}
			if (tag == SegmentTags.kflidAnalyses && m_objectListPublisher.OwningFieldName == "Wordforms")
			{
				// Changing this potentially changes the list of wordforms that occur in the interesting texts.
				// Hopefully we don't rebuild the list every time; usually this can only be changed in another view.
				// In case we DO have a concordance active in one window while editing another, if this isn't the
				// active window postpone until it is.
				Form window = PropertyTable.GetValue<Form>("window");
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
			// 1) see if the property is the VirtualFlid of the owning clerk. If so,
			// the owning clerk has reloaded, so we should also reload.
			RecordClerk clerkProvidingRootObject = null;
			if (Clerk.TryClerkProvidingRootObject(out clerkProvidingRootObject) &&
				clerkProvidingRootObject.VirtualFlid == tag &&
				cvDel > 0)
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
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="ivMin"></param>
		/// <param name="cvIns"></param>
		/// <param name="cvDel"></param>
		/// <returns></returns>
		protected virtual bool TryModifyingExistingList(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			return false;
		}

		protected bool EntriesDependsUponProp(int tag)
		{
			// Disabled this as part of fix for LT-12092. This means we don't resort the list when
			// the user edits a field that might affect the order.
			//switch (tag / 1000)
			//{
			//    case LexEntryTags.kClassId:
			//    case LexSenseTags.kClassId:
			//    case MoFormTags.kClassId:
			//        return true;
			//}
			return false;
		}

		internal bool ReloadLexEntries
		{
			get { return m_fReloadLexEntries; }
		}

		internal protected virtual bool NeedToReloadList()
		{
			bool fReload = RequestedLoadWhileSuppressed;
			if (Flid == m_cache.ServiceLocator.GetInstance<Virtuals>().LexDbEntries)
				fReload |= ReloadLexEntries;
			return fReload;
		}

		private bool IsPropOwning(int tag)
		{
			IFwMetaDataCache mdc = VirtualListPublisher.MetaDataCache;
			var fieldType = (CellarPropertyType)mdc.GetFieldType(tag);
			if(fieldType == CellarPropertyType.OwningAtomic ||
				fieldType == CellarPropertyType.OwningCollection ||
				fieldType == CellarPropertyType.OwningSequence)
			{
				return true;
			}
			// In addition to normal owning properties, we want to treat the master virtual for all
			// lex entries as owning. I haven't actually made it owning, because we don't currently
			// have any owning virtual properties, and I'm nervous about the consequences of a
			// property which claims to be owning where the target objects don't record the source
			// one as their owner. But RecordList does need to do the checks it does for deleted
			// objects when when a LexEntry goes away.
			if (fieldType == CellarPropertyType.ReferenceSequence)
			{
				var entriesTag = mdc.GetFieldId2(LexDbTags.kClassId, "Entries", false);
				if (tag == entriesTag)
					return true;
			}
			return false;
		}

		#endregion IVwNotifyChange implementation

		#region ISortItemProvider implementation

		/// <summary>
		/// Get the nth item in the main list.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public IManyOnePathSortItem SortItemAt(int index)
		{
			CheckDisposed();

			if (SortedObjects.Count == 0)
				return null;
			else
			return SortedObjects[index] as IManyOnePathSortItem;
		}

		/// <summary>
		/// get the class of the items in this list.
		/// </summary>
		public virtual int ListItemsClass
		{
			get
			{
				CheckDisposed();
				return VirtualListPublisher.MetaDataCache.GetDstClsId(m_flid);
			}
		}

		/// <summary>
		/// verifies that the two classes match, if not, throws message.
		/// </summary>
		/// <param name="beExpectedListItemsClass"></param>
		/// <param name="clerkExpectedListItemsClass"></param>
		internal static void CheckExpectedListItemsClassInSync(int beExpectedListItemsClass, int clerkExpectedListItemsClass)
		{
			if (beExpectedListItemsClass != 0 && clerkExpectedListItemsClass != 0 &&
				beExpectedListItemsClass != clerkExpectedListItemsClass)
			{
				throw new ApplicationException(
					String.Format("for some reason BulkEditBar.ExpectedListItemsClassId({0}) does not match SortItemProvider.ListItemsClass({1}).",
					beExpectedListItemsClass,
					clerkExpectedListItemsClass));
			}
		}

		/// <summary>
		/// An item is being added to your master list. Add the corresponding sort items to
		/// your fake list.
		/// </summary>
		/// <param name="hvo"></param>
		/// <returns>the number of items added.</returns>
		public int AppendItemsFor(int hvo)
		{
			CheckDisposed();

			int start = SortedObjects.Count;
			int result = MakeItemsFor(SortedObjects, hvo);
			if (result != 0)
			{
				int[] newItems = new int[result];
				for (int i = 0; i < result; i++)
					newItems[i] = (SortedObjects[i + start] as IManyOnePathSortItem).RootObjectHvo;
				(VirtualListPublisher as ObjectListPublisher).Replace(m_owningObject.Hvo, start, newItems, 0);
			}
			return result;
		}


		/// <summary>
		/// Remove the corresponding list items. And issue propchanged.
		/// </summary>
		/// <param name="hvoToRemove"></param>
		public void RemoveItemsFor(int hvoToRemove)
		{
			ReplaceListItem(null, hvoToRemove, false);
			SendPropChangedOnListChange(CurrentIndex, SortedObjects, ListChangedEventArgs.ListChangedActions.Normal);
		}


		#endregion ISortItemProvider implementation

		/// <summary>
		/// Transfers ownership of obj to RecordList. RecordList is now responsible for
		/// calling Dispose on the object.
		/// </summary>
		public void TransferOwnership(IDisposable obj)
		{
			if (obj == null)
				return;
			m_ObjectsToDispose.Add(obj);
		}

		/// <summary>
		/// Return the 'root' object (the one from the original list, which goes in the fake flid)
		/// at the specified index.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public ICmObject RootObjectAt(int index)
		{
			CheckDisposed();

			return SortItemAt(index).RootObjectUsing(m_cache);
		}

		internal int RootHvoAt(int index)
		{
			return SortItemAt(index).RootObjectHvo;
		}

		private int[] IndicesOfSortItems(List<int> hvoTargets)
		{
			return IndicesOfSortItems(hvoTargets, false);
		}

		/// <summary>
		///
		/// </summary>
		/// <returns>indices of invalid sort items in reverse order, so that the caller can
		/// remove those invalid indices from SortedObjects without affecting the validity of the next invalid index.</returns>
		protected int[] IndicesOfInvalidSortItems()
		{
			List<int> indices = new List<int>();
			var objectSet = new HashSet<int>(GetObjectSet());
			for (int i = 0; i < SortedObjects.Count; i++)
			{
				int rootHvo = RootHvoAt(i);
				if (!m_cache.ServiceLocator.IsValidObjectId(rootHvo) || !objectSet.Contains(rootHvo))
				{
					indices.Insert(0, i);
				}
			}
			return indices.ToArray();
		}
		private int[] IndicesOfSortItems(List<int> hvoTargets, bool fStopAtFirstFound)
		{
			List<int> indices = new List<int>();
			if (hvoTargets != null && hvoTargets.Count > 0)
			{
				for (int i = 0; i < SortedObjects.Count; i++)
				{
					int hvoItem = SortItemAt(i).RootObjectHvo;
					if (hvoTargets.Contains(hvoItem))
					{
						indices.Add(i);
						if (fStopAtFirstFound)
							break;
					}
				}
			}
			return indices.ToArray();
		}

		protected int IndexOfFirstSortItem(List<int> hvoTargets)
		{
			int [] indices = IndicesOfSortItems(hvoTargets, true);
			if (indices.Length > 0)
				return indices[0];
			else
				return -1;
		}

		public void ChangeOwningObjectId(int hvo)
		{
			CheckDisposed();

			OwningObject = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo);
		}

		public virtual void InitLoad(bool loadList)
		{
			CheckDisposed();

			m_sda = VirtualListPublisher; // needed before ComputeInsertableClasses().
			m_sda.AddNotification(this);

			ComputeInsertableClasses();
			CurrentIndex = -1;
			m_hvoCurrent = 0;

			if (loadList)
				ReloadList();
			else
			{
				ListLoadingSuppressed = true;
				RequestedLoadWhileSuppressed = true;
			}
		}

		/// <summary>
		/// Change the sorter...and resort if the list already exists.
		/// </summary>
		/// <param name="sorter"></param>
		public virtual void ChangeSorter(RecordSorter sorter)
		{
			CheckDisposed();

			Sorter = sorter;

			// JohnT: a different sorter may create a quite different set of IManyOnePathSortItems.
			// Optimize: it may be possible to find some cases in which we don't need to reload fully,
			// for example, when reversing the order on the same column.
			if (m_sortedObjects != null)
				ReloadList();
		}

		#region navigation

		/// <summary>
		/// Return the index (in m_sortedObjects) of the first displayed object.
		/// (In hierarchical lists, this is not necessarily the first item.)
		/// If the list is empty return -1.
		/// </summary>
		public virtual int FirstItemIndex
		{
			get
			{
				CheckDisposed();

				if (m_sortedObjects == null || m_sortedObjects.Count == 0)
					return -1;
				return 0;
			}
		}

		/// <summary>
		/// Return the index (in m_sortedObjects) of the last displayed object.
		/// (In hierarchical lists, this is not necessarily the last item.)
		/// If the list is empty return -1.
		/// </summary>
		public virtual int LastItemIndex
		{
			get
			{
				CheckDisposed();

				if (m_sortedObjects == null || m_sortedObjects.Count == 0)
					return -1;
				return m_sortedObjects.Count - 1;
			}
		}

		/// <summary>
		/// Return the index (in m_sortedObjects) of the object that follows the one
		/// at m_currentIndex.
		/// (In hierarchical lists, this is not necessarily the item at index + 1.)
		/// If the list is empty return -1.
		/// If the current object is the last return m_currentIndex.
		/// If m_currentIndex is -1 return -1.
		/// </summary>
		public virtual int NextItemIndex
		{
			get
			{
				CheckDisposed();

				if (m_sortedObjects == null || m_sortedObjects.Count == 0 || m_currentIndex == -1)
					return -1;
				return Math.Min(m_currentIndex + 1, m_sortedObjects.Count - 1);
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
		public virtual int PrevItemIndex
		{
			get
			{
				CheckDisposed();

				if (m_sortedObjects == null || m_sortedObjects.Count == 0 || m_currentIndex == -1)
					return -1;
				return Math.Max(m_currentIndex - 1, 0);
			}
		}
		#endregion

		/// <summary>
		/// This version of ReloadList assumes that there is a correct current list except that
		/// property m_flid of object m_owningObject has been modified by inserting cvIns objects
		/// and/or deleting cvDel objects at ivMin. May call the regular ReloadList, or
		/// optimize for special cases.
		/// </summary>
		/// <param name="ivMin"></param>
		/// <param name="cvIns"></param>
		/// <param name="cvDel"></param>
		internal virtual void ReloadList(int ivMin, int cvIns, int cvDel)
		{
			CheckDisposed();

			if (RequestedLoadWhileSuppressed)
			{
				// if a previous reload was requested, but suppressed, try to reload the entire list now
				ReloadList();
			}
			// If m_currentIndex is negative the list is empty so we may as well load it fully.
			// This saves worrying about various special cases in the code below.
			else if (cvIns == 1 && (cvDel == 1 || cvDel == 0) &&
				m_owningObject != null && m_hvoCurrent != 0 && m_currentIndex >= 0)
			{
				int cList = VirtualListPublisher.get_VecSize(m_owningObject.Hvo, m_flid);
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
					int hvoReplaced = VirtualListPublisher.get_VecItem(m_owningObject.Hvo, m_flid, ivMin);
					ReplaceListItem(hvoReplaced);
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

		private void ClearOutInvalidItems()
		{
			for (int i = 0; i < SortedObjects.Count; ) // not foreach: we may modify the list
			{
				if (IsInvalidItem((ManyOnePathSortItem)SortedObjects[i]))
					SortedObjects.RemoveAt(i);
				else
					i++;
			}
		}

		private bool IsInvalidItem(ManyOnePathSortItem item)
		{
			if (!m_cache.ServiceLocator.IsValidObjectId(item.KeyObject))
				return true;
			for (int i = 0; i < item.PathLength; i++)
			{
				if (!m_cache.ServiceLocator.IsValidObjectId(item.PathObject(i)))
					return true;
			}
			return false;
		}

		protected internal virtual void ReloadList(int newListItemsClass, int newTargetFlid, bool force)
		{
			// let a bulk-edit record list handle this.
		}

		/// <summary>
		/// this should refresh the display of field values for existing items, rather than altering the
		/// size of the list (e.g. updating list item names)
		/// </summary>
		protected void UpdateListItemName(int hvo)
		{
			int cList = VirtualListPublisher.get_VecSize(m_owningObject.Hvo, m_flid);
			if (cList != 0)
			{
				ListChanged(this, new ListChangedEventArgs(this, ListChangedEventArgs.ListChangedActions.UpdateListItemName, hvo));
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
		/// This will remove the given hvosToRemove (if they exist in our sort items) and any items that refer to invalid objects.
		/// Reload the view if there were any changes, and adjust the CurrentIndex
		/// </summary>
		protected internal void RemoveUnwantedSortItems(List<int> hvosToRemove)
		{
			if (m_sortedObjects == null)
				return;	// nothing to remove.
			bool fUpdatingListOrig = m_fUpdatingList;
			m_fUpdatingList = true;
			try
			{
				int currentIndex = CurrentIndex;
				int cOrigSortObjects = m_sortedObjects.Count;
				// Note: We start with a Set, since it can't have duplicates.
				// First remove the given hvos from our sort items.
				Set<int> unwantedIndices = new Set<int>(IndicesOfSortItems(hvosToRemove));
				// then remove any remaining items that point to invalid objects.
				unwantedIndices.AddRange(IndicesOfInvalidSortItems());
				// Put the now unique indices into a list,
				// so we can make sure they are processed in reverse order.
				List<int> sortedIndices = new List<int>(unwantedIndices.ToArray());
				sortedIndices.Sort();
				sortedIndices.Reverse();
				foreach (int indexOfSortItem in sortedIndices)
				{
					if (indexOfSortItem >= 0)
					{
						m_sortedObjects.RemoveAt(indexOfSortItem);
						if (indexOfSortItem < currentIndex || SortedObjects.Count <= currentIndex)
							currentIndex--;
					}
				}
				if (m_sortedObjects.Count == 0)
					currentIndex = -1;
				else if (currentIndex >= m_sortedObjects.Count)
					currentIndex = m_sortedObjects.Count - 1;
				CurrentIndex = currentIndex;
				if (m_sortedObjects.Count != cOrigSortObjects)
				{
					SendPropChangedOnListChange(CurrentIndex,
						SortedObjects, ListChangedEventArgs.ListChangedActions.Normal);
				}
			}
			finally
			{
				m_fUpdatingList = fUpdatingListOrig;
			}
		}

		/// <summary>
		/// replace any matching items in our sort list. and do normal navigation prop change.
		/// </summary>
		/// <param name="hvoReplaced"></param>
		internal protected void ReplaceListItem(int hvoReplaced)
		{
			ReplaceListItem(hvoReplaced, ListChangedEventArgs.ListChangedActions.Normal);
		}

		/// <summary>
		/// replace any matching items in our sort list.
		/// </summary>
		/// <param name="hvoReplaced"></param>
		/// <param name="listChangeAction"></param>
		internal void ReplaceListItem(int hvoReplaced, ListChangedEventArgs.ListChangedActions listChangeAction)
		{
			bool fUpdatingListOrig = m_fUpdatingList;
			m_fUpdatingList = true;
			try
			{
				int hvoOldCurrentObj = CurrentObjectHvo != 0 ? CurrentObjectHvo : 0;
				ArrayList newSortItems = new ArrayList();
				var objReplaced = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoReplaced);
				newSortItems.AddRange(ReplaceListItem(objReplaced, hvoReplaced, true));
				if (newSortItems.Count > 0)
				{
					// in general, when adding new items, we want to try to maintain the previous selected *object*,
					// which may have changed index.  so try to find its new index location.
					int indexOfCurrentObj = CurrentIndex;
					if (hvoOldCurrentObj != 0 && CurrentObjectHvo != hvoOldCurrentObj)
					{
						int indexOfOldCurrentObj = IndexOf(hvoOldCurrentObj);
						if (indexOfOldCurrentObj >= 0)
							indexOfCurrentObj = indexOfOldCurrentObj;
					}
					SendPropChangedOnListChange(indexOfCurrentObj, SortedObjects, listChangeAction);
				}
			}
			finally
			{
				m_fUpdatingList = fUpdatingListOrig;
			}
		}

		// stub to make empty action.
		void DoNothing(int ignoreMe)
		{
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
			ArrayList newSortItems = new ArrayList();
			List<int> indicesOfSortItemsToRemove = new List<int>(IndicesOfSortItems(new List<int>(new int[] { hvoToReplace })));
			ArrayList remainingInsertItems = new ArrayList();
			int hvoNewObject = 0;
			if (newObj != null)
			{
				hvoNewObject = newObj.Hvo;
				// we don't want to add new sort items, if we've already added them, but we do want to allow
				// a replacement.
				if (hvoToReplace == hvoNewObject || IndexOfFirstSortItem(new List<int>(new int[] { hvoNewObject })) < 0)
					MakeItemsFor(newSortItems, newObj.Hvo);
				remainingInsertItems = (ArrayList)newSortItems.Clone();
				if (fAssumeSame)
				{
					//assume we're converting a dummy item to a real one.
					//In that case, the real item should have same basic content as the dummy item we are replacing,
					//so we can replace the item at the same sortItem indices.
					foreach (object itemToInsert in newSortItems)
					{
						if (indicesOfSortItemsToRemove.Count > 0)
						{
							int iToReplace = indicesOfSortItemsToRemove[0];
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
			foreach (int iToRemove in indicesOfSortItemsToRemove)
			{
				SortedObjects.RemoveAt(iToRemove);
			}
			// add the remaining items.
			if (m_sorter != null)
			{
				m_sorter.DataAccess = m_objectListPublisher; // for safety, ensure this any time we use it.
				m_sorter.SetPercentDone = DoNothing; // no progress for inserting one item
				m_sorter.MergeInto(SortedObjects, remainingInsertItems);
			}
			else
			{
				var items = GetObjectSet().ToList();
				// if we're inserting only one item, try to guess the best position.
				// we can try to assume that SortedObjects is in the same order as items if there is no filter.
				// so go through SortedObjects and insert the new sortedItem at the same place as in items.
				if (remainingInsertItems.Count == 1 && remainingInsertItems[0] is IManyOnePathSortItem  &&
					m_filter == null &&
					SortedObjects.Count == (items.Count - 1))
				{
					var newSortedObject = remainingInsertItems[0] as IManyOnePathSortItem;
					for (int i = 0; i < SortedObjects.Count; i++)
					{
						if (items[i] == SortItemAt(i).RootObjectHvo)
							continue; // already in sorted objects.
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
				m_hvoCurrent = hvoNewObject;
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
			ArrayList newSortedObjects = new ArrayList(hvos.Length);
			foreach (int hvo in hvos)
			{
				MakeItemsFor(newSortedObjects, hvo);
			}
			return newSortedObjects;
		}

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
				CheckDisposed();
				return m_fUpdatingList;
			}
			set
			{
				CheckDisposed();

				m_fUpdatingList = value;
				ListLoadingSuppressed = value;
				if (ListLoadingSuppressed)
					m_requestedLoadWhileSuppressed = true;
			}
		}

		/// <summary>
		/// Indicates whether RecordList is being modified
		/// </summary>
		internal bool UpdatingList
		{
			get { return m_fUpdatingList;  }
			set { m_fUpdatingList = value; }
		}

		protected virtual bool UpdatePrivateList()
		{
			return false;
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
		internal bool ListLoadingSuppressed
		{
			get { return m_suppressingLoadList; }
			set
			{
				if (m_suppressingLoadList == value)
					return;
				SetSuppressingLoadList(value);
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

		/// <summary>
		/// sets the value of m_suppressingLoadList without any side effects (e.g. ReloadList()).
		/// </summary>
		/// <param name="value"></param>
		internal void SetSuppressingLoadList(bool value)
		{
			m_suppressingLoadList = value;
			if (!m_suppressingLoadList)
				UninstallWindowActivated();
		}

		/// <summary>
		/// Indicates whether we tried to reload the list while we were suppressing the reload.
		/// </summary>
		internal protected virtual bool RequestedLoadWhileSuppressed
		{
			get { return m_requestedLoadWhileSuppressed; }
			set { m_requestedLoadWhileSuppressed = value; }
		}


		/// <summary>
		/// Answer true if the current object is valid. Currently we just check for deleted objects.
		/// We could try to get an ICmObject and call IsValidObject, but currently that doesn't do
		/// any more than this, and it fails (spuriously) when working with fake objects.
		/// </summary>
		internal bool CurrentObjectIsValid
		{
			get
			{
				int hvo = CurrentObjectHvo;
				if (hvo == 0 || hvo == (int)SpecialHVOValues.kHvoObjectDeleted)
					return false;
				return true;
			}
		}

		/// <summary>
		/// Useed on occasions like changing views, this should suppress any optimization that prevents real reloads.
		/// </summary>
		public virtual void ForceReloadList()
		{
			ReloadList();  // By default nothing special is needed.
		}
		/// <summary>
		/// Sort and filter the underlying property to create the current list of objects.
		/// </summary>
		public virtual void ReloadList()
		{
			CheckDisposed();

			// Skip multiple reloads and reloading when our clerk is not active.
			if (m_reloadingList)
			{
				return;
			}
			if (m_suppressingLoadList || !Clerk.IsActiveInGui || Clerk.SuspendLoadListUntilOnChangeFilter)
			{
				// if we need to reload the list
				// clear the views property until we are no longer suppressed, so dependent views don't try to access objects
				// that have possibly been deleted.
				if (m_owningObject != null && SortedObjects.Count > 0 &&
					((Clerk.UpdateHelper != null && Clerk.UpdateHelper.ClearBrowseListUntilReload) || !Clerk.IsActiveInGui))
				{
					m_indexToRestoreDuringReload = CurrentIndex;	// try to restore this index during reload.
					// clear everything for now, including the current index, but don't issue a RecordNavigation.
					SendPropChangedOnListChange(-1, new ArrayList(),
						ListChangedEventArgs.ListChangedActions.SkipRecordNavigation);
				}
				m_requestedLoadWhileSuppressed = true;
				// it's possible that we'll want to reload once we become the main active window (cf. LT-9251)
				if (PropertyTable != null)
				{
					var window = PropertyTable.GetValue<Form>("window");
					var app = PropertyTable.GetValue<IApp>("App");
					if (window != null && app != null && window != app.ActiveMainWindow)
					{
						// make sure we don't install more than one.
						RequestReloadOnActivation(window);
					}
				}
				return;
			}
			try
			{
				m_requestedLoadWhileSuppressed = false;
				if (Clerk.UpdateHelper != null && Clerk.UpdateHelper.ClearBrowseListUntilReload)
				{
					if (m_indexToRestoreDuringReload != -1)
					{
						// restoring m_currentIndex directly isn't effective until SortedObjects
						// is greater than 0.
						// so, try to force to restore the current index to what we persist.
						CurrentIndex = -1;
						PropertyTable.SetProperty(Clerk.PersistedIndexProperty, m_indexToRestoreDuringReload,
							SettingsGroup.LocalSettings, true, true);
						m_indexToRestoreDuringReload = -1;
					}
					Clerk.UpdateHelper.ClearBrowseListUntilReload = false;
				}
				m_reloadingList = true;
				if (UpdatePrivateList())
					return; // Cannot complete the reload until PropChangeds complete.
				var newCurrentIndex = CurrentIndex;
				ArrayList newSortedObjects;
				ListChangedEventArgs.ListChangedActions actions;

				// Get the HVO of the current object (but only if it hasn't been deleted).
				// If it has, don't modify m_currentIndex, as the old position is less likely to
				// move the list than going to the top.
				// (We want to keep the current OBJECT, not index, if it's still a real object,
				// because the change that produced the regenerate might be a change of filter or sort
				// that moves it a lot. But if the change is an object deletion, we want to not change
				// the position if we can help it.)
				int hvoCurrent = 0;
				if (m_sortedObjects != null && m_currentIndex != -1 && m_sortedObjects.Count > m_currentIndex
					&& CurrentObjectIsValid)
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
					if (ListChanged != null)
						ListChanged(this, new ListChangedEventArgs(this, ListChangedEventArgs.ListChangedActions.Normal, 0));
					return;
				}

				try
				{
					newSortedObjects = GetFilteredSortedList();
				}
				catch (FDOInvalidFieldException)
				{
					newSortedObjects = HandleInvalidFilterSortField();
				}
				catch(ConfigurationException ce)
				{
					if (ce.InnerException is FDOInvalidFieldException)
						newSortedObjects = HandleInvalidFilterSortField();
					else
						throw;
				}

				// Try to stay on the same object if possible.
				if (hvoCurrent != 0 && newSortedObjects.Count > 0)
				{
					newCurrentIndex = GetNewCurrentIndex(newSortedObjects, hvoCurrent);
					if (newCurrentIndex < 0 && newSortedObjects.Count > 0)
					{
						newCurrentIndex = 0; // expected but not found: move to top, but only if there are items in the list.
						actions = ListChangedEventArgs.ListChangedActions.Normal; // This is a full-blown record change
					}
					else
						// The index changed, so we need to broadcast RecordNavigate, but since we didn't actually change objects,
						// we shouldn't do a save
						actions = ListChangedEventArgs.ListChangedActions.SuppressSaveOnChangeRecord;
				}
				else
				{
					// We didn't even expect to find it, probably it's been deleted or sorted list has become empty.
					// Keep the current position as far as possible.
					if (newCurrentIndex >= newSortedObjects.Count)
						newCurrentIndex = newSortedObjects.Count - 1;
					else
					{
						newCurrentIndex = GetPersistedCurrentIndex(newSortedObjects.Count);
					}

					actions = ListChangedEventArgs.ListChangedActions.Normal; // We definitely changed records
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

		private ArrayList HandleInvalidFilterSortField()
		{
			m_filter = null;
			m_sorter = null;
			MessageBox.Show(Form.ActiveForm, xWorksStrings.ksInvalidFieldInFilterOrSorter, xWorksStrings.ksErrorCaption,
				MessageBoxButtons.OK, MessageBoxIcon.Warning);
			var newSortedObjects = GetFilteredSortedList();
			return newSortedObjects;
		}

		private ArrayList GetFilteredSortedList()
		{
			ArrayList newSortedObjects;
			newSortedObjects = new ArrayList();
			if (m_filter != null)
				m_filter.Preload(OwningObject);
				// Preload the sorter (if any) only if we do NOT have a filter.
				// If we also have a filter, it's pretty well certain currently that it already did
				// the preloading. If we ever have a filter and sorter that want to preload different
				// data, we'll need to refactor so we can determine this, because we don't want to do it twice!
			else if (m_sorter != null)
				m_sorter.Preload(OwningObject);


			var panel = PropertyTable.GetValue<StatusBarProgressPanel>("ProgressBar");
			using (var progress = (panel == null) ? new NullProgressState() : new ProgressState(panel))
			{
				progress.SetMilestone(xWorksStrings.ksSorting);
				// Allocate an arbitrary 20% for making the items.
				var objectSet = GetObjectSet();
				var count = objectSet.Count();
				var done = 0;
				foreach (var obj in objectSet)
				{
					done++;
					var newPercentDone = done*20/count;
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
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void window_Activated(object sender, EventArgs e)
		{
			UninstallWindowActivated();
			if (NeedToReloadList())
				ReloadList();
		}

		private void UninstallWindowActivated()
		{
			if (m_windowPendingOnActivated != null)
				m_windowPendingOnActivated.Activated -= new EventHandler(window_Activated);
			m_windowPendingOnActivated = null;
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
			if (m_sorter != null && !ListAlreadySorted)
			{
				m_sorter.DataAccess = m_objectListPublisher;
				if (m_sorter is IReportsSortProgress)
				{
					// Uses the last 80% of the bar (first part used for building the list).
					((IReportsSortProgress) m_sorter).SetPercentDone =
						percent =>
							{
								progress.PercentDone = 20 + percent * 4 / 5;
								progress.Breath();
							};
				}
				m_sorter.Sort( /*ref*/ newSortedObjects); //YiSpeed 1 secs
				m_sorter.SetPercentDone = DoNothing; // progress about to be disposed.
			}
		}

		protected int MakeItemsFor(ArrayList sortedObjects, int hvo)
		{
			int start = sortedObjects.Count;
			if (m_sorter == null)
				sortedObjects.Add(new ManyOnePathSortItem(hvo, null, null));
			else
			{
				m_sorter.DataAccess = m_objectListPublisher;
				m_sorter.CollectItems(hvo, sortedObjects);
			}
			if (m_filter != null && m_fEnableFilters)
			{
				m_filter.DataAccess = m_objectListPublisher;
				for (int i = start; i < sortedObjects.Count; )
				{
					if (m_filter.Accept(sortedObjects[i] as IManyOnePathSortItem))
						i++; // advance loop if we don't delete!
					else
					{
						sortedObjects.RemoveAt(i);
					}
				}
			}

			return sortedObjects.Count - start;
		}

		protected void SendPropChangedOnListChange(int newCurrentIndex, ArrayList newSortedObjects, ListChangedEventArgs.ListChangedActions actions)
		{
			//Populate the virtual cache property which will hold this set of hvos, in this order.
			var hvos = new int[newSortedObjects.Count];
			var i = 0;
			foreach(IManyOnePathSortItem item in newSortedObjects)
				hvos[i++] = item.RootObjectHvo;
			// In case we're already displaying it, we must have an accurate old length, or if it gets shorter
			// the extra ones won't get deleted. But we must check whether the property is already cached...
			// if it isn't and we try to read it, the code will try to load this fake property from the database,
			// with unfortunate results. (It's not exactly a reference sequence, but any sequence type will do here.)


			int rootHvo = m_owningObject.Hvo;

			if (AboutToReload != null)
				AboutToReload(this, new EventArgs());
			// Must not actually change anything before we do AboutToReload! Then do it all at once
			// before we make any notifications...we want the cache value to always be consistent
			// with m_sortedObjects, and m_currentIndex always in range
			SortedObjects = newSortedObjects;
			// if we haven't already set an index, see if we can restore one from the property table.
			if (SortedObjects.Count > 0 && (newCurrentIndex == -1 || m_hvoCurrent == 0))
				newCurrentIndex = PropertyTable.GetValue(Clerk.PersistedIndexProperty, SettingsGroup.LocalSettings, 0);
			// Ensure the index is in bounds.  See LT-10349.
			if (SortedObjects.Count > 0)
			{
				// The stored value may be past the end of the current collection,
				// so set it to 0 in that case.
				// It sure beats the alternative of a drop dead crash. :-)
				if (newCurrentIndex > SortedObjects.Count - 1 || newCurrentIndex < 0)
					newCurrentIndex = 0; // out of bounds, so set it to 0.
			}
			else
			{
				newCurrentIndex = -1;
			}
			CurrentIndex = newCurrentIndex;
			(VirtualListPublisher as ObjectListPublisher).CacheVecProp(m_owningObject.Hvo, hvos);

			m_oldLength = hvos.Length;

			//TODO: try to stay on the same record
			// Currently Attempts to keep us at the same index as we were.
			// we should try hard to keep us on the actual record that we were currently on,
			// since the reload may be a result of changing the sort order, in which case the index is meaningless.

			//make sure the hvo index is in a reasonable spot
			if (m_currentIndex >= hvos.Length)
				CurrentIndex = hvos.Length - 1;
			if (m_currentIndex < 0)
				CurrentIndex = (hvos.Length>0)? 0: -1;
			if (DoneReload != null)
				DoneReload(this, new EventArgs());

			// Notify any delegates that the selection of the main object in the vector has changed.
			if (ListChanged != null && m_fEnableSendPropChanged)
				ListChanged(this, new ListChangedEventArgs(this, actions, 0));
		}

		/// <summary>
		/// Handle adding and/or removing a filter.
		/// </summary>
		/// <param name="args"></param>
		public virtual void OnChangeFilter(FilterChangeEventArgs args)
		{
			CheckDisposed();

			if (m_filter == null)
			{
				// Had no filter to begin with
				Debug.Assert(args.Removed == null);
				m_filter = args.Added is NullFilter? null : args.Added;
			}
			else if (m_filter.SameFilter(args.Removed))
			{
				// Simplest case: we had just one filter, the one being removed.
				// Change filter to whatever (if anything) replaces it.
				m_filter = args.Added is NullFilter? null : args.Added;
			}
			else if (m_filter is AndFilter)
			{
				AndFilter af = m_filter as AndFilter;
				if (args.Removed != null)
				{
					af.Remove(args.Removed);
				}
				if (args.Added != null)
				{
					//When the user chooses "all records/no filter", the RecordClerk will remove
					//its previous filter and add a NullFilter. In that case, we don't really need to add
					//	that filter. Instead, we can just add nothing.
					if (!(args.Added is NullFilter))
						af.Add(args.Added);
				}
				// Remove AndFilter if we get down to one.
				// This is not just an optimization, it allows the last filter to be removed
				// leaving empty, so the status bar can show that there is then no filter.
				if (af.Filters.Count == 1)
					m_filter = af.Filters[0] as RecordFilter;
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
		}

		/// <summary>
		/// Creates a new AndFilter and registers it for later disposal.
		/// </summary>
		public AndFilter CreateNewAndFilter(params RecordFilter[] filters)
		{
			Debug.Assert(filters.Length > 1, "Need at least two filters to construct an AndFilter");
			AndFilter af = new AndFilter();
			foreach (var filter in filters)
				af.Add(filter);
			return af;
		}

		private IProgress m_progAdvInd = null;
		/// <summary>
		/// Get/set a progress reporter (encapsulated in an IAdvInd4 interface).
		/// </summary>
		public IProgress ProgressReporter
		{
			get
			{
				CheckDisposed();
				return m_progAdvInd;
			}
			set
			{
				CheckDisposed();
				m_progAdvInd = value;
			}
		}

		internal bool IsVirtualPublisherCreated
		{
			get { return m_objectListPublisher != null; }
		}

		protected virtual IEnumerable<int> GetObjectSet()
		{
			return DomainObjectServices.GetObjectSet(VirtualListPublisher,
																m_cache.ServiceLocator.GetInstance<ICmObjectRepository>(),
																m_owningObject, m_flid);
		}

		public virtual bool CanInsertClass(string className)
		{
			CheckDisposed();

			return (GetMatchingClass(className)!= null);
		}

		private ICmObject CreateNewObject(int hvoOwner, IList<ClassAndPropInfo> cpiPath)
		{
			if (cpiPath.Count > 2)
			{
				throw new ArgumentException("We currently only support up to 2 levels for creating a new object.");
			}
			else if (cpiPath.Count == 2)
			{
				if (cpiPath[1].isVector)
					throw new ArgumentException("We expect the second level to be an atomic property.");
			}
			if (!cpiPath[0].isVector)
			{
				throw new ArgumentException("We expect the first level to be a vector property.");
			}
			ISilDataAccess sda = VirtualListPublisher;
			ClassAndPropInfo cpiFinal = cpiPath[cpiPath.Count - 1];

			// assume we need to insert a new object in the vector field following hvoOwner
			ClassAndPropInfo cpi = cpiPath[0];
			int flid = cpi.flid;
			int insertPosition = 0;
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
						int oldIndex = sda.GetObjIndex(hvoOwner, flid, CurrentObjectHvo);
						insertPosition = oldIndex + 1;
					}
					break;
				default:
					// Just possible it's some kind of virtual we can't insert a new object into.
					return null;
			}

			int hvoNew = sda.MakeNewObject(cpi.signatureClsid, hvoOwner, flid, insertPosition);

			// we may need to insert another new class.
			if (cpiPath.Count > 1)
			{
				// assume this is an atomic property.
				ClassAndPropInfo cpiLevel2 = cpiPath[1];
				hvoOwner = hvoNew;
				flid = cpiLevel2.flid;
				insertPosition = 0;
				hvoNew = sda.MakeNewObject(cpiLevel2.signatureClsid, hvoOwner, flid, -2);
			}
			if (hvoNew != 0)
				return m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoNew);
			return null;
		}

		/// <summary>
		/// Create an object of the specified class.
		/// </summary>
		/// <param name="className"></param>
		/// <returns>true if successful (the class is known)</returns>
		public virtual bool CreateAndInsert(string className)
		{
			CheckDisposed();

			ClassAndPropInfo cpi = GetMatchingClass(className);
			Debug.Assert(cpi != null, "This object should not have been asked to insert an object of the class "+ className +".");
			if (cpi != null)
			{
				List<ClassAndPropInfo> cpiPath;
#if NEEDED // If we need to be able to use this code path to insert into virtual properties, we will need to make them
				// Support writing, or do something similar to the old code which figured out a path of places to insert
				// the real object. As far as I (JohnT) can tell, though, we don't currently have any virtual properties
				// at the top level of a clerk into which we try to insert newly created objects in this way.
				// check to see if we're wanting to insert into an owning relationship via a virtual property.
				BaseFDOPropertyVirtualHandler vh = Cache.VwCacheDaAccessor.GetVirtualHandlerId(m_flid) as BaseFDOPropertyVirtualHandler;
				if (vh != null)
				{
					cpi.hvoOwner = m_owningObject.Hvo;
					cpiPath = vh.GetRealOwningPath();
					if (cpiPath.Count == 0)
						return false;
				}
				else
				{
					cpiPath = new List<ClassAndPropInfo>(new ClassAndPropInfo[] { cpi });
				}
#else
				cpiPath = new List<ClassAndPropInfo>(new ClassAndPropInfo[] { cpi });
#endif
				var createAndInsertMethodObj = new CpiPathBasedCreateAndInsert(m_owningObject.Hvo, cpiPath, this);
				var newObj = DoCreateAndInsert(createAndInsertMethodObj);
				int hvoNew = newObj != null ? newObj.Hvo : 0;
				return hvoNew != 0; // If we get zero, we couldn't do it for some reason.
			}
			return false;
		}

		/// <summary>
		/// Create and Insert an item in a list. If this is a hierarchical list, then insert it at the same level
		/// as the current object.
		/// SuppressSaveOnChangeRecord will be true, to allow the user to Undo this action.
		/// NOTE: the caller may want to call Clerk.SaveOnChangeRecord() before calling this.
		/// </summary>
		/// <typeparam name="TObj"></typeparam>
		/// <param name="createAndInsertMethodObj"></param>
		/// <returns></returns>
		public TObj DoCreateAndInsert<TObj>(ICreateAndInsert<TObj> createAndInsertMethodObj)
			where TObj : ICmObject
		{
			TObj newObj;
			int hvoNew = 0;
			var options = new RecordClerk.ListUpdateHelper.ListUpdateHelperOptions
{SuspendPropChangedDuringModification = true};
			using (new RecordClerk.ListUpdateHelper(Clerk, options))
			{
				newObj = createAndInsertMethodObj.Create();
				hvoNew = newObj != null ? newObj.Hvo : 0;
				if (hvoNew != 0)
					ReplaceListItem(hvoNew, ListChangedEventArgs.ListChangedActions.SuppressSaveOnChangeRecord);
			}
			CurrentIndex = IndexOf(hvoNew);
			return newObj;
		}

		/// <summary>
		/// get the index of the given hvo, where it occurs as the root object
		/// (of a IManyOnePathSortItem) in m_sortedObjects.
		/// </summary>
		/// <param name="hvo"></param>
		/// <returns>-1 if the object is not in the list</returns>
		public int IndexOf(int hvo)
		{
			CheckDisposed();

			// Creating a list item and then jumping to the tool can leave the list stale, putting us
			// in a bogus state where the edit pane never gets painted.  (See LT-7580.)
			if (RequestedLoadWhileSuppressed)
				ReloadList();
			return IndexOf(SortedObjects, hvo);
		}

		// get the index of the given hvo, where it occurs as a root object in
		// one of the IManyOnePathSortItems in the given list.
		protected int IndexOf(ArrayList objects, int hvo)
		{
			int i = 0;
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

		/// <summary>
		/// get the index of an item in the list that has a root object that is owned by hvo
		/// </summary>
		/// <param name="hvoTarget"></param>
		/// <returns>-1 if the object is not in the list</returns>
		public int IndexOfChildOf(int hvoTarget)
		{
			CheckDisposed();

			// If the list is made up of fake objects, we can't find one of them owned by our target,
			// and trying to will crash, so give up.
			if (SortedObjects.Count == 0 ||
				!m_cache.ServiceLocator.ObjectRepository.IsValidObjectId(((IManyOnePathSortItem) SortedObjects[0]).RootObjectHvo))
			{
				return -1;
			}

			int i = 0;
			foreach (IManyOnePathSortItem item in SortedObjects)
			{
				var rootObject = item.RootObjectUsing(m_cache);
				if (rootObject == null)
					continue;  // may be something that has been deleted?
				for (var owner = rootObject.Owner; owner != null; owner = owner.Owner)
				{
					if (owner.Hvo == hvoTarget)
						return i;
				}
				++i;
			}
			return -1;
		}

		/// <summary>
		/// get the index of an item in the list that has a root object that (directly or indirectly) owns hvo
		/// </summary>
		/// <param name="hvoTarget"></param>
		/// <returns>-1 if the object is not in the list</returns>
		public int IndexOfParentOf(int hvoTarget)
		{
			CheckDisposed();

			// If the list is made up of fake objects, we can't find one of them that our target owns,
			// and trying to will crash, so give up.
			if (SortedObjects.Count == 0 ||
				!m_cache.ServiceLocator.ObjectRepository.IsValidObjectId(((IManyOnePathSortItem)SortedObjects[0]).RootObjectHvo))
			{
				return -1;
			}

			var target = m_cache.ServiceLocator.ObjectRepository.GetObject(hvoTarget);
			var owners = new Set<int>();
			for(var owner = target.Owner; owner != null; owner = owner.Owner)
				owners.Add(owner.Hvo);

			int i = 0;
			foreach (IManyOnePathSortItem item in SortedObjects)
			{
				if (owners.Contains(item.RootObjectHvo))
				   return i;
				++i;
			}
			return -1;
		}

#if RANDYTODO
		// TODO: Delete in the end, but keep for now, so areas/tools know what to give us.
		// TODO: We now get the owning object, flid, and whether the WS is vern or Anal in a constructor.
		/// <summary>
		/// Return the Flid for the model property that this vector represents.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="owner"></param>
		/// <param name="owningObject"></param>
		/// <param name="fontName"></param>
		/// <param name="typeSize"></param>
		/// <returns></returns>
		internal int GetFlidOfVectorFromName(string name, string owner, out ICmObject owningObject, ref string fontName, ref int typeSize)
		{
			var defAnalWsFontName = m_cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.DefaultFontName;
			owningObject = null;
			int realFlid = 0;
			switch (name)
			{
				default:
					// REVIEW (TimS): This code was added as a way to include information easier in
					// XCore.  Is this a good idea or should we just add another case statement each
					// time a new name is needed?
					try
					{
						// ENHANCE (TimS): The same method of getting the flid could be done to
						// get the owner.
						switch (owner)
						{
							case "LangProject":
								owningObject = m_cache.LanguageProject;
								break;
							case "LexDb":
								owningObject = m_cache.LanguageProject.LexDbOA;
								break;
							case "RnResearchNbk":
								owningObject = m_cache.LanguageProject.ResearchNotebookOA;
								break;
							case "DsDiscourseData":
								owningObject = m_cache.LanguageProject.DiscourseDataOA;
								break;
							default:
								Debug.Assert(false, "Illegal owner specified for possibility list.");
								break;
						}
						realFlid = VirtualListPublisher.MetaDataCache.GetFieldId(owningObject.ClassName, name, true);
					}
					catch (Exception e)
					{
						throw new ConfigurationException("The field '" + name + "' with owner '" +
							owner + "' has not been implemented in the switch statement " +
							"in RecordList.GetVectorFromName().", e);
					}
					break;

				// Other supported stuff
				case "Entries":
					if (owner == "ReversalIndex")
					{
#if RANDYTODO
		// TODO: The client has to deal with creating a new index, since it has to feed in the owning object in a constructor.
						if (m_cache.LangProject.LexDbOA.ReversalIndexesOC.Count == 0)
						{
							// invoke the Reversal listener to create the index for this invalid state
							Publisher.Publish("InsertReversalIndex_FORCE", null);
						}
#endif

						if (m_cache.LanguageProject.LexDbOA.ReversalIndexesOC.Count > 0)
						{
							owningObject = m_cache.LanguageProject.LexDbOA.ReversalIndexesOC.ToArray()[0];
							realFlid = ReversalIndexTags.kflidEntries;
						}
					}
#if RANDYTODO
					// TODO: DONE: Handled in EntriesOrChildClassesRecordList subclass.
					else
					{
						owningObject = m_cache.LanguageProject.LexDbOA;
						realFlid = m_cache.ServiceLocator.GetInstance<Virtuals>().LexDbEntries;
					}
#endif
					break;
				case "Scripture_0_0_Content": // StText that is contents of first section of first book.
					try
					{
						var sb = m_cache.LanguageProject.TranslatedScriptureOA.ScriptureBooksOS[0];
						var ss = sb.SectionsOS[0];
						owningObject = ss.ContentOA;
					}
					catch (NullReferenceException)
					{
						Trace.Fail("Could not get the test Scripture object.  Your language project might not have one (or their could have been some other error).  Try TestLangProj.");
						throw;
					}
					realFlid = StTextTags.kflidParagraphs;
					break;
				case "Texts":
					owningObject = m_cache.LanguageProject;
					realFlid = m_cache.ServiceLocator.GetInstance<Virtuals>().LangProjTexts;
					break;
				case "MsFeatureSystem":
					owningObject = m_cache.LanguageProject;
					realFlid = LangProjectTags.kflidMsFeatureSystem;
					break;

				// phonology
				case "Phonemes":
					if (m_cache.LanguageProject.PhonologicalDataOA.PhonemeSetsOS.Count == 0)
					{
						// Pathological...this helps the memory-only backend mainly, but makes others self-repairing.
						NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(m_cache.ActionHandlerAccessor,
						   () =>
						   {
							   m_cache.LanguageProject.PhonologicalDataOA.PhonemeSetsOS.Add(
							   m_cache.ServiceLocator.GetInstance<IPhPhonemeSetFactory>().Create());
						   });
					}
					owningObject = m_cache.LanguageProject.PhonologicalDataOA.PhonemeSetsOS[0];
					realFlid = PhPhonemeSetTags.kflidPhonemes;
					break;
				case "BoundaryMarkers":
					owningObject = m_cache.LanguageProject.PhonologicalDataOA.PhonemeSetsOS[0];
					realFlid = PhPhonemeSetTags.kflidBoundaryMarkers;
					break;
				case "Environments":
					owningObject = m_cache.LanguageProject.PhonologicalDataOA;
					realFlid = PhPhonDataTags.kflidEnvironments;
					break;
				case "NaturalClasses":
					owningObject = m_cache.LanguageProject.PhonologicalDataOA;
					realFlid = PhPhonDataTags.kflidNaturalClasses;
					fontName = defAnalWsFontName;
					typeSize = GetFontHeightFromStylesheet(m_cache, PropertyTable, true);
					break;

				case "PhonologicalFeatures":
					owningObject = m_cache.LangProject.PhFeatureSystemOA;
					realFlid = FsFeatureSystemTags.kflidFeatures;
					fontName = defAnalWsFontName;
					typeSize = GetFontHeightFromStylesheet(m_cache, PropertyTable, true);
					break;

				case "PhonologicalRules":
					owningObject = m_cache.LangProject.PhonologicalDataOA;
					realFlid = PhPhonDataTags.kflidPhonRules;
					break;

				// morphology
				case "AdhocCoprohibitions":
					owningObject = m_cache.LanguageProject.MorphologicalDataOA;
					realFlid = MoMorphDataTags.kflidAdhocCoProhibitions;
					fontName = defAnalWsFontName;
					typeSize = GetFontHeightFromStylesheet(m_cache, PropertyTable, true);
					break;
				case "CompoundRules":
					owningObject = m_cache.LanguageProject.MorphologicalDataOA;
					realFlid = MoMorphDataTags.kflidCompoundRules;
					fontName = defAnalWsFontName;
					typeSize = GetFontHeightFromStylesheet(m_cache, PropertyTable, true);
					break;

				case "Features":
					owningObject = m_cache.LanguageProject.MsFeatureSystemOA;
					realFlid = FsFeatureSystemTags.kflidFeatures;
					fontName = defAnalWsFontName;
					typeSize = GetFontHeightFromStylesheet(m_cache, PropertyTable, true);
					break;

				case "FeatureTypes":
					owningObject = m_cache.LanguageProject.MsFeatureSystemOA;
					realFlid = FsFeatureSystemTags.kflidTypes;
					fontName = defAnalWsFontName;
					typeSize = GetFontHeightFromStylesheet(m_cache, PropertyTable, true);
					break;

				case "ProdRestrict":
					owningObject = m_cache.LanguageProject.MorphologicalDataOA;
					realFlid = MoMorphDataTags.kflidProdRestrict;
					fontName = defAnalWsFontName;
					typeSize = GetFontHeightFromStylesheet(m_cache, PropertyTable, true);
					break;

				case "Problems":
					owningObject = m_cache.LanguageProject;
					realFlid = LangProjectTags.kflidAnnotations;
					fontName = defAnalWsFontName;
					typeSize = GetFontHeightFromStylesheet(m_cache, PropertyTable, true);
					break;
				case "Wordforms":
					owningObject = m_cache.LanguageProject;
					//realFlid = cache.MetaDataCacheAccessor.GetFieldId("LangProject", "Wordforms", false);
					realFlid = ObjectListPublisher.OwningFlid;
					break;
				case "ReversalIndexes":
					{
						owningObject = m_cache.LanguageProject.LexDbOA;
						realFlid = m_cache.DomainDataByFlid.MetaDataCache.GetFieldId("LexDb", "CurrentReversalIndices", false);
						break;
					}

				//dependent properties
				case "Analyses":
					{
						//TODO: HACK! making it show the first one.
						var wfRepository = m_cache.ServiceLocator.GetInstance<IWfiWordformRepository>();
						if (wfRepository.Count > 0)
							owningObject = wfRepository.AllInstances().First();
						realFlid = WfiWordformTags.kflidAnalyses;
						break;
					}
				case "SemanticDomainList":
					owningObject = m_cache.LanguageProject.SemanticDomainListOA;
					realFlid = CmPossibilityListTags.kflidPossibilities;
					break;
#if RANDYTODO
					// TODO: DONE: Handled in EntriesOrChildClassesRecordList subclass.
				case "AllSenses":
				case "AllEntryRefs":
					{
						owningObject = m_cache.LanguageProject.LexDbOA;
						realFlid = m_cache.DomainDataByFlid.MetaDataCache.GetFieldId("LexDb", name, false);
						// Todo: something about initial sorting...
						break;
					}
#endif
			}

			return realFlid;
		}
#endif

		protected virtual ClassAndPropInfo GetMatchingClass(string className)
		{
			foreach(ClassAndPropInfo cpi in m_insertableClasses)
			{
				if (cpi.signatureClassName == className)
				{
					return cpi;
				}
			}
			return null;
		}

#if RANDYTODO
		/// <summary>
		/// Adjust the name of the menu item if appropriate. PossibilityRecordList overrides.
		/// </summary>
		/// <param name="command"></param>
		/// <param name="display"></param>
		public virtual void AdjustInsertCommandName(Command command, UIItemDisplayProperties display)
		{
			CheckDisposed();

		}
#endif

		protected void ComputeInsertableClasses()
		{
			m_insertableClasses = new List<ClassAndPropInfo>();
			if (OwningObject != null && OwningObject is ICmPossibilityList &&
				(OwningObject as ICmPossibilityList).IsClosed)
			{
				// You can't insert anything in a closed list!
				return;
			}
			if (m_flid != ObjectListPublisher.OwningFlid)
				m_cache.AddClassesForField(m_flid, true, m_insertableClasses);
		}

		/// <summary>
		/// Delete the object without reporting progress.
		/// </summary>
		public void DeleteCurrentObject()
		{
			CheckDisposed();

			DeleteCurrentObject(new NullProgressState(), CurrentObject);
		}

		/// <summary>
		/// Delete the current object, reporting progress as far as possible.
		/// In some cases thingToDelete is not actually the current object, but it should always
		/// be related to it.
		/// </summary>
		/// <param name="state"></param>
		public virtual void DeleteCurrentObject(ProgressState state, ICmObject thingToDelete)
		{
			CheckDisposed();

			try
			{
				// This can happen in some bizarre cases, such as reconciling with another client
				// just before delete, if the other client also deleted the same object.
				if (!IsCurrentObjectValid() || !thingToDelete.IsValidObject)
					return;
				m_deletingObject = true;
				FdoCache cache = m_cache;
				var currentObject = CurrentObject;
				// This looks plausible; but for example IndexOf may reload the list, if a reload is pending;
				// and the current object may no longer match the current filter, so it may be gone.
				//Debug.Assert(currentIndex == IndexOf(currentObject.Hvo));
				string className = currentObject.GetType().Name;
				using (new RecordClerk.ListUpdateHelper(Clerk))
				{
					bool m_fUpdatingListOrig = m_fUpdatingList;
					m_fUpdatingList = true;
					try
					{
						RemoveItemsFor(CurrentObject.Hvo);
						VirtualListPublisher.DeleteObj(thingToDelete.Hvo);
					}
					finally
					{
						m_fUpdatingList = m_fUpdatingListOrig;
					}
				}
			}
			finally
			{
				m_deletingObject = false;
			}
		}

		internal void PersistOn(string pathname)
		{
			// Ensure that all the items in the sorted list are valid ICmObject references before
			// actually persisting anything.  Some lists store dummy objects.
			if (m_sortedObjects == null || m_sortedObjects.Count == 0)
				return;
			var repo = m_cache.ServiceLocator.ObjectRepository;
			foreach (var obj in m_sortedObjects)
			{
				ManyOnePathSortItem item = obj as ManyOnePathSortItem;
				if (item == null)
					return;
				if (item.KeyObject <= 0 || item.RootObjectHvo <= 0 ||
					// The object might have been deleted.  See LT-11169.
					!repo.IsValidObjectId(item.KeyObject) || !repo.IsValidObjectId(item.RootObjectHvo))
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

		private void TryToDelete(string pathname)
		{
			if (File.Exists(pathname))
			{
				try
				{
					File.Delete(pathname);
				}
				catch (IOException)
				{
				}
			}
		}

		/// <summary>
		/// Returns true if it successfully set m_sortedObjects to a restored list.
		/// </summary>
		internal virtual bool RestoreFrom(string pathname)
		{
			// If something has created instances of the class we display, the persisted list may be
			// missing things. For example, if the program starts up in interlinear text view, and the user
			// creates entries as a side effect of glossing texts, we shouldn't use the saved list of
			// lex entries when we switch to the lexicon view.
			if (m_cache.ServiceLocator.ObjectRepository.InstancesCreatedThisSession(ListItemsClass))
				return false;
			ArrayList items;
			using (var stream = new StreamReader(pathname))
			{
				items = ManyOnePathSortItem.ReadItems(stream, m_cache.ServiceLocator.ObjectRepository);
				stream.Close();
			}
			// This particular cache cannot reliably be used again, since items may be created or deleted
			// while the program is running. In case a crash occurs, we don't want to reload an obsolete
			// list the next time we start up.
			FileUtils.Delete(pathname);
			return false; // could not restore, bad file or deleted objects or...
		}

		/// <summary>
		/// LT-12780:  On reloading FLEX there were situtaions where it reloaded on the first element of a list instead of
		/// the last item the user was working on before closing Flex. The CurrentIndex was being set to zero. This method
		/// will access the prersisted index but also make sure it does not use an index which is out of range.
		/// </summary>
		/// <param name="numberOfObjectsInList"></param>
		/// <returns></returns>
		private int GetPersistedCurrentIndex(int numberOfObjectsInList)
		{
			var persistedCurrentIndex = PropertyTable.GetValue(Clerk.PersistedIndexProperty, SettingsGroup.LocalSettings, 0);
			if (persistedCurrentIndex >= numberOfObjectsInList)
				persistedCurrentIndex = numberOfObjectsInList - 1;
			return persistedCurrentIndex;
		}

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

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="propertyTable">Interface to a property table.</param>
		/// <param name="publisher">Interface to the publisher.</param>
		/// <param name="subscriber">Interface to the subscriber.</param>
		public virtual void InitializeFlexComponent(IPropertyTable propertyTable, IPublisher publisher, ISubscriber subscriber)
		{
			FlexComponentCheckingService.CheckInitializationValues(propertyTable, publisher, subscriber, PropertyTable, Publisher, Subscriber);

			PropertyTable = propertyTable;
			Publisher = publisher;
			Subscriber = subscriber;

			m_cache = PropertyTable.GetValue<FdoCache>("cache");
			CurrentIndex = -1;
			m_hvoCurrent = 0;
			m_oldLength = 0;
			var wsContainer = m_cache.ServiceLocator.WritingSystems;
			m_fontName = m_usingAnalysisWs ? wsContainer.DefaultAnalysisWritingSystem.DefaultFontName : wsContainer.DefaultVernacularWritingSystem.DefaultFontName;
			m_typeSize = GetFontHeightFromStylesheet(m_cache, PropertyTable, m_usingAnalysisWs);

			if (m_owningObject != null)
			{
				UpdatePrivateList();
			}
		}

		#endregion

		private class CpiPathBasedCreateAndInsert : ICreateAndInsert<ICmObject>
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
	}
}
