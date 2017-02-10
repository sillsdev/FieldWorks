// Copyright (c) 2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.Utils;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Tests for DictionaryConfigurationImportController.
	/// LT-17397.
	/// These tests write to disk, not just in memory, so they can use the zip library.
	/// </summary>
	class DictionaryConfigurationImportControllerTests : MemoryOnlyBackendProviderTestBase
	{
		private DictionaryConfigurationImportController _controller;
		private string _projectConfigPath = null;
		private readonly string _defaultConfigPath = Path.Combine(FwDirectoryFinder.DefaultConfigurations, "Dictionary");

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

			// Export a configuration that we know how to import

			_zipFile = Path.GetTempFileName();
			DictionaryConfigurationManagerController.ExportConfiguration(configurationToExport, _zipFile);
			Assert.That(File.Exists(_zipFile), "Unit test not set up right");
			Assert.That(new FileInfo(_zipFile).Length, Is.GreaterThan(0), "Unit test not set up right");

			// Clear the configuration away so we can see it gets imported

			File.Delete(_pathToConfiguration);
			Assert.That(!File.Exists(_pathToConfiguration),
				"Unit test not set up right. Configuration should be out of the way for testing export.");
			Assert.That(!_controller._configurations.Any(config => config.Label == "Root"),
				"Unit test not set up right. A config exists with the same label as the config we will import.");
			Assert.That(!_controller._configurations.Any(config => config.Label == "importexportConfiguration"),
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
			Assert.That(_controller.NewConfigToImport.Label, Is.EqualTo("Root"), "Failed to process data to be imported");
			Assert.That(_controller._originalConfigLabel, Is.EqualTo("Root"),
				"Failed to describe original label from data to import.");
			Assert.That(_controller.ImportHappened, Is.False, "Import hasn't actually happened yet, so don't claim it has");
		}

		[Test]
		public void PrepareImport_SpecifiesWhereTheTempConfigIs()
		{
			// SUT
			_controller.PrepareImport(_zipFile);
			Assert.That(_controller._temporaryImportConfigLocation,
				Is.EqualTo(Path.Combine(Path.GetTempPath(), "importexportConfigurationFile.fwdictconfig")),
				"extracted temporary location or filename not set as expected");
		}

		[Test]
		public void DoImport_ImportsConfig()
		{
			_controller.PrepareImport(_zipFile);

			// SUT
			_controller.DoImport();
			Assert.That(File.Exists(Path.Combine(_projectConfigPath, "Root.fwdictconfig")),
				"Configuration not imported or to the right path.");
			Assert.That(_controller._configurations.Any(config => config.Label == "Root"),
				"Imported configuration was not registered. 'Root' is the label in the XML written to the config file in the unit test setup.");
			Assert.That(_controller.NewConfigToImport.FilePath,
				Is.EqualTo(Path.Combine(_projectConfigPath, "Root.fwdictconfig")),
				"FilePath of imported config was not set as expected.");
			Assert.That(_controller.ImportHappened, Is.True, "Alert that import has happened");
		}

		[Test]
		public void PrepareImport_DoesNotChangeRealData()
		{
			// SUT
			_controller.PrepareImport(_zipFile);

			Assert.That(!File.Exists(_projectConfigPath + "importexportConfigurationFile.fwdictconfig"),
				"Configuration should not have been imported if not requested.");
			Assert.That(!_controller._configurations.Any(config => config.Label == "Root"),
				"Configuration should not have been registered. 'Root' is the label in the XML written to the config file in the unit test setup.");
		}

		[Test]
		public void PrepareImport_SuggestsUniqueLabelIfSameLabelAlreadyExists()
		{
			// More test setup

			var alreadyExistingModelWithSameLabel = new DictionaryConfigurationModel
			{
				Label = "Root",
				Publications = new List<string>(),
			};
			var anotherAlreadyExistingModel = new DictionaryConfigurationModel
			{
				Label = "Root-Imported1",
				Publications = new List<string>(),
			};
			_controller._configurations.Add(alreadyExistingModelWithSameLabel);
			_controller._configurations.Add(anotherAlreadyExistingModel);

			// SUT
			_controller.PrepareImport(_zipFile);

			Assert.That(_controller._originalConfigLabel, Is.EqualTo("Root"),
				"This should have been the original label of the config to import.");
			Assert.That(_controller.NewConfigToImport.Label, Is.EqualTo("Root-Imported2"),
				"This should be a new and different label since the original label is already taken.");
		}

		[Test]
		public void DoImport_UseUniqueFileNameToAvoidCollision()
		{
			var alreadyExistingModelWithSameLabel = new DictionaryConfigurationModel
			{
				Label = "Root",
				Publications = new List<string>(),
			};
			DictionaryConfigurationManagerController.GenerateFilePath(_projectConfigPath, _controller._configurations,
				alreadyExistingModelWithSameLabel);

			var anotherAlreadyExistingModel = new DictionaryConfigurationModel
			{
				Label = "Root-Imported1",
				Publications = new List<string>(),
			};
			DictionaryConfigurationManagerController.GenerateFilePath(_projectConfigPath, _controller._configurations,
				anotherAlreadyExistingModel);

			_controller._configurations.Add(alreadyExistingModelWithSameLabel);
			_controller._configurations.Add(anotherAlreadyExistingModel);

			// File that exists but isn't actually registered in the list of configurations
			var fileInTheWay = Path.Combine(_projectConfigPath, "Root-Imported2.fwdictconfig");
			File.WriteAllText(fileInTheWay, "file content");

			_controller.PrepareImport(_zipFile);

			// SUT
			_controller.DoImport();
			// What the filename is will be a combination of label collision handling in PrepareImport() and existing file collision handling performed by DictionaryConfigurationManagerController.
			Assert.That(File.Exists(Path.Combine(_projectConfigPath, "Root-Imported2_1.fwdictconfig")),
				"Configuration not imported or to the right place. Perhaps the existing but not-registered Root-Imported2.fwdictconfig file was not accounted for.");
			Assert.That(_controller._configurations.Any(config => config.Label == "Root-Imported2"),
				"Imported configuration was not registered, or with the expected label.");
			Assert.That(_controller.NewConfigToImport.FilePath,
				Is.EqualTo(Path.Combine(_projectConfigPath, "Root-Imported2_1.fwdictconfig")),
				"FilePath of imported config was not set as expected.");
		}

		[Test]
		public void PrepareImport_BadInputHandled()
		{
			Assert.DoesNotThrow(() => { _controller.PrepareImport("nonexistentfile.zip"); });
			Assert.That(_controller.NewConfigToImport, Is.Null, "Did not handle bad data as desired");
			Assert.That(_controller._originalConfigLabel, Is.Null, "Did not handle bad data as desired");
			Assert.That(_controller._status, Contains.Substring("Cannot"), "Should have set a failure status message");
			_controller._status = string.Empty;
			Assert.DoesNotThrow(() => { _controller.PrepareImport("bad$# \\characters/in!: filename;.~*("); });
			Assert.That(_controller._status, Contains.Substring("Cannot"), "Should have set a failure status message");
		}

		[Test]
		public void UserRequestsOverwrite_ResultsInDifferentLabelAndFilename()
		{
			UserRequestsOverwrite_Helper();
			var configThatShouldBeOverwritten = _controller._configurations.First(config => config.Label == "Root");

			// SUT
			_controller.UserRequestsOverwrite();

			Assert.That(_controller.NewConfigToImport.Label, Is.EqualTo("Root"),
				"Should be using original label in the configuartion to import, not a non-colliding one.");

			// SUT 2
			_controller.DoImport();

			Assert.That(_controller.NewConfigToImport.FilePath,
				Is.EqualTo(Path.Combine(_projectConfigPath, _controller.NewConfigToImport.Label + ".fwdictconfig")),
				"This is nit-picking, but use a filename based on the original label, not based on a non-colliding label. So ORIGINALLABEL+maybesomething, not NONCOLLIDING+something (so not Root-Imported2)");

			Assert.That(!_controller._configurations.Contains(configThatShouldBeOverwritten),
				"old config of same label shouldn't still be there if overwritten");
			var newConfigInRegisteredSet = _controller._configurations.First(config => config.Label == "Root");
			Assert.That(newConfigInRegisteredSet, Is.Not.Null, "Did not find imported configuration with expected label");
			Assert.That(newConfigInRegisteredSet, Is.EqualTo(_controller.NewConfigToImport),
				"Imported config was not what was expected");
		}

		private void UserRequestsOverwrite_Helper()
		{
			var alreadyExistingModelWithSameLabel = new DictionaryConfigurationModel
			{
				Label = "Root",
				Publications = new List<string>(),
			};
			DictionaryConfigurationManagerController.GenerateFilePath(_projectConfigPath, _controller._configurations,
				alreadyExistingModelWithSameLabel);
			FileUtils.WriteStringtoFile(alreadyExistingModelWithSameLabel.FilePath, "file content", Encoding.UTF8);
			var anotherAlreadyExistingModel = new DictionaryConfigurationModel
			{
				Label = "Root-Imported1",
				Publications = new List<string>(),
			};
			DictionaryConfigurationManagerController.GenerateFilePath(_projectConfigPath, _controller._configurations,
				anotherAlreadyExistingModel);
			FileUtils.WriteStringtoFile(anotherAlreadyExistingModel.FilePath, "file content", Encoding.UTF8);

			_controller._configurations.Add(alreadyExistingModelWithSameLabel);
			_controller._configurations.Add(anotherAlreadyExistingModel);

			_controller.PrepareImport(_zipFile);
		}

		[Test]
		public void UserRequestsNotOverwrite_UsesRightLabelAndFilename()
		{
			UserRequestsOverwrite_Helper();
			_controller.UserRequestsOverwrite();

			// SUT
			_controller.UserRequestsNotOverwrite();

			Assert.That(_controller.NewConfigToImport.Label, Is.EqualTo("Root-Imported2"),
				"Should be using non-colliding label.");

			// SUT 2
			_controller.DoImport();

			Assert.That(_controller.NewConfigToImport.FilePath,
				Is.EqualTo(Path.Combine(_projectConfigPath, _controller.NewConfigToImport.Label + ".fwdictconfig")),
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
			Assert.That(_controller.NewConfigToImport.Label, Is.EqualTo("Root"),
				"Should be using original label in the configuartion to import, not a non-colliding one.");
			_controller.UserRequestsNotOverwrite();
			Assert.That(_controller.NewConfigToImport.Label, Is.EqualTo("Root-Imported2"),
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
	}
}
