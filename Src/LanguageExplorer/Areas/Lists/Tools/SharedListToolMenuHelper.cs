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
		private IRecordList _recordList;
		private ITool _tool;
		private readonly ICmPossibilityList _possibilityList;
		private PartiallySharedForToolsWideMenuHelper _partiallySharedForToolsWideMenuHelper;
		private IListArea Area => (IListArea)_tool.Area;

		internal SharedListToolMenuHelper(MajorFlexComponentParameters majorFlexComponentParameters, PartiallySharedForToolsWideMenuHelper partiallySharedForToolsWideMenuHelper, ITool tool, ICmPossibilityList possibilityList, IRecordList recordList)
		{
			Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
			Guard.AgainstNull(partiallySharedForToolsWideMenuHelper, nameof(partiallySharedForToolsWideMenuHelper));
			Guard.AgainstNull(tool, nameof(tool));
			Guard.AgainstNull(possibilityList, nameof(possibilityList));
			Guard.AgainstNull(recordList, nameof(recordList));

			_majorFlexComponentParameters = majorFlexComponentParameters;
			_partiallySharedForToolsWideMenuHelper = partiallySharedForToolsWideMenuHelper;
			_tool = tool;
			_possibilityList = possibilityList;
			_recordList = recordList;
		}

		internal void SetupToolUiWidgets(ToolUiWidgetParameterObject toolUiWidgetParameterObject)
		{
			// Set up File->Export menu, which is visible and enabled in all list area tools, using the default event handler.
			_partiallySharedForToolsWideMenuHelper.SetupFileExportMenu(toolUiWidgetParameterObject);
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
					Area.ModifiedListDisplayName(_tool);
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
				_partiallySharedForToolsWideMenuHelper.Dispose();
			}
			_majorFlexComponentParameters = null;
			_recordList = null;
			_tool = null;

			_isDisposed = true;
		}
		#endregion
	}
}
