// SilSidePane, Copyright 2009-2018 SIL International. All rights reserved.
// SilSidePane is licensed under the Code Project Open License (CPOL), <http://www.codeproject.com/info/cpol10.aspx>.
// Derived from OutlookBar v2 2005 <http://www.codeproject.com/KB/vb/OutlookBar.aspx>, Copyright 2007 by Star Vega.
// Changed in 2008 and 2009 by SIL International to convert to C# and add more functionality.

using System;
using System.Collections;
using System.Drawing;
using System.Linq;

namespace LanguageExplorer.Controls.SilSidePane
{
	/// <summary />
	internal class OutlookBarButtonCollection : CollectionBase
	{
		private readonly OutlookBar Owner;

		/// <summary />
		public OutlookBarButtonCollection(OutlookBar owner)	: base()
		{
			Owner = owner;
		}

		/// <summary />
		public OutlookBarButton this[int index] => (OutlookBarButton)List[index];

		/// <summary />
		public OutlookBarButton this[string text]
		{
			get
			{
				foreach (OutlookBarButton b in List)
				{
					if (b.Text.Equals(text))
					{
						return b;
					}
				}

				return null;
			}
		}

		/// <summary />
		internal OutlookBarButton GetItem(int x, int y)
		{
			foreach (OutlookBarButton b in List)
			{
				if (b.Rectangle != Rectangle.Empty && b.Rectangle.Contains(new Point(x, y)))
				{
					return b;
				}
			}

			return null;
		}

		/// <summary />
		public void Add(OutlookBarButton item)
		{
			item.Owner = Owner;
			List.Add(item);
		}

		/// <summary />
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public void AddRange(OutlookBarButtonCollection items)
		{
			foreach (OutlookBarButton item in items)
			{
				Add(item);
			}
		}

		/// <summary />
		public int IndexOf(OutlookBarButton item)
		{
			return List.IndexOf(item);
		}

		/// <summary />
		public void Insert(int index, OutlookBarButton value)
		{
			List.Insert(index, value);
		}

		/// <summary />
		public void Remove(OutlookBarButton value)
		{
			List.Remove(value);
		}

		/// <summary />
		public bool Contains(OutlookBarButton item)
		{
			return List.Contains(item);
		}

		/// <summary />
		protected override void OnValidate(object value)
		{
			if (!(value is OutlookBarButton))
			{
				throw new ArgumentException("value must be of type OutlookBarButton.", nameof(value));
			}
		}

		/// <summary />
		public int VisibleCount => List.Cast<OutlookBarButton>().Count(b => b.Visible & b.Allowed);
	}
}
