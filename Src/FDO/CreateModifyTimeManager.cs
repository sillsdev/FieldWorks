using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Ling;

namespace SIL.FieldWorks.FDO
{
	/// <summary>
	/// Create an instance of this class and hook it to the main cache to arrange for
	/// PropChanged calls to result in updating the modify and create times of objects.
	/// Hack: also responsible for some other side effects of modifying particular properties.
	/// </summary>
	public class CreateModifyTimeManager : IVwNotifyChange, IFWDisposable
	{
		/// <summary>
		/// Use this to do the Add/RemoveNotifications, since it can be used in the unmanged section of Dispose.
		/// (If m_sda is COM, that is.)
		/// Doing it there will be safer, since there was a risk of it not being removed
		/// in the mananged section, as when disposing was done by the Finalizer.
		/// </summary>
		private ISilDataAccess m_sda;
		FdoCache m_cache;
		IFwMetaDataCache m_mdc;
		DateTime m_currentMinute;
		bool m_fModifyChangeInProgress = false;
		bool m_fDisabled = false;
		// Dictionary where key is hvo and value is a hvo identified as the owner of hvo
		// that needs its modify time updated when hvo is modified.
		Dictionary<int, int> m_recentMods = new Dictionary<int, int>();

		/// <summary>
		/// Create one and install it to work on a particular cache.
		/// </summary>
		/// <param name="cache"></param>
		public CreateModifyTimeManager(FdoCache cache)
		{
			m_cache = cache;
			Debug.Assert(cache.CreateModifyManager == null);
			cache.CreateModifyManager = this;

			m_sda = cache.MainCacheAccessor;
			m_sda.AddNotification(this);

			m_mdc = cache.MetaDataCacheAccessor;
		}
		/// <summary>
		/// While Disabled is true, changes will not affect CreateTime. This is currently used
		/// in setting up tests. It may also be useful for certain kinds of import.
		/// </summary>
		public bool Disabled
		{
			get
			{
				CheckDisposed();
				return m_fDisabled;
			}
			set
			{
				CheckDisposed();
				m_fDisabled = value;
			}
		}

		#region IDisposable & Co. implementation

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
		~CreateModifyTimeManager()
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
				if (m_sda != null)
					m_sda.RemoveNotification(this);

				if (m_cache.CreateModifyManager == this)
					m_cache.CreateModifyManager = null;
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_sda = null;
			m_cache = null; // ensures it can't be used again without crashing.
			m_mdc = null;
			m_recentMods = null;

			m_isDisposed = true;
		}
		#endregion

		/// <summary>
		/// Mainly used in testing, this causes the manager to forget about recently
		/// modified objects, so new modifications will be recorded immediately.
		/// </summary>
		public void ResetDelay()
		{
			CheckDisposed();
			m_currentMinute = DateTime.Now;
			m_recentMods.Clear();
		}
		#region IVwNotifyChange Members

		const int ktagMinVp = 0x7f000000; // copied from VwCacheDa.h, mimimum value for assigned virtual props.
		/// <summary>
		/// We implement this to record modify times.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="ivMin"></param>
		/// <param name="cvIns"></param>
		/// <param name="cvDel"></param>
		public void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			CheckDisposed();
			if (m_fModifyChangeInProgress) // ignore PropChanged on the DateModified property!
				return;
			// Typically an Undo will restore the modify time to what it was before the
			// main change. We don't want to reverse that by recording the time of the Undo!
			// Note that there is one pathological scenario:
			// t1: make a change.
			// t2: undo the change.
			// t3: export all changed records (since before t1).
			// t4: redo the change. Record appears to have been modified at time t2.
			// t5: export all changed records (since t3).
			// If this was the only change to the record, it is not part of either export,
			// and the change might be lost.
			// To prevent this, I think the 'export changed records' function should clear
			// the Undo stack.
			if (m_cache.ActionHandlerAccessor != null && m_cache.ActionHandlerAccessor.IsUndoOrRedoInProgress)
				return;
			if (tag < 0 || tag >= ktagMinVp)
				return; // phony or virtual properties, not real changes (e.g., different filter applied, or selection moving in browse).
			try
			{
				DateTime dtNow = DateTime.Now;
				int hvoMajor = 0; // hvo of the 'major' object that has the DateModified property.
				if (SameMinute(dtNow, m_currentMinute))
				{
					// We can use our Dictionary
					if (m_recentMods.ContainsKey(hvo))
						hvoMajor = m_recentMods[hvo];
				}
				else
				{
					ResetDelay();
				}
				int flidModify = 0;
				if (hvoMajor == 0)
				{
					for (int hvoCandidate = hvo; hvoCandidate != 0;
						hvoCandidate = m_cache.GetOwnerOfObject(hvoCandidate))
					{
						// We assume that this is a dummy object at this point and GetClassOfObject
						// only accepts real objects. Since it's a dummy object, changes should never
						// affect the modify time of a real object. Maybe at some point we can get an
						// interface method to check for dummy objects.
						if (hvoCandidate < 0)
							return;
						int clid = m_cache.GetClassOfObject(hvoCandidate);
						if (clid == 0)
							return; // maybe a deleted object, no good to us, we can't record modify time.
						flidModify = (int)m_mdc.GetFieldId2((uint)clid, "DateModified", true);
						if (flidModify != 0)
						{
							hvoMajor = hvoCandidate;
							break;
						}
					}
					if (hvoMajor == 0)
						return; // found no owner with DateCreated property.
					m_recentMods[hvo] = hvoMajor; // lets us find it fast if modified again soon.
				}
				else
				{
					// We need to set flidModify!
					int clid = m_cache.GetClassOfObject(hvoMajor);
					if (clid != 0)
						flidModify = (int)m_mdc.GetFieldId2((uint)clid, "DateModified", true);
				}
				if (flidModify == 0)
					return; // can't set the time prop without a field...

				DateTime oldModifyTime = m_cache.GetTimeProperty(hvoMajor, flidModify);
				// If we already noted a modify time in the current minute, don't do it again.
				// This is intended to keep down the overhead of recording modify times for frequently
				// modified objects.
				if (SameMinute(oldModifyTime, dtNow))
				{
					//Trace.TraceInformation("New Modify Time ({0}) for {1} is same minute as the old ({2}).\n",
					//    dtNow, hvoMajor, oldModifyTime);
					return;
				}
				//				if (m_currentMinute != null &&
				//					m_currentMinute.Date == dtNow.Date && m_currentMinute.Hour = dtNow.Hour && m_currentMinute.Minute == dtNow.Minute)
				//					return;

				// Set the modify time. If possible tack this on to the last Undo task (or the current one, if a
				// transaction is open).
				if (m_cache.ActionHandlerAccessor != null && m_cache.VwOleDbDaAccessor != null &&
					((m_cache.DatabaseAccessor != null && m_cache.DatabaseAccessor.IsTransactionOpen())
						|| m_cache.CanUndo))
				{
					m_cache.ContinueUndoTask();
					m_cache.SetTimeProperty(hvoMajor, flidModify, dtNow);
					m_cache.EndUndoTask();
				}
				else
				{
					// We don't have an Undo task to append to, so somehow a Save has occurred
					// in the midst of the operation that caused the time stamp modification.
					// If we make an empty undo task, the user will typically have no idea what
					// could be undone, and will be confused. Best to make it impossible to
					// undo the timestamp change also.
					using (new SuppressSubTasks(m_cache))
					{
						m_cache.SetTimeProperty(hvoMajor, flidModify, dtNow);
					}
				}
				//Trace.TraceInformation("Setting new Modify Time ({0}) for {1}\n",
				//        dtNow, hvoMajor);
			}
			finally
			{
				m_fModifyChangeInProgress = false;
			}
		}

		/// <summary>
		/// Return true if the two modify times are in the same minute.
		/// </summary>
		/// <param name="first"></param>
		/// <param name="second"></param>
		/// <returns></returns>
		private bool SameMinute(DateTime first, DateTime second)
		{
			return first.Date == second.Date && first.Hour == second.Hour && first.Minute == second.Minute;
		}

		#endregion
	}

/* This is no longer being used, but but it illustrates a way a handler can be called that monitors a slice being changed.
	class UpdateEntryCitationFormIdleProcessor
	{
		int m_hvo;
		FdoCache m_cache;
		public UpdateEntryCitationFormIdleProcessor(int hvo, FdoCache cache)
		{
			m_hvo = hvo;
			m_cache = cache;
			System.Windows.Forms.Application.Idle += new EventHandler(Application_Idle_UpdateEntryCitationFormIdleProcessor);
		}

		private void Application_Idle_UpdateEntryCitationFormIdleProcessor(object sender, EventArgs e)
		{
			System.Windows.Forms.Application.Idle -= new EventHandler(Application_Idle_UpdateEntryCitationFormIdleProcessor);
			LexEntry le = (LexEntry)LexEntry.CreateFromDBObject(m_cache, m_hvo, false);
			le.UpdateExcludeFromCitationForm();
		}
	}
*/
}
