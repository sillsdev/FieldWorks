//-------------------------------------------------------------------------------------------------
// <copyright file="WixCreateCab.cs" company="Microsoft">
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
// Wrapper class around interop with wixcab.dll to compress files into a cabinet.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Cab
{
	using System;
	using System.Runtime.InteropServices;
	using Microsoft.Tools.WindowsInstallerXml.Cab.Interop;

	/// <summary>
	/// Compression level to use when creating cabinet.
	/// </summary>
	public enum CompressionLevel
	{
		/// <summary>Use no compression.</summary>
		None,
		/// <summary>Use low compression.</summary>
		Low,
		/// <summary>Use medium compression.</summary>
		Medium,
		/// <summary>Use high compression.</summary>
		High,
		/// <summary>Use ms-zip compression.</summary>
		Mszip
	}

	/// <summary>
	/// Wrapper class around interop with wixcab.dll to compress files into a cabinet.
	/// </summary>
	public class WixCreateCab : IDisposable
	{
		protected IntPtr handle = IntPtr.Zero;
		protected bool disposed = false;

		/// <summary>
		/// Creates a cabinet.
		/// </summary>
		/// <param name="cabName">Name of cabinet to create.</param>
		/// <param name="cabDir">Directory to create cabinet in.</param>
		/// <param name="maxSize">Maximum size of cabinet.</param>
		/// <param name="maxThresh">Maximum threshold for each cabinet.</param>
		/// <param name="compressionLevel">Level of compression to apply.</param>
		public WixCreateCab(string cabName, string cabDir, int maxSize, int maxThresh, CompressionLevel compressionLevel)
		{
			int error = CabInterop.CreateCabBegin(cabName, cabDir, (uint)maxSize, (uint)maxThresh, (uint)compressionLevel, out this.handle);

			if (0 != error)
			{
				throw new WixCabCreationException(error);
			}
		}

		/// <summary>
		/// Destructor for cabinet creation.
		/// </summary>
		~WixCreateCab()
		{
			this.Dispose(false);
		}

		/// <summary>
		/// Adds an array of files to the cabinet.
		/// </summary>
		/// <param name="filePaths">Path to files to add to cabinet.</param>
		/// <param name="fileIds">Identifiers for all files to add to cabinet.</param>
		public void AddFiles(string[] filePaths, string[] fileIds)
		{
			int error = CabInterop.CreateCabAddFiles(filePaths, fileIds, (uint) filePaths.Length, this.handle);

			if (0 != error)
			{
				throw new WixCabCreationException(error);
			}
		}

		/// <summary>
		/// Adds an array of files to the cabinet.
		/// </summary>
		/// <param name="filePaths">Path to files to add to cabinet.</param>
		/// <param name="fileIds">Identifiers for all files to add to cabinet.</param>
		/// <param name="batchAdd">Flag to add the files one at a time or in a batch.</param>
		public void AddFiles(string[] filePaths, string[] fileIds, bool batchAdd)
		{
			if (batchAdd)
			{
				this.AddFiles(filePaths, fileIds);
				return;
			}

			for (int i = 0; i < filePaths.Length; i++)
			{
				int error = CabInterop.CreateCabAddFile(filePaths[i], fileIds[i], this.handle);

				if (0 != error)
				{
					throw new WixCabCreationException(filePaths[i], error);
				}
			}
		}

		/// <summary>
		/// Closes the cabinet being created.
		/// </summary>
		public void Close()
		{
			if (IntPtr.Zero == this.handle)
			{
				throw new ArgumentNullException();
			}

			int error = CabInterop.CreateCabFinish(this.handle);
			if (0 != error)
			{
				throw new WixCabCreationException(error);
			}
			this.handle = IntPtr.Zero;
		}

		/// <summary>
		/// Implements IDisposable interface.
		/// </summary>
		void IDisposable.Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Destroys the unmanaged objects in object.
		/// </summary>
		/// <param name="disposing">Flag if called from garbage collector or denstructor</param>
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
				this.Close(); // if not already closed, do that
			}

			this.disposed = true;
		}
	}
}
