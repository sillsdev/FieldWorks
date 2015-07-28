// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;		// controls and etc...
using System.Windows.Forms.VisualStyles;
using System.Xml;
using Palaso.Media;
using Palaso.WritingSystems;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.Utils;
using System.Text;

namespace SIL.FieldWorks.Common.Widgets
{
	/// <summary>
	/// A start at refactoring two similar, but different classes to use some shared code.
	/// 1. InnerLabeledMultiStringView is a RootSiteControl
	/// 2. InnerLabeledMultiStringControl is a SimpleRootSite
	///    but which is wrapped in a LabeledMultiStringControl (used in InsertEntryDlg), which is a UserControl.
	/// </summary>
	internal static class MultiStringSelectionUtils
	{
		internal static IVwSelection GetSelAtStartOfWs(IVwRootBox rootBox, int flid, int wsIndex, IWritingSystem ws)
		{
			try
			{
				return rootBox.MakeTextSelection(0, 0, null, flid, wsIndex, 0, 0, (ws == null) ? 0 : ws.Handle, false, -1, null, false);
			}
			catch (COMException)
			{
				return null; // can fail if we are hiding an empty WS.
			}
		}

		internal static int GetCurrentSelectionIndex(SelectionHelper curSel, List<IWritingSystem> writingSystems)
		{
			var ws = curSel.SelProps.GetWs();
			int index = -1;
			for (var i = 0; i < writingSystems.Count; i++)
			{
				if (writingSystems[i].Handle == ws)
				{
					index = i;
					break;
				}
			}
			return index;
		}

		internal static void HandleUpDownArrows(KeyEventArgs e, IVwRootBox rootBox, SelectionHelper curSel, List<IWritingSystem> wsList, int flid)
		{
			if (curSel == null || !curSel.IsValid) // LT-13805: sometimes selection was null
				return;
			var index = GetCurrentSelectionIndex(curSel, wsList);
			if (index < 0)
				return;
			var maxWsIndex = wsList.Count - 1;
			if (e.KeyCode == Keys.Up)
			{
				// Handle Up arrow
				if ((index - 1) < 0)
					return;
				index--;
			}
			else
			{
				// Handle Down arrow
				if ((index + 1) > maxWsIndex)
					return;
				index++;
			}
			// make new selection at index
			var newSelection = GetSelAtStartOfWs(rootBox, flid, index, wsList[index]);
			newSelection.Install();
			e.Handled = true;
		}
	}
}
