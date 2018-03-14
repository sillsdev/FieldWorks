// Copyright (c) 2006-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Xml.Linq;
using LanguageExplorer.Filters;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.Xml;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// A list choice filter accepts items based on whether an object field in the root object
	/// contains one or more of a list of values.
	///
	/// Enhance: special case when first flid in sort item matches our flid: this means
	/// we are only showing one item from the property in this row. In this case, for Any
	/// show items in the list, for None show items not in the list, for All probably show items in the list.
	/// </summary>
	public abstract class ListChoiceFilter : RecordFilter
	{
		private bool m_fIsUserVisible;

		/// <summary />
		protected LcmCache m_cache;
		/// <summary>
		/// May be derived from cache or set separately.
		/// </summary>
		protected ISilDataAccess m_sda;
		HashSet<int> m_targets;
		int[] m_originalTargets;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:ListChoiceFilter"/> class.
		/// </summary>
		protected ListChoiceFilter(LcmCache cache, ListMatchOptions mode, int[] targets)
		{
			m_cache = cache;
			Mode = mode;
			Targets = targets;
		}

		/// <summary>
		/// Need zero-argument constructor for persistence. Don't use otherwise.
		/// </summary>
		protected ListChoiceFilter()
		{
		}

		internal int[] Targets
		{
			get { return m_originalTargets; }
			set
			{
				m_originalTargets = value;
				m_targets = new HashSet<int>(value);
			}
		}
		internal ListMatchOptions Mode { get; private set; }

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
			// any or none: look for first match
			foreach (var hvo in values)
			{
				if (m_targets.Contains(hvo))
				{
					// If we wanted any, finding one is a success; if we wanted none, finding any is a failure.
					return Mode == ListMatchOptions.Any;
				}
			}
			// If we wanted any, not finding any is failure; if we wanted none, not finding any is success.
			return Mode != ListMatchOptions.Any;
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
			set
			{
				m_sda = value;
			}
		}

		/// <summary>
		/// Persists as XML.
		/// </summary>
		public override void PersistAsXml(XElement node)
		{
			base.PersistAsXml(node);
			XmlUtils.SetAttribute(node, "mode", ((int)Mode).ToString());
			XmlUtils.SetAttribute(node, "targets", XmlUtils.MakeIntegerListValue(m_originalTargets));
			if (m_fIsUserVisible)
			{
				XmlUtils.SetAttribute(node, "visible", "true");
			}
		}

		/// <summary>
		/// Inits the XML.
		/// </summary>
		public override void InitXml(XElement node)
		{
			base.InitXml(node);
			Mode = (ListMatchOptions)XmlUtils.GetMandatoryIntegerAttributeValue(node, "mode");
			Targets = XmlUtils.GetMandatoryIntegerListAttributeValue(node, "targets");
			m_fIsUserVisible = XmlUtils.GetBooleanAttributeValue(node, "visible");
		}

		/// <summary>
		/// Compatibles the filter.
		/// </summary>
		public virtual bool CompatibleFilter(XElement colSpec)
		{
			return BeSpec == XmlUtils.GetOptionalAttributeValue(colSpec, "bulkEdit", null)
			       || BeSpec == XmlUtils.GetOptionalAttributeValue(colSpec, "chooserFilter", null);
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
		public void MakeUserVisible(bool fVisible)
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
				if (m_cache == null)
				{
					return false;
				}
				foreach (var hvo in m_targets)
				{
					ICmObject dummy;
					if (!m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().TryGetObject(hvo, out dummy))
					{
						return false;
					}
				}
				return true;
			}
		}
	}
}