// Copyright (c) 2017-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary>
	/// <para>Factory class that is given to each Slice, so the Slice can then get the context menu(s) it may need.</para>
	/// <para>Each slice is responsible to dispose of any context menu it asks the factory to create.
	/// The slice can pass on the dispose obligation to one of its internal Control instances, or the Slice can do the dispose itself.
	/// The context menu disposal *must* (read: it is imperative that) unwire the event handlers, which are provided.</para>
	/// </summary>
	internal sealed class DataTreeStackContextMenuFactory: IDisposable
	{
		internal PanelMenuContextMenuFactory MainPanelMenuContextMenuFactory { get; private set; }
		internal SliceLeftEdgeContextMenuFactory LeftEdgeContextMenuFactory { get; private set; }
		internal SliceRightClickPopupMenuFactory RightClickPopupMenuFactory { get; private set; }
		internal SliceHotlinksMenuFactory HotlinksMenuFactory { get; private set; }

		internal DataTreeStackContextMenuFactory()
		{
			MainPanelMenuContextMenuFactory = new PanelMenuContextMenuFactory();
			LeftEdgeContextMenuFactory = new SliceLeftEdgeContextMenuFactory();
			RightClickPopupMenuFactory = new SliceRightClickPopupMenuFactory();
			HotlinksMenuFactory = new SliceHotlinksMenuFactory();
		}

		#region IDisposable

		private bool _isDisposed;

		~DataTreeStackContextMenuFactory()
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
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (_isDisposed)
			{
				return; // No need to do it more than once.
			}

			if (disposing)
			{
				MainPanelMenuContextMenuFactory.Dispose();
				LeftEdgeContextMenuFactory.Dispose();
				RightClickPopupMenuFactory.Dispose();
				HotlinksMenuFactory.Dispose();
			}
			MainPanelMenuContextMenuFactory = null;
			LeftEdgeContextMenuFactory = null;
			RightClickPopupMenuFactory = null;
			HotlinksMenuFactory = null;

			_isDisposed = true;
	}
		#endregion
	}
}
