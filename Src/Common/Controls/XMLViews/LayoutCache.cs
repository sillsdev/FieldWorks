using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Diagnostics;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.Filters;
using XCore;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary>
	/// This class caches the layout and part inventories and optimizes looking up a particular item.
	/// </summary>
	public class LayoutCache
	{
		IFwMetaDataCache m_mdc;
		Inventory m_layoutInventory;
		Inventory m_partInventory;
		Dictionary<Tuple<int, string, bool>, XmlNode> m_map = new Dictionary<Tuple<int, string, bool>, XmlNode>();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="LayoutCache"/> class.
		/// </summary>
		/// <param name="mdc">The MDC.</param>
		/// <param name="layouts">The layouts.</param>
		/// <param name="parts">The parts.</param>
		/// ------------------------------------------------------------------------------------
		public LayoutCache(IFwMetaDataCache mdc, Inventory layouts, Inventory parts)
		{
			m_mdc = mdc;
			m_layoutInventory = layouts;
			m_partInventory = parts;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="LayoutCache"/> class.
		/// </summary>
		/// <param name="mdc">The MDC.</param>
		/// <param name="sDatabase">The database name.</param>
		/// <param name="app">The application.</param>
		/// <param name="projectPath">The project folder.</param>
		/// ------------------------------------------------------------------------------------
		public LayoutCache(IFwMetaDataCache mdc, string sDatabase, IApp app, String projectPath)
		{
			m_mdc = mdc;
			m_layoutInventory = Inventory.GetInventory("layouts", sDatabase);
			m_partInventory = Inventory.GetInventory("parts", sDatabase);
			if (m_layoutInventory == null || m_partInventory == null)
			{
				InitializePartInventories(sDatabase, app, projectPath);
				m_layoutInventory = Inventory.GetInventory("layouts", sDatabase);
				m_partInventory = Inventory.GetInventory("parts", sDatabase);
			}
		}

		/// <summary>
		/// Layout Version Number (last updated by JohnT, 27 Feb 2012, to fix LT-12684).
		/// </summary>
		/// <remarks>Note: often we also want to update BrowseViewer.kBrowseViewVersion.</remarks>
		public const int LayoutVersionNumber = 18;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the part inventories.
		/// </summary>
		/// <param name="sDatabase">The name of the database.</param>
		/// <param name="app">The application.</param>
		/// <param name="projectPath">The path to the project folder.</param>
		/// ------------------------------------------------------------------------------------
		public static void InitializePartInventories(string sDatabase, IApp app, String projectPath)
		{
			InitializePartInventories(sDatabase, app, true, projectPath);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the part inventories.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void InitializePartInventories(string sDatabase,
			IApp app, bool fLoadUserOverrides, String projectPath)
		{
			Debug.Assert(app != null, "app cannot be null");

			string partDirectory = Path.Combine(DirectoryFinder.FlexFolder,
				Path.Combine("Configuration", "Parts"));
			var keyAttrs = new Dictionary<string, string[]>();
			keyAttrs["layout"] = new[] {"class", "type", "name", "choiceGuid" };
			keyAttrs["group"] = new[] {"label"};
			keyAttrs["part"] = new[] {"ref"};


			var layoutInventory = new Inventory(new[] {partDirectory},
				"*Layouts.xml", "/LayoutInventory/*", keyAttrs, app.ApplicationName, projectPath);

			layoutInventory.Merger = new LayoutMerger();
			// Holding shift key means don't use extant preference file, no matter what.
			// This includes user overrides of layouts.
			if (fLoadUserOverrides &&
				System.Windows.Forms.Control.ModifierKeys != System.Windows.Forms.Keys.Shift)
			{
				layoutInventory.LoadUserOverrides(LayoutVersionNumber, sDatabase);
			}
			else
			{
				layoutInventory.DeleteUserOverrides(sDatabase);
				// LT-11193: The above may leave some user defined dictionary views to be loaded.
				layoutInventory.LoadUserOverrides(LayoutVersionNumber, sDatabase);
			}
			Inventory.SetInventory("layouts", sDatabase, layoutInventory);

			keyAttrs = new Dictionary<string, string[]>();
			keyAttrs["part"] = new[] {"id"};

			Inventory.SetInventory("parts", sDatabase, new Inventory(new[] {partDirectory},
				"*Parts.xml", "/PartInventory/bin/*", keyAttrs, app.ApplicationName, projectPath));
		}

		/// <summary>
		/// Displaying Reversal Indexes requires expanding a variable number of writing system
		/// specific layouts.  This method does that for a specific writing system and database.
		/// </summary>
		/// <param name="sWsTag"></param>
		/// <param name="sDatabase"></param>
		public static void InitializeLayoutsForWsTag(string sWsTag, string sDatabase)
		{
			Inventory layouts = Inventory.GetInventory("layouts", sDatabase);
			if (layouts != null)
				layouts.ExpandWsTaggedNodes(sWsTag);
		}

		static char[] ktagMarkers = new [] {'-', Inventory.kcMarkLayoutCopy, Inventory.kcMarkNodeCopy};

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the node.
		/// </summary>
		/// <param name="clsid">The CLSID.</param>
		/// <param name="layoutName">Name of the layout.</param>
		/// <param name="fIncludeLayouts">if set to <c>true</c> [f include layouts].</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public XmlNode GetNode(int clsid, string layoutName, bool fIncludeLayouts)
		{
			Tuple<int, string, bool> key = Tuple.Create(clsid, layoutName, fIncludeLayouts);
			if (m_map.ContainsKey(key))
				return m_map[key];

			XmlNode node;
			int classId = clsid;
			string useName = layoutName ?? "default";
			string origName = useName;
			for( ; ; )
			{
				string classname = m_mdc.GetClassName(classId);
				if (fIncludeLayouts)
				{
					// Inventory of layouts has keys class, type, name
					node = m_layoutInventory.GetElement("layout", new[] {classname, "jtview", useName, null});
					if (node != null)
						break;
				}
				// inventory of parts has key id.
				node = m_partInventory.GetElement("part", new[] {classname + "-Jt-" + useName});
				if (node != null)
					break;
				if (classId == 0 && useName == origName)
				{
					// This is somewhat by way of robustness. When we generate a modified layout name we should generate
					// a modified layout to match. If something slips through the cracks, use the unmodified original
					// view in preference to a default view of Object.
					int index = origName.IndexOfAny(ktagMarkers);
					if (index > 0)
					{
						useName = origName.Substring(0, index);
						classId = clsid;
						continue;
					}
				}
				if (classId == 0 && useName != "default")
				{
					// Nothing found all the way to CmObject...try default layout.
					useName = "default";
					classId = clsid;
					continue; // try again with the main class, don't go to its base class at once.
				}
				if (classId == 0)
				{
					if (fIncludeLayouts)
					{
						// Really surprising...default view not found on CmObject??
						throw new ApplicationException("No matching layout found for class " + classname + " jtview layout " + origName);
					}
					// okay to not find specific custom parts...we can generate them.
					return null;
				}
				// Otherwise try superclass.
				classId = m_mdc.GetBaseClsId(classId);
			}
			m_map[key] = node; // find faster next time!
			return node;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the layout inventory.
		/// </summary>
		/// <value>The layout inventory.</value>
		/// ------------------------------------------------------------------------------------
		public Inventory LayoutInventory
		{
			get
			{
				return m_layoutInventory;
			}
		}
	}

	/// <summary>
	/// An interface for lists that can switch between multiple lists of items.
	/// </summary>
	public interface IMultiListSortItemProvider : ISortItemProvider
	{
		/// <summary>
		/// A token to store with items returned from FindCorrespondingItemsInCurrentList()
		/// that can be passed back into that interface to help convert those
		/// items to the relatives in the current list (i.e.
		/// associated with a different ListSourceToken)
		/// </summary>
		object ListSourceToken { get; }

		/// <summary>
		/// The specification that can be used to create a PartOwnershipTree helper.
		/// </summary>
		XmlNode PartOwnershipTreeSpec { get;  }

		/// <summary>
		///
		/// </summary>
		/// <param name="itemAndListSourceTokenPairs"></param>
		/// <returns>a set of hvos of (non-sibling) items related to those given in itemAndListSourceTokenPairs</returns>
		void ConvertItemsToRelativesThatApplyToCurrentList(ref IDictionary<int, object> itemAndListSourceTokenPairs);
	}

	/// <summary>
	/// Describes the relationship between list classes in a PartOwnershipTree
	/// </summary>
	public enum RelationshipOfRelatives
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

	/// <summary>
	/// Helper for handling switching between related (ListItemClass) lists.
	/// </summary>
	public class PartOwnershipTree : FwDisposableBase
	{
		FdoCache m_cache = null;
		XmlNode m_classOwnershipTree = null;
		XmlNode m_parentToChildrenSpecs = null;

		/// <summary>
		/// Factory for returning a PartOwnershipTree
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="sortItemProvider"></param>
		/// <param name="fReturnFirstDecendentOnly"></param>
		/// <returns></returns>
		static public PartOwnershipTree Create(FdoCache cache, IMultiListSortItemProvider sortItemProvider, bool fReturnFirstDecendentOnly)
		{
			return new PartOwnershipTree(cache, sortItemProvider, fReturnFirstDecendentOnly);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="sortItemProvider"></param>
		/// <param name="fReturnFirstDecendentOnly"></param>
		private PartOwnershipTree(FdoCache cache, IMultiListSortItemProvider sortItemProvider, bool fReturnFirstDecendentOnly)
		{
			XmlNode partOwnershipTreeSpec = sortItemProvider.PartOwnershipTreeSpec;
			m_cache = cache;
			m_classOwnershipTree = partOwnershipTreeSpec.SelectSingleNode(".//ClassOwnershipTree");
			XmlNode parentClassPathsToChildren = partOwnershipTreeSpec.SelectSingleNode(".//ParentClassPathsToChildren");
			m_parentToChildrenSpecs = parentClassPathsToChildren.Clone();
			// now go through the seq specs and set the "firstOnly" to the requested value.
			XmlNodeList xnl = m_parentToChildrenSpecs.SelectNodes(".//seq");
			if (xnl == null)
				return;
			foreach (XmlElement xe in xnl)
			{
				XmlAttribute xaFirstOnly = xe.Attributes["firstOnly"];
				if (xaFirstOnly == null)
				{
					// create the first only attribute
					xaFirstOnly = xe.OwnerDocument.CreateAttribute("firstOnly");
					xe.Attributes.Append(xaFirstOnly);
				}
				xaFirstOnly.Value = fReturnFirstDecendentOnly.ToString().ToLowerInvariant();
			}
		}

		private FdoCache Cache
		{
			get { return m_cache; }
		}

		#region FwDisposableBase overrides
		/// <summary>
		///
		/// </summary>
		protected override void DisposeUnmanagedResources()
		{
			m_cache = null;
			m_classOwnershipTree = null;
			m_parentToChildrenSpecs = null;
		}
		#endregion FwDisposableBase overrides


		/// <summary>
		/// Get the field name that should be used for the main record list when we want to edit the specified field
		/// of the specified object class. TargetFieldId may be 0 to get the default (or only) main record list field
		/// for the specified class.
		/// </summary>
		public string GetSourceFieldName(int targetClsId, int targetFieldId)
		{
			string flidName;
			string targetClassName = m_cache.DomainDataByFlid.MetaDataCache.GetClassName(targetClsId);
			XmlNode classNode = m_classOwnershipTree.SelectSingleNode(".//" + targetClassName);
			flidName = XmlUtils.GetManditoryAttributeValue(classNode, "sourceField");
			if (targetFieldId != 0)
			{
				var altSourceField = XmlUtils.GetOptionalAttributeValue(classNode, "altSourceField");
				if (altSourceField != null)
				{
					var targetFieldName = m_cache.MetaDataCacheAccessor.GetFieldName(targetFieldId);
					foreach (var option in altSourceField.Split(';'))
					{
						var parts = option.Split(':');
						if (parts.Length != 2)
							throw new ConfigurationException("altSourceField must contain Field:SourceField;Field:SourceField...");
						if (parts[0].Trim() == targetFieldName)
						{
							flidName = parts[1].Trim();
							break;
						}
					}
				}
			}
			return flidName;
		}

		/// <summary>
		/// Map itemsBeforeListChange (associated with flidForItemsBeforeListChange)
		/// to those in the current list (associated with flidForCurrentList)
		/// and provide a set of common ancestors.
		/// </summary>
		/// <param name="flidForItemsBeforeListChange"></param>
		/// <param name="itemsBeforeListChange"></param>
		/// <param name="flidForCurrentList"></param>
		/// <param name="commonAncestors"></param>
		/// <returns></returns>
		public Set<int> FindCorrespondingItemsInCurrentList(int flidForItemsBeforeListChange, Set<int> itemsBeforeListChange, int flidForCurrentList, out Set<int> commonAncestors)
		{
			Set<int> relatives = new Set<int>();
			commonAncestors = new Set<int>();
			int newListItemsClass = GhostParentHelper.GetBulkEditDestinationClass(Cache, flidForCurrentList);
			int prevListItemsClass = GhostParentHelper.GetBulkEditDestinationClass(Cache, flidForItemsBeforeListChange);
			RelationshipOfRelatives relationshipOfTarget = FindTreeRelationship(prevListItemsClass, newListItemsClass);
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
						GhostParentHelper gph = GetGhostParentHelper(flidForItemsBeforeListChange);
						// the items (e.g. senses) are owned by the new class (e.g. entry),
						// so find the (new class) ancestor for each item.
						foreach (int hvoBeforeListChange in itemsBeforeListChange)
						{
							int hvoAncestorOfItem;
							if (gph != null && gph.GhostOwnerClass == newListItemsClass &&
								gph.IsGhostOwnerClass(hvoBeforeListChange))
							{
								// just add the ghost owner, as the ancestor relative,
								// since it's already in the newListItemsClass
								hvoAncestorOfItem = hvoBeforeListChange;
							}
							else
							{
								var obj =
									Cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoBeforeListChange);
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
						HashSet<int> newClasses =
							new HashSet<int>(((IFwMetaDataCacheManaged)Cache.MetaDataCacheAccessor).GetAllSubclasses(newListItemsClass));
						foreach (int hvoBeforeListChange in itemsBeforeListChange)
						{
							if (!Cache.ServiceLocator.IsValidObjectId(hvoBeforeListChange))
								continue; // skip this one.
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
								hvoCommonAncestor = GetHvoCommonAncestor(hvoBeforeListChange,
																		 prevListItemsClass, newListItemsClass);
							}

							// only add the descendants/cousins if we haven't already processed the ancestor.
							if (!commonAncestors.Contains(hvoCommonAncestor))
							{
								GhostParentHelper gph = GetGhostParentHelper(flidForCurrentList);
								Set<int> descendents = GetDescendents(hvoCommonAncestor, flidForCurrentList);
								if (descendents.Count > 0)
								{
									relatives.AddRange(descendents);
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

		private Set<int> GetDescendents(int hvoCommonAncestor, int relativesFlid)
		{
			string listPropertyName = Cache.MetaDataCacheAccessor.GetFieldName(relativesFlid);
			string parentObjName = Cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoCommonAncestor).ClassName;
			string xpathToPart = "./part[@id='" + parentObjName + "-Jt-" + listPropertyName + "']";
			XmlNode pathSpec = m_parentToChildrenSpecs.SelectSingleNode(xpathToPart);
			Debug.Assert(pathSpec != null,
				String.Format("You are experiencing a rare and difficult-to-reproduce error (LT- 11443 and linked issues). If you can add any information to the issue or fix it please do. If JohnT is available please call him over. Expected to find part ({0}) in ParentClassPathsToChildren", xpathToPart));
			if (pathSpec == null)
				return new Set<int>(); // This just means we don't find a related object. Better than crashing, but not what we intend.
			// get the part spec that gives us the path from obsolete current (parent) list item object
			// to the new one.
			var vc = new XmlBrowseViewBaseVc(m_cache, null);
			var parentItem = new ManyOnePathSortItem(hvoCommonAncestor, null, null);
			var collector = new XmlBrowseViewBaseVc.ItemsCollectorEnv(null, m_cache, hvoCommonAncestor);
			vc.DisplayCell(parentItem, pathSpec, hvoCommonAncestor, collector);
			if (collector.HvosCollectedInCell != null && collector.HvosCollectedInCell.Count > 0)
			{
				return collector.HvosCollectedInCell;
			}
			return new Set<int>();
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
				string newClassName = Cache.DomainDataByFlid.MetaDataCache.GetClassName(newListItemsClass);
				string prevClassName = Cache.DomainDataByFlid.MetaDataCache.GetClassName(prevListItemsClass);
				XmlNode prevClassNode = m_classOwnershipTree.SelectSingleNode(".//" + prevClassName);
				XmlNode newClassNode = m_classOwnershipTree.SelectSingleNode(".//" + newClassName);
				// determine if prevClassName is owned (has anscestor) by the new.
				bool fNewIsAncestorOfPrev = prevClassNode.SelectSingleNode("ancestor::" + newClassName) != null;
				if (fNewIsAncestorOfPrev)
				{
					relationshipOfTarget = RelationshipOfRelatives.Ancestor;
				}
				else
				{
					// okay, now find most related object in new items list.
					bool fNewIsChildOfPrev = newClassNode.SelectSingleNode("ancestor::" + prevClassName) != null;
					if (fNewIsChildOfPrev)
					{
						relationshipOfTarget = RelationshipOfRelatives.Descendent;
					}
					else
					{
						relationshipOfTarget = RelationshipOfRelatives.Cousin;
					}
				}
			}
			return relationshipOfTarget;
		}

		private XmlNode GetTreeNode(int classId)
		{
			string className = Cache.DomainDataByFlid.MetaDataCache.GetClassName(classId);
			return m_classOwnershipTree.SelectSingleNode(".//" + className);
		}

		private int GetHvoCommonAncestor(int hvoBeforeListChange, int prevListItemsClass, int newListItemsClass)
		{
			XmlNode prevClassNode = GetTreeNode(prevListItemsClass);
			XmlNode newClassNode = GetTreeNode(newListItemsClass);
			int hvoCommonAncestor = 0;
			// NOTE: the class of hvoBeforeListChange could be different then prevListItemsClass, if the item is a ghost (owner).
			int classOfHvoBeforeListChange = Cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoBeforeListChange).ClassID;
			// so go up the parent of the previous one until it's an ancestor of the newClass.
			XmlNode ancestorOfPrev = prevClassNode.ParentNode;
			while (ancestorOfPrev != null)
			{
				if (newClassNode.SelectSingleNode("ancestor::" + ancestorOfPrev.Name) != null)
				{
					XmlNode commonAncestor = ancestorOfPrev;
					var classCommonAncestor = Cache.MetaDataCacheAccessor.GetClassId(commonAncestor.Name);
					if (DomainObjectServices.IsSameOrSubclassOf(Cache.DomainDataByFlid.MetaDataCache, classOfHvoBeforeListChange, classCommonAncestor))
						hvoCommonAncestor = hvoBeforeListChange;
					else
					{
						var obj = Cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoBeforeListChange);
						hvoCommonAncestor = obj.OwnerOfClass(classCommonAncestor).Hvo;
					}
					break;
				}
				ancestorOfPrev = ancestorOfPrev.ParentNode;
			}
			return hvoCommonAncestor;
		}
	}
}
