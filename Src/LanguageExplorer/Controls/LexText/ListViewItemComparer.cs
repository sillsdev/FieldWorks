// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
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
		private int col;
		private bool ascendingOrder;
		public ListViewItemComparer(int column, bool order)
		{
			col = column;
			ascendingOrder = order;
		}
		public int Compare(object x, object y)
		{
			ContentMapping a, b;
			a = ((ListViewItem)x).Tag as ContentMapping;
			b = ((ListViewItem)y).Tag as ContentMapping;

			if (col == 1)   // source order case
			{
				if (ascendingOrder)
					return b.Order - a.Order;
				return a.Order - b.Order;
			}
			else if (col == 2)  // count case
			{
				if (ascendingOrder)
					return b.Count - a.Count;
				return a.Count - b.Count;
			}

			string aText = "";
			string bText = "";

			switch (col)
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

			if (ascendingOrder)
				return String.Compare(aText, bText);
			return String.Compare(bText, aText);
		}
	}
}