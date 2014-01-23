// Copyright (c) 2005-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: FileModeOverride.cs
// Responsibility: Flex Team

using System;
using System.IO;
using System.Security;
#if __MonoCS__
using Mono.Unix.Native;
#endif

namespace SIL.Utils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Overrides the system File mode permissions on Linux
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class FileModeOverride : IFWDisposable
	{
#if __MonoCS__
		private FilePermissions m_prevMask;
#endif
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overrides the system File permissions with the default permissions of "002"
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FileModeOverride()
#if __MonoCS__
			: this(FilePermissions.S_IWOTH)
#endif
		{
		}

#if __MonoCS__
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overrides the system File permissions with the value passed in FilePermissions
		/// </summary>
		/// <param name="fp">file permissions value</param>
		/// ------------------------------------------------------------------------------------
		public FileModeOverride(FilePermissions fp)
		{
			SetFileCreationMask(fp);
		}
#endif
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// ------------------------------------------------------------------------------------
		~FileModeOverride()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

#if __MonoCS__
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the File creation mode passed in by filePermissions.
		/// This will also set the previous value for m_prevMask used to reset the
		/// FilePermissions during Dispose.
		/// </summary>
		/// <param name="filePermissions">file permissions value</param>
		/// ------------------------------------------------------------------------------------
		private void SetFileCreationMask(FilePermissions filePermissions)
		{
			m_prevMask = Mono.Unix.Native.Syscall.umask(filePermissions);
		}
#endif

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsDisposed { get; private set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check to see if the object has been disposed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// <remarks>Must not be virtual.</remarks>
		/// ------------------------------------------------------------------------------------
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// <param name="disposing"></param>
		/// ------------------------------------------------------------------------------------
		protected virtual void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
#if __MonoCS__
				SetFileCreationMask(m_prevMask);
#endif
			}
			IsDisposed = true;
		}
	}
}
