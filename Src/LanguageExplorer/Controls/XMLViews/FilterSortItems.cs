// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.ObjectModel;
using System.Xml.Linq;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// List of FilterSortItems that can also be accessed by the XML spec.
	/// </summary>
	internal class FilterSortItems : KeyedCollection<XElement, FilterSortItem>
	{
		protected override XElement GetKeyForItem(FilterSortItem item)
		{
			return item.Spec;
		}
	}
}