// Copyright (c) 2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using System.Linq;
using System.Security;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using FwBuildTasks;
using NUnit.Framework;
using SIL.FieldWorks.Build.Tasks.Localization;
using SIL.TestUtilities;

namespace SIL.FieldWorks.Build.Tasks.FwBuildTasksTests
{
	[TestFixture]
	internal class LocalizeListsTests
	{
		[Test]
		public void ConvertListToXliff_FileNameSavedToOriginalAttribute()
		{
			var testName = "TestFile.xml";
			var xliffDoc = LocalizeLists.ConvertListToXliff(testName,
				XDocument.Parse(
					"<Lists><List owner=\"LexDb\" field=\"DomainTypes\" itemClass=\"CmPossibility\"/></Lists>"));
			AssertThatXmlIn.String(xliffDoc.ToString())
				.HasSpecifiedNumberOfMatchesForXpath($"/xliff/file[@original='{testName}']", 1);
		}

		[Test]
		public void ConvertListToXliff_NameTransUnitSourceCreated()
		{
			var owner = "LexDb";
			var field = "DomainTypes";

			var xliffDoc = LocalizeLists.ConvertListToXliff("test.xml", XDocument.Parse(
				$@"<Lists>
					<List owner='{owner}' field='{field}' itemClass='CmPossibility'>
						<Name><AUni ws='en'>Academic Domains</AUni></Name>
					</List>
				</Lists>"));
			var group = owner + "_" + field;
			AssertThatXmlIn.String(xliffDoc.ToString()).HasSpecifiedNumberOfMatchesForXpath(
				$"/xliff/file/body/group[@id='{group}']/trans-unit[@id='{group}_Name']/source[text()='Academic Domains']", 1, true);
		}

		[Test]
		public void SplitSourceLists_InvalidXmlThrows()
		{
			var notListsXml = "<notlists><somethingElse/></notlists>";
			using (var stringReader = new StringReader(notListsXml))
			using (var xmlReader = new XmlTextReader(stringReader))
			{
				var message = Assert.Throws<ArgumentException>(() =>
					LocalizeLists.SplitLists(xmlReader, Path.GetTempPath())).Message;
				StringAssert.Contains("Source file is not in the expected format", message);
			}
		}

		[Test]
		public void SplitSourceLists_MissingListsThrows()
		{
			var oneListXml = "<Lists><List/></Lists>";
			using (var stringReader = new StringReader(oneListXml))
			using (var xmlReader = new XmlTextReader(stringReader))
			{
				var message = Assert.Throws<ArgumentException>(() =>
					LocalizeLists.SplitLists(xmlReader, Path.GetTempPath())).Message;
				StringAssert.Contains("Source file has an unexpected list count.", message);
			}
		}

		[Test]
		public void SplitSourceLists_MissingSourceFileThrows()
		{
			var message = Assert.Throws<ArgumentException>(() =>
					LocalizeLists.SplitSourceLists(Path.GetRandomFileName(), Path.GetTempPath()))
				.Message;
			StringAssert.Contains("The source file does not exist", message);
		}

		[Test]
		public void ConvertListToXliff_AbbrevAndDescTransUnitsCreated()
		{
			const string listXml = @"<Lists><List owner='LangProject' field='AnthroList' itemClass='CmAnthroItem'>
				<Name>
					<AUni ws='en'>Anthropology Categories</AUni>
					<AUni ws='en-GB'></AUni>
				</Name>
				<Abbreviation>
					<AUni ws='en'>OCM</AUni>
					<AUni ws='en-GB'></AUni>
				</Abbreviation>
				<Description>
					<AStr ws='en'>
					<Run ws='en'>This list contains both Framework for Research and Methodology in Ethnography (FRAME) and the Outline of Cultural Materials (OCM) as well as some expanded lower-level categories.</Run>
					</AStr>
					<AStr ws='en-GB'>
					<Run ws='en-GB'></Run>
					</AStr>
				</Description>
				</List></Lists>";

			var xliffDoc = LocalizeLists.ConvertListToXliff("test.xml", XDocument.Parse(listXml));
			// Test for abbreviation trans-unit source
			AssertThatXmlIn.String(xliffDoc.ToString()).HasSpecifiedNumberOfMatchesForXpath(
				"//group[@id='LangProject_AnthroList']/trans-unit[@id='LangProject_AnthroList_Abbr']/source", 1, true);
			// Test for description trans-unit source
			AssertThatXmlIn.String(xliffDoc.ToString()).HasSpecifiedNumberOfMatchesForXpath(
					"//group/group[@id='LangProject_AnthroList_Desc']/trans-unit[@id='LangProject_AnthroList_Desc_0']/source", 1, true);
		}

		[Test]
		public void ConvertListToXliff_PossibilityCreated()
		{
			const string listXml = @"<Lists><List owner='LangProject' field='AnthroList' itemClass='CmAnthroItem'>
				<Possibilities>
					<CmAnthroItem>
					<Name>
						<AUni ws='en'>anatomy</AUni>
					</Name>
					<Abbreviation>
						<AUni ws='en'>Anat</AUni>
					</Abbreviation>
					</CmAnthroItem>
				</Possibilities>
			</List></Lists>";

			var xliffDoc = LocalizeLists.ConvertListToXliff("test.xml", XDocument.Parse(listXml));
			// This xpath matches the first possibility in the possibilities group
			var xpathToPossGroup = "//group[@id='LangProject_AnthroList']/group[@id='LangProject_AnthroList_Poss']/group[@id='LangProject_AnthroList_Poss_0']";
			// Test for abbreviation trans-unit source
			AssertThatXmlIn.String(xliffDoc.ToString()).HasSpecifiedNumberOfMatchesForXpath(xpathToPossGroup, 1, true);
			// Verify name
			AssertThatXmlIn.String(xliffDoc.ToString()).HasSpecifiedNumberOfMatchesForXpath(xpathToPossGroup + "/trans-unit/source[text()='anatomy']", 1, true);
			// Verify abbreviation
			AssertThatXmlIn.String(xliffDoc.ToString()).HasSpecifiedNumberOfMatchesForXpath(xpathToPossGroup + "/trans-unit/source[text()='Anat']", 1, true);
			// There should be no reverse name or abbreviation, since there weren't any to convert.
			AssertThatXmlIn.String(xliffDoc.ToString()).HasNoMatchForXpath(xpathToPossGroup + "/trans-unit[@id='LangProject_AnthroList_Poss_0_RevName']");
			AssertThatXmlIn.String(xliffDoc.ToString()).HasNoMatchForXpath(xpathToPossGroup + "/trans-unit[@id='LangProject_AnthroList_Poss_0_RevAbbr']");
		}

		[Test]
		public void ConvertListToXliff_SubPossibilityCreated()
		{
			const string listXml = @"<Lists><List owner='LangProject' field='AnthroList' itemClass='CmAnthroItem'>
				<Possibilities>
					<CmAnthroItem>
					<Name>
						<AUni ws='en'>anatomy</AUni>
					</Name>
					<Abbreviation>
						<AUni ws='en'>Anat</AUni>
					</Abbreviation>
						<SubPossibilities>
							<CmAnthroItem>
								<Name>
									<AUni ws='en'>cultural anthropology</AUni>
								</Name>
								<Abbreviation>
									<AUni ws='en'>Cult anthro</AUni>
								</Abbreviation>
							</CmAnthroItem>
							</SubPossibilities>
					</CmAnthroItem>
				</Possibilities>
			</List></Lists>";

			var xliffDoc = LocalizeLists.ConvertListToXliff("test.xml", XDocument.Parse(listXml));
			// This xpath matches the first sub possibility of the first possibility in the possibilities group
			var xpathToSubPosGroup = "//group[@id='LangProject_AnthroList']/group[@id='LangProject_AnthroList_Poss']/group/group[@id='LangProject_AnthroList_Poss_0_SubPos']";
			// Test for subpossibility
			AssertThatXmlIn.String(xliffDoc.ToString()).HasSpecifiedNumberOfMatchesForXpath(xpathToSubPosGroup, 1, true);
			// Verify name
			AssertThatXmlIn.String(xliffDoc.ToString()).HasSpecifiedNumberOfMatchesForXpath(xpathToSubPosGroup + "/group/trans-unit/source[text()='cultural anthropology']", 1, true);
			// Verify abbreviation
			AssertThatXmlIn.String(xliffDoc.ToString()).HasSpecifiedNumberOfMatchesForXpath(xpathToSubPosGroup + "/group/trans-unit/source[text()='Cult anthro']", 1, true);
		}

		/// <summary>
		/// Tests that ReverseName and ReverseAbbr of LexEntryTypes are converted to XLIFF
		/// </summary>
		[Test]
		public void ConvertListToXliff_ReverseNameAndAbbrTransUnitsCreated()
		{
			const string revName = "Dialectal Variant of";
			const string revAbbr = "dial. var. of";
			const string listXml = @"<Lists><List owner='LexDb' field='VariantEntryTypes' itemClass='LexEntryType'>
				<Possibilities>
					<LexEntryType guid='024b62c9-93b3-41a0-ab19-587a0030219a'>
						<Name>
							<AUni ws='en'>Dialectal Variant</AUni>
						</Name>
						<Abbreviation>
							<AUni ws='en'>dial. var.</AUni>
						</Abbreviation>
						<ReverseName>
							<AUni ws='en'>" + revName + @"</AUni>
						</ReverseName>
						<ReverseAbbr>
							<AUni ws='en'>" + revAbbr + @"</AUni>
						</ReverseAbbr>
					</LexEntryType>
				</Possibilities>
			</List></Lists>";

			var xliffDoc = LocalizeLists.ConvertListToXliff("test.xml", XDocument.Parse(listXml)).ToString();
			const string xpathToPossGroup = "//group[@id='LexDb_VariantEntryTypes']/group[@id='LexDb_VariantEntryTypes_Poss']/group";
			// Test for reverse name
			AssertThatXmlIn.String(xliffDoc).HasSpecifiedNumberOfMatchesForXpath(
				xpathToPossGroup + "/trans-unit[@id='LexDb_VariantEntryTypes_Poss_0_RevName']/source[text()='" + revName + "']", 1, true);
			// Test for reverse abbr
			AssertThatXmlIn.String(xliffDoc).HasSpecifiedNumberOfMatchesForXpath(
				xpathToPossGroup + "/trans-unit[@id='LexDb_VariantEntryTypes_Poss_0_RevAbbr']/source[text()='" + revAbbr + "']", 1, true);
		}

		/// <summary>
		/// Tests that ReverseName and ReverseAbbreviation of LexRefTypes are converted to XLIFF
		/// </summary>
		[Test]
		public void ConvertListToXliff_ReverseNameAndAbbrevTransUnitsCreated()
		{
			const string revName = "Whole";
			const string revAbbrev = "wh";
			const string listXml = @"<Lists><List owner='LexDb' field='References' itemClass='LexRefType'>
				<Possibilities>
					<LexRefType>
						<Name>
							<AUni ws='en'>Part</AUni>
						</Name>
						<ReverseName>
							<AUni ws='en'>" + revName + @"</AUni>
						</ReverseName>
						<ReverseAbbreviation>
							<AUni ws='en'>" + revAbbrev + @"</AUni>
						</ReverseAbbreviation>
					</LexRefType>
				</Possibilities>
			</List></Lists>";

			var xliffDoc = LocalizeLists.ConvertListToXliff("test.xml", XDocument.Parse(listXml)).ToString();
			const string xpathToPossGroup = "//group[@id='LexDb_References']/group[@id='LexDb_References_Poss']/group";
			// Test for reverse name
			AssertThatXmlIn.String(xliffDoc).HasSpecifiedNumberOfMatchesForXpath(
				xpathToPossGroup + "/trans-unit[@id='LexDb_References_Poss_0_RevName']/source[text()='" + revName+ "']", 1, true);
			// Test for reverse abbreviation
			AssertThatXmlIn.String(xliffDoc).HasSpecifiedNumberOfMatchesForXpath(
				xpathToPossGroup + "/trans-unit[@id='LexDb_References_Poss_0_RevAbbrev']/source[text()='" + revAbbrev + "']", 1, true);
		}

		[Test]
		public void ConvertListToXliff_SemanticDomainConvertedWithQuestions()
		{
			const string guid = "63403699-07c1-43f3-a47c-069d6e4316e5";
			const string listXml = @"<Lists><List owner='LangProject' field='SemanticDomainList' itemClass='CmSemanticDomain'>
				<Possibilities>
					<CmSemanticDomain guid='" + guid + @"'>
						<Name>
							<AUni ws='en'>Universe, creation</AUni>
						</Name>
						<Abbreviation>
							<AUni ws='en'>1</AUni>
						</Abbreviation>
						<Description>
							<AStr ws='en'>
								<Run ws='en'>Use this domain for general words referring to the physical universe. Some languages may not have a single word for the universe and may have to use a phrase such as 'rain, soil, and things of the sky' or 'sky, land, and water' or a descriptive phrase such as 'everything you can see' or 'everything that exists'.</Run>
							</AStr>
						</Description>
						<Questions>
							<CmDomainQ>
								<Question>
									<AUni ws='en'>(1) What words refer to everything we can see?</AUni>
								</Question>
								<ExampleWords>
									<AUni ws='en'>universe, creation, cosmos, heaven and earth, macrocosm, everything that exists</AUni>
								</ExampleWords>
								<ExampleSentences>
									<AStr ws='en'>
										<Run ws='en'>In the beginning God created &lt;the heavens and the earth&gt;.</Run>
									</AStr>
								</ExampleSentences>
							</CmDomainQ>
						</Questions>
					</CmSemanticDomain>
				</Possibilities>
			</List></Lists>";

			var xliffDoc = LocalizeLists.ConvertListToXliff("test.xml", XDocument.Parse(listXml));
			// Verify that the GUID for SemanticDomain makes it to the file (ignore sil namespace via ugly hack)
			AssertThatXmlIn.String(xliffDoc.ToString()).HasSpecifiedNumberOfMatchesForXpath("//group/*[local-name()='guid'][text()='" + guid + "']", 1);
			// This xpath matches the first semantic domain
			var xpathToPossGroup = "//group/group/group[@id='LangProject_SemanticDomainList_Poss_0']";
			// Verify questions group present
			AssertThatXmlIn.String(xliffDoc.ToString()).HasSpecifiedNumberOfMatchesForXpath(
				xpathToPossGroup + "/group[contains(@id, '_Poss_0_Qs')]", 1, true);
			// Verify first question present
			AssertThatXmlIn.String(xliffDoc.ToString()).HasSpecifiedNumberOfMatchesForXpath(
				xpathToPossGroup + "/group/group[contains(@id, '_Poss_0_Qs_0')]", 1, true);
			// Verify question trans-unit present
			AssertThatXmlIn.String(xliffDoc.ToString()).HasSpecifiedNumberOfMatchesForXpath(
				xpathToPossGroup + "/group/group/trans-unit[contains(@id, '_Poss_0_Qs_0_Q')]", 1, true);
			// Verify ExampleWords trans-unit present
			AssertThatXmlIn.String(xliffDoc.ToString()).HasSpecifiedNumberOfMatchesForXpath(
				xpathToPossGroup + "/group/group/trans-unit[contains(@id, '_Poss_0_Qs_0_EW')]", 1, true);
			// Verify ExampleSentence group present
			AssertThatXmlIn.String(xliffDoc.ToString()).HasSpecifiedNumberOfMatchesForXpath(
				xpathToPossGroup + "/group/group/group[contains(@id, '_Poss_0_Qs_0_ES')]", 1, true);
		}

		[Test]
		public void ConvertListToXliff_RunStylesCreated()
		{
			const string style = "Emphasized Text";
			const string styledText = "something";
			const string unstyledText = "else";
			const string listXml = @"<Lists>
				<List owner='LangProject' field='AnthroList' itemClass='CmAnthroItem'>
					<Description>
						<AStr ws='en'>
							<Run ws='en' namedStyle='" + style + @"'>" + styledText + @"</Run>
							<Run ws='en'>" + unstyledText + @"</Run>
						</AStr>
					</Description>
				</List></Lists>";
			var xmlDoc = XDocument.Parse(listXml);

			var xliff = LocalizeLists.ConvertListToXliff("test.xml", xmlDoc).ToString();
			// Test for description trans-unit source
			const string xpathToGroup = "//group/group[@id='LangProject_AnthroList_Desc']";
			AssertThatXmlIn.String(xliff).HasSpecifiedNumberOfMatchesForXpath(xpathToGroup + "/trans-unit", 2, true);
			// Test for content and styling
			AssertThatXmlIn.String(xliff).HasSpecifiedNumberOfMatchesForXpath(
				xpathToGroup + "/trans-unit[@id='LangProject_AnthroList_Desc_0']/*[local-name()='style'][text()='" + style + "']", 1, true);
			AssertThatXmlIn.String(xliff).HasSpecifiedNumberOfMatchesForXpath(
				xpathToGroup + "/trans-unit[@id='LangProject_AnthroList_Desc_0']/source[text()='" + styledText + "']", 1, true);
			AssertThatXmlIn.String(xliff).HasNoMatchForXpath(
				xpathToGroup + "/trans-unit[@id='LangProject_AnthroList_Desc_1']/*[local-name()='style'][text()='" + style + "']");
			AssertThatXmlIn.String(xliff).HasSpecifiedNumberOfMatchesForXpath(
				xpathToGroup + "/trans-unit[@id='LangProject_AnthroList_Desc_1']/source[text()='" + unstyledText + "']", 1, true);
		}

		[Test]
		public void ConvertXliffToLists_ListElementCreated()
		{
			var listsElement = XElement.Parse("<Lists/>");
			const string xliffXml = @"<xliff version='1.2' xmlns:sil='software.sil.org'>
				<file source-language='EN' datatype='plaintext' original='MiscLists.xlf' target-language='de'>
					<body>
						<group id='LangProject_ConfidenceLevels'/>
					</body></file></xliff>";
			LocalizeLists.ConvertXliffToLists(XDocument.Parse(xliffXml), listsElement);
			AssertThatXmlIn.String(listsElement.ToString()).HasSpecifiedNumberOfMatchesForXpath("/Lists/List[@owner='LangProject' and @field='ConfidenceLevels']", 1);
		}

		[TestCase("de", "de")]
		[TestCase("es-ES", "es")]
		[TestCase("zh-CN", "zh-CN")]
		public void ConvertXliffToLists_ListNameAbbrevAndDescCreated_WithNormalizedTargetLanguage(string targLangOrig, string targLangNormalized)
		{
			const string englishName = "Genres";
			const string targetName = "Géneros";
			const string englishDescription = "DescriptionSource";
			const string translatedDescription = "TLDR";
			var listsElement = XElement.Parse("<Lists/>");
			var xliffXml = $@"<xliff version='1.2' xmlns:sil='software.sil.org'>
				<file source-language='EN' datatype='plaintext' original='MiscLists.xlf' target-language='{targLangOrig}'>
					<body>
						<group id='LangProject_GenreList'>
							<trans-unit id='LangProject_GenreList_Name'>
								<source>{englishName}</source>
								<target state='final'>{targetName}</target>
							</trans-unit>
							<trans-unit id='LangProject_GenreList_Abbr'>
								<source>gnrs</source>
								<target state='needs-translation'>gnrs</target>
							</trans-unit>
							<group id='LangProject_GenreList_Desc'>
								<trans-unit id='LangProject_GenreList_Desc_0'>
									<source>{englishDescription}</source>
									<target state='translated'>{translatedDescription}</target>
								</trans-unit>
							</group>
						</group>
					</body></file></xliff>";
			LocalizeLists.ConvertXliffToLists(XDocument.Parse(xliffXml), listsElement);
			// Verify Name WS and content for English source
			AssertThatXmlIn.String(listsElement.ToString()).HasSpecifiedNumberOfMatchesForXpath(
				$"/Lists/List/Name/AUni[@ws='en' and text()='{englishName}']", 1);
			// Verify Name WS and content for target language
			AssertThatXmlIn.String(listsElement.ToString()).HasSpecifiedNumberOfMatchesForXpath(
				$"/Lists/List/Name/AUni[@ws='{targLangNormalized}' and text()='{targetName}']", 1);
			// Verify Abbreviation WS and content for English source
			AssertThatXmlIn.String(listsElement.ToString()).HasSpecifiedNumberOfMatchesForXpath(
				"/Lists/List/Abbreviation/AUni[@ws='en' and text()='gnrs']", 1);
			// Verify Abbreviation WS has no translated content (it still needs translation)
			AssertThatXmlIn.String(listsElement.ToString()).HasSpecifiedNumberOfMatchesForXpath(
				$"/Lists/List/Abbreviation/AUni[@ws='{targLangNormalized}' and not(text())]", 1);
			// Verify Description WS and content for English source
			AssertThatXmlIn.String(listsElement.ToString()).HasSpecifiedNumberOfMatchesForXpath(
				"/Lists/List/Description/AStr[@ws='en']/Run[text()='" + englishDescription + "']", 1);
			// Verify Description WS and content for target language
			AssertThatXmlIn.String(listsElement.ToString()).HasSpecifiedNumberOfMatchesForXpath(
				$"/Lists/List/Description/AStr[@ws='{targLangNormalized}']/Run[text()='{translatedDescription}']", 1);
		}

		[TestCase("needs-translation", 0)]
		[TestCase("translated", 1)]
		[TestCase("final", 1)]
		public void ConvertXliffToLists_TranslationStateChecked(string transState, int isTranslated)
		{
			const string targetLang = "de";
			const string cognateName = "Name";
			const string englishDescription = "Description";
			const string translatedDescription = "Beschreibung";
			var listsElement = XElement.Parse("<Lists/>");
			var xliffXml = $@"<xliff version='1.2' xmlns:sil='software.sil.org'>
				<file source-language='EN' datatype='plaintext' original='MiscLists.xlf' target-language='{targetLang}'>
					<body>
						<group id='LangProject_GenreList'>
							<trans-unit id='LangProject_GenreList_Name'>
								<source>{cognateName}</source>
								<target state='{transState}'>{cognateName}</target>
							</trans-unit>
							<trans-unit id='LangProject_GenreList_Abbr'>
								<source>abbr</source>
								<target state='{transState}'>Abkü</target>
							</trans-unit>
							<group id='LangProject_GenreList_Desc'>
								<trans-unit id='LangProject_GenreList_Desc_0'>
									<source>{englishDescription}</source>
									<target state='{transState}'>{translatedDescription}</target>
								</trans-unit>
							</group>
						</group>
					</body></file></xliff>";
			LocalizeLists.ConvertXliffToLists(XDocument.Parse(xliffXml), listsElement);
			var result = listsElement.ToString();
			// Verify Name WS and content for English source
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
				$"/Lists/List/Name/AUni[@ws='en' and text()='{cognateName}']", 1);
			// Verify Name WS and content for German target language
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
				$"/Lists/List/Name/AUni[@ws='{targetLang}' and text()='{cognateName}']", isTranslated);
			// Verify Description WS and content for English source
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
				$"/Lists/List/Description/AStr[@ws='en']/Run[text()='{englishDescription}']", 1);
			// Verify Abbreviation WS has no content for German (because German matches English)
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
				$"/Lists/List/Description/AStr[@ws='{targetLang}']/Run[@ws='{targetLang}' and text()='{translatedDescription}']", isTranslated);
		}

		[Test]
		public void ConvertXliffToLists_EmptyPossGroupCreatesPossibilitiesElement()
		{
			var listsElement = XElement.Parse("<Lists/>");
			var xliffXml = @"<xliff version='1.2' xmlns:sil='software.sil.org'>
				<file source-language='EN' datatype='plaintext' original='MiscLists.xlf' target-language='es-ES'>
					<body>
						<group id='LexDb_Languages'>
							<trans-unit id='LexDb_Languages_Name'>
								<source>Languages</source>
							</trans-unit>
							<trans-unit id='LexDb_Languages_Abbr'>
								<source>Lgs</source>
							</trans-unit>
							<group id='LexDb_Languages_Poss'></group>
						</group>
					</body></file></xliff>";
			LocalizeLists.ConvertXliffToLists(XDocument.Parse(xliffXml), listsElement);
			// Verify Possibilities element created
			AssertThatXmlIn.String(listsElement.ToString()).HasSpecifiedNumberOfMatchesForXpath("/Lists/List[@owner='LexDb']/Possibilities", 1);
		}

		[Test]
		public void ConvertXliffToLists_PossAndSubpossGroupsConverted()
		{
			const string guid = "00951e6f-2523-4ac9-a649-ad42c659cf83";
			var listsElement = XElement.Parse("<Lists/>");
			const string xliffXml = @"<xliff version='1.2' xmlns:sil='software.sil.org'>
				<file source-language='EN' datatype='plaintext' original='MiscLists.xlf' target-language='es-ES'>
					<body>
						<group id='LangProject_GenreList'>
							<trans-unit id='LangProject_GenreList_Name'>
								<source>Genres</source>
							</trans-unit>
							<trans-unit id='LangProject_GenreList_Abbr'>
								<source>gnrs</source>
							</trans-unit>
							<group id='LangProject_GenreList_Poss'>
								<group id='LangProject_GenreList_Poss_0'>
									<sil:guid>" + guid + @"</sil:guid>
									<trans-unit id='LangProject_GenreList_Poss_0_Name'>
										<source>Monologue</source>
									</trans-unit>
									<trans-unit id='LangProject_GenreList_Poss_0_Abbr'>
										<source>mnlg</source>
									</trans-unit>
									<group id='LangProject_GenreList_Poss_0_SubPos'>
										<group id='LangProject_GenreList_Poss_0_SubPos_0'>
											<trans-unit id='LangProject_GenreList_Poss_0_SubPos_0_Name'>
												<source>Narrative</source>
											</trans-unit>
											<trans-unit id='LangProject_GenreList_Poss_0_SubPos_0_Abbr'>
												<source>nar</source>
											</trans-unit>
										</group>
									</group>
								</group>
							</group>
						</group>
					</body></file></xliff>";
			LocalizeLists.ConvertXliffToLists(XDocument.Parse(xliffXml), listsElement);
			// Verify Possibilities element created
			AssertThatXmlIn.String(listsElement.ToString())
				.HasSpecifiedNumberOfMatchesForXpath(
					"/Lists/List[@owner='LangProject']/Possibilities/CmPossibility", 1);
			// Verify that the SubPossibility was created
			AssertThatXmlIn.String(listsElement.ToString()).HasSpecifiedNumberOfMatchesForXpath(
				"/Lists/List/Possibilities/CmPossibility/SubPossibilities/CmPossibility", 1);
			// Verify the Name in the Possibility and SubPossibility
			AssertThatXmlIn.String(listsElement.ToString())
				.HasSpecifiedNumberOfMatchesForXpath(
					"//Possibilities/CmPossibility/Name/AUni[text()='Monologue']", 1);
			AssertThatXmlIn.String(listsElement.ToString()).HasSpecifiedNumberOfMatchesForXpath(
				"//CmPossibility/SubPossibilities/CmPossibility/Abbreviation/AUni[text()='nar']",
				1);
			// Verify that the guid was added as an attribute
			AssertThatXmlIn.String(listsElement.ToString()).HasSpecifiedNumberOfMatchesForXpath(
				$"//Possibilities/CmPossibility[@guid='{guid}']", 1);
		}

		/// <summary>
		/// Tests that ReverseName and ReverseAbbr of LexEntryTypes are converted from XLIFF
		/// </summary>
		[Test]
		public void ConvertXliffToLists_ReverseNameAndAbbrConverted()
		{
			const string guid = "024b62c9-93b3-41a0-ab19-587a0030219a";
			const string revName = "Dialectal Variant of";
			const string revAbbr = "dial. var. of";
			var listsElement = XElement.Parse("<Lists/>");
			const string xliffXml = @"<xliff version='1.2' xmlns:sil='software.sil.org'>
				<file source-language='EN' datatype='plaintext' original='test.xml' target-language='de'>
					<body>
						<group id='LexDb_VariantEntryTypes'>
							<group id='LexDb_VariantEntryTypes_Poss'>
								<group id='LexDb_VariantEntryTypes_Poss_0'>
									<sil:guid>" + guid + @"</sil:guid>
									<trans-unit id='LexDb_VariantEntryTypes_Poss_0_Name'>
										<source>Dialectal Variant</source>
									</trans-unit>
									<trans-unit id='LexDb_VariantEntryTypes_Poss_0_Abbr'>
										<source>dial. var.</source>
									</trans-unit>
									<trans-unit id='LexDb_VariantEntryTypes_Poss_0_RevName'>
										<source>" + revName + @"</source>
									</trans-unit>
									<trans-unit id='LexDb_VariantEntryTypes_Poss_0_RevAbbr'>
										<source>" + revAbbr + @"</source>
									</trans-unit>
								</group>
							</group>
						</group>
					</body></file></xliff>";
			LocalizeLists.ConvertXliffToLists(XDocument.Parse(xliffXml), listsElement);
			var result = listsElement.ToString();
			// Verify the Possibilities element was created with the correct owner
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
				"/Lists/List[@owner='LexDb']/Possibilities/LexEntryType", 1);
			// Verify the Reverse Name and Rev. Abbr. in the Possibility
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
				"//Possibilities/LexEntryType/ReverseName/AUni[text()='" + revName + "']", 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
				"//Possibilities/LexEntryType/ReverseAbbr/AUni[text()='" + revAbbr + "']", 1);
			// Verify that the guid was added as an attribute
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
				$"//Possibilities/LexEntryType[@guid='{guid}']", 1);
		}

		/// <summary>
		/// Tests that ReverseName and ReverseAbbreviation of LexRefTypes are converted from XLIFF
		/// </summary>
		[Test]
		public void ConvertXliffToLists_ReverseNameAndAbbreviationConverted()
		{
			const string revName = "Whole";
			const string revAbbrev = "wh";
			var listsElement = XElement.Parse("<Lists/>");
			const string xliffXml = @"<xliff version='1.2' xmlns:sil='software.sil.org'>
				<file source-language='EN' datatype='plaintext' original='test.xml' target-language='de'>
					<body>
						<group id='LexDb_References'>
							<group id='LexDb_References_Poss'>
								<group id='LexDb_References_Poss_0'>
									<trans-unit id='LexDb_References_Poss_0_Name'>
										<source>Part</source>
									</trans-unit>
									<trans-unit id='LexDb_References_Poss_0_Abbr'>
										<source>pt</source>
									</trans-unit>
									<trans-unit id='LexDb_References_Poss_0_RevName'>
										<source>" + revName + @"</source>
									</trans-unit>
									<trans-unit id='LexDb_References_Poss_0_RevAbbrev'>
										<source>" + revAbbrev + @"</source>
									</trans-unit>
								</group>
							</group>
						</group>
					</body></file></xliff>";
			LocalizeLists.ConvertXliffToLists(XDocument.Parse(xliffXml), listsElement);
			var result = listsElement.ToString();
			// Verify the Possibilities element was created with the correct owner
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
				"/Lists/List[@owner='LexDb']/Possibilities/LexRefType", 1);
			// Verify the Reverse Name and Rev. Abbreviation in the Possibility
			AssertThatXmlIn.String(listsElement.ToString()).HasSpecifiedNumberOfMatchesForXpath(
				"//Possibilities/LexRefType/ReverseName/AUni[text()='" + revName + "']", 1);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
				"//Possibilities/LexRefType/ReverseAbbreviation/AUni[text()='" + revAbbrev + "']", 1);
		}

		[Test]
		public void ConvertXliffToLists_SemanticDomainConvertedWithQuestions()
		{
			const string question = "(1) What words refer to everything we can see?";
			const string exampleWords = "universe, creation, cosmos, heaven and earth, macrocosm, everything that exists";
			const string exampleSentence = "In the beginning God created <the heavens and the earth>.";
			var listsElement = XElement.Parse("<Lists/>");
			var xliffXml = @"<xliff version='1.2' xmlns:sil='software.sil.org'>
				<file source-language='EN' datatype='plaintext' original='test.xml' target-language='de'>
					<body>
						<group id='LangProject_SemanticDomainList'>
							<group id='LangProject_SemanticDomainList_Poss'>
								<group id='LangProject_SemanticDomainList_Poss_0'>
									<sil:guid>63403699-07c1-43f3-a47c-069d6e4316e5</sil:guid>
									<trans-unit id='LangProject_SemanticDomainList_Poss_0_Name'>
										<source>Universe, creation</source>
									</trans-unit>
									<trans-unit id='LangProject_SemanticDomainList_Poss_0_Abbr'>
										<source>1</source>
									</trans-unit>
									<group id='LangProject_SemanticDomainList_Poss_0_Desc'>
										<trans-unit id='LangProject_SemanticDomainList_Poss_0_Desc_0'>
											<source>Use this domain for general words referring to the physical universe. Some languages may not have a single word for the universe and may have to use a phrase such as &amp;apos;rain, soil, and things of the sky&amp;apos; or &amp;apos;sky, land, and water&amp;apos; or a descriptive phrase such as &amp;apos;everything you can see&amp;apos; or &amp;apos;everything that exists&amp;apos;.</source>
										</trans-unit>
									</group>
									<group id='LangProject_SemanticDomainList_Poss_0_Qs'>
										<group id='LangProject_SemanticDomainList_Poss_0_Qs_0'>
											<trans-unit id='LangProject_SemanticDomainList_Poss_0_Qs_0_Q'>
												<source>" + question + @"</source>
											</trans-unit>
											<trans-unit id='LangProject_SemanticDomainList_Poss_0_Qs_0_EW'>
												<source>" + exampleWords + @"</source>
											</trans-unit>
											<group id='LangProject_SemanticDomainList_Poss_0_Qs_0_ES'>
												<trans-unit id='LangProject_SemanticDomainList_Poss_0_Qs_0_ES_0'>
													<source>" + SecurityElement.Escape(exampleSentence) + @"</source>
												</trans-unit>
											</group>
										</group>
									</group>
								</group>
							</group>
						</group>
					</body></file></xliff>";
			LocalizeLists.ConvertXliffToLists(XDocument.Parse(xliffXml), listsElement);
			// Verify the Semantic Domain element was created with the correct owner
			AssertThatXmlIn.String(listsElement.ToString()).HasSpecifiedNumberOfMatchesForXpath(
				"/Lists/List[@owner='LangProject']/Possibilities/CmSemanticDomain", 1);
			// Verify the question parent element was created
			AssertThatXmlIn.String(listsElement.ToString()).HasSpecifiedNumberOfMatchesForXpath(
				"//Possibilities/CmSemanticDomain/Questions/CmDomainQ", 1);
			// Verify the contents
			AssertThatXmlIn.String(listsElement.ToString()).HasSpecifiedNumberOfMatchesForXpath(
				"//Questions/CmDomainQ/Question/AUni[text()='" + question + "']", 1);
			AssertThatXmlIn.String(listsElement.ToString()).HasSpecifiedNumberOfMatchesForXpath(
				"//Questions/CmDomainQ/ExampleWords/AUni[text()='" + exampleWords + "']", 1);
			AssertThatXmlIn.String(listsElement.ToString()).HasSpecifiedNumberOfMatchesForXpath(
				"//Questions/CmDomainQ/ExampleSentences/AStr/Run[text()='" + exampleSentence + "']", 1);
		}

		[Test]
		public void ConvertXliffToLists_RunStylesCreated()
		{
			const string style = "Emphasized Text";
			const string styledText = "something";
			const string unstyledText = "else";
			const string styledTrans = "Etwas";
			const string unstyledTrans = "anderes";
			const string targetWs = "de";
			const string xliffXml = @"<xliff version='1.2' xmlns:sil='software.sil.org'>
				<file source-language='EN' datatype='plaintext' original='test.xml' target-language='" + targetWs + @"'>
					<body>
						<group id='LangProject_AnthroList'>
							<group id='LangProject_AnthroList_Desc'>
								<trans-unit id='LangProject_AnthroList_Desc_0'>
									<sil:style>" + style + @"</sil:style>
									<source>" + styledText + @"</source>
									<target state='final'>" + styledTrans + @"</target>
								</trans-unit>
								<trans-unit id='LangProject_AnthroList_Desc_1'>
									<source>" + unstyledText + @"</source>
									<target state='final'>" + unstyledTrans + @"</target>
								</trans-unit>
							</group>
						</group>
					</body></file></xliff>";
			var xliffDoc = XDocument.Parse(xliffXml);

			var listsElement = XElement.Parse("<Lists/>");
			LocalizeLists.ConvertXliffToLists(xliffDoc, listsElement);
			var result = listsElement.ToString();
			// Test for description element
			const string xpathToDescAStr = "/Lists/List[@owner='LangProject']/Description/AStr";
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(xpathToDescAStr + "/Run[@ws='en']", 2, true);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(xpathToDescAStr + "/Run[@ws='" + targetWs + "']", 2, true);
			// Test for styling
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
				xpathToDescAStr + "/Run[text()='" + styledText + "' and @namedStyle='" + style + "']", 1, true);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
				xpathToDescAStr + "/Run[text()='" + unstyledText + "' and not (@namedStyle)]", 1, true);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
				xpathToDescAStr + "/Run[text()='" + styledTrans + "' and @namedStyle='" + style + "']", 1, true);
			AssertThatXmlIn.String(result).HasSpecifiedNumberOfMatchesForXpath(
				xpathToDescAStr + "/Run[text()='" + unstyledTrans + "' and not (@namedStyle)]", 1, true);
		}

		[Test]
		public void RoundTrip_XmlEscapablesSurvive()
		{
			const string unescaped = "This & that <shouldn't> \"cause\" probl'ms.";
			const string escaped = "This &amp; that &lt;shouldn&apos;t&gt; &quot;cause\" probl'ms.";
			const string listXml = @"<Lists><List owner='LangProject' field='AnthroList' itemClass='CmAnthroItem'>
				<Possibilities>
					<CmAnthroItem>
						<Name>
							<AUni ws='en'>" + escaped + @"</AUni>
						</Name>
						<Description>
							<AStr ws='en'>
								<Run ws='en'>" + escaped + @"</Run>
							</AStr>
						</Description>
					</CmAnthroItem>
				</Possibilities>
			</List></Lists>";

			// Test and verify the first direction
			// ReSharper disable PossibleNullReferenceException - XPath Asserts ensure existence of XElements
			var xliffDoc = LocalizeLists.ConvertListToXliff("test.xml", XDocument.Parse(listXml));
			const string xpathToPoss0 = "//group[@id='LangProject_AnthroList']/group/group[@id='LangProject_AnthroList_Poss_0']";
			const string xpathToNameSource = xpathToPoss0 + "/trans-unit[@id='LangProject_AnthroList_Poss_0_Name']/source";
			const string xpathToDescSource = xpathToPoss0 + "/group/trans-unit[@id='LangProject_AnthroList_Poss_0_Desc_0']/source";
			AssertThatXmlIn.String(xliffDoc.ToString()).HasSpecifiedNumberOfMatchesForXpath(xpathToNameSource, 1, true);
			AssertThatXmlIn.String(xliffDoc.ToString()).HasSpecifiedNumberOfMatchesForXpath(xpathToDescSource, 1, true);
			var nameSourceElt = xliffDoc.XPathSelectElement(xpathToNameSource);
			var descSourceElt = xliffDoc.XPathSelectElement(xpathToDescSource);
			Assert.AreEqual(unescaped, nameSourceElt.Value);
			Assert.AreEqual(unescaped, descSourceElt.Value);

			AddTargetLanguage(xliffDoc);

			// Test and verify the round trip
			var roundTripped = XElement.Parse("<Lists/>");
			LocalizeLists.ConvertXliffToLists(xliffDoc, roundTripped);
			const string xpathToItem = "//List/Possibilities/CmAnthroItem";
			const string xpathToNameAUni = xpathToItem + "/Name/AUni[@ws='en']";
			const string xpathToDescRun = xpathToItem + "/Description/AStr[@ws='en']/Run[@ws='en']";
			AssertThatXmlIn.String(roundTripped.ToString()).HasSpecifiedNumberOfMatchesForXpath(xpathToNameAUni, 1);
			AssertThatXmlIn.String(roundTripped.ToString()).HasSpecifiedNumberOfMatchesForXpath(xpathToDescRun, 1);
			var nameAUni = roundTripped.XPathSelectElement(xpathToNameAUni);
			var descRun = roundTripped.XPathSelectElement(xpathToDescRun);
			Assert.AreEqual(unescaped, nameAUni.Value);
			Assert.AreEqual(unescaped, descRun.Value);
			// ReSharper enable PossibleNullReferenceException
		}

		/// <summary>
		/// Test with an export of TranslatedLists from FieldWorks (you must add a second analysis language to enable this option)
		/// </summary>
		[Ignore]
		[Category("ByHand")]
		[Test]
		public void IntegrationTest()
		{
			// clean and create the output directory
			const string outputDir = @"C:\WorkingFiles\XliffTestOutput";
			TaskTestUtils.RecreateDirectory(outputDir);

			// convert from XML to XLIFF
			LocalizeLists.SplitSourceLists(@"C:\WorkingFiles\TranslatedListOutput.xml", outputDir);

			// add a target language (required)
			var files = Directory.GetFiles(@"C:\WorkingFiles\XLiffTestOutput", "*.xlf").ToList();
			foreach (var file in files)
			{
				var xDoc = XDocument.Load(file);
				AddTargetLanguage(xDoc);
				xDoc.Save(file);
			}

			// convert from XLIFF to XML
			LocalizeLists.CombineXliffFiles(files, @"C:\WorkingFiles\RoundTripped.xml");
		}

		private static void AddTargetLanguage(XDocument xliffDoc)
		{
				var fileElement = xliffDoc.XPathSelectElement("/xliff/file");
				fileElement?.Add(new XAttribute("target-language", "."));
		}
	}
}