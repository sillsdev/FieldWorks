// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.Test.TestUtils;
using SIL.Utils;

namespace SIL.FieldWorks.FDO.CoreTests.PersistingLayerTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Test migrating data from each type of BEP to all others.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public sealed class BEPPortTests: BaseTest
	{
		/// <summary>Random number generator to prevent filename conflicts</summary>
		private readonly Random m_random;

		/// <summary/>
		public BEPPortTests()
		{
			m_random = new Random((int)DateTime.Now.Ticks);
		}

		#region Non-test methods.

		private BackendStartupParameter GenerateBackendStartupParameters(bool isTarget, FDOBackendProviderType type)
		{
			var nameSuffix = (isTarget ? "_New" : "") + m_random.Next(1000);
			string name = null;
			switch (type)
			{
				case FDOBackendProviderType.kXML:
					name = FdoFileHelper.GetXmlDataFileName("TLP" + nameSuffix);
					break;
				//case FDOBackendProviderType.kMemoryOnly: name = null;
			}

			return new BackendStartupParameter(true, BackendBulkLoadDomain.All, new TestProjectId(type, name));
		}

		/// <summary>
		/// Actually do the test between the source data in 'sourceGuids'
		/// and the target data in 'targetCache'.
		/// </summary>
		/// <param name="sourceGuids"></param>
		/// <param name="targetCache"></param>
		private static void CompareResults(ICollection<Guid> sourceGuids, FdoCache targetCache)
		{
			var allTargetObjects = GetAllCmObjects(targetCache);
			foreach (var obj in allTargetObjects)
				Assert.IsTrue(sourceGuids.Contains(obj.Guid), "Missing guid in target DB.: " + obj.Guid);
			var targetGuids = allTargetObjects.Select(obj => obj.Guid).ToList();
			foreach (var guid in sourceGuids)
			{
				Assert.IsTrue(targetGuids.Contains(guid), "Missing guid in source DB.: " + guid);
			}
			Assert.AreEqual(sourceGuids.Count, allTargetObjects.Length, "Wrong number of objects in target DB.");
		}

		/// <summary>
		/// Get the ICmObjectRepository from the cache.
		/// </summary>
		/// <param name="cache"></param>
		/// <returns></returns>
		private static ICmObject[] GetAllCmObjects(FdoCache cache)
		{
			return cache.ServiceLocator.GetInstance<ICmObjectRepository>().AllInstances().ToArray();
		}

		/// <summary>
		/// Get the IDataSetup from the cache.
		/// </summary>
		/// <param name="cache"></param>
		/// <returns></returns>
		private static IDataSetup GetMainBEPInterface(FdoCache cache)
		{
			return cache.ServiceLocator.GetInstance<IDataSetup>();
		}

		/// <summary>
		/// Wipe out the current BEP's file(s), since it is about to be created ex-nihilo.
		/// </summary>
		/// <param name="backendParameters"></param>
		private static void DeleteDatabase(BackendStartupParameter backendParameters)
		{
			string pathname = string.Empty;
			if(backendParameters.ProjectId.Type != FDOBackendProviderType.kMemoryOnly)
				pathname = backendParameters.ProjectId.Path;
			if(backendParameters.ProjectId.Type != FDOBackendProviderType.kMemoryOnly &&
			File.Exists(pathname))
			{
				File.Delete(pathname);
				//The File.Delete command returns before the OS has actually removed the file,
				//this causes re-creation of the file to fail intermittently so we'll wait a bit for it to be gone.
				for(var i = 0; File.Exists(pathname) && i < 5; ++i)
				{
					Thread.Sleep(10);
				}
				Assert.That(!File.Exists(pathname), "Database file failed to be deleted.");
			}
		}

		#endregion Non-test methods.

		#region Tests
		/// <summary>
		/// Make sure each BEP type migrates to all other BEP types,
		/// including memory only just to be complete.
		///
		/// This test uses an already opened BEP for the source,
		/// so it tests the BEP method that accepts the source FdoCache
		/// and creates a new target.
		/// </summary>
		[Test]
		[Combinatorial]
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "source/targetDataSetup are singletons; disposed by service locator")]
		public void PortAllBEPsTestsUsingAnAlreadyOpenedSource(
			[Values(FDOBackendProviderType.kXML, FDOBackendProviderType.kMemoryOnly)]
			FDOBackendProviderType sourceType,
			[Values(FDOBackendProviderType.kXML, FDOBackendProviderType.kMemoryOnly)]
			FDOBackendProviderType targetType)
		{
			var sourceBackendStartupParameters = GenerateBackendStartupParameters(false, sourceType);
			var targetBackendStartupParameters = GenerateBackendStartupParameters(true, targetType);

			DeleteDatabase(sourceBackendStartupParameters);

			// Set up data source, but only do it once.
			var sourceGuids = new List<Guid>();
			using (var sourceCache = FdoCache.CreateCacheWithNewBlankLangProj(
				new TestProjectId(sourceBackendStartupParameters.ProjectId.Type,
								sourceBackendStartupParameters.ProjectId.Path), "en", "fr", "en", new DummyFdoUI(), FwDirectoryFinder.FdoDirectories, new FdoSettings()))
			{
				// BEP is a singleton, so we shouldn't call Dispose on it. This will be done
				// by service locator.
				var sourceDataSetup = GetMainBEPInterface(sourceCache);
				// The source is created ex nihilo.
				sourceDataSetup.LoadDomain(sourceBackendStartupParameters.BulkLoadDomain);
				sourceGuids.AddRange(GetAllCmObjects(sourceCache).Select(obj => obj.Guid)); // Collect all source Guids

				DeleteDatabase(targetBackendStartupParameters);

				// Migrate source data to new BEP.
				using (var targetCache = FdoCache.CreateCacheCopy(
					new TestProjectId(targetBackendStartupParameters.ProjectId.Type,
									targetBackendStartupParameters.ProjectId.Path), "en", new DummyFdoUI(), FwDirectoryFinder.FdoDirectories, new FdoSettings(), sourceCache))
				{
					// BEP is a singleton, so we shouldn't call Dispose on it. This will be done
					// by service locator.
					var targetDataSetup = GetMainBEPInterface(targetCache);
					targetDataSetup.LoadDomain(BackendBulkLoadDomain.All);

					CompareResults(sourceGuids, targetCache);
				}
			}
		}

		/// <summary>
		/// Make sure each BEP type migrates to all other BEP types,
		/// including memory only just to be complete.
		///
		/// This test uses an un-opened BEP for the source,
		/// so it tests the BEP method that starts up the source and creates the target.
		/// </summary>
		/// <remarks>
		/// The Memory only source BEP can't tested here, since it can't be deleted, created,
		/// and restarted which is required of all source BEPs in this test.
		/// The source memory BEP is tested in 'PortAllBEPsTestsUsingAnAlreadyOpenedSource',
		/// since source BEPs are only created once and the open connection is reused for
		/// all targets.</remarks>
		[Test]
		[Combinatorial]
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "source/targetDataSetup are singletons; disposed by service locator")]
		public void PortAllBEPsTestsUsingAnUnopenedSource(
			[Values(FDOBackendProviderType.kXML)]
			FDOBackendProviderType sourceType,
			[Values(FDOBackendProviderType.kXML, FDOBackendProviderType.kMemoryOnly)]
			FDOBackendProviderType targetType)
		{
			var path = Path.Combine(Path.GetTempPath(), "FieldWorksTest");
			if (!Directory.Exists(path))
				Directory.CreateDirectory(path);
			var sourceBackendStartupParameters = GenerateBackendStartupParameters(false, sourceType);
			var targetBackendStartupParameters = GenerateBackendStartupParameters(true, targetType);

			var sourceGuids = new List<Guid>();

			DeleteDatabase(sourceBackendStartupParameters);
			DeleteDatabase(targetBackendStartupParameters);

			// Set up data source
			var projId = new TestProjectId(sourceBackendStartupParameters.ProjectId.Type,
													sourceBackendStartupParameters.ProjectId.Path);
			using (FdoCache sourceCache = FdoCache.CreateCacheWithNewBlankLangProj(
				projId, "en", "fr", "en", new DummyFdoUI(), FwDirectoryFinder.FdoDirectories, new FdoSettings()))
			{
				// BEP is a singleton, so we shouldn't call Dispose on it. This will be done
				// by service locator.
				var sourceDataSetup = GetMainBEPInterface(sourceCache);
				sourceCache.ServiceLocator.GetInstance<IUndoStackManager>().Save(); // persist the new db so we can reopen it.
				sourceDataSetup.LoadDomain(BackendBulkLoadDomain.All);
				sourceGuids.AddRange(GetAllCmObjects(sourceCache).Select(obj => obj.Guid)); // Collect all source Guids
			}

			// Migrate source data to new BEP.
			IThreadedProgress progressDlg = new DummyProgressDlg();
			using (var targetCache = FdoCache.CreateCacheWithNoLangProj(
				new TestProjectId(targetBackendStartupParameters.ProjectId.Type, null), "en", new DummyFdoUI(), FwDirectoryFinder.FdoDirectories, new FdoSettings()))
			{
				// BEP is a singleton, so we shouldn't call Dispose on it. This will be done
				// by service locator.
				var targetDataSetup = GetMainBEPInterface(targetCache);
				targetDataSetup.InitializeFromSource(new TestProjectId(targetBackendStartupParameters.ProjectId.Type,
																		targetBackendStartupParameters.ProjectId.Path), sourceBackendStartupParameters, "en", progressDlg);
				targetDataSetup.LoadDomain(BackendBulkLoadDomain.All);
				CompareResults(sourceGuids, targetCache);
			}
			sourceGuids.Clear();
		}
		#endregion
	}
}