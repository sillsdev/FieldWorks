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
// File: FileCacheTests.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using NUnit.Framework;

namespace SIL.FieldWorks.Tools
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class FileCacheTests
	{
		#region Member variables
		private LocalCacheManager m_mgr;
		#endregion

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the temp file.
		/// </summary>
		/// <param name="outFileName">Name of the out file.</param>
		/// ------------------------------------------------------------------------------------
		private static void CreateTempFile(string outFileName)
		{
			using (StreamWriter writer = new StreamWriter(outFileName))
			{
				Random r = new Random();
				for (int i = 0; i < 50; i++)
					writer.Write(r.Next(255));
				writer.Close();
			}
		}
		#endregion

		#region Test setup/teardown
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the test fixture.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public void FixtureSetup()
		{
			m_mgr = new LocalCacheManager(Path.Combine(Path.GetTempPath(), "FileCache"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fixtures the teardown.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureTearDown]
		public void FixtureTeardown()
		{
			m_mgr.Close();
			m_mgr.Clear();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the tests
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public void Setup()
		{
			m_mgr.Initialize();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shutsdown the tests
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public void TearDown()
		{
			m_mgr.Close();
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that GetHash returns <c>null</c> if source file doesn't exist.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetHash_NonexistingFile()
		{
			Assert.IsNull(m_mgr.GetHash(Path.GetRandomFileName()));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that GetHash returns <c>null</c> if source file doesn't exist.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetHash_NonexistingFileWithParams()
		{
			Assert.IsNull(m_mgr.GetHash("bla bla bla", new string[] {Path.GetRandomFileName() }));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests caching a file that is in the cache
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void CacheFile_InCache()
		{
			string outFileName = Path.GetTempFileName();
			string srcFileName = Path.GetTempFileName();

			try
			{
				CreateTempFile(outFileName);
				string outHash = m_mgr.GetHash(outFileName);

				CreateTempFile(srcFileName);
				string handle = m_mgr.GetHash(srcFileName);

				m_mgr.CacheFile(handle, outFileName);
				CachedFile[] resFileName = m_mgr.GetCachedFiles(handle);

				Assert.AreEqual(1, resFileName.Length);
				Assert.AreEqual(outHash, m_mgr.GetHash(resFileName[0].CachedFileName));
				Assert.AreEqual(Path.GetFileName(outFileName), resFileName[0].OriginalName);
			}
			finally
			{
				File.Delete(outFileName);
				File.Delete(srcFileName);
			}
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests retrieving a file from the cache that isn't in the cache.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void GetCachedFile_CacheMiss()
		{
			string srcFileName = Path.GetTempFileName();

			try
			{
				CreateTempFile(srcFileName);
				Assert.IsNull(m_mgr.GetCachedFiles(m_mgr.GetHash(srcFileName)));
			}
			finally
			{
				File.Delete(srcFileName);
			}
		}

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Tests persistence of cache.
		/// </summary>
		/// ------------------------------------------------------------------------------------------
		[Test]
		public void Persistence()
		{
			string outFileName = Path.GetTempFileName();
			string srcFileName = Path.GetTempFileName();

			bool fNeedToDelete = true;
			try
			{
				CreateTempFile(outFileName);
				string outHash = m_mgr.GetHash(outFileName);

				CreateTempFile(srcFileName);
				string handle = "p_jXrLgQMK7dUx+QMtYfjw==.0"; // m_mgr.GetHash(srcFileName);

				m_mgr.CacheFile(handle, outFileName);
				m_mgr.Close();
				File.Delete(outFileName);
				fNeedToDelete = false;

				m_mgr = new LocalCacheManager();
				CachedFile[] resFileName = m_mgr.GetCachedFiles(handle);

				Assert.AreEqual(1, resFileName.Length);
				Assert.AreEqual(outHash, m_mgr.GetHash(resFileName[0].CachedFileName));
				Assert.AreEqual(Path.GetFileName(outFileName), resFileName[0].OriginalName);
			}
			finally
			{
				if (fNeedToDelete)
					File.Delete(outFileName);
				File.Delete(srcFileName);
			}
		}
	}
}
