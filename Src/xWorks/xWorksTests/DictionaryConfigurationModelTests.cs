// Copyright (c) 2014-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using NUnit.Framework;
using SIL.LCModel.Core.Text;
using SIL.IO;
using SIL.TestUtilities;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;

// ReSharper disable InconsistentNaming

namespace SIL.FieldWorks.XWorks
{
	[TestFixture]
	public class DictionaryConfigurationModelTests : MemoryOnlyBackendProviderTestBase
	{
		private const string XmlOpenTagsThruRoot = @"<?xml version=""1.0"" encoding=""utf-8""?>
			<DictionaryConfiguration name=""Root"" version=""1"" lastModified=""2014-02-13"">";
		private const string XmlOpenTagsThruRootWithAllPublications = @"<?xml version=""1.0"" encoding=""utf-8""?>
			<DictionaryConfiguration allPublications=""true"" name=""Root"" version=""1"" lastModified=""2014-02-13"">";
		internal const string XmlOpenTagsThruHeadword =
				XmlOpenTagsThruRoot +
				@"<ConfigurationItem name=""Main Entry"" isEnabled=""true"" field=""LexEntry"">
					<ConfigurationItem name=""Testword"" nameSuffix=""2b""
							before=""["" between="", "" after=""] "" style=""Dictionary-Headword"" isEnabled=""true"" field=""HeadWord"">";

		internal const string XmlCloseTagsFromHeadword = @"
					</ConfigurationItem>
				</ConfigurationItem>
				<SharedItems/>" +
			XmlCloseTagsFromRoot;
		private const string XmlCloseTagsFromRoot = @"</DictionaryConfiguration>";

		private const string m_reference = "Reference";
		private const string m_field = "LexEntry";

		[OneTimeSetUp]
		public void DictionaryConfigModelFixtureSetup()
		{
			CreateStandardStyles();
			TempFile.NamePrefix = GetType().FullName;
		}

		[OneTimeTearDown]
		public void OneTimeTeardown()
		{
			TempFile.NamePrefix = null;
		}

		private void CreateStandardStyles()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var fact = Cache.ServiceLocator.GetInstance<IStStyleFactory>();
				CreateStyle(fact, "Dictionary-Headword", StyleType.kstCharacter);	// needed by Load_LoadsBasicsAndDetails
				CreateStyle(fact, "bold", StyleType.kstCharacter);					// needed by Load_LoadsSenseOptions
			});
		}

		private void CreateStyle(IStStyleFactory fact, string name, StyleType type)
		{
			var st = fact.Create();
			Cache.LangProject.StylesOC.Add(st);
			st.Name = name;
			st.Type = type;
		}

		[Test]
		public void Load_LoadsBasicsAndDetails()
		{
			DictionaryConfigurationModel model;
			using (var modelFile = new TempFile(new[] { XmlOpenTagsThruHeadword, XmlCloseTagsFromHeadword }))
			{
				// SUT
				model = new DictionaryConfigurationModel(modelFile.Path, Cache);
			}

			// basic info
			Assert.AreEqual("Root", model.Label);
			Assert.AreEqual(1, model.Version);
			Assert.AreEqual(new DateTime(2014, 02, 13), model.LastModified);

			// Main Entry
			Assert.AreEqual(1, model.Parts.Count);
			var rootConfigNode = model.Parts[0];
			Assert.AreEqual("Main Entry", rootConfigNode.Label);
			Assert.AreEqual("LexEntry", rootConfigNode.FieldDescription);
			Assert.That(rootConfigNode.LabelSuffix, Is.Null.Or.Empty);
			Assert.That(rootConfigNode.SubField, Is.Null.Or.Empty);
			Assert.That(rootConfigNode.Before, Is.Null.Or.Empty);
			Assert.That(rootConfigNode.Between, Is.Null.Or.Empty);
			Assert.That(rootConfigNode.After, Is.Null.Or.Empty);
			Assert.IsFalse(rootConfigNode.IsCustomField);
			Assert.IsFalse(rootConfigNode.IsDuplicate);
			Assert.IsTrue(rootConfigNode.IsEnabled);

			// Testword
			Assert.AreEqual(1, rootConfigNode.Children.Count);
			var headword = rootConfigNode.Children[0];
			Assert.AreEqual("Testword", headword.Label);
			Assert.AreEqual("2b", headword.LabelSuffix);
			Assert.AreEqual("Dictionary-Headword", model.Parts[0].Children[0].Style);
			Assert.AreEqual("[", headword.Before);
			Assert.AreEqual(", ", headword.Between);
			Assert.AreEqual("] ", headword.After);
			Assert.IsTrue(headword.IsEnabled);
		}

		[Test]
		public void Load_LoadsWritingSystemOptions()
		{
			DictionaryConfigurationModel model;
			using (var modelFile = new TempFile(new[]
			{
				XmlOpenTagsThruHeadword, @"
				<WritingSystemOptions writingSystemType=""vernacular"" displayWSAbreviation=""true"">
					<Option id=""fr"" isEnabled=""true""/>
				</WritingSystemOptions>",
				XmlCloseTagsFromHeadword
			}))
			{
				// SUT
				model = new DictionaryConfigurationModel(modelFile.Path, Cache);
			}

			var testNodeOptions = model.Parts[0].Children[0].DictionaryNodeOptions;
			Assert.IsInstanceOf(typeof(DictionaryNodeWritingSystemOptions), testNodeOptions);
			var wsOptions = (DictionaryNodeWritingSystemOptions)testNodeOptions;
			Assert.IsTrue(wsOptions.DisplayWritingSystemAbbreviations);
			Assert.AreEqual(DictionaryNodeWritingSystemOptions.WritingSystemType.Vernacular, wsOptions.WsType);
			Assert.AreEqual(1, wsOptions.Options.Count);
			Assert.AreEqual("fr", wsOptions.Options[0].Id);
			Assert.IsTrue(wsOptions.Options[0].IsEnabled);
		}

		[Test]
		[TestCase("%O")] // It's obsolete,so we changed to %d
		[TestCase("%d")]
		public void Load_LoadsSenseOptions(string numberingStyle)
		{
			DictionaryConfigurationModel model;
			using (var modelFile = new TempFile(new[]
			{
				XmlOpenTagsThruHeadword, string.Format(@"
				<SenseOptions displayEachSenseInParagraph=""true"" numberStyle=""bold"" numberBefore=""("" numberAfter="") ""
						numberingStyle=""{0}"" numberFont="""" numberSingleSense=""true"" showSingleGramInfoFirst=""true""/>", numberingStyle),
				XmlCloseTagsFromHeadword
			}))
			{
				// SUT
				model = new DictionaryConfigurationModel(modelFile.Path, Cache);
			}

			// The following assertions are based on the specific test data loaded from the file
			var testNodeOptions = model.Parts[0].Children[0].DictionaryNodeOptions;
			Assert.IsInstanceOf(typeof(DictionaryNodeSenseOptions), testNodeOptions);
			var senseOptions = (DictionaryNodeSenseOptions)testNodeOptions;
			Assert.That(senseOptions.NumberingStyle, Is.EqualTo("%d"), "NumberingStyle should be same");
			Assert.AreEqual("(", senseOptions.BeforeNumber);
			Assert.AreEqual(") ", senseOptions.AfterNumber);
			Assert.AreEqual("bold", senseOptions.NumberStyle);
			Assert.IsTrue(senseOptions.DisplayEachSenseInAParagraph);
			Assert.IsTrue(senseOptions.NumberEvenASingleSense);
			Assert.IsTrue(senseOptions.ShowSharedGrammarInfoFirst);
		}

		[Test]
		public void Load_LoadsListOptions()
		{
			DictionaryConfigurationModel model;
			using (var modelFile = new TempFile(new[]
			{
				XmlOpenTagsThruHeadword, @"
				<ListTypeOptions list=""variant"">
					<Option isEnabled=""true"" id=""b0000000-c40e-433e-80b5-31da08771344""/>
					<Option isEnabled=""true"" id=""abcdef01-2345-6789-abcd-ef0123456789""/>
					<Option isEnabled=""true"" id=""024b62c9-93b3-41a0-ab19-587a0030219a""/>
					<Option isEnabled=""true"" id=""4343b1ef-b54f-4fa4-9998-271319a6d74c""/>
					<Option isEnabled=""true"" id=""01d4fbc1-3b0c-4f52-9163-7ab0d4f4711c""/>
					<Option isEnabled=""true"" id=""837ebe72-8c1d-4864-95d9-fa313c499d78""/>
					<Option isEnabled=""true"" id=""a32f1d1c-4832-46a2-9732-c2276d6547e8""/>
					<Option isEnabled=""true"" id=""0c4663b3-4d9a-47af-b9a1-c8565d8112ed""/>
				</ListTypeOptions>",
				XmlCloseTagsFromHeadword
			}))
			{
				// SUT
				model = new DictionaryConfigurationModel(modelFile.Path, Cache);
			}

			// The following assertions are based on the specific test data loaded from the file
			var testNodeOptions = model.Parts[0].Children[0].DictionaryNodeOptions;
			Assert.IsInstanceOf(typeof(DictionaryNodeListOptions), testNodeOptions);
			var listOptions = (DictionaryNodeListOptions)testNodeOptions;
			Assert.AreEqual(DictionaryNodeListOptions.ListIds.Variant, listOptions.ListId);
			// The first guid (b0000000-c40e-433e-80b5-31da08771344) is a special marker for
			// "No Variant Type".  The second guid does not exist, so it gets removed from the list.
			Assert.AreEqual(8, listOptions.Options.Count);
			Assert.AreEqual(8, listOptions.Options.Count(option => option.IsEnabled));
			Assert.AreEqual("b0000000-c40e-433e-80b5-31da08771344", listOptions.Options[0].Id);
		}

		[Test]
		public void Load_LoadsListAndParaOptions()
		{
			DictionaryConfigurationModel model;
			using (var modelFile = new TempFile(new[]
			{
				XmlOpenTagsThruHeadword, @"
				<ComplexFormOptions list=""complex"" displayEachComplexFormInParagraph=""true"">
					<Option isEnabled=""true""  id=""a0000000-dd15-4a03-9032-b40faaa9a754""/>
					<Option isEnabled=""true""  id=""1f6ae209-141a-40db-983c-bee93af0ca3c""/>
					<Option isEnabled=""true""  id=""73266a3a-48e8-4bd7-8c84-91c730340b7d""/>
					<Option isEnabled=""true""  id=""abcdef01-2345-6789-abcd-ef0123456789""/>
				</ComplexFormOptions>",
				XmlCloseTagsFromHeadword
			}))
			{
				// SUT
				model = new DictionaryConfigurationModel(modelFile.Path, Cache);
			}

			// The following assertions are based on the specific test data loaded from the file
			var testNodeOptions = model.Parts[0].Children[0].DictionaryNodeOptions;
			Assert.IsInstanceOf(typeof(DictionaryNodeListAndParaOptions), testNodeOptions);
			var lpOptions = (DictionaryNodeListAndParaOptions)testNodeOptions;
			Assert.AreEqual(DictionaryNodeListOptions.ListIds.Complex, lpOptions.ListId);
			Assert.IsTrue(lpOptions.DisplayEachInAParagraph);
			// There are seven complex form types by default in the language project.  (The second and third
			// guids above are used by two of those default types.)  Ones that are missing in the configuration
			// data are added in, ones that the configuration has but which don't exist in the language project
			// are removed.  Note that the first one above (a0000000-dd15-4a03-9032-b40faaa9a754) is a special
			// value used to indicate "No Complex Form Type".  The fourth value does not exist.
			Assert.AreEqual(8, lpOptions.Options.Count);
			Assert.AreEqual(8, lpOptions.Options.Count(option => option.IsEnabled));
			Assert.AreEqual("a0000000-dd15-4a03-9032-b40faaa9a754", lpOptions.Options[0].Id);
		}

		[Test]
		public void Load_NoListSpecifiedResultsInNone()
		{
			DictionaryConfigurationModel model;
			using (var modelFile = new TempFile(new[]
			{
				XmlOpenTagsThruHeadword, @"
				<ComplexFormOptions displayEachComplexFormInParagraph=""false""/>",
				XmlCloseTagsFromHeadword
			}))
			{
				// SUT
				model = new DictionaryConfigurationModel(modelFile.Path, Cache);
			}

			// The following assertions are based on the specific test data loaded from the file
			var testNodeOptions = model.Parts[0].Children[0].DictionaryNodeOptions;
			Assert.IsInstanceOf(typeof(DictionaryNodeListAndParaOptions), testNodeOptions);
			var lpOptions = (DictionaryNodeListAndParaOptions)testNodeOptions;
			Assert.AreEqual(DictionaryNodeListOptions.ListIds.None, lpOptions.ListId);
			Assert.That(lpOptions.Options, Is.Null.Or.Empty);
			Assert.IsFalse(lpOptions.DisplayEachInAParagraph);
		}

		[Test]
		public void Load_LoadsPublications()
		{
			// "Main Dictionary" was added by base class
			ICmPossibility addedPublication = AddPublication("Another Dictionary");

			DictionaryConfigurationModel model;
			using (var modelFile = new TempFile(new[]
			{
				XmlOpenTagsThruRoot,
				"<Publications><Publication>Main Dictionary</Publication><Publication>Another Dictionary</Publication></Publications>",
				XmlCloseTagsFromRoot
			}))
			{
				// SUT
				model = new DictionaryConfigurationModel(modelFile.Path, Cache);
			}

			Assert.IsNotEmpty(model.Publications);
			Assert.AreEqual(2, model.Publications.Count);
			Assert.AreEqual("Main Dictionary", model.Publications[0]);
			Assert.AreEqual("Another Dictionary", model.Publications[1]);

			RemovePublication(addedPublication);
		}

		private ICmPossibility AddPublication(string publicationName)
		{
			ICmPossibility result = null;
			NonUndoableUnitOfWorkHelper.Do(m_actionHandler, () =>
			{
				if (Cache.LangProject.LexDbOA.PublicationTypesOA == null)
					Cache.LangProject.LexDbOA.PublicationTypesOA =
						Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
				result = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
				Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS.Add(result);
				result.Name.AnalysisDefaultWritingSystem = TsStringUtils.MakeString(publicationName,
					Cache.DefaultAnalWs);
			});
			return result;
		}

		private void RemovePublication(ICmPossibility publication)
		{
			NonUndoableUnitOfWorkHelper.Do(m_actionHandler, () =>
			{
				Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS.Remove(publication);
			});
		}

		private readonly List<string[]> m_NoPublicationsList = new List<string[]>
		{
			// Three different Xml samples with no publications specified
			new[] { XmlOpenTagsThruRoot, XmlCloseTagsFromRoot },
			new[] { XmlOpenTagsThruRoot, @"<Publications/>", XmlCloseTagsFromRoot },
			new[] { XmlOpenTagsThruRoot, @"<Publications></Publications>", XmlCloseTagsFromRoot }
		};

		[Test]
		public void Load_NoPublicationsLoadsNoPublications()
		{
			// "Main Dictionary" was added by base class
			ICmPossibility addedPublication = AddPublication("Another Dictionary");

			// Test three different possibilities of how no publications might present in the xml
			foreach (string[] noPublicationsXml in m_NoPublicationsList)
			{
				DictionaryConfigurationModel model;
				using (var modelFile = new TempFile(noPublicationsXml))
				{
					// SUT
					model = new DictionaryConfigurationModel(modelFile.Path, Cache);
				}

				Assert.IsEmpty(model.Publications, "Should have resulted in an empty set of publications for input XML: " + string.Join("",noPublicationsXml));
			}

			RemovePublication(addedPublication);
		}

		[Test]
		public void Load_AllPublicationsFlagCausesAllPublicationsReported()
		{
			// "Main Dictionary" was added by base class
			ICmPossibility addedPublication = AddPublication("Another Dictionary");

			DictionaryConfigurationModel model;
			using (var modelFile = new TempFile(new[]
			{
				XmlOpenTagsThruRootWithAllPublications,
				"<Publications><Publication>Another Dictionary</Publication></Publications>",
				XmlCloseTagsFromRoot
			}))
			{
				// SUT
				model = new DictionaryConfigurationModel(modelFile.Path, Cache);
			}

			Assert.That(model.AllPublications, Is.True, "Should have turned on AllPublications flag.");
			Assert.IsNotEmpty(model.Publications);
			Assert.AreEqual(2, model.Publications.Count);
			Assert.AreEqual("Main Dictionary", model.Publications[0], "Should have reported this dictionary since AllPublications is enabled.");
			Assert.AreEqual("Another Dictionary", model.Publications[1]);

			RemovePublication(addedPublication);
		}

		[Test]
		public void Load_LoadOnlyRealPublications()
		{
			// "Main Dictionary" was added by base class

			DictionaryConfigurationModel model;
			using (var modelFile = new TempFile(
				new[] {
					XmlOpenTagsThruRoot,
					@"<Publications><Publication>Main Dictionary</Publication><Publication>Not A Real Publication</Publication></Publications>",
					XmlCloseTagsFromRoot }))
			{
				// SUT
				model = new DictionaryConfigurationModel(modelFile.Path, Cache);
			}

			Assert.IsNotEmpty(model.Publications);
			Assert.AreEqual(1, model.Publications.Count);
			Assert.AreEqual("Main Dictionary", model.Publications[0]);
		}

		[Test]
		public void Load_NoRealPublicationLoadsNoPublications()
		{
			// "Main Dictionary" was added by base class

			DictionaryConfigurationModel model;
			using (var modelFile = new TempFile(
				new[] {
					XmlOpenTagsThruRoot,
					@"<Publications><Publication>Not A Real Publication</Publication></Publications>",
					XmlCloseTagsFromRoot }))
			{
				// SUT
				model = new DictionaryConfigurationModel(modelFile.Path, Cache);
			}

			Assert.IsEmpty(model.Publications);
		}

		/// <summary>
		/// To help with LT-17397, which allows adding unknown/new publications into the project when importing a configuration.
		/// </summary>
		[Test]
		public void PublicationsInXml_ReportsAll()
		{
			// "Main Dictionary" was added by base class

			DictionaryConfigurationModel model;
			using (var modelFile = new TempFile(
				new[] {
					XmlOpenTagsThruRoot,
					@"<Publications><Publication>Main Dictionary</Publication><Publication>New and unknown publication 1</Publication><Publication>New and unknown publication 2</Publication></Publications>",
					XmlCloseTagsFromRoot }))
			{
				// SUT
				var result = DictionaryConfigurationModel.PublicationsInXml(modelFile.Path).ToList();
				Assert.That(result.Count, Is.EqualTo(3), "Did not provide all publications in XML file");
				Assert.That(result[0], Is.EqualTo("Main Dictionary"), "Did not process and report publications as expected");
				Assert.That(result[1], Is.EqualTo("New and unknown publication 1"), "Did not process and report publications as expected");
				Assert.That(result[2], Is.EqualTo("New and unknown publication 2"), "Did not process and report publications as expected");
			}
		}

		[Test]
		public void ShippedFilesHaveNoRedundantChildrenOrOrphans([Values("Dictionary", "ReversalIndex")] string subFolder)
		{
			var shippedConfigfolder = Path.Combine(FwDirectoryFinder.FlexFolder, "DefaultConfigurations", subFolder);
			foreach(var shippedFile in Directory.EnumerateFiles(shippedConfigfolder, "*"+DictionaryConfigurationModel.FileExtension))
			{
				var model = new DictionaryConfigurationModel(shippedFile, Cache);
				VerifyNoRedundantChildren(model.Parts);
				if (model.SharedItems != null)
				{
					VerifyNoRedundantChildren(model.SharedItems);
					foreach(var si in model.SharedItems)
						Assert.NotNull(si.Parent, "Shared item {0} is an orphan", si.Label);
				}
			}
		}

		private static void VerifyNoRedundantChildren(List<ConfigurableDictionaryNode> nodes)
		{
			foreach (var node in nodes)
			{
				Assert.That(string.IsNullOrEmpty(node.ReferenceItem) || node.Children == null || !node.Children.Any());
				if(node.Children != null)
					VerifyNoRedundantChildren(node.Children);
			}
		}

		[Test]
		public void Save_BasicValidatesAgainstSchema()
		{
			using (var disposableModelFile = new TempFile())
			{
				var modelFile = disposableModelFile.Path;
				var model = new DictionaryConfigurationModel
				{
					FilePath = modelFile,
					Version = 0,
					Label = "root"
				};
				//SUT
				model.Save();
				ValidateAgainstSchema(modelFile);
				AssertThatXmlIn.File(modelFile).HasSpecifiedNumberOfMatchesForXpath("/DictionaryConfiguration/ConfigurationItem", 0);
			}
		}

		[Test]
		public void Save_HomographConfigurationValidatesAgainstSchema()
		{
			using (var disposableModelFile = new TempFile())
			{
				var modelFile = disposableModelFile.Path;
				var model = new DictionaryConfigurationModel
				{
					FilePath = modelFile,
					Version = 0,
					Label = "root",
					HomographConfiguration = new DictionaryHomographConfiguration
					{
						CustomHomographNumbers = "0;1;2;3;4;5;6;7;8;9",
						HomographNumberBefore = true,
						HomographWritingSystem = "en",
						ShowHwNumber = true,
						ShowHwNumInCrossRef = true,
						ShowHwNumInReversalCrossRef = true
					},
					Publications = new List<string> { "PublishThis" }
				};
				//SUT
				model.Save();
				ValidateAgainstSchema(modelFile);
				AssertThatXmlIn.File(modelFile).HasSpecifiedNumberOfMatchesForXpath("/DictionaryConfiguration/HomographConfiguration", 1);
			}
		}

		[Test]
		public void ShippedFilesValidateAgainstSchema([Values("Dictionary", "ReversalIndex")] string subFolder)
		{
			var shippedConfigfolder = Path.Combine(FwDirectoryFinder.FlexFolder, "DefaultConfigurations", subFolder);
			foreach(var shippedFile in Directory.EnumerateFiles(shippedConfigfolder, "*"+DictionaryConfigurationModel.FileExtension))
			{
				ValidateAgainstSchema(shippedFile);
			}
		}

		[Test]
		public void ShippedFilesHaveCurrentVersion([Values("Dictionary", "ReversalIndex")] string subFolder)
		{
			var shippedConfigfolder = Path.Combine(FwDirectoryFinder.FlexFolder, "DefaultConfigurations", subFolder);
			foreach(var shippedFile in Directory.EnumerateFiles(shippedConfigfolder, "*"+DictionaryConfigurationModel.FileExtension))
			{
				Assert.AreEqual(DictionaryConfigurationMigrator.VersionCurrent, new DictionaryConfigurationModel(shippedFile, Cache).Version);
			}
		}

		[Test]
		public void Save_ConfigWithOneNodeValidatesAgainstSchema()
		{
			using (var disposableModelFile = new TempFile())
			{
				var modelFile = disposableModelFile.Path;
				var oneConfigNode = new ConfigurableDictionaryNode
				{
					Label = "Main Entry",
					IsEnabled = true,
					Before = "[",
					FieldDescription = "LexEntry"
				};

				var model = new DictionaryConfigurationModel
				{
					FilePath = modelFile,
					Version = 0,
					Label = "root",
					Parts = new List<ConfigurableDictionaryNode> { oneConfigNode }
				};
				//SUT
				model.Save();
				ValidateAgainstSchema(modelFile);
				AssertThatXmlIn.File(modelFile).HasSpecifiedNumberOfMatchesForXpath("/DictionaryConfiguration/ConfigurationItem", 1);
			}
		}

		[Test]
		public void Save_ConfigWithTwoNodesValidatesAgainstSchema()
		{
			using (var disposableModelFile = new TempFile())
			{
				var modelFile = disposableModelFile.Path;
				var firstNode = new ConfigurableDictionaryNode
				{
					Label = "Main Entry",
					IsEnabled = true,
					Before = "[",
					FieldDescription = "LexEntry"
				};

				var secondNode = new ConfigurableDictionaryNode
				{
					Label = "Minor Entry",
					Before = "{",
					After = "}",
					FieldDescription = "LexEntry",
					IsEnabled = false
				};

				var model = new DictionaryConfigurationModel
				{
					FilePath = modelFile,
					Version = 0,
					Label = "root",
					Parts = new List<ConfigurableDictionaryNode> { firstNode, secondNode }
				};
				//SUT
				model.Save();
				ValidateAgainstSchema(modelFile);
				AssertThatXmlIn.File(modelFile).HasSpecifiedNumberOfMatchesForXpath("/DictionaryConfiguration/ConfigurationItem", 2);
			}
		}

		[Test]
		public void Save_ConfigNodeWithChildrenValidatesAgainstSchema()
		{
			using (var disposableModelFile = new TempFile())
			{
				var modelFile = disposableModelFile.Path;
				var headword = new ConfigurableDictionaryNode
				{
					Label = "Headword",
					FieldDescription = "LexEntry, headword",
					IsEnabled = true
				};
				var oneConfigNode = new ConfigurableDictionaryNode
				{
					Label = "Main Entry",
					IsEnabled = true,
					Before = "[",
					FieldDescription = "LexEntry",
					Children = new List<ConfigurableDictionaryNode> { headword }
				};

				var model = new DictionaryConfigurationModel
				{
					FilePath = modelFile,
					Version = 0,
					Label = "root",
					Parts = new List<ConfigurableDictionaryNode> { oneConfigNode }
				};
				//SUT
				model.Save();

				ValidateAgainstSchema(modelFile);
				AssertThatXmlIn.File(modelFile).HasSpecifiedNumberOfMatchesForXpath("/DictionaryConfiguration/ConfigurationItem/ConfigurationItem", 1);
			}
		}

		[Test]
		public void Save_ConfigWithReferenceItemValidatesAgainstSchema()
		{
			using  (var disposableModelFile = new TempFile())
			{
				var modelFile=disposableModelFile.Path;
				const string reference = "Reference";
				var oneConfigNode = new ConfigurableDictionaryNode
				{
					Label = "Main Entry",
					IsEnabled = true,
					Before = "[",
					FieldDescription = "LexEntry",
					ReferenceItem = reference
				};
				var oneRefConfigNode = new ConfigurableDictionaryNode
				{
					Label = reference,
					FieldDescription = "LexEntry",
				};
				var model = new DictionaryConfigurationModel
				{
					FilePath = modelFile,
					Version = 0,
					Label = "root",
					Parts = new List<ConfigurableDictionaryNode> { oneConfigNode },
					SharedItems = new List<ConfigurableDictionaryNode> { oneRefConfigNode }
				};

				//SUT
				model.Save();
				ValidateAgainstSchema(modelFile);
				AssertThatXmlIn.File(modelFile).HasSpecifiedNumberOfMatchesForXpath("/DictionaryConfiguration/ConfigurationItem", 1);
				AssertThatXmlIn.File(modelFile).HasSpecifiedNumberOfMatchesForXpath("/DictionaryConfiguration/ConfigurationItem/ReferenceItem", 1);
				AssertThatXmlIn.File(modelFile).HasSpecifiedNumberOfMatchesForXpath("/DictionaryConfiguration/ConfigurationItem/ConfigurationItem", 0);
				AssertThatXmlIn.File(modelFile).HasSpecifiedNumberOfMatchesForXpath("/DictionaryConfiguration/SharedItems/ConfigurationItem", 1);
			}
		}

		[Test]
		public void Save_ConfigWithWritingSystemOptionsValidatesAgainstSchema()
		{
			using (var disposableModelFile = new TempFile())
			{
				var modelFile = disposableModelFile.Path;
				var oneConfigNode = new ConfigurableDictionaryNode
				{
					Label = "Main Entry",
					IsEnabled = true,
					Before = "[",
					FieldDescription = "LexEntry",
					DictionaryNodeOptions = new DictionaryNodeWritingSystemOptions
					{
						Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>
						{
							new DictionaryNodeListOptions.DictionaryNodeOption
								{ Id = "en", IsEnabled = false }
						}
					}
				};

				var model = new DictionaryConfigurationModel
				{
					FilePath = modelFile,
					Version = 0,
					Label = "root",
					Parts = new List<ConfigurableDictionaryNode> { oneConfigNode }
				};
				//SUT
				model.Save();
				ValidateAgainstSchema(modelFile);
				AssertThatXmlIn.File(modelFile).HasSpecifiedNumberOfMatchesForXpath("/DictionaryConfiguration/ConfigurationItem", 1);
				AssertThatXmlIn.File(modelFile).HasSpecifiedNumberOfMatchesForXpath("/DictionaryConfiguration/ConfigurationItem/WritingSystemOptions", 1);
				AssertThatXmlIn.File(modelFile).HasSpecifiedNumberOfMatchesForXpath("/DictionaryConfiguration/ConfigurationItem/WritingSystemOptions/Option", 1);
			}
		}

		[Test]
		public void Save_ConfigWithPictureOptionsValidatesAgainstSchema()
		{
			using (var disposableModelFile = new TempFile())
			{
				var modelFile = disposableModelFile.Path;
				const float maxHeight = 1.5f;
				const float minHeight = 1;
				const float maxWidth = 2.5f;
				const float minWidth = 2;
				var oneConfigNode = new ConfigurableDictionaryNode
				{
					Label = "Main Entry",
					IsEnabled = true,
					Before = "[",
					FieldDescription = "LexEntry",
					DictionaryNodeOptions = new DictionaryNodePictureOptions
					{
						StackMultiplePictures = true,
						PictureLocation = AlignmentType.Left,
						MaximumHeight = maxHeight,
						MinimumHeight = minHeight,
						MaximumWidth = maxWidth,
						MinimumWidth = minWidth
					}
				};

				var model = new DictionaryConfigurationModel
				{
					FilePath = modelFile,
					Version = 0,
					Label = "root",
					Parts = new List<ConfigurableDictionaryNode> { oneConfigNode }
				};
				//SUT
				model.Save();
				ValidateAgainstSchema(modelFile);
				const string matchConfigRoot = "/DictionaryConfiguration/ConfigurationItem";
				AssertThatXmlIn.File(modelFile).HasSpecifiedNumberOfMatchesForXpath(matchConfigRoot, 1);
				const string matchPictureOptions = matchConfigRoot + "/PictureOptions";
				AssertThatXmlIn.File(modelFile).HasSpecifiedNumberOfMatchesForXpath(matchPictureOptions, 1);
				var matchAllOptions = matchPictureOptions +
					$"[@stackPictures='true' and @pictureLocation='left' and @maximumHeight='{maxHeight}' and @minimumHeight='{minHeight}' and @maximumWidth='{maxWidth}' and @minimumWidth='{minWidth}']";
				AssertThatXmlIn.File(modelFile).HasSpecifiedNumberOfMatchesForXpath(matchAllOptions, 1);
			}
		}

		[Test]
		public void Save_ConfigWithSenseOptionsValidatesAgainstSchema()
		{
			using (var disposableModelFile = new TempFile())
			{
				var modelFile = disposableModelFile.Path;
				var senseOptions = new DictionaryNodeSenseOptions
				{
					NumberStyle = "Some-Style",
					BeforeNumber = "(",
					AfterNumber = ")",
					NumberingStyle = "%O",
					DisplayEachSenseInAParagraph = true
				};
				var oneConfigNode = new ConfigurableDictionaryNode
				{
					Label = "Main Entry",
					IsEnabled = true,
					Before = "[",
					FieldDescription = "LexEntry",
					Style = "Some-Style",
					DictionaryNodeOptions = senseOptions
				};

				var model = new DictionaryConfigurationModel
				{
					FilePath = modelFile,
					Version = 0,
					Label = "root",
					Parts = new List<ConfigurableDictionaryNode> { oneConfigNode }
				};
				//SUT
				model.Save();
				ValidateAgainstSchema(modelFile);
				AssertThatXmlIn.File(modelFile).HasSpecifiedNumberOfMatchesForXpath("/DictionaryConfiguration/ConfigurationItem", 1);
				AssertThatXmlIn.File(modelFile).HasSpecifiedNumberOfMatchesForXpath("/DictionaryConfiguration/ConfigurationItem/SenseOptions", 1);
			}
		}

		[Test]
		public void Save_ConfigWithListOptionsValidatesAgainstSchema()
		{
			using (var disposableModelFile = new TempFile())
			{
				var modelFile = disposableModelFile.Path;
				var oneConfigNode = new ConfigurableDictionaryNode
				{
					Label = "Main Entry",
					IsEnabled = true,
					Before = "[",
					FieldDescription = "LexEntry",
					DictionaryNodeOptions = new DictionaryNodeListOptions
					{
						ListId = DictionaryNodeListOptions.ListIds.Entry,
						Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>
						{
							new DictionaryNodeListOptions.DictionaryNodeOption
								{ Id = "1f6ae209-141a-40db-983c-bee93af0ca3c", IsEnabled = false }
						}
					}
				};

				var model = new DictionaryConfigurationModel
				{
					FilePath = modelFile,
					Version = 0,
					Label = "root",
					Parts = new List<ConfigurableDictionaryNode> { oneConfigNode }
				};
				//SUT
				model.Save();
				ValidateAgainstSchema(modelFile);
				AssertThatXmlIn.File(modelFile).HasSpecifiedNumberOfMatchesForXpath("/DictionaryConfiguration/ConfigurationItem", 1);
				AssertThatXmlIn.File(modelFile).HasSpecifiedNumberOfMatchesForXpath("/DictionaryConfiguration/ConfigurationItem/ListTypeOptions", 1);
				AssertThatXmlIn.File(modelFile).HasSpecifiedNumberOfMatchesForXpath("/DictionaryConfiguration/ConfigurationItem/ListTypeOptions/Option", 1);
			}
		}

		[Test]
		public void Save_ConfigWithListAndParaOptionsValidatesAgainstSchema()
		{
			using (var disposableModelFile = new TempFile())
			{
				var modelFile = disposableModelFile.Path;
				var oneConfigNode = new ConfigurableDictionaryNode
				{
					Label = "Main Entry",
					IsEnabled = true,
					Before = "[",
					FieldDescription = "LexEntry",
					DictionaryNodeOptions = new DictionaryNodeListAndParaOptions
					{
						Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>
						{
							new DictionaryNodeListOptions.DictionaryNodeOption
								{ Id = "1f6ae209-141a-40db-983c-bee93af0ca3c", IsEnabled = false }
						}
					}
				};

				var model = new DictionaryConfigurationModel
				{
					FilePath = modelFile,
					Version = 0,
					Label = "root",
					Parts = new List<ConfigurableDictionaryNode> { oneConfigNode }
				};
				//SUT
				model.Save();
				ValidateAgainstSchema(modelFile);
				AssertThatXmlIn.File(modelFile).HasSpecifiedNumberOfMatchesForXpath("/DictionaryConfiguration/ConfigurationItem", 1);
				AssertThatXmlIn.File(modelFile).HasSpecifiedNumberOfMatchesForXpath("/DictionaryConfiguration/ConfigurationItem/ComplexFormOptions", 1);
				AssertThatXmlIn.File(modelFile).HasSpecifiedNumberOfMatchesForXpath("/DictionaryConfiguration/ConfigurationItem/ComplexFormOptions/Option", 1);
			}
		}

		[Test]
		public void Save_ConfigWithOnePublicationValidatesAgainstSchema()
		{
			using (var disposableModelFile = new TempFile())
			{
				var modelFile = disposableModelFile.Path;
				var model = new DictionaryConfigurationModel
				{
					FilePath = modelFile,
					Version = 0,
					Label = "root",
					Publications = new List<string> { "Main Dictionary" }
				};
				//SUT
				model.Save();
				ValidateAgainstSchema(modelFile);
				AssertThatXmlIn.File(modelFile).HasSpecifiedNumberOfMatchesForXpath("/DictionaryConfiguration/ConfigurationItem", 0);
				AssertThatXmlIn.File(modelFile).HasSpecifiedNumberOfMatchesForXpath("/DictionaryConfiguration/Publications", 1);
				AssertThatXmlIn.File(modelFile).HasSpecifiedNumberOfMatchesForXpath("/DictionaryConfiguration/Publications/Publication", 1);
			}
		}

		[Test]
		public void Save_ConfigWithTwoPublicationsValidatesAgainstSchema()
		{
			using (var disposableModelFile = new TempFile())
			{
				var modelFile = disposableModelFile.Path;
				var model = new DictionaryConfigurationModel
				{
					FilePath = modelFile,
					Version = 0,
					Label = "root",
					Publications = new List<string> { "Main Dictionary", "Subset Dictionary" }
				};
				//SUT
				model.Save();
				ValidateAgainstSchema(modelFile);
				AssertThatXmlIn.File(modelFile).HasSpecifiedNumberOfMatchesForXpath("/DictionaryConfiguration/ConfigurationItem", 0);
				AssertThatXmlIn.File(modelFile).HasSpecifiedNumberOfMatchesForXpath("/DictionaryConfiguration/Publications", 1);
				AssertThatXmlIn.File(modelFile).HasSpecifiedNumberOfMatchesForXpath("/DictionaryConfiguration/Publications/Publication", 2);
			}
		}

		[Test]
		public void Save_ConfigWithAllPublicationsValidatesAgainstSchema()
		{
			using  (var disposableModelFile = new TempFile())
			{
				var modelFile=disposableModelFile.Path;
				var model = new DictionaryConfigurationModel
				{
					FilePath = modelFile,
					Version = 0,
					Label = "root",
					Publications = new List<string> { "Main Dictionary" },
					AllPublications = true,
				};
				//SUT
				model.Save();
				ValidateAgainstSchema(modelFile);
				AssertThatXmlIn.File(modelFile).HasSpecifiedNumberOfMatchesForXpath("/DictionaryConfiguration/ConfigurationItem", 0);
				AssertThatXmlIn.File(modelFile).HasSpecifiedNumberOfMatchesForXpath("/DictionaryConfiguration/@allPublications", 1);
				AssertThatXmlIn.File(modelFile).HasSpecifiedNumberOfMatchesForXpath("/DictionaryConfiguration/Publications", 1);
				AssertThatXmlIn.File(modelFile).HasSpecifiedNumberOfMatchesForXpath("/DictionaryConfiguration/Publications/Publication", 1);
			}
		}

		[Test]
		public void Save_RealConfigValidatesAgainstSchema()
		{
			using (var disposableModelFile = new TempFile())
			{
				var modelFile = disposableModelFile.Path;
				var shippedConfigFolder = Path.Combine(FwDirectoryFinder.FlexFolder, "DefaultConfigurations", "Dictionary");
				var sampleShippedFile = Directory.EnumerateFiles(shippedConfigFolder, "*" + DictionaryConfigurationModel.FileExtension).First();
				var model = new DictionaryConfigurationModel(sampleShippedFile, Cache) { FilePath = modelFile };
				model.Parts[1].DuplicateAmongSiblings(model.Parts);
				// SUT
				model.Save();
				ValidateAgainstSchema(modelFile);
			}
		}

		[Test]
		public void Save_UsesISO86010DateTimeFormat()
		{
			using (var disposableModelFile = new TempFile())
			{
				var modelFile = disposableModelFile.Path;
				var model = new DictionaryConfigurationModel
				{
					FilePath = modelFile,
					Version = 0,
					Label = "root",
					Publications = new List<string> { "Main Dictionary" },
					AllPublications = true,
				};
				//SUT
				model.Save();
				ValidateAgainstSchema(modelFile);
				var xDoc = XDocument.Load(modelFile);
				var date = xDoc.Root?.Attribute("lastModified")?.Value;
				Assert.That(date, Does.Match(@"^\d{4}-\d{2}-\d{2}$"), xDoc.ToString());
			}
		}

		[Test]
		public void Save_PrettyPrints()
		{
			using (var disposableModelFile = new TempFile())
			{
				var modelFile = disposableModelFile.Path;
				var oneConfigNode = new ConfigurableDictionaryNode
				{
					Label = "Entry",
					FieldDescription = "LexEntry",
					DictionaryNodeOptions = new DictionaryNodeListAndParaOptions
					{
						Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>
						{
							new DictionaryNodeListOptions.DictionaryNodeOption { Id = "1f6ae209-141a-40db-983c-bee93af0ca3c" }
						}
					}
				};

				var model = new DictionaryConfigurationModel
				{
					FilePath = modelFile,
					Version = 0,
					Label = "root",
					Parts = new List<ConfigurableDictionaryNode> { oneConfigNode }
				};
				//SUT
				model.Save();
				ValidateAgainstSchema(modelFile);
				StringAssert.Contains("      ", File.ReadAllText(modelFile), "Currently expecting default intent style: two spaces");
				StringAssert.Contains(Environment.NewLine, File.ReadAllText(modelFile), "Configuration XML should not all be on one line");
			}
		}

		private static void ValidateAgainstSchema(string xmlFile)
		{
			var schemaLocation = Path.Combine(Path.Combine(FwDirectoryFinder.FlexFolder, "Configuration"), "DictionaryConfiguration.xsd");
			var schemas = new XmlSchemaSet();
			using(var reader = XmlReader.Create(schemaLocation))
			{
				schemas.Add("", reader);
				var document = XDocument.Load(xmlFile);
				document.Validate(schemas, (sender, args) =>
					Assert.Fail("Model saved at {0} did not validate against schema: {1}", xmlFile, args.Message));
			}
		}

		[Test]
		public void SpecifyParentsAndReferences_ThrowsOnNullArgument()
		{
			// SUT
			Assert.Throws<ArgumentNullException>(() => DictionaryConfigurationModel.SpecifyParentsAndReferences(null));
		}

		[Test]
		public void SpecifyParentsAndReferences_DoesNotChangeRootNode()
		{
			var child = new ConfigurableDictionaryNode();
			var rootNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> {child},
				Parent = null
			};
			var parts = new List<ConfigurableDictionaryNode> {rootNode};
			// SUT
			DictionaryConfigurationModel.SpecifyParentsAndReferences(parts);
			Assert.That(parts[0].Parent, Is.Null, "Shouldn't have changed parent of a root node");
		}

		[Test]
		public void SpecifyParentsAndReferences_UpdatesParentPropertyOfChild()
		{
			var rootNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode>()
			};
			var childA = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode>()
			};
			var childB = new ConfigurableDictionaryNode();
			var grandchild = new ConfigurableDictionaryNode();
			rootNode.Children.Add(childA);
			rootNode.Children.Add(childB);
			childA.Children.Add(grandchild);

			var parts = new List<ConfigurableDictionaryNode> { rootNode };
			// SUT
			DictionaryConfigurationModel.SpecifyParentsAndReferences(parts);
			Assert.That(grandchild.Parent, Is.EqualTo(childA), "Parent should have been set");
			Assert.That(childA.Parent, Is.EqualTo(rootNode), "Parent should have been set");
			Assert.That(childB.Parent, Is.EqualTo(rootNode), "Parent should have been set");
		}

		[Test]
		public void SpecifyParentsAndReferences_ThrowsIfReferenceItemDNE()
		{
			var configNode = new ConfigurableDictionaryNode { FieldDescription = "LexEntry", ReferenceItem = "DNE" };
			var model = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { configNode }, SharedItems = null };

			// SUT (DNE b/c no SharedItems)
			Assert.Throws<ArgumentNullException>(() => DictionaryConfigurationModel.SpecifyParentsAndReferences(model.Parts, sharedItems: model.SharedItems), "No SharedItems!");

			model.SharedItems = new List<ConfigurableDictionaryNode>();

			// SUT (DNE b/c SharedItems doesn't contain what was requested)
			Assert.Throws<KeyNotFoundException>(() => DictionaryConfigurationModel.SpecifyParentsAndReferences(model.Parts, sharedItems: model.SharedItems), "No matching item!");
		}

		[Test]
		public void SpecifyParentsAndReferences_ProhibitsReferencesOfIncompatibleTypes()
		{
			var configNode = new ConfigurableDictionaryNode { FieldDescription = m_field, ReferenceItem = m_reference };
			var refConfigNode = new ConfigurableDictionaryNode { FieldDescription = "SensesOS", Label = m_reference };
			var model = CreateSimpleSharingModel(configNode, refConfigNode);

			// SUT (Field is different)
			Assert.Throws<KeyNotFoundException>(() => DictionaryConfigurationModel.SpecifyParentsAndReferences(model.Parts, sharedItems: model.SharedItems));
			Assert.That(configNode.ReferencedNode, Is.Null, "ReferencedNode should not have been set");

			refConfigNode.FieldDescription = m_field;
			refConfigNode.SubField = "SensesOS";

			// SUT (SubField is different)
			Assert.Throws<KeyNotFoundException>(() => DictionaryConfigurationModel.SpecifyParentsAndReferences(model.Parts, sharedItems: model.SharedItems));
			Assert.That(configNode.ReferencedNode, Is.Null, "ReferencedNode should not have been set");
		}

		[Test]
		public void SpecifyParentsAndReferences_UpdatesReferencePropertyOfNodeWithReference()
		{
			var oneConfigNode = new ConfigurableDictionaryNode { FieldDescription = m_field, ReferenceItem = m_reference };
			var oneRefConfigNode = new ConfigurableDictionaryNode { FieldDescription = m_field, Label = m_reference };
			var model = CreateSimpleSharingModel(oneConfigNode, oneRefConfigNode);

			// SUT
			DictionaryConfigurationModel.SpecifyParentsAndReferences(model.Parts, sharedItems: model.SharedItems);

			Assert.AreSame(oneRefConfigNode, oneConfigNode.ReferencedNode);
		}

		[Test]
		public void SpecifyParentsAndReferences_RefsPreferFirstParentIfSameLevel()
		{
			var configNodeOne = new ConfigurableDictionaryNode { FieldDescription = m_field, ReferenceItem = m_reference };
			var configNodeTwo = new ConfigurableDictionaryNode { FieldDescription = m_field, ReferenceItem = m_reference };
			var refdConfigNode = new ConfigurableDictionaryNode { FieldDescription = m_field, Label = m_reference };
			var model = new DictionaryConfigurationModel
			{
				Parts = new List<ConfigurableDictionaryNode> { configNodeOne, configNodeTwo },
				SharedItems = new List<ConfigurableDictionaryNode> { refdConfigNode }
			};

			// SUT
			DictionaryConfigurationModel.SpecifyParentsAndReferences(model.Parts, sharedItems: model.SharedItems);

			Assert.AreSame(configNodeOne, refdConfigNode.Parent, "The Referenced node's 'Parent' should be the first to reference (breadth first)");
		}

		[Test]
		public void SpecifyParentsAndReferences_RefsPreferShallowestParentEvenIfNotFirst()
		{
			var configNodeKid = new ConfigurableDictionaryNode { FieldDescription = m_field, ReferenceItem = m_reference };
			var configNodeOne = new ConfigurableDictionaryNode { Children = new List<ConfigurableDictionaryNode> { configNodeKid } };
			var configNodeTwo = new ConfigurableDictionaryNode { FieldDescription = m_field, ReferenceItem = m_reference };
			var refdConfigNode = new ConfigurableDictionaryNode { FieldDescription = m_field, Label = m_reference };
			var model = new DictionaryConfigurationModel
			{
				Parts = new List<ConfigurableDictionaryNode> { configNodeOne, configNodeTwo },
				SharedItems = new List<ConfigurableDictionaryNode> { refdConfigNode }
			};

			// SUT
			DictionaryConfigurationModel.SpecifyParentsAndReferences(model.Parts, sharedItems: model.SharedItems);

			Assert.AreSame(configNodeTwo, refdConfigNode.Parent, "The Referenced node's 'Parent' should be the first to reference (breadth first)");
		}

		[Test]
		public void SpecifyParentsAndReferences_WorksForCircularReferences()
		{
			var configNode = new ConfigurableDictionaryNode { FieldDescription = m_field, ReferenceItem = m_reference };
			var refdConfigNodeChild = new ConfigurableDictionaryNode { FieldDescription = m_field, ReferenceItem = m_reference };
			var refdConfigNode = new ConfigurableDictionaryNode
			{
				FieldDescription = m_field, Label = m_reference,
				Children = new List<ConfigurableDictionaryNode> { refdConfigNodeChild }
			};
			var model = CreateSimpleSharingModel(configNode, refdConfigNode);

			// SUT
			DictionaryConfigurationModel.SpecifyParentsAndReferences(model.Parts, sharedItems: model.SharedItems);

			Assert.AreSame(refdConfigNode, refdConfigNodeChild.Parent);
			Assert.AreSame(refdConfigNode, refdConfigNodeChild.ReferencedNode);
		}

		[Test]
		public void LinkReferencedNode()
		{
			var configNode = new ConfigurableDictionaryNode { FieldDescription = m_field };
			var refConfigNode = new ConfigurableDictionaryNode { FieldDescription = m_field, Label = m_reference };
			var model = CreateSimpleSharingModel(configNode, refConfigNode);

			// SUT
			DictionaryConfigurationController.LinkReferencedNode(model.SharedItems, configNode, m_reference);
			Assert.AreEqual(refConfigNode.Label, configNode.ReferenceItem);
			Assert.AreSame(refConfigNode, configNode.ReferencedNode);
			Assert.That(refConfigNode.IsEnabled, "Referenced nodes are inaccessible to users, but must be enabled for their children to function");
		}

		[Test]
		public void CanDeepClone()
		{
			var parentNode = new ConfigurableDictionaryNode();
			var child = new ConfigurableDictionaryNode { After = "after", IsEnabled = true, Parent = parentNode };
			var grandchildNode = new ConfigurableDictionaryNode { Before = "childBefore", Parent = child };
			parentNode.Children = new List<ConfigurableDictionaryNode> { child };
			child.Children = new List<ConfigurableDictionaryNode> { grandchildNode };
			var model = new DictionaryConfigurationModel
			{
				FilePath = "C:/projects/<project>/configs/dictionary/*.xml", // existence is irrelevant for this test
				Label = "Root",
				Version = 4,
				Parts = new List<ConfigurableDictionaryNode> { parentNode },
				SharedItems = new List<ConfigurableDictionaryNode> { parentNode.DeepCloneUnderSameParent() },
				Publications = new List<string> { "unabridged", "college", "urban colloquialisms" },
				HomographConfiguration = new DictionaryHomographConfiguration { HomographNumberBefore = true, ShowHwNumber = false },
				Pictures = new PictureConfiguration { Alignment = AlignmentType.Center, Width = .5f }
			};

			// SUT
			var clone = model.DeepClone();

			Assert.AreEqual(model.FilePath, clone.FilePath);
			Assert.AreEqual(model.Label, clone.Label);
			Assert.AreEqual(model.Version, clone.Version);
			ConfigurableDictionaryNodeTests.VerifyDuplicationList(clone.Parts, model.Parts, null);
			ConfigurableDictionaryNodeTests.VerifyDuplicationList(clone.SharedItems, model.SharedItems, null);
			Assert.AreNotSame(model.Publications, clone.Publications);
			Assert.AreEqual(model.Publications.Count, clone.Publications.Count);
			for (int i = 0; i < model.Publications.Count; i++)
			{
				Assert.AreEqual(model.Publications[i], clone.Publications[i]);
			}
			Assert.That(model.HomographConfiguration, Is.Not.SameAs(clone.HomographConfiguration));
			// If we were on NUnit 4
			// Assert.That(model.HomographConfiguration, Is.EqualTo(clone.HomographConfiguration).UsingPropertiesComparer());
			// But we're not, so we have to do it manually or implement otherwise unnecessary equality interfaces
			Assert.That(model.HomographConfiguration.CustomHomographNumbers, Is.EqualTo(clone.HomographConfiguration.CustomHomographNumbers));
			Assert.That(model.HomographConfiguration.HomographNumberBefore, Is.EqualTo(clone.HomographConfiguration.HomographNumberBefore));
			Assert.That(model.HomographConfiguration.HomographWritingSystem, Is.EqualTo(clone.HomographConfiguration.HomographWritingSystem));
			Assert.That(model.HomographConfiguration.ShowHwNumber, Is.EqualTo(clone.HomographConfiguration.ShowHwNumber));
			Assert.That(model.HomographConfiguration.ShowHwNumInCrossRef, Is.EqualTo(clone.HomographConfiguration.ShowHwNumInCrossRef));
			Assert.That(model.HomographConfiguration.ShowHwNumInReversalCrossRef, Is.EqualTo(clone.HomographConfiguration.ShowHwNumInReversalCrossRef));
			// Same here
			Assert.That(model.Pictures, Is.Not.SameAs(clone.Pictures));
			Assert.That(model.Pictures.Alignment, Is.EqualTo(clone.Pictures.Alignment));
			Assert.That(model.Pictures.Width, Is.EqualTo(clone.Pictures.Width));
		}

		[Test]
		public void DeepClone_ConnectsSharedItemsWithinNewModel()
		{
			const string sharedSubsName = "SharedSubentries";
			var subentriesNode = new ConfigurableDictionaryNode { FieldDescription = "Subentries", ReferenceItem = sharedSubsName };
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { subentriesNode },
				FieldDescription = "LexEntry"
			};
			var sharedSubentriesNode = new ConfigurableDictionaryNode { Label = sharedSubsName, FieldDescription = "Subentries" };
			var model = CreateSimpleSharingModel(mainEntryNode, sharedSubentriesNode);
			CssGeneratorTests.PopulateFieldsForTesting(model);
			// SUT
			var clonedModel = model.DeepClone();
			var clonedMainEntry = clonedModel.Parts[0];
			var clonedSubentries = clonedMainEntry.Children[0];
			Assert.AreEqual(sharedSubsName, clonedSubentries.ReferenceItem, "ReferenceItem should have been cloned");
			Assert.AreSame(clonedModel.SharedItems[0], clonedSubentries.ReferencedNode, "ReferencedNode should have been cloned");
			Assert.AreSame(clonedSubentries, clonedModel.SharedItems[0].Parent, "SharedItems' Parents should connect to their new masters");
			Assert.AreNotSame(model.SharedItems[0], clonedModel.SharedItems[0], "SharedItems were not deep cloned");
		}

		internal static DictionaryConfigurationModel CreateSimpleSharingModel(ConfigurableDictionaryNode part, ConfigurableDictionaryNode sharedItem)
		{
			return new DictionaryConfigurationModel
			{
				Parts = new List<ConfigurableDictionaryNode> { part },
				SharedItems = new List<ConfigurableDictionaryNode> { sharedItem }
			};
		}
	}
}
