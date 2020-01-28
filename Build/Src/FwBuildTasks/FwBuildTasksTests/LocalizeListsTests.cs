// Copyright (c) 2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;
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
				string.Format(
					@"<Lists>
					<List owner='{0}' field='{1}' itemClass='CmPossibility'>
						<Name><AUni ws='en'>Academic Domains</AUni></Name>
					</List>
				</Lists>", owner, field)));
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
			var listXml = @"<Lists><List owner='LangProject' field='AnthroList' itemClass='CmAnthroItem'>
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
			var listXml = @"<Lists><List owner='LangProject' field='AnthroList' itemClass='CmAnthroItem'>
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
		}

		[Test]
		public void ConvertListToXliff_SubPossibilityCreated()
		{
			var listXml = @"<Lists><List owner='LangProject' field='AnthroList' itemClass='CmAnthroItem'>
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
			// Test for abbreviation trans-unit source
			AssertThatXmlIn.String(xliffDoc.ToString()).HasSpecifiedNumberOfMatchesForXpath(xpathToSubPosGroup, 1, true);
			// Verify name
			AssertThatXmlIn.String(xliffDoc.ToString()).HasSpecifiedNumberOfMatchesForXpath(xpathToSubPosGroup + "/group/trans-unit/source[text()='cultural anthropology']", 1, true);
			// Verify abbreviation
			AssertThatXmlIn.String(xliffDoc.ToString()).HasSpecifiedNumberOfMatchesForXpath(xpathToSubPosGroup + "/group/trans-unit/source[text()='Cult anthro']", 1, true);
		}

		[Test]
		public void ConvertListToXliff_SemanticDomainConvertedWithQuestions()
		{
			var listXml = @"<Lists><List owner='LangProject' field='SemanticDomainList' itemClass='CmSemanticDomain'>
				<Possibilities>
					<CmSemanticDomain guid='63403699-07c1-43f3-a47c-069d6e4316e5'>
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
			AssertThatXmlIn.String(xliffDoc.ToString()).HasSpecifiedNumberOfMatchesForXpath("//group/*[local-name()='Guid'][text()='63403699-07c1-43f3-a47c-069d6e4316e5']", 1);
			// This xpath matches the first semantic domain
			var xpathToPossGroup = "//group/group/group[@id='LangProject_SemanticDomainList_Poss_0']";
			// Verify questions group present
			AssertThatXmlIn.String(xliffDoc.ToString()).HasSpecifiedNumberOfMatchesForXpath(xpathToPossGroup + "/group[contains(@id, '_Qs')]", 1, true);
			// Verify first question present
			AssertThatXmlIn.String(xliffDoc.ToString()).HasSpecifiedNumberOfMatchesForXpath(xpathToPossGroup + "/group/group[contains(@id, '_Qs_0')]", 1, true);
			// Verify question trans-unit present
			AssertThatXmlIn.String(xliffDoc.ToString()).HasSpecifiedNumberOfMatchesForXpath(xpathToPossGroup + "/group/group/trans-unit[contains(@id, '_Qs_0_Q')]", 1, true);
			// Verify ExampleWords trans-unit present
			AssertThatXmlIn.String(xliffDoc.ToString()).HasSpecifiedNumberOfMatchesForXpath(xpathToPossGroup + "/group/group/trans-unit[contains(@id, '_Qs_0_EW')]", 1, true);
			// Verify ExampleSentence group present
			AssertThatXmlIn.String(xliffDoc.ToString()).HasSpecifiedNumberOfMatchesForXpath(xpathToPossGroup + "/group/group/group[contains(@id, '_Qs_0_ES')]", 1, true);
		}

		[Ignore]
		[Category("ByHand")]
		[Test]
		public void IntegrationTest()
		{
			// Test with an export of TranslatedLists from FieldWorks (you must add a second analysis language first)
			LocalizeLists.SplitSourceLists("C:\\WorkingFiles\\TestOutput.xml", "C:\\WorkingFiles\\XliffTestOutput");
		}

		[Test]
		public void ConvertXliffToLists_ListElementCreated()
		{
			var listsElement = XElement.Parse("<Lists/>");
			var xliffXml = @"<xliff version='1.2' xmlns:sil='software.sil.org'>
				<file source-language='EN' datatype='plaintext' original='MiscLists.xlf'>
					<body>
						<group id='LangProject_ConfidenceLevels'/>
					</body></file></xliff>";
			LocalizeLists.ConvertXliffToLists(XDocument.Parse(xliffXml), listsElement);
			AssertThatXmlIn.String(listsElement.ToString()).HasSpecifiedNumberOfMatchesForXpath("/Lists/List[@owner='LangProject' and @field='ConfidenceLevels']", 1);
		}

		[Test]
		public void ConvertXliffToLists_ListNameAbbrevAndDescCreated()
		{
			var listsElement = XElement.Parse("<Lists/>");
			var xliffXml = @"<xliff version='1.2' xmlns:sil='software.sil.org'>
				<file source-language='EN' datatype='plaintext' original='MiscLists.xlf' target-language='es-ES'>
					  <body>
						<group id='LangProject_GenreList'>
							<trans-unit id='LangProject_GenreList_Name'>
							<source>Genres</source>
							<target state='final'>Géneros</target>
							</trans-unit>
							<trans-unit id='LangProject_GenreList_Abbr'>
							<source>gnrs</source>
							<target state='needs-translatin'>gnrs</target>
							</trans-unit>
							<group id='LangProject_GenreList_Desc'>
							<trans-unit id='LangProject_GenreList_Desc_0'>
							<source>DescriptionSource</source>
							<target state='final'>TLDR</target>
							</trans-unit>
							</group>
						</group>
					</body></file></xliff>";
			LocalizeLists.ConvertXliffToLists(XDocument.Parse(xliffXml), listsElement);
			// Verify Name Elements and content for English source
			AssertThatXmlIn.String(listsElement.ToString()).HasSpecifiedNumberOfMatchesForXpath("/Lists/List/Name/AUni[@ws='en' and text()='Genres']", 1);
			// Verify Name Elements and content for Spanish target language
			AssertThatXmlIn.String(listsElement.ToString()).HasSpecifiedNumberOfMatchesForXpath("/Lists/List/Name/AUni[@ws='es' and text()='Géneros']", 1);
			// Verify Abbreviation Elements and content for English source
			AssertThatXmlIn.String(listsElement.ToString()).HasSpecifiedNumberOfMatchesForXpath("/Lists/List/Abbreviation/AUni[@ws='en' and text()='gnrs']", 1);
			// Verify Abbreviation Elements has no content for Spanish (because Spanish matches English)
			AssertThatXmlIn.String(listsElement.ToString()).HasSpecifiedNumberOfMatchesForXpath("/Lists/List/Abbreviation/AUni[@ws='es' and not(text())]", 1);
			// Verify Description Elements and content for English source
			AssertThatXmlIn.String(listsElement.ToString()).HasSpecifiedNumberOfMatchesForXpath("/Lists/List/Description/AStr[@ws='en']/Run[text()='DescriptionSource']", 1);
			// Verify Abbreviation Elements has no content for Spanish (because Spanish matches English)
			AssertThatXmlIn.String(listsElement.ToString()).HasSpecifiedNumberOfMatchesForXpath("/Lists/List/Description/AStr[@ws='es']/Run[text()='TLDR']", 1);
		}
	}
}