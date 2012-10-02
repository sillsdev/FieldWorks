using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.Cellar;

namespace SIL.FieldWorks.FDO
{
	/// <summary>
	/// Create an instance of SyncWatcher to arrange that Sync records are created in the database
	/// for all changes recorded in the FdoCache (provided at least one other application is
	/// running).
	/// </summary>
	public class SyncWatcher : IVwNotifyChange, IFWDisposable
	{
		/// <summary>
		/// Use this to do the Add/RemoveNotifications, since it can be used in the unmanged section of Dispose.
		/// (If m_sda is COM, that is.)
		/// Doing it there will be safer, since there was a risk of it not being removed
		/// in the mananged section, as when disposing was done by the Finalizer.
		/// </summary>
		private ISilDataAccess m_sda;
		FdoCache m_cache;
		Guid m_appGuid;
		// These two variables record a property that we should not report, typically because
		// the system is currently processing a Sync record from another application.
		int m_hvoIgnore;
		int m_flidIgnore;

		/// <summary>
		/// Create one.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="appGuid"></param>
		public SyncWatcher(FdoCache cache, Guid appGuid)
		{
			m_cache = cache;
			m_appGuid = appGuid;

			m_sda = m_cache.MainCacheAccessor;
			m_sda.AddNotification(this);
		}

		/// <summary>
		/// Set a pair of values for a property for which sync records will not be created.
		/// (Since 0 is never used for an HVO, set to 0,0 to disable.)
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="flid"></param>
		public void SetIgnore(int hvo, int flid)
		{
			CheckDisposed();
			m_hvoIgnore = hvo;
			m_flidIgnore = flid;
		}

		#region IVwNotifyChange Members

		/// <summary>
		/// Handle a change by adding a record to the database (only if the
		/// change does not arise directly out of processing a sync record from elsewhere).
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="ivMin"></param>
		/// <param name="cvIns"></param>
		/// <param name="cvDel"></param>
		public void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			CheckDisposed();
			if (hvo == m_hvoIgnore && tag == m_flidIgnore)
				return;
			if (tag < 0)
				return; // common for fake properties
			// If it's a virtual property we don't want to make a sync record.
			IVwCacheDa cda = m_cache.MainCacheAccessor as IVwCacheDa;
			IVwVirtualHandler vh = cda.GetVirtualHandlerId(tag);
			if (vh != null)
				return;
			// (CLE-76) Topics List Editor needs to be sync'd with the database when
			// we make certain changes to possibility lists, especially structural ones
			// like adding/removing/moving/promoting possibilities. The simplest thing
			// to do for now, is to indicate a full refresh is needed when any of these
			// properties are being altered (even if it turns out to be minor).
			SyncMsg msg = SyncMsg.ksyncSimpleEdit;
			if (tag == (int)CmPossibility.CmPossibilityTags.kflidName ||
				tag == (int)CmPossibility.CmPossibilityTags.kflidAbbreviation ||
				tag == (int)CmPossibilityList.CmPossibilityListTags.kflidPossibilities ||
				tag == (int)CmPossibility.CmPossibilityTags.kflidSubPossibilities)
			{
				// NOTE: assume that apps that watch for ksyncFullRefresh will only
				// refresh once, and not multiple times if multiple refreshes get posted.
				// This is how Topic List Editor currently behaves.
				msg = SyncMsg.ksyncFullRefresh;
			}
			m_cache.StoreSync(m_appGuid, new SyncInfo(msg, hvo, tag));
		}

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
		~SyncWatcher()
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
				if (m_sda != null)
					m_sda.RemoveNotification(this);
			}
			m_cache = null;
			m_sda = null;

			// Dispose unmanaged resources here, whether disposing is true or false.

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation
	}
}
