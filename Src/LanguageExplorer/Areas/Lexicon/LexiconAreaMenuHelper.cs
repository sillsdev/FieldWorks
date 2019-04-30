// Copyright (c) 2017-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using LanguageExplorer.Areas.TextsAndWords.Interlinear;
using LanguageExplorer.LIFT;
using SIL.Code;

namespace LanguageExplorer.Areas.Lexicon
{
#if RANDYTODO
	// TODO: Make this a private class of LexiconArea. No tool/control should use it.
#endif
	/// <summary>
	/// This class handles all interaction for the Lexicon Area common menus.
	/// </summary>
	internal sealed class LexiconAreaMenuHelper : IDisposable, IAreaUiWidgetManager
	{
		private IArea _area;
		private ITool _tool;
		private MajorFlexComponentParameters _majorFlexComponentParameters;
		private CustomFieldsMenuHelper _customFieldsMenuHelper;

		internal PartiallySharedForToolsWideMenuHelper MyPartiallySharedForToolsWideMenuHelper { get; private set; }

		internal LexiconAreaMenuHelper(ITool tool)
		{
			_tool = tool;
		}

		#region Implementation of IAreaUiWidgetManager
		/// <inheritdoc />
		void IAreaUiWidgetManager.Initialize(MajorFlexComponentParameters majorFlexComponentParameters, IArea area, IRecordList recordList)
		{
			Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
			Guard.AgainstNull(area, nameof(area));
			Require.That(area.MachineName == AreaServices.LexiconAreaMachineName);

			_area = area;
			_majorFlexComponentParameters = majorFlexComponentParameters;

			MyPartiallySharedForToolsWideMenuHelper = new PartiallySharedForToolsWideMenuHelper(_majorFlexComponentParameters, recordList);
			_customFieldsMenuHelper = new CustomFieldsMenuHelper(_majorFlexComponentParameters, _area);
			var areaUiWidgetParameterObject = new AreaUiWidgetParameterObject(_area);
			_customFieldsMenuHelper.SetupToolsCustomFieldsMenu(areaUiWidgetParameterObject);
			majorFlexComponentParameters.UiWidgetController.AddHandlers(areaUiWidgetParameterObject);
#if RANDYTODO
			// TODO: Are the following area or tool wide?
#endif
			// Set up File->Export menu, which is visible and enabled in all lexicon area tools, using the default event handler.
			var toolUiWidgetParameterObject = new ToolUiWidgetParameterObject(_tool);
			MyPartiallySharedForToolsWideMenuHelper.SetupFileExportMenu(toolUiWidgetParameterObject);
			// Add two lexicon area-wide import options.
			AddFileImportMenuItems(toolUiWidgetParameterObject);
			majorFlexComponentParameters.UiWidgetController.AddHandlers(toolUiWidgetParameterObject);
		}

		/// <inheritdoc />
		ITool IAreaUiWidgetManager.ActiveTool => _area.ActiveTool;

		/// <inheritdoc />
		void IAreaUiWidgetManager.UnwireSharedEventHandlers()
		{
			// If ActiveToolUiManager is null, then the tool should call this method.
			// Otherwise, ActiveToolUiManager will call it.
		}
		#endregion

		#region IDisposable
		private bool _isDisposed;

		~LexiconAreaMenuHelper()
		{
			// The base class finalizer is called automatically.
			Dispose(false);
		}

		/// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SuppressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (_isDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				MyPartiallySharedForToolsWideMenuHelper.Dispose();
			}
			_majorFlexComponentParameters = null;
			MyPartiallySharedForToolsWideMenuHelper = null;

			_isDisposed = true;
		}
		#endregion

		private void AddFileImportMenuItems(ToolUiWidgetParameterObject toolUiWidgetParameterObject)
		{
			var fileMenuItemsForTool = toolUiWidgetParameterObject.MenuItemsForTool[MainMenu.File];
			// <item command="CmdImportLinguaLinksData" />
			fileMenuItemsForTool.Add(Command.CmdImportLinguaLinksData, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(ImportLinguaLinksData_Clicked, () => CanCmdImportLinguaLinksData));
			// <item command="CmdImportLiftData" />
			fileMenuItemsForTool.Add(Command.CmdImportLiftData, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(ImportLiftData_Clicked, () => CanCmdImportLiftData));
		}

		private Tuple<bool, bool> CanCmdImportLinguaLinksData => new Tuple<bool, bool>(true, true);

		private void ImportLinguaLinksData_Clicked(object sender, EventArgs e)
		{
			using (var importWizardDlg = new LinguaLinksImportDlg())
			{
				AreaServices.HandleDlg(importWizardDlg, _majorFlexComponentParameters.LcmCache, _majorFlexComponentParameters.FlexApp, _majorFlexComponentParameters.MainWindow, _majorFlexComponentParameters.FlexComponentParameters.PropertyTable, _majorFlexComponentParameters.FlexComponentParameters.Publisher);
			}
		}

		private Tuple<bool, bool> CanCmdImportLiftData => new Tuple<bool, bool>(true, true);

		private void ImportLiftData_Clicked(object sender, EventArgs e)
		{
			using (var importWizardDlg = new LiftImportDlg())
			{
				AreaServices.HandleDlg(importWizardDlg, _majorFlexComponentParameters.LcmCache, _majorFlexComponentParameters.FlexApp, _majorFlexComponentParameters.MainWindow, _majorFlexComponentParameters.FlexComponentParameters.PropertyTable, _majorFlexComponentParameters.FlexComponentParameters.Publisher);
			}
		}
	}
}