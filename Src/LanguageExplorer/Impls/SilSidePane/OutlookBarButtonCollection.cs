// Copyright (C) 2008-2022 SIL International.
// Copyright (C) 2007 Star Vega.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;

namespace LanguageExplorer.Impls.SilSidePane
{
	/// <summary />
	internal sealed class OutlookBarButtonCollection : CollectionBase
	{
		private readonly OutlookBar Owner;

		/// <summary />
		internal OutlookBarButtonCollection(OutlookBar owner)
			: base()
		{
			Owner = owner;
		}

		/// <summary />
		internal OutlookBarButton this[int index] => (OutlookBarButton)List[index];

		/// <summary />
		internal OutlookBarButton this[string text]
		{
			get
			{
				return List.Cast<OutlookBarButton>().FirstOrDefault(b => b.Text.Equals(text));
			}
		}

		/// <summary />
		internal OutlookBarButton GetItem(int x, int y)
		{
			return List.Cast<OutlookBarButton>().FirstOrDefault(b => b.Rectangle != Rectangle.Empty && b.Rectangle.Contains(new Point(x, y)));
		}

		/// <summary />
		internal void Add(OutlookBarButton item)
		{
			item.Owner = Owner;
			List.Add(item);
		}

		/// <summary />
		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		internal void AddRange(OutlookBarButtonCollection items)
		{
			foreach (OutlookBarButton item in items)
			{
				Add(item);
			}
		}

		/// <summary />
		internal int IndexOf(OutlookBarButton item)
		{
			return List.IndexOf(item);
		}

		/// <summary />
		internal void Insert(int index, OutlookBarButton value)
		{
			List.Insert(index, value);
		}

		/// <summary />
		internal void Remove(OutlookBarButton value)
		{
			List.Remove(value);
		}

		/// <summary />
		internal bool Contains(OutlookBarButton item)
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
		internal int VisibleCount => List.Cast<OutlookBarButton>().Count(b => b.Visible & b.Allowed);
	}
}