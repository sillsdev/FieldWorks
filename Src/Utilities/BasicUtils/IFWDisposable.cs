// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: IFWDisposable.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;

namespace SIL.Utils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Simple interface to extend the IDisposable interface by adding the IsDisposed
	/// and CheckDisposed properties.  These methods exist so that we can make sure
	/// that objects are being used only while they are valid, not disposed.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IFWDisposable : IDisposable
	{
		/// <summary>
		/// Add the public property for knowing if the object has been disposed of yet
		/// </summary>
		bool IsDisposed { get;}

		/// <summary>
		/// This method throws an ObjectDisposedException if IsDisposed returns
		/// true.  This is the case where a method or property in an object is being
		/// used but the object itself is no longer valid.
		///
		/// This method should be added to all public properties and methods of this
		/// object and all other objects derived from it (extensive).
		/// </summary>
		void CheckDisposed();
		// Sample implementation:
		// {
		//	if (IsDisposed)
		//		throw new ObjectDisposedException("ObjectName",
		//			"This object is being used after it has been disposed: this is an Error.");
		// }
	}

	/// <summary>
	/// base class for helper classes who don't want to copy the same basic code
	/// trying to implement IFWDisposable
	/// </summary>
	public abstract class FwDisposableBase : IFWDisposable
	{
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
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed
		{
			get; private set;
		}

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~FwDisposableBase()
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
			Debug.WriteLineIf(!disposing, "******************* Missing Dispose() call for " + GetType().Name + " ******************");

			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				DisposeManagedResources();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			DisposeUnmanagedResources();
			IsDisposed = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Override to dispose managed resources.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void DisposeManagedResources()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Override to dispose unmanaged resources.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void DisposeUnmanagedResources()
		{
		}

		#endregion IDisposable & Co. implementation
	}
}
