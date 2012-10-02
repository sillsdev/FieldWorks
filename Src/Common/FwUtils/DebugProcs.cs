// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: DebugProcs.cs
// Responsibility: TE Team
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Debugging helper methods. Accesses the unmanaged C++ DebugProcs.dll
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DebugProcs: IFWDisposable
#if DEBUG
		, IDebugReportSink
	{
		private IDebugReport m_DebugReport;
#else
	{
#endif

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new object
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DebugProcs()
			: this(DebugProcs.DefaultTraceListener != null ?
					DebugProcs.DefaultTraceListener.AssertUiEnabled : false)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new object
		/// </summary>
		/// <param name="fShowAssertMsgBox"><c>true</c> to show message box on asserts,
		/// otherwise false</param>
		/// ------------------------------------------------------------------------------------
		public DebugProcs(bool fShowAssertMsgBox)
		{
#if DEBUG
			m_DebugReport = DebugReportClass.Create();
			m_DebugReport.ShowAssertMessageBox(fShowAssertMsgBox);
			m_DebugReport.SetSink(this);
#endif
		}

		#region Internal methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the default trace listener
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static DefaultTraceListener DefaultTraceListener
		{
			get
			{
				foreach (TraceListener listener in Debug.Listeners)
				{
					if (listener is DefaultTraceListener)
						return listener as DefaultTraceListener;
				}

				return null;
			}
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
		~DebugProcs()
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
			Debug.WriteLineIf(!disposing, "****************** DebugProcs 'disposing' is false. ******************");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
#if DEBUG
				if (m_DebugReport != null)
					m_DebugReport.ClearSink();
#endif
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
#if DEBUG
			if (m_DebugReport != null)
			{
					//m_DebugReport.ClearSink();
				Marshal.ReleaseComObject(m_DebugReport);
				m_DebugReport = null;
			}
#endif

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		#region IDebugReportSink Members
#if DEBUG
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Callback method that gets all debug output from unmanaged FieldWorks code.
		/// </summary>
		/// <param name="nReportType">Type of report</param>
		/// <param name="szMsg">Message</param>
		/// ------------------------------------------------------------------------------------
		public virtual void Report(CrtReportType nReportType, string szMsg)
		{
			CheckDisposed();
			Trace.WriteLine(szMsg, nReportType.ToString());
		}

#endif
		#endregion
	}
}
