// Copyright (c) 2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Windows.Forms;

namespace LanguageExplorer
{
	internal sealed class MainMenusParameterObject
	{
		internal IReadOnlyDictionary<MainMenu, ToolStripMenuItem> MainMenus { get; }
		internal IReadOnlyDictionary<MainMenu, Dictionary<Command, ToolStripItem>> SupportedMenuItems { get; }

		internal MainMenusParameterObject(IReadOnlyDictionary<MainMenu, ToolStripMenuItem> mainMenus, IReadOnlyDictionary<MainMenu, Dictionary<Command, ToolStripItem>> supportedMenuItems)
		{
			MainMenus = mainMenus;
			SupportedMenuItems = supportedMenuItems;
		}
	}
}