// Copyright (c) 2018-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using LanguageExplorer.Areas.TextsAndWords.Interlinear;

namespace LanguageExplorer.Areas.TextsAndWords.Tools
{
	/// <summary>
	/// This menu helper is shared between these tools: ConcordanceTool, ComplexConcordanceTool, and WordListConcordanceTool.
	/// </summary>
	internal sealed class PartiallySharedTextsAndWordsToolsMenuHelper
	{
		private readonly MajorFlexComponentParameters _majorFlexComponentParameters;

		internal PartiallySharedTextsAndWordsToolsMenuHelper(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			_majorFlexComponentParameters = majorFlexComponentParameters;
		}

		/// <summary>
		/// All T & W area tools, except the "Concordance" tool are to call this.
		/// </summary>
		internal void AddFileMenusForExpectedTextAndWordsTools(ToolUiWidgetParameterObject toolUiWidgetParameterObject)
		{
			if (toolUiWidgetParameterObject.Tool.MachineName == AreaServices.ConcordanceMachineName)
			{
				throw new InvalidOperationException($"'{toolUiWidgetParameterObject.Tool.MachineName}' is not expected to call this method.");
			}
			var fileMenuItemsForTool = toolUiWidgetParameterObject.MenuItemsForTool[MainMenu.File];
			fileMenuItemsForTool.Add(Command.CmdImportInterlinearSfm, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(ImportInterlinearSfm_Click, () => UiWidgetServices.CanSeeAndDo));
			fileMenuItemsForTool.Add(Command.CmdImportWordsAndGlossesSfm, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(ImportWordsAndGlossesSfm_Click, () => UiWidgetServices.CanSeeAndDo));
			fileMenuItemsForTool.Add(Command.CmdImportInterlinearData, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(ImportInterlinearData_Click, () => UiWidgetServices.CanSeeAndDo));
		}

		private void ImportInterlinearSfm_Click(object sender, EventArgs e)
		{
			using (var importWizardDlg = new InterlinearSfmImportWizard())
			{
				AreaServices.HandleDlg(importWizardDlg, _majorFlexComponentParameters.LcmCache, _majorFlexComponentParameters.FlexApp, _majorFlexComponentParameters.MainWindow, _majorFlexComponentParameters.FlexComponentParameters.PropertyTable, _majorFlexComponentParameters.FlexComponentParameters.Publisher);
			}
		}

		private void ImportWordsAndGlossesSfm_Click(object sender, EventArgs e)
		{
			using (var importWizardDlg = new WordsSfmImportWizard())
			{
				AreaServices.HandleDlg(importWizardDlg, _majorFlexComponentParameters.LcmCache, _majorFlexComponentParameters.FlexApp, _majorFlexComponentParameters.MainWindow, _majorFlexComponentParameters.FlexComponentParameters.PropertyTable, _majorFlexComponentParameters.FlexComponentParameters.Publisher);
			}
		}

		private void ImportInterlinearData_Click(object sender, EventArgs e)
		{
			using (var importWizardDlg = new InterlinearImportDlg())
			{
				AreaServices.HandleDlg(importWizardDlg, _majorFlexComponentParameters.LcmCache, _majorFlexComponentParameters.FlexApp, _majorFlexComponentParameters.MainWindow, _majorFlexComponentParameters.FlexComponentParameters.PropertyTable, _majorFlexComponentParameters.FlexComponentParameters.Publisher);
			}
		}
	}
}