// Copyright (c) 2005-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml.Linq;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.XMLViews;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.Xml;

namespace LanguageExplorer.Filters
{
	/// <summary>
	/// LayoutFinder is an implementation of IStringFinder that finds a string based
	/// on looking up a layout for a particular HVO.
	/// </summary>
	internal class LayoutFinder : IStringFinder
	{
		#region Data members
		protected ISilDataAccess m_sda;
		protected string m_layoutName;
		protected IFwMetaDataCache m_mdc;
		protected LcmCache m_cache;
		protected LayoutCache m_layouts;
		private readonly XElement _columnSpecificationElement;
		protected XmlBrowseViewVc m_vc;
		private readonly IApp m_app;
		#endregion

		/// <summary />
		private LayoutFinder(XElement columnSpecificationElement, string layoutName)
		{
			m_layoutName = layoutName;
			_columnSpecificationElement = columnSpecificationElement;
		}

		/// <summary />
		internal LayoutFinder(LcmCache cache, string layoutName, XElement columnSpecificationElement, IApp app)
			: this(columnSpecificationElement, layoutName)
		{
			m_app = app;
			Cache = cache;
		}

		/// <summary>
		/// For use with IPersistAsXml
		/// </summary>
		internal LayoutFinder(XElement element)
			: this(element.Element("column"), XmlUtils.GetMandatoryAttributeValue(element, "layout"))
		{
		}

		/// <summary>
		/// Make a finder appropriate to the given column specification
		/// </summary>
		internal static IStringFinder CreateFinder(LcmCache cache, XElement columnSpecificationElement, XmlBrowseViewVc vc, IApp app)
		{
			var layoutName = XmlUtils.GetOptionalAttributeValue(columnSpecificationElement, "layout");
			var sSortMethod = XmlUtils.GetOptionalAttributeValue(columnSpecificationElement, "sortmethod");
			LayoutFinder result;
			if (sSortMethod != null)
			{
				result = new SortMethodFinder(cache, sSortMethod, layoutName, columnSpecificationElement, app);
			}
			else
			{
				var sortType = XmlUtils.GetOptionalAttributeValue(columnSpecificationElement, "sortType", null);
				if (sortType == null)
				{
					result = new LayoutFinder(cache, layoutName, columnSpecificationElement, app);
				}
				else
				{
					switch (sortType)
					{
						case "integer":
							result = new IntCompareFinder(cache, layoutName, columnSpecificationElement, app);
							break;
						case "date":
						case "YesNo":
						case "stringList":
						case "genDate":
							// no special action needed here for sorting dates or date that shows as 'yes" or "no";
							// Using a SortCollectorEnv triggers special
							// action in case "datetime"/"gendate" of XmlVc.ProcessFrag().
							result = new LayoutFinder(cache, layoutName, columnSpecificationElement, app);
							break;
						default:
							throw new FwConfigurationException($"unexpected sort type: {sortType}", columnSpecificationElement);
					}
				}
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

		internal XmlBrowseViewVc Vc
		{
			get => m_vc;
			set
			{
				m_vc = value;
				m_sda = m_vc.DataAccess;
				m_mdc = m_sda.MetaDataCache;
			}
		}

		#region StringFinder Members

		/// <summary>
		/// Strings the specified hvo.
		/// </summary>
		public string[] Strings(int hvo)
		{
			try
			{
				string[] result = null;
				if (m_layoutName == null)
				{
					result = StringsFor(hvo, _columnSpecificationElement, m_vc.WsForce);
				}
				if (result == null)
				{
					var layout = XmlVc.GetNodeForPart(hvo, m_layoutName, true, m_sda, m_layouts);
					if (layout == null)
					{
						return new string[0]; // cell will be empty.
					}
					result = StringsFor(hvo, layout, m_vc.WsForce);
				}
				return result ?? new string[0];
			}
			catch (Exception e)
			{
				throw new Exception("Failed to get strings for object " + hvo, e);
			}
		}

		/// <summary>
		/// Figure the node and hvo that correspond to the particular sort item,
		/// and generate its key. This figures out the XML needed for the particular
		/// key object, and interprets it to make a key.
		/// </summary>
		public string[] Strings(IManyOnePathSortItem item, bool sortedFromEnd)
		{
			var result = Key(item, true).Text;
			if (result == null)
			{
				return new string[0];
			}
			if (sortedFromEnd)
			{
				result = TsStringUtils.ReverseString(result);
			}
			return new[] { result };
		}

		/// <summary>
		/// Keys the specified item.
		/// </summary>
		public ITsString Key(IManyOnePathSortItem item)
		{
			return Key(item, false);
		}

		/// <summary>
		/// Keys the specified item.
		/// </summary>
		public ITsString Key(IManyOnePathSortItem item, bool fForSorting)
		{
			if (m_cache == null)
			{
				throw new ApplicationException("There's no way the browse VC (m_vc) can get a string in its current state.");
			}
			var hvo = item.RootObjectHvo;
			var collector = fForSorting ? new SortCollectorEnv(null, m_sda, hvo) : new TsStringCollectorEnv(null, m_sda, hvo);
			// This will check to see if the VC is either null or disposed.  The disposed check is necessary because
			// there are several instances where we can have a reference to an instance that was disposed, which will
			// cause problems later on.
			// Enhance CurtisH/EricP: If this VC gets used in other places, rather than adding more checks like this one,
			// it may be better to refactor ViewBase to cause it to reload the sorter and filter from persistence
			// every time the tool is changed
			if (m_vc == null)
			{
				m_vc = new XmlBrowseViewVc(m_cache)
				{
					// we won't dispose of it, so it mustn't make pictures (which we don't need)
					SuppressPictures = true,
					DataAccess = m_sda
				};
			}
			else
			{
				if (m_vc.Cache == null)
				{
					m_vc.Cache = m_cache;
				}
				if (m_vc.Cache == null)
				{
					throw new ApplicationException("There's no way the browse VC (m_vc) can get a string in its current state.");
				}
			}
			m_vc.DisplayCell(item, _columnSpecificationElement, hvo, collector);
			return collector.Result;
		}

		/// <summary>
		/// For most of these we want to return the same thing.
		/// </summary>
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
		public void CollectItems(int hvo, List<IManyOnePathSortItem> collector)
		{
			XmlViewsUtils.CollectBrowseItems(hvo, _columnSpecificationElement, collector, m_mdc, m_sda, m_layouts);
		}

		private string[] StringsFor(int hvo, XElement layout, int wsForce)
		{
			return XmlViewsUtils.StringsFor(m_cache, m_cache.DomainDataByFlid, layout, hvo, m_layouts, null, wsForce);
		}

		/// <summary>
		/// Answer true if they are the 'same' finder (will find the same strings).
		/// </summary>
		public virtual bool SameFinder(IStringFinder other)
		{
			return other is LayoutFinder otherLayoutFinder && SameLayoutName(otherLayoutFinder) && SameData(otherLayoutFinder) && SameConfiguration(otherLayoutFinder);
		}

		private bool SameLayoutName(LayoutFinder otherLayoutFinder)
		{
			return otherLayoutFinder.m_layoutName == m_layoutName || (string.IsNullOrEmpty(otherLayoutFinder.m_layoutName) && string.IsNullOrEmpty(m_layoutName));
		}

		private bool SameData(LayoutFinder otherLayoutFinder)
		{
			if (otherLayoutFinder.m_sda == m_sda)
			{
				return true;
			}
			var first = RootSdaOf(m_sda);
			return first == RootSdaOf(otherLayoutFinder.m_sda) && first != null;
		}

		private static ISilDataAccessManaged RootSdaOf(ISilDataAccess sda)
		{
			if (sda is DomainDataByFlidDecoratorBase dataByFlidDecoratorBase)
			{
				return RootSdaOf(dataByFlidDecoratorBase.BaseSda);
			}
			return sda as ISilDataAccessManaged;
		}

		private bool SameConfiguration(LayoutFinder otherLayoutFinder)
		{
			// XmlUtils.NodesMatch() is too strict a comparison for identifying column configurations that will
			// display the same value (e.g. we don't care about differences in width), causing us to
			// lose the sort arrow when switching between tools sharing common columns (LT-2858).
			// For now, just assume that columns with the same label will display the same value.
			// If this proves too loose for a particular column, try implementing a sortmethod instead.
			var colSpecLabel = XmlUtils.GetMandatoryAttributeValue(_columnSpecificationElement, "label");
			var otherLayoutFinderLabel = XmlUtils.GetMandatoryAttributeValue(otherLayoutFinder._columnSpecificationElement, "label");
			var colSpecLabel2 = XmlUtils.GetOptionalAttributeValue(_columnSpecificationElement, "headerlabel");
			var otherLayoutFinderLabel2 = XmlUtils.GetOptionalAttributeValue(otherLayoutFinder._columnSpecificationElement, "headerlabel");
			return colSpecLabel == otherLayoutFinderLabel || colSpecLabel == otherLayoutFinderLabel2 || colSpecLabel2 == otherLayoutFinderLabel || colSpecLabel2 == otherLayoutFinderLabel2 && otherLayoutFinderLabel2 != null;
		}

		/// <summary>
		/// Called in advance of 'finding' strings for many instances, typically all or most
		/// of the ones in existence. May preload data to make such a large succession of finds
		/// more efficient. This one looks to see whether its ColSpec specifies a preload,
		/// and if so, invokes it.
		/// </summary>
		public void Preload(ICmObject rootObj)
		{
			m_vc?.SetReversalWritingSystemFromRootObject(rootObj);
			var preload = XmlUtils.GetOptionalAttributeValue(_columnSpecificationElement, "preload", null);
			if (string.IsNullOrEmpty(preload))
			{
				return;
			}
			var splits = preload.Split('.');
			if (splits.Length != 2)
			{
				return; // ignore faulty ones
			}
			var className = splits[0];
			var methodName = splits[1];
			// Get the directory where our DLLs live
			var baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			// For now we assume an LCM class.
			var lcmAssembly = Assembly.LoadFrom(Path.Combine(baseDir, "SIL.LCModel.dll"));
			var targetType = lcmAssembly.GetType("SIL.LCModel." + className);
			if (targetType == null)
			{
				return;
			}
			var info = targetType.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public);
			if (info == null)
			{
				return;
			}
			info.Invoke(null, new object[] { m_cache });
		}

		#endregion

		#region IPersistAsXml Members

		/// <summary>
		/// Persists as XML.
		/// </summary>
		public virtual void PersistAsXml(XElement element)
		{
			XmlUtils.SetAttribute(element, "layout", m_layoutName ?? string.Empty);
			element.Add(_columnSpecificationElement);
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
				{
					return;
				}
				m_sda = value.DomainDataByFlid;
				m_cache = value;
				m_mdc = value.DomainDataByFlid.MetaDataCache;
				m_layouts = new LayoutCache(m_mdc, m_cache.ProjectId.Name, m_app?.ApplicationName ?? FwUtilsConstants.ksFlexAppName, m_cache.ProjectId.ProjectFolder);
				// The VC is set after the cache when created by the view, but it uses a
				// 'real' VC that already has a cache.
				// When the VC is created by restoring a persisted layout finder, the cache
				// is set later, and needs to be copied to the VC.
				if (m_vc != null)
				{
					m_vc.Cache = value;
				}
			}
		}
		#endregion
	}
}