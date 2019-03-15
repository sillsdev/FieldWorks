// Copyright (c) 2017-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using SIL.Code;

namespace LanguageExplorer.Areas.Grammar
{
#if RANDYTODO
	// TODO: Make this a private class of GrammarArea. No tool/control should use it.
#endif
	/// <summary>
	/// This class handles all interaction for the Grammar Area common menus.
	/// </summary>
	internal sealed class GrammarAreaMenuHelper : IAreaUiWidgetManager
	{
		private MajorFlexComponentParameters _majorFlexComponentParameters;
		private IArea _area;
		private PartiallySharedAreaWideMenuHelper _partiallySharedAreaWideMenuHelper;

		internal GrammarAreaMenuHelper()
		{
		}

		#region Implementation of IAreaUiWidgetManager
		/// <inheritdoc />
		void IAreaUiWidgetManager.Initialize(MajorFlexComponentParameters majorFlexComponentParameters, IArea area, IRecordList recordList)
		{
			Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
			Guard.AgainstNull(area, nameof(area));
			Require.That(area.MachineName == AreaServices.GrammarAreaMachineName);

			_majorFlexComponentParameters = majorFlexComponentParameters;
			_area = area;
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

		~GrammarAreaMenuHelper()
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
				_partiallySharedAreaWideMenuHelper.Dispose();
			}
			_majorFlexComponentParameters = null;
			_partiallySharedAreaWideMenuHelper = null;

			_isDisposed = true;
		}
		#endregion
	}
}