// Copyright (c) 2017-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.DetailControls;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorer.Areas.Notebook
{
#if RANDYTODO
	// TODO: Make this a private class of NotebookArea. No tool/control should use it.
#endif
	/// <summary>
	/// Handle creation and use of Notebook area menus.
	/// </summary>
	internal sealed class NotebookAreaMenuHelper : IAreaUiWidgetManager
	{
		internal const string CmdGoToRecord = "CmdGoToRecord";
		internal const string CmdInsertRecord = "CmdInsertRecord";
		internal const string CmdInsertSubRecord = "CmdInsertSubRecord";
		internal const string CmdInsertSubSubRecord = "CmdInsertSubSubRecord";
		private IArea _area;
		private MajorFlexComponentParameters _majorFlexComponentParameters;
		private DataTree MyDataTree { get; set; }
		private ISharedEventHandlers _sharedEventHandlers;
		private ITool _currentNotebookTool;
		private IRecordList _recordList;
		private ToolStripMenuItem _editMenu;
		private List<Tuple<ToolStripMenuItem, EventHandler>> _newEditMenusAndHandlers;
		private ToolStripMenuItem _fileImportMenu;
		private List<Tuple<ToolStripMenuItem, EventHandler>> _newFileMenusAndHandlers = new List<Tuple<ToolStripMenuItem, EventHandler>>();
		private ToolStripMenuItem _insertMenu;
		private List<Tuple<ToolStripMenuItem, EventHandler>> _newInsertMenusAndHandlers;
		private ToolStripMenuItem _insertSubsubrecordMenuItem;
		private ToolStripMenuItem _insertEntryMenu;

		internal NotebookAreaMenuHelper(ITool currentNotebookTool, DataTree dataTree = null)
		{
			Guard.AgainstNull(currentNotebookTool, nameof(currentNotebookTool));

			_currentNotebookTool = currentNotebookTool;
			MyDataTree = dataTree; // May be null.
		}

		#region Implementation of IAreaUiWidgetManager
		/// <inheritdoc />
		void IAreaUiWidgetManager.Initialize(MajorFlexComponentParameters majorFlexComponentParameters, IArea area, IRecordList recordList)
		{
			Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
			Guard.AgainstNull(area, nameof(area));
			Require.That(area.MachineName == AreaServices.NotebookAreaMachineName);

			_majorFlexComponentParameters = majorFlexComponentParameters;
			_sharedEventHandlers = _majorFlexComponentParameters.SharedEventHandlers;
			_sharedEventHandlers.Add(CmdGoToRecord, GotoRecord_Clicked);
			_sharedEventHandlers.Add(CmdInsertRecord, Insert_Record_Clicked);
			_sharedEventHandlers.Add(CmdInsertSubRecord, Insert_Subrecord_Clicked);
			_sharedEventHandlers.Add(CmdInsertSubSubRecord, Insert_Subsubrecord_Clicked);
			_area = area;
			_recordList = recordList;

			var areaUiWidgetParameterObject = new AreaUiWidgetParameterObject(_area);

			// Add Edit menu item that is available in all Notebook tools.
			Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>> toolsMenuItemsForArea;
			if (!areaUiWidgetParameterObject.MenuItemsForArea.TryGetValue(MainMenu.Edit, out toolsMenuItemsForArea))
			{
				toolsMenuItemsForArea = new Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>>();
				areaUiWidgetParameterObject.MenuItemsForArea.Add(MainMenu.Edit, toolsMenuItemsForArea);
			}
			toolsMenuItemsForArea.Add(Command.CmdGoToRecord, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(GotoRecord_Clicked, () => CanCmdGoToRecord));

			// File->Export menu is visible and maybe enabled in this tool.
			if (!areaUiWidgetParameterObject.MenuItemsForArea.TryGetValue(MainMenu.File, out toolsMenuItemsForArea))
			{
				toolsMenuItemsForArea = new Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>>();
				areaUiWidgetParameterObject.MenuItemsForArea.Add(MainMenu.File, toolsMenuItemsForArea);
			}
			toolsMenuItemsForArea.Add(Command.CmdExport, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(FileExportMenu_Click, () => CanCmdExport));
			toolsMenuItemsForArea.Add(Command.CmdImportSFMNotebook, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(ImportSFMNotebook_Clicked, () => CanCmdImportSFMNotebook));
			_majorFlexComponentParameters.UiWidgetController.AddHandlers(areaUiWidgetParameterObject);
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

		~NotebookAreaMenuHelper()
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
				if (MyDataTree != null)
				{
					MyDataTree.CurrentSliceChanged -= MyDataTreeOnCurrentSliceChanged;
				}
				_insertMenu.DropDownOpening -= InsertMenu_DropDownOpening;
				foreach (var menuTuple in _newFileMenusAndHandlers)
				{
					menuTuple.Item1.Click -= menuTuple.Item2;
					_fileImportMenu.DropDownItems.Remove(menuTuple.Item1);
					menuTuple.Item1.Dispose();
				}
				_newFileMenusAndHandlers.Clear();

				foreach (var menuTuple in _newEditMenusAndHandlers)
				{
					menuTuple.Item1.Click -= menuTuple.Item2;
					_editMenu.DropDownItems.Remove(menuTuple.Item1);
					menuTuple.Item1.Dispose();
				}
				_newEditMenusAndHandlers.Clear();

				foreach (var menuTuple in _newInsertMenusAndHandlers)
				{
					menuTuple.Item1.Click -= menuTuple.Item2;
					_insertMenu.DropDownItems.Remove(menuTuple.Item1);
					menuTuple.Item1.Dispose();
				}
				_newInsertMenusAndHandlers?.Clear();

				_sharedEventHandlers.Remove(CmdGoToRecord);
				_sharedEventHandlers.Remove(CmdInsertRecord);
				_sharedEventHandlers.Remove(CmdInsertSubRecord);
				_sharedEventHandlers.Remove(CmdInsertSubSubRecord);
			}
			_majorFlexComponentParameters = null;
			MyDataTree = null;
			_sharedEventHandlers = null;
			_currentNotebookTool = null;
			_recordList = null;
			_fileImportMenu = null;
			_newFileMenusAndHandlers = null;
			_editMenu = null;
			_newEditMenusAndHandlers = null;
			_insertMenu = null;
			_newInsertMenusAndHandlers = null;

			_isDisposed = true;
		}
		#endregion

		internal void AddCommonInsertToolbarItems(List<ToolStripItem> newToolbarItems)
		{
			/*
			  <item command="CmdInsertRecord" defaultVisible="false" /> // Shared locally
				Tooltip: <item id="CmdInsertRecord">Create a new Record in your Notebook.</item>
					<command id="CmdInsertRecord" label="Record" message="InsertItemInVector" icon="nbkRecord" shortcut="Ctrl+I">
					  <params className="RnGenericRec" />
					</command>
			*/
			newToolbarItems.Add(ToolStripButtonFactory.CreateToolStripButton(_sharedEventHandlers.Get(CmdInsertRecord), "toolStripButtonInsertRecord", NotebookResources.nbkRecord, $"{NotebookResources.Create_a_new_Record_in_your_Notebook} (CTRL+I)"));

			/*
			  <item command="CmdGoToRecord" defaultVisible="false" /> // Shared from afar
			*/
#if RANDYTODO
			// TODO: NotebookResources.Find_a_Record_in_your_Notebook has been removed.
			newToolbarItems.Add(ToolStripButtonFactory.CreateToolStripButton(_sharedEventHandlers.Get(CmdGoToRecord), "toolStripButtonInsertFindRecord", NotebookResources.goToRecord, $"{NotebookResources.Find_a_Record_in_your_Notebook} (CTRL+F)"));
#endif
		}

		internal void AddInsertMenuItems()
		{
			_insertMenu = MenuServices.GetInsertMenu(_majorFlexComponentParameters.MenuStrip);
			_newInsertMenusAndHandlers = new List<Tuple<ToolStripMenuItem, EventHandler>>(3);

			var insertIndex = 0;
			/*
			  <item command="CmdInsertRecord" defaultVisible="false" /> // Shared locally
				Tooltip: <item id="CmdInsertRecord">Create a new Record in your Notebook.</item>
					<command id="CmdInsertRecord" label="Record" message="InsertItemInVector" icon="nbkRecord" shortcut="Ctrl+I">
					  <params className="RnGenericRec" />
					</command>
			*/
			ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(_newInsertMenusAndHandlers, _insertMenu, Insert_Record_Clicked, NotebookResources.Record, NotebookResources.Create_a_new_Record_in_your_Notebook, Keys.Control | Keys.I, NotebookResources.nbkRecord, insertIndex++);

			/*
			  <item command="CmdInsertSubrecord" defaultVisible="false" />
				Tooltip: <item id="CmdInsertSubrecord">Create a Subrecord in your Notebook.</item>
					<command id="CmdInsertSubrecord" label="Subrecord" message="InsertItemInVector" icon="nbkRecord">
					  <params className="RnGenericRec" subrecord="true" />
					</command>
			*/
			ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(_newInsertMenusAndHandlers, _insertMenu, Insert_Subrecord_Clicked, NotebookResources.Subrecord, NotebookResources.Create_a_Subrecord_in_your_Notebook, image: NotebookResources.nbkRecord, insertIndex: insertIndex++);

			/*
			  <item command="CmdInsertSubsubrecord" defaultVisible="false" />
					<command id="CmdInsertSubsubrecord" label="Subrecord of subrecord" message="InsertItemInVector" icon="nbkRecord">
					  <params className="RnGenericRec" subrecord="true" subsubrecord="true" />
					</command>
			*/
			_insertSubsubrecordMenuItem = ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(_newInsertMenusAndHandlers, _insertMenu, Insert_Subsubrecord_Clicked, NotebookResources.Subrecord_of_subrecord, image: NotebookResources.nbkRecord, insertIndex: insertIndex++);

			if (MyDataTree != null)
			{
				MyDataTree.CurrentSliceChanged += MyDataTreeOnCurrentSliceChanged;
			}
			_insertMenu.DropDownOpening += InsertMenu_DropDownOpening;
		}

		private void InsertMenu_DropDownOpening(object sender, EventArgs e)
		{
			// Set visibility/Enable for _insertSubsubrecordMenuItem.
			_insertSubsubrecordMenuItem.Visible = _recordList.CurrentObject != null && _recordList.CurrentObject.OwningFlid == RnGenericRecTags.kflidSubRecords;

			var currentSliceAsStTextSlice = PartiallySharedAreaWideMenuHelper.DataTreeCurrentSliceAsStTextSlice(MyDataTree);
			if (_insertEntryMenu != null && _insertEntryMenu.Visible && currentSliceAsStTextSlice != null)
			{
				PartiallySharedAreaWideMenuHelper.Set_CmdAddToLexicon_State(_majorFlexComponentParameters.LcmCache, _insertEntryMenu, currentSliceAsStTextSlice.RootSite.RootBox.Selection);
			}
		}

		private void MyDataTreeOnCurrentSliceChanged(object sender, CurrentSliceChangedEventArgs e)
		{
			if (_insertEntryMenu != null)
			{
				_insertEntryMenu.Enabled = PartiallySharedAreaWideMenuHelper.DataTreeCurrentSliceAsStTextSlice(MyDataTree) != null;
			}
		}

		private void Insert_Record_Clicked(object sender, EventArgs e)
		{
			InsertRecord_Common();
		}

		private void Insert_Subrecord_Clicked(object sender, EventArgs e)
		{
			/*
				<command id="CmdDataTree-Insert-Subrecord" label="Insert _Subrecord" message="InsertItemInVector">
					<parameters className="RnGenericRec" subrecord="true"/>
				</command>
			*/
			InsertRecord_Common(false);
		}

		private void Insert_Subsubrecord_Clicked(object sender, EventArgs e)
		{
			/*
				<command id="CmdDataTree-Insert-Subsubrecord" label="Insert S_ubrecord of Subrecord" message="InsertItemInVector">
					<parameters className="RnGenericRec" subrecord="true" subsubrecord="true"/>
				</command>
			*/
			InsertRecord_Common(false);
		}

		private void InsertRecord_Common(bool insertMainRecord = true)
		{
			using (var dlg = new InsertRecordDlg())
			{
				dlg.InitializeFlexComponent(_majorFlexComponentParameters.FlexComponentParameters);
				var cache = _majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue<LcmCache>(LanguageExplorerConstants.cache);
				ICmObject owner;
				var currentRecord = _recordList.CurrentObject;
				var researchNbk = (IRnResearchNbk)_recordList.OwningObject;
				if (currentRecord == null || insertMainRecord)
				{
					// Notebook is the owner.
					owner = researchNbk;
				}
				else
				{
                    // It could be a sub-record, or a sub-sub-record.
					owner = currentRecord;
				}
				dlg.SetDlgInfo(cache, owner);
				if (dlg.ShowDialog(Form.ActiveForm) == DialogResult.OK)
				{
					LinkHandler.PublishFollowLinkMessage(_majorFlexComponentParameters.FlexComponentParameters.Publisher, new FwLinkArgs(_currentNotebookTool.MachineName, dlg.NewRecord.Guid));
				}
			}
		}

		private Tuple<bool, bool> CanCmdGoToRecord => new Tuple<bool, bool>(true, true);

		private void GotoRecord_Clicked(object sender, EventArgs e)
		{
			/*
			    <command id="CmdGoToRecord" label="_Find Record..." message="GotoRecord" icon="goToRecord" shortcut="Ctrl+F" >
			      <parameters title="Go To Record" formlabel="Go _To..." okbuttonlabel="_Go" />
			    </command>
			*/
			using (var dlg = new RecordGoDlg())
			{
				dlg.InitializeFlexComponent(_majorFlexComponentParameters.FlexComponentParameters);
				var cache = _majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue<LcmCache>(LanguageExplorerConstants.cache);
				dlg.SetDlgInfo(cache, null);
				if (dlg.ShowDialog() == DialogResult.OK)
				{
					LinkHandler.PublishFollowLinkMessage(_majorFlexComponentParameters.FlexComponentParameters.Publisher, new FwLinkArgs(_currentNotebookTool.MachineName, dlg.SelectedObject.Guid));
				}
			}
		}

		private Tuple<bool, bool> CanCmdExport => new Tuple<bool, bool>(true, !_recordList.AreCustomFieldsAProblem(new[] { RnGenericRecTags.kClassId }));

		private void FileExportMenu_Click(object sender, EventArgs e)
		{
			using (var dlg = new NotebookExportDialog())
			{
				dlg.InitializeFlexComponent(_majorFlexComponentParameters.FlexComponentParameters);
				dlg.ShowDialog((Form)_majorFlexComponentParameters.MainWindow);
			}
		}

		private Tuple<bool, bool> CanCmdImportSFMNotebook => new Tuple<bool, bool>(true, true);

		private void ImportSFMNotebook_Clicked(object sender, EventArgs e)
		{
			using (var importWizardDlg = new NotebookImportWiz())
			{
				AreaServices.HandleDlg(importWizardDlg, _majorFlexComponentParameters.LcmCache, _majorFlexComponentParameters.FlexApp, _majorFlexComponentParameters.MainWindow, _majorFlexComponentParameters.FlexComponentParameters.PropertyTable, _majorFlexComponentParameters.FlexComponentParameters.Publisher);
			}
		}
	}
}