// Copyright (c) 2017-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Windows.Forms;
using LanguageExplorer.Controls;
using SIL.Code;

namespace LanguageExplorer.Areas.Lists
{
	internal sealed class ListsAreaToolsMenuManager : IPartialToolUiWidgetManager
	{
		private MajorFlexComponentParameters _majorFlexComponentParameters;
		private IRecordList MyRecordList { get; set; }
		private IListArea _area;
		private ToolStripMenuItem _toolConfigureMenu;
		private ToolStripMenuItem _configureListMenu;

		internal ListsAreaToolsMenuManager()
		{
		}

		#region Implementation of IPartialToolUiWidgetManager

		/// <inheritdoc />
		void IPartialToolUiWidgetManager.Initialize(MajorFlexComponentParameters majorFlexComponentParameters, IToolUiWidgetManager toolUiWidgetManager, IRecordList recordList)
		{
			Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
			Guard.AgainstNull(toolUiWidgetManager, nameof(toolUiWidgetManager));
			Guard.AgainstNull(recordList, nameof(recordList));

			_majorFlexComponentParameters = majorFlexComponentParameters;
			_area = (IListArea)toolUiWidgetManager.ActiveTool.Area;
			MyRecordList = recordList;

			/*
				Tools->Configure: <command id = "CmdConfigureList" label="List..." message="ConfigureList" />
			*/
			_toolConfigureMenu = MenuServices.GetToolsConfigureMenu(_majorFlexComponentParameters.MenuStrip);
			_configureListMenu = ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(_toolConfigureMenu, ConfigureList_Click, ListResources.ConfigureList, ListResources.ConfigureListTooltip, insertIndex: 0);
		}

		/// <inheritdoc />
		void IPartialToolUiWidgetManager.UnwireSharedEventHandlers()
		{
		}

		#endregion

		#region Implementation of IDisposable

		private bool _isDisposed;

		~ListsAreaToolsMenuManager()
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
				_toolConfigureMenu?.DropDownItems.Remove(_configureListMenu);
				if (_configureListMenu != null)
				{
					_configureListMenu.Click -= ConfigureList_Click;
					_configureListMenu.Dispose();
				}
			}
			_majorFlexComponentParameters = null;
			_area = null;
			MyRecordList = null;
			_toolConfigureMenu = null;
			_configureListMenu = null;

			_isDisposed = true;
		}

		#endregion

		private void ConfigureList_Click(object sender, EventArgs e)
		{
			var list = ListsAreaMenuHelper.GetPossibilityList(MyRecordList);
			var originalUiName = list.Name.BestAnalysisAlternative.Text;
			using (var dlg = new ConfigureListDlg(_majorFlexComponentParameters.FlexComponentParameters.PropertyTable, _majorFlexComponentParameters.FlexComponentParameters.Publisher, _majorFlexComponentParameters.LcmCache, list))
			{
				if (dlg.ShowDialog((Form)_majorFlexComponentParameters.MainWindow) == DialogResult.OK && originalUiName != list.Name.BestAnalysisAlternative.Text)
				{
					_area.ModifiedCustomList(_area.ActiveTool);
				}
			}
		}
	}
}