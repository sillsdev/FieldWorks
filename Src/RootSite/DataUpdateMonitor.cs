// Copyright (c) 2004-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary>
	/// Create an instance of this class for the duration of a possibly substantial data update.
	/// The DataUpdateMonitor will properly handle all requests to Commit for the duration of
	/// the data update.
	/// Event handlers that do various data updates will also want to test IsUpdateInProgress
	/// because we don't want to begin a second update for a particular DB connections before
	/// the first is done. For example, you don't want to begin an EditPaste or even process
	/// a keypress if another update has not finished processing.
	/// </summary>
	/// <remarks>The purpose of the DataUpdateMonitor is to fix real and potential crashes
	/// resulting from processing windows messages when a data operation is in progress.</remarks>
	public sealed class DataUpdateMonitor : IDisposable
	{
		// Keys are ISilDataAccess objects, values are UpdateSemaphore objects
		private static UpdateSemaphore s_updateSemaphore;
		private WaitCursor m_wait;

		/// <summary />
		/// <param name="owner">An optional owner (A wait cursor will be put on it if not null).
		/// </param>
		/// <param name="updateDescription">A simple description of what is being done.</param>
		public DataUpdateMonitor(Control owner, string updateDescription)
		{
			if (s_updateSemaphore == null)
			{
				s_updateSemaphore = new UpdateSemaphore(true, updateDescription);
			}
			else
			{
				if (s_updateSemaphore.fDataUpdateInProgress)
				{
					throw new Exception("Concurrent access on Database detected. Previous access: " + s_updateSemaphore.sDescr);
				}
				// Set ((static semaphore) members) for this data update
				s_updateSemaphore.fDataUpdateInProgress = true;
				s_updateSemaphore.sDescr = updateDescription;
			}
			// Set wait cursor
			if (owner != null)
			{
				m_wait = new WaitCursor(owner);
			}
		}

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		private bool IsDisposed { get; set; }

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~DataUpdateMonitor()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
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

		/// <summary>
		/// Executes in two distinct scenarios.
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing">if set to <c>true</c> [disposing].</param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		private void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + " ******************");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				try
				{
					// Dispose managed resources here.
					var semaphore = s_updateSemaphore;
					Debug.Assert(semaphore.fDataUpdateInProgress);
					semaphore.fDataUpdateInProgress = false;
					semaphore.sDescr = string.Empty;
				}
				finally
				{
					// end Wait Cursor
					// Since it needs m_wait, this must be done when 'disposing' is true,
					// as that is a disposable object, which may be gone in
					// Finalizer mode.
					m_wait?.Dispose();
				}
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_wait = null;

			IsDisposed = true;
		}

		/// <summary>
		/// If true, a data update is in progress.
		/// </summary>
		/// <returns><c>true</c> if a data update is in progress, otherwise <c>false</c>.</returns>
		public static bool IsUpdateInProgress()
		{
			return s_updateSemaphore != null && s_updateSemaphore.fDataUpdateInProgress;
		}

		/// <summary>
		/// Remove knowledge of a particular SilDataAccess (DB connection). This is used both
		/// for testing and also whenever a database connection is being closed but the app is
		/// not being destroyed (e.g., during backup, or when the last window connected to a
		/// particular DB is closed).
		/// </summary>
		public static void ClearSemaphore()
		{
			Debug.Assert(!IsUpdateInProgress());
			s_updateSemaphore = null;
		}

		/// <summary>
		/// This class represents the data update status for a given database connection.
		/// Objects of this class are kept in DataUpdateMonitor's static hashtable.
		/// </summary>
		private sealed class UpdateSemaphore
		{
			/// <summary />
			internal bool fDataUpdateInProgress;
			/// <summary />
			internal string sDescr;

			/// <summary />
			/// <param name="fUpdateInProgress">Probably always going to be true.</param>
			/// <param name="sDescription">description of the data update being done,
			/// for debugging.</param>
			internal UpdateSemaphore(bool fUpdateInProgress, string sDescription)
			{
				fDataUpdateInProgress = fUpdateInProgress;
				sDescr = sDescription;
			}
		}
	}
}