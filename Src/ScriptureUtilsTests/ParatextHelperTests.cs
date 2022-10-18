<<<<<<< HEAD:Src/ScriptureUtilsTests/ParatextHelperTests.cs
// Copyright (c) 2011-2020 SIL International
||||||| f013144d5:Src/Common/ScriptureUtils/ScriptureUtilsTests/ParatextHelperTests.cs
// Copyright (c) 2011-2017 SIL International
=======
// Copyright (c) 2011-2021 SIL International
>>>>>>> develop:Src/Common/ScriptureUtils/ScriptureUtilsTests/ParatextHelperTests.cs
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
<<<<<<< HEAD:Src/ScriptureUtilsTests/ParatextHelperTests.cs
||||||| f013144d5:Src/Common/ScriptureUtils/ScriptureUtilsTests/ParatextHelperTests.cs
using Paratext;
using Paratext.LexicalClient;
=======
using Paratext;
using Paratext.LexicalClient;
using SIL.FieldWorks.Test.ProjectUnpacker;
>>>>>>> develop:Src/Common/ScriptureUtils/ScriptureUtilsTests/ParatextHelperTests.cs
using SIL.LCModel;
using SIL.LCModel.DomainServices;
<<<<<<< HEAD:Src/ScriptureUtilsTests/ParatextHelperTests.cs
using SIL.FieldWorks.Test.ProjectUnpacker;
using SIL.LCModel.Utils;
using SIL.PlatformUtilities;
using Rhino.Mocks;
||||||| f013144d5:Src/Common/ScriptureUtils/ScriptureUtilsTests/ParatextHelperTests.cs
using SIL.FieldWorks.Test.ProjectUnpacker;
using SIL.LCModel.Utils;
=======
using SIL.PlatformUtilities;
>>>>>>> develop:Src/Common/ScriptureUtils/ScriptureUtilsTests/ParatextHelperTests.cs

namespace SIL.FieldWorks.Common.ScriptureUtils
{
	#region MockParatextHelper class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class MockParatextHelper : IParatextHelper, IDisposable
	{
		/// <summary>The list of projects to simulate in Paratext</summary>
		public readonly List<IScrText> Projects = new List<IScrText>();

		/// <summary>Allows an implementation of LoadProjectMappings to be injected</summary>
		public IParatextHelper m_loadProjectMappingsImpl;

		#region Disposable stuff
		#if DEBUG
		/// <summary/>
		~MockParatextHelper()
		{
			Dispose(false);
		}
		#endif

		/// <summary/>
		public bool IsDisposed { get; private set; }

		/// <summary/>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary/>
		protected virtual void Dispose(bool fDisposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType() + ". *******");
			if (fDisposing && !IsDisposed)
			{

				Projects.Clear();
			}
			IsDisposed = true;
		}
		#endregion

		#region IParatextHelper implementation
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the Paratext project directory or null if unable to get the project directory
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string ProjectsDirectory
		{
			get { return Platform.IsUnix ? "~/MyParatextProjects/" : @"c:\My Paratext Projects\"; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Refreshes the projects.  (no-op)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void RefreshProjects()
		{
			// Nothing to do
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reloads the specified Paratext project with the latest data. (no-op)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void ReloadProject(IScrText project)
		{
			// Nothing to do
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the sorted list of Paratext short names.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IEnumerable<string> GetShortNames()
		{
			return Projects.Select(x => x.Name).OrderBy(s => s);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the list of Paratext projects.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IEnumerable<IScrText> GetProjects()
		{
			return Projects;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load the mappings for a Paratext 6/7 project into the specified list. (no-op)
		/// We never use this method; for tests, we use <c>Rhino.Mocks.MockRepository</c>
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void LoadProjectMappings(IScrImportSet importSettings)
		{
			if (m_loadProjectMappingsImpl == null)
				throw new NotImplementedException();
			m_loadProjectMappingsImpl.LoadProjectMappings(importSettings);
		}
		#endregion

		#region Other public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a dummy project to the simulated Paratext collection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void AddProject(string shortName)
		{
			AddProject(shortName, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a dummy project to the simulated Paratext collection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void AddProject(string shortName, string associatedProject)
		{
			AddProject(shortName, associatedProject, null, true, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a dummy project to the simulated Paratext collection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void AddProject(string shortName, string associatedProject, string baseProject)
		{
			AddProject(shortName, associatedProject, baseProject, true, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a dummy project to the simulated Paratext collection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void AddProject(string shortName, string associatedProject, string baseProject,
			bool editable, bool isResource)
		{
			AddProject(shortName, associatedProject, baseProject, editable, isResource, "0010000000");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a dummy project to the simulated Paratext collection.
		/// By default it is a standard project if there is no baseProject, otherwise a back translation
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void AddProject(string shortName, string associatedProject, string baseProject,
			bool editable, bool isResource, string booksPresent)
		{
			AddProject(shortName, associatedProject, baseProject, editable, isResource,
				booksPresent, string.IsNullOrEmpty(baseProject) ? Paratext.Data.ProjectType.Standard : Paratext.Data.ProjectType.BackTranslation);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a dummy project to the simulated Paratext collection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void AddProject(string shortName, string associatedProject, string baseProject,
			bool editable, bool isResource, string booksPresent, PtxUtils.Enum<Paratext.Data.ProjectType> translationType)
		{
			var scrText = MockRepository.GenerateMock<IScrText>();
			scrText.Stub(st => st.Name).Return(shortName);
			if (!string.IsNullOrEmpty(associatedProject))
			{
				var lexProj = MockRepository.GenerateMock<LexicalProject>();
				lexProj.Stub(lp => lp.ProjectId).Return(associatedProject);
				lexProj.Stub(lp => lp.ProjectType).Return("FieldWorks");
				lexProj.Stub(lp => lp.ToString()).Return($"FieldWorks:{lexProj.ProjectId}");
				scrText.Stub(st => st.AssociatedLexicalProject).Return(lexProj);
			}
			if (!string.IsNullOrEmpty(baseProject))
			{
				var baseProj = Projects.Select(x => x.Name == baseProject).FirstOrDefault();
				Assert.That(baseProj, Is.Not.Null);
				var translationInfoMock = MockRepository.GenerateStub<ITranslationInfo>(translationType, baseProject);
				translationInfoMock.Stub(ti => ti.BaseProjectName).Return(baseProject);
				scrText.Stub(st => st.TranslationInfo).Return(translationInfoMock);
			}
			else
			{
				var translationInfo = MockRepository.GenerateMock<ITranslationInfo>();
				if (Enum.TryParse(translationType.InternalValue, out ProjectType projectTypeEnum))
				{
					translationInfo.Stub(ti => ti.Type).Return(projectTypeEnum);
					scrText.TranslationInfo = translationInfo;
				}
				else
				{
					Assert.Fail("Testing unsupported Paratext.Data.ProjectType");
				}
			}

			scrText.Stub(st => st.Editable).Return(editable);
			scrText.SetParameterValue("ResourceText", isResource ? "T" : "F");
			if (booksPresent != null)
			{
				var booksPresentSet = MockRepository.GenerateMock<IScriptureProviderBookSet>();
				var books = new List<int>();
				for (var i = 0; i < booksPresent.Length; ++i)
				{
					if (booksPresent[i] == '1')
					{
						books.Add(i + 1); // Book numbers start at 1 not 0
					}
				}

				booksPresentSet.Stub(bps => bps.SelectedBookNumbers).Return(books);
				scrText.Stub(st => st.BooksPresentSet).Return(booksPresentSet);
			}

			Projects.Add(scrText);
		}
		#endregion
	}
	#endregion

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for the ParatextHelper class
	/// </summary>
	/// <remarks>TODO-Linux: Currently excluded on Linux because we get failing tests on build
	/// machine in fixture setup. Unfortunately there is no stack trace that would tell why it
	/// fails. Since Paratext isn't supported on Linux it's easiest to just ignore these tests
	/// for now.</remarks>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	[Platform(Exclude="Linux", Reason = "fails on Linux on build machine in fixture setup")]
	public class ParatextHelperUnitTests
	{
		private MockParatextHelper m_ptHelper;

		#region Setup/Teardown

		/// <summary/>
		public void FixtureTeardown()
		{
			ParatextHelper.Manager.Reset();
		}

		/// <summary/>
		[SetUp]
		public void Setup()
		{
			m_ptHelper = new MockParatextHelper();
			ParatextHelper.Manager.SetParatextHelperAdapter(m_ptHelper);
		}

		/// <summary/>
		[TearDown]
		public void TearDown()
		{
			m_ptHelper.Dispose();
		}
		#endregion

		#region Tests
		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetShortNames method on the ParatextHelper.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void GetShortNames()
		{
			m_ptHelper.AddProject("MNKY");
			m_ptHelper.AddProject("SOUP");
			m_ptHelper.AddProject("GRK");
			m_ptHelper.AddProject("Mony");
			ValidateEnumerable(ParatextHelper.ShortNames, new[] { "MNKY", "SOUP", "GRK", "Mony" });
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetAssociatedProject method on the ParatextHelper.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetAssociatedProject()
		{
			m_ptHelper.AddProject("MNKY", "Soup");
			m_ptHelper.AddProject("SOUP", "Monkey Soup");
			m_ptHelper.AddProject("GRK", "Levington");
			m_ptHelper.AddProject("Mony", "Money");
			IScrText found = ParatextHelper.GetAssociatedProject(new TestProjectId(BackendProviderType.kXML, "Monkey Soup"));
			Assert.AreEqual("SOUP", found.Name);
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the IsProjectWritable method on the ParatextHelper.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetProjectsWithBooks()
		{
			//                                                               1         2         3         4         5         6         7         8
			//                                                      12345678901234567890123456789012345678901234567890123456789012345678901234567890
			m_ptHelper.AddProject("MNKY", null, null, true, false, "0001001001010100010101010001110000000000000000000000000000000000000000000000");
			m_ptHelper.AddProject("SOUP", null, null, true, false, "0000000000000000000000000000000000000000000000000000000000000000000000000000");
			m_ptHelper.AddProject("DEUT", null, null, true, false, "0000000000000000000000000000000000000000000000000000000000000000001000000000");
			m_ptHelper.AddProject("RV8N", null, null, true, false, "0000000000000000000000000000000000000000000000000000000000000000010000000000");
			ValidateEnumerable(ParatextHelper.ProjectsWithBooks.Select(p => p.Name), new[] { "MNKY", "RV8N" });
		}
		#endregion

		#region Private helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Validates the specified enumerable to make sure it contains all of the specified
		/// expected values in any order.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static void ValidateEnumerable<T>(IEnumerable<T> enumerable, IEnumerable<T> expectedValues)
		{
			List<T> expectedValueList = new List<T>(expectedValues);
			foreach (T value in enumerable)
				Assert.IsTrue(expectedValueList.Remove(value), "Got unexpected value in enumerable: " + value);
			Assert.AreEqual(0, expectedValueList.Count);
		}
		#endregion
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Integration Tests for ParatextHelper's interactions with Paratext.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
	public class ParatextHelperTests : ScrInMemoryLcmTestBase
	{
		#region Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ability to save and reload the Scripture and BT Paratext 6 projects
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void LoadParatextMappings_NullProjectName()
		{
			IScrImportSet importSettings = Cache.ServiceLocator.GetInstance<IScrImportSetFactory>().Create();
			ParatextHelper.LoadProjectMappings(importSettings);
			Assert.That(importSettings.ParatextScrProj, Is.Null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
<<<<<<< HEAD:Src/ScriptureUtilsTests/ParatextHelperTests.cs
||||||| f013144d5:Src/Common/ScriptureUtils/ScriptureUtilsTests/ParatextHelperTests.cs
		/// Test the ability to save and reload the Scripture and BT Paratext 6 projects
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Category("LongRunning")]
		public void LoadParatextMappings_Normal()
		{
			if (ScriptureProvider.VersionInUse >= new Version(8, 0))
				Assert.Ignore("This test uses data that is only valid for Paratext7. The test fails with Paratext8 installed.");
			Unpacker.UnPackParatextTestProjects();

			var stylesheet = new LcmStyleSheet();
			stylesheet.Init(Cache, m_scr.Hvo, ScriptureTags.kflidStyles);
			IScrImportSet importSettings = Cache.ServiceLocator.GetInstance<IScrImportSetFactory>().Create();
			Cache.LangProject.TranslatedScriptureOA.ImportSettingsOC.Add(importSettings);
			importSettings.ParatextScrProj = "KAM";
			ParatextHelper.LoadProjectMappings(importSettings);

			ScrMappingList mappingList = importSettings.GetMappingListForDomain(ImportDomain.Main);
			// Test to see that the projects are set correctly
			Assert.AreEqual(44, mappingList.Count);

			Assert.AreEqual(MarkerDomain.Default, mappingList[@"\c"].Domain);
			Assert.AreEqual(MarkerDomain.Default, mappingList[@"\v"].Domain);
			Assert.AreEqual(@"\f*", mappingList[@"\f"].EndMarker);
			Assert.IsTrue(mappingList[@"\p"].IsInUse);
			Assert.IsFalse(mappingList[@"\tb2"].IsInUse);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ability to load a Paratext 6 project and distinguish between markers in use
		/// in the files and those that only come for them STY file, as well as making sure that
		/// the mappings are not in use when rescanning.
		/// Jiras task is TE-2439
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("GetMappingListForDomain is returning null after the merge from release/8.3 - This test was fixed in release/8.3 but likely didn't run on develop.")]
		public void LoadParatextMappings_MarkMappingsInUse()
		{
			if (ScriptureProvider.VersionInUse >= new Version(8, 0))
				Assert.Ignore("This test uses data that is only valid for Paratext7. The test fails with Paratext8 installed.");
			var stylesheet = new LcmStyleSheet();
			stylesheet.Init(Cache, m_scr.Hvo, ScriptureTags.kflidStyles);
			IScrImportSet importSettings = Cache.ServiceLocator.GetInstance<IScrImportSetFactory>().Create();
			Cache.LangProject.TranslatedScriptureOA.ImportSettingsOC.Add(importSettings);
			importSettings.ParatextScrProj = "TEV";
			ScrMappingList mappingList = importSettings.GetMappingListForDomain(ImportDomain.Main);
			Assert.NotNull(mappingList, "Setup Failure, no mapping list returned for the domain.");
			mappingList.Add(new ImportMappingInfo(@"\hahaha", @"\*hahaha", false,
				MappingTargetType.TEStyle, MarkerDomain.Default, "laughing",
				null, null, true, ImportDomain.Main));
			mappingList.Add(new ImportMappingInfo(@"\bthahaha", @"\*bthahaha", false,
				MappingTargetType.TEStyle, MarkerDomain.Default, "laughing",
				"en", null, true, ImportDomain.Main));

			Unpacker.UnPackParatextTestProjects();

			ParatextHelper.LoadProjectMappings(importSettings);

			Assert.IsTrue(mappingList[@"\c"].IsInUse);
			Assert.IsTrue(mappingList[@"\p"].IsInUse);
			Assert.IsFalse(mappingList[@"\ipi"].IsInUse);
			Assert.IsFalse(mappingList[@"\hahaha"].IsInUse,
				"In-use flag should have been cleared before re-scanning when the P6 project changed.");
			Assert.IsTrue(mappingList[@"\bthahaha"].IsInUse,
				"In-use flag should not have been cleared before re-scanning when the P6 project changed because it was in use by the BT.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
=======
		/// Test the ability to save and reload the Scripture and BT Paratext 6 projects
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Category("LongRunning")]
		public void LoadParatextMappings_Normal()
		{
			if (ScriptureProvider.VersionInUse >= new Version(8, 0))
				Assert.Ignore("This test uses data that is only valid for Paratext7. The test fails with Paratext8 installed.");
			Unpacker.UnPackParatextTestProjects();

			var stylesheet = new LcmStyleSheet();
			stylesheet.Init(Cache, m_scr.Hvo, ScriptureTags.kflidStyles);
			IScrImportSet importSettings = Cache.ServiceLocator.GetInstance<IScrImportSetFactory>().Create();
			Cache.LangProject.TranslatedScriptureOA.ImportSettingsOC.Add(importSettings);
			importSettings.ParatextScrProj = "KAM";
			ParatextHelper.LoadProjectMappings(importSettings);

			ScrMappingList mappingList = importSettings.GetMappingListForDomain(ImportDomain.Main);
			Assert.That(mappingList, Is.Not.Null, "Setup Failure, no mapping list returned for the domain.");
			// Test to see that the projects are set correctly
			Assert.AreEqual(44, mappingList.Count);

			Assert.AreEqual(MarkerDomain.Default, mappingList[@"\c"].Domain);
			Assert.AreEqual(MarkerDomain.Default, mappingList[@"\v"].Domain);
			Assert.AreEqual(@"\f*", mappingList[@"\f"].EndMarker);
			Assert.IsTrue(mappingList[@"\p"].IsInUse);
			Assert.IsFalse(mappingList[@"\tb2"].IsInUse);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ability to load a Paratext 6 project and distinguish between markers in use
		/// in the files and those that only come for them STY file, as well as making sure that
		/// the mappings are not in use when rescanning.
		/// Jiras task is TE-2439
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("GetMappingListForDomain is returning null after the merge from release/8.3 - This test was fixed in release/8.3 but likely didn't run on develop.")]
		public void LoadParatextMappings_MarkMappingsInUse()
		{
			if (ScriptureProvider.VersionInUse >= new Version(8, 0))
				Assert.Ignore("This test uses data that is only valid for Paratext7. The test fails with Paratext8 installed.");
			var stylesheet = new LcmStyleSheet();
			stylesheet.Init(Cache, m_scr.Hvo, ScriptureTags.kflidStyles);
			IScrImportSet importSettings = Cache.ServiceLocator.GetInstance<IScrImportSetFactory>().Create();
			Cache.LangProject.TranslatedScriptureOA.ImportSettingsOC.Add(importSettings);
			importSettings.ParatextScrProj = "TEV";
			ScrMappingList mappingList = importSettings.GetMappingListForDomain(ImportDomain.Main);
			Assert.NotNull(mappingList, "Setup Failure, no mapping list returned for the domain.");
			mappingList.Add(new ImportMappingInfo(@"\hahaha", @"\*hahaha", false,
				MappingTargetType.TEStyle, MarkerDomain.Default, "laughing",
				null, null, true, ImportDomain.Main));
			mappingList.Add(new ImportMappingInfo(@"\bthahaha", @"\*bthahaha", false,
				MappingTargetType.TEStyle, MarkerDomain.Default, "laughing",
				"en", null, true, ImportDomain.Main));

			Unpacker.UnPackParatextTestProjects();

			ParatextHelper.LoadProjectMappings(importSettings);

			Assert.IsTrue(mappingList[@"\c"].IsInUse);
			Assert.IsTrue(mappingList[@"\p"].IsInUse);
			Assert.IsFalse(mappingList[@"\ipi"].IsInUse);
			Assert.IsFalse(mappingList[@"\hahaha"].IsInUse,
				"In-use flag should have been cleared before re-scanning when the P6 project changed.");
			Assert.IsTrue(mappingList[@"\bthahaha"].IsInUse,
				"In-use flag should not have been cleared before re-scanning when the P6 project changed because it was in use by the BT.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
>>>>>>> develop:Src/Common/ScriptureUtils/ScriptureUtilsTests/ParatextHelperTests.cs
		/// Test attempting to load a Paratext project when the Paratext SSF references an
		/// encoding file that does not exist.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void LoadParatextMappings_MissingEncodingFile()
		{
			var stylesheet = new LcmStyleSheet();
			stylesheet.Init(Cache, m_scr.Hvo, ScriptureTags.kflidStyles);
			IScrImportSet importSettings = Cache.ServiceLocator.GetInstance<IScrImportSetFactory>().Create();
			Cache.LangProject.TranslatedScriptureOA.ImportSettingsOC.Add(importSettings);
			importSettings.ParatextScrProj = "NEC";

			Unpacker.UnPackMissingFileParatextTestProjects();

			ParatextHelper.LoadProjectMappings(importSettings);
			Assert.That(importSettings.ParatextScrProj, Is.Null);
		}
		#endregion
	}
}
