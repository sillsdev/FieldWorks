// Copyright (c) 2004-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Filters
{
	/// <summary>
	/// A dummy filter to uncheck any selections in the View/Filters menu
	/// </summary>
	public class UncheckAll : NullFilter
	{
		public UncheckAll()
		{
			Name = FiltersStrings.ksUncheckAll;
		}
	}
}