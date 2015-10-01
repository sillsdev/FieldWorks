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
			var newTempControl = CreateNewLabel(tool);
			mainCollapsingSplitContainer.SecondControl.Controls.Add(newTempControl);
			mainCollapsingSplitContainer.SecondControl.ResumeLayout();
		}

		internal static Label CreateNewLabel(ITool tool)
		{
			var newTempControl = new Label
			{
				Text = GetText(tool),
				Dock = DockStyle.Fill,
				ForeColor = Color.Ivory,
				BackColor = Color.Coral
			};
			return newTempControl;
		}

		internal static Label CreateNewLabel(string labelText)
		{
			var newTempControl = new Label
			{
				Text = labelText,
				Dock = DockStyle.Fill,
				ForeColor = Color.Ivory,
				BackColor = Color.Coral
			};
			return newTempControl;
		}

		private static string GetText(IMajorFlexUiComponent tool)
		{
			return @"Selected Tool: " + tool.UiName;
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