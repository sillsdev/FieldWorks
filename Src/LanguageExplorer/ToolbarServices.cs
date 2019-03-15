// Copyright (c) 2017-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace LanguageExplorer
{
#if RANDYTODO
	// TODO: Remove this class when shift in tool bar handling is finished.
#endif
	/// <summary>
	/// Class that helps tools get a toolbar or a button of toolbar.
	/// </summary>
	internal static class ToolbarServices
	{
		#region Standard toolbar

		internal static ToolStripItem GetStandardToolStripRefreshButton(ToolStripContainer toolStripContainer)
		{
			throw new NotSupportedException("GetStandardToolStripRefreshButton");
			//return GetStandardToolStrip(toolStripContainer).Items[LanguageExplorerConstants.ToolStripButton_Refresh];
		}

		#endregion Standard toolbar

		#region View toolbar

		internal static ToolStrip GetViewToolStrip(ToolStripContainer toolStripContainer)
		{
			throw new NotSupportedException("GetViewToolStrip");
			//return GetToolStrip(toolStripContainer, LanguageExplorerConstants.ToolStripView);
		}

		#endregion View toolbar

		#region Insert toolbar

		internal static ToolStripItem GetInsertFindAndReplaceToolStripItem(ToolStripContainer toolStripContainer)
		{
			throw new NotSupportedException("GetInsertFindAndReplaceToolStripItem");
			//return GetInsertToolStrip(toolStripContainer).Items[LanguageExplorerConstants.ToolStripButtonFindText];
		}

		internal static void AddInsertToolbarItems(MajorFlexComponentParameters majorFlexComponentParameters, List<ToolStripItem> insertStripItems)
		{
			throw new NotSupportedException("AddInsertToolbarItems");
			//AddInsertToolbarItems(GetInsertToolStrip(majorFlexComponentParameters.ToolStripContainer), insertStripItems);
		}

		internal static void ResetInsertToolbar(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			throw new NotSupportedException("Don't even 'think' of calling 'ResetInsertToolbar' now.");
			//ResetInsertToolbar(GetInsertToolStrip(majorFlexComponentParameters.ToolStripContainer));
		}

		#endregion Insert toolbar
	}
}