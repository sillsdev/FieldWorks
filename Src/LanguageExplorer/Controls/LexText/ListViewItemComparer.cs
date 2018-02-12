// Copyright (c) 2005-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections;
using System.Windows.Forms;

namespace LanguageExplorer.Controls.LexText
{
	/// <summary>
	/// This class is the IComparer derived class for sorting the Marker
	/// list view.  It uses the columns and sort order
	/// </summary>
	public class ListViewItemComparer : IComparer
	{
		private int _col;
		private bool _ascendingOrder;
		public ListViewItemComparer(int column, bool order)
		{
			_col = column;
			_ascendingOrder = order;
		}
		public int Compare(object x, object y)
		{
			var a = ((ListViewItem)x).Tag as ContentMapping;
			var b = ((ListViewItem)y).Tag as ContentMapping;

			switch (_col)
			{
				case 1:
					if (_ascendingOrder)
					{
						return b.Order - a.Order;
					}
					return a.Order - b.Order;
				case 2:
					if (_ascendingOrder)
					{
						return b.Count - a.Count;
					}
					return a.Count - b.Count;
			}

			var aText = string.Empty;
			var bText = string.Empty;

			switch (_col)
			{
				case 0:
					aText = a.Marker;
					bText = b.Marker;
					break;
				case 3:
					aText = a.Description + "__" + a.Marker;
					bText = b.Description + "__" + b.Marker;
					break;
				case 4:
					aText = a.DestinationField + "__" + a.Marker;
					bText = b.DestinationField + "__" + b.Marker;
					break;
				case 5:
					aText = a.WritingSystem + "__" + a.Marker;
					bText = b.WritingSystem + "__" + b.Marker;
					break;
				default:
					break;
			}

			return _ascendingOrder ? string.Compare(aText, bText) : string.Compare(bText, aText);
		}
	}
}