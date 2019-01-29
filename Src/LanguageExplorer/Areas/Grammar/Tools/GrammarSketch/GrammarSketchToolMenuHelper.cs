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
		private IAreaUiWidgetManager _grammarAreaWideMenuHelper;
		private bool _refreshOriginalValue;
		private ToolStripItem _refreshMenu;
		private ToolStripItem _refreshToolBarBtn;

		internal GrammarSketchToolMenuHelper()
		{
		}

		void FileExportMenu_Click(object sender, EventArgs e)
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
			_grammarAreaWideMenuHelper = new GrammarAreaMenuHelper(FileExportMenu_Click);
			_grammarAreaWideMenuHelper.Initialize(majorFlexComponentParameters, area, this, recordList);
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
			_grammarAreaWideMenuHelper.UnwireSharedEventHandlers();
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
				_grammarAreaWideMenuHelper.Dispose();
			}
			_majorFlexComponentParameters = null;
			_grammarAreaWideMenuHelper = null;
			_refreshMenu = null;
			_refreshToolBarBtn = null;

			_isDisposed = true;
		}
		#endregion
	}
}