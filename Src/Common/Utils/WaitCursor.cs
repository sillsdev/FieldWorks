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
// File: WaitCursor.cs
// Responsibility: Eberhard Beilharz
// Last reviewed:
//
// <remarks>
// Helper class to display a wait cursor
// </remarks>
// --------------------------------------------------------------------------------------------

using System;
using System.Windows.Forms;
using System.Diagnostics;

namespace SIL.FieldWorks.Common.Utils
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
	public class WaitCursor : IFWDisposable
	{
		private Cursor m_oldCursor;
		private Control m_parent;
		private bool m_fOldWaitCursor;
		private delegate void VoidMethodWithBool(bool f);

		// We used to keep track of nested wait cursor calls.
		// This didn't work as expected because the variable was decremented
		// in Dispose() which gets called directly (at the end of the using block)
		// and from the finalizer.
		// We can always restore the old cursor because if we are nested
		// the parent.Cursor is already the wait cursor, so we can restore that.

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

			if (m_parent != null || showBusyCursor)
			{
				m_oldCursor = m_parent.Cursor;
				m_parent.Cursor = showBusyCursor ? Cursors.AppStarting : Cursors.WaitCursor;
			}
			else
			{
				m_fOldWaitCursor = Application.UseWaitCursor;
				Application.UseWaitCursor = true;
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
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
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

			if (m_oldCursor != null)
				m_parent.Cursor = m_oldCursor;
			else
				Application.UseWaitCursor = m_fOldWaitCursor;
		}
	}
}
