using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.DomainServices.DataMigration;
using SIL.FieldWorks.FDO.Infrastructure;

namespace SIL.FieldWorks.FDO.FDOTests.DataMigrationTests
{
	/// <summary>
	/// Test framework for migration from version 7000018 to 7000019.
	/// </summary>
	[TestFixture]
	public sealed class DataMigrationTests7000019 : DataMigrationTestsBase
	{
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
		/// Test the migration from version 7000018 to 7000019.
		/// </summary>
		[Test]
		public void DataMigration7000019Test()
		{
			string storePath = Path.Combine(Path.GetTempPath(), FdoFileHelper.ksWritingSystemsDir);
			PrepareStore(storePath);
			string globalStorePath = DirectoryFinder.GlobalWritingSystemStoreDirectory;
			PrepareStore(globalStorePath);

			var dtos = DataMigrationTestServices.ParseProjectFile("DataMigration7000019Tests.xml");

			IFwMetaDataCacheManaged mockMdc = SetupMdc();

			IDomainObjectDTORepository dtoRepos = new DomainObjectDtoRepository(7000018, dtos, mockMdc, Path.GetTempPath(), FwDirectoryFinder.FdoDirectories);

			// Do the migration.
			m_dataMigrationManager.PerformMigration(dtoRepos, 7000019, new DummyProgressDlg());

			// Verification Phase
			Assert.AreEqual(7000019, dtoRepos.CurrentModelVersion, "Wrong updated version.");

			Assert.AreEqual(0, dtoRepos.AllInstancesSansSubclasses("LgWritingSystem").Count());
			Assert.AreEqual(0, dtoRepos.AllInstancesSansSubclasses("LgCollation").Count());
			Assert.AreEqual(0, dtoRepos.AllInstancesSansSubclasses("CmSortSpec").Count());

			DomainObjectDTO lpDto = dtoRepos.AllInstancesSansSubclasses("LangProject").First();
			XElement lpElem = XElement.Parse(lpDto.Xml);

			var kalabaPath = Path.Combine(storePath, "x-kal.ldml"); // Note this migration does NOT yet convert to qaa-x-kal
			var kalabaNode = XDocument.Parse(Encoding.UTF8.GetString(File.ReadAllBytes(kalabaPath))).Root;
			Assert.That(kalabaNode.Name.LocalName, Is.EqualTo("ldml"));
			var identityNode = kalabaNode.Element("identity");
			Assert.That(identityNode.Element("language").Attribute("type").Value, Is.EqualTo("x-kal"));
			var specialNode = kalabaNode.Element("special");
			XNamespace xmlns = "http://www.w3.org/2000/xmlns/";
			Assert.That(specialNode.Attribute(xmlns  + "palaso").Value, Is.EqualTo("urn://palaso.org/ldmlExtensions/v1"));
			XNamespace palaso = "urn://palaso.org/ldmlExtensions/v1";
			Assert.That(specialNode.Element(palaso + "languageName").Attribute("value").Value, Is.EqualTo("Kalaba"));
			Assert.That(specialNode.Element(palaso + "abbreviation").Attribute("value").Value, Is.EqualTo("Kal"));
			// Todo: check a lot more things.
			//Assert.AreEqual(1033, kalaba.LCID);

			//IWritingSystem frIpa = wsManager.Get("fr-fonipa-x-etic");
			//Assert.AreEqual("FrnI", frIpa.Abbreviation);
			//Assert.AreEqual("IPA Unicode 1.0", frIpa.Keyboard);

			var analysisWss = (string)lpElem.Element("AnalysisWss");
			//foreach (string id in analysisWss.Split(' '))
			//    Assert.IsTrue(wsManager.Exists(id));
			//var vernWss = (string)lpElem.Element("VernWss");
			//foreach (string id in vernWss.Split(' '))
			//    Assert.IsTrue(wsManager.Exists(id));

			//CheckWsProperty(dtoRepos.AllInstancesWithSubclasses("CmPossibilityList"), wsManager);

			//foreach (DomainObjectDTO dto in dtoRepos.AllInstancesWithValidClasses())
			//    CheckStringWsIds(wsManager, dto);

			DomainObjectDTO importSourceDto = dtoRepos.AllInstancesWithSubclasses("ScrImportSource").First();
			XElement importSourceElem = XElement.Parse(importSourceDto.Xml);
			Assert.AreEqual("x-kal", (string)importSourceElem.Element("WritingSystem").Element("Uni"));
			Assert.IsNull(importSourceElem.Element("ICULocale"));

			DomainObjectDTO mappingDto = dtoRepos.AllInstancesWithSubclasses("ScrMarkerMapping").First();
			XElement mappingElem = XElement.Parse(mappingDto.Xml);
			Assert.AreEqual("fr-fonipa-x-etic", (string)mappingElem.Element("WritingSystem").Element("Uni"));
			Assert.IsNull(mappingElem.Element("ICULocale"));

			DomainObjectDTO styleDto = dtoRepos.AllInstancesWithSubclasses("StStyle").First();
			XElement styleElem = XElement.Parse(styleDto.Xml);
			Assert.AreEqual("<default font>", (string)styleElem.Element("Rules").Element("Prop").Attribute("fontFamily"));
		}

		private static void CheckWsProperty(IEnumerable<DomainObjectDTO> dtos, IWritingSystemManager wsManager)
		{
			foreach (DomainObjectDTO dto in dtos)
			{
				XElement elem = XElement.Parse(dto.Xml);
				Assert.IsTrue(wsManager.Exists((string) elem.Element("WritingSystem")));
			}
		}

		private static void CheckStringWsIds(IWritingSystemManager wsManager, DomainObjectDTO dto)
		{
			XElement objElem = XElement.Parse(dto.Xml);
			foreach (XElement elem in objElem.Descendants())
			{
				switch (elem.Name.LocalName)
				{
					case "Run":
					case "AStr":
					case "AUni":
						XAttribute wsAttr = elem.Attribute("ws");
						if (wsAttr != null)
							Assert.IsTrue(wsManager.Exists(wsAttr.Value));
						break;
				}
			}
		}

		private static MockMDCForDataMigration SetupMdc()
		{
			var mockMdc = new MockMDCForDataMigration();
			mockMdc.AddClass(1, "CmObject", null, new List<string> { "CmProject", "CmSortSpec", "CmMajorObject", "UserViewField", "CmAnnotation", "FsFeatDefn", "LgWritingSystem", "LgCollation", "ScrImportSource", "ScrMarkerMapping", "StStyle" });
			mockMdc.AddClass(2, "CmProject", "CmObject", new List<string> { "LangProject" });
			mockMdc.AddClass(3, "LangProject", "CmProject", new List<string>());
			mockMdc.AddClass(4, "CmSortSpec", "CmObject", new List<string>());
			mockMdc.AddClass(5, "CmMajorObject", "CmObject", new List<string> { "CmPossibilityList", "WordformLookupList", "ReversalIndex" });
			mockMdc.AddClass(6, "CmPossibilityList", "CmMajorObject", new List<string>());
			mockMdc.AddClass(7, "WordformLookupList", "CmMajorObject", new List<string>());
			mockMdc.AddClass(8, "ReversalIndex", "CmMajorObject", new List<string>());
			mockMdc.AddClass(9, "UserViewField", "CmObject", new List<string>());
			mockMdc.AddClass(10, "CmAnnotation", "CmObject", new List<string> { "CmBaseAnnotation" });
			mockMdc.AddClass(11, "CmBaseAnnotation", "CmAnnotation", new List<string>());
			mockMdc.AddClass(12, "FsFeatDefn", "CmObject", new List<string> { "FsOpenFeature" });
			mockMdc.AddClass(13, "FsOpenFeature", "FsFeatDefn", new List<string>());
			mockMdc.AddClass(14, "LgWritingSystem", "CmObject", new List<string>());
			mockMdc.AddClass(15, "LgCollation", "CmObject", new List<string>());
			mockMdc.AddClass(16, "ScrImportSource", "CmObject", new List<string> { "ScrImportSFFiles" });
			mockMdc.AddClass(17, "ScrImportSFFiles", "ScrImportSource", new List<string>());
			mockMdc.AddClass(18, "ScrMarkerMapping", "CmObject", new List<string>());
			mockMdc.AddClass(19, "StStyle", "CmObject", new List<string>());
			return mockMdc;
		}
	}
}
