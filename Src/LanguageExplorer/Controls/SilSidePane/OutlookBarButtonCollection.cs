// SilSidePane, Copyright 2009 SIL International. All rights reserved.
// SilSidePane is licensed under the Code Project Open License (CPOL), <http://www.codeproject.com/info/cpol10.aspx>.
// Derived from OutlookBar v2 2005 <http://www.codeproject.com/KB/vb/OutlookBar.aspx>, Copyright 2007 by Star Vega.
// Changed in 2008 and 2009 by SIL International to convert to C# and add more functionality.

using System;
using System.Collections;
using System.Drawing;

namespace LanguageExplorer.Controls.SilSidePane
{
	/// <summary></summary>
	internal class OutlookBarButtonCollection : CollectionBase
	{
		private readonly OutlookBar Owner;

		/// <summary></summary>
		public OutlookBarButtonCollection(OutlookBar owner)	: base()
		{
			Owner = owner;
		}

		/// <summary></summary>
		public OutlookBarButton this[int index]
		{
			get { return List[index] as OutlookBarButton; }
		}

		///// <summary></summary>
		//public OutlookBarButton Item(int index)
		//{
		//    return List[index] as OutlookBarButton;
		//}

		/// <summary></summary>
		public OutlookBarButton this[string text]
		{
			get
			{
				foreach (OutlookBarButton b in List)
				{
					if (b.Text.Equals(text))
						return b;
				}

				return null;
			}
		}

		/// <summary></summary>
		internal OutlookBarButton GetItem(int x, int y)
		{
			foreach (OutlookBarButton b in List)
			{
				if (b.Rectangle != Rectangle.Empty && b.Rectangle.Contains(new Point(x, y)))
					return b;

				//if (!(b.Rectangle == null)
				//{
				//    if (b.Rectangle.Contains(new Point(x, y))) return b;
				//}
			}

			return null;
		}

		/// <summary></summary>
		public void Add(OutlookBarButton item)
		{
			item.Owner = this.Owner;
			List.Add(item);
		}

		/// <summary></summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public void AddRange(OutlookBarButtonCollection items)
		{
			foreach (OutlookBarButton item in items)
				this.Add(item);
		}

		/// <summary></summary>
		public int IndexOf(OutlookBarButton item)
		{
			return List.IndexOf(item);
		}

		/// <summary></summary>
		public void Insert(int index, OutlookBarButton value)
		{
			List.Insert(index, value);
		}

		/// <summary></summary>
		public void Remove(OutlookBarButton value)
		{
			List.Remove(value);
		}

		/// <summary></summary>
		public bool Contains(OutlookBarButton item)
		{
			return List.Contains(item);
		}

		/// <summary></summary>
		protected override void OnValidate(object value)
		{
			if (!typeof(OutlookBarButton).IsAssignableFrom(value.GetType()))
				throw new ArgumentException("value must be of type OutlookBarButton.", "value");
		}

		/// <summary></summary>
		public int VisibleCount
		{
			get
			{
				int count = 0;
				foreach (OutlookBarButton b in this.List)
				{
					if (b.Visible & b.Allowed)
						count++;
				}

				return count;
			}
		}
	}
}
