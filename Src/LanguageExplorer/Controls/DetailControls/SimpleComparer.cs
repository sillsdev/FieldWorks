// Copyright (c) 2005-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary>
	/// This code provides a simple comparison function for keyed objects.
	/// The strings are compared using simple string comparison.
	/// </summary>
	public class SimpleComparer : IComparer
	{
		public SimpleComparer()
		{
		}
		public int Compare(object x, object y)
		{
			var sX = ((IKeyedObject)x).Key;
			var sY = ((IKeyedObject)y).Key;
			return string.Compare(sX, sY, true);
		}
	}
}
