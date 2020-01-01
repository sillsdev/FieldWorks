// Copyright (c) 2004-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Controls.XMLViews;
using LanguageExplorer.Filters;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.DomainImpl;
using SIL.LCModel.DomainServices;

namespace LanguageExplorer.Areas.Lexicon.Tools.BulkEditEntries
{
	/// <summary>
	/// Record list that can be used to bulk edit Entries or its child classes (e.g. Senses or Pronunciations).
	/// The class owning relationship between these properties can be defined by the destinationClass of the
	/// properties listed as 'sourceField' in the RecordList xml definition:
	/// <code>
	///		<ClassOwnershipTree>
	///			<LexEntry sourceField="Entries">
	///				<LexEntryRef sourceField="AllEntryRefs" altSourceField="ComplexEntryTypes:AllComplexEntryRefPropertyTargets;VariantEntryTypes:AllVariantEntryRefPropertyTargets" />
	///				<LexPronunciation sourceField="AllPossiblePronunciations"/>
	///				<LexEtymology sourceField="AllPossibleEtymologies" />
	///				<MoForm sourceField="AllPossibleAllomorphs" />
	///				<LexSense sourceField = "AllSenses" >
	///					<LexExampleSentence sourceField="AllExampleSentenceTargets">
	///						<CmTranslation sourceField = "AllExampleTranslationTargets" />
	///					</LexExampleSentence>
	///					<LexExtendedNote sourceField="AllExtendedNoteTargets" />
	///					<CmPicture sourceField = "AllPossiblePictures" />
	///				</LexSense>
	///			</LexEntry>
	///		</ClassOwnershipTree>
	/// </code>
	/// </summary>
	internal sealed class EntriesOrChildClassesRecordList : RecordList, IMultiListSortItemProvider
	{
		int m_flidEntries;
		int m_prevFlid;
		readonly IDictionary<int, bool> m_reloadNeededForProperty = new Dictionary<int, bool>();
		PartOwnershipTree m_pot;
		// suspend loading the property until given a class by RecordBrowseView via
		// RecordList.OnChangeListItemsClass();
		bool m_suspendReloadUntilOnChangeListItemsClass = true;

		/// <summary />
		internal EntriesOrChildClassesRecordList(string id, StatusBar statusBar, ISilDataAccessManaged decorator, ILexDb owner)
			: base(id, statusBar, decorator, false, new VectorPropertyParameterObject(owner, "Entries", decorator.MetaDataCache.GetFieldId("LexDb", "Entries", false)), new Dictionary<string, PropertyRecordSorter>
				{
					{ AreaServices.Default, new PropertyRecordSorter(AreaServices.ShortName) },
					{ "PrimaryGloss", new PropertyRecordSorter("PrimaryGloss") }
				})
		{
		}

		#region Overrides of RecordList

		/// <summary>
		/// If m_flid is one of the special classes where we need to modify the expected destination class, do so.
		/// </summary>
		public override int ListItemsClass
		{
			get
			{
				var result = base.ListItemsClass;
				return result == 0 ? GhostParentHelper.GetBulkEditDestinationClass(m_cache, m_flid) : result;
			}
		}

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		public override void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			base.InitializeFlexComponent(flexComponentParameters);

			// suspend loading the property until given a class by RecordBrowseView via RecordList.OnChangeListItemsClass();
			m_suspendReloadUntilOnChangeListItemsClass = true;

			// Used for finding first relative of corresponding current object
			m_pot = PartOwnershipTree.Create(m_cache, this, true);
		}

		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				m_pot?.Dispose();
			}
			m_pot = null;

			base.Dispose(disposing);
		}

		public override void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			if (UpdatingList || m_reloadingList)
			{
				return; // we're already in the process of changing our list.
			}
			var fLoadSuppressed = m_requestedLoadWhileSuppressed;
			using (var luh = new ListUpdateHelper(new ListUpdateHelperParameterObject { MyRecordList = this }))
			{
				// don't reload the entire list via propchanges.  just indicate we need to reload.
				luh.TriggerPendingReloadOnDispose = false;
				base.PropChanged(hvo, tag, ivMin, cvIns, cvDel);

				// even if RecordList is in m_fUpdatingList mode, we still want to make sure
				// our alternate list figures out whether it needs to reload as well.
				if (UpdatingList)
				{
					TryHandleUpdateOrMarkPendingReload(hvo, tag, ivMin, cvIns, cvDel);
				}
				// If we edited the list of entries, all our properties are in doubt.
				if (tag == m_cache.ServiceLocator.GetInstance<Virtuals>().LexDbEntries)
				{
					foreach (var key in m_reloadNeededForProperty.Keys.ToArray())
					{
						m_reloadNeededForProperty[key] = true;
					}
				}
			}
			// The ListUpdateHelper doesn't always reload the list when it needs it.  See the
			// second bug listed in FWR-1081.
			if (m_requestedLoadWhileSuppressed && !fLoadSuppressed && !UpdatingList)
			{
				ReloadList();
			}
		}

		protected override void MarkEntriesForReload()
		{
			m_reloadNeededForProperty[m_flidEntries] = true;
		}

		protected override void FinishedReloadList()
		{
			m_reloadNeededForProperty[m_flid] = false;
		}

		public override bool RequestedLoadWhileSuppressed
		{
			get
			{

				return base.RequestedLoadWhileSuppressed || (m_reloadNeededForProperty.ContainsKey(m_flid) && m_reloadNeededForProperty[m_flid]);
			}
			set
			{
				base.RequestedLoadWhileSuppressed = value;
			}
		}

		protected override bool NeedToReloadList()
		{
			return RequestedLoadWhileSuppressed;
		}

		public override bool RestoreListFrom(string pathname)
		{
			// If we are restoring, presumably the 'previous flid' (which should be the flid from the last complete
			// ReloadList) should match the flid that is current, which in turn should correspond to the saved list
			// of sorted objects.
			m_prevFlid = m_flid;
			return base.RestoreListFrom(pathname);
		}

		protected override void ReloadList(int newListItemsClass, int newTargetFlid, bool force)
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
				{
					m_reloadNeededForProperty.Add(m_flid, false);
				}
				if (m_flidEntries == 0 && newListItemsClass == LexEntryTags.kClassId)
				{
					m_flidEntries = m_flid;
				}
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


		protected override void ReloadList()
		{
			if (m_suspendReloadUntilOnChangeListItemsClass)
			{
				m_requestedLoadWhileSuppressed = true;
				return;
			}
			base.ReloadList();
		}

		protected override int GetNewCurrentIndex(List<IManyOnePathSortItem> newSortedObjects, int hvoCurrentBeforeGetObjectSet)
		{
			if (hvoCurrentBeforeGetObjectSet == 0)
			{
				return -1;
			}
			var repo = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>();
			if (!repo.IsValidObjectId(hvoCurrentBeforeGetObjectSet))
			{
				return -1;
			}

			// first lookup the old object in the new list, just in case it's there.
			var newIndex = base.GetNewCurrentIndex(newSortedObjects, hvoCurrentBeforeGetObjectSet);
			if (newIndex != -1)
			{
				return newIndex;
			}
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
			ISet<int> commonAncestors;
			var relatives = m_pot.FindCorrespondingItemsInCurrentList(m_prevFlid, new HashSet<int>(new[] { hvoCurrentBeforeGetObjectSet }), m_flid, out commonAncestors);
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
		/// be reusable in other Entries record lists (e.g. the one used by Lexicon Edit, Browse, Dictionary).
		/// </summary>
		/// <param name="sorterOrFilter"></param>
		/// <returns></returns>
		protected override string PropertyTableId(string sorterOrFilter)
		{
			return $"LexDb.Entries_{sorterOrFilter}";
		}

		#endregion

		/// <summary>
		/// Stores the target flid (that is, typically the field we want to bulk edit) most recently passed to ReloadList.
		/// </summary>
		private int TargetFlid { get; set; }

		#region IMultiListSortItemProvider Members

		/// <summary>
		/// See documentation for IMultiListSortItemProvider
		/// </summary>
		public object ListSourceToken => m_flid;

		/// <summary>
		/// See documentation for IMultiListSortItemProvider
		/// </summary>
		public XElement PartOwnershipTreeSpec { get; } = XDocument.Parse(LexiconResources.EntriesOrChildrenClerkPartOwnershipTree).Root;

		/// <summary>
		/// See documentation for IMultiListSortItemProvider
		/// </summary>
		public void ConvertItemsToRelativesThatApplyToCurrentList(ref IDictionary<int, object> oldItems)
		{
			var oldItemsToRemove = new HashSet<int>();
			var itemsToAdd = new HashSet<int>();
			// Create a PartOwnershipTree in a mode that can return more than one descendent relatives.
			using (var pot = PartOwnershipTree.Create(m_cache, this, false))
			{
				foreach (var oldItem in oldItems)
				{
					var dictOneOldItem = new Dictionary<int, object>
					{
						{
							oldItem.Key,
							oldItem.Value
						}
					};
					var relatives = FindCorrespondingItemsInCurrentList(dictOneOldItem, pot);
					// remove the old item if we found relatives we could convert over to.
					if (relatives.Count > 0)
					{
						itemsToAdd.UnionWith(relatives);
						oldItemsToRemove.Add(oldItem.Key);
					}
				}
			}

			foreach (var itemToRemove in oldItemsToRemove)
			{
				oldItems.Remove(itemToRemove);
			}
			// complete any conversions by adding its relatives.
			var sourceTag = ListSourceToken;
			foreach (var relativeToAdd in itemsToAdd)
			{
				if (!oldItems.ContainsKey(relativeToAdd))
				{
					oldItems.Add(relativeToAdd, sourceTag);
				}
			}
		}

		#endregion

		private HashSet<int> FindCorrespondingItemsInCurrentList(IDictionary<int, object> itemAndListSourceTokenPairs, PartOwnershipTree pot)
		{
			// create a reverse index of classes to a list of items
			var sourceFlidsToItems = MapSourceFlidsToItems(itemAndListSourceTokenPairs);
			var relativesInCurrentList = new HashSet<int>();
			foreach (var sourceFlidToItems in sourceFlidsToItems)
			{
				ISet<int> commonAncestors;
				relativesInCurrentList.UnionWith(pot.FindCorrespondingItemsInCurrentList(sourceFlidToItems.Key, sourceFlidToItems.Value, m_flid, out commonAncestors));
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
	}
}