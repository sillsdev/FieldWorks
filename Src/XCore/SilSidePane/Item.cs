// Copyright (c) 2016 SIL International
// SilOutlookBar is licensed under the MIT license.

using System;
using System.Drawing;

namespace SIL.SilSidePane
{
	/// <summary>
	/// Item of a Tab in a SidePane
	/// </summary>
	public class Item
	{
		/// <summary>
		/// Actual underlying widget associated with this Item instance
		/// </summary>
		internal object UnderlyingWidget { get; set; }

		/// <summary>Internal name of the tab</summary>
		public string Name { get; set; }

		/// <summary>Text that displays on the tab</summary>
		public string Text { get; set; }

		/// <summary></summary>
		public Image Icon { get; set; }

		/// <summary>
		/// A place where clients can store arbitrary data associated with this item.
		/// </summary>
		public object Tag { get; set; }

		/// <summary></summary>
		private Item() {}

		/// <summary></summary>
		public Item(string name)
		{
			if (name == null)
				throw new ArgumentNullException("name");

			Name = name;
			Text = name;
		}

		/// <summary></summary>
		public override string ToString()
		{
			return Name;
		}
	}
}