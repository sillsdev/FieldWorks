// Copyright (c) 2009-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using LanguageExplorer.Filters;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.ObjectModel;
using SIL.Xml;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// Helper for handling switching between related (ListItemClass) lists.
	/// </summary>
	public class PartOwnershipTree : DisposableBase
	{
		private XElement m_classOwnershipTree;
		private XElement m_parentToChildrenSpecs;

		/// <summary>
		/// Factory for returning a PartOwnershipTree
		/// </summary>
		public static PartOwnershipTree Create(LcmCache cache, IMultiListSortItemProvider sortItemProvider, bool fReturnFirstDecendentOnly)
		{
			return new PartOwnershipTree(cache, sortItemProvider, fReturnFirstDecendentOnly);
		}

		/// <summary />
		private PartOwnershipTree(LcmCache cache, IMultiListSortItemProvider sortItemProvider, bool fReturnFirstDecendentOnly)
		{
			var partOwnershipTreeSpec = sortItemProvider.PartOwnershipTreeSpec;
			Cache = cache;
			m_classOwnershipTree = partOwnershipTreeSpec.Element("ClassOwnershipTree");
			var parentClassPathsToChildren = partOwnershipTreeSpec.Element("ParentClassPathsToChildren");
			m_parentToChildrenSpecs = parentClassPathsToChildren.Clone();
			// now go through the seq specs and set the "firstOnly" to the requested value.
			var seqElements = m_parentToChildrenSpecs.Elements("part").Elements("seq");
			foreach (var xe in seqElements)
			{
				var xaFirstOnly = xe.Attribute("firstOnly");
				if (xaFirstOnly == null)
				{
					// Create the first only attribute, with no value (reset soon).
					xaFirstOnly = new XAttribute("firstOnly", string.Empty);
					xe.Add(xaFirstOnly);
				}
				xaFirstOnly.Value = fReturnFirstDecendentOnly.ToString().ToLowerInvariant();
			}
		}

		private LcmCache Cache { get; set; }

		#region DisposableBase overrides

		/// <summary />
		protected override void DisposeUnmanagedResources()
		{
			Cache = null;
			m_classOwnershipTree = null;
			m_parentToChildrenSpecs = null;
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
			var classNode = m_classOwnershipTree.Descendants(targetClassName).First();
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
			var pathSpec = m_parentToChildrenSpecs.XPathSelectElement(xpathToPart);
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
				var prevClassNode = m_classOwnershipTree.XPathSelectElement(".//" + prevClassName);
				var newClassNode = m_classOwnershipTree.XPathSelectElement(".//" + newClassName);
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
			return m_classOwnershipTree.XPathSelectElement(".//" + className);
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