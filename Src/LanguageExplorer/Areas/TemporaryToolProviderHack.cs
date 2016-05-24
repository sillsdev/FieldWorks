// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Drawing;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;

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
			mainCollapsingSplitContainer.SecondControl = CreateNewLabel(tool);
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
			// Re-setting SecondControl, will dispose the Label.
			mainCollapsingSplitContainer.SecondControl = null;
		}
	}
}