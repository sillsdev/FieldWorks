// Copyright (c) 2003-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.WritingSystems;
using SIL.WritingSystems;

// ReSharper disable StringIndexOfIsCultureSpecific.1
// ReSharper disable InconsistentNaming

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary/>
	[TestFixture]
	public class FwNewLangProjectModelTests
	{
		private const string DbName = "Maileingwidj2025";

		/// <summary/>
		[SetUp]
		public void TestSetup()
		{
			// A number of tests rely on this project not existing (most because duplicate names are not valid).
			if (DbExists(DbName))
			{
				DestroyDb(DbName, true);
			}
		}

		/// <summary>
		/// Make sure a new DB gets created
		/// </summary>
		[Test]
		public void FwNewLangProjectModel_VerifyCreateNewLangProject()
		{
			LcmCache cache = null;

			var testProject = new FwNewLangProjectModel(true)
			{
				LoadProjectNameSetup = () => { },
				LoadVernacularSetup = () => { },
				LoadAnalysisSetup = () => { },
				AnthroModel = new FwChooseAnthroListModel { CurrentList = FwChooseAnthroListModel.ListChoice.UserDef }
			};
			try
			{
				testProject.ProjectName = DbName;
				testProject.Next();
				testProject.SetDefaultWs(new LanguageInfo { LanguageTag = "fr" });
				testProject.Next();
				testProject.SetDefaultWs(new LanguageInfo { LanguageTag = "de" });
				using (var threadHelper = new ThreadHelper())
				{
					testProject.CreateNewLangProj(new DummyProgressDlg(), threadHelper);
				}

				Assert.IsTrue(DbExists(DbName));

				// despite of the name is DummyProgressDlg no real dialog (doesn't derive from Control), so
				// we don't need a 'using'
				cache = LcmCache.CreateCacheFromExistingData(
					new TestProjectId(BackendProviderType.kXMLWithMemoryOnlyWsMgr, DbFilename(DbName)), "en", new DummyLcmUI(),
					FwDirectoryFinder.LcmDirectories, new LcmSettings(), new DummyProgressDlg());
				CheckInitialSetOfPartsOfSpeech(cache);

				Assert.AreEqual(2, cache.ServiceLocator.WritingSystems.AnalysisWritingSystems.Count);
				Assert.AreEqual("German", cache.ServiceLocator.WritingSystems.AnalysisWritingSystems.First().LanguageName);
				Assert.AreEqual("English", cache.ServiceLocator.WritingSystems.AnalysisWritingSystems.Last().LanguageName);
				Assert.AreEqual(2, cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems.Count);
				Assert.AreEqual("German", cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.LanguageName);
				Assert.AreEqual("English", cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems.Last().LanguageName,
					"English should be selected as an analysis writing system even if the user tried to remove it");
				Assert.AreEqual(1, cache.ServiceLocator.WritingSystems.VernacularWritingSystems.Count);
				Assert.AreEqual("French", cache.ServiceLocator.WritingSystems.VernacularWritingSystems.First().LanguageName);
				Assert.AreEqual(1, cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems.Count);
				Assert.AreEqual("French", cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.LanguageName);
			}
			finally
			{
				// Blow away the database to clean things up
				if (cache != null)
					cache.Dispose();
				DestroyDb(DbName, false);
			}
		}

		/// <summary/>
		[TestCase("abcdefghijklmnopqrstuvwxyz_-_ABCDEFGHIJKLMNOPQRSTUVWXYZ", true)]
		[TestCase(null, false)]
		[TestCase("\\", false)]
		[TestCase("\"", false)]
		[TestCase("", false)]
		[TestCase(":", false)]
		[TestCase("/", false)]
		[TestCase("<", false)]
		[TestCase(">", false)]
		[TestCase("|", false)]
		[TestCase("?", false)]
		[TestCase("*", false)]
		[TestCase("franç", false)] // upper ASCII is forbidden
		[TestCase("\u0344", false)] // non-ASCII is forbidden
		public void FwNewLangProjectModel_ProjectNameIsValid(string projName, bool expectedResult)
		{
			var testModel = new FwNewLangProjectModel
			{
				LoadProjectNameSetup = () => { },
				ProjectName = projName
			};
			Assert.That(testModel.IsProjectNameValid, Is.EqualTo(expectedResult));
		}

		/// <summary/>
		[Test]
		public void FwNewLangProjectModel_ProjectNameIsUnique()
		{
			try
			{
				CreateDb(DbName);
				string errorMessage;
				Assert.True(FwNewLangProjectModel.CheckForUniqueProjectName("something else"), "unique name should be unique");
				Assert.False(FwNewLangProjectModel.CheckForUniqueProjectName(DbName), "duplicate name should not be unique");

				// Creating a new project is expensive (several seconds), so test this property that also checks uniqueness here:
				var testModel = new FwNewLangProjectModel
				{
					LoadProjectNameSetup = () => { },
					ProjectName = "something new"
				};
				Assert.True(testModel.IsProjectNameValid, "unique name should be valid");
				testModel.ProjectName = DbName;
				Assert.False(testModel.IsProjectNameValid, "duplicate name should not be valid");
			}
			finally
			{
				// Blow away the database to clean things up
				DestroyDb(DbName, false);
			}
		}

		/// <summary/>
		[Test]
		public void FwNewLangProjectModel_InvalidProjectNameMessage_Symbols()
		{
			var testModel = new FwNewLangProjectModel
			{
				LoadProjectNameSetup = () => { },
				ProjectName = "/"
			};
			var messageStart = FwCoreDlgs.ksIllegalNameMsg;
			var firstParamPos = messageStart.IndexOf("{");
			if (firstParamPos > 0)
			{
				messageStart = messageStart.Substring(0, firstParamPos - 1);
			}

			var messageActual = testModel.InvalidProjectNameMessage;
			Assert.That(messageActual, Is.StringStarting(messageStart));
			Assert.That(messageActual, Is.StringContaining(FwCoreDlgs.ksIllegalNameExplanation));
		}

		/// <summary/>
		[Test]
		public void FwNewLangProjectModel_InvalidProjectNameMessage_Diacritics()
		{
			var testModel = new FwNewLangProjectModel
			{
				LoadProjectNameSetup = () => { },
				ProjectName = "español"
			};
			var messageStart = FwCoreDlgs.ksIllegalNameWithDiacriticsMsg;
			var firstParamPos = messageStart.IndexOf("{");
			if (firstParamPos > 0)
			{
				messageStart = messageStart.Substring(0, firstParamPos - 1);
			}

			var messageActual = testModel.InvalidProjectNameMessage;
			Assert.That(messageActual, Is.StringStarting(messageStart));
			Assert.That(messageActual, Is.StringContaining(FwCoreDlgs.ksIllegalNameExplanation));
		}

		/// <summary/>
		[Test]
		public void FwNewLangProjectModel_InvalidProjectNameMessage_NonRoman()
		{
			var testModel = new FwNewLangProjectModel
			{
				LoadProjectNameSetup = () => { },
				ProjectName = "\u0344"
			};
			var messageStart = FwCoreDlgs.ksIllegalNameNonRomanMsg;
			var firstParamPos = messageStart.IndexOf("{");
			if (firstParamPos > 0)
			{
				messageStart = messageStart.Substring(0, firstParamPos - 1);
			}

			var messageActual = testModel.InvalidProjectNameMessage;
			Assert.That(messageActual, Is.StringStarting(messageStart));
			Assert.That(messageActual, Is.StringContaining(FwCoreDlgs.ksIllegalNameExplanation));
		}

		/// <summary/>
		[Test]
		public void FwNewLangProjectModel_CanFinish_TrueIfAllComplete()
		{
			var testModel = new FwNewLangProjectModel();
			foreach (var step in testModel.Steps)
			{
				step.IsComplete = true;
			}
			Assert.True(testModel.CanFinish());
		}

		/// <summary/>
		[Test]
		public void FwNewLangProjectModel_CanFinish_FalseIfNoneComplete()
		{
			var testModel = new FwNewLangProjectModel();
			foreach (var step in testModel.Steps)
			{
				step.IsComplete = false;
			}
			Assert.False(testModel.CanFinish());
		}

		/// <summary/>
		[Test]
		public void FwNewLangProjectModel_CanFinish_TrueIfAllNonOptionalComplete()
		{
			var testModel = new FwNewLangProjectModel();
			Assert.True(testModel.Steps.Any(step => step.IsOptional), "Test data is invalid, no optional steps present");
			foreach (var step in testModel.Steps)
			{
				if (!step.IsOptional)
				{
					step.IsComplete = true;
				}
				else
				{
					step.IsComplete = false;
				}
			}
			Assert.True(testModel.CanFinish());
		}

		/// <summary/>
		[Test]
		public void FwNewLangProjectModel_CanGoBack()
		{
			var model = new FwNewLangProjectModel();
			model.LoadProjectNameSetup = () => { };
			model.LoadVernacularSetup = () => { };
			model.ProjectName = DbName;
			Assert.False(model.CanGoBack());
			model.Next();
			Assert.True(model.CanGoBack());
			model.Back();
			Assert.False(model.CanGoBack());
		}

		/// <summary/>
		[Test]
		public void FwNewLangProjectModel_VernacularAndAnalysisSame_WarningIssued()
		{
			bool warningIssued = false;
			var model = new FwNewLangProjectModel();
			model.LoadProjectNameSetup = () => { };
			model.LoadVernacularSetup = () => { };
			model.LoadAnalysisSetup = () => { };
			model.LoadAnalysisSameAsVernacularWarning = () => { warningIssued = true; };
			model.ProjectName = DbName;
			var fakeTestWs = new CoreWritingSystemDefinition("fr");
			model.WritingSystemContainer.CurrentVernacularWritingSystems.Add(fakeTestWs);
			model.WritingSystemContainer.VernacularWritingSystems.Add(fakeTestWs);
			model.WritingSystemContainer.CurrentAnalysisWritingSystems.Add(fakeTestWs);
			model.WritingSystemContainer.AnalysisWritingSystems.Add(fakeTestWs);
			Assert.True(model.CanGoNext());
			model.Next(); // Move to choose default vernacular
			Assert.True(model.CanGoNext());
			model.Next(); // Move to choose default analysis
			model.SetDefaultWs(new LanguageInfo() {LanguageTag = "fr" });
			Assert.True(warningIssued, "Warning for analysis same as vernacular not triggered");
			Assert.True(model.CanGoNext()); // The user can ignore the warning
		}

		/// <summary/>
		[Test]
		public void FwNewLangProjectModel_CanGoNext()
		{
			var model = new FwNewLangProjectModel();
			model.LoadProjectNameSetup = () => { };
			model.LoadVernacularSetup = () => { };
			model.LoadAnalysisSetup = () => { };
			model.LoadAdvancedWsSetup = () => { };
			model.LoadAnthropologySetup = () => { };
			model.ProjectName = DbName;
			var fakeTestWs = new CoreWritingSystemDefinition("fr");
			model.WritingSystemContainer.CurrentVernacularWritingSystems.Add(fakeTestWs);
			model.WritingSystemContainer.VernacularWritingSystems.Add(fakeTestWs);
			model.WritingSystemContainer.CurrentAnalysisWritingSystems.Add(fakeTestWs);
			model.WritingSystemContainer.AnalysisWritingSystems.Add(fakeTestWs);
			Assert.True(model.CanGoNext());
			model.Next();
			Assert.True(model.CanGoNext());
			model.Next();
			Assert.True(model.CanGoNext());
			model.Next();
			Assert.True(model.CanGoNext());
			model.Next();
			Assert.False(model.CanGoNext());
		}

		/// <summary/>
		[Test]
		public void FwNewLangProjectModel_CannotClickFinishWithBlankProjectName()
		{
			var model = new FwNewLangProjectModel();
			model.LoadProjectNameSetup = () => { };
			model.LoadVernacularSetup = () => { };
			model.LoadAnalysisSetup = () => { };
			model.LoadAdvancedWsSetup = () => { };
			model.LoadAnthropologySetup = () => { };
			model.ProjectName = DbName;
			var fakeTestWs = new CoreWritingSystemDefinition("fr");
			model.WritingSystemContainer.CurrentVernacularWritingSystems.Add(fakeTestWs);
			model.WritingSystemContainer.VernacularWritingSystems.Add(fakeTestWs);
			model.WritingSystemContainer.CurrentAnalysisWritingSystems.Add(fakeTestWs);
			model.WritingSystemContainer.AnalysisWritingSystems.Add(fakeTestWs);
			Assert.True(model.CanGoNext());
			model.Next(); // Vernacular
			model.Next(); // Analysis
			Assert.True(model.CanFinish());
			model.Back();
			model.Back();
			model.ProjectName = "";
			Assert.False(model.CanFinish());
		}

		/// <summary/>
		[Test]
		public void SetDefaultWs_ReplacesExistingDefault()
		{
			var model = new FwNewLangProjectModel(true)
			{
				LoadProjectNameSetup = () => { },
				LoadVernacularSetup = () => { },
				ProjectName = DbName
			};
			model.Next();
			model.WritingSystemContainer.AddToCurrentVernacularWritingSystems(new CoreWritingSystemDefinition("fr"));
			model.WritingSystemContainer.AddToCurrentVernacularWritingSystems(new CoreWritingSystemDefinition("en"));
			model.SetDefaultWs(new LanguageInfo{LanguageTag = "de"});
			Assert.That(model.WritingSystemContainer.VernacularWritingSystems.Count, Is.EqualTo(2), "all");
			Assert.That(model.WritingSystemContainer.VernacularWritingSystems.First().LanguageTag, Is.EqualTo("de"), "first should be German");
			Assert.That(model.WritingSystemContainer.CurrentVernacularWritingSystems.Count, Is.EqualTo(2), "current");
			Assert.That(model.WritingSystemContainer.CurrentVernacularWritingSystems.First().LanguageTag, Is.EqualTo("de"),
				"default should be German");
		}

		/// <summary/>
		[Test]
		public void SetDefaultWs_ManyExist_OthersArePreservedInOrder()
		{
			var model = new FwNewLangProjectModel(true)
			{
				LoadProjectNameSetup = () => { },
				LoadVernacularSetup = () => { },
				ProjectName = DbName
			};
			model.Next();
			model.WritingSystemContainer.AddToCurrentVernacularWritingSystems(new CoreWritingSystemDefinition("en"));
			model.WritingSystemContainer.AddToCurrentVernacularWritingSystems(new CoreWritingSystemDefinition("de"));
			model.WritingSystemContainer.AddToCurrentVernacularWritingSystems(new CoreWritingSystemDefinition("fr"));
			model.SetDefaultWs(new LanguageInfo{LanguageTag = "de"});
			Assert.That(model.WritingSystemContainer.VernacularWritingSystems.Count, Is.EqualTo(3), "all");
			Assert.That(model.WritingSystemContainer.VernacularWritingSystems.First().LanguageTag, Is.EqualTo("de"), "first should be German");
			Assert.That(model.WritingSystemContainer.CurrentVernacularWritingSystems.Count, Is.EqualTo(3), "current");
			Assert.That(model.WritingSystemContainer.CurrentVernacularWritingSystems[0].LanguageTag, Is.EqualTo("de"), "default should be German");
			Assert.That(model.WritingSystemContainer.CurrentVernacularWritingSystems[1].LanguageTag, Is.EqualTo("en"), "econd should be English");
			Assert.That(model.WritingSystemContainer.CurrentVernacularWritingSystems[2].LanguageTag, Is.EqualTo("fr"), "third should be French");
		}

		/// <summary/>
		[Test]
		public void SetDefaultWs_DoesNotDuplicate()
		{
			var model = new FwNewLangProjectModel(true)
			{
				LoadProjectNameSetup = () => { },
				LoadVernacularSetup = () => { },
				ProjectName = DbName
			};
			model.Next();
			model.WritingSystemContainer.AddToCurrentVernacularWritingSystems(new CoreWritingSystemDefinition("de"));
			model.SetDefaultWs(new LanguageInfo{LanguageTag = "de"});
			Assert.That(model.WritingSystemContainer.VernacularWritingSystems.Count, Is.EqualTo(1), "should be only one WS");
			Assert.That(model.WritingSystemContainer.VernacularWritingSystems.First().LanguageTag, Is.EqualTo("de"), "should be German");
			Assert.That(model.WritingSystemContainer.CurrentVernacularWritingSystems.Count, Is.EqualTo(1), "should be only one current");
			Assert.That(model.WritingSystemContainer.CurrentVernacularWritingSystems.First().LanguageTag, Is.EqualTo("de"),
				"default should be German");
		}

		/// <summary/>
		[Test]
		public void SetDefaultWs_PreservesCheckState()
		{
			var model = new FwNewLangProjectModel(true)
			{
				LoadProjectNameSetup = () => { },
				LoadVernacularSetup = () => { },
				ProjectName = DbName
			};
			model.Next();
			var french = new CoreWritingSystemDefinition("fr");
			var esperanto = new CoreWritingSystemDefinition("eo");
			var english = new CoreWritingSystemDefinition("en");
			model.WritingSystemContainer.AddToCurrentVernacularWritingSystems(french); // selected
			model.WritingSystemContainer.AddToCurrentVernacularWritingSystems(esperanto); // selected
			model.WritingSystemContainer.VernacularWritingSystems.Add(english); // deselected
			model.SetDefaultWs(new LanguageInfo{LanguageTag = "de"});
			Assert.That(model.WritingSystemContainer.VernacularWritingSystems.Count, Is.EqualTo(3), "should be three total");
			Assert.That(model.WritingSystemContainer.VernacularWritingSystems.First().LanguageTag, Is.EqualTo("de"), "first should be German");
			Assert.That(model.WritingSystemContainer.VernacularWritingSystems.Contains(esperanto), "should contain Esperanto");
			Assert.That(model.WritingSystemContainer.VernacularWritingSystems.Contains(english), "should contain English");
			Assert.That(model.WritingSystemContainer.CurrentVernacularWritingSystems.Count, Is.EqualTo(2), "should be two selected");
			Assert.That(model.WritingSystemContainer.CurrentVernacularWritingSystems[0].LanguageTag, Is.EqualTo("de"), "default should be German");
			Assert.Contains(esperanto, (ICollection)model.WritingSystemContainer.CurrentVernacularWritingSystems, "Esperanto should be selected");
		}

		/// <summary/>
		[Test]
		public void SetDefaultWs_Analysis()
		{
			var model = new FwNewLangProjectModel(true)
			{
				LoadProjectNameSetup = () => { },
				LoadVernacularSetup = () => { },
				LoadAnalysisSetup = () => { },
				ProjectName = DbName
			};
			model.Next();
			model.WritingSystemContainer.AddToCurrentVernacularWritingSystems(new CoreWritingSystemDefinition("fr"));
			model.Next();
			// English is the default Analysis WS, so we do not need to set it here.
			Assert.That(model.WritingSystemContainer.AnalysisWritingSystems.Count, Is.EqualTo(1), "Test setup problem");
			Assert.That(model.WritingSystemContainer.AnalysisWritingSystems.First().LanguageTag, Is.EqualTo("en"), "Test setup problem");
			Assert.That(model.WritingSystemContainer.CurrentAnalysisWritingSystems.Count, Is.EqualTo(1), "Test setup problem");
			Assert.That(model.WritingSystemContainer.CurrentAnalysisWritingSystems.First().LanguageTag, Is.EqualTo("en"), "Test setup problem");
			model.SetDefaultWs(new LanguageInfo{LanguageTag = "de"});
			Assert.That(model.WritingSystemContainer.AnalysisWritingSystems.Count, Is.EqualTo(1), "all");
			Assert.That(model.WritingSystemContainer.AnalysisWritingSystems.First().LanguageTag, Is.EqualTo("de"), "should be German");
			Assert.That(model.WritingSystemContainer.CurrentAnalysisWritingSystems.Count, Is.EqualTo(1), "current");
			Assert.That(model.WritingSystemContainer.CurrentAnalysisWritingSystems[0].LanguageTag, Is.EqualTo("de"), "default should be German");
		}

		private static void CheckInitialSetOfPartsOfSpeech(LcmCache cache)
		{
			ILangProject lp = cache.LanguageProject;
			int iCount = 0;
			bool fAdverbFound = false;
			bool fNounFound = false;
			bool fProformFound = false;
			bool fVerbFound = false;
			foreach (IPartOfSpeech pos in lp.PartsOfSpeechOA.PossibilitiesOS)
			{
				iCount++;
				switch (pos.CatalogSourceId)
				{
					case "Adverb":
						fAdverbFound = true;
						break;
					case "Noun":
						fNounFound = true;
						break;
					case "Pro-form":
						fProformFound = true;
						break;
					case "Verb":
						fVerbFound = true;
						break;
					default:
						string sFmt = "Unexpected CatalogSourceId ({0}) found in PartOfSpeech {1}.";
						string sMsg = String.Format(sFmt, pos.CatalogSourceId, pos.Name.AnalysisDefaultWritingSystem);
						Assert.Fail(sMsg);
						break;
				}
			}
			Assert.AreEqual(4, iCount, "Expect four initial POSes.");
			Assert.IsTrue(fAdverbFound, "Did not find Adverb CatalogSourceId");
			Assert.IsTrue(fNounFound, "Did not find Noun CatalogSourceId");
			Assert.IsTrue(fProformFound, "Did not find Pro-form CatalogSourceId");
			Assert.IsTrue(fVerbFound, "Did not find Verb CatalogSourceId");
		}

		private static void CreateDb(string dbName, string vernWs = "fr")
		{
			var testProject = new FwNewLangProjectModel(true)
			{
				LoadProjectNameSetup = () => { },
				LoadVernacularSetup = () => { },
				LoadAnalysisSetup = () => { },
				ProjectName = dbName,
				AnthroModel = new FwChooseAnthroListModel { CurrentList = FwChooseAnthroListModel.ListChoice.UserDef }
			};
			testProject.Next();
			testProject.SetDefaultWs(new LanguageInfo { LanguageTag = vernWs });
			testProject.Next();
			using (var threadHelper = new ThreadHelper())
			{
				testProject.CreateNewLangProj(new DummyProgressDlg(), threadHelper);
			}
		}

		private static string DbDirectory(string dbName)
		{
			return Path.Combine(FwDirectoryFinder.ProjectsDirectory, dbName);
		}

		private static string DbFilename(string dbName)
		{
			return Path.Combine(DbDirectory(dbName), LcmFileHelper.GetXmlDataFileName(dbName));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// See if the given FW database exists by attempting to establish a connection to it.
		/// </summary>
		/// <param name="dbName">Name of the FW database to look for</param>
		/// <returns>true iff the underlying file exists.</returns>
		/// ------------------------------------------------------------------------------------
		private static bool DbExists(string dbName)
		{
			string sFile = DbFilename(dbName);
			return File.Exists(sFile);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Blow away the given FW database.
		/// </summary>
		/// <param name="dbName">Name of the FW database to smoke</param>
		/// <param name="failureIsFatal">If true, then failure to delete will fail tests.</param>
		/// ------------------------------------------------------------------------------------
		internal static void DestroyDb(string dbName, bool failureIsFatal)
		{
			try
			{
				string dir = DbDirectory(dbName);
				Directory.Delete(dir, true);
			}
			catch
			{
				string msg = "The test database " + dbName + " could not be deleted.";
				if (failureIsFatal)
					Assert.Fail(msg);
				else
					Debug.WriteLine(msg);
			}
		}
	}
}
