// Copyright (c) 2017-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

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

		#region File menu

		internal static ToolStripMenuItem GetFileMenu(MenuStrip menustrip)
		{
			return GetMenu(menustrip, LanguageExplorerConstants.FileToolStripMenuItemName);
		}

		internal static ToolStripMenuItem GetFilePrintMenu(MenuStrip menustrip)
		{
			return GetMenu(GetFileMenu(menustrip).DropDownItems, LanguageExplorerConstants.FilePrintToolStripMenuItemName);
		}

		internal static ToolStripMenuItem GetFileExportMenu(MenuStrip menustrip)
		{
			return GetMenu(GetFileMenu(menustrip).DropDownItems, LanguageExplorerConstants.FileExportToolStripMenuItemName);
		}

		internal static ToolStripMenuItem GetFileImportMenu(MenuStrip menustrip)
		{
			return GetMenu(GetFileMenu(menustrip).DropDownItems, LanguageExplorerConstants.FileImportToolStripMenuItemName);
		}

		#endregion File menu

		#region Edit menu

		internal static ToolStripMenuItem GetEditMenu(MenuStrip menustrip)
		{
			return GetMenu(menustrip, LanguageExplorerConstants.EditToolStripMenuItemName);
		}

		internal static ToolStripMenuItem GetEditDeleteMenu(MenuStrip menustrip)
		{
			return GetMenu(GetEditMenu(menustrip).DropDownItems, LanguageExplorerConstants.EditDeleteToolStripMenuItemName);
		}

		internal static ToolStripMenuItem GetEditFindMenu(MenuStrip menustrip)
		{
			return GetMenu(GetEditMenu(menustrip).DropDownItems, LanguageExplorerConstants.EditFindToolStripMenuItemName);
		}

		internal static ToolStripMenuItem GetEditFindAndReplaceMenu(MenuStrip menustrip)
		{
			return GetMenu(GetEditMenu(menustrip).DropDownItems, LanguageExplorerConstants.EditReplaceToolStripMenuItemName);
		}

		#endregion Edit menu

		#region View menu

		internal static ToolStripMenuItem GetViewMenu(MenuStrip menustrip)
		{
			return GetMenu(menustrip, LanguageExplorerConstants.ViewToolStripMenuItemName);
		}

		internal static ToolStripMenuItem GetViewRefreshMenu(MenuStrip menustrip)
		{
			return GetMenu(GetViewMenu(menustrip).DropDownItems, LanguageExplorerConstants.ViewRefreshToolStripMenuItemName);
		}

		internal static ToolStripMenuItem GetViewFilterMenu(MenuStrip menustrip)
		{
			return GetMenu(GetViewMenu(menustrip).DropDownItems, LanguageExplorerConstants.ViewFiltersToolStripMenuItemName);
		}

		#endregion View menu

		#region Data menu

		internal static ToolStripMenuItem GetDataMenu(MenuStrip menustrip)
		{
			return GetMenu(menustrip, LanguageExplorerConstants.DataToolStripMenuItemName);
		}

		#endregion Data menu

		#region Insert menu

		internal static ToolStripMenuItem GetInsertMenu(MenuStrip menustrip)
		{
			return GetMenu(menustrip, LanguageExplorerConstants.InsertToolStripMenuItemName);
		}

		#endregion Insert menu

		#region Format menu

		internal static ToolStripMenuItem GetFormatMenu(MenuStrip menustrip)
		{
			return GetMenu(menustrip, LanguageExplorerConstants.FormatToolStripMenuItemName);
		}

		#endregion Format menu

		#region Tools menu

		internal static ToolStripMenuItem GetToolsMenu(MenuStrip menustrip)
		{
			return GetMenu(menustrip, LanguageExplorerConstants.ToolsToolStripMenuItemName);
		}

		internal static ToolStripMenuItem GetToolsConfigureMenu(MenuStrip menustrip)
		{
			return GetMenu(GetToolsMenu(menustrip).DropDownItems, LanguageExplorerConstants.ToolsConfigureToolStripMenuItemName);
		}

		internal static ToolStripMenuItem GetToolsSpellingMenu(MenuStrip menustrip)
		{
			return GetMenu(GetToolsMenu(menustrip).DropDownItems, LanguageExplorerConstants.ToolsSpellingToolStripMenuItemName);
		}

		internal static ToolStripMenuItem GetToolsSpellingShowVernacularSpellingErrorsMenu(MenuStrip menustrip)
		{
			return GetMenu(GetToolsSpellingMenu(menustrip).DropDownItems, LanguageExplorerConstants.ToolsShowVernacularSpellingErrorsToolStripMenuItemName);
		}

		#endregion Tools menu

		#region Parser menu

		internal static ToolStripMenuItem GetParserMenu(MenuStrip menustrip)
		{
			return GetMenu(menustrip, LanguageExplorerConstants.ParserToolStripMenuItemName);
		}

		#endregion Parser menu

		#region Help menu

		internal static ToolStripMenuItem GetHelpMenu(MenuStrip menustrip)
		{
			return GetMenu(menustrip, LanguageExplorerConstants.HelpToolStripMenuItemName);
		}

		#endregion Help menu
	}
}