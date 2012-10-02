//-------------------------------------------------------------------------------------------------
// <copyright file="WixExtractCab.cs" company="Microsoft">
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
// Wrapper class around interop with wixcab.dll to extract files from a cabinet.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Cab
{
	using System;
	using System.Runtime.InteropServices;
	using Microsoft.Tools.WindowsInstallerXml.Cab.Interop;

	/// <summary>
	/// Wrapper class around interop with wixcab.dll to extract files from a cabinet.
	/// </summary>
	public class WixExtractCab : IDisposable
	{
		protected bool closed = false;
		protected bool disposed = false;

		/// <summary>
		/// Creates a cabinet extractor.
		/// </summary>
		public WixExtractCab()
		{
			int err = CabInterop.ExtractCabBegin();
			if (0 != err)
			{
				throw new WixCabExtractionException(new COMException(String.Concat("Failed to begin extracting cabinet, error: ", err), err));
			}
		}

		/// <summary>
		/// Destructor for cabinet extraction.
		/// </summary>
		~WixExtractCab()
		{
			this.Dispose(false);
		}

		/// <summary>
		/// Extracts all the files from a cabinet to a directory.
		/// </summary>
		/// <param name="cabinetFile">Cabinet file to extract from.</param>
		/// <param name="extractDir">Directory to extract files to.</param>
		public void Extract(string cabinetFile, string extractDir)
		{
			if (null == extractDir)
			{
				throw new ArgumentNullException("extractDir");
			}

			if (!extractDir.EndsWith("\\"))
			{
				extractDir = String.Concat(extractDir, "\\");
			}

			int err = CabInterop.ExtractCab(cabinetFile, extractDir);
			if (0 != err)
			{
				throw new WixCabExtractionException(extractDir, new COMException(String.Concat("Failed to extract files from cabinet, error: ", err), err));
			}
			err = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
			if (0 != err)
			{
				throw new WixCabExtractionException(new COMException(String.Concat("Failed to execute cab extract, error: ", err), err));
			}
		}

		/// <summary>
		/// Closes the cabinet being extracted.
		/// </summary>
		public void Close()
		{
			if (this.closed)
			{
				return;
			}

			this.closed = true;
			CabInterop.ExtractCabFinish();

			int err = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
			if (0 != err)
			{
				throw new WixCabExtractionException(new COMException(String.Concat("Failed to close cab extract object, error: ", err), err));
			}
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

			if (!this.closed)
			{
				this.Close(); // if not already closed, do that
			}

			this.disposed = true;
		}
	}
}
