// Copyright (c) 2018-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
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
		/// Several T & W area tools don't want these menus, but all other T & W area tools do want them.
		/// </summary>
		internal void AddMenusForExpectedTextAndWordsTools(ToolUiWidgetParameterObject toolUiWidgetParameterObject)
		{
			var toolsThatAreNotExpectedCallThisMethod = new HashSet<string>
			{
				AreaServices.ComplexConcordanceMachineName,
				AreaServices.ConcordanceMachineName,
				AreaServices.InterlinearEditMachineName,
				AreaServices.WordListConcordanceMachineName
			};
			if (toolsThatAreNotExpectedCallThisMethod.Contains(toolUiWidgetParameterObject.Tool.MachineName))
			{
				throw new InvalidOperationException($"'{toolUiWidgetParameterObject.Tool.MachineName}' is not expected to call this method.");
			}
			var fileMenuItemsForTool = toolUiWidgetParameterObject.MenuItemsForTool[MainMenu.File];
			fileMenuItemsForTool.Add(Command.CmdImportInterlinearSfm, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(ImportInterlinearSfm_Click, () => CanCmdImportInterlinearSfm));
			fileMenuItemsForTool.Add(Command.CmdImportWordsAndGlossesSfm, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(ImportWordsAndGlossesSfm_Click, () => CanCmdImportWordsAndGlossesSfm));
			fileMenuItemsForTool.Add(Command.CmdImportInterlinearData, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(ImportInterlinearData_Click, () => CanCmdImportInterlinearData));
		}

		private Tuple<bool, bool> CanCmdImportInterlinearSfm => new Tuple<bool, bool>(true, true);

		private void ImportInterlinearSfm_Click(object sender, EventArgs e)
		{
			using (var importWizardDlg = new InterlinearSfmImportWizard())
			{
				AreaServices.HandleDlg(importWizardDlg, _majorFlexComponentParameters.LcmCache, _majorFlexComponentParameters.FlexApp, _majorFlexComponentParameters.MainWindow, _majorFlexComponentParameters.FlexComponentParameters.PropertyTable, _majorFlexComponentParameters.FlexComponentParameters.Publisher);
			}
		}

		private Tuple<bool, bool> CanCmdImportWordsAndGlossesSfm => new Tuple<bool, bool>(true, true);

		private void ImportWordsAndGlossesSfm_Click(object sender, EventArgs e)
		{
			using (var importWizardDlg = new WordsSfmImportWizard())
			{
				AreaServices.HandleDlg(importWizardDlg, _majorFlexComponentParameters.LcmCache, _majorFlexComponentParameters.FlexApp, _majorFlexComponentParameters.MainWindow, _majorFlexComponentParameters.FlexComponentParameters.PropertyTable, _majorFlexComponentParameters.FlexComponentParameters.Publisher);
			}
		}

		private Tuple<bool, bool> CanCmdImportInterlinearData => new Tuple<bool, bool>(true, true);

		private void ImportInterlinearData_Click(object sender, EventArgs e)
		{
			using (var importWizardDlg = new InterlinearImportDlg())
			{
				AreaServices.HandleDlg(importWizardDlg, _majorFlexComponentParameters.LcmCache, _majorFlexComponentParameters.FlexApp, _majorFlexComponentParameters.MainWindow, _majorFlexComponentParameters.FlexComponentParameters.PropertyTable, _majorFlexComponentParameters.FlexComponentParameters.Publisher);
			}
		}
	}
}