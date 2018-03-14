// Copyright (c) 2004-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Xml.Linq;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.WritingSystems;
using SIL.Xml;

namespace LanguageExplorer.Filters
{
	/// <summary />
	internal class LcmCompare : IComparer, IPersistAsXml
	{
		/// <summary />
		protected ICollator m_collater;
		/// <summary />
		protected bool m_fUseKeys;
		internal INoteComparision ComparisonNoter { get; set; }
		private Dictionary<object, string> m_sortKeyCache;

		private LcmCache m_cache;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:LcmCompare"/> class.
		/// </summary>
		public LcmCompare(string propertyName, LcmCache cache)
		{
			m_cache = cache;
			Init();
			PropertyName= propertyName;
			m_fUseKeys = propertyName == "ShortName";
			m_sortKeyCache = new Dictionary<object, string>();
		}

		/// <summary>
		/// This constructor is intended to be used for persistence with IPersistAsXml
		/// </summary>
		public LcmCompare()
		{
			Init();
		}

		private void Init()
		{
			m_collater = null;
		}

		/// <summary>
		/// Gets the name of the property.
		/// </summary>
		public string PropertyName { get; protected set; }

		/// <summary>
		/// Add to the specified XML node information required to create a new
		/// object equivalent to yourself. The node already contains information
		/// sufficient to create an instance of the proper class.
		/// </summary>
		public void PersistAsXml(XElement node)
		{
			XmlUtils.SetAttribute(node, "property", PropertyName);
		}

		/// <summary>
		/// Initialize an instance into the state indicated by the node, which was
		/// created by a call to PersistAsXml.
		/// </summary>
		public void InitXml(XElement node)
		{
			PropertyName = XmlUtils.GetMandatoryAttributeValue(node, "property");
		}

		/// <summary>
		/// Gets the property.
		/// </summary>
		protected object GetProperty(ICmObject target, string property)
		{
			var type = target.GetType();
			var info = type.GetProperty(property,
				BindingFlags.Instance |
				BindingFlags.Public |
				BindingFlags.FlattenHierarchy);
			if (info == null)
			{
				throw new ArgumentException($"There is no public property named '{property}' in {type}. Remember, properties often end in a multi-character suffix such as OA, OS, RA, RS, or Accessor.");
			}

			return info.GetValue(target,null);
		}

		/// <summary>
		/// Opens the collating engine.
		/// </summary>
		public void OpenCollatingEngine(string sWs)
		{
			m_collater = m_cache.ServiceLocator.WritingSystemManager.Get(sWs).DefaultCollation.Collator;
		}

		/// <summary>
		/// Closes the collating engine.
		/// </summary>
		public void CloseCollatingEngine()
		{
		}

		private ICmObject GetObjFromItem(object x)
		{
			var itemX = x as IManyOnePathSortItem;
			// This is slightly clumsy but currently it's the only way we have to get a cache.
			return itemX.KeyObjectUsing(m_cache);
		}

		// Compare two objects (expected to be ManyOnePathSortItems).
		/// <summary>
		/// Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.
		/// </summary>
		/// <param name="x">The first object to compare.</param>
		/// <param name="y">The second object to compare.</param>
		/// <returns>
		/// Value Condition Less than zero x is less than y. Zero x equals y. Greater than zero x is greater than y.
		/// </returns>
		/// <exception cref="T:System.ArgumentException">Neither x nor y implements the <see cref="T:System.IComparable"></see> interface.-or- x and y are of different types and neither one can handle comparisons with the other. </exception>
		public int Compare(object x, object y)
		{
			ComparisonNoter?.ComparisonOccurred(); // for progress reporting.

			if (x == y)
			{
				return 0;
			}
			if (x == null)
			{
				return -1;
			}
			if (y == null)
			{
				return 1;
			}

			// One time overhead
			if (m_collater == null)
			{
				OpenCollatingEngine((GetObjFromItem(x)).SortKeyWs);
			}

			return m_collater.Compare(GetObjFromCacheOrItem(x), GetObjFromCacheOrItem(y));
		}

		private string GetObjFromCacheOrItem(object item)
		{
			var item1 = (ManyOnePathSortItem)item;
			string cachedKey;
			if (!m_sortKeyCache.TryGetValue(item1, out cachedKey))
			{
				if (m_fUseKeys)
				{
					cachedKey = GetObjFromItem(item).SortKey;
				}
				else
				{
					cachedKey = (string)GetProperty(GetObjFromItem(item), PropertyName);
				}
				m_sortKeyCache.Add(item1, cachedKey);
			}
			return cachedKey;
		}

		/// <summary />
		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}
			// TODO-Linux: System.Boolean System.Type::op_Inequality(System.Type,System.Type)
			// is marked with [MonoTODO] and might not work as expected in 4.0.
			if (GetType() != obj.GetType())
			{
				return false;
			}
			var that = (LcmCompare)obj;
			return m_fUseKeys == that.m_fUseKeys && (m_collater == that.m_collater && PropertyName == that.PropertyName);
		}

		/// <summary />
		public override int GetHashCode()
		{
			var hash = GetType().GetHashCode();
			if (m_fUseKeys)
			{
				hash *= 3;
			}
			if (m_collater != null)
			{
				hash += m_collater.GetHashCode();
			}
			if (PropertyName != null)
			{
				hash *= PropertyName.GetHashCode();
			}
			return hash;
		}
	}
}