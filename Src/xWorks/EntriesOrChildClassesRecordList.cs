// Copyright (c) 2004-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.FDO.DomainImpl;
using SIL.FieldWorks.FDO.DomainServices;

namespace SIL.FieldWorks.XWorks
{
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
		int m_flidEntries;
		int m_prevFlid;
		readonly IDictionary<int, bool> m_reloadNeededForProperty = new Dictionary<int, bool>();
		PartOwnershipTree m_pot;
		// suspend loading the property until given a class by RecordBrowseView via
		// RecordClerk.OnChangeListItemsClass();
		bool m_suspendReloadUntilOnChangeListItemsClass = true;
		private XElement m_partOwnershipTreeSpec = new XElement("PartOwnershipTree",
			/* the ClassOwnershipTree describes the relative relationship between the target classes in the possible source properties
			   loaded by this list. This especially helps in maintaining the CurrentIndex when switching from one property to the next. */
					new XElement("ClassOwnershipTree",
						new XElement("LexEntry", new XAttribute("sourceField", "Entries"),
							new XElement("LexEntryRef", new XAttribute("sourceField", "AllEntryRefs"), new XAttribute("altSourceField", "ComplexEntryTypes:AllComplexEntryRefPropertyTargets;VariantEntryTypes:AllVariantEntryRefPropertyTargets")),
							new XElement("LexPronunciation", new XAttribute("sourceField", "AllPossiblePronunciations")),
							new XElement("MoForm", new XAttribute("sourceField", "AllPossibleAllomorphs")),
							new XElement("LexSense", new XAttribute("sourceField", "AllSenses"),
								new XElement("LexExampleSentence", new XAttribute("sourceField", "AllExampleSentenceTargets"),
									new XElement("CmTranslation", new XAttribute("sourceField", "AllExampleTranslationTargets")))))),
					/* ParentClassPathsToChildren describes how to get from the parent ListItemsClass to the destinationClass objects
					of the list properties. */
					new XElement("ParentClassPathsToChildren",
						/* NOTE: AllPossiblePronunciations can also have LexEntry items, since it is a ghost field */
						XElement.Parse("<part id='LexEntry-Jt-AllPossiblePronunciations' type='jtview'><seq class='LexEntry' field='Pronunciations' firstOnly='true' layout='empty'><int class='LexPronunciation' field='Self' /></seq></part>"),
						/* NOTE: AllComplexEntryRefPropertyTargets can also have LexEntry items, since it is a ghost field */
						XElement.Parse("<part id='LexEntry-Jt-AllComplexEntryRefPropertyTargets' type='jtview'><seq class='LexEntry' field='EntryRefs' firstOnly='true' layout='empty'><int class='LexEntryRef' field='Self'/></seq></part>"),
						/* NOTE: AllVariantEntryRefPropertyTargets can also have LexEntry items, since it is a ghost field */
						XElement.Parse("<part id='LexEntry-Jt-AllVariantEntryRefPropertyTargets' type='jtview'><seq class='LexEntry' field='EntryRefs' firstOnly='true' layout='empty'><int class='LexEntryRef' field='Self'/></seq></part>"),
						/* NOTE: AllPossibleAllomorphs can also have LexEntry items, since it is a ghost field */
						XElement.Parse("<part id='LexEntry-Jt-AllPossibleAllomorphs' type='jtview'><seq class='LexEntry' field='AlternateForms' firstOnly='true' layout='empty'><int class='MoForm' field='Self'/></seq></part>"),
						XElement.Parse("<part id='LexEntry-Jt-AllEntryRefs' type='jtview'><seq class='LexEntry' field='EntryRefs' firstOnly='true' layout='empty'><int class='LexEntryRef' field='Self'/></seq></part>"),
						XElement.Parse("<part id='LexEntry-Jt-AllSenses' type='jtview'><seq class='LexEntry' field='AllSenses' firstOnly='true' layout='empty'><int class='LexSense' field='Self' /></seq></part>"),
						/* NOTE: The next item is needed to prevent a crash */
						XElement.Parse("<part id='LexSense-Jt-AllSenses' type='jtview'><obj class='LexSense' field='Self' firstOnly='true' layout='empty' /></part>"),
						XElement.Parse("<part id='LexEntry-Jt-AllExampleSentenceTargets' type='jtview'><seq class='LexEntry' field='AllSenses' firstOnly='true' layout='empty'><seq class='LexSense' field='Examples' firstOnly='true' layout='empty'><int class='LexExampleSentence' field='Self'/></seq></seq></part>"),
						XElement.Parse("<part id='LexSense-Jt-AllExampleSentenceTargets' type='jtview'><seq class='LexSense' field='Examples' firstOnly='true' layout='empty'><int class='LexExampleSentence' field='Self'/></seq></part>"),
						XElement.Parse("<part id='LexEntry-Jt-AllExampleTranslationTargets' type='jtview'><seq class='LexEntry' field='AllSenses' firstOnly='true' layout='empty'><seq class='LexSense' field='Examples' firstOnly='true' layout='empty'><seq class='LexExampleSentence' field='Translations' firstOnly='true' layout='empty'><int class='CmTranslation' field='Self'/></seq></seq></seq></part>"),
						XElement.Parse("<part id='LexSense-Jt-AllExampleTranslationTargets' type='jtview'><seq class='LexSense' field='Examples' firstOnly='true' layout='empty'><seq class='LexExampleSentence' field='Translations' firstOnly='true' layout='empty><int class='CmTranslation' field='Self'/></seq></seq></part>"),
						XElement.Parse("<part id='LexExampleSentence-Jt-AllExampleTranslationTargets' type='jtview'><seq class='LexExampleSentence' field='Translations' firstOnly='true' layout='empty'><int class='CmTranslation' field='Self'/></seq></part>")));

		/// <summary>
		/// Create RecordList for SDA-made up property on the given owner.
		/// </summary>
		/// <param name="decorator"></param>
		/// <param name="usingAnalysisWs"></param>
		/// <param name="owner"></param>
		internal EntriesOrChildClassesRecordList(ISilDataAccessManaged decorator, bool usingAnalysisWs, ILexDb owner)
			: base(decorator, usingAnalysisWs, decorator.MetaDataCache.GetFieldId("LexDb", "Entries", false), owner, "Entries")
		{
		}

		/// <summary>
		/// If m_flid is one of the special classes where we need to modify the expected destination class, do so.
		/// </summary>
		public override int ListItemsClass
		{
			get
			{
				var result = base.ListItemsClass;
				if (result == 0)
					return GhostParentHelper.GetBulkEditDestinationClass(m_cache, m_flid);
				return result;
			}
		}

		#region Overrides of RecordList

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="propertyTable">Interface to a property table.</param>
		/// <param name="publisher">Interface to the publisher.</param>
		/// <param name="subscriber">Interface to the subscriber.</param>
		public override void InitializeFlexComponent(IPropertyTable propertyTable, IPublisher publisher, ISubscriber subscriber)
		{
			base.InitializeFlexComponent(propertyTable, publisher, subscriber);

			// suspend loading the property until given a class by RecordBrowseView via
			// RecordClerk.OnChangeListItemsClass();
			m_suspendReloadUntilOnChangeListItemsClass = true;

			// Used for finding first relative of corresponding current object
			m_pot = PartOwnershipTree.Create(m_cache, this, true);
		}

		#endregion

		protected override void DisposeManagedResources()
		{
			if (m_pot != null && !m_pot.IsDisposed)
				m_pot.Dispose();
			base.DisposeManagedResources();
		}

		public override void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			CheckDisposed();

			if (m_fUpdatingList || m_reloadingList)
				return;	// we're already in the process of changing our list.

			var fLoadSuppressed = m_requestedLoadWhileSuppressed;
			using (var luh = new RecordClerk.ListUpdateHelper(Clerk))
			{
				// don't reload the entire list via propchanges.  just indicate we need to reload.
				luh.TriggerPendingReloadOnDispose = false;
				base.PropChanged(hvo, tag, ivMin, cvIns, cvDel);

				// even if RecordList is in m_fUpdatingList mode, we still want to make sure
				// our alternate list figures out whether it needs to reload as well.
				if (m_fUpdatingList)
					TryHandleUpdateOrMarkPendingReload(hvo, tag, ivMin, cvIns, cvDel);

				// If we edited the list of entries, all our properties are in doubt.
				if (tag == m_cache.ServiceLocator.GetInstance<Virtuals>().LexDbEntries)
				{
					foreach (var key in m_reloadNeededForProperty.Keys.ToArray())
						m_reloadNeededForProperty[key] = true;
				}
			}
			// The ListUpdateHelper doesn't always reload the list when it needs it.  See the
			// second bug listed in FWR-1081.
			if (m_requestedLoadWhileSuppressed && !fLoadSuppressed && !m_fUpdatingList)
				ReloadList();
		}

		protected override void MarkEntriesForReload()
		{
			m_reloadNeededForProperty[m_flidEntries] = true;
		}

		protected override void FinishedReloadList()
		{
			m_reloadNeededForProperty[m_flid] = false;
		}

		protected internal override bool RequestedLoadWhileSuppressed
		{
			get
			{

				return base.RequestedLoadWhileSuppressed || m_reloadNeededForProperty[m_flid];
			}
			set
			{
				base.RequestedLoadWhileSuppressed = value;
			}
		}

		protected internal override bool NeedToReloadList()
		{
			return RequestedLoadWhileSuppressed;
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

		internal override bool RestoreFrom(string pathname)
		{
			// If we are restoring, presumably the 'previous flid' (which should be the flid from the last complete
			// ReloadList) should match the flid that is current, which in turn should correspond to the saved list
			// of sorted objects.
			m_prevFlid = m_flid;
			return base.RestoreFrom(pathname);
		}

		/// <summary>
		/// Stores the target flid (that is, typically the field we want to bulk edit) most recently passed to ReloadList.
		/// </summary>
		private int TargetFlid { get; set; }

		protected internal override void ReloadList(int newListItemsClass, int newTargetFlid, bool force)
		{
			// reload list if it differs from current target class (or if forcing a reload anyway).
			if (newListItemsClass != ListItemsClass || newTargetFlid != TargetFlid || force)
			{
				m_prevFlid = m_flid;
				TargetFlid = newTargetFlid;
				// m_owningObject is *always* the LexDB. All of the properties in PartOwnershipTree/ClassOwnershipTree
				// are virtual properties of the LexDb.
				m_flid = newTargetFlid;
				if (!m_reloadNeededForProperty.ContainsKey(m_flid))
					m_reloadNeededForProperty.Add(m_flid, false);
				if (m_flidEntries == 0 && newListItemsClass == LexEntryTags.kClassId)
					m_flidEntries = m_flid;
				CheckExpectedListItemsClassInSync(newListItemsClass, ListItemsClass);
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
			var repo = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>();
			if (!repo.IsValidObjectId(hvoCurrentBeforeGetObjectSet))
				return -1;

			// first lookup the old object in the new list, just in case it's there.
			var newIndex = base.GetNewCurrentIndex(newSortedObjects, hvoCurrentBeforeGetObjectSet);
			if (newIndex != -1)
				return newIndex;
			var newListItemsClass = ListItemsClass;
			// NOTE: the class of hvoBeforeListChange could be different then prevListItemsClass, if the item is a ghost (owner).
			var classOfObsoleteCurrentObj = repo.GetObject(hvoCurrentBeforeGetObjectSet).ClassID;
			if (m_prevFlid == m_flid || DomainObjectServices.IsSameOrSubclassOf(VirtualListPublisher.MetaDataCache, classOfObsoleteCurrentObj, newListItemsClass))
			{
				// nothing else we can do, since we've already looked that class of object up
				// in our newSortedObjects.
				return -1;
			}
			// we've changed list items class, so find the corresponding new object.
			HashSet<int> commonAncestors;
			var relatives = m_pot.FindCorrespondingItemsInCurrentList(m_prevFlid,
				new HashSet<int>(new[] { hvoCurrentBeforeGetObjectSet }),
				m_flid,
				out commonAncestors);
			var newHvoRoot = relatives.Count > 0 ? relatives.ToArray()[0] : 0;
			var hvoCommonAncestor = commonAncestors.Count > 0 ? commonAncestors.ToArray()[0] : 0;
			if (newHvoRoot == 0 && hvoCommonAncestor != 0)
			{
				// see if we can find the parent in the list (i.e. it might be in a ghost list)
				newIndex = base.GetNewCurrentIndex(newSortedObjects, hvoCommonAncestor);
				if (newIndex != -1)
				{
					return newIndex;
				}
			}

			return base.GetNewCurrentIndex(newSortedObjects, newHvoRoot);
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
		public XElement PartOwnershipTreeSpec
		{
			get { return m_partOwnershipTreeSpec; }
		}

		/// <summary>
		/// See documentation for IMultiListSortItemProvider
		/// </summary>
		/// <param name="oldItems"></param>
		/// <returns></returns>
		public void ConvertItemsToRelativesThatApplyToCurrentList(ref IDictionary<int, object> oldItems)
		{
			var oldItemsToRemove = new HashSet<int>();
			var itemsToAdd = new HashSet<int>();
			// Create a PartOwnershipTree in a mode that can return more than one descendent relatives.
			using (var pot = PartOwnershipTree.Create(m_cache, this, false))
			{
				foreach (var oldItem in oldItems)
				{
					IDictionary<int, object> dictOneOldItem = new Dictionary<int, object>();
					dictOneOldItem.Add(oldItem);
					HashSet<int> relatives = FindCorrespondingItemsInCurrentList(dictOneOldItem, pot);

					// remove the old item if we found relatives we could convert over to.
					if (relatives.Count > 0)
					{
						itemsToAdd.UnionWith(relatives);
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

		private HashSet<int> FindCorrespondingItemsInCurrentList(IDictionary<int, object> itemAndListSourceTokenPairs, PartOwnershipTree pot)
		{
			// create a reverse index of classes to a list of items
			IDictionary<int, HashSet<int>> sourceFlidsToItems = MapSourceFlidsToItems(itemAndListSourceTokenPairs);

			HashSet<int> relativesInCurrentList = new HashSet<int>();
			foreach (KeyValuePair<int, HashSet<int>> sourceFlidToItems in sourceFlidsToItems)
			{
				HashSet<int> commonAncestors;
				relativesInCurrentList.UnionWith(pot.FindCorrespondingItemsInCurrentList(sourceFlidToItems.Key,
					sourceFlidToItems.Value, m_flid,
					out commonAncestors));
			}
			return relativesInCurrentList;
		}

		private IDictionary<int, HashSet<int>> MapSourceFlidsToItems(IDictionary<int, object> itemAndListSourceTokenPairs)
		{
			var sourceFlidsToItems = new Dictionary<int, HashSet<int>>();
			foreach (var itemAndSourceTag in itemAndListSourceTokenPairs)
			{
				if ((int)itemAndSourceTag.Value == m_flid)
				{
					// skip items in the current list
					// (we're trying to lookup relatives to items that are a part of
					// a previous list, not the current one)
					continue;
				}
				HashSet<int> itemsInSourceFlid;
				if (!sourceFlidsToItems.TryGetValue((int)itemAndSourceTag.Value, out itemsInSourceFlid))
				{
					itemsInSourceFlid = new HashSet<int>();
					sourceFlidsToItems.Add((int)itemAndSourceTag.Value, itemsInSourceFlid);
				}
				itemsInSourceFlid.Add(itemAndSourceTag.Key);
			}
			return sourceFlidsToItems;
		}

		#endregion
	}
}