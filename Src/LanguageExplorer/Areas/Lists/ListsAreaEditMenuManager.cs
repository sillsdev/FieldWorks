// Copyright (c) 2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using LanguageExplorer.Controls;
using SIL.Code;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Areas.Lists
{
	/// <summary>
	/// Implementation that supports the addition(s) to FLEx's main Edit menu for the List area tools.
	/// </summary>
	internal sealed class ListsAreaEditMenuManager : IToolUiWidgetManager
	{
		private MajorFlexComponentParameters _majorFlexComponentParameters;
		private IRecordList MyRecordList { get; set; }
		private IListArea _listArea;
		private ToolStripMenuItem _editMenu;
		private ToolStripMenuItem _deleteCustomListToolMenu;
		private List<Tuple<ToolStripMenuItem, EventHandler>> _newEditMenusAndHandlers;

		internal ListsAreaEditMenuManager(IListArea listArea)
		{
			Guard.AgainstNull(listArea, nameof(listArea));

			_listArea = listArea;
			_newEditMenusAndHandlers = new List<Tuple<ToolStripMenuItem, EventHandler>>();
		}

		#region Implementation of IToolUiWidgetManager
		/// <inheritdoc />
		void IToolUiWidgetManager.Initialize(MajorFlexComponentParameters majorFlexComponentParameters, IRecordList recordList)
		{
			Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
			Guard.AgainstNull(recordList, nameof(recordList));

			MyRecordList = recordList;

			_editMenu = MenuServices.GetEditMenu(majorFlexComponentParameters.MenuStrip);
			// End of Edit menu: <command id = "CmdDeleteCustomList" label="Delete Custom _List" message="DeleteCustomList" />
			_deleteCustomListToolMenu = ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(_newEditMenusAndHandlers, _editMenu, DeleteCustomList_Click, ListResources.DeleteCustomList);

			Application.Idle += Application_Idle;
		}

		/// <inheritdoc />
		void IToolUiWidgetManager.UnwireSharedEventHandlers()
		{
		}
		#endregion

		#region Implementation of IDisposable

		private bool _isDisposed;

		~ListsAreaEditMenuManager()
		{
			// The base class finalizer is called automatically.
			Dispose(false);
		}

		/// <inheritdoc />
		void IDisposable.Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
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
				// No need to do it more than once.
				return;
			}

			if (disposing)
			{
				Application.Idle -= Application_Idle;
				foreach (var tuple in _newEditMenusAndHandlers)
				{
					_editMenu.DropDownItems.Remove(tuple.Item1);
					tuple.Item1.Click -= tuple.Item2;
					tuple.Item1.Dispose();
				}
				_newEditMenusAndHandlers.Clear();
			}
			_editMenu = null;
			_newEditMenusAndHandlers = null;
			_deleteCustomListToolMenu = null;
			_listArea = null;

			_isDisposed = true;
		}
		#endregion

		private void Application_Idle(object sender, EventArgs e)
		{
			//Debug.WriteLine($"Start: Application.Idle run at: '{DateTime.Now:HH:mm:ss.ffff}': on '{GetType().Name}'.");
			var inDeletingTerritory = false;
			var recordListOwningObject = MyRecordList.OwningObject as ICmPossibilityList;
			if (recordListOwningObject != null && recordListOwningObject.Owner == null)
			{
				inDeletingTerritory = true;
			}
			// Only see and use the delete button for the currently selected tool
			_deleteCustomListToolMenu.Visible = _deleteCustomListToolMenu.Enabled = inDeletingTerritory;
			//Debug.WriteLine($"End: Application.Idle run at: '{DateTime.Now:HH:mm:ss.ffff}': on '{GetType().Name}'.");
		}

		private void DeleteCustomList_Click(object sender, EventArgs e)
		{
			UndoableUnitOfWorkHelper.Do(ListResources.ksUndoDeleteCustomList, ListResources.ksRedoDeleteCustomList, _majorFlexComponentParameters.LcmCache.ActionHandlerAccessor, () => new DeleteCustomList(_majorFlexComponentParameters.LcmCache).Run(ListsAreaMenuHelper.GetPossibilityList(MyRecordList)));
			_listArea.RemoveCustomListTool(_listArea.ActiveTool);
		}
	}
}