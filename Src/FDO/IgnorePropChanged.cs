// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2006' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: IgnorePropChanged.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;

using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.FDO
{
	#region PropChanged handling enum
	/// <summary>Determines how PropChanged should be handled</summary>
	[Flags]
	public enum PropChangedHandling
	{
		/// <summary>No PropChanged notifications are suppressed</summary>
		SuppressNone = 0,
		/// <summary>The view doesn't get any PropChanged notifications</summary>
		SuppressView = 1,
		/// <summary>ChangeWatcher doesn't get PropChanged notifications</summary>
		SuppressChangeWatcher = 2,
		/// <summary>All PropChanged noticiations are suppressed (view + ChangeWatcher)</summary>
		SuppressAll = SuppressView | SuppressChangeWatcher,
	}
	#endregion

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Helper class to ignore PropChanged notifications
	/// </summary>
	/// <remarks>Use like this:
	/// using (new IgnorePropChanged(cache, PropChangedHandling.SuppressChangeWatcher))
	/// {
	///		// do stuff where you don't want PropChanged to be processed
	///	}
	///	</remarks>
	/// ----------------------------------------------------------------------------------------
	public class IgnorePropChanged : IFWDisposable
	{
		private FdoCache m_cache;
		private PropChangedHandling m_oldHandling;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="cache">The FDO cache</param>
		/// ------------------------------------------------------------------------------------
		public IgnorePropChanged(FdoCache cache): this(cache, PropChangedHandling.SuppressAll)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="cache">The FDO cache</param>
		/// <param name="propChangedHandling">How to process PropChanged notifications</param>
		/// ------------------------------------------------------------------------------------
		public IgnorePropChanged(FdoCache cache, PropChangedHandling propChangedHandling)
		{
			m_cache = cache;
			m_oldHandling = m_cache.PropChangedHandling;
			m_cache.PropChangedHandling |= propChangedHandling;
		}

		#region IDisposable & Co. implementation
		// Region last reviewed: never

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		/// True, if the object has been disposed.
		/// </summary>
		private bool m_isDisposed = false;

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed
		{
			get { return m_isDisposed; }
		}

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~IgnorePropChanged()
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
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				if (m_cache != null)
					m_cache.PropChangedHandling = m_oldHandling;
			}
			m_cache = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation
	}

}
