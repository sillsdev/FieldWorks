// Copyright (c) 2004-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Xml.XPath;
using LanguageExplorer.Controls.XMLViews;
using LanguageExplorer.Filters;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.DomainImpl;
using SIL.LCModel.DomainServices;
using SIL.ObjectModel;
using SIL.Xml;

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
		PartOwnershipTree _partOwnershipTree;
		// suspend loading the property until given a class by RecordBrowseView via
		// RecordList.OnChangeListItemsClass();
		bool m_suspendReloadUntilOnChangeListItemsClass = true;

		/// <summary />
		internal EntriesOrChildClassesRecordList(string id, StatusBar statusBar, ISilDataAccessManaged decorator, ILexDb owner)
			: base(id, statusBar, decorator, false, new VectorPropertyParameterObject(owner, "Entries", decorator.MetaDataCache.GetFieldId(LexDbTags.kClassName, "Entries", false)), new Dictionary<string, PropertyRecordSorter>
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
			_partOwnershipTree = new PartOwnershipTree(m_cache, true);
			if (!m_reloadNeededForProperty.ContainsKey(m_flid))
			{
				m_reloadNeededForProperty.Add(m_flid, false);
			}
			m_flidEntries = m_flid;
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
				_partOwnershipTree?.Dispose();
			}
			_partOwnershipTree = null;

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
			var relatives = _partOwnershipTree.FindCorrespondingItemsInCurrentList(m_prevFlid, new HashSet<int>(new[] { hvoCurrentBeforeGetObjectSet }), m_flid, out commonAncestors);
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
		public void ConvertItemsToRelativesThatApplyToCurrentList(ref IDictionary<int, object> oldItems)
		{
			var oldItemsToRemove = new HashSet<int>();
			var itemsToAdd = new HashSet<int>();
			// Create a PartOwnershipTree in a mode that can return more than one descendent relative.
			using (var partOwnershipTree = new PartOwnershipTree(m_cache, false))
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
					var relatives = FindCorrespondingItemsInCurrentList(dictOneOldItem, partOwnershipTree);
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

		/// <summary>
		/// Helper for handling switching between related (ListItemClass) lists.
		/// </summary>
		private sealed class PartOwnershipTree : DisposableBase
		{
			private XElement _classOwnershipTreeElement;
			private XElement _parentClassPathsToChildrenElementClone;
			private LcmCache Cache { get; set; }

			/// <summary />
			internal PartOwnershipTree(LcmCache cache, bool returnFirstDescendentOnly)
			{
				var returnFirstDescendentOnlyAsString = returnFirstDescendentOnly.ToString().ToLowerInvariant();
				var partOwnershipTreeSpec = XDocument.Parse(LexiconResources.EntriesOrChildrenClerkPartOwnershipTree).Root;
				Cache = cache;
				_classOwnershipTreeElement = partOwnershipTreeSpec.Element("ClassOwnershipTree");
				_parentClassPathsToChildrenElementClone = partOwnershipTreeSpec.Element("ParentClassPathsToChildren").Clone();
				// now go through the seq specs and set the "firstOnly" to the requested value.
				foreach (var seqElement in _parentClassPathsToChildrenElementClone.Elements("part").Elements("seq"))
				{
					var firstOnlyAttribute = seqElement.Attribute("firstOnly");
					if (firstOnlyAttribute == null)
					{
						// Create the first only attribute, with no value (reset soon).
						firstOnlyAttribute = new XAttribute("firstOnly", string.Empty);
						seqElement.Add(firstOnlyAttribute);
					}
					firstOnlyAttribute.Value = returnFirstDescendentOnlyAsString;
				}
			}

			#region DisposableBase overrides

			/// <summary />
			protected override void DisposeUnmanagedResources()
			{
				Cache = null;
				_classOwnershipTreeElement = null;
				_parentClassPathsToChildrenElementClone = null;
			}

			/// <inheritdoc />
			protected override void Dispose(bool disposing)
			{
				Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + " ******");
				base.Dispose(disposing);
			}

			#endregion DisposableBase overrides

			/// <summary>
			/// Get the field name that should be used for the main record list when we want to edit the specified field
			/// of the specified object class. TargetFieldId may be 0 to get the default (or only) main record list field
			/// for the specified class.
			/// </summary>
			public string GetSourceFieldName(int targetClsId, int targetFieldId)
			{
				var targetClassName = Cache.DomainDataByFlid.MetaDataCache.GetClassName(targetClsId);
				var classNode = _classOwnershipTreeElement.Descendants(targetClassName).First();
				var flidName = XmlUtils.GetMandatoryAttributeValue(classNode, "sourceField");
				if (targetFieldId == 0)
				{
					return flidName;
				}
				var altSourceField = XmlUtils.GetOptionalAttributeValue(classNode, "altSourceField");
				if (altSourceField == null)
				{
					return flidName;
				}
				var targetFieldName = Cache.MetaDataCacheAccessor.GetFieldName(targetFieldId);
				foreach (var option in altSourceField.Split(';'))
				{
					var parts = option.Split(':');
					if (parts.Length != 2)
					{
						throw new FwConfigurationException("altSourceField must contain Field:SourceField;Field:SourceField...");
					}
					if (parts[0].Trim() == targetFieldName)
					{
						flidName = parts[1].Trim();
						break;
					}
				}
				return flidName;
			}

			/// <summary>
			/// Map itemsBeforeListChange (associated with flidForItemsBeforeListChange)
			/// to those in the current list (associated with flidForCurrentList)
			/// and provide a set of common ancestors.
			/// </summary>
			public ISet<int> FindCorrespondingItemsInCurrentList(int flidForItemsBeforeListChange, ISet<int> itemsBeforeListChange, int flidForCurrentList, out ISet<int> commonAncestors)
			{
				var relatives = new HashSet<int>();
				commonAncestors = new HashSet<int>();
				var newListItemsClass = GhostParentHelper.GetBulkEditDestinationClass(Cache, flidForCurrentList);
				var prevListItemsClass = GhostParentHelper.GetBulkEditDestinationClass(Cache, flidForItemsBeforeListChange);
				var relationshipOfTarget = FindTreeRelationship(prevListItemsClass, newListItemsClass);
				// if new listListItemsClass is same as the given object, there's nothing more we have to do.
				switch (relationshipOfTarget)
				{
					case RelationshipOfRelatives.Sibling:
						{
							Debug.Fail("Sibling relationships are not supported.");
							// no use for this currently.
							break;
						}
					case RelationshipOfRelatives.Ancestor:
						{
							var gph = GetGhostParentHelper(flidForItemsBeforeListChange);
							// the items (e.g. senses) are owned by the new class (e.g. entry),
							// so find the (new class) ancestor for each item.
							foreach (var hvoBeforeListChange in itemsBeforeListChange)
							{
								int hvoAncestorOfItem;
								if (gph != null && gph.GhostOwnerClass == newListItemsClass && gph.IsGhostOwnerClass(hvoBeforeListChange))
								{
									// just add the ghost owner, as the ancestor relative,
									// since it's already in the newListItemsClass
									hvoAncestorOfItem = hvoBeforeListChange;
								}
								else
								{
									var obj = Cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoBeforeListChange);
									hvoAncestorOfItem = obj.OwnerOfClass(newListItemsClass).Hvo;
								}
								relatives.Add(hvoAncestorOfItem);
							}
							commonAncestors = relatives;
							break;
						}
					case RelationshipOfRelatives.Descendent:
					case RelationshipOfRelatives.Cousin:
						{
							var newClasses = new HashSet<int>(Cache.GetManagedMetaDataCache().GetAllSubclasses(newListItemsClass));
							foreach (var hvoBeforeListChange in itemsBeforeListChange)
							{
								if (!Cache.ServiceLocator.IsValidObjectId(hvoBeforeListChange))
								{
									continue; // skip this one.
								}
								if (newClasses.Contains(Cache.ServiceLocator.GetObject(hvoBeforeListChange).ClassID))
								{
									// strangely, the 'before' object is ALREADY one that is valid for, and presumably in,
									// the destination property. One way this happens is at startup, when switching to
									// the saved target column, but we have also saved the list of objects.
									relatives.Add(hvoBeforeListChange);
									continue;
								}
								int hvoCommonAncestor;
								if (relationshipOfTarget == RelationshipOfRelatives.Descendent)
								{
									// the item is the ancestor
									hvoCommonAncestor = hvoBeforeListChange;
								}
								else
								{
									// the item and its cousins have a common ancestor.
									hvoCommonAncestor = GetHvoCommonAncestor(hvoBeforeListChange, prevListItemsClass, newListItemsClass);
								}
								// only add the descendants/cousins if we haven't already processed the ancestor.
								if (!commonAncestors.Contains(hvoCommonAncestor))
								{
									var gph = GetGhostParentHelper(flidForCurrentList);
									var descendents = GetDescendents(hvoCommonAncestor, flidForCurrentList);
									if (descendents.Count > 0)
									{
										relatives.UnionWith(descendents);
									}
									else if (gph != null && gph.IsGhostOwnerClass(hvoCommonAncestor))
									{
										relatives.Add(hvoCommonAncestor);
									}
									commonAncestors.Add(hvoCommonAncestor);
								}
							}
							break;
						}
				}
				return relatives;
			}

			private GhostParentHelper GetGhostParentHelper(int flidToTry)
			{
				return GhostParentHelper.CreateIfPossible(Cache.ServiceLocator, flidToTry);
			}

			private ISet<int> GetDescendents(int hvoCommonAncestor, int relativesFlid)
			{
				var listPropertyName = Cache.MetaDataCacheAccessor.GetFieldName(relativesFlid);
				var parentObjName = Cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoCommonAncestor).ClassName;
				var xpathToPart = "./part[@id='" + parentObjName + "-Jt-" + listPropertyName + "']";
				var pathSpec = _parentClassPathsToChildrenElementClone.XPathSelectElement(xpathToPart);
				Debug.Assert(pathSpec != null, $"You are experiencing a rare and difficult-to-reproduce error (LT- 11443 and linked issues). If you can add any information to the issue or fix it please do. If JohnT is available please call him over. Expected to find part ({xpathToPart}) in ParentClassPathsToChildren");
				if (pathSpec == null)
				{
					return new HashSet<int>(); // This just means we don't find a related object. Better than crashing, but not what we intend.
				}
				// get the part spec that gives us the path from obsolete current (parent) list item object
				// to the new one.
				var vc = new XmlBrowseViewVc(Cache, null);
				var parentItem = new ManyOnePathSortItem(hvoCommonAncestor, null, null);
				var collector = new ItemsCollectorEnv(null, Cache.MainCacheAccessor, hvoCommonAncestor);
				var doc = XDocument.Load(pathSpec.ToString());
				vc.DisplayCell(parentItem, doc.Root.Elements().First(), hvoCommonAncestor, collector);
				if (collector.HvosCollectedInCell != null && collector.HvosCollectedInCell.Count > 0)
				{
					return new HashSet<int>(collector.HvosCollectedInCell);
				}
				return new HashSet<int>();
			}

			private RelationshipOfRelatives FindTreeRelationship(int prevListItemsClass, int newListItemsClass)
			{
				RelationshipOfRelatives relationshipOfTarget;
				if (DomainObjectServices.IsSameOrSubclassOf(Cache.DomainDataByFlid.MetaDataCache, prevListItemsClass, newListItemsClass))
				{
					relationshipOfTarget = RelationshipOfRelatives.Sibling;
				}
				else
				{
					// lookup new class in ownership tree and decide how to select the most related object
					var newClassName = Cache.DomainDataByFlid.MetaDataCache.GetClassName(newListItemsClass);
					var prevClassName = Cache.DomainDataByFlid.MetaDataCache.GetClassName(prevListItemsClass);
					var prevClassNode = _classOwnershipTreeElement.XPathSelectElement(".//" + prevClassName);
					var newClassNode = _classOwnershipTreeElement.XPathSelectElement(".//" + newClassName);
					// determine if prevClassName is owned (has ancestor) by the new.
					var fNewIsAncestorOfPrev = prevClassNode.XPathSelectElement("ancestor::" + newClassName) != null;
					if (fNewIsAncestorOfPrev)
					{
						relationshipOfTarget = RelationshipOfRelatives.Ancestor;
					}
					else
					{
						// okay, now find most related object in new items list.
						var fNewIsChildOfPrev = newClassNode.XPathSelectElement("ancestor::" + prevClassName) != null;
						relationshipOfTarget = fNewIsChildOfPrev ? RelationshipOfRelatives.Descendent : RelationshipOfRelatives.Cousin;
					}
				}
				return relationshipOfTarget;
			}

			private XElement GetTreeNode(int classId)
			{
				var className = Cache.DomainDataByFlid.MetaDataCache.GetClassName(classId);
				return _classOwnershipTreeElement.XPathSelectElement(".//" + className);
			}

			private int GetHvoCommonAncestor(int hvoBeforeListChange, int prevListItemsClass, int newListItemsClass)
			{
				var prevClassNode = GetTreeNode(prevListItemsClass);
				var newClassNode = GetTreeNode(newListItemsClass);
				var hvoCommonAncestor = 0;
				// NOTE: the class of hvoBeforeListChange could be different then prevListItemsClass, if the item is a ghost (owner).
				var classOfHvoBeforeListChange = Cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoBeforeListChange).ClassID;
				// so go up the parent of the previous one until it's an ancestor of the newClass.
				var ancestorOfPrev = prevClassNode.Parent;
				while (ancestorOfPrev != null)
				{
					if (newClassNode.XPathSelectElement("ancestor::" + ancestorOfPrev.Name) != null)
					{
						var commonAncestor = ancestorOfPrev;
						var classCommonAncestor = Cache.MetaDataCacheAccessor.GetClassId(commonAncestor.Name.ToString());
						if (DomainObjectServices.IsSameOrSubclassOf(Cache.DomainDataByFlid.MetaDataCache, classOfHvoBeforeListChange, classCommonAncestor))
						{
							hvoCommonAncestor = hvoBeforeListChange;
						}
						else
						{
							var obj = Cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoBeforeListChange);
							hvoCommonAncestor = obj.OwnerOfClass(classCommonAncestor).Hvo;
						}
						break;
					}
					ancestorOfPrev = ancestorOfPrev.Parent;
				}
				return hvoCommonAncestor;
			}

			/// <summary>
			/// Describes the relationship between list classes in a PartOwnershipTree
			/// </summary>
			private enum RelationshipOfRelatives
			{
				/// <summary>
				/// Items (e.g. Entries) in the same ItemsListClass (LexEntry) are siblings.
				/// </summary>
				Sibling,
				/// <summary>
				/// (Includes Parent relationship)
				/// </summary>
				Ancestor,
				/// <summary>
				/// (Includes Child relationship)
				/// </summary>
				Descendent,
				/// <summary>
				/// Entries.Allomorphs and Entries.Senses are cousins.
				/// </summary>
				Cousin
			}
		}
	}
}