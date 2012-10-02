// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2006' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: RemoteCacheManager.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Runtime.Remoting;

namespace SIL.FieldWorks.Tools.FileCache
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Accesses a remote file cache
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class RemoteCacheManager : MarshalByRefObject
	{
		private LocalCacheManager m_CacheManager;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:RemoteCacheManager"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public RemoteCacheManager()
		{
		}

		#region Dispose methods and Finalizer
		/// <summary></summary>
		private bool m_fDisposed = false;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Releases unmanaged resources and performs other cleanup operations before the
		/// <see cref="T:SIL.FieldWorks.Tools.RemoteCacheManager"/> is reclaimed by garbage
		/// collection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		~RemoteCacheManager()
		{
			Dispose(false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting
		/// unmanaged resources.
		/// </summary>
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
		/// Executes in two distinct scenarios.
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing">if set to <c>true</c> this method is called from the
		/// Dispose() method, if set to <c>false</c> it's called from finalizer.</param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		protected virtual void Dispose(bool disposing)
		{
			if (m_fDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				Close();
				if (m_CacheManager != null)
					m_CacheManager.Dispose();
			}

			// Dispose unmanaged resources here
			m_CacheManager = null;
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the cache manager.
		/// </summary>
		/// <value>The cache manager.</value>
		/// ------------------------------------------------------------------------------------
		private LocalCacheManager CacheManager
		{
			get
			{
				if (m_CacheManager == null)
					m_CacheManager = new LocalCacheManager();
				return m_CacheManager;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Closes this instance.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Close()
		{
			CacheManager.Close();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified files are cached.
		/// </summary>
		/// <param name="handle">The handle, retrieved by a call to <c>GetHash</c></param>
		/// <returns>
		/// 	<c>true</c> if the specified files have changed or if they were never
		/// cached before; otherwise, <c>false</c>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public bool IsCached(string handle)
		{
			return CacheManager.IsCached(handle);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Caches the output file that is the output of the given input files with parameter.
		/// </summary>
		/// <param name="handle">The handle, retrieved by a call to <c>GetHash</c></param>
		/// <param name="outFileStreams">The output file streams.</param>
		/// ------------------------------------------------------------------------------------
		public void CacheFile(string handle, params FileStream[] outFileStreams)
		{
			ArrayList outFileNames = new ArrayList(outFileStreams.Length);
			ArrayList origFileNames = new ArrayList(outFileStreams.Length);
			const int kBufSize = 4096; // internal stream buffer size is 4k
			byte[] buffer = new byte[kBufSize];
			foreach (FileStream reader in outFileStreams)
			{
				string outFile = Path.GetTempFileName();
				using (FileStream writer = new FileStream(outFile, FileMode.Create))
				{
					for (int nCount = reader.Read(buffer, 0, kBufSize); nCount > 0;
						nCount = reader.Read(buffer, 0, kBufSize))
					{
						writer.Write(buffer, 0, nCount);
					}
					writer.Close();
				}
				reader.Close();
				outFileNames.Add(outFile);
				origFileNames.Add(reader.Name);
			}

			CacheManager.CacheFile(handle, (string[])origFileNames.ToArray(typeof(string)),
				(string[])outFileNames.ToArray(typeof(string)));
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a cached file.
		/// </summary>
		/// <param name="handle">The handle, retrieved by a call to <c>GetHash</c></param>
		/// <returns>The filenames of the cached files, or <c>null</c> if file is not cached.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public CachedFile[] GetCachedFiles(string handle)
		{
			ArrayList remoteCachedFiles = new ArrayList();
			foreach (CachedFile cachedFile in CacheManager.GetCachedFiles(handle))
			{
				FileStream stream = File.OpenRead(cachedFile.CachedFileName);
				remoteCachedFiles.Add(new RemoteCachedFile(cachedFile.OriginalName,
					cachedFile.CachedFileName, stream));
			}

			return (CachedFile[])remoteCachedFiles.ToArray(typeof(RemoteCachedFile));
		}

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Purges any cached items older than the specified date.
		/// </summary>
		/// <param name="purgeOlderThan">Purge date.</param>
		/// ------------------------------------------------------------------------------------------
		public void Purge(DateTime purgeOlderThan)
		{
			CacheManager.Purge(purgeOlderThan);
		}
	}
}
