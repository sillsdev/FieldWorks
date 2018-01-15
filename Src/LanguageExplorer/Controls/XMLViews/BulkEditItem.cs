// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// A bulk edit item manages a control (typically a combo) for a column that can
	/// handle a list choice bulk edit operation. (The name reflects an original intent
	/// that it should handle any kind of bulk edit for its column.)
	/// </summary>
	public class BulkEditItem : IDisposable
	{
		IBulkEditSpecControl _bulkEditSpecControl;

		/// <summary />
		public BulkEditItem(IBulkEditSpecControl bulkEditSpecControl)
		{
			_bulkEditSpecControl = bulkEditSpecControl;
		}

		/// <summary>
		///
		/// </summary>
		public IBulkEditSpecControl BulkEditControl
		{
			get
			{
				CheckDisposed();
				return _bulkEditSpecControl;
			}
		}

		#region IDisposable & Co. implementation
		// Region last reviewed: RandyR: Oct. 16, 2005.

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException($"'{GetType().Name}' in use after being disposed.");
		}

		/// <summary>
		/// True, if the object has been disposed.
		/// </summary>
		private bool m_isDisposed;

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed => m_isDisposed;

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~BulkEditItem()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <summary>
		///
		/// </summary>
		/// <remarks>Must not be virtual.</remarks>
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

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected virtual void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + ". ******************");
			// Must not be run more than once.
			if (m_isDisposed)
			{
				return;
			}

			if (disposing)
			{
				// Dispose managed resources here.
				(_bulkEditSpecControl as IDisposable)?.Dispose();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			_bulkEditSpecControl = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation
	}
}