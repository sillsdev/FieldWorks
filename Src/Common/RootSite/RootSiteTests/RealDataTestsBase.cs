using System;
using System.IO;
using System.Threading;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Infrastructure;
using SIL.TestUtilities;
using SIL.WritingSystems;

namespace SIL.FieldWorks.Common.RootSites.RootSiteTests
{
	/// <summary>
	/// Base class for tests requiring a real file-backed LcmCache (not Mock/Memory).
	/// This ensures full schema validation and behaviors (like StVc) work correctly.
	/// </summary>
	[TestFixture]
	public abstract class RealDataTestsBase
	{
		private const string ReusableProjectName = "integration_test_data";
		private const string ProjectMutexName = @"Local\FieldWorks.RealDataTests.integration_test_data";
		private const string TestProjectSentinelFileName = ".fieldworks-real-data-test-project";
		private const int DeleteRetryCount = 3;
		private static readonly TimeSpan DeleteRetryDelay = TimeSpan.FromMilliseconds(250);

		protected FwNewLangProjectModel m_model;
		protected LcmCache Cache;
		protected string m_dbName;
		private string m_projectDirectory;
		private Mutex m_projectMutex;

		[SetUp]
		public virtual void TestSetup()
		{
			m_dbName = ReusableProjectName;
			m_projectDirectory = DbDirectory(m_dbName);
			AcquireProjectMutex();

			try
			{
				DeleteProjectDirectory(m_projectDirectory);

				m_model = new FwNewLangProjectModel(true)
				{
					LoadProjectNameSetup = () => { },
					LoadVernacularSetup = () => { },
					LoadAnalysisSetup = () => { },
					AnthroModel = new FwChooseAnthroListModel
					{
						CurrentList = FwChooseAnthroListModel.ListChoice.UserDef,
					},
				};

				string createdPath;
				using (var threadHelper = new ThreadHelper())
				{
					m_model.ProjectName = m_dbName;
					m_model.Next(); // To Vernacular WS Setup
					m_model.SetDefaultWs(
						new LanguageInfo { LanguageTag = "qaa", DesiredName = "Vernacular" }
					);
					m_model.Next(); // To Analysis WS Setup
					m_model.SetDefaultWs(
						new LanguageInfo { LanguageTag = "en", DesiredName = "English" }
					);
					createdPath = m_model.CreateNewLangProj(new DummyProgressDlg(), threadHelper);
					m_projectDirectory = GetProjectDirectory(createdPath);
					WriteTestProjectSentinel(m_projectDirectory);
				}

				Cache = LcmCache.CreateCacheFromExistingData(
					new TestProjectId(BackendProviderType.kXMLWithMemoryOnlyWsMgr, createdPath),
					"en",
					new DummyLcmUI(),
					FwDirectoryFinder.LcmDirectories,
					new LcmSettings(),
					new DummyProgressDlg()
				);

				try
				{
					using (
						var undoWatcher = new UndoableUnitOfWorkHelper(
							Cache.ActionHandlerAccessor,
							"Test Setup",
							"Undo Test Setup"
						)
					)
					{
						InitializeProjectData();
						CreateTestData();
						undoWatcher.RollBack = false;
					}
				}
				catch (Exception)
				{
					DisposeCache();
					throw;
				}
			}
			catch (Exception)
			{
				DisposeCache();
				TryDeleteProjectDirectoryAfterSetupFailure();
				ReleaseProjectMutex();
				throw;
			}
		}

		protected virtual void InitializeProjectData()
		{
			// Override to add basic project data before CreateTestData
		}

		protected virtual void CreateTestData()
		{
			// Override in subclasses to populate the DB
		}

		[TearDown]
		public virtual void TestTearDown()
		{
			try
			{
				DisposeCache();
				DeleteProjectDirectory(m_projectDirectory);
			}
			finally
			{
				m_projectDirectory = null;
				ReleaseProjectMutex();
			}
		}

		protected string DbDirectory(string name)
		{
			return Path.Combine(FwDirectoryFinder.ProjectsDirectory, name);
		}

		private void AcquireProjectMutex()
		{
			m_projectMutex = new Mutex(false, ProjectMutexName);
			try
			{
				m_projectMutex.WaitOne();
			}
			catch (AbandonedMutexException)
			{
			}
		}

		private void ReleaseProjectMutex()
		{
			if (m_projectMutex == null)
				return;

			try
			{
				m_projectMutex.ReleaseMutex();
			}
			catch (ApplicationException)
			{
			}
			finally
			{
				m_projectMutex.Dispose();
				m_projectMutex = null;
			}
		}

		private void DisposeCache()
		{
			if (Cache == null)
				return;

			Cache.Dispose();
			Cache = null;
		}

		private void TryDeleteProjectDirectoryAfterSetupFailure()
		{
			try
			{
				DeleteProjectDirectory(m_projectDirectory);
			}
			catch (Exception e)
			{
				TestContext.Error.WriteLine(
					"Could not clean up test project directory '{0}' after setup failure: {1}",
					m_projectDirectory,
					e.Message
				);
			}
		}

		private static string GetProjectDirectory(string createdPath)
		{
			if (string.IsNullOrEmpty(createdPath))
				throw new InvalidOperationException("CreateNewLangProj did not return a project path.");

			var fullPath = NormalizePath(createdPath);
			if (Directory.Exists(fullPath))
			{
				EnsureSafeProjectDirectory(fullPath);
				return fullPath;
			}

			if (!File.Exists(fullPath))
				throw new FileNotFoundException("CreateNewLangProj returned a path that does not exist.", fullPath);

			var projectDirectory = Path.GetDirectoryName(fullPath);
			EnsureSafeProjectDirectory(projectDirectory);
			return projectDirectory;
		}

		private static void WriteTestProjectSentinel(string projectDirectory)
		{
			EnsureSafeProjectDirectory(projectDirectory);
			File.WriteAllText(
				GetSentinelFilePath(projectDirectory),
				"Created by FieldWorks RootSiteTests. This directory is safe for tests to delete."
			);
		}

		private static void DeleteProjectDirectory(string projectDirectory)
		{
			if (string.IsNullOrEmpty(projectDirectory) || !Directory.Exists(projectDirectory))
				return;

			var safeProjectDirectory = NormalizePath(projectDirectory);
			EnsureSafeProjectDirectory(safeProjectDirectory);

			if (!File.Exists(GetSentinelFilePath(safeProjectDirectory)))
			{
				throw new InvalidOperationException(
					string.Format(
						"Refusing to delete '{0}' because the test sentinel file '{1}' is missing.",
						safeProjectDirectory,
						TestProjectSentinelFileName
					)
				);
			}

			Exception lastException = null;
			for (var attempt = 1; attempt <= DeleteRetryCount; attempt++)
			{
				try
				{
					Directory.Delete(safeProjectDirectory, true);
					return;
				}
				catch (IOException e)
				{
					lastException = e;
					LogDeleteFailure(safeProjectDirectory, attempt, e);
				}
				catch (UnauthorizedAccessException e)
				{
					lastException = e;
					LogDeleteFailure(safeProjectDirectory, attempt, e);
				}

				if (attempt < DeleteRetryCount)
					Thread.Sleep(DeleteRetryDelay);
			}

			throw new IOException(
				string.Format(
					"Could not delete test project directory '{0}' after {1} attempts.",
					safeProjectDirectory,
					DeleteRetryCount
				),
				lastException
			);
		}

		private static void LogDeleteFailure(string projectDirectory, int attempt, Exception e)
		{
			TestContext.Error.WriteLine(
				"Could not delete test project directory '{0}' on attempt {1} of {2}: {3}",
				projectDirectory,
				attempt,
				DeleteRetryCount,
				e.Message
			);
		}

		private static void EnsureSafeProjectDirectory(string projectDirectory)
		{
			if (string.IsNullOrEmpty(projectDirectory))
				throw new InvalidOperationException("The test project directory path is empty.");

			var safeProjectDirectory = NormalizePath(projectDirectory);
			var expectedProjectDirectory = NormalizePath(
				Path.Combine(FwDirectoryFinder.ProjectsDirectory, ReusableProjectName)
			);

			if (!string.Equals(safeProjectDirectory, expectedProjectDirectory, StringComparison.OrdinalIgnoreCase))
			{
				throw new InvalidOperationException(
					string.Format(
						"Refusing to use test project directory '{0}'; expected '{1}'.",
						safeProjectDirectory,
						expectedProjectDirectory
					)
				);
			}
		}

		private static string GetSentinelFilePath(string projectDirectory)
		{
			return Path.Combine(projectDirectory, TestProjectSentinelFileName);
		}

		private static string NormalizePath(string path)
		{
			return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		}
	}
}
