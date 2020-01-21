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
				.HasSpecifiedNumberOfMatchesForXpath(
					string.Format("/xliff/file[@original='{0}']", testName), 1);
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
				string.Format(
					"/xliff/file/body/group[@id='{0}']/trans-unit[@id='{0}_Name']/source[text()='Academic Domains']",
					group), 1, true);
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
			// This xpath matches the first possiblity in the possiblities group
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
			// This xpath matches the first sub possibility of the first possiblity in the possiblities group
			var xpathToPossGroup = "//group[@id='LangProject_AnthroList']/group[@id='LangProject_AnthroList_Poss']/group/group[@id='LangProject_AnthroList_Poss_0_SubPos']";
			// Test for abbreviation trans-unit source
			AssertThatXmlIn.String(xliffDoc.ToString()).HasSpecifiedNumberOfMatchesForXpath(xpathToPossGroup, 1, true);
			// Verify name
			AssertThatXmlIn.String(xliffDoc.ToString()).HasSpecifiedNumberOfMatchesForXpath(xpathToPossGroup + "/group/trans-unit/source[text()='cultural anthropology']", 1, true);
			// Verify abbreviation
			AssertThatXmlIn.String(xliffDoc.ToString()).HasSpecifiedNumberOfMatchesForXpath(xpathToPossGroup + "/group/trans-unit/source[text()='Cult anthro']", 1, true);
		}

		[Ignore]
		[Category("ByHand")]
		[Test]
		public void IntegrationTest()
		{
			LocalizeLists.SplitSourceLists("C:\\WorkingFiles\\TestOutput.xml", "C:\\WorkingFiles\\XliffTestOutput");
		}
	}
}