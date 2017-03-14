// Copyright (c) 2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.Utils;
// ReSharper disable InconsistentNaming

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Tests for DictionaryConfigurationImportController.
	/// LT-17397.
	/// These tests write to disk, not just in memory, so they can use the zip library.
	/// </summary>
	public class DictionaryConfigurationImportControllerTests : MemoryOnlyBackendProviderReallyRestoredForEachTestTestBase
	{
		private DictionaryConfigurationImportController _controller;
		private string _projectConfigPath;
		private readonly string _defaultConfigPath = Path.Combine(FwDirectoryFinder.DefaultConfigurations, "Dictionary");
		private const string configLabel = "importexportConfiguration";
		private const string configFilename = "importexportConfigurationFile.fwdictconfig";

		/// <summary>
		/// Zip file to import during testing.
		/// </summary>
		private string _zipFile;

		/// <summary>
		/// Path to a dictionary configuration file that will be deleted after every test.
		/// </summary>
		private string _pathToConfiguration;

		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			FileUtils.EnsureDirectoryExists(_defaultConfigPath);
		}

		[TestFixtureTearDown]
		public override void FixtureTeardown()
		{
			base.FixtureTeardown();
		}

		[SetUp]
		public void Setup()
		{
			// Start out with a clean project configuration directory, and with a non-random name so it's easier to examine during testing.
			_projectConfigPath = Path.Combine(Path.GetTempPath(), "dictionaryConfigurationImportTests");
			if (Directory.Exists(_projectConfigPath))
				Directory.Delete(_projectConfigPath, true);
			FileUtils.EnsureDirectoryExists(_projectConfigPath);

			_controller = new DictionaryConfigurationImportController(Cache, _projectConfigPath,
				new List<DictionaryConfigurationModel>());

			// Set up data for import testing.

			_zipFile = null;

			// Prepare configuration to export

			var configurationToExport = new DictionaryConfigurationModel
			{
				Label = configLabel,
				Publications = new List<string> { "Main Dictionary", "unknown pub 1", "unknown pub 2" },
				Parts = new List<ConfigurableDictionaryNode> { new ConfigurableDictionaryNode { FieldDescription = "LexEntry" } },
				FilePath = Path.GetTempPath() + configFilename
			};
			CssGeneratorTests.PopulateFieldsForTesting(configurationToExport);

			_pathToConfiguration = configurationToExport.FilePath;

			// Create XML file
			configurationToExport.Save();

			// Export a configuration that we know how to import

			_zipFile = Path.GetTempFileName();

			// Add a test style to the cache
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var styleFactory = Cache.ServiceLocator.GetInstance<IStStyleFactory>();
				var testStyle = styleFactory.Create(Cache.LangProject.StylesOC, "TestStyle", ContextValues.InternalConfigureView, StructureValues.Body,
					FunctionValues.Line, true, 2, false);
				testStyle.Usage.set_String(Cache.DefaultAnalWs, "Test Style");

				DictionaryConfigurationManagerController.ExportConfiguration(configurationToExport, _zipFile, Cache);
				Cache.LangProject.StylesOC.Clear();
			});
			Assert.That(File.Exists(_zipFile), "Unit test not set up right");
			Assert.That(new FileInfo(_zipFile).Length, Is.GreaterThan(0), "Unit test not set up right");

			// Clear the configuration away so we can see it gets imported

			File.Delete(_pathToConfiguration);
			Assert.That(!File.Exists(_pathToConfiguration),
				"Unit test not set up right. Configuration should be out of the way for testing export.");
			Assert.That(_controller._configurations.All(config => config.Label != configLabel),
				"Unit test not set up right. A config exists with the same label as the config we will import.");
			Assert.That(_controller._configurations.All(config => config.Label != configLabel),
				"Unit test set up unexpectedly. Such a config should not be registered.");
		}

		[TearDown]
		public void TearDown()
		{
			if (_zipFile != null)
				File.Delete(_zipFile);
			if (_pathToConfiguration != null)
				File.Delete(_pathToConfiguration);
		}

		[Test]
		public void PrepareImport_PreparedDataForImport()
		{
			// SUT
			_controller.PrepareImport(_zipFile);

			Assert.That(_controller.NewConfigToImport, Is.Not.Null, "Failed to create config object");
			Assert.That(_controller.NewConfigToImport.Label, Is.EqualTo(configLabel), "Failed to process data to be imported");
			Assert.That(_controller._originalConfigLabel, Is.EqualTo(configLabel),
				"Failed to describe original label from data to import.");
			Assert.That(_controller.ImportHappened, Is.False, "Import hasn't actually happened yet, so don't claim it has");
		}

		[Test]
		public void PrepareImport_IncludesUnknownPublications()
		{
			// SUT
			_controller.PrepareImport(_zipFile);

			Assert.That(_controller.NewConfigToImport.Publications.Count, Is.EqualTo(3), "Did not include all the publications");
			Assert.That(_controller.NewConfigToImport.Publications[1], Is.EqualTo("unknown pub 1"), "unexpected publication");
			Assert.That(_controller.NewConfigToImport.Publications[2], Is.EqualTo("unknown pub 2"), "unexpected publication");
			Assert.That(_controller._newPublications.Contains("unknown pub 1"), "Did not record new publication being added");
			Assert.That(_controller._newPublications.Contains("unknown pub 2"), "Did not record new publication being added");
			Assert.That(_controller._newPublications.Contains("Main Dictionary"), Is.False, "Incorrectly recorded existing publication as new");
		}

		[Test]
		public void PrepareImport_SpecifiesWhereTheTempConfigIs()
		{
			// SUT
			_controller.PrepareImport(_zipFile);
			Assert.That(_controller._temporaryImportConfigLocation,
				Is.EqualTo(Path.Combine(Path.GetTempPath(), configFilename)),
				"extracted temporary location or filename not set as expected");
		}

		[Test]
		public void DoImport_ImportsConfig()
		{
			_controller.PrepareImport(_zipFile);

			// SUT
			_controller.DoImport();
			Assert.That(File.Exists(Path.Combine(_projectConfigPath, "importexportConfiguration.fwdictconfig")),
				"Configuration not imported or to the right path.");
			Assert.That(_controller._configurations.Any(config => config.Label == configLabel),
				"Imported configuration was not registered.");
			Assert.That(_controller.NewConfigToImport.FilePath,
				Is.EqualTo(Path.Combine(_projectConfigPath, "importexportConfiguration.fwdictconfig")),
				"FilePath of imported config was not set as expected.");
			Assert.That(_controller.ImportHappened, Is.True, "Alert that import has happened");
		}

		[Test]
		public void DoImport_ImportsStyles()
		{
			_controller.PrepareImport(_zipFile);
			CollectionAssert.IsEmpty(Cache.LangProject.StylesOC);
			// SUT
			_controller.DoImport();
			var importedTestStyle = Cache.LangProject.StylesOC.FirstOrDefault(style => style.Name == "TestStyle");
			Assert.NotNull(importedTestStyle, "test style was not imported.");
			Assert.That(importedTestStyle.Usage.BestAnalysisAlternative.Text, Is.StringMatching("Test Style"));
			Assert.AreEqual(importedTestStyle.Context, ContextValues.InternalConfigureView);
			Assert.AreEqual(importedTestStyle.Type, StyleType.kstCharacter);
			Assert.AreEqual(importedTestStyle.UserLevel, 2);
		}

		[Test]
		public void PrepareImport_DoesNotChangeRealData()
		{
			// SUT
			_controller.PrepareImport(_zipFile);

			Assert.That(!File.Exists(_projectConfigPath + configFilename),
				"Configuration should not have been imported if not requested.");
			Assert.That(_controller._configurations.All(config => config.Label != configLabel),
				"Configuration should not have been registered.");
		}

		[Test]
		public void PrepareImport_SuggestsUniqueLabelIfSameLabelAlreadyExists()
		{
			// More test setup

			var alreadyExistingModelWithSameLabel = new DictionaryConfigurationModel
			{
				Label = configLabel,
				Publications = new List<string>(),
			};
			var anotherAlreadyExistingModel = new DictionaryConfigurationModel
			{
				Label = "importexportConfiguration-Imported1",
				Publications = new List<string>(),
			};
			_controller._configurations.Add(alreadyExistingModelWithSameLabel);
			_controller._configurations.Add(anotherAlreadyExistingModel);

			// SUT
			_controller.PrepareImport(_zipFile);

			Assert.That(_controller._originalConfigLabel, Is.EqualTo(configLabel),
				"This should have been the original label of the config to import.");
			Assert.That(_controller.NewConfigToImport.Label, Is.EqualTo("importexportConfiguration-Imported2"),
				"This should be a new and different label since the original label is already taken.");
		}

		[Test]
		public void DoImport_UseUniqueFileNameToAvoidCollision()
		{
			var alreadyExistingModelWithSameLabel = new DictionaryConfigurationModel
			{
				Label = configLabel,
				Publications = new List<string>(),
			};
			DictionaryConfigurationManagerController.GenerateFilePath(_projectConfigPath, _controller._configurations,
				alreadyExistingModelWithSameLabel);

			var anotherAlreadyExistingModel = new DictionaryConfigurationModel
			{
				Label = "importexportConfiguration-Imported1",
				Publications = new List<string>(),
			};
			DictionaryConfigurationManagerController.GenerateFilePath(_projectConfigPath, _controller._configurations,
				anotherAlreadyExistingModel);

			_controller._configurations.Add(alreadyExistingModelWithSameLabel);
			_controller._configurations.Add(anotherAlreadyExistingModel);

			// File that exists but isn't actually registered in the list of configurations
			var fileInTheWay = Path.Combine(_projectConfigPath, "importexportConfiguration-Imported2.fwdictconfig");
			// ReSharper disable once LocalizableElement
			File.WriteAllText(fileInTheWay, "arbitrary file content");

			_controller.PrepareImport(_zipFile);

			// SUT
			_controller.DoImport();
			// What the filename is will be a combination of label collision handling in PrepareImport() and existing file collision handling performed by DictionaryConfigurationManagerController.
			Assert.That(File.Exists(Path.Combine(_projectConfigPath, "importexportConfiguration-Imported2_1.fwdictconfig")),
				"Configuration not imported or to the right place. Perhaps the existing but not-registered importexportConfiguration-Imported2.fwdictconfig file was not accounted for.");
			Assert.That(_controller._configurations.Any(config => config.Label == "importexportConfiguration-Imported2"),
				"Imported configuration was not registered, or with the expected label.");
			Assert.That(_controller.NewConfigToImport.FilePath,
				Is.EqualTo(Path.Combine(_projectConfigPath, "importexportConfiguration-Imported2_1.fwdictconfig")),
				"FilePath of imported config was not set as expected.");
		}

		[Test]
		public void PrepareImport_BadInputHandled()
		{
			Assert.DoesNotThrow(() => { _controller.PrepareImport("nonexistentfile.zip"); });
			Assert.That(_controller.NewConfigToImport, Is.Null, "Did not handle bad data as desired");
			Assert.That(_controller._originalConfigLabel, Is.Null, "Did not handle bad data as desired");
			Assert.DoesNotThrow(() => { _controller.PrepareImport("bad$# \\characters/in!: filename;.~*("); });
			Assert.DoesNotThrow(() => { _controller.PrepareImport(""); }, "Don't actually crash for this");
			Assert.That(_controller.NewConfigToImport, Is.Null, "Did not handle bad data as desired");
			Assert.DoesNotThrow(() => { _controller.PrepareImport(null); }, "Don't actually crash for this");
			Assert.That(_controller.NewConfigToImport, Is.Null, "Did not handle bad data as desired");
		}

		[Test]
		public void UserRequestsOverwrite_ResultsInDifferentLabelAndFilename()
		{
			var importOverwriteConfigFilePath = UserRequestsOverwrite_Helper();
			var configThatShouldBeOverwritten = _controller._configurations.First(config => config.Label == configLabel);

			// SUT
			_controller.UserRequestsOverwrite();

			Assert.That(_controller.NewConfigToImport.Label, Is.EqualTo(configLabel),
				"Should be using original label in the configuartion to import, not a non-colliding one.");

			// SUT 2
			_controller.DoImport();

			Assert.That(_controller.NewConfigToImport.FilePath, Is.EqualTo(importOverwriteConfigFilePath),
				"This is nit-picking, but use a filename based on the original label, not based on a non-colliding label."
				+ " So ORIGINALLABEL+maybesomething, not NONCOLLIDING+something (so not importexportConfiguration-Imported2)");
			Assert.That(!_controller._configurations.Contains(configThatShouldBeOverwritten),
				"old config of same label shouldn't still be there if overwritten");
			var newConfigInRegisteredSet = _controller._configurations.First(config => config.Label == configLabel);
			Assert.That(newConfigInRegisteredSet, Is.Not.Null, "Did not find imported configuration with expected label");
			Assert.That(newConfigInRegisteredSet, Is.EqualTo(_controller.NewConfigToImport),
				"Imported config was not what was expected");
		}

		private string UserRequestsOverwrite_Helper()
		{
			// This model has the same label but a non-colliding filename. Proves overwrite the code will always overwrite.
			var alreadyExistingModelWithSameLabel = new DictionaryConfigurationModel
			{
				Label = configLabel,
				Publications = new List<string> { "Main Dictionary", "unknown pub 1", "unknown pub 2" },
				FilePath = Path.Combine(_projectConfigPath, "Different" + configFilename)
			};
			FileUtils.WriteStringtoFile(alreadyExistingModelWithSameLabel.FilePath, "arbitrary file content", Encoding.UTF8);
			var anotherAlreadyExistingModel = new DictionaryConfigurationModel
			{
				Label = "importexportConfiguration-Imported1",
				Publications = new List<string> { "Main Dictionary", "unknown pub 1", "unknown pub 2" },
				FilePath = Path.GetTempPath() + configFilename
			};
			DictionaryConfigurationManagerController.GenerateFilePath(_projectConfigPath, _controller._configurations,
				anotherAlreadyExistingModel);
			FileUtils.WriteStringtoFile(anotherAlreadyExistingModel.FilePath, "arbitrary file content", Encoding.UTF8);

			_controller._configurations.Add(alreadyExistingModelWithSameLabel);
			_controller._configurations.Add(anotherAlreadyExistingModel);

			_controller.PrepareImport(_zipFile);

			return alreadyExistingModelWithSameLabel.FilePath;
		}

		[Test]
		public void UserRequestsNotOverwrite_UsesRightLabelAndFilename()
		{
			UserRequestsOverwrite_Helper();
			_controller.UserRequestsOverwrite();

			// SUT
			_controller.UserRequestsNotOverwrite();

			Assert.That(_controller.NewConfigToImport.Label, Is.EqualTo("importexportConfiguration-Imported2"),
				"Should be using non-colliding label.");

			// SUT 2
			_controller.DoImport();

			Assert.That(_controller.NewConfigToImport.FilePath,
				Is.EqualTo(Path.Combine(_projectConfigPath, _controller.NewConfigToImport.Label + DictionaryConfigurationModel.FileExtension)),
				"Use a filename based on the non-colliding label.");
		}

		[Test]
		public void UserRequestsNotOverwrite_CanBeSpecifiedOverAndOverWithoutBreaking()
		{
			UserRequestsOverwrite_Helper();
			_controller.UserRequestsOverwrite();
			_controller.UserRequestsNotOverwrite();
			_controller.UserRequestsOverwrite();
			_controller.UserRequestsNotOverwrite();
			_controller.UserRequestsOverwrite();
			_controller.UserRequestsNotOverwrite();
			_controller.UserRequestsOverwrite();
			_controller.UserRequestsNotOverwrite();
			_controller.UserRequestsOverwrite();
			Assert.That(_controller.NewConfigToImport.Label, Is.EqualTo(configLabel),
				"Should be using original label in the configuartion to import, not a non-colliding one.");
			_controller.UserRequestsNotOverwrite();
			Assert.That(_controller.NewConfigToImport.Label, Is.EqualTo("importexportConfiguration-Imported2"),
				"Should be using non-colliding label.");
		}

		[Test]
		public void PrepareImport_RevertsImportHappenedFlag()
		{
			_controller.PrepareImport(_zipFile);
			_controller.DoImport();
			Assert.That(_controller.ImportHappened, Is.True, "Unit test not set up correctly.");
			// SUT 1
			_controller.PrepareImport(_zipFile);
			Assert.That(_controller.ImportHappened, Is.False, "The import dialog and controller isn't really meant to be used this way, but don't let it be so that ImportHappened can be true yet NewConfigToImport is only just freshly prepared and not imported yet.");

			_controller.DoImport();
			Assert.That(_controller.ImportHappened, Is.True, "Unit test not set up correctly.");
			// SUT 2
			_controller.PrepareImport("nonexistent.zip");
			Assert.That(_controller.ImportHappened, Is.False, "Also should be false in this case since NewConfigToImport==null");
		}

		/// <summary>
		/// When a dictionary configuration is imported, any publications it mentions that Flex doesn't know about yet should be added to the list of publications that Flex knows about.
		/// </summary>
		[Test]
		public void DoImport_AddsNewPublications()
		{
			var configFile=FileUtils.GetTempFile("unittest.txt");
			FileUtils.WriteStringtoFile(configFile, "arbitrary file contents", Encoding.UTF8);
			_controller._temporaryImportConfigLocation = configFile;
			_controller.NewConfigToImport = new DictionaryConfigurationModel()
			{
				Label = "blah",
				Publications = new List<string>() {"pub 1", "pub 2"},
				Parts = new List<ConfigurableDictionaryNode> { new ConfigurableDictionaryNode() }
			};
			// SUT
			_controller.DoImport();

			Assert.That(_controller.NewConfigToImport.Publications.Contains("pub 1"), "Should not have lost publication from configuration that was imported");
			Assert.That(_controller.NewConfigToImport.Publications.Contains("pub 2"), "Should not have lost publication from configuration that was imported");

			Assert.That(Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS.Select(publicationTypePossibility => publicationTypePossibility.Name.get_String(Cache.DefaultAnalWs).Text).Contains("pub 1"), "Publication not added to Flex");
			Assert.That(Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS.Select(publicationTypePossibility => publicationTypePossibility.Name.get_String(Cache.DefaultAnalWs).Text).Contains("pub 2"), "Publication not added to Flex");
		}

		[Test]
		public void PrepareImport_DoImport_CreatesCustomFieldsFromExportResult()
		{
			var customFieldSameLabel = "KeepMeImGood";
			var customFieldLabel = "TempCustomField";
			var customFieldWrongType = "WrongTypeField";
			var zipFile = Path.GetTempFileName();
			using (var existingSameCf = new CustomFieldForTest(Cache, customFieldSameLabel, customFieldSameLabel, LexSenseTags.kClassId, StTextTags.kClassId, -1,
					CellarPropertyType.OwningAtomic, Guid.Empty))
			{
				using (new CustomFieldForTest(Cache, customFieldLabel, customFieldLabel, LexEntryTags.kClassId, StTextTags.kClassId, -1,
						CellarPropertyType.OwningAtomic, Guid.Empty))
				using (new CustomFieldForTest(Cache, customFieldWrongType, customFieldWrongType, LexEntryTags.kClassId, CmPossibilityTags.kClassId, -1,
						CellarPropertyType.ReferenceAtom, Cache.LangProject.LexDbOA.ComplexEntryTypesOA.Guid))
				{
					var importExportDCModel = new DictionaryConfigurationModel
					{
						Label = "importexportConfiguration",
						Publications = new List<string>(),
						FilePath = Path.GetTempPath() + "importexportConfigurationFile.fwdictconfig",
					};

					var configurationToExport = importExportDCModel;
					_pathToConfiguration = configurationToExport.FilePath;
					File.WriteAllText(_pathToConfiguration,
						DictionaryConfigurationModelTests.XmlOpenTagsThruHeadword +
						DictionaryConfigurationModelTests.XmlCloseTagsFromHeadword);
					// This export should create the zipfile containing the custom field information (currently in LIFT format)
					DictionaryConfigurationManagerController.ExportConfiguration(configurationToExport, zipFile, Cache);
				} // Destroy two of the custom fields and verify the state
				VerifyCustomFieldPresent(customFieldSameLabel, LexSenseTags.kClassId, StTextTags.kClassId, CellarPropertyType.OwningAtomic);
				VerifyCustomFieldAbsent(customFieldLabel, LexEntryTags.kClassId);
				VerifyCustomFieldAbsent(customFieldWrongType, LexEntryTags.kClassId);
				// Re-introduce one of the fields that was in the export, but with a different type
				using (new CustomFieldForTest(Cache, customFieldWrongType, customFieldWrongType, LexEntryTags.kClassId, StTextTags.kClassId, -1,
						CellarPropertyType.OwningAtomic, Guid.Empty))
				{
					// SUT
					_controller.PrepareImport(zipFile);
					// Verify prepare import counted the custom fields
					CollectionAssert.IsNotEmpty(_controller._customFieldsToImport, "No custom fields found in the lift file by PrepareImport");
					CollectionAssert.AreEquivalent(_controller._customFieldsToImport, new[] { customFieldLabel, customFieldSameLabel, customFieldWrongType });

					// Make sure the 'wrongType' custom field has been re-introduced by the test with a different type
					VerifyCustomFieldPresent(customFieldWrongType, LexEntryTags.kClassId, StTextTags.kClassId);
					// SUT
					_controller.DoImport();
					// Assert that the field which was present before the import is still there
					VerifyCustomFieldPresent(customFieldSameLabel, LexSenseTags.kClassId, StTextTags.kClassId);
					// Assert that the field which was not present before the import has been added
					VerifyCustomFieldPresent(customFieldLabel, LexEntryTags.kClassId, StTextTags.kClassId);
					// Assert that the field which was present with the wrong type is still present.
					// In the future if we implement overwriting an existing custom field with the data from the export this
					// is where we would assert the change.
					VerifyCustomFieldPresent(customFieldWrongType, LexEntryTags.kClassId, StTextTags.kClassId);
				}
			}
		}

		private void VerifyCustomFieldPresent(string customFieldLabel, int classWithCustomField, int expectedType, CellarPropertyType ownerType = CellarPropertyType.Nil)
		{
			var mdc = Cache.MetaDataCacheAccessor as IFwMetaDataCacheManaged;
			var flid = mdc.GetFieldId2(classWithCustomField, customFieldLabel, false);
			Assert.IsTrue(mdc.IsCustom(flid));
			Assert.AreEqual(mdc.GetDstClsId(flid), expectedType, "The {0} custom field was not the correct type.", customFieldLabel);
		}

		private void VerifyCustomFieldAbsent(string customFieldLabel, int classWithCustomField)
		{
			var mdc = Cache.MetaDataCacheAccessor as IFwMetaDataCacheManaged;
			Assert.Throws<FDOInvalidFieldException>(() => mdc.GetFieldId2(classWithCustomField, customFieldLabel, false));
		}
	}
}
