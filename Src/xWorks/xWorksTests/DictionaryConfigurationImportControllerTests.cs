// Copyright (c) 2017-2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.IO;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;
using FileUtils = SIL.LCModel.Utils.FileUtils;

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
		private DictionaryConfigurationImportController _reversalController;
		private string _projectConfigPath;
		private string _reversalProjectConfigPath;
		private const string configLabel = "importexportConfiguration";
		private const string reversalConfigLabel = "importexportReversalConfiguration";
		private const string configFilename = "importexportConfigurationFile.fwdictconfig";
		private const string reversalConfigFilename = "importexportReversalConfigurationFile.fwdictconfig";
		private const int CustomRedBGR = 0x0000FE;
		private readonly int NamedRedBGR = (int)ColorUtil.ConvertColorToBGR(Color.Red);
		private readonly int NamedGreenBGR = (int)ColorUtil.ConvertColorToBGR(Color.Green);
		private readonly int NamedBlueBGR = (int)ColorUtil.ConvertColorToBGR(Color.Blue);

		/// <summary>
		/// Zip file to import during testing.
		/// </summary>
		private string _zipFile;
		private string _reversalZipFile;

		/// <summary>
		/// Path to a dictionary configuration file that will be deleted after every test.
		/// </summary>
		private string _pathToConfiguration;
		private string _reversalPathToConfiguration;

		[OneTimeTearDown]
		public override void FixtureTeardown()
		{
			// delete the directory that was created in SetUp

			if (Directory.Exists(_projectConfigPath))
				Directory.Delete(_projectConfigPath, true);

			if (Directory.Exists(_reversalProjectConfigPath))
				Directory.Delete(_reversalProjectConfigPath, true);

			base.FixtureTeardown();
		}

		[SetUp]
		public void Setup()
		{
			// Start out with a clean project configuration directory, and with a non-random name so it's easier to examine during testing.
			_projectConfigPath = Path.Combine(Path.GetTempPath(), "Dictionary");
			if (Directory.Exists(_projectConfigPath))
				Directory.Delete(_projectConfigPath, true);
			FileUtils.EnsureDirectoryExists(_projectConfigPath);

			_reversalProjectConfigPath = Path.Combine(Path.GetTempPath(), "ReversalIndex");
			if (Directory.Exists(_reversalProjectConfigPath))
				Directory.Delete(_reversalProjectConfigPath, true);
			FileUtils.EnsureDirectoryExists(_reversalProjectConfigPath);

			_controller = new DictionaryConfigurationImportController(Cache, null, _projectConfigPath,
				new List<DictionaryConfigurationModel>());

			_reversalController = new DictionaryConfigurationImportController(Cache, null, _reversalProjectConfigPath,
				new List<DictionaryConfigurationModel>());

			// Set up data for import testing.

			_zipFile = null;
			_reversalZipFile = null;

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

			// Prepare configuration to export
			var configurationReversalToExport = new DictionaryConfigurationModel
			{
				Label = reversalConfigLabel,
				WritingSystem = "en",
				Publications = new List<string> { "Main Dictionary", "unknown pub 1", "unknown pub 2" },
				Parts = new List<ConfigurableDictionaryNode> { new ConfigurableDictionaryNode { FieldDescription = "LexEntry" } },
				FilePath = Path.GetTempPath() + reversalConfigFilename
			};
			CssGeneratorTests.PopulateFieldsForTesting(configurationReversalToExport);
			_reversalPathToConfiguration = configurationReversalToExport.FilePath;
			configurationReversalToExport.Save();

			// Export a configuration that we know how to import

			_zipFile = Path.GetTempFileName();
			_reversalZipFile = Path.GetTempFileName() + 1;

			// Add test styles to the cache
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var styleFactory = Cache.ServiceLocator.GetInstance<IStStyleFactory>();
				styleFactory.Create(Cache.LangProject.StylesOC, "Dictionary-Headword",
					ContextValues.InternalConfigureView, StructureValues.Body, FunctionValues.Prose, true, 2, true);
				var testStyle = styleFactory.Create(Cache.LangProject.StylesOC, "TestStyle", ContextValues.InternalConfigureView, StructureValues.Undefined,
					FunctionValues.Prose, true, 2, false);
				testStyle.Usage.set_String(Cache.DefaultAnalWs, "Test Style");
				var normalStyle = styleFactory.Create(Cache.LangProject.StylesOC, "Normal", ContextValues.InternalConfigureView, StructureValues.Undefined,
					FunctionValues.Prose, false, 2, true);
				var propsBldr = TsStringUtils.MakePropsBldr();
				propsBldr.SetIntPropValues((int)FwTextPropType.ktptBackColor, (int)FwTextPropVar.ktpvDefault, 0x2BACCA); // arbitrary color
				normalStyle.Rules = propsBldr.GetTextProps();
				var senseStyle = styleFactory.Create(Cache.LangProject.StylesOC, "Dictionary-Sense",
					ContextValues.InternalConfigureView, StructureValues.Body, FunctionValues.Prose, false, 2, true);
				propsBldr.SetIntPropValues((int)FwTextPropType.ktptBackColor, (int)FwTextPropVar.ktpvDefault, 0x2BACCA); // arbitrary color
				propsBldr.SetIntPropValues((int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault, NamedRedBGR);
				propsBldr.SetStrPropValue((int)FwTextPropType.ktptFontFamily, "Arial");
				propsBldr.SetIntPropValues((int)FwTextPropType.ktptFontSize,
					(int)FwTextPropVar.ktpvMilliPoint, 23000);
				propsBldr.SetStrPropValue((int)FwTextPropType.ktptBulNumFontInfo, "");
				senseStyle.Rules = propsBldr.GetTextProps();
				senseStyle.BasedOnRA = normalStyle;
				var styleWithNamedColors = styleFactory.Create(Cache.LangProject.StylesOC, "Nominal", ContextValues.InternalConfigureView, StructureValues.Body,
					FunctionValues.Prose, false, 2, false);
				styleWithNamedColors.BasedOnRA = normalStyle;
				propsBldr = TsStringUtils.MakePropsBldr();
				propsBldr.SetIntPropValues((int)FwTextPropType.ktptBackColor, (int)FwTextPropVar.ktpvDefault, NamedRedBGR);
				propsBldr.SetIntPropValues((int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault, NamedRedBGR);
				styleWithNamedColors.Rules = propsBldr.GetTextProps();
				var styleWithCustomColors = styleFactory.Create(Cache.LangProject.StylesOC, "Abnormal", ContextValues.InternalConfigureView, StructureValues.Heading,
					FunctionValues.Prose, false, 2, false);
				styleWithCustomColors.BasedOnRA = normalStyle;
				propsBldr = TsStringUtils.MakePropsBldr();
				propsBldr.SetIntPropValues((int)FwTextPropType.ktptBackColor, (int)FwTextPropVar.ktpvDefault, CustomRedBGR);
				propsBldr.SetIntPropValues((int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault, CustomRedBGR);
				styleWithCustomColors.Rules = propsBldr.GetTextProps();
				lock(Cache) // Probably not the best mutex, but this should avoid two tests trying to write styles at once.
				{
					DictionaryConfigurationManagerController.ExportConfiguration(configurationToExport, _zipFile, Cache);
					DictionaryConfigurationManagerController.ExportConfiguration(configurationReversalToExport, _reversalZipFile, Cache);
				}
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
			File.Delete(_reversalPathToConfiguration);
			Assert.That(!File.Exists(_reversalPathToConfiguration),
				"Unit test not set up right. Reversal configuration should be out of the way for testing export.");
			Assert.That(_reversalController._configurations.All(config => config.Label != configLabel),
				"Unit test not set up right. A reversal config exists with the same label as the reversal config we will import.");
			Assert.That(_reversalController._configurations.All(config => config.Label != configLabel),
				"Unit test set up unexpectedly. Such a reversal config should not be registered.");
		}

		[TearDown]
		public void TearDown()
		{
			if (_zipFile != null)
				File.Delete(_zipFile);
			if (_pathToConfiguration != null)
				File.Delete(_pathToConfiguration);
			if (_reversalZipFile != null)
				File.Delete(_reversalZipFile);
			if (_reversalPathToConfiguration != null)
				File.Delete(_reversalPathToConfiguration);
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
			Assert.That(_controller.StyleImportHappened, Is.False, "Import hasn't actually happened yet, so don't claim it has");
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
			Assert.That(_controller.StyleImportHappened, Is.True, "Alert that import has happened");
		}

		[Test]
		public void DoImport_ImportsStyles()
		{
			Assert.IsEmpty(Cache.LangProject.StylesOC);
			_controller.PrepareImport(_zipFile);
			CollectionAssert.IsEmpty(Cache.LangProject.StylesOC);
			// SUT
			_controller.DoImport();
			var importedTestStyle = Cache.LangProject.StylesOC.FirstOrDefault(style => style.Name == "TestStyle");
			Assert.NotNull(importedTestStyle, "test style was not imported.");
			Assert.That(importedTestStyle.Usage.BestAnalysisAlternative.Text, Does.Match("Test Style"));
			Assert.AreEqual(importedTestStyle.Context, ContextValues.InternalConfigureView);
			Assert.AreEqual(importedTestStyle.Type, StyleType.kstCharacter);
			Assert.AreEqual(importedTestStyle.UserLevel, 2);
			var importedParaStyle = Cache.LangProject.StylesOC.FirstOrDefault(style => style.Name == "Nominal");
			Assert.NotNull(importedParaStyle, "test style was not imported.");
			int hasColor;
			var color = importedParaStyle.Rules.GetIntPropValues((int)FwTextPropType.ktptBackColor, out hasColor);
			Assert.That(hasColor == 0, "Background color should be set");
			Assert.AreEqual(NamedRedBGR, color, "Background color should be set to Named Red");
			color = importedParaStyle.Rules.GetIntPropValues((int)FwTextPropType.ktptForeColor, out hasColor);
			Assert.That(hasColor == 0, "Foreground color should be set");
			Assert.AreEqual(NamedRedBGR, color, "Foreground color should be set to Named Red");
			importedParaStyle = Cache.LangProject.StylesOC.FirstOrDefault(style => style.Name == "Abnormal");
			Assert.NotNull(importedParaStyle, "test style was not imported.");
			color = importedParaStyle.Rules.GetIntPropValues((int)FwTextPropType.ktptBackColor, out hasColor);
			Assert.That(hasColor == 0, "Background color should be set");
			Assert.AreEqual(CustomRedBGR, color, "Background color should be set to Custom Red");
			color = importedParaStyle.Rules.GetIntPropValues((int)FwTextPropType.ktptForeColor, out hasColor);
			Assert.That(hasColor == 0, "Foreground color should be set");
			Assert.AreEqual(CustomRedBGR, color, "Foreground color should be set to Custom Red");
		}

		/// <summary>
		/// LT-18267: In addition, hook BasedOn and Next back up for the not-overwritten/preserved styles like Homograph-Number.
		/// </summary>
		[Test]
		public void DoImport_UnhandledStylesLeftUnTouched()
		{
			IStStyle bulletStyle = null;
			IStStyle numberStyle = null;
			IStStyle homographStyle = null;
			IStStyle dictionaryHeadwordStyle = null;
			IStStyle nominalStyle = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				// Set up state of flex before the import happens.
				var styleFactory = Cache.ServiceLocator.GetInstance<IStStyleFactory>();
				bulletStyle = styleFactory.Create(Cache.LangProject.StylesOC, "Bulleted List",
					ContextValues.InternalConfigureView, StructureValues.Body, FunctionValues.Prose, false, 2, true);
				numberStyle = styleFactory.Create(Cache.LangProject.StylesOC, "Numbered List",
					ContextValues.InternalConfigureView, StructureValues.Body, FunctionValues.Prose, false, 2, true);

				dictionaryHeadwordStyle = styleFactory.Create(Cache.LangProject.StylesOC, "Dictionary-Headword", ContextValues.InternalConfigureView, StructureValues.Body, FunctionValues.Prose, true, 2, true);

				// Create a style that we can link to before the import happens. It's not
				// important what it's named, just that it also exists in the exported zip
				// file made by Setup().
				nominalStyle = styleFactory.Create(Cache.LangProject.StylesOC, "Nominal",
					ContextValues.InternalConfigureView, StructureValues.Body, FunctionValues.Prose, false, 2, false);

				homographStyle = styleFactory.Create(Cache.LangProject.StylesOC, "Homograph-Number",
					ContextValues.InternalConfigureView, StructureValues.Body, FunctionValues.Prose, true, 2, true);

				// Style linking to later examine
				homographStyle.BasedOnRA = dictionaryHeadwordStyle;
				bulletStyle.NextRA = nominalStyle;
			});

			Assert.AreEqual(5, Cache.LangProject.StylesOC.Count, "Setup problem. Unexpected number of styles before doing any import activity.");
			_controller.PrepareImport(_zipFile);
			Assert.AreEqual(5, Cache.LangProject.StylesOC.Count, "Setup problem. Should not have changed number of styles from just preparing to import.");
			// SUT
			_controller.DoImport();
			Assert.AreEqual(9, Cache.LangProject.StylesOC.Count, "This unit test starts with 6 styles. 3 are 'unsupported' and kept. 3 are removed. We import 6 styles: 3 are completely new; 3 are replacements for the 3 that were removed. Resulting in 9 styles after import.");
			var importedTestStyle = Cache.LangProject.StylesOC.FirstOrDefault(style => style.Name == "TestStyle");
			Assert.NotNull(importedTestStyle, "test style was not imported.");
			var importedParaStyle = Cache.LangProject.StylesOC.FirstOrDefault(style => style.Name == "Nominal");
			Assert.NotNull(importedParaStyle, "test style was not imported.");
			var bulletTestStyle = Cache.LangProject.StylesOC.FirstOrDefault(style => style.Name == "Bulleted List");
			Assert.NotNull(bulletTestStyle, "test style was not imported.");
			Assert.AreEqual(bulletStyle.Guid, bulletTestStyle.Guid);
			var numberTestStyle = Cache.LangProject.StylesOC.FirstOrDefault(style => style.Name == "Numbered List");
			Assert.NotNull(numberTestStyle, "test style was not imported.");
			Assert.AreEqual(numberStyle.Guid, numberTestStyle.Guid);
			var homographTestStyle = Cache.LangProject.StylesOC.FirstOrDefault(style => style.Name == "Homograph-Number");
			Assert.NotNull(homographTestStyle, "test style was not imported.");
			Assert.AreEqual(homographStyle.Guid, homographTestStyle.Guid);

			var dictionaryHeadwordImportedStyle = Cache.LangProject.StylesOC.FirstOrDefault(style => style.Name == "Dictionary-Headword");
			Assert.That(homographTestStyle.BasedOnRA, Is.EqualTo(dictionaryHeadwordImportedStyle), "Failed to rewire basedon to new Dictionary-Headword style. LT-18267");
			var nominalImportedStyle = Cache.LangProject.StylesOC.FirstOrDefault(style => style.Name == "Nominal");
			Assert.That(bulletTestStyle.NextRA, Is.EqualTo(nominalImportedStyle), "Failed to rewire next to new imported style.");
		}

		// TODO (Hoasso) 2026.01: add tests for: bad XML structure (like these already are); combine into a single parameterized test?
		/// <summary>
		/// LT-20393. Attempting to import an invalid style should not cause styles to be deleted.
		/// </summary>
		[Test]
		public void ImportStyles_CatchesErrors_MissingParagraphElement()
		{
			// Set up valid styles before import
			IStStyle unrelatedStyle = null;
			IStStyle linkedStyle = null;
			IStStyle sameNameAsBadStyle = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var styleFactory = Cache.ServiceLocator.GetInstance<IStStyleFactory>();
				unrelatedStyle = styleFactory.Create(Cache.LangProject.StylesOC, "Unrelated Style",
					ContextValues.InternalConfigureView, StructureValues.Body, FunctionValues.Prose, true, 2, true);
				var propsBldr = TsStringUtils.MakePropsBldr();
				propsBldr.SetIntPropValues((int)FwTextPropType.ktptBackColor, (int)FwTextPropVar.ktpvDefault, NamedRedBGR);
				unrelatedStyle.Rules = propsBldr.GetTextProps();

				sameNameAsBadStyle = styleFactory.Create(Cache.LangProject.StylesOC, "Paragraph-Style",
					ContextValues.InternalConfigureView, StructureValues.Body, FunctionValues.Prose, false, 2, true);
				propsBldr = TsStringUtils.MakePropsBldr();
				propsBldr.SetIntPropValues((int)FwTextPropType.ktptSpaceBefore, (int)FwTextPropVar.ktpvMilliPoint, 8000);
				sameNameAsBadStyle.Rules = propsBldr.GetTextProps();

				linkedStyle = styleFactory.Create(Cache.LangProject.StylesOC, "Based on Paragraph Style",
					ContextValues.InternalConfigureView, StructureValues.Body, FunctionValues.Prose, false, 2, true);
				linkedStyle.BasedOnRA = sameNameAsBadStyle;
			});
			// Create and import an XML file with an invalid style
			using (var styleFile = new TempFile( // TODO (Hasso) 2026.01: fill out the rest of the XML
				$@"<tag id='{sameNameAsBadStyle.Name}' guid='{sameNameAsBadStyle.Guid}' userlevel='0' context='general' type='paragraph'>
  <font />
</tag>"))
			{
				// SUT
				Assert.Throws<InstallationException>(() => _controller.ImportStyles(styleFile.Path),
					"For legacy reasons, the import code throws installation exceptions.");
			}

			// Verify that the styles are still there.
			Assert.That(Cache.LangProject.StylesOC.Count, Is.EqualTo(3), "The number of styles should not have changed");
			var foundUnrelatedStyle = Cache.LangProject.StylesOC.FirstOrDefault(style => style.Name == "Unrelated Style");
			Assert.That(foundUnrelatedStyle, Is.Not.Null, "Should have found 'Unrelated Style'");
			Assert.That(foundUnrelatedStyle.Rules.GetIntPropValues((int)FwTextPropType.ktptBackColor, out _), Is.EqualTo(NamedRedBGR), "colour should be the same");
			Assert.That(foundUnrelatedStyle.Guid, Is.EqualTo(unrelatedStyle.Guid), "'Unrelated Style' GUID should be the same");
			var foundSameNameAsBadStyle = Cache.LangProject.StylesOC.FirstOrDefault(style => style.Name == "Paragraph-Style");
			Assert.That(foundSameNameAsBadStyle, Is.Not.Null, "Should have found 'Paragraph-Style'");
			Assert.That(foundSameNameAsBadStyle.Rules.GetIntPropValues((int)FwTextPropType.ktptSpaceBefore, out _), Is.EqualTo(8000), "space before should be the same");
			Assert.That(foundSameNameAsBadStyle.Guid, Is.EqualTo(sameNameAsBadStyle.Guid), "'Paragraph-Style' GUID should be the same");
			var foundLinkedStyle = Cache.LangProject.StylesOC.FirstOrDefault(style => style.Name == "Based on Paragraph Style");
			Assert.That(foundLinkedStyle, Is.Not.Null, "Should have found 'Based on Paragraph Style'");
			Assert.That(foundLinkedStyle.Guid, Is.EqualTo(linkedStyle.Guid), "'Based on Paragraph Style' GUID should be the same");
			Assert.That(foundLinkedStyle.BasedOnRA, Is.EqualTo(foundSameNameAsBadStyle), "link should be preserved");
		}

		/// <summary>
		/// LT-20393. Attempting to import an invalid style should not cause styles to be deleted.
		/// </summary>
		[TestCase(@"<tag id='Malformed Style' guid='bcc696ef-0740-440d-abb2-323544b5a851' userlevel='2' context='internalConfigureView' type='character' structure='body'>
	</tag>", typeof(NullReferenceException), TestName = "Missing required Font element")]
		[TestCase(@"<tag id='{0}' guid='bcc696ef-0740-440d-abb2-323544b5a851' userlevel='0' context='general' type='character' structure='heading'>
	  <font />
	</tag>", typeof(InstallationException), TestName = "Incompatible Structure (heading, not body)")]
		[TestCase(@"<tag id='Plain_Paragraph' guid='24dfbd48-09f7-4fa1-9ca6-3b210d03068d' userlevel='2' context='internalConfigureView' type='paragraph'>
	  <font size='12' />
	</tag>", typeof(InstallationException), TestName = "Paragraph missing Paragraph Element")]
		public void ImportStyles_CatchesErrors(string xmlToImport, Type exceptionType)
		{
			// Set up valid styles before import
			IStStyle unrelatedStyle = null;
			IStStyle linkedStyle = null;
			IStStyle sameNameAsBadStyle = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var styleFactory = Cache.ServiceLocator.GetInstance<IStStyleFactory>();
				unrelatedStyle = styleFactory.Create(Cache.LangProject.StylesOC, "Unrelated Style",
					ContextValues.InternalConfigureView, StructureValues.Body, FunctionValues.Prose, true, 2, true);
				var propsBldr = TsStringUtils.MakePropsBldr();
				propsBldr.SetIntPropValues((int)FwTextPropType.ktptBackColor, (int)FwTextPropVar.ktpvDefault, NamedRedBGR);
				unrelatedStyle.Rules = propsBldr.GetTextProps();

				sameNameAsBadStyle = styleFactory.Create(Cache.LangProject.StylesOC, "Testable Style",
					ContextValues.InternalConfigureView, StructureValues.Body, FunctionValues.Prose, true, 2, true);
				propsBldr = TsStringUtils.MakePropsBldr();
				propsBldr.SetIntPropValues((int)FwTextPropType.ktptFontSize, (int)FwTextPropVar.ktpvMilliPoint, 18);
				sameNameAsBadStyle.Rules = propsBldr.GetTextProps();

				linkedStyle = styleFactory.Create(Cache.LangProject.StylesOC, "Based on Testable Style",
					ContextValues.InternalConfigureView, StructureValues.Body, FunctionValues.Prose, true, 2, true);
				linkedStyle.BasedOnRA = sameNameAsBadStyle;
			});
			// Create and import an XML file with an invalid style
			using (var styleFile = new TempFile($@"<?xml version='1.0' encoding='utf-8'?>
<Styles DTDver='1610190E-D7A3-42D7-8B48-C0C49320435F' label='Flex Dictionary' date='2026-01-16'>
  <markup version='e5238df8-6fcb-4350-9c85-db9c9726381b'>{string.Format(xmlToImport, sameNameAsBadStyle.Name)}</markup>
</Styles>"))
			{
				// SUT
				Assert.Throws(exceptionType, () => _controller.ImportStyles(styleFile.Path));
			}

			// Verify that the styles are still there.
			Assert.That(Cache.LangProject.StylesOC.Count, Is.EqualTo(3), "The number of styles should not have changed");
			var foundUnrelatedStyle = Cache.LangProject.StylesOC.FirstOrDefault(style => style.Name == "Unrelated Style");
			Assert.That(foundUnrelatedStyle, Is.Not.Null, "Should have found 'Unrelated Style'");
			Assert.That(foundUnrelatedStyle.Rules.GetIntPropValues((int)FwTextPropType.ktptBackColor, out _), Is.EqualTo(NamedRedBGR), "colour should be the same");
			Assert.That(foundUnrelatedStyle.Guid, Is.EqualTo(unrelatedStyle.Guid), "'Unrelated Style' GUID should be the same");
			var foundSameNameAsBadStyle = Cache.LangProject.StylesOC.FirstOrDefault(style => style.Name == "Testable Style");
			Assert.That(foundSameNameAsBadStyle, Is.Not.Null, "Should have found 'Testable Style'");
			Assert.That(foundSameNameAsBadStyle.Rules.GetIntPropValues((int)FwTextPropType.ktptFontSize, out _), Is.EqualTo(18), "font size should be the same");
			Assert.That(foundSameNameAsBadStyle.Guid, Is.EqualTo(sameNameAsBadStyle.Guid), "'Testable Style' GUID should be the same");
			var foundLinkedStyle = Cache.LangProject.StylesOC.FirstOrDefault(style => style.Name == "Based on Testable Style");
			Assert.That(foundLinkedStyle, Is.Not.Null, "Should have found 'Based on Testable Style'");
			Assert.That(foundLinkedStyle.Guid, Is.EqualTo(linkedStyle.Guid), "'Based on Testable Style' GUID should be the same");
			Assert.That(foundLinkedStyle.BasedOnRA, Is.EqualTo(foundSameNameAsBadStyle), "link should be preserved");
		}

		/// <summary>
		/// Prior to FW 9.3.5, the structure and use attributes were not included in the style export. This should not prevent import.
		/// </summary>
		[Test]
		public void ImportStyles_ToleratesMissingStructureAndUse()
		{
			// TODO (Hasso) 2026.01: implement
			// Set up valid styles before import
			IStStyle headingStyle = null;
			IStStyle normalStyle = null;
			IStStyle styleWithNamedColors = null;
			IStStyle chapterStyle = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var styleFactory = Cache.ServiceLocator.GetInstance<IStStyleFactory>();
				headingStyle = styleFactory.Create(Cache.LangProject.StylesOC, "Dictionary-Letter-Heading",
					ContextValues.InternalConfigureView, StructureValues.Heading, FunctionValues.Prose, true, 2, true);
				normalStyle = styleFactory.Create(Cache.LangProject.StylesOC, "Normal", ContextValues.InternalConfigureView, StructureValues.Undefined,
					FunctionValues.Prose, false, 2, true);
				styleWithNamedColors = styleFactory.Create(Cache.LangProject.StylesOC, "Nominal", ContextValues.InternalConfigureView, StructureValues.Body,
					FunctionValues.Prose, false, 2, true);
				styleWithNamedColors.BasedOnRA = normalStyle;
				var propsBldr = TsStringUtils.MakePropsBldr();
				propsBldr.SetIntPropValues((int)FwTextPropType.ktptBackColor, (int)FwTextPropVar.ktpvDefault, NamedRedBGR);
				propsBldr.SetIntPropValues((int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault, NamedGreenBGR);
				styleWithNamedColors.Rules = propsBldr.GetTextProps();
				chapterStyle = styleFactory.Create(Cache.LangProject.StylesOC, "Chapter Number", ContextValues.Text, StructureValues.Body,
					FunctionValues.Chapter, true, 0, true);
				propsBldr = TsStringUtils.MakePropsBldr();
				propsBldr.SetIntPropValues((int)FwTextPropType.ktptItalic, (int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvForceOn);
				DictionaryConfigurationManagerController.PrepareStylesheetExport(Cache);
			});


			// Create and import an XML file with a style missing structure and use attributes
			using (var styleFile = new TempFile($@"<?xml version='1.0' encoding='utf-8'?>
<Styles DTDver='1610190E-D7A3-42D7-8B48-C0C49320435F' label='Flex Dictionary' date='2026-01-16'>
  <markup version='e5238df8-6fcb-4350-9c85-db9c9726381b'>
	<tag id='{headingStyle.Name}' guid='{headingStyle.Guid}' userlevel='2' context='internalConfigureView' type='character'>
	  <font bold='true' />
	</tag>
	<tag id='{normalStyle.Name}' guid='{normalStyle.Guid}' userlevel='2' context='internalConfigureView' type='paragraph'>
	  <font />
	  <paragraph spaceBefore='9 pt' />
	</tag>
	<tag id='{styleWithNamedColors.Name}' guid='{styleWithNamedColors.Guid}' userlevel='2' context='internalConfigureView' type='paragraph'>
	  <font backcolor='green' color='blue' />
	  <paragraph background='(0,128,0)' basedOn='{normalStyle.Name}' /><!--'(0,128,0)' is 'Green', '(0,255,0)' is 'Lime'.-->
	</tag>
	<tag id='{chapterStyle.Name}' guid='{chapterStyle.Guid}' userlevel='0' context='text' type='character'>
	  <font />
	</tag>
  </markup>
</Styles>"))
			{
				// SUT
				_controller.ImportStyles(styleFile.Path);
			}

			// Verify that styles have been imported but that structure and use have been preserved
			Assert.That(Cache.LangProject.StylesOC.Count, Is.EqualTo(4), "No styles should have been added or deleted; only updated.");
			var foundHeadingStyle = Cache.LangProject.StylesOC.FirstOrDefault(style => style.Name == "Dictionary-Letter-Heading");
			Assert.That(foundHeadingStyle, Is.Not.Null, "Should have found 'Dictionary-Letter-Heading'");
			Assert.That(foundHeadingStyle.Structure, Is.EqualTo(StructureValues.Heading),
				"Structure value for 'Dictionary-Letter-Heading' should have been preserved despite being missing from the XML");
			Assert.That(foundHeadingStyle.Function, Is.EqualTo(FunctionValues.Prose), "Function value for Dictionary-Letter-Heading should be the default");
			Assert.That(foundHeadingStyle.Rules.IntPropCount, Is.EqualTo(1), "The imported Dictionary-Letter-Heading should have one property");
			Assert.That(foundHeadingStyle.Rules.GetIntPropValues((int)FwTextPropType.ktptBold, out _), Is.EqualTo((int)FwTextToggleVal.kttvInvert), "should be bold after import");
			var foundNormalStyle = Cache.LangProject.StylesOC.FirstOrDefault(style => style.Name == "Normal");
			Assert.That(foundNormalStyle, Is.Not.Null, "Should have found 'Normal'");
			Assert.That(foundNormalStyle.Structure, Is.EqualTo(StructureValues.Undefined), "Structure value for 'Normal' should be the default");
			Assert.That(foundNormalStyle.Function, Is.EqualTo(FunctionValues.Prose), "Function value for 'Normal' should be the default");
			Assert.That(foundNormalStyle.Rules.IntPropCount, Is.EqualTo(1), "The imported Normal style should have one property");
			Assert.That(foundNormalStyle.Rules.GetIntPropValues((int)FwTextPropType.ktptSpaceBefore, out _), Is.EqualTo(9000), "Space before should be 9k milliPt");
			var foundStyleWithNamedColors = Cache.LangProject.StylesOC.FirstOrDefault(style => style.Name == "Nominal");
			Assert.That(foundStyleWithNamedColors, Is.Not.Null, "Should have found 'Nominal'");
			Assert.That(foundStyleWithNamedColors.Structure, Is.EqualTo(StructureValues.Body),
				"Structure value for 'Nominal' should have been preserved despite being missing from the XML");
			Assert.That(foundStyleWithNamedColors.Function, Is.EqualTo(FunctionValues.Prose), "Function value for 'Nominal' should be the default");
			Assert.That(foundStyleWithNamedColors.Rules.IntPropCount, Is.EqualTo(2), "The imported Nominal style should have two properties");
			Assert.That(foundStyleWithNamedColors.Rules.GetIntPropValues((int)FwTextPropType.ktptBackColor, out _), Is.EqualTo(NamedGreenBGR), "Background color should be Green");
			Assert.That(foundStyleWithNamedColors.Rules.GetIntPropValues((int)FwTextPropType.ktptForeColor, out _), Is.EqualTo(NamedBlueBGR), "Foreground color should be Blue");
			Assert.That(foundStyleWithNamedColors.BasedOnRA, Is.EqualTo(foundNormalStyle), "'Nominal' should be based on 'Normal' before & after import");
			var foundChapterStyle = Cache.LangProject.StylesOC.FirstOrDefault(style => style.Name == "Chapter Number");
			Assert.That(foundChapterStyle, Is.Not.Null, "Should have found 'Chapter Number'");
			Assert.That(foundChapterStyle.Structure, Is.EqualTo(StructureValues.Body),
				"Structure value for 'Chapter Number' should have been preserved despite being missing from the XML");
			Assert.That(foundChapterStyle.Function, Is.EqualTo(FunctionValues.Chapter),
				"Function value for 'Chapter Number' should have been preserved despite being missing from the XML");
			Assert.That(foundChapterStyle.Rules.IntPropCount, Is.EqualTo(0), "The imported Chapter Number style should have no properties");
		}

		[Test]
		public void PrepareImport_DoesNotChangeRealData()
		{
			// SUT
			_controller.PrepareImport(_zipFile);

			Assert.That(!File.Exists(Path.Combine(_projectConfigPath, configFilename)),
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
			FileUtils.WriteStringToFile(alreadyExistingModelWithSameLabel.FilePath, "arbitrary file content", Encoding.UTF8);
			var anotherAlreadyExistingModel = new DictionaryConfigurationModel
			{
				Label = "importexportConfiguration-Imported1",
				Publications = new List<string> { "Main Dictionary", "unknown pub 1", "unknown pub 2" },
				FilePath = Path.GetTempPath() + configFilename
			};
			DictionaryConfigurationManagerController.GenerateFilePath(_projectConfigPath, _controller._configurations,
				anotherAlreadyExistingModel);
			FileUtils.WriteStringToFile(anotherAlreadyExistingModel.FilePath, "arbitrary file content", Encoding.UTF8);

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
			Assert.That(_controller.StyleImportHappened, Is.True, "Unit test not set up correctly.");
			// SUT 1
			_controller.PrepareImport(_zipFile);
			Assert.That(_controller.StyleImportHappened, Is.False, "The import dialog and controller isn't really meant to be used this way, but don't let it be so that ImportHappened can be true yet NewConfigToImport is only just freshly prepared and not imported yet.");

			_controller.DoImport();
			Assert.That(_controller.StyleImportHappened, Is.True, "Unit test not set up correctly.");
			// SUT 2
			_controller.PrepareImport("nonexistent.zip");
			Assert.That(_controller.StyleImportHappened, Is.False, "Also should be false in this case since NewConfigToImport==null");
		}

		[Test]
		public void PrepareImport_ValidateImportConfigs()
		{
			// Import a Dictionary view into a Dictionary area
			_controller.PrepareImport(_zipFile);
			Assert.That(_controller.NewConfigToImport, Is.Not.Null, "Dictionary configuration should have been prepared for import, since we requested to import the right kind of configuration (Dictionary into Dictionary area).");

			// Import a Dictionary view into a ReversalIndex area
			_reversalController.PrepareImport(_zipFile);
			Assert.That(_reversalController.NewConfigToImport, Is.Null, "No configuration to import should have been prepared since the wrong type of configuration was requested to be imported (Dictionary into Reversal area).");

			// Import a Reversal view into a Dictionary area
			_controller.PrepareImport(_reversalZipFile);
			Assert.That(_controller.NewConfigToImport, Is.Null, "No configuration to import should have been prepared since the wrong type of configuration was requested to be imported (Reversal into Dictionary area).");

			// Import a Reversal view into a ReversalIndex area
			_reversalController.PrepareImport(_reversalZipFile);
			Assert.That(_reversalController.NewConfigToImport, Is.Not.Null, "Reversal configuration should have been prepared for import, since we requested to import the right kind of configuration (Reversal into Reversal area).");
		}

		/// <summary>
		/// When a dictionary configuration is imported, any publications it mentions that Flex doesn't know about yet should be added to the list of publications that Flex knows about.
		/// </summary>
		[Test]
		public void DoImport_AddsNewPublications()
		{
			var configFile=FileUtils.GetTempFile("unittest.xml");
			const string XmlOpenTagsThruRoot = @"<?xml version=""1.0"" encoding=""utf-8""?>
			<DictionaryConfiguration name=""Root"" allPublications=""true"" isRootBased=""true"" version=""1"" lastModified=""2014-02-13"">";
			const string XmlCloseTagsFromRoot = @"</DictionaryConfiguration>";
			FileUtils.WriteStringToFile(configFile, XmlOpenTagsThruRoot +
				"<ConfigurationItem name=\"Main Entry\" style=\"Dictionary-Normal\" styleType=\"paragraph\" isEnabled=\"true\" field=\"LexEntry\" cssClassNameOverride=\"entry\"></ConfigurationItem>"
				+ XmlCloseTagsFromRoot, Encoding.UTF8);
			_controller._temporaryImportConfigLocation = configFile;
			_controller.NewConfigToImport = new DictionaryConfigurationModel()
			{
				Label = "blah",
				Publications = new List<string>() {"pub 1", "pub 2"},
				Parts = new List<ConfigurableDictionaryNode> { new ConfigurableDictionaryNode() }
			};
			// SUT
			_controller._proposedNewConfigLabel = "blah";
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
			using (new CustomFieldForTest(Cache, customFieldSameLabel, customFieldSameLabel, LexSenseTags.kClassId, StTextTags.kClassId, -1,
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
					const string XmlOpenTagsThruRoot = @"<?xml version=""1.0"" encoding=""utf-8""?>
			<DictionaryConfiguration name=""Root"" allPublications=""true"" isRootBased=""true"" version=""1"" lastModified=""2014-02-13"">";
					const string XmlCloseTagsFromRoot = @"</DictionaryConfiguration>";
					const string XmlTagsHeaword = @"<ConfigurationItem name=""Main Entry"" isEnabled=""true"" field=""LexEntry"">\r\n\t\t\t\t\t<ConfigurationItem name=""Testword"" nameSuffix=""2b"" before=""["" between="", "" after=""] "" style=""Dictionary-Headword"" isEnabled=""true"" field=""HeadWord"">""\r\n\r\n\r\n\t\t\t\t\t</ConfigurationItem>\r\n\t\t\t\t</ConfigurationItem>\r\n\t\t\t\t<SharedItems/>";
					const string XmlTagsCustomField = @" <ConfigurationItem name = ""CustomField1"" isEnabled=""true"" isCustomField=""true"" before="" "" field=""OwningEntry"" subField=""CustomField1"" />";
					File.WriteAllText(_pathToConfiguration,
						XmlOpenTagsThruRoot + XmlTagsHeaword + XmlTagsCustomField + XmlCloseTagsFromRoot);
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
					var configToImport = (DictionaryConfigurationModel)_controller.NewConfigToImport;
					// Assert that the field which was Enabled or not
					Assert.IsTrue(configToImport.Parts[1].IsEnabled, "CustomField1 should be enabled");
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
			Assert.Throws<LcmInvalidFieldException>(() => mdc.GetFieldId2(classWithCustomField, customFieldLabel, false));
		}

		/// <summary>
		/// LT-18246:Number and Bullet style information lost on export/import of "Configure Dictionary"
		/// </summary>
		[Test]
		public void DoImport_CustomBulletInfoIsImported()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				// Set up state of flex before the import happens.
				var styleFactory = Cache.ServiceLocator.GetInstance<IStStyleFactory>();
				styleFactory.Create(Cache.LangProject.StylesOC, "Dictionary-Sense", ContextValues.InternalConfigureView, StructureValues.Body, FunctionValues.Prose, false, 2, true);
			});
			Assert.AreEqual(1, Cache.LangProject.StylesOC.Count, "Setup problem. Unexpected number of styles before doing any import activity.");
			_controller.PrepareImport(_zipFile);
			Assert.AreEqual(1, Cache.LangProject.StylesOC.Count, "Setup problem. Should not have changed number of styles from just preparing to import.");
			// SUT
			_controller.DoImport();
			Assert.AreEqual(6, Cache.LangProject.StylesOC.Count, "Resulting styles count should be 6 after import.");
			var importedSenseStyle = Cache.LangProject.StylesOC.FirstOrDefault(style => style.Name == "Dictionary-Sense");
			Assert.NotNull(importedSenseStyle, "Dictionary-Sense style was not imported.");
		}
	}
}
