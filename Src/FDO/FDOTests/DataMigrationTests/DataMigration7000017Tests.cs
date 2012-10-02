// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: DataMigration7000017Tests.cs
// Responsibility: mcconnel
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.FDO.DomainServices.DataMigration;

namespace SIL.FieldWorks.FDO.FDOTests.DataMigrationTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for migration from version 7000016 to 7000017.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public sealed class DataMigrationTests7000017 : DataMigrationTestsBase
	{
		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method DeleteWeatherListAndField.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void DeleteWeatherListAndField()
		{
			var dtos = DataMigrationTestServices.ParseProjectFile("DataMigration7000017.xml");

			var mockMdc = SetupMdc();

			IDomainObjectDTORepository repoDTO = new DomainObjectDtoRepository(7000016, dtos, mockMdc, null);

			// SUT: Do the migration.
			m_dataMigrationManager.PerformMigration(repoDTO, 7000017);

			// Verification Phase
			Assert.AreEqual(7000017, repoDTO.CurrentModelVersion, "Wrong updated version.");

			DomainObjectDTO dtoLP = null;
			foreach (DomainObjectDTO dto in repoDTO.AllInstancesSansSubclasses("LangProject"))
			{
				Assert.IsNull(dtoLP, "Only one LangProject object should exist");
				dtoLP = dto;
			}
			Assert.NotNull(dtoLP, "The LangProject object should exist");
			string sXml = dtoLP.Xml;
			Assert.IsFalse(sXml.Contains("<WeatherConditions>"), "The <WeatherConditions> element should have disappeared");
			string sLpOwnerGuid = GetGuidAsOwnerGuid(sXml);

			DomainObjectDTO dtoNbk = null;
			foreach (DomainObjectDTO dto in repoDTO.AllInstancesSansSubclasses("RnResearchNbk"))
			{
				Assert.IsNull(dtoNbk, "Only one RnResearchNbk should exist");
				Assert.IsTrue(dto.Xml.Contains(sLpOwnerGuid), "The RnResearchNbk should be owned by the LangProject");
				dtoNbk = dto;
			}
			Assert.NotNull(dtoNbk, "The RnResearchNbk should exist");
			string sNbkOwnerGuid = GetGuidAsOwnerGuid(dtoNbk.Xml);
			int cList = 0;
			foreach (DomainObjectDTO dto in repoDTO.AllInstancesSansSubclasses("CmPossibilityList"))
			{
				sXml = dto.Xml;
				Assert.IsTrue(sXml.Contains(sNbkOwnerGuid), "Possibility List must be owned by Data Notebook");
				++cList;
			}
			Assert.AreEqual(1, cList, "Only one CmPossibilityList should exist");

			foreach (DomainObjectDTO dto in repoDTO.AllInstancesWithSubclasses("RnGenericRec"))
			{
				Assert.IsFalse(dto.Xml.Contains("<Weather"), "Any <Weather> element should have disappeared");
				Assert.IsFalse(dto.Xml.Contains("<Custom name="), "No <Custom> element should have been created");
			}

			// This test file has three overlays; the first refers to a weather item and the weather possibility list,
			// and should be delted. The second refers to a non-weather possibility, and the third to a non-weather
			// possibility list; they should survive.
			Assert.That(repoDTO.AllInstancesSansSubclasses("CmOverlay"), Has.Count.EqualTo(2));
		}

		private static string GetGuidAsOwnerGuid(string sXml)
		{
			int idxGuid = sXml.IndexOf(" guid=\"");
			Assert.Greater(idxGuid, 0, "Object element must have a guid attribute.");
			string sGuid = sXml.Substring(idxGuid + 1);
			int idxEnd = sGuid.IndexOf('"', 6);
			Assert.Greater(idxEnd, 5, "guid attribute must be terminated.");
			return " owner" + sGuid.Substring(0, idxEnd + 1);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that we convert the weather list to a custom list when some record
		/// has a reference to a Weather value.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void ConvertWeatherToCustomListAndFieldForWeatherField()
		{
			ConvertWeatherToCustomListAndField("DataMigration7000017A.xml");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that we convert the weather list to a custom list when some record
		/// has text tagged with a Weather value.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void ConvertWeatherToCustomListAndFieldForPhraseTagsField()
		{
			ConvertWeatherToCustomListAndField("DataMigration7000017B.xml");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that we convert the weather list to a custom list when some overlay contains
		/// a mixture of weather and other tags.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void ConvertWeatherToCustomListAndFieldForMixedOverlay()
		{
			ConvertWeatherToCustomListAndField("DataMigration7000017C.xml");
		}

		private void ConvertWeatherToCustomListAndField(string datafile)
		{
			var dtos = DataMigrationTestServices.ParseProjectFile(datafile);

			var mockMdc = SetupMdc();

			IDomainObjectDTORepository repoDTO = new DomainObjectDtoRepository(7000016, dtos, mockMdc, null);

			// SUT: Do the migration.
			m_dataMigrationManager.PerformMigration(repoDTO, 7000017);

			// Verification Phase
			Assert.AreEqual(7000017, repoDTO.CurrentModelVersion, "Wrong updated version.");

			DomainObjectDTO dtoLP = null;
			foreach (DomainObjectDTO dto in repoDTO.AllInstancesSansSubclasses("LangProject"))
			{
				Assert.IsNull(dtoLP, "Only one LangProject object should exist");
				dtoLP = dto;
			}
			Assert.NotNull(dtoLP, "The LangProject object should exist");
			string sXml = dtoLP.Xml;
			Assert.IsFalse(sXml.Contains("<WeatherConditions>"), "The <WeatherConditions> element should have disappeared");
			string sLpOwnerGuid = GetGuidAsOwnerGuid(sXml);

			DomainObjectDTO dtoNbk = null;
			foreach (DomainObjectDTO dto in repoDTO.AllInstancesSansSubclasses("RnResearchNbk"))
			{
				Assert.IsNull(dtoNbk, "Only one RnResearchNbk should exist");
				Assert.IsTrue(dto.Xml.Contains(sLpOwnerGuid), "The RnResearchNbk should be owned by the LangProject");
				dtoNbk = dto;
			}
			Assert.NotNull(dtoNbk, "The RnResearchNbk should exist");
			string sNbkOwnerGuid = GetGuidAsOwnerGuid(dtoNbk.Xml);
			int cList = 0;
			DomainObjectDTO dtoTypeList = null;
			foreach (DomainObjectDTO dto in repoDTO.AllInstancesSansSubclasses("CmPossibilityList"))
			{
				sXml = dto.Xml;
				if (sXml.Contains(" ownerguid="))
				{
					Assert.IsTrue(sXml.Contains(sNbkOwnerGuid), "Any remaining owned Possibility List must be owned by Data Notebook");
					Assert.IsNull(dtoTypeList, "Only one list should be owned by the Data Notebook");
					dtoTypeList = dto;
				}
				++cList;
			}
			Assert.AreEqual(2, cList, "Two CmPossibilityList objects should still exist");

			foreach (DomainObjectDTO dto in repoDTO.AllInstancesWithSubclasses("RnGenericRec"))
			{
				sXml = dto.Xml;
				Assert.IsFalse(sXml.Contains("<Weather"), "Any <Weather> element should have disappeared");
				int idxCustom = sXml.IndexOf("<Custom");
				if (idxCustom >= 0)
				{
					string sCustom = sXml.Substring(idxCustom);
					Assert.IsTrue(sCustom.StartsWith("<Custom name=\"Weather\">"), "Converted weather element has proper start element");
					Assert.IsTrue(sCustom.Contains("</Custom>"), "Converted weather element has proper end element");
				}
			}

			Assert.IsTrue(mockMdc.FieldExists("RnGenericRec", "Weather", false), "Weather field exists in RnGenericRec");
			int flid = mockMdc.GetFieldId("RnGenericRec", "Weather", false);
			Assert.IsTrue(mockMdc.IsCustom(flid), "Weather field is a custom field");

			// This test file has three overlays; none should be deleted, since we are keeping the weather list.
			Assert.That(repoDTO.AllInstancesSansSubclasses("CmOverlay"), Has.Count.EqualTo(3));
		}

		private static MockMDCForDataMigration SetupMdc()
		{
			var mockMdc = new MockMDCForDataMigration();
			mockMdc.AddClass(0, "CmObject", null, new List<string> { "CmProject", "CmMajorObject", "CmPossibility", "RnGenericRec" });
			mockMdc.AddClass(CmProjectTags.kClassId, "CmProject", "CmObject", new List<string> { "LangProject" });
			mockMdc.AddClass(CmMajorObjectTags.kClassId, "CmMajorObject", "CmObject", new List<string> { "RnResearchNbk", "CmPossibilityList" });
			mockMdc.AddClass(CmPossibilityTags.kClassId, "CmPossibility", "CmObject", new List<string>());
			mockMdc.AddClass(LangProjectTags.kClassId, "LangProject", "CmProject", new List<string>());
			mockMdc.AddClass(RnResearchNbkTags.kClassId, "RnResearchNbk", "CmMajorObject", new List<string>());
			mockMdc.AddClass(CmPossibilityListTags.kClassId, "CmPossibilityList", "CmMajorObject", new List<string>());
			mockMdc.AddClass(RnGenericRecTags.kClassId, "RnGenericRec", "CmObject", new List<string>());

			return mockMdc;
		}
	}
}
