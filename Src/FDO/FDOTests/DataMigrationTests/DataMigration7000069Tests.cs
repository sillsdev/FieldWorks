using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.DomainServices.DataMigration;

namespace SIL.FieldWorks.FDO.FDOTests.DataMigrationTests
{
	/// <summary>
	/// Unit tests for DataMigration7000069
	/// </summary>
	[TestFixture]
	public class DataMigration7000069Tests : DataMigrationTestsBase
	{
		/// <summary/>
		[SuppressMessage("Gendarme.Rules.Portability", "NewLineLiteralRule",
			Justification="Newline of sampleLayoutData is in source file and so will be correct according to current platform")]
		public DataMigration7000069Tests()
		{
		}

		private string sampleLayoutData =
			@"<LayoutInventory>
				<layout class='LexEntry' type='jtview' name='publishStemPara' css='$fwstyle=Dictionary-Normal' version='11'>
					<part ref='MLHeadWordPub' label='Headword' before='' sep=' ' after='  ' ws='am-Ethi' wsType='vernacular' style='Dictionary-Headword'  />
					<part ref='MLHeadWordPub' label='Headword' before='' sep=' ' after='  ' ws='vernacular' wsType='vernacular' style='Dictionary-Headword'  />
					<part ref='MLHeadWordPub' label='Headword' before='' sep=' ' after='  ' ws='$ws=all analysis' wsType='vernacular' style='Dictionary-Headword'  />
					<part ref='MLHeadWordPub' label='Headword' before='' sep=' ' after='  ' ws='$ws=am-Ethi' wsType='vernacular' style='Dictionary-Headword'  />
					<part ref='MLHeadWordPub' label='Headword' before='' sep=' ' after='  ' ws='am-Ethi-fonipa,am-Ethi' visibleWritingSystems='am-Ethi-fonipa,am-Ethi' wsType='vernacular' style='Dictionary-Headword'  />
				</layout>
			</LayoutInventory>
			";

		private string sampleLocalSettingsData =
			"<ArrayOfProperty xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">"
			+"	<Property>"
			+" <name>db$local$InterlinConfig_Edit_Interlinearizer</name>"
			+" <value xsi:type=\"xsd:string\">EditableInterlinLineChoices,5062001%,5062001%am-Ethi,5112002%,5112002%am-Ethi,103%,103%am-Ethi,5112004%,5112004%fr,5112003%,5112003%fr,5060001%en,5060001%fr,5059003%,5059003%fr,-61%en,-61%fr,-63%en,-63%fr,-62%en,-62%fr</value>"
			+ " </Property> "
			+"<Property>"
			+"<name>db$local$LexDb.Entries_sorter</name>"
			+"<value xsi:type=\"xsd:string\">&lt;sorter assemblyPath=\"Filters.dll\" class=\"SIL.FieldWorks.Filters.GenRecordSorter\"&gt;&lt;comparer assemblyPath=\"Filters.dll\" class=\"SIL.FieldWorks.Filters.StringFinderCompare\"&gt;&lt;finder assemblyPath=\"XMLViews.dll\" class=\"SIL.FieldWorks.Common.Controls.SortMethodFinder\" layout=\"EntryHeadwordForEntry\" sortmethod=\"FullSortKey\" ws=\"vernacular\"&gt;&lt;column layout=\"EntryHeadwordForEntry\" label=\"Headword\" ws=\"$ws=vernacular\" width=\"72000\" sortmethod=\"FullSortKey\" cansortbylength=\"true\" visibility=\"always\" /&gt;&lt;/finder&gt;&lt;comparer assemblyPath=\"Filters.dll\" class=\"SIL.FieldWorks.Filters.WritingSystemComparer\" ws=\"am-Ethi\" /&gt;&lt;/comparer&gt;&lt;/sorter&gt;</value>"
			+"</Property>"
			+ "<Property>"
			+ "<name>db$local$LexDb.Entries_sorter</name>"
			+ "<value xsi:type=\"xsd:string\">&lt;sorter assemblyPath=\"Filters.dll\" class=\"SIL.FieldWorks.Filters.GenRecordSorter\"&gt;&lt;comparer assemblyPath=\"Filters.dll\" class=\"SIL.FieldWorks.Filters.StringFinderCompare\"&gt;&lt;finder assemblyPath=\"XMLViews.dll\" class=\"SIL.FieldWorks.Common.Controls.SortMethodFinder\" layout=\"EntryHeadwordForEntry\" sortmethod=\"FullSortKey\" ws=\"vernacular\"&gt;&lt;column layout=\"EntryHeadwordForEntry\" label=\"Headword\" ws=\"$ws=vernacular\" width=\"72000\" sortmethod=\"FullSortKey\" cansortbylength=\"true\" visibility=\"always\" /&gt;&lt;/finder&gt;&lt;comparer assemblyPath=\"Filters.dll\" class=\"SIL.FieldWorks.Filters.WritingSystemComparer\" ws=\"$ws=am-Ethi\" /&gt;&lt;/comparer&gt;&lt;/sorter&gt;</value>"
			+ "</Property>"
			+ "<Property>"
			+ "<name>db$local$LexDb.Entries_sorter</name>"
			+ "<value xsi:type=\"xsd:string\">&lt;sorter assemblyPath=\"Filters.dll\" class=\"SIL.FieldWorks.Filters.GenRecordSorter\"&gt;&lt;comparer assemblyPath=\"Filters.dll\" class=\"SIL.FieldWorks.Filters.StringFinderCompare\"&gt;&lt;finder assemblyPath=\"XMLViews.dll\" class=\"SIL.FieldWorks.Common.Controls.SortMethodFinder\" layout=\"EntryHeadwordForEntry\" sortmethod=\"FullSortKey\" ws=\"vernacular\"&gt;&lt;column layout=\"EntryHeadwordForEntry\" label=\"Headword\" ws=\"$ws=vernacular\" width=\"72000\" sortmethod=\"FullSortKey\" cansortbylength=\"true\" visibility=\"always\" /&gt;&lt;/finder&gt;&lt;comparer assemblyPath=\"Filters.dll\" class=\"SIL.FieldWorks.Filters.WritingSystemComparer\" ws=\"$wsName\" /&gt;&lt;/comparer&gt;&lt;/sorter&gt;</value>"
			+ "</Property>"
			+ "<Property>"
			+ "<name>db$local$LexDb.Entries_sorter</name>"
			+ "<value xsi:type=\"xsd:string\">&lt;sorter assemblyPath=\"Filters.dll\" class=\"SIL.FieldWorks.Filters.GenRecordSorter\"&gt;&lt;comparer assemblyPath=\"Filters.dll\" class=\"SIL.FieldWorks.Filters.StringFinderCompare\"&gt;&lt;finder assemblyPath=\"XMLViews.dll\" class=\"SIL.FieldWorks.Common.Controls.SortMethodFinder\" layout=\"EntryHeadwordForEntry\" sortmethod=\"FullSortKey\" ws=\"vernacular\"&gt;&lt;column layout=\"EntryHeadwordForEntry\" label=\"Headword\" ws=\"$ws=vernacular\" width=\"72000\" sortmethod=\"FullSortKey\" cansortbylength=\"true\" visibility=\"always\" /&gt;&lt;/finder&gt;&lt;comparer assemblyPath=\"Filters.dll\" class=\"SIL.FieldWorks.Filters.WritingSystemComparer\" ws=\"$ws=reversal\" /&gt;&lt;/comparer&gt;&lt;/sorter&gt;</value>"
			+ "</Property>"
			+ "<Property> <name>db$local$ConcordanceWs</name> <value xsi:type=\"xsd:string\">am-Ethi</value></Property>"
			+ "<Property> <name>db$local$WordformInventory.Wordforms_sorter</name> <value xsi:type=\"xsd:string\">>&lt;sorter assemblyPath=\"Filters.dll\" class=\"SIL.FieldWorks.Filters.GenRecordSorter\"&gt;&lt;comparer assemblyPath=\"Filters.dll\" class=\"SIL.FieldWorks.Filters.StringFinderCompare\"&gt;&lt;finder assemblyPath=\"XMLViews.dll\" class=\"SIL.FieldWorks.Common.Controls.LayoutFinder\" layout=\"\"&gt;&lt;column label=\"Form\" width=\"30%\" cansortbylength=\"true\" ws=\"$ws=best vernacular\" field=\"Form\"&gt;&lt;span&gt;&lt;properties&gt;&lt;bold value=\"off\" /&gt;&lt;/properties&gt;&lt;string field=\"Form\" ws=\"best vernacular\" /&gt;&lt;/span&gt;&lt;/column&gt;&lt;/finder&gt;&lt;comparer assemblyPath=\"Filters.dll\" class=\"SIL.FieldWorks.Filters.WritingSystemComparer\" ws=\"am-Ethi\" /&gt;&lt;/comparer&gt;&lt;/sorter&gt;</value> </Property>"
			+ "<Property>"
			+ "<name>db$local$lexiconEdit_lexentryList_ColumnList</name> "
			+ "<value xsi:type=\"xsd:string\">&lt;root version=\"14\"&gt;&lt;column layout=\"LexemeFormForEntry\" common=\"true\" width=\"72000\" ws=\"$ws=am-Ethi\" sortmethod=\"MorphSortKey\" cansortbylength=\"true\" visibility=\"always\" transduce=\"LexEntry.LexemeForm.Form\" transduceCreateClass=\"MoStemAllomorph\" originalWs=\"vernacular\" originalLabel=\"Lexeme Form\" label=\"Lexeme Form (Sui_ipa)\" /&gt;&lt;column layout=\"EntryHeadwordForEntry\" label=\"Headword\" ws=\"$ws=vernacular\" width=\"72000\" sortmethod=\"FullSortKey\" cansortbylength=\"true\" visibility=\"always\" /&gt;&lt;column layout=\"GlossesForSense\" multipara=\"true\" width=\"72000\" ws=\"$ws=zh-CN\" transduce=\"LexSense.Gloss\" cansortbylength=\"true\" visibility=\"always\" originalWs=\"analysis\" originalLabel=\"Glosses\" label=\"Glosses (ManS)\" /&gt;&lt;column layout=\"GrammaticalInfoFullForSense\" headerlabel=\"Grammatical Info.\" chooserFilter=\"external\" label=\"Grammatical Info. (Full)\" multipara=\"true\" width=\"72000\" visibility=\"always\"&gt;&lt;dynamicloaderinfo assemblyPath=\"FdoUi.dll\" class=\"SIL.FieldWorks.FdoUi.PosFilter\" /&gt;&lt;/column&gt;&lt;/root&gt;</value>"
			+ "</Property>"
			+ "</ArrayOfProperty>";

		/// <summary>
		/// Test the migration from version 7000068 to 7000069.
		/// </summary>
		[Test]
		public void DataMigration7000069Test()
		{
			string projectFolder = Path.Combine(Path.GetTempPath(), "DataMigration7000069Tests");
			try
			{
				if (Directory.Exists(projectFolder))
					Directory.Delete(projectFolder, true);
				Directory.CreateDirectory(projectFolder);
				string storePath = Path.Combine(projectFolder, FdoFileHelper.ksWritingSystemsDir);
				Directory.CreateDirectory(storePath);
				string testDataPath = Path.Combine(FwDirectoryFinder.SourceDirectory, "FDO", "FDOTests", "TestData");
				string testEnglishPath = Path.Combine(storePath, "en.ldml");
				File.Copy(Path.Combine(testDataPath, "en_7000068.ldml"), testEnglishPath);
				File.SetAttributes(testEnglishPath, FileAttributes.Normal); // don't want to copy readonly property.
				string xkalPath = Path.Combine(storePath, "am-Ethi.ldml");
				File.Copy(Path.Combine(testDataPath, "am-Ethi_7000068.ldml"), xkalPath);
				File.SetAttributes(xkalPath, FileAttributes.Normal); // don't want to copy readonly property.
				string xkalFonipaPath = Path.Combine(storePath, "am-Ethi-fonipa.ldml");
				File.Copy(Path.Combine(testDataPath, "am-Ethi-fonipa_7000068.ldml"), xkalFonipaPath);
				File.SetAttributes(xkalFonipaPath, FileAttributes.Normal); // don't want to copy readonly property.

				HashSet<DomainObjectDTO> dtos = DataMigrationTestServices.ParseProjectFile("DataMigration7000069.xml");
				// Create all the Mock classes for the classes in my test data.
				var mockMdc = new MockMDCForDataMigration();
				mockMdc.AddClass(1, "CmObject", null, new List<string>
				{
					"LexEntry", "LangProject", "LexSense", "LexDb",
					"ReversalEntry", "StStyle", "CmPossibilityList", "CmBaseAnnotation"
				});
				mockMdc.AddClass(2, "LangProject", "CmObject", new List<string>());
				mockMdc.AddClass(3, "LexEntry", "CmObject", new List<string>());
				mockMdc.AddClass(4, "LexSense", "CmObject", new List<string>());
				mockMdc.AddClass(5, "LexDb", "CmObject", new List<string>());
				mockMdc.AddClass(6, "ReversalEntry", "CmObject", new List<string>());
				mockMdc.AddClass(7, "StStyle", "CmObject", new List<string>());
				mockMdc.AddClass(8, "CmPossibilityList", "CmObject", new List<string>());
				mockMdc.AddClass(9, "CmBaseAnnotation", "CmObject", new List<string>());

				string settingsFolder = Path.Combine(projectFolder, FdoFileHelper.ksConfigurationSettingsDir);
				Directory.CreateDirectory(settingsFolder);
				string sampleLayout = Path.Combine(settingsFolder, "Test.fwlayout");
				File.WriteAllText(sampleLayout, sampleLayoutData, Encoding.UTF8);
				string sampleSettings = Path.Combine(settingsFolder, "db$local$Settings.xml");
				File.WriteAllText(sampleSettings, sampleLocalSettingsData, Encoding.UTF8);

				IDomainObjectDTORepository dtoRepos = new DomainObjectDtoRepository(7000068, dtos, mockMdc, projectFolder, FwDirectoryFinder.FdoDirectories);
				// Do the migration.
				m_dataMigrationManager.PerformMigration(dtoRepos, 7000069, new DummyProgressDlg());

				// Verification Phase
				Assert.AreEqual(7000069, dtoRepos.CurrentModelVersion, "Wrong updated version.");

				// Todo:
				// Verify that en.ldml is unchanged.
				Assert.That(File.Exists(testEnglishPath));
				// Verify that am-Ethi.ldml is renamed to am and content changed
				Assert.That(File.Exists(Path.Combine(storePath, "am.ldml")));
				// Verify that am-Ethi-fonipa.ldml is renamed to am-fonipa and content changed
				Assert.That(File.Exists(Path.Combine(storePath, "am-fonipa.ldml")));
				// Verify that AUni data in LexEntry" guid="7ecbb299-bf35-4795-a5cc-8d38ce8b891c tag is changed to am
				XElement entry = XElement.Parse(dtoRepos.GetDTO("7ecbb299-bf35-4795-a5cc-8d38ce8b891c").Xml);
				Assert.That(entry.Element("CitationForm").Element("AUni").Attribute("ws").Value, Is.EqualTo("am"));
				// Verify that AStr data in LexSense" guid="e3c2d179-3ccd-431e-ac2e-100bdb883680" tag is changed to am
				XElement sense = XElement.Parse(dtoRepos.GetDTO("e3c2d179-3ccd-431e-ac2e-100bdb883680").Xml);
				Assert.That(sense.Element("Definition").Elements("AStr").Skip(1).First().Attribute("ws").Value, Is.EqualTo("am"));
				Assert.That(sense.Element("Definition").Elements("AStr").Count(), Is.EqualTo(2), "french should be deleted because empty");
				// Verify that the empty alternatives get removed.
				Assert.That(sense.Element("Bibliography").Elements("AUni").First().Attribute("ws").Value, Is.EqualTo("en"));
				Assert.That(sense.Element("Bibliography").Elements("AUni").Count(), Is.EqualTo(1));
				// Verify that Run data in LexSense" guid="e3c2d179-3ccd-431e-ac2e-100bdb883680" tag is changed to am
				Assert.That(sense.Element("Definition").Element("AStr").Elements("Run").Skip(1).First().Attribute("ws").Value, Is.EqualTo("am"));
				// Check LiftResidue lang attributes are fixed; note that a result containing lang=&quot;am&quot
				// would also be acceptable, perhaps even more to be expected, but converting the &quot; s here to " is acceptable.
				Assert.That(sense.Element("LiftResidue").Element("Uni").Value.Contains("lang=\"am\""));
				// Verify that WsProp data in StStyle guid="4d312f11-439e-11d4-b5e7-00400543a266" is changed to am
				XElement style = XElement.Parse(dtoRepos.GetDTO("4d312f11-439e-11d4-b5e7-00400543a266").Xml);
				Assert.That(style.Element("Rules").Element("Prop").Element("WsStyles9999").Elements("WsProp").Skip(1).First().Attribute("ws").Value, Is.EqualTo("am"));
				// Verify that am-Ethi is changed to am in xWss properties of LangProject b8bdad3d-9006-46f0-83e8-ae1d1726f2ad.
				XElement langProj = XElement.Parse(dtoRepos.GetDTO("b8bdad3d-9006-46f0-83e8-ae1d1726f2ad").Xml);
				Assert.That(langProj.Element("AnalysisWss").Element("Uni").Value, Is.EqualTo("en am"));
				Assert.That(langProj.Element("CurVernWss").Element("Uni").Value, Is.EqualTo("seh am fr"));
				Assert.That(langProj.Element("CurAnalysisWss").Element("Uni").Value, Is.EqualTo("en am"));
				Assert.That(langProj.Element("CurPronunWss").Element("Uni").Value, Is.EqualTo("am"));
				Assert.That(langProj.Element("VernWss").Element("Uni").Value, Is.EqualTo("am"));
				// Verify that WritingSystem/Uni is changed to am in ReversalIndex" guid="62105696-da6c-405e-b87f-a2a0294bb179
				XElement ri = XElement.Parse(dtoRepos.GetDTO("62105696-da6c-405e-b87f-a2a0294bb179").Xml);
				Assert.That(ri.Element("WritingSystem").Element("Uni").Value, Is.EqualTo("am"));
				//	and CmPossibilityList" guid="b30aa28d-7510-49e6-b9ac-bc1902398ce6"
				XElement pl = XElement.Parse(dtoRepos.GetDTO("b30aa28d-7510-49e6-b9ac-bc1902398ce6").Xml);
				Assert.That(pl.Element("WritingSystem").Element("Uni").Value, Is.EqualTo("am"));
				//  and CmBaseAnnotation" guid="dc747a85-ceb6-491e-8b54-7fc37d7b2f80"
				XElement cba = XElement.Parse(dtoRepos.GetDTO("dc747a85-ceb6-491e-8b54-7fc37d7b2f80").Xml);
				Assert.That(cba.Element("WritingSystem").Element("Uni").Value, Is.EqualTo("am"));
				// Several other classes have WritingSystem, but we're checking ALL objects, so I think three test cases is plenty.

				// Check the layout
				XElement layoutElt = XElement.Parse(File.ReadAllText(sampleLayout, Encoding.UTF8));
				Assert.That(layoutElt.Element("layout").Element("part").Attribute("ws").Value, Is.EqualTo("am"));
				Assert.That(layoutElt.Element("layout").Elements("part").Skip(1).First().Attribute("ws").Value, Is.EqualTo("vernacular"));
				Assert.That(layoutElt.Element("layout").Elements("part").Skip(2).First().Attribute("ws").Value, Is.EqualTo("$ws=all analysis"));
				Assert.That(layoutElt.Element("layout").Elements("part").Skip(3).First().Attribute("ws").Value, Is.EqualTo("$ws=am"));
				Assert.That(layoutElt.Element("layout").Elements("part").Skip(4).First().Attribute("ws").Value, Is.EqualTo("am-fonipa,am"));
				Assert.That(layoutElt.Element("layout").Elements("part").Skip(4).First().Attribute("visibleWritingSystems").Value, Is.EqualTo("am-fonipa,am"));

				// Check the local settings.
				XElement propTable = XElement.Parse(File.ReadAllText(sampleSettings, Encoding.UTF8));
				Assert.That(propTable.Element("Property").Element("value").Value.Contains("5062001%am"));
				Assert.That(propTable.Element("Property").Element("value").Value.Contains("5112002%am"));
				Assert.That(propTable.Element("Property").Element("value").Value.Contains("103%am"));
				Assert.That(propTable.Elements("Property").Skip(1).First().Element("value").Value.Contains("ws=\"am\""));
				Assert.That(propTable.Elements("Property").Skip(2).First().Element("value").Value.Contains("ws=\"$ws=am\""));
				Assert.That(propTable.Elements("Property").Skip(3).First().Element("value").Value.Contains("ws=\"$wsName\""));
				Assert.That(propTable.Elements("Property").Skip(4).First().Element("value").Value.Contains("ws=\"$ws=reversal\""));
				Assert.That(propTable.Elements("Property").Skip(5).First().Element("value").Value, Is.EqualTo("am"));
				Assert.That(propTable.Elements("Property").Skip(6).First().Element("value").Value.Contains("ws=\"am\""));
				Assert.That(propTable.Elements("Property").Skip(7).First().Element("value").Value.Contains("ws=\"$ws=am\""));
			}
			finally
			{
				if (Directory.Exists(projectFolder))
					Directory.Delete(projectFolder, true);
			}
		}
	}
}
