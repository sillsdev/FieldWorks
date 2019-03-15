// Copyright (c) 2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;

namespace LanguageExplorer
{
	internal sealed class ToolUiWidgetParameterObject
	{
		internal ITool Tool { get; }
		internal Dictionary<MainMenu, Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>>> MenuItemsForTool { get; }
		internal Dictionary<ToolBar, Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>>> ToolBarItemsForTool { get; }

		internal ToolUiWidgetParameterObject(ITool tool)
		{
			Tool = tool;
			MenuItemsForTool = new Dictionary<MainMenu, Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>>>();
			ToolBarItemsForTool = new Dictionary<ToolBar, Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>>>();
		}
	}
}