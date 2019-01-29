// Copyright (c) 2018-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using SIL.Code;

namespace LanguageExplorer.Areas.TextsAndWords.Tools.InterlinearEdit
{
	/// <summary>
	/// This class handles all interaction for the InterlinearEditTool for its menus, toolbars, plus all context menus that are used in Slices and PaneBars.
	/// </summary>
	internal sealed class InterlinearEditToolMenuHelper : IToolUiWidgetManager
	{
		private MajorFlexComponentParameters _majorFlexComponentParameters;
		private IArea _area;
		private IAreaUiWidgetManager _textAndWordsAreaMenuHelper;
		private AreaWideMenuHelper _areaWideMenuHelper;

		internal InterlinearEditToolMenuHelper()
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
			var textAndWordsAreaMenuHelper = new TextAndWordsAreaMenuHelper();
			_textAndWordsAreaMenuHelper = textAndWordsAreaMenuHelper;
			_textAndWordsAreaMenuHelper.Initialize(majorFlexComponentParameters, area, this, recordList);
			_areaWideMenuHelper = new AreaWideMenuHelper(_majorFlexComponentParameters);

			textAndWordsAreaMenuHelper.AddMenusForAllButConcordanceTool();
			_areaWideMenuHelper.SetupToolsCustomFieldsMenu();
		}

		/// <inheritdoc />
		ITool IToolUiWidgetManager.ActiveTool => _area.ActiveTool;

		/// <inheritdoc />
		void IToolUiWidgetManager.UnwireSharedEventHandlers()
		{
			_textAndWordsAreaMenuHelper.UnwireSharedEventHandlers();
		}
		#endregion

		#region Implementation of IDisposable
		private bool _isDisposed;

		~InterlinearEditToolMenuHelper()
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
				_textAndWordsAreaMenuHelper.Dispose();
				_areaWideMenuHelper.Dispose();
			}
			_textAndWordsAreaMenuHelper = null;
			_areaWideMenuHelper = null;
			_majorFlexComponentParameters = null;

			_isDisposed = true;
		}

		#endregion
	}
}