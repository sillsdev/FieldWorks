// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2005, SIL International. All Rights Reserved.
// <copyright from='2005' to='2005' company='SIL International'>
//		Copyright (c) 2005, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FileModeOverride.cs
// Responsibility: Flex Team
// ---------------------------------------------------------------------------------------------
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
		private FilePermissions m_prevMask;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overrides the system File permissions with the default permissions of "002"
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FileModeOverride() : this(FilePermissions.S_IWOTH)
		{
		}

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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// ------------------------------------------------------------------------------------
		~FileModeOverride()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

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
				#if __MonoCS__
				m_prevMask = Mono.Unix.Native.Syscall.umask(filePermissions);
				#endif
		}

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
				SetFileCreationMask(m_prevMask);
			}
			IsDisposed = true;
		}
	}
}
