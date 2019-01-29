// Copyright (c) 2018-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using SIL.Code;

namespace LanguageExplorer.Areas.Lexicon.Tools.Browse
{
	/// <summary>
	/// This class handles all interaction for the NotebookBrowseTool for its menus, toolbars, plus all context menus that are used in Slices and PaneBars.
	/// </summary>
	internal sealed class LexiconBrowseToolMenuHelper : IToolUiWidgetManager
	{
		private MajorFlexComponentParameters _majorFlexComponentParameters;
		private IArea _area;
		private IAreaUiWidgetManager _lexiconAreaMenuHelper;
		internal BrowseViewContextMenuFactory MyBrowseViewContextMenuFactory { get; private set; }

		internal LexiconBrowseToolMenuHelper()
		{
		}

		#region Implementation of IToolUiWidgetManager
		/// <inheritdoc />
		void IToolUiWidgetManager.Initialize(MajorFlexComponentParameters majorFlexComponentParameters, IArea area, IRecordList recordList)
		{
			Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
			Guard.AgainstNull(area, nameof(area));

			_majorFlexComponentParameters = majorFlexComponentParameters;
			_area = area;
			_lexiconAreaMenuHelper = new LexiconAreaMenuHelper();
			_lexiconAreaMenuHelper.Initialize(majorFlexComponentParameters, area, this, recordList);
			MyBrowseViewContextMenuFactory = new BrowseViewContextMenuFactory();
			((LexiconAreaMenuHelper)_lexiconAreaMenuHelper).MyAreaWideMenuHelper.SetupToolsCustomFieldsMenu();
		}

		/// <inheritdoc />
		ITool IToolUiWidgetManager.ActiveTool => _area.ActiveTool;

		/// <inheritdoc />
		void IToolUiWidgetManager.UnwireSharedEventHandlers()
		{
			_lexiconAreaMenuHelper.UnwireSharedEventHandlers();
		}
		#endregion

		#region Implementation of IDisposable
		private bool _isDisposed;

		~LexiconBrowseToolMenuHelper()
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
				// No need to run it more than once.
				return;
			}
			if (disposing)
			{
				MyBrowseViewContextMenuFactory?.Dispose();
				_lexiconAreaMenuHelper?.Dispose();
			}
			_majorFlexComponentParameters = null;
			_area = null;
			_lexiconAreaMenuHelper = null;
			MyBrowseViewContextMenuFactory = null;
			_isDisposed = true;
		}
		#endregion
	}
}