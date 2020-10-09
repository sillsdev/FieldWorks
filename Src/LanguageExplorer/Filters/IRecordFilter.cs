// Copyright (c) 2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml.Linq;
using SIL.LCModel;

namespace LanguageExplorer.Filters
{
	public interface IRecordFilter : IPersistAsXml, IStoresLcmCache, IStoresDataAccess
	{
		/// <summary>
		/// Gets the name.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// this is used, for example, and persist in the selected id in the users xml preferences
		/// </summary>
		string Id { get; }

		/// <summary>
		/// Tells whether the filter should be 'visible' to the user, in the sense that the
		/// status bar pane for 'Filtered' turns on. Some filters should not show up here,
		/// for example, built-in ones that define the possible contents of a view.
		/// By default a filter is not visible.
		/// </summary>
		bool IsUserVisible { get; }

		/// <summary>
		/// Tells whether the filter is currently valid.  This is true by default.
		/// </summary>
		bool IsValid { get; }

		/// <summary>
		/// May be used to preload data for efficient filtering of many instances.
		/// </summary>
		void Preload(ICmObject rootObj);

		/// <summary>
		/// decide whether this object should be included
		/// </summary>
		bool Accept(IManyOnePathSortItem item);

#if RANDYTODO
		// TODO: See if this method can go away, when the filter factory is set up. A simple constructor might be a place to do this.
#endif
		/// <summary>
		/// Initialize the filter
		/// </summary>
		void Init(LcmCache cache, XElement filterNode);

		/// <summary>
		/// This is the start of an equality test for filters, but for now I (JohnT) am not
		/// making it an actual Equals function, since it may not be robust enough to
		/// satisfy all the functions of Equals, and I don't want to mess with changing the
		/// hash function. It is mainly for FilterBarRecordFilters, so for now other classes
		/// just answer false.
		/// </summary>
		bool SameFilter(IRecordFilter other);

		/// <summary>
		/// If this or a contained filter is considered equal to the argument, answer true
		/// </summary>
		bool Contains(IRecordFilter other);
	}
}