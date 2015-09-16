// Copyright (c) 2004-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.Filters;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// This is a specialty subclass for grabbing all of the items from a possibility list.
	/// </summary>
	public class PossibilityRecordList : RecordList
	{
		/// <summary>
		/// Constructor for a list that is owned or not.
		/// </summary>
		internal PossibilityRecordList(ISilDataAccessManaged decorator, ICmPossibilityList ownedPossibilityList)
			: base(decorator, true, 0, ownedPossibilityList, string.Empty)
		{
			ConstructorCommon();
		}

		private void ConstructorCommon()
		{
			m_usingAnalysisWs = true;
			m_oldLength = 0;
			m_flid = CmPossibilityListTags.kflidPossibilities;
			m_sorter = new PropertyRecordSorter("ShortName");
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

			m_fontName = m_cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.DefaultFontName;
			m_typeSize = GetFontHeightFromStylesheet(m_cache, PropertyTable, true);
		}

		#endregion

		private ICmPossibilityList OwningList
		{
			get { return (ICmPossibilityList)m_owningObject; }
		}

		protected override bool ListAlreadySorted
		{
			get
			{
				return !OwningList.IsSorted;
			}
		}

		protected override IEnumerable<int> GetObjectSet()
		{
			return from obj in OwningList.ReallyReallyAllPossibilities select obj.Hvo;
		}

		protected override ClassAndPropInfo GetMatchingClass(string className)
		{
			// A possibility list only allows one type of possibility to be owned in the list.
			var pssl = OwningList;
			var possClass = pssl.ItemClsid;
			var sPossClass = VirtualListPublisher.MetaDataCache.GetClassName(possClass);
			// for the special case of the VariantEntryTypes list, allow inserting a LexEntryInflType object
			// as long as the currently selected object has a parent of that class.
			if (m_owningObject.Guid == new Guid(LangProjectTags.kguidLexVariantTypes))
			{
				// return null only if the class of the owner does not match the given className
				// in case of owner being the list itself, use the general class for the list items.
				var currentObjOwner = CurrentObject.Owner;
				var classNameOfOwnerOfCurrentObject = currentObjOwner.Hvo == pssl.Hvo
					? sPossClass
					: currentObjOwner.ClassName;
				if (classNameOfOwnerOfCurrentObject != className)
					return null;
			}
			else if (sPossClass != className)
			{
				return null;
			}

			return m_insertableClasses.FirstOrDefault(cpi => cpi.signatureClassName == className);
		}

#if RANDYTODO
	/// <summary>
	/// Adjust the name of the menu item if appropriate. PossibilityRecordList overrides.
	/// </summary>
	/// <param name="command"></param>
	/// <param name="display"></param>
		public override void AdjustInsertCommandName(Command command, UIItemDisplayProperties display)
		{
			CheckDisposed();

			var pssl = OwningList;
			var owningFieldName = pssl.Name.BestAnalysisAlternative.Text;
			if (pssl.OwningFlid != 0)
				owningFieldName = VirtualListPublisher.MetaDataCache.GetFieldName(pssl.OwningFlid);
			var itemTypeName = pssl.ItemsTypeName();
			if (itemTypeName != "*" + owningFieldName + "*")
				display.Text = "_" + itemTypeName;	// prepend a keyboard accelarator marker
			var toolTipInsert = display.Text.Replace("_", string.Empty);	// strip any menu keyboard accelerator marker;
			command.ToolTipInsert = toolTipInsert.ToLower();
		}
#endif

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

			if (tag == CmPossibilityTags.kflidSubPossibilities || tag == CmPossibilityListTags.kflidPossibilities
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
			else if (tag == CmPossibilityTags.kflidName || tag == CmPossibilityTags.kflidAbbreviation)
			{
				if (Clerk.BarHandler is TreeBarHandler)
				{
					if (((TreeBarHandler)Clerk.BarHandler).IsHvoATreeNode(hvo))
					{
						UpdateListItemName(hvo);
					}
				}
				else
				{
					var hvoTargets = new List<int>(new[] {hvo});
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
				for (var i = 0; i < m_sortedObjects.Count; i++)
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
				var lastTopIndex = -1;
				for (var i = m_sortedObjects.Count; --i >= 0 ;)
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
		/// <param name="index"></param>
		/// <returns></returns>
		int LastChild(int hvoPoss, int index)
		{
			var pss = m_cache.ServiceLocator.GetInstance<ICmPossibilityRepository>().GetObject(hvoPoss);
			var count = pss.SubPossibilitiesOS.Count;
			if (count == 0)
				return index; // no children

			// Find the last child that occurs in the list.
			for (var ichild = count; --ichild >= 0; )
			{
				var hvoChild = pss.SubPossibilitiesOS[ichild].Hvo;
				var index1 = IndexOf(hvoChild);
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
				var hvoCurrent = PossibilityAt(CurrentIndex).Hvo;
				var curr = m_cache.ServiceLocator.GetInstance<ICmPossibilityRepository>().GetObject(hvoCurrent);
				var count = curr.SubPossibilitiesOS.Count;
				for (var ichild = 0; ichild < count; ichild++)
				{

					var index = IndexOf(curr.SubPossibilitiesOS[ichild].Hvo);
					if (index >= 0)
						return index;
				}

				for ( ; ; )
				{
					// look for a sibling of hvoCurrentBeforeGetObjectSet coming after the starting point.
					var currentObj = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoCurrent);
					var hvoOwner = currentObj.Owner.Hvo;
					var flidOwner = currentObj.OwningFlid;
					var fGotCurrent = false;

					foreach (var hvoChild in VirtualListPublisher.VecProp(hvoOwner, flidOwner))
					{
						if (hvoChild == hvoCurrent)
						{
							fGotCurrent = true;
							continue;
						}
						if (!fGotCurrent)
							continue; // skip items before current one.
						var index = IndexOf(hvoChild);
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
				var pss = PossibilityAt(CurrentIndex);
				var hvoCurrent = pss.Hvo;
				var hvoOwner = pss.Owner.Hvo;
				// Look for a previous sibling in the list.

				var flidOwner = pss.OwningFlid;
				if (flidOwner == 0)
					return CurrentIndex;
				var contents = VirtualListPublisher.VecProp(hvoOwner, flidOwner);
				var count = contents.Length;
				var fGotCurrent = false;
				for (var ichild = count; --ichild >= 0; )
				{
					var hvoChild = contents[ichild];
					if (hvoChild == hvoCurrent)
					{
						fGotCurrent = true;
						continue;
					}
					if (!fGotCurrent)
						continue; // skip items after current one.
					var index = IndexOf(hvoChild);
					if (index >= 0)
						return LastChild(hvoChild, index);
				}

				// OK, no sibling. Return owner if it's in the list.
				var index1 = IndexOf(hvoOwner);
				return (index1 >= 0) ? index1 : CurrentIndex;
			}
		}
		#endregion
	}
}