// Copyright (c) 2017-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
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
			toolUiWidgetParameterObject.MenuItemsForTool[MainMenu.File].Add(Command.CmdExport, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(FileExportMenu_Click, () => CanCmdExport));
			toolUiWidgetParameterObject.MenuItemsForTool[MainMenu.View].Add(Command.CmdRefresh, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(null, () => CanCmdRefresh));
			toolUiWidgetParameterObject.ToolBarItemsForTool[ToolBar.Standard].Add(Command.CmdRefresh, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(null, () => CanCmdRefresh));
			_majorFlexComponentParameters.UiWidgetController.AddHandlers(toolUiWidgetParameterObject);
		}

		private Tuple<bool, bool> CanCmdRefresh => new Tuple<bool, bool>(true, false);

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
			}
			_majorFlexComponentParameters = null;
			_area = null;
			_tool = null;

			_isDisposed = true;
		}
		#endregion
	}
}