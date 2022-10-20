// Copyright (c) 2018-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Paratext.Data;
using Paratext.Data.ProjectFileAccess;
using Paratext.Data.ProjectSettingsAccess;
using Paratext.Data.Users;
using PtxUtils;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.LCModel;
using SIL.Scripture;
using ProjectType = Paratext.Data.ProjectType;

namespace Paratext8Plugin
{
	/// <summary>
	/// Stub class to simulate the Scripture Provider class.
	/// </summary>
	public class MockScriptureProvider
	{
		[Import]
		private ScriptureProvider.IScriptureProvider _potentialScriptureProvider;

		private static ScriptureProvider.IScriptureProvider _provider;
		private static AggregateCatalog _catalog;

		static MockScriptureProvider()
		{
			MockScriptureProvider provider = new MockScriptureProvider();
			_catalog = new AggregateCatalog();
			_catalog.Catalogs.Add(new DirectoryCatalog(TestContext.CurrentContext.TestDirectory, "Paratext8Plugin.dll"));
			using (CompositionContainer container = new CompositionContainer(_catalog))
			{
				container.SatisfyImportsOnce(provider);
			}
			_provider = provider._potentialScriptureProvider;
		}

		public static void Initialize()
		{
			_provider.Initialize();
		}

		public static void Deinitialize()
		{
			_catalog?.Dispose();
		}

		public static bool IsInstalled => _provider != null && _provider.IsInstalled;
	}

	class ConsoleAlert : Alert
	{
		protected override void ShowLaterInternal(string text, string caption, AlertLevel alertLevel)
		{
			Console.WriteLine(text);
		}

		protected override AlertResult ShowInternal(IComponent owner, string text, string caption, AlertButtons alertButtons,
			AlertLevel alertLevel, AlertDefaultButton defaultButton, bool showInTaskbar)
		{
			Console.WriteLine(text);
			return AlertResult.Negative;
		}
	}

	/// <summary>
	/// Tests to determine that the ParatextData dll is functioning as expected.
	/// </summary>
	[TestFixture]
	public class ParatextDataIntegrationTests
	{
		private sealed class MockScrTextCollectionImpl : ScrTextCollection
		{
			public MockScrTextCollectionImpl()
			{
				SettingsDirectoryInternal = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
			}

			public void AddProjectFolder(string project)
			{
				Directory.CreateDirectory(Path.Combine(SettingsDirectoryInternal, project));
			}
		}

		private sealed class MockZipPwProvider : IZippedResourcePasswordProvider
		{
			public string GetPassword()
			{
				return String.Empty;
			}
		}
		private sealed class MockResourceScrText : ScrText
		{

			public override bool IsResourceProject => true;

			public MockResourceScrText(string shortName, ParatextUser associatedUser, bool ignoreLoadErrors = false) : base(shortName, associatedUser, ignoreLoadErrors)
			{
			}
		}

		private sealed class MockParatext8Helper : IParatextHelper
		{
			private List<IScrText> m_projects = new List<IScrText>();
			public void AddProject(MockScrTextCollectionImpl scrTextCollection, string shortName,
				string associatedProject = null, string baseProject = null,
				bool editable = true, bool isResource = false, string booksPresent = null,
				Enum<ProjectType> translationType = default)
			{
				scrTextCollection.AddProjectFolder(shortName);
				var ptUser = new ParatextUser("Lydia", string.Empty);
				ScrText scrText;
				if (isResource)
				{
					scrText = new MockResourceScrText(shortName, ptUser, true);
				}
				else
				{
					scrText = new ScrText(shortName, ptUser, true);
				}
				scrText.Settings.MinParatextDataVersion = Version.Parse("8.1");
				if(!string.IsNullOrEmpty(associatedProject))
				{
					scrText.Settings.AssociatedLexicalProject = new AssociatedLexicalProject("FieldWorks", shortName);
				}

				if (!string.IsNullOrEmpty(baseProject))
				{
					scrText.Settings.TranslationInfo = new TranslationInformation(translationType, baseProject);
				}
				else
				{
					scrText.Settings.TranslationInfo = new TranslationInformation(translationType);
				}

				scrText.Settings.Editable = editable;
				scrText.Settings.ParametersDictionary["ResourceText"] = isResource ? "T" : "F";
				if (booksPresent != null)
				{
					scrText.Settings.BooksPresentSet = new BookSet(booksPresent);
				}
				m_projects.Add(new PT8ScrTextWrapper(scrText));
			}

			public void RefreshProjects() { }

			public void ReloadProject(IScrText project)
			{
				throw new NotImplementedException();
			}

			public IEnumerable<string> GetShortNames()
			{
				return m_projects.Select(p => p.Name);
			}

			public IEnumerable<IScrText> GetProjects()
			{
				return m_projects;
			}

			public void LoadProjectMappings(IScrImportSet importSettings)
			{
				throw new NotImplementedException();
			}

			public void AddProjects(MockScrTextCollectionImpl scrTextCollection)
			{
				AddProject(scrTextCollection, "MNKY");
				AddProject(scrTextCollection, "SOUP", associatedProject: "Monkey Soup", baseProject: null, editable: true, isResource: false);
				AddProject(scrTextCollection, "TWNS", associatedProject: null, baseProject: null, editable: false, isResource: false);
				AddProject(scrTextCollection, "LNDN", associatedProject: null, baseProject: null, editable: false, isResource: true);
				AddProject(scrTextCollection, "Mony", associatedProject: null, baseProject: null, editable: true, isResource: true);
			}
		}

		private string systemFwDir;
		private MockParatext8Helper m_ptHelper;

		[OneTimeSetUp]
		public void FixtureSetUp()
		{
			Alert.Implementation = new ConsoleAlert();
			TestContext.Out.WriteLine($"Setting FIELDWORKSDIR to {TestContext.CurrentContext.TestDirectory} for this fixture run");
			systemFwDir = Environment.GetEnvironmentVariable("FIELDWORKSDIR");
			Environment.SetEnvironmentVariable("FIELDWORKSDIR", TestContext.CurrentContext.TestDirectory);
		}

		[OneTimeTearDown]
		public void FixtureTeardown()
		{
			Environment.SetEnvironmentVariable("FIELDWORKSDIR", systemFwDir);
			MockScriptureProvider.Deinitialize();
		}

		[SetUp]
		public void Setup()
		{
			try
			{
				if (!ParatextInfo.IsParatextInstalled)
					Assert.Ignore("Paratext is not installed");
			}
			catch (Exception)
			{
				Assert.Ignore("ParatextData can't tell us if PT is installed.");
			}

			m_ptHelper = new MockParatext8Helper();
			ParatextHelper.Manager.SetParatextHelperAdapter(m_ptHelper);
			var mockScrTextCollection = new MockScrTextCollectionImpl();
			ScrTextCollection.Implementation = mockScrTextCollection;
			m_ptHelper.AddProjects(mockScrTextCollection);
		}

		[Test]
		public void ParatextCanInitialize()
		{
			try
			{
				MockScriptureProvider.Initialize();
			}
			catch (Exception e)
			{
				// A TypeInitializationException may also be thrown if ParaText 8 is not installed.
				Assert.False(MockScriptureProvider.IsInstalled, $"Paratext is installed but we couldn't initialize our provider because: {e.Message}");
				// A FileLoadException may indicate that ParatextData dependency (i.e. icu.net) has been updated to a new version.
				Assert.False(e.GetType().Name.Contains(typeof(FileLoadException).Name));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetWritableShortNames method on the ParatextHelper.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetWritableShortNames()
		{
			CollectionAssert.AreEquivalent(new [] {"MNKY"}, ParatextHelper.WritableShortNames);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the IsProjectWritable method on the ParatextHelper.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IsProjectWritable()
		{
			Assert.IsTrue(ParatextHelper.IsProjectWritable("MNKY"));
			Assert.IsFalse(ParatextHelper.IsProjectWritable("SOUP"));
			Assert.IsFalse(ParatextHelper.IsProjectWritable("TWNS"));
			Assert.IsFalse(ParatextHelper.IsProjectWritable("LNDN"));
			Assert.IsFalse(ParatextHelper.IsProjectWritable("Mony"));
		}
	}
}
