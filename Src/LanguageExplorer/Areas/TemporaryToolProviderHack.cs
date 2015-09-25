// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Drawing;
using System.Windows.Forms;
using SIL.CoreImpl;

namespace LanguageExplorer.Areas
{
#if RANDYTODO
	// TODO: Remove this file, when no tools use TemporaryToolProviderHack.
#endif
	/// <summary>
	/// Temporarily set up and take down a tool.
	/// </summary>
	internal static class TemporaryToolProviderHack
	{
		/// <summary />
		internal static void SetupToolDisplay(ICollapsingSplitContainer mainCollapsingSplitContainer, ITool tool)
		{
			mainCollapsingSplitContainer.SecondControl.SuspendLayout();
			var newTempControl = new Label
			{
				Text = @"Selected Tool: " + tool.UiName,
				Dock = DockStyle.Fill,
				ForeColor = Color.Ivory,
				BackColor = Color.Coral
			};
			mainCollapsingSplitContainer.SecondControl.Controls.Add(newTempControl);
			mainCollapsingSplitContainer.SecondControl.ResumeLayout();
		}

		/// <summary />
		internal static void RemoveToolDisplay(ICollapsingSplitContainer mainCollapsingSplitContainer)
		{
			if (mainCollapsingSplitContainer.SecondControl.Controls.Count == 0)
			{
				return;
			}
			var tempControl = mainCollapsingSplitContainer.SecondControl.Controls[0];
			try
			{
				mainCollapsingSplitContainer.SecondControl.SuspendLayout();
				mainCollapsingSplitContainer.SecondControl.Controls.RemoveAt(0);
			}
			finally
			{
				tempControl.Dispose();
				mainCollapsingSplitContainer.SecondControl.ResumeLayout();
			}
		}
	}
}