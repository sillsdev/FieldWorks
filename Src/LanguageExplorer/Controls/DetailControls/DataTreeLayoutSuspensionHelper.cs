// Copyright (c) 2018-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorer.Controls.DetailControls
{
	internal sealed class DataTreeLayoutSuspensionHelper : IDisposable
	{
		private IdleProcessingHelper MyIdleProcessingHelper { get; set; }
		private DataTree MyDataTree { get; set; }

		public DataTreeLayoutSuspensionHelper(IFwMainWnd mainWindow, DataTree dataTree)
		{
			Guard.AgainstNull(dataTree, nameof(dataTree));

			MyIdleProcessingHelper = new IdleProcessingHelper(mainWindow);
			MyDataTree = dataTree;
			MyDataTree.DeepSuspendLayout();
			IsDisposed = false;
		}

		#region IDisposable

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. IsDisposed is true)
		/// </summary>
		~DataTreeLayoutSuspensionHelper()
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
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}
			if (disposing)
			{
				MyDataTree.DeepResumeLayout();
				MyIdleProcessingHelper.Dispose();
			}
			MyDataTree = null;
			MyIdleProcessingHelper = null;

			IsDisposed = true;
		}

		/// <summary>
		/// Add the public property for knowing if the object has been disposed of yet
		/// </summary>
		private bool IsDisposed
		{
			get; set;
		}
		#endregion
	}
}