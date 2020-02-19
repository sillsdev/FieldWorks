// Copyright (c) 2019-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary>
	/// This class models a menu item for interacting with the the writing system model.
	/// It holds the string to display in the menu item and the event handler for the menu item click.
	/// </summary>
	public class WSMenuItemModel : Tuple<string, EventHandler, bool>
	{
		/// <summary/>
		public WSMenuItemModel(string menuText, EventHandler clickHandler, bool enabled = true) : base(menuText, clickHandler, enabled)
		{
		}

		/// <summary/>
		public string MenuText => Item1;

		/// <summary/>
		public EventHandler ClickHandler => Item2;

		/// <summary/>
		public bool IsEnabled => Item3;
	}
}