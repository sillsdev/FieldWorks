// Copyright (c) 2004-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Filters
{
	/// <summary>
	/// not certain we will want to continue to have this... it simplifies the task I have
	/// at hand, which is providing a way,from the menu bar to clear all filters
	///
	/// the RecordList will recognize this has been set and will actually clear
	/// its filter... so this will not actually be used and us what actually slow down showing everything.
	/// </summary>
	public class NullFilter : RecordFilter
	{
		/// <summary />
		public NullFilter()
		{
			Name = FiltersStrings.ksNoFilter;
			id = "No Filter";
		}

		/// <summary>
		/// Gets the name of the image.
		/// </summary>
		public override string imageName => "NoFilter";

		/// <summary>
		/// decide whether this object should be included
		/// </summary>
		public override bool Accept(IManyOnePathSortItem item)
		{
			return true;
		}
	}
}