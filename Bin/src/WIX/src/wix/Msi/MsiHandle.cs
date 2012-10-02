//-------------------------------------------------------------------------------------------------
// <copyright file="MsiHandle.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//
//    The use and distribution terms for this software are covered by the
//    Common Public License 1.0 (http://opensource.org/licenses/cpl.php)
//    which can be found in the file CPL.TXT at the root of this distribution.
//    By using this software in any fashion, you are agreeing to be bound by
//    the terms of this license.
//
//    You must not remove this notice, or any other, from this software.
// </copyright>
//
// <summary>
// Wrapper for MSI API handles.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Msi
{
	using System;
	using Microsoft.Tools.WindowsInstallerXml.Msi.Interop;

	/// <summary>
	/// Wrapper class for MSI handle.
	/// </summary>
	public class MsiHandle : IDisposable
	{
		protected bool disposed = false;
		protected IntPtr handle = IntPtr.Zero;

		/// <summary>
		/// MSI handle destructor.
		/// </summary>
		~MsiHandle()
		{
			this.Dispose(false);
		}

		/// <summary>
		/// Provides access to the internal handle value.
		/// </summary>
		/// <value>The value of the internal handle.</value>
		public IntPtr InternalHandle
		{
			get
			{
				return this.handle;
			}
		}

		/// <summary>
		/// Closes the MSI handle.
		/// </summary>
		public virtual void Close()
		{
			if (IntPtr.Zero == this.handle)
			{
				throw new ArgumentNullException();   // TODO: come up with a real exception to throw
			}

			uint er = MsiInterop.MsiCloseHandle(this.handle);
			if (0 != er)
			{
				throw new ArgumentNullException();   // TODO: come up with a real exception to throw
			}
			this.handle = IntPtr.Zero;
		}

		/// <summary>
		/// Closes the handle when the Handle object is being disposed.
		/// </summary>
		void IDisposable.Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Closes the handle before the object is disposed or it goes out of scope.
		/// </summary>
		/// <param name="disposing">Should be true if being called from Dispose() and false if called from the destructor.</param>
		private void Dispose(bool disposing)
		{
			if (this.disposed)
			{
				return;
			}

			if (disposing)
			{
			}

			if (IntPtr.Zero != this.handle)
			{
				this.Close();   // if not already closed, do that
			}
			this.disposed = true;
		}
	}
}
