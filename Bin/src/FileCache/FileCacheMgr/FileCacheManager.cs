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
// File: FileCacheManager.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using SIL.FieldWorks.Tools;

namespace SIL.FieldWorks.Tools.FileCacheMgr
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class FileCacheManager: IDisposable
	{
		private CacheManager m_CacheManager = new CacheManager();

		#region Dispose methods and Finalizer
		/// <summary></summary>
		private bool m_fDisposed = false;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Releases unmanaged resources and performs other cleanup operations before the
		/// <see cref="T:SIL.FieldWorks.Tools.CacheManager"/> is reclaimed by garbage collection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		~FileCacheManager()
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
				m_CacheManager.Dispose();
			}

			// Dispose unmanaged resources here
			m_CacheManager = null;
		}

		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Displays the statistic.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void DisplayStatistic()
		{
			Statistics statistics = m_CacheManager.Statistics;
			Console.WriteLine("File Cache");
			Console.WriteLine("Cache misses: {0}", statistics.Missed);
			Console.WriteLine("Cache hits: {0}", statistics.Hits);
			Console.WriteLine("Cache hits remote: {0}", statistics.RemoteHits);
			Console.WriteLine("Number of objects cached: {0}", m_CacheManager.NumberOfCachedObjects);
			Console.WriteLine("Number of files in cache: {0}", m_CacheManager.NumberOfFiles);
		}

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Resets the statistics.
		/// </summary>
		/// ------------------------------------------------------------------------------------------
		public void ResetStatistics()
		{
			Statistics statistics = m_CacheManager.Statistics;
			statistics.Reset();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Purges the cache.
		/// </summary>
		/// <param name="nMonths">All files not accessed within the last <paramref name="nMonths"/>
		/// will be deleted.</param>
		/// <param name="nDays">All files not accessed within the last <paramref name="nDays"/>
		/// will be deleted.</param>
		/// <param name="fRemote"><c>false</c> to purge local cache, <c>true</c> to purge
		/// remote cache.</param>
		/// ------------------------------------------------------------------------------------
		public void PurgeCache(int nMonths, int nDays, bool fRemote)
		{
			DateTime date = DateTime.Now.AddMonths(-nMonths).AddDays(-nDays);

			if (fRemote)
				m_CacheManager.RemotePurge(date);
			else
				m_CacheManager.Purge(date);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Prints out debug info.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void DebugInfo()
		{
			m_CacheManager.DebugInfo();
		}
	}
}
