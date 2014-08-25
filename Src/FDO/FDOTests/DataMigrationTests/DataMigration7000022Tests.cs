using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.DomainServices.DataMigration;

namespace SIL.FieldWorks.FDO.FDOTests.DataMigrationTests
{
	/// <summary>
	/// Test framework for migration from version 7000021 to 7000022.
	/// </summary>
	[TestFixture]
	public sealed class DataMigrationTests7000022 : DataMigrationTestsBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the migration from version 7000021 to 7000022.
		/// (Fill names of Lists area lists with current UI possibilities.)
		/// This migration updates names for 26 lists in the following cultures: en (English),
		/// es (Spanish), fa (Farsi), fr (French), in (Indonesian), pt (Portuguese),
		/// ru (Russian), and zh-CN (Chinese). Some lists don't have all languages yet.
		/// But in this test, the data has all the lists available (no pathological cases).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DataMigration7000022Test()
		{
			var dtos = DataMigrationTestServices.ParseProjectFile("DataMigration7000022.xml");

			// Set up mock MDC.
			var mockMDC = new MockMDCForDataMigration();
			mockMDC.AddClass(1, "CmObject", null, new List<string> { "LangProject", "CmMajorObject",
																	 "DsDiscourseData" });
			mockMDC.AddClass(2, "LangProject", "CmObject", new List<string>());
			mockMDC.AddClass(3, "DsDiscourseData", "CmObject", new List<string>());
			mockMDC.AddClass(4, "CmMajorObject", "CmObject", new List<string> { "RnResearchNbk", "CmPossibilityList", "LexDb" });
			mockMDC.AddClass(5, "RnResearchNbk", "CmMajorObject", new List<string>());
			mockMDC.AddClass(6, "LexDb", "CmMajorObject", new List<string>());
			mockMDC.AddClass(7, "CmPossibilityList", "CmMajorObject", new List<string>());
			IDomainObjectDTORepository dtoRepos = new DomainObjectDtoRepository(7000021, dtos, mockMDC, null, FwDirectoryFinder.FdoDirectories);

			// SUT
			m_dataMigrationManager.PerformMigration(dtoRepos, 7000022, new DummyProgressDlg());

			// Verification section
			Assert.AreEqual(7000022, dtoRepos.CurrentModelVersion, "Wrong updated version.");

			// We should have 26 migrated possibility lists plus one unowned Custom list
			// that shouldn't be touched.
			var lists = dtoRepos.AllInstancesSansSubclasses("CmPossibilityList");
			Assert.AreEqual(27, lists.Count(), "Wrong number of Possibility Lists found.");

			// We need 'LangProject' and its DsDiscourseData, LexDb and RnResearchNbk objects.
			var lpDto = dtoRepos.AllInstancesSansSubclasses("LangProject").FirstOrDefault();
			Assert.IsNotNull(lpDto);
			var lpElement = XElement.Parse(lpDto.Xml);

			var discourseDataDto = dtoRepos.AllInstancesSansSubclasses("DsDiscourseData").FirstOrDefault();
			Assert.IsNotNull(discourseDataDto);
			var dsDataElement = XElement.Parse(discourseDataDto.Xml);

			var lexDbDto = dtoRepos.AllInstancesSansSubclasses("LexDb").FirstOrDefault();
			Assert.IsNotNull(lexDbDto);
			var lexDbElement = XElement.Parse(lexDbDto.Xml);

			var nbkDto = dtoRepos.AllInstancesSansSubclasses("RnResearchNbk").FirstOrDefault();
			Assert.IsNotNull(nbkDto);
			var nbkElement = XElement.Parse(nbkDto.Xml);

			// List 1: Get ChartMarkers list from DsDiscourseData
			var listNameElement = GetPossibilityListNameElement(dtoRepos, dsDataElement, "ChartMarkers");
			var altList = AssertNameHasXAlternates(listNameElement, 8);
			AssertNameAltExists(altList, "en", "Text Chart Markers");
			AssertNameAltExists(altList, "es", "Marcadores para la tabla de texto");
			AssertNameAltExists(altList, "fa", "نشانه جدول متن");
			AssertNameAltExists(altList, "fr", "Étiquettes de tableau de texte");
			AssertNameAltExists(altList, "id", "Penanda-penanda Bagan Teks");
			AssertNameAltExists(altList, "pt", "Marcadores de Quadro de Textos");
			AssertNameAltExists(altList, "ru", "Метки схемы текста");
			AssertNameAltExists(altList, "zh-CN", "文本图表标记");

			// List 3: Get AffixCategories list from LangProject
			listNameElement = GetPossibilityListNameElement(dtoRepos, lpElement, "AffixCategories");
			altList = AssertNameHasXAlternates(listNameElement, 1);
			AssertNameAltExists(altList, "en", "Affix Categories");

			// List 12: Get Positions list from LangProject
			listNameElement = GetPossibilityListNameElement(dtoRepos, lpElement, "Positions");
			altList = AssertNameHasXAlternates(listNameElement, 7);
			AssertNameAltExists(altList, "en", "Positions");
			AssertNameAltExists(altList, "fa", "شغلها");
			AssertNameAltExists(altList, "fr", "Positions");
			AssertNameAltExists(altList, "id", "Posisi");
			AssertNameAltExists(altList, "pt", "Posições");
			AssertNameAltExists(altList, "ru", "Позиции");
			AssertNameAltExists(altList, "zh-CN", "位置");

			// List 16: Get TimeOfDay list from LangProject
			listNameElement = GetPossibilityListNameElement(dtoRepos, lpElement, "TimeOfDay");
			altList = AssertNameHasXAlternates(listNameElement, 2);
			AssertNameAltExists(altList, "en", "Time Of Day");
			AssertNameAltExists(altList, "fr", "Moment de la journée");

			// List 19: Get DomainTypes list from LexDb
			listNameElement = GetPossibilityListNameElement(dtoRepos, lexDbElement, "DomainTypes");
			altList = AssertNameHasXAlternates(listNameElement, 8);
			AssertNameAltExists(altList, "en", "Academic Domains");
			AssertNameAltExists(altList, "es", "Dominios académicos");
			AssertNameAltExists(altList, "fa", "حوضه‌های دانشگاهی و علمی");
			AssertNameAltExists(altList, "fr", "Domaines techniques");
			AssertNameAltExists(altList, "id", "Ranah Akademis");
			AssertNameAltExists(altList, "pt", "Domínios Acadêmicos");
			AssertNameAltExists(altList, "ru", "Области знания");
			AssertNameAltExists(altList, "zh-CN", "学术领域");

			// List 26: Get the RecTypes Element in RnResearchNbk
			listNameElement = GetPossibilityListNameElement(dtoRepos, nbkElement, "RecTypes");
			altList = AssertNameHasXAlternates(listNameElement, 2);
			AssertNameAltExists(altList, "en", "Notebook Record Types");
			AssertNameAltExists(altList, "fr", "Types d'enregistrement de carnet");
		}

		private static IEnumerable<XElement> AssertNameHasXAlternates(XElement nameElement, int calternates)
		{
			if (calternates > 0)
				Assert.IsNotNull(nameElement, "No Name element for this list!");
			else
			{
				if (nameElement == null)
					return null;
				Assert.IsNull(nameElement.XPathSelectElements("AUni"),
					"Found Name alternates in list that should have none!");
			}
			var alternates = nameElement.XPathSelectElements("AUni");
			var defName = alternates.FirstOrDefault() == null ? "" : alternates.First().Value;
			Assert.AreEqual(calternates, alternates.Count(), String.Format(
				"List name ({0}) has wrong number of alternates.", defName));
			return alternates;
		}

		private static void AssertNameAltExists(IEnumerable<XElement> altList, string locale, string listName)
		{
			var ffoundIt = false;
			foreach (var nameAlt in altList)
			{
				var altLocale = nameAlt.Attribute("ws").Value;
				if (altLocale != locale)
					continue;
				ffoundIt = true;
				var altFound = nameAlt.Value;
				Assert.AreEqual(listName, altFound, String.Format(
					"Writing System {0} found with wrong name: {1}.", locale, altFound));
				break;
			}
			Assert.IsTrue(ffoundIt, String.Format("Didn't find expected name alternate for {0}.", locale));
		}

		private static XElement GetPossibilityListNameElement(IDomainObjectDTORepository dtoRepos,
			XElement ownerElement, string flidName)
		{
			Assert.IsNotNull(ownerElement.XPathSelectElement(flidName));
			var xPath = flidName+"/objsur";
			var dto = dtoRepos.GetDTO(ownerElement.XPathSelectElement(xPath).Attribute("guid").Value);
			return XElement.Parse(dto.Xml).XPathSelectElement("Name");
		}
	}
}