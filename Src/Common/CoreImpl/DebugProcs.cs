// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DebugProcs.cs
// Responsibility: FW Team

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;

namespace SIL.CoreImpl
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Debugging helper methods. Accesses the unmanaged C++ DebugProcs.dll
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DebugProcs : IFWDisposable
#if DEBUG
		, IDebugReportSink
	{
		private IDebugReport m_DebugReport;
#else
	{
#endif

		private const int MaxLineLength = 60;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new object
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DebugProcs()
		{
#if DEBUG
			m_DebugReport = DebugReportClass.Create();
			m_DebugReport.SetSink(this);
#endif
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
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed { get; private set;}

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
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
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

			IsDisposed = true;
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Managed assert method. This method gets called when an assert fails
		/// in unmanaged code.
		/// </summary>
		/// <param name="expression">the expression of the assertion that failed</param>
		/// <param name="filename">the filename of the failed assertion</param>
		/// <param name="nLine">the line number of the failed assertion</param>
		/// ------------------------------------------------------------------------------------
		public virtual void AssertProc(string expression, string filename, int nLine)
		{
			Debug.Fail(GetMessage(expression, filename, nLine));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the message.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected static string GetMessage(string expression, string filePath, int nLine)
		{
			var bldr = new StringBuilder();

			// Line 1: box intro line
			bldr.AppendLine("Assertion failed!");
			bldr.AppendLine();

			// Line 2: program line
			bldr.Append("Program: ");

			// Note that we can't use Assembly.GetEntryAssembly() because that returns null
			// since this method gets called from unmanaged code!
			var exeName = new StringBuilder(255);
			Win32.GetModuleFileName(IntPtr.Zero, exeName, exeName.Capacity);

			if (exeName.Length > MaxLineLength + 9) // "Program: ".Length
			{
				bldr.Append("...");
				exeName.Remove(0, exeName.Length - MaxLineLength - 9);
			}
			bldr.AppendLine(exeName.ToString());

			// Line 3: file line
			bldr.Append("File: ");
			if (filePath.Length > MaxLineLength - 6) // "File: ".Length
			{
				// path doesn't fit in the line
				const int availLength = MaxLineLength - 6;

				// Determine where to put the ...
				var nameOnly = Path.DirectorySeparatorChar + Path.GetFileName(filePath);
				if ((availLength - availLength / 3) < (filePath.Length - nameOnly.Length) &&
					availLength / 3 > nameOnly.Length)
				{
					// path too long. Using first part of path and the filename string
					bldr.Append(filePath.Substring(0, availLength - 3 - nameOnly.Length));
					bldr.Append("...");
					bldr.AppendLine(nameOnly);
				}
				else if ((availLength - availLength/3) > (filePath.Length - nameOnly.Length))
				{
					// path is smaller. keeping full path and putting ... in the
					// middle of filename
					bldr.Append(filePath.Substring(0, availLength - 3 - nameOnly.Length/2)); // "...".Length
					bldr.Append("...");
					bldr.AppendLine(nameOnly.Substring(nameOnly.Length/2));
				}
				else
				{
					// both path and filename are long. Using first part of path. Using first
					// and last part of filename
					bldr.Append(filePath.Substring(0, availLength - availLength/3 - 3)); // "...".Length
					bldr.Append("...");
					bldr.Append(nameOnly.Substring(1, availLength/6 - 1));
					bldr.Append("...");
					bldr.AppendLine(nameOnly.Substring(nameOnly.Length - availLength/6 + 2));
				}
			}
			else
				bldr.AppendLine(filePath);

			// Line 4: line line
			bldr.AppendFormat("Line: {0}", nLine);
			bldr.AppendLine();
			bldr.AppendLine();

			// Line 5: message line
			bldr.Append("Expression: ");
			bldr.AppendLine(expression);
			bldr.AppendLine();

			// Line 6, 7: info line
			bldr.AppendLine("For information on how your program can cause an assertion");
			bldr.AppendLine("failure, see the Visual C++ documentation on asserts");
			bldr.AppendLine();

			// Line 8: help line
			bldr.Append("(Press Retry to debug the application - JIT must be enabled)");
			return bldr.ToString();
		}
#endif
		#endregion

	}
}
