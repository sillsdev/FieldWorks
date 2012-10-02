using System;
using System.Collections.Generic;
using System.Text;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.Common.FXT
{
	/// <summary>
	/// The actual item itself with the key pieces of information
	/// </summary>
	public class ChangedDataItem : IFWDisposable
	{
		private int m_hvo;
		private int m_flid;
		private int m_classid;
		private string m_sClassName;

		public ChangedDataItem(int hvo, int flid, int classid, string sClassName)
		{
			m_hvo = hvo;
			m_flid = flid;
			m_classid = classid;
			m_sClassName = sClassName;
		}

		/// <summary>
		/// Get the class ID of the changed item
		/// </summary>
		public int ClassId
		{
			get
			{
				CheckDisposed();
				return m_classid;
			}
		}
		/// <summary>
		/// Get the class name of the changed item
		/// </summary>
		public string ClassName
		{
			get
			{
				CheckDisposed();
				return m_sClassName;
			}
		}
		/// <summary>
		/// Get the flid of the changed item
		/// </summary>
		public int Flid
		{
			get
			{
				CheckDisposed();
				return m_flid;
			}
		}
		/// <summary>
		/// Get the hvo of the changed item
		/// </summary>
		public int Hvo
		{
			get
			{
				CheckDisposed();
				return m_hvo;
			}
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
		~ChangedDataItem()
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
		private void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{

				// Dispose managed resources here.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

	}

}
