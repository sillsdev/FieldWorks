using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Runtime.InteropServices;
using System.Diagnostics;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.FDO;
using SIL.Utils;
using SIL.FieldWorks.Filters;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary>
	/// This class caches the layout and part inventories and optimizes looking up a particular item.
	/// </summary>
	public class LayoutCache : IFWDisposable
	{
		IFwMetaDataCache m_mdc;
		Inventory m_layoutInventory;
		Inventory m_partInventory;
		Dictionary<Triple, XmlNode> m_map = new Dictionary<Triple, XmlNode>();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:LayoutCache"/> class.
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
		/// Initializes a new instance of the <see cref="T:LayoutCache"/> class.
		/// </summary>
		/// <param name="mdc">The MDC.</param>
		/// <param name="sDatabase">The s database.</param>
		/// ------------------------------------------------------------------------------------
		public LayoutCache(IFwMetaDataCache mdc, string sDatabase)
		{
			InitializeDynamically(mdc, false, sDatabase);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:LayoutCache"/> class.
		/// </summary>
		/// <param name="mdc">The MDC.</param>
		/// <param name="fLoadFlexLayouts">if set to <c>true</c> [f load flex layouts].</param>
		/// <param name="sDatabase">The s database.</param>
		/// ------------------------------------------------------------------------------------
		public LayoutCache(IFwMetaDataCache mdc, bool fLoadFlexLayouts, string sDatabase)
		{
			InitializeDynamically(mdc, fLoadFlexLayouts, sDatabase);
		}

		private void InitializeDynamically(IFwMetaDataCache mdc, bool fLoadFlexLayouts, string sDatabase)
		{
			m_mdc = mdc;
			m_layoutInventory = Inventory.GetInventory("layouts", sDatabase);
			m_partInventory = Inventory.GetInventory("parts", sDatabase);
			if (m_layoutInventory == null || m_partInventory == null)
			{
				InitializePartInventories(fLoadFlexLayouts, sDatabase);
				m_layoutInventory = Inventory.GetInventory("layouts", sDatabase);
				m_partInventory = Inventory.GetInventory("parts", sDatabase);
			}
		}
		#region IDisposable & Co. implementation
		// Region last reviewed: never

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		/// True, if the object has been disposed.
		/// </summary>
		private bool m_isDisposed = false;

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed
		{
			get { return m_isDisposed; }
		}

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~LayoutCache()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <summary>
		///
		/// </summary>
		/// <remarks>Must not be virtual.</remarks>
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected virtual void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_map != null)
					m_map.Clear();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_mdc = null;
			m_layoutInventory = null;
			m_partInventory = null;
			m_map = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation


		/// <summary>
		/// Last update by SteveMc, May 26, 2009, for FW 6.0, due to changes in dictionary
		/// layout configuration options.
		/// </summary>
		/// <remarks>Note: often we also want to update BrowseViewer.kBrowseViewVersion.</remarks>
		public const int LayoutVersionNumber = 9;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the part inventories.
		/// </summary>
		/// <param name="sDatabase">The name of the database.</param>
		/// ------------------------------------------------------------------------------------
		public static void InitializePartInventories(string sDatabase)
		{
			InitializePartInventories(false, sDatabase);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the part inventories.
		/// </summary>
		/// <param name="fLoadFlexLayouts">if set to <c>true</c> [f load flex layouts].</param>
		/// <param name="sDatabase">The name of the database.</param>
		/// ------------------------------------------------------------------------------------
		public static void InitializePartInventories(bool fLoadFlexLayouts, string sDatabase)
		{
			InitializePartInventories(fLoadFlexLayouts, sDatabase, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the part inventories.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void InitializePartInventories(bool fLoadFlexLayouts, string sDatabase,
			bool fLoadUserOverrides)
		{
			string partDirectory = Path.Combine(SIL.FieldWorks.Common.Utils.DirectoryFinder.FWCodeDirectory,
				@"Language Explorer\Configuration\Parts");
			Dictionary<string, string[]> keyAttrs = new Dictionary<string, string[]>();
			keyAttrs["layout"] = new string[] {"class", "type", "name" };
			keyAttrs["group"] = new string[] {"label"};
			keyAttrs["part"] = new string[] {"ref"};


			Inventory layoutInventory = new Inventory(new string[] {partDirectory},
				"*Layouts.xml", "/LayoutInventory/*", keyAttrs);
			layoutInventory.Merger = new LayoutMerger();
			// Holding shift key means don't use extant preference file, no matter what.
			// This includes user overrides of layouts.
			if (fLoadUserOverrides &&
				System.Windows.Forms.Control.ModifierKeys != System.Windows.Forms.Keys.Shift)
			{
				layoutInventory.LoadUserOverrides(LayoutVersionNumber, sDatabase);
				if (fLoadFlexLayouts)
					layoutInventory.LoadFlexUserOverrides(LayoutVersionNumber, sDatabase);
			}
			else
			{
				layoutInventory.DeleteUserOverrides(sDatabase);
			}
			Inventory.SetInventory("layouts", sDatabase, layoutInventory);

			keyAttrs = new Dictionary<string, string[]>();
			keyAttrs["part"] = new string[] {"id"};

			Inventory.SetInventory("parts", sDatabase, new Inventory(new string[] {partDirectory},
				"*Parts.xml", "/PartInventory/bin/*", keyAttrs));
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
			CheckDisposed();

			Triple key = new Triple(clsid, layoutName, fIncludeLayouts);
			if (m_map.ContainsKey(key))
				return m_map[key];

			XmlNode node = null;
			uint classId = (uint) clsid;
			string classname;
			string useName = layoutName == null ? "default" : layoutName;
			string origName = useName;
			for( ; ; )
			{
				classname = m_mdc.GetClassName(classId);
				if (fIncludeLayouts)
				{
					// Inventory of layouts has keys class, type, name
					node = m_layoutInventory.GetElement("layout", new string[] {classname, "jtview", useName});
					if (node != null)
						break;
				}
				// inventory of parts has key id.
				node = m_partInventory.GetElement("part", new string[] {classname + "-Jt-" + useName});
				if (node != null)
					break;
				if (classId == 0 && useName != "default")
				{
					// Nothing found all the way to CmObject...try default layout.
					useName = "default";
					classId = (uint) clsid;
				}
				if (classId == 0)
				{
					if (fIncludeLayouts)
					{
						// Really surprising...default view not found on CmObject??
						throw new ApplicationException("No matching layout found for class " + classname + " jtview layout " + origName);
					}
					else
					{
						// okay to not find specific custom parts...we can generate them.
						return null;
					}
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
				CheckDisposed();
				return m_layoutInventory;
			}
		}

		class Triple
		{
			object m_first, m_second, m_third;
			public Triple(object first, object second, object third)
			{
				m_first = first;
				m_second = second;
				m_third = third;
			}

			bool TestEquals(object arg1, object arg2)
			{
				if (arg1 == null)
					return arg2 == null;
				return arg1.Equals(arg2);
			}

			public override bool Equals(object obj)
			{
				Triple objT = obj as Triple;
				return objT != null && TestEquals(objT.m_first, m_first) && TestEquals(objT.m_second, m_second) && TestEquals(objT.m_third, m_third);
			}

			int GetHash(object obj)
			{
				if (obj == null)
					return 0;
				return obj.GetHashCode();
			}

			public override int GetHashCode()
			{
				return GetHash(m_first) + GetHash(m_second) + GetHash(m_third);
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
		protected override void DisposeManagedResources()
		{
			// nothing to dispose
		}

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
		/// NOTE: THis assumes that the PartOwnershipTree doesn't have multiple properties pointing to the
		/// same target class. This is an assumption that's likely to change in the future.
		/// </summary>
		/// <param name="targetClsId"></param>
		/// <returns></returns>
		public string GetSourceFieldName(int targetClsId)
		{
			string flidName;
			string targetClassName = m_cache.GetClassName((uint)targetClsId);
			XmlNode classNode = m_classOwnershipTree.SelectSingleNode(".//" + targetClassName);
			flidName = XmlUtils.GetManditoryAttributeValue(classNode, "sourceField");
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
			int newListItemsClass = (int)Cache.GetDestinationClass((uint)flidForCurrentList);
			uint prevListItemsClass = Cache.GetDestinationClass((uint) flidForItemsBeforeListChange);
			RelationshipOfRelatives relationshipOfTarget = FindTreeRelationship((int)prevListItemsClass, newListItemsClass);
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
							int hvoAncestorOfItem = 0;
							if (gph != null && gph.GhostOwnerClass == (uint) newListItemsClass &&
								gph.IsGhostOwnerClass(hvoBeforeListChange))
							{
								// just add the ghost owner, as the ancestor relative,
								// since it's already in the newListItemsClass
								hvoAncestorOfItem = hvoBeforeListChange;
							}
							else
							{
								hvoAncestorOfItem = Cache.GetOwnerOfObjectOfClass(hvoBeforeListChange, newListItemsClass);
							}
							relatives.Add(hvoAncestorOfItem);
						}
						commonAncestors = relatives;
						break;
					}
				case RelationshipOfRelatives.Descendent:
				case RelationshipOfRelatives.Cousin:
					{
						foreach (int hvoBeforeListChange in itemsBeforeListChange)
						{
							int hvoCommonAncestor = 0;
							if (relationshipOfTarget == RelationshipOfRelatives.Descendent)
							{
								// the item is the ancestor
								hvoCommonAncestor = hvoBeforeListChange;
							}
							else
							{
								// the item and its cousins have a common ancestor.
								hvoCommonAncestor = GetHvoCommonAncestor(hvoBeforeListChange,
																		 (int) prevListItemsClass, newListItemsClass);
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
			GhostParentHelper gph = null;
			IVwVirtualHandler vh;
			if (Cache.TryGetVirtualHandler(flidToTry, out vh) && vh is FDOGhostSequencePropertyVirtualHandler)
				gph = GhostParentHelper.CreateGhostParentHelper(Cache, vh.Tag);
			return gph;
		}

		private Set<int> GetDescendents(int hvoCommonAncestor, int relativesFlid)
		{
			string listPropertyName = Cache.MetaDataCacheAccessor.GetFieldName((uint)relativesFlid);
			string parentObjName = Cache.MetaDataCacheAccessor.GetClassName((uint)m_cache.GetClassOfObject(hvoCommonAncestor));
			string xpathToPart = "./part[@id='" + parentObjName + "-Jt-" + listPropertyName + "']";
			XmlNode pathSpec = m_parentToChildrenSpecs.SelectSingleNode(xpathToPart);
			if (pathSpec == null)
				throw new ArgumentException("Expected to find part ({0}) in ParentClassPathsToChildren", xpathToPart);
			// get the part spec that gives us the path from obsolete current (parent) list item object
			// to the new one.
			using (XmlBrowseViewBaseVc vc = new XmlBrowseViewBaseVc(m_cache, null))
			{
				ManyOnePathSortItem parentItem = new ManyOnePathSortItem(hvoCommonAncestor, null, null);
				XmlBrowseViewBaseVc.ItemsCollectorEnv collector =
					new XmlBrowseViewBaseVc.ItemsCollectorEnv(null, m_cache, hvoCommonAncestor);
				vc.DisplayCell(parentItem, pathSpec, hvoCommonAncestor, collector);
				if (collector.HvosCollectedInCell != null && collector.HvosCollectedInCell.Count > 0)
				{
				   return collector.HvosCollectedInCell;
				}
			}
			return new Set<int>();
		}

		private RelationshipOfRelatives FindTreeRelationship(int prevListItemsClass, int newListItemsClass)
		{
			RelationshipOfRelatives relationshipOfTarget;
			if (Cache.IsSameOrSubclassOf(prevListItemsClass, newListItemsClass))
			{
				relationshipOfTarget = RelationshipOfRelatives.Sibling;
			}
			else
			{
				// lookup new class in ownership tree and decide how to select the most related object
				string newClassName = Cache.GetClassName((uint)newListItemsClass);
				string prevClassName = Cache.GetClassName((uint)prevListItemsClass);
				XmlNode prevClassNode = m_classOwnershipTree.SelectSingleNode(".//" + prevClassName);
				XmlNode newClassNode = m_classOwnershipTree.SelectSingleNode(".//" + newClassName);
				// determine if prevClassName is owned (has anscestor) by the new.
				bool fNewIsChildOfPrev = false;
				bool fNewIsAncestorOfPrev = prevClassNode.SelectSingleNode("ancestor::" + newClassName) != null;
				if (fNewIsAncestorOfPrev)
				{
					relationshipOfTarget = RelationshipOfRelatives.Ancestor;
				}
				else
				{
					// okay, now find most related object in new items list.
					fNewIsChildOfPrev = newClassNode.SelectSingleNode("ancestor::" + prevClassName) != null;
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

		private XmlNode GetTreeNode(uint classId)
		{
			string className = Cache.GetClassName(classId);
			return m_classOwnershipTree.SelectSingleNode(".//" + className);
		}

		private int GetHvoCommonAncestor(int hvoBeforeListChange, int prevListItemsClass, int newListItemsClass)
		{
			XmlNode prevClassNode = GetTreeNode((uint)prevListItemsClass);
			XmlNode newClassNode = GetTreeNode((uint)newListItemsClass);
			int hvoCommonAncestor = 0;
			// NOTE: the class of hvoBeforeListChange could be different then prevListItemsClass, if the item is a ghost (owner).
			int classOfHvoBeforeListChange = Cache.GetClassOfObject(hvoBeforeListChange);
			// so go up the parent of the previous one until it's an ancestor of the newClass.
			XmlNode ancestorOfPrev = prevClassNode.ParentNode;
			while (ancestorOfPrev != null)
			{
				if (newClassNode.SelectSingleNode("ancestor::" + ancestorOfPrev.Name) != null)
				{
					XmlNode commonAncestor = ancestorOfPrev;
					int classCommonAncestor = (int)Cache.MetaDataCacheAccessor.GetClassId(commonAncestor.Name);
					if (Cache.IsSameOrSubclassOf(classOfHvoBeforeListChange, classCommonAncestor))
						hvoCommonAncestor = hvoBeforeListChange;
					else
						hvoCommonAncestor = Cache.GetOwnerOfObjectOfClass(hvoBeforeListChange, classCommonAncestor);
					break;
				}
				ancestorOfPrev = ancestorOfPrev.ParentNode;
			}
			return hvoCommonAncestor;
		}
	}
}
