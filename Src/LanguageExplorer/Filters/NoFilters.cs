// Copyright (c) 2004-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml.Linq;

namespace LanguageExplorer.Filters
{
	/// <summary>
	/// A filter for the View/Filter menu/toolbar to turn off all filters.
	/// </summary>
	internal sealed class NoFilters : NullFilter
	{
		/// <summary>
		/// Regular usage.
		/// </summary>
		internal NoFilters()
		{
		}
		/// <summary>
		/// For use with IPersistAsXml
		/// </summary>
		internal NoFilters(XElement element)
			: base(element)
		{
		}
	}
}