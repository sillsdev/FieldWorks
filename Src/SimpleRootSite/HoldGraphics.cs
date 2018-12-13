// Copyright (c) 2004-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary>
	/// Wraps the (un)initialization of the graphics object
	/// </summary>
	/// <example>
	/// REQUIRED usage:
	/// using(new HoldGraphics(this)) // this initializes the graphics object
	/// {
	///		doStuff();
	/// } // this uninitializes the graphics object
	/// </example>
	public class HoldGraphics : IDisposable
	{
		private SimpleRootSite m_parent;

		/// <summary />
		/// <param name="parent">Containing rootsite</param>
		public HoldGraphics(SimpleRootSite parent)
		{
			if (parent.Disposing || parent.IsDisposed)
			{
				return; // don't do anything if the parent is disposing or already disposed
			}
			m_parent = parent;
			m_parent.InitGraphics();
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
		~HoldGraphics()
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
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				// Dispose managed resources here.
				// The previous comment was not correct, as this code was indeed being executed when
				// the parent was null.  A DestroyHandle has been added to help with the re-entrant processing.
				Debug.Assert(m_parent != null && !m_parent.IsDisposed && !m_parent.Disposing);

				m_parent?.UninitGraphics();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_parent = null;

			IsDisposed = true;
		}
	}
}