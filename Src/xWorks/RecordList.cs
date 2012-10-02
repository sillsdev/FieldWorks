// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: .cs
// History: John Hatton, created
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Xml;
using System.Runtime.InteropServices;
using System.Reflection;

using XCore;
using SIL.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks;
using SIL.FieldWorks.Filters;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Framework;
using System.Windows.Forms;

namespace SIL.FieldWorks.XWorks
{

	/// <summary>
	/// this kind of the event is fired when he RecordList recognizes that its list has changed for any reason.
	/// </summary>
	public class ListChangedEventArgs : EventArgs
	{
		protected RecordList m_list;
		protected ListChangedActions m_actions;

		/// <summary>
		/// Actions to take on a ListChanged event.
		/// SkipRecordNavigation will skip broadcasting OnRecordNavigation.
		/// SuppressSaveOnChangeRecord will broadcast OnRecordNavigation, but not save the cache (not saving the cache preserves the Undo/Redo stack)
		/// Normal will broadcast OnRecordNavigation and save the cache
		/// UpdateListItemName will simply reload the record tree bar item for the CurrentObject.
		/// </summary>
		public enum ListChangedActions {
			SkipRecordNavigation,
			SuppressSaveOnChangeRecord,
			Normal,
			UpdateListItemName
		};


		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="list"></param>
		/// <param name="actions">Actions to take during the ListChanged event</param>
		public ListChangedEventArgs(RecordList list, ListChangedActions actions)
		{
			m_list = list;
			m_actions = actions;
		}

		public RecordList List
		{
			get
			{
				return m_list;
			}
		}

		/// <summary>
		/// if true, RecordClerk can skip Broadcasting OnRecordNavigation.
		/// </summary>
		public ListChangedActions Actions
		{
			get
			{
				return m_actions;
			}
		}
	}

	/// <summary>
	/// this is simply a definition of the kind of method which can be tied to this kind of event
	/// </summary>
	public delegate void ListChangedEventHandler(object sender, ListChangedEventArgs e);


	public class ConcordanceWordformsRecordList : DummyRecordList
	{
		long m_lastTimeReloaded = 0;
		List<IStText> m_concordanceTexts;
		bool m_fReloadConcordanceTexts = false;
		/// <summary>
		/// used to determine if our wordform recordlist is based upon the latest
		/// wordform inventory state.
		/// </summary>
		object m_wordformInventoryCookie = null;
		int ktagWordformOccurrences = 0;
		int vtagInterlinearTexts = 0; //used in PropChanged to control when a refresh is done. If Texts have changed
									//by the Include Scripture comman the tag passed to PropChanged will be this one.

		public ConcordanceWordformsRecordList() { }

		public override void Init(FdoCache cache, Mediator mediator, XmlNode recordListNode)
		{
			CheckDisposed();

			base.Init(cache, mediator, recordListNode);
			if (m_cache != null)
			{
				ktagWordformOccurrences = WfiWordform.OccurrencesFlid(m_cache);
				vtagInterlinearTexts = LangProject.InterlinearTextsFlid(m_cache);
			}
			if (m_owningObject == null && m_cache != null)
			{
				m_owningObject = m_cache.LangProject.WordformInventoryOA;
			}
		}

		private bool IsUpToDate()
		{
			bool fUpToDate = true;

			foreach (IStText text in m_concordanceTexts)
			{
				if (!text.IsUpToDate() || m_lastTimeReloaded < text.LastParsedTimestamp)
				{
					fUpToDate = false;
					break;
				}
			}
			return fUpToDate;
		}

		public override void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			CheckDisposed();

			if (m_fUpdatingList &&
				(tag == m_flid ||
				tag == (int)WfiWordform.WfiWordformTags.kflidForm ||
				tag == (int)WordformInventory.WordformInventoryTags.kflidWordforms))
			{
				return;	// we're already in the process of changing our list.
			}

			// if we've added or removed an occurrence of a wordform, then update the relevant items in our list.
			int hvoWordform = 0;
			switch (tag)
			{
				default:
					// ktagWordformOccurrences isn't a constant,
					// so it can't be in its own case statement.
					if (tag == ktagWordformOccurrences)
						hvoWordform = hvo;
					break;
				case (int)WfiWordform.WfiWordformTags.kflidForm:
					WordformInventory.OnChangedWordformsOC();
					hvoWordform = hvo;
					break;
				case (int)WordformInventory.WordformInventoryTags.kflidWordforms:
					WordformInventory.OnChangedWordformsOC();
					if (cvDel > 0)
					{
						RemoveUnwantedSortItems(null);
						// Only quit, if none were also added.
						// If they were also added, then it will end up calling the base class impl.
						if (cvIns == 0)
							return;
					}
					break;
				case (int)LangProject.LangProjectTags.kflidTexts:
				case (int)Text.TextTags.kflidContents:
					m_fReloadConcordanceTexts = true;
					break;
				case (int)StText.StTextTags.kflidParagraphs: // Fall through.
				case (int)StTxtPara.StTxtParaTags.kflidContents:
					// we should mark the owning text as being modified.
					// how do we detect that a paragraph has been deleted?
					//m_fReloadConcordanceTexts = true;
					break;
			}

			// update the display of any word that has changed its occurrences
			// Enhance: we could expand this to include changes to other properties in our columns.
			if (hvoWordform != 0)
			{
				List<int> wordformsList;
				// try to avoid reloading our ConcordanceWordforms if it's not already loaded.
				if (VirtualHandler.IsPropInCache(Cache.MainCacheAccessor, m_owningObject.Hvo, 0))
				{
					wordformsList = new List<int>(m_cache.GetVectorProperty(m_owningObject.Hvo, m_flid, true));
					int owningIndex = wordformsList.IndexOf(hvoWordform);
					if (owningIndex >= 0)
					{
						try
						{
							// Don't apply filters while we're editing away.  See LT-7607.
							m_fEnableFilters = false;
							ReplaceListItem(hvoWordform);
						}
						finally
						{
							m_fEnableFilters = true;
						}
						return;
					}
				}
			}

			// in general, we don't want to reload this list due to PropChanges
			// since that could take a long time.
			// We'll just let the list figure out if it needs to reload
			// next time we re-enter the tool or the user does Refresh.
			using (RecordClerk.ListUpdateHelper luh = new RecordClerk.ListUpdateHelper(Clerk))
			{
				base.PropChanged(hvo, tag, ivMin, cvIns, cvDel);
				// if LangProject.InterlinearTexts has changed, we do
				// want to reload the list, otherwise we don't.
				if (tag != vtagInterlinearTexts)
					luh.TriggerPendingReloadOnDispose = false;
			}
		}

		/// <summary>
		/// overridden to figure in ReloadConcordanceTexts
		/// </summary>
		internal protected override bool RequestedLoadWhileSuppressed
		{
			get { return base.RequestedLoadWhileSuppressed || m_fReloadConcordanceTexts; }
		}

		protected override FdoObjectSet<ICmObject> GetObjectSet()
		{
			// if any of our texts have been modified or parsed since we built our concordance, then reload everything.
			// Otherwise we'll assume we're already cached.
			// NOTE: a text will not be up to date after a refresh, since we store the timestamp in the cache.
			List<IStText> prevTexts = m_concordanceTexts;
			FDOSequencePropertyTableVirtualHandler pvh = Cache.VwCacheDaAccessor.GetVirtualHandlerId(LangProject.InterlinearTextsFlid(Cache)) as FDOSequencePropertyTableVirtualHandler;
			if (!pvh.IsUpToDate)
			{
				pvh.ReloadList();
				m_concordanceTexts = null;
			}
			if (m_concordanceTexts == null || m_fReloadConcordanceTexts)
			{
				List<int> concordanceTextIds;
				pvh.Load(pvh.OwningObj.Hvo, 0, out concordanceTextIds);
				m_concordanceTexts = new List<IStText>(concordanceTextIds.Count);
				foreach (int textId in concordanceTextIds)
					m_concordanceTexts.Add(StText.CreateFromDBObject(m_cache, textId));
				m_fReloadConcordanceTexts = false;
			}
			if (NeedToReloadVirtualProperty == false
				&& (prevTexts != null && m_concordanceTexts.Count != prevTexts.Count
				|| !this.IsUpToDate()))
			{
				NeedToReloadVirtualProperty = true;
			}
			bool fRecomputeLoadedTimeStamp = NeedToReloadVirtualProperty;
			FdoObjectSet<ICmObject> words = base.GetObjectSet();
			// record the state of the wordformInventory.
			m_wordformInventoryCookie = (m_cache.LangProject.WordformInventoryOA as WordformInventory).WordformInventoryCookie;
			if (fRecomputeLoadedTimeStamp)
			{
				foreach (IStText text in m_concordanceTexts)
				{
					long textLastParsedTimestamp = text.LastParsedTimestamp;
					if (m_lastTimeReloaded < textLastParsedTimestamp)
						m_lastTimeReloaded = textLastParsedTimestamp;
				}
			}
			return words;
		}

		/// <summary>
		/// overriden to take in consideration the state of the wordform inventory.
		/// </summary>
		public override bool NeedToReloadVirtualProperty
		{
			get
			{
				return base.NeedToReloadVirtualProperty ||
					(m_cache.LangProject.WordformInventoryOA as WordformInventory).IsExpiredWordformInventoryCookie(m_wordformInventoryCookie);
			}
		}
	}

	public abstract class PropertyTableVirtualHandler : BaseVirtualHandler
	{
		public static List<IVwVirtualHandler> InstallVirtuals(XmlNode virtualsNode, FdoCache cache, Mediator mediator)
		{
			List<IVwVirtualHandler> installedVirtualHandlers = BaseVirtualHandler.InstallVirtuals(virtualsNode, cache);
			// pass in mediators where needed.
			foreach (IVwVirtualHandler vh in installedVirtualHandlers)
			{
				if (vh is FDOSequencePropertyTableVirtualHandler)
				{
					if ((vh as FDOSequencePropertyTableVirtualHandler).NeedToReloadSettings)
					{
						(vh as FDOSequencePropertyTableVirtualHandler).LoadSettings(mediator.PropertyTable);
					}
				}
			}
			return installedVirtualHandlers;
		}
	}

	/// <summary>
	/// This virtual handler can load information stored in the property table.
	/// </summary>
	public class FDOSequencePropertyTableVirtualHandler : FDOSequencePropertyVirtualHandler
	{
		protected string m_propertyTableKey = "";
		/// <summary>
		/// accessing through OwningObj loads this variable.
		/// </summary>
		protected ICmObject m_owner = null;
		protected bool m_fListHasPropertyTableIds = false;
		protected List<int> m_ids = new List<int>();

		public FDOSequencePropertyTableVirtualHandler(XmlNode configuration, FdoCache cache)
			: base(configuration, cache)
		{
			m_propertyTableKey = XmlUtils.GetManditoryAttributeValue(configuration, "propertyTableKey");
		}

		/// <summary>
		/// </summary>
		public void LoadSettings(PropertyTable pt)
		{
			if (pt == null || !NeedToReloadSettings)
				return;
			ResetOwningObj();
			string sHvos = GetPropertyTableValue(pt);
			m_ids = PropertyTableList(sHvos);
			m_fNeedToReloadSettings = false;
		}

		/// <summary>
		/// Setup the owning object for this virtual handler.
		/// </summary>
		private ICmObject GetOwningObj()
		{
			string fontName;
			int typeSize;
			ICmObject owningObject;
			RecordList.GetFlidOfVectorFromName(this.FieldName, this.ClassName, false, Cache, null,
				out owningObject, out fontName, out typeSize);
			return owningObject;
		}

		/// <summary>
		///	Indicates whether or not this property needs to be reloaded from the property table.
		/// </summary>
		protected bool m_fNeedToReloadSettings = false;
		public bool NeedToReloadSettings
		{
			get { return m_fNeedToReloadSettings; }
			set
			{
				m_fNeedToReloadSettings = value;
				if (m_fNeedToReloadSettings)
				{
					m_ids = new List<int>();
					m_fListHasPropertyTableIds = false;
				}
			}
		}

		internal ICmObject OwningObj
		{
			get
			{
				// we still need to setup the owning object for this handler, just in case
				// xWindow.RestoreProperties() gets skipped due to a Shift Key during startup (LT-7353).
				if (m_owner == null)
					ResetOwningObj();
				return m_owner;
			}
			set { m_owner = value; }
		}

		private void ResetOwningObj()
		{
			m_owner = GetOwningObj();
		}

		protected override void Load(int hvo, int tag, int ws, IVwCacheDa cda, out List<int> ids)
		{
			ids = GetListForCache();
			if (ids == null)
				ids = new List<int>();
			cda.CacheVecProp(hvo, tag, ids.ToArray(), ids.Count);
			if (PropertyTableList().Count > 0)
				m_fListHasPropertyTableIds = true;
			else
				m_fListHasPropertyTableIds = false;
		}

		/// <summary>
		/// Get the list of ids stored in the property table.
		/// </summary>
		/// <returns></returns>
		protected virtual List<int> GetListForCache()
		{
			return PropertyTableList();
		}

		/// <summary>
		/// Get the value we can use to store in the property table.
		/// </summary>
		/// <returns></returns>
		private string GetPropertyTableValue()
		{
			return CmObject.JoinIds(m_ids.ToArray(), ",");
		}

		/// <summary>
		/// get the value for our virtual handler from the property table.
		/// </summary>
		/// <param name="pt"></param>
		/// <returns></returns>
		private string GetPropertyTableValue(PropertyTable pt)
		{
			if (pt == null)
				return null;
			return pt.GetStringProperty(m_propertyTableKey, null);
		}

		/// <summary>
		/// Store this virtual property's settings to the property table so they can be persisted in local settings.
		/// </summary>
		/// <param name="pt"></param>
		internal void StoreSettings(PropertyTable pt)
		{
			if (pt == null)
				return;
			pt.SetProperty(m_propertyTableKey, GetPropertyTableValue(), true, PropertyTable.SettingsGroup.LocalSettings);
		}

		/// <summary>
		/// Get a list that we can save to the property table (only returning ids that refer to valid objects).
		/// </summary>
		/// <returns></returns>
		protected virtual List<int> PropertyTableList()
		{
			string sHvos = GetPropertyTableValue();
			return PropertyTableList(sHvos);
		}

		protected List<int> PropertyTableList(string sHvos)
		{
			return FdoUi.CmObjectUi.ParseSinglePropertySequenceValueIntoHvos(sHvos, Cache, DestinationClassId);
		}

		/// <summary>
		/// Check to make sure the list in the property table matches the list in the cache.
		/// </summary>
		internal protected virtual bool IsUpToDate
		{
			get
			{
				List<int> propertyTableIds = PropertyTableList();
				// since it's possible that we're sharing a property table list that gets deleted
				// by another user, we use 'm_fListHasPropertyTableIds' to determine whether or not
				// we expect to have propertyTableIds.
				if (propertyTableIds.Count == 0)
					return !m_fListHasPropertyTableIds;
				// since we have property table ids, determine whether or not they are all included
				// in our cached set of ids.
				Set<int> idsInCache = new Set<int>(Cache.GetVectorProperty(OwningObj.Hvo, Tag, true));
				Set<int> intersection = idsInCache.Intersection(propertyTableIds);
				return propertyTableIds.Count == intersection.Count;
			}
		}

		/// <summary>
		/// Reloads the list, sync'ing it with the ids in the property table.
		/// </summary>
		internal void ReloadList()
		{
			UpdateList(PropertyTableList().ToArray());
		}

		/// <summary>
		/// Updates the Cache according to the given list and send PropChanged.
		/// </summary>
		/// <param name="hvosForPropertyTable"></param>
		public virtual void UpdateList(int[] hvosForPropertyTable)
		{
			// save filter
			UpdatePropertyTableList(hvosForPropertyTable);
			int hvoRoot = OwningObj.Hvo;
			int oldCount = 0;
			if (Cache.MainCacheAccessor.get_IsPropInCache(hvoRoot, Tag, Type, 0))
			{
				oldCount = Cache.MainCacheAccessor.get_VecSize(hvoRoot, Tag);
			}
			// update the cache
			Load(hvoRoot, Tag, 0, m_cache.VwCacheDaAccessor);
			int newCount = 0;
			if (Cache.MainCacheAccessor.get_IsPropInCache(hvoRoot, Tag, Type, 0))
			{
				newCount = Cache.MainCacheAccessor.get_VecSize(hvoRoot, Tag);
			}
			Cache.PropChanged(null, PropChangeType.kpctNotifyAll, hvoRoot, Tag, 0, newCount, oldCount);
		}

		protected virtual void UpdatePropertyTableList(int[] hvosForPropertyTable)
		{
			m_ids = new List<int>(hvosForPropertyTable);
		}
	}

	public class DummyRecordList : RecordList, IDummy, IxCoreColleague
	{
		const int kMaxDummiesToConvertAtOnce = 1;
		bool m_fConvertRemainingDummiesOnIdle = false;
		int m_flidOwningItemToConvertToReal = 0;
		List<int> m_remainingDummiesToConvertOnIdle = new List<int>();
		List<int> m_requestedDummiesToConvertQueue = new List<int>();
		Dictionary<int, ICmObject> m_dummyToRealDict = new Dictionary<int, ICmObject>();
		Dictionary<int, RequestConversionToRealEventArgs> m_dummiesToConvertRequestsMap = new Dictionary<int, RequestConversionToRealEventArgs>();
		List<BaseFDOPropertyVirtualHandler> m_compatibleHandlerDependencies = new List<BaseFDOPropertyVirtualHandler>();

		public DummyRecordList(){}

		public override void Init(FdoCache cache, Mediator mediator, XmlNode recordListNode)
		{
			CheckDisposed();

			base.Init(cache, mediator, recordListNode);
			m_fConvertRemainingDummiesOnIdle = XmlUtils.GetOptionalBooleanAttributeValue(recordListNode, "convertRemainingDummiesOnIdle", false);
			// only hook up the conversion handler if we have one of these properties, and implements IDummyRequestConversion.
			IVwVirtualHandler handler = (m_cache.MainCacheAccessor as IVwCacheDa).GetVirtualHandlerId(m_flid);
			if (handler is IDummyRequestConversion)
				(handler as IDummyRequestConversion).RequestConversionToReal += new RequestConversionToRealEventHandler(DummyRecordList_RequestConversionToReal);
			// now look through dependencies upon virtual properties, and register to be involved in their dummy conversion process.
			List<int> flids;
			if (handler is BaseFDOPropertyVirtualHandler && Cache.TryGetDependencies(m_flid, out flids))
			{
				foreach (int flidTest in flids)
				{
					BaseFDOPropertyVirtualHandler bvh = null;
					if (Cache.TryMatchCompatibleHandler((handler as BaseFDOPropertyVirtualHandler).DestinationClassId, flidTest, out bvh))
					{
						m_compatibleHandlerDependencies.Add(bvh);
					}
				}
				foreach (IDummyRequestConversion bvh in m_compatibleHandlerDependencies)
				{
					// since both lists are keeping track of the same class of items,
					// we want to see if we can be involved in the dummy conversion process of its items.
					(bvh as IDummyRequestConversion).RequestConversionToReal +=
						new RequestConversionToRealEventHandler(DummyRecordList_RequestConversionToReal);
				}

			}
		}

		public override void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			CheckDisposed();
			if (m_fUpdatingList && tag == m_flidOwningItemToConvertToReal)
				return;	// we're already in the process of changing our list.
			using (RecordClerk.ListUpdateHelper luh = new RecordClerk.ListUpdateHelper(Clerk))
			{
				base.PropChanged(hvo, tag, ivMin, cvIns, cvDel);
			}
		}

		/// <summary>
		/// if we're in a record dummy list, and tag belongs to a compatible list, and cvIns == 1 == cvDel
		/// we will assume that we're in the process of converting an item to a real object, and not simply
		/// replacing it.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="ivMin"></param>
		/// <param name="cvIns"></param>
		/// <param name="cvDel"></param>
		/// <returns></returns>
		protected override bool TryModifyingExistingList(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			return this.TryMarkPendingConversionRequest(hvo, tag, ivMin, cvIns, cvDel);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				IVwVirtualHandler handler = (m_cache.MainCacheAccessor as IVwCacheDa).GetVirtualHandlerId(m_flid);
				if (handler is IDummyRequestConversion)
					(handler as IDummyRequestConversion).RequestConversionToReal -= new RequestConversionToRealEventHandler(DummyRecordList_RequestConversionToReal);
				if (m_compatibleHandlerDependencies != null)
				{
					// now unregister our handler method with the compatible virtual handlers that we depend upon.
					foreach (IDummyRequestConversion vh in m_compatibleHandlerDependencies)
					{
						vh.RequestConversionToReal -= new RequestConversionToRealEventHandler(DummyRecordList_RequestConversionToReal);
					}
				}
			}
			m_dummiesToConvertRequestsMap = null;
			m_remainingDummiesToConvertOnIdle = null;
			m_requestedDummiesToConvertQueue = null;
			m_dummyToRealDict = null;
			m_compatibleHandlerDependencies = null;
			base.Dispose(disposing);
		}

		private void BuildDummiesToConvertList(List<int> queue, List<int> dummiesToConvert)
		{
			ICmObject realObj = null;
			// Add only up to the max allowed.
			foreach (int item in queue.ToArray())
			{
				if (dummiesToConvert.Count == kMaxDummiesToConvertAtOnce)
					break;
				// only add unique and things that haven't already been converted.
				if (!dummiesToConvert.Contains(item))
				{
					if (m_dummyToRealDict.TryGetValue(item, out realObj) && realObj != null)
					{
						// we've already converted this, so remove it from our queue
						queue.Remove(item);
						continue;
					}
					dummiesToConvert.Add(item);
				}
			}
		}

		/// <summary>
		/// Process any pending objects that need to be converted to real objects.
		/// Review: Is there some other event we could use to do this, before OnIdle()?
		/// </summary>
		/// <param name="argument"></param>
		/// <returns></returns>
		public bool OnIdle(object argument)
		{
			CheckDisposed();

			if (m_requestedDummiesToConvertQueue.Count == 0 &&
				(m_fConvertRemainingDummiesOnIdle == false ||
				 m_fConvertRemainingDummiesOnIdle && m_remainingDummiesToConvertOnIdle.Count == 0))
			{
				return false;
			}
			List<int> dummiesToConvert = new List<int>(kMaxDummiesToConvertAtOnce);
			// Process those that are already in the queue, followed by any remaining ones.
			BuildDummiesToConvertList(m_requestedDummiesToConvertQueue, dummiesToConvert);
			if (m_fConvertRemainingDummiesOnIdle)
				BuildDummiesToConvertList(m_remainingDummiesToConvertOnIdle, dummiesToConvert);
			Debug.Assert(m_requestedDummiesToConvertQueue.Count == 0 || m_requestedDummiesToConvertQueue[0] == dummiesToConvert[0],
				"We should empty our request queue first.");
			using (SuppressSubTasks supressActionHandler = new SuppressSubTasks(Cache))
			{
				ConvertDummiesToReal(m_flid, dummiesToConvert.ToArray());
			}
			return false;
		}

		/// <summary>
		/// takes the parameters from a PropChange and tries to determine whether we should expect
		/// to receive a subsequent DummyRecordList_RequestConversionToReal.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="ivMin"></param>
		/// <param name="cvIns"></param>
		/// <param name="cvDel"></param>
		/// <returns>true, if we should wait to do any reloading until the conversion request is satisfied.</returns>
		bool m_fPendingConversionRequest = false;
		internal protected bool TryMarkPendingConversionRequest(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			if (cvIns != 1 || cvDel != 1)	// we could also check that ivMin is a real object.
				return false;
			IVwVirtualHandler vh;
			if (tag == m_flid)
			{
				m_fPendingConversionRequest = true;
				return true;
			}
			else if (Cache.TryGetVirtualHandler(tag, out vh) && vh is IDummyRequestConversion)
			{
				// go through our compatible handlers and see if we match any.
				foreach (BaseFDOPropertyVirtualHandler vhDummyToReal in m_compatibleHandlerDependencies)
				{
					if (vhDummyToReal.Tag == tag)
					{
						m_fPendingConversionRequest = true;
						return true;
					}
				}
			}
			return false;
		}

		/// <summary>
		/// unless the PendingConversionRequest is taken, we still need to reload everything.
		/// </summary>
		public override bool NeedToReloadVirtualProperty
		{
			get
			{
				return base.NeedToReloadVirtualProperty || m_fPendingConversionRequest;
			}
		}

		void DummyRecordList_RequestConversionToReal(object sender, RequestConversionToRealEventArgs e)
		{
			// Make sure we aren't in the process of making a selection, since that selection will become invalid
			// if we convert its root object.

			// take any pending request
			if (m_fPendingConversionRequest)
				m_fPendingConversionRequest = false;

			// To generate the parser data for this object, we need to convert it to a real object.
			// The ParserListener should detect that a real property that it knows the Parser cares about
			// has been detected, and will schedule it to be parsed through the ParserConnection.
			bool fRequiresRealParserGeneratedData = false;
			if (e.Configuration != null)
			{
				fRequiresRealParserGeneratedData =
					XmlUtils.GetOptionalBooleanAttributeValue(e.Configuration, "requiresRealParserGeneratedData", false);
			}
			// first see if the conversion has already been done. if so, just update our lists.
			if (e.RealObject != null)
			{
				// we just want to replace the dummy in our list
				ArrayList newSortItems = new ArrayList();
				ReplaceDummyItemWithReal(e.OwningFlid, e.DummyHvo, e.RealObject, ref newSortItems);
				if (newSortItems.Count > 0)
					SendPropChangedOnListChange(CurrentIndex, SortedObjects, ListChangedEventArgs.ListChangedActions.SkipRecordNavigation);
			}
			else if (e.ConvertNow)
			{
				// see if we've already converted this value. if so, just return the result.
				ICmObject realObj = null;
				if (!m_dummyToRealDict.TryGetValue(e.DummyHvo, out realObj))
				{
					m_dummyToRealDict.Add(e.DummyHvo, null);
				}
				if (realObj == null)
				{
					// insert at the top of the list, to give this one priority.
					m_requestedDummiesToConvertQueue.Insert(0, e.DummyHvo);
					// we'll take responsibility for the conversion.
					e.RealObject = ConvertDummyToReal(e.OwningFlid, e.DummyHvo);
				}
				else
				{
					e.RealObject = realObj;
				}
			}
			else if (fRequiresRealParserGeneratedData || sender is XmlVc)
			{
				// if the sender is XmlVc, assume we're trying to load the information on demand.
				// add it to our queue for converting and issuing a PropChanged for our list when safe to do so.
				// plus, its probably faster to do several conversions at once, rather than one at a time.
				// we don't want to do a conversion and PropChanged while Drawing, because the views objects can still be
				// using and depending upon the old objects.
				if (!m_dummyToRealDict.ContainsKey(e.DummyHvo))
				{
					m_dummyToRealDict.Add(e.DummyHvo, null);
					m_requestedDummiesToConvertQueue.Add(e.DummyHvo);	// add to the end (Enqueue).
					m_dummiesToConvertRequestsMap[e.DummyHvo] = e;
				}
			}
			// Enhance: Should we only create a real object if the parser has been started?
		}

		protected override FdoObjectSet<ICmObject> GetObjectSet()
		{
			BaseVirtualHandler handler = VirtualHandler;
			if (handler != null)
			{
				FdoObjectSet<ICmObject> fdoObjs = null;
				// Original code won't work for virtual property (or anything else that isn't owning), so do
				// this instead.
				// Force the handler to (re)load the property.
				Debug.WriteLine("GetObjectSet: " + handler.ClassName + ", " + handler.FieldName +
					"(" + handler.Tag.ToString() + ") owningObject.Hvo: " + m_owningObject.Hvo.ToString());
				// Assume we're reloading everything, so invalidate anything pending queue requests.
				// Enhance, we only need to invalidate our queue requests if a dummy item becomes invalid.
				m_requestedDummiesToConvertQueue.Clear();
				m_dummiesToConvertRequestsMap.Clear();
				m_dummyToRealDict.Clear();		// we'll be recreating our dummy list.
				using (ProgressState progress = FwXWindow.CreateMilestoneProgressState(m_mediator))
				{
					// give it a new ProgressBar.
					if (handler is BaseFDOPropertyVirtualHandler)
						(handler as BaseFDOPropertyVirtualHandler).Progress = progress;
					if (NeedToReloadVirtualProperty)
					{
						ForceReloadVirtualProperty(handler);
						NeedToReloadVirtualProperty = false;
					}
					int[] items = m_cache.GetVectorProperty(m_owningObject.Hvo, m_flid, true);

					// Identify all remaining dummy items
					if (m_fConvertRemainingDummiesOnIdle)
					{
						m_remainingDummiesToConvertOnIdle.Clear();
						for (int i = 0; i < items.Length; ++i)
						{
							if (m_cache.IsDummyObject(items[i]))
								m_remainingDummiesToConvertOnIdle.Add(items[i]);
						}
					}
					fdoObjs = new FdoObjectSet<ICmObject>(m_cache, items, false);
				}
				if (handler is BaseFDOPropertyVirtualHandler)
					(handler as BaseFDOPropertyVirtualHandler).Progress = null;
				return fdoObjs;
			}
			return null;
		}

		#region IDummy Members

		protected bool SharesResponsibleFor(int hvo)
		{
			int hvoOwner = m_cache.GetOwnerOfObject(hvo);
			int hvoOwningFlid = m_cache.GetOwningFlidOfObject(hvo);
			return (m_owningObject != null && m_owningObject.Hvo == hvoOwner) ||
				hvoOwningFlid == m_flid ||
				Cache.GetDestinationClass((uint)m_flid) == m_cache.GetClassOfObject(hvo);
		}

		public ICmObject ConvertDummyToReal(int owningFlid, int hvoDummy)
		{
			CheckDisposed();

			ICmObject obj = null;
			// Our list only needs to handle conversion if it owns or refers to this hvoDummy.
			Debug.Assert(SharesResponsibleFor(hvoDummy), "We don't need to try to handle converting something that isn't compatible with our list.");
			Dictionary<int, ICmObject> dummyToRealDict = ConvertDummiesToReal(owningFlid, new int[] { hvoDummy });
			dummyToRealDict.TryGetValue(hvoDummy, out obj);
			return obj;
		}

		public virtual bool OnPrepareToRefresh(object args)
		{
			CheckDisposed();

			if (m_owningObject is IDummy)
				(m_owningObject as IDummy).OnPrepareToRefresh(args);
			return false; // other things may wish to prepare too.
		}

		#endregion

		/// <summary>
		/// given hvosToReplace, try to convert each one to real, and update the appropriate list (dummy queues, the m_flid vector, SortedObjects)
		/// </summary>
		/// <param name="owningFlid"></param>
		/// <param name="hvosToReplace"></param>
		/// <returns></returns>
		private Dictionary<int, ICmObject> ConvertDummiesToReal(int owningFlid, int[] hvosToReplace)
		{
			Dictionary<int, ICmObject> batchDummyToRealDict = new Dictionary<int, ICmObject>(hvosToReplace.Length);
			m_fUpdatingList = true;	// prevent a ReloadList while we're updating the list.
			try
			{
				if (hvosToReplace.Length == 0)
				{
					return batchDummyToRealDict;
				}
				int hvoOwner = m_cache.GetOwnerOfObject(hvosToReplace[0]);
				ICmObject owningObject = CmObject.CreateFromDBObject(Cache, hvoOwner);
				if (!(owningObject is IDummy))
					return batchDummyToRealDict;
				m_flidOwningItemToConvertToReal = owningFlid;

				// Can't use Generic class for newSortItems,
				// since the sorter will eventually want to have a say on what kind of objects they are.
				ArrayList newSortItems = new ArrayList(hvosToReplace.Length);
				for (int i = 0; i < hvosToReplace.Length; i++)
				{
					// 1) Let the owning object replace its instance of the object with a real object
					int hvoToReplace = hvosToReplace[i];
					Debug.Assert(m_cache.IsDummyObject(hvoToReplace));
					ICmObject realObj = null;
					realObj = (owningObject as IDummy).ConvertDummyToReal(owningFlid, hvoToReplace);
					// make a temporary dictionary of conversions for this session.
					batchDummyToRealDict.Add(hvoToReplace, realObj);
					// record the conversion to the master dictionary.
					m_dummyToRealDict[hvoToReplace] = realObj;
					ReplaceDummyItemWithReal(owningFlid, hvoToReplace, realObj, ref newSortItems);
				}
				if (newSortItems.Count > 0)
					SendPropChangedOnListChange(CurrentIndex, SortedObjects, ListChangedEventArgs.ListChangedActions.SkipRecordNavigation);
			}
			catch (Exception e)
			{
				throw e;
			}
			finally
			{
				m_flidOwningItemToConvertToReal = 0;
				m_fUpdatingList = false;	// allow ReloadList.
			}

			return batchDummyToRealDict;
		}

		/// <summary>
		/// Update the state of our managed lists (queues, m_flid vector, SortedObjects) by the given hvoToReplace with realObj.Hvo.
		/// </summary>
		/// <param name="owningFlid"></param>
		/// <param name="hvoToReplace"></param>
		/// <param name="realObj"></param>
		/// <param name="newSortItems"></param>
		private void ReplaceDummyItemWithReal(int owningFlid, int hvoToReplace, ICmObject realObj, ref ArrayList newSortItems)
		{
			int hvoRealObject = realObj != null ? realObj.Hvo : 0;
			Debug.Assert(hvoRealObject != 0, hvoToReplace.ToString() + " should have converted to a real object.");
			// replace the item in our virtual property vector if the conversion was on a different owningFlid
			if (owningFlid != m_flid)
			{
				int ihvoToReplace = -1;
				// note: fDoNotify is set to false, so we don't do a PropChanged here. Relevant record lists
				// should be updated through RequestConversionToReal event.
				if (!Cache.TryCacheReplaceOneItemInVector(m_owningObject.Hvo, m_flid, hvoToReplace, hvoRealObject, false, false, out ihvoToReplace))
				{
					return;	// we don't have the hvo in our vector, so it shouldn't be in our SortItems list either.
				}
			}
			// else we only care about conversions owned by OwningObject
			else if (OwningObject == null ||
				Cache.IsDummyObject(hvoToReplace) && Cache.GetOwnerOfObject(hvoToReplace) != OwningObject.Hvo)
			{
				return;		// this isn't owned by our owning object, so we don't expect it to be in our list.
			}
			// remove it from our queues if it's in there.
			Debug.Assert(m_requestedDummiesToConvertQueue.Count == 0 || m_requestedDummiesToConvertQueue[0] == hvoToReplace,
				"We should empty our request queue first.");
			if (m_requestedDummiesToConvertQueue.Count > 0 && m_requestedDummiesToConvertQueue[0] == hvoToReplace)
				m_requestedDummiesToConvertQueue.RemoveAt(0);
			m_dummiesToConvertRequestsMap.Remove(hvoToReplace);
			m_remainingDummiesToConvertOnIdle.Remove(hvoToReplace);

			// 2) See if our RecordList has a cooresponding sort item we need to replace.
			newSortItems.AddRange(ReplaceListItem(realObj, hvoToReplace, true));
		}

		#region IxCoreColleague Members

		public IxCoreColleague[] GetMessageTargets()
		{
			CheckDisposed();

			return new IxCoreColleague[] { this };
		}

		public void Init(Mediator mediator, XmlNode configurationParameters)
		{
			CheckDisposed();

			// should already be handled by our other Init.
		}

		#endregion
	}

	/// <summary>
	/// this is a specialty subclass for grabbing all of the items from a possibility list.
	/// </summary>
	public class PossibilityRecordList : RecordList
	{
		public PossibilityRecordList()
		{
		}

		/// <summary>
		/// A possibility list is specified in the XML file by specifying the owner and the
		/// property of the list (without the "OA" used in FDO).
		/// </summary>
		public override void Init(FdoCache cache, Mediator mediator, XmlNode recordListNode)
		{
			CheckDisposed();

			BaseInit(cache, mediator, recordListNode);
			string owner = XmlUtils.GetManditoryAttributeValue(recordListNode, "owner");
			string property = XmlUtils.GetManditoryAttributeValue(recordListNode, "property");
			ICmObject obj = null;

			switch(owner)
			{
				case "LangProject":
					obj = cache.LangProject;
					break;
				case "LexDb":
					obj = cache.LangProject.LexDbOA;
					break;
				case "MorphologicalData":
					obj = cache.LangProject.MorphologicalDataOA;
					break;
				case "RnResearchNbk":
					obj = cache.LangProject.ResearchNotebookOA;
					break;
				case "DsDiscourseData":
					if (cache.LangProject.DiscourseDataOA == null)
					{
						cache.LangProject.GetDefaultChartTemplate(); // Fixes part of LT-8517; should create DiscourseDataOA
						cache.LangProject.GetDefaultChartMarkers();
					}
					obj = cache.LangProject.DiscourseDataOA;
					break;
				default:
					Debug.Assert(false, "Illegal owner specified for possibility list.");
					break;
			}
			m_owningObject = obj.GetType().InvokeMember(property + "OA",
				BindingFlags.Public | BindingFlags.NonPublic |
				BindingFlags.Instance | BindingFlags.GetProperty, null, obj, null) as FDO.Cellar.CmPossibilityList;
			m_oldLength = 0;

			Debug.Assert(m_owningObject != null, "Failed to find possibility list.");
			m_fontName = cache.LangProject.DefaultAnalysisWritingSystemFont;
			m_typeSize = GetFontHeightFromStylesheet(cache, mediator, true);
			m_flid  = (int)CmPossibilityList.CmPossibilityListTags.kflidPossibilities;
		}

		/// <summary>
		/// We override this so that the list can be made up of nested list items, not just the top-level ones.
		/// </summary>
		/// <returns></returns>
		protected override FdoObjectSet<ICmObject> GetObjectSet()
		{
			// Preload the entire list as efficiently as possible. This can avoid many thousands of queries on
			// larger lists such as semantic domains.
			return new FdoObjectSet<ICmObject>(
				m_cache,
				SIL.FieldWorks.FDO.Cellar.CmPossibility.PreLoadList(m_cache, m_owningObject.Hvo).ToArray(),
				false);
			/*
						// There is probably a more efficient way of doing this specifically for Possibility Items, but
						// we'll use the generic approach for now.
						int clidPoss = (int)CmSemanticDomain.kClassId;
						string squery = "declare @uid uniqueidentifier, @pssClass int" +
							" select @pssClass = ItemClsid from CmPossibilityList where id = " + m_owningObject.Hvo +
							" exec GetLinkedObjects$ @uid output, " + m_owningObject.Hvo + ", 176160768, 1, 0, 1" +
							" select ObjId, @pssClass from ObjInfoTbl$ where uid = @uid and ObjClass = " + clidPoss +
							" exec CleanObjInfoTbl$ @uid";
						return new FdoObjectSet(m_cache, squery, false, true);
			*/
		}

		protected override ClassAndPropInfo GetMatchingClass(string className)
		{
			// A possibility list only allows one type of possibility to be owned in the list.
			ICmPossibilityList pssl = (ICmPossibilityList)m_owningObject;
			int possClass = pssl.ItemClsid;
			string sPossClass = m_cache.MetaDataCacheAccessor.GetClassName((uint)possClass);
			if (sPossClass != className)
				return null;
			foreach(ClassAndPropInfo cpi in m_insertableClasses)
			{
				if (cpi.signatureClassName == className)
				{
					return cpi;
				}
			}
			return null;
		}

		/// <summary>
		/// Adjust the name of the menu item if appropriate. PossibilityRecordList overrides.
		/// </summary>
		/// <param name="command"></param>
		/// <param name="display"></param>
		public override void AdjustInsertCommandName(Command command, UIItemDisplayProperties display)
		{
			CheckDisposed();

			ICmPossibilityList pssl = (ICmPossibilityList)m_owningObject;
			string owningFieldName = Cache.MetaDataCacheAccessor.GetFieldName((uint)pssl.OwningFlid);
			string itemTypeName = pssl.ItemsTypeName(m_mediator.StringTbl);
			if (itemTypeName != "*" + owningFieldName + "*")
				display.Text = "_" + itemTypeName;	// prepend a keyboard accelarator marker
			string toolTipInsert = display.Text.Replace("_", string.Empty);	// strip any menu keyboard accelerator marker;
			command.ToolTipInsert = toolTipInsert.ToLower();
		}

		// For this class we have to reload if sub-possibilities changed, too.
		// Enhance JohnT: we could attempt to verify that hvo is something owned by our root object,
		// and ignore if not. This would only be a gain if there is a probability of modifying some
		// other possibility list while ours is active.
		public override void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			CheckDisposed();

			// Singularly a bad idea to call the base code, which will do a reload,
			// and then do another reload here, if the tags are matching, below.
			// base.PropChanged (hvo, tag, ivMin, cvIns, cvDel);
			// We'll call the base code, only if we don't deal with the change here.

			if (tag == (int)CmPossibility.CmPossibilityTags.kflidSubPossibilities
				 || tag == (int)CmPossibilityList.CmPossibilityListTags.kflidPossibilities
				&& (cvIns > 0 || cvDel > 0))
			{
				// Reload the whole list, since a deleted/added node may have owned sub-possibilities.
				// Those subpossibilities would remain in the full sorted set of objects,
				// even though they have been deleted.
				// That would then cause the crash seen in LT-6036,
				// when the dead objects had a class of 0, which was right for a dead object.
				// At least it was "right" for the current state of the cache code.

				// They will need to be added to the sorted list on an insert, as well.

				// So, whether an item was added or deleted, we need to reload the whole thing.
				ReloadList();
			}
			else if (tag == (int)CmPossibility.CmPossibilityTags.kflidName ||
				tag == (int)CmPossibility.CmPossibilityTags.kflidAbbreviation)
			{
				List<int> hvoTargets = new List<int>(new int[] { hvo });
				if (IndexOfFirstSortItem(hvoTargets) != -1)
					UpdateListItemName();
			}
			else
			{
				base.PropChanged(hvo, tag, ivMin, cvIns, cvDel);
			}
		}
		#region navigation

		/// <summary>
		/// Return the root object at the specified index as a CmPossibility. (Will be null
		/// if it is not a CmPossibility.)
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		ICmPossibility PossibilityAt(int index)
		{
			return RootObjectAt(index) as ICmPossibility;
		}

		/// <summary>
		/// Return the index (in m_sortedObjects) of the first displayed object.
		/// For possibility lists, the first item that isn't owned by another item.
		/// </summary>
		public override int FirstItemIndex
		{
			get
			{
				CheckDisposed();

				if (m_sortedObjects == null || m_sortedObjects.Count == 0)
					return -1;
				for (int i = 0; i < m_sortedObjects.Count; i++)
				{
					ICmPossibility poss = PossibilityAt(i);
					if (m_cache.GetOwningFlidOfObject(poss.Hvo) != (int)CmPossibility.CmPossibilityTags.kflidSubPossibilities)
						return i;
				}
				return -1; // Bizarre..maybe filtering would do this??
			}
		}

		/// <summary>
		/// Return the index (in m_sortedObjects) of the last displayed object.
		/// For possibility lists, this is quite tricky to find.
		/// </summary>
		public override int LastItemIndex
		{
			get
			{
				CheckDisposed();

				if (m_sortedObjects == null || m_sortedObjects.Count == 0)
					return -1;
				int lastTopIndex = -1;
				for (int i = m_sortedObjects.Count; --i >= 0 ;)
				{
					ICmPossibility poss = PossibilityAt(i);
					if (m_cache.GetOwningFlidOfObject(poss.Hvo) != (int)CmPossibility.CmPossibilityTags.kflidSubPossibilities)
					{
						lastTopIndex = i;
						break;
					}
				}
				if (lastTopIndex == -1)
					return -1; // Bizarre..maybe filtering would do this??
				return LastChild(PossibilityAt(lastTopIndex).Hvo, lastTopIndex);
			}
		}

		/// <summary>
		/// If hvoPoss has children, return the index of the last of them
		/// that occurs in m_sortedObjects
		/// (and recursively, the last of its children).
		/// If not, return the index of hvoPoss itself (the index passed).
		/// </summary>
		/// <param name="hvoPoss"></param>
		/// <returns></returns>
		int LastChild(int hvoPoss, int index)
		{
			int count = m_cache.MainCacheAccessor.get_VecSize(hvoPoss,
				(int)CmPossibility.CmPossibilityTags.kflidSubPossibilities);
			if (count == 0)
				return index; // no children
			// Find the last child that occurs in the list.
			for (int ichild = count; --ichild >= 0; )
			{
				int hvoChild = m_cache.MainCacheAccessor.get_VecItem(hvoPoss,
					(int)CmPossibility.CmPossibilityTags.kflidSubPossibilities, ichild);
				int index1 = IndexOf(hvoChild);
				if (index1 >= 0)
					return LastChild(hvoChild, index1);
			}
			return index; // we didn't find it, treat as having no children.
		}

		/// <summary>
		/// Return the index (in m_sortedObjects) of the object that follows the one
		/// at m_currentIndex.
		/// In a possibility list, if the current item has a child it is the first of those
		/// in m_sortedObjects.
		/// Otherwise, it's the first object owned by the owner of this AFTER this.
		/// If the list is empty return -1.
		/// If the current object is the last return m_currentIndex.
		/// If m_currentIndex is -1 return -1.
		/// </summary>
		public override int NextItemIndex
		{
			get
			{
				CheckDisposed();

				if (m_sortedObjects == null || m_sortedObjects.Count == 0 || CurrentIndex == -1)
					return -1;
				ISilDataAccess sda = m_cache.MainCacheAccessor;
				int hvoCurrent = PossibilityAt(CurrentIndex).Hvo;
				int count = sda.get_VecSize(hvoCurrent,
					(int)CmPossibility.CmPossibilityTags.kflidSubPossibilities);
				for (int ichild = 0; ichild < count; ichild++)
				{

					int index = IndexOf(sda.get_VecItem(hvoCurrent,
						(int)CmPossibility.CmPossibilityTags.kflidSubPossibilities, ichild));
					if (index >= 0)
						return index;
				}

				for ( ; ; )
				{
					// look for a sibling of hvoCurrentBeforeGetObjectSet coming after the starting point.
					int hvoOwner = m_cache.GetOwnerOfObject(hvoCurrent);
					int flidOwner = m_cache.GetOwningFlidOfObject(hvoCurrent);
					int count1 = sda.get_VecSize(hvoOwner, flidOwner);
					bool fGotCurrent = false;
					for (int ichild = 0; ichild < count1; ichild++)
					{
						int hvoChild = sda.get_VecItem(hvoOwner, flidOwner, ichild);
						if (hvoChild == hvoCurrent)
						{
							fGotCurrent = true;
							continue;
						}
						if (!fGotCurrent)
							continue; // skip items before current one.
						int index = IndexOf(hvoChild);
						if (index >= 0)
							return index;
					}
					// No subsequent sibling of this.
					// Look for a sibling of the owner.
					// But, if the owning property is not sub-possibilities, we've reached the root
					// and can search no further.
					if (flidOwner != (int)CmPossibility.CmPossibilityTags.kflidSubPossibilities)
						return CurrentIndex;
					hvoCurrent = hvoOwner;
				}
			}
		}

		/// <summary>
		/// Return the index (in m_sortedObjects) of the object that precedes the one
		/// at m_currentIndex.
		/// In possibility lists, this is the last child of the preceding sibling,
		/// or the owner,...
		/// If the list is empty return -1.
		/// If the current object is the first return m_currentIndex.
		/// If m_currentIndex is -1 return -1.
		/// </summary>
		public override int PrevItemIndex
		{
			get
			{
				CheckDisposed();

				if (m_sortedObjects == null || m_sortedObjects.Count == 0 || CurrentIndex == -1)
					return -1;
				ISilDataAccess sda = m_cache.MainCacheAccessor;
				int hvoCurrent = PossibilityAt(CurrentIndex).Hvo;
				int hvoOwner = m_cache.GetOwnerOfObject(hvoCurrent);
				// Look for a previous sibling in the list.
				int flidOwner = m_cache.GetOwningFlidOfObject(hvoCurrent);
				int count = sda.get_VecSize(hvoOwner, flidOwner);
				bool fGotCurrent = false;
				for (int ichild = count; --ichild >= 0; )
				{
					int hvoChild = sda.get_VecItem(hvoOwner, flidOwner, ichild);
					if (hvoChild == hvoCurrent)
					{
						fGotCurrent = true;
						continue;
					}
					if (!fGotCurrent)
						continue; // skip items after current one.
					int index = IndexOf(hvoChild);
					if (index >= 0)
						return LastChild(hvoChild, index);
				}

				// OK, no sibling. Return owner if it's in the list.
				int index1 = IndexOf(hvoOwner);
				if (index1 >= 0)
					return index1;
				return CurrentIndex; // no previous object exists.
			}
		}
		#endregion
	}

	/// <summary>
	/// Record list that can be used to bulk edit Entries or its child classes (e.g. Senses or Pronunciations).
	/// The class owning relationship between these properties can be defined by the destinationClass of the
	/// properties listed as 'sourceField' in the RecordList xml definition:
	/// <code>
	///		<ClassOwnershipTree>
	///			<LexEntry sourceField="Entries">
	///				<LexPronunciation sourceField="AllPronunciations"/>
	///				<LexSense sourceField="AllSenses"/>
	///			</LexEntry>
	///		</ClassOwnershipTree>
	/// </code>
	/// </summary>
	public class EntriesOrChildClassesRecordList : RecordList, IMultiListSortItemProvider
	{
		int m_flidEntries = 0;
		int m_prevFlid = 0;
		IDictionary<int, bool> m_reloadNeededForProperty = new Dictionary<int, bool>();
		PartOwnershipTree m_pot = null;
		XmlNode m_configuration = null;

		bool m_suspendReloadUntilOnChangeListItemsClass = false;

		public EntriesOrChildClassesRecordList()
		{ }

		/// <summary>
		/// this list can work with multiple properties to load its list (e.g. Entries or AllSenses).
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="mediator"></param>
		/// <param name="recordListNode"></param>
		public override void Init(FdoCache cache, XCore.Mediator mediator, XmlNode recordListNode)
		{
			CheckDisposed();
			// suspend loading the property until given a class by RecordBrowseView via
			// RecordClerk.OnChangeListItemsClass();
			m_suspendReloadUntilOnChangeListItemsClass = true;

			m_configuration = recordListNode;
			BaseInit(cache, mediator, recordListNode);
			bool analysis = XmlUtils.GetOptionalBooleanAttributeValue(recordListNode, "analysisWs", false);
			// used for finding first relative of corresponding current object
			m_pot = PartOwnershipTree.Create(cache, this, true);

			GetDefaultFontNameAndSize(analysis, cache, mediator, out m_fontName, out m_typeSize);
			string owner = XmlUtils.GetOptionalAttributeValue(recordListNode, "owner");
			// by default we'll setup for Entries
			GetTargetFieldInfo(LexEntry.kclsidLexEntry, owner, out m_owningObject, out m_flid, out m_propertyName);

		}

		protected override void DisposeManagedResources()
		{
			if (m_pot != null && !m_pot.IsDisposed)
				m_pot.Dispose();
			base.DisposeManagedResources();
		}

		public override void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			using (RecordClerk.ListUpdateHelper luh = new RecordClerk.ListUpdateHelper(Clerk))
			{
				// don't reload the entire list via propchanges.  just indicate we need to reload.
				luh.TriggerPendingReloadOnDispose = false;
				base.PropChanged(hvo, tag, ivMin, cvIns, cvDel);

				// even if RecordList is in m_fUpdatingList mode, we still want to make sure
				// our alternate list figures out wheter it needs to reload as well.
				if (m_fUpdatingList)
					TryHandleUpdateOrMarkPendingReload(hvo, tag, ivMin, cvIns, cvDel);
			}
		}

		/// <summary>
		/// if a bulk edit has created a child for a ghost entry, then exchange the entry for the new child.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="ivMin"></param>
		/// <param name="cvIns"></param>
		/// <param name="cvDel"></param>
		/// <returns></returns>
		protected override bool TryModifyingExistingList(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			IVwVirtualHandler vh;
			if (Cache.TryGetVirtualHandler(m_flid, out vh))
			{
				if (vh.DoesResultDependOnProp((int)m_owningObject.Hvo, hvo, tag, 0))
				{
					// TODO: determine whether hvo is a ghost entry. if so, change that entry in our list and sort items.
				}
			}
			return false;

		}

		protected override bool TryHandleUpdateOrMarkPendingReload(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			// first try to handle updates for currently loaded m_flid
			bool fHandled = base.TryHandleUpdateOrMarkPendingReload(hvo, tag, ivMin, cvIns, cvDel);

			// next try to handle marking pending reloads for our alternate flids.
			List<int> flidsLoaded = new List<int>(m_reloadNeededForProperty.Keys);
			foreach (int flid in flidsLoaded)
			{
				if (m_flid == flid)
					continue;	// already handled by base.TryHandleUpdateOrMarkPendingReload
				// check to make sure AllSenses do not dependent upon tag.
				IVwVirtualHandler vh;
				if (Cache.TryGetVirtualHandler(flid, out vh))
				{
					if (vh.DoesResultDependOnProp((int)m_owningObject.Hvo, hvo, tag, 0))
						m_reloadNeededForProperty[flid] = true;
				}
				else if (flid == m_flidEntries && (tag == m_flidEntries || EntriesDependsUponProp(tag)))
				{
					// adding or removing senses should not cause reloading of owning Entries list,
					// unless we are removing entries.
					MarkEntriesForReload();
				}
			}
			return fHandled;
		}

		protected override void MarkEntriesForReload()
		{
			m_reloadNeededForProperty[m_flidEntries] = true;
		}

		protected override void FinishedReloadList()
		{
			m_reloadNeededForProperty[m_flid] = false;
		}

		public override bool NeedToReloadVirtualProperty
		{
			get
			{
				return base.NeedToReloadVirtualProperty ||
					m_flid != m_flidEntries && m_reloadNeededForProperty[m_flid];
			}
			set
			{
				if (m_flid != m_flidEntries)
				{
					m_reloadNeededForProperty[m_flid] = value;
					base.NeedToReloadVirtualProperty = value;
				}
			}
		}

		protected internal override bool RequestedLoadWhileSuppressed
		{
			get
			{

				return base.RequestedLoadWhileSuppressed ||
					m_reloadNeededForProperty[m_flid];
			}
			set
			{
				base.RequestedLoadWhileSuppressed = value;
			}
		}

		protected internal override bool NeedToReloadList()
		{
			return this.RequestedLoadWhileSuppressed;
		}


		public override void ReloadList()
		{
			if (m_suspendReloadUntilOnChangeListItemsClass)
			{
				m_requestedLoadWhileSuppressed = true;
				return;
			}
			base.ReloadList();
		}

		protected internal override void ReloadList(int newListItemsClass, bool force)
		{
			// reload list if it differs from current target class (or if forcing a reload anyway).
			if (newListItemsClass != this.ListItemsClass || force)
			{
				string owner = m_owningObject.GetType().Name;
				ICmObject owningObj;
				m_prevFlid = m_flid;
				GetTargetFieldInfo(newListItemsClass, owner, out owningObj, out m_flid, out m_propertyName);
				CheckExpectedListItemsClassInSync(newListItemsClass, this.ListItemsClass);
				ReloadList();
			}
			// wait until afterwards, so that the dispose will reload the list for the first time
			// whether or not we've loaded yet.
			if (m_suspendReloadUntilOnChangeListItemsClass)
			{
				m_suspendReloadUntilOnChangeListItemsClass = false;
				ReloadList();
			}
			// otherwise, we'll assume there isn't anything to load.
		}

		protected override int GetNewCurrentIndex(ArrayList newSortedObjects, int hvoCurrentBeforeGetObjectSet)
		{
			if (hvoCurrentBeforeGetObjectSet == 0)
				return -1;
			// first lookup the old object in the new list, just in case it's there.
			int newIndex = base.GetNewCurrentIndex(newSortedObjects, hvoCurrentBeforeGetObjectSet);
			if (newIndex != -1)
				return newIndex;
			int newListItemsClass = this.ListItemsClass;
			// NOTE: the class of hvoBeforeListChange could be different then prevListItemsClass, if the item is a ghost (owner).
			int classOfObsoleteCurrentObj = Cache.GetClassOfObject(hvoCurrentBeforeGetObjectSet);
			if (Cache.IsSameOrSubclassOf(classOfObsoleteCurrentObj, newListItemsClass))
			{
				// nothing else we can do, since we've already looked that class of object up
				// in our newSortedObjects.
				return -1;
			}

			// we've changed list items class, so find the corresponding new object.
			Set<int> commonAncestors;
			Set<int> relatives = m_pot.FindCorrespondingItemsInCurrentList(m_prevFlid,
				new Set<int>(new int[] {hvoCurrentBeforeGetObjectSet}),
					m_flid,
				out commonAncestors);
			int newHvoRoot = relatives.Count > 0 ? relatives.ToArray()[0] : 0;
			int hvoCommonAncestor = commonAncestors.Count > 0 ? commonAncestors.ToArray()[0] : 0;
			if (newHvoRoot == 0 && hvoCommonAncestor != 0)
			{
				// see if we can find the parent in the list (i.e. it might be in a ghost list)
				newIndex = base.GetNewCurrentIndex(newSortedObjects, hvoCommonAncestor);
				if (newIndex != -1)
					return newIndex;
			}

			return base.GetNewCurrentIndex(newSortedObjects, newHvoRoot);
		}


		/// <summary>
		/// Get the appropriate flid for the targetClsId, and cache the results on m_flidAllSenses or m_flidEntries.
		/// </summary>
		/// <param name="targetClsId">if 0, use default "LexEntry" class</param>
		/// <param name="owner"></param>
		/// <param name="owningObj"></param>
		/// <param name="flid"></param>
		/// <param name="flidName"></param>
		private void GetTargetFieldInfo(int targetClsId, string owner, out ICmObject owningObj, out int flid, out string flidName)
		{
			owningObj = null;
			flidName = "";
			flid = 0;
			// first try to find the expected source field.
			flidName = m_pot.GetSourceFieldName(targetClsId);
			flid = GetFlidOfVectorFromName(flidName, owner, Cache, m_mediator, out owningObj);
			if (!m_reloadNeededForProperty.ContainsKey(flid))
				m_reloadNeededForProperty.Add(flid, false);
			if (m_flidEntries == 0 && targetClsId == LexEntry.kclsidLexEntry)
				m_flidEntries = flid;
		}

		/// <summary>
		/// these bulk edit column filters/sorters can be considered Entries based, so that they can possibly
		/// be reusable in other Entries clerks (e.g. the one used by Lexicon Edit, Browse, Dictionary).
		/// </summary>
		/// <param name="sorterOrFilter"></param>
		/// <returns></returns>
		internal protected override string PropertyTableId(string sorterOrFilter)
		{
			return String.Format("{0}.{1}_{2}", "LexDb", "Entries", sorterOrFilter);
		}

		#region IMultiListSortItemProvider Members

		/// <summary>
		/// See documentation for IMultiListSortItemProvider
		/// </summary>
		public object ListSourceToken
		{
			get { return m_flid; }
		}

		/// <summary>
		/// See documentation for IMultiListSortItemProvider
		/// </summary>
		public XmlNode PartOwnershipTreeSpec
		{
			get
			{
				return m_configuration != null ? m_configuration.SelectSingleNode("./PartOwnershipTree") : null;
			}
		}

		/// <summary>
		/// See documentation for IMultiListSortItemProvider
		/// </summary>
		/// <param name="itemAndListSourceTokenPairs"></param>
		/// <returns></returns>
		public void ConvertItemsToRelativesThatApplyToCurrentList(ref IDictionary<int, object> oldItems)
		{
			Set<int> oldItemsToRemove = new Set<int>();
			Set<int> itemsToAdd = new Set<int>();
			// Create a PartOwnershipTree in a mode that can return more than one descendent relatives.
			using (PartOwnershipTree pot = PartOwnershipTree.Create(Cache, this, false))
			{
				foreach (KeyValuePair<int, object> oldItem in oldItems)
				{
					IDictionary<int, object> dictOneOldItem = new Dictionary<int, object>();
					dictOneOldItem.Add(oldItem);
					Set<int> relatives = FindCorrespondingItemsInCurrentList(dictOneOldItem, pot);

					// remove the old item if we found relatives we could convert over to.
					if (relatives.Count > 0)
					{
						itemsToAdd.AddRange(relatives);
						oldItemsToRemove.Add(oldItem.Key);
					}
				}
			}

			foreach (int itemToRemove in oldItemsToRemove)
				oldItems.Remove(itemToRemove);

			// complete any conversions by adding its relatives.
			object sourceTag = ListSourceToken;
			foreach (int relativeToAdd in itemsToAdd)
			{
				if (!oldItems.ContainsKey(relativeToAdd))
					oldItems.Add(relativeToAdd, sourceTag);
			}
		}

		private Set<int> FindCorrespondingItemsInCurrentList(IDictionary<int, object> itemAndListSourceTokenPairs, PartOwnershipTree pot)
		{
			// create a reverse index of classes to a list of items
			IDictionary<int, Set<int>> sourceFlidsToItems = MapSourceFlidsToItems(itemAndListSourceTokenPairs);

			Set<int> relativesInCurrentList = new Set<int>();
			foreach (KeyValuePair<int, Set<int>> sourceFlidToItems in sourceFlidsToItems)
			{
				Set<int> commonAncestors;
				relativesInCurrentList.AddRange(pot.FindCorrespondingItemsInCurrentList(sourceFlidToItems.Key,
																		 sourceFlidToItems.Value, m_flid,
																		 out commonAncestors));
			}
			return relativesInCurrentList;
		}

		private IDictionary<int, Set<int>> MapSourceFlidsToItems(IDictionary<int, object> itemAndListSourceTokenPairs)
		{
			IDictionary<int, Set<int>> sourceFlidsToItems = new Dictionary<int, Set<int>>();
			foreach (KeyValuePair<int, object> itemAndSourceTag in itemAndListSourceTokenPairs)
			{
				if ((int)itemAndSourceTag.Value == m_flid)
				{
					// skip items in the current list
					// (we're trying to lookup relatives to items that are a part of
					// a previous list, not the current one)
					continue;
				}
				Set<int> itemsInSourceFlid;
				if (!sourceFlidsToItems.TryGetValue((int)itemAndSourceTag.Value, out itemsInSourceFlid))
				{
					itemsInSourceFlid = new Set<int>();
					sourceFlidsToItems.Add((int)itemAndSourceTag.Value, itemsInSourceFlid);
				}
				itemsInSourceFlid.Add(itemAndSourceTag.Key);
			}
			return sourceFlidsToItems;
		}

		#endregion
	}

	/// <summary>
	/// This type of record list is used in conjunction with a MatchingItemsRecordClerk.
	/// </summary>
	public class MatchingItemsRecordList : RecordList
	{
		private XmlNode m_configNode;
		private int[] m_rghvo;

		public MatchingItemsRecordList()
		{
		}

		public override void Init(FdoCache cache, XCore.Mediator mediator, XmlNode recordListNode)
		{
			m_fEnableSendPropChanged = false;
			m_configNode = recordListNode;
			base.Init(cache, mediator, recordListNode);
		}

		public override void InitLoad()
		{
			CheckDisposed();
			ComputeInsertableClasses();
			m_currentIndex = -1;
			m_hvoCurrent = 0;
		}

		public override void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
		}

		/// <summary>
		/// We never want to filter matching items displayed in the dialog.  See LT-6422.
		/// </summary>
		public override RecordFilter Filter
		{
			get
			{
				return null;
			}
			set
			{
				return;
			}
		}

		protected override FdoObjectSet<ICmObject> GetObjectSet()
		{
			return new FdoObjectSet<ICmObject>(m_cache, m_rghvo, false);
		}

		/// <summary>
		/// This reloads the list using the supplied set of hvos.
		/// </summary>
		/// <param name="rghvo"></param>
		public void UpdateList(int[] rghvo)
		{
			m_rghvo = rghvo;
			ReloadList();
		}
	}

	/// <summary>
	/// RecordList is a vector of objects
	/// </summary>
	/// <remarks>RecordList is not XCore aware but is aware of RecordSorter and RecordFilter.
	/// </remarks>
	public class RecordList : FwDisposableBase, IVwNotifyChange, ISortItemProvider
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

		protected FdoCache m_cache;
		/// <summary>
		/// Use this to do the Add/RemoveNotifications, since it can be used in the unmanged section of Dispose.
		/// (If m_sda is COM, that is.)
		/// Doing it there will be safer, since there was a risk of it not being removed
		/// in the mananged section, as when disposing was done by the Finalizer.
		/// </summary>
		private ISilDataAccess m_sda;
		protected RecordClerk m_clerk;
		protected RecordSorter m_sorter;
		protected RecordFilter m_filter;
		protected RecordFilter m_filterPrev;
		protected string m_propertyName;
		protected string m_fontName;
		protected int m_typeSize = 10;
		protected bool m_reloadingList = false;
		private bool m_suppressingLoadList = false;
		protected bool m_requestedLoadWhileSuppressed = false;
		protected bool m_deletingObject = false;
		protected int m_oldLength = 0;
		protected bool m_fUpdatingList = false;
		/// <summary>
		/// The actual database flid from which we get our list of objects, and apply a filter to.
		/// </summary>
		protected int m_flid;
		/// <summary>
		/// This is true if the list is the LexDb/LexEntries, and one of the entries has
		/// changed.
		/// </summary>
		protected bool m_fReloadLexEntries = false;
		/// <summary>
		/// Fake Field ID for the property in which we store our filtered and sorted list in cache
		/// </summary>
		protected int m_virtualFlid = 0;
		/// <summary>
		///
		/// </summary>
		protected Mediator m_mediator;
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
		protected int m_currentIndex;

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
		/// It is tempting to use List<ManyOnePathSortItems>, but it won't work,
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

		#endregion Data members

		#region Construction & initialization

		/// <summary>
		/// a factory method for RecordLists
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="recordListNode"></param>
		/// <returns></returns>
		static public RecordList Create(FdoCache cache, Mediator mediator, XmlNode recordListNode)
		{
			RecordList list = null;

			//does the configuration specify a special RecordList class?
			XmlNode customListNode = recordListNode.SelectSingleNode("dynamicloaderinfo");
			if (customListNode != null)
				list = (RecordList)DynamicLoader.CreateObject(customListNode);
			else
				list = new RecordList();

			list.Init(cache, mediator, recordListNode);
			return list;
		}

		public RecordList()
		{
		}

		/// <summary>
		/// Get the font size from the Stylesheet
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="mediator"></param>
		/// <param name="analysisWs">pass in 'true' for the DefaultAnalysisWritingSystem
		/// pass in 'false' for the DefaultVernacularWritingSystem</param>
		/// <returns>return Font size from stylesheet</returns>
		static protected int GetFontHeightFromStylesheet(FdoCache cache, Mediator mediator, bool analysisWs)
		{
			int fontHeight = 10;
			if (mediator != null)
			{
				IVwStylesheet stylesheet = FontHeightAdjuster.StyleSheetFromMediator(mediator);

				if (analysisWs)
				{
					fontHeight = FontHeightAdjuster.GetFontHeightForStyle(
					"Normal", stylesheet,
					cache.LangProject.DefaultAnalysisWritingSystem,
					cache.LanguageWritingSystemFactoryAccessor) / 1000; //fontHeight is probably pixels

				}
				else
				{
					fontHeight = FontHeightAdjuster.GetFontHeightForStyle(
					"Normal", stylesheet,
					cache.LangProject.DefaultVernacularWritingSystem,
					cache.LanguageWritingSystemFactoryAccessor) / 1000; //fontHeight is probably pixels

				}
			}
			return fontHeight;
		}

		public virtual void Init(FdoCache cache, Mediator mediator, XmlNode recordListNode)
		{
			CheckDisposed();

			BaseInit(cache, mediator, recordListNode);
			string owner = XmlUtils.GetOptionalAttributeValue(recordListNode, "owner");
			bool analysis = XmlUtils.GetOptionalBooleanAttributeValue(recordListNode, "analysisWs", false);
			if (m_propertyName != null && m_propertyName != "")
			{
				m_flid = GetFlidOfVectorFromName(m_propertyName, owner, analysis, cache, mediator,
					out m_owningObject, out m_fontName, out m_typeSize);
			}
			else
			{
				// TODO: This option won't set the font, so it uses Arial, which isn't right.
				m_fontName = "Arial";
				// Only other current option is to specify an ordinary property (or a virtual one0.
				m_flid = (int)cache.MetaDataCacheAccessor.GetFieldId(
					XmlUtils.GetManditoryAttributeValue(recordListNode, "class"),
					XmlUtils.GetManditoryAttributeValue(recordListNode, "field"), true);
				// Review JohnH(JohnT): This is only useful for dependent clerks, but I don't know how to check this is one.
				m_owningObject = null;
			}
			m_oldLength = 0;
		}

		protected void BaseInit(FdoCache cache, Mediator mediator, XmlNode recordListNode)
		{
			Debug.Assert(mediator != null);

			m_mediator = mediator;
			m_propertyName = XmlUtils.GetOptionalAttributeValue(recordListNode, "property", "");
			m_cache = cache;
			m_currentIndex = -1; // Can't use setter before sorted objects has a value.
			m_hvoCurrent = 0;
			m_virtualFlid = FdoCache.DummyFlid;
			m_oldLength = 0;
		}

		#endregion Construction & initialization

		#region Properties

		internal protected virtual string PropertyTableId(string sorterOrFilter)
		{
			// Dependent lists do not have owner/property set. Rather they have class/field.
			string className = Cache.MetaDataCacheAccessor.GetOwnClsName((uint)m_flid);
			string fieldName = Cache.MetaDataCacheAccessor.GetFieldName((uint)m_flid);
			if (String.IsNullOrEmpty(PropertyName) || PropertyName == fieldName)
			{
				return String.Format("{0}.{1}_{2}", className, fieldName, sorterOrFilter);
			}
			else
			{
				return String.Format("{0}.{1}_{2}", className, PropertyName, sorterOrFilter);
			}
		}

		public string PropertyName
		{
			get
			{
				CheckDisposed();

				return m_propertyName;
			}
		}

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
				//if (m_sorter != null)
				//	m_sorter.Dispose();
				m_sorter = value;
			}
		}

		public virtual RecordFilter Filter
		{
			set
			{
				CheckDisposed();

				if (m_filter == value)
					return;
				//if (m_filter != null)
				//	m_filter.Dispose();
				m_filter = value;
			}
			get
			{
				CheckDisposed();

				return m_filter;
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
				return m_virtualFlid;
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

		public ICmObject CurrentObject
		{
			get
			{
				CheckDisposed();

				if (m_sortedObjects == null || m_sortedObjects.Count == 0 || m_currentIndex == -1)
				{
					CurrentIndex = -1;
					return null;
				}

				try
				{
					return RootObjectAt(m_currentIndex);
				}
				catch (ArgumentException)//CmObject throws this when the object has been deleted todo:OR OTHERWISE BAD
				{
					CurrentIndex = -1;
					return null;
				}
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
		/// Returns the virtual handler for our Flid (if virtual).
		/// </summary>
		BaseVirtualHandler m_virtualHandler = null;
		protected BaseVirtualHandler VirtualHandler
		{
			get
			{
				if (m_virtualHandler == null)
				{
					IVwVirtualHandler vh;
					if (Cache.TryGetVirtualHandler(m_flid, out vh))
					{
						m_virtualHandler = vh as BaseVirtualHandler;
					}
				}
				return m_virtualHandler;
			}
		}

		/// <summary>
		/// will return -1 if the vector is empty.
		/// </summary>
		public int CurrentIndex
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
						m_currentIndex = -1;
					}
				}
				else
				{
					if (m_currentIndex < 0 || m_currentIndex >= m_sortedObjects.Count)
					{
						Debug.WriteLine("RecordList index out of range");
						m_currentIndex = 0;
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
					hvoCurrent = RootObjectAt(value).Hvo;
				if (m_currentIndex == value && m_hvoCurrent == hvoCurrent)
					return;

				m_currentIndex = value;
				m_hvoCurrent = hvoCurrent;

				if (m_currentIndex < 0)
					Logger.WriteEvent("No current record");
				else
				{
					if (this.CurrentObject != null && CurrentObject.IsValidObject())
						Logger.WriteEvent("Current Record = "+this.CurrentObject.ShortName);
					else
						Logger.WriteEvent("Current Record not valid");
				}
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

		internal FdoCache Cache
		{
			get
			{
				CheckDisposed();
				return m_cache;
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
			// make sure we uninstall any remaining event handler,
			// irrespective of whether we're active or not.
			if (m_windowPendingOnActivated != null && !m_windowPendingOnActivated.IsDisposed)
				UninstallWindowActivated();
			if (m_sda != null)
				m_sda.RemoveNotification(this);
			if (m_sorter != null && m_sorter is IDisposable)
				(m_sorter as IDisposable).Dispose();
			if (m_filter != null && m_filter is IDisposable)
				(m_filter as IDisposable).Dispose();
			if (m_insertableClasses != null)
				m_insertableClasses.Clear();
			if (m_sortedObjects != null)
				m_sortedObjects.Clear();
		}

		protected override void DisposeUnmanagedResources()
		{
			m_sda = null;
			m_cache = null;
			m_mediator = null;
			m_sorter = null;
			m_filter = null;
			m_owningObject = null;
			m_propertyName = null;
			m_fontName = null;
			m_insertableClasses = null;
			m_sortedObjects = null;
			m_owningObject = null;
		}

		#endregion FwDisposableBase

		#region IVwNotifyChange implementation

		public virtual void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			CheckDisposed();

			if (m_fUpdatingList || m_reloadingList)
				return;	// we're already in the process of changing our list.

			if (TryHandleUpdateOrMarkPendingReload(hvo, tag, ivMin, cvIns, cvDel))
				return;

			// Try to catch things that don't obviously affect us, but will cause problems.
			TryReloadForInvalidPathObjectsOnCurrentObject(tag, cvDel);
		}

		private bool TryReloadForInvalidPathObjectsOnCurrentObject(int tag, int cvDel)
		{
			bool fVirtual = VirtualHandler != null;
			IFwMetaDataCache mdc = m_cache.MetaDataCacheAccessor;
			int fieldType = mdc.GetFieldType((uint)tag);
			if (IsPropOwning(fieldType) && cvDel > 0 && CurrentIndex >= 0)
			{
				// We've deleted an object. Unfortunately we can't find out what object was deleted.
				// Therefore it will be too slow to check every item in our list. Checking out the current one
				// thoroughly prevents many problems (e.g., LT-4880)
				ManyOnePathSortItem item = SortedObjects[CurrentIndex] as ManyOnePathSortItem;
				{
					if (!m_cache.IsValidObject(item.KeyObject))
					{
						ReloadList(fVirtual);
						return true;
					}
					for (int i = 0; i < item.PathLength; i++)
					{
						if (!m_cache.IsValidObject(item.PathObject(i)))
						{
							ReloadList(fVirtual);
							return true;
						}
					}
				}
			}
			return false;
		}

		protected virtual bool TryHandleUpdateOrMarkPendingReload(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			if (tag == m_flid)
			{
				if (m_owningObject != null && m_owningObject.Hvo != hvo)
					return true;		// This PropChanged doesn't really apply to us.
				else
				{
					this.NeedToReloadVirtualProperty = false; // we've modified the property directly, no need to resync.
					ReloadList(ivMin, cvIns, cvDel);
					return true;
				}
			}
			else
			{
				// This may be a change to content we depend upon.

				// 1) If it's a virtual property, see whether it knows that its result depends
				// on the thing that changed.
				IVwVirtualHandler handler;
				if (Cache.TryGetVirtualHandler(m_flid, out handler))
				{
					if (m_owningObject == null || handler.DoesResultDependOnProp((int)m_owningObject.Hvo, hvo, tag, 0))
					{
						if (TryModifyingExistingList(hvo, tag, ivMin, cvIns, cvDel))
						{
							return true;	// we'll try to wait until we get OnDummyRecordList_RequestConversionToReal to handle this.
						}

						// Don't use the three-argument version. We need to force the virtual property to recompute,
						// not to figure the result of inserting one item into the sequence it is based on.
						// That's too hard in general.

						// Enhance JohnT/CurtisH: A potential optimization could be made that would check if this
						// list is actually in use before refreshing it
						ReloadList(true);
						return true;
					}
				}
				// 2) see if the property is the VirtualFlid of the owning clerk. If so,
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
				// 3) Entries depend upon a few different properties.
				if (m_flid == (int)LexDb.LexDbTags.kflidEntries &&
					EntriesDependsUponProp(tag))
				{
					MarkEntriesForReload();
					return true;
				}
			}

			return false;
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
			switch (tag / 1000)
			{
				case (int)LexEntry.kclsidLexEntry:
				case (int)LexSense.kclsidLexSense:
				case (int)MoForm.kclsidMoForm:
					return true;
			}
			return false;
		}

		internal bool ReloadLexEntries
		{
			get { return m_fReloadLexEntries; }
		}

		internal protected virtual bool NeedToReloadList()
		{
			bool fReload = this.RequestedLoadWhileSuppressed;
			if (this.Flid == (int)SIL.FieldWorks.FDO.Ling.LexDb.LexDbTags.kflidEntries)
				fReload |= this.ReloadLexEntries;
			return fReload;
		}


		private static bool IsPropOwning(int fieldType)
		{
			return fieldType == (int)CellarModuleDefns.kcptOwningAtom
							|| fieldType == (int)CellarModuleDefns.kcptOwningCollection
							|| fieldType == (int)CellarModuleDefns.kcptOwningSequence;
		}

		#endregion IVwNotifyChange implementation

		#region ISortItemProvider implementation

		/// <summary>
		/// Get the nth item in the main list.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public ManyOnePathSortItem SortItemAt(int index)
		{
			CheckDisposed();

			if (SortedObjects.Count == 0)
				return null;
			else
				return SortedObjects[index] as ManyOnePathSortItem;
		}

		/// <summary>
		/// get the class of the items in this list.
		/// </summary>
		public int ListItemsClass
		{
			get
			{
				CheckDisposed();
				return (int)Cache.GetDestinationClass((uint)m_flid);
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

			ICmObject obj = CmObject.CreateFromDBObject(m_cache, hvo);
			int start = SortedObjects.Count;
			int result = MakeItemsFor(SortedObjects, obj);
			if (result != 0)
			{
				int[] newItems = new int[result];
				for (int i = 0; i < result; i++)
					newItems[i] = (SortedObjects[i + start] as ManyOnePathSortItem).RootObject.Hvo;
				int rootHvo = m_owningObject.Hvo;
				m_cache.VwCacheDaAccessor.CacheReplace(rootHvo, m_virtualFlid, start, start,
					newItems, newItems.Length);
				m_cache.MainCacheAccessor.PropChanged(null, (int)PropChangeType.kpctNotifyAll,
					rootHvo, m_virtualFlid, start, newItems.Length, 0);
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
		/// Return the 'root' object (the one from the original list, which goes in the fake flid)
		/// at the specified index.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public ICmObject RootObjectAt(int index)
		{
			CheckDisposed();

			return SortItemAt(index).RootObject;
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
			for (int i = 0; i < SortedObjects.Count; i++)
			{
				int hvoItem = RootObjectAt(i).Hvo;
				if (!CmObject.IsValidObject(Cache, hvoItem))
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
					int hvoItem = RootObjectAt(i).Hvo;
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

			OwningObject=  CmObject.CreateFromDBObject(m_cache,hvo);
		}

		public virtual void InitLoad()
		{
			CheckDisposed();

			ComputeInsertableClasses();
			m_currentIndex = -1;
			m_hvoCurrent = 0;

			m_sda = m_cache.MainCacheAccessor;
			m_sda.AddNotification(this);

			ReloadList();
		}

		/// <summary>
		/// Change the sorter...and resort if the list already exists.
		/// </summary>
		/// <param name="sorter"></param>
		public void ChangeSorter(RecordSorter sorter)
		{
			CheckDisposed();

			Sorter = sorter;

			// JohnT: a different sorter may create a quite different set of ManyOnePathSortItems.
			// Optimize: it may be possible to find some cases in which we don't need to reload fully,
			// for example, when reversing the order on the same column.
			if (m_sortedObjects != null)
				ReloadList();
			//			if (m_sortedObjects != null && m_sorter != null)
			//				m_sorter.Sort(/*ref*/ m_sortedObjects);
			//			// Update everything that depends on the list.
			//			SendPropChangedOnListChange();
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
				int cList = m_cache.MainCacheAccessor.get_VecSize(m_owningObject.Hvo, m_flid);
				if (cList == 1)
				{
					// we only have one item in our list, so let's just do a full reload.
					// We don't want to insert completely new items in an obsolete list (Cf. LT-6741,6845).
					ReloadList();
					return;
				}
				if (this is DummyRecordList && (this as DummyRecordList).TryMarkPendingConversionRequest(m_owningObject.Hvo, m_flid, ivMin, cvIns, cvDel))
				{
					return; // anticipate a subsequent pending conversion request.
				}
				int hvoReplaced = m_cache.MainCacheAccessor.get_VecItem(m_owningObject.Hvo, m_flid, ivMin);
				ReplaceListItem(hvoReplaced);
			}
			else if (cvIns == 0 && cvDel == 0 && m_owningObject != null && m_hvoCurrent != 0)
			{
				UpdateListItemName();
			}
			else
			{
				ReloadList();
			}
		}

		protected internal virtual void ReloadList(int newListItemsClass, bool force)
		{
			// let a bulk-edit record list handle this.
		}

		/// <summary>
		/// this should refresh the display of field values for existing items, rather than altering the
		/// size of the list (e.g. updating list item names)
		/// </summary>
		protected void UpdateListItemName()
		{
			int cList = m_cache.MainCacheAccessor.get_VecSize(m_owningObject.Hvo, m_flid);
			if (cList != 0)
			{
				ListChanged(this, new ListChangedEventArgs(this, ListChangedEventArgs.ListChangedActions.UpdateListItemName));
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
		/// replace any matching items in our sort list.
		/// </summary>
		/// <param name="hvoReplaced"></param>
		internal protected void ReplaceListItem(int hvoReplaced)
		{
			bool fUpdatingListOrig = m_fUpdatingList;
			m_fUpdatingList = true;
			try
			{
				int hvoOldCurrentObj = CurrentObject != null ? CurrentObject.Hvo : 0;
				ArrayList newSortItems = new ArrayList();
				ICmObject objReplaced = CmObject.CreateFromDBObject(m_cache, hvoReplaced);
				newSortItems.AddRange(ReplaceListItem(objReplaced, hvoReplaced, true));
				if (newSortItems.Count > 0)
				{
					// in general, when adding new items, we want to try to maintain the previous selected *object*,
					// which may have changed index.  so try to find its new index location.
					int indexOfCurrentObj = CurrentIndex;
					if (hvoOldCurrentObj != 0 && CurrentObject.Hvo != hvoOldCurrentObj)
					{
						int indexOfOldCurrentObj = IndexOf(hvoOldCurrentObj);
						if (indexOfOldCurrentObj >= 0)
							indexOfCurrentObj = indexOfOldCurrentObj;
					}
					SendPropChangedOnListChange(indexOfCurrentObj, SortedObjects, ListChangedEventArgs.ListChangedActions.Normal);
				}
			}
			finally
			{
				m_fUpdatingList = fUpdatingListOrig;
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
			ArrayList newSortItems = new ArrayList();
			// Note: don't check NeedToReloadVirtualProperty here, so we can update the list, even if we need to
			// reload it at a later time. This allows joining/breaking wordforms in the Concordance tools, without
			// necessarily having to reload the entire list. Typically replacements will be with real ids, and those
			// should be stable to add in the new view.
			//if (NeedToReloadVirtualProperty)
			//    return newSortItems;	// we don't need to update the list, if we're planning to reload the whole thing.
			List<int> indicesOfSortItemsToRemove = new List<int>(IndicesOfSortItems(new List<int>(new int[] { hvoToReplace })));
			ArrayList remainingInsertItems = new ArrayList();
			int hvoNewObject = 0;
			if (newObj != null)
			{
				hvoNewObject = newObj.Hvo;
				// we don't want to add new sort items, if we've already added them, but we do want to allow
				// a replacement.
				if (hvoToReplace == hvoNewObject || IndexOfFirstSortItem(new List<int>(new int[] { hvoNewObject })) < 0)
					MakeItemsFor(newSortItems, newObj);
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
				m_sorter.MergeInto(SortedObjects, remainingInsertItems);
			}
			else
			{
				// Add at the end.
				SortedObjects.AddRange(remainingInsertItems);
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
				m_currentIndex = 0;
				m_hvoCurrent = CurrentObject.Hvo;
			}
			else
			{
				// Update m_currentIndex if necessary.  The index should either stay the
				// same (if the added object comes later in the list) or be incremented by
				// one (if the added object comes earlier in the list).
				if (CurrentObject.Hvo != m_hvoCurrent)
				{
					++m_currentIndex;
					Debug.Assert(CurrentObject.Hvo == m_hvoCurrent);
				}
			}
		}

		protected ArrayList MakeSortItemsFor(int[] hvos)
		{
			FdoObjectSet<ICmObject> newObjs = new FdoObjectSet<ICmObject>(m_cache, hvos, false);
			ArrayList newSortedObjects = new ArrayList(newObjs.Count);
			foreach (ICmObject obj in newObjs)
			{
				MakeItemsFor(newSortedObjects, obj);
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
						ReloadList();
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
		///	Reloads the list and also reloads the virtual property that the list is based upon.
		/// </summary>
		/// <param name="fForceReloadVirtualProperty">if true, reloads not just the list, but also the virtual property.</param>
		protected void ReloadList(bool fForceReloadVirtualProperty)
		{
			NeedToReloadVirtualProperty |= fForceReloadVirtualProperty;
			ReloadList();
		}

		private int m_indexToRestoreDuringReload = -1;

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
					m_indexToRestoreDuringReload = CurrentIndex;    // try to restore this index during reload.
					// clear everything for now, including the current index, but don't issue a RecordNavigation.
					SendPropChangedOnListChange(-1, new ArrayList(),
												ListChangedEventArgs.ListChangedActions.SkipRecordNavigation);
				}
				m_requestedLoadWhileSuppressed = true;
				// it's possible that we'll want to reload once we become the main active window (cf. LT-9251)
				if (m_mediator != null)
				{
					Form window = (Form) m_mediator.PropertyTable.GetValue("window");
					if (window != null && FwApp.App != null && window != FwApp.App.ActiveMainWindow)
					{
						// make sure we don't install more than one.
						window.Activated -= new EventHandler(window_Activated);
						window.Activated += new EventHandler(window_Activated);
						m_windowPendingOnActivated = window;
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
						m_currentIndex = -1;
						m_mediator.PropertyTable.SetProperty(Clerk.PersistedIndexProperty, m_indexToRestoreDuringReload, PropertyTable.SettingsGroup.LocalSettings);
						m_indexToRestoreDuringReload = -1;
					}
					Clerk.UpdateHelper.ClearBrowseListUntilReload = false;
				}
				m_reloadingList = true;
				int newCurrentIndex = m_currentIndex;
				ArrayList newSortedObjects = null;
				ListChangedEventArgs.ListChangedActions actions;
				try
				{
					// Get the HVO of the current object (but only if it hasn't been deleted).
					// If it has, don't modify m_currentIndex, as the old position is less likely to
					// move the list than going to the top.
					// (We want to keep the current OBJECT, not index, if it's still a real object,
					// because the change that produced the regenerate might be a change of filter or sort
					// that moves it a lot. But if the change is an object deletion, we want to not change
					// the position if we can help it.)
					int hvoCurrent = 0;
					if (m_sortedObjects != null && m_currentIndex != -1 && m_sortedObjects.Count > m_currentIndex
						&& CurrentObject.IsValidObject())
					{
						hvoCurrent = CurrentObject.Hvo;
					}

					// We want to do this AFTER the IsValidObject test, otherwise, IsValidObject will reload
					// the class, owner, and owning flid of every object of that type! (NOT true any longer)
					m_cache.EnableBulkLoadingIfPossible(true);
					//this happens when the set is dependent on another one, but no item is selected in the
					//primary list. For example, if there are no word forms, then the list which holds the analyses
					//of a selected wordform will not have been owning object from which to pull analyses.
					if (m_owningObject == null)
					{
						SortedObjects = new ArrayList(0);
						// We should not do SendPropChangedOnListChange, because that caches a property
						// on m_owningObject, and issues various notifications based on its existence.
						// Nothing should be displaying the list if there is no root object.
						// However, as a safety precaution in case we previously had one, let's fix the
						// current object information.
						m_currentIndex = -1;
						m_hvoCurrent = 0;
						// we still need to broadcast the list changed to update dependent views. (LT-5987)
						if (ListChanged != null)
							ListChanged(this, new ListChangedEventArgs(this, ListChangedEventArgs.ListChangedActions.Normal));
						return;
					}

					// YiSpeed 6 seconds on startup, 10 seconds afterwards!
					//review: eventually, we need to decide between storing a vector of hvos into storing a vector of objects.
					//it may be that storing a vector of all objects (e.g. all lexical entries) is not feasible.
					newSortedObjects = new ArrayList();
					if (m_filter != null)
						m_filter.Preload();
					// Preload the sorter (if any) only if we do NOT have a filter.
					// If we also have a filter, it's pretty well certain currently that it already did
					// the preloading. If we ever have a filter and sorter that want to preload different
					// data, we'll need to refactor so we can determine this, because we don't want to do it twice!
					else if (m_sorter != null)
						m_sorter.Preload();

					foreach (ICmObject obj in GetObjectSet())
					{
						MakeItemsFor(newSortedObjects, obj);
					}

					SortList(newSortedObjects);

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

						actions = ListChangedEventArgs.ListChangedActions.Normal; // We definitely changed records
					}
				}
				finally
				{
					// This MUST be done, even if a return is executed earlier or an exception is thrown, or
					// subsequent calls will never do anything.
					m_cache.EnableBulkLoadingIfPossible(false);
				}
				SendPropChangedOnListChange(newCurrentIndex, newSortedObjects, actions); //YiSpeed 6.5 secs (mostly filling tree bar)
				FinishedReloadList();
			}
			finally
			{
				m_reloadingList = false;
			}
		}

		/// <summary>
		/// we want to keep a reference to our window, separate from the PropertyTable
		/// in case there's a race condition during dispose with the window
		/// getting removed from the PropertyTable before it's been disposed.
		/// </summary>
		Form m_windowPendingOnActivated = null;
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

		protected void SortList(ArrayList newSortedObjects)
		{
			if (m_sorter != null && !ListAlreadySorted)
				m_sorter.Sort(/*ref*/ newSortedObjects);//YiSpeed 1 secs
		}

		protected int MakeItemsFor(ArrayList sortedObjects, ICmObject obj)
		{
//			if (!obj.IsValidObject())
//				throw new Exception("GetObjectSet returned an invalid object with HVO " + obj.Hvo);
			int start = sortedObjects.Count;
			if (m_sorter == null)
				sortedObjects.Add(new ManyOnePathSortItem(obj));
			else
				m_sorter.CollectItems(obj, sortedObjects);
			if (m_filter != null && m_fEnableFilters)
			{
				for (int i = start; i < sortedObjects.Count; )
				{
					if (m_filter.Accept(sortedObjects[i] as ManyOnePathSortItem))
						i++; // advance loop if we don't delete!
					else
					{
						//ManyOnePathSortItem hasBeen = (ManyOnePathSortItem)sortedObjects[i];
						sortedObjects.RemoveAt(i);
						//hasBeen.Dispose();
					}
				}
			}
//			for (int i = start; i < sortedObjects.Count; i++)
//			{
//				ManyOnePathSortItem item = sortedObjects[i] as ManyOnePathSortItem;
//				item.AssertValid();
//				if (!item.KeyCmObjectUsing(m_cache).IsValidObject())
//					throw new Exception("ManyOnePathSortItem has an invalid key object with HVO " + item.KeyObject);
//			}
			return sortedObjects.Count - start;
		}

		protected void SendPropChangedOnListChange(int newCurrentIndex, ArrayList newSortedObjects, ListChangedEventArgs.ListChangedActions actions)
		{
			//Populate the virtual cache property which will hold this set of hvos, in this order.
			int[] hvos = new int[newSortedObjects.Count];
			int i = 0;
			foreach(ManyOnePathSortItem item in newSortedObjects)
				hvos[i++] = item.RootObject.Hvo;
			// In case we're already displaying it, we must have an accurate old length, or if it gets shorter
			// the extra ones won't get deleted. But we must check whether the property is already cached...
			// if it isn't and we try to read it, the code will try to load this fake property from the database,
			// with unfortunate results. (It's not exactly a reference sequence, but any sequence type will do here.)


			int rootHvo = m_owningObject.Hvo;

			int oldLength = 0;
			if (m_cache.MainCacheAccessor.get_IsPropInCache(rootHvo, m_virtualFlid,
				(int)CellarModuleDefns.kcptReferenceSequence, 0))
			{
				oldLength = m_cache.MainCacheAccessor.get_VecSize(rootHvo, m_virtualFlid);
			}
			else
			{
				// on a general refresh operation all the caches get cleared before anything else
				// happens. So, use the old length that we stored earlier (cf. LT-2075).
				oldLength = m_oldLength;
			}

			if (AboutToReload != null)
				AboutToReload(this, new EventArgs());
			// Must not actually change anything before we do AboutToReload! Then do it all at once
			// before we make any notifications...we want the cache value to always be consistent
			// with m_sortedObjects, and m_currentIndex always in range
			SortedObjects = newSortedObjects;
			// if we haven't already set an index, see if we can restore one from the property table.
			if (SortedObjects.Count > 0 && (newCurrentIndex == -1 || m_hvoCurrent == 0))
				newCurrentIndex = m_mediator.PropertyTable.GetIntProperty(Clerk.PersistedIndexProperty, 0, PropertyTable.SettingsGroup.LocalSettings);
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
			m_cache.VwCacheDaAccessor.CacheVecProp(rootHvo, m_virtualFlid, hvos, hvos.Length);
			m_cache.MainCacheAccessor.PropChanged(this, (int)PropChangeType.kpctNotifyAllButMe,
				rootHvo,
				m_virtualFlid,
				0, // Start at the beginning of the vector
				hvos.Length, // Pretend like we inserted and deleted them all,
				oldLength); // since we kind if did it that way.

			m_oldLength = hvos.Length;

			//TODO: try to stay on the same record
			/// Currently Attempts to keep us at the same index as we were.
			/// we should try hard to keep us on the actual record that we were currently on,
			/// since the reload may be a result of changing the sort order, in which case the index is meaningless.

			//make sure the hvo index is in a reasonable spot
			if (m_currentIndex >= hvos.Length)
				CurrentIndex = hvos.Length -1;
			if (m_currentIndex < 0)
				CurrentIndex = (hvos.Length>0)? 0: -1;
			if (DoneReload != null)
				DoneReload(this, new EventArgs());

			// Notify any delegates that the selection of the main object in the vector has changed.
			if (ListChanged != null && m_fEnableSendPropChanged)
				ListChanged(this, new ListChangedEventArgs(this, actions));
		}

		/// <summary>
		/// Handle adding and/or removing a filter.
		/// </summary>
		/// <param name="args"></param>
		public void OnChangeFilter(FilterChangeEventArgs args)
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
					AndFilter af = new AndFilter();
					af.Add(m_filter);
					af.Add(args.Added);
					m_filter = af;
				}
			}
			// Now we have a new filter, we have to recompute what to show.
			ReloadList();
		}

		/// <summary>
		/// indicate whether or not we need to reload the virtual property that our RecordList is based upon.
		/// returns false if it's not based on a virtual property.
		/// Since the virtual property can last longer than the clerk/list, its value should be in the property table.
		/// </summary>
		public virtual bool NeedToReloadVirtualProperty
		{
			get
			{
				IVwVirtualHandler handler;
				if (Cache.TryGetVirtualHandler(m_flid, out handler))
				{
					// if the virtual handler is already set up to compute every time, we do not have to
					// worry about forcing it to reload, because it will do that automatically when we try to get value(s)
					// from the property.
					if (handler.ComputeEveryTime)
					{
						return false;
					}
					else if (AlwaysRecomputeVirtualOnReloadList())
					{
						return true;
					}
					else if (m_mediator.PropertyTable.GetBoolProperty(m_clerk.Id + "_NeedToReloadVirtualProperty", false, PropertyTable.SettingsGroup.LocalSettings))
					{
						return true;
					}
					else
					{
						// see if we are using a special handler that can determine whether or not it needs to reload.
						FDOSequencePropertyTableVirtualHandler pvh = handler as FDOSequencePropertyTableVirtualHandler;
						return pvh != null && !pvh.IsUpToDate;
					}
				}
				return false;	// not a virtual property.
			}
			set
			{
				m_mediator.PropertyTable.SetProperty(m_clerk.Id + "_NeedToReloadVirtualProperty", value, false, PropertyTable.SettingsGroup.LocalSettings);
				m_mediator.PropertyTable.SetPropertyPersistence(m_clerk.Id + "_NeedToReloadVirtualProperty", false);
			}
		}

		private bool AlwaysRecomputeVirtualOnReloadList()
		{
			return m_mediator.PropertyTable.GetBoolProperty(m_clerk.Id + "_AlwaysRecomputeVirtualOnReloadList", false);
		}

		private IAdvInd4 m_progAdvInd = null;
		/// <summary>
		/// Get/set a progress reporter (encapsulated in an IAdvInd4 interface).
		/// </summary>
		public IAdvInd4 ProgressReporter
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

		protected virtual FdoObjectSet<ICmObject> GetObjectSet()
		{
			IVwVirtualHandler handler = (m_cache.MainCacheAccessor as IVwCacheDa).GetVirtualHandlerId(m_flid);
			if (handler != null)
			{
				int[] items = new int[0];
				try
				{
					m_cache.EnableBulkLoadingIfPossible(true);
					// Original code won't work for virtual property (or anything else that isn't owning), so do
					// this instead.
					// Force the handler to (re)load the property.
					if (NeedToReloadVirtualProperty)
					{
						ForceReloadVirtualProperty(handler);
						NeedToReloadVirtualProperty = false;
					}
					items = m_cache.GetVectorProperty(m_owningObject.Hvo, m_flid, true, m_progAdvInd);
				}
				finally
				{
					m_cache.EnableBulkLoadingIfPossible(false);
				}
				// Review JohnH(JohnT): I'm not sure this constructor is meant to be used like this, but
				// in general we can't be sure the objects are all of the same class, which rules out
				// all other constructors that don't obtain the HVOs directly from SQL.
				return new FdoObjectSet<ICmObject>(m_cache, items, false);
			}
			string sqlQuery = string.Format("select Id, Class$ from CmObject"
				+ " where Owner$={0} and OwnFlid$={1}"
				//++++ Could put filter in here
				//(EricP) Is this " order by Class$" still necessary? We might be able to
				// get away with simply using EnableBulkLoadingIfPossible, as long as the
				// code that was loading these objects according to class blocks is also not needed.
				// If m_flid is a sequence, we could just " order by OwnOrd" so we could cache the owningFlid results
				// That could improve performance for the next time anyone needs to read the property from the cache.
				// For that matter, would (re)loading the property through FDO be significantly slower than through DbOps?
				+ " order by Class$",	// can't mess with this sort unless homogenous collection
				m_owningObject.Hvo, m_flid);

			//System.Windows.Forms.MessageBox.Show(null, sqlQuery, "Debug");

			//note the performance enhancement here: don't actually load the fdo object properties:
			//			return new FdoObjectSet(m_cache, sqlQuery, false, true); //LoadAllOfType: if true, will filter still work???

			int[] hvos = DbOps.ReadIntArrayFromCommand(m_cache, sqlQuery, null);
//			ManyOnePathSortItem.AssertValidHvoArray(hvos);
			return new FdoObjectSet<ICmObject>(m_cache, hvos, false); //LoadAllOfType: if true, will filter still work???
		}

		protected virtual void ForceReloadVirtualProperty(IVwVirtualHandler handler)
		{
			handler.Load(m_owningObject.Hvo, m_flid, 0, m_cache.MainCacheAccessor as IVwCacheDa);
		}

		public bool CanInsertClass(string className)
		{
			CheckDisposed();

			return (GetMatchingClass(className)!= null);
		}

		/// <summary>
		/// Create and Insert an item in a list. If this is a hierarchical list, then insert it at the same level
		/// as the current object.
		/// </summary>
		/// <param name="cpi"></param>
		/// <returns></returns>
		protected int DoCreateAndInsert(int hvoOwner, List<ClassAndPropInfo> cpiPath)
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
			ClassAndPropInfo cpiFinal = cpiPath[cpiPath.Count - 1];
			m_cache.BeginUndoTask(String.Format(xWorksStrings.UndoAdd0To1, cpiFinal.signatureClassName, cpiFinal.fieldName),
				String.Format(xWorksStrings.RedoAdd0To1, cpiFinal.signatureClassName, cpiFinal.fieldName));
			// assume we need to insert a new object in the vector field following hvoOwner
			ClassAndPropInfo cpi = cpiPath[0];
			int flid = (int)cpi.flid;
			int insertPosition = 0;
			if (CurrentObject != null && cpiPath.Count == 1)
			{
				hvoOwner = CurrentObject.OwnerHVO;
				flid = CurrentObject.OwningFlid;
				insertPosition = CurrentObject.IndexInOwner + 1;
				//insertPosition = m_cache.GetVectorSize(hvoOwner,flid);
			}
			else
			{
				// Just possible it's some kind of virtual we can't insert a new object into. Check.
				FieldType cpt = m_cache.GetFieldType(flid);
				if (cpt != FieldType.kcptOwningCollection && cpt != FieldType.kcptOwningSequence)
					return 0;
			}
			ISilDataAccess sda = m_cache.MainCacheAccessor;
			// This is used below for a check. Note also that it just might make things work better to
			// force the property into the cache at this stage, which otherwise isn't necessarily done
			// if it is empty and there is no current object.
			int chvoCheck1 = sda.get_VecSize(hvoOwner, flid);
			int hvoNew = m_cache.CreateObject((int)(cpi.signatureClsid), hvoOwner, flid, insertPosition);
			if (sda.get_VecItem(hvoOwner, flid, insertPosition) != hvoNew)
			{
				// Must be an owning collection...we need to know where it was actually inserted.
				// Currently, index 0 is typicaly, which is nice becase the loop only does one iteration.
				int chvo = insertPosition; // was read from old vector size
				for (insertPosition = 0;
					insertPosition < chvo && sda.get_VecItem(hvoOwner, flid, insertPosition) != hvoNew;
					insertPosition++)
				{}
			}
			// we may need to insert another new class.
			if (cpiPath.Count > 1)
			{
				// assume this is an atomic property.
				ClassAndPropInfo cpiLevel2 = cpiPath[1];
				hvoOwner = hvoNew;
				flid = (int)cpiLevel2.flid;
				insertPosition = 0;
				hvoNew = m_cache.CreateObject((int)(cpiLevel2.signatureClsid), hvoOwner, flid , 0);
			}
			m_cache.EndUndoTask();
			ICmObject newObj = CmObject.CreateFromDBObject(m_cache, hvoNew);
			if (cpiPath.Count == 1)
			{
				// This has failed mysteriously. In case it happens again, make a very informative message.
				int chvoCheck = sda.get_VecSize(hvoOwner, flid);
				if (chvoCheck1 != chvoCheck - 1)
				{
					throw new Exception("inserting failed: sequence used to have " + chvoCheck1 +
						", but after inserting one it had " + chvoCheck + " for object "
						+ hvoOwner + " property " + flid);
				}
				if (insertPosition >= chvoCheck)
				{
					throw new Exception("insert position " + insertPosition + " is not valid for object "
						+ hvoOwner + " property " + flid + " which has only " + chvoCheck + " items.");
				}
			}
			try
			{
				m_cache.MainCacheAccessor.PropChanged(null,
					1, /* kpctNotifyAll */
					hvoOwner, flid, insertPosition, 1, 0);
			}
			catch (Exception e)
			{
				throw new Exception("PropChanged on insert object with hvoOwner = " + hvoOwner
					+ " flid = " + flid + " insertPosition = " + insertPosition, e);
			}

			return hvoNew;
		}

		/// <summary>
		/// Create an object of the specified class
		/// </summary>
		/// <param name="className"></param>
		/// <returns>true if successful (the class is known)</returns>
		public bool CreateAndInsert(string className )
		{
			CheckDisposed();

			ClassAndPropInfo cpi = GetMatchingClass(className);
			Debug.Assert(cpi != null, "This object should not have been asked to insert an object of the class "+ className +".");
			if (cpi != null)
			{
				// check to see if we're wanting to insert into an owning relationship via a virtual property.
				BaseFDOPropertyVirtualHandler vh = Cache.VwCacheDaAccessor.GetVirtualHandlerId(m_flid) as BaseFDOPropertyVirtualHandler;
				List<ClassAndPropInfo> cpiPath;
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
				int hvoNew = 0;
				using (new RecordClerk.ListUpdateHelper(Clerk))
				{
					hvoNew = DoCreateAndInsert(m_owningObject.Hvo, cpiPath);
				}
				CurrentIndex = IndexOf(hvoNew);
				return hvoNew != 0; // If we get zero, we couldn't do it for some reason.
			}
			return false;
		}

		/// <summary>
		/// get the index of the given hvo, where it occurs as the root object
		/// (of a ManyOnePathSortItem) in m_sortedObjects.
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
		// one of the ManyOnePathSortItems in the given list.
		protected int IndexOf(ArrayList objects, int hvo)
		{
			int i = 0;
			if (objects != null && hvo != 0)
			{
				foreach (ManyOnePathSortItem item in objects)
				{
					if (item.RootObject.Hvo == hvo)
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
		/// <param name="hvo"></param>
		/// <returns>-1 if the object is not in the list</returns>
		public int IndexOfChildOf(int hvoTarget, FdoCache cache)
		{
			CheckDisposed();

			int i = 0;
			foreach (ManyOnePathSortItem item in SortedObjects)
			{
				ICmObject orange = item.RootObject;
				for (int hvoOwner = cache.GetOwnerOfObject(orange.Hvo);
					hvoOwner != 0;
					hvoOwner = cache.GetOwnerOfObject(hvoOwner))
				{
					if (hvoOwner == hvoTarget)
						return i;
				}
				++i;
			}
			return -1;
		}

		/// <summary>
		///
		/// </summary>
		/// <remarks> notice that currently, we do not require (or allow)
		/// the class owning object to be specified. It is inferred.
		/// Also notice that we assume that all of these collections are owned by,
		/// essentially, a Singleton object in the database.  For example,
		/// there is only one lexical database, so we do not need nor have a need for a way to
		/// specify which lexical database we want to browse.</remarks>
		/// <remarks> The initial plan was to do something smarter, so that we would not have this
		/// big switch statement.  There are various possibilities, but this is our first pass
		/// in order to get something working.</remarks>
		/// <param name="name">the name of the vector, as defined here</param>
		/// <param name="owner">the name of the owner of the vector (can be null if using a
		/// defined type)</param>
		/// <param name="analysisWs">True to use the analysis font, false otherwise</param>
		/// <param name="owningObject">the object which owns the vector</param>
		/// <param name="fontName"></param>
		/// <returns>The real flid of the vector in the database.</returns>
		static public int GetFlidOfVectorFromName(string name, string owner, bool analysisWs,
			FdoCache cache, Mediator mediator, out ICmObject owningObject, out string fontName,
			out int typeSize)
		{
			// Many of these are vernacular, but if not,
			// they should set it to the default anal font by using the "analysisWs" property.
			GetDefaultFontNameAndSize(analysisWs, cache, mediator, out fontName, out typeSize);

			return GetFlidOfVectorFromName(name, owner, cache, mediator, out owningObject, ref fontName, ref typeSize);
		}

		internal static int GetFlidOfVectorFromName(string propertyName, string owner, FdoCache cache, Mediator mediator, out ICmObject owningObject)
		{
			string fontName = "";
			int typeSize = 0;
			return GetFlidOfVectorFromName(propertyName, owner, cache, mediator, out owningObject, ref fontName, ref typeSize);
		}

		internal static int GetFlidOfVectorFromName(string name, string owner, FdoCache cache, Mediator mediator, out ICmObject owningObject, ref string fontName, ref int typeSize)
		{
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
								owningObject = cache.LangProject;
								break;
							case "LexDb":
								owningObject = cache.LangProject.LexDbOA;
								break;
							case "RnResearchNbk":
								owningObject = cache.LangProject.ResearchNotebookOA;
								break;
							case "WordformInventory":
								owningObject = cache.LangProject.WordformInventoryOA;
								break;
							case "DsDiscourseData":
								owningObject = cache.LangProject.DiscourseDataOA;
								break;
							default:
								Debug.Assert(false, "Illegal owner specified for possibility list.");
								break;
						}

						IVwVirtualHandler vh = cache.VwCacheDaAccessor.GetVirtualHandlerName(owner, name);
						if (vh != null)
						{
							// get the flid for the virtual handler.
							realFlid = vh.Tag;
						}
						else
						{
							// real fields will have a FdoVector collection format
							FdoVector<ICmObject> collection = (FdoVector<ICmObject>)owningObject.GetType().InvokeMember(name,
								BindingFlags.Public | BindingFlags.NonPublic |
								BindingFlags.Instance | BindingFlags.GetProperty, null, owningObject, null);
							realFlid = collection.Flid;
						}
					}
					catch
					{
						throw new ConfigurationException("The field '" + name + "' with owner '" +
							owner + "' has not been implemented in the switch statement " +
							"in RecordList.GetVectorFromName().");
					}
					break;

				// Other supported stuff
				case "Entries":
					if (owner == "ReversalIndex")
					{
						// The null objects shouldn't happen, but LTB-846 indicates one did.
						if (cache.LangProject.LexDbOA.ReversalIndexesOC == null ||
							cache.LangProject.LexDbOA.ReversalIndexesOC.HvoArray == null ||
							cache.LangProject.LexDbOA.ReversalIndexesOC.HvoArray.Length == 0)
						{
							// invoke the Reversal listener to create the index for this invalid state
							Mediator msgMediator = (FwXApp.App != null ? ((XCore.XWindow)(FwXApp.App.ActiveMainWindow)).Mediator : null);
							msgMediator.SendMessage("InsertReversalIndex_FORCE", null);
						}

						if (cache.LangProject.LexDbOA.ReversalIndexesOC.HvoArray.Length > 0)
						{
							int hvo = cache.LangProject.LexDbOA.ReversalIndexesOC.HvoArray[0];
							owningObject = ReversalIndex.CreateFromDBObject(cache, hvo);
							realFlid = (int)ReversalIndex.ReversalIndexTags.kflidEntries;
						}
					}
					else
					{
						owningObject = cache.LangProject.LexDbOA;
						realFlid = (int)LexDb.LexDbTags.kflidEntries;
					}
					break;
				case "Scripture_0_0_Content": // StText that is contents of first section of first book.
					try
					{
						IScrBook sb = cache.LangProject.TranslatedScriptureOA.ScriptureBooksOS[0];
						IScrSection ss = sb.SectionsOS[0];
						owningObject = ss.ContentOA;
					}
					catch (NullReferenceException)
					{
						Trace.Fail("Could not get the test Scripture object.  Your language project might not have one (or their could have been some other error).  Try TestLangProj.");
						throw;
					}
					realFlid = (int)StText.StTextTags.kflidParagraphs;
					break;
				case "Texts":
					owningObject = cache.LangProject;
					realFlid = (int)LangProject.LangProjectTags.kflidTexts;
					break;
				case "MsFeatureSystem":
					owningObject = cache.LangProject;
					realFlid = (int)LangProject.LangProjectTags.kflidMsFeatureSystem;
					break;

				// phonology
				case "Phonemes":
					owningObject = cache.LangProject.PhonologicalDataOA.PhonemeSetsOS[0];
					realFlid = (int)PhPhonemeSet.PhPhonemeSetTags.kflidPhonemes;
					break;
				case "BoundaryMarkers":
					owningObject = cache.LangProject.PhonologicalDataOA.PhonemeSetsOS[0];
					realFlid = (int)PhPhonemeSet.PhPhonemeSetTags.kflidBoundaryMarkers;
					break;
				case "Environments":
					owningObject = cache.LangProject.PhonologicalDataOA;
					realFlid = (int)PhPhonData.PhPhonDataTags.kflidEnvironments;
					break;
				case "NaturalClasses":
					owningObject = cache.LangProject.PhonologicalDataOA;
					realFlid = (int)PhPhonData.PhPhonDataTags.kflidNaturalClasses;
					fontName = cache.LangProject.DefaultAnalysisWritingSystemFont;
					typeSize = GetFontHeightFromStylesheet(cache, mediator, true);
					break;

				case "PhonologicalFeatures":
					owningObject = cache.LangProject.PhFeatureSystemOA;
					realFlid = (int)FsFeatureSystem.FsFeatureSystemTags.kflidFeatures;
					fontName = cache.LangProject.DefaultAnalysisWritingSystemFont;
					typeSize = GetFontHeightFromStylesheet(cache, mediator, true);
					break;

				case "PhonologicalRules":
					owningObject = cache.LangProject.PhonologicalDataOA;
					realFlid = (int)PhPhonData.PhPhonDataTags.kflidPhonRules;
					break;

				// morphology
				case "AdhocCoprohibitions":
					owningObject = cache.LangProject.MorphologicalDataOA;
					realFlid = (int)MoMorphData.MoMorphDataTags.kflidAdhocCoProhibitions;
					fontName = cache.LangProject.DefaultAnalysisWritingSystemFont;
					typeSize = GetFontHeightFromStylesheet(cache, mediator, true);
					break;
				case "CompoundRules":
					owningObject = cache.LangProject.MorphologicalDataOA;
					realFlid = (int)MoMorphData.MoMorphDataTags.kflidCompoundRules;
					fontName = cache.LangProject.DefaultAnalysisWritingSystemFont;
					typeSize = GetFontHeightFromStylesheet(cache, mediator, true);
					break;

				case "Features":
					owningObject = cache.LangProject.MsFeatureSystemOA;
					realFlid = (int)FsFeatureSystem.FsFeatureSystemTags.kflidFeatures;
					fontName = cache.LangProject.DefaultAnalysisWritingSystemFont;
					typeSize = GetFontHeightFromStylesheet(cache, mediator, true);
					break;

				case "FeatureTypes":
					owningObject = cache.LangProject.MsFeatureSystemOA;
					realFlid = (int)FsFeatureSystem.FsFeatureSystemTags.kflidTypes;
					fontName = cache.LangProject.DefaultAnalysisWritingSystemFont;
					typeSize = GetFontHeightFromStylesheet(cache, mediator, true);
					break;

				case "ProdRestrict":
					owningObject = cache.LangProject.MorphologicalDataOA;
					realFlid = (int)MoMorphData.MoMorphDataTags.kflidProdRestrict;
					fontName = cache.LangProject.DefaultAnalysisWritingSystemFont;
					typeSize = GetFontHeightFromStylesheet(cache, mediator, true);
					break;

				case "Problems":
					owningObject = cache.LangProject;
					realFlid = (int)LangProject.LangProjectTags.kflidAnnotations;
					fontName = cache.LangProject.DefaultAnalysisWritingSystemFont;
					typeSize = GetFontHeightFromStylesheet(cache, mediator, true);
					break;
				case "Wordforms":
					owningObject = cache.LangProject.WordformInventoryOA;
					realFlid = (int)SIL.FieldWorks.FDO.Ling.WordformInventory.WordformInventoryTags.kflidWordforms;
					break;

				case "ConcordanceWords":
					owningObject = cache.LangProject.WordformInventoryOA;
					realFlid = WordformInventory.ConcordanceWordformsFlid(cache);
					break;

				case "MatchingConcordanceItems":
					owningObject = cache.LangProject.WordformInventoryOA;
					realFlid = WordformInventory.MatchingConcordanceItemsFlid(cache);
					break;

				case "ReversalIndexes":
					{
						owningObject = cache.LangProject.LexDbOA;
						realFlid = BaseVirtualHandler.GetInstalledHandlerTag(cache, "LexDb", "CurrentReversalIndices");
						break;
					}

				//dependent properties
				case "Analyses":
					{
						IWordformInventory wfi = cache.LangProject.WordformInventoryOA;
						if (wfi != null)
						{
							//TODO: HACK! making it show the first one.
							if (wfi.WordformsOC.Count > 0)
								owningObject = CmObject.CreateFromDBObject(cache, wfi.WordformsOC.HvoArray[0]);
						}
						realFlid = (int)WfiWordform.WfiWordformTags.kflidAnalyses;
						break;
					}
				case "SemanticDomainList":
					owningObject = cache.LangProject.SemanticDomainListOA;
					realFlid = (int)SIL.FieldWorks.FDO.Cellar.CmPossibilityList.CmPossibilityListTags.kflidPossibilities;
					break;
				case "AllSenses":
					{
						owningObject = cache.LangProject.LexDbOA;
						realFlid = BaseVirtualHandler.GetInstalledHandlerTag(cache, "LexDb", "AllSenses");
						// Todo: something about initial sorting...
						break;
					}
				case "AllExampleSentenceTargets":
					{
						owningObject = cache.LangProject.LexDbOA;
						realFlid = BaseVirtualHandler.GetInstalledHandlerTag(cache, "LexDb", "AllExampleSentenceTargets");
						// Todo: something about initial sorting...
						break;
					}
				case "AllPossiblePronunciations":
					{
						owningObject = cache.LangProject.LexDbOA;
						realFlid = BaseVirtualHandler.GetInstalledHandlerTag(cache, "LexDb", "AllPossiblePronunciations");
						// Todo: something about initial sorting...
						break;
					}
				case "AllEntryRefs":
					{
						owningObject = cache.LangProject.LexDbOA;
						realFlid = BaseVirtualHandler.GetInstalledHandlerTag(cache, "LexDb", "AllEntryRefs");
						// Todo: something about initial sorting...
						break;
					}
				case "AllPossibleAllomorphs":
					{
						owningObject = cache.LangProject.LexDbOA;
						realFlid = BaseVirtualHandler.GetInstalledHandlerTag(cache, "LexDb", "AllPossibleAllomorphs");
						// Todo: something about initial sorting...
						break;
					}
				case "AllAllomorphsList":
					{
						owningObject = cache.LangProject.LexDbOA;
						realFlid = BaseVirtualHandler.GetInstalledHandlerTag(cache, "LexDb", name);
						// Todo: something about initial sorting...
						break;
					}
				case "AllMLAnalysesClientIDs": // Fall through.
				case "AllAnalysesClientIDs": // Fall through.
				case "AllWordformClientIDs": // Fall through.
				case "AllSentenceClientIDs": // Fall through.
				case "AllCompoundRuleClientIDs": // Fall through.
				case "AllEntryClientIDs": // Fall through.
				case "AllSenseClientIDs": // Fall through.
				case "AllMSAClientIDs":
					{
						switch (owner)
						{
							case "PartOfSpeech":
								owningObject = cache.LangProject.PartsOfSpeechOA.PossibilitiesOS[0];
								break;
							case "LexSense":
								int senseId;
								DbOps.ReadOneIntFromCommand(cache, "SELECT TOP 1 Id FROM LexSense", null, out senseId);
								if (senseId != 0)	// Can happen with Concorder plug-in.  See LT-7960.
									owningObject = LexSense.CreateFromDBObject(cache, senseId);
								break;
						}
						realFlid = BaseVirtualHandler.GetInstalledHandlerTag(cache, owner, name);
						// Todo: something about initial sorting...
						break;
					}
			}

			return realFlid;
		}

		internal static void GetDefaultFontNameAndSize(bool analysisWs, FdoCache cache, Mediator mediator, out string fontName, out int typeSize)
		{
			fontName = (analysisWs ? cache.LangProject.DefaultAnalysisWritingSystemFont :
						 cache.LangProject.DefaultVernacularWritingSystemFont);
			typeSize = GetFontHeightFromStylesheet(cache, mediator, analysisWs);
		}

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

		/// <summary>
		/// Adjust the name of the menu item if appropriate. PossibilityRecordList overrides.
		/// </summary>
		/// <param name="command"></param>
		/// <param name="display"></param>
		public virtual void AdjustInsertCommandName(Command command, UIItemDisplayProperties display)
		{
			CheckDisposed();

		}

		protected void ComputeInsertableClasses()
		{
			m_insertableClasses = new List<ClassAndPropInfo>();
			if (OwningObject != null && OwningObject is ICmPossibilityList &&
				(OwningObject as ICmPossibilityList).IsClosed)
			{
				// You can't insert anything in a closed list!
				return;
			}
			m_cache.AddClassesForField((uint)m_flid, true, m_insertableClasses);
		}

		/// <summary>
		/// Delete the object without reporting progress.
		/// </summary>
		public void DeleteCurrentObject()
		{
			CheckDisposed();

			DeleteCurrentObject(new NullProgressState());
		}

		/// <summary>
		/// Delete the current object, reporting progress as far as possible.
		/// </summary>
		/// <param name="state"></param>
		public virtual void DeleteCurrentObject(ProgressState state)
		{
			CheckDisposed();

			try
			{
				m_deletingObject = true;
				int currentIndex = CurrentIndex;
				FdoCache cache = m_cache;
				ICmObject currentObject = CurrentObject;
				// This looks plausible; but for example IndexOf may reload the list, if a reload is pending;
				// and the current object may no longer match the current filter, so it may be gone.
				//Debug.Assert(currentIndex == IndexOf(currentObject.Hvo));
				string className = currentObject.GetType().Name;
				cache.BeginUndoTask(String.Format(xWorksStrings.UndoDelete0, className),
					String.Format(xWorksStrings.RedoDelete0, className));
				using (new RecordClerk.ListUpdateHelper(Clerk))
				{
					bool m_fUpdatingListOrig = m_fUpdatingList;
					m_fUpdatingList = true;
					try
					{
						RemoveItemsFor(CurrentObject.Hvo);
						currentObject.DeleteUnderlyingObject(state);
					}
					finally
					{
						m_fUpdatingList = m_fUpdatingListOrig;
					}
				}
				cache.EndUndoTask();
			}
			finally
			{
				m_deletingObject = false;
			}
		}
	}
}
