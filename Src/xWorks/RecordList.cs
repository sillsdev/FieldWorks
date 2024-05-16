// Copyright (c) 2004-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;
using SIL.LCModel.Core.Cellar;
using SIL.FieldWorks.Common.Controls;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FdoUi;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.DomainImpl;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using SIL.FieldWorks.Filters;
using SIL.LCModel.Core.Text;
using SIL.ObjectModel;
using SIL.Reporting;
using SIL.LCModel.Utils;
using SIL.Utils;
using XCore;
using ConfigurationException = SIL.Utils.ConfigurationException;

namespace SIL.FieldWorks.XWorks
{

	/// <summary>
	/// this kind of the event is fired when he RecordList recognizes that its list has changed for any reason.
	/// </summary>
	public class ListChangedEventArgs : EventArgs
	{
		protected RecordList m_list;
		protected ListChangedActions m_actions;
		protected int m_hvoItem;

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
		/// <param name="hvoItem">hvo of the affected item (may be 0)</param>
		public ListChangedEventArgs(RecordList list, ListChangedActions actions, int hvoItem)
		{
			m_list = list;
			m_actions = actions;
			m_hvoItem = hvoItem;
		}

		public RecordList List
		{
			get
			{
				return m_list;
			}
		}

		/// <summary>
		/// if SkipRecordNavigation, RecordClerk can skip Broadcasting OnRecordNavigation.
		/// </summary>
		public ListChangedActions Actions
		{
			get
			{
				return m_actions;
			}
		}

		/// <summary>
		/// If nonzero, the hvo of the affected list item.
		/// </summary>
		public int ItemHvo
		{
			get { return m_hvoItem; }
		}
	}

	/// <summary>
	/// this is simply a definition of the kind of method which can be tied to this kind of event
	/// </summary>
	public delegate void ListChangedEventHandler(object sender, ListChangedEventArgs e);

	/// <summary>
	/// this is a specialty subclass for grabbing all of the items from a possibility list.
	/// </summary>
	public class PossibilityRecordList : RecordList
	{
		/// <summary>
		/// A possibility list is specified in the XML file by specifying the owner and the
		/// property of the list (without the "OA" used in FDO).
		/// Exception: If owner is "unowned", then property should be a GUID of a list that
		/// we'll get from the ICmPossibilityListRepository.
		/// </summary>
		public override void Init(LcmCache cache, Mediator mediator, PropertyTable propertyTable, XmlNode recordListNode)
		{
			CheckDisposed();

			BaseInit(cache, mediator, propertyTable, recordListNode);
			string owner = XmlUtils.GetMandatoryAttributeValue(recordListNode, "owner");
			string property = XmlUtils.GetMandatoryAttributeValue(recordListNode, "property");
			m_owningObject = GetListFromOwnerAndProperty(cache, owner, property);
			Debug.Assert(m_owningObject != null, "Illegal owner or other problem in spec for possibility list.");
			m_oldLength = 0;

			Debug.Assert(m_owningObject != null, "Failed to find possibility list.");
			m_fontName = cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.DefaultFontName;
			m_typeSize = GetFontHeightFromStylesheet(cache, propertyTable, true);
			m_flid  = CmPossibilityListTags.kflidPossibilities;
		}

		public static ICmObject GetListFromOwnerAndProperty(LcmCache cache, string owner, string property)
		{
			ICmObject obj = null;

			switch (owner)
			{
				case "LangProject":
					obj = cache.LanguageProject;
					break;
				case "LexDb":
					obj = cache.LanguageProject.LexDbOA;
					break;
				case "MorphologicalData":
					obj = cache.LanguageProject.MorphologicalDataOA;
					break;
				case "RnResearchNbk":
					obj = cache.LanguageProject.ResearchNotebookOA;
					break;
				case "DsDiscourseData":
					if (cache.LanguageProject.DiscourseDataOA == null)
					{
						NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(
							cache.ActionHandlerAccessor, () =>
															{
																cache.LanguageProject.GetDefaultChartTemplate();
																// Fixes part of LT-8517; should create DiscourseDataOA
																cache.LanguageProject.GetDefaultChartMarkers();
															});
					}
					obj = cache.LanguageProject.DiscourseDataOA;
					break;
				case "Scripture":
					if (cache.LanguageProject.TranslatedScriptureOA == null)
					{
						NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(cache.ActionHandlerAccessor,
							() => cache.LanguageProject.TranslatedScriptureOA = cache.ServiceLocator.GetInstance<IScriptureFactory>().Create());
					}
					obj = cache.LanguageProject.TranslatedScriptureOA;
					break;
				case "unowned":
					// In this case 'property' contains a GUID string from the configuration XML.
					var repo = cache.ServiceLocator.GetInstance<ICmPossibilityListRepository>();
					return repo.GetObject(new Guid(property));
				default:
					return null;
			}
			if (obj.GetType().GetProperty(property + "OA",
				BindingFlags.Public | BindingFlags.NonPublic |
				BindingFlags.Instance | BindingFlags.GetProperty)== null)
				return null;
			var result = obj.GetType().InvokeMember(property + "OA",
				BindingFlags.Public | BindingFlags.NonPublic |
				BindingFlags.Instance | BindingFlags.GetProperty, null, obj, null) as ICmPossibilityList;

			// self-repairing!
			if (result == null)
			{
				NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(cache.ActionHandlerAccessor,
					() =>
						{
							var newList = cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
							obj.GetType().InvokeMember(property + "OA",
								BindingFlags.Public | BindingFlags.NonPublic |
								BindingFlags.Instance | BindingFlags.SetProperty, null, obj,
								new object[] {newList});
							newList.ItemClsid = CmPossibilityTags.kClassId;
							result = newList;
						});
			}
			return result;
		}

		protected override bool ListAlreadySorted
		{
			get
			{
				return !(m_owningObject as ICmPossibilityList).IsSorted;
			}
		}

		protected override IEnumerable<int> GetObjectSet()
		{
			return from obj in (m_owningObject as ICmPossibilityList).ReallyReallyAllPossibilities select obj.Hvo;
		}

		protected override ClassAndPropInfo GetMatchingClass(string className)
		{
			// A possibility list only allows one type of possibility to be owned in the list.
			var pssl = (ICmPossibilityList)m_owningObject;
			int possClass = pssl.ItemClsid;
			string sPossClass = VirtualListPublisher.MetaDataCache.GetClassName(possClass);
			// for the special case of the VariantEntryTypes list, allow inserting a LexEntryInflType object
			// as long as the currently selected object has a parent of that class.
			if (PropertyName == "VariantEntryTypes")
			{
				// return null only if the class of the owner does not match the given className
				// in case of owner being the list itself, use the general class for the list items.
				var currentObjOwner = CurrentObject.Owner;
				string classNameOfOwnerOfCurrentObject = currentObjOwner.Hvo == pssl.Hvo
														 ? sPossClass
														 : currentObjOwner.ClassName;
				if (classNameOfOwnerOfCurrentObject != className)
					return null;
			}
			else if (sPossClass != className)
			{
				return null;
			}

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

			var pssl = (ICmPossibilityList)m_owningObject;
			string owningFieldName = pssl.Name.BestAnalysisAlternative.Text;
			if (pssl.OwningFlid != 0)
				owningFieldName = VirtualListPublisher.MetaDataCache.GetFieldName(pssl.OwningFlid);
			string itemTypeName = CmPossibilityUi.GetPossibilityDisplayName(pssl);
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

			if (tag == CmPossibilityTags.kflidSubPossibilities
				 || tag == CmPossibilityListTags.kflidPossibilities
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
			else if (tag == CmPossibilityTags.kflidName ||
				tag == CmPossibilityTags.kflidAbbreviation)
			{
				if (Clerk.BarHandler is TreeBarHandler)
				{
					if ((Clerk.BarHandler as TreeBarHandler).IsHvoATreeNode(hvo))
					{
						UpdateListItemName(hvo);
					}
				}
				else
				{
					List<int> hvoTargets = new List<int>(new[] {hvo});
					if (IndexOfFirstSortItem(hvoTargets) != -1)
						UpdateListItemName(hvo);
				}
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
					var poss = PossibilityAt(i);
					if (poss.OwningFlid != CmPossibilityTags.kflidSubPossibilities)
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
					var poss = PossibilityAt(i);
					if (poss.OwningFlid != CmPossibilityTags.kflidSubPossibilities)
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
			var pss = m_cache.ServiceLocator.GetInstance<ICmPossibilityRepository>().GetObject(hvoPoss);
			int count = pss.SubPossibilitiesOS.Count;
			if (count == 0)
				return index; // no children

			// Find the last child that occurs in the list.
			for (int ichild = count; --ichild >= 0; )
			{
				int hvoChild = pss.SubPossibilitiesOS[ichild].Hvo;
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
				ISilDataAccess sda = VirtualListPublisher;
				int hvoCurrent = PossibilityAt(CurrentIndex).Hvo;
				var curr = m_cache.ServiceLocator.GetInstance<ICmPossibilityRepository>().GetObject(hvoCurrent);
				int count = curr.SubPossibilitiesOS.Count;
				for (int ichild = 0; ichild < count; ichild++)
				{

					int index = IndexOf(curr.SubPossibilitiesOS[ichild].Hvo);
					if (index >= 0)
						return index;
				}

				for ( ; ; )
				{
					// look for a sibling of hvoCurrentBeforeGetObjectSet coming after the starting point.
					var currentObj = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoCurrent);
					int hvoOwner = currentObj.Owner.Hvo;
					int flidOwner = currentObj.OwningFlid;
					bool fGotCurrent = false;

					foreach (int hvoChild in VirtualListPublisher.VecProp(hvoOwner, (int)flidOwner))
					{
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
					if (flidOwner != CmPossibilityTags.kflidSubPossibilities)
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
				ISilDataAccess sda = VirtualListPublisher;
				var pss = PossibilityAt(CurrentIndex);
				int hvoCurrent = pss.Hvo;
				int hvoOwner = pss.Owner.Hvo;
				// Look for a previous sibling in the list.

				int flidOwner = pss.OwningFlid;
				if (flidOwner == 0)
					return CurrentIndex;
				int[] contents = VirtualListPublisher.VecProp(hvoOwner, (int)flidOwner);
				int count = contents.Length;
				bool fGotCurrent = false;
				for (int ichild = count; --ichild >= 0; )
				{
					int hvoChild = contents[ichild];
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
		/// If m_flid is one of the special classes where we need to modify the expected destination class, do so.
		/// </summary>
		public override int ListItemsClass
		{
			get
			{
				var result = base.ListItemsClass;
				if (result == 0)
					return GhostParentHelper.GetBulkEditDestinationClass(Cache, m_flid);
				return result;
			}
		}

		/// <summary>
		/// this list can work with multiple properties to load its list (e.g. Entries or AllSenses).
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="mediator"></param>
		/// <param name="propertyTable"></param>
		/// <param name="recordListNode"></param>
		public override void Init(LcmCache cache, Mediator mediator, PropertyTable propertyTable, XmlNode recordListNode)
		{
			CheckDisposed();
			// suspend loading the property until given a class by RecordBrowseView via
			// RecordClerk.OnChangeListItemsClass();
			m_suspendReloadUntilOnChangeListItemsClass = true;

			m_configuration = recordListNode;
			BaseInit(cache, mediator, propertyTable, recordListNode);
			bool analysis = XmlUtils.GetOptionalBooleanAttributeValue(recordListNode, "analysisWs", false);
			// used for finding first relative of corresponding current object
			m_pot = PartOwnershipTree.Create(cache, this, true);

			GetDefaultFontNameAndSize(analysis, cache, propertyTable, out m_fontName, out m_typeSize);
			string owner = XmlUtils.GetOptionalAttributeValue(recordListNode, "owner");
			// by default we'll setup for Entries
			GetTargetFieldInfo(LexEntryTags.kClassId, owner, 0, out m_owningObject, out m_flid, out m_propertyName);
		}

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

			bool fLoadSuppressed = m_requestedLoadWhileSuppressed;
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
				if (tag == Cache.ServiceLocator.GetInstance<Virtuals>().LexDbEntries)
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
			if (newListItemsClass != this.ListItemsClass || newTargetFlid != TargetFlid || force)
			{
				string owner = m_owningObject.GetType().Name;
				ICmObject owningObj;
				m_prevFlid = m_flid;
				TargetFlid = newTargetFlid;
				GetTargetFieldInfo(newListItemsClass, owner, newTargetFlid, out owningObj, out m_flid, out m_propertyName);
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
			ICmObjectRepository repo = Cache.ServiceLocator.GetInstance<ICmObjectRepository>();
			if (!repo.IsValidObjectId(hvoCurrentBeforeGetObjectSet))
				return -1;

			// first lookup the old object in the new list, just in case it's there.
			int newIndex = base.GetNewCurrentIndex(newSortedObjects, hvoCurrentBeforeGetObjectSet);
			if (newIndex != -1)
				return newIndex;
			int newListItemsClass = this.ListItemsClass;
			// NOTE: the class of hvoBeforeListChange could be different then prevListItemsClass, if the item is a ghost (owner).
			int classOfObsoleteCurrentObj = repo.GetObject(hvoCurrentBeforeGetObjectSet).ClassID;
			if (m_prevFlid == m_flid ||
				DomainObjectServices.IsSameOrSubclassOf(VirtualListPublisher.MetaDataCache, classOfObsoleteCurrentObj, newListItemsClass))
			{
				// nothing else we can do, since we've already looked that class of object up
				// in our newSortedObjects.
				return -1;
			}
			// we've changed list items class, so find the corresponding new object.
			ISet<int> commonAncestors;
			ISet<int> relatives = m_pot.FindCorrespondingItemsInCurrentList(m_prevFlid, new HashSet<int> {hvoCurrentBeforeGetObjectSet},
				m_flid, out commonAncestors);
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
		/// <param name="targetFlid">If non-zero, get a field suitable for bulk editing this field of the target class.</param>
		/// <param name="owningObj"></param>
		/// <param name="flid"></param>
		/// <param name="flidName"></param>
		private void GetTargetFieldInfo(int targetClsId, string owner, int targetFlid,
			out ICmObject owningObj, out int flid, out string flidName)
		{
			// first try to find the expected source field.
			flidName = m_pot.GetSourceFieldName(targetClsId, targetFlid);
			flid = GetFlidOfVectorFromName(flidName, owner, Cache, m_mediator, m_propertyTable, out owningObj);
			if (!m_reloadNeededForProperty.ContainsKey(flid))
				m_reloadNeededForProperty.Add(flid, false);
			if (m_flidEntries == 0 && targetClsId == LexEntryTags.kClassId)
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
		public void ConvertItemsToRelativesThatApplyToCurrentList(ref IDictionary<int, object> oldItems)
		{
			var oldItemsToRemove = new HashSet<int>();
			var itemsToAdd = new HashSet<int>();
			// Create a PartOwnershipTree in a mode that can return more than one descendent relatives.
			using (PartOwnershipTree pot = PartOwnershipTree.Create(Cache, this, false))
			{
				foreach (KeyValuePair<int, object> oldItem in oldItems)
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

		private HashSet<int> FindCorrespondingItemsInCurrentList(IDictionary<int, object> itemAndListSourceTokenPairs,
			PartOwnershipTree pot)
		{
			// create a reverse index of classes to a list of items
			IDictionary<int, HashSet<int>> sourceFlidsToItems = MapSourceFlidsToItems(itemAndListSourceTokenPairs);

			var relativesInCurrentList = new HashSet<int>();
			foreach (KeyValuePair<int, HashSet<int>> sourceFlidToItems in sourceFlidsToItems)
			{
				ISet<int> commonAncestors;
				relativesInCurrentList.UnionWith(pot.FindCorrespondingItemsInCurrentList(sourceFlidToItems.Key, sourceFlidToItems.Value,
					m_flid, out commonAncestors));
			}
			return relativesInCurrentList;
		}

		private IDictionary<int, HashSet<int>> MapSourceFlidsToItems(IDictionary<int, object> itemAndListSourceTokenPairs)
		{
			var sourceFlidsToItems = new Dictionary<int, HashSet<int>>();
			foreach (KeyValuePair<int, object> itemAndSourceTag in itemAndListSourceTokenPairs)
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

	/// <summary>
	/// This type of record list is used in conjunction with a MatchingItemsRecordClerk.
	/// </summary>
	public class MatchingItemsRecordList : RecordList
	{
		private XmlNode m_configNode;
		private IEnumerable<int> m_objs = null;

		public override void Init(LcmCache cache, Mediator mediator, PropertyTable propertyTable, XmlNode recordListNode)
		{
			m_fEnableSendPropChanged = false;
			m_configNode = recordListNode;
			base.Init(cache, mediator, propertyTable, recordListNode);
		}

		public override void InitLoad(bool loadList)
		{
			CheckDisposed();
			ComputeInsertableClasses();
			CurrentIndex = -1;
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

		protected override IEnumerable<int> GetObjectSet()
		{
			if (m_objs == null)
				return new int[0]; // not null, throws exception if used in forall.
			return m_objs;
		}

		/// <summary>
		/// This reloads the list using the supplied set of hvos.
		/// </summary>
		/// <param name="rghvo"></param>
		public void UpdateList(IEnumerable<int> objs)
		{
			m_objs = objs;
			ReloadList();
		}
	}

	/// <summary>
	/// RecordList is a vector of objects
	/// </summary>
	/// <remarks>RecordList is not XCore aware but is aware of RecordSorter and RecordFilter.
	/// </remarks>
	public class RecordList : DisposableBase, IVwNotifyChange, ISortItemProvider
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

		protected LcmCache m_cache;
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
		protected string m_propertyName;
		protected string m_fontName;
		protected int m_typeSize = 10;
		protected bool m_reloadingList;
		private bool m_suppressingLoadList;
		protected bool m_requestedLoadWhileSuppressed;
		protected bool m_deletingObject;
		protected int m_oldLength;
		protected bool m_fUpdatingList;
		/// <summary>
		/// The actual database flid from which we get our list of objects, and apply a filter to.
		/// </summary>
		protected int m_flid;

		/// <summary>
		///
		/// </summary>
		protected Mediator m_mediator;

		protected PropertyTable m_propertyTable;
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

		/// <summary>
		/// This becomes the SilDataAccess for any views which want to see the filtered, sorted record list.
		/// </summary>
		private ObjectListPublisher m_publisher;

		// As passed to Init().
		private XmlNode m_recordListNode;

		/// <summary>
		/// Set of objects that we own and have to dispose
		/// </summary>
		private readonly DisposableObjectsSet<IDisposable> m_ObjectsToDispose =
			new DisposableObjectsSet<IDisposable>();

		#endregion Data members

		#region Construction & initialization

		/// <summary>
		/// a factory method for RecordLists
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="propertyTable"></param>
		/// <param name="recordListNode"></param>
		/// <param name="mediator"></param>
		/// <returns></returns>
		static public RecordList Create(LcmCache cache, Mediator mediator, PropertyTable propertyTable, XmlNode recordListNode)
		{
			RecordList list = null;

			//does the configuration specify a special RecordList class?
			XmlNode customListNode = recordListNode.SelectSingleNode("dynamicloaderinfo");
			if (customListNode != null)
				list = (RecordList)DynamicLoader.CreateObject(customListNode);
			else
				list = new RecordList();

			list.Init(cache, mediator, propertyTable, recordListNode);
			return list;
		}

		public RecordList()
		{
		}

		/// <summary>
		/// Get the font size from the Stylesheet
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="propertyTable"></param>
		/// <param name="analysisWs">pass in 'true' for the DefaultAnalysisWritingSystem
		/// pass in 'false' for the DefaultVernacularWritingSystem</param>
		/// <returns>return Font size from stylesheet</returns>
		static protected int GetFontHeightFromStylesheet(LcmCache cache, PropertyTable propertyTable, bool analysisWs)
		{
			int fontHeight;
			IVwStylesheet stylesheet = FontHeightAdjuster.StyleSheetFromPropertyTable(propertyTable);
				IWritingSystemContainer wsContainer = cache.ServiceLocator.WritingSystems;
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

		public virtual void Init(LcmCache cache, Mediator mediator, PropertyTable propertyTable, XmlNode recordListNode)
		{
			CheckDisposed();

			BaseInit(cache, mediator, propertyTable, recordListNode);
			string owner = XmlUtils.GetOptionalAttributeValue(recordListNode, "owner");
			bool analysis = XmlUtils.GetOptionalBooleanAttributeValue(recordListNode, "analysisWs", false);
			if (!string.IsNullOrEmpty(m_propertyName))
			{
				m_flid = GetFlidOfVectorFromName(m_propertyName, owner, analysis, cache,
					mediator, propertyTable,
					out m_owningObject, out m_fontName, out m_typeSize);
				UpdatePrivateList();
			}
			else
			{
				// We don't know which font to use, so choose a standard one for now.
				m_fontName = MiscUtils.StandardSansSerif;
				// Only other current option is to specify an ordinary property (or a virtual one).
				m_flid = VirtualListPublisher.MetaDataCache.GetFieldId(
					XmlUtils.GetMandatoryAttributeValue(recordListNode, "class"),
					XmlUtils.GetMandatoryAttributeValue(recordListNode, "field"), true);
				// Review JohnH(JohnT): This is only useful for dependent clerks, but I don't know how to check this is one.
				m_owningObject = null;
			}
			m_oldLength = 0;
		}

		protected void BaseInit(LcmCache cache, Mediator mediator, PropertyTable propertyTable, XmlNode recordListNode)
		{
			Debug.Assert(mediator != null);

			m_recordListNode = recordListNode;
			m_mediator = mediator;
			m_propertyTable = propertyTable;
			m_propertyName = XmlUtils.GetOptionalAttributeValue(recordListNode, "property", "");
			m_cache = cache;
			CurrentIndex = -1;
			m_hvoCurrent = 0;
			m_oldLength = 0;
		}

		#endregion Construction & initialization

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
				if (m_publisher == null)
				{
					var baseDa = m_cache.MainCacheAccessor as ISilDataAccessManaged;
					if (m_recordListNode != null)
					{
						var virtualListSpec = XmlUtils.FindNode(m_recordListNode, "decoratorClass");
						if (virtualListSpec != null)
						{
							baseDa = GetDynamicListPublisher(virtualListSpec);
							if (baseDa is ISetMediator)
								((ISetMediator)baseDa).SetMediator(m_mediator, m_propertyTable);
							if (baseDa is ISetRootHvo)
								((ISetRootHvo)baseDa).SetRootHvo(m_owningObject.Hvo);
							if (baseDa is ISetCache)
								((ISetCache)baseDa).SetCache(m_cache);
						}
					}
					m_publisher = new ObjectListPublisher(baseDa, RecordListFlid);
				}
				return m_publisher;
			}
		}

		/// <summary>
		/// Get the list publisher specified by the argument node.
		/// If the argument node has a "key" attribute, look in the mediator to see whether this
		/// publisher has already been created; if so, use it.
		/// Otherwise create it using the usual dynamic loader attributes (and save it if we have a key).
		/// This allows multiple panes to readily use the same list publisher.
		/// </summary>
		/// <param name="virtualListSpec"></param>
		private ISilDataAccessManaged GetDynamicListPublisher(XmlNode virtualListSpec)
		{
			string key = XmlUtils.GetOptionalAttributeValue(virtualListSpec, "key");
			ISilDataAccessManaged result = null;
			if (key != null)
			{
				result = m_propertyTable.GetValue<ISilDataAccessManaged>(key);
			}
			if (result == null)
			{
				result = (ISilDataAccessManaged)DynamicLoader.CreateObject(virtualListSpec,
					m_cache.MainCacheAccessor as ISilDataAccessManaged, virtualListSpec, m_cache.ServiceLocator);
				if (key != null)
				{
					m_propertyTable.SetProperty(key, result, true);
					m_propertyTable.SetPropertyPersistence(key, false);
				}
			}
			return result;
		}

		internal protected virtual string PropertyTableId(string sorterOrFilter)
		{
			// Dependent lists do not have owner/property set. Rather they have class/field.
			string className = VirtualListPublisher.MetaDataCache.GetOwnClsName((int)m_flid);
			string fieldName = VirtualListPublisher.MetaDataCache.GetFieldName((int)m_flid);
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
			}
		}

		public virtual RecordFilter Filter
		{
			get
			{
				CheckDisposed();

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
				var repo = Cache.ServiceLocator.ObjectRepository;
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

		public LcmCache Cache
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

		#region DisposableBase

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
			m_mediator = null;
			m_sorter = null;
			m_filter = null;
			m_owningObject = null;
			m_propertyName = null;
			m_fontName = null;
			m_insertableClasses = null;
			m_sortedObjects = null;
		}

		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + " ******");
			base.Dispose(disposing);
		}

		#endregion DisposableBase

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
					hvo == Cache.LangProject.Hvo &&
					tag == Cache.ServiceLocator.GetInstance<Virtuals>().LangProjectAllWordforms)
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

		private bool TryReloadForInvalidPathObjectsOnCurrentObject(int tag, int cvDel)
		{
			// see if the property is the VirtualFlid of the owning clerk. If so,
			// the owning clerk has reloaded, so we should also reload.
			RecordClerk clerkProvidingRootObject = null;
			if (Clerk.TryClerkProvidingRootObject(out clerkProvidingRootObject) &&
				clerkProvidingRootObject.VirtualFlid == tag &&
				cvDel > 0)
			{
				// we're deleting or replacing items, so assume need to reload.
				// we want to wait until after everything is finished reloading, however.
				m_requestedLoadWhileSuppressed = true;
			}

			// Try to catch things that don't obviously affect us, but will cause problems.
			if (cvDel > 0 && CurrentIndex >= 0 && IsPropOwning(tag))
			{
				// We've deleted an object. Unfortunately we can't find out what object was deleted.
				// Therefore it will be too slow to check every item in our list. Checking out the current one
				// thoroughly prevents many problems (e.g., LT-4880)
				IManyOnePathSortItem item = SortedObjects[CurrentIndex] as IManyOnePathSortItem;
				{
					if (!m_cache.ServiceLocator.IsValidObjectId(item.KeyObject))
					{
						ReloadList();
						return true;
					}
					for (int i = 0; i < item.PathLength; i++)
					{
						if (!m_cache.ServiceLocator.IsValidObjectId(item.PathObject(i)))
						{
							ReloadList();
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
					ReloadList(ivMin, cvIns, cvDel);
					return true;
				}
			}
			// tag == WfiWordformTags.kflidAnalyses is needed for wordforms that don't appear in a segment.
			else if ((tag == SegmentTags.kflidAnalyses || tag == WfiWordformTags.kflidAnalyses) && m_publisher.OwningFieldName == "Wordforms")
			{
				// Changing this potentially changes the list of wordforms that occur in the interesting texts.
				// Hopefully we don't rebuild the list every time; usually this can only be changed in another view.
				// In case we DO have a concordance active in one window while editing another, if this isn't the
				// active window postpone until it is.
				Form window = m_propertyTable.GetValue<Form>("window");
				if (window != Form.ActiveForm)
				{
					RequestedLoadWhileSuppressed = true;
					RequestReloadOnActivation(window);
					return true;
				}
				ReloadList();
				return true;
			}
			else
			{
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
				if (m_flid == Cache.ServiceLocator.GetInstance<Virtuals>().LexDbEntries &&
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
			ReloadLexEntries = true;
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

		/// <summary>
		/// True if the list is the LexDb (LexEntries) and one of the entries has changed.
		/// </summary>
		protected internal bool ReloadLexEntries { get; protected set; }

		protected internal virtual bool NeedToReloadList()
		{
			bool fReload = RequestedLoadWhileSuppressed;
			if (Flid == Cache.ServiceLocator.GetInstance<Virtuals>().LexDbEntries)
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
				if (!Cache.ServiceLocator.IsValidObjectId(rootHvo) || !objectSet.Contains(rootHvo))
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
		internal virtual void ReloadList(int ivMin, int cvIns, int cvDel)
		{
			CheckDisposed();

			// if a previous reload was requested, but suppressed, try to reload the entire list now.
			// If we are missing a valid owning object, current HVO, or current index, the list is empty; load it now.
			// If there is more than one "new" item, reload the whole list.
			// If there is a new item that doesn't replace an old one, we need to reload the whole list (LT-20952).
			if (RequestedLoadWhileSuppressed || m_owningObject == null || m_hvoCurrent == 0 || m_currentIndex < 0 || cvIns > 1 || cvIns > cvDel)
			{
				ReloadList();
				return;
			}

			var cList = VirtualListPublisher.get_VecSize(m_owningObject.Hvo, m_flid);
			switch (cvIns)
			{
				case 1 when cvDel == 1:
				{
					if (cList == 1)
					{
						// we have only one item in our list, so let's just do a full reload.
						// We don't want to insert completely new items in an obsolete list (Cf. LT-6741,6845).
						ReloadList();
						return;
					}
					// LT-12632: Before we try to insert a new one, we need to delete the deleted one,
					// otherwise it may crash as it tries to compare the new item with an invalid one.
					ClearOutInvalidItems();
					if (ivMin < cList)
					{
						// REVIEW (Hasso) 2022.07: How does this method know what the new item is if we pass only the current "replaced" HVO?
						int hvoReplaced = VirtualListPublisher.get_VecItem(m_owningObject.Hvo, m_flid, ivMin);
						ReplaceListItem(hvoReplaced);
					}
					return;
				}
				case 0 when cvDel == 0:
					UpdateListItemName(m_hvoCurrent);
					return;
				// If we are deleting less than half of the list, this may be more efficient than reloading (testing performance is proving difficult)
				case 0 when cvDel > 0 && cvDel * 2 < cList:
					ClearOutInvalidItems();
					DoneReload?.Invoke(this, EventArgs.Empty);
					return;
				default:
					ReloadList();
					return;
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
			if (!Cache.ServiceLocator.IsValidObjectId(item.KeyObject))
				return true;
			for (int i = 0; i < item.PathLength; i++)
			{
				if (!Cache.ServiceLocator.IsValidObjectId(item.PathObject(i)))
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
				var unwantedIndices = new HashSet<int>(IndicesOfSortItems(hvosToRemove));
				// then remove any remaining items that point to invalid objects.
				unwantedIndices.UnionWith(IndicesOfInvalidSortItems());
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
		protected internal void ReplaceListItem(int hvoReplaced)
		{
			ReplaceListItem(hvoReplaced, ListChangedEventArgs.ListChangedActions.Normal);
		}

		/// <summary>
		/// replace any matching items in our sort list.
		/// </summary>
		internal void ReplaceListItem(int hvoReplaced, ListChangedEventArgs.ListChangedActions listChangeAction)
		{
			bool fUpdatingListOrig = m_fUpdatingList;
			m_fUpdatingList = true;
			try
			{
				var hvoOldCurrentObj = CurrentObjectHvo;
				var objReplaced = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoReplaced);
				if (ReplaceListItem(objReplaced, hvoReplaced, true).Count > 0)
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
		/// <param name="fAssumeSameIndex">if true, we'll try to replace sort objects for hvoToReplace with newObj at the same indices.
		/// if false, we'll rely upon sorter to merge the new item into the right index, or else add to the end.
		/// Enhance: Is there some way we can compare the sort/filter results for newObj and hvoToReplace that is hvo-independent?</param>
		/// <returns>resulting list of newSortItems added to SortedObjects</returns>
		protected ArrayList ReplaceListItem(ICmObject newObj, int hvoToReplace, bool fAssumeSameIndex)
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
				if (fAssumeSameIndex)
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
				m_sorter.DataAccess = m_publisher; // for safety, ensure this any time we use it.
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

		private int m_indexToRestoreDuringReload = -1;

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
				if (m_owningObject != null && SortedObjects.Count > 0 && Clerk.UpdateHelper != null &&
					((Clerk.UpdateHelper.ClearBrowseListUntilReload) || !Clerk.IsActiveInGui))
				{
					m_indexToRestoreDuringReload = CurrentIndex;	// try to restore this index during reload.
					// clear everything for now, including the current index, but don't issue a RecordNavigation.
					SendPropChangedOnListChange(-1, new ArrayList(),
						ListChangedEventArgs.ListChangedActions.SkipRecordNavigation);
				}
				m_requestedLoadWhileSuppressed = true;
				// it's possible that we'll want to reload once we become the main active window (cf. LT-9251)
				if (m_mediator != null)
				{
					Form window = m_propertyTable.GetValue<Form>("window");
					IApp app = m_propertyTable.GetValue<IApp>("App");
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
						m_propertyTable.SetProperty(Clerk.PersistedIndexProperty, m_indexToRestoreDuringReload,
							PropertyTable.SettingsGroup.LocalSettings,
							true);
						m_indexToRestoreDuringReload = -1;
					}
					Clerk.UpdateHelper.ClearBrowseListUntilReload = false;
				}
				m_reloadingList = true;
				if (UpdatePrivateList())
					return; // Cannot complete the reload until PropChangeds complete.
				int newCurrentIndex = CurrentIndex;
				ArrayList newSortedObjects = null;
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
				catch (LcmInvalidFieldException)
				{
					newSortedObjects = HandleInvalidFilterSortField();
				}
				catch(ConfigurationException ce)
				{
					if (ce.InnerException is LcmInvalidFieldException)
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
			ArrayList newSortedObjects;
			m_filter = null;
			m_sorter = null;
			MessageBox.Show(Form.ActiveForm, xWorksStrings.ksInvalidFieldInFilterOrSorter, xWorksStrings.ksErrorCaption,
				MessageBoxButtons.OK, MessageBoxIcon.Warning);
			newSortedObjects = GetFilteredSortedList();
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

			using (var progress = FwXWindow.CreateSimpleProgressState(m_propertyTable))
			{
				progress.SetMilestone(xWorksStrings.ksSorting);
				// Allocate an arbitrary 20% for making the items.
				var objectSet = GetObjectSet();
				int count = objectSet.Count();
				int done = 0;
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
			ReloadLexEntries = false;
		}

		protected virtual int GetNewCurrentIndex(ArrayList newSortedObjects, int hvoCurrent)
		{
			return IndexOf(newSortedObjects, hvoCurrent);
		}

		protected void SortList(ArrayList newSortedObjects, ProgressState progress)
		{
			if (m_sorter != null && !ListAlreadySorted)
			{
				m_sorter.DataAccess = m_publisher;
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
//			if (!obj.IsValidObject)
//				throw new Exception("GetObjectSet returned an invalid object with HVO " + obj.Hvo);
			int start = sortedObjects.Count;
			if (m_sorter == null)
				sortedObjects.Add(new ManyOnePathSortItem(hvo, null, null));
			else
			{
				m_sorter.DataAccess = m_publisher;
				m_sorter.CollectItems(hvo, sortedObjects);
			}
			if (m_filter != null && m_fEnableFilters)
			{
				m_filter.DataAccess = m_publisher;
				for (int i = start; i < sortedObjects.Count; )
				{
					if (m_filter.Accept(sortedObjects[i] as IManyOnePathSortItem))
						i++; // advance loop if we don't delete!
					else
					{
						//IManyOnePathSortItem hasBeen = (IManyOnePathSortItem)sortedObjects[i];
						sortedObjects.RemoveAt(i);
						//hasBeen.Dispose();
					}
				}
			}
//			for (int i = start; i < sortedObjects.Count; i++)
//			{
//				IManyOnePathSortItem item = sortedObjects[i] as IManyOnePathSortItem;
//				item.AssertValid();
//				if (!item.KeyCmObjectUsing(m_cache).IsValidObject)
//					throw new Exception("IManyOnePathSortItem has an invalid key object with HVO " + item.KeyObject);
//			}
			return sortedObjects.Count - start;
		}

		protected void SendPropChangedOnListChange(int newCurrentIndex, ArrayList newSortedObjects, ListChangedEventArgs.ListChangedActions actions)
		{
			//Populate the virtual cache property which will hold this set of hvos, in this order.
			int[] hvos = new int[newSortedObjects.Count];
			int i = 0;
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
				newCurrentIndex = m_propertyTable.GetIntProperty(Clerk.PersistedIndexProperty, 0, PropertyTable.SettingsGroup.LocalSettings);
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
			get { return m_publisher != null; }
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

			// If this is a Text Discourse Chart Template, populate it with sample column groups and columns (LT-20768)
			if (OwningObject.OwningFlid == DsDiscourseDataTags.kflidConstChartTempl)
			{
				var newPossibility = (ICmPossibility)Cache.ServiceLocator.GetObject(hvoNew);
				if (newPossibility.Owner is ICmPossibilityList)
				{
					SetUpConstChartTemplateTemplate(newPossibility);
				}
			}


			if (hvoNew != 0)
				return Cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoNew);
			return null;
		}

		/// <summary>
		/// Populates a discourse chart template with sample column groups and columns so that users have an idea what to do with it (LT-20768)
		/// </summary>
		/// <remarks>
		/// The names of the template template parts are in the UI language, but they are stored in the default analysis WS. This could be problematic in
		/// the unlikely case that the default analysis WS font doesn't have characters for the UI language data. But this is unlikely, and the labels
		/// are temporary.
		/// </remarks>
		public static void SetUpConstChartTemplateTemplate(ICmPossibility templateRoot)
		{
			var factory = templateRoot.Services.GetInstance<ICmPossibilityFactory>();
			var analWS = templateRoot.Services.WritingSystems.DefaultAnalysisWritingSystem.Handle;
			templateRoot.Name.set_String(analWS, xWorksStrings.ksNewTemplate);

			var group1 = factory.Create(Guid.NewGuid(), templateRoot);
			group1.Name.set_String(analWS, string.Format(xWorksStrings.ksColumnGroupX, 1));
			for (var i = 1; i <= 2; i++)
			{
				factory.Create(Guid.NewGuid(), group1).Name.set_String(analWS, string.Format(xWorksStrings.ksColumnX, $"1.{i}"));
			}
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
			Debug.Assert(cpi != null, "This object should not have been asked to insert an object of the class " + className + ".");
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
			var options = new RecordClerk.ListUpdateHelper.ListUpdateHelperOptions();
			options.SuspendPropChangedDuringModification = true;
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

		internal class CpiPathBasedCreateAndInsert : ICreateAndInsert<ICmObject>
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

		/// <summary>
		/// Interface for creating method objects that can be passed into DoCreateAndInsert
		/// in order to create an object, insert them into our list, and adjust CurrentIndex in one operation.
		/// </summary>
		/// <typeparam name="TObject"></typeparam>
		public interface ICreateAndInsert<TObject>
			where TObject : ICmObject
		{
			TObject Create();
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
		/// <param name="hvo"></param>
		/// <returns>-1 if the object is not in the list</returns>
		public int IndexOfChildOf(int hvoTarget, LcmCache cache)
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
		/// <param name="hvo"></param>
		/// <returns>-1 if the object is not in the list</returns>
		public int IndexOfParentOf(int hvoTarget, LcmCache cache)
		{
			CheckDisposed();

			// If the list is made up of fake objects, we can't find one of them that our target owns,
			// and trying to will crash, so give up.
			if (SortedObjects.Count == 0 ||
				!m_cache.ServiceLocator.ObjectRepository.IsValidObjectId(((IManyOnePathSortItem)SortedObjects[0]).RootObjectHvo) ||
				!m_cache.ServiceLocator.ObjectRepository.IsValidObjectId(hvoTarget))
			{
				return -1;
			}

			var target = m_cache.ServiceLocator.ObjectRepository.GetObject(hvoTarget);
			var owners = new HashSet<int>();
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
		/// <param name="propertyTable"></param>
		/// <param name="owningObject">the object which owns the vector</param>
		/// <param name="fontName"></param>
		/// <param name="cache"></param>
		/// <param name="mediator"></param>
		/// <param name="typeSize"></param>
		/// <returns>The real flid of the vector in the database.</returns>
		internal int GetFlidOfVectorFromName(string name, string owner, bool analysisWs,
			LcmCache cache, Mediator mediator, PropertyTable propertyTable, out ICmObject owningObject, out string fontName,
			out int typeSize)
		{
			// Many of these are vernacular, but if not,
			// they should set it to the default anal font by using the "analysisWs" property.
			GetDefaultFontNameAndSize(analysisWs, cache, propertyTable, out fontName, out typeSize);

			return GetFlidOfVectorFromName(name, owner, cache, mediator, propertyTable, out owningObject, ref fontName, ref typeSize);
		}

		internal int GetFlidOfVectorFromName(string propertyName, string owner, LcmCache cache, Mediator mediator, PropertyTable propertyTable, out ICmObject owningObject)
		{
			string fontName = "";
			int typeSize = 0;
			return GetFlidOfVectorFromName(propertyName, owner, cache, mediator, propertyTable, out owningObject, ref fontName, ref typeSize);
		}

		/// <summary>
		/// Return the Flid for the model property that this vector represents.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="owner"></param>
		/// <param name="cache"></param>
		/// <param name="mediator"></param>
		/// <param name="propertyTable"></param>
		/// <param name="owningObject"></param>
		/// <param name="fontName"></param>
		/// <param name="typeSize"></param>
		/// <returns></returns>
		internal int GetFlidOfVectorFromName(string name, string owner, LcmCache cache, Mediator mediator, PropertyTable propertyTable, out ICmObject owningObject, ref string fontName, ref int typeSize)
		{
			var defAnalWsFontName = cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.DefaultFontName;
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
								owningObject = cache.LanguageProject;
								break;
							case "LexDb":
								owningObject = cache.LanguageProject.LexDbOA;
								break;
							case "RnResearchNbk":
								owningObject = cache.LanguageProject.ResearchNotebookOA;
								break;
							case "DsDiscourseData":
								owningObject = cache.LanguageProject.DiscourseDataOA;
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
						if (cache.LanguageProject.LexDbOA.ReversalIndexesOC.Count > 0)
						{
							owningObject = cache.LanguageProject.LexDbOA.ReversalIndexesOC.ToArray()[0];
							realFlid = ReversalIndexTags.kflidEntries;
						}
					}
					else
					{
						owningObject = cache.LanguageProject.LexDbOA;
						realFlid = Cache.ServiceLocator.GetInstance<Virtuals>().LexDbEntries;
					}
					break;
				case "Scripture_0_0_Content": // StText that is contents of first section of first book.
					try
					{
						var sb = cache.LanguageProject.TranslatedScriptureOA.ScriptureBooksOS[0];
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
					owningObject = cache.LanguageProject;
					realFlid = cache.ServiceLocator.GetInstance<Virtuals>().LangProjTexts;
					break;
				case "MsFeatureSystem":
					owningObject = cache.LanguageProject;
					realFlid = LangProjectTags.kflidMsFeatureSystem;
					break;

				// phonology
				case "Phonemes":
					if (cache.LanguageProject.PhonologicalDataOA.PhonemeSetsOS.Count == 0)
					{
						// Pathological...this helps the memory-only backend mainly, but makes others self-repairing.
						NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(cache.ActionHandlerAccessor,
						   ()=>
						   {
							cache.LanguageProject.PhonologicalDataOA.PhonemeSetsOS.Add(
								cache.ServiceLocator.GetInstance<IPhPhonemeSetFactory>().Create());
						   });
					}
					owningObject = cache.LanguageProject.PhonologicalDataOA.PhonemeSetsOS[0];
					realFlid = PhPhonemeSetTags.kflidPhonemes;
					break;
				case "BoundaryMarkers":
					owningObject = cache.LanguageProject.PhonologicalDataOA.PhonemeSetsOS[0];
					realFlid = PhPhonemeSetTags.kflidBoundaryMarkers;
					break;
				case "Environments":
					owningObject = cache.LanguageProject.PhonologicalDataOA;
					realFlid = PhPhonDataTags.kflidEnvironments;
					break;
				case "NaturalClasses":
					owningObject = cache.LanguageProject.PhonologicalDataOA;
					realFlid = PhPhonDataTags.kflidNaturalClasses;
					fontName = defAnalWsFontName;
					typeSize = GetFontHeightFromStylesheet(cache, propertyTable, true);
					break;

				case "PhonologicalFeatures":
					owningObject = cache.LangProject.PhFeatureSystemOA;
					realFlid = FsFeatureSystemTags.kflidFeatures;
					fontName = defAnalWsFontName;
					typeSize = GetFontHeightFromStylesheet(cache, propertyTable, true);
					break;

				case "PhonologicalRules":
					owningObject = cache.LangProject.PhonologicalDataOA;
					realFlid = PhPhonDataTags.kflidPhonRules;
					break;

				// morphology
				case "AdhocCoprohibitions":
					owningObject = cache.LanguageProject.MorphologicalDataOA;
					realFlid = MoMorphDataTags.kflidAdhocCoProhibitions;
					fontName = defAnalWsFontName;
					typeSize = GetFontHeightFromStylesheet(cache, propertyTable, true);
					break;
				case "CompoundRules":
					owningObject = cache.LanguageProject.MorphologicalDataOA;
					realFlid = MoMorphDataTags.kflidCompoundRules;
					fontName = defAnalWsFontName;
					typeSize = GetFontHeightFromStylesheet(cache, propertyTable, true);
					break;

				case "Features":
					owningObject = cache.LanguageProject.MsFeatureSystemOA;
					realFlid = FsFeatureSystemTags.kflidFeatures;
					fontName = defAnalWsFontName;
					typeSize = GetFontHeightFromStylesheet(cache, propertyTable, true);
					break;

				case "FeatureTypes":
					owningObject = cache.LanguageProject.MsFeatureSystemOA;
					realFlid = FsFeatureSystemTags.kflidTypes;
					fontName = defAnalWsFontName;
					typeSize = GetFontHeightFromStylesheet(cache, propertyTable, true);
					break;

				case "ProdRestrict":
					owningObject = cache.LanguageProject.MorphologicalDataOA;
					realFlid = MoMorphDataTags.kflidProdRestrict;
					fontName = defAnalWsFontName;
					typeSize = GetFontHeightFromStylesheet(cache, propertyTable, true);
					break;

				case "Problems":
					owningObject = cache.LanguageProject;
					realFlid = LangProjectTags.kflidAnnotations;
					fontName = defAnalWsFontName;
					typeSize = GetFontHeightFromStylesheet(cache, propertyTable, true);
					break;
				case "Wordforms":
					owningObject = cache.LanguageProject;
					//realFlid = cache.MetaDataCacheAccessor.GetFieldId("LangProject", "Wordforms", false);
					realFlid = ObjectListPublisher.OwningFlid;
					break;
				case "ReversalIndexes":
					{
					owningObject = cache.LanguageProject.LexDbOA;
					realFlid = cache.DomainDataByFlid.MetaDataCache.GetFieldId("LexDb", "CurrentReversalIndices", false);
						break;
					}

				//dependent properties
				case "Analyses":
					{
						//TODO: HACK! making it show the first one.
						var wfRepository = cache.ServiceLocator.GetInstance<IWfiWordformRepository>();
						if (wfRepository.Count > 0)
							owningObject = wfRepository.AllInstances().First();
						realFlid = WfiWordformTags.kflidAnalyses;
						break;
					}
				case "SemanticDomainList":
					owningObject = cache.LanguageProject.SemanticDomainListOA;
					realFlid = CmPossibilityListTags.kflidPossibilities;
					break;
				case "AllSenses":
				case "AllEntryRefs":
					{
						owningObject = cache.LanguageProject.LexDbOA;
						realFlid = cache.DomainDataByFlid.MetaDataCache.GetFieldId("LexDb", name, false);
						// Todo: something about initial sorting...
						break;
					}
#if NEEDED // not ported from old FW because not currently used.
				case "AllPossibleAllomorphs":
					{
						owningObject = cache.LangProject.LexDbOA;
						realFlid = BaseVirtualHandler.GetInstalledHandlerTag(cache, "LexDb", "AllPossibleAllomorphs");
						// Todo: something about initial sorting...
						break;
					}
#endif
			}

			return realFlid;
		}

		internal static void GetDefaultFontNameAndSize(bool analysisWs, LcmCache cache, PropertyTable propertyTable, out string fontName, out int typeSize)
		{
			IWritingSystemContainer wsContainer = cache.ServiceLocator.WritingSystems;
			fontName = analysisWs
				? wsContainer.DefaultAnalysisWritingSystem.DefaultFontName
				: wsContainer.DefaultVernacularWritingSystem.DefaultFontName;
			typeSize = GetFontHeightFromStylesheet(cache, propertyTable, analysisWs);
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
				LcmCache cache = m_cache;
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
			var repo = Cache.ServiceLocator.ObjectRepository;
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
			if (Cache.ServiceLocator.ObjectRepository.InstancesCreatedThisSession(ListItemsClass))
				return false;
			ArrayList items;
			using (var stream = new StreamReader(pathname))
			{
				items = ManyOnePathSortItem.ReadItems(stream, Cache.ServiceLocator.ObjectRepository);
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
			var persistedCurrentIndex = m_propertyTable.GetIntProperty(Clerk.PersistedIndexProperty, 0, PropertyTable.SettingsGroup.LocalSettings);
			if (persistedCurrentIndex >= numberOfObjectsInList)
				persistedCurrentIndex = numberOfObjectsInList - 1;
			return persistedCurrentIndex;
		}
	}

	/// <summary>
	/// This is an interface that Virtual List Publisher decorators (specified in the decoratorClass child
	/// of a recordList element in the XML configuration) may implement if they want to be passed the
	/// mediator.
	/// </summary>
	public interface ISetMediator
	{
		void SetMediator(Mediator mediator, PropertyTable propertyTable);
	}

	/// <summary>
	/// Similarly an interface that Virtual List Publisher decorators may implement if they need
	/// to be notified of the root object that their virtual property applies to.
	/// </summary>
	interface ISetRootHvo
	{
		void SetRootHvo(int hvo);
	}

	/// <summary>
	/// Similarly an interface that Virtual List Publisher decorators may implement if they need
	/// to be notified of the Cache.
	/// </summary>
	public interface ISetCache
	{
		void SetCache(LcmCache cache);
	}
}
