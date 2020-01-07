// Copyright (c) 2019-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using LanguageExplorer.Controls;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorer.Areas.Notebook.Tools
{
	/// <summary>
	/// Handle setting up UI for Notebook area tools.
	/// </summary>
	internal sealed class SharedNotebookToolsUiWidgetMenuHelper : IDisposable
	{
		private MajorFlexComponentParameters _majorFlexComponentParameters;
		private ISharedEventHandlers _sharedEventHandlers;
		private IRecordList _recordList;
		private ITool _tool;

		internal SharedNotebookToolsUiWidgetMenuHelper(MajorFlexComponentParameters majorFlexComponentParameters, IRecordList recordList)
		{
			Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
			Guard.AgainstNull(recordList, nameof(recordList));

			_majorFlexComponentParameters = majorFlexComponentParameters;
			_sharedEventHandlers = _majorFlexComponentParameters.SharedEventHandlers;
			_recordList = recordList;

			// Add handlers that are shared in places, such as context menus.
			_sharedEventHandlers.Add(Command.CmdInsertSubrecord, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(Insert_Subrecord_Clicked, () => CanCmdInsertSubrecord));
			_sharedEventHandlers.Add(Command.CmdInsertSubsubrecord, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(Insert_Subsubrecord_Clicked, () => CanCmdInsertSubsubrecord));
		}

		internal ContextMenuStrip CreateBrowseViewContextMenu()
		{
			Require.That(_tool == null || _tool.MachineName != AreaServices.NotebookDocumentToolMachineName, "The Notebook Document tool is not expected to call this method.");

			// The actual menu declaration has a gazillion menu items, but only one of them is seen in this tool.
			// Start: <menu id="mnuBrowseView" (partial) >
			var contextMenuStrip = new ContextMenuStrip
			{
				Name = ContextMenuName.mnuBrowseView.ToString()
			};
			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);

			// <command id="CmdDeleteSelectedObject" label="Delete selected {0}" message="DeleteSelectedItem"/>
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, DeleteSelectedBrowseViewObject_Clicked, string.Format(AreaResources.Delete_selected_0, StringTable.Table.GetString("RnGenericRec", StringTable.ClassNames)));

			// End: <menu id="mnuBrowseView" (partial) >
			return contextMenuStrip;
		}

		private void DeleteSelectedBrowseViewObject_Clicked(object sender, EventArgs e)
		{
			_recordList.DeleteRecord(string.Format(AreaResources.Delete_selected_0, StringTable.Table.GetString("RnGenericRec", StringTable.ClassNames)), StatusBarPanelServices.GetStatusBarProgressPanel(_majorFlexComponentParameters.StatusBar));
		}

		internal void SetupToolUiWidgets(ToolUiWidgetParameterObject toolUiWidgetParameterObject, HashSet<Command> commands)
		{
			Guard.AgainstNull(toolUiWidgetParameterObject, nameof(toolUiWidgetParameterObject));

			_tool = toolUiWidgetParameterObject.Tool;

			var insertMenuDictionary = toolUiWidgetParameterObject.MenuItemsForTool[MainMenu.Insert];
			var insertToolBarDictionary = toolUiWidgetParameterObject.ToolBarItemsForTool[ToolBar.Insert];

			foreach (var command in commands)
			{
				switch (command)
				{
					case Command.CmdExport:
						toolUiWidgetParameterObject.MenuItemsForTool[MainMenu.File].Add(Command.CmdExport, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(FileExportMenu_Click, () => CanCmdExport));
						break;
					case Command.CmdInsertRecord:
						var cmdInsertRecordTuple = new Tuple<EventHandler, Func<Tuple<bool, bool>>>(Insert_Record_Clicked, () => UiWidgetServices.CanSeeAndDo);
						insertMenuDictionary.Add(Command.CmdInsertRecord, cmdInsertRecordTuple);
						insertToolBarDictionary.Add(Command.CmdInsertRecord, cmdInsertRecordTuple);
						break;
					case Command.CmdInsertSubrecord:
						insertMenuDictionary.Add(Command.CmdInsertSubrecord, _sharedEventHandlers.Get(Command.CmdInsertSubrecord));
						break;
					case Command.CmdInsertSubsubrecord:
						insertMenuDictionary.Add(Command.CmdInsertSubsubrecord, _sharedEventHandlers.Get(Command.CmdInsertSubsubrecord));
						break;
					default:
						throw new ArgumentOutOfRangeException($"Dont know how to process command: '{command.ToString()}'");
				}
			}
		}

		private Tuple<bool, bool> CanCmdExport => new Tuple<bool, bool>(true, !_majorFlexComponentParameters.LcmCache.GetManagedMetaDataCache().AreCustomFieldsAProblem(new[] { RnGenericRecTags.kClassId }));

		private void FileExportMenu_Click(object sender, EventArgs e)
		{
			using (var dlg = new NotebookExportDialog())
			{
				dlg.InitializeFlexComponent(_majorFlexComponentParameters.FlexComponentParameters);
				dlg.ShowDialog((Form)_majorFlexComponentParameters.MainWindow);
			}
		}

		private void Insert_Record_Clicked(object sender, EventArgs e)
		{
			InsertRecord_Common();
		}

		private Tuple<bool, bool> CanCmdInsertSubrecord
		{
			get
			{
				var visible = _recordList.CurrentObject != null && _recordList.CurrentObject.OwningFlid == RnResearchNbkTags.kflidRecords;
				return new Tuple<bool, bool>(visible, visible);
			}
		}

		private void Insert_Subrecord_Clicked(object sender, EventArgs e)
		{
			InsertRecord_Common(false);
		}

		private Tuple<bool, bool> CanCmdInsertSubsubrecord
		{
			get
			{
				var enabled = _recordList.CurrentObject != null && _recordList.CurrentObject.OwningFlid == RnGenericRecTags.kflidSubRecords;
				return new Tuple<bool, bool>(true, enabled);
			}
		}

		private void Insert_Subsubrecord_Clicked(object sender, EventArgs e)
		{
			/*
				<command id="CmdDataTree_Insert_Subsubrecord" label="Insert S_ubrecord of Subrecord" message="InsertItemInVector">
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
				var cache = _majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue<LcmCache>(FwUtils.cache);
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
					LinkHandler.PublishFollowLinkMessage(_majorFlexComponentParameters.FlexComponentParameters.Publisher, new FwLinkArgs(_tool.MachineName, dlg.NewRecord.Guid));
				}
			}
		}

		#region Implementation of IDisposable
		private bool _isDisposed;

		~SharedNotebookToolsUiWidgetMenuHelper()
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
				_sharedEventHandlers.Remove(Command.CmdInsertSubrecord);
				_sharedEventHandlers.Remove(Command.CmdInsertSubsubrecord);
			}
			_majorFlexComponentParameters = null;
			_tool = null;
			_sharedEventHandlers = null;
			_recordList = null;

			_isDisposed = true;
		}
		#endregion
	}
}