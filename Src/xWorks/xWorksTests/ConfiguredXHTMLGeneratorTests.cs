// Copyright (c) 2014-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using NUnit.Framework;
using Palaso.TestUtilities;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.FDO.DomainImpl;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	// ReSharper disable InconsistentNaming
	[TestFixture]
	class ConfiguredXHTMLGeneratorTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase, IDisposable
	{
		private int m_wsEn, m_wsFr;

		private FwXApp m_application;
		private FwXWindow m_window;
		private Mediator m_mediator;

		StringBuilder XHTMLStringBuilder { get; set; }

		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			FwRegistrySettings.Init();
			m_application = new MockFwXApp(new MockFwManager { Cache = Cache }, null, null);
			var m_configFilePath = Path.Combine(FwDirectoryFinder.CodeDirectory, m_application.DefaultConfigurationPathname);
			m_window = new MockFwXWindow(m_application, m_configFilePath);
			((MockFwXWindow)m_window).Init(Cache); // initializes Mediator values
			m_mediator = m_window.Mediator;

			m_mediator.PropertyTable.SetProperty("currentContentControl", "lexiconDictionary");
			Cache.ProjectId.Path = Path.Combine(FwDirectoryFinder.SourceDirectory, "xWorks/xWorksTests/TestData/");
			m_wsEn = Cache.WritingSystemFactory.GetWsFromStr("en");
			m_wsFr = Cache.WritingSystemFactory.GetWsFromStr("fr");
		}

		[TestFixtureTearDown]
		public override void FixtureTeardown()
		{
			base.FixtureTeardown();
			Dispose();
		}

		#region disposal
		protected virtual void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (disposing)
			{
				if (m_application != null)
					m_application.Dispose();
				if (m_window != null)
					m_window.Dispose();
				if (m_mediator != null)
					m_mediator.Dispose();
			}
		}

		~ConfiguredXHTMLGeneratorTests()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
		}
		#endregion disposal

		[SetUp]
		public void SetupExportVariables()
		{
			XHTMLStringBuilder = new StringBuilder();
		}

		[TearDown]
		public void ResetModelAssembly()
		{
			// Specific tests override this, reset to Fdo.dll needed by most tests in the file
			ConfiguredXHTMLGenerator.AssemblyFile = "FDO";
		}

		const string xpathThruSense = "/div[@class='lexentry']/span[@class='senses']/span[@class='sense']";
		private const string TestVariantName = "Crazy Variant";

		[Test]
		public void GeneratorSettings_NullArgsThrowArgumentNull()
		{
			using(var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				// ReSharper disable AccessToDisposedClosure // Justification: Assert calls lambdas immediately, so XHTMLWriter is not used after being disposed
				// ReSharper disable ObjectCreationAsStatement // Justification: We expect the constructor to throw, so there's no created object to assign anywhere :)
				Assert.Throws(typeof(ArgumentNullException), () => new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, null, false, false, null));
				Assert.Throws(typeof(ArgumentNullException), () => new ConfiguredXHTMLGenerator.GeneratorSettings(null, m_mediator, XHTMLWriter, false, false, null));
				// ReSharper restore ObjectCreationAsStatement
				// ReSharper restore AccessToDisposedClosure
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_NullArgsThrowArgumentNull()
		{
			using(var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var mainEntryNode = new ConfigurableDictionaryNode();
				var factory = Cache.ServiceLocator.GetInstance<ILexEntryFactory>();
				var entry = factory.Create();
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				//SUT
				Assert.Throws(typeof(ArgumentNullException), () => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(null, mainEntryNode, null, settings));
				Assert.Throws(typeof(ArgumentNullException), () => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entry, (ConfigurableDictionaryNode)null, null, settings));
				Assert.Throws(typeof(ArgumentNullException), () => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entry, mainEntryNode, null, null));
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_BadConfigurationThrows()
		{
			var mainEntryNode = new ConfigurableDictionaryNode();
			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			using(var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				// ReSharper disable AccessToDisposedClosure
				// Justification: Assert calls lambdas immediately, so XHTMLWriter is not used after being disposed
				//Test a blank main node description
				//SUT
				Assert.That(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entry, mainEntryNode, null, settings),
					Throws.InstanceOf<ArgumentException>().With.Message.Contains("Invalid configuration"));
				mainEntryNode.FieldDescription = "LexSense";
				//Test a configuration with a valid but incorrect type
				Assert.That(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entry, mainEntryNode, null, settings),
					Throws.InstanceOf<ArgumentException>().With.Message.Contains("doesn't configure this type"));
// ReSharper restore AccessToDisposedClosure
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_HeadwordConfigurationGeneratesCorrectResult()
		{
			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLHeadWord",
				Label = "Headword",
				CSSClassNameOverride = "headword",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" }),
				IsEnabled = true
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { headwordNode },
				FieldDescription = "LexEntry",
				IsEnabled = true
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });
			var entry = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(entry, "HeadWordTest", m_wsFr, Cache);
			using(var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				//SUT
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entry, mainEntryNode, null, settings));
				XHTMLWriter.Flush();
				const string frenchHeadwordOfHeadwordTest = "/div[@class='lexentry']/span[@class='headword']/a/span[@lang='fr' and text()='HeadWordTest']";
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(frenchHeadwordOfHeadwordTest, 1);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_LexemeFormConfigurationGeneratesCorrectResult()
		{
			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexemeFormOA",
				Label = "Lexeme Form",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "vernacular" }),
				IsEnabled = true
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { headwordNode },
				FieldDescription = "LexEntry",
				IsEnabled = true
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> {mainEntryNode});
			var wsFr = Cache.WritingSystemFactory.GetWsFromStr("fr");
			var entry = CreateInterestingLexEntry(Cache);
			//Fill in the LexemeForm
			var morph = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			entry.LexemeFormOA = morph;
			morph.Form.set_String(wsFr, Cache.TsStrFactory.MakeString("LexemeFormTest", wsFr));
			using(var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				//SUT
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entry, mainEntryNode, null, settings));
				XHTMLWriter.Flush();
				const string frenchLexForm = "/div[@class='lexentry']/span[@class='lexemeformoa']/a/span[@lang='fr' and text()='LexemeFormTest']";
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(frenchLexForm, 1);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_PronunciationLocationGeneratesCorrectResult()
		{
			var nameNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Name",
				Label = "Name",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" }),
				IsEnabled = true
			};
			var locationNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LocationRA",
				CSSClassNameOverride = "Location",
				Label = "Spoken here",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { nameNode }
			};
			var pronunciationsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "PronunciationsOS",
				CSSClassNameOverride = "Pronunciations",
				Label = "Speak this",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { locationNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { pronunciationsNode },
				FieldDescription = "LexEntry",
				IsEnabled = true
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> {mainEntryNode});
			var wsFr = Cache.WritingSystemFactory.GetWsFromStr("fr");
			var entry = CreateInterestingLexEntry(Cache);
			//Create and fill in the Location
			var pronunciation = Cache.ServiceLocator.GetInstance<ILexPronunciationFactory>().Create();
			entry.PronunciationsOS.Add(pronunciation);
			var possListFactory = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>();
			var possList1 = possListFactory.Create();
			Cache.LangProject.LocationsOA = possList1;
			var location = Cache.ServiceLocator.GetInstance<ICmLocationFactory>().Create();
			possList1.PossibilitiesOS.Add(location);
			location.Name.set_String(wsFr, Cache.TsStrFactory.MakeString("Here!", wsFr));
			pronunciation.LocationRA = location;
			using(var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				//SUT
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entry, mainEntryNode, null, settings));
				XHTMLWriter.Flush();
				const string hereLocation = "/div[@class='lexentry']/span[@class='pronunciations']/span[@class='pronunciation']/span[@class='location']/span[@class='name']/span[@lang='fr' and text()='Here!']";
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(hereLocation, 1);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_NoEnabledConfigurationsWritesNothing()
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
				IsEnabled = false
			};
			var entryOne = CreateInterestingLexEntry(Cache);

			using(var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				//SUT
				ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entryOne, mainEntryNode, null, settings);
				Assert.IsEmpty(XHTMLStringBuilder.ToString(), "Should not have generated anything for a disabled node");
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
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });
			var entryOne = CreateInterestingLexEntry(Cache);
			var entryTwo = CreateInterestingLexEntry(Cache);

			using(var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				XHTMLWriter.WriteStartElement("TESTWRAPPER"); //keep the xml valid (single root element)
				//SUT
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entryOne, mainEntryNode, null, settings));
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entryTwo, mainEntryNode, null, settings));
				XHTMLWriter.WriteEndElement();
				XHTMLWriter.Flush();
				var entryWithHomograph = "/TESTWRAPPER/div[@class='lexentry']/span[@class='homographnumber' and text()='1']";
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
			var glossNode = new ConfigurableDictionaryNode { FieldDescription = "Gloss", DictionaryNodeOptions = wsOpts, IsEnabled = true };
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Senses",
				Label = "Senses",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { glossNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { sensesNode },
				FieldDescription = "LexEntry",
				IsEnabled = true
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> {mainEntryNode});
			var testEntry = CreateInterestingLexEntry(Cache);

			using(var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				//SUT
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, null, settings));
				XHTMLWriter.Flush();
				const string oneSenseWithGlossOfGloss = xpathThruSense + "//span[@lang='en' and text()='gloss']";
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
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> {mainEntryNode});
			var entryOne = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(entryOne, "FirstHeadword", m_wsFr, Cache);
			var entryTwo = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(entryTwo, "SecondHeadword", m_wsFr, Cache);
			entryTwo.SensesOS.Clear();
			var entryOneId = entryOne.Hvo;
			var entryTwoId = entryTwo.Hvo;

			using(var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				XHTMLWriter.WriteStartElement("TESTWRAPPER"); //keep the xml valid (single root element)
				//SUT
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entryOne, mainEntryNode, null, settings));
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entryTwo, mainEntryNode, null, settings));
				XHTMLWriter.WriteEndElement();
				XHTMLWriter.Flush();
				var entryOneHasSensesSpan = "/TESTWRAPPER/div[@class='lexentry' and @id='hvo" + entryOneId + "']/span[@class='senses']";
				var entryTwoExists = "/TESTWRAPPER/div[@class='lexentry' and @id='hvo" + entryTwoId + "']";
				var entryTwoHasNoSensesSpan = "/TESTWRAPPER/div[@class='lexentry' and @id='hvo" + entryTwoId + "']/span[@class='senses']";
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(entryOneHasSensesSpan, 1);
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(entryTwoExists, 1);
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(entryTwoHasNoSensesSpan, 0);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_DefaultRootGeneratesResult()
		{
			var pubDecorator = new DictionaryPublicationDecorator(Cache,
																					(ISilDataAccessManaged)Cache.MainCacheAccessor,
																					Cache.ServiceLocator.GetInstance<Virtuals>().LexDbEntries);
			string defaultRoot = string.Concat(
				Path.Combine(FwDirectoryFinder.DefaultConfigurations, "Dictionary", "Root"), DictionaryConfigurationModel.FileExtension);
			var entry = CreateInterestingLexEntry(Cache);
			var dictionaryModel = new DictionaryConfigurationModel(defaultRoot, Cache);
			using(var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				//SUT
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entry, dictionaryModel.Parts[0], pubDecorator, settings));
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
			var entryOne = CreateInterestingLexEntry(Cache);

			using(var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				//SUT
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entryOne, mainEntryNode, null, settings));
				XHTMLWriter.Flush();
				const string sensesThatShouldNotBe = "/div[@class='entry']/span[@class='senses']";
				const string headwordThatShouldNotBe = "//span[@class='gloss']";
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(sensesThatShouldNotBe, 0);
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(headwordThatShouldNotBe, 0);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_TwoSensesWithSameInfoShowGramInfoFirst()
		{
			var DictionaryNodeSenseOptions = new DictionaryNodeSenseOptions
			{
				ShowSharedGrammarInfoFirst = true
			};
			var categorynfo = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLPartOfSpeech",
				Label = "Category Info.",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode>(),
				DictionaryNodeOptions = GetWsOptionsForLanguages(new [] { "en" } )
			};
			var gramInfoNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MorphoSyntaxAnalysisRA",
				CSSClassNameOverride = "msas",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { categorynfo }
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "Senses",
				IsEnabled = true,
				DictionaryNodeOptions = DictionaryNodeSenseOptions,
				Children = new List<ConfigurableDictionaryNode> { gramInfoNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { sensesNode },
				FieldDescription = "LexEntry",
				IsEnabled = true
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });
			var entry = CreateInterestingLexEntry(Cache);

			var posSeq = Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS;
			var pos = Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>().Create();
			posSeq.Add(pos);
			var sense = entry.SensesOS.First();

			var msa = Cache.ServiceLocator.GetInstance<IMoInflAffMsaFactory>().Create();
			entry.MorphoSyntaxAnalysesOC.Add(msa);
			sense.MorphoSyntaxAnalysisRA = msa;

			msa.PartOfSpeechRA = pos;
			msa.PartOfSpeechRA.Abbreviation.set_String(m_wsEn, "Blah");
			AddSenseToEntry(entry, "second sense", m_wsEn, Cache);
			var secondMsa = Cache.ServiceLocator.GetInstance<IMoStemMsaFactory>().Create();
			var secondSense = entry.SensesOS[1];
			entry.MorphoSyntaxAnalysesOC.Add(secondMsa);
			secondSense.MorphoSyntaxAnalysisRA = secondMsa;
			secondMsa.PartOfSpeechRA = pos;

			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				// SUT
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entry, mainEntryNode, null, settings));
				XHTMLWriter.Flush();
				const string sharedGramInfoPath = "//div[@class='lexentry']/span[@class='senses']/span[@class='sharedgrammaticalinfo']";
				const string gramInfoPath = "//div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense']/span[@class='msas']/span[@class='mlpartofspeech']";
				var xhtmlString = XHTMLStringBuilder.ToString();
				AssertThatXmlIn.String(xhtmlString).HasNoMatchForXpath(gramInfoPath);
				AssertThatXmlIn.String(xhtmlString).HasSpecifiedNumberOfMatchesForXpath(sharedGramInfoPath, 1);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_TwoSensesWithDifferentGramInfoShowInfoInSenses()
		{
			var DictionaryNodeSenseOptions = new DictionaryNodeSenseOptions
			{
				ShowSharedGrammarInfoFirst = true
			};
			var categorynfo = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLPartOfSpeech",
				Label = "Category Info.",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode>(),
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" })
			};
			var gramInfoNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MorphoSyntaxAnalysisRA",
				CSSClassNameOverride = "msas",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { categorynfo }
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "Senses",
				IsEnabled = true,
				DictionaryNodeOptions = DictionaryNodeSenseOptions,
				Children = new List<ConfigurableDictionaryNode> { gramInfoNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { sensesNode },
				FieldDescription = "LexEntry",
				IsEnabled = true
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });
			var entry = CreateInterestingLexEntry(Cache);

			var posSeq = Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS;
			var pos = Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>().Create();
			posSeq.Add(pos);
			var pos2 = Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>().Create();
			posSeq.Add(pos2);

			var sense = entry.SensesOS.First();

			var msa = Cache.ServiceLocator.GetInstance<IMoInflAffMsaFactory>().Create();
			entry.MorphoSyntaxAnalysesOC.Add(msa);
			sense.MorphoSyntaxAnalysisRA = msa;

			msa.PartOfSpeechRA = pos;
			msa.PartOfSpeechRA.Abbreviation.set_String(m_wsEn, "Blah");
			AddSenseToEntry(entry, "second sense", m_wsEn, Cache);
			var secondMsa = Cache.ServiceLocator.GetInstance<IMoStemMsaFactory>().Create();
			var secondSense = entry.SensesOS[1];
			entry.MorphoSyntaxAnalysesOC.Add(secondMsa);
			secondSense.MorphoSyntaxAnalysisRA = secondMsa;
			secondMsa.PartOfSpeechRA = pos2;
			secondMsa.PartOfSpeechRA.Abbreviation.set_String(m_wsEn, "NotBlah");

			using(var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				// SUT
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entry, mainEntryNode, null, settings));
				XHTMLWriter.Flush();
				const string sharedGramInfoPath = "//div[@class='lexentry']/span[@class='sensesos']/span[@class='sharedgrammaticalinfo']";
				const string gramInfoPath = "//div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense']/span[@class='msas']/span[@class='mlpartofspeech']";
				var xhtmlString = XHTMLStringBuilder.ToString();
				AssertThatXmlIn.String(xhtmlString).HasSpecifiedNumberOfMatchesForXpath(gramInfoPath, 2);
				AssertThatXmlIn.String(xhtmlString).HasNoMatchForXpath(sharedGramInfoPath);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_TwoSensesWithNoGramInfoDisplaysNothingForSharedGramInfo()
		{
			var DictionaryNodeSenseOptions = new DictionaryNodeSenseOptions
			{
				ShowSharedGrammarInfoFirst = true
			};
			var categorynfo = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLPartOfSpeech",
				Label = "Category Info.",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode>()
			};
			var gramInfoNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MorphoSyntaxAnalysisRA",
				CSSClassNameOverride = "MorphoSyntaxAnalysis",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { categorynfo }
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				IsEnabled = true,
				DictionaryNodeOptions = DictionaryNodeSenseOptions,
				Children = new List<ConfigurableDictionaryNode> { gramInfoNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { sensesNode },
				FieldDescription = "LexEntry",
				IsEnabled = true
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });
			var entry = CreateInterestingLexEntry(Cache);
			AddSenseToEntry(entry, "sense 2", m_wsEn, Cache);

			using(var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				// SUT
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entry, mainEntryNode, null, settings));
				XHTMLWriter.Flush();
				const string sharedGramInfoPath = "//div[@class='lexentry']/span[@class='sensesos']/span[@class='sharedgrammaticalinfo']";
				const string gramInfoPath = "//div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense']/span[@class='msas']/span[@class='mlpartofspeech']";
				var xhtmlString = XHTMLStringBuilder.ToString();
				AssertThatXmlIn.String(xhtmlString).HasNoMatchForXpath(gramInfoPath);
				AssertThatXmlIn.String(xhtmlString).HasNoMatchForXpath(sharedGramInfoPath);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_MakesSpanForRA()
		{
			var gramInfoNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MorphoSyntaxAnalysisRA",
				CSSClassNameOverride = "MorphoSyntaxAnalysis",
				IsEnabled = true
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Senses",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { gramInfoNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { sensesNode },
				FieldDescription = "LexEntry",
				IsEnabled = true
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> {mainEntryNode});
			var entry = CreateInterestingLexEntry(Cache);

			var sense = entry.SensesOS.First();

			var msa = Cache.ServiceLocator.GetInstance<IMoInflAffMsaFactory>().Create();
			entry.MorphoSyntaxAnalysesOC.Add(msa);
			sense.MorphoSyntaxAnalysisRA = msa;

			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				// SUT
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entry, mainEntryNode, null, settings));
				XHTMLWriter.Flush();
				const string gramInfoPath = xpathThruSense + "/span[@class='morphosyntaxanalysis']";
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(gramInfoPath, 1);
			}
		}

		/// <summary>
		/// If the dictionary configuration specifies to export grammatical info, but there is no such grammatical info object to export, don't write a span.
		/// </summary>
		[Test]
		public void GenerateXHTMLForEntry_DoesNotMakeSpanForRAIfNoData()
		{
			var gramInfoNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MorphoSyntaxAnalysisRA",
				IsEnabled = true
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Senses",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { gramInfoNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { sensesNode },
				FieldDescription = "LexEntry",
				IsEnabled = true
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> {mainEntryNode});
			var entry = CreateInterestingLexEntry(Cache);

			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				// SUT
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entry, mainEntryNode, null, settings));
				XHTMLWriter.Flush();
				const string gramInfoPath = xpathThruSense + "/span[@class='morphosyntaxanalysis']";
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(gramInfoPath, 0);
			}
		}

		/// <summary>
		/// If the dictionary configuration specifies to export scientific category, but there is no data in the field to export, don't write a span.
		/// </summary>
		[Test]
		public void GenerateXHTMLForEntry_DoesNotMakeSpanForTSStringIfNoData()
		{
			var scientificName = new ConfigurableDictionaryNode
			{
				FieldDescription = "ScientificName",
				IsEnabled = true
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Senses",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { scientificName }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { sensesNode },
				FieldDescription = "LexEntry",
				IsEnabled = true
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });
			var entry = CreateInterestingLexEntry(Cache);

			using(var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				// SUT
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entry, mainEntryNode, null, settings));
				XHTMLWriter.Flush();
				const string scientificCatPath = xpathThruSense + "/span[@class='scientificname']";
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(scientificCatPath, 0);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_SupportsGramAbbrChildOfMSARA()
		{
			var gramAbbrNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "InterlinearAbbrTSS",
				IsEnabled = true,
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};
			var gramNameNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "InterlinearNameTSS",
				IsEnabled = true,
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};
			var gramInfoNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MorphoSyntaxAnalysisRA",
				CSSClassNameOverride = "MorphoSyntaxAnalysis",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode>{gramAbbrNode,gramNameNode}
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Senses",
				IsEnabled = true,
				Children=new List<ConfigurableDictionaryNode>{gramInfoNode}
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { sensesNode },
				FieldDescription = "LexEntry",
				IsEnabled = true
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> {mainEntryNode});
			var wsFr = Cache.WritingSystemFactory.GetWsFromStr("fr");
			var entry = CreateInterestingLexEntry(Cache);

			ILangProject lp = Cache.LangProject;

			IFdoOwningSequence<ICmPossibility> posSeq = lp.PartsOfSpeechOA.PossibilitiesOS;
			IPartOfSpeech pos = Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>().Create();
			posSeq.Add(pos);

			var sense = entry.SensesOS.First();

			var msa = Cache.ServiceLocator.GetInstance<IMoInflAffMsaFactory>().Create();
			entry.MorphoSyntaxAnalysesOC.Add(msa);
			sense.MorphoSyntaxAnalysisRA = msa;

			msa.PartOfSpeechRA = pos;
			msa.PartOfSpeechRA.Abbreviation.set_String(wsFr,"Blah");

			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				// SUT
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entry, mainEntryNode, null, settings));
				XHTMLWriter.Flush();

				const string gramAbbr1 = xpathThruSense + "/span[@class='morphosyntaxanalysis']/span[@class='interlinearabbrtss']/span[@lang='fr' and text()='Blah']";
				const string gramAbbr2 = xpathThruSense + "/span[@class='morphosyntaxanalysis']/span[@class='interlinearabbrtss']/span[@lang='fr' and text()=':Any']";
				const string gramName1 = xpathThruSense + "/span[@class='morphosyntaxanalysis']/span[@class='interlinearnametss']/span[@lang='fr' and text()='Blah']";
				const string gramName2 = xpathThruSense + "/span[@class='morphosyntaxanalysis']/span[@class='interlinearnametss']/span[@lang='fr' and text()=':Any']";
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(gramAbbr1, 1);
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(gramAbbr2, 1);
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(gramName1, 1);
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(gramName2, 1);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_DefinitionOrGlossWorks()
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
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode>
				{
					new ConfigurableDictionaryNode { FieldDescription = "DefinitionOrGloss", DictionaryNodeOptions = wsOpts, IsEnabled = true }
				}
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { senses },
				FieldDescription = "LexEntry",
				IsEnabled = true
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> {mainEntryNode});
			var entryOne = CreateInterestingLexEntry(Cache);

			using(var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				//SUT
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entryOne, mainEntryNode, null, settings));
				XHTMLWriter.Flush();
				const string senseWithdefinitionOrGloss = "//span[@class='sense']/span[@class='definitionorgloss']/span[text()='gloss']";
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(senseWithdefinitionOrGloss, 1);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_DefinitionOrGlossWorks_WithAbbrev()
		{
			var wsOpts = new DictionaryNodeWritingSystemOptions
			{
				Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>
				{
					new DictionaryNodeListOptions.DictionaryNodeOption {Id = "en", IsEnabled = true,}
				},
				DisplayWritingSystemAbbreviations = true
			};
			var senses = new ConfigurableDictionaryNode
			{
				FieldDescription = "Senses",
				Label = "Senses",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode>
				{
					new ConfigurableDictionaryNode
					{
						FieldDescription = "DefinitionOrGloss",
						DictionaryNodeOptions = wsOpts,
						IsEnabled = true
					}
				}
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> {senses},
				FieldDescription = "LexEntry",
				IsEnabled = true
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> {mainEntryNode});
			var entryOne = CreateInterestingLexEntry(Cache);

			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				//SUT
				Assert.DoesNotThrow(
					() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entryOne, mainEntryNode, null, settings));
				XHTMLWriter.Flush();
				const string senseWithdefinitionOrGloss =
					"//span[@class='sense']/span[@class='definitionorgloss']/span[@class='writingsystemprefix' and normalize-space(text())='Eng']";
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString())
					.HasSpecifiedNumberOfMatchesForXpath(senseWithdefinitionOrGloss, 1);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_DuplicateConfigNodeWithSpaceWorks()
		{
			var senses = new ConfigurableDictionaryNode
			{
				FieldDescription = "Senses",
				Label = "Senses",
				IsDuplicate = true,
				LabelSuffix = "Test one",
				IsEnabled = true,
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { senses },
				FieldDescription = "LexEntry",
				IsEnabled = true
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });
			var entryOne = CreateInterestingLexEntry(Cache);

			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				//SUT
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entryOne, mainEntryNode, null, settings));
				XHTMLWriter.Flush();
				const string senseWithHyphenSuffix = "//span[@class='senses_test-one']/span[@class='senses_test-on']";
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(senseWithHyphenSuffix, 1);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_DuplicateConfigNodeWithPuncWorks()
		{
			var senses = new ConfigurableDictionaryNode
			{
				FieldDescription = "Senses",
				Label = "Senses",
				IsDuplicate = true,
				LabelSuffix = "#Test",
				IsEnabled = true,
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { senses },
				FieldDescription = "LexEntry",
				IsEnabled = true
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });
			var entryOne = CreateInterestingLexEntry(Cache);

			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				//SUT
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entryOne, mainEntryNode, null, settings));
				XHTMLWriter.Flush();
				const string senseWithHyphenSuffix = "//span[@class='senses_-test']/span[@class='senses_-tes']";
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(senseWithHyphenSuffix, 1);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_DuplicateConfigNodeWithMultiPuncWorks()
		{
			var senses = new ConfigurableDictionaryNode
			{
				FieldDescription = "Senses",
				Label = "Senses",
				IsDuplicate = true,
				LabelSuffix = "#Test$",
				IsEnabled = true,
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { senses },
				FieldDescription = "LexEntry",
				IsEnabled = true
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });
			var entryOne = CreateInterestingLexEntry(Cache);

			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				//SUT
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entryOne, mainEntryNode, null, settings));
				XHTMLWriter.Flush();
				const string senseWithHyphenSuffix = "//span[@class='senses_-test-']/span[@class='senses_-test']";
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(senseWithHyphenSuffix, 1);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_MLHeadWordVirtualPropWorks()
		{
			var wsOpts = new DictionaryNodeWritingSystemOptions
			{
				Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>
				{
					new DictionaryNodeListOptions.DictionaryNodeOption { Id = "vernacular", IsEnabled = true }
				}
			};
			const string headWord = "mlhw";
			var mlHeadWordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLHeadWord",
				CSSClassNameOverride = headWord,
				DictionaryNodeOptions = wsOpts,
				IsEnabled = true
			};
			const string nters = "nters";
			var nonTrivialRoots = new ConfigurableDictionaryNode
			{
				FieldDescription = "NonTrivialEntryRoots",
				DictionaryNodeOptions = wsOpts,
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { mlHeadWordNode },
				CSSClassNameOverride = nters
			};
			var otherRefForms = new ConfigurableDictionaryNode
			{
				FieldDescription = "ComplexFormsNotSubentries",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { nonTrivialRoots },
				CSSClassNameOverride = "cfns"
			};
			var senses = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				Label = "Senses",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { otherRefForms }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { senses },
				FieldDescription = "LexEntry",
				IsEnabled = true
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });
			// Build up model that will allow for testing of the MLHeadword virtual property under
			// the NonTrivialEntryRoots back reference field.
			var entryOne = CreateInterestingLexEntry(Cache);
			var entryTwo = CreateInterestingLexEntry(Cache);
			var entryThree = CreateInterestingLexEntry(Cache);
			const string entryThreeForm = "MLHW";
			AddHeadwordToEntry(entryThree, entryThreeForm, m_wsFr, Cache);
			var complexEntryRef = Cache.ServiceLocator.GetInstance<ILexEntryRefFactory>().Create();
			entryTwo.EntryRefsOS.Add(complexEntryRef);
			complexEntryRef.RefType = LexEntryRefTags.krtComplexForm;
			complexEntryRef.ComponentLexemesRS.Add(entryOne.SensesOS[0]);
			complexEntryRef.ComponentLexemesRS.Add(entryThree);
			complexEntryRef.PrimaryLexemesRS.Add(entryThree);
			complexEntryRef.ShowComplexFormsInRS.Add(entryThree);
			complexEntryRef.ShowComplexFormsInRS.Add(entryOne.SensesOS[0]);

			using(var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				// SUT
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entryOne, mainEntryNode, null, settings));
				XHTMLWriter.Flush();
				var headwordMatch = string.Format("//span[@class='{0}']//span[@class='{1}']/span[text()='{2}']",
															 nters, headWord, entryThreeForm);
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(headwordMatch, 1);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_EtymologySourceWorks()
		{
			//This test also proves to verify that .NET String properties can be generated
			var etymology = new ConfigurableDictionaryNode
			{
				FieldDescription = "EtymologyOA",
				CSSClassNameOverride = "Etymology",
				Label = "Etymology",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode>
				{
					new ConfigurableDictionaryNode { FieldDescription = "Source", IsEnabled = true }
				}
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { etymology },
				FieldDescription = "LexEntry",
				IsEnabled = true
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });
			var entryOne = CreateInterestingLexEntry(Cache);
			entryOne.EtymologyOA = Cache.ServiceLocator.GetInstance<ILexEtymologyFactory>().Create();
			entryOne.EtymologyOA.Source = "George";

			using(var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				//SUT
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entryOne, mainEntryNode, null, settings));
				XHTMLWriter.Flush();
				const string etymologyWithGeorgeSource = "//span[@class='etymology']/span[@class='source' and text()='George']";
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(etymologyWithGeorgeSource, 1);
			}
		}

		[Test]
		public void GetPropertyTypeForConfigurationNode_NullConfigurationNodeThrowsNullArgument()
		{
			// SUT
			Assert.Throws<ArgumentNullException>(() => ConfiguredXHTMLGenerator.GetPropertyTypeForConfigurationNode(null));
		}

		[Test]
		public void GetPropertyTypeForConfigurationNode_RootMemberWorks()
		{
			ConfiguredXHTMLGenerator.AssemblyFile = "xWorksTests";
			var stringNode = new ConfigurableDictionaryNode { FieldDescription = "RootMember" };
			var rootNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SIL.FieldWorks.XWorks.TestRootClass",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { stringNode }
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> {rootNode});
			var result = ConfiguredXHTMLGenerator.PropertyType.InvalidProperty;
			// SUT
			Assert.DoesNotThrow(() => result = ConfiguredXHTMLGenerator.GetPropertyTypeForConfigurationNode(stringNode));
			Assert.That(result, Is.EqualTo(ConfiguredXHTMLGenerator.PropertyType.PrimitiveType));
		}

		[Test]
		public void GetPropertyTypeForConfigurationNode_InterfacePropertyWorks()
		{
			ConfiguredXHTMLGenerator.AssemblyFile = "xWorksTests";
			var interfaceNode = new ConfigurableDictionaryNode { FieldDescription = "TestString" };
			var memberNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "RootMember",
				Children = new List<ConfigurableDictionaryNode> { interfaceNode }
			};
			var rootNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SIL.FieldWorks.XWorks.TestRootClass",
				Children = new List<ConfigurableDictionaryNode> { memberNode }
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> {rootNode});
			var result = ConfiguredXHTMLGenerator.PropertyType.InvalidProperty;
			// SUT
			Assert.DoesNotThrow(() => result = ConfiguredXHTMLGenerator.GetPropertyTypeForConfigurationNode(interfaceNode));
			Assert.That(result, Is.EqualTo(ConfiguredXHTMLGenerator.PropertyType.PrimitiveType));
		}

		[Test]
		public void GetPropertyTypeForConfigurationNode_FirstParentInterfacePropertyIsUsable()
		{
			ConfiguredXHTMLGenerator.AssemblyFile = "xWorksTests";
			var interfaceNode = new ConfigurableDictionaryNode { FieldDescription = "TestMoForm" };
			var memberNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "RootMember",
				Children = new List<ConfigurableDictionaryNode> { interfaceNode }
			};
			var rootNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SIL.FieldWorks.XWorks.TestRootClass",
				Children = new List<ConfigurableDictionaryNode> { memberNode }
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> {rootNode});
			var result = ConfiguredXHTMLGenerator.PropertyType.InvalidProperty;
			// SUT
			Assert.DoesNotThrow(() => result = ConfiguredXHTMLGenerator.GetPropertyTypeForConfigurationNode(interfaceNode));
			Assert.That(result, Is.EqualTo(ConfiguredXHTMLGenerator.PropertyType.MoFormType));
		}

		[Test]
		public void GetPropertyTypeForConfigurationNode_SecondParentInterfacePropertyIsUsable()
		{
			ConfiguredXHTMLGenerator.AssemblyFile = "xWorksTests";
			var interfaceNode = new ConfigurableDictionaryNode { FieldDescription = "TestIcmObject" };
			var memberNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "RootMember",
				Children = new List<ConfigurableDictionaryNode> { interfaceNode }
			};
			var rootNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SIL.FieldWorks.XWorks.TestRootClass",
				Children = new List<ConfigurableDictionaryNode> { memberNode }
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> {rootNode});
			var result = ConfiguredXHTMLGenerator.PropertyType.InvalidProperty;
			// SUT
			Assert.DoesNotThrow(() => result = ConfiguredXHTMLGenerator.GetPropertyTypeForConfigurationNode(interfaceNode));
			Assert.That(result, Is.EqualTo(ConfiguredXHTMLGenerator.PropertyType.CmObjectType));
		}

		[Test]
		public void GetPropertyTypeForConfigurationNode_GrandparentInterfacePropertyIsUsable()
		{
			ConfiguredXHTMLGenerator.AssemblyFile = "xWorksTests";
			var interfaceNode = new ConfigurableDictionaryNode { FieldDescription = "TestCollection" };
			var memberNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "RootMember",
				Children = new List<ConfigurableDictionaryNode> { interfaceNode }
			};
			var rootNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SIL.FieldWorks.XWorks.TestRootClass",
				Children = new List<ConfigurableDictionaryNode> { memberNode }
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> {rootNode});
			var result = ConfiguredXHTMLGenerator.PropertyType.InvalidProperty;
			// SUT
			Assert.DoesNotThrow(() => result = ConfiguredXHTMLGenerator.GetPropertyTypeForConfigurationNode(interfaceNode));
			Assert.That(result, Is.EqualTo(ConfiguredXHTMLGenerator.PropertyType.CollectionType));
		}

		[Test]
		public void GetPropertyTypeForConfigurationNode_NonInterfaceMemberIsUsable()
		{
			ConfiguredXHTMLGenerator.AssemblyFile = "xWorksTests";
			var stringNodeInClass = new ConfigurableDictionaryNode { FieldDescription = "TestNonInterfaceString" };
			var memberNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ConcreteMember",
				Children = new List<ConfigurableDictionaryNode> { stringNodeInClass }
			};
			var rootNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SIL.FieldWorks.XWorks.TestRootClass",
				Children = new List<ConfigurableDictionaryNode> { memberNode }
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> {rootNode});
			var result = ConfiguredXHTMLGenerator.PropertyType.InvalidProperty;
			// SUT
			Assert.DoesNotThrow(() => result = ConfiguredXHTMLGenerator.GetPropertyTypeForConfigurationNode(stringNodeInClass));
			Assert.That(result, Is.EqualTo(ConfiguredXHTMLGenerator.PropertyType.PrimitiveType));
		}

		[Test]
		public void GetPropertyTypeForConfigurationNode_InvalidChildDoesNotThrow()
		{
			ConfiguredXHTMLGenerator.AssemblyFile = "xWorksTests";
			var interfaceNode = new ConfigurableDictionaryNode { FieldDescription = "TestCollection" };
			var memberNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ConcreteMember",
				SubField = "TestNonInterfaceString",
				Children = new List<ConfigurableDictionaryNode> { interfaceNode }
			};
			var rootNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SIL.FieldWorks.XWorks.TestRootClass",
				Children = new List<ConfigurableDictionaryNode> { memberNode }
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> {rootNode});
			var result = ConfiguredXHTMLGenerator.PropertyType.PrimitiveType;
			// SUT
			Assert.DoesNotThrow(() => result = ConfiguredXHTMLGenerator.GetPropertyTypeForConfigurationNode(interfaceNode));
			Assert.That(result, Is.EqualTo(ConfiguredXHTMLGenerator.PropertyType.InvalidProperty));
		}

		[Test]
		public void GetPropertyTypeForConfigurationNode_SubFieldWorks()
		{
			ConfiguredXHTMLGenerator.AssemblyFile = "xWorksTests";
			var memberNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ConcreteMember",
				SubField = "TestNonInterfaceString"
			};
			var rootNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SIL.FieldWorks.XWorks.TestRootClass",
				Children = new List<ConfigurableDictionaryNode> { memberNode }
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> {rootNode});
			var result = ConfiguredXHTMLGenerator.PropertyType.InvalidProperty;
			// SUT
			Assert.DoesNotThrow(() => result = ConfiguredXHTMLGenerator.GetPropertyTypeForConfigurationNode(memberNode));
			Assert.That(result, Is.EqualTo(ConfiguredXHTMLGenerator.PropertyType.PrimitiveType));
		}

		[Test]
		public void GetPropertyTypeForConfigurationNode_InvalidSubFieldReturnsInvalidProperty()
		{
			ConfiguredXHTMLGenerator.AssemblyFile = "xWorksTests";
			var memberNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ConcreteMember",
				SubField = "NonExistantSubField"
			};
			var rootNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SIL.FieldWorks.XWorks.TestRootClass",
				Children = new List<ConfigurableDictionaryNode> { memberNode }
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { rootNode });
			var result = ConfiguredXHTMLGenerator.PropertyType.PrimitiveType;
			// SUT
			Assert.DoesNotThrow(() => result = ConfiguredXHTMLGenerator.GetPropertyTypeForConfigurationNode(memberNode));
			Assert.That(result, Is.EqualTo(ConfiguredXHTMLGenerator.PropertyType.InvalidProperty));
		}

		[Test]
		public void GetPropertyTypeForConfigurationNode_InvalidRootThrowsWithMessage()
		{
			ConfiguredXHTMLGenerator.AssemblyFile = "xWorksTests";
			var rootNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SIL.FieldWorks.XWorks.NonExistantClass",
			};
			// SUT
			Assert.That(() => ConfiguredXHTMLGenerator.GetPropertyTypeForConfigurationNode(rootNode),
				Throws.InstanceOf<ArgumentException>().With.Message.Contains(rootNode.FieldDescription));
		}

		[Test]
		public void GetPropertyTypeForConfigurationNode_PictureFileReturnsCmPictureType()
		{
			ConfiguredXHTMLGenerator.AssemblyFile = "xWorksTests";
			var pictureFileNode = new ConfigurableDictionaryNode { FieldDescription = "PictureFileRA" };
			var memberNode = new ConfigurableDictionaryNode
			{
				DictionaryNodeOptions = new DictionaryNodePictureOptions(),
				FieldDescription = "Pictures",
				Children = new List<ConfigurableDictionaryNode> { pictureFileNode }
			};
			var rootNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SIL.FieldWorks.XWorks.TestPictureClass",
				Children = new List<ConfigurableDictionaryNode> { memberNode }
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { rootNode });
			var result = ConfiguredXHTMLGenerator.PropertyType.InvalidProperty;
			// SUT
			Assert.DoesNotThrow(() => result = ConfiguredXHTMLGenerator.GetPropertyTypeForConfigurationNode(pictureFileNode));
			Assert.That(result, Is.EqualTo(ConfiguredXHTMLGenerator.PropertyType.CmPictureType));
		}

		[Test]
		public void GetPropertyTypeForConfigurationNode_StTextReturnsPrimitive()
		{
			var fieldName = "CustomMultiPara";
			using (var customField = new CustomFieldForTest(Cache, fieldName, fieldName, Cache.MetaDataCacheAccessor.GetClassId("LexEntry"), StTextTags.kClassId, -1,
			 CellarPropertyType.OwningAtomic, Guid.Empty))
			{
				var customFieldNode = new ConfigurableDictionaryNode
				{
					FieldDescription = fieldName,
					IsEnabled = true,
					IsCustomField = true
				};
				var mainEntryNode = new ConfigurableDictionaryNode
				{
					Children = new List<ConfigurableDictionaryNode> { customFieldNode },
					FieldDescription = "LexEntry",
					IsEnabled = true
				};
				DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });
				var testEntry = CreateInterestingLexEntry(Cache);
				const string customData = @"I am custom data";
				var locator = Cache.ServiceLocator;
				// Set custom field data
				var multiParaHvo = Cache.MainCacheAccessor.MakeNewObject(StTextTags.kClassId, testEntry.Hvo, customField.Flid, -2);
				var textObject = locator.GetInstance<IStTextRepository>().GetObject(multiParaHvo);
				var paragraph = locator.GetInstance<IStTxtParaFactory>().Create();
				textObject.ParagraphsOS.Add(paragraph);
				paragraph.Contents = Cache.TsStrFactory.MakeString(customData, m_wsFr);
				//SUT
				var type = ConfiguredXHTMLGenerator.GetPropertyTypeForConfigurationNode(customFieldNode, Cache);
				Assert.AreEqual(ConfiguredXHTMLGenerator.PropertyType.PrimitiveType, type);
			}
		}

		[Test]
		public void IsMinorEntry_ReturnsTrueForMinorEntry()
		{
			var mainEntry = CreateInterestingLexEntry(Cache);
			var minorEntry = CreateInterestingLexEntry(Cache);
			CreateVariantForm(mainEntry, minorEntry);
			// SUT
			Assert.That(ConfiguredXHTMLGenerator.IsMinorEntry(minorEntry));
		}

		[Test]
		public void IsMinorEntry_ReturnsFalseWhenNotAMinorEntry()
		{
			var mainEntry = CreateInterestingLexEntry(Cache);
			var minorEntry = CreateInterestingLexEntry(Cache);
			CreateVariantForm(mainEntry, minorEntry);
			// SUT
			Assert.False(ConfiguredXHTMLGenerator.IsMinorEntry(mainEntry));
			Assert.False(ConfiguredXHTMLGenerator.IsMinorEntry(Cache.ServiceLocator.GetInstance<IReversalIndexEntryFactory>().Create()));
		}

		[Test]
		public void GenerateEntryHtmlWithStyles_NullEntryThrowsArgumentNull()
		{
			Assert.Throws<ArgumentNullException>(() => ConfiguredXHTMLGenerator.GenerateEntryHtmlWithStyles(null, new DictionaryConfigurationModel(), null, null));
		}

		[Test]
		public void GenerateEntryHtmlWithStyles_MinorEntryUsesMinorEntryFormatting()
		{
			var pubDecorator = new DictionaryPublicationDecorator(Cache, (ISilDataAccessManaged)Cache.MainCacheAccessor,
																					Cache.ServiceLocator.GetInstance<Virtuals>().LexDbEntries);
			var configModel = CreateInterestingConfigurationModel();
			var mainEntry = CreateInterestingLexEntry(Cache);
			var minorEntry = CreateInterestingLexEntry(Cache);
			CreateVariantForm(mainEntry, minorEntry);
			SetPublishAsMinorEntry(minorEntry, true);
			configModel.Parts[1].DictionaryNodeOptions =
				configModel.Parts[2].DictionaryNodeOptions = GetFullyEnabledListOptions(DictionaryNodeListOptions.ListIds.Minor);
			//SUT
			var xhtml = ConfiguredXHTMLGenerator.GenerateEntryHtmlWithStyles(minorEntry, configModel, pubDecorator, m_mediator);
			// this test relies on specific test data from CreateInterestingConfigurationModel
			const string xpath = "/html/body/div[@class='minorentry']/span[@class='entry']";
			AssertThatXmlIn.String(xhtml).HasSpecifiedNumberOfMatchesForXpath(xpath, 2);
		}

		[Test]
		public void GenerateEntryHtmlWithStyles_MinorEntryUnCheckedItemsGenerateNothing()
		{
			var pubDecorator = new DictionaryPublicationDecorator(Cache, (ISilDataAccessManaged)Cache.MainCacheAccessor,
																					Cache.ServiceLocator.GetInstance<Virtuals>().LexDbEntries);
			var configModel = CreateInterestingConfigurationModel();
			var mainEntry = CreateInterestingLexEntry(Cache);
			var minorEntry = CreateInterestingLexEntry(Cache);
			CreateVariantForm(mainEntry, minorEntry);
			configModel.Parts[1].DictionaryNodeOptions = configModel.Parts[2].DictionaryNodeOptions =
				GetListOptionsForItems(DictionaryNodeListOptions.ListIds.Minor, new ICmPossibility[0]);
			SetPublishAsMinorEntry(minorEntry, true);
			//SUT
			var xhtml = ConfiguredXHTMLGenerator.GenerateEntryHtmlWithStyles(minorEntry, configModel, pubDecorator, m_mediator);
			// this test relies on specific test data from CreateInterestingConfigurationModel
			const string xpath = "/html/body/div[@class='minorentry']/span[@class='entry']";
			// only the variant is selected, so the other minor entry should not have been generated
			AssertThatXmlIn.String(xhtml).HasSpecifiedNumberOfMatchesForXpath(xpath, 0);
		}

		[Test]
		public void GenerateEntryHtmlWithStyles_DoesNotShowHiddenMinorEntries()
		{
			var pubDecorator = new DictionaryPublicationDecorator(Cache, (ISilDataAccessManaged)Cache.MainCacheAccessor,
																					Cache.ServiceLocator.GetInstance<Virtuals>().LexDbEntries);
			var configModel = CreateInterestingConfigurationModel();
			var mainEntry = CreateInterestingLexEntry(Cache);
			var minorEntry = CreateInterestingLexEntry(Cache);
			CreateVariantForm(mainEntry, minorEntry);
			SetPublishAsMinorEntry(minorEntry, false);

			//SUT
			var xhtml = ConfiguredXHTMLGenerator.GenerateEntryHtmlWithStyles(minorEntry, configModel, pubDecorator, m_mediator);
			// this test relies on specific test data from CreateInterestingConfigurationModel
			const string xpath = "/div[@class='minorentry']/span[@class='entry']";
			AssertThatXmlIn.String(xhtml).HasNoMatchForXpath(xpath);
		}

		[Test]
		public void GenerateXHTMLForEntry_SenseNumbersGeneratedForMultipleSenses()
		{
			var pubDecorator = new DictionaryPublicationDecorator(Cache,
																					(ISilDataAccessManaged)Cache.MainCacheAccessor,
																					Cache.ServiceLocator.GetInstance<Virtuals>().LexDbEntries);
			var wsOpts = new DictionaryNodeWritingSystemOptions
			{
				Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>
				{
					new DictionaryNodeListOptions.DictionaryNodeOption { Id = "en", IsEnabled = true }
				}
			};
			var glossNode = new ConfigurableDictionaryNode { FieldDescription = "Gloss", DictionaryNodeOptions = wsOpts, IsEnabled = true };
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "Senses",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions { NumberingStyle = "%d" },
				Children = new List<ConfigurableDictionaryNode> { glossNode },
				IsEnabled = true
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { sensesNode },
				IsEnabled = true
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });
			var testEntry = CreateInterestingLexEntry(Cache);
			AddSenseToEntry(testEntry, "second gloss", m_wsEn, Cache);
			using(var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				//SUT
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, pubDecorator, settings));
				XHTMLWriter.Flush();
				const string senseNumberOne = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense' and preceding-sibling::span[@class='sensenumber' and text()='1']]//span[@lang='en' and text()='gloss']";
				const string senseNumberTwo = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense' and preceding-sibling::span[@class='sensenumber' and text()='2']]//span[@lang='en' and text()='second gloss']";
				//This assert is dependent on the specific entry data created in CreateInterestingLexEntry
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(senseNumberOne, 1);
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(senseNumberTwo, 1);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_SingleSenseGetsNoSenseNumber()
		{
			var pubDecorator = new DictionaryPublicationDecorator(Cache,
																					(ISilDataAccessManaged)Cache.MainCacheAccessor,
																					Cache.ServiceLocator.GetInstance<Virtuals>().LexDbEntries);
			var wsOpts = new DictionaryNodeWritingSystemOptions
			{
				Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>
				{
					new DictionaryNodeListOptions.DictionaryNodeOption { Id = "en", IsEnabled = true }
				}
			};
			var glossNode = new ConfigurableDictionaryNode { FieldDescription = "Gloss", DictionaryNodeOptions = wsOpts, IsEnabled = true };
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "Senses",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions { NumberEvenASingleSense = false },
				Children = new List<ConfigurableDictionaryNode> { glossNode },
				IsEnabled = true
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { sensesNode },
				IsEnabled = true
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });
			var testEntry = CreateInterestingLexEntry(Cache);

			using(var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				// SUT
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, pubDecorator, settings));
				XHTMLWriter.Flush();
				const string senseNumberOne = "/div[@class='lexentry']/span[@class='senses']/span[@class='sense' and preceding-sibling::span[@class='sensenumber' and text()='1']]/span[@lang='en' and text()='gloss']";
				// This assert is dependent on the specific entry data created in CreateInterestingLexEntry
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasNoMatchForXpath(senseNumberOne);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_NumberingSingleSenseAlsoCountsSubsense()
		{
			var pubDecorator = new DictionaryPublicationDecorator(Cache, (ISilDataAccessManaged)Cache.MainCacheAccessor, Cache.ServiceLocator.GetInstance<Virtuals>().LexDbEntries);
			var wsOpts = new DictionaryNodeWritingSystemOptions
			{
				Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>
				{
					new DictionaryNodeListOptions.DictionaryNodeOption { Id = "en", IsEnabled = true }
				}
			};
			var DictionaryNodeSenseOptions = new DictionaryNodeSenseOptions
			{
				BeforeNumber = "",
				AfterNumber = ")",
				NumberStyle = "Dictionary-SenseNumber",
				NumberingStyle = "%d",
				DisplayEachSenseInAParagraph = false,
				NumberEvenASingleSense = false,
				ShowSharedGrammarInfoFirst = false
			};
			var DictionaryNodeSubSenseOptions = new DictionaryNodeSenseOptions
			{
				BeforeNumber = "",
				AfterNumber = ")",
				NumberStyle = "Dictionary-SenseNumber",
				NumberingStyle = "%d.d",
				DisplayEachSenseInAParagraph = false,
				NumberEvenASingleSense = false,
				ShowSharedGrammarInfoFirst = false
			};

			var glossNode = new ConfigurableDictionaryNode { FieldDescription = "Gloss", DictionaryNodeOptions = wsOpts, IsEnabled = true };
			var subSenseNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				Label = "Subsenses",
				DictionaryNodeOptions = DictionaryNodeSubSenseOptions,
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { glossNode }
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				DictionaryNodeOptions = DictionaryNodeSenseOptions,
				Children = new List<ConfigurableDictionaryNode> { glossNode, subSenseNode },
				IsEnabled = true
			};

			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { sensesNode },
				IsEnabled = true
			};

			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });
			var testEntry = CreateInterestingLexEntry(Cache);
			AddSingleSubSenseToSense(testEntry, "gloss",testEntry.SensesOS.First());
			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				//SUT
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, pubDecorator, settings));
				XHTMLWriter.Flush();
				const string SenseOneSubSense = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense' and preceding-sibling::span[@class='sensenumber' and text()='1']]/span[@class='senses']/span[@class='sensecontent']//span[@lang='en' and text()='gloss1.1']";
				//This assert is dependent on the specific entry data created in CreateInterestingLexEntry
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(SenseOneSubSense, 1);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_SensesAndSubSensesWithDifferentNumberingStyle()
		{
			var pubDecorator = new DictionaryPublicationDecorator(Cache, (ISilDataAccessManaged)Cache.MainCacheAccessor, Cache.ServiceLocator.GetInstance<Virtuals>().LexDbEntries);
			var wsOpts = new DictionaryNodeWritingSystemOptions
			{
				Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>
				{
					new DictionaryNodeListOptions.DictionaryNodeOption { Id = "en", IsEnabled = true }
				}
			};
			var DictionaryNodeSenseOptions = new DictionaryNodeSenseOptions
			{
				BeforeNumber = "",
				AfterNumber = ")",
				NumberStyle = "Dictionary-SenseNumber",
				NumberingStyle = "%A",
				DisplayEachSenseInAParagraph = false,
				NumberEvenASingleSense = true,
				ShowSharedGrammarInfoFirst = false
			};
			var DictionaryNodeSubSenseOptions = new DictionaryNodeSenseOptions
			{
				BeforeNumber = "",
				AfterNumber = ")",
				NumberStyle = "Dictionary-SenseNumber",
				NumberingStyle = "%I",
				DisplayEachSenseInAParagraph = false,
				NumberEvenASingleSense = true,
				ShowSharedGrammarInfoFirst = false
			};

			var glossNode = new ConfigurableDictionaryNode { FieldDescription = "Gloss", DictionaryNodeOptions = wsOpts, IsEnabled = true };
			var subSenseNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				Label = "Subsenses",
				DictionaryNodeOptions = DictionaryNodeSubSenseOptions,
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { glossNode }
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				DictionaryNodeOptions = DictionaryNodeSenseOptions,
				Children = new List<ConfigurableDictionaryNode> { glossNode, subSenseNode },
				IsEnabled = true
			};

			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { sensesNode },
				IsEnabled = true
			};

			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });
			var testEntry = CreateInterestingLexEntry(Cache);
			AddSubSenseToSense(testEntry, "second gloss");
			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				//SUT
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, pubDecorator, settings));
				XHTMLWriter.Flush();
				const string senseNumberOne = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense' and preceding-sibling::span[@class='sensenumber' and text()='A']]//span[@lang='en' and text()='gloss']";
				const string senseNumberTwo = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense' and preceding-sibling::span[@class='sensenumber' and text()='B']]//span[@lang='en' and text()='second gloss']";
				const string subSensesNumberTwoOne = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense' and preceding-sibling::span[@class='sensenumber' and text()='I']]//span[@lang='en' and text()='second gloss2.1']";
				const string subSenseNumberTwoTwo = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense' and preceding-sibling::span[@class='sensenumber' and text()='II']]//span[@lang='en' and text()='second gloss2.2']";
				//This assert is dependent on the specific entry data created in CreateInterestingLexEntry
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(senseNumberOne, 1);
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(senseNumberTwo, 1);
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(subSensesNumberTwoOne, 1);
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(subSenseNumberTwoTwo, 1);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_SensesAndSubSensesWithNumberingStyle()
		{
			var pubDecorator = new DictionaryPublicationDecorator(Cache, (ISilDataAccessManaged)Cache.MainCacheAccessor, Cache.ServiceLocator.GetInstance<Virtuals>().LexDbEntries);
			var wsOpts = new DictionaryNodeWritingSystemOptions
			{
				Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>
				{
					new DictionaryNodeListOptions.DictionaryNodeOption { Id = "en", IsEnabled = true }
				}
			};
			var DictionaryNodeSenseOptions = new DictionaryNodeSenseOptions
			{
				BeforeNumber = "",
				AfterNumber = ")",
				NumberStyle = "Dictionary-SenseNumber",
				NumberingStyle = "%A",
				DisplayEachSenseInAParagraph = false,
				NumberEvenASingleSense = true,
				ShowSharedGrammarInfoFirst = false
			};
			var DictionaryNodeSubSenseOptions = new DictionaryNodeSenseOptions
			{
				BeforeNumber = "",
				AfterNumber = ")",
				NumberStyle = "Dictionary-SenseNumber",
				NumberingStyle = "%O",
				DisplayEachSenseInAParagraph = false,
				NumberEvenASingleSense = true,
				ShowSharedGrammarInfoFirst = false
			};

			var glossNode = new ConfigurableDictionaryNode { FieldDescription = "Gloss", DictionaryNodeOptions = wsOpts, IsEnabled = true };
			var subSenseNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				Label = "Subsenses",
				DictionaryNodeOptions = DictionaryNodeSubSenseOptions,
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { glossNode }
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				DictionaryNodeOptions = DictionaryNodeSenseOptions,
				Children = new List<ConfigurableDictionaryNode> { glossNode, subSenseNode },
				IsEnabled = true
			};

			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { sensesNode },
				IsEnabled = true
			};

			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });
			var testEntry = CreateInterestingLexEntry(Cache);
			AddSubSenseToSense(testEntry, "second gloss");
			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				//SUT
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, pubDecorator, settings));
				XHTMLWriter.Flush();
				const string senseNumberOne = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense' and preceding-sibling::span[@class='sensenumber' and text()='A']]//span[@lang='en' and text()='gloss']";
				const string senseNumberTwo = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense' and preceding-sibling::span[@class='sensenumber' and text()='B']]//span[@lang='en' and text()='second gloss']";
				const string subSensesNumberTwoOne = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense' and preceding-sibling::span[@class='sensenumber' and text()='B.1']]//span[@lang='en' and text()='second gloss2.1']";
				const string subSenseNumberTwoTwo = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense' and preceding-sibling::span[@class='sensenumber' and text()='B.2']]//span[@lang='en' and text()='second gloss2.2']";
				//This assert is dependent on the specific entry data created in CreateInterestingLexEntry
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(senseNumberOne, 1);
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(senseNumberTwo, 1);
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(subSensesNumberTwoOne, 1);
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(subSenseNumberTwoTwo, 1);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_SensesNoneAndSubSensesWithNumberingStyle()
		{
			var pubDecorator = new DictionaryPublicationDecorator(Cache, (ISilDataAccessManaged)Cache.MainCacheAccessor, Cache.ServiceLocator.GetInstance<Virtuals>().LexDbEntries);
			var wsOpts = new DictionaryNodeWritingSystemOptions
			{
				Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>
				{
					new DictionaryNodeListOptions.DictionaryNodeOption { Id = "en", IsEnabled = true }
				}
			};
			var DictionaryNodeSenseOptions = new DictionaryNodeSenseOptions
			{
				BeforeNumber = "",
				AfterNumber = ")",
				NumberStyle = "Dictionary-SenseNumber",
				DisplayEachSenseInAParagraph = false,
				NumberEvenASingleSense = true,
				ShowSharedGrammarInfoFirst = false
			};
			var DictionaryNodeSubSenseOptions = new DictionaryNodeSenseOptions
			{
				BeforeNumber = "",
				AfterNumber = ")",
				NumberStyle = "Dictionary-SenseNumber",
				NumberingStyle = "%O",
				DisplayEachSenseInAParagraph = false,
				NumberEvenASingleSense = true,
				ShowSharedGrammarInfoFirst = false
			};

			var glossNode = new ConfigurableDictionaryNode { FieldDescription = "Gloss", DictionaryNodeOptions = wsOpts, IsEnabled = true };
			var subSenseNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				Label = "Subsenses",
				DictionaryNodeOptions = DictionaryNodeSubSenseOptions,
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { glossNode }
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				DictionaryNodeOptions = DictionaryNodeSenseOptions,
				Children = new List<ConfigurableDictionaryNode> { glossNode, subSenseNode },
				IsEnabled = true
			};

			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { sensesNode },
				IsEnabled = true
			};

			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });
			var testEntry = CreateInterestingLexEntry(Cache);
			AddSubSenseToSense(testEntry, "second gloss");
			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				//SUT
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, pubDecorator, settings));
				XHTMLWriter.Flush();
				const string subSensesNumberOne = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense' and preceding-sibling::span[@class='sensenumber' and text()='1']]//span[@lang='en' and text()='second gloss2.1']";
				const string subSenseNumberTwo = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense' and preceding-sibling::span[@class='sensenumber' and text()='2']]//span[@lang='en' and text()='second gloss2.2']";
				//This assert is dependent on the specific entry data created in CreateInterestingLexEntry
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(subSensesNumberOne, 1);
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(subSenseNumberTwo, 1);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_SensesGeneratedForMultipleSubSenses()
		{
			var pubDecorator = new DictionaryPublicationDecorator(Cache, (ISilDataAccessManaged)Cache.MainCacheAccessor, Cache.ServiceLocator.GetInstance<Virtuals>().LexDbEntries);
			var wsOpts = new DictionaryNodeWritingSystemOptions
			{
				Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>
				{
					new DictionaryNodeListOptions.DictionaryNodeOption { Id = "en", IsEnabled = true }
				}
			};
			var DictionaryNodeSubSenseOptions = new DictionaryNodeSenseOptions
			{
				BeforeNumber = "",
				AfterNumber = ")",
				NumberStyle = "Dictionary-SenseNumber",
				NumberingStyle = "%d",
				DisplayEachSenseInAParagraph = false,
				NumberEvenASingleSense = true,
				ShowSharedGrammarInfoFirst = false
			};

			var glossNode = new ConfigurableDictionaryNode { FieldDescription = "Gloss", DictionaryNodeOptions = wsOpts, IsEnabled = true };
			var subSenseNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				Label = "Subsenses",
				DictionaryNodeOptions = DictionaryNodeSubSenseOptions,
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { glossNode }
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				DictionaryNodeOptions = DictionaryNodeSubSenseOptions,
				Children = new List<ConfigurableDictionaryNode> { glossNode, subSenseNode },
				IsEnabled = true
			};

			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { sensesNode },
				IsEnabled = true
			};

			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });
			var testEntry = CreateInterestingLexEntry(Cache);
			AddSubSenseToSense(testEntry, "second gloss");
			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				//SUT
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, pubDecorator, settings));
				XHTMLWriter.Flush();
				const string senseNumberOne = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense' and preceding-sibling::span[@class='sensenumber' and text()='1']]//span[@lang='en' and text()='gloss']";
				const string senseNumberTwo = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense' and preceding-sibling::span[@class='sensenumber' and text()='2']]//span[@lang='en' and text()='second gloss']";
				const string subSensesNumberTwoOne = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense' and preceding-sibling::span[@class='sensenumber' and text()='1']]//span[@lang='en' and text()='second gloss2.1']";
				const string subSenseNumberTwoTwo = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense' and preceding-sibling::span[@class='sensenumber' and text()='2']]//span[@lang='en' and text()='second gloss2.2']";
				//This assert is dependent on the specific entry data created in CreateInterestingLexEntry
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(senseNumberOne, 1);
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(senseNumberTwo, 1);
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(subSensesNumberTwoOne, 1);
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(subSenseNumberTwoTwo, 1);
			}
		}


		[Test]
		public void GenerateXHTMLForEntry_SingleSenseGetsNumberWithNumberEvenOneSenseOption()
		{
			var pubDecorator = new DictionaryPublicationDecorator(Cache,
																					(ISilDataAccessManaged)Cache.MainCacheAccessor,
																					Cache.ServiceLocator.GetInstance<Virtuals>().LexDbEntries);
			var wsOpts = new DictionaryNodeWritingSystemOptions
			{
				Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>
				{
					new DictionaryNodeListOptions.DictionaryNodeOption { Id = "en", IsEnabled = true }
				}
			};
			var glossNode = new ConfigurableDictionaryNode { FieldDescription = "Gloss", DictionaryNodeOptions = wsOpts, IsEnabled = true };
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "Senses",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions { NumberEvenASingleSense = true, NumberingStyle = "%d" },
				Children = new List<ConfigurableDictionaryNode> { glossNode },
				IsEnabled = true
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { sensesNode },
				IsEnabled = true
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });
			var testEntry = CreateInterestingLexEntry(Cache);

			using(var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				// SUT
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, pubDecorator, settings));
				XHTMLWriter.Flush();
				const string senseNumberOne = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense' and preceding-sibling::span[@class='sensenumber' and text()='1']]//span[@lang='en' and text()='gloss']";
				// This assert is dependent on the specific entry data created in CreateInterestingLexEntry
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(senseNumberOne, 1);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_SenseContentWithGuid()
		{
			var pubDecorator = new DictionaryPublicationDecorator(Cache,
																					(ISilDataAccessManaged)Cache.MainCacheAccessor,
																					Cache.ServiceLocator.GetInstance<Virtuals>().LexDbEntries);
			var wsOpts = new DictionaryNodeWritingSystemOptions
			{
				Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>
				{
					new DictionaryNodeListOptions.DictionaryNodeOption { Id = "en", IsEnabled = true }
				}
			};
			var glossNode = new ConfigurableDictionaryNode { FieldDescription = "Gloss", DictionaryNodeOptions = wsOpts, IsEnabled = true };
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "Senses",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions(),
				Children = new List<ConfigurableDictionaryNode> { glossNode },
				IsEnabled = true
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { sensesNode },
				IsEnabled = true
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });
			var testEntry = CreateInterestingLexEntry(Cache);

			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				// SUT
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, pubDecorator, settings));
				XHTMLWriter.Flush();
				const string senseEntryGuid = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense' and @entryguid]";
				// This assert is dependent on the specific entry data created in CreateInterestingLexEntry
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(senseEntryGuid, 1);
			}
		}

		public void GenerateXHTMLForEntry_ExampleAndTranslationAreGenerated()
		{
			var translationNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Translation", IsEnabled = true, DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" })
			};
			var translationsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "TranslationsOC", CSSClassNameOverride = "translations", IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { translationNode }
			};
			var exampleNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Example", IsEnabled = true, DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};
			var examplesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ExamplesOS", CSSClassNameOverride = "examples", IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { exampleNode, translationsNode }
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS", CSSClassNameOverride = "senses", IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { examplesNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry", IsEnabled = true, Children = new List<ConfigurableDictionaryNode> { sensesNode }
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });

			const string example = "Example Sentence On Entry";
			const string translation = "Translation of the Example";
			var testEntry = CreateInterestingLexEntry(Cache);
			AddExampleToSense(testEntry.SensesOS[0], example, translation);

			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				//SUT
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, null, settings));
				XHTMLWriter.Flush();
				const string xpathThruExample = xpathThruSense + "/span[@class='examples']/span[@class='example']";
				var oneSenseWithExample = string.Format(xpathThruExample + "/span[@lang='fr' and text()='{0}']", example);
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(oneSenseWithExample, 1);
				var oneExampleSentenceTranslation = string.Format(
					xpathThruExample + "/span[@class='translations']/span[@class='translation']/span[@lang='en' and text()='{0}']", translation);
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(oneExampleSentenceTranslation, 1);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_ExampleSentenceAndTranslationAreGenerated()
		{
			var translationNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Translation", IsEnabled = true, DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" })
			};
			var translationsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "TranslationsOC", CSSClassNameOverride = "translations", IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { translationNode }
			};
			var exampleNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Example", IsEnabled = true, DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};
			var examplesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ExampleSentences", IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { exampleNode, translationsNode }
			};
			var otherRcfsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ComplexFormsNotSubentries", IsEnabled = true, Children = new List<ConfigurableDictionaryNode> { examplesNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry", IsEnabled = true, Children = new List<ConfigurableDictionaryNode> { otherRcfsNode }
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });

			const string example = "Example Sentence On Variant Form";
			const string translation = "Translation of the Sentence";
			var mainEntry = CreateInterestingLexEntry(Cache);
			var minorEntry = CreateInterestingLexEntry(Cache);
			CreateComplexForm(mainEntry, minorEntry, false);
			AddExampleToSense(minorEntry.SensesOS[0], example, translation);

			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				//SUT
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(mainEntry, mainEntryNode, null, settings));
				XHTMLWriter.Flush();
				const string xpathThruExampleSentence = "/div[@class='lexentry']/span[@class='complexformsnotsubentries']/span[@class='complexformsnotsubentrie']/span[@class='examplesentences']/span[@class='examplesentence']";
				var oneSenseWithExample = string.Format(xpathThruExampleSentence + "//span[@lang='fr' and text()='{0}']", example);
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(oneSenseWithExample, 1);
				var oneExampleSentenceTranslation = string.Format(
					xpathThruExampleSentence + "/span[@class='translations']/span[@class='translation']//span[@lang='en' and text()='{0}']", translation);
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(oneExampleSentenceTranslation, 1);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_EnvironmentsAndAllomorphsAreGenerated()
		{
			var stringRepNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "StringRepresentation", IsEnabled = true,
				DictionaryNodeOptions = GetWsOptionsForLanguages(new [] { "en" })
			};
			var environmentsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "AllomorphEnvironments", IsEnabled = true, Children = new List<ConfigurableDictionaryNode> { stringRepNode }
			};
			var formNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Form", IsEnabled = true,
				DictionaryNodeOptions = GetWsOptionsForLanguages(new [] { "fr" })
			};
			var allomorphsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "AlternateFormsOS", IsEnabled = true, Children = new List<ConfigurableDictionaryNode> { formNode, environmentsNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry", IsEnabled = true, Children = new List<ConfigurableDictionaryNode> { allomorphsNode }
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });

			var mainEntry = CreateInterestingLexEntry(Cache);
			AddAllomorphToEntry(mainEntry);

			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				//SUT
				ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(mainEntry, mainEntryNode, null, settings);
				XHTMLWriter.Flush();
				const string xPathThruAllomorph = "/div[@class='lexentry']/span[@class='alternateformsos']/span[@class='alternateformso']";
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(
					xPathThruAllomorph + "/span[@class='form']/span[@lang='fr' and text()='Allomorph']", 1);
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(xPathThruAllomorph +
					"/span[@class='allomorphenvironments']/span[@class='allomorphenvironment']/span[@class='stringrepresentation']/span[@lang='en' and text()='phoneyEnv']", 1);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_ReferencedComplexFormsIncludesSubentriesAndOtherReferencedComplexForms()
		{
			var complexFormNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwningEntry", SubField = "MLHeadWord", IsEnabled = true,
				DictionaryNodeOptions = GetWsOptionsForLanguages(new [] { "fr" })
			};
			var rcfsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VisibleComplexFormBackRefs", IsEnabled = true, Children = new List<ConfigurableDictionaryNode> { complexFormNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry", IsEnabled = true, Children = new List<ConfigurableDictionaryNode> { rcfsNode }
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });

			var mainEntry = CreateInterestingLexEntry(Cache);
			var otherReferencedComplexForm = CreateInterestingLexEntry(Cache);
			var subentry = CreateInterestingLexEntry(Cache);
			CreateComplexForm(mainEntry, subentry, true);
			CreateComplexForm(mainEntry, otherReferencedComplexForm, false);

			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				//SUT
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(mainEntry, mainEntryNode, null, settings));
				XHTMLWriter.Flush();
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(
					"/div[@class='lexentry']/span[@class='visiblecomplexformbackrefs']/span[@class='visiblecomplexformbackref']//span[@lang='fr']", 4);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_GeneratesLinksForReferencedForms()
		{
			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "HeadWord", CSSClassNameOverride = "headword", IsEnabled = true,
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" })
			};
			var refNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ConfigReferencedEntries", IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { headwordNode },
				CSSClassNameOverride = "referencedentries"
			};
			var variantsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VisibleVariantEntryRefs", IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { refNode }
			};
			var complexFormNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwningEntry", SubField = "MLHeadWord", CSSClassNameOverride = "headword", IsEnabled = true,
				DictionaryNodeOptions = GetWsOptionsForLanguages(new [] { "fr" })
			};
			var rcfsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VisibleComplexFormBackRefs", IsEnabled = true, Children = new List<ConfigurableDictionaryNode> { complexFormNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry", IsEnabled = true, Children = new List<ConfigurableDictionaryNode> { variantsNode, rcfsNode }
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });

			var mainEntry = CreateInterestingLexEntry(Cache);
			var variantForm = CreateInterestingLexEntry(Cache);
			var subentry = CreateInterestingLexEntry(Cache);
			CreateVariantForm(variantForm, mainEntry);
			CreateComplexForm(mainEntry, subentry, true);

			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				//SUT
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(mainEntry, mainEntryNode, null, settings));
				XHTMLWriter.Flush();
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(
					"//span[@class='visiblevariantentryrefs']/span[@class='visiblevariantentryref']/span[@class='referencedentries']/span[@class='referencedentrie']/span[@class='headword']/a[@href]/span[@lang='en']", 2);
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(
					"/div[@class='lexentry']/span[@class='visiblecomplexformbackrefs']/span[@class='visiblecomplexformbackref']/span[@class='headword']/a[@href]/span[@lang='fr']", 2);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_GeneratesLinksForCrossReferences()
		{
			var mainEntry = CreateInterestingLexEntry(Cache);
			var referencedEntry = CreateInterestingLexEntry(Cache);
			const string refTypeName = "TestRefType";
			CreateLexicalReference(mainEntry, referencedEntry, refTypeName);
			var refType = Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.First(poss => poss.Name.BestAnalysisAlternative.Text == refTypeName);
			Assert.IsNotNull(refType);

			var formNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "HeadWord", CSSClassNameOverride = "headword", IsEnabled = true,
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};
			var targetsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ConfigTargets", IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { formNode }
			};
			var crossReferencesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MinimalLexReferences", IsEnabled = true,
				DictionaryNodeOptions = new DictionaryNodeListOptions
				{
					ListId = DictionaryNodeListOptions.ListIds.Entry,
					Options = DictionaryDetailsControllerTests.ListOfEnabledDNOsFromStrings(new [] { refType.Guid.ToString() })
				},
				Children = new List<ConfigurableDictionaryNode> { targetsNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry", IsEnabled = true, Children = new List<ConfigurableDictionaryNode> { crossReferencesNode }
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });

			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				//SUT
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(mainEntry, mainEntryNode, null, settings));
				XHTMLWriter.Flush();
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(
					"//span[@class='minimallexreferences']/span[@class='minimallexreference']/span[@class='configtargets']/span[@class='configtarget']/span[@class='headword']/a[@href]/span[@lang='fr']", 2);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_GeneratesCrossReferencesOnUnCheckConfigTargets()
		{
			var mainEntry = CreateInterestingLexEntry(Cache);
			var referencedEntry = CreateInterestingLexEntry(Cache);
			const string refTypeName = "TestRefType";
			CreateLexicalReference(mainEntry, referencedEntry, refTypeName);
			var refType = Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.First(poss => poss.Name.BestAnalysisAlternative.Text == refTypeName);
			Assert.IsNotNull(refType);

			var formNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "HeadWord",
				CSSClassNameOverride = "headword",
				IsEnabled = true,
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};
			var targetsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ConfigTargets",
				IsEnabled = false,
				Children = new List<ConfigurableDictionaryNode> { formNode }
			};
			var crossReferencesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MinimalLexReferences",
				IsEnabled = true,
				DictionaryNodeOptions = new DictionaryNodeListOptions
				{
					ListId = DictionaryNodeListOptions.ListIds.Entry,
					Options = DictionaryDetailsControllerTests.ListOfEnabledDNOsFromStrings(new[] { refType.Guid.ToString() })
				},
				Children = new List<ConfigurableDictionaryNode> { targetsNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { crossReferencesNode }
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });

			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				//SUT-
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(mainEntry, mainEntryNode, null, settings));
				XHTMLWriter.Flush();
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasNoMatchForXpath(
					"//span[@class='minimallexreferences']/span[@class='minimallexreference']/span[@class='configtargets']");
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_GeneratesForwardNameForSymmetricCrossReferences()
		{
			var mainEntry = CreateInterestingLexEntry(Cache);
			var referencedEntry = CreateInterestingLexEntry(Cache);
			const string refTypeName = "TestRefType";
			CreateLexicalReference(mainEntry, referencedEntry, refTypeName);
			var refType = Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.First(poss => poss.Name.BestAnalysisAlternative.Text == refTypeName);
			Assert.IsNotNull(refType);

			var nameNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwnerType", SubField = "Name", IsEnabled = true,
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" })
			};
			var crossReferencesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MinimalLexReferences", IsEnabled = true,
				DictionaryNodeOptions = new DictionaryNodeListOptions
				{
					ListId = DictionaryNodeListOptions.ListIds.Entry,
					Options = DictionaryDetailsControllerTests.ListOfEnabledDNOsFromStrings(new [] { refType.Guid.ToString() })
				},
				Children = new List<ConfigurableDictionaryNode> { nameNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry", IsEnabled = true, Children = new List<ConfigurableDictionaryNode> { crossReferencesNode }
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });

			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				//SUT
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(referencedEntry, mainEntryNode, null, settings));
				XHTMLWriter.Flush();
				var fwdNameXpath = string.Format(
					"//span[@class='minimallexreferences']/span[@class='minimallexreference']/span[@class='ownertype_name']/span[@lang='en' and text()='{0}']", refTypeName);
				const string anyNameXpath =
					"//span[@class='minimallexreferences']/span[@class='minimallexreference']/span[@class='ownertype_name']/span[@lang='en']";
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(fwdNameXpath, 1);
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(anyNameXpath, 1); // ensure there are no spurious names
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_GeneratesForwardNameForForwardCrossReferences()
		{
			var mainEntry = CreateInterestingLexEntry(Cache);
			var referencedEntry = CreateInterestingLexEntry(Cache);
			const string refTypeName = "TestRefType";
			const string refTypeRevName = "epyTfeRtseT";
			CreateLexicalReference(mainEntry, referencedEntry, refTypeName, refTypeRevName);
			var refType = Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.First(poss => poss.Name.BestAnalysisAlternative.Text == refTypeName);
			Assert.IsNotNull(refType);

			var nameNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwnerType", SubField = "Name", IsEnabled = true,
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" })
			};
			var crossReferencesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MinimalLexReferences", IsEnabled = true,
				DictionaryNodeOptions = new DictionaryNodeListOptions
				{
					ListId = DictionaryNodeListOptions.ListIds.Entry,
					Options = DictionaryDetailsControllerTests.ListOfEnabledDNOsFromStrings(new [] { refType.Guid + ":f" })
				},
				Children = new List<ConfigurableDictionaryNode> { nameNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry", IsEnabled = true, Children = new List<ConfigurableDictionaryNode> { crossReferencesNode }
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });

			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				//SUT
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(mainEntry, mainEntryNode, null, settings));
				XHTMLWriter.Flush();
				var fwdNameXpath = string.Format(
					"//span[@class='minimallexreferences']/span[@class='minimallexreference']/span[@class='ownertype_name']/span[@lang='en' and text()='{0}']", refTypeName);
				var revNameXpath = string.Format(
					"//span[@class='minimallexreferences']/span[@class='minimallexreference']/span[@class='ownertype_name']/span[@lang='en' and text()='{0}']", refTypeRevName);
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(fwdNameXpath, 1);
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasNoMatchForXpath(revNameXpath);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_GeneratesReverseNameForReverseCrossReferences()
		{
			var mainEntry = CreateInterestingLexEntry(Cache);
			var referencedEntry = CreateInterestingLexEntry(Cache);
			const string refTypeName = "TestRefType";
			const string refTypeRevName = "sURsyoT";
			CreateLexicalReference(mainEntry, referencedEntry, refTypeName, refTypeRevName);
			var refType = Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.First(poss => poss.Name.BestAnalysisAlternative.Text == refTypeName);
			Assert.IsNotNull(refType);

			var nameNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwnerType", SubField = "Name", IsEnabled = true,
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" })
			};
			var crossReferencesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MinimalLexReferences", IsEnabled = true,
				DictionaryNodeOptions = new DictionaryNodeListOptions
				{
					ListId = DictionaryNodeListOptions.ListIds.Entry,
					Options = DictionaryDetailsControllerTests.ListOfEnabledDNOsFromStrings(new [] { refType.Guid + ":r" })
				},
				Children = new List<ConfigurableDictionaryNode> { nameNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry", IsEnabled = true, Children = new List<ConfigurableDictionaryNode> { crossReferencesNode }
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });

			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				//SUT
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(referencedEntry, mainEntryNode, null, settings));
				XHTMLWriter.Flush();
				var fwdNameXpath = string.Format(
					"//span[@class='minimallexreferences']/span[@class='minimallexreference']/span[@class='ownertype_name']/span[@lang='en' and text()='{0}']", refTypeName);
				var revNameXpath = string.Format(
					"//span[@class='minimallexreferences']/span[@class='minimallexreference']/span[@class='ownertype_name']/span[@lang='en' and text()='{0}']", refTypeRevName);
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasNoMatchForXpath(fwdNameXpath);
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(revNameXpath, 1);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_GeneratesForwardNameForForwardLexicalRelations()
		{
			var mainEntry = CreateInterestingLexEntry(Cache);
			var referencedEntry = CreateInterestingLexEntry(Cache);
			const string refTypeName = "TestRefType";
			const string refTypeRevName = "sURsyoT";
			CreateLexicalReference(mainEntry.SensesOS.First(), referencedEntry, refTypeName, refTypeRevName);
			var refType = Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.First(poss => poss.Name.BestAnalysisAlternative.Text == refTypeName);
			Assert.IsNotNull(refType);

			var nameNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwnerType", SubField = "Name", IsEnabled = true,
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" })
			};
			var referencesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexSenseReferences",
				IsEnabled = true,
				DictionaryNodeOptions = new DictionaryNodeListOptions
				{
					ListId = DictionaryNodeListOptions.ListIds.Sense,
					Options = DictionaryDetailsControllerTests.ListOfEnabledDNOsFromStrings(new [] { refType.Guid + ":f" })
				},
				Children = new List<ConfigurableDictionaryNode> { nameNode }
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS", IsEnabled = true, Children = new List<ConfigurableDictionaryNode> { referencesNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry", IsEnabled = true, Children = new List<ConfigurableDictionaryNode> { sensesNode }
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });

			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				//SUT
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(mainEntry, mainEntryNode, null, settings));
				XHTMLWriter.Flush();
				var fwdNameXpath = string.Format(
					"//span[@class='lexsensereferences']/span[@class='lexsensereference']/span[@class='ownertype_name']/span[@lang='en' and text()='{0}']", refTypeName);
				var revNameXpath = string.Format(
					"//span[@class='lexsensereferences']/span[@class='lexsensereference']/span[@class='ownertype_name']/span[@lang='en' and text()='{0}']", refTypeRevName);
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(fwdNameXpath, 1);
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasNoMatchForXpath(revNameXpath);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_GeneratesReverseNameForReverseLexicalRelations()
		{
			var mainEntry = CreateInterestingLexEntry(Cache);
			var referencedEntry = CreateInterestingLexEntry(Cache);
			const string refTypeName = "TestRefType";
			const string refTypeRevName = "sURsyoT";
			CreateLexicalReference(mainEntry, referencedEntry.SensesOS.First(), refTypeName, refTypeRevName);
			var refType = Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.First(poss => poss.Name.BestAnalysisAlternative.Text == refTypeName);
			Assert.IsNotNull(refType);

			var nameNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwnerType", SubField = "Name", IsEnabled = true,
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" })
			};
			var referencesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexSenseReferences",
				IsEnabled = true,
				DictionaryNodeOptions = new DictionaryNodeListOptions
				{
					ListId = DictionaryNodeListOptions.ListIds.Sense,
					Options = DictionaryDetailsControllerTests.ListOfEnabledDNOsFromStrings(new [] { refType.Guid + ":r" })
				},
				Children = new List<ConfigurableDictionaryNode> { nameNode }
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS", IsEnabled = true, Children = new List<ConfigurableDictionaryNode> { referencesNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry", IsEnabled = true, Children = new List<ConfigurableDictionaryNode> { sensesNode }
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });

			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				//SUT
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(referencedEntry, mainEntryNode, null, settings));
				XHTMLWriter.Flush();
				var fwdNameXpath = string.Format(
					"//span[@class='lexsensereferences']/span[@class='lexsensereference']/span[@class='ownertype_name']/span[@lang='en' and text()='{0}']", refTypeName);
				var revNameXpath = string.Format(
					"//span[@class='lexsensereferences']/span[@class='lexsensereference']/span[@class='ownertype_name']/span[@lang='en' and text()='{0}']", refTypeRevName);
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasNoMatchForXpath(fwdNameXpath);
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(revNameXpath, 1);
			}
		}


		[Test]
		public void GenerateXHTMLForEntry_GeneratesAsymmetricRelationsProperly()
		{
			var bodyEntry = CreateInterestingLexEntry(Cache);
			var firstWord = "corps";
			AddHeadwordToEntry(bodyEntry, firstWord, m_wsFr, Cache);
			bodyEntry.SensesOS.First().Gloss.set_String(m_wsEn, Cache.TsStrFactory.MakeString("body", m_wsEn));
			var armEntry = CreateInterestingLexEntry(Cache);
			var secondWord = "bras";
			AddHeadwordToEntry(armEntry, secondWord, m_wsFr, Cache);
			armEntry.SensesOS.First().Gloss.set_String(m_wsEn, Cache.TsStrFactory.MakeString("arm", m_wsEn));
			var legEntry = CreateInterestingLexEntry(Cache);
			var thirdWord = "jambe";
			AddHeadwordToEntry(legEntry, thirdWord, m_wsFr, Cache);
			legEntry.SensesOS.First().Gloss.set_String(m_wsEn, Cache.TsStrFactory.MakeString("leg", m_wsEn));
			const string refTypeName = "Part";
			const string refTypeRevName = "Whole";
			CreateLexicalReference(bodyEntry, armEntry.SensesOS.First(), legEntry.SensesOS.First(), refTypeName, refTypeRevName);
			var refType = Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.First(poss => poss.Name.BestAnalysisAlternative.Text == refTypeName);
			Assert.IsNotNull(refType);

			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "HeadWord",
				IsEnabled = true,
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};
			var glossNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Gloss",
				IsEnabled = true,
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" })
			};
			var refListNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ConfigTargets",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { headwordNode, glossNode }
			};
			var nameNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwnerType", SubField = "Name", IsEnabled = true,
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" })
			};
			var referencesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexSenseReferences",
				IsEnabled = true,
				DictionaryNodeOptions = new DictionaryNodeListOptions
				{
					ListId = DictionaryNodeListOptions.ListIds.Sense,
					Options = DictionaryDetailsControllerTests.ListOfEnabledDNOsFromStrings(new [] { refType.Guid + ":r" })
				},
				Children = new List<ConfigurableDictionaryNode> { nameNode, refListNode }
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS", IsEnabled = true, Children = new List<ConfigurableDictionaryNode> { referencesNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry", IsEnabled = true, Children = new List<ConfigurableDictionaryNode> { sensesNode }
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });

			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				//SUT
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(armEntry, mainEntryNode, null, settings));
				XHTMLWriter.Flush();
				var output = XHTMLStringBuilder.ToString();
				var fwdNameXpath = string.Format(
					"//span[@class='lexsensereferences']/span[@class='lexsensereference']/span[@class='ownertype_name']/span[@lang='en' and text()='{0}']", refTypeName);
				AssertThatXmlIn.String(output).HasNoMatchForXpath(fwdNameXpath);
				var revNameXpath = string.Format(
					"//span[@class='lexsensereferences']/span[@class='lexsensereference']/span[@class='ownertype_name']/span[@lang='en' and text()='{0}']", refTypeRevName);
				AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(revNameXpath, 1);
				var badTarget1 = "//span[@class='lexsensereferences']/span[@class='lexsensereference']/span[@class='configtargets']/span[@class='configtarget']/span[@class='gloss']";
				AssertThatXmlIn.String(output).HasNoMatchForXpath(badTarget1);
				var badTarget2 = string.Format(
					"//span[@class='lexsensereferences']/span[@class='lexsensereference']/span[@class='configtargets']/span[@class='configtarget']/span[@class='headword']/span[@lang='fr' and text()='{0}']", secondWord);
				AssertThatXmlIn.String(output).HasNoMatchForXpath(badTarget2);
				var badTarget3 = string.Format(
					"//span[@class='lexsensereferences']/span[@class='lexsensereference']/span[@class='configtargets']/span[@class='configtarget']/span[@class='headword']/span[@lang='fr' and text()='{0}']", thirdWord);
				AssertThatXmlIn.String(output).HasNoMatchForXpath(badTarget3);
				var goodTarget = string.Format(
					"//span[@class='lexsensereferences']/span[@class='lexsensereference']/span[@class='configtargets']/span[@class='configtarget']/span[@class='headword']/span[@lang='fr' and text()='{0}']", firstWord);
				AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(goodTarget, 1);
			}
		}

		[Test]
		public void IsListItemSelectedForExport_Variant_SelectedItemReturnsTrue()
		{
			var mainEntry = CreateInterestingLexEntry(Cache);
			var variantForm = CreateInterestingLexEntry(Cache);
			CreateVariantForm(mainEntry, variantForm);
			var crazyVariantPoss = Cache.LangProject.LexDbOA.VariantEntryTypesOA.PossibilitiesOS.First(variant => variant.Name.BestAnalysisAlternative.Text == TestVariantName);

			var variantsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VariantFormEntryBackRefs",
				IsEnabled = true,
				DictionaryNodeOptions = GetListOptionsForItems(DictionaryNodeListOptions.ListIds.Variant, new [] { crazyVariantPoss })
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { variantsNode }
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });

			//SUT
			Assert.IsTrue(ConfiguredXHTMLGenerator.IsListItemSelectedForExport(variantsNode, variantForm.VisibleVariantEntryRefs.First(), variantForm));
		}

		/// <summary>
		/// Test the new section of ConfiguredXHTMLGenerator.IsListItemSelectedForExport() that
		/// handles Minor Entry -> Variant Of
		/// </summary>
		[Test]
		public void IsListItemSelectedForExport_MinorVariant_SelectedItemReturnsTrue()
		{
			var mainEntry = CreateInterestingLexEntry(Cache);
			var minorEntry = CreateInterestingLexEntry(Cache);
			CreateVariantForm(mainEntry, minorEntry);
			var crazyVariantPoss = Cache.LangProject.LexDbOA.VariantEntryTypesOA.PossibilitiesOS.First(variant => variant.Name.BestAnalysisAlternative.Text == TestVariantName);

			var minorEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				CSSClassNameOverride = "minorentryvariant",
				IsEnabled = true,
				DictionaryNodeOptions = GetListOptionsForItems(DictionaryNodeListOptions.ListIds.Variant, new[] { crazyVariantPoss })
			};

			//SUT
			Assert.IsTrue(ConfiguredXHTMLGenerator.IsListItemSelectedForExport(minorEntryNode, minorEntry, null));
		}

		[Test]
		public void IsListItemSelectedForExport_Variant_UnselectedItemReturnsFalse()
		{
			var formNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwningEntry",
				SubField = "MLHeadWord",
				IsEnabled = true,
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};
			var mainEntry = CreateInterestingLexEntry(Cache);
			var variantForm = CreateInterestingLexEntry(Cache);
			CreateVariantForm (mainEntry, variantForm);
			var notCrazyVariant = Cache.LangProject.LexDbOA.VariantEntryTypesOA.PossibilitiesOS.FirstOrDefault(variant => variant.Name.BestAnalysisAlternative.Text != TestVariantName);
			Assert.IsNotNull(notCrazyVariant);
			var rcfsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VariantFormEntryBackRefs",
				IsEnabled = true,
				DictionaryNodeOptions = GetListOptionsForItems(DictionaryNodeListOptions.ListIds.Variant, new [] { notCrazyVariant }),
				Children = new List<ConfigurableDictionaryNode> { formNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry", IsEnabled = true, Children = new List<ConfigurableDictionaryNode> { rcfsNode }
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });

			//SUT
			Assert.IsFalse(ConfiguredXHTMLGenerator.IsListItemSelectedForExport(rcfsNode, variantForm.VisibleVariantEntryRefs.First(), variantForm));
		}

		[Test]
		public void IsListItemSelectedForExport_Complex_SelectedItemReturnsTrue()
		{
			var mainEntry = CreateInterestingLexEntry(Cache);
			var complexForm = CreateInterestingLexEntry(Cache);
			var complexFormRef = CreateComplexForm(mainEntry, complexForm, false);
			var complexRefName = complexFormRef.ComplexEntryTypesRS[0].Name.BestAnalysisAlternative.Text;
			var complexTypePoss = Cache.LangProject.LexDbOA.ComplexEntryTypesOA.PossibilitiesOS.First(complex => complex.Name.BestAnalysisAlternative.Text == complexRefName);

			var variantsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VisibleComplexFormBackRefs",
				IsEnabled = true,
				DictionaryNodeOptions = GetListOptionsForItems(DictionaryNodeListOptions.ListIds.Complex, new [] { complexTypePoss })
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { variantsNode }
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });

			//SUT
			Assert.IsTrue(ConfiguredXHTMLGenerator.IsListItemSelectedForExport(variantsNode, mainEntry.VisibleComplexFormBackRefs.First(), mainEntry));
		}

		[Test]
		public void IsListItemSelectedForExport_Complex_SubentrySelectedItemReturnsTrue()
		{
			var mainEntry = CreateInterestingLexEntry(Cache);
			var complexForm = CreateInterestingLexEntry(Cache);
			var complexFormRef = CreateComplexForm(mainEntry, complexForm, true);
			var complexRefName = complexFormRef.ComplexEntryTypesRS[0].Name.BestAnalysisAlternative.Text;
			var complexTypePoss = Cache.LangProject.LexDbOA.ComplexEntryTypesOA.PossibilitiesOS.First(complex => complex.Name.BestAnalysisAlternative.Text == complexRefName);

			var variantsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Subentries",
				IsEnabled = true,
				DictionaryNodeOptions = GetListOptionsForItems(DictionaryNodeListOptions.ListIds.Complex, new [] { complexTypePoss })
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { variantsNode }
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });

			//SUT
			Assert.IsTrue(ConfiguredXHTMLGenerator.IsListItemSelectedForExport(variantsNode, mainEntry.Subentries.First(), mainEntry));
		}

		[Test]
		public void IsListItemSelectedForExport_Complex_UnselectedItemReturnsFalse()
		{
			var formNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwningEntry",
				SubField = "MLHeadWord",
				IsEnabled = true,
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};
			var mainEntry = CreateInterestingLexEntry(Cache);
			var complexForm = CreateInterestingLexEntry(Cache);
			var complexFormRef = CreateComplexForm(mainEntry, complexForm, false);
			var complexRefName = complexFormRef.ComplexEntryTypesRS[0].Name.BestAnalysisAlternative.Text;
			var notComplexTypePoss = Cache.LangProject.LexDbOA.ComplexEntryTypesOA.PossibilitiesOS.First(complex => complex.Name.BestAnalysisAlternative.Text != complexRefName);
			Assert.IsNotNull(notComplexTypePoss);
			var rcfsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VisibleComplexFormBackRefs",
				IsEnabled = true,
				DictionaryNodeOptions = GetListOptionsForItems(DictionaryNodeListOptions.ListIds.Complex, new [] { notComplexTypePoss }),
				Children = new List<ConfigurableDictionaryNode> { formNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { rcfsNode }
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });

			//SUT
			Assert.IsFalse(ConfiguredXHTMLGenerator.IsListItemSelectedForExport(rcfsNode, mainEntry.VisibleComplexFormBackRefs.First(), mainEntry));
		}

		[Test]
		public void IsListItemSelectedForExport_Entry_SelectedItemReturnsTrue()
		{
			var formNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwningEntry",
				SubField = "MLHeadWord",
				IsEnabled = true,
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};
			var mainEntry = CreateInterestingLexEntry(Cache);
			var referencedEntry = CreateInterestingLexEntry(Cache);
			const string refTypeName = "TestRefType";
			CreateLexicalReference(mainEntry, referencedEntry, refTypeName);
			var notComplexTypePoss = Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.First(refType => refType.Name.BestAnalysisAlternative.Text == refTypeName);
			Assert.IsNotNull(notComplexTypePoss);
			var entryReferenceNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MinimalLexReferences",
				IsEnabled = true,
				DictionaryNodeOptions = GetListOptionsForItems(DictionaryNodeListOptions.ListIds.Entry, new [] { notComplexTypePoss }),
				Children = new List<ConfigurableDictionaryNode> { formNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { entryReferenceNode }
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });
			Assert.IsTrue(ConfiguredXHTMLGenerator.IsListItemSelectedForExport(entryReferenceNode, mainEntry.MinimalLexReferences.First(), mainEntry));
			Assert.IsTrue(ConfiguredXHTMLGenerator.IsListItemSelectedForExport(entryReferenceNode, referencedEntry.MinimalLexReferences.First(), referencedEntry));
		}

		/// <summary>
		/// Some relationships have :r or :f added to the id in the list to indicate which
		/// direction of the relationship the user wants to display. This test fakes a selected reverse
		/// list item and verifies it are identified as selected.
		/// </summary>
		[Test]
		public void IsListItemSelectedForExport_Entry_SelectedReverseRelationshipReturnsTrue()
		{
			var formNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwningEntry",
				SubField = "MLHeadWord",
				IsEnabled = true,
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};
			var mainEntry = CreateInterestingLexEntry(Cache);
			var referencedEntry = CreateInterestingLexEntry(Cache);
			const string refTypeName = "TestRefType";
			CreateLexicalReference(mainEntry, referencedEntry, refTypeName, "ReverseName");
			var notComplexTypePoss = Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.First(refType => refType.Name.BestAnalysisAlternative.Text == refTypeName);
			Assert.IsNotNull(notComplexTypePoss);
			var entryReferenceNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MinimalLexReferences",
				IsEnabled = true,
				DictionaryNodeOptions = new DictionaryNodeListOptions
				{
					ListId = DictionaryNodeListOptions.ListIds.Entry,
					Options = DictionaryDetailsControllerTests.ListOfEnabledDNOsFromStrings(new [] { notComplexTypePoss.Guid + ":r" })
				},
				Children = new List<ConfigurableDictionaryNode> { formNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { entryReferenceNode }
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });
			Assert.IsFalse(ConfiguredXHTMLGenerator.IsListItemSelectedForExport(entryReferenceNode, mainEntry.MinimalLexReferences.First(), mainEntry));
			Assert.IsTrue(ConfiguredXHTMLGenerator.IsListItemSelectedForExport(entryReferenceNode, referencedEntry.MinimalLexReferences.First(), referencedEntry));
		}

		/// <summary>
		/// Some relationships have :r or :f added to the id in the list to indicate which
		/// direction of the relationship the user wants to display. This test fakes a selected forward
		/// list item and verifies it are identified as selected.
		/// </summary>
		[Test]
		public void IsListItemSelectedForExport_Entry_SelectedForwardRelationshipReturnsTrue()
		{
			var formNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwningEntry",
				SubField = "MLHeadWord",
				IsEnabled = true,
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};
			var mainEntry = CreateInterestingLexEntry(Cache);
			var referencedEntry = CreateInterestingLexEntry(Cache);
			const string refTypeName = "TestRefType";
			CreateLexicalReference(mainEntry, referencedEntry, refTypeName, "ReverseName");
			var notComplexTypePoss = Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.First(refType => refType.Name.BestAnalysisAlternative.Text == refTypeName);
			Assert.IsNotNull(notComplexTypePoss);
			var entryReferenceNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MinimalLexReferences",
				IsEnabled = true,
				DictionaryNodeOptions = new DictionaryNodeListOptions
				{
					ListId = DictionaryNodeListOptions.ListIds.Entry,
					Options = DictionaryDetailsControllerTests.ListOfEnabledDNOsFromStrings(new [] { notComplexTypePoss.Guid + ":f" })
				},
				Children = new List<ConfigurableDictionaryNode> { formNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { entryReferenceNode }
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });
			Assert.IsTrue(ConfiguredXHTMLGenerator.IsListItemSelectedForExport(entryReferenceNode, mainEntry.MinimalLexReferences.First(), mainEntry));
			Assert.IsFalse(ConfiguredXHTMLGenerator.IsListItemSelectedForExport(entryReferenceNode, referencedEntry.MinimalLexReferences.First(), referencedEntry));
		}

		/// <summary>
		/// Some relationships have :r or :f added to the id in the list to indicate which
		/// direction of the relationship the user wants to display. This test fakes a selected forward
		/// list item and a selected reverse list item and verifies that both are identified as selected.
		/// </summary>
		[Test]
		public void IsListItemSelectedForExport_Entry_SelectedBothDirectionsBothReturnTrue()
		{
			var formNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwningEntry",
				SubField = "MLHeadWord",
				IsEnabled = true,
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};
			var mainEntry = CreateInterestingLexEntry(Cache);
			var referencedEntry = CreateInterestingLexEntry(Cache);
			const string refTypeName = "TestRefType";
			CreateLexicalReference(mainEntry, referencedEntry, refTypeName, "ReverseName");
			var notComplexTypePoss = Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.First(refType => refType.Name.BestAnalysisAlternative.Text == refTypeName);
			Assert.IsNotNull(notComplexTypePoss);
			var entryReferenceNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MinimalLexReferences",
				IsEnabled = true,
				DictionaryNodeOptions = new DictionaryNodeListOptions
				{
					ListId = DictionaryNodeListOptions.ListIds.Entry,
					Options = DictionaryDetailsControllerTests.ListOfEnabledDNOsFromStrings(new [] { notComplexTypePoss.Guid + ":f", notComplexTypePoss.Guid + ":r",})
				},
				Children = new List<ConfigurableDictionaryNode> { formNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { entryReferenceNode }
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });
			Assert.IsTrue(ConfiguredXHTMLGenerator.IsListItemSelectedForExport(entryReferenceNode, mainEntry.MinimalLexReferences.First(), mainEntry));
			Assert.IsTrue(ConfiguredXHTMLGenerator.IsListItemSelectedForExport(entryReferenceNode, referencedEntry.MinimalLexReferences.First(), referencedEntry));
		}

		[Test]
		public void IsListItemSelectedForExport_Entry_UnselectedItemReturnsFalse()
		{
			var formNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwningEntry",
				SubField = "MLHeadWord",
				IsEnabled = true,
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};
			var mainEntry = CreateInterestingLexEntry(Cache);
			var referencedEntry = CreateInterestingLexEntry(Cache);
			// Make an unused LexRefType
			var lrt = Cache.ServiceLocator.GetInstance<ILexRefTypeFactory>().Create();
			if(Cache.LangProject.LexDbOA.ReferencesOA == null)
				Cache.LangProject.LexDbOA.ReferencesOA = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
			Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.Add(lrt);
			lrt.Name.set_String(Cache.DefaultAnalWs, "NotOurTestRefType");

			const string refTypeName = "TestRefType";
			CreateLexicalReference(mainEntry, referencedEntry, refTypeName);
			var notComplexTypePoss = Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.First(refType => refType.Name.BestAnalysisAlternative.Text != refTypeName);
			Assert.IsNotNull(notComplexTypePoss);
			var entryReferenceNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MinimalLexReferences",
				IsEnabled = true,
				DictionaryNodeOptions = GetListOptionsForItems(DictionaryNodeListOptions.ListIds.Entry, new [] { notComplexTypePoss }),
				Children = new List<ConfigurableDictionaryNode> { formNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { entryReferenceNode }
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });
			Assert.IsFalse(ConfiguredXHTMLGenerator.IsListItemSelectedForExport(entryReferenceNode, mainEntry.MinimalLexReferences.First(), mainEntry));
			Assert.IsFalse(ConfiguredXHTMLGenerator.IsListItemSelectedForExport(entryReferenceNode, referencedEntry.MinimalLexReferences.First(), referencedEntry));
		}

		[Test]
		public void IsListItemSelectedForExport_EntryWithNoOptions_Throws()
		{
			var formNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwningEntry",
				SubField = "MLHeadWord",
				IsEnabled = true
			};
			var mainEntry = CreateInterestingLexEntry(Cache);
			var referencedEntry = CreateInterestingLexEntry(Cache);
			const string refTypeName = "TestRefType";
			CreateLexicalReference(mainEntry, referencedEntry, refTypeName);
			var notComplexTypePoss = Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.First(refType => refType.Name.BestAnalysisAlternative.Text == refTypeName);
			Assert.IsNotNull(notComplexTypePoss);
			var entryReferenceNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MinimalLexReferences",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { formNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { entryReferenceNode }
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });
			Assert.Throws<ArgumentException>(() => ConfiguredXHTMLGenerator.IsListItemSelectedForExport(entryReferenceNode, mainEntry.MinimalLexReferences.First(), mainEntry));
		}

		[Test]
		public void GenerateXHTMLForEntry_NoncheckedListItemsAreNotGenerated()
		{
			var formNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwningEntry",
				SubField = "MLHeadWord",
				IsEnabled = true,
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};
			var mainEntry = CreateInterestingLexEntry(Cache);
			var variantForm = CreateInterestingLexEntry(Cache);
			CreateVariantForm (mainEntry, variantForm);
			var notCrazyVariant = Cache.LangProject.LexDbOA.VariantEntryTypesOA.PossibilitiesOS.FirstOrDefault(variant => variant.Name.BestAnalysisAlternative.Text != TestVariantName);
			Assert.IsNotNull(notCrazyVariant);
			var variantsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VariantFormEntryBackRefs",
				IsEnabled = true,
				DictionaryNodeOptions = GetListOptionsForItems(DictionaryNodeListOptions.ListIds.Variant, new [] { notCrazyVariant }),
				Children = new List<ConfigurableDictionaryNode> { formNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry", IsEnabled = true, Children = new List<ConfigurableDictionaryNode> { variantsNode }
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });

			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				//SUT
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(mainEntry, mainEntryNode, null, settings));
				XHTMLWriter.Flush();
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(
					"/div[@class='lexentry']/span[@class='variantformentrybackrefs']/span[@class='variantformentrybackref']/span[@lang='fr']", 0);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_CheckedListItemsAreGenerated()
		{
			var formNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwningEntry",
				SubField = "MLHeadWord",
				IsEnabled = true,
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};
			var mainEntry = CreateInterestingLexEntry(Cache);
			var variantForm = CreateInterestingLexEntry(Cache);
			CreateVariantForm (mainEntry, variantForm);
			var crazyVariant = Cache.LangProject.LexDbOA.VariantEntryTypesOA.PossibilitiesOS.FirstOrDefault(variant => variant.Name.BestAnalysisAlternative.Text == TestVariantName);
			Assert.IsNotNull(crazyVariant);
			var variantsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VariantFormEntryBackRefs",
				IsEnabled = true,
				DictionaryNodeOptions = GetListOptionsForItems(DictionaryNodeListOptions.ListIds.Variant, new [] { crazyVariant }),
				Children = new List<ConfigurableDictionaryNode> { formNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry", IsEnabled = true, Children = new List<ConfigurableDictionaryNode> { variantsNode }
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });

			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				//SUT
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(mainEntry, mainEntryNode, null, settings));
				XHTMLWriter.Flush();
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(
					"/div[@class='lexentry']/span[@class='variantformentrybackrefs']/span[@class='variantformentrybackref']//span[@lang='fr']", 2);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_ReferencedComplexFormsUnderSensesIncludesSubentriesAndOtherReferencedComplexForms()
		{
			var complexFormNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwningEntry", SubField = "MLHeadWord", IsEnabled = true,
				DictionaryNodeOptions = GetWsOptionsForLanguages(new [] { "fr" })
			};
			var rcfsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VisibleComplexFormBackRefs",IsEnabled = true, Children = new List<ConfigurableDictionaryNode> { complexFormNode }
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS", CSSClassNameOverride = "senses", IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { rcfsNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry", IsEnabled = true, Children = new List<ConfigurableDictionaryNode> { sensesNode }
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });

			var mainEntry = CreateInterestingLexEntry(Cache);
			var otherReferencedComplexForm = CreateInterestingLexEntry(Cache);
			var subentry = CreateInterestingLexEntry(Cache);
			CreateComplexForm(mainEntry.SensesOS[0], subentry, true);
			CreateComplexForm(mainEntry.SensesOS[0], otherReferencedComplexForm, false);

			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				//SUT
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(mainEntry, mainEntryNode, null, settings));
				XHTMLWriter.Flush();
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(
					xpathThruSense + "/span[@class='visiblecomplexformbackrefs']/span[@class='visiblecomplexformbackref']//span[@lang='fr']", 4);
			}
		}

		[Test]
		public void GenerateLetterHeaderIfNeeded_GeneratesHeaderIfNoPreviousHeader()
		{
			var entry = CreateInterestingLexEntry(Cache);
			using(var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				// SUT
				string last = null;
				XHTMLWriter.WriteStartElement("TestElement");
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateLetterHeaderIfNeeded(entry, ref last, XHTMLWriter, Cache));
				XHTMLWriter.WriteEndElement();
				XHTMLWriter.Flush();
				const string letterHeaderToMatch = "//div[@class='letHead']/div[@class='letter' and text()='C c']";
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(letterHeaderToMatch, 1);
			}
		}

		[Test]
		public void GenerateLetterHeaderIfNeeded_GeneratesHeaderIfPreviousHeaderDoesNotMatch()
		{
			var entry = CreateInterestingLexEntry(Cache);
			using(var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				// SUT
				var last = "A a";
				XHTMLWriter.WriteStartElement("TestElement");
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateLetterHeaderIfNeeded(entry, ref last, XHTMLWriter, Cache));
				XHTMLWriter.WriteEndElement();
				XHTMLWriter.Flush();
				const string letterHeaderToMatch = "//div[@class='letHead']/div[@class='letter' and text()='C c']";
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(letterHeaderToMatch, 1);
			}
		}

		[Test]
		public void GenerateLetterHeaderIfNeeded_GeneratesNoHeaderIfPreviousHeaderDoesMatch()
		{
			var entry = CreateInterestingLexEntry(Cache);
			using(var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				// SUT
				var last = "A a";
				XHTMLWriter.WriteStartElement("TestElement");
				ConfiguredXHTMLGenerator.GenerateLetterHeaderIfNeeded(entry, ref last, XHTMLWriter, Cache);
				ConfiguredXHTMLGenerator.GenerateLetterHeaderIfNeeded(entry, ref last, XHTMLWriter, Cache);
				XHTMLWriter.WriteEndElement();
				XHTMLWriter.Flush();
				const string letterHeaderToMatch = "//div[@class='letHead']/div[@class='letter' and text()='C c']";
				const string proveOnlyOneHeader = "//div[@class='letHead']/div[@class='letter']";
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(letterHeaderToMatch, 1);
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(proveOnlyOneHeader, 1);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_OneSenseWithSinglePicture()
		{
			var wsOpts = new DictionaryNodeWritingSystemOptions
			{
				Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>
				{
					new DictionaryNodeListOptions.DictionaryNodeOption { Id = "en", IsEnabled = true }
				}
			};
			var captionNode = new ConfigurableDictionaryNode { FieldDescription = "Caption", IsEnabled = true, DictionaryNodeOptions = wsOpts };
			var thumbNailNode = new ConfigurableDictionaryNode { FieldDescription = "PictureFileRA", CSSClassNameOverride = "photo", IsEnabled = true };
			var pictureNode = new ConfigurableDictionaryNode
			{
				DictionaryNodeOptions = new DictionaryNodePictureOptions(),
				FieldDescription = "PicturesOfSenses",
				IsEnabled = true,
				CSSClassNameOverride = "Pictures",
				Children = new List<ConfigurableDictionaryNode> { thumbNailNode, captionNode }
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Senses",
				Label = "Senses",
				IsEnabled = true,
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { sensesNode, pictureNode },
				FieldDescription = "LexEntry",
				IsEnabled = true
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });
			var testEntry = CreateInterestingLexEntry(Cache);
			var sense = testEntry.SensesOS[0];
			var sensePic = Cache.ServiceLocator.GetInstance<ICmPictureFactory>().Create();
			sense.PicturesOS.Add(sensePic);
			var wsEn = Cache.WritingSystemFactory.GetWsFromStr("en");
			sensePic.Caption.set_String(wsEn, Cache.TsStrFactory.MakeString("caption", wsEn));
			var pic = Cache.ServiceLocator.GetInstance<ICmFileFactory>().Create();
			var folder = Cache.ServiceLocator.GetInstance<ICmFolderFactory>().Create();
			Cache.LangProject.MediaOC.Add(folder);
			folder.FilesOC.Add(pic);
			pic.InternalPath = "picture";
			sensePic.PictureFileRA = pic;

			using(var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				//SUT
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, null, settings));
				XHTMLWriter.Flush();
				const string oneSenseWithPicture = "/div[@class='lexentry']/span[@class='pictures']/div[@class='picture']/img[@class='photo' and @id]";
				const string oneSenseWithPictureCaption = "/div[@class='lexentry']/span[@class='pictures']/div[@class='picture']/div[@class='caption']//span[text()='caption']";
				//This assert is dependent on the specific entry data created in CreateInterestingLexEntry
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(oneSenseWithPicture, 1);
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(oneSenseWithPictureCaption, 1);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_PictureWithNonUnicodePathLinksCorrectly()
		{
			var mainEntryNode = CreatePictureModel();
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });
			var testEntry = CreateInterestingLexEntry(Cache);
			var sense = testEntry.SensesOS[0];
			var sensePic = Cache.ServiceLocator.GetInstance<ICmPictureFactory>().Create();
			sense.PicturesOS.Add(sensePic);
			var wsEn = Cache.WritingSystemFactory.GetWsFromStr("en");
			sensePic.Caption.set_String(wsEn, Cache.TsStrFactory.MakeString("caption", wsEn));
			var pic = Cache.ServiceLocator.GetInstance<ICmFileFactory>().Create();
			var folder = Cache.ServiceLocator.GetInstance<ICmFolderFactory>().Create();
			Cache.LangProject.MediaOC.Add(folder);
			folder.FilesOC.Add(pic);
			const string pathWithUtf8Char = "cave\u00E7on";
			var decomposedPath = Icu.Normalize(pathWithUtf8Char, Icu.UNormalizationMode.UNORM_NFD);
			var composedPath = Icu.Normalize(pathWithUtf8Char, Icu.UNormalizationMode.UNORM_NFC);
			// Set the internal path to decomposed (which is what FLEx does when it loads data)
			pic.InternalPath = decomposedPath;
			sensePic.PictureFileRA = pic;

			using(var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				// generates a src attribute with an absolute file path
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				//SUT
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, null, settings));
				XHTMLWriter.Flush();
				var pictureWithComposedPath = "/div[@class='lexentry']/span[@class='pictures']/span[@class='picture']/img[contains(@src, '" + composedPath + "')]";
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(pictureWithComposedPath, 1);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_PictureCopiedAndRelativePathUsed()
		{
			var mainEntryNode = CreatePictureModel();
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });
			var testEntry = CreateInterestingLexEntry(Cache);
			var sense = testEntry.SensesOS[0];
			var sensePic = Cache.ServiceLocator.GetInstance<ICmPictureFactory>().Create();
			sense.PicturesOS.Add(sensePic);
			var pic = Cache.ServiceLocator.GetInstance<ICmFileFactory>().Create();
			var folder = Cache.ServiceLocator.GetInstance<ICmFolderFactory>().Create();
			Cache.LangProject.MediaOC.Add(folder);
			folder.FilesOC.Add(pic);
			var filePath = Path.GetTempFileName();
			// Write a couple of jpeg header bytes (for no particular reason)
			File.WriteAllBytes(filePath, new byte[] { 0xFF, 0xE0, 0x0, 0x0});
			pic.InternalPath = filePath;
			sensePic.PictureFileRA = pic;

			using(var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var tempFolder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "ConfigDictPictureExportTest"));
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, true, true, tempFolder.FullName);
				//SUT
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, null, settings));
				XHTMLWriter.Flush();
				var pictureRelativePath = Path.Combine("pictures", Path.GetFileName(filePath));
				var pictureWithComposedPath = "/div[@class='lexentry']/span[@class='pictures']/span[@class='picture']/img[starts-with(@src, '" + pictureRelativePath + "')]";
				if (!MiscUtils.IsUnix)
					AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(pictureWithComposedPath, 1);
				// that src starts with a string, and escaping any Windows path separators
				AssertRegex(XHTMLStringBuilder.ToString(), string.Format("src=\"{0}[^\"]*\"", pictureRelativePath.Replace(@"\", @"\\")), 1);
				Assert.IsTrue(File.Exists(Path.Combine(tempFolder.Name, "pictures", filePath)));
				Palaso.IO.DirectoryUtilities.DeleteDirectoryRobust(tempFolder.FullName);
			}
			File.Delete(filePath);
		}

		[Test]
		public void GenerateXHTMLForEntry_MissingPictureFileDoesNotCrashOnCopy()
		{
			var mainEntryNode = CreatePictureModel();
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });
			var testEntry = CreateInterestingLexEntry(Cache);
			var sense = testEntry.SensesOS[0];
			var sensePic = Cache.ServiceLocator.GetInstance<ICmPictureFactory>().Create();
			sense.PicturesOS.Add(sensePic);
			var pic = Cache.ServiceLocator.GetInstance<ICmFileFactory>().Create();
			var folder = Cache.ServiceLocator.GetInstance<ICmFolderFactory>().Create();
			Cache.LangProject.MediaOC.Add(folder);
			folder.FilesOC.Add(pic);
			var filePath = Path.GetRandomFileName();
			pic.InternalPath = filePath;
			sensePic.PictureFileRA = pic;

			using(var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var tempFolder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "ConfigDictPictureExportTest"));
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, true, true, tempFolder.FullName);
				//SUT
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, null, settings));
				XHTMLWriter.Flush();
				var pictureRelativePath = Path.Combine("pictures", Path.GetFileName(filePath));
				var pictureWithComposedPath = "/div[@class='lexentry']/span[@class='pictures']/span[@class='picture']/img[starts-with(@src, '" + pictureRelativePath + "')]";
				if (!MiscUtils.IsUnix)
					AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(pictureWithComposedPath, 1);
				// that src starts with a string, and escaping any Windows path separators
				AssertRegex(XHTMLStringBuilder.ToString(), string.Format("src=\"{0}[^\"]*\"", pictureRelativePath.Replace(@"\", @"\\")), 1);
				Assert.IsFalse(File.Exists(Path.Combine(tempFolder.Name, "pictures", filePath)));
				Palaso.IO.DirectoryUtilities.DeleteDirectoryRobust(tempFolder.FullName);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_TwoDifferentFilesGetTwoDifferentResults()
		{
			var mainEntryNode = CreatePictureModel();
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });
			var testEntry = CreateInterestingLexEntry(Cache);
			AddSenseToEntry(testEntry, "second", m_wsEn, Cache);
			var sense = testEntry.SensesOS[0];
			var sensePic = Cache.ServiceLocator.GetInstance<ICmPictureFactory>().Create();
			sense.PicturesOS.Add(sensePic);
			var pic = Cache.ServiceLocator.GetInstance<ICmFileFactory>().Create();
			var folder = Cache.ServiceLocator.GetInstance<ICmFolderFactory>().Create();
			var sense2 = testEntry.SensesOS[1];
			var sensePic2 = Cache.ServiceLocator.GetInstance<ICmPictureFactory>().Create();
			sense2.PicturesOS.Add(sensePic2);
			var pic2 = Cache.ServiceLocator.GetInstance<ICmFileFactory>().Create();
			var folder2 = Cache.ServiceLocator.GetInstance<ICmFolderFactory>().Create();
			Cache.LangProject.MediaOC.Add(folder);
			Cache.LangProject.MediaOC.Add(folder2);
			folder.FilesOC.Add(pic);
			folder2.FilesOC.Add(pic2);
			var fileName = Path.GetRandomFileName();
			var tempPath1 = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
			Directory.CreateDirectory(tempPath1);
			var tempPath2 = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
			Directory.CreateDirectory(tempPath2);
			// Write a couple of jpeg header bytes (for no particular reason)
			var filePath1 = Path.Combine(tempPath1, fileName);
			File.WriteAllBytes(filePath1, new byte[] { 0xFF, 0xE0, 0x0, 0x0, 0x1 });
			var filePath2 = Path.Combine(tempPath2, fileName);
			File.WriteAllBytes(filePath2, new byte[] { 0xFF, 0xE0, 0x0, 0x0, 0x2 });
			pic.InternalPath = filePath1;
			pic2.InternalPath = filePath2;
			sensePic.PictureFileRA = pic;
			sensePic2.PictureFileRA = pic2;

			var tempFolder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "ConfigDictPictureExportTest"));
			try
			{
				using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
				{
					var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, true, true, tempFolder.FullName);
					//SUT
					Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, null, settings));
					XHTMLWriter.Flush();
					var pictureRelativePath = Path.Combine("pictures", Path.GetFileName(fileName));
					var pictureWithComposedPath = "/div[@class='lexentry']/span[@class='pictures']/span[@class='picture']/img[contains(@src, '" + pictureRelativePath + "')]";
					if (!MiscUtils.IsUnix)
						AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(pictureWithComposedPath, 1);
					// that src contains a string, and escaping any Windows path separators
					AssertRegex(XHTMLStringBuilder.ToString(), string.Format("src=\"[^\"]*{0}[^\"]*\"", pictureRelativePath.Replace(@"\", @"\\")), 1);
					// The second file with the same name should have had something appended to the end of the filename but the initial filename should match both entries
					var filenameWithoutExtension = Path.GetFileNameWithoutExtension(pictureRelativePath);
					var pictureStartsWith ="/div[@class='lexentry']/span[@class='pictures']/span[@class='picture']/img[contains(@src, '" +filenameWithoutExtension + "')]";
					if (!MiscUtils.IsUnix)
						AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(pictureStartsWith, 2);
					// that src contains a string
					AssertRegex(XHTMLStringBuilder.ToString(), string.Format("src=\"[^\"]*{0}[^\"]*\"", filenameWithoutExtension), 2);
					Assert.AreEqual(2, Directory.EnumerateFiles(Path.Combine(tempFolder.FullName, "pictures")).Count(), "Wrong number of pictures copied.");
				}
			}
			finally
			{
				Palaso.IO.DirectoryUtilities.DeleteDirectoryRobust(tempFolder.FullName);
				File.Delete(filePath1);
				File.Delete(filePath2);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_TwoDifferentLinksToTheSamefileWorks()
		{
			var mainEntryNode = CreatePictureModel();
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });
			var testEntry = CreateInterestingLexEntry(Cache);
			AddSenseToEntry(testEntry, "second", m_wsEn, Cache);
			var sense = testEntry.SensesOS[0];
			var sensePic = Cache.ServiceLocator.GetInstance<ICmPictureFactory>().Create();
			sense.PicturesOS.Add(sensePic);
			var pic = Cache.ServiceLocator.GetInstance<ICmFileFactory>().Create();
			var folder = Cache.ServiceLocator.GetInstance<ICmFolderFactory>().Create();
			var sense2 = testEntry.SensesOS[1];
			var sensePic2 = Cache.ServiceLocator.GetInstance<ICmPictureFactory>().Create();
			sense2.PicturesOS.Add(sensePic2);
			Cache.LangProject.MediaOC.Add(folder);
			folder.FilesOC.Add(pic);
			var fileName = Path.GetTempFileName();
			// Write a couple of jpeg header bytes (for no particular reason)
			File.WriteAllBytes(fileName, new byte[] { 0xFF, 0xE0, 0x0, 0x0, 0x1 });
			pic.InternalPath = fileName;
			sensePic.PictureFileRA = pic;
			sensePic2.PictureFileRA = pic;

			var tempFolder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "ConfigDictPictureExportTest"));
			try
			{
				using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
				{
					var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, true, true, tempFolder.FullName);
					//SUT
					Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, null, settings));
					XHTMLWriter.Flush();
					var pictureRelativePath = Path.Combine("pictures", Path.GetFileName(fileName));
					var pictureWithComposedPath = "/div[@class='lexentry']/span[@class='pictures']/span[@class='picture']/img[contains(@src, '" + pictureRelativePath + "')]";
					if (!MiscUtils.IsUnix)
						AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(pictureWithComposedPath, 2);
					// that src starts with string, and escaping Windows directory separators
					AssertRegex(XHTMLStringBuilder.ToString(), string.Format("src=\"{0}[^\"]*\"", pictureRelativePath.Replace(@"\",@"\\")), 2);
					// The second file reference should not have resulted in a copy
					Assert.AreEqual(Directory.EnumerateFiles(Path.Combine(tempFolder.FullName, "pictures")).Count(), 1, "Wrong number of pictures copied.");
				}
			}
			finally
			{
				Palaso.IO.DirectoryUtilities.DeleteDirectoryRobust(tempFolder.FullName);
				File.Delete(fileName);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_StringCustomFieldGeneratesContent()
		{
			using(var customField = new CustomFieldForTest(Cache, "CustomString", Cache.MetaDataCacheAccessor.GetClassId("LexEntry"), 0,
			 CellarPropertyType.String, Guid.Empty))
			{
				var customFieldNode = new ConfigurableDictionaryNode
				{
					FieldDescription = "CustomString",
					Label = "Custom String",
					IsEnabled = true,
					IsCustomField = true
				};
				var mainEntryNode = new ConfigurableDictionaryNode
				{
					Children = new List<ConfigurableDictionaryNode> { customFieldNode },
					FieldDescription = "LexEntry",
					IsEnabled = true
				};
				DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });
				var testEntry = CreateInterestingLexEntry(Cache);
				const string customData = @"I am custom data";
				var wsEn = Cache.WritingSystemFactory.GetWsFromStr("en");

				// Set custom field data
				Cache.MainCacheAccessor.SetString(testEntry.Hvo, customField.Flid, Cache.TsStrFactory.MakeString(customData, wsEn));
				using(var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
				{
					var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
					//SUT
					Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, null, settings));
					XHTMLWriter.Flush();
					var customDataPath = string.Format("/div[@class='lexentry']/span[@class='customstring']/span[text()='{0}']", customData);
					AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(customDataPath, 1);
				}
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_GetPropertyTypeForConfigurationNode_StringCustomFieldIsPrimitive()
		{
			using(var customField = new CustomFieldForTest(Cache, "CustomString", Cache.MetaDataCacheAccessor.GetClassId("LexEntry"), 0,
			 CellarPropertyType.String, Guid.Empty))
			{
				var customFieldNode = new ConfigurableDictionaryNode
				{
					FieldDescription = "CustomString",
					Label = "Custom String",
					IsEnabled = true,
					IsCustomField = true
				};
				var mainEntryNode = new ConfigurableDictionaryNode
				{
					Children = new List<ConfigurableDictionaryNode> { customFieldNode },
					FieldDescription = "LexEntry",
					IsEnabled = true
				};
				DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });
				var testEntry = CreateInterestingLexEntry(Cache);
				const string customData = @"I am custom data";
				var wsEn = Cache.WritingSystemFactory.GetWsFromStr("en");

				// Set custom field data
				Cache.MainCacheAccessor.SetString(testEntry.Hvo, customField.Flid, Cache.TsStrFactory.MakeString(customData, wsEn));
				//SUT
				Assert.AreEqual(ConfiguredXHTMLGenerator.PropertyType.PrimitiveType, ConfiguredXHTMLGenerator.GetPropertyTypeForConfigurationNode(customFieldNode, Cache));
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_StringCustomFieldOnSenseGeneratesContent()
		{
			using(var customField = new CustomFieldForTest(Cache, "CustomString", Cache.MetaDataCacheAccessor.GetClassId("LexSense"), 0,
			 CellarPropertyType.String, Guid.Empty))
			{
				var customFieldNode = new ConfigurableDictionaryNode
				{
					FieldDescription = "CustomString",
					Label = "Custom String",
					IsEnabled = true,
					IsCustomField = true
				};
				var senseNode = new ConfigurableDictionaryNode
				{
					FieldDescription = "SensesOS",
					IsEnabled = true,
					Children = new List<ConfigurableDictionaryNode> { customFieldNode },
					CSSClassNameOverride = "es"
				};
				var mainEntryNode = new ConfigurableDictionaryNode
				{
					Children = new List<ConfigurableDictionaryNode> { senseNode },
					FieldDescription = "LexEntry",
					IsEnabled = true,
					CSSClassNameOverride = "l"
				};
				DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });
				var testEntry = CreateInterestingLexEntry(Cache);
				const string customData = @"I am custom sense data";
				var wsEn = Cache.WritingSystemFactory.GetWsFromStr("en");
				var testSence = testEntry.SensesOS[0];

				// Set custom field data
				Cache.MainCacheAccessor.SetString(testSence.Hvo, customField.Flid, Cache.TsStrFactory.MakeString(customData, wsEn));
				using(var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
				{
					var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
					//SUT
					Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, null, settings));
					XHTMLWriter.Flush();
					var customDataPath = string.Format("/div[@class='l']/span[@class='es']/span[@class='e']/span[@class='customstring']/span[text()='{0}']", customData);
					AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(customDataPath, 1);
				}
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_StringCustomFieldOnExampleGeneratesContent()
		{
			using(var customField = new CustomFieldForTest(Cache, "CustomString", Cache.MetaDataCacheAccessor.GetClassId("LexExampleSentence"), 0,
			 CellarPropertyType.String, Guid.Empty))
			{
				var customFieldNode = new ConfigurableDictionaryNode
				{
					FieldDescription = "CustomString",
					Label = "Custom String",
					IsEnabled = true,
					IsCustomField = true
				};
				var exampleNode = new ConfigurableDictionaryNode
				{
					FieldDescription = "ExamplesOS",
					IsEnabled = true,
					Children = new List<ConfigurableDictionaryNode> { customFieldNode },
					CSSClassNameOverride = "xs"
				};
				var senseNode = new ConfigurableDictionaryNode
				{
					FieldDescription = "SensesOS",
					IsEnabled = true,
					Children = new List<ConfigurableDictionaryNode> { exampleNode },
					CSSClassNameOverride = "es"
				};
				var mainEntryNode = new ConfigurableDictionaryNode
				{
					Children = new List<ConfigurableDictionaryNode> { senseNode },
					FieldDescription = "LexEntry",
					IsEnabled = true,
					CSSClassNameOverride = "l"
				};
				DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });
				var testEntry = CreateInterestingLexEntry(Cache);
				const string customData = @"I am custom example data";
				var wsEn = Cache.WritingSystemFactory.GetWsFromStr("en");
				var testSense = testEntry.SensesOS[0];
				var exampleSentence = AddExampleToSense(testSense, @"I'm an example");

				// Set custom field data
				Cache.MainCacheAccessor.SetString(exampleSentence.Hvo, customField.Flid, Cache.TsStrFactory.MakeString(customData, wsEn));
				using(var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
				{
					var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
					//SUT
					Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, null, settings));
					XHTMLWriter.Flush();
					var customDataPath = string.Format(
						"/div[@class='l']/span[@class='es']//span[@class='xs']/span[@class='x']/span[@class='customstring']/span[text()='{0}']", customData);
					AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(customDataPath, 1);
				}
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_StringCustomFieldOnAllomorphGeneratesContent()
		{
			using(var customField = new CustomFieldForTest(Cache, "CustomString", Cache.MetaDataCacheAccessor.GetClassId("MoForm"), 0,
			 CellarPropertyType.String, Guid.Empty))
			{
				var customFieldNode = new ConfigurableDictionaryNode
				{
					FieldDescription = "CustomString",
					Label = "Custom String",
					IsEnabled = true,
					IsCustomField = true
				};
				var senseNode = new ConfigurableDictionaryNode
				{
					FieldDescription = "AlternateFormsOS",
					IsEnabled = true,
					Children = new List<ConfigurableDictionaryNode> { customFieldNode },
					CSSClassNameOverride = "as"
				};
				var mainEntryNode = new ConfigurableDictionaryNode
				{
					Children = new List<ConfigurableDictionaryNode> { senseNode },
					FieldDescription = "LexEntry",
					IsEnabled = true,
					CSSClassNameOverride = "l"
				};
				DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });
				var testEntry = CreateInterestingLexEntry(Cache);
				const string customData = @"I am custom morph data";
				var wsEn = Cache.WritingSystemFactory.GetWsFromStr("en");
				var allomorph = AddAllomorphToEntry(testEntry);

				// Set custom field data
				Cache.MainCacheAccessor.SetString(allomorph.Hvo, customField.Flid, Cache.TsStrFactory.MakeString(customData, wsEn));
				using(var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
				{
					var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
					//SUT
					Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, null, settings));
					XHTMLWriter.Flush();
					var customDataPath = string.Format(
						"/div[@class='l']/span[@class='as']/span[@class='a']/span[@class='customstring']/span[text()='{0}']", customData);
					AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(customDataPath, 1);
				}
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_MultiStringCustomFieldGeneratesContent()
		{
			using(var customField = new CustomFieldForTest(Cache, "CustomString", Cache.MetaDataCacheAccessor.GetClassId("LexEntry"), 0,
			 CellarPropertyType.MultiString, Guid.Empty))
			{
				var customFieldNode = new ConfigurableDictionaryNode
				{
					FieldDescription = "CustomString",
					Label = "Custom String",
					IsEnabled = true,
					IsCustomField = true,
					DictionaryNodeOptions = GetWsOptionsForLanguages(new [] { "en" })
				};
				var mainEntryNode = new ConfigurableDictionaryNode
				{
					Children = new List<ConfigurableDictionaryNode> { customFieldNode },
					FieldDescription = "LexEntry",
					IsEnabled = true
				};
				DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });
				var testEntry = CreateInterestingLexEntry(Cache);
				const string customData = @"I am custom data";
				var wsEn = Cache.WritingSystemFactory.GetWsFromStr("en");

				// Set custom field data
				Cache.MainCacheAccessor.SetMultiStringAlt(testEntry.Hvo, customField.Flid, wsEn, Cache.TsStrFactory.MakeString(customData, wsEn));
				using(var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
				{
					var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
					//SUT
					Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, null, settings));
					XHTMLWriter.Flush();
					var customDataPath = string.Format("/div[@class='lexentry']/span[@class='customstring']/span[text()='{0}']", customData);
					AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(customDataPath, 1);
				}
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_ListItemCustomFieldGeneratesContent()
		{
			var wsEn = Cache.WritingSystemFactory.GetWsFromStr("en");
			var possibilityItem = Cache.LanguageProject.LocationsOA.FindOrCreatePossibility("Djbuti", wsEn);
			possibilityItem.Name.set_String(wsEn, "Djbuti");

			using(var customField = new CustomFieldForTest(Cache, "CustomListItem", Cache.MetaDataCacheAccessor.GetClassId("LexEntry"), 0,
					CellarPropertyType.OwningAtomic, Cache.LanguageProject.LocationsOA.Guid))
			{
				var nameNode = new ConfigurableDictionaryNode
				{
					FieldDescription = "Name", IsEnabled = true,
					DictionaryNodeOptions = GetWsOptionsForLanguages(new[]{ "en" })
				};
				var customFieldNode = new ConfigurableDictionaryNode
				{
					FieldDescription = "CustomListItem",
					Label = "Custom List Item",
					IsEnabled = true,
					IsCustomField = true,
					Children = new List<ConfigurableDictionaryNode> { nameNode }
				};
				var mainEntryNode = new ConfigurableDictionaryNode
				{
					Children = new List<ConfigurableDictionaryNode> { customFieldNode },
					FieldDescription = "LexEntry",
					IsEnabled = true
				};
				DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });
				var testEntry = CreateInterestingLexEntry(Cache);

				// Set custom field data
				Cache.MainCacheAccessor.SetObjProp(testEntry.Hvo, customField.Flid, possibilityItem.Hvo);
				using(var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
				{
					var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
					//SUT
					Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, null, settings));
					XHTMLWriter.Flush();
					const string customDataPath = "/div[@class='lexentry']/span[@class='customlistitem']/span[@class='name']/span[text()='Djbuti']";
					AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(customDataPath, 1);
				}
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_MultiListItemCustomFieldGeneratesContent()
		{
			var wsEn = Cache.WritingSystemFactory.GetWsFromStr("en");
			var possibilityItem1 = Cache.LanguageProject.LocationsOA.FindOrCreatePossibility("Dallas", wsEn);
			var possibilityItem2 = Cache.LanguageProject.LocationsOA.FindOrCreatePossibility("Barcelona", wsEn);
			possibilityItem1.Name.set_String(wsEn, "Dallas");
			possibilityItem2.Name.set_String(wsEn, "Barcelona");

			using(var customField = new CustomFieldForTest(Cache, "CustomListItems", Cache.MetaDataCacheAccessor.GetClassId("LexEntry"), 0,
					CellarPropertyType.ReferenceSequence, Cache.LanguageProject.LocationsOA.Guid))
			{
				var nameNode = new ConfigurableDictionaryNode
				{
					FieldDescription = "Name",
					IsEnabled = true,
					DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" })
				};
				var customFieldNode = new ConfigurableDictionaryNode
				{
					FieldDescription = "CustomListItems",
					Label = "Custom List Items",
					IsEnabled = true,
					IsCustomField = true,
					Children = new List<ConfigurableDictionaryNode> { nameNode }
				};
				var mainEntryNode = new ConfigurableDictionaryNode
				{
					Children = new List<ConfigurableDictionaryNode> { customFieldNode },
					FieldDescription = "LexEntry",
					IsEnabled = true
				};
				DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });
				var testEntry = CreateInterestingLexEntry(Cache);

				// Set custom field data
				Cache.MainCacheAccessor.Replace(testEntry.Hvo, customField.Flid, 0, 0, new [] {possibilityItem1.Hvo, possibilityItem2.Hvo}, 2);
				using(var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
				{
					var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
					//SUT
					Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, null, settings));
					XHTMLWriter.Flush();
					const string customDataPath1 = "/div[@class='lexentry']/span[@class='customlistitems']/span[@class='customlistitem']/span[@class='name']/span[text()='Dallas']";
					const string customDataPath2 = "/div[@class='lexentry']/span[@class='customlistitems']/span[@class='customlistitem']/span[@class='name']/span[text()='Barcelona']";
					AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(customDataPath1, 1);
					AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(customDataPath2, 1);
				}
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_DateCustomFieldGeneratesContent()
		{
			using(var customField = new CustomFieldForTest(Cache, "CustomDate", Cache.MetaDataCacheAccessor.GetClassId("LexEntry"), 0,
			 CellarPropertyType.Time, Guid.Empty))
			{
				var customFieldNode = new ConfigurableDictionaryNode
				{
					FieldDescription = "CustomDate",
					Label = "Custom Date",
					IsEnabled = true,
					IsCustomField = true
				};
				var mainEntryNode = new ConfigurableDictionaryNode
				{
					Children = new List<ConfigurableDictionaryNode> { customFieldNode },
					FieldDescription = "LexEntry",
					IsEnabled = true
				};
				DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });
				var testEntry = CreateInterestingLexEntry(Cache);
				var customData = DateTime.Now;

				// Set custom field data
				SilTime.SetTimeProperty(Cache.MainCacheAccessor, testEntry.Hvo, customField.Flid, customData);
				using(var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
				{
					var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
					//SUT
					Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, null, settings));
					XHTMLWriter.Flush();
					var customDataPath = string.Format("/div[@class='lexentry']/span[@class='customdate' and text()='{0}']", customData.ToLongDateString());
					AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(customDataPath, 1);
				}
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_IntegerCustomFieldGeneratesContent()
		{
			using (var customField = new CustomFieldForTest(Cache, "CustomInteger", Cache.MetaDataCacheAccessor.GetClassId("LexEntry"), 0,
			 CellarPropertyType.Integer, Guid.Empty))
			{
				var customFieldNode = new ConfigurableDictionaryNode
				{
					FieldDescription = "CustomInteger",
					Label = "Custom Integer",
					IsEnabled = true,
					IsCustomField = true
				};
				var mainEntryNode = new ConfigurableDictionaryNode
				{
					Children = new List<ConfigurableDictionaryNode> { customFieldNode },
					FieldDescription = "LexEntry",
					IsEnabled = true
				};
				DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });
				var testEntry = CreateInterestingLexEntry(Cache);
				const int customData = 123456;

				// Set custom field data
				Cache.MainCacheAccessor.SetInt(testEntry.Hvo, customField.Flid, customData);
				using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
				{
					var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
					//SUT
					Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, null, settings));
					XHTMLWriter.Flush();
					var customDataPath = string.Format("/div[@class='lexentry']/span[@class='custominteger' and text()='{0}']", customData);
					AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(customDataPath, 1);
				}
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_MultiLineCustomFieldGeneratesContent()
		{
			using (
				var customField = new CustomFieldForTest(Cache, "MultiplelineTest",
					Cache.MetaDataCacheAccessor.GetClassId("LexEntry"), 0,
					CellarPropertyType.OwningAtomic, Guid.Empty))
			{
				var memberNode = new ConfigurableDictionaryNode
				{
					Label = "Multiple lineTest",
					FieldDescription = "MultiplelineTest",
					IsCustomField = true,
					IsEnabled = true
				};
				var rootNode = new ConfigurableDictionaryNode
				{
					FieldDescription = "LexEntry",
					IsEnabled = true,
					Children = new List<ConfigurableDictionaryNode> {memberNode}
				};
				DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> {rootNode});
				var testEntry = CreateInterestingLexEntry(Cache);
				var text = CreateMultiParaText("Custom string", Cache);
				// SUT
				Cache.MainCacheAccessor.SetObjProp(testEntry.Hvo, customField.Flid, text.Hvo);
				using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
				{
					var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
					//SUT
					Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, rootNode, null, settings));
					XHTMLWriter.Flush();
					const string customDataPath =
						"/div[@class='lexentry']/div/span[text()='First para Custom string'] | /div[@class='lexentry']/div/span[text()='Second para Custom string']";
					AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(customDataPath, 2);
				}
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_VariantOfReferencedHeadWord()
		{
			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "HeadWord",
				IsEnabled = true,
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" })
			};
			var refNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ConfigReferencedEntries",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { headwordNode },
				CSSClassNameOverride = "referencedentries"
			};
			var variantsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VisibleVariantEntryRefs",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { refNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { variantsNode }
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });

			var mainEntry = CreateInterestingLexEntry(Cache);
			var variantForm = CreateInterestingLexEntry(Cache);
			CreateVariantForm(variantForm, mainEntry);
			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				//SUT
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(mainEntry, mainEntryNode, null, settings));
				XHTMLWriter.Flush();
				const string referencedEntries =
					"//span[@class='visiblevariantentryrefs']/span[@class='visiblevariantentryref']/span[@class='referencedentries']/span[@class='referencedentrie']/span[@class='headword']/span[@lang='en']";
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString())
					.HasSpecifiedNumberOfMatchesForXpath(referencedEntries, 2);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_WsAudiowithHyperlink()
		{
			IWritingSystem wsEnAudio;
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("en-Zxxx-x-audio", out wsEnAudio);
			Cache.ServiceLocator.WritingSystems.AddToCurrentVernacularWritingSystems(wsEnAudio);
			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexemeFormOA",
				Label = "Lexeme Form",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en-Zxxx-x-audio" }),
				IsEnabled = true
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { headwordNode },
				FieldDescription = "LexEntry",
				IsEnabled = true
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });
			var entryOne = CreateInterestingLexEntry(Cache);
			var senseaudio = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			entryOne.LexemeFormOA = senseaudio;
			senseaudio.Form.set_String(wsEnAudio.Handle, Cache.TsStrFactory.MakeString("TestAudio.wav", wsEnAudio.Handle));
			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				//SUT
				Assert.DoesNotThrow(
					() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entryOne, mainEntryNode, null, settings));
				XHTMLWriter.Flush();

				const string audioTagwithSource = "//audio/source/@src";
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString())
					.HasSpecifiedNumberOfMatchesForXpath(audioTagwithSource, 1);
				const string audioFileUrl =
					@"Src/xWorks/xWorksTests/TestData/LinkedFiles/AudioVisual/TestAudio.wav";
				Assert.That(XHTMLStringBuilder.ToString(), Contains.Substring(audioFileUrl));
				const string linkTagwithOnClick =
					"//span[@class='lexemeformoa']/span/a[@class='en-Zxxx-x-audio' and contains(@onclick,'play()')]";
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString())
					.HasSpecifiedNumberOfMatchesForXpath(linkTagwithOnClick, 1);

			}
		}

		[Test]
		public void GenerateXHTMLForEntry_WsAudiowithRelativePaths()
		{
			IWritingSystem wsEnAudio;
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("en-Zxxx-x-audio", out wsEnAudio);
			Cache.ServiceLocator.WritingSystems.AddToCurrentVernacularWritingSystems(wsEnAudio);
			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexemeFormOA",
				Label = "Lexeme Form",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en-Zxxx-x-audio" }),
				IsEnabled = true
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { headwordNode },
				FieldDescription = "LexEntry",
				IsEnabled = true
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });
			var entryOne = CreateInterestingLexEntry(Cache);
			var senseaudio = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			entryOne.LexemeFormOA = senseaudio;
			senseaudio.Form.set_String(wsEnAudio.Handle, Cache.TsStrFactory.MakeString("TestAudio.wav", wsEnAudio.Handle));
			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, true, true, "//audio/source/@src");
				//SUT
				Assert.DoesNotThrow(
					() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entryOne, mainEntryNode, null, settings));
				XHTMLWriter.Flush();

				const string audioTagwithSource = "//audio/source/@src";
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString())
					.HasSpecifiedNumberOfMatchesForXpath(audioTagwithSource, 1);
				string audioFileUrl = Path.Combine("AudioVisual", "TestAudio.wav");
				Assert.That(XHTMLStringBuilder.ToString(), Contains.Substring(audioFileUrl));
				const string linkTagwithOnClick =
					"//span[@class='lexemeformoa']/span/a[@class='en-Zxxx-x-audio' and contains(@onclick,'play()')]";
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString())
					.HasSpecifiedNumberOfMatchesForXpath(linkTagwithOnClick, 1);

			}
		}

		[Test]
		public void GenerateXHTMLForEntry_GeneratesComplexFormTypeForSubentry()
		{
			var lexentry = CreateInterestingLexEntry(Cache);

			var subentry = CreateInterestingLexEntry(Cache);
			var subentryRef = CreateComplexForm(lexentry, subentry, true);

			var complexRefAbbr = subentryRef.ComplexEntryTypesRS[0].Abbreviation.BestAnalysisAlternative.Text;
			var complexRefRevAbbr = subentryRef.ComplexEntryTypesRS[0].ReverseAbbr.BestAnalysisAlternative.Text;

			var revAbbrevNode  = new ConfigurableDictionaryNode
			{
				FieldDescription = "ReverseAbbr", IsEnabled = true,
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" })
			};
			var refTypeNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LookupComplexEntryType", IsEnabled = true,
				CSSClassNameOverride = "complexformtypes",
				Children = new List<ConfigurableDictionaryNode> { revAbbrevNode }
			};
			var subentryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { refTypeNode },
				DictionaryNodeOptions = new DictionaryNodeComplexFormOptions(),
				FieldDescription = "Subentries", IsEnabled = true
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { subentryNode },
				FieldDescription = "LexEntry", IsEnabled = true
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });

			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				//SUT
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(lexentry, mainEntryNode, null, settings));
				XHTMLWriter.Flush();
				var fwdNameXpath = string.Format(
					"//span[@class='subentries']/span[@class='subentrie']/span[@class='complexformtypes']/span[@class='complexformtype']/span/span[@lang='en' and text()='{0}']",
					complexRefAbbr);
				var revNameXpath = string.Format(
					"//span[@class='subentries']/span[@class='subentrie']/span[@class='complexformtypes']/span[@class='complexformtype']/span[@class='reverseabbr']/span[@lang='en' and text()='{0}']",
					complexRefRevAbbr);
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasNoMatchForXpath(fwdNameXpath);
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(revNameXpath, 1);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_DoesntGeneratesComplexFormType_WhenDisabled()
		{
			var lexentry = CreateInterestingLexEntry(Cache);

			var subentry = CreateInterestingLexEntry(Cache);
			var subentryRef = CreateComplexForm(lexentry, subentry, true);

			var complexRefAbbr = subentryRef.ComplexEntryTypesRS[0].Abbreviation.BestAnalysisAlternative.Text;

			var revAbbrevNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ReverseAbbr", IsEnabled = true,
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" })
			};
			var refTypeNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LookupComplexEntryType", IsEnabled = false,
				CSSClassNameOverride = "complexformtypes",
				Children = new List<ConfigurableDictionaryNode> { revAbbrevNode }
			};
			var subentryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { refTypeNode },
				DictionaryNodeOptions = new DictionaryNodeComplexFormOptions(),
				FieldDescription = "Subentries", IsEnabled = true
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { subentryNode },
				FieldDescription = "LexEntry", IsEnabled = true
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });

			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				//SUT
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(lexentry, mainEntryNode, null, settings));
				XHTMLWriter.Flush();
				const string refTypeXpath = "//span[@class='subentries']/span[@class='subentrie']/span[@class='complexformtypes']";
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasNoMatchForXpath(refTypeXpath);
				StringAssert.DoesNotContain(complexRefAbbr, XHTMLStringBuilder.ToString());
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_GeneratesComplexForm_NoTypeSpecified()
		{
			var lexentry = CreateInterestingLexEntry(Cache);
			var complexEntry = CreateInterestingLexEntry(Cache);
			var complexFormRef= CreateComplexForm(lexentry, complexEntry, false);
			complexFormRef.ComplexEntryTypesRS.Clear(); // no complex form type specified

			var formNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwningEntry",
				SubField = "MLHeadWord",
				Label = "Headword",
				CSSClassNameOverride = "headword",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" }),
				IsEnabled = true
			};
			var complexEntryTypeNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ComplexEntryTypesRS",
				IsEnabled = true,
				CSSClassNameOverride = "complexformtypes",
			};
			var complexOptions = GetFullyEnabledListOptions(DictionaryNodeListOptions.ListIds.Complex, true);
			var referencedCompFormNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> {complexEntryTypeNode, formNode },
				DictionaryNodeOptions = complexOptions,
				FieldDescription = "VisibleComplexFormBackRefs",
				IsEnabled = true
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { referencedCompFormNode },
				FieldDescription = "LexEntry",
				IsEnabled = true
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });

			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				//SUT
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(lexentry, mainEntryNode, null, settings));
				XHTMLWriter.Flush();
				var result = XHTMLStringBuilder.ToString();
				const string refTypeXpath = "//span[@class='visiblecomplexformbackrefs']/span[@class='visiblecomplexformbackref']/span[@class='complexformtypes']/span[@class='complexformtype']";
				AssertThatXmlIn.String(result).HasNoMatchForXpath(refTypeXpath);
				const string headwordXpath = "//span[@class='visiblecomplexformbackrefs']/span[@class='visiblecomplexformbackref']/span[@class='headword']";
				AssertThatXmlIn.String(result).HasAtLeastOneMatchForXpath(headwordXpath);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_GeneratesSubentry_NoTypeSpecified()
		{
			var lexentry = CreateInterestingLexEntry(Cache);
			var subentry = CreateInterestingLexEntry(Cache);
			var subentryRef = CreateComplexForm(lexentry, subentry, true);
			subentryRef.ComplexEntryTypesRS.Clear(); // no complex form type specified

			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLHeadWord",
				Label = "Headword",
				CSSClassNameOverride = "headword",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" }),
				IsEnabled = true
			};
			var refTypeNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LookupComplexEntryType",
				IsEnabled = true,
				CSSClassNameOverride = "complexformtypes",
			};
			var complexOptions = GetFullyEnabledListOptions(DictionaryNodeListOptions.ListIds.Complex, true);
			var subentryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { refTypeNode, headwordNode },
				DictionaryNodeOptions = complexOptions,
				FieldDescription = "Subentries",
				IsEnabled = true
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { subentryNode },
				FieldDescription = "LexEntry",
				IsEnabled = true
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });

			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				//SUT
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(lexentry, mainEntryNode, null, settings));
				XHTMLWriter.Flush();
				var result = XHTMLStringBuilder.ToString();
				const string refTypeXpath = "//span[@class='subentries']/span[@class='subentrie']/span[@class='complexformtypes']/span[@class='complexformtype']";
				AssertThatXmlIn.String(result).HasNoMatchForXpath(refTypeXpath);
				const string headwordXpath = "//span[@class='subentries']/span[@class='subentrie']/span[@class='headword']";
				AssertThatXmlIn.String(result).HasAtLeastOneMatchForXpath(headwordXpath);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_GeneratesVariant_NoTypeSpecified()
		{
			var lexentry = CreateInterestingLexEntry(Cache);
			var variantEntry = CreateInterestingLexEntry(Cache);
			var variantEntryRef = CreateVariantForm(lexentry, variantEntry);
			variantEntryRef.VariantEntryTypesRS.Clear(); // no variant entry type specified

			var formNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwningEntry",
				SubField = "MLHeadWord",
				Label = "Headword",
				CSSClassNameOverride = "headword",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" }),
				IsEnabled = true
			};
			var variantEntryTypeNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VariantEntryTypesRS",
				IsEnabled = true,
			};
			var variantFormNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { variantEntryTypeNode, formNode },
				DictionaryNodeOptions = GetFullyEnabledListOptions(DictionaryNodeListOptions.ListIds.Variant),
				FieldDescription = "VariantFormEntryBackRefs",
				IsEnabled = true
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { variantFormNode },
				FieldDescription = "LexEntry",
				IsEnabled = true
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });

			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				//SUT
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(lexentry, mainEntryNode, null, settings));
				XHTMLWriter.Flush();
				var result = XHTMLStringBuilder.ToString();
				const string refTypeXpath = "//span[@class='variantformentrybackrefs']/span[@class='variantformentrybackref']/span[@class='variantentrytypesrs']/span[@class='variantentrytypesr']";
				AssertThatXmlIn.String(result).HasNoMatchForXpath(refTypeXpath);
				const string headwordXpath = "//span[@class='variantformentrybackrefs']/span[@class='variantformentrybackref']/span[@class='headword']";
				AssertThatXmlIn.String(result).HasAtLeastOneMatchForXpath(headwordXpath);
			}
		}

		public enum FormType { Specified, Unspecified, None }

		[Test]
		public void GenerateXHTMLForEntry_GeneratesCorrectMinorEntries(
			[Values(FormType.Specified, FormType.Unspecified, FormType.None)] FormType complexForm,
			[Values(true, false)] bool isUnspecifiedComplexTypeEnabled,
			[Values(FormType.Specified, FormType.Unspecified, FormType.None)] FormType variantForm,
			[Values(true, false)] bool isUnspecifiedVariantTypeEnabled)
		{
			if (complexForm == FormType.None && variantForm == FormType.None)
				return; // A Minor entry makes no sense if it's neither complex nor variant

			var mainEntry = CreateInterestingLexEntry(Cache);
			var minorEntry = CreateInterestingLexEntry(Cache);
			var enabledMinorEntryTypes = new List<string>();

			if (complexForm != FormType.None)
			{
				var complexRef = CreateComplexForm(mainEntry, minorEntry, false);
				if (complexForm == FormType.Unspecified)
					complexRef.ComplexEntryTypesRS.Clear();
			}

			if(isUnspecifiedComplexTypeEnabled)
				enabledMinorEntryTypes.Add(XmlViewsUtils.GetGuidForUnspecifiedComplexFormType().ToString());

			if (variantForm != FormType.None)
			{
				var variantRef = CreateVariantForm(mainEntry, minorEntry);
				if(variantForm == FormType.Unspecified)
					variantRef.VariantEntryTypesRS.Clear();
			}

			if(isUnspecifiedVariantTypeEnabled)
				enabledMinorEntryTypes.Add(XmlViewsUtils.GetGuidForUnspecifiedVariantType().ToString());

			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLHeadWord", Label = "Headword", CSSClassNameOverride = "headword",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" }),
				IsEnabled = true
			};
			var minorEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { headwordNode },
				DictionaryNodeOptions =  GetListOptionsForStrings(DictionaryNodeListOptions.ListIds.Minor, enabledMinorEntryTypes),
				FieldDescription = "LexEntry", IsEnabled = true
			};
			var model = new DictionaryConfigurationModel
			{
				Parts = new List<ConfigurableDictionaryNode> {new ConfigurableDictionaryNode(), minorEntryNode} // dummy main entry node
			};
			DictionaryConfigurationModel.SpecifyParents(model.Parts);

			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				//SUT
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(minorEntry, model, null, settings));
				XHTMLWriter.Flush();
				var result = XHTMLStringBuilder.ToString();

				var isComplexFormShowing = complexForm == FormType.Unspecified && isUnspecifiedComplexTypeEnabled;
				var isVariantFormShowing = variantForm == FormType.Unspecified && isUnspecifiedVariantTypeEnabled;
				var isMinorEntryShowing = isComplexFormShowing || isVariantFormShowing;

				if (isMinorEntryShowing)
					AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath("/div[@class='lexentry']/span[@class='headword']", 1);
				else
					Assert.IsEmpty(result);
			}
		}

		[Test]
		public void IsCollectionType()
		{
			var assembly = Assembly.Load(ConfiguredXHTMLGenerator.AssemblyFile);
			Assert.True(ConfiguredXHTMLGenerator.IsCollectionType(typeof(IEnumerable<>)));
			Assert.True(ConfiguredXHTMLGenerator.IsCollectionType(typeof(IFdoOwningSequence<>)));
			Assert.True(ConfiguredXHTMLGenerator.IsCollectionType(typeof(IFdoReferenceCollection<>)));
			var twoParamImplOfIFdoVector =
				assembly.GetType("SIL.FieldWorks.FDO.DomainImpl.ScrTxtPara").GetNestedType("OwningSequenceWrapper`2", BindingFlags.NonPublic);
			Assert.True(ConfiguredXHTMLGenerator.IsCollectionType(twoParamImplOfIFdoVector));

			// Strings and MultiStrings, while enumerable, are not collections as we define them for the purpose of publishing data as XHTML
			Assert.False(ConfiguredXHTMLGenerator.IsCollectionType(typeof(string)));
			Assert.False(ConfiguredXHTMLGenerator.IsCollectionType(typeof(ITsString)));
			Assert.False(ConfiguredXHTMLGenerator.IsCollectionType(typeof(IMultiStringAccessor)));
			Assert.False(ConfiguredXHTMLGenerator.IsCollectionType(assembly.GetType("SIL.FieldWorks.FDO.DomainImpl.VirtualStringAccessor")));
		}

		[Test]
		public void GenerateXHTMLForEntry_FilterByPublication()
		{
			// Note that my HS French is nonexistent after 40+ years.  But this is only test code...
			var typeMain = CreatePublicationType("main");
			var typeTest = CreatePublicationType("test");

			// This entry is published for both main and test.  Its first sense (and example) are published in main, its
			// second sense(and example) are published in test.
			// The second example of the first sense should not be published at all, since it is not published in main and
			// its owner is not published in test.
			var entryCorps = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(entryCorps, "corps", m_wsFr, Cache);
			entryCorps.SensesOS[0].Gloss.set_String (m_wsEn, "body");
			var exampleCorpsBody1 = AddExampleToSense(entryCorps.SensesOS[0], "Le corps est gros.", "The body is big.");
			var exampleCorpsBody2 = AddExampleToSense(entryCorps.SensesOS[0], "Le corps est esprit.", "The body is spirited.");
			AddSenseToEntry(entryCorps, "corpse", m_wsEn, Cache);
			var exampleCorpsCorpse1 = AddExampleToSense(entryCorps.SensesOS[1], "Le corps est morte.", "The corpse is dead.");

			entryCorps.SensesOS[0].DoNotPublishInRC.Add(typeTest);
			exampleCorpsBody1.DoNotPublishInRC.Add(typeTest);
			exampleCorpsBody2.DoNotPublishInRC.Add(typeMain);	// should not show at all!

			entryCorps.SensesOS[1].DoNotPublishInRC.Add(typeMain);
			//exampleCorpsCorpse1.DoNotPublishInRC.Add(typeMain); -- should not show in main because its owner is not shown there

			// This entry is published only in main, together with its sense and example.
			var entryBras = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(entryBras, "bras", m_wsFr, Cache);
			entryBras.SensesOS[0].Gloss.set_String(m_wsEn, "arm");
			var exampleBrasArm1 = AddExampleToSense(entryBras.SensesOS[0], "Mon bras est broken.", "My arm is broken.");
			AddSenseToEntry(entryBras, "hand", m_wsEn, Cache);
			var exampleBrasHand1 = AddExampleToSense(entryBras.SensesOS[1], "Ma bras est fine.", "My arm is fine.");
			entryBras.DoNotPublishInRC.Add(typeTest);
			entryBras.SensesOS[0].DoNotPublishInRC.Add(typeTest);
			entryBras.SensesOS[1].DoNotPublishInRC.Add(typeTest);
			//exampleBrasArm1.DoNotPublishInRC.Add(typeTest); -- should not show in test because its owner is not shown there
			//exampleBrasHand1.DoNotPublishInRC.Add(typeTest); -- should not show in test because its owner is not shown there

			// This entry is published only in test, together with its sense and example.
			var entryOreille = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(entryOreille, "oreille", m_wsFr, Cache);
			entryOreille.SensesOS[0].Gloss.set_String(m_wsEn, "ear");
			var exampleOreille1 = AddExampleToSense(entryOreille.SensesOS[0], "Lac Pend d'Oreille est en Idaho.", "Lake Pend d'Oreille is in Idaho.");
			entryOreille.DoNotPublishInRC.Add(typeMain);
			entryOreille.SensesOS[0].DoNotPublishInRC.Add(typeMain);
			//exampleOreille1.DoNotPublishInRC.Add(typeMain); -- should not show in main because its owner is not shown there

			var entryEntry = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(entryEntry, "entry", m_wsFr, Cache);
			entryEntry.SensesOS[0].Gloss.set_String(m_wsEn, "entry");
			var entryMainsubentry = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(entryMainsubentry, "mainsubentry", m_wsFr, Cache);
			entryMainsubentry.SensesOS[0].Gloss.set_String (m_wsEn, "mainsubentry");
			entryMainsubentry.DoNotPublishInRC.Add(typeTest);
			var complexFormRef1 = CreateComplexForm(entryEntry, entryMainsubentry, true);
			var complexRefName1 = complexFormRef1.ComplexEntryTypesRS[0].Name.BestAnalysisAlternative.Text;
			var complexTypePoss1 = Cache.LangProject.LexDbOA.ComplexEntryTypesOA.PossibilitiesOS.First(complex => complex.Name.BestAnalysisAlternative.Text == complexRefName1);
			var entryTestsubentry = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(entryTestsubentry, "testsubentry", m_wsFr, Cache);
			entryTestsubentry.SensesOS[0].Gloss.set_String (m_wsEn, "testsubentry");
			entryTestsubentry.DoNotPublishInRC.Add(typeMain);
			var complexFormRef2 = CreateComplexForm(entryEntry, entryTestsubentry, true);
			var complexRefName2 = complexFormRef2.ComplexEntryTypesRS[0].Name.BestAnalysisAlternative.Text;
			var complexTypePoss2 = Cache.LangProject.LexDbOA.ComplexEntryTypesOA.PossibilitiesOS.First(complex => complex.Name.BestAnalysisAlternative.Text == complexRefName2);

			// Note that the decorators must be created (or refreshed) *after* the data exists.
			int flidVirtual = Cache.ServiceLocator.GetInstance<Virtuals>().LexDbEntries;
			var pubEverything = new DictionaryPublicationDecorator(Cache, (ISilDataAccessManaged)Cache.MainCacheAccessor, flidVirtual);
			var pubMain = new DictionaryPublicationDecorator(Cache, (ISilDataAccessManaged)Cache.MainCacheAccessor, flidVirtual, typeMain);
			var pubTest = new DictionaryPublicationDecorator(Cache, (ISilDataAccessManaged)Cache.MainCacheAccessor, flidVirtual, typeTest);
			//SUT
			var hvosMain = new List<int>( pubMain.GetEntriesToPublish(m_mediator, flidVirtual) );
			Assert.AreEqual(4, hvosMain.Count, "there are four entries in the main publication");
			Assert.IsTrue(hvosMain.Contains(entryCorps.Hvo), "corps is shown in the main publication");
			Assert.IsTrue(hvosMain.Contains(entryBras.Hvo), "bras is shown in the main publication");
			Assert.IsFalse(hvosMain.Contains(entryOreille.Hvo), "oreille is not shown in the main publication");
			Assert.IsTrue(hvosMain.Contains(entryEntry.Hvo), "entry is shown in the main publication");
			Assert.IsTrue(hvosMain.Contains(entryMainsubentry.Hvo), "mainsubentry is shown in the main publication");
			Assert.IsFalse(hvosMain.Contains(entryTestsubentry.Hvo), "testsubentry is not shown in the main publication");
			var hvosTest = new List<int>( pubTest.GetEntriesToPublish(m_mediator, flidVirtual) );
			Assert.AreEqual(4, hvosTest.Count, "there are four entries in the test publication");
			Assert.IsTrue(hvosTest.Contains(entryCorps.Hvo), "corps is shown in the test publication");
			Assert.IsFalse(hvosTest.Contains(entryBras.Hvo), "bras is not shown in the test publication");
			Assert.IsTrue(hvosTest.Contains(entryOreille.Hvo), "oreille is shown in the test publication");
			Assert.IsTrue(hvosTest.Contains(entryEntry.Hvo), "entry is shown in the test publication");
			Assert.IsFalse(hvosTest.Contains(entryMainsubentry.Hvo), "mainsubentry is shown in the test publication");
			Assert.IsTrue(hvosTest.Contains(entryTestsubentry.Hvo), "testsubentry is shown in the test publication");

			var subHeadwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLHeadWord", IsEnabled = true,
				CSSClassNameOverride = "subentry",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};
			var subentryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Subentries", IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { subHeadwordNode }
			};
			var translationNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Translation",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" }),
				CSSClassNameOverride = "translatedsentence",
				IsEnabled = true
			};
			var translationsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "TranslationsOC",
				Children = new List<ConfigurableDictionaryNode> { translationNode },
				CSSClassNameOverride = "translations",
				IsEnabled = true
			};
			var exampleNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Example",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" }),
				CSSClassNameOverride = "examplesentence",
				IsEnabled = true
			};
			var examplesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ExamplesOS",
				Children = new List<ConfigurableDictionaryNode> { exampleNode, translationsNode },
				CSSClassNameOverride = "examples",
				IsEnabled = true
			};
			var glossNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "DefinitionOrGloss",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] {"en"}),
				CSSClassNameOverride = "definitionorgloss",
				IsEnabled = true
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions(),
				Children = new List<ConfigurableDictionaryNode> { glossNode, examplesNode },
				CSSClassNameOverride = "senses",
				IsEnabled = true
			};
			var mainHeadwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "HeadWord",
				Label = "Headword",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" }),
				CSSClassNameOverride = "entry",
				IsEnabled = true
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { mainHeadwordNode, sensesNode, subentryNode },
				FieldDescription = "LexEntry",
				IsEnabled = true
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });

			const string matchFrenchEntry = "//span[@class='entry']/span[@lang='fr']";
			const string matchEnglishDefOrGloss =
				"//span[@class='senses']/span[@class='sensecontent']/span[@class='sense']/span[@class='definitionorgloss']/span[@lang='en']";
			const string matchFrenchExample =
				"//span[@class='senses']/span[@class='sensecontent']/span[@class='sense']/span[@class='examples']/span[@class='example']/span[@class='examplesentence']/span[@lang='fr']";
			const string matchEnglishTranslation =
				"//span[@class='senses']/span[@class='sensecontent']/span[@class='sense']/span[@class='examples']/span[@class='example']/span[@class='translations']/span[@class='translation']/span[@class='translatedsentence']/span[@lang='en']";

			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				//SUT
				Assert.DoesNotThrow (
					() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entryCorps, mainEntryNode, pubEverything, settings));
				XHTMLWriter.Flush();
				var output = XHTMLStringBuilder.ToString();
				Assert.IsNotNullOrEmpty(output);
				// Verify that the unfiltered output displays everything.
				AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFrenchEntry, 1);
				AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchEnglishDefOrGloss, 2);
				AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFrenchExample, 3);
				AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchEnglishTranslation, 3);
			}
			XHTMLStringBuilder.Clear();
			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				//SUT
				Assert.DoesNotThrow (
					() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entryCorps, mainEntryNode, pubMain, settings));
				XHTMLWriter.Flush();
				var output = XHTMLStringBuilder.ToString();
				Assert.IsNotNullOrEmpty(output);
				// Verify that the main publication output displays what it should.
				AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFrenchEntry, 1);
				AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchEnglishDefOrGloss, 1);
				AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFrenchExample, 1);
				AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchEnglishTranslation, 1);
				const string matchBodyIsBig =
					"//span[@class='examples']/span[@class='example']/span[@class='translations']/span[@class='translation']/span[@class='translatedsentence']/span[@lang='en' and text()='The body is big.']";
				AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchBodyIsBig, 1);
			}
			XHTMLStringBuilder.Clear();
			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				//SUT
				Assert.DoesNotThrow (
					() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entryCorps, mainEntryNode, pubTest, settings));
				XHTMLWriter.Flush();
				var output = XHTMLStringBuilder.ToString();
				Assert.IsNotNullOrEmpty(output);
				// Verify that the test publication output displays what it should.
				AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFrenchEntry, 1);
				AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchEnglishDefOrGloss, 1);
				AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFrenchExample, 1);
				AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchEnglishTranslation, 1);
				const string matchCorpseIsDead =
					"//span[@class='examples']/span[@class='example']/span[@class='translations']/span[@class='translation']/span[@class='translatedsentence']/span[@lang='en' and text()='The corpse is dead.']";
				AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchCorpseIsDead, 1);
			}

			XHTMLStringBuilder.Clear();
			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				//SUT
				Assert.DoesNotThrow (
					() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entryBras, mainEntryNode, pubEverything, settings));
				XHTMLWriter.Flush();
				var output = XHTMLStringBuilder.ToString();
				Assert.IsNotNullOrEmpty(output);
				// Verify that the unfiltered output displays everything.
				AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFrenchEntry, 1);
				AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchEnglishDefOrGloss, 2);
				AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFrenchExample, 2);
				AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchEnglishTranslation, 2);
			}
			XHTMLStringBuilder.Clear();
			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				//SUT
				Assert.DoesNotThrow (
					() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entryBras, mainEntryNode, pubMain, settings));
				XHTMLWriter.Flush();
				var output = XHTMLStringBuilder.ToString();
				Assert.IsNotNullOrEmpty(output);
				// Verify that the main publication output displays everything.
				AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFrenchEntry, 1);
				AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchEnglishDefOrGloss, 2);
				AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFrenchExample, 2);
				AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchEnglishTranslation, 2);
			}
			XHTMLStringBuilder.Clear();
			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				//SUT
				// We can still produce test publication output for the entry since we have a copy of it.  Its senses and
				// examples should not be displayed because the senses are separately hidden.
				Assert.DoesNotThrow (
					() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entryBras, mainEntryNode, pubTest, settings));
				XHTMLWriter.Flush();
				var output = XHTMLStringBuilder.ToString();
				Assert.IsNotNullOrEmpty(output);
				// Verify that the test output doesn't display the senses and examples.
				AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFrenchEntry, 1);
				AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchEnglishDefOrGloss, 0);
				AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFrenchExample, 0);
				AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchEnglishTranslation, 0);
			}

			XHTMLStringBuilder.Clear();
			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				//SUT
				Assert.DoesNotThrow (
					() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entryOreille, mainEntryNode, pubEverything, settings));
				XHTMLWriter.Flush();
				var output = XHTMLStringBuilder.ToString();
				Assert.IsNotNullOrEmpty(output);
				// Verify that the unfiltered output displays everything.
				AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFrenchEntry, 1);
				AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchEnglishDefOrGloss, 1);
				AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFrenchExample, 1);
				AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchEnglishTranslation, 1);
			}
			XHTMLStringBuilder.Clear();
			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				//SUT
				// We can still produce main publication output for the entry since we have a copy of it.  Its sense and
				// example should not be displayed because the sense is separately hidden.
				Assert.DoesNotThrow (
					() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entryOreille, mainEntryNode, pubMain, settings));
				XHTMLWriter.Flush();
				var output = XHTMLStringBuilder.ToString();
				Assert.IsNotNullOrEmpty(output);
				// Verify that the test output doesn't display the sense and example.
				AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFrenchEntry, 1);
				AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchEnglishDefOrGloss, 0);
				AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFrenchExample, 0);
				AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchEnglishTranslation, 0);
			}
			XHTMLStringBuilder.Clear();
			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				//SUT
				Assert.DoesNotThrow (
					() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entryOreille, mainEntryNode, pubTest, settings));
				XHTMLWriter.Flush();
				var output = XHTMLStringBuilder.ToString();
				Assert.IsNotNullOrEmpty(output);
				// Verify that the test publication output displays everything.
				AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFrenchEntry, 1);
				AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchEnglishDefOrGloss, 1);
				AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFrenchExample, 1);
				AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchEnglishTranslation, 1);
			}

			var matchFrenchSubentry = "//span[@class='subentries']/span[@class='subentrie']/span[@class='subentry']/span[@lang='fr']";
			var matchMainsubentry = "//span[@class='subentries']/span[@class='subentrie']/span[@class='subentry']/span[@lang='fr'and text()='mainsubentry']";
			var matchTestsubentry = "//span[@class='subentries']/span[@class='subentrie']/span[@class='subentry']/span[@lang='fr'and text()='testsubentry']";
			XHTMLStringBuilder.Clear();
			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				//SUT
				Assert.DoesNotThrow (
					() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entryEntry, mainEntryNode, pubMain, settings));
				XHTMLWriter.Flush();
				var output = XHTMLStringBuilder.ToString();
				Assert.IsNotNullOrEmpty(output);
				// Verify that the main publication output displays what it should.
				AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFrenchEntry, 1);
				AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchEnglishDefOrGloss, 1);
				AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFrenchSubentry, 1);
				AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchMainsubentry, 1);
				AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchTestsubentry, 0);
			}
			XHTMLStringBuilder.Clear();
			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				//SUT
				Assert.DoesNotThrow (
					() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entryEntry, mainEntryNode, pubTest, settings));
				XHTMLWriter.Flush();
				var output = XHTMLStringBuilder.ToString();
				Assert.IsNotNullOrEmpty(output);
				// Verify that the test publication output displays what it should.
				AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFrenchEntry, 1);
				AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchEnglishDefOrGloss, 1);
				AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFrenchSubentry, 1);
				AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchMainsubentry, 0);
				AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchTestsubentry, 1);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_ComplexFormAndSenseInPara()
		{
			var lexentry = CreateInterestingLexEntry(Cache);

			var subentry = CreateInterestingLexEntry(Cache);
			var subentryRef = CreateComplexForm(lexentry, subentry, true);

			var complexRefRevAbbr = subentryRef.ComplexEntryTypesRS[0].ReverseAbbr.BestAnalysisAlternative.Text;

			var revAbbrevNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ReverseAbbr",
				IsEnabled = true,
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" })
			};
			var refTypeNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LookupComplexEntryType",
				IsEnabled = true,
				CSSClassNameOverride = "complexformtypes",
				Children = new List<ConfigurableDictionaryNode> { revAbbrevNode }
			};
			var subentryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { refTypeNode },
				DictionaryNodeOptions = new DictionaryNodeComplexFormOptions{DisplayEachComplexFormInAParagraph = true},
				FieldDescription = "Subentries",
				IsEnabled = true
			};
			var glossNode = new ConfigurableDictionaryNode { FieldDescription = "Gloss", IsEnabled = true, DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" }) };
			var SenseNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				Label = "Senses",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions{DisplayEachSenseInAParagraph = true},
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { glossNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { SenseNode, subentryNode },
				FieldDescription = "LexEntry",
				IsEnabled = true
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });

			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				//SUT
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(lexentry, mainEntryNode, null, settings));
				XHTMLWriter.Flush();
				const string senseXpath = "div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense']/span[@class='gloss']/span[@lang='en' and text()='gloss']";
				var paracontinuationxpath = string.Format(
					"div[@class='lexentry']/div[@class='paracontinuation']//span[@class='subentries']/span[@class='subentrie']/span[@class='complexformtypes']/span[@class='complexformtype']/span[@class='reverseabbr']/span[@lang='en' and text()='{0}']",
					complexRefRevAbbr);
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(senseXpath, 1);
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(paracontinuationxpath, 1);
			}
		}

		[Test]
		public void SavePublishedHtmlWithStyles_ProduceLetHeadOnlyWhenDesired()
		{
			var lexentry1 = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(lexentry1, "femme", m_wsFr, Cache);
			var lexentry2 = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(lexentry2, "homme", m_wsFr, Cache);
			int flidVirtual = Cache.ServiceLocator.GetInstance<Virtuals>().LexDbEntries;
			var pubEverything = new DictionaryPublicationDecorator(Cache, (ISilDataAccessManaged)Cache.MainCacheAccessor, flidVirtual);
			var glossNode = new ConfigurableDictionaryNode { FieldDescription = "Gloss", IsEnabled = true, DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" }) };
			var SenseNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				Label = "Senses",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions{DisplayEachSenseInAParagraph = true},
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { glossNode }
			};
			var mainHeadwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "HeadWord",
				Label = "Headword",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" }),
				CSSClassNameOverride = "entry",
				IsEnabled = true
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { mainHeadwordNode, SenseNode },
				FieldDescription = "LexEntry",
				IsEnabled = true
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });
			var model = new DictionaryConfigurationModel
			{
				Parts = new List<ConfigurableDictionaryNode> { mainEntryNode }
			};
			var xhtmlPath = Path.GetTempFileName();
			File.Delete(xhtmlPath);
			xhtmlPath = Path.ChangeExtension(xhtmlPath, "xhtml");
			var cssPath = Path.ChangeExtension(xhtmlPath, "css");
			var xpath = "//div[@class='letHead']";
			try
			{
				ConfiguredXHTMLGenerator.SavePublishedHtmlWithStyles(new int[] { lexentry1.Hvo, lexentry2.Hvo }, pubEverything, model, m_mediator, xhtmlPath, cssPath);
				var xhtml = File.ReadAllText(xhtmlPath);
				AssertThatXmlIn.String(xhtml).HasSpecifiedNumberOfMatchesForXpath(xpath, 2);

				ConfiguredXHTMLGenerator.SavePublishedHtmlWithStyles(new int[] { lexentry1.Hvo, lexentry2.Hvo }, null, model, m_mediator, xhtmlPath, cssPath);
				xhtml = File.ReadAllText(xhtmlPath);
				AssertThatXmlIn.String(xhtml).HasSpecifiedNumberOfMatchesForXpath(xpath, 2);

				ConfiguredXHTMLGenerator.SavePublishedHtmlWithStyles(new int[] { lexentry1.Hvo }, pubEverything, model, m_mediator, xhtmlPath, cssPath);
				xhtml = File.ReadAllText(xhtmlPath);
				AssertThatXmlIn.String(xhtml).HasSpecifiedNumberOfMatchesForXpath(xpath, 1);

				ConfiguredXHTMLGenerator.SavePublishedHtmlWithStyles(new int[] { lexentry1.Hvo }, null, model, m_mediator, xhtmlPath, cssPath);
				xhtml = File.ReadAllText(xhtmlPath);
				AssertThatXmlIn.String(xhtml).HasSpecifiedNumberOfMatchesForXpath(xpath, 0);
			}
			finally
			{
				File.Delete(xhtmlPath);
				File.Delete(cssPath);
			}
		}

		#region Helpers

		/// <summary>Creates a DictionaryConfigurationModel with one Main and two Minor Entry nodes, all with enabled HeadWord children</summary>
		private static DictionaryConfigurationModel CreateInterestingConfigurationModel()
		{
			var mainHeadwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "HeadWord",
				Label = "Headword",
				CSSClassNameOverride = "entry",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" }),
				Before = "MainEntry: ",
				IsEnabled = true
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { mainHeadwordNode },
				FieldDescription = "LexEntry",
				IsEnabled = true
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });

			var minorEntryNode = mainEntryNode.DeepCloneUnderSameParent();
			minorEntryNode.CSSClassNameOverride = "minorentry";
			minorEntryNode.Before = "MinorEntry: ";

			var minorSecondNode = minorEntryNode.DeepCloneUnderSameParent();
			minorSecondNode.Before = "HalfStep: ";

			return new DictionaryConfigurationModel
			{
				AllPublications = true,
				Parts = new List<ConfigurableDictionaryNode> { mainEntryNode, minorEntryNode, minorSecondNode }
			};
		}

		private static ConfigurableDictionaryNode CreatePictureModel()
		{
			var thumbNailNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "PictureFileRA",
				CSSClassNameOverride = "picture",
				IsEnabled = true
			};
			var pictureNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "PicturesOfSenses",
				IsEnabled = true,
				CSSClassNameOverride = "Pictures",
				Children = new List<ConfigurableDictionaryNode> { thumbNailNode }
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Senses",
				Label = "Senses",
				IsEnabled = true,
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { sensesNode, pictureNode },
				FieldDescription = "LexEntry",
				IsEnabled = true
			};
			return mainEntryNode;
		}

		internal static ILexEntry CreateInterestingLexEntry(FdoCache cache)
		{
			var factory = cache.ServiceLocator.GetInstance<ILexEntryFactory>();
			var entry = factory.Create();
			cache.LangProject.AddToCurrentAnalysisWritingSystems(
				cache.WritingSystemFactory.get_Engine("en") as IWritingSystem);
			cache.LangProject.AddToCurrentVernacularWritingSystems(
				cache.WritingSystemFactory.get_Engine("fr") as IWritingSystem);
			var wsEn = cache.WritingSystemFactory.GetWsFromStr("en");
			var wsFr = cache.WritingSystemFactory.GetWsFromStr("fr");
			AddHeadwordToEntry(entry, "Citation", wsFr, cache);
			entry.Comment.set_String(wsEn, cache.TsStrFactory.MakeString("Comment", wsEn));
			AddSenseToEntry(entry, "gloss", wsEn, cache);
			return entry;
		}

		private ILexEntryRef CreateVariantForm(ILexEntry main, ILexEntry variantForm)
		{
			var owningList = Cache.LangProject.LexDbOA.VariantEntryTypesOA;
			Assert.IsNotNull(owningList, "No VariantEntryTypes property on Lexicon object.");
			var ws = Cache.DefaultAnalWs;
			var varType = Cache.ServiceLocator.GetInstance<ILexEntryTypeFactory>().Create();
			owningList.PossibilitiesOS.Add(varType);
			varType.Name.set_String(ws, TestVariantName);
			return variantForm.MakeVariantOf(main, varType);
		}

		private ILexEntryRef CreateComplexForm(ICmObject main, ILexEntry complexForm, bool subentry)
		{
			var complexEntryRef = Cache.ServiceLocator.GetInstance<ILexEntryRefFactory>().Create();
			complexForm.EntryRefsOS.Add(complexEntryRef);
			var complexEntryType = (ILexEntryType) Cache.LangProject.LexDbOA.ComplexEntryTypesOA.PossibilitiesOS[0];
			var complexEntryTypeAbbrText = complexEntryType.Abbreviation.BestAnalysisAlternative.Text;
			var complexEntryTypeRevAbbr = complexEntryType.ReverseAbbr;
			// If there is no reverseAbbr, generate one from the forward abbr (e.g. "comp. of") by trimming the trailing " of"
			if(complexEntryTypeRevAbbr.BestAnalysisAlternative.Equals(complexEntryTypeRevAbbr.NotFoundTss))
				complexEntryTypeRevAbbr.SetAnalysisDefaultWritingSystem(complexEntryTypeAbbrText.Substring(0, complexEntryTypeAbbrText.Length - 3));
			complexEntryRef.ComplexEntryTypesRS.Add(complexEntryType);
			complexEntryRef.RefType = LexEntryRefTags.krtComplexForm;
			complexEntryRef.ComponentLexemesRS.Add(main);
			if (subentry)
				complexEntryRef.PrimaryLexemesRS.Add(main);
			else
				complexEntryRef.ShowComplexFormsInRS.Add(main);
			return complexEntryRef;
		}

		/// <summary>
		/// Generates a Lexical Reference.
		/// If refTypeReverseName is specified, generates a Ref of an Asymmetric Type (EntryOrSenseTree) with the specified reverse name;
		/// otherwise, generates a Ref of a Symmetric Type (EntryOrSenseSequence).
		/// </summary>
		private void CreateLexicalReference(ICmObject mainEntry, ICmObject referencedForm, string refTypeName, string refTypeReverseName = null)
		{
			CreateLexicalReference(mainEntry, referencedForm, null, refTypeName, refTypeReverseName);
		}

		private void CreateLexicalReference(ICmObject firstEntry, ICmObject secondEntry, ICmObject thirdEntry, string refTypeName, string refTypeReverseName = null)
		{
			var lrt = Cache.ServiceLocator.GetInstance<ILexRefTypeFactory>().Create();
			if(Cache.LangProject.LexDbOA.ReferencesOA == null)
				Cache.LangProject.LexDbOA.ReferencesOA = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
			Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.Add(lrt);
			lrt.Name.set_String(Cache.DefaultAnalWs, refTypeName);
			if(string.IsNullOrEmpty(refTypeReverseName))
			{
				lrt.MappingType = (int)MappingTypes.kmtEntryOrSenseSequence;
			}
			else
			{
				lrt.ReverseName.set_String(Cache.DefaultAnalWs, refTypeReverseName);
				lrt.MappingType = (int)MappingTypes.kmtEntryOrSenseTree;
			}
			var lexRef = Cache.ServiceLocator.GetInstance<ILexReferenceFactory>().Create();
			lrt.MembersOC.Add(lexRef);
			lexRef.TargetsRS.Add(firstEntry);
			lexRef.TargetsRS.Add(secondEntry);
			if (thirdEntry != null)
				lexRef.TargetsRS.Add(thirdEntry);
		}

		private ICmPossibility CreatePublicationType(string name)
		{
			if (Cache.LangProject.LexDbOA.PublicationTypesOA == null)
				Cache.LangProject.LexDbOA.PublicationTypesOA = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
			var item = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
			Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS.Add(item);
			item.Name.set_String(m_wsEn, name);
			Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS.Add(item);
			return item;
		}

		private static void AddHeadwordToEntry(ILexEntry entry, string headword, int wsId, FdoCache cache)
		{
			// The headword field is special: it uses Citation if available, or LexemeForm if Citation isn't filled in
			entry.CitationForm.set_String(wsId, cache.TsStrFactory.MakeString(headword, wsId));
		}

		private static void AddSenseToEntry(ILexEntry entry, string gloss, int wsId, FdoCache cache)
		{
			var senseFactory = cache.ServiceLocator.GetInstance<ILexSenseFactory>();
			var sense = senseFactory.Create();
			entry.SensesOS.Add(sense);
			sense.Gloss.set_String(wsId, cache.TsStrFactory.MakeString(gloss, wsId));
		}

		private void AddSubSenseToSense(ILexEntry entry, string gloss)
		{
			var senseFactory = Cache.ServiceLocator.GetInstance<ILexSenseFactory>();
			var sense = senseFactory.Create();
			entry.SensesOS.Add(sense);
			sense.Gloss.set_String(m_wsEn, Cache.TsStrFactory.MakeString(gloss, m_wsEn));
			var subSensesOne = senseFactory.Create();
			sense.SensesOS.Add(subSensesOne);
			subSensesOne.Gloss.set_String(m_wsEn, Cache.TsStrFactory.MakeString(gloss + "2.1", m_wsEn));
			var subSensesTwo = senseFactory.Create();
			sense.SensesOS.Add(subSensesTwo);
			subSensesTwo.Gloss.set_String(m_wsEn, Cache.TsStrFactory.MakeString(gloss + "2.2", m_wsEn));
		}

		private void AddSingleSubSenseToSense(ILexEntry entry, string gloss,ILexSense sense)
		{
			sense.Gloss.set_String(m_wsEn, Cache.TsStrFactory.MakeString(gloss, m_wsEn));
			var subSensesOne = sense.Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			sense.SensesOS.Add(subSensesOne);
			subSensesOne.Gloss.set_String(m_wsEn, Cache.TsStrFactory.MakeString(gloss + "1.1", m_wsEn));
		}
		private ILexExampleSentence AddExampleToSense(ILexSense sense, string content, string translation = null)
		{
			var exampleFact = Cache.ServiceLocator.GetInstance<ILexExampleSentenceFactory>();
			var example = exampleFact.Create(new Guid(), sense);
			example.Example.set_String(m_wsFr, Cache.TsStrFactory.MakeString(content, m_wsFr));
			if (translation != null)
			{
				var type = Cache.ServiceLocator.GetInstance<ICmPossibilityRepository>().GetObject(CmPossibilityTags.kguidTranFreeTranslation);
				var cmTranslation = Cache.ServiceLocator.GetInstance<ICmTranslationFactory>().Create(example, type);
				cmTranslation.Translation.set_String(m_wsEn, Cache.TsStrFactory.MakeString(translation, m_wsEn));
				example.TranslationsOC.Add(cmTranslation);
			}
			return example;
		}

		private IMoForm AddAllomorphToEntry(ILexEntry entry)
		{
			var morphFact = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>();
			var morph = morphFact.Create();
			entry.AlternateFormsOS.Add(morph);
			morph.Form.set_String(m_wsFr, Cache.TsStrFactory.MakeString("Allomorph", m_wsFr));

			// add environment to the allomorph
			const int stringRepresentationFlid = 5097008;
			var env = Cache.ServiceLocator.GetInstance<IPhEnvironmentFactory>().Create();
			Cache.LangProject.PhonologicalDataOA.EnvironmentsOS.Add(env);
			morph.PhoneEnvRC.Add(env);
			Cache.MainCacheAccessor.SetString(env.Hvo, stringRepresentationFlid, Cache.TsStrFactory.MakeString("phoneyEnv", m_wsEn));

			return morph;
		}

		IStText CreateMultiParaText(string content, FdoCache cache)
		{
			var text = cache.ServiceLocator.GetInstance<ITextFactory>().Create();
			//cache.LangProject.
			var stText = cache.ServiceLocator.GetInstance<IStTextFactory>().Create();
			cache.LangProject.InterlinearTexts.Add(stText);
			text.ContentsOA = stText;
			var para = cache.ServiceLocator.GetInstance<IStTxtParaFactory>().Create();
			stText.ParagraphsOS.Add(para);
			para.Contents = MakeVernTss("First para " + content, cache);
			var para1 = cache.ServiceLocator.GetInstance<IStTxtParaFactory>().Create();
			stText.ParagraphsOS.Add(para1);
			para1.Contents = MakeVernTss("Second para " + content, cache);
			return text.ContentsOA;
		}

		private ITsString MakeVernTss(string content,FdoCache cache)
		{
			return Cache.TsStrFactory.MakeString(content, Cache.DefaultVernWs);
		}

		private static void SetPublishAsMinorEntry(ILexEntry entry, bool publish)
		{
			foreach (var ler in entry.EntryRefsOS)
				ler.HideMinorEntry = publish ? 0 : 1;
		}

		public static DictionaryNodeOptions GetWsOptionsForLanguages(string[] languages)
		{
			var wsOptions = new DictionaryNodeWritingSystemOptions { Options = DictionaryDetailsControllerTests.ListOfEnabledDNOsFromStrings(languages) };
			return wsOptions;
		}

		public static DictionaryNodeOptions GetWsOptionsForLanguageswithDisplayWsAbbrev(string[] languages)
		{
			var wsOptions = new DictionaryNodeWritingSystemOptions { Options = DictionaryDetailsControllerTests.ListOfEnabledDNOsFromStrings(languages),DisplayWritingSystemAbbreviations = true};
			return wsOptions;
		}

		public static DictionaryNodeOptions GetListOptionsForItems(DictionaryNodeListOptions.ListIds listName, ICmPossibility[] checkedItems)
		{
			var listOptions = new DictionaryNodeListOptions {
				ListId = listName,
				Options = DictionaryDetailsControllerTests.ListOfEnabledDNOsFromStrings (checkedItems.Select (id => id.Guid.ToString()).ToList())
			};
			return listOptions;
		}

		public static DictionaryNodeOptions GetListOptionsForStrings(DictionaryNodeListOptions.ListIds listName, IEnumerable<string> checkedItems)
		{
			var listOptions = new DictionaryNodeListOptions {
				ListId = listName,
				Options = DictionaryDetailsControllerTests.ListOfEnabledDNOsFromStrings(checkedItems)
			};
			return listOptions;
		}

		public DictionaryNodeOptions GetFullyEnabledListOptions(DictionaryNodeListOptions.ListIds listName, bool isComplex = false)
		{
			List<DictionaryNodeListOptions.DictionaryNodeOption> dnoList;
			switch (listName)
			{
				case DictionaryNodeListOptions.ListIds.Minor:
					dnoList = DictionaryDetailsControllerTests.ListOfEnabledDNOsFromStrings(
						new [] { XmlViewsUtils.GetGuidForUnspecifiedVariantType(), XmlViewsUtils.GetGuidForUnspecifiedComplexFormType() }
							.Select(guid => guid.ToString())
						.Union(Cache.LangProject.LexDbOA.ComplexEntryTypesOA.PossibilitiesOS
						.Union(Cache.LangProject.LexDbOA.VariantEntryTypesOA.PossibilitiesOS).Select(item => item.Guid.ToString())));
					break;
				case DictionaryNodeListOptions.ListIds.Variant:
					dnoList = DictionaryDetailsControllerTests.ListOfEnabledDNOsFromStrings(
						new [] { XmlViewsUtils.GetGuidForUnspecifiedVariantType().ToString() }
						.Union(Cache.LangProject.LexDbOA.VariantEntryTypesOA.PossibilitiesOS.Select(item => item.Guid.ToString())));
					break;
				case DictionaryNodeListOptions.ListIds.Complex:
					dnoList = DictionaryDetailsControllerTests.ListOfEnabledDNOsFromStrings(
						new [] { XmlViewsUtils.GetGuidForUnspecifiedComplexFormType().ToString() }
						.Union(Cache.LangProject.LexDbOA.ComplexEntryTypesOA.PossibilitiesOS.Select(item => item.Guid.ToString())));
					break;
				default:
					throw new NotImplementedException(string.Format("Unknown list id {0}", listName));
			}

			DictionaryNodeListOptions listOptions = isComplex ? new DictionaryNodeComplexFormOptions() : new DictionaryNodeListOptions();

			listOptions.ListId = listName;
			listOptions.Options = dnoList;
			return listOptions;
		}

		/// <summary>
		/// Search haystack with regexQuery, and assert that requiredNumberOfMatches matches are found.
		/// Can be used in place of AssertThatXmlIn.String().HasSpecifiedNumberOfMatchesForXpath(),
		/// when slashes are needed in an argument to xpath starts-with.
		/// </summary>
		private static void AssertRegex(string haystack, string regexQuery, int requiredNumberOfMatches)
		{
			var regex = new Regex(regexQuery);
			var matches = regex.Matches(haystack);
			Assert.That(matches.Count, Is.EqualTo(requiredNumberOfMatches), "Unexpected number of matches");
		}
		#endregion Helpers
	}

	#region Test classes and interfaces for testing the reflection code in GetPropertyTypeForConfigurationNode
	class TestRootClass
	{
		public ITestInterface RootMember { get; set; }
		public TestNonInterface ConcreteMember { get; set; }
	}

	interface ITestInterface : ITestBaseOne, ITestBaseTwo
	{
		string TestString { get; }
	}

	interface ITestBaseOne
	{
		IMoForm TestMoForm { get; }
	}

	interface ITestBaseTwo : ITestGrandParent
	{
		ICmObject TestIcmObject { get; }
	}

	class TestNonInterface
	{
// ReSharper disable UnusedMember.Local // Justification: called by reflection
		string TestNonInterfaceString { get; set; }
// ReSharper restore UnusedMember.Local
	}

	interface ITestGrandParent
	{
		Stack<TestRootClass> TestCollection { get; }
	}

	class TestPictureClass
	{
		public IFdoList<ICmPicture> Pictures { get; set; }
	}
	#endregion
}
