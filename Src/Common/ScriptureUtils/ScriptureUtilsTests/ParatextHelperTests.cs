// Copyright (c) 2011-2021 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Utilities;
using Paratext;
using Paratext.LexicalClient;
using SIL.FieldWorks.Test.ProjectUnpacker;
using SIL.LCModel;
using SIL.LCModel.DomainServices;

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

		private sealed class TestScrText : IScrText
		{
			private sealed class TestLexicalProject : ILexicalProject
			{
				public TestLexicalProject(string projectType, string projectId)
				{
					ProjectType = projectType ?? string.Empty;
					ProjectId = projectId ?? string.Empty;
				}

				public string ProjectId { get; }
				public string ProjectType { get; }

				public override string ToString() => $"{ProjectType}:{ProjectId}";
			}

			private sealed class TestTranslationInfo : ITranslationInfo
			{
				public TestTranslationInfo(string baseProjectName, ProjectType type)
				{
					BaseProjectName = baseProjectName ?? string.Empty;
					Type = type;
				}

				public string BaseProjectName { get; }
				public ProjectType Type { get; }
			}

			private sealed class TestBookSet : IScriptureProviderBookSet
			{
				private readonly string m_booksPresent;
				public TestBookSet(string booksPresent)
				{
					m_booksPresent = booksPresent ?? string.Empty;
				}

				public IEnumerable<int> SelectedBookNumbers => Enumerable.Range(1, m_booksPresent.Length)
					.Where(i => m_booksPresent[i - 1] == '1');
			}

			public TestScrText(
				string name,
				string associatedProjectId,
				bool editable,
				bool isResourceText,
				string booksPresent,
				ProjectType translationType,
				string baseProjectName)
			{
				Name = name;
				Editable = editable;
				IsResourceText = isResourceText;
				BooksPresent = booksPresent;
				BooksPresentSet = new TestBookSet(booksPresent);
				AssociatedLexicalProject = string.IsNullOrEmpty(associatedProjectId)
					? new TestLexicalProject(string.Empty, string.Empty)
					: new TestLexicalProject("FieldWorks", associatedProjectId);
				TranslationInfo = new TestTranslationInfo(baseProjectName, translationType);
			}

			public void Reload() { }
			public IScriptureProviderStyleSheet DefaultStylesheet => null;
			public IScriptureProviderParser Parser => null;
			public IScriptureProviderBookSet BooksPresentSet { get; }
			public string Name { get; set; }
			public ILexicalProject AssociatedLexicalProject { get; set; }
			public ITranslationInfo TranslationInfo { get; set; }
			public bool Editable { get; set; }
			public bool IsResourceText { get; }
			public string Directory => string.Empty;
			public string BooksPresent { get; set; }
			public IScrVerse Versification => null;
			public string JoinedNameAndFullName => Name;
			public string FileNamePrePart => string.Empty;
			public string FileNameForm => string.Empty;
			public string FileNamePostPart => string.Empty;
			public object CoreScrText => null;
			public void SetParameterValue(string resourcetext, string s) { }
			public bool BookPresent(int bookCanonicalNum) => false;
			public bool IsCheckSumCurrent(int bookCanonicalNum, string checksum) => false;
			public string GetBookCheckSum(int canonicalNum) => string.Empty;
		}

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
			get { return SIL.PlatformUtilities.Platform.IsUnix ? "~/MyParatextProjects/" : @"c:\My Paratext Projects\"; }
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
		/// We never use this method; for tests, we use <c>Moq</c>
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
			var projectType = string.IsNullOrEmpty(baseProject)
				? (isResource ? Paratext.ProjectType.Resource : Paratext.ProjectType.Standard)
				: Paratext.ProjectType.BackTranslation;
			AddProject(shortName, associatedProject, baseProject, editable, isResource, booksPresent, projectType);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a dummy project to the simulated Paratext collection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void AddProject(string shortName, string associatedProject, string baseProject,
			bool editable, bool isResource, string booksPresent, Utilities.Enum<Paratext.ProjectType> translationType)
		{
			var baseProjectName = baseProject ?? string.Empty;
			var fwProjectType = Enum.TryParse(translationType.ToString(), out ProjectType parsedType)
				? parsedType
				: ProjectType.Unknown;
			Projects.Add(new TestScrText(shortName, associatedProject, editable, isResource, booksPresent, fwProjectType, baseProjectName));
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
			AppDomain.CurrentDomain.SetData(ScriptureProvider.TestProviderDataKey, new TestScriptureProvider(m_ptHelper));
			ParatextHelper.Manager.SetParatextHelperAdapter(m_ptHelper);
		}

		/// <summary/>
	internal class TestScriptureProvider : ScriptureProvider.IScriptureProvider
	{
		private readonly MockParatextHelper m_helper;

		public TestScriptureProvider(MockParatextHelper helper)
		{
			m_helper = helper;
		}

		public string SettingsDirectory => string.Empty;

		public IEnumerable<string> NonEditableTexts => new[] { "grk7" };

		public IEnumerable<string> ScrTextNames => m_helper.Projects.Select(p => p.Name);

		public void Initialize()
		{
		}

		public void RefreshScrTexts()
		{
		}

		public IEnumerable<IScrText> ScrTexts()
		{
			return m_helper.Projects;
		}

		public IScrText Get(string project)
		{
			return m_helper.Projects.FirstOrDefault(p => p.Name == project);
		}

		public IScrText MakeScrText(string paratextProjectId)
		{
			return new PT7ScrTextWrapper(new ScrText { Name = paratextProjectId });
		}

		public ScriptureProvider.IScriptureProviderParserState GetParserState(IScrText ptProjectText, IVerseRef ptCurrBook)
		{
			throw new NotSupportedException();
		}

		public IVerseRef MakeVerseRef(int bookNum, int i, int i1)
		{
			return new PT7VerseRefWrapper(new VerseRef(bookNum, i, i1));
		}

		public Version MaximumSupportedVersion => new Version(7, 0);

		public bool IsInstalled => true;
	}

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
			Assert.That(found.Name, Is.EqualTo("SOUP"));
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
			m_ptHelper.AddProject("Grk7", null, null, false, false); // Considered a source language text so should be ignored
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
			m_ptHelper.AddProject("Grk7", null, null, false, false); // Considered a source language text so should be ignored

			Assert.That(ParatextHelper.IsProjectWritable("MNKY"), Is.True);
			Assert.That(ParatextHelper.IsProjectWritable("SOUP"), Is.False);
			Assert.That(ParatextHelper.IsProjectWritable("TWNS"), Is.False);
			Assert.That(ParatextHelper.IsProjectWritable("LNDN"), Is.False);
			Assert.That(ParatextHelper.IsProjectWritable("Mony"), Is.False);
			Assert.That(ParatextHelper.IsProjectWritable("Grk7"), Is.False);
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
				Assert.That(expectedValueList.Remove(value), Is.True, "Got unexpected value in enumerable: " + value);
			Assert.That(expectedValueList.Count, Is.EqualTo(0));
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
			Assert.That(mappingList, Is.Not.Null, "Setup Failure, no mapping list returned for the domain.");
			// Test to see that the projects are set correctly
			Assert.That(mappingList.Count, Is.EqualTo(44));

			Assert.That(mappingList[@"\c"].Domain, Is.EqualTo(MarkerDomain.Default));
			Assert.That(mappingList[@"\v"].Domain, Is.EqualTo(MarkerDomain.Default));
			Assert.That(mappingList[@"\f"].EndMarker, Is.EqualTo(@"\f*"));
			Assert.That(mappingList[@"\p"].IsInUse, Is.True);
			Assert.That(mappingList[@"\tb2"].IsInUse, Is.False);
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
			Assert.That(mappingList, Is.Not.Null, "Setup Failure, no mapping list returned for the domain.");
			mappingList.Add(new ImportMappingInfo(@"\hahaha", @"\*hahaha", false,
				MappingTargetType.TEStyle, MarkerDomain.Default, "laughing",
				null, null, true, ImportDomain.Main));
			mappingList.Add(new ImportMappingInfo(@"\bthahaha", @"\*bthahaha", false,
				MappingTargetType.TEStyle, MarkerDomain.Default, "laughing",
				"en", null, true, ImportDomain.Main));

			Unpacker.UnPackParatextTestProjects();

			ParatextHelper.LoadProjectMappings(importSettings);

			Assert.That(mappingList[@"\c"].IsInUse, Is.True);
			Assert.That(mappingList[@"\p"].IsInUse, Is.True);
			Assert.That(mappingList[@"\ipi"].IsInUse, Is.False);
			Assert.That(mappingList[@"\hahaha"].IsInUse, Is.False, "In-use flag should have been cleared before re-scanning when the P6 project changed.");
			Assert.That(mappingList[@"\bthahaha"].IsInUse, Is.True, "In-use flag should not have been cleared before re-scanning when the P6 project changed because it was in use by the BT.");
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
