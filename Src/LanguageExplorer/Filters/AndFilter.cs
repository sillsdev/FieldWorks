// Copyright (c) 2004-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Filters
{
	/// <summary>
	/// An AndFilter is initialized with a sequence of other RecordFilters, and accepts anything
	/// they all accept. Typically there is one item for each non-blank cell in the FilterBar,
	/// plus possibly the original filter that helps define the collection we are viewing.
	/// </summary>
	internal sealed class AndFilter : RecordFilter
	{
		/// <summary>
		/// Regular usage.
		/// </summary>
		internal AndFilter()
		{
			Filters = new List<IRecordFilter>();
		}

		/// <summary>
		/// For use with IPersistAsXml
		/// </summary>
		internal AndFilter(IPersistAsXmlFactory factory, XElement element)
			: base(element)
		{
			Filters = new List<IRecordFilter>(element.Elements().Count());
			foreach (var child in element.Elements())
			{
				Filters.Add(factory.Create<IRecordFilter>(child));
			}
		}

		/// <summary>
		/// decide whether this object should be included
		/// </summary>
		public override bool Accept(IManyOnePathSortItem item)
		{
			return Filters.All(f => f.Accept(item));
		}

		/// <summary>
		/// Gets the filters.
		/// </summary>
		internal List<IRecordFilter> Filters { get; } = new List<IRecordFilter>();

		/// <summary>
		/// Adds the specified filter.
		/// </summary>
		internal void Add(IRecordFilter newbieFilter)
		{
			Debug.Assert(!Contains(newbieFilter), "This filter (" + newbieFilter + ") has already been added to the list.");
			Filters.Add(newbieFilter);
		}

		/// <summary>
		/// Removes the specified filter.
		/// </summary>
		internal void Remove(IRecordFilter gonnerFilter)
		{
			for (var i = 0; i < Filters.Count; ++i)
			{
				if (Filters[i].SameFilter(gonnerFilter))
				{
					Filters.RemoveAt(i);
					break;
				}
			}
		}

		/// <summary>
		/// Gets the count.
		/// </summary>
		internal int Count => Filters.Count;

		/// <summary>
		/// Persists as XML.
		/// </summary>
		public override void PersistAsXml(XElement element)
		{
			base.PersistAsXml(element);
			foreach (var rf in Filters)
			{
				LanguageExplorerServices.PersistObject(rf, element, "filter");
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
					if (obj is IStoresLcmCache cacheStorer)
					{
						cacheStorer.Cache = value;
					}
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
					if (obj is IStoresDataAccess storesDataAccess)
					{
						storesDataAccess.DataAccess = value;
					}
				}
			}
		}

		/// <summary>
		///  An AndFilter is user-visible if ANY of its components is.
		/// </summary>
		public override bool IsUserVisible
		{
			get
			{
				return Filters.Any(f => f.IsUserVisible);
			}
		}

		/// <summary>
		/// Does our AndFilter Contain the other recordfilter or one equal to it? If so answer the equal one.
		/// </summary>
		protected internal override IRecordFilter EqualContainedFilter(IRecordFilter other)
		{
			return Filters.Cast<RecordFilter>().Select(recordFilter => recordFilter.EqualContainedFilter(other)).FirstOrDefault();
		}
	}
}