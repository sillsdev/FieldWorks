// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using NUnit.Framework;
using Palaso.TestUtilities;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;

namespace SIL.FieldWorks.XWorks
{
	[TestFixture]
	class ConfiguredXHTMLGeneratorTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		StringBuilder XHTMLStringBuilder { get; set; }

		[SetUp]
		public void SetupExportVariables()
		{
			XHTMLStringBuilder = new StringBuilder();
		}

		[Test]
		public void GenerateXHTMLForEntry_NullArgsThrowArgumentNull()
		{
			using(var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var mainEntryNode = new ConfigurableDictionaryNode();
				var factory = Cache.ServiceLocator.GetInstance<ILexEntryFactory>();
				var entry = factory.Create();
				//SUT
				Assert.Throws(typeof(ArgumentNullException), () => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(null, mainEntryNode, XHTMLWriter, Cache));
				Assert.Throws(typeof(ArgumentNullException), () => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entry, null, XHTMLWriter, Cache));
				Assert.Throws(typeof(ArgumentNullException), () => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entry, mainEntryNode, null, Cache));
				Assert.Throws(typeof(ArgumentNullException), () => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entry, mainEntryNode, XHTMLWriter, null));
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_BadConfigurationThrows()
		{
			var mainEntryNode = new ConfigurableDictionaryNode();
			var factory = Cache.ServiceLocator.GetInstance<ILexEntryFactory>();
			var entry = factory.Create();
			using(var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				//Test a blank main node description
				//SUT
				Assert.That(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entry, mainEntryNode, XHTMLWriter, Cache),
					Throws.InstanceOf<ArgumentException>().With.Message.Contains("Invalid configuration"));
				mainEntryNode.FieldDescription = "LexSense";
				//Test a configuration with a valid but incorrect type
				Assert.That(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entry, mainEntryNode, XHTMLWriter, Cache),
					Throws.InstanceOf<ArgumentException>().With.Message.Contains("doesn't configure this type"));
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_HeadwordConfigurationGeneratesCorrectResult()
		{
			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "HeadWord",
				Label = "Headword",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" }),
				IsEnabled = true
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { headwordNode },
				FieldDescription = "LexEntry",
				IsEnabled = true
			};
			var wsFr = Cache.WritingSystemFactory.GetWsFromStr("fr");
			var entry = CreateInterestingLexEntry();
			// The headword field is special it uses either Citation or LexemeForm if Citation isn't filled in
			entry.CitationForm.set_String(wsFr, Cache.TsStrFactory.MakeString("HeadWordTest", wsFr));
			using(var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				//SUT
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entry, mainEntryNode, XHTMLWriter, Cache));
				XHTMLWriter.Flush();
				var frenchHeadwordOfHeadwordTest = "/div[@class='entry']/span[@class='headword' and @lang='fr' and text()='HeadWordTest']";
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(frenchHeadwordOfHeadwordTest, 1);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_NoEnabledConfigurationsThrowsArgument()
		{
			var homographNum = new ConfigurableDictionaryNode
			{
				FieldDescription = "HomographNumber",
				Label = "Homograph Number",
				IsEnabled = true
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { homographNum },
				FieldDescription = "LexEntry"
			};
			var entryOne = CreateInterestingLexEntry();

			using(var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				//SUT
				Assert.That(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entryOne, mainEntryNode, XHTMLWriter,Cache),
								Throws.InstanceOf<ArgumentException>().With.Message.Contains("must use an enabled configuration node"));
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_HomographNumbersGeneratesCorrectResult()
		{
			var homographNum = new ConfigurableDictionaryNode
			{
				FieldDescription = "HomographNumber",
				Label = "Homograph Number",
				IsEnabled = true
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { homographNum },
				FieldDescription = "LexEntry",
				IsEnabled = true
			};
			var entryOne = CreateInterestingLexEntry();
			var entryTwo = CreateInterestingLexEntry();

			using(var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				XHTMLWriter.WriteStartElement("TESTWRAPPER"); //keep the xml valid (single root element)
				//SUT
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entryOne, mainEntryNode, XHTMLWriter,
																								Cache));
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entryTwo, mainEntryNode, XHTMLWriter,
																								Cache));
				XHTMLWriter.WriteEndElement();
				XHTMLWriter.Flush();
				var entryWithHomograph = "/TESTWRAPPER/div[@class='entry']/span[@class='homographnumber' and text()='1']";
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(entryWithHomograph, 1);
				entryWithHomograph = entryWithHomograph.Replace('1', '2');
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(entryWithHomograph, 1);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_OneSenseWithGlossGeneratesCorrectResult()
		{
			var wsOpts = new DictionaryNodeWritingSystemOptions
			{
				Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>
				{
					new DictionaryNodeListOptions.DictionaryNodeOption { Id = "en", IsEnabled = true }
				}
			};

			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Senses",
				Label = "Senses",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode>
				{
					new ConfigurableDictionaryNode { FieldDescription = "Gloss", DictionaryNodeOptions = wsOpts, IsEnabled = true }
				}
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { sensesNode },
				FieldDescription = "LexEntry",
				IsEnabled = true
			};
			var testEntry = CreateInterestingLexEntry();

			using(var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				//SUT
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, XHTMLWriter, Cache));
				XHTMLWriter.Flush();
				const string oneSenseWithGlossOfGloss = "/div[@class='entry']/span[@class='senses']/span[@class='sense']/span[@lang='en' and text()='gloss']";
				//This assert is dependent on the specific entry data created in CreateInterestingLexEntry
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(oneSenseWithGlossOfGloss, 1);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_OneEntryWithSenseAndOneWithoutWorks()
		{
			var wsOpts = new DictionaryNodeWritingSystemOptions
			{
				Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>
				{ new DictionaryNodeListOptions.DictionaryNodeOption { Id = "en", IsEnabled = true} }
			};

			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Senses",
				Label = "Senses",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { new ConfigurableDictionaryNode { FieldDescription = "Gloss",
																															  DictionaryNodeOptions = wsOpts} }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { sensesNode },
				FieldDescription = "LexEntry",
				IsEnabled = true
			};
			var entryOne = CreateInterestingLexEntry();
			var wsFr = Cache.WritingSystemFactory.GetWsFromStr("fr");
			entryOne.CitationForm.set_String(wsFr, Cache.TsStrFactory.MakeString("FirstHeadword", wsFr));
			var entryTwo = CreateInterestingLexEntry();
			entryTwo.CitationForm.set_String(wsFr, Cache.TsStrFactory.MakeString("SecondHeadword", wsFr));
			entryTwo.SensesOS.Clear();
			var entryOneId = entryOne.Hvo;
			var entryTwoId = entryTwo.Hvo;

			using(var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				XHTMLWriter.WriteStartElement("TESTWRAPPER"); //keep the xml valid (single root element)
				//SUT
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entryOne, mainEntryNode, XHTMLWriter,
																							  Cache));
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entryTwo, mainEntryNode, XHTMLWriter,
																							  Cache));
				XHTMLWriter.WriteEndElement();
				XHTMLWriter.Flush();
				var entryOneHasSensesSpan = "/TESTWRAPPER/div[@class='entry' and @id='hvo" + entryOneId + "']/span[@class='senses']";
				var entryTwoExists = "/TESTWRAPPER/div[@class='entry' and @id='hvo" + entryTwoId + "']";
				var entryTwoHasNoSensesSpan = "/TESTWRAPPER/div[@class='entry' and @id='hvo" + entryTwoId + "']/span[@class='senses']";
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(entryOneHasSensesSpan, 1);
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(entryTwoExists, 1);
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(entryTwoHasNoSensesSpan, 0);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_DefaultRootGeneratesResult()
		{
			string defaultRoot =
				Path.Combine(Path.Combine(DirectoryFinder.DefaultConfigurations, "Dictionary"), "Root.xml");
			var entry = CreateInterestingLexEntry();
			var dictionaryModel = new DictionaryConfigurationModel(defaultRoot);
			dictionaryModel.Load();
			using(var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				//SUT
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entry, dictionaryModel.Parts[0], XHTMLWriter, Cache));
				XHTMLWriter.Flush();
				var entryExists = "/div[@class='entry' and @id='hvo" + entry.Hvo + "']";
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(entryExists, 1);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_DoesNotDescendThroughDisabledNode()
		{
			var wsOpts = new DictionaryNodeWritingSystemOptions
			{
				Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>
				{
					new DictionaryNodeListOptions.DictionaryNodeOption { Id = "en", IsEnabled = true }
				}
			};
			var senses = new ConfigurableDictionaryNode
			{
				FieldDescription = "Senses",
				Label = "Senses",
				IsEnabled = false,
				Children = new List<ConfigurableDictionaryNode>
				{
					new ConfigurableDictionaryNode { FieldDescription = "Gloss", DictionaryNodeOptions = wsOpts, IsEnabled = true }
				}
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { senses },
				FieldDescription = "LexEntry",
				IsEnabled = true
			};
			var entryOne = CreateInterestingLexEntry();

			using(var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				//SUT
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entryOne, mainEntryNode, XHTMLWriter, Cache));
				XHTMLWriter.Flush();
				const string sensesThatShouldNotBe = "/div[@class='entry']/span[@class='senses']";
				const string headwordThatShouldNotBe = "//span[@class='gloss']";
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(sensesThatShouldNotBe, 0);
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(headwordThatShouldNotBe, 0);
			}
		}

		private ILexEntry CreateInterestingLexEntry()
		{
			var factory = Cache.ServiceLocator.GetInstance<ILexEntryFactory>();
			var senseFactory = Cache.ServiceLocator.GetInstance<ILexSenseFactory>();
			var entry = factory.Create();
			Cache.LangProject.AddToCurrentAnalysisWritingSystems(
				Cache.WritingSystemFactory.get_Engine("en") as IWritingSystem);
			Cache.LangProject.AddToCurrentVernacularWritingSystems(
				Cache.WritingSystemFactory.get_Engine("fr") as IWritingSystem);
			var wsEn = Cache.WritingSystemFactory.GetWsFromStr("en");
			var wsFr = Cache.WritingSystemFactory.GetWsFromStr("fr");
			entry.CitationForm.set_String(wsFr, Cache.TsStrFactory.MakeString("Citation", wsFr));
			entry.Comment.set_String(wsEn, Cache.TsStrFactory.MakeString("Comment", wsEn));
			var sense = senseFactory.Create();
			entry.SensesOS.Add(sense);
			sense.Gloss.set_String(wsEn, Cache.TsStrFactory.MakeString("gloss", wsEn));
			return entry;
		}

		private DictionaryNodeOptions GetWsOptionsForLanguages(string[] languages)
		{
			var wsOptions = new DictionaryNodeWritingSystemOptions();
			wsOptions.Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>();
			foreach(var lang in languages)
			{
				wsOptions.Options.Add(new DictionaryNodeListOptions.DictionaryNodeOption { Id = lang, IsEnabled = true });
			}
			return wsOptions;
		}
	}
}
