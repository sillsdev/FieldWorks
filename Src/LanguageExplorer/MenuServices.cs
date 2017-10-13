// Copyright (c) 2017-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;

namespace LanguageExplorer
{
	/// <summary>
	/// Class that helps tools get a main menu or a sub-menu of a main menu.
	/// </summary>
	internal static class MenuServices
	{
		private static ToolStripMenuItem GetMenu(ToolStripItemCollection dropDownItems, string menuName)
		{
			return (ToolStripMenuItem)dropDownItems[menuName];
		}

		private static ToolStripMenuItem GetMenu(ToolStrip menustrip, string menuName)
		{
			return (ToolStripMenuItem)menustrip.Items[menuName];
		}

		internal static ToolStripMenuItem GetFileMenu(MenuStrip menustrip)
		{
			return GetMenu(menustrip, LanguageExplorerConstants.FileToolStripMenuItem);
		}

		internal static ToolStripMenuItem GetFilePrintMenu(MenuStrip menustrip)
		{
			return GetMenu(GetMenu(menustrip, LanguageExplorerConstants.FileToolStripMenuItem).DropDownItems, "printToolStripMenuItem");
		}

		internal static ToolStripMenuItem GetEditMenu(MenuStrip menustrip)
		{
			return GetMenu(menustrip, LanguageExplorerConstants.EditToolStripMenuItem);
		}

		internal static ToolStripMenuItem GetViewMenu(MenuStrip menustrip)
		{
			return GetMenu(menustrip, LanguageExplorerConstants.ViewToolStripMenuItem);
		}

		internal static ToolStripMenuItem GetDataMenu(MenuStrip menustrip)
		{
			return GetMenu(menustrip, LanguageExplorerConstants.DataToolStripMenuItem);
		}

		internal static ToolStripMenuItem GetInsertMenu(MenuStrip menustrip)
		{
			return GetMenu(menustrip, LanguageExplorerConstants.InsertToolStripMenuItem);
		}

		internal static ToolStripMenuItem GetFormatMenu(MenuStrip menustrip)
		{
			return GetMenu(menustrip, LanguageExplorerConstants.FormatToolStripMenuItem);
		}

		internal static ToolStripMenuItem GetToolsMenu(MenuStrip menustrip)
		{
			return GetMenu(menustrip, LanguageExplorerConstants.ToolsToolStripMenuItem);
		}

		internal static ToolStripMenuItem GetHelpMenu(MenuStrip menustrip)
		{
			return GetMenu(menustrip, LanguageExplorerConstants.HelpToolStripMenuItem);
		}
	}
}