// Copyright (c) 2011-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Paratext;
using Paratext.LexicalClient;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.FieldWorks.Test.ProjectUnpacker;
using SIL.LCModel.Utils;

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
				// dispose managed and unmanaged objects
				foreach (var scrText in Projects)
					((PT7ScrTextWrapper)scrText).DisposePTObject();

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
			get { return MiscUtils.IsUnix ? "~/MyParatextProjects/" : @"c:\My Paratext Projects\"; }
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
				booksPresent, string.IsNullOrEmpty(baseProject) ? Paratext.ProjectType.Standard : Paratext.ProjectType.BackTranslation);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a dummy project to the simulated Paratext collection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void AddProject(string shortName, string associatedProject, string baseProject,
			bool editable, bool isResource, string booksPresent, Utilities.Enum<Paratext.ProjectType> translationType)
		{
			ScrText scrText = new ScrText();
			scrText.Name = shortName;

			if (!string.IsNullOrEmpty(associatedProject))
				scrText.AssociatedLexicalProject = new AssociatedLexicalProject(LexicalAppType.FieldWorks, associatedProject);
			// Don't know how to implement a test involving baseProject now that BaseTranslation is gone from the PT API.
			// However all clients I can find so far pass null.
			if (!string.IsNullOrEmpty(baseProject))
			{
				//scrText.BaseTranslation = new BaseTranslation(derivedTranslationType, baseProject, string.Empty);
				var baseProj = Projects.Select(x => x.Name == baseProject).FirstOrDefault();
				Assert.That(baseProj, Is.Not.Null);
				scrText.TranslationInfo = new TranslationInformation(translationType, baseProject, string.Empty);
				Assert.That(scrText.TranslationInfo.BaseProjectName, Is.EqualTo(baseProject));
			}
			else
			{
				scrText.TranslationInfo = new TranslationInformation(translationType);
			}

			scrText.Editable = editable;
			scrText.SetParameterValue("ResourceText", isResource ? "T" : "F");
			if (booksPresent != null)
				scrText.BooksPresent = booksPresent;

			Projects.Add(new PT7ScrTextWrapper(scrText));
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
		/// Tests the GetWritableShortNames method on the ParatextHelper.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetWritableShortNames()
		{
			if (ScriptureProvider.VersionInUse >= new Version(8, 0))
				Assert.Ignore("This test is insufficiently mocked and uses Paratext7 data with Paratext8 logic if Paratext8 is installed.");
			m_ptHelper.AddProject("MNKY");
			m_ptHelper.AddProject("SOUP", "Monkey Soup", null, true, false);
			m_ptHelper.AddProject("TWNS", null, null, false, false);
			m_ptHelper.AddProject("LNDN", null, null, false, true);
			m_ptHelper.AddProject("Mony", null, null, true, true);
			m_ptHelper.AddProject("Grk7"); // Considered a source language text so should be ignored
			m_ptHelper.AddProject("Sup");

			ValidateEnumerable(ParatextHelper.WritableShortNames, new[] { "MNKY", "Sup" });
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the IsProjectWritable method on the ParatextHelper.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IsProjectWritable()
		{
			if (ScriptureProvider.VersionInUse >= new Version(8, 0))
				Assert.Ignore("This test is insufficiently mocked and uses Paratext7 data with Paratext8 logic if Paratext8 is installed.");
			m_ptHelper.AddProject("MNKY");
			m_ptHelper.AddProject("SOUP", "Monkey Soup", null, true, false);
			m_ptHelper.AddProject("TWNS", null, null, false, false);
			m_ptHelper.AddProject("LNDN", null, null, false, true);
			m_ptHelper.AddProject("Mony", null, null, true, true);
			m_ptHelper.AddProject("Grk7"); // Considered a source language text so should be ignored

			Assert.IsTrue(ParatextHelper.IsProjectWritable("MNKY"));
			Assert.IsFalse(ParatextHelper.IsProjectWritable("SOUP"));
			Assert.IsFalse(ParatextHelper.IsProjectWritable("TWNS"));
			Assert.IsFalse(ParatextHelper.IsProjectWritable("LNDN"));
			Assert.IsFalse(ParatextHelper.IsProjectWritable("Mony"));
			Assert.IsFalse(ParatextHelper.IsProjectWritable("Grk7"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the IsProjectWritable method on the ParatextHelper.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetProjectsWithBooks()
		{
			//                                                         1         2         3         4         5         6         7         8
			//                                                12345678901234567890123456789012345678901234567890123456789012345678901234567890
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
