// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using NUnit.Framework;
using Palaso.IO;
using Palaso.TestUtilities;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FDO.Infrastructure;

namespace SIL.FieldWorks.XWorks
{
	[TestFixture]
	class DictionaryConfigurationModelTests : MemoryOnlyBackendProviderTestBase
	{
		private const string XmlOpenTagsThruRoot = @"<?xml version=""1.0"" encoding=""utf-8""?>
			<DictionaryConfiguration name=""Root"" version=""1"" lastModified=""2014-02-13"">";
		private const string XmlOpenTagsThruHeadword =
				XmlOpenTagsThruRoot +
				@"<ConfigurationItem name=""Main Entry"" isEnabled=""true"" field=""LexEntry"">
					<ConfigurationItem name=""Testword"" nameSuffix=""2b""
							before=""["" between="", "" after=""] "" style=""Dictionary-Headword"" isEnabled=""true"" field=""HeadWord"">";

		private const string XmlCloseTagsFromHeadword = @"
					</ConfigurationItem>
				</ConfigurationItem>
				<SharedItems/>" +
			XmlCloseTagsFromRoot;
		private const string XmlCloseTagsFromRoot = @"</DictionaryConfiguration>";

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
			Assert.IsNullOrEmpty(rootConfigNode.LabelSuffix);
			Assert.IsNullOrEmpty(rootConfigNode.SubField);
			Assert.IsNullOrEmpty(rootConfigNode.Before);
			Assert.IsNullOrEmpty(rootConfigNode.Between);
			Assert.IsNullOrEmpty(rootConfigNode.After);
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
				<WritingSystemOptions writingSystemType=""analysis"" displayWSAbreviation=""true"">
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
			Assert.AreEqual(DictionaryNodeWritingSystemOptions.WritingSystemType.Analysis, wsOptions.WsType);
			Assert.AreEqual(1, wsOptions.Options.Count);
			Assert.AreEqual("fr", wsOptions.Options[0].Id);
			Assert.IsTrue(wsOptions.Options[0].IsEnabled);
		}

		[Test]
		public void Load_LoadsSenseOptions()
		{
			DictionaryConfigurationModel model;
			using (var modelFile = new TempFile(new[]
			{
				XmlOpenTagsThruHeadword, @"
				<SenseOptions displayEachSenseInParagraph=""true"" numberStyle=""bold"" numberBefore=""("" numberAfter="") ""
						numberingStyle=""%O"" numberFont="""" numberSingleSense=""true"" showSingleGramInfoFirst=""true""/>",
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
			Assert.AreEqual("%O", senseOptions.NumberingStyle);
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
			Assert.AreEqual(7, listOptions.Options.Count);
			Assert.AreEqual(7, listOptions.Options.Count(option => option.IsEnabled));
			Assert.AreEqual("b0000000-c40e-433e-80b5-31da08771344", listOptions.Options[0].Id);
		}

		[Test]
		public void Load_LoadsComplexFormOptions()
		{
			DictionaryConfigurationModel model;
			using (var modelFile = new TempFile(new[]
			{
				XmlOpenTagsThruHeadword, @"
				<ComplexFormOptions list=""complex"" displayEachComplexFormInParagraph=""true"">
					<Option isEnabled=""true""  id=""a0000000-dd15-4a03-9032-b40faaa9a754""/>
					<Option isEnabled=""true""  id=""1f6ae209-141a-40db-983c-bee93af0ca3c""/>
					<Option isEnabled=""true""  id=""73266a3a-48e8-4bd7-8c84-91c730340b7d""/>
				</ComplexFormOptions>",
				XmlCloseTagsFromHeadword
			}))
			{
				// SUT
				model = new DictionaryConfigurationModel(modelFile.Path, Cache);
			}

			// The following assertions are based on the specific test data loaded from the file
			var testNodeOptions = model.Parts[0].Children[0].DictionaryNodeOptions;
			Assert.IsInstanceOf(typeof(DictionaryNodeComplexFormOptions), testNodeOptions);
			var cfOptions = (DictionaryNodeComplexFormOptions)testNodeOptions;
			Assert.AreEqual(DictionaryNodeListOptions.ListIds.Complex, cfOptions.ListId);
			Assert.IsTrue(cfOptions.DisplayEachComplexFormInAParagraph);
			Assert.AreEqual(3, cfOptions.Options.Count);
			Assert.AreEqual(3, cfOptions.Options.Count(option => option.IsEnabled));
			Assert.AreEqual("a0000000-dd15-4a03-9032-b40faaa9a754", cfOptions.Options[0].Id);
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
			Assert.IsInstanceOf(typeof(DictionaryNodeComplexFormOptions), testNodeOptions);
			var cfOptions = (DictionaryNodeComplexFormOptions)testNodeOptions;
			Assert.AreEqual(DictionaryNodeListOptions.ListIds.None, cfOptions.ListId);
			Assert.That(cfOptions.Options, Is.Null.Or.Empty);
			Assert.IsFalse(cfOptions.DisplayEachComplexFormInAParagraph);
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
				result.Name.AnalysisDefaultWritingSystem = Cache.TsStrFactory.MakeString(publicationName,
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
		public void Load_NoPublicationsLoadsAllPublications()
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

				Assert.IsNotEmpty(model.Publications);
				Assert.AreEqual(2, model.Publications.Count);
				Assert.AreEqual("Main Dictionary", model.Publications[0]);
				Assert.AreEqual("Another Dictionary", model.Publications[1]);
			}

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
		public void Load_NoRealPublicationLoadsAllPublications()
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

			Assert.IsNotEmpty(model.Publications);
			Assert.AreEqual(1, model.Publications.Count);
			Assert.AreEqual("Main Dictionary", model.Publications[0]);
		}

		[Test]
		public void Save_BasicValidatesAgainstSchema()
		{
			var modelFile = Path.GetTempFileName();
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

		[Test]
		public void Save_ConfigWithOneNodeValidatesAgainstSchema()
		{
			var modelFile = Path.GetTempFileName();
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

		[Test]
		public void Save_ConfigWithTwoNodesValidatesAgainstSchema()
		{
			var modelFile = Path.GetTempFileName();
			var firstNode = new ConfigurableDictionaryNode
				{
					Label = "Main Entry",
					IsEnabled = true,
					Before = "[",
					FieldDescription = "LexEntry"
				};

			var secondNode = new ConfigurableDictionaryNode()
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

		[Test]
		public void Save_ConfigNodeWithChildrenValidatesAgainstSchema()
		{
			var modelFile = Path.GetTempFileName();
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

		[Test]
		public void Save_ConfigWithReferenceItemValidatesAgainstSchema()
		{
			var modelFile = Path.GetTempFileName();
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

		[Test]
		public void Save_ConfigWithWritingSystemOptionsValidatesAgainstSchema()
		{
			var modelFile = Path.GetTempFileName();
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
						new DictionaryNodeListOptions.DictionaryNodeOption { Id = "en", IsEnabled = false }
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

		[Test]
		public void Save_ConfigWithSenseOptionsValidatesAgainstSchema()
		{
			var modelFile = Path.GetTempFileName();
			var oneConfigNode = new ConfigurableDictionaryNode
			{
				Label = "Main Entry",
				IsEnabled = true,
				Before = "[",
				FieldDescription = "LexEntry",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions()
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

		[Test]
		public void Save_ConfigWithListOptionsValidatesAgainstSchema()
		{
			var modelFile = Path.GetTempFileName();
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
						new DictionaryNodeListOptions.DictionaryNodeOption { Id = "1f6ae209-141a-40db-983c-bee93af0ca3c", IsEnabled = false }
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

		[Test]
		public void Save_ConfigWithComplexFormOptionsValidatesAgainstSchema()
		{
			var modelFile = Path.GetTempFileName();
			var oneConfigNode = new ConfigurableDictionaryNode
			{
				Label = "Main Entry",
				IsEnabled = true,
				Before = "[",
				FieldDescription = "LexEntry",
				DictionaryNodeOptions = new DictionaryNodeComplexFormOptions
				{
					Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>
					{
						new DictionaryNodeListOptions.DictionaryNodeOption { Id = "1f6ae209-141a-40db-983c-bee93af0ca3c", IsEnabled = false }
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

		[Test]
		public void Save_ConfigWithOnePublicationValidatesAgainstSchema()
		{
			var modelFile = Path.GetTempFileName();
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

		[Test]
		public void Save_ConfigWithTwoPublicationsValidatesAgainstSchema()
		{
			var modelFile = Path.GetTempFileName();
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

		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
							  Justification = "Certain types can't be validated. e.g. xs:byte, otherwise implemented enough for us")]
		private static void ValidateAgainstSchema(string xmlFile)
		{
			var schemaLocation = Path.Combine(Path.Combine(DirectoryFinder.FlexFolder, "Configuration"), "DictionaryConfiguration.xsd");
			var schemas = new XmlSchemaSet();
			using(var reader = XmlReader.Create(schemaLocation))
			{
				schemas.Add("", reader);
				var document = XDocument.Load(xmlFile);
				document.Validate(schemas, (sender, args) => Assert.Fail("Model saved as xml did not validate against schema: {0}", args.Message));
			}
		}

		[Test]
		public void SpecifyParents_ThrowsOnNullArgument()
		{
			// SUT
			Assert.Throws<ArgumentNullException>(() => DictionaryConfigurationModel.SpecifyParents(null));
		}

		[Test]
		public void SpecifyParents_DoesNotChangeRootNode()
		{
			var child = new ConfigurableDictionaryNode();
			var rootNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode>() {child},
				Parent = null
			};
			var parts = new List<ConfigurableDictionaryNode> {rootNode};
			// SUT
			DictionaryConfigurationModel.SpecifyParents(parts);
			Assert.That(parts[0].Parent, Is.Null, "Shouldn't have changed parent of a root node");
		}

		[Test]
		public void SpecifyParents_UpdatesParentPropertyOfChild()
		{
			var rootNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode>()
			};
			var childA = new ConfigurableDictionaryNode()
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
			DictionaryConfigurationModel.SpecifyParents(parts);
			Assert.That(grandchild.Parent, Is.EqualTo(childA), "Parent should have been set");
			Assert.That(childA.Parent, Is.EqualTo(rootNode), "Parent should have been set");
			Assert.That(childB.Parent, Is.EqualTo(rootNode), "Parent should have been set");
		}
	}
}
