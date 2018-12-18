// Copyright (c) 2004-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// <remarks>
//	At the moment (July 2004), the only instances of this class do filtering in memory.
//	This does not imply that all filtering will always be done in memory, only that we haven't
//	yet designed or implemented a way to do the filtering while querying.
//		when we do, we will probably split RecordFilter into two subclasses, one for in memory
//	filters which can use LCM properties, and one which will somehow contribute to the Where
//	clause which populates the RecordList. In that case, the RecordList will be aware of the filter
//	and will be responsible for somehow incorporating its filtering desires as it does the query.

// (JohnT, May 2004). Added additional classes and interfaces designed to support a filter bar
// in a Browse view. (Should these be spread across more files?)

// The filter bar looks like a set of combos, one per column, which the user can fill in to limit
// the items shown in the view to ones that satisfy some condition.

// The idea here is that, given the XML that defines a cell view, if it is one of the types we
// recognize, we instantiate a corresponding implementation of the StringFinder interface, which
// allows us to obtain the strings shown in the cell given the base object.

// Then, given the full object list, we populate a combo box menu with 'blanks', 'non-blanks',
// and a list of the unique values that occur (found by applying the string finder to the objects;
// if more than about 30 are found, give up). Menu could also contain 'use patterns' (then the user
// may type a regexp); a submenu of pattern functions; 'Any of...' (brings up a dialog with the
// complete list of values that occur, and check boxes); 'Any except...' (similar); and perhaps
// eventually something like what the 'Custom...' item brings up in Excel autofilters. The user can
// also just type a string; without pattern matching, only initial and final # (start and end of string)
// have special meaning.
//
// When the user selects a filter option for a cell, we instantiate the appropriate kind of Matcher,
// and combine this with the string finder to make a FilterBarCellFilter. If more than one cell
// is set, or there was an original filter defining the list, we further combine all the filters
// using an AndFilter.
// </remarks>

using System.Xml.Linq;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Xml;

namespace LanguageExplorer.Filters
{
	/// <summary />
	public abstract class RecordFilter : IPersistAsXml, IStoresLcmCache, IStoresDataAccess
	{
		/// <summary />
		protected RecordFilter()
		{
			Name = FiltersStrings.ksUnknown;
			id = "Unknown";
		}


		/// <summary>
		/// Set the cache on the specified object if it wants it.
		/// </summary>
		internal void SetCache(object obj, LcmCache cache)
		{
			var sfc = obj as IStoresLcmCache;
			if (sfc != null)
			{
				sfc.Cache = cache;
			}
		}

		internal void SetDataAccess(object obj, ISilDataAccess sda)
		{
			var sfc = obj as IStoresDataAccess;
			if (sfc != null)
			{
				sfc.DataAccess = sda;
			}
		}

		/// <summary>
		/// Gets the name.
		/// </summary>
		public string Name { get; protected set; }

		/// <summary>
		/// this is used, for example, and persist in the selected id in the users xml preferences
		/// </summary>
		public string id { get; protected set; }

		/// <summary>
		/// Gets the name of the image.
		/// </summary>
		public virtual string imageName => "SimpleFilter";

		/// <summary>
		/// May be used to preload data for efficient filtering of many instances.
		/// </summary>
		public virtual void Preload(object rootObj)
		{
		}

		/// <summary>
		/// decide whether this object should be included
		/// </summary>
		public abstract bool Accept(IManyOnePathSortItem item);

		/// <summary>
		/// Tells whether the filter should be 'visible' to the user, in the sense that the
		/// status bar pane for 'Filtered' turns on. Some filters should not show up here,
		/// for example, built-in ones that define the possible contents of a view.
		/// By default a filter is not visible.
		/// </summary>
		public virtual bool IsUserVisible => false;

		/// <summary>
		/// Tells whether the filter is currently valid.  This is true by default.
		/// </summary>
		public virtual bool IsValid => true;

		/// <summary>
		/// a factory method for filters
		/// </summary>
		public static RecordFilter Create(LcmCache cache, XElement configuration)
		{
			var filter = (RecordFilter)DynamicLoader.CreateObject(configuration);
			filter.Init(cache, configuration);
			return filter;
		}

		/// <summary>
		/// Initialize the filter
		/// </summary>
		public virtual void Init(LcmCache cache, XElement filterNode)
		{
		}

		/// <summary>
		/// This is the start of an equality test for filters, but for now I (JohnT) am not
		/// making it an actual Equals function, since it may not be robust enough to
		/// satisfy all the functions of Equals, and I don't want to mess with changing the
		/// hash function. It is mainly for FilterBarRecordFilters, so for now other classes
		/// just answer false.
		/// </summary>
		public virtual bool SameFilter(RecordFilter other)
		{
			return other == this;
		}

		/// <summary>
		/// If this or a contained filter is considered equal to the argument, answer true
		/// </summary>
		public bool Contains(RecordFilter other)
		{
			return EqualContainedFilter(other) != null;
		}

		/// <summary>
		/// If this or a contained filter is considered equal to the argument, answer the filter
		/// or subfilter that is consiered equal.
		/// Record Filters that can contain more than one filter (e.g. AndFilter) should override this.
		/// </summary>
		public virtual RecordFilter EqualContainedFilter(RecordFilter other)
		{
			return SameFilter(other) ? this : null;
		}

		#region IPersistAsXml Members

		/// <summary>
		/// Persists as XML.
		/// </summary>
		public virtual void PersistAsXml(XElement element)
		{
			XmlUtils.SetAttribute(element, "name", Name);
		}

		/// <summary>
		/// Inits the XML.
		/// </summary>
		public virtual void InitXml(XElement element)
		{
			Name = XmlUtils.GetMandatoryAttributeValue(element, "name");
		}

		#endregion

		#region IStoresLcmCache

		/// <summary>
		/// Set the cache.
		/// </summary>
		public virtual LcmCache Cache
		{
			set
			{
				//nothing to do
			}
		}
		#endregion

		/// <summary>
		/// Allows setting some data access other than the one derived from the Cache.
		/// To have this effect, it must be called AFTER setting the cache.
		/// </summary>
		public virtual ISilDataAccess DataAccess
		{
			set { }
		}
	}
}