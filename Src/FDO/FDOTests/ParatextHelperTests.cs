// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2011' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ParatextHelperTests.cs
// Responsibility: FW Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using NUnit.Framework;
using Paratext;
using Paratext.DerivedTranslation;
using Paratext.LexicalClient;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.Test.ProjectUnpacker;
using SIL.FieldWorks.Test.TestUtils;
using SIL.Utils;

namespace SIL.FieldWorks.FDO.FDOTests
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
		public readonly List<ScrText> Projects = new List<ScrText>();

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
					scrText.Dispose();

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
		public void ReloadProject(ScrText project)
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
		public IEnumerable<ScrText> GetProjects()
		{
			return Projects;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load the mappings for a Paratext 6/7 project into the specified list. (no-op)
		/// We never use this method; for tests, we use <c>Rhino.Mocks.MockRepository</c>
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool LoadProjectMappings(string project, ScrMappingList mappingList, ImportDomain domain)
		{
			if (m_loadProjectMappingsImpl == null)
				throw new NotImplementedException();
			return m_loadProjectMappingsImpl.LoadProjectMappings(project, mappingList, domain);
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
				booksPresent, string.IsNullOrEmpty(baseProject) ? ProjectType.Standard : ProjectType.BackTranslation);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a dummy project to the simulated Paratext collection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="ScrText gets added to Projects collection and disposed there")]
		public void AddProject(string shortName, string associatedProject, string baseProject,
			bool editable, bool isResource, string booksPresent, Utilities.Enum<ProjectType> translationType)
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
				scrText.TranslationInfo = new TranslationInformation(translationType,
					baseProject, string.Empty);
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
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification="Unit test - m_ptHelper gets disposed in TearDown()")]
	public class ParatextHelperUnitTests : BaseTest
	{
		private MockParatextHelper m_ptHelper;

		#region Setup/Teardown
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void FixtureTeardown()
		{
			base.FixtureTeardown();
			ParatextHelper.Manager.Reset();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
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
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="found is a reference")]
		public void GetAssociatedProject()
		{
			m_ptHelper.AddProject("MNKY", "Soup");
			m_ptHelper.AddProject("SOUP", "Monkey Soup");
			m_ptHelper.AddProject("GRK", "Levington");
			m_ptHelper.AddProject("Mony", "Money");
			ScrText found = ParatextHelper.GetAssociatedProject(new TestProjectId(FDOBackendProviderType.kXML, "Monkey Soup"));
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
	public class ParatextHelperTests : ScrInMemoryFdoTestBase
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
			Assert.IsFalse(ParatextHelper.LoadProjectMappings(null, null, ImportDomain.Main));
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
			Unpacker.UnPackParatextTestProjects();

			FwStyleSheet stylesheet = new FwStyleSheet();
			stylesheet.Init(Cache, m_scr.Hvo, ScriptureTags.kflidStyles);
			ScrMappingList mappingList = new ScrMappingList(MappingSet.Main, stylesheet);

			Assert.IsTrue(ParatextHelper.LoadProjectMappings("KAM", mappingList, ImportDomain.Main));

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
		[Category("LongRunning")]
		[Ignore("Has not been run for a while and no longer works; possibly obsolete")]
		public void LoadParatextMappings_MarkMappingsInUse()
		{
			FwStyleSheet stylesheet = new FwStyleSheet();
			stylesheet.Init(Cache, m_scr.Hvo, ScriptureTags.kflidStyles);
			ScrMappingList mappingList = new ScrMappingList(MappingSet.Main, stylesheet);
			mappingList.Add(new ImportMappingInfo(@"\hahaha", @"\*hahaha", false,
				MappingTargetType.TEStyle, MarkerDomain.Default, "laughing",
				null, null, true, ImportDomain.Main));
			mappingList.Add(new ImportMappingInfo(@"\bthahaha", @"\*bthahaha", false,
				MappingTargetType.TEStyle, MarkerDomain.Default, "laughing",
				"en", null, true, ImportDomain.Main));

			Unpacker.UnPackParatextTestProjects();
			Assert.IsTrue(ParatextHelper.LoadProjectMappings("TEV", mappingList, ImportDomain.Main));

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
		/// Test attempting to load a Paratext project when the Paratext SSF references an
		/// encoding file that does not exist.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Category("LongRunning")]
		public void LoadParatextMappings_MissingEncodingFile()
		{
			FwStyleSheet stylesheet = new FwStyleSheet();
			stylesheet.Init(Cache, m_scr.Hvo, ScriptureTags.kflidStyles);
			ScrMappingList mappingList = new ScrMappingList(MappingSet.Main, stylesheet);

			Unpacker.UnPackMissingFileParatextTestProjects();
			Assert.IsFalse(ParatextHelper.LoadProjectMappings("NEC", mappingList, ImportDomain.Main));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test attempting to load a Paratext project when the Paratext SSF references a
		/// style file that does not exist.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("Causes build to hang since Paratext code displays a 'missing style file' message box")]
		public void LoadParatextMappings_MissingStyleFile()
		{
			FwStyleSheet stylesheet = new FwStyleSheet();
			stylesheet.Init(Cache, m_scr.Hvo, ScriptureTags.kflidStyles);
			ScrMappingList mappingList = new ScrMappingList(MappingSet.Main, stylesheet);

			Unpacker.UnPackMissingFileParatextTestProjects();
			Assert.IsFalse(ParatextHelper.LoadProjectMappings("NSF", mappingList, ImportDomain.Main));
		}
		#endregion
	}
}
