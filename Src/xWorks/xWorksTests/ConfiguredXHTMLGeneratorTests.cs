// Copyright (c) 2014-2022 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using Icu.Collation;
using NUnit.Framework;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Widgets;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.DomainImpl;
using SIL.LCModel.DomainServices;
using SIL.Linq;
using SIL.PlatformUtilities;
using SIL.TestUtilities;
using SIL.WritingSystems;
using XCore;
// ReSharper disable StringLiteralTypo

namespace SIL.FieldWorks.XWorks
{
	// ReSharper disable InconsistentNaming
	[TestFixture]
	public partial class ConfiguredXHTMLGeneratorTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase, IDisposable
	{
		private int m_wsEn, m_wsFr;

		private FwXApp m_application;
		private FwXWindow m_window;
		private XCore.PropertyTable m_propertyTable;
		private Mediator m_mediator;
		private RecordClerk m_Clerk;

		private StringBuilder XHTMLStringBuilder { get; set; }
		private const string DictionaryNormal = "Dictionary-Normal";
		private BaseStyleInfo DictionaryNormalStyle { get { return FontHeightAdjuster.StyleSheetFromPropertyTable(m_propertyTable).Styles[DictionaryNormal]; } }

		private ConfiguredLcmGenerator.GeneratorSettings DefaultSettings
		{
			get { return new ConfiguredLcmGenerator.GeneratorSettings(Cache, new ReadOnlyPropertyTable(m_propertyTable), false, false, null); }
		}

		private DictionaryPublicationDecorator DefaultDecorator
		{
			get
			{
				return new DictionaryPublicationDecorator(Cache, (ISilDataAccessManaged)Cache.MainCacheAccessor,
					Cache.ServiceLocator.GetInstance<Virtuals>().LexDbEntries);
			}
		}

		[OneTimeSetUp]
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			FwRegistrySettings.Init();
			m_application = new MockFwXApp(new MockFwManager { Cache = Cache }, null, null);
			var m_configFilePath = Path.Combine(FwDirectoryFinder.CodeDirectory, m_application.DefaultConfigurationPathname);
			m_window = new MockFwXWindow(m_application, m_configFilePath);
			((MockFwXWindow)m_window).Init(Cache); // initializes Mediator values
			m_propertyTable = m_window.PropTable;
			m_mediator = m_window.Mediator;
			m_window.LoadUI(m_configFilePath); // actually loads UI here; needed for non-null stylesheet

			var styles = FontHeightAdjuster.StyleSheetFromPropertyTable(m_propertyTable).Styles;
			if (!styles.Contains(DictionaryNormal))
				styles.Add(new BaseStyleInfo { Name = DictionaryNormal });

			m_Clerk = CreateClerk();
			m_propertyTable.SetProperty("ActiveClerk", m_Clerk, false);

			m_propertyTable.SetProperty("currentContentControl", "lexiconDictionary", false);
			Cache.ProjectId.Path = Path.Combine(FwDirectoryFinder.SourceDirectory, "xWorks/xWorksTests/TestData/");
			m_wsEn = Cache.WritingSystemFactory.GetWsFromStr("en");
			m_wsFr = Cache.WritingSystemFactory.GetWsFromStr("fr");
		}

		private RecordClerk CreateClerk()
		{
			const string entryClerk = @"<?xml version='1.0' encoding='UTF-8'?>
			<root>
				<clerks>
					<clerk id='entries'>
						<recordList owner='LexDb' property='Entries'/>
					</clerk>
				</clerks>
				<tools>
					<tool label='Dictionary' value='lexiconDictionary' icon='DocumentView'>
						<control>
							<dynamicloaderinfo assemblyPath='xWorks.dll' class='SIL.FieldWorks.XWorks.XhtmlDocView'/>
							<parameters area='lexicon' clerk='entries' layout='Bartholomew' layoutProperty='DictionaryPublicationLayout' editable='false' configureObjectName='Dictionary'/>
						</control>
					</tool>
				</tools>
			</root>";
			var doc = new XmlDocument();
			doc.LoadXml(entryClerk);
			XmlNode clerkNode = doc.SelectSingleNode("//tools/tool[@label='Dictionary']//parameters[@area='lexicon']");
			RecordClerk clerk = RecordClerkFactory.CreateClerk(m_mediator, m_propertyTable, clerkNode, false);
			clerk.SortName = "Headword";
			return clerk;
		}

		[OneTimeTearDown]
		public override void FixtureTeardown()
		{
			base.FixtureTeardown();
			FwRegistrySettings.Release();
			Dispose();
		}

		#region disposal
		protected virtual void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (disposing)
			{
				m_Clerk?.Dispose();
				m_application?.Dispose();
				m_window?.Dispose();
				m_mediator?.Dispose();
				m_propertyTable?.Dispose();
			}
		}

		~ConfiguredXHTMLGeneratorTests()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
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
			// Specific tests override this, reset to SIL.LCModel.dll needed by most tests in the file
			ConfiguredLcmGenerator.Init();
		}

		const string xpathThruSense = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense']";
		private const string TestVariantName = "Crazy Variant";

		[Test]
		public void GenerateContentForEntry_HeadwordConfigurationGeneratesCorrectResult()
		{
			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLHeadWord",
				CSSClassNameOverride = "headword",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { headwordNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var entry = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(entry, "HeadWordTest", m_wsFr);
			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, new ReadOnlyPropertyTable(m_propertyTable), false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entry, mainEntryNode, null, settings).ToString();
			const string frenchHeadwordOfHeadwordTest = "/div[@class='lexentry']/span[@class='headword']/span[@lang='fr']/a[text()='HeadWordTest']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(frenchHeadwordOfHeadwordTest, 1);
		}

		[Test]
		public void GenerateContentForEntry_InvalidUnicodeHeadword_GeneratesErrorResult()
		{
			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLHeadWord",
				CSSClassNameOverride = "headword",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { headwordNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var entry = CreateInterestingLexEntry(Cache, "\uD900");
			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, new ReadOnlyPropertyTable(m_propertyTable), false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entry, mainEntryNode, null, settings).ToString();
			const string invalidCharsHeadwordTest = "/div[@class='lexentry']/span[@class='headword']/span[text()='\u0fff\u0fff\u0fff']";
			// change Headword back to something legal so that we don't crash trying to save bad data into the cache.
			AddHeadwordToEntry(entry, "notbadanymore", Cache.DefaultVernWs);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(invalidCharsHeadwordTest, 1);
		}

		[Test]
		public void GenerateContentForEntry_SortByHeadwordWithSpecificWsGeneratesLetterHeadings()
		{
			var firstAEntry = CreateInterestingLexEntry(Cache, "alpha1");
			// PublicationDecorator is used to force generation of Letter Headings when there is only one entry
			var flidVirtual = Cache.ServiceLocator.GetInstance<Virtuals>().LexDbEntries;
			var pubEverything = new DictionaryPublicationDecorator(Cache, (ISilDataAccessManaged)Cache.MainCacheAccessor, flidVirtual);
			var mainHeadwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLHeadWord",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" }),
				CSSClassNameOverride = "entry"
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { mainHeadwordNode },
				FieldDescription = "LexEntry"
			};
			var model = new DictionaryConfigurationModel
			{
				Parts = new List<ConfigurableDictionaryNode> { mainEntryNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(model);
			string xhtmlPath = null;
			const string letterHeadingXPath = "//div[@class='letHead']";
			try
			{
				var clerk = m_propertyTable.GetValue<RecordClerk>("ActiveClerk", null);
				clerk.SortName = "Headword (fr)";
				xhtmlPath = LcmXhtmlGenerator.SavePreviewHtmlWithStyles(new[] { firstAEntry.Hvo }, pubEverything, model, m_propertyTable);
				AssertThatXmlIn.File(xhtmlPath).HasSpecifiedNumberOfMatchesForXpath(letterHeadingXPath, 1);
			}
			finally
			{
				DeleteTempXhtmlAndCssFiles(xhtmlPath);
			}
		}

		[Test]
		public void GenerateContentForEntry_LexemeFormConfigurationGeneratesCorrectResult()
		{
			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexemeFormOA",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "vernacular" })
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { headwordNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var wsFr = Cache.WritingSystemFactory.GetWsFromStr("fr");
			var entry = CreateInterestingLexEntry(Cache);
			//Fill in the LexemeForm
			var morph = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			entry.LexemeFormOA = morph;
			morph.Form.set_String(wsFr, TsStringUtils.MakeString("LexemeFormTest", wsFr));
			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, new ReadOnlyPropertyTable(m_propertyTable), false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entry, mainEntryNode, null, settings).ToString();
			const string frenchLexForm = "/div[@class='lexentry']/span[@class='lexemeformoa']/span[@lang='fr']/a[text()='LexemeFormTest']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(frenchLexForm, 1);
		}

		[Test]
		public void GenerateContentForEntry_PronunciationLocationGeneratesCorrectResult()
		{
			var nameNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Name",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};
			var locationNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LocationRA",
				CSSClassNameOverride = "Location",
				Children = new List<ConfigurableDictionaryNode> { nameNode }
			};
			var pronunciationsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "PronunciationsOS",
				CSSClassNameOverride = "Pronunciations",
				Children = new List<ConfigurableDictionaryNode> { locationNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { pronunciationsNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
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
			location.Name.set_String(wsFr, TsStringUtils.MakeString("Here!", wsFr));
			pronunciation.LocationRA = location;
			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entry, mainEntryNode, null, settings).ToString();
			const string hereLocation = "/div[@class='lexentry']/span[@class='pronunciations']/span[@class='pronunciation']/span[@class='location']/span[@class='name']/span[@lang='fr' and text()='Here!']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(hereLocation, 1);
		}

		[Test]
		public void GenerateContentForEntry_PronunciationVideoFileGeneratesAnchorTag()
		{
			var pronunciationsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "PronunciationsOS",
				CSSClassNameOverride = "pronunciations",
				Children = new List<ConfigurableDictionaryNode> { CreateMediaNode() }
			};
			var variantPronunciationsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwningEntry",
				SubField = "PronunciationsOS",
				CSSClassNameOverride = "variantpronunciations",
				Children = new List<ConfigurableDictionaryNode> { CreateMediaNode() }
			};
			var variantFormTypeNameNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Name",
				IsEnabled = true,
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "analysis" })
			};
			var variantFormTypeNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VariantEntryTypesRS",
				CSSClassNameOverride = "variantentrytypes",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { variantFormTypeNameNode },
			};
			var variantFormsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VariantFormEntryBackRefs",
				DictionaryNodeOptions = GetFullyEnabledListOptions(DictionaryNodeListOptions.ListIds.Variant, Cache),
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { variantFormTypeNode, variantPronunciationsNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { pronunciationsNode, variantFormsNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var entry = CreateInterestingLexEntry(Cache);
			var variant = CreateInterestingLexEntry(Cache);
			// we need a real Variant Type to pass the list options test
			CreateVariantForm(Cache, entry, variant, "Spelling Variant");
			// Create a folder in the project to hold the media files
			var folder = Cache.ServiceLocator.GetInstance<ICmFolderFactory>().Create();
			Cache.LangProject.MediaOC.Add(folder);
			// Create and fill in the media files
			const string expectedMediaFolder = @"Src/xWorks/xWorksTests/TestData/LinkedFiles/AudioVisual/";
			var pron1 = Cache.ServiceLocator.GetInstance<ILexPronunciationFactory>().Create();
			entry.PronunciationsOS.Add(pron1);
			var fileName1 = "test1.mp4";
			CreateTestMediaFile(Cache, fileName1, folder, pron1);
			var videoFileUrl1 = expectedMediaFolder + fileName1;
			var pron2 = Cache.ServiceLocator.GetInstance<ILexPronunciationFactory>().Create();
			variant.PronunciationsOS.Add(pron2);
			var fileName2 = "test2.mp4";
			CreateTestMediaFile(Cache, fileName2, folder, pron2);
			var videoFileUrl2 = expectedMediaFolder + fileName2;
			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);

			const string movieCameraChar = "\U0001f3a5";
			const string movieCamSearch = "/a/text()['" + movieCameraChar + "']";
			const string entryPart = "/div[@class='lexentry']";
			const string pronunciationsPart = "/span[@class='pronunciations']/span[@class='pronunciation']";
			const string mediaFilePart = "/span[@class='mediafiles']/span[@class='mediafile']";
			const string mediaFileAnchor1 = entryPart + pronunciationsPart + mediaFilePart + movieCamSearch;
			const string variantsPart = "/span[@class='variantformentrybackrefs']/span[@class='variantformentrybackref']";
			const string varPronPart = "/span[@class='variantpronunciations']/span[@class='variantpronunciation']";
			const string mediaFileAnchor2 = entryPart + variantsPart + varPronPart + mediaFilePart + movieCamSearch;

			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entry, mainEntryNode, null, settings).ToString();
			Assert.That(result, Contains.Substring(videoFileUrl1));
			Assert.That(result, Contains.Substring(videoFileUrl2));
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(mediaFileAnchor1, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(mediaFileAnchor2, 1);
		}

		private static void CreateTestMediaFile(LcmCache cache, string name, ICmFolder localMediaFolder, ILexPronunciation pronunciation)
		{
			var mainMediaFile = cache.ServiceLocator.GetInstance<ICmMediaFactory>().Create();
			pronunciation.MediaFilesOS.Add(mainMediaFile);
			var mainFile = cache.ServiceLocator.GetInstance<ICmFileFactory>().Create();
			localMediaFolder.FilesOC.Add(mainFile);
			// InternalPath is null by default, but trying to set it to null throws an exception
			if (name != null)
				mainFile.InternalPath = name;
			mainMediaFile.MediaFileRA = mainFile;
		}

		private static ConfigurableDictionaryNode CreateMediaNode()
		{
			var mediaFileNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MediaFileRA",
				IsEnabled = true
			};
			var mediaNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MediaFilesOS",
				CSSClassNameOverride = "mediafiles",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { mediaFileNode }
			};
			return mediaNode;
		}

		[Test]
		public void GenerateContentForEntry_NoEnabledConfigurationsWritesNothing()
		{
			var homographNum = new ConfigurableDictionaryNode
			{
				FieldDescription = "HomographNumber",
				IsEnabled = true
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { homographNum },
				FieldDescription = "LexEntry",
				IsEnabled = false
			};
			var entryOne = CreateInterestingLexEntry(Cache);

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entryOne, mainEntryNode, null, settings).ToString();
			Assert.IsEmpty(result, "Should not have generated anything for a disabled node");
		}

		[Test]
		public void GenerateContentForEntry_HomographNumbersGeneratesCorrectResult()
		{
			var homographNum = new ConfigurableDictionaryNode { FieldDescription = "HomographNumber" };
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { homographNum },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var entryOne = CreateInterestingLexEntry(Cache);
			var entryTwo = CreateInterestingLexEntry(Cache);

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//keep the xml valid (single root element)
			XHTMLStringBuilder.AppendLine("<TESTWRAPPER>");
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entryOne, mainEntryNode, null, settings).ToString();
			XHTMLStringBuilder.Append(result);
			result = ConfiguredLcmGenerator.GenerateContentForEntry(entryTwo, mainEntryNode, null, settings).ToString();
			XHTMLStringBuilder.Append(result);
			XHTMLStringBuilder.AppendLine("</TESTWRAPPER>");

			var entryWithHomograph = "/TESTWRAPPER/div[@class='lexentry']/span[@class='homographnumber' and text()='1']";
			AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(entryWithHomograph, 1);
			entryWithHomograph = entryWithHomograph.Replace('1', '2');
			AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(entryWithHomograph, 1);
		}

		[Test]
		public void GenerateContentForEntry_HeadwordRefConfigurationGeneratesWithTwoWS()
		{
			var mainEntry = CreateInterestingLexEntry(Cache, "MainEntry");
			var compareReferencedEntry = CreateInterestingLexEntry(Cache, "bFR", "b comparable");
			AddHeadwordToEntry(compareReferencedEntry, "bEN", m_wsEn);

			const string comRefTypeName = "Compare";
			var comRefType = CreateLexRefType(Cache, LexRefTypeTags.MappingTypes.kmtEntryCollection, comRefTypeName, "cf", string.Empty, string.Empty);

			CreateLexReference(comRefType, new List<ICmObject> { mainEntry, compareReferencedEntry });

			var mainEntryNode = ModelForCrossReferences(new[] { comRefType.Guid.ToString() });
			// SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(mainEntry, mainEntryNode, null, DefaultSettings).ToString();
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(crossRefOwnerTypeXpath, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(CrossRefOwnerTypeXpath(comRefTypeName), 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(HeadwordWsInCrossRefsXpath("en", "bEN"), 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(HeadwordWsInCrossRefsXpath("fr", "bFR"), 1);
		}

		[Test]
		public void GenerateContentForEntry_OneSenseWithGlossGeneratesCorrectResult()
		{
			var wsOpts = GetWsOptionsForLanguages(new[] { "en" });
			var glossNode = new ConfigurableDictionaryNode { FieldDescription = "Gloss", DictionaryNodeOptions = wsOpts };
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Senses",
				DictionaryNodeOptions = GetSenseNodeOptions(),
				Children = new List<ConfigurableDictionaryNode> { glossNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { sensesNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var testEntry = CreateInterestingLexEntry(Cache);

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			// SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(testEntry, mainEntryNode, null, settings).ToString();
			const string oneSenseWithGlossOfGloss = xpathThruSense + "//span[@lang='en' and text()='gloss']";
			// This assert is dependent on the specific entry data created in CreateInterestingLexEntry
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(oneSenseWithGlossOfGloss, 1);
		}


		[Test]
		public void GenerateContentForEntry_OneEntryWithSenseAndOneWithoutWorks()
		{
			var glossNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Gloss",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" }),
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode>()
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Senses",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { glossNode }
			};
			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLHeadWord",
				CSSClassNameOverride = "headword",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" }),
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode>()
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { headwordNode, sensesNode },
				FieldDescription = "LexEntry",
				IsEnabled = true
			};
			DictionaryConfigurationModel.SpecifyParentsAndReferences(new List<ConfigurableDictionaryNode> { mainEntryNode });
			var entryOne = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(entryOne, "FirstHeadword", m_wsFr);
			var entryTwo = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(entryTwo, "SecondHeadword", m_wsFr);
			entryTwo.SensesOS.Clear();
			var entryOneId = entryOne.Guid;
			var entryTwoId = entryTwo.Guid;

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//keep the xml valid (single root element)
			XHTMLStringBuilder.AppendLine("<TESTWRAPPER>");
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entryOne, mainEntryNode, null, settings).ToString();
			XHTMLStringBuilder.Append(result);
			result = ConfiguredLcmGenerator.GenerateContentForEntry(entryTwo, mainEntryNode, null, settings).ToString();
			XHTMLStringBuilder.Append(result);
			XHTMLStringBuilder.AppendLine("</TESTWRAPPER>");
			result = XHTMLStringBuilder.ToString();
			var entryOneHasSensesSpan = "/TESTWRAPPER/div[@class='lexentry' and @id='g" + entryOneId + "']/span[@class='senses']";
			var entryTwoExists = "/TESTWRAPPER/div[@class='lexentry' and @id='g" + entryTwoId + "']";
			var entryTwoHasNoSensesSpan = "/TESTWRAPPER/div[@class='lexentry' and @id='g" + entryTwoId + "']/span[@class='senses']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(entryOneHasSensesSpan, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(entryTwoExists, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(entryTwoHasNoSensesSpan, 0);
		}

		[Test]
		public void GenerateContentForEntry_DefaultRootGeneratesResult()
		{
			var defaultRoot = string.Concat(
				Path.Combine(FwDirectoryFinder.DefaultConfigurations, "Dictionary", "Root"), DictionaryConfigurationModel.FileExtension);
			var entry = CreateInterestingLexEntry(Cache);
			var dictionaryModel = new DictionaryConfigurationModel(defaultRoot, Cache);
			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entry, dictionaryModel.Parts[0], DefaultDecorator, settings).ToString();
			var entryExists = "/div[@class='entry' and @id='g" + entry.Guid + "']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(entryExists, 1);
		}

		[Test]
		public void GenerateContentForEntry_DoesNotDescendThroughDisabledNode()
		{
			var wsOpts = GetWsOptionsForLanguages(new[] { "en" });
			var senses = new ConfigurableDictionaryNode
			{
				FieldDescription = "Senses",
				IsEnabled = false,
				Children = new List<ConfigurableDictionaryNode>
					{
						new ConfigurableDictionaryNode { FieldDescription = "Gloss", DictionaryNodeOptions = wsOpts, IsEnabled = true }
					}
			};
			var headword = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLHeadWord",
				CSSClassNameOverride = "headword",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" }),
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode>()
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { headword, senses },
				FieldDescription = "LexEntry",
				IsEnabled = true
			};
			DictionaryConfigurationModel.SpecifyParentsAndReferences(new List<ConfigurableDictionaryNode> { mainEntryNode });
			var entryOne = CreateInterestingLexEntry(Cache);

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entryOne, mainEntryNode, null, settings).ToString();
			const string sensesThatShouldNotBe = "/div[@class='entry']/span[@class='senses']";
			const string headwordThatShouldNotBe = "//span[@class='gloss']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(sensesThatShouldNotBe, 0);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(headwordThatShouldNotBe, 0);
		}

		[Test]
		public void GenerateContentForEntry_ProduceNothingWithOnlyDisabledNode()
		{
			var wsOpts = GetWsOptionsForLanguages(new[] { "en" });
			var senses = new ConfigurableDictionaryNode
			{
				FieldDescription = "Senses",
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
			DictionaryConfigurationModel.SpecifyParentsAndReferences(new List<ConfigurableDictionaryNode> { mainEntryNode });
			var entryOne = CreateInterestingLexEntry(Cache);

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entryOne, mainEntryNode, null, settings).ToString();
			Assert.IsEmpty(result, "With only one subnode that is disabled, there should be nothing generated!");
		}

		[Test]
		public void GenerateContentForEntry_TwoSensesWithSameInfoShowGramInfoFirst()
		{
			var DictionaryNodeSenseOptions = new DictionaryNodeSenseOptions
			{
				ShowSharedGrammarInfoFirst = true
			};
			var categorynfo = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLPartOfSpeech",
				Children = new List<ConfigurableDictionaryNode>(),
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" })
			};
			var gramInfoNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MorphoSyntaxAnalysisRA",
				CSSClassNameOverride = "msas",
				Children = new List<ConfigurableDictionaryNode> { categorynfo }
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "Senses",
				DictionaryNodeOptions = DictionaryNodeSenseOptions,
				Children = new List<ConfigurableDictionaryNode> { gramInfoNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { sensesNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
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

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			// SUT
			var xhtmlString = ConfiguredLcmGenerator.GenerateContentForEntry(entry, mainEntryNode, null, settings).ToString();
			const string sharedGramInfoPath = "//div[@class='lexentry']/span[@class='senses']/span[@class='sharedgrammaticalinfo']";
			const string gramInfoPath = "//div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense']/span[@class='msas']/span[@class='mlpartofspeech']";
			AssertThatXmlIn.String(xhtmlString).HasNoMatchForXpath(gramInfoPath);
			AssertThatXmlIn.String(xhtmlString).HasSpecifiedNumberOfMatchesForXpath(sharedGramInfoPath, 1);
		}

		[Test]
		public void GenerateContentForEntry_TwoSensesWithSameInfo_ThirdSenseNotPublished_ShowGramInfoFirst()
		{
			var DictionaryNodeSenseOptions = new DictionaryNodeSenseOptions
			{
				ShowSharedGrammarInfoFirst = true
			};
			var categoryInfo = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLPartOfSpeech",
				Children = new List<ConfigurableDictionaryNode>(),
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" })
			};
			var gramInfoNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MorphoSyntaxAnalysisRA",
				CSSClassNameOverride = "msa",
				Children = new List<ConfigurableDictionaryNode> { categoryInfo }
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "Senses",
				DictionaryNodeOptions = DictionaryNodeSenseOptions,
				Children = new List<ConfigurableDictionaryNode> { gramInfoNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { sensesNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var entry = CreateInterestingLexEntry(Cache);

			var posSeq = Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS;
			var pos1 = Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>().Create();
			var pos2 = Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>().Create();
			posSeq.Add(pos1);
			posSeq.Add(pos2);
			var sense = entry.SensesOS.First();

			var msa = Cache.ServiceLocator.GetInstance<IMoInflAffMsaFactory>().Create();
			entry.MorphoSyntaxAnalysesOC.Add(msa);
			sense.MorphoSyntaxAnalysisRA = msa;

			msa.PartOfSpeechRA = pos1;
			msa.PartOfSpeechRA.Abbreviation.set_String(m_wsEn, "Noun");

			// Add second sense; same msa
			AddSenseToEntry(entry, "second sense", m_wsEn, Cache);
			var secondMsa = Cache.ServiceLocator.GetInstance<IMoStemMsaFactory>().Create();
			var secondSense = entry.SensesOS[1];
			entry.MorphoSyntaxAnalysesOC.Add(secondMsa);
			secondSense.MorphoSyntaxAnalysisRA = secondMsa;
			secondMsa.PartOfSpeechRA = pos1;

			// Add third sense; different msa
			AddSenseToEntry(entry, "third sense", m_wsEn, Cache);
			var thirdMsa = Cache.ServiceLocator.GetInstance<IMoStemMsaFactory>().Create();
			var thirdSense = entry.SensesOS[2];
			entry.MorphoSyntaxAnalysesOC.Add(thirdMsa);
			thirdSense.MorphoSyntaxAnalysisRA = thirdMsa;
			thirdMsa.PartOfSpeechRA = pos2;
			thirdMsa.PartOfSpeechRA.Abbreviation.set_String(m_wsEn, "Verb");

			// Setup publication
			// If the 3rd sense with the different msa is NOT published in the Main Dictionary
			// then when we generate XHTML for Main Dictionary, the shared grammatical info
			// (shared between the other two senses) should cause the gramm. info to be
			// put out front.
			var mainDict = Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS[0];
			thirdSense.DoNotPublishInRC.Add(mainDict);

			// create decorator
			var mainDictionaryDecorator = new DictionaryPublicationDecorator(Cache, (ISilDataAccessManaged)Cache.MainCacheAccessor,
				Cache.ServiceLocator.GetInstance<Virtuals>().LexDbEntries, mainDict);
			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			// SUT
			var xhtmlString = ConfiguredLcmGenerator.GenerateContentForEntry(entry, mainEntryNode, mainDictionaryDecorator, settings).ToString();
			const string sharedGramInfoPath = "//div[@class='lexentry']/span[@class='senses']/span[@class='sharedgrammaticalinfo']";
			const string gramInfoPath = "//div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense']/span[@class='msa']/span[@class='mlpartofspeech']";
			AssertThatXmlIn.String(xhtmlString).HasNoMatchForXpath(gramInfoPath);
			AssertThatXmlIn.String(xhtmlString).HasSpecifiedNumberOfMatchesForXpath(sharedGramInfoPath, 1);
		}

		[Test]
		public void GenerateContentForEntry_TwoSensesWithDifferentGramInfoShowInfoInSenses()
		{
			var DictionaryNodeSenseOptions = new DictionaryNodeSenseOptions
			{
				ShowSharedGrammarInfoFirst = true
			};
			var categorynfo = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLPartOfSpeech",
				Children = new List<ConfigurableDictionaryNode>(),
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" })
			};
			var gramInfoNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MorphoSyntaxAnalysisRA",
				CSSClassNameOverride = "msas",
				Children = new List<ConfigurableDictionaryNode> { categorynfo }
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "Senses",
				DictionaryNodeOptions = DictionaryNodeSenseOptions,
				Children = new List<ConfigurableDictionaryNode> { gramInfoNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { sensesNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
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

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			// SUT
			var xhtmlString = ConfiguredLcmGenerator.GenerateContentForEntry(entry, mainEntryNode, null, settings).ToString();
			const string sharedGramInfoPath = "//div[@class='lexentry']/span[@class='sensesos']/span[@class='sharedgrammaticalinfo']";
			const string gramInfoPath = "//div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense']/span[@class='msas']/span[@class='mlpartofspeech']";
			AssertThatXmlIn.String(xhtmlString).HasSpecifiedNumberOfMatchesForXpath(gramInfoPath, 2);
			AssertThatXmlIn.String(xhtmlString).HasNoMatchForXpath(sharedGramInfoPath);
		}

		[Test]
		public void GenerateContentForEntry_TwoSensesWithNoGramInfoDisplaysNothingForSharedGramInfo()
		{
			var DictionaryNodeSenseOptions = new DictionaryNodeSenseOptions
			{
				ShowSharedGrammarInfoFirst = true
			};
			var categorynfo = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLPartOfSpeech",
				Children = new List<ConfigurableDictionaryNode>()
			};
			var gramInfoNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MorphoSyntaxAnalysisRA",
				CSSClassNameOverride = "morphosyntaxanalysis",
				Children = new List<ConfigurableDictionaryNode> { categorynfo }
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				DictionaryNodeOptions = DictionaryNodeSenseOptions,
				Children = new List<ConfigurableDictionaryNode> { gramInfoNode }
			};
			var headword = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLHeadWord",
				CSSClassNameOverride = "headword",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" }),
				Children = new List<ConfigurableDictionaryNode>()
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { headword, sensesNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var entry = CreateInterestingLexEntry(Cache);
			AddSenseToEntry(entry, "sense 2", m_wsEn, Cache);

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			// SUT
			var xhtmlString = ConfiguredLcmGenerator.GenerateContentForEntry(entry, mainEntryNode, null, settings).ToString();
			const string sharedGramInfoPath = "//div[@class='lexentry']/span[@class='sensesos']/span[@class='sharedgrammaticalinfo']";
			const string gramInfoPath = "//div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense']/span[@class='msas']/span[@class='mlpartofspeech']";
			AssertThatXmlIn.String(xhtmlString).HasNoMatchForXpath(gramInfoPath);
			AssertThatXmlIn.String(xhtmlString).HasNoMatchForXpath(sharedGramInfoPath);
		}

		[Test]
		public void GenerateContentForEntry_MorphemeType()
		{
			var morphemeTypeAbbrev = new ConfigurableDictionaryNode()
			{
				FieldDescription = "Abbreviation",
				IsEnabled = true,
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" })
			};
			var morphemeType = new ConfigurableDictionaryNode()
			{
				FieldDescription = "MorphTypes",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { morphemeTypeAbbrev }
			};
			var gramInfoNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MorphoSyntaxAnalysisRA",
				CSSClassNameOverride = "MorphoSyntaxAnalysis",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { morphemeType }
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Senses",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { gramInfoNode }
			};
			var headword = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLHeadWord",
				CSSClassNameOverride = "headword",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" }),
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode>()
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { headword, sensesNode },
				FieldDescription = "LexEntry",
				IsEnabled = true
			};
			DictionaryConfigurationModel.SpecifyParentsAndReferences(new List<ConfigurableDictionaryNode> { mainEntryNode });
			var entry = CreateInterestingSuffix(Cache, " ba");

			var sense = entry.SensesOS.First();

			var msa = Cache.ServiceLocator.GetInstance<IMoInflAffMsaFactory>().Create();
			entry.MorphoSyntaxAnalysesOC.Add(msa);
			sense.MorphoSyntaxAnalysisRA = msa;

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			// SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entry, mainEntryNode, null, settings).ToString();
			const string morphTypePath = "//span[@class='morphosyntaxanalysis']/span[@class='morphtypes']/span[@class='morphtype']/span[@class='abbreviation']/span[@lang='en' and text()='sfx']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(morphTypePath, 1);
		}

		[Test]
		public void GenerateContentForEntry_MakesSpanForRA()
		{
			var gramInfoAbbrev = new ConfigurableDictionaryNode()
			{
				FieldDescription = "InterlinearAbbrTSS"
			};
			var gramInfoNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MorphoSyntaxAnalysisRA",
				CSSClassNameOverride = "MorphoSyntaxAnalysis",
				Children = new List<ConfigurableDictionaryNode> { gramInfoAbbrev }
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Senses",
				DictionaryNodeOptions = GetSenseNodeOptions(),
				Children = new List<ConfigurableDictionaryNode> { gramInfoNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { sensesNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var entry = CreateInterestingLexEntry(Cache);

			var sense = entry.SensesOS.First();

			var msa = Cache.ServiceLocator.GetInstance<IMoInflAffMsaFactory>().Create();
			entry.MorphoSyntaxAnalysesOC.Add(msa);
			sense.MorphoSyntaxAnalysisRA = msa;

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			// SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entry, mainEntryNode, null, settings).ToString();
			const string gramInfoPath = xpathThruSense + "/span[@class='morphosyntaxanalysis']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(gramInfoPath, 1);
		}

		[Test]
		public void GenerateContentForEntry_CmObjectWithNoEnabledChildrenSkipsSpan()
		{
			var gramInfoAbbrev = new ConfigurableDictionaryNode()
			{
				FieldDescription = "InterlinearAbbrTSS",
				IsEnabled = false
			};
			var gramInfoNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MorphoSyntaxAnalysisRA",
				CSSClassNameOverride = "MorphoSyntaxAnalysis",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { gramInfoAbbrev } // There are no enabled children, so this span should be skipped
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Senses",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { gramInfoNode }
			};
			var headword = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLHeadWord",
				CSSClassNameOverride = "headword",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" }),
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode>()
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { headword, sensesNode },
				FieldDescription = "LexEntry",
				IsEnabled = true
			};
			DictionaryConfigurationModel.SpecifyParentsAndReferences(new List<ConfigurableDictionaryNode> { mainEntryNode });
			var entry = CreateInterestingLexEntry(Cache);

			var sense = entry.SensesOS.First();

			var msa = Cache.ServiceLocator.GetInstance<IMoInflAffMsaFactory>().Create();
			entry.MorphoSyntaxAnalysesOC.Add(msa);
			sense.MorphoSyntaxAnalysisRA = msa;

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			// SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entry, mainEntryNode, null, settings).ToString();
			const string gramInfoPath = xpathThruSense + "/span[@class='morphosyntaxanalysis']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(gramInfoPath, 0);
		}

		/// <summary>
		/// If the dictionary configuration specifies to export grammatical info, but there is no such grammatical info object to export, don't write a span.
		/// </summary>
		[Test]
		public void GenerateContentForEntry_DoesNotMakeSpanForRAIfNoData()
		{
			var gramInfoNode = new ConfigurableDictionaryNode { FieldDescription = "MorphoSyntaxAnalysisRA" };
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Senses",
				Children = new List<ConfigurableDictionaryNode> { gramInfoNode }
			};
			var headword = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLHeadWord",
				CSSClassNameOverride = "headword",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" }),
				Children = new List<ConfigurableDictionaryNode>()
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { headword, sensesNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var entry = CreateInterestingLexEntry(Cache);

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			// SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entry, mainEntryNode, null, settings).ToString();
			const string gramInfoPath = xpathThruSense + "/span[@class='morphosyntaxanalysis']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(gramInfoPath, 0);
		}

		/// <summary>
		/// If the dictionary configuration specifies to export scientific category, but there is no data in the field to export, don't write a span.
		/// </summary>
		[Test]
		public void GenerateContentForEntry_DoesNotMakeSpanForTSStringIfNoData()
		{
			var scientificName = new ConfigurableDictionaryNode { FieldDescription = "ScientificName" };
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Senses",
				Children = new List<ConfigurableDictionaryNode> { scientificName }
			};
			var headword = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLHeadWord",
				CSSClassNameOverride = "headword",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" }),
				Children = new List<ConfigurableDictionaryNode>()
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { headword, sensesNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var entry = CreateInterestingLexEntry(Cache);

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			// SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entry, mainEntryNode, null, settings).ToString();
			const string scientificCatPath = xpathThruSense + "/span[@class='scientificname']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(scientificCatPath, 0);
		}

		[Test]
		public void GenerateContentForEntry_SupportsGramAbbrChildOfMSARA()
		{
			var gramAbbrNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "InterlinearAbbrTSS",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};
			var gramNameNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "InterlinearNameTSS",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};
			var gramInfoNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MorphoSyntaxAnalysisRA",
				CSSClassNameOverride = "MorphoSyntaxAnalysis",
				Children = new List<ConfigurableDictionaryNode> { gramAbbrNode, gramNameNode }
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Senses",
				DictionaryNodeOptions = GetSenseNodeOptions(),
				Children = new List<ConfigurableDictionaryNode> { gramInfoNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { sensesNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var wsFr = Cache.WritingSystemFactory.GetWsFromStr("fr");
			var entry = CreateInterestingLexEntry(Cache);

			ILangProject lp = Cache.LangProject;

			ILcmOwningSequence<ICmPossibility> posSeq = lp.PartsOfSpeechOA.PossibilitiesOS;
			IPartOfSpeech pos = Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>().Create();
			posSeq.Add(pos);

			var sense = entry.SensesOS.First();

			var msa = Cache.ServiceLocator.GetInstance<IMoInflAffMsaFactory>().Create();
			entry.MorphoSyntaxAnalysesOC.Add(msa);
			sense.MorphoSyntaxAnalysisRA = msa;

			msa.PartOfSpeechRA = pos;
			msa.PartOfSpeechRA.Abbreviation.set_String(wsFr, "Blah");

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			// SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entry, mainEntryNode, null, settings).ToString();

			const string gramAbbr1 = xpathThruSense + "/span[@class='morphosyntaxanalysis']/span[@class='interlinearabbrtss']/span[@lang='fr']/span[@lang='fr' and text()='Blah']";
			const string gramAbbr2 = xpathThruSense + "/span[@class='morphosyntaxanalysis']/span[@class='interlinearabbrtss']/span[@lang='fr']/span[@lang='en' and text()=':Any']";
			const string gramName1 = xpathThruSense + "/span[@class='morphosyntaxanalysis']/span[@class='interlinearnametss']/span[@lang='fr']/span[@lang='fr' and text()='Blah']";
			const string gramName2 = xpathThruSense + "/span[@class='morphosyntaxanalysis']/span[@class='interlinearnametss']/span[@lang='fr']/span[@lang='en' and text()=':Any']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(gramAbbr1, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(gramAbbr2, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(gramName1, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(gramName2, 1);
		}

		[Test]
		public void GenerateContentForEntry_DontDisplayNotSure()
		{
			var gramAbbrNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "InterlinearAbbrTSS",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};
			var gramNameNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "InterlinearNameTSS",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};
			var gramInfoNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MorphoSyntaxAnalysisRA",
				CSSClassNameOverride = "MorphoSyntaxAnalysis",
				Children = new List<ConfigurableDictionaryNode> { gramAbbrNode, gramNameNode }
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Senses",
				DictionaryNodeOptions = GetSenseNodeOptions(),
				Children = new List<ConfigurableDictionaryNode> { gramInfoNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { sensesNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var wsFr = Cache.WritingSystemFactory.GetWsFromStr("fr");
			var entry = CreateInterestingLexEntry(Cache);

			ILangProject lp = Cache.LangProject;

			ILcmOwningSequence<ICmPossibility> posSeq = lp.PartsOfSpeechOA.PossibilitiesOS;
			IPartOfSpeech pos = Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>().Create();
			posSeq.Add(pos);

			var sense = entry.SensesOS.First();

			var msa = Cache.ServiceLocator.GetInstance<IMoInflAffMsaFactory>().Create();
			entry.MorphoSyntaxAnalysesOC.Add(msa);
			sense.MorphoSyntaxAnalysisRA = msa;

			msa.PartOfSpeechRA = pos;
			msa.PartOfSpeechRA.Abbreviation.set_String(wsFr, "<Not Sure>");

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			// SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entry, mainEntryNode, null, settings).ToString();

			const string gramAbbr1 = xpathThruSense + "/span[@class='morphosyntaxanalysis']/span[@class='interlinearabbrtss']/span[@lang='fr']/span[@lang='fr' and text()='<Not Sure>']";
			const string gramAbbr2 = xpathThruSense + "/span[@class='morphosyntaxanalysis']/span[@class='interlinearabbrtss']/span[@lang='fr']/span[@lang='en' and text()=':Any']";
			const string gramName1 = xpathThruSense + "/span[@class='morphosyntaxanalysis']/span[@class='interlinearnametss']/span[@lang='fr']/span[@lang='fr' and text()='<Not Sure>']";
			const string gramName2 = xpathThruSense + "/span[@class='morphosyntaxanalysis']/span[@class='interlinearnametss']/span[@lang='fr']/span[@lang='en' and text()=':Any']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(gramAbbr1, 0);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(gramAbbr2, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(gramName1, 0);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(gramName2, 1);
		}

		[Test]
		public void GenerateContentForEntry_CaptionOrHeadwordGetsCaption()
		{
			var wsOpts = GetWsOptionsForLanguages(new[] { "en" });
			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLHeadWord",
				CSSClassNameOverride = "headword",
				DictionaryNodeOptions = wsOpts
			};

			var captionOrHeadwordNode = new ConfigurableDictionaryNode { FieldDescription = "CaptionOrHeadword", DictionaryNodeOptions = wsOpts };
			var pictureNode = new ConfigurableDictionaryNode
			{
				DictionaryNodeOptions = new DictionaryNodePictureOptions(),
				FieldDescription = "PicturesOfSenses",
				CSSClassNameOverride = "Pictures",
				Children = new List<ConfigurableDictionaryNode> { captionOrHeadwordNode }
			};

			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Senses",
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { sensesNode, pictureNode, headwordNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var entryOne = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(entryOne, "HeadwordEn", m_wsEn);
			var sense = entryOne.SensesOS[0];
			sense.PicturesOS.Add(CreatePicture(Cache, true, "captionEn", "en"));

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entryOne, mainEntryNode, null, settings).ToString();
			const string captionOrHeadwordContainsCaption = "/div[@class='lexentry']/span[@class='pictures']/div[@class='picture']/div[@class='captionContent']/span[@class='captionorheadword']//span[text()='captionEn']";
			//This assert is dependent on the specific entry data created in CreateInterestingLexEntry
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(captionOrHeadwordContainsCaption, 1);
		}

		[Test]
		public void GenerateContentForEntry_CaptionOrHeadwordGetsHeadword()
		{
			var wsOpts = GetWsOptionsForLanguages(new[] { "en" });
			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLHeadWord",
				CSSClassNameOverride = "headword",
				DictionaryNodeOptions = wsOpts
			};

			var captionOrHeadwordNode = new ConfigurableDictionaryNode { FieldDescription = "CaptionOrHeadword", DictionaryNodeOptions = wsOpts };
			var pictureNode = new ConfigurableDictionaryNode
			{
				DictionaryNodeOptions = new DictionaryNodePictureOptions(),
				FieldDescription = "PicturesOfSenses",
				CSSClassNameOverride = "Pictures",
				Children = new List<ConfigurableDictionaryNode> { captionOrHeadwordNode }
			};

			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Senses",
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { sensesNode, pictureNode, headwordNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var entryOne = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(entryOne, "HeadwordEn", m_wsEn);
			var sense = entryOne.SensesOS[0];
			sense.PicturesOS.Add(CreatePicture(Cache, true, null, "en"));    // Create the picture with a null caption.

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entryOne, mainEntryNode, null, settings).ToString();
			const string captionOrHeadwordContainsHeadword = "/div[@class='lexentry']/span[@class='pictures']/div[@class='picture']/div[@class='captionContent']/span[@class='captionorheadword']//span[text()='HeadwordEn']";
			//This assert is dependent on the specific entry data created in CreateInterestingLexEntry
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(captionOrHeadwordContainsHeadword, 1);
		}

		[Test]
		public void GenerateContentForEntry_CaptionOrHeadword_HandlePerWs()
		{
			var wsOpts = GetWsOptionsForLanguages(new[] { "en", "fr" });
			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLHeadWord",
				CSSClassNameOverride = "headword",
				DictionaryNodeOptions = wsOpts
			};

			var captionOrHeadwordNode = new ConfigurableDictionaryNode { FieldDescription = "CaptionOrHeadword", DictionaryNodeOptions = wsOpts };
			var pictureNode = new ConfigurableDictionaryNode
			{
				DictionaryNodeOptions = new DictionaryNodePictureOptions(),
				FieldDescription = "PicturesOfSenses",
				CSSClassNameOverride = "Pictures",
				Children = new List<ConfigurableDictionaryNode> { captionOrHeadwordNode }
			};

			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Senses",
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { sensesNode, pictureNode, headwordNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var entryOne = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(entryOne, "HeadwordEn", m_wsEn);
			AddHeadwordToEntry(entryOne, "HeadwordFr", m_wsFr);
			var sense = entryOne.SensesOS[0];
			sense.PicturesOS.Add(CreatePicture(Cache, true, "captionEn", "en"));    // Create the picture with a en caption, but no fr caption.

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entryOne, mainEntryNode, null, settings).ToString();
			const string captionOrHeadwordContainsCaptionEn = "/div[@class='lexentry']/span[@class='pictures']/div[@class='picture']/div[@class='captionContent']/span[@class='captionorheadword']//span[@lang='en' and text()='captionEn']";
			const string captionOrHeadwordContainsHeadwordFr = "/div[@class='lexentry']/span[@class='pictures']/div[@class='picture']/div[@class='captionContent']/span[@class='captionorheadword']//span[@lang='fr' and text()='HeadwordFr']";
			//This assert is dependent on the specific entry data created in CreateInterestingLexEntry
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(captionOrHeadwordContainsCaptionEn, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(captionOrHeadwordContainsHeadwordFr, 1);
		}


		[Test]
		public void GenerateContentForEntry_DefinitionOrGlossWorks()
		{
			var wsOpts = GetWsOptionsForLanguages(new[] { "en" });
			var senses = new ConfigurableDictionaryNode
			{
				FieldDescription = "Senses",
				Children = new List<ConfigurableDictionaryNode>
					{
						new ConfigurableDictionaryNode { FieldDescription = "DefinitionOrGloss", DictionaryNodeOptions = wsOpts }
					}
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { senses },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var entryOne = CreateInterestingLexEntry(Cache);

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entryOne, mainEntryNode, null, settings).ToString();
			const string senseWithdefinitionOrGloss = "//span[@class='sense']/span[@class='definitionorgloss']/span[text()='gloss']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(senseWithdefinitionOrGloss, 1);
		}

		[Test]
		public void GenerateContentForEntry_DefinitionOrGlossWorks_WithAbbrev()
		{
			var senses = new ConfigurableDictionaryNode
			{
				FieldDescription = "Senses",
				Children = new List<ConfigurableDictionaryNode>
					{
						new ConfigurableDictionaryNode
						{
							FieldDescription = "DefinitionOrGloss",
							DictionaryNodeOptions = GetWsOptionsForLanguageswithDisplayWsAbbrev(new[] { "en" })
						}
					}
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { senses },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var entryOne = CreateInterestingLexEntry(Cache);

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entryOne, mainEntryNode, null, settings).ToString();
			const string senseWithdefinitionOrGloss =
				"//span[@class='sense']/span[@class='definitionorgloss']/span[@class='writingsystemprefix' and normalize-space(text())='Eng']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(senseWithdefinitionOrGloss, 1);
		}

		[Test]
		public void GenerateContentForEntry_DefinitionOrGloss_HandlePerWS()
		{
			var wsOpts = GetWsOptionsForLanguages(new[] { "en", "es" });
			var senses = new ConfigurableDictionaryNode
			{
				FieldDescription = "Senses",
				Children = new List<ConfigurableDictionaryNode>
					{
						new ConfigurableDictionaryNode { FieldDescription = "DefinitionOrGloss", DictionaryNodeOptions = wsOpts }
					}
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { senses },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var entryOne = CreateInterestingLexEntry(Cache);
			var wsEs = EnsureWritingSystemSetup(Cache, "es", false);
			entryOne.SensesOS.First().Definition.set_String(wsEs, "definition");
			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entryOne, mainEntryNode, null, settings).ToString();
			const string senseWithdefinitionOrGlossTwoWs = "//span[@class='sense']/span[@class='definitionorgloss' and span[1]='gloss' and span[2]='definition']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(senseWithdefinitionOrGlossTwoWs, 1);
		}

		[Test]
		public void GenerateContentForEntry_ReferencedComplexFormDefinitionOrGloss_HandlePerWS()
		{
			// LT-19073: Definition and gloss display behaviour for LT-7445 should apply to "Definition (or Gloss)" field in Referenced Complex Froms.
			// Check that different combinations of present or missing definition have successful fallback to gloss, and independently of other senses.

			var typeMain = Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS[0];

			var entryEntry = CreateInterestingLexEntry(Cache, "entry");

			// Add analysis ws German
			var wsDe = EnsureWritingSystemSetup(Cache, "de", false);

			// Both senses have gloss and definition
			var firstComplexForm = CreateInterestingLexEntry(Cache, "entry1", "glossA1", "definitionA1");
			AddSenseToEntry(firstComplexForm, "glossA2", wsDe, Cache, "definitionA2");
			CreateComplexForm(Cache, entryEntry, firstComplexForm, false);

			// both senses have gloss, not definition
			var secondComplexForm = CreateInterestingLexEntry(Cache, "entry2", "glossB1");
			AddSenseToEntry(secondComplexForm, "glossB2", wsDe, Cache);
			CreateComplexForm(Cache, entryEntry, secondComplexForm, false);

			// second sense has gloss, not definition
			var thirdComplexForm = CreateInterestingLexEntry(Cache, "entry3", "glossC1", "definitionC1");
			AddSenseToEntry(thirdComplexForm, "glossC2", wsDe, Cache);
			CreateComplexForm(Cache, entryEntry, thirdComplexForm, false);

			// first sense has gloss, not definition
			var fourthComplexForm = CreateInterestingLexEntry(Cache, "entry4", "glossD1");
			AddSenseToEntry(fourthComplexForm, "glossD2", wsDe, Cache, "definitionD2");
			CreateComplexForm(Cache, entryEntry, fourthComplexForm, false);

			var flidVirtual = Cache.ServiceLocator.GetInstance<Virtuals>().LexDbEntries;
			var pubMain = new DictionaryPublicationDecorator(Cache, (ISilDataAccessManaged)Cache.MainCacheAccessor, flidVirtual, typeMain);

			var definitionOrGlossNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "DefinitionOrGloss",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en", "de" }),
			};
			var complexFormNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VisibleComplexFormBackRefs",
				Children = new List<ConfigurableDictionaryNode> { definitionOrGlossNode },
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { complexFormNode },
				FieldDescription = "LexEntry"
			};

			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			// SUT
			var output = ConfiguredLcmGenerator.GenerateContentForEntry(entryEntry, mainEntryNode, pubMain, DefaultSettings).ToString();

			// set of xpaths and required number of matches.
			var checkthis = new Dictionary<string, int>()
				{
					{ "/div/span[@class='visiblecomplexformbackrefs']/span[@class='visiblecomplexformbackref']/span[@class='definitionorgloss']/span[.='definitionA1']", 1 },
					{ "/div/span[@class='visiblecomplexformbackrefs']/span[@class='visiblecomplexformbackref']/span[@class='definitionorgloss']/span[.='definitionA2']", 1 },

					{ "/div/span[@class='visiblecomplexformbackrefs']/span[@class='visiblecomplexformbackref']/span[@class='definitionorgloss']/span[.='glossB1']", 1 },
					{ "/div/span[@class='visiblecomplexformbackrefs']/span[@class='visiblecomplexformbackref']/span[@class='definitionorgloss']/span[.='glossB2']", 1 },

					{ "/div/span[@class='visiblecomplexformbackrefs']/span[@class='visiblecomplexformbackref']/span[@class='definitionorgloss']/span[.='definitionC1']", 1 },
					{ "/div/span[@class='visiblecomplexformbackrefs']/span[@class='visiblecomplexformbackref']/span[@class='definitionorgloss']/span[.='glossC2']", 1 },

					{ "/div/span[@class='visiblecomplexformbackrefs']/span[@class='visiblecomplexformbackref']/span[@class='definitionorgloss']/span[.='glossD1']", 1 },
					{ "/div/span[@class='visiblecomplexformbackrefs']/span[@class='visiblecomplexformbackref']/span[@class='definitionorgloss']/span[.='definitionD2']", 1 },
				};
			foreach (var thing in checkthis)
			{
				AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(thing.Key, thing.Value);
			}
		}

		[Test]
		public void GenerateContentForEntry_OtherReferencedComplexForms()
		{
			var complexformoptions = new DictionaryNodeListAndParaOptions
			{
				Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>
					{
						new DictionaryNodeListOptions.DictionaryNodeOption { Id = "73266a3a-48e8-4bd7-8c84-91c730340b7d" }
					}
			};

			var revAbbrevNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ReverseAbbr",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" })
			};
			var refTypeNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ComplexEntryTypesRS",
				CSSClassNameOverride = "complexformtypes",
				Children = new List<ConfigurableDictionaryNode> { revAbbrevNode }
			};
			var orchNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwningEntry",
				SubField = "HeadWordRef",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};
			var orcfNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ComplexFormsNotSubentries",
				DictionaryNodeOptions = complexformoptions,
				Children = new List<ConfigurableDictionaryNode> { refTypeNode, orchNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { orcfNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);

			var mainEntry = CreateInterestingLexEntry(Cache);
			var otherReferencedComplexForm = CreateInterestingLexEntry(Cache);
			var complexformentryref = CreateComplexForm(Cache, mainEntry, otherReferencedComplexForm, false, new Guid(complexformoptions.Options.First().Id));

			var complexRefAbbr = complexformentryref.ComplexEntryTypesRS[0].Abbreviation.BestAnalysisAlternative.Text;
			var complexRefRevAbbr = complexformentryref.ComplexEntryTypesRS[0].ReverseAbbr.BestAnalysisAlternative.Text;

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(mainEntry, mainEntryNode, null, settings).ToString();
			var fwdNameXpath = string.Format(
				"//span[@class='complexformsnotsubentries']/span[@class='complexformtypes']/span[@class='complexformtype']/span/span[@lang='en' and text()='{0}']",
					complexRefAbbr);
			var revNameXpath = string.Format(
				"//span[@class='complexformsnotsubentries']/span[@class='complexformtypes']/span[@class='complexformtype']/span[@class='reverseabbr']/span[@lang='en' and text()='{0}']",
					complexRefRevAbbr);
			AssertThatXmlIn.String(result).HasNoMatchForXpath(fwdNameXpath);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(revNameXpath, 1);
		}

		[Test]
		public void GenerateContentForEntry_DuplicateConfigNodeWithSpaceWorks()
		{
			var defOrGloss = new ConfigurableDictionaryNode
			{
				FieldDescription = "DefinitionOrGloss",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" })

			};
			var senses = new ConfigurableDictionaryNode
			{
				FieldDescription = "Senses",
				IsDuplicate = true,
				LabelSuffix = "Test one",
				Children = new List<ConfigurableDictionaryNode> { defOrGloss }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { senses },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var entryOne = CreateInterestingLexEntry(Cache);

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entryOne, mainEntryNode, null, settings).ToString();
			const string senseWithHyphenSuffix = "//span[@class='senses_test-one']/span[@class='sense_test-one']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(senseWithHyphenSuffix, 1);
		}

		[Test]
		public void GenerateContentForEntry_DuplicateConfigNodeWithPuncWorks()
		{
			var defOrGloss = new ConfigurableDictionaryNode
			{
				FieldDescription = "DefinitionOrGloss",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" })

			};
			var senses = new ConfigurableDictionaryNode
			{
				FieldDescription = "Senses",
				IsDuplicate = true,
				LabelSuffix = "#Test",
				Children = new List<ConfigurableDictionaryNode> { defOrGloss }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { senses },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var entryOne = CreateInterestingLexEntry(Cache);

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entryOne, mainEntryNode, null, settings).ToString();
			const string senseWithHyphenSuffix = "//span[@class='senses_-test']/span[@class='sense_-test']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(senseWithHyphenSuffix, 1);
		}

		[Test]
		public void GenerateContentForEntry_DuplicateConfigNodeWithMultiPuncWorks()
		{
			var defOrGloss = new ConfigurableDictionaryNode
			{
				FieldDescription = "DefinitionOrGloss",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" })

			};
			var senses = new ConfigurableDictionaryNode
			{
				FieldDescription = "Senses",
				IsDuplicate = true,
				LabelSuffix = "#Test$",
				Children = new List<ConfigurableDictionaryNode> { defOrGloss }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { senses },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var entryOne = CreateInterestingLexEntry(Cache);

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entryOne, mainEntryNode, null, settings).ToString();
			const string senseWithHyphenSuffix = "//span[@class='senses_-test-']/span[@class='sense_-test-']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(senseWithHyphenSuffix, 1);
		}

		[Test]
		public void GenerateContentForEntry_HeadWordRefVirtualPropWorks()
		{
			var wsOpts = GetWsOptionsForLanguages(new[] { "vernacular" });
			const string headWord = "mlhw";
			var mlHeadWordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "HeadWordRef",
				CSSClassNameOverride = headWord,
				DictionaryNodeOptions = wsOpts
			};
			const string nters = "nters";
			var nonTrivialRoots = new ConfigurableDictionaryNode
			{
				FieldDescription = "NonTrivialEntryRoots",
				DictionaryNodeOptions = wsOpts,
				Children = new List<ConfigurableDictionaryNode> { mlHeadWordNode },
				CSSClassNameOverride = nters
			};
			var otherRefForms = new ConfigurableDictionaryNode
			{
				FieldDescription = "ComplexFormsNotSubentries",
				Children = new List<ConfigurableDictionaryNode> { nonTrivialRoots },
				CSSClassNameOverride = "cfns"
			};
			var senses = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				Children = new List<ConfigurableDictionaryNode> { otherRefForms }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { senses },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			// Build up model that will allow for testing of the MLHeadword virtual property under
			// the NonTrivialEntryRoots back reference field.
			var entryOne = CreateInterestingLexEntry(Cache);
			var entryTwo = CreateInterestingLexEntry(Cache);
			var entryThree = CreateInterestingLexEntry(Cache);
			const string entryThreeForm = "MLHW";
			AddHeadwordToEntry(entryThree, entryThreeForm, m_wsFr);
			var complexEntryRef = Cache.ServiceLocator.GetInstance<ILexEntryRefFactory>().Create();
			entryTwo.EntryRefsOS.Add(complexEntryRef);
			complexEntryRef.RefType = LexEntryRefTags.krtComplexForm;
			complexEntryRef.ComponentLexemesRS.Add(entryOne.SensesOS[0]);
			complexEntryRef.ComponentLexemesRS.Add(entryThree);
			complexEntryRef.PrimaryLexemesRS.Add(entryThree);
			complexEntryRef.ShowComplexFormsInRS.Add(entryThree);
			complexEntryRef.ShowComplexFormsInRS.Add(entryOne.SensesOS[0]);

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			// SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entryOne, mainEntryNode, null, settings).ToString();
			var headwordMatch = string.Format("//span[@class='{0}']//span[@class='{1}']/span[text()='{2}']",
				nters, headWord, entryThreeForm);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(headwordMatch, 1);
		}

		[Test]
		public void GenerateContentForEntry_EtymologyLanguageWorks()
		{
			//This test also proves to verify that .NET String properties can be generated
			var abbrNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Abbreviation",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" })
			};
			var etymology = new ConfigurableDictionaryNode
			{
				FieldDescription = "EtymologyOS",
				CSSClassNameOverride = "etymologies",
				Children = new List<ConfigurableDictionaryNode>
					{
						new ConfigurableDictionaryNode()
						{
							Label = "Source Language",
							FieldDescription = "LanguageRS",
							CSSClassNameOverride = "languages",
							Children = new List<ConfigurableDictionaryNode> { abbrNode }
						},
						new ConfigurableDictionaryNode
						{
							Label = "Source Language Notes",
							FieldDescription = "LanguageNotes",
							DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "analysis" })
						}
					}
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { etymology },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var language = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
			Cache.LangProject.LexDbOA.LanguagesOA.PossibilitiesOS.Add(language);
			language.Abbreviation.set_String(m_wsEn, TsStringUtils.MakeString("ar", m_wsEn));
			language.Name.set_String(m_wsEn, TsStringUtils.MakeString("Arabic", m_wsEn));
			var entryOne = CreateInterestingLexEntry(Cache);
			var etym = Cache.ServiceLocator.GetInstance<ILexEtymologyFactory>().Create();
			entryOne.EtymologyOS.Add(etym);
			etym.LanguageNotes.SetAnalysisDefaultWritingSystem("Georgian");
			etym.LanguageRS.Add(language);

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entryOne, mainEntryNode, null, settings).ToString();
			const string etymologyWithArabicSrcLanguage = "//span[@class='etymologies']/span[@class='etymology']/span[@class='languages']/span[@class='language']/span[@class='abbreviation']/span[@lang='en' and text()='ar']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(etymologyWithArabicSrcLanguage, 1);
			const string etymologyWithGeorgianNotes = "//span[@class='etymologies']/span[@class='etymology']/span[@class='languagenotes']/span[@lang='en' and text()='Georgian']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(etymologyWithGeorgianNotes, 1);
		}

		[Test]
		public void GenerateEntryHtmlWithStyles_NullEntryThrowsArgumentNull()
		{
			Assert.Throws<ArgumentNullException>(() => LcmXhtmlGenerator.GenerateEntryHtmlWithStyles(null, new DictionaryConfigurationModel(), null, null));
		}

		[Test]
		public void GenerateEntryHtmlWithStyles_SelectsDirectionUsingDictionaryNormal()
		{
			try
			{
				SetDictionaryNormalDirection(new InheritableStyleProp<TriStateBool>(TriStateBool.triTrue));
				var configModel = CreateInterestingConfigurationModel(Cache, m_propertyTable);
				var mainEntry = CreateInterestingLexEntry(Cache);
				//SUT
				var xhtml = LcmXhtmlGenerator.GenerateEntryHtmlWithStyles(mainEntry, configModel, DefaultDecorator, m_propertyTable);
				// this test relies on specific test data from CreateInterestingConfigurationModel
				const string xpath = "/html[@dir='rtl']/body[@dir='rtl']/div[@class='lexentry']/span[@class='entry']";
				AssertThatXmlIn.String(xhtml).HasSpecifiedNumberOfMatchesForXpath(xpath, 1);
			}
			finally
			{
				SetDictionaryNormalDirection(new InheritableStyleProp<TriStateBool>()); // unset direction
			}
		}

		[Test]
		public void GenerateEntryHtmlWithStyles_MinorEntryUsesMinorEntryFormatting(
			[Values(DictionaryNodeListOptions.ListIds.Complex, DictionaryNodeListOptions.ListIds.Variant)] DictionaryNodeListOptions.ListIds type)
		{
			var mainEntry = CreateInterestingLexEntry(Cache);
			var minorEntry = CreateInterestingLexEntry(Cache);
			if (type == DictionaryNodeListOptions.ListIds.Complex)
				CreateComplexForm(Cache, mainEntry, minorEntry, false);
			else
				CreateVariantForm(Cache, mainEntry, minorEntry);
			SetPublishAsMinorEntry(minorEntry, true);
			var configModel = CreateInterestingConfigurationModel(Cache, m_propertyTable);
			//SUT
			var xhtml = LcmXhtmlGenerator.GenerateEntryHtmlWithStyles(minorEntry, configModel, DefaultDecorator, m_propertyTable);
			// this test relies on specific test data from CreateInterestingConfigurationModel
			const string xpath = "/html/body/div[@class='minorentry']/span[@class='entry']";
			AssertThatXmlIn.String(xhtml).HasSpecifiedNumberOfMatchesForXpath(xpath, 1);
		}

		[Test]
		public void GenerateEntryHtmlWithStyles_MinorEntryUnCheckedItemsGenerateNothing()
		{
			var configModel = CreateInterestingConfigurationModel(Cache, m_propertyTable);
			var mainEntry = CreateInterestingLexEntry(Cache);
			var minorEntry = CreateInterestingLexEntry(Cache);
			CreateVariantForm(Cache, mainEntry, minorEntry);
			configModel.Parts[1].DictionaryNodeOptions = configModel.Parts[2].DictionaryNodeOptions =
				GetListOptionsForItems(DictionaryNodeListOptions.ListIds.Minor, new ICmPossibility[0]);
			SetPublishAsMinorEntry(minorEntry, true);
			//SUT
			var xhtml = LcmXhtmlGenerator.GenerateEntryHtmlWithStyles(minorEntry, configModel, DefaultDecorator, m_propertyTable);
			// this test relies on specific test data from CreateInterestingConfigurationModel
			const string xpath = "/html/body/div[@class='minorentry']/span[@class='entry']";
			// only the variant is selected, so the other minor entry should not have been generated
			AssertThatXmlIn.String(xhtml).HasSpecifiedNumberOfMatchesForXpath(xpath, 0);
		}

		[Test]
		public void GenerateEntryHtmlWithStyles_DoesNotShowHiddenMinorEntries()
		{
			var configModel = CreateInterestingConfigurationModel(Cache, m_propertyTable);
			var mainEntry = CreateInterestingLexEntry(Cache);
			var minorEntry = CreateInterestingLexEntry(Cache);
			CreateVariantForm(Cache, mainEntry, minorEntry);
			SetPublishAsMinorEntry(minorEntry, false);

			//SUT
			var xhtml = LcmXhtmlGenerator.GenerateEntryHtmlWithStyles(minorEntry, configModel, DefaultDecorator, m_propertyTable);
			// this test relies on specific test data from CreateInterestingConfigurationModel
			const string xpath = "/div[@class='minorentry']/span[@class='entry']";
			AssertThatXmlIn.String(xhtml).HasNoMatchForXpath(xpath);
		}

		[Test]
		public void GenerateEntryHtmlWithStyles_DoesNotShowMinorEntriesTwice([Values(true, false)] bool verifyPrefersVariant)
		{
			var mainEntry = CreateInterestingLexEntry(Cache);
			var minorEntry = CreateInterestingLexEntry(Cache);
			CreateComplexForm(Cache, mainEntry, minorEntry, false);
			CreateVariantForm(Cache, mainEntry, minorEntry);
			SetPublishAsMinorEntry(minorEntry, true);
			var configModel = CreateInterestingConfigurationModel(Cache, m_propertyTable);
			if (verifyPrefersVariant) // Exclude Complex Form Parts from those counted.
				configModel.Parts.Where(part => part.Label.Contains("Complex")).ForEach(part => part.CSSClassNameOverride = "complexentry");
			Assert.That(ConfiguredLcmGenerator.IsListItemSelectedForExport(configModel.Parts[1], minorEntry),
				"This test is valid only if the minor entry matches more than one node");
			Assert.That(ConfiguredLcmGenerator.IsListItemSelectedForExport(configModel.Parts[2], minorEntry),
				"This test is valid only if the minor entry matches more than one node");
			//SUT
			var xhtml = LcmXhtmlGenerator.GenerateEntryHtmlWithStyles(minorEntry, configModel, DefaultDecorator, m_propertyTable);
			// this test relies on specific test data from CreateInterestingConfigurationModel
			const string xpath = "/html/body/div[@class='minorentry']/span[@class='entry']";
			AssertThatXmlIn.String(xhtml).HasSpecifiedNumberOfMatchesForXpath(xpath, 1);
		}

		[Test]
		public void GenerateContentForEntry_LexemeBasedConsidersComplexFormsMainEntries()
		{
			var configModel = CreateInterestingConfigurationModel(Cache, m_propertyTable);
			for (var i = 1; i < configModel.Parts.Count; i++)
				configModel.Parts[i].IsEnabled = false; // don't display Minor entries
			var componentEntry = CreateInterestingLexEntry(Cache);
			var complexEntry = CreateInterestingLexEntry(Cache);
			CreateComplexForm(Cache, componentEntry, complexEntry, false);
			configModel.Parts[1].DictionaryNodeOptions = configModel.Parts[2].DictionaryNodeOptions =
				GetListOptionsForItems(DictionaryNodeListOptions.ListIds.Minor, new ICmPossibility[0]);
			SetPublishAsMinorEntry(complexEntry, false);
			//SUT
			var xhtml = LcmXhtmlGenerator.GenerateEntryHtmlWithStyles(complexEntry, configModel, DefaultDecorator, m_propertyTable);
			// this test relies on specific test data from CreateInterestingConfigurationModel
			const string xpath = "/html/body/div[@class='minorentry']/span[@class='entry']";
			// only the variant is selected, so the other minor entry should not have been generated
			AssertThatXmlIn.String(xhtml).HasSpecifiedNumberOfMatchesForXpath(xpath, 0);
		}

		/// <summary>
		/// If the numbering style for Senses says to number it, and
		/// if this is not the only sense, then number it.
		/// (See LT-17906.)
		/// Also verify that custom homograph numbers are used and we can count past 9 with them
		/// </summary>
		[TestCase("en", null)]
		[TestCase("fr", null)]
		[TestCase("fr", new [] { "y", "1", "2", "3", "4", "5", "6", "7", "8", "9" })]
		public void GenerateContentForEntry_SenseNumbersGeneratedForMultipleSenses(string homographWs, string[] customHomographs)
		{
			var homographConfig = Cache.ServiceLocator.GetInstance<HomographConfiguration>();
			var tenthSenseNumber = customHomographs == null ? "10" : customHomographs[1] + customHomographs[0];
			homographConfig.WritingSystem = homographWs;
			if (customHomographs != null)
			{
				homographConfig.CustomHomographNumbers = new List<string>(customHomographs);
			}
			var wsOpts = GetWsOptionsForLanguages(new[] { "en" });
			var glossNode = new ConfigurableDictionaryNode { FieldDescription = "Gloss", DictionaryNodeOptions = wsOpts };
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "Senses",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions { NumberingStyle = "%d" },
				Children = new List<ConfigurableDictionaryNode> { glossNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { sensesNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var testEntry = CreateInterestingLexEntry(Cache);
			AddSenseToEntry(testEntry, "second gloss", m_wsEn, Cache);
			AddSenseToEntry(testEntry, "3", m_wsEn, Cache);
			AddSenseToEntry(testEntry, "4", m_wsEn, Cache);
			AddSenseToEntry(testEntry, "5", m_wsEn, Cache);
			AddSenseToEntry(testEntry, "6", m_wsEn, Cache);
			AddSenseToEntry(testEntry, "7", m_wsEn, Cache);
			AddSenseToEntry(testEntry, "8", m_wsEn, Cache);
			AddSenseToEntry(testEntry, "9", m_wsEn, Cache);
			AddSenseToEntry(testEntry, "10", m_wsEn, Cache);
			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(testEntry, mainEntryNode, DefaultDecorator, settings).ToString();
			string senseNumberOne = $"/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense' and preceding-sibling::span[@class='sensenumber' and @lang='{homographWs}' and text()='1']]//span[@lang='en' and text()='gloss']";
			string senseNumberTwo = $"/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense' and preceding-sibling::span[@class='sensenumber' and @lang='{homographWs}' and  text()='2']]//span[@lang='en' and text()='second gloss']";
			string senseNumberTen = $"//span[@class='sensecontent']/spansenses/span[@class='sense' and preceding-sibling::span[@class='sensenumber' and @lang='{homographWs}' and  text()='{tenthSenseNumber}']]//span[@lang='en' and text()='10']";
			//This assert is dependent on the specific entry data created in CreateInterestingLexEntry
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(senseNumberOne, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(senseNumberTwo, 1);

			// Piggy-back a test for ShouldThisSenseBeNumbered
			Assert.That(ConfiguredLcmGenerator.ShouldThisSenseBeNumbered(testEntry.AllSenses.First(), sensesNode, testEntry.SensesOS), Is.True);
		}

		/// <summary>
		/// If the numbering style for subsenses says to number it, and
		/// if this is not the only subsense, then number the subsense.
		/// (See LT-17906.)
		/// </summary>
		[Test]
		public void ShouldThisSenseBeNumbered_SubSenseNumbersRequestedForMultipleSubSenses()
		{
			var wsOpts = GetWsOptionsForLanguages(new[] { "en" });
			var glossNode = new ConfigurableDictionaryNode { FieldDescription = "Gloss", DictionaryNodeOptions = wsOpts };
			var senseOptions = new DictionaryNodeSenseOptions
			{
				AfterNumber = ")",
				NumberStyle = "Dictionary-SenseNumber",
				NumberingStyle = "%d",
				NumberEvenASingleSense = false
			};
			var subSenseOptions = new DictionaryNodeSenseOptions
			{
				AfterNumber = ")",
				NumberStyle = "Dictionary-SenseNumber",
				NumberingStyle = "%a",
				NumberEvenASingleSense = false,
			};
			var subSenseNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "Senses",
				DictionaryNodeOptions = subSenseOptions,
				Children = new List<ConfigurableDictionaryNode> { glossNode },
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "Senses",
				DictionaryNodeOptions = senseOptions,
				Children = new List<ConfigurableDictionaryNode> { glossNode, subSenseNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { sensesNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			subSenseNode.IsEnabled = false;
			var testEntry = CreateInterestingLexEntry(Cache);
			AddSubSenseToSense("ss1", testEntry.AllSenses.First());
			AddSubSenseToSense("ss2", testEntry.AllSenses.First());

			Assert.That(ConfiguredLcmGenerator.ShouldThisSenseBeNumbered(testEntry.SensesOS[0].SensesOS[0], subSenseNode, testEntry.SensesOS[0].SensesOS), Is.True);
		}

		/// <summary>
		/// Part of LT-17906.
		/// </summary>
		[Test]
		public void AreThereEnabledSubsensesWithNumberingStyle_SubsensesEnabledOrNot()
		{
			var subSenseNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "Senses",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions()
				{
					NumberStyle = "Dictionary-SenseNumber",
					NumberingStyle = "%d"
				},
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "Senses",
				Children = new List<ConfigurableDictionaryNode> { subSenseNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(sensesNode);
			subSenseNode.IsEnabled = false; // Unchecked in the configuration dialog

			Assert.That(ConfiguredLcmGenerator.AreThereEnabledSubsensesWithNumberingStyle(sensesNode), Is.False, "Should have noticed that there are no enabled subsense nodes");

			// Okay, but if they are showing...
			subSenseNode.IsEnabled = true;

			Assert.That(ConfiguredLcmGenerator.AreThereEnabledSubsensesWithNumberingStyle(sensesNode), Is.True, "Should have seen the enabled subsense node.");
		}

		/// <summary>
		/// Part of LT-17906.
		/// </summary>
		[Test]
		public void AreThereEnabledSubsensesWithNumberingStyle_SubsensesHaveNumberingStyleOrNot()
		{
			var subSenseNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "Senses",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions
				{
					NumberStyle = "Dictionary-SenseNumber",
					NumberingStyle = string.Empty
				},
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "Senses",
				Children = new List<ConfigurableDictionaryNode> { subSenseNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(sensesNode);

			Assert.That(ConfiguredLcmGenerator.AreThereEnabledSubsensesWithNumberingStyle(sensesNode), Is.False, "Should have return false since no numbering style");

			// Okay, but if the style for the subsense does say to number the subsenses...
			((DictionaryNodeSenseOptions)subSenseNode.DictionaryNodeOptions).NumberingStyle = "%d";

			Assert.That(ConfiguredLcmGenerator.AreThereEnabledSubsensesWithNumberingStyle(sensesNode), Is.True, "Should have return true since there is a numbering style");
		}

		/// <summary>
		/// If the numbering style for Senses says to number it, and
		/// if this is the only sense, and
		/// if the box for "Number even a single sense" is NOT ticked, and
		/// if there are no subsenses,
		/// then do not number the sense.
		/// (See LT-17906.)
		/// </summary>
		[Test]
		public void GenerateContentForEntry_SingleSenseGetsNoSenseNumber()
		{

			var wsOpts = GetWsOptionsForLanguages(new[] { "en" });
			var glossNode = new ConfigurableDictionaryNode { FieldDescription = "Gloss", DictionaryNodeOptions = wsOpts };
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "Senses",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions { NumberEvenASingleSense = false, NumberingStyle = "%d" },
				Children = new List<ConfigurableDictionaryNode> { glossNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { sensesNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var testEntry = CreateInterestingLexEntry(Cache);

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			Assert.That(testEntry.AllSenses.Count, Is.EqualTo(1), "Test set up incorrectly. There should just be one sense.");
			Assert.That(testEntry.AllSenses.First().AllSenses.Count, Is.EqualTo(1), "Test not set up correctly. There should be no subsenses.");
			// SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(testEntry, mainEntryNode, DefaultDecorator, settings).ToString();
			const string senseNumberOne = "/div[@class='lexentry']/span[@class='senses']/span[@class='sense' and preceding-sibling::span[@class='sensenumber' and text()='1']]/span[@lang='en' and text()='gloss']";
			// This assert is dependent on the specific entry data created in CreateInterestingLexEntry
			AssertThatXmlIn.String(result).HasNoMatchForXpath(senseNumberOne);

			// Piggy-back a test for ShouldThisSenseBeNumbered
			Assert.That(ConfiguredLcmGenerator.ShouldThisSenseBeNumbered(testEntry.AllSenses.First(), sensesNode, testEntry.SensesOS), Is.False);
		}

		/// <summary>
		/// If the numbering style for Senses says to number it, and
		/// if this is the only sense (at the currently-being examined level), and
		/// if the box for "Number even a single sense" is NOT ticked, and
		/// if there ARE subsenses, and
		/// if the subsenses are not showing (turned off in the config),
		/// then do not number the sense.
		/// (See LT-17906.)
		/// </summary>
		[Test]
		public void GenerateContentForEntry_TurnedOffSubsensesCausesSenseToBehaveLikeSingleSense_WithNoSenseNumber()
		{

			var wsOpts = GetWsOptionsForLanguages(new[] { "en" });
			var glossNode = new ConfigurableDictionaryNode { FieldDescription = "Gloss", DictionaryNodeOptions = wsOpts };
			var senseOptions = new DictionaryNodeSenseOptions
			{
				AfterNumber = ")",
				NumberStyle = "Dictionary-SenseNumber",
				NumberingStyle = "%d",
				NumberEvenASingleSense = false
			};
			var subSenseOptions = new DictionaryNodeSenseOptions
			{
				AfterNumber = ")",
				NumberStyle = "Dictionary-SenseNumber",
				NumberingStyle = "%a",
				NumberEvenASingleSense = false,
			};
			var subSenseNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "Senses",
				DictionaryNodeOptions = subSenseOptions,
				Children = new List<ConfigurableDictionaryNode> { glossNode },
				IsEnabled = false // Unchecked in the configuration dialog
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "Senses",
				DictionaryNodeOptions = senseOptions,
				Children = new List<ConfigurableDictionaryNode> { glossNode, subSenseNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { sensesNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			subSenseNode.IsEnabled = false;
			var testEntry = CreateInterestingLexEntry(Cache);
			AddSubSenseToSense("ss1", testEntry.AllSenses.First());
			AddSubSenseToSense("ss2", testEntry.AllSenses.First());

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			Assert.That(testEntry.AllSenses.Count, Is.EqualTo(3), "Test set up incorrectly.");
			Assert.That(testEntry.AllSenses.First().AllSenses.Count, Is.EqualTo(3), "Test not set up correctly.");
			// SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(testEntry, mainEntryNode, DefaultDecorator, settings).ToString();
			const string senseNumberXpath = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sensenumber']";
			AssertThatXmlIn.String(result).HasNoMatchForXpath(senseNumberXpath); // Should not have a sense number on top sense.

			// Piggy-back a test for ShouldThisSenseBeNumbered
			Assert.That(ConfiguredLcmGenerator.ShouldThisSenseBeNumbered(testEntry.AllSenses.First(), sensesNode, testEntry.SensesOS), Is.False);
		}

		/// <summary>
		/// If the numbering style for Senses says to number it, and
		/// if this is the only sense, and
		/// if the box for "Number even a single sense" is NOT ticked, and
		/// if there ARE subsenses, and
		/// if the subsenses are showing (turned on in the config), and
		/// if the style for the subsense says NOT to number the subsense,
		/// then do not number the sense.
		/// (See LT-17906.)
		/// </summary>
		[Test]
		public void GenerateContentForEntry_EmptyStyleSubsensesCausesSenseToBehaveLikeSingleSense_WithNoSenseNumber()
		{

			var wsOpts = GetWsOptionsForLanguages(new[] { "en" });
			var glossNode = new ConfigurableDictionaryNode { FieldDescription = "Gloss", DictionaryNodeOptions = wsOpts };
			var senseOptions = new DictionaryNodeSenseOptions
			{
				AfterNumber = ")",
				NumberStyle = "Dictionary-SenseNumber",
				NumberingStyle = "%d",
				NumberEvenASingleSense = false
			};
			var subSenseOptions = new DictionaryNodeSenseOptions
			{
				AfterNumber = ")",
				NumberStyle = "Dictionary-SenseNumber",
				NumberingStyle = "", // Subsense has empty numbering style
				NumberEvenASingleSense = false,
			};
			var subSenseNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "Senses",
				DictionaryNodeOptions = subSenseOptions,
				Children = new List<ConfigurableDictionaryNode> { glossNode },
				IsEnabled = true // Checked in the configuration dialog
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "Senses",
				DictionaryNodeOptions = senseOptions,
				Children = new List<ConfigurableDictionaryNode> { glossNode, subSenseNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { sensesNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var testEntry = CreateInterestingLexEntry(Cache);
			AddSubSenseToSense("ss1", testEntry.AllSenses.First());
			AddSubSenseToSense("ss2", testEntry.AllSenses.First());

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			Assert.That(testEntry.AllSenses.Count, Is.EqualTo(3), "Test set up incorrectly.");
			Assert.That(testEntry.AllSenses.First().AllSenses.Count, Is.EqualTo(3), "Test not set up correctly.");
			// SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(testEntry, mainEntryNode, DefaultDecorator, settings).ToString();
			const string senseNumberXpath = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sensenumber']";
			AssertThatXmlIn.String(result).HasNoMatchForXpath(senseNumberXpath); // Should not have a sense number on top sense.

			// Piggy-back a test for ShouldThisSenseBeNumbered
			Assert.That(ConfiguredLcmGenerator.ShouldThisSenseBeNumbered(testEntry.AllSenses.First(), sensesNode, testEntry.SensesOS), Is.False);
		}

		/// <summary>
		/// If the numbering style for Senses says to number it, and
		/// if this is the only sense, and
		/// if the box for "Number even a single sense" is NOT ticked, and
		/// if there ARE subsenses, and
		/// if the subsenses are showing (turned on in the config), and
		/// if the style for the subsense says to number the subsense,
		/// then number the sense.
		/// (See LT-17906.)
		/// </summary>
		[Test]
		public void GenerateContentForEntry_SubsenseStyleInfluencesSenseNumberShown()
		{

			var wsOpts = GetWsOptionsForLanguages(new[] { "en" });
			var glossNode = new ConfigurableDictionaryNode { FieldDescription = "Gloss", DictionaryNodeOptions = wsOpts };
			var senseOptions = new DictionaryNodeSenseOptions
			{
				AfterNumber = ")",
				NumberStyle = "Dictionary-SenseNumber",
				NumberingStyle = "%d",
				NumberEvenASingleSense = false
			};
			var subSenseOptions = new DictionaryNodeSenseOptions
			{
				AfterNumber = ")",
				NumberStyle = "Dictionary-SenseNumber",
				NumberingStyle = "%d", // Subsense has numbering style
				NumberEvenASingleSense = false,
			};
			var subSenseNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "Senses",
				DictionaryNodeOptions = subSenseOptions,
				Children = new List<ConfigurableDictionaryNode> { glossNode },
				IsEnabled = true // Checked in the configuration dialog
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "Senses",
				DictionaryNodeOptions = senseOptions,
				Children = new List<ConfigurableDictionaryNode> { glossNode, subSenseNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { sensesNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var testEntry = CreateInterestingLexEntry(Cache);
			AddSubSenseToSense("ss1", testEntry.AllSenses.First());
			AddSubSenseToSense("ss2", testEntry.AllSenses.First());

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			Assert.That(testEntry.AllSenses.Count, Is.EqualTo(3), "Test set up incorrectly.");
			Assert.That(testEntry.AllSenses.First().AllSenses.Count, Is.EqualTo(3), "Test not set up correctly.");
			// SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(testEntry, mainEntryNode, DefaultDecorator, settings).ToString();
			const string senseNumberXpath = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sensenumber']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(senseNumberXpath, 1); // Should have sense number on top sense.

			// Piggy-back a test for ShouldThisSenseBeNumbered
			Assert.That(ConfiguredLcmGenerator.ShouldThisSenseBeNumbered(testEntry.AllSenses.First(), sensesNode, testEntry.SensesOS), Is.True);
		}

		[Test]
		public void GenerateContentForEntry_NumberingSingleSenseAlsoCountsSubSense()
		{
			var wsOpts = GetWsOptionsForLanguages(new[] { "en" });
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

			var glossNode = new ConfigurableDictionaryNode { FieldDescription = "Gloss", DictionaryNodeOptions = wsOpts };
			var subSenseNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				DictionaryNodeOptions = DictionaryNodeSenseOptions,
				Children = new List<ConfigurableDictionaryNode> { glossNode }
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				DictionaryNodeOptions = DictionaryNodeSenseOptions,
				Children = new List<ConfigurableDictionaryNode> { glossNode, subSenseNode }
			};

			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { sensesNode }
			};

			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var testEntry = CreateInterestingLexEntry(Cache);
			AddSingleSubSenseToSense("gloss", testEntry.SensesOS.First());
			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(testEntry, mainEntryNode, DefaultDecorator, settings).ToString();
			const string SenseOneSubSense = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense' and preceding-sibling::span[@class='sensenumber' and text()='1']]/span[@class='senses']/span[@class='sensecontent']//span[@lang='en' and text()='gloss1.1']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(SenseOneSubSense, 1);
		}

		[Test]
		public void GenerateContentForEntry_SensesAndSubSensesWithDifferentNumberingStyle()
		{
			var wsOpts = GetWsOptionsForLanguages(new[] { "en" });
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

			var glossNode = new ConfigurableDictionaryNode { FieldDescription = "Gloss", DictionaryNodeOptions = wsOpts };
			var subSenseNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				DictionaryNodeOptions = DictionaryNodeSubSenseOptions,
				Children = new List<ConfigurableDictionaryNode> { glossNode }
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				DictionaryNodeOptions = DictionaryNodeSenseOptions,
				Children = new List<ConfigurableDictionaryNode> { glossNode, subSenseNode }
			};

			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { sensesNode }
			};

			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var testEntry = CreateInterestingLexEntry(Cache);
			AddSenseAndTwoSubsensesToEntry(testEntry, "second gloss");
			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(testEntry, mainEntryNode, DefaultDecorator, settings).ToString();
			const string senseNumberOne = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense' and preceding-sibling::span[@class='sensenumber' and text()='A']]//span[@lang='en' and text()='gloss']";
			const string senseNumberTwo = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense' and preceding-sibling::span[@class='sensenumber' and text()='B']]//span[@lang='en' and text()='second gloss']";
			const string subSensesNumberTwoOne = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense' and preceding-sibling::span[@class='sensenumber' and text()='I']]//span[@lang='en' and text()='second gloss2.1']";
			const string subSenseNumberTwoTwo = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense' and preceding-sibling::span[@class='sensenumber' and text()='II']]//span[@lang='en' and text()='second gloss2.2']";
			//This assert is dependent on the specific entry data created in CreateInterestingLexEntry
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(senseNumberOne, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(senseNumberTwo, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(subSensesNumberTwoOne, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(subSenseNumberTwoTwo, 1);
		}

		[Test]
		public void GenerateContentForEntry_SensesAndSubSensesWithNumberingStyle()
		{

			var wsOpts = GetWsOptionsForLanguages(new[] { "en" });
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
				NumberingStyle = "%d",
				DisplayEachSenseInAParagraph = false,
				NumberEvenASingleSense = true,
				ShowSharedGrammarInfoFirst = false
			};

			var glossNode = new ConfigurableDictionaryNode { FieldDescription = "Gloss", DictionaryNodeOptions = wsOpts };
			var subSenseNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				DictionaryNodeOptions = DictionaryNodeSubSenseOptions,
				Children = new List<ConfigurableDictionaryNode> { glossNode }
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				DictionaryNodeOptions = DictionaryNodeSenseOptions,
				Children = new List<ConfigurableDictionaryNode> { glossNode, subSenseNode }
			};

			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { sensesNode }
			};

			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var testEntry = CreateInterestingLexEntry(Cache);
			AddSenseAndTwoSubsensesToEntry(testEntry, "second gloss");
			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(testEntry, mainEntryNode, DefaultDecorator, settings).ToString();
			const string senseNumberOne = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense' and preceding-sibling::span[@class='sensenumber' and text()='A']]//span[@lang='en' and text()='gloss']";
			const string senseNumberTwo = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense' and preceding-sibling::span[@class='sensenumber' and text()='B']]//span[@lang='en' and text()='second gloss']";
			const string subSensesNumberTwoOne = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense' and preceding-sibling::span[@class='sensenumber' and text()='1']]//span[@lang='en' and text()='second gloss2.1']";
			const string subSenseNumberTwoTwo = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense' and preceding-sibling::span[@class='sensenumber' and text()='2']]//span[@lang='en' and text()='second gloss2.2']";
			//This assert is dependent on the specific entry data created in CreateInterestingLexEntry
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(senseNumberOne, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(senseNumberTwo, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(subSensesNumberTwoOne, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(subSenseNumberTwoTwo, 1);
		}

		/// <summary>
		/// If the numbering style for Senses says not to number it, don't.
		/// (See LT-17906.)
		/// </summary>
		[Test]
		public void GenerateContentForEntry_NoSenseNumberFIfStyleSaysNoNumbering()
		{

			var wsOpts = GetWsOptionsForLanguages(new[] { "en" });
			var DictionaryNodeSenseOptions = new DictionaryNodeSenseOptions
			{
				BeforeNumber = "",
				AfterNumber = ")",
				NumberStyle = "Dictionary-SenseNumber",
				NumberingStyle = string.Empty,
				DisplayEachSenseInAParagraph = false,
				NumberEvenASingleSense = true,
				ShowSharedGrammarInfoFirst = false
			};

			var glossNode = new ConfigurableDictionaryNode { FieldDescription = "Gloss", DictionaryNodeOptions = wsOpts };
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				DictionaryNodeOptions = DictionaryNodeSenseOptions,
				Children = new List<ConfigurableDictionaryNode> { glossNode }
			};

			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { sensesNode }
			};

			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var testEntry = CreateInterestingLexEntry(Cache);
			AddSenseToEntry(testEntry, "gloss", m_wsEn, Cache);
			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(testEntry, mainEntryNode, DefaultDecorator, settings).ToString();
			const string senseNumberXpath = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sensenumber']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(senseNumberXpath, 0); // Should not have produced sense number if style said not to number it.

			// Piggy-back a test for ShouldThisSenseBeNumbered
			Assert.That(ConfiguredLcmGenerator.ShouldThisSenseBeNumbered(testEntry.AllSenses.First(), sensesNode, testEntry.SensesOS), Is.False);
		}

		[Test]
		public void GenerateContentForEntry_SensesNoneAndSubSensesWithNumberingStyle()
		{

			var wsOpts = GetWsOptionsForLanguages(new[] { "en" });
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

			var glossNode = new ConfigurableDictionaryNode { FieldDescription = "Gloss", DictionaryNodeOptions = wsOpts };
			var subSenseNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				DictionaryNodeOptions = DictionaryNodeSubSenseOptions,
				Children = new List<ConfigurableDictionaryNode> { glossNode }
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				DictionaryNodeOptions = DictionaryNodeSenseOptions,
				Children = new List<ConfigurableDictionaryNode> { glossNode, subSenseNode }
			};

			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { sensesNode }
			};

			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var testEntry = CreateInterestingLexEntry(Cache);
			AddSenseAndTwoSubsensesToEntry(testEntry, "second gloss");
			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(testEntry, mainEntryNode, DefaultDecorator, settings).ToString();
			const string subSensesNumberOne = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense' and preceding-sibling::span[@class='sensenumber' and text()='1']]//span[@lang='en' and text()='second gloss2.1']";
			const string subSenseNumberTwo = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense' and preceding-sibling::span[@class='sensenumber' and text()='2']]//span[@lang='en' and text()='second gloss2.2']";
			//This assert is dependent on the specific entry data created in CreateInterestingLexEntry
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(subSensesNumberOne, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(subSenseNumberTwo, 1);
		}

		[Test]
		public void GenerateContentForEntry_SensesGeneratedForMultipleSubSenses()
		{

			var wsOpts = GetWsOptionsForLanguages(new[] { "en" });
			var senseOptions = new DictionaryNodeSenseOptions { AfterNumber = ")", NumberingStyle = "%d", NumberEvenASingleSense = true };

			var glossNode = new ConfigurableDictionaryNode { FieldDescription = "Gloss", DictionaryNodeOptions = wsOpts };
			var subSenseNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				DictionaryNodeOptions = senseOptions,
				Children = new List<ConfigurableDictionaryNode> { glossNode }
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				DictionaryNodeOptions = senseOptions,
				Children = new List<ConfigurableDictionaryNode> { glossNode, subSenseNode }
			};

			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { sensesNode }
			};

			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var testEntry = CreateInterestingLexEntry(Cache);
			AddSenseAndTwoSubsensesToEntry(testEntry, "second gloss");
			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(testEntry, mainEntryNode, DefaultDecorator, settings).ToString();
			const string senseNumberOne = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense' and preceding-sibling::span[@class='sensenumber' and text()='1']]//span[@lang='en' and text()='gloss']";
			const string senseNumberTwo = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense' and preceding-sibling::span[@class='sensenumber' and text()='2']]//span[@lang='en' and text()='second gloss']";
			const string subSensesNumberTwoOne = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense' and preceding-sibling::span[@class='sensenumber' and text()='1']]//span[@lang='en' and text()='second gloss2.1']";
			const string subSenseNumberTwoTwo = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense' and preceding-sibling::span[@class='sensenumber' and text()='2']]//span[@lang='en' and text()='second gloss2.2']";
			//This assert is dependent on the specific entry data created in CreateInterestingLexEntry
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(senseNumberOne, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(senseNumberTwo, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(subSensesNumberTwoOne, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(subSenseNumberTwoTwo, 1);
		}

		[Test]
		public void GenerateContentForEntry_SubSenseParentSenseNumberingStyleJoined()
		{
			var wsOpts = GetWsOptionsForLanguages(new[] { "en" });

			var SubSubSenseOptions = new DictionaryNodeSenseOptions { NumberingStyle = "%A", ParentSenseNumberingStyle = "%j", NumberEvenASingleSense = true };
			var subSenseOptions = new DictionaryNodeSenseOptions { NumberingStyle = "%a", ParentSenseNumberingStyle = "%j" };
			var senseOptions = new DictionaryNodeSenseOptions { NumberingStyle = "%d" };
			var glossNode = new ConfigurableDictionaryNode { FieldDescription = "Gloss", DictionaryNodeOptions = wsOpts };
			var subSubsenses = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				DictionaryNodeOptions = SubSubSenseOptions,
				Children = new List<ConfigurableDictionaryNode> { glossNode }
			};
			var subSenseNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				DictionaryNodeOptions = subSenseOptions,
				Children = new List<ConfigurableDictionaryNode> { subSubsenses, glossNode }
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				DictionaryNodeOptions = senseOptions,
				Children = new List<ConfigurableDictionaryNode> { subSenseNode }
			};

			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { sensesNode }
			};

			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var testEntry = CreateInterestingLexEntry(Cache);
			AddSenseAndTwoSubsensesToEntry(testEntry, "second gloss");
			AddSenseAndTwoSubsensesToEntry(testEntry.SensesOS[1].SensesOS[0], "matte");
			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(testEntry, mainEntryNode, DefaultDecorator, settings).ToString();
			const string senseNumber = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense' and preceding-sibling::span[@class='sensenumber' and text()='2']]";
			const string subSenseNumber = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense' and preceding-sibling::span[@class='sensenumber' and text()='2a']]";
			const string subSubSenseNumber = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense' and preceding-sibling::span[@class='sensenumber' and text()='2aA']]";

			//This assert is dependent on the specific entry data created in CreateInterestingLexEntry
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(senseNumber, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(subSenseNumber, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(subSubSenseNumber, 1);
		}

		[Test]
		public void GenerateContentForEntry_SubSenseParentSenseNumberingStyleSeparatedByDot()
		{
			var wsOpts = GetWsOptionsForLanguages(new[] { "en" });

			var SubSubSenseOptions = new DictionaryNodeSenseOptions { NumberingStyle = "%A", ParentSenseNumberingStyle = "%.", NumberEvenASingleSense = true };
			var subSenseOptions = new DictionaryNodeSenseOptions { NumberingStyle = "%a", ParentSenseNumberingStyle = "%." };
			var senseOptions = new DictionaryNodeSenseOptions { NumberingStyle = "%d" };
			var glossNode = new ConfigurableDictionaryNode { FieldDescription = "Gloss", DictionaryNodeOptions = wsOpts };
			var subSubsenses = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				DictionaryNodeOptions = SubSubSenseOptions,
				Children = new List<ConfigurableDictionaryNode> { glossNode }
			};
			var subSenseNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				DictionaryNodeOptions = subSenseOptions,
				Children = new List<ConfigurableDictionaryNode> { subSubsenses, glossNode }
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				DictionaryNodeOptions = senseOptions,
				Children = new List<ConfigurableDictionaryNode> { subSenseNode }
			};

			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { sensesNode }
			};

			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var testEntry = CreateInterestingLexEntry(Cache);
			AddSenseAndTwoSubsensesToEntry(testEntry, "second gloss");
			AddSenseAndTwoSubsensesToEntry(testEntry.SensesOS[1].SensesOS[0], "matte");
			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(testEntry, mainEntryNode, DefaultDecorator, settings).ToString();
			const string senseNumber = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense' and preceding-sibling::span[@class='sensenumber' and text()='2']]";
			const string subSenseNumber = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense' and preceding-sibling::span[@class='sensenumber' and text()='2.a']]";
			const string subSubSenseNumber = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense' and preceding-sibling::span[@class='sensenumber' and text()='2.a.A']]";

			//This assert is dependent on the specific entry data created in CreateInterestingLexEntry
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(senseNumber, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(subSenseNumber, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(subSubSenseNumber, 1);
		}

		[Test]
		public void GenerateContentForEntry_SubSenseParentSenseNumberingStyleNone()
		{
			var wsOpts = GetWsOptionsForLanguages(new[] { "en" });

			var SubSubSenseOptions = new DictionaryNodeSenseOptions { NumberingStyle = "%A", ParentSenseNumberingStyle = "", NumberEvenASingleSense = true };
			var subSenseOptions = new DictionaryNodeSenseOptions { NumberingStyle = "%a", ParentSenseNumberingStyle = "" };
			var senseOptions = new DictionaryNodeSenseOptions { NumberingStyle = "%d" };
			var glossNode = new ConfigurableDictionaryNode { FieldDescription = "Gloss", DictionaryNodeOptions = wsOpts };
			var subSubsenses = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				DictionaryNodeOptions = SubSubSenseOptions,
				Children = new List<ConfigurableDictionaryNode> { glossNode }
			};
			var subSenseNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				DictionaryNodeOptions = subSenseOptions,
				Children = new List<ConfigurableDictionaryNode> { subSubsenses, glossNode }
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				DictionaryNodeOptions = senseOptions,
				Children = new List<ConfigurableDictionaryNode> { subSenseNode }
			};

			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { sensesNode }
			};

			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var testEntry = CreateInterestingLexEntry(Cache);
			AddSenseAndTwoSubsensesToEntry(testEntry, "second gloss");
			AddSenseAndTwoSubsensesToEntry(testEntry.SensesOS[1].SensesOS[0], "matte");
			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(testEntry, mainEntryNode, DefaultDecorator, settings).ToString();
			const string senseNumber = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense' and preceding-sibling::span[@class='sensenumber' and text()='2']]";
			const string subSenseNumber = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense' and preceding-sibling::span[@class='sensenumber' and text()='a']]";
			const string subSubSenseNumber = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense' and preceding-sibling::span[@class='sensenumber' and text()='A']]";

			//This assert is dependent on the specific entry data created in CreateInterestingLexEntry
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(senseNumber, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(subSenseNumber, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(subSubSenseNumber, 1);
		}

		[Test]
		public void GenerateContentForEntry_SubSubSensesWithNumberingStyle()
		{

			var wsOpts = GetWsOptionsForLanguages(new[] { "en" });
			var senseOptions = new DictionaryNodeSenseOptions { AfterNumber = ")", NumberingStyle = "%d", NumberEvenASingleSense = true };

			var glossNode = new ConfigurableDictionaryNode { FieldDescription = "Gloss", DictionaryNodeOptions = wsOpts };
			var subSenseNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "shares",
				DictionaryNodeOptions = senseOptions
			};
			var kalashnikovSensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				DictionaryNodeOptions = senseOptions,
				Children = new List<ConfigurableDictionaryNode> { glossNode, subSenseNode }
			};
			subSenseNode.ReferencedNode = kalashnikovSensesNode;
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { kalashnikovSensesNode }
			};

			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var testEntry = CreateInterestingLexEntry(Cache);
			AddSenseAndTwoSubsensesToEntry(testEntry, "second gloss");
			AddSenseAndTwoSubsensesToEntry(testEntry.SensesOS[1].SensesOS[0], "matte");
			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(testEntry, mainEntryNode, DefaultDecorator, settings).ToString();
			const string senseNumberOne = "//span[@class='sense' and preceding-sibling::span[@class='sensenumber' and text()='1']]//span[@lang='en' and text()='gloss']";
			const string senseNumberTwo = "//span[@class='sense' and preceding-sibling::span[@class='sensenumber' and text()='2']]//span[@lang='en' and text()='second gloss']";
			const string subSenseNumberTwoOne = "//span[@class='share sense' and preceding-sibling::span[@class='sensenumber' and text()='1']]//span[@lang='en' and text()='second gloss2.1']";
			const string subSenseNumberTwoTwo = "//span[@class='share sense' and preceding-sibling::span[@class='sensenumber' and text()='2']]//span[@lang='en' and text()='second gloss2.2']";
			const string subSubSenseNumberTwoOneOne = "//span[@class='share sense' and preceding-sibling::span[@class='sensenumber' and text()='1']]//span[@lang='en' and text()='matte']";
			const string subSubSubSenseNumberTwoOneOneOne = "//span[@class='share sense' and preceding-sibling::span[@class='sensenumber' and text()='1']]//span[@lang='en' and text()='matte2.1']";
			const string subSubSubSenseNumberTwoOneOneTwo = "//span[@class='share sense' and preceding-sibling::span[@class='sensenumber' and text()='2']]//span[@lang='en' and text()='matte2.2']";
			//This assert is dependent on the specific entry data created in CreateInterestingLexEntry
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(senseNumberOne, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(senseNumberTwo, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(subSenseNumberTwoOne, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(subSenseNumberTwoTwo, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(subSubSenseNumberTwoOneOne, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(subSubSubSenseNumberTwoOneOneOne, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(subSubSubSenseNumberTwoOneOneTwo, 1);
		}

		[Test]
		public void GenerateContentForEntry_GeneratesGramInfoFirstEvenSingleSense()
		{
			var posNoun = CreatePartOfSpeech("noun", "n");

			var firstHeadword = "homme";
			var firstEntry = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(firstEntry, firstHeadword, m_wsFr);
			AddSingleSubSenseToSense("man", firstEntry.SensesOS[0]);
			var msa1 = CreateMSA(firstEntry, posNoun);
			firstEntry.SensesOS[0].MorphoSyntaxAnalysisRA = msa1;
			firstEntry.SensesOS[0].SensesOS[0].MorphoSyntaxAnalysisRA = msa1;

			int flidVirtual = Cache.ServiceLocator.GetInstance<Virtuals>().LexDbEntries;
			var pubEverything = new DictionaryPublicationDecorator(Cache, (ISilDataAccessManaged)Cache.MainCacheAccessor, flidVirtual);

			var categNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLPartOfSpeech",
				DictionaryNodeOptions =
					GetWsOptionsForLanguages(new[] { "analysis" }, DictionaryNodeWritingSystemOptions.WritingSystemType.Analysis),
				Children = new List<ConfigurableDictionaryNode>()
			};
			var gramInfoNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MorphoSyntaxAnalysisRA",
				CSSClassNameOverride = "morphosyntaxanalysis",
				Children = new List<ConfigurableDictionaryNode> { categNode }
			};
			var glossNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Gloss",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" })
			};
			var senseNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions
				{
					BeforeNumber = " ",
					AfterNumber = ") ",
					NumberingStyle = "%d",
					NumberEvenASingleSense = true,
					ShowSharedGrammarInfoFirst = true
				},
				Children = new List<ConfigurableDictionaryNode> { gramInfoNode, glossNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { senseNode },
				FieldDescription = "LexEntry"
			};
			var model = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { mainEntryNode } };
			CssGeneratorTests.PopulateFieldsForTesting(model);

			string xhtmlPath = null;
			try
			{
				xhtmlPath = LcmXhtmlGenerator.SavePreviewHtmlWithStyles(new[] { firstEntry.Hvo }, pubEverything, model, m_propertyTable);
				var xhtml = File.ReadAllText(xhtmlPath);
				// SUT
				const string gramInfoPath = "/html/body/div[@class='lexentry']/span[@class='senses']/span[@class='sharedgrammaticalinfo']/span[@class='morphosyntaxanalysis']/span[@class='mlpartofspeech']/span[@lang='en' and text()='n']";
				AssertThatXmlIn.String(xhtml).HasSpecifiedNumberOfMatchesForXpath(gramInfoPath, 1);

				const string senseNumberPath = "/html/body/div[@class='lexentry']/span[@class='senses']/span[2][@class='sensecontent']/span[@class='sensenumber' and text()='1']";
				AssertThatXmlIn.String(xhtml).HasSpecifiedNumberOfMatchesForXpath(senseNumberPath, 1);

				const string senseTextPath = "/html/body/div[@class='lexentry']/span[@class='senses']/span[2][@class='sensecontent']/span[@class='sense']/span[@class='gloss']/span[@lang='en' and text()='man']";
				AssertThatXmlIn.String(xhtml).HasSpecifiedNumberOfMatchesForXpath(senseTextPath, 1);
			}
			finally
			{
				DeleteTempXhtmlAndCssFiles(xhtmlPath);
			}
		}

		[Test]
		public void GenerateContentForEntry_SubSensesOfSingleSenses_GetFullNumbers()
		{

			var wsOpts = GetWsOptionsForLanguages(new[] { "en" });
			var senseOptions = new DictionaryNodeSenseOptions
			{
				AfterNumber = ")",
				NumberStyle = "Dictionary-SenseNumber",
				NumberingStyle = "%d",
				NumberEvenASingleSense = false
			};
			var subSenseOptions = new DictionaryNodeSenseOptions
			{
				AfterNumber = ")",
				NumberStyle = "Dictionary-SenseNumber",
				NumberingStyle = "%a",
				NumberEvenASingleSense = true
			};

			var glossNode = new ConfigurableDictionaryNode { FieldDescription = "Gloss", DictionaryNodeOptions = wsOpts };
			var subSenseNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "shares",
				DictionaryNodeOptions = subSenseOptions
			};
			var kalashnikovSensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				DictionaryNodeOptions = senseOptions,
				Children = new List<ConfigurableDictionaryNode> { glossNode, subSenseNode }
			};
			subSenseNode.ReferencedNode = kalashnikovSensesNode;
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { kalashnikovSensesNode }
			};

			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var testEntry = CreateInterestingLexEntry(Cache);
			AddSenseAndTwoSubsensesToEntry(testEntry.SensesOS[0], "subGloss");
			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(testEntry, mainEntryNode, DefaultDecorator, settings).ToString();
			const string subSenseNumberOneOne = "//span[@class='share sense' and preceding-sibling::span[@class='sensenumber' and text()='a']]//span[@lang='en' and text()='subGloss']";
			const string subosoSenseNumberOneOneOne = "//span[@lang='en' and text()='subGloss2.1']";
			const string subosoSenseNumberOneOneTwo = "//span[@lang='en' and text()='subGloss2.2']";
			//This assert is dependent on the specific entry data created in CreateInterestingLexEntry
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(subSenseNumberOneOne, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(subosoSenseNumberOneOneOne, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(subosoSenseNumberOneOneTwo, 1);
		}

		/// <summary>Sense numbers for Main Entry->Senses->Subentries->Senses should not contain the Component Sense's number</summary>
		[Test]
		public void GenerateContentForEntry_SubentriesSensesDontGetMainEntrySensesNumbers()
		{

			var wsOpts = GetWsOptionsForLanguages(new[] { "en" });
			var senseOptions = new DictionaryNodeSenseOptions { AfterNumber = ")", NumberingStyle = "%O", NumberEvenASingleSense = true };

			var glossNode = new ConfigurableDictionaryNode { FieldDescription = "Gloss", DictionaryNodeOptions = wsOpts };
			var subEntrySenseNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				DictionaryNodeOptions = senseOptions,
				Children = new List<ConfigurableDictionaryNode> { glossNode }
			};
			var subEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Subentries",
				Children = new List<ConfigurableDictionaryNode> { subEntrySenseNode }
			};
			var mainEntrySenseNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				DictionaryNodeOptions = senseOptions,
				Children = new List<ConfigurableDictionaryNode> { glossNode, subEntryNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { mainEntrySenseNode }
			};

			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var testEntry = CreateInterestingLexEntry(Cache);
			var subEntry = CreateInterestingLexEntry(Cache, "Subcitation", "subgloss");
			CreateComplexForm(Cache, testEntry.SensesOS[0], subEntry, true);
			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(testEntry, mainEntryNode, DefaultDecorator, settings).ToString();
			const string senseContent = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']";
			const string senseNumberOne = senseContent + "/span[@class='sense' and preceding-sibling::span[@class='sensenumber' and text()='1']]//span[@lang='en' and text()='gloss']";
			const string subentrySenseContent = senseContent + "/span[@class='sense']/span[@class='subentries']/span[@class='subentry']/span[@class='senses']/span[@class='sensecontent']";
			const string subentrySenseNumberOne = subentrySenseContent + "/span[@class='sense' and preceding-sibling::span[@class='sensenumber' and text()='1']]//span[@lang='en' and text()='subgloss']";
			const string subentrySenseNumberOneOne = subentrySenseContent + "/span[@class='sense' and preceding-sibling::span[@class='sensenumber' and text()='1.1']]//span[@lang='en']";
			//This assert is dependent on the specific entry data created in CreateInterestingLexEntry
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(senseNumberOne, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(subentrySenseNumberOne, 1);
			AssertThatXmlIn.String(result).HasNoMatchForXpath(subentrySenseNumberOneOne);
		}

		/// <summary>
		/// If the numbering style for Senses says to number it, and
		/// if this is the only sense, and
		/// if the box for "Number even a single sense" is ticked,
		/// then number the sense.
		/// (See LT-17906.)
		/// </summary>
		[Test]
		public void GenerateContentForEntry_SingleSenseGetsNumberWithNumberEvenOneSenseOption()
		{
			var wsOpts = GetWsOptionsForLanguages(new[] { "en" });
			var glossNode = new ConfigurableDictionaryNode { FieldDescription = "Gloss", DictionaryNodeOptions = wsOpts };
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "Senses",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions { NumberEvenASingleSense = true, NumberingStyle = "%d" },
				Children = new List<ConfigurableDictionaryNode> { glossNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { sensesNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var testEntry = CreateInterestingLexEntry(Cache);

			// SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(testEntry, mainEntryNode, DefaultDecorator, DefaultSettings).ToString();
			const string senseNumberOne = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense' and preceding-sibling::span[@class='sensenumber' and text()='1']]//span[@lang='en' and text()='gloss']";
			// This assert is dependent on the specific entry data created in CreateInterestingLexEntry
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(senseNumberOne, 1);

			// Piggy-back a test for ShouldThisSenseBeNumbered
			Assert.That(ConfiguredLcmGenerator.ShouldThisSenseBeNumbered(testEntry.AllSenses.First(), sensesNode, testEntry.SensesOS), Is.True);
		}

		[Test]
		public void GenerateContentForEntry_SenseContentWithGuid()
		{
			var wsOpts = GetWsOptionsForLanguages(new[] { "en" });
			var glossNode = new ConfigurableDictionaryNode { FieldDescription = "Gloss", DictionaryNodeOptions = wsOpts };
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "Senses",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions(),
				Children = new List<ConfigurableDictionaryNode> { glossNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { sensesNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var testEntry = CreateInterestingLexEntry(Cache);

			// SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(testEntry, mainEntryNode, DefaultDecorator, DefaultSettings).ToString();
			const string senseEntryGuid = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense' and @entryguid]";
			// This assert is dependent on the specific entry data created in CreateInterestingLexEntry
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(senseEntryGuid, 1);
			string senseEntryGuidstatsWithG = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense' and @entryguid='g" + testEntry.Guid + "']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(senseEntryGuidstatsWithG, 1);
		}

		[Test]
		public void GenerateContentForEntry_ExampleAndTranslationAreGenerated()
		{
			var translationNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Translation",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" })
			};
			var translationsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "TranslationsOC",
				CSSClassNameOverride = "translationcontents",
				Children = new List<ConfigurableDictionaryNode> { translationNode }
			};
			var exampleNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Example",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};
			var examplesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ExamplesOS",
				CSSClassNameOverride = "examplescontents",
				Children = new List<ConfigurableDictionaryNode> { exampleNode, translationsNode }
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				DictionaryNodeOptions = GetSenseNodeOptions(),
				Children = new List<ConfigurableDictionaryNode> { examplesNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { sensesNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);

			const string example = "Example Sentence On Entry";
			const string translation = "Translation of the Example";
			var testEntry = CreateInterestingLexEntry(Cache);
			AddExampleToSense(testEntry.SensesOS[0], example, Cache, m_wsFr, m_wsEn, translation);

			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(testEntry, mainEntryNode, null, DefaultSettings).ToString();
			const string xpathThruExample = xpathThruSense + "/span[@class='examplescontents']/span[@class='examplescontent']";
			var oneSenseWithExample = string.Format(xpathThruExample + "/span[@class='example']/span[@lang='fr' and text()='{0}']", example);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(oneSenseWithExample, 1);
			var oneExampleSentenceTranslation = string.Format(xpathThruExample +
				"/span[@class='translationcontents']/span[@class='translationcontent']/span[@class='translation']/span[@lang='en' and text()='{0}']", translation);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(oneExampleSentenceTranslation, 1);
		}

		[Test]
		public void GenerateContentForEntry_ExampleSentenceAndTranslationAreGenerated()
		{
			var translationNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Translation",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" })
			};
			var translationsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "TranslationsOC",
				CSSClassNameOverride = "translations",
				Children = new List<ConfigurableDictionaryNode> { translationNode }
			};
			var exampleNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Example",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};
			var examplesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ExampleSentences",
				Children = new List<ConfigurableDictionaryNode> { exampleNode, translationsNode }
			};
			var otherRcfsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ComplexFormsNotSubentries",
				Children = new List<ConfigurableDictionaryNode> { examplesNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { otherRcfsNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);

			const string example = "Example Sentence On Variant Form";
			const string translation = "Translation of the Sentence";
			var mainEntry = CreateInterestingLexEntry(Cache);
			var minorEntry = CreateInterestingLexEntry(Cache);
			CreateComplexForm(Cache, mainEntry, minorEntry, false);
			AddExampleToSense(minorEntry.SensesOS[0], example, Cache, m_wsFr, m_wsEn, translation);

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(mainEntry, mainEntryNode, null, settings).ToString();
			const string xpathThruExampleSentence = "/div[@class='lexentry']/span[@class='complexformsnotsubentries']/span[@class='complexformsnotsubentry']/span[@class='examplesentences']/span[@class='examplesentence']";
			var oneSenseWithExample = string.Format(xpathThruExampleSentence + "//span[@lang='fr' and text()='{0}']", example);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(oneSenseWithExample, 1);
			var oneExampleSentenceTranslation = string.Format(
				xpathThruExampleSentence + "/span[@class='translations']/span[@class='translation']//span[@lang='en' and text()='{0}']", translation);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(oneExampleSentenceTranslation, 1);
		}

		[Test]
		public void GenerateContentForEntry_LineSeperatorUnicodeCharBecomesBrElement()
		{
			var translationNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Translation",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" })
			};
			var translationsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "TranslationsOC",
				CSSClassNameOverride = "translations",
				Children = new List<ConfigurableDictionaryNode> { translationNode }
			};
			var exampleNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Example",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};
			var examplesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ExampleSentences",
				Children = new List<ConfigurableDictionaryNode> { exampleNode, translationsNode }
			};
			var otherRcfsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ComplexFormsNotSubentries",
				Children = new List<ConfigurableDictionaryNode> { examplesNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { otherRcfsNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);

			const string example = "Example\u2028Sentence On Variant Form";
			const string translation = "Translation\u2028of the Sentence";
			var mainEntry = CreateInterestingLexEntry(Cache);
			var minorEntry = CreateInterestingLexEntry(Cache);
			CreateComplexForm(Cache, mainEntry, minorEntry, false);
			AddExampleToSense(minorEntry.SensesOS[0], example, Cache, m_wsFr, m_wsEn, translation);

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(mainEntry, mainEntryNode, null, settings).ToString();
			const string xpathThruExampleSentence = "/div[@class='lexentry']/span[@class='complexformsnotsubentries']/span[@class='complexformsnotsubentry']/span[@class='examplesentences']/span[@class='examplesentence']";
			var oneSenseWithExample = string.Format(xpathThruExampleSentence + "//span[@lang='fr']//br");
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(oneSenseWithExample, 1);
			var oneExampleSentenceTranslation = string.Format(
				xpathThruExampleSentence + "/span[@class='translations']/span[@class='translation']//span[@lang='en']//br");
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(oneExampleSentenceTranslation, 1);
		}

		[Test]
		public void GenerateContentForEntry_ExtendedNoteChildrenAreGenerated()
		{
			var translationNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Translation",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" })
			};
			var translationsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "TranslationsOC",
				CSSClassNameOverride = "translations",
				Children = new List<ConfigurableDictionaryNode> { translationNode }
			};
			var exampleNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Example",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};
			var examplesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ExamplesOS",
				CSSClassNameOverride = "examples",
				Children = new List<ConfigurableDictionaryNode> { exampleNode, translationsNode }
			};
			var discussionTypeNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Discussion",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};
			var noteTypeNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ExtendedNoteTypeRA",
				SubField = "Name",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" })
			};
			var extendedNoteNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ExtendedNoteOS",
				CSSClassNameOverride = "extendednotecontents",
				Children = new List<ConfigurableDictionaryNode> { noteTypeNode, discussionTypeNode, examplesNode }
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				DictionaryNodeOptions = GetSenseNodeOptions(),
				Children = new List<ConfigurableDictionaryNode> { extendedNoteNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { sensesNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);

			const string noteType = "Cultural";
			const string discussion = "Discussion";
			const string example = "Example Sentence On Entry";
			const string translation = "Translation of the Example";
			var testEntry = CreateInterestingLexEntry(Cache);

			AddExampleToExtendedNote(testEntry.SensesOS[0], noteType, discussion, example, translation);

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(testEntry, mainEntryNode, null, settings).ToString();

			const string extendedNote = xpathThruSense + "/span[@class='extendednotecontents']/span[@class='extendednotecontent']";
			var xpathThruNoteType = string.Format(extendedNote + "/span[@class='extendednotetypera_name']/span[@lang='en' and text()='{0}']", noteType);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(xpathThruNoteType, 1);

			var xpathThruDiscussion = string.Format(extendedNote + "/span[@class='discussion']/span[@lang='fr' and text()='{0}']", discussion);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(xpathThruDiscussion, 1);

			const string xpathThruExample = extendedNote + "/span[@class='examples']/span[@class='example']";
			var oneSenseWithExample = string.Format(xpathThruExample + "/span[@class='example']/span[@lang='fr' and text()='{0}']", example);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(oneSenseWithExample, 1);
			var oneExampleSentenceTranslation = string.Format(
				xpathThruExample + "/span[@class='translations']/span[@class='translation']/span[@class='translation']/span[@lang='en' and text()='{0}']", translation);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(oneExampleSentenceTranslation, 1);
		}

		[Test]
		public void GenerateContentForEntry_ExtendedNoteNoteTypeEmptyAreGenerated()
		{
			var translationNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Translation",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" })
			};
			var translationsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "TranslationsOC",
				CSSClassNameOverride = "translations",
				Children = new List<ConfigurableDictionaryNode> { translationNode }
			};
			var exampleNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Example",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};
			var examplesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ExamplesOS",
				CSSClassNameOverride = "examples",
				Children = new List<ConfigurableDictionaryNode> { exampleNode, translationsNode }
			};
			var discussionTypeNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Discussion",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};
			var noteTypeNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ExtendedNoteTypeRA",
				SubField = "Name",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" })
			};
			var extendedNoteNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ExtendedNoteOS",
				CSSClassNameOverride = "extendednotecontents",
				Children = new List<ConfigurableDictionaryNode> { noteTypeNode, discussionTypeNode, examplesNode }
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				Children = new List<ConfigurableDictionaryNode> { extendedNoteNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { sensesNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);

			const string noteType = "";
			const string discussion = "Discussion";
			const string example = "Example Sentence On Entry";
			const string translation = "Translation of the Example";
			var testEntry = CreateInterestingLexEntry(Cache);

			AddExampleToExtendedNote(testEntry.SensesOS[0], noteType, discussion, example, translation);

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(testEntry, mainEntryNode, null, settings).ToString();

			const string extendedNote = xpathThruSense + "/span[@class='extendednotecontents']/span[@class='extendednotecontent']";
			var xpathThruNoteType = string.Format(extendedNote + "/span[@class='extendednotetypera_name']/span[@lang='en' and text()='{0}']", noteType);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(xpathThruNoteType, 0);
		}

		private void AddExampleToExtendedNote(ILexSense sense, string noteType, string discussion, string examples, string translation = null)
		{
			var extendedNoteFact = Cache.ServiceLocator.GetInstance<ILexExtendedNoteFactory>();
			var extendedNote = extendedNoteFact.Create();
			sense.ExtendedNoteOS.Add(extendedNote);

			var extendedNoteType = CreateExtendedNoteType(noteType);
			extendedNote.ExtendedNoteTypeRA = extendedNoteType;
			extendedNote.Discussion.set_String(m_wsFr, discussion);

			var exampleFact = Cache.ServiceLocator.GetInstance<ILexExampleSentenceFactory>();
			var example = exampleFact.Create();
			extendedNote.ExamplesOS.Add(example);
			example.Example.set_String(m_wsFr, TsStringUtils.MakeString(examples, m_wsFr));
			if (translation != null)
			{
				var type = Cache.ServiceLocator.GetInstance<ICmPossibilityRepository>().GetObject(CmPossibilityTags.kguidTranFreeTranslation);
				var cmTranslation = Cache.ServiceLocator.GetInstance<ICmTranslationFactory>().Create(example, type);
				cmTranslation.Translation.set_String(m_wsEn, TsStringUtils.MakeString(translation, m_wsEn));
				example.TranslationsOC.Add(cmTranslation);
			}
		}

		private ICmPossibility CreateExtendedNoteType(string name)
		{
			if (Cache.LangProject.LexDbOA.ExtendedNoteTypesOA == null)
				Cache.LangProject.LexDbOA.ExtendedNoteTypesOA = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
			var item = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
			Cache.LangProject.LexDbOA.ExtendedNoteTypesOA.PossibilitiesOS.Add(item);
			item.Name.set_String(m_wsEn, name);
			Cache.LangProject.LexDbOA.ExtendedNoteTypesOA.PossibilitiesOS.Add(item);
			return item;
		}

		[Test]
		public void GenerateContentForEntry_EnvironmentsAndAllomorphsAreGenerated()
		{
			var stringRepNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "StringRepresentation",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" })
			};
			var environmentsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "AllomorphEnvironments",
				Children = new List<ConfigurableDictionaryNode> { stringRepNode }
			};
			var formNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Form",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};
			var allomorphsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "AlternateFormsOS",
				Children = new List<ConfigurableDictionaryNode> { formNode, environmentsNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { allomorphsNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);

			var mainEntry = CreateInterestingLexEntry(Cache);
			AddAllomorphToEntry(mainEntry);

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(mainEntry, mainEntryNode, null, settings).ToString();
			const string xPathThruAllomorph = "/div[@class='lexentry']/span[@class='alternateformsos']/span[@class='alternateformso']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
				xPathThruAllomorph + "/span[@class='form']/span[@lang='fr' and text()='Allomorph']", 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(xPathThruAllomorph +
				"/span[@class='allomorphenvironments']/span[@class='allomorphenvironment']/span[@class='stringrepresentation']/span[@lang='en' and text()='phoneyEnv']", 1);
		}

		[Test]
		public void GenerateContentForEntry_ReferencedComplexFormsIncludesSubentriesAndOtherReferencedComplexForms()
		{
			var complexFormNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwningEntry",
				SubField = "MLHeadWord",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};
			var rcfsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VisibleComplexFormBackRefs",
				Children = new List<ConfigurableDictionaryNode> { complexFormNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { rcfsNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);

			var mainEntry = CreateInterestingLexEntry(Cache);
			var otherReferencedComplexForm = CreateInterestingLexEntry(Cache);
			var subentry = CreateInterestingLexEntry(Cache);
			CreateComplexForm(Cache, mainEntry, subentry, true);
			CreateComplexForm(Cache, mainEntry, otherReferencedComplexForm, false);

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(mainEntry, mainEntryNode, null, settings).ToString();
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
				"/div[@class='lexentry']/span[@class='visiblecomplexformbackrefs']/span[@class='visiblecomplexformbackref']//span[@lang='fr']/span[@lang='fr']", 4);
		}

		[Test]
		public void GenerateContentForEntry_GeneratesLinksForReferencedForms()
		{
			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "HeadWord",
				CSSClassNameOverride = "headword",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};
			var refNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ConfigReferencedEntries",
				Children = new List<ConfigurableDictionaryNode> { headwordNode },
				CSSClassNameOverride = "referencedentries"
			};
			var variantsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VisibleVariantEntryRefs",
				Children = new List<ConfigurableDictionaryNode> { refNode }
			};
			var complexFormNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwningEntry",
				SubField = "MLHeadWord",
				CSSClassNameOverride = "headword",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};
			var rcfsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VisibleComplexFormBackRefs",
				Children = new List<ConfigurableDictionaryNode> { complexFormNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { variantsNode, rcfsNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);

			var mainEntry = CreateInterestingLexEntry(Cache);
			var variantForm = CreateInterestingLexEntry(Cache);
			var subentry = CreateInterestingLexEntry(Cache);
			CreateVariantForm(Cache, variantForm, mainEntry);
			CreateComplexForm(Cache, mainEntry, subentry, true);

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(mainEntry, mainEntryNode, null, settings).ToString();
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
				"//span[@class='visiblevariantentryrefs']/span[@class='visiblevariantentryref']/span[@class='referencedentries']/span[@class='referencedentry']/span[@class='headword']/span[@lang='fr']/span[@lang='fr']/a[@href]", 2);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
				"/div[@class='lexentry']/span[@class='visiblecomplexformbackrefs']/span[@class='visiblecomplexformbackref']/span[@class='headword']/span[@lang='fr']/span[@lang='fr']/a[@href]", 2);
		}

		[Test]
		public void GenerateContentForEntry_GeneratesLinksForPrimaryEntryReferences()
		{
			var mainEntry = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(mainEntry, "Test", m_wsFr);
			var referencedEntry = CreateInterestingLexEntry(Cache);
			var otherMainEntry = CreateInterestingLexEntry(Cache);
			const string refTypeName = "TestRefType";
			CreateComplexForm(Cache, mainEntry, referencedEntry, true);
			CreateLexicalReference(Cache, otherMainEntry, referencedEntry, refTypeName);

			var refType = Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.First(poss => poss.Name.BestAnalysisAlternative.Text == refTypeName);
			Assert.That(refType, Is.Not.Null);

			var RevNameNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ReverseName",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};

			var RevAbbrNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ReverseAbbr",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};

			var typeNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "EntryTypes",
				CSSClassNameOverride = "entrytypes",
				Children = new List<ConfigurableDictionaryNode> { RevNameNode, RevAbbrNode }
			};

			var refHeadwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "HeadWord",
				CSSClassNameOverride = "headword",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" })
			};

			var glossOrSummaryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "GlossOrSummary",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};

			var primaryEntrysNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ConfigReferencedEntries",
				CSSClassNameOverride = "referencedentries",
				Children = new List<ConfigurableDictionaryNode> { refHeadwordNode, glossOrSummaryNode }
			};

			var commentNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Summary",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};

			var primaryEntryRefNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Item",
				SubField = "EntryRefsOS",
				CSSClassNameOverride = "primaryentryrefs",
				Children = new List<ConfigurableDictionaryNode> { typeNode, primaryEntrysNode, commentNode }
			};

			var targetsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ConfigTargets",
				Children = new List<ConfigurableDictionaryNode> { primaryEntryRefNode }
			};
			var crossReferencesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MinimalLexReferences",
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
				Children = new List<ConfigurableDictionaryNode> { crossReferencesNode }
			};

			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(otherMainEntry, mainEntryNode, null, settings).ToString();
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
				"//span[@class='minimallexreferences']/span[@class='minimallexreference']/span[@class='configtargets']/span[@class='configtarget']/span[@class='primaryentryrefs']/span[@class='primaryentryref']/span[@class='referencedentries']/span[@class='referencedentry']/span[@class='headword']/span[@lang='fr']/a[@href]", 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
				"//span[@class='minimallexreferences']/span[@class='minimallexreference']/span[@class='configtargets']/span[@class='configtarget']/span[@class='primaryentryrefs']/span[@class='primaryentryref']/span[@class='referencedentries']/span[@class='referencedentry']/span[@class='headword']/span[@lang='fr']/a[@href][contains(text(), 'Test')]", 1);
		}

		[Test]
		public void GenerateContentForEntry_GeneratesLinksForCrossReferences()
		{
			var mainEntry = CreateInterestingLexEntry(Cache);
			var referencedEntry = CreateInterestingLexEntry(Cache);
			const string refTypeName = "TestRefType";
			CreateLexicalReference(Cache, mainEntry, referencedEntry, refTypeName);
			var refType = Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.First(poss => poss.Name.BestAnalysisAlternative.Text == refTypeName);
			Assert.That(refType, Is.Not.Null);

			var formNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "HeadWord",
				CSSClassNameOverride = "headword",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};
			var targetsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ConfigTargets",
				Children = new List<ConfigurableDictionaryNode> { formNode }
			};
			var crossReferencesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MinimalLexReferences",
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
				Children = new List<ConfigurableDictionaryNode> { crossReferencesNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(mainEntry, mainEntryNode, null, settings).ToString();
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
				"//span[@class='minimallexreferences']/span[@class='minimallexreference']/span[@class='configtargets']/span[@class='configtarget']/span[@class='headword']/span[@lang='fr']/span[@lang='fr']/a[@href]", 4);
		}

		[Test]
		public void GenerateContentForEntry_GeneratesCssForConfigTargetsInLexReferences()
		{
			var mainEntry = CreateInterestingLexEntry(Cache);
			var referencedEntry = CreateInterestingLexEntry(Cache);
			const string refTypeName = "TestRefType";
			CreateLexicalReference(Cache, mainEntry, referencedEntry, refTypeName);
			var refType = Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.First(poss => poss.Name.BestAnalysisAlternative.Text == refTypeName);
			Assert.That(refType, Is.Not.Null);

			var formNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "HeadWord",
				CSSClassNameOverride = "headword",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};
			var targetsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ConfigTargets",
				Children = new List<ConfigurableDictionaryNode> { formNode },
				Before = " ",
				Between = ";",
				After = "!"
			};
			var crossReferencesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MinimalLexReferences",
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
				Children = new List<ConfigurableDictionaryNode> { crossReferencesNode }
			};

			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);

			ConfiguredLcmGenerator.GenerateContentForEntry(mainEntry, mainEntryNode, null, settings);

			var result = ((CssGenerator)settings.StylesGenerator).GetStylesString();

			var pattern = @".configtargets>\s*\.configtarget\s*\+\s*\.configtarget:before\s*\{\s*content:\s*';';\s*\}\s*\.configtargets:before\s*\{\s*content:\s*' ';\s*\}\s*\.configtargets:after\s*\{\s*content:\s*'!';\s*\}";

			CssGeneratorTests.VerifyRegex(result, pattern, "CSS verification failed.");
		}


		[Test]
		public void GenerateContentForEntry_GeneratesLinksForCrossReferencesWithReferencedNodes()
		{
			var mainEntry = CreateInterestingLexEntry(Cache);
			var referencedEntry = CreateInterestingLexEntry(Cache);
			const string refTypeName = "TestRefType";
			CreateLexicalReference(Cache, mainEntry, referencedEntry, refTypeName);
			var refType = Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.First(poss => poss.Name.BestAnalysisAlternative.Text == refTypeName);
			Assert.That(refType, Is.Not.Null);

			var formNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "HeadWord",
				CSSClassNameOverride = "headword",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};
			var targetsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ConfigTargets",
				Children = new List<ConfigurableDictionaryNode> { formNode }
			};
			var refdCrossRefsNode = new ConfigurableDictionaryNode
			{
				Label = "CrossRefRef",
				CSSClassNameOverride = "refdrefs",
				FieldDescription = "MinimalLexReferences",
				Children = new List<ConfigurableDictionaryNode> { targetsNode }
			};
			var crossReferencesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MinimalLexReferences",
				ReferenceItem = "CrossRefRef",
				DictionaryNodeOptions = new DictionaryNodeListOptions
				{
					ListId = DictionaryNodeListOptions.ListIds.Entry,
					Options = DictionaryDetailsControllerTests.ListOfEnabledDNOsFromStrings(new[] { refType.Guid.ToString() })
				}
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { crossReferencesNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(DictionaryConfigurationModelTests.CreateSimpleSharingModel(mainEntryNode, refdCrossRefsNode));

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(mainEntry, mainEntryNode, null, settings).ToString();
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
				"//span[@class='configtargets']/span[@class='configtarget']/span[@class='headword']/span[@lang='fr']//a[@href]", 4);
		}

		[Test]
		public void GenerateContentForEntry_GeneratesCrossReferencesOnUnCheckConfigTargets()
		{
			var mainEntry = CreateInterestingLexEntry(Cache);
			var referencedEntry = CreateInterestingLexEntry(Cache);
			const string refTypeName = "TestRefType";
			CreateLexicalReference(Cache, mainEntry, referencedEntry, refTypeName);
			var refType = Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.First(poss => poss.Name.BestAnalysisAlternative.Text == refTypeName);
			Assert.That(refType, Is.Not.Null);

			var formNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLHeadWord",
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
			var headword = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLHeadWord",
				CSSClassNameOverride = "headword",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" }),
				IsEnabled = true
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { headword, crossReferencesNode }
			};
			DictionaryConfigurationModel.SpecifyParentsAndReferences(new List<ConfigurableDictionaryNode> { mainEntryNode });

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT-
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(mainEntry, mainEntryNode, null, settings).ToString();
			AssertThatXmlIn.String(result).HasNoMatchForXpath(
				"//span[@class='minimallexreferences']/span[@class='minimallexreference']/span[@class='configtargets']");
		}

		[Test]
		public void GenerateContentForEntry_GeneratesForwardNameForSymmetricCrossReferences()
		{
			var mainEntry = CreateInterestingLexEntry(Cache);
			var referencedEntry = CreateInterestingLexEntry(Cache);
			const string refTypeName = "TestRefType";
			CreateLexicalReference(Cache, mainEntry, referencedEntry, refTypeName);
			var refType = Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.First(poss => poss.Name.BestAnalysisAlternative.Text == refTypeName);
			Assert.That(refType, Is.Not.Null);

			var nameNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwnerType",
				SubField = "Name",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" })
			};
			var crossReferencesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MinimalLexReferences",
				DictionaryNodeOptions = new DictionaryNodeListOptions
				{
					ListId = DictionaryNodeListOptions.ListIds.Entry,
					Options = DictionaryDetailsControllerTests.ListOfEnabledDNOsFromStrings(new[] { refType.Guid.ToString() })
				},
				Children = new List<ConfigurableDictionaryNode> { nameNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { crossReferencesNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(referencedEntry, mainEntryNode, null, settings).ToString();
			var fwdNameXpath = string.Format(
				"//span[@class='minimallexreferences']/span[@class='minimallexreference']/span[@class='ownertype_name']/span[@lang='en' and text()='{0}']", refTypeName);
			const string anyNameXpath =
				"//span[@class='minimallexreferences']/span[@class='minimallexreference']/span[@class='ownertype_name']/span[@lang='en']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(fwdNameXpath, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(anyNameXpath, 1); // ensure there are no spurious names
		}

		[Test]
		public void GenerateContentForEntry_GeneratesForwardNameForForwardCrossReferences()
		{
			var mainEntry = CreateInterestingLexEntry(Cache);
			var referencedEntry = CreateInterestingLexEntry(Cache);
			const string refTypeName = "TestRefType";
			const string refTypeRevName = "epyTfeRtseT";
			CreateLexicalReference(Cache, mainEntry, referencedEntry, refTypeName, refTypeRevName);
			var refType = Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.First(poss => poss.Name.BestAnalysisAlternative.Text == refTypeName);
			Assert.That(refType, Is.Not.Null);

			var nameNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwnerType",
				SubField = "Name",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" })
			};
			var crossReferencesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MinimalLexReferences",
				DictionaryNodeOptions = new DictionaryNodeListOptions
				{
					ListId = DictionaryNodeListOptions.ListIds.Entry,
					Options = DictionaryDetailsControllerTests.ListOfEnabledDNOsFromStrings(new[] { refType.Guid + ":f" })
				},
				Children = new List<ConfigurableDictionaryNode> { nameNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { crossReferencesNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(mainEntry, mainEntryNode, null, settings).ToString();
			var fwdNameXpath = string.Format(
				"//span[@class='minimallexreferences']/span[@class='minimallexreference']/span[@class='ownertype_name']/span[@lang='en' and text()='{0}']", refTypeName);
			var revNameXpath = string.Format(
				"//span[@class='minimallexreferences']/span[@class='minimallexreference']/span[@class='ownertype_name']/span[@lang='en' and text()='{0}']", refTypeRevName);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(fwdNameXpath, 1);
			AssertThatXmlIn.String(result).HasNoMatchForXpath(revNameXpath);
		}

		[Test]
		public void GenerateContentForEntry_GeneratesReverseNameForReverseCrossReferences()
		{
			var mainEntry = CreateInterestingLexEntry(Cache);
			var referencedEntry = CreateInterestingLexEntry(Cache);
			const string refTypeName = "TestRefType";
			const string refTypeRevName = "sURsyoT";
			CreateLexicalReference(Cache, mainEntry, referencedEntry, refTypeName, refTypeRevName);
			var refType = Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.First(poss => poss.Name.BestAnalysisAlternative.Text == refTypeName);
			Assert.That(refType, Is.Not.Null);

			var nameNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwnerType",
				SubField = "Name",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" })
			};
			var crossReferencesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MinimalLexReferences",
				DictionaryNodeOptions = new DictionaryNodeListOptions
				{
					ListId = DictionaryNodeListOptions.ListIds.Entry,
					Options = DictionaryDetailsControllerTests.ListOfEnabledDNOsFromStrings(new[] { refType.Guid + ":r" })
				},
				Children = new List<ConfigurableDictionaryNode> { nameNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { crossReferencesNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(referencedEntry, mainEntryNode, null, settings).ToString();
			var fwdNameXpath = string.Format(
				"//span[@class='minimallexreferences']/span[@class='minimallexreference']/span[@class='ownertype_name']/span[@lang='en' and text()='{0}']", refTypeName);
			var revNameXpath = string.Format(
				"//span[@class='minimallexreferences']/span[@class='minimallexreference']/span[@class='ownertype_name']/span[@lang='en' and text()='{0}']", refTypeRevName);
			AssertThatXmlIn.String(result).HasNoMatchForXpath(fwdNameXpath);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(revNameXpath, 1);
		}

		[Test]
		public void GenerateContentForEntry_GeneratesForwardNameForForwardLexicalRelations()
		{
			var mainEntry = CreateInterestingLexEntry(Cache);
			var referencedEntry = CreateInterestingLexEntry(Cache);
			const string refTypeName = "TestRefType";
			const string refTypeRevName = "sURsyoT";
			CreateLexicalReference(Cache, mainEntry.SensesOS.First(), referencedEntry, refTypeName, refTypeRevName);
			var refType = Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.First(poss => poss.Name.BestAnalysisAlternative.Text == refTypeName);
			Assert.That(refType, Is.Not.Null);

			var nameNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwnerType",
				SubField = "Name",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" })
			};
			var referencesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexSenseReferences",
				DictionaryNodeOptions = new DictionaryNodeListOptions
				{
					ListId = DictionaryNodeListOptions.ListIds.Sense,
					Options = DictionaryDetailsControllerTests.ListOfEnabledDNOsFromStrings(new[] { refType.Guid + ":f" })
				},
				Children = new List<ConfigurableDictionaryNode> { nameNode }
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				Children = new List<ConfigurableDictionaryNode> { referencesNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { sensesNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(mainEntry, mainEntryNode, null, settings).ToString();
			var fwdNameXpath = string.Format(
				"//span[@class='lexsensereferences']/span[@class='lexsensereference']/span[@class='ownertype_name']/span[@lang='en' and text()='{0}']", refTypeName);
			var revNameXpath = string.Format(
				"//span[@class='lexsensereferences']/span[@class='lexsensereference']/span[@class='ownertype_name']/span[@lang='en' and text()='{0}']", refTypeRevName);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(fwdNameXpath, 1);
			AssertThatXmlIn.String(result).HasNoMatchForXpath(revNameXpath);
		}

		[Test]
		public void GenerateContentForEntry_GeneratesLexicalRelationsLabelWithNoRepetition()
		{
			var mainEntry = CreateInterestingLexEntry(Cache);
			var referencedEntry1 = CreateInterestingLexEntry(Cache);
			var referencedEntry2 = CreateInterestingLexEntry(Cache);
			const string refTypeName = "TestRefType";
			const string refTypeRevName = "sURsyoT";
			CreateLexicalReference(Cache, mainEntry.SensesOS.First(), referencedEntry1, refTypeName, refTypeRevName);
			CreateLexicalReference(Cache, mainEntry.SensesOS.First(), referencedEntry2, refTypeName, refTypeRevName);
			var refType1 = Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.First(poss => poss.Name.BestAnalysisAlternative.Text == refTypeName);
			Assert.That(refType1, Is.Not.Null);
			var nameNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwnerType",
				SubField = "Name",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" })
			};
			var referencesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexSenseReferences",
				DictionaryNodeOptions = new DictionaryNodeListOptions
				{
					ListId = DictionaryNodeListOptions.ListIds.Sense,
					Options = DictionaryDetailsControllerTests.ListOfEnabledDNOsFromStrings(new[] { refType1.Guid + ":f" })
				},
				Children = new List<ConfigurableDictionaryNode> { nameNode }
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				Children = new List<ConfigurableDictionaryNode> { referencesNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { sensesNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(mainEntry, mainEntryNode, null, settings).ToString();
			var fwdNameXpath = string.Format(
				"//span[@class='lexsensereferences']/span[@class='lexsensereference']/span[@class='ownertype_name']/span[@lang='en' and text()='{0}']", refTypeName);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(fwdNameXpath, 1);
		}

		[Test]
		public void GenerateContentForEntry_GeneratesReverseNameForReverseLexicalRelations()
		{
			var mainEntry = CreateInterestingLexEntry(Cache);
			var referencedEntry = CreateInterestingLexEntry(Cache);
			const string refTypeName = "TestRefType";
			const string refTypeRevName = "sURsyoT";
			CreateLexicalReference(Cache, mainEntry, referencedEntry.SensesOS.First(), refTypeName, refTypeRevName);
			var refType = Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.First(poss => poss.Name.BestAnalysisAlternative.Text == refTypeName);
			Assert.That(refType, Is.Not.Null);

			var nameNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwnerType",
				SubField = "Name",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" })
			};
			var referencesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexSenseReferences",
				DictionaryNodeOptions = new DictionaryNodeListOptions
				{
					ListId = DictionaryNodeListOptions.ListIds.Sense,
					Options = DictionaryDetailsControllerTests.ListOfEnabledDNOsFromStrings(new[] { refType.Guid + ":r" })
				},
				Children = new List<ConfigurableDictionaryNode> { nameNode }
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				Children = new List<ConfigurableDictionaryNode> { referencesNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { sensesNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(referencedEntry, mainEntryNode, null, settings).ToString();
			var fwdNameXpath = string.Format(
				"//span[@class='lexsensereferences']/span[@class='lexsensereference']/span[@class='ownertype_name']/span[@lang='en' and text()='{0}']", refTypeName);
			var revNameXpath = string.Format(
				"//span[@class='lexsensereferences']/span[@class='lexsensereference']/span[@class='ownertype_name']/span[@lang='en' and text()='{0}']", refTypeRevName);
			AssertThatXmlIn.String(result).HasNoMatchForXpath(fwdNameXpath);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(revNameXpath, 1);
		}

		[Test]
		public void GenerateContentForEntry_LexicalRelationsSortbyNodeOptionsOrder()
		{
			var mainEntry = CreateInterestingLexEntry(Cache);
			var compareReferencedEntry = CreateInterestingLexEntry(Cache);
			var etymologyReferencedEntry = CreateInterestingLexEntry(Cache);
			const string comRefTypeName = "Compare";
			const string comRefTypeRevName = "cp";
			const string etyRefTypeName = "Etymology";
			const string etyRefTypeRevName = "ety";
			CreateLexicalReference(Cache, mainEntry, compareReferencedEntry, comRefTypeName, comRefTypeRevName);
			CreateLexicalReference(Cache, mainEntry, etymologyReferencedEntry, etyRefTypeName, etyRefTypeRevName);
			var comRefType =
				Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.First(
					poss => poss.Name.BestAnalysisAlternative.Text == comRefTypeName);
			var etyRefType =
				Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.First(
					poss => poss.Name.BestAnalysisAlternative.Text == etyRefTypeName);
			Assert.That(comRefType, Is.Not.Null);
			Assert.That(etyRefType, Is.Not.Null);

			var nameNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwnerType",
				SubField = "Name",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" })
			};
			var crossReferencesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MinimalLexReferences",
				DictionaryNodeOptions = new DictionaryNodeListOptions
				{
					ListId = DictionaryNodeListOptions.ListIds.Entry,
					Options =
						DictionaryDetailsControllerTests.ListOfEnabledDNOsFromStrings(new[] { etyRefType.Guid + ":f", comRefType.Guid + ":f" })
				},
				Children = new List<ConfigurableDictionaryNode> { nameNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { crossReferencesNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(mainEntry, mainEntryNode, null, settings).ToString();
			const string NameXpath = "//span[@class='minimallexreferences']/span[@class='minimallexreference' and position()='{0}']/span[@class='ownertype_name']/span[@lang='en' and text()='{1}']";
			var fwdNameFirstXpath = string.Format(NameXpath, "1", etyRefTypeName);
			var fwdNameSecondXpath = string.Format(NameXpath, "2", comRefTypeName);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(fwdNameFirstXpath, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(fwdNameSecondXpath, 1);
			crossReferencesNode.DictionaryNodeOptions = new DictionaryNodeListOptions
			{
				ListId = DictionaryNodeListOptions.ListIds.Entry,
				Options =
					DictionaryDetailsControllerTests.ListOfEnabledDNOsFromStrings(new[] { comRefType.Guid + ":f", etyRefType.Guid + ":f" })
			};
			var resultAfterChange = ConfiguredLcmGenerator.GenerateContentForEntry(mainEntry, mainEntryNode, null, settings).ToString();
			var fwdNameChangedFirstXpath = string.Format(NameXpath, "1", comRefTypeName);
			var fwdNameChangedSecondXpath = string.Format(NameXpath, "2", etyRefTypeName);
			AssertThatXmlIn.String(resultAfterChange).HasSpecifiedNumberOfMatchesForXpath(fwdNameChangedFirstXpath, 1);
			AssertThatXmlIn.String(resultAfterChange).HasSpecifiedNumberOfMatchesForXpath(fwdNameChangedSecondXpath, 1);
		}

		[Test]
		public void GenerateContentForEntry_GeneratesAsymmetricRelationsProperly()
		{
			const string firstWord = "corps";
			var bodyEntry = CreateInterestingLexEntry(Cache, firstWord, "body");
			const string secondWord = "bras";
			var armEntry = CreateInterestingLexEntry(Cache, secondWord, "arm");
			const string thirdWord = "jambe";
			var legEntry = CreateInterestingLexEntry(Cache, thirdWord, "leg");
			const string refTypeName = "Part";
			const string refTypeRevName = "Whole";
			CreateLexicalReference(Cache, bodyEntry, armEntry.SensesOS.First(), legEntry.SensesOS.First(), refTypeName, refTypeRevName);
			var refType = Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.First(poss => poss.Name.BestAnalysisAlternative.Text == refTypeName);
			Assert.That(refType, Is.Not.Null);

			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "HeadWord",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};
			var glossNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Gloss",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" })
			};
			var refListNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ConfigTargets",
				Children = new List<ConfigurableDictionaryNode> { headwordNode, glossNode }
			};
			var nameNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwnerType",
				SubField = "Name",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" })
			};
			var referencesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexSenseReferences",
				DictionaryNodeOptions = new DictionaryNodeListOptions
				{
					ListId = DictionaryNodeListOptions.ListIds.Sense,
					Options = DictionaryDetailsControllerTests.ListOfEnabledDNOsFromStrings(new[] { refType.Guid + ":r" })
				},
				Children = new List<ConfigurableDictionaryNode> { nameNode, refListNode }
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				Children = new List<ConfigurableDictionaryNode> { referencesNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { sensesNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var output = ConfiguredLcmGenerator.GenerateContentForEntry(armEntry, mainEntryNode, null, settings).ToString();
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

		[Test]
		public void GenerateContentForEntry_GeneratesConfigTargetsForSubSenseProperly()
		{
			const string firstHeadword = "homme";
			var firstEntry = CreateInterestingLexEntry(Cache, firstHeadword);
			AddSingleSubSenseToSense("man", firstEntry.SensesOS[0]);
			var legEntry = CreateInterestingLexEntry(Cache, "jambe", "leg");
			const string refTypeName = "Part";
			const string refTypeRevName = "Whole";
			CreateLexicalReference(Cache, firstEntry, firstEntry.SensesOS[0].SensesOS[0], legEntry.SensesOS.First(), refTypeName, refTypeRevName);
			var refType = Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.First(poss => poss.Name.BestAnalysisAlternative.Text == refTypeName);
			Assert.That(refType, Is.Not.Null);

			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "HeadWord",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};
			var glossNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Gloss",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" }, DictionaryNodeWritingSystemOptions.WritingSystemType.Analysis)
			};
			var refListNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ConfigTargets",
				Children = new List<ConfigurableDictionaryNode> { headwordNode, glossNode }
			};
			var referencesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexSenseReferences",
				DictionaryNodeOptions = new DictionaryNodeListOptions
				{
					ListId = DictionaryNodeListOptions.ListIds.Sense,
					Options = DictionaryDetailsControllerTests.ListOfEnabledDNOsFromStrings(new[] { refType.Guid + ":r" })
				},
				Children = new List<ConfigurableDictionaryNode> { refListNode }
			};
			var subSensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" }),
				Children = new List<ConfigurableDictionaryNode> { referencesNode }
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				Children = new List<ConfigurableDictionaryNode> { subSensesNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { sensesNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var output = ConfiguredLcmGenerator.GenerateContentForEntry(firstEntry, mainEntryNode, null, settings).ToString();
			var goodTarget = string.Format(
				"//span[@class='lexsensereferences']/span[@class='lexsensereference']/span[@class='configtargets']/span[@class='configtarget']/span[@class='headword']/span[@lang='fr' and text()='{0}']", firstHeadword);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(goodTarget, 1);
			var badTarget = "//span[@class='lexsensereferences']/span[@class='lexsensereference']/span[@class='configtargets']/span[@class='configtarget']/span[@class='gloss']";
			AssertThatXmlIn.String(output).HasNoMatchForXpath(badTarget);
		}

		[Test]
		public void GenerateContentForEntry_GeneratesConfigTargetsForTreeBetweenSenses()
		{
			const string headword = "headword";
			var firstEntry = CreateInterestingLexEntry(Cache, headword, "b1");
			AddSenseToEntry(firstEntry, "b2", m_wsEn, Cache);
			const string refTypeName = "Part";
			const string refTypeRevName = "Whole";
			CreateLexicalReference(Cache, firstEntry.SensesOS[0], firstEntry.SensesOS[1], refTypeName, refTypeRevName);
			var refType = Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.First(poss => poss.Name.BestAnalysisAlternative.Text == refTypeName);
			Assert.That(refType, Is.Not.Null);

			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "HeadWord",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" }),
				IsEnabled = true
			};
			var glossNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Gloss",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" }, DictionaryNodeWritingSystemOptions.WritingSystemType.Analysis)
			};
			var refListNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ConfigTargets",
				Children = new List<ConfigurableDictionaryNode> { headwordNode, glossNode }
			};
			var nameNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwnerType",
				SubField = "Name",
				After = ": ",
				DictionaryNodeOptions =
					GetWsOptionsForLanguages(new[] { "analysis" }, DictionaryNodeWritingSystemOptions.WritingSystemType.Analysis)
			};
			var referencesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexSenseReferences",
				DictionaryNodeOptions = new DictionaryNodeListOptions
				{
					ListId = DictionaryNodeListOptions.ListIds.Sense,
					Options = DictionaryDetailsControllerTests.ListOfEnabledDNOsFromStrings(new[] { refType.Guid + ":r", refType.Guid + ":f" })
				},
				Children = new List<ConfigurableDictionaryNode> { nameNode, refListNode }
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions { NumberingStyle = "%d" },
				Children = new List<ConfigurableDictionaryNode> { glossNode, referencesNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { sensesNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(firstEntry, mainEntryNode, DefaultDecorator, settings).ToString();

			var goodTarget1 = "//span[@class='lexsensereferences']/span[@class='lexsensereference']/span[@class='ownertype_name']/span[text()='Part']/ancestor::span[1]/following-sibling::node()//span[@class='configtarget']/span[@class='gloss']/span[@lang='en' and text()='b2']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(goodTarget1, 1);
			var badTarget1 = "//span[@class='lexsensereferences']/span[@class='lexsensereference']/span[@class='ownertype_name']/span[text()='Part']/ancestor::span[1]/following-sibling::node()//span[@class='configtarget']/span[@class='gloss']/span[@lang='en' and text()='b1']";
			AssertThatXmlIn.String(result).HasNoMatchForXpath(badTarget1);
			var goodTarget2 = "//span[@class='lexsensereferences']/span[@class='lexsensereference']/span[@class='ownertype_name']/span[text()='Whole']/ancestor::span[1]/following-sibling::node()//span[@class='configtarget']/span[@class='gloss']/span[@lang='en' and text()='b1']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(goodTarget2, 1);
			var badTarget2 = "//span[@class='lexsensereferences']/span[@class='lexsensereference']/span[@class='ownertype_name']/span[text()='Whole']/ancestor::span[1]/following-sibling::node()//span[@class='configtarget']/span[@class='gloss']/span[@lang='en' and text()='b2']";
			AssertThatXmlIn.String(result).HasNoMatchForXpath(badTarget2);
		}

		[Test]
		public void IsListItemSelectedForExport_Variant_SelectedItemReturnsTrue()
		{
			var mainEntry = CreateInterestingLexEntry(Cache);
			var variantForm = CreateInterestingLexEntry(Cache);
			CreateVariantForm(Cache, mainEntry, variantForm);
			var crazyVariantPoss = Cache.LangProject.LexDbOA.VariantEntryTypesOA.PossibilitiesOS.First(variant => variant.Name.BestAnalysisAlternative.Text == TestVariantName);

			var variantsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VariantFormEntryBackRefs",
				DictionaryNodeOptions = GetListOptionsForItems(DictionaryNodeListOptions.ListIds.Variant, new[] { crazyVariantPoss })
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { variantsNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);

			//SUT
			Assert.IsTrue(ConfiguredLcmGenerator.IsListItemSelectedForExport(variantsNode, variantForm.VisibleVariantEntryRefs.First(), variantForm));
		}

		/// <summary>
		/// Test the new section of ConfiguredLcmGenerator.IsListItemSelectedForExport() that
		/// handles Minor Entry -> Variant Of
		/// </summary>
		[Test]
		public void IsListItemSelectedForExport_MinorVariant_SelectedItemReturnsTrue()
		{
			var mainEntry = CreateInterestingLexEntry(Cache);
			var minorEntry = CreateInterestingLexEntry(Cache);
			CreateVariantForm(Cache, mainEntry, minorEntry);
			var crazyVariantPoss = Cache.LangProject.LexDbOA.VariantEntryTypesOA.PossibilitiesOS.First(variant => variant.Name.BestAnalysisAlternative.Text == TestVariantName);

			var minorEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				CSSClassNameOverride = "minorentryvariant",
				IsEnabled = true,
				DictionaryNodeOptions = GetListOptionsForItems(DictionaryNodeListOptions.ListIds.Variant, new[] { crazyVariantPoss })
			};

			//SUT
			Assert.IsTrue(ConfiguredLcmGenerator.IsListItemSelectedForExport(minorEntryNode, minorEntry));
		}

		[Test]
		public void IsListItemSelectedForExport_Variant_UnselectedItemReturnsFalse()
		{
			var formNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwningEntry",
				SubField = "MLHeadWord",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};
			var mainEntry = CreateInterestingLexEntry(Cache);
			var variantForm = CreateInterestingLexEntry(Cache);
			CreateVariantForm(Cache, mainEntry, variantForm);
			var notCrazyVariant = Cache.LangProject.LexDbOA.VariantEntryTypesOA.PossibilitiesOS.FirstOrDefault(variant => variant.Name.BestAnalysisAlternative.Text != TestVariantName);
			Assert.That(notCrazyVariant, Is.Not.Null);
			var rcfsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VariantFormEntryBackRefs",
				DictionaryNodeOptions = GetListOptionsForItems(DictionaryNodeListOptions.ListIds.Variant, new[] { notCrazyVariant }),
				Children = new List<ConfigurableDictionaryNode> { formNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { rcfsNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);

			//SUT
			Assert.IsFalse(ConfiguredLcmGenerator.IsListItemSelectedForExport(rcfsNode, variantForm.VisibleVariantEntryRefs.First(), variantForm));
		}

		[Test]
		public void IsListItemSelectedForExport_Complex_SelectedItemReturnsTrue()
		{
			var mainEntry = CreateInterestingLexEntry(Cache);
			var complexForm = CreateInterestingLexEntry(Cache);
			var complexFormRef = CreateComplexForm(Cache, mainEntry, complexForm, false);
			var complexRefName = complexFormRef.ComplexEntryTypesRS[0].Name.BestAnalysisAlternative.Text;
			var complexTypePoss = Cache.LangProject.LexDbOA.ComplexEntryTypesOA.PossibilitiesOS.First(complex => complex.Name.BestAnalysisAlternative.Text == complexRefName);

			var variantsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VisibleComplexFormBackRefs",
				DictionaryNodeOptions = GetListOptionsForItems(DictionaryNodeListOptions.ListIds.Complex, new[] { complexTypePoss })
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { variantsNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);

			//SUT
			Assert.IsTrue(ConfiguredLcmGenerator.IsListItemSelectedForExport(variantsNode, mainEntry.VisibleComplexFormBackRefs.First(), mainEntry));
		}

		[Test]
		public void IsListItemSelectedForExport_Complex_SubentrySelectedItemReturnsTrue()
		{
			var mainEntry = CreateInterestingLexEntry(Cache);
			var complexForm = CreateInterestingLexEntry(Cache);
			var complexFormRef = CreateComplexForm(Cache, mainEntry, complexForm, true);
			var complexRefName = complexFormRef.ComplexEntryTypesRS[0].Name.BestAnalysisAlternative.Text;
			var complexTypePoss = Cache.LangProject.LexDbOA.ComplexEntryTypesOA.PossibilitiesOS.First(complex => complex.Name.BestAnalysisAlternative.Text == complexRefName);

			var variantsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Subentries",
				DictionaryNodeOptions = GetListOptionsForItems(DictionaryNodeListOptions.ListIds.Complex, new[] { complexTypePoss })
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { variantsNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);

			//SUT
			Assert.IsTrue(ConfiguredLcmGenerator.IsListItemSelectedForExport(variantsNode, mainEntry.Subentries.First(), mainEntry));
		}

		[Test]
		public void IsListItemSelectedForExport_Complex_UnselectedItemReturnsFalse()
		{
			var formNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwningEntry",
				SubField = "MLHeadWord",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};
			var mainEntry = CreateInterestingLexEntry(Cache);
			var complexForm = CreateInterestingLexEntry(Cache);
			var complexFormRef = CreateComplexForm(Cache, mainEntry, complexForm, false);
			var complexRefName = complexFormRef.ComplexEntryTypesRS[0].Name.BestAnalysisAlternative.Text;
			var notComplexTypePoss = Cache.LangProject.LexDbOA.ComplexEntryTypesOA.PossibilitiesOS.First(complex => complex.Name.BestAnalysisAlternative.Text != complexRefName);
			Assert.That(notComplexTypePoss, Is.Not.Null);
			var rcfsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VisibleComplexFormBackRefs",
				DictionaryNodeOptions = GetListOptionsForItems(DictionaryNodeListOptions.ListIds.Complex, new[] { notComplexTypePoss }),
				Children = new List<ConfigurableDictionaryNode> { formNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { rcfsNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);

			//SUT
			Assert.IsFalse(ConfiguredLcmGenerator.IsListItemSelectedForExport(rcfsNode, mainEntry.VisibleComplexFormBackRefs.First(), mainEntry));
		}

		[Test]
		public void IsListItemSelectedForExport_Entry_SelectedItemReturnsTrue()
		{
			var formNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwningEntry",
				SubField = "MLHeadWord",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};
			var mainEntry = CreateInterestingLexEntry(Cache);
			var referencedEntry = CreateInterestingLexEntry(Cache);
			const string refTypeName = "TestRefType";
			CreateLexicalReference(Cache, mainEntry, referencedEntry, refTypeName);
			var notComplexTypePoss = Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.First(refType => refType.Name.BestAnalysisAlternative.Text == refTypeName);
			Assert.That(notComplexTypePoss, Is.Not.Null);
			var entryReferenceNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MinimalLexReferences",
				DictionaryNodeOptions = GetListOptionsForItems(DictionaryNodeListOptions.ListIds.Entry, new[] { notComplexTypePoss }),
				Children = new List<ConfigurableDictionaryNode> { formNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { entryReferenceNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			Assert.IsTrue(ConfiguredLcmGenerator.IsListItemSelectedForExport(entryReferenceNode, mainEntry.MinimalLexReferences.First(), mainEntry));
			Assert.IsTrue(ConfiguredLcmGenerator.IsListItemSelectedForExport(entryReferenceNode, referencedEntry.MinimalLexReferences.First(), referencedEntry));
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
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};
			var mainEntry = CreateInterestingLexEntry(Cache);
			var referencedEntry = CreateInterestingLexEntry(Cache);
			const string refTypeName = "TestRefType";
			CreateLexicalReference(Cache, mainEntry, referencedEntry, refTypeName, "ReverseName");
			var notComplexTypePoss = Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.First(refType => refType.Name.BestAnalysisAlternative.Text == refTypeName);
			Assert.That(notComplexTypePoss, Is.Not.Null);
			var entryReferenceNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MinimalLexReferences",
				DictionaryNodeOptions = new DictionaryNodeListOptions
				{
					ListId = DictionaryNodeListOptions.ListIds.Entry,
					Options = DictionaryDetailsControllerTests.ListOfEnabledDNOsFromStrings(new[] { notComplexTypePoss.Guid + ":r" })
				},
				Children = new List<ConfigurableDictionaryNode> { formNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { entryReferenceNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			Assert.IsFalse(ConfiguredLcmGenerator.IsListItemSelectedForExport(entryReferenceNode, mainEntry.MinimalLexReferences.First(), mainEntry));
			Assert.IsTrue(ConfiguredLcmGenerator.IsListItemSelectedForExport(entryReferenceNode, referencedEntry.MinimalLexReferences.First(), referencedEntry));
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
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};
			var mainEntry = CreateInterestingLexEntry(Cache);
			var referencedEntry = CreateInterestingLexEntry(Cache);
			const string refTypeName = "TestRefType";
			CreateLexicalReference(Cache, mainEntry, referencedEntry, refTypeName, "ReverseName");
			var notComplexTypePoss = Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.First(refType => refType.Name.BestAnalysisAlternative.Text == refTypeName);
			Assert.That(notComplexTypePoss, Is.Not.Null);
			var entryReferenceNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MinimalLexReferences",
				DictionaryNodeOptions = new DictionaryNodeListOptions
				{
					ListId = DictionaryNodeListOptions.ListIds.Entry,
					Options = DictionaryDetailsControllerTests.ListOfEnabledDNOsFromStrings(new[] { notComplexTypePoss.Guid + ":f" })
				},
				Children = new List<ConfigurableDictionaryNode> { formNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { entryReferenceNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			Assert.IsTrue(ConfiguredLcmGenerator.IsListItemSelectedForExport(entryReferenceNode, mainEntry.MinimalLexReferences.First(), mainEntry));
			Assert.IsFalse(ConfiguredLcmGenerator.IsListItemSelectedForExport(entryReferenceNode, referencedEntry.MinimalLexReferences.First(), referencedEntry));
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
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};
			var mainEntry = CreateInterestingLexEntry(Cache);
			var referencedEntry = CreateInterestingLexEntry(Cache);
			const string refTypeName = "TestRefType";
			CreateLexicalReference(Cache, mainEntry, referencedEntry, refTypeName, "ReverseName");
			var notComplexTypePoss = Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.First(refType => refType.Name.BestAnalysisAlternative.Text == refTypeName);
			Assert.That(notComplexTypePoss, Is.Not.Null);
			var entryReferenceNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MinimalLexReferences",
				DictionaryNodeOptions = new DictionaryNodeListOptions
				{
					ListId = DictionaryNodeListOptions.ListIds.Entry,
					Options = DictionaryDetailsControllerTests.ListOfEnabledDNOsFromStrings(new[] { notComplexTypePoss.Guid + ":f", notComplexTypePoss.Guid + ":r", })
				},
				Children = new List<ConfigurableDictionaryNode> { formNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { entryReferenceNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			Assert.IsTrue(ConfiguredLcmGenerator.IsListItemSelectedForExport(entryReferenceNode, mainEntry.MinimalLexReferences.First(), mainEntry));
			Assert.IsTrue(ConfiguredLcmGenerator.IsListItemSelectedForExport(entryReferenceNode, referencedEntry.MinimalLexReferences.First(), referencedEntry));
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
			if (Cache.LangProject.LexDbOA.ReferencesOA == null)
				Cache.LangProject.LexDbOA.ReferencesOA = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
			Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.Add(lrt);
			lrt.Name.set_String(Cache.DefaultAnalWs, "NotOurTestRefType");

			const string refTypeName = "TestRefType";
			CreateLexicalReference(Cache, mainEntry, referencedEntry, refTypeName);
			var notComplexTypePoss = Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.First(refType => refType.Name.BestAnalysisAlternative.Text != refTypeName);
			Assert.That(notComplexTypePoss, Is.Not.Null);
			var entryReferenceNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MinimalLexReferences",
				IsEnabled = true,
				DictionaryNodeOptions = GetListOptionsForItems(DictionaryNodeListOptions.ListIds.Entry, new[] { notComplexTypePoss }),
				Children = new List<ConfigurableDictionaryNode> { formNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { entryReferenceNode }
			};
			DictionaryConfigurationModel.SpecifyParentsAndReferences(new List<ConfigurableDictionaryNode> { mainEntryNode });
			Assert.IsFalse(ConfiguredLcmGenerator.IsListItemSelectedForExport(entryReferenceNode, mainEntry.MinimalLexReferences.First(), mainEntry));
			Assert.IsFalse(ConfiguredLcmGenerator.IsListItemSelectedForExport(entryReferenceNode, referencedEntry.MinimalLexReferences.First(), referencedEntry));
		}

		[Test]
		public void IsListItemSelectedForExport_EntryWithNoOptions_Throws()
		{
			var formNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwningEntry",
				SubField = "MLHeadWord"
			};
			var mainEntry = CreateInterestingLexEntry(Cache);
			var referencedEntry = CreateInterestingLexEntry(Cache);
			const string refTypeName = "TestRefType";
			CreateLexicalReference(Cache, mainEntry, referencedEntry, refTypeName);
			var notComplexTypePoss = Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.First(refType => refType.Name.BestAnalysisAlternative.Text == refTypeName);
			Assert.That(notComplexTypePoss, Is.Not.Null);
			var entryReferenceNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MinimalLexReferences",
				Children = new List<ConfigurableDictionaryNode> { formNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { entryReferenceNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			Assert.Throws<ArgumentException>(() => ConfiguredLcmGenerator.IsListItemSelectedForExport(entryReferenceNode, mainEntry.MinimalLexReferences.First(), mainEntry));
		}

		[Test]
		public void GenerateContentForEntry_NoncheckedListItemsAreNotGenerated()
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
			CreateVariantForm(Cache, mainEntry, variantForm);
			var notCrazyVariant = Cache.LangProject.LexDbOA.VariantEntryTypesOA.PossibilitiesOS.FirstOrDefault(variant => variant.Name.BestAnalysisAlternative.Text != TestVariantName);
			Assert.That(notCrazyVariant, Is.Not.Null);
			var variantsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VariantFormEntryBackRefs",
				IsEnabled = true,
				DictionaryNodeOptions = GetListOptionsForItems(DictionaryNodeListOptions.ListIds.Variant, new[] { notCrazyVariant }),
				Children = new List<ConfigurableDictionaryNode> { formNode }
			};
			var headword = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLHeadWord",
				CSSClassNameOverride = "headword",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" }),
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode>()
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { headword, variantsNode }
			};
			DictionaryConfigurationModel.SpecifyParentsAndReferences(new List<ConfigurableDictionaryNode> { mainEntryNode });

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(mainEntry, mainEntryNode, null, settings).ToString();
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
				"/div[@class='lexentry']/span[@class='variantformentrybackrefs']/span[@class='variantformentrybackref']/span[@lang='fr']", 0);
		}

		[Test]
		public void GenerateContentForEntry_CheckedListItemsAreGenerated()
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
			CreateVariantForm(Cache, mainEntry, variantForm);
			var crazyVariant = Cache.LangProject.LexDbOA.VariantEntryTypesOA.PossibilitiesOS.FirstOrDefault(variant => variant.Name.BestAnalysisAlternative.Text == TestVariantName);
			Assert.That(crazyVariant, Is.Not.Null);

			var variantFormTypeNameNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Name",
				IsEnabled = true,
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "analysis" })
			};
			var variantFormTypeNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VariantEntryTypesRS",
				CSSClassNameOverride = "variantentrytypes",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { variantFormTypeNameNode },
			};
			var variantsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VariantFormEntryBackRefs",
				IsEnabled = true,
				DictionaryNodeOptions = GetListOptionsForItems(DictionaryNodeListOptions.ListIds.Variant, new[] { crazyVariant }),
				Children = new List<ConfigurableDictionaryNode> { variantFormTypeNode, formNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { variantsNode }
			};
			DictionaryConfigurationModel.SpecifyParentsAndReferences(new List<ConfigurableDictionaryNode> { mainEntryNode });

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(mainEntry, mainEntryNode, null, settings).ToString();
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
				"/div[@class='lexentry']/span[@class='variantformentrybackrefs']/span[@class='variantformentrybackref']//span[@lang='fr']/span[@lang='fr']", 2);
		}

		[Test]
		public void GenerateContentForEntry_VariantTypeIsUncheckedAndHeadwordIsChecked()
		{
			var mainEntry = CreateInterestingLexEntry(Cache);
			var variantForm = CreateInterestingLexEntry(Cache);
			CreateVariantForm(Cache, mainEntry, variantForm);
			var crazyVariant = Cache.LangProject.LexDbOA.VariantEntryTypesOA.PossibilitiesOS.FirstOrDefault(variant => variant.Name.BestAnalysisAlternative.Text == TestVariantName);
			Assert.That(crazyVariant, Is.Not.Null);

			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "HeadWord",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" }),
				IsEnabled = true
			};
			var refNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ConfigReferencedEntries",
				Children = new List<ConfigurableDictionaryNode> { headwordNode },
				CSSClassNameOverride = "referencedentries",
				IsEnabled = true
			};
			var variantFormTypeNameNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Name",
				IsEnabled = true,
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "analysis" })
			};
			var variantFormTypeNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VariantEntryTypesRS",
				CSSClassNameOverride = "variantentrytypes",
				IsEnabled = false,
				Children = new List<ConfigurableDictionaryNode> { variantFormTypeNameNode },
			};
			var variantsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VariantFormEntryBackRefs",
				IsEnabled = true,
				DictionaryNodeOptions = GetListOptionsForItems(DictionaryNodeListOptions.ListIds.Variant, new[] { crazyVariant }),
				Children = new List<ConfigurableDictionaryNode> { variantFormTypeNode, refNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { variantsNode }
			};
			DictionaryConfigurationModel.SpecifyParentsAndReferences(new List<ConfigurableDictionaryNode> { mainEntryNode });

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(mainEntry, mainEntryNode, null, settings).ToString();
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
				"/div[@class='lexentry']/span[@class='variantformentrybackrefs']/span[@class='variantformentrybackref']/span[@class='referencedentries']" +
				"/span[@class='referencedentry']/span[@class='headword']/span[@lang='fr']/span[@lang='fr' and text()='Citation']", 1);
		}

		[Test]
		public void GenerateContentForEntry_ReferencedComplexFormsUnderSensesIncludesSubentriesAndOtherReferencedComplexForms()
		{
			var complexFormNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwningEntry",
				SubField = "MLHeadWord",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};
			var rcfsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VisibleComplexFormBackRefs",
				Children = new List<ConfigurableDictionaryNode> { complexFormNode }
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				DictionaryNodeOptions = GetSenseNodeOptions(),
				Children = new List<ConfigurableDictionaryNode> { rcfsNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { sensesNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);

			var mainEntry = CreateInterestingLexEntry(Cache);
			var otherReferencedComplexForm = CreateInterestingLexEntry(Cache);
			var subentry = CreateInterestingLexEntry(Cache);
			CreateComplexForm(Cache, mainEntry.SensesOS[0], subentry, true);
			CreateComplexForm(Cache, mainEntry.SensesOS[0], otherReferencedComplexForm, false);

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(mainEntry, mainEntryNode, null, settings).ToString();
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
				xpathThruSense + "/span[@class='visiblecomplexformbackrefs']/span[@class='visiblecomplexformbackref']//span[@lang='fr']/span[@lang='fr']", 4);
		}

		[Test]
		public void GenerateLetterHeaderIfNeeded_GeneratesHeaderIfNoPreviousHeader()
		{
			var entry = CreateInterestingLexEntry(Cache);
			var vernWs = Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Id;
			using (var col = new CollatorForTest(vernWs))
			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				// SUT
				string last = null;
				XHTMLWriter.WriteStartElement("TestElement");
				LcmXhtmlGenerator.GenerateLetterHeaderIfNeeded(entry, ref last, XHTMLWriter, col, DefaultSettings, m_Clerk);
				XHTMLWriter.WriteEndElement();
				XHTMLWriter.Flush();
				const string letterHeaderToMatch = "//div[@class='letHead']/span[@class='letter' and @lang='fr' and text()='C c']";
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(letterHeaderToMatch, 1);
			}
		}

		[Test]
		public void GenerateLetterHeaderIfNeeded_GeneratesHeaderIfPreviousHeaderDoesNotMatch()
		{
			var entry = CreateInterestingLexEntry(Cache);
			var vernWs = Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Id;
			using (var col = new CollatorForTest(vernWs))
			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				// SUT
				var last = "A a";
				XHTMLWriter.WriteStartElement("TestElement");
				LcmXhtmlGenerator.GenerateLetterHeaderIfNeeded(entry, ref last, XHTMLWriter, col, DefaultSettings, m_Clerk);
				XHTMLWriter.WriteEndElement();
				XHTMLWriter.Flush();
				const string letterHeaderToMatch = "//div[@class='letHead']/span[@class='letter' and @lang='fr' and text()='C c']";
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(letterHeaderToMatch, 1);
			}
		}

		[Test]
		public void GenerateLetterHeaderIfNeeded_GeneratesHeaderForSuffixWithNewBaseLetter()
		{
			var entry = CreateInterestingSuffix(Cache, " ba");
			var vernWs = Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Id;
			using (var col = new CollatorForTest(vernWs))
			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				// SUT
				var last = "A a";
				XHTMLWriter.WriteStartElement("TestElement");
				LcmXhtmlGenerator.GenerateLetterHeaderIfNeeded(entry, ref last, XHTMLWriter, col, DefaultSettings, m_Clerk);
				XHTMLWriter.WriteEndElement();
				XHTMLWriter.Flush();
				const string letterHeaderToMatch = "//div[@class='letHead']/span[@class='letter' and @lang='fr' and text()='B b']";
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(letterHeaderToMatch, 1);
			}
		}

		[Test]
		public void GenerateLetterHeaderIfNeeded_GeneratesNoHeaderIfPreviousHeaderDoesMatch()
		{
			var entry = CreateInterestingLexEntry(Cache); var vernWs = Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Id;
			using (var col = new CollatorForTest(vernWs))
			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				// SUT
				var last = "A a";
				XHTMLWriter.WriteStartElement("TestElement");
				LcmXhtmlGenerator.GenerateLetterHeaderIfNeeded(entry, ref last, XHTMLWriter, col, DefaultSettings, m_Clerk);
				LcmXhtmlGenerator.GenerateLetterHeaderIfNeeded(entry, ref last, XHTMLWriter, col, DefaultSettings, m_Clerk);
				XHTMLWriter.WriteEndElement();
				XHTMLWriter.Flush();
				const string letterHeaderToMatch = "//div[@class='letHead']/span[@class='letter' and @lang='fr' and text()='C c']";
				const string proveOnlyOneHeader = "//div[@class='letHead']/span[@class='letter']";
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(letterHeaderToMatch, 1);
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(proveOnlyOneHeader, 1);
			}
		}

		[Test]
		public void GenerateLetterHeaderIfNeeded_WSHasCaseAlias_GeneratesHeadingWithCorrectPair()
		{
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("tkr", out var wsDef);
			wsDef.CaseAlias = "tur";
			Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem = wsDef;
			var dotlessEntry = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(dotlessEntry, "Ia", wsDef.Handle);
			var dottedEntry1 = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(dottedEntry1, "\u0130brahim", wsDef.Handle);
			var dottedEntry2 = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(dottedEntry2, "icaza", wsDef.Handle);
			using (var col = new CollatorForTest(wsDef.Id))
			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				// SUT
				string last = null;
				XHTMLWriter.WriteStartElement("TestElement");
				LcmXhtmlGenerator.GenerateLetterHeaderIfNeeded(dotlessEntry, ref last, XHTMLWriter, col, DefaultSettings, m_Clerk);
				LcmXhtmlGenerator.GenerateLetterHeaderIfNeeded(dottedEntry1, ref last, XHTMLWriter, col, DefaultSettings, m_Clerk);
				LcmXhtmlGenerator.GenerateLetterHeaderIfNeeded(dottedEntry2, ref last, XHTMLWriter, col, DefaultSettings, m_Clerk);
				XHTMLWriter.WriteEndElement();
				XHTMLWriter.Flush();
				const string dotlessHeadingXpath = "//div[@class='letHead']/span[@class='letter' and @lang='tkr' and text()='I \u0131']";
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(dotlessHeadingXpath, 1);
				const string dottedHeadingXpath = "//div[@class='letHead']/span[@class='letter' and @lang='tkr' and text()='\u0130 i']";
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(dottedHeadingXpath, 1);
			}
		}

		[Test]
		public void GenerateLetterHeaderIfNeeded_GeneratesHeaderLexemeFormSorting()
		{
			var entry = CreateInterestingLexEntry(Cache);
			var vernWs = Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Id;
			AddLexemeFormToEntry(entry, "LexFormStr", Cache);
			using (var col = new CollatorForTest(vernWs))
			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				// SUT
				string last = null;
				XHTMLWriter.WriteStartElement("TestElement");
				string oldSort = m_Clerk.SortName;
				try
				{
					m_Clerk.SortName = "Lexeme Form";
					LcmXhtmlGenerator.GenerateLetterHeaderIfNeeded(entry, ref last, XHTMLWriter, col, DefaultSettings, m_Clerk);
				}
				finally
				{
					m_Clerk.SortName = oldSort;
				}

				XHTMLWriter.WriteEndElement();
				XHTMLWriter.Flush();
				const string letterHeaderToMatch = "//div[@class='letHead']/span[@class='letter' and @lang='fr' and text()='L l']";
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(letterHeaderToMatch, 1);
			}
		}

		[Test]
		public void GenerateLetterHeaderIfNeeded_GeneratesHeaderCitationFormSorting()
		{
			var entry = CreateInterestingLexEntry(Cache, "CitFormStr");
			var vernWs = Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Id;
			AddLexemeFormToEntry(entry, "LexFormStr", Cache);
			using (var col = new CollatorForTest(vernWs))
			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				// SUT
				string last = null;
				XHTMLWriter.WriteStartElement("TestElement");
				string oldSort = m_Clerk.SortName;
				try
				{
					m_Clerk.SortName = "Citation Form";
					LcmXhtmlGenerator.GenerateLetterHeaderIfNeeded(entry, ref last, XHTMLWriter, col, DefaultSettings, m_Clerk);
				}
				finally
				{
					m_Clerk.SortName = oldSort;
				}

				XHTMLWriter.WriteEndElement();
				XHTMLWriter.Flush();
				const string letterHeaderToMatch = "//div[@class='letHead']/span[@class='letter' and @lang='fr' and text()='C c']";
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(letterHeaderToMatch, 1);
			}
		}

		[Test]
		public void GenerateContentForEntry_OneSenseWithSinglePicture()
		{
			var wsOpts = GetWsOptionsForLanguages(new[] { "en" });
			var thumbNailNode = new ConfigurableDictionaryNode { FieldDescription = "PictureFileRA", CSSClassNameOverride = "photo" };
			var senseNumberNode = new ConfigurableDictionaryNode { FieldDescription = "SenseNumberTSS", CSSClassNameOverride = "sensenumber" };
			var captionNode = new ConfigurableDictionaryNode { FieldDescription = "Caption", DictionaryNodeOptions = wsOpts };
			var pictureNode = new ConfigurableDictionaryNode
			{
				DictionaryNodeOptions = new DictionaryNodePictureOptions(),
				FieldDescription = "PicturesOfSenses",
				CSSClassNameOverride = "Pictures",
				Children = new List<ConfigurableDictionaryNode> { thumbNailNode, senseNumberNode, captionNode }
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Senses",
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { sensesNode, pictureNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var testEntry = CreateInterestingLexEntry(Cache);
			var sense = testEntry.SensesOS[0];
			sense.PicturesOS.Add(CreatePicture(Cache));

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(testEntry, mainEntryNode, null, settings).ToString();
			const string oneSenseWithPicture = "/div[@class='lexentry']/span[@class='pictures']/div[@class='picture']/img[@class='photo' and @id]";
			const string oneSenseWithPictureCaption = "/div[@class='lexentry']/span[@class='pictures']/div[@class='picture']/div[@class='captionContent']/span[@class='caption']//span[text()='caption']";
			//This assert is dependent on the specific entry data created in CreateInterestingLexEntry
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(oneSenseWithPicture, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(oneSenseWithPictureCaption, 1);
		}

		[Test]
		public void GenerateContentForEntry_PictureFileMissing()
		{
			var wsOpts = GetWsOptionsForLanguages(new[] { "en" });
			var thumbNailNode = new ConfigurableDictionaryNode { FieldDescription = "PictureFileRA", CSSClassNameOverride = "photo" };
			var senseNumberNode = new ConfigurableDictionaryNode { FieldDescription = "SenseNumberTSS", CSSClassNameOverride = "sensenumber" };
			var captionNode = new ConfigurableDictionaryNode { FieldDescription = "Caption", DictionaryNodeOptions = wsOpts };
			var pictureNode = new ConfigurableDictionaryNode
			{
				DictionaryNodeOptions = new DictionaryNodePictureOptions(),
				FieldDescription = "PicturesOfSenses",
				CSSClassNameOverride = "Pictures",
				Children = new List<ConfigurableDictionaryNode> { thumbNailNode, senseNumberNode, captionNode }
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Senses",
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { sensesNode, pictureNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var testEntry = CreateInterestingLexEntry(Cache);
			var sense = testEntry.SensesOS[0];
			sense.PicturesOS.Add(CreatePicture(Cache, false));

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(testEntry, mainEntryNode, null, settings).ToString();
			Assert.IsEmpty(result);
		}

		/// <summary>LT-21573: PictureFileRA can be null after an incomplete SFM import</summary>
		[Test]
		public void GenerateContentForEntry_PictureFileRAMissing()
		{
			var wsOpts = GetWsOptionsForLanguages(new[] { "en" });
			var thumbNailNode = new ConfigurableDictionaryNode { FieldDescription = "PictureFileRA", CSSClassNameOverride = "photo" };
			var senseNumberNode = new ConfigurableDictionaryNode { FieldDescription = "SenseNumberTSS", CSSClassNameOverride = "sensenumber" };
			var captionNode = new ConfigurableDictionaryNode { FieldDescription = "Caption", DictionaryNodeOptions = wsOpts };
			var pictureNode = new ConfigurableDictionaryNode
			{
				DictionaryNodeOptions = new DictionaryNodePictureOptions(),
				FieldDescription = "PicturesOfSenses",
				CSSClassNameOverride = "Pictures",
				Children = new List<ConfigurableDictionaryNode> { thumbNailNode, senseNumberNode, captionNode }
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Senses",
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { sensesNode, pictureNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var testEntry = CreateInterestingLexEntry(Cache);
			var pic = CreatePicture(Cache);
			pic.PictureFileRA = null;
			testEntry.SensesOS[0].PicturesOS.Add(pic);

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(testEntry, mainEntryNode, null, settings).ToString();
			Assert.That(result, Is.Empty);
		}

		[Test]
		public void GenerateContentForEntry_PictureWithCreator()
		{
			var thumbNailNode = new ConfigurableDictionaryNode { FieldDescription = "PictureFileRA", CSSClassNameOverride = "photo" };
			var creatorNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "@extension:SIL.FieldWorks.XWorks.DictConfigModelExt.Creator",
				CSSClassNameOverride = "creator"
			};
			var pictureNode = new ConfigurableDictionaryNode
			{
				DictionaryNodeOptions = new DictionaryNodePictureOptions(),
				FieldDescription = "PicturesOfSenses",
				CSSClassNameOverride = "Pictures",
				Children = new List<ConfigurableDictionaryNode> { thumbNailNode, creatorNode }
			};
			var sensesNode = new ConfigurableDictionaryNode { FieldDescription = "Senses" };
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { sensesNode, pictureNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var testEntry = CreateInterestingLexEntry(Cache);
			var sense = testEntry.SensesOS[0];
			sense.PicturesOS.Add(CreatePicture(Cache));

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(testEntry, mainEntryNode, null, settings).ToString();
			const string oneSenseWithPicture = "/div[@class='lexentry']/span[@class='pictures']/div[@class='picture']/img[@class='photo' and @id]";
			const string oneSenseWithPictureCaption = "/div[@class='lexentry']/span[@class='pictures']/div[@class='picture']/div[@class='captionContent']/span[@class='creator' and text()='Jason Naylor']";
			//This assert is dependent on the specific entry data created in CreateInterestingLexEntry
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(oneSenseWithPicture, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(oneSenseWithPictureCaption, 1);
		}

		[Test]
		public void GenerateContentForEntry_PictureWithNonUnicodePathLinksCorrectly()
		{
			var mainEntryNode = CreatePictureModel();
			var testEntry = CreateInterestingLexEntry(Cache);
			var sense = testEntry.SensesOS[0];
			var sensePic = Cache.ServiceLocator.GetInstance<ICmPictureFactory>().Create();
			sense.PicturesOS.Add(sensePic);
			var wsEn = Cache.WritingSystemFactory.GetWsFromStr("en");
			sensePic.Caption.set_String(wsEn, TsStringUtils.MakeString("caption", wsEn));
			var pic = Cache.ServiceLocator.GetInstance<ICmFileFactory>().Create();
			var folder = Cache.ServiceLocator.GetInstance<ICmFolderFactory>().Create();
			Cache.LangProject.MediaOC.Add(folder);
			folder.FilesOC.Add(pic);
			const string pathWithUtf8Char = "cave\u00E7on";
			var decomposedPath = CustomIcu.GetIcuNormalizer(FwNormalizationMode.knmNFD).Normalize(pathWithUtf8Char);
			var composedPath = CustomIcu.GetIcuNormalizer(FwNormalizationMode.knmNFC).Normalize(pathWithUtf8Char);
			// Set the internal path to decomposed (which is what FLEx does when it loads data)
			pic.InternalPath = decomposedPath;
			sensePic.PictureFileRA = pic;

			// generates a src attribute with an absolute file path
			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(testEntry, mainEntryNode, null, settings).ToString();
			var pictureWithComposedPath = "/div[@class='lexentry']/span[@class='pictures']/span[@class='picture']/img[contains(@src, '" + composedPath + "')]";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(pictureWithComposedPath, 1);
		}

		[Test]
		public void GenerateContentForEntry_PictureCopiedAndRelativePathUsed()
		{
			var mainEntryNode = CreatePictureModel();
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
			File.WriteAllBytes(filePath, new byte[] { 0xFF, 0xE0, 0x0, 0x0 });
			pic.InternalPath = filePath;
			sensePic.PictureFileRA = pic;

			var tempFolder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "ConfigDictPictureExportTest"));
			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, true, true, tempFolder.FullName);
			try
			{
				//SUT
				var result = ConfiguredLcmGenerator.GenerateContentForEntry(testEntry, mainEntryNode, null, settings).ToString();
				var pictureRelativePath = Path.Combine("pictures", Path.GetFileName(filePath));
				var pictureWithComposedPath = "/div[@class='lexentry']/span[@class='pictures']/span[@class='picture']/img[starts-with(@src, '" + pictureRelativePath + "')]";
				if (!Platform.IsUnix)
					AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(pictureWithComposedPath, 1);
				// that src starts with a string, and escaping any Windows path separators
				AssertRegex(result, string.Format("src=\"{0}[^\"]*\"", pictureRelativePath.Replace(@"\", @"\\")), 1);
				Assert.IsTrue(File.Exists(Path.Combine(tempFolder.Name, "pictures", filePath)));
			}
			finally
			{
				IO.RobustIO.DeleteDirectoryAndContents(tempFolder.FullName);
				File.Delete(filePath);
			}
		}

		[Test]
		public void GenerateContentForEntry_MissingPictureFileDoesNotCrashOnCopy()
		{
			var mainEntryNode = CreatePictureModel();
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

			var tempFolder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "ConfigDictPictureExportTest"));
			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, true, true, tempFolder.FullName);
			try
			{
				//SUT
				var result = ConfiguredLcmGenerator.GenerateContentForEntry(testEntry, mainEntryNode, null, settings).ToString();
				var pictureRelativePath = Path.Combine("pictures", Path.GetFileName(filePath));
				var pictureWithComposedPath = "/div[@class='lexentry']/span[@class='pictures']/span[@class='picture']/img[starts-with(@src, '" + pictureRelativePath + "')]";
				if (!Platform.IsUnix)
					AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(pictureWithComposedPath, 1);
				// that src starts with a string, and escaping any Windows path separators
				AssertRegex(result, string.Format("src=\"{0}[^\"]*\"", pictureRelativePath.Replace(@"\", @"\\")), 1);
				Assert.IsFalse(File.Exists(Path.Combine(tempFolder.Name, "pictures", filePath)));
			}
			finally
			{
				IO.RobustIO.DeleteDirectoryAndContents(tempFolder.FullName);
			}
		}

		[Test]
		public void GenerateContentForEntry_TwoDifferentFilesGetTwoDifferentResults()
		{
			var mainEntryNode = CreatePictureModel();
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
				var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, true, true, tempFolder.FullName);
				//SUT
				var result = ConfiguredLcmGenerator.GenerateContentForEntry(testEntry, mainEntryNode, null, settings).ToString();
				var pictureRelativePath = Path.Combine("pictures", Path.GetFileName(fileName));
				var pictureWithComposedPath = "/div[@class='lexentry']/span[@class='pictures']/span[@class='picture']/img[contains(@src, '" + pictureRelativePath + "')]";
				if (!Platform.IsUnix)
					AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(pictureWithComposedPath, 1);
				// that src contains a string, and escaping any Windows path separators
				AssertRegex(result, string.Format("src=\"[^\"]*{0}[^\"]*\"", pictureRelativePath.Replace(@"\", @"\\")), 1);
				// The second file with the same name should have had something appended to the end of the filename but the initial filename should match both entries
				var filenameWithoutExtension = Path.GetFileNameWithoutExtension(pictureRelativePath);
				var pictureStartsWith = "/div[@class='lexentry']/span[@class='pictures']/span[@class='picture']/img[contains(@src, '" + filenameWithoutExtension + "')]";
				if (!Platform.IsUnix)
					AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(pictureStartsWith, 2);
				// that src contains a string
				AssertRegex(result, string.Format("src=\"[^\"]*{0}[^\"]*\"", filenameWithoutExtension), 2);
				Assert.AreEqual(2, Directory.EnumerateFiles(Path.Combine(tempFolder.FullName, "pictures")).Count(), "Wrong number of pictures copied.");
			}
			finally
			{
				IO.RobustIO.DeleteDirectoryAndContents(tempFolder.FullName);
				File.Delete(filePath1);
				File.Delete(filePath2);
			}
		}

		[Test]
		public void GenerateContentForEntry_UniqueIdsForSameFile()
		{
			var mainEntryNode = CreatePictureModel();
			var testEntry = CreateInterestingLexEntry(Cache);
			AddSenseToEntry(testEntry, "second", m_wsEn, Cache);
			var sense = testEntry.SensesOS[0];
			var sensePic1 = Cache.ServiceLocator.GetInstance<ICmPictureFactory>().Create();
			sense.PicturesOS.Add(sensePic1);
			var pic = Cache.ServiceLocator.GetInstance<ICmFileFactory>().Create();
			var folder = Cache.ServiceLocator.GetInstance<ICmFolderFactory>().Create();
			var sense2 = testEntry.SensesOS[1];
			var sensePic2 = Cache.ServiceLocator.GetInstance<ICmPictureFactory>().Create();
			sense2.PicturesOS.Add(sensePic2);
			Cache.LangProject.MediaOC.Add(folder);
			folder.FilesOC.Add(pic);
			var fileName = Path.GetRandomFileName();
			var tempPath1 = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
			Directory.CreateDirectory(tempPath1);
			// Write a couple of jpeg header bytes (for no particular reason)
			var filePath1 = Path.Combine(tempPath1, fileName);
			File.WriteAllBytes(filePath1, new byte[] { 0xFF, 0xE0, 0x0, 0x0, 0x1 });
			pic.InternalPath = filePath1;
			sensePic1.PictureFileRA = pic;
			sensePic2.PictureFileRA = pic;

			var tempFolder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "ConfigDictPictureExportTest"));
			try
			{
				var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, true, true, tempFolder.FullName);
				//SUT
				var result = ConfiguredLcmGenerator.GenerateContentForEntry(testEntry, mainEntryNode, null, settings).ToString();
				var pictureRelativePath = Path.Combine("pictures", Path.GetFileName(fileName));
				const string pictureXPath = "/div[@class='lexentry']/span[@class='pictures']/span[@class='picture']/img";
				var pictureWithComposedPath = pictureXPath + "[contains(@src, '" + pictureRelativePath + "')]";
				if (!Platform.IsUnix)
					AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(pictureWithComposedPath, 2);
				else
					// that src contains a string, and escaping any Windows path separators
					AssertRegex(result, string.Format("src=\"[^\"]*{0}[^\"]*\"", pictureRelativePath.Replace(@"\", @"\\")), 2);
				const string guidXPath = pictureXPath + "[@id='g{0}']";
				AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(string.Format(guidXPath, sensePic1.Guid), 1);
				AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(string.Format(guidXPath, sensePic2.Guid), 1);
			}
			finally
			{
				SIL.IO.RobustIO.DeleteDirectoryAndContents(tempFolder.FullName);
				File.Delete(filePath1);
			}
		}

		[Test]
		public void GenerateContentForEntry_BadFileNameDoesNotCrash()
		{
			var mainEntryNode = CreatePictureModel();
			var testEntry = CreateInterestingLexEntry(Cache);
			var sense = testEntry.SensesOS[0];
			var sensePic = Cache.ServiceLocator.GetInstance<ICmPictureFactory>().Create();
			sense.PicturesOS.Add(sensePic);
			var pic = Cache.ServiceLocator.GetInstance<ICmFileFactory>().Create();
			var folder = Cache.ServiceLocator.GetInstance<ICmFolderFactory>().Create();
			Cache.LangProject.MediaOC.Add(folder);
			folder.FilesOC.Add(pic);
			var fileName = ".G.Images\\Marine images\\Cephalopholis leopardus.jpg;1.5\"; 1\";JPG";
			pic.InternalPath = fileName;
			sensePic.PictureFileRA = pic;

			var tempFolder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "ConfigDictPictureExportTest"));
			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, true, true, tempFolder.FullName);
			//SUT
			Assert.DoesNotThrow(() => ConfiguredLcmGenerator.GenerateContentForEntry(testEntry, mainEntryNode, null, settings));
		}

		[Test]
		public void GenerateContentForEntry_NullFilePathDoesNotCrash()
		{
			var mainEntryNode = CreatePictureModel();
			var testEntry = CreateInterestingLexEntry(Cache);
			var sense = testEntry.SensesOS[0];
			var sensePic = Cache.ServiceLocator.GetInstance<ICmPictureFactory>().Create();
			sense.PicturesOS.Add(sensePic);
			var pic = Cache.ServiceLocator.GetInstance<ICmFileFactory>().Create();
			var folder = Cache.ServiceLocator.GetInstance<ICmFolderFactory>().Create();
			Cache.LangProject.MediaOC.Add(folder);
			folder.FilesOC.Add(pic);
			pic.InternalPath = string.Empty;
			sensePic.PictureFileRA = pic;

			var tempFolder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "ConfigDictPictureExportTest"));
			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, true, true, tempFolder.FullName);
			//SUT
			Assert.DoesNotThrow(() => ConfiguredLcmGenerator.GenerateContentForEntry(testEntry, mainEntryNode, null, settings));
		}

		[Test]
		public void GenerateContentForEntry_NullInternalPathDoesNotCrash()
		{
			var thumbNailNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "PictureFileRA",
				CSSClassNameOverride = "picture"
			};
			var pictureNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "PicturesOfSenses",
				CSSClassNameOverride = "Pictures",
				Children = new List<ConfigurableDictionaryNode> { thumbNailNode }
			};
			var pronunciationsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "PronunciationsOS",
				CSSClassNameOverride = "pronunciations",
				Children = new List<ConfigurableDictionaryNode> { CreateMediaNode() }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { pronunciationsNode, pictureNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var entry = CreateInterestingLexEntry(Cache);

			// Create a folder in the project to hold the media files
			var folder = Cache.ServiceLocator.GetInstance<ICmFolderFactory>().Create();
			Cache.LangProject.MediaOC.Add(folder);

			var pron1 = Cache.ServiceLocator.GetInstance<ILexPronunciationFactory>().Create();
			entry.PronunciationsOS.Add(pron1);
			CreateTestMediaFile(Cache, null, folder, pron1);

			AddSenseToEntry(entry, "second", m_wsEn, Cache);
			var sense = entry.SensesOS[0];
			var sensePic = Cache.ServiceLocator.GetInstance<ICmPictureFactory>().Create();
			sense.PicturesOS.Add(sensePic);
			var pic = Cache.ServiceLocator.GetInstance<ICmFileFactory>().Create();
			Cache.LangProject.MediaOC.Add(folder);
			folder.FilesOC.Add(pic);
			sensePic.PictureFileRA = pic;

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);

			//SUT
			Assert.DoesNotThrow(() => ConfiguredLcmGenerator.GenerateContentForEntry(entry, mainEntryNode, null, settings));
		}

		[Test]
		public void GenerateContentForEntry_TwoDifferentLinksToTheSamefileWorks()
		{
			var mainEntryNode = CreatePictureModel();
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
				var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, true, true, tempFolder.FullName);
				//SUT
				var result = ConfiguredLcmGenerator.GenerateContentForEntry(testEntry, mainEntryNode, null, settings).ToString();
				var pictureRelativePath = Path.Combine("pictures", Path.GetFileName(fileName));
				var pictureWithComposedPath = "/div[@class='lexentry']/span[@class='pictures']/span[@class='picture']/img[contains(@src, '" + pictureRelativePath + "')]";
				if (!Platform.IsUnix)
					AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(pictureWithComposedPath, 2);
				// that src starts with string, and escaping Windows directory separators
				AssertRegex(result, string.Format("src=\"{0}[^\"]*\"", pictureRelativePath.Replace(@"\", @"\\")), 2);
				// The second file reference should not have resulted in a copy
				Assert.AreEqual(Directory.EnumerateFiles(Path.Combine(tempFolder.FullName, "pictures")).Count(), 1, "Wrong number of pictures copied.");
			}
			finally
			{
				IO.RobustIO.DeleteDirectoryAndContents(tempFolder.FullName);
				File.Delete(fileName);
			}
		}

		[Test]
		public void GenerateContentForEntry_StringCustomFieldGeneratesContent()
		{
			using (var customField = new CustomFieldForTest(Cache, "CustomString", Cache.MetaDataCacheAccessor.GetClassId("LexEntry"), 0,
			 CellarPropertyType.String, Guid.Empty))
			{
				var customFieldNode = new ConfigurableDictionaryNode
				{
					FieldDescription = "CustomString",
					IsCustomField = true
				};
				var mainEntryNode = new ConfigurableDictionaryNode
				{
					Children = new List<ConfigurableDictionaryNode> { customFieldNode },
					FieldDescription = "LexEntry"
				};
				CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
				var testEntry = CreateInterestingLexEntry(Cache);
				const string customData = @"I am custom data";
				var wsEn = Cache.WritingSystemFactory.GetWsFromStr("en");

				// Set custom field data
				Cache.MainCacheAccessor.SetString(testEntry.Hvo, customField.Flid, TsStringUtils.MakeString(customData, wsEn));
				var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
				//SUT
				var result = ConfiguredLcmGenerator.GenerateContentForEntry(testEntry, mainEntryNode, null, settings).ToString();
				var customDataPath = string.Format("/div[@class='lexentry']/span[@class='customstring']/span[text()='{0}']", customData);
				AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(customDataPath, 1);
			}
		}

		[Test]
		public void GenerateContentForEntry_CustomFieldInGroupingNodeGeneratesContent()
		{
			using (var customField = new CustomFieldForTest(Cache, "CustomString", Cache.MetaDataCacheAccessor.GetClassId("LexEntry"), 0,
				CellarPropertyType.String, Guid.Empty))
			{
				var customFieldNode = new ConfigurableDictionaryNode
				{
					FieldDescription = "CustomString",
					IsCustomField = true
				};
				var groupingNode = new ConfigurableDictionaryNode
				{
					FieldDescription = "CustomGroup",
					Children = new List<ConfigurableDictionaryNode> { customFieldNode },
					DictionaryNodeOptions = new DictionaryNodeGroupingOptions()
				};
				var mainEntryNode = new ConfigurableDictionaryNode
				{
					Children = new List<ConfigurableDictionaryNode> { groupingNode },
					FieldDescription = "LexEntry"
				};
				CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
				var testEntry = CreateInterestingLexEntry(Cache);
				const string customData = "I am custom data";
				var wsEn = Cache.WritingSystemFactory.GetWsFromStr("en");

				// Set custom field data
				Cache.MainCacheAccessor.SetString(testEntry.Hvo, customField.Flid, TsStringUtils.MakeString(customData, wsEn));
				var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
				//SUT
				var result = ConfiguredLcmGenerator.GenerateContentForEntry(testEntry, mainEntryNode, null, settings).ToString();
				var customDataPath = $"/div[@class='lexentry']/span[@class='grouping_customgroup']/span[@class='customstring']/span[text()='" + customData + "']";
				AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(customDataPath, 1);
			}
		}

		[Test]
		public void GenerateContentForEntry_CustomFieldInNestedGroupingNodeGeneratesContent()
		{
			using (var customField = new CustomFieldForTest(Cache, "CustomString", Cache.MetaDataCacheAccessor.GetClassId("LexEntry"), 0,
				CellarPropertyType.String, Guid.Empty))
			{
				var customFieldNode = new ConfigurableDictionaryNode
				{
					FieldDescription = "CustomString",
					IsCustomField = true
				};
				var groupingNode3 = new ConfigurableDictionaryNode
				{
					FieldDescription = "CustomGroup",
					Children = new List<ConfigurableDictionaryNode> { customFieldNode },
					DictionaryNodeOptions = new DictionaryNodeGroupingOptions()
				};
				var groupingNode2 = new ConfigurableDictionaryNode
				{
					FieldDescription = "CustomGroup",
					Children = new List<ConfigurableDictionaryNode> { groupingNode3 },
					DictionaryNodeOptions = new DictionaryNodeGroupingOptions()
				};
				var groupingNode1 = new ConfigurableDictionaryNode
				{
					FieldDescription = "CustomGroup",
					Children = new List<ConfigurableDictionaryNode> { groupingNode2 },
					DictionaryNodeOptions = new DictionaryNodeGroupingOptions()
				};
				var mainEntryNode = new ConfigurableDictionaryNode
				{
					Children = new List<ConfigurableDictionaryNode> { groupingNode1 },
					FieldDescription = "LexEntry"
				};
				CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
				var testEntry = CreateInterestingLexEntry(Cache);
				const string customData = "This is custom data";
				var wsEn = Cache.WritingSystemFactory.GetWsFromStr("en");

				// Set custom field data
				Cache.MainCacheAccessor.SetString(testEntry.Hvo, customField.Flid, TsStringUtils.MakeString(customData, wsEn));
				var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
				//SUT
				var result = ConfiguredLcmGenerator.GenerateContentForEntry(testEntry, mainEntryNode, null, settings).ToString();
				const string grpXPath = "/span[@class='grouping_customgroup']";
				var customDataPath = $"/div[@class='lexentry']{grpXPath}{grpXPath}{grpXPath}/span[@class='customstring']/span[text()='{customData}']";
				AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(customDataPath, 1);
			}
		}

		[Test]
		public void GenerateContentForEntry_GetPropertyTypeForConfigurationNode_StringCustomFieldIsPrimitive()
		{
			using (var customField = new CustomFieldForTest(Cache, "CustomString", Cache.MetaDataCacheAccessor.GetClassId("LexEntry"), 0,
			 CellarPropertyType.String, Guid.Empty))
			{
				var customFieldNode = new ConfigurableDictionaryNode
				{
					FieldDescription = "CustomString",
					IsCustomField = true
				};
				var mainEntryNode = new ConfigurableDictionaryNode
				{
					Children = new List<ConfigurableDictionaryNode> { customFieldNode },
					FieldDescription = "LexEntry"
				};
				CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
				var testEntry = CreateInterestingLexEntry(Cache);
				const string customData = @"I am custom data";
				var wsEn = Cache.WritingSystemFactory.GetWsFromStr("en");

				// Set custom field data
				Cache.MainCacheAccessor.SetString(testEntry.Hvo, customField.Flid, TsStringUtils.MakeString(customData, wsEn));
				//SUT
				Assert.AreEqual(ConfiguredLcmGenerator.PropertyType.PrimitiveType, ConfiguredLcmGenerator.GetPropertyTypeForConfigurationNode(customFieldNode, Cache));
			}
		}

		[Test]
		public void GenerateContentForEntry_StringCustomFieldOnSenseGeneratesContent()
		{
			using (var customField = new CustomFieldForTest(Cache, "CustomString", Cache.MetaDataCacheAccessor.GetClassId("LexSense"), 0,
			 CellarPropertyType.String, Guid.Empty))
			{
				var customFieldNode = new ConfigurableDictionaryNode
				{
					FieldDescription = "CustomString",
					IsCustomField = true
				};
				var senseNode = new ConfigurableDictionaryNode
				{
					FieldDescription = "SensesOS",
					Children = new List<ConfigurableDictionaryNode> { customFieldNode },
					CSSClassNameOverride = "es"
				};
				var mainEntryNode = new ConfigurableDictionaryNode
				{
					Children = new List<ConfigurableDictionaryNode> { senseNode },
					FieldDescription = "LexEntry",
					CSSClassNameOverride = "l"
				};
				CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
				var testEntry = CreateInterestingLexEntry(Cache);
				const string customData = @"I am custom sense data";
				var wsEn = Cache.WritingSystemFactory.GetWsFromStr("en");
				var testSence = testEntry.SensesOS[0];

				// Set custom field data
				Cache.MainCacheAccessor.SetString(testSence.Hvo, customField.Flid, TsStringUtils.MakeString(customData, wsEn));
				var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
				//SUT
				var result = ConfiguredLcmGenerator.GenerateContentForEntry(testEntry, mainEntryNode, null, settings).ToString();
				var customDataPath = string.Format("/div[@class='l']/span[@class='es']/span[@class='e']/span[@class='customstring']/span[text()='{0}']", customData);
				AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(customDataPath, 1);
			}
		}

		[Test]
		public void GenerateContentForEntry_StringCustomFieldOnExampleGeneratesContent()
		{
			using (var customField = new CustomFieldForTest(Cache, "CustomString", Cache.MetaDataCacheAccessor.GetClassId("LexExampleSentence"), 0,
			 CellarPropertyType.String, Guid.Empty))
			{
				var customFieldNode = new ConfigurableDictionaryNode
				{
					FieldDescription = "CustomString",
					IsCustomField = true
				};
				var exampleNode = new ConfigurableDictionaryNode
				{
					FieldDescription = "ExamplesOS",
					Children = new List<ConfigurableDictionaryNode> { customFieldNode },
					CSSClassNameOverride = "xs"
				};
				var senseNode = new ConfigurableDictionaryNode
				{
					FieldDescription = "SensesOS",
					Children = new List<ConfigurableDictionaryNode> { exampleNode },
					CSSClassNameOverride = "es"
				};
				var mainEntryNode = new ConfigurableDictionaryNode
				{
					Children = new List<ConfigurableDictionaryNode> { senseNode },
					FieldDescription = "LexEntry",
					CSSClassNameOverride = "l"
				};
				CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
				var testEntry = CreateInterestingLexEntry(Cache);
				const string customData = @"I am custom example data";
				var wsEn = Cache.WritingSystemFactory.GetWsFromStr("en");
				var testSense = testEntry.SensesOS[0];
				var exampleSentence = AddExampleToSense(testSense, @"I'm an example", Cache, m_wsFr, m_wsEn);

				// Set custom field data
				Cache.MainCacheAccessor.SetString(exampleSentence.Hvo, customField.Flid, TsStringUtils.MakeString(customData, wsEn));
				var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
				//SUT
				var result = ConfiguredLcmGenerator.GenerateContentForEntry(testEntry, mainEntryNode, null, settings).ToString();
				var customDataPath = string.Format(
					"/div[@class='l']/span[@class='es']//span[@class='xs']/span[@class='x']/span[@class='customstring']/span[text()='{0}']", customData);
				AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(customDataPath, 1);
			}
		}

		[Test]
		public void GenerateContentForEntry_StringCustomFieldOnAllomorphGeneratesContent()
		{
			using (var customField = new CustomFieldForTest(Cache, "CustomString", Cache.MetaDataCacheAccessor.GetClassId("MoForm"), 0,
			 CellarPropertyType.String, Guid.Empty))
			{
				var customFieldNode = new ConfigurableDictionaryNode
				{
					FieldDescription = "CustomString",
					IsCustomField = true
				};
				var senseNode = new ConfigurableDictionaryNode
				{
					FieldDescription = "AlternateFormsOS",
					Children = new List<ConfigurableDictionaryNode> { customFieldNode },
					CSSClassNameOverride = "as"
				};
				var mainEntryNode = new ConfigurableDictionaryNode
				{
					Children = new List<ConfigurableDictionaryNode> { senseNode },
					FieldDescription = "LexEntry",
					CSSClassNameOverride = "l"
				};
				CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
				var testEntry = CreateInterestingLexEntry(Cache);
				const string customData = @"I am custom morph data";
				var allomorph = AddAllomorphToEntry(testEntry);

				// Set custom field data
				Cache.MainCacheAccessor.SetString(allomorph.Hvo, customField.Flid, TsStringUtils.MakeString(customData, m_wsEn));
				var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
				//SUT
				var result = ConfiguredLcmGenerator.GenerateContentForEntry(testEntry, mainEntryNode, null, settings).ToString();
				var customDataPath = string.Format(
					"/div[@class='l']/span[@class='as']/span[@class='a']/span[@class='customstring']/span[text()='{0}']", customData);
				AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(customDataPath, 1);
			}
		}

		[Test]
		public void GenerateContentForEntry_MultiStringCustomFieldGeneratesContent()
		{
			using (var customField = new CustomFieldForTest(Cache, "CustomString", Cache.MetaDataCacheAccessor.GetClassId("LexEntry"), 0,
			 CellarPropertyType.MultiString, Guid.Empty))
			{
				var customFieldNode = new ConfigurableDictionaryNode
				{
					FieldDescription = "CustomString",
					IsCustomField = true,
					DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" })
				};
				var mainEntryNode = new ConfigurableDictionaryNode
				{
					Children = new List<ConfigurableDictionaryNode> { customFieldNode },
					FieldDescription = "LexEntry"
				};
				CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
				var testEntry = CreateInterestingLexEntry(Cache);
				const string customData = @"I am custom data";

				// Set custom field data
				Cache.MainCacheAccessor.SetMultiStringAlt(testEntry.Hvo, customField.Flid, m_wsEn, TsStringUtils.MakeString(customData, m_wsEn));
				var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
				//SUT
				var result = ConfiguredLcmGenerator.GenerateContentForEntry(testEntry, mainEntryNode, null, settings).ToString();
				var customDataPath = string.Format("/div[@class='lexentry']/span[@class='customstring']/span[text()='{0}']", customData);
				AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(customDataPath, 1);
			}
		}

		[Test]
		public void GenerateContentForEntry_CustomFieldOnISenseOrEntryGeneratesContentForEntry()
		{
			var entryCustom = new ConfigurableDictionaryNode
			{
				FieldDescription = "EntryCString",
				IsCustomField = true,
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" })
			};
			var senseCustom = new ConfigurableDictionaryNode
			{
				FieldDescription = "SenseCString",
				IsCustomField = true,
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" })
			};
			var targets = new ConfigurableDictionaryNode
			{
				FieldDescription = "ConfigTargets",
				Children = new List<ConfigurableDictionaryNode> { entryCustom, senseCustom }
			};
			var crossRefs = new ConfigurableDictionaryNode
			{
				FieldDescription = "MinimalLexReferences",
				CSSClassNameOverride = "mlrs",
				Children = new List<ConfigurableDictionaryNode> { targets }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { crossRefs }
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			const string refType = "SomeType";
			using (var entryCustomField = new CustomFieldForTest(Cache, "EntryCString", Cache.MetaDataCacheAccessor.GetClassId("LexEntry"), 0,
				CellarPropertyType.MultiString, Guid.Empty))
			using (var senseCustomField = new CustomFieldForTest(Cache, "SenseCString", Cache.MetaDataCacheAccessor.GetClassId("LexSense"), 0,
				CellarPropertyType.MultiString, Guid.Empty))
			{
				var testEntry = CreateInterestingLexEntry(Cache);
				var refdEntry = CreateInterestingLexEntry(Cache);
				CreateLexicalReference(Cache, testEntry, refdEntry, refType);
				var lexrefType = Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.First(
						r => r.Name.BestAnalysisAlternative.Text == refType);
				crossRefs.DictionaryNodeOptions = new DictionaryNodeListOptions
				{
					ListId = DictionaryNodeListOptions.ListIds.Entry,
					Options = DictionaryDetailsControllerTests.ListOfEnabledDNOsFromStrings(new[] { lexrefType.Guid.ToString() })
				};
				const string entryCustomData = "Another custom string";
				const string senseCustomData = "My custom string";
				Cache.MainCacheAccessor.SetMultiStringAlt(refdEntry.Hvo, entryCustomField.Flid, m_wsEn,
					TsStringUtils.MakeString(entryCustomData, m_wsEn));
				Cache.MainCacheAccessor.SetMultiStringAlt(refdEntry.SensesOS[0].Hvo, senseCustomField.Flid, m_wsEn,
					TsStringUtils.MakeString(senseCustomData, m_wsEn));
				var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
				//SUT
				var result = ConfiguredLcmGenerator.GenerateContentForEntry(testEntry, mainEntryNode, null, settings).ToString();
				var entryDataPath = string.Format("/div[@class='lexentry']/span[@class='mlrs']/span[@class='mlr']/span[@class='configtargets']/span[@class='configtarget']/span[@class='entrycstring']/span[text()='{0}']", entryCustomData);
				AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(entryDataPath, 1);
				var senseDataPath = string.Format("/div[@class='lexentry']/span[@class='mlrs']/span[@class='mlr']/span[@class='configtargets']/span[@class='configtarget']/span[@class='sensecstring']/span[text()='{0}']", senseCustomData);
				AssertThatXmlIn.String(result).HasNoMatchForXpath(senseDataPath, message: "Ref is to Entry; should be no Sense Custom Data");
			}
		}

		[Test]
		public void GenerateContentForEntry_CustomFieldOnISenseOrEntryGeneratesContentForSense()
		{
			var entryCustom = new ConfigurableDictionaryNode
			{
				FieldDescription = "EntryCString",
				IsCustomField = true,
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" })
			};
			var senseCustom = new ConfigurableDictionaryNode
			{
				FieldDescription = "SenseCString",
				IsCustomField = true,
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" })
			};
			var targets = new ConfigurableDictionaryNode
			{
				FieldDescription = "ConfigTargets",
				Children = new List<ConfigurableDictionaryNode> { entryCustom, senseCustom }
			};
			var crossRefs = new ConfigurableDictionaryNode
			{
				FieldDescription = "MinimalLexReferences",
				CSSClassNameOverride = "mlrs",
				Children = new List<ConfigurableDictionaryNode> { targets }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { crossRefs }
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			const string refType = "SomeType";
			using (var entryCustomField = new CustomFieldForTest(Cache, "EntryCString", Cache.MetaDataCacheAccessor.GetClassId("LexEntry"), 0,
				CellarPropertyType.MultiString, Guid.Empty))
			using (var senseCustomField = new CustomFieldForTest(Cache, "SenseCString", Cache.MetaDataCacheAccessor.GetClassId("LexSense"), 0,
				CellarPropertyType.MultiString, Guid.Empty))
			{
				var testEntry = CreateInterestingLexEntry(Cache);
				var refdEntry = CreateInterestingLexEntry(Cache);
				CreateLexicalReference(Cache, testEntry, refdEntry.SensesOS[0], refType);
				var lexrefType = Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.First(
						r => r.Name.BestAnalysisAlternative.Text == refType);
				crossRefs.DictionaryNodeOptions = new DictionaryNodeListOptions
				{
					ListId = DictionaryNodeListOptions.ListIds.Entry,
					Options = DictionaryDetailsControllerTests.ListOfEnabledDNOsFromStrings(new[] { lexrefType.Guid.ToString() })
				};
				const string entryCustomData = "Another custom string";
				const string senseCustomData = "My custom string";
				Cache.MainCacheAccessor.SetMultiStringAlt(refdEntry.Hvo, entryCustomField.Flid, m_wsEn,
					TsStringUtils.MakeString(entryCustomData, m_wsEn));
				Cache.MainCacheAccessor.SetMultiStringAlt(refdEntry.SensesOS[0].Hvo, senseCustomField.Flid, m_wsEn,
					TsStringUtils.MakeString(senseCustomData, m_wsEn));
				var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
				//SUT
				var result = ConfiguredLcmGenerator.GenerateContentForEntry(testEntry, mainEntryNode, null, settings).ToString();
				var entryDataPath = string.Format("/div[@class='lexentry']/span[@class='mlrs']/span[@class='mlr']/span[@class='configtargets']/span[@class='configtarget']/span[@class='entrycstring']/span[text()='{0}']", entryCustomData);
				AssertThatXmlIn.String(result).HasNoMatchForXpath(entryDataPath, message: "Ref is to Sense; should be no Entry Custom Data");
				var senseDataPath = string.Format("/div[@class='lexentry']/span[@class='mlrs']/span[@class='mlr']/span[@class='configtargets']/span[@class='configtarget']/span[@class='sensecstring']/span[text()='{0}']", senseCustomData);
				AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(senseDataPath, 1);
			}
		}

		[Test]
		public void GenerateContentForEntry_CustomFieldOnRefdLexEntryGeneratesContent()
		{
			var customConfig = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwningEntry",
				SubField = "CustomString",
				IsCustomField = true,
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" })
			};
			var crossRefs = new ConfigurableDictionaryNode
			{
				FieldDescription = "VariantFormEntryBackRefs",
				CSSClassNameOverride = "vars",
				Children = new List<ConfigurableDictionaryNode> { customConfig }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { crossRefs }
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			using (var customField = new CustomFieldForTest(Cache, "CustomString", Cache.MetaDataCacheAccessor.GetClassId("LexEntry"), 0,
				CellarPropertyType.MultiString, Guid.Empty))
			{
				var testEntry = CreateInterestingLexEntry(Cache);
				var refdEntry = CreateInterestingLexEntry(Cache);
				CreateVariantForm(Cache, testEntry, refdEntry);
				const string customData = "My custom string";
				Cache.MainCacheAccessor.SetMultiStringAlt(refdEntry.Hvo, customField.Flid, m_wsEn, TsStringUtils.MakeString(customData, m_wsEn));
				var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
				//SUT
				var result = ConfiguredLcmGenerator.GenerateContentForEntry(testEntry, mainEntryNode, null, settings).ToString();
				var customDataPath = string.Format("/div[@class='lexentry']/span[@class='vars']/span[@class='var']/span[@class='owningentry_customstring']/span[text()='{0}']", customData);
				AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(customDataPath, 1);
			}
		}

		[Test]
		public void GenerateContentForEntry_MultiStringDefinition_GeneratesMultilingualSpans()
		{
			var definitionNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Definition",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "analysis" })
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions(),
				Children = new List<ConfigurableDictionaryNode> { definitionNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { sensesNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);

			// Setup 1st Sense with multilingual Tss string
			var testEntry = CreateInterestingLexEntry(Cache);
			var multirunContent = new[] { "This definition includes ", "chat ", "and, ", "chien." };
			var defn = MakeMulitlingualTss(multirunContent);
			testEntry.SensesOS[0].Definition.set_String(m_wsEn, defn);

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(testEntry, mainEntryNode, null, settings).ToString();
			var definitionXpath = "//div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense']/span[@class='definition']/span[@lang='en']";
			var str1Xpath = string.Format(definitionXpath + "/span[@lang='en' and text()='{0}']", multirunContent[0]);
			var str2Xpath = string.Format(definitionXpath + "/span[@lang='fr' and text()='{0}']", multirunContent[1]);
			var str3Xpath = string.Format(definitionXpath + "/span[@lang='en' and text()='{0}']", multirunContent[2]);
			var str4Xpath = string.Format(definitionXpath + "/span[@lang='fr' and text()='{0}']", multirunContent[3]);
			var str2BadXpath = string.Format(definitionXpath + "/span[@lang='en' and text()='{0}']", multirunContent[1]);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(definitionXpath + "/span", 4);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(str1Xpath, 1);
			AssertThatXmlIn.String(result).HasNoMatchForXpath(str2BadXpath);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(str2Xpath, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(str3Xpath, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(str4Xpath, 1);
		}

		[Test]
		public void GenerateContentForEntry_ListItemCustomFieldGeneratesContent()
		{
			var wsEn = Cache.WritingSystemFactory.GetWsFromStr("en");
			var possibilityItem = Cache.LanguageProject.LocationsOA.FindOrCreatePossibility("Djbuti", wsEn);
			possibilityItem.Name.set_String(wsEn, "Djbuti");

			using (var customField = new CustomFieldForTest(Cache, "CustomListItem", Cache.MetaDataCacheAccessor.GetClassId("LexEntry"), 0,
					CellarPropertyType.OwningAtomic, Cache.LanguageProject.LocationsOA.Guid))
			{
				var nameNode = new ConfigurableDictionaryNode
				{
					FieldDescription = "Name",
					DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" })
				};
				var customFieldNode = new ConfigurableDictionaryNode
				{
					FieldDescription = "CustomListItem",
					IsCustomField = true,
					Children = new List<ConfigurableDictionaryNode> { nameNode }
				};
				var mainEntryNode = new ConfigurableDictionaryNode
				{
					Children = new List<ConfigurableDictionaryNode> { customFieldNode },
					FieldDescription = "LexEntry"
				};
				CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
				var testEntry = CreateInterestingLexEntry(Cache);

				// Set custom field data
				Cache.MainCacheAccessor.SetObjProp(testEntry.Hvo, customField.Flid, possibilityItem.Hvo);
				var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
				//SUT
				var result = ConfiguredLcmGenerator.GenerateContentForEntry(testEntry, mainEntryNode, null, settings).ToString();
				const string customDataPath = "/div[@class='lexentry']/span[@class='customlistitem']/span[@class='name']/span[text()='Djbuti']";
				AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(customDataPath, 1);
			}
		}

		[Test]
		public void GenerateContentForEntry_MultiListItemCustomFieldGeneratesContent()
		{
			var wsEn = Cache.WritingSystemFactory.GetWsFromStr("en");
			var possibilityItem1 = Cache.LanguageProject.LocationsOA.FindOrCreatePossibility("Dallas", wsEn);
			var possibilityItem2 = Cache.LanguageProject.LocationsOA.FindOrCreatePossibility("Barcelona", wsEn);
			possibilityItem1.Name.set_String(wsEn, "Dallas");
			possibilityItem2.Name.set_String(wsEn, "Barcelona");

			using (var customField = new CustomFieldForTest(Cache, "CustomListItems", Cache.MetaDataCacheAccessor.GetClassId("LexEntry"), 0,
					CellarPropertyType.ReferenceSequence, Cache.LanguageProject.LocationsOA.Guid))
			{
				var nameNode = new ConfigurableDictionaryNode
				{
					FieldDescription = "Name",
					DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" })
				};
				var customFieldNode = new ConfigurableDictionaryNode
				{
					FieldDescription = "CustomListItems",
					IsCustomField = true,
					Children = new List<ConfigurableDictionaryNode> { nameNode }
				};
				var mainEntryNode = new ConfigurableDictionaryNode
				{
					Children = new List<ConfigurableDictionaryNode> { customFieldNode },
					FieldDescription = "LexEntry"
				};
				CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
				var testEntry = CreateInterestingLexEntry(Cache);

				// Set custom field data
				Cache.MainCacheAccessor.Replace(testEntry.Hvo, customField.Flid, 0, 0, new[] { possibilityItem1.Hvo, possibilityItem2.Hvo }, 2);
				var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
				//SUT
				var result = ConfiguredLcmGenerator.GenerateContentForEntry(testEntry, mainEntryNode, null, settings).ToString();
				const string customDataPath1 = "/div[@class='lexentry']/span[@class='customlistitems']/span[@class='customlistitem']/span[@class='name']/span[text()='Dallas']";
				const string customDataPath2 = "/div[@class='lexentry']/span[@class='customlistitems']/span[@class='customlistitem']/span[@class='name']/span[text()='Barcelona']";
				AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(customDataPath1, 1);
				AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(customDataPath2, 1);
			}
		}

		[Test]
		public void GenerateContentForEntry_DateCustomFieldGeneratesContent()
		{
			using (var customField = new CustomFieldForTest(Cache, "CustomDate", Cache.MetaDataCacheAccessor.GetClassId("LexEntry"), 0,
			 CellarPropertyType.Time, Guid.Empty))
			{
				var customFieldNode = new ConfigurableDictionaryNode
				{
					FieldDescription = "CustomDate",
					IsCustomField = true
				};
				var mainEntryNode = new ConfigurableDictionaryNode
				{
					Children = new List<ConfigurableDictionaryNode> { customFieldNode },
					FieldDescription = "LexEntry"
				};
				CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
				var testEntry = CreateInterestingLexEntry(Cache);
				var customData = DateTime.Now;

				// Set custom field data
				SilTime.SetTimeProperty(Cache.MainCacheAccessor, testEntry.Hvo, customField.Flid, customData);
				var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
				//SUT
				var result = ConfiguredLcmGenerator.GenerateContentForEntry(testEntry, mainEntryNode, null, settings).ToString();
				var customDataPath = string.Format("/div[@class='lexentry']/span[@class='customdate' and text()='{0}']", customData.ToLongDateString());
				AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(customDataPath, 1);
			}
		}

		[Test]
		public void GenerateContentForEntry_IntegerCustomFieldGeneratesContent()
		{
			using (var customField = new CustomFieldForTest(Cache, "CustomInteger", Cache.MetaDataCacheAccessor.GetClassId("LexEntry"), 0,
			 CellarPropertyType.Integer, Guid.Empty))
			{
				var customFieldNode = new ConfigurableDictionaryNode
				{
					FieldDescription = "CustomInteger",
					IsCustomField = true
				};
				var mainEntryNode = new ConfigurableDictionaryNode
				{
					Children = new List<ConfigurableDictionaryNode> { customFieldNode },
					FieldDescription = "LexEntry"
				};
				CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
				var testEntry = CreateInterestingLexEntry(Cache);
				const int customData = 123456;

				// Set custom field data
				Cache.MainCacheAccessor.SetInt(testEntry.Hvo, customField.Flid, customData);
				var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
				//SUT
				var result = ConfiguredLcmGenerator.GenerateContentForEntry(testEntry, mainEntryNode, null, settings).ToString();
				var customDataPath = string.Format("/div[@class='lexentry']/span[@class='custominteger' and text()='{0}']", customData);
				AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(customDataPath, 1);
			}
		}

		[Test]
		public void GenerateContentForEntry_MultiLineCustomFieldGeneratesContent()
		{
			using (
				var customField = new CustomFieldForTest(Cache, "MultiplelineTest",
					Cache.MetaDataCacheAccessor.GetClassId("LexEntry"), 0,
					CellarPropertyType.OwningAtomic, Guid.Empty))
			{
				var memberNode = new ConfigurableDictionaryNode
				{
					FieldDescription = "MultiplelineTest",
					IsCustomField = true
				};
				var rootNode = new ConfigurableDictionaryNode
				{
					FieldDescription = "LexEntry",
					Children = new List<ConfigurableDictionaryNode> { memberNode }
				};
				CssGeneratorTests.PopulateFieldsForTesting(rootNode);
				var testEntry = CreateInterestingLexEntry(Cache);
				var text = CreateMultiParaText("Custom string", Cache);
				// SUT
				Cache.MainCacheAccessor.SetObjProp(testEntry.Hvo, customField.Flid, text.Hvo);
				var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
				//SUT
				var result = ConfiguredLcmGenerator.GenerateContentForEntry(testEntry, rootNode, null, settings).ToString();
				const string customDataPath =
					"/div[@class='lexentry']/div/span[text()='First para Custom string'] | /div[@class='lexentry']/div/span[text()='Second para Custom string']";
				AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(customDataPath, 2);
			}
		}

		[Test]
		public void GenerateContentForEntry_VariantOfReferencedHeadWord()
		{
			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "HeadWord",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};
			var refNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ConfigReferencedEntries",
				Children = new List<ConfigurableDictionaryNode> { headwordNode },
				CSSClassNameOverride = "referencedentries"
			};
			var variantsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VisibleVariantEntryRefs",
				Children = new List<ConfigurableDictionaryNode> { refNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { variantsNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);

			var mainEntry = CreateInterestingLexEntry(Cache);
			var variantForm = CreateInterestingLexEntry(Cache);
			CreateVariantForm(Cache, variantForm, mainEntry);
			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(mainEntry, mainEntryNode, null, settings).ToString();
			const string referencedEntries =
				"//span[@class='visiblevariantentryrefs']/span[@class='visiblevariantentryref']/span[@class='referencedentries']/span[@class='referencedentry']/span[@class='headword']/span[@lang='fr']/span[@lang='fr']";
			AssertThatXmlIn.String(result)
				.HasSpecifiedNumberOfMatchesForXpath(referencedEntries, 2);
		}

		[Test]
		public void GenerateContentForEntry_WsAudiowithHyperlink()
		{
			CoreWritingSystemDefinition wsEnAudio;
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("en-Zxxx-x-audio", out wsEnAudio);
			Cache.ServiceLocator.WritingSystems.AddToCurrentVernacularWritingSystems(wsEnAudio);
			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexemeFormOA",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en-Zxxx-x-audio" })
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { headwordNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var entryOne = CreateInterestingLexEntry(Cache);
			var senseaudio = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			entryOne.LexemeFormOA = senseaudio;
			const string audioFileName = "Test Audi'o.wav";
			senseaudio.Form.set_String(wsEnAudio.Handle, TsStringUtils.MakeString(audioFileName, wsEnAudio.Handle));
			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entryOne, mainEntryNode, null, settings).ToString();

			const string audioTagwithSource = "//audio/source/@src";
			AssertThatXmlIn.String(result)
				.HasSpecifiedNumberOfMatchesForXpath(audioTagwithSource, 1);
			var audioFileUrl =
				@"Src/xWorks/xWorksTests/TestData/LinkedFiles/AudioVisual/" + audioFileName;
			Assert.That(result, Contains.Substring(audioFileUrl));
			const string linkTagwithOnClick =
				"//span[@class='lexemeformoa']/span/a[@class='en-Zxxx-x-audio' and contains(@onclick,'play()')]";
			AssertThatXmlIn.String(result)
				.HasSpecifiedNumberOfMatchesForXpath(linkTagwithOnClick, 1);
		}

		/// <summary>
		/// Tests that during a web export the .wav file is automatically converted into an .mp3 file
		/// and saved in the destination file if the file does not already exist.
		/// </summary>
		/// <param name="isWebExport"> bool indicating if a web export is in progress </param>
		[Test]
		[TestCase(true)] //Is WebExport so the copied .wav file should be converted to an .mp3 file
		[TestCase(false)] //Is not a WebExport so the copied .wav file should remain a .wav file
		public void GenerateContentForEntry_AudioConversionDestinationDoesNotExist(bool isWebExport)
		{
			var pronunciationsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "PronunciationsOS",
				CSSClassNameOverride = "pronunciations",
				Children = new List<ConfigurableDictionaryNode> { CreateMediaNode() }
			};
			var variantPronunciationsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwningEntry",
				SubField = "PronunciationsOS",
				CSSClassNameOverride = "variantpronunciations",
				Children = new List<ConfigurableDictionaryNode> { CreateMediaNode() }
			};
			var variantFormTypeNameNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Name",
				IsEnabled = true,
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "analysis" })
			};
			var variantFormTypeNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VariantEntryTypesRS",
				CSSClassNameOverride = "variantentrytypes",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { variantFormTypeNameNode },
			};
			var variantFormsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VariantFormEntryBackRefs",
				DictionaryNodeOptions = GetFullyEnabledListOptions(DictionaryNodeListOptions.ListIds.Variant, Cache),
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { variantFormTypeNode, variantPronunciationsNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { pronunciationsNode, variantFormsNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var entry = CreateInterestingLexEntry(Cache);
			var variant = CreateInterestingLexEntry(Cache);
			// we need a real Variant Type to pass the list options test
			CreateVariantForm(Cache, entry, variant, "Spelling Variant");
			// Create a folder in the project to hold the media files
			var folder = Cache.ServiceLocator.GetInstance<ICmFolderFactory>().Create();
			Cache.LangProject.MediaOC.Add(folder);
			// Create and fill in the media files
			var pron1 = Cache.ServiceLocator.GetInstance<ILexPronunciationFactory>().Create();
			entry.PronunciationsOS.Add(pron1);
			var fileName1 = "abu2.wav";
			CreateTestMediaFile(Cache, fileName1, folder, pron1);

			// Use directories in using block so that they will be deleted even if the test fails
			using (var expectedMediaFolder = new TemporaryFolder(Path.GetRandomFileName()))
			{
				string expectedMediaFolderPath = expectedMediaFolder.Path;
				var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, true, true, expectedMediaFolderPath, false, isWebExport);
				settings.Cache.LangProject.LinkedFilesRootDir = expectedMediaFolderPath;

				// create a temp directory and copy a .wav file into it
				string destination = Path.Combine(expectedMediaFolderPath, "AudioVisual");
				Directory.CreateDirectory(destination);
				string path = Path.Combine(FwDirectoryFinder.SourceDirectory, "xWorks/xWorksTests/TestData/AudioFiles/abu2.wav");
				File.Copy(path, Path.Combine(destination, Path.GetFileName(path)), true);

				//SUT
				var result = ConfiguredLcmGenerator.GenerateContentForEntry(entry, mainEntryNode, null, settings).ToString();
				if (isWebExport)
				{
					Assert.That(result, Contains.Substring("abu2.mp3"), "The automatic audio conversion in the CopyFileSafely method failed");
				}
				else
				{
					Assert.That(result, Contains.Substring("abu2.wav"), "ConfiguredLcmGenerator.GenerateContentForEntry returned a string that did not include abu2.wav");
				}
			}
		}

		/// <summary>
		/// Tests that If an mp3 file with the same name as the destination exists and it has the contents the converted form
		/// of the .wav file should have the wav file is not converted or copied. This would only happen during a web export.
		/// </summary>
		/// <param name="isWebExport"> bool indicating if a web export is in progress </param>
		[Test]
		[TestCase(true)] //Is WebExport so the copied .wav file should be converted to an .mp3 file
		[TestCase(false)] //Is not a WebExport so the copied .wav file should remain a .wav file
		public void GenerateContentForEntry_AudioConversionIdenticalFileExists(bool isWebExport)
		{
			var pronunciationsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "PronunciationsOS",
				CSSClassNameOverride = "pronunciations",
				Children = new List<ConfigurableDictionaryNode> { CreateMediaNode() }
			};
			var variantPronunciationsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwningEntry",
				SubField = "PronunciationsOS",
				CSSClassNameOverride = "variantpronunciations",
				Children = new List<ConfigurableDictionaryNode> { CreateMediaNode() }
			};
			var variantFormTypeNameNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Name",
				IsEnabled = true,
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "analysis" })
			};
			var variantFormTypeNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VariantEntryTypesRS",
				CSSClassNameOverride = "variantentrytypes",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { variantFormTypeNameNode },
			};
			var variantFormsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VariantFormEntryBackRefs",
				DictionaryNodeOptions = GetFullyEnabledListOptions(DictionaryNodeListOptions.ListIds.Variant, Cache),
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { variantFormTypeNode, variantPronunciationsNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { pronunciationsNode, variantFormsNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var entry = CreateInterestingLexEntry(Cache);
			var variant = CreateInterestingLexEntry(Cache);
			// we need a real Variant Type to pass the list options test
			CreateVariantForm(Cache, entry, variant, "Spelling Variant");
			// Create a folder in the project to hold the media files
			var folder = Cache.ServiceLocator.GetInstance<ICmFolderFactory>().Create();
			Cache.LangProject.MediaOC.Add(folder);
			// Create and fill in the media files
			var pron1 = Cache.ServiceLocator.GetInstance<ILexPronunciationFactory>().Create();
			entry.PronunciationsOS.Add(pron1);
			var fileName1 = "abu2.wav";
			CreateTestMediaFile(Cache, fileName1, folder, pron1);

			// Use directories in using block so that they will be deleted even if the test fails
			using (var expectedMediaFolder = new TemporaryFolder(Path.GetRandomFileName()))
			{
				string expectedMediaFolderPath = expectedMediaFolder.Path;
				var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, true, true, expectedMediaFolderPath, false, isWebExport);
				settings.Cache.LangProject.LinkedFilesRootDir = expectedMediaFolderPath;
				string destination = Path.Combine(expectedMediaFolderPath, "AudioVisual");

				// create a temp directory and copy a .wav file into it
				Directory.CreateDirectory(destination);
				string path = Path.Combine(FwDirectoryFinder.SourceDirectory, "xWorks/xWorksTests/TestData/AudioFiles/abu2.wav");
				if (isWebExport)
					WavConverter.WavToMp3(path, Path.Combine(destination, "abu2.mp3"));
				File.Copy(path, Path.Combine(destination, Path.GetFileName(path)), true);

				//SUT
				var result = ConfiguredLcmGenerator.GenerateContentForEntry(entry, mainEntryNode, null, settings).ToString();
				if (isWebExport)
				{
					Assert.That(result, Contains.Substring("abu2.mp3"), "The automatic audio conversion in the CopyFileSafely method failed");
				}
				else
				{
					Assert.That(result, Contains.Substring("abu2.wav"), "ConfiguredLcmGenerator.GenerateContentForEntry returned a string that did not include abu2.wav");
				}
			}
		}

		/// <summary>
		/// Tests that If an mp3 file with the same name as the destination file exists, but has different contents than the converted
		/// form of the .wav file should have, then the wav file is converted and saved under a different name. This would only happen
		/// during a web export.
		/// </summary>
		/// <param name="isWebExport"> bool indicating if a web export is in progress </param>
		[Test]
		[TestCase(true)] //Is WebExport so the copied .wav file should be converted to an .mp3 file
		[TestCase(false)] //Is not a WebExport so the copied .wav file should remain a .wav file
		public void GenerateContentForEntry_AudioConversionNonIdenticalFileExists(bool isWebExport)
		{
			var pronunciationsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "PronunciationsOS",
				CSSClassNameOverride = "pronunciations",
				Children = new List<ConfigurableDictionaryNode> { CreateMediaNode() }
			};
			var variantPronunciationsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwningEntry",
				SubField = "PronunciationsOS",
				CSSClassNameOverride = "variantpronunciations",
				Children = new List<ConfigurableDictionaryNode> { CreateMediaNode() }
			};
			var variantFormTypeNameNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Name",
				IsEnabled = true,
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "analysis" })
			};
			var variantFormTypeNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VariantEntryTypesRS",
				CSSClassNameOverride = "variantentrytypes",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { variantFormTypeNameNode },
			};
			var variantFormsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VariantFormEntryBackRefs",
				DictionaryNodeOptions = GetFullyEnabledListOptions(DictionaryNodeListOptions.ListIds.Variant, Cache),
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { variantFormTypeNode, variantPronunciationsNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { pronunciationsNode, variantFormsNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var entry = CreateInterestingLexEntry(Cache);
			var variant = CreateInterestingLexEntry(Cache);
			// we need a real Variant Type to pass the list options test
			CreateVariantForm(Cache, entry, variant, "Spelling Variant");
			// Create a folder in the project to hold the media files
			var folder = Cache.ServiceLocator.GetInstance<ICmFolderFactory>().Create();
			Cache.LangProject.MediaOC.Add(folder);
			// Create and fill in the media files
			var pron1 = Cache.ServiceLocator.GetInstance<ILexPronunciationFactory>().Create();
			entry.PronunciationsOS.Add(pron1);
			var fileName1 = "abu2.wav";
			CreateTestMediaFile(Cache, fileName1, folder, pron1);

			// Use directories in using block so that they will be deleted even if the test fails
			using (var expectedMediaFolder = new TemporaryFolder(Path.GetRandomFileName()))
			{
				string expectedMediaFolderPath = expectedMediaFolder.Path;
				var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, true, true, expectedMediaFolderPath, false, isWebExport);
				settings.Cache.LangProject.LinkedFilesRootDir = expectedMediaFolderPath;
				string destination = Path.Combine(expectedMediaFolderPath, "AudioVisual");

				// create a temp directory and copy a .wav file into it
				Directory.CreateDirectory(destination);
				string path = Path.Combine(FwDirectoryFinder.SourceDirectory, "xWorks/xWorksTests/TestData/AudioFiles/abu2.wav");
				File.Copy(path, Path.Combine(destination, Path.GetFileName(path)), true);

				// create a fake file with the same name as the destination file but different content than the destination file should have after a conversion
				if (isWebExport)
				{
					string fakePath = Path.Combine(destination, "abu2.mp3");
					byte[] bytes = { 177, 209, 137, 61, 204, 127, 103, 88 };
					File.WriteAllBytes(fakePath, bytes);
				}

				//SUT
				var result = ConfiguredLcmGenerator.GenerateContentForEntry(entry, mainEntryNode, null, settings).ToString();
				if (isWebExport)
				{
					Assert.That(result, Contains.Substring("abu21.mp3"), "The automatic audio conversion code in the CopyFileSafely method did not change the file name as it should have since a file with the same name but different contents already exists");
				}
				else
				{
					Assert.That(result, Contains.Substring("abu2.wav"), "ConfiguredLcmGenerator.GenerateContentForEntry returned a string that did not include abu2.wav");
				}
			}
		}

		[Test]
		public void GenerateContentForEntry_WsAudiowithRelativePaths()
		{
			CoreWritingSystemDefinition wsEnAudio;
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("en-Zxxx-x-audio", out wsEnAudio);
			Cache.ServiceLocator.WritingSystems.AddToCurrentVernacularWritingSystems(wsEnAudio);
			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexemeFormOA",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en-Zxxx-x-audio" })
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { headwordNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var entryOne = CreateInterestingLexEntry(Cache);
			var senseaudio = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			entryOne.LexemeFormOA = senseaudio;
			const string audioFileName = "Test Audi'o.wav";
			senseaudio.Form.set_String(wsEnAudio.Handle, TsStringUtils.MakeString(audioFileName, wsEnAudio.Handle));
			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, true, true, "//audio/source/@src");
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entryOne, mainEntryNode, null, settings).ToString();

			const string safeAudioId = "gTest_Audi_o";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath("//audio[contains(@id," + safeAudioId + ")]", 1);
			const string audioTagwithSource = "//audio/source/@src";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(audioTagwithSource, 1);
			var audioFileUrl = Path.Combine("AudioVisual", audioFileName);
			Assert.That(result, Contains.Substring(audioFileUrl));
			var linkTagwithOnClick = "//span[@class='lexemeformoa']/span/a[@class='en-Zxxx-x-audio'";
			linkTagwithOnClick += " and @href='#" + safeAudioId + "'";
			linkTagwithOnClick += " and contains(@onclick,'" + safeAudioId + "') and contains(@onclick,'.play()')]";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(linkTagwithOnClick, 1);
		}

		[Test]
		public void GenerateContentForEntry_WsAudioCrashOnPrimarySelection()
		{
			CoreWritingSystemDefinition wsEn, wsEnAudio;
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("en-Zxxx-x-audio", out wsEnAudio);
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("en", out wsEn);

			try
			{
				//Ensure Audio ws should be First and Primary item
				Cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems.Clear();
				Cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems.Insert(0, wsEnAudio);
				Cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems.Insert(1, wsEn);

				Cache.ServiceLocator.WritingSystems.AnalysisWritingSystems.Clear();
				Cache.ServiceLocator.WritingSystems.AnalysisWritingSystems.Add(wsEnAudio);
				Cache.ServiceLocator.WritingSystems.AnalysisWritingSystems.Add(wsEn);

				var DictionaryNodeSenseOptions = new DictionaryNodeSenseOptions
				{
					ShowSharedGrammarInfoFirst = true
				};
				var categorynfo = new ConfigurableDictionaryNode
				{
					FieldDescription = "MLPartOfSpeech",
					Children = new List<ConfigurableDictionaryNode>(),
					DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "analysis" })
				};
				var gramInfoNode = new ConfigurableDictionaryNode
				{
					FieldDescription = "MorphoSyntaxAnalysisRA",
					CSSClassNameOverride = "msas",
					Children = new List<ConfigurableDictionaryNode> { categorynfo }
				};
				var sensesNode = new ConfigurableDictionaryNode
				{
					FieldDescription = "SensesOS",
					CSSClassNameOverride = "Senses",
					DictionaryNodeOptions = DictionaryNodeSenseOptions,
					Children = new List<ConfigurableDictionaryNode> { gramInfoNode }
				};
				var mainEntryNode = new ConfigurableDictionaryNode
				{
					Children = new List<ConfigurableDictionaryNode> { sensesNode },
					FieldDescription = "LexEntry"
				};

				CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
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

				var entryOne = CreateInterestingLexEntry(Cache);
				var senseaudio = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
				entryOne.LexemeFormOA = senseaudio;
				senseaudio.Form.set_String(wsEnAudio.Handle, TsStringUtils.MakeString("TestAudio.wav", wsEnAudio.Handle));
				var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, true, true, "//audio/source/@src");

				// SUT
				Assert.DoesNotThrow(() => ConfiguredLcmGenerator.GenerateContentForEntry(entry, mainEntryNode, null, settings), "Having an audio ws first should not cause crash.");
			}
			finally
			{
				//Remove the AudioWS from the Cache which was added in AnalysisWritingSystem for this test
				Cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems.Remove(wsEnAudio);
			}
		}

		[Test]
		public void GenerateContentForEntry_GeneratesComplexFormTypeForSubentryUnderSense()
		{
			var lexentry = CreateInterestingLexEntry(Cache);
			var lexsense = lexentry.SensesOS[0];

			var subentry = CreateInterestingLexEntry(Cache);
			var subentryRef = CreateComplexForm(Cache, lexsense, subentry, true);

			var complexRefAbbr = subentryRef.ComplexEntryTypesRS[0].Abbreviation.BestAnalysisAlternative.Text;
			var complexRefRevAbbr = subentryRef.ComplexEntryTypesRS[0].ReverseAbbr.BestAnalysisAlternative.Text;

			var revAbbrevNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ReverseAbbr",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" })
			};
			var refTypeNode = new ConfigurableDictionaryNode
			{
				FieldDescription = ConfiguredLcmGenerator.LookupComplexEntryType,
				CSSClassNameOverride = "complexformtypes",
				Children = new List<ConfigurableDictionaryNode> { revAbbrevNode }
			};
			var subentryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { refTypeNode },
				DictionaryNodeOptions = GetFullyEnabledListOptions(DictionaryNodeListOptions.ListIds.Complex, Cache),
				FieldDescription = "Subentries"
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { subentryNode },
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses"
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { sensesNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(lexentry, mainEntryNode, null, settings).ToString();
			var fwdNameXpath = string.Format(
				"//span[@class='sense']/span[@class='subentries']/span[@class='subentry']/span[@class='complexformtypes']/span[@class='complexformtype']/span/span[@lang='en' and text()='{0}']",
					complexRefAbbr);
			var revNameXpath = string.Format(
				"//span[@class='sense']/span[@class='subentries']/span[@class='subentry']/span[@class='complexformtypes']/span[@class='complexformtype']/span[@class='reverseabbr']/span[@lang='en' and text()='{0}']",
					complexRefRevAbbr);
			AssertThatXmlIn.String(result).HasNoMatchForXpath(fwdNameXpath);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(revNameXpath, 1);
		}

		[Test]
		public void GenerateContentForEntry_GeneratesComplexFormTypeForSubentry()
		{
			var lexentry = CreateInterestingLexEntry(Cache);

			var subentry = CreateInterestingLexEntry(Cache);
			var subentryRef = CreateComplexForm(Cache, lexentry, subentry, true);

			var complexRefAbbr = subentryRef.ComplexEntryTypesRS[0].Abbreviation.BestAnalysisAlternative.Text;
			var complexRefRevAbbr = subentryRef.ComplexEntryTypesRS[0].ReverseAbbr.BestAnalysisAlternative.Text;

			var revAbbrevNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ReverseAbbr",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" })
			};
			var refTypeNode = new ConfigurableDictionaryNode
			{
				FieldDescription = ConfiguredLcmGenerator.LookupComplexEntryType,
				CSSClassNameOverride = "complexformtypes",
				Children = new List<ConfigurableDictionaryNode> { revAbbrevNode }
			};
			var subentryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { refTypeNode },
				DictionaryNodeOptions = GetFullyEnabledListOptions(DictionaryNodeListOptions.ListIds.Complex, Cache),
				FieldDescription = "Subentries"
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { subentryNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(lexentry, mainEntryNode, null, settings).ToString();
			var fwdNameXpath = string.Format(
				"//span[@class='subentries']/span[@class='subentry']/span[@class='complexformtypes']/span[@class='complexformtype']/span/span[@lang='en' and text()='{0}']",
					complexRefAbbr);
			var revNameXpath = string.Format(
				"//span[@class='subentries']/span[@class='subentry']/span[@class='complexformtypes']/span[@class='complexformtype']/span[@class='reverseabbr']/span[@lang='en' and text()='{0}']",
					complexRefRevAbbr);
			AssertThatXmlIn.String(result).HasNoMatchForXpath(fwdNameXpath);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(revNameXpath, 1);
		}

		/// <param name="isUnderSense">
		/// Whether the subentry is under a sense of the main entry. We do *not* support subentries under senses of subentries.
		/// </param>
		[Test]
		public void GenerateContentForEntry_GeneratesComplexFormTypeForSubsubentry([Values(true, false)] bool isUnderSense)
		{
			var lexentry = CreateInterestingLexEntry(Cache);

			var subentry = CreateInterestingLexEntry(Cache);
			var otherComplexRefRevAbbr = CreateComplexForm(Cache, isUnderSense ? (ICmObject)lexentry : lexentry.SensesOS.First(), subentry, true, 2)
				.ComplexEntryTypesRS[0].ReverseAbbr.BestAnalysisAlternative.Text;

			var subsubentry = CreateInterestingLexEntry(Cache);
			var subsubentryRef = CreateComplexForm(Cache, subentry, subsubentry, true, 4);

			var complexRefAbbr = subsubentryRef.ComplexEntryTypesRS[0].Abbreviation.BestAnalysisAlternative.Text;
			var complexRefRevAbbr = subsubentryRef.ComplexEntryTypesRS[0].ReverseAbbr.BestAnalysisAlternative.Text;

			var subsubentryNode = new ConfigurableDictionaryNode
			{
				DictionaryNodeOptions = GetFullyEnabledListOptions(DictionaryNodeListOptions.ListIds.Complex, Cache),
				FieldDescription = "Subentries",
				ReferenceItem = "Subentries"
			};
			var revAbbrevNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ReverseAbbr",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" })
			};
			var refTypeNode = new ConfigurableDictionaryNode
			{
				FieldDescription = ConfiguredLcmGenerator.LookupComplexEntryType,
				CSSClassNameOverride = "complexformtypes",
				Children = new List<ConfigurableDictionaryNode> { revAbbrevNode }
			};
			var sharedSubentryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { refTypeNode, subsubentryNode },
				FieldDescription = "Subentries"
			};
			var subentryNode = new ConfigurableDictionaryNode
			{
				DictionaryNodeOptions = GetFullyEnabledListOptions(DictionaryNodeListOptions.ListIds.Complex, Cache),
				ReferenceItem = "Subentries",
				FieldDescription = "Subentries"
			};
			var senseNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				Children = new List<ConfigurableDictionaryNode> { subentryNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { senseNode, subentryNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(DictionaryConfigurationModelTests.CreateSimpleSharingModel(mainEntryNode, sharedSubentryNode));

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(lexentry, mainEntryNode, null, settings).ToString();
			const string fwdNameXpath =
				"//span[@class='subentries']/span[@class='subentry subentry']/span[@class='subentries']/span[@class='subentry subentry']"
				+ "/span[@class='complexformtypes']/span[@class='complexformtype']/span/span[@lang='en' and text()='{0}']";
			const string revNameXpath =
				"//span[@class='subentries']/span[@class='subentry subentry']/span[@class='subentries']/span[@class='subentry subentry']"
				+ "/span[@class='complexformtypes']/span[@class='complexformtype']/span[@class='reverseabbr']/span[@lang='en' and text()='{0}']";
			AssertThatXmlIn.String(result).HasNoMatchForXpath(string.Format(fwdNameXpath, complexRefAbbr));
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(string.Format(revNameXpath, complexRefRevAbbr), 1);
			AssertThatXmlIn.String(result).HasNoMatchForXpath(string.Format(revNameXpath, otherComplexRefRevAbbr),
				message: "should be confined to subentry");
		}

		[Test]
		public void GenerateContentForEntry_DoesntGeneratesComplexFormType_WhenDisabled()
		{
			var lexentry = CreateInterestingLexEntry(Cache);

			var subentry = CreateInterestingLexEntry(Cache);
			var subentryRef = CreateComplexForm(Cache, lexentry, subentry, true);

			var complexRefAbbr = subentryRef.ComplexEntryTypesRS[0].Abbreviation.BestAnalysisAlternative.Text;

			var revAbbrevNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ReverseAbbr",
				IsEnabled = true,
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" })
			};
			var refTypeNode = new ConfigurableDictionaryNode
			{
				FieldDescription = ConfiguredLcmGenerator.LookupComplexEntryType,
				IsEnabled = false,
				CSSClassNameOverride = "complexformtypes",
				Children = new List<ConfigurableDictionaryNode> { revAbbrevNode }
			};
			var subentryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { refTypeNode },
				DictionaryNodeOptions = new DictionaryNodeListAndParaOptions(),
				FieldDescription = "Subentries",
				IsEnabled = true
			};
			var headword = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLHeadWord",
				CSSClassNameOverride = "headword",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" }),
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode>()
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { headword, subentryNode },
				FieldDescription = "LexEntry",
				IsEnabled = true
			};
			DictionaryConfigurationModel.SpecifyParentsAndReferences(new List<ConfigurableDictionaryNode> { mainEntryNode });

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(lexentry, mainEntryNode, null, settings).ToString();
			const string refTypeXpath = "//span[@class='subentries']/span[@class='subentry']/span[@class='complexformtypes']";
			AssertThatXmlIn.String(result).HasNoMatchForXpath(refTypeXpath);
			StringAssert.DoesNotContain(complexRefAbbr, result);
		}

		[Test]
		public void GenerateContentForEntry_GeneratesComplexForm_WithEmptyList()
		{
			var lexentry = CreateInterestingLexEntry(Cache);
			var complexEntry = CreateInterestingLexEntry(Cache);
			var complexFormRef = CreateComplexForm(Cache, lexentry, complexEntry, false);
			complexFormRef.ComplexEntryTypesRS.Clear(); // no complex form type specified

			var formNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwningEntry",
				SubField = "MLHeadWord",
				CSSClassNameOverride = "headword",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};
			var complexTypeNameNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Name",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "analysis" })
			};
			var complexEntryTypeNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ComplexEntryTypesRS",
				CSSClassNameOverride = "complexformtypes",
				Children = new List<ConfigurableDictionaryNode> { complexTypeNameNode },
			};
			var complexOptions = GetFullyEnabledListOptions(DictionaryNodeListOptions.ListIds.Complex, Cache);
			var referencedCompFormNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { complexEntryTypeNode, formNode },
				DictionaryNodeOptions = complexOptions,
				FieldDescription = "VisibleComplexFormBackRefs"
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { referencedCompFormNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			Assert.DoesNotThrow(() => ConfiguredLcmGenerator.GenerateContentForEntry(lexentry, mainEntryNode, null, settings), "Having an empty complexentrytype list after the click event should not cause crash.");
		}

		[Test]
		public void GenerateContentForEntry_GeneratesComplexForm_NoTypeSpecified()
		{
			var lexentry = CreateInterestingLexEntry(Cache);
			var complexEntry = CreateInterestingLexEntry(Cache);
			var complexFormRef = CreateComplexForm(Cache, lexentry, complexEntry, false);
			complexFormRef.ComplexEntryTypesRS.Clear(); // no complex form type specified

			var formNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwningEntry",
				SubField = "MLHeadWord",
				CSSClassNameOverride = "headword",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};
			var complexEntryTypeNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ComplexEntryTypesRS",
				CSSClassNameOverride = "complexformtypes",
			};
			var complexOptions = GetFullyEnabledListOptions(DictionaryNodeListOptions.ListIds.Complex, Cache);
			var referencedCompFormNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { complexEntryTypeNode, formNode },
				DictionaryNodeOptions = complexOptions,
				FieldDescription = "VisibleComplexFormBackRefs"
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { referencedCompFormNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(lexentry, mainEntryNode, null, settings).ToString();
			const string refTypeXpath = "//span[@class='visiblecomplexformbackrefs']/span[@class='visiblecomplexformbackref']/span[@class='complexformtypes']/span[@class='complexformtype']";
			AssertThatXmlIn.String(result).HasNoMatchForXpath(refTypeXpath);
			const string headwordXpath = "//span[@class='visiblecomplexformbackrefs']/span[@class='visiblecomplexformbackref']/span[@class='headword']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(headwordXpath, 1);
		}

		[Test]
		public void GenerateContentForEntry_GeneratesSubentry_NoTypeSpecified()
		{
			var lexentry = CreateInterestingLexEntry(Cache);
			var subentry = CreateInterestingLexEntry(Cache);
			var subentryRef = CreateComplexForm(Cache, lexentry, subentry, true);
			subentryRef.ComplexEntryTypesRS.Clear(); // no complex form type specified

			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLHeadWord",
				CSSClassNameOverride = "headword",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};
			var refTypeNode = new ConfigurableDictionaryNode
			{
				FieldDescription = ConfiguredLcmGenerator.LookupComplexEntryType,
				CSSClassNameOverride = "complexformtypes",
			};
			var complexOptions = GetFullyEnabledListOptions(DictionaryNodeListOptions.ListIds.Complex, Cache);
			var subentryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { refTypeNode, headwordNode },
				DictionaryNodeOptions = complexOptions,
				FieldDescription = "Subentries"
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { subentryNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(lexentry, mainEntryNode, null, settings).ToString();
			const string refTypeXpath = "//span[@class='subentries']/span[@class='subentry']/span[@class='complexformtypes']/span[@class='complexformtype']";
			AssertThatXmlIn.String(result).HasNoMatchForXpath(refTypeXpath);
			const string headwordXpath = "//span[@class='subentries']/span[@class='subentry']/span[@class='headword']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(headwordXpath, 1);
		}

		// ComplexForm: Don't generate the reference if we are hiding minor entries AND we are publishing to Webonary.
		[Test]
		public void GenerateContentForEntry_ComplexFormDontGenerateReference()
		{
			var lexentry = CreateInterestingLexEntry(Cache);
			var subentry = CreateInterestingLexEntry(Cache);
			var subentryRef = CreateComplexForm(Cache, lexentry, subentry, true);

			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLHeadWord",
				CSSClassNameOverride = "headword",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};
			var refTypeNode = new ConfigurableDictionaryNode
			{
				FieldDescription = ConfiguredLcmGenerator.LookupComplexEntryType,
				CSSClassNameOverride = "complexformtypes",
			};
			var complexOptions = GetFullyEnabledListOptions(DictionaryNodeListOptions.ListIds.Complex, Cache);
			var subentryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { refTypeNode, headwordNode },
				DictionaryNodeOptions = complexOptions,
				FieldDescription = "Subentries"
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { subentryNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);

			subentryRef.HideMinorEntry = 1;
			const string withReference = "/div[@class='lexentry']/span[@class='subentries']/span[@class='subentry']/span[@class='headword']/span[@lang='fr']/span[@lang='fr']/a[@href]";
			const string withoutReference = "/div[@class='lexentry']/span[@class='subentries']/span[@class='subentry']/span[@class='headword']/span[@lang='fr']/span[@lang='fr']";

			// When hiding minor entries this should still generate the reference (if not publishing to Webonary).
			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null, false, false);
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(lexentry, mainEntryNode, null, settings).ToString();
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(withReference, 2);

			//SUT
			// When hiding minor entries and publishing to Webonary this should NOT generate the reference.
			settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null, false, true);
			result = ConfiguredLcmGenerator.GenerateContentForEntry(lexentry, mainEntryNode, null, settings).ToString();
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(withReference, 0);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(withoutReference, 2);
		}

		[Test]
		public void GenerateContentForEntry_GeneratesVariant_WithEmptyList()
		{
			var lexentry = CreateInterestingLexEntry(Cache);
			var variantEntry = CreateInterestingLexEntry(Cache);
			var variantEntryRef = CreateVariantForm(Cache, lexentry, variantEntry);
			variantEntryRef.VariantEntryTypesRS.Clear(); // no variant entry type specified

			var formNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwningEntry",
				SubField = "MLHeadWord",
				CSSClassNameOverride = "headword",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};
			var variantTypeNameNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Name",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "analysis" })
			};
			var variantEntryTypeNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VariantEntryTypesRS",
				CSSClassNameOverride = "variantentrytypes",
				Children = new List<ConfigurableDictionaryNode> { variantTypeNameNode },
			};
			var variantFormNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { variantEntryTypeNode, formNode },
				DictionaryNodeOptions = GetFullyEnabledListOptions(DictionaryNodeListOptions.ListIds.Variant, Cache),
				FieldDescription = "VariantFormEntryBackRefs"
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { variantFormNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			Assert.DoesNotThrow(() => ConfiguredLcmGenerator.GenerateContentForEntry(lexentry, mainEntryNode, null, settings), "Having an empty variantentrytype list after the click event should not cause crash.");
		}

		[Test]
		public void GenerateContentForEntry_GeneratesVariant_NoTypeSpecified()
		{
			var lexentry = CreateInterestingLexEntry(Cache);
			var variantEntry = CreateInterestingLexEntry(Cache);
			var variantEntryRef = CreateVariantForm(Cache, lexentry, variantEntry);
			variantEntryRef.VariantEntryTypesRS.Clear(); // no variant entry type specified

			var formNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwningEntry",
				SubField = "MLHeadWord",
				CSSClassNameOverride = "headword",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};

			var variantFormTypeNameNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Name",
				IsEnabled = true,
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "analysis" })
			};
			var variantEntryTypeNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VariantEntryTypesRS",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { variantFormTypeNameNode },
			};
			var variantFormNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { variantEntryTypeNode, formNode },
				DictionaryNodeOptions = GetFullyEnabledListOptions(DictionaryNodeListOptions.ListIds.Variant, Cache),
				IsEnabled = true,
				FieldDescription = "VariantFormEntryBackRefs"
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { variantFormNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(lexentry, mainEntryNode, null, settings).ToString();
			const string refTypeXpath = "//span[@class='variantformentrybackrefs']/span[@class='variantformentrybackref']/span[@class='variantentrytypesrs']/span[@class='variantentrytypesr']";
			AssertThatXmlIn.String(result).HasNoMatchForXpath(refTypeXpath);
			const string headwordXpath = "//span[@class='variantformentrybackrefs']/span[@class='variantformentrybackref']/span[@class='headword']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(headwordXpath, 1);
		}

		[Test]
		public void GenerateContentForEntry_VariantShowsIfNotHideMinorEntry_ViewDoesntMatter()
		{
			var lexentry = CreateInterestingLexEntry(Cache);
			var variantEntry = CreateInterestingLexEntry(Cache);
			var variantEntryRef = CreateVariantForm(Cache, lexentry, variantEntry);
			variantEntryRef.VariantEntryTypesRS[0] = Cache.LangProject.LexDbOA.VariantEntryTypesOA.PossibilitiesOS[0] as ILexEntryType;
			variantEntryRef.HideMinorEntry = 1; // This should hide a Variant no matter the view

			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLHeadWord",
				CSSClassNameOverride = "headword",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};
			var minorEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { headwordNode },
				DictionaryNodeOptions = GetFullyEnabledListOptions(DictionaryNodeListOptions.ListIds.Variant, Cache),
				FieldDescription = "LexEntry",
				Label = "Minor Entry (Variants)"
			};
			var model = new DictionaryConfigurationModel
			{
				Parts = new List<ConfigurableDictionaryNode> { new ConfigurableDictionaryNode(), minorEntryNode }, // dummy main entry node
				IsRootBased = false
			};
			CssGeneratorTests.PopulateFieldsForTesting(model);

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(variantEntry, model, null, settings).ToString();
			Assert.IsEmpty(result);
			// try with HideMinorEntry off
			variantEntryRef.HideMinorEntry = 0;
			result = ConfiguredLcmGenerator.GenerateContentForEntry(variantEntry, model, null, settings).ToString();
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath("/div[@class='lexentry']/span[@class='headword']", 1);
			// Should get the same results if in Root based view
			model.IsRootBased = true;
			result = ConfiguredLcmGenerator.GenerateContentForEntry(variantEntry, model, null, settings).ToString();
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath("/div[@class='lexentry']/span[@class='headword']", 1);
			variantEntryRef.HideMinorEntry = 1;
			result = ConfiguredLcmGenerator.GenerateContentForEntry(variantEntry, model, null, settings).ToString();
			Assert.IsEmpty(result);
		}

		// Variant: Continue to generate the reference even if we are hiding the minor entry (useful for preview).
		[Test]
		public void GenerateContentForEntry_VariantGenerateReferenceForHiddenEntry()
		{
			var lexentry = CreateInterestingLexEntry(Cache);
			var variantEntry = CreateInterestingLexEntry(Cache);
			var variantEntryRef = CreateVariantForm(Cache, lexentry, variantEntry);
			variantEntryRef.VariantEntryTypesRS[0] = Cache.LangProject.LexDbOA.VariantEntryTypesOA.PossibilitiesOS[0] as ILexEntryType;

			var variantFormNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwningEntry",
				CSSClassNameOverride = "headword",
				SubField = "HeadWordRef",
				IsEnabled = true,
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "vernacular" })
			};
			var variantTypeNameNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Name",
				IsEnabled = true,
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "analysis" })
			};
			var variantTypeNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VariantEntryTypesRS",
				CSSClassNameOverride = "variantentrytypes",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { variantTypeNameNode },
			};
			var variantNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VariantFormEntryBackRefs",
				Children = new List<ConfigurableDictionaryNode> { variantTypeNode, variantFormNode },
				DictionaryNodeOptions = GetFullyEnabledListOptions(DictionaryNodeListOptions.ListIds.Variant, Cache)
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { variantNode }
			};
			var model = new DictionaryConfigurationModel
			{
				Parts = new List<ConfigurableDictionaryNode> { mainEntryNode },
				IsRootBased = false
			};
			CssGeneratorTests.PopulateFieldsForTesting(model);

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null, false, false);
			const string withReference = "/div[@class='lexentry']/span[@class='variantformentrybackrefs']/span[@class='variantformentrybackref']/span[@class='headword']/span[@lang='fr']/span[@lang='fr']/a[@href]";

			// When not hiding minor entries this should generate the reference.
			variantEntryRef.HideMinorEntry = 0;
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(lexentry, model, null, settings).ToString();
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(withReference, 2);

			//SUT
			// When hiding minor entries this should still generate the reference.
			variantEntryRef.HideMinorEntry = 1;
			result = ConfiguredLcmGenerator.GenerateContentForEntry(lexentry, model, null, settings).ToString();
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(withReference, 2);
		}

		// Variant: Don't generate the reference if we are hiding minor entries AND we are publishing to Webonary.
		[Test]
		public void GenerateContentForEntry_VariantDontGenerateReference()
		{
			var lexentry = CreateInterestingLexEntry(Cache);
			var variantEntry = CreateInterestingLexEntry(Cache);
			var variantEntryRef = CreateVariantForm(Cache, lexentry, variantEntry);
			variantEntryRef.VariantEntryTypesRS[0] = Cache.LangProject.LexDbOA.VariantEntryTypesOA.PossibilitiesOS[0] as ILexEntryType;

			var variantFormNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwningEntry",
				CSSClassNameOverride = "headword",
				SubField = "HeadWordRef",
				IsEnabled = true,
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "vernacular" })
			};
			var variantTypeNameNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Name",
				IsEnabled = true,
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "analysis" })
			};
			var variantTypeNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VariantEntryTypesRS",
				CSSClassNameOverride = "variantentrytypes",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { variantTypeNameNode },
			};
			var variantNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VariantFormEntryBackRefs",
				Children = new List<ConfigurableDictionaryNode> { variantTypeNode, variantFormNode },
				DictionaryNodeOptions = GetFullyEnabledListOptions(DictionaryNodeListOptions.ListIds.Variant, Cache)
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { variantNode }
			};
			var model = new DictionaryConfigurationModel
			{
				Parts = new List<ConfigurableDictionaryNode> { mainEntryNode },
				IsRootBased = false
			};
			CssGeneratorTests.PopulateFieldsForTesting(model);

			variantEntryRef.HideMinorEntry = 1;
			const string withReference = "/div[@class='lexentry']/span[@class='variantformentrybackrefs']/span[@class='variantformentrybackref']/span[@class='headword']/span[@lang='fr']/span[@lang='fr']/a[@href]";
			const string withoutReference = "/div[@class='lexentry']/span[@class='variantformentrybackrefs']/span[@class='variantformentrybackref']/span[@class='headword']/span[@lang='fr']/span[@lang='fr']";

			// When hiding minor entries this should still generate the reference (if not publishing to Webonary).
			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null, false, false);
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(lexentry, model, null, settings).ToString();
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(withReference, 2);

			//SUT
			// When hiding minor entries and publishing to Webonary this should NOT generate the reference.
			settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null, false, true);
			result = ConfiguredLcmGenerator.GenerateContentForEntry(lexentry, model, null, settings).ToString();
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(withReference, 0);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(withoutReference, 2);
		}

		public enum FormType { Specified, Unspecified, None }

		[Test]
		public void GenerateContentForEntry_ReferencedNode_GeneratesBothClasses()
		{
			var lexentry = CreateInterestingLexEntry(Cache);
			var subentry = CreateInterestingLexEntry(Cache);
			CreateComplexForm(Cache, lexentry, subentry, true);

			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLHeadWord",
				CSSClassNameOverride = "headword",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};
			var sharedSubentryNode = new ConfigurableDictionaryNode
			{
				Label = "SharedSubentries",
				Children = new List<ConfigurableDictionaryNode> { headwordNode },
				FieldDescription = "Subentries",
				CSSClassNameOverride = "sharedsubentries"
			};
			var subentryNode = new ConfigurableDictionaryNode
			{
				ReferenceItem = "SharedSubentries",
				//DictionaryNodeOptions = GetFullyEnabledListOptions(DictionaryNodeListOptions.ListIds.Complex, true),
				FieldDescription = "Subentries",
				CSSClassNameOverride = "reffingsubs"
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { subentryNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(DictionaryConfigurationModelTests.CreateSimpleSharingModel(mainEntryNode, sharedSubentryNode));

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(lexentry, mainEntryNode, null, settings).ToString();
			const string headwordXpath = "//span[@class='reffingsubs']/span[@class='reffingsub sharedsubentry']/span[@class='headword']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(headwordXpath, 1);
		}

		[Test]
		public void GenerateContentForEntry_GeneratesCorrectMainAndMinorEntries()
		{
			var firstMainEntry = CreateInterestingLexEntry(Cache);
			var idiom = CreateInterestingLexEntry(Cache, "entry1", "myComplexForm");
			CreateComplexForm(Cache, firstMainEntry, idiom, false, 4);

			var idiomGuid = "b2276dec-b1a6-4d82-b121-fd114c009c59";

			var enabledComplexEntryTypes = new List<string>();
			enabledComplexEntryTypes.Add(idiomGuid);// Idiom

			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLHeadWord",
				CSSClassNameOverride = "headword",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { headwordNode },
				FieldDescription = "LexEntry",
				Label = "Main Entry",
				DictionaryNodeOptions = GetListOptionsForStrings(DictionaryNodeListOptions.ListIds.Complex, enabledComplexEntryTypes)
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForMainEntry(idiom, mainEntryNode, null, settings, 0).ToString();
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath("/div[@class='lexentry']/span[@class='headword']", 1);
			var css = ((CssGenerator)settings.StylesGenerator).GetStylesString();
			// verify that the flow reset css is generated
			Assert.That(css, Contains.Substring("white-space:pre-wrap"));
			Assert.That(css, Contains.Substring("clear:both"));
			var complexOptions = (DictionaryNodeListOptions)mainEntryNode.DictionaryNodeOptions;
			complexOptions.Options[0].IsEnabled = false;
			result = ConfiguredLcmGenerator.GenerateContentForMainEntry(idiom, mainEntryNode, null, settings, 1).ToString();
			Assert.IsEmpty(result);
		}

		/// <remarks>Note that the "Unspecified" Types mentioned here are truly unspecified, not the specified Type "Unspecified Form Type"</remarks>
		[Test]
		public void GenerateContentForEntry_GeneratesCorrectMinorEntries(
			[Values(FormType.Specified, FormType.Unspecified, FormType.None)] FormType complexForm,
			[Values(true, false)] bool isUnspecifiedComplexTypeEnabled,
			[Values(FormType.Specified, FormType.Unspecified, FormType.None)] FormType variantForm,
			[Values(true, false)] bool isUnspecifiedVariantTypeEnabled,
			[Values(true, false)] bool isRootBased)
		{
			if ((variantForm == FormType.None && complexForm == FormType.None) // A Minor entry makes no sense if it's neither complex nor variant
				|| (complexForm != FormType.None && !isRootBased)) // Only Root-based configurations consider complex forms to be main entries
				return;

			var mainEntry = CreateInterestingLexEntry(Cache);
			var minorEntry = CreateInterestingLexEntry(Cache);
			var enabledMinorEntryTypes = new List<string>();

			if (complexForm != FormType.None)
			{
				var complexRef = CreateComplexForm(Cache, mainEntry, minorEntry, false);
				if (complexForm == FormType.Unspecified)
					complexRef.ComplexEntryTypesRS.Clear();
			}

			if (isUnspecifiedComplexTypeEnabled)
				enabledMinorEntryTypes.Add(XmlViewsUtils.GetGuidForUnspecifiedComplexFormType().ToString());

			if (variantForm != FormType.None)
			{
				var variantRef = CreateVariantForm(Cache, mainEntry, minorEntry);
				if (variantForm == FormType.Unspecified)
					variantRef.VariantEntryTypesRS.Clear();
			}

			if (isUnspecifiedVariantTypeEnabled)
				enabledMinorEntryTypes.Add(XmlViewsUtils.GetGuidForUnspecifiedVariantType().ToString());

			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLHeadWord",
				CSSClassNameOverride = "headword",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" }),
				IsEnabled = true
			};
			var minorEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { headwordNode },
				DictionaryNodeOptions = GetListOptionsForStrings(DictionaryNodeListOptions.ListIds.Minor, enabledMinorEntryTypes),
				FieldDescription = "LexEntry",
				IsEnabled = true
			};
			var model = new DictionaryConfigurationModel
			{
				Parts = new List<ConfigurableDictionaryNode> { new ConfigurableDictionaryNode(), minorEntryNode }, // dummy main entry node
				IsRootBased = isRootBased
			};
			CssGeneratorTests.PopulateFieldsForTesting(model);

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(minorEntry, model, null, settings).ToString();

			var isComplexFormShowing = complexForm == FormType.Unspecified && isUnspecifiedComplexTypeEnabled;
			var isVariantFormShowing = variantForm == FormType.Unspecified && isUnspecifiedVariantTypeEnabled;
			var isMinorEntryShowing = isComplexFormShowing || isVariantFormShowing;

			if (isMinorEntryShowing)
				AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath("/div[@class='lexentry']/span[@class='headword']", 1);
			else
				Assert.IsEmpty(result);
		}

		[Test]
		public void IsCollectionType()
		{
			var assembly = Assembly.Load(ConfiguredLcmGenerator.AssemblyFile);
			Assert.True(ConfiguredLcmGenerator.IsCollectionType(typeof(IEnumerable<>)));
			Assert.True(ConfiguredLcmGenerator.IsCollectionType(typeof(ILcmOwningSequence<>)));
			Assert.True(ConfiguredLcmGenerator.IsCollectionType(typeof(ILcmReferenceCollection<>)));
			var twoParamImplOfIFdoVector =
				assembly.GetType("SIL.LCModel.DomainImpl.ScrTxtPara").GetNestedType("OwningSequenceWrapper`2", BindingFlags.NonPublic);
			Assert.True(ConfiguredLcmGenerator.IsCollectionType(twoParamImplOfIFdoVector));
			Assert.True(ConfiguredLcmGenerator.IsCollectionType(typeof(ILcmVector)), "Custom fields containing list items may no longer work.");

			// Strings and MultiStrings, while enumerable, are not collections as we define them for the purpose of publishing data as XHTML
			Assert.False(ConfiguredLcmGenerator.IsCollectionType(typeof(string)));
			Assert.False(ConfiguredLcmGenerator.IsCollectionType(typeof(ITsString)));
			Assert.False(ConfiguredLcmGenerator.IsCollectionType(typeof(IMultiStringAccessor)));
			Assert.False(ConfiguredLcmGenerator.IsCollectionType(assembly.GetType("SIL.LCModel.DomainImpl.VirtualStringAccessor")));
		}

		[Test]
		public void GenerateContentForEntry_FilterByPublication()
		{
			// Note that my HS French is nonexistent after 40+ years.  But this is only test code...
			var typeMain = CreatePublicationType("main", Cache);
			var typeTest = CreatePublicationType("test", Cache);

			// This entry is published for both main and test.  Its first sense (and example) are published in main, its
			// second sense(and example) are published in test.
			// The second example of the first sense should not be published at all, since it is not published in main and
			// its owner is not published in test.
			var entryCorps = CreateInterestingLexEntry(Cache, "corps", "body");
			entryCorps.DoNotPublishInRC.Remove(typeMain);
			entryCorps.DoNotPublishInRC.Remove(typeTest);
			var Pronunciation = AddPronunciationToEntry(entryCorps, "pronunciation", m_wsFr, Cache);
			var exampleCorpsBody1 = AddExampleToSense(entryCorps.SensesOS[0], "Le corps est gros.", Cache, m_wsFr, m_wsEn, "The body is big.");
			var exampleCorpsBody2 = AddExampleToSense(entryCorps.SensesOS[0], "Le corps est esprit.", Cache, m_wsFr, m_wsEn, "The body is spirit.");
			AddSenseToEntry(entryCorps, "corpse", m_wsEn, Cache);
			AddExampleToSense(entryCorps.SensesOS[1], "Le corps est mort.", Cache, m_wsFr, m_wsEn, "The corpse is dead.");

			var sensePic = CreatePicture(Cache, ws: "fr");
			entryCorps.SensesOS[0].PicturesOS.Add(sensePic);

			Pronunciation.DoNotPublishInRC.Add(typeTest);
			entryCorps.SensesOS[0].DoNotPublishInRC.Add(typeTest);
			exampleCorpsBody1.DoNotPublishInRC.Add(typeTest);
			exampleCorpsBody2.DoNotPublishInRC.Add(typeMain);   // should not show at all!
			sensePic.DoNotPublishInRC.Add(typeTest);

			entryCorps.SensesOS[1].DoNotPublishInRC.Add(typeMain);
			//exampleCorpsCorpse1.DoNotPublishInRC.Add(typeMain); -- should not show in main because its owner is not shown there

			// This entry is published only in main, together with its sense and example.
			var entryBras = CreateInterestingLexEntry(Cache, "bras", "arm");
			entryBras.DoNotPublishInRC.Remove(typeMain);
			AddExampleToSense(entryBras.SensesOS[0], "Mon bras est casse.", Cache, m_wsFr, m_wsEn, "My arm is broken.");
			AddSenseToEntry(entryBras, "hand", m_wsEn, Cache);
			AddExampleToSense(entryBras.SensesOS[1], "Mon bras va bien.", Cache, m_wsFr, m_wsEn, "My arm is fine.");
			entryBras.DoNotPublishInRC.Add(typeTest);
			entryBras.SensesOS[0].DoNotPublishInRC.Add(typeTest);
			entryBras.SensesOS[1].DoNotPublishInRC.Add(typeTest);
			//exampleBrasArm1.DoNotPublishInRC.Add(typeTest); -- should not show in test because its owner is not shown there
			//exampleBrasHand1.DoNotPublishInRC.Add(typeTest); -- should not show in test because its owner is not shown there

			// This entry is published only in test, together with its sense and example.
			var entryOreille = CreateInterestingLexEntry(Cache, "oreille", "ear");
			AddExampleToSense(entryOreille.SensesOS[0], "Lac Pend d'Oreille est en Idaho.", Cache, m_wsFr, m_wsEn, "Lake Pend d'Oreille is in Idaho.");
			entryOreille.DoNotPublishInRC.Add(typeMain);
			entryOreille.SensesOS[0].DoNotPublishInRC.Add(typeMain);
			entryOreille.DoNotPublishInRC.Remove(typeTest);
			entryOreille.SensesOS[0].DoNotPublishInRC.Remove(typeTest);
			//exampleOreille1.DoNotPublishInRC.Add(typeMain); -- should not show in main because its owner is not shown there

			var entryEntry = CreateInterestingLexEntry(Cache, "entry", "entry");
			entryEntry.DoNotPublishInRC.Remove(typeMain);
			entryEntry.DoNotPublishInRC.Remove(typeTest);
			var entryMainsubentry = CreateInterestingLexEntry(Cache, "mainsubentry", "mainsubentry");
			entryMainsubentry.DoNotPublishInRC.Remove(typeMain);
			entryMainsubentry.DoNotPublishInRC.Add(typeTest);
			CreateComplexForm(Cache, entryEntry, entryMainsubentry, true);

			var entryTestsubentry = CreateInterestingLexEntry(Cache, "testsubentry", "testsubentry");
			entryTestsubentry.DoNotPublishInRC.Add(typeMain);
			entryTestsubentry.DoNotPublishInRC.Remove(typeTest);
			CreateComplexForm(Cache, entryEntry, entryTestsubentry, true);
			var bizarroVariant = CreateInterestingLexEntry(Cache, "bizarre", "myVariant");
			CreateVariantForm(Cache, entryEntry, bizarroVariant, "Spelling Variant");
			bizarroVariant.DoNotPublishInRC.Remove(typeMain);
			bizarroVariant.DoNotPublishInRC.Add(typeTest);

			// Note that the decorators must be created (or refreshed) *after* the data exists.
			int flidVirtual = Cache.ServiceLocator.GetInstance<Virtuals>().LexDbEntries;
			var pubEverything = new DictionaryPublicationDecorator(Cache, (ISilDataAccessManaged)Cache.MainCacheAccessor, flidVirtual);
			var pubMain = new DictionaryPublicationDecorator(Cache, (ISilDataAccessManaged)Cache.MainCacheAccessor, flidVirtual, typeMain);
			var pubTest = new DictionaryPublicationDecorator(Cache, (ISilDataAccessManaged)Cache.MainCacheAccessor, flidVirtual, typeTest);
			//SUT
			var hvosMain = new List<int>(pubMain.GetEntriesToPublish(m_propertyTable, flidVirtual));
			Assert.AreEqual(5, hvosMain.Count, "there are five entries in the main publication");
			Assert.IsTrue(hvosMain.Contains(entryCorps.Hvo), "corps is shown in the main publication");
			Assert.IsTrue(hvosMain.Contains(entryBras.Hvo), "bras is shown in the main publication");
			Assert.IsTrue(hvosMain.Contains(bizarroVariant.Hvo), "bizarre is shown in the main publication");
			Assert.IsFalse(hvosMain.Contains(entryOreille.Hvo), "oreille is not shown in the main publication");
			Assert.IsTrue(hvosMain.Contains(entryEntry.Hvo), "entry is shown in the main publication");
			Assert.IsTrue(hvosMain.Contains(entryMainsubentry.Hvo), "mainsubentry is shown in the main publication");
			Assert.IsFalse(hvosMain.Contains(entryTestsubentry.Hvo), "testsubentry is not shown in the main publication");
			var hvosTest = new List<int>(pubTest.GetEntriesToPublish(m_propertyTable, flidVirtual));
			Assert.AreEqual(4, hvosTest.Count, "there are four entries in the test publication");
			Assert.IsTrue(hvosTest.Contains(entryCorps.Hvo), "corps is shown in the test publication");
			Assert.IsFalse(hvosTest.Contains(entryBras.Hvo), "bras is not shown in the test publication");
			Assert.IsFalse(hvosTest.Contains(bizarroVariant.Hvo), "bizarre is not shown in the test publication");
			Assert.IsTrue(hvosTest.Contains(entryOreille.Hvo), "oreille is shown in the test publication");
			Assert.IsTrue(hvosTest.Contains(entryEntry.Hvo), "entry is shown in the test publication");
			Assert.IsFalse(hvosTest.Contains(entryMainsubentry.Hvo), "mainsubentry is shown in the test publication");
			Assert.IsTrue(hvosTest.Contains(entryTestsubentry.Hvo), "testsubentry is shown in the test publication");

			var variantFormNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwningEntry",
				CSSClassNameOverride = "headword",
				SubField = "HeadWordRef",
				IsEnabled = true,
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "vernacular" })
			};
			var variantTypeNameNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Name",
				IsEnabled = true,
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "analysis" })
			};
			var variantTypeNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VariantEntryTypesRS",
				CSSClassNameOverride = "variantentrytypes",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { variantTypeNameNode },
			};
			var variantNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VariantFormEntryBackRefs",
				Children = new List<ConfigurableDictionaryNode> { variantTypeNode, variantFormNode },
				DictionaryNodeOptions = GetFullyEnabledListOptions(DictionaryNodeListOptions.ListIds.Variant, Cache)
			};
			var subHeadwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLHeadWord",
				CSSClassNameOverride = "subentry",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};
			var subentryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Subentries",
				Children = new List<ConfigurableDictionaryNode> { subHeadwordNode }
			};
			var translationNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Translation",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" }),
				CSSClassNameOverride = "translatedsentence"
			};
			var translationsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "TranslationsOC",
				Children = new List<ConfigurableDictionaryNode> { translationNode },
				CSSClassNameOverride = "translations"
			};
			var exampleNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Example",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" }),
				CSSClassNameOverride = "examplesentence"
			};
			var examplesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ExamplesOS",
				Children = new List<ConfigurableDictionaryNode> { exampleNode, translationsNode },
				CSSClassNameOverride = "examples"
			};
			var glossNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "DefinitionOrGloss",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" }),
				CSSClassNameOverride = "definitionorgloss"
			};
			var captionNode = new ConfigurableDictionaryNode { FieldDescription = "Caption", DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" }) };
			var pictureNode = new ConfigurableDictionaryNode
			{
				DictionaryNodeOptions = new DictionaryNodePictureOptions(),
				FieldDescription = "PicturesOfSenses",
				CSSClassNameOverride = "Pictures",
				Children = new List<ConfigurableDictionaryNode> { captionNode }
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions(),
				Children = new List<ConfigurableDictionaryNode> { glossNode, examplesNode },
				CSSClassNameOverride = "senses"
			};
			var pronunciationForm = new ConfigurableDictionaryNode
			{
				FieldDescription = "Form",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};
			var mainPronunciationsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "PronunciationsOS",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" }),
				CSSClassNameOverride = "pronunciations",
				Children = new List<ConfigurableDictionaryNode> { pronunciationForm }
			};
			var mainHeadwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "HeadWord",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" }),
				CSSClassNameOverride = "entry"
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { mainHeadwordNode, mainPronunciationsNode, sensesNode, pictureNode, subentryNode, variantNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);

			const string matchFrenchEntry = "//span[@class='entry']/span[@lang='fr']";
			const string matchFrenchPronunciation = "//span[@class='pronunciations']/span[@class='pronunciation']/span[@class='form']/span[@lang='fr']";
			const string matchEnglishDefOrGloss =
				"//span[@class='senses']/span[@class='sensecontent']/span[@class='sense']/span[@class='definitionorgloss']/span[@lang='en']";
			const string matchFrenchExample =
				"//span[@class='senses']/span[@class='sensecontent']/span[@class='sense']/span[@class='examples']/span[@class='example']/span[@class='examplesentence']/span[@lang='fr']";
			const string matchEnglishTranslation =
				"//span[@class='senses']/span[@class='sensecontent']/span[@class='sense']/span[@class='examples']/span[@class='example']/span[@class='translations']/span[@class='translation']/span[@class='translatedsentence']/span[@lang='en']";
			const string matchFrenchPictureCaption = "//span[@class='pictures']/div[@class='picture']/div[@class='captionContent']/span[@class='caption']/span[@lang='fr']";

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var output = ConfiguredLcmGenerator.GenerateContentForEntry(entryCorps, mainEntryNode, pubEverything, settings).ToString();
			Console.WriteLine(output);
			Assert.That(output, Is.Not.Null.Or.Empty);
			// Verify that the unfiltered output displays everything.
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFrenchEntry, 1);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFrenchPronunciation, 1);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchEnglishDefOrGloss, 2);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFrenchExample, 3);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchEnglishTranslation, 3);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFrenchPictureCaption, 1);

			//SUT
			output = ConfiguredLcmGenerator.GenerateContentForEntry(entryCorps, mainEntryNode, pubMain, settings).ToString();
			Assert.That(output, Is.Not.Null.Or.Empty);
			// Verify that the main publication output displays what it should.
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFrenchEntry, 1);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFrenchPronunciation, 1);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchEnglishDefOrGloss, 1);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFrenchExample, 1);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchEnglishTranslation, 1);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFrenchPictureCaption, 1);
			const string matchBodyIsBig =
				"//span[@class='examples']/span[@class='example']/span[@class='translations']/span[@class='translation']/span[@class='translatedsentence']/span[@lang='en' and text()='The body is big.']";
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchBodyIsBig, 1);

			//SUT
			output = ConfiguredLcmGenerator.GenerateContentForEntry(entryCorps, mainEntryNode, pubTest, settings).ToString();
			Assert.That(output, Is.Not.Null.Or.Empty);
			// Verify that the test publication output displays what it should.
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFrenchEntry, 1);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFrenchPronunciation, 0);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchEnglishDefOrGloss, 1);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFrenchExample, 1);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchEnglishTranslation, 1);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFrenchPictureCaption, 0);
			const string matchCorpseIsDead =
				"//span[@class='examples']/span[@class='example']/span[@class='translations']/span[@class='translation']/span[@class='translatedsentence']/span[@lang='en' and text()='The corpse is dead.']";
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchCorpseIsDead, 1);

			//SUT
			output = ConfiguredLcmGenerator.GenerateContentForEntry(entryBras, mainEntryNode, pubEverything, settings).ToString();
			Assert.That(output, Is.Not.Null.Or.Empty);
			// Verify that the unfiltered output displays everything.
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFrenchEntry, 1);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchEnglishDefOrGloss, 2);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFrenchExample, 2);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchEnglishTranslation, 2);

			//SUT
			output = ConfiguredLcmGenerator.GenerateContentForEntry(entryBras, mainEntryNode, pubMain, settings).ToString();
			Assert.That(output, Is.Not.Null.Or.Empty);
			// Verify that the main publication output displays everything.
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFrenchEntry, 1);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchEnglishDefOrGloss, 2);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFrenchExample, 2);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchEnglishTranslation, 2);

			//SUT
			// We can still produce test publication output for the entry since we have a copy of it.  Its senses and
			// examples should not be displayed because the senses are separately hidden.
			output = ConfiguredLcmGenerator.GenerateContentForEntry(entryBras, mainEntryNode, pubTest, settings).ToString();
			Assert.That(output, Is.Not.Null.Or.Empty);
			// Verify that the test output doesn't display the senses and examples.
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFrenchEntry, 1);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchEnglishDefOrGloss, 0);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFrenchExample, 0);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchEnglishTranslation, 0);

			//SUT
			output = ConfiguredLcmGenerator.GenerateContentForEntry(entryOreille, mainEntryNode, pubEverything, settings).ToString();
			Assert.That(output, Is.Not.Null.Or.Empty);
			// Verify that the unfiltered output displays everything.
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFrenchEntry, 1);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchEnglishDefOrGloss, 1);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFrenchExample, 1);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchEnglishTranslation, 1);

			//SUT
			// We can still produce main publication output for the entry since we have a copy of it.  Its sense and
			// example should not be displayed because the sense is separately hidden.
			output = ConfiguredLcmGenerator.GenerateContentForEntry(entryOreille, mainEntryNode, pubMain, settings).ToString();
			Assert.That(output, Is.Not.Null.Or.Empty);
			// Verify that the test output doesn't display the sense and example.
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFrenchEntry, 1);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchEnglishDefOrGloss, 0);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFrenchExample, 0);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchEnglishTranslation, 0);

			//SUT
			output = ConfiguredLcmGenerator.GenerateContentForEntry(entryOreille, mainEntryNode, pubTest, settings).ToString();
			Assert.That(output, Is.Not.Null.Or.Empty);
			// Verify that the test publication output displays everything.
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFrenchEntry, 1);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchEnglishDefOrGloss, 1);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFrenchExample, 1);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchEnglishTranslation, 1);

			const string matchFrenchSubentry = "//span[@class='subentries']/span[@class='subentry']/span[@class='subentry']/span[@lang='fr']";
			const string matchMainsubentry = "//span[@class='subentries']/span[@class='subentry']/span[@class='subentry']/span[@lang='fr'and text()='mainsubentry']";
			const string matchTestsubentry = "//span[@class='subentries']/span[@class='subentry']/span[@class='subentry']/span[@lang='fr'and text()='testsubentry']";
			const string matchVariantRef = "//span[@class='variantentrytypes']/span[@class='variantentrytype']/span[@class='name']/span[@lang='en']";

			//SUT
			output = ConfiguredLcmGenerator.GenerateContentForEntry(entryEntry, mainEntryNode, pubMain, settings).ToString();
			Assert.That(output, Is.Not.Null.Or.Empty);
			// Verify that the main publication output displays what it should.
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFrenchEntry, 1);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchEnglishDefOrGloss, 1);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFrenchSubentry, 1);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchMainsubentry, 1);
			AssertThatXmlIn.String(output).HasNoMatchForXpath(matchTestsubentry);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchVariantRef, 1);

			//SUT
			output = ConfiguredLcmGenerator.GenerateContentForEntry(entryEntry, mainEntryNode, pubTest, settings).ToString();
			Assert.That(output, Is.Not.Null.Or.Empty);
			// Verify that the test publication output displays what it should.
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFrenchEntry, 1);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchEnglishDefOrGloss, 1);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFrenchSubentry, 1);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchMainsubentry, 0);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchTestsubentry, 1);
			AssertThatXmlIn.String(output).HasNoMatchForXpath(matchVariantRef);
		}

		[Test]
		public void GenerateContentForEntry_GeneratesVariantEntryTypesLabelWithNoRepetition()
		{
			var variantTypeNameNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Name",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "analysis" })
			};
			var variantTypeNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VariantEntryTypesRS",
				CSSClassNameOverride = "variantentrytypes",
				Children = new List<ConfigurableDictionaryNode> { variantTypeNameNode },
			};
			var variantNameNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwningEntry",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" }),
				SubField = "HeadWordRef",
				CSSClassNameOverride = "headword"
			};
			var variantNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VariantFormEntryBackRefs",
				Children = new List<ConfigurableDictionaryNode> { variantTypeNode, variantNameNode },
				DictionaryNodeOptions = GetFullyEnabledListOptions(DictionaryNodeListOptions.ListIds.Variant, Cache)
			};
			var mainHeadwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "HeadWord",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" }),
				CSSClassNameOverride = "entry"
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { mainHeadwordNode, variantNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);

			var entryEntry = CreateInterestingLexEntry(Cache);

			var ve1 = CreateInterestingLexEntry(Cache, "variantEntry1");
			var ve2 = CreateInterestingLexEntry(Cache, "variantEntry2");
			var ve3 = CreateInterestingLexEntry(Cache, "variantEntry3");
			var ve4 = CreateInterestingLexEntry(Cache, "variantEntry4");
			// (specifying GUID's to ensure GUID sort does not muss up Type grouping)
			using (CreateVariantForm(Cache, entryEntry, ve1, new Guid("00000000-0000-0000-0000-000000000001"), "Free Variant")) // unique Type; generated
			using (CreateVariantForm(Cache, entryEntry, ve2, new Guid("00000000-0000-0000-0000-000000000002"), "Spelling Variant")) // unique Type; generated
			using (CreateVariantForm(Cache, entryEntry, ve3, new Guid("00000000-0000-0000-0000-000000000003"), "Free Variant")) // repeat Type; consolidated
			using (CreateVariantForm(Cache, entryEntry, ve4, new Guid("00000000-0000-0000-0000-000000000004"), null)) // no Type; none generated
			{
				var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
				var output = ConfiguredLcmGenerator.GenerateContentForEntry(entryEntry, mainEntryNode, null, settings).ToString();
				const string matchVariantRef = "//span[@class='variantentrytypes']/span[@class='variantentrytype']/span[@class='name']/span[@lang='en']";
				AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchVariantRef, 2);
			}
		}

		[Test]
		public void GenerateContentForEntry_GeneratesVariantEntryTypesShowOnlySelectedListItem()
		{
			var variantTypeNameNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Name",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "analysis" })
			};
			var variantTypeNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VariantEntryTypesRS",
				CSSClassNameOverride = "variantentrytypes",
				Children = new List<ConfigurableDictionaryNode> { variantTypeNameNode },
				DictionaryNodeOptions = GetFullyEnabledListOptions(DictionaryNodeListOptions.ListIds.Variant, Cache)
			};
			var variantNameNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwningEntry",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" }),
				SubField = "HeadWordRef",
				CSSClassNameOverride = "headword"
			};
			var variantNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VariantFormEntryBackRefs",
				Children = new List<ConfigurableDictionaryNode> { variantTypeNode, variantNameNode },
				DictionaryNodeOptions = GetFullyEnabledListOptions(DictionaryNodeListOptions.ListIds.Variant, Cache)
			};
			var mainHeadwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "HeadWord",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" }),
				CSSClassNameOverride = "entry"
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { mainHeadwordNode, variantNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);

			var entryEntry = CreateInterestingLexEntry(Cache);

			var ve1 = CreateInterestingLexEntry(Cache, "variantEntry1");
			var ve2 = CreateInterestingLexEntry(Cache, "variantEntry2");

			//Uncheck all other variant types except "Free Variant"
			const string freeVariantGuid = "4343b1ef-b54f-4fa4-9998-271319a6d74c";
			var variantOptions = (DictionaryNodeListOptions)mainEntryNode.Children[1].DictionaryNodeOptions;
			foreach (var variantType in variantOptions.Options)
			{
				variantType.IsEnabled = variantType.Id == freeVariantGuid;
			}

			CreateVariantForm(Cache, entryEntry, ve1, "Free Variant"); // unique Type;
			CreateVariantForm(Cache, entryEntry, ve2, "Spelling Variant"); // unique Type; UnChecked
			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			var output = ConfiguredLcmGenerator.GenerateContentForEntry(entryEntry, mainEntryNode, null, settings).ToString();
			const string matchFreeVariantRef = "//span[@class='variantentrytypes']/span[@class='variantentrytype']/span[@class='name']/span[@lang='en' and text()='Free Variant']";
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFreeVariantRef, 1);
			const string matchSpellingVariantRef = "//span[@class='variantentrytypes']/span[@class='variantentrytype']/span[@class='name']/span[@lang='en' and text()='Spelling Variant']";
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchSpellingVariantRef, 0);
		}

		[Test]
		public void GenerateContentForEntry_GeneratesComplexFormEntryTypesLabelWithNoRepetition()
		{
			var typeMain = Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS[0];

			var entryEntry = CreateInterestingLexEntry(Cache, "entry");

			var firstComplexForm = CreateInterestingLexEntry(Cache, "entry1", "myComplexForm");
			CreateComplexForm(Cache, entryEntry, firstComplexForm, false); //Compound

			var secondComplexForm = CreateInterestingLexEntry(Cache, "entry2", "myComplexForm");
			CreateComplexForm(Cache, entryEntry, secondComplexForm, false); //Compound

			var thirdComplexForm = CreateInterestingLexEntry(Cache, "entry3", "myComplexForm");
			CreateComplexForm(Cache, entryEntry, thirdComplexForm, false); //Compound

			int flidVirtual = Cache.ServiceLocator.GetInstance<Virtuals>().LexDbEntries;
			var pubMain = new DictionaryPublicationDecorator(Cache, (ISilDataAccessManaged)Cache.MainCacheAccessor, flidVirtual, typeMain);

			var complexFormTypeNameNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Name",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "analysis" })
			};
			var complexFormTypeNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ComplexEntryTypesRS",
				CSSClassNameOverride = "complexformtypes",
				Children = new List<ConfigurableDictionaryNode> { complexFormTypeNameNode },
			};
			var complexFormNameNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwningEntry",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" }),
				SubField = "HeadWordRef",
				CSSClassNameOverride = "headword"
			};
			var complexFormNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VisibleComplexFormBackRefs",
				Children = new List<ConfigurableDictionaryNode> { complexFormTypeNode, complexFormNameNode },
				DictionaryNodeOptions = GetFullyEnabledListOptions(DictionaryNodeListOptions.ListIds.Complex, Cache)
			};
			var mainHeadwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "HeadWord",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" }),
				CSSClassNameOverride = "entry"
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { mainHeadwordNode, complexFormNode },
				FieldDescription = "LexEntry"
			};

			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var output = ConfiguredLcmGenerator.GenerateContentForEntry(entryEntry, mainEntryNode, pubMain, DefaultSettings).ToString();
			const string matchComplexFormRef = "//span[@class='complexformtypes']/span[@class='complexformtype']/span[@class='name']/span[@lang='en']";
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchComplexFormRef, 1);
		}

		[Test]
		public void GenerateContentForEntry_GeneratesComplexFormEntryTypesAndNamesGroup()
		{
			var typeMain = Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS[0];

			var entryEntry = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(entryEntry, "entry", m_wsFr);

			var firstComplexForm = CreateInterestingLexEntry(Cache, "entry1", "myComplexForm");
			CreateComplexForm(Cache, entryEntry, firstComplexForm, false); //Compound
			CreateComplexForm(Cache, entryEntry, firstComplexForm, false, 4); //Idiom

			var secondComplexForm = CreateInterestingLexEntry(Cache, "entry2", "myComplexForm");
			CreateComplexForm(Cache, entryEntry, secondComplexForm, false); //Compound

			var thirdComplexForm = CreateInterestingLexEntry(Cache, "entry3", "myComplexForm");
			CreateComplexForm(Cache, entryEntry, thirdComplexForm, false); //Compound

			int flidVirtual = Cache.ServiceLocator.GetInstance<Virtuals>().LexDbEntries;
			var pubMain = new DictionaryPublicationDecorator(Cache, (ISilDataAccessManaged)Cache.MainCacheAccessor, flidVirtual, typeMain);

			var complexFormTypeNameNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Name",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "analysis" })
			};
			var complexFormTypeNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ComplexEntryTypesRS",
				CSSClassNameOverride = "complexformtypes",
				Children = new List<ConfigurableDictionaryNode> { complexFormTypeNameNode },
			};
			var complexFormNameNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwningEntry",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" }),
				SubField = "HeadWordRef",
				CSSClassNameOverride = "headword"
			};
			var complexFormNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VisibleComplexFormBackRefs",
				Children = new List<ConfigurableDictionaryNode> { complexFormTypeNode, complexFormNameNode },
				DictionaryNodeOptions = GetFullyEnabledListOptions(DictionaryNodeListOptions.ListIds.Complex, Cache)
			};
			var mainHeadwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "HeadWord",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" }),
				CSSClassNameOverride = "entry"
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { mainHeadwordNode, complexFormNode },
				FieldDescription = "LexEntry"
			};

			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var output = ConfiguredLcmGenerator.GenerateContentForEntry(entryEntry, mainEntryNode, pubMain, DefaultSettings).ToString();
			const string matchComplexFormTypeCompound = "//span[@class='complexformtypes']/span[@class='complexformtype']/span[@class='name']/span[@lang='en' and text()='Compound']";
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchComplexFormTypeCompound, 1);
			const string matchComplexFormTypeIdiom = "//span[@class='complexformtypes']/span[@class='complexformtype']/span[@class='name']/span[@lang='en' and text()='Idiom']";
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchComplexFormTypeIdiom, 1);
			const string matchComplexFormName = "//span[@class='visiblecomplexformbackref']/span[@class='headword']/span[@lang='fr']/a";
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchComplexFormName, 4);
		}

		[Test]
		public void GenerateContentForEntry_ComplexFormAndSenseInPara()
		{
			var lexentry = CreateInterestingLexEntry(Cache);

			var subentry = CreateInterestingLexEntry(Cache);
			var subentryRef = CreateComplexForm(Cache, lexentry, subentry, true);

			var complexRefRevAbbr = subentryRef.ComplexEntryTypesRS[0].ReverseAbbr.BestAnalysisAlternative.Text;

			var revAbbrevNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ReverseAbbr",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" })
			};
			var refTypeNode = new ConfigurableDictionaryNode
			{
				FieldDescription = ConfiguredLcmGenerator.LookupComplexEntryType,
				CSSClassNameOverride = "complexformtypes",
				Children = new List<ConfigurableDictionaryNode> { revAbbrevNode }
			};
			var subentryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { refTypeNode },
				DictionaryNodeOptions = GetFullyEnabledListOptions(DictionaryNodeListOptions.ListIds.Complex, Cache),
				FieldDescription = "Subentries"
			};
			((IParaOption)subentryNode.DictionaryNodeOptions).DisplayEachInAParagraph = true;
			var glossNode = new ConfigurableDictionaryNode { FieldDescription = "Gloss", DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" }) };
			var SenseNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions { DisplayEachSenseInAParagraph = true },
				Children = new List<ConfigurableDictionaryNode> { glossNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { SenseNode, subentryNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);

			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(lexentry, mainEntryNode, null, DefaultSettings).ToString();
			const string senseXpath = "div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense']/span[@class='gloss']/span[@lang='en' and text()='gloss']";
			var paracontinuationxpath = string.Format(
				"div[@class='lexentry']//span[@class='subentries']/span[@class='subentry']/span[@class='complexformtypes']/span[@class='complexformtype']/span[@class='reverseabbr']/span[@lang='en' and text()='{0}']",
				complexRefRevAbbr);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(senseXpath, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(paracontinuationxpath, 1);
		}

		[Test]
		public void GenerateContentForEntry_MinorComplexForm_GeneratesGlossOrSummaryDefinition()
		{
			var wsEn = Cache.WritingSystemFactory.GetWsFromStr("en");
			var lexentry = CreateInterestingLexEntry(Cache);
			AddSenseToEntry(lexentry, "gloss2", wsEn, Cache);
			AddSenseToEntry(lexentry, string.Empty, wsEn, Cache);
			lexentry.SummaryDefinition.SetAnalysisDefaultWritingSystem("MainEntrySummaryDefn");
			lexentry.SensesOS[0].Definition.SetAnalysisDefaultWritingSystem("MainEntryS1Defn");
			lexentry.SensesOS[2].Definition.SetAnalysisDefaultWritingSystem("MainEntryS3Defn");

			var subentry1 = CreateInterestingLexEntry(Cache);
			CreateComplexForm(Cache, lexentry, subentry1, true); // subentry references main ILexEntry

			var subentry2 = CreateInterestingLexEntry(Cache);
			CreateComplexForm(Cache, lexentry.SensesOS[1], subentry2, true); // subentry references 2nd ILexSense

			var subentry3 = CreateInterestingLexEntry(Cache);
			CreateComplexForm(Cache, lexentry.SensesOS[2], subentry3, true); // subentry references 3rd ILexSense

			var glossOrSummDefnNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "GlossOrSummary",
				Label = "Gloss (or Summary Definition)",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "analysis" })
			};
			var refentryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { glossOrSummDefnNode },
				FieldDescription = "ConfigReferencedEntries",
				Label = "Referenced Entries",
				CSSClassNameOverride = "referencedentries"
			};
			var ComponentsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ComplexFormEntryRefs",
				Label = "Components",
				DictionaryNodeOptions = GetFullyEnabledListOptions(DictionaryNodeListOptions.ListIds.Complex, Cache),
				Children = new List<ConfigurableDictionaryNode> { refentryNode }
			};
			var minorEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { ComponentsNode },
				FieldDescription = "LexEntry",
				CSSClassNameOverride = "minorentrycomplex",
			};
			CssGeneratorTests.PopulateFieldsForTesting(minorEntryNode);

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(subentry1, minorEntryNode, null, settings).ToString();
			const string complexFormEntryRefXpath = "div[@class='minorentrycomplex']/span[@class='complexformentryrefs']/span[@class='complexformentryref']";
			const string referencedEntriesXpath = "/span[@class='referencedentries']/span[@class='referencedentry']";
			const string glossOrSummXpath1 = complexFormEntryRefXpath + referencedEntriesXpath + "/span[@class='glossorsummary']/span[@lang='en' and text()='MainEntrySummaryDefn']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(glossOrSummXpath1, 1);

			//SUT
			var result2 = ConfiguredLcmGenerator.GenerateContentForEntry(subentry2, minorEntryNode, null, settings).ToString();
			const string glossOrSummXpath2 = complexFormEntryRefXpath + referencedEntriesXpath + "/span[@class='glossorsummary']/span[@lang='en' and text()='gloss2']";
			AssertThatXmlIn.String(result2).HasSpecifiedNumberOfMatchesForXpath(glossOrSummXpath2, 1);

			//SUT
			var result3 = ConfiguredLcmGenerator.GenerateContentForEntry(subentry3, minorEntryNode, null, settings).ToString();
			const string glossOrSummXpath3 = complexFormEntryRefXpath + referencedEntriesXpath + "/span[@class='glossorsummary']/span[@lang='en' and text()='MainEntryS3Defn']";
			AssertThatXmlIn.String(result3).HasSpecifiedNumberOfMatchesForXpath(glossOrSummXpath3, 1);
		}

		[Test]
		public void GenerateContentForEntry_ContinuationParagraphWithEmtpyContentDoesNotGenerateSelfClosingTag()
		{
			var lexentry = CreateInterestingLexEntry(Cache);
			var glossNode = new ConfigurableDictionaryNode { FieldDescription = "Gloss", DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" }) };
			var subentryNode = new ConfigurableDictionaryNode
			{
				DictionaryNodeOptions = new DictionaryNodeListAndParaOptions { DisplayEachInAParagraph = true },
				FieldDescription = "Subentries"
			};
			var SenseNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions { DisplayEachSenseInAParagraph = true },
				Children = new List<ConfigurableDictionaryNode> { glossNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { SenseNode, subentryNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(lexentry, mainEntryNode, null, settings).ToString();
			const string senseXpath = "div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense']/span[@class='gloss']/span[@lang='en' and text()='gloss']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(senseXpath, 1);
			Assert.That(result, Does.Not.Match(@"<div class=['""]paracontinuation['""]\s*/>"),
				"Empty Self closing <div> element should not generated after senses in paragraph");
			Assert.That(result, Does.Not.Match(@"<div class=['""]paracontinuation['""]\s*></div>"),
				"Empty <div> element should not be generated after senses in paragraph");
		}

		[Test]
		public void SavePublishedHtmlWithStyles_ProduceLetHeadOnlyWhenDesired()
		{
			var lexentry1 = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(lexentry1, "femme", m_wsFr);
			var lexentry2 = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(lexentry2, "homme", m_wsFr);
			int flidVirtual = Cache.ServiceLocator.GetInstance<Virtuals>().LexDbEntries;
			var pubEverything = new DictionaryPublicationDecorator(Cache, (ISilDataAccessManaged)Cache.MainCacheAccessor, flidVirtual);
			var glossNode = new ConfigurableDictionaryNode { FieldDescription = "Gloss", DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" }) };
			var SenseNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions { DisplayEachSenseInAParagraph = true },
				Children = new List<ConfigurableDictionaryNode> { glossNode }
			};
			var mainHeadwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLHeadWord",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" }),
				CSSClassNameOverride = "entry"
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { mainHeadwordNode, SenseNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var model = new DictionaryConfigurationModel
			{
				Parts = new List<ConfigurableDictionaryNode> { mainEntryNode }
			};
			string xhtmlPath = null;
			const string xpath = "//div[@class='letHead']";
			try
			{
				xhtmlPath = LcmXhtmlGenerator.SavePreviewHtmlWithStyles(new[] { lexentry1.Hvo, lexentry2.Hvo }, pubEverything, model, m_propertyTable);
				var xhtml = File.ReadAllText(xhtmlPath);
				AssertThatXmlIn.String(xhtml).HasSpecifiedNumberOfMatchesForXpath(xpath, 2);
				DeleteTempXhtmlAndCssFiles(xhtmlPath);

				xhtmlPath = LcmXhtmlGenerator.SavePreviewHtmlWithStyles(new[] { lexentry1.Hvo, lexentry2.Hvo }, null, model, m_propertyTable);
				xhtml = File.ReadAllText(xhtmlPath);
				AssertThatXmlIn.String(xhtml).HasSpecifiedNumberOfMatchesForXpath(xpath, 2);
				DeleteTempXhtmlAndCssFiles(xhtmlPath);

				xhtmlPath = LcmXhtmlGenerator.SavePreviewHtmlWithStyles(new[] { lexentry1.Hvo }, pubEverything, model, m_propertyTable);
				xhtml = File.ReadAllText(xhtmlPath);
				AssertThatXmlIn.String(xhtml).HasSpecifiedNumberOfMatchesForXpath(xpath, 1);
				DeleteTempXhtmlAndCssFiles(xhtmlPath);

				xhtmlPath = LcmXhtmlGenerator.SavePreviewHtmlWithStyles(new[] { lexentry1.Hvo }, null, model, m_propertyTable);
				xhtml = File.ReadAllText(xhtmlPath);
				AssertThatXmlIn.String(xhtml).HasSpecifiedNumberOfMatchesForXpath(xpath, 0);
			}
			finally
			{
				DeleteTempXhtmlAndCssFiles(xhtmlPath);
			}
		}

		[Test]
		public void SavePublishedHtmlWithStyles_SortByNonHeadwordProducesNoLetHead()
		{
			var firstAEntry = CreateInterestingLexEntry(Cache);
			var firstAHeadword = "alpha1";
			var bHeadword = "beta";
			AddHeadwordToEntry(firstAEntry, firstAHeadword, m_wsFr);
			var bEntry = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(bEntry, bHeadword, m_wsFr);
			int flidVirtual = Cache.ServiceLocator.GetInstance<Virtuals>().LexDbEntries;
			var pubEverything = new DictionaryPublicationDecorator(Cache, (ISilDataAccessManaged)Cache.MainCacheAccessor, flidVirtual);
			var mainHeadwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLHeadWord",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" }),
				CSSClassNameOverride = "entry"
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { mainHeadwordNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var model = new DictionaryConfigurationModel
			{
				Parts = new List<ConfigurableDictionaryNode> { mainEntryNode }
			};
			string xhtmlPath = null;
			const string letterHeaderXPath = "//div[@class='letHead']";
			try
			{
				var clerk = m_propertyTable.GetValue<RecordClerk>("ActiveClerk", null);
				clerk.SortName = "Glosses";
				xhtmlPath = LcmXhtmlGenerator.SavePreviewHtmlWithStyles(new[] { firstAEntry.Hvo }, pubEverything, model, m_propertyTable);
				var xhtml = File.ReadAllText(xhtmlPath);
				AssertThatXmlIn.String(xhtml).HasSpecifiedNumberOfMatchesForXpath(letterHeaderXPath, 0);
			}
			finally
			{
				DeleteTempXhtmlAndCssFiles(xhtmlPath);
			}
		}

		[Test]
		public void SavePublishedHtmlWithStyles_ProducesHeadingsAndEntriesInOrder()
		{
			var firstAEntry = CreateInterestingLexEntry(Cache);
			var firstAHeadword = "alpha1";
			var secondAHeadword = "alpha2";
			var bHeadword = "beta";
			AddHeadwordToEntry(firstAEntry, firstAHeadword, m_wsFr);
			var secondAEntry = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(secondAEntry, secondAHeadword, m_wsFr);
			var bEntry = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(bEntry, bHeadword, m_wsFr);
			int flidVirtual = Cache.ServiceLocator.GetInstance<Virtuals>().LexDbEntries;
			var pubEverything = new DictionaryPublicationDecorator(Cache, (ISilDataAccessManaged)Cache.MainCacheAccessor, flidVirtual);
			var mainHeadwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLHeadWord",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" }),
				CSSClassNameOverride = "entry"
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { mainHeadwordNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var model = new DictionaryConfigurationModel
			{
				Parts = new List<ConfigurableDictionaryNode> { mainEntryNode }
			};
			string xhtmlPath = null;
			const string letterHeaderXPath = "//div[@class='letHead']";
			try
			{
				xhtmlPath = LcmXhtmlGenerator.SavePreviewHtmlWithStyles(new[] { firstAEntry.Hvo, secondAEntry.Hvo, bEntry.Hvo }, pubEverything, model, m_propertyTable);
				var xhtml = File.ReadAllText(xhtmlPath);
				//System.Diagnostics.Debug.WriteLine(String.Format("GENERATED XHTML = \r\n{0}\r\n=====================", xhtml));
				// There should be only 2 letter headers if both a entries are generated in order
				AssertThatXmlIn.String(xhtml).HasSpecifiedNumberOfMatchesForXpath(letterHeaderXPath, 2);
				var firstHeadwordLoc = xhtml.IndexOf(firstAHeadword, StringComparison.Ordinal);
				var secondHeadwordLoc = xhtml.IndexOf(secondAHeadword, StringComparison.Ordinal);
				var thirdHeadwordLoc = xhtml.IndexOf(bHeadword, StringComparison.Ordinal);
				// The headwords should show up in the xhtml in the given order (firstA, secondA, b)
				Assert.True(firstHeadwordLoc != -1 && firstHeadwordLoc < secondHeadwordLoc && secondHeadwordLoc < thirdHeadwordLoc,
					"Entries generated out of order: first at {0}, second at {1}, third at {2}", firstHeadwordLoc, secondHeadwordLoc, thirdHeadwordLoc);
			}
			finally
			{
				DeleteTempXhtmlAndCssFiles(xhtmlPath);
			}
		}

		[Test]
		public void SavePublishedHtmlWithStyles_MoreEntriesThanLimitProducesPageDivs()
		{
			var firstAEntry = CreateInterestingLexEntry(Cache);
			var firstAHeadword = "alpha1";
			var secondAHeadword = "alpha2";
			var bHeadword = "beta";
			AddHeadwordToEntry(firstAEntry, firstAHeadword, m_wsFr);
			var secondAEntry = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(secondAEntry, secondAHeadword, m_wsFr);
			var bEntry = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(bEntry, bHeadword, m_wsFr);
			int flidVirtual = Cache.ServiceLocator.GetInstance<Virtuals>().LexDbEntries;
			var pubEverything = new DictionaryPublicationDecorator(Cache, (ISilDataAccessManaged)Cache.MainCacheAccessor, flidVirtual);
			var mainHeadwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLHeadWord",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" }),
				CSSClassNameOverride = "entry"
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { mainHeadwordNode },
				FieldDescription = "LexEntry"
			};
			var model = new DictionaryConfigurationModel
			{
				Parts = new List<ConfigurableDictionaryNode> { mainEntryNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(model);
			string xhtmlPath = null;
			const string pagesDivXPath = "//div[@class='pages']";
			const string pageButtonXPath = "//div[@class='pages']/span[@class='pagebutton' and @lang='fr']";
			try
			{
				xhtmlPath = LcmXhtmlGenerator.SavePreviewHtmlWithStyles(new[] { firstAEntry.Hvo, secondAEntry.Hvo, bEntry.Hvo }, pubEverything, model, m_propertyTable, entriesPerPage: 1);
				var xhtml = File.ReadAllText(xhtmlPath);
				AssertThatXmlIn.String(xhtml).HasSpecifiedNumberOfMatchesForXpath(pagesDivXPath, 2);
				AssertThatXmlIn.String(xhtml).HasSpecifiedNumberOfMatchesForXpath(pageButtonXPath, 6);
				var cssPath = Path.ChangeExtension(xhtmlPath, "css");
				var css = File.ReadAllText(cssPath);
				// verify that the css file contains a line similar to: @media screen {
				Assert.That(css, Does.Match(@"@media\s*screen\s*{\s*\.pages\s*{\s*display:\s*table;\s*width:\s*100%;"),
								  "Css for page buttons did not generate a screen-only rule");
				// verify that the css file contains a line similar to: @media print {
				Assert.That(css, Does.Match(@"@media\s*print\s*{\s*\.pages\s*{\s*display:\s*none;\s*}"),
								  "Css for page buttons did not generate a print-only rule");
			}
			finally
			{
				DeleteTempXhtmlAndCssFiles(xhtmlPath);
			}
		}

		[Test]
		public void SavePublishedHtmlWithStyles_ExtraEntriesIncludedInLastPage()
		{
			int[] hvos = new int[21];
			//Generate 21 entries for the test
			for (var i = 0; i < 21; ++i)
			{
				var entry = CreateInterestingLexEntry(Cache);
				AddHeadwordToEntry(entry, "a" + i, m_wsFr);
				hvos[i] = entry.Hvo;
			}
			int flidVirtual = Cache.ServiceLocator.GetInstance<Virtuals>().LexDbEntries;
			var pubEverything = new DictionaryPublicationDecorator(Cache, (ISilDataAccessManaged)Cache.MainCacheAccessor, flidVirtual);
			var mainHeadwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLHeadWord",
				Label = "Headword",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" }),
				CSSClassNameOverride = "entry"
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { mainHeadwordNode },
				FieldDescription = "LexEntry",
				CSSClassNameOverride = "entry"
			};
			var model = new DictionaryConfigurationModel
			{
				Parts = new List<ConfigurableDictionaryNode> { mainEntryNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(model);
			string xhtmlPath = null;
			const string pagesDivXPath = "//div[@class='pages']";
			const string pageButtonXPath = "//div[@class='pages']/span[@class='pagebutton']";
			const string pageButtonLastIndexPath = "//div[@class='pages']/span[@class='pagebutton' and @endIndex='20']";
			const string entryDivXPath = "//div[@class='entry']";
			try
			{
				xhtmlPath = LcmXhtmlGenerator.SavePreviewHtmlWithStyles(hvos, pubEverything, model, m_propertyTable, entriesPerPage: 10);
				var xhtml = File.ReadAllText(xhtmlPath);
				AssertThatXmlIn.String(xhtml).HasSpecifiedNumberOfMatchesForXpath(pagesDivXPath, 2);
				AssertThatXmlIn.String(xhtml).HasSpecifiedNumberOfMatchesForXpath(pageButtonXPath, 4); // 2 page buttons (top and bottom)
				AssertThatXmlIn.String(xhtml).HasSpecifiedNumberOfMatchesForXpath(pageButtonLastIndexPath, 2); // last page includes the last entry
				AssertThatXmlIn.String(xhtml).HasSpecifiedNumberOfMatchesForXpath(entryDivXPath, 10); // 10 entries generated on first page
			}
			finally
			{
				DeleteTempXhtmlAndCssFiles(xhtmlPath);
			}
		}

		[Test]
		public void SavePublishedHtmlWithStyles_ExtraEntriesMoreThanTenPercentGetOwnPage()
		{
			int[] hvos = new int[21];
			//Generate 21 entries for the test
			for (var i = 0; i < 21; ++i)
			{
				hvos[i] = CreateInterestingLexEntry(Cache, "a" + i).Hvo;
			}
			int flidVirtual = Cache.ServiceLocator.GetInstance<Virtuals>().LexDbEntries;
			var pubEverything = new DictionaryPublicationDecorator(Cache, (ISilDataAccessManaged)Cache.MainCacheAccessor, flidVirtual);
			var mainHeadwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLHeadWord",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" }),
				CSSClassNameOverride = "entry"
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { mainHeadwordNode },
				FieldDescription = "LexEntry",
				CSSClassNameOverride = "entry"
			};
			var model = new DictionaryConfigurationModel
			{
				Parts = new List<ConfigurableDictionaryNode> { mainEntryNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(model);
			string xhtmlPath = null;
			const string pagesDivXPath = "//div[@class='pages']";
			const string pageButtonXPath = "//div[@class='pages']/span[@class='pagebutton']";
			const string firstPageButtonXPath = "//div[@class='pages']/span[@class='pagebutton' and @id='currentPageButton' and @startIndex='0' and @endIndex='7']";
			const string lastPageButtonXPath = "//div[@class='pages']/span[@class='pagebutton' and @startIndex='16' and @endIndex='20']";
			const string entryXPath = "//div[@class='entry']";
			try
			{
				xhtmlPath = LcmXhtmlGenerator.SavePreviewHtmlWithStyles(hvos, pubEverything, model, m_propertyTable, entriesPerPage: 8);
				var xhtml = File.ReadAllText(xhtmlPath);
				AssertThatXmlIn.String(xhtml).HasSpecifiedNumberOfMatchesForXpath(pagesDivXPath, 2);
				AssertThatXmlIn.String(xhtml).HasSpecifiedNumberOfMatchesForXpath(pageButtonXPath, 6); // 3 pages on top and bottom
				AssertThatXmlIn.String(xhtml).HasSpecifiedNumberOfMatchesForXpath(firstPageButtonXPath, 2);
				AssertThatXmlIn.String(xhtml).HasSpecifiedNumberOfMatchesForXpath(lastPageButtonXPath, 2);
				AssertThatXmlIn.String(xhtml).HasSpecifiedNumberOfMatchesForXpath(entryXPath, 8); // 8 entries per page
			}
			finally
			{
				DeleteTempXhtmlAndCssFiles(xhtmlPath);
			}
		}

		[Test]
		public void SavePublishedHtmlWithStyles_ZeroEntriesDoesNotThrow()
		{
			var hvos = new int[0];
			var flidVirtual = Cache.ServiceLocator.GetInstance<Virtuals>().LexDbEntries;
			var pubEverything = new DictionaryPublicationDecorator(Cache, (ISilDataAccessManaged)Cache.MainCacheAccessor, flidVirtual);
			var mainHeadwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLHeadWord",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { mainHeadwordNode },
				FieldDescription = "LexEntry",
				CSSClassNameOverride = "entry"
			};
			var model = new DictionaryConfigurationModel
			{
				Parts = new List<ConfigurableDictionaryNode> { mainEntryNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(model);
			string xhtmlPath = null;
			try
			{
				xhtmlPath = LcmXhtmlGenerator.SavePreviewHtmlWithStyles(hvos, pubEverything, model, m_propertyTable, entriesPerPage: 8);
				var xhtml = File.ReadAllText(xhtmlPath);
				AssertThatXmlIn.String(xhtml).HasSpecifiedNumberOfMatchesForXpath("//div[@entry]", 0);
				AssertThatXmlIn.String(xhtml).HasSpecifiedNumberOfMatchesForXpath("//*[@page]", 0);
			}
			finally
			{
				DeleteTempXhtmlAndCssFiles(xhtmlPath);
			}
		}

		[Test]
		public void SavePublishedHtmlWithStyles_ProducesDocumentTitle()
		{
			var entry = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(entry, "femme", m_wsFr);
			const string configName = "Test Config Name";
			var model = CreateInterestingConfigurationModel(Cache, m_propertyTable);
			model.FilePath = "/nowhere/" + configName + DictionaryConfigurationModel.FileExtension;
			string xhtmlPath = null;
			try
			{
				//SUT
				xhtmlPath = LcmXhtmlGenerator.SavePreviewHtmlWithStyles(new[] { entry.Hvo }, null, model, m_propertyTable);
				// Since this is for the LexEdit Preview, the config name will be appended with '-Preview'
				// Note: because the project name is the file name, and there is no file behind our cache, Name is the empty string
				var xpath = $"/html/head/title[text()='{configName}-Preview - {Cache.ProjectId.Name}']";
				AssertThatXmlIn.File(xhtmlPath).HasSpecifiedNumberOfMatchesForXpath(xpath, 1);
			}
			finally
			{
				DeleteTempXhtmlAndCssFiles(xhtmlPath);
			}
		}

		[Test]
		public void CheckSubsenseOutput()
		{
			var posNoun = CreatePartOfSpeech("noun", "n");
			var posAdj = CreatePartOfSpeech("adjective", "adj");

			var firstHeadword = "homme";
			var firstEntry = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(firstEntry, firstHeadword, m_wsFr);
			AddSingleSubSenseToSense("man", firstEntry.SensesOS[0]);
			var msa1 = CreateMSA(firstEntry, posNoun);
			firstEntry.SensesOS[0].MorphoSyntaxAnalysisRA = msa1;
			firstEntry.SensesOS[0].SensesOS[0].MorphoSyntaxAnalysisRA = msa1;

			var secondHeadword = "femme";
			var secondEntry = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(secondEntry, secondHeadword, m_wsFr);
			AddSenseAndTwoSubsensesToEntry(secondEntry, "woman");
			var msa2 = CreateMSA(secondEntry, posNoun);
			foreach (var sense in secondEntry.SensesOS)
			{
				sense.MorphoSyntaxAnalysisRA = msa2;
				foreach (var sub in sense.SensesOS)
					sub.MorphoSyntaxAnalysisRA = msa2;
			}

			var thirdHeadword = "bon";
			var thirdEntry = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(thirdEntry, thirdHeadword, m_wsFr);
			AddSenseAndTwoSubsensesToEntry(thirdEntry, "good");
			var msa3 = CreateMSA(thirdEntry, posAdj);
			foreach (var sense in thirdEntry.SensesOS)
			{
				sense.MorphoSyntaxAnalysisRA = msa3;
				foreach (var sub in sense.SensesOS)
					sub.MorphoSyntaxAnalysisRA = msa3;
			}
			var msa4 = CreateMSA(thirdEntry, posNoun);
			thirdEntry.SensesOS[1].SensesOS[1].MorphoSyntaxAnalysisRA = msa4;

			int flidVirtual = Cache.ServiceLocator.GetInstance<Virtuals>().LexDbEntries;
			var pubEverything = new DictionaryPublicationDecorator(Cache, (ISilDataAccessManaged)Cache.MainCacheAccessor, flidVirtual);

			var subCategNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLPartOfSpeech",
				DictionaryNodeOptions =
					GetWsOptionsForLanguages(new[] { "analysis" }, DictionaryNodeWritingSystemOptions.WritingSystemType.Analysis),
				Children = new List<ConfigurableDictionaryNode>()
			};
			var subGramInfoNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MorphoSyntaxAnalysisRA",
				CSSClassNameOverride = "morphosyntaxanalysis",
				Children = new List<ConfigurableDictionaryNode> { subCategNode }
			};
			var subGlossNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Gloss",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" })
			};
			var subSenseNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions
				{
					NumberEvenASingleSense = false,
					ShowSharedGrammarInfoFirst = true
				},
				Children = new List<ConfigurableDictionaryNode> { subGramInfoNode, subGlossNode }
			};
			var categNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLPartOfSpeech",
				DictionaryNodeOptions =
					GetWsOptionsForLanguages(new[] { "analysis" }, DictionaryNodeWritingSystemOptions.WritingSystemType.Analysis),
				Children = new List<ConfigurableDictionaryNode>()
			};
			var gramInfoNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MorphoSyntaxAnalysisRA",
				CSSClassNameOverride = "morphosyntaxanalysis",
				Children = new List<ConfigurableDictionaryNode> { categNode }
			};
			var glossNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Gloss",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" })
			};
			var senseNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions
				{
					DisplayEachSenseInAParagraph = true,
					NumberEvenASingleSense = false,
					ShowSharedGrammarInfoFirst = true
				},
				Children = new List<ConfigurableDictionaryNode> { gramInfoNode, glossNode, subSenseNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { senseNode },
				FieldDescription = "LexEntry"
			};
			var model = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { mainEntryNode } };
			CssGeneratorTests.PopulateFieldsForTesting(model);

			string xhtmlPath = null;
			var letterHeaderXPath = "//div[@class='letHead']";
			try
			{
				xhtmlPath = LcmXhtmlGenerator.SavePreviewHtmlWithStyles(new[] { thirdEntry.Hvo, secondEntry.Hvo, firstEntry.Hvo }, pubEverything, model, m_propertyTable);
				var xhtml = File.ReadAllText(xhtmlPath);
				//System.Diagnostics.Debug.WriteLine(String.Format("GENERATED XHTML = \r\n{0}\r\n=====================", xhtml));
				// SUT
				const string allCategsPath = "//span[@class='morphosyntaxanalysis']/span[@class='mlpartofspeech']/span[@lang='en']";
				const string firstCategPath = "/html/body/div[@class='lexentry']/span[@class='senses']//span[@class='sensecontent']/span[@class='sense']/span[@class='morphosyntaxanalysis']/span[@class='mlpartofspeech']/span[@lang='en' and text()='n']";
				const string secondCategPath = "/html/body/div[@class='lexentry']/span[@class='senses']/span[@class='sharedgrammaticalinfo']/span[@class='morphosyntaxanalysis']/span[@class='mlpartofspeech']/span[@lang='en' and text()='n']";
				const string thirdCategPath = "/html/body/div[@class='lexentry']/span[@class='senses']/span[@class='sharedgrammaticalinfo']/span[@class='morphosyntaxanalysis']/span[@class='mlpartofspeech']/span[@lang='en' and text()='adj']";
				const string fourthCategPath = "/html/body/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense']/span[@class='morphosyntaxanalysis']/span[@class='mlpartofspeech']/span[@lang='en' and text()='adj']";
				const string fifthCategPath = "/html/body/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense']/span[@class='morphosyntaxanalysis']/span[@class='mlpartofspeech']/span[@lang='en' and text()='n']";

				AssertThatXmlIn.String(xhtml).HasSpecifiedNumberOfMatchesForXpath(allCategsPath, 5);
				AssertThatXmlIn.String(xhtml).HasSpecifiedNumberOfMatchesForXpath(firstCategPath, 1);
				AssertThatXmlIn.String(xhtml).HasSpecifiedNumberOfMatchesForXpath(secondCategPath, 2);
				AssertThatXmlIn.String(xhtml).HasSpecifiedNumberOfMatchesForXpath(thirdCategPath, 1);
				AssertThatXmlIn.String(xhtml).HasSpecifiedNumberOfMatchesForXpath(fourthCategPath, 1);
				AssertThatXmlIn.String(xhtml).HasSpecifiedNumberOfMatchesForXpath(fifthCategPath, 1);
			}
			finally
			{
				DeleteTempXhtmlAndCssFiles(xhtmlPath);
			}
		}

		[Test]
		public void SavePublishedHtmlWithStyles_DoesNotThrowIfFileIsLocked()
		{
			var entries = new int[0];
			var model = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode>() };
			var preferredPath = LcmXhtmlGenerator.SavePreviewHtmlWithStyles(entries, null, model, m_propertyTable); // to get the preferred path
			var actualPath = preferredPath;
			try
			{
				using (new StreamWriter(preferredPath)) // lock the preferred path
				{
					Assert.DoesNotThrow(() => actualPath = LcmXhtmlGenerator.SavePreviewHtmlWithStyles(entries, null, model, m_propertyTable));
				}
				Assert.AreNotEqual(preferredPath, actualPath, "Should have saved to a different path.");
			}
			finally
			{
				DeleteTempXhtmlAndCssFiles(preferredPath);
				DeleteTempXhtmlAndCssFiles(actualPath);
			}
		}

		[Test]
		public void SavePublishedHtmlWithCustomCssFile()
		{
			var entries = new int[0];
			var model = new DictionaryConfigurationModel
			{
				Parts = new List<ConfigurableDictionaryNode>(),
				FilePath = Path.Combine(DictionaryConfigurationListener.GetProjectConfigurationDirectory(m_propertyTable),
										"filename" + DictionaryConfigurationModel.FileExtension)
			};
			var xhtmlPath = LcmXhtmlGenerator.SavePreviewHtmlWithStyles(entries, null, model, m_propertyTable);
			try
			{
				var previewXhtmlContent = File.ReadAllText(xhtmlPath);
				// ReSharper disable once AssignNullToNotNullAttribute -- Justification: XHTML is always saved in a directory
				var fileName = "ProjectDictionaryOverrides.css";
				StringAssert.Contains(fileName, previewXhtmlContent, "Custom css file should added in the XHTML file");
			}
			finally
			{
				DeleteTempXhtmlAndCssFiles(xhtmlPath);
			}
		}

		[Test]
		public void GenerateContentForEntry_EmbeddedWritingSystemGeneratesCorrectResult()
		{
			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Bibliography",
				CSSClassNameOverride = "bib",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr", "en" })
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { headwordNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var entry = CreateInterestingLexEntry(Cache);
			var englishStr = TsStringUtils.MakeString("English", m_wsEn);
			var frenchString = TsStringUtils.MakeString("French with  embedded", m_wsFr);
			var multiRunString = frenchString.Insert(12, englishStr);
			entry.Bibliography.set_String(m_wsFr, multiRunString);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entry, mainEntryNode, null, DefaultSettings).ToString();
			const string nestedEn = "/div[@class='lexentry']/span[@class='bib']/span[@lang='fr']/span[@lang='en']";
			const string nestedFr = "/div[@class='lexentry']/span[@class='bib']/span[@lang='fr']/span[@lang='fr']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(nestedEn, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(nestedFr, 2);
		}

		[Test]
		public void GenerateContentForEntry_EmbeddedWritingSystemOfOppositeDirectionGeneratesCorrectResult()
		{
			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Bibliography",
				CSSClassNameOverride = "bib",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "he" })
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { headwordNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var entry = CreateInterestingLexEntry(Cache);
			var multiRunString = MakeBidirectionalTss(new[] { "דוד", " et ", "דניאל" }, Cache);
			var wsHe = Cache.ServiceLocator.WritingSystemManager.GetWsFromStr("he");
			entry.Bibliography.set_String(wsHe, multiRunString);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entry, mainEntryNode, null, DefaultSettings).ToString();
			const string nestedEn = "/div[@class='lexentry']/span[@class='bib']/span[@lang='he']/span[@dir='rtl']/span[@lang='en']/span[@dir='ltr']";
			const string nestedHe = "/div[@class='lexentry']/span[@class='bib']/span[@lang='he']/span[@dir='rtl']/span[@lang='he']";
			const string extraDirection = "/div[@class='lexentry']/span[@class='bib']/span[@lang='he']/span[@dir='rtl']/span[@lang='he']/span[@dir='rtl']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(nestedEn, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(nestedHe, 2);
			AssertThatXmlIn.String(result).HasNoMatchForXpath(extraDirection);
		}

		[Test]
		public void GenerateContentForEntry_WritingSystemOfSameDirectionGeneratesNoExtraDirectionSpan()
		{
			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Bibliography",
				CSSClassNameOverride = "bib",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "he" })
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { headwordNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var entry = CreateInterestingLexEntry(Cache);
			var multiRunString = MakeBidirectionalTss(new[] { "ירמיהו", " was a bullfrog." }, Cache);
			var wsHe = Cache.ServiceLocator.WritingSystemManager.GetWsFromStr("he");
			entry.Bibliography.set_String(wsHe, multiRunString);
			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null, true); // Right-to-Left
																															 //SUT
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entry, mainEntryNode, null, settings).ToString();
			const string nestedEn = "/div[@class='lexentry']/span[@class='bib']/span[@lang='he']/span[@lang='en']/span[@dir='ltr']";
			const string nestedHe = "/div[@class='lexentry']/span[@class='bib']/span[@lang='he']/span[@lang='he']";
			const string extraDirection0 = "/div[@class='lexentry']/span[@class='bib']/span[@lang='he']/span[@dir='rtl']";
			const string extraDirection1 = "/div[@class='lexentry']/span[@class='bib']/span[@lang='he']/span[@lang='he']/span[@dir='rtl']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(nestedEn, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(nestedHe, 1);
			AssertThatXmlIn.String(result).HasNoMatchForXpath(extraDirection0);
			AssertThatXmlIn.String(result).HasNoMatchForXpath(extraDirection1);
		}

	  [Test]
	  public void GenerateContentForEntry_EmbeddedHyperlinkGeneratesAnchor()
	  {
		  var headwordNode = new ConfigurableDictionaryNode
		  {
			  FieldDescription = "Bibliography",
			  CSSClassNameOverride = "bib",
			  DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
		  };
		  var mainEntryNode = new ConfigurableDictionaryNode
		  {
			  Children = new List<ConfigurableDictionaryNode> { headwordNode },
			  FieldDescription = "LexEntry"
		  };
		  CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
		  var entry = CreateInterestingLexEntry(Cache);
		  var multiRunString = MakeVernTss("a link", Cache);
		  var stringBldr = multiRunString.GetBldr();
		  // Set the hyperlink style.
		  stringBldr.SetStrPropValue(2, stringBldr.Length, (int)FwTextPropType.ktptNamedStyle, "Hyperlink");
		  // Set the hyperlink data
		  const string testUrl = "https://software.sil.org/fieldworks";
		  // Note: There is a little wart stored in the front of external links in the string properties
		  stringBldr.SetStrPropValue(2, stringBldr.Length, (int)FwTextPropType.ktptObjData,
			  (char)FwObjDataTypes.kodtExternalPathName + testUrl);
		  entry.Bibliography.set_String(m_wsFr, stringBldr.GetString());
		  // SUT
		  var result = ConfiguredLcmGenerator.GenerateContentForEntry(entry, mainEntryNode, null, DefaultSettings).ToString();
		  string nestedLink = $"/div[@class='lexentry']/span[@class='bib']/span/span/a[@href='{testUrl}']";
		  AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(nestedLink, 1);
	  }

	  private const string crossRefOwnerTypeXpath =
			"//span[@class='minimallexreferences']/span[@class='minimallexreference']/span[@class='ownertype_name']";

		private static string CrossRefOwnerTypeXpath(string type)
		{
			return string.Format("//span[@class='minimallexreferences']/span[@class='minimallexreference']/span[@class='ownertype_name']" +
				"/span[@lang='en' and text()='{0}']", type);
		}

		private static string HeadwordOrderInCrossRefsXpath(int position, string headword)
		{
			return string.Format("//span[@class='minimallexreferences']/span[@class='minimallexreference']/span[@class='configtargets']" +
				"/span[@class='configtarget' and position()='{0}']/span/span/a[text()='{1}']", position, headword);
		}

		private static string HeadwordWsInCrossRefsXpath(string ws, string headword) // REVIEW (Hasso) 2017.04: move these helpers to Helpers?
		{
			return string.Format("//span[@class='minimallexreferences']/span[@class='minimallexreference']/span[@class='configtargets']" +
				"/span[@class='configtarget']/span/span[@lang='{0}']/a[text()='{1}']", ws, headword);
		}

		[Test]
		public void GenerateContentForEntry_CompareRelations_SimpleSituations_SortByHeadword([Values(true, false)] bool SeparateReferences)
		{
			var mainEntry = CreateInterestingLexEntry(Cache, "MainEntry");
			var compareReferencedEntry1 = CreateInterestingLexEntry(Cache, "b", "b comparable");
			var compareReferencedEntry2 = CreateInterestingLexEntry(Cache, "a", "a comparable");
			var compareReferencedEntry3 = CreateInterestingLexEntry(Cache, "c", "c comparable");
			const string comRefTypeName = "Compare";
			var comRefType = CreateLexRefType(Cache, LexRefTypeTags.MappingTypes.kmtEntryCollection, comRefTypeName, "cf", string.Empty, string.Empty);
			if (SeparateReferences)
			{
				const string Guid1 = "11111111-f1ac-4950-8562-4d617e0ace18";
				const string Guid2 = "22222222-e929-4202-8886-b156a4c035f5";
				const string Guid3 = "33333333-9a03-49bc-8375-2ab9bafbc90b";

				// these have specific Guids so we know they would be in this order if ordered by Guid
				CreateLexReference(comRefType, new List<ICmObject> { mainEntry, compareReferencedEntry1 }, new Guid(Guid1));
				CreateLexReference(comRefType, new List<ICmObject> { mainEntry, compareReferencedEntry2 }, new Guid(Guid2));
				CreateLexReference(comRefType, new List<ICmObject> { mainEntry, compareReferencedEntry3 }, new Guid(Guid3));
			}
			else
			{
				CreateLexReference(comRefType, new[] { mainEntry, compareReferencedEntry1, compareReferencedEntry2, compareReferencedEntry3 });
			}

			var mainEntryNode = ModelForCrossReferences(new[] { comRefType.Guid.ToString() });
			// SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(mainEntry, mainEntryNode, null, DefaultSettings).ToString();
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(crossRefOwnerTypeXpath, 1); // ensure there is only one
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(CrossRefOwnerTypeXpath(comRefTypeName), 1); // ...the *correct* one
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(HeadwordOrderInCrossRefsXpath(1, "a"), 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(HeadwordOrderInCrossRefsXpath(2, "b"), 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(HeadwordOrderInCrossRefsXpath(3, "c"), 1);
		}

		[Test]
		public void GenerateContentForEntry_CompareRelations_ComplexSituation_SortByHeadword()
		{
			var mainEntry = CreateInterestingLexEntry(Cache, "MainEntry");
			var compareReferencedEntry1 = CreateInterestingLexEntry(Cache, "b", "b comparable");
			var compareReferencedEntry2 = CreateInterestingLexEntry(Cache, "a", "a comparable");
			var compareReferencedEntry3 = CreateInterestingLexEntry(Cache, "c", "c comparable");
			var compareReferencedEntry4 = CreateInterestingLexEntry(Cache, "ba", "ba comparable");
			var compareReferencedEntry5 = CreateInterestingLexEntry(Cache, "ca", "ca comparable");
			const string comRefTypeName = "Compare";
			var comRefType = CreateLexRefType(Cache, LexRefTypeTags.MappingTypes.kmtEntryCollection, comRefTypeName, "cf", string.Empty, string.Empty);
			CreateLexReference(comRefType, new List<ICmObject> { mainEntry, compareReferencedEntry3, compareReferencedEntry2 });
			CreateLexReference(comRefType, new List<ICmObject> { mainEntry, compareReferencedEntry5, compareReferencedEntry4, compareReferencedEntry1 });
			Assert.That(comRefType, Is.Not.Null);

			var mainEntryNode = ModelForCrossReferences(new[] { comRefType.Guid.ToString() });
			// SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(mainEntry, mainEntryNode, null, DefaultSettings).ToString();
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(CrossRefOwnerTypeXpath(comRefTypeName), 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(HeadwordOrderInCrossRefsXpath(1, "a"), 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(HeadwordOrderInCrossRefsXpath(2, "b"), 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(HeadwordOrderInCrossRefsXpath(3, "ba"), 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(HeadwordOrderInCrossRefsXpath(4, "c"), 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(HeadwordOrderInCrossRefsXpath(5, "ca"), 1);
		}

		[Test]
		public void GenerateContentForEntry_CrossRefs_Sequences_SequencePreserved()
		{
			var alphaEntry = CreateInterestingLexEntry(Cache, "alpha", "alpha");
			var redEntry = CreateInterestingLexEntry(Cache, "rouge", "red");
			var greenEntry = CreateInterestingLexEntry(Cache, "vert", "green");
			var blueEntry = CreateInterestingLexEntry(Cache, "bleu", "blue");
			var midAlphabetEntry = CreateInterestingLexEntry(Cache, "omega", "middle of the Roman alphabet; we're not testing Greek :-)");
			const string colorTypeName = "Color";
			var colorType = CreateLexRefType(Cache, LexRefTypeTags.MappingTypes.kmtEntrySequence, colorTypeName, "col", string.Empty, string.Empty);
			CreateLexReference(colorType, new List<ICmObject> { alphaEntry, redEntry, greenEntry, blueEntry });
			const string greekTypeName = "Greek";
			var greekType = CreateLexRefType(Cache, LexRefTypeTags.MappingTypes.kmtEntrySequence, greekTypeName, "grk", string.Empty, string.Empty);
			CreateLexReference(greekType, new List<ICmObject> { alphaEntry, midAlphabetEntry });

			var mainEntryNode = ModelForCrossReferences(new[] { colorType.Guid.ToString(), greekType.Guid.ToString() });
			// SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(alphaEntry, mainEntryNode, null, DefaultSettings).ToString();
			// first sequence: colors: ARGB
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(CrossRefOwnerTypeXpath(colorTypeName), 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(HeadwordOrderInCrossRefsXpath(1, "alpha"), 2); // the first in both
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(HeadwordOrderInCrossRefsXpath(2, "rouge"), 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(HeadwordOrderInCrossRefsXpath(3, "vert"), 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(HeadwordOrderInCrossRefsXpath(4, "bleu"), 1);
			// second sequence: greek letters (ok, not *all* of them): AΩ
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(CrossRefOwnerTypeXpath(greekTypeName), 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(HeadwordOrderInCrossRefsXpath(2, "omega"), 1);
		}

		[Test]
		public void GenerateContentForEntry_CrossRefs_Unidirectional_SequencePreserved()
		{
			var stoogesEntry = CreateInterestingLexEntry(Cache, "Stooges");
			var larryEntry = CreateInterestingLexEntry(Cache, "Larry");
			var curlyEntry = CreateInterestingLexEntry(Cache, "Curly");
			var moeEntry = CreateInterestingLexEntry(Cache, "Moe");
			const string characterTypeName = "Character";
			var characterType = CreateLexRefType(Cache, LexRefTypeTags.MappingTypes.kmtEntryUnidirectional, characterTypeName, "char", string.Empty, string.Empty);
			CreateLexReference(characterType, new List<ICmObject> { stoogesEntry, larryEntry, curlyEntry, moeEntry });

			var mainEntryNode = ModelForCrossReferences(new[] { characterType.Guid + ":f" });
			// SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(stoogesEntry, mainEntryNode, null, DefaultSettings).ToString();
			// sequence of Stooges:
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(CrossRefOwnerTypeXpath(characterTypeName), 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(HeadwordOrderInCrossRefsXpath(1, "Larry"), 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(HeadwordOrderInCrossRefsXpath(2, "Curly"), 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(HeadwordOrderInCrossRefsXpath(3, "Moe"), 1);
			AssertThatXmlIn.String(result).HasNoMatchForXpath(HeadwordOrderInCrossRefsXpath(4, "Stooges")); // Unidirectional excludes the owner
		}

		private static ConfigurableDictionaryNode ModelForCrossReferences(IEnumerable<string> types)
		{
			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "HeadWordRef",
				CSSClassNameOverride = "headword",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr", "en" }, DictionaryNodeWritingSystemOptions.WritingSystemType.Vernacular)
			};
			var targetsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ConfigTargets",
				Children = new List<ConfigurableDictionaryNode> { headwordNode }
			};
			var nameNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwnerType",
				SubField = "Name",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" })
			};
			var crossReferencesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MinimalLexReferences",
				DictionaryNodeOptions = new DictionaryNodeListOptions
				{
					ListId = DictionaryNodeListOptions.ListIds.Entry,
					Options = DictionaryDetailsControllerTests.ListOfEnabledDNOsFromStrings(types)
				},
				Children = new List<ConfigurableDictionaryNode> { nameNode, targetsNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { crossReferencesNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			return mainEntryNode;
		}

		/// <summary>
		/// This tests the fixes for
		/// - LT-16504: Lexical References should be sorted by LexRefType in the order specified in the configuration
		/// - LT-17384: Lexical References should be in the same order every time
		///   (we accomplish this by sorting by Headword within each LexRefType)
		/// - LT-18294: Relation Abbrev and Relation Name Configuration order ignored
		/// Intermittent failures should NOT be ignored.
		/// </summary>
		[Test]
		public void GenerateContentForEntry_LexicalReferencesOrderedCorrectly([Values(true, false)] bool usingSubfield)
		{
			var manEntry = CreateInterestingLexEntry(Cache, "homme", "man");
			var womanEntry = CreateInterestingLexEntry(Cache, "femme", "woman");
			var familyEntry = CreateInterestingLexEntry(Cache, "famille", "family");
			var boyEntry = CreateInterestingLexEntry(Cache, "garçon", "boy");
			var girlEntry = CreateInterestingLexEntry(Cache, "fille", "girl");
			var individualEntry = CreateInterestingLexEntry(Cache, "individuel", "individual");
			var thingEntry = CreateInterestingLexEntry(Cache, "truc", "thing");
			var beastEntry = CreateInterestingLexEntry(Cache, "bête", "beast");
			var armEntry = CreateInterestingLexEntry(Cache, "bras", "arm");
			var legEntry = CreateInterestingLexEntry(Cache, "jambe", "leg");

			var antonyms = CreateLexRefType(Cache, LexRefTypeTags.MappingTypes.kmtEntryPair, "Antonym", "ant", null, null);
			CreateLexReference(antonyms, new[] { manEntry, womanEntry });
			CreateLexReference(antonyms, new[] { manEntry, boyEntry });
			CreateLexReference(antonyms, new[] { manEntry, thingEntry });
			CreateLexReference(antonyms, new[] { manEntry, beastEntry });
			CreateLexReference(antonyms, new[] { familyEntry, individualEntry });

			var wholeparts = CreateLexRefType(Cache, LexRefTypeTags.MappingTypes.kmtEntryTree, "Part", "pt", "Whole", "wh");
			CreateLexReference(wholeparts, new[] { familyEntry, manEntry, womanEntry, boyEntry, girlEntry });
			// Girl is both a whole and a part, but has no other refs. When these targets are alphabetized by headword, their types alternate.
			CreateLexReference(wholeparts, new[] { girlEntry, armEntry, legEntry });


			var refHeadwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "HeadWord",
				DictionaryNodeOptions =
					GetWsOptionsForLanguages(new[] { "vernacular" }, DictionaryNodeWritingSystemOptions.WritingSystemType.Vernacular)
			};
			var targetsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ConfigTargets",
				Between = ", ",
				Children = new List<ConfigurableDictionaryNode> { refHeadwordNode }
			};
			var relNameNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwnerType",
				SubField = "Name",
				After = ": ",
				DictionaryNodeOptions =
					GetWsOptionsForLanguages(new[] { "analysis" }, DictionaryNodeWritingSystemOptions.WritingSystemType.Analysis)
			};
			var relAbbrNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwnerType",
				SubField = "Abbreviation",
				After = ": ",
				DictionaryNodeOptions =
					GetWsOptionsForLanguages(new[] { "analysis" }, DictionaryNodeWritingSystemOptions.WritingSystemType.Analysis)
			};
			var relationsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MinimalLexReferences",
				CSSClassNameOverride = "lexrefs",
				Between = "; ",
				DictionaryNodeOptions = GetListOptionsForStrings(DictionaryNodeListOptions.ListIds.Sense, new[]
					{
							wholeparts.Guid + ":r",
							antonyms.Guid.ToString(),
							wholeparts.Guid + ":f"
						}),
				Children = new List<ConfigurableDictionaryNode> { relAbbrNode, relNameNode, targetsNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { relationsNode }
			};
			var xpathLexRef = "//div/span[@class='lexrefs']/span[@class='lexref']";
			if (usingSubfield)
			{
				// If we are testing subfields, insert 'SensesOS->Entry', which returns the same data, but allows us to make LexRefs a subfield.
				relationsNode.SubField = relationsNode.FieldDescription;
				relationsNode.FieldDescription = "Entry";
				var senseNode = new ConfigurableDictionaryNode
				{
					FieldDescription = "SensesOS",
					CSSClassNameOverride = "senses",
					DictionaryNodeOptions = new DictionaryNodeSenseOptions
					{
						DisplayEachSenseInAParagraph = false,
						NumberEvenASingleSense = false,
						ShowSharedGrammarInfoFirst = false
					},
					Children = mainEntryNode.Children
				};
				mainEntryNode.Children = new List<ConfigurableDictionaryNode> { senseNode };
				xpathLexRef = "//div/span[@class='senses']/span[@class='sensecontent']/span[@class='sense']/span[@class='lexrefs']/span[@class='lexref']";
			}
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var settings = DefaultSettings;
			string antAbbrSpan = $"<span class=\"ownertype_abbreviation\"><span nodeId=\"{relAbbrNode.GetHashCode()}\" lang=\"en\">ant</span></span>";
			string whSpan = $"<span class=\"ownertype_abbreviation\"><span nodeId=\"{relAbbrNode.GetHashCode()}\" lang=\"en\">wh</span></span>";
			string ptSpan = $"<span class=\"ownertype_abbreviation\"><span nodeId=\"{relAbbrNode.GetHashCode()}\" lang=\"en\">pt</span></span>";
			string antNameSpan = $"<span class=\"ownertype_name\"><span nodeId=\"{relNameNode.GetHashCode()}\" lang=\"en\">Antonym</span></span>";
			string femmeSpan = $"<span class=\"headword\"><span nodeId=\"{refHeadwordNode.GetHashCode()}\" lang=\"fr\">femme</span></span>";
			var garçonSpan = TsStringUtils.Compose($"<span class=\"headword\"><span nodeId=\"{refHeadwordNode.GetHashCode()}\" lang=\"fr\">garçon</span></span>");
			var bêteSpan = TsStringUtils.Compose($"<span class=\"headword\"><span nodeId=\"{refHeadwordNode.GetHashCode()}\" lang=\"fr\">bête</span></span>");
			string trucSpan = $"<span class=\"headword\"><span nodeId=\"{refHeadwordNode.GetHashCode()}\" lang=\"fr\">truc</span></span>";
			//SUT
			//Console.WriteLine(LcmXhtmlGenerator.SavePreviewHtmlWithStyles(new[] { manEntry.Hvo, familyEntry.Hvo, girlEntry.Hvo, individualEntry.Hvo }, null,
			//	new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { mainEntryNode } }, m_mediator)); // full output for diagnostics
			var manResult = ConfiguredLcmGenerator.GenerateContentForEntry(manEntry, mainEntryNode, null, settings).ToString();
			AssertThatXmlIn.String(manResult).HasSpecifiedNumberOfMatchesForXpath(xpathLexRef, 2); // antonyms are grouped into one span
			var idxAntonymAbbr = manResult.IndexOf(antAbbrSpan, StringComparison.Ordinal);
			var idxWhole = manResult.IndexOf(whSpan, StringComparison.Ordinal);
			var idxPart = manResult.IndexOf(ptSpan, StringComparison.Ordinal);
			var idxAntonymName = manResult.IndexOf(antNameSpan, StringComparison.Ordinal);
			Assert.Less(0, idxAntonymAbbr, "Antonym abbreviation relation should exist for homme (man)");
			Assert.Less(0, idxWhole, "Whole relation should exist for homme (man)");
			Assert.AreEqual(-1, idxPart, "Part relation should not exist for homme (man)");
			Assert.Less(idxWhole, idxAntonymAbbr, "Whole relation should come before Antonym relation for homme (man)");
			Assert.Less(idxAntonymAbbr, idxAntonymName, "Antonym name should exist after Antonym abbreviation");
			var idxFemme = manResult.IndexOf(femmeSpan, StringComparison.Ordinal);
			var idxGarcon = manResult.IndexOf(garçonSpan, StringComparison.Ordinal);
			var idxBete = manResult.IndexOf(bêteSpan, StringComparison.Ordinal);
			var idxTruc = manResult.IndexOf(trucSpan, StringComparison.Ordinal);
			// LT-15764 The Antonyms are now sorted by Headword
			Assert.Less(idxAntonymAbbr, idxBete);
			Assert.Less(idxBete, idxFemme);
			Assert.Less(idxFemme, idxGarcon);
			Assert.Less(idxGarcon, idxTruc);
			Assert.Less(idxAntonymAbbr, idxAntonymName, "Antonym name should come after Antonym abbreviation");
			Assert.Less(idxAntonymName, idxBete, "Target entry should come after Antonym name");

			// Ignore if usingSubfield. Justification: Part-Whole direction is miscalculated for field=Entry, subfield=MinimalLexReferences (LT-17571)
			if (!usingSubfield)
			{
				var familyResult = ConfiguredLcmGenerator.GenerateContentForEntry(familyEntry, mainEntryNode, null, settings).ToString();
				AssertThatXmlIn.String(familyResult).HasSpecifiedNumberOfMatchesForXpath(xpathLexRef, 2);
				idxAntonymAbbr = familyResult.IndexOf(antAbbrSpan, StringComparison.Ordinal);
				idxWhole = familyResult.IndexOf(whSpan, StringComparison.Ordinal);
				idxPart = familyResult.IndexOf(ptSpan, StringComparison.Ordinal);
				idxAntonymName = familyResult.IndexOf(antNameSpan, StringComparison.Ordinal);
				Assert.Less(0, idxAntonymAbbr, "Antonym abbreviation relation should exist for famille");
				Assert.AreEqual(-1, idxWhole, "Whole relation should not exist for famille");
				Assert.Less(0, idxPart, "Part relation should exist for famille");
				Assert.Less(idxAntonymAbbr, idxPart, "Antonym abbreviation relation should come before Part relation for famille");
				Assert.Less(idxAntonymAbbr, idxAntonymName, "Antonym name should come after Antonym abbreviation");

				// SUT: Ensure that both directions of part-whole are kept separate
				var girlResult = ConfiguredLcmGenerator.GenerateContentForEntry(girlEntry, mainEntryNode, null, settings).ToString();
				AssertThatXmlIn.String(girlResult).HasSpecifiedNumberOfMatchesForXpath(xpathLexRef, 2); // whole and part
				idxAntonymAbbr = girlResult.IndexOf(antAbbrSpan, StringComparison.Ordinal);
				idxWhole = girlResult.IndexOf(whSpan, StringComparison.Ordinal);
				idxPart = girlResult.IndexOf(ptSpan, StringComparison.Ordinal);
				idxAntonymName = girlResult.IndexOf(antNameSpan, StringComparison.Ordinal);
				Assert.AreEqual(-1, idxAntonymAbbr, "Antonym abbreviation relation should not exist for fille (girl)");
				Assert.Less(0, idxWhole, "Whole relation should exist for fille (girl)");
				Assert.Less(0, idxPart, "Part relation should exist for fille (girl)");
				Assert.Less(idxWhole, idxPart, "Whole relation should come before Part relation for fille (girl)");
				Assert.AreEqual(-1, idxAntonymName, "Antonym name relation should not exist for fille (girl)");
			}

			var individualResult = ConfiguredLcmGenerator.GenerateContentForEntry(individualEntry, mainEntryNode, null, settings).ToString();
			AssertThatXmlIn.String(individualResult).HasSpecifiedNumberOfMatchesForXpath(xpathLexRef, 1);
			idxAntonymAbbr = individualResult.IndexOf(antAbbrSpan, StringComparison.Ordinal);
			idxWhole = individualResult.IndexOf(whSpan, StringComparison.Ordinal);
			idxPart = individualResult.IndexOf(ptSpan, StringComparison.Ordinal);
			idxAntonymName = individualResult.IndexOf(antNameSpan, StringComparison.Ordinal);
			Assert.Less(0, idxAntonymAbbr, "Antonym abbreviation relation should exist for individuel");
			Assert.AreEqual(-1, idxWhole, "Whole relation should not exist for individuel");
			Assert.AreEqual(-1, idxPart, "Part relation should not exist for individuel");
			Assert.Less(idxAntonymAbbr, idxAntonymName, "Antonym name relation should exist for individuel");
		}

		/// <summary>
		/// LT-17384. LT-17762. Intermittent failures should NOT be ignored.
		/// </summary>
		[Test]
		public void GenerateContentForEntry_VariantsOfEntryAreOrdered()
		{
			var lexentry = CreateInterestingLexEntry(Cache);

			using (CreateVariantForm(Cache, lexentry, CreateInterestingLexEntry(Cache, "headwordB"), new Guid("00000000-0000-0000-0000-000000000001")))
			using (CreateVariantForm(Cache, lexentry, CreateInterestingLexEntry(Cache, "headwordA"), new Guid("00000000-0000-0000-0000-000000000003")))
			using (CreateVariantForm(Cache, lexentry, CreateInterestingLexEntry(Cache, "headwordD"), new Guid("00000000-0000-0000-0000-000000000004")))
			using (CreateVariantForm(Cache, lexentry, CreateInterestingLexEntry(Cache, "headwordC"), new Guid("00000000-0000-0000-0000-000000000002")))
			{
				var variantTypeNameNode = new ConfigurableDictionaryNode
				{
					FieldDescription = "Name",
					DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "analysis" })
				};
				var variantTypeNode = new ConfigurableDictionaryNode
				{
					FieldDescription = "VariantEntryTypesRS",
					CSSClassNameOverride = "variantentrytypes",
					Children = new List<ConfigurableDictionaryNode> { variantTypeNameNode },
				};
				var formNode = new ConfigurableDictionaryNode
				{
					FieldDescription = "OwningEntry",
					SubField = "MLHeadWord",
					IsEnabled = true,
					DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
				};
				var variantFormNode = new ConfigurableDictionaryNode
				{
					DictionaryNodeOptions = GetFullyEnabledListOptions(DictionaryNodeListOptions.ListIds.Variant, Cache),
					FieldDescription = "VariantFormEntryBackRefs",
					Children = new List<ConfigurableDictionaryNode> { formNode, variantTypeNode }
				};
				var mainEntryNode = new ConfigurableDictionaryNode
				{
					Children = new List<ConfigurableDictionaryNode> { variantFormNode },
					FieldDescription = "LexEntry"
				};

				DictionaryConfigurationModel.SpecifyParentsAndReferences(new List<ConfigurableDictionaryNode> { mainEntryNode });
				CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
				var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);

				//SUT
				var result = ConfiguredLcmGenerator.GenerateContentForEntry(lexentry, mainEntryNode, null, settings).ToString();

				// Test that variantformentrybackref items are in alphabetical order
				Assert.That(result.IndexOf("headwordA", StringComparison.InvariantCulture),
					Is.LessThan(result.IndexOf("headwordB", StringComparison.InvariantCulture)), "variant form not sorted in expected order");
				Assert.That(result.IndexOf("headwordB", StringComparison.InvariantCulture),
					Is.LessThan(result.IndexOf("headwordC", StringComparison.InvariantCulture)), "variant form not sorted in expected order");
				Assert.That(result.IndexOf("headwordC", StringComparison.InvariantCulture),
					Is.LessThan(result.IndexOf("headwordD", StringComparison.InvariantCulture)), "variant form not sorted in expected order");

				// Test that variantformentrybackref is before variantentrytypes. LT-20622 Order of Type and Form is important.
				Assert.That(result.IndexOf("headwordA", StringComparison.InvariantCulture),
					Is.LessThan(result.IndexOf("variantentrytypes", StringComparison.InvariantCulture)), "variant form not before variant type");

			}
		}

		/// <summary>
		/// LT-20622 Order of Type and Form is important.
		/// </summary>
		[Test]
		public void GenerateContentForEntry_TypeBeforeForm()
		{
			var lexentry = CreateInterestingLexEntry(Cache);

			using (CreateVariantForm(Cache, lexentry, CreateInterestingLexEntry(Cache, "headwordA"), new Guid("00000000-0000-0000-0000-000000000001")))
			{
				var variantTypeNameNode = new ConfigurableDictionaryNode
				{
					FieldDescription = "Name",
					DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "analysis" })
				};
				var variantTypeNode = new ConfigurableDictionaryNode
				{
					FieldDescription = "VariantEntryTypesRS",
					CSSClassNameOverride = "variantentrytypes",
					Children = new List<ConfigurableDictionaryNode> { variantTypeNameNode },
				};
				var formNode = new ConfigurableDictionaryNode
				{
					FieldDescription = "OwningEntry",
					SubField = "MLHeadWord",
					IsEnabled = true,
					DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
				};
				var variantFormNode = new ConfigurableDictionaryNode
				{
					DictionaryNodeOptions = GetFullyEnabledListOptions(DictionaryNodeListOptions.ListIds.Variant, Cache),
					FieldDescription = "VariantFormEntryBackRefs",
					Children = new List<ConfigurableDictionaryNode> { variantTypeNode, formNode }
				};
				var mainEntryNode = new ConfigurableDictionaryNode
				{
					Children = new List<ConfigurableDictionaryNode> { variantFormNode },
					FieldDescription = "LexEntry"
				};

				DictionaryConfigurationModel.SpecifyParentsAndReferences(new List<ConfigurableDictionaryNode> { mainEntryNode });
				CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
				var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);

				//SUT
				var result = ConfiguredLcmGenerator.GenerateContentForEntry(lexentry, mainEntryNode, null, settings).ToString();

				// Test that variantentrytypes is before variantformentrybackref
				Assert.That(result.IndexOf("variantentrytypes", StringComparison.InvariantCulture),
					Is.LessThan(result.IndexOf("headwordA", StringComparison.InvariantCulture)), "variant type not before variant form");
			}
		}

		/// <summary>LT-17918. Intermittent failures should NOT be ignored.</summary>
		[Test]
		public void GenerateContentForEntry_ComplexFormsAreOrderedAsUserSpecified(
			[Values(true, false)] bool useNotSubentries, [Values(true, false)] bool useVirtualOrdering, [Values(true, false)] bool showInPara)
		{
			var lexentry = CreateInterestingLexEntry(Cache);

			using (var c1 = CreateComplexForm(Cache, lexentry, CreateInterestingLexEntry(Cache, "headwordB"), new Guid("00000000-0000-0000-0000-000000000001"), false))
			using (var c3 = CreateComplexForm(Cache, lexentry, CreateInterestingLexEntry(Cache, "headwordA"), new Guid("00000000-0000-0000-0000-000000000003"), false))
			using (var c2 = CreateComplexForm(Cache, lexentry, CreateInterestingLexEntry(Cache, "headwordD"), new Guid("00000000-0000-0000-0000-000000000004"), false))
			using (var c4 = CreateComplexForm(Cache, lexentry, CreateInterestingLexEntry(Cache, "headwordC"), new Guid("00000000-0000-0000-0000-000000000002"), false))
			{
				var headwords = new[] { "headwordA", "headwordB", "headwordC", "headwordD" };
				if (useVirtualOrdering)
				{
					var varFlid = Cache.MetaDataCacheAccessor.GetFieldId("LexEntry", "VisibleComplexFormBackRefs", true);
					VirtualOrderingServices.SetVO(lexentry, varFlid, new[] { c1.Item, c2.Item, c3.Item, c4.Item });
					headwords = new[]
					{
							c1.Item.OwningEntry.HomographForm,
							c2.Item.OwningEntry.HomographForm,
							c3.Item.OwningEntry.HomographForm,
							c4.Item.OwningEntry.HomographForm
						};
				}
				var complexTypeNameNode = new ConfigurableDictionaryNode
				{
					FieldDescription = "Name",
					DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "analysis" })
				};
				var complexTypeNode = new ConfigurableDictionaryNode
				{
					FieldDescription = "ComplexEntryTypesRS",
					Children = new List<ConfigurableDictionaryNode> { complexTypeNameNode },
				};
				var formNode = new ConfigurableDictionaryNode
				{
					FieldDescription = "OwningEntry",
					SubField = "MLHeadWord",
					IsEnabled = true,
					DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
				};
				var complexFormNode = new ConfigurableDictionaryNode
				{
					DictionaryNodeOptions = GetFullyEnabledListOptions(DictionaryNodeListOptions.ListIds.Complex, Cache),
					FieldDescription = useNotSubentries ? "ComplexFormsNotSubentries" : "VisibleComplexFormBackRefs",
					Children = new List<ConfigurableDictionaryNode> { formNode, complexTypeNode }
				};
				((IParaOption)complexFormNode.DictionaryNodeOptions).DisplayEachInAParagraph = showInPara;
				var mainEntryNode = new ConfigurableDictionaryNode
				{
					Children = new List<ConfigurableDictionaryNode> { complexFormNode },
					FieldDescription = "LexEntry"
				};

				DictionaryConfigurationModel.SpecifyParentsAndReferences(new List<ConfigurableDictionaryNode> { mainEntryNode });
				CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
				var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);

				//SUT
				var result = ConfiguredLcmGenerator.GenerateContentForEntry(lexentry, mainEntryNode, null, settings).ToString();

				// Test that variantformentrybackref items are in (alphabetical or) virtual order
				Assert.That(result.IndexOf(headwords[0], StringComparison.InvariantCulture),
					Is.LessThan(result.IndexOf(headwords[1], StringComparison.InvariantCulture)), "complex form not sorted in expected order\n{0}", result);
				Assert.That(result.IndexOf(headwords[1], StringComparison.InvariantCulture),
					Is.LessThan(result.IndexOf(headwords[2], StringComparison.InvariantCulture)), "complex form not sorted in expected order\n{0}", result);
				Assert.That(result.IndexOf(headwords[2], StringComparison.InvariantCulture),
					Is.LessThan(result.IndexOf(headwords[3], StringComparison.InvariantCulture)), "complex form not sorted in expected order\n{0}", result);
			}
		}

		/// <summary>
		/// LT-18018.
		/// The implementation code changes were done in GenerateXHTMLForILexEntryRefsByType.
		/// </summary>
		[Test]
		public void GenerateContentForFieldByReflection_VariantFormTypesAreOrderedBasedOnOptionOrdering()
		{
			var variantTypeNameNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Name",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "analysis" })
			};
			var variantTypeNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VariantEntryTypesRS",
				CSSClassNameOverride = "variantentrytypes",
				Children = new List<ConfigurableDictionaryNode> { variantTypeNameNode },
			};
			var formNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwningEntry",
				SubField = "MLHeadWord",
				IsEnabled = true,
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};
			var variantFormNode = new ConfigurableDictionaryNode
			{
				DictionaryNodeOptions = GetFullyEnabledListOptions(DictionaryNodeListOptions.ListIds.Variant, Cache),
				FieldDescription = "VariantFormEntryBackRefs",
				Children = new List<ConfigurableDictionaryNode> { formNode, variantTypeNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { variantFormNode },
				FieldDescription = "LexEntry"
			};

			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);

			// Use the second item in the list for testing at this time, since the first item in the list isn't being handled the same way right now (20170111) by the current implementation.
			var earlyTypeInOptionsListGuid = new Guid(((DictionaryNodeListOptions)variantFormNode.DictionaryNodeOptions).Options[1].Id);
			var finalTypeInOptionsListGuid = new Guid(((DictionaryNodeListOptions)variantFormNode.DictionaryNodeOptions).Options.Last().Id);

			var earlyTypeInOptionsList = Cache.LangProject.LexDbOA.VariantEntryTypesOA.ReallyReallyAllPossibilities.First(t => t.Guid == earlyTypeInOptionsListGuid);
			var finalTypeInOptionsList = Cache.LangProject.LexDbOA.VariantEntryTypesOA.ReallyReallyAllPossibilities.First(t => t.Guid == finalTypeInOptionsListGuid);

			var lexentry = CreateInterestingLexEntry(Cache);

			CreateInterestingLexEntry(Cache, "headwordA").MakeVariantOf(lexentry, (ILexEntryType)earlyTypeInOptionsList);
			CreateInterestingLexEntry(Cache, "headwordB").MakeVariantOf(lexentry, (ILexEntryType)finalTypeInOptionsList);

			// SUT1
			var result = ConfiguredLcmGenerator.GenerateContentForFieldByReflection(lexentry, variantFormNode, null, DefaultSettings).ToString();

			Assert.That(result.IndexOf("headwordA", StringComparison.InvariantCulture),
				Is.LessThan(result.IndexOf("headwordB", StringComparison.InvariantCulture)), "variant forms not appearing in an order corresponding to their type sorting");

			// Change the order of variantFormNode.DictionaryNodeOptions, which should result in the data being ordered differently.
			((DictionaryNodeListOptions)variantFormNode.DictionaryNodeOptions).Options.Reverse();

			// SUT2
			result = ConfiguredLcmGenerator.GenerateContentForFieldByReflection(lexentry, variantFormNode, null, DefaultSettings).ToString();

			Assert.That(result.IndexOf("headwordB", StringComparison.InvariantCulture),
				Is.LessThan(result.IndexOf("headwordA", StringComparison.InvariantCulture)), "variant forms not appearing in an order corresponding to their type sorting");
		}

		/// <summary>
		/// LT-18018.
		/// The implementation code changes were done in GenerateContentForLexEntryRefsByType.
		/// </summary>
		[Test]
		public void GenerateContentForFieldByReflection_SubentryTypesAreOrderedBasedOnOptionOrdering()
		{
			var complexFormTypeNameNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Name",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "analysis" })
			};
			var complexFormTypeNode = new ConfigurableDictionaryNode
			{
				FieldDescription = ConfiguredLcmGenerator.LookupComplexEntryType,
				Children = new List<ConfigurableDictionaryNode> { complexFormTypeNameNode },
			};
			var formNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLHeadWord",
				IsEnabled = true,
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};
			var subentryNode = new ConfigurableDictionaryNode
			{
				DictionaryNodeOptions = GetFullyEnabledListOptions(DictionaryNodeListOptions.ListIds.Complex, Cache),
				FieldDescription = "Subentries",
				Children = new List<ConfigurableDictionaryNode> { formNode, complexFormTypeNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { subentryNode },
				FieldDescription = "LexEntry"
			};

			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);

			// Use the second item in the list for testing at this time, since the first item in the list (<none>) isn't being handled the same way right now (20170119) by the current implementation.
			var earlyTypeInOptionsListGuid = new Guid(((DictionaryNodeListOptions)subentryNode.DictionaryNodeOptions).Options[1].Id);
			var finalTypeInOptionsListGuid = new Guid(((DictionaryNodeListOptions)subentryNode.DictionaryNodeOptions).Options.Last().Id);
			var lexentry = CreateInterestingLexEntry(Cache);
			CreateComplexForm(Cache, lexentry, CreateInterestingLexEntry(Cache, "headwordA"), true, earlyTypeInOptionsListGuid);
			CreateComplexForm(Cache, lexentry, CreateInterestingLexEntry(Cache, "headwordB"), true, finalTypeInOptionsListGuid);

			// SUT1
			var result = ConfiguredLcmGenerator.GenerateContentForFieldByReflection(lexentry, subentryNode, null, DefaultSettings).ToString();

			Assert.That(result.IndexOf("headwordA", StringComparison.InvariantCulture),
				Is.LessThan(result.IndexOf("headwordB", StringComparison.InvariantCulture)), "Subentries should be sorted by Type");

			// Reverse the order of the DictionaryNodeOptions, which should result in the data being ordered differently.
			((DictionaryNodeListOptions)subentryNode.DictionaryNodeOptions).Options.Reverse();

			// SUT2
			result = ConfiguredLcmGenerator.GenerateContentForFieldByReflection(lexentry, subentryNode, null, DefaultSettings).ToString();

			Assert.That(result.IndexOf("headwordB", StringComparison.InvariantCulture),
				Is.LessThan(result.IndexOf("headwordA", StringComparison.InvariantCulture)), "Subentries should be sorted by Type");
		}

		// <summary>
		/// LT-18171:Crash displaying entry or doing xhtml export
		/// </summary>
		[Test]
		public void GenerateContentForFieldByReflection_NullOrEmptyMediaFilePathDoesNotCrash()
		{
			var pronunciationsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "PronunciationsOS",
				CSSClassNameOverride = "pronunciations",
				Children = new List<ConfigurableDictionaryNode> { CreateMediaNode() }
			};
			var variantPronunciationsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwningEntry",
				SubField = "PronunciationsOS",
				CSSClassNameOverride = "variantpronunciations",
				Children = new List<ConfigurableDictionaryNode> { CreateMediaNode() }
			};
			var variantFormTypeNameNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Name",
				IsEnabled = true,
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "analysis" })
			};
			var variantFormTypeNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VariantEntryTypesRS",
				CSSClassNameOverride = "variantentrytypes",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { variantFormTypeNameNode },
			};
			var variantFormsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VariantFormEntryBackRefs",
				DictionaryNodeOptions = GetFullyEnabledListOptions(DictionaryNodeListOptions.ListIds.Variant, Cache),
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { variantFormTypeNode, variantPronunciationsNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { pronunciationsNode, variantFormsNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var entry = CreateInterestingLexEntry(Cache);
			var variant = CreateInterestingLexEntry(Cache);
			// we need a real Variant Type to pass the list options test
			CreateVariantForm(Cache, entry, variant, "Spelling Variant");
			// Create a folder in the project to hold the media files
			var folder = Cache.ServiceLocator.GetInstance<ICmFolderFactory>().Create();
			Cache.LangProject.MediaOC.Add(folder);
			// Create and fill in the media files
			var pron1 = Cache.ServiceLocator.GetInstance<ILexPronunciationFactory>().Create();
			entry.PronunciationsOS.Add(pron1);
			var fileName1 = string.Empty;
			CreateTestMediaFile(Cache, fileName1, folder, pron1);
			var pron2 = Cache.ServiceLocator.GetInstance<ILexPronunciationFactory>().Create();
			variant.PronunciationsOS.Add(pron2);
			CreateTestMediaFile(Cache, null, folder, pron2);
			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null);

			//SUT
			Assert.DoesNotThrow(() => ConfiguredLcmGenerator.GenerateContentForEntry(entry, mainEntryNode, null, settings), "Invalid filename in CmFile should not lead to crash");
		}

		[TestCase("Bob", false, "Bo")]
		[TestCase("Bob", true, "B")]
		[TestCase("a", false, "a")]
		[TestCase("", false, "")]
		// surrogate pairs
		[TestCase("\ud81b\udf00\ud81b\udf55", true, "\ud81b\udf00")]
		[TestCase("\ud81b\udf00\ud81b\udf55", false, "\ud81b\udf00\ud81b\udf55")]
		[TestCase("a\ud81b\udf55", false, "a\ud81b\udf55")]
		[TestCase("\ud81b\udf00test", false, "\ud81b\udf00t")]
		public void GetIndexLettersOfSortWord(string sortWord, bool onlyFirstLetter, string expected)
		{
			var actual = typeof(LcmXhtmlGenerator)
				.GetMethod("GetIndexLettersOfSortWord", BindingFlags.NonPublic | BindingFlags.Static)
				.Invoke(null, new object[] { sortWord, onlyFirstLetter });
			Assert.AreEqual(expected, actual, $"{onlyFirstLetter} {sortWord}");
		}

		[Test]
		public void GenerateAdjustedPageNumbers_NoAdjacentWhenUpButtonConsumesAllEntries()
		{
			var firstEntry = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(firstEntry, "homme", m_wsFr);

			var secondEntry = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(secondEntry, "femme", m_wsFr);

			var thirdEntry = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(thirdEntry, "famille", m_wsFr);
			var currentPage = new Tuple<int, int>(0, 2);
			var adjacentPage = new Tuple<int, int>(2, 2);
			Tuple<int, int> current;
			Tuple<int, int> adjacent;
			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, "");
			// SUT
			LcmXhtmlGenerator.GenerateAdjustedPageButtons(new[] { firstEntry.Hvo, secondEntry.Hvo, thirdEntry.Hvo }, settings, currentPage, adjacentPage, 2,
				out current, out adjacent);
			Assert.That(adjacent, Is.Null, "The Adjacent page should have been consumed into the current page");
			Assert.AreEqual(0, current.Item1, "Current page should start at 0");
			Assert.AreEqual(2, current.Item2, "Current page should end at 2");
		}

		[Test]
		public void GenerateAdjustedPageNumbers_NoAdjacentWhenDownButtonConsumesAllEntries()
		{
			var firstEntry = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(firstEntry, "homme", m_wsFr);

			var secondEntry = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(secondEntry, "femme", m_wsFr);

			var thirdEntry = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(thirdEntry, "famille", m_wsFr);
			var currentPage = new Tuple<int, int>(1, 2);
			var adjPage = new Tuple<int, int>(0, 1);
			Tuple<int, int> current;
			Tuple<int, int> adjacent;
			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, "");
			// SUT
			LcmXhtmlGenerator.GenerateAdjustedPageButtons(new[] { firstEntry.Hvo, secondEntry.Hvo, thirdEntry.Hvo }, settings, currentPage, adjPage, 2,
				out current, out adjacent);
			Assert.That(adjacent, Is.Null, "The Adjacent page should have been consumed into the current page");
			Assert.AreEqual(0, current.Item1, "Current page should start at 0");
			Assert.AreEqual(2, current.Item2, "Current page should end at 2");
		}

		[Test]
		public void GenerateAdjustedPageNumbers_AdjacentAndCurrentPageAdjustCorrectlyUp()
		{
			var firstEntry = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(firstEntry, "homme", m_wsFr);

			var secondEntry = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(secondEntry, "femme", m_wsFr);

			var thirdEntry = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(thirdEntry, "famille", m_wsFr);

			var fourthEntry = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(thirdEntry, "famille", m_wsFr);

			var currentPage = new Tuple<int, int>(0, 2);
			var adjPage = new Tuple<int, int>(3, 4);
			Tuple<int, int> current;
			Tuple<int, int> adjacent;
			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, "");
			// SUT
			LcmXhtmlGenerator.GenerateAdjustedPageButtons(new[] { firstEntry.Hvo, secondEntry.Hvo, thirdEntry.Hvo, fourthEntry.Hvo }, settings, currentPage, adjPage, 1,
				out current, out adjacent);
			Assert.AreEqual(0, current.Item1, "Current page should start at 0");
			Assert.AreEqual(3, current.Item2, "Current page should end at 3");
			Assert.AreEqual(4, adjacent.Item1, "Adjacent page should start at 4");
			Assert.AreEqual(4, adjacent.Item2, "Adjacent page should end at 4");
		}

		[Test]
		public void GenerateAdjustedPageNumbers_AdjacentAndCurrentPageAdjustCorrectlyDown()
		{
			var firstEntry = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(firstEntry, "homme", m_wsFr);

			var secondEntry = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(secondEntry, "femme", m_wsFr);

			var thirdEntry = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(thirdEntry, "famille", m_wsFr);

			var fourthEntry = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(thirdEntry, "famille", m_wsFr);

			var adjPage = new Tuple<int, int>(0, 2);
			var currentPage = new Tuple<int, int>(3, 4);
			Tuple<int, int> current;
			Tuple<int, int> adjacent;
			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, "");
			// SUT
			LcmXhtmlGenerator.GenerateAdjustedPageButtons(new[] { firstEntry.Hvo, secondEntry.Hvo, thirdEntry.Hvo, fourthEntry.Hvo }, settings, currentPage, adjPage, 1,
				out current, out adjacent);
			Assert.AreEqual(2, current.Item1, "Current page should start at 2");
			Assert.AreEqual(4, current.Item2, "Current page should end at 4");
			Assert.AreEqual(0, adjacent.Item1, "Adjacent page should start at 0");
			Assert.AreEqual(1, adjacent.Item2, "Adjacent page should end at 1");
		}

		[Test]
		public void GenerateNextFewEntries_UpReturnsRequestedEntries()
		{
			var firstEntry = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(firstEntry, "homme", m_wsFr);

			var secondEntry = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(secondEntry, "femme", m_wsFr);

			var thirdEntry = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(thirdEntry, "famille", m_wsFr);

			var fourthEntry = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(fourthEntry, "familliar", m_wsFr);

			var pubEverything = new DictionaryPublicationDecorator(Cache, (ISilDataAccessManaged)Cache.MainCacheAccessor, Cache.ServiceLocator.GetInstance<Virtuals>().LexDbEntries);
			var mainHeadwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLHeadWord",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" }),
				CSSClassNameOverride = "entry"
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { mainHeadwordNode },
				FieldDescription = "LexEntry"
			};
			var model = new DictionaryConfigurationModel
			{
				Parts = new List<ConfigurableDictionaryNode> { mainEntryNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(model);
			var configPath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
			model.FilePath = configPath;
			model.Save();
			try
			{
				var adjPage = new Tuple<int, int>(0, 2);
				var currentPage = new Tuple<int, int>(3, 3);
				Tuple<int, int> current;
				Tuple<int, int> adjacent;
				var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, "");

				// SUT
				var entries = LcmXhtmlGenerator.GenerateNextFewEntries(pubEverything, new[] { firstEntry.Hvo, secondEntry.Hvo, thirdEntry.Hvo, fourthEntry.Hvo }, configPath,
					settings, currentPage, adjPage, 1, out current, out adjacent);
				Assert.AreEqual(1, entries.Count, "No entries generated");
				Assert.That(entries[0].ToString(), Does.Contain(thirdEntry.HeadWord.Text));
			}
			finally
			{
				File.Delete(model.FilePath);
			}
		}

		[Test]
		public void GenerateNextFewEntries_DownReturnsRequestedEntries()
		{
			var firstEntry = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(firstEntry, "homme", m_wsFr);

			var secondEntry = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(secondEntry, "femme", m_wsFr);

			var thirdEntry = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(thirdEntry, "famille", m_wsFr);

			var fourthEntry = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(fourthEntry, "familliar", m_wsFr);

			var pubEverything = new DictionaryPublicationDecorator(Cache, (ISilDataAccessManaged)Cache.MainCacheAccessor, Cache.ServiceLocator.GetInstance<Virtuals>().LexDbEntries);
			var mainHeadwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLHeadWord",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" }),
				CSSClassNameOverride = "entry"
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { mainHeadwordNode },
				FieldDescription = "LexEntry"
			};
			var model = new DictionaryConfigurationModel
			{
				Parts = new List<ConfigurableDictionaryNode> { mainEntryNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(model);
			var configPath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
			model.FilePath = configPath;
			model.Save();
			try
			{
				var adjPage = new Tuple<int, int>(2, 3);
				var currentPage = new Tuple<int, int>(0, 1);
				Tuple<int, int> current;
				Tuple<int, int> adjacent;
				var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, "");

				// SUT
				var entries = LcmXhtmlGenerator.GenerateNextFewEntries(pubEverything, new[] { firstEntry.Hvo, secondEntry.Hvo, thirdEntry.Hvo, fourthEntry.Hvo }, configPath,
					settings, currentPage, adjPage, 2, out current, out adjacent);
				Assert.AreEqual(2, entries.Count, "Not enough entries generated");
				Assert.That(entries[0].ToString(), Does.Contain(thirdEntry.HeadWord.Text));
				Assert.That(entries[1].ToString(), Does.Contain(fourthEntry.HeadWord.Text));
				Assert.That(adjacent, Is.Null);
			}
			finally
			{
				File.Delete(model.FilePath);
			}
		}

		[Test]
		public void GenerateContentForEntry_GroupingNodeGeneratesSpanAndInnerContentWorks()
		{
			var wsOpts = GetWsOptionsForLanguages(new[] { "en" });
			var glossNode = new ConfigurableDictionaryNode { FieldDescription = "Gloss", DictionaryNodeOptions = wsOpts };
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Senses",
				Children = new List<ConfigurableDictionaryNode> { glossNode }
			};
			var groupingNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SenseGroup",
				Children = new List<ConfigurableDictionaryNode> { sensesNode },
				DictionaryNodeOptions = new DictionaryNodeGroupingOptions()
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { groupingNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var testEntry = CreateInterestingLexEntry(Cache);

			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(testEntry, mainEntryNode, null, DefaultSettings).ToString();

			const string oneSenseWithGlossOfGloss = "/div[@class='lexentry']/span[@class='grouping_sensegroup']"
				+ "/span[@class='senses']/span[@class='sense']//span[@lang='en' and text()='gloss']";
			//This assert is dependent on the specific entry data created in CreateInterestingLexEntry
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(oneSenseWithGlossOfGloss, 1);
		}

		[Test]
		public void GenerateContentForEntry_GeneratesNFC()
		{
			var node = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				CSSClassNameOverride = "über",
				Children = new List<ConfigurableDictionaryNode>
					{
						new ConfigurableDictionaryNode
						{
							FieldDescription = "MLHeadWord",
							DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "ko" })
						}
					}
			};

			CssGeneratorTests.PopulateFieldsForTesting(node);
			Cache.LangProject.AddToCurrentVernacularWritingSystems(Cache.WritingSystemFactory.get_Engine("ko") as CoreWritingSystemDefinition);
			var wsKo = Cache.WritingSystemFactory.GetWsFromStr("ko");
			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			var headword = TsStringUtils.MakeString("자ㄱㄴ시", wsKo); // Korean NFD
			entry.CitationForm.set_String(wsKo, headword);
			Assert.That(entry.CitationForm.get_String(wsKo).get_IsNormalizedForm(FwNormalizationMode.knmNFD), "Should be NFDecomposed in memory");
			Assert.AreEqual(6, headword.Text.Length);
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entry, node, null, DefaultSettings).ToString();
			var tsResult = TsStringUtils.MakeString(result, Cache.DefaultAnalWs);
			Assert.False(TsStringUtils.IsNullOrEmpty(tsResult), "Results should have been generated");
			Assert.That(tsResult.get_IsNormalizedForm(FwNormalizationMode.knmNFC), "Resulting XHTML should be NFComposed");
		}

		[Test]
		public void GenerateContentForEntry_CompareRelations_ComplexSituation_CustomSort()
		{
			CoreWritingSystemDefinition ws = Cache.LangProject.DefaultVernacularWritingSystem;
			var customRule = new IcuRulesCollationDefinition("standard")
			{
				IcuRules = "& [last tertiary ignorable] = ‘",
				OwningWritingSystemDefinition = ws
			};
			customRule.Validate(out _);
			ws.DefaultCollation = customRule;
			var mainEntry = CreateInterestingLexEntry(Cache, "MainEntry");

			var compareReferencedEntry1 = CreateInterestingLexEntry(Cache, "atest", "atest comparable");
			var compareReferencedEntry2 = CreateInterestingLexEntry(Cache, "ctest", "ctest comparable");
			var compareReferencedEntry3 = CreateInterestingLexEntry(Cache, "‘mtest", "‘mtest comparable");
			var compareReferencedEntry4 = CreateInterestingLexEntry(Cache, "ztest", "ztest comparable");
			const string comRefTypeName = "Compare";
			var comRefType = CreateLexRefType(Cache, LexRefTypeTags.MappingTypes.kmtEntryCollection, comRefTypeName, "cf", string.Empty, string.Empty);
			CreateLexReference(comRefType, new List<ICmObject> { mainEntry, compareReferencedEntry4, compareReferencedEntry1, compareReferencedEntry3, compareReferencedEntry2 });
			Assert.That(comRefType, Is.Not.Null);

			var mainEntryNode = ModelForCrossReferences(new[] { comRefType.Guid.ToString() });
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(mainEntry, mainEntryNode, null, DefaultSettings).ToString();
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(CrossRefOwnerTypeXpath(comRefTypeName), 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(HeadwordOrderInCrossRefsXpath(1, "atest"), 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(HeadwordOrderInCrossRefsXpath(2, "ctest"), 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(HeadwordOrderInCrossRefsXpath(3, "‘mtest"), 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(HeadwordOrderInCrossRefsXpath(4, "ztest"), 1);
		}

		[Test]
		public void GenerateXHTMLTemplate_OnlyGeneratesSelectedParts()
		{
			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLHeadWord",
				CSSClassNameOverride = "headword",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { headwordNode },
				FieldDescription = "LexEntry"
			};
			var model = new DictionaryConfigurationModel
			{
				Parts = new List<ConfigurableDictionaryNode> { mainEntryNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(model);
			var unselectedPart = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { headwordNode },
				FieldDescription = "LexEntry",
				CSSClassNameOverride = "minorentry"
			};
			model.Parts.Add(unselectedPart);
			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, new ReadOnlyPropertyTable(m_propertyTable), false, false, null, isTemplate: true);
			var entry = CreateInterestingLexEntry(Cache);
			//SUT
			var result = LcmXhtmlGenerator.GenerateXHTMLTemplatesForConfigurationModel(model, Cache);
			const string expectedTemplate = "/div[@class='lexentry']";
			Assert.That(result.Count(), Is.EqualTo(1));
			// verify the one result matches the selected node
			AssertThatXmlIn.String(result[0]).HasSpecifiedNumberOfMatchesForXpath(expectedTemplate, 1);
		}

		[Test]
		public void GenerateXHTMLTemplate_HeadWordWorks()
		{
			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLHeadWord",
				CSSClassNameOverride = "headword",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { headwordNode },
				FieldDescription = "LexEntry"
			};
			var model = new DictionaryConfigurationModel
			{
				Parts = new List<ConfigurableDictionaryNode> { mainEntryNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(model);
			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, new ReadOnlyPropertyTable(m_propertyTable), false, false, null, isTemplate: true);
			var entry = CreateInterestingLexEntry(Cache);
			//SUT
			var result = LcmXhtmlGenerator.GenerateXHTMLTemplatesForConfigurationModel(model, Cache);
			const string expectedTemplate = "/div[@class='lexentry']/span[@class='headword']/span[@lang='fr']/a[@href='%headword.guid%' and text()='%headword.[lang=fr].value%']";
			AssertThatXmlIn.String(result[0]).HasSpecifiedNumberOfMatchesForXpath(expectedTemplate, 1);
		}

		[Test]
		public void GenerateXHTMLTemplate_MagicAnalysisWsIdWorks()
		{
			var headwordAll = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLHeadWord",
				CSSClassNameOverride = "all",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "all analysis" })
			};
			var headwordBest = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLHeadWord",
				CSSClassNameOverride = "headword",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "best analysis" })
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { headwordAll, headwordBest },
				FieldDescription = "LexEntry"
			};
			var model = new DictionaryConfigurationModel
			{
				Parts = new List<ConfigurableDictionaryNode> { mainEntryNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(model);
			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, new ReadOnlyPropertyTable(m_propertyTable), false, false, null, isTemplate: true);
			var entry = CreateInterestingLexEntry(Cache);
			//SUT
			var result = LcmXhtmlGenerator.GenerateXHTMLTemplatesForConfigurationModel(model, Cache);
			const string expectedTemplate = "/div[@class='lexentry']/span[@class='all']/span[@lang='en' and text()='%all.[lang=en].value%']";
			const string expectedHwTemplate = "/div[@class='lexentry']/span[@class='headword']/span[@lang='en']/a[@href='%headword.guid%' and text()='%headword.[lang=en].value%']";
			AssertThatXmlIn.String(result[0]).HasSpecifiedNumberOfMatchesForXpath(expectedTemplate, 1);
			AssertThatXmlIn.String(result[0]).HasSpecifiedNumberOfMatchesForXpath(expectedHwTemplate, 1);
		}

		[Test]
		public void GenerateContentForEntry_BadWaveFileThrowsWithEntryInfo()
		{
			var pronunciationsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "PronunciationsOS",
				CSSClassNameOverride = "Pronunciations",
				Children = new List<ConfigurableDictionaryNode> { CreateMediaNode() }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { pronunciationsNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			var pronunciation =
				Cache.ServiceLocator.GetInstance<ILexPronunciationFactory>().Create();
			entry.PronunciationsOS.Add(pronunciation);

			var tempWavFilePath = Path.GetTempFileName() + ".wav";
			var badWavContainer =
				Cache.ServiceLocator.GetInstance<ICmMediaFactory>().Create();
			pronunciation.MediaFilesOS.Add(badWavContainer);
			var folder = Cache.ServiceLocator.GetInstance<ICmFolderFactory>().Create();
			Cache.LangProject.MediaOC.Add(folder);
			var badWavFile =
				Cache.ServiceLocator.GetInstance<ICmFileFactory>().Create();
			folder.FilesOC.Add(badWavFile);
			badWavContainer.MediaFileRA = badWavFile;
			File.WriteAllText(tempWavFilePath, "I am not a wave file");
			badWavFile.InternalPath = tempWavFilePath;
			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache,
				new ReadOnlyPropertyTable(m_propertyTable), true, true,
				Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()), false, true);
			//SUT
			Assert.That(
				() => ConfiguredLcmGenerator.GenerateContentForEntry(entry, mainEntryNode, null,
					settings),
				Throws.Exception.With.Message.Contains("Exception generating entry:"));
		}
	}

	internal class CollatorForTest : IDisposable
	{
		private Collator collator;

		public static implicit operator Collator(CollatorForTest col) => col.collator;

		public CollatorForTest(string vernWs)
		{
			Collator col = null;
			try
			{
				var icuLocale = new Icu.Locale(vernWs).Name;
				col = Collator.Create(icuLocale);
			}
			catch (Exception)
			{
				// no Collator can be created, not fatal, just means people might not like their letter headers
			}
		}

		public void Dispose()
		{
			collator?.Dispose();
		}
	}
}
