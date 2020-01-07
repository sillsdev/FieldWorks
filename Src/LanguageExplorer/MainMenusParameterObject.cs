// Copyright (c) 2019-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace LanguageExplorer
{
	internal sealed class MainMenusParameterObject
	{
		internal IReadOnlyDictionary<MainMenu, ToolStripMenuItem> MainMenus { get; }
		internal IReadOnlyDictionary<MainMenu, Tuple<Dictionary<Command, ToolStripItem>, List<ISeparatorMenuBundle>>> SupportedMenuItems { get; }

		internal MainMenusParameterObject(IReadOnlyDictionary<MainMenu, ToolStripMenuItem> mainMenus, IReadOnlyDictionary<MainMenu, Tuple<Dictionary<Command, ToolStripItem>, List<ISeparatorMenuBundle>>> supportedMenuItems)
		{
			MainMenus = mainMenus;
			SupportedMenuItems = supportedMenuItems;
		}
	}
}