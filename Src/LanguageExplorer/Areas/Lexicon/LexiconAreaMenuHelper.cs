// Copyright (c) 2017-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using LanguageExplorer.Areas.TextsAndWords.Interlinear;
using LanguageExplorer.Controls;
using LanguageExplorer.LIFT;
using SIL.Code;

namespace LanguageExplorer.Areas.Lexicon
{
	/// <summary>
	/// This class handles all interaction for the Lexicon Area common menus.
	/// </summary>
	internal sealed class LexiconAreaMenuHelper : IAreaUiWidgetManager
	{
		private IArea _area;
		private MajorFlexComponentParameters _majorFlexComponentParameters;
		private ToolStripMenuItem _fileImportMenu;
		private List<Tuple<ToolStripMenuItem, EventHandler>> _newFileMenusAndHandlers = new List<Tuple<ToolStripMenuItem, EventHandler>>();

		internal AreaWideMenuHelper MyAreaWideMenuHelper { get; private set; }

		internal LexiconAreaMenuHelper()
		{
		}

		#region Implementation of IAreaUiWidgetManager
		/// <inheritdoc />
		void IAreaUiWidgetManager.Initialize(MajorFlexComponentParameters majorFlexComponentParameters, IArea area, IToolUiWidgetManager toolUiWidgetManager, IRecordList recordList)
		{
			Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
			Guard.AgainstNull(area, nameof(area));
			Require.That(area.MachineName == AreaServices.LexiconAreaMachineName);

			_area = area;
			_majorFlexComponentParameters = majorFlexComponentParameters;
			_activeToolUiManager = toolUiWidgetManager; // May be null;
			MyAreaWideMenuHelper = new AreaWideMenuHelper(_majorFlexComponentParameters, recordList);
			// Set up File->Export menu, which is visible and enabled in all lexicon area tools, using the default event handler.
			MyAreaWideMenuHelper.SetupFileExportMenu();
			// Add two lexicon area-wide import options.
			AddFileImportMenuItems();
			MyAreaWideMenuHelper.SetupToolsCustomFieldsMenu();
		}

		/// <inheritdoc />
		ITool IAreaUiWidgetManager.ActiveTool => _area.ActiveTool;

		/// <inheritdoc />
		IToolUiWidgetManager IAreaUiWidgetManager.ActiveToolUiManager => _activeToolUiManager;

		/// <inheritdoc />
		void IAreaUiWidgetManager.UnwireSharedEventHandlers()
		{
			// If ActiveToolUiManager is null, then the tool should call this method.
			// Otherwise, ActiveToolUiManager will call it.
		}
		#endregion

		#region IDisposable
		private bool _isDisposed;
		private IToolUiWidgetManager _activeToolUiManager;

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
				MyAreaWideMenuHelper.Dispose();
				foreach (var menuTuple in _newFileMenusAndHandlers)
				{
					menuTuple.Item1.Click -= menuTuple.Item2;
					_fileImportMenu.DropDownItems.Remove(menuTuple.Item1);
					menuTuple.Item1.Dispose();
				}
				_newFileMenusAndHandlers.Clear();
			}
			_majorFlexComponentParameters = null;
			MyAreaWideMenuHelper = null;
			_fileImportMenu = null;
			_newFileMenusAndHandlers = null;

			_isDisposed = true;
		}
		#endregion

		private void AddFileImportMenuItems()
		{
			_fileImportMenu = MenuServices.GetFileImportMenu(_majorFlexComponentParameters.MenuStrip);

			// <item command="CmdImportLinguaLinksData" />
			ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(_newFileMenusAndHandlers, _fileImportMenu, ImportLinguaLinksData_Clicked, LexiconResources.ImportLinguaLinksData, insertIndex: 1);

			// <item command="CmdImportLiftData" />
			ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(_newFileMenusAndHandlers, _fileImportMenu, ImportLiftData_Clicked, LexiconResources.ImportLIFTLexicon, insertIndex: 2);
		}

		private void ImportLinguaLinksData_Clicked(object sender, EventArgs e)
		{
			using (var importWizardDlg = new LinguaLinksImportDlg())
			{
				AreaServices.HandleDlg(importWizardDlg, _majorFlexComponentParameters.LcmCache, _majorFlexComponentParameters.FlexApp, _majorFlexComponentParameters.MainWindow, _majorFlexComponentParameters.FlexComponentParameters.PropertyTable, _majorFlexComponentParameters.FlexComponentParameters.Publisher);
			}
		}

		private void ImportLiftData_Clicked(object sender, EventArgs e)
		{
			using (var importWizardDlg = new LiftImportDlg())
			{
				AreaServices.HandleDlg(importWizardDlg, _majorFlexComponentParameters.LcmCache, _majorFlexComponentParameters.FlexApp, _majorFlexComponentParameters.MainWindow, _majorFlexComponentParameters.FlexComponentParameters.PropertyTable, _majorFlexComponentParameters.FlexComponentParameters.Publisher);
			}
		}
	}
}