// Copyright (c) 2014-2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Ionic.Zip;
using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.FwKernelInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.TestUtilities;
using SIL.Utils;
using FileUtils = SIL.Utils.FileUtils;
// ReSharper disable InconsistentNaming

namespace SIL.FieldWorks.XWorks
{
	[TestFixture]
	public class DictionaryConfigurationManagerControllerTests : MemoryOnlyBackendProviderTestBase
	{
		private DictionaryConfigurationManagerController _controller;
		private List<DictionaryConfigurationModel> _configurations;

		private readonly string _projectConfigPath = Path.GetTempPath();
		private readonly string _defaultConfigPath = Path.Combine(FwDirectoryFinder.DefaultConfigurations, "Dictionary");
		private IFileOS _mockFilesystem = new MockFileOS();
		private IStStyle _characterTestStyle;
		private IStStyle _paraTestStyle;
		private IStStyle _paraChildTestStyle;
		private IStStyle _bulletedTestStyle;
		private IStStyle _numberedTestStyle;
		private IStStyle _homographTestStyle;

		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			FileUtils.Manager.SetFileAdapter(_mockFilesystem);

			FileUtils.EnsureDirectoryExists(_defaultConfigPath);
			NonUndoableUnitOfWorkHelper.DoSomehow(Cache.ActionHandlerAccessor, () =>
			{
				var styleFactory = Cache.ServiceLocator.GetInstance<IStStyleFactory>();
				_characterTestStyle = styleFactory.Create(Cache.LangProject.StylesOC, "TestStyle", ContextValues.InternalConfigureView, StructureValues.Body, FunctionValues.Line, true, 2, false);
				_characterTestStyle.Usage.set_String(Cache.DefaultAnalWs, "Test Style");
				var propsBldr = TsStringUtils.MakePropsBldr();
				propsBldr.SetIntPropValues((int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault,
					(int)ColorUtil.ConvertColorToBGR(Color.Red));
				propsBldr.SetIntPropValues((int)FwTextPropType.ktptUnderline, (int)FwTextPropVar.ktpvDefault,
					(int)FwUnderlineType.kuntDouble);
				propsBldr.SetIntPropValues((int)FwTextPropType.ktptUnderColor, (int)FwTextPropVar.ktpvDefault,
					(int)ColorUtil.ConvertColorToBGR(Color.Blue));
				propsBldr.SetIntPropValues((int)FwTextPropType.ktptBold, (int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvForceOn);
				propsBldr.SetIntPropValues((int)FwTextPropType.ktptItalic, (int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvForceOn);
				propsBldr.SetStrPropValue((int)FwTextPropType.ktptFontFamily, "times");
				_characterTestStyle.Rules = propsBldr.GetTextProps();
				_paraTestStyle = styleFactory.Create(Cache.LangProject.StylesOC, "ParaTestStyle", ContextValues.InternalConfigureView, StructureValues.Body, FunctionValues.Line, false, 2, false);
				propsBldr.Clear();
				propsBldr.SetIntPropValues((int)FwTextPropType.ktptBackColor, (int)FwTextPropVar.ktpvDefault,
					(int)ColorUtil.ConvertColorToBGR(Color.Lime));
				propsBldr.SetIntPropValues((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum, (int)FwTextAlign.ktalCenter);
				propsBldr.SetIntPropValues((int)FwTextPropType.ktptLineHeight, (int)FwTextPropVar.ktpvMilliPoint, -3000);
				propsBldr.SetIntPropValues((int)FwTextPropType.ktptTrailingIndent, (int)FwTextPropVar.ktpvDefault, 4000);
				propsBldr.SetIntPropValues((int)FwTextPropType.ktptFirstIndent, (int)FwTextPropVar.ktpvDefault, -5000);
				propsBldr.SetIntPropValues((int)FwTextPropType.ktptLeadingIndent, (int)FwTextPropVar.ktpvDefault, 6000);
				propsBldr.SetIntPropValues((int)FwTextPropType.ktptSpaceBefore, (int)FwTextPropVar.ktpvDefault, 7000);
				propsBldr.SetIntPropValues((int)FwTextPropType.ktptSpaceAfter, (int)FwTextPropVar.ktpvDefault, 8000);
				_paraTestStyle.Rules = propsBldr.GetTextProps();
				_paraChildTestStyle = styleFactory.Create(Cache.LangProject.StylesOC, "ParaChildTesttStyle",
					ContextValues.InternalConfigureView, StructureValues.Body, FunctionValues.Line, false, 3, false);
				propsBldr.Clear();
				propsBldr.SetIntPropValues((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum, (int)FwTextAlign.ktalJustify);
				_paraChildTestStyle.Rules = propsBldr.GetTextProps();
				_paraChildTestStyle.BasedOnRA = _paraTestStyle;
				_bulletedTestStyle = styleFactory.Create(Cache.LangProject.StylesOC, "Bulleted List", ContextValues.InternalConfigureView, StructureValues.Body, FunctionValues.Line, false, 2, false);
				_numberedTestStyle = styleFactory.Create(Cache.LangProject.StylesOC, "Numbered List", ContextValues.InternalConfigureView, StructureValues.Body, FunctionValues.Line, false, 2, false);
				_homographTestStyle = styleFactory.Create(Cache.LangProject.StylesOC, "Homograph-Number", ContextValues.InternalConfigureView, StructureValues.Body, FunctionValues.Line, true, 2, false);

				var characterStyleBasedOnSomething = styleFactory.Create(Cache.LangProject.StylesOC, "CharacterStyleBasedOnSomething", ContextValues.InternalConfigureView, StructureValues.Body, FunctionValues.Line, true, 2, false);
				characterStyleBasedOnSomething.BasedOnRA = _characterTestStyle;
			});
		}

		[TestFixtureTearDown]
		public override void FixtureTeardown()
		{
			FileUtils.Manager.Reset();
			base.FixtureTeardown();
		}

		[SetUp]
		public void Setup()
		{
			_configurations = new List<DictionaryConfigurationModel>
			{
				new DictionaryConfigurationModel { Label = "configuration0", Publications = new List<string>() },
				new DictionaryConfigurationModel { Label = "configuration1", Publications = new List<string>() }
			};

			var publications = new List<string>
			{
				"publicationA",
				"publicationB"
			};

			_controller = new DictionaryConfigurationManagerController(Cache, _configurations, publications, _projectConfigPath, _defaultConfigPath);
		}

		[TearDown]
		public void TearDown()
		{
		}

		[Test]
		public void GetPublication_UsesAssociations()
		{
			_configurations[0].Publications.Add("publicationA");
			// SUT
			var pubs = _controller.GetPublications(_configurations[0]);
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
			Assert.Throws<ArgumentNullException>(() => _controller.AssociatePublication(null, _configurations[0]), "Don't allow trying to add null");

			Assert.Throws<ArgumentOutOfRangeException>(() => _controller.AssociatePublication("unknown publication", _configurations[0]), "Don't associate with an invalid/unknown publication");
		}

		[Test]
		public void AssociatesPublication()
		{
			// SUT
			_controller.AssociatePublication("publicationA", _configurations[0]);
			Assert.That(_configurations[0].Publications, Contains.Item("publicationA"), "failed to associate");
			Assert.That(_configurations[0].Publications, Is.Not.Contains("publicationB"), "should not have associated with publicationB");

			// SUT
			_controller.AssociatePublication("publicationA", _configurations[1]);
			_controller.AssociatePublication("publicationB", _configurations[1]);
			Assert.That(_configurations[1].Publications, Contains.Item("publicationA"), "failed to associate");
			Assert.That(_configurations[1].Publications, Contains.Item("publicationB"), "failed to associate");
			Assert.That(_configurations[0].Publications, Is.Not.Contains("publicationB"),
				"should not have associated configuration0 with publicationB");
		}

		[Test]
		public void AssociatesPublicationOnlyOnce()
		{
			for (int i = 0; i < 3; i++)
			{
				// SUT
				_controller.AssociatePublication("publicationA", _configurations[0]);
			}
			Assert.AreEqual(1, _configurations[0].Publications.Count(pub => pub.Equals("publicationA")), "associated too many times");
		}

		[Test]
		public void DisassociatePublication_BadArgsTests()
		{
			Assert.Throws<ArgumentNullException>(() => _controller.DisassociatePublication(null, null), "No configuration to disassociate. No publication to disassociate from.");
			Assert.Throws<ArgumentNullException>(() => _controller.DisassociatePublication("publicationA", null), "No configuration");
			Assert.Throws<ArgumentNullException>(() => _controller.DisassociatePublication(null, _configurations[0]), "No publication");

			Assert.Throws<ArgumentOutOfRangeException>(() => _controller.DisassociatePublication("unknown publication", _configurations[0]), "Don't try to operate using an invalid/unknown publication");
		}

		[Test]
		public void DisassociatesPublication()
		{
			_controller.AssociatePublication("publicationA", _configurations[1]);
			_controller.AssociatePublication("publicationB", _configurations[1]);
			// SUT
			_controller.DisassociatePublication("publicationA", _configurations[1]);

			Assert.That(_configurations[1].Publications, Contains.Item("publicationB"), "Should not have disassociated unrelated publication");
			Assert.That(_configurations[1].Publications, Is.Not.Contains("publicationA"), "failed to disassociate");
		}

		[Test]
		public void Rename_RevertsOnCancel()
		{
			var selectedConfig = _configurations[0];
			var listViewItem = new ListViewItem { Tag = selectedConfig };
			var oldLabel = selectedConfig.Label;

			// SUT
			Assert.True(_controller.RenameConfiguration(listViewItem, new LabelEditEventArgs(0, null)), "'Cancel' should complete successfully");

			Assert.AreEqual(oldLabel, selectedConfig.Label, "Configuration should not have been renamed");
			Assert.AreEqual(oldLabel, listViewItem.Text, "ListViewItem Text should have been reset");
			Assert.False(_controller.IsDirty, "No changes; should not be dirty");
		}

		[Test]
		public void Rename_PreventsDuplicate()
		{
			var dupLabelArgs = new LabelEditEventArgs(0, "DuplicateLabel");
			var configA = _configurations[0];
			var configB = new DictionaryConfigurationModel { Label = "configuration2", Publications = new List<string>() };
			_controller.RenameConfiguration(new ListViewItem { Tag = configA }, dupLabelArgs);
			_configurations.Insert(0, configB);

			// SUT
			Assert.False(_controller.RenameConfiguration(new ListViewItem { Tag = configB }, dupLabelArgs), "Duplicate should return 'incomplete'");

			Assert.AreEqual(dupLabelArgs.Label, configA.Label, "The first config should have been given the specified name");
			Assert.AreNotEqual(dupLabelArgs.Label, configB.Label, "The second config should not have been given the same name");
		}

		[Test]
		public void Rename_RenamesConfigAndFile()
		{
			const string newLabel = "NewLabel";
			var selectedConfig = _configurations[0];
			selectedConfig.FilePath = null;
			// SUT
			Assert.True(_controller.RenameConfiguration(new ListViewItem { Tag = selectedConfig }, new LabelEditEventArgs(0, newLabel)),
				"Renaming a config to a unique name should complete successfully");
			Assert.AreEqual(newLabel, selectedConfig.Label, "The configuration should have been renamed");
			Assert.AreEqual(DictionaryConfigurationManagerController.FormatFilePath(_controller._projectConfigDir, newLabel), selectedConfig.FilePath, "The FilePath should have been generated");
			Assert.True(_controller.IsDirty, "Made changes; should be dirty");
		}



		private DictionaryConfigurationModel GenerateFilePath_Helper(out List<DictionaryConfigurationModel> conflictingConfigs)
		{
			var configToRename = new DictionaryConfigurationModel
			{
				Label = "configuration3",
				FilePath = null,
				Publications = new List<string>()
			};
			conflictingConfigs = new List<DictionaryConfigurationModel>
			{
				new DictionaryConfigurationModel
				{
					Label = "conflicting file 3-0",
					FilePath = DictionaryConfigurationManagerController.FormatFilePath(_controller._projectConfigDir, "configuration3"),
					Publications = new List<string>()
				},
				new DictionaryConfigurationModel
				{
					Label = "conflicting file 3-1",
					FilePath = DictionaryConfigurationManagerController.FormatFilePath(_controller._projectConfigDir, "configuration3_1"),
					Publications = new List<string>()
				},
				new DictionaryConfigurationModel
				{
					Label =
						"conflicting file 3-2--in another directory to prove we can't accidentally mask unchanged default configurations",
					FilePath = Path.Combine(Path.Combine(_projectConfigPath, "subdir"),
						"configuration3_2" + DictionaryConfigurationModel.FileExtension),
					Publications = new List<string>()
				}
			};
			_configurations.Add(configToRename);
			_configurations.AddRange(conflictingConfigs);
			return configToRename;
		}

		[Test]
		public void GenerateFilePath()
		{
			List<DictionaryConfigurationModel> conflictingConfigs;
			var configToRename = GenerateFilePath_Helper(out conflictingConfigs);

			// SUT
			DictionaryConfigurationManagerController.GenerateFilePath(_controller._projectConfigDir, _controller._configurations, configToRename);

			var newFilePath = configToRename.FilePath;
			StringAssert.StartsWith(_projectConfigPath, newFilePath);
			StringAssert.EndsWith(DictionaryConfigurationModel.FileExtension, newFilePath);
			Assert.AreEqual(DictionaryConfigurationManagerController.FormatFilePath(_controller._projectConfigDir, "configuration3_3"), configToRename.FilePath, "The file path should be based on the label");
			foreach (var config in conflictingConfigs)
			{
				Assert.AreNotEqual(Path.GetFileName(newFilePath), Path.GetFileName(config.FilePath), "File name should be unique");
			}
		}

		/// <summary>
		/// Also account for files on disk, rather than just considering what is registered in the set of configurations we know about.
		/// </summary>
		[Test]
		public void GenerateFilePath_AccountsForFilesOnDisk()
		{
			List<DictionaryConfigurationModel> conflictingConfigs;
			var configToRename = GenerateFilePath_Helper(out conflictingConfigs);

			FileUtils.WriteStringtoFile(Path.Combine(_projectConfigPath, "configuration3_3.fwdictconfig"), "file contents of config file that is in the way on disk but not actually registered in the list of configurations", Encoding.UTF8);

			// SUT
			DictionaryConfigurationManagerController.GenerateFilePath(_controller._projectConfigDir, _controller._configurations, configToRename);

			var newFilePath = configToRename.FilePath;
			Assert.That(newFilePath, Is.EqualTo(Path.Combine(_projectConfigPath, "configuration3_4.fwdictconfig")), "Did not account for collision with unregistered configuration on disk");
		}

		[Test]
		public void FormatFilePath()
		{
			var formattedFilePath = DictionaryConfigurationManagerController.FormatFilePath(_controller._projectConfigDir, "\nFile\\Name/With\"Chars<?>"); // SUT
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
			_configurations.InsertRange(0, extantConfigs);

			// SUT
			var newConfig = _controller.CopyConfiguration(extantConfigs[0]);

			Assert.AreEqual("Copy of configuration4 (3)", newConfig.Label, "The new label should be based on the original");
			Assert.Contains(newConfig, _configurations, "The new config should have been added to the list");
			Assert.AreEqual(1, _configurations.Count(conf => newConfig.Label.Equals(conf.Label)), "The label should be unique");

			Assert.AreEqual(pubs.Count, newConfig.Publications.Count, "Publications were not copied");
			for (int i = 0; i < pubs.Count; i++)
			{
				Assert.AreEqual(pubs[i], newConfig.Publications[i], "Publications were not copied");
			}
			Assert.IsNull(newConfig.FilePath, "Path should be null to signify that it should be generated on rename");
			Assert.True(_controller.IsDirty, "Made changes; should be dirty");
		}

		[Test]
		public void DeleteConfigurationRemovesFromList()
		{
			var configurationToDelete = _configurations[0];
			// SUT
			_controller.DeleteConfiguration(configurationToDelete);

			Assert.That(_configurations, Is.Not.Contains(configurationToDelete), "Should have removed configuration from list of configurations");
			Assert.That(_controller.IsDirty, "made changes; should be dirty");
		}

		[Test]
		public void DeleteConfigurationRemovesFromDisk()
		{
			var configurationToDelete = _configurations[0];

			DictionaryConfigurationManagerController.GenerateFilePath(_controller._projectConfigDir, _controller._configurations, configurationToDelete);
			var pathToConfiguration = configurationToDelete.FilePath;
			FileUtils.WriteStringtoFile(pathToConfiguration, "file contents", Encoding.UTF8);
			Assert.That(FileUtils.FileExists(pathToConfiguration), "Unit test not set up right");

			// SUT
			_controller.DeleteConfiguration(configurationToDelete);

			Assert.That(!FileUtils.FileExists(pathToConfiguration), "File should have been deleted");
			Assert.That(_controller.IsDirty, "made changes; should be dirty");
		}

		[Test]
		public void DeleteConfigurationDoesNotCrashIfNullFilePath()
		{
			var configurationToDelete = _configurations[0];
			Assert.That(configurationToDelete.FilePath, Is.Null, "Unit test not testing what it used to. Perhaps the code is smarter now.");

			// SUT
			Assert.DoesNotThrow(()=> _controller.DeleteConfiguration(configurationToDelete), "Don't crash if the FilePath isn't set for some reason.");
			Assert.That(_controller.IsDirty, "made changes; should be dirty");
		}

		[Test]
		public void DeleteConfigurationCrashesOnNullArgument()
		{
			Assert.Throws<ArgumentNullException>(() => _controller.DeleteConfiguration(null), "Failed to throw");
		}

		#region Insufficiently Mocked Tests - When DeleteConfiguration resets a config it loads the default config from the real filesystem
		[Test]
		public void DeleteConfigurationResetsForShippedDefaultRatherThanDelete()
		{
			var shippedRootDefaultConfigurationPath = Path.Combine(_defaultConfigPath, "Root" + DictionaryConfigurationModel.FileExtension);
			FileUtils.WriteStringtoFile(shippedRootDefaultConfigurationPath, "bogus data that is unread, the file is read from the real defaults", Encoding.UTF8);

			var configurationToDelete = _configurations[0];
			configurationToDelete.FilePath = Path.Combine("whateverdir", "Root" + DictionaryConfigurationModel.FileExtension);
			configurationToDelete.Label = "customizedLabel";

			var pathToConfiguration = configurationToDelete.FilePath;
			FileUtils.WriteStringtoFile(pathToConfiguration, "customized file contents", Encoding.UTF8);
			Assert.That(FileUtils.FileExists(pathToConfiguration), "Unit test not set up right");

			// SUT
			_controller.DeleteConfiguration(configurationToDelete);

			Assert.That(FileUtils.FileExists(pathToConfiguration), "The Root configuration file should have been reset to defaults, not deleted.");
			Assert.That(configurationToDelete.Label, Is.EqualTo("Root-based (complex forms as subentries)"), "The reset should match the shipped defaults.");
			Assert.Contains(configurationToDelete, _configurations, "The configuration should still be present in the list after being reset.");
			Assert.That(_controller.IsDirty, "Resetting is a change that is saved later; should be dirty");

			// Not asserting that the configurationToDelete.FilePath file contents are reset because that will happen later when it is saved.
		}

		[Test]
		public void DeleteConfigurationResetsReversalToShippedDefaultIfNoProjectAllReversal()
		{
			var defaultReversalPath = Path.Combine(FwDirectoryFinder.DefaultConfigurations, "ReversalIndex");
			// construct a controller to work in the default reversal directory
			_controller = new DictionaryConfigurationManagerController(Cache, _configurations, new List<string>(), _projectConfigPath, defaultReversalPath);
			var allRevFileName = DictionaryConfigurationModel.AllReversalIndexesFilenameBase + DictionaryConfigurationModel.FileExtension;
			var shippedRootDefaultConfigurationPath = Path.Combine(defaultReversalPath, allRevFileName);
			FileUtils.WriteStringtoFile(shippedRootDefaultConfigurationPath, "bogus data that is unread, the file is read from the real defaults", Encoding.UTF8);

			var configurationToDelete = _configurations[0];
			configurationToDelete.FilePath = Path.Combine("whateverdir", "English" + DictionaryConfigurationModel.FileExtension);
			configurationToDelete.Label = "English";
			configurationToDelete.WritingSystem = "en";

			var pathToConfiguration = configurationToDelete.FilePath;
			FileUtils.WriteStringtoFile(pathToConfiguration, "customized file contents", Encoding.UTF8);
			Assert.That(FileUtils.FileExists(pathToConfiguration), "Unit test not set up right");
			Assert.IsFalse(FileUtils.FileExists(Path.Combine(_projectConfigPath, allRevFileName)), "Unit test not set up right");

			// SUT
			_controller.DeleteConfiguration(configurationToDelete);

			Assert.That(FileUtils.FileExists(pathToConfiguration), "The English reversal file should have been reset to defaults, not deleted.");
			Assert.That(configurationToDelete.Label, Is.EqualTo("English"), "The label should still be English after a reset.");
			Assert.That(configurationToDelete.IsReversal, Is.True, "The reset configuration files should still be a reversal file.");
			Assert.Contains(configurationToDelete, _configurations, "The configuration should still be present in the list after being reset.");
			Assert.That(_controller.IsDirty, "Resetting is a change that is saved later; should be dirty");

			// Not asserting that the configurationToDelete.FilePath file contents are reset because that will happen later when it is saved.
		}
		#endregion

		[Test]
		public void KnowsWhenNotAShippedDefault()
		{
			var configuration = new DictionaryConfigurationModel
			{
				Label = "configuration",
				FilePath = Path.Combine("whateverdir", "somefile" + DictionaryConfigurationModel.FileExtension)
			};

			// SUT
			var claimsToBeDerived = _controller.IsConfigurationACustomizedOriginal(configuration);

			Assert.That(claimsToBeDerived, Is.False, "Should not have reported this as a shipped default configuration.");
		}

		[Test]
		public void NotAShippedDefaultIfNullFilePath()
		{
			var configuration = new DictionaryConfigurationModel
			{
				Label = "configuration",
				FilePath = null
			};

			// SUT
			var claimsToBeDerived = _controller.IsConfigurationACustomizedOriginal(configuration);

			Assert.That(claimsToBeDerived, Is.False, "Should not have reported this as a shipped default configuration.");
		}

		[Test]
		public void KnowsWhenIsAShippedDefault()
		{
			var configuration = new DictionaryConfigurationModel
			{
				Label = "configuration",
				FilePath = Path.Combine("whateverdir", "Root" + DictionaryConfigurationModel.FileExtension)
			};

			// SUT
			var claimsToBeDerived = _controller.IsConfigurationACustomizedOriginal(configuration);

			Assert.That(claimsToBeDerived, Is.True, "Should have reported this as a shipped default configuration.");
		}

		[Test]
		public void ReversalCopyIsNotACustomizedOriginal()
		{
			var configuration = new DictionaryConfigurationModel
			{
				Label = "English Copy",
				WritingSystem = "en",
				FilePath = Path.Combine("whateverdir", "Copy of English" + DictionaryConfigurationModel.FileExtension)
			};

			// SUT
			var claimsToBeDerived = _controller.IsConfigurationACustomizedOriginal(configuration);

			Assert.That(claimsToBeDerived, Is.False, "Copy of a reversal should not claim to be a customized original");
		}

		[Test]
		public void ReversalMatchingLanguageIsACustomizedOriginal()
		{
			var configuration = new DictionaryConfigurationModel
			{
				Label = "English",
				WritingSystem = "en",
				FilePath = Path.Combine("whateverdir", "English" + DictionaryConfigurationModel.FileExtension)
			};

			// SUT
			var claimsToBeDerived = _controller.IsConfigurationACustomizedOriginal(configuration);

			Assert.That(claimsToBeDerived, Is.True, "Should have reported this as a shipped default configuration.");
		}

		[Test]
		public void RenamedReversalMatchingLanguageIsACustomizedOriginal()
		{
			var configuration = new DictionaryConfigurationModel
			{
				Label = "Manglish",
				WritingSystem = "en",
				FilePath = Path.Combine("whateverdir", "English" + DictionaryConfigurationModel.FileExtension)
			};

			// SUT
			var claimsToBeDerived = _controller.IsConfigurationACustomizedOriginal(configuration);

			Assert.That(claimsToBeDerived, Is.True, "Should have reported this as a shipped default configuration.");
		}

		[Test]
		public void ReversalNotMatchingLanguageIsACustomizedOriginal()
		{
			var configuration = new DictionaryConfigurationModel
			{
				Label = "English (copy)",
				WritingSystem = "en",
				FilePath = Path.Combine("whateverdir", "English-Copy" + DictionaryConfigurationModel.FileExtension)
			};

			// SUT
			var claimsToBeDerived = _controller.IsConfigurationACustomizedOriginal(configuration);

			Assert.That(claimsToBeDerived, Is.False, "This is a copy and not a customized original and should have reported false.");
		}

		[Test]
		public void ReversalOfLanguageWithRegionIsACustomizedOriginal()
		{
			var configuration = new DictionaryConfigurationModel
			{
				Label = "German (Algeria)",
				WritingSystem = "de-DZ",
				FilePath = Path.Combine("whateverdir", "German (Algeria)" + DictionaryConfigurationModel.FileExtension)
			};

			// SUT
			var claimsToBeDerived = _controller.IsConfigurationACustomizedOriginal(configuration);

			Assert.That(claimsToBeDerived, Is.True, "Should have reported this as a shipped default configuration.");
		}

		[Test]
		public void ReversalOfInvalidLanguageIsNotACustomizedOriginal()
		{
			var configuration = new DictionaryConfigurationModel
			{
				Label = "English",
				WritingSystem = "enz1a",
				FilePath = Path.Combine("whateverdir", "enz1a" + DictionaryConfigurationModel.FileExtension)
			};

			// SUT
			var claimsToBeDerived = _controller.IsConfigurationACustomizedOriginal(configuration);

			Assert.That(claimsToBeDerived, Is.False, "Should have reported this as a shipped default configuration.");
		}

		[Test]
		public void ExportConfiguration_ThrowsOnBadInput()
		{
			Assert.Throws<ArgumentNullException>(() => DictionaryConfigurationManagerController.ExportConfiguration(null, "a", Cache));
			Assert.Throws<ArgumentNullException>(() => DictionaryConfigurationManagerController.ExportConfiguration(_configurations[0], null, Cache));
			Assert.Throws<ArgumentNullException>(() => DictionaryConfigurationManagerController.ExportConfiguration(_configurations[0], "a", null));
			Assert.Throws<ArgumentNullException>(() => DictionaryConfigurationManagerController.ExportConfiguration(null, null, null));
			// Empty string
			Assert.Throws<ArgumentException>(() => DictionaryConfigurationManagerController.ExportConfiguration(_configurations[0], "", Cache));

		}

		/// <summary>
		/// LT-17397.
		/// </summary>
		[Test]
		public void ExportConfiguration_ExportsZip()
		{
			// Writing to disk, not just in memory, so can use zip library.

			FileUtils.Manager.Reset();
			string expectedZipOutput = null;
			try
			{
				var configurationToExport = _configurations[0];
				DictionaryConfigurationManagerController.GenerateFilePath(_controller._projectConfigDir, _controller._configurations, configurationToExport);
				var pathToConfiguration = configurationToExport.FilePath;
				expectedZipOutput = Path.GetTempFileName();
				File.WriteAllText(pathToConfiguration, "file contents");
				Assert.That(File.Exists(pathToConfiguration), "Unit test not set up right");
				Assert.That(new FileInfo(expectedZipOutput).Length, Is.EqualTo(0),
					"Unit test not set up right. File will exist for convenience of writing the test but should not have any content yet.");

				// SUT
				DictionaryConfigurationManagerController.ExportConfiguration(configurationToExport, expectedZipOutput, Cache);

				Assert.That(File.Exists(expectedZipOutput), "File not exported");
				Assert.That(new FileInfo(expectedZipOutput).Length, Is.GreaterThan(0),
					"Exported file should have content");

				using (var zip = new ZipFile(expectedZipOutput))
				{
					Assert.That(zip.Count, Is.GreaterThanOrEqualTo(3), "Zip file must be missing parts of the export");
				}
			}
			finally
			{
				if (expectedZipOutput != null)
					File.Delete(expectedZipOutput);
				FileUtils.Manager.SetFileAdapter(_mockFilesystem);
			}
		}

		[Test]
		public void PrepareCustomFieldsExport_Works()
		{
			var customFieldLabel = "TestField";
			using (new CustomFieldForTest(Cache, customFieldLabel, customFieldLabel, LexEntryTags.kClassId, StTextTags.kClassId, -1,
				CellarPropertyType.OwningAtomic, Guid.Empty))
			{
				// SUT
				var customFieldFiles = DictionaryConfigurationManagerController.PrepareCustomFieldsExport(Cache).ToList();
				Assert.That(customFieldFiles.Count, Is.EqualTo(2), "Not enough files prepared");
				Assert.That(customFieldFiles[0], Is.StringEnding("CustomFields.lift"));
				Assert.That(customFieldFiles[1], Is.StringEnding("CustomFields.lift-ranges"));
				AssertThatXmlIn.File(customFieldFiles[0]).HasAtLeastOneMatchForXpath("//field[@tag='" + customFieldLabel + "']");
			}
		}

		[Test]
		public void PrepareStylesheetExport_Works()
		{
			// SUT
			var styleSheetFile = DictionaryConfigurationManagerController.PrepareStylesheetExport(Cache);
			Assert.False(string.IsNullOrEmpty(styleSheetFile), "No stylesheet data prepared");
			AssertThatXmlIn.File(styleSheetFile).HasSpecifiedNumberOfMatchesForXpath("/Styles/markup", 1);
			AssertThatXmlIn.File(styleSheetFile).HasSpecifiedNumberOfMatchesForXpath("/Styles/markup/tag[@id='" + _characterTestStyle.Name + "']", 1);
			var enWsId = Cache.WritingSystemFactory.GetStrFromWs(_characterTestStyle.Usage.AvailableWritingSystemIds[0]);
			AssertThatXmlIn.File(styleSheetFile).HasSpecifiedNumberOfMatchesForXpath("/Styles/markup/tag/usage[@wsId='" + enWsId + "']", 1);
			// Test font color, underline, underline color, bold and italic
			var attributeTests = "@family='times' and @color='red' and @underline='double' and @underlineColor='blue' and @bold='true' and @italic='true'";
			AssertThatXmlIn.File(styleSheetFile).HasSpecifiedNumberOfMatchesForXpath("/Styles/markup/tag/font[" + attributeTests + "]", 1);

			AssertThatXmlIn.File(styleSheetFile).HasSpecifiedNumberOfMatchesForXpath("/Styles/markup/tag[@id='" + _paraTestStyle.Name + "']", 1);
			// Test paragraph alignment margins and spacing
			attributeTests = "@lineSpacing='3 pt' and @lineSpacingType='exact' and @alignment='center' and @indentRight='4 pt' and @hanging='5 pt' and @indentLeft='6 pt' and @spaceBefore='7 pt' and @spaceAfter='8 pt'";
			AssertThatXmlIn.File(styleSheetFile).HasSpecifiedNumberOfMatchesForXpath("/Styles/markup/tag/paragraph[" + attributeTests + "]", 1);
			// Test paragraph background color, TODO border type and bullet info
			attributeTests = "@background='(0,255,0)'";
			AssertThatXmlIn.File(styleSheetFile).HasSpecifiedNumberOfMatchesForXpath("/Styles/markup/tag/paragraph[" + attributeTests + "]", 1);

			// Test that a child style gets the basedOn for paragraph and does not write out inherited values
			AssertThatXmlIn.File(styleSheetFile).HasSpecifiedNumberOfMatchesForXpath("/Styles/markup/tag[@id='" + _paraChildTestStyle.Name + "']", 1);
			attributeTests = string.Format("@basedOn='{0}' and @alignment='full' and not(@lineSpacing='3 pt') and not(@indentRight='4 pt')",
				_paraTestStyle.Name);
			AssertThatXmlIn.File(styleSheetFile).HasSpecifiedNumberOfMatchesForXpath("/Styles/markup/tag/paragraph[" + attributeTests + "]", 1);

			// LT-18267 Make sure character styles based on another style have their basedOn
			// information recorded. Assert that there is 1 character style with a non-empty
			// basedOn attribute, that was successfully exported.
			attributeTests = "@type='character' and @basedOn!=''";
			AssertThatXmlIn.File(styleSheetFile).HasSpecifiedNumberOfMatchesForXpath("/Styles/markup/tag[" + attributeTests + "]", 1);

			// Verify that each known unsupported style is excluded from the export
			foreach (var unsupported in DictionaryConfigurationImportController.UnsupportedStyles)
			{
				AssertThatXmlIn.File(styleSheetFile).HasNoMatchForXpath("/Styles/markup/tag[@id='" + unsupported.Replace(' ', '_') + "']");
			}
		}
	}
}
