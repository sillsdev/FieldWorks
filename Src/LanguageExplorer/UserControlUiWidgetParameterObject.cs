// Copyright (c) 2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace LanguageExplorer
{
	internal sealed class UserControlUiWidgetParameterObject
	{
		internal UserControl UserControl { get; }
		internal Dictionary<MainMenu, Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>>> MenuItemsForUserControl { get; }
		internal Dictionary<ToolBar, Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>>> ToolBarItemsForUserControl { get; }

		internal UserControlUiWidgetParameterObject(UserControl userControl)
		{
			UserControl = userControl;
			MenuItemsForUserControl = UiWidgetServices.PopulateForMenus;
			ToolBarItemsForUserControl = UiWidgetServices.PopulateForToolBars;
		}
	}
}