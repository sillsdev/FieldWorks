using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;		// controls and etc...
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;

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
		internal static IVwSelection GetSelAtStartOfWs(IVwRootBox rootBox, int flid, int wsIndex, CoreWritingSystemDefinition ws)
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

		internal static int GetCurrentSelectionIndex(SelectionHelper curSel, List<CoreWritingSystemDefinition> writingSystems)
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

		internal static void HandleUpDownArrows(KeyEventArgs e, IVwRootBox rootBox, SelectionHelper curSel, List<CoreWritingSystemDefinition> wsList, int flid)
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
