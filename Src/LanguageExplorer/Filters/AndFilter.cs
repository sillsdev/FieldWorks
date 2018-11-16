// Copyright (c) 2004-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Filters
{
	/// <summary>
	/// An AndFilter is initialized with a sequence of other RecordFilters, and accepts anything
	/// they all accept. Typically there is one item for each non-blank cell in the FilterBar,
	/// plus possibly the original filter that helps define the collection we are viewing.
	/// </summary>
	public class AndFilter : RecordFilter
	{
		/// <summary>
		/// decide whether this object should be included
		/// </summary>
		public override bool Accept(IManyOnePathSortItem item)
		{
			foreach (RecordFilter f in Filters)
			{
				if (!f.Accept(item))
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Gets the filters.
		/// </summary>
		public ArrayList Filters { get; private set; } = new ArrayList();

		/// <summary>
		/// Adds the specified f.
		/// </summary>
		public void Add(RecordFilter f)
		{
			Debug.Assert(!Contains(f), "This filter (" + f + ") has already been added to the list.");
			Filters.Add(f);
		}

		/// <summary>
		/// Removes the specified f.
		/// </summary>
		public void Remove(RecordFilter f)
		{
			for (var i = 0; i < Filters.Count; ++i)
			{
				if (((RecordFilter)Filters[i]).SameFilter(f))
				{
					Filters.RemoveAt(i);
					break;
				}
			}
		}

		/// <summary>
		/// Gets the count.
		/// </summary>
		public int Count => Filters.Count;

		/// <summary>
		/// Persists as XML.
		/// </summary>
		public override void PersistAsXml(XElement element)
		{
			base.PersistAsXml(element);
			foreach (RecordFilter rf in Filters)
			{
				DynamicLoader.PersistObject(rf, element, "filter");
			}
		}

		/// <summary>
		/// Inits the XML.
		/// </summary>
		public override void InitXml(XElement element)
		{
			base.InitXml (element);
			Debug.Assert(Filters != null && Filters.Count == 0);
			Filters = new ArrayList(element.Elements().Count());
			foreach (var child in element.Elements())
			{
				Filters.Add(DynamicLoader.RestoreFromChild(child, "."));
			}
		}

		/// <summary>
		/// Set the cache.
		/// </summary>
		public override LcmCache Cache
		{
			set
			{
				base.Cache = value;
				foreach (var obj in Filters)
				{
					SetCache(obj, value);
				}
			}
		}

		/// <summary>
		/// Pass it on to any subfilters that can use it.
		/// </summary>
		public override ISilDataAccess DataAccess
		{
			set
			{
				base.DataAccess = value;
				foreach (var obj in Filters)
				{
					if (obj is IStoresDataAccess)
					{
						((IStoresDataAccess)obj).DataAccess = value;
					}
				}
			}
		}

		/// <summary>
		///  An AndFilter is user-visible if ANY of its compoents is.
		/// </summary>
		public override bool IsUserVisible
		{
			get
			{
				return Filters.Cast<RecordFilter>().Any(f => f.IsUserVisible);
			}
		}

		/// <summary>
		/// Does our AndFilter Contain the other recordfilter or one equal to it? If so answer the equal one.
		/// </summary>
		public override RecordFilter EqualContainedFilter(RecordFilter other)
		{
			for (var i = 0; i < Count; ++i)
			{
				var filter = Filters[i] as RecordFilter;
				var result = filter.EqualContainedFilter(other);
				if (result != null)
				{
					return result;
				}
			}
			return null;
		}
	}
}