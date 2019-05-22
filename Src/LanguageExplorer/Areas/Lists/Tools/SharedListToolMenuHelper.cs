// Copyright (c) 2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Windows.Forms;
using SIL.Code;
using SIL.LCModel;

namespace LanguageExplorer.Areas.Lists.Tools
{
	internal sealed class SharedListToolMenuHelper : IDisposable
	{
		private MajorFlexComponentParameters _majorFlexComponentParameters;
		private ITool _tool;
		private readonly ICmPossibilityList _possibilityList;
		private FileExportMenuHelper _fileExportMenuHelper;
		private IListArea Area => (IListArea)_tool.Area;

		internal SharedListToolMenuHelper(MajorFlexComponentParameters majorFlexComponentParameters, FileExportMenuHelper fileExportMenuHelper, ITool tool, ICmPossibilityList possibilityList)
		{
			Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
			Guard.AgainstNull(fileExportMenuHelper, nameof(fileExportMenuHelper));
			Guard.AgainstNull(tool, nameof(tool));
			Guard.AgainstNull(possibilityList, nameof(possibilityList));

			_majorFlexComponentParameters = majorFlexComponentParameters;
			_fileExportMenuHelper = fileExportMenuHelper;
			_tool = tool;
			_possibilityList = possibilityList;
		}

		internal void SetupToolUiWidgets(ToolUiWidgetParameterObject toolUiWidgetParameterObject)
		{
			// Set up File->Export menu, which is visible and enabled in all list area tools, using the default event handler.
			_fileExportMenuHelper.SetupFileExportMenu(toolUiWidgetParameterObject);
			// <command id = "CmdConfigureList" label="List..." message="ConfigureList" />
			toolUiWidgetParameterObject.MenuItemsForTool[MainMenu.Tools].Add(Command.CmdConfigureList, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(ConfigureList_Click, ()=> CanCmdConfigureList));
		}

		private static Tuple<bool, bool> CanCmdConfigureList => new Tuple<bool, bool>(true, true);

		private void ConfigureList_Click(object sender, EventArgs e)
		{
			var originalUiName = _possibilityList.Name.BestAnalysisAlternative.Text;
			using (var dlg = new ConfigureListDlg(_majorFlexComponentParameters.FlexComponentParameters.PropertyTable, _majorFlexComponentParameters.FlexComponentParameters.Publisher, _majorFlexComponentParameters.LcmCache, _possibilityList))
			{
				if (dlg.ShowDialog((Form)_majorFlexComponentParameters.MainWindow) == DialogResult.OK && originalUiName != _possibilityList.Name.BestAnalysisAlternative.Text)
				{
					Area.UpdateListDisplayName(_tool);
				}
			}
		}

		#region Implementation of IDisposable
		private bool _isDisposed;

		~SharedListToolMenuHelper()
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
				_fileExportMenuHelper.Dispose();
			}
			_majorFlexComponentParameters = null;
			_tool = null;
			_fileExportMenuHelper = null;

			_isDisposed = true;
		}
		#endregion
	}
}
