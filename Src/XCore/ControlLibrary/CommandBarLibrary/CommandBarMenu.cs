// ---------------------------------------------------------
// Windows Forms CommandBar Control
// Copyright (C) 2001-2003 Lutz Roeder. All rights reserved.
// http://www.aisto.com/roeder
// roeder@aisto.com
// ---------------------------------------------------------
namespace Reflector.UserInterface
{
	using System;

	public class CommandBarMenu : CommandBarItem
	{
		public event EventHandler DropDown;
		private CommandBarItemCollection items = new CommandBarItemCollection();

		public CommandBarMenu(string text) : base(text)
		{
		}

		protected override void Dispose(bool fDisposing)
		{
			if (fDisposing)
			{
				if (items != null)
				{
					// Disposing the item might remove it from the collection, so we better work on
					// a copy of the collection.
					var copiedItems = new CommandBarItem[items.Count];
					items.CopyTo(copiedItems, 0);
					foreach (var item in copiedItems)
						item.Dispose();

					items.Clear();
				}
			}
			items = null;

			base.Dispose(fDisposing);
		}

		public CommandBarItemCollection Items
		{
			get { return this.items; }
		}

		protected virtual void OnDropDown(EventArgs e)
		{
			if (this.DropDown != null)
			{
				this.DropDown(this, e);
			}
		}

		internal void PerformDropDown(EventArgs e)
		{
			this.OnDropDown(e);
		}
	}
}
