// Copyright (c) 2016 SIL International
// SilOutlookBar is licensed under the MIT license.

using System;
using System.Drawing;

namespace LanguageExplorer.Impls.SilSidePane
{
	/// <summary>
	/// Item of a Tab in a SidePane
	/// </summary>
	internal class Item
	{
		/// <summary>
		/// Actual underlying widget associated with this Item instance
		/// </summary>
		internal object UnderlyingWidget { get; set; }

		/// <summary>Internal name of the tab</summary>
		internal string Name { get; set; }

		/// <summary>Text that displays on the tab</summary>
		internal string Text { get; set; }

		/// <summary />
		internal Image Icon { get; set; }

		/// <summary>
		/// A place where clients can store arbitrary data associated with this item.
		/// </summary>
		internal object Tag { get; set; }

		/// <summary />
		internal Item(string name)
		{
			Name = name ?? throw new ArgumentNullException(nameof(name));
			Text = name;
		}

		/// <summary />
		public override string ToString()
		{
			return Name;
		}
	}
}