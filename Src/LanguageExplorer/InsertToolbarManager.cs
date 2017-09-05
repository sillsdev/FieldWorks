// Copyright (c) 2017-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace LanguageExplorer
{
	internal static class InsertToolbarManager
	{
		internal static void AddInsertToolbarItems(MajorFlexComponentParameters majorFlexComponentParameters, List<ToolStripButton> insertStripButtons)
		{
			var toolStripInsert = GetInsertToolStrip(majorFlexComponentParameters);
			toolStripInsert.Items.AddRange(insertStripButtons.ToArray());
			toolStripInsert.Visible = insertStripButtons.Any();
		}

		internal static void DeactivateInsertToolbar(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			var toolStripInsert = GetInsertToolStrip(majorFlexComponentParameters);
			toolStripInsert.Items.Clear();
			toolStripInsert.Visible = false;
		}

		private static ToolStrip GetInsertToolStrip(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			return (ToolStrip)majorFlexComponentParameters.ToolStripContainer.TopToolStripPanel.Controls.Find("toolStripInsert", false).First();
		}
	}
}
