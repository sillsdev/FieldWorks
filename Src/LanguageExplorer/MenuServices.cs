// Copyright (c) 2017-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;

namespace LanguageExplorer
{
#if RANDYTODO
	// TODO: Remove this class when shift in menu handling is finished.
#endif
	/// <summary>
	/// Class that helps tools get a main menu or a sub-menu of a main menu.
	/// </summary>
	internal static class MenuServices
	{
		#region File menu

		internal static ToolStripMenuItem GetFileExportMenu(MenuStrip menustrip)
		{
			throw new NotSupportedException("GetFileExportMenu");
			//return GetMenu(GetFileMenu(menustrip).DropDownItems, Command.CmdExport.ToString("g"));
		}

		internal static ToolStripMenuItem GetFileImportMenu(MenuStrip menustrip)
		{
			throw new NotSupportedException("GetFileImportMenu");
			//return GetMenu(GetFileMenu(menustrip).DropDownItems, Command.ImportMenu.ToString("g"));
		}

		#endregion File menu

		#region Edit menu

		internal static ToolStripMenuItem GetEditMenu(MenuStrip menustrip)
		{
			throw new NotSupportedException("GetEditMenu");
			//return GetMenu(menustrip, LanguageExplorerConstants.EditToolStripMenuItemName);
		}

		internal static ToolStripMenuItem GetEditFindMenu(MenuStrip menustrip)
		{
			throw new NotSupportedException("GetEditFindMenu");
			//return GetMenu(GetEditMenu(menustrip).DropDownItems, LanguageExplorerConstants.EditFindToolStripMenuItemName);
		}

		internal static ToolStripMenuItem GetEditFindAndReplaceMenu(MenuStrip menustrip)
		{
			throw new NotSupportedException("GetEditFindAndReplaceMenu");
			//return GetMenu(GetEditMenu(menustrip).DropDownItems, LanguageExplorerConstants.EditReplaceToolStripMenuItemName);
		}

		#endregion Edit menu

		#region View menu

		internal static ToolStripMenuItem GetViewMenu(MenuStrip menustrip)
		{
			throw new NotSupportedException("GetViewMenu");
			//return GetMenu(menustrip, LanguageExplorerConstants.ViewToolStripMenuItemName);
		}

		internal static ToolStripMenuItem GetViewRefreshMenu(MenuStrip menustrip)
		{
			throw new NotSupportedException("GetViewRefreshMenu");
			//return GetMenu(GetViewMenu(menustrip).DropDownItems, LanguageExplorerConstants.ViewRefreshToolStripMenuItemName);
		}

		#endregion View menu

		#region Insert menu

		internal static ToolStripMenuItem GetInsertMenu(MenuStrip menustrip)
		{
			throw new NotSupportedException("GetInsertMenu");
			//return GetMenu(menustrip, LanguageExplorerConstants.InsertToolStripMenuItemName);
		}

		#endregion Insert menu

		#region Tools menu

		internal static ToolStripMenuItem GetToolsMenu(MenuStrip menustrip)
		{
			throw new NotSupportedException("GetToolsMenu");
			//return GetMenu(menustrip, LanguageExplorerConstants.ToolsToolStripMenuItemName);
		}

		internal static ToolStripMenuItem GetToolsConfigureMenu(MenuStrip menustrip)
		{
			throw new NotSupportedException("GetToolsConfigureMenu");
			//return GetMenu(GetToolsMenu(menustrip).DropDownItems, Command.Configure.ToString("g"));
		}

		#endregion Tools menu
	}
}