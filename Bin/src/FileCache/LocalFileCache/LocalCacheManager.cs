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
// File: LocalCacheManager.cs
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
using System.Security.Cryptography;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace SIL.FieldWorks.Tools
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class LocalCacheManager: MarshalByRefObject, IDisposable
	{
		private MD5CryptoServiceProvider m_md5 = new MD5CryptoServiceProvider();
		private Hashtable m_htCache = new Hashtable();
		private FileManager m_FileManager = new FileManager();

		/// <summary>Statistics about cache usage</summary>
		protected Statistics m_Statistics = new Statistics();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:CacheManager"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public LocalCacheManager(): this(null)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:LocalCacheManager"/> class.
		/// </summary>
		/// <param name="fileCachePath">The file cache path.</param>
		/// <remarks>This overload of the constructor should only be used for testing.</remarks>
		/// ------------------------------------------------------------------------------------
		public LocalCacheManager(string fileCachePath)
		{
			if (fileCachePath != null)
				FileCachePath = fileCachePath;

			m_FileManager.EnsureDirectories();
			Initialize();
		}

		#region Dispose methods and Finalizer
		/// <summary></summary>
		private bool m_fDisposed = false;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Releases unmanaged resources and performs other cleanup operations before the
		/// <see cref="T:SIL.FieldWorks.Tools.CacheManager"/> is reclaimed by garbage collection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		~LocalCacheManager()
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
			}

			// Dispose unmanaged resources here
		}

		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes this instance.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Initialize()
		{
			if (!File.Exists(CacheFileName))
				return;

			IFormatter formatter = new BinaryFormatter();
			using (Stream stream = new FileStream(CacheFileName, FileMode.Open))
			{
				m_htCache = (Hashtable)formatter.Deserialize(stream);
				m_Statistics = (Statistics)formatter.Deserialize(stream);
				stream.Close();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Closes this instance.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Close()
		{
			IFormatter formatter = new BinaryFormatter();
			using (Stream stream = new FileStream(CacheFileName, FileMode.Create))
			{
				formatter.Serialize(stream, m_htCache);
				formatter.Serialize(stream, m_Statistics);
				stream.Close();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clears the cache. Mainly used for testing.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Clear()
		{
			m_htCache.Clear();
			m_Statistics.Reset();
			File.Delete(CacheFileName);
			m_FileManager.Clear();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the directory where the file cache stores the files.
		/// </summary>
		/// <value>The file cache path.</value>
		/// ------------------------------------------------------------------------------------
		public string FileCachePath
		{
			get { return Properties.Settings.Default.FileCachePath; }
			set
			{
				Properties.Settings.Default.FileCachePath = value;
				m_FileManager.EnsureDirectories();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the file where the file cache stores data about cached files.
		/// </summary>
		/// <value>The name of the cache file.</value>
		/// ------------------------------------------------------------------------------------
		private string CacheFileName
		{
			get
			{
				return Path.Combine(Properties.Settings.Default.FileCachePath,
					Properties.Settings.Default.CacheFile);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified files are cached.
		/// </summary>
		/// <param name="handle">The handle, retrieved by a call to <c>GetHash</c></param>
		/// <returns>
		/// 	<c>true</c> if the specified files are cached, <c>false</c> if they are not
		/// cached.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public virtual bool IsCached(string handle)
		{
			return m_htCache.Contains(handle);
		}

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Caches the output file that is the output of the given input files with parameter.
		/// </summary>
		/// <param name="handle">The handle, retrieved by a call to <c>GetHash</c></param>
		/// <param name="outFileNames">The output file names.</param>
		/// ------------------------------------------------------------------------------------------
		public virtual void CacheFile(string handle, params string[] outFileNames)
		{
			CacheFile(handle, outFileNames, outFileNames);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Caches the files.
		/// </summary>
		/// <param name="handle">The handle, retrieved by a call to <c>GetHash</c></param>
		/// <param name="origFileNames">The orig file names.</param>
		/// <param name="outFileNames">The out file names.</param>
		/// ------------------------------------------------------------------------------------
		public void CacheFile(string handle, string[] origFileNames, string[] outFileNames)
		{
			if (!m_htCache.Contains(handle))
			{
				m_htCache.Add(handle, m_FileManager.CacheFile(handle, origFileNames, outFileNames));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a cached file.
		/// </summary>
		/// <param name="handle">The handle, retrieved by a call to <c>GetHash</c></param>
		/// <returns>The filenames of the cached files, or <c>null</c> if file is not cached.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public virtual CachedFile[] GetCachedFiles(string handle)
		{
			if (!m_htCache.Contains(handle))
			{
				m_Statistics.Missed++;
				return null;
			}
			m_Statistics.Hits++;
			return ((CacheFileInfo)m_htCache[handle]).Files;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Calculates the hash values for the given files.
		/// </summary>
		/// <param name="fileNames">The file names.</param>
		/// <returns>The hash value, or <c>null</c> if none of the files exists.</returns>
		/// ------------------------------------------------------------------------------------
		public string GetHash(params string[] fileNames)
		{
			return GetHash(null, fileNames);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Calculates the hash values for the given files.
		/// </summary>
		/// <param name="parameters">The parameters.</param>
		/// <param name="fileNames">The file names.</param>
		/// <returns>The hash value, or <c>null</c> if none of the files exists.</returns>
		/// ------------------------------------------------------------------------------------
		public string GetHash(string parameters, string[] fileNames)
		{
			ArrayList hashs = new ArrayList();
			foreach (string fileName in fileNames)
			{
				if (File.Exists(fileName))
				{
					hashs.AddRange(ComputeHash(fileName));
				}
			}

			if (hashs.Count == 0)
				return null;

			if (parameters != null)
			{
				UnicodeEncoding enc = new UnicodeEncoding();
				hashs.AddRange(m_md5.ComputeHash(enc.GetBytes(parameters)));
			}

			return Convert.ToBase64String(m_md5.ComputeHash((byte[])hashs.ToArray(typeof(byte))));
		}

		/// ---------------------------------------------------------------------------------------
		/// <summary>
		/// Computes the hash value for a file.
		/// </summary>
		/// <param name="fileName">Name of the file.</param>
		/// <returns>Hash value</returns>
		/// ---------------------------------------------------------------------------------------
		private byte[] ComputeHash(string fileName)
		{
			using (PeFileProcessor fileProcessor = new PeFileProcessor(fileName))
			{
				byte[] buffer = m_md5.ComputeHash(fileProcessor.Stream);
				if (Properties.Settings.Default.Verbose)
					Console.WriteLine("{0}: {1}", fileName, Convert.ToBase64String(buffer));
				return buffer;
			}
		}

		#region Statistic related methods
		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the statistics.
		/// </summary>
		/// <value>The statistics.</value>
		/// ------------------------------------------------------------------------------------------
		public Statistics Statistics
		{
			get { return m_Statistics; }
		}

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the number of cached objects.
		/// </summary>
		/// <value>The number of cached objects.</value>
		/// ------------------------------------------------------------------------------------------
		public int NumberOfCachedObjects
		{
			get { return m_htCache.Count; }
		}

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the number of files.
		/// </summary>
		/// <value>The number of files.</value>
		/// ------------------------------------------------------------------------------------------
		public int NumberOfFiles
		{
			get { return m_FileManager.NumberOfFiles; }
		}

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Increments the cache miss counter. This must be done manually if the IsCached property
		/// is used.
		/// </summary>
		/// ------------------------------------------------------------------------------------------
		public void AddCacheMiss()
		{
			m_Statistics.Missed++;
		}
		#endregion

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Purges any cached items older than the specified date.
		/// </summary>
		/// <param name="purgeOlderThan">Purge date.</param>
		/// ------------------------------------------------------------------------------------------
		public void Purge(DateTime purgeOlderThan)
		{
			ArrayList toRemove = new ArrayList();
			foreach (DictionaryEntry entry in m_htCache)
			{
				if (((CacheFileInfo)entry.Value).LastAccessed < purgeOlderThan)
					toRemove.Add(entry);
			}

			foreach (DictionaryEntry entry in toRemove)
			{
				m_htCache.Remove(entry.Key);
				m_FileManager.RemoveFiles(entry.Value as CacheFileInfo);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Prints out debug information
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void DebugInfo()
		{
			foreach (DictionaryEntry entry in m_htCache)
			{
				CacheFileInfo info = entry.Value as CacheFileInfo;
				Console.WriteLine("{0}, last accessed: {1}", entry.Key,
					info.LastAccessed);
				foreach (CachedFile file in info.DebugFiles)
					Console.WriteLine("\t{0} ({1})", file.OriginalName,
						Path.GetFileName(file.CachedFileName));

				Console.WriteLine();
			}
		}
	}
}
