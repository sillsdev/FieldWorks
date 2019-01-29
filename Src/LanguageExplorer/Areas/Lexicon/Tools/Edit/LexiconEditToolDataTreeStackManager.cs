// Copyright (c) 2018-2019 SIL International
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
	/// Implementation that supports the addition(s) to the DataTree's context menus and hotlinks for the Lexicon Edit tool.
	/// </summary>
	internal sealed class LexiconEditToolDataTreeStackManager : IPartialToolUiWidgetManager
	{
		private const string MainPanelManager = "MainPanelManager";
		private const string LexEntryManager = "LexEntryManager";
		private Dictionary<string, IPartialToolUiWidgetManager> _dataTreeWidgetManagers;

		public LexiconEditToolDataTreeStackManager(DataTree dataTree)
		{
			Guard.AgainstNull(dataTree, nameof(dataTree));

			_dataTreeWidgetManagers = new Dictionary<string, IPartialToolUiWidgetManager>
			{
				{ MainPanelManager, new LexiconEditToolDataTreeMainPanelContextMenuStripManager(dataTree.DataTreeStackContextMenuFactory) },
				{ LexEntryManager, new LexiconEditToolDataTreeStackLexEntryManager(dataTree) }
			};
		}

		#region Implementation of IPartialToolUiWidgetManager

		/// <inheritdoc />
		void IPartialToolUiWidgetManager.Initialize(MajorFlexComponentParameters majorFlexComponentParameters, IToolUiWidgetManager toolUiWidgetManager, IRecordList recordList)
		{
			Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
			Guard.AgainstNull(recordList, nameof(recordList));

			foreach (var manager in _dataTreeWidgetManagers.Values)
			{
				manager.Initialize(majorFlexComponentParameters, toolUiWidgetManager, recordList);
			}
		}

		/// <inheritdoc />
		void IPartialToolUiWidgetManager.UnwireSharedEventHandlers()
		{
			foreach (var manager in _dataTreeWidgetManagers.Values)
			{
				manager.UnwireSharedEventHandlers();
			}
		}

		#endregion

		#region IDisposable

		private bool _isDisposed;

		~LexiconEditToolDataTreeStackManager()
		{
			// The base class finalizer is called automatically.
			Dispose(false);
		}

		/// <inheritdoc />
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
				// No need to do it more than once.
				return;
			}

			if (disposing)
			{
				foreach (var manager in _dataTreeWidgetManagers.Values)
				{
					manager.Dispose();
				}
				_dataTreeWidgetManagers.Clear();
			}
			_dataTreeWidgetManagers = null;

			_isDisposed = true;
		}

		#endregion
	}
}