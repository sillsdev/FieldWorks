// Copyright (c) 2017-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorer.Areas.Grammar.Tools.GrammarSketch
{
	/// <summary>
	/// Handle creation and use of the grammar sketch tool menus.
	/// </summary>
	internal sealed class GrammarSketchToolMenuHelper : IToolUiWidgetManager
	{
		private MajorFlexComponentParameters _majorFlexComponentParameters;
		private IArea _area;
		private ITool _tool;
		private bool _refreshOriginalValue;
		private ToolStripItem _refreshMenu;
		private ToolStripItem _refreshToolBarBtn;

		internal GrammarSketchToolMenuHelper(ITool tool)
		{
			_tool = tool;
		}

		private Tuple<bool, bool> CanCmdExport => new Tuple<bool, bool>(true, true);

		private void FileExportMenu_Click(object sender, EventArgs e)
		{
			using (var dlg = new ExportDialog(_majorFlexComponentParameters.StatusBar))
			{
				dlg.InitializeFlexComponent(_majorFlexComponentParameters.FlexComponentParameters);
				dlg.ShowDialog(_majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue<Form>(FwUtils.window));
			}
		}

		#region Implementation of IToolUiWidgetManager
		/// <inheritdoc />
		void IToolUiWidgetManager.Initialize(MajorFlexComponentParameters majorFlexComponentParameters, IArea area, IRecordList recordList)
		{
			Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
			Guard.AgainstNull(area, nameof(area));

			_majorFlexComponentParameters = majorFlexComponentParameters;
			_area = area;
			var toolUiWidgetParameterObject = new ToolUiWidgetParameterObject(_tool);
			Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>> toolsMenuItemsForTool;
			if (!toolUiWidgetParameterObject.MenuItemsForTool.TryGetValue(MainMenu.File, out toolsMenuItemsForTool))
			{
				toolsMenuItemsForTool = new Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>>();
				toolUiWidgetParameterObject.MenuItemsForTool.Add(MainMenu.File, toolsMenuItemsForTool);
			}
			toolsMenuItemsForTool.Add(Command.CmdExport, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(FileExportMenu_Click, () => CanCmdExport));
			_majorFlexComponentParameters.UiWidgetController.AddHandlers(toolUiWidgetParameterObject);
			// F5 refresh is disabled in this tool.
			_refreshMenu = MenuServices.GetViewRefreshMenu(_majorFlexComponentParameters.MenuStrip);
			_refreshOriginalValue = _refreshMenu.Enabled;
			_refreshMenu.Enabled = false;
			_refreshToolBarBtn = ToolbarServices.GetStandardToolStripRefreshButton(_majorFlexComponentParameters.ToolStripContainer);
			_refreshToolBarBtn.Enabled = false;
		}

		/// <inheritdoc />
		ITool IToolUiWidgetManager.ActiveTool => _area.ActiveTool;

		/// <inheritdoc />
		void IToolUiWidgetManager.UnwireSharedEventHandlers()
		{
		}
		#endregion

		#region IDisposable
		private bool _isDisposed;

		~GrammarSketchToolMenuHelper()
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
				return; // No need to do it more than once.
			}

			if (disposing)
			{
				_refreshMenu.Enabled = _refreshOriginalValue;
				_refreshToolBarBtn.Enabled = _refreshOriginalValue;
			}
			_majorFlexComponentParameters = null;
			_refreshMenu = null;
			_refreshToolBarBtn = null;
			_area = null;
			_tool = null;

			_isDisposed = true;
		}
		#endregion
	}
}