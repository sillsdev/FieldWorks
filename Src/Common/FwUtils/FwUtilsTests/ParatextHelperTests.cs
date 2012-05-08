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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using NUnit.Framework;
using Paratext;
using Paratext.DerivedTranslation;
using Paratext.LexicalClient;
using SIL.FieldWorks.Test.TestUtils;
using SIL.Utils;

namespace SIL.FieldWorks.Common.FwUtils
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
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void AddProject(string shortName, string associatedProject, string baseProject,
			bool editable, bool isResource, string booksPresent)
		{
			AddProject(shortName, associatedProject, baseProject, editable, isResource,
				booksPresent, DerivedTranslationType.BackTranslation);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a dummy project to the simulated Paratext collection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="ScrText gets added to Projects collection and disposed there")]
		public void AddProject(string shortName, string associatedProject, string baseProject,
			bool editable, bool isResource, string booksPresent, DerivedTranslationType derivedTranslationType)
		{
			ScrText scrText = new ScrText();
			scrText.Name = shortName;

			if (!string.IsNullOrEmpty(associatedProject))
				scrText.AssociatedLexicalProject = new AssociatedLexicalProject(LexicalAppType.FieldWorks, associatedProject);
			if (!string.IsNullOrEmpty(baseProject))
				scrText.BaseTranslation = new BaseTranslation(derivedTranslationType, baseProject, string.Empty);

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
	public class ParatextHelperTests : BaseTest
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
}
