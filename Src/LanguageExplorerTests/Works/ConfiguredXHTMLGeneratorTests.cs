// Copyright (c) 2014-2018 SIL International
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
using SIL.Linq;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.DomainImpl;
using SIL.LCModel.DomainServices;
using SIL.TestUtilities;
using SIL.LCModel.Utils;
using LanguageExplorer;
using LanguageExplorer.Works;
using SIL.FieldWorks.Filters;
using LanguageExplorer.Areas;
using LanguageExplorer.Areas.Lexicon;
using System.Windows.Forms;
using SIL.IO;
using LanguageExplorer.Controls.XMLViews;
using LanguageExplorer.Impls;
using SIL.FieldWorks.Common.Widgets;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorerTests.Works
{
	[TestFixture]
	public class ConfiguredXHTMLGeneratorTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private FlexComponentParameters _flexComponentParameters;
		private IRecordListRepositoryForTools _recordListRepositoryForTools;
		private IRecordList _recordList;
		private StatusBar _statusBar;
		private int _wsEn, _wsFr, _wsHe;
		const string xpathThruSense = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense']";
		private const string TestVariantName = "Crazy Variant";
		private StringBuilder XHTMLStringBuilder { get; set; }
		private const string DictionaryNormal = "Dictionary-Normal";
		private BaseStyleInfo DictionaryNormalStyle { get { return FontHeightAdjuster.StyleSheetFromPropertyTable(_flexComponentParameters.PropertyTable).Styles[DictionaryNormal]; } }

		private GeneratorSettings DefaultSettings
		{
			get { return new GeneratorSettings(Cache, new ReadOnlyPropertyTable(_flexComponentParameters.PropertyTable), false, false, null); }
		}

		private DictionaryPublicationDecorator DefaultDecorator
		{
			get
			{
				return new DictionaryPublicationDecorator(Cache, (ISilDataAccessManaged)Cache.MainCacheAccessor,
					Cache.ServiceLocator.GetInstance<Virtuals>().LexDbEntries);
			}
		}

		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			base.FixtureSetup();
;
			_flexComponentParameters = TestSetupServices.SetupEverything(Cache);

			var styles = _flexComponentParameters.PropertyTable.GetValue<LcmStyleSheet>("FlexStyleSheet").Styles;
			if (!styles.Contains(DictionaryNormal))
			{
				styles.Add(new BaseStyleInfo { Name = DictionaryNormal });
			}

			_recordListRepositoryForTools = new RecordListRepository(Cache, _flexComponentParameters);
			RecordList.ActiveRecordListRepository = _recordListRepositoryForTools;
			_statusBar = new StatusBar();
			_recordList = CreateRecordList();
			RecordList.ActiveRecordListRepository.AddRecordList(_recordList);
			RecordList.ActiveRecordListRepository.ActiveRecordList = _recordList;

			_flexComponentParameters.PropertyTable.SetProperty(AreaServices.ToolChoice, AreaServices.LexiconDictionaryMachineName, false, false);
			Cache.ProjectId.Path = Path.Combine(FwDirectoryFinder.SourceDirectory, "LanguageExplorerTests", "Works", "TestData", "TestData.fwdata");
			_wsEn = Cache.WritingSystemFactory.GetWsFromStr("en");
			_wsFr = Cache.WritingSystemFactory.GetWsFromStr("fr");
		}

		private IRecordList CreateRecordList()
		{
			var recordList = new RecordList(LexiconArea.Entries, _statusBar,
				Cache.ServiceLocator.GetInstance<ISilDataAccessManaged>(), false,
				new VectorPropertyParameterObject(Cache.LanguageProject.LexDbOA, "Entries", Cache.MetaDataCacheAccessor.GetFieldId2(Cache.LanguageProject.LexDbOA.ClassID, "Entries", false)),
				new Dictionary<string, PropertyRecordSorter>
				{
					{ AreaServices.Default, new PropertyRecordSorter(AreaServices.ShortName) },
					{ "PrimaryGloss", new PropertyRecordSorter("PrimaryGloss") }
				});
			recordList.SortName = "Headword";
			recordList.InitializeFlexComponent(_flexComponentParameters);
			return recordList;
		}

		[TestFixtureTearDown]
		public override void FixtureTeardown()
		{
			RecordList.ActiveRecordListRepository = null;
			_statusBar?.Dispose();
			_recordListRepositoryForTools?.Dispose();
			_flexComponentParameters.PropertyTable.Dispose();
			_recordListRepositoryForTools = null;
			_statusBar = null;
			_recordList = null;
			_flexComponentParameters = null;

			base.FixtureTeardown();
		}

		[SetUp]
		public void SetupExportVariables()
		{
			XHTMLStringBuilder = new StringBuilder();
		}

		[TearDown]
		public void ResetModelAssembly()
		{
			XHTMLStringBuilder = null;

			// Specific tests override this, reset to SIL.LCModel.dll needed by most tests in the file
			ConfiguredXHTMLGenerator.AssemblyFile = "SIL.LCModel";
		}

		[Test]
		public void GeneratorSettings_NullArgsThrowArgumentNull()
		{
			// ReSharper disable AccessToDisposedClosure // Justification: Assert calls lambdas immediately, so XHTMLWriter is not used after being disposed
			// ReSharper disable ObjectCreationAsStatement // Justification: We expect the constructor to throw, so there's no created object to assign anywhere :)
			Assert.Throws(typeof(ArgumentNullException), () => new GeneratorSettings(Cache, (IPropertyTable)null, false, false, null));
			Assert.Throws(typeof(ArgumentNullException), () => new GeneratorSettings(null, new ReadOnlyPropertyTable(_flexComponentParameters.PropertyTable), false, false, null));
			// ReSharper restore ObjectCreationAsStatement
			// ReSharper restore AccessToDisposedClosure
		}

		[Test]
		public void GenerateXHTMLForEntry_NullArgsThrowArgumentNull()
		{
			var mainEntryNode = new ConfigurableDictionaryNode();
			var factory = Cache.ServiceLocator.GetInstance<ILexEntryFactory>();
			var entry = factory.Create();
			var settings = new GeneratorSettings(Cache,new ReadOnlyPropertyTable(_flexComponentParameters.PropertyTable), false, false, null);
			//SUT
			Assert.Throws(typeof(ArgumentNullException), () => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(null, mainEntryNode, null, settings));
			Assert.Throws(typeof(ArgumentNullException), () => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entry, (ConfigurableDictionaryNode)null, null, settings));
			Assert.Throws(typeof(ArgumentNullException), () => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entry, mainEntryNode, null, null));
		}

		[Test]
		public void GenerateXHTMLForEntry_BadConfigurationThrows()
		{
			var mainEntryNode = new ConfigurableDictionaryNode();
			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			var settings = new GeneratorSettings(Cache, new ReadOnlyPropertyTable(_flexComponentParameters.PropertyTable), false, false, null);
			//SUT
			//Test a blank main node description
			Assert.That(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entry, mainEntryNode, null, settings),
				Throws.InstanceOf<ArgumentException>().With.Message.Contains("Invalid configuration"));
			//Test a configuration with a valid but incorrect type
			mainEntryNode.FieldDescription = "LexSense";
			Assert.That(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entry, mainEntryNode, null, settings),
				Throws.InstanceOf<ArgumentException>().With.Message.Contains("doesn't configure this type"));
		}

		[Test]
		public void GenerateXHTMLForEntry_HeadwordConfigurationGeneratesCorrectResult()
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
			AddHeadwordToEntry(entry, "HeadWordTest", _wsFr);
			var settings = new GeneratorSettings(Cache, new ReadOnlyPropertyTable(_flexComponentParameters.PropertyTable), false, false, null);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entry, mainEntryNode, null, settings);
			const string frenchHeadwordOfHeadwordTest = "/div[@class='lexentry']/span[@class='headword']/span[@lang='fr']/a[text()='HeadWordTest']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(frenchHeadwordOfHeadwordTest, 1);
		}

		[Test]
		public void GenerateXHTMLForEntry_SortByHeadwordWithSpecificWsGeneratesLetterHeadings()
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
				var recordList = RecordList.ActiveRecordListRepository.ActiveRecordList;
				recordList.SortName = "Headword (fr)";
				xhtmlPath = ConfiguredXHTMLGenerator.SavePreviewHtmlWithStyles(new[] { firstAEntry.Hvo }, pubEverything, model, _flexComponentParameters.PropertyTable, Cache, recordList);
				AssertThatXmlIn.File(xhtmlPath).HasSpecifiedNumberOfMatchesForXpath(letterHeadingXPath, 1);
			}
			finally
			{
				DeleteTempXhtmlAndCssFiles(xhtmlPath);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_LexemeFormConfigurationGeneratesCorrectResult()
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
			var settings = new GeneratorSettings(Cache, new ReadOnlyPropertyTable(_flexComponentParameters.PropertyTable), false, false, null);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entry, mainEntryNode, null, settings);
			const string frenchLexForm = "/div[@class='lexentry']/span[@class='lexemeformoa']/span[@lang='fr']/a[text()='LexemeFormTest']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(frenchLexForm, 1);
		}

		[Test]
		public void GenerateXHTMLForEntry_PronunciationLocationGeneratesCorrectResult()
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
			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entry, mainEntryNode, null, settings);
			const string hereLocation = "/div[@class='lexentry']/span[@class='pronunciations']/span[@class='pronunciation']/span[@class='location']/span[@class='name']/span[@lang='fr' and text()='Here!']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(hereLocation, 1);
		}

		[Test]
		public void GenerateXHTMLForEntry_PronunciationVideoFileGeneratesAnchorTag()
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
				DictionaryNodeOptions = GetFullyEnabledListOptions(DictionaryNodeListOptions.ListIds.Variant),
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
			CreateVariantForm(Cache, entry, variant, "Spelling Variant"); // we need a real Variant Type to pass the list options test
			// Create a folder in the project to hold the media files
			var folder = Cache.ServiceLocator.GetInstance<ICmFolderFactory>().Create();
			Cache.LangProject.MediaOC.Add(folder);
			// Create and fill in the media files
			const string expectedMediaFolder = @"Src/LanguageExplorerTests/Works/TestData/LinkedFiles/AudioVisual/";
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
			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);

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
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entry, mainEntryNode, null, settings);
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
		public void GenerateXHTMLForEntry_NoEnabledConfigurationsWritesNothing()
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

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entryOne, mainEntryNode, null, settings);
			Assert.IsEmpty(result, "Should not have generated anything for a disabled node");
		}

		[Test]
		public void GenerateXHTMLForEntry_HomographNumbersGeneratesCorrectResult()
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

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			XHTMLStringBuilder.AppendLine("<TESTWRAPPER>"); //keep the xml valid (single root element)
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entryOne, mainEntryNode, null, settings);
			XHTMLStringBuilder.Append(result);
			result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entryTwo, mainEntryNode, null, settings);
			XHTMLStringBuilder.Append(result);
			XHTMLStringBuilder.AppendLine("</TESTWRAPPER>");

			var entryWithHomograph = "/TESTWRAPPER/div[@class='lexentry']/span[@class='homographnumber' and text()='1']";
			AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(entryWithHomograph, 1);
			entryWithHomograph = entryWithHomograph.Replace('1', '2');
			AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(entryWithHomograph, 1);
		}

		[Test]
		public void GenerateXHTMLForEntry_HeadwordRefConfigurationGeneratesWithTwoWS()
		{
			var mainEntry = CreateInterestingLexEntry(Cache, "MainEntry");
			var compareReferencedEntry = CreateInterestingLexEntry(Cache, "bFR", "b comparable");
			AddHeadwordToEntry(compareReferencedEntry, "bEN", _wsEn);

			const string comRefTypeName = "Compare";
			var comRefType = CreateLexRefType(LexRefTypeTags.MappingTypes.kmtEntryCollection, comRefTypeName, "cf", string.Empty, string.Empty);

			CreateLexReference(comRefType, new List<ICmObject> { mainEntry, compareReferencedEntry });

			var mainEntryNode = ModelForCrossReferences(new[] { comRefType.Guid.ToString() });
			// SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(mainEntry, mainEntryNode, null, DefaultSettings);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(crossRefOwnerTypeXpath, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(CrossRefOwnerTypeXpath(comRefTypeName), 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(HeadwordWsInCrossRefsXpath("en", "bEN"), 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(HeadwordWsInCrossRefsXpath("fr", "bFR"), 1);
		}

		[Test]
		public void GenerateXHTMLForEntry_OneSenseWithGlossGeneratesCorrectResult()
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

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, null, settings);
			const string oneSenseWithGlossOfGloss = xpathThruSense + "//span[@lang='en' and text()='gloss']";
			//This assert is dependent on the specific entry data created in CreateInterestingLexEntry
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(oneSenseWithGlossOfGloss, 1);
		}

		[Test]
		public void GenerateXHTMLForEntry_OneEntryWithSenseAndOneWithoutWorks()
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
			AddHeadwordToEntry(entryOne, "FirstHeadword", _wsFr);
			var entryTwo = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(entryTwo, "SecondHeadword", _wsFr);
			entryTwo.SensesOS.Clear();
			var entryOneId = entryOne.Guid;
			var entryTwoId = entryTwo.Guid;

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			XHTMLStringBuilder.AppendLine("<TESTWRAPPER>"); //keep the xml valid (single root element)
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entryOne, mainEntryNode, null, settings);
			XHTMLStringBuilder.Append(result);
			result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entryTwo, mainEntryNode, null, settings);
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
		public void GenerateXHTMLForEntry_DefaultRootGeneratesResult()
		{
			var defaultRoot = string.Concat(
				Path.Combine(FwDirectoryFinder.DefaultConfigurations, "Dictionary", "Root"), LanguageExplorerConstants.DictionaryConfigurationFileExtension);
			var entry = CreateInterestingLexEntry(Cache);
			var dictionaryModel = new DictionaryConfigurationModel(defaultRoot, Cache);
			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entry, dictionaryModel.Parts[0], DefaultDecorator, settings);
			var entryExists = "/div[@class='entry' and @id='g" + entry.Guid + "']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(entryExists, 1);
		}

		[Test]
		public void GenerateXHTMLForEntry_DoesNotDescendThroughDisabledNode()
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

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entryOne, mainEntryNode, null, settings);
			const string sensesThatShouldNotBe = "/div[@class='entry']/span[@class='senses']";
			const string headwordThatShouldNotBe = "//span[@class='gloss']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(sensesThatShouldNotBe, 0);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(headwordThatShouldNotBe, 0);
		}

		[Test]
		public void GenerateXHTMLForEntry_ProduceNothingWithOnlyDisabledNode()
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

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entryOne, mainEntryNode, null, settings);
			Assert.IsEmpty(result, "With only one subnode that is disabled, there should be nothing generated!");
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
			msa.PartOfSpeechRA.Abbreviation.set_String(_wsEn, "Blah");
			AddSenseToEntry(entry, "second sense", _wsEn, Cache);
			var secondMsa = Cache.ServiceLocator.GetInstance<IMoStemMsaFactory>().Create();
			var secondSense = entry.SensesOS[1];
			entry.MorphoSyntaxAnalysesOC.Add(secondMsa);
			secondSense.MorphoSyntaxAnalysisRA = secondMsa;
			secondMsa.PartOfSpeechRA = pos;

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			// SUT
			var xhtmlString = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entry, mainEntryNode, null, settings);
			const string sharedGramInfoPath = "//div[@class='lexentry']/span[@class='senses']/span[@class='sharedgrammaticalinfo']";
			const string gramInfoPath = "//div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense']/span[@class='msas']/span[@class='mlpartofspeech']";
			AssertThatXmlIn.String(xhtmlString).HasNoMatchForXpath(gramInfoPath);
			AssertThatXmlIn.String(xhtmlString).HasSpecifiedNumberOfMatchesForXpath(sharedGramInfoPath, 1);
		}
		[Test]
		public void GenerateXHTMLForEntry_TwoSensesWithSameInfo_ThirdSenseNotPublished_ShowGramInfoFirst()
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
			msa.PartOfSpeechRA.Abbreviation.set_String(_wsEn, "Noun");

			// Add second sense; same msa
			AddSenseToEntry(entry, "second sense", _wsEn, Cache);
			var secondMsa = Cache.ServiceLocator.GetInstance<IMoStemMsaFactory>().Create();
			var secondSense = entry.SensesOS[1];
			entry.MorphoSyntaxAnalysesOC.Add(secondMsa);
			secondSense.MorphoSyntaxAnalysisRA = secondMsa;
			secondMsa.PartOfSpeechRA = pos1;

			// Add third sense; different msa
			AddSenseToEntry(entry, "third sense", _wsEn, Cache);
			var thirdMsa = Cache.ServiceLocator.GetInstance<IMoStemMsaFactory>().Create();
			var thirdSense = entry.SensesOS[2];
			entry.MorphoSyntaxAnalysesOC.Add(thirdMsa);
			thirdSense.MorphoSyntaxAnalysisRA = thirdMsa;
			thirdMsa.PartOfSpeechRA = pos2;
			thirdMsa.PartOfSpeechRA.Abbreviation.set_String(_wsEn, "Verb");

			// Setup publication
			// If the 3rd sense with the different msa is NOT published in the Main Dictionary
			// then when we generate XHTML for Main Dictionary, the shared grammatical info
			// (shared between the other two senses) should cause the gramm. info to be
			// put out front.
			var mainDict = CreatePublicationType("Main Dictionary");
			thirdSense.DoNotPublishInRC.Add(mainDict);

			// create decorator
			var mainDictionaryDecorator = new DictionaryPublicationDecorator(Cache, (ISilDataAccessManaged)Cache.MainCacheAccessor,
				Cache.ServiceLocator.GetInstance<Virtuals>().LexDbEntries, mainDict);
			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			// SUT
			var xhtmlString = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entry, mainEntryNode, mainDictionaryDecorator, settings);
			const string sharedGramInfoPath = "//div[@class='lexentry']/span[@class='senses']/span[@class='sharedgrammaticalinfo']";
			const string gramInfoPath = "//div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense']/span[@class='msa']/span[@class='mlpartofspeech']";
			AssertThatXmlIn.String(xhtmlString).HasNoMatchForXpath(gramInfoPath);
			AssertThatXmlIn.String(xhtmlString).HasSpecifiedNumberOfMatchesForXpath(sharedGramInfoPath, 1);
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
			msa.PartOfSpeechRA.Abbreviation.set_String(_wsEn, "Blah");
			AddSenseToEntry(entry, "second sense", _wsEn, Cache);
			var secondMsa = Cache.ServiceLocator.GetInstance<IMoStemMsaFactory>().Create();
			var secondSense = entry.SensesOS[1];
			entry.MorphoSyntaxAnalysesOC.Add(secondMsa);
			secondSense.MorphoSyntaxAnalysisRA = secondMsa;
			secondMsa.PartOfSpeechRA = pos2;
			secondMsa.PartOfSpeechRA.Abbreviation.set_String(_wsEn, "NotBlah");

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			// SUT
			var xhtmlString = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entry, mainEntryNode, null, settings);
			const string sharedGramInfoPath = "//div[@class='lexentry']/span[@class='sensesos']/span[@class='sharedgrammaticalinfo']";
			const string gramInfoPath = "//div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense']/span[@class='msas']/span[@class='mlpartofspeech']";
			AssertThatXmlIn.String(xhtmlString).HasSpecifiedNumberOfMatchesForXpath(gramInfoPath, 2);
			AssertThatXmlIn.String(xhtmlString).HasNoMatchForXpath(sharedGramInfoPath);
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
			AddSenseToEntry(entry, "sense 2", _wsEn, Cache);

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			// SUT
			var xhtmlString = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entry, mainEntryNode, null, settings);
			const string sharedGramInfoPath = "//div[@class='lexentry']/span[@class='sensesos']/span[@class='sharedgrammaticalinfo']";
			const string gramInfoPath = "//div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense']/span[@class='msas']/span[@class='mlpartofspeech']";
			AssertThatXmlIn.String(xhtmlString).HasNoMatchForXpath(gramInfoPath);
			AssertThatXmlIn.String(xhtmlString).HasNoMatchForXpath(sharedGramInfoPath);
		}

		[Test]
		public void GenerateXHTMLForEntry_MorphemeType()
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

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			// SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entry, mainEntryNode, null, settings);
			const string morphTypePath = "//span[@class='morphosyntaxanalysis']/span[@class='morphtypes']/span[@class='morphtype']/span[@class='abbreviation']/span[@lang='en' and text()='sfx']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(morphTypePath, 1);
		}

		[Test]
		public void GenerateXHTMLForEntry_MakesSpanForRA()
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

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			// SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entry, mainEntryNode, null, settings);
			const string gramInfoPath = xpathThruSense + "/span[@class='morphosyntaxanalysis']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(gramInfoPath, 1);
		}

		[Test]
		public void GenerateXHTMLForEntry_CmObjectWithNoEnabledChildrenSkipsSpan()
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

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			// SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entry, mainEntryNode, null, settings);
			const string gramInfoPath = xpathThruSense + "/span[@class='morphosyntaxanalysis']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(gramInfoPath, 0);
		}

		/// <summary>
		/// If the dictionary configuration specifies to export grammatical info, but there is no such grammatical info object to export, don't write a span.
		/// </summary>
		[Test]
		public void GenerateXHTMLForEntry_DoesNotMakeSpanForRAIfNoData()
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

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			// SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entry, mainEntryNode, null, settings);
			const string gramInfoPath = xpathThruSense + "/span[@class='morphosyntaxanalysis']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(gramInfoPath, 0);
		}

		/// <summary>
		/// If the dictionary configuration specifies to export scientific category, but there is no data in the field to export, don't write a span.
		/// </summary>
		[Test]
		public void GenerateXHTMLForEntry_DoesNotMakeSpanForTSStringIfNoData()
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

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			// SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entry, mainEntryNode, null, settings);
			const string scientificCatPath = xpathThruSense + "/span[@class='scientificname']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(scientificCatPath, 0);
		}

		[Test]
		public void GenerateXHTMLForEntry_SupportsGramAbbrChildOfMSARA()
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
				Children=new List<ConfigurableDictionaryNode>{gramInfoNode}
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

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			// SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entry, mainEntryNode, null, settings);

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
		public void GenerateXHTMLForEntry_DefinitionOrGlossWorks()
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

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entryOne, mainEntryNode, null, settings);
			const string senseWithdefinitionOrGloss = "//span[@class='sense']/span[@class='definitionorgloss']/span[text()='gloss']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(senseWithdefinitionOrGloss, 1);
		}

		[Test]
		public void GenerateXHTMLForEntry_DefinitionOrGlossWorks_WithAbbrev()
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

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entryOne, mainEntryNode, null, settings);
			const string senseWithdefinitionOrGloss =
				"//span[@class='sense']/span[@class='definitionorgloss']/span[@class='writingsystemprefix' and normalize-space(text())='Eng']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(senseWithdefinitionOrGloss, 1);
		}

		[Test]
		public void GenerateXHTMLForEntry_OtherReferencedComplexForms()
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
				FieldDescription = "OwningEntry", SubField = "HeadWordRef",
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

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(mainEntry, mainEntryNode, null, settings);
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
		public void GenerateXHTMLForEntry_DuplicateConfigNodeWithSpaceWorks()
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

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entryOne, mainEntryNode, null, settings);
			const string senseWithHyphenSuffix = "//span[@class='senses_test-one']/span[@class='sense_test-one']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(senseWithHyphenSuffix, 1);
		}

		[Test]
		public void GenerateXHTMLForEntry_DuplicateConfigNodeWithPuncWorks()
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

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entryOne, mainEntryNode, null, settings);
			const string senseWithHyphenSuffix = "//span[@class='senses_-test']/span[@class='sense_-test']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(senseWithHyphenSuffix, 1);
		}

		[Test]
		public void GenerateXHTMLForEntry_DuplicateConfigNodeWithMultiPuncWorks()
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

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entryOne, mainEntryNode, null, settings);
			const string senseWithHyphenSuffix = "//span[@class='senses_-test-']/span[@class='sense_-test-']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(senseWithHyphenSuffix, 1);
		}

		[Test]
		public void GenerateXHTMLForEntry_HeadWordRefVirtualPropWorks()
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
			AddHeadwordToEntry(entryThree, entryThreeForm, _wsFr);
			var complexEntryRef = Cache.ServiceLocator.GetInstance<ILexEntryRefFactory>().Create();
			entryTwo.EntryRefsOS.Add(complexEntryRef);
			complexEntryRef.RefType = LexEntryRefTags.krtComplexForm;
			complexEntryRef.ComponentLexemesRS.Add(entryOne.SensesOS[0]);
			complexEntryRef.ComponentLexemesRS.Add(entryThree);
			complexEntryRef.PrimaryLexemesRS.Add(entryThree);
			complexEntryRef.ShowComplexFormsInRS.Add(entryThree);
			complexEntryRef.ShowComplexFormsInRS.Add(entryOne.SensesOS[0]);

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			// SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entryOne, mainEntryNode, null, settings);
			var headwordMatch = string.Format("//span[@class='{0}']//span[@class='{1}']/span[text()='{2}']",
														 nters, headWord, entryThreeForm);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(headwordMatch, 1);
		}

		[Test]
		public void GenerateXHTMLForEntry_EtymologyLanguageWorks()
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
			language.Abbreviation.set_String(_wsEn, TsStringUtils.MakeString("ar", _wsEn));
			language.Name.set_String(_wsEn, TsStringUtils.MakeString("Arabic", _wsEn));
			var entryOne = CreateInterestingLexEntry(Cache);
			var etym = Cache.ServiceLocator.GetInstance<ILexEtymologyFactory>().Create();
			entryOne.EtymologyOS.Add(etym);
			etym.LanguageNotes.SetAnalysisDefaultWritingSystem("Georgian");
			etym.LanguageRS.Add(language);

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entryOne, mainEntryNode, null, settings);
			const string etymologyWithArabicSrcLanguage = "//span[@class='etymologies']/span[@class='etymology']/span[@class='languages']/span[@class='language']/span[@class='abbreviation']/span[@lang='en' and text()='ar']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(etymologyWithArabicSrcLanguage, 1);
			const string etymologyWithGeorgianNotes = "//span[@class='etymologies']/span[@class='etymology']/span[@class='languagenotes']/span[@lang='en' and text()='Georgian']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(etymologyWithGeorgianNotes, 1);
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
			ConfiguredXHTMLGenerator.AssemblyFile = "LanguageExplorerTests";
			var stringNode = new ConfigurableDictionaryNode { FieldDescription = "RootMember" };
			var rootNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LanguageExplorerTests.Works.TestRootClass",
				Children = new List<ConfigurableDictionaryNode> { stringNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(rootNode);
			var result = PropertyType.InvalidProperty;
			// SUT
			Assert.DoesNotThrow(() => result = ConfiguredXHTMLGenerator.GetPropertyTypeForConfigurationNode(stringNode));
			Assert.That(result, Is.EqualTo(PropertyType.PrimitiveType));
		}

		[Test]
		public void GetPropertyTypeForConfigurationNode_InterfacePropertyWorks()
		{
			ConfiguredXHTMLGenerator.AssemblyFile = "LanguageExplorerTests";
			var interfaceNode = new ConfigurableDictionaryNode { FieldDescription = "TestString" };
			var memberNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "RootMember",
				Children = new List<ConfigurableDictionaryNode> { interfaceNode }
			};
			var rootNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LanguageExplorerTests.Works.TestRootClass",
				Children = new List<ConfigurableDictionaryNode> { memberNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(rootNode);
			var result = PropertyType.InvalidProperty;
			// SUT
			Assert.DoesNotThrow(() => result = ConfiguredXHTMLGenerator.GetPropertyTypeForConfigurationNode(interfaceNode));
			Assert.That(result, Is.EqualTo(PropertyType.PrimitiveType));
		}

		[Test]
		public void GetPropertyTypeForConfigurationNode_FirstParentInterfacePropertyIsUsable()
		{
			ConfiguredXHTMLGenerator.AssemblyFile = "LanguageExplorerTests";
			var interfaceNode = new ConfigurableDictionaryNode { FieldDescription = "TestMoForm" };
			var memberNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "RootMember",
				Children = new List<ConfigurableDictionaryNode> { interfaceNode }
			};
			var rootNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LanguageExplorerTests.Works.TestRootClass",
				Children = new List<ConfigurableDictionaryNode> { memberNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(rootNode);
			var result = PropertyType.InvalidProperty;
			// SUT
			Assert.DoesNotThrow(() => result = ConfiguredXHTMLGenerator.GetPropertyTypeForConfigurationNode(interfaceNode));
			Assert.That(result, Is.EqualTo(PropertyType.MoFormType));
		}

		[Test]
		public void GetPropertyTypeForConfigurationNode_SecondParentInterfacePropertyIsUsable()
		{
			ConfiguredXHTMLGenerator.AssemblyFile = "LanguageExplorerTests";
			var interfaceNode = new ConfigurableDictionaryNode { FieldDescription = "TestIcmObject" };
			var memberNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "RootMember",
				Children = new List<ConfigurableDictionaryNode> { interfaceNode }
			};
			var rootNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LanguageExplorerTests.Works.TestRootClass",
				Children = new List<ConfigurableDictionaryNode> { memberNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(rootNode);
			var result = PropertyType.InvalidProperty;
			// SUT
			Assert.DoesNotThrow(() => result = ConfiguredXHTMLGenerator.GetPropertyTypeForConfigurationNode(interfaceNode));
			Assert.That(result, Is.EqualTo(PropertyType.CmObjectType));
		}

		[Test]
		public void GetPropertyTypeForConfigurationNode_GrandparentInterfacePropertyIsUsable()
		{
			ConfiguredXHTMLGenerator.AssemblyFile = "LanguageExplorerTests";
			var interfaceNode = new ConfigurableDictionaryNode { FieldDescription = "TestCollection" };
			var memberNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "RootMember",
				Children = new List<ConfigurableDictionaryNode> { interfaceNode }
			};
			var rootNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LanguageExplorerTests.Works.TestRootClass",
				Children = new List<ConfigurableDictionaryNode> { memberNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(rootNode);
			var result = PropertyType.InvalidProperty;
			// SUT
			Assert.DoesNotThrow(() => result = ConfiguredXHTMLGenerator.GetPropertyTypeForConfigurationNode(interfaceNode));
			Assert.That(result, Is.EqualTo(PropertyType.CollectionType));
		}

		[Test]
		public void GetPropertyTypeForConfigurationNode_NonInterfaceMemberIsUsable()
		{
			ConfiguredXHTMLGenerator.AssemblyFile = "LanguageExplorerTests";
			var stringNodeInClass = new ConfigurableDictionaryNode { FieldDescription = "TestNonInterfaceString" };
			var memberNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ConcreteMember",
				Children = new List<ConfigurableDictionaryNode> { stringNodeInClass }
			};
			var rootNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LanguageExplorerTests.Works.TestRootClass",
				Children = new List<ConfigurableDictionaryNode> { memberNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(rootNode);
			var result = PropertyType.InvalidProperty;
			// SUT
			Assert.DoesNotThrow(() => result = ConfiguredXHTMLGenerator.GetPropertyTypeForConfigurationNode(stringNodeInClass));
			Assert.That(result, Is.EqualTo(PropertyType.PrimitiveType));
		}

		[Test]
		public void GetPropertyTypeForConfigurationNode_InvalidChildDoesNotThrow()
		{
			ConfiguredXHTMLGenerator.AssemblyFile = "LanguageExplorerTests";
			var interfaceNode = new ConfigurableDictionaryNode { FieldDescription = "TestCollection" };
			var memberNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ConcreteMember",
				SubField = "TestNonInterfaceString",
				Children = new List<ConfigurableDictionaryNode> { interfaceNode }
			};
			var rootNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LanguageExplorerTests.Works.TestRootClass",
				Children = new List<ConfigurableDictionaryNode> { memberNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(rootNode);
			var result = PropertyType.PrimitiveType;
			// SUT
			Assert.DoesNotThrow(() => result = ConfiguredXHTMLGenerator.GetPropertyTypeForConfigurationNode(interfaceNode));
			Assert.That(result, Is.EqualTo(PropertyType.InvalidProperty));
		}

		[Test]
		public void GetPropertyTypeForConfigurationNode_SubFieldWorks()
		{
			ConfiguredXHTMLGenerator.AssemblyFile = "LanguageExplorerTests";
			var memberNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ConcreteMember",
				SubField = "TestNonInterfaceString"
			};
			var rootNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LanguageExplorerTests.Works.TestRootClass",
				Children = new List<ConfigurableDictionaryNode> { memberNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(rootNode);
			var result = PropertyType.InvalidProperty;
			// SUT
			Assert.DoesNotThrow(() => result = ConfiguredXHTMLGenerator.GetPropertyTypeForConfigurationNode(memberNode, (IFwMetaDataCacheManaged)Cache.MetaDataCacheAccessor));
			Assert.That(result, Is.EqualTo(PropertyType.PrimitiveType));
		}

		[Test]
		public void GetPropertyTypeForConfigurationNode_InvalidSubFieldReturnsInvalidProperty()
		{
			ConfiguredXHTMLGenerator.AssemblyFile = "LanguageExplorerTests";
			var memberNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ConcreteMember",
				SubField = "NonExistantSubField"
			};
			var rootNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LanguageExplorerTests.Works.TestRootClass",
				Children = new List<ConfigurableDictionaryNode> { memberNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(rootNode);
			var result = PropertyType.PrimitiveType;
			// SUT
			Assert.DoesNotThrow(() => result = ConfiguredXHTMLGenerator.GetPropertyTypeForConfigurationNode(memberNode));
			Assert.That(result, Is.EqualTo(PropertyType.InvalidProperty));
		}

		[Test]
		public void GetPropertyTypeForConfigurationNode_InvalidRootThrowsWithMessage()
		{
			ConfiguredXHTMLGenerator.AssemblyFile = "LanguageExplorerTests";
			var rootNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LanguageExplorerTests.Works.NonExistantClass",
			};
			// SUT
			Assert.That(() => ConfiguredXHTMLGenerator.GetPropertyTypeForConfigurationNode(rootNode),
				Throws.InstanceOf<ArgumentException>().With.Message.Contains(rootNode.FieldDescription));
		}

		[Test]
		public void GetPropertyTypeForConfigurationNode_PictureFileReturnsCmPictureType()
		{
			ConfiguredXHTMLGenerator.AssemblyFile = "LanguageExplorerTests";
			var pictureFileNode = new ConfigurableDictionaryNode { FieldDescription = "PictureFileRA" };
			var memberNode = new ConfigurableDictionaryNode
			{
				DictionaryNodeOptions = new DictionaryNodePictureOptions(),
				FieldDescription = "Pictures",
				Children = new List<ConfigurableDictionaryNode> { pictureFileNode }
			};
			var rootNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LanguageExplorerTests.Works.TestPictureClass",
				Children = new List<ConfigurableDictionaryNode> { memberNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(rootNode);
			var result = PropertyType.InvalidProperty;
			// SUT
			Assert.DoesNotThrow(() => result = ConfiguredXHTMLGenerator.GetPropertyTypeForConfigurationNode(pictureFileNode));
			Assert.That(result, Is.EqualTo(PropertyType.CmPictureType));
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
				var locator = Cache.ServiceLocator;
				// Set custom field data
				var multiParaHvo = Cache.MainCacheAccessor.MakeNewObject(StTextTags.kClassId, testEntry.Hvo, customField.Flid, -2);
				var textObject = locator.GetInstance<IStTextRepository>().GetObject(multiParaHvo);
				var paragraph = locator.GetInstance<IStTxtParaFactory>().Create();
				textObject.ParagraphsOS.Add(paragraph);
				paragraph.Contents = TsStringUtils.MakeString(customData, _wsFr);
				//SUT
				var type = ConfiguredXHTMLGenerator.GetPropertyTypeForConfigurationNode(customFieldNode, (IFwMetaDataCacheManaged)Cache.MetaDataCacheAccessor);
				Assert.AreEqual(PropertyType.PrimitiveType, type);
			}
		}

		[Test]
		public void IsMainEntry_ReturnsFalseForMinorEntry()
		{
			var mainEntry = CreateInterestingLexEntry(Cache);
			var variantEntry = CreateInterestingLexEntry(Cache);
			CreateVariantForm(Cache, mainEntry, variantEntry);
			var complexEntry = CreateInterestingLexEntry(Cache);
			CreateComplexForm(Cache, mainEntry, complexEntry, false);
			var rootConfig = new DictionaryConfigurationModel(true);
			var lexemeConfig = new DictionaryConfigurationModel(false);
			// SUT
			Assert.False(ConfiguredXHTMLGenerator.IsMainEntry(variantEntry, lexemeConfig), "Variant, Lexeme");
			Assert.False(ConfiguredXHTMLGenerator.IsMainEntry(variantEntry, rootConfig), "Variant, Root");
			Assert.False(ConfiguredXHTMLGenerator.IsMainEntry(complexEntry, rootConfig), "Complex, Root");
			// (complex entries are considered main entries in lexeme-based configs)
		}

		[Test]
		public void IsMainEntry_ReturnsTrueForMainEntry()
		{
			var mainEntry = CreateInterestingLexEntry(Cache);
			var minorEntry = CreateInterestingLexEntry(Cache);
			CreateVariantForm(Cache, mainEntry, minorEntry);
			var complexEntry = CreateInterestingLexEntry(Cache);
			CreateComplexForm(Cache, mainEntry, complexEntry, false);
			var rootConfig = new DictionaryConfigurationModel(true);
			var lexemeConfig = new DictionaryConfigurationModel(false);
			// SUT
			Assert.That(ConfiguredXHTMLGenerator.IsMainEntry(mainEntry, rootConfig), "Main, Root");
			Assert.That(ConfiguredXHTMLGenerator.IsMainEntry(mainEntry, lexemeConfig), "Main, Lexeme");
			Assert.That(ConfiguredXHTMLGenerator.IsMainEntry(complexEntry, lexemeConfig), "Complex, Lexeme");
			// (complex entries are considered minor entries in root-based configs)
			Assert.That(ConfiguredXHTMLGenerator.IsMainEntry(Cache.ServiceLocator.GetInstance<IReversalIndexEntryFactory>().Create(),
				new DictionaryConfigurationModel()), "Reversal Index Entries are always considered Main Entries");
		}

		[Test]
		public void GenerateEntryHtmlWithStyles_NullEntryThrowsArgumentNull()
		{
			Assert.Throws<ArgumentNullException>(() => ConfiguredXHTMLGenerator.GenerateEntryHtmlWithStyles(null, new DictionaryConfigurationModel(), null, _flexComponentParameters.PropertyTable, Cache));
		}

		[Test]
		public void GenerateEntryHtmlWithStyles_SelectsDirectionUsingDictionaryNormal()
		{
			try
			{
				SetDictionaryNormalDirection(new InheritableStyleProp<TriStateBool>(TriStateBool.triTrue));
				var configModel = CreateInterestingConfigurationModel(Cache, _flexComponentParameters.PropertyTable);
				var mainEntry = CreateInterestingLexEntry(Cache);
				//SUT
				var xhtml = ConfiguredXHTMLGenerator.GenerateEntryHtmlWithStyles(mainEntry, configModel, DefaultDecorator, _flexComponentParameters.PropertyTable, Cache);
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
			var configModel = CreateInterestingConfigurationModel(Cache, _flexComponentParameters.PropertyTable);
			//SUT
			var xhtml = ConfiguredXHTMLGenerator.GenerateEntryHtmlWithStyles(minorEntry, configModel, DefaultDecorator, _flexComponentParameters.PropertyTable, Cache);
			// this test relies on specific test data from CreateInterestingConfigurationModel
			const string xpath = "/html/body/div[@class='minorentry']/span[@class='entry']";
			AssertThatXmlIn.String(xhtml).HasSpecifiedNumberOfMatchesForXpath(xpath, 1);
		}

		[Test]
		public void GenerateEntryHtmlWithStyles_MinorEntryUnCheckedItemsGenerateNothing()
		{
			var configModel = CreateInterestingConfigurationModel(Cache, _flexComponentParameters.PropertyTable);
			var mainEntry = CreateInterestingLexEntry(Cache);
			var minorEntry = CreateInterestingLexEntry(Cache);
			CreateVariantForm(Cache, mainEntry, minorEntry);
			configModel.Parts[1].DictionaryNodeOptions = configModel.Parts[2].DictionaryNodeOptions =
				GetListOptionsForItems(DictionaryNodeListOptions.ListIds.Minor, new ICmPossibility[0]);
			SetPublishAsMinorEntry(minorEntry, true);
			//SUT
			var xhtml = ConfiguredXHTMLGenerator.GenerateEntryHtmlWithStyles(minorEntry, configModel, DefaultDecorator, _flexComponentParameters.PropertyTable, Cache);
			// this test relies on specific test data from CreateInterestingConfigurationModel
			const string xpath = "/html/body/div[@class='minorentry']/span[@class='entry']";
			// only the variant is selected, so the other minor entry should not have been generated
			AssertThatXmlIn.String(xhtml).HasSpecifiedNumberOfMatchesForXpath(xpath, 0);
		}

		[Test]
		public void GenerateEntryHtmlWithStyles_DoesNotShowHiddenMinorEntries()
		{
			var configModel = CreateInterestingConfigurationModel(Cache, _flexComponentParameters.PropertyTable);
			var mainEntry = CreateInterestingLexEntry(Cache);
			var minorEntry = CreateInterestingLexEntry(Cache);
			CreateVariantForm(Cache, mainEntry, minorEntry);
			SetPublishAsMinorEntry(minorEntry, false);

			//SUT
			var xhtml = ConfiguredXHTMLGenerator.GenerateEntryHtmlWithStyles(minorEntry, configModel, DefaultDecorator, _flexComponentParameters.PropertyTable, Cache);
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
			var configModel = CreateInterestingConfigurationModel(Cache, _flexComponentParameters.PropertyTable);
			if (verifyPrefersVariant) // Exclude Complex Form Parts from those counted.
				configModel.Parts.Where(part => part.Label.Contains("Complex")).ForEach(part => part.CSSClassNameOverride = "complexentry");
			Assert.That(ConfiguredXHTMLGenerator.IsListItemSelectedForExport(configModel.Parts[1], minorEntry),
				"This test is valid only if the minor entry matches more than one node");
			Assert.That(ConfiguredXHTMLGenerator.IsListItemSelectedForExport(configModel.Parts[2], minorEntry),
				"This test is valid only if the minor entry matches more than one node");
			//SUT
			var xhtml = ConfiguredXHTMLGenerator.GenerateEntryHtmlWithStyles(minorEntry, configModel, DefaultDecorator, _flexComponentParameters.PropertyTable, Cache);
			// this test relies on specific test data from CreateInterestingConfigurationModel
			const string xpath = "/html/body/div[@class='minorentry']/span[@class='entry']";
			AssertThatXmlIn.String(xhtml).HasSpecifiedNumberOfMatchesForXpath(xpath, 1);
		}

		[Test]
		public void GenerateXHTMLForEntry_LexemeBasedConsidersComplexFormsMainEntries()
		{
			var configModel = CreateInterestingConfigurationModel(Cache, _flexComponentParameters.PropertyTable);
			for (var i = 1; i < configModel.Parts.Count; i++)
				configModel.Parts[i].IsEnabled = false; // don't display Minor entries
			var componentEntry = CreateInterestingLexEntry(Cache);
			var complexEntry = CreateInterestingLexEntry(Cache);
			CreateComplexForm(Cache, componentEntry, complexEntry, false);
			configModel.Parts[1].DictionaryNodeOptions = configModel.Parts[2].DictionaryNodeOptions =
				GetListOptionsForItems(DictionaryNodeListOptions.ListIds.Minor, new ICmPossibility[0]);
			SetPublishAsMinorEntry(complexEntry, false);
			//SUT
			var xhtml = ConfiguredXHTMLGenerator.GenerateEntryHtmlWithStyles(complexEntry, configModel, DefaultDecorator, _flexComponentParameters.PropertyTable, Cache);
			// this test relies on specific test data from CreateInterestingConfigurationModel
			const string xpath = "/html/body/div[@class='minorentry']/span[@class='entry']";
			// only the variant is selected, so the other minor entry should not have been generated
			AssertThatXmlIn.String(xhtml).HasSpecifiedNumberOfMatchesForXpath(xpath, 0);
		}

		/// <summary>
		/// If the numbering style for Senses says to number it, and
		/// if this is not the only sense, then number it.
		/// (See LT-17906.)
		/// </summary>
		[Test]
		public void GenerateXHTMLForEntry_SenseNumbersGeneratedForMultipleSenses()
		{
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
			AddSenseToEntry(testEntry, "second gloss", _wsEn, Cache);
			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, DefaultDecorator, settings);
			const string senseNumberOne = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense' and preceding-sibling::span[@class='sensenumber' and text()='1']]//span[@lang='en' and text()='gloss']";
			const string senseNumberTwo = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense' and preceding-sibling::span[@class='sensenumber' and text()='2']]//span[@lang='en' and text()='second gloss']";
			//This assert is dependent on the specific entry data created in CreateInterestingLexEntry
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(senseNumberOne, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(senseNumberTwo, 1);

			// Piggy-back a test for ShouldThisSenseBeNumbered
			Assert.That(ConfiguredXHTMLGenerator.ShouldThisSenseBeNumbered(testEntry.AllSenses.First(), sensesNode, testEntry.SensesOS), Is.True);
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

			Assert.That(ConfiguredXHTMLGenerator.ShouldThisSenseBeNumbered(testEntry.SensesOS[0].SensesOS[0], subSenseNode,testEntry.SensesOS[0].SensesOS), Is.True);
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
				DictionaryNodeOptions=new DictionaryNodeSenseOptions()
				{
					NumberStyle = "Dictionary-SenseNumber",
					NumberingStyle="%d"
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

			Assert.That(ConfiguredXHTMLGenerator.AreThereEnabledSubsensesWithNumberingStyle(sensesNode), Is.False, "Should have noticed that there are no enabled subsense nodes");

			// Okay, but if they are showing...
			subSenseNode.IsEnabled = true;

			Assert.That(ConfiguredXHTMLGenerator.AreThereEnabledSubsensesWithNumberingStyle(sensesNode), Is.True, "Should have seen the enabled subsense node.");
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

			Assert.That(ConfiguredXHTMLGenerator.AreThereEnabledSubsensesWithNumberingStyle(sensesNode), Is.False, "Should have return false since no numbering style");

			// Okay, but if the style for the subsense does say to number the subsenses...
			((DictionaryNodeSenseOptions)subSenseNode.DictionaryNodeOptions).NumberingStyle = "%d";

			Assert.That(ConfiguredXHTMLGenerator.AreThereEnabledSubsensesWithNumberingStyle(sensesNode), Is.True, "Should have return true since there is a numbering style");
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
		public void GenerateXHTMLForEntry_SingleSenseGetsNoSenseNumber()
		{

			var wsOpts = GetWsOptionsForLanguages(new[] { "en" });
			var glossNode = new ConfigurableDictionaryNode { FieldDescription = "Gloss", DictionaryNodeOptions = wsOpts };
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "Senses",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions { NumberEvenASingleSense = false, NumberingStyle = "%d"},
				Children = new List<ConfigurableDictionaryNode> { glossNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { sensesNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var testEntry = CreateInterestingLexEntry(Cache);

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			Assert.That(testEntry.AllSenses.Count, Is.EqualTo(1), "Test set up incorrectly. There should just be one sense.");
			Assert.That(testEntry.AllSenses.First().AllSenses.Count, Is.EqualTo(1), "Test not set up correctly. There should be no subsenses.");
			// SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, DefaultDecorator, settings);
			const string senseNumberOne = "/div[@class='lexentry']/span[@class='senses']/span[@class='sense' and preceding-sibling::span[@class='sensenumber' and text()='1']]/span[@lang='en' and text()='gloss']";
			// This assert is dependent on the specific entry data created in CreateInterestingLexEntry
			AssertThatXmlIn.String(result).HasNoMatchForXpath(senseNumberOne);

			// Piggy-back a test for ShouldThisSenseBeNumbered
			Assert.That(ConfiguredXHTMLGenerator.ShouldThisSenseBeNumbered(testEntry.AllSenses.First(), sensesNode, testEntry.SensesOS), Is.False);
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
		public void GenerateXHTMLForEntry_TurnedOffSubsensesCausesSenseToBehaveLikeSingleSense_WithNoSenseNumber()
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

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			Assert.That(testEntry.AllSenses.Count, Is.EqualTo(3), "Test set up incorrectly.");
			Assert.That(testEntry.AllSenses.First().AllSenses.Count, Is.EqualTo(3), "Test not set up correctly.");
			// SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, DefaultDecorator, settings);
			const string senseNumberXpath = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sensenumber']";
			AssertThatXmlIn.String(result).HasNoMatchForXpath(senseNumberXpath); // Should not have a sense number on top sense.

			// Piggy-back a test for ShouldThisSenseBeNumbered
			Assert.That(ConfiguredXHTMLGenerator.ShouldThisSenseBeNumbered(testEntry.AllSenses.First(), sensesNode, testEntry.SensesOS), Is.False);
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
		public void GenerateXHTMLForEntry_EmptyStyleSubsensesCausesSenseToBehaveLikeSingleSense_WithNoSenseNumber()
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

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			Assert.That(testEntry.AllSenses.Count, Is.EqualTo(3), "Test set up incorrectly.");
			Assert.That(testEntry.AllSenses.First().AllSenses.Count, Is.EqualTo(3), "Test not set up correctly.");
			// SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, DefaultDecorator, settings);
			const string senseNumberXpath = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sensenumber']";
			AssertThatXmlIn.String(result).HasNoMatchForXpath(senseNumberXpath); // Should not have a sense number on top sense.

			// Piggy-back a test for ShouldThisSenseBeNumbered
			Assert.That(ConfiguredXHTMLGenerator.ShouldThisSenseBeNumbered(testEntry.AllSenses.First(), sensesNode, testEntry.SensesOS), Is.False);
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
		public void GenerateXHTMLForEntry_SubsenseStyleInfluencesSenseNumberShown()
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

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			Assert.That(testEntry.AllSenses.Count, Is.EqualTo(3), "Test set up incorrectly.");
			Assert.That(testEntry.AllSenses.First().AllSenses.Count, Is.EqualTo(3), "Test not set up correctly.");
			// SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, DefaultDecorator, settings);
			const string senseNumberXpath = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sensenumber']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(senseNumberXpath,1); // Should have sense number on top sense.

			// Piggy-back a test for ShouldThisSenseBeNumbered
			Assert.That(ConfiguredXHTMLGenerator.ShouldThisSenseBeNumbered(testEntry.AllSenses.First(), sensesNode, testEntry.SensesOS), Is.True);
		}

		[Test]
		public void GenerateXHTMLForEntry_NumberingSingleSenseAlsoCountsSubSense()
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
			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, DefaultDecorator, settings);
			const string SenseOneSubSense = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense' and preceding-sibling::span[@class='sensenumber' and text()='1']]/span[@class='senses']/span[@class='sensecontent']//span[@lang='en' and text()='gloss1.1']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(SenseOneSubSense, 1);
		}

		[Test]
		public void GenerateXHTMLForEntry_SensesAndSubSensesWithDifferentNumberingStyle()
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
			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, DefaultDecorator, settings);
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
		public void GenerateXHTMLForEntry_SensesAndSubSensesWithNumberingStyle()
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
			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, DefaultDecorator, settings);
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
		public void GenerateXHTMLForEntry_NoSenseNumberFIfStyleSaysNoNumbering()
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
			AddSenseToEntry(testEntry, "gloss", _wsEn, Cache);
			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, DefaultDecorator, settings);
			const string senseNumberXpath = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sensenumber']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(senseNumberXpath, 0); // Should not have produced sense number if style said not to number it.

			// Piggy-back a test for ShouldThisSenseBeNumbered
			Assert.That(ConfiguredXHTMLGenerator.ShouldThisSenseBeNumbered(testEntry.AllSenses.First(), sensesNode, testEntry.SensesOS), Is.False);
		}

		[Test]
		public void GenerateXHTMLForEntry_SensesNoneAndSubSensesWithNumberingStyle()
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
			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, DefaultDecorator, settings);
			const string subSensesNumberOne = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense' and preceding-sibling::span[@class='sensenumber' and text()='1']]//span[@lang='en' and text()='second gloss2.1']";
			const string subSenseNumberTwo = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense' and preceding-sibling::span[@class='sensenumber' and text()='2']]//span[@lang='en' and text()='second gloss2.2']";
			//This assert is dependent on the specific entry data created in CreateInterestingLexEntry
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(subSensesNumberOne, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(subSenseNumberTwo, 1);
		}

		[Test]
		public void GenerateXHTMLForEntry_SensesGeneratedForMultipleSubSenses()
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
			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, DefaultDecorator, settings);
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
		public void GenerateXHTMLForEntry_SubSenseParentSenseNumberingStyleJoined()
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
			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, DefaultDecorator, settings);
			const string senseNumber =    "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense' and preceding-sibling::span[@class='sensenumber' and text()='2']]";
			const string subSenseNumber = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense' and preceding-sibling::span[@class='sensenumber' and text()='2a']]";
			const string subSubSenseNumber = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense' and preceding-sibling::span[@class='sensenumber' and text()='2aA']]";

			//This assert is dependent on the specific entry data created in CreateInterestingLexEntry
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(senseNumber, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(subSenseNumber, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(subSubSenseNumber, 1);
		}

		[Test]
		public void GenerateXHTMLForEntry_SubSenseParentSenseNumberingStyleSeparatedByDot()
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
			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, DefaultDecorator, settings);
			const string senseNumber = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense' and preceding-sibling::span[@class='sensenumber' and text()='2']]";
			const string subSenseNumber = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense' and preceding-sibling::span[@class='sensenumber' and text()='2.a']]";
			const string subSubSenseNumber = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense' and preceding-sibling::span[@class='sensenumber' and text()='2.a.A']]";

			//This assert is dependent on the specific entry data created in CreateInterestingLexEntry
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(senseNumber, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(subSenseNumber, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(subSubSenseNumber, 1);
		}

		[Test]
		public void GenerateXHTMLForEntry_SubSenseParentSenseNumberingStyleNone()
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
			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, DefaultDecorator, settings);
			const string senseNumber = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense' and preceding-sibling::span[@class='sensenumber' and text()='2']]";
			const string subSenseNumber = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense' and preceding-sibling::span[@class='sensenumber' and text()='a']]";
			const string subSubSenseNumber = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense' and preceding-sibling::span[@class='sensenumber' and text()='A']]";

			//This assert is dependent on the specific entry data created in CreateInterestingLexEntry
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(senseNumber, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(subSenseNumber, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(subSubSenseNumber, 1);
		}

		[Test]
		public void GenerateXHTMLForEntry_SubSubSensesWithNumberingStyle()
		{

			var wsOpts = GetWsOptionsForLanguages(new[] { "en" });
			var senseOptions = new DictionaryNodeSenseOptions { AfterNumber = ")", NumberingStyle = "%d", NumberEvenASingleSense = true };

			var glossNode = new ConfigurableDictionaryNode { FieldDescription = "Gloss", DictionaryNodeOptions = wsOpts };
			var subSenseNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS", CSSClassNameOverride = "shares", DictionaryNodeOptions = senseOptions
			};
			var kalashnikovSensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS", CSSClassNameOverride = "senses", DictionaryNodeOptions = senseOptions,
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
			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, DefaultDecorator, settings);
			const string senseContent = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']";
			const string senseNumberOne = senseContent + "/span[@class='sense' and preceding-sibling::span[@class='sensenumber' and text()='1']]//span[@lang='en' and text()='gloss']";
			const string senseNumberTwo = senseContent + "/span[@class='sense' and preceding-sibling::span[@class='sensenumber' and text()='2']]//span[@lang='en' and text()='second gloss']";
			const string subSenseContent = senseContent + "/span[@class='sense']/span[@class='shares senses']/span[@class='sensecontent']";
			const string subSenseNumberTwoOne = subSenseContent + "/span[@class='share sense' and preceding-sibling::span[@class='sensenumber' and text()='1']]//span[@lang='en' and text()='second gloss2.1']";
			const string subSenseNumberTwoTwo = subSenseContent + "/span[@class='share sense' and preceding-sibling::span[@class='sensenumber' and text()='2']]//span[@lang='en' and text()='second gloss2.2']";
			const string subSubSenseContent = subSenseContent + "/span[@class='share sense']/span[@class='shares senses']/span[@class='sensecontent']";
			const string subSubSenseNumberTwoOneOne = subSubSenseContent + "/span[@class='share sense' and preceding-sibling::span[@class='sensenumber' and text()='1']]//span[@lang='en' and text()='matte']";
			const string subSubSubSenseContent = subSubSenseContent + "/span[@class='share sense']/span[@class='shares senses']/span[@class='sensecontent']";
			const string subSubSubSenseNumberTwoOneOneOne = subSubSubSenseContent + "/span[@class='share sense' and preceding-sibling::span[@class='sensenumber' and text()='1']]//span[@lang='en' and text()='matte2.1']";
			const string subSubSubSenseNumberTwoOneOneTwo = subSubSubSenseContent + "/span[@class='share sense' and preceding-sibling::span[@class='sensenumber' and text()='2']]//span[@lang='en' and text()='matte2.2']";
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
		public void GenerateXHTMLForEntry_GeneratesGramInfoFirstEvenSingleSense()
		{
			var posNoun = CreatePartOfSpeech("noun", "n");

			var firstHeadword = "homme";
			var firstEntry = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(firstEntry, firstHeadword, _wsFr);
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
			var letterHeaderXPath = "//div[@class='letHead']";
			try
			{
				xhtmlPath = ConfiguredXHTMLGenerator.SavePreviewHtmlWithStyles(new[] { firstEntry.Hvo }, pubEverything, model, _flexComponentParameters.PropertyTable, Cache, _recordList);
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
		public void GenerateXHTMLForEntry_SubSensesOfSingleSenses_GetFullNumbers()
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
				FieldDescription = "SensesOS", CSSClassNameOverride = "shares", DictionaryNodeOptions = subSenseOptions
			};
			var kalashnikovSensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS", CSSClassNameOverride = "senses", DictionaryNodeOptions = senseOptions,
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
			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, DefaultDecorator, settings);
			const string senseContent = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']";
			const string subSenseContent = senseContent + "/span[@class='sense']/span[@class='shares senses']/span[@class='sensecontent']";
			const string subSenseNumberOneOne = subSenseContent + "/span[@class='share sense' and preceding-sibling::span[@class='sensenumber' and text()='a']]//span[@lang='en' and text()='subGloss']";
			const string subosoSenseContent = subSenseContent + "/span[@class='share sense']/span[@class='shares senses']/span[@class='sensecontent']";
			const string subosoSenseNumberOneOneOne = subosoSenseContent + "/span[@class='share sense' and preceding-sibling::span[@class='sensenumber' and text()='a']]//span[@lang='en' and text()='subGloss2.1']";
			const string subosoSenseNumberOneOneTwo = subosoSenseContent + "/span[@class='share sense' and preceding-sibling::span[@class='sensenumber' and text()='b']]//span[@lang='en' and text()='subGloss2.2']";
			//This assert is dependent on the specific entry data created in CreateInterestingLexEntry
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(subSenseNumberOneOne, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(subosoSenseNumberOneOneOne, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(subosoSenseNumberOneOneTwo, 1);
		}

		/// <summary>Sense numbers for Main Entry->Senses->Subentries->Senses should not contain the Component Sense's number</summary>
		[Test]
		public void GenerateXHTMLForEntry_SubentriesSensesDontGetMainEntrySensesNumbers()
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
			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, DefaultDecorator, settings);
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
		public void GenerateXHTMLForEntry_SingleSenseGetsNumberWithNumberEvenOneSenseOption()
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
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, DefaultDecorator, DefaultSettings);
			const string senseNumberOne = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense' and preceding-sibling::span[@class='sensenumber' and text()='1']]//span[@lang='en' and text()='gloss']";
			// This assert is dependent on the specific entry data created in CreateInterestingLexEntry
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(senseNumberOne, 1);

			// Piggy-back a test for ShouldThisSenseBeNumbered
			Assert.That(ConfiguredXHTMLGenerator.ShouldThisSenseBeNumbered(testEntry.AllSenses.First(), sensesNode, testEntry.SensesOS), Is.True);
		}

		[Test]
		public void GenerateXHTMLForEntry_SenseContentWithGuid()
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
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, DefaultDecorator, DefaultSettings);
			const string senseEntryGuid = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense' and @entryguid]";
			// This assert is dependent on the specific entry data created in CreateInterestingLexEntry
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(senseEntryGuid, 1);
			string senseEntryGuidstatsWithG = "/div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense' and @entryguid='g" + testEntry.Guid + "']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(senseEntryGuidstatsWithG, 1);
		}

		[Test]
		public void GenerateXHTMLForEntry_ExampleAndTranslationAreGenerated()
		{
			var translationNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Translation",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" })
			};
			var translationsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "TranslationsOC", CSSClassNameOverride = "translationcontents",
				Children = new List<ConfigurableDictionaryNode> { translationNode }
			};
			var exampleNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Example",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};
			var examplesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ExamplesOS", CSSClassNameOverride = "examplescontents",
				Children = new List<ConfigurableDictionaryNode> { exampleNode, translationsNode }
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS", CSSClassNameOverride = "senses",
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
			AddExampleToSense(testEntry.SensesOS[0], example, translation);

			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, null, DefaultSettings);
			const string xpathThruExample = xpathThruSense + "/span[@class='examplescontents']/span[@class='examplescontent']";
			var oneSenseWithExample = string.Format(xpathThruExample + "/span[@class='example']/span[@lang='fr' and text()='{0}']", example);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(oneSenseWithExample, 1);
			var oneExampleSentenceTranslation = string.Format(xpathThruExample +
				"/span[@class='translationcontents']/span[@class='translationcontent']/span[@class='translation']/span[@lang='en' and text()='{0}']", translation);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(oneExampleSentenceTranslation, 1);
		}

		[Test]
		public void GenerateXHTMLForEntry_ExampleSentenceAndTranslationAreGenerated()
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
			AddExampleToSense(minorEntry.SensesOS[0], example, translation);

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(mainEntry, mainEntryNode, null, settings);
			const string xpathThruExampleSentence = "/div[@class='lexentry']/span[@class='complexformsnotsubentries']/span[@class='complexformsnotsubentry']/span[@class='examplesentences']/span[@class='examplesentence']";
			var oneSenseWithExample = string.Format(xpathThruExampleSentence + "//span[@lang='fr' and text()='{0}']", example);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(oneSenseWithExample, 1);
			var oneExampleSentenceTranslation = string.Format(
				xpathThruExampleSentence + "/span[@class='translations']/span[@class='translation']//span[@lang='en' and text()='{0}']", translation);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(oneExampleSentenceTranslation, 1);
		}

		[Test]
		public void GenerateXHTMLForEntry_LineSeperatorUnicodeCharBecomesBrElement()
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
			AddExampleToSense(minorEntry.SensesOS[0], example, translation);

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(mainEntry, mainEntryNode, null, settings);
			const string xpathThruExampleSentence = "/div[@class='lexentry']/span[@class='complexformsnotsubentries']/span[@class='complexformsnotsubentry']/span[@class='examplesentences']/span[@class='examplesentence']";
			var oneSenseWithExample = string.Format(xpathThruExampleSentence + "//span[@lang='fr']//br");
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(oneSenseWithExample, 1);
			var oneExampleSentenceTranslation = string.Format(
				xpathThruExampleSentence + "/span[@class='translations']/span[@class='translation']//span[@lang='en']//br");
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(oneExampleSentenceTranslation, 1);
		}

		[Test]
		public void GenerateXHTMLForEntry_ExtendedNoteChildrenAreGenerated()
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

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, null, settings);

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
		public void GenerateXHTMLForEntry_ExtendedNoteNoteTypeEmptyAreGenerated()
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

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, null, settings);

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
			extendedNote.Discussion.set_String(_wsFr, discussion);

			var exampleFact = Cache.ServiceLocator.GetInstance<ILexExampleSentenceFactory>();
			var example = exampleFact.Create();
			extendedNote.ExamplesOS.Add(example);
			example.Example.set_String(_wsFr, TsStringUtils.MakeString(examples, _wsFr));
			if (translation != null)
			{
				var type = Cache.ServiceLocator.GetInstance<ICmPossibilityRepository>().GetObject(CmPossibilityTags.kguidTranFreeTranslation);
				var cmTranslation = Cache.ServiceLocator.GetInstance<ICmTranslationFactory>().Create(example, type);
				cmTranslation.Translation.set_String(_wsEn, TsStringUtils.MakeString(translation, _wsEn));
				example.TranslationsOC.Add(cmTranslation);
			}
		}

		private ICmPossibility CreateExtendedNoteType(string name)
		{
			if (Cache.LangProject.LexDbOA.ExtendedNoteTypesOA == null)
				Cache.LangProject.LexDbOA.ExtendedNoteTypesOA = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
			var item = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
			Cache.LangProject.LexDbOA.ExtendedNoteTypesOA.PossibilitiesOS.Add(item);
			item.Name.set_String(_wsEn, name);
			Cache.LangProject.LexDbOA.ExtendedNoteTypesOA.PossibilitiesOS.Add(item);
			return item;
		}

		[Test]
		public void GenerateXHTMLForEntry_EnvironmentsAndAllomorphsAreGenerated()
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

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(mainEntry, mainEntryNode, null, settings);
			const string xPathThruAllomorph = "/div[@class='lexentry']/span[@class='alternateformsos']/span[@class='alternateformso']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
					xPathThruAllomorph + "/span[@class='form']/span[@lang='fr' and text()='Allomorph']", 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(xPathThruAllomorph +
					"/span[@class='allomorphenvironments']/span[@class='allomorphenvironment']/span[@class='stringrepresentation']/span[@lang='en' and text()='phoneyEnv']", 1);
		}

		[Test]
		public void GenerateXHTMLForEntry_ReferencedComplexFormsIncludesSubentriesAndOtherReferencedComplexForms()
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

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(mainEntry, mainEntryNode, null, settings);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
				"/div[@class='lexentry']/span[@class='visiblecomplexformbackrefs']/span[@class='visiblecomplexformbackref']//span[@lang='fr']/span[@lang='fr']", 4);
		}

		[Test]
		public void GenerateXHTMLForEntry_GeneratesLinksForReferencedForms()
		{
			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "HeadWord", CSSClassNameOverride = "headword",
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

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(mainEntry, mainEntryNode, null, settings);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
				"//span[@class='visiblevariantentryrefs']/span[@class='visiblevariantentryref']/span[@class='referencedentries']/span[@class='referencedentry']/span[@class='headword']/span[@lang='fr']/span[@lang='fr']/a[@href]", 2);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
				"/div[@class='lexentry']/span[@class='visiblecomplexformbackrefs']/span[@class='visiblecomplexformbackref']/span[@class='headword']/span[@lang='fr']/span[@lang='fr']/a[@href]", 2);
		}

		[Test]
		public void GenerateXHTMLForEntry_GeneratesLinksForPrimaryEntryReferences()
		{
			var mainEntry = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(mainEntry, "Test", _wsFr);
			var referencedEntry = CreateInterestingLexEntry(Cache);
			var otherMainEntry = CreateInterestingLexEntry(Cache);
			const string refTypeName = "TestRefType";
			CreateComplexForm(Cache, mainEntry, referencedEntry, true);
			CreateLexicalReference(otherMainEntry, referencedEntry, refTypeName);

			var refType = Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.First(poss => poss.Name.BestAnalysisAlternative.Text == refTypeName);
			Assert.IsNotNull(refType);

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

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(otherMainEntry, mainEntryNode, null, settings);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
				"//span[@class='minimallexreferences']/span[@class='minimallexreference']/span[@class='configtargets']/span[@class='configtarget']/span[@class='primaryentryrefs']/span[@class='primaryentryref']/span[@class='referencedentries']/span[@class='referencedentry']/span[@class='headword']/span[@lang='fr']/a[@href]", 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
				"//span[@class='minimallexreferences']/span[@class='minimallexreference']/span[@class='configtargets']/span[@class='configtarget']/span[@class='primaryentryrefs']/span[@class='primaryentryref']/span[@class='referencedentries']/span[@class='referencedentry']/span[@class='headword']/span[@lang='fr']/a[@href][contains(text(), 'Test')]", 1);
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

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(mainEntry, mainEntryNode, null, settings);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
				"//span[@class='minimallexreferences']/span[@class='minimallexreference']/span[@class='configtargets']/span[@class='configtarget']/span[@class='headword']/span[@lang='fr']/span[@lang='fr']/a[@href]", 4);
		}

		[Test]
		public void GenerateXHTMLForEntry_GeneratesLinksForCrossReferencesWithReferencedNodes()
		{
			var mainEntry = CreateInterestingLexEntry(Cache);
			var referencedEntry = CreateInterestingLexEntry(Cache);
			const string refTypeName = "TestRefType";
			CreateLexicalReference(mainEntry, referencedEntry, refTypeName);
			var refType = Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.First(poss => poss.Name.BestAnalysisAlternative.Text == refTypeName);
			Assert.IsNotNull(refType);

			var formNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "HeadWord", CSSClassNameOverride = "headword", DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" })
			};
			var targetsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ConfigTargets", Children = new List<ConfigurableDictionaryNode> { formNode }
			};
			var refdCrossRefsNode = new ConfigurableDictionaryNode
			{
				Label = "CrossRefRef", CSSClassNameOverride = "refdrefs",
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
					Options = DictionaryDetailsControllerTests.ListOfEnabledDNOsFromStrings(new [] { refType.Guid.ToString() })
				}
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry", Children = new List<ConfigurableDictionaryNode> { crossReferencesNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(DictionaryConfigurationModelTests.CreateSimpleSharingModel(mainEntryNode, refdCrossRefsNode));

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(mainEntry, mainEntryNode, null, settings);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
				"//span[@class='minimallexreferences refdrefs']/span[@class='minimallexreference refdref']/span[@class='configtargets']/span[@class='configtarget']/span[@class='headword']/span[@lang='fr']//a[@href]", 4);
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

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT-
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(mainEntry, mainEntryNode, null, settings);
			AssertThatXmlIn.String(result).HasNoMatchForXpath(
					"//span[@class='minimallexreferences']/span[@class='minimallexreference']/span[@class='configtargets']");
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

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(referencedEntry, mainEntryNode, null, settings);
			var fwdNameXpath = string.Format(
				"//span[@class='minimallexreferences']/span[@class='minimallexreference']/span[@class='ownertype_name']/span[@lang='en' and text()='{0}']", refTypeName);
			const string anyNameXpath =
				"//span[@class='minimallexreferences']/span[@class='minimallexreference']/span[@class='ownertype_name']/span[@lang='en']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(fwdNameXpath, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(anyNameXpath, 1); // ensure there are no spurious names
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

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(mainEntry, mainEntryNode, null, settings);
			var fwdNameXpath = string.Format(
				"//span[@class='minimallexreferences']/span[@class='minimallexreference']/span[@class='ownertype_name']/span[@lang='en' and text()='{0}']", refTypeName);
			var revNameXpath = string.Format(
				"//span[@class='minimallexreferences']/span[@class='minimallexreference']/span[@class='ownertype_name']/span[@lang='en' and text()='{0}']", refTypeRevName);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(fwdNameXpath, 1);
			AssertThatXmlIn.String(result).HasNoMatchForXpath(revNameXpath);
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

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(referencedEntry, mainEntryNode, null, settings);
			var fwdNameXpath = string.Format(
				"//span[@class='minimallexreferences']/span[@class='minimallexreference']/span[@class='ownertype_name']/span[@lang='en' and text()='{0}']", refTypeName);
			var revNameXpath = string.Format(
				"//span[@class='minimallexreferences']/span[@class='minimallexreference']/span[@class='ownertype_name']/span[@lang='en' and text()='{0}']", refTypeRevName);
			AssertThatXmlIn.String(result).HasNoMatchForXpath(fwdNameXpath);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(revNameXpath, 1);
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

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(mainEntry, mainEntryNode, null, settings);
			var fwdNameXpath = string.Format(
				"//span[@class='lexsensereferences']/span[@class='lexsensereference']/span[@class='ownertype_name']/span[@lang='en' and text()='{0}']", refTypeName);
			var revNameXpath = string.Format(
				"//span[@class='lexsensereferences']/span[@class='lexsensereference']/span[@class='ownertype_name']/span[@lang='en' and text()='{0}']", refTypeRevName);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(fwdNameXpath, 1);
			AssertThatXmlIn.String(result).HasNoMatchForXpath(revNameXpath);
		}

		[Test]
		public void GenerateXHTMLForEntry_GeneratesLexicalRelationsLabelWithNoRepetition()
		{
			var mainEntry = CreateInterestingLexEntry(Cache);
			var referencedEntry1 = CreateInterestingLexEntry(Cache);
			var referencedEntry2 = CreateInterestingLexEntry(Cache);
			const string refTypeName = "TestRefType";
			const string refTypeRevName = "sURsyoT";
			CreateLexicalReference(mainEntry.SensesOS.First(), referencedEntry1, refTypeName, refTypeRevName);
			CreateLexicalReference(mainEntry.SensesOS.First(), referencedEntry2, refTypeName, refTypeRevName);
			var refType1 = Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.First(poss => poss.Name.BestAnalysisAlternative.Text == refTypeName);
			Assert.IsNotNull(refType1);
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
					Options = DictionaryDetailsControllerTests.ListOfEnabledDNOsFromStrings(new[] { refType1.Guid + ":f"})
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

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(mainEntry, mainEntryNode, null, settings);
			var fwdNameXpath = string.Format(
				"//span[@class='lexsensereferences']/span[@class='lexsensereference']/span[@class='ownertype_name']/span[@lang='en' and text()='{0}']", refTypeName);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(fwdNameXpath, 1);
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

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(referencedEntry, mainEntryNode, null, settings);
			var fwdNameXpath = string.Format(
				"//span[@class='lexsensereferences']/span[@class='lexsensereference']/span[@class='ownertype_name']/span[@lang='en' and text()='{0}']", refTypeName);
			var revNameXpath = string.Format(
				"//span[@class='lexsensereferences']/span[@class='lexsensereference']/span[@class='ownertype_name']/span[@lang='en' and text()='{0}']", refTypeRevName);
			AssertThatXmlIn.String(result).HasNoMatchForXpath(fwdNameXpath);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(revNameXpath, 1);
		}

		[Test]
		public void GenerateXHTMLForEntry_LexicalRelationsSortbyNodeOptionsOrder()
		{
			var mainEntry = CreateInterestingLexEntry(Cache);
			var compareReferencedEntry = CreateInterestingLexEntry(Cache);
			var etymologyReferencedEntry = CreateInterestingLexEntry(Cache);
			const string comRefTypeName = "Compare";
			const string comRefTypeRevName = "cp";
			const string etyRefTypeName = "Etymology";
			const string etyRefTypeRevName = "ety";
			CreateLexicalReference(mainEntry, compareReferencedEntry, comRefTypeName, comRefTypeRevName);
			CreateLexicalReference(mainEntry, etymologyReferencedEntry, etyRefTypeName, etyRefTypeRevName);
			var comRefType =
				Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.First(
					poss => poss.Name.BestAnalysisAlternative.Text == comRefTypeName);
			var etyRefType =
				Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.First(
					poss => poss.Name.BestAnalysisAlternative.Text == etyRefTypeName);
			Assert.IsNotNull(comRefType);
			Assert.IsNotNull(etyRefType);

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
			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(mainEntry, mainEntryNode, null, settings);
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
			var resultAfterChange = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(mainEntry, mainEntryNode, null, settings);
			var fwdNameChangedFirstXpath = string.Format(NameXpath, "1", comRefTypeName);
			var fwdNameChangedSecondXpath = string.Format(NameXpath, "2", etyRefTypeName);
			AssertThatXmlIn.String(resultAfterChange).HasSpecifiedNumberOfMatchesForXpath(fwdNameChangedFirstXpath, 1);
			AssertThatXmlIn.String(resultAfterChange).HasSpecifiedNumberOfMatchesForXpath(fwdNameChangedSecondXpath, 1);
		}

		[Test]
		public void GenerateXHTMLForEntry_GeneratesAsymmetricRelationsProperly()
		{
			const string firstWord = "corps";
			var bodyEntry = CreateInterestingLexEntry(Cache, firstWord, "body");
			const string secondWord = "bras";
			var armEntry = CreateInterestingLexEntry(Cache, secondWord, "arm");
			const string thirdWord = "jambe";
			var legEntry = CreateInterestingLexEntry(Cache, thirdWord, "leg");
			const string refTypeName = "Part";
			const string refTypeRevName = "Whole";
			CreateLexicalReference(bodyEntry, armEntry.SensesOS.First(), legEntry.SensesOS.First(), refTypeName, refTypeRevName);
			var refType = Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.First(poss => poss.Name.BestAnalysisAlternative.Text == refTypeName);
			Assert.IsNotNull(refType);

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

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			var output = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(armEntry, mainEntryNode, null, settings);
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
		public void GenerateXHTMLForEntry_GeneratesConfigTargetsForSubSenseProperly()
		{
			const string firstHeadword = "homme";
			var firstEntry = CreateInterestingLexEntry(Cache, firstHeadword);
			AddSingleSubSenseToSense("man", firstEntry.SensesOS[0]);
			var legEntry = CreateInterestingLexEntry(Cache, "jambe", "leg");
			const string refTypeName = "Part";
			const string refTypeRevName = "Whole";
			CreateLexicalReference(firstEntry, firstEntry.SensesOS[0].SensesOS[0], legEntry.SensesOS.First(), refTypeName, refTypeRevName);
			var refType = Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.First(poss => poss.Name.BestAnalysisAlternative.Text == refTypeName);
			Assert.IsNotNull(refType);

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
				Children = new List<ConfigurableDictionaryNode> { referencesNode}
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

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			var output = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(firstEntry, mainEntryNode, null, settings);
			var goodTarget = string.Format(
				"//span[@class='lexsensereferences']/span[@class='lexsensereference']/span[@class='configtargets']/span[@class='configtarget']/span[@class='headword']/span[@lang='fr' and text()='{0}']", firstHeadword);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(goodTarget, 1);
			var badTarget = "//span[@class='lexsensereferences']/span[@class='lexsensereference']/span[@class='configtargets']/span[@class='configtarget']/span[@class='gloss']";
			AssertThatXmlIn.String(output).HasNoMatchForXpath(badTarget);
		}

		[Test]
		public void GenerateXHTMLForEntry_GeneratesConfigTargetsForTreeBetweenSenses()
		{
			const string headword = "headword";
			var firstEntry = CreateInterestingLexEntry(Cache, headword, "b1");
			AddSenseToEntry(firstEntry, "b2", _wsEn, Cache);
			const string refTypeName = "Part";
			const string refTypeRevName = "Whole";
			CreateLexicalReference(firstEntry.SensesOS[0], firstEntry.SensesOS[1], refTypeName, refTypeRevName);
			var refType = Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.First(poss => poss.Name.BestAnalysisAlternative.Text == refTypeName);
			Assert.IsNotNull(refType);

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

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(firstEntry, mainEntryNode, DefaultDecorator, settings);

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
			Assert.IsTrue(ConfiguredXHTMLGenerator.IsListItemSelectedForExport(minorEntryNode, minorEntry));
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
			CreateVariantForm (Cache, mainEntry, variantForm);
			var notCrazyVariant = Cache.LangProject.LexDbOA.VariantEntryTypesOA.PossibilitiesOS.FirstOrDefault(variant => variant.Name.BestAnalysisAlternative.Text != TestVariantName);
			Assert.IsNotNull(notCrazyVariant);
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
			Assert.IsFalse(ConfiguredXHTMLGenerator.IsListItemSelectedForExport(rcfsNode, variantForm.VisibleVariantEntryRefs.First(), variantForm));
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
			Assert.IsTrue(ConfiguredXHTMLGenerator.IsListItemSelectedForExport(variantsNode, mainEntry.VisibleComplexFormBackRefs.First(), mainEntry));
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
			Assert.IsTrue(ConfiguredXHTMLGenerator.IsListItemSelectedForExport(variantsNode, mainEntry.Subentries.First(), mainEntry));
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
			Assert.IsNotNull(notComplexTypePoss);
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
			Assert.IsFalse(ConfiguredXHTMLGenerator.IsListItemSelectedForExport(rcfsNode, mainEntry.VisibleComplexFormBackRefs.First(), mainEntry));
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
			CreateLexicalReference(mainEntry, referencedEntry, refTypeName);
			var notComplexTypePoss = Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.First(refType => refType.Name.BestAnalysisAlternative.Text == refTypeName);
			Assert.IsNotNull(notComplexTypePoss);
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
			if (Cache.LangProject.LexDbOA.ReferencesOA == null)
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
			Assert.IsFalse(ConfiguredXHTMLGenerator.IsListItemSelectedForExport(entryReferenceNode, mainEntry.MinimalLexReferences.First(), mainEntry));
			Assert.IsFalse(ConfiguredXHTMLGenerator.IsListItemSelectedForExport(entryReferenceNode, referencedEntry.MinimalLexReferences.First(), referencedEntry));
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
			CreateLexicalReference(mainEntry, referencedEntry, refTypeName);
			var notComplexTypePoss = Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.First(refType => refType.Name.BestAnalysisAlternative.Text == refTypeName);
			Assert.IsNotNull(notComplexTypePoss);
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
			CreateVariantForm (Cache, mainEntry, variantForm);
			var notCrazyVariant = Cache.LangProject.LexDbOA.VariantEntryTypesOA.PossibilitiesOS.FirstOrDefault(variant => variant.Name.BestAnalysisAlternative.Text != TestVariantName);
			Assert.IsNotNull(notCrazyVariant);
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

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(mainEntry, mainEntryNode, null, settings);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
					"/div[@class='lexentry']/span[@class='variantformentrybackrefs']/span[@class='variantformentrybackref']/span[@lang='fr']", 0);
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
			CreateVariantForm (Cache, mainEntry, variantForm);
			var crazyVariant = Cache.LangProject.LexDbOA.VariantEntryTypesOA.PossibilitiesOS.FirstOrDefault(variant => variant.Name.BestAnalysisAlternative.Text == TestVariantName);
			Assert.IsNotNull(crazyVariant);

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
				DictionaryNodeOptions = GetListOptionsForItems(DictionaryNodeListOptions.ListIds.Variant, new [] { crazyVariant }),
				Children = new List<ConfigurableDictionaryNode> { variantFormTypeNode, formNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { variantsNode }
			};
			DictionaryConfigurationModel.SpecifyParentsAndReferences(new List<ConfigurableDictionaryNode> { mainEntryNode });

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(mainEntry, mainEntryNode, null, settings);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
				"/div[@class='lexentry']/span[@class='variantformentrybackrefs']/span[@class='variantformentrybackref']//span[@lang='fr']/span[@lang='fr']", 2);
		}

		[Test]
		public void GenerateXHTMLForEntry_VariantTypeIsUncheckedAndHeadwordIsChecked()
		{
			var mainEntry = CreateInterestingLexEntry(Cache);
			var variantForm = CreateInterestingLexEntry(Cache);
			CreateVariantForm(Cache, mainEntry, variantForm);
			var crazyVariant = Cache.LangProject.LexDbOA.VariantEntryTypesOA.PossibilitiesOS.FirstOrDefault(variant => variant.Name.BestAnalysisAlternative.Text == TestVariantName);
			Assert.IsNotNull(crazyVariant);

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

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(mainEntry, mainEntryNode, null, settings);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
				"/div[@class='lexentry']/span[@class='variantformentrybackrefs']/span[@class='variantformentrybackref']/span[@class='referencedentries']" +
				"/span[@class='referencedentry']/span[@class='headword']/span[@lang='fr']/span[@lang='fr' and text()='Citation']", 1);
		}

		[Test]
		public void GenerateXHTMLForEntry_ReferencedComplexFormsUnderSensesIncludesSubentriesAndOtherReferencedComplexForms()
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
				FieldDescription = "SensesOS", CSSClassNameOverride = "senses",
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

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(mainEntry, mainEntryNode, null, settings);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
				xpathThruSense + "/span[@class='visiblecomplexformbackrefs']/span[@class='visiblecomplexformbackref']//span[@lang='fr']/span[@lang='fr']", 4);
		}

		[Test]
		public void GenerateLetterHeaderIfNeeded_GeneratesHeaderIfNoPreviousHeader()
		{
			var entry = CreateInterestingLexEntry(Cache);
			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				// SUT
				string last = null;
				XHTMLWriter.WriteStartElement("TestElement");
				ConfiguredXHTMLGenerator.GenerateLetterHeaderIfNeeded(entry, ref last, XHTMLWriter, DefaultSettings);
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
			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				// SUT
				var last = "A a";
				XHTMLWriter.WriteStartElement("TestElement");
				ConfiguredXHTMLGenerator.GenerateLetterHeaderIfNeeded(entry, ref last, XHTMLWriter, DefaultSettings);
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
			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				// SUT
				var last = "A a";
				XHTMLWriter.WriteStartElement("TestElement");
				ConfiguredXHTMLGenerator.GenerateLetterHeaderIfNeeded(entry, ref last, XHTMLWriter, DefaultSettings);
				XHTMLWriter.WriteEndElement();
				XHTMLWriter.Flush();
				const string letterHeaderToMatch = "//div[@class='letHead']/span[@class='letter' and @lang='fr' and text()='B b']";
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(letterHeaderToMatch, 1);
			}
		}

		[Test]
		public void GenerateLetterHeaderIfNeeded_GeneratesNoHeaderIfPreviousHeaderDoesMatch()
		{
			var entry = CreateInterestingLexEntry(Cache);
			using (var XHTMLWriter = XmlWriter.Create(XHTMLStringBuilder))
			{
				// SUT
				var last = "A a";
				XHTMLWriter.WriteStartElement("TestElement");
				ConfiguredXHTMLGenerator.GenerateLetterHeaderIfNeeded(entry, ref last, XHTMLWriter, DefaultSettings);
				ConfiguredXHTMLGenerator.GenerateLetterHeaderIfNeeded(entry, ref last, XHTMLWriter, DefaultSettings);
				XHTMLWriter.WriteEndElement();
				XHTMLWriter.Flush();
				const string letterHeaderToMatch = "//div[@class='letHead']/span[@class='letter' and @lang='fr' and text()='C c']";
				const string proveOnlyOneHeader = "//div[@class='letHead']/span[@class='letter']";
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(letterHeaderToMatch, 1);
				AssertThatXmlIn.String(XHTMLStringBuilder.ToString()).HasSpecifiedNumberOfMatchesForXpath(proveOnlyOneHeader, 1);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_OneSenseWithSinglePicture()
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
			var sensePic = Cache.ServiceLocator.GetInstance<ICmPictureFactory>().Create();
			sense.PicturesOS.Add(sensePic);
			var wsEn = Cache.WritingSystemFactory.GetWsFromStr("en");
			sensePic.Caption.set_String(wsEn, TsStringUtils.MakeString("caption", wsEn));
			var pic = Cache.ServiceLocator.GetInstance<ICmFileFactory>().Create();
			var folder = Cache.ServiceLocator.GetInstance<ICmFolderFactory>().Create();
			Cache.LangProject.MediaOC.Add(folder);
			folder.FilesOC.Add(pic);
			pic.InternalPath = "picture";
			sensePic.PictureFileRA = pic;

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, null, settings);
			const string oneSenseWithPicture = "/div[@class='lexentry']/span[@class='pictures']/div[@class='picture']/img[@class='photo' and @id]";
			const string oneSenseWithPictureCaption = "/div[@class='lexentry']/span[@class='pictures']/div[@class='picture']/div[@class='captionContent']/span[@class='caption']//span[text()='caption']";
			//This assert is dependent on the specific entry data created in CreateInterestingLexEntry
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(oneSenseWithPicture, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(oneSenseWithPictureCaption, 1);
		}

		[Test]
		public void GenerateXHTMLForEntry_PictureWithNonUnicodePathLinksCorrectly()
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
			var decomposedPath = Icu.Normalize(pathWithUtf8Char, Icu.UNormalizationMode.UNORM_NFD);
			var composedPath = Icu.Normalize(pathWithUtf8Char, Icu.UNormalizationMode.UNORM_NFC);
			// Set the internal path to decomposed (which is what FLEx does when it loads data)
			pic.InternalPath = decomposedPath;
			sensePic.PictureFileRA = pic;

			// generates a src attribute with an absolute file path
			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, null, settings);
			var pictureWithComposedPath = "/div[@class='lexentry']/span[@class='pictures']/span[@class='picture']/img[contains(@src, '" + composedPath + "')]";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(pictureWithComposedPath, 1);
		}

		[Test]
		public void GenerateXHTMLForEntry_PictureCopiedAndRelativePathUsed()
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
			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, true, true, tempFolder.FullName);
			try
			{
				//SUT
				var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, null, settings);
				var pictureRelativePath = Path.Combine("pictures", Path.GetFileName(filePath));
				var pictureWithComposedPath = "/div[@class='lexentry']/span[@class='pictures']/span[@class='picture']/img[starts-with(@src, '" + pictureRelativePath + "')]";
				if (!MiscUtils.IsUnix)
					AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(pictureWithComposedPath, 1);
				// that src starts with a string, and escaping any Windows path separators
				AssertRegex(result, string.Format("src=\"{0}[^\"]*\"", pictureRelativePath.Replace(@"\", @"\\")), 1);
				Assert.IsTrue(File.Exists(Path.Combine(tempFolder.Name, "pictures", filePath)));
			}
			finally
			{
				DirectoryUtilities.DeleteDirectoryRobust(tempFolder.FullName);
				File.Delete(filePath);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_MissingPictureFileDoesNotCrashOnCopy()
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
			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, true, true, tempFolder.FullName);
			try
			{
				//SUT
				var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, null, settings);
				var pictureRelativePath = Path.Combine("pictures", Path.GetFileName(filePath));
				var pictureWithComposedPath = "/div[@class='lexentry']/span[@class='pictures']/span[@class='picture']/img[starts-with(@src, '" + pictureRelativePath + "')]";
				if (!MiscUtils.IsUnix)
					AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(pictureWithComposedPath, 1);
				// that src starts with a string, and escaping any Windows path separators
				AssertRegex(result, string.Format("src=\"{0}[^\"]*\"", pictureRelativePath.Replace(@"\", @"\\")), 1);
				Assert.IsFalse(File.Exists(Path.Combine(tempFolder.Name, "pictures", filePath)));
			}
			finally
			{
				DirectoryUtilities.DeleteDirectoryRobust(tempFolder.FullName);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_TwoDifferentFilesGetTwoDifferentResults()
		{
			var mainEntryNode = CreatePictureModel();
			var testEntry = CreateInterestingLexEntry(Cache);
			AddSenseToEntry(testEntry, "second", _wsEn, Cache);
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
				var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, true, true, tempFolder.FullName);
				//SUT
				var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, null, settings);
				var pictureRelativePath = Path.Combine("pictures", Path.GetFileName(fileName));
				var pictureWithComposedPath = "/div[@class='lexentry']/span[@class='pictures']/span[@class='picture']/img[contains(@src, '" + pictureRelativePath + "')]";
				if (!MiscUtils.IsUnix)
					AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(pictureWithComposedPath, 1);
				// that src contains a string, and escaping any Windows path separators
				AssertRegex(result, string.Format("src=\"[^\"]*{0}[^\"]*\"", pictureRelativePath.Replace(@"\", @"\\")), 1);
				// The second file with the same name should have had something appended to the end of the filename but the initial filename should match both entries
				var filenameWithoutExtension = Path.GetFileNameWithoutExtension(pictureRelativePath);
				var pictureStartsWith = "/div[@class='lexentry']/span[@class='pictures']/span[@class='picture']/img[contains(@src, '" + filenameWithoutExtension + "')]";
				if (!MiscUtils.IsUnix)
					AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(pictureStartsWith, 2);
				// that src contains a string
				AssertRegex(result, string.Format("src=\"[^\"]*{0}[^\"]*\"", filenameWithoutExtension), 2);
				Assert.AreEqual(2, Directory.EnumerateFiles(Path.Combine(tempFolder.FullName, "pictures")).Count(), "Wrong number of pictures copied.");
			}
			finally
			{
				DirectoryUtilities.DeleteDirectoryRobust(tempFolder.FullName);
				File.Delete(filePath1);
				File.Delete(filePath2);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_UniqueIdsForSameFile()
		{
			var mainEntryNode = CreatePictureModel();
			var testEntry = CreateInterestingLexEntry(Cache);
			AddSenseToEntry(testEntry, "second", _wsEn, Cache);
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
				var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, true, true, tempFolder.FullName);
				//SUT
				var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, null, settings);
				var pictureRelativePath = Path.Combine("pictures", Path.GetFileName(fileName));
				const string pictureXPath = "/div[@class='lexentry']/span[@class='pictures']/span[@class='picture']/img";
				var pictureWithComposedPath = pictureXPath + "[contains(@src, '" + pictureRelativePath + "')]";
				if (!MiscUtils.IsUnix)
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
				SIL.IO.DirectoryUtilities.DeleteDirectoryRobust(tempFolder.FullName);
				File.Delete(filePath1);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_BadFileNameDoesNotCrash()
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
			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, true, true, tempFolder.FullName);
			//SUT
			Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, null, settings));
		}

		[Test]
		public void GenerateXHTMLForEntry_NullFilePathDoesNotCrash()
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
			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, true, true, tempFolder.FullName);
			//SUT
			Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, null, settings));
		}

		[Test]
		public void GenerateXHTMLForEntry_NullInternalPathDoesNotCrash()
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

			AddSenseToEntry(entry, "second", _wsEn, Cache);
			var sense = entry.SensesOS[0];
			var sensePic = Cache.ServiceLocator.GetInstance<ICmPictureFactory>().Create();
			sense.PicturesOS.Add(sensePic);
			var pic = Cache.ServiceLocator.GetInstance<ICmFileFactory>().Create();
			Cache.LangProject.MediaOC.Add(folder);
			folder.FilesOC.Add(pic);
			sensePic.PictureFileRA = pic;
			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);

			//SUT
			Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entry, mainEntryNode, null, settings));
		}

		[Test]
		public void GenerateXHTMLForEntry_TwoDifferentLinksToTheSamefileWorks()
		{
			var mainEntryNode = CreatePictureModel();
			var testEntry = CreateInterestingLexEntry(Cache);
			AddSenseToEntry(testEntry, "second", _wsEn, Cache);
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
				var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, true, true, tempFolder.FullName);
				//SUT
				var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, null, settings);
				var pictureRelativePath = Path.Combine("pictures", Path.GetFileName(fileName));
				var pictureWithComposedPath = "/div[@class='lexentry']/span[@class='pictures']/span[@class='picture']/img[contains(@src, '" + pictureRelativePath + "')]";
				if (!MiscUtils.IsUnix)
					AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(pictureWithComposedPath, 2);
				// that src starts with string, and escaping Windows directory separators
				AssertRegex(result, string.Format("src=\"{0}[^\"]*\"", pictureRelativePath.Replace(@"\", @"\\")), 2);
				// The second file reference should not have resulted in a copy
				Assert.AreEqual(Directory.EnumerateFiles(Path.Combine(tempFolder.FullName, "pictures")).Count(), 1, "Wrong number of pictures copied.");
			}
			finally
			{
				DirectoryUtilities.DeleteDirectoryRobust(tempFolder.FullName);
				File.Delete(fileName);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_StringCustomFieldGeneratesContent()
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
				var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
				//SUT
				var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, null, settings);
				var customDataPath = string.Format("/div[@class='lexentry']/span[@class='customstring']/span[text()='{0}']", customData);
				AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(customDataPath, 1);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_GetPropertyTypeForConfigurationNode_StringCustomFieldIsPrimitive()
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
				Assert.AreEqual(PropertyType.PrimitiveType, ConfiguredXHTMLGenerator.GetPropertyTypeForConfigurationNode(customFieldNode, (IFwMetaDataCacheManaged)Cache.MetaDataCacheAccessor));
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_StringCustomFieldOnSenseGeneratesContent()
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
				var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
				//SUT
				var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, null, settings);
				var customDataPath = string.Format("/div[@class='l']/span[@class='es']/span[@class='e']/span[@class='customstring']/span[text()='{0}']", customData);
				AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(customDataPath, 1);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_StringCustomFieldOnExampleGeneratesContent()
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
				var exampleSentence = AddExampleToSense(testSense, @"I'm an example");

				// Set custom field data
				Cache.MainCacheAccessor.SetString(exampleSentence.Hvo, customField.Flid, TsStringUtils.MakeString(customData, wsEn));
				var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
				//SUT
				var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, null, settings);
				var customDataPath = string.Format(
					"/div[@class='l']/span[@class='es']//span[@class='xs']/span[@class='x']/span[@class='customstring']/span[text()='{0}']", customData);
				AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(customDataPath, 1);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_StringCustomFieldOnAllomorphGeneratesContent()
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
				Cache.MainCacheAccessor.SetString(allomorph.Hvo, customField.Flid, TsStringUtils.MakeString(customData, _wsEn));
				var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
				//SUT
				var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, null, settings);
				var customDataPath = string.Format(
					"/div[@class='l']/span[@class='as']/span[@class='a']/span[@class='customstring']/span[text()='{0}']", customData);
				AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(customDataPath, 1);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_MultiStringCustomFieldGeneratesContent()
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
				Cache.MainCacheAccessor.SetMultiStringAlt(testEntry.Hvo, customField.Flid, _wsEn, TsStringUtils.MakeString(customData, _wsEn));
				var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
				//SUT
				var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, null, settings);
				var customDataPath = string.Format("/div[@class='lexentry']/span[@class='customstring']/span[text()='{0}']", customData);
				AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(customDataPath, 1);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_CustomFieldOnISenseOrEntryGeneratesContentForEntry()
		{
			var entryCustom = new ConfigurableDictionaryNode
			{
				FieldDescription = "EntryCString", IsCustomField = true, DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" })
			};
			var senseCustom = new ConfigurableDictionaryNode
			{
				FieldDescription = "SenseCString", IsCustomField = true, DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" })
			};
			var targets = new ConfigurableDictionaryNode
			{
				FieldDescription = "ConfigTargets", Children = new List<ConfigurableDictionaryNode> { entryCustom, senseCustom }
			};
			var crossRefs = new ConfigurableDictionaryNode
			{
				FieldDescription = "MinimalLexReferences", CSSClassNameOverride = "mlrs",
				Children = new List<ConfigurableDictionaryNode> { targets }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry", Children = new List<ConfigurableDictionaryNode> { crossRefs }
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
				CreateLexicalReference(testEntry, refdEntry, refType);
				var lexrefType = Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.First(
						r => r.Name.BestAnalysisAlternative.Text == refType);
				crossRefs.DictionaryNodeOptions = new DictionaryNodeListOptions
				{
					ListId = DictionaryNodeListOptions.ListIds.Entry,
					Options = DictionaryDetailsControllerTests.ListOfEnabledDNOsFromStrings(new[] { lexrefType.Guid.ToString()})
				};
				const string entryCustomData = "Another custom string";
				const string senseCustomData = "My custom string";
				Cache.MainCacheAccessor.SetMultiStringAlt(refdEntry.Hvo, entryCustomField.Flid, _wsEn,
					TsStringUtils.MakeString(entryCustomData, _wsEn));
				Cache.MainCacheAccessor.SetMultiStringAlt(refdEntry.SensesOS[0].Hvo, senseCustomField.Flid, _wsEn,
					TsStringUtils.MakeString(senseCustomData, _wsEn));
				var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
				//SUT
				var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, null, settings);
				var entryDataPath = string.Format("/div[@class='lexentry']/span[@class='mlrs']/span[@class='mlr']/span[@class='configtargets']/span[@class='configtarget']/span[@class='entrycstring']/span[text()='{0}']", entryCustomData);
				AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(entryDataPath, 1);
				var senseDataPath = string.Format("/div[@class='lexentry']/span[@class='mlrs']/span[@class='mlr']/span[@class='configtargets']/span[@class='configtarget']/span[@class='sensecstring']/span[text()='{0}']", senseCustomData);
				AssertThatXmlIn.String(result).HasNoMatchForXpath(senseDataPath, message: "Ref is to Entry; should be no Sense Custom Data");
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_CustomFieldOnISenseOrEntryGeneratesContentForSense()
		{
			var entryCustom = new ConfigurableDictionaryNode
			{
				FieldDescription = "EntryCString", IsCustomField = true, DictionaryNodeOptions = GetWsOptionsForLanguages(new [] { "en" })
			};
			var senseCustom = new ConfigurableDictionaryNode
			{
				FieldDescription = "SenseCString", IsCustomField = true, DictionaryNodeOptions = GetWsOptionsForLanguages(new [] { "en" })
			};
			var targets = new ConfigurableDictionaryNode
			{
				FieldDescription = "ConfigTargets", Children = new List<ConfigurableDictionaryNode> { entryCustom, senseCustom }
			};
			var crossRefs = new ConfigurableDictionaryNode
			{
				FieldDescription = "MinimalLexReferences", CSSClassNameOverride = "mlrs", Children = new List<ConfigurableDictionaryNode> { targets }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry", Children = new List<ConfigurableDictionaryNode> { crossRefs }
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
				CreateLexicalReference(testEntry, refdEntry.SensesOS[0], refType);
				var lexrefType = Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.First(
						r => r.Name.BestAnalysisAlternative.Text == refType);
				crossRefs.DictionaryNodeOptions = new DictionaryNodeListOptions
				{
					ListId = DictionaryNodeListOptions.ListIds.Entry,
					Options = DictionaryDetailsControllerTests.ListOfEnabledDNOsFromStrings(new[] { lexrefType.Guid.ToString() })
				};
				const string entryCustomData = "Another custom string";
				const string senseCustomData = "My custom string";
				Cache.MainCacheAccessor.SetMultiStringAlt(refdEntry.Hvo, entryCustomField.Flid, _wsEn,
					TsStringUtils.MakeString(entryCustomData, _wsEn));
				Cache.MainCacheAccessor.SetMultiStringAlt(refdEntry.SensesOS[0].Hvo, senseCustomField.Flid, _wsEn,
					TsStringUtils.MakeString(senseCustomData, _wsEn));
				var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
				//SUT
				var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, null, settings);
				var entryDataPath = string.Format("/div[@class='lexentry']/span[@class='mlrs']/span[@class='mlr']/span[@class='configtargets']/span[@class='configtarget']/span[@class='entrycstring']/span[text()='{0}']", entryCustomData);
				AssertThatXmlIn.String(result).HasNoMatchForXpath(entryDataPath, message: "Ref is to Sense; should be no Entry Custom Data");
				var senseDataPath = string.Format("/div[@class='lexentry']/span[@class='mlrs']/span[@class='mlr']/span[@class='configtargets']/span[@class='configtarget']/span[@class='sensecstring']/span[text()='{0}']", senseCustomData);
				AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(senseDataPath, 1);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_CustomFieldOnRefdLexEntryGeneratesContent()
		{
			var customConfig = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwningEntry", SubField = "CustomString", IsCustomField = true,
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" })
			};
			var crossRefs = new ConfigurableDictionaryNode
			{
				FieldDescription = "VariantFormEntryBackRefs", CSSClassNameOverride = "vars",
				Children = new List<ConfigurableDictionaryNode> { customConfig }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry", Children = new List<ConfigurableDictionaryNode> { crossRefs }
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			using (var customField = new CustomFieldForTest(Cache, "CustomString", Cache.MetaDataCacheAccessor.GetClassId("LexEntry"), 0,
				CellarPropertyType.MultiString, Guid.Empty))
			{
				var testEntry = CreateInterestingLexEntry(Cache);
				var refdEntry = CreateInterestingLexEntry(Cache);
				CreateVariantForm(Cache, testEntry, refdEntry);
				const string customData = "My custom string";
				Cache.MainCacheAccessor.SetMultiStringAlt(refdEntry.Hvo, customField.Flid, _wsEn, TsStringUtils.MakeString(customData, _wsEn));
				var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
				//SUT
				var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, null, settings);
				var customDataPath = string.Format("/div[@class='lexentry']/span[@class='vars']/span[@class='var']/span[@class='owningentry_customstring']/span[text()='{0}']", customData);
				AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(customDataPath, 1);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_MultiStringDefinition_GeneratesMultilingualSpans()
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
			testEntry.SensesOS[0].Definition.set_String(_wsEn, defn);

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, null, settings);
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
		public void GenerateXHTMLForEntry_ListItemCustomFieldGeneratesContent()
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
				var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
				//SUT
				var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, null, settings);
				const string customDataPath = "/div[@class='lexentry']/span[@class='customlistitem']/span[@class='name']/span[text()='Djbuti']";
				AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(customDataPath, 1);
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
				var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
				//SUT
				var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, null, settings);
				const string customDataPath1 = "/div[@class='lexentry']/span[@class='customlistitems']/span[@class='customlistitem']/span[@class='name']/span[text()='Dallas']";
				const string customDataPath2 = "/div[@class='lexentry']/span[@class='customlistitems']/span[@class='customlistitem']/span[@class='name']/span[text()='Barcelona']";
				AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(customDataPath1, 1);
				AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(customDataPath2, 1);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_DateCustomFieldGeneratesContent()
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
				var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
				//SUT
				var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, null, settings);
				var customDataPath = string.Format("/div[@class='lexentry']/span[@class='customdate' and text()='{0}']", customData.ToLongDateString());
				AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(customDataPath, 1);
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
				var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
				//SUT
				var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, null, settings);
				var customDataPath = string.Format("/div[@class='lexentry']/span[@class='custominteger' and text()='{0}']", customData);
				AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(customDataPath, 1);
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
				var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
				//SUT
				var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, rootNode, null, settings);
				const string customDataPath =
					"/div[@class='lexentry']/div/span[text()='First para Custom string'] | /div[@class='lexentry']/div/span[text()='Second para Custom string']";
				AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(customDataPath, 2);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_VariantOfReferencedHeadWord()
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
			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(mainEntry, mainEntryNode, null, settings);
			const string referencedEntries =
				"//span[@class='visiblevariantentryrefs']/span[@class='visiblevariantentryref']/span[@class='referencedentries']/span[@class='referencedentry']/span[@class='headword']/span[@lang='fr']/span[@lang='fr']";
			AssertThatXmlIn.String(result)
					.HasSpecifiedNumberOfMatchesForXpath(referencedEntries, 2);
		}

		[Test]
		public void GenerateXHTMLForEntry_WsAudiowithHyperlink()
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
			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entryOne, mainEntryNode, null, settings);

			const string audioTagwithSource = "//audio/source/@src";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(audioTagwithSource, 1);
			var audioFileUrl = @"Src/LanguageExplorerTests/Works/TestData/LinkedFiles/AudioVisual/" + audioFileName;
			Assert.That(result, Contains.Substring(audioFileUrl));
			const string linkTagwithOnClick = "//span[@class='lexemeformoa']/span/a[@class='en-Zxxx-x-audio' and contains(@onclick,'play()')]";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(linkTagwithOnClick, 1);
		}

		/// <summary>
		/// Tests that during a web export the .wav file is automatically converted into an .mp3 file
		/// and saved in the destination file if the file does not already exist.
		/// </summary>
		/// <param name="isWebExport"> bool indicating if a web export is in progress </param>
		[Test]
		[TestCase(true)] //Is WebExport so the copied .wav file should be converted to an .mp3 file
		[TestCase(false)] //Is not a WebExport so the copied .wav file should remain a .wav file
		public void GenerateXHTMLForEntry_AudioConversionDestinationDoesNotExist(bool isWebExport)
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
				DictionaryNodeOptions = GetFullyEnabledListOptions(DictionaryNodeListOptions.ListIds.Variant),
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
			CreateVariantForm(Cache, entry, variant, "Spelling Variant"); // we need a real Variant Type to pass the list options test
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
				var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, true, true, expectedMediaFolderPath, false, isWebExport);
				settings.Cache.LangProject.LinkedFilesRootDir = expectedMediaFolderPath;

				// create a temp directory and copy a .wav file into it
				string destination = Path.Combine(expectedMediaFolderPath, "AudioVisual");
				Directory.CreateDirectory(destination);
				string path = Path.Combine(FwDirectoryFinder.SourceDirectory, "LanguageExplorerTests", "Works", "TestData", "AudioFiles", "abu2.wav");
				File.Copy(path, Path.Combine(destination, Path.GetFileName(path)), true);

				//SUT
				var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entry, mainEntryNode, null, settings);
				if (isWebExport)
				{
					Assert.That(result, Contains.Substring("abu2.mp3"), "The automatic audio conversion in the CopyFileSafely method failed");
				}
				else
				{
					Assert.That(result, Contains.Substring("abu2.wav"), "ConfiguredXHTMLGenerator.GenerateXHTMLForEntry returned a string that did not include abu2.wav");
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
		public void GenerateXHTMLForEntry_AudioConversionIdenticalFileExists(bool isWebExport)
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
				DictionaryNodeOptions = GetFullyEnabledListOptions(DictionaryNodeListOptions.ListIds.Variant),
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
			CreateVariantForm(Cache, entry, variant, "Spelling Variant"); // we need a real Variant Type to pass the list options test
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
				var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, true, true, expectedMediaFolderPath, false, isWebExport);
				settings.Cache.LangProject.LinkedFilesRootDir = expectedMediaFolderPath;
				string destination = Path.Combine(expectedMediaFolderPath, "AudioVisual");

				// create a temp directory and copy a .wav file into it
				Directory.CreateDirectory(destination);
				string path = Path.Combine(FwDirectoryFinder.SourceDirectory, "LanguageExplorerTests", "Works", "TestData", "AudioFiles", "abu2.wav");
				if (isWebExport)
				{
					WavConverter.WavToMp3(path, Path.Combine(destination, "abu2.mp3"));
				}
				File.Copy(path, Path.Combine(destination, Path.GetFileName(path)), true);

				//SUT
				var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entry, mainEntryNode, null, settings);
				if (isWebExport)
				{
					Assert.That(result, Contains.Substring("abu2.mp3"), "The automatic audio conversion in the CopyFileSafely method failed");
				}
				else
				{
					Assert.That(result, Contains.Substring("abu2.wav"), "ConfiguredXHTMLGenerator.GenerateXHTMLForEntry returned a string that did not include abu2.wav");
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
		public void GenerateXHTMLForEntry_AudioConversionNonIdenticalFileExists(bool isWebExport)
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
				DictionaryNodeOptions = GetFullyEnabledListOptions(DictionaryNodeListOptions.ListIds.Variant),
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
			CreateVariantForm(Cache, entry, variant, "Spelling Variant"); // we need a real Variant Type to pass the list options test
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
				var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, true, true, expectedMediaFolderPath, false, isWebExport);
				settings.Cache.LangProject.LinkedFilesRootDir = expectedMediaFolderPath;
				string destination = Path.Combine(expectedMediaFolderPath, "AudioVisual");

				// create a temp directory and copy a .wav file into it
				Directory.CreateDirectory(destination);
				string path = Path.Combine(FwDirectoryFinder.SourceDirectory, "LanguageExplorerTests", "Works", "TestData", "AudioFiles", "abu2.wav");
				File.Copy(path, Path.Combine(destination, Path.GetFileName(path)), true);

				// create a fake file with the same name as the destination file but different content than the destination file should have after a conversion
				if (isWebExport)
				{
					string fakePath = Path.Combine(destination, "abu2.mp3");
					byte[] bytes = { 177, 209, 137, 61, 204, 127, 103, 88 };
					File.WriteAllBytes(fakePath, bytes);
				}

				//SUT
				var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entry, mainEntryNode, null, settings);
				if (isWebExport)
				{
					Assert.That(result, Contains.Substring("abu21.mp3"), "The automatic audio conversion code in the CopyFileSafely method did not change the file name as it should have since a file with the same name but different contents already exists");
				}
				else
				{
					Assert.That(result, Contains.Substring("abu2.wav"), "ConfiguredXHTMLGenerator.GenerateXHTMLForEntry returned a string that did not include abu2.wav");
				}
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_WsAudiowithRelativePaths()
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
			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, true, true, "//audio/source/@src");
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entryOne, mainEntryNode, null, settings);

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
		public void GenerateXHTMLForEntry_WsAudioCrashOnPrimarySelection()
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
				msa.PartOfSpeechRA.Abbreviation.set_String(_wsEn, "Blah");

				var entryOne = CreateInterestingLexEntry(Cache);
				var senseaudio = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
				entryOne.LexemeFormOA = senseaudio;
				senseaudio.Form.set_String(wsEnAudio.Handle, TsStringUtils.MakeString("TestAudio.wav", wsEnAudio.Handle));
				var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, true, true, "//audio/source/@src");

				// SUT
				Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entry, mainEntryNode, null, settings), "Having an audio ws first should not cause crash.");
			}
			finally
			{
				//Remove the AudioWS from the Cache which was added in AnalysisWritingSystem for this test
				Cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems.Remove(wsEnAudio);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_GeneratesComplexFormTypeForSubentryUnderSense()
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
				FieldDescription = ConfiguredXHTMLGenerator.LookupComplexEntryType,
				CSSClassNameOverride = "complexformtypes",
				Children = new List<ConfigurableDictionaryNode> { revAbbrevNode }
			};
			var subentryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { refTypeNode },
				DictionaryNodeOptions = GetFullyEnabledListOptions(DictionaryNodeListOptions.ListIds.Complex),
				FieldDescription = "Subentries"
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { subentryNode },
				FieldDescription = "SensesOS", CSSClassNameOverride = "senses"
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { sensesNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(lexentry, mainEntryNode, null, settings);
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
		public void GenerateXHTMLForEntry_GeneratesComplexFormTypeForSubentry()
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
				FieldDescription = ConfiguredXHTMLGenerator.LookupComplexEntryType,
				CSSClassNameOverride = "complexformtypes",
				Children = new List<ConfigurableDictionaryNode> { revAbbrevNode }
			};
			var subentryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { refTypeNode },
				DictionaryNodeOptions = GetFullyEnabledListOptions(DictionaryNodeListOptions.ListIds.Complex),
				FieldDescription = "Subentries"
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { subentryNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(lexentry, mainEntryNode, null, settings);
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
		public void GenerateXHTMLForEntry_GeneratesComplexFormTypeForSubsubentry([Values(true, false)] bool isUnderSense)
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
				DictionaryNodeOptions = GetFullyEnabledListOptions(DictionaryNodeListOptions.ListIds.Complex),
				FieldDescription = "Subentries", ReferenceItem = "Subentries"
			};
			var revAbbrevNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ReverseAbbr",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "en" })
			};
			var refTypeNode = new ConfigurableDictionaryNode
			{
				FieldDescription = ConfiguredXHTMLGenerator.LookupComplexEntryType,
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
				DictionaryNodeOptions = GetFullyEnabledListOptions(DictionaryNodeListOptions.ListIds.Complex),
				ReferenceItem = "Subentries", FieldDescription = "Subentries"
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

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(lexentry, mainEntryNode, null, settings);
			const string fwdNameXpath =
				"//span[@class='subentries subentries']/span[@class='subentry subentry']/span[@class='subentries subentries']/span[@class='subentry subentry']"
				+ "/span[@class='complexformtypes']/span[@class='complexformtype']/span/span[@lang='en' and text()='{0}']";
			const string revNameXpath =
				"//span[@class='subentries subentries']/span[@class='subentry subentry']/span[@class='subentries subentries']/span[@class='subentry subentry']"
				+ "/span[@class='complexformtypes']/span[@class='complexformtype']/span[@class='reverseabbr']/span[@lang='en' and text()='{0}']";
			AssertThatXmlIn.String(result).HasNoMatchForXpath(string.Format(fwdNameXpath, complexRefAbbr));
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(string.Format(revNameXpath, complexRefRevAbbr), 1);
			AssertThatXmlIn.String(result).HasNoMatchForXpath(string.Format(revNameXpath, otherComplexRefRevAbbr),
				message: "should be confined to subentry");
		}

		[Test]
		public void GenerateXHTMLForEntry_DoesntGeneratesComplexFormType_WhenDisabled()
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
				FieldDescription = ConfiguredXHTMLGenerator.LookupComplexEntryType, IsEnabled = false,
				CSSClassNameOverride = "complexformtypes",
				Children = new List<ConfigurableDictionaryNode> { revAbbrevNode }
			};
			var subentryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { refTypeNode },
				DictionaryNodeOptions = new DictionaryNodeListAndParaOptions(),
				FieldDescription = "Subentries", IsEnabled = true
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

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(lexentry, mainEntryNode, null, settings);
			const string refTypeXpath = "//span[@class='subentries']/span[@class='subentry']/span[@class='complexformtypes']";
			AssertThatXmlIn.String(result).HasNoMatchForXpath(refTypeXpath);
			StringAssert.DoesNotContain(complexRefAbbr, result);
		}

		[Test]
		public void GenerateXHTMLForEntry_GeneratesComplexForm_WithEmptyList()
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
			var complexOptions = GetFullyEnabledListOptions(DictionaryNodeListOptions.ListIds.Complex);
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

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(lexentry, mainEntryNode, null, settings), "Having an empty complexentrytype list after the click event should not cause crash.");
		}

		[Test]
		public void GenerateXHTMLForEntry_GeneratesComplexForm_NoTypeSpecified()
		{
			var lexentry = CreateInterestingLexEntry(Cache);
			var complexEntry = CreateInterestingLexEntry(Cache);
			var complexFormRef= CreateComplexForm(Cache, lexentry, complexEntry, false);
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
			var complexOptions = GetFullyEnabledListOptions(DictionaryNodeListOptions.ListIds.Complex);
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

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(lexentry, mainEntryNode, null, settings);
			const string refTypeXpath = "//span[@class='visiblecomplexformbackrefs']/span[@class='visiblecomplexformbackref']/span[@class='complexformtypes']/span[@class='complexformtype']";
			AssertThatXmlIn.String(result).HasNoMatchForXpath(refTypeXpath);
			const string headwordXpath = "//span[@class='visiblecomplexformbackrefs']/span[@class='visiblecomplexformbackref']/span[@class='headword']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(headwordXpath, 1);
		}

		[Test]
		public void GenerateXHTMLForEntry_GeneratesSubentry_NoTypeSpecified()
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
				FieldDescription = ConfiguredXHTMLGenerator.LookupComplexEntryType,
				CSSClassNameOverride = "complexformtypes",
			};
			var complexOptions = GetFullyEnabledListOptions(DictionaryNodeListOptions.ListIds.Complex);
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

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(lexentry, mainEntryNode, null, settings);
			const string refTypeXpath = "//span[@class='subentries']/span[@class='subentry']/span[@class='complexformtypes']/span[@class='complexformtype']";
			AssertThatXmlIn.String(result).HasNoMatchForXpath(refTypeXpath);
			const string headwordXpath = "//span[@class='subentries']/span[@class='subentry']/span[@class='headword']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(headwordXpath, 1);
		}

		[Test]
		public void GenerateXHTMLForEntry_GeneratesVariant_WithEmptyList()
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
				DictionaryNodeOptions = GetFullyEnabledListOptions(DictionaryNodeListOptions.ListIds.Variant),
				FieldDescription = "VariantFormEntryBackRefs"
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { variantFormNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(lexentry, mainEntryNode, null, settings), "Having an empty variantentrytype list after the click event should not cause crash.");
		}

		[Test]
		public void GenerateXHTMLForEntry_GeneratesVariant_NoTypeSpecified()
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
				DictionaryNodeOptions = GetFullyEnabledListOptions(DictionaryNodeListOptions.ListIds.Variant),
				IsEnabled = true,
				FieldDescription = "VariantFormEntryBackRefs"
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { variantFormNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(lexentry, mainEntryNode, null, settings);
			const string refTypeXpath = "//span[@class='variantformentrybackrefs']/span[@class='variantformentrybackref']/span[@class='variantentrytypesrs']/span[@class='variantentrytypesr']";
			AssertThatXmlIn.String(result).HasNoMatchForXpath(refTypeXpath);
			const string headwordXpath = "//span[@class='variantformentrybackrefs']/span[@class='variantformentrybackref']/span[@class='headword']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(headwordXpath, 1);
		}

		[Test]
		public void GenerateXHTMLForEntry_VariantShowsIfNotHideMinorEntry_ViewDoesntMatter()
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
				DictionaryNodeOptions = GetFullyEnabledListOptions(DictionaryNodeListOptions.ListIds.Variant),
				FieldDescription = "LexEntry",
				Label = "Minor Entry (Variants)"
			};
			var model = new DictionaryConfigurationModel
			{
				Parts = new List<ConfigurableDictionaryNode> { new ConfigurableDictionaryNode(), minorEntryNode }, // dummy main entry node
				IsRootBased = false
			};
			CssGeneratorTests.PopulateFieldsForTesting(model);

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(variantEntry, model, null, settings);
			Assert.IsNullOrEmpty(result);
			// try with HideMinorEntry off
			variantEntryRef.HideMinorEntry = 0;
			result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(variantEntry, model, null, settings);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath("/div[@class='lexentry']/span[@class='headword']", 1);
			// Should get the same results if in Root based view
			model.IsRootBased = true;
			result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(variantEntry, model, null, settings);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath("/div[@class='lexentry']/span[@class='headword']", 1);
			variantEntryRef.HideMinorEntry = 1;
			result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(variantEntry, model, null, settings);
			Assert.IsNullOrEmpty(result);
		}

		[Test]
		public void GenerateXHTMLForEntry_ReferencedNode_GeneratesBothClasses()
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

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(lexentry, mainEntryNode, null, settings);
			const string headwordXpath = "//span[@class='reffingsubs sharedsubentries']/span[@class='reffingsub sharedsubentry']/span[@class='headword']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(headwordXpath, 1);
		}

		[Test]
		public void GenerateXHTMLForEntry_GeneratesCorrectMainAndMinorEntries()
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

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForMainEntry(idiom, mainEntryNode, null, settings);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath("/div[@class='lexentry']/span[@class='headword']", 1);

			var complexOptions = (DictionaryNodeListOptions)mainEntryNode.DictionaryNodeOptions;
			complexOptions.Options[0].IsEnabled = false;
			result = ConfiguredXHTMLGenerator.GenerateXHTMLForMainEntry(idiom, mainEntryNode, null, settings);
			Assert.IsEmpty(result);
		}

		/// <remarks>Note that the "Unspecified" Types mentioned here are truly unspecified, not the specified Type "Unspecified Form Type"</remarks>
		[Test]
		public void GenerateXHTMLForEntry_GeneratesCorrectMinorEntries(
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

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(minorEntry, model, null, settings);

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
			var assembly = Assembly.Load(ConfiguredXHTMLGenerator.AssemblyFile);
			Assert.True(ConfiguredXHTMLGenerator.IsCollectionType(typeof(IEnumerable<>)));
			Assert.True(ConfiguredXHTMLGenerator.IsCollectionType(typeof(ILcmOwningSequence<>)));
			Assert.True(ConfiguredXHTMLGenerator.IsCollectionType(typeof(ILcmReferenceCollection<>)));
			var twoParamImplOfIFdoVector =
				assembly.GetType("SIL.LCModel.DomainImpl.ScrTxtPara").GetNestedType("OwningSequenceWrapper`2", BindingFlags.NonPublic);
			Assert.True(ConfiguredXHTMLGenerator.IsCollectionType(twoParamImplOfIFdoVector));
			Assert.True(ConfiguredXHTMLGenerator.IsCollectionType(typeof(ILcmVector)), "Custom fields containing list items may no longer work.");

			// Strings and MultiStrings, while enumerable, are not collections as we define them for the purpose of publishing data as XHTML
			Assert.False(ConfiguredXHTMLGenerator.IsCollectionType(typeof(string)));
			Assert.False(ConfiguredXHTMLGenerator.IsCollectionType(typeof(ITsString)));
			Assert.False(ConfiguredXHTMLGenerator.IsCollectionType(typeof(IMultiStringAccessor)));
			Assert.False(ConfiguredXHTMLGenerator.IsCollectionType(assembly.GetType("SIL.LCModel.DomainImpl.VirtualStringAccessor")));
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
			var entryCorps = CreateInterestingLexEntry(Cache, "corps", "body");
			var Pronunciation = AddPronunciationToEntry(entryCorps, "pronunciation", _wsFr, Cache);
			var exampleCorpsBody1 = AddExampleToSense(entryCorps.SensesOS[0], "Le corps est gros.", "The body is big.");
			var exampleCorpsBody2 = AddExampleToSense(entryCorps.SensesOS[0], "Le corps est esprit.", "The body is spirit.");
			AddSenseToEntry(entryCorps, "corpse", _wsEn, Cache);
			AddExampleToSense(entryCorps.SensesOS[1], "Le corps est mort.", "The corpse is dead.");

			var sensePic = Cache.ServiceLocator.GetInstance<ICmPictureFactory>().Create();
			var wsFr = Cache.WritingSystemFactory.GetWsFromStr("fr");
			sensePic.Caption.set_String(wsFr, TsStringUtils.MakeString("caption", wsFr));
			entryCorps.SensesOS[0].PicturesOS.Add(sensePic);

			Pronunciation.DoNotPublishInRC.Add(typeTest);
			entryCorps.SensesOS[0].DoNotPublishInRC.Add(typeTest);
			exampleCorpsBody1.DoNotPublishInRC.Add(typeTest);
			exampleCorpsBody2.DoNotPublishInRC.Add(typeMain);	// should not show at all!
			sensePic.DoNotPublishInRC.Add(typeTest);

			entryCorps.SensesOS[1].DoNotPublishInRC.Add(typeMain);
			//exampleCorpsCorpse1.DoNotPublishInRC.Add(typeMain); -- should not show in main because its owner is not shown there

			// This entry is published only in main, together with its sense and example.
			var entryBras = CreateInterestingLexEntry(Cache, "bras", "arm");
			AddExampleToSense(entryBras.SensesOS[0], "Mon bras est casse.", "My arm is broken.");
			AddSenseToEntry(entryBras, "hand", _wsEn, Cache);
			AddExampleToSense(entryBras.SensesOS[1], "Mon bras va bien.", "My arm is fine.");
			entryBras.DoNotPublishInRC.Add(typeTest);
			entryBras.SensesOS[0].DoNotPublishInRC.Add(typeTest);
			entryBras.SensesOS[1].DoNotPublishInRC.Add(typeTest);
			//exampleBrasArm1.DoNotPublishInRC.Add(typeTest); -- should not show in test because its owner is not shown there
			//exampleBrasHand1.DoNotPublishInRC.Add(typeTest); -- should not show in test because its owner is not shown there

			// This entry is published only in test, together with its sense and example.
			var entryOreille = CreateInterestingLexEntry(Cache, "oreille", "ear");
			AddExampleToSense(entryOreille.SensesOS[0], "Lac Pend d'Oreille est en Idaho.", "Lake Pend d'Oreille is in Idaho.");
			entryOreille.DoNotPublishInRC.Add(typeMain);
			entryOreille.SensesOS[0].DoNotPublishInRC.Add(typeMain);
			//exampleOreille1.DoNotPublishInRC.Add(typeMain); -- should not show in main because its owner is not shown there

			var entryEntry = CreateInterestingLexEntry(Cache, "entry", "entry");
			var entryMainsubentry = CreateInterestingLexEntry(Cache, "mainsubentry", "mainsubentry");
			entryMainsubentry.DoNotPublishInRC.Add(typeTest);
			CreateComplexForm(Cache, entryEntry, entryMainsubentry, true);

			var entryTestsubentry = CreateInterestingLexEntry(Cache, "testsubentry", "testsubentry");
			entryTestsubentry.DoNotPublishInRC.Add(typeMain);
			CreateComplexForm(Cache, entryEntry, entryTestsubentry, true);
			var bizarroVariant = CreateInterestingLexEntry(Cache, "bizarre", "myVariant");
			CreateVariantForm(Cache, entryEntry, bizarroVariant, "Spelling Variant");
			bizarroVariant.DoNotPublishInRC.Add(typeTest);

			// Note that the decorators must be created (or refreshed) *after* the data exists.
			int flidVirtual = Cache.ServiceLocator.GetInstance<Virtuals>().LexDbEntries;
			var pubEverything = new DictionaryPublicationDecorator(Cache, (ISilDataAccessManaged)Cache.MainCacheAccessor, flidVirtual);
			var pubMain = new DictionaryPublicationDecorator(Cache, (ISilDataAccessManaged)Cache.MainCacheAccessor, flidVirtual, typeMain);
			var pubTest = new DictionaryPublicationDecorator(Cache, (ISilDataAccessManaged)Cache.MainCacheAccessor, flidVirtual, typeTest);
			//SUT
			var hvosMain = new List<int>( pubMain.GetEntriesToPublish(_flexComponentParameters.PropertyTable, flidVirtual) );
			Assert.AreEqual(5, hvosMain.Count, "there are five entries in the main publication");
			Assert.IsTrue(hvosMain.Contains(entryCorps.Hvo), "corps is shown in the main publication");
			Assert.IsTrue(hvosMain.Contains(entryBras.Hvo), "bras is shown in the main publication");
			Assert.IsTrue(hvosMain.Contains(bizarroVariant.Hvo), "bizarre is shown in the main publication");
			Assert.IsFalse(hvosMain.Contains(entryOreille.Hvo), "oreille is not shown in the main publication");
			Assert.IsTrue(hvosMain.Contains(entryEntry.Hvo), "entry is shown in the main publication");
			Assert.IsTrue(hvosMain.Contains(entryMainsubentry.Hvo), "mainsubentry is shown in the main publication");
			Assert.IsFalse(hvosMain.Contains(entryTestsubentry.Hvo), "testsubentry is not shown in the main publication");
			var hvosTest = new List<int>(pubTest.GetEntriesToPublish(_flexComponentParameters.PropertyTable, flidVirtual));
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
				DictionaryNodeOptions = GetFullyEnabledListOptions(DictionaryNodeListOptions.ListIds.Variant)
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

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			var output = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entryCorps, mainEntryNode, pubEverything, settings);
			Assert.IsNotNullOrEmpty(output);
			// Verify that the unfiltered output displays everything.
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFrenchEntry, 1);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFrenchPronunciation, 1);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchEnglishDefOrGloss, 2);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFrenchExample, 3);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchEnglishTranslation, 3);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFrenchPictureCaption, 1);

			//SUT
			output = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entryCorps, mainEntryNode, pubMain, settings);
			Assert.IsNotNullOrEmpty(output);
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
			output = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entryCorps, mainEntryNode, pubTest, settings);
			Assert.IsNotNullOrEmpty(output);
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
			output = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entryBras, mainEntryNode, pubEverything, settings);
			Assert.IsNotNullOrEmpty(output);
			// Verify that the unfiltered output displays everything.
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFrenchEntry, 1);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchEnglishDefOrGloss, 2);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFrenchExample, 2);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchEnglishTranslation, 2);

			//SUT
			output = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entryBras, mainEntryNode, pubMain, settings);
			Assert.IsNotNullOrEmpty(output);
			// Verify that the main publication output displays everything.
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFrenchEntry, 1);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchEnglishDefOrGloss, 2);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFrenchExample, 2);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchEnglishTranslation, 2);

			//SUT
			// We can still produce test publication output for the entry since we have a copy of it.  Its senses and
			// examples should not be displayed because the senses are separately hidden.
			output = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entryBras, mainEntryNode, pubTest, settings);
			Assert.IsNotNullOrEmpty(output);
			// Verify that the test output doesn't display the senses and examples.
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFrenchEntry, 1);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchEnglishDefOrGloss, 0);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFrenchExample, 0);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchEnglishTranslation, 0);

			//SUT
			output = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entryOreille, mainEntryNode, pubEverything, settings);
			Assert.IsNotNullOrEmpty(output);
			// Verify that the unfiltered output displays everything.
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFrenchEntry, 1);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchEnglishDefOrGloss, 1);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFrenchExample, 1);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchEnglishTranslation, 1);

			//SUT
			// We can still produce main publication output for the entry since we have a copy of it.  Its sense and
			// example should not be displayed because the sense is separately hidden.
			output = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entryOreille, mainEntryNode, pubMain, settings);
			Assert.IsNotNullOrEmpty(output);
			// Verify that the test output doesn't display the sense and example.
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFrenchEntry, 1);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchEnglishDefOrGloss, 0);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFrenchExample, 0);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchEnglishTranslation, 0);

			//SUT
			output = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entryOreille, mainEntryNode, pubTest, settings);
			Assert.IsNotNullOrEmpty(output);
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
			output = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entryEntry, mainEntryNode, pubMain, settings);
			Assert.IsNotNullOrEmpty(output);
			// Verify that the main publication output displays what it should.
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFrenchEntry, 1);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchEnglishDefOrGloss, 1);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFrenchSubentry, 1);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchMainsubentry, 1);
			AssertThatXmlIn.String(output).HasNoMatchForXpath(matchTestsubentry);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchVariantRef, 1);

			//SUT
			output = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entryEntry, mainEntryNode, pubTest, settings);
			Assert.IsNotNullOrEmpty(output);
			// Verify that the test publication output displays what it should.
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFrenchEntry, 1);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchEnglishDefOrGloss, 1);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFrenchSubentry, 1);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchMainsubentry, 0);
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchTestsubentry, 1);
			AssertThatXmlIn.String(output).HasNoMatchForXpath(matchVariantRef);
		}

		[Test]
		public void GenerateXHTMLForEntry_GeneratesVariantEntryTypesLabelWithNoRepetition()
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
				DictionaryNodeOptions = GetFullyEnabledListOptions(DictionaryNodeListOptions.ListIds.Variant)
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
				var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
				var output = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entryEntry, mainEntryNode, null, settings);
				const string matchVariantRef = "//span[@class='variantentrytypes']/span[@class='variantentrytype']/span[@class='name']/span[@lang='en']";
				AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchVariantRef, 2);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_GeneratesVariantEntryTypesShowOnlySelectedListItem()
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
				DictionaryNodeOptions = GetFullyEnabledListOptions(DictionaryNodeListOptions.ListIds.Variant)
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
				DictionaryNodeOptions = GetFullyEnabledListOptions(DictionaryNodeListOptions.ListIds.Variant)
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
			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			var output = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entryEntry, mainEntryNode, null, settings);
			const string matchFreeVariantRef = "//span[@class='variantentrytypes']/span[@class='variantentrytype']/span[@class='name']/span[@lang='en' and text()='Free Variant']";
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchFreeVariantRef, 1);
			const string matchSpellingVariantRef = "//span[@class='variantentrytypes']/span[@class='variantentrytype']/span[@class='name']/span[@lang='en' and text()='Spelling Variant']";
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchSpellingVariantRef, 0);
		}

		[Test]
		public void GenerateXHTMLForEntry_GeneratesComplexFormEntryTypesLabelWithNoRepetition()
		{
			var typeMain = CreatePublicationType("main");

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
				DictionaryNodeOptions = GetFullyEnabledListOptions(DictionaryNodeListOptions.ListIds.Complex)
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
			var output = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entryEntry, mainEntryNode, pubMain, DefaultSettings);
			const string matchComplexFormRef = "//span[@class='complexformtypes']/span[@class='complexformtype']/span[@class='name']/span[@lang='en']";
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchComplexFormRef, 1);
		}

		[Test]
		public void GenerateXHTMLForEntry_GeneratesComplexFormEntryTypesAndNamesGroup()
		{
			var typeMain = CreatePublicationType("main");

			var entryEntry = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(entryEntry, "entry", _wsFr);

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
				DictionaryNodeOptions = GetFullyEnabledListOptions(DictionaryNodeListOptions.ListIds.Complex)
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
			var output = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entryEntry, mainEntryNode, pubMain, DefaultSettings);
			const string matchComplexFormTypeCompound = "//span[@class='complexformtypes']/span[@class='complexformtype']/span[@class='name']/span[@lang='en' and text()='Compound']";
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchComplexFormTypeCompound, 1);
			const string matchComplexFormTypeIdiom = "//span[@class='complexformtypes']/span[@class='complexformtype']/span[@class='name']/span[@lang='en' and text()='Idiom']";
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchComplexFormTypeIdiom, 1);
			const string matchComplexFormName = "//span[@class='visiblecomplexformbackref']/span[@class='headword']/span[@lang='fr']/a";
			AssertThatXmlIn.String(output).HasSpecifiedNumberOfMatchesForXpath(matchComplexFormName, 4);
		}

		[Test]
		public void GenerateXHTMLForEntry_ComplexFormAndSenseInPara()
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
				FieldDescription = ConfiguredXHTMLGenerator.LookupComplexEntryType,
				CSSClassNameOverride = "complexformtypes",
				Children = new List<ConfigurableDictionaryNode> { revAbbrevNode }
			};
			var subentryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { refTypeNode },
				DictionaryNodeOptions = GetFullyEnabledListOptions(DictionaryNodeListOptions.ListIds.Complex),
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
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(lexentry, mainEntryNode, null, DefaultSettings);
			const string senseXpath = "div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense']/span[@class='gloss']/span[@lang='en' and text()='gloss']";
			var paracontinuationxpath = string.Format(
			"div[@class='lexentry']//span[@class='subentries']/span[@class='subentry']/span[@class='complexformtypes']/span[@class='complexformtype']/span[@class='reverseabbr']/span[@lang='en' and text()='{0}']",
				complexRefRevAbbr);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(senseXpath, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(paracontinuationxpath, 1);
		}

		[Test]
		public void GenerateXHTMLForEntry_MinorComplexForm_GeneratesGlossOrSummaryDefinition()
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
				DictionaryNodeOptions = GetFullyEnabledListOptions(DictionaryNodeListOptions.ListIds.Complex),
				Children = new List<ConfigurableDictionaryNode> { refentryNode }
			};
			var minorEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { ComponentsNode },
				FieldDescription = "LexEntry",
				CSSClassNameOverride = "minorentrycomplex",
			};
			CssGeneratorTests.PopulateFieldsForTesting(minorEntryNode);

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(subentry1, minorEntryNode, null, settings);
			const string complexFormEntryRefXpath = "div[@class='minorentrycomplex']/span[@class='complexformentryrefs']/span[@class='complexformentryref']";
			const string referencedEntriesXpath = "/span[@class='referencedentries']/span[@class='referencedentry']";
			const string glossOrSummXpath1 = complexFormEntryRefXpath + referencedEntriesXpath + "/span[@class='glossorsummary']/span[@lang='en' and text()='MainEntrySummaryDefn']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(glossOrSummXpath1, 1);

			//SUT
			var result2 = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(subentry2, minorEntryNode, null, settings);
			const string glossOrSummXpath2 = complexFormEntryRefXpath + referencedEntriesXpath + "/span[@class='glossorsummary']/span[@lang='en' and text()='gloss2']";
			AssertThatXmlIn.String(result2).HasSpecifiedNumberOfMatchesForXpath(glossOrSummXpath2, 1);

			//SUT
			var result3 = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(subentry3, minorEntryNode, null, settings);
			const string glossOrSummXpath3 = complexFormEntryRefXpath + referencedEntriesXpath + "/span[@class='glossorsummary']/span[@lang='en' and text()='MainEntryS3Defn']";
			AssertThatXmlIn.String(result3).HasSpecifiedNumberOfMatchesForXpath(glossOrSummXpath3, 1);
		}

		[Test]
		public void GenerateXHTMLForEntry_ContinuationParagraphWithEmtpyContentDoesNotGenerateSelfClosingTag()
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

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(lexentry, mainEntryNode, null, settings);
			const string senseXpath = "div[@class='lexentry']/span[@class='senses']/span[@class='sensecontent']/span[@class='sense']/span[@class='gloss']/span[@lang='en' and text()='gloss']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(senseXpath, 1);
			Assert.That(result, Is.Not.StringMatching(@"<div class=['""]paracontinuation['""]\s*/>"),
				"Empty Self closing <div> element should not generated after senses in paragraph");
			Assert.That(result, Is.Not.StringMatching(@"<div class=['""]paracontinuation['""]\s*></div>"),
				"Empty <div> element should not be generated after senses in paragraph");
		}

		[Test]
		public void SavePublishedHtmlWithStyles_ProduceLetHeadOnlyWhenDesired()
		{
			var lexentry1 = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(lexentry1, "femme", _wsFr);
			var lexentry2 = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(lexentry2, "homme", _wsFr);
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
				xhtmlPath = ConfiguredXHTMLGenerator.SavePreviewHtmlWithStyles(new[] { lexentry1.Hvo, lexentry2.Hvo }, pubEverything, model, _flexComponentParameters.PropertyTable, Cache, _recordList);
				var xhtml = File.ReadAllText(xhtmlPath);
				AssertThatXmlIn.String(xhtml).HasSpecifiedNumberOfMatchesForXpath(xpath, 2);
				DeleteTempXhtmlAndCssFiles(xhtmlPath);

				xhtmlPath = ConfiguredXHTMLGenerator.SavePreviewHtmlWithStyles(new[] { lexentry1.Hvo, lexentry2.Hvo }, null, model, _flexComponentParameters.PropertyTable, Cache, _recordList);
				xhtml = File.ReadAllText(xhtmlPath);
				AssertThatXmlIn.String(xhtml).HasSpecifiedNumberOfMatchesForXpath(xpath, 2);
				DeleteTempXhtmlAndCssFiles(xhtmlPath);

				xhtmlPath = ConfiguredXHTMLGenerator.SavePreviewHtmlWithStyles(new[] { lexentry1.Hvo }, pubEverything, model, _flexComponentParameters.PropertyTable, Cache, _recordList);
				xhtml = File.ReadAllText(xhtmlPath);
				AssertThatXmlIn.String(xhtml).HasSpecifiedNumberOfMatchesForXpath(xpath, 1);
				DeleteTempXhtmlAndCssFiles(xhtmlPath);

				xhtmlPath = ConfiguredXHTMLGenerator.SavePreviewHtmlWithStyles(new[] { lexentry1.Hvo }, null, model, _flexComponentParameters.PropertyTable, Cache, _recordList);
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
			AddHeadwordToEntry(firstAEntry, firstAHeadword, _wsFr);
			var bEntry = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(bEntry, bHeadword, _wsFr);
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
				var recordList = RecordList.ActiveRecordListRepository.ActiveRecordList;
				recordList.SortName = "Glosses";
				xhtmlPath = ConfiguredXHTMLGenerator.SavePreviewHtmlWithStyles(new[] { firstAEntry.Hvo }, pubEverything, model, _flexComponentParameters.PropertyTable, Cache, _recordList);
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
			AddHeadwordToEntry(firstAEntry, firstAHeadword, _wsFr);
			var secondAEntry = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(secondAEntry, secondAHeadword, _wsFr);
			var bEntry = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(bEntry, bHeadword, _wsFr);
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
				xhtmlPath = ConfiguredXHTMLGenerator.SavePreviewHtmlWithStyles(new[] { firstAEntry.Hvo, secondAEntry.Hvo, bEntry.Hvo }, pubEverything, model, _flexComponentParameters.PropertyTable, Cache, _recordList);
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
			AddHeadwordToEntry(firstAEntry, firstAHeadword, _wsFr);
			var secondAEntry = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(secondAEntry, secondAHeadword, _wsFr);
			var bEntry = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(bEntry, bHeadword, _wsFr);
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
			const string pageButtonXPath = "//div[@class='pages']/span[@class='pagebutton']";
			try
			{
				xhtmlPath = ConfiguredXHTMLGenerator.SavePreviewHtmlWithStyles(new[] { firstAEntry.Hvo, secondAEntry.Hvo, bEntry.Hvo }, pubEverything, model, _flexComponentParameters.PropertyTable, Cache, _recordList, entriesPerPage: 1);
				var xhtml = File.ReadAllText(xhtmlPath);
				AssertThatXmlIn.String(xhtml).HasSpecifiedNumberOfMatchesForXpath(pagesDivXPath, 2);
				AssertThatXmlIn.String(xhtml).HasSpecifiedNumberOfMatchesForXpath(pageButtonXPath, 6);
				var cssPath = Path.ChangeExtension(xhtmlPath, "css");
				var css = File.ReadAllText(cssPath);
				// verify that the css file contains a line similar to: @media screen {
				Assert.IsTrue(Regex.Match(css, @"@media\s*screen\s*{\s*\.pages\s*{\s*display:\s*table;\s*width:\s*100%;").Success,
								  "Css for page buttons did not generate a screen-only rule");
				// verify that the css file contains a line similar to: @media print {
				Assert.IsTrue(Regex.Match(css, @"@media\s*print\s*{\s*\.pages\s*{\s*display:\s*none;\s*}").Success,
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
				AddHeadwordToEntry(entry, "a" + i, _wsFr);
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
				xhtmlPath = ConfiguredXHTMLGenerator.SavePreviewHtmlWithStyles(hvos, pubEverything, model, _flexComponentParameters.PropertyTable, Cache, _recordList, entriesPerPage: 10);
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
				xhtmlPath = ConfiguredXHTMLGenerator.SavePreviewHtmlWithStyles(hvos, pubEverything, model, _flexComponentParameters.PropertyTable, Cache, _recordList, entriesPerPage: 8);
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
				xhtmlPath = ConfiguredXHTMLGenerator.SavePreviewHtmlWithStyles(hvos, pubEverything, model, _flexComponentParameters.PropertyTable, Cache, _recordList, entriesPerPage: 8);
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
		public void CheckSubsenseOutput()
		{
			var posNoun = CreatePartOfSpeech("noun", "n");
			var posAdj = CreatePartOfSpeech("adjective", "adj");

			var firstHeadword = "homme";
			var firstEntry = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(firstEntry, firstHeadword, _wsFr);
			AddSingleSubSenseToSense("man", firstEntry.SensesOS[0]);
			var msa1 = CreateMSA(firstEntry, posNoun);
			firstEntry.SensesOS[0].MorphoSyntaxAnalysisRA = msa1;
			firstEntry.SensesOS[0].SensesOS[0].MorphoSyntaxAnalysisRA = msa1;

			var secondHeadword = "femme";
			var secondEntry = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(secondEntry, secondHeadword, _wsFr);
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
			AddHeadwordToEntry(thirdEntry, thirdHeadword, _wsFr);
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
				xhtmlPath = ConfiguredXHTMLGenerator.SavePreviewHtmlWithStyles(new[] { thirdEntry.Hvo, secondEntry.Hvo, firstEntry.Hvo }, pubEverything, model, _flexComponentParameters.PropertyTable, Cache, _recordList);
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
			var preferredPath = ConfiguredXHTMLGenerator.SavePreviewHtmlWithStyles(entries, null, model, _flexComponentParameters.PropertyTable, Cache, _recordList); // to get the preferred path
			var actualPath = preferredPath;
			try
			{
				using (new StreamWriter(preferredPath)) // lock the preferred path
				{
					Assert.DoesNotThrow(() => actualPath = ConfiguredXHTMLGenerator.SavePreviewHtmlWithStyles(entries, null, model, _flexComponentParameters.PropertyTable, Cache, _recordList));
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
				FilePath = Path.Combine(DictionaryConfigurationServices.GetProjectConfigurationDirectory(_flexComponentParameters.PropertyTable), "filename" + LanguageExplorerConstants.DictionaryConfigurationFileExtension)
			};
			var xhtmlPath = ConfiguredXHTMLGenerator.SavePreviewHtmlWithStyles(entries, null, model, _flexComponentParameters.PropertyTable, Cache, _recordList);
			try
			{
				var previewXhtmlContent = File.ReadAllText(xhtmlPath);
				// ReSharper disable once AssignNullToNotNullAttribute -- Justification: XHTML is always saved in a directory
				var fileName = "ProjectDictionaryOverrides.css";
				StringAssert.Contains(fileName, previewXhtmlContent, "Custom css file should be added in the XHTML file");
			}
			finally
			{
				DeleteTempXhtmlAndCssFiles(xhtmlPath);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_EmbeddedWritingSystemGeneratesCorrectResult()
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
			var englishStr = TsStringUtils.MakeString("English", _wsEn);
			var frenchString = TsStringUtils.MakeString("French with  embedded", _wsFr);
			var multiRunString = frenchString.Insert(12, englishStr);
			entry.Bibliography.set_String(_wsFr, multiRunString);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entry, mainEntryNode, null, DefaultSettings);
			const string nestedEn = "/div[@class='lexentry']/span[@class='bib']/span[@lang='fr']/span[@lang='en']";
			const string nestedFr = "/div[@class='lexentry']/span[@class='bib']/span[@lang='fr']/span[@lang='fr']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(nestedEn, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(nestedFr, 2);
		}

		[Test]
		public void GenerateXHTMLForEntry_EmbeddedWritingSystemOfOppositeDirectionGeneratesCorrectResult()
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
			var multiRunString = MakeBidirectionalTss(new[] { "דוד", " et ", "דניאל" });
			entry.Bibliography.set_String(_wsHe, multiRunString);
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entry, mainEntryNode, null, DefaultSettings);
			const string nestedEn = "/div[@class='lexentry']/span[@class='bib']/span[@lang='he']/span[@dir='rtl']/span[@lang='en']/span[@dir='ltr']";
			const string nestedHe = "/div[@class='lexentry']/span[@class='bib']/span[@lang='he']/span[@dir='rtl']/span[@lang='he']";
			const string extraDirection = "/div[@class='lexentry']/span[@class='bib']/span[@lang='he']/span[@dir='rtl']/span[@lang='he']/span[@dir='rtl']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(nestedEn, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(nestedHe, 2);
			AssertThatXmlIn.String(result).HasNoMatchForXpath(extraDirection);
		}

		[Test]
		public void GenerateXHTMLForEntry_WritingSystemOfSameDirectionGeneratesNoExtraDirectionSpan()
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
			var multiRunString = MakeBidirectionalTss(new[] { "ירמיהו", " was a bullfrog." });
			entry.Bibliography.set_String(_wsHe, multiRunString);
			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null, true); // Right-to-Left
			//SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entry, mainEntryNode, null, settings);
			const string nestedEn = "/div[@class='lexentry']/span[@class='bib']/span[@lang='he']/span[@lang='en']/span[@dir='ltr']";
			const string nestedHe = "/div[@class='lexentry']/span[@class='bib']/span[@lang='he']/span[@lang='he']";
			const string extraDirection0 = "/div[@class='lexentry']/span[@class='bib']/span[@lang='he']/span[@dir='rtl']";
			const string extraDirection1 = "/div[@class='lexentry']/span[@class='bib']/span[@lang='he']/span[@lang='he']/span[@dir='rtl']";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(nestedEn, 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(nestedHe, 1);
			AssertThatXmlIn.String(result).HasNoMatchForXpath(extraDirection0);
			AssertThatXmlIn.String(result).HasNoMatchForXpath(extraDirection1);
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
		public void GenerateXHTMLForEntry_CompareRelations_SimpleSituations_SortByHeadword([Values(true, false)] bool SeparateReferences)
		{
			var mainEntry = CreateInterestingLexEntry(Cache, "MainEntry");
			var compareReferencedEntry1 = CreateInterestingLexEntry(Cache, "b", "b comparable");
			var compareReferencedEntry2 = CreateInterestingLexEntry(Cache, "a", "a comparable");
			var compareReferencedEntry3 = CreateInterestingLexEntry(Cache, "c", "c comparable");
			const string comRefTypeName = "Compare";
			var comRefType = CreateLexRefType(LexRefTypeTags.MappingTypes.kmtEntryCollection, comRefTypeName, "cf", string.Empty, string.Empty);
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
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(mainEntry, mainEntryNode, null, DefaultSettings);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(crossRefOwnerTypeXpath, 1); // ensure there is only one
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(CrossRefOwnerTypeXpath(comRefTypeName), 1); // ...the *correct* one
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(HeadwordOrderInCrossRefsXpath(1, "a"), 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(HeadwordOrderInCrossRefsXpath(2, "b"), 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(HeadwordOrderInCrossRefsXpath(3, "c"), 1);
		}

		[Test]
		public void GenerateXHTMLForEntry_CompareRelations_ComplexSituation_SortByHeadword()
		{
			var mainEntry = CreateInterestingLexEntry(Cache, "MainEntry");
			var compareReferencedEntry1 = CreateInterestingLexEntry(Cache, "b", "b comparable");
			var compareReferencedEntry2 = CreateInterestingLexEntry(Cache, "a", "a comparable");
			var compareReferencedEntry3 = CreateInterestingLexEntry(Cache, "c", "c comparable");
			var compareReferencedEntry4 = CreateInterestingLexEntry(Cache, "ba", "ba comparable");
			var compareReferencedEntry5 = CreateInterestingLexEntry(Cache, "ca", "ca comparable");
			const string comRefTypeName = "Compare";
			var comRefType = CreateLexRefType(LexRefTypeTags.MappingTypes.kmtEntryCollection, comRefTypeName, "cf", string.Empty, string.Empty);
			CreateLexReference(comRefType, new List<ICmObject> { mainEntry, compareReferencedEntry3, compareReferencedEntry2 });
			CreateLexReference(comRefType, new List<ICmObject> { mainEntry, compareReferencedEntry5, compareReferencedEntry4, compareReferencedEntry1 });
			Assert.IsNotNull(comRefType);

			var mainEntryNode = ModelForCrossReferences(new[] { comRefType.Guid.ToString() });
			// SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(mainEntry, mainEntryNode, null, DefaultSettings);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(CrossRefOwnerTypeXpath(comRefTypeName), 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(HeadwordOrderInCrossRefsXpath(1, "a"), 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(HeadwordOrderInCrossRefsXpath(2, "b"), 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(HeadwordOrderInCrossRefsXpath(3, "ba"), 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(HeadwordOrderInCrossRefsXpath(4, "c"), 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(HeadwordOrderInCrossRefsXpath(5, "ca"), 1);
		}

		[Test]
		public void GenerateXHTMLForEntry_CrossRefs_Sequences_SequencePreserved()
		{
			var alphaEntry = CreateInterestingLexEntry(Cache, "alpha", "alpha");
			var redEntry = CreateInterestingLexEntry(Cache, "rouge", "red");
			var greenEntry = CreateInterestingLexEntry(Cache, "vert", "green");
			var blueEntry = CreateInterestingLexEntry(Cache, "bleu", "blue");
			var midAlphabetEntry = CreateInterestingLexEntry(Cache, "omega", "middle of the Roman alphabet; we're not testing Greek :-)");
			const string colorTypeName = "Color";
			var colorType = CreateLexRefType(LexRefTypeTags.MappingTypes.kmtEntrySequence, colorTypeName, "col", string.Empty, string.Empty);
			CreateLexReference(colorType, new List<ICmObject> { alphaEntry, redEntry, greenEntry, blueEntry });
			const string greekTypeName = "Greek";
			var greekType = CreateLexRefType(LexRefTypeTags.MappingTypes.kmtEntrySequence, greekTypeName, "grk", string.Empty, string.Empty);
			CreateLexReference(greekType, new List<ICmObject> { alphaEntry, midAlphabetEntry });

			var mainEntryNode = ModelForCrossReferences(new[] { colorType.Guid.ToString(), greekType.Guid.ToString() });
			// SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(alphaEntry, mainEntryNode, null, DefaultSettings);
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
		public void GenerateXHTMLForEntry_CrossRefs_Unidirectional_SequencePreserved()
		{
			var stoogesEntry = CreateInterestingLexEntry(Cache, "Stooges");
			var larryEntry = CreateInterestingLexEntry(Cache, "Larry");
			var curlyEntry = CreateInterestingLexEntry(Cache, "Curly");
			var moeEntry = CreateInterestingLexEntry(Cache, "Moe");
			const string characterTypeName = "Character";
			var characterType = CreateLexRefType(LexRefTypeTags.MappingTypes.kmtEntryUnidirectional, characterTypeName, "char", string.Empty, string.Empty);
			CreateLexReference(characterType, new List<ICmObject> { stoogesEntry, larryEntry, curlyEntry, moeEntry });

			var mainEntryNode = ModelForCrossReferences(new[] { characterType.Guid + ":f" });
			// SUT
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(stoogesEntry, mainEntryNode, null, DefaultSettings);
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
		public void GenerateXHTMLForEntry_LexicalReferencesOrderedCorrectly([Values(true, false)] bool usingSubfield)
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

			var antonyms = CreateLexRefType(LexRefTypeTags.MappingTypes.kmtEntryPair, "Antonym", "ant", null, null);
			CreateLexReference(antonyms, new[] { manEntry, womanEntry });
			CreateLexReference(antonyms, new[] { manEntry, boyEntry });
			CreateLexReference(antonyms, new[] { manEntry, thingEntry });
			CreateLexReference(antonyms, new[] { manEntry, beastEntry });
			CreateLexReference(antonyms, new[] { familyEntry, individualEntry });

			var wholeparts = CreateLexRefType(LexRefTypeTags.MappingTypes.kmtEntryTree, "Part", "pt", "Whole", "wh");
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
			const string antAbbrSpan = "<span class=\"ownertype_abbreviation\"><span lang=\"en\">ant</span></span>";
			const string whSpan = "<span class=\"ownertype_abbreviation\"><span lang=\"en\">wh</span></span>";
			const string ptSpan = "<span class=\"ownertype_abbreviation\"><span lang=\"en\">pt</span></span>";
			const string antNameSpan = "<span class=\"ownertype_name\"><span lang=\"en\">Antonym</span></span>";
			const string femmeSpan = "<span class=\"headword\"><span lang=\"fr\">femme</span></span>";
			var garçonSpan = TsStringUtils.Compose("<span class=\"headword\"><span lang=\"fr\">garçon</span></span>");
			var bêteSpan = TsStringUtils.Compose("<span class=\"headword\"><span lang=\"fr\">bête</span></span>");
			const string trucSpan = "<span class=\"headword\"><span lang=\"fr\">truc</span></span>";
			//SUT
			//Console.WriteLine(ConfiguredXHTMLGenerator.SavePreviewHtmlWithStyles(new[] { manEntry.Hvo, familyEntry.Hvo, girlEntry.Hvo, individualEntry.Hvo }, null,
			//	new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { mainEntryNode } }, m_mediator)); // full output for diagnostics
			var manResult = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(manEntry, mainEntryNode, null, settings);
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
				var familyResult = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(familyEntry, mainEntryNode, null, settings);
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
				var girlResult = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(girlEntry, mainEntryNode, null, settings);
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

			var individualResult = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(individualEntry, mainEntryNode, null, settings);
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
		public void GenerateXHTMLForEntry_VariantsOfEntryAreOrdered()
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
					DictionaryNodeOptions = GetFullyEnabledListOptions(DictionaryNodeListOptions.ListIds.Variant),
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
				var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);

				//SUT
				var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(lexentry, mainEntryNode, null, settings);

				// Test that variantformentrybackref items are in alphabetical order
				Assert.That(result.IndexOf("headwordA", StringComparison.InvariantCulture),
					Is.LessThan(result.IndexOf("headwordB", StringComparison.InvariantCulture)), "variant form not sorted in expected order");
				Assert.That(result.IndexOf("headwordB", StringComparison.InvariantCulture),
					Is.LessThan(result.IndexOf("headwordC", StringComparison.InvariantCulture)), "variant form not sorted in expected order");
				Assert.That(result.IndexOf("headwordC", StringComparison.InvariantCulture),
					Is.LessThan(result.IndexOf("headwordD", StringComparison.InvariantCulture)), "variant form not sorted in expected order");
			}
		}

		/// <summary>LT-17918. Intermittent failures should NOT be ignored.</summary>
		[Test]
		public void GenerateXHTMLForEntry_ComplexFormsAreOrderedAsUserSpecified(
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
					DictionaryNodeOptions = GetFullyEnabledListOptions(DictionaryNodeListOptions.ListIds.Complex),
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
				var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);

				//SUT
				var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(lexentry, mainEntryNode, null, settings);

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
		public void GenerateXHTMLForFieldByReflection_VariantFormTypesAreOrderedBasedOnOptionOrdering()
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
				DictionaryNodeOptions = GetFullyEnabledListOptions(DictionaryNodeListOptions.ListIds.Variant),
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
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForFieldByReflection(lexentry, variantFormNode, null, DefaultSettings);

			Assert.That(result.IndexOf("headwordA", StringComparison.InvariantCulture),
				Is.LessThan(result.IndexOf("headwordB", StringComparison.InvariantCulture)), "variant forms not appearing in an order corresponding to their type sorting");

			// Change the order of variantFormNode.DictionaryNodeOptions, which should result in the data being ordered differently.
			((DictionaryNodeListOptions)variantFormNode.DictionaryNodeOptions).Options.Reverse();

			// SUT2
			result = ConfiguredXHTMLGenerator.GenerateXHTMLForFieldByReflection(lexentry, variantFormNode, null, DefaultSettings);

			Assert.That(result.IndexOf("headwordB", StringComparison.InvariantCulture),
				Is.LessThan(result.IndexOf("headwordA", StringComparison.InvariantCulture)), "variant forms not appearing in an order corresponding to their type sorting");
		}

		/// <summary>
		/// LT-18018.
		/// The implementation code changes were done in GenerateXHTMLForILexEntryRefsByType.
		/// </summary>
		[Test]
		public void GenerateXHTMLForFieldByReflection_SubentryTypesAreOrderedBasedOnOptionOrdering()
		{
			var complexFormTypeNameNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Name",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "analysis" })
			};
			var complexFormTypeNode = new ConfigurableDictionaryNode
			{
				FieldDescription = ConfiguredXHTMLGenerator.LookupComplexEntryType,
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
				DictionaryNodeOptions = GetFullyEnabledListOptions(DictionaryNodeListOptions.ListIds.Complex),
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
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForFieldByReflection(lexentry, subentryNode, null, DefaultSettings);

			Assert.That(result.IndexOf("headwordA", StringComparison.InvariantCulture),
				Is.LessThan(result.IndexOf("headwordB", StringComparison.InvariantCulture)), "Subentries should be sorted by Type");

			// Reverse the order of the DictionaryNodeOptions, which should result in the data being ordered differently.
			((DictionaryNodeListOptions)subentryNode.DictionaryNodeOptions).Options.Reverse();

			// SUT2
			result = ConfiguredXHTMLGenerator.GenerateXHTMLForFieldByReflection(lexentry, subentryNode, null, DefaultSettings);

			Assert.That(result.IndexOf("headwordB", StringComparison.InvariantCulture),
				Is.LessThan(result.IndexOf("headwordA", StringComparison.InvariantCulture)), "Subentries should be sorted by Type");
		}

		// <summary>
		/// LT-18171:Crash displaying entry or doing xhtml export
		/// </summary>
		[Test]
		public void GenerateXHTMLForFieldByReflection_NullOrEmptyMediaFilePathDoesNotCrash()
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
				DictionaryNodeOptions = GetFullyEnabledListOptions(DictionaryNodeListOptions.ListIds.Variant),
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
			CreateVariantForm(Cache, entry, variant, "Spelling Variant"); // we need a real Variant Type to pass the list options test
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
			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null);

			//SUT
			Assert.DoesNotThrow(() => ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entry, mainEntryNode, null, settings), "Invalid filename in CmFile should not lead to crash");
		}

		[Test]
		public void GenerateAdjustedPageNumbers_NoAdjacentWhenUpButtonConsumesAllEntries()
		{
			var firstEntry = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(firstEntry, "homme", _wsFr);

			var secondEntry = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(secondEntry, "femme", _wsFr);

			var thirdEntry = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(thirdEntry, "famille", _wsFr);
			var currentPage = new Tuple<int, int>(0, 2);
			var adjacentPage = new Tuple<int, int>(2, 2);
			Tuple<int, int> current;
			Tuple<int, int> adjacent;
			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, "");
			// SUT
			ConfiguredXHTMLGenerator.GenerateAdjustedPageButtons(new[] { firstEntry.Hvo, secondEntry.Hvo, thirdEntry.Hvo }, settings, currentPage, adjacentPage, 2,
				out current, out adjacent);
			Assert.IsNull(adjacent, "The Adjacent page should have been consumed into the current page");
			Assert.AreEqual(0, current.Item1, "Current page should start at 0");
			Assert.AreEqual(2, current.Item2, "Current page should end at 2");
		}

		[Test]
		public void GenerateAdjustedPageNumbers_NoAdjacentWhenDownButtonConsumesAllEntries()
		{
			var firstEntry = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(firstEntry, "homme", _wsFr);

			var secondEntry = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(secondEntry, "femme", _wsFr);

			var thirdEntry = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(thirdEntry, "famille", _wsFr);
			var currentPage = new Tuple<int, int>(1, 2);
			var adjPage = new Tuple<int, int>(0, 1);
			Tuple<int, int> current;
			Tuple<int, int> adjacent;
			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, "");
			// SUT
			ConfiguredXHTMLGenerator.GenerateAdjustedPageButtons(new[] { firstEntry.Hvo, secondEntry.Hvo, thirdEntry.Hvo }, settings, currentPage, adjPage, 2,
				out current, out adjacent);
			Assert.IsNull(adjacent, "The Adjacent page should have been consumed into the current page");
			Assert.AreEqual(0, current.Item1, "Current page should start at 0");
			Assert.AreEqual(2, current.Item2, "Current page should end at 2");
		}

		[Test]
		public void GenerateAdjustedPageNumbers_AdjacentAndCurrentPageAdjustCorrectlyUp()
		{
			var firstEntry = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(firstEntry, "homme", _wsFr);

			var secondEntry = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(secondEntry, "femme", _wsFr);

			var thirdEntry = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(thirdEntry, "famille", _wsFr);

			var fourthEntry = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(thirdEntry, "famille", _wsFr);

			var currentPage = new Tuple<int, int>(0, 2);
			var adjPage = new Tuple<int, int>(3, 4);
			Tuple<int, int> current;
			Tuple<int, int> adjacent;
			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, string.Empty);
			// SUT
			ConfiguredXHTMLGenerator.GenerateAdjustedPageButtons(new[] { firstEntry.Hvo, secondEntry.Hvo, thirdEntry.Hvo, fourthEntry.Hvo }, settings, currentPage, adjPage, 1,
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
			AddHeadwordToEntry(firstEntry, "homme", _wsFr);

			var secondEntry = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(secondEntry, "femme", _wsFr);

			var thirdEntry = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(thirdEntry, "famille", _wsFr);

			var fourthEntry = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(thirdEntry, "famille", _wsFr);

			var adjPage = new Tuple<int, int>(0, 2);
			var currentPage = new Tuple<int, int>(3, 4);
			Tuple<int, int> current;
			Tuple<int, int> adjacent;
			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, string.Empty);
			// SUT
			ConfiguredXHTMLGenerator.GenerateAdjustedPageButtons(new[] { firstEntry.Hvo, secondEntry.Hvo, thirdEntry.Hvo, fourthEntry.Hvo }, settings, currentPage, adjPage, 1,
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
			AddHeadwordToEntry(firstEntry, "homme", _wsFr);

			var secondEntry = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(secondEntry, "femme", _wsFr);

			var thirdEntry = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(thirdEntry, "famille", _wsFr);

			var fourthEntry = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(fourthEntry, "familliar", _wsFr);

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
				var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, string.Empty);

				// SUT
				var entries = ConfiguredXHTMLGenerator.GenerateNextFewEntries(pubEverything, new[] { firstEntry.Hvo, secondEntry.Hvo, thirdEntry.Hvo, fourthEntry.Hvo }, configPath,
					settings, currentPage, adjPage, 1, out current, out adjacent);
				Assert.AreEqual(1, entries.Count, "No entries generated");
				Assert.That(entries[0], Is.StringContaining(thirdEntry.HeadWord.Text));
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
			AddHeadwordToEntry(firstEntry, "homme", _wsFr);

			var secondEntry = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(secondEntry, "femme", _wsFr);

			var thirdEntry = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(thirdEntry, "famille", _wsFr);

			var fourthEntry = CreateInterestingLexEntry(Cache);
			AddHeadwordToEntry(fourthEntry, "familliar", _wsFr);

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
				var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, string.Empty);

				// SUT
				var entries = ConfiguredXHTMLGenerator.GenerateNextFewEntries(pubEverything, new[] { firstEntry.Hvo, secondEntry.Hvo, thirdEntry.Hvo, fourthEntry.Hvo }, configPath,
					settings, currentPage, adjPage, 2, out current, out adjacent);
				Assert.AreEqual(2, entries.Count, "Not enough entries generated");
				Assert.That(entries[0], Is.StringContaining(thirdEntry.HeadWord.Text));
				Assert.That(entries[1], Is.StringContaining(fourthEntry.HeadWord.Text));
				Assert.IsNull(adjacent);
			}
			finally
			{
				File.Delete(model.FilePath);
			}
		}

		[Test]
		public void GenerateXHTMLForEntry_GroupingNodeGeneratesSpanAndInnerContentWorks()
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
				Children = new List<ConfigurableDictionaryNode> {sensesNode},
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
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, null, DefaultSettings);

			const string oneSenseWithGlossOfGloss = "/div[@class='lexentry']/span[@class='grouping_sensegroup']"
				+ "/span[@class='senses']/span[@class='sense']//span[@lang='en' and text()='gloss']";
			//This assert is dependent on the specific entry data created in CreateInterestingLexEntry
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(oneSenseWithGlossOfGloss, 1);
		}

		[Test]
		public void GenerateXHTMLForEntry_GeneratesNFC()
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
			var result = ConfiguredXHTMLGenerator.GenerateXHTMLForEntry(entry, node, null, DefaultSettings);
			var tsResult = TsStringUtils.MakeString(result, Cache.DefaultAnalWs);
			Assert.False(TsStringUtils.IsNullOrEmpty(tsResult), "Results should have been generated");
			Assert.That(tsResult.get_IsNormalizedForm(FwNormalizationMode.knmNFC), "Resulting XHTML should be NFComposed");
		}
		private static void DeleteTempXhtmlAndCssFiles(string xhtmlPath)
		{
			if (string.IsNullOrEmpty(xhtmlPath))
				return;
			File.Delete(xhtmlPath);
			File.Delete(Path.ChangeExtension(xhtmlPath, "css"));
			var xhtmlDir = Path.GetDirectoryName(xhtmlPath);
			if (string.IsNullOrEmpty(xhtmlDir))
				return;
			File.Delete(Path.Combine(xhtmlDir, "ProjectDictionaryOverrides.css"));
			File.Delete(Path.Combine(xhtmlDir, "ProjectReversalOverrides.css"));
		}

		/// <summary>
		/// Creates a DictionaryConfigurationModel with one Main and one of each neeeded Minor Entry nodes, all with enabled HeadWord children
		/// </summary>
		internal static DictionaryConfigurationModel CreateInterestingConfigurationModel(LcmCache cache, IPropertyTable propertyTable, ConfigType configType = ConfigType.Root)
		{
			var mainHeadwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "HeadWord",
				CSSClassNameOverride = "entry",
				DictionaryNodeOptions = GetWsOptionsForLanguages(new[] { "fr" }),
				Before = "MainEntry: ",
			};
			var subEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { mainHeadwordNode },
				FieldDescription = "Subentries"
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { mainHeadwordNode },
				FieldDescription = "LexEntry"
			};
			if (configType == ConfigType.Hybrid || configType == ConfigType.Root)
				mainEntryNode.Children.Add(subEntryNode);
			if (configType == ConfigType.Hybrid || configType == ConfigType.Lexeme)
				mainEntryNode.DictionaryNodeOptions = GetFullyEnabledListOptions(cache, DictionaryNodeListOptions.ListIds.Complex);

			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);

			var minorEntryNode = mainEntryNode.DeepCloneUnderSameParent();
			minorEntryNode.CSSClassNameOverride = "minorentry";
			minorEntryNode.Before = "MinorEntry: ";
			minorEntryNode.DictionaryNodeOptions = GetFullyEnabledListOptions(cache, DictionaryNodeListOptions.ListIds.Complex);

			var minorSecondNode = minorEntryNode.DeepCloneUnderSameParent();
			minorSecondNode.Before = "HalfStep: ";
			minorSecondNode.DictionaryNodeOptions = GetFullyEnabledListOptions(cache, DictionaryNodeListOptions.ListIds.Variant);

			// Needs "TestData" inserted in path name
			var worksBaseDir = DictionaryConfigurationServices.GetProjectConfigurationDirectory(propertyTable);
			var pathParts = worksBaseDir.Split(new[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries).ToList();
			int idx;
			for (idx = 0; idx < pathParts.Count; ++idx)
			{
				var pathPart = pathParts[idx];
				if (pathPart == "ConfigurationSettings")
				{
					break;
				}
			}
			pathParts.Insert(idx, "TestData");
			pathParts.Add("filename" + LanguageExplorerConstants.DictionaryConfigurationFileExtension);
			var model = new DictionaryConfigurationModel
			{
				AllPublications = true,
				Parts = new List<ConfigurableDictionaryNode> { mainEntryNode, minorEntryNode, minorSecondNode },
				FilePath = propertyTable == null ? null : Path.Combine(pathParts.ToArray()),
				IsRootBased = configType == ConfigType.Root
			};

			if (configType != ConfigType.Root)
				model.Parts.Remove(minorEntryNode);

			return model;
		}

		private static ConfigurableDictionaryNode CreatePictureModel()
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
			var sensesNode = new ConfigurableDictionaryNode { FieldDescription = "Senses" };
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { sensesNode, pictureNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			return mainEntryNode;
		}

		/// <summary>
		/// Creates an ILexEntry object, optionally with specified headword and gloss
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="headword">Optional: defaults to 'Citation'</param>
		/// <param name="gloss">Optional: defaults to 'gloss'</param>
		/// <returns></returns>
		internal static ILexEntry CreateInterestingLexEntry(LcmCache cache, string headword = "Citation", string gloss = "gloss")
		{
			var entryFactory = cache.ServiceLocator.GetInstance<ILexEntryFactory>();
			var entry = entryFactory.Create();
			var wsEn = EnsureWritingSystemSetup(cache, "en", false);
			var wsFr = EnsureWritingSystemSetup(cache, "fr", true);
			AddHeadwordToEntry(entry, headword, wsFr);
			entry.Comment.set_String(wsEn, TsStringUtils.MakeString("Comment", wsEn));
			AddSenseToEntry(entry, gloss, wsEn, cache);
			return entry;
		}

		private static int EnsureWritingSystemSetup(LcmCache cache, string wsStr, bool isVernacular)
		{
			var wsFact = cache.WritingSystemFactory;
			var result = wsFact.GetWsFromStr(wsStr);
			if (result < 1)
			{
				if (isVernacular)
				{
					cache.LangProject.AddToCurrentVernacularWritingSystems(cache.WritingSystemFactory.get_Engine(wsStr) as CoreWritingSystemDefinition);
				}
				else
				{
					cache.LangProject.AddToCurrentAnalysisWritingSystems(cache.WritingSystemFactory.get_Engine(wsStr) as CoreWritingSystemDefinition);
				}
			}
			return wsFact.GetWsFromStr(wsStr);
		}

		/// <summary>
		/// Creates an ILexEntry object, optionally with specified headword and gloss
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="headword">Optional: defaults to 'Citation'</param>
		/// <param name="gloss">Optional: defaults to 'gloss'</param>
		/// <returns></returns>
		internal static ILexEntry CreateInterestingSuffix(LcmCache cache, string headword = "ba", string gloss = "gloss")
		{
			var entry = CreateInterestingLexEntry(cache, headword, gloss);
			var wsEn = cache.WritingSystemFactory.GetWsFromStr("en");
			var suffixType = cache.LangProject.LexDbOA.MorphTypesOA.FindOrCreatePossibility("suffix", wsEn);
			entry.LexemeFormOA = cache.ServiceLocator.GetInstance<IMoAffixAllomorphFactory>().Create();
			entry.LexemeFormOA.MorphTypeRA = suffixType as IMoMorphType;
			return entry;
		}

		/// <summary>
		/// Use reflection to set the guid on a variant form. May not work for all kinds of tests or appropriately be editing the database.
		/// Because changing the Guid causes teardown problem, it must be reset prior to teardown (hence the Disposable <returns/>)
		/// </summary>
		internal static TempGuidOn<ILexEntryRef> CreateVariantForm(LcmCache cache, IVariantComponentLexeme main, ILexEntry variantForm, Guid guid,
			string type = TestVariantName)
		{
			return new TempGuidOn<ILexEntryRef>(CreateVariantForm(cache, main, variantForm, type), guid);
		}

		/// <summary>
		/// 'internal static' so Reversal tests can use it
		/// </summary>
		internal static ILexEntryRef CreateVariantForm(LcmCache cache, IVariantComponentLexeme main, ILexEntry variantForm, string type = TestVariantName)
		{
			var owningList = cache.LangProject.LexDbOA.VariantEntryTypesOA;
			Assert.IsNotNull(owningList, "No VariantEntryTypes property on Lexicon object.");
			var varType = owningList.ReallyReallyAllPossibilities.LastOrDefault(poss => poss.Name.AnalysisDefaultWritingSystem.Text == type) as ILexEntryType;
			if (varType == null && type != null) // if this type doesn't exist, create it
			{
				varType = cache.ServiceLocator.GetInstance<ILexEntryTypeFactory>().Create();
				owningList.PossibilitiesOS.Add(varType);
				varType.Name.set_String(cache.DefaultAnalWs, type);
			}
			var refOut = variantForm.MakeVariantOf(main, varType);
			// ILexEntry.MakeVariantOf sets a Type even if null is specified. But we want to test typeless variants, so clear them if null is specified.
			if (type == null)
				refOut.VariantEntryTypesRS.Clear();
			return refOut;
		}

		/// <summary>
		/// Use reflection to set the guid on a complex form. May not work for all kinds of tests or appropriately be editing the database.
		/// Because changing the Guid causes teardown problem, it must be reset prior to teardown (hence the Disposable <returns/>)
		/// </summary>
		internal static TempGuidOn<ILexEntryRef> CreateComplexForm(LcmCache cache, IVariantComponentLexeme main, ILexEntry complexForm, Guid guid,
			bool subentry)
		{
			return new TempGuidOn<ILexEntryRef>(CreateComplexForm(cache, main, complexForm, subentry), guid);
		}

		internal static ILexEntryRef CreateComplexForm(LcmCache cache, ICmObject main, ILexEntry complexForm, bool subentry, byte complexFormTypeIndex = 1)
		{
			return CreateComplexForm(cache, main, complexForm, subentry,
				(ILexEntryType)cache.LangProject.LexDbOA.ComplexEntryTypesOA.PossibilitiesOS[complexFormTypeIndex]);
		}

		private static ILexEntryRef CreateComplexForm(LcmCache cache, ICmObject main, ILexEntry complexForm, bool subentry, Guid typeGuid)
		{
			return CreateComplexForm(cache, main, complexForm, subentry,
				(ILexEntryType)cache.LangProject.LexDbOA.ComplexEntryTypesOA.PossibilitiesOS.First(x => x.Guid == typeGuid));
		}

		private static ILexEntryRef CreateComplexForm(LcmCache cache, ICmObject main, ILexEntry complexForm, bool subentry, ILexEntryType complexEntryType)
		{
			var complexEntryRef = cache.ServiceLocator.GetInstance<ILexEntryRefFactory>().Create();
			complexForm.EntryRefsOS.Add(complexEntryRef);
			var complexEntryTypeAbbrText = complexEntryType.Abbreviation.BestAnalysisAlternative.Text;
			var complexEntryTypeRevAbbr = complexEntryType.ReverseAbbr;
			// If there is no reverseAbbr, generate one from the forward abbr (e.g. "comp. of") by trimming the trailing " of"
			if (complexEntryTypeRevAbbr.BestAnalysisAlternative.Equals(complexEntryTypeRevAbbr.NotFoundTss))
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
			var lrt = CreateLexRefType(LexRefTypeTags.MappingTypes.kmtEntryOrSenseSequence, refTypeName, "", refTypeReverseName, "");
			if (!string.IsNullOrEmpty(refTypeReverseName))
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

		private ILexRefType CreateLexRefType(LexRefTypeTags.MappingTypes type, string name, string abbr, string revName, string revAbbr)
		{
			if (Cache.LangProject.LexDbOA.ReferencesOA == null)
			{
				Cache.LangProject.LexDbOA.ReferencesOA = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
			}
			var referencePossibilities = Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS;
			if (referencePossibilities.Any(r => r.Name.BestAnalysisAlternative.Text == name))
			{
				return referencePossibilities.First(r => r.Name.BestAnalysisAlternative.Text == name) as ILexRefType;
			}
			var lrt = Cache.ServiceLocator.GetInstance<ILexRefTypeFactory>().Create();
			referencePossibilities.Add(lrt);
			lrt.MappingType = (int)type;
			lrt.Name.set_String(_wsEn, name);
			lrt.Abbreviation.set_String(_wsEn, abbr);
			if (!string.IsNullOrEmpty(revName))
				lrt.ReverseName.set_String(_wsEn, revName);
			if (!string.IsNullOrEmpty(revAbbr))
				lrt.ReverseAbbreviation.set_String(_wsEn, revAbbr);
			return lrt;
		}

		private void CreateLexReference(ILexRefType lrt, IEnumerable<ICmObject> sensesAndEntries)
		{
			CreateLexReference(lrt, sensesAndEntries, Guid.Empty);
		}

		private void CreateLexReference(ILexRefType lrt, IEnumerable<ICmObject> sensesAndEntries, Guid lexRefGuid)
		{
			var lexRef = Cache.ServiceLocator.GetInstance<ILexReferenceFactory>().Create(lexRefGuid, lrt);
			foreach (var senseOrEntry in sensesAndEntries)
				lexRef.TargetsRS.Add(senseOrEntry);
		}

		private ICmPossibility CreatePublicationType(string name)
		{
			return DictionaryConfigurationImportController.AddPublicationType(name, Cache);
		}

		private static void AddHeadwordToEntry(ILexEntry entry, string headword, int wsId)
		{
			// The headword field is special: it uses Citation if available, or LexemeForm if Citation isn't filled in
			entry.CitationForm.set_String(wsId, TsStringUtils.MakeString(headword, wsId));
		}

		private static ILexPronunciation AddPronunciationToEntry(ILexEntry entry, string content, int wsId, LcmCache cache)
		{
			var pronunciation = cache.ServiceLocator.GetInstance<ILexPronunciationFactory>().Create();
			entry.PronunciationsOS.Add(pronunciation);
			pronunciation.Form.set_String(wsId, TsStringUtils.MakeString(content, wsId));
			return pronunciation;
		}

		private static void AddSenseToEntry(ILexEntry entry, string gloss, int wsId, LcmCache cache)
		{
			var senseFactory = cache.ServiceLocator.GetInstance<ILexSenseFactory>();
			var sense = senseFactory.Create();
			entry.SensesOS.Add(sense);
			if (!string.IsNullOrEmpty(gloss))
				sense.Gloss.set_String(wsId, TsStringUtils.MakeString(gloss, wsId));
		}

		private void AddSenseAndTwoSubsensesToEntry(ICmObject entryOrSense, string gloss)
		{
			var senseFactory = Cache.ServiceLocator.GetInstance<ILexSenseFactory>();
			var sense = senseFactory.Create();
			var entry = entryOrSense as ILexEntry;
			if (entry != null)
				entry.SensesOS.Add(sense);
			else
				((ILexSense)entryOrSense).SensesOS.Add(sense);
			sense.Gloss.set_String(_wsEn, TsStringUtils.MakeString(gloss, _wsEn));
			var subSensesOne = senseFactory.Create();
			sense.SensesOS.Add(subSensesOne);
			subSensesOne.Gloss.set_String(_wsEn, TsStringUtils.MakeString(gloss + "2.1", _wsEn));
			var subSensesTwo = senseFactory.Create();
			sense.SensesOS.Add(subSensesTwo);
			subSensesTwo.Gloss.set_String(_wsEn, TsStringUtils.MakeString(gloss + "2.2", _wsEn));
		}

		private void AddSingleSubSenseToSense(string gloss, ILexSense sense)
		{
			sense.Gloss.set_String(_wsEn, TsStringUtils.MakeString(gloss, _wsEn));
			AddSubSenseToSense(gloss + "1.1", sense);
		}

		private void AddSubSenseToSense(string gloss, ILexSense sense)
		{
			var subSensesOne = sense.Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			sense.SensesOS.Add(subSensesOne);
			subSensesOne.Gloss.set_String(_wsEn, TsStringUtils.MakeString(gloss, _wsEn));
		}

		private ILexExampleSentence AddExampleToSense(ILexSense sense, string content, string translation = null)
		{
			var exampleFact = Cache.ServiceLocator.GetInstance<ILexExampleSentenceFactory>();
			var example = exampleFact.Create(new Guid(), sense);
			example.Example.set_String(_wsFr, TsStringUtils.MakeString(content, _wsFr));
			if (translation != null)
			{
				var type = Cache.ServiceLocator.GetInstance<ICmPossibilityRepository>().GetObject(CmPossibilityTags.kguidTranFreeTranslation);
				var cmTranslation = Cache.ServiceLocator.GetInstance<ICmTranslationFactory>().Create(example, type);
				cmTranslation.Translation.set_String(_wsEn, TsStringUtils.MakeString(translation, _wsEn));
				example.TranslationsOC.Add(cmTranslation);
			}
			return example;
		}

		private IMoForm AddAllomorphToEntry(ILexEntry entry)
		{
			var morphFact = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>();
			var morph = morphFact.Create();
			entry.AlternateFormsOS.Add(morph);
			morph.Form.set_String(_wsFr, TsStringUtils.MakeString("Allomorph", _wsFr));

			// add environment to the allomorph
			const int stringRepresentationFlid = 5097008;
			var env = Cache.ServiceLocator.GetInstance<IPhEnvironmentFactory>().Create();
			Cache.LangProject.PhonologicalDataOA.EnvironmentsOS.Add(env);
			morph.PhoneEnvRC.Add(env);
			Cache.MainCacheAccessor.SetString(env.Hvo, stringRepresentationFlid, TsStringUtils.MakeString("phoneyEnv", _wsEn));

			return morph;
		}

		private static IStText CreateMultiParaText(string content, LcmCache cache)
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

		private static ITsString MakeVernTss(string content, LcmCache cache)
		{
			return TsStringUtils.MakeString(content, cache.DefaultVernWs);
		}

		private ITsString MakeMulitlingualTss(IEnumerable<string> content)
		{
			// automatically alternates runs between 'en' and 'fr'
			var tsFact = TsStringUtils.TsStrFactory;
			var lastWs = _wsFr;
			var builder = tsFact.GetIncBldr();
			foreach (var runContent in content)
			{
				lastWs = lastWs == _wsEn ? _wsFr : _wsEn; // switch ws for each run
				builder.AppendTsString(TsStringUtils.MakeString(runContent, lastWs));
			}
			return builder.GetString();
		}

		private ITsString MakeBidirectionalTss(IEnumerable<string> content)
		{
			EnsureHebrewExists();
			// automatically alternates runs between 'en' and 'he' (Hebrew)
			var tsFact = TsStringUtils.TsStrFactory;
			var lastWs = _wsEn;
			var builder = tsFact.GetIncBldr();
			foreach (var runContent in content)
			{
				lastWs = lastWs == _wsEn ? _wsHe : _wsEn; // switch ws for each run
				builder.AppendTsString(tsFact.MakeString(runContent, lastWs));
			}
			return builder.GetString();
		}

		private void EnsureHebrewExists()
		{
			if (_wsHe > 0)
				return;
			var wsManager = Cache.ServiceLocator.WritingSystemManager;
			CoreWritingSystemDefinition hebrew;
			wsManager.GetOrSet("he", out hebrew);
			hebrew.RightToLeftScript = true;
			_wsHe = hebrew.Handle;
		}

		private void SetDictionaryNormalDirection(InheritableStyleProp<TriStateBool> rightToLeft)
		{
			ReflectionHelper.SetField(DictionaryNormalStyle, "m_rtl", rightToLeft);
		}

		internal static void SetPublishAsMinorEntry(ILexEntry entry, bool publish)
		{
			foreach (var ler in entry.EntryRefsOS)
				ler.HideMinorEntry = publish ? 0 : 1;
		}

		public static DictionaryNodeOptions GetSenseNodeOptions()
		{
			return new DictionaryNodeSenseOptions { DisplayEachSenseInAParagraph = false };
		}

		public static DictionaryNodeOptions GetWsOptionsForLanguages(string[] languages)
		{
			return new DictionaryNodeWritingSystemOptions { Options = DictionaryDetailsControllerTests.ListOfEnabledDNOsFromStrings(languages) };
		}

		public static DictionaryNodeOptions GetWsOptionsForLanguages(string[] languages, DictionaryNodeWritingSystemOptions.WritingSystemType type)
		{
			return new DictionaryNodeWritingSystemOptions
			{
				Options = DictionaryDetailsControllerTests.ListOfEnabledDNOsFromStrings(languages),
				WsType = type
			};
		}

		public static DictionaryNodeOptions GetWsOptionsForLanguageswithDisplayWsAbbrev(string[] languages,
			DictionaryNodeWritingSystemOptions.WritingSystemType type = 0)
		{
			return new DictionaryNodeWritingSystemOptions
			{
				Options = DictionaryDetailsControllerTests.ListOfEnabledDNOsFromStrings(languages),
				DisplayWritingSystemAbbreviations = true,
				WsType = type
			};
		}

		public static DictionaryNodeOptions GetListOptionsForItems(DictionaryNodeListOptions.ListIds listName, ICmPossibility[] checkedItems)
		{
			var listOptions = new DictionaryNodeListOptions
			{
				ListId = listName,
				Options = DictionaryDetailsControllerTests.ListOfEnabledDNOsFromStrings(checkedItems.Select(id => id.Guid.ToString()).ToList())
			};
			return listOptions;
		}

		public static DictionaryNodeOptions GetListOptionsForStrings(DictionaryNodeListOptions.ListIds listName, IEnumerable<string> checkedItems)
		{
			var listOptions = new DictionaryNodeListOptions
			{
				ListId = listName,
				Options = DictionaryDetailsControllerTests.ListOfEnabledDNOsFromStrings(checkedItems)
			};
			return listOptions;
		}

		public DictionaryNodeOptions GetFullyEnabledListOptions(DictionaryNodeListOptions.ListIds listName)
		{
			return GetFullyEnabledListOptions(Cache, listName);
		}

		public static DictionaryNodeOptions GetFullyEnabledListOptions(LcmCache cache, DictionaryNodeListOptions.ListIds listName)
		{
			List<DictionaryNodeListOptions.DictionaryNodeOption> dnoList;
			var useParaOptions = false;
			switch (listName)
			{
				case DictionaryNodeListOptions.ListIds.Minor:
					dnoList = DictionaryDetailsControllerTests.ListOfEnabledDNOsFromStrings(
						new[] { XmlViewsUtils.GetGuidForUnspecifiedVariantType(), XmlViewsUtils.GetGuidForUnspecifiedComplexFormType() }
							.Select(guid => guid.ToString())
						.Union(cache.LangProject.LexDbOA.ComplexEntryTypesOA.PossibilitiesOS
						.Union(cache.LangProject.LexDbOA.VariantEntryTypesOA.PossibilitiesOS).Select(item => item.Guid.ToString())));
					break;
				case DictionaryNodeListOptions.ListIds.Variant:
					dnoList = DictionaryDetailsControllerTests.ListOfEnabledDNOsFromStrings(
						new[] { XmlViewsUtils.GetGuidForUnspecifiedVariantType().ToString() }
						.Union(cache.LangProject.LexDbOA.VariantEntryTypesOA.PossibilitiesOS.Select(item => item.Guid.ToString())));
					break;
				case DictionaryNodeListOptions.ListIds.Complex:
					useParaOptions = true;
					dnoList = DictionaryDetailsControllerTests.ListOfEnabledDNOsFromStrings(
						new[] { XmlViewsUtils.GetGuidForUnspecifiedComplexFormType().ToString() }
						.Union(cache.LangProject.LexDbOA.ComplexEntryTypesOA.PossibilitiesOS.Select(item => item.Guid.ToString())));
					break;
				case DictionaryNodeListOptions.ListIds.Note:
					useParaOptions = true;
					dnoList = DictionaryDetailsControllerTests.ListOfEnabledDNOsFromStrings(
						new[] { XmlViewsUtils.GetGuidForUnspecifiedExtendedNoteType().ToString() }
						.Union(cache.LangProject.LexDbOA.ExtendedNoteTypesOA.PossibilitiesOS.Select(item => item.Guid.ToString())));
					break;
				default:
					throw new NotImplementedException(string.Format("Unknown list id {0}", listName));
			}

			DictionaryNodeListOptions listOptions = useParaOptions ? new DictionaryNodeListAndParaOptions() : new DictionaryNodeListOptions();

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

		public IPartOfSpeech CreatePartOfSpeech(string name, string abbr)
		{
			var posSeq = Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS;
			var pos = Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>().Create();
			posSeq.Add(pos);
			pos.Name.set_String(_wsEn, name);
			pos.Abbreviation.set_String(_wsEn, abbr);
			return pos;
		}

		// ReSharper disable once InconsistentNaming
		public IMoMorphSynAnalysis CreateMSA(ILexEntry entry, IPartOfSpeech pos)
		{
			var msa = Cache.ServiceLocator.GetInstance<IMoStemMsaFactory>().Create();
			entry.MorphoSyntaxAnalysesOC.Add(msa);
			msa.PartOfSpeechRA = pos;
			return msa;
		}
	}
}