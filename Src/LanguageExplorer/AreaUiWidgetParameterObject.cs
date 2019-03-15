// Copyright (c) 2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;

namespace LanguageExplorer
{
	internal sealed class AreaUiWidgetParameterObject
	{
		internal IArea Area { get; }
		internal Dictionary<MainMenu, Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>>> MenuItemsForArea { get; }
		internal Dictionary<ToolBar, Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>>> ToolBarItemsForArea { get; }

		internal AreaUiWidgetParameterObject(IArea area)
		{
			Area = area;
			MenuItemsForArea = new Dictionary<MainMenu, Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>>>();
			ToolBarItemsForArea = new Dictionary<ToolBar, Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>>>();
		}
	}
}