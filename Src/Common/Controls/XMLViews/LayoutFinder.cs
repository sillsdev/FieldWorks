// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.Filters;
using SIL.Utils;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary>
	/// LayoutFinder is an implementation of IStringFinder that finds a string based
	/// on looking up a layout for a particular HVO.
	/// </summary>
	public class LayoutFinder : IStringFinder, IPersistAsXml,
		IStoresFdoCache, IStoresDataAccess, IAcceptsStringTable
	{
		#region Data members
		internal ISilDataAccess m_sda;
		internal string m_layoutName;
		internal IFwMetaDataCache m_mdc;
		internal FdoCache m_cache;
		internal LayoutCache m_layouts;
		internal XmlNode m_colSpec;
		/// <summary/>
		protected XmlBrowseViewBaseVc m_vc;
		/// <summary/>
		protected bool m_fDisposeVc;
		private StringTable m_stringTbl;
		private IApp m_app;
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// normal constructor.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="layoutName">Name of the layout.</param>
		/// <param name="colSpec">The col spec.</param>
		/// <param name="stringTbl">The string TBL.</param>
		/// <param name="app">The application.</param>
		/// ------------------------------------------------------------------------------------
		public LayoutFinder(FdoCache cache, string layoutName, XmlNode colSpec,
			StringTable stringTbl, IApp app): this()
		{
			m_layoutName = layoutName;
			m_colSpec = colSpec;
			m_stringTbl = stringTbl;
			m_app = app;
			Cache = cache;
		}

		/// <summary>
		/// Default constructor for persistence.
		/// </summary>
		public LayoutFinder()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make a finder appropriate to the given column specification
		/// </summary>
		/// <param name="cache">FdoCache</param>
		/// <param name="colSpec">column specification</param>
		/// <param name="vc">The vc.</param>
		/// <param name="app">The application.</param>
		/// <returns>finder for colSpec</returns>
		/// ------------------------------------------------------------------------------------
		static public IStringFinder CreateFinder(FdoCache cache, XmlNode colSpec,
			XmlBrowseViewBaseVc vc, IApp app)
		{
			string layoutName = XmlUtils.GetOptionalAttributeValue(colSpec, "layout");
			string sSortMethod = XmlUtils.GetOptionalAttributeValue(colSpec, "sortmethod");
			string sortType = XmlUtils.GetOptionalAttributeValue(colSpec, "sortType", null);
			LayoutFinder result;
			if (sSortMethod != null)
			{
				result = new SortMethodFinder(cache, sSortMethod, layoutName, colSpec, app);
			}
			else if (sortType != null)
			{
				switch (sortType)
				{
					case "integer":
						result = new IntCompareFinder(cache, layoutName, colSpec, app);
						break;
					case "date":
					case "YesNo":
					case "stringList":
					case "genDate":
						// no special action needed here for sorting dates or date that shows as 'yes" or "no";
						// Using a SortCollectorEnv triggers special
						// action in case "datetime"/"gendate" of XmlVc.ProcessFrag().
						result = new LayoutFinder(cache, layoutName, colSpec, vc.StringTbl, app);
						break;
					default:
						throw new ConfigurationException("unexpected sort type: " + sortType, colSpec);
				}
			}
			else
			{
				result = new LayoutFinder(cache, layoutName, colSpec, vc.StringTbl, app);
			}
			result.Vc = vc;
			return result;
		}

		/// <summary>
		/// Set the SDA when we need to override the one from the cache (using a decorator).
		/// </summary>
		public ISilDataAccess DataAccess
		{
			set
			{
				m_sda = value;
				m_mdc = m_sda.MetaDataCache;
			}
		}

		internal XmlBrowseViewBaseVc Vc
		{
			get
			{
				return m_vc;
			}
			set
			{
				m_vc = value;
				if (m_vc != null && m_stringTbl == null)
					m_stringTbl = m_vc.StringTbl;
				m_sda = m_vc.DataAccess;
				m_mdc = m_sda.MetaDataCache;
			}
		}

//		/// ------------------------------------------------------------------------------------
//		/// <summary>
//		/// Given a spec that might be some sort of element, or might be something wrapping a flow object
//		/// around that element, return the element. Or, it might be a "frag" element wrapping all of that.
//		/// </summary>
//		/// <param name="viewSpec">The view spec.</param>
//		/// <returns></returns>
//		/// ------------------------------------------------------------------------------------
//		XmlNode ExtractFromFlow(XmlNode viewSpec)
//		{
//			if (viewSpec == null)
//				return null;
//			if (viewSpec.Name == "frag")
//				viewSpec = viewSpec.FirstChild;
//			if (viewSpec.Name == "para" || viewSpec.Name == "div")
//		{
//				if (viewSpec.ChildNodes.Count == 2 && viewSpec.FirstChild.Name == "properties")
//					return viewSpec.ChildNodes[1];
//				else if (viewSpec.ChildNodes.Count == 1)
//					return viewSpec.FirstChild;
//		}
//			return viewSpec; // None of the special flow object cases, use the node itself.
//		}

		#region StringFinder Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Stringses the specified hvo.
		/// </summary>
		/// <param name="hvo">The hvo.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public string[] Strings(int hvo)
		{
			try
			{
				string[] result = null;
				if (m_layoutName == null)
					result = StringsFor(hvo, m_colSpec, m_vc.WsForce);
				if (result == null)
				{
					XmlNode layout = XmlVc.GetNodeForPart(hvo, m_layoutName, true, m_sda, m_layouts);
					if (layout == null)
						return new string[0]; // cell will be empty.
					result = StringsFor(hvo, layout, m_vc.WsForce);
				}

				if (result == null)
					return new string[0];
				else
					return result;
			}
			catch (Exception e)
			{
				throw new Exception("Failed to get strings for object " + hvo, e);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Figure the node and hvo that correspond to the particular sort item,
		/// and generate its key. This figures out the XML needed for the particular
		/// key object, and interprets it to make a key.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <param name="sortedFromEnd"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public string[] Strings(IManyOnePathSortItem item, bool sortedFromEnd)
		{
			string result = Key(item, true).Text;
			if (result == null)
				return new string[0];
			else
			{
				if (sortedFromEnd)
					result = TsStringUtils.ReverseString(result);

				return new string[] { result };
			}
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Keys the specified item.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public ITsString Key(IManyOnePathSortItem item)
		{
			return Key(item, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Keys the specified item.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <param name="fForSorting">if set to <c>true</c> [f for sorting].</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public ITsString Key(IManyOnePathSortItem item, bool fForSorting)
		{
			if (m_cache == null)
				throw new ApplicationException("There's no way the browse VC (m_vc) can get a string in its current state.");
			int hvo = item.RootObjectHvo;
			TsStringCollectorEnv collector;
			if (fForSorting)
			{
				collector = new SortCollectorEnv(null, m_sda, hvo);
			}
			else
			{
				collector = new TsStringCollectorEnv(null, m_sda, hvo);
			}

			// This will check to see if the VC is either null or disposed.  The disposed check is neccesary because
			// there are several instances where we can have a reference to an instance that was disposed, which will
			// cause problems later on.
			// Enhance CurtisH/EricP: If this VC gets used in other places, rather than adding more checks like this one,
			// it may be better to refactor XWorksViewBase to cause it to reload the sorter and filter from persistence
			// every time the tool is changed
			if (m_vc == null)
			{
				m_vc = new XmlBrowseViewBaseVc(m_cache, m_stringTbl);
				m_vc.SuppressPictures = true; // we won't dispose of it, so it mustn't make pictures (which we don't need)
				m_vc.DataAccess = m_sda;
			}
			else
			{
				if (m_vc.Cache == null)
					m_vc.Cache = m_cache;
				if (m_vc.Cache == null)
					throw new ApplicationException("There's no way the browse VC (m_vc) can get a string in its current state.");
				if (m_vc.StringTbl == null)
					m_vc.StringTbl = m_stringTbl;
			}
			m_vc.DisplayCell(item, m_colSpec, hvo, collector);
			return collector.Result;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// For most of these we want to return the same thing.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <param name="sortedFromEnd">if set to <c>true</c> [sorted from end].</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public virtual string[] SortStrings(IManyOnePathSortItem item, bool sortedFromEnd)
		{
			return Strings(item, sortedFromEnd);
		}
		/// <summary>
		/// Add to collector the ManyOnePathSortItems which this sorter derives from
		/// the specified object. This implementation follows object and sequence properties,
		/// if there is only one in a given structure, and makes an item for each thing
		/// found.
		/// </summary>
		public void CollectItems(int hvo, ArrayList collector)
		{
			int start = collector.Count;
			XmlViewsUtils.CollectBrowseItems(hvo, m_colSpec, collector, m_mdc, m_sda, m_layouts);
		}

		private string[] StringsFor(int hvo, XmlNode layout, int wsForce)
		{
			return XmlViewsUtils.StringsFor(m_cache, m_cache.DomainDataByFlid, layout, hvo, m_layouts, null, m_stringTbl, wsForce);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Answer true if they are the 'same' finder (will find the same strings).
		/// </summary>
		/// <param name="other">The other.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public virtual bool SameFinder(IStringFinder other)
		{
			var otherLf = other as LayoutFinder;
			if (otherLf == null)
				return false;
			return SameLayoutName(otherLf)
				&& SameData(otherLf)
				&& SameConfiguration(otherLf);
		}

		bool SameLayoutName(LayoutFinder otherLf)
		{
			return otherLf.m_layoutName == m_layoutName ||
				(String.IsNullOrEmpty(otherLf.m_layoutName) && String.IsNullOrEmpty(m_layoutName));
		}

		private bool SameData(LayoutFinder otherLf)
		{
			if (otherLf.m_sda == m_sda)
				return true;
			ISilDataAccessManaged first = RootSdaOf(m_sda);
			ISilDataAccessManaged second = RootSdaOf(otherLf.m_sda);
			return (first == second && first != null);
		}

		private static ISilDataAccessManaged RootSdaOf(ISilDataAccess sda)
		{
			if (sda is DomainDataByFlidDecoratorBase)
				return RootSdaOf((sda as DomainDataByFlidDecoratorBase).BaseSda);
			else
				return sda as ISilDataAccessManaged;
		}

		private bool SameConfiguration(LayoutFinder otherLf)
		{
			// XmlUtils.NodesMatch() is too strict a comparison for identifying column configurations that will
			// display the same value (e.g. we don't care about differences in width), causing us to
			// lose the sort arrow when switching between tools sharing common columns (LT-2858).
			// For now, just assume that columns with the same label will display the same value.
			// If this proves too loose for a particular column, try implementing a sortmethod instead.
			string colSpecLabel = XmlUtils.GetManditoryAttributeValue(m_colSpec, "label");
			string otherLfLabel = XmlUtils.GetManditoryAttributeValue(otherLf.m_colSpec, "label");
			string colSpecLabel2 = XmlUtils.GetOptionalAttributeValue(m_colSpec, "headerlabel");
			string otherLfLabel2 = XmlUtils.GetOptionalAttributeValue(otherLf.m_colSpec, "headerlabel");
			return (colSpecLabel == otherLfLabel ||
					colSpecLabel == otherLfLabel2 ||
					colSpecLabel2 == otherLfLabel ||
					(colSpecLabel2 == otherLfLabel2 && otherLfLabel2 != null));
		}

		/// <summary>
		/// Called in advance of 'finding' strings for many instances, typically all or most
		/// of the ones in existence. May preload data to make such a large succession of finds
		/// more efficient. This one looks to see whether its ColSpec specifies a preload,
		/// and if so, invokes it.
		/// </summary>
		public void Preload(object rootObj)
		{
			if (m_vc != null)
				m_vc.SetReversalWritingSystemFromRootObject(rootObj);
			string preload = XmlUtils.GetOptionalAttributeValue(m_colSpec, "preload", null);
			if (String.IsNullOrEmpty(preload))
				return;
			string[] splits = preload.Split('.');
			if (splits.Length != 2)
				return; // ignore faulty ones
			string className = splits[0];
			string methodName = splits[1];
			// Get the directory where our DLLs live
			string baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			// For now we assume an FDO class.
			Assembly fdoAssembly = Assembly.LoadFrom(Path.Combine(baseDir, "FDO.dll"));
			Type targetType = fdoAssembly.GetType("SIL.FieldWorks.FDO." + className);
			if (targetType == null)
				return;
			MethodInfo info = targetType.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public);
			if (info == null)
				return;
			info.Invoke(null, new object[] { m_cache });
		}

		#endregion

		#region IPersistAsXml Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Persists as XML.
		/// </summary>
		/// <param name="node">The node.</param>
		/// ------------------------------------------------------------------------------------
		public virtual void PersistAsXml(XmlNode node)
		{
			XmlUtils.AppendAttribute(node, "layout", m_layoutName);
			node.AppendChild(node.OwnerDocument.ImportNode(m_colSpec, true));
			}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inits the XML.
		/// </summary>
		/// <param name="node">The node.</param>
		/// ------------------------------------------------------------------------------------
		public virtual void InitXml(XmlNode node)
		{
			m_layoutName = XmlUtils.GetManditoryAttributeValue(node, "layout");
			m_colSpec = node.SelectSingleNode("column");
		}

		#endregion

		#region IStoresFdoCache Members

		/// <summary>
		/// This is used to set the cache when one is recreated from XML.
		/// </summary>
		public FdoCache Cache
		{
			set
			{
				if (m_cache == value)
					return;

				m_sda = value.DomainDataByFlid;
				m_cache = value;
				m_mdc = value.DomainDataByFlid.MetaDataCache;
				m_layouts = new LayoutCache(m_mdc, m_cache.ProjectId.Name, m_app,
					m_cache.ProjectId.ProjectFolder);
				// The VC is set after the cache when created by the view, but it uses a
				// 'real' VC that already has a cache.
				// When the VC is created by restoring a persisted layout finder, the cache
				// is set later, and needs to be copied to the VC.
				if (m_vc != null)
					m_vc.Cache = value;
			}
		}
		#endregion

		#region IAcceptsStringTable Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the string table.
		/// </summary>
		/// <value>The string table.</value>
		/// ------------------------------------------------------------------------------------
		public StringTable StringTable
		{
			set { m_stringTbl = value; }
		}

		#endregion
	}

	/// <summary>
	/// SortMethodFinder is an implementation of StringFinder that finds a sort string based
	/// on a given sort method defined on the class of the objects being sorted.  If the
	/// method does not exist, the finder tries the standard method 'SortKey'.  If that
	/// does not exist, fall back to the string derived from the layout.
	/// The string derived from the layout is still used for filtering.
	/// </summary>
	public class SortMethodFinder : LayoutFinder
	{
		private string m_sMethodName;
		private string m_wsName;
		private int m_ws;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// normal constructor.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="methodName">Name of the method.</param>
		/// <param name="layoutName">Name of the layout.</param>
		/// <param name="colSpec">The col spec.</param>
		/// <param name="app">The application</param>
		/// ------------------------------------------------------------------------------------
		public SortMethodFinder(FdoCache cache, string methodName, string layoutName,
			XmlNode colSpec, IApp app)
			: base(cache, layoutName, colSpec, null, app)
		{
			SortMethod = methodName;
			WritingSystemName = StringServices.GetWsSpecWithoutPrefix(colSpec);
		}

		/// <summary>
		/// Default constructor for persistence.
		/// </summary>
		public SortMethodFinder()
		{
		}

		string SortMethod
		{
			set
			{
				m_sMethodName = value ?? "";
			}
		}

		string WritingSystemName
		{
			get { return m_wsName; }
			set
			{
				m_wsName = value == "" ? null : value;
			}
		}

		/// <summary>
		/// Gets the sort key by traversing the part tree, calling the sort method at the leaves.
		/// </summary>
		/// <param name="layout">The layout.</param>
		/// <param name="cmo">The object.</param>
		/// <param name="item">The item.</param>
		/// <param name="pathIndex">Index of the path.</param>
		/// <param name="sortedFromEnd">if set to <c>true</c> [sorted from end].</param>
		/// <returns></returns>
		private string[] GetKey(XmlNode layout, ICmObject cmo, IManyOnePathSortItem item, int pathIndex, bool sortedFromEnd)
		{
			if (layout == null)
				return null;

			switch (layout.Name)
			{
				case "obj":
					{
						int flid = GetFlid(layout, cmo.Hvo);
						if (pathIndex != -1 && (pathIndex == item.PathLength || flid != item.PathFlid(pathIndex)))
							// we are now off of the path
							pathIndex = -1;

						int objHvo = m_cache.MainCacheAccessor.get_ObjectProp(cmo.Hvo, flid);
						if (objHvo != 0)
						{
							if (pathIndex != -1
								&& (pathIndex < item.PathLength - 1 && objHvo == item.PathObject(pathIndex + 1))
								 || (pathIndex == item.PathLength - 1 && objHvo == item.KeyObject))
							{
								return GetChildObjKey(layout, objHvo, item, pathIndex + 1, sortedFromEnd);
							}

							// we are off of the path
							return GetChildObjKey(layout, objHvo, item, -1, sortedFromEnd);
						}
					}
					break;

				case "seq":
					{
						int flid = GetFlid(layout, cmo.Hvo);
						if (pathIndex != -1 && (pathIndex == item.PathLength || flid != item.PathFlid(pathIndex)))
							// we are now off of the path
							pathIndex = -1;

						int size = m_cache.MainCacheAccessor.get_VecSize(cmo.Hvo, flid);
						StringBuilder sb = null;
						for (int i = 0; i < size; i++)
						{
							int objHvo = m_cache.MainCacheAccessor.get_VecItem(cmo.Hvo, flid, i);
							if (pathIndex != -1
								&& (pathIndex < item.PathLength - 1 && objHvo == item.PathObject(pathIndex + 1))
								|| (pathIndex == item.PathLength - 1 && objHvo == item.KeyObject))
							{
								return GetChildObjKey(layout, objHvo, item, pathIndex + 1, sortedFromEnd);
							}

							// if we are off of the path, we concatenate all vector keys to create an
							// aggregate key
							var childObjKey = GetChildObjKey(layout, objHvo, item, -1, sortedFromEnd);
							if (childObjKey != null)
							{
								if (sb == null)
									sb = new StringBuilder();
								foreach (var subKey in childObjKey)
									sb.Append(subKey);
							}
						}
						if (sb != null)
							return new [] {sb.ToString()};
					}
					break;

				case "layout":
				case "part":
					{
						string partref = XmlUtils.GetOptionalAttributeValue(layout, "ref");
						if (partref != null)
						{
							XmlNode part = XmlVc.GetNodeForPart(cmo.Hvo, partref, true, m_sda, m_layouts);
							return GetKey(part, cmo, item, pathIndex, sortedFromEnd);
						}

						foreach (XmlNode child in layout.ChildNodes)
						{
							if (child is XmlComment)
								continue;

							var key = GetKey(child, cmo, item, pathIndex, sortedFromEnd);
							if (key != null)
								return key;
						}
					}
					break;
			}
			return null;
		}

		private string[] GetChildObjKey(XmlNode layout, int hvo, IManyOnePathSortItem item, int pathIndex, bool sortedFromEnd)
		{
			ICmObject childObj = m_cache.ServiceLocator.ObjectRepository.GetObject(hvo);
			string layoutName = XmlUtils.GetOptionalAttributeValue(layout, "layout");
			XmlNode part = XmlVc.GetNodeForPart(hvo, layoutName, true, m_sda, m_layouts);
			var key = GetKey(part, childObj, item, pathIndex, sortedFromEnd);
			if (key != null)
				return key;
			return CallSortMethod(childObj, sortedFromEnd);
		}

		/// <summary>
		/// This is a simplified version of XmlVc.GetFlid.
		/// It does not look for a flid attr, nor try to cache the result.
		/// It looks for a "field" property, and optionally a "class" one, and uses them
		/// (or the class of hvo, if "class" is missing) to figure the flid.
		/// Virtual properties are assumed already created.
		/// </summary>
		/// <param name="frag">The frag.</param>
		/// <param name="hvo">The hvo.</param>
		/// <returns></returns>
		private int GetFlid(XmlNode frag, int hvo)
		{
			string stClassName = XmlUtils.GetOptionalAttributeValue(frag, "class");
			string stFieldName = XmlUtils.GetManditoryAttributeValue(frag, "field");
			if (string.IsNullOrEmpty(stClassName))
			{
				int classId = m_sda.get_IntProp(hvo,
					(int)CmObjectFields.kflidCmObject_Class);
				return m_mdc.GetFieldId2(classId, stFieldName, true);
			}

			return m_mdc.GetFieldId(stClassName, stFieldName, true);
		}

		#region StringFinder Members

		/// <summary>
		/// Get a key from the item.
		/// </summary>
		/// <param name="item">The item.</param>
		/// <param name="sortedFromEnd">if set to <c>true</c> [sorted from end].</param>
		/// <returns></returns>
		public override string[] SortStrings(IManyOnePathSortItem item, bool sortedFromEnd)
		{
			if (item.KeyObject == 0)
				return new string[0];

			// traverse the part tree from the root, the root object and the root layout node should
			// be compatible
			XmlNode layout = XmlVc.GetNodeForPart(item.RootObjectHvo, m_layoutName, true, m_sda, m_layouts);
			var rootObject = item.RootObjectUsing(m_cache);
			var key = GetKey(layout, rootObject, item, 0, sortedFromEnd);
			if (key == null)
			{
				// the root object sort method is not tried in GetKey
				key = CallSortMethod(rootObject, sortedFromEnd);

				if (key == null)
				{
					// try calling the sort method on the key object
					var keyCmObjectUsing = item.KeyObjectUsing(m_cache);
					key = CallSortMethod(keyCmObjectUsing, sortedFromEnd);

					if (key == null)
					{
						// Try the default fallback if we can't find the method.
						var firstKey = keyCmObjectUsing.SortKey ?? "";
						if (sortedFromEnd)
							firstKey = TsStringUtils.ReverseString(firstKey);

						return new [] {firstKey, keyCmObjectUsing.SortKey2Alpha};
					}
				}
			}

			return key;
		}

		/// <summary>
		/// Calls the sort method.
		/// </summary>
		/// <param name="cmo">The object.</param>
		/// <param name="sortedFromEnd">if set to <c>true</c> [sorted from end].</param>
		/// <returns></returns>
		private string[] CallSortMethod(ICmObject cmo, bool sortedFromEnd)
		{
			Type typeCmo = cmo.GetType();
			try
			{
				MethodInfo mi = typeCmo.GetMethod(m_sMethodName);
				if (mi == null)
					return null;

				object obj;
				if (mi.GetParameters().Length == 2)
				{
					// Enhance JohnT: possibly we should seek to evaluate this every time, in case it is a magic WS like
					// "best vernacular". But interpreting those requires a flid, and we don't have one; indeed, the
					// method may retrieve information from several. So we may as well just accept that the fancy ones
					// won't work.
					if (m_ws == 0 && WritingSystemName != null)
						m_ws = WritingSystemServices.InterpretWsLabel(m_cache, WritingSystemName, null, 0, 0, null);
					obj = mi.Invoke(cmo, new object[] { sortedFromEnd, m_ws });
				}
				else
				{
					obj = mi.Invoke(cmo, new object[] { sortedFromEnd });
				}
				if (obj is string)
					return new [] {(string) obj};
				// otherwise assume it already is a string array.
				return (string[]) obj;
			}
			catch (Exception)
			{
				return null;
			}
		}

		/// <summary>
		/// Answer true if they are the 'same' finder (will find the same strings).
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public override bool SameFinder(IStringFinder other)
		{
			if (other is SortMethodFinder)
			{
				var smf = other as SortMethodFinder;
				return m_sMethodName == smf.m_sMethodName && base.SameFinder(other)
					&& m_wsName == smf.m_wsName;
			}

			return false;
		}

		#endregion

		#region IPersistAsXml Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Persists as XML.
		/// </summary>
		/// <param name="node">The node.</param>
		/// ------------------------------------------------------------------------------------
		public override void PersistAsXml(XmlNode node)
		{
			base.PersistAsXml(node);
			XmlUtils.AppendAttribute(node, "sortmethod", m_sMethodName);
			if (!string.IsNullOrEmpty(m_wsName))
				XmlUtils.AppendAttribute(node, "ws", m_wsName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inits the XML.
		/// </summary>
		/// <param name="node">The node.</param>
		/// ------------------------------------------------------------------------------------
		public override void InitXml(XmlNode node)
		{
			base.InitXml(node);
			SortMethod = XmlUtils.GetManditoryAttributeValue(node, "sortmethod");
			WritingSystemName = XmlUtils.GetOptionalAttributeValue(node, "ws", null);
			// Enhance JohnT: if we start using string tables for browse views,
			// we will need a better way to provide one to the Vc we make here.
			// Note: we don't need a top-level spec because we're only going to process one
			// column's worth.
			m_vc = new XmlBrowseViewBaseVc();
			m_vc.SuppressPictures = true; // we won't dispose of it, so it mustn't make pictures (which we don't need)
		}
		#endregion
	}

	/// <summary>
	/// IntCompareFinder is an implementation of StringFinder that modifies the sort
	/// string by adding leading zeros to pad it to ten digits.
	/// </summary>
	public class IntCompareFinder : LayoutFinder
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// normal constructor.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="layoutName">Name of the layout.</param>
		/// <param name="colSpec">The col spec.</param>
		/// <param name="app">The application</param>
		/// ------------------------------------------------------------------------------------
		public IntCompareFinder(FdoCache cache, string layoutName, XmlNode colSpec, IApp app)
			: base(cache, layoutName, colSpec, null, app)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Default constructor for persistence.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IntCompareFinder()
		{
		}

		#region StringFinder Members

		const int maxDigits = 10; // Int32.MaxValue.ToString().Length;, but that is not 'const'!

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a key from the item for sorting. Add enough leading zeros so string comparison
		/// works.
		///
		/// Collator sorting generally ignores the minus sign as being a hyphen.  So we have
		/// to be tricky handling negative numbers.  Nine's complement with an inverted sign
		/// digit should do the trick...
		/// </summary>
		/// <param name="item">The item.</param>
		/// <param name="sortedFromEnd">if set to <c>true</c> [sorted from end].</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override string[] SortStrings(IManyOnePathSortItem item, bool sortedFromEnd)
		{
			string[] baseResult = base.SortStrings(item, sortedFromEnd);
			if (sortedFromEnd)
				return baseResult; // what on earth would it mean??
			if (baseResult.Length != 1)
				return baseResult;
			string sVal = baseResult[0];
			if (sVal.Length == 0)
				return new string[] { "9" + new String('0', maxDigits) };
			string prefix;
			char chFiller;
			if (sVal[0] == '-')
			{
				sVal = NinesComplement(sVal.Substring(1));
				prefix = "0";	// negative numbers come first.
				chFiller = '9';
			}
			else
			{
				prefix = "9";	// positive numbers come later.
				chFiller = '0';
			}
			if (sVal.Length == maxDigits)
				return new string[] { prefix + sVal };
			else
				return new string[] { prefix + new String(chFiller, maxDigits - sVal.Length) + sVal };
		}

		private string NinesComplement(string sNumber)
		{
			StringBuilder bldr = new StringBuilder();
			while (sNumber.Length > 0)
			{
				switch (sNumber[0])
				{
					case '0': bldr.Append('9'); break;
					case '1': bldr.Append('8'); break;
					case '2': bldr.Append('7'); break;
					case '3': bldr.Append('6'); break;
					case '4': bldr.Append('5'); break;
					case '5': bldr.Append('4'); break;
					case '6': bldr.Append('3'); break;
					case '7': bldr.Append('2'); break;
					case '8': bldr.Append('1'); break;
					case '9': bldr.Append('0'); break;
					default:
						throw new Exception("Invalid character found in supposed integer string!");
				}
				sNumber = sNumber.Substring(1);
			}
			return bldr.ToString();
		}

		/// <summary>
		/// Answer true if they are the 'same' finder (will find the same strings).
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public override bool SameFinder(IStringFinder other)
		{
			if (other is IntCompareFinder)
			{
				return base.SameFinder(other);
			}
			else
			{
				return false;
			}
		}

		#endregion
	}

	/// <summary>
	/// This is a marker class used when building a sort key.
	/// </summary>
	class SortCollectorEnv : TsStringCollectorEnv
	{
		public SortCollectorEnv(IVwEnv baseEnv, ISilDataAccess sda, int hvoRoot)
			: base(baseEnv, sda, hvoRoot)
		{ }
	}
}
