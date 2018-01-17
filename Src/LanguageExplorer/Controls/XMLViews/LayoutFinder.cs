// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.FieldWorks.Filters;
using SIL.Xml;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// LayoutFinder is an implementation of IStringFinder that finds a string based
	/// on looking up a layout for a particular HVO.
	/// </summary>
	internal class LayoutFinder : IStringFinder, IPersistAsXml, IStoresLcmCache, IStoresDataAccess
	{
		#region Data members
		internal ISilDataAccess m_sda;
		internal string m_layoutName;
		internal IFwMetaDataCache m_mdc;
		internal LcmCache m_cache;
		internal LayoutCache m_layouts;
		internal XElement m_colSpec;
		/// <summary/>
		protected XmlBrowseViewBaseVc m_vc;
		/// <summary/>
		protected bool m_fDisposeVc;
		private IApp m_app;
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// normal constructor.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="layoutName">Name of the layout.</param>
		/// <param name="colSpec">The col spec.</param>
		/// <param name="app">The application.</param>
		/// ------------------------------------------------------------------------------------
		internal LayoutFinder(LcmCache cache, string layoutName, XElement colSpec,
			IApp app): this()
		{
			m_layoutName = layoutName;
			m_colSpec = colSpec;
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
		/// <param name="cache">LcmCache</param>
		/// <param name="colSpec">column specification</param>
		/// <param name="vc">The vc.</param>
		/// <param name="app">The application.</param>
		/// <returns>finder for colSpec</returns>
		/// ------------------------------------------------------------------------------------
		internal static IStringFinder CreateFinder(LcmCache cache, XElement colSpec, XmlBrowseViewBaseVc vc, IApp app)
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
						result = new LayoutFinder(cache, layoutName, colSpec, app);
						break;
					default:
						throw new FwConfigurationException("unexpected sort type: " + sortType, colSpec);
				}
			}
			else
			{
				result = new LayoutFinder(cache, layoutName, colSpec, app);
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
		//		XElement ExtractFromFlow(XElement viewSpec)
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
		/// Strings the specified hvo.
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
					var layout = XmlVc.GetNodeForPart(hvo, m_layoutName, true, m_sda, m_layouts);
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
			// it may be better to refactor ViewBase to cause it to reload the sorter and filter from persistence
			// every time the tool is changed
			if (m_vc == null)
			{
				m_vc = new XmlBrowseViewBaseVc(m_cache);
				m_vc.SuppressPictures = true; // we won't dispose of it, so it mustn't make pictures (which we don't need)
				m_vc.DataAccess = m_sda;
			}
			else
			{
				if (m_vc.Cache == null)
					m_vc.Cache = m_cache;
				if (m_vc.Cache == null)
					throw new ApplicationException("There's no way the browse VC (m_vc) can get a string in its current state.");
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

		private string[] StringsFor(int hvo, XElement layout, int wsForce)
		{
			return XmlViewsUtils.StringsFor(m_cache, m_cache.DomainDataByFlid, layout, hvo, m_layouts, null, wsForce);
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
			string colSpecLabel = XmlUtils.GetMandatoryAttributeValue(m_colSpec, "label");
			string otherLfLabel = XmlUtils.GetMandatoryAttributeValue(otherLf.m_colSpec, "label");
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
		public virtual void PersistAsXml(XElement node)
		{
			XmlUtils.SetAttribute(node, "layout", m_layoutName ?? string.Empty);
			node.Add(m_colSpec);
			}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inits the XML.
		/// </summary>
		/// <param name="node">The node.</param>
		/// ------------------------------------------------------------------------------------
		public virtual void InitXml(XElement node)
		{
			m_layoutName = XmlUtils.GetMandatoryAttributeValue(node, "layout");
			m_colSpec = node.Element("column");
		}

		#endregion

		#region IStoresLcmCache Members

		/// <summary>
		/// This is used to set the cache when one is recreated from XML.
		/// </summary>
		public LcmCache Cache
		{
			set
			{
				if (m_cache == value)
					return;

				m_sda = value.DomainDataByFlid;
				m_cache = value;
				m_mdc = value.DomainDataByFlid.MetaDataCache;
				m_layouts = new LayoutCache(m_mdc, m_cache.ProjectId.Name, m_app?.ApplicationName ?? FwUtils.ksFlexAppName, m_cache.ProjectId.ProjectFolder);
				// The VC is set after the cache when created by the view, but it uses a
				// 'real' VC that already has a cache.
				// When the VC is created by restoring a persisted layout finder, the cache
				// is set later, and needs to be copied to the VC.
				if (m_vc != null)
					m_vc.Cache = value;
			}
		}
		#endregion
	}

	/// <summary>
	/// IntCompareFinder is an implementation of StringFinder that modifies the sort
	/// string by adding leading zeros to pad it to ten digits.
	/// </summary>
	internal class IntCompareFinder : LayoutFinder
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
		public IntCompareFinder(LcmCache cache, string layoutName, XElement colSpec, IApp app)
			: base(cache, layoutName, colSpec, app)
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
