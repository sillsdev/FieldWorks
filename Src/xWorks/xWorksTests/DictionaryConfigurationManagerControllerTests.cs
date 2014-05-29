// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using NUnit.Framework;
using SIL.FieldWorks.FDO.FDOTests;

namespace SIL.FieldWorks.XWorks
{
	[TestFixture]
	class DictionaryConfigurationManagerControllerTests : MemoryOnlyBackendProviderTestBase
	{
		private DictionaryConfigurationManagerController _controller;

		private readonly string _projectConfigPath = Path.GetTempPath();

		[SetUp]
		public void Setup()
		{
			var configurations = new List<DictionaryConfigurationModel>
			{
				new DictionaryConfigurationModel { Label = "configuration0", Publications = new List<string>() },
				new DictionaryConfigurationModel { Label = "configuration1", Publications = new List<string>() }
			};

			var publications = new List<string>
			{
				"publicationA",
				"publicationB"
			};

			_controller = new DictionaryConfigurationManagerController(_projectConfigPath)
			{
				Configurations = configurations,
				Publications = publications,
			};
		}

		[TearDown]
		public void TearDown()
		{

		}

		[Test]
		public void GetPublication_UsesAssociations()
		{
			_controller.Configurations[0].Publications.Add("publicationA");
			// SUT
			var pubs = _controller.GetPublications(_controller.Configurations[0]);
			Assert.That(pubs, Contains.Item("publicationA"));
			Assert.That(pubs, Has.Count.EqualTo(1));

			// SUT
			Assert.Throws<ArgumentNullException>(() => _controller.GetPublications(null));
		}

		[Test]
		public void AssociatePublication_BadArgsTests()
		{
			Assert.Throws<ArgumentNullException>(() => _controller.AssociatePublication(null, null), "No configuration to associate with");
			Assert.Throws<ArgumentNullException>(() => _controller.AssociatePublication("publicationA", null), "No configuration to associate with");
			Assert.Throws<ArgumentNullException>(() => _controller.AssociatePublication(null, _controller.Configurations[0]), "Don't allow trying to add null");

			Assert.Throws<ArgumentOutOfRangeException>(() => _controller.AssociatePublication("unknown publication", _controller.Configurations[0]), "Don't associate with an invalid/unknown publication");
		}

		[Test]
		public void AssociatesPublication()
		{
			// SUT
			_controller.AssociatePublication("publicationA", _controller.Configurations[0]);
			Assert.That(_controller.Configurations[0].Publications, Contains.Item("publicationA"), "failed to associate");
			Assert.That(_controller.Configurations[0].Publications, Is.Not.Contains("publicationB"), "should not have associated with publicationB");

			// SUT
			_controller.AssociatePublication("publicationA", _controller.Configurations[1]);
			_controller.AssociatePublication("publicationB", _controller.Configurations[1]);
			Assert.That(_controller.Configurations[1].Publications, Contains.Item("publicationA"), "failed to associate");
			Assert.That(_controller.Configurations[1].Publications, Contains.Item("publicationB"), "failed to associate");
		}

		[Test]
		public void DisassociatePublication_BadArgsTests()
		{
			Assert.Throws<ArgumentNullException>(() => _controller.DisassociatePublication(null, null), "No configuration to disassociate. No publication to disassociate from.");
			Assert.Throws<ArgumentNullException>(() => _controller.DisassociatePublication("publicationA", null), "No configuration");
			Assert.Throws<ArgumentNullException>(() => _controller.DisassociatePublication(null, _controller.Configurations[0]), "No publication");

			Assert.Throws<ArgumentOutOfRangeException>(() => _controller.DisassociatePublication("unknown publication", _controller.Configurations[0]), "Don't try to operate using an invalid/unknown publication");
		}

		[Test]
		public void DisassociatesPublication()
		{
			_controller.AssociatePublication("publicationA", _controller.Configurations[1]);
			_controller.AssociatePublication("publicationB", _controller.Configurations[1]);
			// SUT
			_controller.DisassociatePublication("publicationA", _controller.Configurations[1]);

			Assert.That(_controller.Configurations[1].Publications, Contains.Item("publicationB"), "Should not have disassociated unrelated publication");
			Assert.That(_controller.Configurations[1].Publications, Is.Not.Contains("publicationA"), "failed to disassociate");
		}

		[Test]
		public void Rename_RevertsOnCancel()
		{
			var selectedConfig = _controller.Configurations[0];
			var listViewItem = new ListViewItem { Tag = selectedConfig };
			var oldLabel = selectedConfig.Label;

			// SUT
			Assert.True(_controller.RenameConfiguration(listViewItem, new LabelEditEventArgs(0, null)), "'Cancel' should complete successfully");

			Assert.AreEqual(oldLabel, selectedConfig.Label, "Configuration should not have been renamed");
			Assert.AreEqual(oldLabel, listViewItem.Text, "ListViewItem Text should have been reset");
		}

		[Test]
		public void Rename_PreventsDuplicate()
		{
			var dupLabelArgs = new LabelEditEventArgs(0, "DuplicateLabel");
			var configA = _controller.Configurations[0];
			var configB = new DictionaryConfigurationModel { Label = "configuration2", Publications = new List<string>() };
			_controller.RenameConfiguration(new ListViewItem { Tag = configA }, dupLabelArgs);
			_controller.Configurations.Insert(0, configB);

			// SUT
			Assert.False(_controller.RenameConfiguration(new ListViewItem { Tag = configB }, dupLabelArgs), "Duplicate should return 'incomplete'");

			Assert.AreEqual(dupLabelArgs.Label, configA.Label, "The first config should have been given the specified name");
			Assert.AreNotEqual(dupLabelArgs.Label, configB.Label, "The second config should not have been given the same name");
		}

		[Test]
		public void Rename_RenamesConfigAndFile()
		{
			const string newLabel = "NewLabel";
			var selectedConfig = _controller.Configurations[0];
			selectedConfig.FilePath = null;
			// SUT
			Assert.True(_controller.RenameConfiguration(new ListViewItem { Tag = selectedConfig }, new LabelEditEventArgs(0, newLabel)),
				"Renaming a config to a unique name should complete successfully");
			Assert.AreEqual(newLabel, selectedConfig.Label, "The configuration should have been renamed");
			Assert.AreEqual(_controller.FormatFilePath(newLabel), selectedConfig.FilePath, "The FilePath should have been generated");
		}

		[Test]
		public void GenerateFilePath()
		{
			var configToRename = new DictionaryConfigurationModel
			{
				Label = "configuration3", FilePath = null, Publications = new List<string>()
			};
			var conflictingConfigs = new List<DictionaryConfigurationModel>
			{
				new DictionaryConfigurationModel
				{
					Label = "conflicting file 3-0", FilePath = _controller.FormatFilePath("configuration3"), Publications = new List<string>()
				},
				new DictionaryConfigurationModel
				{
					Label = "conflicting file 3-1", FilePath = _controller.FormatFilePath("configuration3_1"), Publications = new List<string>()
				},
				new DictionaryConfigurationModel
				{
					Label = "conflicting file 3-2--in another directory to prove we can't accidentally mask unchanged default configurations",
					FilePath = Path.Combine(Path.Combine(_projectConfigPath, "subdir"),
						"configuration3_2" + DictionaryConfigurationModel.FileExtension),
					Publications = new List<string>()
				}
			};
			_controller.Configurations.Add(configToRename);
			_controller.Configurations.AddRange(conflictingConfigs);

			// SUT
			_controller.GenerateFilePath(configToRename);

			var newFilePath = configToRename.FilePath;
			StringAssert.StartsWith(_projectConfigPath, newFilePath);
			StringAssert.EndsWith(DictionaryConfigurationModel.FileExtension, newFilePath);
			Assert.AreEqual(_controller.FormatFilePath("configuration3_3"), configToRename.FilePath, "The file path should be based on the label");
			foreach (var config in conflictingConfigs)
			{
				Assert.AreNotEqual(Path.GetFileName(newFilePath), Path.GetFileName(config.FilePath), "File name should be unique");
			}
		}

		[Test]
		public void FormatFilePath()
		{
			var formattedFilePath = _controller.FormatFilePath("\nFile\\Name/With\"Chars<?>"); // SUT
			StringAssert.StartsWith(_projectConfigPath, formattedFilePath);
			StringAssert.EndsWith(DictionaryConfigurationModel.FileExtension, formattedFilePath);
			StringAssert.DoesNotContain("\n", formattedFilePath);
			StringAssert.DoesNotContain("\\", Path.GetFileName(formattedFilePath));
			StringAssert.DoesNotContain("/", Path.GetFileName(formattedFilePath));
			StringAssert.DoesNotContain("\"", formattedFilePath);
			StringAssert.DoesNotContain("<", formattedFilePath);
			StringAssert.DoesNotContain("?", formattedFilePath);
			StringAssert.DoesNotContain(">", formattedFilePath);
			StringAssert.Contains("File", formattedFilePath);
			StringAssert.Contains("Name", formattedFilePath);
			StringAssert.Contains("With", formattedFilePath);
			StringAssert.Contains("Chars", formattedFilePath);
		}

		[Test]
		public void CopyConfiguration()
		{
			// insert a series of "copied" configs
			var pubs = new List<string> { "publicationA", "publicationB" };
			var extantConfigs = new List<DictionaryConfigurationModel>
			{
				new DictionaryConfigurationModel { Label = "configuration4", Publications = pubs },
				new DictionaryConfigurationModel { Label = "Copy of configuration4", Publications = new List<string>() },
				new DictionaryConfigurationModel { Label = "Copy of configuration4 (1)", Publications = new List<string>() },
				new DictionaryConfigurationModel { Label = "Copy of configuration4 (2)", Publications = new List<string>() }
			};
			_controller.Configurations.InsertRange(0, extantConfigs);

			// SUT
			var newConfig = _controller.CopyConfiguration(extantConfigs[0]);

			Assert.AreEqual("Copy of configuration4 (3)", newConfig.Label, "The new label should be based on the original");
			Assert.Contains(newConfig, _controller.Configurations, "The new config should have been added to the list");
			Assert.AreEqual(1, _controller.Configurations.Count(conf => newConfig.Label.Equals(conf.Label)), "The label should be unique");

			Assert.AreEqual(pubs.Count, newConfig.Publications.Count, "Publications were not copied");
			for (int i = 0; i < pubs.Count; i++)
			{
				Assert.AreEqual(pubs[i], newConfig.Publications[i], "Publications were not copied");
			}
			Assert.IsNull(newConfig.FilePath, "Path should be null to signify that it should be generated on rename");
		}
	}
}
