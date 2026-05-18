using System;
using System.IO;
using NUnit.Framework;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.FieldWorks.Common.FwUtils;
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

		protected FwNewLangProjectModel m_model;
		protected LcmCache Cache;
		protected string m_dbName;
		private string m_projectDirectory;

		[SetUp]
		public virtual void TestSetup()
		{
			m_dbName = ReusableProjectName;
			m_projectDirectory = DbDirectory(m_dbName);
			DeleteProjectDirectory(m_projectDirectory);

			// Init New Lang Project Model (headless)
			m_model = new FwNewLangProjectModel(true)
			{
				LoadProjectNameSetup = () => { },
				LoadVernacularSetup = () => { },
				LoadAnalysisSetup = () => { },
				AnthroModel = new FwChooseAnthroListModel { CurrentList = FwChooseAnthroListModel.ListChoice.UserDef }
			};

			string createdPath;
			using (var threadHelper = new ThreadHelper())
			{
				m_model.ProjectName = m_dbName;
				m_model.Next(); // To Vernacular WS Setup
				m_model.SetDefaultWs(new LanguageInfo { LanguageTag = "qaa", DesiredName = "Vernacular" });
				m_model.Next(); // To Analysis WS Setup
				m_model.SetDefaultWs(new LanguageInfo { LanguageTag = "en", DesiredName = "English" });
				createdPath = m_model.CreateNewLangProj(new DummyProgressDlg(), threadHelper);
				m_projectDirectory = Path.GetDirectoryName(createdPath);
			}

			// Load the cache from the newly created .fwdata file
			Cache = LcmCache.CreateCacheFromExistingData(
				new TestProjectId(BackendProviderType.kXMLWithMemoryOnlyWsMgr, createdPath),
				"en",
				new DummyLcmUI(),
				FwDirectoryFinder.LcmDirectories,
				new LcmSettings(),
				new DummyProgressDlg());

			try
			{
				using (var undoWatcher = new UndoableUnitOfWorkHelper(Cache.ActionHandlerAccessor, "Test Setup", "Undo Test Setup"))
				{
					InitializeProjectData();
					CreateTestData();
					undoWatcher.RollBack = false;
				}
			}
			catch (Exception)
			{
				// If setup fails, ensure we don't leave a locked DB
				if (Cache != null)
				{
					Cache.Dispose();
					Cache = null;
				}
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
			if (Cache != null)
			{
				Cache.Dispose();
				Cache = null;
			}

			DeleteProjectDirectory(m_projectDirectory);
			m_projectDirectory = null;
		}

		protected string DbDirectory(string name)
		{
			return Path.Combine(FwDirectoryFinder.ProjectsDirectory, name);
		}

		private static void DeleteProjectDirectory(string projectDirectory)
		{
			if (string.IsNullOrEmpty(projectDirectory) || !Directory.Exists(projectDirectory))
				return;

			try { Directory.Delete(projectDirectory, true); } catch { }
		}
	}
}
