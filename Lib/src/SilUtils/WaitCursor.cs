// --------------------------------------------------------------------------------------------
// Copyright (c) 2002-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: WaitCursor.cs
// Responsibility: Eberhard Beilharz
// Last reviewed:
//
// <remarks>
// Helper class to display a wait cursor
// </remarks>
// --------------------------------------------------------------------------------------------
//#define DEBUG_WAITCURSOR
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;

namespace SIL.Utils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Display a wait cursor while object exists.
	/// </summary>
	/// <example>
	/// Typical usage:
	/// <code>
	/// using(new WaitCursor())
	/// {
	///		// do something
	/// }
	/// </code>
	/// This displays the wait cursor inside of the using block.
	/// </example>
	/// ----------------------------------------------------------------------------------------
	[SuppressMessage("Gendarme.Rules.Correctness", "DisposableFieldsShouldBeDisposedRule",
		Justification="m_oldCursor and m_parent are references; we don't create the object")]
	public class WaitCursor : IDisposable
	{
#if DEBUG_WAITCURSOR
		static int s_depth = 0;		// counter used for debugging/tracing nested uses.
#endif
		private Cursor m_oldCursor;
		private Control m_parent;
		private bool m_fOldWaitCursor;
		private delegate void VoidMethodWithBool(bool f);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:WaitCursor"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public WaitCursor() : this(null, false)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="parent">Parent control</param>
		/// ------------------------------------------------------------------------------------
		public WaitCursor(Control parent) : this(parent, false)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="parent">Parent control (can be null if displaying a normal wait cursor)</param>
		/// <param name="showBusyCursor">True to show a busy cursor (arrow with an
		/// hourglass) instead of the hourglass by itself.</param>
		/// ------------------------------------------------------------------------------------
		public WaitCursor(Control parent, bool showBusyCursor)
		{
			if (parent == null && showBusyCursor)
				throw new ArgumentException("Can't show a busy cursor without having a parent control");
			m_parent = parent;
			SetWaitCursor(showBusyCursor);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the wait cursor.
		/// </summary>
		/// <param name="showBusyCursor">set to <c>true</c> to display the busy cursor,
		/// set to <c>false</c> to display the normal wait cursor.</param>
		/// ------------------------------------------------------------------------------------
		private void SetWaitCursor(bool showBusyCursor)
		{
			if (m_parent != null && m_parent.InvokeRequired)
			{
				m_parent.Invoke(new VoidMethodWithBool(SetWaitCursor), showBusyCursor);
				return;
			}

			if (m_parent != null)
			{
				m_oldCursor = m_parent.Cursor;
				m_parent.Cursor = showBusyCursor ? Cursors.AppStarting : Cursors.WaitCursor;
			}
			else
			{
				m_fOldWaitCursor = Application.UseWaitCursor;
				Application.UseWaitCursor = true;
				// A comment on the web indicates that the following triggers the cursor to actually change.
				// This appears to be true.  (A related post used the Win32 API SendMessage to achieve this.)
				Cursor.Position = Cursor.Position;
			}
#if DEBUG_WAITCURSOR
			++s_depth;
			Debug.WriteLine(String.Format("{0}: WaitCursor.Create({1}): m_parent={2}; m_oldCursor={3}; m_fOldWaitCursor={4}",
				s_depth, showBusyCursor, m_parent, m_oldCursor, m_fOldWaitCursor));
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
		~WaitCursor()
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
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				Restore();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_parent = null;
			m_oldCursor = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Restore the previous cursor
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Restore()
		{
			CheckDisposed();

			if (m_parent != null && m_parent.InvokeRequired)
			{
				m_parent.Invoke(new MethodInvoker(Restore));
				return;
			}

#if DEBUG_WAITCURSOR
			Debug.WriteLine(String.Format("{0}: WaitCursor.Dispose(): m_parent={1}; m_oldCursor={2}; m_fOldWaitCursor={3}",
				s_depth, m_parent, m_oldCursor, m_fOldWaitCursor));
			--s_depth;
#endif
			if (m_oldCursor != null)
			{
				m_parent.Cursor = m_oldCursor;
			}
			else
			{
				Application.UseWaitCursor = m_fOldWaitCursor;
				// A comment on the web indicates that the following helps trigger the cursor to actually change.
				// This appears to be true.  (A related post used the Win32 API SendMessage to achieve this.)
				Cursor.Position = Cursor.Position;
			}
		}
	}
}
