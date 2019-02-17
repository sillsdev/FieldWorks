// Copyright (c) 2017-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using LanguageExplorer.Controls.DetailControls;
using SIL.Code;

namespace LanguageExplorer.Areas.Lexicon.Tools.Edit
{
	/// <summary>
	/// This class handles all interaction for the LexiconEditTool for its menus, toolbars, plus all context menus that are used in Slices and PaneBars.
	/// </summary>
	internal sealed class LexiconEditToolMenuHelper : IToolUiWidgetManager
	{
		private IAreaUiWidgetManager _lexiconAreaMenuHelper;
		private string _extendedPropertyName;
		private MajorFlexComponentParameters _majorFlexComponentParameters;
		private ITool _currentTool;
		private Dictionary<string, IPartialToolUiWidgetManager> _lexiconEditToolUiWidgetManagers = new Dictionary<string, IPartialToolUiWidgetManager>();
		private DataTree MyDataTree { get; set; }
		private RecordBrowseView RecordBrowseView { get; set; }
		private IRecordList MyRecordList { get; set; }
		internal MultiPane InnerMultiPane { get; set; }
		internal DataTreeStackContextMenuFactory MyDataTreeStackContextMenuFactory { get; set; }
		private ISharedEventHandlers _sharedEventHandlers;
		private const string editMenu = "editMenu";
		private const string viewMenu = "viewMenu";
		private const string insertMenu = "insertMenu";
		private const string toolsMenu = "toolsMenu";
		private const string insertToolbar = "insertToolbar";
		private const string dataTreeStack = "dataTreeStack";
		private const string rightClickContextMenu = "rightClickContextMenu";

		internal LexiconEditToolMenuHelper(ITool currentTool, DataTree dataTree, RecordBrowseView recordBrowseView, string extendedPropertyName)
		{
			Guard.AgainstNull(currentTool, nameof(currentTool));
			Guard.AgainstNull(dataTree, nameof(dataTree));
			Guard.AgainstNullOrEmptyString(extendedPropertyName, nameof(extendedPropertyName));

			_currentTool = currentTool;
			MyDataTree = dataTree;
			MyDataTreeStackContextMenuFactory = MyDataTree.DataTreeStackContextMenuFactory;
			RecordBrowseView = recordBrowseView;
			_extendedPropertyName = extendedPropertyName;
		}

		#region Implementation of IToolUiWidgetManager
		/// <inheritdoc />
		void IToolUiWidgetManager.Initialize(MajorFlexComponentParameters majorFlexComponentParameters, IArea area, IRecordList recordList)
		{
			Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
			Guard.AgainstNull(recordList, nameof(recordList));

			_majorFlexComponentParameters = majorFlexComponentParameters;
			MyRecordList = recordList;
			_sharedEventHandlers = majorFlexComponentParameters.SharedEventHandlers;
			// Collect up all shared stuff, before initializing them.
			// That way, they can each have access to everyone's shared event handlers.
			// Otherwise, there is significant risk of them looking for a shared handler, but not finding it.
			_sharedEventHandlers.Add(LexiconAreaConstants.DataTreeMerge, DataTreeMerge_Clicked);
			_sharedEventHandlers.Add(LexiconAreaConstants.DataTreeSplit, DataTreeSplit_Clicked);
			var lexiconAreaMenuHelper = new LexiconAreaMenuHelper();
			_lexiconAreaMenuHelper = lexiconAreaMenuHelper;
			_lexiconAreaMenuHelper.Initialize(_majorFlexComponentParameters, area, this, MyRecordList);
			lexiconAreaMenuHelper.MyAreaWideMenuHelper.SetupToolsCustomFieldsMenu();
			_lexiconEditToolUiWidgetManagers.Add(editMenu, new LexiconEditToolEditMenuManager());
			_lexiconEditToolUiWidgetManagers.Add(viewMenu, new LexiconEditToolViewMenuManager(_extendedPropertyName, InnerMultiPane));
			_lexiconEditToolUiWidgetManagers.Add(insertMenu, new LexiconEditToolInsertMenuManager(MyDataTree));
			_lexiconEditToolUiWidgetManagers.Add(toolsMenu, new LexiconEditToolToolsMenuManager(lexiconAreaMenuHelper, RecordBrowseView.BrowseViewer));
			_lexiconEditToolUiWidgetManagers.Add(insertToolbar, new LexiconEditToolToolbarManager());
			_lexiconEditToolUiWidgetManagers.Add(dataTreeStack, new LexiconEditToolDataTreeStackManager(MyDataTree));
			_lexiconEditToolUiWidgetManagers.Add(rightClickContextMenu, new RightClickContextMenuManager(_currentTool, MyDataTree));
			// Now, it is fine to finish up the initialization of the managers, since all shared event handlers are in '_sharedEventHandlers'.
			foreach (var manager in _lexiconEditToolUiWidgetManagers.Values)
			{
				manager.Initialize(_majorFlexComponentParameters, this, MyRecordList);
			}
		}

		/// <inheritdoc />
		ITool IToolUiWidgetManager.ActiveTool => _currentTool;

		/// <inheritdoc />
		void IToolUiWidgetManager.UnwireSharedEventHandlers()
		{
			_lexiconAreaMenuHelper.UnwireSharedEventHandlers();
			foreach (var handlerKvp in _lexiconEditToolUiWidgetManagers)
			{
				handlerKvp.Value.UnwireSharedEventHandlers();
			}
		}
		#endregion

		private void DataTreeMerge_Clicked(object sender, EventArgs e)
		{
			var currentSlice = MyDataTree.CurrentSlice;
			currentSlice.HandleMergeCommand(true);
		}

		private void DataTreeSplit_Clicked(object sender, EventArgs e)
		{
			var currentSlice = MyDataTree.CurrentSlice;
			currentSlice.HandleSplitCommand();
		}

		#region IDisposable
		private bool _isDisposed;

		~LexiconEditToolMenuHelper()
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
				foreach (var handlerKvp in _lexiconEditToolUiWidgetManagers)
				{
					handlerKvp.Value.Dispose();
				}
				_sharedEventHandlers.Remove(LexiconAreaConstants.DataTreeMerge);
				_sharedEventHandlers.Remove(LexiconAreaConstants.DataTreeSplit);
				_lexiconEditToolUiWidgetManagers.Clear();
				_lexiconAreaMenuHelper.Dispose();
			}
			_lexiconAreaMenuHelper = null;
			_extendedPropertyName = null;
			_majorFlexComponentParameters = null;
			_lexiconEditToolUiWidgetManagers = null;
			_sharedEventHandlers = null;
			MyDataTreeStackContextMenuFactory = null;
			MyDataTree = null;
			MyRecordList = null;
			InnerMultiPane = null;
			RecordBrowseView = null;
			_currentTool = null;

			_isDisposed = true;
		}
		#endregion
	}
}