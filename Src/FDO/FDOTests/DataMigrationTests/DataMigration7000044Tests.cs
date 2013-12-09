using System;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.DomainServices.DataMigration;
using SIL.Utils;

namespace SIL.FieldWorks.FDO.FDOTests.DataMigrationTests
{
	/// <summary>
	/// Tests the indicated migration.
	/// </summary>
	[TestFixture]
	public class DataMigration7000044Tests : DataMigrationTestsBase
	{
		/// <summary/>
		[SuppressMessage("Gendarme.Rules.Portability", "NewLineLiteralRule",
			Justification="Newline of sampleLayoutData is in source file and so will be correct according to current platform")]
		public DataMigration7000044Tests()
		{
		}

		private string sampleLayoutData =
			@"<LayoutInventory>
				<layout class='LexEntry' type='jtview' name='publishStemPara' css='$fwstyle=Dictionary-Normal' version='11'>
					<part ref='MLHeadWordPub' label='Headword' before='' sep=' ' after='  ' ws='x-kal' wsType='vernacular' style='Dictionary-Headword'  />
					<part ref='MLHeadWordPub' label='Headword' before='' sep=' ' after='  ' ws='vernacular' wsType='vernacular' style='Dictionary-Headword'  />
					<part ref='MLHeadWordPub' label='Headword' before='' sep=' ' after='  ' ws='$ws=all analysis' wsType='vernacular' style='Dictionary-Headword'  />
					<part ref='MLHeadWordPub' label='Headword' before='' sep=' ' after='  ' ws='$ws=x-kal' wsType='vernacular' style='Dictionary-Headword'  />
					<part ref='MLHeadWordPub' label='Headword' before='' sep=' ' after='  ' ws='x-kal-fonipa,x-kal' visibleWritingSystems='x-kal-fonipa,x-kal' wsType='vernacular' style='Dictionary-Headword'  />
				</layout>
			</LayoutInventory>
			";

		private string sampleLocalSettingsData =
			"<ArrayOfProperty xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">"
			+"	<Property>"
			+" <name>db$local$InterlinConfig_Edit_Interlinearizer</name>"
			+" <value xsi:type=\"xsd:string\">EditableInterlinLineChoices,5062001%,5062001%x-kal,5112002%,5112002%x-kal,103%,103%x-kal,5112004%,5112004%fr,5112003%,5112003%fr,5060001%en,5060001%fr,5059003%,5059003%fr,-61%en,-61%fr,-63%en,-63%fr,-62%en,-62%fr</value>"
			+ " </Property> "
			+"<Property>"
			+"<name>db$local$LexDb.Entries_sorter</name>"
			+"<value xsi:type=\"xsd:string\">&lt;sorter assemblyPath=\"Filters.dll\" class=\"SIL.FieldWorks.Filters.GenRecordSorter\"&gt;&lt;comparer assemblyPath=\"Filters.dll\" class=\"SIL.FieldWorks.Filters.StringFinderCompare\"&gt;&lt;finder assemblyPath=\"XMLViews.dll\" class=\"SIL.FieldWorks.Common.Controls.SortMethodFinder\" layout=\"EntryHeadwordForEntry\" sortmethod=\"FullSortKey\" ws=\"vernacular\"&gt;&lt;column layout=\"EntryHeadwordForEntry\" label=\"Headword\" ws=\"$ws=vernacular\" width=\"72000\" sortmethod=\"FullSortKey\" cansortbylength=\"true\" visibility=\"always\" /&gt;&lt;/finder&gt;&lt;comparer assemblyPath=\"Filters.dll\" class=\"SIL.FieldWorks.Filters.WritingSystemComparer\" ws=\"x-kal\" /&gt;&lt;/comparer&gt;&lt;/sorter&gt;</value>"
			+"</Property>"
			+ "<Property>"
			+ "<name>db$local$LexDb.Entries_sorter</name>"
			+ "<value xsi:type=\"xsd:string\">&lt;sorter assemblyPath=\"Filters.dll\" class=\"SIL.FieldWorks.Filters.GenRecordSorter\"&gt;&lt;comparer assemblyPath=\"Filters.dll\" class=\"SIL.FieldWorks.Filters.StringFinderCompare\"&gt;&lt;finder assemblyPath=\"XMLViews.dll\" class=\"SIL.FieldWorks.Common.Controls.SortMethodFinder\" layout=\"EntryHeadwordForEntry\" sortmethod=\"FullSortKey\" ws=\"vernacular\"&gt;&lt;column layout=\"EntryHeadwordForEntry\" label=\"Headword\" ws=\"$ws=vernacular\" width=\"72000\" sortmethod=\"FullSortKey\" cansortbylength=\"true\" visibility=\"always\" /&gt;&lt;/finder&gt;&lt;comparer assemblyPath=\"Filters.dll\" class=\"SIL.FieldWorks.Filters.WritingSystemComparer\" ws=\"$ws=x-kal\" /&gt;&lt;/comparer&gt;&lt;/sorter&gt;</value>"
			+ "</Property>"
			+ "<Property>"
			+ "<name>db$local$LexDb.Entries_sorter</name>"
			+ "<value xsi:type=\"xsd:string\">&lt;sorter assemblyPath=\"Filters.dll\" class=\"SIL.FieldWorks.Filters.GenRecordSorter\"&gt;&lt;comparer assemblyPath=\"Filters.dll\" class=\"SIL.FieldWorks.Filters.StringFinderCompare\"&gt;&lt;finder assemblyPath=\"XMLViews.dll\" class=\"SIL.FieldWorks.Common.Controls.SortMethodFinder\" layout=\"EntryHeadwordForEntry\" sortmethod=\"FullSortKey\" ws=\"vernacular\"&gt;&lt;column layout=\"EntryHeadwordForEntry\" label=\"Headword\" ws=\"$ws=vernacular\" width=\"72000\" sortmethod=\"FullSortKey\" cansortbylength=\"true\" visibility=\"always\" /&gt;&lt;/finder&gt;&lt;comparer assemblyPath=\"Filters.dll\" class=\"SIL.FieldWorks.Filters.WritingSystemComparer\" ws=\"$wsName\" /&gt;&lt;/comparer&gt;&lt;/sorter&gt;</value>"
			+ "</Property>"
			+ "<Property>"
			+ "<name>db$local$LexDb.Entries_sorter</name>"
			+ "<value xsi:type=\"xsd:string\">&lt;sorter assemblyPath=\"Filters.dll\" class=\"SIL.FieldWorks.Filters.GenRecordSorter\"&gt;&lt;comparer assemblyPath=\"Filters.dll\" class=\"SIL.FieldWorks.Filters.StringFinderCompare\"&gt;&lt;finder assemblyPath=\"XMLViews.dll\" class=\"SIL.FieldWorks.Common.Controls.SortMethodFinder\" layout=\"EntryHeadwordForEntry\" sortmethod=\"FullSortKey\" ws=\"vernacular\"&gt;&lt;column layout=\"EntryHeadwordForEntry\" label=\"Headword\" ws=\"$ws=vernacular\" width=\"72000\" sortmethod=\"FullSortKey\" cansortbylength=\"true\" visibility=\"always\" /&gt;&lt;/finder&gt;&lt;comparer assemblyPath=\"Filters.dll\" class=\"SIL.FieldWorks.Filters.WritingSystemComparer\" ws=\"$ws=reversal\" /&gt;&lt;/comparer&gt;&lt;/sorter&gt;</value>"
			+ "</Property>"
			+ "<Property> <name>db$local$ConcordanceWs</name> <value xsi:type=\"xsd:string\">x-kal</value></Property>"
			+ "<Property> <name>db$local$WordformInventory.Wordforms_sorter</name> <value xsi:type=\"xsd:string\">>&lt;sorter assemblyPath=\"Filters.dll\" class=\"SIL.FieldWorks.Filters.GenRecordSorter\"&gt;&lt;comparer assemblyPath=\"Filters.dll\" class=\"SIL.FieldWorks.Filters.StringFinderCompare\"&gt;&lt;finder assemblyPath=\"XMLViews.dll\" class=\"SIL.FieldWorks.Common.Controls.LayoutFinder\" layout=\"\"&gt;&lt;column label=\"Form\" width=\"30%\" cansortbylength=\"true\" ws=\"$ws=best vernacular\" field=\"Form\"&gt;&lt;span&gt;&lt;properties&gt;&lt;bold value=\"off\" /&gt;&lt;/properties&gt;&lt;string field=\"Form\" ws=\"best vernacular\" /&gt;&lt;/span&gt;&lt;/column&gt;&lt;/finder&gt;&lt;comparer assemblyPath=\"Filters.dll\" class=\"SIL.FieldWorks.Filters.WritingSystemComparer\" ws=\"x-kal\" /&gt;&lt;/comparer&gt;&lt;/sorter&gt;</value> </Property>"
			+ "<Property>"
			+ "<name>db$local$lexiconEdit_lexentryList_ColumnList</name> "
			+ "<value xsi:type=\"xsd:string\">&lt;root version=\"14\"&gt;&lt;column layout=\"LexemeFormForEntry\" common=\"true\" width=\"72000\" ws=\"$ws=x-kal\" sortmethod=\"MorphSortKey\" cansortbylength=\"true\" visibility=\"always\" transduce=\"LexEntry.LexemeForm.Form\" transduceCreateClass=\"MoStemAllomorph\" originalWs=\"vernacular\" originalLabel=\"Lexeme Form\" label=\"Lexeme Form (Sui_ipa)\" /&gt;&lt;column layout=\"EntryHeadwordForEntry\" label=\"Headword\" ws=\"$ws=vernacular\" width=\"72000\" sortmethod=\"FullSortKey\" cansortbylength=\"true\" visibility=\"always\" /&gt;&lt;column layout=\"GlossesForSense\" multipara=\"true\" width=\"72000\" ws=\"$ws=zh-CN\" transduce=\"LexSense.Gloss\" cansortbylength=\"true\" visibility=\"always\" originalWs=\"analysis\" originalLabel=\"Glosses\" label=\"Glosses (ManS)\" /&gt;&lt;column layout=\"GrammaticalInfoFullForSense\" headerlabel=\"Grammatical Info.\" chooserFilter=\"external\" label=\"Grammatical Info. (Full)\" multipara=\"true\" width=\"72000\" visibility=\"always\"&gt;&lt;dynamicloaderinfo assemblyPath=\"FdoUi.dll\" class=\"SIL.FieldWorks.FdoUi.PosFilter\" /&gt;&lt;/column&gt;&lt;/root&gt;</value>"
			+ "</Property>"
			+ "</ArrayOfProperty>";
		/// <summary>
		/// Test the migration from version 7000018 to 7000019.
		/// </summary>
		[Test]
		public void DataMigration7000044Test()
		{
			var projectFolder = Path.GetTempPath();
			var storePath = Path.Combine(projectFolder, FdoFileHelper.ksWritingSystemsDir);
			PrepareStore(storePath);
			var testDataPath = Path.Combine(DirectoryFinder.FwSourceDirectory, "FDO/FDOTests/TestData");
			var testEnglishPath = Path.Combine(storePath, "en.ldml");
			File.Copy(Path.Combine(testDataPath, "en_7000043.ldml"), testEnglishPath);
			File.SetAttributes(testEnglishPath, FileAttributes.Normal); // don't want to copy readonly property.
			var xkalPath = Path.Combine(storePath, "x-kal.ldml");
			File.Copy(Path.Combine(testDataPath, "x-kal_7000043.ldml"), xkalPath);
			File.SetAttributes(xkalPath, FileAttributes.Normal); // don't want to copy readonly property.
			var xkalFonipaPath = Path.Combine(storePath, "x-kal-fonipa.ldml");
			File.Copy(Path.Combine(testDataPath, "x-kal-fonipa_7000043.ldml"), xkalFonipaPath);
			File.SetAttributes(xkalFonipaPath, FileAttributes.Normal); // don't want to copy readonly property.

			var dtos = DataMigrationTestServices.ParseProjectFile("DataMigration7000044.xml");
			// Create all the Mock classes for the classes in my test data.
			var mockMDC = new MockMDCForDataMigration();
			mockMDC.AddClass(1, "CmObject", null, new List<string> { "LexEntry", "LangProject", "LexSense", "LexDb",
				"ReversalEntry", "StStyle", "CmPossibilityList", "CmBaseAnnotation" });
			mockMDC.AddClass(2, "LangProject", "CmObject", new List<string>());
			mockMDC.AddClass(3, "LexEntry", "CmObject", new List<string>());
			mockMDC.AddClass(4, "LexSense", "CmObject", new List<string>());
			mockMDC.AddClass(5, "LexDb", "CmObject", new List<string>());
			mockMDC.AddClass(6, "ReversalEntry", "CmObject", new List<string>());
			mockMDC.AddClass(7, "StStyle", "CmObject", new List<string>());
			mockMDC.AddClass(8, "CmPossibilityList", "CmObject", new List<string>());
			mockMDC.AddClass(9, "CmBaseAnnotation", "CmObject", new List<string>());

			var settingsFolder = Path.Combine(projectFolder, FdoFileHelper.ksConfigurationSettingsDir);
			Directory.CreateDirectory(settingsFolder);
			var sampleLayout = Path.Combine(settingsFolder, "Test_Layouts.xml");
			File.WriteAllText(sampleLayout, sampleLayoutData, Encoding.UTF8);
			var sampleSettings = Path.Combine(settingsFolder, "db$local$Settings.xml");
			File.WriteAllText(sampleSettings, sampleLocalSettingsData, Encoding.UTF8);

			IDomainObjectDTORepository dtoRepos = new DomainObjectDtoRepository(7000043, dtos, mockMDC, projectFolder);
			// Do the migration.
			m_dataMigrationManager.PerformMigration(dtoRepos, 7000044, new DummyProgressDlg());

			// Verification Phase
			Assert.AreEqual(7000044, dtoRepos.CurrentModelVersion, "Wrong updated version.");

			// Todo:
			// Verify that en.ldml is unchanged.
			Assert.That(File.Exists(testEnglishPath));
			// Verify that x-kal.ldml is renamed to qaa-x-kal and content changed
			Assert.That(File.Exists(Path.Combine(storePath, "qaa-x-kal.ldml")));
			// Verify that x-kal-fonipa.ldml is renamed to qaa-fonipa-x-kal and content changed
			Assert.That(File.Exists(Path.Combine(storePath, "qaa-x-kal-fonipa.ldml")));
			// Verify that AUni data in LexEntry" guid="7ecbb299-bf35-4795-a5cc-8d38ce8b891c tag is changed to qaa-x-kal
			var entry = XElement.Parse(dtoRepos.GetDTO("7ecbb299-bf35-4795-a5cc-8d38ce8b891c").Xml);
			Assert.That(entry.Element("CitationForm").Element("AUni").Attribute("ws").Value, Is.EqualTo("qaa-x-kal"));
			// Verify that AStr data in LexSense" guid="e3c2d179-3ccd-431e-ac2e-100bdb883680" tag is changed to qaa-x-kal
			var sense = XElement.Parse(dtoRepos.GetDTO("e3c2d179-3ccd-431e-ac2e-100bdb883680").Xml);
			Assert.That(sense.Element("Definition").Elements("AStr").Skip(1).First().Attribute("ws").Value, Is.EqualTo("qaa-x-kal"));
			Assert.That(sense.Element("Definition").Elements("AStr").Count(), Is.EqualTo(2), "french should be deleted because empty");
			// Verify that the empty alternatives get removed.
			Assert.That(sense.Element("Bibliography").Elements("AUni").First().Attribute("ws").Value, Is.EqualTo("en"));
			Assert.That(sense.Element("Bibliography").Elements("AUni").Count(), Is.EqualTo(1));
			// Verify that Run data in LexSense" guid="e3c2d179-3ccd-431e-ac2e-100bdb883680" tag is changed to qaa-x-kal
			Assert.That(sense.Element("Definition").Element("AStr").Elements("Run").Skip(1).First().Attribute("ws").Value, Is.EqualTo("qaa-x-kal"));
			// Check LiftResidue lang attributes are fixed; note that a result containing lang=&quot;qaa-x-kal&quot
			// would also be acceptable, perhaps even more to be expected, but converting the &quot; s here to " is acceptable.
			Assert.That(sense.Element("LiftResidue").Element("Uni").Value.Contains("lang=\"qaa-x-kal\""));
			// Verify that WsProp data in StStyle guid="4d312f11-439e-11d4-b5e7-00400543a266" is changed to qaa-x-kal
			var style = XElement.Parse(dtoRepos.GetDTO("4d312f11-439e-11d4-b5e7-00400543a266").Xml);
			Assert.That(style.Element("Rules").Element("Prop").Element("WsStyles9999").Elements("WsProp").Skip(1).First().Attribute("ws").Value, Is.EqualTo("qaa-x-kal"));
			// Verify that x-kal is changed to qaa-x-kal in xWss properties of LangProject b8bdad3d-9006-46f0-83e8-ae1d1726f2ad.
			var langProj = XElement.Parse(dtoRepos.GetDTO("b8bdad3d-9006-46f0-83e8-ae1d1726f2ad").Xml);
			Assert.That(langProj.Element("AnalysisWss").Element("Uni").Value, Is.EqualTo("en qaa-x-kal"));
			Assert.That(langProj.Element("CurVernWss").Element("Uni").Value, Is.EqualTo("seh qaa-x-kal fr"));
			Assert.That(langProj.Element("CurAnalysisWss").Element("Uni").Value, Is.EqualTo("en qaa-x-kal"));
			Assert.That(langProj.Element("CurPronunWss").Element("Uni").Value, Is.EqualTo("qaa-x-kal"));
			Assert.That(langProj.Element("VernWss").Element("Uni").Value, Is.EqualTo("qaa-x-kal"));
			// Verify that WritingSystem/Uni is changed to qaa-x-kal in ReversalIndex" guid="62105696-da6c-405e-b87f-a2a0294bb179
			var ri = XElement.Parse(dtoRepos.GetDTO("62105696-da6c-405e-b87f-a2a0294bb179").Xml);
			Assert.That(ri.Element("WritingSystem").Element("Uni").Value, Is.EqualTo("qaa-x-kal"));
			//	and CmPossibilityList" guid="b30aa28d-7510-49e6-b9ac-bc1902398ce6"
			var pl = XElement.Parse(dtoRepos.GetDTO("b30aa28d-7510-49e6-b9ac-bc1902398ce6").Xml);
			Assert.That(pl.Element("WritingSystem").Element("Uni").Value, Is.EqualTo("qaa-x-kal"));
			//  and CmBaseAnnotation" guid="dc747a85-ceb6-491e-8b54-7fc37d7b2f80"
			var cba = XElement.Parse(dtoRepos.GetDTO("dc747a85-ceb6-491e-8b54-7fc37d7b2f80").Xml);
			Assert.That(cba.Element("WritingSystem").Element("Uni").Value, Is.EqualTo("qaa-x-kal"));
			// Several other classes have WritingSystem, but we're checking ALL objects, so I think three test cases is plenty.

			// Check the layout
			var layoutElt = XElement.Parse(File.ReadAllText(sampleLayout, Encoding.UTF8));
			Assert.That(layoutElt.Element("layout").Element("part").Attribute("ws").Value, Is.EqualTo("qaa-x-kal"));
			Assert.That(layoutElt.Element("layout").Elements("part").Skip(1).First().Attribute("ws").Value, Is.EqualTo("vernacular"));
			Assert.That(layoutElt.Element("layout").Elements("part").Skip(2).First().Attribute("ws").Value, Is.EqualTo("$ws=all analysis"));
			Assert.That(layoutElt.Element("layout").Elements("part").Skip(3).First().Attribute("ws").Value, Is.EqualTo("$ws=qaa-x-kal"));
			Assert.That(layoutElt.Element("layout").Elements("part").Skip(4).First().Attribute("ws").Value, Is.EqualTo("qaa-x-kal-fonipa,qaa-x-kal"));
			Assert.That(layoutElt.Element("layout").Elements("part").Skip(4).First().Attribute("visibleWritingSystems").Value, Is.EqualTo("qaa-x-kal-fonipa,qaa-x-kal"));

			// Check the local settings.
			var propTable = XElement.Parse(File.ReadAllText(sampleSettings, Encoding.UTF8));
			Assert.That(propTable.Element("Property").Element("value").Value.Contains("5062001%qaa-x-kal"));
			Assert.That(propTable.Element("Property").Element("value").Value.Contains("5112002%qaa-x-kal"));
			Assert.That(propTable.Element("Property").Element("value").Value.Contains("103%qaa-x-kal"));
			Assert.That(propTable.Elements("Property").Skip(1).First().Element("value").Value.Contains("ws=\"qaa-x-kal\""));
			Assert.That(propTable.Elements("Property").Skip(2).First().Element("value").Value.Contains("ws=\"$ws=qaa-x-kal\""));
			Assert.That(propTable.Elements("Property").Skip(3).First().Element("value").Value.Contains("ws=\"$wsName\""));
			Assert.That(propTable.Elements("Property").Skip(4).First().Element("value").Value.Contains("ws=\"$ws=reversal\""));
			Assert.That(propTable.Elements("Property").Skip(5).First().Element("value").Value, Is.EqualTo("qaa-x-kal"));
			Assert.That(propTable.Elements("Property").Skip(6).First().Element("value").Value.Contains("ws=\"qaa-x-kal\""));
			Assert.That(propTable.Elements("Property").Skip(7).First().Element("value").Value.Contains("ws=\"$ws=qaa-x-kal\""));
		}

		private static void PrepareStore(string path)
		{
			if (Directory.Exists(path))
			{
				foreach (string file in Directory.GetFiles(path))
					File.Delete(file);
			}
			else
			{
				Directory.CreateDirectory(path);
			}
		}

		/// <summary>
		/// Test it.
		/// </summary>
		[Test]
		public void GetNextDuplPart()
		{
			Assert.That(DataMigration7000044.GetNextDuplPart(null), Is.EqualTo("dupl1"));
			Assert.That(DataMigration7000044.GetNextDuplPart(""), Is.EqualTo("dupl1"));
			Assert.That(DataMigration7000044.GetNextDuplPart("abc"), Is.EqualTo("abc-dupl1"));
			Assert.That(DataMigration7000044.GetNextDuplPart("dupl1"), Is.EqualTo("dupl2"));
			Assert.That(DataMigration7000044.GetNextDuplPart("abc-def-dupl12"), Is.EqualTo("abc-def-dupl13"));
		}
	}
}
