// Copyright (c) 2018-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using LanguageExplorer.Controls;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ViewsInterfaces;

namespace LanguageExplorer.Areas.Notebook.Tools.NotebookDocument
{
	internal sealed class NotebookDocumentToolMenuHelper : IToolUiWidgetManager
	{
		private MajorFlexComponentParameters _majorFlexComponentParameters;
		private IArea _area;
		private ISharedEventHandlers _sharedEventHandlers;
		private ToolStripMenuItem _editFindMenu;
		private ToolStripMenuItem _toolsConfigureMenu;
		private ToolStripMenuItem _toolsConfigureDocumentMenu;
		private IAreaUiWidgetManager _notebookAreaMenuHelper;
		private XmlDocView _docView;
		private ToolStripButton _insertRecordToolStripButton;
		private ToolStripButton _insertFindRecordToolStripButton;
		private ToolStripItem _insertFindAndReplaceButton;

		internal NotebookDocumentToolMenuHelper(ITool currentNotebookTool, XmlDocView docView)
		{
			Guard.AgainstNull(currentNotebookTool, nameof(currentNotebookTool));
			Guard.AgainstNull(docView, nameof(docView));

			_docView = docView;
			_notebookAreaMenuHelper = new NotebookAreaMenuHelper(currentNotebookTool);
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
			_editFindMenu = MenuServices.GetEditFindMenu(_majorFlexComponentParameters.MenuStrip);
			_editFindMenu.Enabled = _editFindMenu.Visible = true;
			_editFindMenu.Click += EditFindMenu_Click;
			_toolsConfigureMenu = MenuServices.GetToolsConfigureMenu(_majorFlexComponentParameters.MenuStrip);

			AddTool_ConfigureItem();
			((NotebookAreaMenuHelper)_notebookAreaMenuHelper).AddInsertMenuItems();

			// The find/Replace button is always on the toolbar, but not always visible/enabled.
			_insertFindAndReplaceButton = ToolbarServices.GetInsertFindAndReplaceToolStripItem(_majorFlexComponentParameters.ToolStripContainer);
			_insertFindAndReplaceButton.Click += EditFindMenu_Click;
			_insertFindAndReplaceButton.Enabled = _insertFindAndReplaceButton.Visible = true;

			AddInsertToolbarItems();
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

		~NotebookDocumentToolMenuHelper()
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
				_editFindMenu.Click -= EditFindMenu_Click;
				_insertFindAndReplaceButton.Click -= EditFindMenu_Click;
				_insertFindAndReplaceButton.Enabled = _insertFindAndReplaceButton.Visible = false;
				_toolsConfigureMenu.DropDownItems.Remove(_toolsConfigureDocumentMenu);
				_toolsConfigureDocumentMenu.Click -= _docView.ConfigureXmlDocView_Clicked;
				_toolsConfigureDocumentMenu.Dispose();
				_insertRecordToolStripButton?.Dispose();
				_insertFindRecordToolStripButton?.Dispose();
				_notebookAreaMenuHelper?.Dispose();
			}
			_majorFlexComponentParameters = null;
			_editFindMenu = null;
			_toolsConfigureMenu = null;
			_toolsConfigureDocumentMenu = null;
			_notebookAreaMenuHelper = null;
			_insertRecordToolStripButton = null;
			_insertFindRecordToolStripButton = null;
			_sharedEventHandlers = null;
			_docView = null;
			_insertFindAndReplaceButton = null;

			_isDisposed = true;
		}
		#endregion

		private void EditFindMenu_Click(object sender, EventArgs e)
		{
			_majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue<IApp>(LanguageExplorerConstants.App).ShowFindReplaceDialog(false, _majorFlexComponentParameters.MainWindow.ActiveView as IVwRootSite, _majorFlexComponentParameters.LcmCache, _majorFlexComponentParameters.MainWindow as Form);
		}

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
			_insertFindRecordToolStripButton = (ToolStripButton)newToolbarItems[1]; ;

			ToolbarServices.AddInsertToolbarItems(_majorFlexComponentParameters, newToolbarItems);
		}

		private void AddTool_ConfigureItem()
		{
			/*
				<item label="{0}" command="CmdConfigureXmlDocView" defaultVisible="false" />
					<command id="CmdConfigureXmlDocView" label="{0}" message="ConfigureXmlDocView" />
			*/
			_toolsConfigureDocumentMenu = ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(_toolsConfigureMenu, _docView.ConfigureXmlDocView_Clicked, AreaResources.ConfigureDocument, insertIndex: 0);
		}
	}
}