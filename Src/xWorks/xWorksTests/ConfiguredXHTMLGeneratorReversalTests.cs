// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using NUnit.Framework;
using Palaso.TestUtilities;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.FDO.DomainImpl;
using SIL.FieldWorks.FDO.FDOTests;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	// ReSharper disable InconsistentNaming
	[TestFixture]
	internal class ConfiguredXHTMLGeneratorReversalTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase, IDisposable
	{
		private int m_wsEn, m_wsFr;

		private FwXApp m_application;
		private FwXWindow m_window;
		private Mediator m_mediator;

		private StringBuilder XHTMLStringBuilder { get; set; }

		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			FwRegistrySettings.Init();
			m_application = new MockFwXApp(new MockFwManager { Cache = Cache }, null, null);
			var m_configFilePath = Path.Combine(FwDirectoryFinder.CodeDirectory,
				m_application.DefaultConfigurationPathname);
			m_window = new MockFwXWindow(m_application, m_configFilePath);
			((MockFwXWindow)m_window).Init(Cache); // initializes Mediator values
			m_mediator = m_window.Mediator;
			// Set up the mediator to look as if we are working in the Reversal Index area
			m_mediator.PropertyTable.SetProperty("ToolForAreaNamed_lexicon", "reversalEditComplete");
			Cache.ProjectId.Path = Path.Combine(FwDirectoryFinder.SourceDirectory,
				"xWorks/xWorksTests/TestData/");
			m_wsEn = Cache.WritingSystemFactory.GetWsFromStr("en");
			m_wsFr = Cache.WritingSystemFactory.GetWsFromStr("fr");
		}

		[TestFixtureTearDown]
		public override void FixtureTeardown()
		{
			base.FixtureTeardown();
			Dispose();
		}

		[SetUp]
		public void SetupExportVariables()
		{
			XHTMLStringBuilder = new StringBuilder();
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

		~ConfiguredXHTMLGeneratorReversalTests()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
		}
		#endregion disposal

		[Test]
		public void GenerateXHTMLForEntry_LexemeFormConfigurationGeneratesCorrectResult()
		{
			var reversalFormNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ReversalForm",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new [] {"en"}),
				Label = "Reversal Form",
				IsEnabled = true
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { reversalFormNode },
				FieldDescription = "ReversalIndexEntry",
				IsEnabled = true
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });
			var entry = CreateInterestingEnglishReversalEntry();
			using(var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				//SUT
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entry, mainEntryNode, null, settings));
				XHTMLWriter.Flush();
				const string frenchLexForm = "/div[@class='reversalindexentry']/span[@class='reversalform']/span[@lang='en' and text()='ReversalForm']";
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(frenchLexForm, 1);
			}
		}

		[Test]
		public void GenerateLetterHeaderIfNeeded_GeneratesHeaderIfNoPreviousHeader()
		{
			var entry = CreateInterestingEnglishReversalEntry();
			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				// SUT
				string last = null;
				XHTMLWriter.WriteStartElement("TestElement");
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateLetterHeaderIfNeeded(entry, ref last, XHTMLWriter, Cache));
				XHTMLWriter.WriteEndElement();
				XHTMLWriter.Flush();
				const string letterHeaderToMatch = "//div[@class='letHead']/div[@class='letter' and text()='R r']";
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(letterHeaderToMatch, 1);
				Assert.AreEqual("r", last, "should have updated the last letter header");
			}
		}

		[Test]
		public void GenerateLetterHeaderIfNeeded_GeneratesHeaderIfPreviousHeaderDoesNotMatch()
		{
			var entry = CreateInterestingEnglishReversalEntry();
			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				// SUT
				var last = "A a";
				XHTMLWriter.WriteStartElement("TestElement");
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateLetterHeaderIfNeeded(entry, ref last, XHTMLWriter, Cache));
				XHTMLWriter.WriteEndElement();
				XHTMLWriter.Flush();
				const string letterHeaderToMatch = "//div[@class='letHead']/div[@class='letter' and text()='R r']";
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(letterHeaderToMatch, 1);
			}
		}

		[Test]
		public void GenerateLetterHeaderIfNeeded_GeneratesNoHeaderIfPreviousHeaderDoesMatch()
		{
			var entry = CreateInterestingEnglishReversalEntry();
			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				// SUT
				var last = "A a";
				XHTMLWriter.WriteStartElement("TestElement");
				ConfiguredXHTMLGenerator.GenerateLetterHeaderIfNeeded(entry, ref last, XHTMLWriter, Cache);
				ConfiguredXHTMLGenerator.GenerateLetterHeaderIfNeeded(entry, ref last, XHTMLWriter, Cache);
				XHTMLWriter.WriteEndElement();
				XHTMLWriter.Flush();
				const string letterHeaderToMatch = "//div[@class='letHead']/div[@class='letter' and text()='R r']";
				const string proveOnlyOneHeader = "//div[@class='letHead']/div[@class='letter']";
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(letterHeaderToMatch, 1);
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(proveOnlyOneHeader, 1);
			}
		}


		[Test]
		public void GenerateXHTMLForEntry_ReversalStringGeneratesContent()
		{
			var formNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ReversalForm",
				Label = "Form",
				DictionaryNodeOptions = new DictionaryNodeWritingSystemOptions
				{
					WsType = DictionaryNodeWritingSystemOptions.WritingSystemType.Reversal,
					Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>
					{
						new DictionaryNodeListOptions.DictionaryNodeOption { Id = "fr", IsEnabled = true,}
					},
					DisplayWritingSystemAbbreviations = false
				},
				IsEnabled = true
			};
			var reversalNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { formNode },
				FieldDescription = "ReversalIndexEntry",
				IsEnabled = true
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { reversalNode });
			var rie = CreateInterestingFrenchReversalEntry() as IReversalIndexEntry;
			var entryHeadWord = rie.ReferringSenses.First().Entry.HeadWord;

			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				//SUT
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(rie, reversalNode, null, settings));
				XHTMLWriter.Flush();
				var result = XHTMLStringBuilder.ToString();
				var reversalFormDataPath = string.Format("/div[@class='reversalindexentry']/span[@class='reversalform']/span[text()='{0}']", rie.LongName);
				var entryDataPath = string.Format("//span[text()='{0}']", entryHeadWord.Text);
				AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(reversalFormDataPath, 1);
				AssertThatXmlIn.String(result).HasNoMatchForXpath(entryDataPath);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_SenseNumbersGeneratedForMultipleReferringSenses()
		{
			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLOwnerOutlineName",
				CSSClassNameOverride = "headword",
				DictionaryNodeOptions = new ReferringSenseOptions()
				{
					WritingSystemOptions = new DictionaryNodeWritingSystemOptions
					{
						WsType = DictionaryNodeWritingSystemOptions.WritingSystemType.Vernacular,
						Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>
						{
							new DictionaryNodeListOptions.DictionaryNodeOption {Id = "vernacular", IsEnabled = true}
						}
					},
					SenseOptions = new DictionaryNodeSenseOptions
					{
						NumberEvenASingleSense = true,
						NumberingStyle = "%A"
					}
				},
				IsEnabled = true,
			};
			var wsOpts = new DictionaryNodeWritingSystemOptions
			{
				Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>
				{
					new DictionaryNodeListOptions.DictionaryNodeOption { Id = "en", IsEnabled = true }
				}
			};
			var glossNode = new ConfigurableDictionaryNode { FieldDescription = "Gloss", DictionaryNodeOptions = wsOpts, IsEnabled = true };
			var formNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ReferringSenses",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions { NumberingStyle = "%d" },
				Children = new List<ConfigurableDictionaryNode> {headwordNode, glossNode},
				IsEnabled = true
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				DictionaryNodeOptions = new DictionaryNodeWritingSystemOptions
				{
					WsType = DictionaryNodeWritingSystemOptions.WritingSystemType.Reversal,
					Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>
					{
						new DictionaryNodeListOptions.DictionaryNodeOption {Id = "en", IsEnabled = true,}
					},
					DisplayWritingSystemAbbreviations = false,
				},
				FieldDescription = "ReversalIndexEntry",
				Children = new List<ConfigurableDictionaryNode> { formNode },
				IsEnabled = true
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });
			var testEntry = CreateInterestingEnglishReversalEntry();
			AddSenseToReversaEntry(testEntry, "second gloss", m_wsEn, Cache);
			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				//SUT
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, null, settings));
				XHTMLWriter.Flush();
				const string senseNumberOne = "/div[@class='reversalindexentry']/span[@class='referringsenses']/span[@class='sensecontent']/span[@class='referringsense' and preceding-sibling::span[@class='sensenumber' and text()='1']]//span[@lang='en' and text()='gloss']";
				const string senseNumberTwo = "/div[@class='reversalindexentry']/span[@class='referringsenses']/span[@class='sensecontent']/span[@class='referringsense' and preceding-sibling::span[@class='sensenumber' and text()='2']]//span[@lang='en' and text()='second gloss']";
				//This assert is dependent on the specific entry data created in CreateInterestingEnglishReversalEntry
				var xhtml = XHTMLStringBuilder.ToString();
				AssertThatXmlIn.String(xhtml).HasSpecifiedNumberOfMatchesForXpath(senseNumberOne, 1);
				AssertThatXmlIn.String(xhtml).HasSpecifiedNumberOfMatchesForXpath(senseNumberTwo, 1);

				const string headwordOne = "/div[@class='reversalindexentry']/span[@class='referringsenses']/span[@class='sensecontent']/span[@class='referringsense']/span[@class='headword']/a/span[@lang='fr' and text()='Citation' and following-sibling::span[@lang='fr' and text()='1']]";
				const string headwordTwo = "/div[@class='reversalindexentry']/span[@class='referringsenses']/span[@class='sensecontent']/span[@class='referringsense']/span[@class='headword']/a/span[@lang='fr' and text()='Citation' and following-sibling::span[@lang='fr' and text()='2']]";
				const string headwordSenseOne = "/div[@class='reversalindexentry']/span[@class='referringsenses']/span[@class='sensecontent']/span[@class='referringsense']/span[@class='headword']/span[@class='referringsensenumber' and text()='A']";
				AssertThatXmlIn.String(xhtml).HasSpecifiedNumberOfMatchesForXpath(headwordOne, 1);
				AssertThatXmlIn.String(xhtml).HasSpecifiedNumberOfMatchesForXpath(headwordTwo, 1);
				AssertThatXmlIn.String(xhtml).HasSpecifiedNumberOfMatchesForXpath(headwordSenseOne, 2);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_VernacularFormWithSubSences()
		{
			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLOwnerOutlineName",
				CSSClassNameOverride = "headword",
				DictionaryNodeOptions = new ReferringSenseOptions()
				{
					WritingSystemOptions = new DictionaryNodeWritingSystemOptions
					{
						WsType = DictionaryNodeWritingSystemOptions.WritingSystemType.Vernacular,
						Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>
						{
							new DictionaryNodeListOptions.DictionaryNodeOption {Id = "vernacular", IsEnabled = true}
						}
					},
					SenseOptions = new DictionaryNodeSenseOptions
					{
						NumberEvenASingleSense = true,
						NumberingStyle = "%O"
					}
				},
				IsEnabled = true,
			};
			var wsOpts = new DictionaryNodeWritingSystemOptions
			{
				Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>
				{
					new DictionaryNodeListOptions.DictionaryNodeOption { Id = "en", IsEnabled = true }
				}
			};
			var glossNode = new ConfigurableDictionaryNode { FieldDescription = "Gloss", DictionaryNodeOptions = wsOpts, IsEnabled = true };
			var formNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ReferringSenses",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions { NumberingStyle = "%d" },
				Children = new List<ConfigurableDictionaryNode> { headwordNode, glossNode },
				IsEnabled = true
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				DictionaryNodeOptions = new DictionaryNodeWritingSystemOptions
				{
					WsType = DictionaryNodeWritingSystemOptions.WritingSystemType.Reversal,
					Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>
					{
						new DictionaryNodeListOptions.DictionaryNodeOption {Id = "en", IsEnabled = true,}
					},
					DisplayWritingSystemAbbreviations = false,
				},
				FieldDescription = "ReversalIndexEntry",
				Children = new List<ConfigurableDictionaryNode> { formNode },
				IsEnabled = true
			};
			DictionaryConfigurationModel.SpecifyParents(new List<ConfigurableDictionaryNode> { mainEntryNode });
			var testEntry = CreateInterestingEnglishReversalEntry();
			AddSingleSubSenseToSense(testEntry, "second gloss", m_wsEn, Cache);
			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				var settings = new ConfiguredXHTMLGenerator.GeneratorSettings(Cache, m_mediator, XHTMLWriter, false, false, null);
				//SUT
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, null, settings));
				XHTMLWriter.Flush();

				var xhtml = XHTMLStringBuilder.ToString();

				const string headwordSenseOne = "/div[@class='reversalindexentry']/span[@class='referringsenses']/span[@class='sensecontent']/span[@class='referringsense']/span[@class='headword']/span[@class='referringsensenumber' and text()='1.1']";
				AssertThatXmlIn.String(xhtml).HasSpecifiedNumberOfMatchesForXpath(headwordSenseOne, 1);
			}
		}

		private IReversalIndexEntry CreateInterestingFrenchReversalEntry()
		{
			var entry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			var revIndex = Cache.ServiceLocator.GetInstance<IReversalIndexRepository>().FindOrCreateIndexForWs(m_wsFr);
			var riEntry = revIndex.FindOrCreateReversalEntry("intéressant");
			entry.SensesOS.First().ReversalEntriesRC.Add(riEntry);
			return riEntry;
		}

		private IReversalIndexEntry CreateInterestingEnglishReversalEntry()
		{
			var entry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			var revIndex = Cache.ServiceLocator.GetInstance<IReversalIndexRepository>().FindOrCreateIndexForWs(m_wsEn);
			var riEntry = revIndex.FindOrCreateReversalEntry("ReversalForm");
			entry.SensesOS.First().ReversalEntriesRC.Add(riEntry);
			return riEntry;
		}
		private static void AddSenseToReversaEntry(IReversalIndexEntry riEntry, string gloss, int wsId, FdoCache cache)
		{
			var entry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(cache);
			entry.SensesOS.First().ReversalEntriesRC.Add(riEntry);
			entry.SensesOS[0].Gloss.set_String(wsId, gloss);
		}

		private static void AddSingleSubSenseToSense(IReversalIndexEntry riEntry, string gloss, int wsId, FdoCache cache)
		{
			CreateSubsenseModel(cache);
			var entry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(cache);
			var sense = entry.SensesOS.First();
			sense.Gloss.set_String(wsId, cache.TsStrFactory.MakeString(gloss, wsId));
			var subSensesOne = sense.Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			sense.SensesOS.Add(subSensesOne);
			var subGloss = "subgloss ";
			subSensesOne.Gloss.set_String(wsId, cache.TsStrFactory.MakeString(subGloss + "1.1", wsId));
			entry.SensesOS[0].SensesOS[0].Gloss.set_String(wsId, subGloss);
			entry.SensesOS.First().SensesOS[0].ReversalEntriesRC.Add(riEntry);
		}

		private static void CreateSubsenseModel(FdoCache Cache)
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
		}
	}
}