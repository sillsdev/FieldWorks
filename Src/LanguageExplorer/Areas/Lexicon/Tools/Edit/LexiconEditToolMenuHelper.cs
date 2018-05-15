// Copyright (c) 2017-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using LanguageExplorer.Controls.DetailControls;
using SIL.Code;
using SIL.Collections;

namespace LanguageExplorer.Areas.Lexicon.Tools.Edit
{
	/// <summary>
	/// This class handles all interaction for the LexiconEditTool for its menus, toolbars, plus all context menus that are used in Slices and PaneBars.
	/// </summary>
	internal sealed class LexiconEditToolMenuHelper : IDisposable
	{
		private LexiconAreaMenuHelper _lexiconAreaMenuHelper;
		private string _extendedPropertyName;
		private MajorFlexComponentParameters _majorFlexComponentParameters;
		private Dictionary<string, IToolUiWidgetManager> _lexiconEditToolUiWidgetManagers = new Dictionary<string, IToolUiWidgetManager>();
		private DataTree MyDataTree { get; set; }
		private RecordBrowseView RecordBrowseView { get; set; }
		private IRecordList MyRecordList { get; set; }
		internal MultiPane InnerMultiPane { get; set; }
		internal SliceContextMenuFactory SliceContextMenuFactory { get; set; }
		private Dictionary<string, EventHandler> _sharedEventHandlers;
		private const string editMenu = "editMenu";
		private const string viewMenu = "viewMenu";
		private const string insertMenu = "insertMenu";
		private const string toolsMenu = "toolsMenu";
		private const string insertToolbar = "insertToolbar";
		private const string dataTreeStack = "dataTreeStack";

		internal LexiconEditToolMenuHelper(MajorFlexComponentParameters majorFlexComponentParameters, DataTree dataTree, RecordBrowseView recordBrowseView, IRecordList recordList, string extendedPropertyName)
		{
			Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
			Guard.AgainstNull(dataTree, nameof(dataTree));
			Guard.AgainstNull(recordList, nameof(recordList));
			Guard.AgainstNullOrEmptyString(extendedPropertyName, nameof(extendedPropertyName));

			_majorFlexComponentParameters = majorFlexComponentParameters;
			MyDataTree = dataTree;
			RecordBrowseView = recordBrowseView;
			MyRecordList = recordList;
			SliceContextMenuFactory = MyDataTree.SliceContextMenuFactory;
			_lexiconAreaMenuHelper = new LexiconAreaMenuHelper(_majorFlexComponentParameters, MyRecordList);
			_extendedPropertyName = extendedPropertyName;
		}

		internal void Initialize()
		{
			_lexiconAreaMenuHelper.Initialize();
			_lexiconAreaMenuHelper.MyAreaWideMenuHelper.SetupToolsCustomFieldsMenu();

			// Collect up all shared stuff, before initializing them.
			// That way, they can each have access to everyone's shared event handlers.
			// Otherwise, there is significant risk of them looking for a shared handler, but not finding it.
			_sharedEventHandlers = new Dictionary<string, EventHandler>();
			InitialManagerSetup(new LexiconEditToolEditMenuManager(), editMenu);
			InitialManagerSetup(new LexiconEditToolViewMenuManager(), viewMenu);
			InitialManagerSetup(new LexiconEditToolInsertMenuManager(), insertMenu);
			InitialManagerSetup(new LexiconEditToolToolsMenuManager(), toolsMenu);
			InitialManagerSetup(new LexiconEditToolToolbarManager(), insertToolbar);
			InitialManagerSetup(new LexiconEditToolDataTreeStackManager(), dataTreeStack);

			// Now, it is fine to finish up the initialization of the managers, since all shared event handlers are in '_sharedEventHandlers'.
			FinalManagerSetup(_lexiconEditToolUiWidgetManagers[editMenu]);
			FinalManagerSetup(_lexiconEditToolUiWidgetManagers[dataTreeStack], new List<object> { SliceContextMenuFactory, MyDataTree }, _sharedEventHandlers);
			FinalManagerSetup(_lexiconEditToolUiWidgetManagers[insertMenu], new List<object> { MyDataTree });
			FinalManagerSetup(_lexiconEditToolUiWidgetManagers[toolsMenu], new List<object> { _lexiconAreaMenuHelper, RecordBrowseView.BrowseViewer });
			FinalManagerSetup(_lexiconEditToolUiWidgetManagers[insertToolbar], sharedEventHandlers: _sharedEventHandlers);
			FinalManagerSetup(_lexiconEditToolUiWidgetManagers[viewMenu], new List<object> { _extendedPropertyName, InnerMultiPane });
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
				return; // No need to do it more than once.
			}

			if (disposing)
			{
				_lexiconAreaMenuHelper.Dispose();

				foreach (var handler in _lexiconEditToolUiWidgetManagers.Values)
				{
					handler.Dispose();
				}
				_lexiconEditToolUiWidgetManagers.Clear();
				_sharedEventHandlers.Clear();
			}
			_lexiconAreaMenuHelper = null;
			_extendedPropertyName = null;
			_majorFlexComponentParameters = null;
			_lexiconEditToolUiWidgetManagers = null;
			SliceContextMenuFactory = null;
			MyDataTree = null;
			MyRecordList = null;
			InnerMultiPane = null;
			RecordBrowseView = null;
			_sharedEventHandlers = null;

			_isDisposed = true;
		}
		#endregion

		private void InitialManagerSetup(IToolUiWidgetManager manager, string key)
		{
			_lexiconEditToolUiWidgetManagers.Add(key, manager);
			_sharedEventHandlers.AddRange(manager.SharedEventHandlers);
		}

		private void FinalManagerSetup(IToolUiWidgetManager menuManager, System.Collections.Generic.IReadOnlyList<object> randomParameters = null, Dictionary<string, EventHandler> sharedEventHandlers = null)
		{
			menuManager.Initialize(_majorFlexComponentParameters, MyRecordList, sharedEventHandlers, randomParameters);
		}
	}
}