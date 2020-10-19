// Copyright (c) 2006-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.Xml;

namespace LanguageExplorer.Filters
{
	/// <summary>
	/// A list choice filter accepts items based on whether an object field in the root object
	/// contains one or more of a list of values.
	///
	/// Enhance: special case when first flid in sort item matches our flid: this means
	/// we are only showing one item from the property in this row. In this case, for Any
	/// show items in the list, for None show items not in the list, for All probably show items in the list.
	/// </summary>
	internal abstract class ListChoiceFilter : RecordFilter
	{
		private bool m_fIsUserVisible;
		/// <summary />
		protected LcmCache m_cache;
		/// <summary>
		/// May be derived from cache or set separately.
		/// </summary>
		protected ISilDataAccess m_sda;
		private HashSet<int> m_targets;
		private int[] m_originalTargets;

		/// <summary />
		protected ListChoiceFilter(LcmCache cache, ListMatchOptions mode, int[] targets)
		{
			m_cache = cache;
			Mode = mode;
			Targets = targets;
		}

		/// <summary>
		/// For use with IPersistAsXml
		/// </summary>
		protected ListChoiceFilter(XElement element)
			: base(element)
		{
			Mode = (ListMatchOptions)XmlUtils.GetMandatoryIntegerAttributeValue(element, "mode");
			Targets = XmlUtils.GetMandatoryIntegerListAttributeValue(element, "targets");
			m_fIsUserVisible = XmlUtils.GetBooleanAttributeValue(element, "visible");
		}

		internal int[] Targets
		{
			get => m_originalTargets;
			set
			{
				m_originalTargets = value;
				m_targets = new HashSet<int>(value);
			}
		}

		internal ListMatchOptions Mode { get; }

		/// <summary>
		/// decide whether this object should be included
		/// </summary>
		public override bool Accept(IManyOnePathSortItem item)
		{
			var values = GetItems(item);
			if (Mode == ListMatchOptions.All || Mode == ListMatchOptions.Exact)
			{
				var matches = new HashSet<int>();
				foreach (var hvo in values)
				{
					if (m_targets.Contains(hvo))
					{
						matches.Add(hvo);
						if (Mode == ListMatchOptions.All && matches.Count == m_targets.Count)
						{
							return true;
						}
					}
					else if (Mode == ListMatchOptions.Exact)
					{
						return false; // found one that isn't present.
					}
				}
				return matches.Count == m_targets.Count; // success if we found them all.
			}
			return values.Any(hvo => m_targets.Contains(hvo)) ? Mode == ListMatchOptions.Any : Mode != ListMatchOptions.Any;
		}

		/// <summary>
		/// Gets the items.
		/// </summary>
		protected abstract int[] GetItems(IManyOnePathSortItem item);

		/// <summary>
		/// Set the cache.
		/// </summary>
		public override LcmCache Cache
		{
			set
			{
				base.Cache = value;
				m_cache = value;
				m_sda = value.DomainDataByFlid;
			}
		}

		/// <summary>
		/// Allows setting some data access other than the one derived from the Cache.
		/// To have this effect, it must be called AFTER setting the cache.
		/// </summary>
		public override ISilDataAccess DataAccess
		{
			set => m_sda = value;
		}

		/// <summary>
		/// Persists as XML.
		/// </summary>
		public override void PersistAsXml(XElement element)
		{
			base.PersistAsXml(element);
			XmlUtils.SetAttribute(element, "mode", ((int)Mode).ToString());
			XmlUtils.SetAttribute(element, "targets", XmlUtils.MakeIntegerListValue(m_originalTargets));
			if (m_fIsUserVisible)
			{
				XmlUtils.SetAttribute(element, "visible", "true");
			}
		}

		/// <summary>
		/// Compatible check of the filter.
		/// </summary>
		internal virtual bool CompatibleFilter(XElement colSpec)
		{
			var beSpec = BeSpec;
			return beSpec == XmlUtils.GetOptionalAttributeValue(colSpec, "bulkEdit", null) || beSpec == XmlUtils.GetOptionalAttributeValue(colSpec, "chooserFilter", null);
		}

		// The value of the "bulkEdit" property that causes this kind of filter to be created.
		/// <summary>
		/// Gets the be spec.
		/// </summary>
		protected abstract string BeSpec { get; }

		/// <summary>
		/// Tells whether the filter should be 'visible' to the user, in the sense that the
		/// status bar pane for 'Filtered' turns on. Some filters should not show up here,
		/// for example, built-in ones that define the possible contents of a view.
		/// By default a filter is not visible.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is user visible; otherwise, <c>false</c>.
		/// </value>
		public override bool IsUserVisible => m_fIsUserVisible;

		/// <summary>
		/// Provides a setter for IsUserVisible.  Needed to fix LT-6250.
		/// </summary>
		internal void MakeUserVisible(bool fVisible)
		{
			m_fIsUserVisible = fVisible;
		}

		/// <summary>
		/// This filter is valid only if all of the targets are valid object ids.
		/// </summary>
		public override bool IsValid
		{
			get
			{
				// We don't want to crash if we haven't been properly initialized!  See LT-9731.
				// And this  filter probably isn't valid anyway.
				return m_cache != null && m_targets.All(hvo => m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().TryGetObject(hvo, out _));
			}
		}
	}
}