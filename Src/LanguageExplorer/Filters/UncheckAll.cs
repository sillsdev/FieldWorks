// Copyright (c) 2004-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml.Linq;

namespace LanguageExplorer.Filters
{
	/// <summary>
	/// A dummy filter to uncheck any selections in the View/Filters menu
	/// </summary>
	internal sealed class UncheckAll : NullFilter
	{
		/// <summary>
		/// For use with IPersistAsXml
		/// </summary>
		internal UncheckAll(XElement element)
			: base(element)
		{
			Name = FiltersStrings.ksUncheckAll;
		}
	}
}