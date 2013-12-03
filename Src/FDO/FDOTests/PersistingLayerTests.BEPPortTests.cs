using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using FwRemoteDatabaseConnector;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.Test.TestUtils;

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
		/// <summary>
		/// Gets the current architecture (i686 or x86_64 on Linux, or
		/// Win on Windows)
		/// </summary>
		private static string Architecture
		{
			get
			{
				if (Environment.OSVersion.Platform == PlatformID.Unix)
				{
					// Unix - return output from 'uname -m'
					using (Process process = new Process())
					{
						process.StartInfo.FileName = "uname";
						process.StartInfo.Arguments = "-m";
						process.StartInfo.UseShellExecute = false;
						process.StartInfo.CreateNoWindow = true;
						process.StartInfo.RedirectStandardOutput = true;
						process.Start();

						string architecture = process.StandardOutput.ReadToEnd().Trim();

						process.StandardOutput.Close();
						process.Close();
						return architecture;
					}
				}
				return "Win";
			}
		}

		/// <summary>
		/// Set up parameters for both source and target databases for use in PortAllBEPsTestsUsingAnAlreadyOpenedSource
		/// </summary>
		private readonly List<BackendStartupParameter> m_sourceInfo;
		private readonly List<BackendStartupParameter> m_targetInfo;

		/// <summary>Database backends used for testing</summary>
		public enum TestBackends
		{
			/// <summary>XML based database backend</summary>
			Xml,
			/// <summary>Memory based database backend</summary>
			Memory,
			/// <summary>Db4o based database backend</summary>
			Db4o
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="BEPPortTests"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public BEPPortTests()
		{
			var randomFileExtension = new Random((int)DateTime.Now.Ticks).Next(1000).ToString();
			m_sourceInfo = new List<BackendStartupParameter>
				{
					new BackendStartupParameter(true, BackendBulkLoadDomain.All,
												new TestProjectId(FDOBackendProviderType.kXML, DirectoryFinder.GetXmlDataFileName("TLP" + randomFileExtension))),
					new BackendStartupParameter(true, BackendBulkLoadDomain.All,
												new TestProjectId(FDOBackendProviderType.kMemoryOnly, null)),
					new BackendStartupParameter(true, BackendBulkLoadDomain.All,
												new TestProjectId(FDOBackendProviderType.kDb4oClientServer, "TLPCS" + randomFileExtension))
				};
			m_targetInfo = new List<BackendStartupParameter>
				{
					new BackendStartupParameter(true, BackendBulkLoadDomain.All,
												new TestProjectId(FDOBackendProviderType.kXML, DirectoryFinder.GetXmlDataFileName("TLP_New" + randomFileExtension))),
					new BackendStartupParameter(true, BackendBulkLoadDomain.All,
												new TestProjectId(FDOBackendProviderType.kMemoryOnly, null)),
					new BackendStartupParameter(true, BackendBulkLoadDomain.All,
												new TestProjectId(FDOBackendProviderType.kDb4oClientServer, "TLPCS_New" + randomFileExtension))
				};
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If a test overrides this, it should call the base implementation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void FixtureSetup()
		{
			RemotingServer.Start();
			base.FixtureSetup();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If a test overrides this, it should call the base implementation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void FixtureTeardown()
		{
			base.FixtureTeardown();
			RemotingServer.Stop();
		}

		#region Non-test methods.

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
			var targetGuids = new List<Guid>();
			foreach (var obj in allTargetObjects)
				targetGuids.Add(obj.Guid);
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
			// db4o client/server has its own mechanism.
			if (backendParameters.ProjectId.Type == FDOBackendProviderType.kDb4oClientServer)
				return;
			string pathname = string.Empty;
			if (backendParameters.ProjectId.Type != FDOBackendProviderType.kMemoryOnly)
				pathname = backendParameters.ProjectId.Path;
			if (backendParameters.ProjectId.Type != FDOBackendProviderType.kMemoryOnly && File.Exists(pathname))
				File.Delete(pathname);
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
			[Values(TestBackends.Xml, TestBackends.Memory, TestBackends.Db4o)] TestBackends iSource,
			[Values(TestBackends.Xml, TestBackends.Memory, TestBackends.Db4o)] TestBackends iTarget)
		{
			var sourceBackendStartupParameters = m_sourceInfo[(int)iSource];
			var targetBackendStartupParameters = m_targetInfo[(int)iTarget];

			DeleteDatabase(sourceBackendStartupParameters);

			// Set up data source, but only do it once.
			var sourceGuids = new List<Guid>();
			using (var sourceCache = FdoCache.CreateCacheWithNewBlankLangProj(
				new TestProjectId(sourceBackendStartupParameters.ProjectId.Type,
								sourceBackendStartupParameters.ProjectId.Path), "en", "fr", "en", new DummyFdoUserAction()))
			{
				// BEP is a singleton, so we shouldn't call Dispose on it. This will be done
				// by service locator.
				var sourceDataSetup = GetMainBEPInterface(sourceCache);
				// The source is created ex nihilo.
				sourceDataSetup.LoadDomain(sourceBackendStartupParameters.BulkLoadDomain);
				foreach (var obj in GetAllCmObjects(sourceCache))
					sourceGuids.Add(obj.Guid); // Collect up all source Guids.

				DeleteDatabase(targetBackendStartupParameters);

				// Migrate source data to new BEP.
				using (var targetCache = FdoCache.CreateCacheCopy(
					new TestProjectId(targetBackendStartupParameters.ProjectId.Type,
									targetBackendStartupParameters.ProjectId.Path), "en", sourceCache, new DummyFdoUserAction()))
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
			[Values(TestBackends.Xml, TestBackends.Db4o)] TestBackends iSource,
			[Values(TestBackends.Xml, TestBackends.Memory, TestBackends.Db4o)] TestBackends iTarget)
		{
			var path = Path.Combine(Path.GetTempPath(), "FieldWorksTest");
			if (!Directory.Exists(path))
				Directory.CreateDirectory(path);
			var sourceBackendStartupParameters = m_sourceInfo[(int)iSource];
			var targetBackendStartupParameters = m_targetInfo[(int)iTarget];

			var sourceGuids = new List<Guid>();

			DeleteDatabase(sourceBackendStartupParameters);
			DeleteDatabase(targetBackendStartupParameters);

			// Set up data source
			TestProjectId projId = new TestProjectId(sourceBackendStartupParameters.ProjectId.Type,
													sourceBackendStartupParameters.ProjectId.Path);
			IThreadedProgress progressDlg = new DummyProgressDlg();
			using (FdoCache sourceCache = FdoCache.CreateCacheWithNewBlankLangProj(
				projId, "en", "fr", "en", new DummyFdoUserAction()))
			{
				// BEP is a singleton, so we shouldn't call Dispose on it. This will be done
				// by service locator.
				var sourceDataSetup = GetMainBEPInterface(sourceCache);
				sourceCache.ServiceLocator.GetInstance<IUndoStackManager>().Save(); // persist the new db so we can reopen it.
				sourceDataSetup.LoadDomain(BackendBulkLoadDomain.All);
				foreach (var obj in GetAllCmObjects(sourceCache))
					sourceGuids.Add(obj.Guid); // Collect up all source Guids.
			}

			// Migrate source data to new BEP.
			progressDlg = new DummyProgressDlg();
			using (var targetCache = FdoCache.CreateCacheWithNoLangProj(
				new TestProjectId(targetBackendStartupParameters.ProjectId.Type, null), "en", new DummyFdoUserAction()))
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