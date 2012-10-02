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
// File: CacheManager.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Reflection;
using System.Security.Permissions;
using System.Text;
using SIL.FieldWorks.Tools.FileCache;

namespace SIL.FieldWorks.Tools
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class CacheManager: LocalCacheManager
	{
		private RemoteCacheManager m_remoteCacheMgr;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:CacheManager"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public CacheManager()
		{
			if (!Properties.Settings.Default.UseFileCache)
				return;

			// Register the remote class
			if (Properties.Settings.Default.UseRemoteCache)
			{
				if (RemotingConfiguration.IsWellKnownClientType(typeof(RemoteCacheManager)) == null)
				{
					// save temporary config file to configure the type
					string config = string.Format("<?xml version=\"1.0\" encoding=\"utf-8\" ?>" +
								"<configuration><system.runtime.remoting><application><client>" +
								"<wellknown  type=\"SIL.FieldWorks.Tools.FileCache.RemoteCacheManager, RemoteFileCache\" " +
								"url=\"tcp://{0}:8700/FileCache.rem\"/></client>" +
								"<channels><channel ref=\"tcp\" port=\"0\"><serverProviders><formatter ref=\"binary\" typeFilterLevel=\"Full\" />" +
								"</serverProviders><clientProviders><formatter ref=\"binary\" typeFilterLevel=\"Full\"/></clientProviders>" +
								"</channel></channels></application></system.runtime.remoting></configuration>",
								Properties.Settings.Default.RemoteHost);
					string tempConfigFile = Path.GetTempFileName();
					StreamWriter writer = new StreamWriter(tempConfigFile);
					writer.Write(config);
					writer.Close();

					RemotingConfiguration.Configure(tempConfigFile, true);

					// now delete our temporary config file
					File.Delete(tempConfigFile);
				}

				m_remoteCacheMgr = new RemoteCacheManager();
			}
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
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				// Dispose managed resources here.
				if (m_remoteCacheMgr != null)
				{
					try
					{
						m_remoteCacheMgr.Close();
					}
					catch (Exception e)
					{
						Console.WriteLine("Got exception accessing remote cache: " + e.Message);
					}
				}
			}

			// Dispose unmanaged resources here
			m_remoteCacheMgr = null;

			base.Dispose(disposing);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified files are cached.
		/// </summary>
		/// <param name="handle">The handle, retrieved by a call to <c>GetHash</c></param>
		/// <returns>
		/// 	<c>true</c> if the specified files have changed, otherwise, <c>false</c>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override bool IsCached(string handle)
		{
			if (!Properties.Settings.Default.UseFileCache)
				return false;

			bool fCached = base.IsCached(handle);
			if (!fCached && Properties.Settings.Default.UseRemoteCache)
			{
				try
				{
					fCached = m_remoteCacheMgr.IsCached(handle);
				}
				catch (Exception e)
				{
					Console.WriteLine("Got exception accessing remote cache: " + e.Message);
				}
			}
			return fCached;
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a cached file.
		/// </summary>
		/// <param name="handle">The handle, retrieved by a call to <c>GetHash</c></param>
		/// <returns>
		/// The filenames of the cached files, or <c>null</c> if file is not cached.
		/// </returns>
		/// -------------------------------------------------------------------------------------
		public override CachedFile[] GetCachedFiles(string handle)
		{
			if (!Properties.Settings.Default.UseFileCache)
				return null;

			CachedFile[] cachedFiles = base.GetCachedFiles(handle);
			if (cachedFiles == null && Properties.Settings.Default.UseRemoteCache)
			{
				try
				{
					cachedFiles = m_remoteCacheMgr.GetCachedFiles(handle);
					if (cachedFiles != null)
					{
						// Cache these files locally so that it will be faster next time
						string tempDir = Path.GetTempPath();
						ArrayList outFiles = new ArrayList(cachedFiles.Length);
						foreach (CachedFile cacheFile in cachedFiles)
						{
							cacheFile.CopyTo(tempDir);
							outFiles.Add(Path.Combine(tempDir, cacheFile.OriginalName));
						}
						base.CacheFile(handle, (string[])outFiles.ToArray(typeof(string)));

						foreach (string toDel in outFiles)
							File.Delete(toDel);

						// Adjust the statistics - it's neither a miss nor a local hit, but a remote hit.
						m_Statistics.Missed--;
						m_Statistics.Hits--;
						m_Statistics.RemoteHits++;

						// Return files from local cache - otherwise get a remoting exception
						return base.GetCachedFiles(handle);
					}
				}
				catch (Exception e)
				{
					Console.WriteLine("Got exception accessing remote cache: " + e.Message);
				}
			}
			return cachedFiles;
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Caches the output file that is the output of the given input files with parameter.
		/// </summary>
		/// <param name="handle">The handle, retrieved by a call to <c>GetHash</c></param>
		/// <param name="outFileNames">The output file names.</param>
		/// -------------------------------------------------------------------------------------
		public override void CacheFile(string handle, params string[] outFileNames)
		{
			if (!Properties.Settings.Default.UseFileCache)
				return;

			base.CacheFile(handle, outFileNames);

			if (Properties.Settings.Default.UseRemoteCache)
			{
				try
				{
					ArrayList outFiles = new ArrayList();
					foreach (string fileName in outFileNames)
					{
						FileStream stream = File.OpenRead(fileName);
						outFiles.Add(stream);
					}
					m_remoteCacheMgr.CacheFile(handle, (FileStream[])outFiles.ToArray(typeof(FileStream)));
				}
				catch (Exception e)
				{
					Console.WriteLine("Got exception accessing remote cache: " + e.Message);
				}
			}
		}

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Purges any cached items older than the specified date on the remote cache.
		/// </summary>
		/// <param name="purgeOlderThan">Purge date.</param>
		/// ------------------------------------------------------------------------------------------
		public void RemotePurge(DateTime purgeOlderThan)
		{
			if (Properties.Settings.Default.UseRemoteCache)
			{
				try
				{
					m_remoteCacheMgr.Purge(purgeOlderThan);
				}
				catch (Exception e)
				{
					Console.WriteLine("Got exception accessing remote cache: " + e.Message);
				}
			}
		}

	}
}
