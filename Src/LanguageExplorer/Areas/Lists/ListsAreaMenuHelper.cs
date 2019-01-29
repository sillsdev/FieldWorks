// Copyright (c) 2017-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using LanguageExplorer.Controls.DetailControls;
using SIL.Code;
using SIL.LCModel;

namespace LanguageExplorer.Areas.Lists
{
	/// <summary>
	/// This class handles all interaction for the Lists Area common menus.
	/// </summary>
	internal sealed class ListsAreaMenuHelper : IAreaUiWidgetManager
	{
		private MajorFlexComponentParameters _majorFlexComponentParameters;
		private Dictionary<string, IPartialToolUiWidgetManager> _listAreaUiWidgetManagers;
		private ISharedEventHandlers _sharedEventHandlers;
		private IListArea _area;
		private IRecordList MyRecordList { get; set; }
		private IToolUiWidgetManager _activeToolUiManager;
		private DataTree MyDataTree { get; }
		private const string editMenu = "editMenu";
		private const string insertMenu = "insertMenu";
		private const string toolsMenu = "toolsMenu";
		private const string insertToolbar = "insertToolbar";
		private const string dataTreeStack = "dataTreeStack";
		internal const string AddNewPossibilityListItem = "AddNewPossibilityListItem";
		internal const string AddNewSubPossibilityListItem = "AddNewSubPossibilityListItem";
		internal const string InsertFeatureType = "InsertFeatureType";
		internal AreaWideMenuHelper MyAreaWideMenuHelper { get; private set; }

		internal ListsAreaMenuHelper(DataTree dataTree)
		{
			Guard.AgainstNull(dataTree, nameof(dataTree));

			MyDataTree = dataTree;
		}

		#region Implementation of IAreaUiWidgetManager
		/// <inheritdoc />
		void IAreaUiWidgetManager.Initialize(MajorFlexComponentParameters majorFlexComponentParameters, IArea area, IToolUiWidgetManager toolUiWidgetManager, IRecordList recordList)
		{
			Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
			Guard.AgainstNull(area, nameof(area));
			Require.That(area.MachineName == AreaServices.ListsAreaMachineName);
			Require.That(area is IListArea);
			Guard.AgainstNull(recordList, nameof(recordList));

			_majorFlexComponentParameters = majorFlexComponentParameters;
			_area = (IListArea)area;
			_activeToolUiManager = toolUiWidgetManager; // May be null;
			MyRecordList = recordList;
			MyAreaWideMenuHelper = new AreaWideMenuHelper(_majorFlexComponentParameters, recordList); // We want this to get the shared AreaServices.DataTreeDelete handler.
			// Set up File->Export menu, which is visible and enabled in all list area tools, using the default event handler.
			MyAreaWideMenuHelper.SetupFileExportMenu();

			_listAreaUiWidgetManagers = new Dictionary<string, IPartialToolUiWidgetManager>
			{
				{ editMenu, new ListsAreaEditMenuManager() },
				{ insertMenu, new ListsAreaInsertMenuManager(MyDataTree) },
				{ toolsMenu, new ListsAreaToolsMenuManager() },
				// The ListsAreaInsertMenuManager instance adds shared event handlers that ListsAreaToolbarManager needs to use.
				{ insertToolbar, new ListsAreaToolbarManager(MyDataTree) },
				{ dataTreeStack, new ListsAreaDataTreeStackManager(MyDataTree) }
			};

			// Now, it is fine to finish up the initialization of the managers, since all shared event handlers are in '_sharedEventHandlers'.
			foreach (var manager in _listAreaUiWidgetManagers.Values)
			{
				manager.Initialize(_majorFlexComponentParameters, _activeToolUiManager, MyRecordList);
			}
		}

		/// <inheritdoc />
		ITool IAreaUiWidgetManager.ActiveTool => _area.ActiveTool;

		/// <inheritdoc />
		IToolUiWidgetManager IAreaUiWidgetManager.ActiveToolUiManager => _activeToolUiManager;

		/// <inheritdoc />
		void IAreaUiWidgetManager.UnwireSharedEventHandlers()
		{
			// If ActiveToolUiManager is null, then the tool should call this method.
			// Otherwise, ActiveToolUiManager will call it.
			foreach (var manager in _listAreaUiWidgetManagers.Values)
			{
				manager.UnwireSharedEventHandlers();
			}
		}
		#endregion

		#region IDisposable

		private bool _isDisposed;

		~ListsAreaMenuHelper()
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
				foreach (var manager in _listAreaUiWidgetManagers.Values)
				{
					manager.Dispose();
				}
				_listAreaUiWidgetManagers.Clear();
				MyAreaWideMenuHelper.Dispose();
			}
			_majorFlexComponentParameters = null;
			MyAreaWideMenuHelper = null;
			_area = null;
			MyRecordList = null;
			_sharedEventHandlers = null;
			_listAreaUiWidgetManagers = null;

			_isDisposed = true;
		}
		#endregion

		internal static ICmPossibilityList GetPossibilityList(IRecordList recordList)
		{
			return recordList.OwningObject as ICmPossibilityList; // This will be null for the AreaServices.FeatureTypesAdvancedEditMachineName tool, which isn't a list at all.
		}
	}
}