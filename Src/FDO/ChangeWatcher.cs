// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2002, SIL International. All Rights Reserved.
// <copyright from='2002' to='2002' company='SIL International'>
//		Copyright (c) 2002, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ChangeWatcher.cs
// Responsibility: TomB
// Last reviewed:
//
// <remarks>
// ChangeWatchers are used for responding programmatically to data changes
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.FDO
{
	/// -------------------------------------------------------------------------------------------
	/// <summary>
	/// ChangeWatcher base class.
	/// </summary>
	/// -------------------------------------------------------------------------------------------
	public abstract class ChangeWatcher : IVwNotifyChange, IFWDisposable
	{
		#region Data members
		/// <summary>
		/// Use this to do the Add/RemoveNotifications, since it can be used in the unmanged section of Dispose.
		/// (If m_sda is COM, that is.)
		/// Doing it there will be safer, since there was a risk of it not being removed
		/// in the mananged section, as when disposing was done by the Finalizer.
		/// </summary>
		private ISilDataAccess m_sda = null;
		/// <summary>The FDO cache</summary>
		protected FdoCache m_cache;
		/// <summary>
		/// The property tag that the caller wants to be notified about
		/// </summary>
		protected int m_Tag;
		#endregion

		#region Construction
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor to watch for changes in a single flid
		/// </summary>
		/// <param name="cache">The instance of the DB connection representing the channel
		/// through which notifications come</param>
		/// <param name="tag">The property tag that the caller wants to be notified about
		/// </param>
		/// ------------------------------------------------------------------------------------
		public ChangeWatcher(FdoCache cache, int tag)
		{
			if (cache != null)
				Init(cache, tag);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializer
		/// </summary>
		/// <param name="cache">The instance of the DB connection representing the channel
		/// through which notifications come</param>
		/// <param name="tag">The property tag that the caller wants to be notified about
		/// </param>
		/// ------------------------------------------------------------------------------------
		internal void Init(FdoCache cache, int tag)
		{
			m_cache = cache;
			if (cache.AddChangeWatcher(this))
			{
				m_sda = m_cache.MainCacheAccessor;
				m_sda.AddNotification(this); // register this in the ISilDataAccess
			}
			m_Tag = tag;
		}
		#endregion

		#region IVwNotifyChange methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The dafault behavior is for change watchers to call DoEffectsOfPropChange if the
		/// data for the tag being watched has changed.
		/// </summary>
		/// <param name="hvo">The object that was changed</param>
		/// <param name="tag">The property of the object that was changed</param>
		/// <param name="ivMin">the starting index where the change occurred</param>
		/// <param name="cvIns">the number of items inserted</param>
		/// <param name="cvDel">the number of items deleted</param>
		/// ------------------------------------------------------------------------------------
		public virtual void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			CheckDisposed();
			if (tag == m_Tag && HandlePropChanged)
			{
				DoEffectsOfPropChange(hvo, ivMin, cvIns, cvDel);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether to handle PropChanged notifications.
		/// </summary>
		/// <remarks>Default behavior is to ignore PropChanged notifications if the cache is
		/// ignoring them, but dervied classes might want to override this</remarks>
		/// ------------------------------------------------------------------------------------
		protected virtual bool HandlePropChanged
		{
			get
			{
				return (m_cache.PropChangedHandling & PropChangedHandling.SuppressChangeWatcher) ==
					PropChangedHandling.SuppressNone;
			}
		}
		#endregion

		#region Internal methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Subclass must implement this function to do the effects desired.
		/// see IVwNotifyChange for parameter details
		/// </summary>
		/// <param name="hvo">The object that was changed</param>
		/// <param name="ivMin">the starting character index where the change occurred</param>
		/// <param name="cvIns">the number of characters inserted</param>
		/// <param name="cvDel">the number of characters deleted</param>
		/// ------------------------------------------------------------------------------------
		protected abstract void DoEffectsOfPropChange(int hvo, int ivMin, int cvIns, int cvDel);
		#endregion

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
		~ChangeWatcher()
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

			// m_sda COM object block removed due to crash in Finializer thread LT-6124

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_cache != null)
					m_cache.RemoveChangeWatcher(this);

				if (m_sda != null)
					m_sda.RemoveNotification(this);
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_cache = null;
			m_sda = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation
	}
}
