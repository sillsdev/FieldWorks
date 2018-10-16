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
		internal static void AddInsertToolbarItems(MajorFlexComponentParameters majorFlexComponentParameters, List<ToolStripItem> insertStripItems)
		{
			var toolStripInsert = GetInsertToolStrip(majorFlexComponentParameters);
			var currentCount = toolStripInsert.Items.Count;
			foreach (var toolStripItem in insertStripItems)
			{
				toolStripInsert.Items.Insert(currentCount++, toolStripItem);
			}
			toolStripInsert.Visible = insertStripItems.Any(button => button.Enabled);
		}

		internal static void ResetInsertToolbar(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			var toolStripInsert = GetInsertToolStrip(majorFlexComponentParameters);
			var goners = new List<ToolStripItem>();
			foreach (ToolStripItem item in toolStripInsert.Items)
			{
				if (item.Name == ToolbarServices.ToolStripButtonFindText)
				{
					item.Enabled = false;
				}
				else
				{
					goners.Add(item);
				}
			}
			foreach (var goner in goners)
			{
				toolStripInsert.Items.Remove(goner);
			}
			var toolStripInsertIsVisible = false;
			foreach (ToolStripItem item in toolStripInsert.Items)
			{
				if (item.Enabled)
				{
					toolStripInsertIsVisible = true;
					break;
				}
			}
			toolStripInsert.Visible = toolStripInsertIsVisible;
		}

		private static ToolStrip GetInsertToolStrip(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			return (ToolStrip)majorFlexComponentParameters.ToolStripContainer.TopToolStripPanel.Controls.Find(ToolbarServices.ToolStripInsert, false).First();
		}
	}
}
