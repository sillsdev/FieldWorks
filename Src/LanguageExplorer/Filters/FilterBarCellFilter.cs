// Copyright (c) 2004-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using System.Xml.Linq;
using System.Xml.XPath;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Filters
{
	/// <summary>
	/// A FilterBarCellFilter handles one cell of a filter bar. It is made up of two components: a StringFinder
	/// specifies how to find target strings from the hvo of the target object, while a IMatcher
	/// specifies how to determine whether a string matches. An object passes if any of the target strings
	/// produced by the finder matches the pattern.
	///
	/// The design avoids using LCM objects as performance is likely to be critical. For a long list,
	/// it would be well to pre-load the cache with all relevant properties in as few queries as
	/// possible. It is designed to allow easy conversion to passing an HVO to Accept.
	/// </summary>
	internal class FilterBarCellFilter : RecordFilter
	{
		/// <summary />
		internal FilterBarCellFilter(IStringFinder finder, IMatcher matcher)
		{
			Finder = finder;
			Matcher = matcher;
		}

		/// <summary>
		/// Default constructor for IPersistAsXml
		/// </summary>
		public FilterBarCellFilter()
		{
		}

		/// <summary>
		/// Get the finder
		/// </summary>
		public IStringFinder Finder { get; private set; }

		/// <summary>
		/// Get the matcher.
		/// </summary>
		public IMatcher Matcher { get; private set; }

		/// <summary>
		/// decide whether this object should be included
		/// </summary>
		public override bool Accept(IManyOnePathSortItem item)
		{
			return Matcher.Accept(Finder.Key(item));
		}

		/// <summary>
		/// Let your finder preload whatever it wants to.
		/// </summary>
		public override void Preload(ICmObject rootObj)
		{
			Finder.Preload(rootObj);
		}

		/// <summary>
		/// Valid if the matcher is (finder doesn't yet have a way to be invalid)
		/// </summary>
		public override bool IsValid => base.IsValid && Matcher.IsValid();

		/// <summary>
		/// This is the start of an equality test for filters, but for now I (JohnT) am not
		/// making it an actual Equals function, since it may not be robust enough to
		/// satisfy all the functions of Equals, and I don't want to mess with changing the
		/// hash function. It is mainly for FilterBarRecordFilters, so for now other classes
		/// just answer false.
		/// </summary>
		public override bool SameFilter(IRecordFilter other)
		{
			return other is FilterBarCellFilter fbcOther && fbcOther.Finder.SameFinder(Finder) && fbcOther.Matcher.SameMatcher(Matcher);
		}

		#region IPersistAsXml Members

		/// <summary>
		/// Persists as XML.
		/// </summary>
		public override void PersistAsXml(XElement element)
		{
			base.PersistAsXml(element);
			DynamicLoader.PersistObject(Finder, element, "finder");
			DynamicLoader.PersistObject(Matcher, element, "matcher");
		}

		/// <summary>
		/// Inits the XML.
		/// </summary>
		public override void InitXml(XElement element)
		{
			base.InitXml(element);
			Debug.Assert(Finder == null);
			Finder = DynamicLoader.RestoreObject<IStringFinder>(element.Element("finder"));
			Matcher = DynamicLoader.RestoreObject<IMatcher>(element.Element("matcher"));
		}

		#endregion

		/// <summary>
		/// Set the cache.
		/// </summary>
		public override LcmCache Cache
		{
			set
			{
				base.Cache = value;
				if (Finder is IStoresLcmCache finderCacheStorer)
				{
					finderCacheStorer.Cache = value;
				}
				if (Matcher is IStoresLcmCache matcherCacheStorer)
				{
					matcherCacheStorer.Cache = value;
				}
			}
		}

		public override ISilDataAccess DataAccess
		{
			set
			{
				base.DataAccess = value;
				if (Finder is IStoresDataAccess finderStoresDataAccess)
				{
					finderStoresDataAccess.DataAccess = value;
				}
				if (Matcher is IStoresDataAccess matcherStoresDataAccess)
				{
					matcherStoresDataAccess.DataAccess = value;
				}
			}
		}

		/// <summary>
		/// These filters are always created by user action in the filter bar, and thus visible to the user.
		/// </summary>
		public override bool IsUserVisible => true;
	}
}