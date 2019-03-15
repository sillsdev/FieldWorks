// Copyright (c) 2018-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using SIL.Code;

namespace LanguageExplorer.Areas.Notebook.Tools.NotebookBrowse
{
	/// <summary>
	/// This class handles all interaction for the NotebookBrowseTool for its menus, toolbars, plus all context menus that are used in Slices and PaneBars.
	/// </summary>
	internal sealed class NotebookBrowseToolMenuHelper : IToolUiWidgetManager
	{
		private MajorFlexComponentParameters _majorFlexComponentParameters;
		private IArea _area;
		private ITool _tool;
		private ISharedEventHandlers _sharedEventHandlers;
		private IAreaUiWidgetManager _notebookAreaMenuHelper;
		private PartiallySharedAreaWideMenuHelper _partiallySharedAreaWideMenuHelper;
		private RecordBrowseView _browseView;
		private ToolStripButton _insertRecordToolStripButton;
		private ToolStripButton _insertFindRecordToolStripButton;

		internal NotebookBrowseToolMenuHelper(ITool currentNotebookTool, RecordBrowseView browseView)
		{
			Guard.AgainstNull(currentNotebookTool, nameof(currentNotebookTool));
			Guard.AgainstNull(browseView, nameof(browseView));

			_tool = currentNotebookTool;
			_notebookAreaMenuHelper = new NotebookAreaMenuHelper(currentNotebookTool);
			_browseView = browseView;
		}

		#region Implementation of IToolUiWidgetManager
		/// <inheritdoc />
		void IToolUiWidgetManager.Initialize(MajorFlexComponentParameters majorFlexComponentParameters, IArea area, IRecordList recordList)
		{
			Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
			Guard.AgainstNull(area, nameof(area));

			_majorFlexComponentParameters = majorFlexComponentParameters;
			_area = area;
			_sharedEventHandlers = _majorFlexComponentParameters.SharedEventHandlers;
			_notebookAreaMenuHelper.Initialize(majorFlexComponentParameters, area, recordList);
			_partiallySharedAreaWideMenuHelper = new PartiallySharedAreaWideMenuHelper(_majorFlexComponentParameters, recordList);
			var toolUiWidgetParameterObject = new ToolUiWidgetParameterObject(_tool);
			_partiallySharedAreaWideMenuHelper.SetupToolsCustomFieldsMenu(toolUiWidgetParameterObject);
			var asImplClass = (NotebookAreaMenuHelper)_notebookAreaMenuHelper;
			asImplClass.AddInsertMenuItems();
			AddInsertToolbarItems();
			_majorFlexComponentParameters.UiWidgetController.AddHandlers(toolUiWidgetParameterObject);
		}

		/// <inheritdoc />
		ITool IToolUiWidgetManager.ActiveTool => _area.ActiveTool;

		/// <inheritdoc />
		void IToolUiWidgetManager.UnwireSharedEventHandlers()
		{
			_insertRecordToolStripButton.Click -= _sharedEventHandlers.Get(NotebookAreaMenuHelper.CmdInsertRecord);
			_insertFindRecordToolStripButton.Click -= _sharedEventHandlers.Get(NotebookAreaMenuHelper.CmdGoToRecord);
			_notebookAreaMenuHelper.UnwireSharedEventHandlers();
		}
		#endregion

		#region Implementation of IDisposable
		private bool _isDisposed;

		~NotebookBrowseToolMenuHelper()
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
				_insertRecordToolStripButton?.Dispose();
				_insertFindRecordToolStripButton?.Dispose();
				_notebookAreaMenuHelper?.Dispose();
				_partiallySharedAreaWideMenuHelper?.Dispose();
			}
			_majorFlexComponentParameters = null;
			_notebookAreaMenuHelper = null;
			_partiallySharedAreaWideMenuHelper = null;
			_insertRecordToolStripButton = null;
			_insertFindRecordToolStripButton = null;
			_sharedEventHandlers = null;
			_browseView = null;

			_isDisposed = true;
		}
		#endregion

		private void AddInsertToolbarItems()
		{
			var newToolbarItems = new List<ToolStripItem>(2);

			((NotebookAreaMenuHelper)_notebookAreaMenuHelper).AddCommonInsertToolbarItems(newToolbarItems);
			/*
			  <item command="CmdInsertRecord" defaultVisible="false" />
				Tooltip: <item id="CmdInsertRecord">Create a new Record in your Notebook.</item>
					<command id="CmdInsertRecord" label="Record" message="InsertItemInVector" icon="nbkRecord" shortcut="Ctrl+I">
					  <params className="RnGenericRec" />
					</command>
			*/
			_insertRecordToolStripButton = (ToolStripButton)newToolbarItems[0];

			/*
			  <item command="CmdGoToRecord" defaultVisible="false" />
			*/
			_insertFindRecordToolStripButton = (ToolStripButton)newToolbarItems[1];

			ToolbarServices.AddInsertToolbarItems(_majorFlexComponentParameters, newToolbarItems);
		}
	}
}