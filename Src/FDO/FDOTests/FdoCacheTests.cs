// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003-2010, SIL International. All Rights Reserved.
// <copyright from='2003' to='2010' company='SIL International'>
//		Copyright (c) 2003-2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FdoCacheTests.cs
// Responsibility: FW Team
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Remoting;
using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.Test.TestUtils;
using SIL.Utils;

namespace SIL.FieldWorks.FDO.CoreTests.FdoCacheTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests the public API of FdoCache.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class FdoCacheTests : MemoryOnlyBackendProviderTestBase
	{
		private string m_oldProjectDirectory;

		/// <summary>Setup for db4o client server tests.</summary>
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			m_oldProjectDirectory = DirectoryFinder.ProjectsDirectory;
			DirectoryFinder.ProjectsDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
			Directory.CreateDirectory(DirectoryFinder.ProjectsDirectory);

			try
			{
				// Allow db4o client server unit test to work without running the window service.
				FwRemoteDatabaseConnector.RemotingServer.Start();
			}
			catch (RemotingException e)
			{
				// This can happen if the server is already running (e.g. if only running CreateNewLangProject_DbFilesExist
				// multiple times. In this case the port is already in use.
				// REVIEW (EberhardB): Do we have to throw for other error cases?
				Console.WriteLine("Got remoting exception: " + e.Message);
			}
		}

		/// <summary>Stop db4o client server.</summary>
		public override void FixtureTeardown()
		{
			FwRemoteDatabaseConnector.RemotingServer.Stop();
			Directory.Delete(DirectoryFinder.ProjectsDirectory, true);
			DirectoryFinder.ProjectsDirectory = m_oldProjectDirectory;
			base.FixtureTeardown();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test when database files already exist.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void CreateNewLangProject_DbFilesExist()
		{
			var preExistingDirs = new List<string>(Directory.GetDirectories(DirectoryFinder.ProjectsDirectory));
			string[] postExistingDirs = null;
			try
			{
				// Setup: Create "pre-existing" DB filenames
				using (new DummyFileMaker(Path.Combine(
					Path.Combine(DirectoryFinder.ProjectsDirectory, "Gumby"), DirectoryFinder.GetXmlDataFileName("Gumby"))))
				{
					using (var threadHelper = new ThreadHelper())
						FdoCache.CreateNewLangProj(new DummyProgressDlg(), "Gumby", threadHelper);
				}
			}
			finally
			{
				RemoveTestDirs(preExistingDirs, Directory.GetDirectories(DirectoryFinder.ProjectsDirectory));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test handling of single quote in language project name.
		/// JIRA Issue TE-6138.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreateNewLangProject_NameWithSingleQuote()
		{
			const string dbName = "!!t'st";
			string dbDir = Path.Combine(DirectoryFinder.ProjectsDirectory, dbName);
			var tmpDbDir = Path.Combine(Path.Combine(DirectoryFinder.ProjectsDirectory, ".."), dbName);
			if (Directory.Exists(dbDir))
			{
				// it might seem strange to move the directory first before deleting it.
				// However, this solves the problem that the Delete() returns before
				// everything is deleted.
				Directory.Move(dbDir, tmpDbDir);
				Directory.Delete(tmpDbDir, true);
			}
			Assert.IsFalse(Directory.Exists(dbDir), "Can't delete directory of test project: " + dbDir);

			var expectedDirs = new List<string>(Directory.GetDirectories(DirectoryFinder.ProjectsDirectory))
				{ dbDir };
			var writingSystemsCommonDir = Path.Combine(DirectoryFinder.ProjectsDirectory, DirectoryFinder.ksWritingSystemsDir);

			List<string> currentDirs = null;
			try
			{
				string dbFileName;
				using (var threadHelper = new ThreadHelper())
					dbFileName = FdoCache.CreateNewLangProj(new DummyProgressDlg(), dbName, threadHelper);

				currentDirs = new List<string>(Directory.GetDirectories(DirectoryFinder.ProjectsDirectory));
				if (currentDirs.Contains(writingSystemsCommonDir) && !expectedDirs.Contains(writingSystemsCommonDir))
					expectedDirs.Add(writingSystemsCommonDir);
				CollectionAssert.AreEquivalent(expectedDirs, currentDirs);
				string dbFileBase = Path.GetFileNameWithoutExtension(dbFileName);
				Assert.AreEqual(dbName, dbFileBase);
			}
			finally
			{
				if (currentDirs != null)
					RemoveTestDirs(expectedDirs, currentDirs);
			}
		}

		#region Private helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Removes the test dirs.
		/// </summary>
		/// <param name="preExistingDirs">The pre existing dirs.</param>
		/// <param name="postExistingDirs">The post existing dirs.</param>
		/// ------------------------------------------------------------------------------------
		private static void RemoveTestDirs(List<string> preExistingDirs, IEnumerable<string> postExistingDirs)
		{
			// Blow away the files to clean things up
			foreach (string dirName in postExistingDirs)
			{
				try
				{
					if (!preExistingDirs.Contains(dirName))
						Directory.Delete(dirName, true);
				}
				catch
				{
				}
			}
		}
		#endregion

		/// <summary>
		/// What it says.
		/// </summary>
		[Test]
		public void ChangingLangProjDefaultVernWs_ChangesCacheDefaultVernWs()
		{
			using (var threadHelper = new ThreadHelper())
			using (
				var cache = FdoCache.CreateCacheWithNewBlankLangProj(new TestProjectId(FDOBackendProviderType.kMemoryOnly, null),
																	 "en", "fr", "en", threadHelper))
			{
				var wsFr = cache.DefaultVernWs;
				Assert.That(cache.LangProject.DefaultVernacularWritingSystem.Handle, Is.EqualTo(wsFr));
				IWritingSystem wsObjGerman = null;
				UndoableUnitOfWorkHelper.Do("undoit", "redoit", cache.ActionHandlerAccessor,
					() =>
					{
						WritingSystemServices.FindOrCreateWritingSystem(cache, "de", false, true, out wsObjGerman);
						Assert.That(cache.DefaultVernWs, Is.EqualTo(wsFr));
						cache.LangProject.DefaultVernacularWritingSystem = wsObjGerman;
						Assert.That(cache.DefaultVernWs, Is.EqualTo(wsObjGerman.Handle));
					});
				UndoableUnitOfWorkHelper.Do("undoit", "redoit", cache.ActionHandlerAccessor,
				   () =>
				   {
					   cache.LangProject.CurVernWss = "fr";
					   Assert.That(cache.DefaultVernWs, Is.EqualTo(wsFr));
				   });
				cache.ActionHandlerAccessor.Undo();
				Assert.That(cache.DefaultVernWs, Is.EqualTo(wsObjGerman.Handle));
				cache.ActionHandlerAccessor.Redo();
				Assert.That(cache.DefaultVernWs, Is.EqualTo(wsFr));
			}
		}
		/// <summary>
		/// What it says.
		/// </summary>
		[Test]
		public void ChangingLangProjDefaultAnalysisWs_ChangesCacheDefaultAnalWs()
		{
			using (var threadHelper = new ThreadHelper())
			using (
				var cache = FdoCache.CreateCacheWithNewBlankLangProj(new TestProjectId(FDOBackendProviderType.kMemoryOnly, null),
																	 "en", "fr", "en", threadHelper))
			{
				var wsEn = cache.DefaultAnalWs;
				Assert.That(cache.LangProject.DefaultAnalysisWritingSystem.Handle, Is.EqualTo(wsEn));
				IWritingSystem wsObjGerman = null;
				UndoableUnitOfWorkHelper.Do("undoit", "redoit", cache.ActionHandlerAccessor,
					() =>
					{
						WritingSystemServices.FindOrCreateWritingSystem(cache, "de", true, false, out wsObjGerman);
						Assert.That(cache.DefaultAnalWs, Is.EqualTo(wsEn));
						cache.LangProject.DefaultAnalysisWritingSystem = wsObjGerman;
						Assert.That(cache.DefaultAnalWs, Is.EqualTo(wsObjGerman.Handle));
					});
				UndoableUnitOfWorkHelper.Do("undoit", "redoit", cache.ActionHandlerAccessor,
				   () =>
				   {
					   cache.LangProject.CurAnalysisWss = "en";
					   Assert.That(cache.DefaultAnalWs, Is.EqualTo(wsEn));
				   });
				cache.ActionHandlerAccessor.Undo();
				Assert.That(cache.DefaultAnalWs, Is.EqualTo(wsObjGerman.Handle));
				cache.ActionHandlerAccessor.Redo();
				Assert.That(cache.DefaultAnalWs, Is.EqualTo(wsEn));
			}
		}

		/// <summary>
		/// What it says.
		/// </summary>
		[Test]
		public void ChangingLangProjDefaultPronunciationWs_ChangesCacheDefaultPronunciationWs()
		{
			using (var threadHelper = new ThreadHelper())
			using (
				var cache = FdoCache.CreateCacheWithNewBlankLangProj(new TestProjectId(FDOBackendProviderType.kMemoryOnly, null),
																	 "en", "fr", "en", threadHelper))
			{
				var wsFr = cache.DefaultPronunciationWs;
				Assert.That(cache.LangProject.DefaultPronunciationWritingSystem.Handle, Is.EqualTo(wsFr));
				IWritingSystem wsObjGerman = null;
				IWritingSystem wsObjSpanish = null;
				UndoableUnitOfWorkHelper.Do("undoit", "redoit", cache.ActionHandlerAccessor,
					() =>
					{
						WritingSystemServices.FindOrCreateWritingSystem(cache, "de", false, true, out wsObjGerman);
						Assert.That(cache.DefaultPronunciationWs, Is.EqualTo(wsFr));
						cache.LangProject.DefaultVernacularWritingSystem = wsObjGerman;
						cache.LangProject.CurrentPronunciationWritingSystems.Clear();
						// Now it re-evaluates to the new default vernacular.
						Assert.That(cache.DefaultPronunciationWs, Is.EqualTo(wsObjGerman.Handle));

						// This no longer works..._IPA does not make a valid WS ID.
						//IWritingSystem wsObjGermanIpa;
						//WritingSystemServices.FindOrCreateWritingSystem(cache, "de__IPA", false, true, out wsObjGermanIpa);
						//cache.LangProject.CurrentPronunciationWritingSystems.Clear();
						//// Once there is an IPA one, we should prefer that
						//Assert.That(cache.DefaultPronunciationWs, Is.EqualTo(wsObjGermanIpa.Handle));

						// Unless we clear the list it does not regenerate.
						WritingSystemServices.FindOrCreateWritingSystem(cache, "es", false, true, out wsObjSpanish);
						// Once we've found a real pronunciation WS, changing the default vernacular should not change it.
						Assert.That(cache.DefaultPronunciationWs, Is.EqualTo(wsObjGerman.Handle));
					});
				UndoableUnitOfWorkHelper.Do("undoit", "redoit", cache.ActionHandlerAccessor,
				   () =>
				   {
					   cache.LangProject.CurPronunWss = "es";
					   Assert.That(cache.DefaultPronunciationWs, Is.EqualTo(wsObjSpanish.Handle));
				   });
				cache.ActionHandlerAccessor.Undo();
				Assert.That(cache.DefaultPronunciationWs, Is.EqualTo(wsObjGerman.Handle));
				cache.ActionHandlerAccessor.Redo();
				Assert.That(cache.DefaultPronunciationWs, Is.EqualTo(wsObjSpanish.Handle));
			}
		}
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests the Disposed related methods on FdoCache.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class FdoCacheDisposedTests: BaseTest
	{
		/// <summary>
		/// Make sure the CheckDisposed method works.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ObjectDisposedException))]
		public void CacheCheckDisposedTest()
		{
			// This can't be in the minimalist class, because it disposes the cache.
			using (var threadHelper = new ThreadHelper())
			{
				var cache = FdoCache.CreateCacheWithNewBlankLangProj(new TestProjectId(FDOBackendProviderType.kMemoryOnly, null),
																	 "en", "fr", "en", threadHelper);
				// Init backend data provider
				var dataSetup = cache.ServiceLocator.GetInstance<IDataSetup>();
				dataSetup.LoadDomain(BackendBulkLoadDomain.All);
				cache.Dispose();
				cache.CheckDisposed();
			}
		}

		/// <summary>
		/// Make sure the IsDisposed method works.
		/// </summary>
		[Test]
		public void CacheIsDisposedTest()
		{
			using (var threadHelper = new ThreadHelper())
			{
				// This can't be in the minimalist class, because it disposes the cache.
				var cache = FdoCache.CreateCacheWithNewBlankLangProj(new TestProjectId(FDOBackendProviderType.kMemoryOnly, null),
																	 "en", "fr", "en", threadHelper);
				// Init backend data provider
				var dataSetup = cache.ServiceLocator.GetInstance<IDataSetup>();
				dataSetup.LoadDomain(BackendBulkLoadDomain.All);
				Assert.IsFalse(cache.IsDisposed, "Should not have been disposed.");
				cache.Dispose();
				Assert.IsTrue(cache.IsDisposed, "Should have been disposed.");
			}
		}

		/// <summary>
		/// Make sure an FDO can't be used, after its FdoCache has been disposed.
		/// </summary>
		[Test]
		public void CacheDisposedForFDOObject()
		{
			using (var threadHelper = new ThreadHelper())
			{
				var cache = FdoCache.CreateCacheWithNewBlankLangProj(new TestProjectId(FDOBackendProviderType.kMemoryOnly, null),
																	 "en", "fr", "en", threadHelper);
				// Init backend data provider
				var dataSetup = cache.ServiceLocator.GetInstance<IDataSetup>();
				dataSetup.LoadDomain(BackendBulkLoadDomain.All);
				var lp = cache.LanguageProject;
				cache.Dispose();
				Assert.IsFalse(lp.IsValidObject);
			}
		}

		/// <summary>
		/// Make sure an FDO can't be used, after its FdoCache has been disposed.
		/// </summary>
		[Test]
		public void FDOObjectDeleted()
		{
			using (var threadHelper = new ThreadHelper())
			using (var cache = FdoCache.CreateCacheWithNewBlankLangProj(new TestProjectId(FDOBackendProviderType.kMemoryOnly, null),
				"en", "fr", "en", threadHelper))
			{
				// Init backend data provider
				var dataSetup = cache.ServiceLocator.GetInstance<IDataSetup>();
				dataSetup.LoadDomain(BackendBulkLoadDomain.All);
				var lp = cache.LanguageProject;
				cache.ActionHandlerAccessor.BeginNonUndoableTask();
				var peopleList = cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
				lp.PeopleOA = peopleList;
				lp.PeopleOA = null;
				cache.ActionHandlerAccessor.EndNonUndoableTask();
				Assert.IsFalse(peopleList.IsValidObject);
			}
		}

		/// <summary>
		/// Test NumberOfRemoteClients with a non client server BEP.
		/// </summary>
		[Test]
		public void NumberOfRemoteClients_NotClientServer_ReturnsZero()
		{
			using (var threadHelper = new ThreadHelper())
			using (
				var cache = FdoCache.CreateCacheWithNewBlankLangProj(new TestProjectId(FDOBackendProviderType.kMemoryOnly, null),
																	 "en", "fr", "en", threadHelper))
			{
				Assert.AreEqual(0, cache.NumberOfRemoteClients);
			}
		}
	}
}
