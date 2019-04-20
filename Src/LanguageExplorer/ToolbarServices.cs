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
		#region Insert toolbar

		internal static void AddInsertToolbarItems(MajorFlexComponentParameters majorFlexComponentParameters, List<ToolStripItem> insertStripItems)
		{
			throw new NotSupportedException("AddInsertToolbarItems");
			//AddInsertToolbarItems(GetInsertToolStrip(majorFlexComponentParameters.ToolStripContainer), insertStripItems);
		}

		#endregion Insert toolbar
	}
}